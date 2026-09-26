using Focus.Agent.Collectors;

var collector = new ActiveWindowCollector();

var info = collector.Collect();

if (info != null)
{
    Console.WriteLine(info.ProcessName);
    Console.WriteLine(info.WindowTitle);
}