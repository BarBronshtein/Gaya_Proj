using System.Globalization;
using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;

namespace Gaya.Application.Evaluators;

public class StringEvaluator : IOperationEvaluator
{
    private static readonly HashSet<string> SupportedOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "concat", "join-delim", "contains", "char-frequency", "levenshtein", "regex-match"
    };

    public bool CanEvaluate(OperationDefinition operation)
    {
        return operation.Category == OperationCategory.String &&
               (SupportedOperations.Contains(operation.Key) ||
                operation.Key.Contains("concat", StringComparison.OrdinalIgnoreCase) ||
                operation.Key.Contains("levenshtein", StringComparison.OrdinalIgnoreCase) ||
                operation.Key.Contains("regex", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(operation.RuleTemplate));
    }

    public Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default)
    {
        var a = fieldA ?? string.Empty;
        var b = fieldB ?? string.Empty;
        var opKey = (operation.Key ?? string.Empty).ToLowerInvariant();

        string result = opKey switch
        {
            _ when opKey == "concat" || opKey.Contains("concat") => $"{a}{b}",
            _ when opKey == "join-delim" || opKey.Contains("join") => $"{a}-{b}",
            _ when opKey == "contains" || opKey.Contains("contains") => EvaluateContains(a, b),
            _ when opKey == "char-frequency" || opKey.Contains("frequency") => EvaluateCharFrequency(a, b),
            _ when opKey == "levenshtein" || opKey.Contains("levenshtein") => EvaluateLevenshtein(a, b),
            _ when opKey == "regex-match" || opKey.Contains("regex") => EvaluateRegexMatch(a, b),
            _ => $"{a}{b}"
        };

        return Task.FromResult(CalculationResult.Ok(result));
    }

    private static string EvaluateContains(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return "false";
        }

        return a.Contains(b, StringComparison.OrdinalIgnoreCase) ? "true" : "false";
    }

    private static string EvaluateCharFrequency(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return "0";
        }

        char target = b[0];
        int count = a.Count(c => char.ToLowerInvariant(c) == char.ToLowerInvariant(target));
        return count.ToString(CultureInfo.InvariantCulture);
    }

    private static string EvaluateLevenshtein(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return (b?.Length ?? 0).ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(b)) return a.Length.ToString(CultureInfo.InvariantCulture);

        int n = a.Length;
        int m = b.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1])) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m].ToString(CultureInfo.InvariantCulture);
    }

    private static string EvaluateRegexMatch(string input, string pattern)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern)) return "false";
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, pattern) ? "true" : "false";
        }
        catch
        {
            return "false";
        }
    }
}
