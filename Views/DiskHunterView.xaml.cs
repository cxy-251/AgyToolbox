using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class DiskHunterView : UserControl
{
    private readonly DiskHunterService _diskHunterService = new();

    public DiskHunterView()
    {
        InitializeComponent();
        TxtScanPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private void BtnScanUserHome_Click(object sender, RoutedEventArgs e)
    {
        TxtScanPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private async void BtnStartScan_Click(object sender, RoutedEventArgs e)
    {
        string path = TxtScanPath.Text.Trim();
        if (!Directory.Exists(path))
        {
            MessageBox.Show("指定目录不存在，请检查路径。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnStartScan.IsEnabled = false;
        TxtScanStatus.Text = "正在多线程枚举全盘文件中，请稍候...";

        var sw = Stopwatch.StartNew();
        var files = await _diskHunterService.ScanTopFilesAsync(path, 20, count =>
        {
            Dispatcher.Invoke(() => TxtScanStatus.Text = $"已检索 {count} 个文件...");
        });
        sw.Stop();

        GridDiskFiles.ItemsSource = files;
        TxtScanStatus.Text = $"扫描完成！耗时 {sw.ElapsedMilliseconds} ms，已展示 Top {files.Count} 大文件。";
        BtnStartScan.IsEnabled = true;
    }

    private void GridDiskFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LocateCurrentSelectedFile();
    }

    private void BtnLocateFile_Click(object sender, RoutedEventArgs e)
    {
        LocateCurrentSelectedFile();
    }

    private void LocateCurrentSelectedFile()
    {
        if (GridDiskFiles.SelectedItem is FileEntry entry)
        {
            _diskHunterService.RevealInExplorer(entry.FullPath);
        }
    }
}
