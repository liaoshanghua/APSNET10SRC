using EasyManufacture.Domain.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyManufacture.Infrastructure.Scheduling;

/// <summary>应用启动时执行一次数据库结构升级（Global.asax Application_Start 前半段）。</summary>
public sealed class DatabaseSchemaUpgradeHostedService : IHostedService
{
    private readonly ILogger<DatabaseSchemaUpgradeHostedService> _logger;
    private readonly ScheduledTasksOptions _options;

    public DatabaseSchemaUpgradeHostedService(
        ILogger<DatabaseSchemaUpgradeHostedService> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.RunSchemaUpgradeOnStartup)
        {
            _logger.LogInformation("数据库结构自检已跳过（ScheduledTasks 配置）");
            return Task.CompletedTask;
        }

        DatabaseSchemaUpgrader.Run(_logger);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
