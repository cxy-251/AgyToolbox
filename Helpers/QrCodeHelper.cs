using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace AgyToolbox.Helpers;

public static class QrCodeHelper
{
    /// <summary>
    /// 生成可直接用于 WPF Image 控件的 BitmapImage
    /// </summary>
    public static BitmapImage GenerateQrBitmap(string text, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        using var pngQr = new PngByteQRCode(data);
        byte[] bytes = pngQr.GetGraphic(pixelsPerModule);

        using var ms = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>
    /// 生成可在控制台直接打印的 ANSI 双字符二维码字符串
    /// </summary>
    public static string GenerateConsoleQr(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        using var asciiQr = new AsciiQRCode(data);
        return asciiQr.GetGraphic(1, "██", "  ", drawQuietZones: true);
    }
}
