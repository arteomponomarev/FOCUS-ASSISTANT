# Focus Assistant Avatar for Windows

The Unity companion runs alongside the React client. It pairs to one owned
session through an authenticated Windows Agent and reacts to the latest
processed Focus Engine estimate. It can also run without activity collection.

## Build a Windows x64 Player

Use Unity **6000.6.2f1** with Windows Standalone support, an active Unity license,
and Windows x64 with Direct3D 11. Install the .NET SDK selected in
[`global.json`](../global.json) for the Agent. The Unity project uses only built-in
Unity modules. The tested build uses the Mono scripting backend.

From the repository root in PowerShell:

```powershell
$editor = 'D:\6000.6.2f1\Editor\Unity.exe' # Set to your installed editor path.
$project = (Resolve-Path .\Focus.Avatar).Path
$log = Join-Path $PWD 'artifacts\avatar-unity.log'
New-Item -ItemType Directory -Force .\artifacts | Out-Null
$arguments = '-batchmode -quit -buildTarget Win64 -projectPath "{0}" -executeMethod Focus.Avatar.Editor.AvatarBuild.BuildWindows -logFile "{1}"' -f $project, $log
$build = Start-Process -FilePath $editor -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($build.ExitCode -ne 0) { throw "Unity build failed. Read $log" }
```

Alternatively add `Focus.Avatar` in Unity Hub and use **Focus Assistant >
Build Windows x64**. Keep the entire `artifacts/avatar-win64/` directory together
when moving the Player. Run the build under the Windows user with the Unity
license. A license error before compilation can indicate that a separate shell
or automation account is missing an entitlement.

## Pair and launch

1. Start the existing PostgreSQL, API and website using the
   [Windows review guide](../docs/frontend/manual-review-checklist.md). Its
   local API and dashboard use ports **8080** and **5173** respectively.
2. In React, create a planned session with a goal. Copy the **Connect Windows
   Agent** session UUID. Start the Agent in a Windows terminal and sign in
   with the same account as the website. To try controls without observing
   activity, use:

   ```powershell
   $session = Read-Host 'Session UUID'
   dotnet run --project Focus.Agent -c Release -- --api http://127.0.0.1:8080 --session $session --watch
   ```

   To collect counts, enable telemetry consent in React under **Settings >
   Privacy**, then run the recording variant and enter `START`:

   ```powershell
   $session = Read-Host 'Session UUID'
   dotnet run --project Focus.Agent -c Release -- --api http://127.0.0.1:8080 --session $session --collect --avatar
   ```

3. Start the Unity Player from a second PowerShell terminal using the same UUID:

   ```powershell
   $session = Read-Host 'Session UUID'
   & .\artifacts\avatar-win64\FocusAvatar.exe -focus-session $session -focus-dashboard http://127.0.0.1:5173
   ```

   Use the configured HTTPS origins for remote API or browser access. The
   paired Agent and Unity Player must run under the same Windows account.
4. Use **Start**, **Pause** or **Resume** in the panel. **Finish in browser**
   opens the owned session detail in the web client for ratings, goal outcomes
   and confirmation. **Minimize** dismisses controls, **Hide** dismisses the
   companion until the displayed recovery shortcut, and **Exit** closes the
   Player. Drag the panel by its title. The Player tries **Ctrl + Alt + F9**,
   then **Ctrl + Alt + F10**, then **Ctrl + Alt + F8**. If all three shortcuts
   are unavailable, the window keeps the standard Windows frame and disables
   hiding and click-through. **Settings** offers a local Reduce motion switch
   and a link to the account's Assistant settings.
   Ctrl+C in the Agent terminal stops its local bridge
   and any authorized recording. Exit both and repeat pairing to use another
   session.

See the [desktop demo checklist](../docs/avatar/demo-checklist.md),
[architecture](../docs/avatar/architecture.md), and
[visual design and verification](../docs/avatar/desktop-visual-design.md).
