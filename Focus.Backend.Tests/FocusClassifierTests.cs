using Focus.Domain.Enums;
using Focus.Engine;

namespace Focus.Backend.Tests;

public sealed class FocusClassifierTests
{
    [Theory]
    [InlineData(60, 10, 0, 2, 15, false, ProcessCategory.Allowed, FocusStateType.Focused)]
    [InlineData(60, 10, 0, 3, 15, false, ProcessCategory.Allowed, FocusStateType.Neutral)]
    [InlineData(60, 0, 0, 6, 29, false, ProcessCategory.Distracting, FocusStateType.Distracted)]
    [InlineData(60, 1, 0, 6, 30, false, ProcessCategory.Distracting, FocusStateType.Neutral)]
    [InlineData(60, 200, 50, 0, 0, false, ProcessCategory.Neutral, FocusStateType.Neutral)]
    [InlineData(60, 0, 0, 0, 45, false, ProcessCategory.Allowed, FocusStateType.Idle)]
    [InlineData(120, 0, 0, 0, 118, false, ProcessCategory.Neutral, FocusStateType.Absent)]
    [InlineData(119, 0, 0, 0, 119, false, ProcessCategory.Neutral, FocusStateType.Idle)]
    [InlineData(120, 1, 0, 0, 120, false, ProcessCategory.Neutral, FocusStateType.Idle)]
    [InlineData(120, 0, 0, 0, 120, true, ProcessCategory.Allowed, FocusStateType.Break)]
    [InlineData(60, 0, 0, 0, 0, false, ProcessCategory.Allowed, FocusStateType.Neutral)]
    public void ClassificationIsVersionedDeterministicAndRespectsPrecedence(int seconds, int keys, int clicks,
        int switches, int idle, bool pause, ProcessCategory category, FocusStateType expected)
    {
        var classifier = new FocusClassifier();
        var input = new SignalWindow(seconds, keys, clicks, switches, idle, pause, category);
        var first = classifier.Evaluate(input);
        Assert.Equal(expected, first.State);
        Assert.Equal(FocusClassifier.Version, first.RuleVersion);
        Assert.Equal(first, classifier.Evaluate(input));
        Assert.NotEmpty(first.Reason);
    }

    [Theory]
    [InlineData(0, 0, ProcessCategory.Neutral)]
    [InlineData(10, 11, ProcessCategory.Neutral)]
    [InlineData(10, -1, ProcessCategory.Neutral)]
    [InlineData(10, 0, (ProcessCategory)99)]
    public void RejectsImpossibleWindows(int seconds, int idle, ProcessCategory category) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new FocusClassifier()
            .Evaluate(new SignalWindow(seconds, 0, 0, 0, idle, false, category)));
}
