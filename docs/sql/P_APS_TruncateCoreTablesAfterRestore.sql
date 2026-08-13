/*
  还原库后清空「指定前缀」的 APS 业务表（保留表结构，删除全部行）。
  由 scripts/Restore-ApsDatabase.ps1 或托盘「还原/清空库」在验证维护密码后调用。

  维护密码：appsettings.json → DatabaseMaintenance:Password
  执行前会将密码哈希写入 dbo.APS_DatabaseMaintenance，存储过程校验 @ConfirmPassword。

  表名前缀（dbo 下凡 name LIKE 前缀+'%' 的用户表均会 TRUNCATE）：
    APS_Order, APS_Material, APS_PO,
    APS_ProcessMaterial, APS_ProcessPlan, APS_SalesOrder
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.APS_DatabaseMaintenance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.APS_DatabaseMaintenance (
        Id int NOT NULL CONSTRAINT PK_APS_DatabaseMaintenance PRIMARY KEY,
        PasswordHash varbinary(32) NOT NULL
    );
    INSERT dbo.APS_DatabaseMaintenance (Id, PasswordHash) VALUES (1, 0x0);
END
GO

CREATE OR ALTER PROCEDURE dbo.P_APS_TruncateCoreTablesAfterRestore
    @ConfirmPassword nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ConfirmPassword IS NULL OR LEN(LTRIM(RTRIM(@ConfirmPassword))) = 0
    BEGIN
        RAISERROR(N'需要维护密码参数 @ConfirmPassword。', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (
            SELECT 1
            FROM dbo.APS_DatabaseMaintenance
            WHERE Id = 1
              AND PasswordHash <> 0x0
        )
    BEGIN
        RAISERROR(N'未配置维护密码哈希。请在 appsettings.json 设置 DatabaseMaintenance:Password 后，通过 APS 或 Restore-ApsDatabase.ps1 执行一次。', 16, 1);
        RETURN;
    END

    IF HASHBYTES('SHA2_256', @ConfirmPassword) <> (
            SELECT PasswordHash FROM dbo.APS_DatabaseMaintenance WHERE Id = 1
        )
    BEGIN
        RAISERROR(N'维护密码错误，已拒绝清空业务表。', 16, 1);
        RETURN;
    END

    DECLARE @prefixes TABLE (
        Prefix sysname NOT NULL PRIMARY KEY
    );

    INSERT INTO @prefixes (Prefix) VALUES
        (N'APS_Order'),
        (N'APS_Material'),
        (N'APS_PO'),
        (N'APS_ProcessMaterial'),
        (N'APS_ProcessPlan'),
        (N'APS_SalesOrder');

    DECLARE @tables TABLE (
        TableName sysname NOT NULL PRIMARY KEY,
        SortOrder int NOT NULL
    );

    INSERT INTO @tables (TableName, SortOrder)
    SELECT
        t.name,
        ROW_NUMBER() OVER (ORDER BY LEN(t.name) DESC, t.name ASC)
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.is_ms_shipped = 0
      AND EXISTS (
            SELECT 1
            FROM @prefixes p
            WHERE t.name LIKE p.Prefix + N'%'
        );

    IF NOT EXISTS (SELECT 1 FROM @tables)
    BEGIN
        RAISERROR(N'未找到匹配前缀的用户表，未执行 TRUNCATE。', 10, 1);
        RETURN;
    END

    DECLARE @disable nvarchar(max) = N'';
    DECLARE @enable nvarchar(max) = N'';

    SELECT
        @disable = @disable
            + N'ALTER TABLE '
            + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
            + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' NOCHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10),
        @enable = @enable
            + N'ALTER TABLE '
            + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
            + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id IN (
            SELECT t.object_id
            FROM sys.tables t
            INNER JOIN @tables x ON t.name = x.TableName
        )
       OR fk.referenced_object_id IN (
            SELECT t.object_id
            FROM sys.tables t
            INNER JOIN @tables x ON t.name = x.TableName
        );

    BEGIN TRY
        BEGIN TRAN;

        IF LEN(@disable) > 0
            EXEC sys.sp_executesql @disable;

        DECLARE @name sysname;
        DECLARE @sql nvarchar(600);

        DECLARE c CURSOR LOCAL FAST_FORWARD FOR
            SELECT TableName FROM @tables ORDER BY SortOrder;

        OPEN c;
        FETCH NEXT FROM c INTO @name;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sql = N'TRUNCATE TABLE dbo.' + QUOTENAME(@name) + N';';
            EXEC sys.sp_executesql @sql;
            FETCH NEXT FROM c INTO @name;
        END

        CLOSE c;
        DEALLOCATE c;

        IF LEN(@enable) > 0
            EXEC sys.sp_executesql @enable;

        COMMIT TRAN;

        SELECT TableName AS TruncatedTable
        FROM @tables
        ORDER BY SortOrder;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        IF LEN(@enable) > 0
        BEGIN TRY
            EXEC sys.sp_executesql @enable;
        END TRY
        BEGIN CATCH
        END CATCH;

        THROW;
    END CATCH
END
GO
