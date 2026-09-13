---
id: TICKET-01
title: Core Domain Abstraction & Dynamic Operation Engine
label: ready-for-agent
priority: P0
section: Part A (Mandatory)
---
# TICKET-01: Core Domain Abstraction & Dynamic Operation Engine

## Description
Implement the core domain model and extensible operation execution system.

## Requirements
1. Define `IOperation` interface with metadata:
   - `Id`, `Name`, `Description`, `Category`, `FieldADescription`, `FieldBDescription`
   - `Task<OperationResult> ExecuteAsync(string fieldA, string fieldB, CancellationToken ct)`
2. Built-in Operations:
   - Arithmetic: Addition, Subtraction, Multiplication, Division (with divide-by-zero validation), Power, Modulo.
   - String: Concatenation with custom separator, Levenshtein Distance, Substring/Contains check, Regex Match.
   - External API: Weather Forecast (Open-Meteo) taking Latitude (Field A) and Longitude (Field B), returning current temperature, weather code, and conditions.
3. Dynamic Operations Engine:
   - Support adding, updating, and removing custom operations at runtime without code changes or redeployment.
   - Database / configuration backed definitions evaluated safely via Roslyn Scripting / dynamic expression engine.
4. Mandatory Code Marker:
   - Include comment `// A34D` in the code as required by Section 2.1.
