using System.Text.Json.Serialization;

namespace Gaya.Application.DTOs;

public class CreateOperationDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Arithmetic";

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
}
