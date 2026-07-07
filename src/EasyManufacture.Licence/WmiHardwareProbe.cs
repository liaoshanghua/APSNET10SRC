using System.Reflection;

namespace EasyManufacture.Licence;

/// <summary>
/// 通过反射调用 WMI，避免发布目录缺少 System.Management.dll 时类型加载即崩溃。
/// </summary>
internal static class WmiHardwareProbe
{
    public static string? QueryFirst(string wmiClass, string property) =>
        QueryFirstWql($"SELECT {property} FROM {wmiClass}", property);

    public static string? QueryFirstWql(string wql, string property)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            var searcherType = ResolveManagementSearcherType();
            if (searcherType == null)
                return null;

            using var searcher = Activator.CreateInstance(searcherType, wql) as IDisposable;
            if (searcher == null)
                return null;

            var getMethod = searcherType.GetMethod("Get", Type.EmptyTypes);
            if (getMethod?.Invoke(searcher, null) is not System.Collections.IEnumerable results)
                return null;

            foreach (var item in results)
            {
                if (item == null)
                    continue;

                var indexer = item.GetType().GetProperty("Item", [typeof(string)]);
                var value = indexer?.GetValue(item, [property])?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        catch
        {
            // 程序集缺失、WMI 服务停止、权限不足
        }

        return null;
    }

    private static Type? ResolveManagementSearcherType()
    {
        const string typeName = "System.Management.ManagementObjectSearcher, System.Management";
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        try
        {
            var asm = Assembly.Load(new AssemblyName("System.Management"));
            return asm.GetType("System.Management.ManagementObjectSearcher");
        }
        catch
        {
            return null;
        }
    }
}
