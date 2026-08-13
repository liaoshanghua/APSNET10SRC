using EasyManufacture.Infrastructure.Legacy;
using Microsoft.Extensions.Logging;

namespace EasyManufacture.Infrastructure.Scheduling;

/// <summary>启动时数据库结构自检（摘自 Global.asax Application_Start）。</summary>
public static class DatabaseSchemaUpgrader
{
    public static void Run(ILogger logger)
    {
        try
        {
            UpgradeDevDictionaryField(logger);
            UpgradeDevDictionary(logger);
            UpgradeDevOrganize(logger);
            UpgradeApsDayPlan(logger);
            UpgradeDevSysLog(logger);
            logger.LogInformation("数据库结构自检完成");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "数据库结构自检异常（不影响站点启动）");
        }
    }

    /// <summary>查 INFORMATION_SCHEMA，不依赖表是否有数据、也不依赖 P_UPDATE_Dev_Dictionary 是否已刷新。</summary>
    private static bool ColumnExists(string tableName, string columnName)
    {
        var dt = SqlHelper.ExecuteDataTable($@"
SELECT 1
FROM INFORMATION_SCHEMA.COLUMNS (NOLOCK)
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = '{tableName}'
  AND COLUMN_NAME = '{columnName}'");
        return dt.Rows.Count > 0;
    }

    private static void UpgradeDevDictionaryField(ILogger logger)
    {
        if (!ColumnExists("Dev_DictionaryField", "IsAdd"))
        {
            logger.LogInformation("补丁 Dev_DictionaryField.IsAdd");
            SqlHelper.ExecuteNonQuery(@"
alter table Dev_DictionaryField add IsAdd bit 
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'允许新增' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Dev_DictionaryField', @level2type=N'COLUMN',@level2name=N'IsAdd'
exec [dbo].[P_UPDATE_Dev_Dictionary]
update Dev_DictionaryField set IsVisible=1,IsEdit=1,isadd=IsEdit,FieldIndex=10
where ParameterName='IsAdd' AND CAST(GETDATE() AS DATE)=CAST(CreatedOn AS DATE)
update Dev_DictionaryField set isadd=isedit");
        }

        if (!ColumnExists("Dev_DictionaryField", "IsSelect"))
        {
            logger.LogInformation("补丁 Dev_DictionaryField.IsSelect");
            SqlHelper.ExecuteNonQuery(@"
alter table Dev_DictionaryField add IsSelect bit default (1) NOT NULL
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'返回数据' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Dev_DictionaryField', @level2type=N'COLUMN',@level2name=N'IsSelect'
exec [dbo].[P_UPDATE_Dev_Dictionary]
update Dev_DictionaryField set IsVisible=1,IsEdit=1,isadd=1,FieldIndex=11,Width=60 where ParameterName='IsSelect'");
        }
    }

    private static void UpgradeDevDictionary(ILogger logger)
    {
        var alterField = string.Empty;

        if (!ColumnExists("Dev_Dictionary", "SyncDatetime"))
            alterField += "alter table Dev_Dictionary add SyncDatetime datetime \n";
        if (!ColumnExists("Dev_Dictionary", "DataSource"))
            alterField += "alter table Dev_Dictionary add DataSource NVARCHAR(50) NULL \n";
        if (!ColumnExists("Dev_Dictionary", "SyncRate"))
            alterField += "alter table Dev_Dictionary add SyncRate int NULL \n";
        if (!ColumnExists("Dev_Dictionary", "VisitDatetime"))
            alterField += "alter table Dev_Dictionary add VisitDatetime datetime NULL \n";
        if (!ColumnExists("Dev_Dictionary", "RecordCount"))
            alterField += "alter table Dev_Dictionary add RecordCount bigint NULL \n";
        if (!ColumnExists("Dev_Dictionary", "RunSeconds"))
            alterField += "alter table Dev_Dictionary add RunSeconds decimal(18,3) NULL \n";
        if (!ColumnExists("Dev_Dictionary", "Visits"))
            alterField += "alter table Dev_Dictionary add Visits bigint NULL \n";
        if (!ColumnExists("Dev_Dictionary", "SyncContent"))
            alterField += "alter table Dev_Dictionary add SyncContent nvarchar(300) default('数据来源：{0},同步频率：{1},最新同步日期：{2:yyyy-MM-dd HH:mm:ss}') NOT NULL \n";
        if (!ColumnExists("Dev_Dictionary", "MenuName"))
            alterField += "alter table Dev_Dictionary add MenuName nvarchar(50) \n";
        if (!ColumnExists("Dev_Dictionary", "AfterExecution"))
            alterField += "alter table Dev_Dictionary add AfterExecution varchar(6000) \n";

        if (string.IsNullOrEmpty(alterField)) return;

        logger.LogInformation("补丁 Dev_Dictionary 扩展列");
        SqlHelper.ExecuteNonQuery(alterField);
        SqlHelper.ExecuteNonQuery("update Dev_Dictionary set SyncDatetime = null; exec [dbo].[P_UPDATE_Dev_Dictionary]");
    }

    private static void UpgradeDevOrganize(ILogger logger)
    {
        if (ColumnExists("Dev_Organize", "GroupName")) return;

        logger.LogInformation("补丁 Dev_Organize.GroupName");
        SqlHelper.ExecuteNonQuery(@"
alter table Dev_Organize add GroupName int NULL
exec [dbo].[P_UPDATE_Dev_Dictionary]");
    }

    private static void UpgradeApsDayPlan(ILogger logger)
    {
        if (ColumnExists("APS_DayPlan", "PLT")) return;

        logger.LogInformation("补丁 APS_DayPlan.PLT");
        SqlHelper.ExecuteNonQuery(@"
alter table APS_DayPlan add PLT int NULL
exec [dbo].[P_UPDATE_Dev_Dictionary]");
    }

    /// <summary>Dev_SysLog.MenuName：保存请求头 Vuemenunameforlog（菜单地址）。</summary>
    private static void UpgradeDevSysLog(ILogger logger)
    {
        if (ColumnExists("Dev_SysLog", "MenuName")) return;

        logger.LogInformation("补丁 Dev_SysLog.MenuName");
        SqlHelper.ExecuteNonQuery(@"
ALTER TABLE dbo.Dev_SysLog ADD MenuName nvarchar(200) NULL;
EXEC sys.sp_addextendedproperty
  @name = N'MS_Description', @value = N'菜单地址（Vuemenunameforlog）',
  @level0type = N'SCHEMA', @level0name = N'dbo',
  @level1type = N'TABLE', @level1name = N'Dev_SysLog',
  @level2type = N'COLUMN', @level2name = N'MenuName';");
    }
}
