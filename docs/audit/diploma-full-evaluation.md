# Focus Assistant independent diploma evaluation

As of 2026-11-23, Focus Assistant has a working full-stack local research product, with disposable PostgreSQL integration, real test workflows, modular React/Redux state and a valid three-device visual artifact. Submission material can be reviewed on its recorded SHA. Full compliance across all eight selected tracks is not established. Public production and an actually managed service are missing, the newest published backend check is red, and scope for the ML criterion remains unfulfilled.

## How to review this audit

- Start with [grade estimates](grade-estimates.md) and [critical deficiencies](critical-deficiencies.md), then follow the [requirement-level trace](requirements-traceability.md).
- [Execution evidence](test-evidence.md) contains the commands, counts, raw artifact locations and GitHub runs. [Operation coverage](api-endpoint-coverage.md) enumerates all 42 local routes and the 41 published application operations.
- [Remediation roadmap](remediation-roadmap.md) separates pre-deadline work, the remaining research work, and legitimate advanced directions. The written thesis, signed rubric version, academic grading formula and policy on replacing an assessed track were unavailable. Detailed criteria and penalty rules were taken from the assignment.

## Snapshot boundaries

| Snapshot | Evidence and consequences |
|---|---|
| Submitted/pinned `8cbe9bc66a3b0d08ed755ceb38647518f60ab692` | Backend verify, secret scan and Frontend verify succeeded remotely, including Docker build and isolated Compose. 41 application operations. React + Redux and six-page Adaptive UI PDF. No Unity Avatar or Focus.Vision source in that snapshot. |
| Actual GitHub `refs/heads/main` `195d8c867621d9408f6cbbdd1396a92e5b9eecc2` | Adds Orbit Windows Avatar and named pipe integration. Adds no CV. Backend verify fails formatting. Secret scan passes. No frontend run on that exact snapshot. |
| Local `main` `0b78a10d0807c05cb635bdd2e3ba753b55f85aaf` | One commit ahead. Includes Focus.Vision and camera consent/aggregation, migration and browser settings. Local standard checks and actual Windows Player build exercised. No remote CI for this commit. |
| Additional local working tree | Pre-existing workflows and deployment docs/prototypes must not be cited as published proof. Audit reports are new untracked docs. |

Pre-existing modified paths: `.github/workflows/deploy-backend.yml`, `.github/workflows/frontend-ci.yml`, `.github/workflows/publish-backend.yml`, `compose.production.yaml`, `docs/deployment/backend.md`, `docs/frontend/api-integration.md`, `docs/submission/evidence-index.md`. Pre-existing untracked: `compose.production.single-host.yaml`, `deployment/Caddyfile.single-host`, `deployment/bootstrap-postgres.sql`, `deployment/production.env.example`, `docs/deployment/hosting.md`, `docs/deployment/verification.md`, `docs/frontend/compliance.md`, `docs/frontend/final-qa-report.md`, `docs/frontend/performance.md`, `scripts/smoke-public.py`. Audit did not edit these files. No Git ref, commit, remote or production resource was written.

## Eight-track evaluation

| Criterion | Independently established | Primary grade risk |
|---|---|---|
| Backend | ASP.NET Core/.NET 10, typed JSON, layered use cases and EF Core PostgreSQL persistence, structured logs, Problem Details, authorization, real integration coverage and 87.44% line coverage. | No public production deployment. No current definitive container/Compose result for local candidate. |
| Frontend | React 19 SPA, Redux slices, RTK Query, cancellable Saga and export Thunk. 28 unit / 10 browser passes. Typed OpenAPI and real authenticated browser tests. | Breadth of API examples, authenticated performance and full manual accessibility. |
| Adaptive UI | Six published PDF pages with 15 reference captures. Core widths, portrait and landscape, native zoom, sampled touch target and contrast tests. | Full WCAG AA cannot be inferred from sample. Real devices, soft keyboards and display scaling coverage incomplete. |
| API Documentation | Versioned and validated OpenAPI and frontend types. Getting Started, tutorial, authentication, errors, changelog, diagrams. | Sparse per-operation examples and schema semantics. Thesis documentation strategy unavailable. |
| Database OLTP | Appropriate relational schema, versioned migrations, ownership and idempotency constraints, transactions and tested fresh/upgrade/restore workflows. | Production runtime/migration grants and normalization rationale unproven. No reason to score OLAP/NoSQL as selected tracks. |
| CI/CD | Actual push/PR jobs, coverage thresholds, Gitleaks, locked dependencies, stored test artifacts and historical Docker matrix execution. | Current published format step fails. No executed publishing or release deployment. |
| Cloud | Deployment architecture/config/runbook, TLS/private DB intention, and local recovery steps. | No approved production environment, public health/functional proof, or actual managed service. |
| Machine Learning | Genuine local third-party YuNet face detection on synthetic frames; approximate geometry/calibration and constrained optional heuristic integration. | No genuine project training, valid dataset/split/preprocessing, project evaluation or independent trained-model artifact. |

## Architecture and security

The API composition root in `Focus.Api/Program.cs` configures transport, centralized exception handling, OpenAPI, health and observability. Controllers own HTTP decisions; `Focus.Application` owns use cases behind store interfaces, `Focus.Domain` owns rules, and `Focus.Infrastructure` owns EF/Npgsql queries and transactions. `Focus.Engine` estimates focus state with a versioned deterministic classifier. Separate observed telemetry, estimate, self-report and outcomes in research reporting. Retain this modular topology until a deployment boundary is justified.

