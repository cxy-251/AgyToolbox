using System;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class NetworkStackSection : UserControl
{
    private readonly WinTricksService _service = new();

    public NetworkStackSection()
    {
        InitializeComponent();
    }

    private void BtnCheckPort_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtPortToKill.Text.Trim(), out int port))
        {
            var occs = _service.FindPortOccupants(port);
            if (occs.Count == 0)
            {
                TxtPortResult.Text = $"✓ 端口 {port} 空闲未被占用";
                TxtPortResult.Foreground = ThemeBrushes.Success;
                PanelKillPort.Visibility = Visibility.Collapsed;
            }
            else
            {
                var first = occs[0];
                TxtPortResult.Text = $"✗ 端口正在被占用！";
                TxtPortResult.Foreground = ThemeBrushes.Danger;
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
            if (_service.KillProcessByPid(pid))
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

    private void BtnTestNetConnection_Click(object sender, RoutedEventArgs e)
    {
        _service.RunTestNetConnectionInConsole("api.github.com", 443);
    }

    private void BtnFlushDns_Click(object sender, RoutedEventArgs e)
    {
        var msg = _service.FlushDns();
        MessageBox.Show(msg, "DNS 解析缓存", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnResetWinsock_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            "【重置 Winsock 目录提示】\n\n" +
            "重置后将清除第三方代理、抓包软件与防病毒软件注入的 LSP 套接字劫持。\n" +
            "此操作需要【重启计算机】后方能完全生效。\n\n" +
            "是否确认在独立管理员终端中执行 netsh winsock reset？",
            "Winsock 重置确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            _service.ResetWinsockAndIp();
        }
    }

    private void BtnFirewall_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenAdvancedFirewall();
    }

    private void BtnCopyCmd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            Clipboard.SetText(cmd);
            MessageBox.Show($"已复制命令到剪贴板:\n{cmd}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private readonly WinOptimizerService _optimizerService = new();

    private void BtnOpenAdvancedSharing_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "control.exe",
                Arguments = "/name Microsoft.NetworkAndSharingCenter /page Advanced",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开高级共享设置失败: {ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnListNetShares_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k net share",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void BtnResyncTime_Click(object sender, RoutedEventArgs e)
    {
        var (ok, output) = _optimizerService.ResyncNetworkTime();
        MessageBox.Show(output, "NTP 网络授时同步", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnToggleRealTimeIsUniversal_Click(object sender, RoutedEventArgs e)
    {
        bool current = _optimizerService.IsRealTimeIsUniversalEnabled();
        var res = _optimizerService.SetRealTimeIsUniversal(!current);
        MessageBox.Show(res.Message, "双系统 UTC 硬件时钟", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }
}
