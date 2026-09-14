using System.Text.Json;
using Gaya.Domain.Entities;
using Gaya.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gaya.Api.Middleware;

/// <summary>
/// Global exception handling middleware converting unhandled faults into RFC 7807 ProblemDetails
/// and logging fault details into ISystemErrorRepository.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ISystemErrorRepository errorRepository)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(errorRepository);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled fault occurred while executing {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await HandleExceptionAsync(context, ex, errorRepository);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ISystemErrorRepository errorRepository)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            ArgumentNullException or ArgumentException or InvalidOperationException or BadHttpRequestException
                => (StatusCodes.Status400BadRequest, "Bad Request"),
            DivideByZeroException => (StatusCodes.Status500InternalServerError, "Division by Zero"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        // 1. Asynchronously log fault details to ISystemErrorRepository
        try
        {
            var systemError = new SystemError
            {
                ErrorMessage = exception.Message,
                StackTrace = exception.StackTrace,
                SourcePath = context.Request.Path.Value ?? "/",
                StatusCode = statusCode,
                CreatedAt = DateTime.UtcNow
            };

            await errorRepository.AddAsync(systemError);
        }
        catch
        {
            // Suppress secondary repository failures during emergency fault response
        }

        // 2. Deliver RFC 7807 ProblemDetails JSON response
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = $"https://httpstatuses.io/{statusCode}",
                Title = title,
                Status = statusCode,
                Detail = exception.Message,
                Instance = context.Request.Path.Value ?? "/"
            };

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(json);
        }
    }
}
