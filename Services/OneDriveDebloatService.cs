using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public class OneDriveDebloatService
{
    private static readonly string[] PossibleOneDrivePaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\OneDrive\OneDrive.exe"),
        @"C:\Program Files\Microsoft OneDrive\OneDrive.exe",
        @"C:\Program Files (x86)\Microsoft OneDrive\OneDrive.exe"
    ];

    /// <summary>
    /// 检测当前系统是否安装了 OneDrive
    /// </summary>
    public bool IsOneDriveInstalled()
    {
        return PossibleOneDrivePaths.Any(File.Exists);
    }

    /// <summary>
    /// 获取 OneDrive 官方卸载器路径
    /// </summary>
    public string? GetOneDriveSetupPath()
    {
        string sysRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string[] candidates =
        [
            Path.Combine(sysRoot, "SysWOW64", "OneDriveSetup.exe"),
            Path.Combine(sysRoot, "System32", "OneDriveSetup.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\OneDrive\Update\OneDriveSetup.exe")
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// 彻底卸载 OneDrive 并清除资源管理器左侧栏常驻图标
    /// </summary>
    public async Task<(bool Success, string Message)> UninstallOneDriveAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // 1. 杀死正在运行的 OneDrive 进程
                foreach (var p in Process.GetProcessesByName("OneDrive"))
                {
                    try { p.Kill(); } catch { }
                }

                // 2. 找到官方卸载器
                string? setupExe = GetOneDriveSetupPath();
                if (!string.IsNullOrEmpty(setupExe))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = setupExe,
                        Arguments = "/uninstall",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(30000);
                }

                // 3. 删除开机自启动项
                try
                {
                    using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    runKey?.DeleteValue("OneDrive", false);
                }
                catch { }

                // 4. 从资源管理器左侧导航窗格中隐藏 OneDrive 图标
                try
                {
                    string clsidKey = @"Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}";
                    using var key = Registry.CurrentUser.CreateSubKey(clsidKey);
                    key.SetValue("System.IsPinnedToNameSpaceTree", 0, RegistryValueKind.DWord);
                }
                catch { }

                return (true, "已成功彻底卸载 OneDrive，并已移除开机自启与资源管理器左侧导航栏图标！");
            }
            catch (Exception ex)
            {
                return (false, $"卸载 OneDrive 失败: {ex.Message}");
            }
        });
    }
}
