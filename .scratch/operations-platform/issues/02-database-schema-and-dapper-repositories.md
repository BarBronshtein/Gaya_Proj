# 02 Database Schema and Dapper Repositories
Type: task
Status: ready-for-agent
Blocked by: 01

## Question
How should the relational schema, Dapper mappings, and database initialization be structured for operations, history, and system audit logs?

### Scope & Acceptance Criteria
- Create SQL schema and initialization scripts for:
  - `Operations`: Key, DisplayName, Category (Arithmetic, String, ExternalApi), Template/Rule, FieldAPrompt, FieldBPrompt, IsActive, CreatedAt.
  - `OperationHistories`: Id, OperationKey, FieldA, FieldB, Result, DurationMs, ExecutedAt.
  - `ApiAuditLogs`: Id, Path, Method, StatusCode, LatencyMs, RequestBody, ResponseBody, CreatedAt.
  - `SystemErrors`: Id, ErrorMessage, StackTrace, SourcePath, StatusCode, CreatedAt.
- Seed default operations:
  - Arithmetic: Add (`+`), Subtract (`-`), Multiply (`*`), Divide (`/`), Power (`^`), Modulo (`%`).
  - String: Concatenate, Join with Delimiter, Case-Insensitive Contains, Character Frequency.
  - External API: Weather Forecast (Open-Meteo calling lat/long parameters).
- Implement Dapper repositories with clean query methods and indexing on `(OperationKey, ExecutedAt DESC)`.
