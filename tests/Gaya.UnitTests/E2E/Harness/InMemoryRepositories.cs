using System.Collections.Concurrent;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;

namespace Gaya.UnitTests.E2E.Harness;

public class InMemoryOperationRepository : IOperationRepository
{
    private readonly ConcurrentDictionary<string, OperationDefinition> _operations = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryOperationRepository(bool seedDefaults = true)
    {
        if (seedDefaults)
        {
            SeedDefaults();
        }
    }

    public Task<IReadOnlyList<OperationDefinition>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var list = _operations.Values
            .Where(op => !activeOnly || op.IsActive)
            .OrderBy(op => op.Key)
            .ToList();
        return Task.FromResult<IReadOnlyList<OperationDefinition>>(list);
    }

    public Task<OperationDefinition?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        _operations.TryGetValue(key, out var op);
        return Task.FromResult(op);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(_operations.ContainsKey(key));
    }

    public Task UpsertAsync(OperationDefinition operation, CancellationToken ct = default)
    {
        _operations.AddOrUpdate(operation.Key, operation, (_, _) => operation);
        return Task.CompletedTask;
    }

    public Task SetActiveStatusAsync(string key, bool isActive, CancellationToken ct = default)
    {
        if (_operations.TryGetValue(key, out var op))
        {
            op.IsActive = isActive;
            op.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public void SeedDefaults()
    {
        var defaults = new List<OperationDefinition>
        {
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
            }
        };

        foreach (var op in defaults)
        {
            _operations[op.Key] = op;
        }
    }
}

public class InMemoryOperationHistoryRepository : IOperationHistoryRepository
{
    private readonly List<OperationHistory> _records = new();
    private readonly object _lock = new();
    private long _idSequence = 0;

    public Task<long> AddAsync(OperationHistory history, CancellationToken ct = default)
    {
        lock (_lock)
        {
            history.Id = Interlocked.Increment(ref _idSequence);
            _records.Add(new OperationHistory
            {
                Id = history.Id,
                OperationKey = history.OperationKey,
                FieldA = history.FieldA,
                FieldB = history.FieldB,
                Result = history.Result,
                DurationMs = history.DurationMs,
                ExecutedAt = history.ExecutedAt
            });
            return Task.FromResult(history.Id);
        }
    }

    public Task<IReadOnlyList<OperationHistory>> GetRecentByOperationKeyAsync(string operationKey, int count = 3, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var results = _records
                .Where(r => string.Equals(r.OperationKey, operationKey, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.ExecutedAt)
                .ThenByDescending(r => r.Id)
                .Take(count)
                .ToList();

            return Task.FromResult<IReadOnlyList<OperationHistory>>(results);
        }
    }

    public Task<int> GetMonthlyCountAsync(string operationKey, DateTime startOfMonthUtc, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var total = _records.Count(r =>
                string.Equals(r.OperationKey, operationKey, StringComparison.OrdinalIgnoreCase) &&
                r.ExecutedAt >= startOfMonthUtc);

            return Task.FromResult(total);
        }
    }

    public IReadOnlyList<OperationHistory> GetAllRecords()
    {
        lock (_lock)
        {
            return _records.ToList();
        }
    }
}

public class InMemoryApiAuditLogRepository : IApiAuditLogRepository
{
    private readonly List<ApiAuditLog> _logs = new();
    private readonly object _lock = new();
    private long _idSequence = 0;

    public Task<long> AddAsync(ApiAuditLog log, CancellationToken ct = default)
    {
        lock (_lock)
        {
            log.Id = Interlocked.Increment(ref _idSequence);
            _logs.Add(new ApiAuditLog
            {
                Id = log.Id,
                Path = log.Path,
                Method = log.Method,
                StatusCode = log.StatusCode,
                LatencyMs = log.LatencyMs,
                RequestBody = log.RequestBody,
                ResponseBody = log.ResponseBody,
                ClientIp = log.ClientIp,
                CreatedAt = log.CreatedAt
            });
            return Task.FromResult(log.Id);
        }
    }

    public Task<IReadOnlyList<ApiAuditLog>> GetRecentAsync(int count = 50, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var list = _logs
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .Take(count)
                .ToList();
            return Task.FromResult<IReadOnlyList<ApiAuditLog>>(list);
        }
    }
}

public class InMemorySystemErrorRepository : ISystemErrorRepository
{
    private readonly List<SystemError> _errors = new();
    private readonly object _lock = new();
    private long _idSequence = 0;

    public Task<long> AddAsync(SystemError error, CancellationToken ct = default)
    {
        lock (_lock)
        {
            error.Id = Interlocked.Increment(ref _idSequence);
            _errors.Add(new SystemError
            {
                Id = error.Id,
                ErrorMessage = error.ErrorMessage,
                StackTrace = error.StackTrace,
                SourcePath = error.SourcePath,
                StatusCode = error.StatusCode,
                CreatedAt = error.CreatedAt
            });
            return Task.FromResult(error.Id);
        }
    }

    public Task<IReadOnlyList<SystemError>> GetRecentAsync(int count = 50, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var list = _errors
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .Take(count)
                .ToList();
            return Task.FromResult<IReadOnlyList<SystemError>>(list);
        }
    }
}
