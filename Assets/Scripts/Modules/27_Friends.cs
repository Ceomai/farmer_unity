// ================= KASABALILARLA DOSTLUK (27-friends.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public static readonly int[] FR_LV = { 0, 3, 8, 15 };
        public static readonly string[] FR_N = { "Yabancı", "Tanıdık", "Dost", "Can dostu" };
        public static readonly Dictionary<string, string> FR_PERK = new Dictionary<string, string> {
            { "Kasap Rıza", "Et ve süt siparişleri" }, { "Fırıncı Nuri", "Un ve ekmek siparişleri" }, { "Lokantacı Sevim", "Sebze siparişleri" },
            { "Manav Hilmi", "Meyve-sebze" }, { "Otelci Gülten", "Lüks ürünler" }, { "Kooperatif", "Toplu alım" }, { "Bakkal Ahmet", "Günlük ihtiyaç" },
            { "Konakçı Halit Bey", "Pahalı siparişler" }, { "Pazarcı Zehra", "Pazar tezgâhı" } };

        public static Friend FrOf(string n)
        {
            if (S.friends == null) S.friends = new Dictionary<string, Friend>();
            Friend f;
            if (n == null) n = "";
            if (!S.friends.TryGetValue(n, out f) || f == null) { f = new Friend { d = 0, gift = "" }; S.friends[n] = f; }
            return f;
        }
        public static int FrLv(string n)
        {
            int d = FrOf(n).d, l = 0;
            for (int i = 0; i < FR_LV.Length; i++) if (d >= FR_LV[i]) l = i;
            return l;
        }
        public static double FrMult(string n) => 1 + FrLv(n) * .06;
        public static string Hearts(string n)
        {
            int l = FrLv(n);
            return string.Concat(Enumerable.Repeat("❤️", l)) + string.Concat(Enumerable.Repeat("🤍", 3 - l));
        }

        public static void Befriend(string n, int amt)
        {
            int before = FrLv(n);
            FrOf(n).d += amt;
            int after = FrLv(n);
            if (after > before)
            {
                S.gems += after;
                SetTimeout(() =>
                {
                    RectTransform card;
                    var root = G2_Modal("login", out card);
                    G2_Badge(card, Face(n));
                    G2_H2(card, UI.Esc(n), 26);
                    G2_P(card, $"Artık <b>{FR_N[after]}</b>sunuz! {Hearts(n)}\nSiparişleri %{after * 6} daha fazla ödüyor.{(after >= 2 ? "\nHer gün sana küçük bir hediye bırakacak." : "")}\n+{after} 🍀");
                    G2_BigBtn(card, "Sağ ol!", () => UI.Kill(root));
                    Sfx("win");
                }, 600);
            }
        }

        public static void FriendGifts()
        {
            var t = TodayStr();
            var outL = new List<string>();
            foreach (var n in NAMES)
            {
                var f = FrOf(n);
                if (FrLv(n) >= 2 && f.gift != t)
                {
                    f.gift = t;
                    var pool = Producible();
                    var k0 = pool.Count > 0 ? pool[FloorI(Rnd() * pool.Count)] : null;
                    var k = k0 ?? "yem";
                    int q = 2 + FrLv(n);
                    outL.Add($"{Face(n)} {n}: {GiveItems(new Dictionary<string, int> { { k, q } })}");
                }
            }
            if (outL.Count > 0)
            {
                RectTransform card;
                var root = G2_Modal("login", out card);
                G2_Badge(card, "🎁");
                G2_H2(card, "Kasabadan hediye!", 26);
                G2_P(card, string.Join("\n", outL));
                G2_BigBtn(card, "Teşekkürler", () => UI.Kill(root));
            }
        }

        public static void FriendsBody(Transform parent)
        {
            G2_Note(parent, "Bir kişinin siparişini teslim ettikçe dostluğunuz büyür. Dostlar daha çok öder, can dostlar her gün hediye bırakır. Tanıdık olduğun kasabalıların küçük hikâyeleri burada açılır.");
            foreach (var n in NAMES)
            {
                var f = FrOf(n); int l = FrLv(n);
                int nx = l + 1 < FR_LV.Length ? FR_LV[l + 1] : 0;
                var job = G2_Job(parent, 7, 12);
                G2_Big(job, Face(n), 26);
                var pw = G2_Pw(job);
                G2_PwB(pw, n);
                G2_PwSmall(pw, $"{FR_N[l]} · {Hearts(n)} · ödül +%{l * 6}");
                if (nx != 0)
                {
                    G2_Pbar(pw, (float)(Math.Min(100, (double)(f.d - FR_LV[l]) / (nx - FR_LV[l]) * 100) / 100), UI.Hex("#e8657a"));
                    string perk; FR_PERK.TryGetValue(n, out perk);
                    G2_PwSmall(pw, $"{f.d}/{nx} teslimat · {perk ?? ""}");
                }
                else G2_PwSmall(pw, "En yüksek dostluk!");
                QuestHTML(parent, n);
            }
        }
    }
}
