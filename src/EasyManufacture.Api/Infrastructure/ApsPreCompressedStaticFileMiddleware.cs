using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using System.Reflection;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 优先返回 wwwroot 下预压缩的 .br / .gz（大 JS/CSS 体积更小、CPU 更低），无预压缩文件时走后续动态压缩。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal sealed class ApsPreCompressedStaticFileMiddleware
{
    private static readonly HashSet<string> CompressibleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".js", ".css", ".json", ".html", ".htm", ".svg", ".xml", ".wasm", ".txt", ".map"
    };

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly FileExtensionContentTypeProvider _contentTypes = new();

    public ApsPreCompressedStaticFileMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var requestPath = context.Request.Path.Value;
        if (string.IsNullOrEmpty(requestPath) || !TryGetPhysicalPath(requestPath, out var originalPath))
        {
            await _next(context);
            return;
        }

        if (!File.Exists(originalPath))
        {
            await _next(context);
            return;
        }

        var accept = context.Request.Headers.AcceptEncoding.ToString();
        if (accept.Contains("br", StringComparison.OrdinalIgnoreCase)
            && TryGetFreshCompressedPath(originalPath, ".br", out var brPath))
        {
            await ServeAsync(context, brPath, originalPath, "br");
            return;
        }

        if (accept.Contains("gzip", StringComparison.OrdinalIgnoreCase)
            && TryGetFreshCompressedPath(originalPath, ".gz", out var gzPath))
        {
            await ServeAsync(context, gzPath, originalPath, "gzip");
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 仅当预压缩文件不早于原文件时使用（避免 index.html 已换 Vue，仍返回旧的 .br 占位页）。
    /// </summary>
    private static bool TryGetFreshCompressedPath(string originalPath, string suffix, out string compressedPath)
    {
        compressedPath = originalPath + suffix;
        if (!File.Exists(compressedPath))
            return false;

        var originalTime = File.GetLastWriteTimeUtc(originalPath);
        var compressedTime = File.GetLastWriteTimeUtc(compressedPath);
        if (compressedTime < originalTime)
            return false;

        return true;
    }

    private bool TryGetPhysicalPath(string requestPath, out string physicalPath)
    {
        physicalPath = string.Empty;
        var ext = Path.GetExtension(requestPath);
        if (string.IsNullOrEmpty(ext) || !CompressibleExtensions.Contains(ext))
            return false;

        if (string.IsNullOrEmpty(_environment.WebRootPath))
            return false;

        var relative = requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        physicalPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relative));

        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        if (!physicalPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private async Task ServeAsync(HttpContext context, string compressedPath, string originalPath, string encoding)
    {
        var fileInfo = new FileInfo(compressedPath);
        var contentType = _contentTypes.TryGetContentType(originalPath, out var type)
            ? type
            : "application/octet-stream";

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = contentType;
        context.Response.Headers[HeaderNames.ContentEncoding] = encoding;
        context.Response.Headers[HeaderNames.Vary] = HeaderNames.AcceptEncoding;
        context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=31536000,immutable";
        context.Response.ContentLength = fileInfo.Length;
        context.Response.Headers[HeaderNames.LastModified] = fileInfo.LastWriteTimeUtc.ToString("R");

        if (HttpMethods.IsHead(context.Request.Method))
            return;

        await using var stream = new FileStream(
            compressedPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}

internal static class ApsPreCompressedStaticFileExtensions
{
    public static IApplicationBuilder UseApsPreCompressedStaticFiles(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        if (!configuration.GetValue("ResponseCompression:PreCompressedStaticFiles", true))
            return app;

        // 不用 UseMiddleware<T>（Reactor 混淆会重命名 InvokeAsync，反射注册会失败）
        return app.Use((RequestDelegate next) =>
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var middleware = new ApsPreCompressedStaticFileMiddleware(next, env);
            return middleware.InvokeAsync;
        });
    }
}
