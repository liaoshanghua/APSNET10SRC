using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Infrastructure.SystemInterface.SAP;
using EasyManufacture.Licence;
using Microsoft.Extensions.Logging;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 对应旧版 InterfaceSAP.Start(true) 中 Timer 触发的 <see cref="InterfaceSAP.Invoke"/>。
/// RFC 连接由 <see cref="SapConnectionBootstrapHostedService"/> 在启动时初始化。
/// </summary>
public sealed class SapInterfaceSyncJob
{
    private readonly SystemLog _systemLog = new();
    private readonly ILogger<SapInterfaceSyncJob> _logger;

    public SapInterfaceSyncJob(ILogger<SapInterfaceSyncJob> logger) => _logger = logger;

    public Task RunAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(AppInfo.PushType))
            return Task.CompletedTask;

        try
        {
            var dataTable = SqlHelper.ExecuteDataTable("""
                SELECT TOP 1 FID FROM [dbo].[APS_InterfaceSAP] WHERE [status]=1
                """);
            if (dataTable.Rows.Count == 0)
                return Task.CompletedTask;

            InterfaceSAP.Invoke();
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);
            _logger.LogError(ex, "SAP 接口同步失败");
        }

        return Task.CompletedTask;
    }
}
