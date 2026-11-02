// ================= PANAYIR (20-fair.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Ciftlik
{
    public class ContestDef
    {
        public string id, n, e;
        public Dictionary<string, int> need;
        [JsonIgnore] public Func<double> sk;
    }
    // Yarışma sonucu satırı: {n, s, me}
    public class ContestEntry { public string n; public double s; public int me; }

    // JS'te done[id].all yarışanlar dizisidir; çekirdekteki ContestResult.all sayı olarak tanımlandığından
    // dizi "entrants" alanında tutulur, "all" yarışan sayısını taşır.
    public partial class ContestResult { public List<ContestEntry> entrants; }

    public static partial class Game
    {
        public static int LvOf(string u) { Unit s; return S.units.TryGetValue(u, out s) && s != null ? s.l : 0; }

        static Dictionary<string, int> Fair_N(string k, int n) => new Dictionary<string, int> { { k, n } };
        public static readonly List<ContestDef> CONTESTS = new List<ContestDef> {
            new ContestDef { id = "kabak", n = "En Büyük Kabak", e = "🎃", need = Fair_N("kabak", 1), sk = () => S.fieldLv * 12 },
            new ContestDef { id = "peynir", n = "Peynir Yarışması", e = "🧀", need = Fair_N("peynir", 2), sk = () => LvOf("mandira") * 12 },
            new ContestDef { id = "ekmek", n = "Ekmek Yarışması", e = "🍞", need = Fair_N("ekmek", 2), sk = () => LvOf("firin") * 12 },
            new ContestDef { id = "inek", n = "En Güzel İnek", e = "🐄", need = Fair_N("sut", 5), sk = () => LvOf("ahir") * 13 },
            new ContestDef { id = "bal", n = "Bal Tadımı", e = "🍯", need = Fair_N("bal", 3), sk = () => LvOf("kovan") * 12 },
            new ContestDef { id = "yun", n = "Yün Eğirme", e = "🧶", need = Fair_N("yun", 4), sk = () => LvOf("agil") * 12 },
            new ContestDef { id = "domates", n = "Domates Güzeli", e = "🍅", need = Fair_N("domates", 6), sk = () => S.fieldLv * 12 },
            new ContestDef { id = "yumurta", n = "Yumurta Sepeti", e = "🥚", need = Fair_N("yumurta", 8), sk = () => LvOf("kumes") * 12 },
            new ContestDef { id = "bugday", n = "Başak Demeti", e = "🌾", need = Fair_N("bugday", 10), sk = () => S.fieldLv * 12 },
            new ContestDef { id = "uzum", n = "Üzüm Salkımı", e = "🍇", need = Fair_N("uzum", 12), sk = () => LvOf("bag") * 13 },
            new ContestDef { id = "pekmez", n = "Pekmez Tadımı", e = "🍶", need = Fair_N("pekmez", 2), sk = () => LvOf("pekmezhane") * 13 },
        };

        // JS: arr.sort(()=>Math.random()-.5) — rastgele karıştırma
        internal static List<T> G2_Shuffle<T>(IEnumerable<T> src)
        {
            var a = src.ToList();
            for (int i = a.Count - 1; i > 0; i--) { int j = FloorI(Rnd() * (i + 1)); var t = a[i]; a[i] = a[j]; a[j] = t; }
            return a;
        }

        public static void EnsureContests()
        {
            var t = TodayStr();
            if (S.contests.day == t && S.contests.list.Count > 0) return;
            var pool = Producible();
            var ok = CONTESTS.Where(c => c.need.Keys.All(k => pool.Contains(k)));
            var list = G2_Shuffle(ok).Take(3).Select(c => c.id).ToList();
            var wk = WeeklyContest();
            if (!string.IsNullOrEmpty(wk) && !list.Contains(wk)) { if (list.Count > 0) list.RemoveAt(list.Count - 1); list.Insert(0, wk); }
            S.contests = new Contests { day = t, list = list, done = new Dictionary<string, ContestResult>() };
        }

        public static void EnterContest(string id)
        {
            var c = CONTESTS.Find(x => x.id == id);
            if (c == null || S.contests.done.ContainsKey(id)) return;
            foreach (var kv in c.need) if (Has(kv.Key) < kv.Value) { UI.Toast("Eksik ürün var"); return; }
            foreach (var kv in c.need) S.items[kv.Key] -= kv.Value;
            double me = Round(c.sk() + 25 + Rnd() * 45);
            var opp = new[] { 0, 1, 2 }.Select(_ => Round(30 + Rnd() * 45 + S.lv * 1.5)).ToList();
            var names = new[] { "Hacer Hanım", "Muhtar Cemil", "Komşu Ali" };
            var all0 = new List<ContestEntry> { new ContestEntry { n = "Sen", s = me, me = 1 } };
            for (int i = 0; i < opp.Count; i++) all0.Add(new ContestEntry { n = names[i], s = opp[i] });
            var all = all0.OrderByDescending(x => x.s).ToList();   // kararlı sıralama (JS sort gibi)
            int place = all.FindIndex(x => x.me == 1) + 1;
            int wx = WeeklyContest() == id ? 2 : 1;
            var prizes = new[] { new[] { 0, 0 }, new[] { 600, 3 }, new[] { 250, 1 }, new[] { 100, 0 }, new[] { 0, 0 } };
            double pc = prizes[place][0] * wx; int pg = prizes[place][1] * wx;
            Gain(pc, "Panayır"); S.gems += pg; AddXP(place == 1 ? 60 : 20);
            if (place == 1)
            {
                Stat("contest_win"); S.rep = Math.Min(100, S.rep + 3);
                if (Get(S.album, "kupa") == 0)
                {
                    S.album["kupa"] = 1; S.rep = Math.Min(100, S.rep + 5); S.gems += 1;
                    SetTimeout(() => ShowFind(COL.Find(x => x.id == "kupa")), 700);
                }
            }
            S.contests.done[id] = new ContestResult { place = place, all = all.Count, entrants = all };
            Stat("contest");
            Sfx(place == 1 ? "win" : place <= 3 ? "coin" : "fail");
            if (pc != 0) FlyCoins(place == 1 ? 8 : 4);
            Commit();
        }

        public static void FairBody(Transform parent)
        {
            EnsureContests();
            int shown = 0;
            foreach (var id in S.contests.list)
            {
                var c = CONTESTS.Find(x => x.id == id); if (c == null) continue;
                shown++;
                ContestResult r; S.contests.done.TryGetValue(id, out r);
                if (r != null)
                {
                    var o = G2_Order(parent);
                    var medals = new[] { "", "🥇", "🥈", "🥉", "🎗️" };
                    G2_Who(o, c.e + " " + c.n, (r.place >= 0 && r.place < medals.Length ? medals[r.place] : "") + " " + r.place + ". oldun");
                    var nd = G2_Needs(o);
                    var ent = r.entrants ?? new List<ContestEntry>();
                    for (int j = 0; j < ent.Count; j++)
                    {
                        var x = ent[j];
                        if (x.me == 1) G2_Need(nd, null, $"{j + 1}. {UI.Esc(x.n)} · {G2_Num(x.s)}", false);
                        else G2_Need(nd, null, $"{j + 1}. {UI.Esc(x.n)} · {G2_Num(x.s)}", true, UI.Hex("#efe5cf"), UI.C.muted);
                    }
                    continue;
                }
                bool ok = c.need.All(kv => Has(kv.Key) >= kv.Value);
                bool wk = WeeklyContest() == id;
                var od = G2_Order(parent, wk ? UI.Hex("#fff3c8") : (Color?)null);
                G2_Who(od, c.e + " " + c.n + (wk ? " <size=11><color=#8a5a00><b>⭐ Haftanın yarışması · ödül ×2</b></color></size>" : ""),
                    $"🥇 ₺{(wk ? 1200 : 600)} + 🍀{(wk ? 6 : 3)}");
                var needs = G2_Needs(od);
                foreach (var kv in c.need) G2_Need(needs, ITEMS[kv.Key].e, Math.Min(Has(kv.Key), kv.Value) + "/" + kv.Value, Has(kv.Key) < kv.Value);
                var ur = G2_Urow(od);
                G2_PillAct(ur, "🎪 Katıl", "contest", "gold", ok, "id", id);
                G2_Tm(ur, "Bina seviyesi puanı artırır");
            }
            if (shown == 0) G2_Note(parent, "Yarışmalara katılmak için önce ürün çeşitliliğini artır.");
        }

        static void Init_20()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("contest", d => EnterContest(Arg(d, "id")));
        }
    }
}
