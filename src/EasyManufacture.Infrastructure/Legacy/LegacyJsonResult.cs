namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容旧版 System.Web.Mvc.JsonResult。</summary>
public sealed class LegacyJsonResult
{
    public object? Data { get; set; }
}
