# Focus Assistant design system

The implementation source is [base.css](../../Focus.Web/src/styles/base.css), [workspace.css](../../Focus.Web/src/styles/workspace.css) and [features.css](../../Focus.Web/src/styles/features.css). The [Adaptive UI PDF](focus-assistant-adaptive-ui.pdf) includes component state specimens and actual browser captures at mobile, tablet and desktop sizes.

| Token | Light | Dark | Role |
| --- | --- | --- | --- |
| Canvas | #F6F5FA | #171421 | Workspace background |
| Surface | #FFFFFF | #211D31 | Panels and forms |
| Text | #231D35 | #F0EDF7 | Primary copy |
| Muted | #615A75 | #BFB6D3 | Secondary copy |
| Primary | #5232CD | #CEBCFF | Actions and links |
| Focus | #5331D6 | #DABDFF | Keyboard outline |
| Success | #1A654B | #ACF4C8 | Saved or completed |
| Warning | #805211 | #FFDC92 | Signals and intermediate outcomes |
| Error | #932744 | #FFB6CA | Validation and destructive actions |

System sans-serif typography uses a 1 rem body, responsive 1.8–3 rem page headings and 1.1–1.35 rem section headings. Shared cards use 1.125 rem radii. Main gutters vary from .875 to 2 rem. Interactive controls use a 2.75 rem minimum size, and focus/disabled/hover/loading/error states share the corresponding tokens.

Desktop uses a labeled sidebar. Tablet uses a condensed sidebar and flexible content grids. Mobile uses bottom navigation and stacked session cards. Grid tracks are shrinkable, charts provide numeric values, and labels accompany semantic colors. Reduced-motion preferences shorten transitions and animations.
