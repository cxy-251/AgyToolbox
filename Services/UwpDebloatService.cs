using System.Diagnostics;

namespace AgyToolbox.Services;

public record UwpAppInfo(
    string Key,
    string DisplayName,
    string PackagePrefix,
    string Category,
    string Description,
    string DebloatAdvice,
    string OpenSourceAlternative,
    bool IsInstalled
);

public class UwpDebloatService
{
    private static readonly (string Key, string DisplayName, string PackagePrefix, string Category, string Description, string DebloatAdvice, string OpenSourceAlternative)[] KnownApps =
    [
        (
            "Weather",
            "天气 (MSN Weather)",
            "Microsoft.BingWeather",
            "日常资讯",
            "微软 MSN 提供的天气小组件与应用，常驻后台获取位置更新并推送天气通知。",
            "如果你习惯看手机天气或浏览器查天气，完全无用，常驻自启消耗内存。",
            "🌐 浏览器书签 / 手机天气 / 极简天气插件"
        ),
        (
            "People",
            "人脉 / 联系人 (Microsoft People)",
            "Microsoft.People",
            "通讯社交",
            "早年 Windows Phone 时代遗留的联系人同步中心，现已基本被微软边缘化。",
            "现代绝大多数人使用微信、钉钉或手机通讯录，此组件属于纯正僵尸应用，强烈建议卸载。",
            "🟢 微信 / 钉钉 / 手机端联系人管理"
        ),
        (
            "PhoneLink",
            "手机连接 (Phone Link / Your Phone)",
            "Microsoft.YourPhone",
            "多屏协同",
            "用于通过蓝牙或局域网与安卓/苹果手机同步通知、短信与照片。",
            "如果不使用 Windows 原生与手机跨屏互联功能，常驻后台吃内存，可安全卸载。",
            "📡 本工具自带的【局域网快传 WebDrop】(手机扫码秒传)"
        ),
        (
            "Cortana",
            "小娜语音助手 (Cortana)",
            "Microsoft.549981C6F30E1",
            "语音助理",
            "微软旧代语音助理，官方已全面停运并不再维护，被 Copilot 取代。",
            "官方已停止服务，属于彻底的遗留垃圾，强烈建议卸载。",
            "🤖 现代主流 AI 对话客户端"
        ),
        (
            "XboxOverlay",
            "Xbox 游戏录屏与状态栏 (Game Bar)",
            "Microsoft.XboxGamingOverlay",
            "游戏辅助",
            "按 Win+G 调出的游戏遮罩层，提供录屏、帧率监控和 Xbox 好友聊天。",
            "不玩大型游戏、或使用显卡驱动自带录屏的开发者，卸载可提升游戏性能与系统响应。",
            "🎥 OBS Studio (开源专业录屏) / Cap (极简录屏)"
        ),
        (
            "XboxApp",
            "Xbox 游戏中心与身份验证",
            "Microsoft.XboxApp",
            "游戏平台",
            "微软 Xbox 主机联动与 PC Game Pass (XGP) 客户端。",
            "办公、开发用机或非 XGP 玩家无需保留，可放心卸载。",
            "🎮 Steam / 独立游戏客户端"
        ),
        (
            "XboxTCUI",
            "Xbox 游戏界面与语音朗读",
            "Microsoft.XboxSpeechToTextOverlay",
            "游戏辅助",
            "用于多人联机游戏时的语音转文字和辅屏遮罩服务。",
            "普通办公/代码开发用机绝无保留必要，建议连同清理。",
            "无 (办公开发不需要)"
        ),
        (
            "BingNews",
            "资讯与新闻 (MSN News)",
            "Microsoft.BingNews",
            "资讯广告",
            "MSN 聚合新闻，任务栏小组件新闻主要来源之一，附带商业推广与八卦营销。",
            "充斥各种营销新闻，建议卸载以还桌面清爽。",
            "📰 RSS 阅读器 (Fluent Reader 开源) / 专注文档"
        ),
        (
            "FeedbackHub",
            "反馈中心 (Feedback Hub)",
            "Microsoft.WindowsFeedbackHub",
            "系统工具",
            "向微软提交 Windows Bug 和建议的渠道，普通用户极少使用。",
            "除了参与 Windows Insider 预览体验计划的极客，普通开发者无需保留。",
            "无 (遇到问题直接搜索引擎/社区)"
        ),
        (
            "GetHelp",
            "获取帮助 (Get Help)",
            "Microsoft.GetHelp",
            "帮助文档",
            "内置故障排查向导，但实际解决能力有限，多为引导去官网支持库。",
            "遇到问题查搜索引擎更直接，可安全卸载。",
            "🔍 Google / Bing / 官方开发者技术文档"
        ),
        (
            "Tips",
            "提示 / 快速上手 (Microsoft Tips)",
            "Microsoft.Getstarted",
            "系统教程",
            "新装机后弹出的 Windows 11 功能教学卡片指南。",
            "看一遍就失去意义的预装指南，纯占磁盘空间，建议卸载。",
            "📖 本工具自带的【系统自带工具深度指南】"
        ),
        (
            "Clipchamp",
            "Clipchamp 视频剪辑",
            "Clipchamp.Clipchamp",
            "媒体创作",
            "微软收购的云端在线视频剪辑工具，导出高画质往往需要订阅 Microsoft 365。",
            "专业用户使用剪映、PR 或剪辑软件，卸载完全不影响系统多媒体播放。",
            "✂️ Shotcut (跨平台开源) / Kdenlive / 剪映"
        ),
        (
            "ZuneMusic",
            "Groove 音乐 (Media Player)",
            "Microsoft.ZuneMusic",
            "影音播放",
            "Windows 11 新版 Media Player，用于本地音频播放，功能简陋且占用大。",
            "常用第三方播放器的用户可按需卸载，释放内存。",
            "🎵 Foobar2000 (极简低内存标杆) / YesPlayMusic (高颜值开源)"
        ),
        (
            "ZuneVideo",
            "电影和电视 (Films & TV)",
            "Microsoft.ZuneVideo",
            "影音播放",
            "Windows 默认视频播放器，很多专业编码格式（如 MKV、HEVC）需要付费下载扩展才能放。",
            "体验远落后于开源的 PotPlayer / VLC，推荐安装第三方播放器后卸载它。",
            "🎬 PotPlayer (超强硬解滤镜) / VLC (工业级全能开源)"
        ),
        (
            "Photos",
            "微软照片 (Microsoft Photos)",
            "Microsoft.Windows.Photos",
            "图片浏览",
            "自带的照片查看器，启动卡顿，常夹带相册人脸识别后台服务和视频编辑器入口。",
            "启动较慢，遇到超大尺寸图片容易卡死。",
            "🖼️ ImageGlass (开源极速轻量看图) / Honeyview (蜂蜜浏览器)"
        ),
        (
            "StickyNotes",
            "微软便笺 (Sticky Notes)",
            "Microsoft.MicrosoftStickyNotes",
            "日常笔记",
            "桌面黄色便签小卡片，支持微软账户云同步。",
            "排版简陋不支持代码高亮，易丢失，现代开发者多使用专业知识库。",
            "📝 Notepad-- (国产轻量开源) / Obsidian / VS Code"
        ),
        (
            "SoundRecorder",
            "Windows 录音机 (Voice Recorder)",
            "Microsoft.WindowsSoundRecorder",
            "音频工具",
            "极简系统录音工具，格式单一，无降噪或剪辑功能。",
            "使用率极低，普通电脑用户极少需要录制原生音频。",
            "🎙️ Audacity (专业级开源多轨录音与降噪神器)"
        ),
        (
            "Paint3D",
            "画图 3D (Paint 3D)",
            "Microsoft.MSPaint",
            "图形创作",
            "微软早年宣传 3D 创作的实验产品，现官方已彻底停止维护与更新。",
            "已被微软官方放弃，占用磁盘无任何实用价值，强烈建议卸载。",
            "🎨 Paint.NET (轻量全功能) / GIMP (全能开源)"
        ),
        (
            "Teams",
            "个人版 Teams 聊天 (Chat / Teams)",
            "MicrosoftTeams",
            "即时通讯",
            "Windows 11 任务栏强推的个人版 Teams 磁贴，并非企业版。",
            "国内几乎无人使用个人版 Teams，纯粹占用任务栏和后台。",
            "💬 微信 / 钉钉 / 飞书"
        ),
        (
            "Skype",
            "Skype 网络电话",
            "Microsoft.SkypeApp",
            "通讯社交",
            "传统网络电话与视频会议客户端，早年海外使用居多。",
            "已被时代淘汰，国内完全无用，放心卸载。",
            "📞 腾讯会议 / 微信音视频"
        ),
        (
            "Solitaire",
            "微软纸牌游戏集合 (Solitaire Collection)",
            "Microsoft.MicrosoftSolitaireCollection",
            "内置游戏",
            "传统的纸牌、蜘蛛纸牌小游戏，现代版本内嵌了开屏广告和内购升级。",
            "纯娱乐游戏，办公与开发用机无保留必要，可直接卸载。",
            "🎯 纯粹单机游戏或 Steam"
        ),
        (
            "Maps",
            "Windows 地图 (Windows Maps)",
            "Microsoft.WindowsMaps",
            "地图导航",
            "离线地图与导航，国内地图数据更新滞后，PC 端使用频率极低。",
            "PC 端基本都用网页版高德/百度地图，可直接卸载。",
            "🗺️ 网页版高德地图 / 百度地图"
        ),
        (
            "QuickAssist",
            "快速助手 (Quick Assist)",
            "MicrosoftCorporationII.QuickAssist",
            "远程协助",
            "微软内置远程桌面协助工具，经常网络连不上或强制要求登微软账号。",
            "体验不如专业远程桌面工具，极少使用，可安全卸载。",
            "🚀 RustDesk (全平台开源远程桌面标杆) / ToDesk"
        ),
        (
            "PowerAutomate",
            "Power Automate 自动化桌面版",
            "Microsoft.PowerAutomateDesktop",
            "流程自动化",
            "微软强推的企业 RPA 桌面流自动化应用，体积极大 (占用几个 G 空间)。",
            "普通个人电脑和开发者完全不需要，纯粹消耗磁盘空间，强烈建议卸载。",
            "🐍 Python 自动化脚本 / 批处理"
        ),
        (
            "OneNoteUwp",
            "OneNote for Windows 10 (遗留 UWP 版)",
            "Microsoft.Office.OneNote",
            "笔记应用",
            "Windows 10 时代遗留的 UWP 版 OneNote，微软官方已全面停更并不再维护。",
            "官方停更产品，若需要请安装 Office 365 桌面版或现代知识库。",
            "📚 Obsidian (本地离线双链笔记) / Notion"
        ),
        (
            "MixedReality",
            "混合现实门户 (Mixed Reality Portal)",
            "Microsoft.MixedReality.Portal",
            "VR 虚拟现实",
            "微软 Windows Mixed Reality 头显控制门户，微软已正式宣布全面放弃该项目。",
            "项目已遭微软砍掉，纯占盘垃圾组件，建议直接清理。",
            "无 (已废弃技术)"
        ),
        (
            "Alarms",
            "闹钟和时钟 (Windows Alarms)",
            "Microsoft.WindowsAlarms",
            "时钟工具",
            "提供电脑端闹钟、倒计时与世界时钟。",
            "大部分用户使用手机或手表闹钟，PC 睡眠后闹钟无法唤醒电脑，实用性低。",
            "⏰ 手机系统自带闹钟"
        ),
        (
            "Store",
            "微软应用商店 (Microsoft Store)",
            "Microsoft.WindowsStore",
            "官方商城",
            "UWP 软件与游戏下载平台。但部分极简极客只用 winget 安装纯净软件，不需要应用商店。",
            "⚠️ 注意：卸载后将无法从商店图形界面下载应用。但极客可通过 winget 命令行完全替代！确认需要再卸载。",
            "⚡ winget (官方原生命令行包管理器，全自动化干净安装)"
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
                    item.OpenSourceAlternative,
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
