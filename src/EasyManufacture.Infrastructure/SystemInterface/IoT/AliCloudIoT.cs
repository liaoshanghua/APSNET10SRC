using EasyManufacture.Infrastructure.Legacy;

namespace EasyManufacture.Infrastructure.SystemInterface.IoT;

/// <summary>阿里物联网（自 EasyManufacture.Core/SystemInterface/AliClound.cs 迁入）。</summary>
public static class AliCloudIoT
{
    public static AlibabaCloud.SDK.Iot20180120.Client CreateClient(string accessKeyId, string accessKeySecret)
    {
        var config = new AlibabaCloud.OpenApiClient.Models.Config
        {
            AccessKeyId = accessKeyId,
            AccessKeySecret = accessKeySecret,
            Endpoint = "iot.cn-shanghai.aliyuncs.com"
        };
        return new AlibabaCloud.SDK.Iot20180120.Client(config);
    }

    public static void Main()
    {
        var accessKeyId = Environment.GetEnvironmentVariable("ALICLOUD_IOT_ACCESS_KEY_ID");
        var accessKeySecret = Environment.GetEnvironmentVariable("ALICLOUD_IOT_ACCESS_KEY_SECRET");
        var productKey = Environment.GetEnvironmentVariable("ALICLOUD_IOT_PRODUCT_KEY");
        var deviceName = Environment.GetEnvironmentVariable("ALICLOUD_IOT_DEVICE_NAME");
        if (string.IsNullOrWhiteSpace(accessKeyId) || string.IsNullOrWhiteSpace(accessKeySecret))
            throw new InvalidOperationException("请配置环境变量 ALICLOUD_IOT_ACCESS_KEY_ID / ALICLOUD_IOT_ACCESS_KEY_SECRET");
        if (string.IsNullOrWhiteSpace(productKey) || string.IsNullOrWhiteSpace(deviceName))
            throw new InvalidOperationException("请配置环境变量 ALICLOUD_IOT_PRODUCT_KEY / ALICLOUD_IOT_DEVICE_NAME");

        var client = CreateClient(accessKeyId, accessKeySecret);
        var request = new AlibabaCloud.SDK.Iot20180120.Models.QueryDevicePropertyStatusRequest
        {
            ProductKey = productKey,
            DeviceName = deviceName,
        };
        var result = client.QueryDevicePropertyStatus(request);
        SqlHelper.ExecuteNonQuery(
            SqlHelper.MSSQLConnectionString,
            System.Data.CommandType.Text,
            string.Format(
                "update aps_order set qty={0} where orderno='{1}'",
                result.Body.Data.List.PropertyStatusInfo[7].Value,
                result.Body.Data.List.PropertyStatusInfo[2].Value));
    }
}
