using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class NativeDevView : UserControl
{
    private readonly NativeDevService _nativeDevService = new();

    public NativeDevView()
    {
        InitializeComponent();
        RefreshExecPolicyStatus();
    }

    #region Shell 生态与执行策略 (ExecutionPolicy / $PROFILE)

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

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet && !string.IsNullOrWhiteSpace(snippet))
        {
            Clipboard.SetText(snippet);
            string preview = snippet.Length > 160
                ? snippet.Substring(0, 160).TrimEnd() + "...\n\n(完整脚本源码已成功复制到剪贴板，可直接粘贴使用)"
                : snippet;
            MessageBox.Show($"已复制至剪贴板：\n\n{preview}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion
}
