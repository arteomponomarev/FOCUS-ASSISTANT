using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Focus.Agent.Sync;

internal static class LocalAvatarPipe
{
    public static NamedPipeServerStream Open(string name) => OperatingSystem.IsWindows()
        ? OpenWindows(name)
        : new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    [SupportedOSPlatform("windows")]
    private static NamedPipeServerStream OpenWindows(string name)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User?.Value ?? throw new UnauthorizedAccessException("No Windows user SID.");
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor($"D:P(A;;GA;;;{sid})", 1, out var descriptor, out _))
            throw new Win32Exception();
        try
        {
            var security = new SecurityAttributes { Size = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
            const uint duplexOverlappedFirst = 0x00000003 | 0x40000000 | 0x00080000;
            const uint rejectRemoteClients = 0x00000008;
            var pipe = CreateNamedPipe($@"\\.\pipe\{name}", duplexOverlappedFirst, rejectRemoteClients, 1, 8192, 8192, 0, ref security);
            if (pipe.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                pipe.Dispose();
                throw new Win32Exception(error);
            }

            try
            {
                return new NamedPipeServerStream(PipeDirection.InOut, isAsync: true, isConnected: false, pipe);
            }
            catch
            {
                pipe.Dispose();
                throw;
            }
        }
        finally
        {
            LocalFree(descriptor);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Size;
        public IntPtr Descriptor;
        public int InheritHandle;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        string text, uint revision, out IntPtr descriptor, out uint size);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafePipeHandle CreateNamedPipe(string name, uint openMode, uint pipeMode,
        uint maximumInstances, uint outputBuffer, uint inputBuffer, uint timeout, ref SecurityAttributes security);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
