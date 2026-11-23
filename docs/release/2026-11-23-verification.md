# Desktop and Vision release candidate verification

Verified locally on November 23, 2026. The candidate is the commit containing this record. Its application baseline is `0b78a10d0807c05cb635bdd2e3ba753b55f85aaf`, with the Windows overlay recovery change described below. Build and test on Windows used .NET SDK 10.0.401, PostgreSQL 17 in a separate loopback cluster, Node 22.20.0, installed Chrome, and Unity 6000.6.2f1. For exact-SHA remote results, see [GitHub Actions](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions?query=branch%3Amain).

## Prior CI failure

On `195d8c867621d9408f6cbbdd1396a92e5b9eecc2`, [Backend verify](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions/runs/45210063369/job/137816145434) failed with exit 2 at `dotnet format`. The audit reproduced whitespace diagnostics in `AvatarWorkflowTests.cs` at lines 42 and 142. The included `0b78a10` already formats those initializers without changing their assertions. The exact CI restore, Release build, and formatting commands passed in an isolated copy of the candidate. The formatting checks and 80% line-coverage gate are intact.

## Local test commands and results

| Check | Reproduction command or source | Result |
|---|---|---|
| .NET restore and Release build | `dotnet restore "FOCUS ASSISTANT.slnx" --locked-mode -p:WarningsAsErrors="NU1901%3BNU1902%3BNU1903%3BNU1904"`; `dotnet build "FOCUS ASSISTANT.slnx" -c Release --no-restore -warnaserror` | Passed, 0 warnings and 0 errors |
| Exact backend format gate | `dotnet format "FOCUS ASSISTANT.slnx" --no-restore --verify-no-changes` | Passed |
| Backend, Agent, protocol and Vision unit tests | `dotnet test Focus.Backend.Tests -c Release --no-build --collect "XPlat Code Coverage" --settings docs/testing/business-logic.runsettings`; set `FOCUS_VISION_TEST_PYTHON` to the test Python interpreter | 58 passed, zero failed or skipped |
| Backend coverage gate | `python scripts/check-coverage.py <coverage-directory> --minimum 80` | 724/828 lines, **87.44%** line and **78.17%** branch coverage |
| Standard API integration suite | `dotnet test Focus.Api.IntegrationTests -c Release --no-build` with `FOCUS_REQUIRE_POSTGRES=1` and `FOCUS_TEST_DATABASE` set to a disposable loopback PostgreSQL cluster | 12 passed, 0 failed, 1 optional visual test skipped in the standard run |
| Database/API local production smoke | `python -B scripts/smoke-release.py Focus.Api/bin/Release/net10.0/Focus.Api.dll --openssl <executable>` against a disposable database | Passed HTTPS, OTLP, credential and research persistence after restart |
| Frontend static and unit checks | In Focus.Web, `npm run typecheck`, `npm run lint`, `npm run format:check`, `npm run api:check`, `npm run test`, `npm run build` | All passed, 28 Vitest tests. Vite emitted two dependency annotation warnings from Zod |
| Browser regression | In Focus.Web, `npm run test:e2e`, `npm run test:e2e:qa`, `npm run test:e2e:agent`, and `npm run test:e2e:zoom`; set `FOCUS_WEB_TEST_DATABASE` and point `FOCUS_WEB_API_DLL` to the Release build | 5 + 3 + 1 + 1 passed. Covers Redux cache refresh, auth, session lifecycle, late Agent sync, CV totals and zoom |
| Vision | In Focus.Vision, `python -m pip check` and `python -B -m unittest -v` with the documented venv and pinned model | Dependencies consistent, 7 synthetic tests passed |
| Contract/Actions | `python -B -m openapi_spec_validator docs/api/openapi.json`; generated contract parity integration test; `actionlint -shellcheck=` | Passed, 42 API operations; actionlint checked workflow syntax |
| Dependencies | `dotnet package list --project "FOCUS ASSISTANT.slnx" --vulnerable --include-transitive --no-restore`, `npm audit --include=dev --audit-level=moderate` | No advisories reported |
| Secret scan | `gitleaks git --redact=100 --no-banner --gitleaks-ignore-path .gitleaksignore .` | No leaks in the candidate history before committing the overlay fix and this record |
| Local Docker | `docker version --format '{{.Server.Version}}'`; `docker compose config --quiet` | Configuration valid. The local Windows Docker Linux engine socket was unavailable, so runtime Compose relies on exact-SHA GitHub verification |

## Database and Computer Vision scope

`20261123161834_AddCameraObservation` adds nullable consent and aggregate payloads, a default-false experimental flag, and a constraint separating camera-only rows from desktop counters. The downgrade explicitly refuses to remove the columns while camera aggregate rows exist. Fresh and upgrade tests, ownership, deduplication, late windows, consent removal and contract parity passed on isolated PostgreSQL. No older migration changed in the stabilization commit.

Camera capture stays opt-in. It requires the Agent Vision option, explicit per-run acknowledgment, active session and separate server consent. The Python worker emits coarse samples, and persisted records contain aggregate durations and counts. Camera stop, ambiguity, head-angle geometry, consent, telemetry-only fallback and browser presentation were verified with synthetic input. Physical webcam and real desktop activity were outside this verification.

## Windows overlay

The audit found the secure fallback frame when Ctrl+Alt+F9 and Ctrl+Alt+F10 could not register. A native probe reproduced Windows error 1409 (hotkey already registered) on both and found Ctrl+Alt+F8 free. The Player now tries F8 after those two. When all three are unavailable it retains the close affordance of the native window and leaves click-through and hide disabled.

Unity 6000.6.2f1 built Windows x64 with `-batchmode -quit -buildTarget Win64 -executeMethod Focus.Avatar.Editor.AvatarBuild.BuildWindows` on an isolated project copy. `dotnet test Focus.Api.IntegrationTests -c Release --no-build --filter "FullyQualifiedName~Avatar"` passed both tests, with `FOCUS_AVATAR_WINDOW_SMOKE` and `FOCUS_AVATAR_VISUAL_SMOKE` pointing at the smoke script next to that build and `FOCUS_AVATAR_WINDOW_SCALES=100,125,150`. Captured nine animated states. The focused overlay displayed F8 and passed native topmost, position, color-key transparency, hit tests, passive click-through, hotkey recovery and clean exit. The three test sizes are client dimensions on a 96 DPI desktop. Actual OS display scale changes were outside this run. The paired Agent and database observations were synthetic.

## Historical evidence

[The independent audit](../audit/diploma-full-evaluation.md) records the earlier original window failure and old remote SHA separately. Current local artifacts stay under ignored `artifacts/task18/`. This release checkpoint does not establish public production, cloud service provisioning, or a trained project-specific ML model.
