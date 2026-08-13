using EasyManufacture.Domain.Options;
using EasyManufacture.Infrastructure.Scheduling.Jobs;
using EasyManufacture.Licence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyManufacture.Infrastructure.Scheduling;

/// <summary>
/// 对应旧版 Global.asax Application_Start 中按 AppInfo.PushType / AppInfo.WX 注册的 System.Timers.Timer。
/// </summary>
public sealed class GlobalScheduledTasksHostedService : BackgroundService
{
    private readonly ILogger<GlobalScheduledTasksHostedService> _logger;
    private readonly ScheduledTasksOptions _options;
    private readonly YrfExcelImportJob _yrfJob;
    private readonly IsgoPdfScheduledJob _isgoJob;
    private readonly WeChatWebhookPushJob _weChatJob;
    private readonly DingTalkWebhookPushJob _dingTalkJob;
    private readonly EkMoStartJob _ekJob;
    private readonly SapInterfaceSyncJob _sapJob;
    private readonly GlobalLegacyPushTypeJob _legacyJob;

    public GlobalScheduledTasksHostedService(
        ILogger<GlobalScheduledTasksHostedService> logger,
        IOptions<ScheduledTasksOptions> options,
        YrfExcelImportJob yrfJob,
        IsgoPdfScheduledJob isgoJob,
        WeChatWebhookPushJob weChatJob,
        DingTalkWebhookPushJob dingTalkJob,
        EkMoStartJob ekJob,
        SapInterfaceSyncJob sapJob,
        GlobalLegacyPushTypeJob legacyJob)
    {
        _logger = logger;
        _options = options.Value;
        _yrfJob = yrfJob;
        _isgoJob = isgoJob;
        _weChatJob = weChatJob;
        _dingTalkJob = dingTalkJob;
        _ekJob = ekJob;
        _sapJob = sapJob;
        _legacyJob = legacyJob;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("后台定时任务已禁用（ScheduledTasks:Enabled=false）");
            return;
        }

        var loops = new List<Task> { RegisterSapSyncLoop(stoppingToken) };

        var pushType = AppInfo.PushType?.Trim() ?? string.Empty;
        _logger.LogInformation("启动 Global 定时任务，PushType={PushType}, WX={Wx}, DingTalk={DingTalk}",
            pushType, AppInfo.WX, AppInfo.DingTalk);

        switch (pushType)
        {
            case "0":
                break;
            case "2":
                loops.Add(StubLoop("PushType-2", TimeSpan.FromMinutes(2), stoppingToken));
                break;
            case "3":
                loops.Add(StubLoop("Timer_Elapsed2", TimeSpan.FromMinutes(2), stoppingToken));
                break;
            case "4":
                loops.Add(StubLoop("Timer_Elapsed3-工艺卡", TimeSpan.FromHours(1), stoppingToken));
                break;
            case "6":
                loops.Add(StubLoop("Timer_Elapsed5", TimeSpan.FromMinutes(1), stoppingToken));
                loops.Add(StubLoop("Timer_SendPlanChange", TimeSpan.FromMinutes(1), stoppingToken));
                break;
            case "7":
                loops.Add(StubLoop("Timer_Elapsed6-物联网", TimeSpan.FromSeconds(30), stoppingToken));
                break;
            case "8":
                loops.Add(StubLoop("Timer_Elapsed7-SAP(EAST)", TimeSpan.FromHours(AppInfo.ERPSyncCycle), stoppingToken));
                loops.Add(StubLoop("Timer_Elapsed9-SAP订单", TimeSpan.FromMinutes(5), stoppingToken));
                break;
            case "9":
                loops.Add(StubLoop("Timer_Elapsed8-JGMES", TimeSpan.FromHours(AppInfo.ERPSyncCycle), stoppingToken));
                break;
            case "TPHK":
                loops.Add(StubLoop("Timer_ElapsedTPHK", TimeSpan.FromMinutes(1), stoppingToken));
                break;
            case "12":
                loops.Add(StubLoop("Timer_Elapsed12-模具", TimeSpan.FromMinutes(AppInfo.ERPSyncCycle), stoppingToken));
                break;
            case "ISGO":
                loops.Add(IsgoLoop(stoppingToken));
                break;
            case "OUSAI":
                loops.Add(StubLoop("OUSAI-WMS", TimeSpan.FromMinutes(20), stoppingToken));
                break;
            case "EK":
                loops.Add(EkScheduleLoop(stoppingToken));
                break;
            case "YRF":
                loops.Add(YrfLoop(stoppingToken));
                break;
            case "YS":
                // 与旧站 Web.config 一致：PushType=YS 时不注册 PushType 专用 Timer，仅 InterfaceSAP.Start
                _logger.LogInformation("PushType=YS：无专用定时器（与旧 Global.asax 行为一致），仅 SAP 接口检查");
                break;
            default:
                if (!string.IsNullOrEmpty(pushType))
                    loops.Add(StubLoop($"PushType-{pushType}", TimeSpan.FromHours(1), stoppingToken));
                break;
        }

