// CanvasRenderingContext2D'nin (06-engine canvasTex çizimlerinde kullanılan kısmı) karşılığı.
// Koordinatlar canvas'ın MANTIKSAL birimleridir (JS'teki cx.scale(Q,Q) burada Q ile çarpılarak uygulanır); çizim Paint'e yapılır.
// Renkler CSS dizgisi ('#78ad45', 'rgba(0,0,0,.28)') ya da Color olabilir (CssColor örtük dönüşümü).
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Ciftlik
{
    public struct CssColor
    {
        public Color c;
        public CssColor(Color c) { this.c = c; }
        public static implicit operator CssColor(string s) => new CssColor(Parse(s));
        public static implicit operator CssColor(Color c) => new CssColor(c);
        public static implicit operator Color(CssColor c) => c.c;

        static readonly Dictionary<string, Color> _memo = new Dictionary<string, Color>();
        public static Color Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return new Color(0, 0, 0, 1);
            Color r;
            if (_memo.TryGetValue(s, out r)) return r;
            var t = s.Trim().ToLowerInvariant();
            if (t.StartsWith("#"))
            {
                var h = t.Substring(1);
                if (h.Length == 3 || h.Length == 4) { var e = ""; foreach (var ch in h) e += new string(ch, 2); h = e; }
                int v = int.Parse(h.Substring(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                float a = h.Length >= 8 ? int.Parse(h.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f : 1;
                r = new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, a);
            }
            else if (t.StartsWith("rgb"))
            {
                int o = t.IndexOf('('), c = t.IndexOf(')');
                var parts = t.Substring(o + 1, c - o - 1).Split(',');
                float F(int i) => float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
                r = new Color(F(0) / 255f, F(1) / 255f, F(2) / 255f, parts.Length > 3 ? F(3) : 1);
            }
            else if (t == "white") r = new Color(1, 1, 1, 1);
            else if (t == "black") r = new Color(0, 0, 0, 1);
            else if (t == "transparent") r = new Color(0, 0, 0, 0);
            else r = new Color(0, 0, 0, 1);
            _memo[s] = r;
            return r;
        }
    }

    public class Ctx2D
    {
        public readonly Paint P;
        public readonly float Q;
        public readonly float W, H;   // mantıksal boyut
        public Ctx2D(Paint p, float q, float w, float h) { P = p; Q = q; W = w; H = h; }

        Vector2 S(float x, float y) => new Vector2(x * Q, y * Q);
        Vector2[] S(IList<Vector2> pts) { var r = new Vector2[pts.Count]; for (int i = 0; i < r.Length; i++) r[i] = new Vector2(pts[i].x * Q, pts[i].y * Q); return r; }

        public Ctx2D FillRect(float x, float y, float w, float h, CssColor c) { P.Rect(x * Q, y * Q, w * Q, h * Q, c); return this; }
        public Ctx2D StrokeRect(float x, float y, float w, float h, float lw, CssColor c)
        {
            // canvas strokeRect: çizgi kenarın ortasında
            FillRect(x - lw / 2, y - lw / 2, w + lw, lw, c); FillRect(x - lw / 2, y + h - lw / 2, w + lw, lw, c);
            FillRect(x - lw / 2, y + lw / 2, lw, h - lw, c); FillRect(x + w - lw / 2, y + lw / 2, lw, h - lw, c);
            return this;
        }
        public Ctx2D FillCircle(float cx, float cy, float r, CssColor c) { P.Circle(cx * Q, cy * Q, r * Q, c); return this; }
        public Ctx2D StrokeCircle(float cx, float cy, float r, float lw, CssColor c) { P.Ring(cx * Q, cy * Q, r * Q, lw * Q, c); return this; }
        public Ctx2D Line(float ax, float ay, float bx, float by, float lw, CssColor c) { P.Line(ax * Q, ay * Q, bx * Q, by * Q, lw * Q, c); return this; }
        public Ctx2D Polyline(IList<Vector2> pts, float lw, CssColor c) { P.Polyline(S(pts), lw * Q, c); return this; }
        public Ctx2D FillPoly(IList<Vector2> pts, CssColor c) { P.Poly(S(pts), c); return this; }
        public Ctx2D VGradient(float x, float y, float w, float h, CssColor top, CssColor bottom) { P.VGradient(x * Q, y * Q, w * Q, h * Q, top, bottom); return this; }
        public Ctx2D RoundRect(float x, float y, float w, float h, float r, CssColor c) { P.RoundRect(x * Q, y * Q, w * Q, h * Q, r * Q, c); return this; }
        public Ctx2D StrokeRoundRect(float x, float y, float w, float h, float r, float lw, CssColor c) { P.StrokeRoundRect(x * Q, y * Q, w * Q, h * Q, r * Q, lw * Q, c); return this; }
        // quadraticCurveTo / bezierCurveTo çizgileri (örneklenmiş)
        public static List<Vector2> Quad(float x0, float y0, float cx, float cy, float x1, float y1, int n = 12)
        {
            var r = new List<Vector2>(n + 1);
            for (int i = 0; i <= n; i++) { float t = (float)i / n, u = 1 - t; r.Add(new Vector2(u * u * x0 + 2 * u * t * cx + t * t * x1, u * u * y0 + 2 * u * t * cy + t * t * y1)); }
            return r;
        }
        public static List<Vector2> Bezier(float x0, float y0, float c1x, float c1y, float c2x, float c2y, float x1, float y1, int n = 20)
        {
            var r = new List<Vector2>(n + 1);
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n, u = 1 - t;
                r.Add(new Vector2(u * u * u * x0 + 3 * u * u * t * c1x + 3 * u * t * t * c2x + t * t * t * x1, u * u * u * y0 + 3 * u * u * t * c1y + 3 * u * t * t * c2y + t * t * t * y1));
            }
            return r;
        }
        public Ctx2D QuadStroke(float x0, float y0, float cx, float cy, float x1, float y1, float lw, CssColor c) => Polyline(Quad(x0, y0, cx, cy, x1, y1), lw, c);
        public Ctx2D BezierStroke(float x0, float y0, float c1x, float c1y, float c2x, float c2y, float x1, float y1, float lw, CssColor c) => Polyline(Bezier(x0, y0, c1x, c1y, c2x, c2y, x1, y1), lw, c);
        public Ctx2D Noise(float amount, int seed = 1) { P.Noise(amount, seed); return this; }

        // Çokgeni yatay şeride kırp (y0 ≤ y ≤ y1) — gradyanla doldurma için (Sutherland–Hodgman)
        public static List<Vector2> ClipY(IList<Vector2> poly, float y0, float y1)
        {
            var a = ClipHalf(poly, y0, true); return ClipHalf(a, y1, false);
        }
        static List<Vector2> ClipHalf(IList<Vector2> poly, float yl, bool keepAbove)
        {
            var r = new List<Vector2>();
            bool In(Vector2 p) => keepAbove ? p.y >= yl : p.y <= yl;
            for (int i = 0; i < poly.Count; i++)
            {
                var cur = poly[i]; var prev = poly[(i + poly.Count - 1) % poly.Count];
                bool ci = In(cur), pi = In(prev);
                if (ci != pi) { float t = (yl - prev.y) / (cur.y - prev.y); r.Add(new Vector2(prev.x + (cur.x - prev.x) * t, yl)); }
                if (ci) r.Add(cur);
            }
            return r;
        }
        // Çokgeni dikey gradyanla doldur (clip + linearGradient)
        public Ctx2D FillPolyVGradient(IList<Vector2> poly, float gy0, float gy1, CssColor top, CssColor bottom)
        {
            float minY = float.MaxValue, maxY = float.MinValue; foreach (var p in poly) { minY = Math.Min(minY, p.y); maxY = Math.Max(maxY, p.y); }
            float step = 1f / Q;
            for (float y = minY; y < maxY; y += step)
            {
                var strip = ClipY(poly, y, Math.Min(maxY, y + step));
                if (strip.Count < 3) continue;
                float t = gy1 > gy0 ? Mathf.Clamp01((y + step / 2 - gy0) / (gy1 - gy0)) : 0;
                Color a = top, b = bottom;
                FillPoly(strip, new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t));
            }
            return this;
        }
        public static bool Inside(IList<Vector2> poly, Vector2 p)
        {
            bool c = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
            return c;
        }
    }
}
