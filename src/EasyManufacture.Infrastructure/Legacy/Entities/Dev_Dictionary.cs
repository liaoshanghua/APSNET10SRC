namespace EasyManufacture.Entitys;

public class Dev_Dictionary
{
    public int DictionaryID { get; set; }
    public string? ObjectName { get; set; }
    public string? ObjectText { get; set; }
    public string? AppCode { get; set; }
    public string? TabelName { get; set; }
    public string? ObjectType { get; set; }
    public string? BeforeUpdate { get; set; }
    public string? AfterUpdate { get; set; }
    public string? TreeField { get; set; }
    public string? ParentField { get; set; }
    public bool? IsShowCheck { get; set; }
    public string? BeforeAdd { get; set; }
    public string? AfterAdd { get; set; }
    public string? BeforeDelete { get; set; }
    public string? AfterDelete { get; set; }
    public string? DeleteCondition { get; set; }
    public string? AfterExecution { get; set; }
    public string? MenuCode { get; set; }
    public int PageSize { get; set; }
    public string? WorkFlowInstanceID { get; set; }
    public int? Status { get; set; }
    /// <summary>为 "true" 时表格行带 update=true（与旧 APSCore.tableUpdate 一致）。</summary>
    public string? Remark2 { get; set; }
}
