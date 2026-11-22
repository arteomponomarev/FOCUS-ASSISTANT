using System.Net;

namespace Focus.Agent.Sync;

internal sealed record Delivery(int Accepted, int Rejected, bool HadWork, bool ConsentDenied);

internal sealed class OfflineSync(IAgentEndpoint endpoint, ActivityQueue queue, TimeProvider clock)
{
    public async Task<Delivery> DeliverAsync(DateTimeOffset consent, CancellationToken ct)
    {
        var batch = queue.Pending(clock.GetUtcNow(), consent);
        if (batch.Length == 0)
            return new Delivery(0, 0, false, false);

        try
        {
            await SendAsync(batch, ct);
            return new Delivery(batch.Length, 0, true, false);
        }
        catch (RemoteFailure failure) when (IsRejected(failure.Status))
        {
            if (batch.Length == 1 || failure.Status == HttpStatusCode.NotFound)
            {
                queue.Acknowledge(batch);
                return new Delivery(0, batch.Length, true, false);
            }

            var accepted = 0;
            var rejected = 0;
            foreach (var item in batch)
            {
                try
                {
                    await SendAsync([item], ct);
                    accepted++;
                }
                catch (RemoteFailure single) when (IsRejected(single.Status))
                {
                    queue.Acknowledge([item]);
                    rejected++;
                }
                catch (RemoteFailure single) when (single.Status == HttpStatusCode.Forbidden)
                {
                    return new Delivery(accepted, rejected, true, true);
                }
            }

            return new Delivery(accepted, rejected, true, false);
        }
        catch (RemoteFailure failure) when (failure.Status == HttpStatusCode.Forbidden)
        {
            return new Delivery(0, 0, true, true);
        }
    }

    private async Task SendAsync(PendingActivity[] batch, CancellationToken ct)
    {
        var receipt = await endpoint.UploadAsync(batch[0].SessionId, batch.Select(entry => entry.Item).ToArray(), ct);
        if (receipt.Created < 0 || receipt.Duplicates < 0 || (long)receipt.Created + receipt.Duplicates != batch.Length)
            throw new HttpRequestException("The API acknowledgement did not cover the batch.");

        queue.Acknowledge(batch);
    }

    private static bool IsRejected(HttpStatusCode status) =>
        status is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict
            or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnprocessableEntity;
}

internal sealed class RetrySchedule
{
    private int _failures;

    public TimeSpan Fail(Exception error)
    {
        var delay = TimeSpan.FromSeconds(1 << Math.Min(_failures++, 6));
        return error is RemoteFailure { RetryAfter: { } serverDelay } && serverDelay > delay ? serverDelay : delay;
    }

    public void Reset() => _failures = 0;
}
