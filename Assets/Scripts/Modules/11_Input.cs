// ================= GİRDİ (11-input.js) =================
// Dokunma + fare: seçim ışını (kendi üçgen testimiz; Collider gerekmez), dokun/sürükle ayrımı (10 px),
// tarlada sürükleyerek ekme/toplama, masa paralaksı, üzerine gelme. Yörünge denetimi (pan/döndür/kıstır) Ctl/Ictl.Poll ile.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public class In_Hit { public int? plot; public bool plotBuy, farmer; public string unit, loc, region, open, desk, act; public Transform obj; }
    public class In_Down { public float x, y, lx; public bool sw, multi; }
    public class In_Swipe { public string kind; public HashSet<int> done = new HashSet<int>(); public int n; }

    public static partial class Game
    {
        static In_Down down = null; static double hoverT = 0;
        public static In_Swipe Swipe = null;

        // ---- ışın testi ----
        class In_MeshData { public Vector3[] v; public int[] t; }
        static readonly Dictionary<Mesh, In_MeshData> _inMesh = new Dictionary<Mesh, In_MeshData>();
        static bool In_Tri(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0; var e1 = b - a; var e2 = c - a; var p = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-9f) return false; float inv = 1 / det; var s = o - a; float u = Vector3.Dot(s, p) * inv; if (u < 0 || u > 1) return false;
            var q = Vector3.Cross(s, e1); float v = Vector3.Dot(d, q) * inv; if (v < 0 || u + v > 1) return false;
            t = Vector3.Dot(e2, q) * inv; return t > 0;
        }
        // THREE Raycaster.intersectObjects(list,true) karşılığı: yakından uzağa sıralı isabetler
        static List<(float d, Transform o)> In_Raycast(Ray ray, IEnumerable<Transform> roots)
        {
            var hits = new List<(float, Transform)>();
            foreach (var root in roots)
            {
                if (root == null || !root.gameObject.activeInHierarchy) continue;
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!r.enabled) continue; float bd; if (!r.bounds.IntersectRay(ray, out bd)) continue;
                    var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue; var mesh = mf.sharedMesh;
                    In_MeshData md;
                    if (!_inMesh.TryGetValue(mesh, out md))
                    {
                        // yok edilen (birleştirme/yeniden kurma sonrası) ağların kopyalarını bırak
                        if (_inMesh.Count > 256) foreach (var dead in _inMesh.Keys.Where(km => km == null).ToList()) _inMesh.Remove(dead);
                        md = new In_MeshData { v = mesh.vertices, t = mesh.triangles }; _inMesh[mesh] = md;
                    }
                    if (md.v == null || md.t == null) continue;
                    var w2l = r.transform.worldToLocalMatrix; var l2w = r.transform.localToWorldMatrix;
                    var lo = w2l.MultiplyPoint3x4(ray.origin); var ld = w2l.MultiplyVector(ray.direction);
                    float best = float.PositiveInfinity;
                    for (int i = 0; i + 2 < md.t.Length; i += 3)
                    {
                        float t; if (In_Tri(lo, ld, md.v[md.t[i]], md.v[md.t[i + 1]], md.v[md.t[i + 2]], out t) && t < best) best = t;
                    }
                    if (float.IsPositiveInfinity(best)) continue;
                    var wp = l2w.MultiplyPoint3x4(lo + ld * best);
                    hits.Add((Vector3.Distance(ray.origin, wp), r.transform));
                }
            }
            hits.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return hits;
        }
        // sp: ekran pikseli (Unity, sol alttan)
        static In_Hit Pick(Vector2 sp)
        {
            if (Mode == "desk")
            {
                if (Dcam == null) return null; var hits = In_Raycast(Dcam.ScreenPointToRay(sp), DESKP); if (hits.Count == 0) return null;
                var o = hits[0].o; while (o != null && string.IsNullOrEmpty(In_DeskOf(o))) o = o.parent;
                return o != null ? new In_Hit { desk = o.GetComponent<DeskTag>().desk } : null;
            }
            if (Mode != "interior")
            {
                var list = LocGroups.Values.Concat(new[] { Farmer, Dog }).ToList(); if (RegG != null) list.Add(RegG); if (LampPost != null && LampPost.gameObject.activeSelf) list.Add(LampPost);
                var hits = In_Raycast(Cam.ScreenPointToRay(sp), list); if (hits.Count == 0) return null;
                var o = hits[0].o; var res = new In_Hit();
                while (o != null)
                {
                    var u = W_UDr(o);
                    if (u != null)
                    {
                        if (u.hasPlot && res.plot == null) res.plot = u.plot; if (u.plotBuy) res.plotBuy = true; if (!string.IsNullOrEmpty(u.unit) && res.unit == null) res.unit = u.unit;
                        if (!string.IsNullOrEmpty(u.loc)) { res.loc = u.loc; break; }
                        if (!string.IsNullOrEmpty(u.region)) { res.region = u.region; break; }
                        if (u.farmer) { res.farmer = true; break; }
                        if (!string.IsNullOrEmpty(u.open)) { res.open = u.open; break; }
                    }
                    o = o.parent;
                }
                return res;
            }
            if (Interior == null) return null;
            var ch = new List<Transform>(); for (int i = 0; i < Interior.scene.childCount; i++) ch.Add(Interior.scene.GetChild(i));
            foreach (var hh in In_Raycast(Icam.ScreenPointToRay(sp), ch)) { var u = W_UDr(hh.o); if (u != null && !string.IsNullOrEmpty(u.act)) return new In_Hit { act = u.act, obj = hh.o }; }
            return null;
        }

        // ---- Sürükleyerek ekme / toplama ----
        // Tarlalar'dayken parmağı parsellerin üzerinde gezdirince ilk parselin durumuna göre hepsi ekilir ya da toplanır.
        static string SwipeKind(int i) { var f = i < S.fields.Count ? S.fields[i] : null; if (f == null) return null; if (f.job == null) return "plant"; if (Now() >= f.job.e) return "harvest"; return null; }
        static void SwipeApply(int i, float x, float y)
        {
            if (Swipe == null || Swipe.done.Contains(i)) return; var f = i < S.fields.Count ? S.fields[i] : null; if (f == null) return; Swipe.done.Add(i);
            if (Swipe.kind == "plant" && f.job == null) { if (PlantPlot(i, S.lastCrop)) { Swipe.n++; FloatXY(x, y, ITEMS[S.lastCrop].e); PLATFORM.hap(); } }
            else if (Swipe.kind == "harvest" && f.job != null && Now() >= f.job.e) { if (HarvestPlot(i, x, y)) Swipe.n++; }
        }
        static void SwipeEnd()
        {
            if (Swipe == null) return; var s = Swipe; Swipe = null; Ctl.enabled = Mode != "interior" && Mode != "desk";
            if (s.n != 0) { if (s.kind == "harvest" && s.n >= 3) Celebrate(s.n >= 6 ? "Bereket yağdı!" : "Bereketli!"); Commit(); }
        }

        static string In_DeskOf(Transform o) { var d = o.GetComponent<DeskTag>(); return d != null ? d.desk : null; }
        static void Init_11() { OnFrame += In_Frame; }

        static Vector2 In_LastScreen;
        static void In_Frame(float dt)
        {
            if (Cam == null || S == null || !Booted) return;   // açılış (Boot) bitmeden dokunma işlenmez
            // birincil işaretçi (ilk parmak ya da fare sol tuşu)
            bool touch = Input.touchCount > 0; bool pressed; Vector2 sp; int fid = -1; bool beganNow = false, endedNow = false;
            if (touch)
            {
                var t0 = Input.GetTouch(0); sp = t0.position; fid = t0.fingerId;
                beganNow = t0.phase == TouchPhase.Began; endedNow = t0.phase == TouchPhase.Ended || t0.phase == TouchPhase.Canceled; pressed = !endedNow;
            }
            else
            {
                sp = Input.mousePosition; pressed = Input.GetMouseButton(0);
                beganNow = Input.GetMouseButtonDown(0); endedNow = Input.GetMouseButtonUp(0);
            }
            bool overUI = UI.PointerOverUI(fid) || UI.AnyModal;
            // yörünge denetimleri (pointerdown arayüz üzerindeyse başlamaz)
            bool canStart = !overUI;
            if (Ctl != null) Ctl.Poll(canStart && Mode != "interior" && Mode != "desk");
            if (Ictl != null) Ictl.Poll(canStart && Mode == "interior");
            var css = UI.ScreenToCss(sp); float x = css.x, y = css.y;
            if (touch && Input.touchCount > 1 && down != null) down.multi = true;

            // pointerdown (tuvalde)
            if (beganNow && !overUI)
            {
                down = new In_Down { x = x, y = y, lx = x };
                if (Mode == "focus" && Focus == "tarla")
                {
                    var h = Pick(sp);
                    if (h != null && h.plot != null) { var k = SwipeKind(h.plot.Value); if (k != null) { Swipe = new In_Swipe { kind = k }; Ctl.enabled = false; SwipeApply(h.plot.Value, x, y - 24); down.sw = true; } }
                }
            }
            // pointermove
            bool moved = (sp - In_LastScreen).sqrMagnitude > .01f || beganNow;
            if (moved && (pressed || !touch) && !endedNow && (down != null || !overUI)) In_Move(sp, x, y);
            In_LastScreen = sp;
            // pointerup (pencere: swipeEnd; tuval: dokunma)
            if (endedNow)
            {
                SwipeEnd();
                if (down != null)
                {
                    var dn = down; down = null;
                    bool mv = Mathf.Sqrt((x - dn.x) * (x - dn.x) + (y - dn.y) * (y - dn.y)) > 10;
                    if (!mv && !dn.sw && !dn.multi) In_Tap(sp, x, y - 24);
                }
            }
        }
        static void In_Move(Vector2 sp, float cx, float cy)
        {
            if (Swipe != null) { var h = Pick(sp); if (h != null && h.plot != null) SwipeApply(h.plot.Value, cx, cy - 24); return; }
            if (Mode == "desk")
            {
                DPar.x = (cx / Mathf.Max(1, UI.W) - .5f) * 2; DPar.y = -(cy / Mathf.Max(1, UI.H) - .5f) * 2;
                if (down != null) { double rg = DeskPanRange(); DcamX = (float)Math.Max(-rg, Math.Min(rg, DcamX - (cx - down.lx) * .012)); down.lx = cx; return; }
            }
            if (down != null || Now() - hoverT < 60) return; hoverT = Now();
            var hh = Pick(sp);
            HoverLoc = Mode != "interior" && hh != null && hh.loc != null ? hh.loc : null;
            HoverDesk = Mode == "desk" && hh != null && hh.desk != null ? hh.desk : null;
        }
        static void In_Tap(Vector2 sp, float x, float y)
        {
            var h = Pick(sp);
            if (Mode == "desk") { if (h != null && h.desk != null) { Sfx("click"); DeskAction(h.desk); } return; }
            if (Mode == "interior")
            {
                if (h == null) return;
                if (h.act == "collect" && Interior.key != "kulube") CollectUnit(Interior.key, x, y);
                else if (h.act == "pet")
                {
                    var o = h.obj; while (o.parent != null && o.parent != Interior.scene) o = o.parent; W_UD(o).hop = 1;
                    Sfx(GetOr(UNIT_SFX, Interior.key) ?? "pop");
                    var pl = new List<Transform>(); for (int i = 0; i < Interior.scene.childCount; i++) { var c = Interior.scene.GetChild(i); if (W_UDr(c)?.act == "pet") pl.Add(c); }
                    PetAnimal(Interior.key, Math.Max(0, pl.IndexOf(o)), x, y);
                }
                return;
            }
            if (h == null) return;
            if (h.farmer) { FWave = 1.6; FloatXY(x, y, new[] { "Merhaba!", "Hayırlı işler!", "Hav hav!", "Bereketli olsun!" }[(int)Math.Floor(Rnd() * 4)]); return; }
            if (h.open != null) { OpenView(h.open); return; }
            if (h.region != null) { OpenRegion(h.region); return; }
            if (h.loc == null) return;
            if (Mode == "focus" && h.loc == Focus)
            {
                if (h.plot != null) { PlotTap(h.plot.Value, x, y); return; }
                if (h.plotBuy) { BuyPlot(); return; }
                if (h.unit != null)
                {
                    var s = GetOr(S.units, h.unit);
                    if (s != null && s.job != null && Now() >= s.job.e) { CollectUnit(h.unit, x, y); return; }
                    if (s != null && INSIDE_UNITS.ContainsKey(h.unit)) { EnterInterior(h.unit); return; }
                }
                if (Focus == "ev") { EnterInterior("kulube"); return; }
                return;
            }
            GoLocation(h.loc);
        }
    }
}
