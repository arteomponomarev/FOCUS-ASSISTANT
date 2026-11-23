using System;
using Focus.Avatar.Protocol;

namespace Focus.Avatar.Animation
{
    public readonly struct AvatarPose
    {
        public readonly float Height, Stretch, Tilt, Eyes, Gaze, Brow, Smile, Energy, Confetti;

        public AvatarPose(float height, float stretch, float tilt, float eyes, float gaze, float brow, float smile,
            float energy, float confetti = 0)
        {
            Height = height;
            Stretch = stretch;
            Tilt = tilt;
            Eyes = eyes;
            Gaze = gaze;
            Brow = brow;
            Smile = smile;
            Energy = energy;
            Confetti = confetti;
        }

        internal static AvatarPose Blend(AvatarPose a, AvatarPose b, float t) => new AvatarPose(
            Mix(a.Height, b.Height, t), Mix(a.Stretch, b.Stretch, t), Mix(a.Tilt, b.Tilt, t),
            Mix(a.Eyes, b.Eyes, t), Mix(a.Gaze, b.Gaze, t), Mix(a.Brow, b.Brow, t),
            Mix(a.Smile, b.Smile, t), Mix(a.Energy, b.Energy, t), Mix(a.Confetti, b.Confetti, t));

        private static float Mix(float a, float b, float t) => a + (b - a) * t;
    }

    public sealed class AvatarAnimator
    {
        public const double TransitionSeconds = 0.45;
        private AvatarMood mood;
        private AvatarPose from;
        private double enteredAt;
        private bool initialized;

        public AvatarPose Sample(AvatarMood target, double now, bool reduced, string intensity)
        {
            var motion = intensity == "Quiet" ? .55f : intensity == "Active" ? 1f : .78f;
            if (!initialized || target != mood)
            {
                from = initialized ? Evaluate(now, reduced, motion) : AtRest(target);
                mood = target;
                enteredAt = now;
                initialized = true;
            }

            return Evaluate(now, reduced, motion);
        }

        private AvatarPose Evaluate(double now, bool reduced, float motion)
        {
            var rest = AtRest(mood);
            if (reduced)
                return rest;

            var elapsed = Math.Max(0, now - enteredAt);
            var (speed, lift, sway) = mood switch
            {
                AvatarMood.Focused => (.25, .018f, .5f),
                AvatarMood.Distracted => (.45, .026f, 2.7f),
                AvatarMood.Idle => (.28, .034f, 1.8f),
                AvatarMood.Break => (.18, .016f, 1.4f),
                AvatarMood.Absent => (.13, .012f, .5f),
                AvatarMood.Tired => (.16, .014f, 1.0f),
                AvatarMood.Celebrating => (.36, .025f, 1.3f),
                AvatarMood.Disconnected => (.16, .009f, .8f),
                _ => (.31, .026f, 1.6f)
            };
            var wave = (float)Math.Sin(elapsed * speed * 2 * Math.PI);
            var blinkTime = elapsed % 5.3 - 3.8;
            var blink = blinkTime is > 0 and < .17 ? (float)Math.Sin(Math.PI * blinkTime / .17) : 0;
            var jump = mood == AvatarMood.Celebrating && elapsed < 1.15
                ? (float)Math.Sin(Math.PI * elapsed / 1.15) : 0;
            var pose = new AvatarPose(
                rest.Height + motion * (wave * lift - jump * .12f),
                1 + motion * (wave * lift * .46f + jump * .05f),
                rest.Tilt + motion * (wave * sway - jump * 5),
                rest.Eyes * (1 - blink * .93f),
                rest.Gaze + (mood == AvatarMood.Distracted ? wave * .026f : 0),
                rest.Brow, rest.Smile, rest.Energy, jump * motion);
            var blend = Math.Clamp((float)(elapsed / TransitionSeconds), 0, 1);
            return AvatarPose.Blend(from, pose, blend * blend * (3 - 2 * blend));
        }

        private static AvatarPose AtRest(AvatarMood target) => target switch
        {
            AvatarMood.Focused => new AvatarPose(-.025f, 1, 0, .88f, 0, -.18f, .65f, 1),
            AvatarMood.Distracted => new AvatarPose(0, 1, 5, .74f, .07f, .9f, -.6f, .8f),
            AvatarMood.Idle => new AvatarPose(.01f, 1, -2, .76f, -.024f, .09f, .35f, .77f),
            AvatarMood.Break => new AvatarPose(.071f, 1, -6, .35f, 0, 0, .55f, .84f),
            AvatarMood.Absent => new AvatarPose(.085f, 1, 8, 0, 0, 0, .05f, .55f),
            AvatarMood.Tired => new AvatarPose(.061f, 1, -7, .21f, 0, .26f, -.21f, .63f),
            AvatarMood.Celebrating => new AvatarPose(-.03f, 1, -4, .13f, 0, -.3f, 1, 1),
            AvatarMood.Disconnected => new AvatarPose(.035f, 1, 1, .48f, 0, .02f, -.06f, .48f),
            _ => new AvatarPose(0, 1, -1.5f, .95f, 0, 0, .25f, .9f)
        };
    }
}
