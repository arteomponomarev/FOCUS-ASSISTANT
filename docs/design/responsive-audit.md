# Responsive UI audit

The browser suite runs with an isolated PostgreSQL 17 database and the release ASP.NET Core build. It uses synthetic accounts and telemetry. No real desktop activity is collected.

| Test area | Coverage |
| --- | --- |
| Mobile portrait | 320×720, 375×812, 390×844, 430×932 |
| Mobile landscape | 844×390 |
| Tablet portrait and landscape | 768×1024 and 1024×768 |
| Desktop | 1440×1000 and 1920×1080 |
| Additional viewport reflow | 1152×800, 960×667 and 720×500 |
| Native browser zoom | 125%, 150%, 200% via Chromium Appearance settings in a temporary 1440px profile, verified by pixel ratio and CSS viewport width |
| Main routes | Dashboard, session history, session creation, active session, analytics and settings |
| Navigation | Labeled desktop/sidebar links and mobile bottom navigation |
| Touch targets | At least 44×44 CSS px for visible links, buttons, inputs, checkbox label surfaces and expandable sections on the sampled 390px sign-in, overview, session, history, analytics and account settings screens |
| Keyboard | Visible keyboard focus, Enter to open completion, Tab containment, Escape and focus restoration |
| Contrast | axe color-contrast and WCAG 2.0/2.1 A/AA checks on sign-in, the overview in light and dark themes, and the completion dialog |

The sampled WCAG tags returned zero axe violations. Measured foreground and background token pairs:

| Pair | Light | Dark |
| --- | ---: | ---: |
| Body text on panel | 16.17:1 | 14.14:1 |
| Secondary text on panel | 6.51:1 | 8.44:1 |
| Primary button text on fill | 7.79:1 | 9.68:1 |
| Focus outline on canvas | 6.97:1 | 10.97:1 |

The [PDF](focus-assistant-adaptive-ui.pdf) contains 15 actual viewport captures of five screens at three widths plus a design-system plate. Visually inspected examples include the desktop overview and mobile active session. The PDF uses the same stylesheet and backend data as the verified browser session.

To recreate from `Focus.Web`, follow [browser test setup](../frontend/testing.md), then run:

```powershell
$env:FOCUS_CAPTURE_DESIGN = '1'
npm run test:e2e -- e2e/responsive.spec.ts
npm run design:pdf
Remove-Item Env:FOCUS_CAPTURE_DESIGN
```

The captures remain in ignored `artifacts/frontend-design/`. The PDF is the separate submission artifact. Physical-device behavior, mobile on-screen keyboard interaction, screen-reader behavior and exhaustive contrast coverage remain outside this sample.

The [submission verification](../submission/final-verification.md#adaptive-ui-and-lighthouse) re-ran the viewport suite on the Redux version and inspected all six pages of the PDF. Set `FOCUS_CAPTURE_QA=1` before the responsive test to capture the nine major screens in both themes and additional viewport sizes.

## TASK 20 verification, November 23, 2026

Chromium passed the expanded checks on the production Vite build with the real API and an isolated PostgreSQL database. The suite covered login, registration, overview, session history, session creation, active and completed details, analytics, settings and Privacy with Computer Vision. The 13 windows ranged from 320×720 to 1920×1080. Landscape samples included 667×375 and 844×390 on phones and 1024×768 on tablets. The test checked horizontal overflow and navigation throughout, and measured sampled touch targets, including Privacy controls, at 390 px. Desktop activity and camera consent remain independently controlled in the test.

The [native zoom test](../../Focus.Web/e2e/zoom.spec.ts) passed at 125%, 150% and 200%, including the Privacy tab and camera controls. The [responsive test](../../Focus.Web/e2e/responsive.spec.ts) also passed its WCAG 2.1 A/AA axe rules on sampled screens in light and dark, and exercised visible keyboard focus, dialog tab placement, Escape dismissal and return of focus. No UI or stylesheet change was required by these checks.

| Semantic text on its tinted surface | Light | Dark |
| --- | ---: | ---: |
| Success | 6.24:1 | 8.98:1 |
| Warning | 5.87:1 | 8.91:1 |
| Error | 6.99:1 | 7.18:1 |

The ratios above were calculated from the CSS tokens. The 390×844 Privacy screenshots were visually inspected in [light](screenshots/task20-mobile-light-privacy.png) and [dark](screenshots/task20-mobile-dark-privacy.png). Both show an isolated synthetic account. All six existing PDF pages were rendered and inspected again. The screens and the design-system plate were readable and did not require replacement. The [performance report](../frontend/task20-performance.md) contains local mobile and desktop results.

Physical touch and on-screen keyboard behavior, operating-system display scaling, actual assistive technology output and exhaustive WCAG conformance remain unverified. Use NVDA or VoiceOver and a physical phone for the remaining interaction checks.
