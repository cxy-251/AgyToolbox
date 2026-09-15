using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class NativeDevView : UserControl
{
    private readonly NativeDevService _nativeDevService = new();

    public NativeDevView()
    {
        InitializeComponent();

        RefreshWslStatus();
        RefreshSandboxStatus();
        RefreshSshAgentStatus();
        RefreshDevModeStatus();
    }

    private void RefreshDevModeStatus()
    {
        bool enabled = _nativeDevService.IsDeveloperModeEnabled();
        TxtDevModeStatus.Text = enabled ? "[已开启 (免提权 Symlink 就绪)]" : "[未开启 (创建 Symlink 需管理员权限)]";
        TxtDevModeStatus.Foreground = enabled ? Brushes.DarkGreen : Brushes.DarkOrange;
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

    private void RefreshWslStatus()
    {
        var (installed, info) = _nativeDevService.GetWslStatus();
        TxtWslStatus.Text = installed ? "[已安装就绪]" : "[尚未安装]";
        TxtWslStatus.Foreground = installed ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnCheckWsl_Click(object sender, RoutedEventArgs e)
    {
        var (installed, info) = _nativeDevService.GetWslStatus();
        RefreshWslStatus();
        MessageBox.Show($"WSL 状态检测结果：\n\n{info}", "WSL 状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnInstallWsl_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.InstallWslInConsole();
        MessageBox.Show(msg, ok ? "已启动" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void RefreshSandboxStatus()
    {
        bool enabled = _nativeDevService.IsSandboxEnabled();
        TxtSandboxStatus.Text = enabled ? "[已启用]" : "[未启用]";
        TxtSandboxStatus.Foreground = enabled ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnCheckSandbox_Click(object sender, RoutedEventArgs e)
    {
        RefreshSandboxStatus();
        bool enabled = _nativeDevService.IsSandboxEnabled();
        MessageBox.Show(enabled ? "Windows Sandbox 沙盒特性已在当前系统启用！" : "Windows Sandbox 尚未启用，需点击启用并重启。", "沙盒状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnEnableSandbox_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.EnableSandboxInConsole();
        MessageBox.Show(msg, ok ? "已发起启用" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void RefreshSshAgentStatus()
    {
        bool running = _nativeDevService.IsSshAgentAutoStart();
        TxtSshAgentStatus.Text = running ? "[已开启自启与运行]" : "[当前未自启]";
        TxtSshAgentStatus.Foreground = running ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnEnableSshAgent_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.EnableSshAgentAutoStart();
        RefreshSshAgentStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet)
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"已复制命令到剪贴板：\n{snippet}", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
