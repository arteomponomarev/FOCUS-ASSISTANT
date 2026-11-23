# Submission evidence index

Date: November 23, 2026. Application baseline: `1e7ddd9b6bfa9501bcdce053baef37c8cb4ac4ad`. The checkpoint adds the reviewed design/evidence files and OpenAPI examples. See the [traceability matrix](compliance-matrix.md), [verification record](final-verification.md) and [test results JSON](evidence/verification-results.json).

## Start a review

| Track | Start here | Reproduce |
| --- | --- | --- |
| Backend Development | [Backend matrix](compliance-matrix.md#backend-development), [architecture and request flows](../architecture/backend.md) | [Backend testing](../testing/backend.md), [build and results](final-verification.md#build-and-test-results) |
| Frontend Development | [Frontend matrix](compliance-matrix.md#frontend-development), [SPA architecture](../frontend/architecture.md) | [Windows walkthrough](../frontend/manual-review-checklist.md), [Playwright and Vitest commands](../frontend/testing.md) |
| Adaptive UI | [Adaptive UI matrix](compliance-matrix.md#adaptive-ui), [six-page PDF](../design/focus-assistant-adaptive-ui.pdf) | [Design system](../design/design-system.md), [responsive audit and PDF export](../design/responsive-audit.md), [accessibility](../frontend/accessibility.md) |
| API Documentation | [API matrix](compliance-matrix.md#api-documentation), [OpenAPI and response examples](../api/README.md#response-examples) | [Getting started](../api/getting-started.md), [integration tutorial](../api/integration-tutorial.md) |

Database and Cloud are separate project tracks. See the [database matrix](compliance-matrix.md#database) and [cloud matrix](compliance-matrix.md#cloud). The [blocker list](critical-blockers.md) gives the remaining deployment work.

## Requirements sources

| Source | Available material |
| --- | --- |
| T01 | Task 01 in the project conversation. Minimum and advanced requirements for Backend, Frontend, Adaptive UI and API Documentation |
| T02 | Task 02 in the project conversation. Production accessibility, reproducibility and backend coverage |
| T05 | Task 05 in the project conversation. Frontend, responsive UI, accessibility and design deliverable |
| T07 | Task 07 in the project conversation. Agent, short sessions, retry, consent and browser integration |
| T10 | Task 10 in the project conversation. Submission audit and separate Database and Cloud tracks |
| Repository plans | [Project README](../../README.md), [backend progress](../implementation/backend-progress.md), [research metrics](../architecture/research-metrics.md), [topology ADR](../architecture/adr-001-backend-topology.md), [deployment plan](../deployment/backend.md) |
| Official institutional rubric | Original form and grading weights were unavailable during the audit. See [blockers](critical-blockers.md). |

## Evidence map

The PDF contains the viewport screenshots. The test results report contains sanitized data from local machine reports. Reproduction commands generate detailed local artifacts. The workflow runs contain GitHub evidence within the configured retention period.

| ID | Evidence | Path or link |
| --- | --- | --- |
| E00 | Git baseline, materials and external boundary | [Scope and Git](final-verification.md#scope-and-git), [blockers](critical-blockers.md) |
| E01 | .NET architecture and Release build | [Solution](<../../FOCUS ASSISTANT.slnx>), [SDK](../../global.json), [composition root](../../Focus.Api/Program.cs), [verification](final-verification.md#build-and-test-results) |
| E02 | PostgreSQL-backed API tests and live OpenAPI | [Session/auth/upgrade](../../Focus.Api.IntegrationTests/SessionWorkflowTests.cs), [research atomicity](../../Focus.Api.IntegrationTests/ResearchWorkflowTests.cs), [constraints and shutdown](../../Focus.Api.IntegrationTests/ReleaseVerificationTests.cs), [synthetic Agent pipeline](../../Focus.Api.IntegrationTests/AgentPipelineTests.cs), [contract tests](../../Focus.Api.IntegrationTests/ContractTests.cs), [results](evidence/verification-results.json) |
| E03 | Business logic and coverage | [Runsettings](../testing/business-logic.runsettings), [coverage gate](../../scripts/check-coverage.py), [session rules](../../Focus.Backend.Tests/SessionTests.cs), [use cases](../../Focus.Backend.Tests/ApplicationTests.cs), [classifier](../../Focus.Backend.Tests/FocusClassifierTests.cs), [coverage results](evidence/verification-results.json) |
| E04 | Frontend tools, build and unit tests | [Package scripts](../../Focus.Web/package.json), [lockfile](../../Focus.Web/package-lock.json), [unit tests](../../Focus.Web/src/tests/), [commands](../frontend/testing.md), [results](evidence/verification-results.json) |
| E05 | Real Playwright UI and responsive coverage | [Workflows](../../Focus.Web/e2e/workflows.spec.ts), [settings](../../Focus.Web/e2e/settings.spec.ts), [QA](../../Focus.Web/e2e/qa.spec.ts), [Agent browser](../../Focus.Web/e2e/agent.spec.ts), [responsive and axe](../../Focus.Web/e2e/responsive.spec.ts), [zoom](../../Focus.Web/e2e/zoom.spec.ts), [browser results](evidence/verification-results.json) |
| E06 | PowerShell tutorial and PostgreSQL catalog | [Tutorial](../api/integration-tutorial.md), [script](../../scripts/demo-backend.ps1), [database model](../../Focus.Infrastructure/Persistence/FocusDbContext.cs), [migrations](../../Focus.Infrastructure/Persistence/Migrations/), [EF mappings](../../Focus.Infrastructure/Persistence/Configurations/), [catalog and tutorial results](final-verification.md#openapi-and-database) |
| E07 | Local production TLS, OTLP and restart smoke | [Smoke script](../../scripts/smoke-release.py), [deployment scope and findings](final-verification.md#deployment-and-runtime-scope), [observability runbook](../deployment/observability.md) |
| E08 | Security, validation and data paths | [HTTP options and limits](../../Focus.Api/Hosting/ApiHosting.cs), [error handler](../../Focus.Api/Hosting/ApiErrorHandler.cs), [authentication](../../Focus.Api/Hosting/AccountAuthentication.cs), [production TLS](../../Focus.Api/Hosting/ProductionTransport.cs), [OTLP](../../Focus.Api/Hosting/ApiObservability.cs), [ingestion](../../Focus.Application/Research/TelemetryService.cs) |
| E09 | API consumer package | [OpenAPI 3.1.1](../api/openapi.json), [response examples](../api/README.md#response-examples), [generator](../../Focus.Api/Hosting/OpenApiContract.cs), [start](../api/getting-started.md), [auth](../api/authentication.md), [errors](../api/error-handling.md), [tutorial](../api/integration-tutorial.md), [versioning](../api/versioning.md), [changelog](../api/changelog.md) |
| E10 | GitHub and workflow evidence | [Checks on main](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions?query=branch%3Amain), [baseline Frontend CI](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions/runs/45083706969), [baseline Backend CI](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions/runs/45083706994), [frontend workflow](../../.github/workflows/frontend-ci.yml), [backend workflow](../../.github/workflows/backend-ci.yml), [Gitleaks exceptions](../../.gitleaksignore) |
| E11 | Design, screenshots, accessibility and Lighthouse | [Adaptive UI PDF with 15 screenshots](../design/focus-assistant-adaptive-ui.pdf), [design system](../design/design-system.md), [responsive audit](../design/responsive-audit.md), [accessibility evidence](../frontend/accessibility.md), [PDF exporter](../../Focus.Web/scripts/export-design.mjs), [Lighthouse report](final-verification.md#adaptive-ui-and-lighthouse), [machine summary](evidence/verification-results.json) |
| E12 | Containerization and deployment inputs | [Dockerfile](../../Dockerfile), [development Compose](../../compose.yaml), [production Compose](../../compose.production.yaml), [Caddy](../../deployment/Caddyfile), [publish workflow](../../.github/workflows/publish-backend.yml), [deploy workflow](../../.github/workflows/deploy-backend.yml), [deployment runbook](../deployment/backend.md) |
| E13 | Redux, RTK Query, Thunk and Saga | [Store and auth](../../Focus.Web/src/app/store.ts), [API cache](../../Focus.Web/src/api/state.ts), [export thunk](../../Focus.Web/src/features/settings/accountExport.ts), [signals saga](../../Focus.Web/src/features/sessions/signalsSaga.ts), [Redux tests](../../Focus.Web/src/tests/redux.test.ts), [state ownership](../frontend/state-management.md) |
| E14 | Architecture, diagrams and research definitions | [Backend diagram and sequences](../architecture/backend.md), [frontend diagram](../frontend/architecture.md), [navigation and sequences](../frontend/navigation-and-user-flows.md), [baseline ERD](../database/schema.md), [dictionary](../database/data-dictionary.md), [v1 addendum](../database/backend-v1.md), [research metrics](../architecture/research-metrics.md), [topology ADR](../architecture/adr-001-backend-topology.md) |
| E15 | Windows Agent and manual demonstration | [Setup](../agent/windows-setup.md), [entry point](../../Focus.Agent/Program.cs), [sync](../../Focus.Agent/Sync/SessionAgent.cs), [encrypted queue](../../Focus.Agent/Sync/ActivityQueue.cs), [sync tests](../../Focus.Backend.Tests/AgentSyncTests.cs), [privacy tests](../../Focus.Backend.Tests/AgentPrivacyTests.cs) |
| E16 | Open deployment and manual evidence | [Exact remaining actions](critical-blockers.md), [deployment scope](final-verification.md#deployment-and-runtime-scope) |
