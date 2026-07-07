using EasyManufacture.Domain.Models;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace EasyManufacture.Entitys;

public partial class V_Dev_Account
{
    public const string HttpContextItemKey = "Dev_Account";

    public static V_Dev_Account? GetDev_Account()
    {
        var ctx = LicenceRuntime.Http.HttpContext;
        if (ctx?.Items.TryGetValue(HttpContextItemKey, out var obj) == true && obj is V_Dev_Account account)
            return account;
        return null;
    }

    public static void SetDev_Account(HttpContext context, V_Dev_Account? account)
    {
        if (account == null)
            context.Items.Remove(HttpContextItemKey);
        else
            context.Items[HttpContextItemKey] = account;
    }

    /// <summary>账号角色配置（旧站 <c>V_Dev_RoleMap</c> 视图字段）。</summary>
    public List<V_Dev_RoleMap> RoleMap { get; set; } = new();

    /// <summary>最后访问时间（登录响应 dev_Account 字段，与旧 Session 一致）。</summary>
    public DateTime LastVisitTime { get; set; } = DateTime.Now;

    /// <summary>按钮权限（旧 LoginController.CheckAccount 写入）。</summary>
    public List<Dictionary<string, object?>> ButtonMenuRoleMap { get; set; } = new();

    /// <summary>Vue 路由菜单。</summary>
    public List<Dictionary<string, object?>> MenuVue { get; set; } = new();

    /// <summary>可访问组织 ID 集合。</summary>
    public List<long> Organizes { get; set; } = new();
}

public static class VDevAccountRoleMapLoader
{
    public static List<V_Dev_RoleMap> Load(string account)
    {
        var safe = account.Replace("'", "''");
        var dt = SqlHelper.ExecuteDataTable($@"
SELECT Account, Email, Name, RoleName, ID, RoleID
FROM V_Dev_RoleMap WITH (NOLOCK)
WHERE Account = '{safe}'");

        return dt.Rows.Cast<DataRow>().Select(MapRow).Where(r => !string.IsNullOrEmpty(r.RoleID)).ToList();
    }

    public static V_Dev_RoleMap ToEntity(RoleMapItem item) => new()
    {
        Account = item.Account,
        Email = item.Email,
        Name = item.Name,
        RoleName = item.RoleName,
        ID = item.ID,
        RoleID = item.RoleID
    };

    private static V_Dev_RoleMap MapRow(DataRow row) => new()
    {
        Account = row["Account"]?.ToString() ?? string.Empty,
        Email = row["Email"]?.ToString(),
        Name = row["Name"]?.ToString(),
        RoleName = row["RoleName"]?.ToString(),
        ID = row["ID"] is int id ? id : Convert.ToInt32(row["ID"] ?? 0),
        RoleID = row["RoleID"]?.ToString() ?? string.Empty
    };
}
