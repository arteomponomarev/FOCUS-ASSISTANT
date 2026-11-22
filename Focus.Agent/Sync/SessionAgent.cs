using System.Net;
using Focus.Domain.Enums;

namespace Focus.Agent.Sync;

internal sealed class SessionAgent(
    Guid sessionId, IAgentEndpoint endpoint, ISignalSource source, ActivityQueue queue,
    TimeProvider clock, Action<string> log)
{
    private static readonly TimeSpan SamplePeriod = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan CheckPeriod = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PermissionLifetime = TimeSpan.FromSeconds(3);
    private readonly CaptureBuffer _buffer = new(sessionId, queue.Add);
    private readonly OfflineSync _sync = new(endpoint, queue, clock);
    private bool _capturing;

    public async Task RunAsync(CancellationToken ct)
    {
        using var workers = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var timer = new PeriodicTimer(SamplePeriod, clock);
        var checkRetry = new RetrySchedule();
        var uploadRetry = new RetrySchedule();
        var nextCheck = DateTimeOffset.MinValue;
        var nextUpload = DateTimeOffset.MinValue;
        var lastVerified = 0L;
        var checkStarted = 0L;
        DateTimeOffset? consent = null;
        SessionStatus? state = null;
        Task<Authority>? check = null;
        Task<Delivery>? upload = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var now = clock.GetUtcNow();
                if (upload?.IsCompleted == true)
                {
                    try
                    {
                        var result = await upload;
                        if (result.ConsentDenied)
                        {
                            Revoke();
                            return;
                        }

                        if (result.HadWork)
                            log($"event=upload accepted={result.Accepted} rejected={result.Rejected}");
                        uploadRetry.Reset();
                        nextUpload = now + CheckPeriod;
                    }
                    catch (Exception error) when (IsTransient(error, ct))
                    {
                        nextUpload = now + uploadRetry.Fail(error);
                        log("event=upload_retry");
                    }

                    upload = null;
                }

                if (check?.IsCompleted == true)
                {
                    try
                    {
                        var result = await check;
                        if (!result.Privacy.TelemetryEnabled || result.Privacy.ConsentedAt is not { } enabledAt)
                        {
                            Revoke();
                            return;
                        }

                        if (result.Session is not { } session)
                            throw new InvalidDataException("The API did not return session status.");

                        if (session.Status != SessionStatus.Active || (consent is { } previous && previous != enabledAt))
                            StopSource();

                        _buffer.Confirm(result);
                        if (session.Status == SessionStatus.Cancelled)
                            queue.Discard(sessionId);
                        if (state != session.Status)
                            log($"event=session_state state={session.Status}");

                        state = session.Status;
                        consent = enabledAt;
                        lastVerified = checkStarted;
                        checkRetry.Reset();
                        nextCheck = now + CheckPeriod;
                    }
                    catch (Exception error) when (IsTransient(error, ct))
                    {
                        StopSource();
                        _buffer.FlushConfirmed();
                        consent = null;
                        nextCheck = now + checkRetry.Fail(error);
                        log("event=authority_retry collection=paused");
                    }

                    check = null;
                }

                var fresh = consent is not null && clock.GetElapsedTime(lastVerified) < PermissionLifetime;
                var shouldCapture = fresh && state == SessionStatus.Active;
                if (shouldCapture && !_capturing)
                {
                    source.Start();
                    _capturing = true;
                    log("event=capture_started");
                }
                else if (!shouldCapture && _capturing)
                {
                    StopSource();
                    _buffer.FlushConfirmed();
                }

                if (_capturing && source.Sample() is { } sample)
                    _buffer.Add(sample);

                if (state is SessionStatus.Completed or SessionStatus.Cancelled && upload is null && queue.IsEmpty)
                    return;

                if (check is null && now >= nextCheck)
                {
                    checkStarted = clock.GetTimestamp();
                    check = endpoint.CheckAsync(sessionId, workers.Token);
                }

                if (fresh && consent is { } consentAt && upload is null && now >= nextUpload && !queue.IsEmpty)
                    upload = _sync.DeliverAsync(consentAt, workers.Token);

                await timer.WaitForNextTickAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Ctrl+C uses the same bounded cleanup path as a cancelled timer tick.
        }
        finally
        {
            try
            {
                if (ct.IsCancellationRequested && _capturing && source.Sample() is { } tail)
                    _buffer.Add(tail);
            }
            finally
            {
                StopSource();
            }
            await workers.CancelAsync();
            await ObserveAsync(check);
            await ObserveAsync(upload);
            if (ct.IsCancellationRequested)
                await FinishAsync();
            else
                _buffer.Suspend();
        }
    }

    private async Task FinishAsync()
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        try
        {
            var result = await endpoint.CheckAsync(sessionId, limit.Token);
            if (!result.Privacy.TelemetryEnabled || result.Privacy.ConsentedAt is not { } consent)
            {
                Revoke();
                return;
            }

            _buffer.Confirm(result);
            _buffer.Suspend();
            if (result.Session?.Status == SessionStatus.Cancelled)
                queue.Discard(sessionId);

            var delivery = await _sync.DeliverAsync(consent, limit.Token);
            if (delivery.ConsentDenied)
                Revoke();
            else if (delivery.HadWork)
                log($"event=shutdown_upload accepted={delivery.Accepted} rejected={delivery.Rejected}");
        }
        catch (Exception error) when (error is HttpRequestException or RemoteFailure or OperationCanceledException)
        {
            _buffer.Suspend();
            log("event=shutdown_pending");
        }
    }

    private void Revoke()
    {
        StopSource();
        _buffer.Discard();
        queue.Discard();
        log("event=telemetry_disabled action=enable_consent_and_relaunch");
    }

    private void StopSource()
    {
        if (!_capturing)
            return;
        source.Stop();
        _capturing = false;
        log("event=capture_stopped");
    }

    private static bool IsTransient(Exception error, CancellationToken ct) =>
        !ct.IsCancellationRequested && error is (HttpRequestException or OperationCanceledException or RemoteFailure { IsTransient: true });

    private static async Task ObserveAsync(Task? task)
    {
        if (task is null)
            return;
        try
        {
            await task;
        }
        catch (Exception error) when (error is HttpRequestException or RemoteFailure or OperationCanceledException)
        {
            // The queue retains batches that lack a successful acknowledgement.
        }
    }
}
