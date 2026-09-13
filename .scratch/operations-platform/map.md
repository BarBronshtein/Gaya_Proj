# Wayfinder Map: Operations Platform

## Destination

Deliver a production-grade .NET 8 Web API and Angular client implementing Part A (dynamic 2-operand extensible operations engine, REST API, UI, mandatory "A34D" comment) and Part B (Dapper-backed MS SQL Server history persistence, 3 recent executions, monthly execution counter, system error logging, full request/response audit logging, and OpenTelemetry + OpenObserve in Docker Compose).

## Notes

- Platform: .NET 8 (C#) Clean Architecture with Dapper micro-ORM (explicitly no Entity Framework).
- Client: Angular with standalone components (Calculator screen + Operations Hub).
- Database & Observability: MS SQL Server 2022 and OpenObserve containerized via `docker-compose.yml`.
- Critical Marker: The string `"A34D"` must be present as a code comment in the codebase.
- Domain glossary: [`CONTEXT.md`](file:///Users/admin/Projects/Gaya-project/CONTEXT.md)
- Architectural Decisions: [`docs/adr/`](file:///Users/admin/Projects/Gaya-project/docs/adr/)

## Decisions so far

- [0001: Hybrid Dynamic Operations Engine](file:///Users/admin/Projects/Gaya-project/docs/adr/0001-hybrid-dynamic-operations-engine.md) — Runtime evaluation engine for math expressions, string operations, and external REST APIs without recompilation.
- [0002: Dapper and Microsoft SQL Server Persistence](file:///Users/admin/Projects/Gaya-project/docs/adr/0002-dapper-mssql-persistence.md) — High performance Dapper micro-ORM and MS SQL Server 2022 in Docker Compose.
- [0003: OpenTelemetry and OpenObserve Observability](file:///Users/admin/Projects/Gaya-project/docs/adr/0003-opentelemetry-openobserve.md) — OTLP instrumentation in .NET 8 exported to OpenObserve Docker service + custom DB audit tables.
- [0004: Angular Client Architecture](file:///Users/admin/Projects/Gaya-project/docs/adr/0004-angular-client-architecture.md) — Calculator view with bonus metrics plus Operations Hub for live operation management.
- [01 Scaffold Solution and Docker Infrastructure](file:///Users/admin/Projects/Gaya-project/.scratch/operations-platform/issues/01-scaffold-solution-and-docker.md) — .NET 8 Clean Architecture solution, Angular 18 standalone client, and Docker Compose with SQL Server 2022 and OpenObserve.
- [02 Database Schema and Dapper Repositories](file:///Users/admin/Projects/Gaya-project/.scratch/operations-platform/issues/02-database-schema-and-dapper-repositories.md) — Relational schema DDL, Dapper repositories, database bootstrapping with retry logic, and 11 default seeded operations.

## Not yet specified

- Advanced rate limiting or multi-tenant authorization policies (can be added if user requires security hardening beyond the home assignment spec).
- Specialized offline PWA caching service workers for Angular.

## Out of scope

- Entity Framework Core migrations or ORM mapping (ruled out in favor of Dapper).
- Arbitrary N-operand operations (spec explicitly states: "כל הפעולות תמיד מתבצעות על שתי פרמטרים").
