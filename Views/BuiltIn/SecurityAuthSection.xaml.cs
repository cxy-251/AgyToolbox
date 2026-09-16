using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class SecurityAuthSection : UserControl
{
    private readonly WinTricksService _service = new();

    public SecurityAuthSection()
    {
        InitializeComponent();
    }

    private void BtnSecPol_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenLocalSecurityPolicy();
    }

    private void BtnLusrMgr_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenLocalUsersAndGroups();
    }

    private void BtnCertMgr_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenCertificateManager();
    }

    private void BtnGpUpdate_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.RunGpUpdateForceInConsole();
        if (!res.Success)
        {
            MessageBox.Show(res.Message, "执行失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnMrt_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenMrt();
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
