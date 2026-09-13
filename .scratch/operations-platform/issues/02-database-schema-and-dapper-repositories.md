# 02 Database Schema and Dapper Repositories
Type: task
Status: resolved
Assignee: Antigravity
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

## Answer
Successfully implemented the complete relational schema, Dapper repositories, database bootstrapping, and initial seed records:
1. **Domain Layer**:
   - `OperationCategory` enum (`Arithmetic`, `String`, `ExternalApi`).
   - Domain entities: `OperationDefinition`, `OperationHistory`, `ApiAuditLog`, `SystemError`.
   - Repository interfaces: `IOperationRepository`, `IOperationHistoryRepository`, `IApiAuditLogRepository`, `ISystemErrorRepository`.
2. **Infrastructure Layer**:
   - `IDbConnectionFactory` and `DbConnectionFactory` with resilient catalog switching between master and target `OperationsDb`.
   - `DatabaseInitializer` with idempotent DDL:
     - `Operations` table with primary key `[Key]`.
     - `OperationHistories` table with compound index `IX_OperationHistories_Key_ExecutedAt` on `([OperationKey], [ExecutedAt] DESC)` for sub-millisecond 3-recent-records queries and `IX_OperationHistories_ExecutedAt` for monthly count queries.
     - `ApiAuditLogs` table with index on `[CreatedAt] DESC`.
     - `SystemErrors` table with index on `[CreatedAt] DESC`.
     - Seeds 11 default operations across Arithmetic (6), String (4), and External API (1 - Open-Meteo).
     - Exponential backoff retry loop for containerized SQL Server startup resilience.
   - Dapper repositories: `OperationRepository`, `OperationHistoryRepository`, `ApiAuditLogRepository`, `SystemErrorRepository`.
   - Dependency injection registration in `DependencyInjection.AddInfrastructure()`.
3. **Verification**:
   - Solution builds cleanly across all 5 projects (`0 Warning(s), 0 Error(s)`).
   - 8 unit tests created in `Gaya.UnitTests` covering domain entities, connection factory catalog manipulation, and DI registration passing 100%.
