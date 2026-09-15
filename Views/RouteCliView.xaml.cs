using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class RouteCliView : UserControl
{
    private readonly WinTricksService _winTricksService = new();
    private CancellationTokenSource? _diagCts;

    public RouteCliView()
    {
        InitializeComponent();
    }

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

    private void BtnCopyCli_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd && !string.IsNullOrWhiteSpace(cmd))
        {
            Clipboard.SetText(cmd);
            MessageBox.Show($"已复制命令到剪贴板：\n\n{cmd}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

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
}
