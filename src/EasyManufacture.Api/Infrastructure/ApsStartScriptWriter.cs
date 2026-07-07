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
              powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0Install-ApsDependencies.ps1" >> logs\deps-install.log 2>&1
            )

            if exist "%~dp0.dotnet-local-path" (
              set /p _DOTNET_DIR=<"%~dp0.dotnet-local-path"
              set DOTNET_ROOT=%_DOTNET_DIR%
              set PATH=%_DOTNET_DIR%;%PATH%
            )

            set ASPNETCORE_ENVIRONMENT=Production
            echo [%date% %time%] start-api.bat >> logs\startup.log
            if exist "%~dp0{entryExe}" (
              "%~dp0{entryExe}" >> "%~dp0logs\aps-console.log" 2>&1
            ) else (
              dotnet "%~dp0{entryDll}" >> "%~dp0logs\aps-console.log" 2>&1
            )
            echo [%date% %time%] exited !ERRORLEVEL! >> "%~dp0logs\startup.log"
            """;

        File.WriteAllText(batPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        WriteStartApiMinVbs(publishPath);
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
            ' 7=最小化窗口；False=不等待（计划任务立即返回）
            sh.Run "cmd /c " & Chr(34) & base & "start-api.bat" & Chr(34), 7, False
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
