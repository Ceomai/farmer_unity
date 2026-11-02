// ================= DÜNYA (07-world.js) =================
// Dünya sahnesi (Game.SCENE), harita kamerası (Cam) ve yörünge denetimi (Ctl), ışıklar, sabit çevre,
// arazi bölgeleri ve yollar, konum modelleri (syncWorld), çiftçi ve köpeği.
// Konum mantığı Three.js koordinatlarında yürür; Unity'ye V(x,y,z) ile çevrilir (z ters).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    // userData alanları (07/08/09/10/11 modüllerinin ilk kez kullandıkları). fx/legs/arms/head/tail/chimney → 06-engine.
    public partial class UData
    {
        public string region, act, open;          // loc, unit, plot, hop → Engine/UData.cs
        public bool hasPlot;                      // JS: userData.plot != null
        public bool plotBuy, farmer;
        public double? pop;                       // konum "zıplama" animasyonu (null = yok)
        public float squash;
        public List<Action<float, float>> allFx;  // konum grubundaki tüm fx (33-loop/25-desk çalıştırır)
    }

    // THREE.OrbitControls (r147) birebir karşılığı. target ve position THREE koordinatındadır.
    public class OrbitControls
    {
        public enum Act { NONE, ROTATE, DOLLY, PAN, DOLLY_ROTATE }
        public Camera camera;
        public bool enabled = true, enableDamping, screenSpacePanning = true, enablePan = true, enableZoom = true, enableRotate = true;
        public float dampingFactor = .05f, rotateSpeed = 1, panSpeed = 1, zoomSpeed = 1;
        public float minDistance = 0, maxDistance = float.PositiveInfinity;
        public float minPolarAngle = 0, maxPolarAngle = Mathf.PI;
        public float minAzimuthAngle = float.NegativeInfinity, maxAzimuthAngle = float.PositiveInfinity;
        public Act mouseLEFT = Act.ROTATE, mouseMIDDLE = Act.DOLLY, mouseRIGHT = Act.PAN;
        public Act touchONE = Act.ROTATE, touchTWO = Act.DOLLY_ROTATE;   // TWO: DOLLY_PAN yerine DOLLY_ROTATE kullanılır
        public Vector3 target;                    // THREE koordinatı
        public event Action change;

        float sdTheta, sdPhi, scale = 1;
        Vector3 panOffset;
        Vector3 lastPosition; Quaternion lastQuaternion = Quaternion.identity;
        const float EPS = 1e-6f;

        public OrbitControls(Camera cam) { camera = cam; }
        // camera.position (THREE koordinatı)
        public Vector3 position
        {
            get => Game.ToThree(camera.transform.position);
            set => camera.transform.position = Game.V(value.x, value.y, value.z);
        }
        public void LookAt(Vector3 t3) { camera.transform.LookAt(Game.V(t3.x, t3.y, t3.z), Vector3.up); }

        public bool Update() => update();
        public bool update()
        {
            var pos = position;
            var offset = pos - target;
            // Spherical.setFromVector3
            float radius = offset.magnitude, theta = 0, phi = 0;
            if (radius > 0) { theta = Mathf.Atan2(offset.x, offset.z); phi = Mathf.Acos(Mathf.Clamp(offset.y / radius, -1, 1)); }
            if (enableDamping) { theta += sdTheta * dampingFactor; phi += sdPhi * dampingFactor; }
            else { theta += sdTheta; phi += sdPhi; }
            float min = minAzimuthAngle, max = maxAzimuthAngle, twoPI = 2 * Mathf.PI;
            if (!float.IsInfinity(min) && !float.IsInfinity(max))
            {
                if (min < -Mathf.PI) min += twoPI; else if (min > Mathf.PI) min -= twoPI;
                if (max < -Mathf.PI) max += twoPI; else if (max > Mathf.PI) max -= twoPI;
                if (min <= max) theta = Mathf.Max(min, Mathf.Min(max, theta));
                else theta = theta > (min + max) / 2 ? Mathf.Max(min, theta) : Mathf.Min(max, theta);
            }
            phi = Mathf.Max(minPolarAngle, Mathf.Min(maxPolarAngle, phi));
            phi = Mathf.Max(EPS, Mathf.Min(Mathf.PI - EPS, phi));   // makeSafe
            radius *= scale;
            radius = Mathf.Max(minDistance, Mathf.Min(maxDistance, radius));
            if (enableDamping) target += panOffset * dampingFactor; else target += panOffset;
            float sp = Mathf.Sin(phi) * radius;
            offset = new Vector3(sp * Mathf.Sin(theta), Mathf.Cos(phi) * radius, sp * Mathf.Cos(theta));
            pos = target + offset;
            position = pos;
            LookAt(target);
            if (enableDamping) { sdTheta *= 1 - dampingFactor; sdPhi *= 1 - dampingFactor; panOffset *= 1 - dampingFactor; }
            else { sdTheta = sdPhi = 0; panOffset = Vector3.zero; }
            scale = 1;
            var q = camera.transform.rotation;
            float dq = lastQuaternion.x * q.x + lastQuaternion.y * q.y + lastQuaternion.z * q.z + lastQuaternion.w * q.w;
            if ((lastPosition - pos).sqrMagnitude > EPS || 8 * (1 - dq) > EPS)
            {
                change?.Invoke();
                lastPosition = position; lastQuaternion = camera.transform.rotation;
                return true;
            }
            return false;
        }

        // ---- yardımcılar (OrbitControls içi) ----
        float ClientH => Mathf.Max(1, UI.H);
        void RotateLeft(float a) { sdTheta -= a; }
        void RotateUp(float a) { sdPhi -= a; }
        void PanLeft(float d)
        {
            var right3 = Game.ToThree(camera.transform.right);   // kamera matrisinin 0. sütunu
            panOffset += right3 * -d;
        }
        void PanUp(float d)
        {
            Vector3 v;
            if (screenSpacePanning) v = Game.ToThree(camera.transform.up);
            else v = Vector3.Cross(new Vector3(0, 1, 0), Game.ToThree(camera.transform.right));
            panOffset += v * d;
        }
        void Pan(float dx, float dy)
        {
            var offset = position - target;
            float td = offset.magnitude * Mathf.Tan(camera.fieldOfView / 2 * Mathf.PI / 180);
            PanLeft(2 * dx * td / ClientH);
            PanUp(2 * dy * td / ClientH);
        }
        float ZoomScale => Mathf.Pow(.95f, zoomSpeed);
        void DollyOut(float s) { scale /= s; }
        void DollyIn(float s) { scale *= s; }

        // ---- işaretçi durumu ----
        Act state = Act.NONE; bool touchMode; int lastTouchCount;
        Vector2 rotateStart, panStart, dollyStart; float dollyDist;
        public bool Busy => state != Act.NONE;

        // Her karede (11-input) çağrılır. canStart: işaretçi arayüz üzerinde değilse true.
        public void Poll(bool canStart)
        {
            if (Input.touchCount > 0 || touchMode)
            {
                int n = Input.touchCount;
                if (n == 0) { touchMode = false; state = Act.NONE; lastTouchCount = 0; return; }
                touchMode = true;
                if (!enabled) { lastTouchCount = n; return; }
                if (n > lastTouchCount)
                {
                    bool began = false; for (int i = 0; i < n; i++) if (Input.GetTouch(i).phase == TouchPhase.Began) began = true;
                    if (began && (canStart || lastTouchCount > 0)) TouchStart(n);
                }
                else if (n < lastTouchCount) state = Act.NONE;
                lastTouchCount = n;
                if (state != Act.NONE) TouchMove(n);
                return;
            }
            var mp = UI.ScreenToCss(Input.mousePosition);
            if (!enabled) { if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)) state = Act.NONE; return; }
            if (state == Act.NONE && canStart)
            {
                Act a = Act.NONE;
                if (Input.GetMouseButtonDown(0)) a = mouseLEFT; else if (Input.GetMouseButtonDown(2)) a = mouseMIDDLE; else if (Input.GetMouseButtonDown(1)) a = mouseRIGHT;
                if (a == Act.DOLLY && enableZoom) { dollyStart = mp; state = a; }
                else if (a == Act.ROTATE && enableRotate) { rotateStart = mp; state = a; }
                else if (a == Act.PAN && enablePan) { panStart = mp; state = a; }
                var wd = Input.mouseScrollDelta.y;
                if (state == Act.NONE && enableZoom && wd != 0) { if (wd > 0) DollyIn(ZoomScale); else DollyOut(ZoomScale); update(); }
            }
            if (state != Act.NONE)
            {
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)) { state = Act.NONE; return; }
                if (state == Act.ROTATE)
                {
                    var d = (mp - rotateStart) * rotateSpeed;
                    if (d.x != 0 || d.y != 0) { RotateLeft(2 * Mathf.PI * d.x / ClientH); RotateUp(2 * Mathf.PI * d.y / ClientH); rotateStart = mp; update(); }
                }
                else if (state == Act.DOLLY)
                {
                    var d = mp - dollyStart;
                    if (d.y > 0) DollyOut(ZoomScale); else if (d.y < 0) DollyIn(ZoomScale);
                    if (d.y != 0) { dollyStart = mp; update(); }
                }
                else if (state == Act.PAN)
                {
                    var d = (mp - panStart) * panSpeed;
                    if (d.x != 0 || d.y != 0) { Pan(d.x, d.y); panStart = mp; update(); }
                }
            }
        }
        static Vector2 TouchCss(int i) => UI.ScreenToCss(Input.GetTouch(i).position);
        static Vector2 Mid(int n) => n == 1 ? TouchCss(0) : (TouchCss(0) + TouchCss(1)) * .5f;
        void TouchStart(int n)
        {
            state = Act.NONE;
            if (n == 1)
            {
                if (touchONE == Act.ROTATE && enableRotate) { rotateStart = TouchCss(0); state = Act.ROTATE; }
                else if (touchONE == Act.PAN && enablePan) { panStart = TouchCss(0); state = Act.PAN; }
            }
            else if (n == 2)
            {
                if (touchTWO == Act.DOLLY_ROTATE && (enableZoom || enableRotate))
                {
                    if (enableZoom) dollyDist = Vector2.Distance(TouchCss(0), TouchCss(1));
                    if (enableRotate) rotateStart = Mid(2);
                    state = Act.DOLLY_ROTATE;
                }
            }
        }
        void TouchMove(int n)
        {
            if (state == Act.ROTATE)
            {
                var p = Mid(Mathf.Min(n, 2)); var d = (p - rotateStart) * rotateSpeed;
                RotateLeft(2 * Mathf.PI * d.x / ClientH); RotateUp(2 * Mathf.PI * d.y / ClientH); rotateStart = p; update();
            }
            else if (state == Act.PAN)
            {
                var p = Mid(Mathf.Min(n, 2)); var d = (p - panStart) * panSpeed;
                Pan(d.x, d.y); panStart = p; update();
            }
            else if (state == Act.DOLLY_ROTATE && n >= 2)
            {
                if (enableZoom)
                {
                    float dist = Vector2.Distance(TouchCss(0), TouchCss(1));
                    if (dollyDist > 0 && dist > 0) DollyOut(Mathf.Pow(dist / dollyDist, zoomSpeed));
                    dollyDist = dist;
                }
                if (enableRotate)
                {
                    var p = Mid(2); var d = (p - rotateStart) * rotateSpeed;
                    RotateLeft(2 * Mathf.PI * d.x / ClientH); RotateUp(2 * Mathf.PI * d.y / ClientH); rotateStart = p;
                }
                update();
            }
        }
    }

    // THREE.CatmullRomCurve3 ('catmullrom', gerilim .5, açık) — xz düzleminde; getPointAt/getTangentAt yay uzunluğuyla.
    public class World_Curve
    {
        readonly Vector2[] pts; readonly float tension; readonly float[] lengths;
        const int DIV = 200;
        public World_Curve(double[][] p, float tension = .5f)
        {
            pts = p.Select(a => new Vector2((float)a[0], (float)a[1])).ToArray(); this.tension = tension;
            lengths = new float[DIV + 1]; var last = GetPoint(0); float sum = 0;
            for (int i = 1; i <= DIV; i++) { var c = GetPoint(i / (float)DIV); sum += Vector2.Distance(c, last); lengths[i] = sum; last = c; }
        }
        static float Cubic(float x0, float x1, float x2, float x3, float t, float ten)
        {
            float t0 = ten * (x2 - x0), t1 = ten * (x3 - x1);
            float c0 = x1, c1 = t0, c2 = -3 * x1 + 3 * x2 - 2 * t0 - t1, c3 = 2 * x1 - 2 * x2 + t0 + t1;
            return c0 + c1 * t + c2 * t * t + c3 * t * t * t;
        }
        public Vector2 GetPoint(float t)
        {
            int l = pts.Length; float p = (l - 1) * t; int ip = Mathf.FloorToInt(p); float w = p - ip;
            if (w == 0 && ip == l - 1) { ip = l - 2; w = 1; }
            Vector2 p0 = ip > 0 ? pts[(ip - 1) % l] : (pts[0] - pts[1]) + pts[0];
            Vector2 p1 = pts[ip % l], p2 = pts[(ip + 1) % l];
            Vector2 p3 = ip + 2 < l ? pts[(ip + 2) % l] : (pts[l - 1] - pts[l - 2]) + pts[l - 1];
            return new Vector2(Cubic(p0.x, p1.x, p2.x, p3.x, w, tension), Cubic(p0.y, p1.y, p2.y, p3.y, w, tension));
        }
        public float Length => lengths[DIV];
        float UtoT(float u)
        {
            int il = lengths.Length; float target = u * lengths[il - 1];
            int low = 0, high = il - 1, i = 0;
            while (low <= high)
            {
                i = low + (high - low) / 2; float cmp = lengths[i] - target;
                if (cmp < 0) low = i + 1; else if (cmp > 0) high = i - 1; else { high = i; break; }
            }
            i = Mathf.Max(0, high);
            if (lengths[i] == target) return i / (float)(il - 1);
            if (i >= il - 1) return 1;
            float before = lengths[i], seg = lengths[i + 1] - before;
            return (i + (target - before) / seg) / (il - 1);
        }
        public Vector2 PointAt(double u) => GetPoint(UtoT((float)u));
        public Vector2 TangentAt(double u)
        {
            float t = UtoT((float)u), t1 = t - .0001f, t2 = t + .0001f;
            if (t1 < 0) t1 = 0; if (t2 > 1) t2 = 1;
            return (GetPoint(t2) - GetPoint(t1)).normalized;
        }
    }

    // Kameralara takılan kanca: sahne sarsıntısı (29-celebrate, #stage.shake) — izdüşüm matrisi ekran uzayında kaydırılır
    public class World_CamHook : MonoBehaviour
    {
        Camera cam; Matrix4x4 saved; bool shaken, wasAuto;
        void Awake() { cam = GetComponent<Camera>(); }
        void OnPreCull()
        {
            if (cam == null) return;
            if (Game.Cel_ShakeOff != Vector2.zero || Game.Cel_ShakeRot != 0)
            {
                saved = cam.projectionMatrix; shaken = true;
                // projectionMatrix'e atama kamerayı "özel matris" kipine sokar (fov/en-boy değişimi artık izlenmez);
                // önceki matris otomatikse sarsıntıdan sonra ResetProjectionMatrix ile otomatiğe dönülür.
                var def = Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
                wasAuto = !cam.orthographic; for (int i = 0; i < 16 && wasAuto; i++) if (Mathf.Abs(def[i] - saved[i]) > 1e-4f) wasAuto = false;
                float w = Mathf.Max(1, UI.W), h = Mathf.Max(1, UI.H);
                var m = Matrix4x4.Translate(new Vector3(2 * Game.Cel_ShakeOff.x / w, -2 * Game.Cel_ShakeOff.y / h, 0)) * Matrix4x4.Rotate(Quaternion.AngleAxis(-Game.Cel_ShakeRot, Vector3.forward));
                cam.projectionMatrix = m * saved;
            }
        }
        void OnPostRender() { if (shaken) { if (wasAuto) cam.ResetProjectionMatrix(); else cam.projectionMatrix = saved; shaken = false; } }
    }

    public static partial class Game
    {
        // ---------- küçük yardımcılar (07 öneki W_) ----------
        internal const int W_LAYER_INT = 9, W_LAYER_THUMB = 29;   // 30 = masa (DESK_LAYER); küçük resimler masa ışıklarından ayrı kalmalı
        internal static double[] W_V(params double[] a) => a;
        internal static float[] W_F(params double[] a) { var r = new float[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = (float)a[i]; return r; }
        internal static double[] W_D(float[] a) { if (a == null) return null; var r = new double[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = a[i]; return r; }
        internal static double[][] W_Pts(params double[] xz) { var r = new double[xz.Length / 2][]; for (int i = 0; i < r.Length; i++) r[i] = new[] { xz[i * 2], xz[i * 2 + 1] }; return r; }
        internal static UData W_UD(Transform t) { var u = t.GetComponent<UData>(); return u != null ? u : t.gameObject.AddComponent<UData>(); }
        internal static UData W_UDr(Transform t) => t == null ? null : t.GetComponent<UData>();
        internal static void W_Shadow(Transform t, bool cast, bool recv)
        {
            if (t == null) return; var r = t.GetComponent<Renderer>(); if (r == null) return;
            r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = recv;
        }
        internal static void W_SetLayer(Transform t, int layer)
        {
            if (t == null) return; t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) W_SetLayer(t.GetChild(i), layer);
        }
        internal static void W_Add(Transform par, Transform child) { if (child != null) child.SetParent(par, false); }
        internal static void W_Remove(Transform t) { if (t != null) { t.gameObject.SetActive(false); t.SetParent(null, false); UnityEngine.Object.Destroy(t.gameObject); } }
        internal static Vector3 W_Pos3(Transform t) => ToThree(t.localPosition);     // THREE yerel konum
        internal static void W_SetPos3(Transform t, Vector3 p) => t.localPosition = V(p.x, p.y, p.z);
        internal static void W_Scale(Transform t, double s) => t.localScale = Vector3.one * (float)s;
        internal static List<Action<float, float>> W_CollectFx(Transform root)
        {
            var fx = new List<Action<float, float>>();
            foreach (var u in root.GetComponentsInChildren<UData>(true)) if (u.fx != null) fx.AddRange(u.fx);
            return fx;
        }
        // CSS cubic-bezier(x1,y1,x2,y2) zaman eğrisi
        internal static float W_Bezier(float x1, float y1, float x2, float y2, float t)
        {
            if (t <= 0) return 0; if (t >= 1) return 1;
            float u = t;
            for (int i = 0; i < 8; i++)
            {
                float x = 3 * (1 - u) * (1 - u) * u * x1 + 3 * (1 - u) * u * u * x2 + u * u * u - t;
                float dx = 3 * (1 - u) * (1 - u) * x1 + 6 * (1 - u) * u * (x2 - x1) + 3 * u * u * (1 - x2);
                if (Mathf.Abs(x) < 1e-5f || Mathf.Abs(dx) < 1e-6f) break;
                u = Mathf.Clamp01(u - x / dx);
            }
            return 3 * (1 - u) * (1 - u) * u * y1 + 3 * (1 - u) * u * u * y2 + u * u * u;
        }
        static Mesh _wDisc;
        internal static Mesh W_DiscMesh()   // THREE.CircleGeometry(1,28) xz düzleminde, yukarı bakar
        {
            if (_wDisc != null) return _wDisc;
            const int N = 28; var v = new Vector3[N + 2]; var uv = new Vector2[N + 2]; var tri = new int[N * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(.5f, .5f);
            for (int i = 0; i <= N; i++)
            {
                float a = i / (float)N * Mathf.PI * 2, x = Mathf.Cos(a), y = Mathf.Sin(a);
                v[i + 1] = new Vector3(x, 0, y); uv[i + 1] = new Vector2((x + 1) / 2, (y + 1) / 2);
            }
            for (int i = 0; i < N; i++) { tri[i * 3] = 0; tri[i * 3 + 1] = i + 2; tri[i * 3 + 2] = i + 1; }
            _wDisc = new Mesh { name = "disc" }; _wDisc.vertices = v; _wDisc.uv = uv; _wDisc.triangles = tri; _wDisc.RecalculateNormals(); _wDisc.RecalculateBounds();
            return _wDisc;
        }
        internal static Transform W_MeshObj(Transform par, Mesh mesh, Material mat, string name = "mesh")
        {
            var go = new GameObject(name); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (par != null) go.transform.SetParent(par, false); return go.transform;
        }

        // ---------- sahne, kamera, denetim ----------
        // SCENE (THREE "world" kökü) 06-engine Init_06'da kurulur.
        public static Transform World => SCENE;
        public static Camera Cam;
        public static OrbitControls Ctl;
        public static readonly Vector3 HUB_T = new Vector3(2, 0, 3), HUB_DIR = new Vector3(-.2f, .44f, .88f).normalized;   // THREE koordinatı
        public static float HUB_D = 50;
        public static EngEnv HemiL;                    // THREE.HemisphereLight (EngEnv: ortam ışığı + sis)
        public static Light Sun;
        public static readonly List<Action<float, float>> WorldFx = new List<Action<float, float>>();
        public static Texture2D WaterTex; public static Material WaterMat;   // 25-desk: WaterMat.mainTextureOffset ile akış
        public static Transform LampPost, GROUND;

        public static void HubLimits() { Ctl.enablePan = true; Ctl.minDistance = 24; Ctl.maxDistance = 90; Ctl.minPolarAngle = .8f; Ctl.maxPolarAngle = 1.22f; }
        public static void FocusLimits() { Ctl.minPolarAngle = .35f; Ctl.maxPolarAngle = 1.15f; }
        public static bool InAnyRect(double x, double z, double m = 0) => REG.Values.Any(r => r.rect != null && x > r.rect[0] - m && x < r.rect[2] + m && z > r.rect[1] - m && z < r.rect[3] + m);

        static void Init_07()
        {
            
            Eng_SceneFog(SCENE, 0xe2dcbc, 90, 200);
            Cam = Eng_Camera("cam", 36, .5f, 600); var cgo = Cam.gameObject; cgo.tag = "MainCamera";
            Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = Hex3(0xe2dcbc);
            Cam.cullingMask = ~((1 << 5) | (1 << W_LAYER_INT) | (1 << W_LAYER_THUMB));
            cgo.AddComponent<World_CamHook>();
            Ctl = new OrbitControls(Cam) { enableDamping = true, dampingFactor = .09f, screenSpacePanning = false, minPolarAngle = .35f, maxPolarAngle = 1.15f, rotateSpeed = .6f, panSpeed = 1.2f };
            Ctl.mouseLEFT = OrbitControls.Act.PAN; Ctl.mouseMIDDLE = OrbitControls.Act.DOLLY; Ctl.mouseRIGHT = OrbitControls.Act.ROTATE;
            Ctl.touchONE = OrbitControls.Act.PAN; Ctl.touchTWO = OrbitControls.Act.DOLLY_ROTATE;
            HUB_D = UI.W < 700 ? 66 : 50;
            Ctl.position = HUB_T + HUB_DIR * HUB_D; Ctl.target = HUB_T;
            HubLimits();
            Ctl.change += () =>
            {
                if (Mode != "hub") return; var t = Ctl.target;
                float cx = Mathf.Max(-32, Mathf.Min(46, t.x)), cz = Mathf.Max(-24, Mathf.Min(24, t.z));
                if (cx != t.x || cz != t.z) { var p = Ctl.position; p.x += cx - t.x; p.z += cz - t.z; Ctl.position = p; t.x = cx; t.z = cz; Ctl.target = t; }
            };
            HemiL = Eng_HemiLight(SCENE, 0xf3ead2, 0x7d6a3a, .75f);
            Sun = Eng_Sun(SCENE, 0xffe6c0, 1.25f, -30, 48, 26, true, GFX.shadowSize);
            Sun.cullingMask = ~((1 << W_LAYER_INT) | (1 << W_LAYER_THUMB));
            World_BuildStatic();
            // çiftçi ve köpeği
            Farmer = FarmerModel(); W_Scale(Farmer, 1.35); W_Add(SCENE, Farmer);
            Dog = DogModel(); W_Scale(Dog, 1.25); W_Add(SCENE, Dog);
            SetPos(Farmer, HUBP[0], .07, HUBP[2]); SetPos(Dog, HUBP[0] + .8, .07, HUBP[2] + .4);
            WorldFx.Add(World_FarmerFx);
        }

        static void World_BuildStatic()
        {
            var world = SCENE;
            // zemin
            var grMat = ToonMat(new MatOpts { map = TEX.grass() });   // tekrar [70,70] dokuya kayıtlı (CanvasTex rep)
            var grGo = GameObject.CreatePrimitive(PrimitiveType.Plane); var col = grGo.GetComponent<Collider>(); if (col != null) UnityEngine.Object.Destroy(col);
            grGo.name = "ground"; grGo.transform.SetParent(world, false); grGo.transform.localScale = new Vector3(50, 1, 50);
            var grR = grGo.GetComponent<MeshRenderer>(); grR.sharedMaterial = grMat; grR.receiveShadows = true; grR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GROUND = grGo.transform;
            var q = Rng(42);
            // kıvrımlı dere, kumsal kıyılar, taşlar, nilüferler
            var RV = W_Pts(-140, -26, -90, -23.5, -50, -27.5, -15, -23.8, 8, -25, 30, -27, 62, -23.6, 100, -26.5, 150, -24);
            Ribbon(world, RV, 8.2, ToonMat(new MatOpts { color = 0xd9c690, side = 2 }), .03, .6);
            WaterTex = TEX.water(); Eng_TexRepeat(WaterTex, 1, 1); WaterMat = ToonMat(new MatOpts { map = WaterTex, side = MatOpts.DoubleSide });
            Ribbon(world, RV, 5.2, WaterMat, .06, .6, 6);
            var rc = new World_Curve(RV);
            int[] stoneC = { 0x9b968a, 0xb0a99a, 0x8c8678 };
            for (int k = 0; k < 70; k++)
            {
                double t = q(); var p = rc.PointAt(t); var tg = rc.TangentAt(t); double sd = q() < .5 ? -1 : 1, o = 3 + q() * .8;
                P(world, "ico", stoneC[k % 3], W_V(.25 + q() * .45, .18 + q() * .2, .25 + q() * .45), W_V(p.x - tg.y * o * sd, .12, p.y + tg.x * o * sd));
            }
            for (int k = 0; k < 26; k++)
            {
                double t = q(); var p = rc.PointAt(t); var tg = rc.TangentAt(t); double o = (q() - .5) * 3;
                var lp = G(world, W_V(p.x - tg.y * o, .08, p.y + tg.x * o)); P(lp, "cyl", 0x5f9a45, W_V(.38, .04, .38));
                if (k % 3 == 0) P(lp, "lsph", 0xf3b6c8, W_V(.12, .08, .12), W_V(.05, .07, 0));
            }
            for (int k = 0; k < 40; k++)
            {
                double t = q(); var p = rc.PointAt(t); var tg = rc.TangentAt(t); double sd = q() < .5 ? -1 : 1, o = 2.7 + q() * .5;
                for (int m = 0; m < 3; m++) P(world, "cone", 0x6f9a3e, W_V(.05, .7 + q() * .4, .05), W_V(p.x - tg.y * o * sd + (q() - .5) * .3, .4, p.y + tg.x * o * sd + (q() - .5) * .3), W_V((q() - .5) * .3, 0, (q() - .5) * .3));
            }
            var br = G(world, W_V(8, 0, -25)); P(br, "box", 0x8a6038, W_V(2.4, .18, 7), W_V(0, .55, 0)); for (int k = 0; k < 9; k++) P(br, "box", 0x6e4a2a, W_V(2.5, .05, .14), W_V(0, .66, -3.4 + k * .85));
            var rail = G(br, W_V(0, .6, 0)); foreach (var x in new[] { -1.15, 1.15 }) FenceRun(rail, x, -3.4, x, 3.4, 0x6e4a2a, .6);
            // kasaba yolu (hafif kıvrımlı) ve fenerler
            var TR = W_Pts(-150, 14.4, -80, 13.2, -30, 14.4, 0, 13.6, 30, 14.2, 80, 13.2, 150, 14);
            Ribbon(world, TR, 3.8, DirtEdge(), .03, .5);
            Ribbon(world, TR, 3, DirtMat(), .045, .5, 3);
            for (int k = -3; k <= 3; k++)
            {
                if (k == 0) continue; double x = k * 13; if (Math.Abs(x - 19) < 3) continue;
                var lp = G(world, W_V(x, 0, 16)); P(lp, "cyl", 0x3a3a40, W_V(.06, 2.4, .06), W_V(0, 1.2, 0)); P(lp, "box", 0xffe2a0, W_V(.22, .28, .22), W_V(0, 2.45, 0), null, new MatOpts { emissive = 0xffc860, emissiveIntensity = .5f });
            }
            // ördekli gölet
            var pd = G(world, W_V(2.6, 0, 10));
            var pw = W_MeshObj(pd, W_DiscMesh(), ToonMat(new MatOpts { map = TEX.water() }), "pond"); pw.localScale = new Vector3(2.2f, 1, 1.6f); pw.localPosition = new Vector3(0, .06f, 0); W_Shadow(pw, false, true);
            for (int k = 0; k < 16; k++) { double a = k / 16.0 * 6.28; P(pd, "ico", k % 2 == 0 ? 0x9b968a : 0xb0a99a, W_V(.32, .2, .3), W_V(Math.Cos(a) * 2.35, .1, Math.Sin(a) * 1.75)); }
            for (int k = 0; k < 7; k++) { double a = 2.4 + k * .18; P(pd, "cone", 0x6f9a3e, W_V(.05, .9, .05), W_V(Math.Cos(a) * 2, .45, Math.Sin(a) * 1.4)); if (k % 2 == 1) P(pd, "cyl", 0x6b4423, W_V(.06, .25, .06), W_V(Math.Cos(a) * 2, .95, Math.Sin(a) * 1.4)); }
            var ducks = new List<Transform>(); var duckHeads = new List<Transform>();
            for (int k = 0; k < 2; k++)
            {
                var d = G(pd); P(d, "sph", k != 0 ? 0xf6efe0 : 0xf5d24a, W_V(.24, .18, .3), W_V(0, .18, 0));
                var hd = G(d, W_V(0, .38, .2)); P(hd, "sph", k != 0 ? 0xf6efe0 : 0x3f7a4a, W_V(.13, .13, .13)); P(hd, "box", 0xf08a24, W_V(.08, .04, .14), W_V(0, -.02, .14));
                P(hd, "lsph", 0x111111, W_V(.025, .025, .025), W_V(.07, .04, .06)); P(hd, "lsph", 0x111111, W_V(.025, .025, .025), W_V(-.07, .04, .06));
                P(d, "box", k != 0 ? 0xf6efe0 : 0xf5d24a, W_V(.06, .12, .12), W_V(0, .3, -.26), W_V(-.6, 0, 0)); ducks.Add(d); duckHeads.Add(hd);
            }
            WorldFx.Add((dt, t) =>
            {
                for (int k = 0; k < ducks.Count; k++)
                {
                    var d = ducks[k]; float a = t * .35f + k * 3.1f;
                    SetPos(d, Math.Cos(a) * 1.1, .02 + Math.Sin(t * 3 + k) * .02, Math.Sin(a) * .75); SetRotY(d, -a);
                    SetRot(duckHeads[k], Math.Max(0, Math.Sin(t * 1.7 + k * 2)) * .7, 0, 0);
                }
            });
            // çevre: yama yama tarlalar ve çit sıraları
            int[] PATCH = { 0xd9b44a, 0x9cbf5a, 0x86b04c, 0xe6cf72, 0xa8c46a };
            var plowP = new Paint(64, 64); plowP.Rect(0, 0, 64, 64, UI.Hex("#9a6a3c")); for (int i = 0; i < 64; i += 8) plowP.Rect(0, i, 64, 4, UI.Hex("#7d5230"));
            var plowTex = plowP.Apply(); Eng_TexRepeat(plowTex, 4, 4);
            var plow = ToonMat(new MatOpts { map = plowTex });
            void quilt(double x0, double z0, int cols, int rows, double cw, double ch, double rot)
            {
                var qg = G(world, W_V(x0, 0, z0), rot);
                for (int i = 0; i < cols; i++) for (int j = 0; j < rows; j++)
                    {
                        double x = (i - (cols - 1) / 2.0) * cw, z = (j - (rows - 1) / 2.0) * ch, c = q();
                        if (c < .22) F(qg, plow, W_V(cw - .9, .05, ch - .9), W_V(x, .03, z));
                        else F(qg, PATCH[(int)Math.Floor(q() * PATCH.Length)], W_V(cw - .9, .05, ch - .9), W_V(x, .03, z));
                        if (c > .85) for (int k = 0; k < 6; k++) Oak(qg, x + (q() - .5) * (cw - 3), z + (q() - .5) * (ch - 3), .6 + q() * .3);
                    }
                for (int i = 0; i <= cols; i++) { double x = (i - cols / 2.0) * cw; for (int k = 0; k < rows * ch / 1.6; k++) if (q() < .75) P(qg, "ico1", 0x4f8a36, W_V(.5, .42, .5), W_V(x, .3, -rows * ch / 2 + k * 1.6 + .8)); }
                for (int j = 0; j <= rows; j++) { double z = (j - rows / 2.0) * ch; for (int k = 0; k < cols * cw / 1.6; k++) if (q() < .75) P(qg, "ico1", 0x4f8a36, W_V(.5, .42, .5), W_V(-cols * cw / 2 + k * 1.6 + .8, .3, z)); }
            }
            quilt(4, 33, 9, 3, 12, 8, .04); quilt(-56, -2, 3, 4, 9, 10, -.06); quilt(66, -2, 3, 4, 10, 10, .08);
            bool inQuilt(double x, double z) => (z > 20 && z < 46 && x > -52 && x < 60) || (x < -40 && x > -72 && z > -22 && z < 19) || (x > 50 && x < 82 && z > -22 && z < 19);
            // orman kümeleri
            for (int c = 0; c < 26; c++)
            {
                double cx = -110 + q() * 220, cz = -70 + q() * 125;
                if (InAnyRect(cx, cz, 6) || inQuilt(cx, cz) || (cz > -33 && cz < -17) || (cz > 10 && cz < 19)) continue;
                bool pin = cz < -30 || q() < .4;
                for (int k = 0; k < 9 + q() * 9; k++)
                {
                    double x = cx + (q() - .5) * 14, z = cz + (q() - .5) * 10;
                    if (InAnyRect(x, z, 1.5) || inQuilt(x, z) || (z > -31 && z < -19) || (z > 11 && z < 17.5)) continue;
                    double s = .8 + q() * .8; if (pin) Pine(world, x, z, s * 1.2); else Oak(world, x, z, s);
                }
            }
            for (int k = 0; k < 60; k++)
            {
                double x = -100 + q() * 200, z = -60 + q() * 110;
                if (InAnyRect(x, z, 1.5) || inQuilt(x, z) || (z > -31 && z < -19) || (z > 11 && z < 24 && x > -20 && x < 22) || (z > 11.5 && z < 16.5)) continue;
                if (q() < .5) Oak(world, x, z, .8 + q() * .6); else Bush(world, x, z, 1 + q());
            }
            // uzak dağlar (katmanlı)
            var MT = new[] { new[] { 0x9fb69a, -95 }, new[] { 0x8fa98e, -110 }, new[] { 0x7f9a88, -128 } };
            for (int i = 0; i < MT.Length; i++)
            {
                int c = MT[i][0]; double z = MT[i][1];
                for (int k = 0; k < 9; k++)
                {
                    double x = -170 + k * 42 + (q() - .5) * 20, h = 18 + q() * 16 + i * 6, r = 16 + q() * 10;
                    P(world, "cone6", c, W_V(r, h, r * .7), W_V(x, h / 2 - 1, z + (q() - .5) * 8));
                    if (i > 0 && q() < .6) P(world, "cone6", 0xf6f2e6, W_V(r * .32, h * .32, r * .22), W_V(x, h * .86 - 1, z + (q() - .5) * 8 + .5));
                }
            }
            // küçük ve seyrek bulutlar
            for (int k = 0; k < 4; k++)
            {
                var c = G(world, W_V(-160 + k * 95 + q() * 30, 30 + q() * 6, -50 + q() * 70)); W_Scale(c, .5 + q() * .25);
                for (int m = 0; m < 4; m++) { double s = 1.4 + q() * 1.4; var mm = P(c, "ico1", 0xfdf8ec, W_V(s * 1.4, s, s), W_V(m * 1.7 - 2.6, q() * .8, q() * 1.2)); W_Shadow(mm, false, false); }
                float sp = (float)(.25 + q() * .25);
                WorldFx.Add((dt, t) => { var lp = c.localPosition; lp.x += dt * sp; if (lp.x > 190) lp.x = -190; c.localPosition = lp; });
            }
            for (int f = 0; f < 2; f++)
            {
                var fl = G(world, W_V(-80 + f * 70, 18 + f * 4, -30 + f * 22)); var wings = new List<(Transform w, int s, int k)>();
                for (int k = 0; k < 5; k++)
                {
                    var b = G(fl, W_V(-Math.Abs(k - 2) * 1.3, Math.Sin(k) * .4, (k - 2) * 1.2)); P(b, "box", 0x3a2a20, W_V(.5, .12, .18));
                    foreach (var s in new[] { -1, 1 }) { var w2 = G(b, W_V(0, 0, s * .08)); P(w2, "box", 0x3a2a20, W_V(.25, .04, .7), W_V(0, 0, s * .35)); wings.Add((w2, s, k)); }
                }
                float sp = 3 + f * 1.5f;
                WorldFx.Add((dt, t) =>
                {
                    var lp = fl.localPosition; lp.x += dt * sp; if (lp.x > 100) lp.x = -100; fl.localPosition = lp;
                    foreach (var (w2, s, k) in wings) SetRot(w2, s * Math.Sin(t * 9 + k) * .7, 0, 0);
                });
            }
            // arka sokak feneri (gizli)
            LampPost = G(null); LampPost.name = "lampPost"; LampPost.gameObject.SetActive(false);
            for (int k = 0; k < 2; k++)
            {
                var pp = Person(new PersonOpt { shirt = k == 0 ? 0x9a3a22 : 0x3f6fa0, hat = k != 0 ? "cap" : "scarf", hatc = k == 0 ? 0xf2b330 : 0x2a2a2a, dress = k != 0 ? (int?)null : 0x9a3a22 });
                W_Scale(pp, 1.25); W_Add(world, pp);
                WorldFx.Add(WalkPerson(pp, k != 0 ? W_Pts(-26, 14.2, 30, 13.8) : W_Pts(34, 14.6, -20, 13.4), 1.2, 4));
            }
            WorldFx.Add((dt, t) => { for (int i = 0; i < SwayList.Count; i++) SwayList[i](dt, t); });
        }

        // --- arazi bölgeleri ve yollar ---
        public static Transform RegG, RoadG;
        public static List<Action<float, float>> RegFx = new List<Action<float, float>>();
        public static string RegSig = "";
        public static readonly double[] HUBP = { -1, 0, 5.6 };
        public static double[] Entrance(string k) { var L = LOC[k]; return new double[] { L.p[0], L.p[2] + (L.kind == "fields" ? 7.2 : L.kind == "race" ? 7.5 : L.r * .95) }; }
        public static void Road(Transform par, double x1, double z1, double x2, double z2, double w = 1.5)
        {
            double L0 = Math.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1)); if (L0 < .1) return;
            double mx = (x1 + x2) / 2, mz = (z1 + z2) / 2, bend = (((x1 * 7 + z2 * 3) % 5) / 5 - .5) * .28 * L0, nx = -(z2 - z1) / L0, nz = (x2 - x1) / L0;
            var pts = new[] { new[] { x1, z1 }, new[] { mx + nx * bend, mz + nz * bend }, new[] { x2, z2 } };
            Ribbon(par, pts, w + .5, DirtEdge(), .03); Ribbon(par, pts, w, DirtMat(), .045);
        }
        public static void _RoadOld(Transform par, double x1, double z1, double x2, double z2, double w = 1.5)
        {
            double L = Math.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1)); if (L < .1) return;
            F(par, 0xcfb27e, W_V(L + w * .4, .05, w), W_V((x1 + x2) / 2, .035, (z1 + z2) / 2), W_V(0, -Math.Atan2(z2 - z1, x2 - x1), 0));
        }
        public static void SyncRegions()
        {
            var keys = S.regions.Keys.ToList(); keys.Sort(string.CompareOrdinal); var sig = string.Join(",", keys);
            if (sig == RegSig) return; RegSig = sig;
            if (RegG != null) { DisposeBaked(RegG); W_Remove(RegG); }
            if (RoadG != null) { DisposeBaked(RoadG); W_Remove(RoadG); }
            RegG = G(SCENE); RegG.name = "regions"; RoadG = G(SCENE); RoadG.name = "roads";
            foreach (var r in REG.Keys)
            {
                var d = REG[r]; if (d.rect == null || Owned(r)) continue;
                double x1 = d.rect[0], z1 = d.rect[1], x2 = d.rect[2], z2 = d.rect[3], cx = (x1 + x2) / 2, cz = (z1 + z2) / 2, w = x2 - x1, dd = z2 - z1;
                var g = G(RegG, W_V(cx, 0, cz)); W_UD(g).region = r;
                F(g, 0xb9bd88, W_V(w - .4, .06, dd - .4), W_V(0, .04, 0)); var q = Rng(r[0] * 97 + w);
                for (int k = 0; k < RoundI(w * dd / 22); k++)
                {
                    double a = (q() - .5) * (w - 2), b = (q() - .5) * (dd - 2);
                    if (q() < .5) Bush(g, a, b, .7 + q() * .5, 0x7d9a52); else P(g, "ico", 0xa9a294, W_V(.3 + q() * .3, .2, .3 + q() * .3), W_V(a, .15, b));
                }
                int n = RoundI(w * dd / 15);
                for (int k = 0; k < n; k++)
                {
                    double a = (q() - .5) * (w - 1.5), b = (q() - .5) * (dd - 1.5); var c = G(g, W_V(a, .5 + q() * .5, b)); int m = 3 + (int)Math.Floor(q() * 3);
                    for (int j = 0; j < m; j++) { double s = .8 + q() * 1; var mm = P(c, "ico1", 0xf6f0e2, W_V(s * 1.3, s * .7, s), W_V((q() - .5) * 2.6, q() * .6, (q() - .5) * 2.2)); W_Shadow(mm, false, false); }
                    float ph = (float)(q() * 6); var p0 = W_Pos3(c); float y0 = p0.y, x0 = p0.x;
                    AddFx(g, (dt, t) => { var p = W_Pos3(c); p.y = y0 + Mathf.Sin(t * .55f + ph) * .18f; p.x = x0 + Mathf.Sin(t * .25f + ph) * .35f; W_SetPos3(c, p); });
                }
            }
            RegFx = W_CollectFx(RegG);
            foreach (var k in LOC.Keys)
            {
                var L = LOC[k]; if (!Owned(L.reg) || k == "ev") continue; var e = Entrance(k);
                if (L.reg == "town" || L.reg == "bagyolu") Road(RoadG, e[0], e[1] - 2.6, e[0], 13.8, 1.4); else Road(RoadG, HUBP[0], HUBP[2], e[0], e[1]);
            }
            Road(RoadG, HUBP[0], HUBP[2], HUBP[0], 13.8, 1.8);
            BuildPaths(); BatchWatch(RegG); BatchWatch(RoadG);
        }

        // --- konumlar ---
        public static readonly Dictionary<string, Transform> LocGroups = new Dictionary<string, Transform>();
        public static readonly Dictionary<string, string> LocSigs = new Dictionary<string, string>();
        public static string LocSig(string k)
        {
            var L = LOC[k]; if (!Owned(L.reg)) return "L"; string s = "";
            if (L.kind == "house") s += "h" + S.house + ToJson(S.decor.farm);
            if (L.kind == "fields") s += Get(S.workers, "fields") != 0 ? "W" : "";
            if (L.units != null) foreach (var u in L.units) if (Get(S.workers, u) != 0) s += "W" + u;
            if (L.kind == "fields") s += "f" + string.Join(",", S.fields.Select(f => f.job != null ? f.job.r + StageOf(f.job) : "_"));
            if (L.units != null) foreach (var u in L.units)
                {
                    var x = GetOr(S.units, u);
                    s += u + (x != null ? x.l + ((u == "sera" || u == "bahce") && x.job != null ? x.job.r + StageOf(x.job) : "") : (S.lv >= B[u].lvl ? "o" : "x"));
                }
            return s;
        }
        public static float[] UnitOffset(string k, int j) { var L = LOC[k]; return L.slots != null ? L.slots[j] : new float[] { 0, 0 }; }
        public static double UnitScale(string k) => LOC[k].slots != null && LOC[k].kind != "race" ? 1 : 1.25;
        // THREE koordinatında dünya konumu
        public static Vector3 UnitWorld(string u, double y = 0) { var k = LocOf(u); var L = LOC[k]; var o = UnitOffset(k, L.units.IndexOf(u)); return new Vector3(L.p[0] + o[0], (float)y, L.p[2] + o[1]); }
        public static double[] PlotOff(int i) => new double[] { ((i % 3) - 1) * 4.2, (Math.Floor(i / 3.0) - 1) * 4.2 };
        public static Vector3 PlotWorld(int i, double y = 0) { var L = LOC["tarla"]; var o = PlotOff(i); return new Vector3((float)(L.p[0] + o[0]), (float)y, (float)(L.p[2] + o[1])); }
        public static void Breathe(Transform g, Transform host, double ph, bool soft = true)
        {
            AddFx(host, (dt, tm) =>
            {
                var gu = W_UDr(g); float sq = gu != null ? gu.squash : 0;
                if (sq > 0) { sq = Mathf.Max(0, sq - dt * 1.8f); gu.squash = sq; }
                float b = (soft ? Mathf.Sin(tm * 2.2f + (float)ph) * .014f : 0) + Mathf.Sin((1 - sq) * Mathf.PI * 4) * sq * .16f;
                host.localScale = new Vector3(1 - b * .55f, 1 + b, 1 - b * .55f);
            });
        }
        public static Transform MakeLoc(string k)
        {
            var L = LOC[k]; var g = G(null, W_D(L.p)); g.name = "loc_" + k; W_UD(g).loc = k; if (!Owned(L.reg)) return g;
            switch (L.kind)
            {
                case "house":
                    {
                        var hg = G(g); var h = House(S.house); h.localScale *= 1.35f; W_Add(hg, h); Breathe(g, hg, 1);
                        var hu = W_UDr(h); if (hu != null && hu.chimney != null) AddSmoke(h, hu.chimney, () => true, hg);
                        Well(g, 3.8, 1.6); Flowers(g, -3.2, 2.6, 1.6, 3); Flowers(g, 2.6, 3.2, 1.2, 9); Bush(g, -3.4, -1.6, .9); Bush(g, 3.4, -1.8, .8); F(g, 0xd8c8a2, W_V(1.2, .05, 2.6), W_V(0, .03, 3.4));
                        var mb = G(g, W_V(1.2, 0, 4.3)); P(mb, "box", 0x6e4a2a, W_V(.08, .9, .08), W_V(0, .45, 0)); P(mb, "box", 0x3f6fa0, W_V(.3, .25, .45), W_V(0, .95, 0)); FarmDecor(g); break;
                    }
                case "fields":
                    {
                        double h = 6.5; FenceRun(g, -h, -h, h, -h); FenceRun(g, -h, -h, -h, h); FenceRun(g, h, -h, h, h); FenceRun(g, -h, h, -1.2, h); FenceRun(g, 1.2, h, h, h); Scarecrow(g, -7.4, 6.6);
                        if (Get(S.workers, "fields") != 0) { var w = Person(new PersonOpt { shirt = 0x7a9a4a, hat = "straw" }); W_Add(g, w); AddFx(g, WalkPerson(w, W_Pts(-2.1, -2.1, 2.1, -2.1, 2.1, 2.1, -2.1, 2.1), 1, 1.5)); }
                        for (int i = 0; i < 9; i++)
                        {
                            var xz = PlotOff(i); var pg = G(g, W_V(xz[0], 0, xz[1]));
                            if (i < S.fields.Count) { W_UD(pg).plot = i; W_UD(pg).hasPlot = true; FieldBed(pg, S.fields[i]); if (PopPlot == i) { W_UD(pg).pop = 0; PopPlot = null; } }
                            else if (i == S.fields.Count)
                            {
                                W_UD(pg).plotBuy = true; P(pg, "box", 0xa7c46a, W_V(3.7, .08, 3.7), W_V(0, .04, 0));
                                foreach (var ab in new[] { new[] { -1.7, -1.7 }, new[] { 1.7, -1.7 }, new[] { -1.7, 1.7 }, new[] { 1.7, 1.7 } }) P(pg, "box", 0xc9a05a, W_V(.08, .6, .08), W_V(ab[0], .35, ab[1]));
                                var s = G(pg, W_V(0, 0, .4)); P(s, "box", 0x6e4a2a, W_V(.08, 1, .08), W_V(0, .5, 0)); var b = P(s, "box", 0xf2b330, W_V(.9, .55, .06), W_V(0, 1, 0));
                                P(s, "box", 0x2f6b3a, W_V(.4, .1, .08), W_V(0, 1, .03)); P(s, "box", 0x2f6b3a, W_V(.1, .4, .08), W_V(0, 1, .03));
                                AddFx(g, (dt, t) => SetRot(b, 0, 0, Math.Sin(t * 2.4) * .1));
                            }
                            else
                            {
                                var q = Rng(i * 13); W_Shadow(P(pg, "box", 0x93a85a, W_V(3.7, .06, 3.7), W_V(0, .03, 0)), false, true);
                                for (int m = 0; m < 5; m++) P(pg, "cone", 0x7d9a45, W_V(.08, .4, .08), W_V((q() - .5) * 3, .25, (q() - .5) * 3));
                            }
                        }
                        break;
                    }
                case "market": MarketModel(g); break;
                case "orders": HanModel(g); break;
                case "race": RaceTrack(g, GetOr(S.units, "hara")); break;
                case "fair": FairModel(g); break;
            }
            if (L.units != null) for (int j = 0; j < L.units.Count; j++)
                {
                    var u = L.units[j]; var o = UnitOffset(k, j); var ug = G(g, W_V(o[0], 0, o[1])); W_UD(ug).unit = u; var st = GetOr(S.units, u);
                    if (st != null)
                    {
                        var sc = G(ug); W_Scale(sc, UnitScale(k)); var inner = G(sc); MODELS[u](inner, st); Scaffold(ug, st); Breathe(ug, inner, j * .9 + k.Length, !B[u].field);
                        if (Get(S.workers, u) != 0) { var w = Person(new PersonOpt { shirt = 0xc98a3a, hat = "cap", hatc = 0x3f6fa0 }); SetPos(w, 1.9, 0, 2.1); W_Add(ug, w); AddFx(ug, IdlePerson(w, j)); }
                    }
                    else Foundation(ug, S.lv < B[u].lvl);
                }
            if (k == "atolye")
            {
                F(g, StdTex(TEX.stone(W_V(6, 3))), W_V(17.5, .06, 9.5), W_V(0, .03, 0));
                foreach (var xz in new[] { new[] { -8.8, 4.4 }, new[] { 8.8, 4.4 } })
                {
                    var lp = G(g, W_V(xz[0], 0, xz[1])); P(lp, "cyl", 0x3a3a40, W_V(.06, 2.4, .06), W_V(0, 1.2, 0));
                    P(lp, "box", 0xffe2a0, W_V(.22, .28, .22), W_V(0, 2.45, 0), null, new MatOpts { emissive = 0xffc860, emissiveIntensity = .5f });
                }
            }
            return g;
        }
        public static void Squash(string k, string u = null)
        {
            Transform g; if (!LocGroups.TryGetValue(k, out g) || g == null) return;
            if (u != null) { for (int i = 0; i < g.childCount; i++) { var c = g.GetChild(i); var cu = W_UDr(c); if (cu != null && cu.unit == u) cu.squash = 1; } }
            else W_UD(g).squash = 1;
        }
        public static void SyncWorld(bool force = false)
        {
            SyncRegions();
            foreach (var k in LOC.Keys)
            {
                var s = LocSig(k); if (!force && LocSigs.TryGetValue(k, out var old) && s == old) continue; LocSigs[k] = s;
                if (LocGroups.TryGetValue(k, out var og) && og != null) { DisposeBaked(og); W_Remove(og); }
                var g = MakeLoc(k); W_UD(g).allFx = W_CollectFx(g); LocGroups[k] = g; W_Add(SCENE, g); BatchWatch(g);
                if (PopLoc == k) { W_UD(g).pop = 0; PopLoc = null; }
            }
            LampPost.gameObject.SetActive(false); SyncMarks();
        }

        // --- çiftçi ve köpeği ---
        public static Transform FarmerModel()
        {
            var g = G(null); g.name = "farmer"; int skin = 0xe8b48a; var legs = new List<Transform>(); var arms = new List<Transform>();
            foreach (var s in new[] { -1, 1 }) { var l = G(g, W_V(s * .11, .52, 0)); P(l, "box", 0x3f5f8a, W_V(.15, .5, .17), W_V(0, -.25, 0)); P(l, "box", 0x3a2416, W_V(.17, .09, .24), W_V(0, -.5, .03)); legs.Add(l); }
            P(g, "box", 0x3f5f8a, W_V(.4, .44, .26), W_V(0, .74, 0)); P(g, "box", 0xe9e2cf, W_V(.42, .22, .28), W_V(0, 1.0, 0)); foreach (var s in new[] { -1, 1 }) P(g, "box", 0x3f5f8a, W_V(.06, .3, .03), W_V(s * .12, .98, .14));
            foreach (var s in new[] { -1, 1 }) { var a = G(g, W_V(s * .27, 1.06, 0)); P(a, "box", 0xe9e2cf, W_V(.13, .44, .14), W_V(0, -.21, 0)); P(a, "sph", skin, W_V(.075, .075, .075), W_V(0, -.46, 0)); arms.Add(a); }
            var hd = G(g, W_V(0, 1.33, 0)); P(hd, "sph", skin, W_V(.21, .22, .21)); P(hd, "sph", 0xd99a6a, W_V(.06, .06, .06), W_V(0, -.03, .21)); P(hd, "lsph", 0x1a1a1a, W_V(.03, .035, .03), W_V(.08, .05, .18)); P(hd, "lsph", 0x1a1a1a, W_V(.03, .035, .03), W_V(-.08, .05, .18));
            P(hd, "box", 0x8a5a32, W_V(.22, .06, .05), W_V(0, -.1, .19)); P(hd, "cyl", 0xe2c46a, W_V(.38, .03, .38), W_V(0, .15, 0)); P(hd, "cyl", 0xe2c46a, W_V(.18, .2, .18), W_V(0, .26, 0)); P(hd, "cyl", 0xb5402c, W_V(.185, .045, .185), W_V(0, .19, 0));
            var u = W_UD(g); u.legs = legs; u.arms = arms; u.head = hd; u.farmer = true; return g;
        }
        public static Transform DogModel()
        {
            var g = G(null); g.name = "dog"; int c = 0xb07a46; P(g, "box", c, W_V(.26, .24, .55), W_V(0, .34, 0)); P(g, "box", 0xf0e2c4, W_V(.2, .12, .3), W_V(0, .24, .08));
            var h = G(g, W_V(0, .5, .3)); P(h, "box", c, W_V(.24, .22, .24)); P(h, "box", 0xf0e2c4, W_V(.14, .1, .14), W_V(0, -.04, .15)); P(h, "lsph", 0x1a1a1a, W_V(.035, .03, .03), W_V(0, 0, .23));
            foreach (var s in new[] { -1, 1 }) { P(h, "box", 0x6e4424, W_V(.07, .16, .09), W_V(s * .12, .06, -.03), W_V(0, 0, s * .45)); P(h, "lsph", 0x1a1a1a, W_V(.025, .025, .025), W_V(s * .06, .05, .12)); }
            var tail = G(g, W_V(0, .42, -.27)); P(tail, "box", c, W_V(.06, .06, .24), W_V(0, .06, -.1), W_V(.7, 0, 0)); var legs = new List<Transform>();
            foreach (var xz in new[] { new[] { .09, .19 }, new[] { -.09, .19 }, new[] { .09, -.19 }, new[] { -.09, -.19 } }) { var l = G(g, W_V(xz[0], .24, xz[1])); P(l, "box", c, W_V(.08, .24, .08), W_V(0, -.12, 0)); legs.Add(l); }
            var u = W_UD(g); u.tail = tail; u.head = h; u.legs = legs; u.farmer = true; return g;
        }
        public static Transform Farmer, Dog;
        static readonly List<double[]> fRoute = new List<double[]>();
        static double fRouteHold, fWait = 1;
        public static double FWave = 0;
        static double fRotY, dRotY;
        public static void BuildPaths() { fRoute.Clear(); fRouteHold = 0; }
        public static double LerpA(double a, double b, double k) { double d = b - a; d = Math.Atan2(Math.Sin(d), Math.Cos(d)); return a + d * k; }
        static void World_FarmerFx(float dt, float t)
        {
            var u = W_UDr(Farmer); var fp = W_Pos3(Farmer); bool walking = false;
            if (fRoute.Count == 0)
            {
                fWait -= dt;
                if (fWait <= 0)
                {
                    var ks = LOC.Keys.Where(k => Owned(LOC[k].reg) && k != "ev" && LOC[k].reg != "town").ToList(); string k0 = ks.Count > 0 ? ks[(int)Math.Floor(Rnd() * ks.Count)] : null;
                    if (k0 != null) { var e = Entrance(k0); fRoute.Clear(); fRouteHold = 0; fRoute.Add(new[] { e[0], e[1], 1.5 + Rnd() * 2.5 }); fRoute.Add(new[] { HUBP[0], HUBP[2], 1 + Rnd() * 2 }); }
                }
            }
            else
            {
                double tx = fRoute[0][0], tz = fRoute[0][1], dx = tx - fp.x, dz = tz - fp.z, d = Math.Sqrt(dx * dx + dz * dz);
                if (d < .06) { fWait = fRoute[0][2]; fRoute.RemoveAt(0); if (fRoute.Count > 0) fRouteHold = fWait; }
                else if (fRouteHold > 0) fRouteHold -= dt;
                else { walking = true; double s = Math.Min(d, 1.9 * dt); fp.x += (float)(dx / d * s); fp.z += (float)(dz / d * s); fRotY = LerpA(fRotY, Math.Atan2(dx, dz), Math.Min(1, dt * 8)); }
            }
            double sw = walking ? Math.Sin(t * 9) : 0;
            SetRot(u.legs[0], sw * .6, 0, 0); SetRot(u.legs[1], -sw * .6, 0, 0); SetRot(u.arms[0], -sw * .5, 0, 0);
            double a1x = sw * .5, a1z;
            fp.y = (float)(.07 + (walking ? Math.Abs(Math.Sin(t * 9)) * .08 : 0));
            if (FWave > 0) { FWave -= dt; a1z = -2.4 + Math.Sin(t * 14) * .35; a1x = 0; } else a1z = 0;
            SetRot(u.arms[1], a1x, 0, a1z);
            SetRotY(u.head, walking ? 0 : Math.Sin(t * .8) * .5);
            W_SetPos3(Farmer, fp); SetRotY(Farmer, fRotY);
            var du = W_UDr(Dog); var dp = W_Pos3(Dog);
            double ox = fp.x - Math.Sin(fRotY + .7) * 1.1, oz = fp.z - Math.Cos(fRotY + .7) * 1.1, ddx = ox - dp.x, ddz = oz - dp.z, dd = Math.Sqrt(ddx * ddx + ddz * ddz); bool dw = false;
            if (dd > .15) { dw = true; double s = Math.Min(dd, (dd > 1.5 ? 3.4 : 2.2) * dt); dp.x += (float)(ddx / dd * s); dp.z += (float)(ddz / dd * s); dRotY = LerpA(dRotY, Math.Atan2(ddx, ddz), Math.Min(1, dt * 9)); }
            SetRotY(du.tail, Math.Sin(t * (dw ? 16 : 9)) * .7); double ds = dw ? Math.Sin(t * 16) : 0;
            for (int k = 0; k < du.legs.Count; k++) SetRot(du.legs[k], (k % 2 == 1 ? ds : -ds) * .6, 0, 0);
            dp.y = (float)(.07 + (dw ? Math.Abs(ds) * .05 : 0)); W_SetPos3(Dog, dp); SetRotY(Dog, dRotY);
            SetRot(du.head, 0, 0, dw ? 0 : Math.Sin(t * 1.3) * .15);
        }
    }
}
