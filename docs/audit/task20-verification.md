# TASK 20 diploma verification

Verified locally on November 23, 2026 against a candidate based on `6a2b06f6a9dbc1b77cb5b9e926c9a75a2f28318f`. The grading references are the [eight-track audit](diploma-full-evaluation.md) and the assignment requirements transcribed into the [traceability matrix](requirements-traceability.md). The original university rubric and final written thesis were unavailable for independent review. The ranges below are technical assessments, with no implication of an official grade.

| Criterion | Mandatory evidence | Advanced evidence | Remaining gap | Estimated range |
| --- | --- | --- | --- | --- |
| API Documentation | PASS for contract inventory, applicable request and success examples, named schemas, parameters, error examples, security, versioning and onboarding. PARTIAL for thesis integration. | PASS for automated OpenAPI/schema/DTO checks, live example capture, diagrams and documented token lifecycle, pagination and idempotency. | Repeat independent onboarding and reference the verified materials in the final thesis. Some Problem Details examples are representative of a shared envelope. | 8–9 |
| PostgreSQL OLTP | PASS for migration-generated DDL, fresh install, upgrade, keys and constraints, test fixtures, ownership checks, privileges and rollback on an isolated cluster. | PASS for measured owner-scoped queries, index rationale, sanitized synthetic demo export, and backup/restore rehearsal. | Provider-specific production role and recovery evidence, workload representative of concurrent use, and field-level camera invariants currently enforced in application code. | 8–9 |
| Adaptive UI | PASS for layouts across three device classes, browser zoom and portrait/landscape samples, touch-size checks and the six-page evidence deliverable. PARTIAL for WCAG 2.1 AA coverage. | PASS for tablet/mobile reflow, light/dark themes and reusable components. PARTIAL for performance evidence and platform-specific behavior. | Independent screen-reader pass, physical touch and on-screen keyboard, OS scaling and cold authenticated performance on representative hardware. | 7–9 |

## API contract

The [per-operation table](../api/operation-coverage.md) counts all 42 business operations in the generated specification. From the baseline, applicable request examples increased from 5/16 to 16/16 and successful JSON responses from 4/37 to 37/37. Five 204 responses are bodyless. The gate checks 194 Problem Details examples, 51 parameter occurrences, 55 named models and 213 field descriptions. Every successful body and applicable JSON request was captured from a real fixture API with a synthetic account. The shared fallback examples illustrate allowed status/mime/envelope combinations. Only a subset of error branches was exercised in the capture run. Tokens are replaced by nonfunctional placeholders. The contract test compares runtime output with the checked-in document, and the frontend check confirms matching TypeScript types.

Use the [contract guide](../api/contract-guide.md) for system context, request flows, versioned errors, ownership, token lifecycle, offset pagination, late activity, opt-in camera aggregates and reproduction instructions. For goal updates, nullable fields are omittable in the generated request schema, matching the existing partial update behavior.

## Database

The [OLTP verification](../database/oltp-verification.md) links the physical dictionary, migration SQL, logical ERD, functional dependencies, explicit denormalization and query probes. `oltp-schema.sql` applied twice on a fresh PostgreSQL 17 fixture. Integration checks verified legacy upgrade, integrity, rollback, idempotency and cross-account boundaries. A runtime role could insert fixture data and read the migration version, and was denied schema DDL and migration history writes. Reapplying grants after a fixture schema addition was verified. A synthetic dump restored 15,000 sessions, 105,000 windows and eight history records. All migration and role checks targeted a local fixture on port 55454.

On the deterministic fixture, the measured history, goals, desktop activity, camera activity and analytics query times were respectively 0.202, 0.067, 0.171, 0.067 and 1.504 ms. An experimental expression index improved the history query to 0.056 ms. The probe rolled back that index. Its write/storage trade-off was not warranted by a single small observed gain. No migration or application query changed.

## Adaptive UI

[Responsive verification](../design/responsive-audit.md) records 13 browser viewport sizes, both phone orientations, native zoom up to 200%, keyboard focus and modal behavior, sampled axe checks and contrast tokens, with two new Privacy screenshots. The existing [six-page design PDF](../design/focus-assistant-adaptive-ui.pdf) was rendered and inspected. [Lighthouse](../frontend/task20-performance.md) records actual timings and snapshots for populated protected routes, and mobile and desktop cold login performance. The SPA, Redux Toolkit, RTK Query, Thunk and Saga flows stayed in use during the browser tests.

## Commands and results

| Verification | Command or reproduction | Local result |
| --- | --- | --- |
| Release | `dotnet build "FOCUS ASSISTANT.slnx" -c Release --no-restore -warnaserror` using separate `--artifacts-path` | PASS, 0 warnings and 0 errors |
| .NET format | `dotnet format "FOCUS ASSISTANT.slnx" --no-restore --verify-no-changes` | PASS |
| .NET unit and coverage | `dotnet test Focus.Backend.Tests -c Release --no-build --collect "XPlat Code Coverage" --settings docs/testing/business-logic.runsettings` and `python scripts/check-coverage.py <results> --minimum 80` | PASS, 58 tests. Business logic coverage 87.44% of lines (724/828) and 78.17% of branches |
| Integration and OpenAPI parity | `dotnet test Focus.Api.IntegrationTests -c Release --no-build` with `FOCUS_REQUIRE_POSTGRES=1` and the local fixture | PASS, 12 tests and one optional Unity capture skip |
| OpenAPI | `python -m openapi_spec_validator docs/api/openapi.json`, `python scripts/check-api-documentation.py`, `npm run api:check` from `Focus.Web` | PASS |
| Frontend static and unit checks | `npm run typecheck`, `npm run lint`, `npm run format:check`, `npm test`, `npm run build` from `Focus.Web` | PASS, 28 Vitest cases. Vite emits two existing warnings about Zod purity comments |
| Browser | `npm run test:e2e`, `npm run test:e2e:qa`, `npm run test:e2e:agent`, `npm run test:e2e:zoom` from `Focus.Web` | PASS, 5 + 3 + 1 + 1 with the fixture API |
| PostgreSQL | [SQL reproduction](../database/oltp-verification.md#reproduce-without-personal-data), plus role smoke and dump/restore | PASS |
| Vision synthetic regressions | `python -B -m unittest -v test_vision` from `Focus.Vision` with installed fixture dependencies | PASS, 7 tests. Windows physical camera not activated |
| Workflow syntax and dependency advisories | `actionlint -shellcheck=`, `npm audit --audit-level=moderate`, `dotnet list package --vulnerable --include-transitive` | PASS, no npm or NuGet advisories reported |
| Git secret scanning | Gitleaks 8.24.3 with `--redact=100` on staged diff and Git history | PASS, no leaks reported. Screenshots from the fixture account were reviewed separately |

This checkpoint supplies no new public deployment or Machine Learning qualification evidence. The CI workflow checks the OpenAPI document, its examples and generated API types. Separate GitHub Actions results belong to the exact pushed commit and are available from [Actions](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions).
