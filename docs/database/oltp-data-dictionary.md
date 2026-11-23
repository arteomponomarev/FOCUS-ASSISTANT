# PostgreSQL OLTP physical data dictionary

Generated from the disposable PostgreSQL 17 catalog after applying all EF migrations.
Column nullability and defaults come from the catalog. Refer to the [3NF and integrity analysis](oltp-verification.md) for domains and semantics.
UNIQUE indexes are listed even when the catalog represents them as indexes rather than named constraints.

## AccountCredentials

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `UserId` | `uuid` | No | - |
| `NormalizedEmail` | `character varying(320)` | No | - |
| `PasswordHash` | `character varying(512)` | No | - |

| Constraint | Definition |
| --- | --- |
| `FK_AccountCredentials_Users_UserId` | `FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE` |
| `PK_AccountCredentials` | `PRIMARY KEY ("UserId")` |

| Index | Definition |
| --- | --- |
| `IX_AccountCredentials_NormalizedEmail` | `CREATE UNIQUE INDEX "IX_AccountCredentials_NormalizedEmail" ON public."AccountCredentials" USING btree ("NormalizedEmail")` |
| `PK_AccountCredentials` | `CREATE UNIQUE INDEX "PK_AccountCredentials" ON public."AccountCredentials" USING btree ("UserId")` |

## AccountLogins

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `UserId` | `uuid` | No | - |
| `ExpiresAt` | `timestamp with time zone` | No | - |
| `FamilyId` | `uuid` | Yes | - |

| Constraint | Definition |
| --- | --- |
| `FK_AccountLogins_AccountCredentials_UserId` | `FOREIGN KEY ("UserId") REFERENCES "AccountCredentials"("UserId") ON DELETE CASCADE` |
| `PK_AccountLogins` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `IX_AccountLogins_UserId_ExpiresAt` | `CREATE INDEX "IX_AccountLogins_UserId_ExpiresAt" ON public."AccountLogins" USING btree ("UserId", "ExpiresAt")` |
| `IX_AccountLogins_UserId_FamilyId` | `CREATE UNIQUE INDEX "IX_AccountLogins_UserId_FamilyId" ON public."AccountLogins" USING btree ("UserId", "FamilyId")` |
| `PK_AccountLogins` | `CREATE UNIQUE INDEX "PK_AccountLogins" ON public."AccountLogins" USING btree ("Id")` |

## ActivityAggregates

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `FocusSessionId` | `uuid` | No | - |
| `StartedAt` | `timestamp with time zone` | No | - |
| `EndedAt` | `timestamp with time zone` | No | - |
| `ActiveProcessName` | `character varying(200)` | Yes | - |
| `KeyboardEventCount` | `integer` | No | - |
| `MouseClickCount` | `integer` | No | - |
| `WindowSwitchCount` | `integer` | No | - |
| `IdleSeconds` | `integer` | No | - |
| `ClientEventId` | `uuid` | Yes | - |
| `Vision` | `jsonb` | Yes | - |

| Constraint | Definition |
| --- | --- |
| `AK_ActivityAggregates_FocusSessionId_Id` | `UNIQUE ("FocusSessionId", "Id")` |
| `CK_ActivityAggregates_CameraOnly` | `CHECK ((("Vision" IS NULL) OR ((jsonb_typeof("Vision") = 'object'::text) AND ("ActiveProcessName" IS NULL) AND ("KeyboardEventCount" = 0) AND ("MouseClickCount" = 0) AND ("WindowSwitchCount" = 0) AND ("IdleSeconds" = 0))))` |
| `FK_ActivityAggregates_FocusSessions_FocusSessionId` | `FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions"("Id") ON DELETE CASCADE` |
| `PK_ActivityAggregates` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `AK_ActivityAggregates_FocusSessionId_Id` | `CREATE UNIQUE INDEX "AK_ActivityAggregates_FocusSessionId_Id" ON public."ActivityAggregates" USING btree ("FocusSessionId", "Id")` |
| `IX_ActivityAggregates_FocusSessionId_ClientEventId` | `CREATE UNIQUE INDEX "IX_ActivityAggregates_FocusSessionId_ClientEventId" ON public."ActivityAggregates" USING btree ("FocusSessionId", "ClientEventId")` |
| `IX_ActivityAggregates_FocusSessionId_StartedAt` | `CREATE INDEX "IX_ActivityAggregates_FocusSessionId_StartedAt" ON public."ActivityAggregates" USING btree ("FocusSessionId", "StartedAt")` |
| `PK_ActivityAggregates` | `CREATE UNIQUE INDEX "PK_ActivityAggregates" ON public."ActivityAggregates" USING btree ("Id")` |

