using System.Diagnostics;
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
                    WindowHandle = windowHandle
                };
            }
            catch
            {
                return null;
            }
        }

    }
}
