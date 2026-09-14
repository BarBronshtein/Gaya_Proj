using Gaya.Api.Middleware;
using Gaya.Infrastructure.Data;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Controllers, Swagger & Routing Options
builder.Services.AddControllers();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Gaya Operations Platform API",
        Version = "v1",
        Description = "Dynamic 2-Operand Extensible Operations Platform with Dapper Persistence, Audit Logging, and OpenTelemetry Observability."
    });
});

// 2. Register Application & Infrastructure Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Configure CORS allowing Angular Client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                }
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 4. Configure OpenTelemetry Tracing & Logging (OTLP targeting OpenObserve)
var rawOtlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? builder.Configuration["OpenTelemetry:OtlpEndpoint"]
    ?? "http://localhost:5080/api/default";

var otlpHeaders = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS")
    ?? builder.Configuration["OpenTelemetry:Headers"]
    ?? "Authorization=Basic cm9vdEBleGFtcGxlLmNvbTpDb21wbGV4UGFzcyMxMjM="; // root@example.com:ComplexPass#123

string tracesEndpointStr = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT")
    ?? builder.Configuration["OpenTelemetry:TracesEndpoint"]
    ?? (rawOtlpEndpoint.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase)
        ? rawOtlpEndpoint
        : rawOtlpEndpoint.TrimEnd('/') + "/v1/traces");

string logsEndpointStr = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT")
    ?? builder.Configuration["OpenTelemetry:LogsEndpoint"]
    ?? (rawOtlpEndpoint.EndsWith("/v1/logs", StringComparison.OrdinalIgnoreCase)
        ? rawOtlpEndpoint
        : (rawOtlpEndpoint.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase)
            ? rawOtlpEndpoint[..^10] + "/v1/logs"
            : rawOtlpEndpoint.TrimEnd('/') + "/v1/logs"));

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "Gaya.Api", serviceVersion: "1.0.0");

if (Uri.TryCreate(tracesEndpointStr, UriKind.Absolute, out var tracesUri))
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/swagger");
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = tracesUri;
                    opts.Protocol = OtlpExportProtocol.HttpProtobuf;
                    if (!string.IsNullOrWhiteSpace(otlpHeaders))
                    {
                        opts.Headers = otlpHeaders;
                    }
                });
        });
}

if (Uri.TryCreate(logsEndpointStr, UriKind.Absolute, out var logsUri))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.SetResourceBuilder(resourceBuilder);
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.AddOtlpExporter(opts =>
        {
            opts.Endpoint = logsUri;
            opts.Protocol = OtlpExportProtocol.HttpProtobuf;
            if (!string.IsNullOrWhiteSpace(otlpHeaders))
            {
                opts.Headers = otlpHeaders;
            }
        });
    });
}

var app = builder.Build();

// 5. Application Startup: Resolve IDatabaseInitializer and initialize schema & seed data
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await databaseInitializer.InitializeAsync();
        logger.LogInformation("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database auto-initialization deferred or skipped (database may still be starting).");
    }
}

// 6. Middleware Pipeline Order: Exception Handling -> Audit -> CORS -> Routing/Endpoints
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiAuditMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gaya Operations Platform API v1");
    });
}

app.UseCors("AllowAngular");

app.UseRouting();
app.MapControllers();

app.Run();
