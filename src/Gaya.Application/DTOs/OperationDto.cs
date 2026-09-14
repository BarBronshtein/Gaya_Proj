using System.Text.Json.Serialization;

namespace Gaya.Application.DTOs;

public class OperationDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("ruleTemplate")]
    public string RuleTemplate { get; set; } = string.Empty;

    [JsonPropertyName("fieldAPrompt")]
    public string FieldAPrompt { get; set; } = string.Empty;

    [JsonPropertyName("fieldBPrompt")]
    public string FieldBPrompt { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
