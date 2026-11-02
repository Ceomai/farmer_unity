// ================= DURUM (02-state.js) =================
// Kayıt biçimi web sürümüyle aynıdır: alan adları JS anahtarlarıyla birebir, JSON (Newtonsoft) ile okunur/yazılır.
// Böylece web kaydı ya da bulut kaydı Unity sürümüne aktarılabilir.
// Tüm sınıflar "partial": bir modül kendi alanını kendi dosyasında ekleyebilir (alan sahibi modül tanımlar).
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ciftlik
{
    public partial class Job
    {
        public string r;            // tarif / ürün kimliği
        public double s, e;         // başlangıç ve bitiş (ms, Game.Now())
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class Unit
    {
        public int l = 1;           // bina seviyesi
        public Job job;             // süren iş
        public string last;         // son tarif
        public List<string> q;      // üretim kuyruğu (en çok 3)
        public double? built;       // kurulma zamanı
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class Field
    {
        public Job job;
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class SatEntry { public double v, t; }

    public partial class Market
    {
        public double at;
        public Dictionary<string, double> m = new Dictionary<string, double>();          // fiyat çarpanı
        public Dictionary<string, List<double>> h = new Dictionary<string, List<double>>(); // son 8 çarpan
        public Dictionary<string, SatEntry> sat;                                          // pazar doygunluğu
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class FinDay { public string d; public double i, e; }

    public partial class Fin
    {
        public string day = "";
        public Dictionary<string, double> inc = new Dictionary<string, double>();
        public Dictionary<string, double> exp = new Dictionary<string, double>();
        public List<FinDay> hist = new List<FinDay>();
    }

    public partial class Order
    {
        public string who;
        public Dictionary<string, int> items = new Dictionary<string, int>();
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class Contract
    {
        public string who;
        public string k;
        public int total;
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class Horse
    {
        public string n;
        public int spd, sta, w;
        public double rest, train;
    }

    public partial class League { public int tier; public int pts; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class Decor
    {
        public Dictionary<string, int> owned = new Dictionary<string, int>();
        public Dictionary<string, string> house = new Dictionary<string, string>();
        public Dictionary<string, string> farm = new Dictionary<string, string>();
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class Ads { public string day = ""; public int n; }

    public partial class Sound { public bool music = true, sfx = true, notif = false; }

    public partial class DailyTask
    {
        public string k, e, d;
        public double n, p, r;
        public bool c;
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class Daily
    {
        public string day;
        public int streak = 1;
        public bool login, bonus;
        public List<DailyTask> tasks = new List<DailyTask>();
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }

    public partial class ContestResult { public int place; public int all; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class Contests
    {
        public string day = "";
        public List<string> list = new List<string>();
        public Dictionary<string, ContestResult> done = new Dictionary<string, ContestResult>();
    }

    public partial class Weather { public string k = "sun"; public double until; }

    public partial class Consent { public bool analytics; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class Tut { public int? i; public int? done; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class UiPrefs { public string fs; public bool left; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class Recent { public string loc; public string n; public double at; }

    public partial class Love { public double v = 50, t; public Dictionary<string, double> pet = new Dictionary<string, double>(); [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class Friend { public int d; public string gift = ""; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class Evt { public double next; public string cur; [JsonExtensionData] public IDictionary<string, JToken> extra; }

    public partial class State
    {
        public double coins = 300, xp = 0;
        public int lv = 1;
        public double rep = 50;
        public Dictionary<string, int> items = new Dictionary<string, int> { { "yem", 6 } };
        public int house = 1;
        public Dictionary<string, int> regions = new Dictionary<string, int> { { "baba", 1 }, { "town", 1 } };
        public Dictionary<string, Unit> units = new Dictionary<string, Unit> { { "kumes", new Unit { l = 1 } } };
        public List<Field> fields = new List<Field> { new Field(), new Field() };
        public int fieldLv = 1;
        public string lastCrop = "bugday";
        public List<Order> orders = new List<Order>();
        public Market market = new Market();
        public Fin fin = new Fin();
        public Dictionary<string, double> stats = new Dictionary<string, double>();
        public int goal = 0;
        public double created;
        public int gems = 5;
        public Dictionary<string, int> album = new Dictionary<string, int>();
        public Dictionary<string, double> workers = new Dictionary<string, double>(); // bina → vardiya bitişi (ms)
        public List<Contract> contracts = new List<Contract>();
        public List<Horse> horses = new List<Horse>();
        public League league = new League();
        public Decor decor = new Decor();
        public Ads ads = new Ads();
        public Sound sound = new Sound();
        public Daily daily;
        public Contests contests = new Contests();
        public Weather weather = new Weather();
        public double lastSeen;
        public Consent consent;

        // Sonradan eklenen (isteğe bağlı) alanlar
        public Tut tut, tut2;
        public UiPrefs ui;
        public Recent recent;
        public Dictionary<string, Love> love;
        public Dictionary<string, Friend> friends;
        public Evt evt;
        public Dictionary<string, int> quests;
        public Dictionary<string, int> ach;
        public Dictionary<string, double> proj;
        public int gen;
        public int keepN = 10;
        public bool autoSell, fastNav;
        public double haggleAt, rescueAt;

        // Tanınmayan alanlar kaybolmasın (web sürümünün yeni alanları vb.)
        [JsonExtensionData] public IDictionary<string, JToken> extra;
    }
}
