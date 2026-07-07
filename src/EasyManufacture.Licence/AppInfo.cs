using System;
using System.Collections.Generic;

using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;







namespace EasyManufacture.Licence;

public class AppInfo
{
    public static List<AccountLoginInfo> AccountLoginInfos = new();
    public static List<LockedIpEntry> LockedIps = new();
        /// <summary>
        /// 当前的IP是否被锁定
        /// </summary>
        /// <returns></returns>
        public static  bool IsLock()
        {
            var obj = LockedIps.Where(m => m.IPAddress == LicenceRuntime.ClientIpAddress && m.IsLock).FirstOrDefault();
            if (obj != null)
            {
                if ((DateTime.Now - obj.LastTime).TotalSeconds >= 60*10)
                {
                    obj.IsLock = false;
                }
            }
            bool result = false;
            if (obj != null)
            {
                result = true;
            }
            else if (obj?.IsLock == true)
            {
                result = true;
            }
            return result;
        }
        /// <summary>
        /// 启用攻击
        /// </summary>
        public static  bool IsSafe
        {
            get
            {
                bool isSafe = false;
                if (LicenceConfig.Get("IsSafe") != null)
                {
                    isSafe = LicenceConfig.Get("IsSafe").ToString()=="1";
                }
                return isSafe;
            }
        }
        /// <summary>
        /// 2秒内访问20次则锁定账号
        /// </summary>
        /// <returns></returns>
        public static void CheckLogCount()
        {
            var obj = LockedIps.Where(m => m.IPAddress == LicenceRuntime.ClientIpAddress).FirstOrDefault();
            if (obj == null)
            {
                obj = new LockedIpEntry() { IPAddress = LicenceRuntime.ClientIpAddress, IsLock = false };
                LockedIps.Add(obj);
            }
            if ((DateTime.Now - obj.LastTime).TotalSeconds <=1.5)
            {//如果在1秒内访问超过20，则锁定
                obj.Visits +=1;
                if (obj.Visits > 30)
                {
                    obj.IsLock = true;
                }
            }
            else
            {
                obj.Visits = 0;
                if ((DateTime.Now - obj.LastTime).TotalSeconds >60*10)
                {
                    obj.IsLock = false;
                }
                obj.LastTime = DateTime.Now;
            }
           
        }

        /// <summary>
        /// 项目代码
        /// </summary>
        public static string AppCode
        {
            get
            {
                string appCode = "";
                if (LicenceConfig.Get("AppCode") != null)
                {
                    appCode = LicenceConfig.Get("AppCode").ToString();
                }
                return appCode;
            }
        }
        private static  string externalNetwork = "";
        /// <summary>
        /// 外网地址，如果存在则判断账号是否可以访问
        /// </summary>
        public static string ExternalNetwork
        {
            get
            {
             
                if (string.IsNullOrEmpty(externalNetwork) &&LicenceConfig.Get("ExternalNetwork") != null)
                {
                    externalNetwork = LicenceConfig.Get("ExternalNetwork").ToString();
                }
                return externalNetwork;
            }
        }
        /// <summary>
        /// 周计划排产是周几
        /// </summary>
        public static int ConfigStartWeek
        {
            get
            {
                int weekDay = 0;
                if (LicenceConfig.Get("ConfigStartWeek") != null)
                {
                    weekDay = int.Parse(LicenceConfig.Get("ConfigStartWeek").ToString());
                }
                return weekDay;
            }
        }
        public static string Report1
        {
            get
            {
                string Report1 = "0";
                if (LicenceConfig.Get("Report1") != null)
                {
                    Report1 = LicenceConfig.Get("Report1").ToString();
                }
                return Report1;
            }
        }
        /// <summary>
        /// ERP同步天数
        /// </summary>
        public static int ERPSyncDay
        {
            get
            {
                int eRPSyncDay = 1;
                if (LicenceConfig.Get("ERPSyncDay") != null)
                {
                    eRPSyncDay =int.Parse(LicenceConfig.Get("ERPSyncDay").ToString());
                }
                return eRPSyncDay;
            }
        }
        /// <summary>
        /// ERP同步频率
        /// </summary>
        public static double ERPSyncCycle
        {
            get
            {
                double eRPSyncDay = 1;
                if (LicenceConfig.Get("ERPSyncCycle") != null)
                {
                    eRPSyncDay = double.Parse(LicenceConfig.Get("ERPSyncCycle").ToString());
                }
                return eRPSyncDay;
            }
        }
        
