using EasyManufacture.Licence;

namespace EasyManufacture.Entitys;

public partial class V_Dev_Account : ISysLogUser
{
    public string Account { get; set; } = string.Empty;
    public string? Pwd { get; set; }
    public string? Name { get; set; }
    public int OrganizeID { get; set; }
    public int? Status { get; set; }
    public string? WorkFlowInstanceID { get; set; }
    public string? Extend1 { get; set; }
    public string? Extend2 { get; set; }
    public string? Extend3 { get; set; }
    public int CenterID { get; set; }
    public int GroupID { get; set; }
}
