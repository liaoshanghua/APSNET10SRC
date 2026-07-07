namespace EasyManufacture.Entitys;

/// <summary>
/// 字典字段 + 字典主表联接列（GetConfig / SqlQueryRaw 用）。
/// 不可继承 <see cref="Dev_DictionaryField"/>，否则 EF Core 会启用 TPH 并查询无效列。
/// </summary>
public class V_DictionaryField
{
    public int ID { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public int? DictionaryID { get; set; }
    public string? Comment { get; set; }
    public string? DataType { get; set; }
    public string? ControlType { get; set; }
    public string? DataSourceID { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsVisibleApp { get; set; }
    public int? Width { get; set; }
    public bool? IsFrozen { get; set; }
    public bool? IsKey { get; set; }
    public bool? IsIdentity { get; set; }
    public bool? IsEdit { get; set; }
    public bool? Required { get; set; }
    public bool? IsQuery { get; set; }
    public string? ValidType { get; set; }
    public int? FieldIndex { get; set; }
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? DefaultValue { get; set; }
    public string? Formatter { get; set; }
    public string? Formula { get; set; }
    public string? ColTitle { get; set; }
    public string? SaveParameterName { get; set; }
    public string? sortable { get; set; }
    public string? active { get; set; }
    public string? icon { get; set; }
    public string? button { get; set; }
    public string? component { get; set; }
    public string? fix { get; set; }
    public bool? isMerge { get; set; }
    public bool? RouterName { get; set; }
    public string? align { get; set; }
    public string? DefaultAddValue { get; set; }
    public bool? IsAdd { get; set; }
    public bool? IsQueryParams { get; set; }
    public string? ForeignKey { get; set; }
    public bool? IsSelect { get; set; }
    public string? FooterType { get; set; }
    public int? QueryMethod { get; set; }
    public int? UPrecision { get; set; }
    public int? Status { get; set; }
    public int? FieldLength { get; set; }
    public int? RowSpan { get; set; }
    public bool? ReadOnly { get; set; }
    public string? AppWith { get; set; }
    public string? UIDataOptions { get; set; }
    public bool? RowToColumn { get; set; }
    public bool? RowToColumnValue { get; set; }
    public bool? ImportRequired { get; set; }

    public string? ObjectName { get; set; }
    public string? TabelName { get; set; }
    public bool? IsShowCheck { get; set; }
    public string? MenuCode { get; set; }
    /// <summary>来自 <c>Dev_Dictionary.Region</c>（联接查询 B.Region），非 Dev_DictionaryField 表列。</summary>
    public string? Region { get; set; }
}
