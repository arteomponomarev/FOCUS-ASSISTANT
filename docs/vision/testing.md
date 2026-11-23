# Focus.Vision testing

Run the synthetic Python suite after [setup](setup.md):

```powershell
Push-Location .\Focus.Vision
try {
    & .\.venv\Scripts\python.exe -B -m unittest -v
    & .\.venv\Scripts\python.exe -B benchmark.py
} finally {
    Pop-Location
}
```

The tests exercise the actual pinned YuNet file with a synthetic background,
an unusable frame and a face drawn in memory. The generic pose fit is
tested against synthetic projected points. Other cases cover ambiguous
multi-face results, calibration and release and recovery of a mocked camera.

Run .NET against a disposable PostgreSQL role and server as described in
[backend testing](../testing/backend.md). To include the Python subprocess
fixture and actual Unity window capture, build the
[Player](../../Focus.Avatar/README.md) and run in the same Windows desktop:

```powershell
$output = 'artifacts/vision-dotnet'
$env:FOCUS_VISION_TEST_PYTHON = (Resolve-Path Focus.Vision/.venv/Scripts/python.exe).Path
$env:FOCUS_VISION_WINDOW_SMOKE = (Resolve-Path scripts/test-avatar-window.ps1).Path
$env:FOCUS_AVATAR_VISUAL_SMOKE = $env:FOCUS_VISION_WINDOW_SMOKE
dotnet restore 'FOCUS ASSISTANT.slnx' --artifacts-path $output --locked-mode
dotnet build 'FOCUS ASSISTANT.slnx' --artifacts-path $output -c Release --no-restore -warnaserror
dotnet test 'FOCUS ASSISTANT.slnx' --artifacts-path $output -c Release --no-build --results-directory artifacts/vision-release-tests --logger 'trx;LogFilePrefix=vision-release'
```

- `VisionTests.cs` covers gaps, ambiguous orientation, a 20-second final
  boundary and opt-in rules without camera evidence.
- `CameraProcessTests.cs` launches a synthetic Python child. It tests the
  private protocol, unavailable and recovered codes, and child shutdown.
- `VisionWorkflowTests.cs` upgrades an existing disposable schema, uses
  owned and foreign accounts, exercises consent, strict JSON, deduplication,
  pause/resume, a final late upload and account-level camera shutdown on web
  logout. It proves different baseline and experimental estimates and
  observes the synthetic camera status and estimate in the built Windows
  Player when requested. The capture is `artifacts/vision-avatar-player.png`.
- `Focus.Web/e2e/vision.spec.ts` uses the real isolated API and PostgreSQL.
  It toggles consent independently and renders previously absent totals
  after a synthetic upload. Use the ordinary
  [frontend test setup](../frontend/testing.md) and `npm run test:e2e`.

## Reference performance

Windows x64, Python 3.12.9, OpenCV 4.12.0 and one OpenCV worker thread.
The benchmark ran 150 synthetic 320 × 240 frames on CPU at the 5 FPS
setting. It measured **4.99 FPS**, **7.64 ms mean inference and filtering**,
**10.23 ms p95**, **3.54% of one CPU core** and a **53.0 MiB peak working
set**. Physical camera timing and real-workload CPU usage remain unverified.
The 15-second Agent window and short final interval were checked with
synthetic timestamps rather than using the benchmark for capture latency.

The Windows Vision CI workflow runs the synthetic Python suite and
verifies the pinned model digest. It requires no camera or system input hooks.
The [manual procedure](manual-verification.md) verifies hardware behavior
with explicit owner participation.

## Verification results

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Python synthetic model and camera-adapter tests | 7 | 0 | 0 |
| .NET unit tests including the private subprocess and occupied-channel fixtures | 58 | 0 | 0 |
| ASP.NET Core / PostgreSQL tests and requested Unity scenarios | 13 | 0 | 0 |
| React Vitest | 28 | 0 | 0 |
| Playwright default, Agent, QA and zoom suites | 10 | 0 | 0 |

Locked .NET restore, Release build with warnings as errors, OpenAPI runtime
comparison, independent OpenAPI validation, generated TypeScript types,
TypeScript, ESLint, Prettier and Unity Windows x64 build succeeded. Vite
still prints the two existing Rollup/Zod annotation notices. The optional
transparent-pixel and click-through tests could not acquire either global
shortcut during this task. The final Unity CV and avatar-state tests ran
in their window capture modes on the real Player, showing the persisted
experimental estimate and local status for synthetic input. Full native
hotkey and click-through behavior was verified in the previous desktop task.

