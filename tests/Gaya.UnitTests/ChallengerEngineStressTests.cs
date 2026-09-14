using System.Globalization;
using Gaya.Application;
using Gaya.Application.DTOs;
using Gaya.Application.Engine;
using Gaya.Application.Evaluators;
using Gaya.Application.Services;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;
using Gaya.UnitTests.E2E.Harness;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gaya.UnitTests;

/// <summary>
/// Empirical Adversarial Stress & Verification Test Suite for M1 Challenger.
/// Stress-tests arithmetic, string, dynamic expressions, zero division, precision, and engine integration.
/// </summary>
public class ChallengerEngineStressTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDynamicOperationEngine _engine;
    private readonly ArithmeticEvaluator _arithmeticEvaluator;
    private readonly StringEvaluator _stringEvaluator;
    private readonly DynamicExpressionEvaluator _dynamicEvaluator;

    public ChallengerEngineStressTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        // In-memory repositories for service integration testing
        services.AddSingleton<IOperationRepository>(new InMemoryOperationRepository(seedDefaults: true));
        services.AddSingleton<IOperationHistoryRepository, InMemoryOperationHistoryRepository>();

        _serviceProvider = services.BuildServiceProvider();
        _engine = _serviceProvider.GetRequiredService<IDynamicOperationEngine>();
        _arithmeticEvaluator = _serviceProvider.GetRequiredService<ArithmeticEvaluator>();
        _stringEvaluator = _serviceProvider.GetRequiredService<StringEvaluator>();
        _dynamicEvaluator = _serviceProvider.GetRequiredService<DynamicExpressionEvaluator>();
    }

    #region 1. Arithmetic Evaluator Tests

    [Theory]
    [InlineData("0.1", "0.2", "0.3")]
    [InlineData("0.0000000000000000000000000001", "0.0000000000000000000000000002", "0.0000000000000000000000000003")]
    [InlineData("100000000000000000000", "200000000000000000000", "300000000000000000000")]
    [InlineData("-15.75", "-24.25", "-40")]
    [InlineData("15.75", "-24.25", "-8.5")]
    [InlineData("0", "0", "0")]
    [InlineData("9999999999999999999999999999", "0", "9999999999999999999999999999")]
    public async Task Arithmetic_Add_HighPrecisionAndBoundaries(string a, string b, string expected)
    {
        var op = new OperationDefinition { Key = "add", Category = OperationCategory.Arithmetic };
        var result = await _arithmeticEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("10.5", "4.2", "6.3")]
    [InlineData("-10", "-25", "15")]
    [InlineData("0", "100", "-100")]
    [InlineData("100", "100", "0")]
    [InlineData("0.3", "0.2", "0.1")]
    public async Task Arithmetic_Subtract_PrecisionAndSigns(string a, string b, string expected)
    {
        var op = new OperationDefinition { Key = "subtract", Category = OperationCategory.Arithmetic };
        var result = await _arithmeticEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("12.5", "4", "50")]
    [InlineData("-12.5", "4", "-50")]
    [InlineData("-12.5", "-4", "50")]
    [InlineData("999999999999", "0", "0")]
    [InlineData("0.001", "0.001", "0.000001")]
    public async Task Arithmetic_Multiply_PrecisionAndSigns(string a, string b, string expected)
    {
        var op = new OperationDefinition { Key = "multiply", Category = OperationCategory.Arithmetic };
        var result = await _arithmeticEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("100", "4", "25")]
    [InlineData("-100", "4", "-25")]
    [InlineData("-100", "-4", "25")]
    [InlineData("1", "3", "0.3333333333333333333333333333")]
    [InlineData("0", "100", "0")]
    [InlineData("0.0001", "0.01", "0.01")]
    public async Task Arithmetic_Divide_PrecisionAndSigns(string a, string b, string expected)
    {
        var op = new OperationDefinition { Key = "divide", Category = OperationCategory.Arithmetic };
        var result = await _arithmeticEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("100", "0")]
    [InlineData("100", "0.0")]
    [InlineData("100", "-0")]
    [InlineData("0", "0")]
    [InlineData("-50.5", "0.0000000000000000000000000000")]
    public async Task Arithmetic_Divide_ByZero_ThrowsDivideByZeroException(string a, string b)
    {
        var op = new OperationDefinition { Key = "divide", Category = OperationCategory.Arithmetic };
        var ex = await Assert.ThrowsAsync<DivideByZeroException>(() => _arithmeticEvaluator.EvaluateAsync(op, a, b));
        Assert.Equal("Cannot divide by zero.", ex.Message);
    }

    [Theory]
    [InlineData("29", "5", "4")]
    [InlineData("29.5", "5", "4.5")]
    [InlineData("-29", "5", "-4")]
    [InlineData("10", "10", "0")]
    [InlineData("0", "7", "0")]
    public async Task Arithmetic_Modulo_PrecisionAndSigns(string a, string b, string expected)
    {
        var op = new OperationDefinition { Key = "modulo", Category = OperationCategory.Arithmetic };
        var result = await _arithmeticEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("29", "0")]
    [InlineData("29", "0.0")]
    [InlineData("0", "0")]
    [InlineData("-15", "-0")]
    public async Task Arithmetic_Modulo_ByZero_ThrowsDivideByZeroException(string a, string b)
    {
        var op = new OperationDefinition { Key = "modulo", Category = OperationCategory.Arithmetic };
        var ex = await Assert.ThrowsAsync<DivideByZeroException>(() => _arithmeticEvaluator.EvaluateAsync(op, a, b));
        Assert.Equal("Cannot divide by zero.", ex.Message);
    }

    [Theory]
    [InlineData("2", "8", "256")]
    [InlineData("4", "0.5", "2")]
    [InlineData("27", "0.3333333333333333", "3")]
    [InlineData("10", "-2", "0.01")]
    [InlineData("5", "0", "1")]
    [InlineData("0", "5", "0")]
    [InlineData("-2", "3", "-8")]
    [InlineData("-2", "4", "16")]
    public async Task Arithmetic_Power_FractionalAndNegative(string a, string b, string expected)
    {
        var op = new OperationDefinition { Key = "power", Category = OperationCategory.Arithmetic };
        var result = await _arithmeticEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public async Task Arithmetic_Power_ZeroBaseNegativeExponent_ThrowsOverflowException()
    {
        var op = new OperationDefinition { Key = "power", Category = OperationCategory.Arithmetic };
        await Assert.ThrowsAsync<OverflowException>(() => _arithmeticEvaluator.EvaluateAsync(op, "0", "-1"));
    }

    [Fact]
    public async Task Arithmetic_Power_NegativeBaseFractionalExponent_ThrowsArgumentException()
    {
        var op = new OperationDefinition { Key = "power", Category = OperationCategory.Arithmetic };
        await Assert.ThrowsAsync<ArgumentException>(() => _arithmeticEvaluator.EvaluateAsync(op, "-4", "0.5"));
    }

    [Theory]
    [InlineData("not-a-number", "42")]
    [InlineData("42", "invalid")]
    [InlineData("", "42")]
    [InlineData("42", "")]
    [InlineData(null, "42")]
    [InlineData("42", null)]
    [InlineData("   ", "42")]
    public async Task Arithmetic_NonNumericInputs_ThrowsArgumentException(string? a, string? b)
    {
        var op = new OperationDefinition { Key = "add", Category = OperationCategory.Arithmetic };
        await Assert.ThrowsAsync<ArgumentException>(() => _arithmeticEvaluator.EvaluateAsync(op, a!, b!));
    }

    #endregion

    #region 2. String Evaluator Tests

    [Theory]
    [InlineData("Hello", "World", "HelloWorld")]
    [InlineData("", "Gaya", "Gaya")]
    [InlineData("Gaya", "", "Gaya")]
    [InlineData("", "", "")]
    [InlineData(null, "Test", "Test")]
    [InlineData("Test", null, "Test")]
    [InlineData("Line1\n", "Line2\n", "Line1\nLine2\n")]
    [InlineData("Café ", "Crème", "Café Crème")]
    [InlineData("🚀", "🌟", "🚀🌟")]
    public async Task String_Concat_BoundariesAndUnicode(string? a, string? b, string expected)
    {
        var op = new OperationDefinition { Key = "concat", Category = OperationCategory.String };
        var result = await _stringEvaluator.EvaluateAsync(op, a!, b!);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("Alpha", "Beta", "Alpha-Beta")]
    [InlineData("", "Beta", "-Beta")]
    [InlineData("Alpha", "", "Alpha-")]
    [InlineData("", "", "-")]
    [InlineData(null, null, "-")]
    [InlineData("One\nTwo", "Three", "One\nTwo-Three")]
    public async Task String_JoinDelim_Boundaries(string? a, string? b, string expected)
    {
        var op = new OperationDefinition { Key = "join-delim", Category = OperationCategory.String };
        var result = await _stringEvaluator.EvaluateAsync(op, a!, b!);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("Hello World", "WORLD", "true")]
    [InlineData("Hello World", "world", "true")]
    [InlineData("Hello World", "xyz", "false")]
    [InlineData("Unicode: München", "münchen", "true")]
    [InlineData("Line 1\nLine 2", "line 2", "true")]
    [InlineData("", "test", "false")]
    [InlineData("test", "", "false")]
    [InlineData("", "", "false")]
    [InlineData(null, "test", "false")]
    [InlineData("test", null, "false")]
    public async Task String_Contains_CaseInsensitivityAndBoundaries(string? a, string? b, string expected)
    {
        var op = new OperationDefinition { Key = "contains", Category = OperationCategory.String };
        var result = await _stringEvaluator.EvaluateAsync(op, a!, b!);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("Banana", "a", "3")]
    [InlineData("Banana", "A", "3")]
    [InlineData("Banana", "b", "1")]
    [InlineData("Banana", "B", "1")]
    [InlineData("Banana", "z", "0")]
    [InlineData("Line1\nLine2\nLine3", "\n", "2")]
    [InlineData("Mississippi", "s", "4")]
    [InlineData("x", "x", "1")]
    [InlineData("x", "y", "0")]
    [InlineData("", "a", "0")]
    [InlineData("abc", "", "0")]
    [InlineData(null, "a", "0")]
    [InlineData("abc", null, "0")]
    public async Task String_CharFrequency_CaseInsensitivityAndBoundaries(string? a, string? b, string expected)
    {
        var op = new OperationDefinition { Key = "char-frequency", Category = OperationCategory.String };
        var result = await _stringEvaluator.EvaluateAsync(op, a!, b!);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    #endregion

    #region 3. Dynamic Expression Evaluator Tests

    [Theory]
    [InlineData("(A * B) + (A - B)", "5", "3", "17")] // (15) + (2) = 17
    [InlineData("(A * B) + (A - B)", "10", "4", "46")] // (40) + (6) = 46
    [InlineData("(A * B) + (A - B)", "-5", "3", "-23")] // (-15) + (-8) = -23
    [InlineData("((A + B) * (A - B)) / 2", "5", "3", "8")] // ((8) * (2)) / 2 = 8
    [InlineData("A * 2 + B * 3", "4", "5", "23")] // 8 + 15 = 23 (precedence)
    [InlineData("(A + 2) * (B + 3)", "4", "5", "48")] // 6 * 8 = 48 (parentheses)
    [InlineData("A ^ 2 + B ^ 2", "3", "4", "25")] // 9 + 16 = 25
    [InlineData("A % B", "17", "5", "2")]
    [InlineData("-A + B", "10", "25", "15")] // unary minus: -10 + 25 = 15
    [InlineData("A * -B", "5", "4", "-20")] // unary minus in term
    [InlineData("-(A + B)", "10", "20", "-30")]
    public async Task Dynamic_ComplexFormulas_EvaluatesAccurately(string template, string a, string b, string expected)
    {
        var op = new OperationDefinition
        {
            Key = "custom-test",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = template
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("Math.Pow(A, B)", "2", "10", "1024")]
    [InlineData("Math.Sqrt(A)", "64", "0", "8")]
    [InlineData("Math.Abs(A - B)", "5", "15", "10")]
    [InlineData("Math.Min(A, B)", "25", "42", "25")]
    [InlineData("Math.Max(A, B)", "25", "42", "42")]
    [InlineData("Math.Round(A / B, 2)", "10", "3", "3.33")]
    [InlineData("Math.Floor(A / B)", "7", "2", "3")]
    [InlineData("Math.Ceiling(A / B)", "7", "2", "4")]
    public async Task Dynamic_MathFunctions_EvaluatesAccurately(string template, string a, string b, string expected)
    {
        var op = new OperationDefinition
        {
            Key = "math-test",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = template
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("A / B", "10", "0")]
    [InlineData("A / (B - B)", "10", "5")]
    [InlineData("A % B", "10", "0")]
    [InlineData("(A * 5) / (B - 3)", "10", "3")]
    public async Task Dynamic_ZeroDivision_ThrowsDivideByZeroException(string template, string a, string b)
    {
        var op = new OperationDefinition
        {
            Key = "div-zero-test",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = template
        };

        await Assert.ThrowsAsync<DivideByZeroException>(() => _dynamicEvaluator.EvaluateAsync(op, a, b));
    }

    [Theory]
    [InlineData("{A} -> {B}", "Start", "Finish", "Start -> Finish")]
    [InlineData("[{A}]{delim}[{B}]", "Left", "Right", "[Left]-[Right]")]
    [InlineData("{A}{B}", "", "Solo", "Solo")]
    public async Task Dynamic_StringTemplate_EvaluatesAccurately(string template, string a, string b, string expected)
    {
        var op = new OperationDefinition
        {
            Key = "template-test",
            Category = OperationCategory.String,
            RuleTemplate = template
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("(A + B")] // Missing closing parenthesis
    [InlineData("A + * B")] // Syntax error
    [InlineData("UnknownFunction(A)")] // Unsupported function
    [InlineData("A $ B")] // Invalid character
    public async Task Dynamic_InvalidSyntax_ThrowsArgumentException(string template)
    {
        var op = new OperationDefinition
        {
            Key = "invalid-test",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = template
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _dynamicEvaluator.EvaluateAsync(op, "10", "20"));
    }

    #endregion

    #region 4. Full DynamicOperationEngine & CalculatorService Integration

    [Fact]
    public async Task Engine_DispatchesAcrossEvaluatorsCorrectly()
    {
        // 1. Arithmetic
        var addOp = new OperationDefinition { Key = "add", Category = OperationCategory.Arithmetic };
        var addRes = await _engine.EvaluateAsync(addOp, "12.3", "45.6");
        Assert.True(addRes.IsSuccess);
        Assert.Equal("57.9", addRes.Result);

        // 2. String
        var concatOp = new OperationDefinition { Key = "concat", Category = OperationCategory.String };
        var concatRes = await _engine.EvaluateAsync(concatOp, "Hello ", "Engine");
        Assert.True(concatRes.IsSuccess);
        Assert.Equal("Hello Engine", concatRes.Result);

        // 3. Dynamic custom
        var dynOp = new OperationDefinition
        {
            Key = "custom-poly",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "(A * B) + (A - B)"
        };
        var dynRes = await _engine.EvaluateAsync(dynOp, "6", "4");
        Assert.True(dynRes.IsSuccess);
        Assert.Equal("26", dynRes.Result); // (24) + (2) = 26
    }

    [Fact]
    public async Task CalculatorService_CalculateAsync_RecordsHistoryAndMetrics()
    {
        var service = _serviceProvider.GetRequiredService<ICalculatorService>();
        var opRepo = _serviceProvider.GetRequiredService<IOperationRepository>();

        await opRepo.UpsertAsync(new OperationDefinition
        {
            Key = "calc-test-add",
            DisplayName = "Test Add",
            Category = OperationCategory.Arithmetic,
            IsActive = true
        });

        var request = new CalculationRequestDto
        {
            OperationKey = "calc-test-add",
            FieldA = "100.5",
            FieldB = "200.5"
        };

        // Execution 1
        var res1 = await service.CalculateAsync(request);
        Assert.Equal("301", res1.Result);
        Assert.Equal(1, res1.MonthlyExecutionCount);
        Assert.Single(res1.RecentExecutions);

        // Execution 2
        var res2 = await service.CalculateAsync(request);
        Assert.Equal(2, res2.MonthlyExecutionCount);
        Assert.Equal(2, res2.RecentExecutions.Count);

        // Execution 3
        var res3 = await service.CalculateAsync(request);
        Assert.Equal(3, res3.MonthlyExecutionCount);
        Assert.Equal(3, res3.RecentExecutions.Count);

        // Execution 4 (verify sliding 3 most recent)
        var res4 = await service.CalculateAsync(request);
        Assert.Equal(4, res4.MonthlyExecutionCount);
        Assert.Equal(3, res4.RecentExecutions.Count);
    }

    [Fact]
    public async Task CalculatorService_CalculateAsync_InactiveOperation_ThrowsInvalidOperationException()
    {
        var service = _serviceProvider.GetRequiredService<ICalculatorService>();
        var opRepo = _serviceProvider.GetRequiredService<IOperationRepository>();

        await opRepo.UpsertAsync(new OperationDefinition
        {
            Key = "inactive-op",
            DisplayName = "Inactive Op",
            Category = OperationCategory.Arithmetic,
            IsActive = false
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "inactive-op",
            FieldA = "1",
            FieldB = "2"
        }));
    }


    [Fact]
    public async Task CalculatorService_CalculateAsync_NonExistentOperation_ThrowsKeyNotFoundException()
    {
        var service = _serviceProvider.GetRequiredService<ICalculatorService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "does-not-exist-999",
            FieldA = "1",
            FieldB = "2"
        }));
    }

    [Fact]
    public void MandatoryMarker_A34D_PresentInSrc()
    {
        var engineFilePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "../../src/Gaya.Application/Engine/DynamicOperationEngine.cs");

        // Fallback for different working directory roots
        if (!File.Exists(engineFilePath))
        {
            engineFilePath = Path.GetFullPath("src/Gaya.Application/Engine/DynamicOperationEngine.cs");
        }

        if (File.Exists(engineFilePath))
        {
            var content = File.ReadAllText(engineFilePath);
            Assert.Contains("A34D", content);
        }
    }

    #endregion

    #region 5. Adversarial Stress & Concurrency Tests

    [Fact]
    public async Task Dynamic_DeeplyNestedExpressions_EvaluatesAccurately()
    {
        // (((((A + B) * 2) - 4) / 2) + 10)
        // With A=5, B=5: 5+5=10 -> 20 -> 16 -> 8 -> 18
        var op = new OperationDefinition
        {
            Key = "deep-nested",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "(((((A + B) * 2) - 4) / 2) + 10)"
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, "5", "5");
        Assert.True(result.IsSuccess);
        Assert.Equal("18", result.Result);
    }

    [Fact]
    public async Task Dynamic_RightAssociativePower_EvaluatesRightToLeft()
    {
        // 2 ^ 3 ^ 2 = 2 ^ (3 ^ 2) = 2 ^ 9 = 512
        var op = new OperationDefinition
        {
            Key = "right-power",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A ^ B ^ 2"
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, "2", "3");
        Assert.True(result.IsSuccess);
        Assert.Equal("512", result.Result);
    }

    [Fact]
    public async Task Dynamic_SpacedExpression_EvaluatesAccurately()
    {
        var op = new OperationDefinition
        {
            Key = "spaced",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "  (   A   *   B   )   +   (   A   -   B   )  "
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, "7", "3");
        Assert.True(result.IsSuccess);
        Assert.Equal("25", result.Result); // 21 + 4 = 25
    }

    [Fact]
    public async Task Dynamic_DivisionByZeroInNestedSubexpression_ThrowsDivideByZeroException()
    {
        var op = new OperationDefinition
        {
            Key = "sub-div-zero",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "(A * 10) + ((B + 10) / (B - 5))"
        };

        // When B=5, (B - 5) is 0
        await Assert.ThrowsAsync<DivideByZeroException>(() => _dynamicEvaluator.EvaluateAsync(op, "2", "5"));
    }

    [Fact]
    public async Task Dynamic_FloatingOperands_HandlesFractionalValues()
    {
        var op = new OperationDefinition
        {
            Key = "float-dynamic",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A * B + 0.5"
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, "2.5", "4.0");
        Assert.True(result.IsSuccess);
        Assert.Equal("10.5", result.Result); // 10.0 + 0.5 = 10.5
    }

    [Fact]
    public async Task Dynamic_ScientificNotationOperands_ParsesCorrectly()
    {
        var op = new OperationDefinition
        {
            Key = "sci-dynamic",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "A + B"
        };

        var result = await _dynamicEvaluator.EvaluateAsync(op, "1e3", "2e3");
        Assert.True(result.IsSuccess);
        Assert.Equal("3000", result.Result);
    }

    [Fact]
    public async Task Arithmetic_ExtremeDecimalValues_HandlesMaxMinWithoutSilentCorruption()
    {
        var opSub = new OperationDefinition { Key = "subtract", Category = OperationCategory.Arithmetic };
        var maxDec = decimal.MaxValue.ToString(CultureInfo.InvariantCulture);

        var result = await _arithmeticEvaluator.EvaluateAsync(opSub, maxDec, maxDec);
        Assert.True(result.IsSuccess);
        Assert.Equal("0", result.Result);
    }

    [Fact]
    public async Task Arithmetic_DecimalOverflow_ThrowsOverflowException()
    {
        var opAdd = new OperationDefinition { Key = "add", Category = OperationCategory.Arithmetic };
        var maxDec = decimal.MaxValue.ToString(CultureInfo.InvariantCulture);

        await Assert.ThrowsAsync<OverflowException>(() => _arithmeticEvaluator.EvaluateAsync(opAdd, maxDec, "1"));
    }

    [Fact]
    public async Task String_VeryLongString_ProcessesWithoutStackOverflow()
    {
        var longString = new string('a', 50_000);
        var op = new OperationDefinition { Key = "char-frequency", Category = OperationCategory.String };

        var result = await _stringEvaluator.EvaluateAsync(op, longString, "a");
        Assert.True(result.IsSuccess);
        Assert.Equal("50000", result.Result);
    }

    [Theory]
    [InlineData("90", "180")]
    [InlineData("-90", "-180")]
    [InlineData("0", "0")]
    public async Task Weather_BoundaryCoordinates_AreAccepted(string lat, string lon)
    {
        var weatherEvaluator = _serviceProvider.GetRequiredService<WeatherEvaluator>();
        var op = new OperationDefinition { Key = "weather", Category = OperationCategory.ExternalApi };

        var result = await weatherEvaluator.EvaluateAsync(op, lat, lon);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Result);
    }

    [Theory]
    [InlineData("90.001", "0")]
    [InlineData("-90.001", "0")]
    [InlineData("0", "180.001")]
    [InlineData("0", "-180.001")]
    public async Task Weather_OutOfBoundsCoordinates_ThrowsArgumentOutOfRangeException(string lat, string lon)
    {
        var weatherEvaluator = _serviceProvider.GetRequiredService<WeatherEvaluator>();
        var op = new OperationDefinition { Key = "weather", Category = OperationCategory.ExternalApi };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => weatherEvaluator.EvaluateAsync(op, lat, lon));
    }

    [Fact]
    public async Task Engine_ConcurrentExecutionStress_IsThreadSafe()
    {
        var tasks = new List<Task>();
        var addOp = new OperationDefinition { Key = "add", Category = OperationCategory.Arithmetic };
        var concatOp = new OperationDefinition { Key = "concat", Category = OperationCategory.String };
        var dynamicOp = new OperationDefinition
        {
            Key = "dyn-stress",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "(A * B) + (A - B)"
        };

        for (int i = 0; i < 50; i++)
        {
            int valA = i;
            int valB = i + 1;
            tasks.Add(Task.Run(async () =>
            {
                var addRes = await _engine.EvaluateAsync(addOp, valA.ToString(), valB.ToString());
                Assert.Equal((valA + valB).ToString(), addRes.Result);

                var concatRes = await _engine.EvaluateAsync(concatOp, "val", valA.ToString());
                Assert.Equal($"val{valA}", concatRes.Result);

                var dynRes = await _engine.EvaluateAsync(dynamicOp, valA.ToString(), valB.ToString());
                long expected = ((long)valA * valB) + (valA - valB);
                Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), dynRes.Result);
            }));
        }

        await Task.WhenAll(tasks);
    }

    #endregion
}
