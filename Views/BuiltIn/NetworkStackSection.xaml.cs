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
}
