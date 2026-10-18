using Focus.Agent.Models;
using System.Threading;

namespace Focus.Agent.Collectors
{
    internal sealed class WindowSwitchTracker
    {
        private nint _lastWindowHandle;
        private int _switchCount;

        public void Observe(ActiveWindowInfo? activeWindow)
        {
            if (activeWindow == null ||
                activeWindow.WindowHandle == nint.Zero)
            {
                return;
            }

            if (_lastWindowHandle != nint.Zero &&
                _lastWindowHandle != activeWindow.WindowHandle)
            {
                Interlocked.Increment(ref _switchCount);
            }

            _lastWindowHandle = activeWindow.WindowHandle;
        }

        public int SnapshotAndReset()
        {
            return Interlocked.Exchange(ref _switchCount, 0);
        }
    }
}