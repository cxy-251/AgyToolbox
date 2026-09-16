using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class BootRecoverySection : UserControl
{
    private readonly WinTricksService _service = new();

    public BootRecoverySection()
    {
        InitializeComponent();
    }

    private void BtnReagentcInfo_Click(object sender, RoutedEventArgs e)
    {
        _service.RunReagentcInfoInConsole();
    }

    private void BtnColdRestart_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            "【绝对冷重启警告】\n\n" +
            "即将执行 shutdown /r /t 0。\n" +
            "该操作将彻底重置 Windows 内核与所有硬件驱动状态（绕过快速启动混合休眠），且会立即重启计算机！\n\n" +
            "请务必先保存所有未完成的文档与项目。\n是否现在立即冷重启？",
            "冷重启确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (res == MessageBoxResult.Yes)
        {
            _service.TriggerColdRestart();
        }
    }

    private void BtnMsConfig_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenSystemConfiguration();
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
