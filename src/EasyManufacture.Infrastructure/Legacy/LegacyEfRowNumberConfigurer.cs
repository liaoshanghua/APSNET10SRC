using EasyManufacture.Entitys;
using EasyManufacture.Entitys.Ex;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>EFRowNumber 派生类型：断开 TPH，忽略内存/扩展属性（EF6 不映射，EF Core 需显式配置）。</summary>
internal static class LegacyEfRowNumberConfigurer
{
    private static readonly HashSet<string> AlwaysIgnored = new(StringComparer.Ordinal)
    {
        "RowNumber", "isChecked"
    };

    private static readonly Dictionary<Type, string[]> ExtraIgnored = new()
    {
        [typeof(V_APS_OrganizeProcessID)] = ["MaterialID", "OrderID", "TotalSchedulingQty", "label", "value"],
        [typeof(V_APS_OrganizeProcess)] = ["MaterialID", "OrderID", "TotalSchedulingQty", "label", "value"],
    };

    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var type in typeof(ManufactureDbContext).Assembly.GetTypes())
        {
            if (type.Namespace != "EasyManufacture.Entitys" || !type.IsClass || type.IsAbstract)
                continue;
            if (!IsEfRowNumberDerived(type))
                continue;

            var entity = modelBuilder.Entity(type);
            entity.HasBaseType((Type)null);

            foreach (var name in AlwaysIgnored)
                entity.Ignore(name);

            if (ExtraIgnored.TryGetValue(type, out var extras))
            {
                foreach (var name in extras)
                    entity.Ignore(name);
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    entity.Ignore(prop.Name);
                else if (prop.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType))
                    entity.Ignore(prop.Name);
            }
        }
    }

    private static bool IsEfRowNumberDerived(Type type)
    {
        for (var b = type.BaseType; b != null && b != typeof(object); b = b.BaseType)
        {
            if (b == typeof(EFRowNumber))
                return true;
        }
        return false;
    }
}
