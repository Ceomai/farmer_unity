// ================= OYUN VERİSİ (01-data.js) =================
using System.Collections.Generic;
using System.Linq;

namespace Ciftlik
{
    public class ItemDef { public string n, e; public double p; public double buy; }

    public class Recipe
    {
        public string id, n;
        public double cost;
        public Dictionary<string, int> @in;
        public double t;
        public Dictionary<string, int> @out;
        public int lvl;
        public string col;
        public List<int> season;
        // JS {...c} kopyası
        public Recipe Clone()
        {
            return new Recipe
            {
                id = id, n = n, cost = cost, @in = @in == null ? null : new Dictionary<string, int>(@in), t = t,
                @out = @out == null ? null : new Dictionary<string, int>(@out), lvl = lvl, col = col, season = season == null ? null : new List<int>(season)
            };
        }
    }

    public class BuildingDef { public string n, e, d; public double cost; public int lvl; public List<Recipe> recipes; public int storage; public bool field; }
    public class HouseDef { public string n, e; public double cost; public int lvl; }
    public class RegionDef { public string n; public double cost; public int lvl; public float[] rect; }
    public class LocDef { public string n, reg, kind, e; public float[] p; public float r, h; public List<string> units; public List<float[]> slots; public float[] s; }
    public class UpReqDef { public int house, lv; }

    public static partial class Game
    {
        static Dictionary<string, int> Data_D(params object[] kv)
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[(string)kv[i]] = (int)kv[i + 1];
            return d;
        }
        static Recipe Data_R(string id, string n, double t, Dictionary<string, int> @in, Dictionary<string, int> @out, int lvl, double cost = 0, string col = null, List<int> season = null)
            => new Recipe { id = id, n = n, t = t, @in = @in, @out = @out, lvl = lvl, cost = cost, col = col, season = season };
        static ItemDef Data_I(string n, string e, double p, double buy = 0) => new ItemDef { n = n, e = e, p = p, buy = buy };
        static Dictionary<string, int> Data_E() => new Dictionary<string, int>();

        // Veriler ilk erişimde kurulur (partial sınıfın dosyalar arası alan başlatma sırası belirsiz olduğu için)
        static Dictionary<string, ItemDef> _items;
        public static Dictionary<string, ItemDef> ITEMS => _items ?? (_items = new Dictionary<string, ItemDef>
        {
            { "bugday", Data_I("Buğday", "🌾", 4) }, { "misir", Data_I("Mısır", "🌽", 9) }, { "domates", Data_I("Domates", "🍅", 16) },
            { "aycicegi", Data_I("Ayçiçeği", "🌻", 22) }, { "pamuk", Data_I("Pamuk", "☁️", 30) }, { "cilek", Data_I("Çilek", "🍓", 38) }, { "elma", Data_I("Elma", "🍎", 12) },
            { "yumurta", Data_I("Yumurta", "🥚", 10) }, { "sut", Data_I("Süt", "🥛", 22) }, { "yun", Data_I("Yün", "🧶", 36) }, { "bal", Data_I("Bal", "🍯", 42) }, { "et", Data_I("Et", "🥩", 130) },
            { "yem", Data_I("Yem", "🌰", 3, 7) }, { "un", Data_I("Un", "🥡", 12) }, { "ekmek", Data_I("Ekmek", "🍞", 42) }, { "kek", Data_I("Kek", "🎂", 120) },
            { "peynir", Data_I("Peynir", "🧀", 95) }, { "yogurt", Data_I("Yoğurt", "🥣", 58) }, { "salca", Data_I("Salça", "🫙", 85) }, { "yag", Data_I("Ayçiçek Yağı", "🧴", 110) },
            { "kumas", Data_I("Kumaş", "🧵", 160) }, { "at", Data_I("Yarış Tayı", "🐎", 1800) },
            { "kabak", Data_I("Kabak", "🎃", 240) }, { "kasar", Data_I("Eski Kaşar", "🟨", 950) }, { "kestane", Data_I("Kestane Balı", "🏺", 330) }, { "tereyag", Data_I("Tereyağı", "🧈", 150) }, { "recel", Data_I("Reçel", "🥫", 140) }, { "baklava", Data_I("Baklava", "🥮", 520) }, { "hali", Data_I("El Dokuma Halı", "🟥", 1400) }, { "sabun", Data_I("Bal Sabunu", "🧼", 170) }, { "pastirma", Data_I("Pastırma", "🥓", 430) },
            { "altinbugday", Data_I("Altın Buğday", "🌟", 35) }, { "morhavuc", Data_I("Mor Havuç", "🥕", 28) },
            { "lale", Data_I("Lale", "🌷", 26) }, { "uzum", Data_I("Üzüm", "🍇", 14) }, { "pekmez", Data_I("Pekmez", "🍶", 160) }, { "pestil", Data_I("Pestil", "🍫", 260) }, { "sirke", Data_I("Üzüm Sirkesi", "🍾", 230) },
            { "simit", Data_I("Simit", "🥯", 190) }, { "kaymak", Data_I("Kaymak", "🍦", 240) }, { "yorgan", Data_I("Yün Yorgan", "🛏️", 950) }, { "polen", Data_I("Polen", "🌼", 300) }, { "karpuz", Data_I("Karpuz", "🍉", 70) }, { "patates", Data_I("Patates", "🥔", 21) }, { "lahana", Data_I("Lahana", "🥬", 40) },
        });

