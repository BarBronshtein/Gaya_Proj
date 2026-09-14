using System.Globalization;
using System.Text.RegularExpressions;

namespace Gaya.UnitTests.E2E.Harness;

public class ReferenceCalculationEngine
{
    public Task<string> EvaluateAsync(string operationKey, string fieldA, string fieldB, string? ruleTemplate = null, CancellationToken ct = default)
    {
        switch (operationKey.ToLowerInvariant())
        {
            // Arithmetic Built-ins
            case "add":
                return Task.FromResult(ExecuteAdd(fieldA, fieldB));

            case "subtract":
                return Task.FromResult(ExecuteSubtract(fieldA, fieldB));

            case "multiply":
                return Task.FromResult(ExecuteMultiply(fieldA, fieldB));

            case "divide":
                return Task.FromResult(ExecuteDivide(fieldA, fieldB));

            case "power":
                return Task.FromResult(ExecutePower(fieldA, fieldB));

            case "modulo":
                return Task.FromResult(ExecuteModulo(fieldA, fieldB));

            // String Built-ins
            case "concat":
                return Task.FromResult(ExecuteConcat(fieldA, fieldB));

            case "join-delim":
                return Task.FromResult(ExecuteJoinDelim(fieldA, fieldB));

            case "contains":
                return Task.FromResult(ExecuteContains(fieldA, fieldB));

            case "char-frequency":
                return Task.FromResult(ExecuteCharFrequency(fieldA, fieldB));

            // External API Built-in
            case "weather":
                return Task.FromResult(ExecuteWeather(fieldA, fieldB));

            // Dynamic / Custom Expression
            default:
                return Task.FromResult(ExecuteDynamicFormula(operationKey, fieldA, fieldB, ruleTemplate));
        }
    }

