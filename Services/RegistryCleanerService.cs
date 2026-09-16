using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AgyToolbox.Services;

/// <summary>
/// 注册表卸载残留项实体模型
/// </summary>
public class RegistryResidualItem
{
    public bool IsSelected { get; set; } = true;
    public string Category { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string RootKeyName { get; set; } = "";
    public string SubKeyPath { get; set; } = "";
    public string ValueName { get; set; } = "";
    public string DeadPath { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool IsSubKeyDeletion { get; set; } // true: 删除整个子键; false: 删除特定值项

    public string FullKeyPath => $"{RootKeyName}\\{SubKeyPath}";
}

/// <summary>
/// 软件卸载残留注册表深度扫描与清理服务
/// 遵循纯血原生 Windows NT 机制，无第三方依赖，严格保护系统核心注册表项
/// </summary>
public class RegistryCleanerService
{
    // 系统级受保护白名单，绝对禁止误触
    private static readonly HashSet<string> ProtectedSubKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft", "Windows", "Windows NT", "Windows Defender", "System", "Classes",
        "Policies", "CurrentControlSet", "Hardware", "SAM", "SECURITY", "SOFTWARE"
    };

    /// <summary>
    /// 异步扫描所有已卸载软件的注册表残留信息
    /// </summary>
    public async Task<List<RegistryResidualItem>> ScanResidualsAsync()
    {
        return await Task.Run(() =>
        {
            var results = new List<RegistryResidualItem>();

            // 1. 扫描无效卸载项 (控制面板/设置中的幽灵应用与残留安装条目)
            ScanOrphanedUninstallEntries(results);

            // 2. 扫描无效自启动项残留 (Run / RunOnce 中指向已删除可执行文件的条目)
            ScanDeadStartupEntries(results);

            // 3. 扫描 MuiCache 历史执行路径残留 (已删除软件留下的友好名称与执行缓存)
            ScanDeadMuiCache(results);

            // 4. 扫描 HKCU\Software\Classes 中的死链打开方式与 ProgID
            ScanDeadFileAssociations(results);

            return results;
        });
    }

    #region 1. 无效卸载项扫描

    private void ScanOrphanedUninstallEntries(List<RegistryResidualItem> results)
    {
        // 扫描三处 Uninstall 键：HKLM (64位与32位) 以及 HKCU
        var uninstallKeys = new[]
        {
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM (32位)"),
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", "HKCU")
        };

        foreach (var (root, path, rootLabel) in uninstallKeys)
        {
            try
            {
                using var baseKey = root.OpenSubKey(path);
                if (baseKey == null) continue;

                foreach (var subKeyName in baseKey.GetSubKeyNames())
                {
                    try
                    {
                        using var appKey = baseKey.OpenSubKey(subKeyName);
                        if (appKey == null) continue;

                        // 过滤系统组件、KB补丁与语言包
                        var sysComp = appKey.GetValue("SystemComponent");
                        if (sysComp is int s && s == 1) continue;

                        string displayName = appKey.GetValue("DisplayName") as string ?? "";
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            // 若没有 DisplayName 且名称不是 GUID，跳过
                            if (!subKeyName.StartsWith("{")) continue;
                            displayName = subKeyName;
                        }

                        // 跳过 Windows 核心与更新补丁
                        if (displayName.StartsWith("Security Update", StringComparison.OrdinalIgnoreCase) ||
                            displayName.StartsWith("Update for Windows", StringComparison.OrdinalIgnoreCase) ||
                            displayName.StartsWith("KB", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string installLocation = appKey.GetValue("InstallLocation") as string ?? "";
                        string uninstallString = appKey.GetValue("UninstallString") as string ?? "";
                        string displayIcon = appKey.GetValue("DisplayIcon") as string ?? "";

                        bool hasInstallLoc = !string.IsNullOrWhiteSpace(installLocation);
                        bool hasUninstallStr = !string.IsNullOrWhiteSpace(uninstallString);

                        // 如果完全没有卸载字符串也没有安装路径，且没有图标，无法判定，跳过
                        if (!hasInstallLoc && !hasUninstallStr) continue;

                        var (hasUninstallExe, uninstallExe, uninstallExists) = ExtractAndCheckPath(uninstallString);
                        var (hasIconExe, iconExe, iconExists) = ExtractAndCheckPath(displayIcon);

                        bool installLocExists = hasInstallLoc && Directory.Exists(installLocation);

                        // 判定孤儿规则：
                        // 1. 明确声明了 InstallLocation 但该目录在物理磁盘上已不存在，且 UninstallString 指向的文件亦不存在
                        // 2. 或未声明 InstallLocation，但明确声明了包含 .exe 路径的 UninstallString 且该可执行文件已物理不存在
                        bool isOrphan = false;
                        string deadPath = "";
                        string reason = "";

                        if (hasInstallLoc && !installLocExists)
                        {
                            if (hasUninstallExe && !uninstallExists)
                            {
                                isOrphan = true;
                                deadPath = installLocation;
                                reason = $"安装目录与卸载程序均已不存在 ({deadPath})";
                            }
                            else if (!hasUninstallExe && !hasIconExe)
                            {
                                isOrphan = true;
                                deadPath = installLocation;
                                reason = $"安装目录已物理删除 ({deadPath})";
                            }
                        }
                        else if (!hasInstallLoc && hasUninstallExe && !uninstallExists)
                        {
                            // 如果还有 DisplayIcon 且存在，可能只是快捷卸载丢了
                            if (!hasIconExe || !iconExists)
                            {
                                isOrphan = true;
                                deadPath = uninstallExe;
                                reason = $"卸载程序已物理删除 ({deadPath})";
                            }
                        }

                        if (isOrphan)
                        {
                            results.Add(new RegistryResidualItem
                            {
                                Category = "无效卸载项",
                                DisplayName = displayName,
                                RootKeyName = rootLabel,
                                SubKeyPath = $"{path}\\{subKeyName}",
                                DeadPath = deadPath,
                                Reason = reason,
                                IsSubKeyDeletion = true
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    #endregion

    #region 2. 无效自启动项残留扫描

    private void ScanDeadStartupEntries(List<RegistryResidualItem> results)
    {
        var runKeys = new[]
        {
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU"),
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU"),
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM"),
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM (32位)")
        };

        foreach (var (root, path, rootLabel) in runKeys)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) continue;

                foreach (var valName in key.GetValueNames())
                {
                    try
                    {
                        string rawVal = key.GetValue(valName)?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(rawVal)) continue;

                        var (hasPath, exePath, exists) = ExtractAndCheckPath(rawVal);
                        if (hasPath && !exists)
                        {
                            results.Add(new RegistryResidualItem
                            {
                                Category = "无效自启动项",
                                DisplayName = valName,
                                RootKeyName = rootLabel,
                                SubKeyPath = path,
                                ValueName = valName,
                                DeadPath = exePath,
                                Reason = $"自启动程序已从磁盘删除 ({exePath})",
                                IsSubKeyDeletion = false
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    #endregion

    #region 3. MuiCache 历史执行路径残留扫描

    private void ScanDeadMuiCache(List<RegistryResidualItem> results)
    {
        const string muiPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(muiPath);
            if (key == null) return;

            // 限制最多采样扫描前 200 条，避免无响应
            var valNames = key.GetValueNames().Take(300);

            foreach (var valName in valNames)
            {
                try
                {
                    // MuiCache 项通常格式为: "C:\Path\To\app.exe.FriendlyAppName" 或 "C:\Path\To\app.exe.ApplicationCompany"
                    string testPath = valName;
                    int dotIndex = testPath.IndexOf(".FriendlyAppName", StringComparison.OrdinalIgnoreCase);
                    if (dotIndex == -1) dotIndex = testPath.IndexOf(".ApplicationCompany", StringComparison.OrdinalIgnoreCase);

                    if (dotIndex > 0)
                    {
                        testPath = testPath.Substring(0, dotIndex);
                    }

                    if (testPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        testPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Path.IsPathRooted(testPath) && !File.Exists(testPath))
                        {
                            results.Add(new RegistryResidualItem
                            {
                                Category = "MuiCache 运行缓存",
                                DisplayName = Path.GetFileName(testPath),
                                RootKeyName = "HKCU",
                                SubKeyPath = muiPath,
                                ValueName = valName,
                                DeadPath = testPath,
                                Reason = $"历史执行文件已不存在 ({testPath})",
                                IsSubKeyDeletion = false
                            });
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    #endregion

    #region 4. 失效文件关联与打开方式扫描

    private void ScanDeadFileAssociations(List<RegistryResidualItem> results)
    {
        const string classesPath = @"Software\Classes";
        try
        {
            using var baseKey = Registry.CurrentUser.OpenSubKey(classesPath);
            if (baseKey == null) return;

            // 遍历 HKCU\Software\Classes 下的用户自定义 ProgID / Applications
            var subNames = baseKey.GetSubKeyNames();
            foreach (var name in subNames)
            {
                // 只扫描 Applications 或带点/下划线的自定义扩展关联
                if (!name.StartsWith("Applications", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains(".", StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    string commandKeyPath = $"{classesPath}\\{name}\\shell\\open\\command";
                    using var cmdKey = Registry.CurrentUser.OpenSubKey(commandKeyPath);
                    if (cmdKey == null) continue;

                    string defaultCmd = cmdKey.GetValue("")?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(defaultCmd)) continue;

                    var (hasPath, exePath, exists) = ExtractAndCheckPath(defaultCmd);
                    if (hasPath && !exists)
                    {
                        results.Add(new RegistryResidualItem
                        {
                            Category = "失效文件关联",
                            DisplayName = name,
                            RootKeyName = "HKCU",
                            SubKeyPath = $"{classesPath}\\{name}",
                            DeadPath = exePath,
                            Reason = $"右键打开程序已不存在 ({exePath})",
                            IsSubKeyDeletion = true
                        });
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    #endregion

    #region 5. 清理执行

    /// <summary>
    /// 清理选中的注册表残留项
    /// </summary>
    public (int SuccessCount, int FailCount, string Message) CleanResiduals(IEnumerable<RegistryResidualItem> items)
    {
        int success = 0;
        int fail = 0;

        foreach (var item in items)
        {
            try
            {
                RegistryKey? root = item.RootKeyName.StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;

                if (item.IsSubKeyDeletion)
                {
                    // 安全守卫：绝对禁止删除系统白名单根目录
                    string[] parts = item.SubKeyPath.Split('\\');
                    if (parts.Length < 3 || ProtectedSubKeys.Contains(parts.Last()))
                    {
                        fail++;
                        continue;
                    }

                    // 获取父级键与子键名
                    string parentPath = string.Join("\\", parts.Take(parts.Length - 1));
                    string childName = parts.Last();

                    using var parentKey = root.OpenSubKey(parentPath, writable: true);
                    if (parentKey != null)
                    {
                        parentKey.DeleteSubKeyTree(childName, throwOnMissingSubKey: false);
                        success++;
                    }
                    else
                    {
                        fail++;
                    }
                }
                else
                {
                    // 删除特定值项
                    using var key = root.OpenSubKey(item.SubKeyPath, writable: true);
                    if (key != null)
                    {
                        key.DeleteValue(item.ValueName, throwOnMissingValue: false);
                        success++;
                    }
                    else
                    {
                        fail++;
                    }
                }
            }
            catch
            {
                fail++;
            }
        }

        string msg = $"清理完成！\n成功清理: {success} 项注册表残留\n失败/跳过: {fail} 项";
        return (success, fail, msg);
    }

    #endregion

    #region 路径提取与有效性辅助

    /// <summary>
    /// 从任意命令行调用串中提取可执行文件物理路径并检测其是否存在
    /// 例: "\"C:\Program Files\App\app.exe\" /run" -> "C:\Program Files\App\app.exe"
    /// </summary>
    public static (bool HasPath, string ExtractedPath, bool Exists) ExtractAndCheckPath(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (false, "", false);

        raw = raw.Trim();

        // 展开系统环境变量 (如 %ProgramFiles%, %SystemRoot%)
        try
        {
            raw = Environment.ExpandEnvironmentVariables(raw);
        }
        catch { }

        // 1. 如果以双引号开头: "C:\path\to\file.exe" args...
        if (raw.StartsWith("\""))
        {
            int secondQuote = raw.IndexOf('\"', 1);
            if (secondQuote > 1)
            {
                string path = raw.Substring(1, secondQuote - 1).Trim();
                // 排除含参数如 ,0 等图标后缀
                int comma = path.IndexOf(',');
                if (comma > 0) path = path.Substring(0, comma).Trim();

                if (Path.IsPathRooted(path))
                {
                    return (true, path, File.Exists(path) || Directory.Exists(path));
                }
            }
        }

        // 2. 正则捕获驱动器盘符绝对路径 (C:\... 直到空格或逗号或结尾)
        var match = Regex.Match(raw, @"([a-zA-Z]:\\[^"",/]+?\.(?:exe|dll|bat|cmd))", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string path = match.Groups[1].Value.Trim();
            return (true, path, File.Exists(path));
        }

        // 3. 如果整串本身就是根路径
        if (Path.IsPathRooted(raw))
        {
            int comma = raw.IndexOf(',');
            string clean = comma > 0 ? raw.Substring(0, comma).Trim() : raw;
            return (true, clean, File.Exists(clean) || Directory.Exists(clean));
        }

        return (false, "", false);
    }

    #endregion
}
