using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;

namespace Gaya.Application.Evaluators;

public interface IOperationEvaluator
{
    bool CanEvaluate(OperationDefinition operation);

    Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default);
}
