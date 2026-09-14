using System.Diagnostics;
using System.Text;
using Gaya.Domain.Entities;
using Gaya.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Gaya.Api.Middleware;

/// <summary>
/// Captures HTTP request and response payloads, status codes, latency, and client IP
/// and records an immutable audit log to IApiAuditLogRepository asynchronously.
/// Preserves the response body stream so it is correctly returned to the client.
/// </summary>
public class ApiAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiAuditMiddleware> _logger;

    public ApiAuditMiddleware(RequestDelegate next, ILogger<ApiAuditMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IApiAuditLogRepository auditLogRepository)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(auditLogRepository);

        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        // 1. Capture incoming Request Body
        request.EnableBuffering();
        string requestBody = string.Empty;
        if (request.Body.CanRead)
        {
            using var requestReader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            requestBody = await requestReader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        // 2. Resolve Client IP
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            clientIp = forwardedFor.ToString().Split(',')[0].Trim();
        }

        // 3. Swap Response Body to memory stream to capture response payload
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        Exception? capturedException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var latencyMs = Math.Max(0, stopwatch.ElapsedMilliseconds);

            string responseBody = string.Empty;
            int statusCode = context.Response.StatusCode;

            if (capturedException != null)
            {
                // In case of unhandled exception bubbling to outer middleware, determine status
                statusCode = capturedException switch
                {
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    ArgumentNullException or ArgumentException or InvalidOperationException or BadHttpRequestException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };
                responseBody = $"{{\"error\":\"{capturedException.Message}\"}}";

                // Restore response body stream so upstream exception handling can write to it
                context.Response.Body = originalResponseBody;
            }
            else
            {
                // Read captured response body
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                using var responseReader = new StreamReader(
                    responseBodyStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);
                responseBody = await responseReader.ReadToEndAsync();

                // Reset position and copy contents back to original stream for client delivery
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
            }

            // 4. Asynchronously persist ApiAuditLog entry
            try
            {
                var auditEntry = new ApiAuditLog
                {
                    Path = request.Path.Value ?? "/",
                    Method = request.Method,
                    StatusCode = statusCode,
                    LatencyMs = latencyMs,
                    RequestBody = string.IsNullOrWhiteSpace(requestBody) ? null : requestBody,
                    ResponseBody = string.IsNullOrWhiteSpace(responseBody) ? null : responseBody,
                    ClientIp = clientIp,
                    CreatedAt = DateTime.UtcNow
                };

                await auditLogRepository.AddAsync(auditEntry);
                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {LatencyMs}ms (Client: {ClientIp})", request.Method, request.Path, statusCode, latencyMs, clientIp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist API audit log for {Method} {Path}", request.Method, request.Path);
            }
        }
    }
}

/// <summary>
/// Alias for ApiAuditMiddleware supporting alternative naming conventions.
/// </summary>
public class AuditMiddleware : ApiAuditMiddleware
{
    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        : base(next, logger)
    {
    }
}
