// ================= DURUM (02-state.js) =================
using System;
using System.Collections.Generic;

namespace Ciftlik
{
    // 04-economy siparişleri bu alanlarla üretir (genOrder: base, xp)
    public partial class Order { public double @base; public double xp; }

    public static partial class Game
    {
        public const string SAVE = "ciftlik-imparatorlugu-v3";

        public static State Fresh()
        {
            return new State
            {
                coins = 300, xp = 0, lv = 1, rep = 50, items = new Dictionary<string, int> { { "yem", 6 } }, house = 1,
                regions = new Dictionary<string, int> { { "baba", 1 }, { "town", 1 } },
                units = new Dictionary<string, Unit> { { "kumes", new Unit { l = 1 } } },
                fields = new List<Field> { new Field(), new Field() }, fieldLv = 1, lastCrop = "bugday",
                orders = new List<Order>(), market = new Market { at = 0, m = new Dictionary<string, double>(), h = new Dictionary<string, List<double>>() },
                fin = new Fin { day = "", inc = new Dictionary<string, double>(), exp = new Dictionary<string, double>(), hist = new List<FinDay>() },
                stats = new Dictionary<string, double>(), goal = 0, created = DateNow(),
                gems = 5, album = new Dictionary<string, int>(), workers = new Dictionary<string, double>(), contracts = new List<Contract>(), horses = new List<Horse>(),
                league = new League { tier = 0, pts = 0 }, decor = new Decor { owned = new Dictionary<string, int>(), house = new Dictionary<string, string>(), farm = new Dictionary<string, string>() },
                ads = new Ads { day = "", n = 0 }, sound = new Sound { music = true, sfx = true, notif = false }, daily = null,
                contests = new Contests { day = "", list = new List<string>(), done = new Dictionary<string, ContestResult>() },
                weather = new Weather { k = "sun", until = 0 }, lastSeen = 0, consent = null
            };
        }

        public static State Load()
        {
            try { var r = LsGet(SAVE); if (!string.IsNullOrEmpty(r)) return FromJson<State>(r); } catch (Exception) { }
            return null;
        }

        public static void Save()
        {
            try
            {
                S.lastSeen = DateNow(); var j = ToJson(S); LsSet(SAVE, j);
                if (PLATFORM != null) PLATFORM.Backup(j);
            }
            catch (Exception) { }
        }
    }
}
