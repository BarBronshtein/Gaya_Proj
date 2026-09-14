using System.Text.Json.Serialization;

namespace Gaya.UnitTests.E2E.Contracts;

public class CalculationRequestDto
{
    [JsonPropertyName("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [JsonPropertyName("fieldA")]
    public string FieldA { get; set; } = string.Empty;

    [JsonPropertyName("fieldB")]
    public string FieldB { get; set; } = string.Empty;
}

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
}

public class CreateOperationDto
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
}

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

public class OperationMetricsDto
{
    [JsonPropertyName("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [JsonPropertyName("monthlyExecutionCount")]
    public int MonthlyExecutionCount { get; set; }

    [JsonPropertyName("recentExecutions")]
    public List<OperationHistoryDto> RecentExecutions { get; set; } = new();
}

public class ProblemDetailsDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }
}
