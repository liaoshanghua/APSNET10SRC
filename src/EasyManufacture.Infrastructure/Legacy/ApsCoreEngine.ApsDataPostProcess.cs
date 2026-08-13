using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// APSData 查库后处理：合计行、单元格颜色、dic 6734 list1、6705 Android 待报等。
/// 精简 APSDataCore 与 LegacyCore.APSData 共用。
/// </summary>
public partial class ApsCoreEngine
{
    /// <summary>
    /// 与旧 APSCore.APSData 查库后、序列化前一致：dicID/update、合计行、颜色、dic 6734 的 list1、6705 Android 待报。
    /// </summary>
    private List<object> PrepareApsDataTableBeforeSerialize(DataTable dt, V_Dev_Account? account)
    {
        var list1 = BuildApsDataList1(dt);

        var tableUpdate = _dbContext.DevDictionaries.AsNoTracking()
            .Where(m => m.DictionaryID == dicID)
            .Select(m => m.Remark2)
            .FirstOrDefault() == "true";

        EnsureApsDataColumn(dt, "dicID", typeof(int));
        EnsureApsDataColumn(dt, "update", typeof(bool));

        AppendApsDataFooterRow(dt);
        SetDataColor(dt);

        DataTable? materialReportToday = null;
        var isAndroid = IsAndroidUserAgent();
        if (dicID == 6705 && isAndroid)
        {
            materialReportToday = SqlHelper.ExecuteDataTable(@"
SELECT *
FROM APS_MaterialReportDetail
WHERE ProducedDate >= CAST(GETDATE() AS DATE)");
        }

        foreach (DataRow dataRow in dt.Rows)
        {
            dataRow["dicID"] = dicID;
            dataRow["update"] = tableUpdate;

            if (dicID == 6705 && isAndroid && account != null && materialReportToday != null)
            {
                var filter = $"CreatedBy='{account.Account}' AND MaterialID={dataRow["MaterialID"]} AND Remark2='{dataRow["MaterialName"]}'";
                if (materialReportToday.Select(filter).Length == 0 && dt.Columns.Contains("ProducedQty"))
                    dataRow["ProducedQty"] = DBNull.Value;
            }
        }

        return list1;
    }

    private static void EnsureApsDataColumn(DataTable dt, string columnName, Type dataType)
    {
        if (!dt.Columns.Contains(columnName))
            dt.Columns.Add(columnName, dataType);
    }

    private void AppendApsDataFooterRow(DataTable dt)
    {
        if (mSSQLCore == null || IsAndroidUserAgent())
            return;

        var footerFields = mSSQLCore.Dev_DictionaryFields
            .Where(m => !string.IsNullOrEmpty(m.FooterType))
            .ToList();
        if (footerFields.Count == 0 || dt.Rows.Count == 0)
            return;

        try
        {
            var footerRow = dt.NewRow();
            foreach (var field in footerFields)
            {
                if (!dt.Columns.Contains(field.ParameterName))
                    continue;
                var aggregate = dt.Compute($"{field.FooterType}([{field.ParameterName}])", "RowNumber>0");
                if (aggregate != null && aggregate != DBNull.Value)
                    footerRow[field.ParameterName] = aggregate;
            }
            dt.Rows.Add(footerRow);
        }
        catch
        {
            /* 与旧版一致：合计失败不影响主数据 */
        }
    }

    private List<object> BuildApsDataList1(DataTable dt)
    {
        var list1 = new List<object>();
        if (dicID != 6734)
            return list1;

        foreach (DataRow dataRow in dt.Rows)
        {
            var d1 = dataRow["HasQty"]?.ToString() == "" ? 0m : decimal.Parse(dataRow["HasQty"]!.ToString()!);
            var d2 = dataRow["ExpectTime"]?.ToString() == "" ? 0m : decimal.Parse(dataRow["ExpectTime"]!.ToString()!);
            var d3 = dataRow["Qty"]?.ToString() == "" ? 0m : decimal.Parse(dataRow["Qty"]!.ToString()!);
            if (d3 <= 0)
                continue;

            list1.Add(new
            {
                id = dataRow["ProcessPlanID"],
                LineName = dataRow["LineName"],
                OrderNo = dataRow["OrderNo"],
                Spec = dataRow["Spec"],
                Qty = dataRow["Qty"],
                start = string.Format("{0:yyyy-MM-dd}", dataRow["StartDate"]),
                duration = d2 * 60 * 60 * 1000,
                percent = d1 / d3 * 100,
                type = "project"
            });
        }

        return list1;
    }

    private static bool IsAndroidUserAgent()
    {
        var ua = LicenceRuntime.Http?.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "";
        return ua.Contains("Android", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>移植自旧 APSCore.SetDataColor。</summary>
    private void SetDataColor(DataTable dtData)
    {
        try
        {
            var dtColor = SqlHelper.ExecuteDataTable(
                "SELECT * FROM Dev_ColorConfig where Status=1 and(  ObjectID='" + dicID +
                "' OR ObjectID LIKE '%," + dicID + ",%') ");
            var isSetColor = false;

            var dtTmp = new DataTable();
            dtTmp.Columns.Add("a");
            var drNew = dtTmp.NewRow();
            drNew["a"] = "a";
            dtTmp.Rows.Add(drNew);

            if (dtColor.Rows.Count > 0)
            {
                EnsureApsDataColumn(dtData, "BColors", typeof(Dictionary<string, string>));
                EnsureApsDataColumn(dtData, "FColors", typeof(Dictionary<string, string>));
                isSetColor = true;

                var isReducedValue = dtColor.Columns.Contains("ReducedValue");
                foreach (DataRow rowData in dtData.Rows)
                {
                    var fColors = rowData["FColors"] is Dictionary<string, string> fc
                        ? new Dictionary<string, string>(fc)
                        : new Dictionary<string, string>();
                    var bColors = rowData["BColors"] is Dictionary<string, string> bc
                        ? new Dictionary<string, string>(bc)
                        : new Dictionary<string, string>();

                    foreach (DataRow rowColor in dtColor.Rows)
                    {
                        var reducedValue = isReducedValue ? rowColor["ReducedValue"]?.ToString() ?? "" : "";
                        var colorFields = rowColor["RemarkField"]?.ToString()?.Split(',') ?? Array.Empty<string>();
                        var showField = rowColor["JudgeField"]?.ToString() ?? "";

                        if (string.IsNullOrEmpty(reducedValue))
                        {
                            if (!string.IsNullOrEmpty(showField))
                            {
                                var color = rowData[showField]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(color))
                                {
                                    foreach (var colorField in colorFields)
                                    {
                                        if (string.IsNullOrEmpty(colorField))
                                            continue;
                                        ApplyColorEntry(rowColor, colorField, color, bColors, fColors);
                                    }
                                }
                            }
                            else
                            {
                                var color = rowColor["Color"]?.ToString() ?? "";
                                foreach (var colorField in colorFields)
                                    ApplyColorEntry(rowColor, colorField, color, bColors, fColors);
                            }
                        }
                        else
                        {
                            var color = rowColor["Color"]?.ToString() ?? "";
                            if (reducedValue.Length < 2)
                                continue;
                            var w = reducedValue[0].ToString();
                            reducedValue = reducedValue.Substring(1);
                            var showFields = showField.Split(',');
                            foreach (var colorField in colorFields)
                            {
                                if (string.IsNullOrEmpty(colorField))
                                    continue;
                                var flag = EvaluateColorCondition(dtData, dtTmp, rowData, colorField, w, reducedValue);
                                if (!flag)
                                    continue;

                                ApplyColorEntry(rowColor, colorField, rowColor["Color"]?.ToString() ?? "", bColors, fColors);
                                foreach (var f in showFields)
                                    ApplyColorEntry(rowColor, f, rowColor["Color"]?.ToString() ?? "", bColors, fColors);
                            }
                        }
                    }

                    foreach (var ele in elementTableOuputs.Where(m =>
                                 m.isEdit == false && m.prop2 != null && m.prop2.Contains("dy2")))
                    {
                        if (!bColors.ContainsKey(ele.prop))
                            bColors.Add(ele.prop, "#e9e1e1");
                    }

                    rowData["BColors"] = bColors;
                    rowData["FColors"] = fColors;
                }
            }

            if (!isSetColor)
            {
                EnsureApsDataColumn(dtData, "BColors", typeof(Dictionary<string, string>));
                EnsureApsDataColumn(dtData, "FColors", typeof(Dictionary<string, string>));
                foreach (var ele in elementTableOuputs.Where(m =>
                             m.isEdit == false && m.prop2 != null && m.prop2.Contains("dy2")))
                {
                    foreach (DataRow rowData in dtData.Rows)
                    {
                        rowData["BColors"] = new Dictionary<string, string> { { ele.prop, "#e9e1e1" } };
                        rowData["FColors"] = new Dictionary<string, string>();
                    }
                }
            }
        }
        catch
        {
            /* 与旧版一致 */
        }
    }

    private static void ApplyColorEntry(
        DataRow rowColor,
        string colorField,
        string color,
        Dictionary<string, string> bColors,
        Dictionary<string, string> fColors)
    {
        if (string.IsNullOrEmpty(color) || string.IsNullOrEmpty(colorField))
            return;
        var dict = rowColor["ColorType"]?.ToString() == "BColors" ? bColors : fColors;
        if (!dict.ContainsKey(colorField))
            dict.Add(colorField, color);
    }

    private static bool EvaluateColorCondition(
        DataTable dtData,
        DataTable dtTmp,
        DataRow rowData,
        string colorField,
        string w,
        string reducedValue)
    {
        if (!dtData.Columns.Contains(colorField))
            return false;

        switch (w)
        {
            case "=":
                // 空/NULL 不参与数值比较（避免 TryParse 失败后 d=0 误命中 =0）
                if (IsBlankCell(rowData, colorField))
                    return false;
                if (IsNumericColumn(dtData.Columns[colorField]))
                {
                    return decimal.TryParse(rowData[colorField]?.ToString(), out var d)
                        && d == decimal.Parse(reducedValue);
                }
                return rowData[colorField]?.ToString() == reducedValue;
            case "L":
                return rowData[colorField]?.ToString()?.StartsWith(reducedValue) == true;
            case "R":
                return rowData[colorField]?.ToString()?.EndsWith(reducedValue) == true;
            case "!":
                // 空/NULL 视为不等于具体值
                if (IsBlankCell(rowData, colorField))
                    return true;
                if (IsNumericColumn(dtData.Columns[colorField]))
                {
                    return !decimal.TryParse(rowData[colorField]?.ToString(), out var d)
                        || d != decimal.Parse(reducedValue);
                }
                return rowData[colorField]?.ToString() != reducedValue;
            case "I":
                return rowData[colorField]?.ToString()?.Contains(reducedValue) == true;
            case ">":
                if (IsBlankCell(rowData, colorField))
                    return false;
                return decimal.TryParse(rowData[colorField]?.ToString(), out var gt)
                    && gt > decimal.Parse(reducedValue);
            case "<":
                if (IsBlankCell(rowData, colorField))
                    return false;
                return decimal.TryParse(rowData[colorField]?.ToString(), out var lt)
                    && lt < decimal.Parse(reducedValue);
            case "S":
                var expr = reducedValue;
                foreach (DataColumn dc in dtData.Columns)
                {
                    if (IsNumericColumn(dc))
                    {
                        decimal.TryParse(rowData[dc.ColumnName]?.ToString(), out var d);
                        expr = expr.Replace("[" + dc.ColumnName + "]", d.ToString());
                    }
                    else
                        expr = expr.Replace("[" + dc.ColumnName + "]", rowData[dc.ColumnName]?.ToString() ?? "");
                }
                return dtTmp.Select(expr).Length > 0;
            case "#":
                return rowData[colorField]?.ToString() != "";
            case "E":
                return rowData[colorField]?.ToString() == "";
            default:
                return false;
        }
    }

    private static bool IsBlankCell(DataRow rowData, string colorField) =>
        rowData[colorField] == DBNull.Value
        || string.IsNullOrEmpty(rowData[colorField]?.ToString());

    private static bool IsNumericColumn(DataColumn column) =>
        column.DataType == typeof(decimal) || column.DataType == typeof(decimal?)
        || column.DataType == typeof(int) || column.DataType == typeof(int?)
        || column.DataType == typeof(double?) || column.DataType == typeof(float?);
}
