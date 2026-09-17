using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views.BuiltIn;

public partial class SecurityAuthSection : UserControl
{
    private readonly WinTricksService _service = new();
    private readonly NativeDevService _nativeDevService = new();

    public SecurityAuthSection()
    {
        InitializeComponent();

        RefreshSudoStatus();
        RefreshSshAgentStatus();
    }

    private void BtnSecPol_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenLocalSecurityPolicy();
    }

    private void BtnLusrMgr_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenLocalUsersAndGroups();
    }

    private void BtnCertMgr_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenCertificateManager();
    }

    private void BtnGpUpdate_Click(object sender, RoutedEventArgs e)
    {
        var res = _service.RunGpUpdateForceInConsole();
        if (!res.Success)
        {
            MessageBox.Show(res.Message, "执行失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnMrt_Click(object sender, RoutedEventArgs e)
    {
        _service.OpenMrt();
    }

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

    private void BtnCopyCmd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cmd)
        {
            Clipboard.SetText(cmd);
            MessageBox.Show($"已复制命令到剪贴板:\n{cmd}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
