using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using QueryMethod = EasyManufacture.Infrastructure.Legacy.MssqlQueryMethods.QueryMethod;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>组织树相关辅助（GetOrganize / GetOrgData 实现在 APSAPIController.LegacyBusiness）。</summary>
public partial class ApsCoreEngine
{
    private static readonly Dictionary<string, DataTable> LstDataSourceDefs = new();
    private readonly Dictionary<string, DataTable> _lstDataSourceData = new();

    /// <summary>生成行的下拉数据（旧 APSCore.GetDataSource(DataTable, MSSQLCore)）。</summary>
    protected void EnrichRowDataSource(DataTable dt, MSSQLCore mSSQLCore)
    {
        if (!dt.Columns.Contains("BColors"))
            dt.Columns.Add("BColors", typeof(Dictionary<string, string>));
        if (!dt.Columns.Contains("FColors"))
            dt.Columns.Add("FColors", typeof(Dictionary<string, string>));

        foreach (var n in mSSQLCore.Dev_DictionaryFields.Where(m =>
                     (m.ControlType == "combobox" || m.ControlType == "el-select") && m.IsVisible == true))
        {
            if (!string.IsNullOrEmpty(n.DataSourceID))
            {
                if (n.QueryMethod == (int)QueryMethod.模糊匹配 || n.QueryMethod == (int)QueryMethod.精确匹配 ||
                    n.QueryMethod.HasValue == false)
                {
                    if (!LstDataSourceDefs.TryGetValue(n.DataSourceID, out var dataTable))
                    {
                        dataTable = SqlHelper.ExecuteDataTable(
                            "SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");
                        LstDataSourceDefs[n.DataSourceID] = dataTable;
                    }

                    if (dt.Columns.Contains(n.ParameterName + "Text") == false)
                        dt.Columns.Add(n.ParameterName + "Text");

                    if (dataTable.Rows.Count > 0)
                    {
                        var value = dataTable.Rows[0]["DataValue"]?.ToString() ?? "";
                        var label = dataTable.Rows[0]["DataText"]?.ToString() ?? "";
                        var usql = dataTable.Rows[0]["USQL"]?.ToString() ?? "";
                        try
                        {
                            if (dev_Account != null)
                            {
                                usql = usql.Replace("{CenterID}", dev_Account.CenterID.ToString());
                                usql = usql.Replace("{WorkFlowInstanceID}", dev_Account.WorkFlowInstanceID ?? "");
                                usql = usql.Replace("{Account}", dev_Account.Account);
                                usql = usql.Replace("{GroupID}", dev_Account.GroupID.ToString());
                            }
                        }
                        catch
                        {
                            // 与旧版一致：替换失败时忽略
                        }

                        if (!_lstDataSourceData.TryGetValue(n.DataSourceID, out var dtUsql))
                        {
                            dtUsql = SqlHelper.ExecuteDataTable(usql);
                            _lstDataSourceData[n.DataSourceID] = dtUsql;
                        }

                        if (!dtUsql.Columns.Contains("value"))
                            dtUsql.Columns.Add("value", dtUsql.Columns[value].DataType);
                        if (!dtUsql.Columns.Contains("label"))
                            dtUsql.Columns.Add("label");
                        if (!dtUsql.Columns.Contains("text"))
                            dtUsql.Columns.Add("text");

                        if (!value.Equals("value", StringComparison.OrdinalIgnoreCase))
                            dtUsql.Columns["value"]!.Expression = value;
                        if (!value.Equals("label", StringComparison.OrdinalIgnoreCase))
                            dtUsql.Columns["label"]!.Expression = label;
                        if (!value.Equals("text", StringComparison.OrdinalIgnoreCase))
                            dtUsql.Columns["text"]!.Expression = label;

                        var dataSourceName = dataTable.Rows[0]["DataSouceName"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(dataSourceName) && !dt.Columns.Contains(dataSourceName))
                            dt.Columns.Add(dataSourceName, typeof(DataTable));

                        var listColumnsFilter = new List<string>();
                        foreach (DataRow rowData in dt.Rows)
                        {
                            var dtDataSource = dtUsql.Clone();
                            if (dataTable.Columns.Contains("RowFilter") &&
                                !string.IsNullOrEmpty(dataTable.Rows[0]["RowFilter"]?.ToString()))
                            {
                                var rowFilter = dataTable.Rows[0]["RowFilter"]!.ToString()!;
                                if (listColumnsFilter.Count == 0)
                                {
                                    foreach (DataColumn dataColumn1 in dt.Columns)
                                    {
                                        if (rowFilter.Contains(dataColumn1.ColumnName, StringComparison.Ordinal))
                                            listColumnsFilter.Add(dataColumn1.ColumnName);
                                    }
                                }

                                foreach (var s in listColumnsFilter)
                                    rowFilter = rowFilter.Replace("{" + s + "}", rowData[s].ToString());

                                try
                                {
                                    var drSelect = dtUsql.Select(rowFilter);
                                    foreach (var dataRow in drSelect)
                                        dtDataSource.ImportRow(dataRow);

                                    if (drSelect.Length == 0 && dt.Columns.Contains("RowNumber") &&
                                        rowData["RowNumber"]?.ToString() != "")
                                    {
                                        var bColors = new Dictionary<string, string> { { n.ParameterName, "#F25353" } };
                                        rowData["BColors"] = bColors;
                                    }
                                }
                                catch
                                {
                                    // 与旧版一致
                                }

                                if (!string.IsNullOrEmpty(dataSourceName))
                                    rowData[dataSourceName] = dtDataSource;
                            }
                            else
                            {
                                dtDataSource = dtUsql;
                            }

                            if ((lstDayList.Contains(dicID) || AppInfo.PushType == "TP") &&
                                !string.IsNullOrEmpty(dataSourceName))
                                rowData[dataSourceName] = dtDataSource;

                            try
                            {
                                if (!string.IsNullOrEmpty(rowData[n.ParameterName]?.ToString()))
                                {
                                    var dataRows = dtDataSource.Select("Value='" + rowData[n.ParameterName] + "'");
                                    if (dataRows.Length > 0)
                                        rowData[n.ParameterName + "Text"] = dataRows[0]["label"]?.ToString();
                                }
                            }
                            catch
                            {
                                // 与旧版一致
                            }
                        }
                    }
                }
            }
            else if (n.ParameterName == "QueryMethod")
            {
                if (!dt.Columns.Contains("QueryMethodProp"))
                    dt.Columns.Add("QueryMethodProp", typeof(DataTable));

                var dtUsql = new DataTable();
                dtUsql.Columns.Add("value", typeof(int));
                dtUsql.Columns.Add("label");
                dtUsql.Columns.Add("text");

                var index = 0;
                foreach (var obj in Enum.GetValues(typeof(QueryMethod)))
                {
                    var newRow = dtUsql.NewRow();
                    newRow["value"] = index;
                    newRow["label"] = obj.ToString();
                    newRow["text"] = obj.ToString();
                    dtUsql.Rows.Add(newRow);
                    index++;
                }

                if (!dt.Columns.Contains(n.ParameterName + "Text"))
                    dt.Columns.Add(n.ParameterName + "Text");

                foreach (DataRow dataRow1 in dt.Rows)
                {
                    dataRow1["QueryMethodProp"] = dtUsql;
                    try
                    {
                        if (!string.IsNullOrEmpty(dataRow1[n.ParameterName]?.ToString()))
                        {
                            var dataRows = dtUsql.Select("Value='" + dataRow1[n.ParameterName] + "'");
                            if (dataRows.Length > 0)
                                dataRow1[n.ParameterName + "Text"] = dataRows[0]["label"]?.ToString();
                        }
                    }
                    catch
                    {
                        // 与旧版一致
                    }
                }
            }
        }
    }
}
