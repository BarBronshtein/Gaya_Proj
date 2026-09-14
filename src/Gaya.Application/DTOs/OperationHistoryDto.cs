using System.Text.Json.Serialization;

namespace Gaya.Application.DTOs;

public class OperationHistoryDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

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
}
