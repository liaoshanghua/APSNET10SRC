using EasyManufacture.Licence;
using Microsoft.EntityFrameworkCore;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>EF6 兼容桩（LegacyCore 编译用）。</summary>
public sealed class ObjectParameter
{
    public ObjectParameter(string name, object? value) { Name = name; Value = value; }
    public string Name { get; }
    public object? Value { get; set; }
}

public static class Ef6QueryableCompat
{
    public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) where T : class => source;
    public static IQueryable<T> AsNoTracking<T>(this IEnumerable<T> source) where T : class =>
        source.AsQueryable();
}

public static class NPOIHelper
{
    public static void ExportExcel(System.Data.DataTable dt, string fileName) { }

    public static string TableToExcel(System.Data.DataTable dt, string file, string mergedRegionName = "") =>
        $"/UpdateLoad/DownLoad/{file}";
}
