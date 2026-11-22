# ADR 001 — Backend topology

**Status** Accepted.

Keep one deployable API and PostgreSQL database. The application separates account,
session, telemetry, classification, analytics and preferences responsibilities.
The Windows Agent can run separately and upload through an HTTP contract.

| Option | Consequences |
|---|---|
| Modular backend | One deployment and migration job. Ownership and research consistency use one transaction. Application ports and versioned HTTP contracts preserve replacement boundaries. |
| Separate ingestion/classification service | Independent worker scaling and deployment. Requires an outbox or queue, retry and duplicate handling, a contract for consent changes, separate deployment checks and observation of eventual consistency. |
| Separate analytics service | Can isolate heavy reporting when justified. Requires replication or clear ownership of read models, backfill, freshness objectives, and consistent deletion. |

Revisit when workloads demonstrably need separate scaling, when a team owns an
independent deployable capability, or when a data isolation requirement changes the
persistence boundary. The current synchronous bounded classifier and owner-scoped
queries do not establish those conditions.
