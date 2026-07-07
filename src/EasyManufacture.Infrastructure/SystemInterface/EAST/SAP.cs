
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using EasyManufacture.Entitys;
using NPOI.SS.Formula.Functions;
using SAP.Middleware.Connector;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.UserModel.HeaderFooter;
using static NPOI.HSSF.Util.HSSFColor;

namespace EasyManufacture.Infrastructure.SystemInterface.EAST
{
    public class SAP
    {
        SystemLog systemLog = new SystemLog();
        RfcDestination destination = null;
        RfcRepository repository = null;
        bool isStart = false;
        public SAP()
        {
            destination = RfcDestinationManager.GetDestination("Conn");
            repository = destination.Repository;
        }
        /// <summary>
        /// 是否跑了历史记录
        /// </summary>
        public bool IsRunHistory
        {
            get; set;
        }
        public bool IsRunningHistory
        {
            get; set;
        }
        public void Start()
        {
            if (isStart == false)
            {
                isStart = true;
                try
                {
                    //this.GetAllTmp();
                    //return;
                    if (((DateTime.Now.Hour == 6) || DateTime.Now.Hour == 19) && DateTime.Now.Hour <= 30)
                    {//凌晨做一个批量更新
                        if (IsRunHistory == false)
                        {
                            IsRunningHistory = true;
                            this.GetAllHistory();
                            IsRunningHistory = false;
                            this.IsRunHistory = true;
                        }


                    }
                    else if (IsRunningHistory == false)
                    {
                        this.GetAll();
                        this.IsRunHistory = false;
                    }
                    ///获取MD04
                    // this.GetMD04();
                    // this.GetZWMS_MAINDATA_025();
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "SAP接口错误" + d1 + "," + ex.Message, null, null);
                }
                isStart = false;
            }

        }




        DateTime d1 = DateTime.Now.AddDays(-30);
        DateTime d2 = DateTime.Now;
        int? startDay;

