using System.Diagnostics;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public class WinOptimizerService
{
    private const string ClassicMenuClsidKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

    /// <summary>
    /// 检测是否已开启 Win10 经典右键完整菜单 (Win11 无需二级折叠展开)
    /// </summary>
    public bool IsClassicContextMenuEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ClassicMenuClsidKey);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 切换 Win11 经典右键菜单状态
    /// </summary>
    public (bool Success, string Message) SetClassicContextMenu(bool enable)
    {
        try
        {
            if (enable)
            {
                using var key = Registry.CurrentUser.CreateSubKey(ClassicMenuClsidKey);
                key.SetValue("", ""); // 默认值设为空字符串即可劫持生效
                return (true, "已开启 Win10 经典完整右键菜单！需要重启资源管理器 (explorer.exe) 生效。");
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
                return (true, "已恢复 Win11 现代折叠右键菜单！需要重启资源管理器生效。");
            }
        }
        catch (Exception ex)
        {
            return (false, $"设置右键菜单失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测是否已禁用任务栏必应在线搜索与网络热搜广告
    /// </summary>
    public bool IsBingSearchDisabled()
    {
        try
        {
            using var searchKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            var val = searchKey?.GetValue("BingSearchEnabled");
            if (val is int intVal && intVal == 0) return true;

            using var policyKey = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\Explorer");
            var polVal = policyKey?.GetValue("DisableSearchBoxSuggestions");
            return polVal is int polInt && polInt == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 切换任务栏必应搜索广告禁用状态（纯粹保留本地文件秒级搜索）
    /// </summary>
    public (bool Success, string Message) SetBingSearchDisabled(bool disable)
    {
        try
        {
            using var searchKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            searchKey.SetValue("BingSearchEnabled", disable ? 0 : 1, RegistryValueKind.DWord);

            using var policyKey = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer");
            policyKey.SetValue("DisableSearchBoxSuggestions", disable ? 1 : 0, RegistryValueKind.DWord);

            return (true, disable
                ? "已成功关闭任务栏必应网络搜索与广告！重启资源管理器后生效。"
                : "已恢复必应网络搜索建议。重启资源管理器后生效。");
        }
        catch (Exception ex)
        {
            return (false, $"设置必应搜索失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测是否已关闭微软遥测与个性化诊断数据收集
    /// </summary>
    public bool IsTelemetryDisabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Privacy");
            var val = key?.GetValue("TailoredExperiencesWithDiagnosticDataEnabled");
            return val is int intVal && intVal == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 切换个性化遥测与商业广告建议
    /// </summary>
    public (bool Success, string Message) SetTelemetryDisabled(bool disable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Privacy");
            key.SetValue("TailoredExperiencesWithDiagnosticDataEnabled", disable ? 0 : 1, RegistryValueKind.DWord);

            return (true, disable
                ? "已禁用 Windows 个性化遥测与广告推送！"
                : "已恢复默认遥测设置。");
        }
        catch (Exception ex)
        {
            return (false, $"设置遥测失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 一键平滑重启 Windows 资源管理器 (explorer.exe)
    /// </summary>
    public void RestartExplorer()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("explorer"))
            {
                p.Kill();
            }
        }
        catch
        {
            // 如果自动拉起失败，主动启动 explorer
        }

        // 确保 explorer 重新运行
        Task.Delay(500).ContinueWith(_ =>
        {
            if (Process.GetProcessesByName("explorer").Length == 0)
            {
                Process.Start("explorer.exe");
            }
        });
    }
}
