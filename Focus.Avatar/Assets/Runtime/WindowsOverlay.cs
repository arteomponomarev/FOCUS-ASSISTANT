using System;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
#endif

namespace Focus.Avatar
{
    internal sealed class WindowsOverlay : IDisposable
    {
        public bool Transparent { get; private set; }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int Style = -16, ExStyle = -20;
        private const long Popup = 0x80000000L, Overlapped = 0x00CF0000L;
        private const long Layered = 0x00080000L, PassThrough = 0x20, NoActivate = 0x08000000L;
        private const uint NoSize = 0x01, NoMove = 0x02, NoZOrder = 0x04, NoFocus = 0x10, FrameChanged = 0x20;
        private static readonly IntPtr Topmost = new IntPtr(-1);
        private IntPtr window;
        private bool wasHidden, initiallyPositioned;
        private RecoveryHotkey hotkey;
        private Point dragStart;
        private Rect original;
        private bool dragging;
        public bool CanRecover => hotkey != null && hotkey.Registered;
        public string RecoveryShortcut => CanRecover ? hotkey.Shortcut : "";
        public bool ConsumeToggle() => hotkey != null && hotkey.Consume();
        public static bool AllowsAnimations() =>
            !SystemParametersInfo(0x1042, 0, out var enabled, 0) || enabled != 0;

        public void Initialize()
        {
            if (window != IntPtr.Zero)
                return;

            using var process = Process.GetCurrentProcess();
            window = process.MainWindowHandle;
            if (window == IntPtr.Zero)
                return;

            var previousStyle = GetWindowLongPtr(window, Style).ToInt64();
            var previousExStyle = GetWindowLongPtr(window, ExStyle).ToInt64();
            hotkey = new RecoveryHotkey(process.Id);

            SetWindowLongPtr(window, ExStyle, new IntPtr(previousExStyle | Layered));
            Transparent = SetLayeredWindowAttributes(window, 0x00FF00FF, 255, 0x01);
            // Keep the normal system close affordance if the hotkey is unavailable.
            if (Transparent && CanRecover && GetClientRect(window, out var client))
            {
                SetWindowLongPtr(window, Style, new IntPtr((previousStyle & ~Overlapped) | Popup));
                SetWindowPos(window, Topmost, 0, 0, client.Right, client.Bottom, NoMove | NoFocus | FrameChanged);
                return;
            }
            if (!Transparent)
                SetWindowLongPtr(window, ExStyle, new IntPtr(previousExStyle));
            SetWindowPos(window, Topmost, 0, 0, 0, 0, NoMove | NoSize | NoFocus | FrameChanged);
        }

        public void Position()
        {
            if (window == IntPtr.Zero || initiallyPositioned || !GetWindowRect(window, out var bounds))
                return;
            var monitor = MonitorFromWindow(window, 2);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return;
            var x = Math.Max(info.Work.Left, info.Work.Right - (bounds.Right - bounds.Left) - 24);
            var y = Math.Max(info.Work.Top, info.Work.Bottom - (bounds.Bottom - bounds.Top) - 24);
            initiallyPositioned = SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, NoSize | NoZOrder | NoFocus);
        }

