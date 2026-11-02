// ================= TABELALAR VE BALONCUKLAR (08-markers.js) =================
// HTML #marks katmanı → UI.Marks üzerindeki uGUI öğeleri; her karede Camera.WorldToViewportPoint ile konumlanır.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // locStatus sonucu: c = sınıf (ok/new/idle/run/lock/""), t = metin, n = sayı (0 = yok)
    public class Mk_Status { public string c = "", t = ""; public int n; }

    // Hafif yukarı-aşağı sallanma (CSS @keyframes bob 1.1s)
    public class Mk_Bob : MonoBehaviour
    {
        RectTransform rt; Vector2 basePos; float t0;
        void Awake() { rt = (RectTransform)transform; basePos = rt.anchoredPosition; t0 = Time.unscaledTime; }
        void Update() { float k = ((Time.unscaledTime - t0) % 1.1f) / 1.1f; rt.anchoredPosition = basePos + new Vector2(0, 4 * (.5f - .5f * Mathf.Cos(k * Mathf.PI * 2))); }
    }

    public class Mk_Mark
    {
        public RectTransform el;          // .mk (sol üstten translate)
        public RectTransform child;       // el.firstElementChild
        public string sig = "";
        public bool hasPos; public Vector3 pos;   // Unity dünya konumu
        public string tag; public bool below, side, dk;
        public float w, h;
        public Image fill;                // .prog --p
        public Image hotBg; public Color hotBase, hotOn; public bool hotScale;
    }

    public static partial class Game
    {
        public static RectTransform Marks => UI.Marks;
        public static readonly Dictionary<string, Mk_Mark> MK = new Dictionary<string, Mk_Mark>();

        public static double MarkH(string u)
        {
            var k = LocOf(u); double h;
            switch (u) { case "ahir": h = 3.8; break; case "degirmen": h = 4.4; break; case "silo": h = 4; break; case "sera": h = 3; break; case "hara": h = 2.4; break; case "bahce": h = 2.6; break; case "kovan": h = 2; break; case "kumes": h = 2.6; break; default: h = 2.6; break; }
            return h * UnitScale(k);
        }
        public static Mk_Status LocStatus(string k)
        {
            var L = LOC[k]; int ready = 0, run = 0, idle = 0, unbuilt = 0, canBuild = 0; double minLeft = double.PositiveInfinity;
            if (L.kind == "market") return new Mk_Status { c = "", t = "Al · Sat" };
            if (L.kind == "fair")
            {
                EnsureContests();
                int n = S.contests.list.Count(id => !S.contests.done.ContainsKey(id) && CONTESTS.Find(x => x.id == id).need.All(kv => Has(kv.Key) >= kv.Value));
                return n != 0 ? new Mk_Status { c = "ok", t = n + " yarışmaya katılabilirsin", n = n } : new Mk_Status();
            }
            if (L.kind == "orders") { int r = ReadyOrders(); return r != 0 ? new Mk_Status { c = "ok", t = r + " sipariş teslime hazır", n = r } : new Mk_Status(); }
            if (L.kind == "house") { var nx = S.house < HOUSE.Count ? HOUSE[S.house] : null; return nx != null && S.lv >= nx.lvl && S.coins >= nx.cost ? new Mk_Status { c = "new", t = "Büyütülebilir" } : new Mk_Status { c = "", t = HOUSE[S.house - 1].n }; }
            if (L.kind == "fields") foreach (var f in S.fields) { if (f.job == null) idle++; else if (Now() >= f.job.e) ready++; else { run++; minLeft = Math.Min(minLeft, f.job.e - Now()); } }
            if (L.units != null) foreach (var u in L.units)
                {
                    var s = GetOr(S.units, u);
                    if (s == null) { unbuilt++; if (S.lv >= B[u].lvl && S.coins >= B[u].cost) canBuild++; continue; }
                    if (B[u].recipes == null) continue;
                    if (s.job == null) idle++; else if (Now() >= s.job.e) ready++; else { run++; minLeft = Math.Min(minLeft, s.job.e - Now()); }
                }
            if (ready != 0) return new Mk_Status { c = "ok", t = ready + " hazır", n = ready };
            if (L.units != null && unbuilt == L.units.Count && string.IsNullOrEmpty(L.kind))
            {
                var u = L.units[0];
                return S.lv < B[u].lvl ? new Mk_Status { c = "lock", t = "Seviye " + B[u].lvl } : S.coins >= B[u].cost ? new Mk_Status { c = "new", t = "İnşa et ₺" + Fmt(B[u].cost) } : new Mk_Status();
            }
            if (idle != 0) return new Mk_Status { c = "idle", t = idle + " boşta" };
            if (run != 0) return new Mk_Status { c = "run", t = "⏱ " + Secs(CeilI(minLeft / 1000)) };
            if (canBuild != 0) return new Mk_Status { c = "new", t = "Yeni bina kurulabilir" };
            if (k == "silo") return new Mk_Status { c = "", t = "Ambar " + Stored() + "/" + Cap() };
            return new Mk_Status();
        }
        public static List<string> WantMarks()
        {
            var w = new List<string>();
            if (Mode == "desk") { foreach (var k in DeskObjs.Keys) w.Add("D:" + k); return w; }
            if (Mode == "hub") { }
            else if (Mode == "focus")
            {
                if (Focus == null || !LOC.ContainsKey(Focus)) return w; var L = LOC[Focus];
                if (L.kind == "fields") for (int i = 0; i < S.fields.Count; i++) if (S.fields[i].job != null) w.Add("P:" + i);
                if (L.units != null) foreach (var u in L.units) { var s = GetOr(S.units, u); if (s != null && s.job != null) w.Add("U:" + u); }
            }
            return w;
        }
        public static void SyncMarks()
        {
            var w = WantMarks(); var ws = new HashSet<string>(w);
            foreach (var k in MK.Keys.ToList()) if (!ws.Contains(k)) { UI.Kill(MK[k].el); MK.Remove(k); }
            foreach (var k in w) if (!MK.ContainsKey(k))
                {
                    var el = UI.New("mk", Marks); el.anchorMin = el.anchorMax = new Vector2(0, 1); el.pivot = new Vector2(0, 1); el.sizeDelta = Vector2.zero;
                    MK[k] = new Mk_Mark { el = el, sig = "" };
                }
            UpdateMarks();
        }

        // ---- görsel parçalar ----
        static Sprite _mkCircle, _mkTri;
        static Sprite Mk_Circle
        {
            get
            {
                if (_mkCircle != null) return _mkCircle;
                var p = new Paint(64, 64); p.Circle(32, 32, 32, Color.white); var t = p.Apply();
                return _mkCircle = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100);
            }
        }
        static Sprite Mk_Tri   // aşağı bakan üçgen (.tag::after)
        {
            get
            {
                if (_mkTri != null) return _mkTri;
                var p = new Paint(24, 12); p.Poly(new[] { new Vector2(0, 0), new Vector2(24, 0), new Vector2(12, 12) }, Color.white); var t = p.Apply();
                return _mkTri = Sprite.Create(t, new Rect(0, 0, 24, 12), new Vector2(.5f, .5f), 100);
            }
        }
        static void Mk_Clear(Mk_Mark m) { UI.Clear(m.el); m.child = null; m.fill = null; m.hotBg = null; }
        // .tag düğmesi (kind: "" | "lock" | "below dk" | "side dk")
        static void Mk_BuildTag(Mk_Mark m, string cls, string ti, string name, string small, string dotCls, string dotTxt, Action onClick)
        {
            Mk_Clear(m);
            bool lk = cls.Contains("lock"), dk = cls.Contains("dk"), below = cls.Contains("below"), side = cls.Contains("side"), ph = UI.Phone && !dk;
            m.dk = dk; m.below = below; m.side = side;
            Color bg = lk ? UI.Hex("#4a3a2a") : dk ? UI.Hex("#fffaf0") : UI.C.paper;
            float hgt = dk ? 24 : ph ? 25 : 28;
            var btn = UI.Box(m.el, bg, hgt / 2, 2, UI.C.ink, new Vector2(2, 2), "tag");
            btn.anchorMin = btn.anchorMax = new Vector2(0, 1);
            if (below) { btn.pivot = new Vector2(.5f, 1); btn.anchoredPosition = new Vector2(0, -7); }
            else if (side) { btn.pivot = new Vector2(0, .5f); btn.anchoredPosition = new Vector2(8, 0); }
            else { btn.pivot = new Vector2(.5f, 0); btn.anchoredPosition = new Vector2(0, 8); }
            var hl = btn.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleLeft; hl.spacing = dk ? 5 : 6; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            hl.padding = lk ? UI.Pad(3, 11, 3, 4) : UI.Pad(3, dk ? 9 : ph ? 9 : 11, 0, 0);
            var fit = UI.FitBoth(btn); UI.Size(btn, -1, -1, -1, -1, -1, hgt);
            float tis = dk || ph ? 19 : 22;
            var tib = UI.Box(btn, lk ? UI.Hex("#6a5440") : Color.white, tis / 2, 1.5f, UI.C.ink, null, "ti"); UI.Size(tib, tis, tis);
            var tt = UI.Txt(tib, ti, dk || ph ? 11 : 12.5f, lk ? Color.white : UI.C.text, false, TextAnchor.MiddleCenter, false); UI.Stretch(tt.rectTransform);
            Color bc = lk ? UI.Hex("#f3e6c4") : dk ? UI.Hex("#2a1a10") : UI.Hex("#3a2414");
            if (small != null)
            {
                var col = UI.VStack(btn, 0, null, TextAnchor.MiddleLeft, "txt"); col.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
                UI.Txt(col, UI.Esc(name), 12.5f, bc, false, TextAnchor.MiddleLeft, false, true);
                UI.Txt(col, small, 10.5f, UI.Hex("#f2c96a"), true, TextAnchor.MiddleLeft, false);
            }
            else UI.Txt(btn, UI.Esc(name), dk ? 12 : ph ? 11 : 12.5f, bc, dk, TextAnchor.MiddleLeft, false, !dk);
            // ok
            var tri = UI.Fill(btn, UI.C.ink, 0, "arrow"); tri.sprite = Mk_Tri; tri.raycastTarget = false; UI.IgnoreLayout(tri);
            var trt = tri.rectTransform; trt.sizeDelta = new Vector2(12, 6);
            if (below) { trt.anchorMin = trt.anchorMax = new Vector2(.5f, 1); trt.pivot = new Vector2(.5f, 0); trt.anchoredPosition = new Vector2(0, 1); trt.localRotation = Quaternion.Euler(0, 0, 180); }
            else if (side) { trt.anchorMin = trt.anchorMax = new Vector2(0, .5f); trt.pivot = new Vector2(.5f, .5f); trt.sizeDelta = new Vector2(12, 6); trt.anchoredPosition = new Vector2(-4, 0); trt.localRotation = Quaternion.Euler(0, 0, -90); }
            else { trt.anchorMin = trt.anchorMax = new Vector2(.5f, 0); trt.pivot = new Vector2(.5f, 1); trt.anchoredPosition = new Vector2(0, -1); }
            // durum noktası (i.s)
            if (dotCls != null)
            {
                Color dbg = dotCls == "ok" ? UI.C.leaf : dotCls == "new" ? UI.C.honey : dotCls == "warn" ? UI.C.brick : UI.C.card;
                Color dfg = dotCls == "ok" || dotCls == "warn" ? Color.white : UI.C.text;
                float ds = dk ? 18 : 20;
                var dot = UI.Box(btn, dbg, ds / 2, 2, UI.C.ink, null, "s"); UI.IgnoreLayout(dot);
                dot.anchorMin = dot.anchorMax = new Vector2(0, 1); dot.pivot = new Vector2(0, 1); dot.anchoredPosition = new Vector2(-9, dk ? 9 : 11);
                var dtx = UI.Txt(dot, dotTxt, dk || dotCls == "idle" ? 9.5f : 10.5f, dfg, true, TextAnchor.MiddleCenter, false);
                dot.sizeDelta = new Vector2(Mathf.Max(ds, dtx.preferredWidth + 6), ds); UI.Stretch(dtx.rectTransform);
                if (dotCls == "ok") dot.gameObject.AddComponent<Mk_Bob>();
            }
            UI.Clickable(btn, onClick);
            m.child = btn; m.w = 0;
            var inner = btn.Find("bg"); m.hotBg = inner != null ? inner.GetComponent<Image>() : btn.GetComponent<Image>();
            m.hotBase = bg; m.hotOn = lk ? bg : dk ? UI.Hex("#ffe7a8") : UI.Hex("#fff8e3"); m.hotScale = !dk && !below && !side;
        }
        static void Mk_SetHot(Mk_Mark m, bool hot)
        {
            if (m.child == null) return;
            if (m.hotBg != null) m.hotBg.color = hot ? m.hotOn : m.hotBase;
            m.child.localScale = Vector3.one * (hot && m.hotScale ? 1.1f : 1);
        }
        public static void Bubble(Mk_Mark m, string em, double pg, string act, string id)
        {
            if (pg >= 1)
            {
                var h = "ready|" + act + "|" + id + "|" + em;
                if (m.sig != h)
                {
                    m.sig = h; Mk_Clear(m);
                    var b = UI.Box(m.el, UI.Hex("#fffaf0"), 20, 2.5f, UI.C.ink, new Vector2(2, 3), "bub");
                    b.anchorMin = b.anchorMax = new Vector2(0, 1); b.pivot = new Vector2(.5f, 0); b.anchoredPosition = Vector2.zero; b.sizeDelta = new Vector2(40, 40);
                    var t = UI.Txt(b, em, 20, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform);
                    var ck = UI.Box(b, UI.C.leaf, 8.5f, 2, UI.C.ink, null, "ok"); ck.anchorMin = ck.anchorMax = new Vector2(1, 1); ck.pivot = new Vector2(1, 1); ck.anchoredPosition = new Vector2(6, 6); ck.sizeDelta = new Vector2(17, 17);
                    var ct = UI.Txt(ck, "✓", 10, Color.white, true, TextAnchor.MiddleCenter, false); UI.Stretch(ct.rectTransform);
                    b.gameObject.AddComponent<Mk_Bob>();
                    var bb = b;
                    UI.Clickable(b, () =>
                    {
                        var r = Mk_CssRect(bb);
                        Act(act, D("id", id, "cx", (r.x + r.width / 2).ToString(CultureInfo.InvariantCulture), "cy", r.y.ToString(CultureInfo.InvariantCulture)));
                    });
                    m.child = b;
                }
            }
            else if (m.sig == "p" + em && m.fill != null) m.fill.fillAmount = (float)pg;
            else
            {
                m.sig = "p" + em; Mk_Clear(m);
                var p = UI.Box(m.el, UI.A(UI.Hex("#fffaf0"), .8f), 15, 2.5f, UI.C.ink, null, "prog");
                p.anchorMin = p.anchorMax = new Vector2(0, 1); p.pivot = new Vector2(.5f, 0); p.anchoredPosition = Vector2.zero; p.sizeDelta = new Vector2(30, 30);
                var host = p.Find("bg") ?? p;
                var f = UI.Fill(host, UI.C.honey, 0, "fill"); UI.Stretch(f.rectTransform); f.sprite = Mk_Circle; f.type = Image.Type.Filled; f.fillMethod = Image.FillMethod.Radial360;
                f.fillOrigin = (int)Image.Origin360.Top; f.fillClockwise = true; f.fillAmount = (float)pg; f.raycastTarget = false;
                var sp = UI.Fill(p, UI.Hex("#fffaf0"), 0, "span"); sp.sprite = Mk_Circle; sp.raycastTarget = false; var srt = sp.rectTransform;
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(.5f, .5f); srt.sizeDelta = new Vector2(20, 20); srt.anchoredPosition = Vector2.zero;
                var t = UI.Txt(srt, em, 11, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform);
                m.fill = f; m.child = p;
            }
        }
        // Öğenin ekrandaki dikdörtgeni (CSS px, sol üstten)
        internal static Rect Mk_CssRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            float x0 = Mathf.Min(c[0].x, c[2].x), x1 = Mathf.Max(c[0].x, c[2].x), y0 = Mathf.Min(c[0].y, c[2].y), y1 = Mathf.Max(c[0].y, c[2].y);
            float s = UI.Scale <= 0 ? 1 : UI.Scale;
            return new Rect(x0 / s, (Screen.height - y1) / s, (x1 - x0) / s, (y1 - y0) / s);
        }
        public static void UpdateMarks()
        {
            foreach (var id in MK.Keys.ToList())
            {
                var m = MK[id]; int ci = id.IndexOf(':'); string t = id.Substring(0, ci), k = id.Substring(ci + 1);
                if (t == "L")
                {
                    var L = LOC[k]; var st = LocStatus(k); m.pos = V(L.p[0] + L.s[0], .5, L.p[2] + L.s[1]); m.hasPos = true; m.tag = k;
                    string dc = st.c == "ok" ? "ok" : st.c == "new" ? "new" : st.c == "idle" ? "idle" : null, dt = st.c == "ok" ? (st.n != 0 ? st.n.ToString() : "✓") : st.c == "new" ? "🔨" : st.c == "idle" ? "💤" : null;
                    var h = "L|" + k + "|" + L.e + "|" + L.n + "|" + dc + "|" + dt;
                    if (h != m.sig) { m.sig = h; var kk = k; Mk_BuildTag(m, "", L.e, L.n, null, dc, dt, () => { PLATFORM.hap(); GoLocation(kk); }); }
                }
                else if (t == "R")
                {
                    var d = REG[k]; double x1 = d.rect[0], z1 = d.rect[1], x2 = d.rect[2], z2 = d.rect[3]; m.pos = V((x1 + x2) / 2, 3, (z1 + z2) / 2); m.hasPos = true; m.tag = "R";
                    var sm = "₺" + Fmt(RegCost(k)) + " · Sv " + d.lvl; var h = "R|" + k + "|" + sm;
                    if (h != m.sig) { m.sig = h; var kk = k; Mk_BuildTag(m, "lock", "🔒", d.n, sm, null, null, () => OpenRegion(kk)); }
                }
                else if (t == "D")
                {
                    Transform g; if (!DeskObjs.TryGetValue(k, out g) || g == null) continue;
                    object[] an = DANCH.ContainsKey(k) ? DANCH[k] : new object[] { 0.0, 0.0, .4 };
                    var v = g.TransformPoint(V(Convert.ToDouble(an[0]), Convert.ToDouble(an[1]), Convert.ToDouble(an[2]))); m.pos = v; m.hasPos = true; m.tag = k; m.below = true;
                    bool sd = an.Length > 3 && (an[3] as string) == "side"; var st = DeskStatus(k); var nm = DNAMES[k];
                    string dc = st != null ? "" + st.c : null, dtx = st != null ? "" + st.n : null;
                    var h = "D|" + k + "|" + sd + "|" + dc + "|" + dtx;
                    if (h != m.sig) { m.sig = h; var kk = k; Mk_BuildTag(m, (sd ? "side" : "below") + " dk", nm[0], nm[1], null, dc, dtx, () => { Sfx("click"); DeskAction(kk); }); m.below = true; }
                }
                else if (t == "P")
                {
                    int i = int.Parse(k); var f = i < S.fields.Count ? S.fields[i] : null; if (f == null || f.job == null) continue;
                    m.pos = V3T(PlotWorld(i, 1.7)); m.hasPos = true; Bubble(m, ITEMS[f.job.r].e, Prog(f.job), "harvestP", k);
                }
                else if (t == "U")
                {
                    var s = GetOr(S.units, k); if (s == null || s.job == null) continue;
                    m.pos = V3T(UnitWorld(k, MarkH(k))); m.hasPos = true; Bubble(m, ITEMS[UnitRecipe(k).@out.Keys.First()].e, Prog(s.job), "collectU", k);
                }
            }
        }
        internal static Vector3 V3T(Vector3 three) => V(three.x, three.y, three.z);
        public static void PlaceMarks()
        {
            float W = UI.W, H = UI.H; var tags = new List<(Mk_Mark m, float x, float y)>();
            bool photo = Int_BodyHas("photo"); UI.Show(Marks, !photo);
            var camr = Mode == "desk" ? Dcam : Cam;
            foreach (var k in MK.Keys)
            {
                var m = MK[k]; if (!m.hasPos || camr == null) continue;
                var vp = camr.WorldToViewportPoint(m.pos);
                float nx = vp.x * 2 - 1, ny = vp.y * 2 - 1;
                bool vis = vp.z > 0 && vp.z < camr.farClipPlane && Mode != "interior" && Mathf.Abs(nx) < 1.15f && Mathf.Abs(ny) < 1.15f;
                UI.Show(m.el, vis); if (!vis) continue;
                float x = vp.x * W, y = (1 - vp.y) * H;
                if (m.tag != null)
                {
                    if (m.w == 0 && m.child != null) { LayoutRebuilder.ForceRebuildLayoutImmediate(m.child); m.w = m.child.rect.width; m.h = m.child.rect.height; }
                    tags.Add((m, x, y));
                    Mk_SetHot(m, (Mode == "desk" ? HoverDesk : HoverLoc) == m.tag);
                    continue;
                }
                m.el.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(y));
            }
            tags.Sort((a, b) => b.y.CompareTo(a.y)); var placed = new List<(float x, float y, float w)>();
            if (tags.Any(t => t.m.below)) tags.Sort((a, b) => a.y.CompareTo(b.y));
            foreach (var t in tags)
            {
                float y = t.y; float dn = t.m.below ? 1 : -1;
                for (int it = 0; it < 6; it++)
                {
                    int hi = placed.FindIndex(p => Mathf.Abs(p.x - t.x) < (p.w + t.m.w) / 2 + 4 && Mathf.Abs(p.y - y) < t.m.h + 6);
                    if (hi < 0) break; y = placed[hi].y + dn * (t.m.h + 6);
                }
                placed.Add((t.x, y, t.m.w)); t.m.el.anchoredPosition = new Vector2(Mathf.Round(t.x), -Mathf.Round(y));
            }
        }
        // p: THREE koordinatı; dönen değer CSS px (sol üst)
        public static Vector2 ScreenOfW(Vector3 p)
        {
            var vp = Cam.WorldToViewportPoint(V(p.x, p.y, p.z));
            return new Vector2(vp.x * UI.W, (1 - vp.y) * UI.H - 30);
        }
    }
}
