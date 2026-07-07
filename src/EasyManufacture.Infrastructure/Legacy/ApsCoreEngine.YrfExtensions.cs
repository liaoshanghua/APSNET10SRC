using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Licence;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 盈瑞丰 SetDt/setDetail 实现（精简模式，EnableLegacyApsApiSource=false 时编译）。
/// 全量模式下同名方法在 LegacyApi.cs；dic 挂载见 YrfDicHooks.cs。
/// </summary>
public partial class ApsCoreEngine
{
        public void SetDt28574(ref DataTable dt)
        {

            // 获取计划数据，包含SalesPlanProcessMaterialDayID
            DtDetail = SqlHelper.ExecuteDataTable(string.Format(@"
                    SELECT ProcessID,ProcessName
                    FROM   APS_Process A where A.ProcessName is not null
            "));

            DtDetail1 = SqlHelper.ExecuteDataTable(string.Format(@"
            SELECT A.ProcessID,
                    A.Account,
                    Max(B.PositionLevel) AS PositionLevel,
                    C.ProcessName
            FROM   APS_ProcessPosition A
                    LEFT JOIN Dev_PositionLevel B
                            ON A.LevelID = B.PositionLevelID
                    LEFT JOIN APS_Process C
                            ON A.ProcessID = C.ProcessID
            GROUP  BY A.ProcessID,
                        A.Account,
                        C.ProcessName
            "));
            foreach (DataRow row in DtDetail.Rows)
            {
                string ProcessID = row["ProcessID"].ToString();
                dt.Columns.Add(ProcessID);
                ElementColumn[0].Add(new ElementTableOuput() { label = row["ProcessName"] + "", prop = ProcessID, width = "80", prop2 = ProcessID, isEdit = false, ControlType = "textbox" });
            }
        }
        /// <summary>
        /// 人员工序技能矩阵报表
        /// </summary>
        /// <param name="dataRow"></param>
        public void setDetai28574(DataRow dataRow)
        {
            foreach (DataRow row in DtDetail.Rows)
            {
                string ProcessID = row["ProcessID"].ToString();
                DataRow[] rows = DtDetail1.Select("ProcessID='" + ProcessID + "' AND Account='" + dataRow["Account"] + "'");
                if (rows.Length > 0)
                {
                    dataRow[ProcessID] = rows[0]["PositionLevel"];
                }
            }
        }

        //
        /// <summary>
        /// 盈瑞丰：按客户月度统计
        /// </summary>
        /// <param name="dt"></param>

        public void SetDt28581(ref DataTable dt)
        {
            // ... (前部分代码保持不变：日期计算、列添加、SQL查询) ...

            DateTime startDate = DateTime.Now.Date;
            DateTime endDate = startDate;
            if (jObject.ContainsKey("monthRange"))
            {
                JArray jArray = jObject["monthRange"] as JArray;
                if (!string.IsNullOrEmpty(jArray[0].ToString()))
                {
                    startDate = DateTime.Parse(jArray[0].ToString());
                    endDate = DateTime.Parse(jArray[1].ToString());
                }
            }

            dt.Columns.Add("StockOutQtySummary", typeof(decimal));
            ElementColumn[0].Add(new ElementTableOuput() { label = "交货数", prop = "StockOutQtySummary", prop2 = "StockOutQtySummary", width = "80", isEdit = false, ControlType = "textbox", formatter = "#,##0" });
            dt.Columns.Add("AmountSummary", typeof(decimal));
            ElementColumn[0].Add(new ElementTableOuput() { label = "金额", prop = "AmountSummary", prop2 = "AmountSummary", width = "80", isEdit = false, ControlType = "textbox", formatter = "#,##0" });

            DateTime FirstDate = startDate;
            while (FirstDate <= endDate)
            {
                string label = string.Format("{0:yyyy年MM月}", FirstDate);
                string prop = string.Format("{0:yyyy-MM}", FirstDate);
                dt.Columns.Add(prop, typeof(decimal));
                ElementColumn[0].Add(new ElementTableOuput() { label = label, prop = prop, prop2 = prop, width = "100", isEdit = false, ControlType = "textbox", formatter = "#,##0" });
                FirstDate = FirstDate.AddMonths(1);
            }

            // 销售订单查询
            DtDetail = SqlHelper.ExecuteDataTable(string.Format(@"
     SELECT CustomerID
   ,CustomerName
   ,Format(OrderDate, 'yyyy-MM') AS YearMonthTime
   ,Sum(StockOutQty) AS StockOutQty
   ,CONVERT(INT,Sum(StockOutQty * Price)) AS Amount
    FROM   V_APS_SalesOrderDetailALL
    WHERE  1=1
    AND Format(OrderDate, 'yyyy-MM') >= '{0}'
    AND Format(OrderDate, 'yyyy-MM') <= '{1}'
    GROUP  BY CustomerID, CustomerName, Format(OrderDate, 'yyyy-MM') 
    ", string.Format("{0:yyyy-MM}", startDate), string.Format("{0:yyyy-MM}", endDate)));

            dt.Columns.Add("BColors", typeof(Dictionary<string, string>));

            // ... (填充数据的 foreach 循环保持不变) ...
            foreach (DataRow dataRow in dt.Rows)
            {
                string CustomerID = dataRow["CustomerID"].ToString();
                Dictionary<string, string> bColors = new Dictionary<string, string>();
                decimal StockOutQtySummary = 0;
                decimal AmountSummary = 0;

                // 注意：这里使用了 Compute，确保 DtDetail 中有数据
                string StockOutQtySummaryTemp = DtDetail.Compute("SUM(StockOutQty)", "CustomerID = '" + CustomerID.Replace("'", "''") + "'").ToString(); // 防止SQL注入风格的单引号错误
                string AmountSummaryTemp = DtDetail.Compute("SUM(Amount)", "CustomerID = '" + CustomerID.Replace("'", "''") + "'").ToString(); // 防止SQL注入风格的单引号错误

                decimal.TryParse(StockOutQtySummaryTemp, out StockOutQtySummary);
                decimal.TryParse(AmountSummaryTemp, out AmountSummary);
                dataRow["StockOutQtySummary"] = StockOutQtySummary; // 直接赋值 decimal，避免 ToString 后再转换
                dataRow["AmountSummary"] = AmountSummary; // 直接赋值 decimal，避免 ToString 后再转换
                bColors.Add("StockOutQtySummary", "#EBFF10");
                DateTime SecondDate = startDate;
                while (SecondDate <= endDate)
                {
                    string prop = string.Format("{0:yyyy-MM}", SecondDate);
                    decimal Amount = 0;
                    DataRow[] dataRows = DtDetail.Select("YearMonthTime='" + prop + "' AND CustomerID='" + CustomerID.Replace("'", "''") + "'");

                    if (dataRows.Length > 0)
                    {
                        decimal s;
                        decimal.TryParse(dataRows[0]["Amount"].ToString(), out s);
                        Amount = s;
                    }
                    dataRow[prop] = Amount;

                    SecondDate = SecondDate.AddMonths(1);
                }
                dataRow["BColors"] = bColors;
            }

            // ================= 新增排序逻辑开始 =================

            // 1. 创建 DataView 基于当前的 dt
            DataView dv = dt.DefaultView;

            // 2. 设置排序表达式：按 AmountSummary 降序 (DESC)
            // 如果需要升序，去掉 "DESC" 即可
            dv.Sort = "AmountSummary DESC";

            // 3. 将排序后的视图转换回 DataTable
            DataTable sortedDt = dv.ToTable();

            // 4. 清空原 dt 的行，并将排序后的行导入回去
            // 这样做是为了保持传入的 ref dt 引用地址不变，确保外部调用者拿到的是排序后的数据
            dt.Clear();

            // 导入行时保留原始值
            foreach (DataRow row in sortedDt.Rows)
            {
                dt.ImportRow(row);
            }

            // ================= 新增排序逻辑结束 =================
        }
        //
        /// <summary>
        /// 盈瑞丰：按客户月度统计
        /// </summary>
        /// <param name="dt"></param>

        public void SetDt28580(ref DataTable dt)
        {
            // 1. 日期范围初始化
            DateTime startDate = DateTime.Now.Date;
            // 默认结束时间为开始时间往后推3个月（一个季度），或者根据你的业务需求调整
            // 如果 jObject 传入的是具体起止日期，则以下逻辑会被覆盖
            DateTime endDate = startDate.AddMonths(3);

            if (jObject.ContainsKey("monthRange")) // 假设前端传入的 key 还是 monthRange，或者是 quarterRange
            {
                JArray jArray = jObject["monthRange"] as JArray;
                if (jArray != null && jArray.Count >= 2 && !string.IsNullOrEmpty(jArray[0].ToString()))
                {
                    startDate = DateTime.Parse(jArray[0].ToString());
                    endDate = DateTime.Parse(jArray[1].ToString());
                }
            }

            // 确保 startDate 是当月1号，方便计算季度
            startDate = new DateTime(startDate.Year, startDate.Month, 1);

            // 2. 添加汇总列
            // 注意：如果 dt 已经有行，这里只加列；如果 dt 是新的，没问题。
            if (!dt.Columns.Contains("StockOutQtySummary"))
            {
                dt.Columns.Add("StockOutQtySummary", typeof(decimal));
                ElementColumn[0].Add(new ElementTableOuput() { label = "交货数", prop = "StockOutQtySummary", prop2 = "StockOutQtySummary", width = "80", isEdit = false, ControlType = "textbox", formatter = "#,##0" });
            }
            if (!dt.Columns.Contains("AmountSummary"))
            {
                dt.Columns.Add("AmountSummary", typeof(decimal));
                ElementColumn[0].Add(new ElementTableOuput() { label = "金额", prop = "AmountSummary", prop2 = "AmountSummary", width = "80", isEdit = false, ControlType = "textbox", formatter = "#,##0" });
            }

            // 3. 动态添加季度列
            DateTime FirstDate = startDate;
            while (FirstDate <= endDate)
            {
                // 计算当前日期属于第几季度
                int quarter = (FirstDate.Month - 1) / 3 + 1;

                // 标签：2026年Q1季度
                string label = string.Format("{0:yyyy}年Q{1}季度", FirstDate, quarter);

                // 属性名/列名：2026-Q1 (避免使用中文或特殊字符作为列名，方便处理)
                string prop = string.Format("{0:yyyy}-Q{1}", FirstDate, quarter);

                // 防止重复添加列
                if (!dt.Columns.Contains(prop))
                {
                    dt.Columns.Add(prop, typeof(decimal));
                    ElementColumn[0].Add(new ElementTableOuput() { label = label, prop = prop, prop2 = prop, width = "120", isEdit = false, ControlType = "textbox", formatter = "#,##0" });
                }

                // 每次增加 3 个月
                FirstDate = FirstDate.AddMonths(3);
            }

            // 4. SQL 查询 (按季度分组)
            // 逻辑：将月份转换为 'yyyy-Qx' 格式字符串
            string sql = string.Format(@"
      SELECT CustomerID
           , CustomerName
           , CAST(YEAR(OrderDate) AS VARCHAR) + '-Q' + CAST((MONTH(OrderDate) - 1) / 3 + 1 AS VARCHAR) AS YearQuarterTime
           , SUM(StockOutQty) AS StockOutQty
           , CONVERT(INT,Sum(StockOutQty * Price)) AS Amount
      FROM V_APS_SalesOrderDetailALL
      WHERE 1=1
        -- 筛选起始季度：计算 startDate 所在的季度字符串
        AND (CAST(YEAR(OrderDate) AS VARCHAR) + '-Q' + CAST((MONTH(OrderDate) - 1) / 3 + 1 AS VARCHAR)) >= '{0}'
        -- 筛选结束季度：计算 endDate 所在的季度字符串
        AND (CAST(YEAR(OrderDate) AS VARCHAR) + '-Q' + CAST((MONTH(OrderDate) - 1) / 3 + 1 AS VARCHAR)) <= '{1}'
      GROUP BY CustomerID, CustomerName, CAST(YEAR(OrderDate) AS VARCHAR) + '-Q' + CAST((MONTH(OrderDate) - 1) / 3 + 1 AS VARCHAR)
    ",
                string.Format("{0:yyyy}-Q{1}", startDate, (startDate.Month - 1) / 3 + 1),
                string.Format("{0:yyyy}-Q{1}", endDate, (endDate.Month - 1) / 3 + 1)
            );

            DtDetail = SqlHelper.ExecuteDataTable(sql);

            // 5. 添加背景色列
            if (!dt.Columns.Contains("BColors"))
            {
                dt.Columns.Add("BColors", typeof(Dictionary<string, string>));
            }

            // 6. 填充数据
            // 注意：这里假设 dt 的行（客户列表）已经由其他逻辑生成好了。
            // 如果 dt 是空的，这段循环不会执行。通常 SetDt 方法前会有填充 CustomerID 的逻辑。
            foreach (DataRow dataRow in dt.Rows)
            {
                string CustomerID = dataRow["CustomerID"].ToString();
                Dictionary<string, string> bColors = new Dictionary<string, string>();
                decimal StockOutQtySummary = 0;
                decimal AmountSummary = 0;

                // 计算该客户的总交货数
                // 使用 Replace 防止 CustomerID 中包含单引号导致 Compute 报错
                string filter = "CustomerID = '" + CustomerID.Replace("'", "''") + "'";
                object sumObj = DtDetail.Compute("SUM(StockOutQty)", filter);
                object sumObjAmount = DtDetail.Compute("SUM(Amount)", filter);

                if (sumObj != DBNull.Value)
                {
                    decimal.TryParse(sumObj.ToString(), out StockOutQtySummary);
                }
                if (sumObj != DBNull.Value)
                {
                    decimal.TryParse(sumObjAmount.ToString(), out AmountSummary);
                }
                dataRow["StockOutQtySummary"] = StockOutQtySummary;
                dataRow["AmountSummary"] = AmountSummary;
                bColors.Add("StockOutQtySummary", "#EBFF10");
                // 按季度填充具体数值
                DateTime SecondDate = startDate;
                while (SecondDate <= endDate)
                {
                    int quarter = (SecondDate.Month - 1) / 3 + 1;
                    string prop = string.Format("{0:yyyy}-Q{1}", SecondDate, quarter);

                    decimal Amount = 0;

                    if (dt.Columns.Contains(prop)) // 确保列存在
                    {
                        // 构造筛选条件：季度匹配 且 客户ID匹配
                        string detailFilter = "YearQuarterTime = '" + prop + "' AND CustomerID = '" + CustomerID.Replace("'", "''") + "'";

                        DataRow[] dataRows = DtDetail.Select(detailFilter);

                        if (dataRows.Length > 0)
                        {
                            decimal s;
                            if (dataRows[0]["Amount"] != DBNull.Value)
                            {
                                decimal.TryParse(dataRows[0]["Amount"].ToString(), out s);
                                Amount = s;
                            }
                        }
                        dataRow[prop] = Amount;
                    }

                    // 每次增加 3 个月
                    SecondDate = SecondDate.AddMonths(3);
                }

                dataRow["BColors"] = bColors;
            }

            // 7. 排序逻辑 (按汇总数降序)
            DataView dv = dt.DefaultView;
            dv.Sort = "AmountSummary DESC";

            DataTable sortedDt = dv.ToTable();

            dt.Clear();
            foreach (DataRow row in sortedDt.Rows)
            {
                dt.ImportRow(row);
            }
        }
        /// <summary>
        /// 盈瑞丰：出勤工时明细 汇总+动态列
        /// </summary>
        /// <param name="dt"></param>
        public void SetDt28589(ref DataTable dt)
        {
            string targetYearMonth = DateTime.Now.ToString("yyyy-MM");
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            if (jObject.ContainsKey("YearMonth"))
            {
                string inputVal = jObject["YearMonth"]?.ToString();
                DateTime parsedDate;

                // 尝试严格按照 "yyyy-MM" 格式解析
                if (!string.IsNullOrEmpty(inputVal) &&
                    DateTime.TryParseExact(inputVal, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    targetYearMonth = inputVal;
                    year = parsedDate.Year;
                    month = parsedDate.Month;
                }
            }

            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1); // 下月1号，作为排他边界

            string sql = $@"
                SELECT 
                    *,
                CAST(AttendanceDate AS DATE) AS AttendanceDate99
                FROM APS_LineAttendanceDetail 
                WHERE AttendanceDate >= '{startDate:yyyy-MM-dd}'
                  AND AttendanceDate <  '{endDate:yyyy-MM-dd}' -- 使用 < 下月1号 覆盖整月
                  AND AttendanceStatus = '76'
            ";

            // 执行查询
            DtDetail = SqlHelper.ExecuteDataTable(sql);

            int daysInMonth = DateTime.DaysInMonth(year, month);

            // --- B. 添加日期列 (1号 到 月末) ---
            for (int day = 1; day <= daysInMonth; day++)
            {
                string prop = $"{month:D2}-{day:D2}";
                //string dayColumnKey = $"Day{dayStr}";
                //string dayLabel = $"{month:D2}-{day:D2}";

                // 判断是否是周末 (可选优化：周末列标红或特殊显示)
                DateTime currentDayDate = new DateTime(year, month, day);
                bool isWeekend = currentDayDate.DayOfWeek == DayOfWeek.Saturday || currentDayDate.DayOfWeek == DayOfWeek.Sunday;

                if (!dt.Columns.Contains(prop))
                {
                    dt.Columns.Add(prop);
                    ElementColumn[0].Add(new ElementTableOuput() { label = prop, prop = prop, width = "60", prop2 = prop, routerName = true, isEdit = false, ControlType = "number" });
                }
            }
        }
        /// <summary>
        /// 盈瑞丰：出勤工时明细 汇总+动态列
        /// </summary>
        /// <param name="dataRow"></param>
        public void setDetail28589(DataRow dataRow)
        {
            //2026-03格式
            string YearMonth = dataRow["YearMonth"].ToString();
            int Year = int.Parse(YearMonth.Substring(0, 4));
            int Month = int.Parse(YearMonth.Substring(5, 2));
            int daysInMonth = DateTime.DaysInMonth(Year, Month);
            string OrganizeID = dataRow["OrganizeID"].ToString();
            string Extend1 = dataRow["Extend1"].ToString();
            string Account = dataRow["Account"].ToString();
            string PeriodOfTime = dataRow["PeriodOfTime"].ToString();
            for (int day = 1; day <= daysInMonth; day++)
            {
                DataRow[] rows = DtDetail.Select($"OrganizeID={OrganizeID} AND Account='{Account}' AND Remark1='{Extend1}' AND AttendanceDate='{Year}-{Month}-{day}'");
                if (rows.Length > 0)
                {
                    decimal hours = 0;
                    if (PeriodOfTime == "AM")
                    {
                        hours = (decimal)rows[0]["WorkTimeMorning"];
                    }
                    else if (PeriodOfTime == "PM")
                    {
                        hours = (decimal)rows[0]["WorkTimeAfternoon"];
                    }
                    else if (PeriodOfTime == "加班")
                    {
                        hours = (decimal)rows[0]["WorkTimeEvening"];
                    }
                    else
                    {
                        hours = (decimal)rows[0]["WorkTimeMorning"] + (decimal)rows[0]["WorkTimeAfternoon"] + (decimal)rows[0]["WorkTimeEvening"];
                    }
                    dataRow[$"{Month:D2}-{day:D2}"] = hours.ToString();
                }
            }
        }
        /// <summary>
        /// DSMT周计划
        /// </summary>
        public void SetDt28634(ref DataTable dt)
        {
            // 获取 年份、周次，为空则默认当前年、当前周（周一为一周开始）
            int Years = DateTime.Now.Year;
            int Weeks = DatePartWeek(DateTime.Now);

            // 判断是否传入 Years、Weeks
            if (jObject.ContainsKey("Years") && jObject["Years"] != null && !string.IsNullOrEmpty(jObject["Years"].ToString()))
            {
                int.TryParse(jObject["Years"].ToString(), out Years);
            }
            // 安全获取 Weeks（为空则保持默认当前周）
            if (jObject.ContainsKey("Weeks") && jObject["Weeks"] != null && !string.IsNullOrEmpty(jObject["Weeks"].ToString()))
            {
                int.TryParse(jObject["Weeks"].ToString(), out Weeks);
            }
            // 根据 年+周 生成周一、周日
            var (startDate, endDate) = GetWeekRange(Years, Weeks);

            DateTime FirstDate = startDate;
            while (FirstDate <= endDate)
            {
                string label = string.Format("{0:MM月dd日}", FirstDate);
                string prop = string.Format("{0:yyyy-MM-dd}", FirstDate);
                dt.Columns.Add(prop, typeof(decimal));
                ElementColumn[0].Add(new ElementTableOuput() { label = label, prop = prop, prop2 = prop, width = "80", isEdit = false, ControlType = "textbox" });

                FirstDate = FirstDate.AddDays(1);
            }

            // 日计划查询 → 按 Years + Weeks 查询
            DtDetail = SqlHelper.ExecuteDataTable(string.Format(@"
SELECT A.ProcessPlanID,A.PlanDay,A.PlanQty,A.Years,A.Months,A.Weeks
FROM   APS_DayPlan A INNER JOIN APS_ProcessPlan B ON A.ProcessPlanID = B.ProcessPlanID
INNER JOIN Dev_Organize C ON B.LineID = C.OrganizeID
WHERE  1=1
AND C.OrganizeName LIKE 'SMT'
AND Years = {0}
AND Weeks = {1}
", Years, Weeks));

            // 报工查询 → 按 Years + Weeks 查询
            DtDetail1 = SqlHelper.ExecuteDataTable(string.Format(@"
SELECT A.ProcessPlanID,A.ProductionDate AS PlanDay,A.ReportQty AS PlanQty,A.Years,A.Months,A.Weeks
FROM   V_APS_Report29 A INNER JOIN APS_ProcessPlan B ON A.ProcessPlanID = B.ProcessPlanID
INNER JOIN Dev_Organize C ON B.LineID = C.OrganizeID
WHERE  1=1
AND C.OrganizeName LIKE 'SMT'
AND Years = {0}
AND Weeks = {1}
", Years, Weeks));

            dt.Columns.Add("BColors", typeof(Dictionary<string, string>));

            // ===================== 核心修改：根据 DataType 切换数据源 =====================
            foreach (DataRow dataRow in dt.Rows)
            {
                string ProcessPlanID = dataRow["ProcessPlanID"].ToString();
                string DataType = dataRow["DataType"].ToString(); // 获取当前行类型：预计/实际
                Dictionary<string, string> bColors = new Dictionary<string, string>();

                DateTime SecondDate = startDate;
                while (SecondDate <= endDate)
                {
                    string prop = string.Format("{0:yyyy-MM-dd}", SecondDate);
                    decimal Qty = 0;

                    // 预计 → 读计划数据
                    if (DataType == "预计")
                    {
                        DataRow[] dataRows = DtDetail.Select("PlanDay = '" + prop + "' AND ProcessPlanID = '" + ProcessPlanID + "'");
                        if (dataRows.Length > 0)
                        {
                            decimal.TryParse(dataRows[0]["PlanQty"].ToString(), out Qty);
                        }
                    }
                    // 实际 → 读报工数据
                    else if (DataType == "实际")
                    {
                        DataRow[] dataRows = DtDetail1.Select("PlanDay = '" + prop + "' AND ProcessPlanID = '" + ProcessPlanID + "'");
                        if (dataRows.Length > 0)
                        {
                            decimal.TryParse(dataRows[0]["PlanQty"].ToString(), out Qty);
                        }
                    }

                    dataRow[prop] = Qty;
                    SecondDate = SecondDate.AddDays(1);
                }
                dataRow["BColors"] = bColors;
            }
        }
        /// <summary>
        /// DIP周计划
        /// </summary>
        public void SetDt28636(ref DataTable dt)
        {
            // 从 APS_ProcessPlanHistory 取 第一条 Status=1 的 SaveDate
            DataTable dtFreeze = SqlHelper.ExecuteDataTable(@"
        SELECT TOP 1 SaveDate
        FROM APS_ProcessPlanHistory
        WHERE Status = 1
        ORDER BY SaveDate DESC
    ");

            DateTime startDate = DateTime.Now;
            DateTime endDate = DateTime.Now;

            if (dtFreeze.Rows.Count > 0)
            {
                startDate = Convert.ToDateTime(dtFreeze.Rows[0]["SaveDate"]);
                endDate = startDate.AddDays(10);
            }

            DateTime FirstDate = startDate;
            while (FirstDate <= endDate)
            {
                string label = string.Format("{0:MM月dd日}", FirstDate);
                string prop = string.Format("{0:yyyy-MM-dd}", FirstDate);
                dt.Columns.Add(prop, typeof(decimal));
                ElementColumn[0].Add(new ElementTableOuput() { label = label, prop = prop, prop2 = prop, width = "80", isEdit = false, ControlType = "textbox" });

                FirstDate = FirstDate.AddDays(1);
            }

            DateTime minQueryDate = startDate;
            DateTime maxQueryDate = endDate;

            DtDetail = SqlHelper.ExecuteDataTable(string.Format(@"
SELECT A.ProcessPlanID,A.PlanDay,A.PlanQty 
FROM   [APS_DayPlanHistory] A INNER JOIN APS_ProcessPlanHistory B ON A.ProcessPlanID = B.ProcessPlanID and a.savedate=b.savedate
INNER JOIN Dev_Organize C ON B.LineID = C.OrganizeID
WHERE  1=1
AND C.OrganizeName NOT LIKE '%SMT%'
AND A.PlanDay >= '{0}'
AND A.PlanDay <= '{1}'
", minQueryDate.ToString("yyyy-MM-dd"), maxQueryDate.ToString("yyyy-MM-dd")));

            DtDetail1 = SqlHelper.ExecuteDataTable(string.Format(@"
SELECT A.ProcessPlanID,A.ProductionDate AS PlanDay,A.ReportQty AS PlanQty,A.Years,A.Months,A.Weeks
FROM   V_APS_Report29 A INNER JOIN APS_ProcessPlan B ON A.ProcessPlanID = B.ProcessPlanID
INNER JOIN Dev_Organize C ON B.LineID = C.OrganizeID
WHERE  1=1
AND C.OrganizeName NOT LIKE '%SMT%'
AND A.ProductionDate >= '{0}'
AND A.ProductionDate <= '{1}'
", minQueryDate.ToString("yyyy-MM-dd"), maxQueryDate.ToString("yyyy-MM-dd")));

            dt.Columns.Add("BColors", typeof(Dictionary<string, string>));

            foreach (DataRow dataRow in dt.Rows)
            {
                string ProcessPlanID = dataRow["ProcessPlanID"].ToString();
                string DataType = dataRow["DataType"].ToString();
                Dictionary<string, string> bColors = new Dictionary<string, string>();

                DateTime SecondDate = startDate;
                while (SecondDate <= endDate)
                {
                    string prop = string.Format("{0:yyyy-MM-dd}", SecondDate);
                    decimal Qty = 0;

                    if (DataType == "预计")
                    {
                        DataRow[] dataRows = DtDetail.Select("PlanDay = '" + prop + "' AND ProcessPlanID = '" + ProcessPlanID + "'");
                        if (dataRows.Length > 0)
                        {
                            decimal.TryParse(dataRows[0]["PlanQty"].ToString(), out Qty);
                        }
                    }
                    else if (DataType == "实际")
                    {
                        DataRow[] dataRows = DtDetail1.Select("PlanDay = '" + prop + "' AND ProcessPlanID = '" + ProcessPlanID + "'");
                        if (dataRows.Length > 0)
                        {
                            decimal.TryParse(dataRows[0]["PlanQty"].ToString(), out Qty);
                        }
                    }
                    if (Qty > 0) dataRow[prop] = Qty;
                    SecondDate = SecondDate.AddDays(1);
                }
                dataRow["BColors"] = bColors;
            }
        }

        // 获取周次（严格匹配：1.1→第一周，周一~周日为完整周）
        private int DatePartWeek(DateTime dt)
        {
            DateTime firstDayOfYear = new DateTime(dt.Year, 1, 1);

            // 第一步：算出当年第一个周日
            DateTime firstSunday = firstDayOfYear;
            while (firstSunday.DayOfWeek != DayOfWeek.Sunday)
                firstSunday = firstSunday.AddDays(1);

            // 日期在第一周范围内
            if (dt <= firstSunday)
                return 1;

            // 超过第一周，按周一~周日计算周次
            TimeSpan diff = dt - firstSunday.AddDays(1);
            return 1 + (diff.Days / 7) + 1;
        }

        // 根据 年+周 获取开始/结束日期（你要的最终正确逻辑）
        private (DateTime startDate, DateTime endDate) GetWeekRange(int year, int weekNum)
        {
            DateTime firstDayOfYear = new DateTime(year, 1, 1);
            DateTime firstSunday = firstDayOfYear;

            // 找到当年第一个周日
            while (firstSunday.DayOfWeek != DayOfWeek.Sunday)
                firstSunday = firstSunday.AddDays(1);

            // ==========================================
            // 第1周：1月1日 ~ 第一个周日
            // ==========================================
            if (weekNum == 1)
            {
                return (firstDayOfYear, firstSunday);
            }
            // ==========================================
            // 第2周及以后：周一 ~ 周日，紧接上一周
            // ==========================================
            else
            {
                DateTime startDate = firstSunday.AddDays(1); // 第一周结束+1天 = 第二周周一
                startDate = startDate.AddDays((weekNum - 2) * 7); // 往后跳整周
                DateTime endDate = startDate.AddDays(6);

                return (startDate, endDate);
            }
        }
        public string RequestSSOUrl(string loginid, long timestamp, string token, long EFTime, string gopage = "")
        {
            try
            {
                // 调用独立的校验方法验证请求参数
                var (isValid, errors, dt) = ValidateRequestParameters(loginid, timestamp, token, EFTime);
                if (!isValid)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        iserror = errors.Count,
                        errormsg = string.Join("; ", errors),
                        ssourl = ""
                    });
                }
                string localToken = "";
                string[] parts = AppInfo.SSOUrl.Split('‖').Select(p => p.Trim()).ToArray();
                //0项目，1地址
                string projectName = parts[0];
                string projectUrl = parts[1];
                if (projectName == "EK")
                {
                    // EK AES加密
                    // 定义AES密钥（和EK约定好的密钥）
                    // string AESKey = "EkIT2007Y123321Y";
                    //EK‖http://192.168.1.166‖EkIT2007Y123321Y
                    string AESKey = parts[2];
                    // 根据传入参数生成本地 token
                    localToken = EncryptToken($"ek_{loginid}|{timestamp}_sso", AESKey);
                }
                else if (projectName == "XingHe")
                {
                    // 星河 MD5 加密
                    //XingHe‖http://192.168.1.231‖tJ7qE9sA2fDgHkLzP6bN4cV1mR8uY0w
                    string XINGHE_SALT = parts[2];//"tJ7qE9sA2fDgHkLzP6bN4cV1mR8uY0w";
                    string signStr = $"{loginid}|{timestamp}|{XINGHE_SALT}";
                    localToken = MD5Encrypt(signStr);
                }


                // 验证 token 是否一致
                if (localToken != token)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        iserror = "1",
                        errormsg = "token 不一致",
                        ssourl = ""
                    });
                }
                token = System.Net.WebUtility.UrlEncode(token);
                // 计算免登录地址的过期时间戳
                long expirationTimestamp = timestamp + (EFTime * 60); // 当前时间戳 + 有效时间（秒）

                // 构建 ssourl，将过期时间戳包含在地址中
                string ssourl = projectUrl + $"/redirect?loginid={loginid}&timestamp={timestamp}&token={token}&EFTime={EFTime}&expiration={expirationTimestamp}&gopage={gopage}";

                // 返回成功结果
                return JsonConvert.SerializeObject(new
                {
                    iserror = "0",
                    errormsg = "",
                    ssourl = ssourl
                });
            }
            catch (Exception ex)
            {
                // 捕获异常并返回错误信息
                return JsonConvert.SerializeObject(new
                {
                    iserror = "1",
                    errormsg = ex.Message,
                    ssourl = ""
                });
            }
        }
        /// <summary>
        /// 接收前端发送的SSO请求参数，并验证其合法性
        /// </summary>
        /// <returns>验证结果</returns>
        public string APSRequestSSOUrl()
        {

            try
            {
                JObject jObject = JsonConvert.DeserializeObject(BodyJson) as JObject;
                if (jObject == null || jObject.Count == 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = "未接受到数据，请确认是否为JSON格式"
                    });
                }

                string loginid = jObject["loginid"]?.ToString() ?? string.Empty;
                long timestamp = jObject["timestamp"] != null ? Convert.ToInt64(jObject["timestamp"]) : 0;
                string token = jObject["token"]?.ToString() ?? string.Empty;
                long EFTime = jObject["EFTime"] != null ? Convert.ToInt64(jObject["EFTime"]) : 0;
                long expiration = jObject["expiration"] != null ? Convert.ToInt64(jObject["expiration"]) : 0;
                string gopage = jObject["gopage"]?.ToString() ?? string.Empty;

                // 调用独立的校验方法验证请求参数
                var (isValid, errors, dt) = ValidateRequestParameters(loginid, timestamp, token, EFTime);
                if (!isValid)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = string.Join("; ", errors)
                    });
                }

                // 获取当前时间戳（秒级）
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 校验 expiration 是否过期，允许 5 分钟误差
                if (expiration + 300 <= currentTimestamp)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = "链接已过期"
                    });
                }
                string localToken = "";
                string[] parts = AppInfo.SSOUrl.Split('‖').Select(p => p.Trim()).ToArray();
                //0项目，1地址
                string projectName = parts[0];
                string projectUrl = parts[1];
                if (projectName == "EK")
                {
                    // EK AES加密
                    // 定义AES密钥（和EK约定好的密钥）
                    // string AESKey = "EkIT2007Y123321Y";
                    //EK‖http://192.168.1.166‖EkIT2007Y123321Y
                    string AESKey = parts[2];
                    // 根据传入参数生成本地 token
                    localToken = EncryptToken($"ek_{loginid}|{timestamp}_sso", AESKey);
                }
                else if (projectName == "XingHe")
                {
                    // 星河 MD5 加密
                    //XingHe‖http://192.168.1.231‖tJ7qE9sA2fDgHkLzP6bN4cV1mR8uY0w
                    string XINGHE_SALT = parts[2];//"tJ7qE9sA2fDgHkLzP6bN4cV1mR8uY0w";
                    string signStr = $"{loginid}|{timestamp}|{XINGHE_SALT}";
                    localToken = MD5Encrypt(signStr);
                }

                // 验证 token 是否一致
                if (localToken != token)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = "token 不一致"
                    });
                }

