namespace EasyManufacture.Application.Abstractions;

public interface IConfigService
{
    Task<string> GetConfigAsync(string bodyJson, CancellationToken cancellationToken = default);
    Task<string> ConfigTableAsync(string bodyJson, CancellationToken cancellationToken = default);
}
