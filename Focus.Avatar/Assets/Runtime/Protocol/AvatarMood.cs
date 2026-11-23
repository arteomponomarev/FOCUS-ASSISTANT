using System;

namespace Focus.Avatar.Protocol
{
    public enum AvatarMood { Neutral, Focused, Distracted, Idle, Break, Absent, Celebrating, Tired, Disconnected }

    public static class AvatarPresentation
    {
        public static AvatarMood Mood(AvatarMessage snapshot, bool celebrating, double consistentIdleSeconds = 0)
        {
            if (snapshot.result != "ok")
                return AvatarMood.Disconnected;
            if (celebrating)
                return AvatarMood.Celebrating;
            if (snapshot.status == "Paused")
                return AvatarMood.Break;

            return snapshot.estimate switch
            {
                "Focused" => AvatarMood.Focused,
                "Distracted" => AvatarMood.Distracted,
                "Idle" => consistentIdleSeconds >= 30 ? AvatarMood.Tired : AvatarMood.Idle,
                "Break" => AvatarMood.Break,
                "Absent" => AvatarMood.Absent,
                _ => AvatarMood.Neutral
            };
        }

        public static bool ShowCompanion(AvatarMessage snapshot, bool celebrating) => snapshot.visibility switch
        {
            "Hidden" => false,
            "AlwaysVisible" => true,
            "NotificationsOnly" => celebrating && snapshot.intensity != "Quiet",
            _ => snapshot.status is "Active" or "Paused" || celebrating
        };

        public static bool ShouldCelebrate(string previous, string current) =>
            current == "Completed" && previous is "Active" or "Paused";

        public static double ActiveSeconds(AvatarMessage snapshot, double secondsSinceSnapshot)
        {
            // Extrapolate only across the normal polling interval. State changes remain server-authoritative.
            return snapshot.activeSeconds + (snapshot.status == "Active"
                ? Math.Max(0, Math.Min(secondsSinceSnapshot, 3))
                : 0);
        }
    }
}
