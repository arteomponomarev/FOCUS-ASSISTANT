using Focus.Avatar.Animation;
using Focus.Avatar.Protocol;

namespace Focus.Backend.Tests;

public sealed class AvatarAnimatorTests
{
    [Fact]
    public void AllNineStatesHaveDistinctStaticPoses()
    {
        var animator = new AvatarAnimator();
        var poses = Enum.GetValues<AvatarMood>().Select(mood => animator.Sample(mood, 1, true, "Balanced"));
        Assert.Equal(9, poses.Select(pose => (pose.Height, pose.Tilt, pose.Eyes, pose.Brow, pose.Smile, pose.Energy))
            .Distinct().Count());
    }

    [Fact]
    public void InterruptingAndCompletingATransitionKeepsThePoseContinuous()
    {
        var animator = new AvatarAnimator();
        var neutral = animator.Sample(AvatarMood.Neutral, 10, false, "Balanced");
        var start = animator.Sample(AvatarMood.Distracted, 10, false, "Balanced");
        Assert.Equal(neutral.Eyes, start.Eyes);
        Assert.Equal(neutral.Tilt, start.Tilt);
        var halfway = animator.Sample(AvatarMood.Distracted, 10.2, false, "Balanced");
        Assert.InRange(halfway.Smile, -.59f, .24f);
        var interrupted = animator.Sample(AvatarMood.Break, 10.2, false, "Balanced");
        Assert.Equal(halfway.Smile, interrupted.Smile);
        var rest = animator.Sample(AvatarMood.Break, 10.2 + AvatarAnimator.TransitionSeconds, false, "Balanced");
        Assert.Equal(.55f, rest.Smile, 4);
    }

    [Fact]
    public void ReducedMotionFreezesThePoseAndCelebrationIsBounded()
    {
        var animator = new AvatarAnimator();
        var start = animator.Sample(AvatarMood.Celebrating, 0, false, "Active");
        var jump = animator.Sample(AvatarMood.Celebrating, .55, false, "Active");
        var settled = animator.Sample(AvatarMood.Celebrating, 1.3, false, "Active");
        Assert.InRange(jump.Height, -.17f, start.Height - .05f);
        Assert.True(jump.Confetti > .9f);
        Assert.Equal(0, settled.Confetti);
        var reduced = animator.Sample(AvatarMood.Celebrating, .55, true, "Active");
        Assert.Equal(reduced, animator.Sample(AvatarMood.Celebrating, 3, true, "Active"));
        Assert.Equal(0, reduced.Confetti);
        Assert.Equal(1, reduced.Stretch);
    }

    [Fact]
    public void IdleBreathesAndBlinksWithoutChangingTheClassification()
    {
        var animator = new AvatarAnimator();
        var idle = animator.Sample(AvatarMood.Idle, 0, false, "Balanced");
        var breath = animator.Sample(AvatarMood.Idle, .9, false, "Balanced");
        var blink = animator.Sample(AvatarMood.Idle, 3.885, false, "Balanced");
        Assert.NotEqual(idle.Height, breath.Height);
        Assert.True(blink.Eyes < idle.Eyes * .15f);
    }
}
