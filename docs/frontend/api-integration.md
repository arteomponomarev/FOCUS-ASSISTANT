# Frontend API integration

The contract is [OpenAPI v1](../api/openapi.json). Run `npm run api:generate` in `Focus.Web` after a contract update and include the generated `src/api/schema.ts`. CI checks that generated types match the specification.

| Feature | Implemented operations used by the client |
| --- | --- |
| Authentication | Register, login, refresh, logout, get/update current user, change password |
| Dashboard and history | Search sessions with server pagination and date/status/sort filters, analytics summary and time buckets |
| Session workspace | Create and retrieve sessions, start, pause, resume, complete, cancel, create/edit/delete goals |
| Session signals | Activity and estimated states with pagination, summary and pauses from session timeline |
| Analytics | Summary, time buckets by Day/Week/Month and per-session research metrics |
| Account settings | Read/update Pomodoro, avatar preferences and telemetry consent, account deletion |
| Profiles | List/create/update/delete work profiles and their application rules |
| Data export | Backend account export manifest and authorized paginated session, timeline and signal reads |

[API errors](../api/error-handling.md) are normalized centrally. Client-side validation identifies fields and the backend's redacted Problem Details messages appear inline. The client distinguishes 401, 403, 404, 409, 413, 415, 429 and 5xx responses, timeouts and network errors. Request IDs may be displayed for diagnostics. Unexpected server details are suppressed. The client uses a 15-second timeout and aborts canceled reads.

## Browser authentication

Bearer credentials live in JavaScript memory for the current page lifecycle. Expiring credentials use one shared rotating refresh request per tab. A rejected refresh clears credentials and the query cache. A full reload requires sign-in. Only the theme is stored in web storage. Public routes do not require a database connection to display the form. Session operations require the live API.

## Agent signals

Session details show the session UUID for explicit Windows Agent pairing. The selected
signals tab polls every two seconds while active or paused and for the first two
minutes after completion. Refresh signals is available beyond that window. Account
consent, recorded zero counts and the absence of records have distinct UI states.
Agent presence remains unknown to the browser. [Connect the Windows Agent](../agent/windows-setup.md)

## Deployment

Build with `npm ci` and `npm run build` in `Focus.Web`. Serve `Focus.Web/dist` over HTTPS with immutable caching for hashed assets and revalidation for `index.html`. Route `/api/*` to the existing API and return `index.html` for frontend paths so deep links work after reload. The Vite preview server is intended for local verification.

A separate HTTPS API origin can be set at build time through `VITE_API_BASE_URL`. Configure the existing backend CORS allowlist with the exact web origin through `Http__AllowedOrigins__0` in that topology. The API already exposes Location, Retry-After and X-Request-Id. The build must never receive a secret through a `VITE_` variable.

Local Vite proxy ingress is configured through `FOCUS_API_ORIGIN`. See [API Getting Started](../api/getting-started.md) for database startup and [backend deployment](../deployment/backend.md) for production configuration.
