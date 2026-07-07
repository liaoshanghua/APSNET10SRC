using EasyManufacture.Domain.Options;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Data;

namespace EasyManufacture.Infrastructure.Scheduling.Jobs;

public sealed class YrfExcelImportJob
{
    private readonly ScheduledTasksOptions _options;
    private readonly SystemLog _systemLog = new();

    public YrfExcelImportJob(IOptions<ScheduledTasksOptions> options) => _options = options.Value;

    public void Run()
        {
            bool result = true;
            string msg = "";
            try
            {
                string UserName = "system";
                string UserAccount = "system";
                string StoredProcedure = "P_APS_ProcessMaterialImport";

                // 2. 扫描指定目录下的Excel文件
                string directoryPath = _options.YrfExcelDirectory;
                if (!Directory.Exists(directoryPath))
                {
                    _systemLog.SaveLog(SystemLog.SystemLogType.下载Excel, "目录不存在：" + directoryPath, null, null);
                    return;
                }

                // 3. 获取所有Excel文件（排除已处理过的文件）
                var excelFiles = Directory.GetFiles(directoryPath, "*.xlsx")
                    .Where(f => !f.EndsWith("已导入.xlsx") && !f.EndsWith("导入失败.xlsx"))
                    .ToList();

                if (excelFiles.Count == 0)
                {
                    msg = "没有需要导入的Excel文件";
                    result = true; // 这不算错误，只是没有文件
                    //jsonResult = base.FormResult(result, msg, ReponseData);
                    //return jsonResult;
                }

                // 4. 记录处理结果
                List<string> processResults = new List<string>();
                int successCount = 0;
                int failCount = 0;

                foreach (string filePath in excelFiles)
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileName(filePath);
                        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        string fileExtension = System.IO.Path.GetExtension(filePath);

                        // 5. 读取Excel文件并转换为对象列表
                        var dataList = YRFReadExcelAndConvertProcessMaterial(filePath);

                        if (dataList.Count == 0)
                        {
                            // 文件没有数据
                            //string newFileName = System.IO.Path.Combine(directoryPath, $"{fileNameWithoutExt}导入失败{fileExtension}");
                            //SafeFileMove(filePath, newFileName);
                            processResults.Add($"{fileName}: 文件为空，没有数据");
                            failCount++;
                            continue;
                        }

                        // 6. 序列化为JSON
                        string jsonData = JsonConvert.SerializeObject(dataList);

                        // 7. 调用存储过程
                        SqlParameter[] sqlParameters = new SqlParameter[6];
                        sqlParameters[0] = new SqlParameter("@jsonData", jsonData);
                        sqlParameters[1] = new SqlParameter("@UserName", UserName);
                        sqlParameters[2] = new SqlParameter("@UserAccount", UserAccount);
                        sqlParameters[3] = new SqlParameter("@Result", SqlDbType.Bit);
                        sqlParameters[4] = new SqlParameter("@Msg", SqlDbType.NVarChar, 500);
                        sqlParameters[5] = new SqlParameter("@ReponseData", SqlDbType.NVarChar, -1);
                        sqlParameters[3].Direction = sqlParameters[4].Direction = sqlParameters[5].Direction = ParameterDirection.Output;

                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, StoredProcedure, sqlParameters);

                        bool fileResult = sqlParameters[3].Value.ToString().ToLower() == "true";
                        string fileMsg = sqlParameters[4].Value.ToString();

                        // 8. 根据导入结果重命名文件
                        if (fileResult)
                        {
                            //string newFileName = System.IO.Path.Combine(directoryPath, $"{fileNameWithoutExt}已导入{fileExtension}");
                            //SafeFileMove(filePath, newFileName);
                            processResults.Add($"{fileName}: 导入成功 - {fileMsg}");
                            successCount++;
                        }
                        else
                        {
                            //string newFileName = System.IO.Path.Combine(directoryPath, $"{fileNameWithoutExt}导入失败{fileExtension}");
                            //SafeFileMove(filePath, newFileName);
                            processResults.Add($"{fileName}: 导入失败 - {fileMsg}");
                            failCount++;
                        }
                    }
                    catch (Exception fileEx)
                    {
                        // 单个文件处理失败，记录并继续处理其他文件
                        string fileName = System.IO.Path.GetFileName(filePath);
                        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        string fileExtension = System.IO.Path.GetExtension(filePath);

                        //string newFileName = System.IO.Path.Combine(directoryPath, $"{fileNameWithoutExt}导入失败{fileExtension}");
                        //SafeFileMove(filePath, newFileName);

                        processResults.Add($"{fileName}: 处理异常 - {fileEx.Message}");
                        failCount++;
                    }
                }

                // 9. 汇总处理结果
                if (processResults.Count > 0)
                {
                    msg = $"文件处理完成。成功：{successCount}个，失败：{failCount}个。\n" +
                          string.Join("\n", processResults);
                    result = failCount == 0; // 如果有失败的文件，整体结果为false
                    _systemLog.SaveLog(SystemLog.SystemLogType.下载Excel, msg, null, null);
                }
                else
                {
                    msg = "没有文件需要处理";
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                msg = "系统异常：" + ex.Message;
            }

            //jsonResult = base.FormResult(result, msg, ReponseData);
            //return jsonResult;
        }

        private List<JObject> YRFReadExcelAndConvertProcessMaterial(string filePath)
        {
            var dataList = new List<JObject>();

            try
            {
                // 使用EPPlus读取Excel文件
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];

                    // 检查是否有数据
                    if (worksheet.Dimension == null)
                    {
                        return dataList; // 返回空列表
                    }

                    // 读取表头行，建立列索引映射
                    Dictionary<string, int> columnMapping = new Dictionary<string, int>();
                    int colCount = worksheet.Dimension.Columns;

                    for (int col = 1; col <= colCount; col++)
                    {
                        string header = worksheet.Cells[1, col].Text.Trim();

                        // 根据中文表头映射到英文属性名
                        switch (header)
                        {
                            case "产品编码":
                                columnMapping["Code"] = col;
                                break;
                            case "顺序":
                                columnMapping["ViewSort"] = col;
                                break;
                            case "工段":
                                columnMapping["ProcessPartName"] = col;
                                break;
                            case "工序":
                                columnMapping["ProcessName"] = col;
                                break;
                            case "人数":
                                columnMapping["StandardPeoples"] = col;
                                break;
                            case "治具/设备名称":
                                columnMapping["MachineName"] = col;
                                break;
                            case "治具/设备数量":
                                columnMapping["MachineNum"] = col;
                                break;
                            case "实测工时":
                                columnMapping["ActualTime"] = col;
                                break;
                            case "标准工时":
                                columnMapping["Seconds"] = col;
                                break;
                            case "平均时间":
                                columnMapping["AverageTime"] = col;
                                break;
                            case "瓶颈工时":
                                columnMapping["BottleneckTime"] = col;
                                break;
                            case "标准小时产能":
                                columnMapping["Capacity"] = col;  // 注意：这里改为 Capacity
                                break;
                            case "工段总工时":
                                columnMapping["ProcessGroupTime"] = col;
                                break;
                            case "平衡率":
                                columnMapping["Efficiency"] = col;  // 注意：这里改为 Efficiency
                                break;
                            case "换线时间(分)":
                                columnMapping["Remark2"] = col;
                                break;
                            case "管理项目":
                                columnMapping["Project"] = col;
                                break;
                            case "品质特性":
                                columnMapping["QualityCharacteristics"] = col;
                                break;
                            case "基准/规格":
                                columnMapping["Spec"] = col;  // 注意：这里改为 Spec
                                break;
                            case "记录方式":
                                columnMapping["RecordType"] = col;
                                break;
                            case "加工要求":
                                columnMapping["ProcessRequire"] = col;
                                break;
                        }
                    }

                    // 检查是否有必要的列
                    if (!columnMapping.ContainsKey("Code"))
                    {
                        throw new Exception("Excel文件缺少必要的列：产品编码");
                    }

                    // 读取数据行（从第2行开始）
                    int rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        // 跳过空行
                        string code = columnMapping.ContainsKey("Code") ?
                            worksheet.Cells[row, columnMapping["Code"]].Text.Trim() : "";

                        if (string.IsNullOrEmpty(code))
                            continue; // 跳过产品编码为空的记录

                        // 创建JObject
                        JObject data = new JObject();
                        data["Code"] = code;

                        if (columnMapping.ContainsKey("ViewSort"))
                        {
                            string viewSortStr = worksheet.Cells[row, columnMapping["ViewSort"]].Text.Trim();
                            if (!string.IsNullOrEmpty(viewSortStr) && int.TryParse(viewSortStr, out int viewSort))
                                data["ViewSort"] = viewSort;
                        }

                        if (columnMapping.ContainsKey("ProcessPartName"))
                            data["ProcessPartName"] = worksheet.Cells[row, columnMapping["ProcessPartName"]].Text.Trim();

                        if (columnMapping.ContainsKey("ProcessName"))
                            data["ProcessName"] = worksheet.Cells[row, columnMapping["ProcessName"]].Text.Trim();

                        if (columnMapping.ContainsKey("StandardPeoples"))
                        {
                            string peoplesStr = worksheet.Cells[row, columnMapping["StandardPeoples"]].Text.Trim();
                            if (!string.IsNullOrEmpty(peoplesStr) && decimal.TryParse(peoplesStr, out decimal peoples))
                                data["StandardPeoples"] = peoples;
                        }

                        if (columnMapping.ContainsKey("MachineName"))
                            data["MachineName"] = worksheet.Cells[row, columnMapping["MachineName"]].Text.Trim();

                        if (columnMapping.ContainsKey("MachineNum"))
                        {
                            string numStr = worksheet.Cells[row, columnMapping["MachineNum"]].Text.Trim();
                            if (!string.IsNullOrEmpty(numStr) && int.TryParse(numStr, out int num))
                                data["MachineNum"] = num;
                        }

                        // 其他字段
                        if (columnMapping.ContainsKey("ActualTime"))
                        {
                            string actualTimeStr = worksheet.Cells[row, columnMapping["ActualTime"]].Text.Trim();
                            if (!string.IsNullOrEmpty(actualTimeStr) && decimal.TryParse(actualTimeStr, out decimal actualTime))
                                data["ActualTime"] = actualTime;
                        }

                        if (columnMapping.ContainsKey("Seconds"))
                        {
                            string secondsStr = worksheet.Cells[row, columnMapping["Seconds"]].Text.Trim();
                            if (!string.IsNullOrEmpty(secondsStr) && decimal.TryParse(secondsStr, out decimal seconds))
                                data["Seconds"] = seconds;
                        }

                        if (columnMapping.ContainsKey("AverageTime"))
                        {
                            string avgTimeStr = worksheet.Cells[row, columnMapping["AverageTime"]].Text.Trim();
                            if (!string.IsNullOrEmpty(avgTimeStr) && decimal.TryParse(avgTimeStr, out decimal avgTime))
                                data["AverageTime"] = avgTime;
                        }

                        if (columnMapping.ContainsKey("BottleneckTime"))
                        {
                            string bottleneckStr = worksheet.Cells[row, columnMapping["BottleneckTime"]].Text.Trim();
                            if (!string.IsNullOrEmpty(bottleneckStr) && decimal.TryParse(bottleneckStr, out decimal bottleneck))
                                data["BottleneckTime"] = bottleneck;
                        }

                        // 注意：这里改成 Capacity 而不是 ProcessCapacity
                        if (columnMapping.ContainsKey("Capacity"))
                        {
                            string capacityStr = worksheet.Cells[row, columnMapping["Capacity"]].Text.Trim();
                            if (!string.IsNullOrEmpty(capacityStr) && decimal.TryParse(capacityStr, out decimal capacity))
                                data["Capacity"] = capacity;  // 注意：属性名改为 Capacity
                        }

                        if (columnMapping.ContainsKey("ProcessGroupTime"))
                        {
                            string groupTimeStr = worksheet.Cells[row, columnMapping["ProcessGroupTime"]].Text.Trim();
                            if (!string.IsNullOrEmpty(groupTimeStr) && decimal.TryParse(groupTimeStr, out decimal groupTime))
                                data["ProcessGroupTime"] = groupTime;
                        }

                        // 注意：这里改成 Efficiency 而不是 Remark2
                        if (columnMapping.ContainsKey("Efficiency"))
                        {
                            string efficiencyStr = worksheet.Cells[row, columnMapping["Efficiency"]].Text.Trim();
                            if (!string.IsNullOrEmpty(efficiencyStr) && decimal.TryParse(efficiencyStr, out decimal efficiency))
                                data["Efficiency"] = efficiency;  // 注意：属性名改为 Efficiency
                        }

                        if (columnMapping.ContainsKey("Remark2"))
                            data["Remark2"] = worksheet.Cells[row, columnMapping["Remark2"]].Text.Trim();

                        if (columnMapping.ContainsKey("Project"))
                            data["Project"] = worksheet.Cells[row, columnMapping["Project"]].Text.Trim();

                        if (columnMapping.ContainsKey("QualityCharacteristics"))
                            data["QualityCharacteristics"] = worksheet.Cells[row, columnMapping["QualityCharacteristics"]].Text.Trim();

                        // 注意：这里改成 Spec 而不是 ProcessRequire
                        if (columnMapping.ContainsKey("Spec"))
                            data["Spec"] = worksheet.Cells[row, columnMapping["Spec"]].Text.Trim();  // 注意：属性名改为 Spec

                        if (columnMapping.ContainsKey("RecordType"))
                            data["RecordType"] = worksheet.Cells[row, columnMapping["RecordType"]].Text.Trim();

                        if (columnMapping.ContainsKey("ProcessRequire"))
                            data["ProcessRequire"] = worksheet.Cells[row, columnMapping["ProcessRequire"]].Text.Trim();

                        dataList.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"读取Excel文件失败：{ex.Message}");
            }

            return dataList;
        }

}
