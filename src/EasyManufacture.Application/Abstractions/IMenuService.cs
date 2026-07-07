namespace EasyManufacture.Application.Abstractions;

public interface IMenuService
{
    Task<string> GetMenuVueAsync(string account, CancellationToken cancellationToken = default);
}
