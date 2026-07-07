namespace EasyManufacture.Domain.Config;

public sealed class ElementTableInput
{
    public int ID { get; set; }
    public string? Index { get; set; }
    public string? ConfigStartWeek { get; set; }
}

public sealed class ElementTableOutput
{
    public ElementTableOutput()
    {
        visible = true;
    }

    public string? label { get; set; }
    public string? prop { get; set; }
    public string? propName { get; set; }
    public string? width { get; set; }
    public string? fix { get; set; }
    public string? sortable { get; set; }
    public object? align { get; set; }
    public string? render { get; set; }
    public string? icon { get; set; }
    public string? button { get; set; }
    public object? active { get; set; }
    public string? component { get; set; }
    public bool isEdit { get; set; }
    public bool isMerge { get; set; }
    public string? dicID { get; set; }
    public bool isLook { get; set; }
    public bool routerName { get; set; }
    public bool visible { get; set; }
}

public sealed class SearchForm
{
    public string? placeholder { get; set; }
    public string? label { get; set; }
    public string? prop { get; set; }
    public string? dicID { get; set; }
    public object? value { get; set; }
    public string? width { get; set; }
    public string? type { get; set; }
    public bool multiple { get; set; }
    public object? options { get; set; }
}

public sealed class DictionaryFieldRow
{
    public long DictionaryID { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? Width { get; set; }
    public string? fix { get; set; }
    public string? sortable { get; set; }
    public string? align { get; set; }
    public string? Formatter { get; set; }
    public string? icon { get; set; }
    public string? button { get; set; }
    public string? active { get; set; }
    public string? component { get; set; }
    public bool? IsEdit { get; set; }
    public bool? isMerge { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsQuery { get; set; }
    public string? ControlType { get; set; }
    public string? DataSourceID { get; set; }
    public string? DefaultValue { get; set; }
    public int? FieldIndex { get; set; }
    public bool RouterName { get; set; }
    public string? ObjectName { get; set; }
    public string? TabelName { get; set; }
    public string? MenuCode { get; set; }
    public string? Region { get; set; }
}
