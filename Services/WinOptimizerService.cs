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
    /// 检测桌面“此电脑”与“控制面板”等核心系统图标是否已显示
    /// </summary>
    public bool IsDesktopIconsVisible()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel");
            var val = key?.GetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 在桌面一键显示或隐藏【此电脑】、【控制面板】、【用户文件】、【网络】图标
    /// </summary>
    public (bool Success, string Message) SetDesktopIconsVisible(bool show)
    {
        try
        {
            int val = show ? 0 : 1; // 0 = 显示, 1 = 隐藏
            string[] paths = new[]
            {
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu"
            };

            foreach (var p in paths)
            {
                using var key = Registry.CurrentUser.CreateSubKey(p);
                key.SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", val, RegistryValueKind.DWord); // 此电脑
                key.SetValue("{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", val, RegistryValueKind.DWord); // 控制面板
                key.SetValue("{59031a47-3f72-44a7-89c5-5595fe6b30ee}", val, RegistryValueKind.DWord); // 用户文件
                key.SetValue("{F02C1A0D-BE21-4350-88B0-753720A61B06}", val, RegistryValueKind.DWord); // 网络
            }

            return (true, show
                ? "已在桌面显示【此电脑】、【控制面板】、【用户文件】与【网络】！重启资源管理器或刷新桌面生效。"
                : "已恢复默认隐藏桌面系统图标。");
        }
        catch (Exception ex) { return (false, $"设置桌面图标失败: {ex.Message}"); }
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

    #region 步骤二强化：休眠三档/快速启动/保留存储/传递优化/工程师视图

    /// <summary>
    /// 休眠文件三挡控制: 0 = 彻底关闭(释放100%空间), 1 = Reduced 极简模式(仅快速启动), 2 = Full 全量模式
    /// </summary>
    public (bool Success, string Message) SetHibernationTier(int tier)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            switch (tier)
            {
                case 0:
                    psi.Arguments = "-h off";
                    Process.Start(psi)?.WaitForExit(3000);
                    return (true, "已彻底关闭休眠文件 (C:\\hiberfil.sys)，完全回收 100% 物理内存大小磁盘空间！");
                case 1:
                    psi.Arguments = "-h on";
                    Process.Start(psi)?.WaitForExit(3000);
                    psi.Arguments = "-h -type reduced";
                    Process.Start(psi)?.WaitForExit(3000);
                    return (true, "已开启 Reduced 极简模式！仅保留快速启动支持，文件体积大幅缩减约 50%~80%。");
                case 2:
                default:
                    psi.Arguments = "-h on";
                    Process.Start(psi)?.WaitForExit(3000);
                    psi.Arguments = "-h -type full";
                    Process.Start(psi)?.WaitForExit(3000);
                    return (true, "已开启 Full 全量休眠模式！完整支持休眠与混合睡眠。");
            }
        }
        catch (Exception ex)
        {
            return (false, $"设置休眠模式失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测快速启动 (Fast Startup) 状态
    /// </summary>
    public bool IsFastStartupEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power");
            var val = key?.GetValue("HiberbootEnabled");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置快速启动 (Fast Startup) 开关 (关闭可解决多系统引导冲突及关机后硬件断电不彻底问题)
    /// </summary>
    public (bool Success, string Message) SetFastStartup(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power");
            key.SetValue("HiberbootEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
            return (true, enable
                ? "已开启【快速启动】。"
                : "已关闭【快速启动】！解决多系统引导冲突与部分主板断电残留问题。");
        }
        catch (Exception ex)
        {
            return (false, $"设置快速启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 查询系统“保留的存储 (Reserved Storage)”状态与大小
    /// </summary>
    public (bool IsEnabled, string Details) GetReservedStorageStatus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "fsutil",
                Arguments = "storagereserve query C:",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(3000);

            bool isEnabled = output.Contains("已启用", StringComparison.OrdinalIgnoreCase) ||
                             output.Contains("Enabled", StringComparison.OrdinalIgnoreCase);

            return (isEnabled, output.Trim());
        }
        catch (Exception ex)
        {
            return (false, $"查询保留存储失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 释放或恢复系统保留的存储 (约释放 7GB C 盘空间)
    /// </summary>
    public (bool Success, string Message) SetReservedStorage(bool enable)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "fsutil",
                Arguments = enable ? "storagereserve set reserved 1" : "storagereserve set reserved 0",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string outStr = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit(5000);

            return (true, enable
                ? "已重新启用保留的存储。"
                : "已成功提交关闭【保留的存储】指令！在系统完成下一次维护或更新后将释放约 7GB 磁盘空间。");
        }
        catch (Exception ex)
        {
            return (false, $"设置保留存储失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测传递优化 (Delivery Optimization) P2P 局域网/公网上传是否已被彻底禁用
    /// </summary>
    public bool IsDeliveryOptimizationP2PDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization");
            var val = key?.GetValue("DODownloadMode");
            return val is int intVal && (intVal == 0 || intVal == 99);
        }
        catch { return false; }
    }

    /// <summary>
    /// 彻底禁用或恢复传递优化 P2P 上传分享 (防止后台偷跑上传带宽)
    /// </summary>
    public (bool Success, string Message) SetDeliveryOptimizationP2PDisabled(bool disable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization");
            if (disable)
            {
                // 0 = 仅从 HTTP 源下载，彻底禁止向其他设备或互联网上传
                key.SetValue("DODownloadMode", 0, RegistryValueKind.DWord);
                return (true, "已彻底关闭【传递优化】P2P 上传！禁止向局域网及公网其他 PC 上传更新数据包。");
            }
            else
            {
                key.DeleteValue("DODownloadMode", false);
                return (true, "已恢复传递优化默认设置。");
            }
        }
        catch (Exception ex)
        {
            return (false, $"设置传递优化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测受保护的操作系统隐藏文件是否显示
    /// </summary>
    public bool IsSuperHiddenFilesVisible()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            var val = key?.GetValue("ShowSuperHidden");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置是否显示受保护的操作系统文件 (desktop.ini / boot.ini 等底层系统文件)
    /// </summary>
    public (bool Success, string Message) SetSuperHiddenFilesVisible(bool show)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            key.SetValue("ShowSuperHidden", show ? 1 : 0, RegistryValueKind.DWord);
            return (true, show ? "已设置为【显示受保护的操作系统文件】。" : "已恢复默认隐藏受保护系统文件。");
        }
        catch (Exception ex)
        {
            return (false, $"设置系统文件隐藏失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测是否已配置 Active Hours (活动时间) 与防工作中断自动重启
    /// </summary>
    public bool IsNoAutoRebootConfigured()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
            var val = key?.GetValue("NoAutoRebootWithLoggedOnUsers");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 配置用户登录时不自动重启 (防工作期间因 Windows Update 突发重启导致未保存代码丢失)
    /// </summary>
    public (bool Success, string Message) SetNoAutoReboot(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
            key.SetValue("NoAutoRebootWithLoggedOnUsers", enable ? 1 : 0, RegistryValueKind.DWord);
            return (true, enable
                ? "已开启【用户登录时禁止自动重启】策略！即使更新完成，也不会在您离开工位时擅自重启丢失进度。"
                : "已恢复默认重启策略。");
        }
        catch (Exception ex)
        {
            return (false, $"设置自动重启策略失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测存储感知 (Storage Sense) 自动清理是否开启
    /// </summary>
    public bool IsStorageSenseEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy");
            var val = key?.GetValue("01");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭存储感知 (Storage Sense) 自动清理规则
    /// </summary>
    public (bool Success, string Message) SetStorageSenseEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy");
            key.SetValue("01", enable ? 1 : 0, RegistryValueKind.DWord);
            return (true, enable ? "已开启【存储感知】自动释放空间！" : "已关闭存储感知自动清理。");
        }
        catch (Exception ex)
        {
            return (false, $"设置存储感知失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 UAC 是否开启安全桌面隔离 (PromptOnSecureDesktop)
    /// </summary>
    public bool IsUacSecureDesktopEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            var val = key?.GetValue("PromptOnSecureDesktop");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置 UAC 安全桌面隔离级别 (黑屏高安全隔离 vs 普通窗口不闪黑屏)
    /// </summary>
    public (bool Success, string Message) SetUacSecureDesktop(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            key.SetValue("PromptOnSecureDesktop", enable ? 1 : 0, RegistryValueKind.DWord);
            return (true, enable
                ? "已开启【安全桌面隔离】(UAC 弹窗时黑屏独占，最高安全级别)。"
                : "已关闭【安全桌面隔离】(UAC 弹窗时不黑屏，平滑弹窗)。");
        }
        catch (Exception ex)
        {
            return (false, $"设置 UAC 安全桌面失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测任务栏搜索框模式: 0 = 隐藏, 1 = 仅搜索图标, 2 = 搜索框, 3 = 搜索按钮
    /// </summary>
    public int GetTaskbarSearchMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            var val = key?.GetValue("SearchboxTaskbarMode");
            return val is int intVal ? intVal : 2;
        }
        catch { return 2; }
    }

    /// <summary>
    /// 设置任务栏搜索框模式 (精简任务栏宽度)
    /// </summary>
    public (bool Success, string Message) SetTaskbarSearchMode(int mode)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            key.SetValue("SearchboxTaskbarMode", mode, RegistryValueKind.DWord);
            return (true, mode switch
            {
                0 => "已隐藏任务栏搜索框！(释放最大任务栏宽度)",
                1 => "已将任务栏搜索简化为【仅搜索图标】！",
                _ => "已恢复任务栏完整搜索框。"
            });
        }
        catch (Exception ex)
        {
            return (false, $"设置任务栏搜索模式失败: {ex.Message}");
        }
    }

    #endregion

    #region 步骤二强化：原生开发机 CPU 与磁盘 I/O 性能释放 (WSearch / Defender / SysMain / 空闲维护 / HVCI)

    /// <summary>
    /// 检测 Windows Search (WSearch) 索引服务是否已禁用
    /// </summary>
    public bool IsWSearchDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch");
            var val = key?.GetValue("Start");
            return val is int intVal && intVal == 4;
        }
        catch { return false; }
    }

    /// <summary>
    /// 停用或开启 Windows Search 索引服务
    /// </summary>
    public (bool Success, string Message) SetWSearchDisabled(bool disable)
    {
        try
        {
            var script = disable
                ? "Stop-Service -Name 'WSearch' -Force -ErrorAction SilentlyContinue; Set-Service -Name 'WSearch' -StartupType Disabled -ErrorAction SilentlyContinue"
                : "Set-Service -Name 'WSearch' -StartupType Automatic -ErrorAction SilentlyContinue; Start-Service -Name 'WSearch' -ErrorAction SilentlyContinue";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(6000);

            return (true, disable
                ? "已成功停止并禁用 Windows Search (WSearch) 索引服务！\n避免后台持续扫描源码工程占用 CPU 与磁盘 I/O。"
                : "已恢复 Windows Search 索引服务自动启动。");
        }
        catch (Exception ex) { return (false, $"设置索引服务失败: {ex.Message}"); }
    }

    /// <summary>
    /// 一键清理并释放 Windows.edb 索引数据库历史占用空间
    /// </summary>
    public (bool Success, string Message) CleanWindowsEdb()
    {
        try
        {
            string script = @"
Stop-Service -Name 'WSearch' -Force -ErrorAction SilentlyContinue
$path = 'C:\ProgramData\Microsoft\Search\Data\Applications\Windows'
$freed = 0
if (Test-Path $path) {
    Get-ChildItem -Path $path -File -Force -ErrorAction SilentlyContinue | ForEach-Object {
        $freed += $_.Length
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    }
}
[math]::Round($freed / 1MB, 2)
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string outStr = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "0";
            proc?.WaitForExit(8000);

            return (true, $"已成功清理 Windows Search 索引数据库缓存，已释放约 {outStr} MB 磁盘空间！");
        }
        catch (Exception ex)
        {
            return (false, $"清理索引数据库异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 SysMain (原 Superfetch) 内存预载服务是否已禁用
    /// </summary>
    public bool IsSysMainDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain");
            var val = key?.GetValue("Start");
            return val is int intVal && intVal == 4;
        }
        catch { return false; }
    }

    /// <summary>
    /// 停用或恢复 SysMain 服务
    /// </summary>
    public (bool Success, string Message) SetSysMainDisabled(bool disable)
    {
        try
        {
            var script = disable
                ? "Stop-Service -Name 'SysMain' -Force -ErrorAction SilentlyContinue; Set-Service -Name 'SysMain' -StartupType Disabled -ErrorAction SilentlyContinue"
                : "Set-Service -Name 'SysMain' -StartupType Automatic -ErrorAction SilentlyContinue; Start-Service -Name 'SysMain' -ErrorAction SilentlyContinue";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, disable
                ? "已成功停止并禁用 SysMain 服务！\n在高速 NVMe SSD 开发机上消除了后台无谓的内存页面换进换出与压缩。"
                : "已恢复 SysMain 默认自动运行。");
        }
        catch (Exception ex) { return (false, $"设置 SysMain 异常: {ex.Message}"); }
    }

    /// <summary>
    /// 检测是否已关闭空闲自动维护任务
    /// </summary>
    public bool IsIdleMaintenanceDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance");
            var val = key?.GetValue("MaintenanceDisabled");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭空闲自动维护任务 (防止编译挂机时突然被抢占 CPU)
    /// </summary>
    public (bool Success, string Message) SetIdleMaintenanceDisabled(bool disable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance");
            key.SetValue("MaintenanceDisabled", disable ? 1 : 0, RegistryValueKind.DWord);
            return (true, disable
                ? "已成功关闭【空闲自动维护】！\n防止系统在无人操作或长耗时编译挂机时突然抢占 CPU 和磁盘 I/O。"
                : "已恢复系统默认自动维护策略。");
        }
        catch (Exception ex) { return (false, $"设置自动维护失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 Defender 实时防护监控是否已关闭
    /// </summary>
    public bool IsDefenderRealtimeDisabled()
    {
        try
        {
            // 先读组策略注册表
            using var polKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
            if (polKey?.GetValue("DisableRealtimeMonitoring") is int intVal && intVal == 1) return true;

            // 调用快速查询
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"(Get-MpPreference).DisableRealtimeMonitoring\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string outStr = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(3000);
            return outStr.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 Defender 实时监控
    /// </summary>
    public (bool Success, string Message) SetDefenderRealtimeDisabled(bool disable)
    {
        try
        {
            var script = disable
                ? "Set-MpPreference -DisableRealtimeMonitoring $true"
                : "Set-MpPreference -DisableRealtimeMonitoring $false";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            // 同时写入策略键
            using var polKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
            polKey.SetValue("DisableRealtimeMonitoring", disable ? 1 : 0, RegistryValueKind.DWord);

            return (true, disable
                ? "已关闭 Defender 实时监控！\n彻底解除 MsMpEng.exe 对编译器与中间小文件的句柄劫持扫描。\n(注：Windows 篡改防护若开启可能会在重启后提示还原)"
                : "已恢复 Defender 实时监控。");
        }
        catch (Exception ex) { return (false, $"设置 Defender 实时防护失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 SmartScreen 筛选器是否已禁用
    /// </summary>
    public bool IsSmartScreenDisabled()
    {
        try
        {
            using var sysKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
            var val = sysKey?.GetValue("EnableSmartScreen");
            if (val is int intVal && intVal == 0) return true;
            if (val is string strVal && (strVal.Equals("Off", StringComparison.OrdinalIgnoreCase) || strVal == "0")) return true;

            using var userKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AppHost");
            var ev = userKey?.GetValue("EnableWebContentEvaluation");
            return ev is int evVal && evVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 SmartScreen 筛选器
    /// </summary>
    public (bool Success, string Message) SetSmartScreenDisabled(bool disable)
    {
        try
        {
            using var sysKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
            sysKey.SetValue("EnableSmartScreen", disable ? 0 : 1, RegistryValueKind.DWord);

            using var userKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\AppHost");
            userKey.SetValue("EnableWebContentEvaluation", disable ? 0 : 1, RegistryValueKind.DWord);

            return (true, disable
                ? "已关闭 SmartScreen 筛选器！\n运行自编译可执行文件或通过命令行下载工具链时不再触发阻断拦截。"
                : "已恢复 SmartScreen 筛选器。");
        }
        catch (Exception ex) { return (false, $"设置 SmartScreen 失败: {ex.Message}"); }
    }

    /// <summary>
    /// 一键为 Windows Defender 注入原生开发目录与编译器进程白名单 (安全与性能兼顾方案)
    /// </summary>
    public (bool Success, string Message) InjectNativeDevExclusions()
    {
        try
        {
            string script = @"
$paths = @('C:\02Programmer', 'C:\Dev', 'D:\Dev', 'D:\Code', 'C:\Users\*\AppData\Local\Temp')
$procs = @('cl.exe', 'csc.exe', 'dotnet.exe', 'rustc.exe', 'cargo.exe', 'go.exe', 'gcc.exe', 'g++.exe', 'msbuild.exe', 'ninja.exe', 'cmake.exe', 'link.exe')
foreach ($p in $paths) {
    if (Test-Path ($p.Replace('*', $env:USERNAME))) {
        Add-MpPreference -ExclusionPath $p -ErrorAction SilentlyContinue
    }
}
Add-MpPreference -ExclusionProcess $procs -ErrorAction SilentlyContinue
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(6000);

            return (true, "已成功将常用开发目录 (C:\\02Programmer, C:\\Dev, D:\\Dev 等) 与 12 种主流编译器进程注入 Defender 豁免白名单！\n编译与构建速度将显著提升。");
        }
        catch (Exception ex)
        {
            return (false, $"注入开发白名单失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检测 HVCI 内存完整性 (HypervisorEnforcedCodeIntegrity) 是否已关闭
    /// </summary>
    public bool IsHvciDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity");
            var val = key?.GetValue("Enabled");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 HVCI 内存完整性 (关闭可消除虚拟化内核层带来的 5%~15% 原生性能折损)
    /// </summary>
    public (bool Success, string Message) SetHvciDisabled(bool disable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity");
            key.SetValue("Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
            return (true, disable
                ? "已关闭 HVCI 内存完整性策略！(需重启电脑生效)\n消除基于虚拟化的安全内核隔离对高频系统调用的性能折损。"
                : "已恢复 HVCI 内存完整性开启。(需重启生效)");
        }
        catch (Exception ex) { return (false, $"设置 HVCI 失败: {ex.Message}"); }
    }

    #endregion

    #region 11. NTFS 底层文件系统吞吐调优 (8.3 短文件名 / 访问时间戳 / 缓存扩充)

    /// <summary>
    /// 检测 NTFS 是否已全局禁用 8.3 短文件名生成
    /// </summary>
    public bool IsNtfs8dot3Disabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
            var val = key?.GetValue("NtfsDisable8dot3NameCreation");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 NTFS 8.3 短文件名生成 (禁用后消除 node_modules 与源码目录海量小文件的目录哈希碰撞)
    /// </summary>
    public (bool Success, string Message) SetNtfs8dot3Disabled(bool disable)
    {
        try
        {
            int val = disable ? 1 : 2; // 1: 禁用所有卷; 2: 系统默认按卷
            using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
            {
                key.SetValue("NtfsDisable8dot3NameCreation", val, RegistryValueKind.DWord);
            }

            // 同步通过原生 fsutil 生效驱动层配置
            var psi = new ProcessStartInfo
            {
                FileName = "fsutil.exe",
                Arguments = $"8dot3name set {(disable ? "1" : "2")}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);

            return (true, disable
                ? "已成功全局禁用 NTFS 8.3 短文件名生成！\n消除海量长文件名文件在创建时的冲突散列计算，提升工程目录构建吞吐。"
                : "已恢复 NTFS 8.3 短文件名默认配置。");
        }
        catch (Exception ex) { return (false, $"配置 8.3 短文件名失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测是否已禁用 NTFS 最后访问时间戳更新 (NtfsDisableLastAccessUpdate)
    /// </summary>
    public bool IsNtfsLastAccessDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
            var val = key?.GetValue("NtfsDisableLastAccessUpdate");
            return val is int intVal && (intVal == 1 || intVal == 3);
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 NTFS 最后访问时间戳更新 (关闭后避免纯读文件触发元数据写盘放大)
    /// </summary>
    public (bool Success, string Message) SetNtfsLastAccessDisabled(bool disable)
    {
        try
        {
            int val = disable ? 1 : 2; // 1: 禁用更新; 2: 恢复系统托管
            using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
            {
                key.SetValue("NtfsDisableLastAccessUpdate", val, RegistryValueKind.DWord);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "fsutil.exe",
                Arguments = $"behavior set disablelastaccess {(disable ? "1" : "2")}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);

            return (true, disable
                ? "已成功禁用 NTFS 最后访问时间戳记录！\n读取源码或编译时不再向 SSD 反写时间戳元数据，杜绝 I/O 放大。"
                : "已恢复 NTFS 访问时间戳系统托管模式。");
        }
        catch (Exception ex) { return (false, $"配置访问时间戳失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 NTFS 物理内存主缓存池是否已扩充 (NtfsMemoryUsage = 2)
    /// </summary>
    public bool IsNtfsMemoryUsageIncreased()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
            var val = key?.GetValue("NtfsMemoryUsage");
            return val is int intVal && intVal == 2;
        }
        catch { return false; }
    }

    /// <summary>
    /// 扩充或恢复 NTFS 文件系统元数据内存缓存池
    /// </summary>
    public (bool Success, string Message) SetNtfsMemoryUsageIncreased(bool increase)
    {
        try
        {
            int val = increase ? 2 : 1;
            using (var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
            {
                key.SetValue("NtfsMemoryUsage", val, RegistryValueKind.DWord);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "fsutil.exe",
                Arguments = $"behavior set memoryusage {(increase ? "2" : "1")}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);

            return (true, increase
                ? "已成功扩充 NTFS 文件系统元数据内存缓存池！(需重启生效)\n显著提升大量小文件的 MFT 检索与缓存命中率。"
                : "已恢复 NTFS 默认内存缓存配置。(需重启生效)");
        }
        catch (Exception ex) { return (false, $"配置 NTFS 缓存失败: {ex.Message}"); }
    }

    #endregion

    #region 12. 内存管理、CPU 调度与 Localhost 网络栈 (MemoryCompression / Win32Priority / TCP / WER)

    /// <summary>
    /// 检测 Windows 内存压缩是否已关闭
    /// </summary>
    public bool IsMemoryCompressionDisabled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"(Get-MMAgent).MemoryCompression\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(3000);

            return output.Equals("False", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 Windows 内存压缩机制 (适用于 32GB+ 充裕内存开发机，消除 CPU 实时压缩开销)
    /// </summary>
    public (bool Success, string Message) SetMemoryCompressionDisabled(bool disable)
    {
        try
        {
            string cmd = disable ? "Disable-MMAgent -MemoryCompression" : "Enable-MMAgent -MemoryCompression";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{cmd}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            return (true, disable
                ? "已成功禁用 Windows 内存压缩！(需重启生效)\n大内存机器不再耗费 CPU 核心周期压缩页面，杜绝多核满载编译时的微卡顿。"
                : "已恢复 Windows 默认内存压缩机制。(需重启生效)");
        }
        catch (Exception ex) { return (false, $"配置内存压缩失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 CPU 线程调度配额是否已针对原生编译长进程优化 (Win32PrioritySeparation)
    /// </summary>
    public bool IsWin32PriorityOptimizedForDev()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl");
            var val = key?.GetValue("Win32PrioritySeparation");
            return val is int intVal && (intVal == 0x28 || intVal == 0x24 || intVal == 0x18);
        }
        catch { return false; }
    }

    /// <summary>
    /// 配置 CPU 时间片配额 (0x28: 固定平权长配额，窗口失焦切换时后台编译绝不被系统降权)
    /// </summary>
    public (bool Success, string Message) SetWin32PriorityOptimizedForDev(bool optimize)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl");
            key.SetValue("Win32PrioritySeparation", optimize ? 0x28 : 0x2, RegistryValueKind.DWord);

            return (true, optimize
                ? "已开启 CPU 编译平权长配额调度 (0x28)！\n当您切换到浏览器或文档时，后台的 MSBuild/Cargo/Ninja 等编译工具链不再被系统剥夺 CPU 配额。"
                : "已恢复桌面默认前台动态高加速调度 (0x2)。");
        }
        catch (Exception ex) { return (false, $"配置 CPU 优先级失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 Localhost 网络栈高并发端口复用是否已调优 (TcpTimedWaitDelay & MaxUserPort)
    /// </summary>
    public bool IsTcpPortReuseOptimized()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
            var delay = key?.GetValue("TcpTimedWaitDelay");
            var port = key?.GetValue("MaxUserPort");
            return (delay is int d && d <= 30) && (port is int p && p >= 65534);
        }
        catch { return false; }
    }

    /// <summary>
    /// 优化 Localhost 网络栈 (TcpTimedWaitDelay 缩至 30s，MaxUserPort 提升至 65534，杜绝 TIME_WAIT 端口耗尽)
    /// </summary>
    public (bool Success, string Message) SetTcpPortReuseOptimized(bool optimize)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
            if (optimize)
            {
                key.SetValue("TcpTimedWaitDelay", 30, RegistryValueKind.DWord);
                key.SetValue("MaxUserPort", 65534, RegistryValueKind.DWord);
                key.SetValue("StrictTimeWaitCreation", 1, RegistryValueKind.DWord);
                return (true, "已成功调优 TCP 网络栈！\nTIME_WAIT 回收缩短至 30 秒，动态端口池扩大至 65534，彻底杜绝本地微服务与热重载端口耗尽 (10055)。");
            }
            else
            {
                key.DeleteValue("TcpTimedWaitDelay", throwOnMissingValue: false);
                key.DeleteValue("MaxUserPort", throwOnMissingValue: false);
                key.DeleteValue("StrictTimeWaitCreation", throwOnMissingValue: false);
                return (true, "已恢复 Windows 默认 TCP 协议栈超时参数。");
            }
        }
        catch (Exception ex) { return (false, $"配置 TCP 参数失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 Windows 错误报告 (WER Watson) 是否已拦截禁用
    /// </summary>
    public bool IsWerDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
            var dis = key?.GetValue("Disabled");
            var ui = key?.GetValue("DontShowUI");
            return (dis is int d && d == 1) && (ui is int u && u == 1);
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 Windows 错误报告与弹窗 (开发调试崩溃秒级由 IDE/WinDbg 接管，不挂起进程写大 dump)
    /// </summary>
    public (bool Success, string Message) SetWerDisabled(bool disable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting");
            key.SetValue("Disabled", disable ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue("DontShowUI", disable ? 1 : 0, RegistryValueKind.DWord);

            return (true, disable
                ? "已禁用 Windows 错误报告 (WER) 与 Watson 上报！\n原生调试崩溃时不再挂起进程生成巨型转储，直接秒级返回给调试器。"
                : "已恢复 Windows 错误报告默认上报。");
        }
        catch (Exception ex) { return (false, $"配置 WER 失败: {ex.Message}"); }
    }

    #endregion

    #region 13. 系统响应与内核不分页注册表极致调优 (HungApp / MenuShowDelay / DisablePagingExecutive)

    /// <summary>
    /// 检测系统挂起超时与卡死强制结束优化是否已启用
    /// </summary>
    public bool IsHungAppTimeoutOptimized()
    {
        try
        {
            using var userKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            string? hung = userKey?.GetValue("HungAppTimeout") as string;
            return hung == "1000";
        }
        catch { return false; }
    }

    /// <summary>
    /// 优化系统卡死超时 (HungAppTimeout 1s, WaitToKillApp 2s, WaitToKillService 2s)
    /// 【安全策略】：严格保持 AutoEndTasks = 0，确保关机时弹出未保存工作提示，绝不静默强杀！
    /// </summary>
    public (bool Success, string Message) SetHungAppTimeoutOptimized(bool optimize)
    {
        try
        {
            using (var userKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
            {
                userKey.SetValue("HungAppTimeout", optimize ? "1000" : "5000", RegistryValueKind.String);
                userKey.SetValue("WaitToKillAppTimeout", optimize ? "2000" : "20000", RegistryValueKind.String);
                // 确保安全基线：绝不静默强杀，保持 AutoEndTasks = 0 允许关机前保存
                userKey.SetValue("AutoEndTasks", "0", RegistryValueKind.String);
            }

            using (var machKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control"))
            {
                machKey.SetValue("WaitToKillServiceTimeout", optimize ? "2000" : "5000", RegistryValueKind.String);
            }

            return (true, optimize
                ? "已启用进程挂起超时与快速响应优化！\n未响应应用判定缩至 1 秒，关机服务超时缩至 2 秒。\n【安全承诺】保持 AutoEndTasks=0，关机遇未保存工作仍会弹窗询问，绝不静默强杀。"
                : "已恢复系统默认未响应判定 (5s) 与关机等待时长 (5s/20s)。");
        }
        catch (Exception ex) { return (false, $"配置超时失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测关机/注销时是否启用静默强杀未响应任务 (AutoEndTasks = 1)
    /// </summary>
    public bool IsAutoEndTasksEnabled()
    {
        try
        {
            using var userKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            string? val = userKey?.GetValue("AutoEndTasks") as string;
            return val == "1";
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭关机静默强杀未响应任务 (AutoEndTasks)
    /// ⚠️ 高危操作：若 IDE 或文档有未保存内容，将被直接杀掉导致丢失！
    /// </summary>
    public (bool Success, string Message) SetAutoEndTasksEnabled(bool enable)
    {
        try
        {
            using var userKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            userKey.SetValue("AutoEndTasks", enable ? "1" : "0", RegistryValueKind.String);
            return (true, enable
                ? "⚠️ 已开启关机静默强杀任务 (AutoEndTasks=1)！\n关机或注销时，系统将不再弹窗询问，直接强制杀死所有未响应进程（存在未保存工作丢失风险）。"
                : "已恢复关机安全防护 (AutoEndTasks=0)。关机遇未响应或未保存程序将正常弹窗提示。");
        }
        catch (Exception ex) { return (false, $"配置 AutoEndTasks 失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测菜单展开悬停延迟是否已归零 (MenuShowDelay = 0)
    /// </summary>
    public bool IsMenuShowDelayZero()
    {
        try
        {
            using var userKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            string? val = userKey?.GetValue("MenuShowDelay") as string;
            return val == "0";
        }
        catch { return false; }
    }

    /// <summary>
    /// 设置菜单展开悬停延迟为 0 毫秒 (即点即开零迟滞)
    /// </summary>
    public (bool Success, string Message) SetMenuShowDelayZero(bool zero)
    {
        try
        {
            using var userKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
            userKey.SetValue("MenuShowDelay", zero ? "0" : "400", RegistryValueKind.String);

            return (true, zero
                ? "已将菜单悬停延迟调至 0 毫秒！\n右键菜单与级联子菜单秒级弹出，彻底消除 400ms 人为等待迟滞。"
                : "已恢复系统默认 400 毫秒悬停延迟。");
        }
        catch (Exception ex) { return (false, $"配置菜单延迟失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测系统核心执行体与驱动是否已强制常驻物理内存 (DisablePagingExecutive)
    /// </summary>
    public bool IsPagingExecutiveDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            var val = key?.GetValue("DisablePagingExecutive");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 强制系统内核与驱动常驻物理内存不分页 (消除换入换出 DPC 延迟)
    /// </summary>
    public (bool Success, string Message) SetPagingExecutiveDisabled(bool disablePaging)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            key.SetValue("DisablePagingExecutive", disablePaging ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue("LargeSystemCache", disablePaging ? 1 : 0, RegistryValueKind.DWord);

            return (true, disablePaging
                ? "已启用内核驱动常驻物理内存策略 (DisablePagingExecutive=1)！(需重启生效)\n强制系统内核代码与驱动绝不换出至磁盘，消除微卡顿与 DPC 延迟。"
                : "已恢复内核分页策略系统托管。(需重启生效)");
        }
        catch (Exception ex) { return (false, $"配置内核常驻内存失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 Windows Update 是否已禁止在有用户登录时强制自动重启
    /// </summary>
    public bool IsAutoRebootDisabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
            var val = key?.GetValue("NoAutoRebootWithLoggedOnUsers");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 Windows Update 登录态免自动重启 (开发构建与长时间运算不被打断)
    /// </summary>
    public (bool Success, string Message) SetAutoRebootDisabled(bool disable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU");
            key.SetValue("NoAutoRebootWithLoggedOnUsers", disable ? 1 : 0, RegistryValueKind.DWord);
            if (disable)
            {
                key.SetValue("AlwaysAutoRebootAtScheduledTime", 0, RegistryValueKind.DWord);
            }
            return (true, disable
                ? "已开启【Windows Update 登录态免重启保护】！\n只要有用户登录系统，Windows Update 绝不会强制重启打断编译或任务。"
                : "已恢复 Windows Update 默认自动重启策略。");
        }
        catch (Exception ex) { return (false, $"配置 Windows Update 重启策略失败: {ex.Message}"); }
    }

    /// <summary>
    /// 检测 GameDVR / 屏幕录制后台捕获是否已禁用
    /// </summary>
    public bool IsGameDvrDisabled()
    {
        try
        {
            using var cuKey = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore");
            var val = cuKey?.GetValue("GameDVR_Enabled");
            if (val is int intVal && intVal == 0) return true;

            using var lmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR");
            var allow = lmKey?.GetValue("AllowGameDVR");
            return allow is int allowVal && allowVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 GameDVR 与后台捕获 (消除 DirectX/Vulkan 渲染管道后台钩子开销)
    /// </summary>
    public (bool Success, string Message) SetGameDvrDisabled(bool disable)
    {
        try
        {
            using (var cuKey = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore"))
            {
                cuKey.SetValue("GameDVR_Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            using (var lmKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR"))
            {
                lmKey.SetValue("AllowGameDVR", disable ? 0 : 1, RegistryValueKind.DWord);
            }
            using (var dvrKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"))
            {
                dvrKey.SetValue("AppCaptureEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
            }

            return (true, disable
                ? "已禁用 GameDVR 与后台录屏捕获！\n剥离图形渲染管道 Direct3D/Vulkan 后台截取钩子，释放 GPU 运算与渲染性能。"
                : "已恢复 GameDVR 默认配置。");
        }
        catch (Exception ex) { return (false, $"配置 GameDVR 失败: {ex.Message}"); }
    }

    #endregion

    #region 14. 系统还原点与备份安全机制 (后悔药机制)

    /// <summary>
    /// 解除系统还原点创建频率限制并创建还原点
    /// </summary>
    public (bool Success, string Message) CreateSystemRestorePoint(string description)
    {
        try
        {
            // 1. 解除 24 小时内只能建一次的频率限制
            using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore"))
            {
                key.SetValue("SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord);
            }

            // 2. 确保 C 盘已启用还原保护，并创建还原点
            string script = $@"
try {{
    Enable-ComputerRestore -Drive 'C:\' -ErrorAction SilentlyContinue
    Checkpoint-Computer -Description '{description}' -RestorePointType 'APPLICATION_INSTALL' -ErrorAction Stop
    Write-Output 'SUCCESS'
}} catch {{
    Write-Output $_.Exception.Message
}}";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(15000);

            if (output.Contains("SUCCESS"))
            {
                return (true, $"系统还原点【{description}】创建成功！\n如需回退，可随时点击【启动系统还原向导】一键恢复系统状态。");
            }
            else
            {
                return (false, $"创建还原点失败: {output}\n提示：请确保系统磁盘保护已开启且拥有足够磁盘配额。");
            }
        }
        catch (Exception ex)
        {
            return (false, $"创建系统还原点异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开系统还原向导 (rstrui.exe)
    /// </summary>
    public void OpenSystemRestoreWizard()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "rstrui.exe", UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// 打开系统保护配置窗口 (SystemPropertiesProtection.exe)
    /// </summary>
    public void OpenSystemProtectionSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "SystemPropertiesProtection.exe", UseShellExecute = true });
        }
        catch { }
    }

    #endregion

    #region 15. 系统激活与许可证排查 (slmgr.vbs)

    /// <summary>
    /// 查询 Windows 激活到期状态 (slmgr.vbs /xpr)
    /// </summary>
    public (bool Success, string Message) QueryActivationExpiry()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cscript.exe",
                Arguments = @"//nologo C:\Windows\System32\slmgr.vbs /xpr",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(6000);

            return (true, string.IsNullOrWhiteSpace(output) ? "未查询到激活到期信息。" : output);
        }
        catch (Exception ex) { return (false, $"查询激活到期状态失败: {ex.Message}"); }
    }

    /// <summary>
    /// 查询 Windows 许可证详细信息 (slmgr.vbs /dli)
    /// </summary>
    public (bool Success, string Message) QueryLicenseDetails()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cscript.exe",
                Arguments = @"//nologo C:\Windows\System32\slmgr.vbs /dli",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(6000);

            return (true, string.IsNullOrWhiteSpace(output) ? "未查询到许可证信息。" : output);
        }
        catch (Exception ex) { return (false, $"查询许可证失败: {ex.Message}"); }
    }

    /// <summary>
    /// 打开官方激活设置页
    /// </summary>
    public void OpenActivationSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "ms-settings:activation", UseShellExecute = true });
        }
        catch { }
    }

    #endregion

    #region 16. 本地账户与登录管理 (netplwiz / 免密自动登录 / OOBE)

    /// <summary>
    /// 检测 netplwiz 自动登录复选框是否已还原展示 (DevicePasswordLessBuildVersion = 0)
    /// </summary>
    public bool IsNetplwizAutoLogonCheckboxRestored()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device");
            var val = key?.GetValue("DevicePasswordLessBuildVersion");
            return val is int intVal && intVal == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// 还原或隐藏 netplwiz 中的【要使用本计算机，用户必须输入用户名和密码】复选框
    /// </summary>
    public (bool Success, string Message) SetNetplwizAutoLogonCheckbox(bool restore)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device");
            key.SetValue("DevicePasswordLessBuildVersion", restore ? 0 : 2, RegistryValueKind.DWord);

            return (true, restore
                ? "已成功还原 netplwiz 自动登录复选框！\n打开用户账户窗口后，取消勾选【要使用本计算机，用户必须输入用户名和密码】并输入密码，即可实现开机跳过密码直达桌面。"
                : "已恢复微软无密码现代登录策略限制 (隐藏该复选框)。");
        }
        catch (Exception ex) { return (false, $"设置 netplwiz 复选框失败: {ex.Message}"); }
    }

    /// <summary>
    /// 打开用户账户控制面板 (netplwiz)
    /// </summary>
    public void OpenNetplwiz()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "netplwiz.exe", UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// 打开本地用户和组管理 (lusrmgr.msc)
    /// </summary>
    public void OpenLusrmgr()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "lusrmgr.msc", UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// 打开 Windows 登录选项与 Windows Hello 设置
    /// </summary>
    public void OpenSignInOptions()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "ms-settings:signinoptions", UseShellExecute = true });
        }
        catch { }
    }

    #endregion

    #region 17. 虚拟内存与分页文件 (pagefile.sys) 调优

    /// <summary>
    /// 打开系统属性高级性能选项 (虚拟内存设置页)
    /// </summary>
    public void OpenVirtualMemorySettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "SystemPropertiesPerformance.exe", Arguments = "3", UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// 检测关机时是否自动清空虚拟内存页面文件 (ClearPageFileAtShutdown)
    /// </summary>
    public bool IsClearPageFileAtShutdownEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            var val = key?.GetValue("ClearPageFileAtShutdown");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭关机清空页面文件 (安全抹除敏感 RAM 碎片，但略微增加关机时间)
    /// </summary>
    public (bool Success, string Message) SetClearPageFileAtShutdown(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
            key.SetValue("ClearPageFileAtShutdown", enable ? 1 : 0, RegistryValueKind.DWord);

            return (true, enable
                ? "已启用关机清空页面文件！(关机时擦除 pagefile.sys 敏感内存数据，保障高密数据安全)"
                : "已恢复默认不清除页面文件 (关机速度更快)。");
        }
        catch (Exception ex) { return (false, $"配置页面文件策略失败: {ex.Message}"); }
    }

    #endregion

    #region 18. 物理磁盘 SMART 健康状态检测

    /// <summary>
    /// 查询系统中所有物理硬盘的 SMART 健康状态、介质类型与总线
    /// </summary>
    public (bool Success, string Output) QueryPhysicalDisksHealth()
    {
        try
        {
            string script = @"
Get-PhysicalDisk | Select-Object DeviceId, FriendlyName, MediaType, BusType, HealthStatus, OperationalStatus, @{Name='SizeGB';Expression={[math]::Round($_.Size/1GB, 1)}} | Format-Table -AutoSize | Out-String -Width 120
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(8000);

            return (true, string.IsNullOrWhiteSpace(output) ? "未查询到物理磁盘信息。" : output);
        }
        catch (Exception ex) { return (false, $"查询磁盘 SMART 健康失败: {ex.Message}"); }
    }

    #endregion

    #region 19. 打印服务队列卡死一键修复

    /// <summary>
    /// 清空卡死的打印队列缓存并安全重启 Print Spooler 服务
    /// </summary>
    public (bool Success, string Message) ClearPrintQueueAndRestartSpooler()
    {
        try
        {
            string script = @"
Stop-Service -Name Spooler -Force -ErrorAction SilentlyContinue
Remove-Item -Path '$env:SystemRoot\System32\spool\PRINTERS\*' -Force -Recurse -ErrorAction SilentlyContinue
Start-Service -Name Spooler
Write-Output 'DONE'
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string output = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(8000);

            return (true, "已成功停止打印后台服务、彻底清空卡死的 PRINTERS 队列缓存，并重新启动 Print Spooler！\n卡死的打印任务已全部注销。");
        }
        catch (Exception ex) { return (false, $"清空打印队列失败: {ex.Message}"); }
    }

    #endregion

    #region 20. 音频与蓝牙子系统快速排错与服务重启

    /// <summary>
    /// 一键重启 Windows 原生音频服务 (AudioSrv 与 AudioEndpointBuilder)
    /// </summary>
    public (bool Success, string Message) RestartAudioSubsystem()
    {
        try
        {
            string script = @"
Restart-Service -Name AudioEndpointBuilder -Force -ErrorAction SilentlyContinue
Restart-Service -Name Audiosrv -Force -ErrorAction SilentlyContinue
Write-Output 'DONE'
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(8000);

            return (true, "已重启 Windows 音频终端生成器 (AudioEndpointBuilder) 与音频服务 (Audiosrv)！\n驱动挂死或无声故障已重置。");
        }
        catch (Exception ex) { return (false, $"重启音频服务失败: {ex.Message}"); }
    }

    /// <summary>
    /// 一键重启蓝牙支持服务 (bthserv)
    /// </summary>
    public (bool Success, string Message) RestartBluetoothService()
    {
        try
        {
            string script = @"
Restart-Service -Name bthserv -Force -ErrorAction SilentlyContinue
Write-Output 'DONE'
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(8000);

            return (true, "已成功重启蓝牙支持服务 (bthserv)！\n可尝试重新发起设备配对或连接。");
        }
        catch (Exception ex) { return (false, $"重启蓝牙服务失败: {ex.Message}"); }
    }

    #endregion

    #region 21. 双系统时差修复与 w32tm 时间强制同步

    /// <summary>
    /// 检测是否已开启双系统 UTC 硬件时钟同步 (解决 Windows 与 Linux 差 8 小时问题)
    /// </summary>
    public bool IsRealTimeIsUniversalEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TimeZoneInformation");
            var val = key?.GetValue("RealTimeIsUniversal");
            return val is int intVal && intVal == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// 开启或关闭 RealTimeIsUniversal (双系统时差一键修复)
    /// </summary>
    public (bool Success, string Message) SetRealTimeIsUniversal(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\TimeZoneInformation");
            if (enable)
            {
                key.SetValue("RealTimeIsUniversal", 1, RegistryValueKind.DWord);
                return (true, "已开启【双系统 UTC 硬件时钟模式 (RealTimeIsUniversal=1)】！\n主板 BIOS/RTC 将以 UTC 计数，彻底终结 Windows 与 Linux 双系统切换时时钟相差 8 小时的顽疾。");
            }
            else
            {
                key.DeleteValue("RealTimeIsUniversal", false);
                return (true, "已恢复 Windows 默认本地时间 (Local Time) 主板硬件时钟模式。");
            }
        }
        catch (Exception ex) { return (false, $"设置时区时钟失败: {ex.Message}"); }
    }

    /// <summary>
    /// 强制与 Windows / NTP 网络时间服务器立即同步 (w32tm /resync /force)
    /// </summary>
    public (bool Success, string Output) ResyncNetworkTime()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "w32tm.exe",
                Arguments = "/resync /force",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            string std = proc?.StandardOutput.ReadToEnd()?.Trim() ?? "";
            string err = proc?.StandardError.ReadToEnd()?.Trim() ?? "";
            proc?.WaitForExit(8000);

            string combined = (std + "\n" + err).Trim();
            return (proc?.ExitCode == 0, string.IsNullOrWhiteSpace(combined) ? "时间同步指令已成功发送。" : combined);
        }
        catch (Exception ex) { return (false, $"时间同步失败: {ex.Message}"); }
    }

    #endregion

    #region 22. 语言、输入法与区域控制台

    /// <summary>
    /// 打开 Windows 现代语言和区域设置 (ms-settings:regionlanguage)
    /// </summary>
    public void OpenRegionLanguageSettings()
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true }); } catch { }
    }

    /// <summary>
    /// 打开 Windows 可选功能设置 (ms-settings:optionalfeatures)
    /// </summary>
    public void OpenOptionalFeaturesSettings()
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:optionalfeatures") { UseShellExecute = true }); } catch { }
    }

    /// <summary>
    /// 打开经典区域控制台 (intl.cpl)
    /// </summary>
    public void OpenIntlCpl()
    {
        try { Process.Start(new ProcessStartInfo("intl.cpl") { UseShellExecute = true }); } catch { }
    }

    #endregion
}
