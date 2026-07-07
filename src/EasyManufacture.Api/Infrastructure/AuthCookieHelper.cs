using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>与旧站 CheckAccount / BaseController 一致的登录 Cookie 名称与选项。</summary>
public static class AuthCookieHelper
{
    public static readonly string[] TokenCookieNames = ["token", "vue_admin_template_token"];

    public static void AppendLoginCookies(HttpResponse response, HttpRequest request, string appCode, string token)
    {
        var options = CreateCookieOptions(request);

        if (!string.IsNullOrWhiteSpace(appCode))
            response.Cookies.Append(appCode.Trim(), token, options);

        // 与旧 BaseController.OnAuthorization 一致：每次鉴权成功也会刷新 token Cookie
        foreach (var name in TokenCookieNames)
            response.Cookies.Append(name, token, options);

        var legacyCode = AppInfo.AppCode;
        if (!string.IsNullOrWhiteSpace(legacyCode)
            && !string.Equals(legacyCode, appCode, StringComparison.OrdinalIgnoreCase))
            response.Cookies.Append(legacyCode.Trim(), token, options);
    }

    public static CookieOptions CreateCookieOptions(HttpRequest request) => new()
    {
        Path = "/",
        HttpOnly = false,
        // localhost 不同端口属 same-site，Lax 即可在 withCredentials 请求中携带
        SameSite = SameSiteMode.Lax,
        Secure = request.IsHttps,
        Expires = DateTimeOffset.UtcNow.AddDays(30)
    };

    public static void ClearLoginCookies(HttpResponse response, HttpRequest request, string? configuredAppCode)
    {
        var expired = new CookieOptions
        {
            Path = "/",
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = request.IsHttps,
            Expires = DateTimeOffset.UnixEpoch
        };

        foreach (var name in TokenCookieNames)
            response.Cookies.Delete(name, expired);

        if (!string.IsNullOrWhiteSpace(configuredAppCode))
            response.Cookies.Delete(configuredAppCode.Trim(), expired);

        var legacyCode = AppInfo.AppCode;
        if (!string.IsNullOrWhiteSpace(legacyCode)
            && !string.Equals(legacyCode, configuredAppCode, StringComparison.OrdinalIgnoreCase))
            response.Cookies.Delete(legacyCode.Trim(), expired);

        foreach (var legacy in new[] { "ISGO", "E01" })
            response.Cookies.Delete(legacy, expired);
    }
}
