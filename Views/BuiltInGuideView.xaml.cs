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
    private void BtnMrt_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenMrt();
    private void BtnDxDiag_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenDxDiag();
    private void BtnEditHosts_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenHostsFile();
    private void BtnPsr_Click(object sender, RoutedEventArgs e) => _winTricksService.OpenStepsRecorder();

    private void BtnBatteryReport_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg, _) = _winTricksService.GenerateBatteryReport();
        MessageBox.Show(msg, success ? "电池健康体检成功" : "提示", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnFlushDns_Click(object sender, RoutedEventArgs e)
    {
        var msg = _winTricksService.FlushDns();
        MessageBox.Show(msg, "DNS 解析缓存", MessageBoxButton.OK, MessageBoxImage.Information);
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
