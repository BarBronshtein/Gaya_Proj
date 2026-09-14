using System.Net;
using Gaya.Application.Common.Models;
using Gaya.Application.DTOs;
using Gaya.Application.Engine;
using Gaya.Application.Evaluators;
using Gaya.Application.Services;
using Gaya.Domain.Entities;
using Gaya.Domain.Enums;
using Gaya.Domain.Repositories;
using Gaya.UnitTests.E2E.Harness;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using E2EContracts = Gaya.UnitTests.E2E.Contracts;

namespace Gaya.UnitTests;

public class ExternalApiEvaluatorTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public void CanEvaluate_ReturnsTrue_ForExternalApiCategory()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "crypto-price",
            DisplayName = "Crypto Price",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://api.coingecko.com/api/v3/simple/price?ids={A}&vs_currencies={B}"
        };

        // Act & Assert
        Assert.True(evaluator.CanEvaluate(op));
    }

    [Fact]
    public void CanEvaluate_ReturnsTrue_ForHttpUrlRuleTemplate_EvenIfCategoryIsDynamic()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "custom-api",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "https://api.example.com/data/{A}"
        };

        Assert.True(evaluator.CanEvaluate(op));
    }

    [Fact]
    public void CanEvaluate_ReturnsFalse_ForBuiltInWeather()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "weather",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://api.open-meteo.com/v1/forecast"
        };

        // Weather is handled specifically by WeatherEvaluator
        Assert.False(evaluator.CanEvaluate(op));
    }

    [Fact]
    public void CanEvaluate_ReturnsFalse_ForArithmeticFormulas()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "add-custom",
            Category = OperationCategory.Arithmetic,
            RuleTemplate = "(A * 2) + B"
        };

        Assert.False(evaluator.CanEvaluate(op));
    }

    [Fact]
    public async Task EvaluateAsync_SubstitutesParametersWithUrlEncoding()
    {
        // Arrange
        HttpRequestMessage? interceptedRequest = null;
        var mockResponseJson = "{\"status\":\"ok\",\"result\":\"test data\"}";

        var handler = new MockHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseJson, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "search-api",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://api.example.com/search?query={A}&tag={B}"
        };

        // Act: Pass values with spaces and special characters that must be URL encoded
        var result = await evaluator.EvaluateAsync(op, "hello world", "c#/.net");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(mockResponseJson, result.Result);
        Assert.NotNull(interceptedRequest);
        Assert.Equal("https://api.example.com/search?query=hello%20world&tag=c%23%2F.net", interceptedRequest.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task EvaluateAsync_HandlesCaseInsensitivePlaceholders()
    {
        // Arrange
        HttpRequestMessage? interceptedRequest = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":123}")
            };
        });

        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "lower-case-placeholders",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://api.example.com/users/{a}/posts/{b}"
        };

        // Act
        var result = await evaluator.EvaluateAsync(op, "admin", "42");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(interceptedRequest);
        Assert.Equal("https://api.example.com/users/admin/posts/42", interceptedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task EvaluateAsync_WhenEmptyRuleTemplate_ThrowsArgumentException()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "no-template",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "   "
        };

        await Assert.ThrowsAsync<ArgumentException>(() => evaluator.EvaluateAsync(op, "1", "2"));
    }

    [Fact]
    public async Task EvaluateAsync_WhenInvalidUrl_ThrowsArgumentException()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "bad-url",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "not-a-valid-url-{A}"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => evaluator.EvaluateAsync(op, "1", "2"));
    }

    [Fact]
    public async Task EvaluateAsync_WhenHttpError_ReturnsFailedCalculationResult()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found"
        });

        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "not-found-api",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://api.example.com/missing/{A}"
        };

        var result = await evaluator.EvaluateAsync(op, "item123", "");

        Assert.False(result.IsSuccess);
        Assert.Contains("404", result.ErrorMessage);
        Assert.Contains("Not Found", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateAsync_WhenHttpRequestExceptionThrown_ReturnsFailedCalculationResult()
    {
        var handler = new MockHttpMessageHandler(_ => throw new HttpRequestException("DNS resolution failed"));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "dns-fail",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://nonexistent.domain.xyz/{A}"
        };

        var result = await evaluator.EvaluateAsync(op, "data", "");

        Assert.False(result.IsSuccess);
        Assert.Contains("DNS resolution failed", result.ErrorMessage);
    }

    [Fact]
    public async Task EvaluateAsync_WithHebrewUnicodeOperands_EncodesCorrectly()
    {
        HttpRequestMessage? interceptedRequest = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            interceptedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}")
            };
        });

        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);

        var op = new OperationDefinition
        {
            Key = "hebrew-api",
            Category = OperationCategory.ExternalApi,
            RuleTemplate = "https://api.example.com/search?q={A}&city={B}"
        };

        var result = await evaluator.EvaluateAsync(op, "שלום", "תל אביב");

        Assert.True(result.IsSuccess);
        Assert.NotNull(interceptedRequest);
        // "שלום" and "תל אביב" should be percent encoded in URI
        var uriStr = interceptedRequest.RequestUri?.AbsoluteUri ?? string.Empty;
        Assert.Contains("q=%D7%A9%D7%9C%D7%95%D7%9D", uriStr);
        Assert.Contains("city=%D7%AA%D7%9C%20%D7%90%D7%91%D7%99%D7%91", uriStr);
    }

    [Fact]
    public async Task CalculatorService_CreateOperationAsync_WithExternalApi_RequiresValidHttpUrl()
    {
        // Arrange
        var opRepo = new InMemoryOperationRepository();
        var histRepo = new InMemoryOperationHistoryRepository();
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var evaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);
        var engine = new DynamicOperationEngine(new IOperationEvaluator[] { evaluator }, NullLogger<DynamicOperationEngine>.Instance);
        var service = new CalculatorService(opRepo, histRepo, engine, NullLogger<CalculatorService>.Instance);

        // Act & Assert 1: Missing RuleTemplate
        var emptyTemplateDto = new CreateOperationDto
        {
            Key = "api-empty",
            DisplayName = "Empty API",
            Category = "ExternalApi",
            RuleTemplate = ""
        };
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateOperationAsync(emptyTemplateDto));

        // Act & Assert 2: Non-HTTP RuleTemplate
        var badUrlDto = new CreateOperationDto
        {
            Key = "api-bad",
            DisplayName = "Bad API",
            Category = "ExternalApi",
            RuleTemplate = "ftp://files.example.com/{A}"
        };
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateOperationAsync(badUrlDto));

        // Act & Assert 3: Valid ExternalApi
        var validDto = new CreateOperationDto
        {
            Key = "api-catfacts",
            DisplayName = "Cat Facts API",
            Category = "ExternalApi",
            RuleTemplate = "https://catfact.ninja/fact?max_length={A}",
            FieldAPrompt = "Max Length",
            FieldBPrompt = "Unused"
        };
        var created = await service.CreateOperationAsync(validDto);

        Assert.Equal("api-catfacts", created.Key);
        Assert.Equal("ExternalApi", created.Category);
        Assert.Equal("https://catfact.ninja/fact?max_length={A}", created.RuleTemplate);
    }

    [Fact]
    public async Task Engine_And_CalculatorService_ExecuteDynamicExternalApiSuccessfully()
    {
        // Arrange
        var opRepo = new InMemoryOperationRepository();
        var histRepo = new InMemoryOperationHistoryRepository();
        var expectedJson = "{\"fact\":\"Cats sleep 70% of their lives.\",\"length\":33}";

        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri?.ToString() == "https://catfact.ninja/fact?max_length=50")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(expectedJson, System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        var httpClient = new HttpClient(handler);
        var externalEvaluator = new ExternalApiEvaluator(httpClient, NullLogger<ExternalApiEvaluator>.Instance);
        var arithmeticEvaluator = new ArithmeticEvaluator();
        var dynamicEvaluator = new DynamicExpressionEvaluator();

        var engine = new DynamicOperationEngine(
            new IOperationEvaluator[] { arithmeticEvaluator, dynamicEvaluator, externalEvaluator },
            NullLogger<DynamicOperationEngine>.Instance);

        var service = new CalculatorService(opRepo, histRepo, engine, NullLogger<CalculatorService>.Instance);

        // 1. Create dynamic external API operation
        await service.CreateOperationAsync(new CreateOperationDto
        {
            Key = "cat-fact",
            DisplayName = "Random Cat Fact",
            Category = "ExternalApi",
            RuleTemplate = "https://catfact.ninja/fact?max_length={A}",
            FieldAPrompt = "Max Length",
            FieldBPrompt = "N/A"
        });

        // 2. Execute calculation
        var response = await service.CalculateAsync(new CalculationRequestDto
        {
            OperationKey = "cat-fact",
            FieldA = "50",
            FieldB = "0"
        });

        // 3. Assert
        Assert.Equal("cat-fact", response.OperationKey);
        Assert.Equal(expectedJson, response.Result);
        Assert.Equal(1, response.MonthlyExecutionCount);
        Assert.Single(response.RecentExecutions);
        Assert.Equal(expectedJson, response.RecentExecutions[0].Result);
    }

    [Fact]
    public async Task E2E_Harness_ExecutesDynamicExternalApiOperation()
    {
        using var harness = new E2ETestHarness();

        // 1. Create dynamic ExternalApi operation
        var created = await harness.CreateOperationAsync(new E2EContracts.CreateOperationDto
        {
            Key = "user-lookup",
            DisplayName = "User Profile Lookup",
            Category = "ExternalApi",
            RuleTemplate = "https://api.github.com/users/{A}",
            FieldAPrompt = "GitHub Username",
            FieldBPrompt = "Unused"
        });

        Assert.Equal("user-lookup", created.Key);
        Assert.Equal("ExternalApi", created.Category);

        // 2. Execute calculation
        var result = await harness.CalculateAsync(new E2EContracts.CalculationRequestDto
        {
            OperationKey = "user-lookup",
            FieldA = "octocat",
            FieldB = ""
        });

        // 3. Assert
        Assert.NotNull(result.Result);
        Assert.Contains("octocat", result.Result);
        Assert.Equal(1, result.MonthlyExecutionCount);
    }
}
