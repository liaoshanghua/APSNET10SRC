using System.Data;
using Dapper;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Application.Security;
using EasyManufacture.Domain.Models;
using EasyManufacture.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>
/// 账号数据访问（Dapper）。
/// 查库逻辑移植自 <c>EasyManufacture.Entitys.Ex.V_Dev_Account.CheckDev_Account</c>；
/// 登录外壳（限流、日志）在 <see cref="LicenceLoginService"/>。
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly ISqlConnectionFactory _factory;

    /// <summary>
    /// <c>V_Dev_Account</c> 视图实际列（勿选 CenterID/GroupID，二者由存储过程计算）。
    /// 参见旧实体 <c>EasyManufacture.Entitys\V_Dev_Account.cs</c>（EF 生成，无 Center/Group 列）。
    /// </summary>
    private const string AccountSelectColumns =
        "Account, Name, Pwd, Status, OrganizeID, WorkFlowInstanceID, Extend1, Extend2, Extend3";

    public AccountService(ISqlConnectionFactory factory) => _factory = factory;

    /// <inheritdoc />
    /// <remarks>
    /// 支持明文或 MD5 密码；<paramref name="isAutoLogin"/> 为 true 时仅校验账号存在（旧站账号以 <c>.</c> 结尾等场景）。
    /// 若库中密码为 MD5 且本次为明文登录，会回写明文到 <c>Dev_Account.Pwd</c>（与旧 <c>IsMD5</c> 逻辑一致）。
    /// </remarks>
    public async Task<DevAccount?> CheckAccountAsync(string account, string pwd, bool isAutoLogin = false, CancellationToken cancellationToken = default)
    {
        account = account.Trim().TrimEnd('.');
        pwd = pwd.Trim();
        var md5Pwd = Md5Helper.Encrypt(pwd);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var sql = $@"
SELECT TOP 1 {AccountSelectColumns}
FROM V_Dev_Account WITH (NOLOCK)
WHERE (Account = @account OR Name = @account)
  AND (Pwd = @pwd OR Pwd = @md5Pwd)
  AND Status = 1";

        var user = await conn.QueryFirstOrDefaultAsync<DevAccount>(
            new CommandDefinition(sql, new { account, pwd, md5Pwd }, cancellationToken: cancellationToken));

        // 免密/自动登录：与旧 CheckDev_Account(isAutoLogin) 一致，仅按 Account 匹配，不强制 Status=1
        if (user == null && isAutoLogin)
        {
            user = await conn.QueryFirstOrDefaultAsync<DevAccount>(
                new CommandDefinition(
                    $"SELECT TOP 1 {AccountSelectColumns} FROM V_Dev_Account WITH (NOLOCK) WHERE Account = @account",
                    new { account },
                    cancellationToken: cancellationToken));
        }

        // 历史数据：库中为 MD5 时升级为明文存储
        if (user != null && !string.IsNullOrEmpty(pwd) && Md5Helper.IsMd5Hash(user.Pwd))
        {
            await conn.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Dev_Account SET Pwd = @pwd, ModifyedOn = GETDATE() WHERE Account = @account",
                    new { pwd, account = user.Account },
                    cancellationToken: cancellationToken));
        }

        if (user != null)
            await FillOrganizeIdsAsync(conn, user, cancellationToken);

        return user;
    }

    /// <inheritdoc />
    /// <remarks>用于 token/Cookie 解析后的每次请求，会补算 CenterID/GroupID 供 SQL 占位符替换。</remarks>
    public async Task<DevAccount?> GetByAccountAsync(string account, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var user = await conn.QueryFirstOrDefaultAsync<DevAccount>(
            new CommandDefinition(
                $"SELECT TOP 1 {AccountSelectColumns} FROM V_Dev_Account WITH (NOLOCK) WHERE Account = @account AND Status = 1",
                new { account },
                cancellationToken: cancellationToken));

        if (user != null)
            await FillOrganizeIdsAsync(conn, user, cancellationToken);

        return user;
    }

    /// <inheritdoc />
    /// <remarks>对应旧 <c>V_Dev_RoleMap</c> 视图（含 RoleName、Email 等）。</remarks>
    public async Task<IReadOnlyList<RoleMapItem>> GetRoleMapsAsync(string account, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var list = await conn.QueryAsync<RoleMapItem>(
            new CommandDefinition(
                @"SELECT Account, Email, Name, RoleName, ID, RoleID
FROM V_Dev_RoleMap WITH (NOLOCK)
WHERE Account = @account",
                new { account },
                cancellationToken: cancellationToken));
        return list.ToList();
    }

    /// <inheritdoc />
    /// <remarks>对应旧站 <c>V_Dev_ButtonMenuRoleMapRead</c> 查询（含子角色 UNION）。</remarks>
    public async Task<IReadOnlyList<object>> GetButtonMenuRoleMapAsync(string account, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync(
            new CommandDefinition(@"
SELECT *, ButtonName AS BtnName, OnClick AS Methods, ButtonType AS Type
FROM V_Dev_ButtonMenuRoleMapRead WITH (NOLOCK)
WHERE RoleID IN (
    SELECT RoleID FROM Dev_RoleMap WITH (NOLOCK) WHERE Account = @account
    UNION ALL
    SELECT C.RoleID FROM Dev_RoleMap A WITH (NOLOCK)
    INNER JOIN Dev_Role B WITH (NOLOCK) ON A.RoleID = B.RoleID
    INNER JOIN Dev_Role C WITH (NOLOCK) ON C.ParentID = B.RoleID
    WHERE A.Account = @account
)
ORDER BY ViewSort", new { account }, cancellationToken: cancellationToken));

        return rows.Cast<object>().ToList();
    }

    /// <summary>
    /// 根据组织 ID 计算分公司/集团 ID（旧 <c>CheckDev_Account</c> 内两次 <c>P_GetOrganizeID</c>）。
    /// </summary>
    /// <param name="conn">已打开的连接。</param>
    /// <param name="user">已含 <see cref="DevAccount.OrganizeID"/> 的账号。</param>
    /// <remarks>
    /// organizeTypeID=2：输入 OrganizeID，输出写入 CenterID；<br/>
    /// organizeTypeID=0：输入上一步的 CenterID，输出写入 GroupID（与旧 ObjectParameter 复用顺序一致）。
    /// </remarks>
    private static async Task FillOrganizeIdsAsync(SqlConnection conn, DevAccount user, CancellationToken cancellationToken)
    {
        if (user.OrganizeID is not > 0)
            return;

        var organizeId = user.OrganizeID.Value;

        var centerParams = new DynamicParameters();
        centerParams.Add("OrganizeID", organizeId, DbType.Int32, ParameterDirection.InputOutput);
        centerParams.Add("organizeTypeID", 2, DbType.Int32);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "P_GetOrganizeID",
                centerParams,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        user.CenterID = centerParams.Get<int>("OrganizeID");

        var groupParams = new DynamicParameters();
        groupParams.Add("OrganizeID", user.CenterID.Value, DbType.Int32, ParameterDirection.InputOutput);
        groupParams.Add("organizeTypeID", 0, DbType.Int32);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "P_GetOrganizeID",
                groupParams,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        user.GroupID = groupParams.Get<int>("OrganizeID");
    }
}
