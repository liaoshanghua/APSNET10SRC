using EasyManufacture.Domain.Models;

namespace EasyManufacture.Application.Abstractions;

public interface ICurrentUser
{
    DevAccount? Account { get; }
    bool IsAuthenticated { get; }
    void SetAccount(DevAccount? account);
}

public interface IRequestBodyAccessor
{
    string BodyJson { get; set; }
}
