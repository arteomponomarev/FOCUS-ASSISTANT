using Focus.Agent.Collectors;
using Focus.Agent.Hooks;

var activeWindowCollector = new ActiveWindowCollector();
var idleTimeCollector = new IdleTimeCollector();

using var globalInputHook = new GlobalInputHook();
using var inputActivityCollector =
    new InputActivityCollector(globalInputHook);

using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

globalInputHook.Start();

Console.WriteLine("Focus Agent started.");
Console.WriteLine("Press Ctrl+C to stop.");

try
{
    while (!cancellationTokenSource.IsCancellationRequested)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(5),
            cancellationTokenSource.Token);

        var activeWindow = activeWindowCollector.Collect();
        var idleTime = idleTimeCollector.Collect();
        var inputActivity = inputActivityCollector.SnapshotAndReset();

        Console.Clear();

        Console.WriteLine("=== Focus Agent Telemetry ===");
        Console.WriteLine();

        if (activeWindow != null)
        {
            Console.WriteLine($"Process:  {activeWindow.ProcessName}");
            Console.WriteLine($"Window:   {activeWindow.WindowTitle}");
        }
        else
        {
            Console.WriteLine("Process:  unavailable");
            Console.WriteLine("Window:   unavailable");
        }

        Console.WriteLine($"Idle:     {idleTime.TotalSeconds:F1} sec");
        Console.WriteLine($"Keyboard: {inputActivity.KeyboardEvents}");
        Console.WriteLine($"Mouse:    {inputActivity.MouseEvents}");

        Console.WriteLine();
        Console.WriteLine("Press Ctrl+C to stop.");
    }
}
catch (TaskCanceledException)
{
    // Normal shutdown after Ctrl+C.
}

globalInputHook.Stop();