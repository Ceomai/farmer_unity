// ================= PAZAR / SİPARİŞ / HEDEF (04-economy.js) =================
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ciftlik
{
    public class GoalDef { public string d; public Func<bool> ok; public double r; }

    public static partial class Game
    {
        public static void RefreshMarket(bool force = false)
        {
            if (!force && Now() - S.market.at < 180000) return; S.market.at = Now();
            if (S.market.h == null) S.market.h = new Dictionary<string, List<double>>();
            if (S.market.m == null) S.market.m = new Dictionary<string, double>();
            foreach (var k in ITEMS.Keys)
            {
                S.market.m[k] = Round((0.8 + Rnd() * 0.45) * 100) / 100;   // +(…).toFixed(2)
                List<double> h; if (!S.market.h.TryGetValue(k, out h) || h == null) { h = new List<double>(); S.market.h[k] = h; }
                h.Add(S.market.m[k]); if (h.Count > 8) h.RemoveAt(0);
            }
        }
        // Pazar doygunluğu: aynı üründen çok satınca fiyat düşer (en fazla %60), 15 dakikada yarıya iner.
        // Satılan değer (₺) üzerinden ölçülür; oyuncu büyüdükçe pazar da büyür.
        public static double SatNow(string k)
        {
            SatEntry s = S.market.sat != null ? GetOr(S.market.sat, k) : null;
            if (s == null || !IsFinite(s.v) || !IsFinite(s.t)) return 0;
            return Math.Max(0, s.v) * Math.Pow(.5, Math.Max(0, Now() - s.t) / 900000);   // saat geri alınsa da büyümez
        }
        public static double SatCap() => (1500 + 400 * S.lv) * (ProjOn("pazaryeri") ? 2 : 1);
        public static double SatF(string k, double add = 0) => Math.Max(.4, 1 / (1 + (SatNow(k) + add) / SatCap()));
        public static double BasePrice(string k) { double m = Get(S.market.m, k); return ITEMS[k].p * ((IsFinite(m) && m > 0) ? m : 1); }
        public static double SellPrice(string k, int n = 1)
        {
            double v = Round(BasePrice(k) * SatF(k, ITEMS[k].p * n / 2.0));
            if (!IsFinite(v) || v == 0) v = 1;
            return Math.Max(1, v);
        }
        public static void AddSat(string k, int n)
        {
            if (S.market.sat == null) S.market.sat = new Dictionary<string, SatEntry>();
            S.market.sat[k] = new SatEntry { v = SatNow(k) + ITEMS[k].p * n, t = Now() };
        }
        public static void Sell(string k, int n)
        {
            n = Math.Min(n, Has(k)); if (n <= 0) return; double pr = SellPrice(k, n); S.items[k] -= n; AddSat(k, n); Gain(pr * n, "Pazar"); Stat("sold", n); Sfx("coin"); FlyCoins(Math.Min(6, 2 + n));
            Toast(n + " " + ITEMS[k].n + " satıldı · +₺" + Fmt(pr * n) + (SatF(k) < .8 ? " · pazar bu üründen doydu, fiyat düşük" : "")); Commit();
        }
        // Fazlayı sat: her üründen "keep" kadar bırakıp gerisini satar (yem ve taylar hariç). Tek bir özet bildirim gösterir.
        public static double SellExcess(int keep, bool quiet = false)
        {
            double tot = 0; int n = 0;
            foreach (var k in S.items.Keys.ToList())
            {
                if (k == "yem" || k == "at") continue; int x = Has(k) - keep; if (x <= 0) continue;
                double pr = SellPrice(k, x); S.items[k] -= x; AddSat(k, x); tot += pr * x; n += x;
            }
            if (n == 0) { if (!quiet) Toast("Satılacak fazla ürün yok"); return 0; }
            Gain(tot, "Pazar"); Stat("sold", n); Sfx("coin"); FlyCoins(6); Toast((quiet ? "📦 Ambar dolmak üzereydi: " : "") + n + " ürün satıldı · +₺" + Fmt(tot)); Commit(); return tot;
        }
        // Ambar %95 dolunca (ayar açıksa) fazlası kendiliğinden satılır
        public static void AutoSellTick() { if (!S.autoSell || Stored() < Cap() * .95) return; SellExcess(S.keepN, true); }
        public static void BuyFeed(int n)
        {
            double c = ITEMS["yem"].buy * n; if (S.coins < c) { Toast("Yeterli paran yok"); return; }
            if (Stored() + n > Cap()) { Toast("Ambar dolu"); return; }
            Pay(c, "Yem"); S.items["yem"] = Has("yem") + n; Toast(n + " yem alındı"); Commit();
        }
        // Oyuncunun şu an üretebileceği ürünler (sipariş/sözleşme/yarışma bunlardan seçilir): koleksiyon tohumu bulunmamış ve mevsimi olmayan ürünler hariç
        public static List<string> Producible()
        {
            var set = new List<string>();
            void add(string k) { if (!set.Contains(k)) set.Add(k); }
            foreach (var c in CROPS) if (!CropLocked(c)) add(c.id);
            foreach (var u in S.units.Keys)
            {
                BuildingDef d; if (!B.TryGetValue(u, out d) || d.recipes == null) continue;
                foreach (var r in d.recipes) if (r.lvl <= S.lv && (r.col == null || Has1(r.col))) foreach (var k in r.@out.Keys) if (k != "at") add(k);
            }
            return set;
        }
        public static readonly List<string> NAMES = new List<string> { "Kasap Rıza", "Fırıncı Nuri", "Lokantacı Sevim", "Manav Hilmi", "Otelci Gülten", "Kooperatif", "Bakkal Ahmet", "Konakçı Halit Bey", "Pazarcı Zehra" };
        public static Order GenOrder()
        {
            var pool = Producible();
            int n = Math.Min(pool.Count, 1 + (int)Math.Floor(Rnd() * Math.Min(3, 1 + S.lv / 3.0))); var items = new Dictionary<string, int>(); double val = 0;
            // pool.sort(()=>Math.random()-.5): karıştır
            foreach (var k in pool.OrderBy(_ => Rnd()).Take(n).ToList())
            {
                int q = (int)Math.Max(1, Round((3 + Rnd() * 6) * Math.Pow(20 / Math.Max(ITEMS[k].p, 6), 0.5))); items[k] = q; val += ITEMS[k].p * q;
            }
            return new Order { who = NAMES[(int)Math.Floor(Rnd() * NAMES.Count)], items = items, @base = Round(val * 1.5), xp = Round(5 + val / 12) };
        }
        public static double RepMult() => (0.7 + 0.6 * S.rep / 100) * (Has1("kangal") ? 1.05 : 1) * (ProjOn("okul") ? 1.1 : 1);
        public static void EnsureOrders() { while (S.orders.Count < 3) S.orders.Add(GenOrder()); }
        public static void FillOrder(int i)
        {
            if (i < 0 || i >= S.orders.Count) return;
            var o = S.orders[i]; foreach (var kv in o.items) if (Has(kv.Key) < kv.Value) { Toast("Eksik ürün var"); return; }
            foreach (var kv in o.items) S.items[kv.Key] -= kv.Value; double r = Round(o.@base * RepMult() * FrMult(o.who)); Gain(r, "Sipariş"); AddXP(o.xp); Befriend(o.who, 1); S.rep = Math.Min(100, S.rep + 1); Stat("orders");
            S.orders[i] = GenOrder(); Sfx("coin"); FlyCoins(7); Toast(o.who + " memnun kaldı · +₺" + Fmt(r)); Commit();
        }
        public static int ReadyOrders() => S.orders.Count(o => o.items.All(kv => Has(kv.Key) >= kv.Value));

        static double Eco_St(string k) => Get(S.stats, k);
        static List<GoalDef> _goals;
        public static List<GoalDef> GOALS => _goals ?? (_goals = new List<GoalDef>
        {
            new GoalDef { d = "Tarlalar'a git, buğday ek", ok = () => Eco_St("plant_bugday") >= 1, r = 40 },
            new GoalDef { d = "Buğdayı hasat et", ok = () => Eco_St("harvest_field") >= 1, r = 40 },
            new GoalDef { d = "Kümese gir, tavukları besle ve yumurtaları topla", ok = () => Eco_St("harvest_kumes") >= 1, r = 50 },
            new GoalDef { d = "Köy Pazarı'nda ürün sat", ok = () => Eco_St("sold") >= 1, r = 60 },
            new GoalDef { d = "Tarlalar'da üçüncü parseli satın al", ok = () => S.fields.Count >= 3, r = 80 },
            new GoalDef { d = "Tüccar Hanı'nda bir sipariş teslim et", ok = () => Eco_St("orders") >= 1, r = 120 },
            new GoalDef { d = "Silo kur", ok = () => S.units.ContainsKey("silo"), r = 150 },
            new GoalDef { d = "Dere Kenarı arazisini satın al", ok = () => Owned("dere"), r = 200 },
            new GoalDef { d = "Değirmen Sokağı'nda yem değirmeni kur", ok = () => S.units.ContainsKey("yemhane"), r = 150 },
            new GoalDef { d = "Kulübeyi Taş Ev'e büyüt", ok = () => S.house >= 2, r = 300 },
            new GoalDef { d = "Ahır kur ve süt sağ", ok = () => Eco_St("harvest_ahir") >= 1, r = 300 },
            new GoalDef { d = "Bir binayı 2. seviyeye çıkar", ok = () => S.units.Values.Any(u => u.l >= 2), r = 400 },
            new GoalDef { d = "5 sipariş teslim et", ok = () => Eco_St("orders") >= 5, r = 600 },
            new GoalDef { d = "Kavaklık arazisini satın al", ok = () => Owned("kavak"), r = 800 },
            new GoalDef { d = "Mandıradan peynir çıkar", ok = () => Eco_St("make_peynir") >= 1, r = 800 },
            new GoalDef { d = "Çiftlik Evi'ne geç", ok = () => S.house >= 3, r = 1500 },
            new GoalDef { d = "Besi ahırından et al", ok = () => Eco_St("harvest_besi") >= 1, r = 2000 },
            new GoalDef { d = "Yarış Alanı'nda tayınla bir yarışa gir", ok = () => Eco_St("race") >= 1, r = 3000 },
            new GoalDef { d = "Konak inşa et", ok = () => S.house >= 4, r = 10000 },
            new GoalDef { d = "Tüm arazileri satın al", ok = () => REG.Keys.All(Owned), r = 25000 },
            new GoalDef { d = "Tarlaları 3. seviyeye çıkar", ok = () => S.fieldLv >= 3, r = 3000 },
            new GoalDef { d = "3 tedarik sözleşmesi tamamla", ok = () => Eco_St("contracts") >= 3, r = 3500 },
            new GoalDef { d = "Panayırda birinci ol", ok = () => Eco_St("contest_win") >= 1, r = 3500 },
            new GoalDef { d = "Koleksiyondan 6 parça bul", ok = () => COL.Count(c => Get(S.album, c.id) != 0) >= 6, r = 4000 },
            new GoalDef { d = "Mandırada tereyağı üret", ok = () => Eco_St("make_tereyag") >= 1, r = 4500 },
            new GoalDef { d = "25 sipariş teslim et", ok = () => Eco_St("orders") >= 25, r = 5000 },
            new GoalDef { d = "Reçel kaynat", ok = () => Eco_St("make_recel") + Eco_St("make_recel2") >= 1, r = 5000 },
            new GoalDef { d = "Bir binayı 4. seviyeye çıkar", ok = () => S.units.Values.Any(u => u.l >= 4), r = 6000 },
            new GoalDef { d = "Bal sabunu yap", ok = () => Eco_St("make_sabun") >= 1, r = 6000 },
            new GoalDef { d = "Bir kasabalıyla can dostu ol", ok = () => NAMES.Any(n => FrLv(n) >= 3), r = 7000 },
            new GoalDef { d = "Baklava pişir", ok = () => Eco_St("make_baklava") >= 1, r = 8000 },
            new GoalDef { d = "5 işçi çalıştır", ok = () => S.workers.Count >= 5, r = 8000 },
            new GoalDef { d = "El dokuma halı dokut", ok = () => Eco_St("make_hali") >= 1, r = 10000 },
            new GoalDef { d = "Bölge Ligi'ne yüksel", ok = () => S.league.tier >= 2, r = 10000 },
            new GoalDef { d = "Pastırma yap", ok = () => Eco_St("make_pastirma") >= 1, r = 12000 },
            new GoalDef { d = "Seviye 16'ya ulaş", ok = () => S.lv >= 16, r = 15000 },
            new GoalDef { d = "Tarlaları 5. seviyeye çıkar", ok = () => S.fieldLv >= 5, r = 18000 },
            new GoalDef { d = "Bir binayı 5. seviyeye çıkar", ok = () => S.units.Values.Any(u => u.l >= 5), r = 20000 },
            new GoalDef { d = "Ulusal Lig'e yüksel", ok = () => S.league.tier >= 3, r = 25000 },
            new GoalDef { d = "Bütün binaları kur", ok = () => B.Keys.All(u => S.units.ContainsKey(u) && S.units[u] != null), r = 30000 },
            new GoalDef { d = "Kasada 250.000 ₺ biriktir", ok = () => S.coins >= 250000, r = 40000 },
            new GoalDef { d = "Seviye 20'ye ulaş", ok = () => S.lv >= 20, r = 50000 },
            new GoalDef { d = "Koleksiyonu tamamla", ok = () => COL.All(c => Get(S.album, c.id) != 0), r = 60000 },
            new GoalDef { d = "Bütün binaları 5. seviyeye çıkar", ok = () => B.Keys.All(u => S.units.ContainsKey(u) && S.units[u] != null && S.units[u].l >= 5), r = 100000 },
        });
        public static bool GoalReady() { var g = S.goal >= 0 && S.goal < GOALS.Count ? GOALS[S.goal] : null; return g != null && g.ok(); }
        public static void ClaimGoal()
        {
            var g = S.goal >= 0 && S.goal < GOALS.Count ? GOALS[S.goal] : null; if (g == null || !g.ok()) return;
            Gain(g.r, "Ödül"); AddXP(Round(g.r / 5)); S.goal++; Track("goal", new Dictionary<string, object> { { "i", S.goal } }); Toast("Hedef tamam · +₺" + Fmt(g.r)); Commit();
        }
    }
}
