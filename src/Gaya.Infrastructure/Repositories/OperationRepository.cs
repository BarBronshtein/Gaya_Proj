using System.Data;
using Dapper;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;
using Gaya.Infrastructure.Data;

namespace Gaya.Infrastructure.Repositories;

public class OperationRepository : IOperationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OperationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<OperationDefinition>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT [Key], [DisplayName], [Category], [RuleTemplate], [FieldAPrompt], [FieldBPrompt], [Description], [IsActive], [CreatedAt], [UpdatedAt]
            FROM Operations
            WHERE (@ActiveOnly = 0 OR [IsActive] = 1)
            ORDER BY [DisplayName] ASC;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<OperationDbRow>(new CommandDefinition(sql, new { ActiveOnly = activeOnly ? 1 : 0 }, cancellationToken: ct));

        return rows.Select(MapToDomain).ToList();
    }

    public async Task<OperationDefinition?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT [Key], [DisplayName], [Category], [RuleTemplate], [FieldAPrompt], [FieldBPrompt], [Description], [IsActive], [CreatedAt], [UpdatedAt]
            FROM Operations
            WHERE [Key] = @Key;";

        using var conn = _connectionFactory.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<OperationDbRow>(new CommandDefinition(sql, new { Key = key }, cancellationToken: ct));

        return row != null ? MapToDomain(row) : null;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        const string sql = "SELECT 1 FROM Operations WHERE [Key] = @Key;";

        using var conn = _connectionFactory.CreateConnection();
        var result = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { Key = key }, cancellationToken: ct));

        return result.HasValue;
    }

    public async Task UpsertAsync(OperationDefinition operation, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE Operations AS target
            USING (SELECT @Key AS [Key]) AS source
            ON (target.[Key] = source.[Key])
            WHEN MATCHED THEN
                UPDATE SET 
                    DisplayName = @DisplayName,
                    Category = @Category,
                    RuleTemplate = @RuleTemplate,
                    FieldAPrompt = @FieldAPrompt,
                    FieldBPrompt = @FieldBPrompt,
                    Description = @Description,
                    IsActive = @IsActive,
                    UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT ([Key], [DisplayName], [Category], [RuleTemplate], [FieldAPrompt], [FieldBPrompt], [Description], [IsActive], [CreatedAt])
                VALUES (@Key, @DisplayName, @Category, @RuleTemplate, @FieldAPrompt, @FieldBPrompt, @Description, @IsActive, SYSUTCDATETIME());";

        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            operation.Key,
            operation.DisplayName,
            Category = operation.Category.ToString(),
            operation.RuleTemplate,
            operation.FieldAPrompt,
            operation.FieldBPrompt,
            operation.Description,
            operation.IsActive
        }, cancellationToken: ct));
    }

    public async Task SetActiveStatusAsync(string key, bool isActive, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE Operations
            SET [IsActive] = @IsActive, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Key] = @Key;";

        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Key = key, IsActive = isActive }, cancellationToken: ct));
    }

    private static OperationDefinition MapToDomain(OperationDbRow row)
    {
        Enum.TryParse<OperationCategory>(row.Category, true, out var category);

        return new OperationDefinition
        {
            Key = row.Key,
            DisplayName = row.DisplayName,
            Category = category,
            RuleTemplate = row.RuleTemplate,
            FieldAPrompt = row.FieldAPrompt,
            FieldBPrompt = row.FieldBPrompt,
            Description = row.Description,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };
    }

    private sealed class OperationDbRow
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RuleTemplate { get; set; } = string.Empty;
        public string FieldAPrompt { get; set; } = string.Empty;
        public string FieldBPrompt { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
