using EasyManufacture.Application.Security;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 与旧 <c>BaseController.OnAuthorization</c> 相同的凭证来源与顺序（并补充 Vue 常用 token Cookie / Authorization）。
/// </summary>
public static class AuthTokenResolver
{
    public const string SessionAccountKey = "Dev_Account_Account";

    private static readonly string[] HeaderNames = ["token", "Token", "X-Token"];

    private static readonly string[] LegacyAppCodeCookieNames = ["ISGO", "E01"];

    /// <summary>凭证来源（顺序与旧站 OnAuthorization 一致）。</summary>
    public enum CredentialSource
    {
        AppCodeCookie,
        FormCurrentAccount,
        BodyCurrentAccount,
        QueryCurrentAccount,
        QueryUserName,
        HeaderToken,
        TokenCookie
    }

    public readonly record struct Credential(string Raw, CredentialSource Source);

    /// <summary>
    /// 收集候选凭证。跨域 Vue（如 localhost:9528）通常只带 Header token，故优先 Header / token Cookie，
    /// 再按旧站顺序尝试 AppCode Cookie、Form、Body、Query。
    /// </summary>
    public static IReadOnlyList<Credential> CollectCredentials(
        HttpRequest request,
        string? bodyJson,
        string configuredAppCode)
    {
        var list = new List<Credential>();

        AddHeaderCredentials(request, list);

        foreach (var value in ReadTokenCookies(request, configuredAppCode))
            list.Add(new Credential(value, CredentialSource.TokenCookie));

        foreach (var value in ReadAppCodeCookies(request, configuredAppCode))
            list.Add(new Credential(value, CredentialSource.AppCodeCookie));

        var fromForm = ReadCurrentAccountFromForm(request);
        if (!string.IsNullOrWhiteSpace(fromForm))
            list.Add(new Credential(fromForm, CredentialSource.FormCurrentAccount));

        var fromBody = ReadCurrentAccountFromBody(bodyJson);
        if (!string.IsNullOrWhiteSpace(fromBody))
            list.Add(new Credential(fromBody, CredentialSource.BodyCurrentAccount));

        if (request.Query.TryGetValue("CrurentAccount", out var q1))
        {
            var v = q1.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(v))
                list.Add(new Credential(v, CredentialSource.QueryCurrentAccount));
        }

