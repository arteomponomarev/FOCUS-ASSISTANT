# Focus.Web architecture

Focus.Web is a React and TypeScript SPA built by Vite. The existing ASP.NET Core API owns all durable application state. The frontend does not access PostgreSQL and does not collect desktop activity.

```mermaid
flowchart LR
    Entry[React entry and Redux Provider] --> Router[React Router]
    Entry --> Store[Redux Toolkit store]
    Router --> Public[Public authentication layout]
    Router --> Workspace[Protected workspace layout]
    Workspace --> Features[Lazy feature pages]
    Features --> Shared[Reusable controls, charts and feedback]
    Features --> Queries[RTK Query endpoints]
    Features --> Store
    Store --> Queries
    Store --> Effects[Signals saga and export thunk]
    Effects --> Queries
    Effects --> Client
    Public --> Client
    Queries --> Client[Typed HTTP client]
    Client --> Transport[Bearer session and fetch transport]
    Transport --> API[ASP.NET Core v1]
    API --> DB[(PostgreSQL)]
```

| Layer | Location | Responsibilities |
| --- | --- | --- |
| Application | `Focus.Web/src/app` | Router, protected layout, typed Redux store and theme |
| Features | `Focus.Web/src/features` | Screen composition, feature queries, forms and mutations |
| Boundary | `Focus.Web/src/api` | Generated OpenAPI types, shared RTK Query API, typed requests, authentication and normalized errors |
| Shared UI | `Focus.Web/src/ui`, `Focus.Web/src/styles` | Controls, dialog, status feedback, charts and responsive styles |

The router lazy-loads primary screens. Session controls derive available actions from the latest server status. A monotonic browser timer displays elapsed time between API updates. It does not write timestamps or durations to the database.

See [technology decisions](technology-decisions.md), [state management](state-management.md) and [navigation and user flows](navigation-and-user-flows.md).