        if (AppInfo.WX)
            loops.Add(WeChatLoop(stoppingToken));

        if (AppInfo.DingTalk)
            loops.Add(DingTalkLoop(stoppingToken));

        await Task.WhenAll(loops).ConfigureAwait(false);
    }

    private Task RegisterSapSyncLoop(CancellationToken ct)
    {
        var period = TimeSpan.FromMinutes(Math.Max(1, AppInfo.ERPSyncCycle));
        var guard = new OverlappingExecutionGuard();
        return PeriodicTaskLoop.RunAsync(
            "InterfaceSAP",
            period,
            runImmediately: true,
            guard,
            _ => _sapJob.RunAsync(ct),
            _logger,
            ct);
    }

    private Task YrfLoop(CancellationToken ct)
    {
        var guard = new OverlappingExecutionGuard();
        return PeriodicTaskLoop.RunAsync(
            "YRF-产能Excel",
            TimeSpan.FromMinutes(30),
            runImmediately: false,
            guard,
            _ =>
            {
                Task.Run(() => _yrfJob.Run(), ct);
                return Task.CompletedTask;
            },
            _logger,
            ct);
    }

    private Task IsgoLoop(CancellationToken ct)
    {
        var period = TimeSpan.FromMinutes(Math.Max(1, AppInfo.ERPSyncCycle));
        var guard1 = new OverlappingExecutionGuard();
        var guard2 = new OverlappingExecutionGuard();

        var loop1 = PeriodicTaskLoop.RunAsync(
            "ISGO-图纸扫描",
            period,
            runImmediately: true,
            guard1,
            _ =>
            {
                Task.Run(() => _isgoJob.ScanDrawingPdf(), ct);
                return Task.CompletedTask;
            },
            _logger,
            ct);

        var loop2 = PeriodicTaskLoop.RunAsync(
            "ISGO-PDF报表",
            period,
            runImmediately: true,
            guard2,
            _ =>
            {
                Task.Run(() => _isgoJob.ImportPdfReport(), ct);
                return Task.CompletedTask;
            },
            _logger,
            ct);

        return Task.WhenAll(loop1, loop2);
    }

    private Task WeChatLoop(CancellationToken ct)
    {
        var guard = new OverlappingExecutionGuard();
        return PeriodicTaskLoop.RunAsync(
            "企业微信机器人",
            TimeSpan.FromMinutes(1),
            runImmediately: false,
            guard,
            token => _weChatJob.RunAsync(token),
            _logger,
            ct);
    }

    private Task DingTalkLoop(CancellationToken ct)
    {
        var guard = new OverlappingExecutionGuard();
        return PeriodicTaskLoop.RunAsync(
            "钉钉群机器人",
            TimeSpan.FromMinutes(1),
            runImmediately: false,
            guard,
            token => _dingTalkJob.RunAsync(token),
            _logger,
            ct);
    }

    private Task EkScheduleLoop(CancellationToken ct)
    {
        var guard = new OverlappingExecutionGuard();
        var schedule = new EkDailyScheduleState();

        return PeriodicTaskLoop.RunAsync(
            "EK-MO开工",
            TimeSpan.FromSeconds(30),
            runImmediately: false,
            guard,
            _ =>
            {
                var now = DateTime.Now;
                if (now.Hour == 18 && now.Minute is >= 10 and <= 16 && !schedule.HasRunToday)
                {
                    schedule.HasRunToday = true;
                    return _ekJob.RunAsync(ct);
                }

                if (now.Hour >= 19)
                    schedule.HasRunToday = false;

                return Task.CompletedTask;
            },
            _logger,
            ct);
    }

    private sealed class EkDailyScheduleState
    {
        public bool HasRunToday { get; set; }
    }

    private Task StubLoop(string name, TimeSpan period, CancellationToken ct)
    {
        var guard = new OverlappingExecutionGuard();
        return PeriodicTaskLoop.RunAsync(
            name,
            period,
            runImmediately: true,
            guard,
            token => _legacyJob.RunAsync(name, token),
            _logger,
            ct);
    }
}
