// ================= OLAYLAR, HAFTANIN YARIŞMASI, KASABALI HİKÂYELERİ, BAŞARIMLAR (40-fun.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public partial class Evt { public double until; }

    public class EventDef { public Func<bool> ok; public string say; public Action run; }
    public class QuestDef
    {
        public string txt; public Dictionary<string, int> need;
        public QuestDef(string txt, Dictionary<string, int> need) { this.txt = txt; this.need = need; }
    }
    // questOf(n) sonucu
    public class QuestInfo { public int i; public string txt; public Dictionary<string, int> need; public bool last; }
    public class QuestRewardInfo { public double c; public int g; }
    public class AchDef { public string id, e, n, d; public Func<bool> ok; public int g; }

    public static partial class Game
    {
        // ---------- Ortak arayüz yardımcıları (G7) ----------
        internal static bool Fun_HasUnit(string u) => S.units != null && S.units.ContainsKey(u) && S.units[u] != null;
        static Dictionary<string, int> Fun_D(params object[] kv)
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[(string)kv[i]] = Convert.ToInt32(kv[i + 1]);
            return d;
        }
        // JS sayıyı dizgiye çevirir gibi (binlik ayırıcı yok): 12, 4.5
        internal static string Fun_Num(double v) => Math.Floor(v) == v && Math.Abs(v) < 1e15 ? ((long)v).ToString() : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        static Transform Fun_Find(Transform t, string name)
        {
            if (t == null) return null; if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { var r = Fun_Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }
        // <div class="sh-body"> — kaydırılabilir panel gövdesi; içerik kabını döndürür
        internal static RectTransform Fun_ShBody(Transform parent, string cls = "")
        {
            RectTransform content;
            var sc = UI.Scroll(parent, out content, 10, UI.Pad(12));
            sc.name = "sh-body"; UI.Size(sc, -1, -1, 1, 1);
            if (cls != "") content.name = "sh-body-" + cls;
            return content;
        }
        // .tabs{background:paper-2;padding:3px;border-radius:999px;border:2px ink} button{flex:1;padding:5px;font-weight:900;font-size:12.5px} [aria-selected=true]{background:card;box-shadow:0 0 0 1.5px ink}
        internal static RectTransform Fun_Tabs(Transform parent, IList<string[]> tabs, string cur, int perRow = 0)
        {
            var box = UI.Box(parent, UI.C.paper2, perRow > 0 ? 16 : 18, 2, UI.C.ink, null, "tabs");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 5; v.padding = UI.Pad(3);
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            int n = perRow > 0 ? perRow : tabs.Count; RectTransform row = null;
            for (int i = 0; i < tabs.Count; i++)
            {
                if (i % n == 0) { row = UI.HStack(box, 5, TextAnchor.MiddleCenter); var hg = row.GetComponent<HorizontalLayoutGroup>(); hg.childForceExpandWidth = true; }
                string k = tabs[i][0]; bool sel = k == cur;
                RectTransform b = sel ? UI.Box(row, UI.C.card, 14, 1.5f, UI.C.ink, null, "tab") : UI.Box(row, UI.C.clear, 14, 0, null, null, "tab");
                UI.Size(b, 0, 28, 1);
                var t = UI.Txt(b, tabs[i][1], perRow > 0 ? 12 : 12.5f, UI.C.text, true, TextAnchor.MiddleCenter, false);
                UI.Stretch(t.rectTransform, 4, 0, 4, 0); UI.IgnoreLayout(t);
                UI.Clickable(b, () => Act("vtab", D("t", k)));
            }
            return box;
        }
        internal static Text Fun_Note(Transform parent, string s) { return UI.Txt(parent, s, 12.5f, UI.C.muted, true); }
        // .pill-btn{border:2px ink;border-radius:999px;padding:6px 12px;font-weight:900;font-size:12.5px;background:paper-2;box-shadow:2px 2px 0 ink} .gold{honey;#4a2f00} .go{forest;#fff} :disabled{#ddd1b6;#8b7d62}
        internal static Button Fun_Pill(Transform parent, string label, Action onClick, string cls, bool enabled)
        {
            Color bg = cls == "gold" ? UI.C.honey : cls == "go" ? UI.C.forest : UI.C.paper2, fg = cls == "gold" ? UI.C.goldText : cls == "go" ? Color.white : UI.C.text, bc = UI.C.ink;
            if (!enabled) { bg = UI.Hex("#ddd1b6"); fg = UI.Hex("#8b7d62"); bc = UI.Hex("#8b7d62"); }
            var box = UI.Box(parent, bg, 15, 2, bc, new Vector2(2, 2), "pill-btn");
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = UI.Pad(12, 12, 6, 6); h.spacing = 6; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            UI.Txt(box, label, 12.5f, fg, true, TextAnchor.MiddleCenter, false);
            var b = UI.Clickable(box, () => { if (onClick != null) onClick(); });
            b.interactable = enabled;
            return b;
        }
        // .needs / .need (eksikse .miss)
        internal static void Fun_Needs(Transform parent, Dictionary<string, int> need)
        {
            var row = UI.HStack(parent, 5, TextAnchor.MiddleLeft, null, "needs");
            foreach (var kv in need)
            {
                bool miss = Has(kv.Key) < kv.Value;
                var p = UI.Box(row, miss ? UI.Hex("#fbe0d6") : UI.Hex("#e3f1d6"), 11, 1.5f, UI.A(UI.C.ink, .4f), null, "need");
                var h = p.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = UI.Pad(4, 8, 1, 1); h.spacing = 2; h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
                UI.Txt(p, ITEMS[kv.Key].e, 15, null, false, TextAnchor.MiddleCenter, false);
                UI.Txt(p, Math.Min(Has(kv.Key), kv.Value) + "/" + kv.Value, 12, miss ? UI.C.brick : UI.C.forest, true, TextAnchor.MiddleCenter, false);
            }
        }
        // .modal.login: rozet + başlık + metin + düğmeler
        internal static RectTransform Fun_Modal(Action<RectTransform> build) => UI.Modal(build, true, false, 320);
        // .modal .badge{80px yuvarlak, bal rengi, 3px mürekkep kenarlık, 36px}
        internal static void Fun_Badge(Transform card, string e)
        {
            var wrap = UI.HStack(card, 0, TextAnchor.MiddleCenter, UI.Pad(0, 0, 0, 10), "badgeRow");
            var b = UI.Box(wrap, UI.C.honey, 40, 3, UI.C.ink, null, "badge"); UI.Size(b, 80, 80, 0, 0, 80, 80);
            var t = UI.Txt(b, e, 36, UI.Hex("#6b4100"), false, TextAnchor.MiddleCenter, false, true); UI.Stretch(t.rectTransform); UI.IgnoreLayout(t);
            UI.Boing(b);
        }
        // .modal h2{font-family:display;font-size:30px;color:forest}
        internal static Text Fun_H2(Transform card, string s, float size = 30) => UI.Txt(card, s, size, UI.C.forest, false, TextAnchor.MiddleCenter, true, true);
        // .modal p{font-weight:700;color:#5d503c;line-height:1.5}
        internal static Text Fun_P(Transform card, string s, float size = 14, TextAnchor align = TextAnchor.MiddleCenter) { var t = UI.Txt(card, s, size, UI.Hex("#5d503c"), true, align, true); t.lineSpacing = 1.25f; return t; }

        // --- Ödül verme yardımcısı: ürün ambara sığmazsa karşılığı para olarak verilir ---
        public static string GiveItems(Dictionary<string, int> @out)
        {
            double money = 0; var got = new Dictionary<string, int>();
            foreach (var kv in @out)
            {
                string k = kv.Key; double free = Math.Max(0, (double)Cap() - Stored());
                int n = k == "at" ? kv.Value : (int)Math.Min(kv.Value, free);
                if (n > 0) { S.items[k] = Has(k) + n; got[k] = n; }
                int rest = kv.Value - n; if (rest > 0) money += rest * ITEMS[k].p;
            }
            if (money != 0) Gain(money, "Ödül");
            return string.Join(" ", got.Select(e => $"+{e.Value} {ITEMS[e.Key].e}")) + (money != 0 ? $" +₺{Fmt(money)}" : "");
        }

        // --- Rastgele olaylar: 20–40 dakikada bir, çiftçinin balonunda duyurulur; 10 dakika içinde dokunulmazsa kaybolur ---
        public static readonly Dictionary<string, EventDef> EVENTS = new Dictionary<string, EventDef> {
            { "tilki", new EventDef { ok = () => Fun_HasUnit("kumes"), say = "🦊 Kümese tilki dadandı! Kovalayalım mı?", run = FoxGame } },
            { "kuzu", new EventDef { ok = () => Fun_HasUnit("agil"), say = "🐑 Bir kuzu kayboldu, çalılıklarda arayalım mı?", run = LambGame } },
            { "tuccar", new EventDef { ok = () => S.lv >= 3, say = "🧳 Gezgin tüccar kasabaya uğradı, nadir mallar getirmiş!", run = TraderModal } },
            { "ogul", new EventDef { ok = () => Fun_HasUnit("kovan"), say = "🐝 Arılar oğul verdi! Petekleri hemen toplayalım mı?", run = () => { var r = GiveItems(Fun_D("bal", 3 + LvOf("kovan"))); EvtEnd($"Petekler toplandı! {r}"); } } },
            { "yagmurdurasi", new EventDef { ok = () => S.fields.Count >= 3, say = "🌈 Gökkuşağı çıktı! Tarlada bereket var, bir bakalım mı?", run = () => {
                int n = 0; foreach (var f in S.fields) { if (f.job != null && Now() < f.job.e) { f.job.e = Math.Max(Now(), f.job.e - 60000); n++; } }
                EvtEnd(n > 0 ? $"{n} parseldeki ekinler bir dakika erken olgunlaşacak!" : "Tarlalar boştu ama gökkuşağı güzeldi. +1 🍀", n > 0 ? 0 : 1); } } },
        };
        public static Evt EvtState() { if (S.evt == null) S.evt = new Evt { next = Now() + 15 * 60000 }; return S.evt; }
        public static void EvtTick()
        {
            var E = EvtState(); if (E.cur != null && Now() > E.until) E.cur = null;
            if (E.cur != null || Now() < E.next || Mode == "desk" || TutStep() >= 0 || Tut2Step() >= 0 || UI.AnyModal) return;
            var ok = EVENTS.Keys.Where(k => EVENTS[k].ok()).ToList(); E.next = Now() + (20 + Rnd() * 20) * 60000; if (ok.Count == 0) return;
            E.cur = ok[(int)Math.Floor(Rnd() * ok.Count)]; E.until = Now() + 10 * 60000; Sfx("chime"); FarmerSay(true);
        }
        public static FarmerHint EvtHint()
        {
            var E = S != null ? S.evt : null; if (E == null || string.IsNullOrEmpty(E.cur) || Now() > E.until || !EVENTS.ContainsKey(E.cur)) return null;
            return new FarmerHint("evt" + E.cur, EVENTS[E.cur].say, () => RunEvent());
        }
        public static void RunEvent()
        {
            var E = EvtState(); EventDef ev = E.cur != null && EVENTS.ContainsKey(E.cur) ? EVENTS[E.cur] : null; E.cur = null; if (ev == null) return;
            Stat("event"); try { ev.run(); } catch (Exception e) { Debug.LogWarning(e); }
        }
        public static void EvtEnd(string msg, int gems = 0) { if (gems != 0) S.gems += gems; Toast(msg); Sfx("win"); Commit(); }

        public static void FoxGame()
        {
            var o = MgOpen("🦊 Tilkiyi kovala", "Tilki kümesin etrafında koşuyor. 10 saniyede 5 kez dokun!"); int hits = 0; bool done = false; int iv = 0, tm = 0;
            // <div class="mgfield fox"><button class="foxbtn">🦊</button></div><div class="mgscore">0 / 5</div>
            var f = UI.Box(o.body, UI.Hex("#bcd895"), 14, 2, UI.C.ink, null, "mgfield fox"); UI.Size(f, -1, 250, 1, 0, -1, 250);
            var top = UI.Fill(f, UI.Hex("#cfe6b0"), 12, "grad"); top.raycastTarget = false; UI.IgnoreLayout(top);
            top.rectTransform.anchorMin = new Vector2(0, .5f); top.rectTransform.anchorMax = Vector2.one; top.rectTransform.offsetMin = new Vector2(2, 0); top.rectTransform.offsetMax = new Vector2(-2, -2);
            var bot = UI.Fill(f, UI.Hex("#a9c97a"), 12, "grad"); bot.raycastTarget = false; UI.IgnoreLayout(bot);
            bot.rectTransform.anchorMin = Vector2.zero; bot.rectTransform.anchorMax = new Vector2(1, .5f); bot.rectTransform.offsetMin = new Vector2(2, 2); bot.rectTransform.offsetMax = new Vector2(-2, 0);
            var b = UI.New("foxbtn", f); UI.IgnoreLayout(b); b.sizeDelta = new Vector2(56, 56); b.pivot = new Vector2(.5f, .5f);
            var bt = UI.Txt(b, "🦊", 40, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(bt.rectTransform);
            var sc = UI.Txt(o.body, "0 / 5", 20, UI.C.ink, false, TextAnchor.MiddleCenter, false, true); UI.Size(sc, -1, 32);
            Action hop = () =>
            {
                if (!b) return;
                var to = new Vector2((float)(5 + Rnd() * 75) / 100f, 1 - (float)(8 + Rnd() * 70) / 100f); var from = b.anchorMin;
                UI.Tween(.25f, k => { if (b) { b.anchorMin = b.anchorMax = Vector2.Lerp(from, to, 1 - (1 - k) * (1 - k)); b.anchoredPosition = Vector2.zero; } });
            };
            b.anchorMin = b.anchorMax = new Vector2(.4f, .5f); b.anchoredPosition = Vector2.zero;
            hop(); iv = SetInterval(hop, 800);
            Action<bool> end = win =>
            {
                if (done) return; done = true; ClearTimer(iv); ClearTimer(tm);
                if (win) { var r = GiveItems(Fun_D("yumurta", 4 + 2 * LvOf("kumes"))); S.gems += 1; Stat("fox"); MgEnd(o, $"Tilki kaçtı, tavuklar güvende! {r} +1 🍀"); }
                else MgEnd(o, "Tilki ormana kaçtı. Tavuklar korktu ama hepsi yerinde.");
                Commit();
            };
            UI.Clickable(b, () => { hits++; sc.text = hits + " / 5"; PLATFORM.hap(); Sfx("pop"); hop(); if (hits >= 5) end(true); });
            o.onClose = () => { ClearTimer(iv); ClearTimer(tm); done = true; };
            tm = SetTimeout(() => end(false), 10000);
        }
        public static void LambGame()
        {
            var o = MgOpen("🐑 Kayıp kuzu", "Kuzu üç çalıdan birinin arkasında. İki hakkın var!"); int at = (int)Math.Floor(Rnd() * 3); int tries = 2; bool done = false;
            // .bushes{display:flex;justify-content:center;gap:14px;margin:8px 0} .bush{font-size:46px;border:2.5px ink;border-radius:18px;background:#e7f0d2;width:78px;height:78px;box-shadow:2px 3px 0 ink}
            var row = UI.HStack(o.body, 14, TextAnchor.MiddleCenter, UI.Pad(0, 0, 8, 8), "bushes");
            var sc = UI.Txt(o.body, "2 hak", 20, UI.C.ink, false, TextAnchor.MiddleCenter, false, true); UI.Size(sc, -1, 32);
            for (int i = 0; i < 3; i++)
            {
                int bi = i;
                var bb = UI.Box(row, UI.Hex("#e7f0d2"), 18, 2.5f, UI.C.ink, new Vector2(2, 3), "bush"); UI.Size(bb, 78, 78, 0, 0, 78, 78);
                var t = UI.Txt(bb, "🌳", 46, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform); UI.IgnoreLayout(t);
                Button btn = null;
                btn = UI.Clickable(bb, () =>
                {
                    if (done || !btn.interactable) return;
                    if (bi == at)
                    {
                        done = true; t.text = "🐑"; var r = GiveItems(Fun_D("yun", 3 + LvOf("agil"))); Stat("lamb"); Sfx("win");
                        SetTimeout(() => { MgEnd(o, $"Kuzu bulundu, annesine kavuştu! {r}"); Commit(); }, 600);
                    }
                    else
                    {
                        t.text = "🍃"; UI.SetEnabled(btn, false); tries--; sc.text = tries + " hak"; Sfx("pop");
                        if (tries == 0) { done = true; SetTimeout(() => { var r = GiveItems(Fun_D("yun", 1)); MgEnd(o, $"Kuzu akşam kendisi döndü. {r}"); Commit(); }, 500); }
                    }
                });
            }
        }
        class Fun_Deal { public Dictionary<string, int> give, get; public int gems; public string col; public double coins; }
        public static void TraderModal()
        {
            var own = ITEMS.Keys.Where(k => Has(k) >= 8 && ITEMS[k].p < 60 && k != "yem").ToList();
            var rare = new List<string> { "kestane", "kasar", "baklava", "hali", "pastirma", "sabun" }.Where(k => ITEMS.ContainsKey(k)).ToList();
            var deals = new List<Fun_Deal>();
            if (own.Count > 0 && rare.Count > 0)
            {
                string g = own[(int)Math.Floor(Rnd() * own.Count)], t = rare[(int)Math.Floor(Rnd() * rare.Count)];
                int n = Math.Max(4, Math.Min(Has(g), CeilI(ITEMS[t].p * 0.65 / ITEMS[g].p)));
                if (Has(g) >= n) deals.Add(new Fun_Deal { give = Fun_D(g, n), get = Fun_D(t, 1) });
            }
            var seeds = COL.Where(c => c.src == "field" && Get(S.album, c.id) == 0).ToList(); if (seeds.Count > 0) deals.Add(new Fun_Deal { gems = 8, col = seeds[0].id });
            deals.Add(new Fun_Deal { coins = Math.Max(200, S.lv * 120), get = Fun_D("yem", 30) });
            RectTransform d = null;
            Action<string> pick = v =>
            {
                UI.Kill(d); if (v == "x") return; var x = deals[int.Parse(v)];
                if (x.col != null) { if (S.gems < x.gems) return; S.gems -= x.gems; S.album[x.col] = Get(S.album, x.col) + 1; Stat("trade"); ShowFind(COL.Find(c => c.id == x.col)); Commit(); return; }
                if (x.give != null)
                {
                    foreach (var kv in x.give) if (Has(kv.Key) < kv.Value) { Toast("Eksik ürün var"); return; }
                    foreach (var kv in x.give) S.items[kv.Key] -= kv.Value;
                }
                else { if (S.coins < x.coins) { Toast("Yeterli paran yok"); return; } Pay(x.coins, "Tüccar"); }
                Stat("trade"); EvtEnd("Anlaştınız! " + GiveItems(x.get));
            };
            d = Fun_Modal(card =>
            {
                Fun_Badge(card, "🧳"); Fun_H2(card, "Gezgin tüccar", 24);
                UI.Txt(card, "\"Bugün buradayım, yarın yolda! Ne dersin?\"", 12.5f, UI.C.muted, true, TextAnchor.MiddleCenter);
                for (int i = 0; i < deals.Count; i++)
                {
                    var x = deals[i]; string idx = i.ToString();
                    string txt;
                    if (x.col != null) { var c = COL.Find(cc => cc.id == x.col); txt = $"8 🍀 → {c.e} {c.n}"; }
                    else if (x.give != null) txt = $"{string.Join(" ", x.give.Select(e => e.Value + " " + ITEMS[e.Key].e))} → {string.Join(" ", x.get.Select(e => e.Value + " " + ITEMS[e.Key].e + " " + ITEMS[e.Key].n))}";
                    else txt = $"₺{Fmt(x.coins)} → 30 🌰 yem";
                    bool can = x.col != null ? S.gems >= x.gems : x.give != null ? x.give.All(e => Has(e.Key) >= e.Value) : S.coins >= x.coins;
                    UI.Btn(card, txt, () => pick(idx), can ? "big gold" : "big dis");
                }
                UI.Btn(card, "Teşekkürler, sonra", () => pick("x"), "ghost");
            });
        }

        // --- Haftanın yarışması: her hafta bir panayır yarışmasının ödülü iki katı ---
        public static int WeekNo() => (int)Math.Floor((DateNow() - 1704067200000.0) / (7 * 864e5));   // Date.UTC(2024,0,1)
        public static string WeeklyContest()
        {
            var pool = Producible();
            var ok = CONTESTS.Where(c => c.need.Keys.All(k => pool.Contains(k))).ToList();
            return ok.Count > 0 ? ok[WeekNo() % ok.Count].id : null;
        }

        // --- Kasabalı hikâyeleri: Tanıdık olunca her kasabalının 3 adımlık küçük bir isteği açılır ---
        public static readonly Dictionary<string, List<QuestDef>> QUESTS = new Dictionary<string, List<QuestDef>> {
            { "Kasap Rıza", new List<QuestDef> { new QuestDef("Dükkânın vitrini boş kaldı, biraz yumurta getirir misin?", Fun_D("yumurta", 8)), new QuestDef("Bayram yaklaşıyor, kavurma için ete ihtiyacım var.", Fun_D("et", 4)), new QuestDef("Oğlumun düğününe pastırma lazım, senden iyisini kimse yapamaz!", Fun_D("pastirma", 3)) } },
            { "Fırıncı Nuri", new List<QuestDef> { new QuestDef("Sabah ekmeği için un bitti, yardım eder misin?", Fun_D("un", 6)), new QuestDef("Mahalleye simit dağıtacağım, ekmeklerini alabilir miyim?", Fun_D("ekmek", 5)), new QuestDef("Ustamın tarifiyle baklava yapacağım, senin baklavandan tadına bakmalıyım!", Fun_D("baklava", 1)) } },
            { "Lokantacı Sevim", new List<QuestDef> { new QuestDef("Menüye taze domates salatası koyacağım.", Fun_D("domates", 10)), new QuestDef("Kışlık turşu için lahana lazım.", Fun_D("lahana", 6)), new QuestDef("Yeni lokantamın açılışına salça ve tereyağı gerek.", Fun_D("salca", 3, "tereyag", 2)) } },
            { "Manav Hilmi", new List<QuestDef> { new QuestDef("Tezgâhta mısır kalmadı, biraz getirir misin?", Fun_D("misir", 12)), new QuestDef("Okul çocuklarına elma dağıtacağım.", Fun_D("elma", 15)), new QuestDef("Pazarda en güzel karpuz benim tezgâhımda olsun!", Fun_D("karpuz", 4)) } },
            { "Otelci Gülten", new List<QuestDef> { new QuestDef("Odalara taze süt koyacağım.", Fun_D("sut", 8)), new QuestDef("Kahvaltı tabağına peynir ve bal lazım.", Fun_D("peynir", 3, "bal", 3)), new QuestDef("Otelin lobisine el dokuma bir halı istiyorum.", Fun_D("hali", 1)) } },
            { "Kooperatif", new List<QuestDef> { new QuestDef("Ortak ambara buğday desteği verir misin?", Fun_D("bugday", 30)), new QuestDef("Köylüler için yem stoku yapıyoruz.", Fun_D("yem", 25)), new QuestDef("Kooperatif dokuma atölyesi kuruyor, kumaş gerek.", Fun_D("kumas", 4)) } },
            { "Bakkal Ahmet", new List<QuestDef> { new QuestDef("Rafta yoğurt kalmadı.", Fun_D("yogurt", 4)), new QuestDef("Kış için reçel stoklamak istiyorum.", Fun_D("recel", 3)), new QuestDef("Bakkalın yanına küçük bir sabun köşesi açıyorum.", Fun_D("sabun", 4)) } },
            { "Konakçı Halit Bey", new List<QuestDef> { new QuestDef("Konağın bahçesine lale diktirmek istiyorum.", Fun_D("lale", 9)), new QuestDef("Misafirlerime kek ikram edeceğim.", Fun_D("kek", 3)), new QuestDef("Konağın kilerine eski kaşar koymalıyım, şan olsun!", Fun_D("kasar", 2)) } },
            { "Pazarcı Zehra", new List<QuestDef> { new QuestDef("Tezgâhıma patates lazım evladım.", Fun_D("patates", 12)), new QuestDef("Yünden çorap öreceğim, yün getir.", Fun_D("yun", 8)), new QuestDef("Pazarın en güzel ayçiçek yağı benim tezgâhımda olsun.", Fun_D("yag", 4)) } },
        };
        public static QuestInfo QuestOf(string n)
        {
            if (S.quests == null) S.quests = new Dictionary<string, int>();
            int i = Get(S.quests, n); List<QuestDef> Q; QUESTS.TryGetValue(n, out Q);
            return Q != null && i < Q.Count ? new QuestInfo { i = i, txt = Q[i].txt, need = Q[i].need, last = i == Q.Count - 1 } : null;
        }
        public static QuestRewardInfo QuestReward(QuestInfo q) => new QuestRewardInfo { c = Round(q.need.Sum(e => ITEMS[e.Key].p * e.Value) * 2.5), g = q.last ? 5 : 2 };
        // .quest{margin:-4px 0 6px 18px;padding:8px 10px;border:2px dashed rgba(42,26,16,.35);border-radius:12px;background:#fffaf0;gap:6px;font-size:12.5px;font-weight:700}
        public static void QuestHTML(Transform parent, string n)
        {
            if (FrLv(n) < 1) return;
            var wrap = UI.VStack(parent, 0, UI.Pad(18, 0, 0, 6), TextAnchor.UpperLeft, "questWrap");
            var q = QuestOf(n);
            var box = UI.Box(wrap, UI.Hex("#fffaf0"), 12, 2, UI.A(UI.C.ink, .35f), null, "quest");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 6; v.padding = UI.Pad(10, 10, 8, 8);
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            if (q == null) { UI.Txt(box, $"✓ {UI.Esc(n)} ile bütün hikâye tamam", 12, UI.C.muted, true); return; }
            var r = QuestReward(q); bool ok = q.need.All(e => Has(e.Key) >= e.Value);
            UI.Txt(box, $"<i>“{q.txt}”</i>", 12.5f, UI.Hex("#4a3420"), true);
            Fun_Needs(box, q.need);
            var urow = UI.HStack(box, 7, TextAnchor.MiddleLeft, null, "urow");
            string nn = n;
            Fun_Pill(urow, $"Teslim et · ₺{Fmt(r.c)} +🍀{r.g}", () => Act("quest", D("n", nn)), "gold", ok);
            UI.Txt(urow, $"{q.i + 1}/3", 10.5f, UI.C.muted, true, TextAnchor.MiddleLeft, false);
        }
        public static void DoQuest(string n)
        {
            var q = QuestOf(n); if (q == null) return;
            foreach (var kv in q.need) if (Has(kv.Key) < kv.Value) { Toast("Eksik ürün var"); return; }
            foreach (var kv in q.need) S.items[kv.Key] -= kv.Value;
            var r = QuestReward(q);
            Gain(r.c, "Hikâye"); S.gems += r.g; S.quests[n] = q.i + 1; Befriend(n, 2); AddXP(Round(r.c / 20)); Stat("quest"); Sfx("win"); FlyCoins(8);
            Toast(q.last ? $"{Face(n)} {n}: \"Sen olmasan ne yapardık!\" Hikâye tamam · +₺{Fmt(r.c)} +🍀{r.g}" : $"{Face(n)} {n} çok sevindi · +₺{Fmt(r.c)} +🍀{r.g}");
            Commit();
        }

        // --- Başarımlar: koşulu sağlanınca ödülü Koleksiyon > Başarımlar'dan alınır ---
        static double AchSt(string k) => S.stats != null ? Get(S.stats, k) : 0;
        public static readonly List<AchDef> ACH = new List<AchDef> {
            new AchDef{ id="ilkhasat", e="🌾", n="İlk hasat", d="Tarladan 1 kez hasat yap", ok=()=>AchSt("harvest_field")>=1, g=1 },
            new AchDef{ id="hasat100", e="🚜", n="Toprak ana", d="100 kez hasat yap", ok=()=>AchSt("harvest_field")>=100, g=3 },
            new AchDef{ id="hasat1000", e="🏆", n="Bereket ustası", d="1000 kez hasat yap", ok=()=>AchSt("harvest_field")>=1000, g=10 },
            new AchDef{ id="satis100", e="💰", n="Pazarcı", d="Pazarda 100 ürün sat", ok=()=>AchSt("sold")>=100, g=2 },
            new AchDef{ id="satis2000", e="🏪", n="Tüccar ruhu", d="2000 ürün sat", ok=()=>AchSt("sold")>=2000, g=6 },
            new AchDef{ id="siparis10", e="📜", n="Güvenilir çiftçi", d="10 sipariş teslim et", ok=()=>AchSt("orders")>=10, g=3 },
            new AchDef{ id="siparis100", e="🤝", n="Kasabanın gözdesi", d="100 sipariş teslim et", ok=()=>AchSt("orders")>=100, g=10 },
            new AchDef{ id="sozlesme5", e="✍️", n="Sözünün eri", d="5 sözleşme tamamla", ok=()=>AchSt("contracts")>=5, g=4 },
            new AchDef{ id="bina5", e="🔨", n="Usta inşaatçı", d="5 bina kur", ok=()=>S.units.Count>=5, g=2 },
            new AchDef{ id="binahepsi", e="🏗️", n="Çiftlik kompleksi", d="Bütün binaları kur", ok=()=>B.Keys.All(u=>Fun_HasUnit(u)), g=10 },
            new AchDef{ id="sv5", e="⭐", n="Yeni başlayan değil", d="Bir binayı 5. seviyeye çıkar", ok=()=>S.units.Values.Any(s=>s!=null&&s.l>=5), g=5 },
            new AchDef{ id="arazi", e="🗺️", n="Toprak ağası", d="Bütün arazileri satın al", ok=()=>REG.Keys.All(r=>Get(S.regions,r)>0), g=10 },
            new AchDef{ id="konak", e="🏰", n="Konak sahibi", d="Evini Konağa büyüt", ok=()=>S.house>=4, g=8 },
            new AchDef{ id="sev50", e="💗", n="Hayvan dostu", d="Hayvanları 50 kez sev", ok=()=>AchSt("pet")>=50, g=3 },
            new AchDef{ id="panayir", e="🎪", n="Panayır birincisi", d="Bir panayır yarışmasını kazan", ok=()=>AchSt("contest_win")>=1, g=3 },
            new AchDef{ id="panayir10", e="🥇", n="Kupa avcısı", d="10 panayır yarışması kazan", ok=()=>AchSt("contest_win")>=10, g=8 },
            new AchDef{ id="koleksiyon5", e="📖", n="Meraklı", d="Koleksiyonda 5 buluntu", ok=()=>COL.Count(c=>Get(S.album,c.id)>0)>=5, g=4 },
            new AchDef{ id="dost", e="❤️", n="Can dostu", d="Bir kasabalıyla can dostu ol", ok=()=>NAMES.Any(n=>FrLv(n)>=3), g=5 },
            new AchDef{ id="hikaye", e="📚", n="Hikâye anlatıcısı", d="Bir kasabalının hikâyesini bitir", ok=()=>(S.quests??new Dictionary<string,int>()).Values.Any(v=>v>=3), g=5 },
            new AchDef{ id="olay10", e="🎲", n="Maceracı", d="10 rastgele olaya katıl", ok=()=>AchSt("event")>=10, g=4 },
            new AchDef{ id="tilki", e="🦊", n="Tilki kovalayan", d="Tilkiyi 3 kez kovala", ok=()=>AchSt("fox")>=3, g=3 },
            new AchDef{ id="mevsim", e="🍂", n="Dört mevsim", d="Dört mevsim ürününün hepsini hasat et", ok=()=>new[]{"lale","karpuz","patates","lahana"}.All(k=>AchSt("make_"+k)>0), g=6 },
            new AchDef{ id="restorasyon", e="🏛️", n="Kasabanın hayırseveri", d="Bir kasaba projesini tamamla", ok=()=>(S.proj??new Dictionary<string,double>()).Count>=1, g=5 },
            new AchDef{ id="nesil", e="🌳", n="Yeni nesil", d="Çiftliği yeni nesle devret", ok=()=>S.gen>=1, g=10 },
        };
        public static List<AchDef> AchReady() { if (S.ach == null) S.ach = new Dictionary<string, int>(); return ACH.Where(a => Get(S.ach, a.id) == 0 && a.ok()).ToList(); }
        static string achSeen = "";
        public static void AchTick()
        {
            var r = AchReady(); string sig = string.Join(",", r.Select(a => a.id));
            if (sig != "" && sig != achSeen)
            {
                var seen = achSeen.Split(','); var nw = r.Where(a => !seen.Contains(a.id)).ToList();
                if (nw.Count > 0 && achSeen != "") Toast($"🏅 Başarım: {nw[0].e} {nw[0].n} · ödülünü Koleksiyon'dan al");
            }
            achSeen = sig;
        }
        public static void ClaimAch(string id)
        {
            var a = ACH.Find(x => x.id == id); if (S.ach == null) S.ach = new Dictionary<string, int>();
            if (a == null || Get(S.ach, id) > 0 || !a.ok()) return;
            S.ach[id] = 1; S.gems += a.g; Sfx("win"); Toast($"🏅 {a.n} · +🍀{a.g}"); Commit();
        }
        public static void AchHTML(Transform parent)
        {
            AchReady(); int done = ACH.Count(a => Get(S.ach, a.id) > 0);
            Fun_Note(parent, $"{done} / {ACH.Count} başarım. Koşulu sağladığın başarımın ödülünü buradan al.");
            var list = UI.VStack(parent, 6, null, TextAnchor.UpperLeft, "achs");
            foreach (var a in ACH)
            {
                bool got = Get(S.ach, a.id) > 0, ok = !got && a.ok();
                // .ach{gap:10px;border:2px ink;border-radius:12px;background:card;padding:6px 10px;opacity:.7} .ok{opacity:1;#fff3c8} .got{opacity:1;#e8f3d8}
                var row = UI.Box(list, got ? UI.Hex("#e8f3d8") : ok ? UI.Hex("#fff3c8") : UI.C.card, 12, 2, UI.C.ink, null, "ach");
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.padding = UI.Pad(10, 10, 6, 6); h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
                if (!got && !ok) row.gameObject.AddComponent<CanvasGroup>().alpha = .7f;
                UI.Txt(row, a.e, 24, null, false, TextAnchor.MiddleCenter, false);
                var at = UI.VStack(row, 0, null, TextAnchor.MiddleLeft, "at"); UI.Size(at, -1, -1, 1);
                UI.Txt(at, $"<b>{a.n}</b>", 14, UI.C.text, false);
                UI.Txt(at, a.d, 11.5f, UI.C.muted, true);
                if (got) UI.Txt(row, "✓", 10.5f, UI.C.muted, true, TextAnchor.MiddleCenter, false);
                else { string id = a.id; Fun_Pill(row, $"🍀{a.g}", () => Act("ach", D("id", id)), ok ? "gold" : "", ok); }
            }
        }
        public static void FunTick() { EvtTick(); AchTick(); }

        // Koleksiyon sayfasına "Başarımlar" sekmesi (17-collection'ın sheetAlbum'u SheetAlbumCol adıyla G1'de)
        public static void SheetAlbum(Transform parent)
        {
            string tb = View != null && View.tab != null ? View.tab : "c"; int n = AchReady().Count;
            var tabs = new List<string[]> { new[] { "c", "📖 Koleksiyon" }, new[] { "ach", "🏅 Başarımlar" + (n > 0 ? " (" + n + ")" : "") } };
            if (tb == "ach")
            {
                Head(parent, "🏅", "Başarımlar", $"{ACH.Count(a => Get(S.ach, a.id) > 0)} / {ACH.Count}");
                var body = Fun_ShBody(parent); Fun_Tabs(body, tabs, tb); AchHTML(body);
                return;
            }
            SheetAlbumCol(parent);
            // .replace('<div class="sh-body">','<div class="sh-body">'+tabs): sekmeler gövdenin en başına
            var sb = Fun_Find(parent, "sh-body"); Transform host = parent;
            if (sb != null) { var sr = sb.GetComponent<ScrollRect>(); host = sr != null && sr.content != null ? sr.content : sb; }
            var tbox = Fun_Tabs(host, tabs, tb); tbox.SetAsFirstSibling();
        }

        static void Init_40()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("quest", d => DoQuest(Arg(d, "n")));
            // RegisterAct("ach", d => ClaimAch(Arg(d, "id")));
        }
    }
}
