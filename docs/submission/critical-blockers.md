# Submission blockers and remaining work

Target submission: November 25, 2026. The review period supplied in the project conversation is December 2–10, 2026. Treat the submitted snapshot as final unless the institution confirms that replacements are permitted.

## P0 — before sending the submission

| Order | Item | Status | Required action and completion evidence |
| --- | --- | --- | --- |
| 1 | Provide a publicly reachable production backend | FAIL | Obtain authorization and an approved Linux/cloud target, HTTPS hostname and PostgreSQL. Configure private connectivity, verified PostgreSQL TLS, separate runtime/migration grants, secret injection and persistent protected application keys. Build/publish the reviewed image, record its digest, verify a backup, run the one-shot migration, and start API and TLS proxy. From another network check both health endpoints plus an authorized disposable workflow. Record the public URL, commit/digest, times and results. If the deadline prevents deployment, disclose the gap and provide the tested local demonstration. |
| 2 | Confirm the official rubric and submission route | NOT VERIFIED | Obtain the original criteria and weights, whether an externally reachable API and a PDF are accepted/mandatory in the intended submission, and whether the reviewer can open the repository and submission attachments. Reconcile with the matrix before final delivery. |

[Local Production verification](final-verification.md#deployment-and-runtime-scope) already covers TLS, keys, OTLP and persistence. GitHub verified the Docker image build and development Compose smoke. Docker Desktop's Linux Engine returned HTTP 500 during the baseline audit. The tested native Windows setup provides a local alternative.

## Closed in the evidence checkpoint

| Item | Status | Evidence |
| --- | --- | --- |
| Adaptive UI PDF and reviewer dossier | PASS | The [evidence index](evidence-index.md) links the six-page PDF, design system, accessibility report, test results and track entry points. |
| Representative API examples | PASS | The [API guide](../api/README.md#response-examples) links live-derived success and Problem Details examples in the synchronized contract. |

## P1 — complete if time permits before November 25

| Order | Item | Status | What to do and record |
| --- | --- | --- | --- |
| 3 | Record a short physical Windows Agent demonstration | NOT VERIFIED | Follow the [Agent guide](../agent/windows-setup.md) with an explicitly consenting disposable account. Show a browser-started session, the terminal confirmation, 15 seconds plus the final partial interval, and persisted counts and estimated states on reopen. Keep personal windows and account data outside the recording. |
| 4 | Complete the practical manual accessibility sample | PARTIAL | Check an actual phone and tablet for orientation/touch/soft keyboard and manually sample a screen reader on sign-in, navigation, dialog and chart alternatives. Record device/browser/assistive technology, results and gaps. Current automated results are for sampled routes and states. |
| 5 | Consolidate the database diagram and evidence | PARTIAL | Include the v1 addendum with the baseline ERD. If a single current ERD is required, render all 11 application tables and mark same-owner, non-cascading and aggregate-to-estimate relationships. |

## Can wait if the official rubric does not require them

- Cloud log aggregation, dashboards, alert delivery, recovery-time measurements and an actual restore exercise for a later production operating milestone.
- Production load testing, query plans with realistic scale, analytics caching and a background processing queue if measured workloads justify them.
- Swagger/Scalar UI, a Figma source and broader component examples. JSON OpenAPI and the PDF already support the corresponding workflow.
- Unity Avatar, ML/CV, hypothesis testing, microservices and external desktop distributions. The current submission demonstrates explainable focus estimates and self-reports separately.
