# Backend architecture

ASP.NET Core translates HTTP contracts into application operations. Application owns
workflow orchestration and persistence contracts. The domain owns lifecycle rules
and has no HTTP, EF Core, or database dependencies. Infrastructure implements the
application contracts using PostgreSQL and is registered by the API composition root.

## Decisions

- Continue as one server deployment. Resource ownership and atomic session/telemetry
  changes currently benefit from a common PostgreSQL transaction boundary.
- Use controllers and the `/api/v1` prefix. The previous host had no business routes.
- Publish Problem Details with a request trace identifier. Internal exception text and
  rejected input values do not belong in API error bodies.
- Keep the original migrations intact. Apply new migrations in an explicit job.
- Keep the Windows collector separate from the server and preserve the distinction
  between observed events, heuristic estimates, goal outcomes, and self-reports.

## Components and dependencies

```mermaid
flowchart LR
    Clients["React / Windows Agent / Unity clients"] -->|HTTPS / JSON| Api["Focus.Api"]
    Api --> App["Focus.Application"]
    Api --> Infra["Focus.Infrastructure registered in composition root"]
    App --> Domain["Focus.Domain"]
    App --> Engine["Focus.Engine"]
    Engine --> Domain
    Infra -->|implements ports| App
    Infra --> Domain
    Infra --> Database[("PostgreSQL")]
```

Read models expose separate HTTP projections. Scoped EF adapters share one
context per request. API errors are translated at the HTTP boundary. The engine
depends on the domain's enums and evaluates pure data. Application uses the engine
directly because it is a deterministic in-process module without I/O.

## Authentication and request lifecycle

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Accounts
    participant DB as PostgreSQL
    Client->>API: Sign in
    API->>Accounts: Validate credentials
    Accounts->>DB: Read hash and create login
    API-->>Client: Protected access and refresh tickets
    Client->>API: Bearer request
    API->>DB: Check signed login ID is active
    API->>DB: Read or write resource with owner predicate
    API-->>Client: JSON or Problem Details with trace ID
```

## Telemetry ingestion

```mermaid
sequenceDiagram
    participant Agent
    participant API
    participant App
    participant Engine
    participant DB as PostgreSQL
    Agent->>API: Submit aggregate batch with stable item IDs
    API->>App: Authenticated owner and validated fields
    App->>DB: Lock account and session, check consent and IDs
    loop Each new compatible window
        App->>Engine: Aggregate features and effective process category
        Engine-->>App: Versioned estimate and reason
    end
    App->>DB: Commit observations and estimates together
    API-->>Agent: Created and duplicate counts
```

## Session completion

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant App
    participant DB as PostgreSQL
    Client->>API: Complete session with two ratings and goal outcomes
    API->>App: Map request and authenticate owner
    App->>DB: Lock owned account and session
    App->>App: Apply domain transitions and close open pause
    App->>DB: Commit all changes as one transaction
    API-->>Client: Persisted session, ratings, and goals
```
