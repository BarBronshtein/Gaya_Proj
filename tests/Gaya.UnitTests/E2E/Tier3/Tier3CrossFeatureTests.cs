using Gaya.Domain.Entities;
using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier3;

public class Tier3CrossFeatureTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task XF_01_Engine_Plus_Persistence_CalculatesAndPersistsHistory()
    {
        // Act: Execute operation
        var res = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "77",
            FieldB = "33"
        });

        // Assert: Result correct
        Assert.Equal("110", res.Result);

        // Assert: Persisted to repository
        var histories = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("add", 1);
        Assert.Single(histories);
        Assert.Equal("77", histories[0].FieldA);
        Assert.Equal("33", histories[0].FieldB);
        Assert.Equal("110", histories[0].Result);
        Assert.True(histories[0].DurationMs >= 0);
    }

    [Fact]
    public async Task XF_02_Calculation_Plus_BonusMetrics_UpdatesRecentQueueAndMonthlyCount()
    {
        // Act: 4 consecutive calculations
        for (int i = 1; i <= 4; i++)
        {
            var res = await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "subtract",
                FieldA = (i * 10).ToString(),
                FieldB = "5"
            });
            Assert.Equal(((i * 10) - 5).ToString(), res.Result);
        }

        // Assert: Recent executions in last response holds exactly 3 newest
        var metrics = await _harness.GetMetricsAsync("subtract");
        Assert.Equal(4, metrics.MonthlyExecutionCount);
        Assert.Equal(3, metrics.RecentExecutions.Count);
        Assert.Equal("35", metrics.RecentExecutions[0].Result); // 40 - 5
        Assert.Equal("25", metrics.RecentExecutions[1].Result); // 30 - 5
        Assert.Equal("15", metrics.RecentExecutions[2].Result); // 20 - 5
    }

    [Fact]
    public async Task XF_03_Engine_Plus_AuditMiddleware_CapturesVerbatimHttpPayload()
    {
        // Arrange & Act
        var res = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "concat",
            FieldA = "Clean",
            FieldB = "Architecture"
        });

        // Assert
        Assert.Equal("CleanArchitecture", res.Result);

        var auditLogs = await _harness.AuditLogRepository.GetRecentAsync(5);
        var audit = auditLogs.FirstOrDefault(l => l.Path == "/api/calculate");
        Assert.NotNull(audit);
        Assert.Equal(200, audit.StatusCode);
        Assert.Contains("Clean", audit.RequestBody);
        Assert.Contains("Architecture", audit.RequestBody);
        Assert.Contains("CleanArchitecture", audit.ResponseBody);
    }

    [Fact]
    public async Task XF_04_DynamicCreation_Plus_ZeroRestartCalculation()
    {
        // Step 1: Create dynamic operation
        var created = await _harness.CreateOperationAsync(new CreateOperationDto
        {
            Key = "cube",
            DisplayName = "Cube (A^3)",
            Category = "Arithmetic",
            RuleTemplate = "Math.Pow(A, 3)",
            FieldAPrompt = "Base",
            FieldBPrompt = "N/A"
        });
        Assert.Equal("cube", created.Key);

        // Step 2: Immediately execute without restart
        var res = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "cube",
            FieldA = "3",
            FieldB = "3"
        });

        // Assert: 3^3 = 27
        Assert.Equal("27", res.Result);
        Assert.Equal(1, res.MonthlyExecutionCount);
    }

    [Fact]
    public async Task XF_05_CalculationFault_Plus_GlobalException_LogsToSystemErrorsAndAudit()
    {
        // Act: Trigger fault
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
            // Handled
        }

        // Assert: Both SystemErrors and ApiAuditLogs captured fault
        var errors = await _harness.SystemErrorRepository.GetRecentAsync(1);
        Assert.Single(errors);
        Assert.Equal(500, errors[0].StatusCode);
        Assert.Contains("Division by zero", errors[0].ErrorMessage);

        var auditLogs = await _harness.AuditLogRepository.GetRecentAsync(1);
        Assert.Single(auditLogs);
        Assert.Equal(500, auditLogs[0].StatusCode);
    }

    [Fact]
    public async Task XF_06_WeatherApi_Plus_HistoryLatencyTracking()
    {
        // Act: Execute weather calculation
        var res = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "weather",
            FieldA = "51.5074",
            FieldB = "-0.1278" // London
        });

        // Assert: Result contains weather telemetry and latency is tracked
        Assert.NotNull(res.Result);
        Assert.Contains("temperature_2m", res.Result);
        Assert.True(res.DurationMs >= 0);

        var history = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("weather", 1);
        Assert.Single(history);
        Assert.True(history[0].DurationMs >= 0);
    }

    [Fact]
    public async Task XF_07_DynamicConfiguration_Plus_Discovery()
    {
        // Arrange
        await _harness.CreateOperationAsync(new CreateOperationDto
        {
            Key = "area-rectangle",
            DisplayName = "שטח מלבן",
            Category = "Arithmetic",
            RuleTemplate = "A * B",
            FieldAPrompt = "אורך",
            FieldBPrompt = "רוחב"
        });

        // Act: Query operations
        var allOps = await _harness.GetOperationsAsync();
        var discovered = allOps.FirstOrDefault(o => o.Key == "area-rectangle");

        // Assert: Discovered in live list
        Assert.NotNull(discovered);
        Assert.Equal("שטח מלבן", discovered.DisplayName);
        Assert.Equal("אורך", discovered.FieldAPrompt);
        Assert.Equal("רוחב", discovered.FieldBPrompt);
    }

    [Fact]
    public async Task XF_08_Calculation_Plus_LiveMetricsAggregation()
    {
        // Arrange: 2 executions
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "power", FieldA = "5", FieldB = "2" });
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "power", FieldA = "5", FieldB = "3" });

        // Act: Query metrics API
        var metrics = await _harness.GetMetricsAsync("power");

        // Assert
        Assert.Equal("power", metrics.OperationKey);
        Assert.Equal(2, metrics.MonthlyExecutionCount);
        Assert.Equal(2, metrics.RecentExecutions.Count);
        Assert.Equal("125", metrics.RecentExecutions[0].Result); // 5^3
        Assert.Equal("25", metrics.RecentExecutions[1].Result);  // 5^2
    }
}
