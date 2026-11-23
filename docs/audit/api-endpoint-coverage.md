# API operation coverage

Audit date: 2026-11-23. Sources: [versioned OpenAPI](../api/openapi.json), [generator](../../Focus.Api/Hosting/OpenApiContract.cs), [HTTP controllers](../../Focus.Api/Controllers/), [contract parity test](../../Focus.Api.IntegrationTests/ContractTests.cs), and [execution evidence](test-evidence.md). The table covers the local snapshot. The published operation set excludes `GET /api/v1/sessions/{id}/vision`.

An example counts only when explicitly attached to operation request/response media or its inline schema. Model-level example reuse, request snippets in Markdown, and bodyless responses are separate evidence. The denominator for success examples excludes bodyless responses. Every operation still needs accurate statuses and security. All operation IDs, methods, paths, parameters and documented schemas are included in the generated contract parity check. Named functional suites below give behavioral evidence on subsets. Remaining operations have contract-level evidence only.

| Metric | 8cbe9bc | 195d8c8 | Local 0b78a10 |
|---|---:|---:|---:|
| Operations | 41 | 41 | 42 |
| Paths | 30 | 30 | 31 |
| Operations with summary | 41 | 41 | 42 |
| Operations with description | 0 | 0 | 0 |
| Operations with request schema | 16 | 16 | 16 |
| Operations with request example | 5 | 5 | 5 |
| Operations with success body | 36 | 36 | 37 |
| Operations with success example | 4 | 4 | 4 |
| Operations with error statuses | 41 | 41 | 42 |
| Operations with at least one error example | 5 | 5 | 5 |
| Named schemas | 51 | 51 | 55 |
| Schemas with descriptions | 0 | 0 | 0 |
| Direct schema properties | 187 | 187 | 213 |
| Direct properties with descriptions | 1 | 1 | 1 |

## Per-operation inventory

"ex" is an explicit example count for the preceding body or HTTP status. A dash stands for a field absent from the specification. Authenticated resource ownership is checked in application and persistence layers. The table records the boundary contract, followed by a separate map of runtime evidence.

