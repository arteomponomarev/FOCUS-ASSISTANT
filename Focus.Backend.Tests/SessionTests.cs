using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Backend.Tests;

public sealed class SessionTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 23, 50, 0, TimeSpan.Zero);

    [Fact]
    public void LifecycleTracksPauseAcrossMidnightAndPersistsReportsSeparately()
    {
        var session = NewSession();
        var goal = session.AddGoal("Write the methods section");
        session.Start(Epoch);
        session.Pause(Epoch.AddMinutes(5));
        session.Resume(Epoch.AddMinutes(15));
        session.SetGoalOutcome(goal.Id, GoalStatus.PartiallyCompleted);
        session.Complete(4, "  interruption  ", 2, Epoch.AddMinutes(25));

        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.Equal(4, session.SelfReportedConcentration);
        Assert.Equal(2, session.SelfReportedProductivity);
        Assert.Equal("interruption", session.Reflection);
        Assert.Equal(TimeSpan.FromMinutes(15), session.GetDurations(Epoch.AddHours(1)).Active);
        var clipped = session.GetDurations(Epoch.AddMinutes(10), Epoch.AddMinutes(20), Epoch.AddHours(1));
        Assert.Equal(TimeSpan.FromMinutes(5), clipped.Active);
        Assert.Equal(TimeSpan.FromMinutes(5), clipped.Paused);
        Assert.Equal(50, GoalStatistics.From(session.Goals).CompletionPercentage);
    }

    [Fact]
    public void InvalidTransitionsLeaveStateUnchanged()
    {
        var session = NewSession();
        Assert.Throws<SessionConflictException>(() => session.Resume(Epoch));
        Assert.Throws<SessionConflictException>(() => session.Complete(3));
        var goal = session.AddGoal("Outline");
        session.RenameGoal(goal.Id, " Outline revision ");
        Assert.Equal("Outline revision", goal.Description);
        session.Start(Epoch);
        Assert.Throws<SessionConflictException>(() => session.Start(Epoch));
        Assert.Throws<SessionConflictException>(() => session.RemoveGoal(goal.Id));
        Assert.Throws<SessionConflictException>(() => session.AddGoal("Late goal"));
        session.Pause(Epoch);
        session.Cancel(Epoch.AddMinutes(2));
        Assert.Equal(SessionStatus.Cancelled, session.Status);
        Assert.Equal(Epoch.AddMinutes(2), Assert.Single(session.Pauses).EndedAt);
        Assert.Throws<SessionConflictException>(() => session.Cancel(Epoch));
    }

    [Theory]
    [InlineData(0, 4, "selfReportedConcentration")]
    [InlineData(6, 4, "selfReportedConcentration")]
    [InlineData(4, 0, "selfReportedProductivity")]
    [InlineData(4, 6, "selfReportedProductivity")]
    public void RatingsOutsideScaleDoNotEndSession(int concentration, int productivity, string field)
    {
        var session = NewSession();
        session.Start(Epoch);
        Assert.Throws<ArgumentOutOfRangeException>(field, () => session.Complete(concentration, null, productivity, Epoch));
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void SupportsCompletingFromPauseAndLegacyOptionalProductivity()
    {
        var session = NewSession();
        session.Start(Epoch);
        session.Pause(Epoch.AddMinutes(2));
        session.Complete(1, "  ", now: Epoch.AddMinutes(4));
        Assert.Null(session.SelfReportedProductivity);
        Assert.Null(session.Reflection);
        Assert.Equal(TimeSpan.FromMinutes(2), session.GetDurations(Epoch.AddHours(1)).Active);
        Assert.Throws<SessionConflictException>(() => session.Complete(5));
    }

    [Fact]
    public void PlannedGoalDeletionAndEmptyStatistics()
    {
        var session = NewSession();
        var goal = session.AddGoal("Draft");
        session.RemoveGoal(goal.Id);
        Assert.Empty(session.Goals);
        Assert.Equal(new GoalStatistics(0, 0, 0, 0), GoalStatistics.From(session.Goals));
        Assert.Throws<SessionConflictException>(() => session.RemoveGoal(goal.Id));
    }

    private static FocusSession NewSession() => new(Guid.NewGuid(), TimeSpan.FromMinutes(25), Epoch);
}
