# Connect the Windows Agent

The Agent signs in independently and uploads aggregate activity to the same API and PostgreSQL database as the web application. The Agent pairs to one session ID supplied by the user. A browser sign-in and an Agent sign-in create separate authentication sessions.

## Start a recording

1. Start the API and web client against the intended local database. The [Windows review guide](../frontend/manual-review-checklist.md) provides the full setup. For the local preview the web client is at `http://127.0.0.1:5173` and the API is at `http://127.0.0.1:8080`.
2. Sign in to the website. In **Settings → Privacy**, enable **Allow aggregate telemetry**.
3. Create a planned session and open its details. Expand **Connect Windows Agent** under **Session signals** and copy the session UUID.
4. Open an interactive Windows PowerShell window at the repository root. Enter the real session ID when prompted.

```powershell
$session = Read-Host 'Session UUID from Connect Windows Agent'
dotnet run --project Focus.Agent -c Release -- --api http://127.0.0.1:8080 --session $session --collect
```

5. At the Agent prompt, type `START` and enter the email and password for the same account used in the browser. Windows input hooks start once the Agent confirms the session is active and account consent is enabled. A planned or paused session leaves the collector stopped.
6. Select **Start session** on the website. Switch between benign applications such as Calculator and Notepad and perform sample clicks and key presses. After about 20 seconds select **Finish session**, enter both ratings and save.
7. Open **Activity aggregates** and **Estimated states**. The web view checks for new results automatically for two minutes after completion. **Refresh signals** can check again after a longer outage. A recorded zero count is displayed as zero. The browser has no Agent presence endpoint and shows that connection status is unavailable.
8. Open the same session from History or reload and sign in again. Persisted activity and estimates remain visible. Close the Agent with Ctrl+C if it is still running.

For a remote installation, use the actual trusted HTTPS API origin. Loopback HTTP is accepted for local development. API origins use the root path. The Agent uses the operating system's TLS validation and accepts only the selected account and session.
Keep Windows and server UTC clocks synchronized. The API checks captured timestamps against the recorded session history.

## Collection and synchronization

- The Agent checks account consent and the owned session timeline approximately every second. Samples use a roughly 250 ms cadence. An active authorization expires after three seconds without a fresh check. Delivery delays and CLI scheduling affect the exact capture duration.
- Confirmed adjacent samples form approximately 15-second aggregates. The Agent flushes a partial aggregate at a pause, completion or authorized shutdown. The backend admits a late aggregate inside the recorded session boundary. A sample that straddles a pause or end boundary is excluded in full, so activity after a transition is never reassigned into the session. The start delay and sampling cadence can shorten the captured duration of a 20-second session.
- Pausing stops the Windows hooks after confirmation. Resuming starts a new interval. Completion ends capture and drains pending uploads. Cancellation clears undelivered records for that session. Consent withdrawal stops collection and removes the account's undelivered queue on the next authoritative check or upload rejection. Launch again to approve another run.
- The Agent assigns one UUID to each aggregate before writing an atomic disk record. It sends batches of up to 20, removes records on a successful acknowledgement, and retains them across retries and restarts. Temporary failures back off from one second to 64 seconds and respect a longer `Retry-After`. Server rejections of individual invalid records are counted in the console without printing data. Completed sessions support only a bounded late-upload window.
- Records use Windows DPAPI CurrentUser under `%LOCALAPPDATA%\FocusAssistant\agent-v1\`, scoped by API origin and account. A later login from the same Windows profile and API origin can deliver pending records for that account. Windows and account identities should remain the same for recovery. The queue prunes records older than 24 hours on a successful synchronization check. A stopped Agent leaves the encrypted queue on disk until the next run. The queue is capped at 4,096 records. Capacity or storage failure stops collection and retains records already saved.
- On temporary loss of authority, the Agent stops the hooks and persists already confirmed activity. It keeps the small unconfirmed tail in memory while the process remains open and reconciles it after a fresh API check. During Ctrl+C it attempts a bounded final check and delivery. A process crash or shutdown during a prolonged outage can lose unconfirmed samples. Confirmed queue entries remain on disk.

## Privacy

Tokens and the entered password live in process memory. Password input is hidden, and logout is attempted when the Agent exits. The collector uses foreground process identity, counts of key-down and mouse-button-down messages, sampled window switches, timestamps and idle duration. The Agent keeps native window handles only in memory to detect changes. Local JSONL created by older builds is separate from the encrypted queue and is never used for upload.

Focus Engine estimates come from the aggregate signals. They describe rule outputs. User concentration and productivity ratings remain self-reports.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| API unavailable | Confirm API readiness and the origin passed through `--api`. |
| HTTP 401 during login | Use the same email and password as the web account. |
| HTTP 404 after login | Copy the ID from a session owned by that account. |
| `event=telemetry_disabled` | Enable account telemetry consent and launch the Agent again. |
| An Agent is already using the queue | Close the other Agent for the same account and API before launching this one. |
| Activity is still empty | Confirm `event=capture_started` and `event=upload` in the terminal, then select **Refresh signals**. A sampled interval can be excluded when collection starts at the last moment or crosses a boundary. |

The automated suites inject synthetic aggregate samples without installing Windows hooks. The steps above exercise physical Windows activity manually.

## Verification evidence

The Windows Release build ran with warnings treated as errors. The .NET suite passed 37 backend and Agent tests and 10 PostgreSQL integration tests. The synthetic Agent test exercised actual login, batching, delayed finalization, queue recovery and backend restart against the same disposable test database. Two intervals of 15 and 5 seconds yielded two observations and two rule-versioned estimates. A replay returned two duplicates. The Windows suite also verified a DPAPI-encrypted disk round trip.

The frontend passed 25 unit tests and nine Playwright tests. The new browser workflow started and completed a short session through React, checked the first aggregate during the session, observed a zero-count final interval arrive after completion, and verified the same estimate and timeline counts after sign-in on reload. The browser test uploaded synthetic counts through the real API.

Run the .NET and OpenAPI checks with the disposable PostgreSQL environment from [backend testing](../testing/backend.md). An independent build directory keeps a running Windows review API from locking test output.

```powershell
dotnet build 'FOCUS ASSISTANT.slnx' -c Release --artifacts-path artifacts/agent-integration -warnaserror
dotnet test 'FOCUS ASSISTANT.slnx' -c Release --artifacts-path artifacts/agent-integration --no-build
$env:FOCUS_WEB_API_DLL = Join-Path (Get-Location) 'artifacts/agent-integration/bin/Focus.Api/release/Focus.Api.dll'
cd Focus.Web
npm run api:check
npm run build
npm run test:e2e:agent
```

The Playwright command also requires `FOCUS_WEB_TEST_DATABASE` and a Chromium browser, as described in [frontend testing](../frontend/testing.md). Real keystrokes, mouse clicks and window changes through the native Windows hooks are **NOT VERIFIED** in the automated run.