        public void GetAllTmp()
        {


            // function.SetValue("i_num1", 2);
            //  function.SetValue("i_num2", 4);


            IRfcStructure matra = null;

            IRfcTable result = null;


            bool isFirst = true;
            DataTable dt = SqlHelper.ExecuteDataTable(@"SELECT  top 1 CreatedOn FROM Dev_SysLog(nolock)
WHERE Title = '接口访问'
order by LogID desc");
            try
            {
                if (startDay.HasValue == false)
                {
                    startDay = AppInfo.ERPSyncDay;//默认0
                }
            }
            catch (Exception ex)
            {
                startDay = 0;
            }
            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口读取开始", null, null);
            if (dt.Rows.Count > 0 && startDay < 1)
            {//如果配置的大于1 ，说明要重置

                startDay = Convert.ToInt32(DateTime.Now.Subtract(DateTime.Parse(dt.Rows[0]["CreatedOn"].ToString())).TotalDays);
            }
            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"truncate table APS_OrderProcessReport;"
             );
            for (int i = startDay.Value; i >= 0; i--)
            {


                IRfcFunction fun021 = repository.CreateFunction("ZWMS_MAINDATA_021");//工单工序报工信息

                List<string> listOrderNo = new List<string>();
                string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i - 10));
                string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                var rfcs = fun021.GetStructure("I_BUDATS");
                rfcs.SetValue("DATE_FROM", d1);
                rfcs.SetValue("DATE_TO", d2);
                fun021.SetValue("I_BUDATS", rfcs);//指定日期

                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, d1 + "单报工序接口读取开始" + d1, null, null);
                decimal j = 0;
                StringBuilder stringBuilder = new StringBuilder();



                try
                {



                    #region 工单报工信息
                    try
                    {

                        //                      DataTable dtProcessPlan = SqlHelper.ExecuteDataTable(@" SELECT  d.OrderNo as Describe1
                        //FROM  [dbo].[APS_Order] d
                        //where OrderDate>='2021-01-01'  ");
                        // IRfcTable itb = fun021.GetTable("IT_ITEM");
                        //int report = 0;
                        //foreach (DataRow dr in dtProcessPlan.Rows)
                        //{
                        //    if(report % 500==0&&report>0)
                        //    {

                        //物料主数据
                        //  fun021.SetValue("IT_ITEM", itb);//指定日期
                        fun021.Invoke(destination);
                        result = fun021.GetTable("OT_DATA");
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单报工序开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@"
INSERT INTO [dbo].[APS_OrderProcessReport]
           ([ProcessID]
,[ProcessName]
,[DemandQty]
           ,[ProducedQty]
           ,[ConfirmQty]
           ,[BadQty]
           ,[CreatedBy]
           ,[CreatedByName]
           ,[CreatedOn]
           ,[Remark1]
           ,[Remark2]
   
           ,[StartDate]
           ,[EndDate]
,Extend1
,Extend2
     )
SELECT {0}
 ,{1}
,{2}
,{3}
,{4}
,{5}
,{6}
,'SAP'
,GETDATE()
,{7}
,{8}
,{9}
,{10}
,{11}
,{12}
"
    , StringHelper.ReplaceSqlValue(item.GetValue("KTSCH").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("LTXA1").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("SMENG").ToString())//demandqty
    , StringHelper.ReplaceSqlValue(item.GetValue("GMNGA").ToString())//producedqty
    , StringHelper.ReplaceSqlValue(item.GetValue("LMNGA").ToString())//报工2，ConfirmQty
    , StringHelper.ReplaceSqlValue(item.GetValue("XMNGA").ToString())//报废
    , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("RUECK").ToString() + "-" + item.GetValue("RMZHL").ToString())

    , StringHelper.ReplaceSqlValue(item.GetValue("BUDAT").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BUDAT").ToString())

       , StringHelper.ReplaceSqlValue(item.GetValue("STOKZ").ToString())

          , StringHelper.ReplaceSqlValue(item.GetValue("STZHL").ToString())
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

                        // itb = fun021.GetTable("IT_ITEM");
                        //    }
                        //    itb.Insert();
                        //    itb.CurrentRow.SetValue("AUFNR", dr["Describe1"]);
                        //    report++;
                        //}


                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单工序报工读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "工单工序报工错误" + d1 + ex.Message, null, null);
                    }

                    #endregion
                }
                catch (Exception ex)
                {

                }
                stringBuilder = new StringBuilder();










            }

            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口读取结束+" + DateTime.Now, null, null);
        }


        public void GetOrder()
        {
            return;
            if (isStart == false)
            {


                isStart = true;
                // function.SetValue("i_num1", 2);
                //  function.SetValue("i_num2", 4);

                IRfcStructure matra = null;

                IRfcTable result = null;
                IRfcFunction function = repository.CreateFunction("ZWMS_MAINDATA_009");//工单

                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"TRUNCATE TABLE APS_OrderImport;"
                 );

                #region 工单
                try
                {
                    //工单和BOM一起了
                    systemLog = new SystemLog();



                    // function.SetValue("I_AUART", "1000317470");
                    // matra.Append(strumatra);
                    string d1, d2;
                    d1 = d2 = string.Format("yyyyMMdd", DateTime.Now);
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO独立接口读取开始+" + d1 + "", null, null);
                    matra = function.GetStructure("I_ERDAT");

                    matra.SetValue("DATE_FROM", d1);
                    matra.SetValue("DATE_TO", d2);
                    function.SetValue("I_ERDAT", matra);//指定日期
                                                        //function.SetValue("I_DELCLS", "X");//状态为空，获取全部数据
                    function.SetValue("I_VAILD", "X");//不返回删除
                    function.Invoke(destination);
                    //工单和BOM一起了
                    result = function.GetTable("OT_DATA");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO独立接口读取完成+" + d1 + "记录数" + result.Count + "，已插入：" + 0, null, null);
                    StringBuilder stringBuilder = new StringBuilder();
                    int j = 0;
                    foreach (var item in result)
                    {

                        string orderNo = item.GetValue("AUFNR").ToString(); ;

                        stringBuilder.Append(string.Format(@"
INSERT INTO [dbo].[APS_OrderImport]
           (
		   SalesOrderDetailID
,GroupCabinet
,OrderNo
,ActualDay
,ERPID
,WorkOrderTypeID
,ERPEndDate
,ERPStartDate
,CreatedOn
,CreatedBy
,Extend10
,Extend9
,Extend5
,Extend4
,Extend3
 
 
,StockOutQty
,ProductionStatus
,CompletionQty
,Extend17
,DeliveryDate
,Qty
,Extend12
,Extend13
,Extend14
,Extend15
,Extend16
 ,ControlID
,Extend11
,Extend18 --BOMERPID
,OrderDate
,extend19
,extend20
,extend21
,Describe1

,Extend1
,Extend2
,RSPOS
,MSpec
          )
VALUES
(
{0}
,{1}
,{2}
,{3}
,{4}
,{5}
,{6}
,{7}
,{8}
,{9}
,{10}
,{11}
,{12}

,{13}
,{14}
 
 
,{17}
,26
,{17}
,{18}
,{19}
,{20}
,{21}
,{22}
,{23}
,{24}
,{25}
,{26}
,{27}
,{28}
,{8}
,{29}
,{30}
,{31}
,{32}

,{34}
,{35}
,{36}
,{37}
)
", StringHelper.ReplaceSqlValue(item.GetValue("KDAUF").ToString() + "-" + item.GetValue("KDPOS").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("LGORT").ToString())
    , StringHelper.ReplaceSqlValue(orderNo)
    , StringHelper.ReplaceSqlValue(item.GetValue("GSTRP").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUART").ToString())
    , item.GetValue("GLTRP").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GLTRP").ToString())
    , item.GetValue("GSTRP").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GSTRP").ToString())
    , item.GetValue("ERDAT").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ZRESOURCE").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("FEVOR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ISCRTD").ToString())

    , StringHelper.ReplaceSqlValue(item.GetValue("POSNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BDMNG").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())
    , item.GetValue("WEMNG").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("WEMNG").ToString())//CompleteQty已交货
    , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())//料号,extend17
    , item.GetValue("GLTRP").ToString() == string.Empty ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GLTRP").ToString()) //DeliveryDate


    , item.GetValue("PSMNG").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("PSMNG").ToString())//QTY
    , item.GetValue("DWERK").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("DWERK").ToString()) //制造组织ID，



    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())//组件料号
    , StringHelper.ReplaceSqlValue(item.GetValue("BDMNG").ToString())//需求
    , StringHelper.ReplaceSqlValue(item.GetValue("ENMNG").ToString())//已发
    , StringHelper.ReplaceSqlValue(item.GetValue("POSNR").ToString())//组件号,行号
    , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())//控制zhe
    , StringHelper.ReplaceSqlValue(item.GetValue("STATUS").ToString())//状态
    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString() + "-" + item.GetValue("RSNUM").ToString() + "-" + item.GetValue("RSPOS").ToString())
     , StringHelper.ReplaceSqlValue(item.GetValue("XLOEK_P").ToString())//组件删除标记
      , StringHelper.ReplaceSqlValue(item.GetValue("SCHGT").ToString())//散装物料，ex20
        , StringHelper.ReplaceSqlValue(item.GetValue("DUMPS").ToString())//虚拟表示,ex21
        , StringHelper.ReplaceSqlValue(orderNo)
            , StringHelper.ReplaceSqlValue(item.GetValue("VORNR").ToString())
             , StringHelper.ReplaceSqlValue(item.GetValue("XLOEK").ToString())//工单删除标记
                              , StringHelper.ReplaceSqlValue(item.GetValue("RGEKZ").ToString())//反冲
    , StringHelper.ReplaceSqlValue(item.GetValue("RSPOS").ToString())//RSPOS
     , StringHelper.ReplaceSqlValue(item.GetValue("MATXT").ToString())//MSpec--物料规格
    )); ;
                        if (j % 5000 == 0 && j > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        j++;



                    }
                    if (stringBuilder.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                        stringBuilder = new StringBuilder();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO独立接口插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO独立接口读取结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "MO独立接口读取错误" + d1 + "," + ex.Message, null, null);
                }
                #endregion

                try
                {
                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, "exec[dbo].[P_ImportDataDB] '" + string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(0)) + "'");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, d1 + "MO独立接口读取结束", null, null);
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, d1 + "MO独立接口读取结束，但是内部处理错误" + d1 + "," + ex.Message, null, null);
                }
                isStart = false;

            }
        }
        /// <summary>
        /// 获取MO
        /// </summary>
        public void GetAll()
        {


            // function.SetValue("i_num1", 2);
            //  function.SetValue("i_num2", 4);


            IRfcStructure matra = null;

            IRfcTable result = null;


            bool isFirst = true;
            DataTable dt = SqlHelper.ExecuteDataTable(@"SELECT  top 1 CreatedOn FROM Dev_SysLog(nolock)
WHERE Title = '接口访问'
order by LogID desc");
            try
            {
                if (startDay.HasValue == false)
                {
                    startDay = AppInfo.ERPSyncDay;//默认0
                }
            }
            catch (Exception ex)
            {
                startDay = 0;
            }
            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口读取开始", null, null);
            if (dt.Rows.Count > 0 && startDay < 1)
            {//如果配置的大于1 ，说明要重置

                startDay = Convert.ToInt32(DateTime.Now.Subtract(DateTime.Parse(dt.Rows[0]["CreatedOn"].ToString())).TotalDays);
                if (DateTime.Now.Date == DateTime.Parse("2022-10-27") && DateTime.Now.Hour == 15)
                {
                    startDay = 3;
                }
            }
            for (int i = startDay.Value; i >= 0; i--)
            {
                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"TRUNCATE TABLE APS_OrderImport;TRUNCATE TABLE APS_Material_TEMP;" +
               "TRUNCATE TABLE APS_SalesOrderDetailImport; TRUNCATE TABLE [dbo].[APS_WorkOrderTypeImport]; " +
               "TRUNCATE TABLE [dbo].[wms_stockimport];" +
               "TRUNCATE TABLE APS_POImport;TRUNCATE TABLE Dev_SupplierImport" +
               ";TRUNCATE TABLE Dev_CustomerImport;" +
               "TRUNCATE TABLE APS_OrderProcessImport;truncate table APS_OrderProcessReport;truncate table APS_SalesOrderImport;TRUNCATE TABLE Dev_UnitImport"
               );
                IRfcFunction fun003 = repository.CreateFunction("ZWMS_MAINDATA_003");//物料主数据
                IRfcFunction function = repository.CreateFunction("ZWMS_MAINDATA_009");//工单
                IRfcFunction fun011 = repository.CreateFunction("ZWMS_MAINDATA_011");//工单补充
                IRfcFunction fun036 = repository.CreateFunction("ZWMS_MAINDATA_036");//销售单
                IRfcFunction fun020 = repository.CreateFunction("ZWMS_MAINDATA_020");//单据类型
                IRfcFunction fun010 = repository.CreateFunction("ZWMS_MAINDATA_010");//库存信息
                IRfcFunction fun006 = repository.CreateFunction("ZWMS_MAINDATA_006");//采购头和行
                IRfcFunction fun001 = repository.CreateFunction("ZWMS_MAINDATA_001");//供应商
                IRfcFunction fun002 = repository.CreateFunction("ZWMS_MAINDATA_002");//客户信息

                IRfcFunction fun019 = repository.CreateFunction("ZWMS_MAINDATA_019");//工单工序信息
                IRfcFunction fun021 = repository.CreateFunction("ZWMS_MAINDATA_021");//工单工序报工信息
                IRfcFunction funUNITS_GET_FOR_DIMENSION = repository.CreateFunction("UNITS_GET_FOR_DIMENSION");//单位
                List<string> listOrderNo = new List<string>();



                string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));

                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, d1 + "接口读取开始", null, null);
                decimal j = 0;
                StringBuilder stringBuilder = new StringBuilder();


                if (true)
                {





                    #region 销售单

                    try
                    {


                        matra = fun036.GetStructure("I_ERDAT");

                        //string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        //string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "销售单行开始+" + d1, null, null);
                        matra.SetValue("DATE_FROM", d1);
                        matra.SetValue("DATE_TO", d2);

                        fun036.SetValue("I_ERDAT", matra);//指定日期
                        fun036.Invoke(destination);
                        //工单
                        // result = function.GetTable("IT_ITEM");
                        //bom
                        result = fun036.GetTable("OT_DATA");
                        j = 0;
                        foreach (var item in result)
                        {
                            stringBuilder.Append(
                                string.Format(@" 
            
INSERT INTO [dbo].[APS_SalesOrderDetailImport]
           (   Status,
            SalesOrderID,
            SalesOrderDetailID,
            CreatedBy,
            CreatedOn,
            OrderDate,
            Qty,
            ProductionStatus,
            DeliveryDate
,extend1
,extend2
,extend3
,extend4
,extend5
,extend6
,extend7
,extend8
,StockOutQty
,extend9
,extend10
,extend11
,extend15
)
VALUES
(
1
,{0}
,{1}
,{2}
,{3}
,{4}
,{5}
,26
,{6}
,{10}
,{11}
,{12}
,{13}
,{14}
,{15}
,{16}
,{17}
,{18}
,{19}
,{20}
,{21}
,{22}
)


INSERT INTO APS_SalesOrderImport (
            Status,
            SalesOrderID,
            SalesOrderNo,
            CreatedBy,
            CreatedOn,
            OrderDate,
            CustomerID,
            WorkOrderTypeID
            )
            VALUES ( 
            1,
            {0},
            {7},
            {2},
            {3},
            {4},
            {8},
            {9}
            )
   ", StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString())//SalesOrderID
                                 , StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString() + "-" + item.GetValue("POSNR").ToString())//SalesOrderDetailID
                                , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())//CreatedBy
                                   , StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())//日期
                                 , StringHelper.ReplaceSqlValue(item.GetValue("BSTDK").ToString())//采购订单日期
                                  , StringHelper.ReplaceSqlValue(item.GetValue("KWMENG").ToString())//qty
                                    , StringHelper.ReplaceSqlValue(item.GetValue("EDATU").ToString())//计划行日期,DeliveryDate
                                , StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString())
                                 , StringHelper.ReplaceSqlValue(item.GetValue("KUNNR").ToString())
                                  , StringHelper.ReplaceSqlValue(item.GetValue("AUART").ToString())
                                        , StringHelper.ReplaceSqlValue(item.GetValue("UDATE1").ToString())
                                              , StringHelper.ReplaceSqlValue(item.GetValue("UREASON1").ToString())
                                                   , StringHelper.ReplaceSqlValue(item.GetValue("UDATE2").ToString())
                                              , StringHelper.ReplaceSqlValue(item.GetValue("UREASON2").ToString())
                                                   , StringHelper.ReplaceSqlValue(item.GetValue("UDATE3").ToString())
                                              , StringHelper.ReplaceSqlValue(item.GetValue("UREASON3").ToString())
                                                   , StringHelper.ReplaceSqlValue(item.GetValue("UDATE4").ToString())
                                              , StringHelper.ReplaceSqlValue(item.GetValue("UREASON4").ToString())
                                                  , StringHelper.ReplaceSqlValue(item.GetValue("LFIMG").ToString())
                                                      , StringHelper.ReplaceSqlValue(item.GetValue("NAME1").ToString())
                                                          , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())
                                                             , StringHelper.ReplaceSqlValue(item.GetValue("WERKS").ToString())
                                                               , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())

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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "销售单行结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "销售单行错误" + d1 + "," + ex.Message, null, null);
                    }

                    #endregion
                    //   continue;//临时屏蔽




                    //IRfcStructure strumatra = matra.Metadata.LineType.CreateStructure();

                    #region 物料主数据
                    try
                    {
                        j = 0;
                        matra = fun003.GetStructure("I_DATTMS");
                        matra.SetValue("DATEFROM", d1);
                        matra.SetValue("DATETO", d2);
                        // matra.SetValue("DATE_TO", d2);


                        //物料主数据
                        fun003.SetValue("I_DATTMS", matra);//指定日期
                        fun003.Invoke(destination);
                        result = fun003.GetTable("OT_DATA");
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "物料当天读取开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@"
INSERT INTO APS_Material_TEMP (
            Code,
            MaterialName,
            Spec,
            MaterialType,
            Weight,
            CreatedOn,
            IsScheduling,
            Status,
         
     
            Extend1,
            Extend2,
            Extend3,
            Extend4,
            Extend5,
            Extend6,
            Extend7
,extend8
            )
VALUES
(
{0}
,{1}
,{13}
,{2}
,{3}
,GETDATE()
,1
,1
,{4}
,{5}
,{6}
,{7}
,{8}
,{9}
,{10}
,{11}
)
"
        , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MAKTX").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MTART").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("NTGEW").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("NTGEW").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("BRGEW").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("NTGEW").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("GEWEI").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("LAENG").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("BREIT").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("HOEHE").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MEABM").ToString())
          , StringHelper.ReplaceSqlValue(item.GetValue("TXT01").ToString())
               , StringHelper.ReplaceSqlValue(item.GetValue("BSTRF").ToString())

        ));
                            if (j % 5000 == 0 && j > 0)
                            {
                                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                                stringBuilder = new StringBuilder();
                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "物料插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                            }
                            j++;



                        }


                        if (stringBuilder.Length > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                        }
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "物料当天读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, d1 + "物料当天错误" + d1 + "," + ex.Message, null, null);
                    }

                    #endregion




                    List<string> lstPO = new List<string>();
                    #region 采购头和行,根据WMS更新记录

                    j = 0;
                    DataTable dataTable = SqlHelper.ExecuteDataTable(@"select distinct Remark1 from [WMS_ReceiptDetail]
where ModifyedOn>=DATEADD(HOUR,-3,GETDATE())
");

                    //                        DataTable dataTable = SqlHelper.ExecuteDataTable(@"select distinct Remark1 from [WMS_ReceiptDetail]
                    //where ModifyedOn>=getdate()-10
                    //");
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        try
                        {
                            lstPO.Add(dataRow["Remark1"].ToString());
                            //物料主数据
                            fun006.SetValue("I_EBELN", dataRow["Remark1"]);//指定日期
                            fun006.SetValue("I_PSTYP", "*");//所有单据类型
                            fun006.Invoke(destination);
                            result = fun006.GetTable("OT_DATA");
                            var eket = fun006.GetTable("OT_EKET");
                            systemLog = new SystemLog();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "采购头和行读取开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                            int index = 0;
                            foreach (var item in result)
                            {

                                string PRDate = "";//PR
                                string ZMRPD2 = "";//物控回复日期

                                try
                                {
                                    PRDate = eket[index].GetValue("BADAT").ToString();
                                    ZMRPD2 = eket[index].GetValue("ZMRPD2").ToString();
                                }
                                catch (Exception ex)
                                {

                                }
                                index++;
                                stringBuilder.Append(string.Format(@"INSERT INTO APS_POImport (
          
            SupplierCode,
       
            Code,
            PODocNo,
            POQty,
            UnitName,
            ERPID,
            Status,
            ReceivedQty,
            ReturnedQty,
            CreatedOn,
            DemandDate,
            POLineNo
            ,MonetaryUnit
,SupplierName
,ReplyDate
,CreatedBy
,Remark1
,PRDate
,Remark2
,POType
            )
            VALUES ( 
          
            {0},
           
            {1},
            {2},
            {3},
            {4},
            {5},
            1,
            {6},
            {7},
            {8},
            {9},
            {10}
            ,{11}
   ,{12}
   ,{13}
  ,{14}
  ,{15}
,{16}
 ,{17}
 ,{18}
            )
 
"
            , StringHelper.ReplaceSqlValue(item.GetValue("LIFNR").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("EBELN").ToString())

            , StringHelper.ReplaceSqlValue(item.GetValue("MENGE").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("MEINS").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("EBELN").ToString() + "-" + item.GetValue("EBELP").ToString())

            , StringHelper.ReplaceSqlValue(item.GetValue("MENGE0").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("BPMNG_R").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
               , StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
                  , StringHelper.ReplaceSqlValue(item.GetValue("EBELP").ToString())

                , StringHelper.ReplaceSqlValue(item.GetValue("WAERS").ToString())
                 , StringHelper.ReplaceSqlValue(item.GetValue("NAME_ORG1").ToString())
               , StringHelper.ReplaceSqlValue(ZMRPD2)//物控回复
                  , StringHelper.ReplaceSqlValue(item.GetValue("EKGRP").ToString())//采购员账号
                                 , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())//MRP控制者
   , StringHelper.ReplaceSqlValue(PRDate)//
                                    , StringHelper.ReplaceSqlValue(item.GetValue("WERKS").ToString())//工厂
                      , StringHelper.ReplaceSqlValue(item.GetValue("BSART").ToString())//采购单类型
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
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "采购头和行读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        catch (Exception ex)
                        {
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "采购头和行读取开始+" + ex.Message + ",PO" + dataRow["Remark1"].ToString(), null, null);
                        }

                    }



                    #endregion


                    #region 采购头和行
                    try
                    {
                        fun006 = repository.CreateFunction("ZWMS_MAINDATA_006");//采购头和行
                        matra = fun006.GetStructure("I_ERDAT");
                        if (DateTime.Now.Date == DateTime.Parse("2022-12-26") && DateTime.Now.Hour == 10)
                        {
                            matra.SetValue("DATEFROM", String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-1000)));
                            matra.SetValue("DATETO", "20221101");
                        }
                        else
                        {
                            matra.SetValue("DATEFROM", d1);
                            matra.SetValue("DATETO", d2);
                        }


                        //物料主数据
                        fun006.SetValue("I_ERDAT", matra);//指定日期
                        fun006.SetValue("I_PSTYP", "*");//所有单据类型
                        fun006.Invoke(destination);
                        result = fun006.GetTable("OT_DATA");
                        var eket = fun006.GetTable("OT_EKET");
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "采购头和行读取开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                        int index = 0;
                        foreach (var item in result)
                        {
                            if (lstPO.Contains(item.GetValue("EBELN").ToString()) == true)
                            {
                                continue;
                            }
                            string PRDate = "";//PR
                            string ZMRPD2 = "";//物控回复日期


                            try
                            {
                                PRDate = eket[index].GetValue("BADAT").ToString();
                                ZMRPD2 = eket[index].GetValue("ZMRPD2").ToString();
                            }
                            catch (Exception ex)
                            {

                            }
                            index++;
                            stringBuilder.Append(string.Format(@"INSERT INTO APS_POImport (
          
           SupplierCode,
       
            Code,
            PODocNo,
            POQty,
            UnitName,
            ERPID,
            Status,
            ReceivedQty,
            ReturnedQty,
            CreatedOn,
            DemandDate,
            POLineNo
            ,MonetaryUnit
,SupplierName
,ReplyDate
,CreatedBy
,Remark1
,PRDate
,Remark2
,POType

            )
            VALUES ( 
          
            {0},
           
            {1},
            {2},
            {3},
            {4},
            {5},
            1,
            {6},
            {7},
            {8},
            {9},
            {10}
            ,{11}
   ,{12}
   ,{13}
  ,{14}
  ,{15}
,{16}
 ,{17}
 ,{18}
            )
 
"
        , StringHelper.ReplaceSqlValue(item.GetValue("LIFNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("EBELN").ToString())

        , StringHelper.ReplaceSqlValue(item.GetValue("MENGE").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MEINS").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("EBELN").ToString() + "-" + item.GetValue("EBELP").ToString())

        , StringHelper.ReplaceSqlValue(item.GetValue("MENGE0").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("BPMNG_R").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
           , StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
              , StringHelper.ReplaceSqlValue(item.GetValue("EBELP").ToString())

            , StringHelper.ReplaceSqlValue(item.GetValue("WAERS").ToString())
             , StringHelper.ReplaceSqlValue(item.GetValue("NAME_ORG1").ToString())
           , StringHelper.ReplaceSqlValue(ZMRPD2)//物控回复
              , StringHelper.ReplaceSqlValue(item.GetValue("EKGRP").ToString())//采购员账号
                             , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())//MRP控制者
, StringHelper.ReplaceSqlValue(PRDate)//
  , StringHelper.ReplaceSqlValue(item.GetValue("WERKS").ToString())//工厂
      , StringHelper.ReplaceSqlValue(item.GetValue("BSART").ToString())//采购单类型
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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "采购头和行读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "采购头和行读取开始+" + ex.Message, null, null);
                    }

                    #endregion

                    // continue;
                    #region 工单
                    try
                    {
                        //工单和BOM一起了
                        systemLog = new SystemLog();



                        // function.SetValue("I_AUART", "1000317470");
                        // matra.Append(strumatra);
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO当天读取开始+" + d1 + "", null, null);
                        matra = function.GetStructure("I_ERDAT");
                        if (DateTime.Now.Date == DateTime.Parse("2022-11-16") && DateTime.Now.Hour == 17)
                        {
                            d1 = "20221108";
                        }
                        matra.SetValue("DATE_FROM", d1);
                        matra.SetValue("DATE_TO", d2);
                        function.SetValue("I_ERDAT", matra);//指定日期
                                                            //function.SetValue("I_DELCLS", "X");//状态为空，获取全部数据
                                                            // function.SetValue("I_VAILD", "X");//不返回删除
                        function.Invoke(destination);
                        //工单和BOM一起了
                        result = function.GetTable("OT_DATA");
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO当天读取完成+" + d1 + "记录数" + result.Count + "，已插入：" + 0, null, null);
                        j = 0;
                        foreach (var item in result)
                        {

                            string orderNo = item.GetValue("AUFNR").ToString(); ;
                            if (listOrderNo.Contains(orderNo) == false)
                            {
                                listOrderNo.Add(orderNo);
                            }
                            stringBuilder.Append(string.Format(@"
INSERT INTO [dbo].[APS_OrderImport]
           (
		   SalesOrderDetailID
,GroupCabinet
,OrderNo
,ActualDay
,ERPID
,WorkOrderTypeID
,ERPEndDate
,ERPStartDate
,CreatedOn
,CreatedBy
,Extend10
,Extend9
,Extend5
,Extend4
,Extend3
 
 
,StockOutQty
,ProductionStatus
,CompletionQty
,Extend17
,DeliveryDate
,Qty
,Extend12
,Extend13
,Extend14
,Extend15
,Extend16
 ,ControlID
,Extend11
,Extend18 --BOMERPID
,OrderDate
,extend19
,extend20
,extend21
,Describe1

,Extend1
,Extend2
,RSPOS
,MSpec
          )
VALUES
(
{0}
,{1}
,{2}
,{3}
,{4}
,{5}
,{6}
,{7}
,{8}
,{9}
,{10}
,{11}
,{12}

,{13}
,{14}
 
 
,{17}
,26
,{17}
,{18}
,{19}
,{20}
,{21}
,{22}
,{23}
,{24}
,{25}
,{26}
,{27}
,{28}
,{8}
,{29}
,{30}
,{31}
,{32}

,{34}
,{35}
,{36}
,{37}
)
", StringHelper.ReplaceSqlValue(item.GetValue("KDAUF").ToString() + "-" + item.GetValue("KDPOS").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("LGORT").ToString())
        , StringHelper.ReplaceSqlValue(orderNo)
        , StringHelper.ReplaceSqlValue(item.GetValue("GSTRP").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("AUART").ToString())
        , item.GetValue("GLTRP").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GLTRP").ToString())
        , item.GetValue("GSTRP").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GSTRP").ToString())
        , item.GetValue("ERDAT").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("ZRESOURCE").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("FEVOR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("ISCRTD").ToString())

        , StringHelper.ReplaceSqlValue(item.GetValue("POSNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("BDMNG").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())
        , item.GetValue("WEMNG").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("WEMNG").ToString())//CompleteQty已交货
        , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())//料号,extend17
        , item.GetValue("GLTRP").ToString() == string.Empty ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GLTRP").ToString()) //DeliveryDate


        , item.GetValue("PSMNG").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("PSMNG").ToString())//QTY
        , item.GetValue("DWERK").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("DWERK").ToString()) //制造组织ID，



        , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())//组件料号
        , StringHelper.ReplaceSqlValue(item.GetValue("BDMNG").ToString())//需求
        , StringHelper.ReplaceSqlValue(item.GetValue("ENMNG").ToString())//已发
        , StringHelper.ReplaceSqlValue(item.GetValue("POSNR").ToString())//组件号,行号
        , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())//控制zhe
        , StringHelper.ReplaceSqlValue(item.GetValue("STATUS").ToString())//状态
        , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString() + "-" + item.GetValue("RSNUM").ToString() + "-" + item.GetValue("RSPOS").ToString())
         , StringHelper.ReplaceSqlValue(item.GetValue("XLOEK_P").ToString())//组件删除标记
          , StringHelper.ReplaceSqlValue(item.GetValue("SCHGT").ToString())//散装物料，ex20
            , StringHelper.ReplaceSqlValue(item.GetValue("DUMPS").ToString())//虚拟表示,ex21
            , StringHelper.ReplaceSqlValue(orderNo)
                , StringHelper.ReplaceSqlValue(item.GetValue("VORNR").ToString())
                 , StringHelper.ReplaceSqlValue(item.GetValue("XLOEK").ToString())//工单删除标记
                                  , StringHelper.ReplaceSqlValue(item.GetValue("RGEKZ").ToString())//反冲
     , StringHelper.ReplaceSqlValue(item.GetValue("RSPOS").ToString())//RSPOS
         , StringHelper.ReplaceSqlValue(item.GetValue("MATXT").ToString())//MSpec--物料规格
        )); ;
                            if (j % 5000 == 0 && j > 0)
                            {
                                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                                stringBuilder = new StringBuilder();
                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                            }
                            j++;



                        }
                        if (stringBuilder.Length > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO当天读取结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "MO当天读取错误" + d1 + "," + ex.Message, null, null);
                    }
                    #endregion


                    //更新工单，BOM
                    try
                    {


                        // matra = fun011.GetStructure("I_ERDAT");

                        //string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        //string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO补充读取开始+" + d1, null, null);
                        //matra.SetValue("DATE_FROM", d1);
                        //   matra.SetValue("DATE_TO", d2);


                        IRfcTable itb = fun011.GetTable("IT_ITEM");
                        foreach (string orderNo in listOrderNo)
                        {
                            itb.Insert();
                            itb.CurrentRow.SetValue("AUFNR", orderNo);
                        }

                        //物料主数据
                        fun011.SetValue("IT_ITEM", itb);//指定日期
                                                        //  fun011.SetValue("I_ERDAT", matra);//指定日期
                        fun011.Invoke(destination);
                        //工单
                        // result = function.GetTable("IT_ITEM");
                        //bom
                        result = fun011.GetTable("OT_DATA");
                        j = 0;
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@" UPDATE
 [dbo].[APS_OrderImport]
SET  Extend5 = {0},
                     
                      Extend7 = {2},
                      Extend11 = {3}
where orderno={4}



", StringHelper.ReplaceSqlValue(item.GetValue("WEMNG").ToString())
         , StringHelper.ReplaceSqlValue(item.GetValue("GMNGA").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("VERID").ToString())
           , StringHelper.ReplaceSqlValue(item.GetValue("STATUS").ToString())
                                   , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
                            , StringHelper.GenerateStringID()
                         , StringHelper.ReplaceSqlValue(item.GetValue("VORNR").ToString())
                               , StringHelper.ReplaceSqlValue(item.GetValue("ISM02").ToString())
                                 , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())
                                ));
                            if (j % 5000 == 0 && j > 0)
                            {
                                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                                stringBuilder = new StringBuilder();
                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO补充" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                            }
                            j++;
                        }
                        if (stringBuilder.Length > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO补充" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO补充结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "MO补充结束错误" + d1 + "," + ex.Message, null, null);
                    }



                    //读取未完工的工单，用于读取工序产能
                    DataTable dataTable1 = SqlHelper.ExecuteDataTable(@"SELECT OrderNo FROM (
SELECT 
OrderNo,ROW_NUMBER() over(partition by a.materialid order by  a.ERPStartDate desc) as R
FROM APS_Order   a
WHERE   A.CompletionDate IS NULL
         --AND A.ProductionStatus = 26
      
         AND A.MFGOrganizeID IN( 160, 162 )
         AND ( A.Extend11 LIKE '%CRTD%'
                OR A.Extend11 LIKE '%REL%' )
         AND A.WorkOrderTypeID IN( 'ZP01', 'ZP02',  'ZP04' )
         AND A.Extend11 NOT LIKE '%标记%'

		 and a.CompletionQty=0
		 and ERPStartDate<=getdate()+7
		 and ERPStartDate>=GETDATE()-7
	 ) A WHERE R=1
