using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 盈瑞丰 APSData dic 前置钩子：在 RunAPSData → APSData 前挂载 setDt / setRowDetail。
/// 当前仅 6 个 dic（28574/28580/28581/28589/28634/28636）；
/// 旧 Web APSAPIController.override APSData() 内 100+ case 尚未完整迁入。
/// SetDt 方法体位于 LegacyApi.cs。
/// </summary>
public partial class ApsCoreEngine
{
    private void ApplyApsDataDicHooks()
    {
        try
        {
            jObject = JsonConvert.DeserializeObject<JObject>(BodyJson);
            if (jObject?.ContainsKey("dicID") == true)
                dicID = int.Parse(jObject["dicID"]!.ToString());
        }
        catch { }

        switch (dicID)
        {
            case 28574:
                setRowDetail += setDetai28574;
                setDt = SetDt28574;
                break;
            case 28581:
                setDt = SetDt28581;
                break;
            case 28580:
                setDt = SetDt28580;
                break;
            case 28589:
                setRowDetail += setDetail28589;
                setDt = SetDt28589;
                break;
            case 28634:
                setDt = SetDt28634;
                break;
            case 28636:
                setDt = SetDt28636;
                break;
        }
    }
}
