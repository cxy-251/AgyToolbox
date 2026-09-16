using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgyToolbox.Services;

public record WifiRecord(string Ssid, string Password);
public record PortOccupant(int Port, int Pid, string ProcessName, string Protocol);
public record DnsPingResult(string Provider, string Ip, bool? Success, long LatencyMs, string LatencyDisplay, string Description);

public class WinTricksService
{
    /// <summary>
    /// 获取当前系统已连接并保存的 WLAN 无线配置文件及安全密钥 (netsh wlan show profile key=clear)
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
    /// 生成系统电池容量与历史充放电统计 HTML 诊断报告 (powercfg /batteryreport)
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
                return (true, "电池诊断报告已生成，已在浏览器中打开。\n文件路径: " + outPath, outPath);
            }
            else
            {
                return (false, $"生成未完成 (台式机等非电池供电设备无此数据)：{output}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"生成失败: {ex.Message}", null);
        }
    }

    /// <summary>
    /// 生成系统 Modern Standby 待机与睡眠耗电 HTML 诊断报告 (powercfg /sleepstudy)
    /// </summary>
    public (bool Success, string Message, string? Path) GenerateSleepStudyReport()
    {
        try
        {
            string outPath = Path.Combine(Path.GetTempPath(), "agy_sleepstudy_report.html");
            var output = RunProcessAndGetOutput("powercfg", $"/sleepstudy /output \"{outPath}\"");

            if (File.Exists(outPath))
            {
                Process.Start(new ProcessStartInfo(outPath) { UseShellExecute = true });
                return (true, "Modern Standby 待机能耗报告已生成，已在浏览器中打开。\n文件路径: " + outPath, outPath);
            }
            else
            {
                return (false, $"生成未完成：{output}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"生成失败: {ex.Message}", null);
        }
    }


    /// <summary>
    /// 获取当前系统阻止睡眠的请求源与上一次唤醒源 (powercfg -lastwake & powercfg /requests)
    /// </summary>
    public (bool Success, string Info) GetWakeAndRequestsInfo()
    {
        try
        {
            string lastWake = RunProcessAndGetOutput("powercfg", "-lastwake");
            string requests = RunProcessAndGetOutput("powercfg", "/requests");

            string result = "【最近一次系统唤醒源 (-lastwake)】\n" +
                            (string.IsNullOrWhiteSpace(lastWake) ? "无记录" : lastWake.Trim()) +
                            "\n\n────────────────────────────────────\n" +
                            "【当前阻止系统进入休眠的活动请求 (/requests)】\n" +
                            (string.IsNullOrWhiteSpace(requests) ? "无活动阻止请求" : requests.Trim());

            return (true, result);
        }
        catch (Exception ex)
        {
            return (false, $"查询失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 解锁并导入 Windows 原生“卓越性能”电源方案 (Ultimate Performance Scheme)
    /// </summary>
    public (bool Success, string Message) EnableUltimatePerformanceScheme()
    {
        try
        {
            var output = RunProcessAndGetOutput("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
            return (true, "已成功导入 Windows 原生【卓越性能】电源方案！\n可在系统“电源与睡眠”或控制面板“电源选项”中勾选启用。");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动 Windows 内存诊断工具 (mdsched.exe)
    /// </summary>
    public void OpenMemoryDiagnostic()
    {
        Process.Start("mdsched.exe");
    }

    /// <summary>
    /// 启动驱动程序验证程序管理器 (verifier.exe)
    /// </summary>
    public void OpenDriverVerifier()
    {
        Process.Start("verifier.exe");
    }

    /// <summary>
    /// 启动系统信息工具 (msinfo32.exe)
    /// </summary>
    public void OpenMsInfo32()
    {
        Process.Start("msinfo32.exe");
    }

    /// <summary>
    /// 启动 ClearType 文本调谐器 (cttune.exe)
    /// </summary>
    public void OpenClearTypeTuner()
    {
        Process.Start("cttune.exe");
    }

    /// <summary>
    /// 启动显示颜色校准工具 (dccw.exe)
    /// </summary>
    public void OpenColorCalibration()
    {
        Process.Start("dccw.exe");
    }

    /// <summary>
    /// 启动驱动器优化与碎片整理工具 (dfrgui.exe)
    /// </summary>
    public void OpenDiskDefrag()
    {
        Process.Start("dfrgui.exe");
    }

    /// <summary>
    /// 在管理员控制台中启动 chkdsk 联机只读只检预扫描 (chkdsk C: /scan)
    /// </summary>
    public (bool Success, string Message) RunChkdskScanInConsole(string drive = "C:")
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k echo [正在执行 NTFS 文件系统只读联机预检: chkdsk {drive} /scan] && chkdsk {drive} /scan",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, $"已在管理员控制台启动 chkdsk {drive} /scan 联机预检！\n该预检不锁定卷，系统可正常使用。");
        }
        catch (Exception ex)
        {
            return (false, $"启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动系统可靠性监视器 (perfmon.exe /rel)
    /// </summary>
    public void OpenReliabilityMonitor()
    {
        Process.Start("perfmon.exe", "/rel");
    }

    /// <summary>
    /// 启动性能监视器 (perfmon.msc)
    /// </summary>
    public void OpenPerformanceMonitor()
    {
        Process.Start("perfmon.msc");
    }

    /// <summary>
    /// 启动事件查看器 (eventvwr.msc)
    /// </summary>
    public void OpenEventViewer()
    {
        Process.Start("eventvwr.msc");
    }

    /// <summary>
    /// 启动 Windows 全局设置集合面板 (GodMode)
    /// </summary>
    public void OpenGodMode()
    {
        Process.Start("explorer.exe", "shell:::{ED7BA470-8E54-465E-825C-99712043E01C}");
    }

    /// <summary>
    /// 启动恶意软件删除工具 (mrt.exe)
    /// </summary>
    public void OpenMrt()
    {
        Process.Start("mrt.exe");
    }

    /// <summary>
    /// 启动 DirectX 诊断工具 (dxdiag.exe)
    /// </summary>
    public void OpenDxDiag()
    {
        Process.Start("dxdiag.exe");
    }

    /// <summary>
    /// 启动资源监视器 (resmon.exe)
    /// </summary>
    public void OpenResMon()
    {
        Process.Start("resmon.exe");
    }

    /// <summary>
    /// 启动本地安全策略编辑器 (secpol.msc)
    /// </summary>
    public void OpenLocalSecurityPolicy()
    {
        Process.Start("secpol.msc");
    }

    /// <summary>
    /// 启动本地用户和组管理器 (lusrmgr.msc)
    /// </summary>
    public void OpenLocalUsersAndGroups()
    {
        Process.Start("lusrmgr.msc");
    }

    /// <summary>
    /// 启动当前用户证书管理器 (certmgr.msc)
    /// </summary>
    public void OpenCertificateManager()
    {
        Process.Start("certmgr.msc");
    }

    /// <summary>
    /// 启动组件服务管理器 (dcomcnfg.exe)
    /// </summary>
    public void OpenComponentServices()
    {
        Process.Start("dcomcnfg.exe");
    }

    /// <summary>
    /// 启动远程协助客户端 (msra.exe)
    /// </summary>
    public void OpenRemoteAssistance()
    {
        Process.Start("msra.exe");
    }

    /// <summary>
    /// 启动系统属性窗口 (sysdm.cpl)
    /// </summary>
    public void OpenSystemProperties()
    {
        Process.Start("sysdm.cpl");
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
    /// 启动步骤记录器 (psr.exe - Problem Steps Recorder)
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

    /// <summary>
    /// 启动管理员权限 CMD 窗口并自动执行 SFC 系统完整性扫描 (sfc /scannow)
    /// </summary>
    public (bool Success, string Message) RunSfcScanInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在执行 Windows 系统核心文件完整性扫描修复] && sfc /scannow",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已呼出管理员终端执行 sfc /scannow 修复！请观察控制台进度。");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动管理员权限 CMD 执行 DISM 在线组件库深度修复 (DISM /Online /Cleanup-Image /RestoreHealth)
    /// </summary>
    public (bool Success, string Message) RunDismRepairInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在连接官方镜像源深度修复 Windows 系统组件库] && DISM /Online /Cleanup-Image /RestoreHealth",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已呼出管理员终端执行 DISM 深度组件库修复！");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动管理员终端执行组策略强制即时刷新 (gpupdate /force)
    /// </summary>
    public (bool Success, string Message) RunGpUpdateForceInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在强制即时刷新计算机与用户组策略引擎] && gpupdate /force",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已呼出管理员终端执行 gpupdate /force！策略将在数秒内强制刷新完毕。");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 网络协议栈终极急救复位 (netsh winsock reset + netsh int ip reset)
    /// </summary>
    public (bool Success, string Message) ResetWinsockAndIp()
    {
        try
        {
            RunProcessAndGetOutput("netsh", "winsock reset");
            RunProcessAndGetOutput("netsh", "int ip reset");
            return (true, "已成功重置 Winsock 套接字目录与 TCP/IP 协议栈！\n请重启电脑使新网络协议栈彻底生效。");
        }
        catch (Exception ex)
        {
            return (false, $"重置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 彻底抹除 WinSxS 冗余历史补丁与陈旧组件备份 (彻底释放 C 盘数 GB 到几十 GB)
    /// </summary>
    public (bool Success, string Message) RunDismCleanupBaseInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在执行 WinSxS 冗余历史更新组件库终极瘦身清理] && Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已在管理员控制台中启动 WinSxS 终极组件库瘦身！");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动磁盘管理控制台 (diskmgmt.msc)
    /// </summary>
    public void OpenDiskManagement()
    {
        Process.Start("diskmgmt.msc");
    }

    /// <summary>
    /// 在管理员控制台启动 DiskPart 交互式磁盘分区管理工具
    /// </summary>
    public void OpenDiskPartConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在启动 DiskPart 交互式磁盘分区引擎] && diskpart.exe",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动 DiskPart 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动高级安全 Windows 防火墙 (wf.msc)
    /// </summary>
    public void OpenAdvancedFirewall()
    {
        Process.Start("wf.msc");
    }

    /// <summary>
    /// 启动 Windows 性能记录器图形化界面 (wprui.exe)
    /// </summary>
    public (bool Success, string Message) OpenPerformanceRecorder()
    {
        try
        {
            Process.Start("wprui.exe");
            return (true, "已成功启动 Windows Performance Recorder (WPR)！");
        }
        catch (Exception ex)
        {
            return (false, $"启动 WPR 失败: {ex.Message}。\n提示：部分精简版系统可能未安装 WPR 模块，可通过命令行 wpr.exe 录制。");
        }
    }

    /// <summary>
    /// 启动系统配置实用程序 (msconfig.exe)
    /// </summary>
    public void OpenSystemConfiguration()
    {
        Process.Start("msconfig.exe");
    }

    /// <summary>
    /// 在管理员控制台中检查 Windows RE 恢复环境状态 (reagentc /info)
    /// </summary>
    public (bool Success, string Message) RunReagentcInfoInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在检查 Windows RE 恢复环境状态: reagentc /info] && reagentc /info",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已在管理员控制台启动 Windows RE (reagentc /info) 状态查询！");
        }
        catch (Exception ex)
        {
            return (false, $"启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开物理蓝屏转储存储路径 (C:\\Windows\\Minidump)
    /// </summary>
    public (bool Success, string Message) OpenMinidumpFolder()
    {
        try
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string minidumpDir = Path.Combine(winDir, "Minidump");
            if (!Directory.Exists(minidumpDir))
            {
                Directory.CreateDirectory(minidumpDir);
            }
            Process.Start("explorer.exe", minidumpDir);
            return (true, $"已打开转储目录: {minidumpDir}");
        }
        catch (Exception ex)
        {
            return (false, $"打开 Minidump 目录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 在控制台中执行现代 CIM/PowerShell 物理硬盘与健康状态查询 (替代废弃的 wmic)
    /// </summary>
    public void RunGetPhysicalDiskInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoExit -Command \"Write-Host '== 现代 PowerShell/CIM 物理存储健康状态与介质类型诊断 (已弃用旧版 wmic) ==' -ForegroundColor Cyan; Get-PhysicalDisk | Select-Object DeviceId, FriendlyName, MediaType, BusType, OperationalStatus, HealthStatus | Format-Table -AutoSize; Write-Host '`n提示: HealthStatus 为 Healthy 表示 S.M.A.R.T. 与驱动器运行正常。' -ForegroundColor Green\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动 CIM 磁盘诊断失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 在控制台中执行 Test-NetConnection (TNC 原生端口连通性与路由跃点探测，取代 Telnet)
    /// </summary>
    public void RunTestNetConnectionInConsole(string host = "api.github.com", int port = 443)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"Write-Host '== 原生网络端口连通性探测 (Test-NetConnection 代替第三方 Telnet) ==' -ForegroundColor Cyan; Test-NetConnection -ComputerName '{host}' -Port {port} -InformationLevel Detailed\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动 TNC 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 触发彻底冷重启 (shutdown /r /t 0，绕过快速启动内核混合休眠)
    /// </summary>
    public void TriggerColdRestart()
    {
        Process.Start("shutdown.exe", "/r /t 0");
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
