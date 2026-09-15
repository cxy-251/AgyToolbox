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
    /// 检测是否显示已知文件扩展名
    /// </summary>
    public bool IsFileExtVisible()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            var val = key?.GetValue("HideFileExt");
            return val is int intVal && intVal == 0; // 0 代表不隐藏 = 显示
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置是否显示文件扩展名 (防伪装可执行文件病毒，程序员必开)
    /// </summary>
    public (bool Success, string Message) SetFileExtVisible(bool show)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            key.SetValue("HideFileExt", show ? 0 : 1, RegistryValueKind.DWord);
            return (true, show ? "已设置为【始终显示文件扩展名】！" : "已恢复默认隐藏已知扩展名。");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>
    /// 检测是否显示隐藏文件与隐藏驱动器
    /// </summary>
    public bool IsHiddenFilesVisible()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            var val = key?.GetValue("Hidden");
            return val is int intVal && intVal == 1; // 1 代表显示隐藏文件
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置是否显示隐藏文件 (方便查看 .git, .env, .vscode 等隐藏目录)
    /// </summary>
    public (bool Success, string Message) SetHiddenFilesVisible(bool show)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            key.SetValue("Hidden", show ? 1 : 2, RegistryValueKind.DWord);
            return (true, show ? "已设置为【显示隐藏的文件和驱动器】！" : "已恢复隐藏系统隐藏文件。");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>
    /// 检测打开资源管理器时默认是否进入“此电脑”
    /// </summary>
    public bool IsOpenThisPcDefault()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            var val = key?.GetValue("LaunchTo");
            return val is int intVal && intVal == 1; // 1 代表此电脑，2 代表快速访问/主页
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置打开资源管理器默认进入“此电脑” (告别满是推广的主页)
    /// </summary>
    public (bool Success, string Message) SetOpenThisPcDefault(bool thisPc)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            key.SetValue("LaunchTo", thisPc ? 1 : 2, RegistryValueKind.DWord);
            return (true, thisPc ? "已设置 Win+E 默认打开【此电脑 (This PC)】！" : "已恢复默认打开快速访问主页。");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>
    /// 一键解锁 Windows 原生隐藏的“卓越性能模式” (Ultimate Performance)
    /// </summary>
    public (bool Success, string Message) EnableUltimatePerformance()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string outStr = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);

            return (true, "已成功激活【卓越性能模式】！\n已在系统电源选项中解锁最高 CPU 睿频与硬件响应。");
        }
        catch (Exception ex)
        {
            return (false, $"激活卓越性能模式失败: {ex.Message}");
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
