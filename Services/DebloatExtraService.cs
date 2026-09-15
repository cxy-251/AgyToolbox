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
            "联想、戴尔、华硕等新机普遍预装 30 天试用版，极其顽固，常驻 10+ 后台进程，占用大量 CPU 与内存，强行拦截正常下载。",
            "试用到期后频繁弹窗恐吓诱导付费续订。强烈建议彻底卸载，使用 Windows 原生自带的 Defender 即可。"
        ),
        (
            "华硕奥创中心 (Armoury Crate)",
            "ASUS",
            "ArmouryCrate",
            "华硕天选/ROG 游戏本预装的灯效控制与性能调度中心，常驻 20 多个无用服务，是华硕电脑卡顿、偶发掉驱动的罪魁祸首之一。",
            "极客多使用轻量开源无广告的 G-Helper 完美替代，内存占用从 1GB 降至 20MB！"
        ),
        (
            "联想电脑管家 (Lenovo Vantage / PC Manager)",
            "Lenovo",
            "LenovoVantage",
            "联想拯救者/小新预装管理软件，附带保修查询、驱动更新与桌面小工具挂件，夹带资讯与开屏推送。",
            "若只需电池养护（限充80%）或性能模式切换，可使用开源轻量替代品（如 LenovoLegionToolkit）。"
        ),
        (
            "戴尔支持助手 (Dell SupportAssist)",
            "Dell",
            "SupportAssist",
            "戴尔笔记本官方驱动和硬件诊断套件，后台周期性唤醒扫描磁盘与硬件，容易导致风扇狂转与掉帧。",
            "普通排查直接使用本工具的硬件诊断即可，非必要建议卸载。"
        ),
        (
            "惠普助手 (HP Support Assistant)",
            "HP",
            "HPSupportAssistant",
            "惠普预装助手，常驻托盘收集设备日志并推送驱动通知。",
            "可安全移除，驱动由 Windows Update 或官网单包更新更稳定。"
        )
    ];

    /// <summary>
    /// 扫描当前系统是否运行或安装了常见 OEM 品牌垃圾全家桶
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
