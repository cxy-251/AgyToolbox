using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgyToolbox.Helpers;
using AgyToolbox.Services;

namespace AgyToolbox.Gui;

public class PathDisplayItem
{
    public string Path { get; set; } = "";
    public bool Exists { get; set; }
    public string ExistsText => Exists ? "✅ 路径有效" : "🚫 幽灵死路径 (不存在)";
    public System.Windows.Media.Brush StatusBrush => Exists
        ? System.Windows.Media.Brushes.DarkGreen
        : System.Windows.Media.Brushes.Red;
}

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

public partial class MainWindow : Window
{
    private readonly WebDropService _webDropService = new();
    private readonly DiskHunterService _diskHunterService = new();
    private readonly SysInfoService _sysInfoService = new();
    private readonly WinTricksService _winTricksService = new();
    private readonly EnvManagerService _envManagerService = new();
    private readonly WinOptimizerService _winOptimizerService = new();
    private readonly DevToolsService _devToolsService = new();
    private readonly UwpDebloatService _uwpDebloatService = new();
    private readonly ObservableCollection<PathDisplayItem> _pathItems = new();
    private readonly ObservableCollection<UwpAppDisplayItem> _uwpItems = new();

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

        // 初始化加载系统信息与默认 DNS 列表
        GridDns.ItemsSource = _winTricksService.GetDefaultDnsList();

        // 初始化预装精简
        GridUwpApps.ItemsSource = _uwpItems;
        _ = LoadUwpAppsAsync();

        // 初始化环境变量与系统优化
        GridPathEntries.ItemsSource = _pathItems;
        LoadPathEntries(false);
        RefreshOptimizerStatus();
        BtnFillCurrentTs_Click(this, new RoutedEventArgs());
        BtnGenGuid_Click(this, new RoutedEventArgs());

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
    private void BtnEditHosts_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenHostsFile();
    private void BtnPsr_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenStepsRecorder();

