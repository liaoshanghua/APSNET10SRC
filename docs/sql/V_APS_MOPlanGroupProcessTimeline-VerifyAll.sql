/*
  全流程验证 — 8.排产汇总（字典 28649）
  在阶段 A/B/C 全部执行完后运行
*/
SET NOCOUNT ON;

DECLARE @DictionaryId INT = 28649;
DECLARE @MenuName     NVARCHAR(50) = N'8.排产汇总';
DECLARE @ViewName     SYSNAME = N'V_APS_MOPlanGroupProcessTimeline';

PRINT N'========== ① 视图 ==========';
IF OBJECT_ID(N'dbo.' + @ViewName, N'V') IS NOT NULL
    PRINT N'[OK] 视图存在: ' + @ViewName;
ELSE
    PRINT N'[FAIL] 视图不存在，请先执行阶段 A';

SELECT TOP 5
        GroupDisplay, CustomerName, PlanQty, SmtStartDate, AssemblyEndDate
FROM    dbo.V_APS_MOPlanGroupProcessTimeline
ORDER   BY PCDeliveryDate;

PRINT N'========== ② 字典 ==========';
SELECT  DictionaryID, ObjectText, TabelName, MenuCode
FROM    dbo.Dev_Dictionary
WHERE   DictionaryID = @DictionaryId;

PRINT N'========== ③ 栏位（可见 15 列） ==========';
SELECT  FieldIndex, ParameterName, Comment, Width, IsVisible
FROM    dbo.Dev_DictionaryField
WHERE   DictionaryID = @DictionaryId AND IsVisible = 1
ORDER   BY FieldIndex;

DECLARE @VisibleCount INT = (
    SELECT COUNT(*) FROM dbo.Dev_DictionaryField
    WHERE DictionaryID = @DictionaryId AND IsVisible = 1
);
IF @VisibleCount = 15
    PRINT N'[OK] 可见栏位 15 列';
ELSE
    PRINT N'[WARN] 可见栏位 ' + CAST(@VisibleCount AS NVARCHAR(10)) + N' 列，期望 15';

PRINT N'========== ④ 菜单 ==========';
SELECT  M.MenuCode, M.MenuName, M.ParentCode, M.Url,
        M.TargetFor, M.Ico, M.Component, M.Name, M.Remark2 AS dicID,
        D.DictionaryID, D.MenuCode AS DictMenuCode, D.ObjectText
FROM    dbo.Dev_Menu AS M
        INNER JOIN dbo.Dev_Dictionary AS D
            ON D.DictionaryID = @DictionaryId
           AND D.MenuCode = M.MenuCode
WHERE   M.ParentCode = N'M2312230003'
        AND M.MenuName = @MenuName;

PRINT N'期望：MenuName=8.排产汇总, Url=dic28649, Name=CommonReport, Remark2=28649';
