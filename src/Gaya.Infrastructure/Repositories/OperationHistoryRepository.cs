using Dapper;
using Gaya.Domain.Entities;
using Gaya.Domain.Repositories;
using Gaya.Infrastructure.Data;

namespace Gaya.Infrastructure.Repositories;

public class OperationHistoryRepository : IOperationHistoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OperationHistoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(OperationHistory history, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO OperationHistories (OperationKey, FieldA, FieldB, Result, DurationMs, ExecutedAt)
            VALUES (@OperationKey, @FieldA, @FieldB, @Result, @DurationMs, @ExecutedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var conn = _connectionFactory.CreateConnection();
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            history.OperationKey,
            history.FieldA,
            history.FieldB,
            history.Result,
            history.DurationMs,
            history.ExecutedAt
        }, cancellationToken: ct));

        history.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<OperationHistory>> GetRecentByOperationKeyAsync(string operationKey, int count = 3, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT TOP (@Count) [Id], [OperationKey], [FieldA], [FieldB], [Result], [DurationMs], [ExecutedAt]
            FROM OperationHistories
            WHERE [OperationKey] = @OperationKey
            ORDER BY [ExecutedAt] DESC, [Id] DESC;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<OperationHistory>(new CommandDefinition(sql, new
        {
            OperationKey = operationKey,
            Count = count
        }, cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<int> GetMonthlyCountAsync(string operationKey, DateTime startOfMonthUtc, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM OperationHistories
            WHERE [OperationKey] = @OperationKey
              AND [ExecutedAt] >= @StartOfMonthUtc;";

        using var conn = _connectionFactory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            OperationKey = operationKey,
            StartOfMonthUtc = startOfMonthUtc
        }, cancellationToken: ct));

        return total;
    }
}
