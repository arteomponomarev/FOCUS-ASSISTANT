# Frontend technology decisions

- React 19, strict TypeScript and Vite provide component composition, compile-time checks and route-level build chunks for the SPA.
- React Router owns navigation and outlet layouts. Protected navigation returns to the original local path after sign-in.
- TanStack Query owns server state, invalidation, cancellation and cache lifetime. A separate general-purpose state store is unnecessary for the current features.
- React Hook Form and Zod validate authentication and session creation. Native form constraints and API validation support smaller settings forms. Backend validation is authoritative.
- CSS custom properties, Grid and Flexbox provide theming and adaptive layouts. Native form elements and dialogs preserve browser semantics. Lucide provides consistent scalable icons.
- Lightweight CSS charts include a tabular alternative and require no chart runtime.
- The OpenAPI schema generates TypeScript transport types. `openapi-fetch` retains request and response typing across operations.
- Vitest, Testing Library, Playwright and axe cover unit, browser and sampled accessibility checks. npm lockfile, ESLint and Prettier provide reproducible local checks.
