
# Focus Assistant

**Backend v1** is implemented in the existing ASP.NET Core and PostgreSQL projects.
See the [API guide](docs/api/README.md), [deployment runbook](docs/deployment/backend.md),
[verification instructions](docs/testing/backend.md), and
[implementation evidence](docs/implementation/backend-progress.md), and
[independent release verification](docs/implementation/backend-verification.md).

Focus Assistant is a research-oriented application designed to study the relationship between computer activity, user concentration, and self-reported productivity.

The system collects aggregated Windows activity data, supports goal-oriented work sessions, and provides a foundation for evaluating concentration using explainable rules.

The project is developed as an individual diploma project.

## 1. Project Objectives

The main objectives are:

- Collect computer activity without recording typed text.
- Allow users to create and manage focus sessions.
- Track session goals and their completion.
- Store aggregated activity and estimated focus states.
- Collect user ratings of concentration and productivity.
- Investigate correlations between behavioral signals and self-reported concentration.
- Provide a foundation for an adaptive desktop assistant.

The system does not claim to measure human attention directly.

## 2. Technologies

| Component | Technology |
|---|---|
| Desktop Agent | C# / .NET 10 |
| Backend API | ASP.NET Core |
| Domain and Application Logic | C# |
| Database | PostgreSQL 17 |
| Object-Relational Mapping | Entity Framework Core 10 |
| PostgreSQL Provider | Npgsql |
| Database Migrations | EF Core Migrations |
| Development Environment | Visual Studio / VS Code |
| Version Control | Git / GitHub |

Planned additional components:

- Unity desktop avatar
- React web dashboard
- Python-based statistical analysis
- Optional computer vision using MediaPipe and OpenCV

These additional components are not required to reproduce the current PostgreSQL database.

## 3. Solution Structure

The solution contains six C# product projects and two automated test projects.

```text
FOCUS ASSISTANT/
|
|-- Focus.Agent/
|   |-- Collectors/
|   |-- Controllers/
|   |-- Hooks/
|   |-- Interop/
|   |-- Models/
|   |-- Services/
|   |-- Storage/
|   `-- Program.cs
|
|-- Focus.Api/
|   |-- Program.cs
|   |-- appsettings.json
|   `-- appsettings.Development.json
|
|-- Focus.Application/
|
|-- Focus.Domain/
|   |-- Entities/
|   `-- Enums/
|
|-- Focus.Engine/
|
|-- Focus.Infrastructure/
|   |-- Persistence/
|   |   |-- Configurations/
|   |   |-- Migrations/
|   |   `-- FocusDbContext.cs
|   `-- DependencyInjection.cs
|
|-- docs/
|   `-- database/
|       |-- data-dictionary.md
|       `-- schema.md
|
`-- README.md
```

### Project Responsibilities

**Focus.Agent**

Contains Windows activity collectors for active windows, idle time, keyboard/mouse activity, and window switching.

**Focus.Api**

ASP.NET Core application that registers application services and database infrastructure.

**Focus.Application**

Layer intended for application-level use cases and coordination.

**Focus.Domain**

Contains domain entities, enums, and business rules for users, focus sessions, and goals.

**Focus.Engine**

Project intended for rule-based estimation of user concentration.

**Focus.Infrastructure**

Contains EF Core database configuration, PostgreSQL integration, and migrations.

## 4. Database Overview

Focus Assistant uses PostgreSQL as its primary database.

Entity Framework Core is responsible for mapping C# entities to database tables.

The original schema contains five core application tables. Backend v1 adds
account credentials, sessions' pause intervals, and preferences as described in the
[schema addendum](docs/database/backend-v1.md).

| Table | Purpose |
|---|---|
| Users | User account information |
| FocusSessions | Work sessions and self-reported ratings |
| SessionGoals | Goals associated with work sessions |
| ActivityAggregates | Aggregated computer activity |
| FocusStateRecords | Estimated concentration states |

Entity relationships:

- One User can have multiple FocusSessions.
- One FocusSession can have multiple SessionGoals.
- One FocusSession can have multiple ActivityAggregates.
- One FocusSession can have multiple FocusStateRecords.

The database also contains the automatically managed `__EFMigrationsHistory` table.

### Database Documentation

Detailed database documentation is available here:

- [Data Dictionary](docs/database/data-dictionary.md)
- [Logical Database Schema and ER Diagram](docs/database/schema.md)

The data dictionary describes all tables, columns, PostgreSQL types, primary keys, foreign keys, indexes, and constraints.

The database schema document describes entity relationships and design decisions.

## 5. Database Setup

### 5.1 Prerequisites

Install the following:

- .NET 10 SDK
- PostgreSQL (tested locally with version 17)
- Entity Framework Core CLI tools
- Visual Studio or another compatible development environment

Check the .NET SDK:

```powershell
dotnet --version
```

Check the EF Core CLI:

```powershell
dotnet ef --version
```

For compatibility, use EF Core CLI version 10 with the project's EF Core 10 dependencies.

### 5.2 Restore Dependencies

Open a terminal in the solution root directory:

```powershell
dotnet restore
```

Build the solution:

```powershell
dotnet build
```

The solution must build successfully before migrations can be applied.

### 5.3 Configure PostgreSQL

Start the PostgreSQL server.

The application reads its database connection string from:

`ConnectionStrings:PostgreSQL`

For local development, the recommended configuration method is .NET User Secrets.

In Visual Studio:

1. Right-click the `Focus.Api` project.
2. Select **Manage User Secrets**.
3. Add the following configuration:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=focus_assistant;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

Replace `YOUR_PASSWORD` with your local PostgreSQL password.

The database name and username may be changed to match your environment.

User Secrets are stored outside the repository and must not be committed to Git.

