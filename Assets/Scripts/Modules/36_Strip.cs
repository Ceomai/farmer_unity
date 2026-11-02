// ================= HARİTA: ALT KART ŞERİDİ (36-strip.js) =================
// Haritadayken bölümler ekranın altında büyük, resimli kartlar olarak yana kayar.
// Kartın üstüne dokununca o bölüme gidilir; karttaki yeşil düğme ise hiçbir yere gitmeden işi yapar (topla / yeniden başlat).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciftlik
{
    public class StripOwn { public string k; public LocDef L; public string c, t; public int n; public int pri; }
    public class StripLock { public string r; public RegionDef d; }
    public class QuickInfoR { public int ready, idle, brk; public double next = double.PositiveInfinity; }
    public class QuickRes { public int got, started, renewed; public Dictionary<string, int> @out = new Dictionary<string, int>(); public bool full; }

    public static partial class Game
    {
        static RectTransform StripEl => HudView.StripContent;
        public static string StripSig = "";

        static void Init_36()
        {
            HudView.Ensure();
            // const qpop=document.createElement('div') ... hidden
            Qpop = Css.Box(UI.SheetLayer, UI.C.paper, 18, 2.5f, Css.Ink, new Vector2(3, 5), Css.Ink, "qpop");
            Dom.Tag(Qpop, ".qpop").Hidden = true;
            // uzun basmadan hemen sonraki tıklama karta gitmesin; qpop dışına basınca kapanır
            OnFrame += St_Frame;
        }

        // ---------- stripCards() ----------
        public static void StripCards(out List<StripOwn> own, out List<StripLock> lck)
        {
            own = new List<StripOwn>(); lck = new List<StripLock>();
            var keys = LOC.Keys.ToList();
            foreach (var k in keys)
            {
                var L = LOC[k]; if (!Owned(L.reg)) continue; var st = LocStatus(k);
                own.Add(new StripOwn { k = k, L = L, c = st.c ?? "", t = st.t ?? "", n = st.n, pri = st.c == "ok" ? 0 : st.c == "new" ? 1 : st.c == "idle" ? 2 : 3 });
            }
            own = own.OrderBy(a => a.pri).ThenBy(a => keys.IndexOf(a.k)).ToList();
            foreach (var r in REG.Keys) { var d = REG[r]; if (d.rect != null && !Owned(r)) lck.Add(new StripLock { r = r, d = d }); }
        }
        // Bir bölümde tek dokunuşla yapılabilecek işler: hazır olanlar ve son tarifle yeniden başlatılabilecek boştakiler
        public static QuickInfoR QuickInfo(string k)
        {
            var L = LOC[k]; var q = new QuickInfoR(); var units = L.units ?? new List<string>();
            Action<Job> nx = j => { if (j != null && j.e > Now()) q.next = Math.Min(q.next, j.e); };
            if (L.kind == "fields") foreach (var f in S.fields) nx(f.job);
            foreach (var u in units) { var s = GetOr(S.units, u); if (s != null) nx(s.job); }
            Action<string> wk = u => { if (Get(S.workers, u) != 0 && !WorkOn(u)) q.brk++; };
            if (L.kind == "fields") wk("fields"); foreach (var u in units) wk(u);
            if (L.kind == "fields")
            {
                var c = CropOf(S.lastCrop);
                foreach (var f in S.fields) { if (f.job != null && Now() >= f.job.e) q.ready++; else if (f.job == null && c != null && !CropLocked(c) && S.coins >= c.cost) q.idle++; }
            }
            foreach (var u in units)
            {
                var s = GetOr(S.units, u); if (s == null || B[u].recipes == null) continue;
                if (s.job != null) { if (Now() >= s.job.e) q.ready++; continue; }
                var rid = !string.IsNullOrEmpty(s.last) ? s.last : B[u].recipes[0].id;
                var r = B[u].recipes.Find(x => x.id == rid);
                if (r != null && r.lvl <= S.lv && (r.col == null || Has1(r.col)) && CanPay(r)) q.idle++;
            }
            return q;
        }
        public static QuickRes QuickLoc(string k)
        {
            var L = LOC[k]; var before = new Dictionary<string, int>(S.items); var g0 = AutoGain; var res = new QuickRes(); var units = L.units ?? new List<string>();
            if (L.kind == "fields" && RenewWorker("fields", true)) res.renewed++;
            foreach (var u in units) if (RenewWorker(u, true)) res.renewed++;
            if (L.kind == "fields") for (int i = 0; i < S.fields.Count; i++) { var f = S.fields[i]; if (f.job != null && Now() >= f.job.e) { if (HarvestPlot(i, null, null, true)) res.got++; else res.full = true; } }
            foreach (var u in units) { var s = GetOr(S.units, u); if (s != null && s.job != null && Now() >= s.job.e) { if (CollectUnit(u, null, null, true)) res.got++; else res.full = true; } }
            if (L.kind == "fields") { var c = CropOf(S.lastCrop); for (int i = 0; i < S.fields.Count; i++) { if (S.fields[i].job == null && c != null && PlantPlot(i, S.lastCrop, true)) res.started++; } }
            foreach (var u in units)
            {
                var s = GetOr(S.units, u);
                if (s != null && s.job == null && B[u].recipes != null)
                {
                    if (!QueueNext(u) && !(s.q != null && s.q.Count > 0)) StartUnit(u, !string.IsNullOrEmpty(s.last) ? s.last : B[u].recipes[0].id, true);
                    if (s.job != null) res.started++;
                }
            }
            AutoGain = g0;
            foreach (var kv in S.items) { var d = kv.Value - Get(before, kv.Key); if (d > 0) res.@out[kv.Key] = d; }
            return res;
        }
        // el: tıklanan öğenin orta-üst noktası (CSS px); yoksa ekranın altı
        public static void QuickFeedback(QuickRes res, Vector2? at)
        {
            float x = at.HasValue ? at.Value.x : UI.W / 2, y = at.HasValue ? at.Value.y : UI.H - 140;
            var txt = string.Join("  ", res.@out.Select(kv => "+" + kv.Value + " " + ITEMS[kv.Key].e));
            if (txt != "") { FloatXY(x, y, txt); FlyItems(x, y, res.@out); Sfx("pop"); } else if (res.started > 0) Sfx("plant");
            if (res.started > 0) FloatXY(x, y - (txt != "" ? 34 : 0), "▶ " + res.started + " yeniden başladı");
            if (res.renewed > 0) FloatXY(x, y - 68, "☕ " + res.renewed + " işçi işe döndü");
            if (res.full) Toast("Ambar dolu! Pazarda sat ya da siloyu büyüt.");
            if (txt == "" && res.started == 0 && !res.full && res.renewed == 0) Toast("Şu an yapılacak iş yok");
            if (res.got >= 6) Celebrate("Bereketli!");
            PLATFORM.Hap(res.got > 0 ? "medium" : "light"); Commit(); RenderStrip(true);
        }
        public static void QuickAll(Vector2? at)
        {
            var tot = new QuickRes();
            foreach (var k in LOC.Keys.ToList())
            {
                if (!Owned(LOC[k].reg)) continue; var r = QuickLoc(k);
                tot.got += r.got; tot.started += r.started; tot.renewed += r.renewed; tot.full = tot.full || r.full;
                foreach (var kv in r.@out) tot.@out[kv.Key] = Get(tot.@out, kv.Key) + kv.Value;
            }
            QuickFeedback(tot, at);
        }

        // ---------- renderStrip(force) ----------
        public static void RenderStrip(bool force = false)
        {
            HudView.Ensure();
            bool show = Mode == "hub"; Dom.Of(HudView.StripRoot).Hidden = !show; if (!show) return;
            List<StripOwn> own; List<StripLock> lck; StripCards(out own, out lck);
            var qi = new Dictionary<string, QuickInfoR>(); int tr = 0, ti = 0, tb = 0;
            foreach (var o in own) { var q = QuickInfo(o.k); qi[o.k] = q; tr += q.ready; ti += q.idle; tb += q.brk; }
            var sig = string.Join(",", own.Select(o => o.k + o.c + Pn_N(o.n) + qi[o.k].ready + qi[o.k].idle + qi[o.k].brk)) + string.Join(",", lck.Select(l => l.r + (S.lv >= l.d.lvl ? 1 : 0) + (S.coins >= RegCost(l.r) ? 1 : 0))) + S.house;
            if (!force && sig == StripSig) return; StripSig = sig;
            float sl = StripEl.anchoredPosition.x;
            UI.Clear(StripEl);
            if (tr + ti + tb > 1)
            {
                string sm = (tr > 0 ? tr + " hazır" : "") + (tr > 0 && ti > 0 ? " · " : "") + (ti > 0 ? ti + " boşta" : "") + (tb > 0 ? (tr > 0 || ti > 0 ? " · " : "") + "☕" + tb : "");
                var all = St_Card(StripEl, "all", null, tr > 0 ? "🧺" : "▶️", tr > 0 ? "Hepsini topla" : "Hepsini başlat");
                Css.T(all, sm, 10.5f, UI.Hex("#e8f3d8"), true, TextAnchor.MiddleCenter, true).alignment = TextAnchor.UpperCenter;
                Css.Act(all, "data-quickall", true);
            }
            foreach (var o in own)
            {
                var q = qi[o.k]; var k = o.k; var L = o.L;
                string qaLbl = q.ready > 0 ? "🧺 Topla" : q.idle > 0 ? "▶ Başlat" : q.brk > 0 ? "☕ Devam" : null; bool re = q.ready == 0;
                var thk = L.kind == "house" ? "kulube" + S.house : L.units != null ? L.units[0] : k;
                Texture tex; THUMB.TryGetValue(thk, out tex);
                var card = St_Card(StripEl, qaLbl != null ? "has-qa" : "", tex, L.e, L.n);
                var dom = Dom.Of(card); dom.data["go"] = k; dom.title = o.t != "" ? o.t : L.n;
                Tut_Tag(card, ".scard[data-go=\"" + k + "\"]");
                if (qaLbl == null && !double.IsPositiveInfinity(q.next))
                {
                    var cd = Css.Countdown(card, q.next, "⏱", true, 10.5f, Css.Muted); cd.alignment = TextAnchor.MiddleCenter; Dom.Tag(cd, ".cd");
                }
                if (qaLbl != null)
                {
                    var qa = Css.Box(card, re ? Css.Honey : Css.Leaf, 10, 2, Css.Ink, new Vector2(0, 2), Css.Ink, "qa");
                    Css.Row(qa, 0, UI.Pad(0, 0, 6, 6), TextAnchor.MiddleCenter);
                    Css.T(qa, qaLbl, 12, re ? Css.Ink : Color.white, true, TextAnchor.MiddleCenter, false);
                    if (!re) qa.gameObject.AddComponent<Pulse>();
                    Css.Act(qa, "data-quick", true, "quick", k); Dom.Tag(qa, ".qa" + (re ? ".re" : ""));
                }
                if (o.c == "ok") Css.Badge(card, "ok", Pn_N(o.n) != "" ? Pn_N(o.n) : "✓", 22, -9);
                else if (o.c == "new") Css.Badge(card, "new", "🔨", 22, -9);
                else if (o.c == "idle") Css.Badge(card, "idle", "💤", 22, -9);
                // karta dokun → bölüme git; basılı tut → tarifler (qpop)
                Css.Click(card, pos => { if (DateNow() - LpSuppress < 500) return; Act("data-go", D("go", k)); }, true, false);
                var lp = card.gameObject.AddComponent<St_LongPress>(); lp.k = k;
            }
            foreach (var l in lck)
            {
                bool can = S.lv >= l.d.lvl;
                var card = St_Card(StripEl, "lock", null, "🔒", l.d.n);
                Css.T(card, can ? "₺" + Fmt(RegCost(l.r)) : "Seviye " + l.d.lvl, 10.5f, Css.Muted, true, TextAnchor.MiddleCenter, false);
                if (can && S.coins >= RegCost(l.r)) Css.Badge(card, "new", "🏳️", 22, -9);
                Css.Act(card, "data-region", true, "region", l.r);
            }
            var p = StripEl.anchoredPosition; p.x = sl; StripEl.anchoredPosition = p;
        }
        // .scard: resim (ya da emoji) + ad
        static RectTransform St_Card(Transform parent, string cls, Texture tex, string emoji, string name)
        {
            bool all = cls == "all", lk = cls == "lock";
            var card = Css.Box(parent, all ? Css.Forest : lk ? Css.Locked : UI.C.card, 16, 2.5f, Css.Ink, new Vector2(3, 4), Css.Ink, "scard");
            Css.Col(card, 4, UI.Pad(6, 6, 6, 7), TextAnchor.UpperCenter); card.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
            bool shortL = HudView.ShortLandscape; float w = shortL ? 84 : 108, ih = shortL ? 40 : 66;
            UI.Size(card, w, -1).minWidth = w;
            var ph = Css.Box(card, all ? UI.Hex("#3f7a4a") : lk ? UI.Hex("#cbbd9a") : UI.Hex("#d4d39d"), 11, 1.5f, all ? Color.white : Css.Ink, null, null, "ph");
            UI.Size(ph, -1, ih);
            if (!all && !lk) { var bg = ph.Find("bg").GetComponent<Image>(); bg.sprite = Css.ThumbBg(UI.Hex("#e9e3b8"), UI.Hex("#b9c27e")); bg.type = Image.Type.Simple; bg.color = Color.white; }
            var clip = UI.New("clip", ph); UI.Stretch(clip, 1.5f, 1.5f, 1.5f, 1.5f); clip.gameObject.AddComponent<RectMask2D>(); UI.IgnoreLayout(clip);
            if (tex != null) Css.ImgOrEmoji(clip, tex, null);
            else { var t = UI.Txt(clip, emoji, all ? 30 : 28, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform); }
            var nt = Css.T(card, name, shortL ? 10.5f : 12, all ? Color.white : lk ? Css.Muted : UI.C.text, true, TextAnchor.MiddleCenter, true); nt.lineSpacing = 1;
            Dom.Tag(card, ".scard" + (cls != "" ? "." + cls : ""));
            return card;
        }

        // Odaktayken bölümler arasında sırayla geçiş (‹ › düğmeleri ve başlıkta sağa-sola kaydırma)
        public static List<string> LocOrder() => LOC.Keys.Where(k => Owned(LOC[k].reg)).ToList();
        public static void StepLoc(int d)
        {
            var o = LocOrder(); var cur = View != null && !string.IsNullOrEmpty(View.loc) ? View.loc : Focus; int i = o.IndexOf(cur); if (i < 0) return;
            var k = o[(i + d + o.Count) % o.Count]; PLATFORM.Hap(); GoLocation(k);
        }

        // ---------- Uzun basma: kartın üstünde tarifler (qpop) ----------
        public static RectTransform Qpop;
        static string LpKey = null; public static double LpSuppress = 0;
        static void QpopHTML(Transform parent, string k)
        {
            var L = LOC[k];
            var h = UI.HStack(parent, 6, TextAnchor.MiddleLeft, null, "qp-h"); Dom.Tag(h, ".qp-h");
            Css.Flex(Css.T(h, L.e + " " + L.n, 17, Css.TitleCol, false, TextAnchor.MiddleLeft, true, true));
            var x = Css.Box(h, Css.Paper2, 15, 2, Css.Ink, null, null, "x"); UI.Size(x, 30, 30).minWidth = 30;
            UI.Stretch(Css.T(x, "✕", 14, Css.Wood, true, TextAnchor.MiddleCenter, false).rectTransform);
            Css.Act(x, "qpclose", true); Dom.Of(x).title = "Kapat";
            var b = UI.VStack(parent, 8, null, TextAnchor.UpperLeft, "qp-b"); Dom.Tag(b, ".qp-b");
            if (L.kind == "fields")
            {
                Pn_Seeds(b, CROPS.Where(c => c.col == null || Has1(c.col)).Where(CropSeasonOk).ToList(), false);
                Css.Btn(b, "🌱 Boş parsellere " + ITEMS[S.lastCrop].e + " ek", "qpplant", "big gold", true);
            }
            var us = (L.units ?? new List<string>()).Where(u => GetOr(S.units, u) != null && B[u].recipes != null).ToList();
            foreach (var u in us)
            {
                var s = S.units[u]; Css.UHead(b, B[u].e + " " + B[u].n);
                if (s.job != null) { JobRow(b, u); QueueHTML(b, u); } else RecipeTiles(b, u);
            }
            if (us.Count == 0 && L.kind != "fields") Css.Note(b, "Bu bölümde başlatılacak üretim yok.");
        }
        static ScrollRect _qpScroll;
        static void St_QpopFill(string k)
        {
            for (int i = Qpop.childCount - 1; i >= 0; i--) { var c = Qpop.GetChild(i); if (c.name == "bg") continue; c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); c.SetParent(null, false); }
            bool ph = HudView.Phone; float W = UI.W, H = UI.H;
            float w = ph ? W - 20 : 440;
            float oldBW = Css.BodyW; Css.BodyW = w - 5 - 24;
            RectTransform content; var sc = UI.Scroll(Qpop, out content, 8, UI.Pad(12, 12, 10, 12)); UI.Stretch(sc, 2.5f, 2.5f, 2.5f, 2.5f); _qpScroll = sc.GetComponent<ScrollRect>();
            QpopHTML(content, k);
            Css.BodyW = oldBW;
            Qpop.anchorMin = Qpop.anchorMax = Qpop.pivot = new Vector2(ph ? 0 : .5f, 0);
            Qpop.anchoredPosition = new Vector2(ph ? 10 : 0, 150 + HudView.SafeB);
            Qpop.sizeDelta = new Vector2(w, Mathf.Min(H * .58f, 520));
            if (Qpop.GetComponent<St_QpopFit>() == null) Qpop.gameObject.AddComponent<St_QpopFit>();
        }
        public static void OpenQpop(string k)
        {
            LpKey = k; St_QpopFill(k); Qpop.SetAsLastSibling(); Dom.Of(Qpop).Hidden = false; PLATFORM.Hap("medium");
            // animation:sayDrop .25s
            var cg = Qpop.gameObject.GetOrAdd<CanvasGroup>(); var rt = Qpop; var p0 = rt.anchoredPosition;
            UI.Tween(.25f, t => { if (!rt) return; float e = 1 - (1 - t) * (1 - t); cg.alpha = e; rt.anchoredPosition = p0 + new Vector2(0, 10 * (1 - e)); });
        }
        public static void CloseQpop() { if (Qpop != null) Dom.Of(Qpop).Hidden = true; LpKey = null; }
        public static void RefreshQpop()
        {
            if (Qpop == null || Dom.Of(Qpop).Hidden || LpKey == null) return;
            float y = _qpScroll != null && _qpScroll.content != null ? _qpScroll.content.anchoredPosition.y : 0;
            St_QpopFill(LpKey);
            if (_qpScroll != null && _qpScroll.content != null) { var p = _qpScroll.content.anchoredPosition; p.y = y; _qpScroll.content.anchoredPosition = p; }
        }
        // qpop dışına basınca kapanır (karta basma hariç)
        static readonly List<RaycastResult> _stHits = new List<RaycastResult>();
        static void St_Frame(float dt)
        {
            if (Qpop == null || Dom.Of(Qpop).Hidden) return;
            bool down = Input.GetMouseButtonDown(0); Vector2 pos = Input.mousePosition;
            if (Input.touchCount > 0) { var t = Input.GetTouch(0); if (t.phase == TouchPhase.Began) { down = true; pos = t.position; } }
            if (!down || EventSystem.current == null) return;
            var ev = new PointerEventData(EventSystem.current) { position = pos }; _stHits.Clear(); EventSystem.current.RaycastAll(ev, _stHits);
            foreach (var h in _stHits)
            {
                if (h.gameObject == null) continue; var tr = h.gameObject.transform;
                if (tr.IsChildOf(Qpop)) return;
                for (var x = tr; x != null; x = x.parent) { var dd = x.GetComponent<Dom>(); if (dd != null && dd.Has("scard")) return; }
            }
            CloseQpop();
        }
    }

    // Kartta basılı tutma (480ms); 9px'ten fazla kayarsa iptal
    public class St_LongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public string k; float t0 = -1; Vector2 start;
        public void OnPointerDown(PointerEventData e)
        {
            // karttaki hızlı iş düğmesine basıldıysa uzun basma yok
            var g = e.pointerCurrentRaycast.gameObject; if (g != null) { var d = g.GetComponentInParent<Dom>(); if (d != null && d.data.ContainsKey("quick")) return; }
            t0 = Time.unscaledTime; start = e.position;
        }
        public void OnPointerUp(PointerEventData e) { t0 = -1; }
        public void OnPointerExit(PointerEventData e) { t0 = -1; }
        void Update()
        {
            if (t0 < 0) return;
            Vector2 cur = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            if (Vector2.Distance(cur, start) / UI.Scale > 9) { t0 = -1; return; }
            if (Time.unscaledTime - t0 >= .48f) { t0 = -1; Game.LpSuppress = Game.DateNow(); try { Game.OpenQpop(k); } catch (Exception x) { Debug.LogException(x); } }
        }
    }
    // qpop yüksekliği: içerik kadar, en çok min(58vh,520px)
    public class St_QpopFit : MonoBehaviour
    {
        void LateUpdate()
        {
            var rt = transform as RectTransform; var sr = GetComponentInChildren<ScrollRect>(); if (rt == null || sr == null || sr.content == null) return;
            float h = Mathf.Min(Mathf.Min(UI.H * .58f, 520), sr.content.rect.height + 5);
            if (Mathf.Abs(rt.sizeDelta.y - h) > .5f) rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
        }
    }
}
