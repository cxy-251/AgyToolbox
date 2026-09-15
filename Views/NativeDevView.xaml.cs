using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;
using Microsoft.Win32;

namespace AgyToolbox.Views;

public partial class NativeDevView : UserControl
{
    private readonly NativeDevService _nativeDevService = new();
    private readonly WinTricksService _winTricksService = new();
    private CancellationTokenSource? _diagCts;

    public NativeDevView()
    {
        InitializeComponent();

        RefreshWslStatus();
        RefreshSandboxStatus();
        RefreshDevDriveStatus();
        RefreshSshAgentStatus();
        RefreshDevModeStatus();
        RefreshSudoStatus();
        RefreshExecPolicyStatus();
    }

    #region 1. 虚拟化与驱动基建 (WSL2 / Sandbox / Dev Drive / Dev Home)

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

    private void RefreshDevDriveStatus()
    {
        var (supported, hasDevDrive, info) = _nativeDevService.GetDevDriveStatus();
        if (hasDevDrive)
        {
            TxtDevDriveStatus.Text = "[已挂载 Dev Drive 开发驱动器]";
            TxtDevDriveStatus.Foreground = Brushes.DarkGreen;
        }
        else if (supported)
        {
            TxtDevDriveStatus.Text = "[系统支持 Dev Drive (当前未创建)]";
            TxtDevDriveStatus.Foreground = Brushes.DodgerBlue;
        }
        else
        {
            TxtDevDriveStatus.Text = "[当前环境暂不支持 Dev Drive]";
            TxtDevDriveStatus.Foreground = Brushes.DarkOrange;
        }
    }

    private void BtnRefreshDevDrive_Click(object sender, RoutedEventArgs e)
    {
        var (supported, hasDevDrive, info) = _nativeDevService.GetDevDriveStatus();
        RefreshDevDriveStatus();
        MessageBox.Show($"Dev Drive 状态检测结果：\n\n{info}", "Dev Drive 状态", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOpenDisksSettings_Click(object sender, RoutedEventArgs e)
    {
        _nativeDevService.OpenDisksAndVolumesSettings();
    }

    private void BtnLaunchDevHome_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.LaunchOrInstallDevHome();
        MessageBox.Show(msg, "Dev Home", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 2. 权限特权与开发者模式 (Sudo / Developer Mode / SSH / icacls)

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
            TxtSudoStatus.Foreground = enabled ? Brushes.DarkGreen : Brushes.DarkOrange;
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
        TxtSshAgentStatus.Foreground = running ? Brushes.DarkGreen : Brushes.DarkOrange;
    }

    private void BtnEnableSshAgent_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.EnableSshAgentAutoStart();
        RefreshSshAgentStatus();
        MessageBox.Show(msg, ok ? "设置成功" : "提示", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
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

    #endregion

    #region 3. 原生网络链路与系统诊断 (pktmon / 路由 / resmon / 性能)

    private async void RunDiagnostic(string cmd, string args)
    {
        _diagCts?.Cancel();
        _diagCts = new CancellationTokenSource();

        if (BtnStopDiag != null) BtnStopDiag.IsEnabled = true;
        TxtDiagConsole.AppendText($"\n>>> [{DateTime.Now:HH:mm:ss}] 启动诊断: {cmd} {args}\n");
        TxtDiagConsole.ScrollToEnd();

        try
        {
            await _winTricksService.RunNetworkDiagnosticAsync(cmd, args, line =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtDiagConsole.AppendText(line + "\n");
                    TxtDiagConsole.ScrollToEnd();
                });
            }, _diagCts.Token);
        }
        catch (Exception ex)
        {
            TxtDiagConsole.AppendText($"[诊断异常]: {ex.Message}\n");
        }
        finally
        {
            if (BtnStopDiag != null) BtnStopDiag.IsEnabled = false;
        }
    }

    private void BtnTracertFast_Click(object sender, RoutedEventArgs e)
    {
        string host = string.IsNullOrWhiteSpace(TxtRouteHost.Text) ? "1.1.1.1" : TxtRouteHost.Text.Trim();
        RunDiagnostic("tracert", $"-d -h 20 {host}");
    }

