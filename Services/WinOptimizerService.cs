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
    /// 检测是否已禁用狂按5次Shift弹出粘滞键提示
    /// </summary>
    public bool IsStickyKeysPromptDisabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys");
            var val = key?.GetValue("Flags") as string;
            return val == "506";
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置是否禁用狂按5次Shift弹出粘滞键提示
    /// </summary>
    public (bool Success, string Message) SetStickyKeysPromptDisabled(bool disable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys");
            key.SetValue("Flags", disable ? "506" : "510", RegistryValueKind.String);
            return (true, disable ? "已禁用狂按 5 次 Shift 触发粘滞键弹窗！打字和打游戏不再被中断。" : "已恢复默认粘滞键快捷键。");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>
    /// 检测是否已开启 Win+V 剪贴板历史记录
    /// </summary>
    public bool IsClipboardHistoryEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard");
            var val = key?.GetValue("EnableClipboardHistory");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置开启或关闭 Win+V 剪贴板历史记录
    /// </summary>
    public (bool Success, string Message) SetClipboardHistoryEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Clipboard");
            key.SetValue("EnableClipboardHistory", enable ? 1 : 0, RegistryValueKind.DWord);
            return (true, enable ? "已成功开启 Win+V 剪贴板历史记录！" : "已关闭剪贴板历史记录。");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    /// <summary>
    /// 检测是否已开启开发者模式与解除 260 字符长路径限制
    /// </summary>
    public bool IsDevModeAndLongPathsEnabled()
    {
        try
        {
            using var keyDev = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            var devVal = keyDev?.GetValue("AllowDevelopmentWithoutDevLicense");
            bool devOk = devVal is int intDev && intDev == 1;

            using var keyPath = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
            var pathVal = keyPath?.GetValue("LongPathsEnabled");
            bool pathOk = pathVal is int intPath && intPath == 1;

            return devOk && pathOk;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启开发者模式并解除 260 字符路径限制 (解决 deep node_modules 或 git clone 报 Filename too long)
    /// </summary>
    public (bool Success, string Message) SetDevModeAndLongPaths(bool enable)
    {
        try
        {
            using var keyDev = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            keyDev.SetValue("AllowDevelopmentWithoutDevLicense", enable ? 1 : 0, RegistryValueKind.DWord);

            using var keyPath = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
            keyPath.SetValue("LongPathsEnabled", enable ? 1 : 0, RegistryValueKind.DWord);

            return (true, enable
                ? "已开启开发者模式并解除 260 字符长路径限制！(再也不怕 node_modules 路径过长报错)"
                : "已恢复默认限制。");
        }
        catch (Exception ex)
        {
            return (false, $"设置失败(可能需要管理员权限): {ex.Message}");
        }
    }

    /// <summary>
    /// 检测当前 C 盘 BitLocker 加密状态 (新机隐形地雷排查)
    /// </summary>
    public (bool IsEncrypted, string Details) GetBitLockerStatus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "manage-bde",
                Arguments = "-status C:",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(3000);

            bool isEncrypted = output.Contains("已加密", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("正在加密", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("100%", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("Protection On", StringComparison.OrdinalIgnoreCase);

            return (isEncrypted, output.Trim());
        }
        catch (Exception ex)
        {
            return (false, $"检测失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 读取并导出 BitLocker 48 位数字恢复密钥 (防 BIOS 升级/主板重置锁机变砖)
    /// </summary>
    public (bool Success, string Message) GetBitLockerRecoveryKey()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "manage-bde",
                Arguments = "-protectors -get C:",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(3000);

            return (true, string.IsNullOrWhiteSpace(output) ? "未查询到 BitLocker 恢复密钥。" : output.Trim());
        }
        catch (Exception ex)
        {
            return (false, $"获取失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测系统休眠文件 (hiberfil.sys) 是否启用
    /// </summary>
    public bool IsHibernationEnabled()
    {
        try
        {
            return File.Exists(@"C:\hiberfil.sys");
        }
        catch { return false; }
    }

    /// <summary>
    /// 一键关停/开启系统休眠文件 (台式机/长插电本关停后瞬间释放 16G~32GB C 盘 SSD 空间)
    /// </summary>
    public (bool Success, string Message) SetHibernation(bool enable)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = enable ? "-h on" : "-h off",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);

            return (true, enable
                ? "已开启休眠文件 (C:\\hiberfil.sys)！"
                : "已成功彻底关停休眠！系统已瞬间释放与物理内存同等大小 (16GB~32GB+) 的 C 盘宝贵空间！");
        }
        catch (Exception ex)
        {
            return (false, $"设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 一键将当前网络位置切换为【专用网络】(Private Network，打通局域网设备互联、快传与 Ping)
    /// </summary>
    public (bool Success, string Message) SetNetworkToPrivate()
    {
        try
        {
            var script = "Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, "已成功将所有当前活跃网络适配器切换为【专用网络】！\n防火墙已放行局域网设备发现与端口通信。");
        }
        catch (Exception ex)
        {
            return (false, $"设置网络类别失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测是否已禁用 Windows 11 后台静默安装推荐应用与系统建议
    /// </summary>
    public bool IsSilentAppInstallDisabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            var val = key?.GetValue("SilentInstalledAppsEnabled");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 启用或禁用 Windows 11 后台静默安装推广应用以及锁屏/开始菜单应用建议
    /// </summary>
    public (bool Success, string Message) SetSilentAppInstallDisabled(bool disable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
            int regVal = disable ? 0 : 1;
            key.SetValue("SilentInstalledAppsEnabled", regVal, RegistryValueKind.DWord);
            key.SetValue("SubscribedContent-338388Enabled", regVal, RegistryValueKind.DWord);
            key.SetValue("SubscribedContent-338389Enabled", regVal, RegistryValueKind.DWord);
            key.SetValue("SystemPaneSuggestionsEnabled", regVal, RegistryValueKind.DWord);

            return (true, disable
                ? "已成功禁用 Windows 11 后台静默安装推荐应用策略，并关闭了锁屏与开始菜单建议。"
                : "已恢复默认内容交付设置。");
        }
        catch (Exception ex)
        {
            return (false, $"设置静默推广策略失败: {ex.Message}");
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
