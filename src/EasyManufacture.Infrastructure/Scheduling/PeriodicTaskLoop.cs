using Microsoft.Extensions.Logging;

namespace EasyManufacture.Infrastructure.Scheduling;

/// <summary>
/// 基于 <see cref="PeriodicTimer"/> 的后台循环，替代旧 Global.asax 中的 <c>System.Timers.Timer</c>。
/// </summary>
internal static class PeriodicTaskLoop
{
    /// <summary>
    /// 按固定周期执行任务；<paramref name="runImmediately"/> 为 true 时启动后先执行一次。
    /// </summary>
    /// <param name="guard">防止上一次未结束时重叠执行（对应旧 Timer 里 isRunning 标志）。</param>
    /// <param name="cancellationToken">应用关闭时取消；<see cref="OperationCanceledException"/> 视为正常退出。</param>
    public static async Task RunAsync(
        string taskName,
        TimeSpan period,
        bool runImmediately,
        OverlappingExecutionGuard guard,
        Func<CancellationToken, Task> work,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (runImmediately)
                await ExecuteOnceAsync(taskName, guard, work, logger, cancellationToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(period);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                await ExecuteOnceAsync(taskName, guard, work, logger, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止调试或 dotnet run 结束时 WaitForNextTickAsync 会抛此异常，属正常行为
            logger.LogDebug("定时任务 {Task} 已随应用关闭而停止", taskName);
        }
    }

    private static async Task ExecuteOnceAsync(
        string taskName,
        OverlappingExecutionGuard guard,
        Func<CancellationToken, Task> work,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!guard.TryEnter())
        {
            logger.LogDebug("定时任务 {Task} 跳过：上一次尚未结束", taskName);
            return;
        }

        try
        {
            await work(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 应用关闭时的正常取消，不记错误
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "定时任务 {Task} 执行异常", taskName);
        }
        finally
        {
            guard.Exit();
        }
    }
}
