using System.Text.Json.Serialization;

namespace Gaya.Application.DTOs;

public class CalculationRequestDto
{
    [JsonPropertyName("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [JsonPropertyName("operationId")]
    public string? OperationId
    {
        get => OperationKey;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(OperationKey))
            {
                OperationKey = value;
            }
        }
    }

    [JsonPropertyName("fieldA")]
    public string FieldA { get; set; } = string.Empty;

    [JsonPropertyName("fieldB")]
    public string FieldB { get; set; } = string.Empty;

    public string GetEffectiveKey() =>
        !string.IsNullOrWhiteSpace(OperationKey) ? OperationKey : (OperationId ?? string.Empty);
}
