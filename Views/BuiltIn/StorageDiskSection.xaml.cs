using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class StorageDiskSection : UserControl
{
    private readonly WinTricksService _service = new();

    public StorageDiskSection()
    {
        InitializeComponent();
    }

    private void BtnChkdskScan_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.RunChkdskScanInConsole("C:");
        if (!res.Success)
        {
            MessageBox.Show(res.Message, "执行提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnDiskPart_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenDiskPartConsole();
    }

    private void BtnDiskMgmt_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenDiskManagement();
    }

    private void BtnDiskDefrag_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenDiskDefrag();
    }

    private void BtnWinSxSClean_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            "【WinSxS 终极深度瘦身警告】\n\n" +
            "执行 Dism ... /ResetBase 会彻底清除所有已被新版本取代的历史更新补丁备份。\n" +
            "优点：可释放数 GB 宝贵系统盘空间；\n" +
            "注意：执行后将【无法回滚卸载】当前已安装的现有 Windows 累积更新。\n\n" +
            "是否确认在独立管理员控制台中开始执行？",
            "WinSxS 终极组件库清理确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (res == MessageBoxResult.Yes)
        {
            _service.RunDismCleanupBaseInConsole();
        }
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