    private void BtnFlushDns_Click(object sender, RoutedEventArgs e)
    {
        var msg = _winTricksService.FlushDns();
        MessageBox.Show(msg, "DNS 解析缓存", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnTestDns_Click(object sender, RoutedEventArgs e)
    {
        var targetBtn = sender as Button;
        if (targetBtn != null) targetBtn.IsEnabled = false;
        try
        {
            var results = await _winTricksService.TestPublicDnsAsync();
            GridDns.ItemsSource = results;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"DNS 测速失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (targetBtn != null) targetBtn.IsEnabled = true;
        }
    }

    private void GridDns_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (GridDns.SelectedItem is DnsPingResult item)
        {
            Clipboard.SetText(item.Ip);
            MessageBox.Show($"已复制 DNS IP 地址 [{item.Ip}] ({item.Provider}) 到剪贴板！\n可直接粘贴到网络适配器 IPv4 属性中使用。", "DNS 复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnApplyCondarc_Click(object sender, RoutedEventArgs e)
    {
        var msg = _winTricksService.ApplyModernCondarc();
        MessageBox.Show(msg, "Conda 换源", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyCondarc_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_winTricksService.GetModernCondarcContent());
        MessageBox.Show("现代清华源 .condarc 配置文本已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyPip_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtPipCmd.Text);
        MessageBox.Show("Pip 换源命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyNpm_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtNpmCmd.Text);
        MessageBox.Show("Npm 最新镜像源命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyNuget_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtNugetCmd.Text);
        MessageBox.Show("NuGet 华为云镜像源命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region 网络路由诊断 (NetRoute)

    private CancellationTokenSource? _diagCts;

    private async void RunDiagnostic(string cmd, string args)
    {
        _diagCts?.Cancel();
        _diagCts = new CancellationTokenSource();

        BtnStopDiag.IsEnabled = true;
        TxtDiagConsole.AppendText($"\n>>> [{DateTime.Now:HH:mm:ss}] 启动诊断: {cmd} {args}\n");
        TxtDiagConsole.ScrollToEnd();

        try
        {
            await _winTricksService.RunNetworkDiagnosticAsync(cmd, args, line =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtDiagConsole.AppendText(line + "\n");
                    TxtDiagConsole.ScrollToEnd();
                });
            }, _diagCts.Token);
        }
        catch (Exception ex)
        {
            TxtDiagConsole.AppendText($"[诊断异常]: {ex.Message}\n");
        }
        finally
        {
            BtnStopDiag.IsEnabled = false;
        }
    }

    private void BtnTracertFast_Click(object sender, RoutedEventArgs e)
    {
        string host = string.IsNullOrWhiteSpace(TxtRouteHost.Text) ? "1.1.1.1" : TxtRouteHost.Text.Trim();
        RunDiagnostic("tracert", $"-d -h 20 {host}");
    }

    private void BtnPathping_Click(object sender, RoutedEventArgs e)
    {
        string host = string.IsNullOrWhiteSpace(TxtRouteHost.Text) ? "1.1.1.1" : TxtRouteHost.Text.Trim();
        RunDiagnostic("pathping", $"-n -q 2 -p 250 -h 15 {host}");
    }

    private void BtnRoutePrint_Click(object sender, RoutedEventArgs e)
    {
        RunDiagnostic("route", "print -4");
    }

    private void BtnArp_Click(object sender, RoutedEventArgs e)
    {
        RunDiagnostic("arp", "-a");
    }

    private void BtnStopDiag_Click(object sender, RoutedEventArgs e)
    {
        _diagCts?.Cancel();
        BtnStopDiag.IsEnabled = false;
        TxtDiagConsole.AppendText("[已请求终止诊断]\n");
    }

    private void BtnClearDiagLog_Click(object sender, RoutedEventArgs e)
    {
        TxtDiagConsole.Text = "[网络诊断控制台已清空就绪]\n";
    }

    #endregion

    #region 环境变量与系统优化 (Env & Optimizer)

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
        if (RbSysPath == null) return;
        LoadPathEntries(RbSysPath.IsChecked == true);
    }

    private void BtnReloadPath_Click(object sender, RoutedEventArgs e)
    {
        LoadPathEntries(RbSysPath.IsChecked == true);
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

    private void BtnRestartExplorer_Click(object sender, RoutedEventArgs e)
    {
        _winOptimizerService.RestartExplorer();
        MessageBox.Show("已向资源管理器发送重启指令，桌面与任务栏将在 1~2 秒内刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region 开发者轻量工具箱 (DevTools)

    private async void ProcessHashFile(string path)
    {
        if (!System.IO.File.Exists(path)) return;
        TxtHashFilePath.Text = path;
        TxtHashMd5.Text = "计算中...";
        TxtHashSha1.Text = "计算中...";
        TxtHashSha256.Text = "计算中...";
        TxtHashSha512.Text = "计算中...";
        TxtHashCompareResult.Text = "";

        try
        {
            var res = await _devToolsService.ComputeFileHashesAsync(path);
            TxtHashMd5.Text = res.Md5;
            TxtHashSha1.Text = res.Sha1;
            TxtHashSha256.Text = res.Sha256;
            TxtHashSha512.Text = res.Sha512;
            CompareHash();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"计算哈希失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSelectHashFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择要计算哈希指纹的文件"
        };
        if (dialog.ShowDialog() == true)
        {
            ProcessHashFile(dialog.FileName);
        }
    }

    private void TxtHashFilePath_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void TxtHashFilePath_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                ProcessHashFile(files[0]);
            }
        }
    }

    private void TxtExpectedHash_TextChanged(object sender, TextChangedEventArgs e)
    {
        CompareHash();
    }

    private void CompareHash()
    {
        string expected = TxtExpectedHash.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(expected))
        {
            TxtHashCompareResult.Text = "";
            return;
        }

        if (expected == TxtHashMd5.Text || expected == TxtHashSha1.Text ||
            expected == TxtHashSha256.Text || expected == TxtHashSha512.Text)
        {
            TxtHashCompareResult.Text = "✅ 哈希完全匹配！文件完整无篡改。";
            TxtHashCompareResult.Foreground = System.Windows.Media.Brushes.DarkGreen;
        }
        else
        {
            TxtHashCompareResult.Text = "❌ 哈希不匹配！文件可能已损坏或被修改。";
            TxtHashCompareResult.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void BtnCopyMd5_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashMd5.Text, "MD5");
    private void BtnCopySha1_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashSha1.Text, "SHA1");
    private void BtnCopySha256_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashSha256.Text, "SHA256");
    private void BtnCopySha512_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashSha512.Text, "SHA512");

    private void CopyText(string text, string label)
    {
        if (!string.IsNullOrEmpty(text) && text != "计算中...")
        {
            Clipboard.SetText(text);
            MessageBox.Show($"{label} 已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnBase64Encode_Click(object sender, RoutedEventArgs e)
    {
        TxtCodecOutput.Text = _devToolsService.Base64Encode(TxtCodecInput.Text);
    }

    private void BtnBase64Decode_Click(object sender, RoutedEventArgs e)
    {
        try { TxtCodecOutput.Text = _devToolsService.Base64Decode(TxtCodecInput.Text); }
        catch (Exception ex) { MessageBox.Show($"Base64 解码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void BtnUrlEncode_Click(object sender, RoutedEventArgs e)
    {
        TxtCodecOutput.Text = _devToolsService.UrlEncode(TxtCodecInput.Text);
    }

    private void BtnUrlDecode_Click(object sender, RoutedEventArgs e)
    {
        TxtCodecOutput.Text = _devToolsService.UrlDecode(TxtCodecInput.Text);
    }

    private void BtnCopyCodecOutput_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtCodecOutput.Text))
        {
            Clipboard.SetText(TxtCodecOutput.Text);
            MessageBox.Show("转换结果已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnFillCurrentTs_Click(object sender, RoutedEventArgs e)
    {
        var (sec, ms) = _devToolsService.GetCurrentTimestamps();
        TxtTimestampInput.Text = sec.ToString();
        TxtTsResult.Text = $"{_devToolsService.TimestampToDateTime(sec)} (当前系统时间)";
    }

    private void BtnConvertTs_Click(object sender, RoutedEventArgs e)
    {
        if (long.TryParse(TxtTimestampInput.Text.Trim(), out long ts))
        {
            TxtTsResult.Text = _devToolsService.TimestampToDateTime(ts);
        }
        else
        {
            MessageBox.Show("请输入合法的数字时间戳（10位秒或13位毫秒）！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnGenGuid_Click(object sender, RoutedEventArgs e)
    {
        bool upper = ChkGuidUpper.IsChecked == true;
        bool noHyphen = ChkGuidNoHyphen.IsChecked == true;
        TxtGuidResult.Text = _devToolsService.GenerateGuid(upper, noHyphen);
    }

    private void BtnCopyGuid_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtGuidResult.Text))
        {
            Clipboard.SetText(TxtGuidResult.Text);
            MessageBox.Show("全新 GUID 已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region 新机开荒与预装精简 (UWP Debloat & Winget)

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
        var targetBtn = sender as Button;
        if (targetBtn != null) targetBtn.IsEnabled = false;
        try
        {
            await LoadUwpAppsAsync();
            MessageBox.Show("预装应用安装状态扫描完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"扫描失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (targetBtn != null) targetBtn.IsEnabled = true;
        }
    }

    private async void BtnUninstallSingleUwp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string prefix)
        {
            var item = _uwpItems.FirstOrDefault(i => i.PackagePrefix == prefix);
            string name = item?.DisplayName ?? prefix;

            var confirm = MessageBox.Show($"确定要从本机彻底卸载【{name}】吗？\n将同时移除当前用户包及新用户预配模板，防止后续死灰复燃。", "确认卸载", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            btn.IsEnabled = false;
            try
            {
                var (ok, msg) = await _uwpDebloatService.UninstallPackageAsync(prefix);
                MessageBox.Show(msg, ok ? "卸载成功" : "卸载提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                await LoadUwpAppsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卸载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }
    }

    private void BtnCopyWingetUpgrade_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText("winget upgrade --all --include-unknown");
        MessageBox.Show("一键静默全盘升级所有软件命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet)
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"命令已复制到剪贴板：\n{snippet}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region Tab 滚动与交互

    /// <summary>
    /// 支持在单行 TabControl 标签栏上通过鼠标滚轮横向平滑滚动标签
    /// </summary>
    private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }

    #endregion
}
