using EasyManufacture.Application.Security;
using EasyManufacture.Infrastructure.Legacy;
using System.Data;

namespace EasyManufacture.Entitys;

public partial class V_Dev_Account
{
    /// <summary>登录验证（移植自旧 <c>V_Dev_Account.CheckDev_Account</c>）。</summary>
    public static V_Dev_Account? CheckDev_Account(
        string account,
        string pwd,
        string appCode,
        bool isAutoLogin = false,
        bool isReadButtomRole = true)
    {
        _ = appCode;
        _ = isReadButtomRole;

        pwd = pwd.Trim();
        var md5Pwd = Md5Helper.Encrypt(pwd);
        var safeAccount = account.Replace("'", "''");
        var safePwd = pwd.Replace("'", "''");
        var safeMd5 = md5Pwd.Replace("'", "''");

        const string columns =
            "Account, Name, Pwd, Status, OrganizeID, WorkFlowInstanceID, Extend1, Extend2, Extend3";

        var sql = $"""
            SELECT TOP 1 {columns}
            FROM V_Dev_Account WITH (NOLOCK)
            WHERE (Account = '{safeAccount}' OR Name = '{safeAccount}')
              AND (Pwd = '{safePwd}' OR Pwd = '{safeMd5}')
              AND Status = 1
            """;

        var dt = SqlHelper.ExecuteDataTable(sql);
        if ((dt == null || dt.Rows.Count == 0) && isAutoLogin)
        {
            dt = SqlHelper.ExecuteDataTable($"""
                SELECT TOP 1 {columns}
                FROM V_Dev_Account WITH (NOLOCK)
                WHERE Account = '{safeAccount}'
                """);
        }

        if (dt == null || dt.Rows.Count == 0)
            return null;

        var user = MapRow(dt.Rows[0]);
        if (string.IsNullOrEmpty(user.Account))
            return null;

        if (!string.IsNullOrEmpty(pwd) && Md5Helper.IsMd5Hash(user.Pwd))
        {
            SqlHelper.ExecuteNonQuery(
                $"UPDATE Dev_Account SET Pwd = '{safePwd}', ModifyedOn = GETDATE() WHERE Account = '{safeAccount}'");
            user.Pwd = pwd;
        }

        return user;
    }

    private static V_Dev_Account MapRow(DataRow row) => new()
    {
        Account = row["Account"]?.ToString() ?? "",
        Name = row["Name"]?.ToString(),
        Pwd = row["Pwd"]?.ToString(),
        Status = row["Status"] is DBNull ? null : Convert.ToInt32(row["Status"]),
        OrganizeID = row["OrganizeID"] is DBNull ? 0 : Convert.ToInt32(row["OrganizeID"]),
        WorkFlowInstanceID = row["WorkFlowInstanceID"]?.ToString(),
        Extend1 = row["Extend1"]?.ToString(),
        Extend2 = row["Extend2"]?.ToString(),
        Extend3 = row["Extend3"]?.ToString()
    };
}
