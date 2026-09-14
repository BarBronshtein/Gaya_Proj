using Gaya.Application.DTOs;
using Gaya.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gaya.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/execute")]
public class CalculateController : ControllerBase
{
    private readonly ICalculatorService _calculatorService;
    private readonly ILogger<CalculateController> _logger;

    public CalculateController(
        ICalculatorService calculatorService,
        ILogger<CalculateController> logger)
    {
        _calculatorService = calculatorService ?? throw new ArgumentNullException(nameof(calculatorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a dynamic 2-operand calculation, records history, and returns execution metrics (3 recent + monthly count).
    /// Endpoints: POST api/calculate and alias POST api/execute.
    /// </summary>
    /// <param name="request">Calculation payload with OperationKey, FieldA, and FieldB</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>CalculationResponseDto with result, duration, recent executions, and monthly count</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CalculationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CalculationResponseDto>> Calculate(
        [FromBody] CalculationRequestDto request,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Calculation request body cannot be null.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _calculatorService.CalculateAsync(request, ct);
        return Ok(response);
    }
}