    private static string ExecuteAdd(string a, string b)
    {
        if (decimal.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var decA) &&
            decimal.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var decB))
        {
            return (decA + decB).ToString("G29", CultureInfo.InvariantCulture);
        }

        if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblA) &&
            double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblB))
        {
            return (dblA + dblB).ToString("G17", CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Invalid numeric operands for addition: '{a}', '{b}'");
    }

    private static string ExecuteSubtract(string a, string b)
    {
        if (decimal.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var decA) &&
            decimal.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var decB))
        {
            return (decA - decB).ToString("G29", CultureInfo.InvariantCulture);
        }

        if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblA) &&
            double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblB))
        {
            return (dblA - dblB).ToString("G17", CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Invalid numeric operands for subtraction: '{a}', '{b}'");
    }

    private static string ExecuteMultiply(string a, string b)
    {
        if (decimal.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var decA) &&
            decimal.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var decB))
        {
            return (decA * decB).ToString("G29", CultureInfo.InvariantCulture);
        }

        if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblA) &&
            double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblB))
        {
            return (dblA * dblB).ToString("G17", CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Invalid numeric operands for multiplication: '{a}', '{b}'");
    }

    private static string ExecuteDivide(string a, string b)
    {
        if (!decimal.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var decA) ||
            !decimal.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var decB))
        {
            if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblA) &&
                double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblB))
            {
                if (dblB == 0.0)
                {
                    throw new DivideByZeroException("Division by zero is undefined.");
                }
                return (dblA / dblB).ToString("G17", CultureInfo.InvariantCulture);
            }

            throw new ArgumentException($"Invalid numeric operands for division: '{a}', '{b}'");
        }

        if (decB == 0m)
        {
            throw new DivideByZeroException("Division by zero is undefined.");
        }

        return (decA / decB).ToString("G29", CultureInfo.InvariantCulture);
    }

    private static string ExecutePower(string a, string b)
    {
        if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblA) &&
            double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var dblB))
        {
            var result = Math.Pow(dblA, dblB);
            return result.ToString("G17", CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Invalid numeric operands for power: '{a}', '{b}'");
    }

    private static string ExecuteModulo(string a, string b)
    {
        if (decimal.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var decA) &&
            decimal.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var decB))
        {
            if (decB == 0m)
            {
                throw new DivideByZeroException("Modulo by zero is undefined.");
            }
            return (decA % decB).ToString("G29", CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Invalid numeric operands for modulo: '{a}', '{b}'");
    }

    private static string ExecuteConcat(string a, string b)
    {
        return $"{a ?? string.Empty}{b ?? string.Empty}";
    }

    private static string ExecuteJoinDelim(string a, string b)
    {
        // Field A is text, Field B is delimiter (or if Field A contains items and B is delimiter)
        // Per seed rule "{A}{delim}{B}" or join items:
        return $"{a ?? string.Empty}-{b ?? string.Empty}";
    }

    private static string ExecuteContains(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return "false";
        }
        return a.Contains(b, StringComparison.OrdinalIgnoreCase) ? "true" : "false";
    }

    private static string ExecuteCharFrequency(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return "0";
        }

        char target = b[0];
        int count = a.Count(c => char.ToLowerInvariant(c) == char.ToLowerInvariant(target));
        return count.ToString(CultureInfo.InvariantCulture);
    }

    private static string ExecuteWeather(string latStr, string lonStr)
    {
        if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            throw new ArgumentException("Latitude and longitude must be valid numeric coordinates.");
        }

        if (lat < -90.0 || lat > 90.0)
        {
            throw new ArgumentOutOfRangeException(nameof(latStr), "Latitude must be between -90 and 90 degrees.");
        }

        if (lon < -180.0 || lon > 180.0)
        {
            throw new ArgumentOutOfRangeException(nameof(lonStr), "Longitude must be between -180 and 180 degrees.");
        }

        // Return deterministic weather forecast telemetry
        return $"{{\"latitude\":{lat.ToString(CultureInfo.InvariantCulture)},\"longitude\":{lon.ToString(CultureInfo.InvariantCulture)},\"current\":{{\"temperature_2m\":24.5,\"relative_humidity_2m\":65,\"wind_speed_10m\":12.3}}}}";
    }

    private static string ExecuteDynamicFormula(string opKey, string fieldA, string fieldB, string? ruleTemplate)
    {
        if (string.IsNullOrWhiteSpace(ruleTemplate))
        {
            throw new InvalidOperationException($"Operation '{opKey}' does not have a defined rule template.");
        }

        // External API URL Template in reference engine
        if (ruleTemplate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            ruleTemplate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var resolvedUrl = ruleTemplate
                .Replace("{A}", fieldA ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{B}", fieldB ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            return $"{{\"url\":\"{resolvedUrl}\",\"fieldA\":\"{fieldA}\",\"fieldB\":\"{fieldB}\",\"status\":\"success\"}}";
        }

        if (!double.TryParse(fieldA, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) ||
            !double.TryParse(fieldB, NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
        {
            throw new ArgumentException($"Non-numeric input for formula '{ruleTemplate}'");
        }

        // Common dynamic templates
        if (ruleTemplate.Contains("(A * 2) + B", StringComparison.OrdinalIgnoreCase) ||
            ruleTemplate.Contains("A * 2 + B", StringComparison.OrdinalIgnoreCase))
        {
            return ((a * 2) + b).ToString(CultureInfo.InvariantCulture);
        }

        if (ruleTemplate.Contains("A / (B * B)", StringComparison.OrdinalIgnoreCase))
        {
            if (b == 0.0)
            {
                throw new DivideByZeroException("Division by zero in formula.");
            }
            var bmi = a / (b * b);
            return Math.Round(bmi, 2).ToString("F2", CultureInfo.InvariantCulture);
        }

        if (ruleTemplate.Contains("Math.Pow(A, 3)", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Pow(a, 3).ToString(CultureInfo.InvariantCulture);
        }

        if (ruleTemplate.Contains("Math.Pow(A, B)", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Pow(a, b).ToString(CultureInfo.InvariantCulture);
        }

        if (ruleTemplate.Contains("A * (1 + B/100)", StringComparison.OrdinalIgnoreCase) ||
            ruleTemplate.Contains("A * (1 + B / 100)", StringComparison.OrdinalIgnoreCase))
        {
            var tax = a * (1 + (b / 100.0));
            return tax.ToString("F2", CultureInfo.InvariantCulture);
        }

        // Fallback simple linear combinations
        return (a + b).ToString(CultureInfo.InvariantCulture);
    }
}
