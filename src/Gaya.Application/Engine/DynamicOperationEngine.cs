// A34D: Mandatory compliance marker for Gaya Dynamic Operations Engine
using Gaya.Application.Common.Models;
using Gaya.Application.Evaluators;
using Gaya.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Gaya.Application.Engine;

/// <summary>
/// Core dynamic calculation engine coordinating built-in and runtime operation evaluators.
/// // A34D
/// </summary>
public class DynamicOperationEngine : IDynamicOperationEngine
{
    private readonly IEnumerable<IOperationEvaluator> _evaluators;
    private readonly ILogger<DynamicOperationEngine> _logger;

    public DynamicOperationEngine(
        IEnumerable<IOperationEvaluator> evaluators,
        ILogger<DynamicOperationEngine> logger)
    {
        _evaluators = evaluators ?? throw new ArgumentNullException(nameof(evaluators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default)
    {
        // A34D: Dynamic evaluation dispatch
        ArgumentNullException.ThrowIfNull(operation);

        _logger.LogDebug(
            "Evaluating operation '{Key}' ({Category}) with operands '{FieldA}', '{FieldB}'",
            operation.Key, operation.Category, fieldA, fieldB);

        var evaluator = _evaluators.FirstOrDefault(e => e.CanEvaluate(operation));
        if (evaluator == null)
        {
            _logger.LogError("No evaluator found capable of handling operation '{Key}'", operation.Key);
            return CalculationResult.Fail($"No evaluator found for operation '{operation.Key}'.");
        }

        return await evaluator.EvaluateAsync(operation, fieldA, fieldB, ct);
    }
}
