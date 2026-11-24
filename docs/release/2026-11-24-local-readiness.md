# Focus Assistant local demo readiness

Verified on Windows on November 24, 2026. The last verified published baseline at the start of this review was `1e6988e238bf8fcd8e9b72a6331f2eb7a8eab84b`, which already includes development Swagger UI. The demo changes and this report belong to the same new checkpoint. Use `git log -1 --format=%H -- docs/release/2026-11-24-local-readiness.md` for its exact SHA and [GitHub Actions](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions?query=branch%3Amain) for that SHA's remote checks.

**Verdict** — the local web, database, classification and Windows overlay demonstration is ready with synthetic signals. Rehearse physical keyboard/mouse and webcam capture on the presentation workstation before promising either live. Keep the API/browser fallback ready.

## Functional feature matrix

Verified may refer to a real application flow with synthetic input. Each row indicates its scope.

| Area and feature | Status | Source and evidence |
| --- | --- | --- |
| Account registration, login and logout | IMPLEMENTED AND VERIFIED | [Auth API](../../Focus.Api/Controllers/AuthController.cs) and [browser auth workflow](../../Focus.Web/e2e/workflows.spec.ts). An isolated browser registered and signed in on Kestrel. The browser suite also logged out and checked the protected route. |
| Authentication continuity | PARTIAL | [Client](../../Focus.Web/src/api/client.ts) and [auth QA](../../Focus.Web/e2e/qa.spec.ts). Explicitly memory-only web credentials, refresh rotation during use, and renewed sign-in on reload or a restart of the ephemeral development key ring. Account and session records persist in PostgreSQL. |
| Privacy settings, independent telemetry opt-in and opt-out | IMPLEMENTED AND VERIFIED | [Privacy settings](../../Focus.Web/src/features/settings/Privacy.tsx), [agent state machine](../../Focus.Agent/Sync/SessionAgent.cs), [sync tests](../../Focus.Backend.Tests/AgentSyncTests.cs) and [authorization workflow](../../Focus.Api.IntegrationTests/AgentPipelineTests.cs). Consent starts disabled and opt-out rejects uploads. |
| Camera OFF by default, explicit opt-in | IMPLEMENTED AND VERIFIED | [Camera settings](../../Focus.Web/src/features/settings/ComputerVision.tsx), [Agent entry point](../../Focus.Agent/Program.cs) and [Vision API workflow](../../Focus.Api.IntegrationTests/VisionWorkflowTests.cs). Account consent and a separate per-run `CAMERA` confirmation control capture. The defaults and server boundaries were exercised. |
| Create session, planned duration and multiple goals | IMPLEMENTED AND VERIFIED | [Session creation](../../Focus.Web/src/features/sessions/Create.tsx), [API](../../Focus.Api/Controllers/SessionsController.cs) and [browser workflows](../../Focus.Web/e2e/workflows.spec.ts). Real Chrome flow with two goals. |
| Start/pause/resume/complete and update goal outcomes | IMPLEMENTED AND VERIFIED | [Session details](../../Focus.Web/src/features/sessions/Detail.tsx), [goals](../../Focus.Web/src/features/sessions/Goals.tsx), [desktop lifecycle regression](../../Focus.Web/e2e/agent.spec.ts) and [IPC integration](../../Focus.Api.IntegrationTests/AvatarWorkflowTests.cs). |
| Reflection, subjective concentration/productivity and history | IMPLEMENTED AND VERIFIED | [Completion form](../../Focus.Web/src/features/sessions/Completion.tsx), [history](../../Focus.Web/src/features/sessions/History.tsx) and [browser workflows](../../Focus.Web/e2e/workflows.spec.ts). Two goal outcomes, reflection and 4/5 and 3/5 were saved and read back. |
| Real Windows key/mouse counts, foreground process changes, idle time | IMPLEMENTED BUT NOT VERIFIED | [Windows hooks](../../Focus.Agent/Hooks/GlobalInputHook.cs), [counts](../../Focus.Agent/Collectors/InputActivityCollector.cs), [window switches](../../Focus.Agent/Collectors/WindowSwitchTracker.cs) and [idle collector](../../Focus.Agent/Collectors/IdleTimeCollector.cs). Physical activity capture requires the owner to run `--collect` and confirm `START`. |
| Active application categories, work profiles and reusable preferences | IMPLEMENTED AND VERIFIED | [Profile editor](../../Focus.Web/src/features/settings/WorkProfiles.tsx), [settings service](../../Focus.Application/Settings/SettingsService.cs), [browser settings](../../Focus.Web/e2e/settings.spec.ts) and [classifier tests](../../Focus.Backend.Tests/FocusClassifierTests.cs). Allowed, Neutral and Distracting are rule categories. An uploaded synthetic `editor` signal with an Allowed rule produced `Focused` in the IPC integration. |
| Focus Engine classification, API ingestion and PostgreSQL storage | IMPLEMENTED AND VERIFIED | [Classifier](../../Focus.Engine/FocusClassifier.cs), [ingestion service](../../Focus.Application/Research/TelemetryService.cs) and [Agent pipeline](../../Focus.Api.IntegrationTests/AgentPipelineTests.cs). A real Kestrel restart retained the synthetic observation and versioned estimate. Estimates are heuristic. |
| Retry, durable queue, final partial window and deduplication | IMPLEMENTED AND VERIFIED | [Queue](../../Focus.Agent/Sync/ActivityQueue.cs), [retry](../../Focus.Agent/Sync/OfflineSync.cs), [capture buffer](../../Focus.Agent/Sync/CaptureBuffer.cs), [sync tests](../../Focus.Backend.Tests/AgentSyncTests.cs), [Agent pipeline](../../Focus.Api.IntegrationTests/AgentPipelineTests.cs) and [late window browser test](../../Focus.Web/e2e/agent.spec.ts). Confirmed records survive restart, and a repeated upload is counted once. The browser updated from 15 seconds plus a final partial window. |
| Configurable focus duration and short/long break preferences | PARTIAL | [Pomodoro form](../../Focus.Web/src/features/settings/Preferences.tsx), [settings service](../../Focus.Application/Settings/SettingsService.cs) and [browser settings](../../Focus.Web/e2e/settings.spec.ts). Values persist and the manual session timer works. The current session flow has no automatic work/break cycle. |
| Timed Pomodoro notifications | MISSING | The [session view](../../Focus.Web/src/features/sessions/Detail.tsx) renders elapsed time and progress. [Avatar presentation](../../Focus.Avatar/Assets/Runtime/AvatarController.cs) responds to session state and intensity. No scheduled break-completion notification path was found. |
| Unity Player, topmost, transparency, click-through, recovery shortcut, graceful exit | IMPLEMENTED AND VERIFIED | [Windows overlay](../../Focus.Avatar/Assets/Runtime/WindowsOverlay.cs) and [Windows smoke](../../scripts/test-avatar-window.ps1). Ran a real Windows x64 Player, checked hit testing and window styles at the 100% client size, sent a recovery hotkey message and closed it cleanly. |
| Unity IPC state changes, appearance and interactions | IMPLEMENTED AND VERIFIED | [Agent bridge](../../Focus.Agent/Sync/AvatarBridge.cs), [Unity controller](../../Focus.Avatar/Assets/Runtime/AvatarController.cs), [owned-session IPC test](../../Focus.Api.IntegrationTests/AvatarWorkflowTests.cs) and [visual test](../../Focus.Api.IntegrationTests/AvatarVisualTests.cs). Nine synthetic states were rendered. The protocol integration exercised desktop start, pause and resume. A physical keyboard press for the shortcut remains a rehearsal item. |
| Vision face presence, approximate pose, no-face/unknown and local-only processing | IMPLEMENTED AND VERIFIED | [Vision worker](../../Focus.Vision/vision.py) and [synthetic model tests](../../Focus.Vision/test_vision.py). The pinned model ran on generated frames and rejected ambiguous inputs. This evidence covers synthetic inference only. |
| Vision aggregates, consent withdrawal and camera resource release | IMPLEMENTED AND VERIFIED | [Camera source](../../Focus.Agent/Sync/CameraSource.cs), [Vision tests](../../Focus.Vision/test_vision.py), [Vision API workflow](../../Focus.Api.IntegrationTests/VisionWorkflowTests.cs) and [Vision browser test](../../Focus.Web/e2e/vision.spec.ts). Tested aggregate shape, authorization, separate consent and release with injected capture devices and synthetic totals. |
| Physical webcam, Windows camera indicator and real pose quality | IMPLEMENTED BUT NOT VERIFIED | [Manual webcam procedure](../vision/manual-verification.md). This run did not activate a physical camera. |
| React dashboard, sessions, goals, analytics, settings and privacy | IMPLEMENTED AND VERIFIED | [Routes](../../Focus.Web/src/app/App.tsx), [browser workflows](../../Focus.Web/e2e/workflows.spec.ts), [settings](../../Focus.Web/e2e/settings.spec.ts) and [QA](../../Focus.Web/e2e/qa.spec.ts). Real local API and PostgreSQL behind the SPA. |
| React mobile, loading/error handling and live refresh | IMPLEMENTED AND VERIFIED | [Responsive](../../Focus.Web/e2e/responsive.spec.ts), [zoom](../../Focus.Web/e2e/zoom.spec.ts), [QA](../../Focus.Web/e2e/qa.spec.ts), [signal Saga](../../Focus.Web/src/features/sessions/signalsSaga.ts) and [desktop lifecycle regression](../../Focus.Web/e2e/agent.spec.ts). Includes real Kestrel outage and restart in the local walkthrough. |
| Swagger UI, all 42 operations, examples, Try it out and opaque Bearer auth | IMPLEMENTED AND VERIFIED | [Development UI](../../Focus.Api/Hosting/DevelopmentOpenApiUi.cs) and [contract tests](../../Focus.Api.IntegrationTests/ContractTests.cs). This review opened all 42 operations in Chrome. The baseline browser verification executed registration, login and authorized `GET /api/v1/auth/me` through Swagger UI. |
| Swagger UI disabled in Production | IMPLEMENTED AND VERIFIED | [Environment gate](../../Focus.Api/Program.cs) and [contract tests](../../Focus.Api.IntegrationTests/ContractTests.cs). Production and Contract lack the UI and its assets. Contract retains raw OpenAPI. |
| Cloud deployment, new ML training and gamification | OUT OF CURRENT SCOPE | Excluded from the local demonstration milestone. |