        public static string SAPConn
        {
            get
            {
                string conn = "";
                if (LicenceConfig.Get("SAPConn") != null)
                {
                    conn = LicenceConfig.Get("SAPConn").ToString();
                }
                return conn;
            }
        }
        public static string ProcessCard
        {
            get
            {
                string appCode = "";
                if (LicenceConfig.Get("ProcessCard") != null)
                {
                    appCode = LicenceConfig.Get("ProcessCard").ToString();
                }
                return appCode;
            }
        }
        public static bool IsControlID
        {
            get
            {
                bool isControlID = false;
                if (LicenceConfig.Get("IsControlID") != null)
                {
                    isControlID = LicenceConfig.Get("IsControlID").ToString() == "1";
                }
                return isControlID;
            }
        }
        /// <summary>
        /// 是否启用来源单号
        /// </summary>
        public static bool IsUserSourceOrderNo
        {
            get
            {
                bool isControlID = false;
                if (LicenceConfig.Get("IsUserSourceOrderNo") != null)
                {
                    isControlID = LicenceConfig.Get("IsUserSourceOrderNo").ToString() == "1";
                }
                return isControlID;
            }
        }
        public static string LoginUrl
        {
            get
            {
                string url = "/Login/Login";
                if (LicenceConfig.Get("LoginUrl") != null)
                {
                    url = LicenceConfig.Get("LoginUrl").ToString();
                }
                return url;
            }
        }

        private static bool? isUseSplit;
        public static bool IsUseSplit
        {
            get
            {
                if(isUseSplit.HasValue==false)
                {
                    if (LicenceConfig.Get("IsUseSplit") != null)
                    {
                        isUseSplit = LicenceConfig.Get("IsUseSplit").ToString() == "1";
                    }
                }
             
                return isUseSplit.GetValueOrDefault();
            }
        }

        private static string redis;
        public static string Redis
        {
            get
            {
                if (string.IsNullOrEmpty(redis))
                {
                    if (LicenceConfig.Get("redis") != null)
                    {
                        redis = LicenceConfig.Get("redis").ToString();
                    }
                }

                return redis;
            }
        }

        private static int? schedulingDays;
        public static  int? SchedulingDays
        {
            get
            {
                if (schedulingDays.HasValue == false)
                {
                    if (LicenceConfig.Get("SchedulingDays") != null)
                    {
                        schedulingDays = int.Parse(LicenceConfig.Get("SchedulingDays"));
                    }
                }

                return schedulingDays.GetValueOrDefault(30);
            }
        }
        private static bool? isAutoSchedulingToLine;
        public static bool IsAutoSchedulingToLine
        {
            get
            {
                if (isAutoSchedulingToLine.HasValue == false)
                {
                    if (LicenceConfig.Get("IsAutoSchedulingToLine") != null)
                    {
                        isAutoSchedulingToLine = LicenceConfig.Get("IsAutoSchedulingToLine").ToString() == "1";
                    }
                }
                return isAutoSchedulingToLine.GetValueOrDefault();
            }


        }
        /// <summary>
        /// 是否推送给ERP
        /// </summary>
        public static bool IsPushERP
        {
            get
            {
                bool isPushERP = false;
                if (LicenceConfig.Get("IsPushERP") != null)
                {
                    isPushERP = LicenceConfig.Get("IsPushERP").ToString() == "1";
                }
                return isPushERP;
            }
        }
        /// <summary>
        /// 是否推送给ERP
        /// </summary>
        public static bool IsHasOrderBOM
        {
            get
            {
                bool isHasOrderBOM = false;
                if (LicenceConfig.Get("IsHasOrderBOM") != null)
                {
                    isHasOrderBOM = LicenceConfig.Get("IsHasOrderBOM").ToString() == "1";
                }
                return isHasOrderBOM;
            }
        }
        


