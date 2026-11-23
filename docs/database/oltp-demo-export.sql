-- Publish only coarse metrics for known synthetic seed users. No personal data is read.
\set ON_ERROR_STOP on
DO $$
BEGIN
    IF current_database() <> 'focus_test_task20_ddl' THEN
        RAISE EXCEPTION 'This export requires the isolated OLTP fixture';
    END IF;
END $$;
COPY (
    SELECT date_trunc('day', s."StartedAt" AT TIME ZONE 'UTC')::date AS day_utc,
           count(*) AS completed_sessions, avg(s."SelfReportedConcentration") AS mean_reported_concentration,
           avg(s."SelfReportedProductivity") AS mean_reported_productivity
    FROM "FocusSessions" s JOIN "Users" u ON u."Id" = s."UserId"
    WHERE s."Status" = 2 AND u."Email" ~ '^oltp-seed-[0-9]+@example\.invalid$'
    GROUP BY 1 ORDER BY 1
) TO STDOUT WITH CSV HEADER;
