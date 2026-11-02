// ================= ÇEKİRDEK =================
// Web sürümündeki tüm modüller aynı küresel kapsamı paylaşır. Unity sürümünde bu kapsam "Game" adlı statik partial sınıftır:
// her JS modülü (src/js/NN-ad.js) Assets/Scripts/Modules/NN_Ad.cs dosyasında "public static partial class Game" olarak taşınır.
// Adlandırma: JS küresel adı PascalCase olur (addXP → AddXP, sheetBarn → SheetBarn, ITEMS → ITEMS).
// Modülün üst düzey (yüklenirken çalışan) kodu "Init_NN" yöntemine konur; Boot bunları src/js/ORDER sırasıyla çağırır.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public static State S;
        public static GameHost Host;

        // ---------- Zaman ----------
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static double TIME_OFF = 0;                        // sunucu saati farkı (ms)
        public static double DateNow() => (DateTime.UtcNow - Epoch).TotalMilliseconds; // JS Date.now()
        public static double Now() => DateNow() + TIME_OFF;       // JS now()
        public static double PerfNow() => Time.realtimeSinceStartupAsDouble * 1000.0; // JS performance.now()
        public static DateTime LocalDate(double ms) => Epoch.AddMilliseconds(ms).ToLocalTime();

        // ---------- JS sayı yardımcıları (JS ile aynı sonuçları vermeleri önemli) ----------
        static readonly System.Random _rng = new System.Random();
        public static double Rnd() => _rng.NextDouble();                         // Math.random()
        public static double Round(double x) => Math.Floor(x + 0.5);              // Math.round (C# Math.Round bankacı yuvarlaması yapar, kullanma)
        public static int RoundI(double x) => (int)Math.Floor(x + 0.5);
        public static int FloorI(double x) => (int)Math.Floor(x);
        public static int CeilI(double x) => (int)Math.Ceiling(x);
        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
        public static T Pick<T>(IList<T> a) => a.Count == 0 ? default(T) : a[(int)Math.Floor(Rnd() * a.Count)];
        public static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        // Sözlükten değer (yoksa 0 / varsayılan) — JS "o[k]||0"
        public static int Get(Dictionary<string, int> d, string k) { int v; return d != null && k != null && d.TryGetValue(k, out v) ? v : 0; }
        public static double Get(Dictionary<string, double> d, string k) { double v; return d != null && k != null && d.TryGetValue(k, out v) ? v : 0; }
        public static TV GetOr<TV>(Dictionary<string, TV> d, string k, TV def = default(TV)) { TV v; return d != null && k != null && d.TryGetValue(k, out v) ? v : def; }
        public static void Inc(Dictionary<string, int> d, string k, int n = 1) { d[k] = Get(d, k) + n; }
        public static void Inc(Dictionary<string, double> d, string k, double n = 1) { d[k] = Get(d, k) + n; }

        // JS toLocaleString('tr-TR') tam sayı: 12.345
        public static string Fmt(double n)
        {
            var v = Math.Floor(n);
            var neg = v < 0; if (neg) v = -v;
            var s = v.ToString("0", CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++) { if (i > 0 && (s.Length - i) % 3 == 0) sb.Append('.'); sb.Append(s[i]); }
            return (neg ? "-" : "") + sb.ToString();
        }
        // Ondalıklı (tr-TR): 1,5
        public static string FmtDec(double n, int digits = 1) => n.ToString("0." + new string('#', digits), CultureInfo.InvariantCulture).Replace('.', ',');
        public static string Pad2(double n) => ((int)n).ToString("00");

        // ---------- Kalıcı küçük depolama (localStorage karşılığı) ----------
        public static string LsGet(string k) { try { return PlayerPrefs.HasKey(k) ? PlayerPrefs.GetString(k) : null; } catch { return null; } }
        public static void LsSet(string k, string v) { try { PlayerPrefs.SetString(k, v); PlayerPrefs.Save(); } catch { } }
        public static void LsRemove(string k) { try { PlayerPrefs.DeleteKey(k); PlayerPrefs.Save(); } catch { } }

        // ---------- JSON ----------
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace, // varsayılan listeler/sözlükler kayıttakiyle birleşmesin
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Double,
            DateParseHandling = DateParseHandling.None,             // tarih gibi görünen dizgiler (ek alanlar) DateTime olup biçim değiştirmesin
            Error = (s, e) => { e.ErrorContext.Handled = true; },   // bozuk alan → atla, onarım (Migrate) düzeltir
        };
        public static string ToJson(object o) => JsonConvert.SerializeObject(o, JsonSettings);
        public static T FromJson<T>(string j) { try { return JsonConvert.DeserializeObject<T>(j, JsonSettings); } catch { return default(T); } }

        // ---------- Zamanlayıcılar (setTimeout / setInterval) ----------
        public static int SetTimeout(Action a, double ms) => Host.AddTimer(a, ms, false);
        public static int SetInterval(Action a, double ms) => Host.AddTimer(a, ms, true);
        public static void ClearTimer(int id) { if (Host != null) Host.RemoveTimer(id); }

        // Her karede çağrılır (requestAnimationFrame). dt saniye.
        public static event Action<float> OnFrame;
        // Her abone ayrı korunur: birinin istisnası diğerlerini (ör. arayüz yeniden boyutlama, döngü) o karede durdurmasın.
        internal static void RaiseFrame(float dt)
        {
            var h = OnFrame; if (h == null) return;
            foreach (Action<float> a in h.GetInvocationList())
            {
                try { a(dt); } catch (Exception e) { Debug.LogException(e); ReportError(e); }
            }
        }

        // Sayfa görünürlüğü (document.hidden / visibilitychange)
        public static bool Hidden;
        public static event Action<bool> OnVisibility;
        internal static void SetHidden(bool h) { if (Hidden == h) return; Hidden = h; OnVisibility?.Invoke(h); }

        // ---------- Eylem dağıtıcısı (data-act karşılığı) ----------
        // HTML'deki data-act="x" data-k="y" → UI.ActBtn(p,"etiket","x","k","y") ya da Game.Act("x", D("k","y")).
        // Her modül kendi eylemlerini Init_NN içinde RegisterAct ile kaydeder.
        static readonly Dictionary<string, Action<Dictionary<string, string>>> _acts = new Dictionary<string, Action<Dictionary<string, string>>>();
        public static void RegisterAct(string name, Action<Dictionary<string, string>> h) { _acts[name] = h; }
        public static bool HasAct(string name) => _acts.ContainsKey(name);
        public static void Act(string name, Dictionary<string, string> d = null)
        {
            Action<Dictionary<string, string>> h;
            if (name != null && _acts.TryGetValue(name, out h)) { try { h(d ?? new Dictionary<string, string>()); } catch (Exception e) { Debug.LogException(e); ReportError(e); } }
            else Debug.LogWarning("Bilinmeyen eylem: " + name);
        }
        public static Dictionary<string, string> D(params string[] kv)
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }
        public static string Arg(Dictionary<string, string> d, string k, string def = null) { string v; return d != null && d.TryGetValue(k, out v) ? v : def; }
        public static int ArgI(Dictionary<string, string> d, string k, int def = 0) { int v; return int.TryParse(Arg(d, k), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : def; }
        public static double ArgD(Dictionary<string, string> d, string k, double def = 0) { double v; return double.TryParse(Arg(d, k), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : def; }

        // Hata raporu kancası (35-cloud dolduruyor)
        public static Action<Exception> ErrorReporter;
        public static void ReportError(Exception e) { try { ErrorReporter?.Invoke(e); } catch { } }

        // ---------- Açılış ----------
        // src/js/ORDER ile aynı sıra. Her modül Init_<NN> (ör. Init_06, Init_000) tanımlayabilir; yoksa atlanır.
        public static readonly string[] ORDER = {
            "000","00","01","02","03","04","05","06","07","08","09","10","11","12","13","14","15","16","17","18","19","20",
            "21","22","23","24","25","26","27","28","29","30","31","32","34","35","36","37","38","39","40","41","33"
        };
        internal static void BootModules()
        {
            var t = typeof(Game);
            foreach (var k in ORDER)
            {
                var m = t.GetMethod("Init_" + k, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) continue;
                try { m.Invoke(null, null); }
                catch (TargetInvocationException e) { Debug.LogException(e.InnerException ?? e); ReportError(e.InnerException ?? e); }
            }
        }
    }
}
