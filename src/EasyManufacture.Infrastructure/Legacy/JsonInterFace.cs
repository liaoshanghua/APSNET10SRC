namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>json 接口（自 EasyManufacture.Core/JsonInterFace.cs 迁入）。</summary>
public class JsonInterFace
{
    public string code { get; set; } = "";
    public string message { get; set; } = "";
    public bool Result => code == "200";
}

public class WMSInterFace
{
    public bool? Data { get; set; }
    public string Code { get; set; } = "";
    public string Msg { get; set; } = "";
}
