#!/usr/bin/env bash
# Install root-owned under /opt/focus. The restricted SSH command supplies two lines on stdin.
set -euo pipefail
umask 077
export PATH=/usr/sbin:/usr/bin:/sbin:/bin
[[ $# == 0 && -z ${SSH_ORIGINAL_COMMAND:-} ]]
IFS= read -r source_sha
IFS= read -r image
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]]
[[ "$image" =~ ^ghcr\.io/arteomponomarev/focus-assistant-api@sha256:[0-9a-f]{64}$ ]]
if IFS= read -r extra || [[ -n "$extra" ]]; then
    echo 'Unexpected deployment input.' >&2
    exit 1
fi

exec 9>/run/lock/focus-managed-deploy.lock
flock -n 9
temp=$(mktemp -d /run/focus-deploy.XXXXXXXX)
trap 'rm -f -- "$temp/env" "$temp/pg_service.conf" "$temp/pgpass"; rmdir -- "$temp"' EXIT
for credential in env pg_service.conf pgpass; do
    systemd-creds decrypt --name="focus-$credential" \
        "/etc/credstore.encrypted/focus-$credential" "$temp/$credential"
    chmod 600 "$temp/$credential"
done
export PGSERVICEFILE="$temp/pg_service.conf" PGPASSFILE="$temp/pgpass"
export FOCUS_API_IMAGE="$image"

compose=(docker compose --project-directory /opt/focus --env-file "$temp/env" \
    -f /opt/focus/compose.production.yaml -f /opt/focus/compose.production.managed.yaml)
"${compose[@]}" --profile migration config --quiet
"${compose[@]}" --profile migration config --format json \
    | python3 -B /opt/focus/deployment/check-managed-connections.py "$PGSERVICEFILE"
"${compose[@]}" pull api proxy
revision=$(docker image inspect --format '{{index .Config.Labels "org.opencontainers.image.revision"}}' "$image")
[[ "$revision" == "$source_sha" ]]

# TLS, role and database checks precede application changes. Logged output is deliberately generic.
for role in migrate runtime; do
    verified=$(PGSERVICE="focus-$role" psql -XqAt --no-password -v ON_ERROR_STOP=1 \
        -c "SELECT current_user = 'focus_$role' AND current_database() = 'focus_production'
            AND EXISTS (SELECT FROM pg_stat_ssl WHERE pid = pg_backend_pid() AND ssl)" 2>/dev/null) || {
        echo 'Private database connection check failed.' >&2
        exit 1
    }
    [[ "$verified" == t ]]
done
if ! "${compose[@]}" --profile migration run --rm migrate >/dev/null 2>&1; then
    echo 'Migration failed. Inspect privately on the host; public rollout has stopped.' >&2
    exit 1
fi
if ! PGSERVICE=focus-migrate psql -Xq --no-password -f /opt/focus/deployment/grant-managed-runtime.sql >/dev/null 2>&1; then
    echo 'Migration-role grants failed; public rollout has stopped.' >&2
    exit 1
fi
migration=$(PGSERVICE=focus-runtime psql -XqAt --no-password \
    -f /opt/focus/deployment/check-managed-runtime.sql 2>/dev/null) || {
    echo 'Runtime privilege check failed; public rollout has stopped.' >&2
    exit 1
}
[[ "$migration" =~ ^[0-9]{14}_[[:alnum:]_]+$ ]]
"${compose[@]}" up -d --wait --wait-timeout 120 api proxy
printf 'source_sha=%s\nimage=%s\nmigration=%s\nrolled_out_utc=%s\n' \
    "$source_sha" "$image" "$migration" "$(date -u +%FT%TZ)"
