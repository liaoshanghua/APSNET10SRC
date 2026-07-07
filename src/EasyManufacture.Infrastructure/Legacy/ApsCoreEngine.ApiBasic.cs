using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>基础接口：移植自旧 <c>APSAPIController.GetGroupName</c> 与 <c>APSCore.GetDataSource</c>。</summary>
public partial class ApsCoreEngine
{
    /// <summary>登录页组织下拉 + SSO 配置（旧 APSAPIController.GetGroupName）。</summary>
    public string GetGroupName()
    {
        var ssoResult = false;
        var url = "";
        var buttonName = "";
        var loginOutUrl = "";

        if (!string.IsNullOrEmpty(AppInfo.SSOUrl))
        {
            var parts = AppInfo.SSOUrl.Split('‖').Select(p => p.Trim()).ToArray();
            if (parts.Length >= 3)
            {
                ssoResult = true;
                url = parts[1] + parts[2];
                buttonName = parts[0];
                if (parts.Length > 5)
                    loginOutUrl = parts[1] + parts[5];
            }
        }

        var ssoData = new
        {
            url,
            buttonName,
            loginOutUrl,
            AppInfo.IsChangePwd
        };

        try
        {
            var dataTable = SqlHelper.ExecuteDataTable(@"SELECT GroupName FROM Dev_Organize
WHERE Status = 1 AND GroupName <> ''
GROUP BY GroupName");

            return JsonConvert.SerializeObject(new
            {
                groupNameData = dataTable,
                result = dataTable.Rows.Count > 1,
                msg = "OK",
                ssoResult,
                ssoData
            });
        }
        catch
        {
            return JsonConvert.SerializeObject(new
            {
                data = (object?)null,
                result = false,
                msg = "没有GoupName",
                ssoResult,
                ssoData
            });
        }
    }

#if !LEGACY_APS_CORE
    /// <summary>下拉数据源（旧 APSCore.GetDataSource），Body 需含 DataSourceID。</summary>
    public string GetDataSource()
    {
        var result = true;
        var msg = "";
        DataTable? dtUSQL = null;

        try
        {
            var jo = JsonConvert.DeserializeObject<JObject>(BodyJson);
            if (jo == null)
            {
                msg = "未接收到数据，请确认是否为JSON格式";
                result = false;
            }
            else if (!jo.ContainsKey("DataSourceID"))
            {
                msg = "未接收到DataSourceID";
                result = false;
            }
            else
            {
                var dataSourceId = jo["DataSourceID"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(dataSourceId))
                {
                    msg = "未接收到DataSourceID";
                    result = false;
                }
                else
                {
                    var account = dev_Account;
                    if (account == null)
                    {
                        result = false;
                        msg = "未登录";
                    }
                    else
                    {
                        var safeId = dataSourceId.Replace("'", "''");
                        var dataTable = SqlHelper.ExecuteDataTable(
                            $"SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='{safeId}'");

                        if (dataTable.Rows.Count == 0)
                        {
                            result = false;
                            msg = "记录不存在";
                        }
                        else
                        {
                            var usql = dataTable.Rows[0]["USQL"]?.ToString() ?? "";
                            try
                            {
                                usql = usql
                                    .Replace("{CenterID}", account.CenterID.ToString(), StringComparison.OrdinalIgnoreCase)
                                    .Replace("{WorkFlowInstanceID}", account.WorkFlowInstanceID ?? "", StringComparison.OrdinalIgnoreCase)
                                    .Replace("{Account}", account.Account, StringComparison.OrdinalIgnoreCase)
                                    .Replace("{GroupID}", account.GroupID.ToString(), StringComparison.OrdinalIgnoreCase);
                            }
                            catch (Exception ex)
                            {
                                result = false;
                                msg = ex.Message;
                            }

                            if (result && !string.IsNullOrEmpty(account.Extend2))
                            {
                                usql = usql.Contains("where", StringComparison.OrdinalIgnoreCase)
                                    ? usql + " AND GroupName='" + account.Extend2.Replace("'", "''") + "'"
                                    : usql + " where GroupName='" + account.Extend2.Replace("'", "''") + "'";
                            }

                            if (result)
                            {
                                dtUSQL = SqlHelper.ExecuteDataTable(usql);
                                msg = "查询成功";
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result = false;
            msg = ex.Message;
        }

        return JsonConvert.SerializeObject(new
        {
            data = dtUSQL,
            result,
            msg,
            count = dtUSQL?.Rows.Count ?? 0
        });
    }
#endif
}
