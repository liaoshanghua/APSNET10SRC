using EasyManufacture.Application.Abstractions;
using EasyManufacture.Core;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using System.Data;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>
/// 登录成功后补全 <see cref="V_Dev_Account"/> 与扩展字段，使 CheckAccount JSON 与旧站一致。
/// 对应旧 <c>LoginController.CheckAccount</c> 中 RoleMap / MenuVue / Organizes 等逻辑。
/// </summary>
public sealed class LoginSessionEnricher
{
    private readonly IAccountService _accountService;

    public LoginSessionEnricher(IAccountService accountService) => _accountService = accountService;

    public sealed class EnrichResult
    {
        public required V_Dev_Account Account { get; init; }
        public object? IsChangePwd { get; init; }
        public object? AvatarUrl { get; init; }
        public int IsUpLoadAvatar { get; init; }
    }

    public async Task<EnrichResult> EnrichAsync(V_Dev_Account account, CancellationToken cancellationToken = default)
    {
        account.LastVisitTime = DateTime.Now;

        var roleMaps = await _accountService.GetRoleMapsAsync(account.Account, cancellationToken);
        account.RoleMap = roleMaps.Select(VDevAccountRoleMapLoader.ToEntity).ToList();

        var roleId = "'no'," + string.Join(",", account.RoleMap.Select(r => $"'{r.RoleID}'"));

        var dtRole = SqlHelper.ExecuteDataTable($@"
SELECT *, ButtonName AS BtnName, OnClick AS Methods, ButtonType AS Type
FROM [dbo].[V_Dev_ButtonMenuRoleMapRead]
WHERE RoleID IN (
    SELECT RoleID FROM [dbo].[Dev_RoleMap] WHERE Account = '{Escape(account.Account)}'
    UNION ALL
    SELECT C.RoleID FROM [dbo].[Dev_RoleMap] A
    INNER JOIN Dev_Role B ON A.RoleID = B.RoleID
    INNER JOIN Dev_Role C ON C.ParentID = B.RoleID
    WHERE A.Account = '{Escape(account.Account)}'
)
ORDER BY ViewSort");
        account.ButtonMenuRoleMap = JsonHelper.GetListFromDt(dtRole);

        var dtMenu = SqlHelper.ExecuteDataTable($@"
SELECT A.* FROM Dev_Menu A
WHERE A.IsEnable = 1 AND A.Component IS NOT NULL
AND (A.IsAllVisible = 1 OR A.MenuCode IN (
    SELECT MenuCode FROM Dev_RoleMenuMap
    WHERE RoleID IN ({roleId.TrimEnd(',')})
))");
        account.MenuVue = JsonHelper.GetListFromDt(dtMenu);

        account.Organizes = new List<long>();
        var dtOrg = SqlHelper.ExecuteDataTable($@"
SELECT B.OrganizeID AS OrganizeIDC, A.OrganizeID
FROM Dev_OrganizeManger A
FULL JOIN Dev_Organize B ON A.OrganizeID = B.ParentID
WHERE A.Account = '{Escape(account.Account)}' AND A.OrganizeID > 0");

        long orgId = 0;
        if (dtOrg.Rows.Count > 0)
        {
            long.TryParse(dtOrg.Rows[0]["OrganizeID"]?.ToString(), out orgId);
            if (orgId > 0)
            {
                var dtTree = SqlHelper.ExecuteDataTable($@"
SELECT OrganizeID FROM Dev_Organize WHERE OrganizeIDs LIKE '%${orgId}$%'");
                foreach (DataRow row in dtTree.Rows)
                {
                    if (long.TryParse(row["OrganizeID"]?.ToString(), out orgId) && orgId > 0 && !account.Organizes.Contains(orgId))
                        account.Organizes.Add(orgId);
                }
            }
        }

        foreach (DataRow row in dtOrg.Rows)
        {
            if (long.TryParse(row["OrganizeID"]?.ToString(), out orgId) && orgId > 0 && !account.Organizes.Contains(orgId))
                account.Organizes.Add(orgId);
            if (long.TryParse(row["OrganizeIDC"]?.ToString(), out orgId) && orgId > 0 && !account.Organizes.Contains(orgId))
                account.Organizes.Add(orgId);
        }

        object? isChangePwd = null;
        object? avatarUrl = null;
        var isUploadAvatar = 0;
        var dtAccount = SqlHelper.ExecuteDataset($"""
            SELECT * FROM V_Dev_Account WHERE Account = '{Escape(account.Account)}';
            SELECT STUFF((
                SELECT N',' + T.OrganizeName
                FROM (
                    SELECT DISTINCT B.OrganizeID, B.OrganizeName
                    FROM [dbo].[Dev_OrganizeManger] A
                    INNER JOIN [dbo].[Dev_Organize] B ON A.OrganizeID = B.OrganizeID
                    WHERE A.Account = '{Escape(account.Account)}'
                ) T
                FOR XML PATH(''), TYPE
            ).value(N'.', N'NVARCHAR(MAX)'), 1, 1, N'') AS OrganizeNames;
            """);
        if (dtAccount.Tables.Count > 0 && dtAccount.Tables[0].Rows.Count > 0)
        {
            var table = dtAccount.Tables[0];
            var row = table.Rows[0];
            account.Extend1 = row["Extend1"]?.ToString();
            if (dtAccount.Tables.Count > 1 && dtAccount.Tables[1].Rows.Count > 0)
                account.Extend2 = dtAccount.Tables[1].Rows[0]["OrganizeNames"]?.ToString();
            else
                account.Extend2 = row["Extend2"]?.ToString();
            account.Extend3 = row["Extend3"]?.ToString();
            // 旧站 dev_Account 含 Pwd，前端刷新 CheckAccount 时会从本地缓存回传
            if (table.Columns.Contains("Pwd"))
                account.Pwd = row["Pwd"]?.ToString();
            if (table.Columns.Contains("IsChangePwd"))
                isChangePwd = row["IsChangePwd"] == DBNull.Value ? null : row["IsChangePwd"];
            if (table.Columns.Contains("AvatarURL"))
            {
                avatarUrl = row["AvatarURL"] == DBNull.Value ? null : row["AvatarURL"];
                isUploadAvatar = 1;
            }
        }

        return new EnrichResult
        {
            Account = account,
            IsChangePwd = isChangePwd,
            AvatarUrl = avatarUrl,
            IsUpLoadAvatar = isUploadAvatar
        };
    }

    private static string Escape(string s) => s.Replace("'", "''");
}
