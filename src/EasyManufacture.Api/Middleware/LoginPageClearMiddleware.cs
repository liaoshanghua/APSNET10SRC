using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace EasyManufacture.Api.Middleware;

/// <summary>
/// 对齐旧站 <c>LoginController.Login()</c>：进入登录页 GET 时清空 Session 与登录 Cookie，
/// 避免 A 退出后仅跳转登录页、Cookie 仍保留导致再登录 B 时仍显示 A。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public sealed class LoginPageClearMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public LoginPageClearMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        if (ShouldClearLoginState(context))
        {
            LoginStateHelper.ClearLoginState(
                context,
                currentUser,
                LoginStateHelper.ResolveAppCode(_appSettings));
        }

        return _next(context);
    }

    private static bool ShouldClearLoginState(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
            return false;

        var path = context.Request.Path.Value ?? string.Empty;
        return path.Equals("/login/login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Login/Login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/login", StringComparison.OrdinalIgnoreCase);
    }
}
