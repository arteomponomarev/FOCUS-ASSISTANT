# Diploma traceability matrix

Audit baseline: November 23, 2026, commit `1e7ddd9b6bfa9501bcdce053baef37c8cb4ac4ad`. Target submission: November 25, 2026. The assessment uses the requirements supplied in Tasks 01, 02, 05 and 10 and the repository plans linked in the [evidence index](evidence-index.md). No original university rubric or weighting sheet was available. The distinction between mandatory and advanced follows those supplied task descriptions. Database and Cloud are assessed separately as project tracks. Their official assessment requirements remain unconfirmed.

**PASS** means the scoped requirement has inspected implementation and suitable evidence. **PARTIAL** means only part of the requirement or its verification is complete. **FAIL** means a mandatory artifact or capability is demonstrably missing. **NOT VERIFIED** means the available evidence cannot establish the result. Evidence IDs resolve to concrete paths or GitHub runs in the [evidence index](evidence-index.md).

## Backend Development

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| BE-01 | Modern backend and successful Release build | Mandatory · T01/T02 | `Focus.Api/Focus.Api.csproj`, `Program.cs`, .NET 10 | Locked restore and Release with warnings as errors | E01, E10 | PASS |
| BE-02 | Real PostgreSQL persistence and EF Core | Mandatory · T01/T02 | `Focus.Infrastructure/Persistence/FocusDbContext.cs`, migrations and stores | Real PostgreSQL suite, fresh catalog and reopen after restart | E02, E06, E07 | PASS |
| BE-03 | Layered architecture and SOLID boundaries | Mandatory · T01/T02 | Domain, Application ports, Infrastructure stores, Engine, API controllers | Project references, thin HTTP endpoints, application tests and layered diagram | E01, E02, E08, E14 | PASS |
| BE-04 | Real session and research API with persisted data | Mandatory · T01/T02 | Sessions, goals, telemetry, rules, analytics and settings in API/Application | PostgreSQL session workflow, research workflow, tutorial, production-mode smoke | E02, E06, E07 | PASS |
| BE-05 | Input validation and HTTP semantics | Mandatory · T01/T02 | `ApiHosting.cs`, DTO validation, application guards, owned resources | Invalid payload, enums, transition conflicts, status, CORS and rate-limit assertions | E02, E08 | PASS |
| BE-06 | Global exception handling and structured logs | Mandatory · T01/T02 | `ApiErrorHandler.cs`, Problem Details, JSON console, trace IDs | Safe errors, correlation headers and production-mode smoke | E02, E07, E08 | PASS |
| BE-07 | Auth, resource ownership and secrets management | Mandatory · T02 | Identity password hasher, opaque bearer credentials, login families and secrets from config | Rotation, revocation, cross-user 404, local TLS, Gitleaks | E02, E07, E08, E10 | PASS |
| BE-08 | Source-controlled, accessible API description | Mandatory · T01/T02 | `docs/api/openapi.json`, runtime generator in Contract/Development | OpenAPI validator, live contract equality and TypeScript contract check | E02, E09 | PASS |
| BE-09 | At least 70% unit-test coverage of business logic | Mandatory · T01/T02 | Coverlet assembly filters for Domain, Application and Engine | 91.28% line and 83.97% branch, gate at 80% | E03 | PASS |
| BE-10 | Reproducible setup and deployment procedure | Mandatory · T01/T02 | Dockerfile, Compose, guarded migration step, local and production runbook | Config validation, native Release smoke and GitHub container smoke | E07, E10, E12 | PASS |
| BE-11 | Publicly reachable production deployment | Mandatory · T02 | Manual production workflow and runbook | Runbook confirms no configured production target. No independently verified URL or HTTPS endpoint | E12, E16 | FAIL |
| BE-A1 | Containerization, CI and quality gates | Advanced · T01/T02 | Backend workflow and gated build, tests, coverage, NuGet audit, Gitleaks, container smoke | Both backend jobs succeeded on audited SHA | E10, E12 | PASS |
| BE-A2 | Monitoring instrumentation and tracing | Advanced · T01/T02 | `ApiObservability.cs`, health endpoints, OTLP and correlation | Local collector received a trace and persisted-window metric | E07, E08, E12 | PASS |
| BE-A3 | Central log ingestion and alerting | Advanced · T01/T02 | JSON logs, exporter settings and operational runbook | Local export works. No running shared log sink, dashboard, retention enforcement or tested alerts | E07, E12 | PARTIAL |
| BE-A4 | Caching and asynchronous processing where justified | Optional · T02 | Bounded synchronous atomic ingestion, query-based analytics, topology ADR | ADR and implementation reviewed. No cache, broker or background delivery worker | E08, E12, E14 | PARTIAL |
| BE-A5 | Justified microservices decision | Advanced design · T02 | `docs/architecture/adr-001-backend-topology.md` | Reviewed cohesive ownership and operational trade-offs. No microservice deployment claimed | E14 | PASS |
| BE-A6 | Dependency auditing and source history scanning | Advanced · T01/T02 | NuGet audit during restore, workflow Gitleaks configuration | GitHub verify and secret-scan plus local redacted scan of 11 commits | E10 | PASS |

