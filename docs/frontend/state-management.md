# Frontend state ownership

| State | Owner | Lifetime |
| --- | --- | --- |
| API records and read errors | TanStack Query | Short-lived cache, scoped to current browser identity |
| Session credentials | HTTP transport | Memory only, until logout, revocation or page reload |
| Filters and pagination | URL search parameters for history, page state elsewhere | Shareable URLs for history, mounted route for other filters |
| Inputs and validation | React Hook Form or native forms | Until submission or unmount |
| Modal visibility and running timer | Feature component | Local interaction only |
| Theme | Context and one localStorage preference | Persists across reloads |

Query keys include route IDs and input filters. Mutations reconcile the affected record and invalidate dependent lists and analytics. Authentication generation changes cancel and clear the previous account's cache. Route errors and root render failures show recoverable UI without logging payloads.
