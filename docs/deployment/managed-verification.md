# Managed production delivery evidence

Prepared November 23, 2026. The working application baseline was `7c62bf56c3b3d922299f2d95434701ff47556577`. Local source inspection, GitHub's main ref and successful Actions results agreed. The infrastructure checkpoint is the commit containing this record. Use its exact-SHA workflow runs, rather than the latest branch badges. [Backend baseline](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions/runs/45243405863), [Frontend baseline](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions/runs/45243406025), [Vision baseline](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions/runs/45243405969), [exact-SHA runs list](https://github.com/arteomponomarev/FOCUS-ASSISTANT/actions?query=branch%3Amain).

## Reproduced preparation

| Check | Method and result | Limitation |
|---|---|---|
| Runtime configuration | Compose 2.34 rendered the managed overlay with the published Compose file and with the preserved local draft. `actionlint` and `bash -n` passed. | Local Windows Docker Engine was unavailable. The Backend CI managed-config stage validates Caddy in a sandbox and does not deploy it. |
| DB boundary configuration | `docker compose --profile migration config --format json` piped to `deployment/check-managed-connections.py`. Valid role/endpoint/CA and a quoted fixture password accepted. Swapped runtime role, weaker TLS and a public endpoint rejected. | No live provider connection. Runtime network/TLS policy is audited once provisioned. |
| PostgreSQL roles and migrations | PostgreSQL 17 fixture at loopback port 55454, separate non-superuser provider-like administrator and dedicated migration/runtime roles. `bootstrap-managed.sql`, Release `Focus.Api.dll --migrate`, `grant-managed-runtime.sql` and `check-managed-runtime.sql` passed. Eight migrations applied through `20261123161834_AddCameraObservation`. A real runtime DDL attempt and a history UPDATE were denied. Runtime could read all migration history. | Tests did not exercise the actual DigitalOcean administrator or managed certificates. |
| End-to-end and restart | `scripts/smoke-managed.py` prepare and verify called by a loopback HTTPS test harness with an ephemeral trusted test certificate. The application used the restricted runtime DB role. Synthetic keyboard and CV aggregates, replay, goal, analytics and persisted readback passed across an API restart. The disposable smoke account was removed. The existing local smoke also confirmed a previously issued bearer credential and OTLP after restart. | TLS used a local test certificate and local PostgreSQL. No public hostname or managed database was contacted. |
| Published image and CD | Quality gates for exact source SHA, immutable GHCR receipt validation, pinned SSH host key, migration permissions, public HTTP-200 checks and an artifact are configured. | No approved provisioned environment, GHCR publication run, digest, public HTTPS check, or cloud CD execution is recorded. |

Developer and DPI/CV verification for the unchanged application remain in the [previous release report](../release/2026-11-23-verification.md). Raw logs and fixture outputs remain in the ignored local artifact directory. The database procedure was executed only against disposable data.

## Requirements trace

| Requirement | Status | Evidence / remaining proof |
|---|---|---|
| Backend production accessibility (B-M09) | FAIL | Requires public HTTPS with an actual external request. |
| Backend reproducible deployment (B-M16) | PARTIAL | Versioned Docker/Compose and managed runbook, restricted roles and local restart test. Repeat on the approved host and record the rollout. |
| CI/CD publication and delivery (CI-M11, CI-M15, CI-M16) | PARTIAL | Public Actions already generated test artifacts and built a container. GHCR publication and the managed production rollout need separate manual approvals and successful runs. |
| Deployment gate and health (CI-M19, CI-M20) | PARTIAL | Exact SHA and artifact guards configured. Local smoke and existing CI health checks pass. Need run evidence for the external workflow. |
| Actual cloud and publicly accessible service (C-M01, C-M02, C-M08) | FAIL | No approved cloud service or actual public URL in release evidence. |
| Genuine managed service (C-M04) | FAIL | Target is provider-operated DigitalOcean Standard PostgreSQL with documented backups. Provisioning and verified connection are pending owner decision. |
| Secrets, network and cost (C-M05, C-M07, C-M09) | PARTIAL | Host secret sealing, role checks, intended VPC, cost estimate and cleanup documented. Verify actual configuration in the owner account. |

## External record to complete after approval

- Provider region and service types, database major version and Trusted Sources, sanitized role check and migration ID.
- DNS hostname, TLS certificate validation, exact SHA and immutable registry digest, publication and deployment run IDs plus timestamps.
- Public `/health/live` and `/health/ready`, public browser/API smoke, the API container's before/after restart timestamps and persistent fixture readback, then deletion of only the synthetic account.
- Backup retention, separate recovery rehearsal, monthly estimate, operational monitoring and cleanup owner. Keep secrets, certificate private keys, raw logs and telemetry out of the public evidence.

Commands and approval boundaries are in the [managed cloud runbook](managed-cloud.md).
