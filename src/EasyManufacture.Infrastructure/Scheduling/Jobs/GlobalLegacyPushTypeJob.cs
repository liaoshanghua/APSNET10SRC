using EasyManufacture.Infrastructure.Legacy;
using EastSap = EasyManufacture.Infrastructure.SystemInterface.EAST.SAP;
using EasyManufacture.Infrastructure.SystemInterface.IoT;
using EasyManufacture.Infrastructure.SystemInterface.JG;
using EasyManufacture.Infrastructure.XingheAIMO;
using EasyManufacture.Licence;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net.Http;
using System.Text;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

/// <summary>旧 Global.asax 各 PushType 定时任务业务（自 Global.asax.cs 迁入）。</summary>
public sealed class GlobalLegacyPushTypeJob
{
    private readonly ILogger<GlobalLegacyPushTypeJob> _logger;
    private readonly SystemLog _systemLog = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OverlappingExecutionGuard _guard = new();

    private bool _sendPlanChange;
    private bool _isSendEmail;
    private EastSap? _eastSap;

    public GlobalLegacyPushTypeJob(ILogger<GlobalLegacyPushTypeJob> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task RunAsync(string taskName, CancellationToken cancellationToken)
    {
        if (!_guard.TryEnter()) return;
        try
        {
            await Task.Run(() =>
            {
                switch (taskName)
                {
                    case "PushType-2":
                        break;
                    case "Timer_Elapsed2":
                        RunTimerElapsed2();
                        break;
                    case "Timer_Elapsed3-工艺卡":
                        RunTimerElapsed3();
                        break;
                    case "Timer_Elapsed5":
                        RunTimerElapsed5();
                        break;
                    case "Timer_SendPlanChange":
                        RunSendPlanChange();
                        break;
                    case "Timer_Elapsed6-物联网":
                        RunTimerElapsed6();
                        break;
                    case "Timer_Elapsed7-SAP(EAST)":
                        RunTimerElapsed7();
                        break;
                    case "Timer_Elapsed9-SAP订单":
                        RunTimerElapsed9();
                        break;
                    case "Timer_Elapsed8-JGMES":
                        JGMES.Start();
                        break;
                    case "Timer_ElapsedTPHK":
                        RunTimerElapsedTphk();
                        break;
                    case "Timer_Elapsed12-模具":
                        RunMouldSync();
                        break;
                    case "OUSAI-WMS":
                        RunOusaiWms();
                        break;
                    default:
                        _logger.LogDebug("Global 任务 {Task} 无额外逻辑", taskName);
                        break;
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _guard.Exit();
        }
    }

    private void RunTimerElapsed2()
    {
        try
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口开始：", null, null);
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口错误：" + ex.Message, null, null);
        }
    }

    private static void RunTimerElapsed3() { }

    private void RunTimerElapsed6()
    {
        try
        {
            AliCloudIoT.Main();
            _systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "阿里访问成功", null, null);
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "阿里访问失败" + ex.Message, null, null);
        }
    }

    private void RunTimerElapsed7()
    {
        _eastSap ??= new EastSap();
        Task.Run(() => _eastSap.Start());
        Task.Run(() => _eastSap.GetERP_ZPPT036());
        Task.Run(() => _eastSap.GetMD04());
    }

    private void RunTimerElapsed9()
    {
        _eastSap ??= new EastSap();
        Task.Run(() => _eastSap.GetOrder());
    }

    private void RunTimerElapsed5()
    {
        if ((DateTime.Now.Hour is not (8 or 16)) || DateTime.Now.Minute != 1) return;
        if (_isSendEmail) return;
        _isSendEmail = true;
        try
        {
            SendUtDailyPlanEmail();
            SendUtCapacityMissingEmail();
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.邮件发送日志, "邮件发送失败:" + ex.Message, null, null);
        }
        if (DateTime.Now.Minute > 1) _isSendEmail = false;
    }

