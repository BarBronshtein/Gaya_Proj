using Dapper;
using Gaya.Domain.Entities;
using Gaya.Domain.Repositories;
using Gaya.Infrastructure.Data;

namespace Gaya.Infrastructure.Repositories;

public class SystemErrorRepository : ISystemErrorRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SystemErrorRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(SystemError error, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO SystemErrors (ErrorMessage, StackTrace, SourcePath, StatusCode, CreatedAt)
            VALUES (@ErrorMessage, @StackTrace, @SourcePath, @StatusCode, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var conn = _connectionFactory.CreateConnection();
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            error.ErrorMessage,
            error.StackTrace,
            error.SourcePath,
            error.StatusCode,
            error.CreatedAt
        }, cancellationToken: ct));

        error.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<SystemError>> GetRecentAsync(int count = 50, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP (@Count) [Id], [ErrorMessage], [StackTrace], [SourcePath], [StatusCode], [CreatedAt]
            FROM SystemErrors
            ORDER BY [CreatedAt] DESC, [Id] DESC;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<SystemError>(new CommandDefinition(sql, new { Count = count }, cancellationToken: ct));

        return rows.ToList();
    }
}
