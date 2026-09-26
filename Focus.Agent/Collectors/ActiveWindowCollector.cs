using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Focus.Agent.Collectors
{
    internal class ActiveWindowCollector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public class ActiveWindowInfo
        {
            public int ProcessId { get; set; }
            public string ProcessName { get; set; } = "";
            public string WindowTitle { get; set; } = "";
        }
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
