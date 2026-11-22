# Backend implementation and evidence

Baseline `21f05fb` built without warnings or errors. It contained six product
projects, two migrations and no business HTTP endpoints or tests. Backend v1
keeps those projects and historical migrations. It adds two test projects,
API and architecture documentation, managed dependency lockfiles, and
deployment artifacts.

## Phase checklist

| Phase | Changes and verification | Status |
|---|---|---|
| 0. Baseline | Verified solution, references, Agent integration boundary, configurations and migrations. Built the original solution. | PASS |
| 1. API foundation | `Focus.Api/Hosting/` adds validation, Problem Details, readiness, HTTPS, CORS, tracing and throttling. HTTP tests run against the real host configuration. | PASS |
| 2. Authentication | `Focus.Application/Accounts/`, `Focus.Infrastructure/Accounts/`, `AuthController`. Registration, login, expiry, concurrent rotation, logout and password change verified. | PASS |
| 3–4. Sessions and goals | `FocusSession`, `SessionService`, `SessionStore`, `SessionsController`. Pause/resume, bounded history, goal mutations and concurrent completion tested. | PASS |
| 5–6. Telemetry and engine | `TelemetryService`, `ResearchStore`, `FocusClassifier`. Ownership, consent, atomic rollback, repeated upload, linked estimates and all six classification outcomes tested. | PASS |
| 7–8. Analytics and settings | `AnalyticsService`, `AnalyticsStore`, `SettingsService`, `SettingsStore`, privacy endpoints. Persistence, DST, overlap handling, isolation and deletion verified. | PASS |
| 9. Contract and docs | Runtime-generated OpenAPI 3.1.1 with 41 operations. Synchronized contract test, independent validator, and PowerShell tutorial passed. | PASS |
| 10. Tests and coverage | 28 unit tests, 5 PostgreSQL HTTP tests, and 1 contract synchronization test passed. Release business-logic line coverage was 91.25%. | PASS |
| 11. Deployment | Release publish, both Compose configurations, local HTTPS smoke, and durability across restart passed. Docker engine returned HTTP 500 and blocked image build. External host pending. | PARTIAL |
| 12. Advanced gates | CI, manual GHCR publish, protected manual deploy, OpenTelemetry and operational docs added. Pipeline and collector require an operational environment. | PARTIAL |

## Mandatory requirement-to-evidence matrix

| Requirement | Component and file paths | Tests and command | Status | Limitation |
|---|---|---|---|---|
| Modern framework and build | `Focus.Api/Focus.Api.csproj`, `global.json`, `FOCUS ASSISTANT.slnx` | `dotnet build "FOCUS ASSISTANT.slnx" -c Release --no-restore -warnaserror` | PASS | Local .NET 10 build completed with zero warnings and errors. |
| PostgreSQL and migrations | `Focus.Infrastructure/Persistence/FocusDbContext.cs`, `Configurations/`, `Migrations/` | `UpgradesFromPreviouslyTrackedMigrationsWithoutDroppingExistingRows`; `dotnet test Focus.Api.IntegrationTests` with disposable DB | PASS | Clean install and upgrade verified on PostgreSQL 17. Existing databases were outside the test scope. |
| Layering and SOLID | `Focus.Application/Sessions/ISessionStore.cs`, `Focus.Infrastructure/Persistence/SessionStore.cs`, `docs/architecture/backend.md` | Application unit tests and PostgreSQL HTTP workflows | PASS | Composition root references the EF adapter and routes use application contracts. |
| Persisted business API and HTTP status codes | `Focus.Api/Controllers/`, `Focus.Infrastructure/Persistence/` | `WorkflowPersistsLifecycleAndLimitsEachUserToTheirOwnResources`, `PersistsObservedAndEstimatedDataAtomicallyAndKeepsResearchOwnerScoped` | PASS | Real persistence and concurrency verified locally. |
| Validation and global exception handling | `Focus.Api/Hosting/ApiHosting.cs`, `ApiErrorHandler.cs`, `Focus.Application/Common/UseCaseException.cs` | `SendsCorsAndTraceHeadersWithSafeValidationAndRateLimitErrors`, session and research workflow tests | PASS | Sensitive inputs are excluded from validation responses. |
| Structured logging and health | `Focus.Api/Program.cs`, `DatabaseHealthCheck.cs`, `ApiObservability.cs` | Health endpoints before and after migration, request trace ID and HTTPS smoke | PASS | Local process and PostgreSQL probes verified. |
| Security and configuration | `AccountAuthentication.cs`, `ProductionTransport.cs`, `AccountStore.cs`, `DependencyInjection.cs` | Credential expiry, refresh replay, logout, password-change revocation, cross-user isolation, CORS and rate-limit tests | PASS | Configure production secret injection and a trusted ingress for external deployment. |
| API description | `docs/api/openapi.json`, `docs/api/integration-tutorial.md`, `ContractTests.cs` | Contract synchronization and `python -m openapi_spec_validator docs/api/openapi.json` | PASS | 41 operations on 30 paths. |
| At least 70% business-logic coverage | `Focus.Backend.Tests/`, `docs/testing/business-logic.runsettings`, `scripts/check-coverage.py` | Run the Coverlet command in `docs/testing/backend.md`; `python scripts/check-coverage.py artifacts/final-backend-coverage --minimum 80` | PASS | 28 unit tests. 688/754 lines, 91.25%; branches 83.92%. |
| Reproducible local deployment | `global.json`, `packages.lock.json`, `DatabaseMigration.cs`, `docs/deployment/backend.md`, `scripts/smoke-release.py` | Locked restore, Release publish, explicit migration, local certificate and restart smoke | PASS | Proven for native Release. Container run is a separate verification. |
| Externally accessible production deployment | `compose.production.yaml`, `deployment/Caddyfile`, `docs/deployment/backend.md` | External HTTPS and readiness smoke pending | FAIL | No approved hosting provider, credentials, DNS name, or external result. |

