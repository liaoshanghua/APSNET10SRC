using System.IO.Compression;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>扫描 wwwroot，为 JS/CSS 等大文件生成 .br / .gz。</summary>
internal static class ApsWwwrootCompressor
{
    private static readonly HashSet<string> CompressibleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".js", ".css", ".json", ".html", ".htm", ".svg", ".wasm", ".map"
    };

    public static async Task<WwwrootCompressResult> RunAsync(
        string wwwRoot,
        WwwrootCompressOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(wwwRoot))
        {
            logger.LogDebug("wwwroot 不存在，跳过预压缩: {Path}", wwwRoot);
            return default;
        }

        var minBytes = Math.Max(1, options.AutoPrecompressMinSizeKB) * 1024;
        var level = ParseLevel(options.AutoPrecompressLevel);

        var scanned = 0;
        var compressed = 0;
        var skipped = 0;

        foreach (var file in EnumerateFiles(wwwRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            scanned++;

            var info = new FileInfo(file);
            if (info.Length < minBytes)
            {
                skipped++;
                continue;
            }

            if (!NeedsCompress(info))
            {
                skipped++;
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            await WriteCompressedAsync(file + ".br", bytes, level, useBrotli: true, cancellationToken);
            await WriteCompressedAsync(file + ".gz", bytes, level, useBrotli: false, cancellationToken);
            compressed++;

            logger.LogDebug("预压缩: {File} ({Size:N0} bytes)", Path.GetFileName(file), info.Length);
        }

        return new WwwrootCompressResult(scanned, compressed, skipped);
    }

    private static IEnumerable<string> EnumerateFiles(string wwwRoot)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(wwwRoot, "*.*", SearchOption.AllDirectories);
        }
        catch (Exception)
        {
            yield break;
        }

        foreach (var path in files)
        {
            var ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
                continue;
            if (ext.Equals(".br", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".gz", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!CompressibleExtensions.Contains(ext))
                continue;

            yield return path;
        }
    }

    private static bool NeedsCompress(FileInfo source)
    {
        var sourceTime = source.LastWriteTimeUtc;
        return IsStale(source.FullName + ".br", sourceTime)
               || IsStale(source.FullName + ".gz", sourceTime);
    }

    private static bool IsStale(string compressedPath, DateTime sourceTimeUtc)
    {
        if (!File.Exists(compressedPath))
            return true;

        return File.GetLastWriteTimeUtc(compressedPath) < sourceTimeUtc;
    }

    private static async Task WriteCompressedAsync(
        string destPath,
        byte[] source,
        CompressionLevel level,
        bool useBrotli,
        CancellationToken cancellationToken)
    {
        var tempPath = destPath + ".tmp";
        await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            if (useBrotli)
            {
                await using var brotli = new BrotliStream(output, level, leaveOpen: true);
                await brotli.WriteAsync(source, cancellationToken);
            }
            else
            {
                await using var gzip = new GZipStream(output, level, leaveOpen: true);
                await gzip.WriteAsync(source, cancellationToken);
            }
        }

        File.Move(tempPath, destPath, overwrite: true);
    }

    private static CompressionLevel ParseLevel(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "fastest" => CompressionLevel.Fastest,
            "smallestsize" or "smallest" => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
}