        if (request.Query.TryGetValue("user_name", out var q2))
        {
            var v = q2.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(v))
                list.Add(new Credential(v, CredentialSource.QueryUserName));
        }

        if (request.Query.TryGetValue("token", out var qToken) ||
            request.Query.TryGetValue("Token", out qToken))
        {
            var v = qToken.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(v))
                list.Add(new Credential(v, CredentialSource.HeaderToken));
        }

        return list;
    }

    /// <summary>请求是否携带任意登录凭证（用于判断是否清除 Session）。</summary>
    public static bool HasAnyCredential(HttpRequest request, string? bodyJson, string configuredAppCode) =>
        CollectCredentials(request, bodyJson, configuredAppCode).Count > 0;

    private static void AddHeaderCredentials(HttpRequest request, List<Credential> list)
    {
        foreach (var name in HeaderNames)
        {
            if (request.Headers.TryGetValue(name, out var values))
            {
                var v = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(v))
                    list.Add(new Credential(v, CredentialSource.HeaderToken));
            }
        }

        if (request.Headers.TryGetValue("Authorization", out var authValues))
        {
            var auth = authValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(auth) &&
                auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var bearer = auth["Bearer ".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(bearer))
                    list.Add(new Credential(bearer, CredentialSource.HeaderToken));
            }
        }
    }

    private static IEnumerable<string> ReadAppCodeCookies(HttpRequest request, string configuredAppCode)
    {
        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredAppCode))
            names.Add(configuredAppCode.Trim());

        var appInfoCode = AppInfo.AppCode;
        if (!string.IsNullOrWhiteSpace(appInfoCode) && !names.Contains(appInfoCode, StringComparer.Ordinal))
            names.Add(appInfoCode);

        foreach (var legacy in LegacyAppCodeCookieNames)
        {
            if (!names.Contains(legacy, StringComparer.Ordinal))
                names.Add(legacy);
        }

        foreach (var name in names)
        {
            if (request.Cookies.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                yield return DecodeCookieValue(value);
        }
    }

    private static IEnumerable<string> ReadTokenCookies(HttpRequest request, string configuredAppCode)
    {
        foreach (var name in AuthCookieHelper.TokenCookieNames)
        {
            if (request.Cookies.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                yield return DecodeCookieValue(value);
        }
    }

    /// <summary>旧站 Request.Form[0]：第一个表单字段里可能是 JSON，含 CurrentAccount。</summary>
    private static string? ReadCurrentAccountFromForm(HttpRequest request)
    {
        if (!request.HasFormContentType || request.Form.Count == 0)
            return null;

        foreach (var key in request.Form.Keys)
        {
            var text = request.Form[key].ToString();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var fromJson = TryParseCurrentAccountJson(text);
            if (!string.IsNullOrWhiteSpace(fromJson))
                return fromJson;
        }

        return null;
    }

    private static string? ReadCurrentAccountFromBody(string? bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson))
            return null;

        try
        {
            var trimmed = bodyJson.Trim();
            if (trimmed.StartsWith('['))
            {
                var arr = JArray.Parse(trimmed);
                if (arr.Count > 0 && arr[0] is JObject row && row["CurrentAccount"] != null)
                    return row["CurrentAccount"]!.ToString();
            }

            if (trimmed.StartsWith('{'))
            {
                var jo = JObject.Parse(trimmed);
                if (jo["CurrentAccount"] != null)
                    return jo["CurrentAccount"]!.ToString();
            }
        }
        catch
        {
            /* ignore */
        }

        return null;
    }

    private static string? TryParseCurrentAccountJson(string text)
    {
        try
        {
            if (!text.TrimStart().StartsWith('{') && !text.TrimStart().StartsWith('['))
                return null;

            var token = Newtonsoft.Json.JsonConvert.DeserializeObject(text);
            switch (token)
            {
                case JObject jo when jo["CurrentAccount"] != null:
                    return jo["CurrentAccount"]!.ToString();
                case JArray arr when arr.Count > 0 && arr[0] is JObject row && row["CurrentAccount"] != null:
                    return row["CurrentAccount"]!.ToString();
            }
        }
        catch
        {
            /* ignore */
        }

        return null;
    }

    /// <summary>DES 解密（与旧 StringHelper.DESDecrypt 一致）；<paramref name="skipDecrypt"/> 时按明文账号处理（user_name）。</summary>
    public static string? ResolveAccountName(string? raw, bool skipDecrypt = false)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var token = DecodeLegacyValue(raw.Trim().Trim('"'));
        if (skipDecrypt)
            return token.Trim().TrimEnd('.');

        var decrypted = DesCrypto.Decrypt(token);
        if (string.IsNullOrWhiteSpace(decrypted))
            return null;

        if (string.Equals(decrypted, token, StringComparison.OrdinalIgnoreCase) && LooksLikeCipherHex(token))
            return null;

        return decrypted.Trim().TrimEnd('.');
    }

    private static string DecodeLegacyValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        value = DecodeCookieValue(value);

        // 旧前端 setCookie 使用 escape()，与 encodeURIComponent 不同；十六进制 token 通常无影响
        if (value.Contains('%', StringComparison.Ordinal))
        {
            try
            {
                value = Uri.UnescapeDataString(value.Replace('+', ' '));
            }
            catch
            {
                /* ignore */
            }
        }

        return value;
    }

    private static string DecodeCookieValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    private static bool LooksLikeCipherHex(string value) =>
        value.Length >= 16 && value.Length % 2 == 0 && value.All(static c =>
            c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f');
}