## Local startup and full workflow

The owner’s installed PostgreSQL 17 service was running. This review used a separate loopback PostgreSQL 17.2 cluster on 55454. A SQL query established connectivity, migrations created the demo database, and the launcher returned healthy API and web endpoints at 8080 and 5173, including a protected API request returning 401 through Vite. The real Swagger UI returned its 42 operations with the local Kestrel host. The release Player was launched from the existing Windows x64 build produced on the same unchanged Avatar source. The Vision venv loaded the model and ran seven synthetic cases. The launcher did not enable activity or a camera.

In a real Chrome browser connected to Kestrel and PostgreSQL, a disposable account registered and logged in, verified both consent flags started off, opted into synthetic desktop telemetry, configured a session with two goals, and observed start/pause/resume commands issued outside the browser. A synthetic window arrived in the web view with a rule version. Replay returned one duplicate. The test stopped the actual Kestrel process and observed an error in React, restarted the API against the same database, signed in again and read the existing estimate. It then completed the session with two goal outcomes, reflection and both ratings, and opened history and analytics.

Separately, the actual Release Windows Agent signed in through Kestrel in `--watch` mode. A local named-pipe client read its state and exercised start, pause and resume with activity and the camera off. Agent pipeline tests exercised synchronization of a completed 20-second session across a new Agent client and API host. The Unity tests used the real Player and local named pipe with controlled synthetic estimates. Physical keyboard, mouse and webcam capture remain for the owner rehearsal.

