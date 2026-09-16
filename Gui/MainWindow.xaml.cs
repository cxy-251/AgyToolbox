using System.Diagnostics;
using System.IO;
using System.Windows;
using AgyToolbox.Services;
using AgyToolbox.Views;

namespace AgyToolbox.Gui;

/// <summary>
/// MainWindow: PowerToys 风格侧边栏导航宿主窗口，支持深色/浅色/系统主题双模动态切换与沉浸式 DWM 标题栏
/// </summary>
public partial class MainWindow : Window
{
    private readonly DebloatView _debloatView = new();
    private readonly SystemOptView _systemOptView = new();
    private readonly BuiltInGuideView _builtInGuideView = new();
    private readonly NativeDevView _nativeDevView = new();
    private readonly PortableToolboxView _portableToolboxView = new();

    public MainWindow()
    {
        InitializeComponent();

        // 初始化主题引擎并绑定沉浸式标题栏
        ThemeService.Instance.Initialize(this);

        CheckAdminPrivileges();

        // 默认显示阶段一：开荒与预装精简
        MainContentHost.Content = _debloatView;
        TxtHeaderBreadcrumb.Text = "步骤一：开荒与预装精简";
    }

    private void CheckAdminPrivileges()
    {
        bool isAdmin = IsAdministrator();
        if (isAdmin)
        {
            Title += " [管理员]";
            BannerNonAdmin.Visibility = Visibility.Collapsed;
        }
        else
        {
            BannerNonAdmin.Visibility = Visibility.Visible;
        }
    }

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void BtnRestartAsAdmin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(processPath))
            {
                processPath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = processPath,
                    Arguments = "--gui",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"提权重启失败或已被取消: {ex.Message}", "权限提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (NavDebloat.IsChecked == true)
        {
            MainContentHost.Content = _debloatView;
            TxtHeaderBreadcrumb.Text = "步骤一：开荒与预装精简";
        }
        else if (NavOpt.IsChecked == true)
        {
            MainContentHost.Content = _systemOptView;
            TxtHeaderBreadcrumb.Text = "步骤二：系统优化与安全";
        }
        else if (NavBuiltIn.IsChecked == true)
        {
            MainContentHost.Content = _builtInGuideView;
            TxtHeaderBreadcrumb.Text = "步骤三：自带工具排错指南";
        }
        else if (NavNativeDev.IsChecked == true)
        {
            MainContentHost.Content = _nativeDevView;
            TxtHeaderBreadcrumb.Text = "步骤四：原生开发与终端基建";
        }
        else if (NavToolbox.IsChecked == true)
        {
            MainContentHost.Content = _portableToolboxView;
            TxtHeaderBreadcrumb.Text = "便携工具箱：内置独立套件";
        }
    }

    private void ThemeRadio_Click(object sender, RoutedEventArgs e)
    {
        if (RbThemeLight.IsChecked == true)
        {
            ThemeService.Instance.ApplyTheme(AppThemeMode.Light);
        }
        else if (RbThemeDark.IsChecked == true)
        {
            ThemeService.Instance.ApplyTheme(AppThemeMode.Dark);
        }
        else if (RbThemeSystem.IsChecked == true)
        {
            ThemeService.Instance.ApplyTheme(AppThemeMode.System);
        }
    }
}
