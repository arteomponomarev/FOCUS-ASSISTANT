# TASK 20 authenticated SPA performance

Measured November 23, 2026 on a Windows workstation using Chrome 140, Node 22.20 and Lighthouse 12.8.2. A Vite production preview and real ASP.NET Core ran over loopback with a disposable PostgreSQL 17 database. Login used a different synthetic account for each profile. Each account had one completed session. The mobile viewport for the authenticated steps was 390×844 and the desktop viewport was 1440×1000. Login uses the Lighthouse default mobile and desktop navigation profiles.

| Cold login navigation | Performance | Accessibility | LCP | TBT | CLS | Requests |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Mobile | 96/100 | 100/100 | 2,475 ms | 14.5 ms | 0 | 9 |
| Desktop | 100/100 | 100/100 | 591 ms | 0 ms | 0 | 9 |

Browser authentication succeeds before the protected measurements. The test clicks the rendered navigation links and captures a Lighthouse Timespan during each transition. A Snapshot follows after the target is visible. Session data and authentication stay in the same browser context.

| Protected route | Mobile requests | Desktop requests | TBT, both profiles | CLS, both profiles | Snapshot accessibility, both profiles |
| --- | ---: | ---: | ---: | ---: | ---: |
| Session history | 1 | 3 | 0 ms | 0 | 100/100 |
| Analytics | 1 | 1 | 0 ms | 0 | 100/100 |
| Settings | 3 | 3 | 0 ms | 0 | 100/100 |
| Overview return | 0 | 0 | 0 ms | 0 | 100/100 |

The initial populated Overview also scored 100/100 on both accessibility snapshots. These runs measure SPA interactions and a cached Overview return. A full navigation at protected routes would discard the in-memory session. Start-to-end request counts can vary with prefetch timing. The [Lighthouse user-flow guide](https://github.com/GoogleChrome/lighthouse/blob/main/docs/user-flows.md) distinguishes navigation from timespan and snapshot measurements. The 90+ overall navigation target is demonstrated on login only. Public network latency, cold protected navigation, lower-end physical phones and authenticated load at larger volumes remain unverified.

Vite reported 142.36 kB gzip for the main JavaScript chunk and 40.00 kB for the validation schema chunk. Individual Overview, Analytics and Settings route chunks were 2.04, 2.11 and 5.58 kB gzip. The main and validation chunks are the largest bundle contributions. Route splits and warm navigation request counts did not warrant a dependency or state-management change in this checkpoint.

## Reproduce

Run from `Focus.Web` after the [isolated browser setup](testing.md). Reserve ports 4197 and 5197. `FOCUS_LIGHTHOUSE_PACKAGES` must point to a directory with `package.json` and the installed `lighthouse@12.8.2` and its `puppeteer-core` dependency. `FOCUS_BROWSER_PATH` must point to the installed Chromium or Chrome executable. Use the fixture database variables documented in the browser setup.

```powershell
npm run build
node scripts/audit-user-flow.mjs
```

The script writes only measurements to ignored `artifacts/task20/lighthouse-user-flow.json`. No account credentials are written to that report. The fixture API uses its own random test database and synthetic accounts.
