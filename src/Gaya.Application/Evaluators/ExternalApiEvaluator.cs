using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Gaya.Application.Evaluators;

/// <summary>
/// Evaluator for dynamically defined External REST API operations.
/// Interpolates URL templates with operands {A} and {B} and executes HTTP GET requests.
/// </summary>
public class ExternalApiEvaluator : IOperationEvaluator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiEvaluator> _logger;

    public ExternalApiEvaluator(HttpClient httpClient, ILogger<ExternalApiEvaluator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanEvaluate(OperationDefinition operation)
    {
        if (operation == null)
        {
            return false;
        }

        // Built-in weather operation is handled by dedicated WeatherEvaluator
        if (operation.Key.Equals("weather", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (operation.Category == OperationCategory.ExternalApi)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(operation.RuleTemplate) &&
            (operation.RuleTemplate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             operation.RuleTemplate.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public async Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (string.IsNullOrWhiteSpace(operation.RuleTemplate))
        {
            throw new ArgumentException($"External API operation '{operation.Key}' does not have a URL template defined.", nameof(operation));
        }

        var rawTemplate = operation.RuleTemplate.Trim();

        // Parameter substitution: replace {A} and {B} placeholders with URL-encoded operand values
        var escapedA = Uri.EscapeDataString(fieldA ?? string.Empty);
        var escapedB = Uri.EscapeDataString(fieldB ?? string.Empty);

        var resolvedUrl = rawTemplate
            .Replace("{A}", escapedA, StringComparison.OrdinalIgnoreCase)
            .Replace("{B}", escapedB, StringComparison.OrdinalIgnoreCase);

        if (!Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Invalid external API URL: '{resolvedUrl}'");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            _logger.LogInformation("Executing external API request for operation '{Key}' to '{Url}'", operation.Key, uri);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                request.Headers.UserAgent.ParseAdd("Gaya-Operations-Engine/1.0");
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("External API request to '{Url}' returned HTTP status {StatusCode} ({ReasonPhrase})",
                    uri, (int)response.StatusCode, response.ReasonPhrase);

                return CalculationResult.Fail($"External API returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            return CalculationResult.Ok(content);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("External API request to '{Url}' timed out after 10 seconds", uri);
            return CalculationResult.Fail($"External API request to '{uri.Host}' timed out after 10 seconds.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP transport error during external API request to '{Url}'", uri);
            return CalculationResult.Fail($"External API HTTP request failed: {ex.Message}");
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected fault during external API request to '{Url}'", uri);
            return CalculationResult.Fail($"External API request encountered an error: {ex.Message}");
        }
    }
}
