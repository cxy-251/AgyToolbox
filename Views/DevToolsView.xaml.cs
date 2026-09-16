using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgyToolbox.Services;

namespace AgyToolbox.Views;

public partial class DevToolsView : UserControl
{
    private readonly DevToolsService _devToolsService = new();

    public DevToolsView()
    {
        InitializeComponent();
        BtnFillCurrentTs_Click(this, new RoutedEventArgs());
        BtnGenGuid_Click(this, new RoutedEventArgs());
    }

    private async void ProcessHashFile(string path)
    {
        if (!File.Exists(path)) return;
        TxtHashFilePath.Text = path;
        TxtHashMd5.Text = "计算中...";
        TxtHashSha1.Text = "计算中...";
        TxtHashSha256.Text = "计算中...";
        TxtHashSha512.Text = "计算中...";
        TxtHashCompareResult.Text = "";

        try
        {
            var res = await _devToolsService.ComputeFileHashesAsync(path);
            TxtHashMd5.Text = res.Md5;
            TxtHashSha1.Text = res.Sha1;
            TxtHashSha256.Text = res.Sha256;
            TxtHashSha512.Text = res.Sha512;
            CompareHash();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"计算哈希失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSelectHashFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择要计算哈希指纹的文件"
        };
        if (dialog.ShowDialog() == true)
        {
            ProcessHashFile(dialog.FileName);
        }
    }

    private void TxtHashFilePath_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void TxtHashFilePath_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                ProcessHashFile(files[0]);
            }
        }
    }

    private void TxtExpectedHash_TextChanged(object sender, TextChangedEventArgs e)
    {
        CompareHash();
    }

    private void CompareHash()
    {
        string expected = TxtExpectedHash.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(expected))
        {
            TxtHashCompareResult.Text = "";
            return;
        }

        if (expected == TxtHashMd5.Text || expected == TxtHashSha1.Text ||
            expected == TxtHashSha256.Text || expected == TxtHashSha512.Text)
        {
            TxtHashCompareResult.Text = "✅ 哈希完全匹配！文件完整无篡改。";
            TxtHashCompareResult.Foreground = ThemeBrushes.Success;
        }
        else
        {
            TxtHashCompareResult.Text = "❌ 哈希不匹配！文件可能已损坏或被修改。";
            TxtHashCompareResult.Foreground = ThemeBrushes.Danger;
        }
    }

    private void BtnCopyMd5_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashMd5.Text, "MD5");
    private void BtnCopySha1_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashSha1.Text, "SHA1");
    private void BtnCopySha256_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashSha256.Text, "SHA256");
    private void BtnCopySha512_Click(object sender, RoutedEventArgs e) => CopyText(TxtHashSha512.Text, "SHA512");

    private void CopyText(string text, string label)
    {
        if (!string.IsNullOrEmpty(text) && text != "计算中...")
        {
            Clipboard.SetText(text);
            MessageBox.Show($"{label} 已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnBase64Encode_Click(object sender, RoutedEventArgs e)
    {
        TxtCodecOutput.Text = _devToolsService.Base64Encode(TxtCodecInput.Text);
    }

    private void BtnBase64Decode_Click(object sender, RoutedEventArgs e)
    {
        try { TxtCodecOutput.Text = _devToolsService.Base64Decode(TxtCodecInput.Text); }
        catch (Exception ex) { MessageBox.Show($"Base64 解码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void BtnUrlEncode_Click(object sender, RoutedEventArgs e)
    {
        TxtCodecOutput.Text = _devToolsService.UrlEncode(TxtCodecInput.Text);
    }

    private void BtnUrlDecode_Click(object sender, RoutedEventArgs e)
    {
        TxtCodecOutput.Text = _devToolsService.UrlDecode(TxtCodecInput.Text);
    }

    private void BtnCopyCodecOutput_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtCodecOutput.Text))
        {
            Clipboard.SetText(TxtCodecOutput.Text);
            MessageBox.Show("转换结果已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnFillCurrentTs_Click(object sender, RoutedEventArgs e)
    {
        var (sec, ms) = _devToolsService.GetCurrentTimestamps();
        TxtTimestampInput.Text = sec.ToString();
        TxtTsResult.Text = $"{_devToolsService.TimestampToDateTime(sec)} (当前系统时间)";
    }

    private void BtnConvertTs_Click(object sender, RoutedEventArgs e)
    {
        if (long.TryParse(TxtTimestampInput.Text.Trim(), out long ts))
        {
            TxtTsResult.Text = _devToolsService.TimestampToDateTime(ts);
        }
        else
        {
            MessageBox.Show("请输入合法的数字时间戳（10位秒或13位毫秒）！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnGenGuid_Click(object sender, RoutedEventArgs e)
    {
        bool upper = ChkGuidUpper.IsChecked == true;
        bool noHyphen = ChkGuidNoHyphen.IsChecked == true;
        TxtGuidResult.Text = _devToolsService.GenerateGuid(upper, noHyphen);
    }

    private void BtnCopyGuid_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TxtGuidResult.Text))
        {
            Clipboard.SetText(TxtGuidResult.Text);
            MessageBox.Show("全新 GUID 已复制到剪贴板！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
