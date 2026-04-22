using UnityEngine;

namespace LandKing.Prototype
{
    internal static class Sprite2DUtil
    {
        private static Texture2D _white;

        public static Texture2D White1x1
        {
            get
            {
                if (_white != null) return _white;
                _white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
                return _white;
            }
        }

        public static Sprite CreateSprite(int w, int h, Color c)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
            var pixels = new Color[w * h];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = c;
            t.SetPixels(pixels);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, Vector4.zero, false, Vector2.zero);
        }
    }
}
