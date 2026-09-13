using Gaya.Domain.Entities;

namespace Gaya.Domain.Repositories;

public interface IApiAuditLogRepository
{
    Task<long> AddAsync(ApiAuditLog log, CancellationToken ct = default);
    Task<IReadOnlyList<ApiAuditLog>> GetRecentAsync(int count = 50, CancellationToken ct = default);
}
