using System.ComponentModel.DataAnnotations.Schema;

namespace EasyManufacture.Entitys;

public partial class V_Dev_Account
{
    /// <summary>登录 Session 菜单列表（内存字段，非 EF 导航）。</summary>
    [NotMapped]
    public List<Dev_Menu>? Menu { get; set; }
}
