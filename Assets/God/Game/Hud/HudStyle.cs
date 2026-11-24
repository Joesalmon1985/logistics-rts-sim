using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// The look of the god HUD: a soft, golden-lit cloud drawn into procedural textures (no art files), and the IMGUI styles
    /// that use them. Built lazily from OnGUI (GUIStyle needs the GUI to be running), once.
    /// </summary>
    public sealed class HudStyle
    {
        public static readonly Color Ink = new Color(0.27f, 0.19f, 0.09f);
        public static readonly Color InkSoft = new Color(0.27f, 0.19f, 0.09f, 0.72f);
        public static readonly Color Cream = new Color(1f, 0.97f, 0.88f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.36f);
        public static readonly Color Good = new Color(0.30f, 0.62f, 0.28f);
        public static readonly Color Bad = new Color(0.80f, 0.28f, 0.22f);

        public Texture2D Strip, Panel, BtnNormal, BtnHover, BtnOn, Tip, Dim;
        public GUIStyle Row, RowSmall, Window, Label, Small, Title, Btn, BtnSel, Tile, TileSel, TipBox, Centered;

        private static HudStyle _inst;
        public static HudStyle Get() => _inst ?? (_inst = new HudStyle());

        private HudStyle()
        {
            Strip = CloudStrip(1024, 160);
            Panel = RoundBox(64, 22, new Color(1f, 0.97f, 0.88f), 0.95f, new Color(1f, 0.82f, 0.45f));
            BtnNormal = RoundBox(40, 14, new Color(1f, 0.97f, 0.90f), 0.62f, new Color(0.92f, 0.78f, 0.50f));
            BtnHover = RoundBox(40, 14, new Color(1f, 0.93f, 0.70f), 0.96f, new Color(1f, 0.78f, 0.35f));
            BtnOn = RoundBox(40, 14, new Color(1f, 0.82f, 0.38f), 1f, new Color(0.95f, 0.62f, 0.20f));
            Tip = RoundBox(40, 12, new Color(1f, 0.98f, 0.92f), 0.98f, new Color(1f, 0.82f, 0.45f));
            Dim = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            Dim.SetPixel(0, 0, new Color(0.12f, 0.09f, 0.04f, 0.38f)); Dim.Apply();

            Window = new GUIStyle
            {
                normal = { background = Panel, textColor = Ink },
                border = new RectOffset(24, 24, 24, 24),
                padding = new RectOffset(16, 16, 12, 14),
                fontSize = 13,
            };
            Label = new GUIStyle { fontSize = 13, wordWrap = true, normal = { textColor = Ink } };
            Small = new GUIStyle(Label) { fontSize = 11, normal = { textColor = InkSoft } };
            Title = new GUIStyle(Label) { fontSize = 16, fontStyle = FontStyle.Bold };
            Row = new GUIStyle(Label) { wordWrap = false, alignment = TextAnchor.MiddleLeft };
            RowSmall = new GUIStyle(Small) { wordWrap = false, alignment = TextAnchor.MiddleLeft };
            Centered = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };
            Btn = ButtonStyle(12);
            BtnSel = ButtonStyle(12);
            BtnSel.normal.background = BtnOn; BtnSel.hover.background = BtnOn;
            Tile = ButtonStyle(11);
            Tile.alignment = TextAnchor.LowerCenter;
            Tile.padding = new RectOffset(2, 2, 2, 5);
            TileSel = new GUIStyle(Tile);
            TileSel.normal.background = BtnOn; TileSel.hover.background = BtnOn;
            TipBox = new GUIStyle(Label)
            {
                normal = { background = Tip, textColor = Ink },
                border = new RectOffset(14, 14, 14, 14),
                padding = new RectOffset(12, 12, 8, 9),
                fontSize = 12,
            };
        }

        private GUIStyle ButtonStyle(int size)
        {
            return new GUIStyle
            {
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(14, 14, 14, 14),
                padding = new RectOffset(6, 6, 4, 5),
                margin = new RectOffset(2, 2, 2, 2),
                normal = { background = BtnNormal, textColor = Ink },
                hover = { background = BtnHover, textColor = Ink },
                active = { background = BtnOn, textColor = Ink },
                onNormal = { background = BtnOn, textColor = Ink },
            };
        }

        // ---------------------------------------------------------------- procedural textures

        private static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>A rounded square with a feathered edge and a golden rim, for 9-slice use.</summary>
        private static Texture2D RoundBox(int size, float radius, Color body, float alpha, Color rim)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to the rounded square (negative inside).
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius), qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float inside = Smooth(0.5f, -1.5f, d);
                    float depth = -d;
                    float rimK = Mathf.Exp(-Mathf.Max(0f, depth) / 3.2f);
                    float glow = Mathf.Clamp01(1f - (float)y / size) * 0.10f; // a little lit from below, like the cloud
                    var c = Color.Lerp(body, rim, rimK * 0.85f) + new Color(glow * 0.5f, glow * 0.3f, 0f, 0f);
                    c.a = inside * Mathf.Lerp(alpha, Mathf.Min(1f, alpha + 0.05f), rimK);
                    px[y * size + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        /// <summary>The cloud strip: solid cream along the top, a puffy golden-lit lower edge.</summary>
        private static Texture2D CloudStrip(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float yb = h * 0.34f;
            var rnd = new PropRandom(7);
            int n = 20;
            var cx = new float[n]; var cr = new float[n]; var cy = new float[n];
            for (int i = 0; i < n; i++)
            {
                cx[i] = (i + 0.5f + rnd.Range(-0.3f, 0.3f)) * w / n;
                cr[i] = rnd.Range(h * 0.13f, h * 0.30f);
                cy[i] = yb + rnd.Range(-h * 0.03f, h * 0.05f);
            }
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float depth = y + 0.5f - yb; // inside the solid part when positive
                    for (int i = 0; i < n; i++)
                    {
                        float dx = x + 0.5f - cx[i], dy = y + 0.5f - cy[i];
                        float di = cr[i] - Mathf.Sqrt(dx * dx + dy * dy);
                        if (di > depth) depth = di;
                    }
                    float a = Smooth(-2.5f, 3.5f, depth);
                    float rim = Mathf.Exp(-Mathf.Max(0f, depth) / 9f);          // golden light caught on the underside
                    float top = Mathf.Clamp01((y - yb) / (h - yb));              // paler towards the top
                    var body = Color.Lerp(new Color(1f, 0.96f, 0.84f), new Color(1f, 0.99f, 0.95f), top);
                    var c = Color.Lerp(body, new Color(1f, 0.80f, 0.40f), rim * 0.65f);
                    c.a = a * Mathf.Lerp(0.90f, 0.97f, 1f - rim);
                    px[y * w + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }
    }
}
