using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Kingdee.CDP.WebApi.SDK;
using Newtonsoft.Json;
using System.Data;

namespace EasyManufacture.Infrastructure.SystemInterface.K3Cloud;

/// <summary>金蝶云星空 K3Cloud API（旧 EasyManufacture.Core.SystemInterface.K3Cloud.K3）。</summary>
public static class K3
{
    private static readonly K3CloudApi Client = new();
    private static readonly SystemLog SystemLog = new();

    public class KingdeeResponse
    {
        public Result? Result { get; set; }
    }

    public class Result
    {
        public ResponseStatus? ResponseStatus { get; set; }
        public string? Id { get; set; }
        public string? Number { get; set; }
        public List<object>? NeedReturnData { get; set; }
    }

    public class ResponseStatus
    {
        public int ErrorCode { get; set; }
        public bool IsSuccess { get; set; }
        public List<ErrorInfo>? Errors { get; set; }
        public List<object>? SuccessEntitys { get; set; }
        public List<object>? SuccessMessages { get; set; }
        public int MsgCode { get; set; }
    }

    public class ErrorInfo
    {
        public string? FieldName { get; set; }
        public string? Message { get; set; }
        public int DIndex { get; set; }
    }

    public class BillEntryItem
    {
        public string FSrcStockId { get; set; } = "";
        public string FDestStockId { get; set; } = "";
        public string FMaterialId { get; set; } = "";
        public double FQty { get; set; }
        public string? Unit { get; set; }
    }

    /// <summary>直接调拨单（旧 TransferDirect）。</summary>
    public static KingdeeResponse TransferDirect(List<BillEntryItem> items)
    {
        if (items.Count == 0)
            throw new Exception("没有可调拨的物料");

        var codes = string.Join(",", items.Select(i => "'" + i.FMaterialId.Replace("'", "''") + "'"));
        var dt = SqlHelper.ExecuteDataTable($"""
            SELECT DISTINCT FNUMBER FROM [V_ERP_T_STK_STKTRANSFERINENTRY]
            WHERE FNUMBER IN({codes})
            """);

        KingdeeResponse response;
        if (dt.Rows.Count == 0)
        {
            var payload = GenerateJsonString(items);
            EnsureClientInitialized();
            var jsonString = Client.Save("STK_TransferDirect", payload);
            response = JsonConvert.DeserializeObject<KingdeeResponse>(jsonString)
                       ?? new KingdeeResponse();
        }
        else
        {
            response = new KingdeeResponse
            {
                Result = new Result
                {
                    ResponseStatus = new ResponseStatus
                    {
                        IsSuccess = false,
                        ErrorCode = 500,
                        Errors = new List<ErrorInfo>()
                    }
                }
            };

            foreach (DataRow dr in dt.Rows)
            {
                response.Result!.ResponseStatus!.Errors!.Add(new ErrorInfo
                {
                    FieldName = dr["FNUMBER"]?.ToString(),
                    Message = $"物料{dr["FNUMBER"]}存在未审核或未关闭的调拨单，不能重复生成调拨单"
                });
            }
        }

        return response;
    }

    /// <summary>工单自动开工（旧 ToStart），orderNos 格式：'MO001','MO002'。</summary>
    public static KingdeeResponse ToStart(string orderNos)
    {
        if (string.IsNullOrEmpty(orderNos))
        {
            SystemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "EK开工工单失败：没有可开工的工单", null, null);
            return FailedResponse("没有可开工的工单");
        }

        EnsureClientInitialized();
        var jsonString = Client.ExcuteOperation(
            "PRD_MO",
            "ToStart",
            "{\"CreateOrgId\":0,\"Numbers\":[" + orderNos +
            "],\"Ids\":\"\",\"PkEntryIds\":[],\"UseOrgId\":0,\"NetworkCtrl\":\"\",\"IgnoreInterationFlag\":\"\"}");

        var response = JsonConvert.DeserializeObject<KingdeeResponse>(jsonString) ?? new KingdeeResponse();
        var status = response.Result?.ResponseStatus;
        if (status == null)
            return response;

