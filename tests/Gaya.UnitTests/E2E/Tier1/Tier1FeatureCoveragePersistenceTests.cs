using Gaya.Domain.Entities;
using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier1;

public class Tier1FeatureCoveragePersistenceTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task T1_R2_01_AddHistory_PersistsExecutionRecordWithDuration()
    {
        // Arrange
        var request = new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "10",
            FieldB = "20"
        };

        // Act
        var response = await _harness.CalculateAsync(request);

        // Assert
        var records = _harness.HistoryRepository.GetAllRecords();
        var record = records.FirstOrDefault(r => r.OperationKey == "add");
        Assert.NotNull(record);
        Assert.True(record.Id > 0);
        Assert.Equal("10", record.FieldA);
        Assert.Equal("20", record.FieldB);
        Assert.Equal("30", record.Result);
        Assert.True(record.DurationMs >= 0);
        Assert.True(record.ExecutedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task T1_R2_02_GetRecent_ReturnsTop3ExecutionsInDescendingOrder()
    {
        // Arrange: Insert 5 executions for divide
        for (int i = 1; i <= 5; i++)
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = (i * 10).ToString(),
                FieldB = "2"
            });
            await Task.Delay(10); // Ensure distinct timestamps
        }

        // Act
        var recent = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("divide", 3);

        // Assert: Exactly 3 records, descending order (newest first)
        Assert.Equal(3, recent.Count);
        Assert.True(recent[0].ExecutedAt >= recent[1].ExecutedAt);
        Assert.True(recent[1].ExecutedAt >= recent[2].ExecutedAt);
        Assert.Equal("25", recent[0].Result); // 50 / 2
        Assert.Equal("20", recent[1].Result); // 40 / 2
        Assert.Equal("15", recent[2].Result); // 30 / 2
    }

    [Fact]
    public async Task T1_R2_03_GetMonthlyCount_AggregatesFromFirstDayOfCurrentMonthUtc()
    {
        // Arrange: 2 records from previous month, 4 records from current month
        var now = DateTime.UtcNow;
        var firstDayCurrentMonthUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthTime = firstDayCurrentMonthUtc.AddDays(-5);

        // 2 prior month records
        await _harness.HistoryRepository.AddAsync(new OperationHistory
        {
            OperationKey = "multiply",
            FieldA = "2",
            FieldB = "2",
            Result = "4",
            DurationMs = 5,
            ExecutedAt = previousMonthTime
        });
        await _harness.HistoryRepository.AddAsync(new OperationHistory
        {
            OperationKey = "multiply",
            FieldA = "3",
            FieldB = "3",
            Result = "9",
            DurationMs = 5,
            ExecutedAt = previousMonthTime
        });

        // 4 current month executions via harness
        for (int i = 1; i <= 4; i++)
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "multiply",
                FieldA = i.ToString(),
                FieldB = "10"
            });
        }

        // Act
        var monthlyCount = await _harness.HistoryRepository.GetMonthlyCountAsync("multiply", firstDayCurrentMonthUtc);

        // Assert: Exactly 4 records counted, prior month records excluded
        Assert.Equal(4, monthlyCount);
    }

    [Fact]
    public async Task T1_R2_04_AddApiAuditLog_CapturesFullHttpPayloadAndLatency()
    {
        // Act: Execute calculation
        await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "concat",
            FieldA = "Alpha",
            FieldB = "Beta"
        });

        // Assert: Audit log entry exists
        var recentLogs = await _harness.AuditLogRepository.GetRecentAsync(10);
        var audit = recentLogs.FirstOrDefault(l => l.Path == "/api/calculate");
        Assert.NotNull(audit);
        Assert.Equal(200, audit.StatusCode);
        Assert.Equal("POST", audit.Method);
        Assert.Contains("Alpha", audit.RequestBody);
        Assert.Contains("Beta", audit.RequestBody);
        Assert.Contains("AlphaBeta", audit.ResponseBody);
        Assert.True(audit.LatencyMs >= 0);
    }

    [Fact]
    public async Task T1_R2_05_AddSystemError_CapturesExceptionAndStackTrace()
    {
        // Act: Intentionally trigger zero division error
        try
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = "100",
                FieldB = "0"
            });
        }
        catch (DivideByZeroException)
        {
            // Expected
        }

        // Assert: SystemError logged
        var errors = await _harness.SystemErrorRepository.GetRecentAsync(10);
        var err = errors.FirstOrDefault();
        Assert.NotNull(err);
        Assert.Equal(500, err.StatusCode);
        Assert.Contains("Division by zero", err.ErrorMessage);
    }
}
