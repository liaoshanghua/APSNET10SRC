namespace EasyManufacture.Api.Infrastructure;

/// <summary>启动失败时写入 logs/aps-crash.log，避免窗口闪退看不到错误。</summary>
internal static class ApsCrashLogger
{
    public static void WriteFatal(Exception ex, string stage = "startup")
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "aps-crash.log");
            var text = $"""
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ({stage})
                {ex}
                
                """;
            File.AppendAllText(path, text);
            Console.Error.WriteLine(text);
        }
        catch
        {
            // ignored
        }
    }
}