        if (!status.IsSuccess)
        {
            var errJson = JsonConvert.SerializeObject(status.Errors);
            SystemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "EK开工工单失败：" + errJson, null, null);
            SqlHelper.ExecuteNonQuery($"""
                UPDATE APS_Order
                SET Extend10='{StringHelper.ReplaceSQL(errJson)}', Extend11='失败'
                WHERE ORDERNO IN({orderNos})
                """);
        }
        else
        {
            SqlHelper.ExecuteNonQuery($"""
                UPDATE APS_Order
                SET Extend4='开工', Extend10='', Extend11='成功'
                WHERE ORDERNO IN({orderNos})
                """);
            SystemLog.SaveLog(
                SystemLog.SystemLogType.接口推送,
                "EK开工工单成功：" + JsonConvert.SerializeObject(status.SuccessMessages),
                null,
                null);
        }

        return response;
    }

    private static KingdeeResponse FailedResponse(string message) =>
        new()
        {
            Result = new Result
            {
                ResponseStatus = new ResponseStatus
                {
                    IsSuccess = false,
                    ErrorCode = 500,
                    Errors = [new ErrorInfo { Message = message }]
                }
            }
        };

    private static void EnsureClientInitialized()
    {
        var cfg = LicenceRuntime.Configuration;
        var acctId = cfg["App:X-KDApi-AcctID"] ?? cfg["X-KDApi-AcctID"] ?? "";
        var appId = cfg["App:X-KDApi-AppID"] ?? cfg["X-KDApi-AppID"] ?? "";
        var appSec = cfg["App:X-KDApi-AppSec"] ?? cfg["X-KDApi-AppSec"] ?? "";
        var userName = V_Dev_Account.GetDev_Account()?.Account
                       ?? cfg["App:X-KDApi-UserName"]
                       ?? cfg["X-KDApi-UserName"]
                       ?? "administrator";

        Client.InitClient(acctId, appId, appSec, userName, 2052);
    }

    private static string GenerateJsonString(List<BillEntryItem> items)
    {
        const string jsonPrefix = """
            {
                "NeedUpDateFields": [],
                "NeedReturnFields": [],
                "IsDeleteEntry": "true",
                "SubSystemId": "",
                "IsVerifyBaseDataField": "false",
                "IsEntryBatchFill": "true",
                "ValidateFlag": "true",
                "NumberSearch": "true",
                "IsAutoAdjustField": "true",
                "InterationFlags": "",
                "IgnoreInterationFlag": "",
                "IsControlPrecision": "false",
                "ValidateRepeatJson": "false",
                "Model": {
                    "FID": 0,
                    "FBillTypeID": { "FNUMBER": "ZJDB01_SYS" },
                    "FBizType": "NORMAL",
                    "FTransferDirect": "GENERAL",
                    "FTransferBizType": "InnerOrgTransfer",
                    "FSaleOrgId": { "FNumber": "100" },
                    "FSettleOrgId": { "FNumber": "100" },
                    "FStockOutOrgId": { "FNumber": "100" },
                    "FOwnerTypeOutIdHead": "BD_OwnerOrg",
                    "FOwnerOutIdHead": { "FNumber": "100" },
                    "FStockOrgId": { "FNumber": "100" },
                    "FOwnerTypeIdHead": "BD_OwnerOrg",
                    "FSETTLECURRID": { "FNUMBER": "PRE001" },
                    "FExchangeRate": 1.0,
                    "FExchangeTypeId": { "FNUMBER": "HLTX01_SYS" },
                    "FIsIncludedTax": true,
                    "FIsPriceExcludeTax": true,
                    "FOwnerIdHead": { "FNumber": "100" },
                    "F_BOS_PrintTimes": 0,
                    "FWriteOffConsign": false,
                    "F_OASP": false,
                    "FISBLD": false,
                    "FBillEntry": [
            """;

        var entryItems = items.Select(item => $$"""
            {
                "FRowType": "Standard",
                "FMaterialId": { "FNumber": "{{item.FMaterialId}}" },
                "FQty": {{item.FQty}},
                "FSrcStockId": { "FNumber": "{{item.FSrcStockId}}" },
                "FDestStockId": { "FNumber": "{{item.FDestStockId}}" },
                "FSrcStockStatusId": { "FNumber": "KCZT01_SYS" },
                "FDestStockStatusId": { "FNumber": "KCZT01_SYS" },
                "FSrcBillTypeId": "",
                "FOwnerTypeOutId": "BD_OwnerOrg",
                "FOwnerOutId": { "FNumber": "100" },
                "FOwnerTypeId": "BD_OwnerOrg",
                "FOwnerId": { "FNumber": "100" },
                "FSrcBillNo": "",
                "FSecQty": 0.0,
                "FExtAuxUnitQty": 0.0,
                "FBaseQty": {{item.FQty}},
                "FISFREE": false,
                "FKeeperTypeId": "BD_KeeperOrg",
                "FActQty": 0.0,
                "FKeeperId": { "FNumber": "100" },
                "FKeeperTypeOutId": "BD_KeeperOrg",
                "FKeeperOutId": { "FNumber": "100" },
                "FDiscountRate": 0.0,
                "FRepairQty": 0.0,
                "FDestMaterialId": { "FNUMBER": "{{item.FMaterialId}}" },
                "FSaleQty": {{item.FQty}},
                "FSalBaseQty": {{item.FQty}},
                "FPriceQty": {{item.FQty}},
                "FPriceBaseQty": {{item.FQty}},
                "FOutJoinQty": 0.0,
                "FBASEOUTJOINQTY": 0.0,
                "FSOEntryId": 0,
                "FTransReserveLink": false,
                "FQmEntryId": 0,
                "FConvertEntryId": 0,
                "FCheckDelivery": false,
                "FBomEntryId": 0
            }
            """);

        return jsonPrefix + string.Join(",", entryItems) + "\n        ]\n    }\n}";
    }
}
