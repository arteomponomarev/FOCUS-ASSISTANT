# Camera privacy

Camera processing defaults to off. The account's Camera consent, the local
`--vision` switch, the terminal confirmation `CAMERA`, a fresh authorization,
and an active owned session are required together. Keyboard/mouse consent
and its local opt-in are independent. Windows permissions apply to probing,
calibration and session observations.

The camera is visible through the Windows device indicator when available.
The Agent console and the Unity panel show when the worker is processing.
The website shows the saved consent and received observation totals.
Revocation blocks uploads immediately at the API. The running Agent normally
reads the change on its next authority check and expires camera authority
after three seconds without a fresh check. Ctrl+C directly stops the local
Agent. Signing out from the web client requests account camera-consent
revocation before the browser login is revoked. Revoking the Agent's login
causes it to close the worker on the next authorization failure.

Frames, crops, landmarks, inferred pose angles, face embeddings, personal
identifiers and geometric templates are held out of the IPC, HTTP, database
and diagnostic logs. Frames and landmarks are used in process memory for one
observation. The optional calibration file is local, explicitly created
and removable. It contains an input index and three quantized neutral
angles, without images or face geometry. Camera summaries exported by the
website contain the same coarse statistics as the owned session report.

The Agent keeps only coarse totals in a DPAPI-protected outbox for an
authenticated account and API origin. Pending entries expire after 24 hours
and are removed on camera revocation when the Agent observes it. Stored
server totals remain available with session history after consent is
withdrawn. Account deletion removes owned sessions and aggregates through
existing cascade rules.