        private static bool? isMultiOrganization = false;
        public static bool IsMultiOrganization
        {
            get
            {
              
                if (!isMultiOrganization.HasValue)
                {
                    isMultiOrganization = LicenceConfig.GetBool("MultiOrganization");
                }
                return isMultiOrganization.Value;
            }
        }
       static string pushType = "";
        /// <summary>
        /// 推送方式
        /// </summary>
        public static string PushType
        {
            get
            {
               
                if (pushType==""&&LicenceConfig.Get("PushType") != null)
                {
                    pushType = LicenceConfig.Get("PushType").ToString();
                }
                return pushType;
            }
        }
        static bool? isVue;
        /// <summary>
        /// 推送方式
        /// </summary>
        public static bool IsVue
        {
            get
            {
                
                if (isVue.HasValue==false&&LicenceConfig.Get("IsVue") != null)
                {
                    isVue = LicenceConfig.Get("IsVue").ToString()=="1";
                }
                return isVue.GetValueOrDefault();
            }
        }
        static bool? isSaveLog = false;
        /// <summary>
        /// 推送方式
        /// </summary>
        public static bool IsSaveLog
        {
            get
            {
               
                if (isSaveLog.HasValue==false&&  LicenceConfig.Get("IsSaveLog") != null)
                {
                    isSaveLog = LicenceConfig.Get("IsSaveLog").ToString() == "1";
                }
                return isSaveLog.GetValueOrDefault();
            }
        }

        


     
        /// <summary>
        /// UI选择
        /// </summary>
        public static string UI
        {
            get
            {
                string ui = "gray";
                if(LicenceConfig.Get("UI")!=null)
                {
                    ui= LicenceConfig.Get("UI").ToString();
                }
                return ui;
            }
        }
       static   string _ERPUrl = "";
        public static string ERPUrl
        {
            get
            {
               if(string.IsNullOrEmpty(_ERPUrl))
                {
                    if (LicenceConfig.Get("ERPUrl") != null)
                    {
                        _ERPUrl = LicenceConfig.Get("ERPUrl").ToString();
                    }

                }
            
                return _ERPUrl;
            }
        }


        /// <summary>
        /// 是否显示清拉
        /// </summary>
        public static bool ShowCleanQty
        {
            get
            {
                bool result = false;
                if (LicenceConfig.Get("ShowCleanQty") != null)
                {
                    result = LicenceConfig.Get("ShowCleanQty").ToString() == "1";
                }
                return result;
            }
        }
        private  static string[] companys = new string[] {
            "1000004,五金二公司"

        };
        private static List<string> ListCompany
        {
            get
            {
                return companys.ToList();
            }
        }

       //public static void SetInfo(int index)
       // {
       //     string mac = StringHelper.GetMAC();
       //     System.Configuration.Configuration cfa = WebConfigurationManager.OpenWebConfiguration("~");

       //     string orgInfo = companys[index] + mac;
       //     //修改节点值
       //     cfa.AppSettings.Settings["OrgInfo"].Value = StringHelper.MD5Encrypt(orgInfo);
       //     //最后保存修改的节点
       //     cfa.Save();
       // }
        //private static  EasyManufactureEntities Entities = new EasyManufactureEntities();
        //public static  bool CheckOrgInfo(ref string msg)
        //{
        //    bool result = true;
        //    string orgInfo = AppInfo.OrgInfoConfig;
        //    if (ListCompany.Where(m => m == orgInfo).Count() == 0)
        //    {
        //        msg = "无效企业信息";
        //        result = false;
                
