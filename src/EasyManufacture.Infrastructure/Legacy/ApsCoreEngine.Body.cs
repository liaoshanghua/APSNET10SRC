using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 精简 APSCore：GetConfig / SaveData 及共用字段（自 APSCore 手工提取）。
/// EnableLegacyApsCoreSource=false 时编译；全量逻辑在 LegacyCore.cs。
/// </summary>
public partial class ApsCoreEngine
{
        private DataTable? _dtWorkTimes;
        protected DataTable dtWorkTimes => _dtWorkTimes ??= SqlHelper.ExecuteDataTable("SELECT WorkingTimesID,WorkingTimesName FROM APS_WorkingTimes");
        protected List<string> listDaysDicID = new List<string>() { "3039" };
        protected List<List<ElementTableOuput>> lstAllColumnCommon = new List<List<ElementTableOuput>>();
        protected List<ElementTableOuput> lstColumnCommon = new List<ElementTableOuput>();
        /// <summary>
        /// 缓存配置，解决慢的问题
        /// </summary>
        public class CatchElemet
        {
            public List<ElementTableOuput> elementTableOuputs
            {
                get; set;
            } = new List<ElementTableOuput>();
            public List<SearchForm> searchFormsAll
            {
                get; set;
            } = new List<SearchForm>();
            public List<List<SearchForm>> searchForms { get; set; } = new List<List<SearchForm>>();
            public List<List<ElementTableOuput>> datas { get; set; } = new List<List<ElementTableOuput>>();

            public List<ElementTableOuput> appColumns { get; set; } = new List<ElementTableOuput>();
            public List<List<ElementTableOuput>> lstAllColumnCommon { get; set; } = new List<List<ElementTableOuput>>();
            public List<ElementTableOuput> lstColumnCommon = new List<ElementTableOuput>();
        }
      
