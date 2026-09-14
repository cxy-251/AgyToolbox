using AgyToolbox.Helpers;

namespace AgyToolbox.Services;

public record FileEntry(int Rank, string Name, long Length, string SizeFormatted, DateTime LastModified, string FullPath);

public class DiskHunterService
{
    public Task<List<FileEntry>> ScanTopFilesAsync(string targetDir, int topCount = 20, Action<long>? progressCallback = null, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var filesList = new List<FileInfo>();
            if (!Directory.Exists(targetDir)) return new List<FileEntry>();

            var enumOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            long count = 0;
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(targetDir, "*", enumOptions))
                {
                    if (ct.IsCancellationRequested) break;
                    count++;
                    if (count % 200 == 0) progressCallback?.Invoke(count);

                    try
                    {
                        var fi = new FileInfo(filePath);
                        filesList.Add(fi);
                    }
                    catch { }
                }
            }
            catch { }

            var top = filesList.OrderByDescending(f => f.Length)
                .Take(topCount)
                .Select((f, index) => new FileEntry(
                    index + 1,
                    f.Name,
                    f.Length,
                    FormatHelper.FormatBytes(f.Length),
                    f.LastWriteTime,
                    f.FullName
                )).ToList();

            return top;
        }, ct);
    }

    public void RevealInExplorer(string fullPath)
    {
        if (File.Exists(fullPath))
        {
            Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
        }
        else if (Directory.Exists(fullPath))
        {
            Process.Start("explorer.exe", $"\"{fullPath}\"");
        }
    }
}
