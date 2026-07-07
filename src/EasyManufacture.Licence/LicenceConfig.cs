namespace EasyManufacture.Licence;

internal static class LicenceConfig
{
    public static string? Get(string key)
    {
        var c = LicenceRuntime.Configuration;
        return c[key] ?? c[$"App:{key}"];
    }

    public static string GetString(string key, string defaultValue = "")
    {
        var v = Get(key);
        return string.IsNullOrEmpty(v) ? defaultValue : v;
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        var v = Get(key);
        if (v == null) return defaultValue;
        return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        var v = Get(key);
        return v != null && int.TryParse(v, out var n) ? n : defaultValue;
    }

    public static double GetDouble(string key, double defaultValue = 0)
    {
        var v = Get(key);
        return v != null && double.TryParse(v, out var n) ? n : defaultValue;
    }
}
