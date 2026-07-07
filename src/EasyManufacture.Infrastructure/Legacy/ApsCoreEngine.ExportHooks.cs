namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>APSDataExcel 导出前钩子（由 APSAPIController 覆盖以挂载 dic SetDt）。</summary>
public partial class ApsCoreEngine
{
    protected virtual void ApplyApsDataExportHooks()
    {
    }
}
