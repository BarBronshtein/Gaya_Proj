using System.Diagnostics;
using Gaya.Application.DTOs;
using Gaya.Application.Engine;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gaya.Application.Services;

public class CalculatorService : ICalculatorService
{
    private readonly IOperationRepository _operationRepository;
    private readonly IOperationHistoryRepository _historyRepository;
    private readonly IDynamicOperationEngine _engine;
    private readonly ILogger<CalculatorService> _logger;

    public CalculatorService(
        IOperationRepository operationRepository,
        IOperationHistoryRepository historyRepository,
        IDynamicOperationEngine engine,
        ILogger<CalculatorService> logger)
    {
        _operationRepository = operationRepository ?? throw new ArgumentNullException(nameof(operationRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CalculationResponseDto> CalculateAsync(CalculationRequestDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = request.GetEffectiveKey()?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Operation key is required.", nameof(request));
        }

        var operation = await _operationRepository.GetByKeyAsync(key, ct);
        if (operation == null)
        {
            _logger.LogWarning("Operation '{Key}' was not found", key);
            throw new KeyNotFoundException($"Operation '{key}' was not found.");
        }

        if (!operation.IsActive)
        {
            _logger.LogWarning("Operation '{Key}' is currently deactivated", key);
            throw new InvalidOperationException($"Operation '{key}' is currently inactive.");
        }

        var sw = Stopwatch.StartNew();
        string resultString;

        try
        {
            var evalResult = await _engine.EvaluateAsync(operation, request.FieldA ?? string.Empty, request.FieldB ?? string.Empty, ct);
            if (!evalResult.IsSuccess)
            {
                throw new InvalidOperationException(evalResult.ErrorMessage ?? "Calculation failed.");
            }
            resultString = evalResult.Result;
        }
        finally
        {
            sw.Stop();
        }

        var executedAt = DateTime.UtcNow;
        var durationMs = sw.ElapsedMilliseconds;

        var history = new OperationHistory
        {
            OperationKey = operation.Key,
            FieldA = request.FieldA ?? string.Empty,
            FieldB = request.FieldB ?? string.Empty,
            Result = resultString,
            DurationMs = durationMs,
            ExecutedAt = executedAt
        };

        var startOfMonthUtc = new DateTime(executedAt.Year, executedAt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        IReadOnlyList<OperationHistory> recent;
        int monthlyCount;

        try
        {
            await _historyRepository.AddAsync(history, ct);
            recent = await _historyRepository.GetRecentByOperationKeyAsync(operation.Key, 3, ct);
            monthlyCount = await _historyRepository.GetMonthlyCountAsync(operation.Key, startOfMonthUtc, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist history or query metrics for operation '{OperationKey}'. Safe fallback applied.", operation.Key);
            recent = new List<OperationHistory> { history };
            monthlyCount = 1;
        }

        return new CalculationResponseDto
        {
            OperationKey = operation.Key,
            FieldA = request.FieldA ?? string.Empty,
            FieldB = request.FieldB ?? string.Empty,
            Result = resultString,
            DurationMs = durationMs,
            ExecutedAt = executedAt,
            RecentExecutions = recent.Select(MapToHistoryDto).ToList(),
            MonthlyExecutionCount = monthlyCount
        };
    }

    public async Task<IEnumerable<OperationDto>> GetOperationsAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var operations = await _operationRepository.GetAllAsync(activeOnly, ct);
        return operations.Select(MapToOperationDto).ToList();
    }

    public async Task<OperationDto?> GetOperationByKeyAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var op = await _operationRepository.GetByKeyAsync(key.Trim(), ct);
        return op != null ? MapToOperationDto(op) : null;
    }

    public async Task<OperationDto> CreateOperationAsync(CreateOperationDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Key))
        {
            throw new ArgumentException("Operation key is required.", nameof(dto.Key));
        }

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            throw new ArgumentException("Operation display name is required.", nameof(dto.DisplayName));
        }

        var key = dto.Key.Trim().ToLowerInvariant();
        var category = Enum.TryParse<OperationCategory>(dto.Category, true, out var cat) ? cat : OperationCategory.Arithmetic;

        var ruleTemplate = dto.RuleTemplate?.Trim() ?? string.Empty;

