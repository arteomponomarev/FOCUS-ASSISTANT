using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Focus.Agent.Interop;

namespace Focus.Agent.Hooks
{
    internal sealed class GlobalInputHook : IDisposable
    {
        private readonly NativeMethods.HookProc _keyboardHookProc;
        private readonly NativeMethods.HookProc _mouseHookProc;

        private readonly ManualResetEventSlim _started = new(false);

        private Thread? _hookThread;

        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;

        private uint _hookThreadId;

        private Exception? _startupException;

        public event Action? KeyboardActivity;
        public event Action? MouseActivity;

        public GlobalInputHook()
        {
            _keyboardHookProc = KeyboardHookCallback;
            _mouseHookProc = MouseHookCallback;
        }

        public void Start()
        {
            if (_hookThread != null)
            {
                return;
            }

            _startupException = null;
            _started.Reset();

            _hookThread = new Thread(HookThreadMain)
            {
                IsBackground = true,
                Name = "Focus.GlobalInputHook"
            };

            _hookThread.Start();

            _started.Wait();

            if (_startupException != null)
            {
                _hookThread = null;

                throw new InvalidOperationException(
                    "Unable to start global input hooks.",
                    _startupException);
            }
        }

        public void Stop()
        {
            if (_hookThread == null)
            {
                return;
            }

            if (_hookThreadId != 0)
            {
                NativeMethods.PostThreadMessage(
                    _hookThreadId,
                    NativeMethods.WmQuit,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }

            _hookThread.Join();

            _hookThread = null;
            _hookThreadId = 0;
        }

        private void HookThreadMain()
        {
            try
            {
                _hookThreadId = NativeMethods.GetCurrentThreadId();

                // Создаём message queue для этого потока.
                NativeMethods.PeekMessage(
                    out _,
                    IntPtr.Zero,
                    0,
                    0,
                    NativeMethods.PmNoRemove);

                IntPtr moduleHandle =
                    NativeMethods.GetModuleHandle(null);

                _keyboardHook = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WhKeyboardLl,
                    _keyboardHookProc,
                    moduleHandle,
                    0);

                if (_keyboardHook == IntPtr.Zero)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }

                _mouseHook = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WhMouseLl,
                    _mouseHookProc,
                    moduleHandle,
                    0);

                if (_mouseHook == IntPtr.Zero)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }

                _started.Set();

                RunMessageLoop();
            }
            catch (Exception exception)
            {
                _startupException = exception;
                _started.Set();
            }
            finally
            {
                RemoveHooks();
            }
        }

        private void RunMessageLoop()
        {
            while (true)
            {
                int result = NativeMethods.GetMessage(
                    out NativeMethods.Message message,
                    IntPtr.Zero,
                    0,
                    0);

                if (result == 0)
                {
                    break;
                }

                if (result == -1)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }

                NativeMethods.TranslateMessage(ref message);
                NativeMethods.DispatchMessage(ref message);
            }
        }

        private IntPtr KeyboardHookCallback(
            int nCode,
            IntPtr wParam,
            IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();

                if (message == NativeMethods.WmKeyDown ||
                    message == NativeMethods.WmSysKeyDown)
                {
                    KeyboardActivity?.Invoke();
                }
            }

            return NativeMethods.CallNextHookEx(
                _keyboardHook,
                nCode,
                wParam,
                lParam);
        }

        private IntPtr MouseHookCallback(
            int nCode,
            IntPtr wParam,
            IntPtr lParam)
        {
            if (nCode >= 0 && IsMouseActivity(wParam.ToInt32()))
            {
                MouseActivity?.Invoke();
            }

            return NativeMethods.CallNextHookEx(
                _mouseHook,
                nCode,
                wParam,
                lParam);
        }

        private static bool IsMouseActivity(int message)
        {
            return message == NativeMethods.WmLButtonDown ||
                   message == NativeMethods.WmRButtonDown ||
                   message == NativeMethods.WmMButtonDown ||
                   message == NativeMethods.WmXButtonDown;
        }

        private void RemoveHooks()
        {
            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Stop();
            _started.Dispose();
        }
    }
}
