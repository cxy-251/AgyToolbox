using AgyToolbox.Core;
using AgyToolbox.Services;

namespace AgyToolbox.Tools;

public class WinTricksTool : ITool
{
    public string Key => "tricks";
    public string Name => "Windows 实用系统运维技巧 (WinTricks)";
    public string Description => "基于 Windows 内置工具的实用诊断与排查技巧：WiFi 密码查看、电池健康报告、故障监视与端口占用分析。";

    private readonly WinTricksService _service = new();

    public Task RunAsync(string[] args)
    {
        while (true)
        {
            try { if (!Console.IsOutputRedirected) Console.Clear(); } catch { }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================");
            Console.WriteLine("          🛠️  Windows 实用系统运维技巧 (WinTricks)             ");
            Console.WriteLine("===============================================================");
            Console.ResetColor();
            Console.WriteLine(" 基于 Windows 原生命令行与诊断工具的快捷功能集合：\n");

            Console.WriteLine("  [1] 📶 查看本机已连接过的 WiFi 配置文件与密码");
            Console.WriteLine("  [2] 🔋 生成笔记本电池健康损耗官方诊断报告 (HTML)");
            Console.WriteLine("  [3] 📉 打开系统可靠性历史监视器 (排查程序异常退出与系统故障)");
            Console.WriteLine("  [4] 🌐 端口占用检测与进程释放 (排查本地网络端口冲突)");
            Console.WriteLine("  [5] 🛡️ 启动微软官方恶意软件删除工具 (mrt.exe)");
            Console.WriteLine("  [6] 🧙 打开 Windows 全能上帝模式控制面板 (GodMode)");
            Console.WriteLine("  [7] 🕹️ 启动 DirectX 硬件体检工具 (dxdiag.exe)");
            Console.WriteLine("\n  [0] 返回主菜单");
            Console.WriteLine("===============================================================");
            Console.Write("请选择功能 [0-7]: ");

            var input = Console.ReadLine();
            if (input == null || input == "0") break;

            switch (input.Trim())
            {
                case "1":
                    ShowWifiPasswords();
                    break;
                case "2":
                    GenerateBatteryReport();
                    break;
                case "3":
                    Console.WriteLine("\n正在启动 Windows 可靠性监视器...");
                    _service.OpenReliabilityMonitor();
                    Pause();
                    break;
                case "4":
                    CheckAndKillPort();
                    break;
                case "5":
                    Console.WriteLine("\n正在启动微软恶意软件删除工具 (mrt)...");
                    _service.OpenMrt();
                    Pause();
                    break;
                case "6":
                    Console.WriteLine("\n正在打开上帝模式 (GodMode)...");
                    _service.OpenGodMode();
                    Pause();
                    break;
                case "7":
                    Console.WriteLine("\n正在启动 DirectX 诊断工具 (dxdiag)...");
                    _service.OpenDxDiag();
                    Pause();
                    break;
                default:
                    Console.WriteLine("无效输入，请重试。");
                    Thread.Sleep(600);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private void ShowWifiPasswords()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n【已连接过的 WiFi 名称与密码提取结果】");
        Console.ResetColor();

        var list = _service.GetSavedWifiPasswords();
        if (list.Count == 0)
        {
            Console.WriteLine("未检索到任何已保存的 WiFi 配置文件。");
        }
        else
        {
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine(string.Format("{0,-30} | {1}", "WiFi 名称 (SSID)", "连接密码 (明文)"));
            Console.WriteLine("------------------------------------------------------------------");
            foreach (var item in list)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0,-30}", item.Ssid));
                Console.ResetColor();
                Console.Write(" | ");
                Console.ForegroundColor = item.Password.StartsWith("<") ? ConsoleColor.DarkGray : ConsoleColor.Cyan;
                Console.WriteLine(item.Password);
                Console.ResetColor();
            }
            Console.WriteLine("------------------------------------------------------------------");
        }
        Pause();
    }

    private void GenerateBatteryReport()
    {
        Console.WriteLine("\n正在请求 Windows 内核电源驱动生成诊断报告...");
        var (success, msg, path) = _service.GenerateBatteryReport();
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[✓] {msg}");
            Console.WriteLine($"    报告路径: {path}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[提示] {msg}");
            Console.ResetColor();
        }
        Pause();
    }

    private void CheckAndKillPort()
    {
        Console.Write("\n请输入要查询的端口号 (例如 8080 / 5000 / 3000): ");
        var portInput = Console.ReadLine();
        if (int.TryParse(portInput, out int port))
        {
            var occupants = _service.FindPortOccupants(port);
            if (occupants.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[✓] 端口 {port} 当前空闲，没有被任何进程占用。");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] 端口 {port} 正在被以下进程占用：");
                Console.ResetColor();
                foreach (var occ in occupants)
                {
                    Console.WriteLine($"    进程名: {occ.ProcessName} | PID: {occ.Pid} | 协议: {occ.Protocol}");
                }

                Console.Write("\n是否杀死该进程以释放端口？(y/n): ");
                var confirm = Console.ReadLine();
                if (confirm?.Trim().ToLowerInvariant() == "y")
                {
                    foreach (var occ in occupants)
                    {
                        if (_service.KillProcessByPid(occ.Pid))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[✓] 已强行终止进程 {occ.ProcessName} (PID: {occ.Pid})，端口已释放！");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine($"[✗] 终止进程失败，可能需要管理员权限。");
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("端口号无效。");
        }
        Pause();
    }

    private static void Pause()
    {
        Console.WriteLine("\n按任意键继续...");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
