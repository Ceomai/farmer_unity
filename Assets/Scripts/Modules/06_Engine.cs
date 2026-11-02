// ================= 3D MOTOR (06-engine.js) =================
// Three.js r147 → Unity Built-in RP. Bu dosya: işleyici ayarları (R), toon malzeme sistemi (grade/toonMat/M/OUT/outline),
// temel geometriler (GE), ilkel yardımcılar P/F/G, rng, canvasTex/TEX, tabelalar, ribbon, toz bulutları, ortam (ışık/sis/gökyüzü/kamera)
// ve Three-koordinatlı dönüş/konum yardımcıları. Modeller (ev, hayvan, ekin, binalar, insanlar…) Engine/EngModels.cs'de.
// Koordinat kuralı: tüm sayılar JS ile aynı (Three koordinatı); SetPos/SetRot/V çevirir (z ters). Dönüşler radyan, Euler XYZ.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ciftlik
{
    // toonMat / M seçenekleri (JS KEEP: emissive, emissiveIntensity, transparent, opacity, depthWrite, map, side)
    public class MatOpts
    {
        public const int FrontSide = 0, BackSide = 1, DoubleSide = 2;
        public int? color, emissive;
        public float? emissiveIntensity, opacity;
        public bool? transparent, depthWrite;
        public Texture map;
        public int? side;
        // JSON.stringify(o) benzeri anahtar (MC önbelleği için)
        public string Key()
        {
            var sb = new StringBuilder("{");
            void Add(string k, string v) { if (sb.Length > 1) sb.Append(','); sb.Append('"').Append(k).Append("\":").Append(v); }
            if (emissive != null) Add("emissive", emissive.Value.ToString(CultureInfo.InvariantCulture));
            if (emissiveIntensity != null) Add("emissiveIntensity", emissiveIntensity.Value.ToString("R", CultureInfo.InvariantCulture));
            if (transparent != null) Add("transparent", transparent.Value ? "true" : "false");
            if (opacity != null) Add("opacity", opacity.Value.ToString("R", CultureInfo.InvariantCulture));
            if (depthWrite != null) Add("depthWrite", depthWrite.Value ? "true" : "false");
            if (side != null) Add("side", side.Value.ToString(CultureInfo.InvariantCulture));
            if (map != null) Add("map", "\"" + map.GetInstanceID() + "\"");
            sb.Append('}');
            return sb.ToString();
        }
        public MatOpts With(int c) => new MatOpts { color = c, emissive = emissive, emissiveIntensity = emissiveIntensity, opacity = opacity, transparent = transparent, depthWrite = depthWrite, map = map, side = side };
    }

    // Eşanlamlı ad (bazı modüller tekil adla yazıyor): new MatOpt{…} her yerde MatOpts yerine geçer
    public class MatOpt : MatOpts { }

    // person(o) seçenekleri
    public class PersonOpt
    {
        public int? skin, shirt, pants, dress, apron, hatc;
        public string hat;          // 'fez' | 'cap' | 'scarf' | 'straw'
        public bool mus;
    }

    // signBoard satırı: {t, s, c}
    public class SignLine { public string t; public float s = 64; public string c; public SignLine() { } public SignLine(string t, float s = 64, string c = null) { this.t = t; this.s = s; this.c = c; } }
    public class SignBoardInfo { public Texture2D tex; public float aspect; public int cw, ch; public float W; public List<SignLine> lines; public List<float> ys; }

    // JS R (THREE.WebGLRenderer) karşılığı. Unity kameraları kendileri çizer; burada yalnızca işleyici ayarları tutulur.
    public class Eng_Renderer
    {
        float _exp = 1.08f;
        public float toneMappingExposure { get => _exp; set { _exp = value; Shader.SetGlobalFloat("_CfExposure", value); } }
        public bool shadowMapEnabled => Game.GFX.shadows;
        // Built-in RP gölge haritalarını her karede yeniden üretir; JS'teki shadowMap.autoUpdate/needsUpdate yalnızca uyumluluk için tutulur.
        public bool shadowAutoUpdate = true, shadowNeedsUpdate;
        public double pixelRatio => Game.Eng_PR;
        public void SetPixelRatio(double pr) => Game.SetPixelRatio(pr);
        public int MaxAnisotropy => Game.MAXANI;
        // R.render(scene, cam) / R.setRenderTarget(rt) karşılığı: kamerayı (isteğe bağlı bir hedefe) hemen çizer
        public void Render(Camera cam, RenderTexture target = null)
        {
            if (cam == null) return;
            var old = cam.targetTexture; cam.targetTexture = target; cam.Render(); cam.targetTexture = old;
        }
    }

    public static partial class Game
    {
        // ---------- işleyici ----------
        public static Transform SCENE;                 // JS world (THREE.Scene) kökü
        public static Eng_Renderer R;
        public static int MAXANI = 8;
        public static float TEXQ = 2;
        public static double Eng_PR = 1;
        // JS GRAD: 4 texel [96,150,205,255], en yakın örnekleme
        public static readonly float[] GRAD = { 96 / 255f, 150 / 255f, 205 / 255f, 255 / 255f };

        static double Eng_DPR() => Application.isMobilePlatform && Screen.dpi > 0 ? Math.Max(1, Screen.dpi / 160.0) : 1;

        static void Init_06()
        {
            R = new Eng_Renderer();
            TEXQ = (float)Math.Min(3, Math.Max(2, Eng_DPR() * 1.5));
            var go = new GameObject("SCENE"); UnityEngine.Object.DontDestroyOnLoad(go); SCENE = go.transform;
            // ışık yoğunlukları doğrusal (Three ile aynı): GraphicsSettings.lightsUseLinearIntensity = true
            try
            {
                var gs = Type.GetType("UnityEngine.Rendering.GraphicsSettings, UnityEngine.CoreModule") ?? typeof(GraphicsSettings);
                var pi = gs.GetProperty("lightsUseLinearIntensity", BindingFlags.Static | BindingFlags.Public);
                if (pi != null && pi.CanWrite) pi.SetValue(null, true, null);
            }
            catch (Exception) { }
            Shader.SetGlobalVector("_CfGrad", new Vector4(GRAD[0], GRAD[1], GRAD[2], GRAD[3]));
            R.toneMappingExposure = 1.08f;
            Eng_ApplyHemi(Color.black, Color.black, 0);
            Eng_ApplySky(UI.C.skyTop, UI.C.skyLow);
            QualitySettings.antiAliasing = 4;
            QualitySettings.pixelLightCount = 4;
            Eng_EnsureGE();
            ApplyGfx();
        }

        // GFX ayarlarını uygula (gölge açık/kapalı, gölge haritası boyutu, kare sınırı). 00-boot setGfx de çağırır.
        static readonly List<Light> Eng_Suns = new List<Light>();
        public static void ApplyGfx()
        {
            var g = GFX;
            QualitySettings.shadows = g.shadows ? ShadowQuality.All : ShadowQuality.Disable;
            QualitySettings.shadowResolution = g.shadowSize >= 4096 ? ShadowResolution.VeryHigh : g.shadowSize >= 2048 ? ShadowResolution.High : ShadowResolution.Medium;
            QualitySettings.shadowDistance = 180;
            QualitySettings.shadowCascades = g.low ? 1 : 2;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            Eng_Suns.RemoveAll(l => l == null);
            foreach (var l in Eng_Suns)
            {
                l.shadows = g.shadows && l.shadowStrength > 0 ? LightShadows.Soft : LightShadows.None;
                l.shadowCustomResolution = g.shadowSize;
            }
            Application.targetFrameRate = g.fpsCap > 0 ? g.fpsCap : 60;
            SetPixelRatio(g.pr > 0 ? g.pr : g.maxPR);
        }

        // R.setPixelRatio: mobilde arka tampon çözünürlüğünü (pr / devicePixelRatio) oranında küçültür; masaüstünde tam çözünürlük.
        static int _engNativeW, _engNativeH;
        public static void SetPixelRatio(double pr)
        {
            Eng_PR = pr;
            if (!Application.isMobilePlatform) return;
            if (_engNativeW == 0) { _engNativeW = Math.Max(Screen.width, Screen.currentResolution.width); _engNativeH = Math.Max(Screen.height, Screen.currentResolution.height); }
            if (_engNativeW <= 0 || _engNativeH <= 0) return;
            double k = Clamp(pr / Eng_DPR(), .3, 1);
            int w = RoundI(_engNativeW * k), h = RoundI(_engNativeH * k);
            if (w != Screen.width || h != Screen.height) Screen.SetResolution(w, h, true);
        }

        // ---------- ortam: yarıküre ışığı, sis, gökyüzü, güneş, nokta ışık, kamera ----------
        public static void Eng_ApplyHemi(Color sky, Color ground, float intensity)
        {
            // Toon gölgelendirici unity_AmbientSky/Ground okur (Unity bunları doğrusala çevirir). Three: doğrusal renk × yoğunluk;
            // aynı sonucu almak için (doğrusal × yoğunluk) değeri sRGB'ye geri çevrilip verilir.
            Color s = sky.linear * intensity, g = ground.linear * intensity, e = Color.Lerp(sky.linear, ground.linear, .5f) * intensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = s.gamma; RenderSettings.ambientGroundColor = g.gamma; RenderSettings.ambientEquatorColor = e.gamma;
        }
        public static void Eng_ApplyFog(bool on, Color c, float near, float far)
        {
            RenderSettings.fog = on;
            if (!on) return;
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = c;
            RenderSettings.fogStartDistance = near; RenderSettings.fogEndDistance = far;
        }
        static Material _engSky;
        public static Material Eng_SkyMat
        {
            get
            {
                if (_engSky == null) { var sh = Shader.Find("Ciftlik/Sky"); if (sh != null) _engSky = new Material(sh) { name = "sky" }; }
                return _engSky;
            }
        }
        public static void Eng_ApplySky(Color top, Color low)
        {
            var m = Eng_SkyMat; if (m == null) return;
            m.SetVector("_Top", new Vector4(top.r, top.g, top.b, 1)); m.SetVector("_Low", new Vector4(low.r, low.g, low.b, 1));
            RenderSettings.skybox = m;
        }
        public static EngEnv Eng_Env(Transform root)
        {
            var e = root.GetComponent<EngEnv>();
            if (e == null) e = root.gameObject.AddComponent<EngEnv>();
            return e;
        }
        // new THREE.HemisphereLight(sky, ground, intensity) → kök sahneye eklenir
        public static EngEnv Eng_HemiLight(Transform root, int sky, int ground, float intensity)
        {
            var e = Eng_Env(root); e.hasHemi = true; e.hemiSky = Hex3(sky); e.hemiGround = Hex3(ground); e.hemiI = intensity; EngEnv.ApplyAll(); return e;
        }
        // scene.fog = new THREE.Fog(color, near, far)
        public static EngEnv Eng_SceneFog(Transform root, int color, float near, float far)
        {
            var e = Eng_Env(root); e.hasFog = true; e.fogColor = Hex3(color); e.fogNear = near; e.fogFar = far; EngEnv.ApplyAll(); return e;
        }
        // fog.color.copy(c) (16-time-weather her karede)
        public static void Eng_SetFogColor(Transform root, Color c)
        {
            var e = Eng_Env(root); e.fogColor = c;
            if (e.IsTop(x => x.hasFog)) RenderSettings.fogColor = c;
        }
        // scene.background (düz renk) / CSS gökyüzü gradyanı
        public static void Eng_SceneBackground(Transform root, Color top, Color low)
        {
            var e = Eng_Env(root); e.hasBg = true; e.bgTop = top; e.bgLow = low;
            if (e.IsTop(x => x.hasBg)) Eng_ApplySky(top, low);
        }
        // new THREE.DirectionalLight(color, intensity); light.position.set(x,y,z) (hedef: orijin); castShadow
        public static Light Eng_Sun(Transform par, int color, float intensity, double x, double y, double z, bool castShadow = true, int shadowSize = 0)
        {
            var go = new GameObject("sun"); if (par != null) go.transform.SetParent(par, false);
            go.transform.localPosition = V(x, y, z);
            var dir = V(-x, -y, -z);
            go.transform.localRotation = Quaternion.LookRotation(dir);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional; l.color = Hex3(color); l.intensity = intensity; l.renderMode = LightRenderMode.ForcePixel;
            l.shadowStrength = castShadow ? 1 : 0;
            l.shadows = castShadow && GFX.shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowCustomResolution = shadowSize > 0 ? shadowSize : GFX.shadowSize;
            l.shadowBias = .02f; l.shadowNormalBias = .3f;
            Eng_Suns.Add(l);
            RenderSettings.sun = l;
            return l;
        }
        // new THREE.PointLight(color, intensity, distance) (decay 2)
        public static Light Eng_PointLight(Transform par, int color, float intensity, float distance)
        {
            var go = new GameObject("pointLight"); if (par != null) go.transform.SetParent(par, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = Hex3(color); l.intensity = intensity; l.range = distance > 0 ? distance : 1000;
            l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
            // Three'te her sahne kendi ışığını kullanır: dünya ışığı iç mekân/masa katmanını aydınlatmasın (iç mekân/masa kendi maskesini atar)
            l.cullingMask = ~((1 << W_LAYER_INT) | (1 << DESK_LAYER));
            return l;
        }
        // new THREE.PerspectiveCamera(fov, aspect, near, far) — fov dikey (Three ile aynı)
        public static Camera Eng_Camera(string name, float fov, float near, float far)
        {
            var go = new GameObject(name); UnityEngine.Object.DontDestroyOnLoad(go);
            var c = go.AddComponent<Camera>();
            c.fieldOfView = fov; c.nearClipPlane = near; c.farClipPlane = far;
            c.clearFlags = CameraClearFlags.Skybox; c.allowMSAA = true; c.allowHDR = false;
            return c;
        }

        // ---------- renk / malzeme ----------
        // grade: renk tonunu sıcağa (ya da maviye) %5 kaydırır, doygunluğu %80'e indirir, açıklığı sıkıştırır (sRGB HSL)
        public static Color Grade(int c) => Grade(Hex3(c));
        public static Color Grade(Color col)
        {
            double r = col.r, g = col.g, b = col.b;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double h = 0, s = 0, l = (min + max) / 2;
            if (min != max)
            {
                double d = max - min;
                s = l <= .5 ? d / (max + min) : d / (2 - max - min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;
                h /= 6;
            }
            double hh = h + ((h > .5 ? 1.1 : .1) - h) * .05;
            return HslToColor(hh, s * .8, Math.Min(.93, l * .96 + .025));
        }
        static Color HslToColor(double h, double s, double l)
        {
            h = ((h % 1) + 1) % 1; s = Clamp(s, 0, 1); l = Clamp(l, 0, 1);
            if (s == 0) return new Color((float)l, (float)l, (float)l);
            double p = l <= .5 ? l * (1 + s) : l + s - l * s, q = 2 * l - p;
            double H2(double t) { if (t < 0) t += 1; if (t > 1) t -= 1; if (t < 1 / 6.0) return q + (p - q) * 6 * t; if (t < .5) return p; if (t < 2 / 3.0) return q + (p - q) * 6 * (2 / 3.0 - t); return q; }
            return new Color((float)H2(h + 1 / 3.0), (float)H2(h), (float)H2(h - 1 / 3.0));
        }

        // doku tekrar/kaydırma (THREE.Texture.repeat / offset) — malzemelere uygulanır
        public static readonly Dictionary<Texture, Vector2> TexRep = new Dictionary<Texture, Vector2>();
        static readonly Dictionary<Texture, Vector2> _texOff = new Dictionary<Texture, Vector2>();
        static readonly Dictionary<Texture, List<Material>> _texMats = new Dictionary<Texture, List<Material>>();
        public static Vector2 Eng_TexOffset(Texture t) { Vector2 v; return t != null && _texOff.TryGetValue(t, out v) ? v : Vector2.zero; }
        public static void Eng_TexOffset(Texture t, float x, float y)
        {
            if (t == null) return;
            var v = new Vector2(x, y); _texOff[t] = v;
            List<Material> ms; if (_texMats.TryGetValue(t, out ms)) foreach (var m in ms) if (m != null) m.mainTextureOffset = v;
        }
        public static void Eng_TexRepeat(Texture t, float x, float y)
        {
            if (t == null) return;
            var v = new Vector2(x, y); TexRep[t] = v; t.wrapMode = TextureWrapMode.Repeat;
            List<Material> ms; if (_texMats.TryGetValue(t, out ms)) foreach (var m in ms) if (m != null) m.mainTextureScale = v;
        }
        static void Eng_BindTex(Material m, Texture t)
        {
            m.mainTexture = t;
            Vector2 v;
            if (TexRep.TryGetValue(t, out v)) m.mainTextureScale = v;
            if (_texOff.TryGetValue(t, out v)) m.mainTextureOffset = v;
            List<Material> ms; if (!_texMats.TryGetValue(t, out ms)) _texMats[t] = ms = new List<Material>();
            ms.Add(m);
        }

        // malzeme işleme durumu (Three transparent/opacity/depthWrite/side)
        public static void Eng_MatState(Material m, bool transparent, float opacity, bool depthWrite, int side)
        {
            m.SetFloat("_Opacity", opacity);
            m.SetFloat("_Transparent", transparent ? 1 : 0);
            m.SetFloat("_Cull", side == MatOpts.DoubleSide ? (float)CullMode.Off : side == MatOpts.BackSide ? (float)CullMode.Front : (float)CullMode.Back);
            m.SetFloat("_ZWrite", depthWrite ? 1 : 0);
            if (transparent)
            {
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
            }
            else
            {
                m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetOverrideTag("RenderType", "Opaque"); m.renderQueue = 2000;
            }
        }
        public static void Eng_SetOpacity(Material m, float o) { if (m != null) m.SetFloat("_Opacity", o); }
        public static float Eng_Opacity(Material m) => m != null && m.HasProperty("_Opacity") ? m.GetFloat("_Opacity") : 1;
        public static bool Eng_IsTransparent(Material m) => m != null && m.HasProperty("_Transparent") && m.GetFloat("_Transparent") > .5f;
        // emissive / emissiveIntensity (16-time-weather gece parlaması)
        public static Color Eng_Emissive(Material m) => m != null && m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
        public static void Eng_SetEmissive(Material m, Color c) { if (m != null) m.SetColor("_EmissionColor", c); }
        public static float Eng_EmissiveIntensity(Material m) => m != null && m.HasProperty("_EmissionIntensity") ? m.GetFloat("_EmissionIntensity") : 0;
        public static void Eng_SetEmissiveIntensity(Material m, float v) { if (m != null) m.SetFloat("_EmissionIntensity", v); }

        static Shader _toonSh, _unlitSh;
        public static Shader Eng_ToonShader => _toonSh != null ? _toonSh : (_toonSh = Shader.Find("Ciftlik/Toon"));
        public static Shader Eng_UnlitShader => _unlitSh != null ? _unlitSh : (_unlitSh = Shader.Find("Ciftlik/Unlit"));

        // toonMat(o): MeshToonMaterial (renk ve emissive grade'den geçer)
        public static Material ToonMat(MatOpts o)
        {
            var m = new Material(Eng_ToonShader) { name = "toon" };
            m.SetColor("_Color", o != null && o.color != null ? Grade(o.color.Value) : Color.white);
            if (o != null && o.map != null) Eng_BindTex(m, o.map);
            m.SetColor("_EmissionColor", o != null && o.emissive != null ? Grade(o.emissive.Value) : Color.black);
            m.SetFloat("_EmissionIntensity", o != null && o.emissiveIntensity != null ? o.emissiveIntensity.Value : 1);
            Eng_MatState(m, o != null && o.transparent == true, o != null && o.opacity != null ? o.opacity.Value : 1, o == null || o.depthWrite != false, o != null && o.side != null ? o.side.Value : MatOpts.FrontSide);
            return m;
        }
        public static Material ToonMat(int color) => ToonMat(new MatOpts { color = color });

        // M(c,o): önbellekli toon malzeme (anahtar JS ile aynı biçim: c+'|'+JSON)
        public static readonly Dictionary<string, Material> MC = new Dictionary<string, Material>();
        public static Material M(int c, MatOpts o = null)
        {
            var k = c.ToString(CultureInfo.InvariantCulture) + "|" + (o != null ? o.Key() : "");
            Material m;
            if (MC.TryGetValue(k, out m) && m != null) return m;
            return MC[k] = ToonMat(o != null ? o.With(c) : new MatOpts { color = c });
        }

        // OUT: dış çizgi malzemesi (paylaşılan). Kalınlık: OUT.SetFloat("_Thick", …) (32-thumbnails .0022 yapar)
        static Material _out;
        public static Material OUT
        {
            get
            {
                if (_out == null)
                {
                    _out = new Material(Shader.Find("Ciftlik/Outline")) { name = "outline" };
                    _out.SetFloat("_Thick", .0024f);
                    // JS: renk doğrusal saklanır ve kodlanmadan yazılır → ekranda doğrusal değer görünür; aynısı için bir kez daha doğrusallaştır
                    _out.SetColor("_Col", Hex3(0x2a1a10).linear);
                    _out.SetFloat("_Jit", 0);
                }
                return _out;
            }
        }

        // smoothN: dış çizgi için yumuşatılmış normal (UV3). Geo ağlarında hazır; dışarıdan gelen ağlar için hesaplanır.
        public static void SmoothN(Mesh g)
        {
            if (g == null || Geo.WithONormal.Contains(g)) return;
            var v = g.vertices; var n = g.normals;
            if (v != null && n != null && n.Length == v.Length) g.SetUVs(3, Geo.SmoothNormals(new List<Vector3>(v), new List<Vector3>(n)));
            Geo.WithONormal.Add(g);
        }

        // outline(m): ağın kendisiyle aynı geometriyi OUT ile ters yüzlü çizen çocuk ekler
        public static Transform Outline(Transform m)
        {
            var mf = m.GetComponent<MeshFilter>(); if (mf == null) return m;
            SmoothN(mf.sharedMesh);
            var o = new GameObject("ol");
            o.transform.SetParent(m, false);
            o.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = OUT;
            int sub = mf.sharedMesh != null ? mf.sharedMesh.subMeshCount : 1;
            if (sub > 1) { var ms = new Material[sub]; for (int i = 0; i < sub; i++) ms[i] = OUT; r.sharedMaterials = ms; }   // çok alt örgülü ağ (masa FaceMesh): her alt örgü çizilsin
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return m;
        }

        // new THREE.Mesh(geo, mat) — gölge ayarlarıyla
        public static Transform Eng_Mesh(Transform par, Mesh g, Material mat, bool castShadow = false, bool receiveShadow = false, string name = "mesh")
        {
            var go = new GameObject(name);
            if (par != null) go.transform.SetParent(par, false);
            go.AddComponent<MeshFilter>().sharedMesh = g;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = receiveShadow;
            return go.transform;
        }
        public static void Eng_CastShadow(Transform t, bool on) { var r = t != null ? t.GetComponent<MeshRenderer>() : null; if (r != null) r.shadowCastingMode = on ? ShadowCastingMode.On : ShadowCastingMode.Off; }
        public static Material Eng_Mat(Transform t) { var r = t != null ? t.GetComponent<MeshRenderer>() : null; return r != null ? r.sharedMaterial : null; }

        // ---------- temel geometriler ----------
        static Dictionary<string, Mesh> _ge;
        public static Dictionary<string, Mesh> GE => _ge ?? Eng_EnsureGE();
        static Dictionary<string, Mesh> Eng_EnsureGE()
        {
            if (_ge != null) return _ge;
            const float PI = (float)Math.PI;
            var d = new Dictionary<string, Mesh>
            {
                { "box", Geo.Box(1, 1, 1) }, { "sph", Geo.Sphere(1, 16, 12) }, { "lsph", Geo.Sphere(1, 8, 6) }, { "ico", Geo.Icosahedron(1, 0) }, { "ico1", Geo.Icosahedron(1, 1) },
                { "cyl", Geo.Cylinder(1, 1, 1, 14) }, { "cyl6", Geo.Cylinder(1, 1, 1, 6) }, { "cone", Geo.Cone(1, 1, 10) }, { "cone6", Geo.Cone(1, 1, 6) }, { "pyr", Geo.Cone(1, 1, 4) },
                { "hsph", Geo.Sphere(1, 16, 8, 0, PI * 2, 0, PI / 2) }, { "tor", Geo.Torus(1, .12f, 6, 18) },
            };
            d["tri"] = Geo.Extrude(new[] { new Vector2(-.5f, 0), new Vector2(.5f, 0), new Vector2(0, 1) }, 1, true, "GE.tri");
            d["gam"] = Geo.Extrude(new[] { new Vector2(-.5f, 0), new Vector2(.5f, 0), new Vector2(.4f, .58f), new Vector2(0, 1), new Vector2(-.4f, .58f) }, 1, true, "GE.gam");
            var arch = new List<Vector2> { new Vector2(-.5f, 0), new Vector2(-.5f, .5f) };
            Geo.AbsArc(arch, 0, .5f, .5f, PI, 0, true, 10);
            arch.Add(new Vector2(.5f, 0));
            d["arch"] = Geo.Extrude(arch.ToArray(), 1, true, "GE.arch");
            return _ge = d;
        }

        static void Eng_Apply(Transform m, double[] s, double[] p, double[] r)
        {
            if (s != null) m.localScale = new Vector3((float)s[0], (float)s[1], (float)s[2]);
            if (p != null) SetPos(m, p[0], p[1], p[2]);
            if (r != null) SetRot(m, r[0], r[1], r[2]);
        }

        // P: gölgeli + dış çizgili ilkel. g: GE anahtarı ya da Mesh; c: renk (hex) ya da Material; s/p/r: ölçek/konum/dönüş (Three, [x,y,z])
        public static Transform P(Transform par, string g, int c, double[] s = null, double[] p = null, double[] r = null, MatOpts o = null) => P(par, GE[g], M(c, o), s, p, r);
        public static Transform P(Transform par, string g, Material c, double[] s = null, double[] p = null, double[] r = null) => P(par, GE[g], c, s, p, r);
        public static Transform P(Transform par, Mesh g, int c, double[] s = null, double[] p = null, double[] r = null, MatOpts o = null) => P(par, g, M(c, o), s, p, r);
        public static Transform P(Transform par, Mesh g, Material mt, double[] s = null, double[] p = null, double[] r = null)
        {
            var m = Eng_Mesh(null, g, mt, true, true, "P");
            Eng_Apply(m, s, p, r);
            if (!(Eng_IsTransparent(mt) && Eng_Opacity(mt) < .9f) && !(s != null && s[0] * s[1] * s[2] < .0025)) Outline(m);
            if (par != null) m.SetParent(par, false);
            return m;
        }
        // F: yalnızca gölge alan düz kutu (zemin parçaları)
        public static Transform F(Transform par, int c, double[] s, double[] p, double[] r = null) => F(par, M(c), s, p, r);
        public static Transform F(Transform par, Material c, double[] s, double[] p, double[] r = null)
        {
            var m = Eng_Mesh(par, GE["box"], c, false, true, "F");
            Eng_Apply(m, s, p, r);
            return m;
        }
        // G: grup
        public static Transform G(Transform par, double[] p = null, double ry = 0)
        {
            var go = new GameObject("G"); var g = go.transform;
            if (par != null) g.SetParent(par, false);
            if (p != null) SetPos(g, p[0], p[1], p[2]);
            if (ry != 0) SetRotY(g, ry);
            return g;
        }

        // rng(seed): mulberry32 (JS ile aynı dizi)
        public static Func<double> Rng(double seed)
        {
            uint a = unchecked((uint)(long)seed);
            return () =>
            {
                unchecked
                {
                    a = a + 0x6D2B79F5u;
                    uint t = (a ^ (a >> 15)) * (1u | a);
                    t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                    return (t ^ (t >> 14)) / 4294967296.0;
                }
            };
        }

        // ---------- Three-koordinatlı dönüş/konum/ölçek yardımcıları (canlandırmalar) ----------
        public static Vector3 Eng_Pos3(Transform t) => ToThree(t.localPosition);
        public static void Eng_SetPos3(Transform t, Vector3 p) => t.localPosition = V(p.x, p.y, p.z);
        public static void Eng_Scale(Transform t, double s) => t.localScale = new Vector3((float)s, (float)s, (float)s);
        public static void Eng_Visible(Transform t, bool v) { if (t != null && t.gameObject.activeSelf != v) t.gameObject.SetActive(v); }
        static bool Eng_QEq(Quaternion a, Quaternion b) => Math.Abs(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w) > 0.9999995f;
        // Unity yerel dönüşü → Three Euler XYZ (x,z en küçük olan eşdeğer çözüm seçilir)
        public static Vector3 Eng_EulerFromUnity(Quaternion q)
        {
            var mu = Matrix4x4.Rotate(q);
            float[] sg = { 1, 1, -1 };
            double M_(int r, int c) => sg[r] * sg[c] * mu[r, c];
            double m11 = M_(0, 0), m12 = M_(0, 1), m13 = M_(0, 2), m22 = M_(1, 1), m23 = M_(1, 2), m32 = M_(2, 1), m33 = M_(2, 2);
            double y = Math.Asin(Clamp(m13, -1, 1)), x, z;
            if (Math.Abs(m13) < 0.9999999) { x = Math.Atan2(-m23, m33); z = Math.Atan2(-m12, m11); }
            else { x = Math.Atan2(m32, m22); z = 0; }
            if (Math.Abs(x) > Math.PI / 2 && Math.Abs(z) > Math.PI / 2)
            {
                x -= Math.Sign(x) * Math.PI; z -= Math.Sign(z) * Math.PI; y = Math.PI - y; if (y > Math.PI) y -= 2 * Math.PI;
            }
            return new Vector3((float)x, (float)y, (float)z);
        }
        public static Vector3 Eng_Rot3(Transform t)
        {
            var u = UData.Of(t); var q = t.localRotation;
            if (!u.rot3ok || !Eng_QEq(u.rot3q, q)) { u.rot3 = Eng_EulerFromUnity(q); u.rot3q = q; u.rot3ok = true; }
            return u.rot3;
        }
        public static void Eng_SetRot3(Transform t, double x, double y, double z)
        {
            var u = UData.Of(t);
            t.localRotation = ThreeRot(x, y, z);
            u.rot3 = new Vector3((float)x, (float)y, (float)z); u.rot3q = t.localRotation; u.rot3ok = true;
        }
        public static void Eng_RotX(Transform t, double v) { var r = Eng_Rot3(t); Eng_SetRot3(t, v, r.y, r.z); }
        public static void Eng_RotY(Transform t, double v) { var r = Eng_Rot3(t); Eng_SetRot3(t, r.x, v, r.z); }
        public static void Eng_RotZ(Transform t, double v) { var r = Eng_Rot3(t); Eng_SetRot3(t, r.x, r.y, v); }

        // ---------- canlandırma kayıtları ----------
        public static void AddFx(Transform g, Action<float, float> fn) { var u = UData.Of(g); (u.fx ?? (u.fx = new List<Action<float, float>>())).Add(fn); }
        public static readonly List<Action<float, float>> SwayList = new List<Action<float, float>>();
        // g ve altındaki tüm fx'leri topla (07-world: traverse + userData.fx)
        public static List<Action<float, float>> Eng_CollectFx(Transform g, List<Action<float, float>> into = null)
        {
            into = into ?? new List<Action<float, float>>();
            foreach (var u in g.GetComponentsInChildren<UData>(true)) if (u.fx != null) into.AddRange(u.fx);
            return into;
        }

        // ---------- canvas dokuları ----------
        // canvasTex(w,h,draw,rep,q): mantıksal w×h tuvale çiz, Q kat çözünürlükte doku üret; rep verilirse tekrar eden doku
        public static Texture2D CanvasTex(float w, float h, Action<Ctx2D, float, float> draw, double[] rep = null, float q = 0)
        {
            float Q = q > 0 ? q : TEXQ;
            var p = new Paint(RoundI(w * Q), RoundI(h * Q));
            draw(new Ctx2D(p, Q, w, h), w, h);
            var t = p.Apply();
            t.anisoLevel = MAXANI;
            if (rep != null) Eng_TexRepeat(t, (float)rep[0], (float)rep[1]);
            return t;
        }
        public static Texture2D CanvasTex(float w, float h, Action<Ctx2D, float, float> draw, float[] rep, float q = 0) => CanvasTex(w, h, draw, Eng_D(rep), q);
        static double[] Eng_D(float[] a) => a == null ? null : new double[] { a[0], a[1] };

        public static class TEX
        {
            // float[] tekrar dizisiyle çağrılabilen eşdeğerler
            public static Texture2D planks(string c1, string c2, float[] rep, bool vert = false) => planks(c1, c2, Eng_D(rep), vert);
            public static Texture2D straw(float[] rep) => straw(Eng_D(rep));
            public static Texture2D stone(float[] rep) => stone(Eng_D(rep));
            public static Texture2D grass() => CanvasTex(256, 256, (x, w, h) =>
            {
                x.FillRect(0, 0, w, h, "#78ad45"); var r = Rng(3);
                string[] cs = { "#6ea23e", "#86ba52", "#7bb049", "#91c25c" };
                for (int i = 0; i < 2600; i++) { float a = (float)(r() * w), b = (float)(r() * h), hh = (float)(3 + r() * 3); x.FillRect(a, b, 2, hh, cs[i % 4]); }
            }, new double[] { 70, 70 });
            public static Texture2D planks(string c1, string c2, double[] rep = null, bool vert = false) => CanvasTex(256, 256, (x, w, h) =>
            {
                var r = Rng(9);
                for (int i = 0; i < 8; i++)
                {
                    string col = i % 2 == 1 ? c1 : c2; float k = i * 32;
                    if (vert) x.FillRect(k, 0, 32, h, col); else x.FillRect(0, k, w, 32, col);
                    if (vert) x.FillRect(k, 0, 2, h, "rgba(0,0,0,.28)"); else x.FillRect(0, k, w, 2, "rgba(0,0,0,.28)");
                    for (int j = 0; j < 3; j++)
                    {
                        float o = (float)(r() * w);
                        if (vert) { float xx = (float)(k + 6 + r() * 20); x.FillRect(xx, o, 1.5f, 40, "rgba(0,0,0,.08)"); }
                        else { float yy = (float)(k + 6 + r() * 20); x.FillRect(o, yy, 40, 1.5f, "rgba(0,0,0,.08)"); }
                    }
                    float s = (float)(r() * w);
                    if (vert) x.FillRect(k, s, 32, 2, "rgba(0,0,0,.2)"); else x.FillRect(s, k, 2, 32, "rgba(0,0,0,.2)");
                }
            }, rep);
            public static Texture2D straw(double[] rep = null) => CanvasTex(256, 256, (x, w, h) =>
            {
                x.FillRect(0, 0, w, h, "#d9b45e"); var r = Rng(5);
                string[] cs = { "#c99b3f", "#e8c876", "#b98b35", "#f0d68e" };
                for (int i = 0; i < 1400; i++)
                {
                    float a = (float)(r() * w), b = (float)(r() * h), an = (float)(r() * 6.28), l = (float)(6 + r() * 12);
                    x.Line(a, b, a + (float)Math.Cos(an) * l, b + (float)Math.Sin(an) * l, 1.5f, cs[i % 4]);
                }
            }, rep);
            public static Texture2D stone(double[] rep = null) => CanvasTex(256, 256, (x, w, h) =>
            {
                x.FillRect(0, 0, w, h, "#8f877a"); var r = Rng(11);
                string[] cs = { "#b4ab9b", "#a39a8a", "#c2b9a8", "#9d9483" };
                for (int y = 0; y < h; y += 32)
                    for (float k = (y / 32 % 2) * -24; k < w; k += 48) x.FillRect(k + 2, y + 2, 44, 28, cs[(int)Math.Floor(r() * 4)]);
            }, rep);
            public static Texture2D water() => CanvasTex(256, 128, (x, w, h) =>
            {
                x.VGradient(0, 0, w, h, "#4aa3cf", "#3b8fbe"); var r = Rng(7);
                for (int i = 0; i < 40; i++) { float a = (float)(r() * w), b = (float)(r() * h); x.QuadStroke(a, b, a + 8, b - 3, a + 16, b, 2, "rgba(255,255,255,.55)"); }
            }, new double[] { 12, 1 });
            public static Texture2D kilim(string[] p = null) => CanvasTex(256, 384, (x, w, h) =>
            {
                var C = p ?? new[] { "#a3271f", "#2c3e6b", "#f0c24b", "#f6ead0", "#3f7a4a" };
                x.FillRect(0, 0, w, h, C[0]); x.FillRect(14, 14, w - 28, h - 28, C[1]); x.FillRect(26, 26, w - 52, h - 52, C[0]);
                var cols = new[] { C[2], C[3], C[1], C[4] };
                for (int i = 0; i < 4; i++)
                {
                    float cy = 60 + i * 88;
                    x.FillPoly(new[] { new Vector2(w / 2, cy - 38), new Vector2(w / 2 + 70, cy), new Vector2(w / 2, cy + 38), new Vector2(w / 2 - 70, cy) }, cols[i % 4]);
                    x.FillPoly(new[] { new Vector2(w / 2, cy - 18), new Vector2(w / 2 + 34, cy), new Vector2(w / 2, cy + 18), new Vector2(w / 2 - 34, cy) }, C[0]);
                    x.FillRect(w / 2 - 5, cy - 5, 10, 10, C[3]);
                }
                for (float y = 0; y < h; y += 16) { x.FillRect(4, y, 6, 8, C[3]); x.FillRect(w - 10, y + 8, 6, 8, C[3]); }
            });
        }
        public static Material StdTex(Texture map, MatOpts o = null)
        {
            var oo = o ?? new MatOpts();
            return ToonMat(new MatOpts { color = oo.color, map = map, emissive = oo.emissive, emissiveIntensity = oo.emissiveIntensity, transparent = oo.transparent, opacity = oo.opacity, depthWrite = oo.depthWrite, side = oo.side });
        }
        static Material _stoneMat;
        public static Material StoneMat() => _stoneMat != null ? _stoneMat : (_stoneMat = StdTex(TEX.stone(new double[] { 1, .6 })));

        // signTex(lines): 256×160 kâğıt tabela dokusu (çerçeve). Yazılar dokuya basılamadığından SignTexLetters ile 3D yazı olarak eklenir.
        public static Texture2D SignTex(IList<string> lines) => CanvasTex(256, 160, (x, w, h) =>
        {
            x.FillRect(0, 0, w, h, "#f5e6c4"); x.StrokeRect(5, 5, w - 10, h - 10, 10, "#7a4e2d");
        });
        // SignTex dokulu düzlemin üstüne JS'teki üç satırı (konum/renk/boyut aynı) 3D yazı olarak koyar. plane: 1×1 PlaneGeometry ağı.
        public static void SignTexLetters(Transform plane, IList<string> lines)
        {
            const float cw = 256, ch = 160;
            var sc = plane.localScale; float fix = sc.x != 0 ? sc.y / sc.x : cw / ch;
            float[] ys = { 46, 84, 128 }; float[] sz = { 30, 24, 32 }; string[] cols = { "#b23a22", "#3a2a18", "#2f6b3a" };
            for (int i = 0; i < Math.Min(3, lines.Count); i++)
            {
                var tm = Text3D(plane, lines[i], new Vector3(0, .5f - ys[i] / ch, -.002f), sz[i] / ch, CssColor.Parse(cols[i]), TextAnchor.LowerCenter, true, false);
                tm.transform.localScale = new Vector3(fix, 1, 1);
            }
        }

        // --- çizgi film ahşap tabelalar ---
        static readonly Dictionary<string, SignBoardInfo> SIGNC = new Dictionary<string, SignBoardInfo>();
        public static Mesh PLANE => Geo.Plane(1, 1);
        static float Eng_TextWidth(string t, int size, Font f)
        {
            float w = 0; bool ok = f != null;
            if (ok) { try { f.RequestCharactersInTexture(t, size); } catch (Exception) { ok = false; } }
            foreach (var ch in t)
            {
                CharacterInfo ci;
                if (ok && f.GetCharacterInfo(ch, out ci, size)) w += ci.advance; else w += size * .6f;
            }
            return w;
        }
        public static SignBoardInfo SignBoard(IList<SignLine> lines)
        {
            var key = new StringBuilder("[");
            foreach (var l in lines) key.Append(l.t).Append('|').Append(l.s.ToString(CultureInfo.InvariantCulture)).Append('|').Append(l.c).Append(';');
            SignBoardInfo info;
            if (SIGNC.TryGetValue(key.ToString(), out info)) return info;
            const float pad = 34;
            var L = new List<SignLine>(lines);
            float w = 0; foreach (var l in L) w = Math.Max(w, Eng_TextWidth(l.t, RoundI(l.s), UI.DisplayFont));
            float sum = 0; foreach (var l in L) sum += l.s * 1.1f;
            int cw = (int)Math.Ceiling(w + pad * 2.4), chh = (int)Math.Ceiling(sum + pad * 1.4 + 12);
            float W = cw - 10, h = chh - 12;
            var p = new Paint(cw, chh);
            var x = new Ctx2D(p, 1, cw, chh);
            List<Vector2> RR(float ox, float oy)
            {
                const float r = 22; var pts = new List<Vector2>();
                pts.Add(new Vector2(r + ox, 4 + oy)); pts.Add(new Vector2(W - r + ox, oy));
                pts.AddRange(Ctx2D.Quad(W - r + ox, oy, W + ox, oy, W + ox, r + oy));
                pts.Add(new Vector2(W - 4 + ox, h - r + oy));
                pts.AddRange(Ctx2D.Quad(W - 4 + ox, h - r + oy, W - 4 + ox, h + oy, W - r - 4 + ox, h + oy));
                pts.Add(new Vector2(r + ox, h - 4 + oy));
                pts.AddRange(Ctx2D.Quad(r + ox, h - 4 + oy, 4 + ox, h - 4 + oy, 4 + ox, h - r + oy));
                pts.Add(new Vector2(6 + ox, r + 4 + oy));
                pts.AddRange(Ctx2D.Quad(6 + ox, r + 4 + oy, 6 + ox, 4 + oy, r + ox, 4 + oy));
                return pts;
            }
            x.FillPoly(RR(7, 9), "#24160c");
            var body = RR(0, 0);
            x.FillPolyVGradient(body, 0, h, "#c0803f", "#8f5629");
            // ahşap damarları (şekle kırpılmış)
            for (int i = 1; i < 4; i++)
            {
                float yy = h * i / 4; var cv = Ctx2D.Bezier(0, yy, W * .3f, yy - 7, W * .6f, yy + 7, W, yy, 40);
                for (int k = 1; k < cv.Count; k++) if (Ctx2D.Inside(body, cv[k - 1]) && Ctx2D.Inside(body, cv[k])) x.Line(cv[k - 1].x, cv[k - 1].y, cv[k].x, cv[k].y, 3, "rgba(70,35,10,.35)");
            }
            var closed = new List<Vector2>(body) { body[0] };
            x.Polyline(closed, 7, "#24160c");
            foreach (var nx in new[] { 22f, W - 22 }) x.FillCircle(nx, 21, 5, "#3a2a1a");
            var ys = new List<float>(); float y = pad * .7f;
            foreach (var l in L) { ys.Add(y); y += l.s * 1.1f; }
            var t = p.Apply(); t.anisoLevel = MAXANI;
            info = new SignBoardInfo { tex = t, aspect = (float)cw / chh, cw = cw, ch = chh, W = W, lines = L, ys = ys };
            SIGNC[key.ToString()] = info;
            return info;
        }
        public static Transform SignMesh(IList<string> lines, float H)
        {
            var L = new List<SignLine>(); foreach (var s in lines) L.Add(new SignLine(s)); return SignMesh(L, H);
        }
        public static Transform SignMesh(IList<SignLine> lines, float H)
        {
            var sb = SignBoard(lines);
            var mat = new Material(Eng_UnlitShader) { name = "sign" };
            mat.mainTexture = sb.tex; mat.SetFloat("_AlphaTest", .3f); mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); mat.SetFloat("_ZWrite", 1);
            var m = Eng_Mesh(null, PLANE, mat, false, false, "sign");
            m.localScale = new Vector3(H * sb.aspect, H, 1);
            // yazılar: koyu kontur (8 yönlü kopya) + dolgu; canvas strokeText(lineWidth=s*.22) / fillText karşılığı
            float fix = 1f / sb.aspect;
            for (int i = 0; i < sb.lines.Count; i++)
            {
                var l = sb.lines[i];
                float lx = (sb.W / 2) / sb.cw - .5f, ly = .5f - sb.ys[i] / sb.ch, em = l.s / sb.ch, o = l.s * .11f / sb.ch;
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * (float)Math.PI * 2;
                    var tm = Text3D(m, l.t, new Vector3(lx + (float)Math.Cos(a) * o * fix, ly + (float)Math.Sin(a) * o, -.001f), em, CssColor.Parse("#24160c"), TextAnchor.UpperCenter, false, true);
                    tm.transform.localScale = new Vector3(fix, 1, 1);
                }
                var tf = Text3D(m, l.t, new Vector3(lx, ly, -.003f), em, CssColor.Parse(l.c ?? "#fff1cf"), TextAnchor.UpperCenter, false, true);
                tf.transform.localScale = new Vector3(fix, 1, 1);
            }
            return m;
        }

        // --- kıvrımlı şeritler (dere, patika) ---
        public static Transform Ribbon(Transform par, IList<double[]> pts, double w, Material mat, double y = .04, double segK = 1.5, double vRep = 4)
        {
            var cp = new List<Vector3>(); foreach (var p in pts) cp.Add(new Vector3((float)p[0], 0, (float)p[1]));
            var curve = new CatmullRomCurve3(cp, false, "catmullrom", .5f);
            float len = curve.GetLength();
            int N = Math.Max(8, RoundI(len * segK));
            var pos = new List<Vector3>(); var uv = new List<Vector2>(); var idx = new List<int>();
            for (int i = 0; i <= N; i++)
            {
                float t = (float)i / N; var p = curve.GetPointAt(t); var tg = curve.GetTangentAt(t); float nx = -tg.z, nz = tg.x;
                pos.Add(new Vector3(p.x + nx * (float)w / 2, (float)y, p.z + nz * (float)w / 2));
                pos.Add(new Vector3(p.x - nx * (float)w / 2, (float)y, p.z - nz * (float)w / 2));
                float v = t * len / (float)vRep; uv.Add(new Vector2(0, v)); uv.Add(new Vector2(1, v));
                if (i < N) { int a = i * 2; idx.Add(a); idx.Add(a + 2); idx.Add(a + 1); idx.Add(a + 1); idx.Add(a + 2); idx.Add(a + 3); }
            }
            var g = Geo.FromThree(pos, Geo.VertexNormals(pos, idx), uv, idx, "ribbon");
            var m = Eng_Mesh(par, g, mat, false, true, "ribbon");
            UData.Of(m).curve = curve;
            return m;
        }
        static Material _dirt, _dirtEdge;
        public static Material DirtMat() => _dirt != null ? _dirt : (_dirt = ToonMat(new MatOpts
        {
            side = MatOpts.DoubleSide,
            map = CanvasTex(64, 128, (x, w, h) =>
            {
                x.FillRect(0, 0, w, h, "#d6bb86"); var r = Rng(21); string[] cs = { "#c4a672", "#e2cb9a", "#b8986a" };
                for (int i = 0; i < 90; i++) { float a = (float)(r() * w), b = (float)(r() * h), rr = (float)(1 + r() * 2.2); x.FillCircle(a, b, rr, cs[i % 3]); }
                x.FillRect(0, 0, 5, h, "rgba(120,85,45,.25)"); x.FillRect(w - 5, 0, 5, h, "rgba(120,85,45,.25)");
            }, new double[] { 1, 1 })
        }));
        public static Material DirtEdge() => _dirtEdge != null ? _dirtEdge : (_dirtEdge = ToonMat(new MatOpts { color = 0x8fa456, side = MatOpts.DoubleSide }));

        // --- toz bulutları ---
        public static readonly List<Transform> Puffs = new List<Transform>();
        public static void Puff(Vector3 v, int n = 9, double sz = 1)
        {
            for (int k = 0; k < n; k++)
            {
                var m = Eng_Mesh(SCENE, GE["ico1"], M(0xfdf8ec), false, false, "puff"); Outline(m);
                double a = (double)k / n * 6.28 + Rnd() * .4;
                SetPos(m, v.x, v.y, v.z);
                var u = UData.Of(m);
                u.vel = new Vector3((float)(Math.Cos(a) * 2.4 * sz), (float)(1 + Rnd() * 1.2), (float)(Math.Sin(a) * 2.4 * sz));
                u.life = 0; u.sz = (float)sz;
                Eng_Scale(m, .25 * sz);
                Puffs.Add(m);
            }
        }
        public static void StepPuffs(float dt)
        {
            for (int i = Puffs.Count - 1; i >= 0; i--)
            {
                var m = Puffs[i];
                if (m == null) { Puffs.RemoveAt(i); continue; }
                var u = UData.Get(m); u.life += dt;
                var p = Eng_Pos3(m); float k = dt * Math.Max(0, 1 - u.life * 1.6f);
                Eng_SetPos3(m, new Vector3(p.x + u.vel.x * k, p.y + u.vel.y * k, p.z + u.vel.z * k));
                Eng_Scale(m, (.25 + u.life * 1.1) * u.sz * (u.life > .45 ? Math.Max(0, 1 - (u.life - .45) * 5) : 1));
                if (u.life > .65) { UnityEngine.Object.Destroy(m.gameObject); Puffs.RemoveAt(i); }
            }
        }
    }
}
