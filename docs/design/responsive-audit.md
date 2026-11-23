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
