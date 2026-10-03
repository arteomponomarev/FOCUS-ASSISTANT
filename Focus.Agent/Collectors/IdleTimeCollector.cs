using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Focus.Agent.Collectors
{
    internal sealed class IdleTimeCollector
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint Size;
            public uint Time;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

        public TimeSpan Collect()
        {
            var lastInputInfo = new LastInputInfo
            {
                Size = (uint)Marshal.SizeOf<LastInputInfo>()
            };

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            uint currentTick = unchecked((uint)Environment.TickCount);
            uint idleMilliseconds = unchecked(currentTick - lastInputInfo.Time);

            return TimeSpan.FromMilliseconds(idleMilliseconds);
        }
    }
}