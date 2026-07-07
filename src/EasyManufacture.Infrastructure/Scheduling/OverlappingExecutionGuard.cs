namespace EasyManufacture.Infrastructure.Scheduling;

/// <summary>防止同一任务在上一次未完成时再次进入（对应 Global.asax 中 YRFisRunning / WXisRunning 等标志）。</summary>
internal sealed class OverlappingExecutionGuard
{
    private int _running;

    public bool TryEnter() => Interlocked.CompareExchange(ref _running, 1, 0) == 0;

    public void Exit() => Interlocked.Exchange(ref _running, 0);
}