| Route | Operation ID, summary / extended description | Path/query parameters | Body schema | Success status/schema | Error status/schema | Authentication | Handler / runtime evidence |
|---|---|---|---|---|---|---|---|
| **GET** `/api/v1/analytics/summary` | `AnalyticsSummary` — Summarize observed activity, estimates, goals, and self-reports in a date range. / — | From, To, TimeZone | — | 200 ReportBucket | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails | Bearer | AnalyticsController / RW |
| **GET** `/api/v1/analytics/statistics` | `AnalyticsStatistics` — Return calendar day, ISO week, or month buckets in the requested IANA time zone. / — | Period, From, To, TimeZone | — | 200 AnalyticsReport | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails | Bearer | AnalyticsController / EB |
| **GET** `/api/v1/analytics/research/sessions` | `AnalyticsResearch` — Page through comparable per-session research measures. / — | From, To, Status, Sort, Page, PageSize | — | 200 PageOfSessionResearchRow | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails | Bearer | AnalyticsController / RW |
| **POST** `/api/v1/auth/register` | `AuthRegister` — Create an account and return its public profile. / — | — | RegisterRequest (ex 1) | 201 Profile (ex 1) | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails; 429 ProblemDetails; 415 ProblemDetails | public | AuthController / SW |
| **POST** `/api/v1/auth/login` | `AuthLogin` — Exchange account credentials for access and refresh tokens. / — | — | LoginRequest | 200 AccessTokenResponse (ex 1) | 400 ProblemDetails; 401 ProblemDetails (ex 1); 500 ProblemDetails; 429 ProblemDetails; 415 ProblemDetails | public | AuthController / SW |
| **POST** `/api/v1/auth/refresh` | `AuthRefresh` — Rotate a refresh token and revoke the previous token pair. / — | — | RefreshRequest | 200 AccessTokenResponse | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails; 429 ProblemDetails; 415 ProblemDetails | public | AuthController / SW |
| **POST** `/api/v1/auth/logout` | `AuthLogout` — Revoke the current login and optionally withdraw camera consent for the account. / — | stopCamera | — | 204 — | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails; 429 ProblemDetails | Bearer | AuthController / SW |
| **GET** `/api/v1/auth/me` | `AuthMe` — Read the authenticated account profile. / — | — | — | 200 Profile | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails | Bearer | AuthController / SW |
| **PATCH** `/api/v1/auth/me` | `AuthRename` — Update the authenticated account's display name. / — | — | RenameRequest | 200 Profile | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | AuthController / EB |
| **POST** `/api/v1/auth/me/change-password` | `AuthChangePassword` — Change the password and revoke all account logins. / — | — | ChangePasswordRequest | 204 — | 400 ProblemDetails; 401 ProblemDetails; 500 ProblemDetails; 429 ProblemDetails; 415 ProblemDetails | Bearer | AuthController / SW |
| **GET** `/api/v1/settings/privacy` | `PrivacyGet` — Read independent desktop and camera consent and the experimental estimate preference. / — | — | — | 200 PrivacySettings | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | PrivacyController / VW |
| **PATCH** `/api/v1/settings/privacy` | `PrivacyUpdate` — Change server-side acceptance or the vision experiment without opening a device. / — | — | PrivacyUpdate (ex 1) | 200 PrivacySettings | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | PrivacyController / VW |
| **GET** `/api/v1/settings/privacy/export` | `PrivacyExport` — Read the account privacy export and links to pageable records. / — | — | — | 200 PrivateExport | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | PrivacyController / EB |
| **POST** `/api/v1/settings/privacy/delete-account` | `PrivacyDeleteAccount` — Verify the account password and delete the account and its owned data. / — | — | DeleteAccountRequest | 204 — | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 429 ProblemDetails; 415 ProblemDetails | Bearer | PrivacyController / EB |
| **POST** `/api/v1/sessions/{id}/activity/batch` | `ResearchUpload` — Store independently consented desktop or camera aggregates with retry deduplication. / — | id | ActivityBatch (ex 1) | 200 IngestionResult (ex 1) | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 403 ProblemDetails; 409 ProblemDetails; 413 ProblemDetails; 422 ProblemDetails; 415 ProblemDetails | Bearer | ResearchController / RW |
| **GET** `/api/v1/sessions/{id}/activity` | `ResearchActivity` — Page through observed aggregate windows in chronological order. / — | id, Page, PageSize | — | 200 PageOfActivityResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | ResearchController / RW |
| **GET** `/api/v1/sessions/{id}/focus-states` | `ResearchEstimates` — Page through heuristic estimates in chronological order. / — | id, Page, PageSize | — | 200 PageOfEstimateResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | ResearchController / RW |
| **GET** `/api/v1/sessions/{id}/focus-states/current` | `ResearchCurrent` — Read the latest available estimate, session status, and goals for an owned session. / — | id | — | 200 CurrentStateResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | ResearchController / AW |
| **GET** `/api/v1/sessions/{id}/vision` | `ResearchVision` — Summarize coarse camera observation totals and compare baseline and experimental estimates for an owned session. / — | id | — | 200 VisionReport | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | ResearchController / VW |
| **GET** `/api/v1/sessions/{id}/timeline` | `ResearchTimeline` — Read lifecycle timestamps, goals, and a page of chronological interval references. / — | id, Page, PageSize | — | 200 TimelineResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | ResearchController / RW |
| **POST** `/api/v1/sessions` | `SessionsCreate` — Create a planned session with optional goals and an owned work profile. / — | — | CreateSessionRequest (ex 1) | 201 SessionResponse | 400 ProblemDetails (ex 1); 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | SessionsController / SW |
| **GET** `/api/v1/sessions` | `SessionsList` — Page through owned sessions by start or creation time. / — | From, To, Status, Sort, Page, PageSize | — | 200 PageOfSessionResponse | 400 ProblemDetails; 401 ProblemDetails (ex 1); 404 ProblemDetails; 500 ProblemDetails | Bearer | SessionsController / SW |
| **GET** `/api/v1/sessions/{id}` | `SessionsGet` — Read one owned session with durations, ratings, and goal statistics. / — | id | — | 200 SessionResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails (ex 1); 500 ProblemDetails | Bearer | SessionsController / SW |
| **POST** `/api/v1/sessions/{id}/start` | `SessionsStart` — Start a planned session. / — | id | — | 200 SessionResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails | Bearer | SessionsController / SW |
| **POST** `/api/v1/sessions/{id}/pause` | `SessionsPause` — Record the beginning of a pause in an active session. / — | id | — | 200 SessionResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails | Bearer | SessionsController / SW |
| **POST** `/api/v1/sessions/{id}/resume` | `SessionsResume` — End the active pause and resume work. / — | id | — | 200 SessionResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails | Bearer | SessionsController / SW |
| **POST** `/api/v1/sessions/{id}/cancel` | `SessionsCancel` — Cancel a planned, active, or paused session. / — | id | — | 200 SessionResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails | Bearer | SessionsController / contract only |
| **POST** `/api/v1/sessions/{id}/complete` | `SessionsComplete` — Finish a started session and atomically record ratings and goal outcomes. / — | id | CompleteSessionRequest (ex 1) | 200 SessionResponse (ex 1) | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails (ex 1); 415 ProblemDetails | Bearer | SessionsController / SW |
| **GET** `/api/v1/sessions/{id}/goals` | `SessionsListGoals` — List goals attached to an owned session. / — | id | — | 200 array[GoalResponse] | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | SessionsController / SW |
| **POST** `/api/v1/sessions/{id}/goals` | `SessionsAddGoal` — Add a goal to a planned session. / — | id | CreateGoalRequest | 201 GoalResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails; 415 ProblemDetails | Bearer | SessionsController / EB |
| **GET** `/api/v1/sessions/{id}/goals/{goalId}` | `SessionsGetGoal` — Read a goal by its ID within an owned session. / — | id, goalId | — | 200 GoalResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | SessionsController / contract only |
| **PATCH** `/api/v1/sessions/{id}/goals/{goalId}` | `SessionsUpdateGoal` — Edit a planned goal description or record a started goal's outcome. / — | id, goalId | UpdateGoalRequest | 200 GoalResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails; 415 ProblemDetails | Bearer | SessionsController / contract only |
| **DELETE** `/api/v1/sessions/{id}/goals/{goalId}` | `SessionsDeleteGoal` — Remove a goal from a planned session. / — | id, goalId | — | 204 — | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails | Bearer | SessionsController / contract only |
| **GET** `/api/v1/settings/pomodoro` | `SettingsGetPomodoro` — Read persisted Pomodoro preferences or the defaults. / — | — | — | 200 PomodoroSettings | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | SettingsController / RW |
| **PUT** `/api/v1/settings/pomodoro` | `SettingsSetPomodoro` — Replace the authenticated account's Pomodoro preferences. / — | — | PomodoroSettings | 200 PomodoroSettings | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | SettingsController / RW |
| **GET** `/api/v1/settings/avatar` | `SettingsGetAvatar` — Read persisted avatar preferences or the defaults. / — | — | — | 200 AvatarSettings | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | SettingsController / EB |
| **PUT** `/api/v1/settings/avatar` | `SettingsSetAvatar` — Replace the authenticated account's avatar preferences. / — | — | AvatarSettings | 200 AvatarSettings | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | SettingsController / RW |
| **GET** `/api/v1/settings/work-profiles` | `SettingsGetProfiles` — Page through the account's work profiles and process rules. / — | Page, PageSize | — | 200 PageOfWorkProfileResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | SettingsController / EB |
| **POST** `/api/v1/settings/work-profiles` | `SettingsCreateProfile` — Create a work profile and optional process rules. / — | — | WorkProfileRequest | 201 WorkProfileResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | SettingsController / RW |
| **GET** `/api/v1/settings/work-profiles/{id}` | `SettingsGetProfile` — Read one owned work profile and its process rules. / — | id | — | 200 WorkProfileResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails | Bearer | SettingsController / RW |
| **PUT** `/api/v1/settings/work-profiles/{id}` | `SettingsReplaceProfile` — Replace an owned work profile and its process rules. / — | id | WorkProfileRequest | 200 WorkProfileResponse | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 415 ProblemDetails | Bearer | SettingsController / RW |
| **DELETE** `/api/v1/settings/work-profiles/{id}` | `SettingsDeleteProfile` — Delete an unused profile. Sessions retain their profile references. / — | id | — | 204 — | 400 ProblemDetails; 401 ProblemDetails; 404 ProblemDetails; 500 ProblemDetails; 409 ProblemDetails | Bearer | SettingsController / RW |

