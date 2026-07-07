namespace EasyManufacture.Application.Abstractions;

/// <summary>
/// 对应旧版 APSCore.SaveData，按字典批量保存。完整逻辑需从 APSCore 逐步迁移。
/// </summary>
public interface ISaveDataService
{
    Task<string> SaveDataAsync(string bodyJson, CancellationToken cancellationToken = default);
}
