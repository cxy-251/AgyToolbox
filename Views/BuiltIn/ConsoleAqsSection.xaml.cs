using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class ConsoleAqsSection : UserControl
{
    private readonly WinTricksService _service = new();

    public ConsoleAqsSection()
    {
        InitializeComponent();
    }

    private void BtnStepsRecorder_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenStepsRecorder();
    }

    private void BtnRemoteAssist_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenRemoteAssistance();
    }

    private void BtnLaunchCmd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = cmd,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "启动异常", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
