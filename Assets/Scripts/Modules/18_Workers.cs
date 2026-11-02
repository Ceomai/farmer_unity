// ================= İŞÇİLER (18-workers.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public const int WORKER_LV = 5;
        public static double WorkerCost(string k) => k == "fields" ? 800 : Math.Max(500, Round(B[k].cost * .6));
        public static int AutoGain = 0, AutoTick = 0;
        // Vardiya: işçi 8 saat çalışır, sonra çay molası verir; oyuncu bir dokunuşla yeniden başlatır (oyuna dönmek için güzel bir sebep).
        // İşçi sayısı evin büyüklüğüne bağlıdır.
        public const double SHIFT = 8 * 3600000.0;
        public static readonly int[] WORKER_MAX = { 2, 5, 9, 99 };
        public static int WorkerMax() { int i = S.house - 1; return i >= 0 && i < WORKER_MAX.Length && WORKER_MAX[i] != 0 ? WORKER_MAX[i] : 2; }
        public static bool WorkOn(string k) => S.workers.ContainsKey(k) && S.workers[k] > Now();
        public static int WorkersHired() => S.workers.Count;

        public static bool RenewWorker(string k, bool quiet = false)
        {
            if (Get(S.workers, k) == 0 || WorkOn(k)) return false;
            S.workers[k] = Now() + SHIFT;
            if (!quiet) { Sfx("chime"); UI.Toast("☕ Çay içildi, işçi işinin başında!"); Commit(); }
            return true;
        }
        public static int RenewAll()
        {
            int n = 0;
            foreach (var k in S.workers.Keys.ToList()) if (RenewWorker(k, true)) n++;
            if (n > 0) { Sfx("chime"); Commit(); }
            return n;
        }
        public static void Hire(string k)
        {
            if (k != "fields" && !(k != null && B.ContainsKey(k) && B[k].recipes != null && S.units.ContainsKey(k))) return;
            if (S.lv < WORKER_LV) { UI.Toast("İşçiler seviye 5'te açılır"); return; }
            if (Get(S.workers, k) != 0) return;
            var c = WorkerCost(k);
            if (S.coins < c) { UI.Toast("Yeterli paran yok"); return; }
            if (WorkersHired() >= WorkerMax()) { UI.Toast($"Bu evde en fazla {WorkerMax()} işçi barınabilir. Evini büyüt!"); return; }
            Pay(c, "İşçi");
            S.workers[k] = Now() + SHIFT;
            Sfx("build"); UI.Toast("👷 İşçi işe başladı!");
            PopLoc = k == "fields" ? "tarla" : LocOf(k);
            Commit();
        }

        // workerBtn(k) → düğme/hap
        public static void WorkerBtn(Transform parent, string k)
        {
            if (Get(S.workers, k) != 0)
            {
                // title="Vardiya bitince çay molası verir"
                if (WorkOn(k)) { G2_PillSpan(parent, "👷 Çalışıyor · " + Secs(Math.Ceiling((S.workers[k] - Now()) / 1000))); return; }
                // title="İşçi mola verdi. Bir çay ısmarla, 8 saat daha çalışsın"
                G2_PillAct(parent, "☕ Mola · devam ettir", "renew", "gold", true, "k", k);
                return;
            }
            // title="Evini büyütünce daha fazla işçi tutabilirsin"
            if (S.lv >= WORKER_LV && WorkersHired() >= WorkerMax()) { G2_PillSpan(parent, $"👷 {WorkersHired()}/{WorkerMax()} işçi"); return; }
            // title="Hazır ürünü toplar, aynı üretimi yeniden başlatır"
            G2_PillAct(parent, "👷 " + (S.lv >= WORKER_LV ? "İşçi ₺" + Fmt(WorkerCost(k)) : "İşçi · Sv " + WORKER_LV), "hire", "",
                S.lv >= WORKER_LV && S.coins >= WorkerCost(k), "k", k);
        }

        // Güvenlik ağı: para tohuma yetmiyor, satacak ürün yok, hiçbir iş çalışmıyor ve işçi yoksa oyuncu kilitlenir.
        // Böyle bir durumda komşu saatte en fazla bir kez tohum parası bırakır.
        public static void Rescue()
        {
            if (S == null || S.coins >= 5) return;
            bool sellable = S.items.Keys.Any(k => Has(k) > 0 && k != "yem");
            bool running = S.fields.Any(f => f != null && f.job != null) || S.units.Values.Any(s => s != null && s.job != null);
            if (sellable || running || S.workers.Keys.Any(WorkOn) || Now() - S.rescueAt < 3600000) return;
            S.rescueAt = Now();
            Gain(30, "Yardım");
            UI.Toast("🧑‍🌾 Komşu Ali tohum parası bıraktı: +₺30. Hadi ekelim!");
            Commit();
        }

        public static bool Automate()
        {
            bool ch = false;
            if (WorkOn("fields"))
                for (int i = 0; i < S.fields.Count; i++)
                {
                    var f = S.fields[i];
                    if (f != null && f.job != null && Now() >= f.job.e && HarvestPlot(i, null, null, true)) ch = true;
                    f = S.fields[i];
                    if (f != null && f.job == null && PlantPlot(i, S.lastCrop, true)) ch = true;
                }
            foreach (var u in S.workers.Keys.ToList())
            {
                if (u == "fields" || !WorkOn(u)) continue;
                Unit s; if (!S.units.TryGetValue(u, out s) || s == null || !B.ContainsKey(u) || B[u].recipes == null) continue;
                if (s.job != null && Now() >= s.job.e && CollectUnit(u, null, null, true)) ch = true;
                if (QueueNext(u)) ch = true;
                if (s.job == null && s.last != null && !(s.q != null && s.q.Count > 0))
                {
                    var r = B[u].recipes.Find(x => x.id == s.last);
                    if (r != null && r.lvl <= S.lv && CanPay(r) && !(!string.IsNullOrEmpty(r.col) && !Has1(r.col))) { StartUnit(u, s.last, true); ch = true; }
                }
            }
            return ch;
        }

        static void Init_18()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("renew", d => RenewWorker(Arg(d, "k")));
            // RegisterAct("hire", d => Hire(Arg(d, "k")));
        }
    }
}
