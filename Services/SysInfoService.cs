using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using AgyToolbox.Helpers;

namespace AgyToolbox.Services;

public record BasicSystemInfo(string MachineName, string OSDescription, string Architecture, int CoreCount, TimeSpan Uptime);
public record DiskInfoItem(string Name, string Label, long TotalBytes, long FreeBytes, double UsedPercent, string TotalFormatted, string FreeFormatted, string UsedFormatted);
public record NetInfoItem(string Name, string Type, string? IPv4, string? Gateway, long SpeedMbps);
public record PingResultItem(string Target, bool Success, long RoundtripMs, string Status);

public class SysInfoService
{
    public BasicSystemInfo GetBasicInfo()
    {
        return new BasicSystemInfo(
            Environment.MachineName,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            TimeSpan.FromMilliseconds(Environment.TickCount64)
        );
    }

    public List<DiskInfoItem> GetDisks()
    {
        var list = new List<DiskInfoItem>();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            double usedPercent = 100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize;
            long usedBytes = drive.TotalSize - drive.AvailableFreeSpace;

            list.Add(new DiskInfoItem(
                drive.Name,
                string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "本地磁盘" : drive.VolumeLabel,
                drive.TotalSize,
                drive.AvailableFreeSpace,
                usedPercent,
                FormatHelper.FormatBytes(drive.TotalSize),
                FormatHelper.FormatBytes(drive.AvailableFreeSpace),
                FormatHelper.FormatBytes(usedBytes)
            ));
        }
        return list;
    }

    public List<NetInfoItem> GetActiveNetworks()
    {
        var list = new List<NetInfoItem>();
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
                    list.Add(new NetInfoItem(
                        ni.Name,
                        ni.NetworkInterfaceType.ToString(),
                        ipv4,
                        string.IsNullOrEmpty(gateways) ? "无" : gateways,
                        ni.Speed / 1_000_000
                    ));
                }
            }
        }
        return list;
    }

    public async Task<List<PingResultItem>> TestPingAsync(string[]? targets = null)
    {
        targets ??= new[] { "223.5.5.5", "119.29.29.29", "1.1.1.1" };
        var results = new List<PingResultItem>();
        using var ping = new Ping();

        foreach (var ip in targets)
        {
            try
            {
                var reply = await ping.SendPingAsync(ip, 1500);
                results.Add(new PingResultItem(
                    ip,
                    reply.Status == IPStatus.Success,
                    reply.RoundtripTime,
                    reply.Status.ToString()
                ));
            }
            catch (Exception ex)
            {
                results.Add(new PingResultItem(ip, false, 0, ex.Message));
            }
        }
        return results;
    }
}
