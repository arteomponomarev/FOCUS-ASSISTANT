# Orbit desktop companion

## Direction

Orbit is a small 2D satellite character with a mint ceramic shell, a dark
visor and one star-shaped antenna. The visor is a separate drawing layer so
eyelids, gaze and mouth follow the body transform. The previous Player used
point-filtered geometric shapes, a shared vertical bob and the default Unity
button skin. The new panel gives the timer and the current action the most
visual weight.

| Token | Value |
| --- | --- |
| Panel, card and raised surfaces | `#171421`, `#211D31`, `#302842` from the Focus.Web dark theme |
| Primary action, text and secondary text | `#CEBCFF`, `#F0EDF7`, `#BFB6D3` |
| Connected / focused, warning and keyboard focus | `#ACF4C8`, `#FFDC92`, `#E9FD9A` |
| Type | Segoe UI with Arial fallback. 12 px captions, 13–15 px controls and content, 18 px headings, 43 px timer at the reference size. |
| Layout | 400 × 478 reference coordinates, 8 px outer inset, 16 px internal section inset, 9–20 px corner radii. |
| Controls | Distinct hover, press, keyboard focus and disabled states. Tab and Shift+Tab cycle currently enabled controls, Enter or Space activates the focused action. |

The timer represents backend **active time** excluding pauses. The progress
bar compares active time with the planned duration. The panel places the
estimate near Orbit and keeps the current goal above the session controls.
Agent connectivity and API health are separate. Camera availability is a
future capability label. Settings includes a local Reduce motion preference
and opens the existing web Assistant settings for account preferences.

## Art and animation

The original transparent shell sprite was generated for this project using
ImageGen and is stored as
[`Assets/Resources/OrbitShell.png`](../../Focus.Avatar/Assets/Resources/OrbitShell.png).
The Unity importer limits the in-game texture to 512 px and keeps its alpha
for the panel. A binary-alpha copy handles the Win32 color-key overlay.
No external font files, third-party character assets or 3D assets are bundled.

| Appearance | Trigger and motion |
| --- | --- |
| Neutral | Relaxed face, small drift, regular blinking. |
| Focused | Centered gaze, slight smile and restrained movement. |
| Distracted | Concerned brow, off-center gaze and a slow look to the side. |
| Idle | Visible breathing with light body stretch and a periodic blink. |
| Break | A paused session or Break estimate. Lower resting pose and half-lidded eyes. |
| Absent | Dimmed sleeping pose with closed eyes and slow breathing. |
| Tired | Drooping pose after 30 continuous seconds of displayed Idle estimates. The recorded estimate stays Idle. |
| Celebrating | Completion observed after an active or paused state. One eased lift with small sparks, then settle. |
| Disconnected | Dim visor, low-motion pose and link marks. The panel explains the connection condition. |

`AvatarAnimator` blends the current sampled pose into the next one over
450 ms, including interrupted transitions. Quiet mode reduces motion amplitude.
Reduce motion and the Windows Animation effects setting suppress bobbing,
blinking and sparks while preserving each expression. Quiet mode retains
the account's existing notification policy. Camera acquisition and inferred
fatigue are outside the design.

## Render and test evidence

- [Before and after, animation states and scale checks](screenshots/redesign/README.md)
- [Windows verification and reproduction](desktop-visual-verification.md)
