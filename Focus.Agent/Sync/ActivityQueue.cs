using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Focus.Domain.Entities;

namespace Focus.Agent.Sync;

internal sealed class ActivityQueue : IDisposable
{
    private const int Capacity = 4096;
    private const int BatchSize = 20;
    private readonly object _gate = new();
    private readonly string _directory;
    private readonly Func<byte[], byte[]> _protect;
    private readonly Func<byte[], byte[]> _unprotect;
    private readonly FileStream _lease;

    internal ActivityQueue(string directory, Func<byte[], byte[]> protect, Func<byte[], byte[]> unprotect)
    {
        Directory.CreateDirectory(directory);
        _directory = directory;
        _protect = protect;
        _unprotect = unprotect;
        _lease = new FileStream(Path.Combine(directory, "agent.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        try
        {
            RecoverInterruptedWrites();
        }
        catch
        {
            _lease.Dispose();
            throw;
        }
    }

    public static ActivityQueue ForWindowsAccount(Uri origin, Guid account, string? root = null, bool camera = false)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The desktop queue requires Windows.");

        var scope = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"{origin.GetLeftPart(UriPartial.Authority)}\n{account:D}")));
        root ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusAssistant", camera ? "vision-v1" : "agent-v1");
        if (!Path.IsPathFullyQualified(root))
            throw new IOException("An absolute Windows profile path is required for the Agent queue.");
        var directory = Path.Combine(root, scope);
        return new ActivityQueue(directory, Protect, Unprotect);
    }

    public void Add(PendingActivity entry)
    {
        lock (_gate)
        {
            if (Files().Take(Capacity).Count() == Capacity)
                throw new IOException("Agent queue capacity reached. Pending records were retained.");

            var path = EntryPath(entry);
            var temp = path + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(_protect(JsonSerializer.SerializeToUtf8Bytes(entry)));
                    stream.Flush(flushToDisk: true);
                }

                File.Move(temp, path);
            }
            finally
            {
                File.Delete(temp);
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_gate)
                return !Files().Any();
        }
    }

    public PendingActivity[] Pending(DateTimeOffset now, DateTimeOffset consent)
    {
        lock (_gate)
        {
            var batch = new List<PendingActivity>();
            foreach (var path in Files().Order(StringComparer.Ordinal))
            {
                var entry = Read(path);
                if (entry.Item.StartedAt < consent || now - entry.Item.EndedAt > ActivityAggregate.MaximumUploadAge)
                    File.Delete(path);
                else if (batch.Count == 0 || entry.SessionId == batch[0].SessionId)
                    batch.Add(entry);
                if (batch.Count == BatchSize)
                    break;
            }

            return batch.ToArray();
        }
    }

    public void Acknowledge(IEnumerable<PendingActivity> entries)
    {
        lock (_gate)
        {
            foreach (var entry in entries)
                File.Delete(EntryPath(entry));
        }
    }

    public void Discard(Guid? session = null)
    {
        lock (_gate)
        {
            foreach (var path in Files())
                if (session is null || Read(path).SessionId == session)
                    File.Delete(path);
        }
    }

    private IEnumerable<string> Files() => Directory.EnumerateFiles(_directory, "*.bin");

    private void RecoverInterruptedWrites()
    {
        foreach (var temp in Directory.EnumerateFiles(_directory, "*.bin.tmp"))
        {
            var destination = temp[..^4];
            if (File.Exists(destination))
            {
                File.Delete(temp);
                continue;
            }

            try
            {
                Read(temp, destination);
            }
            catch (Exception error) when (error is CryptographicException or JsonException or InvalidDataException)
            {
                // A partially written ciphertext cannot be uploaded as an aggregate.
                File.Delete(temp);
                continue;
            }

            File.Move(temp, destination);
        }
    }

    private string EntryPath(PendingActivity entry) =>
        Path.Combine(_directory, $"{entry.Item.StartedAt.UtcTicks:D19}-{entry.Item.ClientEventId:N}.bin");

    private PendingActivity Read(string path, string? expectedPath = null)
    {
        if (new FileInfo(path).Length > 16_384)
            throw new InvalidDataException("An Agent queue record exceeds the size limit.");
        var entry = JsonSerializer.Deserialize<PendingActivity>(_unprotect(File.ReadAllBytes(path)));
        if (entry?.Item is null || entry.SessionId == Guid.Empty || entry.Item.ClientEventId == Guid.Empty ||
            !string.Equals(expectedPath ?? path, EntryPath(entry), StringComparison.OrdinalIgnoreCase))
            throw new JsonException("An Agent queue record is invalid.");

        return entry;
    }

    private static byte[] Protect(byte[] bytes) => OperatingSystem.IsWindows()
        ? ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser)
        : throw new PlatformNotSupportedException();

    private static byte[] Unprotect(byte[] bytes) => OperatingSystem.IsWindows()
        ? ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser)
        : throw new PlatformNotSupportedException();

    public void Dispose() => _lease.Dispose();
}
