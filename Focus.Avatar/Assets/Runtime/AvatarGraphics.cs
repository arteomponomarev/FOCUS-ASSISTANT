using UnityEngine;

namespace Focus.Avatar
{
    internal static class AvatarGraphics
    {
        public static void Card(Rect rect, Color color, float radius = 0) =>
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, radius);

        public static void Line(Vector2 from, Vector2 to, float width, Color color)
        {
            var direction = to - from;
            var matrix = GUI.matrix;
            Rotate(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, from);
            Card(new Rect(from.x, from.y - width / 2, direction.magnitude, width), color, width / 2);
            GUI.matrix = matrix;
        }

        public static void Rotate(float degrees, Vector2 origin)
        {
            var pivot = new Vector3(origin.x, origin.y, 0);
            // Compose in panel coordinates so the same pivot is used at every desktop scale.
            GUI.matrix = GUI.matrix * Matrix4x4.Translate(pivot) *
                Matrix4x4.Rotate(Quaternion.Euler(0, 0, degrees)) * Matrix4x4.Translate(-pivot);
        }

        public static void Sprite(Texture texture, Rect rect, Color tint)
        {
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        public static Texture2D KeyedFrame(int width, int height, float radius, Color32 fill, Color32 edge)
        {
            var frame = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var cornerX = Mathf.Max(0, radius - Mathf.Min(x + .5f, width - x - .5f));
                var cornerY = Mathf.Max(0, radius - Mathf.Min(y + .5f, height - y - .5f));
                var distance = cornerX * cornerX + cornerY * cornerY;
                if (distance > radius * radius)
                    continue;
                var border = x == 0 || x == width - 1 || y == 0 || y == height - 1 ||
                    distance >= (radius - 1) * (radius - 1);
                pixels[y * width + x] = border ? edge : fill;
            }
            frame.SetPixels32(pixels);
            frame.Apply(false, true);
            return frame;
        }
    }
}
