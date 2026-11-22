# Operating the API

- Monitor `/health/live` for process restarts and `/health/ready` for connectivity
  and unapplied migrations.
- Set `Observability__ExportOtlp=true` and `OTEL_EXPORTER_OTLP_ENDPOINT` to an approved
  private collector. Set protocol, authentication, and TLS through operator-managed
  environment settings.
- The server exports ASP.NET Core request metrics and traces, plus
  `focus.database.readiness_failures` and `focus.activity.persisted_windows`. Exported
  spans omit request paths, query strings, and client addresses.
- The app emits JSON console logs. Route them through a platform log drain or a collector.
  Apply access controls, rotation, and a retention period.
- Match `X-Request-Id` and Problem Details `traceId` with request correlation in
  logs and traces. Investigate elevated 5xx, sustained readiness failures, and unusual
  ingestion rejection rates. Derive alert thresholds from the deployment baseline.

Telemetry classification is synchronous and bounded to 100 intervals. Observation and
estimate commit inside one transaction. A future background pipeline needs persisted work
items, retries, idempotent consumers, and backlog monitoring. Analytics is scoped and
time-bounded. Add caching after measuring repeat workload and defining invalidation.
