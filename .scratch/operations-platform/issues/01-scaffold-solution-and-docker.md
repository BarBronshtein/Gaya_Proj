# 01 Scaffold Solution and Docker Infrastructure
Type: task
Status: in-progress
Assignee: Antigravity
Blocked by: none

## Question
How should the .NET 8 solution and Docker Compose infrastructure (MS SQL Server 2022 + OpenObserve) be scaffolded to provide a reproducible, clean development and evaluation environment?

### Scope & Acceptance Criteria
- Create `docker-compose.yml` defining MS SQL Server 2022 (port 1433) and OpenObserve (port 5080).
- Scaffold .NET 8 Clean Architecture solution:
  - `src/Gaya.Domain`
  - `src/Gaya.Application`
  - `src/Gaya.Infrastructure`
  - `src/Gaya.Api`
  - `tests/Gaya.UnitTests`
- Add solution file `Gaya.OperationsPlatform.sln` linking all projects.
- Scaffold Angular application under `client/`.
