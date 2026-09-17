using System.Windows;
using System.Windows.Controls;

namespace AgyToolbox.Views;

public partial class ModernToolchainView : UserControl
{
    public ModernToolchainView()
    {
        InitializeComponent();
    }

    private void BtnCopySnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string snippet && !string.IsNullOrWhiteSpace(snippet))
        {
            Clipboard.SetText(snippet);
            string preview = snippet.Length > 160
                ? snippet.Substring(0, 160).TrimEnd() + "...\n\n(完整内容已复制到剪贴板，可直接粘贴使用)"
                : snippet;
            MessageBox.Show($"已复制至剪贴板：\n\n{preview}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
