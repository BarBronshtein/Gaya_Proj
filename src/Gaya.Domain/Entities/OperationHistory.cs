namespace Gaya.Domain.Entities;

public class OperationHistory
{
    public long Id { get; set; }
    public string OperationKey { get; set; } = string.Empty;
    public string FieldA { get; set; } = string.Empty;
    public string FieldB { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
