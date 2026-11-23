# Windows Avatar verification

Verified November 23, 2026 on Windows x64 with Unity **6000.6.2f1**,
Mono, Direct3D 11, .NET SDK **10.0.401**, and a disposable PostgreSQL 17
instance bound to `127.0.0.1:55454`. The build used Windows Standalone
support and the Unity license of the interactive Windows account.
An installed Unity 2022.3.30f1 LTS editor with Windows support was also
detected. This project and its verified Player use the pinned Unity 6 build.

| Check | Result | Evidence |
| --- | --- | --- |
| Windows Unity compilation and build | PASS | Build exit 0, `artifacts/avatar-unity-owner.log` and `artifacts/avatar-win64/FocusAvatar.exe`. |
| Locked restore and .NET Release build | PASS | Zero warnings and errors on the final build. |
| Backend and Agent unit suite | PASS | 47 passed, zero failures. Includes estimate mapping, framing and visibility tests in [AvatarProtocolTests.cs](../../Focus.Backend.Tests/AvatarProtocolTests.cs). |
| ASP.NET Core / PostgreSQL integration suite | PASS | 11 passed, zero failures. [AvatarWorkflowTests.cs](../../Focus.Api.IntegrationTests/AvatarWorkflowTests.cs) uses real authentication, ownership, consent, command conflicts, persisted classification, restart/reconnect of the bridge and session lifecycle. |
| Windows Player and processed data | PASS | Real Player received the current goal and the persisted **Focused** estimate from the Agent bridge. The synthetic upload went through API, Focus Engine and PostgreSQL. [Rendered screenshot](screenshots/windows-focused.png). |
| Windows overlay | PASS | [Smoke script](../../scripts/test-avatar-window.ps1) verified topmost and color-key settings, placement in the monitor work area, an interactive hit test, passage through transparent pixels, click-through while passive, toggling back to the panel through the hotkey message handler, and clean exit. |
| Frontend regression | PASS | TypeScript, ESLint, Prettier and API schema check passed. Vitest 28 passed. Playwright 4 core tests and 1 dedicated Agent telemetry test passed on Chromium with the isolated database. The Vite build passed with the existing two Rollup/Zod annotation warnings. |
| Physical Windows workflow | NOT VERIFIED | Physical input hooks were not started. Keyboard delivery of the shortcut, hand dragging between monitors, real capture-to-avatar latency, and clicking the desktop browser handoff require the [manual demo](demo-checklist.md). Browser completion is covered by Playwright and the API integration test. |
| Other Windows configurations | NOT VERIFIED | The smoke test used one Windows account and one monitor configuration. Separate Windows user accounts, mixed DPI, and alternate graphics drivers were outside this run. |

An early overlay build exited with Windows access violation during native
cleanup. The final version uses a separate Win32 message-only hotkey receiver,
leaves Unity's main window procedure in place, and avoids style changes during
engine teardown. The same smoke test then passed.

## Reproduce

Build the Player using the [Windows guide](../../Focus.Avatar/README.md).
Set `FOCUS_TEST_DATABASE` to a disposable loopback PostgreSQL connection with
a database name starting with `focus_test_` and a role that can create test
databases. Run from the repository root in a signed-in Windows desktop:

```powershell
dotnet restore 'FOCUS ASSISTANT.slnx' --locked-mode
dotnet build 'FOCUS ASSISTANT.slnx' -c Release --no-restore -warnaserror
$env:FOCUS_REQUIRE_POSTGRES = '1'
$env:FOCUS_AVATAR_WINDOW_SMOKE = (Resolve-Path 'scripts/test-avatar-window.ps1').Path
dotnet test 'FOCUS ASSISTANT.slnx' -c Release --no-build --results-directory artifacts/avatar-test-results --logger 'trx;LogFilePrefix=avatar-release'
Remove-Item Env:FOCUS_AVATAR_WINDOW_SMOKE
```

The window test launches the built Player when the optional environment variable
is set. It uses an active session from the disposable integration fixture and
saves only that window to `artifacts/avatar-window.png`. Standard CI can run the
protocol and integration tests without starting the Windows Player. Run the
frontend suite with the [existing test instructions](../frontend/testing.md)
and the additional `npm run test:e2e:agent` check.
