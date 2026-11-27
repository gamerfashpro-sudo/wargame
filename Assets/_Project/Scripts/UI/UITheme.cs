using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WG.UI
{
    /// <summary>Дизайн-система WARGAME: цвета, шрифты, размеры, генерируемые спрайты.</summary>
    public static class UITheme
    {
        // Цвета (раздел 5 DEV_CONTEXT)
        public static readonly Color Bg = Hex("#14171B", 0.86f);
        public static readonly Color BgSolid = Hex("#101215");
        public static readonly Color Panel = Hex("#1E2329", 0.92f);
        public static readonly Color PanelLight = Hex("#262C33", 0.95f);
        public static readonly Color Divider = Hex("#2C333B");
        public static readonly Color Accent = Hex("#F2B33D");
        public static readonly Color AccentDark = Hex("#B9811F");
        public static readonly Color Text = Hex("#E8E6E1");
        public static readonly Color TextMuted = Hex("#9AA0A6");
        public static readonly Color TextDark = Hex("#15181C");
        public static readonly Color Success = Hex("#5FBF6A");
        public static readonly Color Danger = Hex("#E0563B");
        public static readonly Color Disabled = Hex("#5A6068");

        // Размеры
        public const float Radius = 6f;
        public const float FontTitle = 64f, FontH1 = 34f, FontH2 = 24f, FontBody = 20f, FontSmall = 16f;
        public const float RowHeight = 46f, ButtonHeight = 50f;

        static TMP_FontAsset titleFont, bodyFont, boldFont;
        static readonly Dictionary<string, Sprite> sprites = new();

        public static TMP_FontAsset TitleFont => titleFont ??= LoadFont("Fonts/RussoOne-Regular");
        public static TMP_FontAsset BodyFont => bodyFont ??= LoadFont("Fonts/PT_Sans-Web-Regular");
        public static TMP_FontAsset BoldFont => boldFont ??= LoadFont("Fonts/PT_Sans-Web-Bold");

        static TMP_FontAsset LoadFont(string path)
        {
            var font = Resources.Load<Font>(path);
            TMP_FontAsset asset = null;
            if (font != null) asset = TMP_FontAsset.CreateFontAsset(font);
            if (asset == null)
            {
                Debug.LogWarning($"[WARGAME] Шрифт {path} не загружен, используется шрифт TMP по умолчанию.");
                asset = TMP_Settings.defaultFontAsset;
            }
            else
            {
                asset.name = path;
                // запасной шрифт — для символов, которых нет в основном
                if (TMP_Settings.defaultFontAsset != null)
                    asset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            }
            return asset;
        }

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

        /// <summary>Белый скруглённый прямоугольник для 9-slice (красится через Image.color).</summary>
        public static Sprite Rounded(int radius = 8)
        {
            string key = "round" + radius;
            if (sprites.TryGetValue(key, out var s)) return s;
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = key };
            var px = new Color32[size * size];
            float c = radius + 1.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - c));
                float dy = Mathf.Max(0, Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - c));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius + 0.5f - d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = radius + 2;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprites[key] = s;
            return s;
        }

        /// <summary>Горизонтальный градиент прозрачности (слева непрозрачно → справа прозрачно).</summary>
        public static Sprite GradientH()
        {
            if (sprites.TryGetValue("gradH", out var s)) return s;
            var tex = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 256; x++)
            {
                float t = x / 255f;
                tex.SetPixel(x, 0, new Color(1, 1, 1, Mathf.Pow(1f - t, 1.6f)));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 256, 1), new Vector2(0.5f, 0.5f));
            sprites["gradH"] = s;
            return s;
        }

        /// <summary>Вертикальный градиент (снизу непрозрачно).</summary>
        public static Sprite GradientV()
        {
            if (sprites.TryGetValue("gradV", out var s)) return s;
            var tex = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 256; y++) tex.SetPixel(0, y, new Color(1, 1, 1, Mathf.Pow(1f - y / 255f, 1.4f)));
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 1, 256), new Vector2(0.5f, 0.5f));
            sprites["gradV"] = s;
            return s;
        }

        /// <summary>Пятиконечная звезда (сглаженная).</summary>
        public static Sprite Star()
        {
            if (sprites.TryGetValue("star", out var s)) return s;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 0.48f : 0.19f;
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                pts[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r);
            }
            const int ss = 4; // суперсэмплинг
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                    if (Inside(pts, new Vector2((x + (sx + 0.5f) / ss) / size, (y + (sy + 0.5f) / ss) / size))) hits++;
                tex.SetPixel(x, y, new Color(1, 1, 1, hits / (float)(ss * ss)));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprites["star"] = s;
            return s;
        }

        /// <summary>Треугольник-стрелка вправо (для кнопок «‹ ›» и «Играть»).</summary>
        public static Sprite Arrow()
        {
            if (sprites.TryGetValue("arrow", out var s)) return s;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pts = new[] { new Vector2(0.25f, 0.12f), new Vector2(0.85f, 0.5f), new Vector2(0.25f, 0.88f) };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 3; sy++)
                for (int sx = 0; sx < 3; sx++)
                    if (Inside(pts, new Vector2((x + (sx + 0.5f) / 3f) / size, (y + (sy + 0.5f) / 3f) / size))) hits++;
                tex.SetPixel(x, y, new Color(1, 1, 1, hits / 9f));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprites["arrow"] = s;
            return s;
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
