using System.Diagnostics;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public record StartupItemInfo(string Name, string Command, string Scope);
public record OemAppInfo(string Name, string Vendor, string Description, string DebloatAdvice, bool IsDetected);

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
}
