using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;
using Gaya.UnitTests.E2E.Contracts;

namespace Gaya.UnitTests.E2E.Harness;

public class E2ETestHarness : IDisposable
{
    private readonly HttpClient? _httpClient;
    private readonly bool _useLiveApi;

    private readonly InMemoryOperationRepository _opRepo;
    private readonly InMemoryOperationHistoryRepository _historyRepo;
    private readonly InMemoryApiAuditLogRepository _auditRepo;
    private readonly InMemorySystemErrorRepository _errorRepo;
    private readonly ReferenceCalculationEngine _engine;

    public IOperationRepository OperationRepository => _opRepo;
    public InMemoryOperationHistoryRepository HistoryRepository => _historyRepo;
    public InMemoryApiAuditLogRepository AuditLogRepository => _auditRepo;
    public InMemorySystemErrorRepository SystemErrorRepository => _errorRepo;
    public ReferenceCalculationEngine Engine => _engine;

    public E2ETestHarness()
    {
        var baseUrl = Environment.GetEnvironmentVariable("GAYA_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _useLiveApi = true;
        }

        _opRepo = new InMemoryOperationRepository(seedDefaults: true);
        _historyRepo = new InMemoryOperationHistoryRepository();
        _auditRepo = new InMemoryApiAuditLogRepository();
        _errorRepo = new InMemorySystemErrorRepository();
        _engine = new ReferenceCalculationEngine();
    }

    public async Task<CalculationResponseDto> CalculateAsync(CalculationRequestDto request, CancellationToken ct = default)
    {
        if (_useLiveApi && _httpClient != null)
        {
            var httpRes = await _httpClient.PostAsJsonAsync("/api/calculate", request, ct);
            httpRes.EnsureSuccessStatusCode();
            var body = await httpRes.Content.ReadFromJsonAsync<CalculationResponseDto>(cancellationToken: ct);
            return body ?? throw new InvalidOperationException("Empty response body from /api/calculate");
        }

        return await ProcessCalculationInternalAsync("/api/calculate", request, ct);
    }

    public async Task<CalculationResponseDto> ExecuteAsync(CalculationRequestDto request, CancellationToken ct = default)
    {
        if (_useLiveApi && _httpClient != null)
        {
            var httpRes = await _httpClient.PostAsJsonAsync("/api/execute", request, ct);
            httpRes.EnsureSuccessStatusCode();
            var body = await httpRes.Content.ReadFromJsonAsync<CalculationResponseDto>(cancellationToken: ct);
            return body ?? throw new InvalidOperationException("Empty response body from /api/execute");
        }

        // Interoperable route alias
        return await ProcessCalculationInternalAsync("/api/execute", request, ct);
    }

