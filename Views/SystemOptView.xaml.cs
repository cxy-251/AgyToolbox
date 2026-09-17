using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public class PathDisplayItem
{
    public string Path { get; set; } = "";
    public bool Exists { get; set; }
    public string ExistsText => Exists ? "✅ 路径有效" : "🚫 幽灵死路径 (不存在)";
    public Brush StatusBrush => Exists ? ThemeBrushes.Success : ThemeBrushes.Danger;
}

public partial class SystemOptView : UserControl
{
    private readonly WindowsUpdateService _windowsUpdateService = new();
    private readonly WinOptimizerService _winOptimizerService = new();
    private readonly EnvManagerService _envManagerService = new();
    private readonly ObservableCollection<PathDisplayItem> _pathItems = new();

    public SystemOptView()
    {
        InitializeComponent();

        GridPathEntries.ItemsSource = _pathItems;
        LoadPathEntries(false);
        RefreshOptimizerStatus();
        RefreshUpdateStatus();
        RefreshUpdateTuningStatus();
        RefreshBitLockerStatus();
        RefreshHiberStatus();
        RefreshFastStartupStatus();
        RefreshReservedStorageStatus();
        RefreshStorageSenseStatus();
        RefreshExplorerSettings();
        RefreshDevIoStatus();
    }

    #region 1. Windows 自动更新彻底控制

    private void RefreshUpdateStatus()
    {
        bool disabled = _windowsUpdateService.IsUpdateDisabled();
        TxtUpdateStatus.Text = disabled ? "● 自动更新已彻底关闭并锁定" : "○ 自动更新处于开启状态";
        TxtUpdateStatus.Foreground = disabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
    }

