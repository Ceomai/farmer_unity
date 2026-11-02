// ================= ÇİFTLİK İŞLEMLERİ (05-farm-actions.js) =================
// x, y: ekran konumu (CSS px, sol üstten). null → JS'teki "x==null".
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public static string PopLoc = null;
        public static int? PopPlot = null;

        static Unit Farm_U(string u) => GetOr(S.units, u);

        public static void BuildUnit(string u)
        {
            var d = B[u]; if (Farm_U(u) != null || S.lv < d.lvl) return; if (S.coins < d.cost) { Toast("Yeterli paran yok"); return; }
            Pay(d.cost, "İnşaat"); S.units[u] = new Unit { l = 1, built = Now() }; MarkRecent(LocOf(u), B[u].n); Sfx("build"); Puff(UnitWorld(u, .4f), 14, 1.3f);
            AddXP(Round(d.cost / 50) + 2); Stat("built"); PopLoc = LocOf(u); Toast(d.n + " kuruldu!"); Commit();
        }
        // Yükseltme maliyeti her seviyede iki katına çıkar
        public static double UpCost(string u) => Round(B[u].cost * 1.2 * Math.Pow(2, S.units[u].l - 1));
        public static void UpgradeUnit(string u)
        {
            var s = Farm_U(u); if (s == null || s.l >= MAXL || UpBlock(s.l + 1) != "") return; double c = UpCost(u); if (S.coins < c) { Toast("Yeterli paran yok"); return; }
            Pay(c, "İnşaat"); s.l++; s.built = Now(); MarkRecent(LocOf(u), B[u].n); Sfx("build"); if (Mode != "interior") Puff(UnitWorld(u, .5f), 12, 1.2f); AddXP(Round(c / 50)); PopLoc = LocOf(u);
            Toast(B[u].n + " " + s.l + ". seviyeye çıktı"); Commit(); if (Interior != null && Interior.key == u) BuildInterior(u);
        }
        public static void UpgradeHouse()
        {
            var h = S.house >= 0 && S.house < HOUSE.Count ? HOUSE[S.house] : null; if (h == null || S.lv < h.lvl) return; if (S.coins < h.cost) { Toast("Yeterli paran yok"); return; }
            Pay(h.cost, "İnşaat"); S.house++; MarkRecent("ev", HOUSE[S.house - 1].n); Sfx("build"); AddXP(Round(h.cost / 40)); PopLoc = "ev"; Toast(HOUSE[S.house - 1].n + " hazır!"); Commit();
            if (Interior != null && Interior.key == "kulube") BuildInterior("kulube");
        }
        public static void UpgradeFields()
        {
            if (S.fieldLv < 0 || S.fieldLv >= FIELD_UP.Count || S.fieldLv >= MAXL || UpBlock(S.fieldLv + 1) != "") return; double c = FIELD_UP[S.fieldLv];
            if (S.coins < c) { Toast("Yeterli paran yok"); return; }
            Pay(c, "İnşaat"); S.fieldLv++; AddXP(Round(c / 50)); PopLoc = "tarla"; Toast("Tarlalar " + S.fieldLv + ". seviyede"); Commit();
        }
        public static void StartUnit(string u, string rid, bool quiet = false)
        {
            var s = Farm_U(u); if (s == null || s.job != null || B[u].recipes == null) return; var r = B[u].recipes.Find(x => x.id == rid); if (r == null || r.lvl > S.lv || (r.col != null && !Has1(r.col))) return;
            if (!CanPay(r)) { if (!quiet) Toast("Malzeme ya da para eksik"); return; }
            Pay(r.cost, "Üretim"); if (r.@in != null) foreach (var kv in r.@in) S.items[kv.Key] = Has(kv.Key) - kv.Value;
            var sc = Scaled(r, s.l); double t = sc.t * (u == "mandira" && Has1("bakir_kazan") ? .9 : 1); s.job = new Job { r = rid, s = Now(), e = Now() + t * 1000 }; s.last = rid; Stat("plant_" + rid);
            if (!quiet) { Sfx("plant"); Commit(); }
        }
        // Üretim kuyruğu: bina çalışırken en fazla 3 iş sıraya alınır; biri bitip toplanınca sıradaki kendiliğinden başlar.
        // Malzeme ve para işin başladığı anda alınır; o an eksikse iş sırada bekler.
        public const int QMAX = 3;
        public static void QueueAdd(string u, string rid)
        {
            var s = Farm_U(u); if (s == null || B[u].recipes == null) return; var r = B[u].recipes.Find(x => x.id == rid); if (r == null || r.lvl > S.lv || (r.col != null && !Has1(r.col))) return;
            if (s.job == null) { StartUnit(u, rid); return; }
            if (s.q == null) s.q = new List<string>(); if (s.q.Count >= QMAX) { Toast("Sırada en fazla " + QMAX + " iş olabilir"); return; }
            s.q.Add(rid); Sfx("click"); Commit();
        }
        public static void QueueDel(string u, int i) { var s = Farm_U(u); if (s == null || s.q == null) return; if (i >= 0 && i < s.q.Count) s.q.RemoveAt(i); Commit(); }
        public static bool QueueNext(string u, bool quiet = true)
        {
            var s = Farm_U(u); if (s == null || s.job != null || s.q == null || s.q.Count == 0) return false; StartUnit(u, s.q[0], true);
            if (s.job != null) { s.q.RemoveAt(0); return true; }
            return false;
        }
        public static int QueueAll() { int n = 0; foreach (var u in S.units.Keys.ToList()) if (QueueNext(u)) n++; return n; }

        public static bool GiveOut(Dictionary<string, int> @out, float? x, float? y, double xp, bool quiet)
        {
            int n = @out.Values.Sum(), ns = @out.Where(kv => kv.Key != "at").Sum(kv => kv.Value);
            if (ns > 0 && Stored() + ns > Cap()) { if (!quiet) Toast("Ambar dolu! Pazarda sat ya da siloyu büyüt."); return false; }
            foreach (var kv in @out) S.items[kv.Key] = Has(kv.Key) + kv.Value; AddXP(xp); if (quiet) { AutoGain += n; return true; }
            Sfx("pop"); PLATFORM.Hap(); if (n >= 6) Celebrate();
            if (x != null) { FloatXY(x.Value, y ?? 0, string.Join("  ", @out.Select(kv => "+" + kv.Value + " " + ITEMS[kv.Key].e)) + "  +" + Hlp_N(xp) + " xp"); FlyItems(x.Value, y ?? 0, @out); }
            return true;
        }
        // Ahırda en fazla 6 tay bekler; fazlası pazar fiyatından kendiliğinden satılır (ambar dolmasın, üretim durmasın)
        public const int HORSE_KEEP = 6;
        public static void ExtraHorses(bool quiet)
        {
            int n = Has("at") - HORSE_KEEP; if (n <= 0) return; double pr = SellPrice("at", n); S.items["at"] -= n; AddSat("at", n); Gain(pr * n, "Pazar"); Stat("sold", n);
            if (!quiet) Toast("🐎 Ahır dolu: " + n + " tay satıldı · +₺" + Fmt(pr * n));
        }
        public static bool CollectUnit(string u, float? x = null, float? y = null, bool quiet = false)
        {
            var s = Farm_U(u); if (s == null || s.job == null || Now() < s.job.e) return false; var r = UnitRecipe(u); var sc = Scaled(r, s.l); var o = new Dictionary<string, int>(sc.@out);
            string cb; if (COL_BONUS.TryGetValue(u, out cb) && Has1(cb)) { var k0 = o.Keys.First(); o[k0] += 1; }
            if (LoveBonus(u)) { var k0 = o.Keys.First(); o[k0] += 1; }
            if (!quiet && x == null) { var p = ScreenOfW(UnitWorld(u, MarkH(u))); x = p.x; y = p.y; }
            if (!GiveOut(o, x, y, Math.Ceiling(r.t / 12) + 1, quiet)) return !quiet; Stat("harvest_" + u); Stat("make_" + r.id); s.job = null; TryDrop(u); if (u == "hara") ExtraHorses(quiet); QueueNext(u); if (quiet) return true;
            Sfx(GetOr(UNIT_SFX, u)); if (Mode != "interior") Puff(UnitWorld(u, 1), 8); var lk = LocOf(u); if (lk != null && GetOr(LocGroups, lk) != null) Squash(lk, u); Commit(); return true;
        }
        public static bool PlantPlot(int i, string rid, bool quiet = false)
        {
            var f = i >= 0 && i < S.fields.Count ? S.fields[i] : null; if (f == null || f.job != null) return false; var r = CropOf(rid); if (r == null || CropLocked(r)) return false;
            if (S.coins < r.cost) { if (!quiet) Toast("Tohum için paran yetmiyor"); return false; }
            Pay(r.cost, "Tohum"); var sc = Scaled(r, S.fieldLv); f.job = new Job { r = rid, s = Now(), e = Now() + sc.t * 1000 }; S.lastCrop = rid; Stat("plant_" + rid); if (!quiet) Sfx("plant"); return true;
        }
        public static bool HarvestPlot(int i, float? x = null, float? y = null, bool quiet = false)
        {
            var f = i >= 0 && i < S.fields.Count ? S.fields[i] : null; if (f == null || f.job == null || Now() < f.job.e) return false; var r = CropOf(f.job.r); var sc = Scaled(r, S.fieldLv);
            if (!quiet && x == null) { var p = ScreenOfW(PlotWorld(i, 1.5f)); x = p.x; y = p.y; }
            if (!GiveOut(sc.@out, x, y, Math.Ceiling(r.t / 12) + 1, quiet)) return false; Stat("harvest_field"); Stat("make_" + r.id); f.job = null; TryDrop("field"); if (!quiet) Puff(PlotWorld(i, .6f), 6, .8f); return true;
        }
        public static void PlotTap(int i, float x, float y)
        {
            var f = i >= 0 && i < S.fields.Count ? S.fields[i] : null; if (f == null) return;
            if (f.job != null && Now() >= f.job.e) { HarvestPlot(i, x, y); Commit(); }
            else if (f.job == null) { if (PlantPlot(i, S.lastCrop)) { FloatXY(x, y, ITEMS[S.lastCrop].e + " ekildi"); Commit(); } }
            else Toast(CropOf(f.job.r).n + " büyüyor · " + Secs(Math.Ceiling((f.job.e - Now()) / 1000)));
        }
        public static void PlantAll()
        {
            int n = 0; for (int i = 0; i < S.fields.Count; i++) if (S.fields[i].job == null && PlantPlot(i, S.lastCrop)) n++;
            if (n > 0) Toast(n + " parsele " + CropOf(S.lastCrop).n + " ekildi"); Commit();
        }
        public static void HarvestAll()
        {
            int n = 0; for (int i = 0; i < S.fields.Count; i++) { var f = S.fields[i]; if (f.job != null && Now() >= f.job.e && HarvestPlot(i)) n++; }
            if (n >= 3) Celebrate(n >= 6 ? "Bereket yağdı!" : "Bereketli!"); Commit();
        }
        public static void BuyPlot()
        {
            int n = S.fields.Count; if (n >= 9) return; if (S.lv < PLOT_LV[n]) { Toast("Yeni parsel için seviye " + PLOT_LV[n] + " gerekli"); return; }
            if (S.coins < PLOT_COST[n]) { Toast("Yeterli paran yok"); return; }
            Pay(PLOT_COST[n], "Arazi"); S.fields.Add(new Field()); Sfx("build"); Puff(PlotWorld(n, .4f), 10); PopPlot = n; AddXP(Round(PLOT_COST[n] / 40)); Toast("Yeni tarla parseli!"); Commit();
        }
        public static void BuyRegion(string r)
        {
            var d = REG[r]; if (Owned(r) || S.lv < d.lvl) return; if (S.coins < RegCost(r)) { Toast("Yeterli paran yok"); return; }
            Pay(RegCost(r), "Arazi"); S.regions[r] = 1;
            { var lk = LOC.Keys.FirstOrDefault(k => LOC[k].reg == r); if (lk != null) MarkRecent(lk, REG[r].n + " arazisi"); }
            Sfx("whoosh");
            if (d.rect != null) { float x1 = d.rect[0], z1 = d.rect[1], x2 = d.rect[2], z2 = d.rect[3]; Puff(V((x1 + x2) / 2, 1, (z1 + z2) / 2), 18, 3); }
            AddXP(Round(d.cost / 60)); Toast(d.n + " artık senin!"); View = null; RenderSheet(); Commit();
        }
    }
}
