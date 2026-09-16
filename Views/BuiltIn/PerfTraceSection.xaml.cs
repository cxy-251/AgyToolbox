using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class PerfTraceSection : UserControl
{
    private readonly WinTricksService _service = new();

    public PerfTraceSection()
    {
        InitializeComponent();
    }

    private void BtnResMon_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenResMon();
    }

    private void BtnPerfMon_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenPerformanceMonitor();
    }

    private void BtnEventVwr_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenEventViewer();
    }

    private void BtnWpr_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.OpenPerformanceRecorder();
        if (!res.Success)
        {
            MessageBox.Show(res.Message, "WPR 启动提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
