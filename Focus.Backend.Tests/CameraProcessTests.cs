using System.Collections.Concurrent;
using System.Diagnostics;
using Focus.Agent.Sync;

namespace Focus.Backend.Tests;

public sealed class VisionPythonFactAttribute : FactAttribute
{
    public VisionPythonFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOCUS_VISION_TEST_PYTHON")))
            Skip = "Set FOCUS_VISION_TEST_PYTHON to a local Python executable. This fixture never opens a camera.";
    }
}

public sealed class CameraProcessTests
{
    [VisionPythonFact]
    public async Task WorkerCanReportDisconnectRecoverAndStopThroughThePrivatePipes()
    {
        var temp = Directory.CreateTempSubdirectory("focus-vision-worker-");
        var script = Path.Combine(temp.FullName, "worker-fixture.py");
        var pidPath = Path.Combine(temp.FullName, "pid.txt");
        await File.WriteAllTextAsync(script, """
            import os, pathlib, sys, threading
            if sys.stdin.readline().strip() != "START_SESSION":
                sys.exit(2)
            pathlib.Path(__file__).with_name("pid.txt").write_text(str(os.getpid()), encoding="utf-8")
            sys.stdout.reconfigure(newline="\n")
            stop = threading.Event()
            def end_on_close():
                while sys.stdin.readline():
                    pass
                stop.set()
            threading.Thread(target=end_on_close, daemon=True).start()
            for _ in range(5):
                print("P", flush=True)
                if stop.wait(.15):
                    sys.exit(0)
            print("X", flush=True)
            if stop.wait(.55):
                sys.exit(0)
            while not stop.is_set():
                print("F", flush=True)
                stop.wait(.15)
            """);
        var messages = new ConcurrentQueue<string>();
        try
        {
            using var source = new CameraSource(new CameraOptions(
                Environment.GetEnvironmentVariable("FOCUS_VISION_TEST_PYTHON")!, script, 0, 5),
                TimeProvider.System, messages.Enqueue);
            source.Start();
            await UntilAsync("Processing", source);
            await UntilAsync("Unavailable", source);
            await UntilAsync("Processing", source);
            var pid = int.Parse(await File.ReadAllTextAsync(pidPath), System.Globalization.CultureInfo.InvariantCulture);
            using var worker = Process.GetProcessById(pid);
            source.Stop();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await worker.WaitForExitAsync(stop.Token);
            Assert.Equal("Waiting", source.Status);
            Assert.All(messages, line => Assert.StartsWith("event=camera_state state=", line));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    private static async Task UntilAsync(string status, CameraSource source)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(7))
        {
            source.Sample();
            if (source.Status == status)
                return;
            await Task.Delay(55);
        }
        Assert.Equal(status, source.Status);
    }
}
