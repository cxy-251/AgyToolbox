# 🛠️ AGY 全能便携工具箱 (AgyToolbox)

基于现代 C# / .NET 开发的多功能轻量实用工具箱，**TUI（终端交互）与 GUI（桌面图形窗口）双模一体化**，既能在黑框命令行极速调用，也能弹出精美 Windows 原生图形界面。

---

## 🚀 快速启动

进入项目目录后执行命令：

```bash
# 1. 打开原生桌面图形界面 (GUI 模式)
dotnet run -- gui

# 2. 启动控制台彩色交互菜单 (TUI 模式)
dotnet run

# 3. 命令行快捷调用指定小工具
dotnet run -- drop      # 快速启动局域网快传
dotnet run -- disk      # 启动大文件扫描
dotnet run -- sys       # 运行系统与网络诊断
dotnet run -- tricks   # 打开 Windows 实用系统运维技巧
```

---

## 📦 打包与分发独立程序

```bash
dotnet publish -c Release
```

打包产物位于：
`bin\Release\net8.0-windows\publish\`

* **运行方式**：直接双击里面的 `AgyToolbox.exe`：
  * 在终端菜单中输入 `5` 可直接呼出桌面窗口。
  * 或者给它建一个快捷方式，加上参数 `gui`，双击即可作为纯桌面客户端启动！

---

## 🧩 内置功能一览

| 工具名称 | 触发命令 | GUI 页面支持 | 功能说明 |
| :--- | :--- | :--- | :--- |
| **局域网快传 (WebDrop)** | `drop` | 支持 (附带二维码) | 电脑一键启动 HTTP 服务，手机扫码即可互传文件、照片与长文本 |
| **大文件极速猎手 (DiskHunter)** | `disk` | 支持 | 毫秒级多线程枚举磁盘，抓出 Top 20 大文件，双击直接在资源管理器中高亮定位 |
| **系统与网络诊断 (SysInfo)** | `sys` | 支持 | 硬件信息、磁盘容量彩色进度条、活动网卡 IP 与公网 Ping 连通延迟测速 |
| **Windows 实用系统运维技巧 (WinTricks)** | `tricks` | 支持 | 查看已存WiFi密码、电池健康损耗报告、端口占用排查、系统可靠性崩溃历史等 |

---

## ❓ 关于跨平台的客观分析

* **网络与后端核心 (`WebDrop`)**：底层基于 Kestrel，**100% 跨平台**（Linux / macOS / Windows 均原生支持）。
* **磁盘扫描与诊断 (`DiskHunter`, `SysInfo`)**：**95% 跨平台**。文件枚举和 Ping 均跨平台，但一键打开文件管理器目前针对 Windows 使用了 `explorer.exe`。
* **GUI 桌面窗口**：基于 **WPF**，由于底层深度依赖 DirectX 与 Win32 窗口管理器，**仅支持 Windows**。若未来需要跨 macOS / Linux 桌面，可将 GUI 迁移至 Avalonia UI 或 Webview。
