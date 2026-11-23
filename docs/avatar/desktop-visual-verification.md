# Orbit Windows verification

## Local evidence

Ran on Windows x64 with Unity 6000.6.2f1, Direct3D 11 and PostgreSQL 17
on a separate loopback test port. The test fixtures opened their own
Player windows and captured those windows with Win32 PrintWindow.

| Check | Result |
| --- | --- |
| Unity Windows x64 Player | Build completed. `artifacts/avatar-win64/FocusAvatar.exe`. |
| Locked restore and Release build | Succeeded with zero warnings and zero errors. |
| Unit tests | 53 passed. Includes nine distinct poses, transition interruption, blink and breathing, reduced motion, bounded celebration, v1 IPC compatibility and pause timing. |
| Integration tests and Windows render fixture | 12 passed. Covers the real Agent/API/PostgreSQL workflow and nine visual states rendered in the Player. |
| Agent and Focus Engine | Synthetic aggregate flowed through the authenticated Agent and API. Unity displayed the persisted Focused estimate with the fixture goal. Start, pause, resume, consent, ownership and completion passed through existing API operations. |
| Windows window behavior | Topmost, color key, transparent-pixel hit test, passive click-through, restore to interactive, exit and work-area positioning passed at 400 × 478, 500 × 598 and 600 × 717. |
| Scale coverage | All three client sizes were rendered and inspected at a system DPI of 96. Native display settings at 125% and 150%, mixed-DPI monitors and physical shortcut delivery remain unverified. |
| Physical controls and sensors | Native hit testing and hotkey message delivery were automated. Physical keyboard or mouse telemetry, hardware camera capture and user interaction with the web handoff were outside this run. Camera remains an unavailable status label. |

The first review showed a magenta fringe on the rounded frame. The final
frame uses an opaque cutout for the color-key boundary, while surfaces and
art inside the panel retain their antialiasing. The larger client revealed
face and icon strokes that drifted from their anchors. Rotation now composes
in reference-panel coordinates before the scale transform. An old running companion
held F9 during capture. The updated Player displayed F10 and recovered via
its registered message receiver.

## Reproduce

Build the Player using the [Unity build instructions](../../Focus.Avatar/README.md).
Set `FOCUS_TEST_DATABASE` to a disposable PostgreSQL connection on loopback with
a database name beginning `focus_test_`. Run the following from the repository
root in the signed-in Windows desktop:

```powershell
$output = 'artifacts/avatar-redesign-dotnet'
dotnet restore 'FOCUS ASSISTANT.slnx' --artifacts-path $output --locked-mode
dotnet build 'FOCUS ASSISTANT.slnx' -c Release --artifacts-path $output --no-restore -warnaserror
$env:FOCUS_REQUIRE_POSTGRES = '1'
$env:FOCUS_AVATAR_WINDOW_SMOKE = (Resolve-Path 'scripts/test-avatar-window.ps1').Path
$env:FOCUS_AVATAR_WINDOW_SCALES = '100,125,150'
$env:FOCUS_AVATAR_VISUAL_SMOKE = $env:FOCUS_AVATAR_WINDOW_SMOKE
dotnet test 'FOCUS ASSISTANT.slnx' -c Release --artifacts-path $output --no-build --results-directory artifacts/avatar-task15-final-tests --logger 'trx;LogFilePrefix=task15-final'
```

The real API smoke writes `artifacts/avatar-window.png` and the two scaled
captures. The optional visual test creates a unique directory under
`artifacts/avatar-state-captures` and uses controlled, test-only IPC replies.
The unit tests and the ordinary API tests run without opening the Player.

See [rendered evidence](screenshots/redesign/README.md).

## Change inventory

| Area | Files |
| --- | --- |
| Player UI and drawing | `Focus.Avatar/Assets/Runtime/AvatarController.cs`, `AvatarView.cs`, `AvatarGraphics.cs`, `OrbitCharacter.cs`, `WindowsOverlay.cs` |
| Animation and state | `Focus.Avatar/Assets/Runtime/Animation/AvatarAnimator.cs`, `SessionController.cs`, `Protocol/AvatarMood.cs`, `Protocol/AvatarProtocol.cs` |
| Art, import and build | `Focus.Avatar/Assets/Resources/OrbitShell.png`, `Focus.Avatar/Assets/Editor/OrbitTextureImport.cs`, `AvatarBuild.cs`, `Focus.Avatar/ProjectSettings/ProjectSettings.asset`, and Unity metadata for the new source and asset files |
| Agent timing | `Focus.Agent/Sync/AgentApi.cs` |
| Tests | `Focus.Backend.Tests/Focus.Backend.Tests.csproj`, `AvatarAnimatorTests.cs`, `AvatarProtocolTests.cs`, `Focus.Api.IntegrationTests/AvatarWorkflowTests.cs`, `AvatarVisualTests.cs`, `scripts/test-avatar-window.ps1` |
| Documentation and evidence | `Focus.Avatar/README.md`, `docs/avatar/architecture.md`, `desktop-visual-design.md`, `desktop-visual-verification.md`, `docs/avatar/screenshots/redesign/` |
