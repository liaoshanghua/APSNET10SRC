using EasyManufacture.Infrastructure.SystemInterface.K3Cloud;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>金蝶 K3Cloud 接口（旧 APSAPIController.K3ColudTransferDirect / orderToStart）。</summary>
public partial class ApsCoreEngine
{
    /// <summary>EK 马来欠料页 — 生成调拨单。</summary>
    public string K3ColudTransferDirect()
    {
        var result = true;
        var msg = "数据处理完成";
        List<K3.BillEntryItem> items = [];
        K3.KingdeeResponse? response = null;

        try
        {
            jArray = JsonConvert.DeserializeObject<JArray>(BodyJson);
            if (jArray == null || jArray.Count == 0)
            {
                msg = "未接受到数据，请确认是否为JSON格式";
                result = false;
            }
            else
            {
                var itemDict = new Dictionary<string, K3.BillEntryItem>();
                foreach (JObject jObject in jArray)
                {
                    var warehouseCode = jObject["WarehouseCode"]?.ToString()?.Trim();
                    var materialId = jObject["Code"]?.ToString()?.Trim();
                    var oweQtyStr = jObject["MalaysiaOweQty2"]?.ToString()?.Trim();
                    var codeKey = warehouseCode + "_" + materialId;

                    if (string.IsNullOrEmpty(warehouseCode) ||
                        string.IsNullOrEmpty(materialId) ||
                        string.IsNullOrEmpty(oweQtyStr) ||
                        !double.TryParse(oweQtyStr, out var oweQty) ||
                        oweQty == 0.0)
                        continue;

                    if (itemDict.TryGetValue(codeKey, out var existingItem))
                        existingItem.FQty += oweQty;
                    else
                        itemDict[codeKey] = new K3.BillEntryItem
                        {
                            FSrcStockId = warehouseCode,
                            FDestStockId = "M999",
                            FMaterialId = materialId,
                            FQty = oweQty
                        };
                }

                items = itemDict.Values.ToList();
                foreach (var group in items.GroupBy(x => x.FSrcStockId))
                {
                    response = K3.TransferDirect(group.ToList());
                    var responseStatus = response.Result?.ResponseStatus;
                    if (responseStatus == null)
                        continue;

                    if (responseStatus.IsSuccess)
                    {
                        responseStatus.SuccessEntitys?.ForEach(_ =>
                            msg += "  仓库:" + group.Key + " 物料:" +
                                   string.Join(",", group.Select(x => x.FMaterialId)) + " 操作成功。");
                    }
                    else
                    {
                        result = false;
                        responseStatus.Errors?.ForEach(m =>
                            msg += "  仓库:" + group.Key + " 物料:" +
                                   string.Join(",", group.Select(x => x.FMaterialId)) +
                                   " 操作失败:" + m.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result = false;
            msg = "数据操作失败,错误信息:" + ex.Message;
        }

        return JsonConvert.SerializeObject(new { result, msg, response, items });
    }

    /// <summary>EK 备料计划 — 订单下达（工单开工）。</summary>
    public string OrderToStart()
    {
        var result = true;
        var msg = "数据处理成功";
        var orderNos = "";
        K3.KingdeeResponse? response = null;

        try
        {
            jArray = JsonConvert.DeserializeObject<JArray>(BodyJson);
            if (jArray == null || jArray.Count == 0)
            {
                msg = "未接受到数据，请确认是否为JSON格式";
                result = false;
            }
            else
            {
                orderNos = string.Join("','", jArray.Select(j => j["OrderNo"]?.ToString()));
                if (orderNos.Length > 0)
                    orderNos = "'" + orderNos + "'";

                response = K3.ToStart(orderNos);
                var responseStatus = response.Result?.ResponseStatus;
                if (responseStatus?.IsSuccess == true)
                    msg = "操作成功";
                else
                {
                    result = false;
                    msg = "操作失败，错误信息：";
                    responseStatus?.Errors?.ForEach(m => msg += m.Message + ";");
                }
            }
        }
        catch (Exception ex)
        {
            result = false;
            msg = "数据操作失败,错误信息:" + ex.Message;
        }

        return JsonConvert.SerializeObject(new { result, msg, response, orderNos });
    }
}