## Frontend Development

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| FE-01 | React SPA, strict TypeScript, bundler | Mandatory · T01/T05 | React 19, Vite, strict `tsconfig`, `Focus.Web/src/main.tsx` | TypeScript and production build | E04 | PASS |
| FE-02 | At least four functional routes and protected navigation | Mandatory · T01/T05 | `App.tsx`, `layout.tsx`; login, registration, overview, history, new, detail, analytics and settings | Browser navigation and real auth workflows | E05 | PASS |
| FE-03 | Reusable UI, modules, pages, services and styles | Mandatory · T01/T05 | `Focus.Web/src/{app,features,api,ui,styles}` | Architecture and production chunks reviewed, UI exercised in browser | E04, E05, E14 | PASS |
| FE-04 | Local, global and server state with Redux Toolkit | Mandatory for this migration · T10 | `app/store.ts`, `api/state.ts`, feature endpoints, local forms and URL history filters | Store isolation, browser invalidation and dependency inspection. TanStack absent from code and lockfile | E04, E05, E13 | PASS |
| FE-05 | Central typed client and real backend integration | Mandatory · T01/T05 | `api/client.ts`, `transport.ts`, generated `schema.ts` and shared RTK Query API | Live contract comparison and browser tests with Release API and PostgreSQL | E02, E04, E05, E09 | PASS |
| FE-06 | Forms, sessions, goals, validation and settings | Mandatory · T05 | Auth forms, session creation/lifecycle/completion and settings feature modules | Unit form tests, persisted workflow, settings export and QA | E04, E05 | PASS |
| FE-07 | Lists, filtering, sorting, pagination and responsive presentation | Mandatory · T01/T05 | `History.tsx`, `SessionList.tsx` and backend query parameters | Browser cache freshness and multi-page history, viewport tests | E05 | PASS |
| FE-08 | Loading, empty and recoverable error states | Mandatory · T01/T05 | `ui/feedback.tsx`, route and feature rendering | Empty report, offline error/retry and delayed API data in browser | E05 | PASS |
| FE-09 | Global error boundary and HTTP error mapping | Mandatory · T01/T05 | `App.tsx`, `errors.ts`, `transport.ts` | Expired/revoked auth, offline, timeout, plain API failures and redaction | E04, E05 | PASS |
| FE-10 | ESLint, Prettier and checked lockfile | Mandatory · T01/T05 | `package-lock.json`, ESLint/Prettier config and frontend workflow | GitHub `npm ci` and local lint/format checks | E04, E10 | PASS |
| FE-11 | At least three unit-tested components/services and two integrations | Mandatory · T01/T05 | `src/tests` and `e2e` | 28 unit and 9 browser tests | E04, E05 | PASS |
| FE-12 | Reproducible startup and architecture documentation | Mandatory · T05 | `README.md`, API integration and frontend testing docs | Production preview against clean disposable schema and GitHub runner | E04, E05, E10, E14 | PASS |
| FE-A1 | Lazy routes, feature-based design, charts and light/dark theme | Advanced · T01/T05 | `App.tsx`, `ui/charts.tsx`, `app/ui.ts`, styles | Build chunks, responsive and theme tests, Lighthouse routes | E04, E05, E11 | PASS |
| FE-A2 | One-shot Thunk and event-driven Saga | Advanced project choice · T10 | `settings/accountExport.ts`, `sessions/signalsSaga.ts` | Export, cancellation/isolation, Saga deadline and late upload | E04, E05, E13 | PASS |
| FE-A3 | Lighthouse performance evaluation | Advanced · T05 | Production Vite chunks and performance reports | Local Login Navigation, and authenticated desktop/mobile Timespan and Snapshot. Public field metrics outside scope | E11 | PASS |
| FE-A4 | Native Agent recording through to live browser | Project feature · T07/T10 | `Focus.Agent`, transactional ingest, browser Signals | Real synthetic .NET and React integration passed. No keyboard/mouse hook demonstration was observed during this audit | E02, E05, E06, E15 | PARTIAL |

