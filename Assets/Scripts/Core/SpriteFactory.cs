using UnityEngine;

namespace NoBall
{
    public static class SpriteFactory
    {
        static Sprite _white;
        static Sprite _atom;
        static Sprite _rounded;
        static Sprite _circle;
        static Material _spriteMaterial;

        public static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial == null)
                {
                    var shader = Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _spriteMaterial = shader != null ? new Material(shader) : null;
                }

                return _spriteMaterial;
            }
        }

        public static Sprite White
        {
            get
            {
                if (_white == null)
                    _white = FromTexture(Solid(4, 4, Color.white), 4f);
                return _white;
            }
        }

        public static Sprite Atom
        {
            get
            {
                if (_atom == null)
                    _atom = FromTexture(MakeAtom(64), 64f);
                return _atom;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null)
                    _rounded = FromTexture(MakeRoundedRect(48, 48, 10), 48f, new Vector4(12, 12, 12, 12));
                return _rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                    _circle = FromTexture(MakeCircle(64), 64f);
                return _circle;
            }
        }

        static Sprite FromTexture(Texture2D tex, float ppu, Vector4 border = default)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, false);
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                ppu,
                0,
                SpriteMeshType.FullRect,
                border);
        }

        static Texture2D Solid(int w, int h, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            return tex;
        }

        static Texture2D MakeAtom(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float r = size * 0.48f;
            float outline = size * 0.06f;
            float stripe = size * 0.16f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = Color.clear;
                    if (d <= r)
                    {
                        if (d > r - outline)
                        {
                            c = GameColors.AtomOutline;
                        }
                        else if (Mathf.Abs(dx) < stripe)
                        {
                            c = GameColors.AtomRed;
                        }
                        else
                        {
                            c = GameColors.AtomWhite;
                        }

                        float hx = dx + r * 0.28f;
                        float hy = dy - r * 0.32f;
                        if (hx * hx + hy * hy < (r * 0.16f) * (r * 0.16f))
                            c = Color.Lerp(c, Color.white, 0.55f);
                    }

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Texture2D MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float r = size * 0.5f - 0.75f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            return tex;
        }

        static Texture2D MakeRoundedRect(int w, int h, int radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inside = InsideRounded(x, y, w, h, radius);
                    pixels[y * w + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            return tex;
        }

        static bool InsideRounded(int x, int y, int w, int h, int radius)
        {
            int r = radius;
            if (x >= r && x < w - r) return y >= 0 && y < h;
            if (y >= r && y < h - r) return x >= 0 && x < w;

            float cx = x < r ? r : w - 1 - r;
            float cy = y < r ? r : h - 1 - r;
            float dx = x - cx;
            float dy = y - cy;
            return dx * dx + dy * dy <= (r + 0.5f) * (r + 0.5f);
        }
    }
}
