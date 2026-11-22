# Backend release verification

Verification date: **2026-11-22**. Branch `main`, baseline commit `21f05fb`.
Verification included the previous backend implementation in the working tree and
the corrections described below. The index was empty at baseline. Test data,
certificates, coverage and build outputs were isolated from application data.

## Results

| Check | Status | Evidence |
|---|---|---|
| Initial independent baseline | PASS | Locked restore, Release build without warnings, and 34 passing baseline tests on PostgreSQL 17. |
| Final Release build and formatter | PASS | Complete solution built with warnings treated as errors. `dotnet format --verify-no-changes` passed. |
| Clean-source Release and dependency audit | PASS | Copied intended files into an isolated directory without build products. Locked restore, full Release build, and publish passed. NuGet reported no known vulnerable packages from the configured feeds. |
| Final automated suite | PASS | 29 unit tests and 9 API test-host tests, including 8 requiring disposable PostgreSQL, passed. Zero failures and zero skips. |
| Coverage | PASS | 91.25% lines (688/754) and 83.92% branches across all measured business-logic assemblies. The 80% gate passed and the 100% threshold failed as expected. |
| API security and isolation | PASS | Tests verified expiry, refresh rotation, logout, password-change revocation, owner-only sessions/goals/telemetry/estimates, analytics and export isolation, CORS, throttling, and trace IDs. |
| Negative inputs | PASS | Covered malformed and ambiguous JSON, unsupported content type, invalid ID/enum/rating, invalid transitions, oversized telemetry batch, prohibited fields, negative counters and invalid windows. |
| PostgreSQL clean install and upgrade | PASS | Fresh migrations applied on PostgreSQL 17. Upgrade from the original two-migration schema retained a populated row. Foreign-key, non-null, unique and pause-order constraints rejected invalid SQL. Atomic batch and concurrent retries were verified through HTTP. Historical migrations remain intact. |
| OpenAPI and documentation | PASS | Generated OpenAPI 3.1.1 still exposes 41 operations over 30 paths. Runtime artifact matches versioned JSON. Independent openapi-spec-validator 0.7.2 passed. |
| Production-mode local smoke and tutorial | PASS | Published API completed the full HTTPS workflow, rejected plain HTTP for business routes, and recovered the credential, completed session, goal, observations and estimates after restart. The PowerShell tutorial separately completed on a fresh database. |
| OpenTelemetry and graceful host stop | PASS | The published API delivered traces and the activity counter to a separate loopback OTLP receiver. The test verified that the session ID was absent from the collected trace payloads. The test host signaled application stopping and stopped. |
| Windows Agent privacy | PASS | Reproduced title-field serialization with a synthetic snapshot, removed Win32 window-text retrieval and title fields, and verified a real JSONL write without titles. Live activity was outside the test scope. |
| Workflow lint | PASS | All three workflows validated with actionlint 1.7.12 after declaring the custom `focus-production` runner label. |
| Compose configuration | PASS | Both Compose files accepted by `docker compose config -q` using synthetic configuration. |
| Docker engine | FAIL | CLI 28.0.4 and Compose v2.34.0-desktop.1 were available. The Desktop Linux named-pipe endpoint returned HTTP 500 for `version` before evaluating the Dockerfile. The host listed both Ubuntu and docker-desktop WSL 2 distributions as running. Diagnostic log categories included engine socket errors, closed transport and timeouts. The supplementary read inside the Docker distribution stalled and was canceled. |
| Container build and runtime | NOT VERIFIED | Build and stack startup were deferred pending a healthy engine. |
| Remote GitHub Actions and registry | NOT VERIFIED | Local equivalents and workflow lint were exercised. Remote jobs, image publication and the Gitleaks job remain unverified. |
| External production accessibility | NOT VERIFIED | An approved hostname, hosting and operational credentials are pending. This is an outstanding mandatory diploma criterion. |

Release coverage by assembly:

| Assembly | Line | Branch |
|---|---:|---:|
| Focus.Domain | 83.83% | 74.13% |
| Focus.Application | 96.98% | 85.19% |
| Focus.Engine | 100.00% | 100.00% |
| Total | **91.25%** | **83.92%** |

## Corrections verified

- Reject duplicate JSON properties in MVC and HTTP serialization. The regression
  first demonstrated that two conflicting duration values could create a session.