## Adaptive UI

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| UI-01 | Mobile, tablet, desktop, plus portrait and landscape | Mandatory · T01/T05 | `workspace.css`, `features.css`, responsive history layout | Sampled viewports from 320 to 1920 CSS px, including phone/tablet landscape | E05, E11 | PASS |
| UI-02 | Fluid grid, relative units and scalable typography/icons | Mandatory · T01/T05 | Grid/Flexbox, rem-based CSS, responsive charts/tables, Lucide | Styles and reflow inspected in browser and PDF | E05, E11 | PASS |
| UI-03 | Browser zoom and content reflow | Mandatory · T01/T05 | Flexible content and navigable layouts | Native Chromium zoom at 125%, 150%, 200% on four main routes | E05 | PASS |
| UI-04 | Navigation, focus, landmarks and keyboard access | Mandatory · T01/T05 | Skip link, named navigation, native dialogs and reduced motion | Visible focus, Enter, Tab containment, Escape/restoration, light/dark checks | E05, E11 | PASS |
| UI-05 | Minimum 44 × 44 CSS px targets | Mandatory · T01/T05 | Shared sizes, button and link CSS | Automated measurement on sampled small-screen routes. Untested control variants and physical devices remain | E05, E11 | PARTIAL |
| UI-06 | WCAG 2.1 AA contrast and accessible states | Mandatory · T01/T05 | Design tokens, accessible names, live feedback and semantic markup | Axe on sampled states and token ratios. Full WCAG, non-text contrast and assistive technology coverage incomplete | E05, E11 | PARTIAL |
| UI-07 | Reusable design system with component and interaction states | Mandatory · T01/T05 | `base.css`, `ui/primitives.tsx`, `docs/design/design-system.md` | Visual design plate includes hover, focus, unavailable, loading and error | E11 | PASS |
| UI-08 | Separate PDF or Figma with three device variants in submission | Mandatory · T01/T05 | Six-page PDF and companion files in the checkpoint under `docs/design/` | All pages opened and visually inspected. Five screens, 15 captures, and a design-system plate | E00, E11 | PASS |
| UI-A1 | Manual physical-device and screen-reader verification | Advanced evidence · T05 | Accessible semantics and documented audit plan | No physical mobile keyboard or assistive-technology run evidenced | E11 | NOT VERIFIED |
| UI-A2 | Figma source | Optional · T05 | PDF alternative with reproducible exporter | No Figma file observed. The documented PDF is the supported deliverable | E11 | FAIL |

## API Documentation

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| API-01 | Working generated OpenAPI and versioned JSON/YAML | Mandatory · T01/T02 | `OpenApiContract.cs`, `Program.cs`, `docs/api/openapi.json` | Runtime export, normative validation and equality test | E02, E09 | PASS |
| API-02 | All application operation descriptions, parameters and schemas | Mandatory · T01/T02 | 41 operation summaries, 30 paths and referenced DTOs | Operation count, unique IDs, live equality. Health routes are operational docs outside the versioned application contract | E06, E09 | PASS |
| API-03 | Auth requirements, status codes and error models | Mandatory · T01/T02 | Bearer scheme on 38 operations, written auth/error guide, Problem Details | Inline security/status inspection, API behavior tests | E02, E09 | PASS |
| API-04 | Representative request and response examples | Mandatory · T01/T02 | Embedded examples and `scripts/demo-backend.ps1` workflow | Five request examples and nine response examples, including 400/401/404/409 verified on the real API. Contract parity, example schemas and reproducible tutorial checked | E06, E09 | PASS |
| API-05 | Getting Started and step-by-step integration tutorial | Mandatory · T01/T02 | `getting-started.md`, `integration-tutorial.md`, executable PowerShell example | Actual completed workflow, one aggregate, one estimate and replay deduplication | E06, E09 | PASS |
| API-06 | Architecture and request-flow diagrams | Mandatory · T01/T02 | `docs/architecture/backend.md` and frontend flow diagrams | Reviewed Mermaid component, auth, session completion and ingestion diagrams | E14 | PASS |
| API-07 | API versioning and changelog | Mandatory · T01/T02 | `versioning.md`, `changelog.md` | Reviewed v1 prefix, compatibility policy and release log | E09 | PASS |
| API-A1 | Automated validation and synchronization | Advanced · T01/T02 | GitHub OpenAPI validator, backend contract test, frontend generated type check | Local and both GitHub checks passed | E02, E04, E09, E10 | PASS |
| API-A2 | Interactive documentation and broader example coverage | Optional/high-grade · T01/T02 | JSON, Markdown and representative embedded examples | Spec and route wiring inspected. Interactive Swagger/Scalar and exhaustive per-operation examples remain outside the checkpoint | E09 | PARTIAL |

