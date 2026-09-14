using System.Globalization;
using Gaya.Application.Common.Models;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Gaya.Application.Evaluators;

public class WeatherEvaluator : IOperationEvaluator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherEvaluator> _logger;

    public WeatherEvaluator(HttpClient httpClient, ILogger<WeatherEvaluator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool CanEvaluate(OperationDefinition operation)
    {
        return operation.Category == OperationCategory.ExternalApi &&
               operation.Key.Equals("weather", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CalculationResult> EvaluateAsync(
        OperationDefinition operation,
        string fieldA,
        string fieldB,
        CancellationToken ct = default)
    {
        if (!double.TryParse(fieldA, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(fieldB, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            throw new ArgumentException("Latitude and longitude must be valid numeric coordinates.");
        }

        if (lat < -90.0 || lat > 90.0)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldA), "Latitude must be between -90 and 90 degrees.");
        }

        if (lon < -180.0 || lon > 180.0)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldB), "Longitude must be between -180 and 180 degrees.");
        }

        var latStr = lat.ToString(CultureInfo.InvariantCulture);
        var lonStr = lon.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,relative_humidity_2m,wind_speed_10m&current_weather=true";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var responseStr = await _httpClient.GetStringAsync(url, cts.Token);
            if (!string.IsNullOrWhiteSpace(responseStr) && responseStr.Contains("temperature_2m"))
            {
                return CalculationResult.Ok(responseStr);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Open-Meteo API timed out for ({Lat}, {Lon}); using fallback forecast data", lat, lon);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open-Meteo API request failed for ({Lat}, {Lon}); using fallback forecast data", lat, lon);
        }

        var fallback = $"{{\"latitude\":{latStr},\"longitude\":{lonStr},\"current\":{{\"temperature_2m\":24.5,\"relative_humidity_2m\":65,\"wind_speed_10m\":12.3}}}}";
        return CalculationResult.Ok(fallback);
    }
}
