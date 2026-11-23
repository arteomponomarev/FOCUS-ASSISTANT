CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(320) NOT NULL,
        "DisplayName" character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE TABLE "FocusSessions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PlannedDuration" interval NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "StartedAt" timestamp with time zone,
        "EndedAt" timestamp with time zone,
        "SelfReportedConcentration" integer,
        "Reflection" character varying(1000),
        CONSTRAINT "PK_FocusSessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FocusSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE TABLE "SessionGoals" (
        "Id" uuid NOT NULL,
        "FocusSessionId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        CONSTRAINT "PK_SessionGoals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SessionGoals_FocusSessions_FocusSessionId" FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE INDEX "IX_FocusSessions_StartedAt" ON "FocusSessions" ("StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE INDEX "IX_FocusSessions_UserId" ON "FocusSessions" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE INDEX "IX_SessionGoals_FocusSessionId" ON "SessionGoals" ("FocusSessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261018200509_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261018200509_InitialCreate', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261115233249_AddTelemetryAndResearchData') THEN
    ALTER TABLE "FocusSessions" ADD "SelfReportedProductivity" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261115233249_AddTelemetryAndResearchData') THEN
    CREATE TABLE "ActivityAggregates" (
        "Id" uuid NOT NULL,
        "FocusSessionId" uuid NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone NOT NULL,
        "ActiveProcessName" character varying(200),
        "KeyboardEventCount" integer NOT NULL,
        "MouseClickCount" integer NOT NULL,
        "WindowSwitchCount" integer NOT NULL,
        "IdleSeconds" integer NOT NULL,
        CONSTRAINT "PK_ActivityAggregates" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ActivityAggregates_FocusSessions_FocusSessionId" FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261115233249_AddTelemetryAndResearchData') THEN
    CREATE TABLE "FocusStateRecords" (
        "Id" uuid NOT NULL,
        "FocusSessionId" uuid NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone NOT NULL,
        "State" integer NOT NULL,
        "RuleVersion" character varying(50) NOT NULL,
        "Reason" character varying(500),
        CONSTRAINT "PK_FocusStateRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FocusStateRecords_FocusSessions_FocusSessionId" FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261115233249_AddTelemetryAndResearchData') THEN
    CREATE INDEX "IX_ActivityAggregates_FocusSessionId_StartedAt" ON "ActivityAggregates" ("FocusSessionId", "StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261115233249_AddTelemetryAndResearchData') THEN
    CREATE INDEX "IX_FocusStateRecords_FocusSessionId_StartedAt" ON "FocusStateRecords" ("FocusSessionId", "StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261115233249_AddTelemetryAndResearchData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261115233249_AddTelemetryAndResearchData', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120202_AddAccountAuthentication') THEN
    CREATE TABLE "AccountCredentials" (
        "UserId" uuid NOT NULL,
        "NormalizedEmail" character varying(320) NOT NULL,
        "PasswordHash" character varying(512) NOT NULL,
        CONSTRAINT "PK_AccountCredentials" PRIMARY KEY ("UserId"),
        CONSTRAINT "FK_AccountCredentials_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120202_AddAccountAuthentication') THEN
    CREATE TABLE "AccountLogins" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AccountLogins" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccountLogins_AccountCredentials_UserId" FOREIGN KEY ("UserId") REFERENCES "AccountCredentials" ("UserId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120202_AddAccountAuthentication') THEN
    CREATE UNIQUE INDEX "IX_AccountCredentials_NormalizedEmail" ON "AccountCredentials" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120202_AddAccountAuthentication') THEN
    CREATE INDEX "IX_AccountLogins_UserId_ExpiresAt" ON "AccountLogins" ("UserId", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120202_AddAccountAuthentication') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261122120202_AddAccountAuthentication', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120453_AddSessionLifecycle') THEN
    CREATE TABLE "SessionPauses" (
        "Id" uuid NOT NULL,
        "FocusSessionId" uuid NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone,
        CONSTRAINT "PK_SessionPauses" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_SessionPauses_Interval" CHECK ( "EndedAt" IS NULL OR "EndedAt" >= "StartedAt" ),
        CONSTRAINT "FK_SessionPauses_FocusSessions_FocusSessionId" FOREIGN KEY ("FocusSessionId") REFERENCES "FocusSessions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120453_AddSessionLifecycle') THEN
    CREATE INDEX "IX_FocusSessions_UserId_CreatedAt" ON "FocusSessions" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120453_AddSessionLifecycle') THEN
    CREATE UNIQUE INDEX "IX_SessionPauses_FocusSessionId" ON "SessionPauses" ("FocusSessionId") WHERE  "EndedAt" IS NULL ;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120453_AddSessionLifecycle') THEN
    CREATE INDEX "IX_SessionPauses_FocusSessionId_StartedAt" ON "SessionPauses" ("FocusSessionId", "StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120453_AddSessionLifecycle') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261122120453_AddSessionLifecycle', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    ALTER TABLE "Users" ADD "TelemetryConsentAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    ALTER TABLE "FocusStateRecords" ADD "ActivityAggregateId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    ALTER TABLE "ActivityAggregates" ADD "ClientEventId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    ALTER TABLE "ActivityAggregates" ADD CONSTRAINT "AK_ActivityAggregates_FocusSessionId_Id" UNIQUE ("FocusSessionId", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    CREATE UNIQUE INDEX "IX_FocusStateRecords_ActivityAggregateId" ON "FocusStateRecords" ("ActivityAggregateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    CREATE UNIQUE INDEX "IX_FocusStateRecords_FocusSessionId_ActivityAggregateId" ON "FocusStateRecords" ("FocusSessionId", "ActivityAggregateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    CREATE UNIQUE INDEX "IX_ActivityAggregates_FocusSessionId_ClientEventId" ON "ActivityAggregates" ("FocusSessionId", "ClientEventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    ALTER TABLE "FocusStateRecords" ADD CONSTRAINT "FK_FocusStateRecords_ActivityAggregates_FocusSessionId_Activit~" FOREIGN KEY ("FocusSessionId", "ActivityAggregateId") REFERENCES "ActivityAggregates" ("FocusSessionId", "Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122120945_AddTelemetryConsentAndIdempotency') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261122120945_AddTelemetryConsentAndIdempotency', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    ALTER TABLE "FocusStateRecords" ADD "InputProcessCategory" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    ALTER TABLE "FocusSessions" ADD "WorkProfileId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    CREATE TABLE "UserPreferences" (
        "UserId" uuid NOT NULL,
        "FocusMinutes" integer NOT NULL,
        "ShortBreakMinutes" integer NOT NULL,
        "LongBreakMinutes" integer NOT NULL,
        "IntervalsBeforeLongBreak" integer NOT NULL,
        "Visibility" integer NOT NULL,
        "Intensity" integer NOT NULL,
        "AvatarId" character varying(80),
        CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("UserId"),
        CONSTRAINT "FK_UserPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    CREATE TABLE "WorkProfiles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(500),
        "FocusMinutes" integer NOT NULL,
        "BreakMinutes" integer NOT NULL,
        CONSTRAINT "PK_WorkProfiles" PRIMARY KEY ("Id"),
        CONSTRAINT "AK_WorkProfiles_UserId_Id" UNIQUE ("UserId", "Id"),
        CONSTRAINT "FK_WorkProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    CREATE TABLE "ProcessRules" (
        "WorkProfileId" uuid NOT NULL,
        "ProcessName" character varying(200) NOT NULL,
        "Category" integer NOT NULL,
        CONSTRAINT "PK_ProcessRules" PRIMARY KEY ("WorkProfileId", "ProcessName"),
        CONSTRAINT "FK_ProcessRules_WorkProfiles_WorkProfileId" FOREIGN KEY ("WorkProfileId") REFERENCES "WorkProfiles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    CREATE INDEX "IX_FocusSessions_UserId_WorkProfileId" ON "FocusSessions" ("UserId", "WorkProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    ALTER TABLE "FocusSessions" ADD CONSTRAINT "FK_FocusSessions_WorkProfiles_UserId_WorkProfileId" FOREIGN KEY ("UserId", "WorkProfileId") REFERENCES "WorkProfiles" ("UserId", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122121319_AddWorkProfilesAndPreferences') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261122121319_AddWorkProfilesAndPreferences', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122123708_TrackLoginFamily') THEN
    ALTER TABLE "AccountLogins" ADD "FamilyId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122123708_TrackLoginFamily') THEN
    CREATE UNIQUE INDEX "IX_AccountLogins_UserId_FamilyId" ON "AccountLogins" ("UserId", "FamilyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261122123708_TrackLoginFamily') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261122123708_TrackLoginFamily', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261123161834_AddCameraObservation') THEN
    ALTER TABLE "Users" ADD "CameraConsentAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261123161834_AddCameraObservation') THEN
    ALTER TABLE "Users" ADD "VisionExperimental" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261123161834_AddCameraObservation') THEN
    ALTER TABLE "ActivityAggregates" ADD "Vision" jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261123161834_AddCameraObservation') THEN
    ALTER TABLE "ActivityAggregates" ADD CONSTRAINT "CK_ActivityAggregates_CameraOnly" CHECK ( "Vision" IS NULL OR (jsonb_typeof("Vision") = 'object' AND "ActiveProcessName" IS NULL AND "KeyboardEventCount" = 0 AND "MouseClickCount" = 0 AND "WindowSwitchCount" = 0 AND "IdleSeconds" = 0) );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261123161834_AddCameraObservation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261123161834_AddCameraObservation', '10.0.13');
    END IF;
END $EF$;
COMMIT;
