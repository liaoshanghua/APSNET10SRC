using EasyManufacture.Application.Abstractions;
using EasyManufacture.Infrastructure.Legacy;

namespace EasyManufacture.Infrastructure.Services;

public sealed class SaveDataService : ISaveDataService
{
    private readonly ApsCoreEngine _engine;

    public SaveDataService(ApsCoreEngine engine) => _engine = engine;

    public Task<string> SaveDataAsync(string bodyJson, CancellationToken cancellationToken = default)
    {
        _engine.ResetSaveDataState();
        _engine.BodyJson = bodyJson ?? string.Empty;
        return Task.FromResult(_engine.RunSaveData());
    }
}
