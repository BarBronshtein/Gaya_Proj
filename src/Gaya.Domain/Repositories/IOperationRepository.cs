using Gaya.Domain.Entities;

namespace Gaya.Domain.Repositories;

public interface IOperationRepository
{
    Task<IReadOnlyList<OperationDefinition>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<OperationDefinition?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task UpsertAsync(OperationDefinition operation, CancellationToken ct = default);
    Task SetActiveStatusAsync(string key, bool isActive, CancellationToken ct = default);
}
