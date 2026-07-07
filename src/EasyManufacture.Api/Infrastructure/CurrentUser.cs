using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Models;

namespace EasyManufacture.Api.Infrastructure;

public sealed class CurrentUser : ICurrentUser
{
    private DevAccount? _account;

    public DevAccount? Account => _account;
    public bool IsAuthenticated => _account != null;
    public void SetAccount(DevAccount? account) => _account = account;
}

public sealed class RequestBodyAccessor : IRequestBodyAccessor
{
    public string BodyJson { get; set; } = string.Empty;
}
