---
id: TICKET-03
title: RESTful Web API, Exception & Request-Response Audit Logging
label: ready-for-agent
priority: P0
section: Part A + Part B (Bonus)
---
# TICKET-03: RESTful Web API, Exception & Request-Response Audit Logging

## Description
Expose clean, documented RESTful endpoints and implement enterprise logging and auditing.

## Requirements
1. API Endpoints:
   - `GET /api/operations`: List all available operations with schemas/metadata.
   - `POST /api/execute`: Execute operation with `{ operationId, fieldA, fieldB }`, returns result, execution time, last 3 operations of same type, and current month count.
   - `POST /api/operations`: Admin endpoint to create a dynamic operation.
   - `DELETE /api/operations/{id}`: Admin endpoint to delete a dynamic operation.
   - `GET /api/history`: Retrieve global execution history with paging.
2. Logging & Audit (Bonus 1.3):
   - Global Exception Handling Middleware for system errors (1.3.1).
   - Request-Response Audit Middleware (1.3.2) logging all HTTP requests and responses (payload, headers, status, timing).
   - Serilog structured file and console sinks.
3. Code Marker:
   - Verify presence of `// A34D` in controller or entrypoint.