## Priority findings

- **P0 fixed** — Session details remained Planned after a successful external Start and could remain Paused after external Resume. The page polled only while it already saw Active. The new Playwright case reproduced the stale Planned state before the change. The page now polls every four seconds when focused and Planned, Active or Paused. Terminal status ends lifecycle polling. The regression passes without reloading or refocusing the page.
- **P1 completed** — the [Windows launcher](../../scripts/start-local-demo.ps1) builds into an isolated directory, uses a uniquely named or explicitly selected demo database, checks ports and readiness, and cleans up its own API and web processes. Agent and Avatar are started manually after a session exists.
- **P1 deferred** — automatic Pomodoro cycles and timed notifications. Demonstrate saved preferences and the current manual focus timer.
- **P1 manual** — physical desktop activity and webcam rehearsal. Reauthenticate after restarting the demo API or reloading the SPA. If the API stops during collection, the Agent pauses on loss of authority and keeps confirmed queued windows subject to the normal retry policy.
- **P2 later** — visual enhancements and broader multi-monitor or display-scale coverage. The overlay was checked at the current workstation’s client size.

## Launch on Windows

Run from the repository root in Windows PowerShell. Prerequisites are the [pinned .NET SDK](../../global.json), Node 22, and the local PostgreSQL 17 service. A local PostgreSQL role with database creation permission is required on the first run. The launcher prompts for credentials. The browser and API bind only to loopback.

```powershell
Get-Service postgresql-x64-17
& .\scripts\start-local-demo.ps1 -DatabaseName focus_demo_rehearsal
```

