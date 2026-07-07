using EasyManufacture.Licence;
using StackExchange.Redis;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>与旧版 EasyManufacture.Core.RedisHelper 一致。</summary>
public static class RedisHelper
{
    private static readonly Lazy<ConnectionMultiplexer> Redis = new(() =>
        ConnectionMultiplexer.Connect(AppInfo.Redis));

    public static IDatabase Db => Redis.Value.GetDatabase();

    /// <summary>旧代码使用 RedisHelper.db，保持同名属性。</summary>
    public static IDatabase db => Db;

    public static bool ClearKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;
        return Db.KeyDelete(key);
    }
}
