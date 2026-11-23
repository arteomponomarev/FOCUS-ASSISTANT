-- Read-only catalog snapshot. Run against the disposable migrated database.
\set ON_ERROR_STOP on
SELECT jsonb_build_object(
    'tables', (SELECT jsonb_agg(t.relname ORDER BY t.relname) FROM pg_class t
        WHERE t.relnamespace = 'public'::regnamespace AND t.relkind = 'r'),
    'columns', (SELECT jsonb_agg(jsonb_build_array(t.relname, a.attname,
            format_type(a.atttypid, a.atttypmod), a.attnotnull,
            pg_get_expr(d.adbin, d.adrelid)) ORDER BY t.relname, a.attnum)
        FROM pg_class t JOIN pg_attribute a ON a.attrelid = t.oid
        LEFT JOIN pg_attrdef d ON d.adrelid = t.oid AND d.adnum = a.attnum
        WHERE t.relnamespace = 'public'::regnamespace AND t.relkind = 'r'
            AND a.attnum > 0 AND NOT a.attisdropped),
    'constraints', (SELECT jsonb_agg(jsonb_build_array(t.relname, c.conname, pg_get_constraintdef(c.oid))
            ORDER BY t.relname, c.conname)
        FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid
        WHERE t.relnamespace = 'public'::regnamespace AND c.contype IN ('p','f','u','c')),
    'indexes', (SELECT jsonb_agg(jsonb_build_array(tablename, indexname, indexdef)
            ORDER BY tablename, indexname)
        FROM pg_indexes WHERE schemaname = 'public')
);