    private async Task<CalculationResponseDto> ProcessCalculationInternalAsync(string path, CalculationRequestDto request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        var op = await _opRepo.GetByKeyAsync(request.OperationKey, ct);
        if (op == null)
        {
            await _errorRepo.AddAsync(new SystemError
            {
                ErrorMessage = $"Operation '{request.OperationKey}' not found.",
                SourcePath = path,
                StatusCode = 404
            }, ct);

            await _auditRepo.AddAsync(new ApiAuditLog
            {
                Path = path,
                Method = "POST",
                StatusCode = 404,
                LatencyMs = sw.ElapsedMilliseconds,
                RequestBody = JsonSerializer.Serialize(request),
                ResponseBody = $"{{\"detail\":\"Operation not found\"}}",
                ClientIp = "127.0.0.1"
            }, ct);

            throw new KeyNotFoundException($"Operation '{request.OperationKey}' was not found.");
        }

        if (!op.IsActive)
        {
            await _auditRepo.AddAsync(new ApiAuditLog
            {
                Path = path,
                Method = "POST",
                StatusCode = 400,
                LatencyMs = sw.ElapsedMilliseconds,
                RequestBody = JsonSerializer.Serialize(request),
                ResponseBody = $"{{\"detail\":\"Operation is inactive\"}}",
                ClientIp = "127.0.0.1"
            }, ct);

            throw new InvalidOperationException($"Operation '{request.OperationKey}' is inactive.");
        }

        string calcResult;
        try
        {
            calcResult = await _engine.EvaluateAsync(request.OperationKey, request.FieldA, request.FieldB, op.RuleTemplate, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            await _errorRepo.AddAsync(new SystemError
            {
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                SourcePath = path,
                StatusCode = 500
            }, ct);

            await _auditRepo.AddAsync(new ApiAuditLog
            {
                Path = path,
                Method = "POST",
                StatusCode = 500,
                LatencyMs = sw.ElapsedMilliseconds,
                RequestBody = JsonSerializer.Serialize(request),
                ResponseBody = $"{{\"error\":\"{ex.Message}\"}}",
                ClientIp = "127.0.0.1"
            }, ct);

            throw;
        }

        sw.Stop();
        var duration = Math.Max(1, sw.ElapsedMilliseconds);

        // Record history
        var historyRecord = new OperationHistory
        {
            OperationKey = request.OperationKey,
            FieldA = request.FieldA,
            FieldB = request.FieldB,
            Result = calcResult,
            DurationMs = duration,
            ExecutedAt = startTime
        };
        await _historyRepo.AddAsync(historyRecord, ct);

        // Retrieve 3 recent
        var recentHistories = await _historyRepo.GetRecentByOperationKeyAsync(request.OperationKey, 3, ct);
        var recentDtos = recentHistories.Select(h => new OperationHistoryDto
        {
            Id = h.Id,
            OperationKey = h.OperationKey,
            FieldA = h.FieldA,
            FieldB = h.FieldB,
            Result = h.Result,
            DurationMs = h.DurationMs,
            ExecutedAt = h.ExecutedAt
        }).ToList();

        // Calculate monthly count from 1st day of current month UTC
        var startOfMonthUtc = new DateTime(startTime.Year, startTime.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyCount = await _historyRepo.GetMonthlyCountAsync(request.OperationKey, startOfMonthUtc, ct);

        var response = new CalculationResponseDto
        {
            OperationKey = request.OperationKey,
            FieldA = request.FieldA,
            FieldB = request.FieldB,
            Result = calcResult,
            DurationMs = duration,
            ExecutedAt = startTime,
            RecentExecutions = recentDtos,
            MonthlyExecutionCount = monthlyCount
        };

        // Capture Audit Log
        await _auditRepo.AddAsync(new ApiAuditLog
        {
            Path = path,
            Method = "POST",
            StatusCode = 200,
            LatencyMs = duration,
            RequestBody = JsonSerializer.Serialize(request),
            ResponseBody = JsonSerializer.Serialize(response),
            ClientIp = "127.0.0.1"
        }, ct);

        return response;
    }

    public async Task<IReadOnlyList<OperationDto>> GetOperationsAsync(CancellationToken ct = default)
    {
        if (_useLiveApi && _httpClient != null)
        {
            var res = await _httpClient.GetFromJsonAsync<List<OperationDto>>("/api/operations", ct);
            return res ?? new List<OperationDto>();
        }

        var ops = await _opRepo.GetAllAsync(activeOnly: false, ct);
        return ops.Select(o => new OperationDto
        {
            Key = o.Key,
            DisplayName = o.DisplayName,
            Category = o.Category.ToString(),
            RuleTemplate = o.RuleTemplate,
            FieldAPrompt = o.FieldAPrompt,
            FieldBPrompt = o.FieldBPrompt,
            Description = o.Description,
            IsActive = o.IsActive,
            CreatedAt = o.CreatedAt
        }).ToList();
    }

    public async Task<OperationDto> CreateOperationAsync(CreateOperationDto dto, CancellationToken ct = default)
    {
        if (_useLiveApi && _httpClient != null)
        {
            var res = await _httpClient.PostAsJsonAsync("/api/operations", dto, ct);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<OperationDto>(cancellationToken: ct);
            return body ?? throw new InvalidOperationException("Failed to deserialize created operation.");
        }

        if (!Enum.TryParse<OperationCategory>(dto.Category, true, out var category))
        {
            category = OperationCategory.Arithmetic;
        }

        var opDef = new OperationDefinition
        {
            Key = dto.Key,
            DisplayName = dto.DisplayName,
            Category = category,
            RuleTemplate = dto.RuleTemplate,
            FieldAPrompt = dto.FieldAPrompt,
            FieldBPrompt = dto.FieldBPrompt,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _opRepo.UpsertAsync(opDef, ct);

        return new OperationDto
        {
            Key = opDef.Key,
            DisplayName = opDef.DisplayName,
            Category = opDef.Category.ToString(),
            RuleTemplate = opDef.RuleTemplate,
            FieldAPrompt = opDef.FieldAPrompt,
            FieldBPrompt = opDef.FieldBPrompt,
            Description = opDef.Description,
            IsActive = opDef.IsActive,
            CreatedAt = opDef.CreatedAt
        };
    }

    public async Task<OperationMetricsDto> GetMetricsAsync(string operationKey, CancellationToken ct = default)
    {
        if (_useLiveApi && _httpClient != null)
        {
            var res = await _httpClient.GetFromJsonAsync<OperationMetricsDto>($"/api/operations/{operationKey}/metrics", ct);
            return res ?? throw new InvalidOperationException("Empty metrics response.");
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await _historyRepo.GetMonthlyCountAsync(operationKey, startOfMonth, ct);
        var recents = await _historyRepo.GetRecentByOperationKeyAsync(operationKey, 3, ct);

        return new OperationMetricsDto
        {
            OperationKey = operationKey,
            MonthlyExecutionCount = count,
            RecentExecutions = recents.Select(r => new OperationHistoryDto
            {
                Id = r.Id,
                OperationKey = r.OperationKey,
                FieldA = r.FieldA,
                FieldB = r.FieldB,
                Result = r.Result,
                DurationMs = r.DurationMs,
                ExecutedAt = r.ExecutedAt
            }).ToList()
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
