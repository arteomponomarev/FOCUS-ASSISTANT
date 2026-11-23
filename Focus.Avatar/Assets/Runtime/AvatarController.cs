using System;
using Focus.Avatar.Animation;
using Focus.Avatar.Protocol;
using UnityEngine;

namespace Focus.Avatar
{
    [DisallowMultipleComponent]
    public sealed class AvatarController : MonoBehaviour
    {
        private WindowsOverlay overlay;
        private SessionController session;
        private AvatarView view;
        private Uri dashboard;
        private readonly AvatarAnimator animator = new AvatarAnimator();
        private AvatarPose pose;
        private AvatarMood mood;
        private bool settings;
        private bool reduceMotion;
        private bool systemMotion;
        private float nextMotionCheck;
        private const string ReduceMotionKey = "avatar.reduceMotion";
        private bool panel = true;
        private bool manuallyHidden;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            var args = Environment.GetCommandLineArgs();
            Guid.TryParse(Argument(args, "-focus-session"), out var id);
            dashboard = DashboardOrigin(Argument(args, "-focus-dashboard") ?? "http://127.0.0.1:5173/");
            session = new SessionController(id);
            overlay = new WindowsOverlay();
            view = new AvatarView();
            reduceMotion = PlayerPrefs.GetInt(ReduceMotionKey, 0) != 0;
            systemMotion = WindowsOverlay.AllowsAnimations();

            var camera = new GameObject("Overlay Camera").AddComponent<Camera>();
            camera.transform.SetParent(transform);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(255, 0, 255, 255);
            camera.cullingMask = 0;
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        private void Update()
        {
            overlay.Initialize();
            overlay.Position();
            if (overlay.ConsumeToggle())
            {
                panel = !panel || manuallyHidden;
                manuallyHidden = false;
            }
            var now = Time.realtimeSinceStartup;
            if (now >= nextMotionCheck)
            {
                systemMotion = WindowsOverlay.AllowsAnimations();
                nextMotionCheck = now + 3;
            }
            session.Tick(now);
            mood = session.Mood(now);
            pose = animator.Sample(mood, now, reduceMotion || !systemMotion, session.Snapshot.intensity);
            overlay.SetMode(Visible(), panel);
        }

        private bool Visible() => panel || (!manuallyHidden && overlay.Transparent && AvatarPresentation.ShowCompanion(
            session.Snapshot, session.Celebrating(Time.realtimeSinceStartup)));

        private void OnGUI()
        {
            if (view == null || (!Visible() && overlay.CanRecover))
                return;

            var showPanel = panel || !overlay.CanRecover;
            if (showPanel)
                DragPanel();

            var action = view.Draw(session, pose, mood, showPanel, overlay.RecoveryShortcut, overlay.Transparent,
                dashboard != null, settings, reduceMotion, systemMotion);
            switch (action)
            {
                case "close-panel":
                    panel = false;
                    break;
                case "hide":
                    panel = false;
                    manuallyHidden = true;
                    break;
                case "exit":
                    Application.Quit();
                    break;
                case "dashboard":
                    OpenWeb("dashboard");
                    break;
                case "web-settings":
                    OpenWeb("settings/assistant");
                    break;
                case "settings":
                    settings = !settings;
                    break;
                case "motion":
                    reduceMotion = !reduceMotion;
                    PlayerPrefs.SetInt(ReduceMotionKey, reduceMotion ? 1 : 0);
                    PlayerPrefs.Save();
                    break;
                case "finish":
                    OpenWeb($"sessions/{session.Session:D}");
                    break;
                case AvatarProtocol.Start:
                case AvatarProtocol.Pause:
                case AvatarProtocol.Resume:
                    session.Command(action);
                    break;
            }
        }

        private void DragPanel()
        {
            var input = Event.current;
            var point = (input.mousePosition - AvatarView.Offset) / Mathf.Max(.01f, AvatarView.Scale);
            if (input.type == EventType.MouseDown && input.button == 0 && AvatarView.DragArea.Contains(point))
                overlay.BeginDrag();
            else if (input.type == EventType.MouseDrag && input.button == 0)
                overlay.Drag();
            else if (input.type == EventType.MouseUp && input.button == 0)
                overlay.EndDrag();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                overlay?.EndDrag();
        }

        private void OpenWeb(string route)
        {
            if (dashboard != null)
                Application.OpenURL(new Uri(dashboard, route).AbsoluteUri);
        }

        private static string Argument(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static Uri DashboardOrigin(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var origin) &&
                (origin.Scheme == Uri.UriSchemeHttps || origin.Scheme == Uri.UriSchemeHttp && origin.IsLoopback) &&
                origin.AbsolutePath == "/" && origin.UserInfo.Length == 0 && origin.Query.Length == 0 &&
                origin.Fragment.Length == 0)
                return origin;
            return null;
        }

        private void OnDestroy()
        {
            session?.Dispose();
            overlay?.Dispose();
            view?.Dispose();
        }
    }
}