        if (category == OperationCategory.ExternalApi)
        {
            if (string.IsNullOrWhiteSpace(ruleTemplate))
            {
                throw new ArgumentException("External API operations require a valid URL rule template.", nameof(dto.RuleTemplate));
            }

            if (!ruleTemplate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !ruleTemplate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("External API rule template must start with 'http://' or 'https://'.", nameof(dto.RuleTemplate));
            }
        }

        var entity = new OperationDefinition
        {
            Key = key,
            DisplayName = dto.DisplayName.Trim(),
            Category = category,
            RuleTemplate = ruleTemplate,
            FieldAPrompt = string.IsNullOrWhiteSpace(dto.FieldAPrompt) ? "Field A" : dto.FieldAPrompt.Trim(),
            FieldBPrompt = string.IsNullOrWhiteSpace(dto.FieldBPrompt) ? "Field B" : dto.FieldBPrompt.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        await _operationRepository.UpsertAsync(entity, ct);
        _logger.LogInformation("Operation '{Key}' created or updated successfully", key);

        return MapToOperationDto(entity);
    }

    public async Task<OperationMetricsDto> GetMetricsAsync(string operationKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            throw new ArgumentException("Operation key is required.", nameof(operationKey));
        }

        var key = operationKey.Trim();
        var exists = await _operationRepository.ExistsAsync(key, ct);
        if (!exists)
        {
            throw new KeyNotFoundException($"Operation '{key}' was not found.");
        }

        var now = DateTime.UtcNow;
        var startOfMonthUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        IReadOnlyList<OperationHistory> recent;
        int count;

        try
        {
            recent = await _historyRepository.GetRecentByOperationKeyAsync(key, 3, ct);
            count = await _historyRepository.GetMonthlyCountAsync(key, startOfMonthUtc, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load metrics for operation '{OperationKey}'", key);
            recent = Array.Empty<OperationHistory>();
            count = 0;
        }

        return new OperationMetricsDto
        {
            OperationKey = key,
            MonthlyExecutionCount = count,
            RecentExecutions = recent.Select(MapToHistoryDto).ToList()
        };
    }

    public async Task<IEnumerable<OperationHistoryDto>> GetHistoryAsync(int limit = 50, CancellationToken ct = default)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 500);
        var operations = await _operationRepository.GetAllAsync(activeOnly: false, ct);
        var allRecent = new List<OperationHistory>();

        foreach (var op in operations)
        {
            var opHistories = await _historyRepository.GetRecentByOperationKeyAsync(op.Key, effectiveLimit, ct);
            allRecent.AddRange(opHistories);
        }

        return allRecent
            .OrderByDescending(h => h.ExecutedAt)
            .ThenByDescending(h => h.Id)
            .Take(effectiveLimit)
            .Select(MapToHistoryDto)
            .ToList();
    }

    public async Task SetOperationActiveStatusAsync(string key, bool isActive, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Operation key is required.", nameof(key));
        }

        var exists = await _operationRepository.ExistsAsync(key.Trim(), ct);
        if (!exists)
        {
            throw new KeyNotFoundException($"Operation '{key}' was not found.");
        }

        await _operationRepository.SetActiveStatusAsync(key.Trim(), isActive, ct);
        _logger.LogInformation("Operation '{Key}' active status set to {IsActive}", key, isActive);
    }

    private static OperationDto MapToOperationDto(OperationDefinition entity) => new()
    {
        Key = entity.Key,
        DisplayName = entity.DisplayName,
        Category = entity.Category.ToString(),
        RuleTemplate = entity.RuleTemplate,
        FieldAPrompt = entity.FieldAPrompt,
        FieldBPrompt = entity.FieldBPrompt,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc),
        UpdatedAt = entity.UpdatedAt.HasValue ? DateTime.SpecifyKind(entity.UpdatedAt.Value, DateTimeKind.Utc) : null
    };

    private static OperationHistoryDto MapToHistoryDto(OperationHistory entity) => new()
    {
        Id = entity.Id,
        OperationKey = entity.OperationKey,
        FieldA = entity.FieldA,
        FieldB = entity.FieldB,
        Result = entity.Result,
        DurationMs = entity.DurationMs,
        ExecutedAt = DateTime.SpecifyKind(entity.ExecutedAt, DateTimeKind.Utc)
    };
}
