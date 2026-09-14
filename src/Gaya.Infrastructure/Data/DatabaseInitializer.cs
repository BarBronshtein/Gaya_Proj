using Dapper;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Gaya.Infrastructure.Data;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing database infrastructure and schema...");
        await EnsureDatabaseCreatedWithRetryAsync(ct);
        await ExecuteSchemaScriptsAsync(ct);
        await SeedDefaultOperationsAsync(ct);
        _logger.LogInformation("Database initialization completed successfully.");
    }

    private async Task EnsureDatabaseCreatedWithRetryAsync(CancellationToken ct)
    {
        const int maxRetries = 5;
        var delay = TimeSpan.FromSeconds(2);

        var targetDb = new SqlConnectionStringBuilder(_connectionFactory.ConnectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(targetDb))
        {
            targetDb = "OperationsDb";
        }

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var masterConn = new SqlConnection(_connectionFactory.MasterConnectionString);
                await masterConn.OpenAsync(ct);

                var checkDbSql = "SELECT 1 FROM sys.databases WHERE name = @DbName;";
                var exists = await masterConn.ExecuteScalarAsync<int?>(new CommandDefinition(checkDbSql, new { DbName = targetDb }, cancellationToken: ct));

                if (exists == null)
                {
                    _logger.LogInformation("Database '{TargetDb}' not found. Creating database...", targetDb);
                    var createDbSql = $"CREATE DATABASE [{targetDb}];";
                    await masterConn.ExecuteAsync(new CommandDefinition(createDbSql, cancellationToken: ct));
                    _logger.LogInformation("Database '{TargetDb}' created successfully.", targetDb);
                }
                else
                {
                    _logger.LogInformation("Database '{TargetDb}' exists.", targetDb);
                }

                return;
            }
            catch (SqlException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "SQL Server connection attempt {Attempt}/{MaxRetries} failed. Retrying in {DelaySeconds}s...", attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 1.5);
            }
        }
    }

    private async Task ExecuteSchemaScriptsAsync(CancellationToken ct)
    {
        using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);

        const string schemaSql = @"
        -- Operations Table
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Operations')
        BEGIN
            CREATE TABLE Operations (
                [Key] NVARCHAR(50) NOT NULL PRIMARY KEY,
                [DisplayName] NVARCHAR(100) NOT NULL,
                [Category] NVARCHAR(50) NOT NULL,
                [RuleTemplate] NVARCHAR(500) NOT NULL,
                [FieldAPrompt] NVARCHAR(100) NOT NULL,
                [FieldBPrompt] NVARCHAR(100) NOT NULL,
                [Description] NVARCHAR(500) NULL,
                [IsActive] BIT NOT NULL DEFAULT 1,
                [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                [UpdatedAt] DATETIME2 NULL
            );
        END

        -- OperationHistories Table
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OperationHistories')
        BEGIN
            CREATE TABLE OperationHistories (
                [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [OperationKey] NVARCHAR(50) NOT NULL,
                [FieldA] NVARCHAR(MAX) NOT NULL,
                [FieldB] NVARCHAR(MAX) NOT NULL,
                [Result] NVARCHAR(MAX) NOT NULL,
                [DurationMs] BIGINT NOT NULL,
                [ExecutedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                CONSTRAINT FK_OperationHistories_Operations FOREIGN KEY ([OperationKey]) REFERENCES Operations([Key]) ON DELETE CASCADE
            );

            CREATE NONCLUSTERED INDEX IX_OperationHistories_Key_ExecutedAt 
            ON OperationHistories([OperationKey], [ExecutedAt] DESC);

            CREATE NONCLUSTERED INDEX IX_OperationHistories_ExecutedAt 
            ON OperationHistories([ExecutedAt]);
        END

        -- ApiAuditLogs Table
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiAuditLogs')
        BEGIN
            CREATE TABLE ApiAuditLogs (
                [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Path] NVARCHAR(500) NOT NULL,
                [Method] NVARCHAR(10) NOT NULL,
                [StatusCode] INT NOT NULL,
                [LatencyMs] BIGINT NOT NULL,
                [RequestBody] NVARCHAR(MAX) NULL,
                [ResponseBody] NVARCHAR(MAX) NULL,
                [ClientIp] NVARCHAR(50) NULL,
                [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );

            CREATE NONCLUSTERED INDEX IX_ApiAuditLogs_CreatedAt 
            ON ApiAuditLogs([CreatedAt] DESC);
        END

        -- SystemErrors Table
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemErrors')
        BEGIN
            CREATE TABLE SystemErrors (
                [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ErrorMessage] NVARCHAR(MAX) NOT NULL,
                [StackTrace] NVARCHAR(MAX) NULL,
                [SourcePath] NVARCHAR(500) NULL,
                [StatusCode] INT NOT NULL,
                [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );

            CREATE NONCLUSTERED INDEX IX_SystemErrors_CreatedAt 
            ON SystemErrors([CreatedAt] DESC);
        END
        ";

        await conn.ExecuteAsync(new CommandDefinition(schemaSql, cancellationToken: ct));
        _logger.LogInformation("Database tables and indexes verified.");
    }

    private async Task SeedDefaultOperationsAsync(CancellationToken ct)
    {
        using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);

        var seedOperations = new List<OperationDefinition>
        {
            // Arithmetic Operations
            new()
            {
                Key = "add",
                DisplayName = "חיבור (+)",
                Category = OperationCategory.Arithmetic,
                RuleTemplate = "A + B",
                FieldAPrompt = "מספר ראשון",
                FieldBPrompt = "מספר שני",
                Description = "חיבור שני מספרים עשרוניים או שלמים",
                IsActive = true
            },
            new()
            {
                Key = "subtract",
                DisplayName = "חיסור (-)",
                Category = OperationCategory.Arithmetic,
                RuleTemplate = "A - B",
                FieldAPrompt = "מספר ראשון",
                FieldBPrompt = "מספר שני",
                Description = "חיסור המספר השני מהראשון",
                IsActive = true
            },
            new()
            {
                Key = "multiply",
                DisplayName = "כפל (*)",
                Category = OperationCategory.Arithmetic,
                RuleTemplate = "A * B",
                FieldAPrompt = "מספר ראשון",
                FieldBPrompt = "מספר שני",
                Description = "כפל של שני מספרים",
                IsActive = true
            },
            new()
            {
                Key = "divide",
                DisplayName = "חילוק (/)",
                Category = OperationCategory.Arithmetic,
                RuleTemplate = "A / B",
                FieldAPrompt = "מונה",
                FieldBPrompt = "מכנה",
                Description = "חילוק מונה במכנה עם הגנה משגיאת חלוקה באפס",
                IsActive = true
            },
            new()
            {
                Key = "power",
                DisplayName = "חזקה (^)",
                Category = OperationCategory.Arithmetic,
                RuleTemplate = "Math.Pow(A, B)",
                FieldAPrompt = "בסיס",
                FieldBPrompt = "מעריך",
                Description = "העלאת מספר בסיס בחזקת מעריך",
                IsActive = true
            },
            new()
            {
                Key = "modulo",
                DisplayName = "שארית (%)",
                Category = OperationCategory.Arithmetic,
                RuleTemplate = "A % B",
                FieldAPrompt = "מספר שלם",
                FieldBPrompt = "מחלק שלם",
                Description = "שארית של חלוקת שני מספרים",
                IsActive = true
            },

            // String Operations
            new()
            {
                Key = "concat",
                DisplayName = "שרשור מחרוזות",
                Category = OperationCategory.String,
                RuleTemplate = "{A}{B}",
                FieldAPrompt = "מחרוזת ראשונה",
                FieldBPrompt = "מחרוזת שנייה",
                Description = "שרשור שתי מחרוזות ברצף",
                IsActive = true
            },
            new()
            {
                Key = "join-delim",
                DisplayName = "חיבור עם מפריד",
                Category = OperationCategory.String,
                RuleTemplate = "{A}{delim}{B}",
                FieldAPrompt = "טקסט מקור",
                FieldBPrompt = "תו מפריד",
                Description = "חיבור שתי מחרוזות עם תו מפריד ביניהן",
                IsActive = true
            },
            new()
            {
                Key = "contains",
                DisplayName = "בדיקת הכלה (Contains)",
                Category = OperationCategory.String,
                RuleTemplate = "Contains(A, B)",
                FieldAPrompt = "טקסט לחיפוש",
                FieldBPrompt = "תת-מחרוזת",
                Description = "בדיקה האם הטקסט הראשון מכיל את תת-המחרוזת (ללא תלות ברישיות)",
                IsActive = true
            },
            new()
            {
                Key = "char-frequency",
                DisplayName = "ספירת מופעי תו",
                Category = OperationCategory.String,
                RuleTemplate = "CountChar(A, B)",
                FieldAPrompt = "טקסט מלא",
                FieldBPrompt = "תו לספירה",
                Description = "ספירת מספר הפעמים שתו מסוים מופיע בתוך המחרוזת",
                IsActive = true
            },

            // External API Operations
            new()
            {
                Key = "weather",
                DisplayName = "תחזית מזג אוויר חיה (Open-Meteo)",
                Category = OperationCategory.ExternalApi,
                RuleTemplate = "https://api.open-meteo.com/v1/forecast?latitude={A}&longitude={B}&current=temperature_2m,relative_humidity_2m,wind_speed_10m",
                FieldAPrompt = "קו רוחב (Latitude)",
                FieldBPrompt = "קו אורך (Longitude)",
                Description = "שליפת נתוני מזג אוויר חיים מ-Open-Meteo API לפי קואורדינטות",
                IsActive = true
            },
            new()
            {
                Key = "crypto-price",
                DisplayName = "מחירי קריפטו חיים (CoinGecko)",
                Category = OperationCategory.ExternalApi,
                RuleTemplate = "https://api.coingecko.com/api/v3/simple/price?ids={A}&vs_currencies={B}",
                FieldAPrompt = "מזהה מטבע (e.g. bitcoin, ethereum)",
                FieldBPrompt = "מטבע יעד (e.g. usd, ils, eur)",
                Description = "שליפת מחירי מטבעות קריפטוגרפיים בזמן אמת מ-CoinGecko REST API",
                IsActive = true
            },
            new()
            {
                Key = "predict-age",
                DisplayName = "חיזוי גיל לפי שם (Agify)",
                Category = OperationCategory.ExternalApi,
                RuleTemplate = "https://api.agify.io/?name={A}&country_id={B}",
                FieldAPrompt = "שם פרטי (e.g. michael)",
                FieldBPrompt = "קוד מדינה (e.g. IL, US)",
                Description = "חיזוי גיל דמוגרפי לפי שם פרטי וקוד מדינה מ-Agify REST API",
                IsActive = true
            },
            new()
            {
                Key = "exchange-rate",
                DisplayName = "שערי חליפין (Forex Rates)",
                Category = OperationCategory.ExternalApi,
                RuleTemplate = "https://open.er-api.com/v6/latest/{A}",
                FieldAPrompt = "מטבע בסיס (e.g. USD, EUR, ILS)",
                FieldBPrompt = "לא בשימוש (0)",
                Description = "שערי חליפין בינלאומיים בזמן אמת מ-Exchange Rate API",
                IsActive = true
            },
            new()
            {
                Key = "cat-fact",
                DisplayName = "עובדות חתולים (Cat Fact)",
                Category = OperationCategory.ExternalApi,
                RuleTemplate = "https://catfact.ninja/fact?max_length={A}",
                FieldAPrompt = "אורך מקסימלי (e.g. 50, 100)",
                FieldBPrompt = "לא בשימוש (0)",
                Description = "שליפת עובדה רנדומלית על חתולים מ-CatFact API",
                IsActive = true
            }
        };

        const string seedSql = @"
        IF NOT EXISTS (SELECT 1 FROM Operations WHERE [Key] = @Key)
        BEGIN
            INSERT INTO Operations ([Key], [DisplayName], [Category], [RuleTemplate], [FieldAPrompt], [FieldBPrompt], [Description], [IsActive], [CreatedAt])
            VALUES (@Key, @DisplayName, @Category, @RuleTemplate, @FieldAPrompt, @FieldBPrompt, @Description, @IsActive, SYSUTCDATETIME());
        END";

        foreach (var op in seedOperations)
        {
            await conn.ExecuteAsync(new CommandDefinition(seedSql, new
            {
                op.Key,
                op.DisplayName,
                Category = op.Category.ToString(),
                op.RuleTemplate,
                op.FieldAPrompt,
                op.FieldBPrompt,
                op.Description,
                op.IsActive
            }, cancellationToken: ct));
        }

        _logger.LogInformation("Default operations seeded ({Count} operations verified).", seedOperations.Count);
    }
}
