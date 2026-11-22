using Focus.Agent.Collectors;
using Focus.Agent.Hooks;
using Focus.Agent.Services;
using Focus.Agent.Sync;

namespace Focus.Agent;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        const string usage = "Usage: Focus.Agent --api <origin> --session <uuid> --collect";
        if (args.Length == 0 || args is ["--help"])
        {
            Console.WriteLine(usage);
            return 0;
        }

        if (args is not ["--api", var address, "--session", var value, "--collect"] ||
            !Guid.TryParse(value, out var sessionId) || sessionId == Guid.Empty)
        {
            Console.Error.WriteLine(usage);
            return 2;
        }

        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Connect from an interactive Windows terminal.");
            return 2;
        }

        Uri origin;
        try
        {
            origin = AgentApi.ParseOrigin(address);
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("Use HTTPS or loopback HTTP without credentials or a path.");
            return 2;
        }

        Console.WriteLine("This Agent captures aggregate Windows activity only during the selected session.");
        Console.Write("Enter START to authorize this run: ");
        if (Console.ReadLine() != "START")
            return 0;

        Console.Write("Account email: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(email))
            return 2;

        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };

        using var http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = !origin.IsLoopback
        }) { BaseAddress = origin, Timeout = TimeSpan.FromSeconds(3) };
        using var api = new AgentApi(http, TimeProvider.System);
        try
        {
            var account = await api.SignInAsync(email, ReadPassword(), stop.Token);
            if (account.Id == Guid.Empty)
                throw new InvalidDataException("The API did not return an account identifier.");

            using var queue = ActivityQueue.ForWindowsAccount(origin, account.Id);
            using var hooks = new GlobalInputHook();
            using var counts = new InputActivityCollector(hooks);
            var source = new TelemetryService(new ActiveWindowCollector(), new IdleTimeCollector(), counts,
                new WindowSwitchTracker(), TimeProvider.System);
            Console.WriteLine("event=agent_connected collection=awaiting_session_and_consent");
            await new SessionAgent(sessionId, api, source, queue, TimeProvider.System, Console.WriteLine).RunAsync(stop.Token);
            return 0;
        }
        catch (RemoteFailure error)
        {
            Console.Error.WriteLine($"event=api_error status={(int)error.Status}");
            return 1;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"event=agent_stopped reason={error.GetType().Name}");
            return 1;
        }
        finally
        {
            using var logout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                await api.SignOutAsync(logout.Token);
            }
            catch (Exception error) when (error is RemoteFailure or HttpRequestException or OperationCanceledException)
            {
                Console.WriteLine("event=logout_unconfirmed");
            }
        }
    }

    private static string ReadPassword()
    {
        Console.Write("Password: ");
        var buffer = new char[128];
        var length = 0;
        try
        {
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                    return new string(buffer, 0, length);
                if (key.Key == ConsoleKey.Backspace && length > 0)
                    buffer[--length] = '\0';
                if (!char.IsControl(key.KeyChar) && length < buffer.Length)
                    buffer[length++] = key.KeyChar;
            }
        }
        finally
        {
            Array.Clear(buffer);
            Console.WriteLine();
        }
    }
}