    private void BtnPathping_Click(object sender, RoutedEventArgs e)
    {
        string host = string.IsNullOrWhiteSpace(TxtRouteHost.Text) ? "1.1.1.1" : TxtRouteHost.Text.Trim();
        RunDiagnostic("pathping", $"-n -q 2 -p 250 -h 15 {host}");
    }

    private void BtnRoutePrint_Click(object sender, RoutedEventArgs e)
    {
        RunDiagnostic("route", "print -4");
    }

    private void BtnArp_Click(object sender, RoutedEventArgs e)
    {
        RunDiagnostic("arp", "-a");
    }

    private void BtnStopDiag_Click(object sender, RoutedEventArgs e)
    {
        _diagCts?.Cancel();
        if (BtnStopDiag != null) BtnStopDiag.IsEnabled = false;
        TxtDiagConsole.AppendText("[已请求终止诊断]\n");
    }

    private void BtnClearDiagLog_Click(object sender, RoutedEventArgs e)
    {
        TxtDiagConsole.Text = "[网络诊断控制台已清空就绪]\n";
    }

    private void BtnStartPktMon_Click(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = _nativeDevService.StartPktMonConsole();
        MessageBox.Show(msg, "PktMon 抓包监视器", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    #endregion

    #region 4. 原生实用工具（certutil / fsutil / 通用复制）

    private void BtnPickFileHash_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要使用原生 certutil 计算 SHA256 哈希的文件"
        };
        if (dialog.ShowDialog() == true)
        {
            var (ok, hash) = _nativeDevService.ComputeFileHash(dialog.FileName, "SHA256");
            if (ok)
            {
                Clipboard.SetText(hash);
                MessageBox.Show($"文件: {Path.GetFileName(dialog.FileName)}\n\nSHA256 哈希值:\n{hash}\n\n已自动复制到剪贴板！", "哈希计算成功 (certutil)", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(hash, "计算失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnCreateDummyFile_Click(object sender, RoutedEventArgs e)
    {
        BtnCreateDummyFile100M_Click(sender, e);
    }

    private void BtnCreateDummyFile100M_Click(object sender, RoutedEventArgs e)
    {
        CreateDummyFileWithDialog("test_dummy_100m.dat", 100L * 1024L * 1024L);
    }

    private void BtnCreateDummyFile1G_Click(object sender, RoutedEventArgs e)
    {
        CreateDummyFileWithDialog("test_dummy_1g.dat", 1024L * 1024L * 1024L);
    }

    private void CreateDummyFileWithDialog(string defaultFileName, long sizeBytes)
    {
        var dialog = new SaveFileDialog
        {
            Title = "选择保存测试文件的位置与文件名",
            FileName = defaultFileName,
            Filter = "数据文件 (*.dat)|*.dat|所有文件 (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            var (ok, msg) = _nativeDevService.CreateDummyFile(dialog.FileName, sizeBytes);
            MessageBox.Show(msg, "fsutil 创建测试文件", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }

    private void BtnLaunchTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string app && !string.IsNullOrWhiteSpace(app))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = app,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动工具失败: {ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet && !string.IsNullOrWhiteSpace(snippet))
        {
            Clipboard.SetText(snippet);
            MessageBox.Show($"已复制命令到剪贴板：\n\n{snippet}", "已复制", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyCli_Click(object sender, RoutedEventArgs e)
    {
        BtnCopySnippet_Click(sender, e);
    }

    #endregion

    #region 5. Shell 生态与脚本编程 (Shell Ecosystem & Scripting)

    private void RefreshExecPolicyStatus()
    {
        var (ok, policy, details) = _nativeDevService.GetExecutionPolicy();
        TxtExecPolicyStatus.Text = $"[{policy}]";
        if (policy.Equals("RemoteSigned", StringComparison.OrdinalIgnoreCase) ||
            policy.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase) ||
            policy.Equals("Bypass", StringComparison.OrdinalIgnoreCase))
        {
            TxtExecPolicyStatus.Foreground = Brushes.DarkGreen;
        }
        else if (policy.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
        {
            TxtExecPolicyStatus.Foreground = Brushes.Crimson;
        }
        else
        {
            TxtExecPolicyStatus.Foreground = Brushes.DarkOrange;
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

