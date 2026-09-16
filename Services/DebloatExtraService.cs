using System.Diagnostics;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public record StartupItemInfo(string Name, string Command, string Scope);
public record OemAppInfo(string Name, string Vendor, string Description, string DebloatAdvice, bool IsDetected);
public record FodFeatureInfo(string Key, string DisplayName, string CapabilityName, string Description, string DebloatAdvice, bool IsInstalled);
public record VirtFeatureInfo(string Key, string DisplayName, string FeatureName, string Description, string DebloatAdvice, bool IsEnabled);

public class DebloatExtraService
{
    private static readonly (string Name, string Vendor, string ProcessName, string Description, string DebloatAdvice)[] KnownOemApps =
    [
        (
            "麦咖啡 (McAfee Total Protection / WebAdvisor)",
            "McAfee",
            "mfevtps",
            "部分品牌新机常预装该软件试用版，包含多个后台安全扫描与网页安全过滤服务，会持续占用一定的 CPU 与内存资源。",
            "试用到期后会定期弹出续订提示。若习惯使用 Windows 自带的 Defender，可选择卸载；如遇常规卸载不完全，建议使用官方 MCPR 工具清理。"
        ),
        (
            "华硕奥创中心 (Armoury Crate)",
            "ASUS",
            "ArmouryCrate",
            "华硕笔记本预装的灯效控制与硬件性能调度套件，集成了较多的底层驱动与后台系统服务。",
            "若仅需调节基础性能模式与键盘背光，社区常使用轻量开源工具（如 G-Helper）作为低开销的替代方案。"
        ),
        (
            "联想电脑管家 (Lenovo Vantage / PC Manager)",
            "Lenovo",
            "LenovoVantage",
            "联想设备预装管理软件，提供硬件设置、保修查询与驱动更新，部分版本集成了资讯挂件或服务推送。",
            "若仅需要电池养护（阈值充电）或性能模式切换，亦可考虑使用开源轻量工具（如 LenovoLegionToolkit）。"
        ),
        (
            "戴尔支持助手 (Dell SupportAssist)",
            "Dell",
            "SupportAssist",
            "戴尔设备官方硬件诊断与驱动更新套件，会在后台执行周期性的硬件扫描与日志诊断任务。",
            "日常硬件状态可直接查看系统信息；若不需要其自动诊断与支持上报功能，可按需卸载。"
        ),
        (
            "惠普助手 (HP Support Assistant)",
            "HP",
            "HPSupportAssistant",
            "惠普设备预装管理助手，常驻系统托盘以收集设备运行状态并提示驱动更新。",
            "若更倾向于手动更新驱动或通过 Windows Update 管理，可选择移除以减少系统托盘常驻项。"
        )
    ];

    /// <summary>
    /// 扫描当前系统是否运行或安装了常见 OEM 品牌预装管理软件
    /// </summary>
    public List<OemAppInfo> DetectOemApps()
    {
        var result = new List<OemAppInfo>();
        var processes = Process.GetProcesses();

        foreach (var app in KnownOemApps)
        {
            bool detected = processes.Any(p => p.ProcessName.Contains(app.ProcessName, StringComparison.OrdinalIgnoreCase));
            result.Add(new OemAppInfo(app.Name, app.Vendor, app.Description, app.DebloatAdvice, detected));
        }

        return result;
    }

