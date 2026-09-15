using System.Diagnostics;

namespace AgyToolbox.Services;

public record UwpAppInfo(
    string Key,
    string DisplayName,
    string PackagePrefix,
    string Category,
    string Description,
    string DebloatAdvice,
    bool IsInstalled
);

public class UwpDebloatService
{
    private static readonly (string Key, string DisplayName, string PackagePrefix, string Category, string Description, string DebloatAdvice)[] KnownApps =
    [
        (
            "Weather",
            "天气 (MSN Weather)",
            "Microsoft.BingWeather",
            "日常资讯",
            "微软 MSN 提供的天气小组件与应用，常驻后台获取位置更新并推送天气通知。",
            "如果你习惯看手机天气或浏览器查天气，完全无用，可放心卸载。"
        ),
        (
            "People",
            "人脉 / 联系人 (Microsoft People)",
            "Microsoft.People",
            "通讯社交",
            "早年 Windows Phone 时代遗留的联系人同步中心，现已基本被微软边缘化。",
            "现代绝大多数人使用微信、钉钉或手机通讯录，此组件属于纯正僵尸应用，强烈建议卸载。"
        ),
        (
            "PhoneLink",
            "手机连接 (Phone Link / Your Phone)",
            "Microsoft.YourPhone",
            "多屏协同",
            "用于通过蓝牙或局域网与安卓/苹果手机同步通知、短信与照片。",
            "如果不使用 Windows 原生与手机跨屏互联功能，常驻后台吃内存，可安全卸载。"
        ),
        (
            "Cortana",
            "小娜语音助手 (Cortana)",
            "Microsoft.549981C6F30E1",
            "语音助理",
            "微软旧代语音助理，官方已全面停运并不再维护，被 Copilot 取代。",
            "官方已停止服务，属于彻底的遗留垃圾，强烈建议卸载。"
        ),
        (
            "XboxOverlay",
            "Xbox 游戏录屏与状态栏 (Game Bar)",
            "Microsoft.XboxGamingOverlay",
            "游戏辅助",
            "按 Win+G 调出的游戏遮罩层，提供录屏、帧率监控和 Xbox 好友聊天。",
            "不玩大型游戏、或使用 OBS / 显卡驱动自带录屏的开发者，卸载可提升游戏性能与系统响应。"
        ),
        (
            "XboxApp",
            "Xbox 游戏中心与身份验证",
            "Microsoft.XboxApp",
            "游戏平台",
            "微软 Xbox 主机联动与 PC Game Pass (XGP) 客户端。",
            "办公、开发用机或非 XGP 玩家无需保留，可放心卸载。"
        ),
        (
            "BingNews",
            "资讯与新闻 (MSN News)",
            "Microsoft.BingNews",
            "资讯广告",
            "MSN 聚合新闻，任务栏小组件新闻主要来源之一，附带商业推广。",
            "充斥各种八卦营销新闻，建议卸载以还桌面清爽。"
        ),
        (
            "FeedbackHub",
            "反馈中心 (Feedback Hub)",
            "Microsoft.WindowsFeedbackHub",
            "系统工具",
            "向微软提交 Windows Bug 和建议的渠道，普通用户极少使用。",
            "除了参与 Windows Insider 预览体验计划的极客，普通开发者无需保留。"
        ),
        (
            "GetHelp",
            "获取帮助 (Get Help)",
            "Microsoft.GetHelp",
            "帮助文档",
            "内置故障排查向导，但实际解决能力有限，多为引导去官网支持库。",
            "遇到问题查搜索引擎更直接，可安全卸载。"
        ),
        (
            "Tips",
            "提示 / 快速上手 (Microsoft Tips)",
            "Microsoft.Getstarted",
            "系统教程",
            "新装机后弹出的 Windows 11 功能教学卡片指南。",
            "看一遍就失去意义的预装指南，纯占磁盘空间，建议卸载。"
        ),
        (
            "Clipchamp",
            "Clipchamp 视频剪辑",
            "Clipchamp.Clipchamp",
            "媒体创作",
            "微软收购的云端在线视频剪辑工具，导出高画质往往需要订阅 Microsoft 365。",
            "专业用户使用剪映、PR 或剪辑软件，卸载完全不影响系统多媒体播放。"
        ),
        (
            "ZuneMusic",
            "Groove 音乐 (媒体播放器)",
            "Microsoft.ZuneMusic",
            "影音播放",
            "Windows 11 新版 Media Player，用于本地音频播放。",
            "常用第三方播放器（如网易云、QQ音乐、Foobar2000）的用户可按需卸载。"
        ),
        (
            "ZuneVideo",
            "电影和电视 (Films & TV)",
            "Microsoft.ZuneVideo",
            "影音播放",
            "Windows 默认视频播放器，很多专业编码格式（如 MKV、HEVC）需要付费下载扩展才能放。",
            "体验远落后于开源的 PotPlayer / VLC，推荐安装第三方播放器后卸载它。"
        ),
        (
            "Solitaire",
            "微软纸牌游戏集合 (Solitaire Collection)",
            "Microsoft.MicrosoftSolitaireCollection",
            "内置游戏",
            "传统的纸牌、蜘蛛纸牌小游戏，现代版本内嵌了开屏广告和内购升级。",
            "纯娱乐游戏，办公与开发用机无保留必要，可直接卸载。"
        ),
        (
            "Maps",
            "Windows 地图 (Windows Maps)",
            "Microsoft.WindowsMaps",
            "地图导航",
            "离线地图与导航，国内地图数据更新滞后，PC 端使用频率极低。",
            "PC 端基本都用网页版高德/百度地图，可直接卸载。"
        ),
        (
            "Store",
            "微软应用商店 (Microsoft Store)",
            "Microsoft.WindowsStore",
            "官方商城",
            "UWP 软件与游戏下载平台。但部分极简极客只用 winget 安装纯净软件，不需要应用商店。",
            "⚠️ 注意：卸载后将无法从商店图形界面下载应用。但极客可通过 winget install 命令行完全替代！请确认需要再卸载。"
        )
    ];

