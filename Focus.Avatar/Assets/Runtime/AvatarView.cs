using System;
using System.Collections.Generic;
using Focus.Avatar.Animation;
using Focus.Avatar.Protocol;
using UnityEngine;
using static Focus.Avatar.AvatarGraphics;

namespace Focus.Avatar
{
    internal sealed class AvatarView : IDisposable
    {
        public const float Width = 400;
        public const float Height = 478;
        public static readonly Rect DragArea = new Rect(23, 18, 199, 52);
        private static readonly Color Panel = new Color32(23, 20, 33, 255);
        private static readonly Color Surface = new Color32(33, 29, 49, 255);
        private static readonly Color Raised = new Color32(48, 40, 66, 255);
        private static readonly Color Outline = new Color32(68, 57, 87, 255);
        private static readonly Color Text = new Color32(240, 237, 247, 255);
        private static readonly Color Muted = new Color32(191, 182, 211, 255);
        private static readonly Color Lavender = new Color32(206, 188, 255, 255);
        private static readonly Color OnPrimary = new Color32(36, 19, 74, 255);
        private static readonly Color Mint = new Color32(172, 244, 200, 255);
        private static readonly Color Amber = new Color32(255, 220, 146, 255);
        private static readonly Color Lime = new Color32(233, 253, 154, 255);
        private readonly OrbitCharacter orbit = new OrbitCharacter();
        private readonly Texture2D frame = KeyedFrame(384, 462, 20, Panel, Outline);
        private readonly List<int> tabStops = new List<int>(10);
        private Font font;
        private GUIStyle caption, strong, heading, timer, center, goal;
        private string action;

        public static float Scale => Mathf.Min(Screen.width / Width, Screen.height / Height);
        public static Vector2 Offset => new Vector2((Screen.width - Width * Scale) / 2, (Screen.height - Height * Scale) / 2);

        public string Draw(SessionController session, AvatarPose pose, AvatarMood mood, bool panel, string shortcut,
            bool transparent, bool hasWeb, bool settings, bool reduceMotion, bool systemMotion)
        {
            PrepareStyles();
            action = "";
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab && panel)
                MoveFocus(Event.current.shift ? -1 : 1);
            tabStops.Clear();

            if (!transparent)
                Card(new Rect(0, 0, Screen.width, Screen.height), Panel);

            var matrix = GUI.matrix;
            GUI.matrix = matrix * Matrix4x4.TRS(Offset, Quaternion.identity, Vector3.one * Scale);
            if (panel)
            {
                PanelFrame(session, shortcut);
                if (settings)
                    Preferences(hasWeb, reduceMotion, systemMotion);
                else
                    SessionPanel(session, pose, mood, shortcut, hasWeb);
            }
            else if (Event.current.type == EventType.Repaint)
            {
                orbit.Render(new Rect(249, 303, 132, 132), pose, mood, true);
            }
            GUI.matrix = matrix;
            return action;
        }

        private void PanelFrame(SessionController session, string shortcut)
        {
            Sprite(frame, new Rect(8, 8, 384, 462), Color.white);
            Card(new Rect(24, 26, 34, 34), Raised, 10);
            Mark(new Vector2(41, 43), Lavender);
            Label(new Rect(69, 25, 173, 36), "Focus Assistant", heading, Text);

            var state = session.Snapshot.result == "ok" ? "Agent online" : session.AgentReachable ? "API unavailable" : "Agent offline";
            Chip(new Rect(238, 32, 104, 23), state, state == "Agent online" ? Mint : Amber, Surface, true);
            Control(new Rect(348, 27, 28, 32), "Minimize panel", "close-panel", shortcut.Length != 0, icon: true);
        }

