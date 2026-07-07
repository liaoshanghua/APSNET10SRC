using SAP.Middleware.Connector;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>InterfaceSAP 等旧代码依赖的扩展方法（旧仓库中未单独成文件）。</summary>
public static class LegacyObjectExtensions
{
    public static bool ToSafeBool(this object? value)
    {
        if (value == null || value == DBNull.Value)
            return false;
        if (value is bool b)
            return b;
        var s = value.ToString()?.Trim();
        return s is "1" or "true" or "True" or "Y" or "y";
    }

    public static string ToSafeString(this IRfcFunction? function)
    {
        if (function == null)
            return string.Empty;
        try
        {
            return function.Metadata?.Name ?? function.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
