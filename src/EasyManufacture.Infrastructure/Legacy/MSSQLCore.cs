using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QueryMethod = EasyManufacture.Infrastructure.Legacy.MssqlQueryMethods.QueryMethod;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 数据库查询构建（从旧版 MSSQLCore 精简，覆盖 APSData 主路径）。
/// 极复杂字典逻辑若未覆盖，可配置 LegacyWeb:ForwardApsData=true 转发旧站。
/// </summary>
public class MSSQLCore
{
    private readonly DataTable _dtDictionary;
    private List<Dev_DictionaryField>? _fieldsCache;

    public MSSQLCore(int dictionaryId, JToken? jsToken, string prefix = "")
    {
        DictionaryID = dictionaryId;
        JsToken = jsToken;
        Prefix = prefix ?? string.Empty;
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN", true)
        {
            DateTimeFormat =
            {
                ShortDatePattern = "yyyy-MM-dd",
                FullDateTimePattern = "yyyy-MM-dd HH:mm:ss",
                LongTimePattern = "HH:mm:ss"
            }
        };

        _dtDictionary = SqlHelper.ExecuteDataTable($"""
            SELECT A.*, B.ObjectName, B.TabelName, B.ObjectText, B.AfterUpdate, B.BeforeUpdate,
                   B.AfterAdd, B.Condition, B.BeforeAdd, B.AfterDelete
            FROM Dev_DictionaryField (NOLOCK) A
            INNER JOIN Dev_Dictionary (NOLOCK) B ON A.DictionaryID = B.DictionaryID
            WHERE A.DictionaryID = {dictionaryId} AND A.IsSelect = 1
            ORDER BY A.FieldIndex
            """);

        if (_dtDictionary.Rows.Count == 0)
            throw new Exception("未找到操作对象");

        ObjectName = _dtDictionary.Rows[0]["ObjectName"]?.ToString() ?? string.Empty;
        TabelName = _dtDictionary.Rows[0]["TabelName"]?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(TabelName))
            TabelName = ObjectName;
        ObjectText = _dtDictionary.Rows[0]["ObjectText"]?.ToString() ?? string.Empty;
        Condition = " " + ReplaceSqlCondition(_dtDictionary.Rows[0]["Condition"]?.ToString() ?? string.Empty);
    }

    public int DictionaryID { get; }
    public string Prefix { get; set; } = string.Empty;
    public JToken? JsToken { get; }
    public string ObjectName { get; }
    public string TabelName { get; }
    public string ObjectText { get; }
    public string Condition { get; }
    public string Where { get; set; } = string.Empty;
    public string OrderBy { get; set; } = string.Empty;
    public string GroupBy { get; set; } = string.Empty;
    public string Fields { get; set; } = string.Empty;
    public string SQL { get; private set; } = string.Empty;
    public string SQLOfSelect { get; private set; } = string.Empty;
    public string SQLOfCount { get; private set; } = string.Empty;
    public string ResultWhere { get; set; } = string.Empty;

    public List<Dev_DictionaryField> Dev_DictionaryFields
    {
        get
        {
            if (_fieldsCache != null) return _fieldsCache;
            _fieldsCache = LegacyDbFactory.CreateEntities().Dev_DictionaryField
                .Where(m => m.DictionaryID == DictionaryID && m.IsSelect == true)
                .OrderByDescending(m => m.IsFrozen)
                .ToList();
            return _fieldsCache;
        }
    }

    public void GetSql() => GetSql(OperationType.Select);

    public void GetSql(OperationType operationType)
    {
        if (operationType != OperationType.Select)
            throw new NotSupportedException("Net10 精简版 MSSQLCore 仅支持查询");

        var devAccount = V_Dev_Account.GetDev_Account() ?? new V_Dev_Account();
        var pageSize = 200;
        var pageIndex = 1;
        if (JsToken?["rows"] != null)
            int.TryParse(JsToken["rows"]!.ToString(), out pageSize);
        if (JsToken?["page"] != null)
            int.TryParse(JsToken["page"]!.ToString(), out pageIndex);
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize <= 0) pageSize = 200;

        var fields = Fields;
        if (string.IsNullOrEmpty(fields))
        {
            fields = string.Join(",", Dev_DictionaryFields.Select(f => f.ParameterName));
        }
        if (string.IsNullOrWhiteSpace(fields))
            fields = "*";

        // 与旧版一致：ResultWhere 直接拼接字典 Condition / Where，不以空 " AND " 开头
        var where = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(Condition))
            where.Append(Condition);
        if (!string.IsNullOrEmpty(Where))
            where.Append(Where);

        AppendOrganizeFilter(devAccount, where);
        AppendTokenFilters(where);

        ResultWhere = where.ToString();
        var orderBy = string.IsNullOrWhiteSpace(OrderBy) ? GetDefaultOrder() : OrderBy.Trim().TrimEnd(',');

        if (!string.IsNullOrEmpty(GroupBy))
        {
            SQL = $"SELECT {fields} FROM {ObjectName}(NOLOCK) WHERE 1=1 {ResultWhere} GROUP BY {GroupBy}";
            if (!string.IsNullOrWhiteSpace(orderBy))
                SQL += $" ORDER BY {orderBy}";
        }
        else
        {
            var offset = (pageIndex - 1) * pageSize;
            SQL = $"""
                SELECT {fields} FROM {ObjectName}(NOLOCK) WHERE 1=1 {ResultWhere}
                ORDER BY {orderBy}
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                """;
        }

        SQLOfSelect = SQL;
        SQLOfCount = $"SELECT COUNT(1) FROM {ObjectName}(NOLOCK) WHERE 1=1 {ResultWhere}";
    }

    private string GetDefaultOrder()
    {
        var key = Dev_DictionaryFields.FirstOrDefault(f => f.IsKey == true)?.ParameterName;
        return string.IsNullOrEmpty(key) ? Dev_DictionaryFields.FirstOrDefault()?.ParameterName ?? "1" : key;
    }

    private void AppendOrganizeFilter(V_Dev_Account account, StringBuilder where)
    {
        if (!AppInfo.IsMultiOrganization || account.Account.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return;

        where.Append($" AND (OrganizeID IS NULL OR OrganizeID = {account.CenterID})");
    }

    private void AppendTokenFilters(StringBuilder where)
    {
        if (JsToken is not JObject jo) return;

        foreach (DataRow dr in _dtDictionary.Rows)
        {
            var parameterName = dr["ParameterName"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(parameterName)) continue;
            if (!jo.ContainsKey(parameterName)) continue;

            var token = jo[parameterName];
            if (token == null || token.Type == JTokenType.Null) continue;

            if (token is JArray arr)
            {
                if (arr.Count == 0) continue;
                if (arr.Count >= 2 && IsDateColumn(dr))
                {
                    var v0 = arr[0]?.ToString();
                    var v1 = arr[1]?.ToString();
                    if (!string.IsNullOrEmpty(v0))
                        where.Append($" AND {parameterName} >= '{Escape(v0)}'");
                    if (!string.IsNullOrEmpty(v1))
                        where.Append($" AND {parameterName} <= '{Escape(v1)}'");
                }
                else
                {
                    var inList = string.Join(",", arr.Select(a => $"'{Escape(a.ToString())}'"));
                    where.Append($" AND {parameterName} IN ({inList})");
                }
                continue;
            }

            var value = token.ToString().Trim();
            if (string.IsNullOrEmpty(value)) continue;

            var method = QueryMethod.精确匹配;
            if (int.TryParse(dr["QueryMethod"]?.ToString(), out var qm))
                method = (QueryMethod)qm;

            where.Append(BuildCondition(parameterName, value, method, dr));
        }
    }

    private static bool IsDateColumn(DataRow dr)
    {
        var ct = dr["ControlType"]?.ToString()?.ToLower() ?? string.Empty;
        var dt = dr["DataType"]?.ToString()?.ToLower() ?? string.Empty;
        return ct is "datebox" or "el-date-picker" || dt.Contains("date");
    }

    private static string BuildCondition(string column, string value, QueryMethod method, DataRow dr)
    {
        value = Escape(value);
        return method switch
        {
            QueryMethod.模糊匹配 => $" AND {column} LIKE '%{value}%'",
            QueryMethod.左匹配 => $" AND {column} LIKE '{value}%'",
            QueryMethod.右匹配 => $" AND {column} LIKE '%{value}'",
            QueryMethod.大于 => $" AND {column} > '{value}'",
            QueryMethod.小于 => $" AND {column} < '{value}'",
            QueryMethod.IN => $" AND {column} IN ({string.Join(",", value.Split(',').Select(v => $"'{Escape(v.Trim())}'"))})",
            _ => $" AND {column} = '{value}'"
        };
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string ReplaceSqlCondition(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return string.Empty;
        var account = V_Dev_Account.GetDev_Account();
        if (account == null) return sql;
        return sql
            .Replace("{Account}", account.Account, StringComparison.OrdinalIgnoreCase)
            .Replace("{CenterID}", account.CenterID.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{GroupID}", account.GroupID.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{WorkFlowInstanceID}", account.WorkFlowInstanceID ?? "", StringComparison.OrdinalIgnoreCase);
    }

    public enum OperationType
    {
        Select, Insert, Update, Delete
    }
}