"
                    );

                    foreach (DataRow dataRow1 in dataTable1.Rows)
                    {
                        if (listOrderNo.Contains(dataRow1["OrderNo"].ToString()) == false)
                        {
                            listOrderNo.Add(dataRow1["OrderNo"].ToString());
                        }
                    }

                    #region 工单工序信息
                    try
                    {

                        IRfcTable itb = fun019.GetTable("IT_ITEM");
                        foreach (string orderNo in listOrderNo)
                        {
                            itb.Insert();
                            itb.CurrentRow.SetValue("AUFNR", orderNo);
                        }

                        //物料主数据
                        fun019.SetValue("IT_ITEM", itb);//指定日期
                        fun019.Invoke(destination);
                        result = fun019.GetTable("OT_DATA");
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单工序开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@"
INSERT INTO [dbo].[APS_OrderProcessImport]
           ([ProcessID]
         
  ,ProcessName
      
           ,[CreatedBy]
           ,[CreatedByName]
          
           ,[Status]
           ,[Remark1]
,Remark2
,STEUS
,createdon
,ProducedQty
           )
SELECT {0}
 ,{1}
,'SAP','SAP'
,1
,{2}
,{3}
,{4}
,GETDATE()
,{5}
"
    , StringHelper.ReplaceSqlValue(item.GetValue("KTSCH").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("LTXA1").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())

    , StringHelper.ReplaceSqlValue(item.GetValue("VORNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("STEUS").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("VGW01").ToString())


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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单工序读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "工单工序错误" + d1 + ex.Message, null, null);
                    }

                    #endregion
                }
                try
                {



                    #region 工单报工信息
                    try
                    {
                        // this.GetAllTmp();

                        //                        DataTable dtProcessPlan = SqlHelper.ExecuteDataTable(@" SELECT  d.OrderNo as Describe1
                        //  FROM   APS_ProcessPlan A2
                        //         INNER JOIN APS_Order D
                        //                 ON D.OrderID = A2.OrderID
                        //                    AND D.Status = 1
                        //		  WHERE  d.CompletionDate is null  OR d.CompletionDate>=getdate()-1");
                        //                        IRfcTable itb = fun021.GetTable("IT_ITEM");
                        //                        foreach (DataRow dr in dtProcessPlan.Rows)
                        //                        {
                        //                            itb.Insert();
                        //                            itb.CurrentRow.SetValue("AUFNR", dr["Describe1"]);
                        //                        }

                        //                        //物料主数据
                        //                        fun021.SetValue("IT_ITEM", itb);//指定日期
                        //                        fun021.Invoke(destination);
                        //                        result = fun021.GetTable("OT_DATA");
                        //                        systemLog = new SystemLog();
                        //                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单报工序开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                        //                        foreach (var item in result)
                        //                        {
                        //                            stringBuilder.Append(string.Format(@"
                        //INSERT INTO [dbo].[APS_OrderProcessReport]
                        //           ([ProcessID]
                        //,[ProcessName]
                        //,[DemandQty]
                        //           ,[ProducedQty]
                        //           ,[ConfirmQty]
                        //           ,[BadQty]
                        //           ,[CreatedBy]
                        //           ,[CreatedByName]
                        //           ,[CreatedOn]
                        //           ,[Remark1]
                        //           ,[Remark2]

                        //           ,[StartDate]
                        //           ,[EndDate]
                        //,Extend1
                        //,Extend2
                        //     )
                        //SELECT {0}
                        // ,{1}
                        //,{2}
                        //,{3}
                        //,{4}
                        //,{5}
                        //,{6}
                        //,'SAP'
                        //,GETDATE()
                        //,{7}
                        //,{8}
                        //,{9}
                        //,{10}
                        //,{11}
                        //,{12}
                        //"
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("KTSCH").ToString())
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("LTXA1").ToString())
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("SMENG").ToString())//demandqty
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("GMNGA").ToString())//producedqty
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("LMNGA").ToString())//报工2，ConfirmQty
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("XMNGA").ToString())//报废
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("RUECK").ToString() + "-" + item.GetValue("RMZHL").ToString())

                        //    , StringHelper.ReplaceSqlValue(item.GetValue("BUDAT").ToString())
                        //    , StringHelper.ReplaceSqlValue(item.GetValue("BUDAT").ToString())

                        //       , StringHelper.ReplaceSqlValue(item.GetValue("STOKZ").ToString())

                        //          , StringHelper.ReplaceSqlValue(item.GetValue("STZHL").ToString())
                        //    ));
                        //                            if (j % 5000 == 0 && j > 0)
                        //                            {
                        //                                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                        //                                stringBuilder = new StringBuilder();
                        //                            }
                        //                            j++;



                        //                        }


                        //                        if (stringBuilder.Length > 0)
                        //                        {
                        //                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                        //                            stringBuilder = new StringBuilder();
                        //                        }
                        //                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "工单工序报工读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "工单工序报工错误" + d1 + ex.Message, null, null);
                    }

                    #endregion
                }
                catch (Exception ex)
                {

                }
                stringBuilder = new StringBuilder();



                if (isFirst || true)
                {
                    #region 单据类型

                    try
                    {


                        // matra = fun020.GetStructure("I_AUTYP");

                        //string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        //string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        systemLog = new SystemLog();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "单据类型开始+" + d1, null, null);
                        // matra.SetValue("I_AUTYP", "10");
                        // matra.SetValue("DATE_TO", d2);

                        fun020.SetValue("I_AUTYP", "10");//指定日期
                        fun020.Invoke(destination);
                        //工单
                        // result = function.GetTable("IT_ITEM");
                        //bom
                        result = fun020.GetTable("OT_DATA");
                        j = 0;
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@" 
            
INSERT INTO [dbo].[APS_WorkOrderTypeImport] (
            WorkOrderTypeID,
            Code,
            WorkOrderTypeName,
            Nature
            )
            VALUES ( 
            {0},
            {1},
            {2},
            {3}
            )

   ", StringHelper.ReplaceSqlValue(item.GetValue("AUART").ToString())//SalesOrderID
                                 , StringHelper.ReplaceSqlValue(item.GetValue("NUMKR").ToString())//SalesOrderDetailID
                                , StringHelper.ReplaceSqlValue(item.GetValue("TXT").ToString())//CreatedBy
                                   , StringHelper.ReplaceSqlValue(item.GetValue("AUTYP").ToString())//
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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "单据类型结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "单据类型错误" + d1 + "," + ex.Message, null, null);
                    }

                    #endregion



                    #region 库存信息

                    try
                    {


                        // matra = fun020.GetStructure("I_AUTYP");

                        //string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        //string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                        systemLog = new SystemLog();

                        // matra.SetValue("I_AUTYP", "10");
                        // matra.SetValue("DATE_TO", d2);

                        fun010.SetValue("I_SOBKZ", "");//库存标识:空-工厂;E-销售；O-分包；K-寄售;*-所有 

                        fun010.SetValue("I_WERKS", "1010");//工厂
                        fun010.Invoke(destination);
                        //工单
                        // result = function.GetTable("IT_ITEM");
                        //bom
                        result = fun010.GetTable("OT_DATA");
                        j = 0;
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "库存信息开始+" + d1 + "记录数" + result.Count, null, null);

                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@" 
            
INSERT INTO WMS_StockImport (
            Status,
            Code1,
    
            StockQty,
            InQty,
            OncheckQty,
            WarehouseID
            )
            VALUES ( 
            1,
            {0}, 
            {1},
            {1},
            {2},
            {3}
       
            )
         

   ", StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())//Code1
                                 , StringHelper.ReplaceSqlValue(item.GetValue("CLABS").ToString())//CLABS
                                , StringHelper.ReplaceSqlValue(item.GetValue("CINSM").ToString())//OncheckQty
                                   , StringHelper.ReplaceSqlValue(item.GetValue("LGORT").ToString())//WarehouseID
                                ));

                            if (j % 5000 == 0 && j > 0)
                            {
                                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                                stringBuilder = new StringBuilder();
                                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "库存信息" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                            }
                            j++;
                        }
                        if (stringBuilder.Length > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "库存信息" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "库存信息结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "库存信息错误" + d1 + "," + ex.Message, null, null);
                    }

                    #endregion
                    #region 供应商
                    try
                    {
                        //matra = fun001.GetStructure("I_ERDAT");
                        // matra.SetValue("DATEFROM", d1);
                        // matra.SetValue("DATETO", d2);


                        // fun001.SetValue("I_ERDAT", matra);//指定日期
                        fun001.Invoke(destination);
                        result = fun001.GetTable("OT_DATA");
                        systemLog = new SystemLog();
                        j = 0;
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "供应商读取开始+" + d1 + "记录数" + result.Count, null, null);
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@"