The business-logic coverage gate measured 87.44% line coverage and 78.17%
branch coverage. Gitleaks found no credentials in the CV change set.
Actionlint passed for the new CV workflow. Remote GitHub execution and
physical webcam validation were outside the local run.

The final pre-commit regression repeated the Release build, all 58 unit
tests and 12 integration tests against the disposable PostgreSQL server.
The optional Unity visual fixture was skipped during this run to leave the
interactive desktop undisturbed. The earlier full Unity run is recorded above.

## Local change list

The following Git status was recorded on main at `195d8c8`. Separate
deployment work and earlier untracked reports are outside this list.

```text
M .gitignore
M Focus.Agent/Program.cs
M Focus.Agent/Sync/ActivityQueue.cs
M Focus.Agent/Sync/AgentApi.cs
M Focus.Agent/Sync/CaptureBuffer.cs
M Focus.Agent/Sync/Contracts.cs
M Focus.Agent/Sync/LocalAvatarPipe.cs
M Focus.Agent/Sync/SessionAgent.cs
M Focus.Api.IntegrationTests/AvatarWorkflowTests.cs
M Focus.Api.IntegrationTests/ContractTests.cs
M Focus.Api/Controllers/AuthController.cs
M Focus.Api/Controllers/PrivacyController.cs
M Focus.Api/Controllers/ResearchController.cs
M Focus.Api/Hosting/OpenApiContract.cs
M Focus.Api/Program.cs
M Focus.Application/Accounts/AccountService.cs
M Focus.Application/Accounts/IAccountStore.cs
M Focus.Application/Research/IResearchStore.cs
M Focus.Application/Research/TelemetryService.cs
M Focus.Avatar/Assets/Runtime/AvatarView.cs
M Focus.Avatar/Assets/Runtime/Protocol/AvatarProtocol.cs
M Focus.Backend.Tests/ApplicationTests.cs
M Focus.Backend.Tests/AvatarProtocolTests.cs
M Focus.Domain/Entities/ActivityAggregate.cs
M Focus.Domain/Entities/User.cs
M Focus.Engine/FocusClassifier.cs
M Focus.Infrastructure/Accounts/AccountStore.cs
M Focus.Infrastructure/Persistence/AnalyticsStore.cs
M Focus.Infrastructure/Persistence/Configurations/ActivityAggregateConfiguration.cs
M Focus.Infrastructure/Persistence/Migrations/FocusDbContextModelSnapshot.cs
M Focus.Infrastructure/Persistence/ResearchStore.cs
M Focus.Web/src/api/schema.ts
M Focus.Web/src/features/sessions/Signals.tsx
M Focus.Web/src/features/sessions/data.ts
M Focus.Web/src/features/settings/Account.tsx
M Focus.Web/src/features/settings/Privacy.tsx
M Focus.Web/src/features/settings/data.ts
M Focus.Web/src/features/settings/export.ts
M README.md
M docs/api/README.md
M docs/api/changelog.md
M docs/api/openapi.json
M docs/database/data-dictionary.md
M docs/database/schema.md
M scripts/test-avatar-window.ps1
?? .github/workflows/vision-ci.yml
?? Focus.Agent/Sync/CameraSource.cs
?? Focus.Agent/Sync/VisionSamples.cs
?? Focus.Api.IntegrationTests/VisionWorkflowTests.cs
?? Focus.Application/Research/VisionResearch.cs
?? Focus.Backend.Tests/CameraProcessTests.cs
?? Focus.Backend.Tests/VisionTests.cs
?? Focus.Domain/Entities/VisionAggregate.cs
?? Focus.Infrastructure/Persistence/Migrations/20261123161834_AddCameraObservation.Designer.cs
?? Focus.Infrastructure/Persistence/Migrations/20261123161834_AddCameraObservation.cs
?? Focus.Vision/benchmark.py
?? Focus.Vision/install-model.ps1
?? Focus.Vision/model.json
?? Focus.Vision/requirements.txt
?? Focus.Vision/test_vision.py
?? Focus.Vision/vision.py
?? Focus.Web/e2e/vision.spec.ts
?? Focus.Web/src/features/sessions/VisionSummary.tsx
?? Focus.Web/src/features/settings/ComputerVision.tsx
?? docs/vision/algorithms.md
?? docs/vision/architecture.md
?? docs/vision/limitations.md
?? docs/vision/manual-verification.md
?? docs/vision/privacy.md
?? docs/vision/setup.md
?? docs/vision/testing.md
```
