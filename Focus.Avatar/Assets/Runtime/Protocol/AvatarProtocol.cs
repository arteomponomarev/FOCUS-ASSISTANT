using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Avatar.Protocol
{
    public static class AvatarProtocol
    {
        public const int Version = 1;
        public const string Snapshot = "snapshot";
        public const string Start = "start";
        public const string Pause = "pause";
        public const string Resume = "resume";
        private const int MaximumFrame = 8192;

        public static string PipeName(Guid session) => $"focus-assistant.avatar.v1.{session:N}";

        public static bool CanSend(string action, string status) =>
            (action == Start && status == "Planned") ||
            (action == Pause && status == "Active") ||
            (action == Resume && status == "Paused");

        public static bool IsCommand(string action) => action == Start || action == Pause || action == Resume;

        public static async Task<byte[]> ReadAsync(Stream stream, CancellationToken cancellation)
        {
            var header = new byte[2];
            await ReadFullyAsync(stream, header, cancellation).ConfigureAwait(false);
            var length = (header[0] << 8) | header[1];
            if (length < 1 || length > MaximumFrame)
                throw new InvalidDataException("Invalid Avatar IPC frame length.");

            var payload = new byte[length];
            await ReadFullyAsync(stream, payload, cancellation).ConfigureAwait(false);
            return payload;
        }

        public static async Task WriteAsync(Stream stream, byte[] payload, CancellationToken cancellation)
        {
            if (payload.Length < 1 || payload.Length > MaximumFrame)
                throw new InvalidDataException("Invalid Avatar IPC frame length.");

            var header = new[] { (byte)(payload.Length >> 8), (byte)payload.Length };
            await stream.WriteAsync(header, 0, header.Length, cancellation).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length, cancellation).ConfigureAwait(false);
            await stream.FlushAsync(cancellation).ConfigureAwait(false);
        }

        private static async Task ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken cancellation)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellation)
                    .ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
            }
        }
    }

    [Serializable]
    public sealed class AvatarMessage
    {
        public int version;
        public string sessionId = "";
        public string result = "";
        public string status = "";
        public string estimate = "";
        public string ruleVersion = "";
        public string goal = "";
        public string visibility = "OnlyWhenNeeded";
        public string intensity = "Balanced";
        public bool telemetryAllowed;
        public bool hasTiming;
        public double activeSeconds;
        public double plannedSeconds;

        public bool IsValid(Guid session) =>
            version == AvatarProtocol.Version && Guid.TryParse(sessionId, out var received) && received == session &&
            result is "ok" or "accepted" or "offline" or "unauthorized" or "missing" or "conflict" or "limited" or "rejected" &&
            status is "" or "Planned" or "Active" or "Paused" or "Completed" or "Cancelled" &&
            (estimate == "" || (status == "Active" && telemetryAllowed &&
                estimate is "Neutral" or "Focused" or "Distracted" or "Idle" or "Break" or "Absent")) &&
            ruleVersion != null && ruleVersion.Length <= 128 && (estimate == "" || ruleVersion.Length > 0) &&
            goal != null && goal.Length <= 500 &&
            visibility is "AlwaysVisible" or "OnlyWhenNeeded" or "NotificationsOnly" or "Hidden" &&
            intensity is "Quiet" or "Balanced" or "Active" &&
            (!hasTiming || (result == "ok" && activeSeconds >= 0 && activeSeconds < TimeSpan.MaxValue.TotalSeconds &&
                            plannedSeconds > 0 && plannedSeconds < TimeSpan.MaxValue.TotalSeconds)) &&
            (result == "ok" ? status.Length > 0 : status == "" && goal.Length == 0);

        public static AvatarMessage Reply(Guid session, string result) =>
            new AvatarMessage { version = AvatarProtocol.Version, sessionId = session.ToString("D"), result = result };
    }
}
