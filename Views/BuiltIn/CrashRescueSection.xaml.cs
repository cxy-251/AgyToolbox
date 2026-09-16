using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class CrashRescueSection : UserControl
{
    private readonly WinTricksService _service = new();

    public CrashRescueSection()
    {
        InitializeComponent();
    }

    private void BtnReliability_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenReliabilityMonitor();
    }

    private void BtnMemoryDiag_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenMemoryDiagnostic();
    }

    private void BtnDriverVerifier_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenDriverVerifier();
    }

    private void BtnOpenMinidump_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.OpenMinidumpFolder();
        if (!res.Success)
        {
            MessageBox.Show(res.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnSysDm_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenSystemProperties();
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
