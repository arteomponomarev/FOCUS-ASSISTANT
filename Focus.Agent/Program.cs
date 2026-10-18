using Focus.Agent.Collectors;
using Focus.Agent.Hooks;
using Focus.Agent.Services;
using Focus.Agent.Storage;

var activeWindowCollector = new ActiveWindowCollector();
var idleTimeCollector = new IdleTimeCollector();
var windowSwitchTracker = new WindowSwitchTracker();

using var globalInputHook = new GlobalInputHook();
using var inputActivityCollector =
    new InputActivityCollector(globalInputHook);

var telemetryService = new TelemetryService(
    activeWindowCollector,
    idleTimeCollector,
    inputActivityCollector,
    windowSwitchTracker);

var telemetryStore = new JsonTelemetryStore();

using var cancellationTokenSource =
    new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    // Не даём процессу завершиться мгновенно.
    eventArgs.Cancel = true;

    cancellationTokenSource.Cancel();
};

var interval = TimeSpan.FromSeconds(15);
var samplingInterval = TimeSpan.FromSeconds(1);

// Для тестирования оставляем 5 секунд.
// Позже вынесем в конфигурацию и увеличим.
var idleThreshold = TimeSpan.FromSeconds(5);

Console.WriteLine("Focus Agent starting...");
Console.WriteLine();

try
{
    globalInputHook.Start();

    Console.WriteLine("Focus Agent started.");
    Console.WriteLine(
        $"Telemetry directory: {telemetryStore.DirectoryPath}");
    Console.WriteLine(
        $"Aggregation interval: {interval.TotalSeconds:F0} sec");
    Console.WriteLine(
        $"Sampling interval: {samplingInterval.TotalSeconds:F0} sec");
    Console.WriteLine(
        $"Idle threshold: {idleThreshold.TotalSeconds:F0} sec");
    Console.WriteLine();

    Console.WriteLine("Press Ctrl+C to stop.");
    Console.WriteLine();

    while (!cancellationTokenSource.IsCancellationRequested)
    {
        var snapshot =
            await telemetryService.CollectIntervalAsync(
                interval,
                samplingInterval,
                idleThreshold,
                cancellationTokenSource.Token);

        await telemetryStore.SaveAsync(
            snapshot,
            cancellationTokenSource.Token);

        string processName =
            snapshot.DominantProcessName ?? "Unknown";

        string localTime =
            snapshot.EndedAt
                .ToLocalTime()
                .ToString("HH:mm:ss");

        Console.WriteLine(
            $"[{localTime}] " +
            $"{processName} | " +
            $"Idle: {snapshot.IdleDurationSeconds:F1}s " +
            $"({snapshot.IdleRatio:P0}) | " +
            $"Keyboard: {snapshot.KeyboardEvents} | " +
            $"Mouse: {snapshot.MouseEvents} | " +
            $"Switches: {snapshot.WindowSwitches}");
    }
}
catch (OperationCanceledException)
{
    // Нормальное завершение через Ctrl+C.
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        $"Focus Agent error: {exception.Message}");
}
finally
{
    globalInputHook.Stop();

    Console.WriteLine();
    Console.WriteLine("Focus Agent stopped.");
}