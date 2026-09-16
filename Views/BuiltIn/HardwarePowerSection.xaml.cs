using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class HardwarePowerSection : UserControl
{
    private readonly WinTricksService _service = new();

    public HardwarePowerSection()
    {
        InitializeComponent();
    }

    private void BtnCimDisk_Click(object sender, RoutedEventArgs e)
    {
        _service.RunGetPhysicalDiskInConsole();
    }

    private void BtnMsInfo32_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenMsInfo32();
    }

    private void BtnDxDiag_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenDxDiag();
    }

    private void BtnSleepStudy_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.GenerateSleepStudyReport();
        if (res.Success)
        {
            MessageBox.Show(res.Message, "SleepStudy 报告已生成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(res.Message, "执行提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnBatteryReport_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.GenerateBatteryReport();
        if (res.Success)
        {
            MessageBox.Show(res.Message, "电池健康报告已生成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(res.Message, "执行提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnLastWake_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.GetWakeAndRequestsInfo();
        MessageBox.Show(res.Info, "电源请求与唤醒源分析", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnEnableUltimateScheme_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.EnableUltimatePerformanceScheme();
        if (res.Success)
        {
            MessageBox.Show(res.Message, "卓越性能方案激活", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(res.Message, "执行失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnClearType_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenClearTypeTuner();
    }

    private void BtnColorCalib_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenColorCalibration();
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
