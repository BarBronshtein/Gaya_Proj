using System.Text.Json.Serialization;

namespace Gaya.Application.DTOs;

public class CalculationResponseDto
{
    [JsonPropertyName("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [JsonPropertyName("fieldA")]
    public string FieldA { get; set; } = string.Empty;

    [JsonPropertyName("fieldB")]
    public string FieldB { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("executedAt")]
    public DateTime ExecutedAt { get; set; }

    [JsonPropertyName("recentExecutions")]
    public List<OperationHistoryDto> RecentExecutions { get; set; } = new();

    [JsonPropertyName("monthlyExecutionCount")]
    public int MonthlyExecutionCount { get; set; }
}
