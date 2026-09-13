using Gaya.Domain.Entities;

namespace Gaya.Domain.Repositories;

public interface IOperationHistoryRepository
{
    Task<long> AddAsync(OperationHistory history, CancellationToken ct = default);
    Task<IReadOnlyList<OperationHistory>> GetRecentByOperationKeyAsync(string operationKey, int count = 3, CancellationToken ct = default);
    Task<int> GetMonthlyCountAsync(string operationKey, DateTime startOfMonthUtc, CancellationToken ct = default);
}