    private void RunTimerElapsedTphk()
    {
        if (DateTime.Now.Hour != 8 || DateTime.Now.Minute != 30) return;
        if (_isSendEmail) return;
        _isSendEmail = true;
        try
        {
            var ds = SqlHelper.ExecuteDataset(@"
SELECT A.[Account], B.Email, B.Name
FROM [dbo].[Dev_SendEmail] A
INNER JOIN Dev_Account B ON A.Account=B.Account
WHERE A.Status=1 AND A.Remark1='人力资源'");
            if (ds.Tables[0].Rows.Count == 0) return;
            var lstAddress = ds.Tables[0].Rows.Cast<DataRow>().Select(r => r["Email"].ToString()!).ToList();
            var body = @"<html><body>天宝精密的人力资源信息，请点击<a href=""http://10.172.2.11/#/Peo_ProductionSchedulingManagement2"" target=""_blank"">人力资源需求</a>打开系统查看。</body></html>";
            Email.SendMail(lstAddress, "精密成品制造人力需求", body);
            _systemLog.SaveLog(SystemLog.SystemLogType.邮件发送日志, "邮件提醒邮件发送成功", null, null);
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.邮件发送日志, "邮件发送失败:" + ex.Message, null, null);
        }
        if (DateTime.Now.Minute > 1) _isSendEmail = false;
    }

    private void RunSendPlanChange()
    {
        if (_sendPlanChange) return;
        _sendPlanChange = true;
        try
        {
            var ds = SqlHelper.ExecuteDataset(@"
SELECT A.OrderNo,A.Code,A.MaterialName,A.Spec,A.ChangeReason,A.ChangeType,B.StatusName,C.DocNo,
A.CreatedOn,D3.Email,A.EmailSent,A.CreatedByName as Name,A.OrderPlanChangeID,c.ProcessPartName,
a.ModifiedByName,a.ModifyedOn,A.Reply,a.StartDate,a.EndDate,A.CreatedBy
FROM [dbo].[APS_OrderPlanChange] A
INNER JOIN Dev_Status B ON A.Status = B.StatusID
inner join APS_ProcessPartPlan C ON A.ProcessPartID=C.ProcessPartID
LEFT JOIN Dev_Account D3 on D3.Account = A.CreatedBy
where EmailSent = 0 and A.status = 3 AND D3.EMAIL<>'' order by D3.Account");
            if (ds.Tables[0].Rows.Count == 0) return;

            var listName = ds.Tables[0].Rows.Cast<DataRow>()
                .Select(r => r["CreatedBy"].ToString()!)
                .Distinct()
                .ToList();

            foreach (var createdBy in listName)
            {
                var rows = ds.Tables[0].Select("CreatedBy='" + createdBy + "'");
                if (rows.Length == 0) continue;
                var name = rows[0]["Name"].ToString()!;
                var body = ResetBodyContentName(name);
                var email = "";
                var sqlUpdate = "";
                foreach (var dataRow in rows)
                {
                    email = dataRow["Email"].ToString()!;
                    sqlUpdate += "Update APS_OrderPlanChange set EmailSent = 1 where EmailSent = 0 and Status = 3 and OrderPlanChangeID=" + dataRow["OrderPlanChangeID"];
                    body += AddToBodyContent(dataRow);
                }
                body += "</table></body></html>";
                try
                {
                    Email.SendMail([email], "APS变更单审核不通过", body);
                    SqlHelper.ExecuteNonQuery(sqlUpdate);
                }
                catch (Exception ex)
                {
                    _systemLog.SaveLog(SystemLog.SystemLogType.邮件发送日志, ex.Message, null, null);
                }
            }
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.邮件发送日志, "变更邮件发送失败:" + ex.Message, null, null);
        }
        finally
        {
            _sendPlanChange = false;
        }
    }

    private void SendUtDailyPlanEmail()
    {
        var ds = SqlHelper.ExecuteDataset(@"
SELECT A.[Account],B.Email,B.Name FROM [dbo].[Dev_SendEmail] A
INNER JOIN Dev_Account B ON A.Account=B.Account WHERE A.Status=1 AND A.Remark1='日计划提醒';
SELECT WorkOrderTypeName,OrderNo,MaterialName,Spec,PlanQty,HasQty2,ProcessPartName,DocNo,EndDate
FROM V_OrderPlan3
where EndDate>='2023-01-01' AND WorkOrderTypeID<>'1001108140100002'
AND EndDate<=CAST(GETDATE()+2 AS DATE) and ProductionStatusName<>'已完成' AND CompletionDate IS NULL
order by ProcessPartName, enddate");
        if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0) return;
        var lstAddress = ds.Tables[0].Rows.Cast<DataRow>().Select(r => r["Email"].ToString()!).ToList();
        var names = string.Join("，", ds.Tables[0].Rows.Cast<DataRow>().Select(r => r["Name"].ToString()));
        var body = BuildUtPlanTableHtml(names, ds.Tables[1]);
        Email.SendMail(lstAddress, "APS工单任务提醒", body);
    }

