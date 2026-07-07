using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>替代 System.Web.HttpContext.Current。</summary>
internal static class LicenceHttp
{
    public static HttpContext? Current => LicenceRuntime.Http.HttpContext;

    public static string? GetRequestValue(string key) =>
        Current?.Request.GetRequestValue(key);
}