    /// <summary>
    /// 读取系统所有当前生效的开机自启动程序 (注册表 Run 项)
    /// </summary>
    public List<StartupItemInfo> GetStartupItems()
    {
        var list = new List<StartupItemInfo>();

        void ReadKey(RegistryKey? root, string subKey, string scope)
        {
            try
            {
                using var key = root?.OpenSubKey(subKey);
                if (key == null) return;
                foreach (var name in key.GetValueNames())
                {
                    var cmd = key.GetValue(name)?.ToString() ?? "";
                    list.Add(new StartupItemInfo(name, cmd, scope));
                }
            }
            catch { }
        }

        ReadKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "当前用户 (HKCU)");
        ReadKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "系统全局 (HKLM)");

        return list;
    }

    /// <summary>
    /// 删除指定注册表自启动项
    /// </summary>
    public bool RemoveStartupItem(string name, string scope)
    {
        try
        {
            var isUser = scope.Contains("HKCU");
            using var key = isUser
                ? Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
                : Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (key != null && key.GetValue(name) != null)
            {
                key.DeleteValue(name);
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    #region 商业推广与 UI 广告阻断模块 (Content Delivery Manager / Start Menu / LockScreen / Widgets)

    /// <summary>
    /// 检测是否已关闭 Content Delivery Manager 静默安装推广应用 (Candy Crush 等)
    /// </summary>
    public bool IsSilentAppInstallDisabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            var val = key?.GetValue("SilentInstalledAppsEnabled");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开关 Content Delivery Manager 静默推广应用拦截
    /// </summary>
    public (bool Success, string Message) SetSilentAppInstallDisabled(bool disable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            int regVal = disable ? 0 : 1;
            key.SetValue("SilentInstalledAppsEnabled", regVal, RegistryValueKind.DWord);
            key.SetValue("SubscribedContent-338388Enabled", regVal, RegistryValueKind.DWord);
            key.SetValue("SubscribedContent-338389Enabled", regVal, RegistryValueKind.DWord);
            key.SetValue("SystemPaneSuggestionsEnabled", regVal, RegistryValueKind.DWord);

            return (true, disable
                ? "已成功开启【静默推广拦截】！防止后台自动下载 Candy Crush 等推广软件。"
                : "已恢复系统默认内容交付设置。");
        }
        catch (Exception ex)
        {
            return (false, $"设置静默推广策略异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测开始菜单推荐与应用建议是否已关闭
    /// </summary>
    public bool IsStartMenuAdsDisabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            var iris = key?.GetValue("Start_IrisRecommendations");
            return iris is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开关开始菜单应用推荐与建议
    /// </summary>
    public (bool Success, string Message) SetStartMenuAdsDisabled(bool disable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            key.SetValue("Start_IrisRecommendations", disable ? 0 : 1, RegistryValueKind.DWord);
            return (true, disable
                ? "已关闭开始菜单应用推荐与基于提示的建议！"
                : "已恢复开始菜单推荐显示。");
        }
        catch (Exception ex)
        {
            return (false, $"设置开始菜单建议异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测锁屏聚焦广告与小贴士是否已关闭
    /// </summary>
    public bool IsLockScreenAdsDisabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            var val = key?.GetValue("RotatingLockScreenOverlayEnabled");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开关锁屏界面趣味技巧与广告小贴士
    /// </summary>
    public (bool Success, string Message) SetLockScreenAdsDisabled(bool disable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            int val = disable ? 0 : 1;
            key.SetValue("RotatingLockScreenOverlayEnabled", val, RegistryValueKind.DWord);
            key.SetValue("SubscribedContent-338387Enabled", val, RegistryValueKind.DWord);
            return (true, disable
                ? "已关闭锁屏界面聚焦广告与小贴士！"
                : "已恢复锁屏界面提示显示。");
        }
        catch (Exception ex)
        {
            return (false, $"设置锁屏提示异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测任务栏小组件 (Widgets) 资讯流是否已关闭
    /// </summary>
    public bool IsWidgetsNewsDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Dsh");
            var val = key?.GetValue("AllowNewsAndInterests");
            if (val is int intVal && intVal == 0) return true;

            using var userKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            var taskbarDa = userKey?.GetValue("TaskbarDa");
            return taskbarDa is int daVal && daVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开关任务栏小组件资讯流
    /// </summary>
    public (bool Success, string Message) SetWidgetsNewsDisabled(bool disable)
    {
        try
        {
            // 组策略层
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Dsh");
            key.SetValue("AllowNewsAndInterests", disable ? 0 : 1, RegistryValueKind.DWord);

            // 当前用户任务栏图标隐藏
            using var userKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            userKey.SetValue("TaskbarDa", disable ? 0 : 1, RegistryValueKind.DWord);

            return (true, disable
                ? "已禁用任务栏小组件 (Widgets) 资讯流与浮窗！"
                : "已恢复小组件资讯流。");
        }
        catch (Exception ex)
        {
            return (false, $"设置小组件策略异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 Windows 搜索面板 Bing 联网搜索建议与热搜是否已关闭
    /// </summary>
    public bool IsBingSearchDisabled()
    {
        try
        {
            using var searchKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            var val = searchKey?.GetValue("BingSearchEnabled");
            if (val is int intVal && intVal == 0) return true;

            using var policyKey = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer");
            var polVal = policyKey?.GetValue("DisableSearchBoxSuggestions");
            return polVal is int polInt && polInt == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开关 Windows 搜索面板 Bing 联网搜索建议与热搜
    /// </summary>
    public (bool Success, string Message) SetBingSearchDisabled(bool disable)
    {
        try
        {
            using var searchKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            searchKey.SetValue("BingSearchEnabled", disable ? 0 : 1, RegistryValueKind.DWord);

            using var policyKey = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer");
            policyKey.SetValue("DisableSearchBoxSuggestions", disable ? 1 : 0, RegistryValueKind.DWord);

            return (true, disable
                ? "已成功关闭 Windows 搜索面板必应联网热搜与广告推荐！"
                : "已恢复必应联网搜索建议。");
        }
        catch (Exception ex)
        {
            return (false, $"设置必应搜索策略异常: {ex.Message}");
        }
    }

    #endregion

    #region 遥测、隐私与后台无用服务精简 (DiagTrack / CEIP)

    /// <summary>
    /// 检测 DiagTrack 诊断跟踪服务是否已被禁用
    /// </summary>
    public bool IsDiagTrackDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\DiagTrack");
            var startVal = key?.GetValue("Start");
            return startVal is int intVal && intVal == 4; // 4 为 Disabled
        }
        catch { return false; }
    }

    /// <summary>
    /// 阻断或恢复 DiagTrack 诊断跟踪服务
    /// </summary>
    public (bool Success, string Message) SetDiagTrackDisabled(bool disable)
    {
        try
        {
            var script = disable
                ? "Stop-Service -Name 'DiagTrack' -Force -ErrorAction SilentlyContinue; Set-Service -Name 'DiagTrack' -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name 'dmwappushservice' -Force -ErrorAction SilentlyContinue; Set-Service -Name 'dmwappushservice' -StartupType Disabled -ErrorAction SilentlyContinue"
                : "Set-Service -Name 'DiagTrack' -StartupType Automatic -ErrorAction SilentlyContinue; Start-Service -Name 'DiagTrack' -ErrorAction SilentlyContinue; Set-Service -Name 'dmwappushservice' -StartupType Manual -ErrorAction SilentlyContinue";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, disable
                ? "已成功停止并禁用 DiagTrack 诊断跟踪服务，切断后台唤醒上传通道！"
                : "已恢复 DiagTrack 服务默认自动启动。");
        }
        catch (Exception ex)
        {
            return (false, $"设置诊断跟踪服务异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 CEIP 客户体验改进计划与反馈计划任务是否已禁用
    /// </summary>
    public bool IsCeipTasksDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\SQMClient\Windows");
            var val = key?.GetValue("CEIPEnable");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 禁用或恢复 CEIP 周期性计划任务与客户体验上报
    /// </summary>
    public (bool Success, string Message) SetCeipTasksDisabled(bool disable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\SQMClient\Windows");
            key.SetValue("CEIPEnable", disable ? 0 : 1, RegistryValueKind.DWord);

            var script = disable
                ? @"
Disable-ScheduledTask -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'Consolidator' -ErrorAction SilentlyContinue
Disable-ScheduledTask -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'UsbCeip' -ErrorAction SilentlyContinue
Disable-ScheduledTask -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'KernelCeipTask' -ErrorAction SilentlyContinue
Disable-ScheduledTask -TaskPath '\Microsoft\Windows\Feedback\Siuf\' -TaskName 'DmClient' -ErrorAction SilentlyContinue
"
                : @"
Enable-ScheduledTask -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'Consolidator' -ErrorAction SilentlyContinue
Enable-ScheduledTask -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'UsbCeip' -ErrorAction SilentlyContinue
Enable-ScheduledTask -TaskPath '\Microsoft\Windows\Customer Experience Improvement Program\' -TaskName 'KernelCeipTask' -ErrorAction SilentlyContinue
Enable-ScheduledTask -TaskPath '\Microsoft\Windows\Feedback\Siuf\' -TaskName 'DmClient' -ErrorAction SilentlyContinue
";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(6000);

            return (true, disable
                ? "已成功禁用 CEIP 及 Feedback Hub 周期性计划任务！"
                : "已恢复 CEIP 计划任务默认状态。");
        }
        catch (Exception ex)
        {
            return (false, $"设置计划任务异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 一键批量阻断或恢复全部商业推广与系统广告 (静默应用/开始菜单建议/锁屏小贴士/小组件资讯)
    /// </summary>
    public (bool Success, string Message) SetAllAdsDisabled(bool disable)
    {
        SetSilentAppInstallDisabled(disable);
        SetStartMenuAdsDisabled(disable);
        SetLockScreenAdsDisabled(disable);
        SetWidgetsNewsDisabled(disable);

        return (true, disable
            ? "已一键关闭所有后台推广静默安装、开始菜单广告、锁屏小贴士及小组件资讯流！"
            : "已一键恢复系统默认商业推广与应用推荐设置。");
    }

    #endregion

    #region 系统历史可选功能 (Features On Demand / FOD) 精简

    private static readonly (string Key, string DisplayName, string CapabilityName, string Description, string DebloatAdvice)[] KnownFodFeatures =
    [
        (
            "WMP",
            "Windows Media Player (旧版经典播放器)",
            "Media.WindowsMediaPlayer~~~~0.0.1.0",
            "早年 Windows 遗留的旧版经典多媒体播放组件，长期未维护且缺少现代硬件解码支持。",
            "现代系统已有内置媒体播放器或更轻量开源工具 (如 VLC / PotPlayer)，可安全卸载以减少空间占用。"
        ),
        (
            "IE_Mode",
            "Internet Explorer 模式 (IE 兼容组件)",
            "Browser.InternetExplorer~~~~0.0.1.0",
            "用于向下兼容早期老旧政企 OA 及网银 Active-X 控件的 Trident 引擎遗留组件。",
            "若日常工作不依赖古旧内网系统，建议移除以削减系统的老旧攻击面。"
        ),
        (
            "Fax_Scan",
            "Windows 传真和扫描 (Fax and Scan)",
            "Print.Fax.Scan~~~~0.0.1.0",
            "早期电话拨号调制解调器传真与老旧扫描仪控制工具，现代办公基本完全转向邮件与多功能一体机。",
            "若无传统硬件传真卡，常驻无用，可彻底移除。"
        ),
        (
            "WordPad",
            "写字板 (WordPad)",
            "Microsoft.Windows.WordPad~~~~0.0.1.0",
            "微软已于 Win11 24H2 起正式废弃并计划移除写字板，旧系统版本中仍可手动卸载。",
            "推荐使用 VS Code、记事本或 Office 替代，移除后无系统依赖副作用。"
        ),
        (
            "MathRecognizer",
            "数学识别器 (Math Recognizer)",
            "MathRecognizer~~~~0.0.1.0",
            "用于识别触控笔手写数学公式的辅助面板组件。",
            "如果不使用手写板或触控屏书写数学公式，属于纯冗余组件。"
        )
    ];

    public async Task<List<FodFeatureInfo>> GetFodFeaturesAsync()
    {
        return await Task.Run(() =>
        {
            var result = new List<FodFeatureInfo>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-WindowsCapability -Online | Where-Object { $_.State -eq 'Installed' } | Select-Object -ExpandProperty Name\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var installedCaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        var line = proc.StandardOutput.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line)) installedCaps.Add(line.Trim());
                    }
                    proc.WaitForExit(15000);
                }

                foreach (var item in KnownFodFeatures)
                {
                    bool installed = installedCaps.Any(c => c.Contains(item.Key, StringComparison.OrdinalIgnoreCase) || c.Contains(item.CapabilityName, StringComparison.OrdinalIgnoreCase));
                    result.Add(new FodFeatureInfo(item.Key, item.DisplayName, item.CapabilityName, item.Description, item.DebloatAdvice, installed));
                }
            }
            catch
            {
                foreach (var item in KnownFodFeatures)
                {
                    result.Add(new FodFeatureInfo(item.Key, item.DisplayName, item.CapabilityName, item.Description, item.DebloatAdvice, false));
                }
            }
            return result;
        });
    }

    public async Task<(bool Success, string Message)> RemoveFodFeatureAsync(string capabilityName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/Online /NoRestart /Remove-Capability /CapabilityName:{capabilityName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                string outStr = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit(45000);

                if (outStr.Contains("100.0%") || outStr.Contains("成功") || outStr.Contains("success", StringComparison.OrdinalIgnoreCase))
                {
                    return (true, $"已成功卸载可选功能 [{capabilityName}]！");
                }
                return (false, $"卸载返回: {outStr.Trim()}");
            }
            catch (Exception ex)
            {
                return (false, $"卸载可选功能异常: {ex.Message}");
            }
        });
    }

    #endregion

    #region 原生开发虚拟化与底层组件剥离 (WSL / 虚拟机平台 / Hyper-V / 沙盒)

    private static readonly (string Key, string DisplayName, string FeatureName, string Description, string DebloatAdvice)[] KnownVirtFeatures =
    [
        (
            "WSL",
            "WSL 2 (适用于 Linux 的 Windows 子系统)",
            "Microsoft-Windows-Subsystem-Linux",
            "在 Windows 上运行 Linux ELF64 二进制文件与轻量虚拟机的框架。纯 Windows 原生开发机无需此环境。",
            "关闭后移除 WSL 内核与守护进程，避免虚拟磁盘驱动与网络桥接服务在后台常驻。"
        ),
        (
            "VMP",
            "虚拟机平台 (Virtual Machine Platform)",
            "VirtualMachinePlatform",
            "WSL2 与安卓子系统 (WSA) 依赖的底层虚拟化平台。纯原生开发无需此虚拟化层支持。",
            "关闭可减少宿主机底层设备虚拟化拦截与内存映射开销。"
        ),
        (
            "HyperV",
            "Hyper-V 全套虚拟化组件",
            "Microsoft-Hyper-V-All",
            "微软企业级 Type-1 虚拟机监控程序与配套管理工具。开启时 Windows 运行在 Hypervisor 根分区之上。",
            "对于裸机本地原生开发，关闭 Hyper-V 可避免宿主 CPU 调度与时间片虚拟化损耗。"
        ),
        (
            "Sandbox",
            "Windows 沙盒 (Windows Sandbox)",
            "Containers-DisposableClientVM",
            "基于容器的轻量级纯净隔离运行沙盒环境，运行依赖于专用容器运行时与底层 Hyper-V 支撑。",
            "若不需要动态临时测试不受信软件，可关闭以释放容器运行时依赖。"
        )
    ];

    /// <summary>
    /// 获取虚拟化组件状态列表
    /// </summary>
    public List<VirtFeatureInfo> GetVirtualizationFeatures()
    {
        var result = new List<VirtFeatureInfo>();
        try
        {
            // WSL 检测: LxssManager / WslService 存在或 wsl.exe 存在
            bool wslEnabled = false;
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\LxssManager"))
            {
                if (key != null) wslEnabled = true;
            }
            if (!wslEnabled)
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WslService");
                if (key != null) wslEnabled = true;
            }

            // VMP 检测: vmcompute 容器计算服务
            bool vmpEnabled = false;
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\vmcompute"))
            {
                if (key != null) vmpEnabled = true;
            }

            // Hyper-V 检测: vmms 虚拟机管理服务
            bool hypervEnabled = false;
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\vmms"))
            {
                if (key != null) hypervEnabled = true;
            }

            // Sandbox 检测: WindowsSandbox.exe 是否存在
            bool sandboxEnabled = File.Exists(@"C:\Windows\System32\WindowsSandbox.exe");

            foreach (var item in KnownVirtFeatures)
            {
                bool isEnabled = item.Key switch
                {
                    "WSL" => wslEnabled,
                    "VMP" => vmpEnabled,
                    "HyperV" => hypervEnabled,
                    "Sandbox" => sandboxEnabled,
                    _ => false
                };
                result.Add(new VirtFeatureInfo(item.Key, item.DisplayName, item.FeatureName, item.Description, item.DebloatAdvice, isEnabled));
            }
        }
        catch
        {
            foreach (var item in KnownVirtFeatures)
            {
                result.Add(new VirtFeatureInfo(item.Key, item.DisplayName, item.FeatureName, item.Description, item.DebloatAdvice, false));
            }
        }
        return result;
    }

    /// <summary>
    /// 在管理员控制台通过 DISM 禁用单个虚拟化特性
    /// </summary>
    public (bool Success, string Message) DisableVirtFeatureInConsole(string featureName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k echo [正在执行 Windows 原生可选功能关闭: {featureName}] && dism.exe /online /norestart /disable-feature /featurename:{featureName}",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, $"已呼出管理员终端执行关闭 [{featureName}] 指令！");
        }
        catch (Exception ex)
        {
            return (false, $"执行异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 一键在管理员控制台批量关闭所有 4 项虚拟化与虚拟机底层特性
    /// </summary>
    public (bool Success, string Message) DisableAllVirtFeaturesInConsole()
    {
        try
        {
            string batchCmd = "echo [正在一键关闭纯原生开发不需要的所有虚拟化与虚拟机底层组件...] " +
                "&& dism.exe /online /norestart /disable-feature /featurename:Microsoft-Windows-Subsystem-Linux " +
                "&& dism.exe /online /norestart /disable-feature /featurename:VirtualMachinePlatform " +
                "&& dism.exe /online /norestart /disable-feature /featurename:Microsoft-Hyper-V-All " +
                "&& dism.exe /online /norestart /disable-feature /featurename:Containers-DisposableClientVM " +
                "&& echo. && echo [全部虚拟化组件关闭指令执行完毕！如提示需要重启系统生效，请按需重启。]";

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k {batchCmd}",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已呼出管理员控制台一键执行全套虚拟化组件关闭指令！");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    #endregion

    #region 开发机冗余后台服务精简 (Spooler / Xbox / WerSvc)

    /// <summary>
    /// 检测打印机后台服务 (Spooler) 是否已被禁用
    /// </summary>
    public bool IsSpoolerDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Spooler");
            var val = key?.GetValue("Start");
            return val is int intVal && intVal == 4; // 4 = Disabled
        }
        catch { return false; }
    }

    /// <summary>
    /// 停用或恢复打印机后台服务 (Spooler)
    /// </summary>
    public (bool Success, string Message) SetSpoolerDisabled(bool disable)
    {
        try
        {
            var script = disable
                ? "Stop-Service -Name 'Spooler' -Force -ErrorAction SilentlyContinue; Set-Service -Name 'Spooler' -StartupType Disabled -ErrorAction SilentlyContinue"
                : "Set-Service -Name 'Spooler' -StartupType Automatic -ErrorAction SilentlyContinue; Start-Service -Name 'Spooler' -ErrorAction SilentlyContinue";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, disable ? "已成功停止并禁用打印机后台服务 (Spooler)！" : "已恢复打印机后台服务自动启动。");
        }
        catch (Exception ex) { return (false, $"设置打印机服务失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 Xbox 游戏生态服务是否已禁用
    /// </summary>
    public bool IsXboxServicesDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\XblAuthManager");
            var val = key?.GetValue("Start");
            return val is int intVal && intVal == 4;
        }
        catch { return false; }
    }

    /// <summary>
    /// 停用或恢复 Xbox 游戏生态全套后台服务
    /// </summary>
    public (bool Success, string Message) SetXboxServicesDisabled(bool disable)
    {
        try
        {
            var services = "'XblAuthManager', 'XblGameSave', 'XboxNetApiSvc', 'XboxGipSvc'";
            var script = disable
                ? $"@({services}) | ForEach-Object {{ Stop-Service -Name $_ -Force -ErrorAction SilentlyContinue; Set-Service -Name $_ -StartupType Disabled -ErrorAction SilentlyContinue }}"
                : $"@({services}) | ForEach-Object {{ Set-Service -Name $_ -StartupType Manual -ErrorAction SilentlyContinue }}";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, disable ? "已成功禁用 Xbox 全套游戏后台服务！" : "已恢复 Xbox 服务默认设置。");
        }
        catch (Exception ex) { return (false, $"设置 Xbox 服务失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 Windows 错误报告服务 (WerSvc) 是否已禁用
    /// </summary>
    public bool IsWerSvcDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WerSvc");
            var val = key?.GetValue("Start");
            return val is int intVal && intVal == 4;
        }
        catch { return false; }
    }

    /// <summary>
    /// 停用或恢复 Windows 错误报告服务 (WerSvc)
    /// </summary>
    public (bool Success, string Message) SetWerSvcDisabled(bool disable)
    {
        try
        {
            var script = disable
                ? "Stop-Service -Name 'WerSvc' -Force -ErrorAction SilentlyContinue; Set-Service -Name 'WerSvc' -StartupType Disabled -ErrorAction SilentlyContinue"
                : "Set-Service -Name 'WerSvc' -StartupType Manual -ErrorAction SilentlyContinue";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, disable ? "已成功禁用 Windows 错误报告后台服务 (WerSvc)！" : "已恢复错误报告服务默认设置。");
        }
        catch (Exception ex) { return (false, $"设置错误报告服务失败: {ex.Message}"); }
    }

    /// <summary>
    /// 一键精简开发机所有低频冗余后台服务
    /// </summary>
    public (bool Success, string Message) SetAllDevRedundantServicesDisabled(bool disable)
    {
        SetSpoolerDisabled(disable);
        SetXboxServicesDisabled(disable);
        SetWerSvcDisabled(disable);
        return (true, disable
            ? "已一键禁用打印机 (Spooler)、Xbox 全套服务及错误报告 (WerSvc)！"
            : "已一键恢复上述开发机服务默认配置。");
    }

    #endregion
}