        static List<Recipe> _crops;
        public static List<Recipe> CROPS => _crops ?? (_crops = new List<Recipe>
        {
            Data_R("bugday", "Buğday", 20, null, Data_D("bugday", 3), 1, 5),
            Data_R("misir", "Mısır", 45, null, Data_D("misir", 3), 2, 10),
            Data_R("domates", "Domates", 90, null, Data_D("domates", 3), 4, 20),
            Data_R("aycicegi", "Ayçiçeği", 150, null, Data_D("aycicegi", 3), 6, 30),
            Data_R("pamuk", "Pamuk", 240, null, Data_D("pamuk", 3), 8, 45),
            Data_R("kabak", "Kabak (1 saat)", 3600, null, Data_D("kabak", 2), 3, 80),
            Data_R("altinbugday", "Altın Buğday", 120, null, Data_D("altinbugday", 3), 1, 20, "altin_bugday"),
            Data_R("morhavuc", "Mor Havuç", 300, null, Data_D("morhavuc", 4), 1, 15, "mor_havuc"),
            // Mevsim ürünleri: yalnızca kendi mevsiminde tarlaya ekilir (serada her mevsim yetişir); daha değerlidir
            Data_R("lale", "Lale", 120, null, Data_D("lale", 3), 3, 25, null, new List<int> { 0 }),
            Data_R("karpuz", "Karpuz", 240, null, Data_D("karpuz", 2), 5, 40, null, new List<int> { 1 }),
            Data_R("patates", "Patates", 150, null, Data_D("patates", 4), 4, 20, null, new List<int> { 2 }),
            Data_R("lahana", "Lahana", 200, null, Data_D("lahana", 3), 6, 35, null, new List<int> { 3 }),
        });

        static BuildingDef Data_B(string n, string e, double cost, int lvl, string d, params Recipe[] recipes)
            => new BuildingDef { n = n, e = e, cost = cost, lvl = lvl, d = d, recipes = recipes.Length > 0 ? recipes.ToList() : null };

        static List<Recipe> Data_SeraRecipes()
        {
            var l = CROPS.Select(c => { var r = c.Clone(); r.t = Round(c.t / 2); return r; }).ToList();
            l.Add(Data_R("cilek", "Çilek", 180, null, Data_D("cilek", 4), 9, 60));
            return l;
        }

