using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgyToolbox.Helpers;
using AgyToolbox.Services;

namespace AgyToolbox.Gui;

public partial class MainWindow : Window
{
    private readonly WebDropService _webDropService = new();
    private readonly DiskHunterService _diskHunterService = new();
    private readonly SysInfoService _sysInfoService = new();
    private readonly WinTricksService _winTricksService = new();

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

    private static readonly System.Windows.Media.Brush PrimaryBrush =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
    private static readonly System.Windows.Media.Brush DangerBrush =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));

    #region WebDrop 快传功能

    private async void BtnToggleDrop_Click(object sender, RoutedEventArgs e)
    {
        if (!_webDropService.IsRunning)
        {
            BtnToggleDrop.IsEnabled = false;
            try
            {
                await _webDropService.StartAsync();

                TxtDropStatus.Text = "● 服务运行中";
                TxtDropStatus.Foreground = System.Windows.Media.Brushes.Green;
                BtnToggleDrop.Content = "停止服务";
                BtnToggleDrop.Background = DangerBrush;

                var ips = _webDropService.GetLocalIPv4Addresses();
                string primaryUrl = ips.Count > 0 ? $"http://{ips[0]}:{_webDropService.CurrentPort}" : $"http://localhost:{_webDropService.CurrentPort}";
                TxtDropUrl.Text = $"访问地址: {primaryUrl}";
                TxtQrTargetUrl.Text = primaryUrl;

                // 生成并在界面展示二维码
                ImgQrCode.Source = QrCodeHelper.GenerateQrBitmap(primaryUrl, 8);
                BorderQrCard.Visibility = Visibility.Visible;

                BtnOpenBrowser.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnToggleDrop.IsEnabled = true;
            }
        }
        else
        {
            BtnToggleDrop.IsEnabled = false;
            try
            {
                await _webDropService.StopAsync();

                TxtDropStatus.Text = "● 服务未启动";
                TxtDropStatus.Foreground = System.Windows.Media.Brushes.Gray;
                BtnToggleDrop.Content = "启动快传服务";
                BtnToggleDrop.Background = PrimaryBrush;
                TxtDropUrl.Text = "服务已停止";

                // 隐藏二维码卡片并清空图片
                BorderQrCard.Visibility = Visibility.Collapsed;
                ImgQrCode.Source = null;

                BtnOpenBrowser.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnToggleDrop.IsEnabled = true;
            }
        }
    }

    private void BtnCopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtQrTargetUrl.Text) && TxtQrTargetUrl.Text != "-")
        {
            Clipboard.SetText(TxtQrTargetUrl.Text);
            MessageBox.Show("访问链接已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

    #region WinTricks Windows 绝活透视镜

    private void BtnGetWifi_Click(object sender, RoutedEventArgs e)
    {
        var wifis = _winTricksService.GetSavedWifiPasswords();
        GridWifi.ItemsSource = wifis;
        GridWifi.Visibility = Visibility.Visible;
    }

    private void BtnCheckPort_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtPortToKill.Text.Trim(), out int port))
        {
            var occs = _winTricksService.FindPortOccupants(port);
            if (occs.Count == 0)
            {
                TxtPortResult.Text = $"✓ 端口 {port} 空闲未被占用";
                TxtPortResult.Foreground = System.Windows.Media.Brushes.Green;
                PanelKillPort.Visibility = Visibility.Collapsed;
            }
            else
            {
                var first = occs[0];
                TxtPortResult.Text = $"✗ 端口正在被占用！";
                TxtPortResult.Foreground = System.Windows.Media.Brushes.Red;
                TxtPortOccupantDesc.Text = $"占用进程: {first.ProcessName} (PID: {first.Pid})";
                PanelKillPort.Tag = first.Pid;
                PanelKillPort.Visibility = Visibility.Visible;
            }
        }
        else
        {
            MessageBox.Show("请输入正确的数字端口号。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnKillPortProcess_Click(object sender, RoutedEventArgs e)
    {
        if (PanelKillPort.Tag is int pid)
        {
            if (_winTricksService.KillProcessByPid(pid))
            {
                MessageBox.Show($"已成功终止 PID: {pid} 进程，端口已释放！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnCheckPort_Click(sender, e);
            }
            else
            {
                MessageBox.Show("终止进程失败，可能需要管理员权限。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnBatteryReport_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg, path) = _winTricksService.GenerateBatteryReport();
        MessageBox.Show(msg, success ? "成功" : "提示", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnReliability_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenReliabilityMonitor();
    private void BtnGodMode_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenGodMode();
    private void BtnMrt_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenMrt();
    private void BtnDxDiag_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenDxDiag();
    private void BtnResmon_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenResMon();

    #endregion
}
