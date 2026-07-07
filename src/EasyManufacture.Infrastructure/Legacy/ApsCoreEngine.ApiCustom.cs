using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>APSData 行级/读表后钩子委托（与旧 APSCore setRowDetail、setAfterReadRow 一致）。</summary>
public delegate void SetRowDetail(DataRow dataRow);
public delegate void SetAfterReadRow(ref DataTable dt);

/// <summary>
/// APSData 统一入口：ApplyApsDataDicHooks 后调用 APSData（LegacyCore）或 APSDataCore（精简）。
/// 对应旧 Web APSAPIController 末尾 return base.APSData() 之前的 dic 分发（部分）。
/// </summary>
public partial class ApsCoreEngine
{
    protected DataTable? DtDetail;
    protected DataTable? DtDetail1;
    protected DataTable? DtDetail2;
    protected DataSet? DsDetail;
#if !LEGACY_APS_CORE
    protected DataSet? dsData;
    protected SetRowDetail? setRowDetail;
    protected SetAfterReadRow? setAfterReadRow;
#endif

    private string GetUserAgent() =>
        LicenceRuntime.Http.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? "";

    private static string? GetFormValue(string key) =>
        LicenceRuntime.Http.HttpContext?.Request.GetRequestValue(key);

    private static string? GetQueryValue(string key) =>
        LicenceRuntime.Http.HttpContext?.Request.Query.TryGetValue(key, out var v) == true ? v.ToString() : null;

    public string RunAPSData()
    {
#if LEGACY_APS_CORE
        return APSData();
#else
        ApplyApsDataDicHooks();
        return APSDataCore();
#endif
    }
}
