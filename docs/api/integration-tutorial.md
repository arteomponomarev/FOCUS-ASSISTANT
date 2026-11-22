# Integration tutorial

For actual Windows collection, see the [Windows Agent guide](../agent/windows-setup.md).
The Agent checks account privacy and the owned session timeline before recording.
It sends pending items through the existing activity batch operation. Each item UUID
survives retry and restart. The API generates each estimate in the same transaction
and accepts new records only within 24 hours of their interval end. A late partial
aggregate must still fall inside the recorded session and consent boundaries.

Start the development API as described in [Getting started](getting-started.md).
Use a disposable environment to run the included PowerShell script. Set
`FOCUS_DEMO_PASSWORD` to a synthetic passphrase of at least 12 characters. It will be
kept in the process environment and will not be printed.

```powershell
./scripts/demo-backend.ps1 -BaseUri http://127.0.0.1:8080
```

The script exercises the real API:

1. Register a unique example account and exchange its sign-in credentials for a token.
2. Explicitly enable telemetry consent.
3. Create a planned session and add a goal.
4. Start the session and send a completed activity interval.
5. Retry the same client event ID and assert that it is reported as a duplicate.
6. Complete the session with separate concentration and productivity ratings and a goal outcome.
7. Read session details, history, estimates, timeline, and analytics.
8. Revoke the credentials by logging out.

The activity input is a synthetic fixture. Output derived from that fixture can validate
the workflow and cannot serve as research evidence. In a real deployment, upload only
privacy-minimized aggregates captured by the Agent with consent.

Telemetry requests accept 1–100 records. Generate each UUID once at capture time and
retain it across retries. IDs are scoped to a session. Reusing an ID with different
normalized data returns 409. Every batch is atomic with its classifications.
Use UTC timestamps and omit process name when it is unavailable or inappropriate.
Choose capture windows that fit entirely within the session and within active time
or a single recorded pause. See [research definitions](../architecture/research-metrics.md).
