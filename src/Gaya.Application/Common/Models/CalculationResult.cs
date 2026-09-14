namespace Gaya.Application.Common.Models;

public sealed class CalculationResult
{
    public bool IsSuccess { get; init; }
    public string Result { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static CalculationResult Ok(string result) =>
        new() { IsSuccess = true, Result = result };

    public static CalculationResult Fail(string errorMessage) =>
        new() { IsSuccess = false, Result = string.Empty, ErrorMessage = errorMessage };
}
