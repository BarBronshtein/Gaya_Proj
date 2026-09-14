using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier1;

public class Tier1FeatureCoverageEngineTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task T1_R1_01_Execute_Arithmetic_Add_ReturnsSum()
    {
        // Arrange
        var request = new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "15.5",
            FieldB = "24.5"
        };

        // Act
        var response = await _harness.CalculateAsync(request);

        // Assert
        Assert.Equal("add", response.OperationKey);
        Assert.Equal("40", response.Result);
        Assert.True(response.DurationMs >= 0);
        Assert.Equal(1, response.MonthlyExecutionCount);
        Assert.Single(response.RecentExecutions);
    }

    [Fact]
    public async Task T1_R1_02_Execute_Arithmetic_SubtractMultiplyDivide_ReturnsAccurateResults()
    {
        // Subtract
        var subRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "subtract",
            FieldA = "100",
            FieldB = "4"
        });
        Assert.Equal("96", subRes.Result);

        // Multiply
        var mulRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "multiply",
            FieldA = "100",
            FieldB = "4"
        });
        Assert.Equal("400", mulRes.Result);

        // Divide
        var divRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "divide",
            FieldA = "100",
            FieldB = "4"
        });
        Assert.Equal("25", divRes.Result);
    }

    [Fact]
    public async Task T1_R1_03_Execute_Arithmetic_PowerAndModulo_ReturnsAccurateResults()
    {
        // Power: 2 ^ 8 = 256
        var powRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "power",
            FieldA = "2",
            FieldB = "8"
        });
        Assert.Equal("256", powRes.Result);

        // Modulo: 29 % 5 = 4
        var modRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "modulo",
            FieldA = "29",
            FieldB = "5"
        });
        Assert.Equal("4", modRes.Result);
    }

    [Fact]
    public async Task T1_R1_04_Execute_String_ConcatAndJoinDelim_ReturnsCombinedString()
    {
        // Concat: Gaya + Platform = GayaPlatform
        var concatRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "concat",
            FieldA = "Gaya",
            FieldB = "Platform"
        });
        Assert.Equal("GayaPlatform", concatRes.Result);

        // Join-delim: Gaya + Platform = Gaya-Platform
        var delimRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "join-delim",
            FieldA = "Gaya",
            FieldB = "Platform"
        });
        Assert.Equal("Gaya-Platform", delimRes.Result);
    }

    [Fact]
    public async Task T1_R1_05_Execute_String_ContainsAndCharFrequency_ReturnsValidMetrics()
    {
        // Contains: "operations" contains "tion" -> "true"
        var containsRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "contains",
            FieldA = "operations",
            FieldB = "tion"
        });
        Assert.Equal("true", containsRes.Result);

        // Char-frequency: "banana" contains 'a' 3 times -> "3"
        var freqRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "char-frequency",
            FieldA = "banana",
            FieldB = "a"
        });
        Assert.Equal("3", freqRes.Result);
    }

    [Fact]
    public async Task T1_R1_06_Execute_ExternalApi_Weather_ReturnsParsedForecast()
    {
        // Arrange (Tel Aviv coordinates)
        var request = new CalculationRequestDto
        {
            OperationKey = "weather",
            FieldA = "32.0853",
            FieldB = "34.7818"
        };

        // Act
        var response = await _harness.CalculateAsync(request);

        // Assert
        Assert.NotNull(response.Result);
        Assert.Contains("temperature_2m", response.Result);
        Assert.Contains("32.0853", response.Result);
        Assert.Contains("34.7818", response.Result);
    }

    [Fact]
    public async Task T1_R1_07_Execute_DynamicCustomOperation_EvaluatesFormulaWithoutRecompilation()
    {
        // Arrange: Create new dynamic operation
        var created = await _harness.CreateOperationAsync(new CreateOperationDto
        {
            Key = "double-and-add",
            DisplayName = "Double A and Add B",
            Category = "Arithmetic",
            RuleTemplate = "(A * 2) + B",
            FieldAPrompt = "Base",
            FieldBPrompt = "Offset"
        });
        Assert.Equal("double-and-add", created.Key);

        // Act: Execute newly added operation immediately
        var response = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "double-and-add",
            FieldA = "5",
            FieldB = "10"
        });

        // Assert: (5 * 2) + 10 = 20
        Assert.Equal("20", response.Result);
    }
}
