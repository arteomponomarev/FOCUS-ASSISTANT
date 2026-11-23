-- Run on the dedicated database as its provider administrator after creating the two login roles.
\set ON_ERROR_STOP on
DO $$
BEGIN
    IF current_database() <> 'focus_production'
       OR to_regrole('focus_migrate') IS NULL OR to_regrole('focus_runtime') IS NULL
       OR EXISTS (SELECT FROM pg_roles WHERE rolname IN ('focus_migrate', 'focus_runtime')
                  AND (rolsuper OR rolbypassrls OR rolreplication OR NOT rolcanlogin))
       OR pg_has_role('focus_runtime', 'focus_migrate', 'MEMBER')
       OR EXISTS (SELECT FROM pg_roles WHERE rolname = 'doadmin'
                  AND (pg_has_role('focus_runtime', oid, 'MEMBER') OR pg_has_role('focus_migrate', oid, 'MEMBER')))
       OR EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public') THEN
        RAISE EXCEPTION 'Expected a new dedicated database and two independent non-admin login roles';
    END IF;
END $$;

BEGIN;
DO $$
DECLARE role_info record;
BEGIN
    FOR role_info IN SELECT rolname, rolcreatedb, rolcreaterole FROM pg_roles
        WHERE rolname IN ('focus_migrate', 'focus_runtime')
    LOOP
        IF role_info.rolcreatedb THEN
            EXECUTE format('ALTER ROLE %I NOCREATEDB', role_info.rolname);
        END IF;
        IF role_info.rolcreaterole THEN
            EXECUTE format('ALTER ROLE %I NOCREATEROLE', role_info.rolname);
        END IF;
    END LOOP;
END $$;
REVOKE ALL ON DATABASE focus_production FROM PUBLIC, focus_runtime, focus_migrate;
GRANT CONNECT ON DATABASE focus_production TO focus_migrate, focus_runtime;
REVOKE ALL ON SCHEMA public FROM PUBLIC, focus_runtime, focus_migrate;
GRANT USAGE, CREATE ON SCHEMA public TO focus_migrate;
GRANT USAGE ON SCHEMA public TO focus_runtime;
COMMIT;