## Advanced evidence

| Requirement | Artifacts and check | Status | Limitation |
|---|---|---|---|
| Containerized API and PostgreSQL | `Dockerfile`, `compose.yaml`, `compose.production.yaml`; both `docker compose config --quiet` passed | PARTIAL | Docker Linux engine returned HTTP 500 at `/_ping`. Image build and Compose runtime pending. |
| CI, publication and deployment | `.github/workflows/backend-ci.yml`, `publish-backend.yml`, `deploy-backend.yml` | PARTIAL | GitHub execution, image publication and production deployment are pending. |
| Quality gates | CI uses locked restore, warnings as errors, tests, 80% line threshold, format and contract checks | PASS locally | Test and coverage commands passed locally. GitHub run remains pending. |
| Monitoring and tracing | `ApiObservability.cs`, `DatabaseHealthCheck.cs`, `docs/deployment/observability.md` | PARTIAL | Counters and OTLP exporter available. Collector, dashboards and alert delivery pending. |
| Centralized logging | JSON console logging and operator runbook in `docs/deployment/observability.md` | PARTIAL | Central sink requires deployment configuration. |
| Dependency and secret audit | `dotnet list "FOCUS ASSISTANT.slnx" package --vulnerable --include-transitive --no-restore`; Gitleaks CI job | PARTIAL | NuGet advisories reported no vulnerable packages. Gitleaks CI execution remains pending. |
| Caching and asynchronous processing | `docs/deployment/observability.md`, synchronous bounded ingestion | Scoped out | Introduce after measured latency, freshness, and workload justify them. |
| Service decomposition | `docs/architecture/adr-001-backend-topology.md` | ADR delivered | Modular service remains the chosen architecture. |

Release line coverage by project: Focus.Domain **83.83%**, Focus.Application
**96.98%**, Focus.Engine **100.00%**. Important remaining coverage areas include
rare invalid domain inputs and edge cases for configuration and account operations.
See `docs/testing/backend.md` for exact reproduction commands.

The published artifact completed the tutorial against a fresh database. The
session reached Completed with one history item, two timeline events, one
estimated interval and one session in analytics. The production-mode smoke test
separately verified local TLS and persisted data and credentials across restart.

The Windows Agent uploader, React, and Unity remain separate integration work.
The server rejects window titles. See the verification report for the Agent fix
and the treatment of files produced by older builds. Confirm consent, retention,
backups, and operational ownership before collecting real participant data.
