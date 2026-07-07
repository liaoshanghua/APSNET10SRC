using EasyManufacture.Domain.Data;
using EasyManufacture.Domain.Options;
using Microsoft.Extensions.Options;

namespace EasyManufacture.Infrastructure.Legacy;

public static class LegacyRuntime
{
    public static string ConnectionString { get; private set; } = string.Empty;
    public static string ConnectionStringScm { get; private set; } = string.Empty;
    public static AppSettings App { get; private set; } = new();

    public static void Configure(IOptions<DatabaseSettings> db, IOptions<AppSettings> app)
    {
        ConnectionString = SqlConnectionStringHelper.Normalize(db.Value.MSSQLConnectionString);
        ConnectionStringScm = SqlConnectionStringHelper.Normalize(db.Value.MSSQLConnectionStringSCM);
        App = app.Value;
    }
}

public static class MssqlQueryMethods
{
    public static List<string> ListSystemField { get; } =
        ["CreatedOn", "ModifyedOn", "CreatedBy", "CreatedByName", "ModifiedBy", "ModifiedByName"];

    public enum QueryMethod
    {
        模糊匹配 = 0, 精确匹配 = 1, 左匹配 = 2, 右匹配 = 3, 大于 = 4, 小于 = 5, 大于等于 = 6, 小于等于 = 7, 范围 = 8, 包含逗号 = 9, Null = 10, 大于0 = 11, IN = 12, NOTIN = 13, 多个值与 = 14
    }
}


