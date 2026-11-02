// ================= ÇİFTÇİNİN MASASI (ana menü) (25-desk.js) =================
// Three.js'teki ayrı "deskScene" sahnesi Unity'de DeskScene kök nesnesidir; masa nesneleri DESK_LAYER katmanındadır,
// masa kamerası (Dcam) yalnızca bu katmanı, dünya kameraları ise bu katman dışındakileri çizer. Masa ışıkları yalnızca
// masa katmanını aydınlatır; dünya ışıkları masa katmanından çıkarılır. Hemisfer ışığı Unity'de ortam ışığıdır (Trilight):
// masa görünürken masanın değerleri, dünya çizilirken dünyanın kendi değerleri kullanılır.
// Canvas dokuları Paint ile, üzerlerindeki yazılar Text3D ile yüzeye konur.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // userData.desk / base / s0 / pop / tagY (masa nesneleri; 08-markers ve 11-input bunu okur)
    public class DeskTag : MonoBehaviour { public string desk; public Vector3 @base; public float s0, pop, tagY; }

    public class DeskStat { public string c, n; }

    public class DeskLights { public Light fill, win, lamp; public float dh; }

    // Canvas 2D karşılığı: mantıksal (CSS) koordinatlarla çizer, q katı çözünürlükte Paint'e aktarır; yazıları sonra Text3D olarak yerleştirmek için toplar
    public class DeskLabel { public string s; public float x, y, px; public Color col; public TextAnchor align; public bool bold, italic, middle; }
    public class DeskTex
    {
        public readonly Paint p; public readonly float q; public readonly int w, h;
        public readonly List<DeskLabel> labels = new List<DeskLabel>();
        public DeskTex(int w, int h, float q = 2) { this.w = w; this.h = h; this.q = q; p = new Paint(Mathf.RoundToInt(w * q), Mathf.RoundToInt(h * q)); }
        public void Rect(float x, float y, float ww, float hh, Color c) { p.Rect(x * q, y * q, ww * q, hh * q, c); }
        public void StrokeRect(float x, float y, float ww, float hh, float lw, Color c)
        {
            Rect(x - lw / 2, y - lw / 2, ww + lw, lw, c); Rect(x - lw / 2, y + hh - lw / 2, ww + lw, lw, c);
            Rect(x - lw / 2, y + lw / 2, lw, hh - lw, c); Rect(x + ww - lw / 2, y + lw / 2, lw, hh - lw, c);
        }
        public void Line(float ax, float ay, float bx, float by, float lw, Color c) { p.Line(ax * q, ay * q, bx * q, by * q, lw * q, c); }
        public void Polyline(Vector2[] pts, float lw, Color c) { for (int i = 1; i < pts.Length; i++) Line(pts[i - 1].x, pts[i - 1].y, pts[i].x, pts[i].y, lw, c); }
        // setLineDash([dash,gap]) ile tek çizgi (desen her alt yolda baştan başlar)
        public void Dashed(float ax, float ay, float bx, float by, float lw, Color c, float dash, float gap)
        {
            float L = Mathf.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay)); if (L <= 0) return;
            float dx = (bx - ax) / L, dy = (by - ay) / L;
            for (float s = 0; s < L; s += dash + gap) { float e = Mathf.Min(L, s + dash); Line(ax + dx * s, ay + dy * s, ax + dx * e, ay + dy * e, lw, c); }
        }
        public void DashedRect(float x, float y, float ww, float hh, float lw, Color c, float dash, float gap)
        {
            // strokeRect tek kapalı yol: desen köşelerden devam eder
            var pts = new[] { new Vector2(x, y), new Vector2(x + ww, y), new Vector2(x + ww, y + hh), new Vector2(x, y + hh), new Vector2(x, y) };
            float carry = 0; bool on = true;
            for (int i = 1; i < pts.Length; i++)
            {
                var a = pts[i - 1]; var b = pts[i]; float L = Vector2.Distance(a, b), s = 0; var d = (b - a) / L;
                while (s < L)
                {
                    float seg = (on ? dash : gap) - carry, e = Mathf.Min(L, s + seg);
                    if (on) Line(a.x + d.x * s, a.y + d.y * s, a.x + d.x * e, a.y + d.y * e, lw, c);
                    if (s + seg <= L) { on = !on; carry = 0; } else carry += e - s;
                    s = e;
                }
            }
        }
        public void Circle(float cx, float cy, float r, Color c) { p.Circle(cx * q, cy * q, r * q, c); }
        public void Ring(float cx, float cy, float r, float lw, Color c) { p.Ring(cx * q, cy * q, r * q, lw * q, c); }
        public void Poly(Vector2[] pts, Color c) { var s = new Vector2[pts.Length]; for (int i = 0; i < pts.Length; i++) s[i] = pts[i] * q; p.Poly(s, c); }
        // createRadialGradient(cx,cy,r0, cx,cy,r1): r0'da renk a0, r1'de a1 (yatay kısa şeritlerle)
        public void Radial(float cx, float cy, float r0, float r1, Color c0, Color c1)
        {
            float step = 2;
            for (float y = 0; y < h; y += step / q) for (float x = 0; x < w; x += step / q)
                {
                    float d = Mathf.Sqrt((x + step / q / 2 - cx) * (x + step / q / 2 - cx) + (y + step / q / 2 - cy) * (y + step / q / 2 - cy));
                    var col = Color.Lerp(c0, c1, Mathf.Clamp01((d - r0) / (r1 - r0)));
                    if (col.a > 0.001f) p.Rect(x * q, y * q, step, step, col);
                }
        }
        // fillText: x,y canvas koordinatı; middle → textBaseline 'middle', değilse 'alphabetic'
        public void Text(string s, float x, float y, float px, Color c, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false, bool italic = false, bool middle = false)
        { labels.Add(new DeskLabel { s = s, x = x, y = y, px = px, col = c, align = align, bold = bold, italic = italic, middle = middle }); }
        public Texture2D Apply(bool repeat = false)
        {
            var t = p.Apply(); t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 8;
            return t;
        }
    }

    public static partial class Game
    {
        public const int DESK_LAYER = 30;
        public static int DeskMask => 1 << DESK_LAYER;

        public static Transform DeskScene;                     // const deskScene
        public static Color DeskBg = Hex3(0x3a2a1c);          // deskScene.background
        public static Camera Dcam;
        public static RenderTexture Rt;
        public static Camera WinCam;
        public static Color SkyTop = Hex3(0xa3c4c0);
        public static bool DeskPortrait = false;
        public static Dictionary<string, Transform> DeskObjs = new Dictionary<string, Transform>();
        public static List<Transform> DESKP = new List<Transform>();
        public static List<Action<float, float>> DeskFx = new List<Action<float, float>>();
        public static string DeskSig = "";
        public static DeskLights DeskL = new DeskLights();
        public static string HoverDesk = null;
        public static float DcamX = 0;
        public static Vector2 DPar = Vector2.zero;
        public static int WinFrame = 0;
        public static float DeskWinT = 0;
        public static int DeskShN = 0;
        static readonly List<UnityEngine.Object> Desk_owned = new List<UnityEngine.Object>();

        // Etiket bağlama noktaları: [x,y,z,("side")]
        public static readonly Dictionary<string, object[]> DANCH = new Dictionary<string, object[]>
        {
            { "clock", new object[] { .4, 0.0, .05, "side" } }, { "borsa", new object[] { 0.0, -.72, .05 } }, { "calendar", new object[] { 0.0, -.56, .05 } }, { "trophy", new object[] { 0.0, -.2, .12 } },
            { "map", new object[] { 0.0, 0.0, .8 } }, { "ledger", new object[] { 0.0, 0.0, .52 } }, { "mail", new object[] { 0.0, 0.0, .36 } }, { "radio", new object[] { 0.0, 0.0, .3 } },
            { "piggy", new object[] { 0.0, 0.0, .5 } }, { "basket", new object[] { 0.0, 0.0, .46 } }, { "gazete", new object[] { 0.0, 0.0, .4 } }, { "butce", new object[] { 0.0, 0.0, .32 } },
        };
        public static readonly Dictionary<string, string[]> DNAMES = new Dictionary<string, string[]>
        {
            { "borsa", new[] { "📈", "Borsa" } }, { "gazete", new[] { "📰", "İstatistik" } }, { "butce", new[] { "🧮", "Bütçe" } }, { "map", new[] { "🗺️", "Harita" } },
            { "ledger", new[] { "📒", "Sipariş Defteri" } }, { "mail", new[] { "✉️", "Posta" } }, { "calendar", new[] { "📅", "Takvim" } }, { "radio", new[] { "📻", "Radyo" } },
            { "piggy", new[] { "🐷", "Kumbara" } }, { "trophy", new[] { "🏆", "Koleksiyon" } }, { "basket", new[] { "🧺", "Ambar" } }, { "clock", new[] { "🕰️", "Ayarlar" } },
        };

        static double[] Desk_3(double a, double b, double c) => new double[] { a, b, c };
        static Color Desk_Rgba(int r, int g, int b, float a) => new Color(r / 255f, g / 255f, b / 255f, a);
        static string Desk_Num(double v) => Math.Abs(v) < 1e15 && v == Math.Floor(v) ? v.ToString("0", CultureInfo.InvariantCulture) : v.ToString("R", CultureInfo.InvariantCulture);
        static double Desk_M(string k) { var v = Get(S.market.m, k); return v != 0 ? v : 1; }   // S.market.m[k]||1

        // mulberry32 (06-engine rng ile aynı)
        static Func<double> Desk_Rng(uint seed)
        {
            uint a = seed;
            return () =>
            {
                unchecked
                {
                    a += 0x6D2B79F5;
                    uint t = (a ^ (a >> 15)) * (1 | a);
                    t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
                    return (t ^ (t >> 14)) / 4294967296.0;
                }
            };
        }

        static void Init_25()
        {
            var root = new GameObject("deskScene"); UnityEngine.Object.DontDestroyOnLoad(root);
            DeskScene = root.transform; root.layer = DESK_LAYER;
            var dc = new GameObject("dcam"); UnityEngine.Object.DontDestroyOnLoad(dc);
            Dcam = dc.AddComponent<Camera>(); Dcam.fieldOfView = 42; Dcam.nearClipPlane = .1f; Dcam.farClipPlane = 100;
            Dcam.clearFlags = CameraClearFlags.SolidColor; Dcam.backgroundColor = DeskBg; Dcam.cullingMask = DeskMask; Dcam.enabled = false;
            float dpr = Mathf.Max(1, UI.Scale);
            int RTW = Math.Min(2048, RoundI(Math.Max(UI.W, UI.H) * Math.Min(dpr, 2) * GFX.rtScale)), RTH = RoundI(RTW * .594);
            Rt = new RenderTexture(Math.Max(GFX.low ? 640 : 1024, RTW), Math.Max(GFX.low ? 380 : 608, RTH), 24);
            Rt.antiAliasing = GFX.low ? 1 : 4; Rt.Create();
            var wc = new GameObject("winCam"); UnityEngine.Object.DontDestroyOnLoad(wc);
            WinCam = wc.AddComponent<Camera>(); WinCam.fieldOfView = 34; WinCam.aspect = (float)Rt.width / Rt.height; WinCam.nearClipPlane = .5f; WinCam.farClipPlane = 500;
            WinCam.targetTexture = Rt; WinCam.enabled = false; WinCam.cullingMask = ~DeskMask; WinCam.clearFlags = CameraClearFlags.SolidColor; WinCam.backgroundColor = SkyTop;
            wc.transform.position = V(2, 24, 40); wc.transform.LookAt(V(2, 0, -4));
        }

        static Transform Dobj(string k, double[] p, double ry)
        {
            var g = G(DeskScene, p, ry);
            var tag = g.gameObject.AddComponent<DeskTag>(); tag.desk = k; tag.@base = g.localPosition;
            DeskObjs[k] = g; DESKP.Add(g); return g;
        }
        static DeskTag Desk_Tag(Transform g) => g.GetComponent<DeskTag>();

        // Three BoxGeometry yüz sırası (+x,−x,+y,−y,+z,−z) ile 6 alt örgülü birim kutu
        static Mesh Desk_box, Desk_quad;
        static void Desk_AddQuad(List<Vector3> vs, List<Vector3> ns, List<Vector2> uv, List<int> tris, Vector3 n, Vector3 u, Vector3 v)
        {
            int b = vs.Count; var c = n * .5f;
            vs.Add(c - u * .5f - v * .5f); vs.Add(c - u * .5f + v * .5f); vs.Add(c + u * .5f + v * .5f); vs.Add(c + u * .5f - v * .5f);
            for (int i = 0; i < 4; i++) ns.Add(n);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0));
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }
        static Mesh Desk_BoxMesh()
        {
            if (Desk_box != null) return Desk_box;
            var faces = new[]
            {
                new[] { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0) },
                new[] { new Vector3(-1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0) },
                new[] { new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1) },
                new[] { new Vector3(0, -1, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1) },
                new[] { new Vector3(0, 0, -1), new Vector3(1, 0, 0), new Vector3(0, 1, 0) },   // Three +z (kameraya bakan)
                new[] { new Vector3(0, 0, 1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0) },
            };
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>(); var all = new List<List<int>>();
            foreach (var f in faces) { var t = new List<int>(); Desk_AddQuad(vs, ns, uv, t, f[0], f[1], f[2]); all.Add(t); }
            var m = new Mesh { name = "deskBox" }; m.SetVertices(vs); m.SetNormals(ns); m.SetUVs(0, uv); m.subMeshCount = 6;
            for (int i = 0; i < 6; i++) m.SetTriangles(all[i], i);
            m.RecalculateBounds(); m.RecalculateTangents();
            return Desk_box = m;
        }
        static Mesh Desk_QuadMesh()
        {
            if (Desk_quad != null) return Desk_quad;
            var vs = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            Desk_AddQuad(vs, ns, uv, t, new Vector3(0, 0, -1), new Vector3(1, 0, 0), new Vector3(0, 1, 0));
            for (int i = 0; i < vs.Count; i++) vs[i] = new Vector3(vs[i].x, vs[i].y, 0);
            var m = new Mesh { name = "deskQuad" }; m.SetVertices(vs); m.SetNormals(ns); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return Desk_quad = m;
        }
        static Material Desk_Unlit(Texture tex)
        {
            var m = new Material(Eng_UnlitShader) { mainTexture = tex }; Desk_owned.Add(m); return m;   // "Unlit/Texture" yapıda bulunmayabilir
        }
        static Material Desk_Std(Texture2D tex, float rx, float ry)
        {
            Eng_TexRepeat(tex, rx, ry); var m = StdTex(tex); Desk_owned.Add(tex); Desk_owned.Add(m); return m;
        }

        // faceMesh: yalnızca bir yüzü dokulu kutu. Dönen nesne (ölçeksiz tutucu) döndürülebilir; yazılar tutucunun çocuğudur.
        static Transform FaceMesh(Transform par, double[] size, double[] pos, int faceIdx, DeskTex tex, int side)
        {
            var holder = G(par, pos, 0);
            var go = new GameObject("faceMesh"); go.transform.SetParent(holder, false); go.transform.localScale = new Vector3((float)size[0], (float)size[1], (float)size[2]);
            go.AddComponent<MeshFilter>().sharedMesh = Desk_BoxMesh();
            var mr = go.AddComponent<MeshRenderer>();
            var sm = M(side); var t2 = tex.Apply(); Desk_owned.Add(t2);
            var mats = new Material[6]; for (int i = 0; i < 6; i++) mats[i] = sm; mats[faceIdx] = Desk_Unlit(t2);
            mr.sharedMaterials = mats; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; mr.receiveShadows = true;
            Game.Outline(go.transform);
            Desk_PlaceLabels(holder, tex, size, faceIdx);
            return holder;
        }
        static void Desk_PlaceLabels(Transform holder, DeskTex t, double[] size, int faceIdx)
        {
            bool top = faceIdx == 2;
            float upp = (float)size[0] / t.w;   // dünya birimi / canvas pikseli
            foreach (var L in t.labels)
            {
                float capH = L.px * .72f * upp;
                float cy = L.middle ? L.y : L.y - L.px * .36f;            // alphabetic taban çizgisi → orta
                float lx = (L.x / t.w - .5f) * (float)size[0];
                Vector3 pos; Quaternion rot;
                if (top) { pos = V(lx, size[1] / 2 + .003f, (cy / t.h - .5f) * size[2]); rot = Quaternion.Euler(90, 0, 0); }
                else { pos = V(lx, (.5f - cy / t.h) * size[1], size[2] / 2 + .003f); rot = Quaternion.identity; }
                var tm = Text3D(holder, L.s, pos, capH, L.col, L.align, L.bold);
                tm.transform.localRotation = rot;
                if (L.italic) tm.fontStyle = L.bold ? FontStyle.BoldAndItalic : FontStyle.Italic;
            }
        }

        // ---------- Dokular ----------
        public static DeskTex MapTex()
        {
            var x = new DeskTex(640, 440); int w = 640, h = 440;
            x.Rect(0, 0, w, h, UI.Hex("#ead6a4"));
            x.Radial(w / 2f, h / 2f, h * .25f, w * .62f, new Color(0, 0, 0, 0), Desk_Rgba(120, 80, 30, .4f));
            Func<double, float> X = v => (float)((v + 34) / 82 * w), Y = v => (float)((v + 27) / 50 * h);
            var river = new double[,] { { -40, -26 }, { -15, -23.8 }, { 8, -25 }, { 30, -27 }, { 50, -24 } };
            var rp = new Vector2[5]; for (int i = 0; i < 5; i++) rp[i] = new Vector2(X(river[i, 0]), Y(river[i, 1]));
            x.Polyline(rp, 13, UI.Hex("#6aa6c8"));
            foreach (var kv in REG)
            {
                var d = kv.Value; if (d.rect == null || Owned(kv.Key)) continue;
                float x1 = d.rect[0], z1 = d.rect[1], x2 = d.rect[2], z2 = d.rect[3];
                x.Rect(X(x1), Y(z1), X(x2) - X(x1), Y(z2) - Y(z1), Desk_Rgba(120, 90, 60, .22f));
                x.DashedRect(X(x1), Y(z1), X(x2) - X(x1), Y(z2) - Y(z1), 2.5f, Desk_Rgba(90, 60, 30, .55f), 7, 5);
                x.Text("?", X((x1 + x2) / 2), Y((z1 + z2) / 2) + 9, 26, Desk_Rgba(70, 45, 20, .75f), TextAnchor.MiddleCenter, true);
            }
            var road = UI.Hex("#b08a52");
            x.Line(0, Y(13.8), w, Y(13.8), 8, road);
            foreach (var kv in LOC)
            {
                var L = kv.Value; if (!Owned(L.reg) || kv.Key == "ev" || L.reg == "town") continue;
                x.Dashed(X(-1), Y(5.6), X(L.p[0]), Y(L.p[2]), 4, road, 9, 6);
            }
            var ink = UI.Hex("#5a3a1a");
            foreach (var kv in LOC)
            {
                var L = kv.Value; if (!Owned(L.reg)) continue;
                float px = X(L.p[0]), py = Y(L.p[2]);
                x.Circle(px, py, 19, UI.Hex("#fff6e0")); x.Ring(px, py, 19, 3, ink);
                x.Text(L.e, px, py + 1, 22, Color.white, TextAnchor.MiddleCenter, false, false, true);
            }
            x.StrokeRect(8, 8, w - 16, h - 16, 7, ink);
            x.Text("Çiftlik Haritası", w / 2f, 34, 26, ink, TextAnchor.MiddleCenter, true, true, true);
            float cx = w - 58, cy = h - 58;
            x.Poly(new[] { new Vector2(cx, cy - 30), new Vector2(cx + 8, cy), new Vector2(cx, cy + 30), new Vector2(cx - 8, cy) }, ink);
            x.Text("K", cx, cy - 40, 16, ink, TextAnchor.MiddleCenter, true, false, true);
            return x;
        }

        public static DeskTex CalTex()
        {
            var x = new DeskTex(200, 240, 4); int w = 200, h = 240;
            x.Rect(0, 0, w, h, UI.Hex("#fbf3df")); x.Rect(0, 0, w, 58, UI.Hex("#c4492e"));
            var se = SEASONS[Season()];
            x.Text(se.n.ToUpperInvariant(), w / 2f, 39, 26, UI.Hex("#fff6e0"), TextAnchor.MiddleCenter, true);
            x.Text(Desk_Num(GameDay() % 30 + 1), w / 2f, 172, 112, UI.Hex("#2a1a10"), TextAnchor.MiddleCenter, true);
            x.Text(se.e, w / 2f, 220, 34, UI.Hex("#2a1a10"));
            foreach (var cx in new float[] { 50, 150 }) x.Circle(cx, 10, 6, UI.Hex("#555555"));
            return x;
        }

        public static string DeskSigNow()
        {
            var keys = S.regions.Keys.ToList(); keys.Sort(string.CompareOrdinal);
            return string.Join(",", keys) + "|" + S.album.Count + "|" + Desk_Num(GameDay()) + "|" + Desk_Num(S.market.at);
        }

        static Light Desk_Light(LightType type, int color, float intensity, float range, double[] p)
        {
            var go = new GameObject(type + "Light"); go.transform.SetParent(DeskScene, false); go.transform.localPosition = V(p[0], p[1], p[2]);
            var l = go.AddComponent<Light>(); l.type = type; l.color = Hex3(color); l.intensity = intensity; if (range > 0) l.range = range;
            l.cullingMask = DeskMask; l.shadows = LightShadows.None; return l;
        }

        public static void BuildDesk()
        {
            for (int i = DeskScene.childCount - 1; i >= 0; i--) { var c = DeskScene.GetChild(i).gameObject; c.SetActive(false); UnityEngine.Object.Destroy(c); }
            foreach (var o in Desk_owned) if (o != null) UnityEngine.Object.Destroy(o);
            Desk_owned.Clear();
            DeskObjs = new Dictionary<string, Transform>(); DESKP = new List<Transform>(); DeskFx = new List<Action<float, float>>();
            var sc = DeskScene;
            // HemisphereLight(0xfff0d6,0x5a4030,.7) → ortam ışığı (Desk_Ambient)
            DeskL = new DeskLights { dh = .7f };
            var fill = Desk_Light(LightType.Directional, 0xffe8c8, .75f, 0, Desk_3(-3, 6, 6));
            fill.transform.LookAt(DeskScene.position);
            fill.shadows = LightShadows.Soft; fill.shadowCustomResolution = 2048; fill.shadowBias = .05f; fill.shadowNormalBias = .4f;   // Three: bias −.0005, normalBias .03 → Unity ölçeğinde karşılıkları
            var win = Desk_Light(LightType.Point, 0xffe2b0, .8f, 9, Desk_3(0, 2.6, -1.1));
            var lamp = Desk_Light(LightType.Point, 0xffc070, .5f, 7, Desk_3(2.85, 2.05, -.55));
            DeskL.fill = fill; DeskL.win = win; DeskL.lamp = lamp;

            // Duvar kâğıdı
            var wpx = new DeskTex(128, 128); { int w = 128, h = 128; wpx.Rect(0, 0, w, h, UI.Hex("#efe2c2")); for (int i = 0; i < w; i += 32) wpx.Rect(i, 0, 14, h, Desk_Rgba(170, 110, 60, .13f));
                var r = Desk_Rng(4); for (int k = 0; k < 8; k++) { float a = (float)(r() * w), b = (float)(r() * h); wpx.Circle(a, b, 3.5f, Desk_Rgba(196, 73, 46, .35f)); wpx.Rect(a - 1, b + 3, 2, 6, Desk_Rgba(63, 122, 74, .4f)); } }
            var wp = Desk_Std(wpx.Apply(true), 5, 3);
            bool PT = DeskPortrait = Dcam.aspect < .85f;
            float WY = PT ? 3.55f : 2.65f, WW = PT ? 2.6f : 3.3f, WH = PT ? 1.6f : 1.95f, Z = -1.7f;
            F(sc, wp, Desk_3(4.4, 6, .2), Desk_3(-3.85, 3, Z)); F(sc, wp, Desk_3(4.4, 6, .2), Desk_3(3.85, 3, Z));
            F(sc, wp, Desk_3(WW, 6 - (WY + WH / 2), .2), Desk_3(0, (WY + WH / 2 + 6) / 2, Z)); F(sc, wp, Desk_3(WW, WY - WH / 2, .2), Desk_3(0, (WY - WH / 2) / 2, Z));
            F(sc, StdTex(TEX.planks("#8a5a34", "#7e5030", new float[] { 6, 1 }, true)), Desk_3(12, 1.1, .22), Desk_3(0, .55, Z + .05)); P(sc, "box", 0x5a3a20, Desk_3(12, .12, .3), Desk_3(0, 1.12, Z + .08));
            F(sc, StdTex(TEX.planks("#9a6a3e", "#8a5e36", new float[] { 4, 3 })), Desk_3(12, .1, 8), Desk_3(0, 0, 1.5));
            {
                var vw = new GameObject("window"); vw.transform.SetParent(sc, false); vw.transform.localPosition = V(0, WY, Z - .08); vw.transform.localScale = new Vector3(WW, WH, 1);
                vw.AddComponent<MeshFilter>().sharedMesh = Desk_QuadMesh(); var vr = vw.AddComponent<MeshRenderer>(); vr.sharedMaterial = Desk_Unlit(Rt);
                vr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; vr.receiveShadows = false;
                vw.AddComponent<DeskTag>().desk = "window"; DESKP.Add(vw.transform);
            }
            int fr = 0xf3ead8;
            P(sc, "box", fr, Desk_3(WW + .3, .16, .3), Desk_3(0, WY + WH / 2 + .08, Z + .05)); P(sc, "box", fr, Desk_3(WW + .5, .14, .45), Desk_3(0, WY - WH / 2 - .06, Z + .12));
            foreach (var s in new[] { -1, 1 }) P(sc, "box", fr, Desk_3(.16, WH + .3, .3), Desk_3(s * (WW / 2 + .08), WY, Z + .05));
            P(sc, "box", fr, Desk_3(.08, WH, .12), Desk_3(0, WY, Z)); P(sc, "box", fr, Desk_3(WW, .08, .12), Desk_3(0, WY, Z));
            { float px = PT ? -.75f : -1.2f; P(sc, "cyl", 0xbb5d3a, Desk_3(.16, .22, .16), Desk_3(px, WY - WH / 2 + .12, Z + .15)); P(sc, "ico1", 0x4f8f37, Desk_3(.22, .24, .22), Desk_3(px, WY - WH / 2 + .38, Z + .15)); }
            P(sc, "lsph", 0xe8657a, Desk_3(.07, .07, .07), Desk_3(PT ? -.67 : -1.12, WY - WH / 2 + .5, Z + .28));
            var gx = new DeskTex(64, 64); { gx.Rect(0, 0, 64, 64, UI.Hex("#f6efe0")); for (int i = 0; i < 64; i += 16) { gx.Rect(i, 0, 8, 64, Desk_Rgba(196, 73, 46, .55f)); gx.Rect(0, i, 64, 8, Desk_Rgba(196, 73, 46, .55f)); } }
            var ging = Desk_Std(gx.Apply(true), 3, 4);
            foreach (var s in new[] { -1, 1 }) { var c = G(sc, Desk_3(s * (WW / 2 + .45), WY + WH / 2 + .1, Z + .25), 0); P(c, "box", ging, Desk_3(.7, 2.3, .08), Desk_3(0, -1.15, 0)); P(c, "box", 0xc4492e, Desk_3(.72, .12, .1), Desk_3(0, -1.4, 0)); }
            P(sc, "cyl", 0x6e4a2a, Desk_3(.04, WW + 2, .04), Desk_3(0, WY + WH / 2 + .18, Z + .3), Desk_3(0, 0, Math.PI / 2));
            P(sc, "box", StdTex(TEX.planks("#a8733f", "#9b6837", new float[] { 4, 1 })), Desk_3(6.8, .18, 2.7), Desk_3(0, 1, -.1)); F(sc, 0x7a4a26, Desk_3(6.8, 1, .1), Desk_3(0, .5, 1.24));
            {
                var g = Dobj("map", Desk_3(-.95, 1.1, .2), .06f); FaceMesh(g, Desk_3(2.1, .02, 1.45), Desk_3(0, 0, 0), 2, MapTex(), 0xead6a4);
                P(g, "cyl", 0xead6a4, Desk_3(.07, 1.45, .07), Desk_3(1.05, .04, 0), Desk_3(Math.PI / 2, 0, 0));
                foreach (var a in new[] { new double[] { -.5, -.3, 0xc4492e }, new double[] { .3, .2, 0x3f6fa0 }, new double[] { -.1, .35, 0xf2b330 } }) P(g, "sph", (int)a[2], Desk_3(.05, .05, .05), Desk_3(a[0], .05, a[1]));
                Desk_Tag(g).tagY = .3f;
            }
            {
                var g = Dobj("ledger", Desk_3(1.15, 1.1, .3), -.15f); P(g, "box", 0x7a2f22, Desk_3(1.36, .06, .92), Desk_3(0, 0, 0));
                var pgx = new DeskTex(64, 96); { int w = 64, h = 96; pgx.Rect(0, 0, w, h, UI.Hex("#fbf3df")); for (int i = 12; i < h; i += 9) pgx.Line(4, i, w - 4, i, 1, Desk_Rgba(60, 90, 140, .35f));
                    for (int i = 15; i < h - 10; i += 9) { var pts = new List<Vector2> { new Vector2(8, i) }; for (int k = 8; k < w - 10; k += 6) pts.Add(new Vector2(k, i + Mathf.Sin(k) * 1.5f)); pgx.Polyline(pts.ToArray(), 1.2f, UI.Hex("#3a2a1a")); } }
                var pg = Desk_Std(pgx.Apply(), 1, 1);
                P(g, "box", pg, Desk_3(.62, .05, .86), Desk_3(-.32, .05, 0), Desk_3(0, 0, .04)); P(g, "box", pg, Desk_3(.62, .05, .86), Desk_3(.32, .05, 0), Desk_3(0, 0, -.04));
                var pen = G(g, Desk_3(.45, .12, .15), .6f); P(pen, "cyl", 0x2a2a2a, Desk_3(.025, .6, .025), Desk_3(0, 0, 0), Desk_3(Math.PI / 2, 0, 0)); P(pen, "cone", 0xe0b84a, Desk_3(.025, .08, .025), Desk_3(0, 0, .33), Desk_3(Math.PI / 2, 0, 0));
                Desk_Tag(g).tagY = .3f;
            }
            {
                var g = Dobj("mail", Desk_3(2.35, 1.1, -.5), -.3f); P(g, "box", 0x8a5a32, Desk_3(.9, .12, .6), Desk_3(0, .06, 0));
                for (int k = 0; k < 3; k++) { var e = G(g, Desk_3((k - 1) * .06, .16 + k * .05, (k - 1) * .04), (k - 1) * .15f); P(e, "box", 0xfbf3df, Desk_3(.7, .03, .45)); P(e, "box", 0xc4492e, Desk_3(.12, .035, .1), Desk_3(.25, 0, -.13)); }
                Desk_Tag(g).tagY = .45f;
            }
            {
                var g = Dobj("radio", Desk_3(-2.4, 1.1, -.55), .18f); P(g, "box", 0x8a5530, Desk_3(1.05, .62, .45), Desk_3(0, .31, 0)); P(g, "hsph", 0x8a5530, Desk_3(.52, .22, .225), Desk_3(0, .62, 0));
                var grx = new DeskTex(64, 32); { grx.Rect(0, 0, 64, 32, UI.Hex("#e9d6aa")); for (int i = 3; i < 64; i += 6) grx.Rect(i, 2, 2, 28, UI.Hex("#7a4e2d")); }
                P(g, "box", Desk_Std(grx.Apply(), 1, 1), Desk_3(.55, .36, .02), Desk_3(-.18, .3, .23));
                var dial = P(g, "cyl", 0xf2c94c, Desk_3(.13, .03, .13), Desk_3(.3, .36, .23), Desk_3(Math.PI / 2, 0, 0), new MatOpt { emissive = 0xf2b330, emissiveIntensity = .8f });
                foreach (var xx in new[] { .22, .38 }) P(g, "cyl", 0x3a2a1a, Desk_3(.05, .05, .05), Desk_3(xx, .12, .24), Desk_3(Math.PI / 2, 0, 0));
                var notes = new List<Transform>(); for (int k = 0; k < 3; k++) notes.Add(P(g, "lsph", 0x2a1a10, Desk_3(.05, .05, .05), null));
                var dialMat = dial.GetComponent<Renderer>().sharedMaterial;
                DeskFx.Add((dt, t) =>
                {
                    Eng_SetEmissiveIntensity(dialMat, S.sound.music ? .6f + Mathf.Sin(t * 4) * .2f : 0);
                    for (int k = 0; k < notes.Count; k++)
                    {
                        var n = notes[k]; float ph = (t * .5f + k / 3f) % 1;
                        n.gameObject.SetActive(S.sound.music);
                        SetPos(n, .1 + Math.Sin(ph * 6 + k) * .2, .85 + ph * .8, 0); n.localScale = Vector3.one * (.06f * (1 - ph) + .01f);
                    }
                });
                Desk_Tag(g).tagY = .95f;
            }
            {
                var g = Dobj("piggy", Desk_3(2.5, 1.1, .55), -.5f); P(g, "sph", 0xf3a6b8, Desk_3(.36, .3, .45), Desk_3(0, .32, 0)); P(g, "cyl", 0xe8889e, Desk_3(.12, .1, .12), Desk_3(0, .32, .46), Desk_3(Math.PI / 2, 0, 0));
                foreach (var s in new[] { -1, 1 })
                {
                    P(g, "cone", 0xf3a6b8, Desk_3(.08, .14, .08), Desk_3(s * .15, .6, .2)); P(g, "cyl", 0xf3a6b8, Desk_3(.06, .18, .06), Desk_3(s * .17, .08, .22));
                    P(g, "cyl", 0xf3a6b8, Desk_3(.06, .18, .06), Desk_3(s * .17, .08, -.22)); P(g, "lsph", 0x1a1a1a, Desk_3(.035, .035, .035), Desk_3(s * .12, .42, .37));
                }
                P(g, "box", 0x6a3a3a, Desk_3(.03, .02, .16), Desk_3(0, .62, -.05)); Desk_Tag(g).tagY = .85f;
            }
            {
                var g = Dobj("basket", Desk_3(-2.55, 1.1, .6), 0); P(g, "cyl", 0xb0804a, Desk_3(.42, .32, .42), Desk_3(0, .16, 0)); P(g, "tor", 0x8a5a32, Desk_3(.42, .42, .42), Desk_3(0, .32, 0), Desk_3(Math.PI / 2, 0, 0));
                var cols = new[] { 0xf6ead2, 0xd43a2a, 0xf6ead2, 0xf3c63c, 0xd9a066, 0x7cc04a };
                for (int k = 0; k < 6; k++) P(g, "sph", cols[k], Desk_3(.11, .13, .11), Desk_3(Math.Cos(k) * .2, .36, Math.Sin(k) * .2));
                Desk_Tag(g).tagY = .7f;
            }
            {
                var g = G(sc, PT ? Desk_3(-.2, 1.1, .95) : Desk_3(.2, 1.1, .85), 0); P(g, "cyl", 0xf6efe0, Desk_3(.13, .04, .13), Desk_3(0, .02, 0)); P(g, "cyl", 0xc4492e, Desk_3(.09, .17, .09), Desk_3(0, .11, 0)); P(g, "tor", 0xc4492e, Desk_3(.05, .05, .05), Desk_3(.1, .11, 0));
                var st = new List<KeyValuePair<Transform, Material>>();
                for (int k = 0; k < 3; k++) { var mat = ToonMat(new MatOpt { color = 0xffffff, transparent = true, opacity = .5f }); Desk_owned.Add(mat); var m = P(g, "ico1", mat, Desk_3(.04, .04, .04), null); st.Add(new KeyValuePair<Transform, Material>(m, mat)); }
                DeskFx.Add((dt, t) => { for (int k = 0; k < st.Count; k++) { var m = st[k].Key; float ph = (t * .4f + k / 3f) % 1; SetPos(m, Math.Sin(ph * 5 + k) * .05, .2 + ph * .5, 0); m.localScale = Vector3.one * (.03f + ph * .07f); Eng_SetOpacity(st[k].Value, .5f * (1 - ph)); } });
            }
            {
                var g = G(sc, Desk_3(3.0, 1.1, -.85), 0); if (PT) g.gameObject.SetActive(false);
                P(g, "cyl", 0x2f6b3a, Desk_3(.22, .06, .22), Desk_3(0, .03, 0)); P(g, "cyl", 0x2f6b3a, Desk_3(.03, .8, .03), Desk_3(0, .43, 0)); P(g, "cone", 0x2f6b3a, Desk_3(.28, .3, .28), Desk_3(-.1, .88, .15), Desk_3(.5, 0, .3));
                P(g, "sph", 0xfff1c8, Desk_3(.07, .07, .07), Desk_3(-.12, .8, .2), null, new MatOpt { emissive = 0xffd27a, emissiveIntensity = 1.2f });
            }
            { var g = Dobj("calendar", Desk_3(3.05, 3.35, -1.58), 0); FaceMesh(g, Desk_3(.85, 1.0, .03), Desk_3(0, 0, 0), 4, CalTex(), 0xfbf3df); P(g, "cyl", 0x555555, Desk_3(.03, .08, .03), Desk_3(0, .55, 0)); Desk_Tag(g).tagY = .62f; }
            {
                var g = Dobj("trophy", Desk_3(3.1, 2.0, -1.5), 0); P(g, "box", 0x7a4e2d, Desk_3(1.5, .08, .35), Desk_3(0, 0, 0)); foreach (var s in new[] { -1, 1 }) P(g, "box", 0x5a3a20, Desk_3(.06, .25, .3), Desk_3(s * .6, -.15, 0));
                int n = Math.Min(5, COL.Count(c => Get(S.album, c.id) != 0));
                for (int k = 0; k < 5; k++)
                {
                    double x = -.56 + k * .28;
                    if (k < n) { P(g, "cyl", 0xe0b84a, Desk_3(.07, .04, .07), Desk_3(x, .06, 0)); P(g, "cyl", 0xe0b84a, Desk_3(.03, .12, .03), Desk_3(x, .14, 0)); P(g, "cone", 0xe0b84a, Desk_3(.1, .16, .1), Desk_3(x, .28, 0), Desk_3(Math.PI, 0, 0)); }
                    else P(g, "box", 0xd8c9a6, Desk_3(.12, .18, .03), Desk_3(x, .13, -.1));
                }
                Desk_Tag(g).tagY = .32f;
            }
            {
                var g = Dobj("clock", Desk_3(0, 4.32, -1.5), 0); P(g, "cyl", 0x7a4e2d, Desk_3(.36, .06, .36), Desk_3(0, 0, 0), Desk_3(Math.PI / 2, 0, 0)); P(g, "cyl", 0xfbf3df, Desk_3(.3, .07, .3), Desk_3(0, 0, .01), Desk_3(Math.PI / 2, 0, 0));
                for (int k = 0; k < 12; k++) { double a = k / 12.0 * Math.PI * 2; P(g, "box", 0x2a1a10, Desk_3(.025, .06, .02), Desk_3(Math.Sin(a) * .25, Math.Cos(a) * .25, .05), Desk_3(0, 0, -a)); }
                Transform hh = G(g, Desk_3(0, 0, .06), 0), mm = G(g, Desk_3(0, 0, .07), 0); P(hh, "box", 0x2a1a10, Desk_3(.035, .15, .02), Desk_3(0, .075, 0)); P(mm, "box", 0x2a1a10, Desk_3(.025, .23, .02), Desk_3(0, .115, 0));
                DeskFx.Add((dt, t) => { double ph = DayPhase() * 24; SetRot(hh, 0, 0, -(ph % 12) / 12 * Math.PI * 2); SetRot(mm, 0, 0, -(ph % 1) * Math.PI * 2); });
                Desk_Tag(g).tagY = .45f;
            }
            // BORSA PANOSU (tebeşir tahtası)
            {
                var g = Dobj("borsa", Desk_3(-3.05, 2.65, -1.58), 0); P(g, "box", 0x7a4e2d, Desk_3(1.5, 1.25, .06), Desk_3(0, 0, 0)); FaceMesh(g, Desk_3(1.36, 1.1, .05), Desk_3(0, 0, .03), 4, BorsaTex(), 0x2f4a3a);
                P(g, "box", 0x7a4e2d, Desk_3(1.4, .07, .14), Desk_3(0, -.62, .06)); P(g, "box", 0xf6f2e6, Desk_3(.14, .04, .04), Desk_3(-.3, -.57, .08)); P(g, "box", 0xf3b6c8, Desk_3(.12, .04, .04), Desk_3(-.1, -.57, .1));
                Desk_Tag(g).tagY = .72f;
            }
            // ÇİFTLİK GAZETESİ
            { var g = Dobj("gazete", Desk_3(.05, 1.1, -.62), .12f); FaceMesh(g, Desk_3(.95, .03, .66), Desk_3(0, 0, 0), 2, PaperTex(), 0xf3ead2); SetRotY(FaceMesh(g, Desk_3(.95, .025, .66), Desk_3(.03, .03, .02), 2, PaperTex(), 0xf3ead2), -.08); Desk_Tag(g).tagY = .25f; }
            // BÜTÇE (hesap makinesi)
            {
                var g = Dobj("butce", Desk_3(1.32, 1.1, -.66), -.2f); P(g, "box", 0x3f5a4a, Desk_3(.62, .26, .5), Desk_3(0, .13, 0));
                var top = G(g, Desk_3(0, .27, .05), 0); SetRot(top, -.25, 0, 0); P(top, "box", 0x2f4a3a, Desk_3(.56, .06, .36));
                var kc = new[] { 0xf6f2e6, 0xf6f2e6, 0xc4492e };
                for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) P(top, "cyl", kc[r == 2 && c == 3 ? 2 : 0], Desk_3(.04, .04, .04), Desk_3(-.18 + c * .12, .05, -.1 + r * .1));
                P(g, "cyl", 0xf6f2e6, Desk_3(.08, .36, .08), Desk_3(0, .38, -.2), Desk_3(0, 0, Math.PI / 2)); P(g, "box", 0xfbf3df, Desk_3(.24, .005, .4), Desk_3(0, .39, -.42), Desk_3(-.6, 0, 0));
                var crank = G(g, Desk_3(.33, .18, 0), 0); P(crank, "box", 0xc9ccd2, Desk_3(.04, .04, .3), Desk_3(0, 0, .15)); P(crank, "sph", 0xc4492e, Desk_3(.05, .05, .05), Desk_3(0, 0, .3));
                Desk_Tag(g).tagY = .38f;
            }
            if (PT)
            {
                var LAY = new Dictionary<string, double[]>
                {
                    { "clock", new[] { 0, 4.9, -1.5, .7 } }, { "borsa", new[] { -.68, 2.05, -1.58, .6 } }, { "calendar", new[] { .38, 2.1, -1.58, .48 } }, { "trophy", new[] { .98, 1.7, -1.5, .42 } },
                    { "radio", new[] { -.95, 1.1, -.72, .6, .2 } }, { "gazete", new[] { .05, 1.1, -.72, .8, .1 } }, { "butce", new[] { .95, 1.1, -.7, .75, -.25 } }, { "map", new[] { -.4, 1.1, .05, .72, .05 } }, { "ledger", new[] { .85, 1.1, .12, .62, -.15 } },
                    { "basket", new[] { -.98, 1.1, .95, .6, 0 } }, { "mail", new[] { .4, 1.1, .9, .65, -.2 } }, { "piggy", new[] { 1.0, 1.1, .95, .55, -.5 } },
                };
                foreach (var kv in LAY)
                {
                    Transform g; if (!DeskObjs.TryGetValue(kv.Key, out g)) continue; var a = kv.Value;
                    SetPos(g, a[0], a[1], a[2]); if (a.Length > 4) SetRotY(g, a[4]);
                    var tg = Desk_Tag(g); tg.s0 = (float)a[3]; g.localScale = Vector3.one * (float)a[3]; tg.@base = g.localPosition;
                }
            }
            // Seçim için çarpıştırıcılar (11-input ışın testi) ve masa katmanı
            foreach (var root in DESKP) foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<BoxCollider>();
            Desk_SetLayer(DeskScene);
            Desk_IsolateLights();   // dünya güneşi ilk pencere çiziminden önce de masayı aydınlatmasın
            DeskSig = DeskSigNow();
        }
        static void Desk_SetLayer(Transform t) { t.gameObject.layer = DESK_LAYER; for (int i = 0; i < t.childCount; i++) Desk_SetLayer(t.GetChild(i)); }

        public static int DeskPanRange() { float a = Dcam.aspect; return a < .85f ? 0 : a < 1.3f ? 1 : 0; }

        // Hemisfer ışığı → ortam ışığı. Açıkken masanın, kapalıyken dünyanın (kaydedilen) ortam ışığı geçerlidir.
        static bool Desk_ambOn; static AmbientMode Desk_wMode; static Color Desk_wSky, Desk_wEq, Desk_wGr, Desk_wAmb; static float Desk_wInt;
        public static void Desk_Ambient(bool on)
        {
            if (on)
            {
                if (!Desk_ambOn) { Desk_wMode = RenderSettings.ambientMode; Desk_wSky = RenderSettings.ambientSkyColor; Desk_wEq = RenderSettings.ambientEquatorColor; Desk_wGr = RenderSettings.ambientGroundColor; Desk_wAmb = RenderSettings.ambientLight; Desk_wInt = RenderSettings.ambientIntensity; Desk_ambOn = true; }
                // 06-engine Eng_ApplyHemi ile aynı dönüşüm (Three: doğrusal renk × yoğunluk → sRGB'ye geri)
                float I = DeskL.dh; Color skyL = Hex3(0xfff0d6).linear, grL = Hex3(0x5a4030).linear;
                RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = (skyL * I).gamma; RenderSettings.ambientGroundColor = (grL * I).gamma; RenderSettings.ambientEquatorColor = (Color.Lerp(skyL, grL, .5f) * I).gamma;
            }
            else if (Desk_ambOn)
            {
                RenderSettings.ambientMode = Desk_wMode; RenderSettings.ambientSkyColor = Desk_wSky; RenderSettings.ambientEquatorColor = Desk_wEq; RenderSettings.ambientGroundColor = Desk_wGr; RenderSettings.ambientLight = Desk_wAmb; RenderSettings.ambientIntensity = Desk_wInt;
                Desk_ambOn = false;
            }
        }
        // Dünya ışıkları masayı aydınlatmasın (ayrı sahne davranışı)
        static void Desk_IsolateLights()
        {
            var ls = UnityEngine.Object.FindObjectsOfType<Light>(); if (ls == null) return;
            foreach (var l in ls) if (DeskScene == null || !l.transform.IsChildOf(DeskScene)) l.cullingMask &= ~DeskMask;
        }

        public static void DeskFrame(float dt, float t)
        {
            float d = (float)DayFactor();
            if (DeskL.fill != null) { DeskL.fill.intensity = .45f + .45f * d; DeskL.win.intensity = .3f + .9f * d; DeskL.lamp.intensity = .35f + (1 - d) * 1.3f; DeskL.dh = .5f + .3f * d; }
            Desk_Ambient(true);
            for (int i = 0; i < DeskFx.Count; i++) DeskFx[i](dt, t);
            foreach (var kv in DeskObjs)
            {
                var g = kv.Value; var u = Desk_Tag(g); bool hv = HoverDesk == kv.Key;
                float pop = u.pop; if (pop > 0) { pop = Math.Max(0, pop - dt * 2.5f); u.pop = pop; }
                g.localPosition = u.@base; float s0 = u.s0 != 0 ? u.s0 : 1; g.localScale = Vector3.one * s0;
            }
            double a = Dcam.aspect, vf = Dcam.fieldOfView * Math.PI / 180, hf = 2 * Math.Atan(Math.Tan(vf / 2) * a);
            bool PT = a < .85; double W = PT ? 1.5 : a < 1.3 ? 3 : 3.9;
            double dist = Math.Max(PT ? 2.95 / Math.Tan(vf / 2) : 2.5 / Math.Tan(vf / 2), Math.Min(12, W / Math.Tan(hf / 2))), ly = PT ? (a > .52 ? 2.95 : 2.62) : 2.85;
            if (PT) DcamX = 0;
            Dcam.transform.position = V(DcamX, PT ? (a > .52 ? 5.1 : 4.9) : 3.45, PT ? dist * (a > .52 ? 1.04 : .93) : dist);
            Dcam.transform.LookAt(V(DcamX * .9, ly, -.8));
        }

        // Penceredeki harita artık canlı değil: birkaç saniyede bir yenilenen sabit bir manzara (masa ekranı çok daha hafif ve akıcı)
        public static void RenderWindow()
        {
            StepWinCam(10);
            bool act = DeskScene.gameObject.activeSelf; DeskScene.gameObject.SetActive(false);
            bool amb = Desk_ambOn; Desk_Ambient(false); Desk_IsolateLights();
            WinCam.clearFlags = CameraClearFlags.SolidColor; WinCam.backgroundColor = SkyTop; WinCam.cullingMask = (Cam != null ? Cam.cullingMask : ~0) & ~DeskMask; WinCam.targetTexture = Rt;
            if (Rt != null && !Rt.IsCreated()) Rt.Create();
            WinCam.Render();
            DeskScene.gameObject.SetActive(act); if (amb) Desk_Ambient(true);
        }

        public static DeskStat DeskStatus(string k)
        {
            switch (k)
            {
                case "borsa": { int n = ITEMS.Keys.Count(x => Has(x) > 0 && Desk_M(x) >= 1.12); return n != 0 ? new DeskStat { c = "ok", n = "▲" + n } : null; }
                case "map": { int n = CountReady(); return n != 0 ? new DeskStat { c = "ok", n = n.ToString() } : null; }
                case "ledger": { int n = ReadyOrders(); return n != 0 ? new DeskStat { c = "ok", n = n.ToString() } : null; }
                case "mail": { int n = S.contracts.Count(c => Has(c.k) > 0); return n != 0 ? new DeskStat { c = "ok", n = n.ToString() } : null; }
                case "calendar": return (S.daily != null && (!S.daily.login || S.daily.tasks.Any(t => t.p >= t.n && !t.c))) || GoalReady() ? new DeskStat { c = "new", n = "!" } : null;
                case "basket": return (double)Stored() / Cap() >= .9 ? new DeskStat { c = "warn", n = "!" } : null;
                case "trophy": { int n = AchReady().Count; return n != 0 ? new DeskStat { c = "new", n = "🏅" + n } : null; }
                case "radio": return new DeskStat { c = "idle", n = S.sound.music ? "♪" : "✕" };
                default: return null;
            }
        }

        public static void DeskAction(string k)
        {
            Transform g; if (DeskObjs.TryGetValue(k, out g)) Desk_Tag(g).pop = 1;
            switch (k)
            {
                case "map": case "window": EnterWorld(); break;
                case "ledger": OpenView("ledger"); break;   // view={k:"ledger"};renderSheet()
                case "mail": OpenView("mail"); break;   // view={k:"mail"};renderSheet()
                case "calendar": OpenView("goals"); break;
                case "radio": S.sound.music = !S.sound.music; AudioInit(); ApplySound(); Save(); UI.Toast(S.sound.music ? "📻 Radyo açıldı" : "📻 Radyo kapandı"); SyncMarks(); break;
                case "piggy": OpenView("shop"); break;
                case "trophy": OpenView("album"); break;
                case "basket": OpenView("barn"); break;
                case "clock": OpenView("settings"); break;
                case "borsa": OpenView("borsa"); break;   // view={k:"borsa"};renderSheet()
                case "gazete": OpenView("stats"); break;   // view={k:"stats"};renderSheet()
                case "butce": OpenView("budget"); break;   // view={k:"budget"};renderSheet()
            }
        }

        public static DeskTex BorsaTex()
        {
            var x = new DeskTex(300, 240); int w = 300, h = 240;
            x.Rect(0, 0, w, h, UI.Hex("#2f4a3a"));
            for (int i = 0; i < 30; i++) x.Rect((float)(Rnd() * w), (float)(Rnd() * h), 40, 2, new Color(1, 1, 1, .05f));
            var chalk = UI.Hex("#f6f2e6");
            x.Text("KÖY BORSASI", w / 2f, 34, 26, chalk, TextAnchor.MiddleCenter, true); x.Rect(40, 44, w - 80, 2, chalk);
            var ks = ITEMS.Keys.Where(k => k != "at" && k != "yem").OrderByDescending(k => Math.Abs(Desk_M(k) - 1)).Take(6).ToList();
            for (int i = 0; i < ks.Count; i++)
            {
                var k = ks[i]; double m = Desk_M(k); float y = 74 + i * 28;
                x.Text(ITEMS[k].n, 22, y, 20, chalk, TextAnchor.MiddleLeft);
                x.Text("₺" + Desk_Num(SellPrice(k)), w - 62, y, 20, chalk, TextAnchor.MiddleRight);
                x.Text(m >= 1 ? "▲" : "▼", w - 24, y, 20, UI.Hex(m >= 1 ? "#9fe08a" : "#f39a8a"), TextAnchor.MiddleRight);
            }
            return x;
        }

        public static DeskTex PaperTex()
        {
            var x = new DeskTex(256, 180, 3); int w = 256;
            x.Rect(0, 0, w, 180, UI.Hex("#f3ead2")); var ink = UI.Hex("#2a1a10");
            x.Text("ÇİFTLİK POSTASI", w / 2f, 28, 22, ink, TextAnchor.MiddleCenter, true); x.Rect(12, 36, w - 24, 2, ink);
            x.Text("Seviye " + S.lv + " çiftlik büyüyor!", w / 2f, 56, 14, ink, TextAnchor.MiddleCenter, true);
            for (int c = 0; c < 3; c++) for (int l = 0; l < 9; l++) x.Rect(16 + c * 80, 70 + l * 11, 66 - (l % 3) * 8, 4, Desk_Rgba(42, 26, 16, .55f));
            x.Rect(176, 70, 64, 44, UI.Hex("#b9a77e"));
            return x;
        }

        // Küçük çizgi grafik (svg polyline)
        public static void Spark(Transform parent, List<double> arr)
        {
            if (arr == null || arr.Count < 2) return;
            double mn = arr.Min(), mx = arr.Max(), r = mx - mn; if (r == 0) r = 1;
            var x = new DeskTex(62, 20, 2); var pts = new Vector2[arr.Count];
            for (int i = 0; i < arr.Count; i++) pts[i] = new Vector2((float)(i / (double)(arr.Count - 1) * 60) + 1, (float)(18 - (arr[i] - mn) / r * 16));
            bool up = arr[arr.Count - 1] >= arr[0];
            x.Polyline(pts, 2, UI.Hex(up ? "#3f8a3a" : "#c4492e"));
            var img = UI.Picture(parent, x.Apply(), 62, 20); UI.Size(img, 62, 20);
        }

        // ---------- Sayfa parçaları (game.css: .job .pw .note .uhead .tiles .meterbar .inv .tabs) ----------
        static RectTransform Desk_Job(Transform parent, int padV = 8, int padH = 12)
        {
            var box = UI.Box(parent, UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "job");
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.padding = UI.Pad(padV, padH); h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            return box;
        }
        static RectTransform Desk_Pw(Transform parent, string b, string small)
        {
            var pw = UI.VStack(parent, 3, null, TextAnchor.UpperLeft, "pw"); UI.Size(pw, 0, -1, 1);
            UI.Txt(pw, b, 13.5f, UI.C.text, true);
            if (small != null) UI.Txt(pw, small, 11.5f, UI.C.muted, true);
            return pw;
        }
        static Text Desk_Note(Transform parent, string s) => UI.Txt(parent, s, 12.5f, UI.C.muted, true);
        static RectTransform Desk_UHead(Transform parent, string s, string tm = null)
        {
            var uh = UI.HStack(parent, 8, TextAnchor.MiddleLeft, null, "uhead");
            var t = UI.Txt(uh, s, 14, UI.Hex("#3a2414"), false, TextAnchor.MiddleLeft, false, true); UI.Size(t, -1, -1, 1);
            if (tm != null) UI.Txt(uh, tm, 10.5f, UI.C.muted, true, TextAnchor.MiddleRight, false);
            return uh;
        }
        static void Desk_Tabs(Transform parent, string sel, params string[] kv)
        {
            var tabs = UI.Box(parent, UI.C.paper2, 999, 2, UI.C.ink, null, "tabs");
            var h = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 5; h.padding = UI.Pad(3); h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = false;
            for (int i = 0; i + 1 < kv.Length; i += 2)
            {
                bool on = kv[i] == sel; var tk = kv[i];
                var b = UI.Box(tabs, on ? UI.C.card : UI.C.clear, 999, on ? 1.5f : 0, UI.C.ink, null, "tab"); UI.Size(b, -1, 28, 1);
                var t = UI.Txt(b, kv[i + 1], 12.5f, UI.C.text, true, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform); UI.IgnoreLayout(t);
                UI.Clickable(b, () => Act("vtab", D("t", tk)));
            }
        }
        static RectTransform Desk_Row(Transform parent, int n, float gap)
        {
            var row = UI.HStack(parent, gap, TextAnchor.UpperLeft, null, "row");
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            return row;
        }

        public static void SheetBorsa(Transform parent)
        {
            RefreshMarket();
            int left = Math.Max(0, CeilI((S.market.at + 180000 - Now()) / 1000));
            var ks = Producible().Concat(ITEMS.Keys.Where(k => Has(k) > 0)).Distinct().Where(k => k != "yem").OrderByDescending(k => Desk_M(k)).ToList();
            Head(parent, "📈", "Köy Borsası", "Fiyatlar " + Secs(left) + " sonra değişir");
            var body = ShBody(parent);
            Desk_Note(body, "Yeşil oklu ürünleri şimdi satmak kârlı. Satış Köy Pazarı'nda yapılır.");
            var hist = S.market.h ?? new Dictionary<string, List<double>>();
            foreach (var k in ks)
            {
                double m = Desk_M(k); int pc = RoundI((m - 1) * 100);
                var job = Desk_Job(body, 6, 10);
                UI.Txt(job, ITEMS[k].e, 22, null, false, TextAnchor.MiddleCenter, false);
                Desk_Pw(job, ITEMS[k].n + " · ₺" + Desk_Num(SellPrice(k)), "Elde " + Has(k) + " · taban ₺" + Desk_Num(ITEMS[k].p) + (SatF(k) < .85 ? " · 📉 pazar doydu" : ""));
                List<double> h; hist.TryGetValue(k, out h); Spark(job, h);
                var pt = UI.Txt(job, (pc >= 0 ? "▲" : "▼") + Math.Abs(pc) + "%", 14, pc >= 0 ? UI.C.forest : UI.C.brick, true, TextAnchor.MiddleRight, false); UI.Size(pt, -1, -1, -1, -1, 46);
            }
            UI.Btn(body, "🏪 Pazara git", () => { PLATFORM.Hap(); GoLocation("pazar"); }, "big gold");
        }

        public static void SheetBudget(Transform parent)
        {
            FinRoll();
            var Fn = S.fin; Func<Dictionary<string, double>, double> sm = o => o.Values.Sum();
            double ti = sm(Fn.inc), te = sm(Fn.exp), mx = Math.Max(1, Math.Max(Fn.inc.Count > 0 ? Fn.inc.Values.Max() : double.MinValue, Fn.exp.Count > 0 ? Fn.exp.Values.Max() : double.MinValue));
            Head(parent, "🧮", "Bütçe", "Bugün net " + (ti - te >= 0 ? "+" : "") + "₺" + Fmt(ti - te));
            var body = ShBody(parent);
            var tiles = Desk_Row(body, 3, 7);
            foreach (var tl in new[] { new object[] { "Gelir", ti, UI.C.forest }, new object[] { "Gider", te, UI.C.brick }, new object[] { "Kasa", S.coins, UI.C.text } })
            {
                var tile = UI.Box(tiles, UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "tile"); UI.Size(tile, 0, -1, 1);
                var v = tile.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 2; v.padding = UI.Pad(6, 3, 6, 5); v.childAlignment = TextAnchor.UpperCenter; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                UI.Txt(tile, (string)tl[0], 10.5f, UI.C.muted, true, TextAnchor.MiddleCenter, false);
                UI.Txt(tile, "₺" + Fmt((double)tl[1]), 16, (Color)tl[2], true, TextAnchor.MiddleCenter, false);
            }
            Action<Dictionary<string, double>, Color> rows = (o, col) =>
            {
                if (o.Count == 0) { Desk_Note(body, "Henüz kayıt yok."); return; }
                foreach (var kv in o.OrderByDescending(e => e.Value))
                {
                    var mb = UI.HStack(body, 8, TextAnchor.MiddleLeft, null, "meterbar");
                    var lb = UI.Txt(mb, kv.Key, 12, UI.C.text, true, TextAnchor.MiddleLeft, false); UI.Size(lb, 78, -1);
                    var bar = UI.Bar(mb, (float)(kv.Value / mx), col, 10, UI.Hex("#e8dcbf"));
                    var vt = UI.Txt(mb, "₺" + Fmt(kv.Value), 12, UI.C.text, true, TextAnchor.MiddleRight, false); UI.Size(vt, -1, -1, -1, -1, 62);
                }
            };
            Desk_UHead(body, "Gelirler"); rows(Fn.inc, UI.C.leaf);
            Desk_UHead(body, "Giderler"); rows(Fn.exp, UI.C.brick);
            Desk_UHead(body, "Son günler");
            var hist = new List<FinDay>(Fn.hist) { new FinDay { d = "Bugün", i = ti, e = te } };
            double hm = Math.Max(1, hist.Max(hh => Math.Max(hh.i, hh.e)));
            var chart = UI.VStack(body, 0, null, TextAnchor.LowerLeft, "chart"); UI.Size(chart, -1, 92);
            var cols = UI.HStack(chart, 8, TextAnchor.LowerLeft, UI.Pad(2, 2, 4, 0), "cols"); UI.Size(cols, -1, 90);
            const float innerH = 86;
            foreach (var hh in hist)
            {
                var col = UI.HStack(cols, 2, TextAnchor.LowerCenter, null, "day"); UI.Size(col, 0, innerH, 1);   // flex:1; max-width:48px
                var ib = UI.Box(col, UI.C.leaf, 4, 1.5f, UI.C.ink, null, "i"); UI.Size(ib, 0, Math.Max(1, (float)(hh.i / hm * innerH)), 1);
                var eb = UI.Box(col, UI.C.brick, 4, 1.5f, UI.C.ink, null, "e"); UI.Size(eb, 0, Math.Max(1, (float)(hh.e / hm * innerH)), 1);
            }
            UI.Size(UI.Fill(chart, UI.C.ink, 0, "base"), -1, 2);
            Desk_Note(body, "Yeşil gelir, kırmızı gider. Son sütun bugün.");
        }

        public static void SheetStats(Transform parent)
        {
            var st = S.stats;
            Func<string, double> sum = p => st.Where(kv => kv.Key.StartsWith(p, StringComparison.Ordinal)).Sum(kv => kv.Value);
            double created = S.created != 0 ? S.created : Now();
            int days = Math.Max(1, CeilI((Now() - created) / 864e5));
            var items = new List<string[]>
            {
                new[] { "🌾", "Tarla hasadı", Desk_Num(Get(st, "harvest_field")) }, new[] { "🏭", "Bina üretimi", Desk_Num(sum("harvest_") - Get(st, "harvest_field")) },
                new[] { "📜", "Sipariş", Desk_Num(Get(st, "orders")) }, new[] { "🤝", "Sözleşme", Desk_Num(Get(st, "contracts")) }, new[] { "💰", "Satılan ürün", Desk_Num(Get(st, "sold")) },
                new[] { "🔨", "Kurulan bina", Desk_Num(Get(st, "built")) }, new[] { "🎪", "Panayır yarışması", Desk_Num(Get(st, "contest")) }, new[] { "🏇", "At yarışı", Desk_Num(Get(st, "race")) },
                new[] { "🎮", "Mini oyun", Desk_Num(Get(st, "mg")) }, new[] { "📖", "Koleksiyon", COL.Count(c => Get(S.album, c.id) != 0) + "/" + COL.Count },
                new[] { "🗺️", "Arazi", S.regions.Count + "/" + REG.Count }, new[] { "📅", "Oyun günü", days.ToString() },
            };
            var best = st.Where(kv => kv.Key.StartsWith("make_", StringComparison.Ordinal)).OrderByDescending(kv => kv.Value).Select(kv => (KeyValuePair<string, double>?)kv).FirstOrDefault();
            string bestTxt = "Çiftlik yeni kuruldu!";
            if (best.HasValue)
            {
                var id = best.Value.Key.Substring(5);
                var rec = B.Values.SelectMany(b => b.recipes ?? new List<Recipe>()).Concat(CROPS).FirstOrDefault(r => r.id == id);
                bestTxt = "En çok üretilen: " + (rec != null ? rec.n : id) + " (" + Desk_Num(best.Value.Value) + ")";
            }
            Head(parent, "📰", "Çiftlik Postası", "İstatistikler");
            var body = ShBody(parent);
            var job = Desk_Job(body);
            UI.Txt(job, "🗞️", 26, null, false, TextAnchor.MiddleCenter, false);
            Desk_Pw(job, bestTxt, "Seviye " + S.lv + " · İtibar " + Desk_Num(S.rep) + " · " + HOUSE[S.house - 1].n);
            var inv = UI.VStack(body, 7, null, TextAnchor.UpperLeft, "inv");
            RectTransform row = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (i % 4 == 0) row = Desk_Row(inv, 4, 7);
                var it = items[i];
                var slot = UI.Box(row, UI.C.card, 12, 2, UI.C.ink, new Vector2(2, 2), "slot"); UI.Size(slot, 0, -1, 1);
                var v = slot.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = UI.Pad(5, 2); v.childAlignment = TextAnchor.UpperCenter; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                UI.Txt(slot, it[0], 22, null, false, TextAnchor.MiddleCenter, false);
                UI.Txt(slot, it[2], 13.5f, UI.C.text, true, TextAnchor.MiddleCenter, false);
                UI.Txt(slot, it[1], 10, UI.C.muted, true, TextAnchor.MiddleCenter, true);
            }
            if (row != null) for (int i = items.Count % 4; i != 0 && i < 4; i++) UI.Size(UI.New("pad", row), 0, -1, 1);
        }

        public static void SheetLedger(Transform parent)
        {
            var tb = View.tab ?? "o";
            Head(parent, "📒", "Sipariş Defteri", "★ ×" + RepMult().ToString("0.00", CultureInfo.InvariantCulture));
            var body = ShBody(parent);
            Desk_Tabs(body, tb, "o", "📜 Siparişler", "f", "❤️ Kasabalılar");
            if (tb == "o") OrdersBody(body); else FriendsBody(body);
        }

        public static void SheetMail(Transform parent)
        {
            Head(parent, "✉️", "Posta", "Tedarik sözleşmeleri");
            var body = ShBody(parent);
            ContractsBody(body);
        }

        public static void ToDesk()
        {
            DeskWinT = 0;
            Fade(() =>
            {
                if (Mode == "hub") HubSaved = new Cam_Saved { t = Ctl.target, p = Ctl.position };
                Mode = "desk"; Focus = null; DropInterior(); View = null; Ctl.enabled = false; Ictl.enabled = false; SetModeUI(); RenderSheet(); SyncMarks();
            });
        }

        public static void EnterWorld(Action after = null)
        {
            Fade(() =>
            {
                Mode = "hub"; DropInterior(); Ctl.enabled = true; Ictl.enabled = false; HubLimits();
                var h = HubSaved ?? new Cam_Saved { t = HUB_T, p = HUB_T + HUB_DIR * HUB_D };
                Ctl.target = h.t; Ctl.position = h.p; View = null; SetModeUI(); RenderSheet(); SyncMarks();
                if (after != null) after();
            });
        }

        public static void WorldTick(float dt, float t)
        {
            if (WaterMat != null) WaterMat.mainTextureOffset -= new Vector2(0, dt * .08f);
            for (int i = 0; i < WorldFx.Count; i++) WorldFx[i](dt, t);
            Ambience(dt, t); StepWeather(dt, t);
            for (int i = 0; i < RegFx.Count; i++) RegFx[i](dt, t);
            foreach (var kv in LocGroups)
            {
                var g = kv.Value; var u = g.GetComponent<UData>(); if (u == null) continue;
                if (u.pop.HasValue)
                {
                    u.pop = Math.Min(1, u.pop.Value + dt * 2.2); float p = (float)u.pop.Value, s = 1 + Mathf.Sin(p * Mathf.PI) * .1f;
                    g.localScale = new Vector3(s, .3f + p * .7f * s, s);
                    if (p >= 1) { g.localScale = Vector3.one; u.pop = null; }
                }
                var fx = u.allFx; if (fx != null) for (int i = 0; i < fx.Count; i++) fx[i](dt, t);
            }
            StepPuffs(dt);
        }
    }
}
