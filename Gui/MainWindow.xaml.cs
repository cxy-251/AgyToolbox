using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AgyToolbox.Gui;

/// <summary>
/// MainWindow: 工具箱主窗口，轻量宿主 10 大分类视图 UserControl
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 支持在单行 TabControl 标签栏上通过鼠标滚轮横向平滑滚动标签
    /// </summary>
    private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
