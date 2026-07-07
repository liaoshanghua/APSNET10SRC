using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EasyManufacture.Licence;

/// <summary>供 AppInfo、SystemLog、JDRegister 等静态访问 ASP.NET Core 运行时。</summary>
public static class LicenceRuntime
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static IHttpContextAccessor Http { get; private set; } = null!;
    public static IHostEnvironment Environment { get; private set; } = null!;
    public static string SqlConnectionString { get; private set; } = string.Empty;

    public static void Configure(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        string sqlConnectionString)
    {
        Configuration = configuration;
        Http = httpContextAccessor;
        Environment = environment;
        SqlConnectionString = sqlConnectionString;
    }

    public static string ClientIpAddress
    {
        get
        {
            var ctx = Http.HttpContext;
            if (ctx == null) return "127.0.0.1";
            return ctx.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                   ?? ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                   ?? "127.0.0.1";
        }
    }

    /// <summary>当前请求是否来自本机回环地址（对应旧版 UserHostAddress 127.0.0.1 / ::1）。</summary>
    public static bool IsLoopbackClient
    {
        get
        {
            var ctx = Http.HttpContext;
            if (ctx == null) return false;

            var remote = ctx.Connection.RemoteIpAddress;
            if (remote != null && System.Net.IPAddress.IsLoopback(remote))
                return true;

            var ip = ClientIpAddress;
            return ip is "127.0.0.1" or "::1" or "0:0:0:0:0:0:0:1";
        }
    }

    public static string MapContentPath(string relativePath)
    {
        var path = relativePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Environment.ContentRootPath, path);
    }
}