## Runtime evidence and gaps

Per-row handler abbreviations are class names in `Focus.Api/Controllers/`. Every row had generated-route parity with the committed contract. Runtime labels identify exercised paths; some paths are exercised only on a subset of statuses, or indirectly through the browser. Contract-only rows are not independently behavior-verified.

- SW: `SessionWorkflowTests.cs` / authenticated browser session lifecycle. Goal list is also used in browser workflow.
- RW: `ResearchWorkflowTests.cs` and API session/analytics assertions. Authorized profile retrieval and deletion are supplemented by negative ownership/conflict cases.
- AW: `AvatarWorkflowTests.cs` API and bridge scenario, including persisted current estimate.
- VW: `VisionWorkflowTests.cs` synthetic account/camera consent. The video input itself is not part of this API.
- EB: browser routes from settings, QA and lifecycle/analytics under `Focus.Web/e2e/`.

The versioned contract covers the application API. The anonymous operational routes `GET /health/live` and `GET /health/ready` are documented separately in Markdown, with health/readiness behavior exercised by `SessionWorkflowTests.cs`. Development/Contract also serves generated `GET /openapi/v1.json`; these infrastructure routes are outside the 41/42 application operation counts.


| Route family | Behavior directly exercised | Source |
|---|---|---|
| Authentication and user account | Registration, rotation, revocation, cross-user access, CORS and rate limit. Browser profile, password, refresh, export and disposal of test account. | `SessionWorkflowTests.cs`, `e2e/qa.spec.ts`, `e2e/settings.spec.ts` |
| Sessions and goals | Creation, start, pause, resume, completion, history, persisted goals and ratings. Direct database constraints and upgrade regression. | `SessionWorkflowTests.cs`, `ReleaseVerificationTests.cs`, `e2e/workflows.spec.ts` |
| Telemetry and estimates | Authenticated batch, replay, partial completion window and state history. | `AgentPipelineTests.cs`, `ResearchWorkflowTests.cs`, `e2e/agent.spec.ts` |
| Analytics and research | Owner isolation and persisted derived data, cached browser overview refresh and pagination. | `ResearchWorkflowTests.cs`, `e2e/qa.spec.ts` |
| Settings and privacy | Profiles, rules, preferences and deletion for disposal account. Independent camera opt-in and duplicate handling. | `e2e/settings.spec.ts`, `VisionWorkflowTests.cs`, `e2e/vision.spec.ts` |
| Health | Runtime readiness exercised by browser harness. Dedicated production/Compose health gate skipped on latest remote run. | `scripts/e2e-api.mjs`, `backend-ci.yml` |

The [integration tutorial](../api/integration-tutorial.md), [problem-details guide](../api/error-handling.md) and [authentication guide](../api/authentication.md) complement the specification. Use a dedicated contract fixture for every successful response body and meaningful errors. Capture actual serialized examples, use deterministic synthetic IDs/timestamps, and validate fixture bodies against their schemas. Build the first batches around sessions, account errors, telemetry, analytics, and CV if the CV commit is included in submission. An API version number and generated types cannot establish operational parity for every status path.
