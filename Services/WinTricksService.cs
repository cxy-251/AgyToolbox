using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgyToolbox.Services;

public record WifiRecord(string Ssid, string Password);
public record PortOccupant(int Port, int Pid, string ProcessName, string Protocol);

public class WinTricksService
{
    /// <summary>
    /// 获取本机曾连接并保存的所有 WiFi 及明文密码 (揭秘短视频“黑客破解WiFi”)
    /// </summary>
    public List<WifiRecord> GetSavedWifiPasswords()
    {
        var list = new List<WifiRecord>();
        try
        {
            var profilesOutput = RunProcessAndGetOutput("netsh", "wlan show profiles");
            var profileNames = new List<string>();

            // 匹配中英文 "所有用户配置文件 : 名称" 或 "All User Profile : name"
            var regex = new Regex(@"(?:所有用户配置文件|All User Profile)\s*:\s*(.*)", RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(profilesOutput))
            {
                var name = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    profileNames.Add(name);
                }
            }

            foreach (var name in profileNames)
            {
                var detailOutput = RunProcessAndGetOutput("netsh", $"wlan show profile name=\"{name}\" key=clear");
                // 查找 "关键内容" 或 "Key Content"
                var keyMatch = Regex.Match(detailOutput, @"(?:关键内容|Key Content)\s*:\s*(.*)", RegexOptions.IgnoreCase);
                string pwd = keyMatch.Success ? keyMatch.Groups[1].Value.Trim() : "<无密码 / 开放网络 / 802.1x认证>";
                list.Add(new WifiRecord(name, pwd));
            }
        }
        catch (Exception ex)
        {
            list.Add(new WifiRecord("读取异常", ex.Message));
        }

        return list;
    }

    /// <summary>
    /// 生成官方底层电池寿命与损耗健康报告 (揭秘短视频“一键查二手笔记本损耗”)
    /// </summary>
    public (bool Success, string Message, string? Path) GenerateBatteryReport()
    {
        try
        {
            string outPath = Path.Combine(Path.GetTempPath(), "agy_battery_report.html");
            var output = RunProcessAndGetOutput("powercfg", $"/batteryreport /output \"{outPath}\"");

            if (File.Exists(outPath))
            {
                Process.Start(new ProcessStartInfo(outPath) { UseShellExecute = true });
                return (true, "电池诊断报告已生成，已在浏览器中自动弹出！", outPath);
            }
            else
            {
                return (false, $"生成未完成 (如果是台式机则无内置电池)：{output}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"生成失败: {ex.Message}", null);
        }
    }

    /// <summary>
    /// 呼出系统可靠性监视器 (揭秘短视频“电脑闪退卡顿查凶手神器”)
    /// </summary>
    public void OpenReliabilityMonitor()
    {
        Process.Start("perfmon.exe", "/rel");
    }

    /// <summary>
    /// 呼出全能上帝模式控制面板 (揭秘短视频“解锁Win隐藏200项特权GodMode”)
    /// </summary>
    public void OpenGodMode()
    {
        Process.Start("explorer.exe", "shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
    }

    /// <summary>
    /// 呼出微软自带恶意软件深度查杀工具 (Win+R mrt)
    /// </summary>
    public void OpenMrt()
    {
        Process.Start("mrt.exe");
    }

    /// <summary>
    /// 呼出 DirectX 硬件与显卡声卡体检工具 (Win+R dxdiag)
    /// </summary>
    public void OpenDxDiag()
    {
        Process.Start("dxdiag.exe");
    }

    /// <summary>
    /// 呼出 Windows 高级资源监视器 (Win+R resmon)
    /// </summary>
    public void OpenResMon()
    {
        Process.Start("resmon.exe");
    }

    /// <summary>
    /// 一键刷新 DNS 缓存 (解决“微信能发但网页打不开”/节点解析失效)
    /// </summary>
    public string FlushDns()
    {
        try
        {
            var output = RunProcessAndGetOutput("ipconfig", "/flushdns");
            return string.IsNullOrWhiteSpace(output) ? "DNS 解析缓存已成功刷新！" : output.Trim();
        }
        catch (Exception ex)
        {
            return $"刷新失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 管理员权限一键记事本编辑 Hosts 文件 (解决新手无法保存问题)
    /// </summary>
    public void OpenHostsFile()
    {
        string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        var psi = new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = $"\"{hostsPath}\"",
            UseShellExecute = true,
            Verb = "runas" // 提升为管理员权限打开
        };
        try
        {
            Process.Start(psi);
        }
        catch { }
    }

    /// <summary>
    /// 呼出问题步骤记录器 (Win+R psr，自动点击截图配字)
    /// </summary>
    public void OpenStepsRecorder()
    {
        try
        {
            Process.Start("psr.exe");
        }
        catch { }
    }

    /// <summary>
    /// 查找指定端口当前被哪个进程/软件霸占
    /// </summary>
    public List<PortOccupant> FindPortOccupants(int port)
    {
        var occupants = new List<PortOccupant>();
        try
        {
            var output = RunProcessAndGetOutput("netstat", "-ano -p tcp");
            var lines = output.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("TCP", StringComparison.OrdinalIgnoreCase)) continue;

                var parts = Regex.Split(trimmed, @"\s+");
                if (parts.Length >= 5)
                {
                    var localAddress = parts[1];
                    var pidStr = parts[4];

                    if (localAddress.EndsWith($":{port}") && int.TryParse(pidStr, out int pid))
                    {
                        string processName = "未知进程";
                        try
                        {
                            var proc = Process.GetProcessById(pid);
                            processName = proc.ProcessName;
                        }
                        catch { }

                        occupants.Add(new PortOccupant(port, pid, processName, "TCP"));
                    }
                }
            }
        }
        catch { }

        return occupants;
    }

    /// <summary>
    /// 杀死霸占端口的进程
    /// </summary>
    public bool KillProcessByPid(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            p.Kill(entireProcessTree: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string RunProcessAndGetOutput(string fileName, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.Default
        };

        using var process = Process.Start(psi);
        if (process == null) return "";
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return stdout;
    }
}