    private void SendUtCapacityMissingEmail()
    {
        var ds = SqlHelper.ExecuteDataset(@"
SELECT A.[Account],B.Email,B.Name FROM [dbo].[Dev_SendEmail] A
INNER JOIN Dev_Account B ON A.Account=B.Account WHERE A.Status=1 AND A.Remark1='产能提醒';
SELECT D.Code,D.MaterialName,F.ProcessGroupName,
CASE WHEN F.ProcessGroupID IS NULL THEN '缺工艺' WHEN D3.ErrorCount > 0 THEN '缺产能' END AS ErrorType,
Stuff((SELECT ',' + CONVERT(VARCHAR, t.ProcessName) FROM (
SELECT X1.MaterialID,X2.ProcessID,X2.ProcessName FROM APS_ProcessMaterial X1
INNER JOIN APS_Process X2 ON X1.ProcessID=X2.ProcessID
INNER JOIN APS_ProcessGroupMaterial X4 ON X4.MaterialID=X1.MaterialID
INNER JOIN APS_ProcessGroupInfo X3 ON X3.ProcessID=X2.ProcessID AND X3.ProcessGroupID=X4.ProcessGroupID
WHERE Isnull(Capacity, 0) = 0 group by X1.MaterialID,X2.ProcessID,X2.ProcessName) t
WHERE T.MaterialID=A.MaterialID FOR xml path('')), 1, 1, '') AS ErrorCount,A.OrderNo,A.Spec
FROM APS_Material D
INNER JOIN V_OrderPlan1 A ON D.MaterialID=A.MaterialID AND A.ProductionStatus='26'
LEFT JOIN(SELECT X1.MaterialID,Count(1) AS ErrorCount FROM APS_ProcessMaterial X1
INNER JOIN APS_Process X2 ON X1.ProcessID=X2.ProcessID
INNER JOIN APS_ProcessGroupMaterial X4 ON X4.MaterialID=X1.MaterialID
INNER JOIN APS_ProcessGroupInfo X3 ON X3.ProcessID=X2.ProcessID AND X3.ProcessGroupID=X4.ProcessGroupID
WHERE Isnull(Capacity, 0) = 0 GROUP BY X1.MaterialID) D3 ON D.MaterialID = D3.MaterialID
LEFT JOIN APS_ProcessGroupMaterial E ON d.MaterialID = E.MaterialID
LEFT JOIN APS_ProcessGroup F ON E.ProcessGroupID = F.ProcessGroupID
WHERE e.ProcessGroupID IS NULL OR D3.ErrorCount > 0");
        if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0) return;
        var lstAddress = ds.Tables[0].Rows.Cast<DataRow>().Select(r => r["Email"].ToString()!).ToList();
        var names = string.Join("，", ds.Tables[0].Rows.Cast<DataRow>().Select(r => r["Name"].ToString()));
        var sb = new StringBuilder();
        sb.Append("<html><body>亲爱的").Append(names).Append("，以下MO待生产但工艺产能缺失：<table border='1'>");
        sb.Append("<tr><td>MO</td><td>编码</td><td>名称</td><td>规格</td><td>工艺</td><td>缺失工序</td><td>类型</td></tr>");
        foreach (DataRow row in ds.Tables[1].Rows)
        {
            sb.AppendFormat("<tr><td>{5}</td><td>{0}</td><td>{1}</td><td>{6}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                row["Code"], row["MaterialName"], row["ProcessGroupName"], row["ErrorCount"], row["ErrorType"], row["OrderNo"], row["Spec"]);
        }
        sb.Append("</table></body></html>");
        Email.SendMail(lstAddress, "APS待生产订单工艺产能缺失提醒", sb.ToString());
    }

    private static string BuildUtPlanTableHtml(string names, DataTable table)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body>亲爱的").Append(names).Append("，您有").Append(table.Rows.Count).Append("笔日计划即将过期：<table border='1'>");
        sb.Append("<tr><td>单据类型</td><td>MO</td><td>产品</td><td>规格</td><td>计划数</td><td>完成数</td><td>工段</td><td>批次</td><td>结束日期</td></tr>");
        foreach (DataRow row in table.Rows)
        {
            sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{8}</td><td>{3:G0}</td><td>{4:G0}</td><td>{5}</td><td>{6}</td><td>{7:yyyy-MM-dd}</td></tr>",
                row["WorkOrderTypeName"], row["OrderNo"], row["MaterialName"], row["PlanQty"], row["HasQty2"],
                row["ProcessPartName"], row["DocNo"], row["EndDate"], row["Spec"]);
        }
        sb.Append("</table></body></html>");
        return sb.ToString();
    }

    private void RunOusaiWms()
    {
        OusaiWmsLoginAndThen(client => OusaiWmsGetStock(client));
    }

    private void OusaiWmsLoginAndThen(Action<HttpClient> afterLoginAction)
    {
        try
        {
            var loginUrl = AppInfo.WMSUrl + "/api/login?username=APSUSER&password=123456";
            using var client = _httpClientFactory.CreateClient();
            var response = client.PostAsync(loginUrl, null).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OUSAI WMS 登录失败: {Status}", response.StatusCode);
                return;
            }
            afterLoginAction(client);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OUSAI WMS 登录异常");
        }
    }

    private void OusaiWmsGetStock(HttpClient client)
    {
        var url = AppInfo.WMSUrl + "/api/wms-api/wms-boot/wms/wmsOnhandsItems/list?limit=100000&start=1";
        var response = client.PostAsync(url, null).GetAwaiter().GetResult();
        var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) return;

        var json = JObject.Parse(responseString);
        if (json["success"]?.ToObject<bool>() != true) return;

        var sql = new StringBuilder("TRUNCATE TABLE WMS_StockTemp;");
        var results = JsonConvert.DeserializeObject<JArray>(json["result"]!.ToString())!;
        foreach (JObject obj in results)
        {
            var qty = obj["qty"]?.ToString() ?? "0";
            if (qty == "0") continue;
            var wareHouseName = obj["genWarehouse"]?["shortName"]?.ToString() ?? "";
            var wareHouseCode = obj["genWarehouse"]?["refCode"]?.ToString() ?? "";
            var code = obj["genMaterial"]?["codeNo"]?.ToString() ?? "";
            var materialName = obj["genMaterial"]?["name"]?.ToString() ?? "";
            var rackCode = obj["rackNo"]?.ToString() ?? "";
            var unit = obj["uom"]?.ToString() ?? "";
            sql.Append($@"
INSERT INTO [APS].[dbo].WMS_StockTemp (WareHouseName, WareHouseCode, Code, MaterialName, Qty, RackCode, Unit, Status)
SELECT '{wareHouseName}', '{wareHouseCode}', '{code}', '{materialName}', {qty}, '{rackCode}', '{unit}', 1;");
        }
        sql.Append("EXEC [P_DownStock];");
        SqlHelper.ExecuteNonQuery(sql.ToString());
    }

    private void RunMouldSync()
    {
        try
        {
            const int intervalMonth = 6;
            var service = new XingheMouldClient();
            SqlHelper.ExecuteDataTable("truncate table IM_MouldTemp; truncate table IM_MouldDetailTemp;");
            var endDate = DateTime.Now;
            var requestStartDate = new DateTime(2024, 1, 1);
            while (requestStartDate <= endDate)
            {
                var requestEndDate = requestStartDate.AddMonths(intervalMonth - 1);
                var lastDay = DateTime.DaysInMonth(requestEndDate.Year, requestEndDate.Month);
                requestEndDate = new DateTime(requestEndDate.Year, requestEndDate.Month, lastDay);
                var json = $@"{{ ""create_time_time_range"": [""{requestStartDate:yyyy-MM-dd 00:00:00}"", ""{requestEndDate:yyyy-MM-dd 23:59:59}""] }}";
                var s = service.WApiGetMouldInfo(json);
                var jObject = JsonConvert.DeserializeObject(s) as JObject;
                var jArray = JsonConvert.DeserializeObject(jObject!["m_obj"]!.ToString()) as JArray;
                requestStartDate = requestStartDate.AddMonths(intervalMonth);
                if (jArray == null) continue;
                foreach (JObject obj in jArray)
                {
                    SqlHelper.ExecuteDataTable(string.Format(@"
if not exists (select 1 from IM_MouldTemp where MouldNO ='{5}')
begin
    insert into IM_MouldTemp (Owner,ZhimoUser,YanmoUser,MouldType,ProjectNum,MouldNO,Position)
    select '{0}','{1}','{2}','{3}','{4}','{5}','{6}';
end",
                        obj["m_strOwner"], obj["m_strZhimoUser"], obj["m_strYanmoUser"], obj["m_strMouldType"],
                        obj["m_strProjectNum"], obj["m_strMouldNum"], obj["strPosition"]));
                    var jArray1 = JsonConvert.DeserializeObject(obj["lstMouldVersion"]!.ToString()) as JArray;
                    if (jArray1 == null) continue;
                    foreach (JObject obj1 in jArray1)
                    {
                        SqlHelper.ExecuteDataTable(string.Format(@"
if not exists (select 1 from IM_MouldDetailTemp where MouldVersion ='{5}')
begin
    insert into IM_MouldDetailTemp (MouldNO,Creator,CreateTime,Interest,ScheduleEndTime,MouldVersion,IType,DeadlineTime,IsFinished,IProgress,Designer,Programmer,DTimeProgress,ChangeReason,ChangeDiscription,AbnormalOrderNumber,MaterialNumber,Owner,DelayDay,FinishedTime,IMStatus)
    select '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}';
end",
                            obj["m_strMouldNum"], obj1["m_strCreator"], obj1["m_strCreateTime"], obj1["m_strInterest"],
                            obj1["m_strScheduleEndTime"], obj1["m_strMouldVersion"], obj1["m_iType"], obj1["m_strDeadlineTime"],
                            obj1["m_iIsFinished"], obj1["m_iProgress"], obj1["strDesigner"], obj1["strProgrammer"], obj1["dTimeProgress"],
                            obj1["m_strChangeReason"], obj1["m_strChangeDiscription"], obj1["m_strAbnormalOrderNumber"],
                            obj1["m_strMaterialNumber"], obj1["strOwner"], obj1["dDelayDay"], obj1["m_strFinishedTime"], obj1["strStatus"]));
                    }
                }
                Thread.Sleep(1000);
            }
            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, "P_MouldAndMouldDetail");
        }
        catch (Exception ex)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.其他错误日志, ex.Message, null, null);
        }
    }

    private static string ResetBodyContentName(string name) => @"
<html><body>亲爱的" + name + @"，以下MO变更审核不通过：
<table border='1'><tr>
<td>MO</td><td>产品编码</td><td>产品名称</td><td>规格</td><td>变更原因</td><td>类型</td><td>状态</td>
<td>工段</td><td>工单号</td><td>提交日期</td><td>计划日期</td><td>变更日期</td><td>不通过原因</td><td>审批人</td><td>审批日期</td>
</tr>";

    private static string AddToBodyContent(DataRow dataRow) => string.Format(@"
<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td>
<td>{7}</td><td>{8}</td><td>{9}</td><td>{10:yyyy-MM-dd}</td><td>{11:yyyy-MM-dd}</td><td>{12}</td><td>{13}</td><td>{14}</td></tr>",
        dataRow["OrderNo"], dataRow["Code"], dataRow["MaterialName"], dataRow["Spec"], dataRow["ChangeReason"],
        dataRow["ChangeType"], dataRow["StatusName"], dataRow["ProcessPartName"], dataRow["DocNo"], dataRow["CreatedOn"],
        dataRow["StartDate"], dataRow["EndDate"], dataRow["Reply"], dataRow["ModifiedByName"], dataRow["ModifyedOn"]);
}
