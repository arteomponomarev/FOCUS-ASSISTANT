# Focus Assistant manual review checklist

Allow 10–15 minutes for the walkthrough after the tooling and database are ready. Use synthetic names, goals and ratings.

## Prerequisites

- Windows PowerShell, the .NET SDK pinned in [global.json](../../global.json), and PostgreSQL 17.
- A local PostgreSQL role allowed to create a database, plus its local password.
- Node.js 22.13 or newer in the Node 22 line, npm, and Chrome or Edge.

## Terminal 1 — disposable database and backend

Run in the repository root. The unique database name protects existing development data. Enter credentials only at the prompts.

```powershell
$reviewDb = 'focus_review_' + [Guid]::NewGuid().ToString('N')
$reviewRole = Read-Host 'Local PostgreSQL role with CREATE DATABASE'
$pg = Join-Path $env:ProgramFiles 'PostgreSQL/17/bin'
& (Join-Path $pg 'createdb.exe') -h 127.0.0.1 -p 5432 -U $reviewRole -W $reviewDb
if ($LASTEXITCODE -ne 0) { throw 'The disposable database could not be created.' }

$reviewPassword = Read-Host 'Password for that local role' -AsSecureString
$reviewConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
$reviewConnection['Host'] = '127.0.0.1'
$reviewConnection['Port'] = 5432
$reviewConnection['Database'] = $reviewDb
$reviewConnection['Username'] = $reviewRole
$reviewConnection['Password'] = [System.Net.NetworkCredential]::new('', $reviewPassword).Password

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ConnectionStrings__PostgreSQL = $reviewConnection.ConnectionString
$reviewConnection.Clear()
$reviewPassword.Dispose()
try {
  dotnet restore 'FOCUS ASSISTANT.slnx' --locked-mode
  if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
  dotnet build Focus.Api -c Release --no-restore
  if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
  dotnet run --project Focus.Api -c Release --no-build --no-launch-profile -- --migrate
  if ($LASTEXITCODE -ne 0) { throw 'Migration of the disposable database failed.' }
  dotnet run --project Focus.Api -c Release --no-build --no-launch-profile --urls http://127.0.0.1:8080
} finally {
  Remove-Item Env:ConnectionStrings__PostgreSQL -ErrorAction SilentlyContinue
}
```

Open `http://127.0.0.1:8080/health/ready`. Expect HTTP 200 before opening the SPA. Stop with Ctrl+C when finished.

## Terminal 2 — frontend

Run in a separate PowerShell window from the repository root.

```powershell
cd Focus.Web
npm ci
$env:FOCUS_API_ORIGIN = 'http://127.0.0.1:8080'
npm run dev -- --port 5173 --strictPort
```

Open `http://127.0.0.1:5173`. The Vite proxy sends `/api` to the local backend.

## Walkthrough

| Time | Action | Expected result |
| --- | --- | --- |
| 2 min | Register with a new synthetic email ending in `@example.invalid`, a display name and a password of 12–128 characters. Try one incorrect password, then sign in. | The invalid login shows an inline alert. Successful login opens Overview with a first-session prompt. |
| 3 min | Select **New session**, choose a duration, add two goals, and create the session. Select **Start session**, **Pause**, then **Resume**. | The detail route opens. The timer advances while active and the available controls follow the current status. |
| 2 min | Select **Finish session**. Choose concentration 4, productivity 3, two goal outcomes, and enter a short reflection. Save. | The completed screen shows both ratings, the reflection and saved outcomes. |
| 2 min | Open **Sessions**, filter by **Completed**, then reopen the item. Reload and sign in again. | The item stays in history and the protected detail URL opens after sign-in. |
| 2 min | Open **Overview** and **Analytics**. Change the report period and open **Compare reported ratings, estimates and goals**. | The totals and ratings reflect the saved session. A session without Agent data has no estimated states. |
| 2–4 min | Enable Chrome DevTools device mode with Ctrl+Shift+M while DevTools is open. Try 390×844 and 844×390, switch themes, and navigate from a scrolled page. Open the finish dialog on another active test session. | Navigation remains available. The new route starts at the top. Tab stays inside the dialog, and Escape restores focus. |

Open the [Adaptive UI PDF](../design/focus-assistant-adaptive-ui.pdf) for mobile, tablet and desktop comparisons.

To exercise actual Windows activity, follow the [Agent setup guide](../agent/windows-setup.md)
using the same account and disposable review database. Pair a planned session,
start the session, switch apps for about 20 seconds and finish it. Check both
**Activity aggregates** and **Estimated states** in Session signals.

For automated review against an existing disposable test cluster, follow [browser test setup](testing.md). The Playwright launcher starts the API and production preview, creates a uniquely named database, and applies migrations there.

## Startup recovery

- **Readiness returns 503.** Verify that the PostgreSQL service is running on the selected local port, the temporary database exists, and migration succeeded.
- **A port is already in use.** Choose a free backend port and set `FOCUS_API_ORIGIN` to that address before starting Vite. Keep the browser at the URL printed by Vite.
- **The frontend cannot reach the API.** Check readiness and the proxy target, then restart Vite after changing its environment.
- **`createdb.exe` is absent.** Point `$pg` to the installed PostgreSQL 17 bin directory. If PostgreSQL uses a port other than 5432, update the port in both database commands.
- **The app asks for sign-in after reload.** This is expected for memory-only credentials. Use the same disposable account.
- **Authentication reports too many requests.** Wait for the server's authentication rate limit to reset.
