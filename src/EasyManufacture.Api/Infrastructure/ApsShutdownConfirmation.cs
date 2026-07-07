using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 关闭控制台、托盘或状态窗时必须先确认，避免误关 APS 服务。
/// </summary>
internal static class ApsShutdownConfirmation
{
    private const int CtrlCloseEvent = 2;
    private const int CtrlLogoffEvent = 5;
    private const int CtrlShutdownEvent = 6;

    private static readonly object Gate = new();
    private static IHostApplicationLifetime? _lifetime;
    private static ConsoleCtrlHandler? _handler;
    private static Control? _uiInvoker;
    private static Action? _uiExit;
    private static volatile bool _shutdownApproved;
    private static int _failSafeExitScheduled;

    private delegate bool ConsoleCtrlHandler(int ctrlType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler? handler, bool add);

    public static void Register(IHostApplicationLifetime lifetime)
    {
        if (!OperatingSystem.IsWindows())
            return;

        lock (Gate)
        {
            _lifetime = lifetime;
            if (_handler == null)
            {
                _handler = OnConsoleCtrl;
                SetConsoleCtrlHandler(_handler, add: true);
            }
        }

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            RequestShutdown();
        };
    }

    public static void SetUiInvoker(Control invoker) => _uiInvoker = invoker;

    /// <summary>注册托盘 UI 退出（须在 UI 线程上结束 Application.Run）。</summary>
    public static void RegisterUiExit(Action exitUiLoop) => _uiExit = exitUiLoop;

    /// <summary>用户点「是」时返回 true。</summary>
    public static bool TryConfirmShutdown()
    {
        if (_uiInvoker is { IsHandleCreated: true, InvokeRequired: true })
            return (bool)_uiInvoker.Invoke(new Func<bool>(ShowConfirmDialog))!;

        return ShowConfirmDialog();
    }

    public static void RequestShutdown()
    {
        if (_shutdownApproved)
        {
            _lifetime?.StopApplication();
            return;
        }

        if (!TryConfirmShutdown())
            return;

        ApproveAndStop();
    }

    internal static void ApproveAndStop()
    {
        _shutdownApproved = true;

        // 先结束 WinForms 消息循环，避免 StopAsync 在 UI 线程上 Join 自身导致死锁。
        try { _uiExit?.Invoke(); }
        catch { /* ignored */ }

        ThreadPool.QueueUserWorkItem(static _ =>
        {
            try { _lifetime?.StopApplication(); }
            catch { /* ignored */ }
        });

        ScheduleFailSafeExit();
    }

    /// <summary>宿主已开始停止时再次请求 UI 退出（StopAsync 调用）。</summary>
    public static void NotifyHostStopping()
    {
        try { _uiExit?.Invoke(); }
        catch { /* ignored */ }
    }

    private static void ScheduleFailSafeExit()
    {
        if (Interlocked.Exchange(ref _failSafeExitScheduled, 1) != 0)
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            Environment.Exit(0);
        });
    }

    private static bool ShowConfirmDialog()
    {
        var result = MessageBox.Show(
            _uiInvoker,
            "如果关闭此程序，APS系统将被关闭",
            "APS 排产服务",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        return result == DialogResult.Yes;
    }

    private static bool OnConsoleCtrl(int ctrlType)
    {
        if (ctrlType is not (CtrlCloseEvent or CtrlLogoffEvent or CtrlShutdownEvent))
            return false;

        if (_shutdownApproved)
            return false;

        if (!TryConfirmShutdown())
            return true;

        ApproveAndStop();
        return false;
    }
}
