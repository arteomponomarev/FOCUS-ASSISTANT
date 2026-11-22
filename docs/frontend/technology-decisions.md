# Frontend technology decisions

- React 19, strict TypeScript and Vite provide component composition, compile-time checks and route-level build chunks for the SPA.
- React Router owns navigation and outlet layouts. Protected navigation returns to the original local path after sign-in.
- Redux Toolkit and React Redux provide the shared store and typed hooks. RTK Query owns server state, cancellation, cache lifetime and tag invalidation. Features inject endpoints into one API and use the existing OpenAPI transport.

- Redux Saga coordinates refreshes while a session signals view is open, including the time allowed for the final agent upload. Redux Thunk, included by Redux Toolkit, runs the cancellable one-shot account export.
- React Hook Form and Zod validate authentication and session creation. Native form constraints and API validation support smaller settings forms. Backend validation is authoritative.
- CSS custom properties, Grid and Flexbox provide theming and adaptive layouts. Native form elements and dialogs preserve browser semantics. Lucide provides consistent scalable icons.
- Lightweight CSS charts include a tabular alternative and require no chart runtime.
- The OpenAPI schema generates TypeScript transport types. `openapi-fetch` retains request and response typing across operations.
- Vitest, Testing Library, Playwright and axe cover unit, browser and sampled accessibility checks. npm lockfile, ESLint and Prettier provide reproducible local checks.