        public void SetMode(bool shown, bool interactive)
        {
            if (window == IntPtr.Zero || !CanRecover)
                return;

            var current = GetWindowLongPtr(window, ExStyle).ToInt64();
            var flags = interactive || !Transparent ? current & ~(PassThrough | NoActivate)
                : current | PassThrough | NoActivate;
            if (flags != current)
            {
                SetWindowLongPtr(window, ExStyle, new IntPtr(flags));
                SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, NoMove | NoSize | NoZOrder | NoFocus | FrameChanged);
            }
            if (shown == wasHidden)
            {
                ShowWindow(window, shown ? 4 : 0); // SW_SHOWNOACTIVATE or SW_HIDE
                wasHidden = !shown;
            }
        }

        public void BeginDrag()
        {
            dragging = window != IntPtr.Zero && GetCursorPos(out dragStart) && GetWindowRect(window, out original);
        }

        public void Drag()
        {
            if (!dragging || !GetCursorPos(out var cursor))
                return;

            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromPoint(cursor, 2), ref info))
                return;
            var width = original.Right - original.Left;
            var height = original.Bottom - original.Top;
            var x = Clamp(original.Left + cursor.X - dragStart.X, info.Work.Left, info.Work.Right - width);
            var y = Clamp(original.Top + cursor.Y - dragStart.Y, info.Work.Top, info.Work.Bottom - height);
            SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, NoSize | NoZOrder | NoFocus);
        }

        public void EndDrag() => dragging = false;

        private static int Clamp(int value, int minimum, int maximum) => Math.Max(minimum, Math.Min(maximum, value));

        public void Dispose()
        {
            if (window == IntPtr.Zero)
                return;
            hotkey?.Dispose();
            hotkey = null;
            // The Player owns its window. Avoid triggering a style or size message during engine teardown.
            window = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo
        {
            public int Size;
            public Rect Monitor, Work;
            public uint Flags;
        }
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
        [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint color, byte alpha, uint flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(Point point, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        private static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint flags);

        private sealed class RecoveryHotkey : IDisposable
        {
            private const int KeyId = 9021;
            private const uint HotkeyMessage = 0x0312, StopMessage = 0x8001;
            private readonly Thread thread;
            private readonly int processId;
            private readonly ManualResetEventSlim initialized = new ManualResetEventSlim();
            private IntPtr receiver;
            private volatile bool registered;
            private string shortcut = "";
            private int stopping, toggle;

            public RecoveryHotkey(int processId)
            {
                this.processId = processId;
                thread = new Thread(Listen) { IsBackground = true, Name = "Avatar recovery hotkey" };
                thread.Start();
                initialized.Wait(500);
            }

            public bool Registered => registered;
            public string Shortcut => shortcut;
            public bool Consume() => Interlocked.Exchange(ref toggle, 0) != 0;

            private void Listen()
            {
                // Use a native message-only window so Unity's window procedure is left intact at shutdown.
                var window = CreateWindowEx(0, "STATIC", $"Focus Assistant Hotkey {processId}", 0, 0, 0, 0, 0,
                    new IntPtr(-3), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (window == IntPtr.Zero)
                {
                    initialized.Set();
                    return;
                }

                Volatile.Write(ref receiver, window);
                try
                {
                    if (Volatile.Read(ref stopping) == 0)
                    {
                        // MOD_NOREPEAT | CTRL | ALT. A second companion can fall back to F10.
                        if (RegisterHotKey(window, KeyId, 0x4003, 0x78))
                            shortcut = "Ctrl + Alt + F9";
                        else if (RegisterHotKey(window, KeyId, 0x4003, 0x79))
                            shortcut = "Ctrl + Alt + F10";
                        registered = shortcut.Length != 0;
                    }
                    initialized.Set();
                    while (registered && Volatile.Read(ref stopping) == 0 && GetMessage(out var message, window, 0, 0) > 0)
                    {
                        if (message.Id == StopMessage)
                            break;
                        if (message.Id == HotkeyMessage && message.WParam.ToInt64() == KeyId)
                            Interlocked.Exchange(ref toggle, 1);
                    }
                }
                finally
                {
                    if (registered)
                        UnregisterHotKey(window, KeyId);
                    registered = false;
                    Volatile.Write(ref receiver, IntPtr.Zero);
                    DestroyWindow(window);
                }
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref stopping, 1);
                var window = Volatile.Read(ref receiver);
                if (window != IntPtr.Zero)
                    PostMessage(window, StopMessage, IntPtr.Zero, IntPtr.Zero);
                if (thread.Join(1500))
                    initialized.Dispose();
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct Message
            {
                public IntPtr Window;
                public uint Id;
                public IntPtr WParam, LParam;
                public uint Time;
                public Point Pointer;
                public uint Private;
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr CreateWindowEx(uint exStyle, string className, string name, uint style,
                int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
            [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr window);
            [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
            [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
            [DllImport("user32.dll", EntryPoint = "GetMessageW")]
            private static extern int GetMessage(out Message message, IntPtr window, uint minimum, uint maximum);
            [DllImport("user32.dll", EntryPoint = "PostMessageW")]
            private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        }
#else
        public bool CanRecover => false;
        public string RecoveryShortcut => "";
        public bool ConsumeToggle() => false;
        public static bool AllowsAnimations() => true;
        public void Initialize() { }
        public void Position() { }
        public void SetMode(bool shown, bool interactive) { }
        public void BeginDrag() { }
        public void Drag() { }
        public void EndDrag() { }
        public void Dispose() { }
#endif
    }
}
