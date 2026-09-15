using System.Windows;
using AgyToolbox.Views;

namespace AgyToolbox.Gui;

/// <summary>
/// MainWindow: PowerToys 风格侧边栏导航宿主窗口
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

        CheckAdminPrivileges();

        // 默认显示阶段一：开荒与预装精简
        MainContentHost.Content = _debloatView;
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
        }
        else if (NavOpt.IsChecked == true)
        {
            MainContentHost.Content = _systemOptView;
        }
        else if (NavBuiltIn.IsChecked == true)
        {
            MainContentHost.Content = _builtInGuideView;
        }
        else if (NavNativeDev.IsChecked == true)
        {
            MainContentHost.Content = _nativeDevView;
        }
        else if (NavToolbox.IsChecked == true)
        {
            MainContentHost.Content = _portableToolboxView;
        }
    }
}
