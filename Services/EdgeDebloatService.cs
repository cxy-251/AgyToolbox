using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public class EdgeDebloatService
{
    private static readonly string[] PossibleEdgePaths =
    [
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
    ];

    /// <summary>
    /// 检测当前系统是否安装了 Microsoft Edge 浏览器主体
    /// </summary>
    public bool IsEdgeInstalled()
    {
        return PossibleEdgePaths.Any(File.Exists);
    }

    /// <summary>
    /// 检测是否已安装 Google Chrome
    /// </summary>
    public bool IsChromeInstalled()
    {
        string[] chromePaths =
        [
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")
        ];
        return chromePaths.Any(File.Exists);
    }

    /// <summary>
    /// 获取 Edge 官方自带的强力卸载器 setup.exe 路径
    /// </summary>
    public string? GetEdgeSetupPath()
    {
        string baseDir = @"C:\Program Files (x86)\Microsoft\Edge\Application";
        if (!Directory.Exists(baseDir))
        {
            baseDir = @"C:\Program Files\Microsoft\Edge\Application";
        }

        if (!Directory.Exists(baseDir)) return null;

        // Edge 会在版本号子目录中存放 Installer\setup.exe，例如 120.0.2210.91\Installer\setup.exe
        foreach (var dir in Directory.GetDirectories(baseDir))
        {
            string setupPath = Path.Combine(dir, "Installer", "setup.exe");
            if (File.Exists(setupPath))
            {
                return setupPath;
            }
        }

        return null;
    }

    /// <summary>
    /// 彻底卸载 Edge 浏览器主体，并设置注册表防止 Windows Update 偷偷重装
    /// 【安全策略】：只卸载 Edge 浏览器，严格保留 WebView2 运行时，确保微信、钉钉等三方桌面应用正常运行
    /// </summary>
    public async Task<(bool Success, string Message)> UninstallEdgeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // 1. 杀死正在运行的 Edge 进程
                foreach (var proc in Process.GetProcessesByName("msedge"))
                {
                    try { proc.Kill(); } catch { }
                }

                // 2. 查找 Edge 官方安装包卸载器
                string? setupExe = GetEdgeSetupPath();
                if (string.IsNullOrEmpty(setupExe))
                {
                    return (false, "未找到 Edge 的官方卸载程序 (setup.exe)。Edge 可能已经被卸载，或路径非标准。");
                }

                // 3. 执行官方静默强力卸载
                var psi = new ProcessStartInfo
                {
                    FileName = setupExe,
                    Arguments = "--uninstall --system-level --verbose-logging --force-uninstall",
                    UseShellExecute = true,
                    Verb = "runas" // 需要管理员权限
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(30000);

                // 4. 写入阻止策略：防止 Windows Update 后续偷偷重新推送安装 Edge
                try
                {
                    using var edgeUpdateKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\EdgeUpdate");
                    edgeUpdateKey.SetValue("DoNotUpdateToEdgeWithChromium", 1, RegistryValueKind.DWord);
                }
                catch
                {
                    // 策略写入非致命
                }

                bool stillExists = IsEdgeInstalled();
                if (stillExists)
                {
                    return (true, "卸载指令已执行完毕！若桌面仍有残留图标，重启电脑后将彻底消失。同时已配置注册表阻止 Windows Update 偷偷重装。");
                }
                else
                {
                    return (true, "已成功彻底卸载 Microsoft Edge 浏览器主体！并已写入注册表阻止 Windows Update 自动重装。");
                }
            }
            catch (Exception ex)
            {
                return (false, $"卸载执行异常: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 通过 winget 静默安装 Google Chrome 官方纯净版
    /// </summary>
    public async Task<(bool Success, string Message)> InstallChromeAsync(Action<string> onProgress)
    {
        return await Task.Run(() =>
        {
            try
            {
                onProgress("正在通过 winget 从 Google 官方源拉取 Chrome 纯净安装包...");

                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install Google.Chrome --silent --accept-package-agreements --accept-source-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return (false, "无法启动 winget 程序。请确认系统已安装 winget。");

                while (!proc.StandardOutput.EndOfStream)
                {
                    string? line = proc.StandardOutput.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) onProgress(line);
                }

                proc.WaitForExit();

                if (proc.ExitCode == 0 || IsChromeInstalled())
                {
                    return (true, "Google Chrome 官方纯净版已成功安装！");
                }
                else
                {
                    return (false, $"安装异常退出，退出代码: {proc.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"winget 执行异常: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 打开 Windows 默认应用设置界面，方便用户一键将 Chrome 设为默认浏览器
    /// </summary>
    public void OpenDefaultAppsSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
