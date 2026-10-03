using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Focus.Agent.Models;

namespace Focus.Agent.Collectors
{
    internal class ActiveWindowCollector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public ActiveWindowInfo? Collect()
        {
            IntPtr activeWindowHandle = GetForegroundWindow();
            if (activeWindowHandle == IntPtr.Zero)
            {
                Console.WriteLine("No active window detected.");
                return null;
            }
            GetWindowThreadProcessId(activeWindowHandle, out uint processId);
            try
            {
                using Process activeProcess =
                    Process.GetProcessById((int)processId);

                return new ActiveWindowInfo
                {
                    ProcessId = (int)processId,
                    ProcessName = activeProcess.ProcessName,
                    WindowTitle = activeProcess.MainWindowTitle
                };
            }
            catch
            {
                return null;
            }
        }
        

    }
}
