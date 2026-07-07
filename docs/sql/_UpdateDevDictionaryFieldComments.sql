/*
  更新 Dev_DictionaryField：Comment、FieldIndex、Width、IsVisible（仅 UPDATE，不 INSERT）
  Width：取库内同 ParameterName 最新一条（ModifyedOn / ID 最大），排除当前字典
  IsVisible：图片有的栏位=1，图片没有的栏位=0

  [盈瑞丰] 以下默认值来自盈瑞丰项目；其他项目需按目标库核对 Dev_Menu / 字典结构。
  前提：栏位已由用户在系统中插入
  用法：修改 @DictionaryId 与 @Mapping 后执行
*/
DECLARE @DictionaryId INT = 0; -- TODO

DECLARE @Mapping TABLE (
    ParameterName NVARCHAR(100) NOT NULL,
    Comment       NVARCHAR(200) NOT NULL,
    FieldIndex    INT           NOT NULL
);

-- INSERT INTO @Mapping ...

UPDATE  F
SET     F.Comment = M.Comment,
        F.FieldIndex = M.FieldIndex,
        F.Width = COALESCE(
            (
                SELECT TOP (1) S.Width
                FROM   dbo.Dev_DictionaryField AS S
                WHERE  S.ParameterName = F.ParameterName
                       AND S.DictionaryID <> @DictionaryId
                       AND S.Width IS NOT NULL
                ORDER  BY S.ModifyedOn DESC,
                          S.ID DESC
            ),
            F.Width
        ),
        F.IsVisible = 1,
        F.ModifyedOn = GETDATE()
FROM    dbo.Dev_DictionaryField AS F
        INNER JOIN @Mapping AS M
            ON M.ParameterName = F.ParameterName
WHERE   F.DictionaryID = @DictionaryId;

UPDATE  F
SET     F.IsVisible = 0,
        F.ModifyedOn = GETDATE()
FROM    dbo.Dev_DictionaryField AS F
WHERE   F.DictionaryID = @DictionaryId
        AND NOT EXISTS (
            SELECT 1
            FROM   @Mapping AS M
            WHERE  M.ParameterName = F.ParameterName
        );

SELECT  ParameterName,
        Comment,
        FieldIndex,
        Width,
        IsVisible
FROM    dbo.Dev_DictionaryField
WHERE  DictionaryID = @DictionaryId
ORDER  BY FieldIndex,
         ParameterName;
