using System.Diagnostics;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public class WindowsUpdateService
{
    private const string WuPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
    private const string DriverPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
    private const string ServiceRegKey = @"SYSTEM\CurrentControlSet\Services\wuauserv";

    /// <summary>
    /// 检测 Windows 自动更新是否已被禁用
    /// </summary>
    public bool IsUpdateDisabled()
    {
        try
        {
            // 检查组策略禁止更新
            using var key = Registry.LocalMachine.OpenSubKey(WuPolicyKey);
            var val = key?.GetValue("NoAutoUpdate");
            if (val is int intVal && intVal == 1) return true;

            // 检查服务启动项: 4 代表禁用 (Disabled)
            using var svcKey = Registry.LocalMachine.OpenSubKey(ServiceRegKey);
            var svcVal = svcKey?.GetValue("Start");
            if (svcVal is int startMode && startMode == 4) return true;
        }
        catch
        {
            // 忽略读取异常
        }
        return false;
    }

    /// <summary>
    /// 彻底关闭 Windows 自动更新 (含组策略锁死、防自动更新显卡驱动锁死、停止更新医生服务)
    /// </summary>
    public async Task<(bool Success, string Message)> DisableUpdateAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // 1. 组策略彻底禁止自动更新
                using (var auKey = Registry.LocalMachine.CreateSubKey(WuPolicyKey))
                {
                    auKey.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                    auKey.SetValue("AUOptions", 2, RegistryValueKind.DWord); // 仅通知，绝不自动下载
                }

                // 2. 禁止 Windows Update 自动覆盖显卡驱动 (防无数人游戏黑屏崩溃的元凶)
                using (var driverKey = Registry.LocalMachine.CreateSubKey(DriverPolicyKey))
                {
                    driverKey.SetValue("ExcludeWPDriversInQualityUpdate", 1, RegistryValueKind.DWord);
                }

                // 3. 停止并禁用 Windows Update 核心服务 (wuauserv)
                RunCommand("sc.exe", "stop wuauserv");
                RunCommand("sc.exe", "config wuauserv start= disabled");

                // 4. 禁用微软“更新医生”服务 (WaaSMedicSvc，防止它半夜偷偷重新激活更新)
                RunCommand("sc.exe", "stop WaaSMedicSvc");
                RunCommand("sc.exe", "config WaaSMedicSvc start= disabled");

                return (true, "已彻底关闭 Windows 自动更新！\n1. 组策略已锁定禁止自动下载\n2. 已禁止系统更新静默覆盖显卡驱动\n3. 更新服务与 WaaSMedic 唤醒医生已完全禁用");
            }
            catch (Exception ex)
            {
                return (false, $"关闭更新失败 (需要管理员权限): {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 恢复 Windows 官方默认自动更新机制
    /// </summary>
    public async Task<(bool Success, string Message)> EnableUpdateAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // 1. 删除限制组策略
                Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", false);

                // 2. 恢复服务为自动/手动
                RunCommand("sc.exe", "config wuauserv start= demand");
                RunCommand("sc.exe", "start wuauserv");
                RunCommand("sc.exe", "config WaaSMedicSvc start= demand");

                return (true, "已成功恢复 Windows 官方自动更新策略！系统将按微软默认规则接收安全补丁。");
            }
            catch (Exception ex)
            {
                return (false, $"恢复更新失败: {ex.Message}");
            }
        });
    }

    private static void RunCommand(string fileName, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch
        {
        }
    }
}
