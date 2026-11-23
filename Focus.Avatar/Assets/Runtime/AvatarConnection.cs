using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Focus.Avatar.Protocol;

namespace Focus.Avatar
{
    internal static class AvatarConnection
    {
        public static async Task<string> ExchangeAsync(Guid session, string verb, CancellationToken stop)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(9));
            using var pipe = new NamedPipeClientStream(".", AvatarProtocol.PipeName(session), PipeDirection.InOut,
                PipeOptions.Asynchronous, TokenImpersonationLevel.Anonymous);
            await pipe.ConnectAsync(650, deadline.Token).ConfigureAwait(false);
            VerifyLocalOwner(pipe.SafePipeHandle);
            await AvatarProtocol.WriteAsync(pipe, Encoding.ASCII.GetBytes(verb), deadline.Token).ConfigureAwait(false);
            var reply = await AvatarProtocol.ReadAsync(pipe, deadline.Token).ConfigureAwait(false);
            await pipe.WriteAsync(new byte[] { 1 }, 0, 1, deadline.Token).ConfigureAwait(false);
            return new UTF8Encoding(false, true).GetString(reply);
        }

        private static void VerifyLocalOwner(SafePipeHandle pipe)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!GetNamedPipeServerProcessId(pipe, out var pid))
                throw new Win32Exception();
            using var process = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
            if (process.IsInvalid || !OpenProcessToken(process, 0x0008, out var token))
                throw new Win32Exception();
            using (token)
            {
                using var clientProcess = OpenProcess(0x1000, false, GetCurrentProcessId());
                if (clientProcess.IsInvalid || !OpenProcessToken(clientProcess, 0x0008, out var clientToken))
                    throw new Win32Exception();
                using (clientToken)
                {
                    var serverUser = ReadTokenUser(token);
                    try
                    {
                        var clientUser = ReadTokenUser(clientToken);
                        try
                        {
                            if (!EqualSid(Marshal.ReadIntPtr(serverUser), Marshal.ReadIntPtr(clientUser)))
                                throw new UnauthorizedAccessException("Avatar pipe belongs to a different Windows user.");
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(clientUser);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(serverUser);
                    }
                }
            }
#else
            throw new PlatformNotSupportedException("Avatar IPC is available on Windows.");
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static IntPtr ReadTokenUser(SafeFileHandle token)
        {
            const int tokenUser = 1;
            GetTokenInformation(token, tokenUser, IntPtr.Zero, 0, out var size);
            if (size <= 0 || size > 4096)
                throw new Win32Exception();

            var buffer = Marshal.AllocHGlobal(size);
            if (GetTokenInformation(token, tokenUser, buffer, size, out _))
                return buffer;
            var error = Marshal.GetLastWin32Error();
            Marshal.FreeHGlobal(buffer);
            throw new Win32Exception(error);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint pid);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(SafeFileHandle process, uint access, out SafeFileHandle token);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(SafeFileHandle token, int type, IntPtr data, int size, out int needed);

        [DllImport("advapi32.dll")]
        private static extern bool EqualSid(IntPtr first, IntPtr second);
#endif
    }
}
