using System.Reflection;
using System.Text;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 进程最早期初始化：工作目录、未捕获异常、闪退时写入 logs/aps-crash.log 并弹窗。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal static class ApsStartupGuard
{
    private static bool _fatalShown;

    public static void Initialize()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrWhiteSpace(baseDir))
                Directory.SetCurrentDirectory(baseDir);
        }
        catch
        {
            // ignored
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                ReportFatal(ex, "UnhandledException");
            else
                ReportFatal(new Exception(args.ExceptionObject?.ToString() ?? "unknown"), "UnhandledException");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ReportFatal(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };
    }

    public static void ReportFatal(Exception ex, string stage = "startup")
    {
        ApsCrashLogger.WriteFatal(ex, stage);

        if (!OperatingSystem.IsWindows() || _fatalShown)
            return;

        _fatalShown = true;
        try
        {
            ApsConsoleWindow.Show();
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "aps-crash.log");
            var message = new StringBuilder()
                .AppendLine("APS 启动失败")
                .AppendLine()
                .AppendLine(ExtractRootMessage(ex))
                .AppendLine()
                .AppendLine($"详情: {logPath}")
                .AppendLine()
                .AppendLine("请使用 APS-启动.bat 启动；")
                .AppendLine("若提示缺少运行时，请以管理员运行 APS-安装运行时.bat。")
                .ToString();

            System.Windows.Forms.MessageBox.Show(
                message,
                "APS 启动失败",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
        catch
        {
            // ignored
        }
    }

    private static string ExtractRootMessage(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is System.Net.Sockets.SocketException or Microsoft.AspNetCore.Connections.AddressInUseException)
                return e.Message;
        }

        return ex.Message;
    }
}
