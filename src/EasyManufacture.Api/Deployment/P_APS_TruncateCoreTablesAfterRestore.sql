/*
  还原库后清空「指定前缀」的 APS 业务表（保留表结构，删除全部行）。
  由 scripts/Restore-ApsDatabase.ps1 在 RESTORE 成功后调用。

  表名前缀（dbo 下凡 name LIKE 前缀+'%' 的用户表均会 TRUNCATE）：
    APS_Order, APS_Material, APS_PO,
    APS_ProcessMaterial, APS_ProcessPlan, APS_SalesOrder
*/
SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.P_APS_TruncateCoreTablesAfterRestore
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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
