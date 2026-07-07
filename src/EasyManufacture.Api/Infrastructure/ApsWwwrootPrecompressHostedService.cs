using Microsoft.Extensions.Options;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>APS 启动后在后台为 wwwroot 大文件生成 .br / .gz（不阻塞监听端口）。</summary>
internal sealed class ApsWwwrootPrecompressHostedService : IHostedService
{
    private readonly IWebHostEnvironment _environment;
    private readonly WwwrootCompressOptions _options;
    private readonly ILogger<ApsWwwrootPrecompressHostedService> _logger;

    public ApsWwwrootPrecompressHostedService(
        IWebHostEnvironment environment,
        IOptions<WwwrootCompressOptions> options,
        ILogger<ApsWwwrootPrecompressHostedService> logger)
    {
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled
            || !_options.PreCompressedStaticFiles
            || !_options.AutoPrecompressOnStartup)
        {
            return Task.CompletedTask;
        }

        var wwwRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(wwwRoot) || !Directory.Exists(wwwRoot))
        {
            _logger.LogDebug("wwwroot 未就绪，跳过启动预压缩");
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "wwwroot 预压缩已在后台启动（>{Min}KB 的 js/css 等，Level={Level}）",
            _options.AutoPrecompressMinSizeKB,
            _options.AutoPrecompressLevel);

        _ = Task.Run(async () =>
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await ApsWwwrootCompressor.RunAsync(
                    wwwRoot,
                    _options,
                    _logger,
                    cancellationToken);

                sw.Stop();
                _logger.LogInformation(
                    "wwwroot 预压缩完成：扫描 {Scanned}，新生成/更新 {Compressed}，跳过 {Skipped}，耗时 {Ms}ms",
                    result.Scanned,
                    result.Compressed,
                    result.Skipped,
                    sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 正常关闭
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "wwwroot 启动预压缩失败（不影响 APS 运行）");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class ApsWwwrootPrecompressExtensions
{
    public static IServiceCollection AddApsWwwrootPrecompress(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WwwrootCompressOptions>(configuration.GetSection("ResponseCompression"));
        services.AddHostedService<ApsWwwrootPrecompressHostedService>();
        return services;
    }
}
