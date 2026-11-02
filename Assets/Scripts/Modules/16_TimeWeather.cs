// ================= ZAMAN, MEVSİM, HAVA (16-time-weather.js) =================
// Gökyüzü/ışık/sis/yıldız/kar burada sürülür. Unity karşılıkları:
//  - sun (DirectionalLight) → Game.Sun (07-world'ün Light'ı); hedefi orijin, yönü konumdan orijine.
//  - hemiL (HemisphereLight: gök 0xf3ead2, yer 0x7d6a3a) → RenderSettings ortam ışığı (Trilight).
//  - world.fog.color → RenderSettings.fogColor
//  - body arka plan degradesi (saydam tuvalin arkasındaki gök) → Shader küreselleri _SkyTop/_SkyLow + kamera arka plan rengi.
//  - THREE.Points (yıldız, kar) → kameraya dönük küçük kareler (sabit piksel boyutu: sizeAttenuation:false).
//  - .rainfx (ekran üstünde kayan yağmur çizgileri) → arayüzün en altında iki kat döşenmiş RawImage.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class SeasonDef { public string n, e; }
    public class WeatherDef { public string n, e; }
    public class SeasColDef
    {
        public List<int> oak, pine, bush; public int grass; public int em;
        public List<int> Of(string g) => g == "oak" ? oak : g == "pine" ? pine : g == "bush" ? bush : null;
    }

    // THREE.Points karşılığı: her nokta kameraya dönük bir kare, boyutu ekran pikseli cinsinden sabit
    public class TwPoints
    {
        public GameObject go; public Mesh mesh; public Material mat; public Vector3[] pos; public float sizePx; public int drawN;
        Vector3[] verts;
        public TwPoints(string name, Transform parent, Vector3[] localPos, float sizePx, Color color)
        {
            go = new GameObject(name); if (parent != null) go.transform.SetParent(parent, false);
            pos = localPos; this.sizePx = sizePx; drawN = localPos.Length;
            int n = localPos.Length; verts = new Vector3[n * 4]; var tri = new int[n * 6]; var uv = new Vector2[n * 4];
            for (int i = 0; i < n; i++)
            {
                tri[i * 6] = i * 4; tri[i * 6 + 1] = i * 4 + 1; tri[i * 6 + 2] = i * 4 + 2; tri[i * 6 + 3] = i * 4; tri[i * 6 + 4] = i * 4 + 2; tri[i * 6 + 5] = i * 4 + 3;
                uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(0, 1); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(1, 0);
            }
            mesh = new Mesh(); mesh.MarkDynamic(); mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tri;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000);   // frustumCulled=false
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var sh = Shader.Find("Sprites/Default");     // saydam, ışıksız, sissiz (fog:false, depthWrite:false)
            mat = new Material(sh); mat.color = color; mat.renderQueue = 3000;
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }
        public bool visible { get => go.activeSelf; set { if (go.activeSelf != value) go.SetActive(value); } }
        public float opacity { get => mat.color.a; set { var c = mat.color; c.a = value; mat.color = c; } }
        public void SetColor(Color c) { c.a = mat.color.a; mat.color = c; }
        // Kameraya dönük kareleri yeniden kur
        public void Refresh()
        {
            var cam = Camera.main; if (cam == null || !go.activeInHierarchy) return;
            var tr = go.transform; Vector3 camPos = cam.transform.position;
            Vector3 right = tr.InverseTransformDirection(cam.transform.right), up = tr.InverseTransformDirection(cam.transform.up);
            float k = 2 * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad) / Mathf.Max(1, cam.pixelHeight) * sizePx * .5f;
            float sc = tr.lossyScale.x != 0 ? 1 / tr.lossyScale.x : 1;
            for (int i = 0; i < pos.Length; i++)
            {
                int b = i * 4;
                if (i >= drawN) { verts[b] = verts[b + 1] = verts[b + 2] = verts[b + 3] = Vector3.zero; continue; }
                var p = pos[i]; float h = Vector3.Distance(tr.TransformPoint(p), camPos) * k * sc;
                Vector3 r = right * h, u = up * h;
                verts[b] = p - r - u; verts[b + 1] = p - r + u; verts[b + 2] = p + r + u; verts[b + 3] = p + r - u;
            }
            mesh.vertices = verts;
        }
    }

    public static partial class Game
    {
        public const double DAY_MS = 12 * 60 * 1000;
        public static readonly List<SeasonDef> SEASONS = new List<SeasonDef> { new SeasonDef { n = "İlkbahar", e = "🌸" }, new SeasonDef { n = "Yaz", e = "☀️" }, new SeasonDef { n = "Sonbahar", e = "🍂" }, new SeasonDef { n = "Kış", e = "❄️" } };
        public static readonly Dictionary<string, WeatherDef> WEATHER = new Dictionary<string, WeatherDef>
        {
            { "sun", new WeatherDef { n = "Güneşli", e = "☀️" } }, { "rain", new WeatherDef { n = "Yağmurlu", e = "🌧️" } },
            { "storm", new WeatherDef { n = "Fırtına", e = "⛈️" } }, { "snow", new WeatherDef { n = "Karlı", e = "🌨️" } }
        };
        public static double GameDay() => Math.Floor(Now() / DAY_MS);
        public static double DayPhase() => (Now() % DAY_MS) / DAY_MS;
        public static int Season() => (int)(Math.Floor(GameDay() / 5) % 4);
        public static string ClockStr() { int m = (int)Math.Floor(DayPhase() * 1440); return (m / 60).ToString("00") + ":" + (m % 60).ToString("00"); }
        public static double DayFactor() { double a = (DayPhase() - .25) * Math.PI * 2; return Math.Max(0, Math.Min(1, Math.Sin(a) * 1.7 + .45)); }

        static readonly List<Dictionary<string, int>> Tw_WXTBL = new List<Dictionary<string, int>>
        {
            new Dictionary<string, int> { { "sun", 5 }, { "rain", 4 }, { "storm", 1 } }, new Dictionary<string, int> { { "sun", 8 }, { "rain", 1 }, { "storm", 1 } },
            new Dictionary<string, int> { { "sun", 4 }, { "rain", 4 }, { "storm", 2 } }, new Dictionary<string, int> { { "sun", 3 }, { "snow", 6 }, { "storm", 1 } }
        };
        public static void UpdateWeather()
        {
            var w = S.weather; if (Now() < w.until) return; var tbl = Tw_WXTBL[Season()];
            double r = Rnd() * tbl.Values.Sum(); string k = "sun"; foreach (var kv in tbl) { r -= kv.Value; if (r <= 0) { k = kv.Key; break; } }
            var prev = w.k; w.k = k; w.until = Now() + (120 + Rnd() * 150) * 1000;
            if (k != prev && Booted)
            {
                if (k == "rain") Toast("🌧️ Yağmur başladı: ekinler daha hızlı büyüyor");
                if (k == "storm") { Toast("⛈️ Fırtına! Tavuklar kümese kaçtı"); Sfx("thunder"); }
                if (k == "snow") Toast("🌨️ Kar yağıyor");
            }
        }

        public static readonly Dictionary<string, List<int>> FOL = new Dictionary<string, List<int>>
        {
            { "oak", new List<int> { 0x5c9a3e, 0x4f8a36, 0x68a846 } }, { "pine", new List<int> { 0x2f6b3a, 0x3a7d42, 0x2a5f33 } }, { "bush", new List<int> { 0x4f8f37, 0x5c8a3a } }
        };
        public static readonly List<SeasColDef> SEAS_COL = new List<SeasColDef>
        {
            new SeasColDef { oak = new List<int> { 0x8fcf6a, 0xf2b8c6, 0x7cc04a }, grass = 0xffffff, em = 0 },
            new SeasColDef { grass = 0xf6f0cc, em = 0 },
            new SeasColDef { oak = new List<int> { 0xd9822b, 0xc4562a, 0xe8b13a }, bush = new List<int> { 0xb98a3a, 0xa8722e }, grass = 0xe6d29a, em = 0 },
            new SeasColDef { oak = new List<int> { 0xe9eef2, 0xdfe7ea, 0xf4f7f8 }, pine = new List<int> { 0x6f9a8a, 0x86ab9c, 0x5e8a7a }, bush = new List<int> { 0xdfe7ea, 0xd0dbe0 }, grass = 0xd8e2e8, em = 0x6d7d8a },
        };
        public static int SeasonNow = -1;

        // Toon malzemesinin yayınım özellikleri (06-engine gölgelendiricisi)
        const string TW_EMI = "_EmissionColor", TW_EMI_I = "_EmissionIntensity";

        public static void ApplySeason()
        {
            int se = Season(); if (se == SeasonNow) return; bool first = SeasonNow < 0; SeasonNow = se;
            if (S.lastCrop != null && CropOf(S.lastCrop) != null && !CropSeasonOk(CropOf(S.lastCrop))) S.lastCrop = "bugday"; var sc = SEAS_COL[se];
            foreach (var g in new[] { "oak", "pine", "bush" })
            {
                var lst = FOL[g]; var sg = sc.Of(g);
                for (int i = 0; i < lst.Count; i++) { int c = lst[i]; var m = GetOr(MC, c + "|"); if (m != null) m.color = Grade(sg != null ? sg[i % sg.Count] : c); }
            }
            if (GROUND != null)
            {
                var rd = GROUND.GetComponent<Renderer>();
                if (rd != null && rd.sharedMaterial != null) { rd.sharedMaterial.color = Grade(sc.grass); rd.sharedMaterial.SetColor(TW_EMI, Hex3(sc.em)); }
            }
            if (!first) { Toast(SEASONS[se].e + " " + SEASONS[se].n + " geldi!"); Sfx("chime"); }
        }

        public static readonly Dictionary<string, int[]> SKY = new Dictionary<string, int[]>
        {
            { "day", new[] { 0xa3c4c0, 0xefe0b6 } }, { "dusk", new[] { 0xd98a5a, 0xf3c58a } }, { "night", new[] { 0x2e3d66, 0x6a74a0 } }
        };
        static double Tw_ambT = -1, Tw_flash = 0; static int Tw_glowN = 0;
        static List<Material> GLOWM = new List<Material>();
        static readonly Dictionary<Material, float> Tw_glowBase = new Dictionary<Material, float>();   // m.userData.base
        static void GlowMats()
        {
            int n = MC.Count; if (n == Tw_glowN) return; Tw_glowN = n;
            GLOWM = MC.Values.Where(m => m != null && m.HasProperty(TW_EMI) && m.HasProperty(TW_EMI_I) && m.GetFloat(TW_EMI_I) > 0 && Tw_Sum(m.GetColor(TW_EMI)) > .1f).ToList();
            foreach (var m in GLOWM) if (!Tw_glowBase.ContainsKey(m)) Tw_glowBase[m] = m.GetFloat(TW_EMI_I);
        }
        static float Tw_Sum(Color c) => c.r + c.g + c.b;

        // Yıldızlar ve kar parçacıkları
        static TwPoints starPts, wxPts;
        const int WXN = 900;
        static Vector3[] wxArr;   // Three koordinatı (x,y,z) — Unity yerel konumu V() ile
        static void Init_16()
        {
            var a = new Vector3[500];
            for (int i = 0; i < 500; i++)
            {
                double th = Rnd() * Math.PI * 2, ph = Rnd() * 1.2;
                a[i] = V(Math.Cos(th) * Math.Sin(ph) * 220, Math.Cos(ph) * 220 + 20, Math.Sin(th) * Math.Sin(ph) * 220);
            }
            starPts = new TwPoints("stars", SCENE, a, 2, Tw_Hex(0xfff8e0, 0)); starPts.visible = false;
            wxArr = new Vector3[WXN]; var wp = new Vector3[WXN];
            for (int i = 0; i < WXN; i++) { wxArr[i] = new Vector3((float)((Rnd() - .5) * 70), (float)(Rnd() * 30), (float)((Rnd() - .5) * 70)); wp[i] = V(wxArr[i].x, wxArr[i].y, wxArr[i].z); }
            wxPts = new TwPoints("weather", SCENE, wp, 3, Tw_Hex(0xcfe3f5, .8f)); wxPts.visible = false;
            Tw_BuildRain();
        }
        static Color Tw_Hex(int hex, float a) { var c = Hex3(hex); c.a = a; return c; }

        // Yağmur: 3D parçacık yerine ekranın üstünde kayan çizgiler → oyun kare hızından bağımsız, takılmaz.
        // 3D parçacık yalnızca kar için (yavaş düştüğü için akıcı görünür).
        static Texture2D _rainTex;
        public static Texture2D RAIN_SVG
        {
            get
            {
                if (_rainTex != null) return _rainTex;
                var q = Rng(42); var p = new Paint(180, 180);
                var col = new Color(0xe4 / 255f, 0xf0 / 255f, 0xfb / 255f, .85f);
                for (int i = 0; i < 34; i++)
                {
                    float x = (float)(q() * 180), y = (float)(q() * 180), h = (float)(9 + q() * 9);
                    x = (float)(Round(x * 10) / 10); y = (float)(Round(y * 10) / 10);
                    p.Line(x, y, x - 1.5f, y + h, 1.3f, col);
                    p.Circle(x, y, .65f, col); p.Circle(x - 1.5f, y + h, .65f, col);   // stroke-linecap:round
                }
                _rainTex = p.Apply(); _rainTex.wrapMode = TextureWrapMode.Repeat;
                return _rainTex;
            }
        }
        static RectTransform Tw_rainfx; static RawImage[] Tw_rainI; static CanvasGroup Tw_rainCg; static double Tw_rainT;
        static void Tw_BuildRain()
        {
            if (UI.Root == null) return;
            // .rainfx{position:fixed;left:-15%;right:-15%;top:-200px;bottom:-20px;z-index:1;opacity:.5;overflow:hidden}
            var rt = UI.New("rainfx", UI.Root); rt.SetAsFirstSibling();
            rt.anchorMin = new Vector2(-.15f, 0); rt.anchorMax = new Vector2(1.15f, 1); rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = new Vector2(0, -20); rt.offsetMax = new Vector2(0, 200);
            Tw_rainCg = rt.gameObject.AddComponent<CanvasGroup>(); Tw_rainCg.alpha = .5f; Tw_rainCg.blocksRaycasts = false; Tw_rainCg.interactable = false;
            Tw_rainI = new RawImage[2];
            for (int i = 0; i < 2; i++)
            {
                var ri = UI.New("i", rt).gameObject.AddComponent<RawImage>(); ri.texture = RAIN_SVG; ri.raycastTarget = false;
                UI.Stretch(ri.rectTransform, i == 0 ? 0 : 37, 0, 0, 0);   // .rainfx i+i{inset:0 0 0 37px}
                if (i == 1) ri.color = new Color(1, 1, 1, .55f);
                Tw_rainI[i] = ri;
            }
            Tw_rainfx = rt; rt.gameObject.SetActive(false);
            OnFrame += Tw_RainFrame;
        }
        static void Tw_RainFrame(float dt)
        {
            if (Tw_rainfx == null || !Tw_rainfx.gameObject.activeSelf) return;
            Tw_rainT += Time.unscaledDeltaTime; bool storm = Tw_wxCls == "storm";
            for (int i = 0; i < 2; i++)
            {
                var ri = Tw_rainI[i]; var r = ri.rectTransform.rect;
                double dur = i == 0 ? (storm ? .36 : .5) : (storm ? .55 : .8);   // @keyframes rainA: translateY 0 → 180px
                float off = (float)((Tw_rainT / dur) % 1.0);
                ri.uvRect = new Rect(0, off, Mathf.Max(1, r.width) / 180f, Mathf.Max(1, r.height) / 180f);
            }
        }

        static string Tw_wxCls = "";
        public static void WxUI()
        {
            var k = S.weather.k; bool show = (Mode == "hub" || Mode == "focus") && !BodyHas("photo"); string c = show && (k == "rain" || k == "storm") ? k : "";
            if (c != Tw_wxCls)
            {
                Tw_wxCls = c;
                if (Tw_rainfx != null)
                {
                    Tw_rainfx.gameObject.SetActive(c != "");
                    // body.wx-storm .rainfx{opacity:.65;transform:rotate(12deg)}
                    Tw_rainCg.alpha = c == "storm" ? .65f : .5f;
                    Tw_rainfx.localRotation = c == "storm" ? Quaternion.Euler(0, 0, -12) : Quaternion.identity;
                }
            }
        }

        public static void StepWeather(double dt, double t)
        {
            var k = S.weather.k; WxUI();
            bool on = k == "snow"; wxPts.visible = on; if (!on) return; bool snow = k == "snow"; wxPts.sizePx = snow ? 4.5f : 2.4f; wxPts.SetColor(Hex3(snow ? 0xffffff : 0xbcd6f2));
            var ct = Ctl.target; SetPos(wxPts.go.transform, ct.x, 0, ct.z); double sp = snow ? 3 : k == "storm" ? 34 : 26;
            int N = GFX.low ? WXN / 3 : WXN * 2 / 3; wxPts.drawN = N;
            for (int i = 0; i < N; i++)
            {
                var a = wxArr[i]; double y = a.y - sp * dt; if (snow) a.x += (float)(Math.Sin(t + i) * dt * .6); if (k == "storm") a.x += (float)(dt * 8);
                if (y < 0) { y = 30; a.x = (float)((Rnd() - .5) * 70); a.z = (float)((Rnd() - .5) * 70); }
                a.y = (float)y; wxArr[i] = a; wxPts.pos[i] = V(a.x, a.y, a.z);
            }
            wxPts.Refresh();
        }

        static readonly Color Tw_hemiGround = new Color(0x7d / 255f, 0x6a / 255f, 0x3a / 255f);
        public static void Ambience(double dt, double t)
        {
            double ph = DayPhase(), a = (ph - .25) * Math.PI * 2, d = DayFactor(); var wx = S.weather.k; double cloudy = wx == "rain" || wx == "storm" ? 1 : wx == "snow" ? .5 : 0, sa = Math.Sin(a);
            double dusk = Math.Max(0, 1 - Math.Abs(sa) * 3) * (sa > -.35 ? 1 : 0);
            if (Sun != null)
            {
                double sx = Math.Cos(a) * -55, sy = Math.Max(14, Math.Abs(sa) * 50 + 8), sz = 26;
                SetPos(Sun.transform, sx, sy, sz); Sun.transform.rotation = Quaternion.LookRotation(-V(sx, sy, sz));   // hedef: orijin
                Sun.intensity = (float)((.55 + .7 * d) * (1 - cloudy * .35));
                var sc = Hex3(0xffe6c0); sc = Color.Lerp(sc, Hex3(0xff9a55), (float)(dusk * .8)); sc = Color.Lerp(sc, Hex3(0xb8c8f0), (float)(Math.Max(0, .8 - d) * 1.1)); Sun.color = sc;
            }
            float hemiI = (float)((.62 + .18 * d) * (1 - cloudy * .15) + Tw_flash);
            var hc = Color.Lerp(Hex3(0xf3ead2), Hex3(0xb4c0e8), (float)((1 - d) * .85));
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Eng_ApplyHemi(hc, Tw_hemiGround, hemiI);   // doğrusal × yoğunluk (06-engine ile aynı dönüşüm)
            if (t - Tw_ambT > .25 || Tw_ambT < 0)
            {
                Tw_ambT = t;
                var top = Hex3(SKY["day"][0]); top = Color.Lerp(top, Hex3(SKY["night"][0]), (float)(1 - d)); top = Color.Lerp(top, Hex3(SKY["dusk"][0]), (float)(dusk * .6)); top = Color.Lerp(top, Hex3(0x8e959a), (float)(cloudy * .5));
                var low = Hex3(SKY["day"][1]); low = Color.Lerp(low, Hex3(SKY["night"][1]), (float)(1 - d)); low = Color.Lerp(low, Hex3(SKY["dusk"][1]), (float)(dusk * .7)); low = Color.Lerp(low, Hex3(0xb8b9b0), (float)(cloudy * .5));
                // document.body.style.background = linear-gradient(top → low)
                Shader.SetGlobalColor("_SkyTop", top); Shader.SetGlobalColor("_SkyLow", low); Eng_ApplySky(top, low);
                var cam = Camera.main; if (cam != null) cam.backgroundColor = low;
                RenderSettings.fogColor = low;
                SkyTop = top; GlowMats(); float glow = (float)(.4 + (1 - d) * 1.5);
                foreach (var m in GLOWM) { float b; if (m != null && Tw_glowBase.TryGetValue(m, out b)) m.SetFloat(TW_EMI_I, b * glow); }
                if (starPts != null) { float op = (float)(Math.Max(0, (1 - d) * 1.2 - cloudy) * .9); starPts.opacity = op; starPts.visible = op > 0; }
            }
            if (starPts != null && starPts.visible) starPts.Refresh();
            Tw_flash = Math.Max(0, Tw_flash - dt * 3); if (wx == "storm" && Rnd() < dt * .07) { Tw_flash = 1.3; Sfx("thunder"); }
        }
    }
}
