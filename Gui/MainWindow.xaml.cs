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

        // 默认显示阶段一：开荒与预装精简
        MainContentHost.Content = _debloatView;
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
