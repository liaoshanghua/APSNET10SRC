namespace EasyManufacture.Domain.Options;

/// <summary>Net10 未覆盖的接口可转发至旧版 EasyManufacture.Web（与旧站并行部署时使用）。</summary>
public sealed class LegacyWebOptions
{
    public const string SectionName = "LegacyWeb";

    /// <summary>旧站根地址，例如 http://localhost:端口</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>为 true 且 BaseUrl 非空时，APSData 请求转发至旧站 /APSAPI/APSData。</summary>
    public bool ForwardApsData { get; set; }

    /// <summary>为 true 且 BaseUrl 非空时，未在 Net10 显式实现的 /APSAPI/{action} 转发至旧站。</summary>
    public bool ForwardAllApsApi { get; set; }
}