## Database

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| DB-01 | PostgreSQL with real EF Core model and persisted research data | Core project · T01/T02/T10 | `FocusDbContext.cs`, five legacy tables plus six extensions | PostgreSQL 17.2 catalog, tutorial and persistence tests | E02, E06 | PASS |
| DB-02 | Versioned clean initialization and non-destructive upgrade | Core project · T01/T02/T10 | Seven migrations and snapshot under `Persistence/Migrations` | All seven applied to clean schema. Upgrade test preserves existing rows from old migration | E02, E06 | PASS |
| DB-03 | Referential, ownership and unique/index integrity | Core project · T01/T02/T10 | Configuration classes and SQL migrations | 12 catalogued FKs, unique login/telemetry keys, same-owner profile, one open pause and constraints test | E02, E06 | PASS |
| DB-04 | Atomic changes and stable telemetry replay | Core project · T02/T10 | Repository locks and transactions, aggregate-to-estimate reference | Research rollback/ownership, agent replay and linked catalog rows | E02, E06, E08 | PASS |
| DB-05 | Lifecycle and rating boundaries | Core project · T01/T02/T10 | Session domain rules, distinct self-report columns and PostgreSQL types | Domain and API transition/rating tests. No PostgreSQL CHECK for all domain ratings/statuses when direct writes bypass the app | E02, E03, E06 | PARTIAL |
| DB-06 | Current schema, ERD and data dictionary | Core project · T01/T02/T10 | Historical `schema.md` and `data-dictionary.md` plus v1 addendum | Catalog compared. Baseline ERD does not include all 11 tables in one diagram | E06, E14 | PARTIAL |
| DB-A1 | Restore drill, backup evidence and managed database operations | Advanced operations · T10 | Deployment runbook requires backups and separate roles | No actual production backup/restore drill, recovery timing or live privilege audit | E12, E16 | NOT VERIFIED |
| DB-A2 | Query-plan/load evidence at production scale | Optional · T02/T10 | SQL pagination, scoped analytics and indexes | No measured production plans, representative load or capacity targets | E02, E06 | NOT VERIFIED |

## Cloud

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| CL-01 | Reproducible non-root container and health checks | Core readiness · T02/T10 | Multi-stage `Dockerfile`, loopback development Compose and health probe | GitHub build/Compose smoke passed. Local Docker Desktop Linux Engine returned HTTP 500 | E10, E12 | PASS |
| CL-02 | Production config for HTTPS, trusted proxy, persisted keys and secrets | Core readiness · T02/T10 | Production Compose, Caddy, API transport, encrypted key option | Compose parsing and direct production-mode HTTPS/key persistence smoke passed. Actual proxy/DNS/managed secrets unverified | E07, E12 | PARTIAL |
| CL-03 | Private PostgreSQL and controlled migrations | Core readiness · T02/T10 | Deployment job, external DB roles and restricted connectivity design | Native disposable migrations and production Compose validated. Live DB network policy, TLS and role grants pending | E06, E07, E12 | PARTIAL |
| CL-04 | Public production endpoint, domain and live smoke | Core readiness · T02/T10 | Documented process for an approved host | No production target, verified public URL/TLS or external frontend/API workflow | E12, E16 | FAIL |
| CL-05 | Production publish/deploy and approvals | Core readiness · T02/T10 | Manual registry workflow and protected production runner workflow | Workflow syntax verified. Registry publication and production deployment not executed for release | E10, E12, E16 | PARTIAL |
| CL-06 | Backup, rollout recovery and operating evidence | Core readiness · T02/T10 | Runbook, provisioned persistent volumes and health signals | Production restore, rollback/recovery drill, logs and alert delivery unverified | E12, E16 | NOT VERIFIED |
| CL-A1 | OpenTelemetry, centralized logging, dashboards and alerting | Advanced · T02/T10 | Local OTLP export, JSON logs and alerting plan | Local collector receives signals. No production collector, dashboard or alert verification | E07, E12 | PARTIAL |
| CL-A2 | Production SPA routing and API integration hosting | Optional product deployment · T05/T10 | Documented HTTPS SPA fallback and CORS integration | Local deep links verified; production Compose packages API/proxy only and no external static host has been verified | E05, E12, E16 | PARTIAL |

## Optional research scope

| ID | Requirement | Level / source | Implementation | Test or inspection | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| OP-01 | Unity avatar client | Optional for this submission · T02/T07 | API exposes current state and preferences | Backend seam reviewed. No Unity client in tracked solution | E09, E14 | FAIL |
| OP-02 | ML, computer vision, statistical hypothesis testing | Optional for this submission · T02 | Deterministic versioned Focus Engine and research export | Heuristic and reproducible fixture tests. No ML/CV implementation or study conclusion | E02, E03, E14 | FAIL |
