using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// APS.exe 首次/每次启动时自动注册 Windows 计划任务（对齐 scripts/Install-ApiAutoStart.ps1）。
/// 默认「系统启动时 / SYSTEM」启动（AtStartup）；需管理员权限注册，否则回退为 AtLogOn。
/// </summary>
internal static class ApsAutoStartInstaller
{
    private const string MarkerFileName = ".autostart-installed.json";
    private const string DisabledTriggerMode = "Disabled";

    public static void TryInstall(IConfiguration configuration, ILogger logger)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var section = configuration.GetSection("AutoStart");
        if (!section.GetValue("Enabled", true) || !section.GetValue("InstallOnLaunch", true))
            return;

        var publishPath = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var taskName = section.GetValue("TaskName", "APS") ?? "APS";
        var exeName = section.GetValue("ExeName", "APS") ?? "APS";
        var atLogOn = section.GetValue("AtLogOn", false);
        var atStartup = section.GetValue("AtStartup", true);
        var port = ApsStartScriptWriter.ResolvePort(configuration, section);

        try
        {
            if (IsDisabled(publishPath))
            {
                logger.LogInformation("开机自启已禁止（{Marker}），跳过自动注册", MarkerFileName);
                return;
            }

            if (IsAlreadyInstalled(publishPath, taskName))
            {
                logger.LogDebug("开机自启已注册（任务 {TaskName}），跳过", taskName);
                return;
            }

            var entryExe = $"{exeName}.exe";
            var entryDll = $"{exeName}.dll";
            var exePath = Path.Combine(publishPath, entryExe);
            var dllPath = Path.Combine(publishPath, entryDll);
            if (!File.Exists(exePath) && !File.Exists(dllPath))
            {
                logger.LogWarning("未找到 {Exe} 或 {Dll}，跳过开机自启注册", entryExe, entryDll);
                return;
            }

            var batPath = ApsStartScriptWriter.WriteStartApiBat(publishPath, port, entryExe, entryDll);
            var taskCommand = ApsStartScriptWriter.GetScheduledTaskCommand(publishPath);
            var logonBat = ApsStartScriptWriter.WriteStartApiLogonBat(publishPath, port, entryExe, entryDll);
            var logonTaskName = section.GetValue("LogonTaskName", "APS-Logon") ?? "APS-Logon";
            var logonCommand = $"cmd.exe /c \"{logonBat}\"";
            var isAdmin = IsAdministrator();

            if (atStartup && isAdmin)
            {
                RegisterTask(taskName, taskCommand, publishPath, atLogOn: false, runAsSystem: true);
                RegisterTask(logonTaskName, logonCommand, publishPath, atLogOn: true, runAsSystem: false);
                WriteMarker(publishPath, taskName, "AtStartup+AtLogOn");
                logger.LogInformation(
                    "已注册开机自启：{TaskName}（系统启动）+ {LogonTask}（登录后托盘）",
                    taskName,
                    logonTaskName);
                return;
            }

            if (atStartup && !isAdmin)
            {
                logger.LogWarning(
                    "AutoStart:AtStartup 需要管理员权限；已改为用户登录时启动（AtLogOn）。"
                    + " 请以管理员运行 APS-安装开机自启.bat 或 Install-ApsAutoStart.ps1");
                RegisterTask(taskName, taskCommand, publishPath, atLogOn: true, runAsSystem: false);
                WriteMarker(publishPath, taskName, "AtLogOn");
                logger.LogInformation("已注册开机自启计划任务 {TaskName}（用户登录时，端口 {Port}）", taskName, port);
                return;
            }

            if (atLogOn)
            {
                RegisterTask(taskName, logonCommand, publishPath, atLogOn: true, runAsSystem: false);
                WriteMarker(publishPath, taskName, "AtLogOn");
                logger.LogInformation("已注册开机自启计划任务 {TaskName}（用户登录时，端口 {Port}）", taskName, port);
                return;
            }

            logger.LogWarning("AutoStart 已启用但未配置 AtStartup 或 AtLogOn，跳过注册");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "自动注册开机自启失败，可手动执行 scripts/Install-ApiAutoStart.ps1");
        }
    }

    /// <summary>
    /// 删除 APS / APS-Logon 计划任务，并写入 Disabled 标记，避免下次启动再自动注册。
    /// </summary>
    public static (bool Ok, string Message) TryUninstall(IConfiguration? configuration = null)
    {
        if (!OperatingSystem.IsWindows())
            return (false, "仅支持 Windows");

        var section = configuration?.GetSection("AutoStart");
        var publishPath = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var taskName = section?.GetValue("TaskName", "APS") ?? "APS";
        var logonTaskName = section?.GetValue("LogonTaskName", "APS-Logon") ?? "APS-Logon";

        try
        {
            WriteMarker(publishPath, taskName, DisabledTriggerMode);

            DeleteTaskIfExists(taskName);
            DeleteTaskIfExists(logonTaskName);

            if (TaskExists(taskName) || TaskExists(logonTaskName))
            {
                if (!IsAdministrator())
                    TryDeleteTasksElevated(taskName, logonTaskName);
                else
                {
                    DeleteTaskIfExists(taskName);
                    DeleteTaskIfExists(logonTaskName);
                }
            }

            var remain = new List<string>();
            if (TaskExists(taskName))
                remain.Add(taskName);
            if (TaskExists(logonTaskName))
                remain.Add(logonTaskName);

            if (remain.Count > 0)
            {
                return (false,
                    "已写入「禁止开机启动」标记（下次启动不会再自动注册），"
                    + $"但计划任务未删干净：{string.Join("、", remain)}。"
                    + "请以管理员运行「APS-禁止开机自启.bat」。");
            }

            return (true, "已禁止开机启动：计划任务已删除，下次启动也不会自动注册。");
        }
        catch (Exception ex)
        {
            try { WriteMarker(publishPath, taskName, DisabledTriggerMode); }
            catch { /* ignored */ }

            return (false, "禁止开机启动失败：" + ex.Message
                + "。可尝试以管理员运行「APS-禁止开机自启.bat」。");
        }
    }

    public static bool IsAutoStartDisabled()
    {
        if (!OperatingSystem.IsWindows())
            return false;
        return IsDisabled(AppContext.BaseDirectory.TrimEnd('\\', '/'));
    }

    private static bool IsDisabled(string publishPath)
    {
        var markerPath = Path.Combine(publishPath, MarkerFileName);
        if (!File.Exists(markerPath))
            return false;

        try
        {
            var marker = JsonSerializer.Deserialize<AutoStartMarker>(File.ReadAllText(markerPath));
            return marker != null
                && string.Equals(marker.TriggerMode, DisabledTriggerMode, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAlreadyInstalled(string publishPath, string taskName)
    {
        var markerPath = Path.Combine(publishPath, MarkerFileName);
        if (!File.Exists(markerPath))
            return false;

        try
        {
            var marker = JsonSerializer.Deserialize<AutoStartMarker>(File.ReadAllText(markerPath));
            if (marker is null
                || string.Equals(marker.TriggerMode, DisabledTriggerMode, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(marker.TaskName, taskName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(NormalizePath(marker.PublishPath), NormalizePath(publishPath), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(marker.TriggerMode, "AtStartup+AtLogOn", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TaskExists(taskName) && TaskExists("APS-Logon");
        }
        catch
        {
            return false;
        }
    }

    private static void TryDeleteTasksElevated(string taskName, string logonTaskName)
    {
        var cmd = $"/c schtasks /Delete /TN \"{taskName}\" /F & schtasks /Delete /TN \"{logonTaskName}\" /F";
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            process?.WaitForExit(60000);
        }
        catch
        {
            // 用户取消 UAC 等
        }
    }

    private static void WriteMarker(string publishPath, string taskName, string triggerMode)
    {
        var marker = new AutoStartMarker
        {
            TaskName = taskName,
            PublishPath = publishPath,
            TriggerMode = triggerMode,
            InstalledAt = DateTimeOffset.Now
        };
        var json = JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(publishPath, MarkerFileName), json, Encoding.UTF8);
    }

    private static void RegisterTask(string taskName, string taskCommand, string workingDirectory, bool atLogOn, bool runAsSystem)
    {
        DeleteTaskIfExists(taskName);

        var args = new StringBuilder();
        args.Append("/Create /F ");
        args.Append(CultureInvariant($"/TN \"{taskName}\" "));
        args.Append(CultureInvariant($"/TR \"{taskCommand}\" "));
        args.Append(CultureInvariant($"/RL HIGHEST "));

        if (atLogOn)
        {
            args.Append("/SC ONLOGON ");
            args.Append(CultureInvariant($"/RU \"{Environment.UserDomainName}\\{Environment.UserName}\" "));
        }
        else
        {
            args.Append("/SC ONSTART ");
            args.Append("/RU SYSTEM ");
        }

        RunSchtasks(args.ToString(), workingDirectory);
    }

    private static void DeleteTaskIfExists(string taskName)
    {
        if (!TaskExists(taskName))
            return;

        RunSchtasks(CultureInvariant($"/Delete /TN \"{taskName}\" /F"), null, ignoreErrors: true);
    }

    private static bool TaskExists(string taskName)
    {
        try
        {
            RunSchtasks(CultureInvariant($"/Query /TN \"{taskName}\""), null, ignoreErrors: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RunSchtasks(string arguments, string? workingDirectory, bool ignoreErrors = false)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 && !ignoreErrors)
            throw new InvalidOperationException($"schtasks {arguments} failed ({process.ExitCode}): {stderr}{stdout}");
    }

    private static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path.TrimEnd('\\', '/'));

    private static string CultureInvariant(FormattableString value) =>
        FormattableString.Invariant(value);

    private sealed class AutoStartMarker
    {
        public string TaskName { get; set; } = "";
        public string PublishPath { get; set; } = "";
        public string TriggerMode { get; set; } = "";
        public DateTimeOffset InstalledAt { get; set; }
    }
}
