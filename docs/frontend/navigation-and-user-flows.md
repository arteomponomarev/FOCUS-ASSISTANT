# Navigation and user flows

```mermaid
flowchart TD
    Landing["/"] --> Login["/login"]
    Landing --> Dashboard["/dashboard"]
    Login --> Register["/register"]
    Register --> Login
    Login --> Dashboard
    Dashboard --> History["/sessions"]
    Dashboard --> Analytics["/analytics"]
    Dashboard --> Settings["/settings"]
    Dashboard --> New["/sessions/new"]
    History --> Detail["/sessions/:id"]
    New --> Detail
    Settings --> Preferences["profile · work-profiles · pomodoro · privacy · assistant"]
```

```mermaid
sequenceDiagram
    participant Browser
    participant API
    Browser->>API: Login with email and password
    API-->>Browser: Access and refresh credentials
    Browser->>API: Load current user and dashboard
    Browser->>API: Create session with planned duration and goals
    Browser->>API: Start, pause or resume session
    Browser->>API: Complete session with ratings and goal outcomes
    API-->>Browser: Persisted completed session
    Browser->>API: Revalidate session detail, history and analytics
```

Valid lifecycle actions follow the server's Planned, Active and Paused states. The completion dialog submits both self-reported ratings and goal outcomes together. The Windows Agent is the source of desktop signals. Web sessions remain usable without desktop telemetry.
