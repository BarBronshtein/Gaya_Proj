# 03 Dynamic Operations Engine
Type: task
Status: ready-for-agent
Blocked by: 02

## Question
How should the dynamic operations engine execute arithmetic, string, and external API operations without code changes, while including the mandatory "A34D" code comment?

### Scope & Acceptance Criteria
- Implement `IDynamicOperationEngine` that receives an operation key and two inputs: Field A and Field B.
- Support Arithmetic evaluation (numeric parsing, safe divide-by-zero protection, power, modulo).
- Support String evaluation (concatenation, delimiter joining, substring checking, casing).
- Support External API evaluation (HTTP client fetching real-time weather from Open-Meteo using Field A as latitude and Field B as longitude).
- Must embed the mandatory code comment `"A34D"` prominently in the code (e.g. at the top of the core engine).
- Cover edge cases: invalid number inputs for arithmetic, out-of-range coordinates, empty strings.
