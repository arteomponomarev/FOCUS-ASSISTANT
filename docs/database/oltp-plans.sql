-- Mirrors the important predicates/orderings in SessionStore, ResearchStore and AnalyticsStore.
\set ON_ERROR_STOP on
SELECT md5('oltp-user:42')::uuid AS owner, md5('oltp-session:42:140')::uuid AS session \gset

\echo Session history (current indexes)
EXPLAIN (ANALYZE, BUFFERS)
SELECT "Id", "Status", "StartedAt" FROM "FocusSessions"
WHERE "UserId" = :'owner' AND coalesce("StartedAt", "CreatedAt") >= TIMESTAMPTZ '2026-11-05Z'
ORDER BY coalesce("StartedAt", "CreatedAt") DESC, "Id" DESC LIMIT 25;

\echo Goals at owned session detail
EXPLAIN (ANALYZE, BUFFERS)
SELECT g."Id", g."Status" FROM "SessionGoals" g JOIN "FocusSessions" s ON s."Id" = g."FocusSessionId"
WHERE s."UserId" = :'owner' AND s."Id" = :'session';

\echo Owned desktop aggregate page
EXPLAIN (ANALYZE, BUFFERS)
SELECT a."Id", a."KeyboardEventCount", a."StartedAt" FROM "ActivityAggregates" a
JOIN "FocusSessions" s ON s."Id" = a."FocusSessionId"
WHERE s."UserId" = :'owner' AND a."FocusSessionId" = :'session' AND a."Vision" IS NULL
ORDER BY a."StartedAt", a."Id" LIMIT 25;

\echo Owned CV observation totals input
EXPLAIN (ANALYZE, BUFFERS)
SELECT a."Vision" FROM "ActivityAggregates" a JOIN "FocusSessions" s ON s."Id" = a."FocusSessionId"
WHERE s."UserId" = :'owner' AND a."FocusSessionId" = :'session' AND a."Vision" IS NOT NULL;

\echo Observed analytics range
EXPLAIN (ANALYZE, BUFFERS)
SELECT a."FocusSessionId", a."StartedAt", a."KeyboardEventCount" FROM "ActivityAggregates" a
JOIN "FocusSessions" s ON s."Id" = a."FocusSessionId"
WHERE s."UserId" = :'owner' AND a."Vision" IS NULL
  AND a."StartedAt" >= TIMESTAMPTZ '2026-11-05Z' AND a."StartedAt" < TIMESTAMPTZ '2026-11-20Z';

\echo Candidate expression index (test-only; rolled back)
BEGIN;
DO $$ BEGIN
    IF current_database() <> 'focus_test_task20_ddl' THEN
        RAISE EXCEPTION 'Candidate index probe requires focus_test_task20_ddl';
    END IF;
END $$;
CREATE INDEX task20_history_candidate ON "FocusSessions" ("UserId", coalesce("StartedAt", "CreatedAt"), "Id");
EXPLAIN (ANALYZE, BUFFERS)
SELECT "Id", "Status", "StartedAt" FROM "FocusSessions"
WHERE "UserId" = :'owner' AND coalesce("StartedAt", "CreatedAt") >= TIMESTAMPTZ '2026-11-05Z'
ORDER BY coalesce("StartedAt", "CreatedAt") DESC, "Id" DESC LIMIT 25;
ROLLBACK;
