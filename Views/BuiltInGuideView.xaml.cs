using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AgyToolbox.Views;

public partial class BuiltInGuideView : UserControl
{
    public class DiagnosticSearchItem
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public int TargetTabIndex { get; set; }
        public string Symptom { get; set; } = "";
        public string CommandOrAction { get; set; } = "";
        public string Keywords { get; set; } = "";
    }

    private readonly List<DiagnosticSearchItem> _searchDatabase = new();

    public BuiltInGuideView()
    {
        InitializeComponent();
        InitializeSearchDatabase();
    }

    private void InitializeSearchDatabase()
    {
        _searchDatabase.AddRange(new[]
        {
            // Tab 0: 崩溃与内核急救
            new DiagnosticSearchItem
            {
                Title = "BSOD 蓝屏崩溃转储与停机码定位 (Minidump)",
                Category = "💥 崩溃与内核急救",
                TargetTabIndex = 0,
                Symptom = "蓝屏死机后提取崩溃转储快照，分析 Caused By Driver 故障驱动及 0x3B/0x7E/0x9F/0x133 停机码",
                CommandOrAction = "%SystemRoot%\\Minidump\\*.dmp (小内存转储 256KB)",
                Keywords = "蓝屏 bsod minidump 转储 dump bugcheck 0x3b 0x7e 0x9f 0x133 死机 崩溃"
            },
            new DiagnosticSearchItem
            {
                Title = "系统可靠性监视器 (Reliability Monitor)",
                Category = "💥 崩溃与内核急救",
                TargetTabIndex = 0,
                Symptom = "比事件查看器直观十倍。直观查阅历史每天的应用程序闪退、Windows 未正常关机与稳定性指数",
                CommandOrAction = "perfmon /rel",
                Keywords = "可靠性 稳定性 崩溃历史 perfmon /rel 闪退 记录"
            },
            new DiagnosticSearchItem
            {
                Title = "Windows 物理内存诊断 (mdsched.exe)",
                Category = "💥 崩溃与内核急救",
                TargetTabIndex = 0,
                Symptom = "排查频繁蓝屏、解压文件报 CRC 校验失败、XMP 超频不稳定或物理内存颗粒缺陷",
                CommandOrAction = "mdsched.exe",
                Keywords = "内存诊断 mdsched ram 物理内存 xmp 接触不良 闪退 crc"
            },
            new DiagnosticSearchItem
            {
                Title = "驱动程序验证程序管理器 (verifier.exe)",
                Category = "💥 崩溃与内核急救",
                TargetTabIndex = 0,
                Symptom = "对第三方驱动施加极限边界测试以揪出随机蓝屏元凶；紧急恢复执行 verifier /reset",
                CommandOrAction = "verifier.exe / verifier /reset",
                Keywords = "驱动验证 verifier 蓝屏驱动 极限测试 复原"
            },
            new DiagnosticSearchItem
            {
                Title = "系统文件检查与映像自愈 (SFC + DISM)",
                Category = "💥 崩溃与内核急救",
                TargetTabIndex = 0,
                Symptom = "修复任务栏卡死、DLL 缺失损坏或系统设置报错，支持联机与断网离线 ISO 修复",
                CommandOrAction = "sfc /scannow && dism /online /cleanup-image /restorehealth",
                Keywords = "sfc scannow dism 映像修复 损坏 系统文件 修复"
            },

            // Tab 1: 存储与文件系统
            new DiagnosticSearchItem
            {
                Title = "磁盘文件系统体检与坏道扫描 (chkdsk.exe)",
                Category = "💾 存储与文件系统",
                TargetTabIndex = 1,
                Symptom = "联机只读无感扫描 (/scan)、重启秒级修补 (/spotfix) 与物理坏扇区扫描 (/r)",
                CommandOrAction = "chkdsk C: /scan",
                Keywords = "chkdsk 磁盘检查 坏道 坏扇区 文件系统修复 spotfix"
            },
            new DiagnosticSearchItem
            {
                Title = "底层文件系统行为控制与 TRIM 查询 (fsutil.exe)",
                Category = "💾 存储与文件系统",
                TargetTabIndex = 1,
                Symptom = "查询并开启固态硬盘 TRIM 垃圾回收 (置0)、查询盘符脏位标记与禁用 8.3 短文件名",
                CommandOrAction = "fsutil behavior query DisableDeleteNotify",
                Keywords = "fsutil trim 垃圾回收 脏位 dirty 8.3 短文件名 固态优化"
            },
            new DiagnosticSearchItem
            {
                Title = "交互式磁盘分区与属性清除 (diskpart.exe)",
                Category = "💾 存储与文件系统",
                TargetTabIndex = 1,
                Symptom = "解除 U 盘移动硬盘只读写保护、全盘填零清空 (clean all)、转换 GPT 分区表与挂载 VHDX",
                CommandOrAction = "diskpart (attributes disk clear readonly / clean / convert gpt)",
                Keywords = "diskpart 分区 写保护 只读 清空 clean gpt vhd vhdx"
            },
            new DiagnosticSearchItem
            {
                Title = "多线程鲁棒文件镜像同步 (robocopy.exe)",
                Category = "💾 存储与文件系统",
                TargetTabIndex = 1,
                Symptom = "比普通 copy 快 5 倍。支持 16 线程并发 (/MT:16)、断点续传、忽略锁定文件重试",
                CommandOrAction = "robocopy C:\\Src D:\\Bak /MIR /MT:16 /ZB /R:2 /W:3",
                Keywords = "robocopy 同步 备份 镜像 多线程 复制 拷贝"
            },
            new DiagnosticSearchItem
            {
                Title = "NTFS 现代透明算法无损压缩 (compact.exe)",
                Category = "💾 存储与文件系统",
                TargetTabIndex = 1,
                Symptom = "利用现代 CPU 算力与 LZX 算法压缩只读开发仓库或大型游戏，运行时透明解压节省 30%~50% 空间",
                CommandOrAction = "compact /c /s /exe:lzx \"D:\\Target\\*\"",
                Keywords = "compact 压缩 lzx 透明压缩 节省空间 瘦身"
            },
            new DiagnosticSearchItem
            {
                Title = "空闲空间物理覆写擦除防数据恢复 (cipher.exe)",
                Category = "💾 存储与文件系统",
                TargetTabIndex = 1,
                Symptom = "仅对指定盘符的「未分配可用空间」进行三次填零与随机数物理擦写，绝不破坏现有文件",
                CommandOrAction = "cipher /w:C:",
                Keywords = "cipher 擦除 防恢复 隐私 清除 未分配空间 填零"
            },

            // Tab 2: 硬件拓扑与电源
            new DiagnosticSearchItem
            {
                Title = "现代 PowerShell/CIM 硬件与健康诊断 (替代旧版 wmic)",
                Category = "🔋 硬件拓扑与电源",
                TargetTabIndex = 2,
                Symptom = "Win11 24H2 淘汰 wmic 后的官方标准。查询 NVMe/SSD S.M.A.R.T. 健康、内存插槽单条频率与主板 BIOS",
                CommandOrAction = "Get-PhysicalDisk | Select DeviceId, FriendlyName, MediaType, HealthStatus",
                Keywords = "wmic cim get-physicaldisk 硬盘健康 smart 内存槽位 频率 bios"
            },
            new DiagnosticSearchItem
            {
                Title = "权威系统与图形诊断 (msinfo32.exe / dxdiag.exe)",
                Category = "🔋 硬件拓扑与电源",
                TargetTabIndex = 2,
                Symptom = "核验 UEFI 启动模式、安全引导 (Secure Boot)、VBS 虚拟化安全与 GPU Feature Levels 12_2 特性",
                CommandOrAction = "msinfo32.exe / dxdiag.exe",
                Keywords = "msinfo32 dxdiag uefi 安全引导 secure boot feature level whql directx"
            },
            new DiagnosticSearchItem
            {
                Title = "现代待机能耗与发热排查 (powercfg.exe)",
                Category = "🔋 硬件拓扑与电源",
                TargetTabIndex = 2,
                Symptom = "笔记本合盖放背包发热掉电诊断 (SleepStudy)、查询阻止睡眠的驱动 (/requests) 与电池损耗报告",
                CommandOrAction = "powercfg /sleepstudy && powercfg /requests",
                Keywords = "powercfg sleepstudy 睡眠发热 待机耗电 阻止睡眠 requests batteryreport 卓越性能"
            },

            // Tab 3: 网络栈与路由
            new DiagnosticSearchItem
            {
                Title = "原生端口与 TCP 三次握手测试 (Test-NetConnection)",
                Category = "🌐 网络栈与路由",
                TargetTabIndex = 3,
                Symptom = "彻底替代第三方 Telnet。测试指定远程 IP 或域名的端口连通性、DNS 解析与路由跃点",
                CommandOrAction = "Test-NetConnection -ComputerName <Host> -Port <Port> -InformationLevel Detailed",
                Keywords = "test-netconnection tnc 端口 telnet 连通性 tcp 握手 端口测试"
            },
            new DiagnosticSearchItem
            {
                Title = "Winsock 目录与 TCP/IP 协议栈重置 (netsh)",
                Category = "🌐 网络栈与路由",
                TargetTabIndex = 3,
                Symptom = "修复微信能发但网页打不开、代理残留断网、LSP 劫持与本地 WLAN 无线密码批量导出",
                CommandOrAction = "netsh winsock reset && netsh int ip reset",
                Keywords = "netsh winsock tcp ip 重置 断网 网页打不开 wlan 密码 导出"
            },
            new DiagnosticSearchItem
            {
                Title = "MTU 巨型帧探测与逐跳丢包排查 (ping vs tracert vs pathping)",
                Category = "🌐 网络栈与路由",
                TargetTabIndex = 3,
                Symptom = "ping -f -l 探测不分片 MTU、tracert -d 跳过反向解析 5 秒极速路由追踪、pathping 统计逐跳节点丢包",
                CommandOrAction = "ping -f -l 1472 1.1.1.1 / tracert -d <IP> / pathping <IP>",
                Keywords = "ping mtu tracert pathping 路由追踪 丢包 跃点 延迟"
            },
            new DiagnosticSearchItem
            {
                Title = "静态路由双网卡内外网分流 (route.exe)",
                Category = "🌐 网络栈与路由",
                TargetTabIndex = 3,
                Symptom = "同时连接内网网线与外网 WiFi 时，将指定内网网段流量锁定走内网网关，其余走外网",
                CommandOrAction = "route add -p 10.0.0.0 mask 255.0.0.0 10.12.1.1 metric 10",
                Keywords = "route 静态路由 双网卡 分流 内网 外网 冲突 跃点"
            },

            // Tab 4: 资源与性能巡检
            new DiagnosticSearchItem
            {
                Title = "资源监视器与关联句柄秒解占用 (resmon.exe)",
                Category = "🩺 资源与性能巡检",
                TargetTabIndex = 4,
                Symptom = "在 CPU 标签页搜索关联句柄彻底解决「文件被其他程序占用无法删除」，排查非分页缓冲池驱动内存泄漏",
                CommandOrAction = "resmon.exe (CPU 关联句柄 / 磁盘响应时间 > 50ms / 非分页缓冲池)",
                Keywords = "resmon 句柄 文件占用 删不掉 无法删除 内存泄漏 响应时间 端口监听"
            },
            new DiagnosticSearchItem
            {
                Title = "性能监视器四大黄金基线计数器 (perfmon.msc)",
                Category = "🩺 资源与性能巡检",
                TargetTabIndex = 4,
                Symptom = "CPU 占用率 (<80%)、可用物理内存 (>10%)、磁盘队列长度 (<2*通道数) 与硬换页率 (<20)",
                CommandOrAction = "perfmon.msc",
                Keywords = "perfmon 性能计数器 基线 cpu 内存 磁盘队列 换页"
            },
            new DiagnosticSearchItem
            {
                Title = "事件查看器关键故障 ID 审计 (eventvwr.msc)",
                Category = "🩺 资源与性能巡检",
                TargetTabIndex = 4,
                Symptom = "排查异常掉电 (事件41)、蓝屏转储写入 (事件1001)、服务崩溃 (事件7034) 与磁盘超时 (事件129)",
                CommandOrAction = "eventvwr.msc",
                Keywords = "eventvwr 事件查看器 事件41 事件1001 事件7034 意外断电 崩溃 日志"
            },
            new DiagnosticSearchItem
            {
                Title = "Windows 性能记录器与黑盒追踪 (WPR / wprui.exe)",
                Category = "🩺 资源与性能巡检",
                TargetTabIndex = 4,
                Symptom = "官方系统卡顿、CPU 异常峰值、DPC/ISR 延时、音频爆音的 ETW 内核黑盒录制工具",
                CommandOrAction = "wprui.exe / wpr -start GeneralProfile",
                Keywords = "wpr wprui 性能记录 etw dpc 卡顿 掉帧 爆音"
            },

            // Tab 5: 安全策略与权限
            new DiagnosticSearchItem
            {
                Title = "文件所有权剥夺与 ACL 访问控制重置 (takeown + icacls)",
                Category = "🛡️ 安全策略与权限",
                TargetTabIndex = 5,
                Symptom = "彻底解决删除系统残留文件提示「需要来自 TrustedInstaller 的权限」被拒绝访问",
                CommandOrAction = "takeown /F \"Target\" /R /A /D Y && icacls \"Target\" /grant administrators:F /T /C",
                Keywords = "takeown icacls trustedinstaller 权限 拒绝访问 所有权 删不掉 强制删除"
            },
            new DiagnosticSearchItem
            {
                Title = "原生文件哈希校验与 Base64 编解码 (certutil.exe)",
                Category = "🛡️ 安全策略与权限",
                TargetTabIndex = 5,
                Symptom = "无需下载任何第三方哈希工具，原生极速计算文件 SHA256 / MD5 / SHA1 校验码防篡改",
                CommandOrAction = "certutil -hashfile \"app.exe\" SHA256",
                Keywords = "certutil hash sha256 md5 哈希 校验 编码 base64"
            },
            new DiagnosticSearchItem
            {
                Title = "特权令牌审计与本地安全管理 (whoami / secpol.msc)",
                Category = "🛡️ 安全策略与权限",
                TargetTabIndex = 5,
                Symptom = "查看当前进程特权 (whoami /priv) 与完整性级别，配置账户锁定阈值与用户权利指派",
                CommandOrAction = "whoami /priv && secpol.msc",
                Keywords = "whoami 权限 特权 sedebugprivilege secpol 安全策略 组策略"
            },

            // Tab 6: 引导恢复与系统配置
            new DiagnosticSearchItem
            {
                Title = "Windows 恢复环境状态管理 (reagentc.exe)",
                Category = "⚙️ 引导恢复与系统配置",
                TargetTabIndex = 6,
                Symptom = "解决 Windows Update 0x80070643 补丁故障与开机进不去恢复环境，审计与重建 WinRE 镜像",
                CommandOrAction = "reagentc /info && reagentc /enable",
                Keywords = "reagentc winre 恢复环境 0x80070643 补丁失败 安全模式 引导"
            },
            new DiagnosticSearchItem
            {
                Title = "引导配置数据与强制安全模式 (bcdedit.exe)",
                Category = "⚙️ 引导恢复与系统配置",
                TargetTabIndex = 6,
                Symptom = "无需手忙脚乱按键盘，直接配置下次启动强制进入安全模式，或开启测试签名 (testsigning)",
                CommandOrAction = "bcdedit /set {current} safeboot minimal",
                Keywords = "bcdedit 安全模式 引导 testsigning 测试签名 bcd"
            },
            new DiagnosticSearchItem
            {
                Title = "绝对冷重启 (shutdown.exe) 避开快速启动混合休眠",
                Category = "⚙️ 引导恢复与系统配置",
                TargetTabIndex = 6,
                Symptom = "Windows 10/11 关机实为混合休眠保持内核死锁；执行绝对冷重启彻底重置内核与所有驱动",
                CommandOrAction = "shutdown /r /t 0",
                Keywords = "shutdown 冷重启 彻底重启 快速启动 混合休眠 重置驱动"
            },
            new DiagnosticSearchItem
            {
                Title = "系统配置实用程序与纯净启动 (msconfig.exe)",
                Category = "⚙️ 引导恢复与系统配置",
                TargetTabIndex = 6,
                Symptom = "纯净启动 (Clean Boot) 排错中枢，在服务标签勾选「隐藏所有 Microsoft 服务」定位流氓后台",
                CommandOrAction = "msconfig.exe",
                Keywords = "msconfig 系统配置 纯净启动 clean boot 启动项 服务排查"
            },

            // Tab 7: 检索语法与控制台
            new DiagnosticSearchItem
            {
                Title = "资源管理器高级查询语法 5 维实战矩阵 (AQS)",
                Category = "🧰 检索语法与管理控制台",
                TargetTabIndex = 7,
                Symptom = "无需第三方工具，掌握 kind:code、size:>1GB、datemodified:this week、name/content 全文索引与布尔逻辑",
                CommandOrAction = "(kind:docs OR kind:code) AND size:>10MB NOT name:min",
                Keywords = "aqs 搜索 资源管理器 查询 语法 kind size datemodified 全文检索"
            },
            new DiagnosticSearchItem
            {
                Title = "系统级 MMC 核心管理控制台矩阵 (devmgmt/services/gpedit/cleanmgr)",
                Category = "🧰 检索语法与管理控制台",
                TargetTabIndex = 7,
                Symptom = "直连设备管理器、服务配置、组策略与系统文件深度清理，绕过臃肿设置",
                CommandOrAction = "devmgmt.msc / services.msc / gpedit.msc / cleanmgr.exe",
                Keywords = "mmc devmgmt services gpedit cleanmgr 控制台 设备管理器 组策略"
            }
        });
    }

    private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = TbSearch.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(query))
        {
            TbSearchPlaceholder.Visibility = Visibility.Visible;
            BtnClearSearch.Visibility = Visibility.Collapsed;
            PanelSearchResults.Visibility = Visibility.Collapsed;
            MainTabControl.Visibility = Visibility.Visible;
        }
        else
        {
            TbSearchPlaceholder.Visibility = Visibility.Collapsed;
            BtnClearSearch.Visibility = Visibility.Visible;

            var filtered = _searchDatabase
                .Where(item =>
                    item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Symptom.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.CommandOrAction.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Keywords.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ListSearchResults.ItemsSource = filtered;
            TxtResultCount.Text = $"({filtered.Count} 项)";

            PanelSearchResults.Visibility = Visibility.Visible;
            MainTabControl.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
    {
        TbSearch.Text = "";
    }

    private void BtnGoToTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int tabIndex)
        {
            // 清空搜索框并切换到目标子标签页
            TbSearch.Text = "";
            if (tabIndex >= 0 && tabIndex < MainTabControl.Items.Count)
            {
                MainTabControl.SelectedIndex = tabIndex;
            }
        }
    }

    private void BtnCopySearchResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            Clipboard.SetText(cmd);
            MessageBox.Show($"已复制命令到剪贴板:\n{cmd}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