        static Dictionary<string, BuildingDef> _b;
        public static Dictionary<string, BuildingDef> B => _b ?? (_b = Data_Buildings());
        static Dictionary<string, BuildingDef> Data_Buildings()
        {
            var silo = Data_B("Silo", "🛢️", 700, 2, "Her seviyede ambara +150 yer açar."); silo.storage = 150;
            var sera = Data_B("Sera", "🏕️", 7000, 9, "Ürünler iki kat hızlı büyür, çilek yetişir."); sera.field = true; sera.recipes = Data_SeraRecipes();
            return new Dictionary<string, BuildingDef>
            {
                { "kumes", Data_B("Kümes", "🐔", 250, 1, "Tavukları besle, folluklardan yumurta topla.", Data_R("besle", "Tavukları besle", 40, Data_D("yem", 1), Data_D("yumurta", 2), 1)) },
                { "silo", silo },
                { "yemhane", Data_B("Yem Değirmeni", "⚙️", 600, 2, "Mısırdan hayvan yemi öğütür. Pazardan almaktan ucuz.", Data_R("yem", "Yem öğüt", 30, Data_D("misir", 2), Data_D("yem", 5), 2)) },
                { "degirmen", Data_B("Yel Değirmeni", "🌬️", 1000, 3, "Buğdayı una çevirir. Çalışırken kanatlar hızlanır.", Data_R("un", "Un öğüt", 40, Data_D("bugday", 3), Data_D("un", 2), 3)) },
                { "ahir", Data_B("Ahır", "🐄", 1500, 3, "İnekleri besle, süt sağ.", Data_R("sag", "İnekleri besle", 90, Data_D("yem", 2), Data_D("sut", 2), 3)) },
                { "firin", Data_B("Taş Fırın", "🧱", 2000, 4, "Ekmek ve kek pişirir.",
                    Data_R("ekmek", "Ekmek", 60, Data_D("un", 2), Data_D("ekmek", 1), 4),
                    Data_R("kek", "Kek", 120, Data_D("un", 2, "yumurta", 2, "sut", 1), Data_D("kek", 1), 6),
                    Data_R("baklava", "Baklava", 600, Data_D("un", 3, "bal", 2, "tereyag", 1), Data_D("baklava", 1), 13),
                    Data_R("simit", "Pekmezli simit", 240, Data_D("un", 2, "pekmez", 1), Data_D("simit", 2), 21)) },
                { "agil", Data_B("Ağıl", "🐑", 2500, 5, "Koyunları besle, yün kırp.", Data_R("kirp", "Koyunları besle", 120, Data_D("yem", 2), Data_D("yun", 2), 5)) },
                { "mandira", Data_B("Mandıra", "🧀", 3000, 5, "Sütü peynire ve yoğurda işler.",
                    Data_R("yogurt", "Yoğurt", 60, Data_D("sut", 2), Data_D("yogurt", 1), 5),
                    Data_R("peynir", "Peynir", 120, Data_D("sut", 3), Data_D("peynir", 1), 5),
                    Data_R("kasar", "Eski Kaşar (8 saat)", 28800, Data_D("sut", 6), Data_D("kasar", 1), 7),
                    Data_R("tereyag", "Tereyağı", 180, Data_D("sut", 4), Data_D("tereyag", 1), 10),
                    Data_R("kaymak", "Kaymak", 400, Data_D("sut", 6), Data_D("kaymak", 1), 17)) },
                { "kovan", Data_B("Arılık", "🐝", 1800, 5, "Yem istemez, yavaş ama bedava bal.",
                    Data_R("bal", "Bal hasadı", 180, Data_E(), Data_D("bal", 2), 5),
                    Data_R("kestane", "Kestane balı (6 saat)", 21600, Data_E(), Data_D("kestane", 2), 6),
                    Data_R("polen", "Polen (1 saat)", 3600, Data_E(), Data_D("polen", 2), 25)) },
                { "bahce", Data_B("Elma Bahçesi", "🌳", 2500, 6, "Elma ağaçları. Tohum gerekmez.",
                    Data_R("elma", "Elma topla", 300, Data_E(), Data_D("elma", 6), 6),
                    Data_R("buyukhasat", "Büyük hasat (4 saat)", 14400, Data_E(), Data_D("elma", 40), 6)) },
                { "konserve", Data_B("Salça Atölyesi", "🥫", 4000, 6, "Domatesi salçaya çevirir.",
                    Data_R("salca", "Salça", 90, Data_D("domates", 4), Data_D("salca", 1), 6),
                    Data_R("recel", "Elma reçeli", 240, Data_D("elma", 6), Data_D("recel", 1), 11),
                    Data_R("recel2", "Çilek reçeli", 240, Data_D("cilek", 3), Data_D("recel", 1), 11)) },
                { "besi", Data_B("Besi Ahırı", "🐂", 5000, 7, "Dana al, besle, et olarak sat. Yüksek kâr, uzun süre.",
                    Data_R("besi", "Dana besle", 300, Data_D("yem", 4), Data_D("et", 3), 7, 150),
                    Data_R("pastirma", "Pastırma (2 saat)", 7200, Data_D("et", 3), Data_D("pastirma", 2), 15)) },
                { "yaghane", Data_B("Yağhane", "🛢", 6000, 8, "Ayçiçeğinden yağ sıkar.",
                    Data_R("yag", "Yağ sık", 120, Data_D("aycicegi", 4), Data_D("yag", 1), 8),
                    Data_R("sabun", "Bal sabunu", 300, Data_D("yag", 2, "bal", 1), Data_D("sabun", 2), 12)) },
                { "dokuma", Data_B("Dokuma Atölyesi", "🪡", 6500, 8, "Yün ve pamuktan kumaş dokur.",
                    Data_R("kumas1", "Yün kumaş", 150, Data_D("yun", 3), Data_D("kumas", 1), 8),
                    Data_R("kumas2", "Pamuklu kumaş", 150, Data_D("pamuk", 4), Data_D("kumas", 1), 8),
                    Data_R("hali", "El dokuma halı (3 saat)", 10800, Data_D("kumas", 3, "yun", 2), Data_D("hali", 1), 14),
                    Data_R("yorgan", "Yün yorgan (2 saat)", 7200, Data_D("yun", 6, "kumas", 2), Data_D("yorgan", 1), 23)) },
                { "sera", sera },
                { "bag", Data_B("Üzüm Bağı", "🍇", 20000, 18, "Asma sıraları. Tohum gerekmez; üzüm verir, bağ bozumunda bolca.",
                    Data_R("uzum", "Üzüm topla", 600, Data_E(), Data_D("uzum", 6), 18),
                    Data_R("bagbozumu", "Bağ bozumu (6 saat)", 21600, Data_E(), Data_D("uzum", 60), 19)) },
                { "pekmezhane", Data_B("Pekmezhane", "🍶", 26000, 20, "Üzümü kazanlarda kaynatır: pekmez, pestil ve sirke.",
                    Data_R("pekmez", "Pekmez", 300, Data_D("uzum", 6), Data_D("pekmez", 1), 20),
                    Data_R("pestil", "Pestil", 600, Data_D("uzum", 4, "un", 2), Data_D("pestil", 1), 22),
                    Data_R("sirke", "Sirke (1 saat)", 3600, Data_D("uzum", 8), Data_D("sirke", 2), 24)) },
                { "hara", Data_B("Hara", "🐴", 10000, 9, "Safkan tay yetiştir. Tayını yarışa sokabilir ya da pazarda satabilirsin.",
                    Data_R("tay", "Tay yetiştir", 600, Data_D("yem", 6), Data_D("at", 1), 9, 300)) },
            };
        }

