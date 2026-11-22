using Focus.Application.Common;
using Focus.Application.Sessions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Engine;

namespace Focus.Application.Research;

public sealed record AggregateInput(Guid ClientEventId, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    string? ActiveProcessName, int KeyboardEventCount, int MouseClickCount, int WindowSwitchCount, int IdleSeconds);

public sealed record IngestionResult(int Created, int Duplicates);

public sealed class TelemetryService(ISessionStore sessions, IResearchStore research, FocusClassifier classifier, TimeProvider clock)
{
    public const int BatchLimit = 100;
    public const int CounterLimit = 100_000;

    public Task<IngestionResult> AcceptAsync(Guid owner, Guid sessionId, IReadOnlyList<AggregateInput> items, CancellationToken ct)
    {
        if (items.Count is < 1 or > BatchLimit || items.Any(x => x is null))
            throw new UseCaseException(Failure.Invalid, "A batch requires 1 to 100 non-null items.");

        var inputs = items.Select(Normalize).OrderBy(x => x.StartedAt).ToArray();
        if (inputs.Select(x => x.ClientEventId).Distinct().Count() != inputs.Length)
            throw new UseCaseException(Failure.Invalid, "Client event IDs must be distinct within a batch.");

        return sessions.UpdateAsync(owner, sessionId, (session, token) => PersistAsync(owner, session, inputs, token), ct);
    }

    private async Task<IngestionResult> PersistAsync(Guid owner, FocusSession session, AggregateInput[] inputs, CancellationToken ct)
    {
        var consent = await research.GetConsentForUploadAsync(owner, ct)
            ?? throw new UseCaseException(Failure.Forbidden, "Telemetry collection is disabled.");
        var known = await research.FindByClientIdsAsync(session.Id, inputs.Select(x => x.ClientEventId).ToArray(), ct);
        var newItems = inputs.Where(x => !known.ContainsKey(x.ClientEventId)).ToArray();
        if (newItems.Length > 0 && await research.OverlapsAsync(session.Id, newItems, ct))
            throw UseCaseException.Conflict("Telemetry windows cannot overlap.");

        var categories = await research.GetProcessCategoriesAsync(session.WorkProfileId, ct);
        var created = 0;
        var latest = PersistencePrecision(clock.GetUtcNow());
        DateTimeOffset? previousEnd = null;
        foreach (var input in inputs)
        {
            if (known.TryGetValue(input.ClientEventId, out var existing))
            {
                if (!Matches(existing, input))
                    throw UseCaseException.Conflict("A client event ID was reused with different data.");

                continue;
            }

            if (session.Status is SessionStatus.Planned or SessionStatus.Cancelled)
                throw UseCaseException.Conflict("Telemetry requires a started, non-cancelled session.");

            if (session.StartedAt is not { } start || input.StartedAt < start || input.StartedAt < consent ||
                input.EndedAt > (session.EndedAt ?? latest))
                throw new UseCaseException(Failure.Unprocessable, "The telemetry window is outside the session or consent interval.");

            if (previousEnd > input.StartedAt)
                throw UseCaseException.Conflict("Telemetry windows cannot overlap.");

            var pause = session.Pauses.FirstOrDefault(x => x.StartedAt < input.EndedAt && (x.EndedAt ?? latest) > input.StartedAt);
            if (pause is not null && (input.StartedAt < pause.StartedAt || input.EndedAt > (pause.EndedAt ?? latest)))
                throw new UseCaseException(Failure.Unprocessable, "A telemetry window cannot cross a pause boundary.");

            var activity = new ActivityAggregate(session.Id, input.StartedAt, input.EndedAt, input.ActiveProcessName,
                input.KeyboardEventCount, input.MouseClickCount, input.WindowSwitchCount, input.IdleSeconds, input.ClientEventId);
            var category = input.ActiveProcessName is { } name ? categories.GetValueOrDefault(name) : ProcessCategory.Neutral;
            var estimate = classifier.Evaluate(new SignalWindow((input.EndedAt - input.StartedAt).TotalSeconds,
                input.KeyboardEventCount, input.MouseClickCount, input.WindowSwitchCount, input.IdleSeconds, pause is not null, category));
            var state = new FocusStateRecord(session.Id, input.StartedAt, input.EndedAt,
                estimate.State, estimate.RuleVersion, estimate.Reason, activity.Id, category);
            research.Add(activity, state);
            created++;
            previousEnd = input.EndedAt;
        }

        return new IngestionResult(created, inputs.Length - created);
    }

    private static AggregateInput Normalize(AggregateInput input)
    {
        var start = PersistencePrecision(input.StartedAt);
        var end = PersistencePrecision(input.EndedAt);
        var duration = end - start;
        if (input.ClientEventId == Guid.Empty || duration <= TimeSpan.Zero || duration > TimeSpan.FromMinutes(5) ||
            input.IdleSeconds < 0 || input.IdleSeconds > duration.TotalSeconds ||
            !ValidCounter(input.KeyboardEventCount) || !ValidCounter(input.MouseClickCount) || !ValidCounter(input.WindowSwitchCount) ||
            !ProcessIdentity.TryNormalize(input.ActiveProcessName, out var process))
            throw new UseCaseException(Failure.Invalid, "A telemetry item contains an invalid ID, interval, process name, or counter.");

        return input with { StartedAt = start, EndedAt = end, ActiveProcessName = process };
    }

    private static bool ValidCounter(int value) => value is >= 0 and <= CounterLimit;

    private static DateTimeOffset PersistencePrecision(DateTimeOffset value) =>
        new(value.UtcTicks - (value.UtcTicks % 10), TimeSpan.Zero);

    private static bool Matches(ActivityAggregate expected, AggregateInput input) =>
        expected.StartedAt == input.StartedAt && expected.EndedAt == input.EndedAt &&
        expected.ActiveProcessName == input.ActiveProcessName && expected.KeyboardEventCount == input.KeyboardEventCount &&
        expected.MouseClickCount == input.MouseClickCount && expected.WindowSwitchCount == input.WindowSwitchCount &&
        expected.IdleSeconds == input.IdleSeconds;
}
