using Gaya.Domain.Enums;

namespace Gaya.Domain.Entities;

public class OperationDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public OperationCategory Category { get; set; }
    public string RuleTemplate { get; set; } = string.Empty;
    public string FieldAPrompt { get; set; } = string.Empty;
    public string FieldBPrompt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
