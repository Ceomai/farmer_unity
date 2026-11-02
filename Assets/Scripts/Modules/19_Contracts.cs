// ================= SÖZLEŞMELER (19-contracts.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    // Sözleşmenin kayıttaki diğer alanları (JS: {who,k,total,got,until,reward,xp,gems})
    public partial class Contract
    {
        public int got;
        public double until;
        public double reward;
        public double xp;
        public int gems;
    }

    public static partial class Game
    {
        public static Contract GenContract()
        {
            var pool = Producible().Where(k => ITEMS[k].p >= 8).ToList();
            var k0 = pool.Count > 0 ? pool[FloorI(Rnd() * pool.Count)] : null;
            var k = k0 ?? "misir";
            var p = ITEMS[k].p;
            int total = (int)Math.Max(5, Math.Min(60, Round(500 / p)));
            int hrs = 6 + FloorI(Rnd() * 13);
            return new Contract
            {
                who = NAMES[FloorI(Rnd() * NAMES.Count)], k = k, total = total, got = 0, until = Now() + hrs * 3600000.0,
                reward = Round(p * total * 2.2), xp = Round(20 + p * total / 15), gems = 1
            };
        }
        public static bool EnsureContracts()
        {
            int n = S.contracts.Count;
            S.contracts = S.contracts.Where(c => c != null && Now() < c.until).ToList();
            while (S.contracts.Count < 2) S.contracts.Add(GenContract());
            return n != S.contracts.Count;
        }
        public static void DeliverContract(int i)
        {
            if (i < 0 || i >= S.contracts.Count) return;
            var c = S.contracts[i]; if (c == null) return;
            int n = Math.Min(Has(c.k), c.total - c.got);
            if (n <= 0) { UI.Toast("Elinde bu üründen yok"); return; }
            S.items[c.k] -= n; c.got += n;
            if (c.got >= c.total)
            {
                Befriend(c.who, 3);
                var r = Round(c.reward * RepMult() * FrMult(c.who) * (ProjOn("kopru") ? 1.15 : 1));
                Gain(r, "Sözleşme"); AddXP(c.xp); S.gems += c.gems; S.rep = Math.Min(100, S.rep + 2); Stat("contracts");
                S.contracts[i] = GenContract();
                Sfx("win"); FlyCoins(8); UI.Toast($"Sözleşme tamam! +₺{Fmt(r)} +🍀{c.gems}");
            }
            else { Sfx("pop"); UI.Toast($"{n} {ITEMS[c.k].n} teslim edildi"); }
            Commit();
        }
        public static readonly Dictionary<string, string> FACES = new Dictionary<string, string> {
            { "Kasap Rıza", "🧔" }, { "Fırıncı Nuri", "👨‍🍳" }, { "Lokantacı Sevim", "👩‍🍳" }, { "Manav Hilmi", "👴" }, { "Otelci Gülten", "👩‍💼" },
            { "Kooperatif", "🧑‍🌾" }, { "Bakkal Ahmet", "🧑" }, { "Konakçı Halit Bey", "🎩" }, { "Pazarcı Zehra", "👵" } };
        public static string Face(string n) { string f; return n != null && FACES.TryGetValue(n, out f) ? f : "🧑"; }

        public static void ContractsBody(Transform parent)
        {
            EnsureContracts();
            G2_Note(parent, "Uzun vadeli tedarik: parça parça teslim et, sonunda büyük ödül al.");
            for (int i = 0; i < S.contracts.Count; i++)
            {
                var c = S.contracts[i];
                var o = G2_Order(parent);
                G2_Who(o, Face(c.who) + " " + UI.Esc(c.who), $"₺{Fmt(c.reward * RepMult())} · 🍀{c.gems}");
                var mb = UI.HStack(o, 8, TextAnchor.MiddleLeft, null, "meterbar");
                UI.Txt(mb, ITEMS[c.k].e, 22, null, false, TextAnchor.MiddleCenter, false);
                G2_Pbar(mb, c.total > 0 ? (float)c.got / c.total : 0);
                UI.Txt(mb, "<b>" + c.got + "/" + c.total + "</b>", 12, UI.C.text, true, TextAnchor.MiddleRight, false);
                var ur = G2_Urow(o);
                G2_PillAct(ur, $"📦 Teslim et ({Math.Min(Has(c.k), c.total - c.got)})", "contract", "gold", Has(c.k) > 0, "i", i.ToString());
                G2_Left(ur, c.until, "⏳");
            }
        }

        static void Init_19()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("contract", d => DeliverContract(ArgI(d, "i", -1)));
        }
    }
}
