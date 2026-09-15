using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public record PathEntry(string Path, bool Exists, bool IsSystem);

public class EnvManagerService
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    private const int HWND_BROADCAST = 0xffff;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    /// <summary>
    /// 获取用户或系统级别的 PATH 列表并诊断死路径
    /// </summary>
    public List<PathEntry> GetPathEntries(bool isSystem)
    {
        var list = new List<PathEntry>();
        string? rawPath = null;

        if (isSystem)
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            rawPath = key?.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
        }
        else
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Environment");
            rawPath = key?.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
        }

        if (string.IsNullOrWhiteSpace(rawPath)) return list;

        var parts = rawPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // 展开环境变量如 %USERPROFILE% 后检查物理路径是否存在
            string expanded = Environment.ExpandEnvironmentVariables(trimmed);
            bool exists = Directory.Exists(expanded) || File.Exists(expanded);

            list.Add(new PathEntry(trimmed, exists, isSystem));
        }

        return list;
    }

    /// <summary>
    /// 保存 PATH 条目列表并向全系统广播环境变量变更通知
    /// </summary>
    public (bool Success, string Message) SavePathEntries(bool isSystem, IEnumerable<string> paths)
    {
        try
        {
            string combined = string.Join(";", paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct());

            if (isSystem)
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", true);
                if (key == null) return (false, "无法打开系统注册表 Environment 键，请以管理员身份运行本工具。");
                key.SetValue("Path", combined, RegistryValueKind.ExpandString);
            }
            else
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Environment", true);
                if (key == null) return (false, "无法打开用户注册表 Environment 键。");
                key.SetValue("Path", combined, RegistryValueKind.ExpandString);
            }

            // 广播 WM_SETTINGCHANGE 通知所有运行中的程序（如新开的 CMD/PowerShell）立即刷新环境变量
            NotifyEnvironmentChanged();

            return (true, isSystem ? "系统级 PATH 已更新并广播！" : "当前用户 PATH 已更新并广播！");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "权限不足！修改系统级 PATH 需要右键以管理员身份运行本工具。");
        }
        catch (Exception ex)
        {
            return (false, $"保存异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 广播 WM_SETTINGCHANGE，通知系统环境变量已被修改
    /// </summary>
    public void NotifyEnvironmentChanged()
    {
        try
        {
            SendMessageTimeout(
                (IntPtr)HWND_BROADCAST,
                WM_SETTINGCHANGE,
                UIntPtr.Zero,
                "Environment",
                SMTO_ABORTIFHUNG,
                1500,
                out _);
        }
        catch
        {
            // 忽略非致命广播异常
        }
    }
}
