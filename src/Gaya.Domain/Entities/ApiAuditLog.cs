namespace Gaya.Domain.Entities;

public class ApiAuditLog
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
