using EasyManufacture.Entitys;
using Microsoft.EntityFrameworkCore;

namespace EasyManufacture.Infrastructure.Legacy;

public sealed class ManufactureDbContext : DbContext
{
    public ManufactureDbContext(DbContextOptions<ManufactureDbContext> options) : base(options)
    {
    }

    public DbSet<Dev_Dictionary> DevDictionaries => Set<Dev_Dictionary>();
    public DbSet<Dev_DictionaryField> DevDictionaryFields => Set<Dev_DictionaryField>();
    public DbSet<Dev_Organize> DevOrganizes => Set<Dev_Organize>();
    public DbSet<V_Dev_Account> VDevAccounts => Set<V_Dev_Account>();
    public DbSet<Dev_Account> DevAccounts => Set<Dev_Account>();
    public DbSet<V_APS_SalesOrderDetail> VApsSalesOrderDetails => Set<V_APS_SalesOrderDetail>();
    public DbSet<V_APS_Order> VApsOrders => Set<V_APS_Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Dev_Menu 须先于 V_Dev_Account 配置：Session 字段 Menu 引用此类型，EF 会将其纳入模型
        modelBuilder.Entity<Dev_Menu>(e =>
        {
            e.ToTable("Dev_Menu");
            e.HasKey(x => x.MenuCode);
        });

        modelBuilder.Entity<Dev_Dictionary>(e =>
        {
            e.ToTable("Dev_Dictionary");
            e.HasKey(x => x.DictionaryID);
        });
        modelBuilder.Entity<Dev_DictionaryField>(e =>
        {
            e.ToTable("Dev_DictionaryField");
            e.HasKey(x => x.ID);
        });
        modelBuilder.Entity<V_DictionaryField>(e => e.HasNoKey());

        modelBuilder.Entity<Dev_Organize>(e =>
        {
            e.ToTable("Dev_Organize");
            e.HasKey(x => x.OrganizeID);
        });
        modelBuilder.Entity<Dev_Account>(e =>
        {
            e.ToTable("Dev_Account");
            e.HasKey(x => x.Account);
        });
        modelBuilder.Entity<V_APS_SalesOrderDetail>(e =>
        {
            e.ToView("V_APS_SalesOrderDetail");
            e.HasNoKey();
        });
        modelBuilder.Entity<V_APS_Order>(e =>
        {
            e.ToView("V_APS_Order");
            e.HasKey(x => x.OrderID);
        });
        modelBuilder.Entity<V_Dev_Account>(e =>
        {
            e.ToView("V_Dev_Account");
            e.HasKey(x => x.Account);
            // 以下属性为登录/Session 内存字段，非视图列，避免 EF 当作导航属性
            e.Ignore(x => x.RoleMap);
            e.Ignore(x => x.LastVisitTime);
            e.Ignore(x => x.ButtonMenuRoleMap);
            e.Ignore(x => x.MenuVue);
            e.Ignore(x => x.Organizes);
            e.Ignore(x => x.Menu);
            e.Ignore(x => x.CenterID);
            e.Ignore(x => x.GroupID);
        });

        LegacyEfRowNumberConfigurer.Apply(modelBuilder);
        LegacyEntityModelConfigurer.Apply(modelBuilder);
    }
}

/// <summary>兼容旧版 EasyManufactureEntities 访问方式。</summary>
public sealed partial class EasyManufactureEntities
{
    private readonly ManufactureDbContext _db;

    public EasyManufactureEntities(ManufactureDbContext db) => _db = db;

    public IQueryable<Dev_Dictionary> Dev_Dictionary => _db.DevDictionaries;
    public IQueryable<Dev_DictionaryField> Dev_DictionaryField => _db.DevDictionaryFields;

    public List<V_DictionaryField> SqlQueryV_DictionaryField(string sql) =>
        _db.Database.SqlQueryRaw<V_DictionaryField>(sql).ToList();

    public IQueryable<V_Dev_Account> V_Dev_Account => _db.VDevAccounts;
    public IQueryable<V_APS_SalesOrderDetail> V_APS_SalesOrderDetail => _db.VApsSalesOrderDetails;
    public IQueryable<V_APS_Order> V_APS_Order => _db.VApsOrders;
    public DbSet<Dev_Account> Dev_Account => _db.DevAccounts;

    public LegacyEfConfiguration Configuration { get; } = new();

    public int SaveChanges() => _db.SaveChanges();
}

/// <summary>兼容 EF6 <c>Entities.Configuration</c>。</summary>
public sealed class LegacyEfConfiguration
{
    public bool ValidateOnSaveEnabled { get; set; } = true;
}
