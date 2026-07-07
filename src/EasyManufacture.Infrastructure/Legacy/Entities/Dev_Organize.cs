namespace EasyManufacture.Entitys;

public partial class Dev_Organize
{
    public int OrganizeID { get; set; }
    public string? OrganizeName { get; set; }
    public int? ParentID { get; set; }
    public int? SchedulingDays { get; set; }
}
