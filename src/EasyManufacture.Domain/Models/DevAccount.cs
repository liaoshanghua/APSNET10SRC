namespace EasyManufacture.Domain.Models;

/// <summary>
/// 登录用户 DTO（Net10 应用层）。
/// 与视图 <c>V_Dev_Account</c> 及旧 <c>Entitys.V_Dev_Account</c> 对应。
/// </summary>
public sealed class DevAccount
{
    public string Account { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Pwd { get; set; }
    /// <summary>1=启用，与视图 Status 一致。</summary>
    public int Status { get; set; }
    /// <summary>组织 ID（视图字段）。</summary>
    public int? OrganizeID { get; set; }
    /// <summary>分公司 ID，由 <c>P_GetOrganizeID(OrganizeID, 2)</c> 计算，不在视图中。</summary>
    public int? CenterID { get; set; }
    /// <summary>集团 ID，由 <c>P_GetOrganizeID(CenterID, 0)</c> 计算，不在视图中。</summary>
    public int? GroupID { get; set; }
    /// <summary>工作流实例（视图 WorkFlowInstanceID）。</summary>
    public string? WorkFlowInstanceID { get; set; }
    public string? Extend1 { get; set; }
    public string? Extend2 { get; set; }
    public string? Extend3 { get; set; }
}

/// <summary>对应视图 <c>V_Dev_RoleMap</c>（旧站 CheckDev_Account 写入 dev_Account.RoleMap）。</summary>
public sealed class RoleMapItem
{
    public string Account { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? RoleName { get; set; }
    public int ID { get; set; }
    public string RoleID { get; set; } = string.Empty;
}
