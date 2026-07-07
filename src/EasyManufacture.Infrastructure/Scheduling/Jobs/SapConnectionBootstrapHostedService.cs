using EasyManufacture.Infrastructure.SystemInterface.SAP;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 对应旧版 Global.asax 中 InterfaceSAP.Start 的 RFC 连接初始化（不启动内置 Timer，由 <see cref="SapInterfaceSyncJob"/> 调度）。
/// </summary>
public sealed class SapConnectionBootstrapHostedService : IHostedService
{
    private readonly ILogger<SapConnectionBootstrapHostedService> _logger;

    public SapConnectionBootstrapHostedService(ILogger<SapConnectionBootstrapHostedService> logger) =>
        _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            InterfaceSAP.Start(isRunSplit: false);
            _logger.LogInformation("SAP RFC 连接已初始化（InterfaceSAP.Start isRunSplit=false）");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAP RFC 连接初始化失败，请检查 app.config 中 SAP.Middleware.Connector 配置");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
