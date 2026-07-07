using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容 System.Web.HttpRequest 索引器访问（Query/Form/JSON Body）。</summary>
public static class ApsLegacyHttpRequestHelper
{
    public const string BodyJsonItemKey = "ApsBodyJson";

    public static void SetBodyJson(HttpContext context, string? bodyJson) =>
        context.Items[BodyJsonItemKey] = bodyJson ?? string.Empty;

    public static string? GetRequestValue(this HttpRequest? request, string key)
    {
        if (request == null || string.IsNullOrEmpty(key))
            return null;

        if (request.Query.TryGetValue(key, out var queryValue))
            return queryValue.ToString();

        if (request.HasFormContentType && request.Form.TryGetValue(key, out var formValue))
            return formValue.ToString();

        return GetJsonBodyValue(request.HttpContext, key);
    }

    public static string? GetQueryValue(this QueryString queryString, string key)
    {
        var parsed = QueryHelpers.ParseQuery(queryString.Value ?? "");
        return parsed.TryGetValue(key, out var value) ? value.ToString() : null;
    }

    private static string? GetJsonBodyValue(HttpContext? context, string key)
    {
        if (context?.Items.TryGetValue(BodyJsonItemKey, out var obj) != true || obj is not string body)
            return null;
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var text = body.Trim();
            if (text.StartsWith('['))
            {
                var arr = JArray.Parse(text);
                if (arr.Count == 0)
                    return null;
                if (arr[0] is JObject jo0)
                    return GetPropertyValue(jo0, key);
                return null;
            }

            if (text.StartsWith('{'))
                return GetPropertyValue(JObject.Parse(text), key);
        }
        catch
        {
            // ignore malformed body
        }

        return null;
    }

    private static string? GetPropertyValue(JObject jo, string key)
    {
        if (jo.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var prop)
            && prop.Type != JTokenType.Null)
            return prop.ToString();
        return null;
    }
}
