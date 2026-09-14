using System.Diagnostics;
using AgyToolbox.Core;

namespace AgyToolbox.Tools;

public class DiskHunterTool : ITool
{
    public string Key => "disk";
    public string Name => "大文件极速猎手 (DiskHunter)";
    public string Description => "极速扫描指定目录，抓出霸占空间最大的前 20 个大文件，支持定位到文件夹。";

    public Task RunAsync(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=======================================================");
        Console.WriteLine("          🔍 大文件极速猎手 (DiskHunter)               ");
        Console.WriteLine("=======================================================");
        Console.ResetColor();

        string targetDir = "";
        if (args.Length > 0 && Directory.Exists(args[0]))
        {
            targetDir = args[0];
        }
        else
        {
            Console.WriteLine("请输入要扫描的目录路径（直接回车默认扫描当前用户主目录）：");
            Console.Write("👉 ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                targetDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else
            {
                targetDir = input.Trim('\"', ' ');
            }
        }

        if (!Directory.Exists(targetDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[错误] 路径不存在: {targetDir}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        Console.WriteLine($"\n[⚡] 正在全盘多线程枚举扫描: {targetDir}");
        Console.WriteLine("    (自动跳过无权限目录与符号链接，请稍候...)");

        var sw = Stopwatch.StartNew();
        var filesList = new List<FileInfo>();

        var enumOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        long totalScannedCount = 0;

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(targetDir, "*", enumOptions))
            {
                totalScannedCount++;
                try
                {
                    var fi = new FileInfo(filePath);
                    filesList.Add(fi);
                }
                catch
                {
                    // 忽略单个文件读取属性异常
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[提示] 扫描过程中遇到中断: {ex.Message}");
            Console.ResetColor();
        }

        sw.Stop();

        if (filesList.Count == 0)
        {
            Console.WriteLine("未找到任何文件。");
            return Task.CompletedTask;
        }

        var topFiles = filesList.OrderByDescending(f => f.Length).Take(20).ToList();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[✓] 扫描完成！耗时: {sw.ElapsedMilliseconds} ms，共检索 {totalScannedCount} 个文件。");
        Console.WriteLine($"[📊] 体积排名前 {topFiles.Count} 的大文件：");
        Console.ResetColor();

        Console.WriteLine("--------------------------------------------------------------------------------------");
        Console.WriteLine(string.Format("{0,-4} | {1,-10} | {2,-20} | {3}", "序号", "大小", "修改日期", "文件完整路径"));
        Console.WriteLine("--------------------------------------------------------------------------------------");

        for (int i = 0; i < topFiles.Count; i++)
        {
            var f = topFiles[i];
            string sizeStr = FormatBytes(f.Length);
            string timeStr = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            Console.WriteLine(string.Format("{0,-4} | {1,-10} | {2,-20} | {3}", $"[{i + 1}]", sizeStr, timeStr, f.FullName));
        }
        Console.WriteLine("--------------------------------------------------------------------------------------");

        while (!Console.IsInputRedirected)
        {
            Console.WriteLine("\n[操作] 输入序号 (1-20) 在资源管理器中定位该文件，或直接按回车返回主菜单：");
            Console.Write("👉 ");
            var cmd = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(cmd)) break;

            if (int.TryParse(cmd, out int idx) && idx >= 1 && idx <= topFiles.Count)
            {
                var chosen = topFiles[idx - 1];
                if (File.Exists(chosen.FullName))
                {
                    Process.Start("explorer.exe", $"/select,\"{chosen.FullName}\"");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[✓] 已在 Windows 资源管理器中高亮选中文件: {chosen.Name}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("[提示] 文件可能已被移动或删除。");
                }
            }
            else
            {
                Console.WriteLine("无效序号，请重新输入。");
            }
        }

        return Task.CompletedTask;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
