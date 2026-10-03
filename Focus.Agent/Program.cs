using Focus.Agent.Collectors;

var collector = new ActiveWindowCollector();

while (true)
{
    var info = collector.Collect();

    if (info != null)
    {
        Console.WriteLine(
            $"{DateTime.Now:HH:mm:ss} | " +
            $"{info.ProcessName} | " +
            $"{info.WindowTitle}");
    }

    Thread.Sleep(1000);
    var idleCollector = new IdleTimeCollector();

    while (true)
    {
        TimeSpan idleTime = idleCollector.Collect();

        Console.WriteLine($"Idle: {idleTime.TotalSeconds:F1} sec");

        Thread.Sleep(1000);
    }
}
