using Gaya.Domain.Entities;

namespace Gaya.Domain.Repositories;

public interface ISystemErrorRepository
{
    Task<long> AddAsync(SystemError error, CancellationToken ct = default);
    Task<IReadOnlyList<SystemError>> GetRecentAsync(int count = 50, CancellationToken ct = default);
}