Alternatively, the connection string can be supplied using the environment variable:

`ConnectionStrings__PostgreSQL`

### 5.4 Apply Database Migrations

The database schema is generated automatically using EF Core migrations.

From the solution root, run:

```powershell
dotnet ef database update --project Focus.Infrastructure --startup-project Focus.Api --context FocusDbContext
```

This command applies all pending migrations to the configured PostgreSQL database.

PostgreSQL must be running, and the configured user must have sufficient permissions to create the database schema.

**Do not manually create the application tables in pgAdmin.**

### 5.5 Available Migrations

The historical database baseline starts with two migrations:

1. `InitialCreate`
   - Creates Users
   - Creates FocusSessions
   - Creates SessionGoals
   - Configures keys, relationships, and indexes

2. `AddTelemetryAndResearchData`
   - Creates ActivityAggregates
   - Creates FocusStateRecords
   - Adds SelfReportedProductivity to FocusSessions
   - Configures additional foreign keys and indexes

Migration files are located in:

`Focus.Infrastructure/Persistence/Migrations/`

### 5.6 Verify Applied Migrations

Run:

```powershell
dotnet ef migrations list --project Focus.Infrastructure --startup-project Focus.Api --context FocusDbContext
```

Expected migrations:

```text
20261018200509_InitialCreate
AddTelemetryAndResearchData
```

The second migration's full name includes its generated timestamp.
The v1 migrations appear after these entries.

### 5.7 Verify Database Tables

Connect to the configured PostgreSQL database using pgAdmin or another SQL client.

Execute:

```sql
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
ORDER BY table_name;
```

Expected result:

```text
ActivityAggregates
FocusSessions
FocusStateRecords
SessionGoals
Users
__EFMigrationsHistory
```

The exact order depends on the SQL query.

The `__EFMigrationsHistory` table is managed by Entity Framework Core.

### 5.8 Verify the Database Connection

From the solution root, run:

```powershell
dotnet ef dbcontext info --project Focus.Infrastructure --startup-project Focus.Api --context FocusDbContext
```

The output should show:

- Context: FocusDbContext
- Provider: Npgsql.EntityFrameworkCore.PostgreSQL
- Database: focus_assistant, or your configured database name

## 6. Running the API

From the solution root:

```powershell
dotnet run --project Focus.Api
```

The API uses the connection string configured in the previous steps.

The current API project registers ASP.NET Core controllers and database infrastructure.

Database migrations are applied separately using the EF Core command shown above.

Use the [v1 integration tutorial](docs/api/integration-tutorial.md) to exercise the
implemented backend workflow. The local Agent uploader, React dashboard, and Unity
application are separate deliverables.

## 7. Research Data

The database has been designed to support research into the relationship between computer activity and subjective concentration.

### Activity Data

ActivityAggregates stores:

- Active application process name
- Keyboard event count
- Mouse click count
- Window switching count
- Idle duration
- Start and end timestamps

### User Feedback

FocusSessions stores:

- Self-reported concentration (1–5)
- Self-reported productivity (1–5)
- Optional reflection
- Session status and timestamps

### Estimated Focus States

FocusStateRecords stores:

- Estimated focus state
- State interval
- Rule version
- Explanation of the result

The planned states include:

- Neutral
- Focused
- Distracted
- Idle
- Break
- Absent

The research will compare these behavioral signals and estimated states with user feedback.

Correlation results have not yet been established.

## 8. Privacy and Security

Focus Assistant follows a privacy-oriented design.

The database schema stores aggregated activity rather than actual keyboard input.

The system is designed to avoid collecting:

- Typed text
- Passwords
- Clipboard contents
- Document contents
- Raw webcam recordings

Optional computer vision is planned as a separate feature requiring explicit user consent.

The current PostgreSQL schema does not include dedicated computer vision tables.

### Secrets Management

Never commit the following to Git:

- Database passwords
- API keys
- Access tokens
- Private credentials
- Files containing real secrets

Use .NET User Secrets or environment variables for development credentials.

### Database Integrity

The database uses:

- Primary keys
- Foreign keys
- Unique constraints
- Indexes
- Required fields
- Maximum string lengths
- Cascade deletion for dependent records

Some business constraints are currently validated in C# rather than by PostgreSQL CHECK constraints.

## 9. Database Baseline and Backend v1 Status

Implemented or verified:

- C# solution structure
- Domain entities for users, sessions, and goals
- PostgreSQL integration through EF Core
- Five database tables
- Two database migrations
- Primary and foreign key relationships
- Database indexes
- Database documentation
- Local database connection and migration verification

Backend v1 supports authentication, owned sessions and goals, consent-gated telemetry,
versioned heuristic estimates, analytics, and user preferences. The isolated PostgreSQL
workflow and coverage results are recorded in the implementation evidence.

Integration work outside the backend:

- Windows Agent upload adapter for the backend ingestion contract
- Local offline synchronization
- Unity desktop avatar
- Web dashboard
- Computer vision
- Statistical validation and research experiments

The presence of database tables does not mean that every planned application feature is already implemented.

## 10. Database Reproducibility

The database schema is version-controlled.

An evaluator can reproduce it by:

1. Cloning the repository.
2. Installing .NET and PostgreSQL.
3. Configuring a PostgreSQL connection string.
4. Restoring project dependencies.
5. Applying the EF Core migrations.
6. Verifying the resulting tables and relationships.

No manual creation of application tables is required.

## 11. Project Purpose

Focus Assistant is developed as an academic diploma project.

Its main research objective is to investigate whether computer activity patterns are associated with users' self-reported concentration and productivity.

The project combines software engineering with empirical research while recognizing that behavioral activity is not a direct measurement of attention.
