# Windows desktop demo checklist

Use the [build and pairing guide](../../Focus.Avatar/README.md) with a
disposable session and data you are comfortable collecting.

| Step | Expected result |
| --- | --- |
| Create a planned session with a goal in React. Start Agent with `--watch` and run the Player for the same UUID. | Panel shows Planned and the first pending goal. No hooks are started in watch mode. |
| Start the session from the avatar, then pause and resume. Refresh the detail page in React after each command. | API state matches the command. Pause shows a relaxed expression. |
| Close the panel while the session is active. Click a harmless item behind the overlay. Press Ctrl+Alt+F9. | The click reaches the underlying application. The panel reopens and accepts clicks. |
| Drag by the title, hide, then recover the window with the hotkey. Repeat on another monitor if available. | The overlay stays in the work area. Hide is reversible. |
| Turn on consent. Restart Agent with `--collect --avatar` and confirm `START`. Use benign applications for at least one aggregate window. | React shows a saved aggregate and estimate. The avatar displays the same recent estimate and rule version. |
| Select **Finish in browser**, enter both ratings and any goal outcomes, and confirm in React. | Browser saves the normal completion. The companion briefly celebrates while Agent is connected. |
| Withdraw consent or stop Agent while the panel is visible. Restart Agent if checking reconnection. | Collection stops on consent withdrawal. Connection loss clears private session data. The Player recovers after the bridge returns. |
| Select **Exit**, then Ctrl+C in Agent. | The window closes and the shortcut is released. Agent stops collecting after its normal shutdown path. |

To demonstrate a distinct **Focused** expression, assign the session a work
profile that allows a benign foreground process. Use that process with
occasional input, little idle time and little switching. Without profile
evidence, **Neutral** is a valid estimate. Avoid typing secrets during the
demo even though the collector keeps only event counts.
