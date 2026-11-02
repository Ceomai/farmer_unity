// ================= KASABA RESTORASYONU VE YENİ NESİL (41-content.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class ProjDef { public string id, e, n; public int lv; public double c; public Dictionary<string, int> need; public string bonus; }

    public static partial class Game
    {
        // --- Kasaba projeleri: büyük para + ürün katkısıyla tamamlanır, kalıcı bonus verir (geç oyun para yutağı) ---
        public static readonly List<ProjDef> PROJ = new List<ProjDef> {
            new ProjDef{ id="cesme", e="⛲", n="Köy çeşmesi", lv=8, c=15000, need=Fun_D("bal",5,"sut",10), bonus="Hayvanların mutluluğu iki kat yavaş azalır" },
            new ProjDef{ id="okul", e="🏫", n="Köy okulu", lv=12, c=50000, need=Fun_D("ekmek",20,"yumurta",30), bonus="Sipariş ve sözleşme ödülleri +%10" },
            new ProjDef{ id="kopru", e="🌉", n="Taş köprü", lv=15, c=120000, need=Fun_D("kumas",10,"yag",6), bonus="Sözleşme ödülleri ayrıca +%15" },
            new ProjDef{ id="pazaryeri", e="🏛️", n="Kapalı pazar", lv=18, c=250000, need=Fun_D("peynir",15,"recel",10), bonus="Pazar iki kat geç doyar" },
            new ProjDef{ id="saat", e="🕰️", n="Saat kulesi", lv=22, c=500000, need=Fun_D("baklava",5,"hali",2), bonus="Bütün üretim %5 daha hızlı" },
            new ProjDef{ id="tribun", e="🏟️", n="Hipodrom tribünü", lv=26, c=900000, need=Fun_D("at",3,"yorgan",2), bonus="At yarışı ödülleri +%50" },
        };
        public static bool ProjOn(string id) => S != null && S.proj != null && S.proj.ContainsKey(id) && S.proj[id] != 0;
        public static void ProjBody(Transform parent)
        {
            if (S.proj == null) S.proj = new Dictionary<string, double>();
            Fun_Note(parent, "Kasabayı birlikte güzelleştirin: her proje büyük bir bağış ve açılış ikramı ister, karşılığında kalıcı bir avantaj verir.");
            foreach (var p in PROJ)
            {
                bool done = ProjOn(p.id), lk = S.lv < p.lv, ok = !done && !lk && S.coins >= p.c && p.need.All(e => Has(e.Key) >= e.Value);
                // .order{border:2px ink;border-radius:14px;background:card;box-shadow:2px 3px 0 rgba(42,26,16,.9);padding:8px 10px;gap:6px} .pdone{#e8f3d8}
                var box = UI.Box(parent, done ? UI.Hex("#e8f3d8") : UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "order");
                var v = box.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 6; v.padding = UI.Pad(10, 10, 8, 8);
                v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                // .order .who{justify-content:space-between;font-weight:900;font-size:13.5px} span{font-size:12px;color:forest}
                var who = UI.HStack(box, 6, TextAnchor.MiddleLeft, null, "who");
                var wt = UI.Txt(who, $"{p.e} {p.n}", 13.5f, UI.C.text, true); UI.Size(wt, -1, -1, 1);
                UI.Txt(who, done ? "✓ Açıldı" : lk ? "🔒 Sv " + p.lv : "₺" + Fmt(p.c), 12, UI.C.forest, true, TextAnchor.MiddleRight, false);
                var bn = Fun_Note(box, p.bonus); UI.Size(bn, -1, -1, -1, -1, -1, -1);
                if (!done)
                {
                    Fun_Needs(box, p.need);
                    var urow = UI.HStack(box, 7, TextAnchor.MiddleLeft, null, "urow");
                    string id = p.id;
                    Fun_Pill(urow, "🏗️ Bağışla ve aç", () => Act("proj", D("id", id)), "gold", ok);
                }
            }
        }
        public static void DoProj(string id)
        {
            var p = PROJ.Find(x => x.id == id); if (p == null || ProjOn(id) || S.lv < p.lv) return;
            if (S.coins < p.c) { Toast("Yeterli paran yok"); return; }
            foreach (var kv in p.need) if (Has(kv.Key) < kv.Value) { Toast("Eksik ürün var"); return; }
            Pay(p.c, "Kasaba"); foreach (var kv in p.need) S.items[kv.Key] -= kv.Value;
            if (S.proj == null) S.proj = new Dictionary<string, double>();
            S.proj[id] = Now(); S.rep = Math.Min(100, S.rep + 5); AddXP(Round(p.c / 200)); Stat("proj"); Sfx("win"); Celebrate(p.n + " açıldı!");
            RectTransform d = null;
            d = Fun_Modal(card =>
            {
                Fun_Badge(card, p.e); Fun_H2(card, $"{p.n} açıldı!");
                Fun_P(card, $"Kasaba sana minnettar. ★ +5\n<b>{p.bonus}</b>");
                UI.Btn(card, "Harika", () => UI.Kill(d), "big gold");
            });
            Commit();
        }

        // --- Yeni nesil: Konak ve seviye 30'dan sonra çiftlik yeni nesle devredilir ---
        // Sıfırlanır: para, ürünler, binalar, araziler, ev, tarla, seviye. Kalır: yonca, koleksiyon, başarımlar, süsler, dostluklar, hikâyeler, kasaba projeleri, ayarlar.
        // Kazanılır: her nesil kalıcı +%10 üretim ve 10 yonca.
        public const int GEN_LV = 30;
        public static bool GenReady() => S.lv >= GEN_LV && S.house >= 4;
        public static void GenHTML(Transform parent)
        {
            int g = S.gen;
            // .unit{flex-direction:column;gap:8px} .uhead{justify-content:space-between;font-family:display;font-size:14px;color:#3a2414}
            var unit = UI.VStack(parent, 8, null, TextAnchor.UpperLeft, "unit");
            var uh = UI.HStack(unit, 6, TextAnchor.MiddleLeft, null, "uhead");
            var ut = UI.Txt(uh, "🌳 Yeni nesil", 14, UI.Hex("#3a2414"), false, TextAnchor.MiddleLeft, false, true); UI.Size(ut, -1, -1, 1);
            UI.Txt(uh, g != 0 ? g + ". nesil · üretim +%" + g * 10 : "", 10.5f, UI.C.muted, true, TextAnchor.MiddleRight, false);
            Fun_Note(unit, "Çiftliği çocuklarına devret: baştan başlarsın ama bütün üretim kalıcı olarak +%10 artar. Yonca, koleksiyon, başarımlar, süsler, dostluklar ve kasaba projeleri seninle kalır.");
            if (GenReady()) UI.ActBtn(unit, "🌳 Yeni nesle devret (+%10 üretim, +🍀10)", "gen", "big gold");
            else UI.Btn(unit, $"🔒 Konak + seviye {GEN_LV} gerekli", null, "big dis");
        }
        public static void AskGen()
        {
            if (!GenReady()) return;
            RectTransform d = null;
            d = Fun_Modal(card =>
            {
                Fun_Badge(card, "🌳"); Fun_H2(card, "Yeni nesle devret?", 24);
                Fun_P(card, "<b>Sıfırlanır:</b> para, ürünler, binalar, araziler, ev, tarlalar, seviye.\n<b>Kalır:</b> 🍀 yonca, koleksiyon, başarımlar, süsler, dostluklar, hikâyeler, kasaba projeleri.\n<b>Kazanılır:</b> kalıcı +%10 üretim ve +🍀10.", 13, TextAnchor.MiddleLeft);
                UI.Btn(card, "Evet, devret", () => { UI.Kill(d); DoGen(); }, "big gold");
                UI.Btn(card, "Vazgeç", () => UI.Kill(d), "ghost");
            });
        }
        // Ana hedefler, günlük ödül/görevler, reklam hakkı ve günün panayırı da korunur (yeni nesilde tekrar alınamasın)
        public static void DoGen()
        {
            if (!GenReady()) return;
            if (Race != null && Race.running) { Toast("Önce süren yarışın bitmesini bekle"); return; }
            var o = S; var n = Fresh();
            // keep: gems, album, ach, decor, friends, quests, proj, sound, consent, ui, stats, keepN, autoSell, fastNav, created, goal, daily, ads, contests, colDone
            n.gems = o.gems;
            if (o.album != null) n.album = o.album;
            if (o.ach != null) n.ach = o.ach;
            if (o.decor != null) n.decor = o.decor;
            if (o.friends != null) n.friends = o.friends;
            if (o.quests != null) n.quests = o.quests;
            if (o.proj != null) n.proj = o.proj;
            if (o.sound != null) n.sound = o.sound;
            if (o.consent != null) n.consent = o.consent;
            if (o.ui != null) n.ui = o.ui;
            if (o.stats != null) n.stats = o.stats;
            n.keepN = o.keepN; n.autoSell = o.autoSell; n.fastNav = o.fastNav; n.created = o.created; n.goal = o.goal;
            if (o.daily != null) n.daily = o.daily;
            if (o.ads != null) n.ads = o.ads;
            if (o.contests != null) n.contests = o.contests;
            JToken cd;
            if (o.extra != null && o.extra.TryGetValue("colDone", out cd) && cd != null && cd.Type != JTokenType.Null)
            {
                if (n.extra == null) n.extra = new Dictionary<string, JToken>();
                n.extra["colDone"] = cd;
            }
            int g = o.gen + 1;
            n.gen = g; n.tut = new Tut { done = 1 }; n.tut2 = new Tut { done = 1 };
            S = n;
            S.gems = S.gems + 10; S.stats["gens"] = Get(S.stats, "gens") + 1;
            RefreshMarket(true); EnsureOrders(); Save(); RegSig = ""; View = null; ToHub(); SyncWorld(true); RenderHUD(); Celebrate(g + ". nesil!");
            Toast($"🌳 {g}. nesil başladı: bütün üretim +%{g * 10}"); Track("prestige", new Dictionary<string, object> { { "gen", g } });
        }

        static void Init_41()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("proj", d => DoProj(Arg(d, "id")));
            // RegisterAct("gen", d => AskGen());
        }
    }
}
