using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>旧 APSAPIController.GetMenus：按 dicID 查询 Dev_Menu 树（非 Home/GetMenuVue 侧边栏）。</summary>
public partial class ApsCoreEngine
{
    /// <summary>返回菜单管理树，Body 需含 dicID，可选 MenuCode / MenuName。</summary>
    public string GetMenus()
    {
        var result = true;
        var msg = "";
        var listOrg = new List<Dictionary<string, object>>();

        var jObject = JsonConvert.DeserializeObject<JObject>(BodyJson);
        if (jObject == null)
        {
            msg = "未接收到数据，请确认是否为JSON格式";
            result = false;
        }
        else if (!jObject.ContainsKey("dicID"))
        {
            msg = "没有接收到dicID";
            result = false;
        }
        else
        {
            try
            {
                dicID = int.Parse(jObject["dicID"]!.ToString());
                var mSSQLCore = new MSSQLCore(dicID, jObject, "");
                mSSQLCore.GetSql();

                var menuCode = jObject.ContainsKey("MenuCode") ? jObject["MenuCode"]?.ToString() ?? "" : "";
                var menuName = jObject.ContainsKey("MenuName") ? jObject["MenuName"]?.ToString() ?? "" : "";

                var dt = SqlHelper.ExecuteDataTable("select * from Dev_Menu where 1=1 " + mSSQLCore.ResultWhere);
                EnrichRowDataSource(dt, mSSQLCore);

                var dtParent = SqlHelper.ExecuteDataTable(
                    "select * from Dev_Menu where (ParentCode is null or ParentCode = '' ) " + mSSQLCore.ResultWhere);

                DataSet ds;
                if (string.IsNullOrEmpty(menuCode) && string.IsNullOrEmpty(menuName))
                {
                    ds = new DataSet();
                    ds.Tables.Add(dtParent.Copy());
                    if (!ds.Tables[0].Columns.Contains("dicID"))
                        ds.Tables[0].Columns.Add("dicID", typeof(int));
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(SqlHelper.MSSQLConnectionString, CommandType.Text, mSSQLCore.SQL);
                    if (!ds.Tables[0].Columns.Contains("dicID"))
                        ds.Tables[0].Columns.Add("dicID", typeof(int));
                }

                EnrichRowDataSource(ds.Tables[0], mSSQLCore);

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    if (ds.Tables[0].Columns.Contains("dicID"))
                        dr["dicID"] = 1;

                    var keyValuePairs = RowToDictionary(dr);
                    AppendMenuRowColumns(keyValuePairs, dr, dt);
                    GetMenuChildrenData(dt, dr["MenuCode"]?.ToString() ?? "", keyValuePairs, "ParentCode", "MenuCode");
                    listOrg.Add(keyValuePairs);
                }

                msg = "读取菜单成功";
            }
            catch (Exception ex)
            {
                result = false;
                msg = ex.Message;
            }
        }

        return JsonConvert.SerializeObject(new
        {
            data = listOrg,
            count = listOrg.LongCount(),
            result,
            msg
        });
    }

    /// <summary>与旧版一致：行字典补齐 dt 中的列（含下拉 Text 等）。</summary>
    private static void AppendMenuRowColumns(Dictionary<string, object> target, DataRow dr, DataTable dt)
    {
        foreach (DataColumn dataColumn in dt.Columns)
        {
            if (!target.ContainsKey(dataColumn.ColumnName) && dr.Table.Columns.Contains(dataColumn.ColumnName))
                target[dataColumn.ColumnName] = dr[dataColumn.ColumnName] == DBNull.Value ? null! : dr[dataColumn.ColumnName];
        }
    }

    private static void GetMenuChildrenData(
        DataTable dt,
        string parentCode,
        Dictionary<string, object> parentDictionary,
        string field,
        string idField)
    {
        var list = new List<Dictionary<string, object>>();
        var filter = $"{field}='{parentCode.Replace("'", "''")}'";
        foreach (DataRow dr in dt.Select(filter))
        {
            var keyValuePairs = RowToDictionary(dr);
            AppendMenuRowColumns(keyValuePairs, dr, dt);
            list.Add(keyValuePairs);
            GetMenuChildrenData(dt, dr[idField]?.ToString() ?? "", keyValuePairs, field, idField);
        }

        parentDictionary["children"] = list;
    }
}
