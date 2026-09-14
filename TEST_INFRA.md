# TEST_INFRA.md — Gaya Operations Platform E2E Testing Infrastructure

## 1. Test Philosophy: Opaque-Box & Requirement-Driven

The testing infrastructure for the **Gaya Operations Platform** adheres to an **opaque-box, requirement-driven verification philosophy**. Under this model:
- **Contract Boundary Testing**: The platform is evaluated strictly against observable contracts—HTTP REST API endpoints, standardized JSON request and response payloads, database state transitions, and error formats (RFC 7807 ProblemDetails).
- **Implementation Agnosticism**: Tests assert on *what* the system accomplishes, not *how* internal private methods or classes execute. Internal refactorings do not break tests as long as external contracts and invariants hold.
- **Authoritative Output Derivation**: Every test case derives expected results from mathematical definitions, standard string semantics, Open-Meteo schema specifications, or documented project requirements in `PROJECT.md` and `ORIGINAL_REQUEST.md`.
- **Environment Isolation**: The test suite is 100% self-contained and executable in disconnected/offline environments without requiring live cloud services, external network access, or an active Docker daemon.

---

## 2. Feature Inventory

The 35 system features defined in `PROJECT.md` map to concrete test coverage across Tiers 1–4:

| Feature # | Feature Name | Category | Primary Verification Tier | Target Component |
| :---: | :--- | :--- | :---: | :--- |
| **F-01** | Arithmetic Evaluator | Core Engine | Tier 1, Tier 2 | `Gaya.Application` / `POST /api/calculate` |
| **F-02** | String Evaluator | Core Engine | Tier 1, Tier 2 | `Gaya.Application` / `POST /api/calculate` |
| **F-03** | Weather Evaluator | Core Engine | Tier 1, Tier 2 | `Gaya.Application` / `POST /api/calculate` |
| **F-04** | Dynamic Extensibility | Core Engine | Tier 1, Tier 3, Tier 4 | `Gaya.Application` / `POST /api/operations` |
| **F-05** | Mandatory Marker "A34D" | Verification | Tier 1, Tier 2 | `src/` Codebase Scan |
| **F-06** | Zero-Division Safety | Safety | Tier 2, Tier 4 | `Gaya.Application` / `POST /api/calculate` |
| **F-07** | Float Precision | Numerical | Tier 2 | `Gaya.Application` / `POST /api/calculate` |
| **F-08** | Calculator Service | Application | Tier 1, Tier 3 | `Gaya.Application` |
| **F-09** | History Persistence | Data / Dapper | Tier 1, Tier 3 | `Gaya.Infrastructure` / `OperationHistories` |
| **F-10** | 3 Recent Executions | Analytics / Bonus | Tier 1, Tier 2, Tier 3 | `OperationHistories` / Response Payload |
| **F-11** | Monthly UTC Count | Analytics / Bonus | Tier 1, Tier 2, Tier 3 | `OperationHistories` / Response Payload |
| **F-12** | DI Registration Bug Fix | Architecture | Tier 1 | `Gaya.Infrastructure.DependencyInjection` |
| **F-13** | Calculate Endpoint | Web API | Tier 1, Tier 3 | `POST /api/calculate` |
| **F-14** | Execute Route Alias | Web API | Tier 1, Tier 3 | `POST /api/execute` |
| **F-15** | Operations Query API | Web API | Tier 1 | `GET /api/operations` |
| **F-16** | Operation Creation API | Web API | Tier 1, Tier 3, Tier 4 | `POST /api/operations` |
| **F-17** | Operation History API | Web API | Tier 1 | `GET /api/operations/history` |
| **F-18** | Operation Metrics API | Web API | Tier 1, Tier 3 | `GET /api/operations/{key}/metrics` |
| **F-19** | API Audit Middleware | Observability | Tier 1, Tier 2, Tier 3 | `ApiAuditLogs` / Middleware |
| **F-20** | RFC 7807 Error Handler | Observability / Error | Tier 1, Tier 2, Tier 3 | `SystemErrors` / Middleware |
| **F-21** | OpenTelemetry Setup | Observability | Tier 1 | OpenObserve OTLP Configuration |
| **F-22** | CORS Configuration | Web API | Tier 1 | API Middleware Pipeline |
| **F-23** | Angular Service Layer | Frontend Client | Tier 1, Tier 3 | `client/src/app/services` |
| **F-24** | Calculator View | Frontend Client | Tier 1, Tier 4 | `client/src/app/calculator` |
| **F-25** | Recent Executions Table | Frontend Client | Tier 1, Tier 4 | `client/src/app/calculator` |
| **F-26** | Monthly Count Badge | Frontend Client | Tier 1, Tier 4 | `client/src/app/calculator` |
| **F-27** | Operations Hub View | Frontend Client | Tier 1, Tier 4 | `client/src/app/operations-hub` |
| **F-28** | Standalone Architecture | Architecture | Tier 1 | Angular 18 Component Layout |
| **F-29** | Responsive UI Styling | Frontend Client | Tier 1 | Component CSS / Layout |
| **F-30** | Unit Tests Suite | Verification | Tier 1, Tier 2 | `tests/Gaya.UnitTests` |
| **F-31** | Integration Tests Suite | Verification | Tier 3 | WebApplicationFactory API Tests |
| **F-32** | A34D Verification Test | Verification | Tier 1, Tier 2 | Automated Comment Test |
| **F-33** | E2E Test Suite (Tiers 1-4) | Verification | Tiers 1-4 | `tests/Gaya.UnitTests/E2E` |
| **F-34** | Adversarial Hardening (Tier 5) | Verification | Tier 5 (Post-M4) | White-box Stress Tests |
| **F-35** | Docker Readiness & Readme | Verification / Docs | Tier 1, Tier 2 | `docker-compose.yml`, `README.md` |

