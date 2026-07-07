using EasyManufacture.Domain.Data;
using Microsoft.Data.SqlClient;

namespace EasyManufacture.Infrastructure.Data;

/// <summary>SQL Server 连接工厂（读取 appsettings ConnectionStrings）。</summary>
public interface ISqlConnectionFactory
{
    /// <summary>创建连接（未 Open）；<paramref name="useScm"/> 为 true 时使用 SCM 库连接串。</summary>
    SqlConnection CreateConnection(bool useScm = false);
}

/// <inheritdoc />
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _main;
    private readonly string _scm;

    public SqlConnectionFactory(Microsoft.Extensions.Options.IOptions<Domain.Options.DatabaseSettings> options)
    {
        _main = SqlConnectionStringHelper.Normalize(options.Value.MSSQLConnectionString);
        _scm = SqlConnectionStringHelper.Normalize(options.Value.MSSQLConnectionStringSCM);
    }

    /// <inheritdoc />
    public SqlConnection CreateConnection(bool useScm = false) =>
        new(useScm && !string.IsNullOrWhiteSpace(_scm) ? _scm : _main);
}
