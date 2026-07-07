using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Options;
using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>登录态写入/清除（Session、Cookie、Items），供 Login / APSAPI 共用。</summary>
public static class LoginStateHelper
{
    public static void ClearLoginState(HttpContext context, ICurrentUser currentUser, string? configuredAppCode)
    {
        context.Session.Clear();
        context.Session.Remove(AuthTokenResolver.SessionAccountKey);
        V_Dev_Account.SetDev_Account(context, null);
        currentUser.SetAccount(null);
        AuthCookieHelper.ClearLoginCookies(context.Response, context.Request, configuredAppCode);
    }

    public static string BuildCheckLoginJson(V_Dev_Account? devAccount, bool cleared)
    {
        if (devAccount != null)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                msg = "您好！" + devAccount.Name,
                result = true,
                href = "/login/login",
                account = devAccount.Account,
                password = devAccount.Pwd ?? "",
                OrganizeID = devAccount.OrganizeID.ToString()
            });
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            msg = cleared ? "已退出登录" : "当前用户未登录",
            result = false,
            href = "/login/login",
            account = "",
            password = "",
            OrganizeID = ""
        });
    }

    public static string ResolveAppCode(AppSettings? appSettings) =>
        string.IsNullOrWhiteSpace(appSettings?.AppCode) ? AppInfo.AppCode : appSettings.AppCode.Trim();
}
