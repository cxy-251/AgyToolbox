using System.Diagnostics;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public class NativeDevService
{
    /// <summary>
    /// 检测 WSL2 (Windows Subsystem for Linux) 的安装状态
    /// </summary>
    public (bool IsInstalled, string Info) GetWslStatus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "--status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.Unicode
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(3000);

            if (output.Contains("默认分发", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("Default Distribution", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("WSL 2", StringComparison.OrdinalIgnoreCase))
            {
                return (true, output.Trim());
            }

            return (false, "当前系统尚未安装或未初始化 WSL2。");
        }
        catch (Exception ex)
        {
            return (false, $"检测 WSL 异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 一键呼出控制台执行 WSL2 原生安装 (wsl --install)
    /// </summary>
    public (bool Success, string Message) InstallWslInConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在执行微软官方 WSL2 原生一键安装] && wsl --install",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已启动管理员终端执行 wsl --install！安装完毕后重启电脑即可体验完整 Linux 内核环境。");
        }
        catch (Exception ex)
        {
            return (false, $"执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 Windows 原生沙盒 (Windows Sandbox) 特性是否已启用
    /// </summary>
    public bool IsSandboxEnabled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = "/online /get-featureinfo /featurename:Containers-DisposableClientVM",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);

            return output.Contains("已启用", StringComparison.OrdinalIgnoreCase) ||
                   output.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>
    /// 一键启用 Windows 原生沙盒功能 (Containers-DisposableClientVM)
    /// </summary>
    public (bool Success, string Message) EnableSandboxInConsole()
    {
        try
        {
            var script = "Enable-WindowsOptionalFeature -Online -FeatureName 'Containers-DisposableClientVM' -All -NoRestart";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已呼出管理员终端启用 Windows Sandbox 官方沙箱！完成后需重启一次生效。");
        }
        catch (Exception ex)
        {
            return (false, $"启用失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 OpenSSH 身份验证代理服务 (ssh-agent) 是否已配置为开机自启动
    /// </summary>
    public bool IsSshAgentAutoStart()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ssh-agent");
            var val = key?.GetValue("Start");
            return val is int intVal && intVal == 2; // 2 代表 Automatic (自动启动)
        }
        catch { return false; }
    }

    /// <summary>
    /// 一键将系统原生 ssh-agent 设为开机自启并立刻启动 (免去每次手输私钥密码与重复加载)
    /// </summary>
    public (bool Success, string Message) EnableSshAgentAutoStart()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "config ssh-agent start= auto",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);

            // 立刻启动服务
            var psiStart = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "start ssh-agent",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var procStart = Process.Start(psiStart);
            procStart?.WaitForExit(3000);

            return (true, "已成功将 Windows 原生 OpenSSH Agent 设置为开机自动运行并已就绪！\n支持配合 ssh-add ~/.ssh/id_ed25519 实现全局免密！");
        }
        catch (Exception ex)
        {
            return (false, $"配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 Windows 开发者模式 (Developer Mode) 是否已开启
    /// 开启后核心价值：普通用户无需管理员提权即可直接创建符号链接 (mklink / Symlink)
    /// </summary>
    public bool IsDeveloperModeEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            var val = key?.GetValue("AllowDevelopmentWithoutDevLicense");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置 Windows 开发者模式状态
    /// </summary>
    public (bool Success, string Message) SetDeveloperMode(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            key.SetValue("AllowDevelopmentWithoutDevLicense", enable ? 1 : 0, RegistryValueKind.DWord);
            return (true, enable ? "已开启 Windows 开发者模式！当前用户已获得免提权创建符号链接 (Symlink) 权限。" : "已关闭开发者模式。");
        }
        catch (Exception ex)
        {
            return (false, $"设置开发者模式失败: {ex.Message}\n（注意：写入 HKLM 需要管理员权限）");
        }
    }

    /// <summary>
    /// 打开 Windows 设置中的“开发者选项”页面
    /// </summary>
    public void OpenDeveloperSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:developers",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
