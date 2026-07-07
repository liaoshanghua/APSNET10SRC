namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 前端 Vue Router history 模式（对齐旧 IIS Web.config VueRouter 规则）。
/// 刷新 /login 等路径时须在静态文件阶段回退 index.html，不能仅靠 MapFallback。
/// </summary>
internal static class ApsSpaFallbackExtensions
{
    /// <summary>
    /// 在 UseStaticFiles 之前：将 /login 等前端路由内部重写到 /index.html（保留 QueryString）。
    /// </summary>
    public static IApplicationBuilder UseApsSpaHistoryFallback(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (ApsSpaRouteMatcher.ShouldRewriteToIndexHtml(context))
                context.Request.Path = "/index.html";

            await next();
        });

    public static WebApplication MapApsSpaFallback(this WebApplication app)
    {
        app.MapFallback(async context =>
        {
            if (ApsSpaRouteMatcher.IsBackendApiPath(context.Request.Path)
                || ApsSpaRouteMatcher.IsStaticReportPath(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
            if (!File.Exists(indexPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync(
                    "前端未部署：请将前端 build 产物复制到 wwwroot（需含 index.html）。");
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexPath);
        });

        return app;
    }
}

internal static class ApsSpaRouteMatcher
{
    private static readonly string[] ApiPrefixes =
    [
        "/APSAPI",
        "/health"
    ];

    /// <summary>旧接口 /Login/CheckAccount、/Home/GetMenuVue、/user/info 等（须有子路径）。</summary>
    private static readonly string[] MvcApiRoots =
    [
        "/Login",
        "/Home",
        "/user"
    ];

    public static bool ShouldRewriteToIndexHtml(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            return false;

        var path = context.Request.Path.Value ?? "/";
        if (path.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IsBackendApiPath(context.Request.Path))
            return false;

        if (IsStaticReportPath(context.Request.Path))
            return false;

        // 仅无扩展名的前端路由回退 index.html；真实静态文件（含 APSReport/*.html）须原样访问
        if (!string.IsNullOrEmpty(Path.GetExtension(path)))
            return false;

        return true;
    }

    /// <summary>大屏看板静态页（旧站 EasyManufacture.Web/APSReport）。</summary>
    public static bool IsStaticReportPath(PathString path) =>
        path.StartsWithSegments("/APSReport", StringComparison.OrdinalIgnoreCase);

    public static bool IsBackendApiPath(PathString path)
    {
        foreach (var prefix in ApiPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var root in MvcApiRoots)
        {
            if (path.StartsWithSegments(root, StringComparison.OrdinalIgnoreCase, out var remaining)
                && remaining.HasValue
                && !string.IsNullOrEmpty(remaining.Value))
            {
                return true;
            }
        }

        return false;
    }
}
