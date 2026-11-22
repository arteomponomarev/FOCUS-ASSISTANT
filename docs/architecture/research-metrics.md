# Research definitions

Observed aggregate input, inferred focus state, self-reports, and goal outcomes are separate.
Treat every engine state as an estimate from the recorded inputs.

## Classifier rules-1.0.0

The engine computes idle ratio as `idleSeconds / durationSeconds` and switch rate as
`windowSwitchCount * 60 / durationSeconds`. Input event counts can establish that input
occurred. Their magnitude is not a productivity score. Process category comes from
the optional work profile. An unmatched process is Neutral.

The first matching rule wins.

1. **Break** for a window fully inside a recorded pause.
2. **Absent** for a window of at least 120 seconds with idle ratio at least 0.98, zero
   input and zero switches. Physical presence is unknown.
3. **Idle** when idle ratio is at least 0.75.
4. **Distracted** when the profile flags the process, switch rate is at least 6/min,
   and idle ratio is below 0.50.
5. **Focused** when the profile allows the process, input is observed, switch rate
   is at most 2/min, and idle ratio is at most 0.25.
6. **Neutral** when the information does not support a stronger heuristic.

Each new estimate stores the input aggregate ID, effective process category, rule
version, reason and interval. The evaluation is deterministic. Rules have not been
validated as psychological measurements.

## Duration, windows and reporting

- Use UTC storage and half-open intervals `[start,end)`. Input timestamps are normalized
  to PostgreSQL microseconds.
- Elapsed time begins at session start. Active time is elapsed time minus pauses.
  Planned and unstarted cancelled sessions contribute no tracked active time.
- Completing a paused session ends its open pause at completion.
- Sessions, ratings and goals are attributed to the bucket containing the session's
  start, or creation if unstarted. Active and estimated durations are clipped at the
  bucket boundaries.
- Calendar statistics use IANA time zones. Weeks start Monday. DST days may differ
  from 24 hours. Separate sessions contribute their own durations independently.
- Observed counters and idle seconds belong to the bucket containing their window
  start. Counters are integral and are not prorated across midnight.
- Missing estimates do not contribute any duration. For overlapping estimates in
  legacy data, the earliest-starting record owns the overlap. Record ID breaks start
  ties. Invalid intervals are excluded.
- Concentration and productivity are optional in legacy rows and required separately
  for new HTTP completions. Averages omit missing reports and include sample counts.
- A completed goal contributes 1, a partially completed goal contributes 0.5, and
  other outcomes contribute zero. Divide by all goals, with 0% for no goals.
- Per-session research rows expose metrics suitable for subsequent analysis. No
  correlations or scientific inference are computed by the API.

## Privacy and data lifecycle

Uploading requires explicit per-account consent. Withdrawing it immediately
blocks future ingestion and retains previously accepted observations until deletion.
The backend accepts no raw input, window title, clipboard, browser content, screenshot
or webcam fields. The Agent installs its collectors after confirming the selected
session is active and account consent is enabled. It pauses when fresh authorization
is unavailable. The local synchronized queue uses Windows DPAPI and contains
aggregate fields only. Legacy JSONL is never read by the uploader. Files written
by older builds can still contain titles and require a local retention decision.
[Windows Agent privacy and retention](../agent/windows-setup.md#privacy)

The privacy export is pageable. Read account preferences from the export entry point,
then follow work profile and session paths. Under each session, retrieve goal details,
activity, estimates, and timeline as needed. This produces a client-collected export.
Deletion requires the account password and removes the account's live database rows
through foreign-key cascades. Backups and legacy local Agent files have operator or
device-specific retention. Establish consent wording, access control and retention with
the deployment owner before collecting research data.
