using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace AgyToolbox.Services;

public record FileHashResult(string FilePath, long FileSize, string Md5, string Sha1, string Sha256, string Sha512);

public class DevToolsService
{
    /// <summary>
    /// 流式异步计算文件的四大主流哈希指纹 (MD5, SHA1, SHA256, SHA512)
    /// </summary>
    public async Task<FileHashResult> ComputeFileHashesAsync(string filePath, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var fi = new FileInfo(filePath);
            if (!fi.Exists) throw new FileNotFoundException("文件不存在", filePath);

            using var stream = File.OpenRead(filePath);

            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();
            using var sha256 = SHA256.Create();
            using var sha512 = SHA512.Create();

            // 为避免多次从磁盘读取大文件，使用组合缓冲区单次读取多路哈希
            byte[] buffer = new byte[1024 * 1024]; // 1MB 缓冲
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                sha512.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha512.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            string md5Str = Convert.ToHexString(md5.Hash!).ToLowerInvariant();
            string sha1Str = Convert.ToHexString(sha1.Hash!).ToLowerInvariant();
            string sha256Str = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            string sha512Str = Convert.ToHexString(sha512.Hash!).ToLowerInvariant();

            return new FileHashResult(filePath, fi.Length, md5Str, sha1Str, sha256Str, sha512Str);
        }, ct);
    }

    /// <summary>
    /// Base64 编码 (UTF-8)
    /// </summary>
    public string Base64Encode(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Base64 解码 (UTF-8)
    /// </summary>
    public string Base64Decode(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return "";
        byte[] bytes = Convert.FromBase64String(base64.Trim());
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// URL 编码
    /// </summary>
    public string UrlEncode(string text) => WebUtility.UrlEncode(text ?? "");

    /// <summary>
    /// URL 解码
    /// </summary>
    public string UrlDecode(string text) => WebUtility.UrlDecode(text ?? "");

    /// <summary>
    /// Unix 时间戳转本地时间 (自动识别秒或毫秒)
    /// </summary>
    public string TimestampToDateTime(long ts)
    {
        DateTimeOffset dto;
        if (ts > 100000000000L) // 13位毫秒时间戳
        {
            dto = DateTimeOffset.FromUnixTimeMilliseconds(ts);
        }
        else // 10位秒级时间戳
        {
            dto = DateTimeOffset.FromUnixTimeSeconds(ts);
        }

        return dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    /// <summary>
    /// 获取当前 Unix 时间戳
    /// </summary>
    public (long Seconds, long Milliseconds) GetCurrentTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        return (now.ToUnixTimeSeconds(), now.ToUnixTimeMilliseconds());
    }

    /// <summary>
    /// 生成标准 GUID / UUID
    /// </summary>
    public string GenerateGuid(bool upper = false, bool noHyphen = false)
    {
        var g = Guid.NewGuid();
        string format = noHyphen ? "N" : "D";
        string result = g.ToString(format);
        return upper ? result.ToUpperInvariant() : result.ToLowerInvariant();
    }
}
