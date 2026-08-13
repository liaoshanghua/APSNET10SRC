using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Data;
using EasyManufacture.Infrastructure.Data;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Infrastructure.Scheduling;
using EasyManufacture.Infrastructure.Scheduling.Jobs;
using EasyManufacture.Infrastructure.Services;
using EasyManufacture.Licence;
using EasyManufacture.Domain.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;

namespace EasyManufacture.Infrastructure;

/// <summary>Infrastructure 层 DI 注册（DbContext、Legacy 引擎、定时任务、转发器）。</summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册数据访问、<see cref="ApsCoreEngine"/>、配置/保存/APS 服务及 Global.asax 等价后台任务。
    /// </summary>
    public static IServiceCollection AddEasyManufactureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- 配置绑定 ---
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<ScheduledTasksOptions>(configuration.GetSection(ScheduledTasksOptions.SectionName));
        services.Configure<LegacyWebOptions>(configuration.GetSection(LegacyWebOptions.SectionName));

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // --- 旧站 HTTP 转发（LegacyWeb:ForwardApsData / ForwardAllApsApi）---
        services.AddHttpClient(nameof(WeChatWebhookPushJob));
        services.AddHttpClient(nameof(DingTalkWebhookPushJob));
        services.AddHttpClient(nameof(GlobalLegacyPushTypeJob));
        services.AddHttpClient(nameof(LegacyApsDataForwarder));
        services.AddHttpClient(nameof(LegacyApsApiForwarder));
        services.AddSingleton<LegacyApsDataForwarder>();
        services.AddSingleton<LegacyApsApiForwarder>();
        services.AddScoped<ApsApiLegacyDispatcher>();

        // --- Global.asax 定时任务 Job ---
        services.AddSingleton<YrfExcelImportJob>();
        services.AddSingleton<IsgoPdfScheduledJob>();
        services.AddSingleton<WeChatWebhookPushJob>();
        services.AddSingleton<DingTalkWebhookPushJob>();
        services.AddSingleton<EkMoStartJob>();
        services.AddSingleton<SapInterfaceSyncJob>();
        services.AddSingleton<GlobalLegacyPushTypeJob>();

        services.AddHostedService<SapConnectionBootstrapHostedService>();
        services.AddHostedService<DatabaseSchemaUpgradeHostedService>();
        services.AddHostedService<GlobalScheduledTasksHostedService>();

        // --- EF Core + Dapper ---
        services.AddDbContext<ManufactureDbContext>((sp, options) =>
        {
            var db = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseSettings>>().Value;
            options.UseSqlServer(SqlConnectionStringHelper.Normalize(db.MSSQLConnectionString));
        });

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<DapperSqlHelper>();

        // --- APS Legacy 引擎（LegacyApi + LegacyCore partial）---
        services.AddScoped<ApsCoreEngine>();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<ISaveDataService, SaveDataService>();
        services.AddScoped<IApsDataService, ApsDataService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<LicenceLoginService>();
        services.AddScoped<LoginSessionEnricher>();
        services.AddEasyManufactureLicence(configuration);

        services.AddOptions();
        services.AddSingleton(sp =>
        {
            LegacyRuntime.Configure(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseSettings>>(),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>());
            return LegacyRuntime.App;
        });

        return services;
    }

    /// <summary>启动后初始化 LegacyDbFactory / LegacyRuntime（SqlHelper、MSSQLCore 依赖）。</summary>
    public static void UseEasyManufactureLegacyDbFactory(this IServiceProvider services)
    {
        LegacyDbFactory.Configure(services);
        LegacyRuntime.Configure(
            services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseSettings>>(),
            services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>());
    }
}
