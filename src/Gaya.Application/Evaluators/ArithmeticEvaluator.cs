using System.Globalization;
using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;

namespace Gaya.Application.Evaluators;

public class ArithmeticEvaluator : IOperationEvaluator
{
    private static readonly HashSet<string> SupportedOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "add", "subtract", "multiply", "divide", "power", "modulo"
    };

    public bool CanEvaluate(OperationDefinition operation)
    {
        if (operation.Category != OperationCategory.Arithmetic)
        {
            return false;
        }

        // If a specific rule template exists that requires expression parsing, let DynamicExpressionEvaluator take it unless it's empty
        if (!string.IsNullOrWhiteSpace(operation.RuleTemplate) &&
            !SupportedOperations.Contains(operation.Key) &&
            (operation.RuleTemplate.Contains("(") || operation.RuleTemplate.Contains("{")))
        {
            return false;
        }

        return true;
    }

    public Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default)
    {
        var opKey = (operation.Key ?? string.Empty).ToLowerInvariant();

        if (opKey == "power" || opKey.Contains("pow"))
        {
            if (!double.TryParse(fieldA, NumberStyles.Float, CultureInfo.InvariantCulture, out var da))
            {
                throw new ArgumentException($"Invalid numeric operand for Field A: '{fieldA}'", nameof(fieldA));
            }

            if (!double.TryParse(fieldB, NumberStyles.Float, CultureInfo.InvariantCulture, out var db))
            {
                throw new ArgumentException($"Invalid numeric operand for Field B: '{fieldB}'", nameof(fieldB));
            }

            var powResult = Math.Pow(da, db);
            if (double.IsNaN(powResult))
            {
                throw new ArgumentException("Operation produced an invalid or non-real number.", nameof(fieldA));
            }

            if (double.IsInfinity(powResult))
            {
                throw new OverflowException("Operation resulted in numeric overflow.");
            }

            return Task.FromResult(CalculationResult.Ok(FormatDouble(powResult)));
        }

        if (!decimal.TryParse(fieldA, NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
        {
            throw new ArgumentException($"Invalid numeric operand for Field A: '{fieldA}'", nameof(fieldA));
        }

        if (!decimal.TryParse(fieldB, NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
        {
            throw new ArgumentException($"Invalid numeric operand for Field B: '{fieldB}'", nameof(fieldB));
        }

        decimal result = opKey switch
        {
            _ when opKey == "add" || opKey.Contains("add") || opKey.Contains("plus") => a + b,
            _ when opKey == "subtract" || opKey.Contains("sub") || opKey.Contains("minus") => a - b,
            _ when opKey == "multiply" || opKey.Contains("mul") || opKey.Contains("times") => a * b,
            _ when opKey == "divide" || opKey.Contains("div") => b == 0m ? throw new DivideByZeroException("Cannot divide by zero.") : a / b,
            _ when opKey == "modulo" || opKey.Contains("mod") || opKey.Contains("rem") => b == 0m ? throw new DivideByZeroException("Cannot divide by zero.") : a % b,
            _ => a + b
        };

        return Task.FromResult(CalculationResult.Ok(FormatDecimal(result)));
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);

    private static string FormatDouble(double value) =>
        (value % 1 == 0 && Math.Abs(value) < 1e15)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("G15", CultureInfo.InvariantCulture);
}
