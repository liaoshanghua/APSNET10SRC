using Microsoft.EntityFrameworkCore;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>APSCore 全量迁移：兼容 EF6 的 Entities.V_xxx.SqlQuery / Where 访问模式。</summary>
public sealed partial class EasyManufactureEntities
{
    public IQueryable<T> Query<T>() where T : class => _db.Set<T>().AsNoTracking();

    public List<T> SqlQueryRaw<T>(string sql) where T : class =>
        _db.Database.SqlQueryRaw<T>(sql).AsEnumerable().ToList();
}
