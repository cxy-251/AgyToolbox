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

    private void BtnRebootToBios_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            "【重启至 UEFI/BIOS 固件确认】\n\n" +
            "即将执行: shutdown /r /fw /t 0\n\n" +
            "计算机将立即重启并自动进入主板 UEFI/BIOS 设置界面，无须在开机时疯狂按 Del/F2 键。\n" +
            "注意：该功能要求主板支持 UEFI 固件接口并以管理员身份运行。\n\n" +
            "请务必先保存所有工作内容！是否现在立即重启并进入 BIOS？",
            "重启至 BIOS 确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (res == MessageBoxResult.Yes)
        {
            try
            {
                _service.TriggerRebootToBios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"触发重启至 BIOS 失败: {ex.Message}\n请确保以管理员身份运行且主板支持 UEFI 固件引导。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
