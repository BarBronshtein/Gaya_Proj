using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Gaya.Infrastructure.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _masterConnectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=OperationsDb;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true;";

        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = builder.ConnectionString;
    }

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = builder.ConnectionString;
    }

    public string ConnectionString => _connectionString;
    public string MasterConnectionString => _masterConnectionString;

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
