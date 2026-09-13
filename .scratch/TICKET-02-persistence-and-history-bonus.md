---
id: TICKET-02
title: Database Persistence, History & Analytics
label: ready-for-agent
priority: P0
section: Part B (Bonus)
---
# TICKET-02: Database Persistence, History & Analytics

## Description
Implement Entity Framework Core persistence with SQL Server (and SQLite fallback) for execution history and dynamic operation definitions.

## Requirements
1. `OperationExecutionRecord` entity:
   - `Id` (GUID)
   - `OperationId` (string)
   - `OperationName` (string)
   - `FieldA` (string)
   - `FieldB` (string)
   - `Result` (string)
   - `IsSuccess` (bool)
   - `ErrorMessage` (string?)
   - `ExecutionDurationMs` (long)
   - `ExecutedAtUtc` (DateTime)
2. History Query Service (Bonus 1.2):
   - Retrieve the last 3 operations of the same type (`OperationId`).
   - Count operations of the same type executed since the beginning of the current month (`ExecutedAtUtc >= firstDayOfMonthUtc`).
3. Database Support:
   - Microsoft SQL Server provider configured via `AppDbContext`.
   - SQLite development toggle for macOS/local testing without Docker.
   - Docker Compose file for MS SQL Server 2022.