    private void BtnRefreshUpdateStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshUpdateStatus();
        MessageBox.Show("Windows 更新状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnDisableUpdate_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要彻底关闭 Windows 自动更新吗？\n\n" +
            "【防护机制】：\n" +
            "1. 组策略锁定 NoAutoUpdate=1 (杜绝后台静默下载)；\n" +
            "2. 注册表锁定 ExcludeWPDriversInQualityUpdate=1 (防止微软用公版驱动强行覆盖最新显卡驱动导致玩游戏黑屏)；\n" +
            "3. 停止并禁用更新服务 (wuauserv) 及更新唤醒看门狗 (WaaSMedicSvc)。\n\n" +
            "随时可通过旁边绿色按钮一键恢复。是否继续？",
            "确认关闭自动更新",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        var (ok, msg) = await _windowsUpdateService.DisableUpdateAsync();
        RefreshUpdateStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private async void BtnEnableUpdate_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = await _windowsUpdateService.EnableUpdateAsync();
        RefreshUpdateStatus();
        MessageBox.Show(msg, ok ? "已恢复" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 1.1 更新行为微调 (防自动重启 & 禁用 P2P 上传)

    private void RefreshUpdateTuningStatus()
    {
        bool noAutoReboot = _winOptimizerService.IsNoAutoRebootConfigured();
        TxtNoAutoRebootStatus.Text = noAutoReboot ? "[已配置：用户登录时绝不擅自重启]" : "[系统默认：可能自动重启]";
        TxtNoAutoRebootStatus.Foreground = noAutoReboot ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool p2pDisabled = _winOptimizerService.IsDeliveryOptimizationP2PDisabled();
        TxtDeliveryOptStatus.Text = p2pDisabled ? "[已彻底阻断 P2P 上传偷跑]" : "[系统默认：允许局域网/互联网上传]";
        TxtDeliveryOptStatus.Foreground = p2pDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
    }

    private void BtnRefreshUpdateTuning_Click(object sender, RoutedEventArgs e)
    {
        RefreshUpdateTuningStatus();
        MessageBox.Show("更新微调策略状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnToggleNoAutoReboot_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsNoAutoRebootConfigured();
        var (ok, msg) = _winOptimizerService.SetNoAutoReboot(!current);
        RefreshUpdateTuningStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnToggleDeliveryOpt_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsDeliveryOptimizationP2PDisabled();
        var (ok, msg) = _winOptimizerService.SetDeliveryOptimizationP2PDisabled(!current);
        RefreshUpdateTuningStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 2. BitLocker 状态透视与恢复密钥

    private void RefreshBitLockerStatus()
    {
        var (isEncrypted, details) = _winOptimizerService.GetBitLockerStatus();
        TxtBitLockerStatus.Text = isEncrypted ? "⚠️ C 盘已启用 BitLocker 加密" : "○ C 盘未开启加密";
        TxtBitLockerStatus.Foreground = isEncrypted ? ThemeBrushes.Danger : ThemeBrushes.Success;
    }

    private void BtnRefreshBitLocker_Click(object sender, RoutedEventArgs e)
    {
        RefreshBitLockerStatus();
        MessageBox.Show("BitLocker 状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnGetBitLockerKey_Click(object sender, RoutedEventArgs e)
    {
        var (ok, details) = _winOptimizerService.GetBitLockerRecoveryKey();
        if (ok)
        {
            Clipboard.SetText(details);
            MessageBox.Show(
                $"【重要：BitLocker 保护信息已复制到剪贴板】\n\n{details}\n\n请务必拍照或记录 48 位数字恢复密钥存入手机！",
                "BitLocker 恢复密钥",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(details, "查询提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #endregion

    #region 3. 电源与存储 (休眠三档 / 快速启动 / 保留存储 / 专用网络 / 卓越性能)

    private void RefreshHiberStatus()
    {
        bool exists = _winOptimizerService.IsHibernationEnabled();
        TxtHiberStatus.Text = exists ? "[休眠开启中 (hiberfil.sys 占用中)]" : "[已彻底关停休眠 (已释放 100% 空间)]";
        TxtHiberStatus.Foreground = exists ? ThemeBrushes.Warning : ThemeBrushes.Success;
    }

    private void RefreshFastStartupStatus()
    {
        bool fast = _winOptimizerService.IsFastStartupEnabled();
        TxtFastStartupStatus.Text = fast ? "[已开启快速启动]" : "[已关闭快速启动 (彻底切断硬件电源)]";
        TxtFastStartupStatus.Foreground = fast ? ThemeBrushes.Warning : ThemeBrushes.Success;
    }

    private void RefreshReservedStorageStatus()
    {
        var (enabled, details) = _winOptimizerService.GetReservedStorageStatus();
        TxtReservedStorageStatus.Text = enabled ? "[已启用 (约预留 7GB 磁盘缓冲)]" : "[已停用/已释放]";
        TxtReservedStorageStatus.Foreground = enabled ? ThemeBrushes.Warning : ThemeBrushes.Success;
    }

    private void BtnRefreshHiber_Click(object sender, RoutedEventArgs e)
    {
        RefreshHiberStatus();
        RefreshFastStartupStatus();
        MessageBox.Show("休眠与快速启动状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnHiberOff_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetHibernationTier(0);
        RefreshHiberStatus();
        RefreshFastStartupStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnHiberReduced_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetHibernationTier(1);
        RefreshHiberStatus();
        RefreshFastStartupStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnHiberFull_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetHibernationTier(2);
        RefreshHiberStatus();
        RefreshFastStartupStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnToggleFastStartup_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsFastStartupEnabled();
        var (ok, msg) = _winOptimizerService.SetFastStartup(!current);
        RefreshFastStartupStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnRefreshReservedStorage_Click(object sender, RoutedEventArgs e)
    {
        RefreshReservedStorageStatus();
        var (_, details) = _winOptimizerService.GetReservedStorageStatus();
        MessageBox.Show($"保留存储查询结果：\n\n{details}", "查询结果", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnToggleReservedStorage_Click(object sender, RoutedEventArgs e)
    {
        var (enabled, _) = _winOptimizerService.GetReservedStorageStatus();
        var (ok, msg) = _winOptimizerService.SetReservedStorage(!enabled);
        RefreshReservedStorageStatus();
        MessageBox.Show(msg, ok ? "操作已下发" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnSetPrivateNetwork_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetNetworkToPrivate();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void RefreshStorageSenseStatus()
    {
        bool sense = _winOptimizerService.IsStorageSenseEnabled();
        TxtStorageSenseStatus.Text = sense ? "[已启用存储感知自动清理]" : "[当前已停用]";
        TxtStorageSenseStatus.Foreground = sense ? ThemeBrushes.Success : ThemeBrushes.Warning;
    }

    private void BtnRefreshStorageSense_Click(object sender, RoutedEventArgs e)
    {
        RefreshStorageSenseStatus();
        MessageBox.Show("存储感知状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnToggleStorageSense_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsStorageSenseEnabled();
        var (ok, msg) = _winOptimizerService.SetStorageSenseEnabled(!current);
        RefreshStorageSenseStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnEnableUltimatePerf_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.EnableUltimatePerformance();
        MessageBox.Show(msg, ok ? "激活成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 4. 资源管理器开荒与系统交互

    private void RefreshExplorerSettings()
    {
        ChkShowFileExt.IsChecked = _winOptimizerService.IsFileExtVisible();
        ChkShowHiddenFiles.IsChecked = _winOptimizerService.IsHiddenFilesVisible();
        ChkShowSuperHidden.IsChecked = _winOptimizerService.IsSuperHiddenFilesVisible();
        ChkOpenThisPc.IsChecked = _winOptimizerService.IsOpenThisPcDefault();
    }

    private void BtnRefreshExplorerSettings_Click(object sender, RoutedEventArgs e)
    {
        RefreshExplorerSettings();
        MessageBox.Show("资源管理器设置状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ChkShowFileExt_Click(object sender, RoutedEventArgs e)
    {
        bool show = ChkShowFileExt.IsChecked == true;
        var (ok, msg) = _winOptimizerService.SetFileExtVisible(show);
        if (!ok) MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ChkShowHiddenFiles_Click(object sender, RoutedEventArgs e)
    {
        bool show = ChkShowHiddenFiles.IsChecked == true;
        var (ok, msg) = _winOptimizerService.SetHiddenFilesVisible(show);
        if (!ok) MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ChkShowSuperHidden_Click(object sender, RoutedEventArgs e)
    {
        bool show = ChkShowSuperHidden.IsChecked == true;
        var (ok, msg) = _winOptimizerService.SetSuperHiddenFilesVisible(show);
        if (!ok) MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ChkOpenThisPc_Click(object sender, RoutedEventArgs e)
    {
        bool thisPc = ChkOpenThisPc.IsChecked == true;
        var (ok, msg) = _winOptimizerService.SetOpenThisPcDefault(thisPc);
        if (!ok) MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    #endregion

    #region 5. Windows 11/10 体验去魅优化

    private void RefreshOptimizerStatus()
    {
        bool classic = _winOptimizerService.IsClassicContextMenuEnabled();
        TxtClassicMenuStatus.Text = classic ? "[已开启 Win10 经典菜单]" : "[当前为 Win11 折叠菜单]";
        TxtClassicMenuStatus.Foreground = classic ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool deskIcons = _winOptimizerService.IsDesktopIconsVisible();
        TxtDesktopIconsStatus.Text = deskIcons ? "[已在桌面显示核心图标]" : "[桌面图标隐藏/默认]";
        TxtDesktopIconsStatus.Foreground = deskIcons ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool noBing = _winOptimizerService.IsBingSearchDisabled();
        TxtBingStatus.Text = noBing ? "[已关闭必应搜索广告]" : "[当前保留必应搜索与热搜]";
        TxtBingStatus.Foreground = noBing ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool noTele = _winOptimizerService.IsTelemetryDisabled();
        TxtTelemetryStatus.Text = noTele ? "[已禁用个性化遥测广告]" : "[当前为默认遥测]";
        TxtTelemetryStatus.Foreground = noTele ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool noSticky = _winOptimizerService.IsStickyKeysPromptDisabled();
        TxtStickyStatus.Text = noSticky ? "[已禁用 5 次 Shift 弹窗]" : "[当前为系统默认弹窗]";
        TxtStickyStatus.Foreground = noSticky ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool clipHist = _winOptimizerService.IsClipboardHistoryEnabled();
        TxtClipboardStatus.Text = clipHist ? "[已开启 Win+V 剪贴板历史]" : "[当前未开启剪贴板历史]";
        TxtClipboardStatus.Foreground = clipHist ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool devMode = _winOptimizerService.IsDevModeAndLongPathsEnabled();
        TxtDevModeStatus.Text = devMode ? "[已开启开发者模式与长路径]" : "[当前为标准用户限制]";
        TxtDevModeStatus.Foreground = devMode ? ThemeBrushes.Success : ThemeBrushes.Warning;

        bool uacSec = _winOptimizerService.IsUacSecureDesktopEnabled();
        TxtUacDesktopStatus.Text = uacSec ? "[已开启全屏暗屏防护 (安全推荐)]" : "[已关闭暗屏 (普通桌面弹窗)]";
        TxtUacDesktopStatus.Foreground = uacSec ? ThemeBrushes.Success : ThemeBrushes.Info;

        int searchMode = _winOptimizerService.GetTaskbarSearchMode();
        string searchModeDesc = searchMode switch
        {
            0 => "[当前状态：完全隐藏]",
            1 => "[当前状态：仅显示图标]",
            2 => "[当前状态：展开搜索框]",
            _ => "[当前状态：自定义/未设定]"
        };
        TxtSearchboxModeStatus.Text = searchModeDesc;
        TxtSearchboxModeStatus.Foreground = ThemeBrushes.Success;
    }

    private void BtnToggleClassicMenu_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsClassicContextMenuEnabled();
        var (ok, msg) = _winOptimizerService.SetClassicContextMenu(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleDesktopIcons_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsDesktopIconsVisible();
        var (ok, msg) = _winOptimizerService.SetDesktopIconsVisible(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "桌面图标设置", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleBing_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsBingSearchDisabled();
        var (ok, msg) = _winOptimizerService.SetBingSearchDisabled(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleTelemetry_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsTelemetryDisabled();
        var (ok, msg) = _winOptimizerService.SetTelemetryDisabled(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleSticky_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsStickyKeysPromptDisabled();
        var (ok, msg) = _winOptimizerService.SetStickyKeysPromptDisabled(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleClipboard_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsClipboardHistoryEnabled();
        var (ok, msg) = _winOptimizerService.SetClipboardHistoryEnabled(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleDevMode_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsDevModeAndLongPathsEnabled();
        var (ok, msg) = _winOptimizerService.SetDevModeAndLongPaths(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnToggleUacDesktop_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsUacSecureDesktopEnabled();
        var (ok, msg) = _winOptimizerService.SetUacSecureDesktop(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnSearchModeHide_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetTaskbarSearchMode(0);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnSearchModeIcon_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetTaskbarSearchMode(1);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnSearchModeBox_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.SetTaskbarSearchMode(2);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnRestartExplorer_Click(object sender, RoutedEventArgs e)
    {
        _winOptimizerService.RestartExplorer();
        MessageBox.Show("已向资源管理器发送重启指令，桌面与任务栏将在 1~2 秒内刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region 6. PATH 环境变量管理

    private void LoadPathEntries(bool isSystem)
    {
        _pathItems.Clear();
        var entries = _envManagerService.GetPathEntries(isSystem);
        foreach (var entry in entries)
        {
            _pathItems.Add(new PathDisplayItem
            {
                Path = entry.Path,
                Exists = entry.Exists
            });
        }
    }

    private void RbPathScope_Checked(object sender, RoutedEventArgs e)
    {
        if (GridPathEntries == null) return;
        bool isSystem = RbSysPath.IsChecked == true;
        LoadPathEntries(isSystem);
    }

    private void BtnReloadPath_Click(object sender, RoutedEventArgs e)
    {
        bool isSystem = RbSysPath.IsChecked == true;
        LoadPathEntries(isSystem);
        MessageBox.Show("PATH 环境变量列表已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnRemoveDeadPaths_Click(object sender, RoutedEventArgs e)
    {
        var deadItems = _pathItems.Where(p => !p.Exists).ToList();
        if (deadItems.Count == 0)
        {
            MessageBox.Show("当前未检测到已失效的死路径！环境变量健康。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"检测到 {deadItems.Count} 个已失效死路径，确认一键从列表中清除吗？\n(清除后需点击绿色‘保存并广播’按钮方可正式写入系统)", "清理死路径", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        foreach (var item in deadItems)
        {
            _pathItems.Remove(item);
        }
        MessageBox.Show($"已清除 {deadItems.Count} 个死路径，请记得点击右侧【保存并向系统广播生效】！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnMovePathUp_Click(object sender, RoutedEventArgs e)
    {
        int index = GridPathEntries.SelectedIndex;
        if (index > 0)
        {
            var item = _pathItems[index];
            _pathItems.RemoveAt(index);
            _pathItems.Insert(index - 1, item);
            GridPathEntries.SelectedIndex = index - 1;
        }
    }

    private void BtnMovePathDown_Click(object sender, RoutedEventArgs e)
    {
        int index = GridPathEntries.SelectedIndex;
        if (index >= 0 && index < _pathItems.Count - 1)
        {
            var item = _pathItems[index];
            _pathItems.RemoveAt(index);
            _pathItems.Insert(index + 1, item);
            GridPathEntries.SelectedIndex = index + 1;
        }
    }

    private void BtnAddPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择要添加到 PATH 的文件夹路径"
        };
        if (dialog.ShowDialog() == true)
        {
            string path = dialog.FolderName.Trim();
            if (_pathItems.Any(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该路径已存在于 PATH 列表中！无需重复添加。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _pathItems.Add(new PathDisplayItem
            {
                Path = path,
                Exists = true
            });
        }
    }

    private void BtnDeleteSelectedPath_Click(object sender, RoutedEventArgs e)
    {
        if (GridPathEntries.SelectedItem is PathDisplayItem item)
        {
            _pathItems.Remove(item);
        }
    }

    private void BtnSavePath_Click(object sender, RoutedEventArgs e)
    {
        bool isSystem = RbSysPath.IsChecked == true;
        var paths = _pathItems.Select(p => p.Path).ToList();
        var (ok, msg) = _envManagerService.SavePathEntries(isSystem, paths);
        MessageBox.Show(msg, ok ? "保存成功" : "保存失败", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    #endregion

    #region 10. 原生开发 CPU 与磁盘 I/O 调优

    private void RefreshDevIoStatus()
    {
        // 1. Defender 实时防护
        bool defenderDisabled = _winOptimizerService.IsDefenderRealtimeDisabled();
        TxtDefenderRealtimeStatus.Text = defenderDisabled ? "● 实时监控已关闭 (编译免扫描)" : "○ 实时监控已开启";
        TxtDefenderRealtimeStatus.Foreground = defenderDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleDefenderRealtime.Content = defenderDisabled ? "开启实时防护" : "关闭实时防护";

        // 2. SmartScreen
        bool smartScreenDisabled = _winOptimizerService.IsSmartScreenDisabled();
        TxtSmartScreenStatus.Text = smartScreenDisabled ? "● 筛选器已禁用 (免弹窗拦截)" : "○ 筛选器已开启";
        TxtSmartScreenStatus.Foreground = smartScreenDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleSmartScreen.Content = smartScreenDisabled ? "开启 SmartScreen" : "关闭 SmartScreen";

        // 3. WSearch
        bool wsearchDisabled = _winOptimizerService.IsWSearchDisabled();
        TxtWSearchStatus.Text = wsearchDisabled ? "● 索引服务已禁用 (零磁盘后台占用)" : "○ 正在运行/自动";
        TxtWSearchStatus.Foreground = wsearchDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleWSearch.Content = wsearchDisabled ? "开启索引服务" : "禁用索引服务";

        // 4. SysMain
        bool sysmainDisabled = _winOptimizerService.IsSysMainDisabled();
        TxtSysMainStatus.Text = sysmainDisabled ? "● 内存预载已禁用 (释放 CPU/内存)" : "○ 正在运行/自动";
        TxtSysMainStatus.Foreground = sysmainDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleSysMain.Content = sysmainDisabled ? "开启预载服务" : "禁用预载服务";

        // 5. 空闲维护
        bool idleMaintenanceDisabled = _winOptimizerService.IsIdleMaintenanceDisabled();
        TxtIdleMaintenanceStatus.Text = idleMaintenanceDisabled ? "● 空闲维护已关闭 (防挂机被抢占)" : "○ 默认自动维护";
        TxtIdleMaintenanceStatus.Foreground = idleMaintenanceDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleIdleMaintenance.Content = idleMaintenanceDisabled ? "恢复自动维护" : "关闭自动维护";

        // 6. HVCI
        bool hvciDisabled = _winOptimizerService.IsHvciDisabled();
        TxtHvciStatus.Text = hvciDisabled ? "● 内核隔离已关闭 (满血裸机原生算力)" : "○ 内核隔离已开启/默认";
        TxtHvciStatus.Foreground = hvciDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleHvci.Content = hvciDisabled ? "开启内核隔离" : "关闭内核隔离";

        // 7. NTFS 8.3 短文件名
        bool ntfs8dot3Disabled = _winOptimizerService.IsNtfs8dot3Disabled();
        TxtNtfs8dot3Status.Text = ntfs8dot3Disabled ? "● 8.3 短文件名已禁用 (零哈希碰撞)" : "○ 启用中/系统按卷";
        TxtNtfs8dot3Status.Foreground = ntfs8dot3Disabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleNtfs8dot3.Content = ntfs8dot3Disabled ? "恢复 8.3 默认" : "禁用 8.3 名称";

        // 8. NTFS 访问时间戳
        bool lastAccessDisabled = _winOptimizerService.IsNtfsLastAccessDisabled();
        TxtNtfsLastAccessStatus.Text = lastAccessDisabled ? "● 时间戳已禁用 (读操作零反写)" : "○ 默认系统托管/开启";
        TxtNtfsLastAccessStatus.Foreground = lastAccessDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleNtfsLastAccess.Content = lastAccessDisabled ? "恢复默认时间戳" : "禁用访问时间戳";

        // 9. NTFS 内存主缓存
        bool ntfsMemIncreased = _winOptimizerService.IsNtfsMemoryUsageIncreased();
        TxtNtfsMemoryStatus.Text = ntfsMemIncreased ? "● 主缓存已扩充 (加大 MFT 命中)" : "○ 系统默认缓存 (1)";
        TxtNtfsMemoryStatus.Foreground = ntfsMemIncreased ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleNtfsMemory.Content = ntfsMemIncreased ? "恢复默认缓存" : "扩充主内存缓存";

        // 10. 内存压缩
        bool memCompDisabled = _winOptimizerService.IsMemoryCompressionDisabled();
        TxtMemoryCompressionStatus.Text = memCompDisabled ? "● 内存压缩已禁用 (杜绝核心挤占)" : "○ 默认开启压缩";
        TxtMemoryCompressionStatus.Foreground = memCompDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleMemoryCompression.Content = memCompDisabled ? "开启内存压缩" : "禁用内存压缩";

        // 11. CPU 编译平权配额
        bool win32PriorityOpt = _winOptimizerService.IsWin32PriorityOptimizedForDev();
        TxtWin32PriorityStatus.Text = win32PriorityOpt ? "● 编译平权长配额 (后台失焦不降权)" : "○ 桌面默认前台高加速";
        TxtWin32PriorityStatus.Foreground = win32PriorityOpt ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleWin32Priority.Content = win32PriorityOpt ? "恢复桌面默认" : "开启编译平权";

        // 12. Localhost TCP 快速回收
        bool tcpPortOpt = _winOptimizerService.IsTcpPortReuseOptimized();
        TxtTcpPortReuseStatus.Text = tcpPortOpt ? "● TCP 端口 30s 回收 (池上限 65534)" : "○ 系统默认 120s~240s";
        TxtTcpPortReuseStatus.Foreground = tcpPortOpt ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleTcpPortReuse.Content = tcpPortOpt ? "恢复默认 TCP" : "开启网络栈调优";

        // 13. WER 错误报告拦截
        bool werDisabled = _winOptimizerService.IsWerDisabled();
        TxtWerStatus.Text = werDisabled ? "● 错误报告已拦截 (调试器秒接管)" : "○ 默认 Watson 遥测";
        TxtWerStatus.Foreground = werDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleWer.Content = werDisabled ? "恢复 WER 报告" : "拦截错误报告";

        // 14. 挂起超时快速判定 (安全基线)
        bool hungAppOpt = _winOptimizerService.IsHungAppTimeoutOptimized();
        TxtHungAppStatus.Text = hungAppOpt ? "● 快速判定 (挂起 1s/服务 2s 等待，安全弹窗)" : "○ 系统默认 (5s 判定/20s 等待)";
        TxtHungAppStatus.Foreground = hungAppOpt ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleHungApp.Content = hungAppOpt ? "恢复默认判定" : "优化挂起超时";

        // 15. 关机静默强杀 (高危选项)
        bool autoEndEnabled = _winOptimizerService.IsAutoEndTasksEnabled();
        TxtAutoEndTasksStatus.Text = autoEndEnabled ? "⚠️ 已开启静默强杀 (关机不询问直接杀，有丢件风险)" : "🛡️ 保持安全关闭 (关机弹出未保存提示)";
        TxtAutoEndTasksStatus.Foreground = autoEndEnabled ? ThemeBrushes.Warning : ThemeBrushes.Success;
        BtnToggleAutoEndTasks.Content = autoEndEnabled ? "关闭静默强杀 (推荐)" : "开启静默强杀";

        // 16. 菜单零悬停延迟
        bool menuZero = _winOptimizerService.IsMenuShowDelayZero();
        TxtMenuShowDelayStatus.Text = menuZero ? "● 菜单延迟 0ms (即点即出极速)" : "○ 系统默认 400ms 迟滞";
        TxtMenuShowDelayStatus.Foreground = menuZero ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleMenuShowDelay.Content = menuZero ? "恢复 400ms 默认" : "菜单延迟归零";

        // 17. 内核常驻物理内存
        bool pagingExecutiveDisabled = _winOptimizerService.IsPagingExecutiveDisabled();
        TxtPagingExecutiveStatus.Text = pagingExecutiveDisabled ? "● 内核与驱动常驻 RAM (零换出卡顿)" : "○ 允许内核分页换出";
        TxtPagingExecutiveStatus.Foreground = pagingExecutiveDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnTogglePagingExecutive.Content = pagingExecutiveDisabled ? "恢复默认分页" : "内核常驻内存";

        // 18. Windows Update 登录免强制重启
        bool autoRebootDisabled = _winOptimizerService.IsAutoRebootDisabled();
        TxtAutoRebootStatus.Text = autoRebootDisabled ? "● 免重启保护已开启 (登录时不强制重启)" : "○ 系统默认强制自动重启";
        TxtAutoRebootStatus.Foreground = autoRebootDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleAutoReboot.Content = autoRebootDisabled ? "恢复默认重启" : "开启免重启保护";

        // 19. GameDVR 与后台录屏开销禁用
        bool gameDvrDisabled = _winOptimizerService.IsGameDvrDisabled();
        TxtGameDvrStatus.Text = gameDvrDisabled ? "● GameDVR 已彻底禁用 (图形钩子释放)" : "○ 默认后台挂钩与录屏";
        TxtGameDvrStatus.Foreground = gameDvrDisabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleGameDvr.Content = gameDvrDisabled ? "开启 GameDVR" : "彻底禁用 GameDVR";
    }

    private void BtnRefreshDevIoStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshDevIoStatus();
        MessageBox.Show("原生开发 CPU 与 I/O 状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnToggleDefenderRealtime_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsDefenderRealtimeDisabled();
        if (!isCurrentlyDisabled)
        {
            var confirm = MessageBox.Show(
                "【高风险安全操作确认】\n\n" +
                "您正在尝试【关闭 Windows Defender 实时监控防护】。\n\n" +
                "⚠️ 安全与性能影响说明：\n" +
                "• 风险：系统将停止对新创建或下载的文件进行实时病毒扫描与恶意代码拦截。\n" +
                "• 收益：彻底消除 MsMpEng 驱动对高频编译产生的万级临时文件的 I/O 拦截，构建性能成倍提升。\n" +
                "• 推荐安全替代方案：若主要为了解决编译缓慢，强烈建议使用下方的【⚡ 一键注入白名单】，仅将代码目录与编译器进程列入豁免，既安全又高效。\n\n" +
                "确定要继续关闭 Defender 实时监控防护吗？",
                "高风险确认：关闭 Defender 实时防护",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var res = _winOptimizerService.SetDefenderRealtimeDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "Defender 实时监控策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleSmartScreen_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsSmartScreenDisabled();
        if (!isCurrentlyDisabled)
        {
            var confirm = MessageBox.Show(
                "【安全策略变更确认】\n\n" +
                "您正在尝试【关闭 Windows SmartScreen 筛选器】。\n\n" +
                "⚠️ 影响说明：\n" +
                "• 风险：从网络下载的未知发布者二进制文件将不再经过微软云端安全信誉校验。\n" +
                "• 收益：消除运行本地自编译可执行程序、自签名工具或 GitHub 开源工具链时的未知发布者拦截弹窗。\n\n" +
                "确定要关闭 SmartScreen 筛选器吗？",
                "确认：关闭 SmartScreen 筛选器",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var res = _winOptimizerService.SetSmartScreenDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "SmartScreen 策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnInjectDevExclusions_Click(object sender, RoutedEventArgs e)
    {
        var res = _winOptimizerService.InjectNativeDevExclusions();
        MessageBox.Show(res.Message, "白名单注入结果", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnToggleWSearch_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsWSearchDisabled();
        var res = _winOptimizerService.SetWSearchDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "Windows Search 状态变更", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnCleanWindowsEdb_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要清理 Windows Search 的索引数据库缓存 (Windows.edb) 吗？\n\n该操作将停止索引服务并删除缓存索引，直接释放历史积累的磁盘空间，不会影响您的源代码文件。",
            "确认清理索引数据库",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            var res = _winOptimizerService.CleanWindowsEdb();
            MessageBox.Show(res.Message, "清理结果", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            RefreshDevIoStatus();
        }
    }

    private void BtnToggleSysMain_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsSysMainDisabled();
        var res = _winOptimizerService.SetSysMainDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "SysMain 状态变更", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleIdleMaintenance_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsIdleMaintenanceDisabled();
        var res = _winOptimizerService.SetIdleMaintenanceDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "自动维护策略变更", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleHvci_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsHvciDisabled();
        if (!isCurrentlyDisabled)
        {
            var confirm = MessageBox.Show(
                "【内核级安全策略变更确认】\n\n" +
                "您正在尝试【关闭基于虚拟化的内核代码完整性 (HVCI / 内存完整性)】。\n\n" +
                "⚠️ 关键影响说明：\n" +
                "• 风险：关闭基于虚拟化的安全内核隔离（VBS）对内核模式驱动签名的执行拦截保护。\n" +
                "• 收益：消除虚拟机管理程序在驱动层与高频系统调用中的校验损耗，释放 5%~15% 的 CPU 裸机吞吐性能。\n" +
                "• 注意：此项修改必须【重启计算机】后方可真正生效。\n\n" +
                "确定要关闭 HVCI 内存完整性吗？",
                "确认：关闭 HVCI 内存完整性",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var res = _winOptimizerService.SetHvciDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "HVCI 策略变更", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleNtfs8dot3_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsNtfs8dot3Disabled();
        var res = _winOptimizerService.SetNtfs8dot3Disabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "NTFS 8.3 短文件名策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleNtfsLastAccess_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsNtfsLastAccessDisabled();
        var res = _winOptimizerService.SetNtfsLastAccessDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "NTFS 最后访问时间戳策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleNtfsMemory_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyIncreased = _winOptimizerService.IsNtfsMemoryUsageIncreased();
        var res = _winOptimizerService.SetNtfsMemoryUsageIncreased(!isCurrentlyIncreased);
        MessageBox.Show(res.Message, "NTFS 内存缓存策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleMemoryCompression_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsMemoryCompressionDisabled();
        var res = _winOptimizerService.SetMemoryCompressionDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "内存压缩策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleWin32Priority_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyOptimized = _winOptimizerService.IsWin32PriorityOptimizedForDev();
        var res = _winOptimizerService.SetWin32PriorityOptimizedForDev(!isCurrentlyOptimized);
        MessageBox.Show(res.Message, "CPU 线程调度配额", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleTcpPortReuse_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyOptimized = _winOptimizerService.IsTcpPortReuseOptimized();
        var res = _winOptimizerService.SetTcpPortReuseOptimized(!isCurrentlyOptimized);
        MessageBox.Show(res.Message, "TCP/IP 协议栈调优", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleWer_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsWerDisabled();
        var res = _winOptimizerService.SetWerDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "Windows 错误报告策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleHungApp_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyOptimized = _winOptimizerService.IsHungAppTimeoutOptimized();
        var res = _winOptimizerService.SetHungAppTimeoutOptimized(!isCurrentlyOptimized);
        MessageBox.Show(res.Message, "进程超时与卡死处理", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleAutoEndTasks_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyEnabled = _winOptimizerService.IsAutoEndTasksEnabled();
        if (!isCurrentlyEnabled)
        {
            var confirm = MessageBox.Show(
                "【⚠️ 高风险数据丢失警告】\n\n" +
                "您正在尝试开启【关机/注销静默强杀未响应任务 (AutoEndTasks=1)】。\n\n" +
                "⚠️ 潜在风险与严重后果：\n" +
                "• 开启后，关机或注销时系统将【不再弹出阻止关机询问窗口】，直接强制终止未响应进程。\n" +
                "• 如果您的 IDE（VS Code / Visual Studio 等）存在未保存源码，或进程处于调试断点挂起状态，将被直接强杀并导致未保存工作彻底丢失！\n" +
                "• 强烈建议日常开发环境保持【关闭】。\n\n" +
                "确定仍要开启静默强杀吗？",
                "高风险警告：开启静默强杀",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var res = _winOptimizerService.SetAutoEndTasksEnabled(!isCurrentlyEnabled);
        MessageBox.Show(res.Message, "关机任务处理策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleMenuShowDelay_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyZero = _winOptimizerService.IsMenuShowDelayZero();
        var res = _winOptimizerService.SetMenuShowDelayZero(!isCurrentlyZero);
        MessageBox.Show(res.Message, "菜单展开悬停延迟", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnTogglePagingExecutive_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsPagingExecutiveDisabled();
        var res = _winOptimizerService.SetPagingExecutiveDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "内核常驻物理内存策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleAutoReboot_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsAutoRebootDisabled();
        var res = _winOptimizerService.SetAutoRebootDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "Windows Update 重启策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    private void BtnToggleGameDvr_Click(object sender, RoutedEventArgs e)
    {
        bool isCurrentlyDisabled = _winOptimizerService.IsGameDvrDisabled();
        var res = _winOptimizerService.SetGameDvrDisabled(!isCurrentlyDisabled);
        MessageBox.Show(res.Message, "GameDVR 策略", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RefreshDevIoStatus();
    }

    #endregion
}
