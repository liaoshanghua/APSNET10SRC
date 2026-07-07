using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>解析 CheckAccount 表单/JSON（兼容前端 Pwd 传 null 字符串）。</summary>
public static class LoginFormHelper
{
    public static bool TryReadLoginForm(
        HttpRequest request,
        string? bodyJson,
        out string accountName,
        out string pwd)
    {
        accountName = string.Empty;
        pwd = string.Empty;

        if (request.HasFormContentType && request.Form.ContainsKey("Account"))
        {
            accountName = request.Form["Account"].ToString();
            pwd = request.Form["Pwd"].ToString();
            return !string.IsNullOrWhiteSpace(accountName);
        }

        if (string.IsNullOrWhiteSpace(bodyJson) || !bodyJson.TrimStart().StartsWith('{'))
            return false;

        try
        {
            var jo = JObject.Parse(bodyJson);
            accountName = jo["Account"]?.ToString() ?? "";
            pwd = jo["Pwd"]?.ToString() ?? "";
            return !string.IsNullOrWhiteSpace(accountName);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsMissingPassword(string? pwd)
    {
        if (string.IsNullOrWhiteSpace(pwd))
            return true;

        var t = pwd.Trim();
        return t.Equals("null", StringComparison.OrdinalIgnoreCase)
               || t.Equals("undefined", StringComparison.OrdinalIgnoreCase);
    }
}
