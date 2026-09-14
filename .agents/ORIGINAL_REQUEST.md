# Original User Request

## 2026-09-13T18:52:58Z

Implement the complete production-grade .NET 8 & Angular Operations Platform specified in `.scratch/` and documented in `docs/` and `CONTEXT.md`. The platform delivers a dynamic 2-operand extensible calculation engine, Dapper persistence, full HTTP audit logging, monthly/recent metrics, OpenTelemetry observability, and an Angular client UI.

Working directory: /Users/admin/Projects/Gaya-project
Integrity mode: benchmark

## Requirements

### R1. Dynamic 2-Operand Operations Engine
Implement a runtime-extensible computation engine that processes exactly two operands (Field A and Field B) across arithmetic, string manipulation, and external REST API (Open-Meteo weather) categories without requiring recompilation. Embed the mandatory "A34D" code comment in the engine implementation.

### R2. Dapper Persistence & Bonus Metrics
Implement Dapper-based SQL Server repositories and application services to record operation history, query the 3 most recent executions of the same operation type, compute the aggregated monthly count from the 1st of the current calendar month UTC, and persist system errors and full request/response API audit trails.

### R3. ASP.NET Core 8 Web API & Observability
Provide clean REST endpoints (GET/POST /api/operations, POST /api/calculate), custom request/response audit middleware capturing complete payloads into ApiAuditLogs, global exception handling logging unhandled faults to SystemErrors, and OpenTelemetry instrumentation exporting traces/logs to OpenObserve.

### R4. Angular Standalone Client UI
Deliver an intuitive Angular frontend with a Calculator screen (dynamic dropdown, contextual operand labels, calculation button, result display, recent executions table, and monthly counter badge) and an Operations Hub for real-time operation creation and management.

### R5. Comprehensive Verification & Documentation
Verify the solution with unit/integration tests in tests/Gaya.UnitTests, verify the "A34D" comment presence, Docker compose services, and update the repository README.md.

## Acceptance Criteria

### Execution & Calculation
- [ ] Arithmetic operations execute accurately with safe zero-division handling and float precision.
- [ ] String operations handle concatenation, delimiter joining, and edge cases (empty strings).
- [ ] External API integration fetches real-time temperature/weather given latitude and longitude.
- [ ] Dynamic additions of new operations via API/UI work immediately without service restart.
- [ ] Codebase contains the mandatory code comment "A34D".

### History, Audit & Metrics
- [ ] Every calculation persists to OperationHistories with duration and timestamp.
- [ ] Response includes the 3 most recent executions matching the same operation key.
- [ ] Response includes the total count of executions of that operation key since the 1st of current month UTC.
- [ ] HTTP middleware captures complete incoming/outgoing payloads in ApiAuditLogs.
- [ ] Unhandled exceptions return RFC 7807 ProblemDetails and log to SystemErrors.

### Client & Quality
- [ ] Angular standalone UI connects to backend and reflects live operations.
- [ ] All unit and integration tests in tests/Gaya.UnitTests pass cleanly.
- [ ] Solution builds with zero errors and passes static analysis (dotnet build, ng build).
