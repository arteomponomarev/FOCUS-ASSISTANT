# Errors and retries

Errors use Problem Details with HTTP status and `traceId`. Successful responses also
expose `X-Request-Id`. Invalid request responses redact submitted values.

| Status | Typical reason | Client action |
|---|---|---|
| 400 | Invalid JSON, UUID, enum, fields, range or request type | Correct the request using the contract. |
| 401 | Missing, invalid, expired or revoked credential | Try one refresh, then reauthenticate. |
| 403 | Telemetry disabled | Stop uploading and read the privacy settings. |
| 404 | Missing or unowned resource | Discard stale IDs. |
| 409 | Invalid lifecycle state, duplicate registration, telemetry ID/content collision or overlapping interval | Re-read state. Reuse a telemetry ID only for the same item. |
| 413 | Request body exceeds server limit | Use smaller telemetry batches. |
| 415 | Unsupported request content type | Send the request as JSON. |
| 422 | Aggregate crosses a session, consent, or pause boundary, or its upload window expired | Discard incompatible or expired intervals. |
| 429 | Authentication limit reached | Back off according to `Retry-After` when present. |
| 500 | Unexpected failure | Record the trace ID. Retry only idempotent operations automatically. |
| 503 | Readiness probe failed | Wait for a healthy server. |

Example conflict body:

```json
{"title":"Conflict","status":409,"detail":"Telemetry windows cannot overlap.","traceId":"9ec8bc76e245472b9c98b844a4d02196"}
```
