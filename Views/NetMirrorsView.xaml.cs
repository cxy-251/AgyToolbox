using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class NetMirrorsView : UserControl
{
    private readonly WinTricksService _winTricksService = new();

    public NetMirrorsView()
    {
        InitializeComponent();
        GridDns.ItemsSource = _winTricksService.GetDefaultDnsList();
    }

    private async void BtnTestDns_Click(object sender, RoutedEventArgs e)
    {
        var targetBtn = sender as Button;
        if (targetBtn != null) targetBtn.IsEnabled = false;
        try
        {
            var results = await _winTricksService.TestPublicDnsAsync();
            GridDns.ItemsSource = results;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"DNS 测速失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (targetBtn != null) targetBtn.IsEnabled = true;
        }
    }

    private void GridDns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GridDns.SelectedItem is DnsPingResult item)
        {
            Clipboard.SetText(item.Ip);
            MessageBox.Show($"已复制 DNS IP 地址 [{item.Ip}] ({item.Provider}) 到剪贴板！\n可直接粘贴到网络适配器 IPv4 属性中使用。", "DNS 复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnApplyCondarc_Click(object sender, RoutedEventArgs e)
    {
        var msg = _winTricksService.ApplyModernCondarc();
        MessageBox.Show(msg, "Conda 换源", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyCondarc_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_winTricksService.GetModernCondarcContent());
        MessageBox.Show("现代清华源 .condarc 配置文本已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyPip_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtPipCmd.Text);
        MessageBox.Show("Pip 换源命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyNpm_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtNpmCmd.Text);
        MessageBox.Show("Npm 最新镜像源命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyNuget_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtNugetCmd.Text);
        MessageBox.Show("NuGet 华为云镜像源命令已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
