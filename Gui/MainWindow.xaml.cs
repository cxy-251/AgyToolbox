using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgyToolbox.Services;

namespace AgyToolbox.Gui;

public partial class MainWindow : Window
{
    private readonly WebDropService _webDropService = new();
    private readonly DiskHunterService _diskHunterService = new();
    private readonly SysInfoService _sysInfoService = new();

    public MainWindow()
    {
        InitializeComponent();

        // 绑定快传服务回调
        _webDropService.OnLog += msg =>
        {
            Dispatcher.Invoke(() =>
            {
                ListDropLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                if (ListDropLogs.Items.Count > 100) ListDropLogs.Items.RemoveAt(ListDropLogs.Items.Count - 1);
            });
        };

        _webDropService.OnTextReceived += text =>
        {
            Dispatcher.Invoke(() =>
            {
                ListDropLogs.Items.Insert(0, $"[📩 收到文本] {text}");
            });
        };

        _webDropService.OnFileReceived += (name, len) =>
        {
            Dispatcher.Invoke(() =>
            {
                ListDropLogs.Items.Insert(0, $"[📥 收到文件] {name}");
            });
        };

        // 默认扫描路径
        TxtScanPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // 初始化加载系统信息
        Loaded += async (s, e) => await LoadSysInfoAsync();
    }

    #region WebDrop 快传功能

    private async void BtnToggleDrop_Click(object sender, RoutedEventArgs e)
    {
        if (!_webDropService.IsRunning)
        {
            BtnToggleDrop.IsEnabled = false;
            await _webDropService.StartAsync();

            TxtDropStatus.Text = "● 服务运行中";
            TxtDropStatus.Foreground = System.Windows.Media.Brushes.Green;
            BtnToggleDrop.Content = "停止服务";
            BtnToggleDrop.Background = System.Windows.Media.Brushes.Crimson;

            var ips = _webDropService.GetLocalIPv4Addresses();
            string primaryUrl = ips.Count > 0 ? $"http://{ips[0]}:{_webDropService.CurrentPort}" : $"http://localhost:{_webDropService.CurrentPort}";
            TxtDropUrl.Text = $"访问地址: {primaryUrl} (手机浏览器直接打开)";
            BtnOpenBrowser.IsEnabled = true;
            BtnToggleDrop.IsEnabled = true;
        }
        else
        {
            BtnToggleDrop.IsEnabled = false;
            await _webDropService.StopAsync();

            TxtDropStatus.Text = "● 服务未启动";
            TxtDropStatus.Foreground = System.Windows.Media.Brushes.Gray;
            BtnToggleDrop.Content = "启动快传服务";
            BtnToggleDrop.Background = (System.Windows.Media.Brush)FindResource("SecondaryButton");
            TxtDropUrl.Text = "服务已停止";
            BtnOpenBrowser.IsEnabled = false;
            BtnToggleDrop.IsEnabled = true;
        }
    }

    private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e)
    {
        var ips = _webDropService.GetLocalIPv4Addresses();
        string url = ips.Count > 0 ? $"http://{ips[0]}:{_webDropService.CurrentPort}" : $"http://localhost:{_webDropService.CurrentPort}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void BtnOpenReceived_Click(object sender, RoutedEventArgs e) => _webDropService.OpenReceivedFolder();
    private void BtnOpenShared_Click(object sender, RoutedEventArgs e) => _webDropService.OpenSharedFolder();

    #endregion

    #region DiskHunter 大文件猎手

    private void BtnScanUserHome_Click(object sender, RoutedEventArgs e)
    {
        TxtScanPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private async void BtnStartScan_Click(object sender, RoutedEventArgs e)
    {
        string path = TxtScanPath.Text.Trim();
        if (!Directory.Exists(path))
        {
            MessageBox.Show("指定目录不存在，请检查路径。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnStartScan.IsEnabled = false;
        TxtScanStatus.Text = "正在多线程枚举全盘文件中，请稍候...";

        var sw = Stopwatch.StartNew();
        var files = await _diskHunterService.ScanTopFilesAsync(path, 20, count =>
        {
            Dispatcher.Invoke(() => TxtScanStatus.Text = $"已检索 {count} 个文件...");
        });
        sw.Stop();

        GridDiskFiles.ItemsSource = files;
        TxtScanStatus.Text = $"扫描完成！耗时 {sw.ElapsedMilliseconds} ms，已展示 Top {files.Count} 大文件。";
        BtnStartScan.IsEnabled = true;
    }

    private void GridDiskFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LocateCurrentSelectedFile();
    }

    private void BtnLocateFile_Click(object sender, RoutedEventArgs e)
    {
        LocateCurrentSelectedFile();
    }

    private void LocateCurrentSelectedFile()
    {
        if (GridDiskFiles.SelectedItem is FileEntry entry)
        {
            _diskHunterService.RevealInExplorer(entry.FullPath);
        }
    }

    #endregion

    #region SysInfo 系统与网络诊断

    private async void BtnRefreshSysInfo_Click(object sender, RoutedEventArgs e)
    {
        await LoadSysInfoAsync();
    }

    private async Task LoadSysInfoAsync()
    {
        var basic = _sysInfoService.GetBasicInfo();
        TxtMachineInfo.Text = $"计算机名: {basic.MachineName} | 操作系统: {basic.OSDescription} ({basic.Architecture})";
        TxtCpuUptime.Text = $"逻辑核心数: {basic.CoreCount} 核心 | 开机运行时长: {basic.Uptime:d'天 'hh'小时 'mm'分'}";

        ListDisks.ItemsSource = _sysInfoService.GetDisks();
        ListNets.ItemsSource = _sysInfoService.GetActiveNetworks();

        ListPings.ItemsSource = new[] { new PingResultItem("正在测速中...", false, 0, "") };
        var pings = await _sysInfoService.TestPingAsync();
        ListPings.ItemsSource = pings;
    }

    #endregion
}
