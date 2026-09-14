using System.Globalization;
using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;

namespace Gaya.Application.Evaluators;

public class DynamicExpressionEvaluator : IOperationEvaluator
{
    public bool CanEvaluate(OperationDefinition operation)
    {
        return !string.IsNullOrWhiteSpace(operation.RuleTemplate);
    }

    public Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default)
    {
        var template = operation.RuleTemplate.Trim();

        // 1. Simple string interpolation templates like "{A} - {B}" or "{A}{B}"
        if (template.Contains("{A}", StringComparison.OrdinalIgnoreCase) ||
            template.Contains("{B}", StringComparison.OrdinalIgnoreCase) ||
            template.Contains("{delim}", StringComparison.OrdinalIgnoreCase))
        {
            var formatted = template
                .Replace("{A}", fieldA ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{B}", fieldB ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{delim}", "-", StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(CalculationResult.Ok(formatted));
        }

        // 2. Dynamic AST expression evaluation
        try
        {
            var parser = new SafeExpressionParser(template, fieldA, fieldB);
            var result = parser.ParseAndEvaluate();
            return Task.FromResult(CalculationResult.Ok(result));
        }
        catch (DivideByZeroException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to evaluate dynamic expression '{template}': {ex.Message}", ex);
        }
    }

    internal sealed class SafeExpressionParser
    {
        private readonly string _expr;
        private readonly double _valA;
        private readonly double _valB;
        private int _pos;

        public SafeExpressionParser(string expr, string? fieldA, string? fieldB)
        {
            _expr = expr;
            _pos = 0;

            if (!double.TryParse(fieldA, NumberStyles.Float, CultureInfo.InvariantCulture, out _valA))
            {
                _valA = 0.0;
            }

            if (!double.TryParse(fieldB, NumberStyles.Float, CultureInfo.InvariantCulture, out _valB))
            {
                _valB = 0.0;
            }
        }

        public string ParseAndEvaluate()
        {
            var result = ParseExpression();
            SkipWhitespace();
            if (_pos < _expr.Length)
            {
                throw new InvalidOperationException($"Unexpected character '{_expr[_pos]}' at position {_pos}");
            }

            if (result % 1 == 0 && Math.Abs(result) < 1e15)
            {
                return ((long)result).ToString(CultureInfo.InvariantCulture);
            }

            return Math.Round(result, 6).ToString("0.######", CultureInfo.InvariantCulture);
        }

        private double ParseExpression()
        {
            return ParseAddSubtract();
        }

        private double ParseAddSubtract()
        {
            var left = ParseMultiplyDivide();

            while (_pos < _expr.Length)
            {
                SkipWhitespace();
                if (_pos >= _expr.Length) break;

                char op = _expr[_pos];
                if (op != '+' && op != '-') break;

                _pos++;
                var right = ParseMultiplyDivide();
                left = op == '+' ? left + right : left - right;
            }

            return left;
        }

        private double ParseMultiplyDivide()
        {
            var left = ParsePower();

            while (_pos < _expr.Length)
            {
                SkipWhitespace();
                if (_pos >= _expr.Length) break;

                char op = _expr[_pos];
                if (op != '*' && op != '/' && op != '%') break;

                _pos++;
                var right = ParsePower();

                if (op == '*')
                {
                    left *= right;
                }
                else if (op == '/')
                {
                    if (right == 0.0)
                    {
                        throw new DivideByZeroException("Division by zero in formula.");
                    }
                    left /= right;
                }
                else // '%'
                {
                    if (right == 0.0)
                    {
                        throw new DivideByZeroException("Modulo by zero in formula.");
                    }
                    left %= right;
                }
            }

            return left;
        }

        private double ParsePower()
        {
            var left = ParseUnary();

            SkipWhitespace();
            if (_pos < _expr.Length && _expr[_pos] == '^')
            {
                _pos++;
                var right = ParsePower(); // right-associative
                return Math.Pow(left, right);
            }

            return left;
        }

        private double ParseUnary()
        {
            SkipWhitespace();
            if (_pos >= _expr.Length)
            {
                throw new InvalidOperationException("Unexpected end of expression");
            }

            if (_expr[_pos] == '-')
            {
                _pos++;
                return -ParseUnary();
            }

            if (_expr[_pos] == '+')
            {
                _pos++;
                return ParseUnary();
            }

            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            SkipWhitespace();
            if (_pos >= _expr.Length)
            {
                throw new InvalidOperationException("Unexpected end of expression");
            }

            // Parentheses
            if (_expr[_pos] == '(')
            {
                _pos++;
                var val = ParseExpression();
                SkipWhitespace();
                if (_pos >= _expr.Length || _expr[_pos] != ')')
                {
                    throw new InvalidOperationException("Missing closing parenthesis ')'");
                }
                _pos++;
                return val;
            }

            // Number
            if (char.IsDigit(_expr[_pos]) || _expr[_pos] == '.')
            {
                int start = _pos;
                while (_pos < _expr.Length && (char.IsDigit(_expr[_pos]) || _expr[_pos] == '.'))
                {
                    _pos++;
                }

                var numStr = _expr.Substring(start, _pos - start);
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                {
                    return num;
                }

                throw new InvalidOperationException($"Invalid number literal: '{numStr}'");
            }

            // Identifiers / Functions
            if (char.IsLetter(_expr[_pos]) || _expr[_pos] == '_')
            {
                int start = _pos;
                while (_pos < _expr.Length && (char.IsLetterOrDigit(_expr[_pos]) || _expr[_pos] == '_' || _expr[_pos] == '.'))
                {
                    _pos++;
                }

                var identifier = _expr.Substring(start, _pos - start);

                if (string.Equals(identifier, "A", StringComparison.OrdinalIgnoreCase))
                {
                    return _valA;
                }

                if (string.Equals(identifier, "B", StringComparison.OrdinalIgnoreCase))
                {
                    return _valB;
                }

                // Check function call
                SkipWhitespace();
                if (_pos < _expr.Length && _expr[_pos] == '(')
                {
                    _pos++;
                    var args = new List<double>();
                    SkipWhitespace();
                    if (_pos < _expr.Length && _expr[_pos] != ')')
                    {
                        args.Add(ParseExpression());
                        SkipWhitespace();
                        while (_pos < _expr.Length && _expr[_pos] == ',')
                        {
                            _pos++;
                            args.Add(ParseExpression());
                            SkipWhitespace();
                        }
                    }

                    if (_pos >= _expr.Length || _expr[_pos] != ')')
                    {
                        throw new InvalidOperationException($"Missing closing parenthesis ')' for function '{identifier}'");
                    }
                    _pos++;

                    return EvaluateFunction(identifier, args);
                }

                throw new InvalidOperationException($"Unknown identifier '{identifier}'");
            }

            throw new InvalidOperationException($"Unexpected character '{_expr[_pos]}' at position {_pos}");
        }

        private static double EvaluateFunction(string name, List<double> args)
        {
            var fn = name.ToLowerInvariant();
            if (fn.StartsWith("math."))
            {
                fn = fn.Substring(5);
            }

            return fn switch
            {
                "pow" when args.Count == 2 => Math.Pow(args[0], args[1]),
                "sqrt" when args.Count == 1 => Math.Sqrt(args[0]),
                "abs" when args.Count == 1 => Math.Abs(args[0]),
                "round" when args.Count == 1 => Math.Round(args[0]),
                "round" when args.Count == 2 => Math.Round(args[0], (int)args[1]),
                "floor" when args.Count == 1 => Math.Floor(args[0]),
                "ceiling" or "ceil" when args.Count == 1 => Math.Ceiling(args[0]),
                "min" when args.Count == 2 => Math.Min(args[0], args[1]),
                "max" when args.Count == 2 => Math.Max(args[0], args[1]),
                _ => throw new NotSupportedException($"Function '{name}' with {args.Count} arguments is not supported.")
            };
        }

        private void SkipWhitespace()
        {
            while (_pos < _expr.Length && char.IsWhiteSpace(_expr[_pos]))
            {
                _pos++;
            }
        }
    }
}
