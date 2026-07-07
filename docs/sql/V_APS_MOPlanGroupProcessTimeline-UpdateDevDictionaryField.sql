/*
  阶段 B — 更新 Dev_DictionaryField（仅 UPDATE，不 INSERT）
  视图：V_APS_MOPlanGroupProcessTimeline | 菜单：8.排产汇总
  项目：盈瑞丰 APS 专用 [盈瑞丰] | 创建人：廖尚华 | 2026-06-12

  宽度规则 [盈瑞丰]：
    1. mapping 表 ExplicitWidth（按列类型推荐像素）
    2. 参考字典 6734 同 ParameterName / WidthRef 的 Width
    3. 取两者 MAX；日期列下限 100，客户列下限 180
*/
SET NOCOUNT ON;

DECLARE @DictionaryId    INT = 28649;
DECLARE @RefDictionaryId INT = 6734;

DECLARE @Mapping TABLE (
    FieldIndex            INT           NOT NULL,
    ParameterName         NVARCHAR(100) NOT NULL,
    Comment               NVARCHAR(200) NOT NULL,
    WidthRefParameterName NVARCHAR(100) NULL,
    ExplicitWidth         INT           NOT NULL
);

INSERT INTO @Mapping (FieldIndex, ParameterName, Comment, WidthRefParameterName, ExplicitWidth)
VALUES
    ( 1, N'GroupDisplay',       N'群组',     N'RemarkSalesOrderInfo', 110),
    ( 2, N'CustomerName',       N'客户',     N'CustomerName',         200),
    ( 3, N'Code',               N'产品编码', N'Code',                 100),
    ( 4, N'MaterialName',       N'产品名称', N'MaterialName',         130),
    ( 5, N'PlanQty',            N'数量',     N'PlanQty',               70),
    ( 6, N'ProcessGroupName',   N'工艺',     N'ProcessGroupName',      90),
    ( 7, N'PCDeliveryDate',     N'要求开工', N'PCDeliveryDate',       100),
    ( 8, N'SmtStartDate',       N'贴片开始', N'StartDate',            100),
    ( 9, N'SmtEndDate',         N'贴片完成', N'EndDate',              100),
    (10, N'DipStartDate',       N'插件开始', N'StartDate',            100),
    (11, N'DipEndDate',         N'插件完成', N'EndDate',              100),
    (12, N'WeldStartDate',      N'焊接开始', N'StartDate',            100),
    (13, N'WeldEndDate',        N'焊接完成', N'EndDate',              100),
    (14, N'AssemblyStartDate',  N'组装开始', N'StartDate',            100),
    (15, N'AssemblyEndDate',    N'组装完成', N'EndDate',              100);

UPDATE  F
SET     F.Comment = M.Comment,
        F.FieldIndex = M.FieldIndex,
        F.Width = CASE
            WHEN M.ParameterName LIKE N'%Date' OR M.ParameterName = N'PCDeliveryDate'
                THEN CASE
                    WHEN COALESCE(W.CalcWidth, M.ExplicitWidth) < 100 THEN 100
                    ELSE COALESCE(W.CalcWidth, M.ExplicitWidth)
                END
            WHEN M.ParameterName = N'CustomerName'
                THEN CASE
                    WHEN COALESCE(W.CalcWidth, M.ExplicitWidth) < 180 THEN 180
                    ELSE COALESCE(W.CalcWidth, M.ExplicitWidth)
                END
            ELSE COALESCE(W.CalcWidth, M.ExplicitWidth)
        END,
        F.IsVisible = 1,
        F.ModifyedOn = GETDATE()
FROM    dbo.Dev_DictionaryField AS F
        INNER JOIN @Mapping AS M ON M.ParameterName = F.ParameterName
        OUTER APPLY (
            SELECT MAX(v) AS CalcWidth
            FROM (
                SELECT M.ExplicitWidth AS v
                UNION ALL
                SELECT S.Width
                FROM   dbo.Dev_DictionaryField AS S
                WHERE  S.DictionaryID = @RefDictionaryId
                       AND S.ParameterName = F.ParameterName
                       AND S.Width IS NOT NULL
                UNION ALL
                SELECT S.Width
                FROM   dbo.Dev_DictionaryField AS S
                WHERE  S.DictionaryID = @RefDictionaryId
                       AND S.ParameterName = M.WidthRefParameterName
                       AND S.Width IS NOT NULL
            ) AS X
            WHERE v IS NOT NULL AND v > 0
        ) AS W
WHERE   F.DictionaryID = @DictionaryId;

UPDATE  F
SET     F.IsVisible = 0, F.ModifyedOn = GETDATE()
FROM    dbo.Dev_DictionaryField AS F
WHERE   F.DictionaryID = @DictionaryId
        AND NOT EXISTS (
            SELECT 1 FROM @Mapping AS M WHERE M.ParameterName = F.ParameterName
        );

SELECT FieldIndex, ParameterName, Comment, Width, IsVisible
FROM   dbo.Dev_DictionaryField
WHERE  DictionaryID = @DictionaryId AND IsVisible = 1
ORDER  BY FieldIndex;