        //    }
        //    else
        //    {
        //        var  thisOrgInfo = ListCompany.Where(m => m == orgInfo).FirstOrDefault().Split(',');
        //        var orgName = thisOrgInfo[1];
        //     //   var orgCount = Entities.Dev_Organize.Where(m => m.OrganizeName == orgName && m.OrganizeTypeID <= 3 && m.Status == 1).Count();
        //        //if (orgCount == 0)
        //        //{
        //        //    msg = orgName+"当前企业信息不匹配!";
        //        //    result = false;
                    
        //        //}
        //        //else
        //        //{

        //        //    if (thisOrgInfo[2] != StringHelper.GetMAC())
        //        //    {
        //        //        msg = "服务器不匹配";
        //        //        result = false;
        //        //    }
        //        //}
        //    }
           
        //    return result;
        //}
        /// <summary>
        /// 组织信息
        /// </summary>
        //public static string[] OrgInfo
        //{
        //    get
        //    {
        //        string result = OrgInfoConfig;
        //        return result.Split(',');
        //    }
        //}
        ///// <summary>
        ///// 组织信息
        ///// </summary>
        //public static string OrgInfoConfig
        //{
        //    get
        //    {
        //        string result = "";
        //        if (LicenceConfig.Get("OrgInfo") != null)
        //        {
        //            result = LicenceConfig.Get("OrgInfo").ToString();
        //            string mac = StringHelper.GetMAC();
        //            var obj = ListCompany.Where(m => StringHelper.MD5Encrypt(m+mac) == result).FirstOrDefault();
        //            if(obj!=null)
        //            {
        //                result = obj;
        //            }
        //            else
        //            {
        //                result = "error";
        //            }
        //        }
        //        return result;
        //    }
        //}
        /// <summary>
        /// 大脑推送接口
        /// </summary>
        public static string[] WebJsonInterface
        {
            get
            {
                string result = "";
                if (LicenceConfig.Get("WebJsonInterface") != null)
                {
                    result = LicenceConfig.Get("WebJsonInterface").ToString();
                }
                return result.Split(',');
            }
        }

        public static List<string> ListControlID
        {
            get;set;
        }
        static bool? _XBLoginCheck = null;
        public static bool XBLoginCheck
        {
            get
            {
                if (_XBLoginCheck.HasValue==false)
                {
                    if (LicenceConfig.Get("XBLoginCheck") != null)
                    {
                        _XBLoginCheck = LicenceConfig.Get("XBLoginCheck").ToString()=="1";
                    }

                }

                return _XBLoginCheck.GetValueOrDefault();
            }
        }

        public static U9Context u9Context;
        public static U9Context ERPContextObj
        {
            get
            {

                if (u9Context == null)
                {

                    u9Context = new U9Context();
                    if (LicenceConfig.Get("EnterpriseID") != null)
                    {
                        u9Context.EnterpriseID = LicenceConfig.Get("EnterpriseID").ToString();
                    }
                    if (LicenceConfig.Get("OrgID") != null)
                    {
                        u9Context.OrgID = LicenceConfig.Get("OrgID").ToString();
                    }
                    
                    if (LicenceConfig.Get("UserID") != null)
                    {
                        u9Context.UserID = LicenceConfig.Get("UserID").ToString();
                    }
                    if (LicenceConfig.Get("IsPushERP") != null)
                    {
                        u9Context.PushERP = LicenceConfig.Get("IsPushERP").ToString()=="1";
                    }
                    if (LicenceConfig.Get("Password") != null)
                    {
                        u9Context.Password = LicenceConfig.Get("Password").ToString();
                    }
                    
                }
                return u9Context;
            }
          
    }


        public class U9Context
        {
            public string EnterpriseID
            {
                get;set;
            }
            public string OrgID
            {
                get;set;
            }
       
            public string UserID
            {
                get;set;
            }
            public bool PushERP
            {
                get;set;
            }
               public string Password
            {
                get;set;
            }
        }

