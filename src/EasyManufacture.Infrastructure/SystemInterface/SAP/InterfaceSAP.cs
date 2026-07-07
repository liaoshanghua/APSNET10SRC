using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP.Middleware.Connector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EasyManufacture.Infrastructure.SystemInterface.SAP
{
    public static class InterfaceSAP
    {
        static Licence.SystemLog systemLog = new Licence.SystemLog();
        static RfcDestination destination = null;
        static RfcRepository repository = null;
        static bool isStart = false;

        public static void Start(bool isRunSplit = false)
        {

            try
            {

                destination = RfcDestinationManager.GetDestination("Conn");

                repository = destination.Repository;


            }
            catch (Exception ex)
            {
                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);
            }
            if (isRunSplit)
            {
                try
                {
                    if (AppInfo.PushType != "")
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "获取ERP数据触发", null, null);
                        DataTable dataTable = SqlHelper.ExecuteDataTable(@"SELECT  [FID]
      ,[InterfaceName]
      ,[InterfaceDescription]
      ,APIUrl,SyncDatetime
  FROM  [dbo].[APS_InterfaceSAP]
    where [status]=1 ");
                        // systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "获取ERP数据触发", null, null);
                        if (dataTable.Rows.Count > 0)
                        {
                          
                            timer = new System.Timers.Timer(Math.Round(1000 * 60 * AppInfo.ERPSyncCycle, 0));
                            timer.Elapsed += Timer_Elapsed;
                            timer.Start();
                            timer.AutoReset = true;
                            Timer_Elapsed(null, null);
                        }

                    }
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);
                }


            }
        }


        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {



            Invoke();

        }


        static System.Timers.Timer timer = null;
        //输入参数明细
        static DataTable dataTable5 = null;
        static DataTable dtDelete = null;
        /// <summary>
        /// 执行
        /// </summary>
        /// <returns></returns>
        public async static void Invoke()
        {
#pragma warning disable SA1101 // Prefix local calls with this
            Task.Run(() =>
            {
                //  if (IsRuning == false)
                {


                    try
                    {




                        DataTable dataTable1 = SqlHelper.ExecuteDataTable(@"
SELECT X.*
FROM   [APS_InterfaceSAP] X
WHERE  EXISTS (SELECT *
               FROM   (SELECT *
                       FROM   (SELECT ERPSyncCycle,
                                      ThreadGroup,
                                      Max(SyncDatetime) AS SyncDatetime
                               FROM   [dbo].[APS_InterfaceSAP]
                               WHERE  [status] = 1
                                    AND  
								( StartTime IS NULL  OR CAST(StartTime AS TIME)<=CAST(GETDATE() AS TIME) )
								 AND (
								  EndTime IS NULL  OR CAST(EndTime AS TIME)>=CAST(GETDATE() AS TIME) 
								 )
                             
                               GROUP  BY ERPSyncCycle,
                                         ThreadGroup) A2
                       WHERE  ( Dateadd(MINUTE, Isnull(ERPSyncCycle, 0), SyncDatetime) < Getdate()
                                 OR SyncDatetime IS NULL )

								 
                    -- and FID=4
                      ) A
               WHERE    X.ERPSyncCycle = A.ERPSyncCycle
                      AND X.ThreadGroup = A.ThreadGroup)
       AND Status = 1
      AND  
								( StartTime IS NULL  OR CAST(StartTime AS TIME)<=CAST(GETDATE() AS TIME) )
								 AND (
								  EndTime IS NULL  OR CAST(EndTime AS TIME)>=CAST(GETDATE() AS TIME) 
								 )
                             

ORDER  BY FID 



");
                        //输入参数
                        DataTable dataTable2 = SqlHelper.ExecuteDataTable(@"
SELECT *
  FROM  [dbo].[APS_InterfaceSAPInput]
  where status=1 --AND DefaultValue<>''
  and (StartHour is null or (datepart(hour,getdate())>=StartHour and datepart(hour,getdate())<EndHour))
order by DataSql
");
                        //输出参数主表
                        DataTable dataTable3 = SqlHelper.ExecuteDataTable(@"
SELECT  *
  FROM  [dbo].[APS_InterfaceSAPOutput]
where status=1
");
                        //输出参数字段表
                        DataTable dataTable4 = SqlHelper.ExecuteDataTable(@"
SELECT *
  FROM  [dbo].[APS_InterfaceSAPOutputField]
  where status=1 and [FNameAPS]<>''
order by EID
");
                        //输入参数明细
                        dataTable5 = SqlHelper.ExecuteDataTable(@"
SELECT  [ParameteID]
      ,[EID]
      ,[FName]
      ,[FNameCaption]
      ,[DefaultValue]
      ,[FormatValue]
      ,[DataSql]
  ,[DataType]
  FROM  [dbo].[APS_InterfaceSAPInputParameter]
  where status=1 
");
                        dtDelete = SqlHelper.ExecuteDataTable(@"SELECT a.FNameAPS,B.APSTableName,B.APSTableNameTemp

,'DELETE B FROM '+B.APSTableName+' B LEFT JOIN '+B.APSTableNameTemp +' A ON ' AS T
,B.FID,b.DeleteCondition
FROM [dbo].[APS_InterfaceSAPOutputField] a
inner join APS_InterfaceSAPOutput b on a.EID=b.EID
WHERE  a.MappingFields=1 and 

FID IN (
SELECT FID
FROM  [dbo].[APS_InterfaceSAPInput]
WHERE datepart(hour,getdate())>=StartHour and datepart(hour,getdate())<EndHour
 AND ISALL=1 and status=1
)    and a.Status=1 and b.status=1");
                        if (lstRuningInterFace != null)
                        {

                        }


                        //判断是否已经运行
                        List<String> list = new List<String>();
                        foreach (DataRow dataRow in dataTable1.Rows)
                        {
                            //按分组判断
                            string ThreadGroup = dataRow["ThreadGroup"].ToString();
                            if (lstRuningInterFace.ContainsKey(ThreadGroup) == false && list.Contains(ThreadGroup) == false)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "分组" + ThreadGroup + "接口执行开始,本次执行共" + dataTable1.Select("ThreadGroup='" + ThreadGroup + "'").Length + "个接口", null, null);
                                list.Add(ThreadGroup);
                            }
                            else if (lstRuningInterFace.ContainsKey(ThreadGroup) == true)
                            {
                                // 使用线程安全的方式访问 ConcurrentDictionary
                                List<RuningInterFace> threadList;
                                if (lstRuningInterFace.TryGetValue(ThreadGroup, out threadList) && threadList != null)
                                {
                                    lock (threadList)
                                    {
                                        if (threadList.Count == 1 && threadList[0].RuningCount == 1 && threadList[0].ThreadRuningCount == 0 && threadList[0].ThreadCount > 3 && (DateTime.Now - threadList[0].LastModifyedTime).TotalMinutes > 3)
                                        {//防止异常占用
                                            // 使用线程安全的 TryRemove 方法
                                            List<RuningInterFace> removedList;
                                            lstRuningInterFace.TryRemove(ThreadGroup, out removedList);
                                        }
                                        else
                                        {
                                            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "分组" + ThreadGroup + "已经执行开始,剩余:" + threadList.Where(m => m.IsRuning == true).ToJsonLegacy(), null, null);
                                        }
                                    }
                                }

                            }
                        }
                        int index = 0;



                        foreach (DataRow row in dataTable1.Rows)
                        {
                            try
                            {
                                string InterfaceName = row["InterfaceName"].ToString().Trim();

                                string FID = row["FID"].ToString();

                                string InterfaceDescription = row["InterfaceDescription"].ToString();
                                string ThreadGroup = row["ThreadGroup"].ToString();
                                if (list.Contains(ThreadGroup) == false)
                                {//当前的分组没有执行完，则不执行
                                    continue;
                                }
                                int ThreadCount = 0;
                                int.TryParse(row["ThreadCount"].ToString(), out ThreadCount);
                                if (ThreadCount == 0)
                                {
                                    ThreadCount = 1;
                                }
                                // 使用线程安全的 GetOrAdd 方法获取或创建 List
                                List<RuningInterFace> threadList = lstRuningInterFace.GetOrAdd(ThreadGroup, key => new List<RuningInterFace>());
                                // 注意：List<RuningInterFace> 本身不是线程安全的，需要加锁
                                lock (threadList)
                                {
                                    var tmp = threadList.Where(m => FID == m.FID).FirstOrDefault();
                                    if (tmp != null)
                                    {
                                        continue;
                                    }
                                    threadList.Add(new RuningInterFace() { InterfaceDescription = InterfaceDescription, ThreadCount = ThreadCount, InterfaceName = InterfaceName, FID = FID });
                                }

                                DateTime dateTime = DateTime.Now;
                                if (!string.IsNullOrEmpty(row["APIUrl"].ToString()))
                                {
                                    string g1 = ThreadGroup;
                                    Task.Run(() => GetWebAPI(dataTable2, dataTable3, dataTable4, row, InterfaceName, InterfaceDescription, g1, FID)).ContinueWith(t2 => LivenessTask(t2, InterfaceDescription, dateTime, FID, g1));
                                }
                                else
                                {
                                    string g1 = ThreadGroup;
                                    Task.Run(() => GetSap(dataTable2, dataTable3, dataTable4, row, InterfaceName, InterfaceDescription, dateTime, FID, g1)).ContinueWith(t2 => LivenessTask(t2, InterfaceDescription, dateTime, FID, g1));
                                }


                            }
                            catch (Exception ex)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);
                            }
                            index++;
                            // systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "接口循环过程，" + index + "/" + count + "," + DateTime.Now, null, null);

                        }

                        //  systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "接口执行完成，" + DateTime.Now, null, null);
                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);

                    }

                }
            });
#pragma warning restore SA1101 // Prefix local calls with this

            // return result;
        }
        public enum DataType
        {
            Headers, Body, Params
        }
        public class KeyValueObj
        {

            public string Value { get; set; }
            /// <summary>
            /// 是否需要递归循环
            /// </summary>
            public bool IsCycle { get; set; }
            /// <summary>
            /// 循环的字段名
            /// </summary>
            public string CycleFieldName { get; set; }
            public DateTime CreatedTime { get; set; }
            /// <summary>
            /// 每次跳过多少行，页码*每次调过，为空默认1
            /// </summary>
            public int SkipRows { get; set; } = 1;
        }
        /// <summary>
        /// 定义正在跑的接口线程
        /// </summary>
        public class RuningInterFace
        {
            public RuningInterFace()
            {
                this.RuningCount = 1;
                this.ThreadCount = 0;
                this.ThreadRuningCount = 0;
                this.Function = null;
            }
            /// <summary>
            /// 接口名称描述
            /// </summary>
            public string InterfaceDescription
            {
                get; set;
            }
            /// <summary>
            /// 接口描述
            /// </summary>
            public string InterfaceName
            {
                get; set;
            }
            /// <summary>
            /// 总共在跑数量
            /// </summary>
            public int RuningCount
            {
                get; set;
            }
            /// <summary>
            /// 记录数
            /// </summary>
            public int DataCount
            {
                get; set;
            }
            /// <summary>
            /// 可跑的线程数
            /// </summary>
            public int ThreadCount
            {
                get; set;
            }
            /// <summary>
            /// 在跑的线程数
            /// </summary>
            public int ThreadRuningCount
            {
                get; set;
            }
            /// <summary>
            /// 当前状态的描述
            /// </summary>
            public string Description
            {
                get; set;
            }
            [JsonIgnore]
            public IRfcFunction Function { get; set; }
            public DateTime StartDate { get; set; } = DateTime.Now;
            public DateTime EndDate { get; set; } = DateTime.Now;
            public double Spents
            {
                get
                {
                    return (EndDate - StartDate).TotalSeconds;
                }
            }
            public bool IsRuning
            {
                get; set;
            } = true;
            public DateTime LastModifyedTime
            {
                get; set;
            } = DateTime.Now;
            public string FID
            {
                get; set;
            }
            public string ErrorMsg
            {
                get; set;
            }
        }
        /// <summary>
        /// 记录当前在跑的接口（使用 ConcurrentDictionary 保证线程安全）
        /// </summary>
        static ConcurrentDictionary<string, List<RuningInterFace>> lstRuningInterFace = new ConcurrentDictionary<string, List<RuningInterFace>>();
        //记录token（使用 ConcurrentDictionary 保证线程安全）
        static Dictionary<string, KeyValueObj> token = new Dictionary<string, KeyValueObj>();
        /// <summary>
        /// 获取常规的WebAPI
        /// </summary>
        /// <param name="dataTable2">入参表</param>
        /// <param name="dataTable3">输出表</param>
        /// <param name="dataTable4">输出参数表</param>
        /// <param name="row">当前接口行</param>
        /// <param name="InterfaceName">接口名称</param>
        /// <param name="InterfaceDescription">接口描述</param>
        /// <param name="ThreadGroup">接口分组</param>
        private static async void GetWebAPI(DataTable dataTable2, DataTable dataTable3, DataTable dataTable4, DataRow row, string InterfaceName, string InterfaceDescription, string ThreadGroup, string FID)
        {
            // 使用线程安全的方式访问 ConcurrentDictionary
            List<RuningInterFace> threadList;
            if (!lstRuningInterFace.TryGetValue(ThreadGroup, out threadList) || threadList == null)
            {
                return;
            }
            RuningInterFace thisRuningThread;
            lock (threadList)
            {
                thisRuningThread = threadList.Where(m => m.FID == FID).FirstOrDefault();
            }
            if (thisRuningThread == null)
            {
                return;
            }
            thisRuningThread.DataCount = 0;
            try
            {

                DataRow[] dataRows = dataTable2.Select("FID=" + row["FID"], "DataSql ");
                //是否要是一次查询
                bool isTodo = true;
                ///传入参数集合
                Dictionary<string, KeyValueObj> keyValueHeader = new Dictionary<string, KeyValueObj>();
                Dictionary<string, KeyValueObj> keyValueBody = new Dictionary<string, KeyValueObj>();
                Dictionary<string, KeyValueObj> keyValueParams = new Dictionary<string, KeyValueObj>();
                Dictionary<string, object> jsonBody = new Dictionary<string, object>();
                string bodyString = "";
                DataTable dtDataSource = null;//数据源
                List<string> listField = new List<string>();//对应的接口字段
                foreach (DataRow row1 in dataRows)
                {
                    string DefaultValue = row1["DefaultValue"].ToString();
                    string FormatValue = row1["FormatValue"].ToString();
                    string FName = row1["FName"].ToString();
                    listField.Add(FName);
                    string DataSql = row1["DataSql"].ToString();
                    string Url = row1["Url"].ToString();//参数来源地址
                    string ContentType = row1["ContentType"].ToString();
                    //if (string.IsNullOrEmpty(DefaultValue) && string.IsNullOrEmpty(DataSql) && string.IsNullOrEmpty(Url))
                    //{//没有默认值，没有数据源，跳过
                    //    continue;
                    //}
                    if (!string.IsNullOrEmpty(DataSql))
                    {//有数据源，要遍历,代码未判断未Headers，没业务场景，暂不考虑
                        //isTodo = false;
                        dtDataSource = SqlHelper.ExecuteDataTable(DataSql);
                        //bool isDelete = true;
                        //foreach (DataRow row2 in dataSource.Rows)
                        //{
                        //    try
                        //    {
                        //        if (dataSource.Columns.Count == 1)
                        //        {//只有一列，直接绑定
                        //            if (keyValueHeader.ContainsKey(FName) == false)
                        //            {
                        //                keyValueHeader.Add(FName, row2[0].ToString());
                        //            }
                        //            else
                        //            {
                        //                keyValueHeader[FName] = row2[0].ToString();
                        //            }

                        //        }
                        //        else
                        //        {
                        //            foreach (DataColumn dataColumn in dataSource.Columns)
                        //            {
                        //                if (keyValueHeader.ContainsKey(dataColumn.ColumnName) == false)
                        //                {
                        //                    keyValueHeader.Add(dataColumn.ColumnName, row2[dataColumn.ColumnName].ToString());
                        //                }
                        //                else
                        //                {
                        //                    keyValueHeader[dataColumn.ColumnName] = row2[dataColumn.ColumnName].ToString();
                        //                }

                        //            }

                        //        }



                        //        if (isDelete)
                        //        {
                        //            await WebAPIInvoke(dataTable3, dataTable4, row, InterfaceDescription, row1, isDelete, keyValueHeader);
                        //        }
                        //        else
                        //        {
                        //            WebAPIInvoke(dataTable3, dataTable4, row, InterfaceDescription, row1, isDelete, keyValueHeader);
                        //        }

                        //        isDelete = false;
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescription + "值:" + row2[0].ToString() + "," + ex.Message, null, null);
                        //    }

                        //}
                    }
                     if (string.IsNullOrEmpty(Url))
                    {//这个参数不是通过接口返回的

                        DataRow[] rowsParameter = dataTable5.Select("EID=" + row1["EID"]);
                        if (rowsParameter.Length > 0)
                        {///有子参数
                            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                            foreach (DataRow row2 in rowsParameter)
                            {
                                DefaultValue = row2["DefaultValue"].ToString();
                                FormatValue = row2["FormatValue"].ToString();
                                if (DefaultValue.IndexOf("DAY") > -1)
                                {
                                    DefaultValue = DefaultValue.Replace("DAY", "");
                                    if (!string.IsNullOrEmpty(FormatValue))
                                    {
                                        DefaultValue = string.Format(FormatValue, DateTime.Now.AddDays(double.Parse(DefaultValue)));
                                    }
                                    else
                                    {
                                        DefaultValue = DateTime.Now.AddDays(double.Parse(DefaultValue)).ToString();
                                    }
                                }
                                keyValuePairs.Add(row2["FName"].ToString(), DefaultValue);
                            }
                            if (ContentType == "application/json")
                            {
                                jsonBody.Add(FName, keyValuePairs);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(FName))
                            {


                                KeyValueObj key = new KeyValueObj();
                                if (DefaultValue.IndexOf("DAY") > -1)
                                {
                                    DefaultValue = DefaultValue.Replace("DAY", "");
                                    if (!string.IsNullOrEmpty(FormatValue))
                                    {
                                        DefaultValue = string.Format(FormatValue, DateTime.Now.AddDays(double.Parse(DefaultValue)));
                                    }
                                    else
                                    {
                                        DefaultValue = DateTime.Now.AddDays(double.Parse(DefaultValue)).ToString();
                                    }
                                }
                                key.Value = DefaultValue;
                                if (row1["IsCycle"].ToString().ToLower() == "true")
                                {
                                    key.IsCycle = true;
                                    key.CycleFieldName = row1["CycleFieldName"].ToString();
                                    key.SkipRows = string.IsNullOrEmpty(row1["SkipRows"].ToString())?1: (int)row1["SkipRows"];

                                }
                                if (row1["DataType"].ToString() == DataType.Headers.ToString())
                                {
                                    if (keyValueHeader.ContainsKey(FName) == false)
                                    {
                                        keyValueHeader.Add(FName, key);
                                    }
                                }
                                else if (row1["DataType"].ToString() == DataType.Body.ToString())
                                {
                                    if (keyValueBody.ContainsKey(FName) == false)
                                    {
                                        keyValueBody.Add(FName, key);
                                        if (ContentType == "application/json")
                                        {
                                            jsonBody.Add(FName, ToJsonObjectOrString(ReplaceDatePlaceholders(key.Value)));
                                        }
                                    }
                                }
                                else if (row1["DataType"].ToString() == DataType.Params.ToString())
                                {
                                    if (keyValueParams.ContainsKey(FName) == false)
                                    {
                                        keyValueParams.Add(FName, key);
                                    }
                                }
                            }
                            if (string.IsNullOrEmpty(FName) && !string.IsNullOrEmpty(DefaultValue))
                            {
                                bodyString = DefaultValue;
                            }

                            // fun003.SetValue(FName, keyValuePairs[FName]);//指定值
                        }

                    }
                    else if (!string.IsNullOrEmpty(Url))
                    {//参数来源是取某个链接的token
                        // 使用 ConcurrentDictionary，不需要 lock
                        lock (token)
                        {
                            if (token.ContainsKey(Url) == false || token.Where(m => m.Key == Url && m.Value.CreatedTime.AddMinutes(20) > DateTime.Now).Count() == 0)
                            {
                               // lock (token)
                                {
                                    if (token != null && token.ContainsKey(Url))
                                    {
                                        token.Remove(Url);
                                    }
                                    Encoding encoding = Encoding.UTF8;
                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                                    request.Method = row1["HttpMethod"].ToString() == "" ? "POST" : row1["HttpMethod"].ToString();
                                    request.ContentType = row1["ContentType"].ToString();
                                    request.Timeout = 1000 * 30;
                                    // request.Headers = new WebHeaderCollection();
                                    foreach (DataRow row2 in dataTable5.Select("[DataType]='Headers' and [EID]=" + row1["EID"].ToString()))
                                    {

                                        request.Headers.Add(row2["FName"].ToString(), row2["DefaultValue"].ToString());

                                    }

                                    //Dictionary<string, string> body = new Dictionary<string, string>();
                                    string body = "a=1";
                                    foreach (DataRow row2 in dataTable5.Select("[DataType]='Body' and [EID]=" + row1["EID"].ToString()))
                                    {
                                        DefaultValue = row2["DefaultValue"].ToString();
                                        if (DefaultValue.IndexOf("DAY") > -1)
                                        {
                                            DefaultValue = DefaultValue.Replace("DAY", "");
                                            FormatValue = row2["FormatValue"].ToString();
                                            if (!string.IsNullOrEmpty(FormatValue))
                                            {
                                                DefaultValue = string.Format(FormatValue, DateTime.Now.AddDays(double.Parse(DefaultValue)));
                                            }
                                            else
                                            {
                                                DefaultValue = DateTime.Now.AddDays(double.Parse(DefaultValue)).ToString();
                                            }
                                        }
                                        body += "&" + row2["FName"].ToString() + "=" + DefaultValue;
                                        DefaultValue = "";

                                    }


                                    var dict = ParseQueryStringToDictionary(body);
                                    string json = JsonConvert.SerializeObject(dict);
                                    if (ContentType == "application/json")
                                    {
                                        body = json;
                                    }
                                    //  string s = JsonConvert.SerializeObject(body);
                                    byte[] buffer = null;
                                    if (!string.IsNullOrEmpty(DefaultValue))
                                    {
                                        buffer = encoding.GetBytes(DefaultValue);
                                    }
                                    else
                                    {
                                        buffer = encoding.GetBytes(body);
                                    }

                                    request.ContentLength = buffer.Length;

                                    Stream requestStream = null;
                                    HttpWebResponse response = null;
                                    string res = "";
                                    try
                                    {
                                        // 获取请求流并写入数据
                                        requestStream = request.GetRequestStream();
                                        requestStream.Write(buffer, 0, buffer.Length);
                                        requestStream.Close(); // 关闭请求流

                                        // 获取响应
                                        response = (HttpWebResponse)request.GetResponse();
                                        // lock (token)
                                        {
                                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                            {

                                                res = reader.ReadToEnd();
                                                JObject jObject = JsonConvert.DeserializeObject(res) as JObject;
                                                string outputParameterName = row1["OutputParameterName"].ToString();
                                                if (outputParameterName.IndexOf(".") > -1)
                                                {
                                                    DefaultValue = jObject[outputParameterName.Split('.')[0]][outputParameterName.Split('.')[1]].ToString();
                                                }
                                                else
                                                {
                                                    DefaultValue = jObject[row1["OutputParameterName"].ToString()].ToString();
                                                }

                                                KeyValueObj key = new KeyValueObj();
                                                key.Value = DefaultValue;
                                                key.CreatedTime = DateTime.Now;

                                                // 使用线程安全的 TryAdd 方法

                                                token.Add(Url, key);


                                                if (row1["DataType"].ToString() == "Headers")
                                                {
                                                    if (keyValueHeader.ContainsKey(FName) == false)
                                                    {
                                                        keyValueHeader.Add(FName, key);
                                                    }
                                                }
                                                else
                                                {
                                                    if (keyValueBody.ContainsKey(FName) == false)
                                                    {
                                                        keyValueBody.Add(FName, key);
                                                        if (ContentType == "application/json")
                                                        {
                                                            jsonBody.Add(FName, key.Value);
                                                        }
                                                    }
                                                }
                                                //  keyValueHeader.Add(FName, DefaultValue);

                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        //可能对方的token没有过期，直接报错返回。
                                        // 使用线程安全的 TryGetValue 方法
                                        KeyValueObj tokenValue;
                                        if (token.TryGetValue(Url, out tokenValue))
                                        {
                                            DefaultValue = tokenValue.Value;
                                        }
                                        else
                                        {
                                            DefaultValue = "";
                                        }
                                        KeyValueObj key = new KeyValueObj();
                                        key.Value = DefaultValue;
                                        // if (token.ContainsKey(FName) == false)

                                        if (row1["DataType"].ToString() == "Headers")
                                        {
                                            if (keyValueHeader.ContainsKey(FName) == false)
                                            {
                                                keyValueHeader.Add(FName, key);
                                            }
                                        }
                                        else
                                        {
                                            if (keyValueBody.ContainsKey(FName) == false)
                                            {
                                                keyValueBody.Add(FName, key);
                                                if (ContentType == "application/json")
                                                {
                                                    jsonBody.Add(FName, key.Value);
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        // 确保资源被释放（兼容 .NET Framework 4.8）
                                        if (requestStream != null)
                                        {
                                            requestStream.Dispose();
                                        }
                                        if (response != null)
                                        {
                                            response.Close();
                                            response.Dispose();
                                        }
                                        if (request != null)
                                        {
                                            request.Abort(); // .NET 4.8 中 WebRequest 不实现 IDisposable，使用 Abort()
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // 使用线程安全的 TryGetValue 方法
                                KeyValueObj tokenValue;
                                if (token.TryGetValue(Url, out tokenValue))
                                {
                                    DefaultValue = tokenValue.Value;
                                }
                                else
                                {
                                    DefaultValue = "";
                                }
                                KeyValueObj key = new KeyValueObj();
                                key.Value = DefaultValue;
                                // if (token.ContainsKey(FName) == false)

                                if (row1["DataType"].ToString() == "Headers")
                                {
                                    if (keyValueHeader.ContainsKey(FName) == false)
                                    {
                                        keyValueHeader.Add(FName, key);
                                    }
                                }
                                else
                                {
                                    if (keyValueBody.ContainsKey(FName) == false)
                                    {
                                        keyValueBody.Add(FName, key);
                                        if (ContentType == "application/json")
                                        {
                                            jsonBody.Add(FName, key.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }




                if (isTodo)
                {

                    //systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescription + "WebAPI开始触发00000" + DateTime.Now, null, null);
                    await WebAPIInvoke(dataTable3, dataTable4, row, InterfaceDescription, row, true, keyValueHeader, keyValueBody, keyValueParams, ThreadGroup, FID, jsonBody, bodyString, dtDataSource);

                }

            }
            catch (Exception ex)
            {
                var msg =
$@"Message: {ex.Message}
Inner: {ex.InnerException?.Message}
StackTrace:
{ex.StackTrace}";

              
                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, "GetWebAPI111" + InterfaceDescription + ex.StackTrace, null, null);
                thisRuningThread.RuningCount = -100;
                thisRuningThread.ErrorMsg=ex.Message;
            }
            finally
            {

            }

        }
        static object obj = new object();
        /// <summary>
        /// 替换含日期占位符的字符串，支持 {yyyyMMdd|+1d} 等偏移格式（兼容 .NET 4.8）
        /// </summary>
        /// <summary>
        /// 只替换真正的日期占位符，不误伤其他 {xxx}。
        /// 支持 {yyyyMMdd|+1d}、{yyyy-MM-dd}。
        /// </summary>
        //public static string ReplaceDatePlaceholders(string template, DateTime? baseDate = null)
        //{
        //    if (string.IsNullOrEmpty(template)) return template;

        //    DateTime dt = baseDate ?? DateTime.Now;

        //    // 只匹配“合法日期格式”的占位符
        //    const string datePattern =
        //        @"\{([yMdHhmsf:/\-\s]+)(\|[+\-]\d+[dMyhms])?\}";

        //    return Regex.Replace(template, datePattern, delegate (System.Text.RegularExpressions.Match m)
        //    {
        //        string format = m.Groups[1].Value;
        //        string offset = m.Groups[2].Value;

        //        DateTime result = dt;

        //        try
        //        {
        //            if (!string.IsNullOrEmpty(offset))
        //                result = ApplyOffset(result, offset.Substring(1)); // 去掉 |

        //            return result.ToString(format);
        //        }
        //        catch
        //        {
        //            return m.Value; // 不处理，返回原文
        //        }
        //    });
        //}


        static Dictionary<string, DateLiteralReplaceState> keyDateLiteralReplaceState = new Dictionary<string, DateLiteralReplaceState>();
        /// <summary>
        /// 跨多次 ReplaceDatePlaceholders 调用的字面量日期偏移：第 1 次 +0 天，第 2 次 +1 天，第 3 次 +2 天……
        /// </summary>
        /// <summary>
        /// 字面量日期分页：仅递增模板中第 1 个日期（CallIndex 天），第 2 个日期保持模板不变作为上限；
        /// 当 第1个日期+CallIndex &gt; 第2个日期 时 HasValidLiteralDate 为 false，停止循环。
        /// </summary>
        public class DateLiteralReplaceState
        {
            public int CallIndex { get; set; }

            /// <summary>
            /// 是否继续下一次请求：第1个日期 + (CallIndex+1) 仍不大于 第2个日期时为 true。
            /// </summary>
            public bool HasValidLiteralDate { get; private set; } = true;

            internal DateTime? FirstLiteralDate { get; private set; }
            internal DateTime? SecondLiteralDate { get; private set; }

            private int _literalOrdinal;

            internal void BeginReplacePass()
            {
                _literalOrdinal = 0;
            }

            internal int NextLiteralOrdinal()
            {
                return ++_literalOrdinal;
            }

            internal void SetFirstLiteralDate(DateTime parsed)
            {
                if (!FirstLiteralDate.HasValue)
                    FirstLiteralDate = parsed.Date;
            }

            internal void SetSecondLiteralDate(DateTime parsed)
            {
                if (!SecondLiteralDate.HasValue)
                    SecondLiteralDate = parsed.Date;
            }

            internal void EndReplacePass()
            {
                if (FirstLiteralDate.HasValue && SecondLiteralDate.HasValue)
                    HasValidLiteralDate = FirstLiteralDate.Value.AddDays(CallIndex + 1) <= SecondLiteralDate.Value;
                else
                    HasValidLiteralDate = false;
            }
        }

        /// <summary>
        /// 替换日期占位符 {yyyyMMdd|+1d}（逻辑不变）；字面量：第1个按 CallIndex 递增，第2个不变，直至第1个&gt;第2个。
        /// </summary>
        public static string ReplaceDatePlaceholders(string template, DateTime? baseDate = null)
        {
            return ReplaceDatePlaceholders(template, baseDate, null);
        }

        /// <summary>
        /// 替换日期占位符；字面量配合 DateLiteralReplaceState 做 DATE_FROM/DATE_TO 式分页。
        /// </summary>
        public static string ReplaceDatePlaceholders(string template, DateTime? baseDate, DateLiteralReplaceState state)
        {
            if (string.IsNullOrEmpty(template)) return template;

            DateTime dt = baseDate ?? DateTime.Now;
            if (state != null)
                state.BeginReplacePass();

            const string datePattern =
                @"\{([yMdHhmsf:/\-\s]+)(\|[+\-]\d+[dMyhms])?\}|(?<![0-9])(\d{4}-\d{2}-\d{2})(?![0-9])|(?<![0-9])(\d{8})(?![0-9])";

            string result = Regex.Replace(template, datePattern, delegate (System.Text.RegularExpressions.Match m)
            {
                try
                {
                    if (m.Value.StartsWith("{"))
                    {
                        string format = m.Groups[1].Value;
                        string offset = m.Groups[2].Value;
                        DateTime resolved = dt;
                        if (!string.IsNullOrEmpty(offset))
                            resolved = ApplyOffset(resolved, offset.Substring(1));
                        return resolved.ToString(format);
                    }

                    string literal;
                    string outputFormat;
                    if (m.Groups[3].Success)
                    {
                        literal = m.Groups[3].Value;
                        outputFormat = "yyyy-MM-dd";
                    }
                    else
                    {
                        literal = m.Groups[4].Value;
                        outputFormat = "yyyyMMdd";
                    }

                    DateTime parsed;
                    if (!DateTime.TryParseExact(literal, outputFormat,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out parsed))
                    {
                        return m.Value;
                    }

                    if (state == null)
                        return parsed.ToString(outputFormat);

                    int ordinal = state.NextLiteralOrdinal();
                    if (ordinal == 1)
                    {
                        state.SetFirstLiteralDate(parsed);
                        return state.FirstLiteralDate.Value.AddDays(state.CallIndex).ToString(outputFormat);
                    }

                    if (ordinal == 2)
                    {
                        state.SetSecondLiteralDate(parsed);
                        return literal;
                    }

                    return literal;
                }
                catch
                {
                    return m.Value;
                }
            });

            if (state != null)
            {
                state.EndReplacePass();
                state.CallIndex++;
            }

            return result;
        }

        /// <summary>
        /// 偏移处理：+1d, -2M 等
        /// </summary>
        private static DateTime ApplyOffset(DateTime dt, string offset)
        {
            System.Text.RegularExpressions.Match m = Regex.Match(offset, @"^([+\-])(\d+)([dMyhms])$",
                RegexOptions.IgnoreCase);

            if (!m.Success) return dt;

            int val = int.Parse(m.Groups[2].Value);
            if (m.Groups[1].Value == "-") val = -val;

            string unit = m.Groups[3].Value.ToLower();

            switch (unit)
            {
                case "d": return dt.AddDays(val);
                case "m": return dt.AddMonths(val);
                case "y": return dt.AddYears(val);
                case "h": return dt.AddHours(val);
                case "s": return dt.AddSeconds(val);
            }

            return dt;
        }
        /// <summary>
        /// 线程完成后触发
        /// </summary>
        /// <param name="t"></param>
        private async static void LivenessTask(Task t, string InterfaceDescription, DateTime dateTime, string FID, string ThreadGroup)
        {
            await Task.Run(() =>
            {

            try
            {
                lock (obj)
                {


                    RuningInterFace thisRuningThread = null;
                    // 使用线程安全的方式访问 ConcurrentDictionary
                    List<RuningInterFace> threadList;
                    if (lstRuningInterFace.TryGetValue(ThreadGroup, out threadList) && threadList != null)
                    {
                        lock (threadList)
                        {
                            thisRuningThread = threadList.Where(m => m.FID == FID).FirstOrDefault();
                            if (thisRuningThread != null)
                            {

                                thisRuningThread.LastModifyedTime = DateTime.Now;
                            }
                        }
                    }
                    if (thisRuningThread != null && t.IsCompleted && thisRuningThread.RuningCount <= 0)
                    {


                        thisRuningThread.EndDate = DateTime.Now; ;
                        DateTime StartTime = thisRuningThread.StartDate;
                        DateTime EndTime = thisRuningThread.EndDate;
                        thisRuningThread.IsRuning = false;
                        int count = 0;
                        int start = 0;
                        bool isEmpty = false;
                  
                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据完成, $"当前分组{ThreadGroup}第" + start + "线程结束,接口：" + InterfaceDescription + "总记录数："+ thisRuningThread.DataCount+ ",历时:" + (DateTime.Now - StartTime).TotalMinutes + $"分，整体进度{start}/{count}", null, null, thisRuningThread.Spents);

                        if (thisRuningThread.RuningCount == 0)
                        {
                            SqlHelper.ExecuteNonQuery($@" UPDATE [dbo].[APS_InterfaceSAP] SET SyncDatetime=GETDATE(),SyncResult='成功',RunMinutes={(DateTime.Now - StartTime).TotalMinutes},DataCount={thisRuningThread.DataCount} WHERE FID={FID}; 
 
	 
	  update 
	  
	   a
	  set SyncDatetime=getdate(),SyncRate=A2.ERPSyncCycle,DataSource='ERP'
	  from Dev_Dictionary a
	  inner join APS_InterfaceSAP a2 on a2.FID={FID}
	  inner join APS_InterfaceSAPOutput b on b.FID={FID}  and  
  a.objectname like '%'+b.APSTableName+'%'

");
                                DataRow[] dataRows = dtDelete.Select($"FID={FID}");
                                if (dataRows.Length > 0)
                                {
                                    string deleteSql = string.Empty;
                                    string field = "";
                                    string DeleteCondition = dataRows[0]["DeleteCondition"].ToString();
                                    foreach (DataRow row in dataRows)
                                    {
                                        if (string.IsNullOrEmpty(deleteSql))
                                        {
                                            deleteSql = row["T"].ToString()+" 1=1 ";
                                        }
                                        deleteSql += " AND B." + row["FNameAPS"] + "=A." + row["FNameAPS"];
                                        field = row["FNameAPS"].ToString();
                                    }
                                    deleteSql += " WHERE A." + field + " IS NULL "+ DeleteCondition;
                                    SqlHelper.ExecuteNonQuery(deleteSql);
                                }
                            }
                            else
                            {
                                SqlHelper.ExecuteNonQuery($@" UPDATE [dbo].[APS_InterfaceSAP] SET SyncDatetime=GETDATE(),SyncResult='失败',ErrorMsg='"+StringHelper.ReplaceSQL(thisRuningThread.ErrorMsg)+$@"',ErrorCount=isnull(ErrorCount,0)+1,RunMinutes={(DateTime.Now - StartTime).TotalMinutes},DataCount={thisRuningThread.DataCount} WHERE FID={FID}; 
 
 

");
                            }
                            // 重用之前获取的 threadList
                            if (threadList != null)
                            {
                                lock (threadList)
                                {
                                    count = threadList.Count;
                                    start = threadList.Where(m => m.IsRuning == true).Count();
                                    threadList.Remove(thisRuningThread);
                                    isEmpty = threadList.Count == 0;
                                }
                            }

                            if (isEmpty)
                            {

                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "读取接口线程结束，开始执行APS内存储过程" + ThreadGroup + "更新", null, null);
                                //继续执行的操作
                                try
                                {
                                    if (ThreadGroup.Contains("P_ImportDataDBByNo") == false)
                                    {//订单同步不z行
                                        SqlHelper.ExecuteNonQueryAsync(@"
 exec " + ThreadGroup + @";

 

                            ");
                                    }


                                    //                     DataTable dataTable = SqlHelper.ExecuteDataTable(@"	  SELECT distinct  CODE FROM ERP_MD04(nolock)
                                    //WHERE IsScheduling=1");
                                    //                     md04Count = dataTable.Rows.Count; ;
                                    //                     md04Index = 1;
                                    //                     systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "开始循环执行MD04", null, null);
                                    //                     foreach (DataRow row in dataTable.Rows)
                                    //                     {

                                    //                           RunMD04(row["CODE"].ToString()).ContinueWith(async task =>
                                    //                         {
                                    //                              CompleteMD04(task);
                                    //                  //       });
                                    //  }


                                    systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, ThreadGroup + "接口执行结束,本次用时：" + (EndTime - StartTime).TotalMinutes + "分", null, null, (EndTime - StartTime).TotalSeconds);
                                    if (ThreadGroup == "P_ImportDataDB2")
                                    {
                                        GetPO();
                                    }


                                }
                                catch (Exception ex)
                                {
                                    systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, "APS数据内部更新失败,错误信息：" + ex.Message, null, null);
                                }
                                finally
                                {
                                    // 使用线程安全的 TryRemove 方法
                                    List<RuningInterFace> removedList;
                                    lstRuningInterFace.TryRemove(ThreadGroup, out removedList);
                                }

                            }





                        }
                    }
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescription + "获取ERP数据错误,LivenessTask方法执行错误，" + ex.Message + DateTime.Now, null, null);
                }
            });



        }


        /// <summary>
        /// 循环接口数据
        /// </summary>
        /// <param name="dataTable2"></param>
        /// <param name="dataTable3"></param>
        /// <param name="dataTable4"></param>
        /// <param name="row"></param>
        /// <param name="InterfaceName"></param>
        /// <param name="InterfaceDescription"></param>
        private async static Task<bool> GetSap(DataTable dataTable2, DataTable dataTable3, DataTable dataTable4, DataRow row, string InterfaceName, string InterfaceDescription, DateTime dateTime, string FID, string ThreadGroup, bool isAwait = true, bool isDel = true)
        {
            RuningInterFace thisRuningThread = null;
            // 使用线程安全的方式访问 ConcurrentDictionary
            List<RuningInterFace> threadList;
            if (lstRuningInterFace.TryGetValue(ThreadGroup, out threadList) && threadList != null)
            {
                lock (threadList)
                {
                    thisRuningThread = threadList.Where(m => m.FID == FID).FirstOrDefault();
                }
            }
            if (thisRuningThread == null)
            {
                return false;
            }


            try
            {


                // IRfcFunction fun003 = repository.CreateFunction(InterfaceName);//主接口名称
                DataRow[] dataRows = dataTable2.Select("FID=" + row["FID"], "DataSql ");
                //是否是一次查询
                bool isTodo = true;
                ///传入参数集合
                Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
                foreach (DataRow row1 in dataRows)
                {
                    string DefaultValue = row1["DefaultValue"].ToString();
                    string FormatValue = row1["FormatValue"].ToString();
                    string FName = row1["FName"].ToString();
                    string DataSql = row1["DataSql"].ToString();
                    DataRow[] rowsParameter = dataTable5.Select("EID=" + row1["EID"], "DataSql ");
                    if (string.IsNullOrEmpty(DefaultValue) && string.IsNullOrEmpty(DataSql) && rowsParameter.Length == 0)
                    {//没有默认值，没有数据源，跳过
                        continue;
                    }
                    if (!string.IsNullOrEmpty(DataSql))
                    {//有数据源，要遍历
                        isTodo = false;
                        DataTable dataSource = SqlHelper.ExecuteDataTable(DataSql);
                        bool isDelete = isDel;
                        double rowIndex = 0;
                        int dataSourceCount = dataSource.Rows.Count;

                        thisRuningThread.RuningCount = dataSourceCount;
                        foreach (DataRow row2 in dataSource.Rows)
                        {
                            rowIndex++;
                            try
                            {

                                if (dataSource.Columns.Count == 1)
                                {//只有一列，直接绑定
                                    if (keyValuePairs.ContainsKey(FName) == false)
                                    {
                                        keyValuePairs.Add(FName, row2[0].ToString());
                                    }
                                    else
                                    {
                                        keyValuePairs[FName] = row2[0].ToString();
                                    }
                                    //  fun003.SetValue(FName, row2[0].ToString());//指定值
                                }
                                else
                                {
                                    foreach (DataColumn dataColumn in dataSource.Columns)
                                    {
                                        if (keyValuePairs.ContainsKey(dataColumn.ColumnName) == false)
                                        {
                                            keyValuePairs.Add(dataColumn.ColumnName, row2[dataColumn.ColumnName].ToString());
                                        }
                                        else
                                        {
                                            keyValuePairs[dataColumn.ColumnName] = row2[dataColumn.ColumnName].ToString();
                                        }
                                        //  fun003.SetValue(dataColumn.ColumnName, row2[dataColumn.ColumnName].ToString());//指定值
                                    }

                                }

                                IRfcFunction funTmpThread = null;
                                //第一次执行，使用同步
                                if (thisRuningThread.Function == null)
                                {
                                    funTmpThread = repository.CreateFunction(InterfaceName);//主接口名称
                                    thisRuningThread.Function = funTmpThread;
                                }
                                else
                                {
                                    funTmpThread = thisRuningThread.Function;
                                }

                                IRfcFunction funTmp = funTmpThread.Clone() as IRfcFunction;
                                foreach (var key in keyValuePairs.Keys)
                                {

                                    funTmp.SetValue(key, keyValuePairs[key]);//指定值
                                }
                                //if (isDelete)
                                //{

                                //    await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, keyValuePairs);
                                //    isDelete = false;
                                //}
                                //else
                                {
                                    //非第一次，使用异步，提升速度
                                    // Task.Run(() =>
                                    //  {

                                    //IRfcFunction funTmp = repository.CreateFunction(InterfaceName);//主接口名称
                                    //foreach (var key in keyValuePairs.Keys)
                                    //{

                                    //    funTmp.SetValue(key, keyValuePairs[key]);//指定值
                                    //}
                                    if (thisRuningThread != null)
                                    {

                                        thisRuningThread.LastModifyedTime = DateTime.Now;
                                    }
                                    Dictionary<string, object> copiedDictionary = new Dictionary<string, object>(keyValuePairs);
                                    Task task1 = null;
                                    if (isDelete)
                                    {//记录数太多则使用同步

                                        await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                        {
                                            if (m.IsCompleted)
                                            {

                                                //
                                                lock (obj)
                                                {

                                                    thisRuningThread.RuningCount--;
                                                    LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                }


                                            }
                                        });


                                        isDelete = false;


                                    }
                                    else if (thisRuningThread.ThreadRuningCount >= thisRuningThread.ThreadCount)
                                    {//超过线程数了，要等待
                                        await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                        {
                                            if (m.IsCompleted)
                                            {
                                                //
                                                lock (obj)
                                                {

                                                    thisRuningThread.RuningCount--;
                                                    LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                }


                                            }
                                        });

                                    }
                                    else
                                    {
                                        lock (obj)
                                        {
                                            thisRuningThread.ThreadRuningCount++;
                                        }
                                        if (isAwait)
                                        {
                                            await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                            {
                                                if (m.IsCompleted)
                                                {
                                                    //
                                                    lock (obj)
                                                    {

                                                        thisRuningThread.RuningCount--;
                                                        thisRuningThread.ThreadRuningCount--;
                                                        LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                    }


                                                }
                                            });
                                        }
                                        else
                                        {
                                            Task.Run(async () =>
                                            {

                                                SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                                {
                                                    if (m.IsCompleted)
                                                    {
                                                        //
                                                        lock (obj)
                                                        {

                                                            thisRuningThread.RuningCount--;
                                                            thisRuningThread.ThreadRuningCount--;
                                                            LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                        }


                                                    }
                                                });
                                            });

                                        }

                                    }

                                    // });
                                }


                            }
                            catch (Exception ex)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescription + "值:" + row2[0].ToString() + "," + ex.Message, null, null);
                            }

                            //减少日志的记录数,每次只记录100调条
                            double logCount = Math.Ceiling(dataSourceCount / 20.0D);
                            if (logCount < 1)
                            {
                                logCount = 1;
                            }
                            if (rowIndex % logCount == 0 && dataSourceCount > 0)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescription + "循环数据源,第" + rowIndex + ",共" + dataSourceCount + ",进度:" + string.Format("{0:P2}", (rowIndex / dataSourceCount)) + "，时间：" + DateTime.Now, null, null);
                            }


                        }
                    }
                    else
                    {

                        //有输入参数的更多参数，需要去执行
                        if (rowsParameter.Length > 0)
                        {

                            IRfcFunction fun003 = repository.CreateFunction(InterfaceName);//主接口名称
                            IRfcTable itb = fun003.GetTable(FName);
                            itb.Insert();//没有数据源，则新增，否则使用数据源循环插入
                                         //if (rowsParameter[0]["DataSql"].ToString() == "")
                                         //{
                                         //    itb.Insert();//没有数据源，则新增，否则使用数据源循环插入
                                         //}

                            //是否有SQL语句
                            bool isSql = false;
                            foreach (DataRow row2 in rowsParameter)
                            {

                                DataSql = row2["DataSql"].ToString();
                                if (!string.IsNullOrEmpty(DataSql))
                                {
                                    isSql = true;
                                }
                            }
                            foreach (DataRow row2 in rowsParameter)
                            {

                                DataSql = row2["DataSql"].ToString();
                                string inputFName = row2["FName"].ToString();
                                DefaultValue = row2["DefaultValue"].ToString();
                                FormatValue = row2["FormatValue"].ToString();
                                if (!string.IsNullOrEmpty(DataSql))
                                {

                                    IRfcFunction funTmpThread = null;
                                    //第一次执行，使用同步
                                    if (thisRuningThread.Function == null)
                                    {
                                        funTmpThread = repository.CreateFunction(InterfaceName);//主接口名称
                                        thisRuningThread.Function = funTmpThread;
                                    }
                                    else
                                    {
                                        funTmpThread = thisRuningThread.Function;
                                    }




                                    DataTable dtInput = SqlHelper.ExecuteDataTable(DataSql);


                                    bool isDelete = true;
                                    double rowIndex = 0;
                                    int dataSourceCount = dtInput.Rows.Count;
                                    thisRuningThread.RuningCount = dataSourceCount;
                                    foreach (DataRow row3 in dtInput.Rows)
                                    {
                                        IRfcFunction funTmp = funTmpThread.Clone() as IRfcFunction;
                                        itb = funTmp.GetTable(FName);
                                        itb.Insert();//没有数据源，则新增，否则使用数据源循环插入
                                                     //   itb.Insert();
                                        itb.CurrentRow.SetValue(inputFName, row3[0].ToString());
                                        foreach (DataRow row4 in rowsParameter)
                                        {
                                            if (row4["FName"].ToString() == inputFName)
                                            {
                                                continue;
                                            }
                                            DefaultValue = row4["DefaultValue"].ToString();
                                            if (DefaultValue.IndexOf("DAY") > -1)
                                            {
                                                DefaultValue = DefaultValue.Replace("DAY", "");
                                                if (!string.IsNullOrEmpty("FormatValue"))
                                                {
                                                    DefaultValue = string.Format(FormatValue, DateTime.Now.AddDays(double.Parse(DefaultValue)));
                                                }
                                                else
                                                {
                                                    DefaultValue = DateTime.Now.AddDays(double.Parse(DefaultValue)).ToString();
                                                }
                                            }

                                            itb.CurrentRow.SetValue(row4["FName"].ToString(), DefaultValue);
                                        }





                                        //异步触发



                                        // foreach (var key in keyValuePairs.Keys)
                                        {

                                            funTmp.SetValue(FName, itb);//指定值
                                        }

                                        Dictionary<string, object> copiedDictionary = new Dictionary<string, object>(keyValuePairs);
                                        Task task1 = null;
                                        if (isDelete)
                                        {//记录数太多则使用同步

                                            await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                            {
                                                if (m.IsCompleted)
                                                {
                                                    //
                                                    lock (obj)
                                                    {

                                                        thisRuningThread.RuningCount--;
                                                    }

                                                    LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                }
                                            });


                                            isDelete = false;


                                        }
                                        else if (thisRuningThread.ThreadRuningCount >= thisRuningThread.ThreadCount)
                                        {
                                            await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                            {
                                                if (m.IsCompleted)
                                                {
                                                    //
                                                    lock (obj)
                                                    {

                                                        thisRuningThread.RuningCount--;
                                                    }

                                                    LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                }
                                            });

                                        }
                                        else
                                        {
                                            lock (obj)
                                                thisRuningThread.ThreadRuningCount++;
                                            if (isAwait)
                                            {
                                                await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                                {
                                                    if (m.IsCompleted)
                                                    {
                                                        //
                                                        lock (obj)
                                                        {

                                                            thisRuningThread.RuningCount--;
                                                            thisRuningThread.ThreadRuningCount--;
                                                        }

                                                        LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                    }
                                                });

                                            }
                                            else
                                            {
                                                Task.Run(() =>
                                                {

                                                    SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, isDelete, copiedDictionary, ThreadGroup, FID).ContinueWith(m =>
                                                    {
                                                        if (m.IsCompleted)
                                                        {
                                                            //
                                                            lock (obj)
                                                            {

                                                                thisRuningThread.RuningCount--;
                                                                thisRuningThread.ThreadRuningCount--;
                                                            }

                                                            LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                                                        }
                                                    });
                                                });

                                            }

                                        }


                                    }



                                    // itb.CurrentRow.SetValue(row2["FName"].ToString(), DefaultValue);
                                    break;

                                }
                                else
                                {
                                    if (DefaultValue.IndexOf("DAY") > -1)
                                    {
                                        DefaultValue = DefaultValue.Replace("DAY", "");
                                        if (!string.IsNullOrEmpty(FormatValue))
                                        {
                                            DefaultValue = string.Format(FormatValue, DateTime.Now.AddDays(double.Parse(DefaultValue)));
                                        }
                                        else
                                        {
                                            DefaultValue = DateTime.Now.AddDays(double.Parse(DefaultValue)).ToString();
                                        }
                                    }
                                    // if (keyValuePairs.ContainsKey(inputFName) == false)
                                    {
                                        itb.CurrentRow.SetValue(inputFName, DefaultValue);
                                    }
                                    // fun003.SetValue(FName, keyValuePairs[FName]);//指定值
                                }

                                // fun003.SetValue(FName, itb);
                            }
                            if (isSql == false)
                            {
                                keyValuePairs.Add(FName, itb);
                            }
                            else
                            {
                                isTodo = false;
                            }

                        }
                        else
                        {
                            if (DefaultValue.IndexOf("DAY") > -1)
                            {
                                DefaultValue = DefaultValue.Replace("DAY", "");
                                if (!string.IsNullOrEmpty("FormatValue"))
                                {
                                    DefaultValue = string.Format(FormatValue, DateTime.Now.AddDays(double.Parse(DefaultValue)));
                                }
                                else
                                {
                                    DefaultValue = DateTime.Now.AddDays(double.Parse(DefaultValue)).ToString();
                                }
                            }
                            if (keyValuePairs.ContainsKey(FName) == false)
                            {
                                keyValuePairs.Add(FName, DefaultValue);
                            }
                            // fun003.SetValue(FName, keyValuePairs[FName]);//指定值
                        }

                    }

                }
                //没有发生循环，之间是单线程
                if (isTodo)
                {
                    IRfcFunction funTmp = repository.CreateFunction(InterfaceName);//主接口名称
                    foreach (var key in keyValuePairs.Keys)
                    {

                        funTmp.SetValue(key, keyValuePairs[key]);//指定值
                    }
                    await SapInvoke(dataTable3, dataTable4, row, InterfaceDescription, funTmp, true, keyValuePairs, ThreadGroup, FID).ContinueWith(m =>
                    {
                        if (m.IsCompleted)
                        {
                            //
                            lock (obj)
                            {

                                thisRuningThread.RuningCount--;
                            }

                            LivenessTask(m, InterfaceDescription, dateTime, FID, ThreadGroup);
                        }
                    });
                }


            }
            catch (Exception ex)
            {
                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceName + InterfaceDescription + "获取ERP数据错误" + ex.Message + DateTime.Now, null, null);
                if (thisRuningThread != null)
                {
                    // 重用之前获取的 threadList
                    List<RuningInterFace> threadListForRemove;
                    if (lstRuningInterFace.TryGetValue(ThreadGroup, out threadListForRemove) && threadListForRemove != null)
                    {
                        lock (threadListForRemove)
                        {
                            threadListForRemove.Remove(thisRuningThread);
                        }
                    }
                }

            }
            finally
            {
                // lock (obj)


            }


            return true;

        }
        public static object ToJsonObjectOrString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;
            value = value.Trim();
            // 只有像 JSON 才尝试解析
            if ((value.StartsWith("{") && value.EndsWith("}")) ||
                (value.StartsWith("[") && value.EndsWith("]")))
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch (JsonReaderException)
                {
                    // 配置写错了：看起来像 JSON 但格式非法
                    return value; // 或 throw，看业务要求
                }
            }
            // 普通字符串，原样保留
            return value;
        }
        /// <summary>
        /// 执行WEBAPI接口
        /// </summary>
        /// <param name="dataTable3">输出表</param>
        /// <param name="dataTable4">输出表参数</param>
        /// <param name="row">当前循环的接口行</param>
        /// <param name="InterfaceDescription">接口描述</param>
        /// <param name="rowInput">当前的接口行</param>
        /// <param name="isDelete">是否删除</param>
        /// <param name="keyValuesHeaders">头部参数</param>
        /// <param name="keyValueBody">body参数</param>
        /// <param name="keyValueParams"></param>
        /// <param name="ThreadGroup"></param>
        /// <param name="FID"></param>
        /// <param name="jsonBody"></param>
        /// <param name="bodyString"></param>
        /// <param name="dtDataSource">数据源</param>
        /// <returns></returns>
        private async static Task WebAPIInvoke(DataTable dataTable3, DataTable dataTable4, DataRow row, string InterfaceDescription, DataRow rowInput, bool isDelete, Dictionary<string, KeyValueObj> keyValuesHeaders, Dictionary<string, KeyValueObj> keyValueBody, Dictionary<string, KeyValueObj> keyValueParams, string ThreadGroup, string FID, Dictionary<string, object> jsonBody, string bodyString, DataTable dtDataSource)
        {
            RuningInterFace thisRuningThread = null;

            
            int indexDataSource = 0;//数据源
            try
            {
                // 使用线程安全的方式访问 ConcurrentDictionary
                List<RuningInterFace> threadList;
                if (!lstRuningInterFace.TryGetValue(ThreadGroup, out threadList) || threadList == null)
                {
                    return;
                }

                lock (threadList)
                {
                    thisRuningThread = threadList.Where(m => m.FID == FID).FirstOrDefault();
                }
                if (thisRuningThread == null)
                {
                    return;
                }

                Encoding encoding = Encoding.UTF8;
                string url = rowInput["APIUrl"].ToString();

                int cycleIndex = -1;
                string cycleName = "";
                string CycleFieldName = "";
                foreach (string key in keyValueParams.Keys)
                {//部分旧的分页在上面URL上，这部分新的分页在body里，循环字段分为两种，一种是直接在URL上，另一种是在body里
                    if (keyValueParams[key].IsCycle == false)
                    {
                        url += "&" + key + "=" + keyValueParams[key].Value;
                    }
                    else
                    {
                        cycleIndex = int.Parse(keyValueParams[key].Value);
                        cycleName = key;
                    }

                }
                foreach (string key in keyValueBody.Keys)
                {
                    if (keyValueBody[key].IsCycle == false)
                    {

                    }
                    else
                    {

                        cycleName = key;
                        //需要循环的字段
                        CycleFieldName = keyValueBody[key].CycleFieldName;
                        if (string.IsNullOrEmpty(CycleFieldName))
                        {//找不到分页字段
                            cycleIndex = int.Parse(keyValueBody[key].Value);
                        }
                        else
                        {
                            string jsonString = jsonBody[cycleName].ToString();
                            JObject jsonObj = JObject.Parse(jsonString);


                            cycleIndex = int.Parse(jsonObj[CycleFieldName].ToString());
                        }
                       
                    }

                }
                bool isRead = true;

                DataTable dtDateTime = SqlHelper.ExecuteDataTable(@"SELECT DATA_TYPE,a.TABLE_NAME,A.COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS A
INNER JOIN APS_InterfaceSAPOutputField B ON A.COLUMN_NAME = B.FNameAPS AND B.TurnDate=1
INNER  JOIN APS_InterfaceSAPOutput C ON C.APSTableNameTemp = A.TABLE_NAME
WHERE DATA_TYPE = 'datetime'
 ");
                string afterInsertSQL = "";//执行后续的SQL语句
                string afterInsertSelect = "";//执行后续的SQL语句,临时表的字段
                string afterUpdateSQL = "";//执行后续的SQL语句
                string afterMappingFields = "";

                string afterInsertSQL2 = "";//执行后续的SQL语句
                string afterInsertSelect2 = "";//执行后续的SQL语句,临时表的字段
                string afterUpdateSQL2 = "";//执行后续的SQL语句
                string afterMappingFields2 = "";
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol =
 SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                while (isRead)
                {

                    Dictionary<string, WebApiResult> dicTable = new Dictionary<string, WebApiResult>();
                    bool isPage = false;
                    //目前只发现通过keyValueParams需要循环，只实现这个功能
                    string url2 = url;
                    if (string.IsNullOrEmpty(CycleFieldName))
                    {//本身已经是分页循环的字段了,之间去当前的值就行了
                        if (string.IsNullOrEmpty(cycleName))
                        {
                            isRead = false;
                        }
                        else
                        {
                            if (keyValueParams.Count > 0)
                            {
                                url2 = url + "&" + cycleName + "=" + cycleIndex;
                            }
                            jsonBody[cycleName] = cycleIndex * keyValueBody[cycleName].SkipRows;
                            isPage = true;
                        }
                       
                    }
                    else
                    {
                        string jsonString = jsonBody[cycleName].ToString();
                        JObject jsonObj = JObject.Parse(jsonString);
                        jsonObj[CycleFieldName] = cycleIndex * keyValueBody[cycleName].SkipRows; 
                        jsonBody[cycleName] = jsonObj;
                        isPage = true;
                    }

                
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url2);
                    request.Method = rowInput["HttpMethod"].ToString() == "" ? "POST" : rowInput["HttpMethod"].ToString();
                    request.ContentType = "application/json; charset=utf-8";
                    request.Accept = "application/json";
                    request.Timeout = 1000 *60*60;
                    //  request.Headers.Add("Authorization", "eyJhbGciOiJIUzI1NiJ9.eyJqdGkiOiJkYTcyY2NkOS04M2MyLTQ3YTgtOGI2Mi1lN2ZkZjM3N2YyMjQiLCJpc3MiOiJqZy1tZXMtand0Iiwic3ViIjoie1wicGhvbmVOdW1iZXJcIjpcIjE4NjIwMTIwODgwXCIsXCJsb2dpbkZyb21cIjpcImRldmljZVwiLFwidXNlcm5hbWVcIjpcIjE4NjIwMTIwODgwXCJ9IiwiaWF0IjoxNjUzODk0MzQyfQ.zjydn782NptTzfgg-CdpUtSbquxJUJjsPKPmmuuvtuQ");

                    //  if(rowInput["DataType"].ToString()== "Headers")

                    foreach (var name in keyValuesHeaders.Keys)
                    {
                        request.Headers.Add(name, keyValuesHeaders[name].Value);
                    }

                    Stream requestStream = null;
                    HttpWebResponse response = null;
                    string res = "";
                   
                    string inputParameters = "";
                    try
                    {
                        //else
                        //{
                        if (keyValueBody.Count > 0)
                        {
                            Dictionary<string, string> body = new Dictionary<string, string>();
                            foreach (string key in keyValueBody.Keys)
                            {
                                if (dtDataSource != null && dtDataSource.Columns.Contains(key))
                                {
                                    keyValueBody[key].Value = dtDataSource.Rows[indexDataSource][key].ToString();

                                }
                               
                                body.Add(key, keyValueBody[key].Value);
                            }

                            inputParameters = JsonConvert.SerializeObject(body);
                            if (jsonBody != null && jsonBody.Count > 0)
                            {
                                inputParameters = JsonConvert.SerializeObject(jsonBody);
                            }
                            byte[] buffer = encoding.GetBytes(inputParameters);
                            request.ContentLength = buffer.Length;
                            requestStream = request.GetRequestStream();
                            requestStream.Write(buffer, 0, buffer.Length);
                            requestStream.Close(); // 关闭请求流

                        }
                        else if (!string.IsNullOrEmpty(bodyString))
                        {
                            // bodyString = FilterAndCleanJsonString(bodyString);
                            if (keyDateLiteralReplaceState.ContainsKey(InterfaceDescription) == false)
                            {
                                keyDateLiteralReplaceState.Add(InterfaceDescription, new DateLiteralReplaceState());
                            }
                            //尝试替换日期，每次加1天，目前只能之间在body里实现
                         
                            //if(isPage==false)
                            //isRead = keyDateLiteralReplaceState[InterfaceDescription].HasValidLiteralDate;
                            byte[] buffer = encoding.GetBytes(ReplaceDatePlaceholders(bodyString, null, keyDateLiteralReplaceState[InterfaceDescription]));
                            request.ContentLength = buffer.Length;
                            requestStream = request.GetRequestStream();
                            requestStream.Write(buffer, 0, buffer.Length);
                            requestStream.Close(); // 关闭请求流
                        }
                        else
                        {
                            request.ContentLength = 0;
                        }
                        //}

                        response = (HttpWebResponse)request.GetResponse();

                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            #region 开始读取
                            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescription + "从WebAPI读取数据完成，" + DateTime.Now, null, null);

                            DataRow[] drOutputName = dataTable3.Select("FID=" + row["FID"]);
                            //循环参数表
                            foreach (DataRow dr in drOutputName)
                            {
                                string resultSQL = @"UPDATE  APS_InterfaceSAPOutput set InputParameters=@inputParameters   WHERE EID=" + dr["EID"].ToString() + ";";
                                string APSTableNameTemp = dr["APSTableNameTemp"].ToString();
                                string APSTableName = dr["APSTableName"].ToString();
                                if (isDelete)
                                {
                                    afterInsertSQL += "INSERT INTO " + APSTableName + "(";
                                    afterUpdateSQL += "UPDATE B SET   ";
                                }


                                string OutputName = dr["OutputName"].ToString();
                                string JoinTableName = dr["JoinTableName"].ToString();//关联的
                                string InterfaceDescriptionOutput = InterfaceDescription + "接口的输出参数：" + dr["InterfaceDescription"].ToString();
                                bool isString = dr["IsString"].ToSafeBool();

                                Dictionary<string, bool> dicIsFirst = new Dictionary<string, bool>();//记录是否是第一次执行
                                res = reader.ReadToEnd();
                                try
                                {
                                    JObject jObject = JsonConvert.DeserializeObject(res) as JObject;

                                    string[] outputName = dr["OutputName"].ToString().Split('.');

                                    JArray jArray1 = null;

                                    if (outputName.Length == 2)
                                    {
                                        jArray1 = jObject[outputName[0]][outputName[1]] as JArray;
                                    }
                                    else
                                    {

                                        if (isString)
                                        {///返回的是字符串
                                            jArray1 = JsonConvert.DeserializeObject(jObject[outputName[0]].ToString()) as JArray;
                                        }
                                        else
                                        {
                                            jArray1 = jObject[outputName[0]] as JArray;
                                        }
                                    }
                                    if (jArray1 == null || jArray1.Count == 0 && !string.IsNullOrEmpty(cycleName))
                                    {

                                        isRead = false;
                                    }
                                    int count = jArray1.Count;
                                    thisRuningThread.DataCount += count;
                                    if ((bool)dr["EnableOutputDetail"] == true)
                                    {
                                        SqlParameter[] sqlParameters = new SqlParameter[3];
                                        DataRow[] dataRows = dtDelete.Select($"FID={FID}");
                                        if (dataRows.Length > 0)
                                        {
                                            sqlParameters[0] = new SqlParameter("@FullData", res);
                                        }
                                        else
                                        {
                                            sqlParameters[0] = new SqlParameter("@FullData", "");
                                        }
                                        sqlParameters[1] = new SqlParameter("@LatestData", res);
                                        sqlParameters[2] = new SqlParameter("@InputParameters", inputParameters);
                                        resultSQL += (isDelete ? $"delete from aps_InterfaceSAPOutputDetail where EID={dr["EID"]} ;" : "");
                                        resultSQL += $@"
INSERT INTO [dbo].[APS_InterfaceSAPOutputDetail]
           ([EID]
           ,[FID]
           ,[OutputName]
           ,[APSTableName]
           ,[FullData]
           ,[LatestData]
           ,[InputParameters]
           ,[Remark1]
           ,[Remark2]
           ,[Status]
           ,[CreatedBy]
           ,[CreatedByName]
           ,[ModifiedBy]
           ,[ModifiedByName]
           ,[CreatedOn]
           ,[ModifyedOn]
           ,[SyncDatetime])
 SELECT [EID]
           ,[FID]
           ,[OutputName]
           ,[APSTableName]
           ,@FullData
           ,@LatestData
           ,@InputParameters
           ,[Remark1]
           ,[Remark2]
           ,[Status]
           ,[CreatedBy]
           ,[CreatedByName]
           ,[ModifiedBy]
           ,[ModifiedByName]
           ,GETDATE()
           ,GETDATE()
           ,GETDATE()
           from APS_InterfaceSAPOutput
           where eid={dr["EID"]}

";
                                        SqlHelper.ExecuteNonQuery(resultSQL, sqlParameters);

                                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescriptionOutput + "使用数据库模式,body:" + bodyString ?? "" + "，记录数：" + count + "，时间：" + DateTime.Now, null, null);
                                        isDelete = false;

                                        continue;
                                    }
                            

                                    DataRow[] dataRows2 = dataTable4.Select(" EID=" + dr["EID"]);
                                    StringBuilder stringBuilder = new StringBuilder();
                                    string fields = "INSERT INTO " + APSTableNameTemp + " (";

                                    Dictionary<string, string[]> fNames = new Dictionary<string, string[]>();
                                    string MappingFields = ""; ;
                                    foreach (DataRow row2 in dataRows2)
                                    {
                                        string FNameAPS = row2["FNameAPS"].ToString().Trim();
                                        string FName = row2["FName"].ToString();
                                        string JoinFieldName = row2["JoinFieldName"].ToString();
                                        bool IsUpdate = row2["IsUpdate"].ToSafeBool();
                                        if (row2["MappingFields"].ToString().ToLower() == "true")
                                        {
                                            MappingFields = FNameAPS;
                                        }
                                        if (FName.IndexOf(".") > 0)
                                        {
                                            fNames.Add(FName, FName.Split('.'));
                                        }
                                        else if (!fNames.ContainsKey(FName))
                                        {
                                            fNames.Add(FName, new string[1] { FName });
                                        }
                                        if (row2["FieldType"].ToString() != "数组")
                                        {
                                            fields += FNameAPS + ",";
                                            if (IsUpdate)
                                            {
                                                if (isDelete)
                                                {
                                                    afterInsertSQL += FNameAPS + ",";
                                                    if (string.IsNullOrEmpty(JoinFieldName))
                                                    {

                                                        afterInsertSelect += "A." + FNameAPS + ",";
                                                        afterUpdateSQL += "  B." + FNameAPS + "=A." + FNameAPS + ",";
                                                    }
                                                    else
                                                    {

                                                        afterInsertSelect += JoinFieldName + ",";
                                                        afterUpdateSQL += "  B." + FNameAPS + "=" + JoinFieldName + ",";
                                                    }
                                                }

                                            }


                                            if (row2["MappingFields"].ToString().ToLower() == "true")
                                            {
                                                afterMappingFields += " and  A." + FNameAPS + "=B." + FNameAPS;
                                            }

                                        }
                                        else
                                        {
                                            //   stringBuilder.Append((isDelete ? "truncate table " + FNameAPS : "") + " ;");

                                            //  isDelete = false;
                                            DataRow[] row3s = dataTable4.Select(" ParentFieldID=" + row2["FieldID"]);
                                            int row3Index = 1;
                                            string APSTableName2 = FNameAPS.ToLower().Replace("_temp", "");
                                            //如果来源是数组，则读取相关的表
                                            if (isDelete)
                                            {
                                                afterInsertSQL2 += "INSERT INTO " + APSTableName2 + "(";
                                                afterUpdateSQL2 += "UPDATE B SET   ";
                                            }
                                            string MappingFields2 = "";

                                            foreach (DataRow row3 in row3s)
                                            {
                                                string FNameAPS2 = row3["FNameAPS"].ToString().Trim();
                                                IsUpdate = row3["IsUpdate"].ToSafeBool();
                                                string JoinFieldName2 = row3["JoinFieldName"].ToString();
                                                if (row3["MappingFields"].ToString().ToLower() == "true")
                                                {
                                                    MappingFields2 = FNameAPS2;
                                                }
                                                if (dicTable.ContainsKey(FNameAPS) == false)
                                                {
                                                    if (isDelete)
                                                    {
                                                        dicTable.Add(FNameAPS, new WebApiResult() { Msg = "truncate table " + FNameAPS + ";INSERT INTO " + FNameAPS + "(" });
                                                    }
                                                    else
                                                    {
                                                        dicTable.Add(FNameAPS, new WebApiResult() { Msg = "INSERT INTO " + FNameAPS + "(" });
                                                    }
                                                  
                                                    dicIsFirst.Add(FNameAPS, true);
                                                }
                                                dicTable[FNameAPS].Msg += FNameAPS2 + ",";

                                                if (isDelete)
                                                {
                                                    if (IsUpdate)
                                                    {
                                                        afterInsertSQL2 += FNameAPS2 + ",";
                                                        if (string.IsNullOrEmpty(JoinFieldName2))
                                                        {
                                                            afterInsertSelect2 += "A." + FNameAPS2 + ",";
                                                            afterUpdateSQL2 += "   B." + FNameAPS2 + "=A." + FNameAPS2 + ",";
                                                        }
                                                        else
                                                        {
                                                            afterInsertSelect2 += JoinFieldName2 + ",";
                                                            afterUpdateSQL2 += "   B." + FNameAPS2 + "=" + JoinFieldName2 + ",";
                                                        }
                                                    }

                                                    if (row3["MappingFields"].ToString().ToLower() == "true")
                                                    {
                                                        afterMappingFields2 += " and  B." + FNameAPS2 + "=A." + FNameAPS2;
                                                    }
                                                }


                                                if (row3Index == row3s.Length)
                                                {
                                                    if (row3s.Where(m => m["FNameAPS"].ToString().ToLower() == "status").Count() == 0)
                                                    {
                                                        dicTable[FNameAPS].Msg += "Status)";
                                                    }
                                                }

                                                row3Index++;
                                            }
                                            if (isDelete)
                                            {
                                                if (row3s.Where(m => m["FNameAPS"].ToString().ToLower() == "status").Count() == 0)
                                                {
                                                    afterInsertSQL2 += "Status,SyncDatetime) SELECT " + afterInsertSelect2 + "1,getdate() FROM " + FNameAPS + " A LEFT JOIN " + APSTableName2 + " B ON  1=1 " + afterMappingFields2 + "\r\n" + JoinFieldName + " WHERE B." + MappingFields2 + " IS NULL";
                                                    afterUpdateSQL2 += "B.SyncDatetime=GETDATE() FROM " + FNameAPS + " A INNER JOIN " + APSTableName2 + " B ON  1=1 " + afterMappingFields2 + "\r\n" + JoinFieldName;
                                                }
                                                else
                                                {
                                                    afterInsertSQL2 += "SyncDatetime) SELECT " + afterInsertSelect2 + "getdate() FROM " + FNameAPS + " A LEFT JOIN " + APSTableName2 + " B ON  1=1 " + afterMappingFields2 + "\r\n" + JoinFieldName + " WHERE B." + MappingFields2 + " IS NULL";
                                                    afterUpdateSQL2 += "B.SyncDatetime=GETDATE() FROM " + FNameAPS + " A INNER JOIN " + APSTableName2 + " B ON  1=1 " + afterMappingFields2 + "\r\n" + JoinFieldName;
                                                }

                                                if (string.IsNullOrEmpty(afterMappingFields2))
                                                {
                                                    afterInsertSQL2 = afterUpdateSQL2 = "";
                                                }
                                            }
                                        }


                                    }
                                    if (isDelete)
                                    {
                                        if (dataRows2.Where(m => m["FNameAPS"].ToString().ToLower() == "status").Count() == 0)
                                        {

                                            afterInsertSQL += "Status,SyncDatetime) SELECT " + afterInsertSelect + "1,getdate() FROM " + APSTableNameTemp + " A LEFT JOIN " + APSTableName + " B ON  1=1 " + afterMappingFields + @"
                            " + JoinTableName + " WHERE B." + MappingFields + " IS NULL ;";
                                        }
                                        else
                                        {
                                            afterInsertSQL += "SyncDatetime) SELECT " + afterInsertSelect + "getdate() FROM " + APSTableNameTemp + " A LEFT JOIN " + APSTableName + " B ON  1=1 " + afterMappingFields + @"
                            " + JoinTableName + " WHERE B." + MappingFields + " IS NULL ;";
                                        }

                                        afterUpdateSQL += "B.SyncDatetime=GETDATE() FROM " + APSTableNameTemp + " A INNER JOIN " + APSTableName + " B ON  1=1 " + afterMappingFields + @"
                                        " + JoinTableName + ";";
                                    }
                                    if (dataRows2.Where(m => m["FNameAPS"].ToString().ToLower() == "status").Count() > 0)
                                    {
                                        fields = fields.Trim(',') + ")";
                                    }
                                    else
                                    {

                                        fields = fields + "Status)";
                                    }

                                    if (string.IsNullOrEmpty(afterMappingFields))
                                    {
                                        afterInsertSQL = afterUpdateSQL = "";
                                    }
                                    if (isDelete && !string.IsNullOrEmpty(afterInsertSQL2))
                                    {
                                        afterInsertSQL += ";" + afterInsertSQL2;
                                        afterUpdateSQL += ";" + afterUpdateSQL2;
                                    }
                                    stringBuilder.Append((isDelete ? "truncate table " + APSTableNameTemp + " ;" : "") + fields);

                                    isDelete = false;


                                    decimal rowIndex = 0;
                                 
                                    systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescriptionOutput + "开始循环,body:"+bodyString??""+"，记录数：" + count + "，时间：" + DateTime.Now, null, null);
                                    bool isFirst = true;
                                 

                                    foreach (JObject item in jArray1)
                                    {
                                        if (isFirst)
                                        {
                                            stringBuilder.Append("SELECT ");

                                            isFirst = false;
                                        }
                                        else
                                        {
                                            stringBuilder.Append(" UNION ALL SELECT ");

                                        }
                                        bool isFirstField = true;
                                        //循环所有的输出字段
                                        foreach (DataRow row2 in dataRows2)
                                        {
                                            string FNameAPS = row2["FNameAPS"].ToString().Trim();
                                            string FName = row2["FName"].ToString();
                                            string[] fs = fNames[FName];
                                            string DefaultValue = row2["DefaultValue"].ToString();
                                            //if (index == dataRows2.Length - 1)
                                            //{
                                            //    stringBuilder.Append(StringHelper.ReplaceSqlValue(item.GetValue(row2["FName"].ToString().Trim()).ToString()));
                                            //}
                                            //else
                                            //{
                                            if (row2["FieldType"].ToString()==""||row2["FieldType"].ToString() == "字段")
                                            {
                                                if (isFirstField == false)
                                                {
                                                    stringBuilder.Append(",");
                                                }
                                                //if (row2["DataSource"].ToString() == "输入参数")
                                                //{
                                                //    stringBuilder.Append(StringHelper.ReplaceSqlValue(keyValuesHeaders[row2["FName"].ToString()]) + ",");
                                                //}
                                                //else
                                                //{
                                               
                                                    if (item[FName] == null)
                                                    {
                                                        stringBuilder.Append(string.IsNullOrEmpty(DefaultValue) ? "NULL" : "'" + DefaultValue + "'");
                                                    }
                                                    else
                                                    {
                                                        if (dtDateTime.Select("TABLE_NAME='" + APSTableNameTemp + "' AND COLUMN_NAME='" + FNameAPS + "'").Length > 0)
                                                        {
                                                            //DateTime? dtValue = StringHelper.ReverseToDateTime(item[FName].ToString().Trim());
                                                            //if (dtValue.HasValue)
                                                            //{
                                                            //    stringBuilder.Append("'" + dtValue.Value.ToString("yyyy-MM-dd HH:mm:ss") + "',");
                                                            //}
                                                            //else
                                                            //{
                                                            //    stringBuilder.Append("NULL,");
                                                            //}
                                                            string dd = StringHelper.ReplaceSqlValue(item[FName].ToString().Trim().ToString());
                                                            if (dd != "NULL")
                                                                stringBuilder.Append("CASE WHEN ISDATE(" + dd + ")=1 THEN " + dd + " ELSE NULL END");
                                                            else
                                                                stringBuilder.Append("NULL");
                                                        }
                                                        else
                                                            stringBuilder.Append(StringHelper.ReplaceSqlValue(item[FName].ToString().Trim().ToString()));

                                                    }

                                          
                                                if (isFirstField == true)
                                                {
                                                    isFirstField = false;
                                                }

                                                //}
                                            }
                                            else if (row2["FieldType"].ToString() == "对象")
                                            {
                                                if (isFirstField == false)
                                                {
                                                    stringBuilder.Append(",");
                                                }
                                           
                                                    if (item[fs[0]].Type == JTokenType.Null || item[fs[0]][fs[1]].Type == JTokenType.Null)
                                                    {
                                                        stringBuilder.Append(string.IsNullOrEmpty(DefaultValue) ? "NULL" : "'" + DefaultValue + "'");
                                                    }
                                                    else
                                                    {

                                                        stringBuilder.Append(StringHelper.ReplaceSqlValue(item[fs[0]][fs[1]].ToString()));

                                                    }
                                               

                                                if (isFirstField == true)
                                                {
                                                    isFirstField = false;
                                                }

                                                //}
                                            }
                                            else
                                            {

                                                //数组字段

                                                //if (index == dataRows2.Length - 1)
                                                //{
                                                //    stringBuilder.Append(StringHelper.ReplaceSqlValue(item.GetValue(row2["FName"].ToString().Trim()).ToString()));
                                                //}
                                                //else
                                                //{

                                                //if (row3["DataSource"].ToString() == "输入参数")
                                                //{
                                                //    dicTable[FNameAPS] +=StringHelper.ReplaceSqlValue(keyValuePairs[row2["FName"].ToString()]) + ",";
                                                //}
                                                //else
                                                //{
                                                JArray jItemArray = item[FName] as JArray;

                                                if (jItemArray != null && jItemArray.Count > 0)
                                                {
                                                    dicTable[FNameAPS].Result = true;
                                                    foreach (JToken jItem in jItemArray)
                                                    {
                                                        if (dicIsFirst[FNameAPS])
                                                        {

                                                            // for (int i = 0; i < dicTable.Count; i++)
                                                            {
                                                                dicTable[FNameAPS].Msg += "SELECT ";
                                                            }
                                                            dicIsFirst[FNameAPS] = false;
                                                        }
                                                        else
                                                        {

                                                            // for (int i = 0; i < dicTable.Count; i++)
                                                            {
                                                                dicTable[FNameAPS].Msg += " UNION ALL SELECT  ";
                                                            }
                                                        }
                                                        foreach (DataRow row3 in dataTable4.Select(" ParentFieldID=" + row2["FieldID"]))
                                                        {

                                                            string ParentFieldSource = row3["ParentFieldSource"].ToString();
                                                            if (!string.IsNullOrEmpty(ParentFieldSource))
                                                            {
                                                                dicTable[FNameAPS].Msg += StringHelper.ReplaceSqlValue(item[ParentFieldSource] == null ? "" : item[ParentFieldSource].ToString()) + ",";
                                                            }
                                                            else
                                                            {
                                                                dicTable[FNameAPS].Msg += StringHelper.ReplaceSqlValue(jItem[row3["FName"].ToString()] == null ? "" : jItem[row3["FName"].ToString()].ToString()) + ",";

                                                            }


                                                        }
                                                        dicTable[FNameAPS].Msg += "1";
                                                    }

                                                }


                                                // }





                                            }

                                            // }


                                        }
                                        if (dataRows2.Where(m => m["FNameAPS"].ToString().ToLower() == "status").Count() == 0)
                                        {
                                            stringBuilder.Append(",1");//status默认1
                                        }

                                        if (rowIndex % 5000 == 0 && rowIndex > 0)
                                        {
                                            try
                                            {
                                                isFirst = true;

                                                foreach (var children in dicTable)
                                                {
                                                    if (children.Value.Result)
                                                    {
                                                        stringBuilder.Append(";" + children.Value.Msg);
                                                        dicIsFirst[children.Key] = true;
                                                    }
                                                }
                                                SqlHelper.ExecuteNonQuery(stringBuilder.ToString()); ;
                                                stringBuilder = new StringBuilder(fields);

                                                getChildrenFields(dataTable4, dataRows2, dicTable);

                                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "FID" + FID + InterfaceDescription + thisRuningThread.Description, null, null);
                                                thisRuningThread.Description = InterfaceDescriptionOutput + "执行插入,第" + rowIndex + ",共" + count + ",进度:" + string.Format("{0:P2}", (rowIndex / count)) + "，时间：" + DateTime.Now;
                                            }
                                            catch (Exception ex)
                                            {
                                                isRead = false;
                                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, "WebAPIInvoke1" + InterfaceDescription + ex.Message, null, null);
                                            }
                                        }
                                        rowIndex++;
                                    }

                                    if (stringBuilder.Length > 0 && count > 0)
                                    {
                                        isFirst = true;
                                        foreach (var children in dicTable)
                                        {
                                            if (children.Value.Result)
                                            {
                                                stringBuilder.Append(";" + children.Value.Msg);
                                                dicIsFirst[children.Key] = true;
                                            }
                                        }
                                        SqlHelper.ExecuteNonQuery(stringBuilder.ToString());
                                        thisRuningThread.Description = InterfaceDescriptionOutput + "执行插入,第" + rowIndex + ",共" + count + ",进度:" + string.Format("{0:P2}", (rowIndex / count)) + "，时间：" + DateTime.Now;
                                        //stringBuilder = new StringBuilder(fields);
                                    }

                                    if (!string.IsNullOrEmpty(afterInsertSQL) && isRead == false)
                                    {
                                        SqlHelper.ExecuteNonQuery(afterInsertSQL + ";" + afterUpdateSQL);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    isRead = false;
                                    systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, "WebAPIInvoke2" + InterfaceDescription + ex.Message + "接口返回数据：", null, null);
                                    thisRuningThread.RuningCount = -100;
                                    thisRuningThread.ErrorMsg = ex.Message;
                                }

                            }

                            #endregion



                        }
                    }
                    finally
                    {
                        // 确保资源被释放（兼容 .NET Framework 4.8）
                        if (requestStream != null)
                        {
                            requestStream.Dispose();
                        }
                        if (response != null)
                        {
                            response.Close();
                            response.Dispose();
                        }
                        if (request != null)
                        {
                            request.Abort(); // .NET 4.8 中 WebRequest 不实现 IDisposable，使用 Abort()
                        }
                    }
                    cycleIndex++;

                }



            }
            catch (Exception ex)
            {

                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescription + "接口获取数据错误，" + ex.Message + ex.StackTrace + DateTime.Now, null, null);
                thisRuningThread.RuningCount = -100;
                thisRuningThread.ErrorMsg = ex.Message;
            }
            finally
            {
                lock (obj)
                {
                    thisRuningThread.RuningCount--;
                }
            }

        }
        /// <summary>
        /// 过滤和处理从数据库读取的 JSON 字符串
        /// 处理转义字符、多余空白字符，确保 JSON 格式正确
        /// </summary>
        /// <param name="jsonFromDatabase">从数据库读取的原始 JSON 字符串</param>
        /// <returns>处理后的干净 JSON 字符串</returns>
        public static string FilterAndCleanJsonString(string jsonFromDatabase)
        {
            if (string.IsNullOrWhiteSpace(jsonFromDatabase))
            {
                return string.Empty;
            }

            string cleanedJson = jsonFromDatabase;

            // 1. 移除首尾空白字符
            cleanedJson = cleanedJson.Trim();

            // 2. 处理转义字符：如果数据库存储的是转义后的字符串（如 \"），需要还原
            // 情况1：数据库存储的是 "{\"key\":\"value\"}" 格式
            // 先尝试使用正则表达式处理常见的转义字符
            cleanedJson = Regex.Unescape(cleanedJson);

            // 3. 如果上面处理后还有问题，手动处理常见的转义字符
            // 处理转义的双引号 \" -> "
            cleanedJson = cleanedJson.Replace("\\\"", "\"");
            // 处理转义的反斜杠 \\ -> \
            cleanedJson = cleanedJson.Replace("\\\\", "\\");
            // 处理转义的换行符 \n -> 实际换行（如果需要保留换行，可以注释掉）
            // cleanedJson = cleanedJson.Replace("\\n", "\n");
            // 处理转义的制表符 \t -> 实际制表符
            // cleanedJson = cleanedJson.Replace("\\t", "\t");

            // 4. 移除 JSON 中多余的空白字符（保留必要的空格）
            // 移除多个连续的空格，但保留单个空格（在引号外的）
            // 注意：这里要小心，不要破坏 JSON 结构
            // 先移除多余的空白行
            cleanedJson = Regex.Replace(cleanedJson, @"\r\n\s*\r\n", "\r\n");
            cleanedJson = Regex.Replace(cleanedJson, @"\n\s*\n", "\n");

            // 5. 压缩 JSON：移除不必要的空格（可选，如果需要紧凑格式）
            // 注意：这会移除所有不必要的空格，包括换行和缩进
            // 如果 API 要求格式化的 JSON，可以注释掉这部分
            // cleanedJson = Regex.Replace(cleanedJson, @"\s+", " ");
            // cleanedJson = Regex.Replace(cleanedJson, @"\s*{\s*", "{");
            // cleanedJson = Regex.Replace(cleanedJson, @"\s*}\s*", "}");
            // cleanedJson = Regex.Replace(cleanedJson, @"\s*\[\s*", "[");
            // cleanedJson = Regex.Replace(cleanedJson, @"\s*\]\s*", "]");
            // cleanedJson = Regex.Replace(cleanedJson, @"\s*:\s*", ":");
            // cleanedJson = Regex.Replace(cleanedJson, @"\s*,\s*", ",");

            // 6. 验证 JSON 格式（可选，如果需要验证）
            // 简单检查：确保以 { 或 [ 开头，以 } 或 ] 结尾
            cleanedJson = cleanedJson.Trim();
            if (!string.IsNullOrEmpty(cleanedJson))
            {
                char firstChar = cleanedJson[0];
                char lastChar = cleanedJson[cleanedJson.Length - 1];

                // 如果不是有效的 JSON 开始/结束字符，尝试修复
                if ((firstChar != '{' && firstChar != '[') ||
                    (lastChar != '}' && lastChar != ']'))
                {
                    // 尝试移除可能存在的额外引号
                    if (cleanedJson.StartsWith("\"") && cleanedJson.EndsWith("\""))
                    {
                        cleanedJson = cleanedJson.Substring(1, cleanedJson.Length - 2);
                    }
                }
            }

            // 7. 最终清理：移除首尾空白
            cleanedJson = cleanedJson.Trim();

            return cleanedJson;
        }

        private static void getChildrenFields(DataTable dataTable4, DataRow[] dataRows2, Dictionary<string, WebApiResult> dicTable)
        {
            dicTable.Clear();
            foreach (DataRow row2 in dataRows2)
            {
                string FNameAPS = row2["FNameAPS"].ToString().Trim();
                if (row2["FieldType"].ToString() != "数组")
                {
                    // fields += FNameAPS + ",";
                }
                else
                {



                    DataRow[] row3s = dataTable4.Select(" ParentFieldID=" + row2["FieldID"]);
                    int row3Index = 1;
                    //如果来源是数组，则读取相关的表


                    foreach (DataRow row3 in row3s)
                    {

                        if (dicTable.ContainsKey(FNameAPS) == false)
                        {
                            dicTable.Add(FNameAPS, new WebApiResult() { Msg = "INSERT INTO " + FNameAPS + "(" });
                        }
                        dicTable[FNameAPS].Msg += row3["FNameAPS"].ToString().Trim() + ",";
                        if (row3Index == row3s.Length - 1)
                        {
                            dicTable[FNameAPS].Msg += "Status)";
                        }
                        row3Index++;
                    }
                }


            }
        }

        /// <summary>
        /// 执行SAP查询与保存到APS的过程
        /// </summary>
        /// <param name="dataTable3">输出表</param>
        /// <param name="dataTable4">输出表参数</param>
        /// <param name="row">当前循环的接口行</param>
        /// <param name="InterfaceDescription">接口描述</param>
        /// <param name="fun003">SAP变量</param>
        /// <param name="isDelete">是否删除</param>
        private async static Task SapInvoke(DataTable dataTable3, DataTable dataTable4, DataRow row, string InterfaceDescription, IRfcFunction fun003, bool isDelete, Dictionary<string, object> keyValuePairs, string ThreadGroup, string FID)
        {

            //Task task= Task.Run(() =>
            {
                // 使用线程安全的方式访问 ConcurrentDictionary
                List<RuningInterFace> threadList;
                if (!lstRuningInterFace.TryGetValue(ThreadGroup, out threadList) || threadList == null)
                {
                    return;
                }
                RuningInterFace thisRuningThread;
                lock (threadList)
                {
                    thisRuningThread = threadList.Where(m => m.FID == FID).FirstOrDefault();
                }
                if (thisRuningThread == null)
                {
                    return;
                }
                if (thisRuningThread != null)
                {
                    try
                    {

                        if (isDelete == true)
                        {// 循环的不统计，避免日志太多
                            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescription + "开始触发" + DateTime.Now, null, null);
                        }
                        try
                        {
                            fun003.Invoke(destination);
                        }
                        catch (Exception ex)
                        {
                            Task.Delay(1000);

                            try
                            {
                                fun003.Invoke(destination);
                            }
                            catch (Exception ex1)
                            {
                                Task.Delay(2000);

                                fun003.Invoke(destination);


                            }
                        }





                        if (isDelete == true)
                        {//循环的不统计，避免日志太多
                            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescription + "从SAP读取数据完成，" + DateTime.Now, null, null);
                        }


                        DataRow[] drOutputName = dataTable3.Select("FID=" + row["FID"]);
                        // bool isMDD04new = dataTable3.Select("FID=" + row["FID"] + " AND FName='MDMAT'").Length > 0;     //新版MD04需要使用物料关联
                        //循环参数表
                        foreach (DataRow dr in drOutputName)
                        {
                            string APSTableNameTemp = dr["APSTableNameTemp"].ToString();
                            string OutputName = dr["OutputName"].ToString();
                            string InterfaceDescriptionOutput = InterfaceDescription + "接口的输出参数：" + dr["InterfaceDescription"].ToString();
                            IRfcTable rfcFields = fun003.GetTable(OutputName);
                            DataRow[] dataRows2 = dataTable4.Select("EID=" + dr["EID"]);

                            Dictionary<string, IRfcTable> listOutput = new Dictionary<string, IRfcTable>();
                            string fields = "INSERT INTO " + APSTableNameTemp + " (";
                            foreach (DataRow row2 in dataRows2)
                            {
                                fields += row2["FNameAPS"].ToString().Trim() + ",";
                                string AntherOutputName = row2["OutputName"].ToString();
                                if (!string.IsNullOrEmpty(AntherOutputName))
                                {//字段来源与其他的同级别输出表
                                    if (listOutput.ContainsKey(AntherOutputName) == false)
                                    {
                                        try
                                        {
                                            listOutput.Add(AntherOutputName, fun003.GetTable(AntherOutputName));
                                        }
                                        catch (Exception ex)
                                        {

                                        }

                                    }

                                }
                            }
                            fields = fields + "Status)";
                            if (isDelete)
                            {

                                SqlHelper.ExecuteNonQuery(" truncate table " + APSTableNameTemp);
                            }

                            StringBuilder stringBuilder = new StringBuilder(fields);
                            decimal rowIndex = 0;
                            int count = rfcFields.Count;
                            thisRuningThread.DataCount += count;
                            if (isDelete == true)
                            {//循环的不统计，避免日志太多
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescriptionOutput + "开始循环，记录数：" + count + "，时间：" + DateTime.Now, null, null);
                            }
                            if (count > 0)
                            {

                                //  IsRuning = "第" + rowIndex + ",共" + count + ",进度:" + string.Format("{0:P2}", (rowIndex / count));
                            }
                            bool isFirst = true;
                            int r = 0;//几个表关联的时候，行的索引
                                      //bool isToNext = false;//是否将行索引累计
                            foreach (var item in rfcFields)
                            {

                                if (isFirst)
                                {
                                    stringBuilder.Append("SELECT ");
                                    isFirst = false;
                                }
                                else
                                {
                                    stringBuilder.Append(" UNION ALL SELECT ");

                                }

                                foreach (DataRow row2 in dataRows2)
                                {
                                    string AntherOutputName = row2["OutputName"].ToString();

                                    if (!string.IsNullOrEmpty(AntherOutputName))
                                    {//字段来源与其他的同级别输出表
                                        try
                                        {

                                            if (row2["Relevancy"].ToString() == "ALINE")
                                            {
                                                if (item["ALINE"].ToString() != "")
                                                {
                                                    r = int.Parse(item.GetValue("ALINE").ToString());
                                                }
                                                else
                                                {
                                                    r = 0;
                                                }
                                            }
                                            if (r > 0)
                                            {

                                                //if (isMDD04new)
                                                //{
                                                //    var itemAn = listOutput[AntherOutputName].Where(m => m.GetValue("MATNR").ToString() == item.GetValue("MATNR").ToString()).ToArray();
                                                //    if (itemAn != null && itemAn.Length > 0)
                                                //    {
                                                //        try
                                                //        {
                                                //            stringBuilder.Append(StringHelper.ReplaceSqlValue(itemAn[r - 1].GetValue(row2["FName"].ToString().Trim()).ToString()) + ",");
                                                //        }
                                                //        catch (Exception ex)
                                                //        {

                                                //            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescriptionOutput + "尝试关联其他表出错" + ex.StackTrace + "，当前接口的数据：" + itemAn.ToJsonLegacy(), null, null);


                                                //        }


                                                //    }
                                                //    else
                                                //    {
                                                //        stringBuilder.Append("NULL,");
                                                //    }

                                                //}
                                                //else
                                                //{
                                                stringBuilder.Append(StringHelper.ReplaceSqlValue(listOutput[AntherOutputName][r - 1].GetValue(row2["FName"].ToString().Trim()).ToString()) + ",");
                                                //}




                                            }
                                            else
                                            {
                                                stringBuilder.Append("NULL,");
                                            }

                                        }
                                        catch (Exception ex)
                                        {

                                            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescriptionOutput + "尝试关联其他表出错" + ex.StackTrace + "，当前接口的数据：" + fun003.ToSafeString(), null, null);


                                        }


                                    }
                                    else
                                    {


                                        if (row2["DataSource"].ToString() == "输入参数")
                                        {
                                            stringBuilder.Append(StringHelper.ReplaceSqlValue(keyValuePairs[row2["FName"].ToString()].ToString()) + ",");
                                        }
                                        else
                                        {
                                            stringBuilder.Append(StringHelper.ReplaceSqlValue(item.GetValue(row2["FName"].ToString().Trim()).ToString()) + ",");
                                        }
                                    }



                                }
                                stringBuilder.Append("1");//status默认1
                                if (rowIndex % 3000 == 0 && rowIndex > 0)
                                {
                                    try
                                    {
                                        isFirst = true;
                                        SqlHelper.ExecuteNonQuery(stringBuilder.ToString());
                                        stringBuilder = new StringBuilder(fields);
                                        if (isDelete == true)
                                        {//循环的不统计，避免日志太多
                                            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, InterfaceDescriptionOutput + "执行插入,第" + rowIndex + ",共" + count + ",进度:" + string.Format("{0:P2}", (rowIndex / count)) + "，时间：" + DateTime.Now, null, null);
                                            if (count > 0)
                                            {
                                                //lock (obj)
                                                thisRuningThread.Description = "第" + rowIndex + ",共" + count + ",进度:" + string.Format("{0:P2}", (rowIndex / count));
                                            }
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescriptionOutput + ex.Message, null, null);
                                    }
                                }
                                rowIndex++;

                                //  r++;
                            }
                            try
                            {
                                if (stringBuilder.Length > 0 && count > 0)
                                {
                                    isFirst = true;
                                    SqlHelper.ExecuteNonQuery(stringBuilder.ToString());

                                    stringBuilder = new StringBuilder(fields);
                                }
                            }
                            catch (Exception ex)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescriptionOutput + ex.Message + stringBuilder.ToString(), null, null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceName + InterfaceDescription + "获取ERP数据错误" + ex.Message + "，当前接口的数据：" + fun003.ToSafeString() + DateTime.Now, null, null);
                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, InterfaceDescription + "接口获读取过程错误，" + ex.StackTrace + "，当前接口的数据：" + fun003.ToSafeString() + DateTime.Now, null, null);
                    }
                    finally
                    {

                    }
                }



            }
            //);




        }
        [Serializable]
        public class SplitOrderObj
        {
            public string OrderNo
            {
                get; set;
            }
            /// <summary>
            /// 卸货点
            /// </summary>
            public string DefaultLineName { get; set; }
            public string Code { get; set; }
            /// <summary>
            /// 工厂代码
            /// </summary>
            public string Extend12 { get; set; }
            public string Qty { get; set; }
            public string WorkOrderTypeID { get; set; }
            public string SalesOrderNo { get; set; }
            public string SalesLineNum { get; set; }
            public string ERPEndDate { get; set; }
            public string ERPStartDate { get; set; }
            public string NewQty { get; set; }
            public bool IsNew { get; set; }
            public string NewOrderNo
            {
                get; set;
            }
            public long OrderID { get; set; }
            public string Account { get; set; }
            public string Name { get; set; }
            public string Msg { get; set; }
            public int RowIndex { get; set; }
            public DateTime CreatedOn { get; set; } = DateTime.Now;
            public DateTime ModifyedOn { get; set; } = DateTime.Now;
            public string KeyValue { get; set; }
            /// <summary>
            /// 订单+行号
            /// </summary>
            public string RowNo { get; set; }
            public int Status { get; set; } = 5;
            public string StatusName { get; set; } = "推送失败";
            /// <summary>
            /// 用户状态
            /// </summary>
            public string Extend19 { get; set; }
            /// <summary>
            /// 计数器
            /// </summary>
            public string MachineCode { get; set; }
            /// <summary>
            /// 判定是否可以直接创建订单，非拆单的情况可以
            /// </summary>
            public bool IsOK { get; set; }
            /// <summary>
            /// 生产订单备注
            /// </summary>
            public string Extend18 { get; set; }

        }
        /// <summary>
        /// 同步SAP生产订单
        /// </summary>
        /// <param name="jArray">前端数据</param>
        /// <param name="r2">结果</param>
        /// <param name="rel">传X为要释放</param>
        /// <param name="dev_Account">当前操作的账号</param>
        /// <returns></returns>
        public static async Task<WebApiResult> UpdateOrderStartDate(JArray jArray, string rel, V_Dev_Account dev_Account)
        {
            WebApiResult webApiResult = new WebApiResult();
            bool result = false;
            string allMsg = "";
            string r2 = "";
            if (jArray.Count > 0)
            {


                List<string> listUpdateOrder = new List<string>();
                foreach (JObject jObject in jArray)
                {

                    if (!string.IsNullOrEmpty(jObject["OrderNo"].ToString()))
                    {
                        if (jObject["Status"].ToString() != "0")
                        {
                            continue;
                        }
                        IRfcFunction fun002 = repository.CreateFunction("ZMES_MAINDATA_043");//同步接口

                        // matra.SetValue("DATE_TO", d2);

                        var itb = fun002.GetTable("IT_DATA");
                        Dictionary<string, List<string>> lstOrderPlanChangeID = new Dictionary<string, List<string>>();
                        string orderNo = "";
                        itb.Insert();

                        result = true;

                        //物料主数据
                        itb.CurrentRow.SetValue("AUFNR", jObject["OrderNo"].ToString());//单号

                        r2 = jObject["OrderNo"].ToString();
                        string DefaultLineName = jObject["DefaultLineName"].ToString();
                        orderNo += r2;
                        // fun002.SetValue("I_DISPO", ControlID);//MRP控制者
                        if (!string.IsNullOrEmpty(jObject["NewEndDate"].ToString()))
                        {
                            itb.CurrentRow.SetValue("GLTRP", string.Format("{0:yyyy-MM-dd}", DateTime.Parse(jObject["NewEndDate"].ToString())));//完成日期
                        }
                        else
                        {
                            r2 += "结束日期日期不能为空";
                            result = false;
                        }

                        if (!string.IsNullOrEmpty(jObject["NewStartDate"].ToString()))
                        {
                            itb.CurrentRow.SetValue("GSTRP", string.Format("{0:yyyy-MM-dd}", DateTime.Parse(jObject["NewStartDate"].ToString())));//开始日期
                        }
                        else
                        {
                            r2 += "开始日期不能为空";
                            result = false;
                        }
                        if (!string.IsNullOrEmpty(jObject["Qty"].ToString()))
                        {
                            itb.CurrentRow.SetValue("MENGE", jObject["Qty"].ToString());//开始日期
                        }
                        else
                        {
                            r2 += "数量不能为空";
                            result = false;
                        }
                        itb.CurrentRow.SetValue("ABLAD", jObject["DefaultLineName"].ToString());//卸货点
                        itb.CurrentRow.SetValue("AENAM1", "T" + dev_Account.Account);//账号
                        itb.CurrentRow.SetValue("ZNAME1", dev_Account.Name);//姓名
                        itb.CurrentRow.SetValue("HOSTIP", LicenceRuntime.ClientIpAddress);//账号
                                                                                                       // try { itb.CurrentRow.SetValue("HOST", System.Net.Dns.GetHostEntry(LicenceRuntime.ClientIpAddress).HostName); } catch { }//电脑名称

                        List<string> lst = new List<string>();
                        lst.Add(jObject["OrderPlanChangeID"].ToString());
                        lst.Add(DefaultLineName);
                        if (result)
                        {
                            itb.CurrentRow.SetValue("FLAG", rel);
                            lstOrderPlanChangeID.Add(jObject["OrderNo"].ToString(), lst);
                        }



                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送, r2, dev_Account, null);

                        SqlHelper.ExecuteNonQuery(string.Format("UPDATE   APS_OrderPlanChange SET Status=6,ModifyedOn=GETDATE() where  OrderPlanChangeID={0};", jObject["OrderPlanChangeID"].ToString()));

                        if (result)
                        {
                            try
                            {
                                fun002.SetValue("IT_DATA", itb);
                                fun002.Invoke(destination);
                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "同步SAP生产订单日期,执行完成", dev_Account, null);
                                string r1 = "";
                                try
                                {
                                    r1 = fun002.GetString("O_FLAG");
                                }
                                catch (Exception ex)
                                {
                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "获取O_FLAG错误" + ex.Message, dev_Account, null);
                                }

                                result = r1 == "S";
                                r2 = "";
                                try
                                {
                                    r2 = fun002.GetString("O_TEXT");
                                }
                                catch (Exception ex)
                                {
                                    allMsg += orderNo + "同步失败,";
                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "获取O_TEXT错误" + ex.Message, dev_Account, null);
                                }
                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "同步SAP生产订单日期,输出结果：" + result + r2, dev_Account, null);

                                // if (result)
                                {



                                    IRfcTable ot = fun002.GetTable("OT_RETURN");
                                    string sql = "";
                                    int index = 0;
                                    try
                                    {


                                        foreach (var row in ot)
                                        {
                                            try
                                            {
                                                orderNo = row.GetValue("AUFNR").ToString();

                                                string flag = row.GetValue("FLAG").ToString();
                                                string msg = row.GetValue("MSG").ToString();
                                                if (msg == null)
                                                {
                                                    msg = "";
                                                }
                                                string flag1 = row.GetValue("FLAG1").ToString();
                                                string msg1 = row.GetValue("MSG1").ToString();
                                                if (msg1 == null)
                                                {
                                                    msg1 = "";
                                                }
                                                string id = "";
                                                List<string> list = new List<string>();
                                                lstOrderPlanChangeID.TryGetValue(orderNo, out list);
                                                id = list[0];
                                                if (flag == "S")
                                                {
                                                    // string DefaultLineName = ot.GetValue("ABLAD").ToString();

                                                    if (!string.IsNullOrEmpty(id))
                                                    {
                                                        sql += string.Format(@"UPDATE   APS_OrderPlanChange SET Status=2,ModifyedOn=GETDATE() where  OrderPlanChangeID={0};

UPDATE B SET    b.ERPStartDate=a.NewStartDate,b.ERPEndDate=a.NewEndDate
,b.OldERPStartDate=a.NewStartDate,b.OldERPEndDate=a.NewEndDate
,b.DefaultLineName={1},b.Extend9={1},b.qty=a.qty,b.ModifyedOn = Getdate()
FROM APS_OrderPlanChange a
inner join APS_Order b on a.OrderID=b.OrderID
where a.OrderPlanChangeID={0}
                                    ", id, StringHelper.ReplaceSqlValue(list[1]));
                                                        listUpdateOrder.Add(orderNo);

                                                    }
                                                    allMsg += orderNo + "同步成功，";

                                                }
                                                else // if (flag == "E" || flag1 == "E")
                                                {
                                                    sql += string.Format(@"UPDATE   APS_OrderPlanChange SET Status=7,Remark1='{1}',ModifyedOn=GETDATE() where  OrderPlanChangeID={0};

 
                                    ", id, StringHelper.ReplaceSQL((msg1 + msg)));
                                                    r2 = "，订单：" + orderNo;
                                                    if (flag == "E")
                                                    {

                                                        if (!string.IsNullOrEmpty(msg))
                                                        {
                                                            r2 += orderNo + "修改失败，原因：" + msg + ";";
                                                        }

                                                    }
                                                    if (flag1 == "E")
                                                    {

                                                        if (!string.IsNullOrEmpty(msg1))
                                                        {
                                                            r2 += orderNo + "下达失败，原因：" + msg1 + ";";
                                                        }

                                                    }
                                                    allMsg += r2 + "，";
                                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "接口推送之后，接口返回错误，错误：" + r2, dev_Account, null);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                allMsg += orderNo + "同步失败,";
                                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "接口推送之后，在循环过程中，错误：" + ex.Message, dev_Account, null);
                                            }
                                            index++;

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        allMsg += orderNo + "同步失败,";
                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "接口推送之后，在循环过程，错误：" + ex.Message, dev_Account, null);
                                    }
                                    if (!string.IsNullOrEmpty(sql))
                                    {
                                        try
                                        {
                                            SqlHelper.ExecuteNonQuery(sql);
                                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "执行的语句" + sql, dev_Account, null);
                                        }
                                        catch (Exception ex)
                                        {
                                            allMsg += orderNo + "同步失败,";
                                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "接口推送之后，APS执行更新，错误：" + ex.Message, dev_Account, null);
                                        }
                                    }
                                    else
                                    {
                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "没有拼接到SQL语句", dev_Account, null);
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                allMsg += orderNo + "同步失败,";
                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "推送时错误：" + ex.Message, dev_Account, null);
                            }

                        }
                        else
                        {
                            allMsg += orderNo + "同步失败,";
                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "false", dev_Account, null);
                        }

                    }


                }

                Task.Run(() =>
                {
                    //SAP存在缓存的问题
                    // GetOrderInfoByOrderNo(listUpdateOrder);
                });

                SqlHelper.ExecuteNonQuery("EXEC   P_aps_autoplan");


            }
            else
            {
                allMsg = "没有数据";
            }
            r2 = allMsg;
            webApiResult.Result = result;
            webApiResult.Msg = r2; ;
            return webApiResult;

        }


        /// <summary>
        /// 释放生产订单
        /// </summary>
        /// <param name="jArray">前端数据</param>
        /// <param name="r2">结果</param>
        /// <param name="rel">传X为要释放</param>
        /// <param name="dev_Account">当前操作的账号</param>
        /// <returns></returns>
        public static bool TPOrderToREL(JArray jArray, Dictionary<string, string> lstOut, V_Dev_Account dev_Account)
        {
            bool result = false;
            if (jArray.Count > 0)
            {
                string rel = "X";
                string OrderIDs = "0";
                foreach (JObject jObject in jArray)
                {

                    if (!string.IsNullOrEmpty(jObject["OrderID"].ToString()))
                    {
                        OrderIDs += "," + jObject["OrderID"].ToString();
                    }
                }
                DataTable dataTable = SqlHelper.ExecuteDataTable(string.Format(@"
SELECT OrderID,OrderNo,ERPStartDate,ERPEndDate,Extend11,Qty
      ,[DefaultLineName]
    
  FROM  [dbo].[APS_Order]
WHERE OrderID IN({0})
                ", OrderIDs));

                foreach (DataRow dataRow in dataTable.Rows)
                {
                    string r2 = "";

                    if (!string.IsNullOrEmpty(dataRow["OrderID"].ToString()))
                    {
                        string orderNo = "";
                        orderNo = r2 = dataRow["OrderNo"].ToString();
                        lstOut.Add(orderNo, "");
                        string DefaultLineName = dataRow["DefaultLineName"].ToString();
                        if (dataRow["Extend11"].ToString().ToUpper().IndexOf("CRTD") == -1)
                        {
                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "释放生产订单错误，当前订单：" + orderNo + "，状态:" + dataRow["Extend11"].ToString(), dev_Account, null);
                            lstOut[orderNo] = "释放生产订单错误，当前订单：" + orderNo + "，状态:" + dataRow["Extend11"].ToString();
                            continue;
                        }


                        result = true;
                        IRfcFunction fun002 = repository.CreateFunction("ZPP_RELEASE_ORDER_01");//同步接口

                        // matra.SetValue("DATE_TO", d2);


                        Dictionary<string, string> lstOrderPlanChangeID = new Dictionary<string, string>();




                        //物料主数据
                        fun002.SetValue("I_AUFNR", dataRow["OrderNo"].ToString());//单号


                        orderNo = dataRow["OrderNo"].ToString();



                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送, r2, dev_Account, null);
                        if (result)
                        {
                            try
                            {

                                fun002.Invoke(destination);
                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "释放生产订单推送成功", dev_Account, null);
                                string r1 = "";
                                string msg = "";
                                try
                                {
                                    r1 = fun002.GetString("O_FLAG");
                                    msg = fun002.GetString("O_TEXT");
                                }
                                catch (Exception ex)
                                {
                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "释放生产订单获取O_FLAG错误" + ex.Message, dev_Account, null);
                                }

                                result = r1 == "S";

                                if (result)
                                {
                                    // string DefaultLineName = ot.GetValue("ABLAD").ToString();
                                    string sql = "";
                                    if (!string.IsNullOrEmpty(orderNo))
                                    {
                                        sql = string.Format(@"  
UPDATE A SET  Extend11=REPLACE(Extend11,'CRTD','REL') ,ModifyedOn = Getdate()
  FROM [dbo].[APS_Order] A
WHERE OrderNo='{0}'
                                    ", orderNo);
                                        SqlHelper.ExecuteNonQuery(sql); ;
                                    }

                                }
                                else // if (flag == "E" || flag1 == "E")
                                {

                                    r2 = "，订单：" + orderNo + msg;

                                    lstOut[orderNo] = r2;
                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "释放生产订接口返回错误，错误：" + r2, dev_Account, null);
                                }
                            }
                            catch (Exception ex)
                            {

                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "释放生产订" + ex.Message, dev_Account, null);
                            }

                        }
                        else
                        {
                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, "false", dev_Account, null);
                        }
                    }


                }


            }


            return result;

        }


        /// <summary>
        /// 拆分订单
        /// </summary>
        /// <param name="jArray">需要操作的数据</param>
        /// <param name="lstOut">最终操作的结果</param>
        /// <param name="dev_Account">操作账号</param>
        /// <returns></returns>
        public static async Task<WebApiResult> SplitOrder(JArray jArray, Dictionary<string, string> lstOut, V_Dev_Account dev_Account)
        {

            WebApiResult webApiResult = new WebApiResult();
            webApiResult.Result = true;
            webApiResult.Msg = "推送失败";
            int rowIndex = 0;

            if (jArray.Count > 0)
            {
                foreach (JObject jObject in jArray)
                {

                    string orderNo = "";

                    orderNo = jObject["OrderNo"].ToString();
                    string rowNo = orderNo + "-" + rowIndex;
                    jObject.Add("RowNo", rowNo);
                    lstOut.Add(rowNo, "");
                    string ERPStartDate = "";
                    string ERPEndDate = "";
                    string DefaultLineName = jObject["DefaultLineName"].ToString();
                    string newQty = "";
                    string code = jObject["Code"].ToString();
                    string extend12 = jObject["Extend12"].ToString();//工厂
                    string WorkOrderTypeID = jObject["WorkOrderTypeID"].ToString();
                    string SourceOrderNo = jObject["SourceOrderNo"].ToString();
                    string SalesLineNum = jObject["SalesLineNum"].ToString();
                    if (string.IsNullOrEmpty(code))
                    {
                        lstOut[rowNo] += "没有料号";
                        webApiResult.Result = false;
                    }
                    if (string.IsNullOrEmpty(extend12))
                    {
                        lstOut[rowNo] += "没有工厂";
                        webApiResult.Result = false;
                    }
                    //if (string.IsNullOrEmpty(WorkOrderTypeID))
                    //{
                    //    lstOut[rowNo] += "没有单据类型";
                    //    result = false;
                    //}
                    if (!string.IsNullOrEmpty(jObject["NewQty1"].ToString()) && int.Parse(jObject["NewQty1"].ToString()) > 0)
                    {
                        newQty = jObject["NewQty1"].ToString();//数量
                    }
                    else
                    {

                        lstOut[rowNo] += "数量不能为空";
                        webApiResult.Result = false;
                    }
                    if (!string.IsNullOrEmpty(jObject["ERPEndDate"].ToString()))
                    {
                        // ERPEndDate = jObject["ERPEndDate"].ToString();
                        ERPEndDate = string.Format("{0:yyyy-MM-dd}", jObject["ERPEndDate"].ToString());
                        //  itb.CurrentRow.SetValue("GLTRP", string.Format("{0:yyyy-MM-dd}", jObject["EndDate"].ToString()));//完成日期
                    }
                    else
                    {
                        lstOut[rowNo] = "完成日期不能为空";
                        webApiResult.Result = false;
                    }

                    if (!string.IsNullOrEmpty(jObject["ERPStartDate"].ToString()))
                    {
                        ERPStartDate = string.Format("{0:yyyy-MM-dd}", jObject["ERPStartDate"].ToString());
                    }
                    else
                    {
                        lstOut[rowNo] += "开始日期不能为空";
                        webApiResult.Result = false;
                    }
                    if (webApiResult.Result == true)
                    {
                        if (DateTime.Parse(ERPEndDate) < DateTime.Parse(ERPStartDate))
                        {
                            lstOut[rowNo] += "开始日期不能大于结束日期";
                            webApiResult.Result = false;
                        }
                    }

                    rowIndex++;
                }
                if (webApiResult.Result)
                {//全部检查完毕再执行
                    rowIndex = 0;

                    long index = RedisHelper.db.StringIncrement("splitGroupIndex");
                    string day = RedisHelper.db.StringGetSet("splitGroupIndexDay", DateTime.Now.ToString());
                    if (DateTime.Parse(day).Date != DateTime.Now.Date)
                    {
                        index = 1;
                    }
                    systemLog.SaveLog(Licence.SystemLog.SystemLogType.接口推送, "拆单推送，接收到" + jArray.Count.ToString(), dev_Account, null);
                    List<SplitOrderObj> listOrders = new List<SplitOrderObj>();
                    foreach (JObject jObject in jArray)
                    {
                        string orderNo = "";
                        orderNo = jObject["OrderNo"].ToString();
                        string rowNo = orderNo + "-" + rowIndex;
                        string ERPStartDate = "";
                        string ERPEndDate = "";
                        string DefaultLineName = jObject["DefaultLineName"].ToString();
                        string newQty = "";
                        string code = jObject["Code"].ToString();
                        string extend12 = jObject["Extend12"].ToString();//工厂
                        string WorkOrderTypeID = jObject["WorkOrderTypeID"].ToString();
                        string SalesOrderNo = "";
                        string SalesLineNum = "";
                        if (jObject.ContainsKey("SalesOrderNo"))
                        {
                            SalesOrderNo = jObject["SalesOrderNo"].ToString();
                        }
                        if (jObject.ContainsKey("SalesLineNum"))
                        {
                            SalesLineNum = jObject["SalesLineNum"].ToString();
                        }
                        String qty = jObject["Qty"].ToString();
                        string orderID = jObject["OrderID"].ToString();



                        SplitOrderObj splitOrderObj = new SplitOrderObj();
                        splitOrderObj.OrderNo = orderNo;
                        splitOrderObj.DefaultLineName = DefaultLineName;
                        splitOrderObj.Code = code;
                        splitOrderObj.Extend12 = extend12;
                        splitOrderObj.WorkOrderTypeID = WorkOrderTypeID;
                        splitOrderObj.SalesOrderNo = SalesOrderNo;
                        splitOrderObj.SalesLineNum = SalesLineNum;
                        splitOrderObj.Qty = qty;
                        splitOrderObj.IsNew = jObject["IsNew"].ToString() != "0";
                        splitOrderObj.OrderID = long.Parse(orderID);
                        splitOrderObj.Account = dev_Account.Account;
                        splitOrderObj.Name = dev_Account.Name;
                        splitOrderObj.RowIndex = rowIndex;
                        splitOrderObj.KeyValue = string.Format("{0:yyyyMMdd}-", DateTime.Now) + index.ToString().PadLeft(3, '0');
                        splitOrderObj.RowNo = rowNo;
                        if (jObject.ContainsKey("Extend18"))
                        {
                            splitOrderObj.Extend18 = jObject["Extend18"].ToString();
                        }

                        if (jObject.ContainsKey("IsOK"))
                        {
                            splitOrderObj.IsOK = true;
                        }
                        if (jObject.ContainsKey("Extend19"))
                        {
                            splitOrderObj.Extend19 = jObject["Extend19"].ToString();
                        }

                        if (jObject.ContainsKey("MachineCode"))
                        {
                            splitOrderObj.MachineCode = jObject["MachineCode"].ToString();
                        }



                        if (!string.IsNullOrEmpty(jObject["NewQty1"].ToString()))
                        {
                            newQty = jObject["NewQty1"].ToString();//数量
                            splitOrderObj.NewQty = newQty;
                        }
                        else
                        {

                            lstOut[rowNo] += "数量不能为空";
                            webApiResult.Result = false;
                        }

                        if (!string.IsNullOrEmpty(jObject["ERPEndDate"].ToString()))
                        {
                            // ERPEndDate = jObject["ERPEndDate"].ToString();
                            splitOrderObj.ERPEndDate = string.Format("{0:yyyy-MM-dd}", jObject["ERPEndDate"].ToString());
                            //  itb.CurrentRow.SetValue("GLTRP", string.Format("{0:yyyy-MM-dd}", jObject["EndDate"].ToString()));//完成日期
                        }
                        else
                        {
                            lstOut[rowNo] = "完成日期不能为空";
                            webApiResult.Result = false;
                        }

                        if (!string.IsNullOrEmpty(jObject["ERPStartDate"].ToString()))
                        {
                            splitOrderObj.ERPStartDate = string.Format("{0:yyyy-MM-dd}", jObject["ERPStartDate"].ToString());
                        }
                        else
                        {
                            lstOut[rowNo] += "开始日期不能为空";

                            webApiResult.Result = false;
                        }
                        listOrders.Add(splitOrderObj);
                        // RedisHelper.db.ListRightPush(redisPush, JsonConvert.SerializeObject(splitOrderObj)); //入栈
                        rowIndex++;
                    }
                    if (listOrders.Count > 0)
                    {


                        webApiResult = await PushSplitOrder(dev_Account, lstOut, listOrders);


                    }
                    //);
                }
            }
            else
            {
                webApiResult.Result = false;
            }


            return webApiResult;
        }
        static object objLock1 = new object();

        //记录错误的拆分分组

        // static  string redisPush = AppInfo.PushType+"SplitOrderList";
        static string pushUserStatus = AppInfo.PushType + "UserStatus";
        /// <summary>
        /// 正在同步的订单，避免同时提交（使用 ConcurrentDictionary 保证线程安全，值用于标记）
        /// </summary>
        static ConcurrentDictionary<string, bool> ListUpdateingOrder = new ConcurrentDictionary<string, bool>();

        public static async Task<WebApiResult> PushSplitOrder(V_Dev_Account dev_Account, Dictionary<string, string> lstOut, List<SplitOrderObj> listOrderNos)
        {
            List<string> listUpdateOrder = new List<string>();
            bool r = true;
            WebApiResult webApiResult = new WebApiResult() { Result = false, Msg = "推送失败" };
            if (repository == null)
            {
                webApiResult.Result = false;
                webApiResult.Msg = "与SAP通讯失败";
                return webApiResult;
            }
            try
            {

                List<string> split = new List<string>();
                bool result = false;
                if (!string.IsNullOrEmpty(listOrderNos[0].OrderNo) && ListUpdateingOrder.ContainsKey(listOrderNos[0].OrderNo))
                {
                    webApiResult.Result = false;
                    webApiResult.Msg = listOrderNos[0].OrderNo + "正在提交中，不可重复提交";
                }
                else
                {
                    // 使用线程安全的 TryAdd 方法
                    ListUpdateingOrder.TryAdd(listOrderNos[0].OrderNo, true);
                    foreach (SplitOrderObj splitOrderObj in listOrderNos)
                    {







                        try
                        {
                            string s = splitOrderObj.ToJsonLegacy();
                            if (split.Contains(splitOrderObj.KeyValue) == false)
                            {
                                split.Add(splitOrderObj.KeyValue);
                            }

                            if (splitOrderObj.IsNew == false)
                            {

                                IRfcFunction fun043 = repository.CreateFunction("ZMES_MAINDATA_043");//同步接口

                                // matra.SetValue("DATE_TO", d2);

                                var itb = fun043.GetTable("IT_DATA");


                                itb.Insert();



                                //物料主数据
                                itb.CurrentRow.SetValue("AUFNR", splitOrderObj.OrderNo);//单号






                                itb.CurrentRow.SetValue("GLTRP", splitOrderObj.ERPEndDate);//完成日期



                                itb.CurrentRow.SetValue("GSTRP", splitOrderObj.ERPStartDate);//开始日期


                                itb.CurrentRow.SetValue("MENGE", splitOrderObj.NewQty);//数量

                                itb.CurrentRow.SetValue("ABLAD", splitOrderObj.DefaultLineName);//卸货点

                                itb.CurrentRow.SetValue("AENAM1", "T" + dev_Account.Account);//账号
                                itb.CurrentRow.SetValue("ZNAME1", dev_Account.Name);//姓名
                                itb.CurrentRow.SetValue("HOSTIP", LicenceRuntime.ClientIpAddress);//账号
                                                                                                               // try { itb.CurrentRow.SetValue("HOST", System.Net.Dns.GetHostEntry(LicenceRuntime.ClientIpAddress).HostName); } catch { }//电脑名称
                                itb.CurrentRow.SetValue("FLAG", "");



                                try
                                {
                                    fun043.SetValue("IT_DATA", itb);
                                    fun043.Invoke(destination);
                                    AddSplitKey(splitOrderObj, false);

                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + "拆分生产订单,执行完成", null, null);
                                    string r1 = "";
                                    try
                                    {
                                        r1 = fun043.GetString("O_FLAG");
                                    }
                                    catch (Exception ex)
                                    {
                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, s + "拆分生产订单,获取O_FLAG错误" + ex.Message, null, null);
                                    }

                                    result = r1 == "S";
                                    string msg = "";
                                    try
                                    {
                                        msg = fun043.GetString("O_TEXT");
                                    }
                                    catch (Exception ex)
                                    {


                                    }
                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + "拆分生产订单,执行结果：" + result + msg, null, null);

                                    //  if (result)
                                    {



                                        IRfcTable ot = fun043.GetTable("OT_RETURN");
                                        string sql = "";
                                        int index = 0;
                                        try
                                        {


                                            foreach (var row in ot)
                                            {
                                                try
                                                {


                                                    string flag = row.GetValue("FLAG").ToString();
                                                    msg = row.GetValue("MSG").ToString();
                                                    if (msg == null)
                                                    {
                                                        msg = "";
                                                    }
                                                    string flag1 = row.GetValue("FLAG1").ToString();
                                                    string msg1 = row.GetValue("MSG1").ToString();
                                                    if (msg1 == null)
                                                    {
                                                        msg1 = "";
                                                    }

                                                    if (flag == "S")
                                                    {
                                                        splitOrderObj.NewOrderNo = row.GetValue("AUFNR").ToString();
                                                        // string DefaultLineName = ot.GetValue("ABLAD").ToString();
                                                        lstOut[splitOrderObj.RowNo] = splitOrderObj.OrderNo + "修改成功";
                                                        result = true;
                                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + "拆分成功：" + msg, null, null);
                                                        msg = splitOrderObj.OrderNo + "拆分数量成功";
                                                        splitOrderObj.Status = 3;
                                                        splitOrderObj.ModifyedOn = DateTime.Now;
                                                        SqlHelper.ExecuteNonQuery($@"
                                             UPDATE APS_Order
SET QTY={splitOrderObj.NewQty},ModifyedOn = Getdate()
where OrderID={splitOrderObj.OrderID}
                                            INSERT INTO [dbo].[APS_OrderSplitLog]
                                           ([OrderNo]
                                            ,extend12
                                           ,[CreatedBy]
                                           ,[CreatedByName]
           
                                           ,[NewQty]
                                           ,[NewOrderNo],ERPStartDate,ERPEndDate,SourceOrderNo,Extend15,Qty,Remark1,[CreatedOn],ModifyedOn,Status,Remark2,DefaultLineName)
                                SELECT '{splitOrderObj.OrderNo}','{splitOrderObj.Extend12}','{splitOrderObj.Account}','{splitOrderObj.Name}','{splitOrderObj.NewQty}','','{splitOrderObj.ERPStartDate}','{splitOrderObj.ERPEndDate}','{splitOrderObj.SalesOrderNo}','{splitOrderObj.SalesLineNum}',{splitOrderObj.Qty},'{msg}','{splitOrderObj.CreatedOn}','{splitOrderObj.ModifyedOn}',{splitOrderObj.Status},'{splitOrderObj.KeyValue}','{splitOrderObj.DefaultLineName}'
                                    ");

                                                        AddSplitKey(splitOrderObj, true);

                                                        string msg123 = "";

                                                        listUpdateOrder.Add(splitOrderObj.OrderNo);
                                                        PushMoProcess(splitOrderObj.NewOrderNo, splitOrderObj.MachineCode);

                                                    }
                                                    else // if (flag == "E" || flag1 == "E")
                                                    {
                                                        result = false;


                                                        if (flag == "E")
                                                        {

                                                            if (!string.IsNullOrEmpty(msg))
                                                            {
                                                                msg = "拆分数量失败，服务器返回" + msg;
                                                                webApiResult.Msg = msg;
                                                                lstOut[splitOrderObj.RowNo] = splitOrderObj.OrderNo + "修改失败，原因：" + msg;
                                                            }

                                                        }
                                                        if (flag1 == "E")
                                                        {

                                                            if (!string.IsNullOrEmpty(msg1))
                                                            {
                                                                msg = "拆分数量失败，服务器返回" + msg1;
                                                                webApiResult.Msg = msg;
                                                                lstOut[splitOrderObj.RowNo] = splitOrderObj.OrderNo + "修改失败，原因：" + msg1;
                                                            }

                                                        }

                                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, s + "拆分数量，错误：" + msg, null, null);
                                                        splitOrderObj.Msg = msg;
                                                        AddSplitKey(splitOrderObj, false);
                                                        SaveSplitOrder(splitOrderObj);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    msg += "拆分数量失败" + ex.Message;
                                                    webApiResult.Msg = msg;
                                                    AddSplitKey(splitOrderObj, false);
                                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, s + "拆分数量，错误：" + ex.Message, null, null);
                                                    splitOrderObj.Msg = msg;
                                                    SaveSplitOrder(splitOrderObj);

                                                    result = false;
                                                    break;
                                                }
                                                index++;

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            AddSplitKey(splitOrderObj, false);
                                            result = false;
                                            webApiResult.Msg = msg;
                                            msg += "拆分数量失败" + ex.Message;
                                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, s + "拆分数量，错误：" + ex.Message, null, null);
                                        }


                                    }

                                }
                                catch (Exception ex)
                                {
                                    splitOrderObj.Msg = "拆分数量失败，APS错误信息：" + ex.Message;

                                    SaveSplitOrder(splitOrderObj);

                                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, s + "推送时错误：" + ex.Message, null, null);

                                }
                                webApiResult.Result = result;
                                if (result == false)
                                {


                                    break;
                                }


                            }
                            else
                            {//新的单据

                                if (result || splitOrderObj.IsOK)
                                {//判断修改数量是否成功
                                    try
                                    {
                                        IRfcFunction funCreate = null;

                                        if (splitOrderObj.IsOK)
                                        {//注塑之类的创单
                                            funCreate = repository.CreateFunction("ZAPS_BAPI_PRODORD_CREATE");//创建生产订单
                                            funCreate.SetValue("I_MATNR", splitOrderObj.Code);
                                            funCreate.SetValue("I_WERKS", splitOrderObj.Extend12);
                                            funCreate.SetValue("I_AUART", splitOrderObj.WorkOrderTypeID);
                                            funCreate.SetValue("I_GSTRP", splitOrderObj.ERPStartDate);
                                            funCreate.SetValue("I_GLTRP", splitOrderObj.ERPEndDate);
                                            funCreate.SetValue("I_GAMNG", splitOrderObj.NewQty);
                                            funCreate.SetValue("I_VBELN", splitOrderObj.SalesOrderNo);
                                            funCreate.SetValue("I_POSNR", splitOrderObj.SalesLineNum);
                                            funCreate.SetValue("I_ABLAD", splitOrderObj.DefaultLineName);
                                            funCreate.SetValue("I_AUFNR", splitOrderObj.OrderNo);
                                            funCreate.SetValue("I_PLNAL", splitOrderObj.MachineCode);
                                        }
                                        else
                                        {//成品订单，直接复制原单据
                                            funCreate = repository.CreateFunction("ZAPS_BAPI_PRODORD_CREATE_RE");//创建生产订单


                                            funCreate.SetValue("I_GSTRP", splitOrderObj.ERPStartDate);
                                            funCreate.SetValue("I_GLTRP", splitOrderObj.ERPEndDate);
                                            funCreate.SetValue("I_GAMNG", splitOrderObj.NewQty);
                                            funCreate.SetValue("I_ABLAD", splitOrderObj.DefaultLineName);
                                            funCreate.SetValue("I_AUFNR", splitOrderObj.OrderNo);
                                        }

                                        funCreate.Invoke(destination);

                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + $"创建生产订单,执行完成", null, null);

                                        bool result2 = funCreate.GetString("O_FLAG") == "S";
                                        string r2 = funCreate.GetString("O_MSG");
                                        splitOrderObj.NewOrderNo = funCreate.GetString("O_AUFNR");
                                        if (result2 == true)
                                        {
                                            lstOut[splitOrderObj.RowNo] = splitOrderObj.OrderNo + "创建订单成功，新单号：" + splitOrderObj.NewOrderNo;
                                            splitOrderObj.Status = 3;
                                            splitOrderObj.StatusName = "推送成功";
                                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + $"创建生产订单,第{splitOrderObj.RowIndex}行,旧订单{splitOrderObj.OrderNo},新订单{splitOrderObj.NewOrderNo},执行完成", null, null);
                                            splitOrderObj.Msg = splitOrderObj.OrderNo + $"创建生产订单完成,旧订单{splitOrderObj.OrderNo},新订单{splitOrderObj.NewOrderNo},执行完成";

                                            if (!string.IsNullOrEmpty(splitOrderObj.NewOrderNo))
                                            {
                                                // systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + $"创建生产订单,新订单{splitOrderObj.NewOrderNo},推送修改状态", null, null);
                                                if (!string.IsNullOrEmpty(splitOrderObj.Extend18))
                                                {
                                                    InterfaceSAP.PushOrderRemark(splitOrderObj.NewOrderNo, SplitString(splitOrderObj.Extend18, 132));
                                                }


                                                try
                                                {
                                                    string msg123 = "";



                                                    listUpdateOrder.Add(splitOrderObj.NewOrderNo);

                                                }
                                                catch (Exception ex)
                                                {
                                                    // systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, splitOrderObj.OrderNo + $"创建生产订单,第{splitOrderObj.RowIndex}行,推送状态失败" + ex.Message, null, null);

                                                }
                                            }
                                            else
                                            {
                                                lstOut[splitOrderObj.RowNo] = splitOrderObj.OrderNo + "创建失败，没有返回新订单号。服务器返回结果：" + r2;
                                                systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, splitOrderObj.OrderNo + $"创建生产订单,第{splitOrderObj.RowIndex}行,旧订单{splitOrderObj.OrderNo},创建失败，原因：{r2}", null, null);
                                                splitOrderObj.Msg = $"创建生产订单,第{splitOrderObj.RowIndex}行,旧订单{splitOrderObj.OrderNo},创建失败，原因：{r2}";
                                            }



                                        }
                                        else
                                        {
                                            lstOut[splitOrderObj.RowNo] = splitOrderObj.OrderNo + "创建失败，原因：" + r2;
                                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, splitOrderObj.OrderNo + $"创建生产订单,第{splitOrderObj.RowIndex}行,旧订单{splitOrderObj.OrderNo},创建失败，原因：{r2}", null, null);
                                            splitOrderObj.Msg = $"创建生产订单,第{splitOrderObj.RowIndex}行,旧订单{splitOrderObj.OrderNo},创建失败，原因：{r2}";
                                        }
                                        SaveSplitOrder(splitOrderObj);


                                        //拆分成功后，修改用户状态


                                    }
                                    catch (Exception ex)
                                    {
                                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, splitOrderObj.OrderNo + $"创建生产订单,第{splitOrderObj.RowIndex}行,执行失败" + ex.Message, null, null);
                                        splitOrderObj.Msg = $"创建生产订单,第{splitOrderObj.RowIndex}行,执行失败" + ex.Message;
                                        SaveSplitOrder(splitOrderObj);

                                        webApiResult.Msg = ex.Message;
                                        webApiResult.Result = false;
                                    }
                                }
                                else
                                {
                                    //拆分不成功，需要移除队列

                                    splitOrderObj.Msg = "拆分数量失败，本次创建不执行";
                                    SaveSplitOrder(splitOrderObj);
                                }




                            }

                        }
                        catch (Exception ex)
                        {
                            r = false;
                            webApiResult.Result = false;
                            webApiResult.Msg = "推送失败，具体原因如下：" + ex.Message;
                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, ex.StackTrace, null, null);

                        }





                    }
                    // lock (listUpdateOrder)
                    {
                        if (listUpdateOrder.Count > 0)
                        {
                            // foreach (string orderNo in listUpdateOrder)
                            {
                                string msg = "";
                                Thread.Sleep(3000);
                                Task.Run(() =>
                                {
                                    GetOrderInfoByOrderNo(listUpdateOrder);
                                });


                            }

                        }

                    }
                }
            }

            catch (Exception ex)
            {
                r = false;
                systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, ex.StackTrace, null, null);
            }
            finally
            {
                // 使用线程安全的 TryRemove 方法
                bool removed;
                ListUpdateingOrder.TryRemove(listOrderNos[0].OrderNo, out removed);
            }
            webApiResult.Data = listOrderNos;
            return webApiResult;
            // PushUserStatus();


        }
        static object objLock2 = new object();
        static ConcurrentBag<string> lstOrderNoWaiting = new ConcurrentBag<string>();
        static bool isRuningGetOrderByNo = false;
        /// <summary>
        /// 手工触发订单接口读取
        /// </summary>
        /// <param name="orderNo"></param>
        /// <param name="msg"></param>
        /// <returns></returns>

        public static async Task<bool> GetOrderInfoByOrderNo(List<string> lstOrderNo)
        {
            bool result = true;
            if (lstOrderNo.Count > 0)
            {
                // if (isRuningGetOrderByNo == false)
                {


                    try
                    {
                        isRuningGetOrderByNo = true;
                        DataTable dataTable = SqlHelper.ExecuteDataTable(@"
SELECT  [FID]
      ,[InterfaceName]
      ,[InterfaceDescription]
      ,APIUrl,SyncDatetime
  FROM  [dbo].[APS_InterfaceSAP]
    where [status]=1 
	and fid in(54,36,37)

");
                        if (repository == null)
                        {
                            destination = RfcDestinationManager.GetDestination("Conn");
                            repository = destination.Repository;

                        }


                        //  Task task = await Task.Run(async () =>
                        // {
                        //  if (IsRuning == false)
                        {


                            try
                            {




                                DataTable dataTable1 = SqlHelper.ExecuteDataTable(@"
SELECT * FROM(
SELECT  [FID]
      ,[InterfaceName]
      ,[InterfaceDescription]
   ,ERPSyncCycle
 ,SyncDatetime
,APIUrl
,  ROW_NUMBER() over(partition by  [InterfaceName] order by [SyncDatetime]  ) R
,HttpMethod
,ThreadCount,ThreadGroup
   FROM  [dbo].[APS_InterfaceSAP](nolock)
  where   [status]=1 
	and fid in(54,36,37)

) A WHERE R=1
ORDER BY FID
");
                                //输入参数
                                DataTable dataTable2 = SqlHelper.ExecuteDataTable(@"
SELECT [EID]
      ,[FID]
      ,[FName]
      ,[FNameCaption]
 ,DefaultValue,FormatValue,DataSql
,[DataType]
      ,[Url]
      ,[OutputParameterName],ContentType,HttpMethod,IsCycle
  FROM  [dbo].[APS_InterfaceSAPInput](nolock)
  where status=1 --AND DefaultValue<>''
and fid in(54,36,37)
order by DataSql
");
                                //输出参数主表
                                DataTable dataTable3 = SqlHelper.ExecuteDataTable(@"
SELECT   [EID]
      ,[FID]
      , [APSTableNameTemp]+'No' as APSTableNameTemp
      ,[APSTableName]
      ,[OutputName]
 ,	InterfaceDescription
  FROM  [dbo].[APS_InterfaceSAPOutput](nolock)
where status=1
");
                                //输出参数字段表
                                DataTable dataTable4 = SqlHelper.ExecuteDataTable(@"
SELECT   *
  FROM  [dbo].[APS_InterfaceSAPOutputField](nolock)
  where status=1 and [FNameAPS]<>''
order by EID
");
                                //输入参数明细
                                dataTable5 = SqlHelper.ExecuteDataTable(@"
SELECT  [ParameteID]
      ,[EID]
      ,[FName]
      ,[FNameCaption]
      ,[DefaultValue]
      ,[FormatValue]
      ,[DataSql]
  ,[DataType]
  FROM  [dbo].[APS_InterfaceSAPInputParameter](nolock)
  where status=1 
");




                                //判断是否已经运行
                                List<String> list = new List<String>();

                                int index = 0;
                                string orderNoS = "'APS001'";
                                foreach (string o in lstOrderNo)
                                {
                                    if (!string.IsNullOrEmpty(o))
                                    {
                                        orderNoS += ",'" + o + "'";

                                    }

                                }
                                string delSql = $@"DELETE FROM   APS_OrderImportNo WHERE ORDERNO IN({orderNoS})
DELETE FROM   APS_OrderBOMImportNo  WHERE ORDERNO IN({orderNoS})
DELETE FROM   APS_OrderProcessImportNo  WHERE remark1 IN({orderNoS})
                    ";
                                SqlHelper.ExecuteNonQuery(delSql);
                                foreach (DataRow row in dataTable1.Rows)
                                {
                                    try
                                    {
                                        string InterfaceName = row["InterfaceName"].ToString().Trim();
                                        //  int ERPSyncCycle = 0;
                                        //  int.TryParse(row["ERPSyncCycle"].ToString(), out ERPSyncCycle);
                                        string FID = row["FID"].ToString();
                                        //DateTime? SyncDatetime = null;
                                        //if (!string.IsNullOrEmpty(row["SyncDatetime"].ToString()))
                                        //{
                                        //    SyncDatetime = DateTime.Parse(row["SyncDatetime"].ToString());
                                        //}
                                        //if (ERPSyncCycle > 0 && SyncDatetime.HasValue)
                                        //{
                                        //    if ((DateTime.Now - SyncDatetime.Value).TotalMinutes < ERPSyncCycle)
                                        //    {
                                        //        count--;
                                        //        continue;
                                        //    }


                                        //}

                                        string InterfaceDescription = row["InterfaceDescription"].ToString();
                                        string ThreadGroup = "P_ImportDataDBByNo" + FID;


                                        // 使用线程安全的 GetOrAdd 方法获取或创建 List
                                        List<RuningInterFace> threadList = lstRuningInterFace.GetOrAdd(ThreadGroup, key => new List<RuningInterFace>());
                                        // 注意：List<RuningInterFace> 本身不是线程安全的，需要加锁
                                        lock (threadList)
                                        {
                                            threadList.Add(new RuningInterFace() { InterfaceDescription = InterfaceDescription, ThreadCount = 1, FID = FID });
                                        }

                                        DateTime dateTime = DateTime.Now;

                                        foreach (DataRow dataRow in dataTable2.Rows)
                                        {
                                            if (dataRow["FName"].ToString() == "I_AUFNR")
                                            {
                                                string sql = "";
                                                bool isFirst = true;
                                                foreach (string o in lstOrderNo)
                                                {
                                                    if (!string.IsNullOrEmpty(o))
                                                    {
                                                        orderNoS += ",'" + o + "'";
                                                        if (isFirst)
                                                        {
                                                            sql += " SELECT '" + o + "'";
                                                            isFirst = false;
                                                        }
                                                        else
                                                        {
                                                            sql += "UNION ALL SELECT '" + o + "'";

                                                        }
                                                    }

                                                }
                                                dataRow["DataSql"] = sql;
                                                //  dataRow["DefaultValue"] = orderNo;
                                            }
                                        }

                                        dataTable2.AcceptChanges();
                                        {
                                            string g1 = ThreadGroup;
                                            await GetSap(dataTable2, dataTable3, dataTable4, row, InterfaceName, InterfaceDescription, dateTime, FID, g1, true, false);


                                            result = true;

                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);
                                    }
                                    index++;
                                    // systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "接口循环过程，" + index + "/" + count + "," + DateTime.Now, null, null);

                                }

                                //发现下面没有执行，延迟处理3秒
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "执行立即更新的存储过程开始，" + DateTime.Now, null, null);

                                SqlHelper.ExecuteNonQuery("exec P_ImportDataDBByNo");
                                SqlHelper.ExecuteNonQuery(delSql);


                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "执行立即更新的存储过程结束，" + DateTime.Now, null, null);
                                //  systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "接口执行完成，" + DateTime.Now, null, null);
                            }
                            catch (Exception ex)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据错误, ex.Message, null, null);

                            }

                        }

                        // });
                    }
                    catch (Exception ex)
                    {
                        result = false;
                    }
                    finally
                    {
                        isRuningGetOrderByNo = false;
                    }

                }

            }


            return result;

        }

        ///// <summary>
        ///// 推送状态并且获取订单
        ///// </summary>
        //private static void PushUserStatus()
        //{
        //    bool result = true;
        //    while (result)
        //    {


        //        lock (objLock1)
        //        {

        //            RedisValue[] poppedItems = RedisHelper.db.ListRange(pushUserStatus, 0, 0);

        //            if (poppedItems.Length == 1)
        //            {
        //                RedisValue poppedItem = poppedItems[0];
        //                string s = poppedItem.ToString();

        //                if (!string.IsNullOrEmpty(s))
        //                {
        //                    SplitOrderObj splitOrderObj = JsonConvert.DeserializeObject<SplitOrderObj>(s);
        //                    string msg = "";
        //                    try
        //                    {



        //                        IRfcFunction fun046 = repository.CreateFunction("ZMES_MAINDATA_046");//同步用户状态
        //                        fun046.SetValue("I_AUFNR", splitOrderObj.NewOrderNo);
        //                        fun046.SetValue("I_STAT", splitOrderObj.Extend19);
        //                        fun046.Invoke(destination);
        //                        if (fun046.GetString("O_FLAG").ToUpper() == "S")
        //                        {

        //                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + $"创建生产订单,新订单{splitOrderObj.NewOrderNo},推送成功状态", null, null);

        //                        }
        //                        else
        //                        {
        //                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送, s + $"创建生产订单,新订单{splitOrderObj.NewOrderNo},推送状态失败" + fun046.GetString("O_TEXT"), null, null);
        //                        }
        //                        //出栈
        //                        RedisHelper.db.ListLeftPop(pushUserStatus);


        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送失败, splitOrderObj.OrderNo + $"创建生产订单,第{splitOrderObj.RowIndex}行,推送状态失败" + ex.Message, null, null);

        //                    }
        //                    finally
        //                    {
        //                        List<string> list = new List<string>();
        //                        list.Add(splitOrderObj.NewOrderNo);
        //                        Task.Run(() =>
        //                        {
        //                            GetOrderInfoByOrderNo(list);
        //                        });

        //                    }
        //                }
        //            }
        //            else
        //            {
        //                result = false;
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// 推送生产订单的备注
        /// </summary>
        /// <param name="orderNo"></param>
        /// <param name="orderRemark"></param>
        /// <returns></returns>
        public static WebApiResult PushOrderRemark(string orderNo, string[] orderRemark)
        {
            Start();
            WebApiResult webApiResult = new WebApiResult();
            if (string.IsNullOrEmpty(orderNo)) { webApiResult.Msg = "生产订单不能为空"; }
            else
            {

                try
                {
                    IRfcFunction fun045 = repository.CreateFunction("ZMES_MAINDATA_045");//同步用户状态
                    fun045.SetValue("I_AUFNR", orderNo);


                    var itb = fun045.GetTable("I_LINES");

                    foreach (string s in orderRemark)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {


                            itb.Insert();


                            //物料主数据
                            itb.CurrentRow.SetValue("TDFORMAT", "*");//标记列默认值”*”
                            itb.CurrentRow.SetValue("TDLINE", s);//标记列默认值”*”
                        }
                    }
                    if (orderRemark.Length == 0)
                    {
                        itb.Insert();


                        //物料主数据
                        itb.CurrentRow.SetValue("TDFORMAT", "*");//标记列默认值”*”
                        itb.CurrentRow.SetValue("TDLINE", "");//标记列默认值”*”
                    }

                    fun045.SetValue("I_LINES", itb);
                    fun045.Invoke(destination);
                    webApiResult.Msg = fun045.GetString("O_TEXT");
                    webApiResult.Result = fun045.GetString("O_FLAG").ToUpper() == "S";
                    if (fun045.GetString("O_FLAG").ToUpper() == "S")
                    {

                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送, $"订单{orderNo},推送成功备注", null, null);

                    }
                    else
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口推送, $"订单{orderNo},推送备注失败" + fun045.GetString("O_TEXT"), null, null);
                    }
                }
                catch (Exception ex)
                {
                    webApiResult.Msg = ex.Message;
                }
            }
            return webApiResult;
        }
        /// <summary>
        /// 记录当前的状态是否成功
        /// </summary>
        /// <param name="splitOrderObj"></param>
        /// <param name="result"></param>
        private static void AddSplitKey(SplitOrderObj splitOrderObj, bool result)
        {
            RedisHelper.db.StringGetSet(splitOrderObj.KeyValue, result.ToString().ToLower());
        }

        /// <summary>
        /// 保存拆分的信息
        /// </summary>
        /// <param name="splitOrderObj"></param>
        private static void SaveSplitOrder(SplitOrderObj splitOrderObj)
        {
            splitOrderObj.ModifyedOn = DateTime.Now;
            SqlHelper.ExecuteNonQuery($@"
  INSERT INTO [dbo].[APS_OrderSplitLog]
                                           ([OrderNo]
                                            ,extend12
                                           ,[CreatedBy]
                                           ,[CreatedByName]
           
                                           ,[NewQty]
                                           ,[NewOrderNo],ERPStartDate,ERPEndDate,SourceOrderNo,Extend15,Qty,Remark1,[CreatedOn],ModifyedOn,Status,Remark2,DefaultLineName)
                                SELECT '{splitOrderObj.OrderNo}','{splitOrderObj.Extend12}','{splitOrderObj.Account}','{splitOrderObj.Name}','{splitOrderObj.NewQty}','{splitOrderObj.NewOrderNo}','{splitOrderObj.ERPStartDate}','{splitOrderObj.ERPEndDate}','{splitOrderObj.SalesOrderNo}','{splitOrderObj.SalesLineNum}',{splitOrderObj.Qty},'{splitOrderObj.Msg}','{splitOrderObj.CreatedOn}','{splitOrderObj.ModifyedOn}'
                                  ,{splitOrderObj.Status},'{splitOrderObj.KeyValue}' ,'{splitOrderObj.DefaultLineName}'
update a set   a.OrderNoParent=b.OrderNo FROM APS_Order a
inner join APS_OrderSplitLog b on a.OrderNo=b.NewOrderNo AND B.Status=3
 
WHERE  B.NewOrderNo='{splitOrderObj.NewOrderNo}'


  
		 
  EXEC [dbo].[P_UpdateGroupName] 'Extend12','[dbo].[APS_OrderSplitLog]',1
");
        }
        /// <summary>
        /// 分析计划行的PO信息，已经不再使用
        /// </summary>
        public static void GetPO()
        {
            return;
            DataSet dsPO = SqlHelper.ExecuteDataset(@"
SELECT    DISTINCT a.SalesOrderDetailID,b.Extend8
     ,B.Extend13,c.Remark1 
  FROM  [dbo].[APS_SalesOrderDetailPlanLine] A
  INNER JOIN [dbo].[APS_SalesOrderDetail] B ON A.[SalesOrderDetailID]=B.[SalesOrderDetailID]
  inner join  [dbo].[APS_SalesOrder] B2 ON B.[SalesOrderID]=B2.[SalesOrderID]
  left join [Dev_Customer] C ON B2.CustomerID=C.CustomerID
  WHERE B.Extend13 LIKE '%PO#%' 



SELECT   [ID]
    
  
      ,[PO]
     ,B.Extend13
,a.SalesOrderDetailID
  FROM  [dbo].[APS_SalesOrderDetailPlanLine] A
  INNER JOIN [dbo].[APS_SalesOrderDetail] B ON A.[SalesOrderDetailID]=B.[SalesOrderDetailID]
  WHERE B.Extend13 LIKE '%PO#%'



");

            Regex regex = new Regex("^PO#[\\x20\\S]*");
            StringBuilder stringBuilder = new StringBuilder();
            foreach (DataRow dr in dsPO.Tables[0].Rows)
            {
                string SalesOrderDetailID = dr["SalesOrderDetailID"].ToString();
                string Extend13 = dr["Extend13"].ToString();//销售订单备注

                string Extend8 = dr["Extend8"].ToString();//客户PO，部分客户是有这个就是整个PO
                string Remark1 = dr["Remark1"].ToString();//客户类型
                                                          //
                var m1 = regex.Matches(Extend13);
                DataRow[] drs = dsPO.Tables[1].Select("SalesOrderDetailID='" + SalesOrderDetailID + "'");
                int i = 0;
                string po = "";
                if (Remark1 == "有单就生产")
                {

                    foreach (DataRow dr1 in drs)
                    {
                        if (m1.Count > i)
                        {
                            po = m1[i].Value;
                        }
                        else
                        {
                            if (po != "PO#待提供")
                            {
                                po = "";
                            }

                        }
                        stringBuilder.Append("UPDATE APS_SalesOrderDetailPlanLine SET PO='有单就生产' where ID='" + dr1["ID"].ToString() + "';");
                    }
                }
                else if (Remark1 == "有客户PO就生产" && !string.IsNullOrEmpty(Extend8))
                {
                    foreach (DataRow dr1 in drs)
                    {
                        if (m1.Count > i)
                        {
                            po = m1[i].Value;
                        }
                        else
                        {
                            if (po != "PO#待提供")
                            {
                                po = "";
                            }

                        }
                        stringBuilder.Append("UPDATE APS_SalesOrderDetailPlanLine SET PO='" + Extend8 + "' where ID='" + dr1["ID"].ToString() + "';");
                    }
                }
                else
                {
                    foreach (DataRow dr1 in drs)
                    {
                        if (m1.Count > i)
                        {
                            po = m1[i].Value;
                        }
                        else
                        {
                            if (po != "PO#待提供")
                            {
                                po = "";
                            }

                        }
                        stringBuilder.Append("UPDATE APS_SalesOrderDetailPlanLine SET PO='" + po + "' where ID='" + dr1["ID"].ToString() + "';");
                    }
                }
            }

            SqlHelper.ExecuteNonQuery(stringBuilder.ToString());
        }
        /// <summary>
        /// 推送工单工序
        /// </summary>
        /// <param name="orderNo">订单</param>
        /// <param name="machineCode">计数器</param>
        /// <returns></returns>

        public static async Task<WebApiResult> PushMoProcess(string orderNo, string machineCode)
        {


            Start();
            WebApiResult webApiResult = new WebApiResult();
            if (string.IsNullOrEmpty(orderNo)) { webApiResult.Msg = "生产订单不能为空"; }
            else if (string.IsNullOrEmpty(machineCode)) { webApiResult.Msg = "计数器不能为空"; }
            else if (true)//先禁用，待确认***********************
            {

                try
                {
                    IRfcFunction fun044 = repository.CreateFunction("ZMES_MAINDATA_044");//同步用户状态




                    DataTable dtProcess = SqlHelper.ExecuteDataTable(@"SELECT   a.Code,b.Extend12
FROM[dbo].APS_Material A(NOLOCK)
INNER JOIN APS_Order B(NOLOCK)
 ON A.MaterialID = B.MaterialID
  and b.OrderNo = '" + orderNo + "'  ");

                    if (dtProcess.Rows.Count > 0)
                    {//大于1才修改，因为目前只能选计数器，还没有开放修改其他字段的功能

                        DataRow dr = dtProcess.Rows[0];
                        //foreach(DataRow dr in dtProcess.Rows)
                        //{
                        //    itb.Insert();
                        //    itb.CurrentRow.SetValue("AUFNR", dr["OrderNo"]);
                        //    itb.CurrentRow.SetValue("VORNR", dr["Remark2"]);
                        //    itb.CurrentRow.SetValue("ARBPL", dr["WorkCenter"]);
                        //    itb.CurrentRow.SetValue("STEUS", dr["STEUS"]);
                        //    itb.CurrentRow.SetValue("LTXA1", dr["ERPProcessName"]);
                        //    itb.CurrentRow.SetValue("BMSCH", dr["BaseQty"]);
                        //    itb.CurrentRow.SetValue("VGW01", dr["ReadinessTime"]);
                        //    itb.CurrentRow.SetValue("VGW02", dr["MachineSeconds"]);
                        //    itb.CurrentRow.SetValue("VGW03", dr["Seconds"]);
                        //    itb.CurrentRow.SetValue("SPLIM", dr["SPLIM"]);
                        //    itb.CurrentRow.SetValue("WERKS", dr["Factory"]);
                        //    itb.CurrentRow.SetValue("SPMUS", "X");
                        //    itb.CurrentRow.SetValue("VGE02", dr["VGE02"]);
                        //    itb.CurrentRow.SetValue("VGE03", dr["VGE03"]);
                        //}
                        fun044.SetValue("L_AUFNR", orderNo);
                        fun044.SetValue("L_MATNR", dr["Code"]);
                        fun044.SetValue("L_PLNAL", machineCode);
                        fun044.SetValue("L_WERKS", dr["Extend12"]);
                        fun044.Invoke(destination);
                        var otb = fun044.GetTable("LS_RETURN");

                        webApiResult.Msg = fun044.GetString("O_TEXT");
                        webApiResult.Result = fun044.GetString("O_FLAG").ToUpper() == "S";
                        if (webApiResult.Result)
                        {

                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送, $"订单{orderNo},修改工艺成功！", null, null);

                        }
                        else
                        {
                            foreach (var t in otb)
                            {
                                webApiResult.Msg = "；" + t.GetString("MESSAGE");
                            }

                            systemLog.SaveLog(SystemLog.SystemLogType.接口推送, $"订单{orderNo},修改失败，原因：" + webApiResult.Msg, null, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    webApiResult.Msg = ex.Message;
                }
            }
            return webApiResult;
        }
        public static string[] SplitString(string inputStr, int splitLength)
        {
            //计算分割的段数
            int numberOfsplits = (int)Math.Ceiling((double)inputStr.Length / splitLength);

            //分割字符串
            string[] outputStr = new string[numberOfsplits];
            for (int i = 0; i < numberOfsplits; i++)
            {
                int startIndex = i * splitLength;
                int length = Math.Min(splitLength, inputStr.Length - startIndex);
                outputStr[i] = inputStr.Substring(startIndex, length);
            }

            return outputStr;
        }

        private static Dictionary<string, string?> ParseQueryStringToDictionary(string query)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(query))
                return result;

            var trimmed = query.Trim().TrimStart('?');
            foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                var key = Uri.UnescapeDataString(pair[0]);
                var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
                result[key] = value;
            }

            return result;
        }
    }

}