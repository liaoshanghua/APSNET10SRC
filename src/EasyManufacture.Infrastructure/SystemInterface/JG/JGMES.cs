using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EasyManufacture.Infrastructure.SystemInterface.JG
{
    public class JGMES
    {
        static SystemLog systemLog = new SystemLog();
       static bool IsStart=false;
        /// <summary>
        /// 开始执行接口
        /// </summary>
        public static void Start()
        {
            if (IsStart==false)
            {
                IsStart=true;
                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单报工序开始执行", null, null);
                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"truncate table APS_OrderProcessReport;"
    );
                for (int i = AppInfo.ERPSyncDay; i >= 0; i--)
                {

                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单报工序开始执行"+i, null, null);
                    try
                    {
                        //报工
                        Encoding encoding = Encoding.UTF8;
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://gd.jgyun.cn/api/openapi/pm-production-completion/find-pl-finish-qty");
                        request.Method = "POST";
                        request.ContentType = "application/json";
                        request.Timeout = 1000 * 30;
                        request.Headers.Add("Authorization", "eyJhbGciOiJIUzI1NiJ9.eyJqdGkiOiJkYTcyY2NkOS04M2MyLTQ3YTgtOGI2Mi1lN2ZkZjM3N2YyMjQiLCJpc3MiOiJqZy1tZXMtand0Iiwic3ViIjoie1wicGhvbmVOdW1iZXJcIjpcIjE4NjIwMTIwODgwXCIsXCJsb2dpbkZyb21cIjpcImRldmljZVwiLFwidXNlcm5hbWVcIjpcIjE4NjIwMTIwODgwXCJ9IiwiaWF0IjoxNjUzODk0MzQyfQ.zjydn782NptTzfgg-CdpUtSbquxJUJjsPKPmmuuvtuQ");
                        IDictionary<string, object> para = new Dictionary<string, object>();
                        para.Add("model", "");
                        string s = "{\r\n \"params\": {\r\n  \"isSum\":\"0\",\r\n  \"pclStartDate\": \"" + string.Format("{0:yyyy-MM-dd 00:00:00}", DateTime.Now.AddDays(-i)) + "\",\r\n  \"pclEndDate\": \"" + string.Format("{0:yyyy-MM-dd 23:59:59}", DateTime.Now.AddDays(-i)) + "\"\r\n },\r\n \"pageIndex\": 1,\r\n \"pageRows\": 1000 \r\n}";
                        byte[] buffer = encoding.GetBytes(s);
                        request.ContentLength = buffer.Length;
                        request.GetRequestStream().Write(buffer, 0, buffer.Length);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        string res = "";
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            #region 工单报工信息
                            try
                            {

                                res = reader.ReadToEnd();
                                JObject jArray = JsonConvert.DeserializeObject(res) as JObject;

                                StringBuilder stringBuilder = new StringBuilder();
                                JArray jArray1 = jArray["data"]["data"] as JArray;












                                systemLog = new SystemLog();
                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单报工序开始,记录数" + jArray1.Count + "", null, null);
                                int j = 0;
                                foreach (JObject jObject in jArray1)
                                {
                                    //   foreach (var item in result)

                                    stringBuilder.Append(string.Format(@"
INSERT INTO [dbo].[APS_OrderProcessReport]
           ([ProcessID]
,[ProcessName]
,[DemandQty]
           ,[ProducedQty]
           ,[ConfirmQty]
        
     
           ,[Remark1]
           ,[Remark2]
   
           ,[StartDate]
           ,[EndDate]
 ,Extend1
     )
SELECT {0}
 ,{1}
,{2}
,{3}
,{4}
,{5}

,{6}
,{7}
,{8}
 ,{9}
 
"
            , StringHelper.ReplaceSqlValue(jObject["processCode"].ToString())
            , StringHelper.ReplaceSqlValue(jObject["processName"].ToString())
            , StringHelper.ReplaceSqlValue(jObject["completionQtys"].ToString())//demandqty
            , StringHelper.ReplaceSqlValue(jObject["completionQtys"].ToString())//producedqty
            , StringHelper.ReplaceSqlValue(jObject["completionQtys"].ToString())//报工2，ConfirmQty
            , StringHelper.ReplaceSqlValue(jObject["taskCode"].ToString())//  工单号
            , StringHelper.ReplaceSqlValue(jObject["pclId"].ToString())//ID
            , StringHelper.ReplaceSqlValue(jObject["pclDate"].ToString())
             , StringHelper.ReplaceSqlValue(jObject["pclDate"].ToString())
              , StringHelper.ReplaceSqlValue(jObject["plName"].ToString())

            ));
                                    if (j % 5000 == 0 && j > 0)
                                    {
                                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                                        stringBuilder = new StringBuilder();
                                    }
                                    j++;



                                }


                                if (stringBuilder.Length > 0)
                                {
                                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                                    stringBuilder = new StringBuilder();
                                }



                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "工单工序报工读取结束" + DateTime.Now.AddDays(-i) + "," + jArray1.Count + "，已插入：", null, null);

                            }
                            catch (Exception ex)
                            {
                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "工单工序报工错误" + ex.Message, null, null);
                            }

                            #endregion



                        }
                    }
                    catch(Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "工单工序报工错误" + ex.Message, null, null);
                    }
                }
                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @" EXEC P_ImportDataDB;");
                IsStart=false;
            }
        }

    }
}


