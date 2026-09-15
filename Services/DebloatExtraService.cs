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
}
