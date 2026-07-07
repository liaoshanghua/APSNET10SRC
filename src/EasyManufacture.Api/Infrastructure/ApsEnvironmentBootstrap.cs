using System.Text;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// APS 启动后补齐运行环境（日志目录、启动脚本、register.ini 等）。
/// 注意：.NET 运行时缺失时 APS.exe 无法启动，须由 start-api.bat 先执行 Install-ApsDependencies.ps1。
/// </summary>
internal static class ApsEnvironmentBootstrap
{
    public static void Ensure(IConfiguration configuration, ILogger logger)
    {
        var publishPath = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var section = configuration.GetSection("Dependencies");
        if (!section.GetValue("Enabled", true))
            return;

        EnsureLogsDirectory(publishPath);
        EnsureRegisterIni(publishPath, logger);
        EnsureStartApiBat(publishPath, configuration, section, logger);
        WarnMissingConfiguration(configuration, logger);
    }

    private static void EnsureLogsDirectory(string publishPath)
    {
        var logsDir = Path.Combine(publishPath, "logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);
    }

    private static void EnsureRegisterIni(string publishPath, ILogger logger)
    {
        var iniPath = Path.Combine(publishPath, "register.ini");
        if (File.Exists(iniPath))
            return;

        File.WriteAllText(iniPath, "", Encoding.Default);
        logger.LogInformation("已创建空的 register.ini，请将授权密钥写入该文件");
    }

    private static void EnsureStartApiBat(
        string publishPath,
        IConfiguration configuration,
        IConfigurationSection section,
        ILogger logger)
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (!section.GetValue("EnsureStartScript", true))
            return;

        var batPath = Path.Combine(publishPath, "start-api.bat");
        if (File.Exists(batPath))
            return;

        var port = ApsStartScriptWriter.ResolvePort(configuration, configuration.GetSection("AutoStart"));
        ApsStartScriptWriter.WriteStartApiBat(publishPath, port, "APS.exe", "APS.dll");
        logger.LogInformation("已生成 start-api.bat（含依赖自动安装），建议用它或 APS-启动.bat 启动服务");
    }

    private static void WarnMissingConfiguration(IConfiguration configuration, ILogger logger)
    {
        var conn = configuration.GetConnectionString("MSSQLConnectionString");
        if (string.IsNullOrWhiteSpace(conn))
        {
            logger.LogWarning("ConnectionStrings:MSSQLConnectionString 未配置，请在 appsettings.json 中填写数据库连接串");
        }
    }
}