        /// <summary>
        /// 是否要控制拉线的完成期，默认启用
        /// </summary>
        public static bool ControlLineEndDate
        {
            get
            {
                bool result = true;
                if (LicenceConfig.Get("ControlLineEndDate") != null)
                {
                    result = LicenceConfig.Get("ControlLineEndDate").ToString() == "1";
                }
                return result;
            }
        }
        /// <summary>
        /// MO排产模式，0：下达后到日计划
        /// </summary>
        public static int MOSchedulingType
        {
            get
            {
                int result = 0;
                if (LicenceConfig.Get("MOSchedulingType") != null)
                {
                    result = int.Parse(LicenceConfig.Get("MOSchedulingType").ToString());
                }
                return result;
            }
        }
        /// <summary>
        /// 邮件配置
        /// </summary>
        public static string EmailConfig
        {
            get
            {
                string result = "";
                if (LicenceConfig.Get("emailConfig") != null)
                {
                    result = LicenceConfig.Get("emailConfig").ToString();
                }
                return result;
            }
        }
        /// <summary>
        /// MES地址
        /// </summary>
        public static string MESUrl
        {
            get
            {
                string result = "";
                if (LicenceConfig.Get("MESUrl") != null)
                {
                    result = LicenceConfig.Get("MESUrl").ToString();
                }
                return result;
            }
        }

        /// <summary>
        /// WMS地址
        /// </summary>
        public static string WMSUrl
        {
            get
            {
                string result = "";
                if (LicenceConfig.Get("WMSUrl") != null)
                {
                    result = LicenceConfig.Get("WMSUrl").ToString();
                }
                return result;
            }
        }
        /// <summary>
        /// SRM地址
        /// </summary>
        public static string SRMUrl
        {
            get
            {
                string result = "";
                if (LicenceConfig.Get("SRMUrl") != null)
                {
                    result = LicenceConfig.Get("SRMUrl").ToString();
                }
                return result;
            }
        }
        /// <summary>
        /// SRM地址
        /// </summary>
        public static string SRMApplicationCode
        {
            get
            {
                string result = "";
                if (LicenceConfig.Get("SRMApplicationCode") != null)
                {
                    result = LicenceConfig.Get("SRMApplicationCode").ToString();
                }
                return result;
            }
        }
        /// <summary>
        /// 是否启用SSO
        /// </summary>
        public static string   SSOUrl
        {

            get
            {
               string url = "";
                if (LicenceConfig.Get("SSOUrl") != null)
                {
                    url = LicenceConfig.Get("SSOUrl").ToString();
                }
                return url;
            }
        }
        /// <summary>
        /// OA地址
        /// </summary>
        public static string OAUrl
        {

            get
            {
                string url = "";
                if (LicenceConfig.Get("OAUrl") != null)
                {
                    url = LicenceConfig.Get("OAUrl").ToString();
                }
                return url;
            }
        }
        /// <summary>
        /// 是否启用SSO
        /// </summary>
        public static bool IsChangePwd
        {

            get
            {
                bool isChangePwd = false;
                if (LicenceConfig.Get("IsChangePwd") != null)
                {
                    isChangePwd = LicenceConfig.Get("IsChangePwd").ToString()=="1";
                }
                return isChangePwd;
            }
        }
        /// <summary>
        /// 是否推送企业微信
        /// </summary>
        public static bool WX
        {

            get
            {
                bool wx = false;
                if (LicenceConfig.Get("WX") != null)
                {
                    wx = LicenceConfig.Get("WX").ToString() == "1";
                }
                return wx;
            }
        }
        /// <summary>
        /// 是否配置多语言
        /// </summary>
        public static bool Language
        {

            get
            {
                bool Language = false;
                if (LicenceConfig.Get("Language") != null)
                {
                    Language = LicenceConfig.Get("Language").ToString() == "1";
                }
                return Language;
            }
        }
}

