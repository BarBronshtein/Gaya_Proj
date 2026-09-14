using System.Collections.Concurrent;
using System.Diagnostics;
using Gaya.Application.Common.Models;
using Gaya.Application.DTOs;
using Gaya.Application.Engine;
using Gaya.Application.Evaluators;
using Gaya.Application.Services;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Gaya.UnitTests;

/// <summary>
/// Empirical Stress & Boundary Test Suite for CalculatorService, Duration Tracking,
/// History Persistence, 3 Recent Executions Ordering, and UTC 1st-of-Month Metrics.
/// Executed by m1_challenger_2.
/// </summary>
public class CalculatorServiceStressTests
{
    private readonly InMemoryOperationRepository _operationRepo;
    private readonly TrackingHistoryRepository _historyRepo;
    private readonly DynamicOperationEngine _engine;
    private readonly CalculatorService _sut;

    public CalculatorServiceStressTests()
    {
        _operationRepo = new InMemoryOperationRepository();
        SeedOperations(_operationRepo);

        _historyRepo = new TrackingHistoryRepository();

        var evaluators = new IOperationEvaluator[]
        {
            new ArithmeticEvaluator(),
            new StringEvaluator(),
            new DynamicExpressionEvaluator()
        };

        _engine = new DynamicOperationEngine(evaluators, NullLogger<DynamicOperationEngine>.Instance);

        _sut = new CalculatorService(
            _operationRepo,
            _historyRepo,
            _engine,
            NullLogger<CalculatorService>.Instance);
    }

    #region 1. Call Order & Persistence Before Querying

    [Fact]
    public async Task PersistenceOrder_AddAsyncMustPrecedeRecentAndMonthlyCountQueries()
    {
        // Arrange
        var request = new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "15",
            FieldB = "25"
        };

        // Act
        var response = await _sut.CalculateAsync(request);

        // Assert: Verify exact call sequence
        var calls = _historyRepo.CallLog;
        Assert.True(calls.Count >= 3, $"Expected at least 3 calls, got: {string.Join(", ", calls)}");

        var addIndex = calls.FindIndex(c => c.StartsWith("AddAsync"));
        var recentIndex = calls.FindIndex(c => c.StartsWith("GetRecentByOperationKeyAsync"));
        var countIndex = calls.FindIndex(c => c.StartsWith("GetMonthlyCountAsync"));

        Assert.True(addIndex >= 0, "AddAsync was not invoked.");
        Assert.True(recentIndex >= 0, "GetRecentByOperationKeyAsync was not invoked.");
        Assert.True(countIndex >= 0, "GetMonthlyCountAsync was not invoked.");

        Assert.True(addIndex < recentIndex, $"AddAsync (index {addIndex}) must be called BEFORE GetRecentByOperationKeyAsync (index {recentIndex})");
        Assert.True(addIndex < countIndex, $"AddAsync (index {addIndex}) must be called BEFORE GetMonthlyCountAsync (index {countIndex})");

