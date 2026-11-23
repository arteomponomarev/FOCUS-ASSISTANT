using System;
using Focus.Avatar.Animation;
using Focus.Avatar.Protocol;
using UnityEngine;
using static Focus.Avatar.AvatarGraphics;

namespace Focus.Avatar
{
    internal sealed class OrbitCharacter : IDisposable
    {
        private static readonly Color Glow = new Color32(183, 255, 232, 255);
        private static readonly Color Dim = new Color32(152, 174, 192, 255);
        private static readonly Color Spark = new Color32(233, 253, 154, 255);
        private readonly Texture2D shell;
        private readonly Texture2D cutout;

        public OrbitCharacter()
        {
            shell = Resources.Load<Texture2D>("OrbitShell");
            if (shell == null)
                throw new InvalidOperationException("OrbitShell art is missing.");

            // The Windows color key cannot blend partial alpha with the desktop. Use a hard mask in passive mode.
            var pixels = shell.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
                pixels[i].a = pixels[i].a >= 160 ? (byte)255 : (byte)0;
            cutout = new Texture2D(shell.width, shell.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            cutout.SetPixels32(pixels);
            cutout.Apply(false, true);
        }

        public void Render(Rect area, AvatarPose pose, AvatarMood mood, bool passive)
        {
            var size = area.width;
            var shellBox = new Rect(area.x, area.y + pose.Height * size, size, size * pose.Stretch);
            var previous = GUI.matrix;
            Rotate(pose.Tilt, shellBox.center);
            var tint = Color.Lerp(new Color32(164, 169, 198, 255), Color.white, pose.Energy);
            Sprite(passive ? cutout : shell, shellBox, tint);
            Face(shellBox, pose, mood);
            GUI.matrix = previous;

            if (pose.Confetti > .04f)
            {
                var opacity = passive ? 1f : Mathf.Min(1, pose.Confetti * 2.3f);
                Diamond(new Vector2(area.x + size * (.14f - pose.Confetti * .13f),
                    area.y + size * (.31f - pose.Confetti * .27f)), 7, new Color(Spark.r, Spark.g, Spark.b, opacity));
                Diamond(new Vector2(area.x + size * (.85f + pose.Confetti * .12f),
                    area.y + size * (.29f - pose.Confetti * .23f)), 9, new Color(Glow.r, Glow.g, Glow.b, opacity));
                Diamond(new Vector2(area.x + size * .77f, area.y + size * (.21f - pose.Confetti * .24f)), 4, Color.white);
            }
        }

        private static void Face(Rect box, AvatarPose pose, AvatarMood mood)
        {
            var ink = Color.Lerp(Dim, Glow, pose.Energy);
            float U(float x) => box.x + x * box.width;
            float V(float y) => box.y + y * box.height;
            var gaze = pose.Gaze;
            var eyeY = V(.531f);
            var eyeWidth = box.width * .070f;
            var eyeHeight = Mathf.Max(1.5f, box.height * .112f * pose.Eyes);
            var lid = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .37f, pose.Eyes));
            Eye(U(.387f + gaze), eyeY, eyeWidth, eyeHeight, ink, lid, pose.Smile);
            Eye(U(.613f + gaze), eyeY, eyeWidth, eyeHeight, ink, lid, pose.Smile);

            if (Mathf.Abs(pose.Brow) > .12f)
            {
                var offset = pose.Brow * box.height * .024f;
                var y = eyeY - box.height * .081f;
                var brow = Color.Lerp(Dim, ink, .65f);
                Line(new Vector2(U(.354f + gaze), y + offset), new Vector2(U(.420f + gaze), y - offset), 1.6f, brow);
                Line(new Vector2(U(.580f + gaze), y - offset), new Vector2(U(.646f + gaze), y + offset), 1.6f, brow);
            }

            var mouth = V(.622f);
            var curve = pose.Smile * box.height * .035f;
            Curve(U(.5f), mouth, box.width * .103f, curve, Mathf.Max(1.4f, box.width * .015f), ink);
            if (mood == AvatarMood.Distracted)
            {
                var caution = new Color32(255, 220, 146, 255);
                Card(new Rect(U(.737f), V(.558f), 4, 4), caution, 2);
            }
            else if (mood == AvatarMood.Disconnected)
            {
                Card(new Rect(U(.718f), V(.553f), 4, 3), Dim, 1);
                Card(new Rect(U(.759f), V(.553f), 4, 3), Dim, 1);
            }
        }

        private static void Eye(float x, float y, float width, float height, Color color, float lid, float smile)
        {
            if (lid > 0)
            {
                var tint = new Color(color.r, color.g, color.b, lid);
                Curve(x, y, width * 1.25f, width * Mathf.Lerp(.17f, -.31f, Mathf.InverseLerp(.65f, 1, smile)),
                    Mathf.Max(1.3f, width * .20f), tint);
            }
            if (lid >= 1)
                return;
            Card(new Rect(x - width / 2, y - height / 2, width, height),
                new Color(color.r, color.g, color.b, 1 - lid), width / 2);
            if (height > width)
                Card(new Rect(x - width * .19f, y - height * .27f, width * .28f, width * .28f), Color.white, 1.8f);
        }

        private static void Curve(float x, float y, float width, float bow, float thickness, Color color)
        {
            var previous = new Vector2(x - width / 2, y);
            for (var i = 1; i <= 6; i++)
            {
                var step = i / 6f;
                var point = new Vector2(x + (step - .5f) * width, y + 4 * bow * step * (1 - step));
                Line(previous, point, thickness, color);
                previous = point;
            }
        }

        private static void Diamond(Vector2 center, float size, Color color)
        {
            var previous = GUI.matrix;
            Rotate(45, center);
            Card(new Rect(center.x - size / 2, center.y - size / 2, size, size), color, 1.5f);
            GUI.matrix = previous;
        }

        public void Dispose() => UnityEngine.Object.Destroy(cutout);
    }
}