Password hashes are produced by ASP.NET Identity. Bearer sessions use ASP.NET Data Protection, expiry and DB-checked rotation/revocation; the browser keeps credentials in a transport closure and resets RTK Query on session changes. Desktop queue uses DPAPI and scope by origin/account. CORS is configured by explicit allowed origins; the browser uses Bearer and omits implicit credentials. Reviewed React surfaces render data as text, with no confirmed vulnerable raw HTML injection. Npgsql EF queries are typed and reviewed for bound parameters. API malformed/unmapped requests, owner scoping, account deletion/export, overlap/duplicates and consent were exercised with synthetic data. No confirmed secret leak. Neither local dependency audit nor static inspection is a complete penetration test. Production Data Protection key protection/access, actual secret rotation, API access log retention, private database, rate limiter across replicas and production HTTPS are not established.

The API intentionally transmits process identities only in its coarse activity aggregate contract. The collector uses a foreground window handle/process ID internally, not titles or raw keys. The legacy JSONL mode is explicit/debug-oriented; do not submit local recordings. The Git ignore and history checks covered JSONL, calibration, model and artifact paths. JSON output and telemetry logs contain identifiers and aggregate counts that still require access controls and retention. The Agent has bounded queue with retry from persisted DPAPI entries, consent timestamps and 24-hour maximum upload age. Retention of stored aggregates beyond individual deletion and actual production backups requires a documented owner policy.

## Real integration and hardware

| Capability | Status and verified boundary |
|---|---|
| Windows activity collection, time windows, session and consent | PARTIAL. Windows input hooks and coarse collection exist. Synthetic Agent test used real upload/service, user ownership, consent and 20-second final partial flush. Actual hardware input was not collected during audit. |
| Offline retry and delayed uploads | PASS synthetic for retry/deduplication, stable IDs and final-window updates. Capacity, DPAPI and expiry are source verified. Device/user edge scenarios remain. |
| PostgreSQL and Focus Engine | PASS for local synthetic real persistence and estimate association, versioning, owner scope, replay and migration. Heuristic states are behavioral estimates, not labels for true concentration. |
| React stats, session controls and Redux sync | PASS for tested synthetic end-to-end real transport, totals, ratings and dashboard invalidation; cache separation and abortable tasks covered. |
| Windows Unity player and 2D animation | PASS for built Windows executable and nine actual rendered synthetic states. Historical screenshots and today’s local artifacts are independent evidence. |
| Window behavior and desktop quick controls | PARTIAL. API-owned session and named IPC controls verified in synthetic tests. Separate window test failed on outer corner hit. Native frame and unavailable recovery gesture appeared in screenshot; passive click-through and two later scales not reached. No unconditional overlay PASS. |
| Local IPC, ownership and reconnect | PASS in synthetic real API/pipe integration. Named pipe restricted to current user, bounded request/response, authorized commands, no frames or raw keystrokes. |
| Camera opt-in, consent and cancellation | PASS in synthetic integration. Local Flag / server consent separate from keyboard/mouse; tests cover refusal, late-window guard, invalid fields, stop/reconnect. Required real interaction before activation. |
| Face detection and head pose | PASS for synthetic YuNet inference and synthetic geometry; NOT VERIFIED with actual camera in this audit. Detection absence does not establish the person left. Head orientation does not measure attention. |
| CV sync and UI | PASS in synthetic local integration and browser for consented coarse totals, ownership and retry/dedupe. Unavailable in [R] and [S]. |
| CV-assisted Focus Engine and camera-less fallback | PASS for deterministic rule tests and optional CV branch; telemetry-only remains baseline. Printed inference benchmark is synthetic, not accuracy evidence. |
| Shutdown, pause/restart and real-device behavior | PARTIAL. Graceful API shutdown, durable credentials/data after local Production-mode restart, final aggregate retry, named-pipe reconnect and Python stop paths tested. Physical camera unplug and external production restart remain unverified. |

Computer Vision uses OpenCV 4.12.0.88, NumPy 2.2.6, pinned YuNet ONNX from OpenCV Zoo, documented MIT origin and SHA. `Focus.Vision/vision.py` performs local inference. Raw webcam frames/landmarks are held only inside memory of its process according to source and synthetic adapter tests. It emits defined coarse sample values to the Agent. The Agent aggregates and syncs within authorized sessions, the API validates duration/quality and serves totals, and Unity only consumes small processed state/prefs over IPC. Physical camera consent, driver behavior and runtime performance beyond controlled synthetic benchmark require owner-supervised testing.

## Submission decision

- A technically defensible submission baseline exists for Frontend, Adaptive UI, substantial API and PostgreSQL work. Confirm official procedural requirements in the submitted coursework.
- The current package cannot honestly certify all selected minima. Backend production, Cloud, actual CD and project-specific ML are blocking. There is a significant penalty risk for API examples and a proof gap for database permissions.
- Frontend has the strongest evidence for moving toward grade 9–10. An advanced-feature count should not override the documented incomplete minima in any track.
- Highest leverage is one verified external HTTPS release backed by a real managed PostgreSQL service. Unblock the exact candidate’s CI before publishing and require the owner to approve access and cost. In parallel obtain an explicit academic decision for ML; a pretrained inference demo does not fill the training and evaluation requirements.