        // Newly persisted execution must be included in the response recent executions
        Assert.NotEmpty(response.RecentExecutions);
        Assert.Equal("40", response.Result);
        Assert.Equal("40", response.RecentExecutions[0].Result);
        Assert.Equal(1, response.MonthlyExecutionCount);
    }

    #endregion

    #region 2. Execution Duration Tracking

    [Fact]
    public async Task DurationTracking_ShouldBeNonNegativeForFastCalculation()
    {
        // Act
        var response = await _sut.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "multiply",
            FieldA = "7",
            FieldB = "8"
        });

        // Assert
        Assert.True(response.DurationMs >= 0, $"DurationMs must be >= 0, got {response.DurationMs}");

        // Also check persisted record in repository
        var saved = _historyRepo.AllRecords.LastOrDefault();
        Assert.NotNull(saved);
        Assert.Equal(response.DurationMs, saved.DurationMs);
    }

    [Fact]
    public async Task DurationTracking_SimulatedDelay_ShouldProperlyMeasureElapsedTime()
    {
        // Arrange: Use a custom engine with simulated 50ms delay
        var delayedEngine = new DelayedMockEngine(delayMs: 60);
        var sut = new CalculatorService(_operationRepo, _historyRepo, delayedEngine, NullLogger<CalculatorService>.Instance);

        // Act
        var response = await sut.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "10",
            FieldB = "20"
        });

        // Assert: Duration must reflect at least 45ms
        Assert.True(response.DurationMs >= 45, $"DurationMs was {response.DurationMs}, expected >= 45ms");
        var saved = _historyRepo.AllRecords.LastOrDefault();
        Assert.NotNull(saved);
        Assert.True(saved.DurationMs >= 45);
    }

    [Fact]
    public async Task DurationTracking_WhenCalculationFails_NoHistoryIsPersisted()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DivideByZeroException>(async () =>
        {
            await _sut.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = "100",
                FieldB = "0"
            });
        });

        // Persistence must NOT have been called
        Assert.Empty(_historyRepo.AllRecords);
        Assert.DoesNotContain(_historyRepo.CallLog, c => c.StartsWith("AddAsync"));
    }

    #endregion

    #region 3. Recent Executions Query (Strict Top-3 Limit & Descending Order)

    [Fact]
    public async Task RecentExecutions_UnderflowAndExactBoundaries_ReturnsAccurateCount()
    {
        // 0 prior: 1st calculation -> returns 1 recent
        var r1 = await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "1", FieldB = "1" });
        Assert.Single(r1.RecentExecutions);
        Assert.Equal("2", r1.RecentExecutions[0].Result);

        // 1 prior: 2nd calculation -> returns 2 recent
        var r2 = await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "2", FieldB = "2" });
        Assert.Equal(2, r2.RecentExecutions.Count);
        Assert.Equal("4", r2.RecentExecutions[0].Result);
        Assert.Equal("2", r2.RecentExecutions[1].Result);

        // 2 prior: 3rd calculation -> returns exactly 3 recent
        var r3 = await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "3", FieldB = "3" });
        Assert.Equal(3, r3.RecentExecutions.Count);
        Assert.Equal("6", r3.RecentExecutions[0].Result);
        Assert.Equal("4", r3.RecentExecutions[1].Result);
        Assert.Equal("2", r3.RecentExecutions[2].Result);

        // 3 prior: 4th calculation -> strictly capped at 3 recent
        var r4 = await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = "4", FieldB = "4" });
        Assert.Equal(3, r4.RecentExecutions.Count);
        Assert.Equal("8", r4.RecentExecutions[0].Result);
        Assert.Equal("6", r4.RecentExecutions[1].Result);
        Assert.Equal("4", r4.RecentExecutions[2].Result);
    }

    [Fact]
    public async Task RecentExecutions_StressOverflow_StrictlyCapsAtTop3Newest()
    {
        // Arrange: Perform 10 consecutive calculations
        for (int i = 1; i <= 10; i++)
        {
            await _sut.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "subtract",
                FieldA = (i * 10).ToString(),
                FieldB = "5"
            });
            await Task.Delay(2); // Ensure ascending timestamps
        }

        // Act
        var metrics = await _sut.GetMetricsAsync("subtract");

        // Assert
        Assert.Equal(3, metrics.RecentExecutions.Count);
        Assert.Equal("95", metrics.RecentExecutions[0].Result); // 100 - 5
        Assert.Equal("85", metrics.RecentExecutions[1].Result); // 90 - 5
        Assert.Equal("75", metrics.RecentExecutions[2].Result); // 80 - 5

        // Verify timestamps are strictly descending
        Assert.True(metrics.RecentExecutions[0].ExecutedAt >= metrics.RecentExecutions[1].ExecutedAt);
        Assert.True(metrics.RecentExecutions[1].ExecutedAt >= metrics.RecentExecutions[2].ExecutedAt);
    }

    #endregion

    #region 4. UTC 1st of Month Count Aggregation

    [Fact]
    public async Task MonthlyCount_StrictlyFiltersFrom000000UtcOn1stOfCurrentMonth()
    {
        var nowUtc = DateTime.UtcNow;
        var startOfMonthUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Inject records across boundaries:
        // 1. Prior month boundary: 1 millisecond before 1st of current month (MUST BE EXCLUDED)
        await _historyRepo.AddAsync(new OperationHistory
        {
            OperationKey = "multiply",
            FieldA = "1",
            FieldB = "1",
            Result = "1",
            ExecutedAt = startOfMonthUtc.AddMilliseconds(-1)
        });

        // 2. Prior month: 5 days prior (MUST BE EXCLUDED)
        await _historyRepo.AddAsync(new OperationHistory
        {
            OperationKey = "multiply",
            FieldA = "2",
            FieldB = "2",
            Result = "4",
            ExecutedAt = startOfMonthUtc.AddDays(-5)
        });

        // 3. Exact 1st of current month at 00:00:00.000 UTC (MUST BE INCLUDED)
        await _historyRepo.AddAsync(new OperationHistory
        {
            OperationKey = "multiply",
            FieldA = "3",
            FieldB = "3",
            Result = "9",
            ExecutedAt = startOfMonthUtc
        });

        // 4. Current month: 1 hour into 1st of month (MUST BE INCLUDED)
        await _historyRepo.AddAsync(new OperationHistory
        {
            OperationKey = "multiply",
            FieldA = "4",
            FieldB = "4",
            Result = "16",
            ExecutedAt = startOfMonthUtc.AddHours(1)
        });

        // Act: Execute a new calculation now (MUST BE INCLUDED as 3rd current-month record)
        var response = await _sut.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "multiply",
            FieldA = "5",
            FieldB = "5"
        });

        // Assert
        Assert.Equal(3, response.MonthlyExecutionCount);

        // Verify metrics endpoint returns identical count
        var metrics = await _sut.GetMetricsAsync("multiply");
        Assert.Equal(3, metrics.MonthlyExecutionCount);
    }

    [Theory]
    [InlineData(2026, 1, 1)]  // Year rollover (January 1st)
    [InlineData(2024, 2, 29)] // Leap year boundary (Feb 29th)
    [InlineData(2026, 12, 31)] // End of year boundary
    public void StartOfMonthUtc_MathematicalIntegrity_AlwaysProducesMidnightUtcFirstDay(int year, int month, int day)
    {
        var sampleDate = new DateTime(year, month, day, 15, 30, 45, DateTimeKind.Utc);
        var startOfMonth = new DateTime(sampleDate.Year, sampleDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(year, startOfMonth.Year);
        Assert.Equal(month, startOfMonth.Month);
        Assert.Equal(1, startOfMonth.Day);
        Assert.Equal(0, startOfMonth.Hour);
        Assert.Equal(0, startOfMonth.Minute);
        Assert.Equal(0, startOfMonth.Second);
        Assert.Equal(DateTimeKind.Utc, startOfMonth.Kind);
    }

    #endregion

    #region 5. Key Isolation (No Cross-Contamination)

    [Fact]
    public async Task KeyIsolation_ExecutionsDoNotLeakBetweenOperations()
    {
        // Arrange: 4 operations on 'add'
        for (int i = 0; i < 4; i++)
        {
            await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "add", FieldA = i.ToString(), FieldB = "1" });
        }

        // 2 operations on 'multiply'
        for (int i = 0; i < 2; i++)
        {
            await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "multiply", FieldA = i.ToString(), FieldB = "2" });
        }

        // 1 operation on 'concat'
        await _sut.CalculateAsync(new CalculationRequestDto { OperationKey = "concat", FieldA = "Hello", FieldB = "World" });

        // Act
        var addMetrics = await _sut.GetMetricsAsync("add");
        var multMetrics = await _sut.GetMetricsAsync("multiply");
        var concatMetrics = await _sut.GetMetricsAsync("concat");

        // Assert: Add
        Assert.Equal(4, addMetrics.MonthlyExecutionCount);
        Assert.Equal(3, addMetrics.RecentExecutions.Count);
        Assert.All(addMetrics.RecentExecutions, r => Assert.Equal("add", r.OperationKey));

        // Assert: Multiply
        Assert.Equal(2, multMetrics.MonthlyExecutionCount);
        Assert.Equal(2, multMetrics.RecentExecutions.Count);
        Assert.All(multMetrics.RecentExecutions, r => Assert.Equal("multiply", r.OperationKey));

        // Assert: Concat
        Assert.Equal(1, concatMetrics.MonthlyExecutionCount);
        Assert.Single(concatMetrics.RecentExecutions);
        Assert.Equal("concat", concatMetrics.RecentExecutions[0].OperationKey);
        Assert.Equal("HelloWorld", concatMetrics.RecentExecutions[0].Result);
    }

    #endregion

    #region 6. Resilience & Safe Fallback

    [Fact]
    public async Task Resilience_WhenPersistenceThrows_CalculationStillReturnsGracefully()
    {
        // Arrange: Configure history repo to throw on AddAsync
        _historyRepo.ThrowOnAdd = true;

        // Act: Should NOT crash the calculation
        var response = await _sut.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "100",
            FieldB = "200"
        });

        // Assert: Result is computed successfully, safe fallbacks provided
        Assert.Equal("300", response.Result);
        Assert.Equal(1, response.MonthlyExecutionCount);
        Assert.Single(response.RecentExecutions);
        Assert.Equal("300", response.RecentExecutions[0].Result);
    }

    [Fact]
    public async Task Validation_InactiveOperation_ThrowsInvalidOperationException()
    {
        // Arrange: Deactivate 'divide'
        await _sut.SetOperationActiveStatusAsync("divide", false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _sut.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = "10",
                FieldB = "2"
            });
        });

        Assert.Empty(_historyRepo.AllRecords);
    }

    [Fact]
    public async Task Validation_UnknownOperation_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await _sut.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "non_existent_op_99",
                FieldA = "1",
                FieldB = "2"
            });
        });

        Assert.Empty(_historyRepo.AllRecords);
    }

    [Fact]
    public async Task Validation_NullAndEmptyInputs_ThrowAppropriateExceptions()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CalculateAsync(null!));

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "",
            FieldA = "1",
            FieldB = "2"
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "   ",
            FieldA = "1",
            FieldB = "2"
        }));
    }

    #endregion

    #region 7. Concurrency Stress Test

    [Fact]
    public async Task Concurrency_100ParallelExecutions_AccuratelyPersistsAndCounts()
    {
        const int concurrentCount = 100;
        var tasks = new Task<CalculationResponseDto>[concurrentCount];

        for (int i = 0; i < concurrentCount; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(async () =>
            {
                return await _sut.CalculateAsync(new CalculationRequestDto
                {
                    OperationKey = "add",
                    FieldA = idx.ToString(),
                    FieldB = "1"
                });
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert: All 100 completed
        Assert.Equal(concurrentCount, results.Length);
        Assert.All(results, r => Assert.NotEmpty(r.Result));

        // Total records in history repository must be exactly 100
        Assert.Equal(concurrentCount, _historyRepo.AllRecords.Count);

        var metrics = await _sut.GetMetricsAsync("add");
        Assert.Equal(concurrentCount, metrics.MonthlyExecutionCount);
        Assert.Equal(3, metrics.RecentExecutions.Count);
    }

    #endregion

    #region 8. Timezone and UTC DateTimeKind Preservation Tests

    [Fact]
    public async Task DateTimeKind_RecentExecutionsAndHistory_MustHaveUtcKind()
    {
        // Arrange
        var request = new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "10",
            FieldB = "20"
        };

        // Act
        var response = await _sut.CalculateAsync(request);
        var metrics = await _sut.GetMetricsAsync("add");
        var historyList = (await _sut.GetHistoryAsync(10)).ToList();

        // Assert
        Assert.Equal(DateTimeKind.Utc, response.ExecutedAt.Kind);

        Assert.NotEmpty(response.RecentExecutions);
        Assert.All(response.RecentExecutions, r => Assert.Equal(DateTimeKind.Utc, r.ExecutedAt.Kind));

        Assert.NotEmpty(metrics.RecentExecutions);
        Assert.All(metrics.RecentExecutions, r => Assert.Equal(DateTimeKind.Utc, r.ExecutedAt.Kind));

        Assert.NotEmpty(historyList);
        Assert.All(historyList, h => Assert.Equal(DateTimeKind.Utc, h.ExecutedAt.Kind));
    }

    [Fact]
    public async Task JsonSerialization_OperationHistoryDto_SerializesWithUtcZDesignator()
    {
        // Arrange
        var request = new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "5",
            FieldB = "10"
        };
        var response = await _sut.CalculateAsync(request);

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(response);

        // Assert
        Assert.Contains("\"executedAt\":", json);
        Assert.Matches(@"""executedAt"":""\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z""", json);
    }

    #endregion

    #region Helpers & Test Fakes

    private static void SeedOperations(InMemoryOperationRepository repo)
    {
        repo.UpsertAsync(new OperationDefinition
        {
            Key = "add",
            DisplayName = "Add",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A + B",
            IsActive = true
        }).Wait();

        repo.UpsertAsync(new OperationDefinition
        {
            Key = "subtract",
            DisplayName = "Subtract",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A - B",
            IsActive = true
        }).Wait();

        repo.UpsertAsync(new OperationDefinition
        {
            Key = "multiply",
            DisplayName = "Multiply",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A * B",
            IsActive = true
        }).Wait();

        repo.UpsertAsync(new OperationDefinition
        {
            Key = "divide",
            DisplayName = "Divide",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A / B",
            IsActive = true
        }).Wait();

        repo.UpsertAsync(new OperationDefinition
        {
            Key = "concat",
            DisplayName = "Concat",
            Category = OperationCategory.String,
            RuleTemplate = "{A}{B}",
            IsActive = true
        }).Wait();
    }

    private class TrackingHistoryRepository : IOperationHistoryRepository
    {
        private readonly List<OperationHistory> _records = new();
        private readonly object _lock = new();
        private long _idCounter = 0;

        public List<string> CallLog { get; } = new();
        public bool ThrowOnAdd { get; set; }

        public IReadOnlyList<OperationHistory> AllRecords
        {
            get
            {
                lock (_lock)
                {
                    return _records.ToList();
                }
            }
        }

        public Task<long> AddAsync(OperationHistory history, CancellationToken ct = default)
        {
            lock (_lock)
            {
                CallLog.Add($"AddAsync({history.OperationKey}, {history.Result})");

                if (ThrowOnAdd)
                {
                    throw new InvalidOperationException("Simulated database connection failure.");
                }

                history.Id = Interlocked.Increment(ref _idCounter);
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
                CallLog.Add($"GetRecentByOperationKeyAsync({operationKey}, {count})");

                var list = _records
                    .Where(r => string.Equals(r.OperationKey, operationKey, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.ExecutedAt)
                    .ThenByDescending(r => r.Id)
                    .Take(count)
                    .ToList();

                return Task.FromResult<IReadOnlyList<OperationHistory>>(list);
            }
        }

        public Task<int> GetMonthlyCountAsync(string operationKey, DateTime startOfMonthUtc, CancellationToken ct = default)
        {
            lock (_lock)
            {
                CallLog.Add($"GetMonthlyCountAsync({operationKey}, {startOfMonthUtc:u})");

                var total = _records.Count(r =>
                    string.Equals(r.OperationKey, operationKey, StringComparison.OrdinalIgnoreCase) &&
                    r.ExecutedAt >= startOfMonthUtc);

                return Task.FromResult(total);
            }
        }
    }

    private class DelayedMockEngine : IDynamicOperationEngine
    {
        private readonly int _delayMs;

        public DelayedMockEngine(int delayMs)
        {
            _delayMs = delayMs;
        }

        public async Task<CalculationResult> EvaluateAsync(OperationDefinition operation, string fieldA, string fieldB, CancellationToken ct = default)
        {
            await Task.Delay(_delayMs, ct);
            return CalculationResult.Ok("delayed-result");
        }
    }

    private class InMemoryOperationRepository : IOperationRepository
    {
        private readonly ConcurrentDictionary<string, OperationDefinition> _dict = new(StringComparer.OrdinalIgnoreCase);

        public Task<OperationDefinition?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            _dict.TryGetValue(key, out var op);
            return Task.FromResult(op);
        }

        public Task<IReadOnlyList<OperationDefinition>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
        {
            var list = _dict.Values.Where(o => !activeOnly || o.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<OperationDefinition>>(list);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult(_dict.ContainsKey(key));
        }

        public Task UpsertAsync(OperationDefinition operation, CancellationToken ct = default)
        {
            _dict[operation.Key] = operation;
            return Task.CompletedTask;
        }

        public Task SetActiveStatusAsync(string key, bool isActive, CancellationToken ct = default)
        {
            if (_dict.TryGetValue(key, out var op))
            {
                op.IsActive = isActive;
            }
            return Task.CompletedTask;
        }
    }

    #endregion
}
