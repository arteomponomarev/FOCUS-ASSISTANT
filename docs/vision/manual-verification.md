# Manual webcam verification

1. Follow [Windows setup](setup.md). Start a disposable database, migrate it
   with `dotnet run --project Focus.Api -c Release --no-launch-profile -- --migrate`,
   and start the API at `http://127.0.0.1:8080` and Vite at
   `http://127.0.0.1:5173`. The
   [Windows review guide](../frontend/manual-review-checklist.md) supplies the
   exact connection and frontend commands.
2. Sign in with a disposable account and open **Settings > Privacy >
   Computer Vision**. Enable camera totals. Enable the experiment if you
   plan to compare against desktop idle activity.
3. Optionally probe and calibrate a device. Create a planned session of
   about one minute, and copy its UUID from **Connect Windows Agent**.
4. Launch Agent and enter the same account's credentials. Type `CAMERA`
   to approve the camera for this run. In combined mode also type `START`.
   If an earlier Agent is paired to this session, stop it with Ctrl+C first.
   The message `event=avatar_bridge_unavailable` can indicate that the
   previous Agent still owns the session's avatar channel.
   If the API returns `429`, wait for the displayed retry period before
   signing in again. The browser and Agent can share the same authentication
   rate limit on localhost.
   Build in a separate output directory to avoid locked files under
   `Focus.Agent/bin`. Run these commands from the repository root.

```powershell
dotnet build Focus.Agent -c Release --artifacts-path artifacts/vision-dotnet
if ($LASTEXITCODE -ne 0) { throw 'Agent build failed.' }
$agent = (Resolve-Path '.\artifacts\vision-dotnet\bin\Focus.Agent\release\Focus.Agent.dll').Path
$session = Read-Host 'Session UUID'
# Camera only
dotnet $agent --api http://127.0.0.1:8080 --session $session --watch --vision --camera 0 --fps 5
# Camera and desktop activity, as an alternative
dotnet $agent --api http://127.0.0.1:8080 --session $session --collect --avatar --vision --camera 0 --fps 5
```

5. Launch the rebuilt Windows Avatar in a second PowerShell window:

```powershell
$session = Read-Host 'Same session UUID'
& .\artifacts\avatar-win64\FocusAvatar.exe -focus-session $session -focus-dashboard http://127.0.0.1:5173
```

6. Ensure the camera stays idle while Planned. Start the session. Confirm
   the Windows device indicator and **Camera · processing locally** in
   Unity. Sit in good light for several seconds. Turn left and right, look
   down, move out of frame for at least 15 seconds, then return. A position
   may be unclassified. A wake-up reaction comes from the next processed
   estimate after the underlying state changes.
7. Pause and verify that capture stops. Resume and complete. Open
   **Session signals > Computer Vision**. Verify observed and unknown
   durations, ratios, and a final partial interval in the totals. Do a
   second run of about 20 seconds to exercise the partial upload.
8. Revoke camera consent during a new active session. Confirm the indicator
   goes out and observations stop. Verify Ctrl+C immediately stops local
   processing. Review that the existing desktop telemetry path still works
   independently.
