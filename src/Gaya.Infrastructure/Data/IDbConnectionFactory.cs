using System.Data;

namespace Gaya.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
    string ConnectionString { get; }
    string MasterConnectionString { get; }
}
