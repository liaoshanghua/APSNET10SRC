using System.Text;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>生成 start-api.bat（依赖检查 + 最小化窗口启动 APS）。</summary>
internal static class ApsStartScriptWriter
{
    public static string WriteStartApiBat(string publishPath, int port, string entryExe, string entryDll)
    {
        var batPath = Path.Combine(publishPath, "start-api.bat");
        var content = $"""
            @echo off
            cd /d "%~dp0"
            if not exist logs mkdir logs

            if exist "%~dp0Install-ApsDependencies.ps1" (
              powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" >> logs\deps-console.log 2>&1
            )

            set ASPNETCORE_ENVIRONMENT=Production
            set ASPNETCORE_URLS=http://0.0.0.0:{port}
            echo [%date% %time%] start-api.bat >> logs\startup.log
            if exist "%~dp0{entryExe}" (
              start "APS" /MIN "%~dp0{entryExe}"
            ) else (
              start "APS" /MIN dotnet "%~dp0{entryDll}"
            )
            echo [%date% %time%] launched >> logs\startup.log
            """;

        // 无 BOM：cmd 对 UTF-8 BOM 的 @echo off 解析不稳定
        File.WriteAllText(batPath, content.Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        WriteStartApiMinVbs(publishPath);
        WriteStartApiLogonBat(publishPath, port, entryExe, entryDll);
        return batPath;
    }

    /// <summary>用户登录后：结束旧 APS，在当前桌面会话重启（显示托盘）。</summary>
    public static string WriteStartApiLogonBat(string publishPath, int port, string entryExe, string entryDll)
    {
        var batPath = Path.Combine(publishPath, "start-api-logon.bat");
        var content = $"""
            @echo off
            cd /d "%~dp0"
            if not exist logs mkdir logs
            echo [%date% %time%] logon: restart for tray >> logs\startup.log
            taskkill /F /IM {entryExe} >nul 2>&1
            ping 127.0.0.1 -n 3 >nul
            set ASPNETCORE_ENVIRONMENT=Production
            set ASPNETCORE_URLS=http://0.0.0.0:{port}
            if exist "%~dp0{entryExe}" (
              start "APS" "%~dp0{entryExe}"
            ) else (
              start "APS" dotnet "%~dp0{entryDll}"
            )
            echo [%date% %time%] logon: launched >> logs\startup.log
            """;
        File.WriteAllText(batPath, content.Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return batPath;
    }

    /// <summary>计划任务入口：VBS 最小化调用 start-api.bat，避免前台 cmd 被误关。</summary>
    public static string WriteStartApiMinVbs(string publishPath)
    {
        var vbsPath = Path.Combine(publishPath, "start-api-min.vbs");
        const string content = """
            ' APS 计划任务用：最小化启动 start-api.bat
            Option Explicit
            Dim sh, fso, base
            Set sh = CreateObject("WScript.Shell")
            Set fso = CreateObject("Scripting.FileSystemObject")
            base = fso.GetParentFolderName(WScript.ScriptFullName)
            If Right(base, 1) <> Chr(92) Then base = base & Chr(92)
            sh.CurrentDirectory = base
            ' 7=最小化窗口；True=等待 bat 结束（False 时计划任务会立刻结束并杀掉子进程，导致 APS 起不来）
            sh.Run "cmd /c " & Chr(34) & base & "start-api.bat" & Chr(34), 7, True
            """;

        File.WriteAllText(vbsPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return vbsPath;
    }

    public static string GetScheduledTaskCommand(string publishPath) =>
        $"wscript.exe //B \"{WriteStartApiMinVbs(publishPath)}\"";

    public static int ResolvePort(IConfiguration configuration, IConfigurationSection autoStartSection)
    {
        var configured = autoStartSection.GetValue("Port", 0);
        if (configured > 0)
            return configured;

        var kestrelUrl = configuration["Kestrel:Endpoints:Http:Url"];
        if (!string.IsNullOrWhiteSpace(kestrelUrl) && Uri.TryCreate(kestrelUrl, UriKind.Absolute, out var uri))
            return uri.Port;

        var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrWhiteSpace(envUrls))
        {
            foreach (var part in envUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Uri.TryCreate(part, UriKind.Absolute, out var envUri))
                    return envUri.Port;
            }
        }

        return 9999;
    }
}
