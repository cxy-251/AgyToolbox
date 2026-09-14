using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using AgyToolbox.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgyToolbox.Tools;

public class WebDropTool : ITool
{
    public string Key => "drop";
    public string Name => "局域网快传中转站 (WebDrop)";
    public string Description => "在局域网内与手机或另一台电脑秒传文本与文件，无需联网与数据线。";

    public async Task RunAsync(string[] args)
    {
        int port = 5280;
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }

        string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "AgyWebDrop");
        string receivedDir = Path.Combine(baseDir, "Received");
        string sharedDir = Path.Combine(baseDir, "Shared");

        Directory.CreateDirectory(receivedDir);
        Directory.CreateDirectory(sharedDir);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=======================================================");
        Console.WriteLine("          🚀 局域网快传中转站 (WebDrop) 启动中...        ");
        Console.WriteLine("=======================================================");
        Console.ResetColor();

        var localIps = GetLocalIPv4Addresses();
        Console.WriteLine("\n[✓] 本机局域网访问地址 (请确保手机连接相同 WiFi)：");
        foreach (var ip in localIps)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   👉 http://{ip}:{port}");
            Console.ResetColor();
        }
        Console.WriteLine($"   👉 http://localhost:{port}");

        Console.WriteLine($"\n[📁] 文件保存路径: {receivedDir}");
        Console.WriteLine($"[📁] 待分享路径:   {sharedDir} (放入该目录的文件可在网页端下载)");

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders(); // 保持控制台干净
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Any, port);
        });

        var app = builder.Build();

        // 首页 HTML
        app.MapGet("/", () => Results.Content(GetHtmlPage(), "text/html; charset=utf-8"));

        // 发送文本接口
        app.MapPost("/api/text", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var content = await reader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(content))
            {
                var time = DateTime.Now.ToString("HH:mm:ss");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n📩 [{time}] 收到文本:");
                Console.ResetColor();
                Console.WriteLine(content);

                var logFile = Path.Combine(receivedDir, "received_texts.txt");
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{content}\n--------------------\n");
                return Results.Json(new { success = true, message = "文本已接收并保存！" });
            }
            return Results.BadRequest(new { success = false, message = "内容为空" });
        });

        // 上传文件接口
        app.MapPost("/api/upload", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("不支持的格式");

            var form = await request.ReadFormAsync();
            var files = form.Files;
            if (files.Count == 0) return Results.BadRequest("未选择文件");

            var savedNames = new List<string>();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var safeFileName = Path.GetFileName(file.FileName);
                    var targetPath = Path.Combine(receivedDir, safeFileName);

                    // 避免同名覆盖
                    int count = 1;
                    while (File.Exists(targetPath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                        var ext = Path.GetExtension(safeFileName);
                        targetPath = Path.Combine(receivedDir, $"{nameWithoutExt}_{count++}{ext}");
                    }

                    using var stream = new FileStream(targetPath, FileMode.Create);
                    await file.CopyToAsync(stream);
                    savedNames.Add(Path.GetFileName(targetPath));

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n📥 收到文件: {Path.GetFileName(targetPath)} ({FormatBytes(file.Length)})");
                    Console.ResetColor();
                }
            }

            return Results.Json(new { success = true, savedFiles = savedNames });
        });

        // 获取可下载的共享文件列表
        app.MapGet("/api/files", () =>
        {
            var files = Directory.GetFiles(sharedDir)
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    size = FormatBytes(new FileInfo(f).Length)
                });
            return Results.Json(files);
        });

        // 下载共享文件
        app.MapGet("/api/download/{fileName}", (string fileName) =>
        {
            var safeName = Path.GetFileName(fileName);
            var filePath = Path.Combine(sharedDir, safeName);
            if (File.Exists(filePath))
            {
                return Results.File(filePath, "application/octet-stream", safeName);
            }
            return Results.NotFound("文件不存在");
        });

        using var cts = new CancellationTokenSource();
        var serverTask = app.RunAsync(cts.Token);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n[提示] 服务正在后台监听，按回车键 [Enter] 或按 [Q] 停止快传并返回菜单...");
        Console.ResetColor();

        if (!Console.IsInputRedirected)
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
        }
        else
        {
            Console.ReadLine();
        }

        Console.WriteLine("\n正在停止服务...");
        cts.Cancel();
        try
        {
            await serverTask;
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("服务已安全退出。\n");
    }

    private static List<string> GetLocalIPv4Addresses()
    {
        var ips = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                var ipProps = ni.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipStr = addr.Address.ToString();
                        // 优先显示常见的内网 IP
                        if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10.") || ipStr.StartsWith("172."))
                        {
                            ips.Add(ipStr);
                        }
                    }
                }
            }
        }
        return ips.Distinct().ToList();
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

    private static string GetHtmlPage()
    {
        return """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>AGY 局域网极速快传</title>
            <style>
                :root { --primary: #0078d4; --bg: #f3f4f6; --card: #ffffff; --text: #1f2937; }
                body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: var(--text); margin: 0; padding: 16px; }
                .container { max-width: 600px; margin: 0 auto; }
                .card { background: var(--card); border-radius: 12px; padding: 20px; margin-bottom: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }
                h1 { font-size: 1.4rem; text-align: center; margin: 10px 0 20px 0; color: var(--primary); }
                h2 { font-size: 1.1rem; margin-top: 0; margin-bottom: 12px; }
                textarea { width: 100%; box-sizing: border-box; height: 100px; border: 1px solid #d1d5db; border-radius: 8px; padding: 10px; font-size: 1rem; resize: vertical; }
                button { background: var(--primary); color: white; border: none; padding: 12px 20px; border-radius: 8px; font-size: 1rem; cursor: pointer; width: 100%; margin-top: 10px; font-weight: 600; }
                button:active { opacity: 0.85; }
                input[type="file"] { width: 100%; box-sizing: border-box; padding: 10px; border: 2px dashed #cbd5e1; border-radius: 8px; background: #f8fafc; }
                .toast { position: fixed; bottom: 20px; left: 50%; transform: translateX(-50%); background: #333; color: #fff; padding: 10px 20px; border-radius: 20px; display: none; z-index: 1000; }
                .file-item { display: flex; justify-content: space-between; align-items: center; padding: 8px 0; border-bottom: 1px solid #f1f5f9; }
                .file-item a { color: var(--primary); text-decoration: none; font-weight: 500; word-break: break-all; }
            </style>
        </head>
        <body>
            <div class="container">
                <h1>⚡ AGY 局域网极速快传</h1>

                <div class="card">
                    <h2>📝 发送文本 / 链接到电脑</h2>
                    <textarea id="textInput" placeholder="在这里输入文字、链接、验证码..."></textarea>
                    <button onclick="sendText()">立即发送到电脑</button>
                </div>

                <div class="card">
                    <h2>📁 发送文件 / 照片到电脑</h2>
                    <input type="file" id="fileInput" multiple>
                    <button onclick="uploadFiles()">立即上传</button>
                </div>

                <div class="card">
                    <h2>📥 从电脑下载文件 (共享区)</h2>
                    <div id="fileList">正在加载共享文件...</div>
                </div>
            </div>
            <div id="toast" class="toast"></div>

            <script>
                function showToast(msg) {
                    const t = document.getElementById('toast');
                    t.innerText = msg;
                    t.style.display = 'block';
                    setTimeout(() => { t.style.display = 'none'; }, 2500);
                }

                async function sendText() {
                    const val = document.getElementById('textInput').value;
                    if (!val) return showToast('请输入内容');
                    try {
                        const res = await fetch('/api/text', {
                            method: 'POST',
                            headers: { 'Content-Type': 'text/plain' },
                            body: val
                        });
                        const data = await res.json();
                        showToast(data.message);
                        document.getElementById('textInput').value = '';
                    } catch (e) {
                        showToast('发送失败，请检查网络');
                    }
                }

                async function uploadFiles() {
                    const input = document.getElementById('fileInput');
                    if (input.files.length === 0) return showToast('请先选择文件');
                    const formData = new FormData();
                    for (let f of input.files) {
                        formData.append('files', f);
                    }
                    showToast('正在上传，请稍候...');
                    try {
                        const res = await fetch('/api/upload', {
                            method: 'POST',
                            body: formData
                        });
                        const data = await res.json();
                        showToast('上传成功！已存入电脑');
                        input.value = '';
                    } catch (e) {
                        showToast('上传出错');
                    }
                }

                async function loadSharedFiles() {
                    try {
                        const res = await fetch('/api/files');
                        const files = await res.json();
                        const list = document.getElementById('fileList');
                        if (files.length === 0) {
                            list.innerHTML = '<span style="color:#94a3b8;">暂无可下载的共享文件（可将文件放入电脑 Shared 目录）</span>';
                            return;
                        }
                        list.innerHTML = files.map(f => `
                            <div class="file-item">
                                <a href="/api/download/${encodeURIComponent(f.name)}" download="${f.name}">${f.name}</a>
                                <span style="font-size: 0.85rem; color: #64748b;">${f.size}</span>
                            </div>
                        `).join('');
                    } catch (e) {
                        document.getElementById('fileList').innerText = '加载失败';
                    }
                }

                loadSharedFiles();
            </script>
        </body>
        </html>
        """;
    }
}
