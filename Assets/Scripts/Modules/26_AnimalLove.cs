// ================= HAYVAN İSİMLERİ VE SEVGİ (26-animal-love.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public static readonly Dictionary<string, List<string>> ANAMES = new Dictionary<string, List<string>> {
            { "kumes", new List<string> { "Pamuk", "Gıdık", "Kınalı", "Benekli", "Ponçik", "Tüylü", "Sarı", "Minnoş", "Cücük" } },
            { "ahir", new List<string> { "Sarıkız", "Menekşe", "Karakız", "Benekli" } },
            { "agil", new List<string> { "Kıvırcık", "Bulut", "Yumak", "Topak", "Dolly", "Pofuduk", "Kar", "Lokum" } },
            { "besi", new List<string> { "Tosun", "Karaoğlan", "Paşa", "Boncuk" } },
            { "hara", new List<string> { "Yıldız", "Rüzgar", "Asil" } },
        };
        public static readonly List<string> LOVE_U = new List<string> { "kumes", "ahir", "agil", "besi", "hara" };

        public static Love LoveOf(string u)
        {
            if (S.love == null) S.love = new Dictionary<string, Love>();
            Love L;
            if (!S.love.TryGetValue(u, out L) || L == null) { L = new Love { v = 50, t = Now(), pet = new Dictionary<string, double>() }; S.love[u] = L; }
            if (L.pet == null) L.pet = new Dictionary<string, double>();
            double DM = ProjOn("cesme") ? 1200000 : 600000;
            double decay = Math.Floor((Now() - L.t) / DM);
            if (decay > 0) { L.v = Math.Max(20, L.v - decay); L.t += decay * DM; }
            return L;
        }
        public static bool LoveBonus(string u) => LOVE_U.Contains(u) && S.units.ContainsKey(u) && S.units[u] != null && LoveOf(u).v >= 70;

        // x,y: ekranda CSS px (sol üstten)
        public static void PetAnimal(string u, int idx, float x, float y)
        {
            var L = LoveOf(u);
            List<string> an; ANAMES.TryGetValue(u, out an);
            int len = an != null ? an.Count : 1;
            string nm = an != null && an.Count > 0 ? (an[((idx % len) + len) % len] ?? "") : "";
            var key = idx.ToString();
            double last = Get(L.pet, key);
            if (Now() - last < 60000) { UI.Float(x, y, $"{nm} ♥"); return; }
            L.pet[key] = Now();
            double was = L.v;
            L.v = Math.Min(100, L.v + 6);
            Stat("pet");
            UI.Float(x, y, $"{nm} ♥ {G2_Num(L.v)}");
            if (was < 70 && L.v >= 70) { UI.Toast($"💗 {B[u].n} hayvanları çok mutlu! Her toplamada +1 ürün"); Sfx("win"); }
            Save();
            if (View != null) RenderSheet();
        }

        public static void LoveRow(Transform parent, string u)
        {
            if (!LOVE_U.Contains(u) || !S.units.ContainsKey(u) || S.units[u] == null) return;
            var L = LoveOf(u);
            int l = S.units[u].l;
            var cnt = new Dictionary<string, int> {
                { "kumes", 3 + 2 * l }, { "ahir", Math.Min(4, l + 1) }, { "agil", 2 + 2 * l }, { "besi", Math.Min(4, l + 1) }, { "hara", Math.Min(3, l) } };
            var names = ANAMES[u].Take(Math.Max(0, Math.Min(ANAMES[u].Count, cnt[u]))).ToList();
            var job = G2_Job(parent, 7, 12);
            G2_Big(job, L.v >= 70 ? "💗" : L.v >= 40 ? "💛" : "🤍", 22);
            var pw = G2_Pw(job);
            G2_PwB(pw, $"Mutluluk {G2_Num(L.v)}/100{(L.v >= 70 ? " · +1 ürün" : "")}");
            G2_Pbar(pw, (float)(L.v / 100), UI.Hex("#f3a6b8"), UI.Hex("#e8657a"));
            G2_PwSmall(pw, string.Join(", ", names) + " · içeri girip sev");
        }
    }
}