                // 验证成功后返回确认信息
                if (dt.Rows.Count > 0) // 确保查询有结果
                {
                    string account = dt.Rows[0]["Account"].ToString(); // 获取账号
                    string password = dt.Rows[0]["Pwd"].ToString();    // 获取密码
                                                                       // string encryptedPwd = StringHelper.DESEncrypt(password); // 对密码进行加密

                    return JsonConvert.SerializeObject(new
                    {
                        result = true,
                        msg = "验证通过",
                        Account = account,
                        Pwd = password // 返回密码
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        result = false,
                        msg = "账号不存在"
                    });
                }
            }
            catch (Exception ex)
            {
                // 捕获异常并返回错误信息
                return JsonConvert.SerializeObject(new
                {
                    result = false,
                    msg = ex.Message
                });
            }
        }
        /// <summary>
        /// 校验请求参数
        /// </summary>
        /// <param name="loginid">用户登录ID</param>
        /// <param name="timestamp">时间戳</param>
        /// <param name="token">请求token</param>
        /// <param name="EFTime">免登录地址过期时间</param>
        /// <returns>校验结果和错误信息列表</returns>
        private (bool isValid, List<string> errors, DataTable dt) ValidateRequestParameters(string loginid, long timestamp, string token, long EFTime)
        {
            var errors = new List<string>();
            DataTable dt = new DataTable(); // 初始化为空的 DataTable

            // 验证 loginid 是否为空
            if (string.IsNullOrEmpty(loginid))
            {
                errors.Add("loginid 不能为空");
            }
            else
            {
                // 查询数据库，判断 loginid 是否存在
                dt = SqlHelper.ExecuteDataTable($@"
             SELECT A.Account, A.Pwd FROM Dev_Account A
             WHERE Account = '{loginid}'");

                if (dt.Rows.Count == 0)
                {
                    errors.Add($"不存在 {loginid} 工号");
                }
            }

            // 验证其他参数
            if (timestamp <= 0) errors.Add("timestamp 不能为空或无效");
            if (string.IsNullOrEmpty(token)) errors.Add("token 不能为空");
            if (EFTime <= 0) errors.Add("EFTime 不能为空或无效");

            // 返回校验结果
            return (errors.Count == 0, errors, dt);
        }

        /// <summary>
        /// AES 加密方法
        /// </summary>
        /// <param name="data">需要加密的内容</param>
        /// <param name="key">AES 密钥</param>
        /// <returns>加密后的 Base64 字符串</returns>
        public string EncryptToken(string data, string key)
        {

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.Mode = CipherMode.ECB; // 使用ECB模式
                    aes.Padding = PaddingMode.PKCS7; // 使用PKCS7填充

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] inputBytes = Encoding.UTF8.GetBytes(data);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                        return Convert.ToBase64String(encryptedBytes); // 返回Base64加密结果
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("加密失败", ex);
            }
        }
        /// <summary>
        /// AES 解密方法
        /// </summary>
        /// <param name="data">需要加密的内容</param>
        /// <param name="key">AES 密钥</param>
        /// <returns>加密后的 string 字符串</returns>
        public string DecryptToken(string data, string key)
        {
            try
            {
                data = data.Replace(" ", "+"); // 还原 + 字符
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(data);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("解密失败", ex);
            }
        }
        /// <summary>
        /// MD5 加密方法
        /// </summary>
        /// <param name="data">需要加密的内容</param>
        /// <returns>加密后的 string 字符串</returns>      
        public string MD5Encrypt(string data)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hashBytes = md5.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

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

