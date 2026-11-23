using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Text.Json;
using Focus.Avatar.Protocol;

namespace Focus.Agent.Sync;

internal interface IAvatarEndpoint
{
    Task<AvatarMessage> ReadAvatarAsync(Guid session, CancellationToken ct);
    Task TransitionAvatarAsync(Guid session, string action, CancellationToken ct);
}

internal sealed class AvatarBridge(Guid session, IAvatarEndpoint endpoint)
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    public async Task RunAsync(CancellationToken stop)
    {
        using var listener = LocalAvatarPipe.Open(AvatarProtocol.PipeName(session));
        while (!stop.IsCancellationRequested)
        {
            var connected = false;
            try
            {
                await listener.WaitForConnectionAsync(stop);
                connected = true;
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
                deadline.CancelAfter(TimeSpan.FromSeconds(8));
                var request = await AvatarProtocol.ReadAsync(listener, deadline.Token);
                var reply = await HandleAsync(Encoding.ASCII.GetString(request), deadline.Token);
                await AvatarProtocol.WriteAsync(listener, JsonSerializer.SerializeToUtf8Bytes(reply, Json), deadline.Token);
                // The peer acknowledges receipt before this server reuses the pipe instance.
                await listener.ReadExactlyAsync(new byte[1], deadline.Token);
            }
            catch (IOException) { }
            catch (OperationCanceledException) when (!stop.IsCancellationRequested) { }
            finally
            {
                if (connected)
                    listener.Disconnect();
            }
        }
    }

    private async Task<AvatarMessage> HandleAsync(string action, CancellationToken ct)
    {
        try
        {
            if (action == AvatarProtocol.Snapshot)
            {
                var snapshot = await endpoint.ReadAvatarAsync(session, ct);
                return snapshot.IsValid(session) ? snapshot : AvatarMessage.Reply(session, "offline");
            }

            if (!AvatarProtocol.IsCommand(action))
                return AvatarMessage.Reply(session, "rejected");

            await endpoint.TransitionAvatarAsync(session, action, ct);
            return AvatarMessage.Reply(session, "accepted");
        }
        catch (RemoteFailure error)
        {
            var result = error.Status switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "unauthorized",
                HttpStatusCode.NotFound => "missing",
                HttpStatusCode.Conflict => "conflict",
                HttpStatusCode.TooManyRequests => "limited",
                _ => (int)error.Status >= 500 ? "offline" : "rejected"
            };
            return AvatarMessage.Reply(session, result);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
                                      error is OperationCanceledException && !ct.IsCancellationRequested)
        {
            return AvatarMessage.Reply(session, "offline");
        }
    }
}
