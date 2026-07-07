/*
  阶段 C — 新增 Dev_Menu 菜单（仅 INSERT，不清理、不更新已有菜单）
  菜单名称：8.排产汇总
  上级菜单：M2312230003
  新字典 ID：28649
  项目    ：盈瑞丰 APS 专用 [盈瑞丰]

  说明：TargetFor/Ico/Component/Name 写死模板 M2508130001 默认值
        Remark2 = 字典 ID；Dev_Dictionary.MenuCode 同步关联
*/
SET NOCOUNT ON;

DECLARE @ParentCode    VARCHAR(20)  = 'M2312230003';
DECLARE @MenuCode      VARCHAR(20)  = 'M2606120004';   -- 已存在则改末位序号
DECLARE @MenuName      NVARCHAR(50) = N'8.排产汇总';
DECLARE @DictionaryId  INT          = 28649;

DECLARE @TargetFor     VARCHAR(20)  = 'tableType=excel';
DECLARE @Ico           VARCHAR(30)  = 'el-icon-s-order';
DECLARE @Component     VARCHAR(100) = 'PageCommon/CommonReport';
DECLARE @ComponentName NVARCHAR(50) = N'CommonReport';
DECLARE @ViewSort      INT          = 20;
DECLARE @RouteUrl      VARCHAR(100) = 'dic' + CONVERT(VARCHAR(20), @DictionaryId);
DECLARE @AppCode       VARCHAR(20);

SELECT  @AppCode = T.AppCode
FROM    dbo.Dev_Menu AS T
WHERE   T.MenuCode = 'M2508130001';

IF EXISTS (SELECT 1 FROM dbo.Dev_Menu WHERE MenuCode = @MenuCode)
BEGIN
    RAISERROR(N'菜单编码已存在：%s，请修改 @MenuCode 后重试', 16, 1, @MenuCode);
    RETURN;
END;

INSERT INTO dbo.Dev_Menu (
    MenuCode, MenuName, IsEnable, IsAllVisible,
    TargetFor, Url, ParentCode, AppCode, Ico,
    CreatedBy, CreatedByName, ModifiedBy, ModifiedByName,
    CreatedOn, ModifyedOn, ViewSort,
    Name, Remark2, Component
)
VALUES (
    @MenuCode, @MenuName, 1, 0,
    @TargetFor, @RouteUrl, @ParentCode, @AppCode, @Ico,
    N'廖尚华', N'廖尚华', N'廖尚华', N'廖尚华',
    GETDATE(), GETDATE(), @ViewSort,
    @ComponentName, CONVERT(NVARCHAR(20), @DictionaryId), @Component
);

UPDATE  dbo.Dev_Dictionary
SET     MenuCode = @MenuCode,
        ObjectText = @MenuName,
        TabelName = N'V_APS_MOPlanGroupProcessTimeline',
        ModifyedOn = GETDATE()
WHERE   DictionaryID = @DictionaryId;

SELECT  M.MenuCode, M.MenuName, M.ParentCode, M.Url,
        M.TargetFor, M.Ico, M.Component, M.Name, M.Remark2 AS dicID,
        D.DictionaryID, D.ObjectText
FROM    dbo.Dev_Menu AS M
        LEFT JOIN dbo.Dev_Dictionary AS D ON D.DictionaryID = @DictionaryId
WHERE   M.MenuCode = @MenuCode;
