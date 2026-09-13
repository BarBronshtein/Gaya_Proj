# 05 Web API, Middlewares and OpenTelemetry
Type: task
Status: ready-for-agent
Blocked by: 04

## Question
How should the ASP.NET Core 8 Web API expose public REST endpoints, capture all HTTP requests and responses into the database, log unhandled errors, and stream OpenTelemetry data to OpenObserve?

### Scope & Acceptance Criteria
- REST Controllers:
  - `GET /api/operations`: List all available operations (metadata, prompts, category).
  - `POST /api/operations`: Create or update an operation dynamically at runtime.
  - `POST /api/calculate`: Execute an operation given `{ operationKey, fieldA, fieldB }` and return calculation result, 3 recent executions, and monthly count.
- Request/Response Audit Middleware:
  - Intercept every incoming request and outgoing response stream.
  - Record Path, Method, Status Code, Elapsed Latency, Request Body, Response Body to `ApiAuditLogs` table via Dapper.
- Global Exception Handler:
  - Catch unhandled exceptions, return standard RFC 7807 `ProblemDetails` response, and log to `SystemErrors` table via Dapper.
- OpenTelemetry Configuration:
  - Instrument ASP.NET Core, HttpClient, and SQL Client.
  - Configure OTLP exporter pointing to OpenObserve at port 5080.
- Include Swagger / OpenAPI documentation.
