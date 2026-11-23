
# Focus Assistant — Database Data Dictionary

The core dictionary below records the historical two-migration baseline. See the
[backend v1 addendum](backend-v1.md) for additional fields, tables, constraints,
and migration compatibility.

## 1. Overview

Focus Assistant uses PostgreSQL as its main database and Entity Framework Core for data access and schema migrations.

The database stores user accounts, focus sessions, session goals, aggregated computer activity, and estimated concentration states.

The collected data supports research into the relationship between computer activity and users' self-reported concentration and productivity.

The historical baseline contains five application tables. The
[backend v1 addendum](backend-v1.md) covers the six additional application tables
and the constraints added by later migrations.

## 2. Users

Stores basic user account information.

| Column | PostgreSQL Type | Constraints | Description |
|---|---|---|---|
| Id | uuid | PK, NOT NULL | Unique user identifier |
| Email | varchar(320) | NOT NULL, UNIQUE | User email address |
| DisplayName | varchar(100) | NOT NULL | User display name |
| CreatedAt | timestamptz | NOT NULL | Account creation timestamp |

Relationships:
- One user can have multiple focus sessions.
- Deleting a user cascades to related sessions.

## 3. FocusSessions

Stores planned, active, completed, and cancelled work sessions.

| Column | PostgreSQL Type | Constraints | Description |
|---|---|---|---|
| Id | uuid | PK, NOT NULL | Unique session identifier |
| UserId | uuid | FK, NOT NULL | References Users.Id |
| PlannedDuration | interval | NOT NULL | Planned session duration |
| Status | integer | NOT NULL | Session status enum |
| CreatedAt | timestamptz | NOT NULL | Session creation timestamp |
| StartedAt | timestamptz | NULL | Actual session start |
| EndedAt | timestamptz | NULL | Actual session end |
| SelfReportedConcentration | integer | NULL | User concentration rating (1–5) |
| SelfReportedProductivity | integer | NULL | User productivity rating (1–5) |
| Reflection | varchar(1000) | NULL | Optional user reflection |

Relationships:
- Each session belongs to one user.
- Each session can contain multiple goals.
- Each session can contain multiple activity aggregates.
- Each session can contain multiple focus state records.

The concentration and productivity ratings are collected after a session and are used in research analysis.

## 4. SessionGoals

Stores goals associated with focus sessions.

| Column | PostgreSQL Type | Constraints | Description |
|---|---|---|---|
| Id | uuid | PK, NOT NULL | Unique goal identifier |
| FocusSessionId | uuid | FK, NOT NULL | References FocusSessions.Id |
| Description | varchar(500) | NOT NULL | Goal description |
| Status | integer | NOT NULL | Goal status enum |

Relationships:
- Each goal belongs to one focus session.
- Deleting a session deletes its goals.

## 5. ActivityAggregates

Stores aggregated computer activity within time intervals.

| Column | PostgreSQL Type | Constraints | Description |
|---|---|---|---|
| Id | uuid | PK, NOT NULL | Unique aggregate identifier |
| FocusSessionId | uuid | FK, NOT NULL | References FocusSessions.Id |
| StartedAt | timestamptz | NOT NULL | Interval start |
| EndedAt | timestamptz | NOT NULL | Interval end |
| ActiveProcessName | varchar(200) | NULL | Active application process name |
| KeyboardEventCount | integer | NOT NULL | Number of keyboard events |
| MouseClickCount | integer | NOT NULL | Number of mouse clicks |
| WindowSwitchCount | integer | NOT NULL | Number of window switches |
| IdleSeconds | integer | NOT NULL | Seconds of inactivity |

Relationships:
- Each aggregate belongs to one focus session.
- A session can have multiple aggregates.
- Deleting a session deletes its aggregates.

Privacy:
- Actual key values are not stored.
- Clipboard contents are not stored.
- Activity is represented by aggregated numeric values.

## 6. FocusStateRecords

Stores concentration states estimated by the rule-based Focus Engine.

| Column | PostgreSQL Type | Constraints | Description |
|---|---|---|---|
| Id | uuid | PK, NOT NULL | Unique record identifier |
| FocusSessionId | uuid | FK, NOT NULL | References FocusSessions.Id |
| StartedAt | timestamptz | NOT NULL | State interval start |
| EndedAt | timestamptz | NOT NULL | State interval end |
| State | integer | NOT NULL | Estimated concentration state |
| RuleVersion | varchar(50) | NOT NULL | Version of the rule-based algorithm |
| Reason | varchar(500) | NULL | Explanation of the estimated state |

The planned FocusStateType enum contains:

| Value | State |
|---|---|
| 0 | Neutral |
| 1 | Focused |
| 2 | Distracted |
| 3 | Idle |
| 4 | Break |
| 5 | Absent |

These states are estimates, not direct measurements of attention.

RuleVersion makes it possible to identify which algorithm version produced each result.

## 7. Indexes

| Table | Index | Purpose |
|---|---|---|
| Users | IX_Users_Email | Unique email lookup |
| FocusSessions | IX_FocusSessions_UserId | Find sessions by user |
| FocusSessions | IX_FocusSessions_StartedAt | Find sessions by start time |
| SessionGoals | IX_SessionGoals_FocusSessionId | Find goals by session |
| ActivityAggregates | IX_ActivityAggregates_FocusSessionId_StartedAt | Retrieve activity in chronological order |
| FocusStateRecords | IX_FocusStateRecords_FocusSessionId_StartedAt | Retrieve estimated states in chronological order |

## 8. Data Integrity

The database uses primary keys, foreign keys, unique indexes, required fields, and maximum string lengths.

The baseline relations from users to sessions and from sessions to dependent
records use cascade deletion. The current schema also has a non-cascading
work-profile reference from a session. See the
[backend v1 addendum](backend-v1.md).

The C# domain model also validates selected business rules, including rating ranges and valid session transitions.

These application-level validations are not all enforced through PostgreSQL CHECK constraints in the current migrations.

## 9. Database Migrations

The baseline was introduced by the first two Entity Framework Core migrations:

1. InitialCreate
2. AddTelemetryAndResearchData

Migrations are stored in:

Focus.Infrastructure/Persistence/Migrations/

The __EFMigrationsHistory table records applied migrations and is maintained by Entity Framework Core.
