// ================= GELİR MODELİ: yonca, ödüllü reklam, dükkân (23-shop.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class DecorDef { public string id, slot, n, e; public double c; public int g; public int farm; }

    public static partial class Game
    {
        public const int AD_MAX = 8;
        public static int AdsLeft()
        {
            var t = TodayStr();
            if (S.ads.day != t) { S.ads.day = t; S.ads.n = 0; }
            return AD_MAX - S.ads.n;
        }
        public static void WatchAd(Action cb)
        {
            if (AdsLeft() <= 0) { UI.Toast("Bugünlük reklam hakkın bitti"); return; }
            if (PLATFORM.ShowAd(cb)) return;
            RectTransform card;
            var d = G2_Modal("mg", out card);
            int n = 5;
            G2_MgFace(card, "📺");
            G2_H2(card, "Ödüllü reklam", 22);
            G2_Note(card, "Mağaza sürümünde burada kısa bir reklam oynar. Bu sürümde örnek olarak 5 saniye bekliyoruz.", TextAnchor.UpperCenter);
            var c = G2_MgScore(card, n.ToString());
            int iv = 0;
            iv = SetInterval(() =>
            {
                n--;
                if (c) c.text = n.ToString();
                if (n <= 0) { ClearTimer(iv); UI.Kill(d); S.ads.n++; cb(); Save(); }
            }, 1000);
        }
        public static int SpeedCost(double ms) => Math.Max(1, CeilI(ms / 600000));
        public static void FinishJob(Job j) { j.e = Now(); }
        public static void SpeedUnit(string u, string how)
        {
            Unit s; if (u == null || !S.units.TryGetValue(u, out s) || s == null || s.job == null || Now() >= s.job.e) return;
            if (how == "ad") WatchAd(() => { if (s.job != null) FinishJob(s.job); Sfx("pop"); UI.Toast("⚡ Üretim tamamlandı!"); Commit(); });
            else
            {
                int c = SpeedCost(s.job.e - Now());
                if (S.gems < c) { UI.Toast("Yeterli yoncan yok"); return; }
                S.gems -= c; FinishJob(s.job); Sfx("pop"); Commit();
            }
        }
        public static void SpeedFields(string how)
        {
            var run = S.fields.Where(f => f != null && f.job != null && Now() < f.job.e).ToList();
            if (run.Count == 0) return;
            double ms = run.Max(f => f.job.e - Now());
            if (how == "ad") WatchAd(() => { foreach (var f in run) if (f.job != null) FinishJob(f.job); Sfx("pop"); UI.Toast("⚡ Bütün ekinler olgunlaştı!"); Commit(); });
            else
            {
                int c = SpeedCost(ms);
                if (S.gems < c) { UI.Toast("Yeterli yoncan yok"); return; }
                S.gems -= c; foreach (var f in run) FinishJob(f.job); Sfx("pop"); Commit();
            }
        }
        // speedBtns(kind,ms,u)
        public static void SpeedBtns(Transform parent, string kind, double ms, string u = null)
        {
            int c = SpeedCost(ms);
            var kv = u != null ? new[] { "u", u } : new string[0];
            // title="Ödüllü reklam izle, hemen bitir"
            G2_PillAct(parent, "📺 Bitir", "ad" + kind, "", AdsLeft() > 0, kv);
            // title="Yonca ile hemen bitir"
            G2_PillAct(parent, "🍀" + c + " Bitir", "gem" + kind, "", S.gems >= c, kv);
        }
        public static void ExtraRow(Transform parent, string u)
        {
            var s = S.units[u];
            if (s.job != null && Now() < s.job.e)
            {
                SpeedBtns(parent, "U", s.job.e - Now(), u);
                if (s.job.mg == 0 && u == "ahir") G2_PillAct(parent, "🎮 Sağım oyunu", "mg", "gold", true, "k", "milk", "u", "ahir");
                if (s.job.mg == 0 && u == "kumes") G2_PillAct(parent, "🎮 Yumurta yakala", "mg", "gold", true, "k", "eggs");
            }
            if (B[u].recipes != null) WorkerBtn(parent, u);
        }

        public static readonly List<DecorDef> DECOR = new List<DecorDef> {
            new DecorDef { id = "tablo_koy", slot = "duvar", n = "Köy manzarası", e = "🖼️", c = 400 }, new DecorDef { id = "tablo_at", slot = "duvar", n = "At portresi", e = "🐎", g = 4 }, new DecorDef { id = "nal", slot = "duvar", n = "Uğurlu nal", e = "🧲", c = 250 },
            new DecorDef { id = "saksi", slot = "kose", n = "Saksı ağacı", e = "🪴", c = 300 }, new DecorDef { id = "sandik", slot = "kose", n = "Çeyiz sandığı", e = "🧰", c = 600 }, new DecorDef { id = "gramofon", slot = "kose", n = "Gramofon", e = "📯", g = 6 },
            new DecorDef { id = "semaver", slot = "masa", n = "Semaver", e = "🫖", c = 500 }, new DecorDef { id = "vazo", slot = "masa", n = "Çiçekli vazo", e = "💐", c = 200 }, new DecorDef { id = "meyve", slot = "masa", n = "Meyve sepeti", e = "🧺", c = 150 },
            new DecorDef { id = "fener", slot = "tavan", n = "Gaz lambası", e = "🏮", c = 350 }, new DecorDef { id = "avize", slot = "tavan", n = "Bakır avize", e = "🔆", g = 8 },
            new DecorDef { id = "kilim_mavi", slot = "kilim", n = "Mavi kilim", e = "🟦", c = 400 }, new DecorDef { id = "kilim_yesil", slot = "kilim", n = "Yeşil kilim", e = "🟩", c = 400 }, new DecorDef { id = "kilim_altin", slot = "kilim", n = "Altın kilim", e = "🟨", g = 5 },
            new DecorDef { id = "heykel", slot = "bahce1", n = "Ördek heykeli", e = "🦆", c = 800, farm = 1 }, new DecorDef { id = "cicek", slot = "bahce1", n = "Çiçek tarhı", e = "🌷", c = 300, farm = 1 },
            new DecorDef { id = "kusevi", slot = "bahce2", n = "Kuş evi", e = "🐦", c = 350, farm = 1 }, new DecorDef { id = "cesme", slot = "bahce2", n = "Taş çeşme", e = "⛲", g = 6, farm = 1 },
            new DecorDef { id = "bayrak", slot = "giris", n = "Bayrak süsü", e = "🎏", c = 250, farm = 1 }, new DecorDef { id = "kemer", slot = "giris", n = "Çiçekli kemer", e = "🌸", g = 5, farm = 1 },
        };
        public static readonly Dictionary<string, string> SLOTN = new Dictionary<string, string> {
            { "duvar", "Duvar" }, { "kose", "Köşe" }, { "masa", "Sofra" }, { "tavan", "Tavan" }, { "kilim", "Kilim" }, { "bahce1", "Bahçe solu" }, { "bahce2", "Bahçe sağı" }, { "giris", "Giriş" } };

        public static void BuyDecor(string id)
        {
            var d = DECOR.Find(x => x.id == id); if (d == null) return;
            if (Get(S.decor.owned, id) == 0)
            {
                if (d.c != 0 && S.coins < d.c) { UI.Toast("Yeterli paran yok"); return; }
                if (d.g != 0 && S.gems < d.g) { UI.Toast("Yeterli yoncan yok"); return; }
                if (d.c != 0) Pay(d.c, "Süs");
                if (d.g != 0) S.gems -= d.g;
                S.decor.owned[id] = 1; Sfx("coin");
            }
            var grp = d.farm != 0 ? S.decor.farm : S.decor.house;
            grp[d.slot] = GetOr(grp, d.slot) == id ? null : id;
            UI.Toast(grp[d.slot] != null ? $"{d.e} {d.n} yerleştirildi" : $"{d.n} kaldırıldı");
            if (d.farm != 0) PopLoc = "ev";
            Commit();
            if (Interior != null && Interior.key == "kulube") BuildInterior("kulube");
        }

        public static void SheetShop(Transform parent)
        {
            var tb = View != null && !string.IsNullOrEmpty(View.tab) ? View.tab : "ev";
            Head(parent, "🛍️", "Dükkân", "Süsle, hızlandır");
            var b = G2_ShBody(parent);
            G2_Tabs(b, tb, new[] { "ev", "ciftlik", "yonca" }, new[] { "🏠 Ev", "🌷 Bahçe", "🍀 Yonca" });
            if (tb == "yonca")
            {
                var job = G2_Job(b);
                G2_Big(job, "🍀");
                var pw = G2_Pw(job);
                G2_PwB(pw, S.gems + " yonca");
                G2_PwSmall(pw, "Hızlandırma ve özel süsler için");
                var ab = UI.ActBtn(b, $"📺 Reklam izle · +1 🍀 ({AdsLeft()} hak)", "adGem", "big gold");
                if (AdsLeft() <= 0) UI.SetEnabled(ab, false);
                var prods = CONFIG.iapProducts;
                var tnames = new[] { "Avuç", "Sepet", "Çuval" };
                G2_Tiles(b, prods.Count, (cell, i) =>
                {
                    var p = prods[i];
                    string nm = i < tnames.Length ? tnames[i] : "";
                    if (PLATFORM.iap)
                    {
                        string pr; PLATFORM.prices.TryGetValue(p.id, out pr);
                        var t = G2_Tile(cell, false, true, () => Act("iap", D("id", p.id)));
                        G2_Big(t, "🍀", 26, "×" + p.gems);
                        G2_TileTm(t, nm);
                        G2_Ins(t, string.IsNullOrEmpty(pr) ? "…" : pr);
                    }
                    else
                    {
                        var t = G2_Tile(cell, false, true, null, .6f);
                        G2_Big(t, "🍀", 26, "×" + p.gems);
                        G2_TileTm(t, nm);
                        G2_TileTm(t, NATIVE ? "Yükleniyor" : "Uygulamada");
                    }
                });
                G2_Note(b, NATIVE ? "Satın alımlar Google Play / App Store hesabın üzerinden yapılır. Yonca oyun içinde de kazanılabilir."
                                  : "Yonca paketleri mobil uygulamada satın alınabilir. Tarayıcı sürümünde ödeme alınmaz; yonca oyun içinde kazanılır.");
            }
            else
            {
                var list = DECOR.Where(d => tb == "ev" ? d.farm == 0 : d.farm != 0).ToList();
                var grp = tb == "ev" ? S.decor.house : S.decor.farm;
                foreach (var sl in list.Select(d => d.slot).Distinct())
                {
                    G2_Uhead(b, SLOTN[sl]);
                    var items = list.Where(d => d.slot == sl).ToList();
                    G2_Tiles(b, items.Count, (cell, i) =>
                    {
                        var d = items[i];
                        bool own = Get(S.decor.owned, d.id) != 0, on = GetOr(grp, sl) == d.id;
                        bool dis = !own && ((d.c != 0 && S.coins < d.c) || (d.g != 0 && S.gems < d.g));
                        var t = G2_Tile(cell, on, !dis, () => Act("decor", D("id", d.id)));
                        G2_Big(t, d.e);
                        G2_TileTm(t, d.n, true);
                        G2_Ins(t, on ? "✓ Yerinde" : own ? "Yerleştir" : d.c != 0 ? "₺" + Fmt(d.c) : "🍀" + d.g);
                    });
                }
                G2_Note(b, tb == "ev" ? "Ev eşyaları evin içinde görünür. Eve girip bak!" : "Bahçe süsleri çiftlik evinin önünde görünür.");
            }
        }

        static void Init_23()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("adU", d => SpeedUnit(Arg(d, "u"), "ad"));
            // RegisterAct("gemU", d => SpeedUnit(Arg(d, "u"), "gem"));
            // RegisterAct("adF", d => SpeedFields("ad"));
            // RegisterAct("gemF", d => SpeedFields("gem"));
            // RegisterAct("decor", d => BuyDecor(Arg(d, "id")));
            // RegisterAct("adGem", d => WatchAd(() => { S.gems++; UI.Toast("+1 🍀"); Sfx("coin"); Commit(); }));
        }
    }
}