## FocusSessions

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `UserId` | `uuid` | No | - |
| `PlannedDuration` | `interval` | No | - |
| `Status` | `integer` | No | - |
| `CreatedAt` | `timestamp with time zone` | No | - |
| `StartedAt` | `timestamp with time zone` | Yes | - |
| `EndedAt` | `timestamp with time zone` | Yes | - |
| `SelfReportedConcentration` | `integer` | Yes | - |
| `Reflection` | `character varying(1000)` | Yes | - |
| `SelfReportedProductivity` | `integer` | Yes | - |
| `WorkProfileId` | `uuid` | Yes | - |

| Constraint | Definition |
| --- | --- |
| `FK_FocusSessions_Users_UserId` | `FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE` |
| `FK_FocusSessions_WorkProfiles_UserId_WorkProfileId` | `FOREIGN KEY ("UserId", "WorkProfileId") REFERENCES "WorkProfiles"("UserId", "Id")` |
| `PK_FocusSessions` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `IX_FocusSessions_StartedAt` | `CREATE INDEX "IX_FocusSessions_StartedAt" ON public."FocusSessions" USING btree ("StartedAt")` |
| `IX_FocusSessions_UserId` | `CREATE INDEX "IX_FocusSessions_UserId" ON public."FocusSessions" USING btree ("UserId")` |
| `IX_FocusSessions_UserId_CreatedAt` | `CREATE INDEX "IX_FocusSessions_UserId_CreatedAt" ON public."FocusSessions" USING btree ("UserId", "CreatedAt")` |
| `IX_FocusSessions_UserId_WorkProfileId` | `CREATE INDEX "IX_FocusSessions_UserId_WorkProfileId" ON public."FocusSessions" USING btree ("UserId", "WorkProfileId")` |
| `PK_FocusSessions` | `CREATE UNIQUE INDEX "PK_FocusSessions" ON public."FocusSessions" USING btree ("Id")` |

## FocusStateRecords

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `FocusSessionId` | `uuid` | No | - |
| `StartedAt` | `timestamp with time zone` | No | - |
| `EndedAt` | `timestamp with time zone` | No | - |
| `State` | `integer` | No | - |
| `RuleVersion` | `character varying(50)` | No | - |
| `Reason` | `character varying(500)` | Yes | - |
| `ActivityAggregateId` | `uuid` | Yes | - |
| `InputProcessCategory` | `integer` | Yes | - |

| Constraint | Definition |
| --- | --- |
| `FK_FocusStateRecords_ActivityAggregates_FocusSessionId_Activit~` | `FOREIGN KEY ("FocusSessionId", "ActivityAggregateId") REFERENCES "ActivityAggregates"("FocusSessionId", "Id") ON DELETE CASCADE` |
| `FK_FocusStateRecords_FocusSessions_FocusSessionId` | `FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions"("Id") ON DELETE CASCADE` |
| `PK_FocusStateRecords` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `IX_FocusStateRecords_ActivityAggregateId` | `CREATE UNIQUE INDEX "IX_FocusStateRecords_ActivityAggregateId" ON public."FocusStateRecords" USING btree ("ActivityAggregateId")` |
| `IX_FocusStateRecords_FocusSessionId_ActivityAggregateId` | `CREATE UNIQUE INDEX "IX_FocusStateRecords_FocusSessionId_ActivityAggregateId" ON public."FocusStateRecords" USING btree ("FocusSessionId", "ActivityAggregateId")` |
| `IX_FocusStateRecords_FocusSessionId_StartedAt` | `CREATE INDEX "IX_FocusStateRecords_FocusSessionId_StartedAt" ON public."FocusStateRecords" USING btree ("FocusSessionId", "StartedAt")` |
| `PK_FocusStateRecords` | `CREATE UNIQUE INDEX "PK_FocusStateRecords" ON public."FocusStateRecords" USING btree ("Id")` |

## ProcessRules

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `WorkProfileId` | `uuid` | No | - |
| `ProcessName` | `character varying(200)` | No | - |
| `Category` | `integer` | No | - |

| Constraint | Definition |
| --- | --- |
| `FK_ProcessRules_WorkProfiles_WorkProfileId` | `FOREIGN KEY ("WorkProfileId") REFERENCES "WorkProfiles"("Id") ON DELETE CASCADE` |
| `PK_ProcessRules` | `PRIMARY KEY ("WorkProfileId", "ProcessName")` |

| Index | Definition |
| --- | --- |
| `PK_ProcessRules` | `CREATE UNIQUE INDEX "PK_ProcessRules" ON public."ProcessRules" USING btree ("WorkProfileId", "ProcessName")` |

## SessionGoals

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `FocusSessionId` | `uuid` | No | - |
| `Description` | `character varying(500)` | No | - |
| `Status` | `integer` | No | - |

