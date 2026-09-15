using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public class NativeDevService
{
    #region 1. WSL2 & Windows Sandbox

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
                StandardOutputEncoding = Encoding.Unicode
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

    #endregion

    #region 2. Dev Drive (开发驱动器 / ReFS 文件系统) & Dev Home

    /// <summary>
    /// 查询 Dev Drive (开发驱动器) 支持状态与当前系统挂载情况
    /// </summary>
    public (bool IsSupported, bool HasDevDrive, string Info) GetDevDriveStatus()
    {
        try
        {
            // 检查已挂载驱动器是否有 ReFS 格式
            var refsDrives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveFormat.Equals("ReFS", StringComparison.OrdinalIgnoreCase))
                .Select(d => $"{d.Name} ({d.VolumeLabel}) - ReFS 容量: {d.TotalSize / 1024 / 1024 / 1024} GB")
                .ToList();

            // 运行 fsutil devdrv query 检查特性支持
            var psi = new ProcessStartInfo
            {
                FileName = "fsutil.exe",
                Arguments = "devdrv query",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(3000);

            // 鲁棒性判定：
            // 1. fsutil devdrv query 执行返回码为 0，说明内核明确支持 Dev Drive 特性
            // 2. 或检测到已有 ReFS 分区
            // 3. 或 Windows 11 Build >= 22621 (22H2/23H2/24H2 内核原生支持)
            bool supported = (proc != null && proc.ExitCode == 0) ||
                             refsDrives.Count > 0 ||
                             (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22621);

            // 如果明确输出包含禁用字样
            if (output.Contains("disabled", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("已禁用", StringComparison.OrdinalIgnoreCase))
            {
                return (false, false, "当前系统组策略或配置禁用了 Dev Drive 特性。");
            }

            if (refsDrives.Count > 0)
            {
                return (true, true, $"检测到已挂载 Dev Drive 分区：\n{string.Join("\n", refsDrives)}");
            }

            if (supported)
            {
                return (true, false, "系统已支持 Dev Drive 特性，当前尚未创建 ReFS 开发驱动器分区。可点击一键直达创建！");
            }

            return (false, false, "当前 Windows 版本暂不支持 Dev Drive (需 Win11 23H2/24H2 现代版本及以上)。");
        }
        catch (Exception ex)
        {
            return (false, false, $"查询异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开 Windows 磁盘与卷设置页（用于引导创建 Dev Drive 虚拟磁盘 VHDX）
    /// </summary>
    public void OpenDisksAndVolumesSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:disksandvolumes",
                UseShellExecute = true
            });
        }
        catch { }
    }

    /// <summary>
    /// 打开或通过 winget 安装微软官方 Dev Home (开发人员主页)
    /// </summary>
    public (bool Success, string Message) LaunchOrInstallDevHome()
    {
        try
        {
            // 尝试通过协议直接打开
            var psi = new ProcessStartInfo
            {
                FileName = "ms-devhome:",
                UseShellExecute = true
            };
            Process.Start(psi);
            return (true, "正在打开微软 Dev Home (开发人员主页)...");
        }
        catch
        {
            // 如果未安装，调用终端通过 winget 安装
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/k echo [正在通过 winget 自动安装微软官方 Dev Home] && winget install --id Microsoft.DevHome --source winget --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return (true, "系统中未检测到 Dev Home，已在控制台中启动 winget 自动安装！");
            }
            catch (Exception ex)
            {
                return (false, $"启动安装失败: {ex.Message}");
            }
        }
    }

    #endregion

    #region 3. Win11 原生 Sudo 命令控制

    /// <summary>
    /// 检测 Win11 24H2+ 原生 sudo.exe 状态
    /// </summary>
    public (bool Supported, bool Enabled, string ModeName, int ModeCode) GetSudoStatus()
    {
        try
        {
            string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string sudoExe = Path.Combine(systemPath, "sudo.exe");
            bool exists = File.Exists(sudoExe);

            if (!exists)
            {
                return (false, false, "当前系统未安装原生 sudo (需 Win11 24H2 Build 26100+)", -1);
            }

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Sudo");
            var val = key?.GetValue("Enabled");
            int modeCode = val is int intVal ? intVal : 0;

            string modeName = modeCode switch
            {
                1 => "已开启 - 强制新窗口 (forceNewWindow)",
                2 => "已开启 - 禁止键盘输入交互 (disableInput)",
                3 => "已开启 - 当前终端内联提权 (normal / 推荐)",
                _ => "已禁用 (Disabled)"
            };

            return (true, modeCode > 0, modeName, modeCode);
        }
        catch (Exception ex)
        {
            return (false, false, $"检测异常: {ex.Message}", -1);
        }
    }

    /// <summary>
    /// 配置 Win11 原生 Sudo 模式 (normal / forceNewWindow / disable)
    /// </summary>
    public (bool Success, string Message) SetSudoMode(string mode)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sudo.exe config --enable {mode}",
                UseShellExecute = true,
                Verb = "runas"
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(4000);
            return (true, $"已向系统请求设置 sudo 模式为 [{mode}]！");
        }
        catch (Exception ex)
        {
            return (false, $"设置 sudo 模式失败: {ex.Message}");
        }
    }

    #endregion

    #region 4. OpenSSH Agent & 开发者模式 (Developer Mode & Symlink)

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
    /// 一键将系统原生 ssh-agent 设为开机自启并立刻启动
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
            return (true, enable ? "已开启 Windows 开发者模式！当前用户已获得免提权直接创建符号链接 (Symlink) 权限。" : "已关闭开发者模式。");
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

    #endregion

    #region 5. 原生实用工具（certutil / fsutil / pktmon）

    /// <summary>
    /// 调用原生 certutil.exe 计算指定文件的哈希值 (SHA256 / MD5 / SHA1)
    /// </summary>
    public (bool Success, string HashResult) ComputeFileHash(string filePath, string algorithm = "SHA256")
    {
        if (!File.Exists(filePath))
        {
            return (false, "目标文件不存在！");
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "certutil.exe",
                Arguments = $"-hashfile \"{filePath}\" {algorithm}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                // 第二行通常为干净的十六进制哈希
                string hash = lines[1].Trim().Replace(" ", "");
                return (true, hash);
            }

            return (false, $"计算未能提取哈希：{output}");
        }
        catch (Exception ex)
        {
            return (false, $"调用 certutil 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 调用原生 fsutil 秒级创建指定大小的测试文件
    /// </summary>
    public (bool Success, string Message) CreateDummyFile(string targetPath, long sizeBytes)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "fsutil.exe",
                Arguments = $"file createnew \"{targetPath}\" {sizeBytes}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);

            if (File.Exists(targetPath))
            {
                return (true, $"已成功秒级创建测试文件！\n文件路径: {targetPath}\n占用空间: {sizeBytes / 1024 / 1024} MB");
            }
            return (false, $"创建失败: {output}");
        }
        catch (Exception ex)
        {
            return (false, $"执行异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 在管理员控制台中启动 PktMon 内核抓包
    /// </summary>
    public (bool Success, string Message) StartPktMonConsole()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k echo [正在启动 PktMon 内核抓包监视器] && pktmon start --capture && echo 抓包进行中... 抓取完毕后输入 pktmon stop 停止，再输入 pktmon etl2pcap PktMon.etl --out trace.pcapng 转为 Wireshark 格式！",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return (true, "已在管理员控制台中启动 PktMon 内核抓包！");
        }
        catch (Exception ex)
        {
            return (false, $"启动抓包失败: {ex.Message}");
        }
    }

    #endregion
}
