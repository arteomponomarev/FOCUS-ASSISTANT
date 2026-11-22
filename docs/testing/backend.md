# Verifying the backend

Use a disposable PostgreSQL 17 server on loopback. Supply `FOCUS_TEST_DATABASE` through
the process environment with a database name starting in `focus_test_`. The fixture
creates unique databases and runs EF migrations. The test role needs database creation
privileges on that disposable server.

```powershell
dotnet restore "FOCUS ASSISTANT.slnx" --locked-mode
dotnet build "FOCUS ASSISTANT.slnx" -c Release --no-restore -warnaserror
dotnet test Focus.Api.IntegrationTests -c Release --no-build
dotnet test Focus.Backend.Tests -c Release --no-build --collect "XPlat Code Coverage" --settings docs/testing/business-logic.runsettings --results-directory artifacts/coverage-release
python scripts/check-coverage.py artifacts/coverage-release --minimum 80
dotnet format "FOCUS ASSISTANT.slnx" --no-restore --verify-no-changes
dotnet list "FOCUS ASSISTANT.slnx" package --vulnerable --include-transitive
```

Use a fresh coverage results directory each time. The denominator is all instrumented
lines in Focus.Domain, Focus.Application, and Focus.Engine. PostgreSQL integration tests
cover HTTP and persistence behavior. The quality gate uses measured line coverage.
When the TRX logger copies an attachment, the gate accepts byte-identical copies
and rejects conflicting reports.

Backend CI scans full Git history with Gitleaks. The ignore file lists two exact
historical fingerprints for a disposable Compose database password. The Compose
smoke test now generates and masks a fresh password per run. The cleanup step uses
the same temporary value. New matches remain subject to the scanner.

```powershell
gitleaks git . --redact --no-banner --gitleaks-ignore-path .gitleaksignore
```

The contract test verifies that runtime OpenAPI matches the versioned document. Regenerate
and validate after changing the contract.

```powershell
$env:FOCUS_EXPORT_OPENAPI = '1'
dotnet test Focus.Api.IntegrationTests --filter FullyQualifiedName~ContractTests
Remove-Item Env:FOCUS_EXPORT_OPENAPI
python -m pip install openapi-spec-validator==0.7.2
python -m openapi_spec_validator docs/api/openapi.json
```

Without `FOCUS_TEST_DATABASE`, PostgreSQL tests explicitly skip. The contract test uses
the ASP.NET Core test host and can run without a database. Set
`FOCUS_REQUIRE_POSTGRES=1` to require the database tests, as the CI job does.

An optional smoke test starts a published artifact on loopback with a generated TLS
certificate and a fresh database. It verifies migration, HTTPS, rejection of plain
HTTP, the authenticated session and telemetry workflow, OTLP export to a loopback
receiver, and persistence of credentials and research records across process restart.
Requires Python 3.9+ and OpenSSL.

```powershell
dotnet publish Focus.Api -c Release --no-restore -o artifacts/backend-release
python scripts/smoke-release.py artifacts/backend-release/Focus.Api.dll
```
