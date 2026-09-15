using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class BuiltInGuideView : UserControl
{
    private readonly WinTricksService _winTricksService = new();

    public BuiltInGuideView()
    {
        InitializeComponent();
    }

    private void BtnReliability_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenReliabilityMonitor();
    private void BtnResmon_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenResMon();
    private void BtnGodMode_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenGodMode();
    private void BtnDxDiag_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenDxDiag();
    private void BtnEditHosts_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenHostsFile();

    private void BtnRunSfc_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winTricksService.RunSfcScanInConsole();
        MessageBox.Show(msg, ok ? "已启动" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnRunDism_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winTricksService.RunDismRepairInConsole();
        MessageBox.Show(msg, ok ? "已启动" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnResetWinsock_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要重置 Windows 网络协议栈 (Winsock + TCP/IP) 吗？\n\n该操作将清除所有残留的代理劫持与网络分层服务提供者 (LSP)，重置后需要重启电脑生效。",
            "确认重置网络协议栈",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        var (ok, msg) = _winTricksService.ResetWinsockAndIp();
        MessageBox.Show(msg, ok ? "重置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnFlushDns_Click(object sender, RoutedEventArgs e)
    {
        var msg = _winTricksService.FlushDns();
        MessageBox.Show(msg, "DNS 解析缓存", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnRunWinSxSCleanup_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _winTricksService.RunDismCleanupBaseInConsole();
        MessageBox.Show(msg, ok ? "已启动" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnBatteryReport_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg, _) = _winTricksService.GenerateBatteryReport();
        MessageBox.Show(msg, success ? "电池健康体检成功" : "提示", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnGetWifi_Click(object sender, RoutedEventArgs e)
    {
        var wifis = _winTricksService.GetSavedWifiPasswords();
        GridWifi.ItemsSource = wifis;
        GridWifi.Visibility = Visibility.Visible;
    }

    private void BtnRunCmd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            var (ok, msg) = _winTricksService.RunSystemCommand(cmd);
            if (!ok)
            {
                MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet)
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"已复制搜索语法到剪贴板：\n\n{snippet}\n\n可以直接在文件资源管理器右上角搜索框中粘贴使用！", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
