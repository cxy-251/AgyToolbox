using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using AgyToolbox.Core;

namespace AgyToolbox.Tools;

public class SysInfoTool : ITool
{
    public string Key => "sys";
    public string Name => "系统与网络诊断 (SysInfo)";
    public string Description => "一键速查硬件状态、磁盘余量可视化、活动网卡 IP 与网络连通延迟。";

    public async Task RunAsync(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=======================================================");
        Console.WriteLine("          💻 系统与网络状态一览 (SysInfo)              ");
        Console.WriteLine("=======================================================");
        Console.ResetColor();

        // 1. 系统基础
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("【操作系统与基础硬件】");
        Console.ResetColor();
        Console.WriteLine($"  计算机名:   {Environment.MachineName}");
        Console.WriteLine($"  操作系统:   {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        Console.WriteLine($"  逻辑处理器: {Environment.ProcessorCount} 核心");
        Console.WriteLine($"  开机时长:   {TimeSpan.FromMilliseconds(Environment.TickCount64):d'天 'hh'小时 'mm'分'}");

        // 2. 磁盘驱动器空间
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n【本地磁盘空间占用】");
        Console.ResetColor();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            double usedPercent = 100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize;
            string bar = RenderProgressBar(usedPercent);
            string usedStr = FormatBytes(drive.TotalSize - drive.AvailableFreeSpace);
            string totalStr = FormatBytes(drive.TotalSize);
            string freeStr = FormatBytes(drive.AvailableFreeSpace);

            Console.WriteLine($"  [{drive.Name.TrimEnd('\\')}] {bar} {usedPercent:0.0}%");
            Console.WriteLine($"      已用: {usedStr} / 总量: {totalStr} (剩余可用: {freeStr})");
        }

        // 3. 活动网络接口
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n【当前活动网络适配器】");
        Console.ResetColor();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                var ipProps = ni.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();

                var gateways = string.Join(", ", ipProps.GatewayAddresses.Select(g => g.Address.ToString()));

                if (!string.IsNullOrEmpty(ipv4))
                {
                    Console.WriteLine($"  网卡名称: {ni.Name} ({ni.NetworkInterfaceType})");
                    Console.WriteLine($"    IPv4 地址: {ipv4}");
                    if (!string.IsNullOrEmpty(gateways))
                    {
                        Console.WriteLine($"    默认网关: {gateways}");
                    }
                    Console.WriteLine($"    链路速度: {ni.Speed / 1_000_000} Mbps");
                }
            }
        }

        // 4. 外网连通与延迟测试
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n【公网连通性与 Ping 延迟测试】");
        Console.ResetColor();

        string[] targets = { "223.5.5.5", "119.29.29.29", "1.1.1.1" };
        using var ping = new Ping();

        foreach (var ip in targets)
        {
            try
            {
                var reply = await ping.SendPingAsync(ip, 1500);
                if (reply.Status == IPStatus.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  [✓] Ping {ip,-15} 正常，往返延迟: {reply.RoundtripTime} ms");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [✗] Ping {ip,-15} 失败: {reply.Status}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  [!] Ping {ip,-15} 异常: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n按任意键返回主菜单...");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }

    private static string RenderProgressBar(double percent, int width = 20)
    {
        int filled = (int)Math.Round(percent / 100.0 * width);
        filled = Math.Clamp(filled, 0, width);
        return "[" + new string('█', filled) + new string('░', width - filled) + "]";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
