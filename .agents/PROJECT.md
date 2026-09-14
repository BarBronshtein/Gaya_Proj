# Project: Gaya Operations Platform (.NET 8 & Angular)

## Architecture
Clean Architecture with dynamic 2-operand extensible operations engine, Dapper SQL Server persistence, custom ASP.NET Core 8 audit/error middleware, OpenTelemetry observability, and Angular 18 standalone client.

```
┌─────────────────────────────────────────────────────────────────┐
│                       Angular 18 Client                         │
│  - Calculator Component (dynamic inputs, 3 recent, month badge) │
│  - Operations Hub Component (create/manage custom operations)   │
└────────────────────────────────┬────────────────────────────────┘
                                 │ HTTP / JSON
┌────────────────────────────────▼────────────────────────────────┐
│                   Gaya.Api (ASP.NET Core 8)                     │
│  - Endpoints: /api/operations, /api/calculate, /api/execute     │
│  - Custom Audit Middleware -> ApiAuditLogs                      │
│  - Global RFC 7807 Exception Handler -> SystemErrors            │
│  - OpenTelemetry Tracing/Logging -> OpenObserve (OTLP:5080)     │
└───────────────┬────────────────────────────────┬────────────────┘
                │                                │
┌───────────────▼────────────────┐ ┌─────────────▼────────────────┐
│        Gaya.Application        │ │      Gaya.Infrastructure     │
│  - IDynamicOperationEngine     │ │  - Dapper Repositories       │
│    (Arithmetic, String,        │ │    (Operations, Histories,   │
│     Weather, Dynamic)          │ │     ApiAuditLogs, SystemErrors)
│  - Mandatory "A34D" Marker     │ │  - DatabaseInitializer (DDL) │
│  - ICalculatorService & DTOs   │ │  - DbConnectionFactory       │
└───────────────┬────────────────┘ └─────────────┬────────────────┘
                │                                │
                └───────────────┬────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                          Gaya.Domain                            │
│  - Entities: OperationDefinition, OperationHistory,             │
│              ApiAuditLog, SystemError                           │
│  - Enums: OperationCategory (Arithmetic, String, ExternalApi)   │
│  - Repository Contracts: IOperationRepository, etc.             │
└─────────────────────────────────────────────────────────────────┘
```

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Arithmetic Evaluator | Evaluates add, subtract, multiply, divide, power, modulo with 2 numeric operands | M1 | R1, TICKET-01 |
| 2 | String Evaluator | Evaluates concat, join-delim, contains, char-frequency with 2 string operands | M1 | R1, TICKET-01 |
| 3 | Weather Evaluator | Queries Open-Meteo external REST API using Field A (lat) and Field B (lon) | M1 | R1, TICKET-01 |
| 4 | Dynamic Extensibility | Evaluates runtime custom expressions without recompilation or service restart | M1 | R1, ADR-0001 |
| 5 | Mandatory Marker A34D | Code comment "A34D" embedded directly in the calculation engine codebase | M1 | R1, ORIGINAL_REQUEST |
| 6 | Zero-Division Safety | Safe handling of divide by zero returning clear error/result without crash | M1 | Acceptance Criteria |
| 7 | Float Precision | Correct float/double precision handling in arithmetic operations | M1 | Acceptance Criteria |
| 8 | Calculator Service | Orchestrates execution, duration tracking, history recording, metrics queries | M1 | R2, TICKET-02 |
| 9 | History Persistence | Saves each execution (operands, result, duration ms, UTC timestamp) to Dapper repo | M1 | R2, TICKET-02 |
| 10 | 3 Recent Executions | Queries 3 most recent execution histories matching the same operation key | M1 | R2, TICKET-02 |
| 11 | Monthly UTC Count | Counts executions of operation key from 00:00:00 UTC on 1st of current month | M1 | R2, TICKET-02 |
| 12 | DI Bug Fix | Fix DbConnectionFactory registration in Gaya.Infrastructure DependencyInjection | M1 | survey_code_explorer_1 |
| 13 | Calculate Endpoint | POST /api/calculate accepts OperationKey, FieldA, FieldB; returns result+metrics | M2 | R3, TICKET-03 |
| 14 | Execute Route Alias | POST /api/execute aliased to /api/calculate for complete interoperability | M2 | survey_spec_miner_1 |
| 15 | Operations Query | GET /api/operations returns list of available operation definitions | M2 | R3, TICKET-03 |
| 16 | Operation Creation | POST /api/operations creates new dynamic operation definition | M2 | R3, TICKET-03 |
| 17 | Operation History API | GET /api/operations/history queries paginated or recent calculation history | M2 | R3, TICKET-03 |
| 18 | Operation Metrics API | GET /api/operations/{key}/metrics returns recent 3 + monthly count | M2 | R3, TICKET-03 |
| 19 | API Audit Middleware | Captures full request & response bodies, status code, latency, IP to ApiAuditLogs | M2 | R3, TICKET-03 |
| 20 | RFC 7807 Error Handler | Global exception middleware returning ProblemDetails and logging to SystemErrors | M2 | R3, TICKET-03 |
| 21 | OpenTelemetry Instrumentation | Tracing and logging exported via OTLP to OpenObserve (port 5080) | M2 | R3, ADR-0003 |
| 22 | CORS Configuration | CORS policy configured to allow Angular frontend origins | M2 | R3, survey_code_explorer_1 |
| 23 | Angular Service Layer | OperationService & CalculatorService calling ASP.NET Core Web API | M3 | R4, ADR-0004 |
| 24 | Calculator View | Operation dropdown, contextual Field A / B labels, calculate button, result card | M3 | R4, TICKET-04 |
| 25 | Recent Executions Table | Displays 3 most recent executions of selected operation with duration/time | M3 | R4, TICKET-04 |
| 26 | Monthly Count Badge | Dynamic badge displaying current calendar month UTC execution count | M3 | R4, TICKET-04 |
| 27 | Operations Hub View | Lists all operations, category filter tabs, creation form for new operations | M3 | R4, TICKET-04 |
| 28 | Standalone Architecture | Modern Angular 18 standalone components without NgModules | M3 | R4, ADR-0004 |
| 29 | Responsive UI Styling | Clean, modern UI with responsive layout and input validation feedback | M3 | R4, TICKET-04 |
| 30 | Unit Tests Suite | Comprehensive xUnit tests in tests/Gaya.UnitTests for engine, service, persistence | M4 | R5, TICKET-05 |
| 31 | Integration Tests Suite | WebApplicationFactory API tests for endpoints, middleware, audit logs | M4 | R5, TICKET-05 |
| 32 | A34D Verification Test | Automated test asserting "A34D" marker presence in src/ codebase | M4 | R5, ORIGINAL_REQUEST |
| 33 | E2E Test Suite (Tiers 1-4) | 4-tier requirement-driven opaque-box test suite passing 100% | M4 | Project Pattern, R5 |
| 34 | Adversarial Hardening (Tier 5) | White-box stress tests & edge case hardening with Challenger loop | M4 | Project Pattern |
| 35 | Docker Readiness & Readme | Verify Docker Compose configuration and update repository README.md | M4 | R5, TICKET-05 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| E2E | E2E Testing Track | Requirement-driven test harness, runner, Tiers 1-4 tests, TEST_READY.md | none | DONE |
| M1 | Core Engine & Application Services (R1) | Gaya.Application (engine, evaluators, ICalculatorService, DTOs, A34D marker) + Infrastructure DI fix | none | DONE |
| M2 | Dapper Persistence & Metrics (R2) | Gaya.Infrastructure (repositories, SQL schema, DatabaseInitializer, seed data) | M1 | DONE |
| M3 | Web API, Audit Middleware & Observability (R3) | Gaya.Api (controllers/endpoints, audit middleware, RFC 7807 handler, OpenTelemetry, CORS) | M1, M2 | DONE |
| M4 | Angular Standalone Client UI (R4) | client/ (Calculator view, Operations Hub view, services, routing, styling) | M3 | DONE |
| M5 | Verification, Docker & Documentation (R5) | tests/Gaya.UnitTests, 100% E2E pass, Docker Compose, README.md | E2E, M1-M4 | IN_PROGRESS |



