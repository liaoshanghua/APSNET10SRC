using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>数据库还原 / 按前缀清空 APS 业务表。</summary>
internal static class ApsDatabaseRestoreService
{
    public static readonly string[] TablePrefixes =
    [
        "APS_Order",
        "APS_Material",
        "APS_PO",
        "APS_ProcessMaterial",
        "APS_ProcessPlan",
        "APS_SalesOrder"
    ];

    public static string BaseDirectory => AppContext.BaseDirectory;

    public static string AppSettingsPath => Path.Combine(BaseDirectory, "appsettings.json");

    public static string RestoreScriptPath => Path.Combine(BaseDirectory, "Restore-ApsDatabase.ps1");

    public static string TruncateProcScriptPath => Path.Combine(BaseDirectory, "P_APS_TruncateCoreTablesAfterRestore.sql");

    public static string PrefixDescription =>
        string.Join(", ", TablePrefixes.Select(p => p + "*"));

    public static string ReadConnectionString()
    {
        if (!File.Exists(AppSettingsPath))
            throw new FileNotFoundException("找不到 appsettings.json", AppSettingsPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(AppSettingsPath));
        var cs = doc.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("MSSQLConnectionString")
            .GetString();

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("appsettings.json 中未配置 ConnectionStrings:MSSQLConnectionString");

        return cs;
    }

    public static (string Server, string Database) ParseServerDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return (builder.DataSource, builder.InitialCatalog);
    }

    public static IReadOnlyList<string> TruncatePrefixTables()
    {
        var connectionString = ReadConnectionString();
        if (!File.Exists(TruncateProcScriptPath))
            throw new FileNotFoundException("找不到清空表 SQL 脚本", TruncateProcScriptPath);

        var script = File.ReadAllText(TruncateProcScriptPath);
        ExecuteSqlBatches(connectionString, script);

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand("EXEC dbo.P_APS_TruncateCoreTablesAfterRestore", conn)
        {
            CommandTimeout = 0
        };
        using var reader = cmd.ExecuteReader();
        var list = new List<string>();
        while (reader.Read())
            list.Add(reader.GetString(0));
        return list;
    }

    public static void LaunchRestoreScript(string backupPath, bool truncateAfterRestore)
    {
        if (!File.Exists(RestoreScriptPath))
            throw new FileNotFoundException("找不到还原脚本", RestoreScriptPath);

        backupPath = Path.GetFullPath(backupPath);
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("备份文件不存在", backupPath);

        var args = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-NoExit",
            "-File", Quote(RestoreScriptPath),
            "-BackupPath", Quote(backupPath),
            "-AppSettingsPath", Quote(AppSettingsPath),
            "-TruncateProcScript", Quote(TruncateProcScriptPath),
            "-Confirm"
        };
        if (!truncateAfterRestore)
            args.Add("-SkipTruncate");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = string.Join(" ", args),
            WorkingDirectory = BaseDirectory,
            UseShellExecute = true
        };
        if (System.Diagnostics.Process.Start(psi) == null)
            throw new InvalidOperationException("无法启动 PowerShell，请确认已安装并可用。");
    }

    private static string Quote(string path) => "\"" + path.Replace("\"", "\\\"") + "\"";

    private static void ExecuteSqlBatches(string connectionString, string script)
    {
        var batches = Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        foreach (var batch in batches)
        {
            var text = batch.Trim();
            if (string.IsNullOrEmpty(text))
                continue;
            using var cmd = new SqlCommand(text, conn) { CommandTimeout = 0 };
            cmd.ExecuteNonQuery();
        }
    }
}
