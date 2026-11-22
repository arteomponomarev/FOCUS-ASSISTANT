# Backend deployment

## Build and local Compose

Install the SDK from `global.json`, Docker Engine with Compose v2, and PostgreSQL
when running natively. Review [testing](../testing/backend.md) before publishing.

```powershell
dotnet restore "FOCUS ASSISTANT.slnx" --locked-mode
dotnet publish Focus.Api -c Release --no-restore -o artifacts/publish
docker build -t focus-assistant-api:local .
```

Set `FOCUS_POSTGRES_PASSWORD` to a locally generated high-entropy alphanumeric value
using your shell or a private environment file. Runtime environment variables are visible
to administrators of the host and container engine.

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps -a
Invoke-RestMethod http://127.0.0.1:8080/health/ready
```

The one-shot `migrate` service runs before the API. The database is reachable within the
Compose network. The API publishes a loopback port. PostgreSQL and the Data Protection
key ring use named volumes. Keep those volumes when replacing containers. See the
[integration tutorial](../api/integration-tutorial.md) for the first authorized workflow.

## Production

The [production Compose file](../../compose.production.yaml) targets an approved Linux
Docker host with external PostgreSQL and Caddy TLS. Reserve a domain, configure DNS,
confirm ports 80 and 443, and plan backups before launching. If the private subnet
changes, update the proxy's IP and the API's trusted proxy setting together.

| Input | Purpose |
|---|---|
| `FOCUS_API_IMAGE` | Reviewed image reference pinned to an immutable digest. |
| `FOCUS_PUBLIC_HOSTNAME` | Approved DNS hostname for HTTPS. |
| `FOCUS_DB_CONNECTION` | PostgreSQL runtime role with data access and migration history read access. |
| `FOCUS_DB_MIGRATION_CONNECTION` | Separate DDL-capable migration role, injected for the migration job. |

Use PostgreSQL TLS with certificate verification across host boundaries. Restrict database
reachability to the application and approved operations clients. Manage the runtime and
migration connection strings in an approved secret store. Restrict access to the private
Data Protection volume and its backups. In a managed deployment, use equivalent protected
persistent storage and narrowly scoped service identities. Configure `Http__AllowedOrigins__N`
for a separately hosted frontend. Production requires HTTPS and accepts forwarded scheme
and client IP from explicitly configured proxies.

To encrypt stored Data Protection keys with an operator-provisioned PKCS#12 certificate,
mount it privately and set `Authentication__KeyEncryptionCertificatePath` and
`Authentication__KeyEncryptionCertificatePassword` through the deployment configuration.
Keep old decryption material accessible throughout any key migration.

With credentials loaded and a verified backup:

```powershell
docker compose -f compose.production.yaml config --quiet
docker compose -f compose.production.yaml --profile migration run --rm migrate
docker compose -f compose.production.yaml up -d api proxy
```

Verify HTTPS from an external network and confirm both health endpoints. Use an isolated
synthetic workflow to check authentication, ownership, and correlation. Record the
actual URL, image digest, migration status, and smoke-test results. The repository has
no configured production target.

If migration fails, stop the rollout and keep the existing service when the schema
is still compatible. Use a reviewed forward fix or an approved backup restore.
Historical down migrations may destroy data.

## Hosting options

- The included Compose files target a Linux host with Docker, DNS, and private
  managed PostgreSQL. This gives control over persistent keys and migration jobs.
- [Azure Container Apps networking](https://learn.microsoft.com/en-us/azure/container-apps/networking)
  provides TLS ingress. An Azure deployment needs private database access, persistent
  application keys, and a scoped migration identity.
- [Render Docker web services](https://render.com/docs/docker) support container images.
  Review [free-tier limitations](https://render.com/docs/free) and key persistence.
  A temporary demo and a durable deployment have different availability requirements.

Select a provider and authorize credentials before creating resources. The
[registry workflow](../../.github/workflows/publish-backend.yml) is manual and
environment-scoped. Configure environment reviewers before enabling it.

The [deployment workflow](../../.github/workflows/deploy-backend.yml) requires a
reviewed, dedicated Linux runner labeled `focus-production`, the protected
`production` environment, environment secrets and a pull grant for the image.
Dispatch with an immutable image digest after confirming backup and upgrade readiness.
Independently verify the deployed URL from the reviewer's network.
