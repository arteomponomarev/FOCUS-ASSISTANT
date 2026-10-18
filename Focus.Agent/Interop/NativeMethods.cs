using System;
using System.Runtime.InteropServices;
using System.Text;


namespace Focus.Agent.Interop
{
    internal static class NativeMethods
    {
        internal const int WhKeyboardLl = 13;
        internal const int WhMouseLl = 14;

        internal const int WmKeyDown = 0x0100;
        internal const int WmSysKeyDown = 0x0104;

        internal const int WmLButtonDown = 0x0201;
        internal const int WmRButtonDown = 0x0204;
        internal const int WmMButtonDown = 0x0207;
        internal const int WmXButtonDown = 0x020B;
        internal const int WmMouseWheel = 0x020A;
        internal const int WmMouseHWheel = 0x020E;

        internal const uint WmQuit = 0x0012;
        internal const uint PmNoRemove = 0x0000;

        internal delegate IntPtr HookProc(
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Message
        {
            public IntPtr HWnd;
            public uint Value;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public Point Point;
            public uint Private;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(
            IntPtr hook);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(
            IntPtr hook,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetMessage(
            out Message message,
            IntPtr windowHandle,
            uint filterMin,
            uint filterMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TranslateMessage(
            ref Message message);

        [DllImport("user32.dll")]
        internal static extern IntPtr DispatchMessage(
            ref Message message);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostThreadMessage(
            uint threadId,
            uint message,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PeekMessage(
            out Message message,
            IntPtr windowHandle,
            uint filterMin,
            uint filterMax,
            uint removeMessage);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(
            string? moduleName);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextLength(
            IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder text,
            int maxCount);
    }
}