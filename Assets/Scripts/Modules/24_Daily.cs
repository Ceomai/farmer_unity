// ================= GÜNLÜK ÖDÜL, GÖREVLER, DÖNÜŞ (24-daily.js) =================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class LoginDef { public double c; public int yem, g; }
    public class DTaskDef { public string k, e, d; public int a, b; public double r; public string need; }

    public static partial class Game
    {
        static string Daily_DayStr(DateTime d) => d.Year + "-" + d.Month + "-" + d.Day;
        public static string TodayStr() => Daily_DayStr(LocalDate(DateNow()));

        public static readonly List<LoginDef> LOGIN = new List<LoginDef> {
            new LoginDef { c = 100 }, new LoginDef { yem = 20 }, new LoginDef { c = 250 }, new LoginDef { g = 2 }, new LoginDef { c = 500 }, new LoginDef { yem = 40, c = 300 }, new LoginDef { g = 6 } };
        public static string RewardTxt(LoginDef r) =>
            string.Join(" ", new[] { r.c != 0 ? "₺" + G2_Num(r.c) : "", r.yem != 0 ? "🌰" + r.yem : "", r.g != 0 ? "🍀" + r.g : "" }.Where(x => x != ""));

        public static readonly List<DTaskDef> DTASK = new List<DTaskDef> {
            new DTaskDef { k = "plant", e = "🌱", d = "{n} kez tarlaya ekim yap", a = 5, b = 12, r = 80 },
            new DTaskDef { k = "harvest_field", e = "🌾", d = "{n} parsel hasat et", a = 4, b = 10, r = 90 },
            new DTaskDef { k = "harvest_kumes", e = "🥚", d = "Kümesten {n} kez yumurta topla", a = 2, b = 4, r = 80, need = "kumes" },
            new DTaskDef { k = "sold", e = "💰", d = "Pazarda {n} ürün sat", a = 10, b = 30, r = 100 },
            new DTaskDef { k = "orders", e = "📜", d = "{n} sipariş teslim et", a = 1, b = 3, r = 150 },
            new DTaskDef { k = "harvest_ahir", e = "🥛", d = "Ahırdan {n} kez süt al", a = 1, b = 3, r = 150, need = "ahir" },
            new DTaskDef { k = "mg", e = "🎮", d = "{n} mini oyun oyna", a = 1, b = 2, r = 120, need = "kumes" },
            new DTaskDef { k = "contest", e = "🎪", d = "Panayırda {n} yarışmaya katıl", a = 1, b = 1, r = 150, need = "lv3" },
        };

        public static bool CheckDaily()
        {
            var t = TodayStr();
            if (S.daily != null && S.daily.day == t) return false;
            var ys = Daily_DayStr(LocalDate(DateNow() - 864e5));
            int streak = S.daily != null && S.daily.day == ys ? (S.daily.streak % 7) + 1 : 1;
            var pool = G2_Shuffle(DTASK.Where(x => x.need == null || (x.need == "lv3" ? S.lv >= 3 : S.units.ContainsKey(x.need)))).Take(3).ToList();
            S.daily = new Daily
            {
                day = t, streak = streak, login = false, bonus = false,
                tasks = pool.Select(x =>
                {
                    int n = x.a + FloorI(Rnd() * (x.b - x.a + 1));
                    int at = x.d.IndexOf("{n}");
                    var dd = at >= 0 ? x.d.Substring(0, at) + n + x.d.Substring(at + 3) : x.d;
                    return new DailyTask { k = x.k, e = x.e, d = dd, n = n, p = 0, r = x.r, c = false };
                }).ToList()
            };
            Save();
            return true;
        }

        public static void ShowLogin()
        {
            var Dd = S.daily;
            if (Dd == null || Dd.login || G2_ModalOpen("login")) return;
            RectTransform card;
            var d = G2_Modal("login", out card);
            G2_H2(card, "Günlük ödül", 26);
            G2_Note(card, $"Her gün uğra, ödül büyüsün! {Dd.streak}. gün", TextAnchor.UpperCenter);
            // .cal: 7 sütun
            var cal = UI.HStack(card, 4, TextAnchor.MiddleCenter, UI.Pad(0, 0, 10, 14), "cal");
            cal.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            for (int i = 0; i < LOGIN.Count; i++)
            {
                bool gotIt = i + 1 < Dd.streak, cur = i + 1 == Dd.streak;
                var sp = UI.Box(cal, cur ? UI.C.honey : UI.C.card, 10, 2, UI.C.ink, null, "day");
                UI.Size(sp, 0, -1, 1);
                var v = sp.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = UI.Pad(1, 1, 4, 4); v.spacing = 2; v.childAlignment = TextAnchor.UpperCenter; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                UI.Txt(sp, (i + 1) + ". gün", 9, UI.C.muted, true, TextAnchor.MiddleCenter, true);
                UI.Txt(sp, RewardTxt(LOGIN[i]), 10, UI.C.text, true, TextAnchor.MiddleCenter, true);
                if (gotIt) sp.gameObject.AddComponent<CanvasGroup>().alpha = .45f;
                if (cur) sp.localScale = new Vector3(1.08f, 1.08f, 1);
            }
            G2_BigBtn(card, "Ödülü al", () =>
            {
                var r = LOGIN[Dd.streak - 1];
                if (r.c != 0) Gain(r.c, "Ödül");
                if (r.yem != 0) GiveItems(new Dictionary<string, int> { { "yem", r.yem } });
                if (r.g != 0) S.gems += r.g;
                Dd.login = true; Sfx("win"); FlyCoins(6); UI.Kill(d); Commit();
            });
        }

        public static void DailyHTML(Transform parent)
        {
            var Dd = S.daily; if (Dd == null) return;
            var unit = G2_Unit(parent);
            G2_Uhead(unit, "📅 Günlük görevler", $"🔥 {Dd.streak}. gün");
            for (int i = 0; i < Dd.tasks.Count; i++)
            {
                var t = Dd.tasks[i];
                var job = G2_Job(unit);
                G2_Big(job, t.e);
                var pw = G2_Pw(job);
                G2_PwB(pw, t.d, 13);
                G2_Pbar(pw, (float)(Math.Min(100, t.p / t.n * 100) / 100));
                G2_PwSmall(pw, $"{G2_Num(Math.Min(t.p, t.n))}/{G2_Num(t.n)} · ₺{G2_Num(t.r)}");
                G2_PillAct(job, t.c ? "✓" : "Al", "dclaim", "gold", t.p >= t.n && !t.c, "i", i.ToString());
            }
            if (Dd.tasks.All(t => t.c) && !Dd.bonus) UI.ActBtn(unit, "Hepsi tamam! +🍀2", "dbonus", "big gold");
        }

        public static int CountReady()
        {
            int n = S.fields.Count(f => f != null && f.job != null && Now() >= f.job.e);
            foreach (var s in S.units.Values) if (s != null && s.job != null && Now() >= s.job.e) n++;
            return n;
        }

        public static void AwaySummary(double away)
        {
            if (away < 120000) return;
            var mins = Round(away / 60000); int ready = CountReady();
            if (ready == 0 && AutoGain == 0) return;
            RectTransform card;
            var root = G2_Modal("login", out card);
            G2_Badge(card, "👋");
            G2_H2(card, "Hoş geldin!", 28);
            G2_P(card, (mins >= 60 ? Math.Floor(mins / 60) + " saat" : mins + " dakika") + " yoktun."
                + (AutoGain != 0 ? $"\nİşçilerin <b>{AutoGain}</b> ürün topladı." : "")
                + (ready != 0 ? $"\n<b>{ready}</b> üretim toplanmayı bekliyor." : ""));
            G2_BigBtn(card, "Çiftliğe dön", () => UI.Kill(root));
        }

        public static int LastReady = 0;
        // document.title karşılığı (pencere başlığı olmayan platformlarda platform katmanı rozet/başlık için okuyabilir)
        public static string DocTitle = "Çiftlik İmparatorluğu";
        public static void NotifyReady()
        {
            int rc = CountReady();
            DocTitle = (rc != 0 ? $"({rc}) " : "") + "Çiftlik İmparatorluğu";
            if (S.sound.notif && Hidden && rc > LastReady)
            {
                try { PLATFORM.LocalNotify("Çiftlik İmparatorluğu", $"{rc} ürün toplanmayı bekliyor 🌾"); } catch (Exception) { }
            }
            LastReady = rc;
        }

        // ---------------------------------------------------------------------------------------------
        // Kayıt onarımı: eski sürüm, yarım kalmış, elle bozulmuş ya da tanınmayan değer içeren her kayıt oyunu açılabilir hale getirilir.
        // Bilinmeyen bina/ürün/tarif atılır, sayılar sınırlara çekilir, ileri tarihli işler (saat geri alınmışsa) makul süreye indirilir.
        // Not: Unity sürümünde kayıt önce türlü State'e okunur (Newtonsoft; tür uyuşmazlığı olan alan atlanır → varsayılan kalır),
        // ardından burada JS migrate() ile aynı kurallarla onarılır.
        static double Mig_Num(double v, double def, double lo = double.NegativeInfinity, double hi = double.PositiveInfinity)
        {
            if (!IsFinite(v)) v = def;
            return Math.Min(hi, Math.Max(lo, v));
        }
        static int Mig_Int(double v, double def, double lo, double hi) => (int)Math.Floor(Mig_Num(v, def, lo, hi));

        public static State Migrate(State s)
        {
            var d = Fresh();
            if (s == null) return d;
            // for(const k in d){if(s[k]==null)s[k]=d[k] ...}
            foreach (var f in typeof(State).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType.IsValueType || f.IsInitOnly) continue;
                if (f.GetValue(s) == null) { var dv = f.GetValue(d); if (dv != null) f.SetValue(s, dv); }
            }
            if (!(s.created > 0) || !IsFinite(s.created)) s.created = d.created;

            s.coins = Mig_Num(s.coins, d.coins, 0); s.gems = Mig_Int(s.gems, d.gems, 0, 1e9); s.lv = Mig_Int(s.lv, 1, 1, 999); s.xp = Mig_Num(s.xp, 0, 0); s.rep = Mig_Num(s.rep, 50, 0, 100);
            s.house = Mig_Int(s.house, 1, 1, HOUSE.Count); s.fieldLv = Mig_Int(s.fieldLv, 1, 1, MAXL); s.goal = Mig_Int(s.goal, 0, 0, GOALS.Count);

            var items = new Dictionary<string, int>();
            foreach (var kv in s.items) if (kv.Key != null && ITEMS.ContainsKey(kv.Key)) { int v = Mig_Int(kv.Value, 0, 0, 1e9); if (v != 0) items[kv.Key] = v; }
            s.items = items;
            var rg = new Dictionary<string, int> { { "baba", 1 }, { "town", 1 } };
            foreach (var kv in s.regions) if (kv.Key != null && REG.ContainsKey(kv.Key) && kv.Value != 0) rg[kv.Key] = 1;
            s.regions = rg;

            double T = Now();
            Func<Job, double, Job> fixJob = (j, sec) =>
            {
                if (j == null) return null;
                // Eksik zaman alanı türlü okumada 0 olur; JS'teki "tanımsız → şimdi" kuralı için 0 eksik sayılır
                j.s = j.s == 0 ? T : Mig_Num(j.s, T); j.e = j.e == 0 ? T : Mig_Num(j.e, T);
                if (j.e > T + sec * 1000 * 2) { j.s = T; j.e = T + sec * 1000; }
                return j;
            };
            var un = new Dictionary<string, Unit>();
            foreach (var kv in s.units)
            {
                var u = kv.Key; var x = kv.Value;
                if (u == null || !B.ContainsKey(u) || x == null) continue;
                var o = new Unit { l = Mig_Int(x.l, 1, 1, MAXL) };
                var rs = B[u].recipes ?? new List<Recipe>();
                if (x.job != null) { var r = rs.Find(rr => rr.id == x.job.r); if (r != null) o.job = fixJob(x.job, r.t); }
                if (rs.Find(rr => rr.id == x.last) != null) o.last = x.last;
                if (x.q != null) { var q = x.q.Where(id => rs.Find(rr => rr.id == id) != null).Take(3).ToList(); if (q.Count > 0) o.q = q; }
                if (x.built.HasValue && x.built.Value != 0 && !double.IsNaN(x.built.Value)) o.built = Mig_Num(x.built.Value, 0);
                un[u] = o;
            }
            s.units = un;
            s.fields = s.fields.Take(9).Select(f =>
            {
                if (f == null) return new Field();
                if (f.job != null) { var c = CropOf(f.job.r); return c != null ? new Field { job = fixJob(f.job, c.t) } : new Field(); }
                return new Field();
            }).ToList();
            while (s.fields.Count < 2) s.fields.Add(new Field());
            if (CropOf(s.lastCrop) == null) s.lastCrop = "bugday";
            if (s.weather.k == null || !WEATHER.ContainsKey(s.weather.k)) s.weather = new Weather { k = "sun", until = 0 };

            var wk = new Dictionary<string, double>();
            foreach (var kv in s.workers)
            {
                var k = kv.Key;
                if (k == "fields" || (k != null && B.ContainsKey(k) && B[k].recipes != null && un.ContainsKey(k)))
                {
                    double v = kv.Value;
                    wk[k] = v > 1e12 && v < T + SHIFT * 2 ? v : T + SHIFT;
                }
            }
            s.workers = wk;
            s.horses = s.horses.Where(h => h != null).Take(4).Select(h => new Horse
            {
                n = !string.IsNullOrEmpty(h.n) ? h.n : "Rüzgar", spd = Mig_Int(h.spd, 5, 1, 20), sta = Mig_Int(h.sta, 5, 1, 20), w = Mig_Int(h.w, 0, 0, 1e6),
                rest = Mig_Num(h.rest, 0), train = Mig_Num(h.train, 0)
            }).ToList();
            if (s.league == null) s.league = d.league;
            s.league.tier = Mig_Int(s.league.tier, 0, 0, TIERS.Count - 1); s.league.pts = Mig_Int(s.league.pts, 0, 0, 1e6);
            Func<Dictionary<string, int>, bool> okItems = o => o != null && o.Count > 0 && o.All(kv => kv.Key != null && ITEMS.ContainsKey(kv.Key) && kv.Value > 0);
            s.orders = s.orders.Where(o => o != null && okItems(o.items)).ToList();
            s.contracts = s.contracts.Where(c => c != null && c.k != null && ITEMS.ContainsKey(c.k) && c.total > 0).ToList();
            if (s.daily != null && s.daily.tasks == null) s.daily = null;
            if (s.daily != null)
            {
                s.daily.streak = Mig_Int(s.daily.streak, 1, 1, 7);
                s.daily.tasks = s.daily.tasks.Where(t => t != null && IsFinite(t.n) && t.n > 0 && t.d != null).Select(t => { t.p = Mig_Num(t.p, 0, 0, 1e9); t.r = Mig_Num(t.r, 0, 0, 1e7); return t; }).ToList();
            }
            {
                var m = new Dictionary<string, double>(); var sat = new Dictionary<string, SatEntry>();
                if (s.market.m != null) foreach (var kv in s.market.m) { double v = kv.Value; if (kv.Key != null && ITEMS.ContainsKey(kv.Key) && IsFinite(v) && v > 0 && v < 5) m[kv.Key] = v; }
                if (s.market.sat != null) foreach (var kv in s.market.sat) { var x = kv.Value; if (kv.Key != null && ITEMS.ContainsKey(kv.Key) && x != null && IsFinite(x.v) && x.v >= 0 && IsFinite(x.t) && x.t <= T) sat[kv.Key] = new SatEntry { v = x.v, t = x.t }; }
                s.market.m = m; s.market.sat = sat;
            }
            if (s.contests == null || s.contests.list == null || s.contests.done == null) s.contests = d.contests;
            s.contests.list = s.contests.list.Where(id => CONTESTS.Find(c => c.id == id) != null).ToList();
            if (s.proj != null) foreach (var k in s.proj.Keys.ToList()) if (PROJ.Find(p => p.id == k) == null) s.proj.Remove(k);
            if (s.decor == null) s.decor = d.decor;
            if (s.decor.house == null) s.decor.house = new Dictionary<string, string>();
            if (s.decor.farm == null) s.decor.farm = new Dictionary<string, string>();
            if (s.decor.owned == null) s.decor.owned = new Dictionary<string, int>();
            // sound: türlü alanlar her zaman bool (bozuk değer okunurken atlanır → fresh varsayılanı kalır)
            if (s.market.h == null) s.market.h = new Dictionary<string, List<double>>();
            s.market.at = Mig_Num(s.market.at, 0);
            if (s.fin.inc == null) s.fin.inc = new Dictionary<string, double>();
            if (s.fin.exp == null) s.fin.exp = new Dictionary<string, double>();
            if (s.fin.hist == null) s.fin.hist = new List<FinDay>();
            // love/friends/ui/evt/quests/ach/proj: nesne değilse okunurken atlanır (null kalır)
            if (s.love != null) foreach (var k in s.love.Keys.ToList()) if (s.love[k] == null) s.love.Remove(k);
            if (s.friends != null) foreach (var k in s.friends.Keys.ToList()) if (s.friends[k] == null) s.friends.Remove(k);
            s.keepN = new[] { 0, 5, 10, 20 }.Contains(s.keepN) ? s.keepN : 10;
            if (s.quests != null) foreach (var n in s.quests.Keys.ToList()) { if (!QUESTS.ContainsKey(n)) s.quests.Remove(n); else s.quests[n] = Mig_Int(s.quests[n], 0, 0, 3); }
            s.gen = Mig_Int(s.gen, 0, 0, 99);
            if (s.evt != null && s.evt.cur != null && !EVENTS.ContainsKey(s.evt.cur)) s.evt.cur = null;
            s.lastSeen = Math.Min(s.lastSeen == 0 ? DateNow() : Mig_Num(s.lastSeen, DateNow()), DateNow());
            // Güvenlik: kayıttaki metinler ekrana basılabildiği için (at adı, görev metni...) tüm metinlerden < > " ' ` temizlenir.
            // Böylece dışarıdan alınmış kötü niyetli bir kayıt (ör. "bu kodu gir" tuzağı) arayüzü bozamaz.
            Mig_Clean(s, 0);
            foreach (var h in s.horses) { var hn = (h.n ?? ""); if (hn.Length > 24) hn = hn.Substring(0, 24); h.n = hn.Length > 0 ? hn : "Rüzgar"; }
            s.orders = s.orders.Where(o => o.who != null && NAMES.Contains(o.who)).ToList();
            s.contracts = s.contracts.Where(c => c.who != null && NAMES.Contains(c.who)).ToList();
            if (s.recent != null && (s.recent.loc == null || !LOC.ContainsKey(s.recent.loc))) s.recent = null;
            if (s.extra != null) { s.extra.Remove("dark"); s.extra.Remove("rooster"); }
            return s;
        }

        static readonly Regex Mig_Bad = new Regex("[<>\"'`]");
        static string Mig_Str(string v) { v = Mig_Bad.Replace(v, ""); return v.Length > 2000 ? v.Substring(0, 2000) : v; }
        static bool Mig_IsLeaf(Type t) => t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || typeof(Delegate).IsAssignableFrom(t) || typeof(UnityEngine.Object).IsAssignableFrom(t);

        // clean(o,d): her düzeyde metin değerlerinden ve anahtarlardan < > " ' ` atılır (en çok 8 düzey)
        static void Mig_Clean(object o, int depth)
        {
            if (depth > 8 || o == null) return;
            var tp = o.GetType();
            if (Mig_IsLeaf(tp)) return;
            if (o is JObject jo)
            {
                foreach (var p in jo.Properties().ToList())
                {
                    if (p.Value.Type == JTokenType.String) p.Value = new JValue(Mig_Str((string)p.Value));
                    else if (p.Value is JContainer) Mig_Clean(p.Value, depth + 1);
                    if (Mig_Bad.IsMatch(p.Name)) { var nk = Mig_Bad.Replace(p.Name, ""); var v = p.Value; jo.Remove(p.Name); jo[nk] = v; }
                }
                return;
            }
            if (o is JArray ja)
            {
                for (int i = 0; i < ja.Count; i++)
                {
                    if (ja[i].Type == JTokenType.String) ja[i] = new JValue(Mig_Str((string)ja[i]));
                    else if (ja[i] is JContainer) Mig_Clean(ja[i], depth + 1);
                }
                return;
            }
            if (o is JToken) return;
            if (o is IDictionary dict)
            {
                var keys = new List<object>(); foreach (var k in dict.Keys) keys.Add(k);
                foreach (var k in keys)
                {
                    var v = dict[k];
                    if (v is string sv) dict[k] = Mig_Str(sv);
                    else if (v != null && !Mig_IsLeaf(v.GetType())) Mig_Clean(v, depth + 1);
                    if (k is string ks && Mig_Bad.IsMatch(ks)) { var nk = Mig_Bad.Replace(ks, ""); var nv = dict[k]; dict.Remove(k); dict[nk] = nv; }
                }
                return;
            }
            if (o is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v is string sv) list[i] = Mig_Str(sv);
                    else if (v != null && !Mig_IsLeaf(v.GetType())) Mig_Clean(v, depth + 1);
                }
                return;
            }
            foreach (var f in tp.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsInitOnly || f.IsLiteral) continue;
                var v = f.GetValue(o);
                if (v is string sv) f.SetValue(o, Mig_Str(sv));
                else if (v != null && !Mig_IsLeaf(v.GetType())) Mig_Clean(v, depth + 1);
            }
        }

        static void Init_24()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("dclaim", d =>
            // {
                // int i = ArgI(d, "i", -1);
                // var t = S.daily != null && i >= 0 && i < S.daily.tasks.Count ? S.daily.tasks[i] : null;
                // if (t != null && t.p >= t.n && !t.c) { t.c = true; Gain(t.r, "Ödül"); AddXP(15); Sfx("coin"); FlyCoins(4); Commit(); }
            // });
            // RegisterAct("dbonus", d =>
            // {
                // if (S.daily != null && S.daily.tasks.All(t => t.c) && !S.daily.bonus) { S.daily.bonus = true; S.gems += 2; Sfx("win"); Commit(); }
            // });
        }
    }
}