- Preserve HTTP 415 for unsupported content types and publish the actual
  `application/problem+json` response schema for JSON request operations.
- Keep dependencies locked while adding the Agent serialization regression.
  Remove the title from the collector, aggregation models, storage schema and
  unused P/Invoke declarations. Older local JSONL files require a separate
  retention decision by their owner.
- Require PostgreSQL in CI via `FOCUS_REQUIRE_POSTGRES=1` and add an isolated
  Compose health check. Preserve manual publishing and production workflows.
- Accept byte-identical Cobertura attachments copied by the TRX logger. The gate
  still fails on missing or different reports, invalid thresholds, and insufficient coverage.
- Expand the published-artifact smoke scenario to include telemetry, lifecycle,
  analytics, restart durability and local OTLP delivery.

## Reproduction commands

Set `FOCUS_TEST_DATABASE` to a disposable loopback cluster and
`FOCUS_REQUIRE_POSTGRES=1` before running the tests.

```powershell
dotnet restore "FOCUS ASSISTANT.slnx" --locked-mode
dotnet build "FOCUS ASSISTANT.slnx" -c Release --no-restore -warnaserror
dotnet test Focus.Backend.Tests -c Release --no-build --collect "XPlat Code Coverage" --settings docs/testing/business-logic.runsettings --logger trx --results-directory artifacts/verification-unit
python scripts/check-coverage.py artifacts/verification-unit --minimum 80
dotnet test Focus.Api.IntegrationTests -c Release --no-build --logger trx --results-directory artifacts/verification-integration
dotnet format "FOCUS ASSISTANT.slnx" --no-restore --verify-no-changes
dotnet list "FOCUS ASSISTANT.slnx" package --vulnerable --include-transitive --no-restore
python -m openapi_spec_validator docs/api/openapi.json
actionlint -color=false
docker compose -f compose.yaml config -q
docker compose -f compose.production.yaml config -q
dotnet publish Focus.Api -c Release --no-restore -o artifacts/backend-release
python scripts/smoke-release.py artifacts/backend-release/Focus.Api.dll
```

Generate a fresh result directory for every coverage run. Use the configuration
described in [the integration tutorial](../api/integration-tutorial.md) to run
`scripts/demo-backend.ps1` with a synthetic account and a temporary API endpoint.

The smoke used a self-signed certificate trusted only by the local test client.
After verification the disposable PostgreSQL server was stopped.

## Docker recovery and release handoff

The failure appears at the engine endpoint. After saving active WSL and container
work, restart Docker Desktop via **Troubleshoot → Restart Docker Desktop**. Check
`wsl --version` and update WSL if required by the installed Docker Desktop release.
Then check `docker --context desktop-linux version` for a populated **Server**
section. Continue with the Dockerfile and Compose smoke only after that check passes.
If the 500 persists, collect a Docker Desktop diagnostic for the workstation owner.
Preserve existing containers, volumes and distributions during diagnosis.
[Docker Desktop troubleshooting](https://docs.docker.com/desktop/troubleshoot-and-support/troubleshoot/)
· [WSL 2 backend requirements](https://docs.docker.com/desktop/features/wsl/)

A healthy Linux runner can execute the isolated Compose smoke. A public release
also needs a selected provider, private PostgreSQL, a dedicated migration role,
an approved DNS hostname and TLS, protected Data Protection keys, and protected
deployment credentials. Select a deployment path from the
[runbook](../deployment/backend.md). Production ingress, a real OTLP collector,
centralized log retention and alerting require an operational environment.

## Git safety

A targeted path-only scan found no private-key headers, high-confidence token
patterns, or developer-specific absolute paths in the intended working tree.
Connection string matches were the documented placeholder, Compose interpolation,
and a synthetic CI fixture. High-confidence tracked-history token/key search
found no matches. This is a targeted scan and does not stand in for a remote
Gitleaks run. The unrelated pre-existing root file beginning
`e schema and documentation` is outside the release set. Build outputs, reports,
private key and local environment file patterns are ignored.

Stage the solution, backend and Agent source, test projects, lockfiles,
Docker and workflow configuration, documentation, and verification scripts after
review. Leave the pre-existing unrelated file and ignored artifacts out of the commit.

Suggested commit message: `feat: deliver and verify Focus Assistant backend v1`.