| Constraint | Definition |
| --- | --- |
| `FK_SessionGoals_FocusSessions_FocusSessionId` | `FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions"("Id") ON DELETE CASCADE` |
| `PK_SessionGoals` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `IX_SessionGoals_FocusSessionId` | `CREATE INDEX "IX_SessionGoals_FocusSessionId" ON public."SessionGoals" USING btree ("FocusSessionId")` |
| `PK_SessionGoals` | `CREATE UNIQUE INDEX "PK_SessionGoals" ON public."SessionGoals" USING btree ("Id")` |

## SessionPauses

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `FocusSessionId` | `uuid` | No | - |
| `StartedAt` | `timestamp with time zone` | No | - |
| `EndedAt` | `timestamp with time zone` | Yes | - |

| Constraint | Definition |
| --- | --- |
| `CK_SessionPauses_Interval` | `CHECK ((("EndedAt" IS NULL) OR ("EndedAt" >= "StartedAt")))` |
| `FK_SessionPauses_FocusSessions_FocusSessionId` | `FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions"("Id") ON DELETE CASCADE` |
| `PK_SessionPauses` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `IX_SessionPauses_FocusSessionId` | `CREATE UNIQUE INDEX "IX_SessionPauses_FocusSessionId" ON public."SessionPauses" USING btree ("FocusSessionId") WHERE ("EndedAt" IS NULL)` |
| `IX_SessionPauses_FocusSessionId_StartedAt` | `CREATE INDEX "IX_SessionPauses_FocusSessionId_StartedAt" ON public."SessionPauses" USING btree ("FocusSessionId", "StartedAt")` |
| `PK_SessionPauses` | `CREATE UNIQUE INDEX "PK_SessionPauses" ON public."SessionPauses" USING btree ("Id")` |

## UserPreferences

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `UserId` | `uuid` | No | - |
| `FocusMinutes` | `integer` | No | - |
| `ShortBreakMinutes` | `integer` | No | - |
| `LongBreakMinutes` | `integer` | No | - |
| `IntervalsBeforeLongBreak` | `integer` | No | - |
| `Visibility` | `integer` | No | - |
| `Intensity` | `integer` | No | - |
| `AvatarId` | `character varying(80)` | Yes | - |

| Constraint | Definition |
| --- | --- |
| `FK_UserPreferences_Users_UserId` | `FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE` |
| `PK_UserPreferences` | `PRIMARY KEY ("UserId")` |

| Index | Definition |
| --- | --- |
| `PK_UserPreferences` | `CREATE UNIQUE INDEX "PK_UserPreferences" ON public."UserPreferences" USING btree ("UserId")` |

## Users

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `Email` | `character varying(320)` | No | - |
| `DisplayName` | `character varying(100)` | No | - |
| `CreatedAt` | `timestamp with time zone` | No | - |
| `TelemetryConsentAt` | `timestamp with time zone` | Yes | - |
| `CameraConsentAt` | `timestamp with time zone` | Yes | - |
| `VisionExperimental` | `boolean` | No | `false` |

| Constraint | Definition |
| --- | --- |
| `PK_Users` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `IX_Users_Email` | `CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email")` |
| `PK_Users` | `CREATE UNIQUE INDEX "PK_Users" ON public."Users" USING btree ("Id")` |

## WorkProfiles

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `Id` | `uuid` | No | - |
| `UserId` | `uuid` | No | - |
| `Name` | `character varying(100)` | No | - |
| `Description` | `character varying(500)` | Yes | - |
| `FocusMinutes` | `integer` | No | - |
| `BreakMinutes` | `integer` | No | - |

| Constraint | Definition |
| --- | --- |
| `AK_WorkProfiles_UserId_Id` | `UNIQUE ("UserId", "Id")` |
| `FK_WorkProfiles_Users_UserId` | `FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE` |
| `PK_WorkProfiles` | `PRIMARY KEY ("Id")` |

| Index | Definition |
| --- | --- |
| `AK_WorkProfiles_UserId_Id` | `CREATE UNIQUE INDEX "AK_WorkProfiles_UserId_Id" ON public."WorkProfiles" USING btree ("UserId", "Id")` |
| `PK_WorkProfiles` | `CREATE UNIQUE INDEX "PK_WorkProfiles" ON public."WorkProfiles" USING btree ("Id")` |

## __EFMigrationsHistory

| Column | PostgreSQL type | Nullable | Default |
| --- | --- | --- | --- |
| `MigrationId` | `character varying(150)` | No | - |
| `ProductVersion` | `character varying(32)` | No | - |

| Constraint | Definition |
| --- | --- |
| `PK___EFMigrationsHistory` | `PRIMARY KEY ("MigrationId")` |

| Index | Definition |
| --- | --- |
| `PK___EFMigrationsHistory` | `CREATE UNIQUE INDEX "PK___EFMigrationsHistory" ON public."__EFMigrationsHistory" USING btree ("MigrationId")` |
