# Frontend testing

Use Node.js 22.13+ and the repository's .NET 10 SDK. Install the locked dependencies and run the static and unit checks from `Focus.Web`.

```powershell
npm ci
npm run api:check
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
```

Vitest and Testing Library exercise form validation, available lifecycle controls, report transformations, error redaction, refresh concurrency, Redux cache isolation across account changes and cancellation of the signals refresh saga.

## Real API browser tests

Build the API in Release from the repository root. Prepare a disposable, loopback PostgreSQL 17 cluster with a test role that can create databases. Set `FOCUS_WEB_TEST_DATABASE` in the test process, using the database prefix `focus_test_` and its local test credentials. Reserve ports 5188 and 4175 for the run. The test launcher generates a unique database name and applies the existing migrations. It refuses a remote host or a database name outside the test convention. Dispose of the test cluster or its generated databases using the test owner's database tools.

```powershell
dotnet restore Focus.Api/Focus.Api.csproj --locked-mode
dotnet build Focus.Api/Focus.Api.csproj -c Release --no-restore
cd Focus.Web
npx playwright install chromium
npm run build
npm run test:e2e
npm run test:e2e:qa
npm run test:e2e:agent
npm run test:e2e:zoom
```

Playwright starts ASP.NET Core and the built Vite preview. It verifies registration, sign-in, logout, protected routes, session lifecycle, goals, completion, persisted ratings, settings, telemetry presentation, export and account deletion. The QA suite covers cache freshness, history pagination, offline recovery, credential rotation and revocation, and profile/password changes. A Chromium-specific test adjusts native browser zoom through the Appearance settings of an isolated browser profile. Each suite starts its own API process and disposable database under the production authentication rate limit. The workflow and QA suites can use an installed Chrome executable via `FOCUS_BROWSER_PATH`. Machine-readable reports are `test-results/results.json`, `test-results/qa-results.json` and `test-results/zoom-results.json`.

[Frontend CI](../../.github/workflows/frontend-ci.yml) runs these checks with a fresh PostgreSQL service. Its remote run has not been verified for these local changes.

## Redux migration verification

Local verification on November 23, 2026 used a Release build of the API and a disposable PostgreSQL 17 cluster. Type checking, lint, formatting, OpenAPI type comparison, Vite build and npm audit passed. The build emitted the two existing Rollup annotation warnings from Zod.

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Vitest | 28 | 0 | 0 |
| Playwright workflows and responsive checks | 4 | 0 | 0 |
| Playwright QA regressions | 3 | 0 | 0 |
| Playwright agent signals with synthetic aggregates | 1 | 0 | 0 |
| Playwright native browser zoom | 1 | 0 | 0 |
| Backend and Agent tests | 37 | 0 | 0 |
| API integration and contract tests | 10 | 0 | 0 |

The agent browser test waits for the Completed screen and a telemetry response containing one interval before submitting the final partial interval through the real API. The second interval appears without a page reload. The test uses synthetic activity and does not start Windows input collection.

## Baseline before Redux migration

Local verification on November 22, 2026 used Windows, Node 22.20, .NET SDK 10.0.401, PostgreSQL 17, Google Chrome and Playwright Chromium.

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Vitest | 25 | 0 | 0 |
| Playwright workflows and responsive checks | 4 | 0 | 0 |
| Playwright QA regressions | 3 | 0 | 0 |
| Playwright native browser zoom | 1 | 0 | 0 |
| Backend tests | 29 | 0 | 0 |
| API integration and contract tests | 9 | 0 | 0 |

`npm ci`, OpenAPI type generation check, strict TypeScript, ESLint, Prettier, Vite production build, `npm audit` and local actionlint all passed. The .NET Release build reported zero warnings and zero errors. The existing Coverlet gate measured 91.25% business-logic line coverage and 83.92% branch coverage. See the [backend verification commands](../testing/backend.md).
