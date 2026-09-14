using System.Text.Json.Serialization;

namespace Gaya.Application.DTOs;

public class OperationMetricsDto
{
    [JsonPropertyName("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [JsonPropertyName("monthlyExecutionCount")]
    public int MonthlyExecutionCount { get; set; }

    [JsonPropertyName("recentExecutions")]
    public List<OperationHistoryDto> RecentExecutions { get; set; } = new();
}
