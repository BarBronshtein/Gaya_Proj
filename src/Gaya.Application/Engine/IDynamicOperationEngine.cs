using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;

namespace Gaya.Application.Engine;

public interface IDynamicOperationEngine
{
    Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default);
}
