# 04 Application Services and Bonus Metrics
Type: task
Status: ready-for-agent
Blocked by: 03

## Question
How should the execution workflow orchestrate operation calculation, history persistence, 3 recent executions retrieval, and monthly aggregation count?

### Scope & Acceptance Criteria
- Implement `ICalculatorService`:
  1. Validate operation existence and active status.
  2. Execute calculation through `IDynamicOperationEngine`.
  3. Measure elapsed time and record execution to `OperationHistories` table via Dapper.
  4. Query and return the 3 most recent executions of the exact same operation type.
  5. Compute and return the total count of executions of this operation type since the start of the current calendar month (1st of month at 00:00:00 UTC).
  6. Return aggregated DTO containing `Result`, `RecentExecutions`, and `MonthlyExecutionCount`.
