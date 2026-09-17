using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class HardwarePowerSection : UserControl
{
    private readonly WinTricksService _service = new();
    private readonly WinOptimizerService _optimizerService = new();

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

    #region 工具 6~10 开荒硬件外设与排错事件处理

    private void BtnDevMgmt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开设备管理器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEnableTestSigning_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "开启驱动测试模式 (testsigning on) 允许安装未通过微软 WHQL 数字签名的驱动程序。\n\n" +
            "【注意】：此设置需要重启计算机方可生效，桌面右下角会显示「测试模式」水印。\n确定要开启测试模式吗？",
            "开启驱动测试模式确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var output = ExecuteBcdedit("set testsigning on");
        MessageBox.Show(output, "测试模式配置结果", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnDisableTestSigning_Click(object sender, RoutedEventArgs e)
    {
        var output = ExecuteBcdedit("set testsigning off");
        MessageBox.Show(output, "恢复签名强制配置结果", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string ExecuteBcdedit(string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bcdedit.exe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            string std = p?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            string err = p?.StandardError.ReadToEnd()?.Trim() ?? "";
            p?.WaitForExit(5000);
            return string.IsNullOrWhiteSpace(err) ? (string.IsNullOrWhiteSpace(std) ? "操作完成" : std) : err;
        }
        catch (Exception ex)
        {
            return $"执行 bcdedit 失败: {ex.Message}";
        }
    }

    private void BtnRestartAudio_Click(object sender, RoutedEventArgs e)
    {
        var res = _optimizerService.RestartAudioSubsystem();
        MessageBox.Show(res.Message, "音频服务重置", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnMmsys_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("mmsys.cpl") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开经典声音面板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnMicPrivacy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:privacy-microphone") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开麦克风权限失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRestartBluetooth_Click(object sender, RoutedEventArgs e)
    {
        var res = _optimizerService.RestartBluetoothService();
        MessageBox.Show(res.Message, "蓝牙服务重置", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnOpenBluetoothSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开蓝牙设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClearPrintQueue_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定要一键清空所有卡死的打印任务吗？\n\n这会临时停止后台 Print Spooler 服务并强制清空 spool\\PRINTERS 目录中的所有排队作业文件。",
            "清空打印队列确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        var res = _optimizerService.ClearPrintQueueAndRestartSpooler();
        MessageBox.Show(res.Message, "打印队列维护", MessageBoxButton.OK, res.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnAddPrinter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("rundll32.exe", "printui.dll,PrintUIEntry /il") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开添加打印机向导失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPrintMgmt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("printmanagement.msc") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开打印管理控制台失败 (仅 Windows 专业版/企业版支持): {ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnOpenPrintersDir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\spool\PRINTERS");
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开 PRINTERS 目录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDisplaySwitch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("DisplaySwitch.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"调起屏幕投影失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOpenDisplaySettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开显示设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}