        /// <summary>
        /// 缓存列配置
        /// </summary>
         public static  Dictionary<string, CatchElemet> APSCatchElemet = new Dictionary<string, CatchElemet>();
        public void GetConfigForObj(ref List<ElementTableOuput> elementTableOuputs, ref List<SearchForm> searchFormsAll,ref  List<List<SearchForm>> searchForms, ref List<List<ElementTableOuput>> datas, ref string msg, ref bool result, JObject jObject, ref List<ElementTableOuput> appColumns, List<string> SyncDatetime)
        {




            try
            {
                List<ElementTableInput>? elementTable = null;
                try
                {
                    elementTable = JsonConvert.DeserializeObject<List<ElementTableInput>>(BodyJson);
                }
                catch
                {
                    /* APSData 等场景可能传入 {"dicID":n}，由下方回退 */
                }
                if ((elementTable == null || elementTable.Count == 0) && jObject?["dicID"] != null)
                {
                    elementTable = new List<ElementTableInput>
                    {
                        new() { ID = int.Parse(jObject["dicID"]!.ToString()) }
                    };
                }
                string id = "";
                if (elementTable == null || elementTable.Count == 0)
                {
                    result = false;
                    msg = "没有接收到ID";
                }
                else
                {
                    string key = BodyJson;

                    if (setDt != null || (jObject != null && jObject.ContainsKey("SDate") == true))
                    {//有委托或者自定义日期的不缓存
                        if (APSCatchElemet.ContainsKey(key)){
                            APSCatchElemet.Remove(key);
                        }
                    }
                    if (APSCatchElemet.ContainsKey(key))
                    {
                        elementTableOuputs = APSCatchElemet[key].elementTableOuputs;
                        searchFormsAll = APSCatchElemet[key].searchFormsAll;
                        searchForms = APSCatchElemet[key].searchForms;
                        datas = APSCatchElemet[key].datas;
                        appColumns = APSCatchElemet[key].appColumns;
                        lstAllColumnCommon= APSCatchElemet[key].lstAllColumnCommon;
                        lstColumnCommon = APSCatchElemet[key].lstColumnCommon;
                        return;
                    }
                    elementTable.ForEach(m =>
                    {
                        id += m.ID + ",";
                      
                    });
                    List<V_DictionaryField> dev_DictionaryFields = Entities.SqlQueryV_DictionaryField(string.Format(@"SELECT A.*,B.ObjectName,B.TabelName,b.MenuCode,B.IsShowCheck,CAST(B.Region AS NVARCHAR(50)) AS Region

  FROM Dev_DictionaryField A(NOLOCK)
  INNER JOIN Dev_Dictionary B(NOLOCK) ON A.DictionaryID=B.DictionaryID
  where A.[DictionaryID] IN({0}) AND IsSelect=1", id.Trim(',')));

                    DataTable DtDic = SqlHelper.ExecuteDataTable(string.Format(@"SELECT * from  Dev_Dictionary A
  where A.[DictionaryID] IN({0})", id.Trim(',')));


                    DataTable dtDictionaryFieldAccount = null;
                    try
                    {

                        dtDictionaryFieldAccount = SqlHelper.ExecuteDataTable(string.Format(@"SELECT A.*
                        FROM Dev_DictionaryField A(NOLOCK)
                        where A.[DictionaryID] IN({0})", id.Trim(',')));
                    }
                    catch
                    {

                    }
                    if (SyncDatetime != null)
                    {
                        elementTable.ForEach(m =>
                        {
                            foreach (DataRow dataRow1 in DtDic.Select("DictionaryID="+m.ID))
                            {
                                if (!string.IsNullOrEmpty(dataRow1["SyncDatetime"].ToString()) && !string.IsNullOrEmpty(dataRow1["SyncRate"].ToString()))
                                {


                                    SyncDatetime.Add(string.Format(dataRow1["SyncContent"].ToString(), dataRow1["DataSource"],
                                        dataRow1["SyncRate"], dataRow1["SyncDatetime"]));
                                }

                                else
                                {
                                    SyncDatetime.Add("");
                                }
                            }
                        }); 



                    
                    }
                 
                    int left = 0;
                    SearchForm searchForm = null;

                    if (dev_DictionaryFields.Where(m => string.IsNullOrEmpty(m.ColTitle)==false).Count()>0)
                    {//分组

                   
                        List<ElementTableOuput> list = new List<ElementTableOuput>();
                        foreach (var n in dev_DictionaryFields.OrderBy(m => m.FieldIndex))
                        {
                            
                            if (n.IsVisible == true)
                            {
                                if (!string.IsNullOrEmpty(n.ColTitle))
                                {//有分组

                                    ElementTableOuput obj = list.Where(m=>m.label==n.ColTitle).FirstOrDefault();
                                    if (obj==null)
                                    {
                                  

                                          obj = new ElementTableOuput();
                                        list.Add(obj);
                                        obj.width = n.Width.GetValueOrDefault(100).ToString();
                                        obj.label = n.ColTitle;
                                        obj.prop = n.ParameterName;
                                       if (n.IsFrozen == true)
                                        {
                                            obj.fix = "left";
                                        }
                                        obj.fix = string.IsNullOrEmpty(n.fix) ? obj.fix : n.fix;
                                        obj.sortable = string.IsNullOrEmpty(n.sortable) ? null : n.sortable;
                                        obj.align = "center";
                                        obj.propName = n.Remark1;
                                        obj.icon = n.icon;
                                        obj.className = n.ValidType;
                                        obj.ValidType = n.ValidType;
                                        obj.formater = n.Formatter;
                                        obj.DataType = n.DataType;
                                        obj.ControlType = n.ControlType;
                                        obj.DataSourceID = n.DataSourceID;
                                        obj.Required = n.Required;
                                        obj.IsVisibleApp = n.IsVisibleApp;
                                        obj.dicID = n.DictionaryID.GetValueOrDefault().ToString();
                                        obj.treeNode=    n.IsQueryParams.GetValueOrDefault();
                                        if(StringHelper.IsNumber(n.Region))
                                        {
                                            obj.Region = int.Parse(n.Region) ;
                                        }
                                     
                                        obj.children = new List<ElementTableOuput>();
                                        var t = GetElementTalbe(DtDic, n, dtDictionaryFieldAccount, new List<ElementTableOuput>());
                                        if (t != null)
                                        {
                                            obj.children.Add(t);
                                        }
                                            elementTableOuputs.Add(obj);
                                    }
                                    else
                                    {
                                      var t=  GetElementTalbe(DtDic, n, dtDictionaryFieldAccount, new List<ElementTableOuput>());
                                        if(t!=null)
                                        obj.children.Add(t);
                                    }
                                }
                                else
                                {
                                    GetElementTalbe(DtDic, n, dtDictionaryFieldAccount, elementTableOuputs);
                                   
                                }
                            }
                            searchForm = GetQuery(searchFormsAll, searchForm, n);
                        }
                        ToList(elementTableOuputs, searchFormsAll, searchForms, datas, id, dev_DictionaryFields, ref left, ref searchForm);
                  
                    }
                    else
                    {


                        foreach (var n in dev_DictionaryFields.OrderBy(m => m.FieldIndex))
                        {

                            if (n.IsVisible.GetValueOrDefault() == false && (n.IsEdit == true || n.IsKey == true) && isDownload == false)
                            {
                                n.IsVisible = true; n.Width = 0;
                            }
                            if (n.IsVisible == true)
                            {

                                //123
                                GetElementTalbe(DtDic, n, dtDictionaryFieldAccount, elementTableOuputs);



                            }
                            if (n.IsVisibleApp == true && appColumns != null)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = n.Width.GetValueOrDefault(100).ToString();
                                obj.label = n.Comment??n.ParameterName;
                                obj.prop = n.ParameterName;
                                if (n.IsFrozen == true)
                                {
                                    obj.fix = "left";
                                }
                                obj.fix = string.IsNullOrEmpty(n.fix) ? obj.fix : n.fix;
                                obj.sortable = string.IsNullOrEmpty(n.sortable) ? null : n.sortable;
                                obj.align = n.align;
                                obj.propName = n.Remark1;
                                obj.icon = n.icon;
                                obj.className = n.ValidType;
                                obj.ValidType = n.ValidType;
                                obj.formater = n.Formatter;
                                obj.DataType = n.DataType;
                                obj.appWidth = string.IsNullOrEmpty(n.AppWith) ? "70" : n.AppWith;
                                if (StringHelper.IsNumber(n.Region))
                                {
                                    obj.Region = int.Parse(n.Region);
                                }
                                obj.IsVisibleApp = n.IsVisibleApp.GetValueOrDefault();
                                obj.formatter = n.Formula;
                             
                                appColumns.Add(obj);
                            }
                             searchForm = GetQuery(searchFormsAll, searchForm, n);
                        }
                        if (jObject != null && jObject.ContainsKey("SDate"))
                        {//**************

                            DateTime startDate = DateTime.Parse(jObject["SDate"].ToString());
                            DateTime endDate = DateTime.Parse(jObject["Edate"].ToString());



                            while (startDate <= endDate)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "60";
                                obj.label = string.Format("{0:MM-dd}({1})", startDate, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(startDate.DayOfWeek).Replace("星期", ""));
                                obj.prop = string.Format("{0:MM-dd}", startDate);
                                obj.prop2 = string.Format("{0:yyyy-MM-dd}", startDate);
                                //obj.DataType = "datetime";
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = jObject["dicID"].ToString();
                                obj.isLook = false;
                                obj.routerName = false;
                                obj.cellStyle = true;
                                startDate = startDate.AddDays(1);
                                elementTableOuputs.Add(obj);
                            }
                        }
                        //  foreach (var dic in elementTable.Where(m => m.ID == 7961 || m.ID == 7959 || m.ID == 6736 || m.ID == 7950 || m.ID == 9009 || (AppInfo.PushType == "9" && m.ID == 7942)).ToList())
                        foreach (var dic in elementTable.Where(m =>  m.ID == 7959 || m.ID == 6736 || m.ID == 7950 || m.ID == 9009 || (AppInfo.PushType == "9" && m.ID == 7942)).ToList())//*****2024011去掉后面的日期
                        {
                            int AutoDays2 = 30;
                            if (jObject != null && jObject.ContainsKey("AutoDays2") && jObject["AutoDays2"] != null)
                            {
                                int.TryParse(jObject["AutoDays2"].ToString(), out AutoDays2);
                            }
                            int startDay = 0;
                            var tmp = elementTable.Where(m => m.ID == dic.ID).FirstOrDefault();
                            if (AppInfo.ConfigStartWeek > 0)
                            {
                                startDay = AppInfo.ConfigStartWeek - (int)DateTime.Now.DayOfWeek;
                            }

                            for (int i = startDay; i < AutoDays2; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "60";
                                obj.label = string.Format("{0:MM-dd}({1})", DateTime.Now.AddDays(i), System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(i).DayOfWeek).Replace("星期", ""));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = dic.ID.ToString();
                                obj.isLook = false;
                                obj.routerName = false;
                                obj.cellStyle = true;
                                obj.prop2 = string.Format("{0:MM-dd}dy2", DateTime.Now.AddDays(i));
                             
                                elementTableOuputs.Add(obj);
                            }
                        }
                        if (elementTable.Where(m => m.ID == 5585).Count() > 0)
                        {
                            for (int i = 0; i < GetSchedulingDays(); i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "60";
                                obj.label = string.Format("{0:MM-dd}({1})", DateTime.Now.AddDays(i), System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(i).DayOfWeek).Replace("星期", ""));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = "{methods:'computedload',type:'input',inputType:'number'}";
                                obj.isEdit = true;
                                obj.isMerge = false;
                                obj.dicID = "5585";
                                obj.isLook = false;
                                obj.routerName = false;
                                obj.cellStyle = true;
                                obj.prop2 = string.Format("{0:MM-dd}dy2", DateTime.Now.AddDays(i));
                                elementTableOuputs.Add(obj);
                            }

                        }
       

                        foreach (int dicID in lstDayList)
                        {


                        

                            if (elementTable.Where(m => m.ID == dicID).Count() > 0)
                            {
                                int freeze = -1;
                                if (jObject != null && freeze == -1 && jObject.ContainsKey("FreezeOrgID"))
                                {
                                    freeze = 0;
                                    DataTable dt = SqlHelper.ExecuteDataTable($"SELECT  FreezeDay FROM  [dbo].[APS_SchedulingFreeze]  where status=1 and OrganizeID={jObject["FreezeOrgID"].ToString()}");
                                    if (dt.Rows.Count > 0)
                                    {
                                        int.TryParse(dt.Rows[0]["FreezeDay"].ToString(), out freeze);
                                    }
                                }
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "25";
                                obj.label = "";
                                obj.prop = "isChecked";
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = "";
                                obj.isEdit = true;
                                obj.isMerge = false;
                                obj.dicID = dicID.ToString();
                                obj.isLook = false;
                                obj.routerName = false;
                                obj.cellStyle = true;
                                obj.prop2 = "";
                               // elementTableOuputs.Insert(0, obj);
                                int SchedulingDays = GetSchedulingDays();

                                DataRow[] drDic = DtDic.Select("DictionaryID=" + dicID);
                                string[] wids = null;
                                if (drDic[0]["WorkFlowInstanceID"].ToString()!="")
                                {

                                    wids= drDic[0]["WorkFlowInstanceID"].ToString().Split(',');
                                }

                                DateTime dateTime1 = DateTime.Now.Date;
                                DateTime dateTime2 = DateTime.Now.Date;
                                if (jObject!=null&&jObject.ContainsKey("DynamicDate1"))
                                {
                                    JArray DynamicDate1 = jObject["DynamicDate1"] as JArray;

                                    DateTime.TryParse(DynamicDate1[0].ToString(), out dateTime1);
                                    DateTime.TryParse(DynamicDate1[1].ToString(), out dateTime2);
                                }

                                else
                                {
                                    dateTime2 = DateTime.Now.Date.AddDays(GetSchedulingDays() + 1);
                                }
                                DataSet dtHoliday = SqlHelper.ExecuteDataset($@"DECLARE @d1 DATETIME
DECLARE @d2 DATETIME

SET @d1='{dateTime1}'
SET @d2='{dateTime2}'

SELECT isnull(SUM(datediff(day, CASE
                       WHEN @d1 >= StartDate THEN @d1
                       ELSE startdate
                     END, CASE
                            WHEN @d2 > EndDate THEN EndDate
                            ELSE @d2
                          END)+1),0) as holidayDays
FROM   APS_Holiday
WHERE  ENDDATE >= GETDATE()
       AND Status = 1
       AND StartDate <= @d2 

SELECT *
FROM   APS_Holiday
WHERE  ENDDATE >= GETDATE()
       AND Status = 1
       AND StartDate <= @d2 

");
                                if (dtHoliday.Tables[0].Rows.Count > 0)
                                {
                                    
                                    dateTime2 = dateTime2.AddDays(int.Parse(dtHoliday.Tables[0].Rows[0][0].ToString()));
                                }
                           
                                // 创建对应的 CultureInfo
                                CultureInfo culture = new CultureInfo(lang);

                                // 获取星期名称
                              
                                while (dateTime1<=dateTime2)
                                {
                                    bool isEdit = dateTime1 >= DateTime.Now.Date; ;
                                    string dayName = culture.DateTimeFormat.GetDayName(dateTime1.DayOfWeek);
                                    if (wids!=null)
                                    { // 这个ID分了白夜班
                                        foreach (string s in wids)
                                        {
                                            string wid = s.Trim();
                                            if (string.IsNullOrEmpty(wid))
                                            {
                                                continue;
                                            }
                                            DataRow[] dataRow = dtWorkTimes.Select("WorkingTimesID='" + wid + "'");
                                            if (dataRow.Length > 0)
                                            {
                                                string ss = dataRow[0]["WorkingTimesName"].ToString();
                                                if (wids.Length == 1)
                                                {
                                                    ss = "";
                                                }
                                                obj = new ElementTableOuput();

                                                obj.width = "80";
                                                obj.label = string.Format("{0:MM-dd}{2}({1})", dateTime1, dayName.Replace("星期", ""),
                                                    ss);
                                                obj.prop = string.Format("{0:MM-dd}{1}", dateTime1, wid);
                                                obj.fix = null;
                                                obj.sortable = null;
                                                obj.active = null;
                                                obj.icon = null;
                                                obj.button = null;
                                                obj.component = "";
                                                obj.isEdit = isEdit;
                                                obj.isMerge = false;
                                                obj.dicID = dicID.ToString();
                                                obj.isLook = false;
                                                obj.routerName = false;
                                                obj.cellStyle = true;
                                                obj.prop2 = string.Format("{0:MM-dd}dy2", dateTime1);
                                                elementTableOuputs.Add(obj);

                                           
                                            }
                                            else
                                            {

                                            }

                                        }

                                    }
                                    else
                                    {
                                        obj = new ElementTableOuput();

                                        obj.width = "60";
                                        obj.label = string.Format("{0:MM-dd}({1})", dateTime1, dayName.Replace("星期", ""));
                                        obj.prop = string.Format("{0:MM-dd}", dateTime1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = "";
                                        obj.isEdit = isEdit;
                                        obj.isMerge = false;
                                        obj.dicID = dicID.ToString();
                                        obj.isLook = false;
                                        obj.routerName = false;
                                        obj.cellStyle = true;
                                        obj.prop2 = string.Format("{0:MM-dd}dy2", dateTime1);
                                        if (dtHoliday.Tables[1].Select($"StartDate<='{dateTime1}' and enddate>='{dateTime1}'").Length > 0)
                                        {
                                            obj.visible = false;
                                            obj.width = "0";
                                        }
                                        elementTableOuputs.Add(obj);

                                     
                                    }
                                    if (drDic[0]["WorkFlowInstanceID"].ToString() != "")
                                    { // 这个ID分了白夜班


                                    }
                                    else
                                    {


                                        obj = new ElementTableOuput();
                                        obj.dicID = dicID.ToString();
                                        obj.width = "0";
                                        obj.label = string.Format("{0:MM-dd}({1})", dateTime1, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(dateTime1.DayOfWeek).Replace("星期", ""));
                                        obj.prop = string.Format("{0:MM-dd}dy", dateTime1);
                                        obj.visible = false;
                                        
                                        //elementTableOuputs.Add(obj);2025-08-15去掉，解决前端不能批量删除的问题
                                    }
                                  
                                    dateTime1 = dateTime1.AddDays(1);
                                }

                           

                                
                            }
                        }

                        if (elementTable.Where(m => m.ID == 6735).Count() > 0 && false)
                        {
                            ElementTableOuput obj = new ElementTableOuput();

                            //obj.width = "60";
                            //obj.label = "选择";
                            //obj.prop = "isChecked";
                            //obj.fix = null;
                            //obj.sortable = null;
                            //obj.active = null;
                            //obj.icon = null;
                            //obj.button = null;
                            //obj.component = "";
                            //obj.isEdit = true;
                            //obj.isMerge = false;
                            //obj.dicID = "6735";
                            //obj.isLook = false;
                            //obj.routerName = false;
                            //obj.cellStyle = true;
                            //obj.prop2 = "";
                            //elementTableOuputs.Insert(0, obj);
                            for (int i = 0; i < GetSchedulingDays(); i++)
                            {
                                obj = new ElementTableOuput();

                                obj.width = "60";
                                obj.label = string.Format("{0:MM-dd}({1})", DateTime.Now.AddDays(i), System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.AddDays(i).DayOfWeek).Replace("星期", ""));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = "";
                                obj.isEdit = true;
                                obj.isMerge = false;
                                obj.dicID = "6735";
                                obj.isLook = false;
                                obj.routerName = false;
                                obj.cellStyle = true;
                                obj.prop2 = string.Format("{0:MM-dd}dy2", DateTime.Now.AddDays(i));
                                elementTableOuputs.Add(obj);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 5584).Count() > 0)
                        {
                            for (int i = -10; i < 0; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "80";
                                obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "5584";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.children = new List<ElementTableOuput>();
                                obj.children.Add(new ElementTableOuput()
                                {
                                    width = "80",
                                    label = "计划数",
                                    prop = "a" + i,
                                    fix = null,
                                    sortable = null,
                                    active = null,
                                    icon = null,
                                    button = null,
                                    component = null,
                                    isEdit = false,
                                    isMerge = false,
                                    dicID = "5584",
                                    isLook = true,
                                    routerName = false

                                });
                                obj.children.Add(new ElementTableOuput()
                                {
                                    width = "80",
                                    label = "实际数",
                                    prop = "b" + i,
                                    fix = null,
                                    sortable = null,
                                    active = null,
                                    icon = null,
                                    button = null,
                                    component = null,
                                    isEdit = false,
                                    isMerge = false,
                                    dicID = "5584",
                                    isLook = true,
                                    routerName = false
                                });
                                obj.children.Add(new ElementTableOuput()
                                {
                                    width = "80",
                                    label = "达成率",
                                    prop = "c" + i,
                                    fix = null,
                                    sortable = null,
                                    active = null,
                                    icon = null,
                                    button = null,
                                    component = null,
                                    isEdit = false,
                                    isMerge = false,
                                    dicID = "5584",
                                    isLook = true,
                                    routerName = false

                                });
                                elementTableOuputs.Add(obj);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 5158).Count() > 0)
                        {
                            for (int i = 0; i < 45; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "60";
                                obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "5158";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.align = "center";
                                if (AppInfo.PushType == "6")
                                {
                                    obj.children = new List<ElementTableOuput>();
                                    obj.children.Add(new ElementTableOuput()
                                    {
                                        width = "80",
                                        label = "需求数",
                                        prop = "a" + i,
                                        fix = null,
                                        sortable = null,
                                        active = null,
                                        icon = null,
                                        button = null,
                                        component = null,
                                        isEdit = false,
                                        isMerge = false,
                                        dicID = "5158",
                                        isLook = true,
                                        routerName = false

                                    });
                                    obj.children.Add(new ElementTableOuput()
                                    {
                                        width = "80",
                                        label = "欠数",
                                        prop = "b" + i,
                                        fix = null,
                                        sortable = null,
                                        active = null,
                                        icon = null,
                                        button = null,
                                        component = null,
                                        isEdit = false,
                                        isMerge = false,
                                        dicID = "5158",
                                        isLook = true,
                                        routerName = false
                                    });
                                    obj.children.Add(new ElementTableOuput()
                                    {
                                        width = "80",
                                        label = "回货数",
                                        prop = "c" + i,
                                        fix = null,
                                        sortable = null,
                                        active = null,
                                        icon = null,
                                        button = null,
                                        component = "{type:'input',inputType:'number'}",
                                        isEdit = true,
                                        isMerge = false,
                                        dicID = "5158",
                                        isLook = true,
                                        routerName = false

                                    });
                                }
                                elementTableOuputs.Add(obj);
                            }

                        }

                        if (elementTable.Where(m => m.ID == 6753).Count() > 0)
                        {
                            for (int i = 0; i < 30; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "60";
                                obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "6753";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.align = "center";

                                elementTableOuputs.Add(obj);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 6731).Count() > 0)
                        {
                            for (int i = -15; i < 0; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "240";
                                obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "6731";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.children = new List<ElementTableOuput>();
                                obj.children.Add(new ElementTableOuput()
                                {
                                    width = "80",
                                    label = "计划数",
                                    prop = "a" + i,
                                    fix = null,
                                    sortable = null,
                                    active = null,
                                    icon = null,
                                    button = null,
                                    component = null,
                                    isEdit = false,
                                    isMerge = false,
                                    dicID = "6731",
                                    isLook = true,
                                    routerName = false

                                }); ;
                                obj.children.Add(new ElementTableOuput()
                                {
                                    width = "80",
                                    label = "报工数",
                                    prop = "b" + i,
                                    fix = null,
                                    sortable = null,
                                    active = null,
                                    icon = null,
                                    button = null,
                                    component = null,
                                    isEdit = false,
                                    isMerge = false,
                                    dicID = "6731",
                                    isLook = true,
                                    routerName = false
                                });
                                obj.children.Add(new ElementTableOuput()
                                {
                                    width = "80",
                                    label = "达成率",
                                    prop = "c" + i,
                                    fix = null,
                                    sortable = null,
                                    active = null,
                                    icon = null,
                                    button = null,
                                    component = null,
                                    isEdit = false,
                                    isMerge = false,
                                    dicID = "6731",
                                    isLook = true,
                                    routerName = false

                                });
                                elementTableOuputs.Add(obj);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 10102).Count() > 0)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "100";
                                obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "10102";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.align = "center";

                                elementTableOuputs.Add(obj);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 7806).Count() > 0)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                ElementTableOuput obj = new ElementTableOuput();

                                obj.width = "100";
                                obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "7806";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.align = "center";

                                elementTableOuputs.Add(obj);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 3039).Count() > 0)
                        {
                            DateTime d1 = DateTime.Now.Date;

                            if (jObject != null && jObject.ContainsKey("YearMonth"))
                            {
                                string ym = jObject["YearMonth"].ToString();
                                d1 = DateTime.Parse(ym.Substring(0, 4) + "-" + ym.Substring(4, 2) + "-01");
                            }
                            else
                            {
                                d1 = DateTime.Now.AddDays(-DateTime.Now.Date.Day).AddDays(1);
                            }
                            DateTime d2 = d1;
                            while (d2 < d1.AddMonths(1))
                            {
                                ElementTableOuput obj = new ElementTableOuput();
                                obj.width = "170";
                                obj.label = string.Format("{0:dd}", d2);
                                obj.prop = string.Format("{0:dd}", d2);
                                obj.fix = null;
                                obj.sortable = null;
                                obj.active = null;
                                obj.icon = null;
                                obj.button = null;
                                obj.component = null;
                                obj.isEdit = false;
                                obj.isMerge = false;
                                obj.dicID = "3039";
                                obj.isLook = true;
                                obj.routerName = false;
                                obj.align = "center";
                                obj.children = new List<ElementTableOuput>();
                                elementTableOuputs.Add(obj);
                                ElementTableOuput obj2 = new ElementTableOuput();

                                obj2.width = "60";
                                obj2.label = string.Format("入", d2);
                                obj2.prop = string.Format("入", d2);
                                obj2.fix = null;
                                obj2.sortable = null;
                                obj2.active = null;
                                obj2.icon = null;
                                obj2.button = null;
                                obj2.component = null;
                                obj2.isEdit = false;
                                obj2.isMerge = false;
                                obj2.dicID = "3039";
                                obj2.isLook = true;
                                obj2.routerName = false;
                                obj2.align = "center";

                                obj.children.Add(obj2);
                                obj2 = new ElementTableOuput();

                                obj2.width = "60";
                                obj2.label = string.Format("出", d2);
                                obj2.prop = string.Format("出", d2);
                                obj2.fix = null;
                                obj2.sortable = null;
                                obj2.active = null;
                                obj2.icon = null;
                                obj2.button = null;
                                obj2.component = null;
                                obj2.isEdit = false;
                                obj2.isMerge = false;
                                obj2.dicID = "3039";
                                obj2.isLook = true;
                                obj2.routerName = false;
                                obj2.align = "center";

                                elementTableOuputs.Add(obj);
                                obj.children.Add(obj2);
                                d2 = d2.AddDays(1);
                            }

                        }
                        if (elementTable.Where(m => m.ID == 7838).Count() > 0 || elementTable.Where(m => m.ID == 7839).Count() > 0 || elementTable.Where(m => m.ID == 7834).Count() > 0 || elementTable.Where(m => m.ID == 7835).Count() > 0 || elementTable.Where(m => m.ID == 7836).Count() > 0 || elementTable.Where(m => m.ID == 7837).Count() > 0 || elementTable.Where(m => m.ID == 7832).Count() > 0)
                        {
                            if (jObject == null)
                            {

                                String dicID = null;
                                int Days = DateTime.Now.Day;
                                if (elementTable.Where(m => m.ID == 7838).Count() > 0)
                                {
                                    for (int j = Days; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7838";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7839).Count() > 0)
                                {
                                    for (int j = Days - 1; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7839";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7834).Count() > 0)
                                {
                                    for (int j = Days; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7834";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7835).Count() > 0)
                                {
                                    for (int j = Days; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7835";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7836).Count() > 0)
                                {
                                    for (int j = Days; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7836";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7837).Count() > 0)
                                {
                                    for (int j = Days - 1; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7837";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }
                                }
                                if (elementTable.Where(m => m.ID == 7832).Count() > 0)
                                {
                                    for (int j = Days - 1; j >= 0; j--)
                                    {
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7832";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                    }
                                }
                                //int Days = DateTime.Now.Day;
                                //DateTime d1 = DateTime.Now.Date;
                                //DateTime d2 = DateTime.Now.Date;
                                //while (d1.Month == d2.Month)
                                //{
                                //    ElementTableOuput obj = new ElementTableOuput();
                                //    d2 = d2.AddDays(-1);
                                //}
                                //DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1);
                                //DateTime d2 = DateTime.Now;
                                //if(!string.IsNullOrEmpty(jObject["month"].ToString()))
                                //{
                                //    d1 = DateTime.Parse(jObject["month"].ToString() + "-01");
                                //    d2 = d1.AddMonths(1).AddDays(-1);
                                //    if(d1.Month==DateTime.Now.Month)
                                //    {
                                //        d2 = DateTime.Now.Date;
                                //    }
                                //}
                                //while(d1<=d1)
                                //{

                                //    //todo
                                //    d1 = d1.AddDays(1);
                                //}

                                //for (int j = Days - 1; j >= 0; j++)
                                //{
                                //    ElementTableOuput obj = new ElementTableOuput();
                                //    obj.width = "100";
                                //    obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                //    obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(-j));
                                //    obj.fix = null;
                                //    obj.sortable = null;
                                //    obj.active = null;
                                //    obj.icon = null;
                                //    obj.button = null;
                                //    obj.component = null;
                                //    obj.isEdit = false;
                                //    obj.isMerge = false;
                                //    obj.dicID = dicID;
                                //    obj.isLook = true;
                                //    obj.routerName = false;
                                //    obj.align = "center";

                                //    elementTableOuputs.Add(obj);

                                //}
                            }
                            else
                            {
                                String dicID = null;
                                if (elementTable.Where(m => m.ID == 7838).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.Date.AddDays(-DateTime.Now.Day).AddDays(1); // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7838";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7839).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1); // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7839";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7834).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1); // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7834";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7835).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1).Date; // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7835";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    };

                                }
                                if (elementTable.Where(m => m.ID == 7836).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1).Date; // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7836";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    }

                                }
                                if (elementTable.Where(m => m.ID == 7837).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1); // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7837";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    };

                                }
                                if (elementTable.Where(m => m.ID == 7832).Count() > 0)
                                {
                                    DateTime d1 = DateTime.Now.AddDays(-DateTime.Now.Day).AddDays(1); // 获取是1号
                                    DateTime d2 = DateTime.Now.Date;
                                    if (!string.IsNullOrEmpty(jObject["PlanDay"].ToString()))
                                    {
                                        int month = DateTime.Now.Month;
                                        int objMonth = int.Parse((jObject["PlanDay"].ToString()).Substring(5, 2));
                                        if (objMonth != month)
                                        {
                                            d1 = DateTime.Parse(jObject["PlanDay"].ToString() + "-01");
                                            d2 = d1.AddMonths(1).AddDays(-1);
                                        }

                                    }
                                    while (d1 <= d2)
                                    {
                                        //todo
                                        ElementTableOuput obj = new ElementTableOuput();
                                        obj.width = "100";
                                        obj.label = string.Format("{0:MM-dd}", d1);
                                        obj.prop = string.Format("{0:MM-dd}", d1);
                                        obj.fix = null;
                                        obj.sortable = null;
                                        obj.active = null;
                                        obj.icon = null;
                                        obj.button = null;
                                        obj.component = null;
                                        obj.isEdit = false;
                                        obj.isMerge = false;
                                        obj.dicID = "7832";
                                        obj.isLook = true;
                                        obj.routerName = false;
                                        obj.align = "center";
                                        elementTableOuputs.Add(obj);
                                        d1 = d1.AddDays(1);
                                    };

                                }
                                //DateTime d1 = DateTime.Now.Date;
                                //DateTime d2 = DateTime.Now.Date;
                                //while (d1.Month == d2.Month)
                                //{
                                //    ElementTableOuput obj = new ElementTableOuput();
                                //    d2 = d2.AddDays(-1);
                                //}


                                //while (d1 <= d2)
                                //{
                                //    //todo
                                //    ElementTableOuput obj = new ElementTableOuput();
                                //    obj.width = "100";
                                //    obj.label = string.Format("{0:MM-dd}", d2);
                                //    obj.prop = string.Format("{0:MM-dd}", d2);
                                //    obj.fix = null;
                                //    obj.sortable = null;
                                //    obj.active = null;
                                //    obj.icon = null;
                                //    obj.button = null;
                                //    obj.component = null;
                                //    obj.isEdit = false;
                                //    obj.isMerge = false;
                                //    obj.dicID = dicID;
                                //    obj.isLook = true;
                                //    obj.routerName = false;
                                //    obj.align = "center";
                                //    elementTableOuputs.Add(obj);
                                //    d1 = d1.AddDays(1);
                                //}

                            }


                            if (elementTable.Where(m => m.ID == 7812).Count() > 0)
                            {

                                for (int i = 1; i < 45; i++)
                                {
                                    ElementTableOuput obj = new ElementTableOuput();

                                    obj.width = "100";
                                    obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                    obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                    obj.fix = null;
                                    obj.sortable = null;
                                    obj.active = null;
                                    obj.icon = null;
                                    obj.button = null;
                                    obj.component = null;
                                    obj.isEdit = false;
                                    obj.isMerge = false;
                                    obj.dicID = "7812";
                                    obj.isLook = true;
                                    obj.routerName = false;
                                    obj.align = "center";

                                    elementTableOuputs.Add(obj);
                                }


                            }
                        }

                        if (jObject == null)
                        {


                            if (elementTable.Where(m => m.ID == 7812).Count() > 0)
                            {

                                for (int i = 1; i < 45; i++)
                                {
                                    ElementTableOuput obj = new ElementTableOuput();

                                    obj.width = "100";
                                    obj.label = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                    obj.prop = string.Format("{0:MM-dd}", DateTime.Now.AddDays(i));
                                    obj.fix = null;
                                    obj.sortable = null;
                                    obj.active = null;
                                    obj.icon = null;
                                    obj.button = null;
                                    obj.component = null;
                                    obj.isEdit = false;
                                    obj.isMerge = false;
                                    obj.dicID = "7812";
                                    obj.isLook = true;
                                    obj.routerName = false;
                                    obj.align = "center";

                                    elementTableOuputs.Add(obj);
                                }


                            }
                        }
                        ToList(elementTableOuputs, searchFormsAll, searchForms, datas, id, dev_DictionaryFields, ref left, ref searchForm);
                    }
                    if(setDt != null|| (jObject!=null&&jObject.ContainsKey("SDate")==true))
                    {//有委托或者自定义日期的不缓存
                 
                    }
                    else if(false)
                    {
                        APSCatchElemet.Add(key, new CatchElemet()
                        {
                            appColumns = appColumns,
                            datas = datas,
                            searchFormsAll = searchFormsAll,
                            searchForms = searchForms,
                            elementTableOuputs = elementTableOuputs,
                            lstAllColumnCommon = lstAllColumnCommon,
                            lstColumnCommon = lstColumnCommon

                        });
                    }
              


                }
                try
                {
                    if (dtLanguage != null)
                    {
                        foreach (var items in datas)
                        {
                            foreach (var item in items)
                            {

                                DataRow[] dataRows = dtLanguage.Select("zhcn='" + StringHelper.ReplaceSQL(item.label) + "'");
                                if (dataRows.Length > 0)
                                {
                                    item.label = dataRows[0]["TargetCode"].ToString();
                                }
                            }
                        }
                        foreach (var items in searchForms)
                        {
                            foreach (var item in items)
                            {

                                DataRow[] dataRows = dtLanguage.Select("zhcn='" + StringHelper.ReplaceSQL(item.label) + "'");
                                if (dataRows.Length > 0)
                                {
                                    item.label = dataRows[0]["TargetCode"].ToString();
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {

                }
            }
            catch (Exception ex)
            {
                msg = ex.StackTrace;
                result = false;
            }



        }
        private void ToList(List<ElementTableOuput> elementTableOuputs, List<SearchForm> searchFormsAll, List<List<SearchForm>> searchForms, List<List<ElementTableOuput>> datas, string id, List<V_DictionaryField> dev_DictionaryFields, ref int left, ref SearchForm searchForm)
        {
            foreach (string dicID in id.Split(','))
            {
                if (string.IsNullOrEmpty(dicID))
                {
                    continue;
                }
                var thisFormDicID = searchFormsAll.Where(m => m.dicID == dicID).ToList();
                var thisFormQuery=searchFormQuery.Where(m => m.dicID == dicID).ToList();
                if (dicID == "6663"  || dicID == "6676" || dicID == "6701" || dicID == "6741" || dicID == "6740" || dicID == "6739" || dicID == "6742" || dicID == "7775" || dicID == "10102" || dicID == "7801" || dicID == "7806" || dicID == "7812" || listDaysDicID.Contains(dicID))
                {
                    searchForm = new SearchForm();
                    searchForm.placeholder = "请输入日期";
                    searchForm.label = "日期";
                    searchForm.prop = "AutoDays";
                    searchForm.dicID = dicID;
                    if (dicID == "6663")
                    {
                        searchForm.value = StringHelper.GetDefaultValue("", "过去30天", "AutoDays", true);
                    }
                    else if (dicID == "6668" || dicID == "6676")
                    {
                        searchForm.value = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(0)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(10)) };// StringHelper.GetDefaultValue("", "未来30天", "AutoDays", true);
                    }
                    else if (dicID == "6668" || dicID == "6676")
                    {
                        searchForm.value = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(0)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(10)) };// StringHelper.GetDefaultValue("", "未来30天", "AutoDays", true);
                    }
                    else if (dicID == "7812")
                    {
                        searchForm.value = new string[2] { string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(1)), string.Format("{0:yyyy-MM-dd}", DateTime.Now.Date.AddDays(30)) };// StringHelper.GetDefaultValue("", 
                    }
                    searchForm.type = "Daterange";
                    searchForm.width = "260";
                    //if (searchFormsAll.Where(m => m.dicID == dicID).Count() == 0)
                    {

                        thisFormDicID.Add(searchForm);
                    }

                }
               
                searchFormQuerys.Add(thisFormQuery);
                searchForms.Add(thisFormDicID);
                left = 0;


                int thisID = int.Parse(dicID);
                var tmp = elementTableOuputs.Where(m => m.dicID == dicID).ToList();

                Dev_Dictionary dev_Dictionary = _dbContext.DevDictionaries.AsNoTracking().Where(m => m.DictionaryID == thisID).FirstOrDefault();
                if (dev_Dictionary != null && dev_Dictionary.IsShowCheck == true)
                {


                    ElementTableOuput obj = new ElementTableOuput();

                    obj.width = "25";
                    obj.label = "";
                    obj.prop = "isChecked";
                    obj.fix = tmp.Where(m => !string.IsNullOrEmpty(m.fix)).Count() > 0 ? "left" : "";
                    obj.sortable = null;
                    obj.active = null;
                    obj.icon = null;
                    obj.button = null;
                    obj.component = "";
                    obj.isEdit = true;
                    obj.isMerge = false;
                    obj.dicID = thisID.ToString();
                    obj.isLook = false;
                    obj.routerName = false;
                    obj.cellStyle = true;
                    obj.prop2 = "";
                    obj.IsSelect = true;
                    tmp.Insert(0, obj);
                }
                if (tmp.Where(m => m.prop == "RowNumber").Count() == 0)
                {
                   // tmp.Insert(1,new ElementTableOuput { prop = "RowNumber", width = "0", visible = false, pageSize = dev_Dictionary.PageSize, IsSelect = dev_Dictionary.IsShowCheck.GetValueOrDefault() });
                }
                foreach (var item in tmp)
                {
                    item.FixCount = tmp.Where(m => !string.IsNullOrEmpty(m.fix)).Count();
                }
           


                //if (dev_Account != null)
                //{
                //    foreach (var obj in dev_Account.ButtonMenuRoleMap.Where(m => m.MenuCode == v_DictionaryField.MenuCode))
                //    {
                //        SearchForm button = new SearchForm();
                //        button.label = obj.MenuName;
                //        button.icon = obj.Icon;
                //        button.type = obj.ButtonType;
                //        button.methods = obj.OnClick;
                //        //searchHandle.Add(button)
                //    }
                //}

                foreach (var obj in tmp)
                {
                    // foreach (var obj1 in obj.Value)
                    {
                        if (!string.IsNullOrEmpty(obj.fix))
                        {
                            obj.left = left;
                            left += int.Parse(obj.width);
                        }

                    }

                }
                datas.Add(tmp);

                tmp = lstColumnCommon.Where(m => m.dicID == dicID).ToList();
                lstAllColumnCommon.Add(tmp);

            }
        }

        /// <summary>
        /// 获取到elementtable的列
        /// </summary>
        /// <param name="DtDic">配置表</param>
        /// <param name="n">列配置</param>
        /// <param name="dtDictionaryFieldAccount">当前个人的配置</param>
        /// <param name="elementTableOuputs">已经配置的记录集合</param>
        /// <returns></returns>
        public ElementTableOuput  GetElementTalbe(DataTable DtDic,V_DictionaryField n,DataTable dtDictionaryFieldAccount,List<ElementTableOuput> elementTableOuputs)
        {
            ElementTableOuput obj = new ElementTableOuput();
            obj.width = n.Width.GetValueOrDefault(100).ToString();
            obj.label = n.Comment??n.ParameterName;
            obj.prop = n.ParameterName;
            if (n.IsFrozen == true)
            {
                obj.fix = "left";
            }
            obj.fix = string.IsNullOrEmpty(n.fix) ? obj.fix : n.fix;
            obj.sortable = string.IsNullOrEmpty(n.sortable) ? null : n.sortable;
            obj.align = n.align;
            obj.propName = n.Remark1;
            obj.icon = n.icon;
            obj.className = n.ValidType;
            obj.ValidType = n.ValidType;
            obj.formater = n.Formatter;
            obj.DataType = n.DataType;
            obj.ControlType = n.ControlType;
            obj.DataSourceID = n.DataSourceID;
            obj.Required = n.Required;
            obj.IsVisibleApp = n.IsVisibleApp;
            obj.treeNode = n.IsQueryParams.GetValueOrDefault();
            obj.formatter = n.Formula;
            if (StringHelper.IsNumber(n.Region))
            {
                obj.Region = int.Parse(n.Region);
            }
            DataRow[] dataRows1 = DtDic.Select("DictionaryID=" + n.DictionaryID);
            if (dataRows1.Length > 0)
            {
                obj.pageSize = dataRows1[0]["PageSize"].ToString() == "" ? 20 : int.Parse(dataRows1[0]["PageSize"].ToString());
                obj.IsSelect = dataRows1[0]["IsShowCheck"].ToString().ToLower() == "true";
            }

         
            try
            {
                if (!string.IsNullOrEmpty(n.button))
                {
                    obj.button = n.button;
                }

                if (!string.IsNullOrEmpty(n.active))
                {
                    obj.active = n.active;
                }
                if (!string.IsNullOrEmpty(n.component))
                {
                    obj.component = n.component;
                }
                if (!string.IsNullOrEmpty(n.icon))
                {
                    obj.icon = n.icon;
                }
            }
            catch (Exception ex)
            {

            }


            obj.isEdit = n.IsEdit.GetValueOrDefault();
            if ((obj.isEdit && string.IsNullOrEmpty(obj.component))||!string.IsNullOrEmpty(n.DataSourceID))
            {
                switch (n.ControlType)
                {
                    case "textbox":

                    case "el-input":
                        obj.component = "{type:'input',inputType:'text'}";
                        break;
                    case "textarea":
                        obj.component = "{type:'input',inputType:'textarea'}";
                        break;
                    case "el-select":
                    case "combobox":
                    case "comboboxMultiple":

                        if (!string.IsNullOrEmpty(n.DataSourceID))
                        {
                           // DataTable dataTable = SqlHelper.ExecuteDataTable("SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");

                            DataTable dataTable = null;
                            if (lstDataSource.ContainsKey(n.DataSourceID) == false)
                            {
                                dataTable = SqlHelper.ExecuteDataTable("SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");
                                lstDataSource.Add(n.DataSourceID, dataTable);
                            }
                            else
                            {
                                dataTable = lstDataSource[n.DataSourceID];
                            }
                            if (dataTable.Rows.Count > 0)
                            {
                                if (string.IsNullOrEmpty(obj.component))
                                {
                                    obj.component = "{type:'select',prop:'" + dataTable.Rows[0]["DataSouceName"].ToString() + "'}";
                                }
                                //if (obj.isEdit == false)
                                //{
                                //   // obj.component = "{type:'input',inputType:'text'}";
                                //    obj.ControlType = "textbox";
                                //}
                                obj.DataSourceName = dataTable.Rows[0]["DataSouceName"].ToString();
                                string value = dataTable.Rows[0]["DataValue"].ToString();
                                string label = dataTable.Rows[0]["DataText"].ToString();
                                string USQL = dataTable.Rows[0]["USQL"].ToString();
                                if (dev_Account != null)
                                {
                                    USQL = USQL.Replace("{CenterID}", dev_Account.CenterID.ToString());
                                    USQL = USQL.Replace("{WorkFlowInstanceID}", dev_Account.WorkFlowInstanceID);
                                    USQL = USQL.Replace("{GroupID}", dev_Account.GroupID.ToString());
                                    USQL = USQL.Replace("{Account}", dev_Account.Account.ToString());
                                }

                                DataTable dtUSQL = SqlHelper.ExecuteDataTable(USQL);
                                if (string.IsNullOrEmpty(n.Remark1))
                                {
                                    obj.propName = obj.prop + "Text";
                                }

                                if (dtUSQL.Columns.Contains("value") == false)
                                {
                                    dtUSQL.Columns.Add("value", dtUSQL.Columns[value].DataType);
                                }
                                if (dtUSQL.Columns.Contains("label") == false)
                                {
                                    dtUSQL.Columns.Add("label", dtUSQL.Columns[label].DataType);
                                }
                                if (dtUSQL.Columns.Contains("text") == false)
                                {
                                    dtUSQL.Columns.Add("text", dtUSQL.Columns[label].DataType);
                                }
                                //if (!string.IsNullOrEmpty(n.DefaultValue))
                                //{

                                //    if (n.DefaultValue.IndexOf("选择项") > -1)
                                //    {
                                //        searchForm.value = dtUSQL.Rows[int.Parse(searchForm.value.ToString())][value].ToString();
                                //    }
                                //    else
                                //    {
                                //        if (dtUSQL.Columns.IndexOf("OrganizeID") > -1)
                                //        {
                                //            if (dtUSQL.Select("OrganizeID=" + dev_Account.OrganizeID).Length > 0)
                                //            {
                                //                searchForm.value = n.DefaultValue.Replace("{OrganizeID}", dev_Account.OrganizeID.ToString());
                                //            }
                                //            else
                                //            {
                                //                searchForm.value = n.DefaultValue.Replace("{OrganizeID}", "");
                                //            }

                                //        }
                                //    }
                                //}
                               // foreach (DataRow dataRow in dtUSQL.Rows)
                                {
                                    // if (dtUSQL.Columns.Contains(value) == false)
                                    {
                                        if (value.ToLower() != "value")
                                        {
                                            dtUSQL.Columns["value"].Expression = value;
                                        }
                                        if (value.ToLower() != "label")
                                        {
                                            dtUSQL.Columns["label"].Expression = label;

                                        }
                                        if (value.ToLower() != "text")
                                        {

                                            dtUSQL.Columns["text"].Expression = label;
                                        }
                                     
                                      
                                        //dataRow["value"] = dataRow[value];
                                        //dataRow["label"] = dataRow[label];
                                        //dataRow["text"] = dataRow[label];
                                    }

                                }

                                if(dataTable.Select("RowFilter='' or RowFilter is null").Length == 1)
                                {
                                    obj.items = dtUSQL;
                                }
                               
                            }
                        }

                        break;
                    case "el-radio":
                    case "radio":
                        obj.component = "{type:'checkbox'}";
                        break;
                    case "numberbox":
                        obj.component = "{inputType:'number'}";
                        break;
                    case "checkbox":
                    case "el-checkbox":
                        obj.component = "{type:'checkbox'}";
                        break;
                    case "datebox":
                    case "el-date-picker":
                        obj.component = "{type:'date'}";
                        break;
                    case "monthrange":
                        obj.component = "{type:'monthrange'}";
                        break;
                    case "month":
                        obj.component = "{type:'month'}";
                        break;
                    case "year":
                        obj.component = "{type:'year'}";
                        break;
                    default:
                        obj.component = "{type:'"+n.ControlType+"'}";
                        break;
                }

            }
            if (n.ParameterName == "QueryMethod")
            {
                obj.DataSourceName = obj.DataSourceID= "QueryMethodProp";
               
                obj.component = "{type:'select',prop:'QueryMethodProp'}";
                obj.propName = "QueryMethodText";
              
            }
            obj.isMerge = n.isMerge.GetValueOrDefault();
            obj.dicID = n.DictionaryID.ToString();
            obj.isLook = false;
            obj.routerName = n.RouterName.GetValueOrDefault();
            lstColumnCommon.Add(obj);
            if (dtDictionaryFieldAccount != null&&false)
            {//查询个人的配置情况

                DataRow[] dataRows = dtDictionaryFieldAccount.Select("DictionaryID=" + n.DictionaryID);
                if (dataRows.Length > 0)
                {
                    dataRows = dtDictionaryFieldAccount.Select("DictionaryID=" + n.DictionaryID + " AND ParameterName='" + n.ParameterName + "' ");
                    if (dataRows.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        n.Comment = dataRows[0]["Comment"].ToString();
                    }
                }

            }

            elementTableOuputs.Add(obj);


            //Spread spread = new Spread();
            //spread.displayName = n.Comment;
            //spread.name = n.ParameterName;
            //spread.size = n.Width.GetValueOrDefault(100);
            //spread.formatter = n.Formatter;
            if (n.IsEdit.GetValueOrDefault() == true&&false)//20240126pa
            {
                switch (n.ControlType)
                {

                    case "el-select":
                    case "combobox":
                    case "comboboxMultiple":

                        if (!string.IsNullOrEmpty(n.DataSourceID))
                        {
                           // DataTable dataTable = SqlHelper.ExecuteDataTable("SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");
                            DataTable dataTable = null;
                            if (lstDataSource.ContainsKey(n.DataSourceID) == false)
                            {
                                dataTable = SqlHelper.ExecuteDataTable("SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");
                                lstDataSource.Add(n.DataSourceID, dataTable);
                            }
                            else
                            {
                                dataTable = lstDataSource[n.DataSourceID];
                            }
                            string value = "", label = "";
                            if (dataTable.Rows.Count > 0)
                            {

                                value = dataTable.Rows[0]["DataValue"].ToString();
                                label = dataTable.Rows[0]["DataText"].ToString();
                                string USQL = dataTable.Rows[0]["USQL"].ToString();
                                USQL = USQL.Replace("{CenterID}", dev_Account.CenterID.ToString());
                                USQL = USQL.Replace("{WorkFlowInstanceID}", dev_Account.WorkFlowInstanceID);
                                USQL = USQL.Replace("{Account}", dev_Account.Account.ToString());
                                USQL = USQL.Replace("{GroupID}", dev_Account.GroupID.ToString());
                                DataTable dtUSQL = SqlHelper.ExecuteDataTable(USQL);
                                if (string.IsNullOrEmpty(n.Remark1))
                                {
                                    obj.propName = obj.prop + "Text";
                                }

                                if (dtUSQL.Columns.Contains("value") == false)
                                {
                                    dtUSQL.Columns.Add("value", dtUSQL.Columns[value].DataType);
                                }
                                if (dtUSQL.Columns.Contains("label") == false)
                                {
                                    dtUSQL.Columns.Add("label", dtUSQL.Columns[label].DataType);
                                }
                                if (dtUSQL.Columns.Contains("text") == false)
                                {
                                    dtUSQL.Columns.Add("text", dtUSQL.Columns[label].DataType);
                                }
                                //if (!string.IsNullOrEmpty(n.DefaultValue))
                                //{

                                //    if (n.DefaultValue.IndexOf("选择项") > -1)
                                //    {
                                //        searchForm.value = dtUSQL.Rows[int.Parse(searchForm.value.ToString())][value].ToString();
                                //    }
                                //    else
                                //    {
                                //        if (dtUSQL.Columns.IndexOf("OrganizeID") > -1)
                                //        {
                                //            if (dtUSQL.Select("OrganizeID=" + dev_Account.OrganizeID).Length > 0)
                                //            {
                                //                searchForm.value = n.DefaultValue.Replace("{OrganizeID}", dev_Account.OrganizeID.ToString());
                                //            }
                                //            else
                                //            {
                                //                searchForm.value = n.DefaultValue.Replace("{OrganizeID}", "");
                                //            }

                                //        }
                                //    }
                                //}
                                //foreach (DataRow dataRow in dtUSQL.Rows)
                                //{
                                //    // if (dtUSQL.Columns.Contains(value) == false)
                                //    {
                                //        dataRow["value"] = dataRow[value];
                                //        dataRow["label"] = dataRow[label];
                                //        dataRow["text"] = dataRow[label];
                                //    }

                                //}
                                if (value.ToLower() != "value")
                                {
                                    dtUSQL.Columns["value"].Expression = value;
                                }
                                if (value.ToLower() != "label")
                                {
                                    dtUSQL.Columns["label"].Expression = label;

                                }
                                if (value.ToLower() != "text")
                                {

                                    dtUSQL.Columns["text"].Expression = label;
                                }
                                //   spread.cellStyle = 4;
                                //  spread.items = dtUSQL;
                            }

                        }

                        break;
                        //case "el-radio":
                        //case "radio":
                        //    searchForm.type = "Radio";
                        //    break;
                        //case "RadioButton":
                        //    searchForm.type = "RadioButton";
                        //    break;
                        //case "checkbox":
                        //case "el-checkbox":
                        //    searchForm.type = "Checkbox";
                        //    break;
                        //case "datebox":
                        //case "el-date-picker":
                        //    searchForm.type = "Daterange";
                        //    break;
                }
            }
            //   spread.isEdit = n.IsEdit.GetValueOrDefault();
            // spreads.Add(spread);

            return obj;

        }
        private SearchForm GetQuery(List<SearchForm> searchFormsAll, SearchForm searchForm, V_DictionaryField n)
        {
           // if (n.IsVisible == true)
            {//查询
                searchForm = new SearchForm();
                searchForm.placeholder = "请输入" + n.Comment;
                searchForm.label = n.Comment;
                searchForm.prop = n.ParameterName;
                searchForm.dicID = n.DictionaryID.GetValueOrDefault().ToString();
                if (n.DefaultValue == "MRP控制者")
                {
                    n.DefaultValue = dev_Account.WorkFlowInstanceID;
                }
                searchForm.value = StringHelper.GetDefaultValue("", n.DefaultValue, n.ParameterName, true);
                List<Dictionary<string,string>> lstQery=new List<Dictionary<string, string>>();
                foreach (int query in Enum.GetValues(typeof(MssqlQueryMethods.QueryMethod)))
                {
                    Dictionary<string, string> d= new Dictionary<string, string>();
                    d.Add("value", query.ToString());
                    d.Add("label", Enum.GetName(typeof(MssqlQueryMethods.QueryMethod), query));
                    searchForm.queryType.Add(d);
                    //Enum.GetName(typeof(QueryMethod), query),query.ToString()
                }
                
                switch (n.ControlType)
                {
                    case "textbox":
                    case "textarea":
                    case "el-input":
                        searchForm.type = "Input";
                        break;
                    case "el-select":
                    case "combobox":
                    case "comboboxMultiple":
                        searchForm.type = "Select";
                        if (!string.IsNullOrEmpty(n.DataSourceID))
                        {
                          //  DataTable dataTable = SqlHelper.ExecuteDataTable("SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");
                            DataTable dataTable = null;
                            if (lstDataSource.ContainsKey(n.DataSourceID) == false)
                            {
                                dataTable = SqlHelper.ExecuteDataTable("SELECT * FROM [Dev_DataSource] WHERE [DataSourceID]='" + n.DataSourceID + "'");
                                lstDataSource.Add(n.DataSourceID, dataTable);
                            }
                            else
                            {
                                dataTable = lstDataSource[n.DataSourceID];
                            }
                            string value = "", label = "";
                            if (dataTable.Rows.Count > 0)
                            {

                                value = dataTable.Rows[0]["DataValue"].ToString();
                                label = dataTable.Rows[0]["DataText"].ToString();
                                string USQL = dataTable.Rows[0]["USQL"].ToString();
                                try
                                {
                                    USQL = USQL.Replace("{CenterID}", dev_Account.CenterID.ToString());
                                    USQL = USQL.Replace("{WorkFlowInstanceID}", dev_Account.WorkFlowInstanceID);
                                    USQL = USQL.Replace("{Account}", dev_Account.Account.ToString());
                                    USQL = USQL.Replace("{GroupID}", dev_Account.GroupID.ToString());
                                }
                                catch
                                { }
                                DataTable dtUSQL = SqlHelper.ExecuteDataTable(USQL);

                                if (dtUSQL.Columns.Contains("value") == false)
                                {
                                    dtUSQL.Columns.Add("value", dtUSQL.Columns[value].DataType);
                                }
                                if (dtUSQL.Columns.Contains("label") == false)
                                {
                                    dtUSQL.Columns.Add("label", dtUSQL.Columns[label].DataType);
                                }

                                if (!string.IsNullOrEmpty(n.DefaultValue))
                                {

                                    if (n.DefaultValue.IndexOf("选择项") > -1)
                                    {
                                        searchForm.value = dtUSQL.Rows[int.Parse(searchForm.value.ToString())][value].ToString();
                                    }
                                    else
                                    {
                                        if (dtUSQL.Columns.IndexOf("OrganizeID") > -1)
                                        {
                                            if (dtUSQL.Select("OrganizeID=" + dev_Account.OrganizeID).Length > 0)
                                            {
                                                searchForm.value = n.DefaultValue.Replace("{OrganizeID}", dev_Account.OrganizeID.ToString());
                                            }
                                            else
                                            {
                                                searchForm.value = n.DefaultValue.Replace("{OrganizeID}", "");
                                            }

                                        }
                                    }
                                }
                                //foreach (DataRow dataRow in dtUSQL.Rows)
                                //{
                                //    // if (dtUSQL.Columns.Contains(value) == false)
                                //    {
                                //        dataRow["value"] = dataRow[value];
                                //        dataRow["label"] = dataRow[label];
                                //    }

                                //}
                                if (value.ToLower() != "value")
                                {
                                    dtUSQL.Columns["value"].Expression = value;
                                }
                                if (value.ToLower() != "label")
                                {
                                    dtUSQL.Columns["label"].Expression = label;

                                }
                               

                                //if (dtUSQL.Columns.Contains("text") == false)
                                //{
                                //    dtUSQL.Columns.Add("text", dtUSQL.Columns[label].DataType);
                                //}
                                //DataTable dtSoureDistinct= dtUSQL.Clone();
                                //foreach(DataRow dataRow1 in dtUSQL.Rows)
                                //{
                                //    DataRow newRow1 = dtSoureDistinct.NewRow();
                                //    DataRow[] rows = dtSoureDistinct.Select("value='" + dataRow1["value"] + "'");
                                //    if (rows.Length == 0)
                                //    {

                                //        newRow1.ItemArray = dataRow1.ItemArray;
                                //        dtSoureDistinct.Rows.Add(newRow1);
                                //    }
                                //}
                                var query = dtUSQL.AsEnumerable()
              .GroupBy(row => row["value"])
              .Select(g => g.First());
                                DataTable distinctTable = null;
                                if (query.Count() > 0)
                                {
                                    distinctTable=query.CopyToDataTable();
                                }
                                else {

                                    distinctTable = dtUSQL;
                                }

                                    searchForm.options = distinctTable;

                            }
                            if (n.ControlType.ToLower() == "comboboxMultiple".ToLower())
                            {
                                searchForm.multiple = true;

                            }
                        }

                        break;
                    case "el-radio":
                    case "radio":
                        searchForm.type = "Radio";
                        break;
                    case "RadioButton":
                        searchForm.type = "RadioButton";
                        break;
                    case "checkbox":
                    case "el-checkbox":
                        searchForm.type = "Select";
                        DataTable dt = new DataTable();
                        dt.Columns.Add("label");
                        dt.Columns.Add("value");
                        DataRow newRow = dt.NewRow();
                        newRow["label"] = "true";
                        newRow["value"] = "1";
                        dt.Rows.Add(newRow);
                        newRow = dt.NewRow();
                        newRow["label"] = "false";
                        newRow["value"] = "0";
                        dt.Rows.Add(newRow);
                        newRow = dt.NewRow();
                        newRow["label"] = "全部";
                        newRow["value"] = "";
                        dt.Rows.Add(newRow);
                        searchForm.width = "60";
                        searchForm.options = dt;
                        break;
                    case "datebox":
                        // case "el-date-picker":
                        // case "el-date-picker":
                        searchForm.type = "Daterange";
                        searchForm.width = "260";
                        break;
                    case "el-date-picker":
                        searchForm.type = "Date";
                        break;
                    case "monthrange":
                        searchForm.type = "monthrange";
                        break;
                    case "month":
                        searchForm.type = "month";
                        break;
                    case "year":
                        searchForm.type = n.ControlType;
                        break;
                    default:
                        // searchForm.type = searchForm.type;
                        searchForm.type = n.ControlType;
                        break;
                }
                if (n.DataType == "int" || n.DataType == "bigint")
                {
                    if (searchForm.value != null && StringHelper.IsNumber(searchForm.value.ToString()))
                    {
                        searchForm.value = int.Parse(searchForm.value.ToString());
                    }
                }
                else if (n.DataType == "datetime")
                {
                    if (searchForm.value != null && searchForm.value.GetType() == typeof(DateTime))
                    {
                        searchForm.value = string.Format("{0:yyyy-MM-dd}", searchForm.value);
                    }
                }
                if (n.IsQuery == true)
                {
                    searchFormsAll.Add(searchForm);
                }
                if (n.IsVisible == true)
                    searchFormQuery.Add(searchForm);


            }

            return searchForm;
        }
        int SchedulingDays = 0;
        private int GetSchedulingDays()
        {

            if (SchedulingDays == 0)
            {


                try
                {
                    if (jObject != null && jObject.ContainsKey("SchedulingDays"))
                    {
                        SchedulingDays = int.Parse(jObject["SchedulingDays"].ToString());
                    }
                    else
                    {


                        DataTable dt = SqlHelper.ExecuteDataTable(@"select SchedulingDays from Dev_Organize
where OrganizeID = " + dev_Account!.OrganizeID);
                        if (dt.Rows.Count > 0)
                        {
                            SchedulingDays = (int)dt.Rows[0]["SchedulingDays"];
                        }
                    }
                }
                catch (Exception ex)
                { }
                if (SchedulingDays == 0)
                {
                    SchedulingDays = AppInfo.SchedulingDays ?? 60;
                }
            }
            return SchedulingDays;
        }
        //特定ID的所有可见列查询条件
        List<SearchForm> searchFormQuery= new List<SearchForm>();
        //所有ID的所有可见列查询条件
        List<List<SearchForm>> searchFormQuerys = new List<List<SearchForm>>();

        public string GetConfig()
        {

            List<ElementTableOuput> elementTableOuputs = new List<ElementTableOuput>();

            List<SearchForm> searchFormsAll = new List<SearchForm>();
            List<List<SearchForm>> searchForms = new List<List<SearchForm>>();
            List<List<ElementTableOuput>> datas = new List<List<ElementTableOuput>>();
            string msg = "读取成功";
            bool result = true;
            List<string> SyncDatetime = new List<string>();
            GetConfigForObj(ref elementTableOuputs, ref searchFormsAll, ref searchForms, ref datas, ref msg, ref result, null, ref AppColumns, SyncDatetime);



            return JsonConvert.SerializeObject(new { result, msg, forms = searchForms,formsAll= searchFormQuerys, datas = datas, btns = new string[0], datas2 = lstAllColumnCommon,SyncDatetime });
        }
       static Dictionary<string, DataTable>    lstDataSource = new Dictionary<string, DataTable>();
        public bool isAllOK = true;
        public string allMsg = "";
        protected JArray jArray = null;
        List<int> listErrorRows = new List<int>();
        string ReturnSql = "";
       /// <summary>
       /// 保存初始化一些工作
       /// </summary>
        protected void DataReset()
        {
            if (Dev_DictionaryAll == null)
            {

                Dev_DictionaryAll = _dbContext.DevDictionaries.AsNoTracking().ToList();
                lstOrgs = _dbContext.DevOrganizes.AsNoTracking().ToList();
            }
           
        }
        /// <summary>
        /// 添加后返回的主键集合
        /// </summary>
        public List<string> lstKeyValue = new List<string>();
        /// <summary>
        /// 通用保存版本
        /// </summary>
        /// <returns></returns>
        public string SaveData()
        {

            //最外层：数组，表头与行的关系：childrens,行的外键ID：ForeginKey保持主表，
            //数据格式：[{dicID:5100,OrderID:2000},         {dicID:5200,OrderID:5201,KeyID:'',childrens:[{Scheduling:201,OrderID:5201,KeyID:''}]}]
            // //  this.firstData[0]["childrens"] = [{ dicID: 22, Remark1: '3344', OrganizeID: null }, { dicID: 22, Remark1: '2244', OrganizeID: null }]

            ResetSaveDataState();
            bool result = true;
          
            int index = -1;
            string msg = "";
            jArray = ParseSaveDataJson(BodyJson);

            if (jArray == null)
            {
                msg = "未接收到数据，请确认是否为JSON格式";
                result = false;
            }
            else
            {
                if (jArray.Count > 0)
                {

                    //var a=  V_Dev_Account.GetDev_Account("admin");
                    //  V_Dev_Account.SetDev_Account(a);
                    if (dev_Account != null)
                    {

                        DataReset();

                             SqlConnection connection = new SqlConnection(SqlHelper.MSSQLConnectionString);
                        connection.Open();
                        using (SqlTransaction tran = connection.BeginTransaction())
                        {
                            try
                            {

                                //   dev_DictionariesAll = _dbContext.DevDictionaryFields.AsNoTracking().ToList();

                             
                                isAllOK = true;
                                var savedRows = 0;
                                string DictionaryID = "";
                                //第一步，循环所有的对象，这里可包含多个表
                                foreach (var jObject1 in jArray)
                                {

                                    index++;
                                    JObject jObject = jObject1 as JObject;
                                    if (jObject == null)
                                    {
                                        continue;
                                    }
                                    if (ShouldSkipSaveDataRow(jObject))
                                        continue;

                                    if (!TryGetRowDicId(jObject, out dicID))
                                        continue;

                                    if (dicID == 35)
                                        DictionaryID = jObject["DictionaryID"]?.ToString() ?? "";
                                    //if(jObject.Count>21)
                                    //{
                                    try
                                    {

                                        if (TryGetRowDicId(jObject, out var rowDic) && rowDic == 6704)
                                        {
                                            jObject["Remark2"] = jObject["MaterialName"].ToString();
                                            if (jObject.ContainsKey("ID") && !string.IsNullOrEmpty(jObject["ID"].ToString()))
                                            {

                                            }
                                            else
                                            {
                                                if (jObject.ContainsKey("childrens") == false || ((JArray)jObject["childrens"]).Count == 0 || string.IsNullOrEmpty(((JArray)jObject["childrens"])[0]["Account"].ToString()) || string.IsNullOrEmpty(((JArray)jObject["childrens"])[0]["dicID"].ToString()))
                                                {
                                                    result = false;
                                                    msg = allMsg = "报工必须选中人员";
                                                    isAllOK = false;
                                                    break;
                                                }
                                            }
                                        }
                                        JObject obj = jObject.DeepClone() as JObject;
                                        if (AppInfo.IsSaveLog)
                                        {


                                            systemLog.SaveLog(SystemLog.SystemLogType.SQL更新, obj.ToString(), dev_Account, null);
                                        }
                                      
                                        result = SaveDB(obj, ref msg, tran, "", "");
                                     
                                        if (result == false)
                                        {
                                            allMsg = msg;
                                            isAllOK = false;
                                            break;
                                        }

                                        savedRows++;
                                        if (dicID == 45 && jObject.ContainsKey("DataSourceID"))
                                        {//去掉数据与的缓存
                                            lstDataSource.Remove(jObject["DataSourceID"]!.ToString());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        tran.Rollback();
                                        msg += "第" + (index + 1) + "行发送错误：" + ex.Message;// + ex.StackTrace;
                                        listErrorRows.Add(index);
                                        if (TryGetRowDicId(jObject, out var errDic) && errDic == 5043)
                                        {
                                            jObject["MaterialType"] = ex.Message;
                                        }
                                        result = false;
                                        allMsg = msg;
                                        isAllOK = false;
                                        break;
                                    }
                                    //}
                                    //else
                                    //{ }


                                }



                                if (isAllOK == true)
                                {
                                    if (savedRows == 0)
                                    {
                                        isAllOK = false;
                                        result = false;
                                        msg = allMsg =
                                            "未保存任何数据，请确认提交为 JSON 数组且每行含 dicID（有 dicID 时 RowNumber 可为空）";
                                    }
                                }

                                if (isAllOK == true)
                                {
                                    msg = "保存成功";
                                    allMsg = msg;

                                    DataTable dataTable = SqlHelper.ExecuteDataTable($@"SELECT AfterExecution
FROM Dev_Dictionary
WHERE DictionaryID={dicID}");
                                    if(dataTable.Rows.Count>0)
                                    {
                                        string AfterExecution = dataTable.Rows[0]["AfterExecution"].ToString();
                                        if (!string.IsNullOrEmpty(AfterExecution))
                                        {
                                            
                                            SqlHelper.ExecuteNonQuery(tran,CommandType.Text,AfterExecution);
                                        }
                                    }
                                    tran.Commit();
                                  

                                    if (dicID == 35)
                                    {
                                        List<string> list = new List<string>();
                                       foreach(string key in APSCatchElemet.Keys)
                                        {

                                            if (key.Contains(DictionaryID)){

                                                list.Add(key);
                                              
                                            }
                                     
                                        }
                                       foreach(string key in list)
                                        {
                                            APSCatchElemet.Remove(key);
                                        }
                                       
                                    }
                                  
                                }
                                else
                                {
                                    try { tran.Rollback(); } catch { /* ignore */ }
                                    result = false;
                                    if (!string.IsNullOrEmpty(allMsg))
                                        msg = allMsg;
                                    else if (string.IsNullOrEmpty(msg))
                                        msg = "保存失败";
                                }






                            }
                            catch (Exception ex)
                            {
                                try { tran.Rollback(); } catch { /* ignore */ }
                                msg = "第" + index + "行发送错误：" + ex.Message;// + ex.StackTrace;
                                result = false;
                                allMsg = msg;
                                isAllOK = false;
                            }

                        }
                        connection.Close();
                    }
                    else
                    {
                        msg = "请登录！";
                        result = false;
                    }
                }
                else
                {
                    msg = "未接收到数据！";
                    result = false;
                }


            }
            msg = Translation(msg);
            return JsonConvert.SerializeObject(new { msg, result, lstKeyValue });

        }
        /// <summary>
        /// 对语言进行翻译，要先配置对应的语言表
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string Translation(string msg)
        {
           
            if (dtLanguage != null)
            {
              // foreach (DataRow dr in dt.Rows)
                {
                    DataRow[] dataRows = dtLanguage.Select("zhcn ='" + StringHelper.ReplaceSQL(msg) + "'");
                    if (dataRows.Length > 0)
                    {
                        msg = dataRows[0]["TargetCode"].ToString();
                    }
                    else
                    {
                        foreach (DataRow dr in dtLanguage.Select("zhcn ='" + StringHelper.ReplaceSQL(msg) + "' AND DataSource='后台信息'"))
                        {
                            
                                msg = msg.Replace(dataRows[0]["zhcn"].ToString(),dataRows[0]["TargetCode"].ToString());
                           
                        }
                    }
                }
            }
            return msg;
        }
        //  List<Dev_DictionaryField> dev_DictionariesAll = null;
        List<Dev_Dictionary> Dev_DictionaryAll = null;

        List<Dev_Organize> lstOrgs = new List<Dev_Organize>();
        List<Dev_DictionaryField> dev_DictionaryFields = null;
        List<Dev_DictionaryField> dev_DictionaryFieldsAll = new List<Dev_DictionaryField>();
        List<Dev_DictionaryField> dev_DictionaryFieldsOld = null;
        Dictionary<string, string> dicInsert = new Dictionary<string, string>();
        List<SqlParameter> errorSqlParameters = new List<SqlParameter>();

        protected bool SaveDB(JObject jObject, ref string msg, SqlTransaction tran, string foreginKey, string foreginKeyValue)
        {
            DataReset();


            bool result = true;
            if (jObject.ContainsKey("dicID") && StringHelper.IsNumber(jObject["dicID"].ToString()))
            {
                int dicID = int.Parse(jObject["dicID"].ToString());
                if (dev_DictionaryFieldsAll == null || dev_DictionaryFieldsAll.Where(m => m.DictionaryID == dicID).Count() == 0)
                {//第一次没有读取到数据
                    dev_DictionaryFields = _dbContext.DevDictionaryFields.AsNoTracking().Where(m => m.DictionaryID == dicID).ToList();
                    //加入数据
                    dev_DictionaryFieldsAll.AddRange(dev_DictionaryFields);
                    dev_DictionaryFieldsOld = dev_DictionaryFields;
                }
                else
                {
                    var tmp = dev_DictionaryFieldsAll.Where(m => m.DictionaryID == dicID).ToList();

                    dev_DictionaryFields = tmp;
                    dev_DictionaryFieldsOld = tmp;
                }


                Dev_Dictionary dev_Dictionary = Dev_DictionaryAll.Where(m => m.DictionaryID == dicID).FirstOrDefault();
                if (dev_Dictionary == null)
                {
                    msg = "当前的ID：" + dicID + "未找到记录";
                    isAllOK = false;
                    return false;
                }
                if (dev_Dictionary.ObjectName != dev_Dictionary.TabelName)
                {//对象名和表名不一致，则需要找数据表本身的配置

                    Dev_Dictionary table = Dev_DictionaryAll.Where(m => m.ObjectName == dev_Dictionary.TabelName).FirstOrDefault();
                    if (table == null)
                    {
                        result = false;
                        msg = "当前的ID：" + dicID + "所配置的数据表不存在";

                    }
                    //改变列
                    if (dev_DictionaryFieldsAll.Where(m => m.DictionaryID == table.DictionaryID).Count() == 0)
                    {//实际的表
                        var tmp = _dbContext.DevDictionaryFields.AsNoTracking().Where(m => m.DictionaryID == table.DictionaryID).ToList();
                        dev_DictionaryFieldsAll.AddRange(tmp);
                        //实际的数据表
                        dev_DictionaryFields = tmp;
                    }
                    else
                    {
                        var tmp = dev_DictionaryFieldsAll.Where(m => m.DictionaryID == table.DictionaryID).ToList();

                        dev_DictionaryFields = tmp;
                    }
                }

                //循环所有的列，与json比较

                if (dev_DictionaryFields.Count == 0)
                {
                    result = false;
                    msg = "当前的ID：" + dicID + "找不到相关的列";

                }
                string sqlFirst = " DECLARE @IDENTITYID BIGINT;DECLARE @OLDQTY  decimal(18,6), @NEWQTY decimal(18,6);"; ;
                string sql = "";

                var key = dev_DictionaryFields.Where(m => m.IsKey == true).FirstOrDefault();

                if (key == null)
                {
                    result = false;
                    msg = "当前的ID：" + dicID + "没有主键";

                }
                if (result == false)
                {
                    isAllOK = false;
                    return result;
                }
                string keyValue = "";
                if (jObject.ContainsKey(key.ParameterName))
                {
                    keyValue = jObject[key.ParameterName].ToString();
                }
                List<SqlParameter> sqlParameters = new List<SqlParameter>();

                int index = 0;
                //假设主键有值则更新
                int operationType = 0;
                string insertValue = " VALUES(";
                string where = " WHERE " + key.ParameterName + "=@" + key.ParameterName;

                if (!string.IsNullOrEmpty(keyValue))//不再判断主键不存在则插入
                {
                    operationType = 1;
                    string keyName = "";
                    var keyObj = dev_DictionaryFields.Where(m => m.IsKey == true).FirstOrDefault();
                    if (keyObj != null && keyValue != "-11111")
                    {
                        keyName = keyObj.ParameterName;
                        DataTable dataTable = SqlHelper.ExecuteDataTable("SELECT 1 FROM " + dev_Dictionary.TabelName + "(NOLOCK) WHERE " + keyName + "='" + keyValue + "'");
                        if (dataTable.Rows.Count == 0)
                        {
                            operationType = 0;
                        }
                    }

                }
            
                if (jObject.ContainsKey("ElementDeleteFlag"))
                {

                    if (jObject["ElementDeleteFlag"].ToString().ToLower() == "true" || jObject["ElementDeleteFlag"].ToString().ToLower() == "1")
                    {
                        operationType = 2;

                    }
                }

                if (operationType == 1)
                {
                    sql += "UPDATE " + dev_Dictionary.TabelName + " SET ";
                }
                else if (operationType == 0)
                {
                    sql += "INSERT  " + dev_Dictionary.TabelName + " ( ";
                }
                else
                {

                    sql = "DELETE FROM  " + dev_Dictionary.TabelName + " WHERE  " + key.ParameterName + "='" + keyValue + "' " + dev_Dictionary.DeleteCondition;

                    // sqlParameters.Add(new SqlParameter("@" + key.ParameterName, keyValue));
                }
                //是否存在子行
                bool isAdd = false;
                //插入前端没有给的数据

                if (key != null && jObject.ContainsKey(key.ParameterName) == false)
                {
                    jObject.Add(key.ParameterName, "");
                }

                if (jObject.ContainsKey("ModifyedOn") == false)
                {
                    jObject.Add("ModifyedOn", DateTime.Now);
                }
                else
                {
                    jObject["ModifyedOn"] = DateTime.Now;
                }
                if (jObject.ContainsKey("ModifiedBy") == false)
                {
                    jObject.Add("ModifiedBy", dev_Account.Account);
                }
                else
                {
                    jObject["ModifiedBy"] = dev_Account.Account;
                }
                if (jObject.ContainsKey("ModifiedByName") == false)
                {
                    jObject.Add("ModifiedByName", dev_Account.Name);
                }
                else
                {
                    jObject["ModifiedByName"] = dev_Account.Name;
                }
                if (operationType == 0)
                {
                    if (jObject.ContainsKey("Status") == false)
                    {
                        jObject.Add("Status", "1");
                    }
                }
                if (operationType == 0)
                {
                    if (jObject.ContainsKey("CreatedBy") == false)
                    {
                        jObject.Add("CreatedBy", dev_Account.Account);
                    }
                    else
                    {
                        jObject["CreatedBy"] = dev_Account.Account;
                    }
                    if (jObject.ContainsKey("CreatedOn") == false)
                    {
                        jObject.Add("CreatedOn", DateTime.Now);
                    }
                    else
                    {
                        jObject["CreatedOn"] = DateTime.Now;
                    }
                    if (jObject.ContainsKey("CreatedByName") == false)
                    {
                        jObject.Add("CreatedByName", dev_Account.Name);
                    }
                    else
                    {
                        jObject["CreatedByName"] = dev_Account.Name;
                    }
                    if(jObject.ContainsKey(key.ParameterName) == false)
                    {
                        jObject[key.ParameterName] = "";
                    }
                    if (jObject.ContainsKey("GroupName") == false)
                    {
                        jObject.Add("GroupName", dev_Account.Extend2);
                    }
                    //else
                    //{
                    //    jObject["GroupName"] = dev_Account.Extend2;
                    //}
                }


                if (operationType == 0)
                {//新增默认值
                    foreach (var field in dev_DictionaryFields.Where(m => !string.IsNullOrEmpty(m.DefaultAddValue)))
                    {
                        if (jObject.ContainsKey(field.ParameterName) == false)
                        {
                            jObject.Add(field.ParameterName, StringHelper.GetDefaultValue(dev_Dictionary.TabelName, field.DefaultAddValue, field.ParameterName, false).ToString());
                        }
                    }
                }
                KeyValuePair<string, JToken> childrens = new KeyValuePair<string, JToken>();
                if (dev_DictionaryFieldsOld.Where(m=>m.ParameterName== "GroupName").Count()>0)
                {
                    dev_DictionaryFieldsOld.Where(m => m.ParameterName == "GroupName").FirstOrDefault().IsAdd = true;
                }
                foreach (var j in jObject)
                {
                    if (j.Key.ToString().IndexOf("-") > -1)
                    {//包含-会报错
                        continue;
                    }
                    //查询当前对象的所有数据列
                    if (j.Key.ToLower() == "workshopname".ToLower())
                    {
                        if (lstOrgs.Where(m => m.OrganizeName == j.Value.ToString().Trim()).FirstOrDefault() != null)
                        {
                            if (jObject.ContainsKey("WorkShopID"))
                            {
                                jObject["WorkShopID"] = lstOrgs.Where(m => m.OrganizeName == j.Value.ToString().Trim()).FirstOrDefault().OrganizeID;
                            }
                        }
                    }
                    //当前页面的配置配置
                    var fieldView = dev_DictionaryFieldsOld.Where(m => m.ParameterName.ToLower() == j.Key.ToLower()).FirstOrDefault();
                    bool isFindfield = false;


                

                    foreach (var field in dev_DictionaryFields.Where(m => m.ParameterName.ToLower() == j.Key.ToLower() || (fieldView != null && m.ParameterName == fieldView.SaveParameterName && !string.IsNullOrEmpty(fieldView.SaveParameterName))))
                    {


                        if (fieldView != null && MssqlQueryMethods.ListSystemField.Contains(fieldView.ParameterName))
                        {
                            //系统字段，直接可编辑
                            fieldView.IsEdit = true;

                        }
                        if (fieldView != null)
                        {
                            
                                field.IsAdd = fieldView.IsAdd;
                            
                                field.IsEdit = fieldView.IsEdit;
                          
                          
                            
                        }

                        //是系统的字段
                        if (MssqlQueryMethods.ListSystemField.Contains(field.ParameterName))
                        {
                            field.IsEdit = true;
                        }
                        if (field.IsKey == true || (fieldView != null && jObject.ContainsKey(fieldView.ParameterName) && (fieldView.IsEdit == true || field.IsEdit == true || field.IsAdd == true)) || fieldView == null)
                        {



                            if (field.IsKey == true)
                            {
                                if (string.IsNullOrEmpty(keyValue))
                                {

                                    if (field.IsKey == true && field.IsIdentity == false)
                                    {//主键，并且非自增长
                                        if (string.IsNullOrEmpty(j.Value.ToString()))
                                        {
                                            keyValue = SqlHelper.GetKeyValue(dev_Dictionary.TabelName, field.ParameterName, AppInfo.AppCode);
                                            sqlParameters.Add(new SqlParameter("@" + field.ParameterName, keyValue));
                                        }
                                        else
                                        {
                                            sqlParameters.Add(new SqlParameter("@" + field.ParameterName, j.Value.ToString()));
                                        }
                                        if (operationType == 0)
                                        {
                                            sql += " " + field.ParameterName + ",";
                                            insertValue += " @" + field.ParameterName + ",";
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                                else
                                {
                                    if (operationType == 0 && field.IsIdentity == false)
                                    {
                                        sql += " " + field.ParameterName + ",";
                                        insertValue += " @" + field.ParameterName + ",";

                                    }
                                    sqlParameters.Add(new SqlParameter("@" + field.ParameterName, j.Value.ToString()));
                                }
                            }
                            else
                            {
                                object value = jObject[field.ParameterName].ToString();
                                if (string.IsNullOrEmpty(value.ToString()))
                                {
                                    if (operationType == 0)
                                    {
                                        value = StringHelper.GetDefaultValue(dev_Dictionary.TabelName, field.DefaultAddValue, field.ParameterName, false);
                                    }
                                }
                                else
                                {
                                    if (field.DataType == "datetime" || field.DataType == "date")
                                    {
                                        try
                                        {
                                            if (value.ToString().IndexOf("/OADate(") == 0)
                                            {
                                                value = value.ToString().Replace("/OADate(", "").Replace(")/", "");
                                                value = System.DateTime.FromOADate(int.Parse(value.ToString()));
                                            }
                                            else
                                            {

                                                if (!string.IsNullOrEmpty(value.ToString()))
                                                {
                                                    value = value.ToString().Replace("　", "");
                                                }
                                                if (StringHelper.IsNumber(value.ToString()))
                                                {
                                                    value = DateTime.Parse("1900-01-01").AddDays(int.Parse(value.ToString())).AddDays(-2);
                                                }

                                            }
                                        }
                                        catch (Exception ex)
                                        { }

                                    }
                                    else if (field.DataType == "decimal" || field.DataType == "int")
                                    {
                                        decimal tmpd = 0;
                                        if (value != null && decimal.TryParse(value.ToString(), out tmpd) == false)
                                        {
                                            value = DBNull.Value;
                                        }
                                    }
                                    else
                                    {
                                        value = value.ToString().Trim();
                                    }
                                }
                             
                                if (field.ParameterName.ToLower() == "ModifyedOn".ToLower())
                                {
                                    value = DateTime.Now;
                                }

                                if (field.ParameterName.ToLower() == "ModifiedBy".ToLower())
                                {
                                    value = dev_Account.Account;
                                }
                                if (field.ParameterName.ToLower() == "ModifiedByName".ToLower())
                                {
                                    value = dev_Account.Name;
                                }
                                if (operationType == 1)
                                {
                                    if (field.IsEdit == false)
                                    {
                                        continue;
                                    }
                                    //                 SET [ModifyedOn]=getdate(),[ModifiedBy]='{2}',[ModifiedByName]='{3}'
                                    if (field.ParameterName.ToLower() == "CreatedOn".ToLower())
                                    {
                                        continue;
                                    }
                                    if (field.ParameterName.ToLower() == "CreatedBy".ToLower())
                                    {
                                        continue;
                                    }
                                    if (field.ParameterName.ToLower() == "CreatedByName".ToLower())
                                    {
                                        continue;
                                    }

                                    sql += "  " + field.ParameterName + "=@" + field.ParameterName + ",";

                                }
                                else if (operationType == 0)
                                {//新增
                                    //if (field.ParameterName.ToLower() == "ModifyedOn".ToLower())
                                    //{
                                    //    value = "";
                                    //    continue;
                                    //}
                                    //if (field.ParameterName.ToLower() == "ModifiedBy".ToLower())
                                    //{
                                    //    value = "";
                                    //    continue;
                                    //}
                                    //if (field.ParameterName.ToLower() == "ModifiedByName".ToLower())
                                    //{
                                    //    value = "";
                                    //    continue;
                                    //}
                                    sql += " " + field.ParameterName + ",";
                                    insertValue += " @" + field.ParameterName + ",";
                                    if (field.ParameterName.ToLower() == "CreatedOn".ToLower() || field.ParameterName.ToLower() == "ModifyedOn".ToLower())
                                    {
                                        value = DateTime.Now;
                                    }
                                    else
                                    {
                                        if (field.IsAdd == false)
                                        {
                                            continue;
                                        }
                                    }
                                    if (dev_Account != null)
                                    {


                                        if (field.ParameterName.ToLower() == "CreatedBy".ToLower())
                                        {
                                            value = dev_Account.Account;
                                        }
                                        if (field.ParameterName.ToLower() == "CreatedByName".ToLower())
                                        {
                                            value = dev_Account.Name;
                                        }

                                    }
                                    if (string.IsNullOrEmpty(field.ForeignKey))
                                    {
                                        field.ForeignKey = field.ParameterName;
                                    }
                                    if (!string.IsNullOrEmpty(field.ForeignKey))
                                    {
                                        if (field.ForeignKey.ToLower() == foreginKey.ToLower())
                                        {
                                            value = foreginKeyValue;
                                        }
                                    }

                                }
                                if (sqlParameters.Where(m => m.ParameterName == "@" + field.ParameterName).Count() == 0)
                                {



                                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                                    {
                                        sqlParameters.Add(new SqlParameter("@" + field.ParameterName, DBNull.Value));
                                    }
                                    else
                                    {
                                        sqlParameters.Add(new SqlParameter("@" + field.ParameterName, value));
                                    }
                                }
                            }


                        }
                        else
                        {

                            sqlParameters.Add(new SqlParameter("@" + field.ParameterName, j.Value.ToString()));
                        }
                        isFindfield = true;

                    }
                    if (isFindfield == false && sqlParameters.Where(m => m.ParameterName == "@" + j.Key).Count() == 0)
                    {
                        if (j.Value.ToString() == "")
                        {
                            sqlParameters.Add(new SqlParameter("@" + j.Key, DBNull.Value));
                        }
                        else
                        {
                            string value = j.Value.ToString();
                            if (value.ToString().IndexOf("/OADate(") == 0)
                            {
                                value = value.ToString().Replace("/OADate(", "").Replace(")/", "");
                                value = System.DateTime.FromOADate(double.Parse(value.ToString())).ToString();
                            }
                            sqlParameters.Add(new SqlParameter("@" + j.Key, value));
                        }
                    }

                    if (j.Key.ToLower() == "childrens")
                    {//子行

                        childrens = j;

                        isAdd = true;
                    }
                    else
                    {


                    }
                }
                if (isAdd == false)
                {


                    if (operationType == 1)
                    {//更新
                        sql = sql.Trim(',') + where;
                        // systemLog.SaveLog(SystemLog.SystemLogType.SQL更新, "1"+dev_Dictionary.DictionaryID + dev_Dictionary.AfterUpdate, dev_Account, null);
                        if (!string.IsNullOrEmpty(dev_Dictionary.BeforeUpdate))
                        {
                            sql = dev_Dictionary.BeforeUpdate + " " + sql;
                        }
                        // systemLog.SaveLog(SystemLog.SystemLogType.SQL更新, sql + dev_Dictionary.AfterUpdate,dev_Account,null);
                        if (!string.IsNullOrEmpty(dev_Dictionary.AfterUpdate))
                        {
                            sql += @"
                            " + dev_Dictionary.AfterUpdate;
                        }
                    }
                    else if (operationType == 0)
                    {//添加
                        if (!string.IsNullOrEmpty(dev_Dictionary.BeforeAdd))
                        {
                            sql = dev_Dictionary.BeforeAdd + " " + sql;
                        }
                        sql = sql.Trim(',') + ")" + insertValue.Trim(',') + ");SET @IDENTITYID =SCOPE_IDENTITY();";
                        if (!string.IsNullOrEmpty(dev_Dictionary.AfterAdd))
                        {
                            sql += @"
                            " + dev_Dictionary.AfterAdd;
                        }
                        if (key.IsIdentity == true)
                        {
                            sql += " SELECT   @IDENTITYID";
                        }
                    }
                    else if (operationType == 2)
                    {
                        sql = dev_Dictionary.BeforeDelete + " " + sql + " " + dev_Dictionary.AfterDelete;
                    }
                    errorSqlParameters = sqlParameters;
                    object obj = SqlHelper.ExecuteScalar(tran, CommandType.Text, sqlFirst + sql, sqlParameters.ToArray()); ;
 
               
                    if (obj != null)
                    {
                        keyValue = obj.ToString();
                    }
                    if (jObject.ContainsKey("isNeedKeyVue"))
                    {
                        lstKeyValue.Add(keyValue);
                    }
                    ReturnSql += sqlFirst + sql;
                    if (operationType == 2)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.SQL删除, jObject.ToString(), dev_Account, null);
                    }
                }
                else
                {
                    if (operationType == 1)
                    {
                        if (!string.IsNullOrEmpty(dev_Dictionary.BeforeUpdate))
                        {
                            sql = dev_Dictionary.BeforeUpdate + " " + sql;
                        }
                        sql = sql.Trim(',') + where;
                        if (!string.IsNullOrEmpty(dev_Dictionary.AfterUpdate))
                        {
                            sql += @"
                            " + dev_Dictionary.AfterUpdate;
                        }
                        // systemLog.SaveLog(SystemLog.SystemLogType.SQL更新, sql, dev_Account, null);

                    }
                    else if (operationType == 0)
                    {

                        sql = sql.Trim(',') + ")" + insertValue.Trim(',') + ")";


                    }
                    else if (operationType == 2)
                    {

                        sql = dev_Dictionary.BeforeDelete + " " + sql + " " + dev_Dictionary.AfterDelete;
                    }
                    if (key.IsIdentity == true)
                    {
                        sql += " SET   @IDENTITYID=SCOPE_IDENTITY();";
                    }
                    if (operationType == 0)
                    {
                        if (!string.IsNullOrEmpty(dev_Dictionary.BeforeAdd))
                        {
                            sql = dev_Dictionary.BeforeAdd + " " + sql;
                        }
                        if (!string.IsNullOrEmpty(dev_Dictionary.AfterAdd))
                        {
                            sql += @"
                            " + dev_Dictionary.AfterAdd;
                        }
                        if (key.IsIdentity == true)
                        {
                            sql += " SELECT   @IDENTITYID";
                        }
                    }
                    string tmpValue = "";
                    try
                    {
                        ReturnSql += sqlFirst + sql;
                        object obj = SqlHelper.ExecuteScalar(tran, CommandType.Text, sqlFirst + sql, sqlParameters.ToArray());
                        if (operationType == 2)
                        {
                            systemLog.SaveLog(SystemLog.SystemLogType.SQL删除, jObject.ToString(), dev_Account, null);
                        }
                        if (obj != null)
                        {
                            tmpValue = obj.ToString();
                        }

                    }
                    catch (Exception ex)
                    {
                        result = false;
                        msg = ex.Message;
                        errorSqlParameters = sqlParameters;
                    }
                    if (string.IsNullOrEmpty(keyValue))
                    {
                        keyValue = tmpValue;
                    }
                    if (result)
                    {
                        if (jObject.ContainsKey(key.ParameterName) && jObject[key.ParameterName].ToString() == "")
                        {
                            jObject[key.ParameterName] = keyValue;
                        }
                        if (jObject.ContainsKey("isNeedKeyVue"))
                        {
                            lstKeyValue.Add(keyValue);
                        }

                        foreach (JObject jObject1 in childrens.Value)
                        {
                            // if (jObject1.ContainsKey(key.ParameterName) == true)
                            {
                                jObject1[key.ParameterName] = keyValue;
                            }
                            result = SaveDB(jObject1, ref msg, tran, key.ParameterName, keyValue);
                        }
                    }
                }
            }
            else
            {
                result = false;
                msg = "dicID没有值";
                isAllOK = false;
            }

            return result;
        }

}
