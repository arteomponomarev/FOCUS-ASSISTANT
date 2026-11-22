using Focus.Application.Accounts;
using Focus.Application.Common;
using Focus.Application.Research;
using Focus.Application.Sessions;
using Focus.Application.Settings;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Engine;

namespace Focus.Backend.Tests;

public sealed class ApplicationTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 3, 8, 6, 0, 0, TimeSpan.Zero);
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Clock TestClock = new(Epoch.AddMinutes(20));

    [Fact]
    public async Task SessionUseCasesApplyValidationAndPreserveOwnership()
    {
        var store = new SessionStore();
        var sessions = new SessionService(store, TestClock);
        await FailureAsync(Failure.Invalid, () => sessions.CreateAsync(Owner, 0, null, default));
        await FailureAsync(Failure.Invalid, () => sessions.CreateAsync(Owner, 900, ["   "], default));
        var session = await sessions.CreateAsync(Owner, 900, ["  Draft "], default);
        var goal = await sessions.AddGoalAsync(Owner, session.Id, "Review", default);
        Assert.Equal("Review", (await sessions.UpdateGoalAsync(Owner, session.Id, goal.Id, " Review ", null, default)).Description);
        await sessions.DeleteGoalAsync(Owner, session.Id, goal.Id, default);
        await FailureAsync(Failure.NotFound, () => sessions.GetAsync(Guid.NewGuid(), session.Id, default));
        await FailureAsync(Failure.NotFound, () => sessions.DeleteGoalAsync(Owner, session.Id, goal.Id, default));
        await FailureAsync(Failure.Invalid, () => sessions.UpdateGoalAsync(Owner, session.Id, goal.Id, null, GoalStatus.Pending, default));
        await FailureAsync(Failure.Invalid, () => sessions.ListAsync(Owner, Filter() with { From = Epoch, To = Epoch }, default));
        Assert.Single((await sessions.ListAsync(Owner, Filter(), default)).Items);

        await sessions.TransitionAsync(Owner, session.Id, SessionCommand.Start, default);
        await sessions.TransitionAsync(Owner, session.Id, SessionCommand.Pause, default);
        await sessions.TransitionAsync(Owner, session.Id, SessionCommand.Resume, default);
        await FailureAsync(Failure.Conflict, () => sessions.AddGoalAsync(Owner, session.Id, "Late", default));
        await FailureAsync(Failure.Invalid, () => sessions.CompleteAsync(Owner, session.Id, 6, 2, null, null, default));
        await FailureAsync(Failure.Invalid, () => sessions.CompleteAsync(Owner, session.Id, 4, 2, null, [
            new GoalOutcome(Guid.Empty, GoalStatus.Completed)], default));
        var draft = Assert.Single(session.Goals);
        session = await sessions.CompleteAsync(Owner, session.Id, 4, 2, "reflection",
            [new GoalOutcome(draft.Id, GoalStatus.Completed)], default);
        Assert.Equal(GoalStatus.Completed, Assert.Single(session.Goals).Status);
        Assert.Equal(SessionStatus.Completed, session.Status);
        await FailureAsync(Failure.Conflict, () => sessions.TransitionAsync(Owner, session.Id, SessionCommand.Cancel, default));

        var page = new Page<int>([3], 2, 25, 30).Select(x => x * 2);
        Assert.Equal(6, Assert.Single(page.Items));
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(25, new PageRequest(2, 25).Offset);
        Assert.Throws<UseCaseException>(() => new PageRequest(0, 25));
    }

    [Fact]
    public async Task AccountUseCasesNormalizeRegistrationAndRequireActiveCredentials()
    {
        var store = new AccountStore();
        var accounts = new AccountService(store, new PasswordStub(), TestClock);
        await FailureAsync(Failure.Invalid, () => accounts.RegisterAsync("test@example.invalid", "test", "  ", default));
        var profile = await accounts.RegisterAsync("  FIRST@example.invalid ", "one", " First ", default);
        Assert.Equal("first@example.invalid", profile.Email);
        Assert.Equal("First", profile.DisplayName);
        Assert.Equal(profile, await accounts.GetProfileAsync(profile.Id, default));
        Assert.Equal("Renamed", (await accounts.RenameAsync(profile.Id, " Renamed ", default)).DisplayName);
        await FailureAsync(Failure.NotFound, () => accounts.GetProfileAsync(Guid.NewGuid(), default));
        await FailureAsync(Failure.Unauthenticated, () => accounts.LoginAsync(profile.Email, "incorrect", TimeSpan.FromDays(1), default));
        var (user, login) = await accounts.LoginAsync("FIRST@example.invalid", "one", TimeSpan.FromDays(1), default);
        Assert.Equal(profile.Id, user);
        Assert.True(await accounts.IsActiveAsync(user, login, default));
        var (renewed, family) = await accounts.RefreshAsync(user, login, TimeSpan.FromDays(2), default);
        Assert.False(await accounts.IsActiveAsync(user, login, default));
        await FailureAsync(Failure.Unauthenticated, () => accounts.RefreshAsync(user, login, TimeSpan.FromDays(2), default));
        await accounts.LogoutAsync(user, family, default);
        Assert.False(await accounts.IsActiveAsync(user, renewed, default));
        await FailureAsync(Failure.Unauthenticated, () => accounts.ChangePasswordAsync(user, "incorrect", "two", default));
        await accounts.ChangePasswordAsync(user, "one", "two", default);
        await FailureAsync(Failure.Unauthenticated, () => accounts.LoginAsync(profile.Email, "one", TimeSpan.FromDays(1), default));
        Assert.False((await accounts.GetPrivacyAsync(user, default)).TelemetryEnabled);
        Assert.Equal(TestClock.GetUtcNow(), (await accounts.SetPrivacyAsync(user, true, default)).ConsentedAt);
        Assert.False((await accounts.SetPrivacyAsync(user, false, default)).TelemetryEnabled);
        await FailureAsync(Failure.Unauthenticated, () => accounts.DeleteAccountAsync(user, "invalid", default));
        await accounts.DeleteAccountAsync(user, "two", default);
        await FailureAsync(Failure.NotFound, () => accounts.GetProfileAsync(user, default));
    }

    [Fact]
    public async Task SettingsSupportIndependentUpdatesAndRejectConflictingProcessRules()
    {
        var settings = new SettingsService(new SettingsStore());
        Assert.Equal(PomodoroSettings.Default, await settings.GetPomodoroAsync(Owner, default));
        Assert.Equal(AvatarSettings.Default, await settings.GetAvatarAsync(Owner, default));
        var pomodoro = new PomodoroSettings(40, 10, 20, 2);
        await FailureAsync(Failure.Invalid, () => settings.SetPomodoroAsync(Owner, pomodoro with { LongBreakMinutes = 5 }, default));
        Assert.Equal(pomodoro, await settings.SetPomodoroAsync(Owner, pomodoro, default));
        var avatar = new AvatarSettings(AvatarVisibility.Hidden, NotificationIntensity.Quiet, "exhibit-1");
        await FailureAsync(Failure.Invalid, () => settings.SetAvatarAsync(Owner, avatar with { AvatarId = "invalid path" }, default));
        Assert.Equal(avatar, await settings.SetAvatarAsync(Owner, avatar, default));
        Assert.Equal(pomodoro, await settings.GetPomodoroAsync(Owner, default));
        Assert.Equal(avatar, await settings.GetAvatarAsync(Owner, default));

        var template = new ProfileInput(" Writing ", " Outline ", 40, 10, [new RuleInput("Editor", ProcessCategory.Allowed)]);
        await FailureAsync(Failure.Invalid, () => settings.AddProfileAsync(Owner, template with
        {
            Rules = [new RuleInput("editor", ProcessCategory.Allowed), new RuleInput("EDITOR", ProcessCategory.Neutral)]
        }, default));
        await FailureAsync(Failure.Invalid, () => settings.AddProfileAsync(Owner, template with { FocusMinutes = 0 }, default));
        var created = await settings.AddProfileAsync(Owner, template, default);
        Assert.Equal("Writing", created.Name);
        Assert.Equal("editor", Assert.Single(created.Rules).ProcessName);
        var changed = await settings.SetProfileAsync(Owner, created.Id, template with
        {
            Rules = [new RuleInput("editor", ProcessCategory.Neutral), new RuleInput("terminal", ProcessCategory.Allowed)]
        }, default);
        Assert.Equal(ProcessCategory.Neutral, changed.Rules.Single(x => x.ProcessName == "editor").Category);
        changed = await settings.SetProfileAsync(Owner, created.Id, template, default);
        Assert.Single(changed.Rules);
        Assert.Equal(created.Id, (await settings.GetProfileAsync(Owner, created.Id, default)).Id);
        Assert.Single((await settings.ListProfilesAsync(Owner, new PageRequest(1, 25), default)).Items);
        await settings.DeleteProfileAsync(Owner, created.Id, default);
        await FailureAsync(Failure.NotFound, () => settings.GetProfileAsync(Owner, created.Id, default));
    }

    [Fact]
    public async Task TelemetryNormalizesSignalsAndHonorsStableClientIdentifiers()
    {
        var sessionStore = new SessionStore();
        var session = new FocusSession(Owner, TimeSpan.FromMinutes(40), Epoch);
        await sessionStore.AddAsync(session, default);
        session.Start(Epoch);
        session.Pause(Epoch.AddMinutes(3));
        session.Resume(Epoch.AddMinutes(4));
        var source = new ResearchStore { Consent = Epoch };
        var service = new TelemetryService(sessionStore, source, new FocusClassifier(), TestClock);
        var activity = new AggregateInput(Guid.NewGuid(), Epoch, Epoch.AddMinutes(1), "  EDITOR  ", 4, 1, 0, 0);

        await FailureAsync(Failure.Invalid, () => service.AcceptAsync(Owner, session.Id, [], default));
        await FailureAsync(Failure.Invalid, () => service.AcceptAsync(Owner, session.Id, [activity, activity], default));
        await FailureAsync(Failure.Invalid, () => service.AcceptAsync(Owner, session.Id, [activity with { ActiveProcessName = "C:/editor" }], default));
        await FailureAsync(Failure.Invalid, () => service.AcceptAsync(Owner, session.Id, [activity with { IdleSeconds = 61 }], default));
        await FailureAsync(Failure.Unprocessable, () => service.AcceptAsync(Owner, session.Id, [
            activity with { StartedAt = Epoch.AddMinutes(2.5), EndedAt = Epoch.AddMinutes(3.5) }], default));
        await FailureAsync(Failure.Unprocessable, () => service.AcceptAsync(Owner, session.Id, [
            activity with { StartedAt = Epoch.AddSeconds(-1) }], default));
        var pause = activity with { ClientEventId = Guid.NewGuid(), StartedAt = Epoch.AddMinutes(3), EndedAt = Epoch.AddMinutes(4) };
        Assert.Equal(new IngestionResult(2, 0), await service.AcceptAsync(Owner, session.Id, [pause, activity], default));
        Assert.Equal("editor", source.Activities[0].ActiveProcessName);
        Assert.Equal(FocusStateType.Focused, source.Estimates[0].State);
        Assert.Equal(FocusStateType.Break, source.Estimates[1].State);
        Assert.Equal(source.Activities[0].Id, source.Estimates[0].ActivityAggregateId);
        Assert.Equal(new IngestionResult(0, 1), await service.AcceptAsync(Owner, session.Id, [activity], default));
        await FailureAsync(Failure.Conflict, () => service.AcceptAsync(Owner, session.Id, [activity with { KeyboardEventCount = 5 }], default));
        await FailureAsync(Failure.Conflict, () => service.AcceptAsync(Owner, session.Id, [activity with { ClientEventId = Guid.NewGuid() }], default));
        source.Consent = null;
        await FailureAsync(Failure.Forbidden, () => service.AcceptAsync(Owner, session.Id, [activity], default));
    }

    [Fact]
    public async Task ReportsSplitDstDaysAndClipOverlappingLegacyEstimates()
    {
        var store = new SessionStore();
        var session = new FocusSession(Owner, TimeSpan.FromMinutes(40), Epoch);
        session.AddGoal("Write");
        session.Start(Epoch);
        session.Complete(5, null, 3, Epoch.AddMinutes(20));
        await store.AddAsync(session, default);

        var source = new AnalyticsStore(session, [
            new ObservedActivity(session.Id, Epoch, 11, 3, 2, 1)
        ], [
            new EstimatedInterval(session.Id, Epoch, Epoch.AddMinutes(10), FocusStateType.Focused),
            new EstimatedInterval(session.Id, Epoch.AddMinutes(5), Epoch.AddMinutes(15), FocusStateType.Distracted),
            new EstimatedInterval(session.Id, Epoch.AddMinutes(8), Epoch.AddMinutes(9), FocusStateType.Idle)
        ]);
        var analytics = new AnalyticsService(source, new SessionService(store, TestClock), TestClock);
        var from = new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero);
        var to = from.AddHours(23);
        var summary = Assert.Single((await analytics.GetReportAsync(Owner, from, to, "America/New_York", ReportPeriod.Day, default)).Buckets);
        Assert.Equal(23, (summary.To - summary.From).TotalHours);
        Assert.Equal(600, summary.Metrics.EstimatedStateSeconds[FocusStateType.Focused]);
        Assert.Equal(300, summary.Metrics.EstimatedStateSeconds[FocusStateType.Distracted]);
        Assert.Equal(0, summary.Metrics.EstimatedStateSeconds[FocusStateType.Idle]);
        Assert.Equal(1200, summary.Metrics.TrackedActiveSeconds);
        Assert.Equal(5, summary.Metrics.AverageSelfReportedConcentration);
        Assert.Equal(3, summary.Metrics.AverageSelfReportedProductivity);
        Assert.Equal(11, summary.Metrics.KeyboardEvents);
        Assert.Single((await analytics.ResearchAsync(Owner, Filter(), default)).Items);
        Assert.Empty((await analytics.ResearchAsync(Guid.NewGuid(), Filter(), default)).Items);

        var week = await analytics.GetReportAsync(Owner, from, to.AddDays(7), "America/New_York", ReportPeriod.Week, default);
        Assert.True(week.Buckets.Count > 1);
        var month = await analytics.GetReportAsync(Owner, from, to.AddDays(35), "UTC", ReportPeriod.Month, default);
        Assert.Equal(2, month.Buckets.Count);
        var empty = await analytics.GetReportAsync(Guid.NewGuid(), null, null, null, null, default);
        Assert.Null(Assert.Single(empty.Buckets).Metrics.AverageSelfReportedConcentration);
        await FailureAsync(Failure.Invalid, () => analytics.GetReportAsync(Owner, from, to, "Bad/Zone", null, default));
        await FailureAsync(Failure.Invalid, () => analytics.GetReportAsync(Owner, to, from, "UTC", null, default));
    }

    private static async Task FailureAsync(Failure expected, Func<Task> operation) =>
        Assert.Equal(expected, (await Assert.ThrowsAsync<UseCaseException>(operation)).Failure);

    private static SessionFilter Filter() => new(new PageRequest(1, 25), null, null, null, true);

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class SessionStore : ISessionStore
    {
        private readonly List<FocusSession> _sessions = [];

        public Task AddAsync(FocusSession session, CancellationToken ct)
        {
            _sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<FocusSession?> FindAsync(Guid owner, Guid id, CancellationToken ct) =>
            Task.FromResult(_sessions.SingleOrDefault(x => x.UserId == owner && x.Id == id));

        public Task<Page<FocusSession>> ListAsync(Guid owner, SessionFilter filter, CancellationToken ct)
        {
            var matches = _sessions.Where(x => x.UserId == owner).ToArray();
            return Task.FromResult(new Page<FocusSession>(matches, filter.Page.Number, filter.Page.Size, matches.Length));
        }

        public async Task<T> UpdateAsync<T>(Guid owner, Guid id, Func<FocusSession, CancellationToken, Task<T>> update, CancellationToken ct) =>
            await update(await FindAsync(owner, id, ct) ?? throw UseCaseException.NotFound(), ct);
    }

    private sealed class AccountStore : IAccountStore
    {
        private Account? _account;
        private readonly Dictionary<Guid, (Guid Family, DateTimeOffset Expires)> _logins = [];

        public Task<Account?> FindForLoginAsync(string normalizedEmail, CancellationToken ct) =>
            Task.FromResult(_account?.User.Email == normalizedEmail ? _account : null);

        public Task RegisterAsync(User user, string normalizedEmail, string hash, CancellationToken ct)
        {
            _account = new Account(user, hash);
            return Task.CompletedTask;
        }

        public Task<Account?> FindCredentialAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_account?.User.Id == id ? _account : null);

        public async Task<User?> FindUserAsync(Guid id, CancellationToken ct) => (await FindCredentialAsync(id, ct))?.User;

        public Task SaveProfileAsync(User user, CancellationToken ct) => Task.CompletedTask;

        public Task ChangePasswordAsync(Guid id, string previous, string hash, CancellationToken ct)
        {
            _account = _account! with { PasswordHash = hash };
            _logins.Clear();
            return Task.CompletedTask;
        }

        public Task CreateLoginAsync(Guid id, string expectedHash, Guid login, DateTimeOffset now, DateTimeOffset expires, CancellationToken ct)
        {
            _logins.Add(login, (login, expires));
            return Task.CompletedTask;
        }

        public Task<bool> IsLoginActiveAsync(Guid id, Guid login, DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(_logins.TryGetValue(login, out var token) && token.Expires > now);

        public async Task<Guid?> RotateLoginAsync(Guid id, Guid login, Guid replacement, DateTimeOffset now, DateTimeOffset expires, CancellationToken ct)
        {
            if (!await IsLoginActiveAsync(id, login, now, ct))
                return null;
            _logins.Remove(login, out var ticket);
            _logins.Add(replacement, (ticket.Family, expires));
            return ticket.Family;
        }

        public Task RevokeLoginAsync(Guid id, Guid family, CancellationToken ct)
        {
            var logins = _logins.Where(x => x.Value.Family == family).Select(x => x.Key).ToArray();
            foreach (var login in logins)
                _logins.Remove(login);
            return Task.CompletedTask;
        }

        public Task SetConsentAsync(Guid id, DateTimeOffset? consent, CancellationToken ct)
        {
            typeof(User).GetProperty(nameof(User.TelemetryConsentAt))!.SetValue(_account!.User, consent);
            return Task.CompletedTask;
        }

        public Task DeleteAccountAsync(Guid id, string expectedHash, CancellationToken ct)
        {
            _account = null;
            _logins.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class PasswordStub : IAccountPassword
    {
        public string Hash(User user, string password) => "fixture:" + password;

        public bool Verify(Account? account, string password) =>
            account is not null && account.PasswordHash == Hash(account.User, password);
    }

    private sealed class SettingsStore : ISettingsStore
    {
        private UserPreferences? _preferences;
        private WorkProfile? _profile;

        public Task<UserPreferences?> GetPreferencesAsync(Guid owner, CancellationToken ct) =>
            Task.FromResult(_preferences?.UserId == owner ? _preferences : null);

        public Task UpdatePreferencesAsync(Guid owner, Action<UserPreferences> update, CancellationToken ct)
        {
            _preferences ??= new UserPreferences(owner);
            update(_preferences);
            return Task.CompletedTask;
        }

        public Task<Page<WorkProfile>> ListProfilesAsync(Guid owner, PageRequest page, CancellationToken ct)
        {
            WorkProfile[] profiles = _profile?.UserId == owner ? [_profile] : [];
            return Task.FromResult(new Page<WorkProfile>(profiles, page.Number, page.Size, profiles.Length));
        }

        public Task<WorkProfile?> GetProfileAsync(Guid owner, Guid id, CancellationToken ct) =>
            Task.FromResult(_profile?.UserId == owner && _profile.Id == id ? _profile : null);

        public Task AddProfileAsync(WorkProfile profile, CancellationToken ct)
        {
            _profile = profile;
            return Task.CompletedTask;
        }

        public async Task<WorkProfile> UpdateProfileAsync(Guid owner, Guid id, Action<WorkProfile> update, CancellationToken ct)
        {
            var profile = await GetProfileAsync(owner, id, ct) ?? throw UseCaseException.NotFound();
            update(profile);
            return profile;
        }

        public Task DeleteProfileAsync(Guid owner, Guid id, CancellationToken ct)
        {
            _profile = null;
            return Task.CompletedTask;
        }
    }

    private sealed class ResearchStore : IResearchStore
    {
        public DateTimeOffset? Consent { get; set; }
        public List<ActivityAggregate> Activities { get; } = [];
        public List<FocusStateRecord> Estimates { get; } = [];

        public Task<DateTimeOffset?> GetConsentForUploadAsync(Guid owner, CancellationToken ct) => Task.FromResult(Consent);

        public Task<Dictionary<Guid, ActivityAggregate>> FindByClientIdsAsync(Guid session, Guid[] ids, CancellationToken ct) =>
            Task.FromResult(Activities.Where(x => x.FocusSessionId == session && ids.Contains(x.ClientEventId!.Value))
                .ToDictionary(x => x.ClientEventId!.Value));

        public Task<bool> OverlapsAsync(Guid session, AggregateInput[] inputs, CancellationToken ct) =>
            Task.FromResult(Activities.Any(stored => stored.FocusSessionId == session &&
                inputs.Any(x => x.StartedAt < stored.EndedAt && x.EndedAt > stored.StartedAt)));

        public Task<Dictionary<string, ProcessCategory>> GetProcessCategoriesAsync(Guid? profile, CancellationToken ct) =>
            Task.FromResult(new Dictionary<string, ProcessCategory> { ["editor"] = ProcessCategory.Allowed });

        public void Add(ActivityAggregate activity, FocusStateRecord estimate)
        {
            Activities.Add(activity);
            Estimates.Add(estimate);
        }

        public Task<Page<ActivityAggregate>> ListActivityAsync(Guid owner, Guid session, PageRequest page, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<Page<FocusStateRecord>> ListEstimatesAsync(Guid owner, Guid session, PageRequest page, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<FocusStateRecord?> GetLatestEstimateAsync(Guid owner, Guid session, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<Page<TimelineEntry>> ListTimelineAsync(Guid owner, Guid session, PageRequest page, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class AnalyticsStore(FocusSession session, ObservedActivity[] activities, EstimatedInterval[] estimates) : IAnalyticsStore
    {
        public IAsyncEnumerable<FocusSession> SessionsAsync(Guid owner, ReportWindow window, CancellationToken ct) =>
            AsAsync(owner == session.UserId ? [session] : Array.Empty<FocusSession>());

        public IAsyncEnumerable<ObservedActivity> ActivityAsync(Guid owner, ReportWindow window, Guid[]? ids, CancellationToken ct) =>
            AsAsync(owner == session.UserId ? activities : []);

        public IAsyncEnumerable<EstimatedInterval> EstimatesAsync(Guid owner, ReportWindow window, Guid[]? ids, CancellationToken ct) =>
            AsAsync(owner == session.UserId ? estimates : []);

        private static async IAsyncEnumerable<T> AsAsync<T>(IEnumerable<T> rows)
        {
            await Task.CompletedTask;
            foreach (var row in rows)
                yield return row;
        }
    }
}