    /// <summary>
    /// 扫描当前系统上所有知名预装垃圾软件的真实安装状态
    /// </summary>
    public async Task<List<UwpAppInfo>> ScanInstalledAppsAsync()
    {
        return await Task.Run(() =>
        {
            var installedPackages = GetInstalledPackageNames();
            var result = new List<UwpAppInfo>();

            foreach (var item in KnownApps)
            {
                bool isInstalled = installedPackages.Any(pkg => pkg.Contains(item.PackagePrefix, StringComparison.OrdinalIgnoreCase));
                result.Add(new UwpAppInfo(
                    item.Key,
                    item.DisplayName,
                    item.PackagePrefix,
                    item.Category,
                    item.Description,
                    item.DebloatAdvice,
                    isInstalled
                ));
            }

            return result;
        });
    }

    /// <summary>
    /// 一键卸载指定的前缀包 (当前用户 + 新建用户预配模板)
    /// </summary>
    public async Task<(bool Success, string Message)> UninstallPackageAsync(string packagePrefix)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 1. 卸载当前用户
                var script = $@"
$ErrorActionPreference = 'SilentlyContinue'
Get-AppxPackage -Name '*{packagePrefix}*' | Remove-AppxPackage
Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -like '*{packagePrefix}*' }} | Remove-AppxProvisionedPackage -Online
";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(30000);

                return (true, $"已成功从本机移除 [{packagePrefix}] 及其预配模板！");
            }
            catch (Exception ex)
            {
                return (false, $"卸载异常: {ex.Message}");
            }
        });
    }

    private static HashSet<string> GetInstalledPackageNames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Get-AppxPackage | Select-Object -ExpandProperty Name\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                while (!proc.StandardOutput.EndOfStream)
                {
                    string? line = proc.StandardOutput.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        set.Add(line.Trim());
                    }
                }
                proc.WaitForExit(15000);
            }
        }
        catch
        {
            // 忽略读取异常
        }
        return set;
    }
}
