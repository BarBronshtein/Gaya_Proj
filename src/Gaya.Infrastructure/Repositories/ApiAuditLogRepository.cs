using Dapper;
using Gaya.Domain.Entities;
using Gaya.Domain.Repositories;
using Gaya.Infrastructure.Data;

namespace Gaya.Infrastructure.Repositories;

public class ApiAuditLogRepository : IApiAuditLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ApiAuditLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(ApiAuditLog log, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO ApiAuditLogs (Path, Method, StatusCode, LatencyMs, RequestBody, ResponseBody, ClientIp, CreatedAt)
            VALUES (@Path, @Method, @StatusCode, @LatencyMs, @RequestBody, @ResponseBody, @ClientIp, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var conn = _connectionFactory.CreateConnection();
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            log.Path,
            log.Method,
            log.StatusCode,
            log.LatencyMs,
            log.RequestBody,
            log.ResponseBody,
            log.ClientIp,
            log.CreatedAt
        }, cancellationToken: ct));

        log.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<ApiAuditLog>> GetRecentAsync(int count = 50, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP (@Count) [Id], [Path], [Method], [StatusCode], [LatencyMs], [RequestBody], [ResponseBody], [ClientIp], [CreatedAt]
            FROM ApiAuditLogs
            ORDER BY [CreatedAt] DESC, [Id] DESC;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<ApiAuditLog>(new CommandDefinition(sql, new { Count = count }, cancellationToken: ct));

        return rows.ToList();
    }
}
