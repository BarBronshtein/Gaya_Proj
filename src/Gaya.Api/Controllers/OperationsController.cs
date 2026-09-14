using Gaya.Application.DTOs;
using Gaya.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gaya.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly ICalculatorService _calculatorService;
    private readonly ILogger<OperationsController> _logger;

    public OperationsController(
        ICalculatorService calculatorService,
        ILogger<OperationsController> logger)
    {
        _calculatorService = calculatorService ?? throw new ArgumentNullException(nameof(calculatorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all available operations (both built-in and dynamically registered).
    /// Endpoint: GET api/operations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OperationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OperationDto>>> GetAll(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var operations = await _calculatorService.GetOperationsAsync(activeOnly, ct);
        return Ok(operations);
    }

    /// <summary>
    /// Retrieves recent calculation execution history across operations.
    /// Endpoint: GET api/operations/history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<OperationHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OperationHistoryDto>>> GetHistory(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var history = await _calculatorService.GetHistoryAsync(limit, ct);
        return Ok(history);
    }

    /// <summary>
    /// Retrieves live execution metrics for a specific operation (recent 3 executions + monthly UTC execution count).
    /// Endpoint: GET api/operations/{key}/metrics
    /// </summary>
    [HttpGet("{key}/metrics")]
    [ProducesResponseType(typeof(OperationMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationMetricsDto>> GetMetrics(
        string key,
        CancellationToken ct = default)
    {
        var metrics = await _calculatorService.GetMetricsAsync(key, ct);
        return Ok(metrics);
    }

    /// <summary>
    /// Retrieves a single operation definition by its unique key.
    /// Endpoint: GET api/operations/{key}
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(OperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationDto>> GetByKey(
        string key,
        CancellationToken ct = default)
    {
        var operation = await _calculatorService.GetOperationByKeyAsync(key, ct);
        if (operation == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Operation Not Found",
                Detail = $"No operation found with key '{key}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(operation);
    }

    /// <summary>
    /// Dynamically creates or updates an operation definition at runtime without server restart.
    /// Endpoint: POST api/operations
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OperationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationDto>> Create(
        [FromBody] CreateOperationDto dto,
        CancellationToken ct = default)
    {
        if (dto == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Operation definition cannot be null.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _calculatorService.CreateOperationAsync(dto, ct);
        return CreatedAtAction(nameof(GetByKey), new { key = created.Key }, created);
    }

    /// <summary>
    /// Toggles the active status of an operation definition.
    /// Endpoint: PUT api/operations/{key}/status
    /// </summary>
    [HttpPut("{key}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(
        string key,
        [FromBody] SetOperationStatusRequest request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Status request cannot be null.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await _calculatorService.SetOperationActiveStatusAsync(key, request.IsActive, ct);
        return NoContent();
    }
}

public record SetOperationStatusRequest(bool IsActive);
