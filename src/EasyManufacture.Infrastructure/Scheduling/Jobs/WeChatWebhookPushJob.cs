using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http;
using System.Text;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

public sealed class WeChatWebhookPushJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SystemLog _systemLog = new();

    public WeChatWebhookPushJob(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            double currentSlot = Math.Floor((DateTime.Now.Hour + DateTime.Now.Minute / 60.0) * 2) / 2.0;
            string msg = "企业微信机器人消息推送开始";
            try
            {
                //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送, msg, null, null);
                // 1. 获取所有机器人配置
                DataTable dtWebhooks = SqlHelper.ExecuteDataTable(@"SELECT * FROM V_Dev_Webhook WHERE Status = 1 AND WebhookType = '企业微信机器人' Order by PushSort");
                if (dtWebhooks.Rows.Count == 0)
                {
                    msg = "未找到有效的企业微信机器人配置";
                    //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                    return;
                }
                //处理每条配置
                foreach (DataRow configRow in dtWebhooks.Rows)
                {
                    //2. 获取数据
                    string info = configRow["Info"]?.ToString();
                    string keys = configRow["Keys"]?.ToString();
                    string pushTime = configRow["PushTime"]?.ToString();
                    string pushTable = configRow["PushTable"]?.ToString();
                    int maxPushCount = configRow["MaxPushCount"] != DBNull.Value ? Convert.ToInt32(configRow["MaxPushCount"]) : 1000;
                    int pushInterval = configRow["PushInterval"] != DBNull.Value ? Convert.ToInt32(configRow["PushInterval"]) * 1000 : 1000;
                    string pushType = configRow["PushType"]?.ToString();
                    int id = Convert.ToInt32(configRow["ID"]);
                    int pushTimes = configRow["PushTimes"] != DBNull.Value ? Convert.ToInt32(configRow["PushTimes"]) : 0;
                    int remainPushTimes = configRow["RemainPushTimes"] != DBNull.Value ? Convert.ToInt32(configRow["RemainPushTimes"]) : 0;
                    double lastPushSlot = configRow["LastPushHour"] != DBNull.Value ? Convert.ToDouble(configRow["LastPushHour"]) : -1;

                    if (string.IsNullOrEmpty(info) || string.IsNullOrEmpty(keys) || string.IsNullOrEmpty(pushTime) || string.IsNullOrEmpty(pushTable) || string.IsNullOrEmpty(pushType))
                    {
                        msg = $"{pushTable}的数据推送数据配置缺失，跳过该表的数据推送";
                        //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                        continue;
                    }
                    double currentTime = DateTime.Now.Hour + DateTime.Now.Minute / 60.0;
                    //3. 检查推送时间
                    var pushTimeList = pushTime.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => double.TryParse(x, out _))
                        .Select(x => double.Parse(x))
                        .ToList();
                    bool shouldPush = pushTimeList.Any(target => currentSlot >= target && currentSlot < target + 0.1);//6分钟误差
                    if (!shouldPush)
                    {
                        continue;
                    }
                    // 3.1 判断是否还允许推送（防止重复执行）
                    if (Math.Abs(lastPushSlot - currentSlot) < 0.01)
                    {
                        msg = $"{pushTable} 的 {currentSlot} 时间段已推送过，跳过重复推送";
                        //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                        continue;
                    }
                    if (remainPushTimes <= 0 || remainPushTimes > pushTimes)
                    {
                        // 推送次数异常，不推送
                        msg = $"{pushTable} 的推送次数状态异常，RemainPushTimes = {remainPushTimes}, PushTimes = {pushTimes}，跳过";
                        //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                        continue;
                    }

                    // 3.2 执行到这里说明应该推送，更新数据库中的 RemainPushTimes - 1
                    int affectRows = SqlHelper.ExecuteNonQuery($@"
                            UPDATE Dev_Webhook
                            SET RemainPushTimes = RemainPushTimes - 1,
                                LastPushHour = {currentSlot},
                                ModifyedOn = Getdate()
                            WHERE ID = {id}
                        ");

                    // 3.3 如果更新失败（并发情况），则跳过推送
                    if (affectRows == 0)
                    {
                        msg = $"{pushTable} 的推送存在并发竞争，RemainPushTimes 未成功更新，跳过此次推送";
                        //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                        continue;
                    }

                    //4. 获取表数据
                    DataTable dtData;
                    dtData = SqlHelper.ExecuteDataTable($"SELECT * FROM {pushTable}");
                    if (dtData.Rows.Count == 0)
                    {
                        msg = $"{pushTable}无数据可推送";
                        //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送, msg, null, null);
                        continue;
                    }
                    //5. 拆分keys
                    var keyList = keys?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(k => k.Trim())
                                    .Where(k => !string.IsNullOrEmpty(k))
                                    .ToList() ?? new List<string>();

                    if (!keyList.Any())
                    {
                        msg = $"{pushTable}的数据推送接口解析失败";
                        //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                        continue;
                    }

                    //绘制数据
                    //string pushTitle = configRow["PushTitle"]?.ToString();
                    string pushTitle = dtData.Rows[0]["PushTitle"]?.ToString();//改成从数据表获取标题
                    string pushMessage = configRow["PushMessage"]?.ToString();

                    int totalCount = dtData.Rows.Count;//数据总量
                    int sendTimes = (int)Math.Ceiling((double)totalCount / maxPushCount);//计算发送的次数
                    Dictionary<string, int> successCounts = new Dictionary<string, int>(); // 记录每个key的成功次数
                    foreach (var key in keyList)
                    {
                        successCounts[key] = 0;
                    }

                    for (int i = 0; i < sendTimes; i++)
                    {
                        int startIndex = i * maxPushCount;
                        int endIndex = Math.Min((i + 1) * maxPushCount, totalCount);
                        // 构建 markdown_v2 内容
                        StringBuilder markdownContent = new StringBuilder();
                        if (pushType == "卡片")
                        {
                            // 添加标题
                            string titleLine = sendTimes == 1
                                ? $"# {pushTitle}\n"
                                : $"# {pushTitle}{i + 1}\n";

                            markdownContent.AppendLine(titleLine);

                            // 添加说明提示
                            //markdownContent.AppendLine("> 数据如下所示：\n");

                            // 构建数据行（每条数据为一块）
                            for (int j = startIndex; j < endIndex; j++)
                            {
                                foreach (DataColumn col in dtData.Columns)
                                {

                                    string colName = col.ColumnName;
                                    // 跳过 PushTitle 字段
                                    if (colName.Equals("PushTitle", StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    string value = dtData.Rows[j][col]?.ToString()?.Replace("\n", " ").Replace("|", "｜") ?? ""; // 防止 markdown 字符冲突
                                    markdownContent.AppendLine($"**{colName}：** {value} \n");
                                }

                                markdownContent.AppendLine("\n---\n"); // 分隔线
                            }
                        }
                        else if (pushType == "表格")
                        {
                            // 添加标题（使用markdown_v2的标题语法）
                            markdownContent.AppendLine($"# {pushTitle}\n");
                            // 构建表头
                            List<string> headers = new List<string>();

                            // 添加序号列
                            //headers.Add("序号");

                            // 添加数据列
                            foreach (DataColumn Col in dtData.Columns)
                            {
                                string colName = Col.ColumnName;
                                // 跳过 PushTitle 字段
                                if (colName.Equals("PushTitle", StringComparison.OrdinalIgnoreCase))
                                    continue;
                                headers.Add(colName);
                            }

                            // 输出表头行
                            markdownContent.AppendLine("| " + string.Join(" | ", headers) + " |");

                            // 输出对齐行
                            markdownContent.AppendLine("|" + string.Join("|", headers.Select(h =>
                            {
                                if (h == "序号") return " :----: ";// 序号居中
                                return " :---- ";// 其他列左对齐
                            })) + "|");

                            // 构建表格数据行
                            for (int j = startIndex; j < endIndex; j++)
                            {
                                List<string> rowData = new List<string>();
                                //rowData.Add((j + 1).ToString());  // 序号

                                foreach (DataColumn col in dtData.Columns)
                                {
                                    if (col.ColumnName.Equals("PushTitle", StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    rowData.Add(dtData.Rows[j][col.ColumnName]?.ToString() ?? "");
                                }

                                markdownContent.AppendLine("| " + string.Join(" | ", rowData) + " |");
                            }
                        }

                        // 构建请求体
                        var contentJson = new
                        {
                            msgtype = "markdown_v2",
                            markdown_v2 = new
                            {
                                content = markdownContent.ToString()
                            }
                        };
                        //开始推送
                        try
                        {
                            var json = JsonConvert.SerializeObject(contentJson);
                            // 对每个key进行推送
                            foreach (var key in keyList)
                            {
                                var fullUrl = $"{info}{key}";
                                var content = new StringContent(json, Encoding.UTF8, "application/json");

                                var client = _httpClientFactory.CreateClient(nameof(WeChatWebhookPushJob));
                                {
                                    var response = await client.PostAsync(fullUrl, content, cancellationToken);
                                    string result = await response.Content.ReadAsStringAsync();

                                    if (response.IsSuccessStatusCode)
                                    {
                                        successCounts[key]++;
                                        _systemLog.SaveLog(SystemLog.SystemLogType.接口推送, $"信息推送(key:{key})成功(第{i + 1}批)，共{endIndex - startIndex}条", null, null);
                                    }
                                    else
                                    {
                                        _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, $"信息推送(key:{key})失败(第{i + 1}批)，错误: {result}", null, null);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, $"请求异常(第{i + 1}批)：{ex.Message}", null, null);
                        }

                        if (i < sendTimes - 1 && pushInterval > 0)
                        {
                            await Task.Delay(pushInterval);
                        }
                    }

                    string successSummary = string.Join("，", successCounts.Select(kv => $"key:{kv.Key}成功{kv.Value}批"));
                    _systemLog.SaveLog(SystemLog.SystemLogType.接口推送, $"消息推送完成，共{sendTimes}批，{successSummary}", null, null);
                }

                // 添加默认返回值
                msg = "全部数据推送完成";
                //_systemLog.SaveLog(SystemLog.SystemLogType.接口推送, msg, null, null);
                return;
            }
            catch (Exception ex)
            {
                msg = $"获取消息内容失败：{ex.Message}";
                _systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, msg, null, null);
                return;
            }
    }
}
