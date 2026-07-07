using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>解析 APS 接口请求 JSON（兼容 dicID 大小写、数组首项、Query/Form）。</summary>
public static class ApsRequestJson
{
    public static JObject? ParseJObject(string? bodyJson, HttpRequest? request = null)
    {
        var fromBody = ParseBodyJson(bodyJson);
        if (fromBody != null && ContainsDicId(fromBody))
            return fromBody;

        var fromForm = ParseFromForm(request);
        if (fromForm != null)
            return MergeJObject(fromBody, fromForm);

        var fromQuery = ParseFromQuery(request);
        if (fromQuery != null)
            return MergeJObject(fromBody ?? fromForm, fromQuery);

        return fromBody;
    }

    public static bool TryGetDicId(JToken? token, out int dicId)
    {
        dicId = 0;
        if (token == null)
            return false;

        if (token is JObject jo)
        {
            foreach (var key in new[] { "dicID", "DicID", "DICID", "DictionaryID", "id", "ID" })
            {
                if (!jo.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var prop))
                    continue;
                if (TryParseDicIdToken(prop, out dicId))
                    return true;
            }
            return false;
        }

        if (token is JArray arr && arr.Count > 0)
            return TryGetDicId(arr[0], out dicId);

        return false;
    }

    private static JObject? ParseBodyJson(string? bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson))
            return null;

        var text = bodyJson.Trim();
        if (text.Length == 0)
            return null;

        try
        {
            if (text.StartsWith('['))
            {
                var arr = JArray.Parse(text);
                if (arr.Count == 0)
                    return null;
                return arr[0] as JObject ?? JObject.FromObject(arr[0]);
            }

            if (text.StartsWith('{'))
                return JObject.Parse(text);
        }
        catch
        {
            /* 非 JSON，尝试 form 风格 a=1&dicID=2 */
            if (text.Contains("dicID", StringComparison.OrdinalIgnoreCase))
            {
                var jo = new JObject();
                foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2)
                        jo[kv[0]] = Uri.UnescapeDataString(kv[1]);
                }
                if (jo.Count > 0)
                    return jo;
            }
        }

        return null;
    }

    private static JObject? ParseFromForm(HttpRequest? request)
    {
        if (request == null || !request.HasFormContentType || request.Form.Count == 0)
            return null;

        var jo = new JObject();
        foreach (var key in request.Form.Keys)
        {
            if (string.IsNullOrEmpty(key))
                continue;
            jo[key] = request.Form[key].ToString();
        }

        return jo.Count > 0 ? jo : null;
    }

    private static JObject? ParseFromQuery(HttpRequest? request)
    {
        if (request == null || request.Query.Count == 0)
            return null;

        var jo = new JObject();
        foreach (var key in request.Query.Keys)
        {
            if (string.IsNullOrEmpty(key))
                continue;
            jo[key] = request.Query[key].ToString();
        }

        return jo.Count > 0 ? jo : null;
    }

    private static JObject MergeJObject(JObject? baseObj, JObject overlay)
    {
        if (baseObj == null)
            return overlay;
        baseObj.Merge(overlay, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Ignore
        });
        return baseObj;
    }

    private static bool ContainsDicId(JObject jo) => TryGetDicId(jo, out _);

    private static bool TryParseDicIdToken(JToken? token, out int dicId)
    {
        dicId = 0;
        if (token == null || token.Type == JTokenType.Null)
            return false;

        var text = token.ToString().Trim();
        return int.TryParse(text, out dicId) && dicId > 0;
    }
}
