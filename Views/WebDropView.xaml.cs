using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Helpers;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class WebDropView : UserControl
{
    private readonly WebDropService _webDropService = new();
    private static readonly Brush PrimaryBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
    private static readonly Brush DangerBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));

    public WebDropView()
    {
        InitializeComponent();

        _webDropService.OnLog += msg =>
        {
            Dispatcher.Invoke(() =>
            {
                ListDropLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                if (ListDropLogs.Items.Count > 100) ListDropLogs.Items.RemoveAt(ListDropLogs.Items.Count - 1);
            });
        };

        _webDropService.OnTextReceived += text =>
        {
            Dispatcher.Invoke(() =>
            {
                ListDropLogs.Items.Insert(0, $"[📩 收到文本] {text}");
            });
        };

        _webDropService.OnFileReceived += (name, len) =>
        {
            Dispatcher.Invoke(() =>
            {
                ListDropLogs.Items.Insert(0, $"[📥 收到文件] {name}");
            });
        };
    }

    private async void BtnToggleDrop_Click(object sender, RoutedEventArgs e)
    {
        if (!_webDropService.IsRunning)
        {
            BtnToggleDrop.IsEnabled = false;
            try
            {
                await _webDropService.StartAsync();

                TxtDropStatus.Text = "● 服务运行中";
                TxtDropStatus.Foreground = ThemeBrushes.Success;
                BtnToggleDrop.Content = "停止服务";
                BtnToggleDrop.Background = DangerBrush;

                var ips = _webDropService.GetLocalIPv4Addresses();
                string primaryUrl = ips.Count > 0 ? $"http://{ips[0]}:{_webDropService.CurrentPort}" : $"http://localhost:{_webDropService.CurrentPort}";
                TxtDropUrl.Text = $"访问地址: {primaryUrl}";
                TxtQrTargetUrl.Text = primaryUrl;

                ImgQrCode.Source = QrCodeHelper.GenerateQrBitmap(primaryUrl, 8);
                BorderQrCard.Visibility = Visibility.Visible;

                BtnOpenBrowser.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnToggleDrop.IsEnabled = true;
            }
        }
        else
        {
            BtnToggleDrop.IsEnabled = false;
            try
            {
                await _webDropService.StopAsync();

                TxtDropStatus.Text = "● 服务未启动";
                TxtDropStatus.Foreground = (Brush)Application.Current.Resources["BrushTextSecondary"];
                BtnToggleDrop.Content = "启动快传服务";
                BtnToggleDrop.Background = PrimaryBrush;
                TxtDropUrl.Text = "服务已停止";

                BorderQrCard.Visibility = Visibility.Collapsed;
                ImgQrCode.Source = null;

                BtnOpenBrowser.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnToggleDrop.IsEnabled = true;
            }
        }
    }

    private void BtnCopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtQrTargetUrl.Text) && TxtQrTargetUrl.Text != "-")
        {
            Clipboard.SetText(TxtQrTargetUrl.Text);
            MessageBox.Show("访问链接已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e)
    {
        var ips = _webDropService.GetLocalIPv4Addresses();
        string url = ips.Count > 0 ? $"http://{ips[0]}:{_webDropService.CurrentPort}" : $"http://localhost:{_webDropService.CurrentPort}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void BtnOpenReceived_Click(object sender, RoutedEventArgs e) => _webDropService.OpenReceivedFolder();
    private void BtnOpenShared_Click(object sender, RoutedEventArgs e) => _webDropService.OpenSharedFolder();
}
