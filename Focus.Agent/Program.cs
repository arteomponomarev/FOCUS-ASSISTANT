using System.Net;
using Focus.Agent.Collectors;
using Focus.Agent.Hooks;
using Focus.Agent.Services;
using Focus.Agent.Sync;

namespace Focus.Agent;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        const string usage = "Usage: Focus.Agent --api <origin> --session <uuid> (--collect [--avatar] | --watch) [--vision [--camera <0-15>] [--fps <1-10>]]";
        if (args.Length == 0 || args is ["--help"])
        {
            Console.WriteLine(usage);
            return 0;
        }

        if (args is not ["--api", var address, "--session", var value, "--collect" or "--watch", ..] ||
            !ParseOptions(args.AsSpan(5), out var avatar, out var vision, out var index, out var fps) ||
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

        var collect = args[4] == "--collect";
        avatar |= !collect;
        if (collect)
        {
            Console.WriteLine("This Agent captures aggregate Windows activity only during the selected session.");
            Console.Write("Enter START to authorize this run: ");
            if (Console.ReadLine() != "START")
                return 0;
        }
        else
        {
            Console.WriteLine("Avatar controls are enabled for this session. Activity collection is off.");
        }
        CameraOptions? camera = null;
        if (vision)
        {
            var directory = Path.GetFullPath("Focus.Vision");
            var python = Path.Combine(directory, ".venv", "Scripts", "python.exe");
            var script = Path.Combine(directory, "vision.py");
            if (!File.Exists(python) || !File.Exists(script))
            {
                Console.Error.WriteLine("Set up Focus.Vision and run the Agent from the repository root.");
                return 2;
            }

            Console.WriteLine("Camera processing is local and limited to an active, consented session. Only coarse totals are uploaded.");
            Console.Write("Enter CAMERA to authorize this run: ");
            if (Console.ReadLine() != "CAMERA")
                return 0;
            camera = new CameraOptions(python, script, index, fps);
        }

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
        })
        {
            BaseAddress = origin,
            Timeout = TimeSpan.FromSeconds(3)
        };
        using var api = new AgentApi(http, TimeProvider.System);
        try
        {
            var account = await api.SignInAsync(email, ReadPassword(), stop.Token);
            if (account.Id == Guid.Empty)
                throw new InvalidDataException("The API did not return an account identifier.");

            if (!avatar)
                await RunSourcesAsync(origin, account.Id, sessionId, api, collect, camera, stop.Token);
            else
                await RunAvatarAsync(origin, account.Id, sessionId, api, collect, camera, stop.Token);
            return 0;
        }
        catch (RemoteFailure error)
        {
            Console.Error.WriteLine($"event=api_error status={(int)error.Status}");
            if (error.Status == HttpStatusCode.TooManyRequests)
            {
                if (error.RetryAfter is { } delay)
                    Console.Error.WriteLine($"API rate limit reached. Wait at least {Math.Max(1, Math.Ceiling(delay.TotalSeconds)):0} seconds before retrying.");
                else
                    Console.Error.WriteLine("API rate limit reached. Wait one minute before retrying.");
            }
            return 1;
        }
        catch (AvatarPipeUnavailableException)
        {
            Console.Error.WriteLine("event=avatar_bridge_unavailable");
            Console.Error.WriteLine("If another Agent is paired to this session, stop it with Ctrl+C before trying again. Run the Agent and Avatar under the same Windows account.");
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

    private static async Task RunAvatarAsync(Uri origin, Guid account, Guid session, AgentApi api,
        bool collect, CameraOptions? camera, CancellationToken stop)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var bridge = new AvatarBridge(session, api).RunAsync(lifetime.Token);
        try
        {
            if (bridge.IsCompleted)
                await bridge;
            Console.WriteLine("event=avatar_bridge_started scope=current_windows_user");
            if (collect || camera is not null)
            {
                var collection = RunSourcesAsync(origin, account, session, api, collect, camera, lifetime.Token);
                if (await Task.WhenAny(collection, bridge) == bridge)
                    lifetime.Cancel();
                await collection;
            }

            await bridge;
        }
        finally
        {
            lifetime.Cancel();
            try { await bridge; }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private static bool ParseOptions(ReadOnlySpan<string> args, out bool avatar, out bool vision, out int index, out int fps)
    {
        avatar = false;
        vision = false;
        index = 0;
        fps = 5;
        var cameraOption = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--avatar" when !avatar: avatar = true; break;
                case "--vision" when !vision: vision = true; break;
                case "--camera" when i + 1 < args.Length && int.TryParse(args[i + 1], out index) && index is >= 0 and <= 15:
                    cameraOption = true;
                    i++;
                    break;
                case "--fps" when i + 1 < args.Length && int.TryParse(args[i + 1], out fps) && fps is >= 1 and <= 10:
                    cameraOption = true;
                    i++;
                    break;
                default: return false;
            }
        }
        return !cameraOption || vision;
    }

    private static async Task RunSourcesAsync(Uri origin, Guid account, Guid session, AgentApi api,
        bool collect, CameraOptions? camera, CancellationToken stop)
    {
        using var scope = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var sources = new List<Task>();
        if (collect)
            sources.Add(CollectAsync(origin, account, session, api, scope.Token));
        if (camera is not null)
            sources.Add(CollectCameraAsync(origin, account, session, api, camera, scope.Token));
        try
        {
            while (sources.Count > 0)
            {
                var completed = await Task.WhenAny(sources);
                sources.Remove(completed);
                await completed;
            }
        }
        finally
        {
            scope.Cancel();
            await Task.WhenAll(sources);
        }
    }

    private static async Task CollectCameraAsync(Uri origin, Guid account, Guid session, AgentApi api,
        CameraOptions options, CancellationToken ct)
    {
        using var queue = ActivityQueue.ForWindowsAccount(origin, account, camera: true);
        using var camera = new CameraSource(options, TimeProvider.System, Console.WriteLine);
        api.CameraStatus = () => camera.Status;
        try
        {
            await new SessionAgent(session, api, camera, queue, TimeProvider.System, Console.WriteLine, camera: true).RunAsync(ct);
        }
        finally
        {
            api.CameraStatus = null;
        }
    }

    private static async Task CollectAsync(Uri origin, Guid account, Guid session, AgentApi api, CancellationToken ct)
    {
        using var queue = ActivityQueue.ForWindowsAccount(origin, account);
        using var hooks = new GlobalInputHook();
        using var counts = new InputActivityCollector(hooks);
        var source = new TelemetryService(new ActiveWindowCollector(), new IdleTimeCollector(), counts,
            new WindowSwitchTracker(), TimeProvider.System);
        Console.WriteLine("event=agent_connected collection=awaiting_session_and_consent");
        await new SessionAgent(session, api, source, queue, TimeProvider.System, Console.WriteLine).RunAsync(ct);
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