        private void SessionPanel(SessionController session, AvatarPose pose, AvatarMood mood, string shortcut, bool hasWeb)
        {
            var state = session.Snapshot;
            var status = state.status switch
            {
                "Active" => "In session",
                "Paused" => "Paused",
                "Planned" => "Ready to start",
                "Completed" => "Completed",
                "Cancelled" => "Cancelled",
                _ => "No session connected"
            };
            var now = Time.realtimeSinceStartup;
            var seconds = session.ActiveSeconds(now);
            var hero = new Rect(24, 73, 352, 151);
            Card(hero, Surface, 15);
            Card(new Rect(41, 192, 143, 4), Outline, 2);
            if (state.hasTiming && seconds > 0)
                Card(new Rect(41, 192, 143 * Mathf.Clamp01((float)(seconds / state.plannedSeconds)), 4), Lavender, 2);
            Label(new Rect(41, 88, 156, 20), "Active time", caption, Muted);
            Label(new Rect(37, 108, 190, 56), state.hasTiming ? Duration(seconds) : "—", timer, Text);
            Chip(new Rect(41, 164, state.status == "" ? 150 : 120, 21), status,
                state.status == "Paused" ? Amber : Lavender, Raised, true);

            if (Event.current.type == EventType.Repaint)
            {
                Card(new Rect(245, 88, 110, 110), Raised, 55);
                Card(new Rect(246, 89, 108, 108), Surface, 54);
                orbit.Render(new Rect(244, 76, 111, 111), pose, mood, false);
            }

            var estimate = state.estimate.Length != 0 ? state.estimate : "Unavailable";
            Label(new Rect(41, 201, 155, 19), state.hasTiming ? $"Planned {Duration(state.plannedSeconds)}" : "",
                caption, Muted);
            Label(new Rect(207, 199, 153, 23), $"Estimate · {estimate}", center, EstimateTone(state.estimate));

            Label(new Rect(24, 233, 260, 19), "Current goal", strong, Text);
            Card(new Rect(24, 256, 352, 55), Surface, 11);
            Label(new Rect(36, 261, 326, 45), GoalText(state.goal), goal, state.goal.Length == 0 ? Muted : Text);

            var verb = state.status switch
            {
                "Planned" => AvatarProtocol.Start,
                "Active" => AvatarProtocol.Pause,
                "Paused" => AvatarProtocol.Resume,
                _ => ""
            };
            var command = verb.Length > 0 ? char.ToUpperInvariant(verb[0]) + verb.Substring(1) : "Start";
            Control(new Rect(24, 323, 171, 40), session.Busy ? "Sending…" : command, verb,
                verb.Length > 0 && session.CanCommand(verb), true);
            Control(new Rect(205, 323, 171, 40), "Finish in browser ↗", "finish",
                hasWeb && state.status is "Active" or "Paused");

            var cameraLabel = state.result != "ok" ? "status unknown" : state.cameraState switch
            {
                "Disabled" => "disabled",
                "Starting" => "starting",
                "Processing" => "processing locally",
                "Unavailable" => "unavailable",
                _ => "waiting for Agent"
            };
            Label(new Rect(26, 369, 170, 23), $"Camera · {cameraLabel}", caption,
                state.cameraState == "Processing" ? Mint : Muted);
            var telemetry = state.result != "ok" ? "Telemetry status unknown" :
                state.telemetryAllowed ? "Telemetry permitted" : "Telemetry off";
            Label(new Rect(200, 369, 174, 23), telemetry, center, state.telemetryAllowed ? Mint : Muted);

            var footer = session.Feedback.Length != 0 ? session.Feedback :
                shortcut.Length != 0 ? $"{shortcut} opens controls" : "Use the Windows title bar to close.";
            Label(new Rect(26, 397, 348, 18), footer, caption, Muted);
            Control(new Rect(24, 425, 101, 28), "Dashboard", "dashboard", hasWeb);
            Control(new Rect(132, 425, 86, 28), "Settings", "settings", true);
            Control(new Rect(225, 425, 72, 28), "Hide", "hide", shortcut.Length != 0);
            Control(new Rect(304, 425, 72, 28), "Exit", "exit", true);
        }

        private void Preferences(bool hasWeb, bool reduced, bool systemMotion)
        {
            Label(new Rect(24, 79, 352, 35), "Desktop settings", heading, Text);
            Card(new Rect(24, 126, 352, 105), Surface, 15);
            Label(new Rect(37, 135, 233, 24), "Reduce motion", strong, Text);
            Label(new Rect(37, 163, 320, 51), systemMotion
                ? "Keeps expressions visible with less movement."
                : "Windows has disabled animation effects.", goal, Muted);
            Control(new Rect(305, 135, 53, 29), reduced || !systemMotion ? "On" : "Off", "motion", systemMotion);
            Control(new Rect(24, 244, 352, 40), "Account appearance in browser ↗", "web-settings", hasWeb);
            Control(new Rect(24, 425, 108, 28), "Back to session", "settings", true);
            Control(new Rect(304, 425, 72, 28), "Exit", "exit", true);
        }

