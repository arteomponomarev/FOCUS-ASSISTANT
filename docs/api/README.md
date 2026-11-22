# Focus Assistant API v1

The API supports account authentication, focus sessions, goals, observed activity,
heuristic focus estimates, reports, work profiles, and account preferences.

- [OpenAPI JSON](openapi.json)
- [Getting started](getting-started.md)
- [Authentication](authentication.md)
- [Errors](error-handling.md)
- [Integration tutorial](integration-tutorial.md)
- [Backend architecture](../architecture/backend.md)
- [Research definitions](../architecture/research-metrics.md)
- [Versioning](versioning.md)

Protected routes require an opaque bearer access token. All owner-scoped queries derive
the account identity from that token. A resource owned by somebody else returns 404.
OpenAPI is served at `/openapi/v1.json` in Development and Contract. Production clients
use the versioned artifact above.

| Resource | Operations under `/api/v1` |
|---|---|
| Accounts | `POST /auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout`, `/auth/me/change-password`; `GET/PATCH /auth/me` |
| Sessions | `GET/POST /sessions`, `GET /sessions/{id}`, `POST /sessions/{id}/{start,pause,resume,cancel,complete}` |
| Goals | `GET/POST /sessions/{id}/goals`, `GET/PATCH/DELETE /sessions/{id}/goals/{goalId}` |
| Telemetry | `POST /sessions/{id}/activity/batch`, `GET /sessions/{id}/activity` |
| Estimates and timeline | `GET /sessions/{id}/focus-states`, `/focus-states/current` and `/timeline` under the same session |
| Analytics | `GET /analytics/summary`, `/analytics/statistics`, `/analytics/research/sessions` |
| Preferences | `GET/PUT /settings/pomodoro` and `/settings/avatar`; `GET/POST /settings/work-profiles`; `GET/PUT/DELETE /settings/work-profiles/{id}` |
| Privacy | `GET/PATCH /settings/privacy`, `GET /settings/privacy/export`, `POST /settings/privacy/delete-account` |

Collections return `items`, `pageNumber`, `pageSize`, and `totalCount`. The default page
is 1 with 25 items. The server default maximum is 100. Session history filters on
`startedAt ?? createdAt` using inclusive `from` and exclusive `to`. Set `sort=asc` or
`sort=desc` and optionally a string `status`. Do paging across a stable range when a
consistent export matters.
