# Focus Assistant

Focus Assistant is a research application for reviewing focus sessions. Create a
session with goals, track its progress, add ratings and a reflection, then compare
the session with its recorded activity and estimates. The web workflow runs on
its own. Windows users can opt in to activity aggregates and use a Unity
companion. The optional local vision worker derives coarse camera observations.

Keyboard and mouse counts, window switches and idle time are aggregated. Typed
text and window titles are excluded. The camera requires separate opt-in, and
raw frames stay on the device. Focus Engine estimates states using explainable rules.
Treat these estimates, observed signals and self-reported ratings as separate measures.

## Diploma Stage 3 — Evaluation Guide

| Criterion | Implementation | Documentation | Tests / Evidence |
| --- | --- | --- | --- |
| Front-end | [React routes](Focus.Web/src/app/App.tsx), [feature pages](Focus.Web/src/features/), [shared UI](Focus.Web/src/ui/), [typed API](Focus.Web/src/api/) | [Architecture diagram](docs/frontend/architecture.md), [user flows](docs/frontend/navigation-and-user-flows.md), [technology decisions](docs/frontend/technology-decisions.md) | [Component and Playwright evidence](#front-end-evidence), [test commands](docs/frontend/testing.md) |
| API Documentation | [OpenAPI 3.1.1 JSON](docs/api/openapi.json), [development Swagger UI](Focus.Api/Hosting/DevelopmentOpenApiUi.cs) | [API guide](docs/api/README.md), [Getting Started](docs/api/getting-started.md), [integration tutorial](docs/api/integration-tutorial.md) | [All 42 operations](docs/api/operation-coverage.md), [runtime contract tests](Focus.Api.IntegrationTests/ContractTests.cs) |
| Adaptive UI | [Responsive styles](Focus.Web/src/styles/), [reusable components](Focus.Web/src/ui/primitives.tsx) | [Six-page design PDF](docs/design/focus-assistant-adaptive-ui.pdf), [design system](docs/design/design-system.md) | [Phone, tablet and desktop screenshots](#adaptive-ui-evidence), [responsive audit](docs/design/responsive-audit.md), [accessibility](docs/frontend/accessibility.md) |
| Back-end | [ASP.NET Core API](Focus.Api/), [application](Focus.Application/), [domain](Focus.Domain/), [EF Core persistence](Focus.Infrastructure/) | [Layered architecture](docs/architecture/backend.md), [PostgreSQL schema and constraints](docs/database/oltp-verification.md), [API guide](docs/api/README.md) | [Coverage evidence](#back-end-evidence), [integration tests](Focus.Api.IntegrationTests/). **Public production deployment remains outstanding.** |

## Architecture and technology

- **Focus.Web** uses React 19 for reusable feature components and lazy routes,
  TypeScript for strict client and generated API contracts, and Vite for local
  development and reproducible bundles. Redux Toolkit holds client state and
  RTK Query manages server state, caching and invalidation. The
  [export thunk](Focus.Web/src/features/settings/accountExport.ts) manages the
  download lifecycle. The [signals saga](Focus.Web/src/features/sessions/signalsSaga.ts)
  manages cancellable refreshes for live and recently completed sessions.
- **Focus.Api** uses ASP.NET Core 10 for HTTP endpoints, validation, middleware and
  authentication. **Focus.Application** coordinates use cases through interfaces.
  **Focus.Domain** models entities and business rules. **Focus.Infrastructure**
  implements EF Core mappings, repositories and migrations. PostgreSQL 17 provides
  transactional storage, constraints and indexes. **Focus.Engine** applies
  deterministic classification rules.
- **Focus.Agent** collects opt-in Windows aggregates and sends them to the API
  through a retryable queue. **Focus.Avatar** is a Windows x64 Unity Player that
  exchanges session controls and snapshots with the Agent over local IPC.
  **Focus.Vision** uses a local Python/OpenCV workflow and the YuNet detector
  to derive optional observations. [System and request diagrams](docs/api/contract-guide.md#system-context)

## Quick start

### Prerequisites

Install the [.NET 10 SDK pinned in global.json](global.json), Node.js 22.13+ with
npm, and PostgreSQL 17. Use a local PostgreSQL role that can create a disposable
review database. The API and web client use cross-platform tooling. The unified
launcher, Agent, Avatar and their hardware walkthroughs target Windows. Python
and the vision model are required only for optional camera processing. Unity
Editor is required only to build the optional Windows Player.

### Windows launcher

From the repository root in PowerShell, with PostgreSQL running:

```powershell
Get-Service postgresql-x64-17
& .\scripts\start-local-demo.ps1 -DatabaseName focus_demo_stage3
```

Enter your local PostgreSQL credentials when prompted. The launcher creates or
migrates the named `focus_demo_` database, builds the .NET solution, starts the API
and Vite on loopback, and prints their readiness and logs. Reuse the same name to
keep rehearsal data. Press Ctrl+C to stop the processes started by the launcher.

| Open | Local URL |
| --- | --- |
| React application | [127.0.0.1:5173](http://127.0.0.1:5173) |
| API readiness | [127.0.0.1:8080/health/ready](http://127.0.0.1:8080/health/ready) |
| Swagger UI in Development | [127.0.0.1:8080/api/docs](http://127.0.0.1:8080/api/docs) |
| OpenAPI JSON in Development | [127.0.0.1:8080/openapi/v1.json](http://127.0.0.1:8080/openapi/v1.json) |

### Database setup

For a manual start, create a dedicated local database and configure
`ConnectionStrings__PostgreSQL` in the API process environment or
`ConnectionStrings:PostgreSQL` in [ASP.NET Core User Secrets](docs/api/getting-started.md).
Use local credentials and keep them out of Git. The
[manual review checklist](docs/frontend/manual-review-checklist.md) provides a
Windows example with credential prompts and a disposable database.

From the root, in the API terminal:

```powershell
dotnet restore 'FOCUS ASSISTANT.slnx' --locked-mode
dotnet build 'FOCUS ASSISTANT.slnx' -c Release --no-restore
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project Focus.Api -c Release --no-build --no-launch-profile -- --migrate
dotnet run --project Focus.Api -c Release --no-build --no-launch-profile -- --urls http://127.0.0.1:8080
```

From the root, in the web terminal:

```powershell
cd Focus.Web
npm ci
$env:FOCUS_API_ORIGIN = 'http://127.0.0.1:8080'
npm run dev -- --port 5173 --strictPort
```

Register a disposable account and follow the [review walkthrough](docs/frontend/manual-review-checklist.md#walkthrough).
For Swagger **Authorize**, use the opaque ASP.NET Core access token returned by
login. See the [Try it out walkthrough](docs/api/getting-started.md#try-the-api-in-swagger-ui).

## Front-end evidence

- [Architecture and navigation](docs/frontend/architecture.md) explain the layout,
  routing and state boundaries. [User flows](docs/frontend/navigation-and-user-flows.md)
  cover authentication and session lifecycle.
- [Three component unit tests and representative end-to-end flows](docs/frontend/testing.md#stage-3-test-map)
  link the exact source and assertions. The core browser cases demonstrate
  signup/sign-out and completing then reopening a persisted session.
- [Validated forms](Focus.Web/src/features/auth/forms.ts), [global error boundary](Focus.Web/src/app/App.tsx),
  [HTTP errors](Focus.Web/src/api/errors.ts) and [shared loading and recovery UI](Focus.Web/src/ui/feedback.tsx)
  show the implementation. Run `npm test`, `npm run lint`, `npm run typecheck` and
  `npm run build` from `Focus.Web` after `npm ci`. Follow the
  [Playwright setup](docs/frontend/testing.md#real-api-browser-tests)
  for isolated API-backed browser tests.

## API Documentation evidence

[Stored OpenAPI 3.1.1](docs/api/openapi.json) contains 42 operations, 16 applicable
request examples, 37 successful JSON response examples, and 194 Problem Details
examples. Five `204` responses are bodyless. See the [coverage table](docs/api/operation-coverage.md),
[Getting Started](docs/api/getting-started.md), [step-by-step tutorial](docs/api/integration-tutorial.md),
and [schema and runtime parity tests](Focus.Api.IntegrationTests/ContractTests.cs).
Swagger UI is available on the local Development API.

## Adaptive UI evidence

The [design PDF](docs/design/focus-assistant-adaptive-ui.pdf) includes a design
system and 15 captures across five screens and three device classes. Open the
standalone Overview screenshots at full size on
[phone](docs/design/screenshots/stage3-overview-mobile.png),
[tablet](docs/design/screenshots/stage3-overview-tablet.png) and
[desktop](docs/design/screenshots/stage3-overview-desktop.png).
The [responsive audit](docs/design/responsive-audit.md) records portrait and landscape,
125–200% browser zoom, sampled touch targets of 44 CSS px, navigation and overflow.
The [design system](docs/design/design-system.md) identifies reusable controls, color
and typography. Sampled normal text pairs exceed 4.5:1. [Accessibility evidence](docs/frontend/accessibility.md)
covers browser automation and its limits.

## Back-end evidence

- [Backend architecture](docs/architecture/backend.md) shows controllers and use-case
  services with separate responsibilities and dependency inversion through persistence
  interfaces. [Session service](Focus.Application/Sessions/SessionService.cs),
  [EF mappings](Focus.Infrastructure/Persistence/Configurations/) and
  [versioned migrations](Focus.Infrastructure/Persistence/Migrations/) show this in code.
- [Global Problem Details error handler](Focus.Api/Hosting/ApiErrorHandler.cs),
  [structured JSON request logging](Focus.Api/Program.cs), and
  [authentication and resource ownership](docs/api/authentication.md)
  show the API middleware and security boundaries.
- [Business-logic tests](Focus.Backend.Tests/) measured **87.44% line coverage**
  (724/828 lines) across Domain, Application and Engine using Coverlet. [Measured results and commands](docs/audit/task20-verification.md#commands-and-results)
  and [reproduction and security scanning](docs/testing/backend.md) describe the scope
  and quality gate. [GitHub Actions](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions)
  includes build, test, coverage and secret-scanning jobs.

## Limitations

Public production deployment is an outstanding Back-end requirement. The API and
Swagger links above are local development addresses. Use the
[local demo report](docs/release/2026-11-24-local-readiness.md) for the verified
workflow, optional [Avatar setup](Focus.Avatar/README.md), and
[manual camera test](docs/vision/manual-verification.md). The camera evidence
covers synthetic input. Browser credentials are held in memory and sign-in
is required after reloading the SPA.
