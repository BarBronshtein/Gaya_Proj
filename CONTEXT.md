# Operations Platform Context

A dynamic 2-operand computational and integration platform enabling runtime extensibility without recompilation, execution telemetry, and audit history.

## Language

**Operation**:
A single executable unit of work that always operates on exactly two inputs (Field A and Field B) to produce an output.
_Avoid_: Function, procedure, task

**Field A**:
The primary operand passed into an Operation (e.g. first number in arithmetic, source string in text processing, latitude in geolocation).
_Avoid_: First input, param1, argA

**Field B**:
The secondary operand passed into an Operation (e.g. second number in arithmetic, delimiter/argument in text processing, longitude in geolocation).
_Avoid_: Second input, param2, argB

**Operation Definition**:
The persisted configuration defining an Operation's metadata, operand types, execution category, and evaluation template.
_Avoid_: Operation schema, rule config

**Operation History**:
A persistent record of a completed operation run, capturing exact inputs, computed result, execution duration, and timestamp.
_Avoid_: Calculation log, audit record

**Monthly Operation Count**:
The aggregated count of executions for a specific Operation type starting from the first day of the current calendar month at 00:00:00 UTC up to the present.
_Avoid_: Monthly usage, total executions

**Recent Executions**:
The three most recent Operation History records matching the same Operation type as the current calculation.
_Avoid_: History preview, last runs

**API Audit Log**:
A persistent trace of incoming HTTP requests and outgoing HTTP responses including payload bodies, status code, latency, and caller IP.
_Avoid_: Request trace, access log

**System Error**:
An unhandled runtime failure or infrastructure fault logged with full stack trace, path, and error code.
_Avoid_: Bug report, crash log
