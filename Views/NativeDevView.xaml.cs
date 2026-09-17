using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;
using Microsoft.Win32;

namespace AgyToolbox.Views;

public partial class NativeDevView : UserControl
{
    private readonly NativeDevService _nativeDevService = new();

    public NativeDevView()
    {
        InitializeComponent();

        RefreshSudoStatus();
        RefreshDevModeStatus();
        RefreshSshAgentStatus();
        RefreshExecPolicyStatus();
    }

    #region 1. 权限特权与开发者模式 (Sudo / Developer Mode / SSH-Agent)

    private void RefreshSudoStatus()
    {
        var (supported, enabled, modeName, _) = _nativeDevService.GetSudoStatus();
        if (!supported)
        {
            TxtSudoStatus.Text = "[系统未安装原生 sudo]";
            TxtSudoStatus.Foreground = Brushes.Gray;
        }
        else
        {
            TxtSudoStatus.Text = $"[{modeName}]";
            TxtSudoStatus.Foreground = enabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        }
    }

    private void BtnRefreshSudo_Click(object sender, RoutedEventArgs e)
    {
        RefreshSudoStatus();
        var (_, _, modeName, _) = _nativeDevService.GetSudoStatus();
        MessageBox.Show($"当前原生 Sudo 状态：\n\n{modeName}", "Sudo 状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnEnableSudoNormal_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.SetSudoMode("normal");
        RefreshSudoStatus();
        MessageBox.Show(msg + "\n模式：normal (当前终端内联执行)", "Sudo 设置", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnEnableSudoWindow_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.SetSudoMode("forceNewWindow");
        RefreshSudoStatus();
        MessageBox.Show(msg + "\n模式：forceNewWindow (每次提权打开新独立控制台窗口)", "Sudo 设置", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnDisableSudo_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.SetSudoMode("disable");
        RefreshSudoStatus();
        MessageBox.Show(msg, "Sudo 设置", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshDevModeStatus()
    {
        bool enabled = _nativeDevService.IsDeveloperModeEnabled();
        TxtDevModeStatus.Text = enabled ? "[已开启 (免提权 Symlink 就绪)]" : "[未开启 (创建 Symlink 需管理员权限)]";
        TxtDevModeStatus.Foreground = enabled ? ThemeBrushes.Success : ThemeBrushes.Warning;
        BtnToggleDevMode.Content = enabled ? "关闭开发者模式" : "开启开发者模式";
    }

    private void BtnToggleDevMode_Click(object sender, RoutedEventArgs e)
    {
        bool current = _nativeDevService.IsDeveloperModeEnabled();
        var (ok, msg) = _nativeDevService.SetDeveloperMode(!current);
        RefreshDevModeStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnOpenDevSettings_Click(object sender, RoutedEventArgs e)
    {
        _nativeDevService.OpenDeveloperSettings();
    }

    private void RefreshSshAgentStatus()
    {
        bool running = _nativeDevService.IsSshAgentAutoStart();
        TxtSshAgentStatus.Text = running ? "[已开启自启与运行]" : "[当前未自启]";
        TxtSshAgentStatus.Foreground = running ? ThemeBrushes.Success : ThemeBrushes.Warning;
    }

    private void BtnEnableSshAgent_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.EnableSshAgentAutoStart();
        RefreshSshAgentStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 2. 原生 CLI 与服务部署 (fsutil / 通用剪贴板复制)

    private void BtnCreateDummyFile100M_Click(object sender, RoutedEventArgs e)
    {
        CreateDummyFileWithDialog("test_dummy_100m.bin", 100L * 1024L * 1024L);
    }

    private void BtnCreateDummyFile1G_Click(object sender, RoutedEventArgs e)
    {
        CreateDummyFileWithDialog("test_dummy_1g.bin", 1024L * 1024L * 1024L);
    }

    private void CreateDummyFileWithDialog(string defaultFileName, long sizeBytes)
    {
        var dialog = new SaveFileDialog
        {
            Title = "选择保存测试文件的位置与文件名",
            FileName = defaultFileName,
            Filter = "二进制数据文件 (*.bin)|*.bin|所有文件 (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            var (ok, msg) = _nativeDevService.CreateDummyFile(dialog.FileName, sizeBytes);
            MessageBox.Show(msg, "fsutil 创建测试文件", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet && !string.IsNullOrWhiteSpace(snippet))
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"已复制命令或代码至剪贴板：\n\n{snippet}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region 3. Shell 生态与差异对比 (ExecutionPolicy / $PROFILE)

    private void RefreshExecPolicyStatus()
    {
        var (ok, policy, details) = _nativeDevService.GetExecutionPolicy();
        TxtExecPolicyStatus.Text = $"[{policy}]";
        if (policy.Equals("RemoteSigned", StringComparison.OrdinalIgnoreCase) ||
            policy.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase) ||
            policy.Equals("Bypass", StringComparison.OrdinalIgnoreCase))
        {
            TxtExecPolicyStatus.Foreground = ThemeBrushes.Success;
        }
        else if (policy.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
        {
            TxtExecPolicyStatus.Foreground = ThemeBrushes.Danger;
        }
        else
        {
            TxtExecPolicyStatus.Foreground = ThemeBrushes.Warning;
        }
    }

    private void BtnCheckExecPolicy_Click(object sender, RoutedEventArgs e)
    {
        RefreshExecPolicyStatus();
        var (ok, policy, details) = _nativeDevService.GetExecutionPolicy();
        MessageBox.Show($"PowerShell 脚本执行策略检测详情：\n\n{details}", "ExecutionPolicy 状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnSetRemoteSigned_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.SetExecutionPolicyRemoteSigned();
        RefreshExecPolicyStatus();
        MessageBox.Show(msg, "执行策略配置", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnOpenProfilePs5_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.OpenOrCreateProfile(isPwsh7: false);
        MessageBox.Show(msg, "PowerShell 5.1 $PROFILE", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnOpenProfilePwsh7_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.OpenOrCreateProfile(isPwsh7: true);
        MessageBox.Show(msg, "PowerShell 7 $PROFILE", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnCopyProfileTemplate_Click(object sender, RoutedEventArgs e)
    {
        string template = _nativeDevService.GetProfileStarterTemplate(false);
        Clipboard.SetText(template);
        MessageBox.Show("已成功复制推荐的 $PROFILE 配置文件模板到剪贴板！\n您可以直接粘贴到配置文件中使用。", "已复制模板", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOpenCwdTipExplorer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{appDir}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开目录失败: {ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #endregion
}
