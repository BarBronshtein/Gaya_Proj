using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier4;

public class Tier4RealWorldScenarioTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task Scenario_01_FinancialMultiStepCalculationPipeline()
    {
        // Step 1: Base allocation + bonus (add)
        var step1 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "10000.50",
            FieldB = "2450.75"
        });
        Assert.Equal("12451.25", step1.Result);

        // Step 2: Scale with interest factor 1.05 (multiply)
        var step2 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "multiply",
            FieldA = step1.Result,
            FieldB = "1.05"
        });
        Assert.Equal("13073.8125", step2.Result);

        // Step 3: Split across 4 accounts (divide)
        var step3 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "divide",
            FieldA = "13073.81",
            FieldB = "4"
        });
        Assert.Equal("3268.4525", step3.Result);

        // Step 4: Leftover integer cents (modulo)
        var step4 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "modulo",
            FieldA = "13073",
            FieldB = "4"
        });
        Assert.Equal("1", step4.Result);

        // Verify history integrity: all 4 operations recorded
        var allHistory = _harness.HistoryRepository.GetAllRecords();
        Assert.True(allHistory.Count >= 4);
    }

    [Fact]
    public async Task Scenario_02_MultilingualTextCleansingAndContentIngestion()
    {
        // Step 1: Substring validation (contains)
        var step1 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "contains",
            FieldA = "שלום עולם ומערכת Gaya",
            FieldB = "עולם"
        });
        Assert.Equal("true", step1.Result);

        // Step 2: Bilingual Header Joining (join-delim)
        var step2 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "join-delim",
            FieldA = "כותרת ראשית",
            FieldB = "תיאור משני"
        });
        Assert.Equal("כותרת ראשית-תיאור משני", step2.Result);

        // Step 3: Hebrew Character Frequency (char-frequency)
        var step3 = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "char-frequency",
            FieldA = "שלום לכולם",
            FieldB = "ל"
        });
        Assert.Equal("3", step3.Result);

        // Verify UTF-8 Unicode preservation in history
        var recent = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("contains", 1);
        Assert.Single(recent);
        Assert.Equal("שלום עולם ומערכת Gaya", recent[0].FieldA);
    }

    [Fact]
    public async Task Scenario_03_RealTimeFieldWeatherTelemetryDispatch()
    {
        // Step 1: Query Tel Aviv weather
        var telAviv = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "weather",
            FieldA = "32.0853",
            FieldB = "34.7818"
        });
        Assert.Contains("temperature_2m", telAviv.Result);
        Assert.Contains("32.0853", telAviv.Result);

        // Step 2: Query London weather
        var london = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "weather",
            FieldA = "51.5074",
            FieldB = "-0.1278"
        });
        Assert.Contains("temperature_2m", london.Result);
        Assert.Contains("51.5074", london.Result);

        // Step 3: Out-of-bounds coordinates rejection
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "weather",
                FieldA = "120.0",
                FieldB = "0.0"
            });
        });

        // Verify history contains the 2 successful calls
        var weatherHistories = await _harness.HistoryRepository.GetRecentByOperationKeyAsync("weather", 5);
        Assert.Equal(2, weatherHistories.Count);
    }

    [Fact]
    public async Task Scenario_04_DynamicExtensibilityLifecycle_ZeroDowntimeExtension()
    {
        // Step 1: Verify current operations count
        var initialOps = await _harness.GetOperationsAsync();
        var initialCount = initialOps.Count;

        // Step 2: Admin adds BMI calculator dynamically
        var created = await _harness.CreateOperationAsync(new CreateOperationDto
        {
            Key = "bmi",
            DisplayName = "מחשבון BMI",
            Category = "Arithmetic",
            RuleTemplate = "A / (B * B)",
            FieldAPrompt = "משקל בק\"ג",
            FieldBPrompt = "גובה במטרים",
            Description = "חישוב מסת גוף BMI",
            IsActive = true
        });
        Assert.Equal("bmi", created.Key);

        // Step 3: Operations list includes BMI immediately
        var updatedOps = await _harness.GetOperationsAsync();
        Assert.Equal(initialCount + 1, updatedOps.Count);
        Assert.Contains(updatedOps, o => o.Key == "bmi");

        // Step 4: User calculates BMI (Weight = 80kg, Height = 1.80m)
        // 80 / (1.8 * 1.8) = 80 / 3.24 = 24.69
        var bmiRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "bmi",
            FieldA = "80",
            FieldB = "1.8"
        });
        Assert.Equal("24.69", bmiRes.Result);
        Assert.Equal(1, bmiRes.MonthlyExecutionCount);
        Assert.Single(bmiRes.RecentExecutions);
    }

    [Fact]
    public async Task Scenario_05_FaultTolerance_ErrorTransparencyAndAuditResilience()
    {
        // 1. Divide-by-zero fault
        try
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = "50",
                FieldB = "0"
            });
        }
        catch (DivideByZeroException) { }

        // 2. Malformed numeric input
        try
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "power",
                FieldA = "not_a_number",
                FieldB = "3"
            });
        }
        catch (ArgumentException) { }

        // 3. Unknown operation
        try
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "phantom-op",
                FieldA = "1",
                FieldB = "1"
            });
        }
        catch (KeyNotFoundException) { }

        // Assert: All 3 failures were logged in ApiAuditLogs and SystemErrors
        var auditLogs = await _harness.AuditLogRepository.GetRecentAsync(10);
        Assert.True(auditLogs.Count >= 3);

        var errors = await _harness.SystemErrorRepository.GetRecentAsync(10);
        Assert.True(errors.Count >= 3);
    }
}