---

## 3. 4-Tier Test Architecture

The Gaya test harness is organized into four complementary tiers guaranteeing exhaustive verification:

```
┌──────────────────────────────────────────────────────────────────────────┐
│                   Tier 4: Real-World Scenarios (5)                       │
│ - Financial Pipeline  - Multilingual Cleansing  - Weather Telemetry      │
│ - Zero-Downtime Dynamic Extension  - Fault Tolerance & Audit Resilience  │
├──────────────────────────────────────────────────────────────────────────┤
│             Tier 3: Cross-Feature Combinations (Pairwise) (8)            │
│ - Engine + Persistence  - Engine + Metrics  - Engine + Audit Middleware  │
│ - Dynamic Creation + Immediate Calc  - Fault + RFC 7807 + Error Logging  │
├──────────────────────────────────────────────────────────────────────────┤
│                 Tier 2: Boundary & Corner Cases (28)                     │
│ - Zero-Division  - Float Precision  - Extreme Limits  - Empty/Nulls      │
│ - Geo Bounds  - Month UTC Boundary  - Malformed JSON  - SQL Injection    │
├──────────────────────────────────────────────────────────────────────────┤
│                 Tier 1: Feature Coverage (Happy Paths) (26)              │
│ - Arithmetic (Add, Sub, Mul, Div, Pow, Mod)  - String (Concat, Delim...) │
│ - Weather  - Dynamic Expression  - Persistence & History  - Metrics APIs │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Tier 1: Feature Coverage (>=5 Tests per Core Feature)
- **Engine (R1)**: Verifies exact outputs for all built-in arithmetic (`add`, `subtract`, `multiply`, `divide`, `power`, `modulo`), string (`concat`, `join-delim`, `contains`, `char-frequency`), external weather API, and custom dynamic formulas (`(A * 2) + B`).
- **Persistence & Metrics (R2)**: Verifies that calculation runs record duration and timestamps to `OperationHistories`, recent-3 executions query returns the newest records in descending order, monthly count aggregates from `00:00:00 UTC` on the 1st of the current month, and audit/error records persist accurately.
- **REST API (R3)**: Verifies `GET /api/operations`, `POST /api/calculate`, `POST /api/execute` (alias), `POST /api/operations`, and middleware request/response capture.
- **Verification (R5)**: Asserts automated detection of mandatory marker `"A34D"`, clean compilation, Docker compose service definitions, and documentation sections.

### 3.2 Tier 2: Boundary & Corner Cases (>=5 Tests per Core Feature)
- **Zero-Division**: Ensures dividing by zero or calculating modulo by zero produces graceful handling without unhandled process termination.
- **Numerical Precision**: Asserts exact IEEE 754 float precision (e.g. `0.1 + 0.2` = `0.3`) without drifting.
- **Extreme Limits**: Evaluates very large values (e.g. `1e308`) and overflow boundaries.
- **String Boundaries**: Null, empty string (`""`), and whitespace-only operands for string operations.
- **Coordinate Boundaries**: Out-of-bounds geographic coordinates (e.g. latitude `95.0`, longitude `200.0`).
- **Persistence & Time Boundaries**: Month-rollover timestamps (`2026-08-31T23:59:59.999Z` vs `2026-09-01T00:00:00.000Z`) to ensure strict UTC monthly partitioning.
- **Security & Large Payloads**: SQL injection payloads safely parameterized; 64KB HTTP request payloads persisted to `NVARCHAR(MAX)` without truncation.
- **API Faults**: Unknown operation key returning `404 ProblemDetails`, deactivated operation returning `400 Bad Request`, malformed JSON syntax returning `400`.

### 3.3 Tier 3: Cross-Feature Combinations (Pairwise Coverage)
Evaluates integration contracts across feature boundaries:
1. **XF-01 (Engine + Persistence)**: Calculation executes $\rightarrow$ elapsed duration measured $\rightarrow$ record immediately saved to `OperationHistories`.
2. **XF-02 (Calculation + Metrics)**: 4 consecutive calculations update recent-3 queue and increment monthly count by 4.
3. **XF-03 (Engine + Audit Middleware)**: Calculation through API logs verbatim JSON request/response bodies and HTTP status into `ApiAuditLogs`.
4. **XF-04 (Dynamic Creation + Zero-Restart Calculation)**: Admin posts new operation $\rightarrow$ immediately available for calculation without process restart.
5. **XF-05 (Fault + Exception Handler + Error Logging)**: Unhandled calculation fault returns RFC 7807 `application/problem+json`, logs to `SystemErrors`, and logs 500 status to `ApiAuditLogs`.
6. **XF-06 (External API + History Latency)**: External weather call network latency tracked in `OperationHistories.DurationMs`.
7. **XF-07 (Dynamic Configuration + Discovery)**: Dynamic operation creation reflects immediately in `GET /api/operations` query.
8. **XF-08 (Calculation + Live Metrics Aggregation)**: Live metrics query (`GET /api/operations/{key}/metrics`) returns updated monthly count and recent items.

### 3.4 Tier 4: Real-World Application Scenarios (End-to-End User Workflows)
1. **Scenario 1: Financial Multi-Step Calculation Pipeline**: Chained calculation flow (`add` $\rightarrow$ `multiply` $\rightarrow$ `divide` $\rightarrow$ `modulo`) maintaining historical records and sub-millisecond latencies.
2. **Scenario 2: Multilingual Text Cleansing & Content Ingestion**: Mixed Hebrew and English text processing (`contains`, `join-delim`, `char-frequency`) ensuring UTF-8 Unicode preservation in SQL Server.
3. **Scenario 3: Real-Time Field Weather Telemetry Dispatch**: Dual coordinate dispatch (Tel Aviv and London) verifying parsed telemetry vs boundary rejection for invalid coordinates.
4. **Scenario 4: Dynamic Extensibility Lifecycle (Zero-Downtime Extension)**: Admin introduces a new BMI calculator (`A / (B * B)`); user calculates BMI with weight and height; metrics and history update immediately.
5. **Scenario 5: Fault Tolerance, Error Transparency & Audit Resilience**: Adverse payloads (zero division, malformed numbers, disabled operations) tested against global RFC 7807 handler, verifying complete audit trail and error logging.

---

## 4. Test Directory Layout

```
tests/Gaya.UnitTests/
├── DomainModelTests.cs                    # Domain entity & enum initialization
├── InfrastructureTests.cs                  # DbConnectionFactory & DI registration
└── E2E/                                   # Requirement-Driven Opaque-Box E2E Suite
    ├── Contracts/                         # Strongly-typed API Contract DTOs
    │   ├── CalculationRequestDto.cs       # OperationKey, FieldA, FieldB
    │   ├── CalculationResponseDto.cs      # Result, DurationMs, Recent, MonthlyCount
    │   ├── CreateOperationDto.cs          # Dynamic operation creation DTO
    │   ├── OperationDto.cs                # Operation metadata DTO
    │   ├── OperationHistoryDto.cs         # Historical calculation execution DTO
    │   ├── OperationMetricsDto.cs         # Bonus metrics query DTO
    │   └── ProblemDetailsDto.cs           # RFC 7807 ProblemDetails contract
    ├── Harness/                           # Opaque-Box Test Harness & Test Doubles
    │   ├── E2ETestHarness.cs              # Unified client (HTTP / In-Memory double)
    │   ├── InMemoryRepositories.cs        # In-memory doubles of domain repos
    │   └── ReferenceCalculationEngine.cs  # High-fidelity reference engine
    ├── Tier1/                             # Tier 1: Feature Coverage Tests
    │   ├── Tier1FeatureCoverageEngineTests.cs
    │   ├── Tier1FeatureCoveragePersistenceTests.cs
    │   ├── Tier1FeatureCoverageApiTests.cs
    │   └── Tier1FeatureCoverageVerificationTests.cs
    ├── Tier2/                             # Tier 2: Boundary & Corner Cases Tests
    │   ├── Tier2BoundaryEngineTests.cs
    │   ├── Tier2BoundaryPersistenceTests.cs
    │   ├── Tier2BoundaryApiTests.cs
    │   └── Tier2BoundaryVerificationTests.cs
    ├── Tier3/                             # Tier 3: Cross-Feature Combinations
    │   └── Tier3CrossFeatureTests.cs
    ├── Tier4/                             # Tier 4: Real-World Application Scenarios
    │   └── Tier4RealWorldScenarioTests.cs
    └── MandatoryMarkerA34DTests.cs        # Verification test for "A34D" comment
```

---

## 5. Test Invocation & Verification Commands

### 5.1 Run Full Backend Test Suite
```bash
dotnet test --logger "console;verbosity=normal"
```

### 5.2 Run E2E Test Suite Only
```bash
dotnet test --filter "FullyQualifiedName~E2E" --logger "console;verbosity=normal"
```

### 5.3 Run by Specific Tier
```bash
# Tier 1 (Feature Coverage)
dotnet test --filter "FullyQualifiedName~Tier1"

# Tier 2 (Boundary & Corner Cases)
dotnet test --filter "FullyQualifiedName~Tier2"

# Tier 3 (Cross-Feature Combinations)
dotnet test --filter "FullyQualifiedName~Tier3"

# Tier 4 (Real-World Scenarios)
dotnet test --filter "FullyQualifiedName~Tier4"

# Mandatory Code Marker "A34D" Test
dotnet test --filter "FullyQualifiedName~MandatoryMarkerA34DTests"
```

### 5.4 Run with Live HTTP Web API (Integration Mode)
When running the ASP.NET Core Web API on port 5000:
```bash
GAYA_API_BASE_URL="http://localhost:5000" dotnet test --filter "FullyQualifiedName~E2E"
```
