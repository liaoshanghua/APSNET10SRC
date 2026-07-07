using EasyManufacture.Domain.Models;

namespace EasyManufacture.Application.Abstractions;

/// <summary>账号与角色数据（对应旧 <c>V_Dev_Account.CheckDev_Account</c> 及 Login 相关 SQL）。</summary>
public interface IAccountService
{
    /// <summary>校验账号密码，成功返回用户（含 CenterID/GroupID）。</summary>
    Task<DevAccount?> CheckAccountAsync(string account, string pwd, bool isAutoLogin = false, CancellationToken cancellationToken = default);

    /// <summary>按账号加载用户（token 续期、中间件鉴权用）。</summary>
    Task<DevAccount?> GetByAccountAsync(string account, CancellationToken cancellationToken = default);

    /// <summary>用户角色 ID 列表。</summary>
    Task<IReadOnlyList<RoleMapItem>> GetRoleMapsAsync(string account, CancellationToken cancellationToken = default);

    /// <summary>按钮权限（Vue/前端 BtnName、Methods 等）。</summary>
    Task<IReadOnlyList<object>> GetButtonMenuRoleMapAsync(string account, CancellationToken cancellationToken = default);
}
