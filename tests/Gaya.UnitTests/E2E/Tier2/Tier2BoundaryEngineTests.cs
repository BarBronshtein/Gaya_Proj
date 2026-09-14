using Gaya.UnitTests.E2E.Contracts;
using Gaya.UnitTests.E2E.Harness;
using Xunit;

namespace Gaya.UnitTests.E2E.Tier2;

public class Tier2BoundaryEngineTests : IDisposable
{
    private readonly E2ETestHarness _harness = new();

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public async Task T2_R1_01_Divide_ByZero_ThrowsDivideByZeroException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DivideByZeroException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "divide",
                FieldA = "100",
                FieldB = "0"
            });
        });
    }

    [Fact]
    public async Task T2_R1_02_Modulo_ByZero_ThrowsDivideByZeroException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DivideByZeroException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "modulo",
                FieldA = "29",
                FieldB = "0"
            });
        });
    }

    [Fact]
    public async Task T2_R1_03_Arithmetic_FloatPrecision_AvoidsDrift()
    {
        // Act: 0.1 + 0.2 in standard IEEE-754 often drifts to 0.30000000000000004
        var response = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "add",
            FieldA = "0.1",
            FieldB = "0.2"
        });

        // Assert: Exact decimal precision
        Assert.Equal("0.3", response.Result);
    }

    [Fact]
    public async Task T2_R1_04_Arithmetic_NonNumericInput_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "multiply",
                FieldA = "not-a-number",
                FieldB = "42"
            });
        });
    }

    [Fact]
    public async Task T2_R1_05_Arithmetic_ExtremeValues_HandlesLargeMagnitudes()
    {
        // Arrange
        var request = new CalculationRequestDto
        {
            OperationKey = "power",
            FieldA = "10",
            FieldB = "10"
        };

        // Act: 10^10 = 10000000000
        var response = await _harness.CalculateAsync(request);

        // Assert
        Assert.Equal("10000000000", response.Result);
    }

    [Fact]
    public async Task T2_R1_06_String_EmptyAndNullOperands_HandledGracefully()
    {
        // Concat with empty string
        var concatRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "concat",
            FieldA = "",
            FieldB = "Gaya"
        });
        Assert.Equal("Gaya", concatRes.Result);

        // Contains with empty target
        var containsRes = await _harness.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "contains",
            FieldA = "",
            FieldB = "test"
        });
        Assert.Equal("false", containsRes.Result);
    }

    [Theory]
    [InlineData("95.0", "34.0")]   // Lat > 90
    [InlineData("-95.0", "34.0")]  // Lat < -90
    [InlineData("32.0", "200.0")]  // Lon > 180
    [InlineData("32.0", "-200.0")] // Lon < -180
    public async Task T2_R1_07_Weather_InvalidCoordinates_ThrowsArgumentOutOfRangeException(string lat, string lon)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await _harness.CalculateAsync(new CalculationRequestDto
            {
                OperationKey = "weather",
                FieldA = lat,
                FieldB = lon
            });
        });
    }
}
