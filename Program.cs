using AgyToolbox.Core;
using AgyToolbox.Gui;
using AgyToolbox.Tools;

namespace AgyToolbox;

internal static class Program
{
    [STAThread]
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var tools = new List<ITool>
        {
            new WebDropTool(),
            new DiskHunterTool(),
            new SysInfoTool(),
            new WinTricksTool()
        };

        // 1. 如果传入 gui 参数，直接打开桌面窗口
        if (args.Length > 0 && (args[0].Equals("gui", StringComparison.OrdinalIgnoreCase) ||
                                args[0].Equals("--gui", StringComparison.OrdinalIgnoreCase) ||
                                args[0].Equals("-g", StringComparison.OrdinalIgnoreCase)))
        {
            LaunchGui();
            return;
        }

        // 2. 如果传入了特定子工具命令（drop / disk / sys）
        if (args.Length > 0)
        {
            var cmd = args[0].ToLowerInvariant().TrimStart('-', '/');
            var tool = tools.FirstOrDefault(t => t.Key.Equals(cmd, StringComparison.OrdinalIgnoreCase));

            if (tool != null)
            {
                await tool.RunAsync(args.Skip(1).ToArray());
                return;
            }
        }

        // 3. 无参数时进入控制台交互菜单
        while (true)
        {
            try { if (!Console.IsOutputRedirected) Console.Clear(); } catch { }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================");
            Console.WriteLine("            🛠️  AGY 全能工具箱 (AgyToolbox)                   ");
            Console.WriteLine("===============================================================");
            Console.ResetColor();
            Console.WriteLine(" 请选择要启动的功能：\n");

            for (int i = 0; i < tools.Count; i++)
            {
                var t = tools[i];
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"  [{i + 1}] {t.Name,-30}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" (快捷命令: {t.Key})");
                Console.ResetColor();
                Console.WriteLine($"      └─ {t.Description}\n");
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"  [{tools.Count + 1}] 🖥️  启动桌面图形窗口界面 (GUI 模式)        (快捷命令: gui)");
            Console.ResetColor();
            Console.WriteLine("      └─ 打开原生 Windows 窗口，具备完整的可视化选项卡与鼠标操作\n");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [0] 退出工具箱");
            Console.ResetColor();
            Console.WriteLine("===============================================================");
            Console.Write($"请输入序号 [0-{tools.Count + 1}]: ");

            var key = Console.ReadLine();
            if (key == null) break;
            if (string.IsNullOrWhiteSpace(key)) continue;

            if (key == "0")
            {
                Console.WriteLine("\n感谢使用，再见！");
                break;
            }

            if (int.TryParse(key, out int selectedIdx))
            {
                if (selectedIdx >= 1 && selectedIdx <= tools.Count)
                {
                    var targetTool = tools[selectedIdx - 1];
                    try
                    {
                        await targetTool.RunAsync(Array.Empty<string>());
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[异常] 运行出错: {ex.Message}");
                        Console.ResetColor();
                        Console.WriteLine("\n按任意键继续...");
                        if (!Console.IsInputRedirected) Console.ReadKey();
                    }
                }
                else if (selectedIdx == tools.Count + 1)
                {
                    LaunchGui();
                }
                else
                {
                    Console.WriteLine("输入无效，请重新输入...");
                    Thread.Sleep(800);
                }
            }
        }
    }

    private static void LaunchGui()
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            RunWpfApp();
        }
        else
        {
            var thread = new Thread(RunWpfApp);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        void RunWpfApp()
        {
            try
            {
                var app = new System.Windows.Application();
                try
                {
                    var dict = new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("/AgyToolbox;component/Gui/Styles.xaml", UriKind.RelativeOrAbsolute)
                    };
                    app.Resources.MergedDictionaries.Add(dict);
                }
                catch { }

                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[GUI 启动异常]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[内部异常]: {ex.InnerException.Message}");
                }
                Console.ResetColor();
            }
        }
    }
}