## Interface Contracts
### Gaya.Application ↔ Gaya.Domain
- `IDynamicOperationEngine`:
  - `Task<CalculationResult> EvaluateAsync(OperationDefinition operation, string fieldA, string fieldB, CancellationToken ct = default)`
- `ICalculatorService`:
  - `Task<CalculationResponseDto> CalculateAsync(CalculationRequestDto request, CancellationToken ct = default)`
  - `Task<IEnumerable<OperationDto>> GetOperationsAsync(CancellationToken ct = default)`
  - `Task<OperationDto> CreateOperationAsync(CreateOperationDto dto, CancellationToken ct = default)`
  - `Task<OperationMetricsDto> GetMetricsAsync(string operationKey, CancellationToken ct = default)`
  - `Task<IEnumerable<OperationHistoryDto>> GetHistoryAsync(int limit = 50, CancellationToken ct = default)`

### Gaya.Api ↔ Gaya.Application
- `POST /api/calculate` (and alias `POST /api/execute`):
  - Request: `{ "operationKey": string, "fieldA": string, "fieldB": string }`
  - Response: `{ "operationKey": string, "fieldA": string, "fieldB": string, "result": string, "durationMs": long, "executedAt": DateTime, "recentExecutions": [...], "monthlyExecutionCount": int }`
- `GET /api/operations` -> `OperationDto[]`
- `POST /api/operations` -> `OperationDto`
- `GET /api/operations/{key}/metrics` -> `{ "operationKey": string, "monthlyExecutionCount": int, "recentExecutions": [...] }`

### Angular Client ↔ Gaya.Api
- Standard JSON REST communication with baseUrl configurable via environment (default `http://localhost:5000` or relative `/api`).

## Code Layout & Write Ownership
- **Track 1 (E2E Test Writer)**:
  - Exclusive write ownership: `tests/Gaya.UnitTests/E2E/` or test suite verification scripts.
- **Milestone 1 (Worker M1)**:
  - Exclusive write ownership: `src/Gaya.Application/**`, `src/Gaya.Infrastructure/DependencyInjection.cs`.
- **Milestone 2 (Worker M2)**:
  - Exclusive write ownership: `src/Gaya.Api/**`.
- **Milestone 3 (Worker M3)**:
  - Exclusive write ownership: `client/src/**`.
- **Milestone 4 (Worker M4)**:
  - Exclusive write ownership: `tests/Gaya.UnitTests/**` (unit/integration test files), `README.md`.
