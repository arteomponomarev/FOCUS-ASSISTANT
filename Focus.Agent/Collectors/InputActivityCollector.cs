using System;
using System.Threading;
using Focus.Agent.Hooks;
using Focus.Agent.Models;

namespace Focus.Agent.Collectors
{
    internal sealed class InputActivityCollector : IDisposable
    {
        private readonly GlobalInputHook _inputHook;

        private int _keyboardEventCount;
        private int _mouseEventCount;

        public InputActivityCollector(GlobalInputHook inputHook)
        {
            _inputHook = inputHook;

            _inputHook.KeyboardActivity += OnKeyboardActivity;
            _inputHook.MouseActivity += OnMouseActivity;
        }

        public void Start()
        {
            _inputHook.Start();
        }

        public void Stop()
        {
            _inputHook.Stop();
        }

        public InputActivitySnapshot SnapshotAndReset()
        {
            return new InputActivitySnapshot
            {
                KeyboardEvents =
                    Interlocked.Exchange(ref _keyboardEventCount, 0),

                MouseEvents =
                    Interlocked.Exchange(ref _mouseEventCount, 0)
            };
        }

        private void OnKeyboardActivity()
        {
            Interlocked.Increment(ref _keyboardEventCount);
        }

        private void OnMouseActivity()
        {
            Interlocked.Increment(ref _mouseEventCount);
        }

        public void Dispose()
        {
            _inputHook.KeyboardActivity -= OnKeyboardActivity;
            _inputHook.MouseActivity -= OnMouseActivity;

        }
    }
}