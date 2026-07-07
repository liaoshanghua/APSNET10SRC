using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Infrastructure.SystemInterface.K3Cloud;
using EasyManufacture.Licence;
using Microsoft.Extensions.Logging;
using System.Data;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

/// <summary>对应 Global.asax PushType=EK：每天 18:10–18:16 执行 MO 开工（EKMOToStart）。</summary>
public sealed class EkMoStartJob
{
    private readonly SystemLog _systemLog = new();
    private readonly ILogger<EkMoStartJob> _logger;

    public EkMoStartJob(ILogger<EkMoStartJob> logger) => _logger = logger;

    public Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "开始执行 EK 的 MO 开工任务", null, null);

            var dataTable = SqlHelper.ExecuteDataTable("""
                SELECT DISTINCT OrderNo
                FROM [dbo].[V_APS_OrderMoPickRepor]
                WHERE Extend4='计划确认'
                """);

            if (dataTable.Rows.Count == 0)
            {
                _systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "EK MO 开工：无待处理工单", null, null);
                return Task.CompletedTask;
            }

            var success = 0;
            var failed = 0;
            foreach (DataRow dataRow in dataTable.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var orderNo = dataRow["OrderNo"]?.ToString();
                if (string.IsNullOrEmpty(orderNo))
                    continue;

                var orderNos = "'" + orderNo.Replace("'", "''") + "'";
                var response = K3.ToStart(orderNos);
                if (response.Result?.ResponseStatus?.IsSuccess == true)
                    success++;
                else
                    failed++;
            }

            var msg = $"EK MO 开工完成：成功 {success} 条，失败 {failed} 条（共 {dataTable.Rows.Count} 条）";
            _systemLog.SaveLog(SystemLog.SystemLogType.接口推送, msg, null, null);
            _logger.LogInformation(msg);
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "EK 的 MO 开工任务执行失败：" + ex.Message, null, null);
            _logger.LogError(ex, "EK MO 开工失败");
        }

        return Task.CompletedTask;
    }
}
