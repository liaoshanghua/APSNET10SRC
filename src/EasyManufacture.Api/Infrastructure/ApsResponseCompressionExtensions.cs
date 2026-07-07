using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// HTTP 动态响应压缩（对齐 IIS urlCompression 动态压缩：gzip + brotli）。
/// </summary>
internal static class ApsResponseCompressionExtensions
{
    /// <summary>与 IIS 动态压缩常见的 MIME 类型对齐。</summary>
    private static readonly string[] IisLikeMimeTypes =
    [
        "text/plain",
        "text/html",
        "text/css",
        "text/xml",
        "text/json",
        "application/json",
        "application/javascript",
        "text/javascript",
        "application/x-javascript",
        "application/xml",
        "application/xml+rss",
        "application/atom+xml",
        "image/svg+xml",
        "application/wasm"
    ];

    public static IServiceCollection AddApsResponseCompression(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!configuration.GetValue("ResponseCompression:Enabled", true))
            return services;

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = configuration.GetValue("ResponseCompression:EnableForHttps", true);
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                .Concat(IisLikeMimeTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        });

        var level = ParseCompressionLevel(configuration["ResponseCompression:Level"]);
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = level);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = level);

        return services;
    }

    public static IApplicationBuilder UseApsResponseCompression(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        if (!configuration.GetValue("ResponseCompression:Enabled", true))
            return app;

        return app.UseResponseCompression();
    }

    private static CompressionLevel ParseCompressionLevel(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "optimal" => CompressionLevel.Optimal,
            "smallestsize" or "smallest" => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Fastest // 对齐 IIS 动态压缩默认倾向：优先速度
        };
}
