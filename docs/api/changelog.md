# Backend API changelog

## Diploma contract evidence

- Embedded operation, parameter, model and field descriptions across all 42 routes.
- Added redacted examples captured against a disposable database and checked
  every published example with JSON Schema 2020-12.
- Corrected the documented planned-goal PATCH shape to allow an omitted outcome
  and removed a spurious registration 401. Session and token semantics remain v1.

## Computer Vision

- Privacy settings now support independent camera consent and optional experimental evaluation.
- The existing activity batch accepts camera-only aggregate intervals under camera consent.
- Added an owned session vision report and optional camera-assisted current-state estimates.
- Browser logout can request camera consent revocation through `stopCamera=true`.

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
