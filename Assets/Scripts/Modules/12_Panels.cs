// ================= PANELLER (12-panels.js) =================
// HTML dizgisi döndüren her fonksiyon burada ilk parametresi "Transform parent" olan void yöntemdir (PORTING.md).
// Kart (aside.sheet) HudView.SheetRoot'tur; RenderSheet onu boşaltıp seçili görünümün sheetXxx(parent) yöntemini çağırır.
// Görünüm başlığı Head(...), gövde ShBody(...) (kaydırılabilir .sh-body). Gövde kurmayan bir sayfa olursa
// başlıktan sonraki her şey kendiliğinden kaydırılabilir gövdeye taşınır.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciftlik
{
    // let view={k, loc, r, tab, mini, sig, confirm}
    public partial class View
    {
        public string k, loc, r, tab, sig;
        public bool mini, confirm;
        public View() { }
        public View(string k) { this.k = k; }
    }

    public static partial class Game
    {
        public static View View = null;          // let view=null
        public static RaceState Race = null;          // race=null (tür 21-horse-league'de)
        public static RectTransform Sheet => HudView.SheetRoot;   // const sheet=$('#sheet')
        public static readonly Dictionary<string, Texture> THUMB = new Dictionary<string, Texture>();   // 32-thumbnails doldurur

        static void Init_12() { HudView.Ensure(); }

        static string Pn_Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);   // JS sayı → dizgi (binlik ayırıcısız)
        static string Pn_N(object n) { if (n == null) return ""; var s = Convert.ToString(n, CultureInfo.InvariantCulture); return s == "0" || s == "False" ? "" : s; }

        // const img=k=>THUMB[k]?`<img>`:(B[k]?B[k].e:(LOC[k]?LOC[k].e:'🏠'))
        public static void Img(Transform parent, string k)
        {
            Texture t; THUMB.TryGetValue(k ?? "", out t);
            Css.ImgOrEmoji(parent, t, ImgEmoji(k));
        }
        public static string ImgEmoji(string k) => k != null && B.ContainsKey(k) ? B[k].e : (k != null && LOC.ContainsKey(k) ? LOC[k].e : "🏠");

        public static void Stars(Transform parent, int n, int max) { Css.Stars(parent, n, max); }

        // ---------- head(thumb,title,sub) ----------
        public static void Head(Transform parent, string thumb, string title, string sub) => Head(parent, p => UI.Txt(p, thumb, 26, null, false, TextAnchor.MiddleCenter, false), title, sub == null ? null : (Action<Transform>)(p => Pn_Sub(p, sub)), !string.IsNullOrEmpty(sub));
        public static void Head(Transform parent, Action<Transform> thumb, string title, string sub) => Head(parent, thumb, title, sub == null ? null : (Action<Transform>)(p => Pn_Sub(p, sub)), !string.IsNullOrEmpty(sub));
        public static void Head(Transform parent, Action<Transform> thumb, string title, Action<Transform> sub) => Head(parent, thumb, title, sub, sub != null);
        static void Pn_Sub(Transform p, string sub) { Css.T(p, sub, 12, Css.Muted, true); }
        static void Head(Transform parent, Action<Transform> thumb, string title, Action<Transform> sub, bool hasSub)
        {
            var h = UI.HStack(parent, 11, TextAnchor.MiddleLeft, UI.Pad(12, 48, 12, 10), "sh-head");
            Dom.Tag(h, ".sh-head");
            Css.Thumb(h, thumb);
            var ttl = UI.VStack(h, 3, null, TextAnchor.MiddleLeft, "ttl"); UI.Size(ttl, -1, -1, 1);
            Css.T(ttl, title, 21, Css.TitleCol, false, TextAnchor.MiddleLeft, true, true);
            if (hasSub && sub != null) { var sb = UI.VStack(ttl, 0, null, TextAnchor.MiddleLeft, "sub"); Dom.Tag(sb, ".sub"); sub(sb); }
            if (View != null && View.k == "loc" && LocOrder().Count > 1)
            {
                foreach (var st in new[] { "-1", "1" })
                {
                    var nb = Css.Box(h, Css.Paper2, 17, 2, Css.Ink, new Vector2(0, 2), Css.Ink, "nav");
                    UI.Size(nb, 34, 34).minWidth = 34;
                    var t = Css.T(nb, st == "-1" ? "‹" : "›", 22, Css.Wood, false, TextAnchor.MiddleCenter, false, true); UI.Stretch(t.rectTransform, 0, 0, 0, 3);
                    Css.Act(nb, "data-locstep", true, "locstep", st);
                    Dom.Of(nb).title = st == "-1" ? "Önceki bölüm" : "Sonraki bölüm";
                }
            }
            // .x kapat düğmesi (sağda, akışın dışında). Haritada değilken kartı küçültür/açar (▾/▴).
            var x = Css.Box(h, Css.Paper2, 15, 2, Css.Ink, null, null, "x"); UI.IgnoreLayout(x);
            x.anchorMin = x.anchorMax = x.pivot = new Vector2(1, .5f); x.sizeDelta = new Vector2(30, 30); x.anchoredPosition = new Vector2(-12, 0);
            string lbl = Mode != "hub" && View != null ? (View.mini ? "▴" : "▾") : "✕";
            var xt = Css.T(x, lbl, 14, Css.Wood, true, TextAnchor.MiddleCenter, false); UI.Stretch(xt.rectTransform);
            Css.Act(x, "close", true); Dom.Of(x).title = Mode != "hub" && View != null ? (View.mini ? "Kartı aç" : "Kartı küçült") : "Kapat";
            // alt kenarda kesikli çizgi
            var line = UI.Fill(h, UI.A(Css.Ink, .25f), 0, "dash"); UI.IgnoreLayout(line); line.raycastTarget = false;
            line.rectTransform.anchorMin = new Vector2(0, 0); line.rectTransform.anchorMax = new Vector2(1, 0); line.rectTransform.pivot = new Vector2(.5f, 0);
            line.rectTransform.sizeDelta = new Vector2(0, 2); line.rectTransform.anchoredPosition = Vector2.zero;
            // başlıkta sağa-sola kaydırma (13-events)
            var bg = h.gameObject.AddComponent<Image>(); bg.color = UI.C.clear; bg.raycastTarget = true;
            h.gameObject.AddComponent<Ev_HeadSwipe>();
        }
        // <div class="sh-body"> — kaydırılabilir gövde; içerik kabını döndürür
        public static RectTransform ShBody(Transform parent)
        {
            RectTransform content;
            var sc = UI.Scroll(parent, out content, 10, UI.Pad(12));
            sc.name = "sh-body"; UI.Size(sc, -1, -1, 1, 1); Dom.Tag(sc, ".sh-body");
            return content;
        }

        public static void CloseSheet() { if (View == null) return; if (Mode != "hub") { View.mini = !View.mini; RenderSheet(); return; } View = null; RenderSheet(); }
        public static void OpenView(string k) { View = new View { k = k }; RenderSheet(); }
        public static void SetDock()
        {
            foreach (var kv in HudView.DockBtns)
            {
                bool cur = View != null && kv.Key == View.k;
                Css.SetBg(kv.Value, cur ? Css.Honey : UI.C.card);
                Dom.Of(kv.Value).data["current"] = cur ? "true" : "false";
            }
        }

        public static void RenderSheet()
        {
            HudView.Ensure();
            var sd = Dom.Of(Sheet);
            if (View == null) { sd.Hidden = true; BodyClass("sheet-open", false); SetDock(); return; }
            bool wasHidden = sd.Hidden;
            sd.Hidden = false; BodyClass("sheet-open", true);
            bool dark = View.k == "alley"; sd.Toggle("dark", dark); Css.SetBg(Sheet, dark ? UI.Hex("#2b2838") : UI.C.paper);
            Action<Transform> f = null;
            switch (View.k)
            {
                case "loc": f = SheetLoc; break;
                case "region": f = SheetRegion; break;
                case "places": f = SheetPlaces; break;
                case "barn": f = SheetBarn; break;
                case "goals": f = SheetGoals; break;
                case "settings": f = SheetSettings; break;
                case "shop": f = SheetShop; break;
                case "album": f = SheetAlbum; break;
                case "ledger": f = SheetLedger; break;
                case "mail": f = SheetMail; break;
                case "guide": f = SheetGuide; break;
                case "borsa": f = SheetBorsa; break;
                case "budget": f = SheetBudget; break;
                case "stats": f = SheetStats; break;
            }
            // kaydırma konumunu koru
            var oldSr = Pn_BodyScroll(); float st = oldSr != null && oldSr.content != null ? oldSr.content.anchoredPosition.y : 0;
            for (int i = Sheet.childCount - 1; i >= 0; i--) { var c = Sheet.GetChild(i); if (c.name == "bg") continue; c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject); c.SetParent(null, false); }
            if (f == null) { Debug.LogWarning("renderSheet: bilinmeyen görünüm " + View.k); }
            else f(Sheet);
            Pn_EnsureBody();
            var nsr = Pn_BodyScroll(); if (nsr != null && nsr.content != null && !wasHidden) { var p = nsr.content.anchoredPosition; p.y = st; nsr.content.anchoredPosition = p; }
            bool mini = Mode != "hub" && View.mini;
            sd.Toggle("mini", mini);
            if (nsr != null) nsr.gameObject.SetActive(!mini);
            var head = Sheet.Find("sh-head"); if (head != null) { var dash = head.Find("dash"); if (dash != null) dash.gameObject.SetActive(!mini); }
            if (wasHidden) HudView.SheetShown();
            Tut_Tag(Sheet, "#sheet");
            View.sig = SheetSig(); SetDock();
        }
        static ScrollRect Pn_BodyScroll()
        {
            if (Sheet == null) return null;
            for (int i = 0; i < Sheet.childCount; i++) { var c = Sheet.GetChild(i); if (!c.gameObject.activeSelf) continue; var sr = c.GetComponent<ScrollRect>(); if (sr != null) return sr; }
            return null;
        }
        // Başlıktan sonra kaydırılabilir gövde yoksa kalan öğeleri bir .sh-body'ye taşı
        static void Pn_EnsureBody()
        {
            if (Pn_BodyScroll() != null) return;
            var rest = new List<Transform>();
            for (int i = 0; i < Sheet.childCount; i++) { var c = Sheet.GetChild(i); if (c.name == "bg" || c.name == "sh-head" || !c.gameObject.activeSelf) continue; rest.Add(c); }
            if (rest.Count == 0) return;
            var body = ShBody(Sheet);
            foreach (var c in rest) c.SetParent(body, false);
        }

        public static string SheetSig()
        {
            if (View == null) return ""; string s = "";
            if (View.k == "loc" && View.loc != null && LOC.ContainsKey(View.loc))
            {
                var L = LOC[View.loc];
                if (L.kind == "fields") s += string.Concat(S.fields.Select(f => f.job != null ? (Now() >= f.job.e ? "r" : "g") : "i"));
                foreach (var u in L.units ?? new List<string>())
                {
                    var x = GetOr(S.units, u);
                    s += x != null ? (x.job != null ? (Now() >= x.job.e ? "r" : "g") : "i") + (x.q != null ? x.q.Count : 0) + (Get(S.workers, u) != 0 ? (WorkOn(u) ? "w" : "b") : "") : "-";
                }
                if (L.kind == "orders") s += ReadyOrders();
            }
            if (View.k == "loc" && View.loc == "hipo") s += string.Join(",", S.horses.Select(h => (Now() < h.rest ? "r" : "") + (Now() < h.train ? "t" : ""))) + Has("at");
            return s;
        }

        // const outOf=sc=>`<span class="big">${ITEMS[k].e}<i>×${sc.out[k]}</i></span>`
        public static void OutOf(Transform parent, ScaledDef sc)
        {
            var k = sc.@out.Keys.First();
            Css.Big(parent, ITEMS[k].e, "×" + sc.@out[k]);
        }

        // ---------- recipeTiles(u,queue) ----------
        public static void RecipeTiles(Transform parent, string u, bool queue = false)
        {
            var d = B[u]; var s = S.units[u];
            var grid = Css.Tiles(parent, queue);
            foreach (var r in d.recipes)
            {
                bool lck = r.lvl > S.lv || (r.col != null && !Has1(r.col)); var sc = Scaled(r, s.l);
                bool ok = !lck && (queue ? (s.q != null ? s.q.Count : 0) < QMAX : CanPay(r));
                var tile = Css.Tile(grid, queue ? "queue" : "start", ok, queue, "u", u, "r", r.id);
                Dom.Of(tile).title = (queue ? "Sıraya ekle: " : "") + r.n;
                if (!queue) Tut_Tag(tile, ".tile[data-u=\"" + u + "\"]");
                OutOf(tile, sc);
                var ins = Css.Ins(tile);
                if (lck) Css.InsB(ins, r.col != null && r.lvl <= S.lv ? "📖 Koleksiyon" : "🔒 Sv " + r.lvl, false);
                else
                {
                    bool any = false;
                    if (r.cost > 0) { Css.InsB(ins, "₺" + Pn_Num(r.cost), S.coins < r.cost); any = true; }
                    foreach (var kv in r.@in ?? new Dictionary<string, int>()) { Css.InsB(ins, ITEMS[kv.Key].e + kv.Value, Has(kv.Key) < kv.Value); any = true; }
                    if (!any) Css.InsB(ins, "Bedava", false);
                }
                Css.Tm(tile, lck ? "" : "⏱ " + Secs(sc.t));
            }
        }

        // ---------- jobRow(u) ----------
        public static void JobRow(Transform parent, string u)
        {
            var s = S.units[u]; var r = UnitRecipe(u); var sc = Scaled(r, s.l);
            if (Now() >= s.job.e)
            {
                var b = Css.Box(parent, Css.Honey, 14, 2, Css.Ink, new Vector2(2, 3), Css.Ink90, "job");
                Css.Row(b, 20, UI.Pad(12, 12, 8, 8), TextAnchor.MiddleCenter);
                OutOf(b, sc); Css.T(b, "Topla!", 17, Css.GoldText, false, TextAnchor.MiddleCenter, false, true);
                Css.Act(b, "collect", true, "u", u); b.gameObject.AddComponent<Bob>();
                Dom.Tag(b, ".job.ready");
                return;
            }
            var j = Css.Job(parent); Dom.Of(j).title = r.n;
            OutOf(j, sc);
            var pw = Css.Pw(j);
            Css.PBar(pw, (float)Prog(s.job), s.job.s, s.job.e);
            Css.Countdown(pw, s.job.e, "⏱");
        }
        // ---------- upBtn(u) ----------
        public static void UpBtn(Transform parent, string u)
        {
            var s = S.units[u]; if (s.l >= MAXL) return; var bl = UpBlock(s.l + 1);
            var b = Css.Up(parent, "⬆ Sv " + (s.l + 1), !string.IsNullOrEmpty(bl) ? bl : "₺" + Fmt(UpCost(u)), "upU", !(!string.IsNullOrEmpty(bl) || S.coins < UpCost(u)), "u", u);
            Dom.Of(b).title = "+%30 ürün, %10 daha kısa süre";
        }
        // ---------- queueHTML(u) ----------
        public static void QueueHTML(Transform parent, string u)
        {
            var s = S.units[u]; var q = s.q ?? new List<string>(); Func<string, Recipe> R = id => B[u].recipes.Find(r => r.id == id);
            var row = Css.Flow(parent, 6, 6, 0, null, "queue"); Dom.Tag(row, ".queue");
            Css.T(row, "Sırada " + q.Count + "/" + QMAX, 13, Css.TitleCol, false, TextAnchor.MiddleLeft, false, true);
            for (int i = 0; i < q.Count; i++)
            {
                var r = R(q[i]); if (r == null) continue;
                var chip = Css.Box(row, UI.C.card, 10, 2, Css.Ink, new Vector2(1, 2), Css.Ink, "qchip");
                Css.Row(chip, 3, UI.Pad(6, 8, 2, 2), TextAnchor.MiddleCenter);
                UI.Txt(chip, ITEMS[r.@out.Keys.First()].e, 18, null, false, TextAnchor.MiddleCenter, false);
                Css.T(chip, "✕", 10, Css.Brick, true, TextAnchor.MiddleCenter, false);
                Css.Act(chip, "qdel", true, "u", u, "i", i.ToString()); Dom.Of(chip).title = "Sıradan çıkar: " + r.n;
                Dom.Tag(chip, ".qchip");
            }
            if (q.Count < QMAX) Css.T(row, "↓ eklemek için tarife dokun", 11, Css.Muted, true, TextAnchor.MiddleLeft, false);
            if (q.Count < QMAX) RecipeTiles(parent, u, true);
        }
        // .unit kabı (ardışık .unit'ler arasında kesikli çizgi + 10px boşluk)
        static RectTransform Pn_Unit(Transform parent)
        {
            bool after = parent.childCount > 0 && Dom.Of(parent.GetChild(parent.childCount - 1)) is Dom pd && pd.Has("unit");
            var u = UI.VStack(parent, 8, UI.Pad(0, 0, after ? 12 : 0, 0), TextAnchor.UpperLeft, "unit");
            if (after)
            {
                var line = UI.Fill(u, UI.A(Css.Ink, .2f), 0, "dash"); UI.IgnoreLayout(line); line.raycastTarget = false;
                line.rectTransform.anchorMin = new Vector2(0, 1); line.rectTransform.anchorMax = new Vector2(1, 1); line.rectTransform.pivot = new Vector2(.5f, 1);
                line.rectTransform.sizeDelta = new Vector2(0, 2); line.rectTransform.anchoredPosition = Vector2.zero;
            }
            Dom.Tag(u, ".unit");
            return u;
        }
        // ---------- unitHTML(u,multi) ----------
        public static void UnitHTML(Transform parent, string u, bool multi = false)
        {
            var d = B[u]; var s = GetOr(S.units, u);
            var unit = Pn_Unit(parent);
            if (s == null)
            {
                bool lck = S.lv < d.lvl;
                var build = Css.Box(unit, UI.C.clear, 14, 2, UI.A(Css.Ink, .7f), null, null, "build");
                Css.Row(build, 10, UI.Pad(10, 10, 8, 8), TextAnchor.MiddleLeft);
                if (lck) Css.Dim(build, .6f);
                Css.Thumb(build, p => Img(p, u), true);
                var t = UI.VStack(build, 2, null, TextAnchor.MiddleLeft, "t"); UI.Size(t, -1, -1, 1);
                Css.T(t, d.n, 14, Css.TitleCol, false, TextAnchor.MiddleLeft, true, true);
                var sm = Css.T(t, d.d, 11.5f, Css.Muted, true); sm.lineSpacing = 1.1f;
                Css.Btn(build, lck ? "🔒 " + d.lvl : "🔨 ₺" + Fmt(d.cost), "build", "gold", !(lck || S.coins < d.cost), "u", u);
                Dom.Tag(build, ".build" + (lck ? ".off" : ""));
                return;
            }
            if (multi) Css.UHead(unit, d.e + " " + d.n, null, p => Stars(p, s.l, MAXL));
            if (d.storage > 0)
            {
                var j = Css.Job(unit); Css.Big(j, "📦"); var pw = Css.Pw(j);
                Css.PwB(pw, "+" + d.storage * s.l + " yer"); Css.PwSmall(pw, "Ambar " + Stored() + " / " + Cap());
            }
            LoveRow(unit, u);
            if (s.job != null) { JobRow(unit, u); if (d.recipes != null) QueueHTML(unit, u); }
            else if (d.recipes != null) RecipeTiles(unit, u);
            var row = Css.Urow(unit);
            if (INSIDE_UNITS.ContainsKey(u) && Mode != "interior") Css.PillBtn(row, "🚪 İçeri", "enterIn", "go", true, "k", u);
            UpBtn(row, u);
            ExtraRow(row, u);
        }

        // ---------- sheetLoc() ----------
        public static void SheetLoc(Transform parent)
        {
            var k = View.loc; var L = LOC[k];
            string thk = L.kind == "house" ? "kulube" + S.house : L.units != null ? L.units[0] : k;
            Action<Transform> th = p => Img(p, thk);
            RectTransform b;
            switch (L.kind)
            {
                case "house":
                    {
                        Head(parent, th, HOUSE[S.house - 1].n, p => Stars(p, S.house, 4)); b = ShBody(parent);
                        var nx = S.house < HOUSE.Count ? HOUSE[S.house] : null;
                        var j = Css.Job(b); Css.Big(j, "📦"); var pw = Css.Pw(j);
                        Css.PwB(pw, "Kiler"); Css.PBar(pw, Mathf.Min(100, Stored() / (float)Cap() * 100) / 100f, 0, 0, 10, false); Css.PwSmall(pw, Stored() + " / " + Cap());
                        var row = Css.Urow(b);
                        if (Mode != "interior") Css.PillBtn(row, "🚪 Eve gir", "enterIn", "go", true, "k", "kulube");
                        if (nx != null) { var ub = Css.Up(row, "⬆ " + nx.n, S.lv >= nx.lvl ? "₺" + Fmt(nx.cost) : "🔒 Sv " + nx.lvl, "upHouse", S.lv >= nx.lvl && S.coins >= nx.cost); Dom.Of(ub).title = "Ambar büyür, binalar daha üst seviyeye çıkabilir"; }
                        GenHTML(b);
                        break;
                    }
                case "fields": Head(parent, th, L.n, p => Stars(p, S.fieldLv, MAXL)); b = ShBody(parent); FieldsBody(b); break;
                case "market":
                    Head(parent, th, L.n, ""); b = ShBody(parent);
                    Css.Btn(b, "🤝 Tüccarla pazarlık et", "mg", "big gold", !(Now() < S.haggleAt), "k", "haggle"); MarketBody(b); break;
                case "orders":
                    {
                        Head(parent, th, L.n, "★ ×" + RepMult().ToString("0.00", CultureInfo.InvariantCulture)); b = ShBody(parent);
                        var tb = View.tab ?? "o";
                        Css.Tabs(b, new Css.Tab("📜 Sipariş", tb == "o", "vtab", "t", "o"), new Css.Tab("🤝 Sözleşme", tb == "c", "vtab", "t", "c"),
                            new Css.Tab("❤️ Dostlar", tb == "f", "vtab", "t", "f"), new Css.Tab("🏛️ Kasaba", tb == "p", "vtab", "t", "p"));
                        if (tb == "o") OrdersBody(b); else if (tb == "c") ContractsBody(b); else if (tb == "p") ProjBody(b); else FriendsBody(b);
                        break;
                    }
                case "fair": Head(parent, th, L.n, "Bugünün yarışmaları"); b = ShBody(parent); FairBody(b); break;
                case "race":
                    if (GetOr(S.units, "hara") != null) Head(parent, th, L.n, p => Stars(p, S.units["hara"].l, MAXL)); else Head(parent, th, L.n, "");
                    b = ShBody(parent); UnitHTML(b, "hara"); RaceBody(b); break;
                default:
                    {
                        bool single = L.units.Count == 1; var s0 = GetOr(S.units, L.units[0]);
                        if (single && s0 != null) Head(parent, th, L.n, p => Stars(p, s0.l, MAXL)); else Head(parent, th, L.n, "");
                        b = ShBody(parent);
                        foreach (var u in L.units) UnitHTML(b, u, !single);
                        break;
                    }
            }
        }

        // ---------- fieldsBody() ----------
        public static void FieldsBody(Transform parent)
        {
            int ready = S.fields.Count(f => f.job != null && Now() >= f.job.e), empty = S.fields.Count(f => f.job == null), n = S.fields.Count;
            var seedE = ITEMS[S.lastCrop].e;
            Pn_Seeds(parent, CROPS.Where(c => (c.col == null || Has1(c.col)) && CropSeasonOk(c)).ToList(), true);
            var plots = Css.Cols(parent, 3, 7, 1.3f, "plots"); Dom.Tag(plots, ".plots");
            for (int i = 0; i < S.fields.Count; i++)
            {
                var f = S.fields[i];
                if (f.job == null) { var pe = Pn_Plot(plots, "empty", "plot", true, seedE, "ek", null, "i", i.ToString()); Dom.Of(pe).title = "Ek"; Pn_PlotSwipe(pe, i); continue; }
                var c = CropOf(f.job.r);
                if (Now() >= f.job.e) { var pr = Pn_Plot(plots, "ready", "plot", true, ITEMS[c.id].e, "topla!", null, "i", i.ToString()); Pn_PlotSwipe(pr, i); }
                else { var pg = Pn_Plot(plots, "", "plot", true, new[] { "", "🌱", "🌿", ITEMS[c.id].e }[StageOf(f.job)], null, f.job, "i", i.ToString()); Pn_PlotSwipe(pg, i); }
            }
            if (n < 9)
            {
                var pb = Pn_Plot(plots, "buy", "buyPlot", S.lv >= PLOT_LV[n] && S.coins >= PLOT_COST[n], "➕", S.lv < PLOT_LV[n] ? "🔒 Sv " + PLOT_LV[n] : "₺" + Fmt(PLOT_COST[n]), null);
                Dom.Of(pb).title = "Yeni parsel";
            }
            var run = S.fields.Where(f => f.job != null && Now() < f.job.e).ToList();
            var row = Css.Urow(parent);
            Css.PillBtn(row, "🌱 Ek (" + empty + ")", "plantAll", "go", empty > 0);
            Css.PillBtn(row, "🧺 Topla (" + ready + ")", "harvestAll", "gold", ready > 0);
            if (S.fieldLv < MAXL)
            {
                var bl = UpBlock(S.fieldLv + 1);
                var ub = Css.Up(row, "⬆ Sv " + (S.fieldLv + 1), !string.IsNullOrEmpty(bl) ? bl : "₺" + Fmt(FIELD_UP[S.fieldLv]), "upFields", !(!string.IsNullOrEmpty(bl) || S.coins < FIELD_UP[S.fieldLv]));
                Dom.Of(ub).title = "+%30 hasat, daha hızlı büyüme";
            }
            if (run.Count > 0) SpeedBtns(row, "F", run.Max(f => f.job.e - Now()));
            WorkerBtn(row, "fields");
        }
        // .seeds: yatay kayan tohum şeridi; her tohum (100% - 24px)/5 genişliğinde
        internal static void Pn_Seeds(Transform parent, List<Recipe> list, bool full)
        {
            float cw = (Css.BodyW - 24) / 5f;
            var root = UI.New("seeds", parent); Dom.Tag(root, ".seeds");
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var vp = UI.New("viewport", root); UI.Stretch(vp); vp.gameObject.AddComponent<RectMask2D>(); var vi = vp.gameObject.AddComponent<Image>(); vi.color = UI.C.clear;
            var content = UI.HStack(vp, 6, TextAnchor.UpperLeft, UI.Pad(2, 2, 2, 4), "content");
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 1); content.offsetMin = content.offsetMax = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp; sr.content = content; sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped;
            UI.Size(root, -1, 56);
            foreach (var c in list)
            {
                bool lck = CropLocked(c), on = c.id == S.lastCrop, mev = full && c.season != null;
                var box = Css.Box(content, on ? Css.Honey : mev ? UI.Hex("#fbecc6") : UI.C.card, 12, 2, Css.Ink, new Vector2(2, 2), Css.Ink90, "seed");
                Css.Col(box, 0, UI.Pad(0, 0, 4, 3), TextAnchor.UpperCenter);
                UI.Size(box, cw, 48).minWidth = cw;
                UI.Txt(box, ITEMS[c.id].e, 21, null, false, TextAnchor.MiddleCenter, false);
                Css.T(box, lck ? "🔒" + c.lvl : "₺" + Pn_Num(c.cost), 10.5f, UI.C.text, true, TextAnchor.MiddleCenter, false);
                Css.Act(box, "seed", !lck, "r", c.id);
                if (lck) Css.Dim(box, .45f);
                if (full) Dom.Of(box).title = c.n + (c.season != null ? " · mevsim ürünü" : "") + " · " + Secs(Scaled(c, S.fieldLv).t);
                if (mev) { var leaf = UI.Txt(box, "🍂", 11, null, false, TextAnchor.MiddleCenter, false); UI.IgnoreLayout(leaf); var lr = leaf.rectTransform; lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(1, 1); lr.sizeDelta = new Vector2(16, 16); lr.anchoredPosition = new Vector2(5, 7); }
                Dom.Tag(box, ".seed" + (on ? ".on" : "") + (mev ? ".mev" : ""));
            }
        }
        // .plot (empty / ready / buy / büyüyor)
        static Texture2D _plotTex;
        static RectTransform Pn_Plot(Transform parent, string cls, string act, bool enabled, string em, string txt, Job job, params string[] kv)
        {
            bool buy = cls == "buy";
            var box = Css.Box(parent, buy ? UI.A(Color.white, .4f) : Color.white, 12, 2, buy ? UI.A(Css.Ink, .75f) : Css.Ink, new Vector2(2, 2), Css.Ink90, "plot");
            if (!buy)
            {
                if (_plotTex == null)
                {
                    _plotTex = new Texture2D(1, 10, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                    var a = new Color32[10]; for (int y = 0; y < 10; y++) a[y] = y < 3 ? new Color32(0x7a, 0x4c, 0x2a, 255) : new Color32(0x8a, 0x5a, 0x33, 255); _plotTex.SetPixels32(a); _plotTex.Apply();
                }
                var bg = box.Find("bg"); var ri = UI.New("stripes", bg); UI.Stretch(ri, 2, 2, 2, 2); var raw = ri.gameObject.AddComponent<RawImage>(); raw.texture = _plotTex; raw.raycastTarget = false;
                ri.gameObject.AddComponent<Pn_TileV>();
            }
            if (cls == "ready")
            {
                var ring = Css.Box(box, UI.C.clear, 14, 3, Css.Honey, null, null, "ring"); UI.IgnoreLayout(ring); UI.Stretch(ring, -3, -3, -3, -3);
                ring.Find("bg").gameObject.SetActive(false); ring.GetComponent<Image>().raycastTarget = false; ring.SetAsFirstSibling();
                var rimg = ring.GetComponent<Image>(); rimg.type = Image.Type.Sliced; rimg.fillCenter = false;
            }
            Css.Col(box, 2, UI.Pad(0), TextAnchor.MiddleCenter);
            Color fg = buy ? UI.C.text : UI.Hex("#fff6e0");
            var e = UI.Txt(box, em, 22, null, false, TextAnchor.MiddleCenter, false); if (cls == "empty") e.color = new Color(1, 1, 1, .5f);
            if (txt != null) Css.T(box, txt, 11, fg, true, TextAnchor.MiddleCenter, false);
            if (job != null) { var holder = UI.HStack(box, 0, TextAnchor.MiddleCenter, null, "pw"); UI.Size(holder, -1, 7); var bar = Css.PBar(holder, (float)Prog(job), job.s, job.e, 7); var le = UI.Size(bar.transform.parent.parent); le.preferredWidth = (Css.BodyW - 14) / 3f * .72f; le.flexibleWidth = 0; }
            Css.Act(box, act, enabled, kv);
            if (!enabled) Css.Dim(box, .55f);
            Dom.Tag(box, ".plot" + (cls != "" ? "." + cls : ""));
            if (cls == "empty") Tut_Tag(box, ".plot.empty"); else if (cls == "ready") Tut_Tag(box, ".plot.ready");
            return box;
        }
        static void Pn_PlotSwipe(RectTransform plot, int i) { var s = plot.gameObject.AddComponent<Ev_PlotSwipe>(); s.i = i; }

        // ---------- marketBody() ----------
        public static void MarketBody(Transform parent)
        {
            var inv = ITEMS.Keys.Where(k => Has(k) > 0).ToList();
            var j = Css.Job(parent); Css.Big(j, "🌰"); var pw = Css.Pw(j);
            Css.PwB(pw, "Yem · ₺" + Pn_Num(ITEMS["yem"].buy)); Css.PwSmall(pw, "Elde " + Has("yem"));
            Css.PillBtn(j, "+5", "feed", "", true, "n", "5"); Css.PillBtn(j, "+20", "feed", "gold", true, "n", "20");
            if (inv.Any(k => SatF(k) < .85)) Css.Note(parent, "📉 Pazar bu ürünlerden doydu: çok satınca fiyat düşer, 15 dakikada toparlanır. Siparişler ve sözleşmeler bundan etkilenmez.");
            if (inv.Count > 0)
            {
                var grid = Css.Tiles(parent);
                foreach (var k in inv)
                {
                    double m = Get(S.market.m, k); if (m == 0 || !IsFinite(m)) m = 1;
                    string tr = m >= 1.1 ? " " + UI.Col("▲", Css.Green) : m <= .9 ? " " + UI.Col("▼", Css.Brick) : "";
                    var tile = Css.Tile(grid, null, true, false); Dom.Tag(tile, ".sell"); Dom.Of(tile).title = ITEMS[k].n;
                    Css.Big(tile, ITEMS[k].e, "×" + Has(k));
                    Css.Tm(tile, "₺" + Pn_Num(SellPrice(k)) + tr + (SatF(k) < .85 ? " " + UI.Col("📉", Css.Brick) : ""));
                    var two = UI.HStack(tile, 4, TextAnchor.MiddleCenter, UI.Pad(0, 0, 3, 0), "two");
                    Pn_SellBtn(two, "1", k, "1", false);
                    Pn_SellBtn(two, "Tümü", k, "999999", true);
                }
            }
            else Css.Note(parent, "Ambar boş. Önce bir şeyler üret.");
        }
        static void Pn_SellBtn(Transform p, string label, string k, string n, bool all)
        {
            var b = Css.Box(p, all ? Css.Forest : Css.Paper2, 8, 1.5f, Css.Ink, null, null, "button");
            Css.Row(b, 0, UI.Pad(7, 7, 1, 1), TextAnchor.MiddleCenter);
            Css.T(b, label, 11, all ? Color.white : UI.C.text, true, TextAnchor.MiddleCenter, false);
            Css.Act(b, "sell", true, "k", k, "n", n);
            if (all) Tut_Tag(b, "[data-act=\"sell\"][data-k=\"" + k + "\"][data-n=\"999999\"]");
        }

        // ---------- ordersBody() ----------
        public static void OrdersBody(Transform parent)
        {
            EnsureOrders();
            for (int i = 0; i < S.orders.Count; i++)
            {
                var o = S.orders[i]; bool ok = o.items.All(kv => Has(kv.Key) >= kv.Value);
                var box = Css.Box(parent, UI.C.card, 14, 2, Css.Ink, new Vector2(2, 3), Css.Ink90, "order");
                Css.Col(box, 6, UI.Pad(10, 10, 8, 8), TextAnchor.UpperLeft); box.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
                Dom.Tag(box, ".order"); Tut_Tag(box, ".order");
                var who = UI.HStack(box, 6, TextAnchor.MiddleLeft, null, "who");
                Css.Flex(Css.T(who, Face(o.who) + " " + UI.Esc(o.who) + " <size=10>" + Hearts(o.who) + "</size>", 13.5f, UI.C.text, true));
                Css.T(who, "₺" + Fmt(o.@base * RepMult() * FrMult(o.who)) + " · " + Pn_Num(o.xp) + " xp", 12, Css.Forest, true, TextAnchor.MiddleRight, false);
                var needs = Css.Flow(box, 5, 5, 0, null, "needs"); Dom.Tag(needs, ".needs");
                foreach (var kv in o.items)
                {
                    bool miss = Has(kv.Key) < kv.Value;
                    var nd = Css.Box(needs, miss ? Css.MissBg : Css.OkBg, 12, 1.5f, UI.A(Css.Ink, .4f), null, null, "need");
                    Css.Row(nd, 2, UI.Pad(4, 8, 1, 1), TextAnchor.MiddleCenter);
                    UI.Txt(nd, ITEMS[kv.Key].e, 15, null, false, TextAnchor.MiddleCenter, false);
                    Css.T(nd, Math.Min(Has(kv.Key), kv.Value) + "/" + kv.Value, 12, miss ? Css.Brick : Css.Forest, true, TextAnchor.MiddleCenter, false);
                    var dn = Dom.Tag(nd, ".need" + (miss ? ".miss" : "")); dn.title = ITEMS[kv.Key].n;
                }
                var row = Css.Urow(box);
                Css.PillBtn(row, "✓ Teslim et", "order", "gold", ok, "i", i.ToString());
                var re = Css.PillBtn(row, "↻", "reorder", "", true, "i", i.ToString()); Dom.Of(re).title = "Başka sipariş iste";
            }
        }

        // ---------- sheetRegion() ----------
        public static void SheetRegion(Transform parent)
        {
            var r = View.r; var d = REG[r]; bool ok = S.lv >= d.lvl;
            var locs = LOC.Keys.Where(k => LOC[k].reg == r).ToList();
            Head(parent, "🗺️", d.n, "Satılık arazi"); var b = ShBody(parent);
            var grid = Css.Cols(b, 3, 8, 0, "places"); Dom.Tag(grid, ".places");
            foreach (var k in locs) { var pl = Pn_Place(grid, false); var kk = k; Css.Thumb(pl, p => Img(p, LOC[kk].units != null ? LOC[kk].units[0] : kk), true); Pn_PlaceName(pl, LOC[k].n, false); }
            Css.Btn(b, ok ? "🏳️ Satın al · ₺" + Fmt(RegCost(r)) : "🔒 Seviye " + d.lvl, "region", "big gold", ok && S.coins >= RegCost(r), "r", r);
        }
        static RectTransform Pn_Place(Transform parent, bool locked)
        {
            var box = Css.Box(parent, locked ? Css.Locked : UI.C.card, 14, 2, Css.Ink, new Vector2(2, 3), Css.Ink90, "place");
            Css.Col(box, 4, UI.Pad(4, 4, 6, 6), TextAnchor.UpperCenter);
            Dom.Tag(box, ".place" + (locked ? ".locked" : ""));
            return box;
        }
        static void Pn_PlaceName(Transform p, string n, bool locked) { var t = Css.T(p, n, 11.5f, locked ? Css.Muted : UI.C.text, true, TextAnchor.MiddleCenter, true); t.alignment = TextAnchor.UpperCenter; t.lineSpacing = 1; }
        // ---------- sheetPlaces() ----------
        public static void SheetPlaces(Transform parent)
        {
            Head(parent, "📍", "Konumlar", ""); var b = ShBody(parent);
            var grid = Css.Cols(b, 3, 8, 0, "places"); Dom.Tag(grid, ".places");
            foreach (var k in LOC.Keys)
            {
                var L = LOC[k]; bool own = Owned(L.reg); var st = own ? LocStatus(k) : null;
                var pl = Pn_Place(grid, !own);
                var kk = k;
                Css.Thumb(pl, p => { if (own) Img(p, L.kind == "house" ? "kulube" + S.house : L.units != null ? L.units[0] : kk); else UI.Txt(p, "🔒", 20, null, false, TextAnchor.MiddleCenter, false); }, true);
                Pn_PlaceName(pl, L.n, !own);
                if (st != null && st.c == "ok") Css.Badge(pl, "ok", Pn_N(st.n) != "" ? Pn_N(st.n) : "✓");
                else if (st != null && st.c == "new") Css.Badge(pl, "new", "🔨");
                else if (st != null && st.c == "idle") Css.Badge(pl, "idle", "💤");
                Css.Act(pl, "data-go", true, "go", k);
            }
        }
        // ---------- sheetBarn() ----------
        public static void SheetBarn(Transform parent)
        {
            var inv = ITEMS.Keys.Where(k => Has(k) > 0).ToList();
            Head(parent, "📦", "Ambar", Stored() + " / " + Cap()); var b = ShBody(parent);
            Css.PBar(b, Mathf.Min(100, Stored() / (float)Cap() * 100) / 100f, 0, 0, 10, false);
            if (inv.Count > 0)
            {
                var grid = Css.Cols(b, 4, 7, 0, "inv"); Dom.Tag(grid, ".inv");
                foreach (var k in inv)
                {
                    var sl = Css.Box(grid, UI.C.card, 12, 2, Css.Ink, new Vector2(2, 2), Css.Ink90, "slot");
                    Css.Col(sl, 0, UI.Pad(2, 2, 5, 5), TextAnchor.UpperCenter);
                    UI.Txt(sl, ITEMS[k].e, 22, null, false, TextAnchor.MiddleCenter, false);
                    Css.T(sl, Has(k).ToString(), 13.5f, UI.C.text, true, TextAnchor.MiddleCenter, false);
                    Dom.Tag(sl, ".slot").title = ITEMS[k].n;
                }
            }
            else Css.Note(b, "Ambar boş.");
            SellBox(b);
            var row = Css.Urow(b); Css.PillBtn(row, "🏪 Pazara git", "data-go", "gold", true, "go", "pazar");
        }
        // Fazlayı sat kutusu: her üründen kaç tane tutulacağı seçilir; isteğe bağlı otomatik satış
        public static void SellBox(Transform parent)
        {
            int kn = S.keepN;
            var unit = Pn_Unit(parent);
            Css.UHead(unit, "💰 Fazlayı sat", "yem ve taylar hariç");
            Css.Tabs(unit, new[] { 0, 5, 10, 20 }.Select(n => new Css.Tab(n != 0 ? n + " tut" : "Hepsi", kn == n, "keepN", "n", n.ToString())).ToArray());
            Css.Btn(unit, "Her üründen " + kn + " tut, gerisini sat", "sellEx", "big gold", true);
            Css.Switch(unit, "optAutoSell", S.autoSell, "Ambar dolunca otomatik sat", "Ambar %95 dolunca fazlası kendiliğinden satılır, üretim durmaz", Ev_Change);
        }
        // ---------- sheetGoals() ----------
        public static void SheetGoals(Transform parent)
        {
            var g = S.goal >= 0 && S.goal < GOALS.Count ? GOALS[S.goal] : null;
            Head(parent, "🎯", "Hedefler", S.goal + " / " + GOALS.Count); var b = ShBody(parent);
            DailyHTML(b);
            Css.UHead(b, "🎯 Ana hedef");
            if (g != null)
            {
                bool ok = g.ok();
                var j = Css.Job(b); Css.Big(j, ok ? "✅" : "🎯"); var pw = Css.Pw(j);
                Css.PwB(pw, g.d, 14); Css.PwSmall(pw, "Ödül ₺" + Fmt(g.r));
                Css.PillBtn(j, "Al", "goal", "gold", ok);
            }
            else Css.Note(b, "Bütün hedefler bitti!");
            Css.Note(b, "Haritadaki tabelaya dokun, o bölüme git. ✓ hazır ürün, 🔨 kurulabilir bina, 💤 boşta bekleyen bina demek.");
        }
        // ---------- sheetSettings() ----------
        public static void SheetSettings(Transform parent)
        {
            Head(parent, "⚙️", "Ayarlar", ""); var b = ShBody(parent);
            AccountHTML(b);
            Css.UHead(b, "🖥️ Grafik", GFX.low ? "Pil tasarrufu açık" : "");
            Css.Tabs(b, new[] { new[] { "auto", "Otomatik" }, new[] { "high", "Yüksek" }, new[] { "low", "Pil tasarrufu" } }.Select(x => new Css.Tab(x[1], GFX.mode == x[0], "gfx", "k", x[0])).ToArray());
            Css.Note(b, "Otomatik: cihaza göre ayarlanır, takılırsa çözünürlüğü düşürür. Pil tasarrufu: gölgeler kapanır, 30 FPS. Değişiklik oyunu yeniden yükler.");
            Css.Switch(b, "optMusic", S.sound.music, "Müzik", "50'ler tarzı çiftlik ezgisi", Ev_Change);
            Css.UHead(b, "🔠 Yazı boyutu");
            string fs = S.ui != null && !string.IsNullOrEmpty(S.ui.fs) ? S.ui.fs : "n";
            Css.Tabs(b, new[] { new[] { "n", "Normal" }, new[] { "b", "Büyük" }, new[] { "xb", "Çok büyük" } }.Select(x => new Css.Tab(x[1], fs == x[0], "fs", "k", x[0])).ToArray());
            Css.Switch(b, "optLeft", S.ui != null && S.ui.left, "Sol el modu", "Kamera düğmeleri ve menüler sol başparmağa yakın olur", Ev_Change);
            Css.Switch(b, "optSfx", S.sound.sfx, "Ses efektleri", "Hayvanlar, hasat, para sesi", Ev_Change);
            Css.Switch(b, "optFast", S.fastNav, "Hızlı gezinme", "Kamera uçmadan bölümler arasında anında geçer", Ev_Change);
            Css.Switch(b, "optNotif", S.sound.notif, "Hazır bildirimleri", "Sekme arka plandayken ürün hazır olunca haber verir. Tarayıcı izin vermezse çalışmaz.", Ev_Change);
            if (View.confirm)
            {
                var row = Css.Urow(b); Css.Note(row, "Tüm ilerleme silinsin mi?");
                Css.Btn(row, "Sil", "reset2", "red", true); Css.Btn(row, "Vazgeç", "reset0", "soft", true);
            }
            else Css.Btn(b, "Baştan başla", "reset", "ghost", true);
        }

        // --- at isimleri ---
        public static readonly List<string> HNAMES = new List<string> { "Rüzgar", "Karayel", "Şahbatur", "Gümüşnal", "Yıldırım", "Bozok", "Sultan", "Asil Bey", "Fırtına", "Demirkır" };
    }

    // Parsel arka planındaki toprak şeritlerini 10px döşe
    public class Pn_TileV : MonoBehaviour
    {
        RawImage ri; RectTransform rt;
        void Update() { if (ri == null) { ri = GetComponent<RawImage>(); rt = transform as RectTransform; } if (ri != null && rt != null) ri.uvRect = new Rect(0, 0, 1, Mathf.Max(1, rt.rect.height) / 10f); }
    }
}
