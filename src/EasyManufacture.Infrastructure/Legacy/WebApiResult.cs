namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>与旧版 EasyManufacture.Core.WebApiResult 一致，供 InterfaceSAP 等使用。</summary>
public class WebApiResult
{
    public bool Result { get; set; }
    public string Msg { get; set; } = "";
    public object? Data { get; set; }
}
