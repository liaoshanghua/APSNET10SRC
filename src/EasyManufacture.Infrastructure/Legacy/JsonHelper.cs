using System.Data;

namespace EasyManufacture.Core;

/// <summary>兼容旧 EasyManufacture.Core.JsonHelper（非 Legacy 代码使用）。</summary>
public static class JsonHelper
{
    public static List<Dictionary<string, object>> GetListFromDt(DataTable dt) =>
        EasyManufacture.Infrastructure.Legacy.LegacyJsonExtensions.GetListFromDt(dt);
}