        private void Chip(Rect rect, string value, Color color, Color background, bool dot)
        {
            Card(rect, background, rect.height / 2);
            if (dot)
                Card(new Rect(rect.x + 9, rect.y + (rect.height - 6) / 2, 6, 6), color, 3);
            Label(new Rect(rect.x + 19, rect.y, rect.width - 21, rect.height), value, caption, color);
        }

        private void Control(Rect rect, string value, string command, bool enabled, bool primary = false, bool icon = false)
        {
            var id = GUIUtility.GetControlID(command.GetHashCode(), FocusType.Keyboard, rect);
            if (enabled)
                tabStops.Add(id);
            var input = Event.current;
            var focused = GUIUtility.keyboardControl == id;
            var pressed = GUIUtility.hotControl == id;
            var hovered = rect.Contains(input.mousePosition);
            switch (input.GetTypeForControl(id))
            {
                case EventType.MouseDown when enabled && hovered && input.button == 0:
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = id;
                    input.Use();
                    break;
                case EventType.MouseUp when pressed:
                    GUIUtility.hotControl = 0;
                    if (enabled && hovered)
                        action = command;
                    input.Use();
                    break;
                case EventType.KeyDown when enabled && focused &&
                    input.keyCode is KeyCode.Space or KeyCode.Return or KeyCode.KeypadEnter:
                    action = command;
                    input.Use();
                    break;
                case EventType.Repaint:
                    var color = !enabled ? Surface : primary
                        ? pressed ? Muted : hovered ? Color.white : Lavender
                        : pressed ? Outline : hovered ? Raised : Surface;
                    if (focused && enabled)
                        Card(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), Lime, 11);
                    Card(rect, color, 9);
                    if (icon)
                        Card(new Rect(rect.center.x - 6, rect.center.y, 12, 2), enabled ? Text : Muted, 1);
                    else
                        Label(rect, value, center, primary && enabled ? OnPrimary : enabled ? Text : Muted);
                    break;
            }
        }

        private void MoveFocus(int direction)
        {
            if (tabStops.Count == 0)
                return;
            var prior = tabStops.IndexOf(GUIUtility.keyboardControl);
            if (prior == -1)
                prior = direction > 0 ? -1 : 0;
            GUIUtility.keyboardControl = tabStops[(prior + direction + tabStops.Count) % tabStops.Count];
            Event.current.Use();
        }

        private static Color EstimateTone(string estimate) => estimate switch
        {
            "Focused" => Mint,
            "Distracted" => Amber,
            "" => Muted,
            _ => Lavender
        };

        private static string GoalText(string value)
        {
            if (value.Length == 0)
                return "No pending goal";
            var singleLine = value.Replace('\r', ' ').Replace('\n', ' ');
            return singleLine.Length <= 82 ? singleLine : singleLine.Substring(0, 81) + "…";
        }

        private static string Duration(double seconds)
        {
            var total = (long)Math.Max(0, Math.Floor(seconds));
            return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
        }

        private static void Mark(Vector2 position, Color color)
        {
            Line(position + new Vector2(-6, 3), position + new Vector2(-1, -4), 1.8f, color);
            Line(position + new Vector2(-1, -4), position + new Vector2(2, 0), 1.8f, color);
            Line(position + new Vector2(2, 0), position + new Vector2(7, -7), 1.8f, color);
            Card(new Rect(position.x - 6, position.y + 6, 13, 1.5f), color, .75f);
        }

        private static void Label(Rect rect, string value, GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            GUI.Label(rect, value, style);
        }

        private void PrepareStyles()
        {
            if (heading != null)
                return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16);
            strong = Style(15, FontStyle.Bold);
            heading = Style(18, FontStyle.Bold);
            timer = Style(43, FontStyle.Bold);
            caption = Style(12, FontStyle.Normal);
            center = Style(13, FontStyle.Bold, TextAnchor.MiddleCenter);
            goal = Style(14, FontStyle.Normal);
            goal.wordWrap = true;
        }

        private GUIStyle Style(int size, FontStyle weight, TextAnchor alignment = TextAnchor.MiddleLeft) => new GUIStyle
        {
            font = font == null ? GUI.skin.font : font,
            fontSize = size,
            fontStyle = weight,
            alignment = alignment,
            clipping = TextClipping.Clip,
            richText = false
        };

        public void Dispose()
        {
            orbit.Dispose();
            UnityEngine.Object.Destroy(frame);
            if (font != null)
                UnityEngine.Object.Destroy(font);
        }
    }
}
