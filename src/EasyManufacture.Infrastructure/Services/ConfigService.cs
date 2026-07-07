using EasyManufacture.Application.Abstractions;
using EasyManufacture.Infrastructure.Legacy;

namespace EasyManufacture.Infrastructure.Services;

public sealed class ConfigService : IConfigService
{
    private readonly ApsCoreEngine _engine;

    public ConfigService(ApsCoreEngine engine) => _engine = engine;

    public Task<string> GetConfigAsync(string bodyJson, CancellationToken cancellationToken = default)
    {
        _engine.BodyJson = bodyJson;
        return Task.FromResult(_engine.RunGetConfig());
    }

    public Task<string> ConfigTableAsync(string bodyJson, CancellationToken cancellationToken = default) =>
        GetConfigAsync(bodyJson, cancellationToken);
}
