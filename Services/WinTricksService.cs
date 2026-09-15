using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgyToolbox.Services;

public record WifiRecord(string Ssid, string Password);
public record PortOccupant(int Port, int Pid, string ProcessName, string Protocol);
public record DnsPingResult(string Provider, string Ip, bool? Success, long LatencyMs, string LatencyDisplay, string Description);

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

    /// <summary>
    /// <summary>
    /// 获取预置的主流国内外公共 DNS 列表（未测速就绪态）
    /// </summary>
    public List<DnsPingResult> GetDefaultDnsList()
    {
        return new List<DnsPingResult>
        {
            new("阿里公共 DNS", "223.5.5.5", null, -1, "待测速 (点击右上角测速)", "国内 Anycast 节点多，解析国内 CDN/网站最快，防运营商篡改"),
            new("腾讯 DNSPod", "119.29.29.29", null, -1, "待测速 (点击右上角测速)", "腾讯云基础设施支持，国内智能解析调度稳定"),
            new("114 DNS", "114.114.114.114", null, -1, "待测速 (点击右上角测速)", "老牌经典公共 DNS，国内三大运营商互联互通良好"),
            new("百度公共 DNS", "180.76.76.76", null, -1, "待测速 (点击右上角测速)", "百度基础设施支撑，纯净无劫持"),
            new("Cloudflare DNS", "1.1.1.1", null, -1, "待测速 (点击右上角测速)", "全球解析最快隐私 DNS，不存日志，但在国内部分地区延迟较高"),
            new("Google DNS", "8.8.8.8", null, -1, "待测速 (点击右上角测速)", "全球知名度最高公共 DNS，国际访问基准解析器")
        };
    }

    /// <summary>
    /// 测试主流国内外公共 DNS 服务器往返延迟
    /// </summary>
    public async Task<List<DnsPingResult>> TestPublicDnsAsync()
    {
        var targets = new (string Provider, string Ip, string Desc)[]
        {
            ("阿里公共 DNS", "223.5.5.5", "国内 Anycast 节点多，解析国内 CDN/网站最快，防运营商篡改"),
            ("腾讯 DNSPod", "119.29.29.29", "腾讯云基础设施支持，国内智能解析调度稳定"),
            ("114 DNS", "114.114.114.114", "老牌经典公共 DNS，国内三大运营商互联互通良好"),
            ("百度公共 DNS", "180.76.76.76", "百度基础设施支撑，纯净无劫持"),
            ("Cloudflare DNS", "1.1.1.1", "全球解析最快隐私 DNS，不存日志，但在国内部分地区延迟较高"),
            ("Google DNS", "8.8.8.8", "全球知名度最高公共 DNS，国际访问基准解析器")
        };

        var results = new List<DnsPingResult>();
        using var ping = new System.Net.NetworkInformation.Ping();

        foreach (var t in targets)
        {
            try
            {
                var reply = await ping.SendPingAsync(t.Ip, 1500);
                bool ok = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                results.Add(new DnsPingResult(
                    t.Provider,
                    t.Ip,
                    ok,
                    ok ? reply.RoundtripTime : 9999,
                    ok ? $"{reply.RoundtripTime} ms" : "超时/丢包",
                    t.Desc
                ));
            }
            catch (Exception ex)
            {
                results.Add(new DnsPingResult(t.Provider, t.Ip, false, 9999, "不可达", $"{t.Desc} ({ex.Message})"));
            }
        }

        return results;
    }

    /// <summary>
    /// 获取现代化标准的 Anaconda / Conda .condarc 配置文件内容 (全 HTTPS、清华源、移除已废弃的 free 通道)
    /// </summary>
    public string GetModernCondarcContent()
    {
        return """
        # 现代化清华 TUNA 镜像源标准配置 (支持 HTTPS，移除已废弃的 free 通道)
        channels:
          - https://mirrors.tuna.tsinghua.edu.cn/anaconda/pkgs/main
          - https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud/conda-forge
          - https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud/msys2/
        show_channel_urls: true
        default_channels:
          - https://mirrors.tuna.tsinghua.edu.cn/anaconda/pkgs/main
          - https://mirrors.tuna.tsinghua.edu.cn/anaconda/pkgs/r
        custom_channels:
          conda-forge: https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud
          msys2: https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud
          pytorch: https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud
        ssl_verify: true
        auto_activate_base: false
        """;
    }

    /// <summary>
    /// 一键将现代标准的 .condarc 写入用户主目录
    /// </summary>
    public string ApplyModernCondarc()
    {
        try
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string condarcPath = Path.Combine(userHome, ".condarc");
            File.WriteAllText(condarcPath, GetModernCondarcContent().Trim(), System.Text.Encoding.UTF8);
            return $"已成功写入标准配置文件：{condarcPath}";
        }
        catch (Exception ex)
        {
            return $"写入失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 异步运行指定的网络诊断命令 (tracert, pathping, route print, arp -a 等) 并将输出行实时推送到回调
    /// </summary>
    public async Task RunNetworkDiagnosticAsync(string command, string arguments, Action<string> onLineReceived, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.Default,
                    StandardErrorEncoding = System.Text.Encoding.Default
                };

                using var proc = new Process { StartInfo = psi };
                proc.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) onLineReceived(e.Data);
                };
                proc.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null) onLineReceived($"[ERR] {e.Data}");
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                using (ct.Register(() =>
                {
                    try { if (!proc.HasExited) proc.Kill(true); } catch { }
                }))
                {
                    proc.WaitForExit();
                }

                onLineReceived($"\n[诊断完成] 退出代码: {proc.ExitCode}");
            }
            catch (OperationCanceledException)
            {
                onLineReceived("\n[用户手动终止诊断]");
            }
            catch (Exception ex)
            {
                onLineReceived($"\n[诊断异常]: {ex.Message}");
            }
        }, ct);
    }

    /// <summary>
    /// 直接以原生外壳启动系统运行命令 (如 devmgmt.msc, ncpa.cpl 等)
    /// </summary>
    public (bool Success, string Message) RunSystemCommand(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = true
            };
            Process.Start(psi);
            return (true, $"已成功启动 [{command}]！");
        }
        catch (Exception ex)
        {
            return (false, $"启动失败: {ex.Message}");
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
