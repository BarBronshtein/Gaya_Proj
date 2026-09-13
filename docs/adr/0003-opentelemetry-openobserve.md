# 0003: OpenTelemetry and OpenObserve Observability

We decided to integrate OpenTelemetry across the .NET 8 Web API for unified traces, metrics, and logs, exported via standard OTLP protocol to an OpenObserve container running in Docker Compose alongside SQL Server. This provides enterprise-level observability, real-time dashboarding, and log searchability while preserving custom database audit logging for requirement compliance.
