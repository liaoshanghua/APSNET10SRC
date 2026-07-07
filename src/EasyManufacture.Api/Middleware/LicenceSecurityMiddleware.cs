using EasyManufacture.Licence;
using System.Reflection;
using System.Text.Json;

namespace EasyManufacture.Api.Middleware;

/// <summary>IP 锁定与高频访问检测（对应旧版 AppInfo.IsSafe / CheckLogCount）。</summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public sealed class LicenceSecurityMiddleware
{
    private readonly RequestDelegate _next;

    public LicenceSecurityMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, JDRegister register)
    {
        if (!register.IsRegister && !IsLicenceBypassPath(context.Request.Path) && !IsLoopbackRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { result = false, msg = "系统未注册或授权已过期" }));
            return;
        }

        if (AppInfo.IsSafe)
        {
            if (AppInfo.IsLock())
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { result = false, msg = "IP已被锁定，请稍后再试" }));
                return;
            }
            AppInfo.CheckLogCount();
        }

        await _next(context);
    }

    /// <summary>未注册时也须可访问：读取机器码、写入 register.ini。</summary>
    private static bool IsLicenceBypassPath(PathString path)
    {
        return path.StartsWithSegments("/APSAPI/SeRegister", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/APSAPI/GetRegister", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>前端与后台同机、经 localhost 访问时免授权（与旧版 JDRegister.Check 一致）。</summary>
    private static bool IsLoopbackRequest(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote != null && System.Net.IPAddress.IsLoopback(remote))
            return true;

        var ip = remote?.MapToIPv4().ToString()
                 ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return ip is "127.0.0.1" or "::1" or "0:0:0:0:0:0:0:1";
    }
}
