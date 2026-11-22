# Backend v1 database addendum

The original migrations and core dictionaries remain the baseline for upgrades.
The current schema adds the following data:

| Table or extension | Data and integrity |
|---|---|
| `AccountCredentials` | `UserId` is both PK and FK to Users. `NormalizedEmail` is unique. `PasswordHash` stores an Identity hash. |
| `AccountLogins` | UUID token-pair ID, credential owner FK, expiry, and optional family ID for historical compatibility. Unique owner/family index keeps one active pair per family. |
| `SessionPauses` | UUID PK, session FK, start and optional end timestamps. A filtered unique index permits only one open pause per session. A check constraint enforces interval order. |
| `WorkProfiles` | UUID PK, owner FK, name and description, default focus and break minutes. The alternate key includes owner ID. |
| `ProcessRules` | Composite PK of profile ID and normalized process name. Integer category values 0 Neutral, 1 Allowed, and 2 Distracting. |
| `UserPreferences` | Owner PK/FK, Pomodoro values, avatar visibility, intensity, and optional avatar identifier. |
| `Users.TelemetryConsentAt` | Nullable UTC consent timestamp. NULL disables ingestion. |
| `FocusSessions.WorkProfileId` | Nullable profile reference with a composite foreign key that enforces common ownership. A referenced profile cannot be deleted independently. |
| `ActivityAggregates.ClientEventId` | Nullable legacy-compatible UUID. A unique index on session and client ID enforces upload idempotency. |
| `FocusStateRecords.ActivityAggregateId` | Nullable for historical rows. Unique FK ties a new estimate to one aggregate in the same session. |
| `FocusStateRecords.InputProcessCategory` | Optional category snapshot. Allows reproduction even if the profile is edited. |

Stored session values remain 0 Planned, 1 Active, 2 Completed, 3 Cancelled, with 4
Paused added. Goal values remain 0 Pending, 1 Completed, 2 NotCompleted, with 3
PartiallyCompleted added. Focus state values are unchanged. No ratings or telemetry
are backfilled for historical rows. Account deletion cascades through owned data.

Session mutations acquire an account lock and then a session row lock. Telemetry
holds those locks through consent validation, overlap checks, and atomic insertion
of the observed and derived rows. Account refresh and logout lock the credential
row to serialize concurrent changes. See the versioned migrations for exact DDL.

Apply with the separate migration command in the
[deployment runbook](../deployment/backend.md). Use a disposable database for
upgrade validation. Existing profiles and user rows are preserved by additive
changes.
