using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>供 MSSQLCore 等静态遗留代码获取 DbContext（无 Http 作用域时的回退）。</summary>
public static class LegacyDbFactory
{
    private static IServiceProvider? _services;

    public static void Configure(IServiceProvider services) => _services = services;

    public static ManufactureDbContext CreateDbContext()
    {
        if (_services == null)
            throw new InvalidOperationException("LegacyDbFactory 未初始化");

        return _services.CreateScope().ServiceProvider.GetRequiredService<ManufactureDbContext>();
    }

    public static EasyManufactureEntities CreateEntities()
    {
        return new EasyManufactureEntities(CreateDbContext());
    }
}
