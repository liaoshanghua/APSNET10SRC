namespace EasyManufacture.Entitys;

public partial class Dev_Account
{
    public string Account { get; set; } = "";
    public string? Pwd { get; set; }
    public int? PositionID { get; set; }
    public string? Name { get; set; }
    public string? CardNo { get; set; }
    public string? NickName { get; set; }
    public int OrganizeID { get; set; }
    public string? Email { get; set; }
    public string? Sex { get; set; }
    public DateTime? EntryDate { get; set; }
    public DateTime? LeaveDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? ModifiedBy { get; set; }
    public string? ModifiedByName { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifyedOn { get; set; }
    public string? WorkFlowInstanceID { get; set; }
    public int? Status { get; set; }
    public int? UserType { get; set; }
    public int? UserAttr { get; set; }
    public string? OrganizeName { get; set; }
    public string? LeadUserCode { get; set; }
    public string? Tel { get; set; }
}
