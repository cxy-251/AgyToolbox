using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class WinTricksView : UserControl
{
    private readonly WinTricksService _winTricksService = new();

    public WinTricksView()
    {
        InitializeComponent();
    }

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
                TxtPortResult.Foreground = Brushes.Green;
                PanelKillPort.Visibility = Visibility.Collapsed;
            }
            else
            {
                var first = occs[0];
                TxtPortResult.Text = $"✗ 端口正在被占用！";
                TxtPortResult.Foreground = Brushes.Red;
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
}
