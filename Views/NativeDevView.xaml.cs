using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class NativeDevView : UserControl
{
    private readonly NativeDevService _nativeDevService = new();
    private readonly WinTricksService _winTricksService = new();
    private CancellationTokenSource? _diagCts;

    public NativeDevView()
    {
        InitializeComponent();

        RefreshWslStatus();
        RefreshSandboxStatus();
        RefreshSshAgentStatus();
        RefreshDevModeStatus();
    }

    #region 1. 虚拟化基建 (WSL2 & Sandbox)

    private void RefreshWslStatus()
    {
        var (installed, info) = _nativeDevService.GetWslStatus();
        TxtWslStatus.Text = installed ? "[已安装就绪]" : "[尚未安装]";
        TxtWslStatus.Foreground = installed ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnCheckWsl_Click(object sender, RoutedEventArgs e)
    {
        var (installed, info) = _nativeDevService.GetWslStatus();
        RefreshWslStatus();
        MessageBox.Show($"WSL 状态检测结果：\n\n{info}", "WSL 状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnInstallWsl_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.InstallWslInConsole();
        MessageBox.Show(msg, ok ? "已启动" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void RefreshSandboxStatus()
    {
        bool enabled = _nativeDevService.IsSandboxEnabled();
        TxtSandboxStatus.Text = enabled ? "[已启用]" : "[未启用]";
        TxtSandboxStatus.Foreground = enabled ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnCheckSandbox_Click(object sender, RoutedEventArgs e)
    {
        RefreshSandboxStatus();
        bool enabled = _nativeDevService.IsSandboxEnabled();
        MessageBox.Show(enabled ? "Windows Sandbox 沙盒特性已在当前系统启用！" : "Windows Sandbox 尚未启用，需点击启用并重启。", "沙盒状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnEnableSandbox_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.EnableSandboxInConsole();
        MessageBox.Show(msg, ok ? "已发起启用" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 2. SSH 认证与开发者模式 (Developer Mode & Symlink)

    private void RefreshSshAgentStatus()
    {
        bool running = _nativeDevService.IsSshAgentAutoStart();
        TxtSshAgentStatus.Text = running ? "[已开启自启与运行]" : "[当前未自启]";
        TxtSshAgentStatus.Foreground = running ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnEnableSshAgent_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.EnableSshAgentAutoStart();
        RefreshSshAgentStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void RefreshDevModeStatus()
    {
        bool enabled = _nativeDevService.IsDeveloperModeEnabled();
        TxtDevModeStatus.Text = enabled ? "[已开启 (免提权 Symlink 就绪)]" : "[未开启 (创建 Symlink 需管理员权限)]";
        TxtDevModeStatus.Foreground = enabled ? Brushes.DarkGreen : Brushes.DarkOrange;
        BtnToggleDevMode.Content = enabled ? "关闭开发者模式" : "开启开发者模式";
    }

    private void BtnToggleDevMode_Click(object sender, RoutedEventArgs e)
    {
        bool current = _nativeDevService.IsDeveloperModeEnabled();
        var (ok, msg) = _nativeDevService.SetDeveloperMode(!current);
        RefreshDevModeStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnOpenDevSettings_Click(object sender, RoutedEventArgs e)
    {
        _nativeDevService.OpenDeveloperSettings();
    }

    #endregion

    #region 3. 原生网络链路与实时诊断

    private async void RunDiagnostic(string cmd, string args)
    {
        _diagCts?.Cancel();
        _diagCts = new CancellationTokenSource();

        if (BtnStopDiag != null) BtnStopDiag.IsEnabled = true;
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
            if (BtnStopDiag != null) BtnStopDiag.IsEnabled = false;
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
        if (BtnStopDiag != null) BtnStopDiag.IsEnabled = false;
        TxtDiagConsole.AppendText("[已请求终止诊断]\n");
    }

    private void BtnClearDiagLog_Click(object sender, RoutedEventArgs e)
    {
        TxtDiagConsole.Text = "[网络诊断控制台已清空就绪]\n";
    }

    #endregion

    #region 4. 通用启动与代码复制

    private void BtnLaunchTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string app && !string.IsNullOrWhiteSpace(app))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = app,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动工具失败: {ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet && !string.IsNullOrWhiteSpace(snippet))
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"已复制命令到剪贴板：\n\n{snippet}", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyCli_Click(object sender, RoutedEventArgs e)
    {
        BtnCopySnippet_Click(sender, e);
    }

    #endregion
}
