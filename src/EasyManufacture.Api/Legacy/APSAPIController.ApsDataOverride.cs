// APSAPIController.override APSData() dic switch
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EasyManufacture.Api.Controllers;

public partial class APSAPIController
{
    protected override void ApplyApsDataExportHooks() => ApplyLegacyApsDataDicHooks();

    void ApplyLegacyApsDataDicHooks()
    {
        try
        {
            jObject = JsonConvert.DeserializeObject(BodyJson) as JObject;
            if (jObject != null)
            {
                foreach (var key in new[] { "dicID", "DicID", "DICID", "DictionaryID", "ID" })
                {
                    if (!jObject.ContainsKey(key))
                        continue;
                    if (int.TryParse(jObject[key]?.ToString(), out var parsed) && parsed > 0)
                    {
                        dicID = parsed;
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        switch (dicID)
            {
                case 10075:
                case 12191:
                    {

                        setRowDetail = setDetail10075;
                        setDt = SetDt10075;
                        break;
                    }
                case 10093:
                    {

                        setRowDetail = setDetail10093;
                        setDt = SetDt10093;
                        break;
                    }

                case 10115:
                    {
                        // setRowDetail = setDetail10115;
                        // setDt = SetDt10115;
                        break;
                    }
                case 10114:
                    {
                        // setRowDetail = setDetail10114;
                        // setDt = SetDt10114;
                        break;
                    }
                case 10123:
                    {

                        setRowDetail = setDetail10123;
                        setDt = SetDt10123;
                        break;
                    }
                case 10130:
                    {

                        setRowDetail = setDetail10130;
                        setDt = SetDt10130;
                        break;
                    }
                case 7847:
                    {
                        setRowDetail = setDetail7847;
                        setDt = SetDt7847;
                        break;
                    }
                case 7908:
                    {
                        setRowDetail = setDetail7908;
                        setDt = SetDt7908;
                        break;
                    }
                case 7915:
                    {
                        setRowDetail = setDetail7915;
                        setDt = SetDt7915;
                        break;
                    }
                case 7910:
                    {
                        try
                        {
                            setRowDetail = setDetail7910;
                            setDt = SetDt7910;
                        }
                        catch (Exception ex)
                        {

                        }
                        break;
                    }
                case 11182:
                case 17250:
                    //case 17251:
                    {
                        setRowDetail = setDetail11182;
                        setDt = SetDt11182;

                        break;
                    }
                case 28668:
                    // 预排齐套欠料汇总：日期列透视（数据源 APS_OrderForecastForm）
                    {
                        setRowDetail = setDetail28668;
                        setDt = SetDt28668;
                        break;
                    }
                case 28686:
                    // 大叶：计划一致性月报（取数 7864→日一致性透视）
                    {
                        setRowDetail = setDetail28686;
                        setDt = SetDt28686;
                        break;
                    }
                case 15214:
                    {//正式计划
                        setRowDetail += setDetai15214;
                        setDt = SetDt15214;
                        // setWhere = SetWhere15214;
                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }

                #region 天宝
                case 11150:
                    {///生产排程管理，订单变更日期

                        setRowDetail = setDetail1150;
                        setDt = SetDt11150;
                        break;
                    }
                case 11149:
                    {///生产排程管理，订单变更日期

                        setRowDetail = setDetail1149;
                        setDt = SetDt11150;
                        break;
                    }
                case 14197:
                    {///生产排程管理，订单变更日期

                        setRowDetail += setDetail14197;
                        setDt = SetDt14197;
                        setWhere += SetWhere14197;
                        break;
                    }
                case 20346:
                    {///生产排程管理，订单变更日期

                        setRowDetail += setDetail20346;
                        setDt = SetDt20346;
                        setWhere += SetWhere20346;
                        break;
                    }
                case 7961:
                    {
                        // setRowDetail = setDetail7961;
                        // setDt = SetDt7961;
                        break;
                    }
                case 14200:
                    {//各线齐套率
                        setRowDetail += setDetail14200;
                        setDt = SetDt14200;

                        setAfterReadRow += setAfterReadRow14200;
                        break;
                    }
                case 14201:
                    {//各线齐套率
                        setRowDetail += setDetail14201;
                        setDt = SetDt14201;
                        setAfterReadRow += setAfterReadRow14201;
                        break;
                    }
                case 14199:
                    {//各线齐套率
                        setRowDetail += setDetail14199;
                        setDt = SetDt14199;

                        setAfterReadRow += setAfterReadRow14199;
                        break;
                    }
                case 15217:
                    {//各线齐套率
                        setRowDetail += setDetail15217;
                        setDt = SetDt15217;

                        break;
                    }
                case 17257:
                    {
                        setRowDetail += setDetail17257;
                        setDt = SetDt17257;
                        setWhere += SetWhere17257;
                        break;
                    }
                case 17271:
                    {
                        setRowDetail += setDetail17271;
                        setDt = SetDt17271;
                        break;
                    }
                case 17273:
                    {
                        setRowDetail += setDetail17273;
                        setDt = SetDt17273;
                        break;
                    }
                case 19314:
                    {
                        setRowDetail += setDetail19314;
                        setDt = SetDt19314;
                        break;
                    }
                case 19315:
                    {
                        setRowDetail += setDetail19315;
                        setDt = SetDt19315;
                        break;
                    }


                case 19338://成品预测计划，动态列
                    {
                        setRowDetail += setDetail19338;
                        setDt = SetDt19338;
                        break;
                    }
                case 19339://陈苹果预测计划汇总，动态列
                    {
                        setRowDetail += setDetail19339;
                        setDt = SetDt19339;
                        break;
                    }

                case 22348://注塑预测计划，动态列
                    {
                        setRowDetail += setDetail22348;
                        setDt = SetDt22348;
                        break;
                    }
                case 22347://SMT预测计划，动态列
                    {
                        setRowDetail += setDetail22347;
                        setDt = SetDt22347;
                        break;
                    }
                #endregion




                #region EK
                case 10108:
                    {

                        setDt += SetDt10108;
                        break;

                    }
                case 23353:
                    {

                        setRowDetail += setDetail23353;
                        setDt = SetDt23353;
                        break;

                    }
                case 20342:
                    {

                        setRowDetail += setDetail20342;
                        setDt = SetDt20342;
                        break;

                    }

                #endregion

                #region 帕尔福
                case 15227:
                    {//销售出货计划
                        setRowDetail += setDetai15227;
                        setDt = SetDt15227;

                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 16228:
                    {//预测计划差异
                        setRowDetail += setDetai16228; ;
                        setDt = SetDt16228;

                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 16233:
                    {//预测计划差异
                        setRowDetail += setDetai16233;
                        setDt = SetDt16233;

                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 15210://瑞能销售与要货对比
                    {
                        setDt += SetDt15210;
                        setRowDetail = setDetail15210;
                        break;
                    }
                case 16240:
                    {//正式计划报表
                        setRowDetail += setDetai15214;
                        setDt = SetDt16240;

                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 16237:
                    {//预测计划报表
                        setRowDetail += setDetai16237;
                        setDt = SetDt16237;
                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 17244:
                    {//采购预测计划
                        setRowDetail += setDetai17244;
                        setDt = SetDt17244;
                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 17243:
                    {//月初正式供应计划
                        setRowDetail += setDetai17243;
                        setDt = SetDt17243;
                        // setAfterReadRow += setAfterReadRow15214;
                        break;
                    }
                case 17248:
                    {
                        //采购配比
                        setDt += SetDt17248;
                        setRowDetail = setDetail17248;
                        break;
                    }
                case 17246:
                    {
                        //成品需求计划
                        //  setDt += SetDt17246;
                        // setRowDetail = setDetail17246;
                        break;
                    }
                case 17255:
                    {//采购正式计划
                        setRowDetail += setDetail17255;
                        setDt = SetDt17255;
                        break;
                    }
                case 17274:
                    {//排班计划
                        setRowDetail += setDetail17274;
                        setDt = SetDt17274;
                        break;
                    }
                case 18287:
                    //瑞能主计划汇总表
                    {
                        setDt += SetDt18287;
                        setRowDetail = setDetail18287;
                        break;
                    }
                #endregion

                #region 百威
                case 9021:
                    {

                        setDt += SetDt9021;
                        break;

                    }
                case 13:
                    {

                        setDt += SetDt13;
                        break;

                    }
                case 17277:
                    {//滚动物料需求
                        setRowDetail += setDetail17277;
                        setDt = SetDt17277;
                        break;
                    }
                case 18288:
                    {//百威周送货计划报表
                        setRowDetail += setDetail18288;
                        setDt = SetDt18288;
                        break;
                    }
                case 18289:
                    {//百威欠料追踪的动态日期
                        setDt = SetDt18289;
                        setRowDetail += setDetail18289;

                        break;
                    }
                case 19310:
                    {//百威五日欠料看板
                        setDt = SetDt19310;
                        setRowDetail += setDetail19310;

                        break;
                    }
                case 19293:
                    {
                        // 齐套与不齐套的齐套率
                        setDt = SetDt19293;
                        setRowDetail += setDetail19293;
                        break;
                    }
                // 百威的模拟计算
                case 7868:
                    {

                        setRowDetail += setDetail7868;
                        setDt = SetDt7868;

                        break;
                    }
                case 19305:
                    {
                        // 品号种类与品号群组
                        setDt = SetDt19305;
                        setRowDetail += setDetail19305;
                        break;
                    }

                #endregion

                #region 星河
                //10115
                //需求计划

                //滚动月
                case 23352:
                    {
                        setRowDetail = setDetail23352;
                        setDt = SetDt23352;
                        break;
                    }
                //滚动周
                case 23351:
                    {
                        setRowDetail = setDetail23351;
                        setDt = SetDt23351;
                        break;
                    }
                //全部滚动周
                case 23349:
                    {
                        setRowDetail = setDetail23349;
                        setDt = SetDt23349;
                        break;
                    }
                //业务计划
                case 23350:
                    {
                        setRowDetail = setDetail23350;
                        setDt = SetDt23350;
                        break;
                    }
                //月度接单明细
                case 23365:
                    {
                        setRowDetail = setDetail23365;
                        setDt = SetDt23365;
                        break;
                    }
                //月度接单汇总
                case 23364:
                    {
                        setRowDetail = setDetail23364;
                        setDt = SetDt23364;
                        break;
                    }
                //用户在线统计
                case 24366:
                    {

                        setRowDetail = setDetail24366;
                        setDt = SetDt24366;
                        break;
                    }
                //机台负荷
                case 24365:
                    {

                        setRowDetail = setDetail24365;
                        setDt = SetDt24365;
                        break;
                    }
                case 24395:
                    {
                        setRowDetail = setDetail24395;
                        setDt = SetDt24395;

                        break;
                    }
                case 24441:
                case 24440:
                    {
                        setRowDetail = setDetail24441;
                        setDt = SetDt24441;
                        break;
                    }
                case 24437:
                case 24436:
                    {
                        setRowDetail = setDetail24437;
                        setDt = SetDt24437;
                        break;
                    }
                case 24439:
                case 24438:
                    {
                        setRowDetail = setDetail24439;
                        setDt = SetDt24439;

                        break;
                    }
                case 24435:
                case 24434:
                    {
                        setRowDetail = setDetail24435;
                        setDt = SetDt24435;

                        break;
                    }

                case 24443:
                    {
                        setRowDetail = setDetail24443;
                        setDt = SetDt24443;
                        break;
                    }

                #endregion

                #region 金星徽
                //装配全局欠料汇总
                //滚动月
                case 5610:
                    {
                        if (AppInfo.AppCode == "JXH")
                        {
                            setDt = SetDt5610;
                        }


                        break;
                    }
                //出货计划
                case 25485:
                    {

                        setDt = SetDt25485;

                        setRowDetail = setDetail25485;


                        break;
                    }

                #endregion
                #region 欧赛
                //需求计划导入结果

                case 24403:
                case 27542:
                    {
                        setRowDetail = setDetail24403;
                        setDt = SetDt24403;
                        break;
                    }
                //销售订单分配需求计划
                case 24415:
                    {
                        setRowDetail = setDetail24415;
                        setDt = SetDt24415;
                        break;
                    }
                //需求计划统计
                case 24448:
                    {
                        setRowDetail = setDetail24448;
                        setDt = SetDt24448;
                        break;
                    }
                case 24457:
                    {
                        setRowDetail = setDetail24457;
                        setDt = SetDt24457;
                        break;
                    }
                case 25461:
                    {
                        setRowDetail = setDetail25461;
                        setDt = SetDt25461;
                        break;
                    }
                case 25464:
                    {
                        setRowDetail = setDetail25464;
                        setDt = SetDt25464;
                        break;
                    }
                //日计划按订单工序显示，读取每日计划的内容
                case 25468:
                    {
                        setRowDetail = setDetail25468;
                        setDt = SetDt25468;
                        break;
                    }
                //计划达成，上下结构显示排期与报工
                case 25482:
                    {
                        setRowDetail = setDetail25482;
                        setDt = SetDt25482;
                        break;
                    }
                case 25486:
                    {
                        setRowDetail = setDetail25486;
                        setDt = SetDt25486;
                        break;
                    }
                case 26486:
                    {
                        setRowDetail = setDetail26486;
                        setDt = SetDt26486;
                        break;
                    }
                case 26487:
                    {
                        setRowDetail = setDetail26487;
                        setDt = SetDt26487;
                        break;
                    }
                case 27541:
                    {//营业额与排产对比
                        setRowDetail += setDetail27541;
                        setDt = SetDt27541;

                        break;
                    }
                #endregion

                #region 盈瑞丰

                case 28574:
                    {//人员工序技能矩阵报表
                        setRowDetail += setDetai28574;
                        setDt = SetDt28574;

                        break;
                    }
                case 28581:
                    {//客户按月度汇总
                        //setRowDetail += setDetail28581;
                        setDt = SetDt28581;

                        break;
                    }
                case 28580:
                    {//客户按季度汇总
                        //setRowDetail += setDetail28581;
                        setDt = SetDt28580;

                        break;
                    }
                case 28589:
                    {
                        setRowDetail += setDetail28589;
                        setDt = SetDt28589;

                        break;
                    }
                case 28636:
                case 28634:
                {//DIP周计划
                        //setRowDetail += setDetail28636;
                        setDt = SetDt28634;
                        break;
                    }
                case 28667:
                    {//采购单预测需求分析（外购PO，布局对齐送货计划17271）
                        setRowDetail += setDetail28667;
                        setDt = SetDt28667;
                        break;
                    }
                 case 7833:
                {
                    //相同钢网的控制颜色
                    if(AppInfo.PushType== "YRF")
                    {
                        setRowDetail += setDetail7833;
                        setDt = SetDt7833;
                    }
                    break;
                }
                    #endregion


            }
        }
  

    public string RunLegacyApsDataWithDicHooks()
    {
        ApplyLegacyApsDataDicHooks();
        return base.APSData();
    }
}