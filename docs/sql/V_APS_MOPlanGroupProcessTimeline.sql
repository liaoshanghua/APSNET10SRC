/*
  视图名称：V_APS_MOPlanGroupProcessTimeline
  说明    ：8.排产汇总 — 群组工序计划时间表（智能新视图）
  项目    ：盈瑞丰 APS 专用 [盈瑞丰]
  参考字典：6734 → V_APS_MOPlanStep4
  菜单名称：8.排产汇总
  创建人  ：廖尚华
  编写日期：2026-06-12
  ------------------------------------------------------------------
  分组规则 [盈瑞丰]：
    · RemarkSalesOrderInfo（群组）有值 → 按群组汇总
    · 群组为空 → 按 OrderID 汇总
  工序透视：ProcessName 匹配 贴片/SMT、插件、焊接、组装 → StartDate/EndDate
  数据源：V_APS_MOPlanStep4（字典 6734）
*/
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.V_APS_MOPlanGroupProcessTimeline', N'V') IS NOT NULL
    DROP VIEW dbo.V_APS_MOPlanGroupProcessTimeline;
GO

CREATE VIEW dbo.V_APS_MOPlanGroupProcessTimeline
AS
    WITH Base AS (
        SELECT  A.ProcessPlanID,
                A.OrderID,
                A.OrderNo,
                A.SourceOrderNo,
                A.RemarkSalesOrderInfo,
                A.CustomerName,
                A.ProcessGroupName,
                A.Code,
                A.MaterialName,
                A.ProcessName,
                A.PlanQty,
                A.OweQty,
                A.PCDeliveryDate,
                A.StartDate,
                A.EndDate,
                A.LineID,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(A.RemarkSalesOrderInfo)), N''),
                    CONVERT(NVARCHAR(30), A.OrderID)
                ) AS GroupKey,
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(A.RemarkSalesOrderInfo)), N'') IS NOT NULL
                        THEN N'RemarkSalesOrderInfo'
                    ELSE N'OrderID'
                END AS GroupType
        FROM    dbo.V_APS_MOPlanStep4 AS A WITH ( NOLOCK )
        WHERE   ISNULL(A.LineID, 0) > 0
                AND A.OrderID IS NOT NULL
    ),
    PivotSrc AS (
        SELECT  B.GroupKey,
                B.GroupType,
                B.OrderID,
                B.OrderNo,
                B.RemarkSalesOrderInfo,
                B.CustomerName,
                B.ProcessGroupName,
                B.Code,
                B.MaterialName,
                B.PlanQty,
                B.OweQty,
                B.PCDeliveryDate,
                B.ProcessName,
                B.StartDate,
                B.EndDate,
                CASE
                    WHEN UPPER(B.ProcessName) COLLATE Latin1_General_CI_AI LIKE N'%SMT%'
                         OR B.ProcessName LIKE N'%贴片%' THEN N'贴片'
                    WHEN B.ProcessName LIKE N'%插件%' THEN N'插件'
                    WHEN B.ProcessName LIKE N'%焊接%' THEN N'焊接'
                    WHEN B.ProcessName LIKE N'%组装%' THEN N'组装'
                    ELSE NULL
                END AS ProcessBucket
        FROM    Base AS B
    )
    SELECT  COALESCE(
                NULLIF(LTRIM(RTRIM(MAX(P.RemarkSalesOrderInfo))), N''),
                MAX(P.OrderNo)
            ) AS GroupDisplay,
            MAX(P.CustomerName) AS CustomerName,
            MAX(P.Code) AS Code,
            MAX(P.MaterialName) AS MaterialName,
            MAX(P.PlanQty) AS PlanQty,
            MAX(P.ProcessGroupName) AS ProcessGroupName,
            MIN(P.PCDeliveryDate) AS PCDeliveryDate,
            MIN(CASE WHEN P.ProcessBucket = N'贴片' THEN P.StartDate END) AS SmtStartDate,
            MAX(CASE WHEN P.ProcessBucket = N'贴片' THEN P.EndDate END) AS SmtEndDate,
            MIN(CASE WHEN P.ProcessBucket = N'插件' THEN P.StartDate END) AS DipStartDate,
            MAX(CASE WHEN P.ProcessBucket = N'插件' THEN P.EndDate END) AS DipEndDate,
            MIN(CASE WHEN P.ProcessBucket = N'焊接' THEN P.StartDate END) AS WeldStartDate,
            MAX(CASE WHEN P.ProcessBucket = N'焊接' THEN P.EndDate END) AS WeldEndDate,
            MIN(CASE WHEN P.ProcessBucket = N'组装' THEN P.StartDate END) AS AssemblyStartDate,
            MAX(CASE WHEN P.ProcessBucket = N'组装' THEN P.EndDate END) AS AssemblyEndDate,
            P.GroupKey,
            MAX(P.GroupType) AS GroupType,
            MAX(P.OrderID) AS OrderID,
            MAX(P.OrderNo) AS OrderNo,
            MAX(P.RemarkSalesOrderInfo) AS RemarkSalesOrderInfo,
            SUM(P.OweQty) AS OweQtyTotal,
            COUNT(DISTINCT P.ProcessBucket) AS ProcessBucketCount
    FROM    PivotSrc AS P
    GROUP BY P.GroupKey;
GO

/* 建视图后验证
SELECT TOP 20 GroupDisplay, CustomerName, PlanQty, SmtStartDate, AssemblyEndDate
FROM   dbo.V_APS_MOPlanGroupProcessTimeline
ORDER  BY PCDeliveryDate, GroupDisplay;
*/