INSERT INTO Dev_SupplierImport (
            SupplierID,
            Tel,
            SupplierName,
            Address,
            CreatedBy,
            ModifyedOn,
            CODE,
            Status
            )
            VALUES ( 
            {0},
            {1},
            {2},
            {3},
            {4},
            {5},
            {6},
            1
            )

 
 
"
        , StringHelper.ReplaceSqlValue(item.GetValue("LIFNR").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("TELF1").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("NAME1").ToString())

        , StringHelper.ReplaceSqlValue(item.GetValue("STRAS").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("UPDAT").ToString())

        , StringHelper.ReplaceSqlValue(item.GetValue("LIFNR").ToString())
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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "供应商读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "供应商读取错误" + d1 + "," + ex.Message, null, null);
                    }

                    #endregion


                    #region 单位信息
                    try
                    {
                        funUNITS_GET_FOR_DIMENSION.Invoke(destination);
                        result = funUNITS_GET_FOR_DIMENSION.GetTable("UNITS_OF_MEASUREMENT");
                        systemLog = new SystemLog();
                        j = 0;
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "单位读取开始+" + d1 + "记录数" + result.Count, null, null);
                        foreach (var item in result)
                        {
                            stringBuilder.Append(string.Format(@"

INSERT INTO Dev_UnitImport (
             UnitCode
,UnitName
            )
            VALUES ( 
            {0},
            {1}
            )

 
 
"
        , StringHelper.ReplaceSqlValue(item.GetValue("UNIT_INT").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("UNIT_TXT").ToString())
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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "供应商读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                    }
                    catch (Exception ex)
                    {
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "供应商读取错误" + d1 + "," + ex.Message, null, null);
                    }

                    #endregion
                    isFirst = false;

                }






                #region 客户信息
                try
                {
                    matra = fun002.GetStructure("I_ERDAT");
                    matra.SetValue("DATEFROM", d1);
                    matra.SetValue("DATETO", d2);


                    //物料主数据
                    fun002.SetValue("I_ERDAT", matra);//指定日期
                    fun002.Invoke(destination);
                    result = fun002.GetTable("OT_DATA");
                    systemLog = new SystemLog();
                    j = 0;
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "客户信息开始+" + d1 + "记录数" + result.Count, null, null);
                    foreach (var item in result)
                    {
                        stringBuilder.Append(string.Format(@"
INSERT INTO Dev_Customer (
            CustomerName,
            Tel,
            CreatedByName,
            Address,
            CreatedBy,
            CreatedOn,
            SystemID,
            CustomerCode,
            Status
            )
            VALUES ( 
            {0},
            {1},
            {2},
            {3},
            {4},
            GETDATE(),
            0,
            {5},
            1
            )
 
"
    , StringHelper.ReplaceSqlValue(item.GetValue("NAME1").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("TELF1").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())

    , StringHelper.ReplaceSqlValue(item.GetValue("STRAS").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("KUNNR").ToString())

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
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "客户信息读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "客户信息错误" + d1 + ex.Message, null, null);
                }

                #endregion


                try
                {
                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, "exec[dbo].[P_ImportDataDB] '" + string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-i)) + "'");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, d1 + "接口读取结束", null, null);
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, d1 + "接口读取结束，但是内部处理错误" + d1 + "," + ex.Message, null, null);
                }


                GC.Collect();




            }

            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口读取结束+" + DateTime.Now, null, null);
        }
        /// <summary>
        /// 获取100天内的，不会触发数据更新的数据
        /// </summary>
        public void GetAllHistory()
        {

            IRfcStructure matra = null;

            IRfcTable result = null;
            // function.SetValue("i_num1", 2);
            //  function.SetValue("i_num2", 4);





            bool isFirst = true;

            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "半夜接口读取开始", null, null);

            int maxDay = 100;
            if (DateTime.Now.Date == DateTime.Parse("2022-07-07"))
            {
                maxDay = 1230;//来一次初始化生产订单
            }
            int i = 0;
            //  for (int i = maxDay; i >= 0; i-=11)
            {
                SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"TRUNCATE TABLE APS_OrderImport;
" +
               "TRUNCATE TABLE APS_SalesOrderDetailImport;  " +
               "" +
               "" +
               "" +
               "TRUNCATE TABLE APS_OrderProcessImport;truncate table APS_SalesOrderImport;TRUNCATE TABLE APS_Material_TEMP;"
               );

                IRfcFunction function = repository.CreateFunction("ZWMS_MAINDATA_009");//工单
                IRfcFunction fun011 = repository.CreateFunction("ZWMS_MAINDATA_011");//工单补充
                IRfcFunction fun036 = repository.CreateFunction("ZWMS_MAINDATA_036");//销售单
                IRfcFunction fun003 = repository.CreateFunction("ZWMS_MAINDATA_003");//物料主数据
                List<string> listOrderNo = new List<string>();
                // string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                // string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-(i-10)));


                string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-100));
                string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(0));
                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, d1 + "接口读取开始", null, null);
                decimal j = 0;
                StringBuilder stringBuilder = new StringBuilder();

                #region 物料主数据
                try
                {
                    matra = fun003.GetStructure("I_DATTMS");
                    matra.SetValue("DATEFROM", d1);
                    matra.SetValue("DATETO", d2);
                    // matra.SetValue("DATE_TO", d2);


                    //物料主数据
                    fun003.SetValue("I_DATTMS", matra);//指定日期
                    fun003.Invoke(destination);
                    result = fun003.GetTable("OT_DATA");
                    systemLog = new SystemLog();
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "物料当天读取开始+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                    foreach (var item in result)
                    {
                        stringBuilder.Append(string.Format(@"
INSERT INTO APS_Material_TEMP (
            Code,
            MaterialName,
            Spec,
            MaterialType,
            Weight,
            CreatedOn,
            IsScheduling,
            Status,
         
     
            Extend1,
            Extend2,
            Extend3,
            Extend4,
            Extend5,
            Extend6,
            Extend7
,extend8
            )
VALUES
(
{0}
,{1}
,{13}
,{2}
,{3}
,GETDATE()
,1
,1
,{4}
,{5}
,{6}
,{7}
,{8}
,{9}
,{10}
,{11}
)
"
    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MAKTX").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MTART").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("NTGEW").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("NTGEW").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BRGEW").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("NTGEW").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("GEWEI").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("LAENG").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BREIT").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("HOEHE").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MEABM").ToString())
      , StringHelper.ReplaceSqlValue(item.GetValue("TXT01").ToString())
           , StringHelper.ReplaceSqlValue(item.GetValue("BSTRF").ToString())

    ));
                        if (j % 5000 == 0 && j > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "物料插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        j++;



                    }


                    if (stringBuilder.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                        stringBuilder = new StringBuilder();
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "物料当天读取结束+" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, d1 + "物料当天错误" + d1 + "," + ex.Message, null, null);
                }

                #endregion

                #region 销售单

                try
                {


                    matra = fun036.GetStructure("I_ERDAT");

                    //string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                    //string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                    systemLog = new SystemLog();
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "销售单行开始+" + d1, null, null);
                    matra.SetValue("DATE_FROM", d1);
                    matra.SetValue("DATE_TO", d2);

                    fun036.SetValue("I_ERDAT", matra);//指定日期
                    fun036.Invoke(destination);
                    //工单
                    // result = function.GetTable("IT_ITEM");
                    //bom
                    result = fun036.GetTable("OT_DATA");
                    j = 0;
                    foreach (var item in result)
                    {
                        stringBuilder.Append(
                            string.Format(@" 
            
INSERT INTO [dbo].[APS_SalesOrderDetailImport]
           (   Status,
            SalesOrderID,
            SalesOrderDetailID,
            CreatedBy,
            CreatedOn,
            OrderDate,
            Qty,
            ProductionStatus,
            DeliveryDate
,extend1
,extend2
,extend3
,extend4
,extend5
,extend6
,extend7
,extend8
,StockOutQty
,extend9
,extend10
,extend11
,extend15
)
VALUES
(
1
,{0}
,{1}
,{2}
,{3}
,{4}
,{5}
,26
,{6}
,{10}
,{11}
,{12}
,{13}
,{14}
,{15}
,{16}
,{17}
,{18}
,{19}
,{20}
,{21}
,{22}
)


INSERT INTO APS_SalesOrderImport (
            Status,
            SalesOrderID,
            SalesOrderNo,
            CreatedBy,
            CreatedOn,
            OrderDate,
            CustomerID,
            WorkOrderTypeID
            )
            VALUES ( 
            1,
            {0},
            {7},
            {2},
            {3},
            {4},
            {8},
            {9}
            )
   ", StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString())//SalesOrderID
                             , StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString() + "-" + item.GetValue("POSNR").ToString())//SalesOrderDetailID
                            , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())//CreatedBy
                               , StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())//日期
                             , StringHelper.ReplaceSqlValue(item.GetValue("BSTDK").ToString())//采购订单日期
                              , StringHelper.ReplaceSqlValue(item.GetValue("KWMENG").ToString())//qty
                                , StringHelper.ReplaceSqlValue(item.GetValue("EDATU").ToString())//计划行日期,DeliveryDate
                            , StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString())
                             , StringHelper.ReplaceSqlValue(item.GetValue("KUNNR").ToString())
                              , StringHelper.ReplaceSqlValue(item.GetValue("AUART").ToString())
                                    , StringHelper.ReplaceSqlValue(item.GetValue("UDATE1").ToString())
                                          , StringHelper.ReplaceSqlValue(item.GetValue("UREASON1").ToString())
                                               , StringHelper.ReplaceSqlValue(item.GetValue("UDATE2").ToString())
                                          , StringHelper.ReplaceSqlValue(item.GetValue("UREASON2").ToString())
                                               , StringHelper.ReplaceSqlValue(item.GetValue("UDATE3").ToString())
                                          , StringHelper.ReplaceSqlValue(item.GetValue("UREASON3").ToString())
                                               , StringHelper.ReplaceSqlValue(item.GetValue("UDATE4").ToString())
                                          , StringHelper.ReplaceSqlValue(item.GetValue("UREASON4").ToString())
                                              , StringHelper.ReplaceSqlValue(item.GetValue("LFIMG").ToString())
                                                  , StringHelper.ReplaceSqlValue(item.GetValue("NAME1").ToString())
                                                      , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())
                                                         , StringHelper.ReplaceSqlValue(item.GetValue("WERKS").ToString())
                                                           , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())

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
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "销售单行插入 +" + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        stringBuilder = new StringBuilder();
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "销售单行结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "销售单行错误" + d1 + "," + ex.Message, null, null);
                }

                #endregion

                // continue;
                #region 工单
                try
                {
                    //工单和BOM一起了
                    systemLog = new SystemLog();



                    // function.SetValue("I_AUART", "1000317470");
                    // matra.Append(strumatra);



                    DataTable dataTable = SqlHelper.ExecuteDataTable(@" 
SELECT 
OrderNo,ROW_NUMBER() over(partition by a.materialid order by  a.ERPStartDate desc) as R
FROM APS_Order   a
WHERE   A.CompletionDate IS NULL
         --AND A.ProductionStatus = 26
      
         AND A.MFGOrganizeID IN( 160, 162 )
         
         AND A.WorkOrderTypeID IN( 'ZP01', 'ZP02','ZP03',  'ZP04' )
         AND A.Extend11 NOT LIKE '%标记%'");
                    IRfcTable itb = function.GetTable("IT_ITEM");
                    foreach (DataRow dataRow in dataTable.Rows)
                    {


                        itb.Insert();
                        itb.CurrentRow.SetValue("AUFNR", dataRow["OrderNo"]);
                    }


                    //物料主数据
                    function.SetValue("IT_ITEM", itb);//指定日期
                                                      //matra = function.GetStructure("I_ERDAT");


                    //function.SetValue("I_ERDAT", matra);//指定日期
                    function.SetValue("I_DELCLS", "");//状态为空，获取全部数据
                    function.Invoke(destination);
                    //工单和BOM一起了
                    result = function.GetTable("OT_DATA");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO当天历史读取开始+" + d1 + "记录数" + result.Count, null, null);
                    j = 0;
                    foreach (var item in result)
                    {

                        string orderNo = item.GetValue("AUFNR").ToString(); ;
                        if (listOrderNo.Contains(orderNo) == false)
                        {
                            listOrderNo.Add(orderNo);
                        }
                        stringBuilder.Append(string.Format(@"
INSERT INTO [dbo].[APS_OrderImport]
           (
		   SalesOrderDetailID
,GroupCabinet
,OrderNo
,ActualDay
,ERPID
,WorkOrderTypeID
,ERPEndDate
,ERPStartDate
,CreatedOn
,CreatedBy
,Extend10
,Extend9
,Extend5
,Extend4
,Extend3
 
 
,StockOutQty
,ProductionStatus
,CompletionQty
,Extend17
,DeliveryDate
,Qty
,Extend12
,Extend13
,Extend14
,Extend15
,Extend16
 ,ControlID
,Extend11
,Extend18 --BOMERPID
,OrderDate
,extend19
,extend20
,extend21
,Describe1

,Extend1
,Extend2
,RSPOS
,MSpec
          )
VALUES
(
{0}
,{1}
,{2}
,{3}
,{4}
,{5}
,{6}
,{7}
,{8}
,{9}
,{10}
,{11}
,{12}

,{13}
,{14}
 
 
,{17}
,26
,{17}
,{18}
,{19}
,{20}
,{21}
,{22}
,{23}
,{24}
,{25}
,{26}
,{27}
,{28}
,{8}
,{29}
,{30}
,{31}
,{32}

,{34}
,{35}
,{36}
,{37}
)
", StringHelper.ReplaceSqlValue(item.GetValue("KDAUF").ToString() + "-" + item.GetValue("KDPOS").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("LGORT").ToString())
    , StringHelper.ReplaceSqlValue(orderNo)
    , StringHelper.ReplaceSqlValue(item.GetValue("GSTRP").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUART").ToString())
    , item.GetValue("GLTRP").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GLTRP").ToString())
    , item.GetValue("GSTRP").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GSTRP").ToString())
    , item.GetValue("ERDAT").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("ERDAT").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ERNAM").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ZRESOURCE").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("FEVOR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("ISCRTD").ToString())

    , StringHelper.ReplaceSqlValue(item.GetValue("POSNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BDMNG").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())
    , item.GetValue("WEMNG").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("WEMNG").ToString())//CompleteQty已交货
    , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())//料号,extend17
    , item.GetValue("GLTRP").ToString() == string.Empty ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("GLTRP").ToString()) //DeliveryDate


    , item.GetValue("PSMNG").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("PSMNG").ToString())//QTY
    , item.GetValue("DWERK").ToString() == "" ? "NULL" : StringHelper.ReplaceSqlValue(item.GetValue("DWERK").ToString()) //制造组织ID，



    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())//组件料号
    , StringHelper.ReplaceSqlValue(item.GetValue("BDMNG").ToString())//需求
    , StringHelper.ReplaceSqlValue(item.GetValue("ENMNG").ToString())//已发
    , StringHelper.ReplaceSqlValue(item.GetValue("POSNR").ToString())//组件号,行号
    , StringHelper.ReplaceSqlValue(item.GetValue("DISPO").ToString())//控制zhe
    , StringHelper.ReplaceSqlValue(item.GetValue("STATUS").ToString())//状态
    , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString() + "-" + item.GetValue("RSNUM").ToString() + "-" + item.GetValue("RSPOS").ToString())
     , StringHelper.ReplaceSqlValue(item.GetValue("XLOEK_P").ToString())//组件删除标记
      , StringHelper.ReplaceSqlValue(item.GetValue("SCHGT").ToString())//散装物料，ex20
        , StringHelper.ReplaceSqlValue(item.GetValue("DUMPS").ToString())//虚拟表示,ex21
        , StringHelper.ReplaceSqlValue(orderNo)
            , StringHelper.ReplaceSqlValue(item.GetValue("VORNR").ToString())//项目号,没用
             , StringHelper.ReplaceSqlValue(item.GetValue("XLOEK").ToString())//工单删除标记
                              , StringHelper.ReplaceSqlValue(item.GetValue("RGEKZ").ToString())//反冲
 , StringHelper.ReplaceSqlValue(item.GetValue("RSPOS").ToString())//RSPOS
     , StringHelper.ReplaceSqlValue(item.GetValue("MATXT").ToString())//MSpec--物料规格
    )); ;
                        if (j % 5000 == 0 && j > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        j++;



                    }
                    if (stringBuilder.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                        stringBuilder = new StringBuilder();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO插入" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO当天历史读取 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);

                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "MO当天历史读取错误" + d1 + "," + ex.Message, null, null);
                }
                #endregion


                //更新工单，BOM
                try
                {


                    // matra = fun011.GetStructure("I_ERDAT");

                    //string d1 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                    //string d2 = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-i));
                    systemLog = new SystemLog();
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO历史补充+" + d1, null, null);
                    //matra.SetValue("DATE_FROM", d1);
                    //   matra.SetValue("DATE_TO", d2);


                    IRfcTable itb = fun011.GetTable("IT_ITEM");
                    foreach (string orderNo in listOrderNo)
                    {
                        itb.Insert();
                        itb.CurrentRow.SetValue("AUFNR", orderNo);
                    }

                    //物料主数据
                    fun011.SetValue("IT_ITEM", itb);//指定日期
                                                    //  fun011.SetValue("I_ERDAT", matra);//指定日期
                    fun011.Invoke(destination);
                    //工单
                    // result = function.GetTable("IT_ITEM");
                    //bom
                    result = fun011.GetTable("OT_DATA");
                    j = 0;
                    foreach (var item in result)
                    {
                        stringBuilder.Append(string.Format(@" UPDATE
 [dbo].[APS_OrderImport]
SET  Extend5 = {0},
                      Extend6 = {1},
                      Extend7 = {2},
                      Extend11 = {3}
where orderno={4}



", StringHelper.ReplaceSqlValue(item.GetValue("WEMNG").ToString())
     , StringHelper.ReplaceSqlValue(item.GetValue("GMNGA").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("VERID").ToString())
       , StringHelper.ReplaceSqlValue(item.GetValue("STATUS").ToString())
                               , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
                        , StringHelper.GenerateStringID()
                     , StringHelper.ReplaceSqlValue(item.GetValue("VORNR").ToString())
                           , StringHelper.ReplaceSqlValue(item.GetValue("ISM02").ToString())
                             , StringHelper.ReplaceSqlValue(item.GetValue("BAUGR").ToString())
                            ));
                        if (j % 5000 == 0 && j > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO历史补充" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                        }
                        j++;
                    }
                    if (stringBuilder.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                        stringBuilder = new StringBuilder();
                        systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO历史补充" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MO历史补充结束 +" + d1 + "记录数" + result.Count + "，已插入：" + j, null, null);
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "MO补充结束错误" + d1 + "," + ex.Message, null, null);
                }










                try
                {
                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, "exec [dbo].[P_ImportDataDB] '" + string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-i)) + "'");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, d1 + "更新正式数据结束", null, null);
                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, d1 + "更新正式数据错误" + d1 + "," + ex.Message, null, null);
                }


                GC.Collect();




            }

            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "接口读取结束+" + DateTime.Now, null, null);
        }


        public bool IsRunMD04 = false;
        public DateTime PreHour = DateTime.Parse("2022-01-01 08:00:00");
        public DateTime md04Datetime = DateTime.Now;
        /// <summary>
        /// MD04接口,
        /// </summary>
        public void GetMD04()
        {
            IRfcStructure matra = null;

            IRfcTable result = null;
            double m = 2 * 60;
            if (DateTime.Now.DayOfWeek == DayOfWeek.Monday || DateTime.Now.DayOfWeek == DayOfWeek.Wednesday || true)
            {
                m = 0.25D * 60;
            }
            systemLog = new SystemLog();
            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口被触发+" + DateTime.Now + ",当前周期：" + Math.Abs((DateTime.Now - md04Datetime).TotalMinutes) + ",判定周期" + m, null, null);

            if (IsRunMD04 == false && (Math.Abs((DateTime.Now - md04Datetime).TotalMinutes) >= m || Math.Abs((DateTime.Now - md04Datetime).TotalMinutes) <= 1) && DateTime.Now.Hour <= 22 && DateTime.Now.Hour >= 8)
            {
                try
                {
                    md04Datetime = DateTime.Now;
                    IsRunMD04 = true;
                    StringBuilder stringBuilder = new StringBuilder();


                    decimal j = 0;
                    systemLog = new SystemLog();
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口开始+" + d1, null, null);

                    IRfcFunction funMD04 = repository.CreateFunction("ZWMS_MAINDATA_022");//MD04

                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"TRUNCATE TABLE  Import_ERP_MD04");
                    DataTable dataTable = SqlHelper.ExecuteDataTable(@"  
 SELECT DISTINCT Code,Extend12 
  FROM  V_APS_Order
  WHERE CompletionQty=0 
  AND ControlID IN('201','210')
  AND [MaterialType]<>'虚拟料号'");
                    IRfcTable itb = funMD04.GetTable("IT_ITEM");
                    foreach (DataRow dataRow in dataTable.Rows)
                    {


                        itb.Insert();
                        itb.CurrentRow.SetValue("MATNR", dataRow["Code"]);
                        itb.CurrentRow.SetValue("WERKS", dataRow["Extend12"]);
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口参数+" + DateTime.Now + "记录数" + itb.Count, null, null);
                    funMD04.SetValue("IT_ITEM", itb);
                    funMD04.Invoke(destination);


                    string msg = funMD04.GetString("O_TEXT");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口读取完成+" + DateTime.Now + "返回结果：" + msg, null, null);
                    result = funMD04.GetTable("OT_DATA");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口读取完成+" + DateTime.Now + "记录数" + result.Count, null, null);

                    foreach (var item in result)
                    {
                        stringBuilder.Append(string.Format(@"

INSERT INTO [dbo].[Import_ERP_MD04]
           ( 
        
           [Code]
            ,REMARK1
           ,[DemandDay]
           ,[DELB0]
           ,[EXTRA]
           ,[DemandQty]
           ,[AvailableQty]
           ,[Exception]
           ,[LGORT]
           ,[UMDAT]
             
           ,[CreatedOn] 
           ,[SyncDatetime]
  ,[RSNUM]
           ,[RSPOS]
           ,[BANFN]
           ,[BNFPO]
           ,[DAT01]
           ,[DAT02]
           ,[EBELN]
           ,[EBELP]
           ,[KDAUF]
           ,[KDPOS]
           ,[VBELN]
           ,[VBELP]
           ,[AUFNR]
           ,[QPLOS]
           ,[AUFVR]
           ,[LIFNR]
           ,[KUNNR]
           ,[BEZEI]
)
 SELECT {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},GETDATE(),GETDATE()
,{10}
,{11}
,{12}
,{13}
,{14}
,{15}
,{16}
,{17}
,{18}
,{19}
,{20}
,{21}
,{22}
,{23}
,{24}
,{25}
,{26}
,{27}
"
    , StringHelper.ReplaceSqlValue(item.GetValue("MATNR").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("WERKS").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("DAT00").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("DELB0").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("EXTRA").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MNG01").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("MNG02").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("AUSKT").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("LGORT").ToString())
       , StringHelper.ReplaceSqlValue(item.GetValue("UMDAT").ToString())
    , StringHelper.ReplaceSqlValue(item.GetValue("RSNUM").ToString())
        , StringHelper.ReplaceSqlValue(item.GetValue("RSPOS").ToString())
            , StringHelper.ReplaceSqlValue(item.GetValue("BANFN").ToString())
                , StringHelper.ReplaceSqlValue(item.GetValue("BNFPO").ToString())
                    , StringHelper.ReplaceSqlValue(item.GetValue("DAT01").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("DAT02").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("EBELN").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("EBELP").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("KDAUF").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("KDPOS").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("VBELN").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("VBELP").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("AUFNR").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("QPLOS").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("AUFVR").ToString()),
                        StringHelper.ReplaceSqlValue(item.GetValue("LIFNR").ToString())
                        , StringHelper.ReplaceSqlValue(item.GetValue("KUNNR").ToString())
                    // , StringHelper.ReplaceSqlValue(item.GetValue("BEDAR").ToString())
                    , StringHelper.ReplaceSqlValue(item.GetValue("BEZEI").ToString())
    ));
                        if (j % 5000 == 0 && j > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04" + d1 + " 进度：" + String.Format("{0:P2}", j / result.Count) + "记录数" + result.Count + "，已插入：" + j, null, null);

                        }
                        j++;



                    }
                    if (stringBuilder.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口从ERP保存完成+" + d1, null, null);
                    SqlHelper.ExecuteNonQuery(@"
 truncate table [ERP_MD04]
INSERT INTO [dbo].[ERP_MD04]
           ([MFOrganizeID]
           ,[OrganizeID]
           ,[OrganizeName]
           ,[GroupID]
           ,[GroupName]
           ,[MaterialID]
           ,[Code]
           ,[DemandDay]
           ,[DELB0]
           ,[EXTRA]
           ,[DemandQty]
           ,[AvailableQty]
           ,[Exception]
           ,[LGORT]
           ,[UMDAT]
           ,[Status]
           ,[CreatedBy]
           ,[CreatedByName]
           ,[ModifiedBy]
           ,[ModifiedByName]
           ,[CreatedOn]
           ,[ModifyedOn]
           ,[Remark1]
           ,[Remark2]
           ,[ERPID]
           ,[SyncDatetime]
           ,[DataSource]
,DocNo
  ,[RSNUM]
           ,[RSPOS]
           ,[BANFN]
           ,[BNFPO]
           ,[DAT01]
           ,[DAT02]
           ,[EBELN]
           ,[EBELP]
           ,[KDAUF]
           ,[KDPOS]
           ,[VBELN]
           ,[VBELP]
           ,[AUFNR]
           ,[QPLOS]
           ,[AUFVR]
           ,[LIFNR]
           ,[KUNNR]
           ,[BEDAR]
           ,[BEZEI]
)
  SELECT 
       C.OrganizeID
      ,C.[OrganizeID]
      ,C.[OrganizeName]
      ,A.[GroupID]
      ,A.[GroupName]
      ,B.[MaterialID]
      ,A.[Code]
      ,A.[DemandDay]
      ,A.[DELB0]
      ,A.[EXTRA]
      ,A.[DemandQty]
      ,A.[AvailableQty]
      ,A.[Exception]
      ,A.[LGORT]
      ,A.[UMDAT]
      ,1
      ,A.[CreatedBy]
      ,A.[CreatedByName]
      ,A.[ModifiedBy]
      ,A.[ModifiedByName]
      ,A.[CreatedOn]
      ,A.[ModifyedOn]
      ,A.[Remark1]
      ,A.[Remark2]
      ,A.[ERPID]
      ,A.[SyncDatetime]
      ,'SAP'
 ,SUBSTRING(A.EXTRA,0,CHARINDEX('/',A.EXTRA))
  ,[RSNUM]
           ,[RSPOS]
           ,[BANFN]
           ,[BNFPO]
           ,[DAT01]
           ,[DAT02]
           ,[EBELN]
           ,[EBELP]
           ,[KDAUF]
           ,[KDPOS]
           ,[VBELN]
           ,[VBELP]
           ,[AUFNR]
           ,[QPLOS]
           ,[AUFVR]
           ,[LIFNR]
           ,[KUNNR]
           ,[BEDAR]
           ,[BEZEI]
  FROM  [dbo].Import_ERP_MD04 A
  INNER JOIN APS_Material B ON A.CODE=B.Code
  inner join Dev_Organize c on a.remark1=c.HRCode and OrganizeTypeID IN(0,2)
order by a.code,a.id
 
 
                ");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "MD04接口内部保存完成+" + d1, null, null);




                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "MD04接口访问错误+" + ex.Message, null, null);
                }
                finally
                {
                    IsRunMD04 = false;
                }
            }
        }
        bool isRun025 = false;
        string[] fields = new string[] {
           "MANDT"
           ,"CPUDT"
           ,"CPUTM"
           ,"WERKS"
           ,"DISPO"
           ,"SEQNO"
           ,"BAUGR"
           ,"MATNR"
           ,"EBELN"
           ,"ENMNG"
           ,"STOCK"
           ,"TRNRES"
           ,"ZCHECK"
           ,"PRDORD"
           ,"PLDORD"
           ,"PURSUR2"
           ,"PURSUR1"
           ,"BACK"
           ,"ZURGE"
           ,"RSNUM"
           ,"BDMNG"
           ,"AUFNR"
           ,"RSPOS"
           ,"KDAUF"
           ,"KDPOS"
           ,"ERFMG"
           ,"BDTER"
           ,"BDTER2"
           ,"MATKL"
           ,"MEINS"
           ,"PLIFZ"
           ,"BESKZ"
           ,"LGPRO"
           ,"FEVOR"
           ,"RGEKZ"
           ,"MAKTX"
           ,"MAKTX2"
           ,"INPUT_WEEK"
           ,"ZQLZT"
           ,"ZWFP"
           ,"ZQLS"
           ,"ZZYKYL"
           ,"ZZYKSL"
           ,"ZBHZYL"
           ,"ZZYJZL"
           ,"ZKCZYL"
           ,"ZKCDQL"
           ,"ZZYDQL"
           ,"DELET"
           ,"ELE_MRP"
           ,"MTART2"
           ,"ZXCD"
           ,"AUART"
           ,"OBJNR"
           ,"DSNAM"
           ,"MTART"
           ,"STLNR"
           ,"BMEIN"
           ,"DJNO"
           ,"DELNR"
           ,"DEL12"
           ,"DELPS"
           ,"LIFNR"
           ,"NAME_ORG1"
           ,"AEDAT"
           ,"EKGRP"
           ,"EINDT"
           ,"EBELP"
           ,"BANFN"
           ,"BNFPO"
           ,"BADAT"
           ,"LFDAT"
           ,"FRGDT"
           ,"TXT"
           ,"TXT2"
           ,"STAT"
           ,"TXT30"
           ,"ZCMS"
           ,"ZCMLS"
           ,"ZXSOSC"
           ,"WEMNG"
           ,"ZWFSL"
           ,"PSMNG"
           ,"ZAVRG"
           ,"KWMENG"
           ,"ZWQSL"
           ,"PEINH"
           ,"NETPR"
           ,"ZREBACK"
           ,"EKNAM"
           ,"LGOBE"
           ,"ZMRPD"
           ,"BWART"
           ,"UMLGO"
           ,"BSART"
           ,"SOBKZ"
           ,"KDAUF2"
           ,"KDPOS2"
           ,"VBELN"
           ,"POSNR"
           ,"PLNUM"
           ,"IHREZ"
           ,"BISMT"
           ,"ZZLONG"
           ,"ZZPAIC"
           ,"ZYDWD"
           ,"EBELN_OUT"
           ,"USNAM"
           ,"ZZTQQ"
           ,"BANFN2"
           ,"BNFPO2"
           ,"ZTXT1"
           ,"ZTXT2"
             ,"BEDNR"
             ,"VIP"
             ,"ZZRFMK"
          };
        /// <summary>
        /// 工序平衡表
        /// </summary>
        public void GetERP_ZPPT036()
        {


            if (((DateTime.Now.Hour == 7 || DateTime.Now.Hour == 12 || DateTime.Now.Hour == 16)))
            {
                systemLog = new SystemLog();
                systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "供需平衡表同步时段开始，PreHour：" + PreHour, null, null);
            }


            if (isRun025 == false && ((DateTime.Now.Hour == 7 || DateTime.Now.Hour == 12 || DateTime.Now.Hour == 16 || DateTime.Now.Hour == 20) && DateTime.Now.Minute < 20))
            {
                StringBuilder stringBuilder = new StringBuilder();
                try
                {

                    isRun025 = true;



                    decimal j = 0;
                    systemLog = new SystemLog();
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "供需平衡表接口开始+" + DateTime.Now, null, null);

                    DataTable dataTable = SqlHelper.ExecuteDataTable(@"SELECT  Account,WorkFlowInstanceID,Extend2,Name FROM Dev_Account
WHERE WorkFlowInstanceID<>'' and isnull(Extend3, '') not like '%经理%'
and Extend3<>'' and Extend2<>''");
                    V_Dev_Account dev_Account = new V_Dev_Account();
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        dev_Account.Account = dataRow["Account"].ToString();
                        dev_Account.Name = dataRow["Name"].ToString();
                        dev_Account.WorkFlowInstanceID = dataRow["WorkFlowInstanceID"].ToString();
                        dev_Account.Extend2 = dataRow["Extend2"].ToString();
                        foreach (string s in dev_Account.WorkFlowInstanceID.Split(','))
                        {
                            if (!string.IsNullOrEmpty(s))
                            {
                                GetZAPSF001(dev_Account.Extend2, s, dev_Account);

                            }

                        }
                    }
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "供需平衡表接口保存完成+" + d1, null, null);




                }
                catch (Exception ex)
                {
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问错误, "供需平衡表接口访问错误+" + ex.Message, null, null);
                }
                finally
                {
                    isRun025 = false;
                }
            }
        }
        /// <summary>
        /// 同步SAP数据
        /// </summary>
        /// <param name="orderNo">单号</param>
        /// <param name="lineName">线名称</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="r2">错误信息</param>
        /// <param name="toREL">转为REL</param>
        /// <returns></returns>
        public bool UpdateOrder(string orderNo, string lineName, string startDate, string endDate, ref string r2, string toREL, string ControlID)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(orderNo))
            {


                IRfcFunction fun002 = repository.CreateFunction("ZWMS_MAINTAIN_002");//同步接口

                // matra.SetValue("DATE_TO", d2);


                //物料主数据
                fun002.SetValue("I_AUFNR", orderNo);//单号
                fun002.SetValue("I_FEVOR", lineName);//生产管理者
                fun002.SetValue("I_DISPO", ControlID);//MRP控制者
                if (!string.IsNullOrEmpty(endDate))
                {
                    fun002.SetValue("I_GLTRP", string.Format("{0:yyyy-MM-dd}", DateTime.Parse(endDate)));//完成日期
                }
                if (!string.IsNullOrEmpty(startDate))
                {
                    fun002.SetValue("I_GSTRP", string.Format("{0:yyyy-MM-dd}", DateTime.Parse(startDate)));//开始日期
                }

                if (!string.IsNullOrEmpty(toREL))
                {
                    fun002.SetValue("I_RELE", "X");//改变状态
                }
                fun002.Invoke(destination);
                string r1 = fun002.GetString("O_FLAG");
                result = r1 == "S";
                r2 = fun002.GetString("O_TEXT");
                string r3 = fun002.GetString("O_FEVOR");
                string r4 = fun002.GetString("O_GLTRP");
                if (result)
                {
                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, CommandType.Text, @"UPDATE   APS_Order
SET Extend8 = '已同步',ControlID='" + ControlID + @"', extend9='" + lineName + @"'
where OrderNo = '" + orderNo + "' ");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "SAP数据同步" + orderNo, null, null);
                }
                else
                {

                }
            }
            return result;

        }
        /// <summary>
        /// 同步SAP数据
        /// </summary>
        /// <param name="orderNo"></param>
        /// <param name="lineName"></param>
        /// <param name="endDate"></param>
        public bool UpdateOrderStartDate(string orderNo, string startDate, string endDate, string lineName, ref string r2, string ControlID)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(orderNo))
            {


                IRfcFunction fun002 = repository.CreateFunction("ZWMS_MAINTAIN_002");//同步接口

                // matra.SetValue("DATE_TO", d2);


                //物料主数据
                fun002.SetValue("I_AUFNR", orderNo);//单号
                fun002.SetValue("I_FEVOR", lineName);//生产管理者

                fun002.SetValue("I_DISPO", ControlID);//MRP控制者
                if (!string.IsNullOrEmpty(endDate))
                {
                    fun002.SetValue("I_GLTRP", string.Format("{0:yyyy-MM-dd}", DateTime.Parse(endDate)));//完成日期
                }
                if (!string.IsNullOrEmpty(startDate))
                {
                    fun002.SetValue("I_GSTRP", string.Format("{0:yyyy-MM-dd}", DateTime.Parse(startDate)));//开始日期
                }


                fun002.Invoke(destination);
                string r1 = fun002.GetString("O_FLAG");
                result = r1 == "S";
                r2 = fun002.GetString("O_TEXT");
                string r3 = fun002.GetString("O_FEVOR");
                string r4 = fun002.GetString("O_GLTRP");
                if (result)
                {

                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, CommandType.Text, @"UPDATE   APS_Order
SET Extend8 = '已同步',ControlID='" + ControlID + @"', extend9='" + lineName + @"',ERPEndDate=" + (string.IsNullOrEmpty(endDate) ? "ERPEndDate" : "'" + endDate + "'") + ",ERPStartDate=" + (string.IsNullOrEmpty(startDate) ? "ERPStartDate" : "'" + startDate + "'") + @"
where OrderNo = '" + orderNo + "' ");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口推送, "SAP数据同步" + orderNo, null, null);
                }
            }
            return result;

        }
        /// <summary>
        /// 获取实时的供需平衡表数据
        /// </summary>
        /// <returns></returns>
        public JsonInterFace GetZAPSF001(string I_WERKS, string I_DISPO, V_Dev_Account dev_Account)
        {

            JsonInterFace jsonInterFace = new JsonInterFace();


            DataTable dataTable = SqlHelper.ExecuteDataTable(@"select top 1 *,DATEDIFF(MINUTE,CreatedOn,GETDATE()) AS T from Dev_SysLog(NOLOCK)
where Title='获取ERP数据' and CreatedBy='" + dev_Account.Account + @"'
order by LogID desc");
            if (dataTable.Rows.Count > 0)
            {
                if (dataTable.Rows[0]["Content"].ToString() != "手工供需平衡表结束" && long.Parse(dataTable.Rows[0]["T"].ToString()) < 30)
                {
                    jsonInterFace.message = "上一次供需平衡表在读取中";
                    return jsonInterFace; ;
                }
            }
            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "手工供需平衡表开始" + DateTime.Now, dev_Account, null);
            try
            {
                IRfcFunction funZAPSF001 = repository.CreateFunction("ZAPSF001");//供需平衡表
                funZAPSF001.SetValue("I_WERKS", I_WERKS);//工厂
                funZAPSF001.SetValue("I_DISPO", I_DISPO);//MRP控制者
                funZAPSF001.Invoke(destination);

                string E_TYP = funZAPSF001.GetString("E_TYP");
                string E_MSG = funZAPSF001.GetString("E_MSG");

                if (E_TYP.ToUpper() == "S")
                {
                    jsonInterFace.code = "200";
                    jsonInterFace.message = E_MSG;
                    var result025 = funZAPSF001.GetTable("OT_DATA");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "手工供需平衡表读取完成+" + DateTime.Now + "记录数" + result025.Count, null, null);
                    if (result025.Count > 0)
                    {
                        PreHour = DateTime.Now;
                    }
                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"DELETE FROM   [Import_ERP_ZPPT036]  WHERE DISPO='" + I_DISPO + @"' AND WERKS='" + I_WERKS + @"';");

                    StringBuilder stringBuilder = new StringBuilder();
                    int j = 0;
                    foreach (var item in result025)
                    {


                        stringBuilder.Append(@"

INSERT INTO [dbo].[Import_ERP_ZPPT036]
           (
CreatedOn
");
                        foreach (string s in fields)
                        {
                            stringBuilder.Append(string.Format(",{0}", s));

                        }
                        stringBuilder.Append(@")
 SELECT GETDATE()
");

                        foreach (string s in fields)
                        {
                            stringBuilder.Append(string.Format(",{0}", StringHelper.ReplaceSqlValue(item.GetValue(s).ToString())));

                        }


                        if (j % 5000 == 0 && j > 0)
                        {
                            SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                            stringBuilder = new StringBuilder();
                            systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "手工供需平衡表接口" + DateTime.Now + " 进度：" + String.Format("{0:P2}", j / result025.Count) + "记录数" + result025.Count + "，已插入：" + j, null, null);

                        }
                        j++;



                    }
                    if (stringBuilder.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, stringBuilder.ToString());
                    }

                    SqlHelper.ExecuteNonQuery(SqlHelper.MSSQLConnectionString, System.Data.CommandType.Text, @"

DELETE FROM  ERP_ZPPT036 WHERE DISPO='" + I_DISPO + @"' AND WERKS='" + I_WERKS + @"';
INSERT INTO ERP_ZPPT036
 
 SELECT *
FROM   Import_ERP_ZPPT036 A
 WHERE DISPO='" + I_DISPO + @"' AND WERKS='" + I_WERKS + @"'
UPDATE ERP_ZPPT036
SET SyncDatetime=GETDATE()  WHERE DISPO='" + I_DISPO + @"' AND WERKS='" + I_WERKS + @"'



UPDATE A SET  A.OweQty=CASE WHEN A.IssuedQty>=A.DemandQty OR A.Status=0 THEN 0 ELSE ISNULL(0-ZQLS,0) END
 
,A.EINDT=case when format(C.EINDT,'MM-dd')='12-30' or  format(C.EINDT,'MM-dd')='12-31'  then null else C.EINDT end
FROM APS_OrderBOM A(NOLOCK)
INNER JOIN APS_Order B (NOLOCK) ON A.OrderID=B.OrderID
LEFT JOIN 
(
SELECT AUFNR,RSPOS,SUM(ZQLS) AS ZQLS,MAX(EINDT) AS EINDT
FROM ERP_ZPPT036(NOLOCK)
GROUP BY AUFNR,RSPOS
)
C ON B.OrderNo=C.AUFNR AND A.RSPOS=C.RSPOS
WHERE B.CompletionDate IS NULL

UPDATE A SET  DELPS= RIGHT('0000000' + CONVERT(VARCHAR, DELPS), 5) 
 FROM   ERP_ZPPT036 A
                           WHERE    ELE_MRP IN( '采购订单未交量', '已确认采购申请' )

 ");
                    systemLog.SaveLog(SystemLog.SystemLogType.接口访问, "手工供需平衡表读取完成+" + DateTime.Now + "执行完毕", null, null);
                }
                else
                {
                    jsonInterFace.code = "400";
                    jsonInterFace.message = E_MSG;
                }
            }
            catch (Exception ex)
            {
                jsonInterFace.code = "400";

                jsonInterFace.message = ex.Message;
            }
            systemLog.SaveLog(SystemLog.SystemLogType.获取ERP数据, "手工供需平衡表结束", dev_Account, null);
            return jsonInterFace;
        }
    }
}

