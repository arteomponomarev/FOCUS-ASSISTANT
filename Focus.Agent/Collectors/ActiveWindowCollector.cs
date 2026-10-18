using System.Diagnostics;
using System.Text;
using Focus.Agent.Interop;
using Focus.Agent.Models;

namespace Focus.Agent.Collectors
{
    internal sealed class ActiveWindowCollector
    {
        public ActiveWindowInfo? Collect()
        {
            IntPtr windowHandle =
                NativeMethods.GetForegroundWindow();

            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            NativeMethods.GetWindowThreadProcessId(
                windowHandle,
                out uint processId);

            try
            {
                using Process process =
                    Process.GetProcessById((int)processId);

                return new ActiveWindowInfo
                {
                    ProcessId = (int)processId,
                    ProcessName = process.ProcessName,
                    WindowTitle = GetWindowTitle(windowHandle),
                    WindowHandle = windowHandle
                };
            }
            catch
            {
                return null;
            }
        }

        private static string GetWindowTitle(
            IntPtr windowHandle)
        {
            int length =
                NativeMethods.GetWindowTextLength(windowHandle);

            if (length <= 0)
            {
                return string.Empty;
            }

            var buffer = new StringBuilder(length + 1);

            NativeMethods.GetWindowText(
                windowHandle,
                buffer,
                buffer.Capacity);

            return buffer.ToString();
        }
    }
}