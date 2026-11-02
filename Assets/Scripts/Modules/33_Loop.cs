// ================= DÖNGÜ (33-loop.js) =================
// Kare döngüsü (requestAnimationFrame → Game.OnFrame), 500 ms'lik tick, 5 sn'lik kayıt ve açılış sırası.
// Bu modül en son başlatılır (ORDER'da 33 sonda) ve oyunu başlatır.
// Unity'de kameralar kendiliğinden çizer: "R.render(sahne,kamera)" yerine o kipin kamerası açılır, diğerleri kapatılır.
// Gölge haritası elle güncellenmez (Unity her karede günceller); WebGL bağlam kaybı Unity'de yoktur.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // data-s / data-e: süren işin ilerleme çubuğu (tick her 500 ms'de genişliği günceller)
    public class LiveBar : MonoBehaviour
    {
        public static readonly List<LiveBar> All = new List<LiveBar>();
        public double s, e; public Image fill;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }
    // data-left / data-pre / data-short: geri sayım yazısı
    public class LiveLeft : MonoBehaviour
    {
        public static readonly List<LiveLeft> All = new List<LiveLeft>();
        public double left; public string pre = ""; public bool @short; public Text txt;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }

    public static partial class Game
    {
        static double Loop_clockPrev = -1, Loop_clockT;   // THREE.Clock
        public static Vector2 Vo = Vector2.zero;           // const vo={x:0,y:0}
        static readonly Dictionary<Camera, bool> Loop_viewOn = new Dictionary<Camera, bool>();
        
        // Bir ilerleme çubuğunu / geri sayımı tick'e bağla (HTML'deki data-e / data-left öznitelikleri)
        public static void Loop_DataE(Image fill, double s, double e) { if (fill == null) return; var b = fill.gameObject.GetOrAdd<LiveBar>(); b.fill = fill; b.s = s; b.e = e; UI.SetBar(fill, (float)Math.Min(1, (Now() - s) / (e - s))); }
        public static void Loop_DataLeft(Text t, double left, string pre, bool shortFmt = false) { if (t == null) return; var l = t.gameObject.GetOrAdd<LiveLeft>(); l.txt = t; l.left = left; l.pre = pre ?? ""; l.@short = shortFmt; }

        // Açık kart dünyanın bir kısmını örtüyorsa sahne görünür alanın ortasına kaydırılır (camera.setViewOffset)
        public static void ViewOffset(Camera c)
        {
            if (c == null) return;
            float W = UI.W, H = UI.H, tx = 0, ty = 0;
            bool sheetOn = Sheet != null && Sheet.gameObject.activeInHierarchy;
            if (sheetOn && Mode != "hub" || (sheetOn && View != null && View.k == "region"))
            {
                var cs = new Vector3[4]; Sheet.GetWorldCorners(cs);
                var tl = UI.ScreenToCss(new Vector2(cs[1].x, cs[1].y));   // sol üst köşe (getBoundingClientRect left/top)
                if (W > 700) tx = Math.Max(0, W - tl.x) / 2; else ty = Math.Max(0, H - tl.y) / 2;
            }
            Vo.x += (tx - Vo.x) * .12f; Vo.y += (ty - Vo.y) * .12f;
            bool on; Loop_viewOn.TryGetValue(c, out on);
            if (tx == 0 && ty == 0 && Math.Abs(Vo.x) < .5f && Math.Abs(Vo.y) < .5f) { Vo.x = Vo.y = 0; if (on) { c.ResetProjectionMatrix(); Loop_viewOn[c] = false; } return; }
            c.ResetProjectionMatrix();
            var p = c.projectionMatrix;
            p[0, 2] += 2 * Vo.x / W; p[1, 2] -= 2 * Vo.y / H;
            c.projectionMatrix = p; Loop_viewOn[c] = true;
        }

        public static void Resize()
        {
            float w = UI.W;
            Dcam.fieldOfView = w < 700 ? 50 : 42;
            if (S != null && (Dcam.aspect < .85f) != DeskPortrait && DESKP.Count > 0) BuildDesk();
            if (Cam != null) Cam.fieldOfView = w < 700 ? 50 : 36;
            if (Icam != null) Icam.fieldOfView = w < 700 ? 60 : 42;
        }

        // Kipin kamerasını aç (R.render(sahne,kamera) karşılığı)
        static void Loop_Cameras()
        {
            bool desk = Mode == "desk", inter = Mode == "interior";
            if (DeskScene != null && DeskScene.gameObject.activeSelf != desk) DeskScene.gameObject.SetActive(desk);
            Dcam.enabled = desk;
            if (Cam != null) { Cam.enabled = !desk && !inter; Cam.cullingMask &= ~DeskMask; }
            if (Icam != null) { Icam.enabled = inter && Interior != null; Icam.cullingMask &= ~DeskMask; }
            if (!desk) Desk_Ambient(false);
        }

        static void Frame(float rawDt)
        {
            double ts = PerfNow();
            if (GFX.fpsCap > 0 && ts - LastFrame < 1000.0 / GFX.fpsCap - 2) return;
            GfxAdapt(ts - (LastFrame != 0 ? LastFrame : ts)); LastFrame = ts;
            double cs = ts / 1000; double delta = Loop_clockPrev < 0 ? 0 : cs - Loop_clockPrev; Loop_clockPrev = cs; Loop_clockT += delta;
            float dt = (float)Math.Min(.05, delta), t = (float)Loop_clockT;
            Loop_Cameras();
            if (Mode == "desk")
            {
                DeskWinT -= dt; bool fresh = DeskWinT <= 0;
                if (fresh) { DeskWinT = 3; Desk_Ambient(false); WorldTick(.016f, t); RenderWindow(); }   // dünya ortam ışığı güncel değerleriyle çizilsin
                if (fresh || ++DeskShN % 8 == 0) { }   // R.shadowMap.needsUpdate: Unity gölgeleri her karede günceller
                ViewOffset(Dcam); DeskFrame(dt, t); PlaceMarks();
            }
            else if (Mode != "interior")
            {
                ViewOffset(Cam); StepFly(); Ctl.Update(); WorldTick(dt, t); PlaceMarks();
            }
            else if (Interior != null)
            {
                ViewOffset(Icam); Ictl.Update();
                var fx = Interior.fx; if (fx != null) for (int i = 0; i < fx.Count; i++) fx[i](dt, t);
                var sc = Interior.scene;
                for (int i = 0; i < sc.childCount; i++)
                {
                    var o = sc.GetChild(i); var u = o.GetComponent<UData>();
                    if (u != null && u.hop > 0) { u.hop = Math.Max(0, u.hop - dt * 2.5f); var lp = o.localPosition; lp.y = Mathf.Sin((1 - u.hop) * Mathf.PI) * .5f; o.localPosition = lp; }
                }
            }
            if (!SayHidden) PlaceBubble();
        }

        static void Tick()
        {
            double n = Now();
            for (int i = LiveBar.All.Count - 1; i >= 0; i--) { var b = LiveBar.All[i]; if (b != null && b.fill != null) UI.SetBar(b.fill, (float)(Math.Min(100, (n - b.s) / (b.e - b.s) * 100) / 100)); }
            for (int i = LiveLeft.All.Count - 1; i >= 0; i--)
            {
                var l = LiveLeft.All[i]; if (l == null || l.txt == null) continue;
                int sec = Math.Max(0, CeilI((l.left - n) / 1000)); l.txt.text = l.pre + " " + (l.@short ? SecShort(sec) : Secs(sec));
            }
            RenderStrip(); if (S.consent != null) { TutStart(); TutTick(); }
            UpdateWeather(); ApplySeason(); if (CheckDaily()) ShowLogin(); EnsureContests();
            if (EnsureContracts() && View != null && View.loc == "han") RenderSheet();
            if (S.weather.k == "rain" || S.weather.k == "storm")
            {
                foreach (var f in S.fields) if (f.job != null && Now() < f.job.e) f.job.e -= 150;
                Unit se; if (S.units.TryGetValue("sera", out se) && se != null && se.job != null && Now() < se.job.e) se.job.e -= 150;
            }
            if (++AutoTick % 4 == 0 && Automate()) { Save(); if (View != null) View.sig = ""; }
            NotifyReady(); if (DeskSigNow() != DeskSig) BuildDesk();
            if (!Int_BodyHas("photo")) FarmerSay(); else SayHide();
            SyncWorld(false);
            if (View != null && SheetSig() != View.sig && !(Race != null && Race.running)) RenderSheet();
            if (View != null && View.k == "loc" && View.loc == "pazar" && Now() - S.market.at >= 180000) { RefreshMarket(); RenderSheet(); }
            if (Interior != null && IntSig(Interior.key) != Interior.sig) Interior.refresh();
            if (QueueAll() > 0) Commit(); Rescue(); FunTick(); AutoSellTick(); BatchCheck(); RenderHUD();
        }

        static bool Loop_frameOn;
        public static void Boot(State data)
        {
            S = Migrate(data != null ? data : (Load() ?? Fresh()));
            double away = S.lastSeen != 0 ? DateNow() - S.lastSeen : 0;
            RefreshMarket(); EnsureOrders(); UpdateWeather(); ApplySeason(); CheckDaily(); EnsureContests(); EnsureContracts(); Automate();
            try { MakeThumbs(); } catch (Exception e) { Debug.LogWarning(e); }
            Ctl.enabled = false; BuildDesk(); Resize(); SyncWorld(true);
            BatchWatch(SCENE, c => c == Farmer || c == Dog || c == LampPost || c == RegG || c == RoadG || (c.GetComponent<UData>() != null && c.GetComponent<UData>().loc != null), 3000);
            RenderHUD(); SetModeUI();
            if (!Loop_frameOn) { Loop_frameOn = true; Frame(0); OnFrame += Frame; SetInterval(Tick, 500); SetInterval(Save, 5000); }
            // Açılış perdesi (#boot) yavaşça kaybolur
            var bt = Loop_Find(UI.Root, "boot");
            if (bt != null)
            {
                var cg = bt.gameObject.GetOrAdd<CanvasGroup>();
                UI.Tween(.5f, k => { if (cg) cg.alpha = 1 - k; });
                SetTimeout(() => { if (bt) UnityEngine.Object.Destroy(bt.gameObject); }, 550);
            }
            Booted = true; AwaySummary(away);
            if (S.consent != null) ShowLogin(); else AskConsent();
            SetTimeout(FriendGifts, 1500); SyncCloud(true); Track("session_start", new Dictionary<string, object> { { "gfx", GFX.mode }, { "pr", GFX.pr } });
            if (Get(S.stats, "welcomed") == 0) { S.stats["welcomed"] = 1; SetTimeout(() => { if (TutStep() < 0 && S.tut != null) UI.Toast("Masadaki haritaya dokun, çiftliğe çık!"); }, 900); }
            Save();
        }
        static Transform Loop_Find(Transform t, string name)
        {
            if (t == null) return null; if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { var r = Loop_Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        // location.reload() karşılığı (kurtarma koduyla geri yükleme sonrası): kaydı baştan okuyup oyunu masadan yeniden kurar
        public static void Reload()
        {
            try { PLAYER = DocIdOf(Account()).Substring(0, 16); } catch (Exception) { }
            S = Migrate(Load() ?? Fresh());
            View = null;
            Mode = "desk"; Focus = null; DropInterior(); Ctl.enabled = false; Ictl.enabled = false; HubSaved = null;
            RefreshMarket(); EnsureOrders(); UpdateWeather(); ApplySeason(); CheckDaily(); EnsureContests(); EnsureContracts(); Automate();
            BuildDesk(); SyncWorld(true); RenderHUD(); SetModeUI(); RenderSheet(); SyncMarks();
            if (S.consent != null) ShowLogin(); else AskConsent();
            Save();
        }

        static IEnumerator Loop_StartGame()
        {
            // Yazı tipleri (document.fonts) Unity'de yerleşik; arayüzün ilk düzeni için bir kare beklenir
            yield return null;
            bool done = false;
            try { PLATFORM.Init(() => done = true); } catch (Exception e) { Debug.LogException(e); done = true; }
            float t0 = Time.realtimeSinceStartup;
            while (!done && Time.realtimeSinceStartup - t0 < 2.5f) yield return null;
            try { Boot(null); }
            catch (Exception e)
            {
                // Açılış hatası: perde dokunmayı kilitlemesin, oyuncu en azından masaya erişsin
                Debug.LogException(e); ReportError(e);
                var bt = Loop_Find(UI.Root, "boot"); if (bt != null) UnityEngine.Object.Destroy(bt.gameObject);
                UI.Toast("Açılışta bir sorun oldu; oyun yine de açıldı.");
            }
        }

        static void Init_33()
        {
            UI.OnResize += Resize;
            Host.Run(Loop_StartGame());
        }
    }
}
