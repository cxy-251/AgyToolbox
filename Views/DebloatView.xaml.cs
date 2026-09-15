using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public class UwpAppDisplayItem
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PackagePrefix { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string DebloatAdvice { get; set; } = "";
    public string OpenSourceAlternative { get; set; } = "";
    public bool IsInstalled { get; set; }
    public string StatusText => IsInstalled ? "● 已安装" : "○ 未安装/已卸载";
    public Brush StatusBrush => IsInstalled ? Brushes.Red : Brushes.DarkGreen;
}

public class OemAppDisplayItem
{
    public string Name { get; set; } = "";
    public string Vendor { get; set; } = "";
    public string Description { get; set; } = "";
    public string DebloatAdvice { get; set; } = "";
    public bool IsDetected { get; set; }
    public string StatusText => IsDetected ? "⚠️ 正在运行" : "✓ 未检出";
    public Brush StatusBrush => IsDetected ? Brushes.Red : Brushes.DarkGreen;
}

public partial class DebloatView : UserControl
{
    private readonly UwpDebloatService _uwpDebloatService = new();
    private readonly EdgeDebloatService _edgeDebloatService = new();
    private readonly OneDriveDebloatService _oneDriveDebloatService = new();
    private readonly DebloatExtraService _debloatExtraService = new();
    private readonly WinOptimizerService _winOptimizerService = new();

    private readonly ObservableCollection<UwpAppDisplayItem> _uwpItems = new();
    private readonly ObservableCollection<OemAppDisplayItem> _oemItems = new();
    private readonly ObservableCollection<StartupItemInfo> _startupItems = new();

    public DebloatView()
    {
        InitializeComponent();

        GridUwpApps.ItemsSource = _uwpItems;
        GridOemApps.ItemsSource = _oemItems;
        GridStartupItems.ItemsSource = _startupItems;

        _ = LoadUwpAppsAsync();
        LoadOemApps();
        LoadStartupItems();
        RefreshSilentAppStatus();
        RefreshBrowserStatus();
        RefreshOneDriveStatus();
    }

    #region 1. UWP 预装应用精简

    private async Task LoadUwpAppsAsync()
    {
        _uwpItems.Clear();
        var apps = await _uwpDebloatService.ScanInstalledAppsAsync();
        foreach (var app in apps)
        {
            _uwpItems.Add(new UwpAppDisplayItem
            {
                Key = app.Key,
                DisplayName = app.DisplayName,
                PackagePrefix = app.PackagePrefix,
                Category = app.Category,
                Description = app.Description,
                DebloatAdvice = app.DebloatAdvice,
                OpenSourceAlternative = app.OpenSourceAlternative,
                IsInstalled = app.IsInstalled
            });
        }
    }

