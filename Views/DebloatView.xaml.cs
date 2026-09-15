using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
    public bool IsInstalled { get; set; }
    public string StatusText => IsInstalled ? "● 已安装" : "○ 未安装/已卸载";
    public System.Windows.Media.Brush StatusBrush => IsInstalled
        ? System.Windows.Media.Brushes.Red
        : System.Windows.Media.Brushes.DarkGreen;
}

public partial class DebloatView : UserControl
{
    private readonly UwpDebloatService _uwpDebloatService = new();
    private readonly EdgeDebloatService _edgeDebloatService = new();
    private readonly OneDriveDebloatService _oneDriveDebloatService = new();
    private readonly ObservableCollection<UwpAppDisplayItem> _uwpItems = new();

    public DebloatView()
    {
        InitializeComponent();

        GridUwpApps.ItemsSource = _uwpItems;
        _ = LoadUwpAppsAsync();
        RefreshBrowserStatus();
        RefreshOneDriveStatus();
    }

    #region UWP 预装应用精简

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

    #region Edge 彻底卸载与 Chrome 替换

    private void RefreshBrowserStatus()
    {
        bool edge = _edgeDebloatService.IsEdgeInstalled();
        bool chrome = _edgeDebloatService.IsChromeInstalled();

        string edgeTxt = edge ? "Edge: 已安装" : "Edge: 已干净卸载";
        string chromeTxt = chrome ? "Chrome: 已安装" : "Chrome: 未安装";

        TxtBrowserStatus.Text = $"[{edgeTxt} | {chromeTxt}]";
        TxtBrowserStatus.Foreground = (!edge && chrome)
            ? System.Windows.Media.Brushes.DarkGreen
            : System.Windows.Media.Brushes.DarkOrange;

        BtnUninstallEdge.IsEnabled = edge;
        BtnInstallChrome.IsEnabled = !chrome;
    }

    private void BtnRefreshBrowserStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshBrowserStatus();
        MessageBox.Show("浏览器安装状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnUninstallEdge_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要彻底卸载 Microsoft Edge 浏览器主体吗？\n\n" +
            "【专业安全保证】：\n" +
            "1. 本工具仅卸载 Edge 浏览器主体，将严格保留核心 WebView2 运行时，确保微信、钉钉等第三方软件不会白屏崩溃。\n" +
            "2. 卸载完成后会自动写入注册表策略，阻止 Windows Update 偷偷重新安装 Edge。\n\n" +
            "是否继续？",
            "确认卸载 Edge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        BtnUninstallEdge.IsEnabled = false;
        try
        {
            var (ok, msg) = await _edgeDebloatService.UninstallEdgeAsync();
            RefreshBrowserStatus();
            MessageBox.Show(msg, ok ? "操作完成" : "卸载提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
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
            var (ok, msg) = await _edgeDebloatService.InstallChromeAsync(line =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtBrowserStatus.Text = $"[正在安装 Chrome...]";
                });
            });

            RefreshBrowserStatus();
            if (ok)
            {
                var ask = MessageBox.Show("Google Chrome 已成功安装！是否立即打开系统设置将其设为默认浏览器？", "安装成功", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (ask == MessageBoxResult.Yes)
                {
                    _edgeDebloatService.OpenDefaultAppsSettings();
                }
            }
            else
            {
                MessageBox.Show(msg, "安装提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装 Chrome 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshBrowserStatus();
        }
    }

    private void BtnSetDefaultBrowser_Click(object sender, RoutedEventArgs e)
    {
        _edgeDebloatService.OpenDefaultAppsSettings();
    }

    #endregion

    #region OneDrive 彻底卸载与去盘符绑定

    private void RefreshOneDriveStatus()
    {
        bool installed = _oneDriveDebloatService.IsOneDriveInstalled();
        TxtOneDriveStatus.Text = installed ? "● 已安装 (常驻后台/可能在同步)" : "○ 未安装 / 已彻底卸载";
        TxtOneDriveStatus.Foreground = installed
            ? System.Windows.Media.Brushes.Red
            : System.Windows.Media.Brushes.DarkGreen;
        BtnUninstallOneDrive.IsEnabled = installed;
    }

    private void BtnRefreshOneDriveStatus_Click(object sender, RoutedEventArgs e)
    {
        RefreshOneDriveStatus();
        MessageBox.Show("OneDrive 安装状态已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

    #region winget 辅助操作

    private void BtnCopyWingetUpgrade_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText("winget upgrade --all --include-unknown");
        MessageBox.Show("已复制全盘更新命令到剪贴板！\n在终端中按下回车即可全盘静默更新所有软件。", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
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
