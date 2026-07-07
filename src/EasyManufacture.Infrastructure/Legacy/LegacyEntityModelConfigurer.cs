using EasyManufacture.Entitys;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>为迁移的 EF 实体注册主键或无主键（APSCore 全量模式）。</summary>
internal static class LegacyEntityModelConfigurer
{
    private static readonly Lazy<Type[]> EntityTypes = new(LoadEntityTypes);

    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var type in EntityTypes.Value)
        {
            var entity = modelBuilder.Entity(type);
            if (entity.Metadata.FindPrimaryKey() != null)
                continue;

            if (type == typeof(V_DictionaryField))
                continue;

            if (type.Name.StartsWith("V_", StringComparison.Ordinal)
                || type.Name.StartsWith("APS_", StringComparison.Ordinal))
            {
                entity.HasNoKey();
                continue;
            }

            if (type.Name.StartsWith("Dev_", StringComparison.Ordinal))
            {
                var keyProp = TryInferDevKey(type);
                if (keyProp != null)
                    entity.HasKey(keyProp.Name);
                else
                    entity.HasNoKey();
            }
        }
    }

    private static PropertyInfo? TryInferDevKey(Type type)
    {
        return type.Name switch
        {
            "Dev_Menu" => type.GetProperty("MenuCode"),
            "Dev_Account" => type.GetProperty("Account"),
            "Dev_Role" => type.GetProperty("RoleID"),
            _ => InferKeyByConvention(type)
        };
    }

    private static PropertyInfo? InferKeyByConvention(Type type)
    {
        var suffix = type.Name.StartsWith("Dev_", StringComparison.Ordinal) ? type.Name[4..] : type.Name;
        var named = type.GetProperty(suffix + "ID");
        if (named != null)
            return named;

        var id = type.GetProperty("ID");
        if (id != null)
            return id;

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.EndsWith("ID", StringComparison.Ordinal)
                                 && IsKeyType(p.PropertyType));
    }

    private static bool IsKeyType(Type type)
    {
        var u = Nullable.GetUnderlyingType(type) ?? type;
        return u == typeof(int) || u == typeof(long) || u == typeof(string) || u == typeof(Guid);
    }

    private static Type[] LoadEntityTypes()
    {
        return typeof(ManufactureDbContext).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "EasyManufacture.Entitys"
                        && t.IsClass
                        && !t.IsAbstract
                        && t != typeof(Dev_Menu)
                        && (t.Name.StartsWith("V_", StringComparison.Ordinal)
                            || t.Name.StartsWith("APS_", StringComparison.Ordinal)
                            || t.Name.StartsWith("Dev_", StringComparison.Ordinal)))
            .ToArray();
    }
}
