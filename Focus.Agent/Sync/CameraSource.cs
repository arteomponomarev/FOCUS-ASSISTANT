using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Focus.Agent.Sync;

internal sealed record CameraOptions(string Python, string Script, int Index, int Fps);

internal sealed class CameraSource(CameraOptions options, TimeProvider clock, Action<string> log) : ISignalSource, IDisposable
{
    private readonly object _gate = new();
    private readonly TimeSpan _gap = TimeSpan.FromSeconds(1.45 / options.Fps);
    private readonly VisionSamples _samples = new(TimeSpan.FromSeconds(1.45 / options.Fps));
    private Process? _process;
    private string _status = "Waiting";
    private DateTimeOffset _cursor;
    private DateTimeOffset _restart;
    private DateTimeOffset _lastOutput;
    private DateTimeOffset _nextPulse;
    private int _failures;
    private bool _enabled;

    public string Status => Volatile.Read(ref _status);

    public void Start()
    {
        _enabled = true;
        _restart = clock.GetUtcNow();
        _failures = 0;
        SetStatus("Starting");
    }

    public SignalSample? Sample()
    {
        if (!_enabled)
            return null;
        var now = clock.GetUtcNow();
        if (_process is null && now >= _restart)
            Launch(now);
        if (_process is not { } process)
            return null;

        try
        {
            bool stalled;
            lock (_gate)
                stalled = now - _lastOutput > TimeSpan.FromSeconds(5);
            if (process.HasExited || stalled)
            {
                Retry(now);
                return null;
            }
            if (now >= _nextPulse)
            {
                process.StandardInput.WriteLine("KEEP");
                _nextPulse = now.AddSeconds(.75);
            }
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or Win32Exception)
        {
            Retry(now);
            return null;
        }

        lock (_gate)
        {
            if (Status != "Processing")
                return null;
            var end = Precision(now - _gap);
            if (_cursor >= end)
                return null;
            var start = _cursor;
            _cursor = end;
            return new SignalSample(start, end, null, 0, 0, 0, 0, _samples.Take(start, end));
        }
    }

    public void Stop()
    {
        _enabled = false;
        Close();
        SetStatus("Waiting");
    }

    public void Dispose() => Stop();

    private void Launch(DateTimeOffset now)
    {
        var info = new ProcessStartInfo(options.Python)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "-I", "-B", options.Script, "worker", "--camera",
            options.Index.ToString(CultureInfo.InvariantCulture), "--fps", options.Fps.ToString(CultureInfo.InvariantCulture) })
            info.ArgumentList.Add(argument);

        try
        {
            var process = Process.Start(info) ?? throw new IOException("Could not launch the local camera worker.");
            lock (_gate)
            {
                _process = process;
                _lastOutput = now;
                _cursor = now;
                _samples.Clear();
            }
            SetStatus("Starting");
            process.StandardInput.AutoFlush = true;
            process.StandardInput.NewLine = "\n";
            process.StandardInput.WriteLine("START_SESSION");
            _nextPulse = now;
            _ = ReadAsync(process);
            _ = IgnoreDiagnosticsAsync(process);
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or Win32Exception)
        {
            Retry(now);
        }
    }

    private async Task ReadAsync(Process process)
    {
        // At most two output characters per message. A malformed child is treated as unavailable.
        var field = new char[1];
        try
        {
            var output = process.StandardOutput;
            while (await output.ReadAsync(field.AsMemory()) == 1)
            {
                var code = field[0];
                if (code is not ('N' or 'P' or 'F' or 'A' or 'U' or 'X') ||
                    await output.ReadAsync(field.AsMemory()) != 1 || field[0] != '\n')
                    break;
                lock (_gate)
                {
                    if (!ReferenceEquals(process, _process))
                        return;
                    var now = Precision(clock.GetUtcNow());
                    _lastOutput = now;
                    if (code == 'X')
                    {
                        _samples.Clear();
                        SetStatus("Unavailable");
                    }
                    else
                    {
                        if (Status != "Processing")
                            _cursor = now;
                        _samples.Add(now, code);
                        SetStatus("Processing");
                    }
                }
            }
        }
        catch (Exception error) when (error is IOException or InvalidOperationException)
        {
            // Pipe closure is expected on cancellation and camera revocation.
        }
    }

    private static async Task IgnoreDiagnosticsAsync(Process process)
    {
        try { await process.StandardError.BaseStream.CopyToAsync(Stream.Null); }
        catch (Exception error) when (error is IOException or InvalidOperationException) { }
    }

    private void Retry(DateTimeOffset now)
    {
        Close();
        _restart = now.AddSeconds(2 << Math.Min(4, _failures++));
        SetStatus("Unavailable");
    }

    private void Close()
    {
        Process? process;
        lock (_gate)
        {
            process = _process;
            _process = null;
            _samples.Clear();
        }
        if (process is null)
            return;
        using (process)
        {
            try
            {
                process.StandardInput.Close();
                if (!process.WaitForExit(450))
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception error) when (error is IOException or InvalidOperationException or Win32Exception) { }
        }
    }

    private void SetStatus(string value)
    {
        if (Interlocked.Exchange(ref _status, value) != value)
            log($"event=camera_state state={value}");
    }

    private static DateTimeOffset Precision(DateTimeOffset value) => new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);
}