        static List<HouseDef> _house;
        public static List<HouseDef> HOUSE => _house ?? (_house = new List<HouseDef>
        {
            new HouseDef { n = "Kulübe", e = "🛖" }, new HouseDef { n = "Taş Ev", e = "🏠", cost = 800, lvl = 3 },
            new HouseDef { n = "Çiftlik Evi", e = "🏡", cost = 4000, lvl = 6 }, new HouseDef { n = "Konak", e = "🏰", cost = 15000, lvl = 10 }
        });

        // Arazi bölgeleri: [x1,z1,x2,z2]
        static Dictionary<string, RegionDef> _reg;
        public static Dictionary<string, RegionDef> REG => _reg ?? (_reg = new Dictionary<string, RegionDef>
        {
            { "baba", new RegionDef { n = "Baba Ocağı", cost = 0, lvl = 1, rect = new float[] { -22, -9, 11, 11 } } },
            { "town", new RegionDef { n = "Kasaba", cost = 0, lvl = 1, rect = null } },
            { "dere", new RegionDef { n = "Dere Kenarı", cost = 600, lvl = 2, rect = new float[] { -6, -21, 24, -9 } } },
            { "kavak", new RegionDef { n = "Kavaklık", cost = 1800, lvl = 4, rect = new float[] { 11, -9, 26, 11 } } },
            { "yamac", new RegionDef { n = "Çam Yamacı", cost = 4500, lvl = 6, rect = new float[] { -31, -21, -6, -9 } } },
            { "ova", new RegionDef { n = "Doğu Ovası", cost = 9000, lvl = 8, rect = new float[] { 26, -15, 46, 11 } } },
            { "bagyolu", new RegionDef { n = "Bağ Yolu", cost = 30000, lvl = 18, rect = new float[] { -42, -9, -22, 11 } } },
        });

