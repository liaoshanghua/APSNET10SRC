using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// Windows 托盘：启动后隐藏控制台，点击图标再显示状态窗体。
/// </summary>
internal sealed class ApsTrayIconHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ApsTrayIconHostedService> _logger;

    private static int _webHostReady;
    private Thread? _uiThread;
    private CancellationTokenSource? _uiCts;
    private ApsTrayUiContext? _uiContext;

    public ApsTrayIconHostedService(
        IConfiguration configuration,
        IHostApplicationLifetime lifetime,
        ILogger<ApsTrayIconHostedService> logger)
    {
        _configuration = configuration;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // 开机 SYSTEM/Session0 无桌面，托盘不可见；登录会话再启 APS 才会出托盘
        if (!Environment.UserInteractive || GetWindowsSessionId() == 0)
        {
            _logger.LogInformation("非交互会话（Session0/后台）：跳过托盘图标");
            return Task.CompletedTask;
        }

        var section = _configuration.GetSection("Tray");
        if (!section.GetValue("Enabled", true))
            return Task.CompletedTask;

        var port = ApsStartScriptWriter.ResolvePort(_configuration, _configuration.GetSection("AutoStart"));
        var homeUrl = section.GetValue("HomeUrl", "")?.Trim();
        if (string.IsNullOrWhiteSpace(homeUrl))
            homeUrl = $"http://localhost:{port}";

        var title = section.GetValue("Title", "APS 排产服务") ?? "APS 排产服务";
        var hideConsole = section.GetValue("HideConsoleOnStart", true);
        var openBrowserOnClick = section.GetValue("OpenBrowserOnClick", false);
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");

        _lifetime.ApplicationStarted.Register(MarkWebHostReady);

        _uiCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _uiThread = new Thread(() => RunUiLoop(title, homeUrl, logsDir, hideConsole, openBrowserOnClick, _uiCts.Token))
        {
            Name = "ApsTrayUi",
            IsBackground = true
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        _logger.LogInformation("托盘图标已启用：{Title}，{Url}", title, homeUrl);
        return Task.CompletedTask;
    }

    internal static void MarkWebHostReady() => Interlocked.Exchange(ref _webHostReady, 1);

    private static int GetWindowsSessionId()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.SessionId;
        }
        catch
        {
            return -1;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _uiCts?.Cancel();
            ApsShutdownConfirmation.NotifyHostStopping();
            _uiContext?.RequestClose();

            if (_uiThread != null
                && _uiThread.IsAlive
                && _uiThread.ManagedThreadId != Environment.CurrentManagedThreadId)
            {
                if (!_uiThread.Join(TimeSpan.FromSeconds(8)))
                    _logger.LogWarning("托盘 UI 线程未在 8 秒内结束");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "停止托盘 UI 时发生异常");
        }

        return Task.CompletedTask;
    }

    private void RunUiLoop(
        string title,
        string homeUrl,
        string logsDir,
        bool hideConsole,
        bool openBrowserOnClick,
        CancellationToken cancellationToken)
    {
        try
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            _uiContext = new ApsTrayUiContext(
                title,
                homeUrl,
                logsDir,
                hideConsole,
                openBrowserOnClick,
                ApsShutdownConfirmation.RequestShutdown,
                cancellationToken);

            ApsShutdownConfirmation.RegisterUiExit(_uiContext.RequestClose);
            System.Windows.Forms.Application.Run(_uiContext);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "托盘 UI 线程异常");
        }
    }

    private sealed class ApsTrayUiContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ApsStatusForm _statusForm;
        private readonly bool _openBrowserOnClick;
        private readonly CancellationToken _cancellationToken;
        private int _exitRequested;

        public ApsTrayUiContext(
            string title,
            string homeUrl,
            string logsDir,
            bool hideConsole,
            bool openBrowserOnClick,
            Action onExit,
            CancellationToken cancellationToken)
        {
            _openBrowserOnClick = openBrowserOnClick;
            _cancellationToken = cancellationToken;

            _statusForm = new ApsStatusForm(title, homeUrl, logsDir);
            ApsShutdownConfirmation.SetUiInvoker(_statusForm);
            _statusForm.FormClosed += (_, _) => ExitThreadCore();

            _notifyIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = title,
                Visible = true
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("打开管理页", null, (_, _) => OpenUrl(homeUrl));
            menu.Items.Add("显示状态窗口", null, (_, _) => ShowStatusForm());
            menu.Items.Add("数据库还原 / 清空业务表", null, (_, _) => ApsDatabaseRestoreForm.ShowRestoreDialog(_statusForm));
            menu.Items.Add("显示控制台", null, (_, _) => ApsConsoleWindow.Show());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("禁止开机启动", null, (_, _) => DisableAutoStart(_statusForm));
            menu.Items.Add("退出 APS", null, (_, _) => onExit());
            _notifyIcon.ContextMenuStrip = menu;

            _notifyIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (_openBrowserOnClick)
                        OpenUrl(homeUrl);
                    else
                        ShowStatusForm();
                }
            };

            if (hideConsole)
                WaitAndHideConsole(cancellationToken);

            _cancellationToken.Register(RequestClose);
        }

        public void RequestClose()
        {
            if (Interlocked.Exchange(ref _exitRequested, 1) != 0)
                return;

            if (_statusForm.IsHandleCreated && _statusForm.InvokeRequired)
                _statusForm.BeginInvoke(ExitThreadCore);
            else
                ExitThreadCore();
        }

        private void ShowStatusForm() => _statusForm.ShowAndActivate();

        private void ExitThreadCore()
        {
            try
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            catch { /* ignored */ }

            try
            {
                if (!_statusForm.IsDisposed)
                    _statusForm.Close();
            }
            catch { /* ignored */ }

            try { ExitThread(); }
            catch { /* ignored */ }
        }

        private static Icon LoadTrayIcon()
        {
            try
            {
                var exePath = Path.Combine(AppContext.BaseDirectory, "APS.exe");
                if (File.Exists(exePath))
                {
                    var extracted = Icon.ExtractAssociatedIcon(exePath);
                    if (extracted != null)
                        return extracted;
                }
            }
            catch
            {
                // ignored
            }

            return SystemIcons.Application;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private static void DisableAutoStart(IWin32Window owner)
        {
            var confirm = MessageBox.Show(
                owner,
                "确定禁止开机启动？\n将删除计划任务 APS / APS-Logon，并阻止下次启动时自动重新注册。",
                "禁止开机启动",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;

            var (ok, message) = ApsAutoStartInstaller.TryUninstall();
            MessageBox.Show(
                owner,
                message,
                "禁止开机启动",
                MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private static void WaitAndHideConsole(CancellationToken cancellationToken)
        {
            for (var i = 0; i < 120; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (Volatile.Read(ref _webHostReady) == 1)
                {
                    ApsConsoleWindow.Hide();
                    return;
                }

                Thread.Sleep(500);
            }
        }
    }
}
