using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 钉钉群机器人推送：按 PushTime 推送，用 LastPushHour 防同槽重复。
/// PushTime 支持半小时槽（10 / 10.5）与时刻点（10.33=10:33）。
/// 不使用 PushTimes / RemainPushTimes。
/// 配置：WebhookType='钉钉机器人'；Info+Keys；可选 Secret 加签。
/// 无数据时不写 LastPushHour（避免到点空表导致本时段再也推不了）。
/// </summary>
public sealed class DingTalkWebhookPushJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SystemLog _systemLog = new();

    public DingTalkWebhookPushJob(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        // 半小时槽：10 / 10.5；时刻点：10.33 = 10:33（分/100）
        double halfHourSlot = Math.Floor((now.Hour + now.Minute / 60.0) * 2) / 2.0;
        double clockHm = now.Hour + now.Minute / 100.0;
        string msg = "钉钉机器人消息推送开始";
        try
        {
            DataTable dtWebhooks = SqlHelper.ExecuteDataTable(
                @"SELECT * FROM V_Dev_Webhook WHERE Status = 1 AND WebhookType = N'钉钉机器人' Order by PushSort");
            if (dtWebhooks.Rows.Count == 0)
                return;

            foreach (DataRow configRow in dtWebhooks.Rows)
            {
                string info = configRow["Info"]?.ToString();
                string keys = configRow["Keys"]?.ToString();
                string pushTime = configRow["PushTime"]?.ToString();
                string pushTable = configRow["PushTable"]?.ToString();
                int maxPushCount = configRow["MaxPushCount"] != DBNull.Value ? Convert.ToInt32(configRow["MaxPushCount"]) : 1000;
                int pushInterval = configRow["PushInterval"] != DBNull.Value ? Convert.ToInt32(configRow["PushInterval"]) * 1000 : 1000;
                string pushType = configRow["PushType"]?.ToString();
                if (string.IsNullOrEmpty(pushType))
                    pushType = "卡片";
                int id = Convert.ToInt32(configRow["ID"]);
                double lastPushSlot = configRow["LastPushHour"] != DBNull.Value ? Convert.ToDouble(configRow["LastPushHour"]) : -1;
                string secret = configRow.Table.Columns.Contains("Secret")
                    ? configRow["Secret"]?.ToString()
                    : null;

                if (string.IsNullOrEmpty(info) || string.IsNullOrEmpty(keys) || string.IsNullOrEmpty(pushTime)
                    || string.IsNullOrEmpty(pushTable))
                {
                    continue;
                }

                var pushTimeList = pushTime.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    .Select(x => double.Parse(x, CultureInfo.InvariantCulture))
                    .ToList();
                // 兼容中文环境下 PushTime 用逗号小数
                if (pushTimeList.Count == 0)
                {
                    pushTimeList = pushTime.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => double.TryParse(x, out _))
                        .Select(double.Parse)
                        .ToList();
                }

                double? matchedSlot = null;
                foreach (double target in pushTimeList)
                {
                    if (IsHalfHourPushTime(target))
                    {
                        if (halfHourSlot >= target && halfHourSlot < target + 0.1)
                        {
                            matchedSlot = halfHourSlot;
                            break;
                        }
                    }
                    else
                    {
                        // 10.33 → 仅在 10:33 这一分钟命中
                        if (clockHm >= target && clockHm < target + 0.01)
                        {
                            matchedSlot = target;
                            break;
                        }
                    }
                }

                if (matchedSlot is null)
                    continue;

                double currentSlot = matchedSlot.Value;
                if (Math.Abs(lastPushSlot - currentSlot) < 0.01)
                    continue;

                // 先查数据：无数据不写 LastPushHour
                DataTable dtData;
                try
                {
                    dtData = SqlHelper.ExecuteDataTable($"SELECT * FROM {pushTable}");
                }
                catch (Exception ex)
                {
                    _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败,
                        $"钉钉读取推送表失败 PushTable={pushTable}：{ex.Message}", null, null);
                    continue;
                }

                if (dtData.Rows.Count == 0)
                    continue;

                var keyList = keys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
                if (keyList.Count == 0)
                    continue;

                string slotLiteral = currentSlot.ToString(CultureInfo.InvariantCulture);
                int affectRows = SqlHelper.ExecuteNonQuery($@"
                            UPDATE Dev_Webhook
                            SET LastPushHour = {slotLiteral},
                                ModifyedOn = Getdate()
                            WHERE ID = {id}
                              AND (LastPushHour IS NULL OR ABS(LastPushHour - {slotLiteral}) >= 0.01)
                        ");
                if (affectRows == 0)
                    continue;

                string pushTitle = dtData.Columns.Contains("PushTitle")
                    ? dtData.Rows[0]["PushTitle"]?.ToString() ?? "APS推送"
                    : "APS推送";
                string? pushHeader = null;
                if (dtData.Columns.Contains("PushHeader")
                    && !string.IsNullOrWhiteSpace(dtData.Rows[0]["PushHeader"]?.ToString()))
                {
                    pushHeader = dtData.Rows[0]["PushHeader"]?.ToString()
                        ?.Replace("[PushTime]", DateTime.Now.ToString("yyyy/M/d HH:mm"));
                }
                else if (configRow.Table.Columns.Contains("Remark2")
                         && !string.IsNullOrWhiteSpace(configRow["Remark2"]?.ToString()))
                {
                    pushHeader = configRow["Remark2"]?.ToString()
                        ?.Replace("[PushTime]", DateTime.Now.ToString("yyyy/M/d HH:mm"));
                }

                int totalCount = dtData.Rows.Count;
                int sendTimes = (int)Math.Ceiling((double)totalCount / maxPushCount);
                var successCounts = keyList.ToDictionary(k => k, _ => 0);

                for (int i = 0; i < sendTimes; i++)
                {
                    int startIndex = i * maxPushCount;
                    int endIndex = Math.Min((i + 1) * maxPushCount, totalCount);
                    string markdownText = BuildMarkdown(dtData, pushTitle, pushHeader, pushType, startIndex, endIndex, sendTimes, i);

                    var contentJson = new
                    {
                        msgtype = "markdown",
                        markdown = new
                        {
                            title = string.IsNullOrWhiteSpace(pushTitle) ? "APS推送" : pushTitle,
                            text = markdownText
                        }
                    };

                    try
                    {
                        var json = JsonConvert.SerializeObject(contentJson);
                        foreach (var key in keyList)
                        {
                            var fullUrl = AppendDingTalkSign($"{info}{key}", secret);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            var client = _httpClientFactory.CreateClient(nameof(DingTalkWebhookPushJob));
                            var response = await client.PostAsync(fullUrl, content, cancellationToken).ConfigureAwait(false);
                            string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                            if (IsDingTalkSuccess(response.IsSuccessStatusCode, result))
                            {
                                successCounts[key]++;
                                _systemLog.SaveLog(SystemLog.SystemLogType.接口推送,
                                    $"钉钉推送(key:{key})成功(第{i + 1}批)，共{endIndex - startIndex}条", null, null);
                            }
                            else
                            {
                                _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败,
                                    $"钉钉推送(key:{key})失败(第{i + 1}批)，错误: {result}", null, null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败,
                            $"钉钉请求异常(第{i + 1}批)：{ex.Message}", null, null);
                    }

                    if (i < sendTimes - 1 && pushInterval > 0)
                        await Task.Delay(pushInterval, cancellationToken).ConfigureAwait(false);
                }

                string successSummary = string.Join("，", successCounts.Select(kv => $"key:{kv.Key}成功{kv.Value}批"));
                _systemLog.SaveLog(SystemLog.SystemLogType.接口推送,
                    $"钉钉消息推送完成，共{sendTimes}批，{successSummary}", null, null);
            }
        }
        catch (Exception ex)
        {
            msg = $"钉钉获取消息内容失败：{ex.Message}";
            _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
        }
    }

    private static string BuildMarkdown(
        DataTable dtData,
        string pushTitle,
        string? pushHeader,
        string pushType,
        int startIndex,
        int endIndex,
        int sendTimes,
        int batchIndex)
    {
        var markdownContent = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(pushHeader))
        {
            markdownContent.AppendLine(pushHeader.Trim());
            markdownContent.AppendLine();
        }
        else
        {
            string titleLine = sendTimes == 1
                ? $"## {pushTitle}\n"
                : $"## {pushTitle}{batchIndex + 1}\n";
            markdownContent.AppendLine(titleLine);
        }

        for (int j = startIndex; j < endIndex; j++)
        {
            foreach (DataColumn col in dtData.Columns)
            {
                if (col.ColumnName.Equals("PushTitle", StringComparison.OrdinalIgnoreCase)
                    || col.ColumnName.Equals("PushHeader", StringComparison.OrdinalIgnoreCase))
                    continue;
                string value = dtData.Rows[j][col]?.ToString()?.Replace("\n", " ") ?? "";
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                markdownContent.AppendLine($"**{col.ColumnName}：** {value}  ");
            }

            markdownContent.AppendLine();
            markdownContent.AppendLine("---");
            markdownContent.AppendLine();
        }

        _ = pushType;
        return markdownContent.ToString();
    }

    /// <summary>
    /// 半小时槽：整数点或 .5（如 10、10.5）；其余按 H.mm 时刻（如 10.33=10:33）。
    /// </summary>
    private static bool IsHalfHourPushTime(double target)
    {
        double frac = Math.Abs(target - Math.Truncate(target));
        return frac < 0.001 || Math.Abs(frac - 0.5) < 0.001;
    }

    private static bool IsDingTalkSuccess(bool httpOk, string body)
    {
        if (!httpOk || string.IsNullOrWhiteSpace(body))
            return false;
        try
        {
            var jo = JObject.Parse(body);
            return (jo["errcode"]?.Value<int>() ?? -1) == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string AppendDingTalkSign(string url, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return url;

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string stringToSign = timestamp + "\n" + secret;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        string sign = WebUtility.UrlEncode(Convert.ToBase64String(hash));
        string sep = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{sep}timestamp={timestamp}&sign={sign}";
    }
}