    private async void BtnScanUwp_Click(object sender, RoutedEventArgs e)
    {
        await LoadUwpAppsAsync();
        MessageBox.Show("预装应用状态扫描完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnUninstallSingleUwp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string prefix)
        {
            var target = _uwpItems.FirstOrDefault(u => u.PackagePrefix == prefix);
            string appName = target?.DisplayName ?? prefix;

            var confirm = MessageBox.Show(
                $"确定要卸载预装应用【{appName}】吗？\n\n卸载操作将同时清理当前登录账户及新用户预配模板，卸载后可通过微软应用商店或 winget 重新装回。",
                "确认卸载",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            btn.IsEnabled = false;
            var (ok, msg) = await _uwpDebloatService.UninstallPackageAsync(prefix);

            if (ok && target != null)
            {
                target.IsInstalled = false;
                GridUwpApps.Items.Refresh();
            }

            MessageBox.Show(msg, ok ? "成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            btn.IsEnabled = true;
        }
    }

    #endregion

    #region 2. OEM 品牌机毒瘤排查

    private void LoadOemApps()
    {
        _oemItems.Clear();
        var list = _debloatExtraService.DetectOemApps();
        foreach (var item in list)
        {
            _oemItems.Add(new OemAppDisplayItem
            {
                Name = item.Name,
                Vendor = item.Vendor,
                Description = item.Description,
                DebloatAdvice = item.DebloatAdvice,
                IsDetected = item.IsDetected
            });
        }
    }

    private void BtnScanOem_Click(object sender, RoutedEventArgs e)
    {
        LoadOemApps();
        MessageBox.Show("OEM 预装应用扫描完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region 3. 锁死 Win11 后台静默安装推广

    private void RefreshSilentAppStatus()
    {
        bool disabled = _winOptimizerService.IsSilentAppInstallDisabled();
        TxtSilentAppStatus.Text = disabled ? "[已彻底锁死静默推广策略]" : "[当前允许后台静默下载推广]";
        TxtSilentAppStatus.Foreground = disabled ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnToggleSilentApps_Click(object sender, RoutedEventArgs e)
    {
        bool current = _winOptimizerService.IsSilentAppInstallDisabled();
        var (ok, msg) = _winOptimizerService.SetSilentAppInstallDisabled(!current);
        RefreshSilentAppStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 4. Edge 彻底卸载与 Chrome 替换

    private void RefreshBrowserStatus()
    {
        bool edgeInstalled = _edgeDebloatService.IsEdgeInstalled();
        bool chromeInstalled = _edgeDebloatService.IsChromeInstalled();

        TxtBrowserStatus.Text = $"Edge: {(edgeInstalled ? "已安装" : "已卸载")} | Chrome: {(chromeInstalled ? "已安装" : "未安装")}";
        BtnUninstallEdge.IsEnabled = edgeInstalled;
    }

    private void BtnRefreshBrowserStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshBrowserStatus();
        MessageBox.Show("浏览器状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnUninstallEdge_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要彻底卸载 Microsoft Edge 浏览器吗？\n\n" +
            "【卸载机制说明】：\n" +
            "1. 仅移除 Edge 浏览器主体，严格保留底层的 WebView2 共享运行库，绝不影响微信、钉钉等客户端！\n" +
            "2. 写入注册表阻止 Windows Update 下周重新静默推送 Edge。\n\n" +
            "卸载前请确保已安装 Chrome 或其它替代浏览器。是否继续？",
            "确认彻底卸载 Edge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        BtnUninstallEdge.IsEnabled = false;
        try
        {
            var (ok, msg) = await _edgeDebloatService.UninstallEdgeAsync();
            RefreshBrowserStatus();
            MessageBox.Show(msg, ok ? "卸载完成" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"卸载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshBrowserStatus();
        }
    }

    private async void BtnInstallChrome_Click(object sender, RoutedEventArgs e)
    {
        BtnInstallChrome.IsEnabled = false;
        try
        {
            var (ok, msg) = await _edgeDebloatService.InstallChromeAsync(_ => { });
            RefreshBrowserStatus();
            MessageBox.Show(msg, ok ? "安装完成" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnInstallChrome.IsEnabled = true;
        }
    }

    private void BtnSetDefaultBrowser_Click(object sender, RoutedEventArgs e)
    {
        _edgeDebloatService.OpenDefaultAppsSettings();
    }

    #endregion

    #region 5. OneDrive 深度卸载

    private void RefreshOneDriveStatus()
    {
        bool installed = _oneDriveDebloatService.IsOneDriveInstalled();
        TxtOneDriveStatus.Text = installed ? "● 正在后台运行 / 已安装" : "○ 未运行 / 已彻底清除";
        TxtOneDriveStatus.Foreground = installed ? Brushes.Red : Brushes.DarkGreen;
        BtnUninstallOneDrive.IsEnabled = installed;
    }

    private void BtnRefreshOneDriveStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshOneDriveStatus();
        MessageBox.Show("OneDrive 状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnUninstallOneDrive_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要彻底卸载微软 OneDrive 并清除资源管理器侧边栏图标吗？\n\n" +
            "【操作内容】：\n" +
            "1. 结束 OneDrive.exe 进程并调用官方静默卸载器；\n" +
            "2. 清除 Windows 开机自启动项；\n" +
            "3. 清理注册表 CLSID 命名空间，彻底抹去资源管理器左侧栏无法删除的蓝云图标。\n\n" +
            "注意：你存放在本地已同步目录的文件不会被删除。\n是否继续？",
            "确认卸载 OneDrive",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        BtnUninstallOneDrive.IsEnabled = false;
        try
        {
            var (ok, msg) = await _oneDriveDebloatService.UninstallOneDriveAsync();
            RefreshOneDriveStatus();
            MessageBox.Show(msg, ok ? "卸载完成" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"卸载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshOneDriveStatus();
        }
    }

    #endregion

    #region 6. 开机自启动项管理

    private void LoadStartupItems()
    {
        _startupItems.Clear();
        var items = _debloatExtraService.GetStartupItems();
        foreach (var item in items)
        {
            _startupItems.Add(item);
        }
    }

    private void BtnReloadStartup_Click(object sender, RoutedEventArgs e)
    {
        LoadStartupItems();
        MessageBox.Show("开机自启动项已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnDeleteStartupItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is StartupItemInfo item)
        {
            var confirm = MessageBox.Show($"确定要删除自启项【{item.Name}】吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            if (_debloatExtraService.RemoveStartupItem(item.Name, item.Scope))
            {
                _startupItems.Remove(item);
                MessageBox.Show("自启项已成功移除！下次开机不再自启动。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("删除失败，可能需要管理员权限。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    #endregion

    #region 7. winget 辅助操作

    private void BtnCopyWingetCheck_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText("winget upgrade");
        MessageBox.Show("已复制【winget upgrade】到剪贴板！\n此命令仅用于扫描列出可升级软件列表，不会做任何修改，安全可靠。", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyWingetPin_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText("winget pin add --id Python.Python.3.10");
        MessageBox.Show("已复制【锁定版本命令】到剪贴板！\n将后面的 ID 改为需要锁定的软件包，可防止开发环境被自动升破大版本。", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyWingetUpgradeSingle_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText("winget upgrade --id 7zip.7zip");
        MessageBox.Show("已复制【单包升级命令】到剪贴板！\n将 ID 改为实际要更新的软件，可控可追溯。", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet)
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"已复制命令到剪贴板：\n{snippet}", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion
}
