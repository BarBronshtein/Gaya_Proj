using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier1;

public class Tier1FeatureCoverageApiTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task T1_R3_01_GetOperations_ReturnsAllActiveOperationsWithPrompts()
    {
        // Act
        var operations = await _harness.GetOperationsAsync();

        // Assert: Seeded operations present
        Assert.NotNull(operations);
        Assert.True(operations.Count >= 11, "Default configuration should contain at least 11 seeded operations.");

        var addOp = operations.FirstOrDefault(o => o.Key == "add");
        Assert.NotNull(addOp);
        Assert.Equal("מספר ראשון", addOp.FieldAPrompt);
        Assert.Equal("מספר שני", addOp.FieldBPrompt);
        Assert.True(addOp.IsActive);

        var weatherOp = operations.FirstOrDefault(o => o.Key == "weather");
        Assert.NotNull(weatherOp);
        Assert.Equal("קו רוחב (Latitude)", weatherOp.FieldAPrompt);
        Assert.Equal("קו אורך (Longitude)", weatherOp.FieldBPrompt);
    }

    [Fact]
    public async Task T1_R3_02_PostCalculate_ValidPayload_ReturnsResultRecentAndMonthlyCount()
    {
        // Act
        var response = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "multiply",
            FieldA = "12",
            FieldB = "12"
        });

        // Assert
        Assert.Equal("multiply", response.OperationKey);
        Assert.Equal("144", response.Result);
        Assert.True(response.DurationMs >= 0);
        Assert.True(response.MonthlyExecutionCount >= 1);
        Assert.NotEmpty(response.RecentExecutions);
    }

    [Fact]
    public async Task T1_R3_03_PostOperations_CreatesNewDynamicOperation()
    {
        // Arrange
        var newOp = new CreateOperationDto
        {
            Key = "vat-calculator",
            DisplayName = "מע\"מ 17%",
            Category = "Arithmetic",
            RuleTemplate = "A * 1.17",
            FieldAPrompt = "סכום לפני מע\"מ",
            FieldBPrompt = "אחוז (קבוע)",
            Description = "חישוב מחיר כולל מע\"מ",
            IsActive = true
        };

        // Act
        var created = await _harness.CreateOperationAsync(newOp);

        // Assert: Operation created
        Assert.Equal("vat-calculator", created.Key);
        Assert.Equal("מע\"מ 17%", created.DisplayName);

        // Verify retrieval via GetOperations
        var ops = await _harness.GetOperationsAsync();
        var retrieved = ops.FirstOrDefault(o => o.Key == "vat-calculator");
        Assert.NotNull(retrieved);
        Assert.Equal("סכום לפני מע\"מ", retrieved.FieldAPrompt);
    }

    [Fact]
    public async Task T1_R3_04_PostExecute_RouteAlias_ProducesIdenticalContractToCalculate()
    {
        // Act: Execute via alias
        var response = await _harness.ExecuteAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "50",
            FieldB = "50"
        });

        // Assert: Produces identical contract to /api/calculate
        Assert.Equal("add", response.OperationKey);
        Assert.Equal("100", response.Result);
        Assert.True(response.DurationMs >= 0);
        Assert.True(response.MonthlyExecutionCount >= 1);
        Assert.NotEmpty(response.RecentExecutions);
    }

    [Fact]
    public async Task T1_R3_05_GetMetrics_ReturnsRecentAndMonthlyCountForSpecificOperation()
    {
        // Arrange: Run 2 executions for power
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "power", FieldA = "2", FieldB = "3" });
        await _harness.CalculateAsync(new CalculationRequestDto { OperationKey = "power", FieldA = "3", FieldB = "3" });

        // Act
        var metrics = await _harness.GetMetricsAsync("power");

        // Assert
        Assert.Equal("power", metrics.OperationKey);
        Assert.True(metrics.MonthlyExecutionCount >= 2);
        Assert.True(metrics.RecentExecutions.Count >= 2);
    }
}
