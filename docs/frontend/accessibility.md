# Accessibility verification

[Responsive audit](../design/responsive-audit.md) specifies the viewports, interaction checks and coverage limits.

The app uses landmarks, a skip link, labeled controls, linked validation descriptions, live feedback, semantic tables, a native modal dialog, keyboard focus styles and reduced-motion preferences. Charts provide a tabular alternative. The mobile navigation uses labeled buttons and links. The web client does not request keyboard, clipboard, camera or other desktop telemetry permissions.

The [Playwright responsive suite](../../Focus.Web/e2e/responsive.spec.ts) runs axe with WCAG 2.0 and 2.1 A/AA tags on the public sign-in page, mobile overview in both themes, and the open Finish session dialog. Keyboard assertions exercise visible focus, Enter, Tab containment and Escape focus restoration. The suite measures 44 × 44 CSS pixel targets on sampled mobile routes. The [native zoom test](../../Focus.Web/e2e/zoom.spec.ts) checks 125%, 150% and 200% reflow.

The [submission verification](../submission/final-verification.md#adaptive-ui-and-lighthouse) records zero axe violations in the tested states and Lighthouse accessibility scores for Login, Overview, Sessions and Analytics on local mobile and desktop profiles. The [Adaptive UI PDF](../design/focus-assistant-adaptive-ui.pdf) shows the local browser captures.

These checks cover specific rendered states. Screen-reader announcements, all component variants and native mobile keyboard behavior require manual device verification. Automated results do not establish full WCAG 2.1 AA conformance.
