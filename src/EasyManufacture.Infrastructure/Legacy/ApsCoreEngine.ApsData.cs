using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 精简版 APSData（EnableLegacyApsCoreSource=false 时使用）。
/// 全量逻辑见 LegacyCore.cs 中 APSData()；dic 钩子见 YrfDicHooks / 旧 Web override switch（待完整迁移）。
/// </summary>
public partial class ApsCoreEngine
{
    /// <summary>
    /// 对应 <c>EasyManufacture.Core.MvcControl.APSCore.APSData()</c> 主流程（通用字典查询 + SetDt 钩子）。
    /// 旧版约 3700 行 dic 分支未全量移植；复杂 dic 可配置 <c>LegacyWeb:ForwardApsData</c> 转发旧站。
    /// </summary>
    public string APSDataCore()
    {
        if (!_jdRegister.IsRegister)
        {
            return JsonConvert.SerializeObject(new
            {
                result = false,
                msg = "软件授权已经到期或者服务器已经更换，请联系软件服务商"
            });
        }

        var result = true;
        var msg = string.Empty;

        var httpRequest = LicenceRuntime.Http?.HttpContext?.Request;
        if (string.IsNullOrWhiteSpace(BodyJson) && httpRequest != null)
            BodyJson = TryReadBodyFromRequest(httpRequest);

        jObject = ApsRequestJson.ParseJObject(BodyJson, httpRequest);
        if (jObject != null && ApsRequestJson.TryGetDicId(jObject, out var parsedDicId))
            dicID = parsedDicId;

        var account = dev_Account ?? V_Dev_Account.GetDev_Account();
        // 与旧 APSCore 一致：未登录且 dicID 不在白名单则拒绝（白名单默认 24430）
        if (account == null && !lstAllSelectDicID.Contains(dicID))
        {
            return JsonConvert.SerializeObject(new
            {
                data = (object?)null,
                result = false,
                msg = "未登录"
            });
        }

        try
        {
            if (jObject == null || !ApsRequestJson.TryGetDicId(jObject, out dicID))
            {
                var preview = string.IsNullOrWhiteSpace(BodyJson)
                    ? "(空)"
                    : BodyJson.Length > 200 ? BodyJson[..200] + "..." : BodyJson;
                throw new Exception("缺少 dicID，当前参数:" + preview);
            }

            if (dicID == 6720)
                jObject["WorkShopName"] = "";

            var fields = jObject.ContainsKey("fields") ? jObject["fields"]!.ToString() : string.Empty;
            mSSQLCore = new MSSQLCore(dicID, jObject, "");
            mSSQLCore.Fields = fields ?? string.Empty;
            if (jObject.ContainsKey("groupby"))
                mSSQLCore.GroupBy = jObject["groupby"]!.ToString();

            setWhere?.Invoke(mSSQLCore);
            mSSQLCore.GetSql();

            dsData = SqlHelper.ExecuteDataset(SqlHelper.MSSQLConnectionString, CommandType.Text, mSSQLCore.SQL);

            elementTableOuputs = new List<ElementTableOuput>();
            searchFormsAll = new List<SearchForm>();
            searchForms = new List<List<SearchForm>>();
            ElementColumn.Clear();

            if (account != null)
            {
                // 与旧 APSCore.APSData 一致：GetConfigForObj 解析的是 [{"ID":dicID}]，不是 APSData 原始 Body
                var savedBodyJson = BodyJson;
                try
                {
                    BodyJson = $"[{{\"ID\":{dicID}}}]";
                    var appCols = AppColumns;
                    GetConfigForObj(
                        ref elementTableOuputs,
                        ref searchFormsAll,
                        ref searchForms,
                        ref ElementColumn,
                        ref msg,
                        ref result,
                        jObject,
                        ref appCols,
                        null);
                    AppColumns = appCols;
                }
                finally
                {
                    BodyJson = savedBodyJson;
                }
            }

            List<object> list1 = new();
            if (dsData.Tables.Count > 0)
            {
                var dt = dsData.Tables[0];

                if (setDt != null)
                {
                    var dtMut = dt;
                    setDt(ref dtMut);
                    dt = dtMut;
                    if (dt.Columns.Count != dsData.Tables[0].Columns.Count)
                        dsData.Tables.RemoveAt(0);
                    dsData.Tables.Add(dt);
                }

                if (setRowDetail != null)
                {
                    foreach (DataRow row in dsData.Tables[0].Rows)
                        setRowDetail(row);
                }

                if (setAfterReadRow != null)
                {
                    var dtAfter = dsData.Tables[0];
                    setAfterReadRow(ref dtAfter);
                }

                list1 = PrepareApsDataTableBeforeSerialize(dsData.Tables[0], account);
                count = dsData.Tables[0].Rows.Count;
            }

            var keyValuePairs = BuildApsDataRows(dsData?.Tables.Count > 0 ? dsData.Tables[0] : null, account);

            if (jObject.ContainsKey("IsOnlyData") &&
                string.Equals(jObject["IsOnlyData"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                return JsonConvert.SerializeObject(new
                {
                    data = keyValuePairs,
                    result,
                    msg,
                    count = count > 0 ? count : dsData?.Tables[0].Rows.Count ?? 0
                });
            }

            foreach (var col in ElementColumn)
            {
                if (col.All(m => m.prop != "RowNumber"))
                    col.Add(new ElementTableOuput { prop = "RowNumber", visible = true, width = "0" });
            }

            var errorMsg = msg;
            if (!result)
                msg = lang == "zh-CN" ? "查询错误" : "error";

            return JsonConvert.SerializeObject(new
            {
                data = keyValuePairs,
                dataFooter,
                result,
                Columns = ElementColumn,
                msg,
                count,
                ElementColumn,
                ExcelColumns,
                mSSQLCore.ResultWhere,
                AppColumns,
                AllColumns = lstAllColumnCommon,
                list1,
                ErrorMsg = errorMsg
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { result = false, msg = ex.Message });
        }
        finally
        {
            try
            {
                new SystemLog().SaveLog(SystemLog.SystemLogType.接口访问, "ID：" + dicID, account, null, 0, dicID);
            }
            catch { /* ignore */ }
        }
    }

    private List<Dictionary<string, object?>> BuildApsDataRows(DataTable? dt, V_Dev_Account? account)
    {
        var rows = new List<Dictionary<string, object?>>();
        if (dt == null) return rows;

        var fields = mSSQLCore!.Dev_DictionaryFields;
        foreach (DataRow dataRow in dt.Rows)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn dataColumn in dt.Columns)
            {
                var field = fields.FirstOrDefault(f =>
                    string.Equals(f.ParameterName, dataColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (field != null && !string.IsNullOrEmpty(field.ValidType) &&
                    account != null && account.RoleMap.All(m => m.RoleID != field.ValidType))
                {
                    if (dt.Columns.Contains("CreatedBy") &&
                        account.Account == dataRow["CreatedBy"]?.ToString())
                        row[dataColumn.ColumnName] = FormatApsDataCellValue(
                            dataRow[dataColumn] == DBNull.Value ? null : dataRow[dataColumn],
                            field,
                            dataColumn.DataType);
                    else
                        row[dataColumn.ColumnName] = "***";
                    continue;
                }

                var raw = dataRow[dataColumn] == DBNull.Value ? null : dataRow[dataColumn];
                row[dataColumn.ColumnName] = FormatApsDataCellValue(raw, field, dataColumn.DataType);
            }
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>与旧 APSCore.APSData 一致：日期列输出 yyyy-MM-dd，避免 JSON 中显示为 ISO T 格式。</summary>
    private static object? FormatApsDataCellValue(object? raw, Dev_DictionaryField? field, Type? columnType = null)
    {
        if (raw == null || raw is DBNull)
            return null;

        if (field != null && !string.IsNullOrEmpty(field.Formatter))
        {
            try
            {
                return string.Format(field.Formatter, raw);
            }
            catch
            {
                /* 继续按 DataType 处理 */
            }
        }

        if (field != null)
        {
            var dataType = field.DataType?.ToLowerInvariant() ?? "";
            var controlType = field.ControlType?.ToLowerInvariant() ?? "";
            if (IsDateField(dataType, controlType))
            {
                if (TryToDateTime(raw, out var dt))
                    return dt.TimeOfDay == TimeSpan.Zero
                        ? dt.ToString("yyyy-MM-dd")
                        : dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (dataType == "decimal")
            {
                var text = raw.ToString();
                if (string.IsNullOrEmpty(text))
                    return null;
                if (decimal.TryParse(text, out _))
                    return decimal.Parse(string.Format("{0:0.##########}", raw));
            }
        }

        if (raw is DateTime dateTime)
            return dateTime.TimeOfDay == TimeSpan.Zero
                ? dateTime.ToString("yyyy-MM-dd")
                : dateTime.ToString("yyyy-MM-dd HH:mm:ss");

        if (columnType == typeof(DateTime) || columnType == typeof(DateTime?))
        {
            if (TryToDateTime(raw, out var dt))
                return dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd")
                    : dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (raw is DateTimeOffset dto)
        {
            var local = dto.LocalDateTime;
            return local.TimeOfDay == TimeSpan.Zero
                ? local.ToString("yyyy-MM-dd")
                : local.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return raw;
    }

    private static bool IsDateField(string dataType, string controlType) =>
        dataType is "date" or "datetime"
        || controlType is "datebox" or "el-date-picker" or "date" or "monthrange" or "month";

    private static string TryReadBodyFromRequest(HttpRequest request)
    {
        try
        {
            if (!request.Body.CanSeek)
                return string.Empty;
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var text = reader.ReadToEnd();
            request.Body.Position = 0;
            return text ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryToDateTime(object raw, out DateTime dateTime)
    {
        switch (raw)
        {
            case DateTime dt:
                dateTime = dt;
                return true;
            case DateTimeOffset dto:
                dateTime = dto.LocalDateTime;
                return true;
            default:
                return DateTime.TryParse(raw.ToString(), out dateTime);
        }
    }
}
