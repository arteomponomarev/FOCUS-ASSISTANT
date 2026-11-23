# Windows Player captures

All PNGs are window-only captures of the running Unity Player. They use
synthetic goals and disposable sessions. The original and updated Focused
captures both use the real Agent, API, Focus Engine and test PostgreSQL.

| Capture | Evidence |
| --- | --- |
| [Before](before.png) | Previous Player, recorded while its first recovery shortcut was in use. |
| [After](focused-100.png) | Orbit, active timer, goal, Focused estimate and session controls. |
| [125% client size](focused-125.png) | 500 × 598 client on the 96 DPI Windows desktop. |
| [150% client size](focused-150.png) | 600 × 717 client on the same desktop. |
| [Celebration recording](celebration.gif) | Short sequence of actual Player frames from the celebration transition. |

The following captures use the separate test-only IPC fixture to make every
pose reproducible:

[Neutral](neutral.png) · [Focused](focused.png) · [Distracted](distracted.png) ·
[Idle](idle.png) · [Break](break.png) · [Absent](absent.png) · [Tired](tired.png) ·
[Celebrating](celebrating.png) · [Disconnected](disconnected.png).
