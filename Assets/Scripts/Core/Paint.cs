// <canvas> 2D çiziminin karşılığı: Texture2D üzerine şekil boyama (dolgu, yuvarlak köşe, daire, çizgi, gradyan).
// Yazı dokuya basılmaz: 3D yüzeylerdeki yazılar için Game.Text3D (TextMesh) kullanılır.
// Koordinatlar canvas gibi SOL ÜSTTEN, piksel cinsinden.
using UnityEngine;

namespace Ciftlik
{
    public class Paint
    {
        public readonly int W, H;
        public readonly Texture2D Tex;
        readonly Color32[] px;

        public Paint(int w, int h, Color? bg = null)
        {
            W = Mathf.Max(1, w); H = Mathf.Max(1, h);
            Tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            px = new Color32[W * H];
            Clear(bg ?? new Color(0, 0, 0, 0));
        }
        public Paint Clear(Color c) { Color32 k = c; for (int i = 0; i < px.Length; i++) px[i] = k; return this; }

        void Blend(int x, int y, Color c, float cov)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || cov <= 0) return;
            int i = (H - 1 - y) * W + x;
            float a = c.a * Mathf.Clamp01(cov);
            if (a >= .999f) { px[i] = c; return; }
            Color d = px[i];
            float oa = a + d.a * (1 - a);
            if (oa <= 0) { px[i] = new Color32(0, 0, 0, 0); return; }
            px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / oa, (c.g * a + d.g * d.a * (1 - a)) / oa, (c.b * a + d.b * d.a * (1 - a)) / oa, oa);
        }
        public Paint Rect(float x, float y, float w, float h, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x)), y0 = Mathf.Max(0, Mathf.FloorToInt(y)), x1 = Mathf.Min(W, Mathf.CeilToInt(x + w)), y1 = Mathf.Min(H, Mathf.CeilToInt(y + h));
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++) Blend(xx, yy, c, 1);
            return this;
        }
        // Dikey gradyan (canvas createLinearGradient(0,y,0,y+h))
        public Paint VGradient(float x, float y, float w, float h, Color top, Color bottom)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x)), y0 = Mathf.Max(0, Mathf.FloorToInt(y)), x1 = Mathf.Min(W, Mathf.CeilToInt(x + w)), y1 = Mathf.Min(H, Mathf.CeilToInt(y + h));
            for (int yy = y0; yy < y1; yy++) { var c = Color.Lerp(top, bottom, h <= 1 ? 0 : (yy - y) / (h - 1)); for (int xx = x0; xx < x1; xx++) Blend(xx, yy, c, 1); }
            return this;
        }
        public Paint RoundRect(float x, float y, float w, float h, float r, Color c)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) / 2);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x)), y0 = Mathf.Max(0, Mathf.FloorToInt(y)), x1 = Mathf.Min(W, Mathf.CeilToInt(x + w)), y1 = Mathf.Min(H, Mathf.CeilToInt(y + h));
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
                {
                    float px_ = xx + .5f, py = yy + .5f;
                    float cx = Mathf.Clamp(px_, x + r, x + w - r), cy = Mathf.Clamp(py, y + r, y + h - r);
                    float d = Mathf.Sqrt((px_ - cx) * (px_ - cx) + (py - cy) * (py - cy));
                    float cov = r > 0 ? Mathf.Clamp01(r - d + .5f) : 1;
                    float edge = Mathf.Min(Mathf.Clamp01(px_ - x + .5f), Mathf.Clamp01(x + w - px_ + .5f)) * Mathf.Min(Mathf.Clamp01(py - y + .5f), Mathf.Clamp01(y + h - py + .5f));
                    Blend(xx, yy, c, Mathf.Min(cov, edge));
                }
            return this;
        }
        public Paint StrokeRoundRect(float x, float y, float w, float h, float r, float lw, Color c)
        {
            // kenar = dış dolgu − iç; dış halkayı piksel başına mesafe ile çiz
            r = Mathf.Min(r, Mathf.Min(w, h) / 2);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x - lw)), y0 = Mathf.Max(0, Mathf.FloorToInt(y - lw)), x1 = Mathf.Min(W, Mathf.CeilToInt(x + w + lw)), y1 = Mathf.Min(H, Mathf.CeilToInt(y + h + lw));
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
                {
                    float px_ = xx + .5f, py = yy + .5f;
                    float cx = Mathf.Clamp(px_, x + r, x + w - r), cy = Mathf.Clamp(py, y + r, y + h - r);
                    float qx = Mathf.Abs(px_ - (x + w / 2)) - (w / 2 - r), qy = Mathf.Abs(py - (y + h / 2)) - (h / 2 - r);
                    float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r; // işaretli mesafe
                    float cov = Mathf.Clamp01(lw / 2 - Mathf.Abs(outside) + .5f);
                    Blend(xx, yy, c, cov);
                }
            return this;
        }
        public Paint Circle(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1)), y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1)), x1 = Mathf.Min(W, Mathf.CeilToInt(cx + r + 1)), y1 = Mathf.Min(H, Mathf.CeilToInt(cy + r + 1));
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
                {
                    float d = Mathf.Sqrt((xx + .5f - cx) * (xx + .5f - cx) + (yy + .5f - cy) * (yy + .5f - cy));
                    Blend(xx, yy, c, r - d + .5f);
                }
            return this;
        }
        public Paint Ring(float cx, float cy, float r, float lw, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - lw)), y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - lw)), x1 = Mathf.Min(W, Mathf.CeilToInt(cx + r + lw)), y1 = Mathf.Min(H, Mathf.CeilToInt(cy + r + lw));
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
                {
                    float d = Mathf.Sqrt((xx + .5f - cx) * (xx + .5f - cx) + (yy + .5f - cy) * (yy + .5f - cy));
                    Blend(xx, yy, c, lw / 2 - Mathf.Abs(d - r) + .5f);
                }
            return this;
        }
        public Paint Line(float ax, float ay, float bx, float by, float lw, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ax, bx) - lw)), y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ay, by) - lw));
            int x1 = Mathf.Min(W, Mathf.CeilToInt(Mathf.Max(ax, bx) + lw)), y1 = Mathf.Min(H, Mathf.CeilToInt(Mathf.Max(ay, by) + lw));
            var a = new Vector2(ax, ay); var ab = new Vector2(bx - ax, by - ay); float L2 = Mathf.Max(1e-6f, ab.sqrMagnitude);
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
                {
                    var p = new Vector2(xx + .5f, yy + .5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / L2);
                    float d = Vector2.Distance(p, a + ab * t);
                    Blend(xx, yy, c, lw / 2 - d + .5f);
                }
            return this;
        }
        public Paint Polyline(Vector2[] pts, float lw, Color c) { for (int i = 1; i < pts.Length; i++) Line(pts[i - 1].x, pts[i - 1].y, pts[i].x, pts[i].y, lw, c); return this; }
        // Dışbükey/basit çokgen dolgusu (tarama çizgisi, çift-tek kuralı)
        public Paint Poly(Vector2[] pts, Color c)
        {
            if (pts == null || pts.Length < 3) return this;
            float minY = float.MaxValue, maxY = float.MinValue; foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            var xs = new System.Collections.Generic.List<float>();
            for (int yy = Mathf.Max(0, Mathf.FloorToInt(minY)); yy < Mathf.Min(H, Mathf.CeilToInt(maxY)); yy++)
            {
                float sy = yy + .5f; xs.Clear();
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    var a = pts[i]; var b = pts[j];
                    if ((a.y > sy) != (b.y > sy)) xs.Add(a.x + (sy - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                    for (int xx = Mathf.Max(0, Mathf.FloorToInt(xs[k])); xx < Mathf.Min(W, Mathf.CeilToInt(xs[k + 1])); xx++)
                        Blend(xx, yy, c, Mathf.Min(1, Mathf.Min(xx + 1 - xs[k], xs[k + 1] - xx)));
            }
            return this;
        }
        // Basit gürültü / kâğıt dokusu
        public Paint Noise(float amount, int seed = 1)
        {
            var r = new System.Random(seed);
            for (int i = 0; i < px.Length; i++)
            {
                float n = ((float)r.NextDouble() - .5f) * 2 * amount;
                Color c = px[i]; c.r = Mathf.Clamp01(c.r + n); c.g = Mathf.Clamp01(c.g + n); c.b = Mathf.Clamp01(c.b + n); px[i] = c;
            }
            return this;
        }
        public Texture2D Apply() { Tex.SetPixels32(px); Tex.Apply(true); return Tex; }
    }

    public static partial class Game
    {
        static readonly System.Collections.Generic.Dictionary<Font, Material> _text3dMats = new System.Collections.Generic.Dictionary<Font, Material>();
        static bool _text3dHooked;
        static void Text3D_Rebuilt(Font f)
        {
            Material m;
            if (f != null && _text3dMats.TryGetValue(f, out m) && m != null && f.material != null) m.mainTexture = f.material.mainTexture;
        }
        // 3D yüzeye yazı (TextMesh). height: büyük harf yüksekliği yaklaşık (dünya birimi). Konum/dönüş Unity yerel koordinatıdır.
        public static TextMesh Text3D(Transform parent, string s, Vector3 localPos, float height, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false, bool display = false)
        {
            var go = new GameObject("text3d");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            var f = display ? UI.DisplayFont : UI.Font;
            tm.font = f; tm.fontSize = 64; tm.characterSize = height / 6.4f * 1.0f; tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft || anchor == TextAnchor.LowerLeft ? TextAlignment.Left
                         : anchor == TextAnchor.MiddleRight || anchor == TextAnchor.UpperRight || anchor == TextAnchor.LowerRight ? TextAlignment.Right : TextAlignment.Center;
            tm.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; tm.color = color; tm.richText = true; tm.text = s ?? "";
            var mr = go.GetComponent<MeshRenderer>();
            if (f == null) { mr.enabled = false; return tm; }
            var sh = Shader.Find("Ciftlik/Text3D");
            if (sh != null)
            {
                // yazı tipi başına tek malzeme; dinamik yazı tipinin atlası yeniden kurulunca (textureRebuilt) dokusu güncellenir
                if (!_text3dHooked) { _text3dHooked = true; Font.textureRebuilt += Text3D_Rebuilt; }
                Material m;
                if (!_text3dMats.TryGetValue(f, out m) || m == null) { m = new Material(sh) { name = "text3d" }; _text3dMats[f] = m; }
                if (m.mainTexture != f.material.mainTexture) m.mainTexture = f.material.mainTexture;
                mr.sharedMaterial = m;
            }
            else mr.sharedMaterial = f.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return tm;
        }
    }
}
