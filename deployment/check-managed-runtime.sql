-- This query only emits a migration identifier after the minimum runtime boundary checks pass.
\set ON_ERROR_STOP on
DO $$
BEGIN
    IF current_database() <> 'focus_production' OR current_user <> 'focus_runtime'
       OR EXISTS (SELECT FROM pg_roles WHERE rolname = current_user
                  AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolbypassrls OR rolreplication))
       OR pg_has_role(current_user, 'focus_migrate', 'MEMBER')
       OR EXISTS (SELECT FROM pg_roles WHERE rolname = 'doadmin' AND pg_has_role(current_user, oid, 'MEMBER'))
       OR has_database_privilege(current_database(), 'CREATE')
       OR has_schema_privilege('public', 'CREATE')
       OR EXISTS (SELECT FROM unnest(ARRAY['INSERT','UPDATE','DELETE','TRUNCATE']) AS p(privilege)
                  WHERE has_table_privilege('public."__EFMigrationsHistory"', p.privilege))
       OR to_regclass('public."ActivityAggregates"') IS NULL
       OR EXISTS (
            SELECT FROM pg_class t JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'public' AND t.relkind IN ('r', 'p') AND t.relname <> '__EFMigrationsHistory'
            AND (t.relowner = current_user::regrole OR EXISTS (
                SELECT FROM unnest(ARRAY['SELECT','INSERT','UPDATE','DELETE']) AS p(privilege)
                WHERE NOT has_table_privilege(t.oid, p.privilege)
            ))
       ) THEN
        RAISE EXCEPTION 'Runtime database privileges do not match the checked deployment policy';
    END IF;
END $$;
SELECT max("MigrationId") FROM public."__EFMigrationsHistory";
