namespace EasyManufacture.Domain.Data;

/// <summary>
/// 内网 SQL Server 常用自签名证书；Microsoft.Data.SqlClient 默认校验 SSL 会失败。
/// </summary>
public static class SqlConnectionStringHelper
{
    public static string Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        var value = connectionString.Trim();
        if (TryGetKeyValue(value, "TrustServerCertificate", out var trustValue))
        {
            if (IsTrue(trustValue))
                return value;

            return ReplaceKeyValue(value, "TrustServerCertificate", "True");
        }

        return value.EndsWith(';') ? value + "TrustServerCertificate=True" : value + ";TrustServerCertificate=True";
    }

    private static bool TryGetKeyValue(string connectionString, string key, out string value)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
                continue;
            if (part[..idx].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = part[(idx + 1)..].Trim();
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static string ReplaceKeyValue(string connectionString, string key, string newValue)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var idx = parts[i].IndexOf('=');
            if (idx <= 0)
                continue;
            if (parts[i][..idx].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = $"{key}={newValue}";
                return string.Join(';', parts);
            }
        }

        return connectionString;
    }

    private static bool IsTrue(string value) =>
        value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
        || value == "1";
}