If the local service is stopped, start it from an elevated PowerShell with `Start-Service postgresql-x64-17` and run the launcher in your normal terminal. Use the same `-DatabaseName` to keep rehearsal data after stopping and restarting. The name must start with `focus_demo_`. Use `-Check` to validate startup and exit. If an intended service already occupies a port, leave it running and either use it deliberately or choose another `-ApiPort` and `-WebPort`, plus `-DatabasePort` if needed. The launcher prints the active addresses and log directory.

Open [React](http://127.0.0.1:5173), [readiness](http://127.0.0.1:8080/health/ready) and [Swagger UI](http://127.0.0.1:8080/api/docs). Register a disposable account, add two goals to a planned session, and copy the UUID under **Connect Windows Agent**.

In another terminal at the repository root, set `$session` and launch exactly one Agent mode. `--watch` gives the Player controls without activity or camera capture. For the physical desktop rehearsal, use the alternative command, enable desktop telemetry under **Settings > Privacy**, then type `START` when prompted. Sign in as the same disposable account.

```powershell
$session = [Guid](Read-Host 'Session UUID')
$agent = '.\artifacts\local-demo\dotnet\bin\Focus.Agent\release\Focus.Agent.dll'
dotnet $agent --api http://127.0.0.1:8080 --session $session --watch
# Alternative for explicit desktop capture
# dotnet $agent --api http://127.0.0.1:8080 --session $session --collect --avatar
```

In a third terminal, use the checked-in [build instructions](../../Focus.Avatar/README.md#build-a-windows-x64-player) if the Windows Player is absent or older than the verified overlay fix. Run it under the same Windows account as Agent.

```powershell
$session = [Guid](Read-Host 'Same session UUID')
& .\artifacts\avatar-win64\FocusAvatar.exe -focus-session $session -focus-dashboard http://127.0.0.1:5173
```

Start from the Avatar and watch React switch to Active with the page open. During the physical desktop rehearsal, switch windows and make some clicks for about 20 seconds. Pause, resume, and use **Finish in browser**. Enter ratings, outcomes and reflection. Inspect **Activity aggregates**, **Estimated states**, history and analytics. Test the recovery shortcut shown by the Player and close it. Use Ctrl+C in Agent before starting another instance. When testing persistence, stop and restart the launcher with the same demo database and sign in again.

## Physical webcam checklist and fallback

1. Prepare the [Vision venv and verified model](../vision/setup.md). Close webcam applications. Keep the camera off for the first pass.
2. In the disposable account enable **Settings > Privacy > Computer Vision**. Stop the previous Agent. Create and copy a new planned session UUID.
3. Run `dotnet $agent --api http://127.0.0.1:8080 --session $session --watch --vision --camera 0 --fps 5`. Enter `CAMERA` yourself. To combine physical activity and camera, replace `--watch` with `--collect --avatar` and confirm both prompts. Start the session and check the Windows camera indicator.
4. Look forward, turn, move outside the frame, and return. Pause to check release. Resume and finish. Open **Session signals > Computer Vision** and verify aggregate durations. Revoke camera consent in a second active session and check that capture stops. See the [full manual procedure](../vision/manual-verification.md).
5. If the camera is unavailable, demonstrate telemetry and estimates independently. If Unity is unavailable, complete the session in React. For a no-hardware fallback, set `FOCUS_DEMO_PASSWORD` to a new synthetic value of at least 12 characters and run `& .\scripts\demo-backend.ps1 -BaseUri http://127.0.0.1:8080` against the demo database. Label its account, inputs and deduplication as synthetic.

## Release checks

| Check | Result |
| --- | --- |
| .NET locked restore, Release build and format | Passed, no build warnings or errors |
| .NET unit and integration on PostgreSQL | 58 unit and 15 integration passed. One opt-in Unity visual test skipped in the standard run |
| Native Unity / IPC | 2 passed in a separate opt-in run. The visual test was included here |
| TypeScript, ESLint, Prettier, API types and Vite | Passed |
| Vitest and Playwright | 28 and 11 passed in the final regression |
| OpenAPI parity, spec and example gate | Passed, 42 operations |
| Vision | `pip check` and seven synthetic tests passed |
| Local startup | `start-local-demo.ps1 -Check` passed with local 8080/5173 and a disposable PostgreSQL database. A busy port was rejected before starting duplicates |

Before submission, rehearse the Windows activity flow and webcam only with the owner’s authorization. Keep the synthetic demo and React fallback for the presentation. The remaining Pomodoro automation and UI enhancements can be scheduled after submission.