        static LocDef Data_L(string n, string reg, float[] p, float r, float h, string e, string kind = null, string[] units = null, float[][] slots = null)
            => new LocDef { n = n, reg = reg, p = p, r = r, h = h, e = e, kind = kind, units = units == null ? null : units.ToList(), slots = slots == null ? null : slots.ToList() };

        // Haritadaki konumlar
        static Dictionary<string, LocDef> _loc;
        public static Dictionary<string, LocDef> LOC => _loc ?? (_loc = Data_Locs());
        static Dictionary<string, LocDef> Data_Locs()
        {
            var L = new Dictionary<string, LocDef>
            {
                { "ev", Data_L("Çiftlik Evi", "baba", new float[] { -1, 0, 0 }, 4, 7, "🏡", "house") },
                { "tarla", Data_L("Tarlalar", "baba", new float[] { -14.5f, 0, .5f }, 7.5f, 3.2f, "🌾", "fields") },
                { "kumes", Data_L("Kümes", "baba", new float[] { 6.2f, 0, -4.2f }, 3.5f, 3.8f, "🐔", null, new[] { "kumes" }) },
                { "silo", Data_L("Ambar ve Silo", "baba", new float[] { 6.8f, 0, 5.2f }, 3.5f, 5.4f, "🛢️", null, new[] { "silo" }) },
                { "pazar", Data_L("Köy Pazarı", "town", new float[] { -8, 0, 18 }, 5.5f, 4, "🏪", "market") },
                { "han", Data_L("Tüccar Hanı", "town", new float[] { 8, 0, 18 }, 5.5f, 5.2f, "📜", "orders") },
                { "atolye", Data_L("Değirmen Sokağı", "dere", new float[] { 2, 0, -15 }, 9, 6, "⚙️", null, new[] { "yemhane", "degirmen", "firin", "mandira", "konserve", "yaghane", "dokuma" },
                    new[] { new float[] { -6.6f, -2.4f }, new float[] { -2.2f, -2.4f }, new float[] { 2.2f, -2.4f }, new float[] { 6.6f, -2.4f }, new float[] { -4.4f, 2.4f }, new float[] { 0, 2.4f }, new float[] { 4.4f, 2.4f } }) },
                { "ahir", Data_L("Ahır", "dere", new float[] { 17, 0, -15 }, 4, 5, "🐄", null, new[] { "ahir" }) },
                { "agil", Data_L("Ağıl", "kavak", new float[] { 17.5f, 0, -3 }, 4, 3.2f, "🐑", null, new[] { "agil" }) },
                { "bahce", Data_L("Arılık ve Bahçe", "kavak", new float[] { 18.5f, 0, 6 }, 5, 3.6f, "🍎", null, new[] { "kovan", "bahce" }, new[] { new float[] { -2.4f, 0 }, new float[] { 2.4f, 0 } }) },
                { "besi", Data_L("Besi Ahırı", "yamac", new float[] { -13, 0, -15 }, 4, 3.4f, "🐂", null, new[] { "besi" }) },
                { "sera", Data_L("Seralar", "yamac", new float[] { -24, 0, -15 }, 4, 4, "🏕️", null, new[] { "sera" }) },
                { "panayir", Data_L("Panayır", "town", new float[] { 24, 0, 18.5f }, 5.5f, 4, "🎪", "fair") },
                { "baglar", Data_L("Bağlar", "bagyolu", new float[] { -32, 0, 1 }, 5.5f, 3.6f, "🍇", null, new[] { "bag", "pekmezhane" }, new[] { new float[] { -2.7f, 0 }, new float[] { 2.7f, 0 } }) },
                { "hipo", Data_L("Yarış Alanı", "ova", new float[] { 36, 0, -1 }, 9.5f, 4.5f, "🏇", "race", new[] { "hara" }, new[] { new float[] { 0, -9.4f } }) },
            };
            foreach (var kv in SIGN) L[kv.Key].s = kv.Value;
            return L;
        }

