# Backend API changelog

## v1 verification

- Added captured, sanitized success and Problem Details response examples to OpenAPI. Documented the optional `traceId` already returned by the API. Paths, DTOs and runtime behavior are unchanged.
- Reject duplicate JSON object properties with a validation error.
- Document HTTP 415 responses for JSON request bodies.

## v1

- Introduced account registration, bearer authentication, refresh rotation, and revocation.
- Added owner-scoped sessions and goals, including pause and resume.
- Added consent-gated atomic telemetry ingestion and versioned focus estimates.
- Added timeline, dashboard reports, calendar buckets, and per-session research summaries.
- Added work profiles, account preferences, and the pageable privacy export workflow.
- Added OpenAPI at `/openapi/v1.json` in Development and Contract.
