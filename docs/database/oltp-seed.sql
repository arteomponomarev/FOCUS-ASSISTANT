-- Deterministic synthetic dataset for the standalone test database only.
\set ON_ERROR_STOP on
DO $$
BEGIN
    IF current_database() <> 'focus_test_task20_ddl' THEN
        RAISE EXCEPTION 'This fixture can only seed focus_test_task20_ddl';
    END IF;
END $$;

BEGIN;
INSERT INTO "Users" ("Id", "Email", "DisplayName", "CreatedAt")
SELECT md5('oltp-user:' || n)::uuid, 'oltp-seed-' || n || '@example.invalid',
       'Synthetic reviewer ' || n, TIMESTAMPTZ '2026-10-01 00:00Z'
FROM generate_series(1, 100) n ON CONFLICT DO NOTHING;

WITH fixtures AS (
    SELECT md5('oltp-session:' || owner || ':' || slot)::uuid AS id, md5('oltp-user:' || owner)::uuid AS user_id,
           TIMESTAMPTZ '2026-11-01 08:00Z' + slot * INTERVAL '3 hours' AS started
    FROM generate_series(1, 100) owner CROSS JOIN generate_series(1, 150) slot
)
INSERT INTO "FocusSessions" ("Id", "UserId", "PlannedDuration", "Status", "CreatedAt",
                            "StartedAt", "EndedAt", "SelfReportedConcentration", "SelfReportedProductivity")
SELECT id, user_id, INTERVAL '30 minutes', 2, started - INTERVAL '2 minutes',
       started, started + INTERVAL '30 minutes', 4, 3
FROM fixtures ON CONFLICT DO NOTHING;

INSERT INTO "SessionGoals" ("Id", "FocusSessionId", "Description", "Status")
SELECT md5('oltp-goal:' || s."Id")::uuid, s."Id", 'Synthetic methods review', 1
FROM "FocusSessions" s JOIN "Users" u ON u."Id" = s."UserId"
WHERE u."Email" ~ '^oltp-seed-[0-9]+@example\.invalid$' ON CONFLICT DO NOTHING;

INSERT INTO "ActivityAggregates" ("Id", "FocusSessionId", "ClientEventId", "StartedAt", "EndedAt",
    "ActiveProcessName", "KeyboardEventCount", "MouseClickCount", "WindowSwitchCount", "IdleSeconds", "Vision")
SELECT md5('oltp-aggregate:' || s."Id" || ':' || slot)::uuid, s."Id",
       md5('oltp-client:' || s."Id" || ':' || slot)::uuid, s."StartedAt" + slot * INTERVAL '15 seconds',
       s."StartedAt" + (slot + 1) * INTERVAL '15 seconds', CASE WHEN slot < 6 THEN 'editor' END,
       CASE WHEN slot < 6 THEN 12 ELSE 0 END, CASE WHEN slot < 6 THEN 3 ELSE 0 END, 0, 0,
       CASE WHEN slot = 6 THEN '{"PresentMs":11000,"NotDetectedMs":2000,"UnknownMs":2000,"FacingMs":9000,"AwayMs":1000,"ValidSamples":45}'::jsonb END
FROM "FocusSessions" s JOIN "Users" u ON u."Id" = s."UserId" CROSS JOIN generate_series(0, 6) slot
WHERE u."Email" ~ '^oltp-seed-[0-9]+@example\.invalid$' ON CONFLICT DO NOTHING;

INSERT INTO "FocusStateRecords" ("Id", "FocusSessionId", "StartedAt", "EndedAt", "State", "RuleVersion", "ActivityAggregateId")
SELECT md5('oltp-estimate:' || a."Id")::uuid, a."FocusSessionId", a."StartedAt", a."EndedAt", 1,
       'synthetic-fixture-v1', a."Id"
FROM "ActivityAggregates" a JOIN "FocusSessions" s ON s."Id" = a."FocusSessionId" JOIN "Users" u ON u."Id" = s."UserId"
WHERE a."Vision" IS NULL AND u."Email" ~ '^oltp-seed-[0-9]+@example\.invalid$' ON CONFLICT DO NOTHING;
COMMIT;
ANALYZE;
