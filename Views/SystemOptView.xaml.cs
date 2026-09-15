using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public class PathDisplayItem
{
    public string Path { get; set; } = "";
    public bool Exists { get; set; }
    public string ExistsText => Exists ? "✅ 路径有效" : "🚫 幽灵死路径 (不存在)";
    public System.Windows.Media.Brush StatusBrush => Exists
        ? System.Windows.Media.Brushes.DarkGreen
        : System.Windows.Media.Brushes.Red;
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
        RefreshExplorerSettings();
    }

    #region Windows 自动更新彻底控制

    private void RefreshUpdateStatus()
    {
        bool disabled = _windowsUpdateService.IsUpdateDisabled();
        TxtUpdateStatus.Text = disabled ? "● 自动更新已彻底关闭并锁定" : "○ 自动更新处于开启状态";
        TxtUpdateStatus.Foreground = disabled
            ? System.Windows.Media.Brushes.DarkGreen
            : System.Windows.Media.Brushes.DarkOrange;
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
            "3. 停止并禁用更新服务 (wuauserv) 及更新唤醒医生 (WaaSMedicSvc，防止半夜偷偷复活)。\n\n" +
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

    #region 资源管理器开荒与卓越性能

    private void RefreshExplorerSettings()
    {
        ChkShowFileExt.IsChecked = _winOptimizerService.IsFileExtVisible();
        ChkShowHiddenFiles.IsChecked = _winOptimizerService.IsHiddenFilesVisible();
        ChkOpenThisPc.IsChecked = _winOptimizerService.IsOpenThisPcDefault();
    }

    private void BtnRefreshExplorerSettings_Click(object sender, RoutedEventArgs e)
    {
        RefreshExplorerSettings();
        MessageBox.Show("资源管理器与系统设置状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void ChkOpenThisPc_Click(object sender, RoutedEventArgs e)
    {
        bool thisPc = ChkOpenThisPc.IsChecked == true;
        var (ok, msg) = _winOptimizerService.SetOpenThisPcDefault(thisPc);
        if (!ok) MessageBox.Show(msg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void BtnEnableUltimatePerf_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winOptimizerService.EnableUltimatePerformance();
        MessageBox.Show(msg, ok ? "激活成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region Windows 11/10 体验去魅优化

    private void RefreshOptimizerStatus()
    {
        bool classic = _winOptimizerService.IsClassicContextMenuEnabled();
        TxtClassicMenuStatus.Text = classic ? "[已开启 Win10 经典菜单]" : "[当前为 Win11 折叠菜单]";
        TxtClassicMenuStatus.Foreground = classic ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkOrange;

        bool noBing = _winOptimizerService.IsBingSearchDisabled();
        TxtBingStatus.Text = noBing ? "[已关闭必应搜索广告]" : "[当前保留必应搜索与热搜]";
        TxtBingStatus.Foreground = noBing ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkOrange;

        bool noTele = _winOptimizerService.IsTelemetryDisabled();
        TxtTelemetryStatus.Text = noTele ? "[已禁用个性化遥测广告]" : "[当前为默认遥测]";
        TxtTelemetryStatus.Foreground = noTele ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkOrange;

        bool noSticky = _winOptimizerService.IsStickyKeysPromptDisabled();
        TxtStickyStatus.Text = noSticky ? "[已禁用 5 次 Shift 弹窗]" : "[当前为系统默认弹窗]";
        TxtStickyStatus.Foreground = noSticky ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkOrange;

        bool clipHist = _winOptimizerService.IsClipboardHistoryEnabled();
        TxtClipboardStatus.Text = clipHist ? "[已开启 Win+V 剪贴板历史]" : "[当前未开启剪贴板历史]";
        TxtClipboardStatus.Foreground = clipHist ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkOrange;

        bool devMode = _winOptimizerService.IsDevModeAndLongPathsEnabled();
        TxtDevModeStatus.Text = devMode ? "[已开启开发者模式与长路径]" : "[当前为标准用户限制]";
        TxtDevModeStatus.Foreground = devMode ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkOrange;
    }

    private void BtnToggleClassicMenu_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsClassicContextMenuEnabled();
        var (ok, msg) = _winOptimizerService.SetClassicContextMenu(!current);
        RefreshOptimizerStatus();
        MessageBox.Show(msg, "设置结果", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
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

    private void BtnRestartExplorer_Click(object sender, RoutedEventArgs e)
    {
        _winOptimizerService.RestartExplorer();
        MessageBox.Show("已向资源管理器发送重启指令，桌面与任务栏将在 1~2 秒内刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region PATH 环境变量管理

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
        MessageBox.Show("已重新读取当前 PATH 列表！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnRemoveDeadPaths_Click(object sender, RoutedEventArgs e)
    {
        var deadList = _pathItems.Where(p => !p.Exists).ToList();
        if (deadList.Count == 0)
        {
            MessageBox.Show("太棒了！当前 PATH 中未检测到任何失效死路径。", "检测提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"检测到 {deadList.Count} 条已不存在的幽灵死路径，是否从列表中清除？\n注意：清除后需点击【保存修改】才会真正写入注册表。", "确认清理", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            foreach (var item in deadList)
            {
                _pathItems.Remove(item);
            }
        }
    }

    private void BtnMovePathUp_Click(object sender, RoutedEventArgs e)
    {
        int idx = GridPathEntries.SelectedIndex;
        if (idx > 0)
        {
            _pathItems.Move(idx, idx - 1);
            GridPathEntries.SelectedIndex = idx - 1;
        }
    }

    private void BtnMovePathDown_Click(object sender, RoutedEventArgs e)
    {
        int idx = GridPathEntries.SelectedIndex;
        if (idx >= 0 && idx < _pathItems.Count - 1)
        {
            _pathItems.Move(idx, idx + 1);
            GridPathEntries.SelectedIndex = idx + 1;
        }
    }

    private void BtnAddPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择要加入 PATH 环境变量的文件夹"
        };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            if (_pathItems.Any(p => string.Equals(p.Path, dialog.FolderName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该路径已存在于列表中，无需重复添加！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _pathItems.Insert(0, new PathDisplayItem
            {
                Path = dialog.FolderName,
                Exists = true
            });
            GridPathEntries.SelectedIndex = 0;
        }
    }

    private void BtnDeleteSelectedPath_Click(object sender, RoutedEventArgs e)
    {
        if (GridPathEntries.SelectedItem is PathDisplayItem item)
        {
            _pathItems.Remove(item);
        }
        else
        {
            MessageBox.Show("请先在表格中点击选中要删除的路径！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnSavePath_Click(object sender, RoutedEventArgs e)
    {
        bool isSystem = RbSysPath.IsChecked == true;
        var (ok, msg) = _envManagerService.SavePathEntries(isSystem, _pathItems.Select(p => p.Path));
        if (ok)
        {
            MessageBox.Show(msg, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPathEntries(isSystem);
        }
        else
        {
            MessageBox.Show(msg, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}
