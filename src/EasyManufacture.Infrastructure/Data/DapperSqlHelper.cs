using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace EasyManufacture.Infrastructure.Data;

/// <summary>对应旧版 EasyManufacture.Core.DataBase.SqlHelper（常用方法）。</summary>
public sealed class DapperSqlHelper
{
    private readonly ISqlConnectionFactory _factory;

    public DapperSqlHelper(ISqlConnectionFactory factory) => _factory = factory;

    public async Task<DataTable> ExecuteDataTableAsync(string commandText, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var reader = await conn.ExecuteReaderAsync(new CommandDefinition(commandText, cancellationToken: cancellationToken));
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public async Task<int> ExecuteNonQueryAsync(string commandText, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.ExecuteAsync(new CommandDefinition(commandText, cancellationToken: cancellationToken));
    }

    public async Task<T?> ExecuteScalarAsync<T>(string commandText, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<T>(new CommandDefinition(commandText, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string commandText, CancellationToken cancellationToken = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.QueryAsync<T>(new CommandDefinition(commandText, cancellationToken: cancellationToken));
    }
}
