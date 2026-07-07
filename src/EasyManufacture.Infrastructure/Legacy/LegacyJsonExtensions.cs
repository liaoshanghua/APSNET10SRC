using System.Data;
using System.Globalization;
using Newtonsoft.Json;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容旧 EasyManufacture.Core.JsonHelper（ToJSON / ToJsonLegacy）。</summary>
public static class LegacyJsonExtensions
{
    public static string ToJSON(this object? dObj) =>
        dObj == null ? "null" : JsonConvert.SerializeObject(dObj);

    public static string ToJsonLegacy(this object? dObj) => ToJSON(dObj);

    public static T? FromJSON<T>(string jsonStr) where T : class =>
        JsonConvert.DeserializeObject<T>(jsonStr);

    public static List<Dictionary<string, object>> GetListFromDt(DataTable dt)
    {
        var list = new List<Dictionary<string, object>>();
        foreach (DataRow dataRow in dt.Rows)
        {
            var row = new Dictionary<string, object>();
            foreach (DataColumn col in dt.Columns)
            {
                if (string.Equals(col.ColumnName, "signName", StringComparison.OrdinalIgnoreCase))
                {
                    var ints = new List<int>();
                    var raw = dataRow[col]?.ToString();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        foreach (var part in raw.Split(','))
                        {
                            var s = part.Trim();
                            if (!string.IsNullOrEmpty(s) && StringHelper.IsNumber(s))
                            {
                                try
                                {
                                    ints.Add(int.Parse(s, CultureInfo.InvariantCulture));
                                }
                                catch
                                {
                                    // 与旧 JsonHelper 一致：非法片段跳过
                                }
                            }
                        }
                    }

                    if (ints.Count > 0)
                        row[col.ColumnName] = ints;
                }
                else
                {
                    row[col.ColumnName] = dataRow[col];
                }
            }

            list.Add(row);
        }

        return list;
    }
}
