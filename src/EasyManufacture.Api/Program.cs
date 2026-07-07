using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Api.Middleware;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Options;
using EasyManufacture.Infrastructure;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Text;

// 部分 Windows Server 精简环境无 GBK(936)，注册 CodePages 供其它组件使用
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 与 APS-启动.bat 中 chcp 65001 一致，避免控制台中文显示为 ?
if (OperatingSystem.IsWindows())
{
    try
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.InputEncoding = Encoding.UTF8;
    }
    catch
    {
        // 无控制台宿主（计划任务最小化）时忽略
    }
}

ApsStartupGuard.Initialize();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<HostOptions>(options =>
    {
        // 后台定时任务异常不应导致整个 APS 进程退出
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });

    builder.WebHost.ConfigureKestrel(options => options.AllowSynchronousIO = true);
    builder.Services.AddApsResponseCompression(builder.Configuration);
    builder.Services.AddApsWwwrootPrecompress(builder.Configuration);

    CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN", true)
    {
        DateTimeFormat =
        {
            ShortDatePattern = "yyyy-MM-dd",
            FullDateTimePattern = "yyyy-MM-dd HH:mm:ss",
            LongTimePattern = "HH:mm:ss"
        }
    };

    builder.Services.AddSingleton<Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelProvider,
        EasyManufacture.Api.Infrastructure.ApsLegacyActionProvider>();
    builder.Services.AddControllers()
        .AddNewtonsoftJson()
        .AddControllersAsServices();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.Cookie.Name = ".EasyManufacture.Session";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.IdleTimeout = TimeSpan.FromDays(30);
    });
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.AddScoped<IRequestBodyAccessor, RequestBodyAccessor>();
    builder.Services.AddEasyManufactureInfrastructure(builder.Configuration);

    if (OperatingSystem.IsWindows())
        builder.Services.AddHostedService<ApsTrayIconHostedService>();

    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
        ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();

            if (builder.Environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(static origin =>
                {
                    if (string.IsNullOrEmpty(origin))
                        return false;
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        return false;
                    return uri.Host is "localhost" or "127.0.0.1";
                });
            }
            else if (corsOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins);
            }
            else
            {
                policy.SetIsOriginAllowed(_ => true);
            }
        });
    });

    var app = builder.Build();

    if (OperatingSystem.IsWindows())
    {
        ApsShutdownConfirmation.Register(app.Lifetime);
        app.Lifetime.ApplicationStopping.Register(ApsShutdownConfirmation.NotifyHostStopping);
        ApsEnvironmentBootstrap.Ensure(app.Configuration, app.Logger);
        ApsAutoStartInstaller.TryInstall(app.Configuration, app.Logger);
    }

    app.Services.UseEasyManufactureLegacyDbFactory();
    app.UseEasyManufactureLicence();

    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            ApsCrashLogger.WriteFatal(ex, $"request {context.Request.Method} {context.Request.Path}");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new { result = false, msg = ex.Message });
            }
        }
    });

    ValidateSsoConfiguration(app.Logger);

    // 须在写响应的中间件之前（对齐 IIS 动态压缩）
    app.UseApsResponseCompression(app.Configuration);

    app.UseCors();
    app.UseSession();

    app.Use((RequestDelegate next) =>
    {
        return async context =>
        {
            var bodyAccessor = context.RequestServices.GetRequiredService<IRequestBodyAccessor>();
            var mw = new RequestBodyMiddleware(next);
            await mw.InvokeAsync(context, bodyAccessor);
        };
    });
    app.Use((RequestDelegate next) =>
    {
        return async context =>
        {
            var register = context.RequestServices.GetRequiredService<JDRegister>();
            var mw = new LicenceSecurityMiddleware(next);
            await mw.InvokeAsync(context, register);
        };
    });
    app.Use((RequestDelegate next) =>
    {
        return async context =>
        {
            var currentUser = context.RequestServices.GetRequiredService<ICurrentUser>();
            var appSettings = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>();
            var mw = new LoginPageClearMiddleware(next, appSettings);
            await mw.InvokeAsync(context, currentUser);
        };
    });
    app.Use((RequestDelegate next) =>
    {
        return async context =>
        {
            var currentUser = context.RequestServices.GetRequiredService<ICurrentUser>();
            var accountService = context.RequestServices.GetRequiredService<IAccountService>();
            var bodyAccessor = context.RequestServices.GetRequiredService<IRequestBodyAccessor>();
            var appSettings = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>();
            var mw = new AccountAuthenticationMiddleware(next, appSettings);
            await mw.InvokeAsync(context, currentUser, accountService, bodyAccessor);
        };
    });

    // 大 JS/CSS：先尝试 .br/.gz，再由 ResponseCompression 动态压缩未预压文件
    app.UseApsPreCompressedStaticFiles(app.Configuration);
    // 对齐旧 IIS rewrite：/login 刷新时回退 index.html（须在 UseStaticFiles 之前）
    app.UseApsSpaHistoryFallback();
    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path.Value ?? "";
            if (path.Contains("/assets/", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
        }
    });

    app.MapGet("/APSAPI/Ping", () => Results.Json(new { ok = true, framework = "net10.0", controller = "APSAPI" }));
    app.MapControllers();
    app.MapGet("/health", () => Results.Redirect("/APSAPI/Ping"));

    // Vue Router history 模式：未匹配的前端路由回退 index.html（避免登录后 404）
    app.MapApsSpaFallback();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        app.Logger.LogInformation("APS 已就绪，Kestrel 开始监听");
    });

    var urls = app.Configuration["Kestrel:Endpoints:Http:Url"] ?? "http://0.0.0.0:9999";
    if (app.Configuration.GetValue("ResponseCompression:Enabled", true))
        app.Logger.LogInformation("HTTP 压缩已启用：预压缩 .br/.gz 优先，动态 gzip/brotli 兜底");
    app.Logger.LogInformation("APS 启动中，监听 {Urls}", urls);
    Console.WriteLine();
    Console.WriteLine("============================================");
    Console.WriteLine("  这是 APS 系统的启动程序，请不要关闭");
    Console.WriteLine("============================================");
    Console.WriteLine("  监听地址: {0}", urls);
    Console.WriteLine("  健康检查: /APSAPI/Ping");
    Console.WriteLine("============================================");
    Console.WriteLine();

    app.Run();
}
catch (Exception ex)
{
    ApsStartupGuard.ReportFatal(ex);
    if (ex.ToString().Contains("appsettings.json", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("appsettings.json 格式错误：Windows 路径和 SQL 连接串里的 \\ 必须写成 \\\\");
        Console.Error.WriteLine("示例: D:\\\\共享\\\\目录  或  Data Source=.\\\\SQLEXPRESS");
        Console.Error.WriteLine("详见 appsettings.配置说明.txt");
    }
    else if (IsPortAlreadyInUse(ex))
    {
        var port = TryReadConfiguredPort();
        Console.Error.WriteLine();
        Console.Error.WriteLine($"端口 {port} 已被占用 (10048)，无法启动 APS。");
        Console.Error.WriteLine("  常见原因：上一次 APS 未退出（托盘/后台仍有 APS.exe）");
        Console.Error.WriteLine("  处理：");
        Console.Error.WriteLine($"    1) 管理员 CMD: netstat -ano | findstr \":{port}\"");
        Console.Error.WriteLine("       找到 LISTENING 行的 PID，再: taskkill /PID <pid> /F");
        Console.Error.WriteLine("    2) 或任务管理器结束 APS.exe / dotnet.exe");
        Console.Error.WriteLine("    3) 或改 appsettings.json 中 Kestrel 端口后重启");
        Console.Error.WriteLine("  也可运行 APS-诊断.bat 查看端口占用。");
    }
    else if (IsPortBindAccessDenied(ex))
    {
        var port = TryReadConfiguredPort();
        Console.Error.WriteLine();
        Console.Error.WriteLine($"端口绑定失败 (10013)：无法监听 {port}，常见原因：");
        Console.Error.WriteLine("  1) 端口被其它程序占用（含旧 APS/IIS）");
        Console.Error.WriteLine("  2) Hyper-V/Docker 保留了该端口段（Windows 动态端口排除）");
        Console.Error.WriteLine("  3) http.sys URL 保留冲突");
        Console.Error.WriteLine("处理：运行 APS-诊断.bat 查看占用与保留端口；或改 appsettings.json：");
        Console.Error.WriteLine(@"  ""Kestrel"": { ""Endpoints"": { ""Http"": { ""Url"": ""http://0.0.0.0:9996"" } } }");
        Console.Error.WriteLine("改端口后需同步防火墙/前端代理地址。");
    }
    Environment.ExitCode = 1;
    throw;
}

static bool IsPortAlreadyInUse(Exception ex)
{
    for (var e = ex; e != null; e = e.InnerException)
    {
        if (e is System.Net.Sockets.SocketException se &&
            (se.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse || se.ErrorCode == 10048))
            return true;
        if (e is Microsoft.AspNetCore.Connections.AddressInUseException)
            return true;
        if (e.Message.Contains("10048", StringComparison.Ordinal)
            || e.Message.Contains("AddressInUse", StringComparison.OrdinalIgnoreCase))
            return true;
    }
    return false;
}

static bool IsPortBindAccessDenied(Exception ex)
{
    for (var e = ex; e != null; e = e.InnerException)
    {
        if (e is System.Net.Sockets.SocketException se &&
            (se.SocketErrorCode == System.Net.Sockets.SocketError.AccessDenied || se.ErrorCode == 10013))
            return true;
        if (e.Message.Contains("10013", StringComparison.Ordinal))
            return true;
    }
    return false;
}

static string TryReadConfiguredPort()
{
    try
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var url = cfg["Kestrel:Endpoints:Http:Url"];
        if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Port.ToString();
    }
    catch { }
    return "9999";
}

static void ValidateSsoConfiguration(ILogger logger)
{
    var sso = EasyManufacture.Licence.AppInfo.SSOUrl;
    if (string.IsNullOrWhiteSpace(sso))
    {
        logger.LogInformation("App:SSOUrl 未配置；RequestSSOUrl / APSRequestSSOUrl 需在 appsettings 中设置，格式：项目名‖回调地址‖密钥");
        return;
    }

    var parts = sso.Split('‖').Select(p => p.Trim()).ToArray();
    if (parts.Length < 3)
        logger.LogWarning("App:SSOUrl 格式应为：EK或XingHe‖http://地址‖密钥（三段，分隔符为 ‖）");
    else
        logger.LogInformation("SSO 已配置项目：{Project}，地址：{Url}", parts[0], parts[1]);
}
