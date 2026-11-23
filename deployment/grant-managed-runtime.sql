-- Run after successful EF migration as focus_migrate. The runtime never owns the schema.
\set ON_ERROR_STOP on
BEGIN;
DO $$
DECLARE tbl record;
BEGIN
    IF current_database() <> 'focus_production' OR current_user <> 'focus_migrate'
       OR to_regclass('public."__EFMigrationsHistory"') IS NULL THEN
        RAISE EXCEPTION 'Expected migration login and initialized Focus Assistant database';
    END IF;

    REVOKE ALL ON TABLE public."__EFMigrationsHistory" FROM PUBLIC, focus_runtime;
    GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO focus_runtime;
    FOR tbl IN SELECT tablename FROM pg_tables WHERE schemaname = 'public'
        AND tableowner = current_user AND tablename <> '__EFMigrationsHistory'
    LOOP
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON public.%I TO focus_runtime', tbl.tablename);
    END LOOP;
END $$;
COMMIT;