        static Dictionary<string, float[]> _sign;
        public static Dictionary<string, float[]> SIGN => _sign ?? (_sign = new Dictionary<string, float[]>
        {
            { "baglar", new float[] { 0, 3.4f } }, { "ev", new float[] { 0, 3.4f } }, { "tarla", new float[] { 0, 7.2f } }, { "kumes", new float[] { 0, 2.6f } }, { "silo", new float[] { 0, 2.6f } },
            { "pazar", new float[] { 0, 3.6f } }, { "han", new float[] { 0, 2.6f } }, { "atolye", new float[] { 0, 5 } }, { "ahir", new float[] { 0, 2.6f } }, { "agil", new float[] { 0, 2.6f } },
            { "bahce", new float[] { 0, 2.8f } }, { "besi", new float[] { 0, 2.6f } }, { "sera", new float[] { 0, 2.6f } }, { "hipo", new float[] { 0, 6.8f } }, { "panayir", new float[] { 0, 4.2f } },
        });
        public static readonly Dictionary<string, int> INSIDE_UNITS = new Dictionary<string, int> { { "kumes", 1 }, { "ahir", 1 }, { "agil", 1 }, { "besi", 1 }, { "hara", 1 } };
        public static readonly List<double> PLOT_COST = new List<double> { 0, 0, 150, 300, 500, 800, 1200, 1800, 2600 };
        public static readonly List<int> PLOT_LV = new List<int> { 1, 1, 1, 2, 3, 4, 5, 6, 7 };
        public static readonly List<double> FIELD_UP = new List<double> { 0, 600, 2500, 9000, 22000 };
        public const int MAXL = 5;
        // Seviye 4 ve 5 için gereken ev ve oyuncu seviyesi
        public static UpReqDef UpReq(int l) => new UpReqDef { house = System.Math.Min(4, l), lv = l >= 5 ? 16 : l >= 4 ? 12 : 0 };
        public static string UpBlock(int l) { var q = UpReq(l); return S.house < q.house ? "🏠 " + HOUSE[q.house - 1].n : S.lv < q.lv ? "🔒 Sv " + q.lv : ""; }
        public static readonly List<string> ERAS = new List<string> { "Kulübe dönemi", "Köy çiftliği", "Büyüyen çiftlik", "Çiftlik işletmesi", "Tarım imparatorluğu" };
        public static string LocOf(string u) { foreach (var kv in LOC) if (kv.Value.units != null && kv.Value.units.Contains(u)) return kv.Key; return null; }
    }
}
