// ================= BULUT (Firebase): kayıt, sunucu saati, analitik, hata raporu (35-cloud.js) =================
// Firebase Authentication (anonim) + Cloud Firestore REST arayüzleri kullanılır; ek SDK gerekmez (UnityWebRequest).
// Güvenlik kuralları: firebase/firestore.rules. CONFIG.firebaseApiKey ya da firebaseProjectId boşsa bulut tamamen kapalıdır.
// Uç noktalar ve JSON gövdeleri web sürümüyle birebir aynıdır; aynı kayıt belgesi web ve Unity sürümünden okunup yazılabilir.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Ciftlik
{
    public class CloudAccount { public string id, secret; }
    public class CloudTok { public string id, refresh; public double exp; }
    public class CloudStatus { public string status; public double last; }
    public class CloudEv { public string n; public JToken p; public double t; public int lv; public string pl; }
    public partial class Consent { public double at; }

    public class CloudError : Exception { public long status; public CloudError(string m, long st = 0) : base(m) { status = st; } }
    // Eşzamansız sonuç kutusu (Promise karşılığı): coroutine bitince val ya da err dolar
    public class CloudRes<T> { public T val; public Exception err; }

    public static partial class Game
    {
        public static bool CLOUD_ON => CONFIG != null && !string.IsNullOrEmpty(CONFIG.firebaseApiKey) && !string.IsNullOrEmpty(CONFIG.firebaseProjectId);
        public const string ID_KEY = "ci-account", TOK_KEY = "ci-fb-token";

        static string Cloud_Hex(byte[] b) { var sb = new StringBuilder(); foreach (var x in b) sb.Append(x.ToString("x2")); return sb.ToString(); }
        static byte[] Cloud_RandBytes(int n) { var b = new byte[n]; using (var r = RandomNumberGenerator.Create()) r.GetBytes(b); return b; }

        // Kurtarma kodu = rastgele kimlik + gizli anahtar. Firestore'daki kayıt belgesinin adı bu kodun SHA-256 özetidir;
        // kodu bilmeyen kayda ulaşamaz, kurallar listelemeyi yasaklar.
        public static CloudAccount Account()
        {
            CloudAccount a = null;
            try { a = FromJson<CloudAccount>(LsGet(ID_KEY) ?? "null"); } catch (Exception) { }
            if (a == null || a.id == null || a.secret == null)
            {
                a = new CloudAccount { id = Guid.NewGuid().ToString(), secret = Cloud_Hex(Cloud_RandBytes(16)) };   // crypto.randomUUID()
                try { LsSet(ID_KEY, ToJson(a)); } catch (Exception) { }
            }
            return a;
        }
        public static string Sha256(string t) { using (var h = SHA256.Create()) return Cloud_Hex(h.ComputeHash(Encoding.UTF8.GetBytes(t))); }
        public static string DocIdOf(CloudAccount a) => Sha256(a.id + "." + a.secret);

        // ---- Anonim oturum (Firebase Auth REST) ----
        public static CloudTok TOK = null;

        static UnityWebRequest Cloud_Req(string url, string method, string body, string contentType)
        {
            var r = new UnityWebRequest(url, method, new DownloadHandlerBuffer(), body != null ? new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)) : null);
            if (contentType != null) r.SetRequestHeader("Content-Type", contentType);
            r.timeout = 30;
            return r;
        }
        static bool Cloud_Ok(UnityWebRequest r) => r.result != UnityWebRequest.Result.ConnectionError && r.responseCode >= 200 && r.responseCode < 300;
        static bool Cloud_NetFail(UnityWebRequest r) => r.result == UnityWebRequest.Result.ConnectionError || r.responseCode == 0;

        public static IEnumerator FbToken(CloudRes<string> res)
        {
            if (TOK != null && TOK.exp > DateNow() + 60000) { res.val = TOK.id; yield break; }
            var key = CONFIG.firebaseApiKey;
            UnityWebRequest r = null;
            if (TOK != null && !string.IsNullOrEmpty(TOK.refresh))
            {
                r = Cloud_Req("https://securetoken.googleapis.com/v1/token?key=" + key, "POST", "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(TOK.refresh), "application/x-www-form-urlencoded");
                yield return r.SendWebRequest();
                if (Cloud_NetFail(r)) { res.err = new CloudError("fetch: " + r.error); r.Dispose(); yield break; }
            }
            if (r == null || !Cloud_Ok(r))
            {
                if (r != null) r.Dispose();
                r = Cloud_Req("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + key, "POST", JsonConvert.SerializeObject(new { returnSecureToken = true }), "application/json");
                yield return r.SendWebRequest();
                if (Cloud_NetFail(r)) { res.err = new CloudError("fetch: " + r.error); r.Dispose(); yield break; }
                if (!Cloud_Ok(r)) { res.err = new CloudError("auth " + r.responseCode, r.responseCode); r.Dispose(); yield break; }
                try { var j = JObject.Parse(r.downloadHandler.text); TOK = new CloudTok { id = (string)j["idToken"], refresh = (string)j["refreshToken"], exp = DateNow() + Cloud_Secs(j["expiresIn"]) * 1000 }; }
                catch (Exception e) { res.err = e; r.Dispose(); yield break; }
            }
            else
            {
                try { var j = JObject.Parse(r.downloadHandler.text); TOK = new CloudTok { id = (string)j["id_token"], refresh = (string)j["refresh_token"], exp = DateNow() + Cloud_Secs(j["expires_in"]) * 1000 }; }
                catch (Exception e) { res.err = e; r.Dispose(); yield break; }
            }
            r.Dispose();
            try { LsSet(TOK_KEY, ToJson(TOK)); } catch (Exception) { }
            res.val = TOK.id;
        }
        // +j.expiresIn||3600
        static double Cloud_Secs(JToken t) { double v; return t != null && double.TryParse(t.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out v) && v != 0 ? v : 3600; }

        public static string FS() => "https://firestore.googleapis.com/v1/projects/" + CONFIG.firebaseProjectId + "/databases/(default)/documents";

        // fs(method,path,body): 404 → val null; başarısızlık → err
        public static IEnumerator Fs(string method, string path, object body, CloudRes<JObject> res)
        {
            if (!CLOUD_ON) { res.err = new CloudError("bulut kapalı"); yield break; }
            double t0 = DateNow();
            var tk = new CloudRes<string>();
            yield return FbToken(tk);
            if (tk.err != null) { res.err = tk.err; yield break; }
            var r = Cloud_Req(FS() + path, method, body != null ? JsonConvert.SerializeObject(body) : null, "application/json");
            r.SetRequestHeader("Authorization", "Bearer " + tk.val);
            yield return r.SendWebRequest();
            if (Cloud_NetFail(r)) { res.err = new CloudError("fetch: " + r.error); r.Dispose(); yield break; }
            var dh = r.GetResponseHeader("Date") ?? r.GetResponseHeader("date");
            DateTimeOffset dto;
            if (dh != null && DateTimeOffset.TryParse(dh, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dto)) { double srv = dto.ToUnixTimeMilliseconds() + (DateNow() - t0) / 2; if (!double.IsNaN(srv)) ServerTime(srv); }
            if (r.responseCode == 404) { r.Dispose(); yield break; }
            if (!Cloud_Ok(r)) { res.err = new CloudError("firestore " + r.responseCode, r.responseCode); r.Dispose(); yield break; }
            try { var txt = r.downloadHandler.text; res.val = string.IsNullOrEmpty(txt) ? new JObject() : JObject.Parse(txt); } catch (Exception e) { res.err = e; }
            r.Dispose();
        }
        public static JObject FsStr(object v) => new JObject { ["stringValue"] = Convert.ToString(v, CultureInfo.InvariantCulture) };
        public static JObject FsInt(double v) => new JObject { ["integerValue"] = Round(IsFinite(v) ? v : 0).ToString("0", CultureInfo.InvariantCulture) };
        public static string Cloud_Iso(double t) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(t).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        public static JObject FsTime(double t) => new JObject { ["timestampValue"] = Cloud_Iso(t) };
        public static string RandId() { var b = Cloud_RandBytes(15); var sb = new StringBuilder(); foreach (var x in b) sb.Append("abcdefghijklmnopqrstuvwxyz0123456789"[x % 36]); return sb.ToString(); }

        // Sunucu saati: telefonun saati 2 dakikadan fazla saparsa oyun sunucu saatini kullanır (saat ileri alma hilesi)
        public static void ServerTime(double srv)
        {
            double off = srv - DateNow();
            if (Math.Abs(off) > 120000) { if (Math.Abs(off - TIME_OFF) > 60000) Track("clock_skew", new Dictionary<string, object> { { "off", Round(off / 1000) } }); TIME_OFF = off; }
            else TIME_OFF = 0;
        }

        public static CloudStatus CLOUD = new CloudStatus { status = "kapalı", last = 0 };

        // Çoklu cihaz: hangi kaydın daha çok oynandığı, hiç azalmayan istatistik sayaçlarının toplamıyla ölçülür
        // (son kaydedilme zamanı her açık cihazda 5 sn'de bir güncellendiği için ilerlemeyi göstermez).
        public static double ProgressOf(State s)
        {
            double n = 0;
            if (s != null && s.stats != null) foreach (var v in s.stats.Values) if (IsFinite(v)) n += v;
            return n + (s != null ? s.gen : 0) * 1e9;
        }

        public static void AdoptRemote(State remote, string msg)
        {
            S = Migrate(remote);
            try { LsSet(SAVE, ToJson(S)); } catch (Exception) { }
            View = null;   // regSig="": SyncRegions bölge kümesi değiştiyse zaten yeniden kurar
            if (Mode != "desk") ToHub();
            SyncWorld(true); RenderHUD(); BuildDesk(); RenderSheet(); UI.Toast(msg);
        }

        // Ham JSON (kayıt metni) ve ayrıştırılmış durum
        public class CloudSave { public string raw; public State state; }
        public static IEnumerator LoadRemote(CloudAccount a, CloudRes<CloudSave> res)
        {
            var d = new CloudRes<JObject>();
            yield return Fs("GET", "/saves/" + DocIdOf(a), null, d);
            if (d.err != null) { res.err = d.err; yield break; }
            var j = d.val?["fields"]?["json"]?["stringValue"];
            if (j == null) yield break;
            try { var raw = (string)j; JToken.Parse(raw); var st = FromJson<State>(raw); if (st != null) res.val = new CloudSave { raw = raw, state = st }; } catch (Exception) { }
        }

        // Eşitleme: buluta yazmadan önce buluttaki kayıt okunur. Bulutta (başka cihazdan) daha çok oynanmış bir kayıt varsa o alınır,
        // yoksa bu cihazın kaydı yazılır. Böylece eski durumdaki bir cihaz yeni ilerlemenin üzerine asla körlemesine yazmaz.
        // Eşitlemeler sıraya alınır (aynı anda iki istek birbirini ezmesin).
        static readonly Queue<KeyValuePair<bool, Action>> Cloud_syncQ = new Queue<KeyValuePair<bool, Action>>();
        static bool Cloud_syncBusy;
        public static void SyncCloud(bool pull, Action done = null)
        {
            if (!CLOUD_ON || S == null) { done?.Invoke(); return; }
            Cloud_syncQ.Enqueue(new KeyValuePair<bool, Action>(pull, done));
            if (!Cloud_syncBusy) { Cloud_syncBusy = true; Host.Run(Cloud_SyncWorker()); }
        }
        static IEnumerator Cloud_SyncWorker()
        {
            try
            {
                while (Cloud_syncQ.Count > 0)
                {
                    var job = Cloud_syncQ.Dequeue();
                    yield return SyncOnce(job.Key);
                    try { job.Value?.Invoke(); } catch (Exception) { }
                }
            }
            finally { Cloud_syncBusy = false; }
        }
        public static IEnumerator SyncOnce(bool pull)
        {
            var a = Account();
            var rem = new CloudRes<CloudSave>();
            yield return LoadRemote(a, rem);
            if (rem.err != null) { CLOUD.status = "bağlantı yok"; yield break; }
            var remote = rem.val != null ? rem.val.state : null;
            if (remote != null && ProgressOf(remote) > ProgressOf(S))
            {
                try { AdoptRemote(remote, pull ? "☁️ Buluttaki daha ileri kayıt yüklendi" : "☁️ Diğer cihazdaki ilerleme yüklendi"); } catch (Exception e) { Debug.LogException(e); CLOUD.status = "bağlantı yok"; yield break; }
                CLOUD.status = "eşitlendi"; CLOUD.last = DateNow(); yield break;
            }
            var body = new JObject { ["fields"] = new JObject { ["json"] = FsStr(ToJson(S)), ["level"] = FsInt(S.lv), ["updatedAt"] = FsTime(DateNow()) } };
            var w = new CloudRes<JObject>();
            yield return Fs("PATCH", "/saves/" + DocIdOf(a), body, w);
            if (w.err != null) { CLOUD.status = "bağlantı yok"; yield break; }
            CLOUD.status = "eşitlendi"; CLOUD.last = DateNow();
        }

        public static void RestoreAccount(string code)
        {
            var m = Regex.Match((code ?? "").Trim(), "^([0-9a-f-]{32,36})\\.([0-9a-f]{32})$", RegexOptions.IgnoreCase);
            if (!m.Success) { UI.Toast("Kod geçersiz"); return; }
            if (!CLOUD_ON) { UI.Toast("Bulut kaydı bu sürümde kapalı"); return; }
            Host.Run(Cloud_Restore(m.Groups[1].Value, m.Groups[2].Value));
        }
        static IEnumerator Cloud_Restore(string id, string secret)
        {
            var rem = new CloudRes<CloudSave>();
            yield return LoadRemote(new CloudAccount { id = id, secret = secret }, rem);
            if (rem.err != null) { UI.Toast("Bağlantı kurulamadı"); yield break; }
            if (rem.val == null) { UI.Toast("Bu koda ait kayıt bulunamadı"); yield break; }
            try
            {
                LsSet(ID_KEY, ToJson(new CloudAccount { id = id, secret = secret })); LsSet(SAVE, rem.val.raw);
                UI.Toast("Kayıt geri yüklendi, oyun yeniden açılıyor");
                SetTimeout(Reload, 900);   // location.reload()
            }
            catch (Exception) { UI.Toast("Bağlantı kurulamadı"); }
        }
        public static string RecoveryCode() { var a = Account(); return a.id + "." + a.secret; }

        // ---- Analitik (izne bağlı) ----
        public const string EVQ_KEY = "ci-events";
        public static List<CloudEv> EVQ = new List<CloudEv>();
        public static bool ConsentOk() => S != null && S.consent != null && S.consent.analytics;
        public static void Track(string name, object props = null)
        {
            if (!ConsentOk()) return;
            JToken p; try { p = props != null ? JToken.FromObject(props) : new JObject(); } catch (Exception) { p = new JObject(); }
            EVQ.Add(new CloudEv { n = name, p = p, t = DateNow(), lv = S != null ? S.lv : 0, pl = PLAT });
            if (EVQ.Count > 200) EVQ = EVQ.GetRange(EVQ.Count - 200, 200);
            try { LsSet(EVQ_KEY, ToJson(EVQ)); } catch (Exception) { }
        }
        public static string PLAYER = "";
        static JObject EvDoc(string col, JObject f) => new JObject { ["update"] = new JObject { ["name"] = "projects/" + CONFIG.firebaseProjectId + "/databases/(default)/documents/" + col + "/" + RandId(), ["fields"] = f } };
        static string Cloud_Slice(string s, int n) => s == null ? "" : s.Length > n ? s.Substring(0, n) : s;

        static bool Cloud_flushing;
        public static void FlushEvents()
        {
            if (!CLOUD_ON || !ConsentOk() || EVQ.Count == 0 || string.IsNullOrEmpty(PLAYER) || Cloud_flushing) return;
            Host.Run(Cloud_Flush());
        }
        static IEnumerator Cloud_Flush()
        {
            Cloud_flushing = true;
            var batch = EVQ.GetRange(0, Math.Min(100, EVQ.Count));
            var writes = new JArray();
            foreach (var e in batch)
                writes.Add(EvDoc("events", new JObject
                {
                    ["player"] = FsStr(PLAYER), ["name"] = FsStr(e.n), ["props"] = FsStr(Cloud_Slice((e.p ?? new JObject()).ToString(Formatting.None), 1000)),
                    ["level"] = FsInt(e.lv), ["platform"] = FsStr(e.pl), ["clientAt"] = FsTime(e.t), ["at"] = new JObject { ["timestampValue"] = Cloud_Iso(Now()) }
                }));
            var res = new CloudRes<JObject>();
            yield return Fs("POST", ":commit", new JObject { ["writes"] = writes }, res);
            Cloud_flushing = false;
            if (res.err != null) yield break;
            EVQ = EVQ.GetRange(batch.Count, EVQ.Count - batch.Count);
            try { LsSet(EVQ_KEY, ToJson(EVQ)); } catch (Exception) { }
        }

        // ---- Hata raporu ----
        static int errCount = 0;
        public static void ReportError(string msg, string stack)
        {
            if (!ConsentOk() || errCount++ > 20) return;
            var e = new JObject
            {
                ["m"] = Cloud_Slice(msg ?? "", 500), ["s"] = Cloud_Slice(stack ?? "", 2000), ["v"] = CONFIG.version, ["pl"] = PLAT,
                ["ua"] = Cloud_Slice(SystemInfo.operatingSystem + " · " + SystemInfo.deviceModel + " · Unity " + Application.unityVersion, 200), ["mode"] = Mode ?? ""
            };
            if (CLOUD_ON) Host.Run(Fs("POST", ":commit", new JObject { ["writes"] = new JArray { EvDoc("errors", new JObject { ["player"] = FsStr(!string.IsNullOrEmpty(PLAYER) ? PLAYER : "?"), ["error"] = FsStr(e.ToString(Formatting.None)), ["at"] = FsTime(Now()) }) } }, new CloudRes<JObject>()));
            else Debug.LogWarning("[hata] " + e.ToString(Formatting.None));
        }

        // ---- KVKK / izin ekranı (ilk açılış) ----
        public static void AskConsent()
        {
            if (S.consent != null) return;
            Toggle cAn = null; RectTransform d = null;
            d = UI.Modal(card =>
            {
                UI.Txt(card, "🌾", 34, null, false, TextAnchor.MiddleCenter);
                UI.Txt(card, "Hoş geldin!", 24, UI.C.ink, false, TextAnchor.MiddleCenter, true, true);
                UI.Txt(card, "Oyunu geliştirebilmek için <b>anonim</b> kullanım verisi (hangi bölümlerin oynandığı, hata raporları) toplamak istiyoruz. Adın, e-postan ya da konumun toplanmaz. " + (NATIVE ? "Ücretsiz sürümde isteğe bağlı ödüllü reklamlar gösterilir." : ""), 13, UI.C.text, false, TextAnchor.UpperLeft);
                cAn = Cloud_Switch(card, "Anonim kullanım verisi ve hata raporu", "İstediğin zaman Ayarlar'dan değiştirebilirsin.", true, null);
                Cloud_Links(card, null);
                UI.Btn(card, "Başlayalım", () =>
                {
                    S.consent = new Consent { analytics = cAn.isOn, at = DateNow() }; Save(); UI.Kill(d);
                    Track("first_open", new Dictionary<string, object> { { "w", RoundI(UI.W) }, { "h", RoundI(UI.H) }, { "gfx", GFX.mode } }); ShowLogin();
                }, "big gold");
            }, true, false, 380);
            d.name = "modal login";   // showLogin bu pencere açıkken giriş ödülünü göstermez ('.modal.login')
        }

        // .switch: kart içinde onay kutusu + kalın başlık + not
        static Toggle Cloud_Switch(Transform parent, string title, string note, bool on, Action<bool> change)
        {
            var box = UI.Box(parent, UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "switch");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>(); v.padding = UI.Pad(10, 12); v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return UI.Check(box, "<b>" + title + "</b>\n<size=12><color=#7d6c52>" + note + "</color></size>", on, change);
        }
        static void Cloud_Links(Transform parent, string tail)
        {
            var row = UI.HStack(parent, 4, TextAnchor.MiddleCenter, null, "note");
            UI.Btn(row, "Gizlilik Politikası", () => Application.OpenURL(CONFIG.privacyUrl), "ghost");
            UI.Txt(row, "·", 12.5f, UI.C.muted, true, TextAnchor.MiddleCenter, false);
            UI.Btn(row, "Kullanım Koşulları", () => Application.OpenURL(CONFIG.termsUrl), "ghost");
            if (tail != null) { var t = UI.Txt(parent, tail, 12.5f, UI.C.muted, true); }
        }

        // Ayarlar sayfasındaki hesap bölümü (accountHTML)
        public static void AccountHTML(Transform parent)
        {
            var uh = UI.HStack(parent, 8, TextAnchor.MiddleLeft, null, "uhead");
            var ht = UI.Txt(uh, "☁️ Hesap ve kayıt", 14, UI.Hex("#3a2414"), false, TextAnchor.MiddleLeft, false, true); UI.Size(ht, -1, -1, 1);
            UI.Txt(uh, CLOUD.status, 10.5f, UI.C.muted, true, TextAnchor.MiddleRight, false);
            if (CLOUD_ON)
            {
                var job = UI.Box(parent, UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "job");
                var h = job.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.padding = UI.Pad(8, 10); h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
                var pw = UI.VStack(job, 3, null, TextAnchor.UpperLeft, "pw"); UI.Size(pw, -1, -1, 1);
                UI.Txt(pw, "Kurtarma kodun", 13.5f, UI.C.text, true);
                var code = UI.Input(pw, RecoveryCode(), "", null); code.readOnly = true; code.textComponent.fontSize = 11; code.textComponent.font = UI.Font;
                UI.Txt(pw, "Bu kodu sakla. Yeni telefonda girince çiftliğin geri gelir.", 11.5f, UI.C.muted, true);
                UI.ActBtn(job, "📋", "copyCode", "soft sm");
                var urow = UI.HStack(parent, 7, TextAnchor.MiddleLeft, null, "urow");
                var inp = UI.Input(urow, "", "Kurtarma kodunu yapıştır", null); UI.Size(inp, -1, -1, 1);
                UI.Btn(urow, "Geri yükle", () => RestoreAccount(inp.text), "gold sm");
            }
            else UI.Txt(parent, "Bulut kaydı bu sürümde kapalı; ilerleme yalnızca bu cihazda saklanıyor.", 12.5f, UI.C.muted, true);
            Cloud_Switch(parent, "Anonim kullanım verisi", "Oyunu iyileştirmek için hangi bölümlerin oynandığı ve hata raporları.", ConsentOk(), on =>
            {
                S.consent = new Consent { analytics = on, at = DateNow() }; Save(); UI.Toast(on ? "Anonim veri paylaşımı açık" : "Anonim veri paylaşımı kapalı");
            });
            Cloud_Links(parent, "Destek: " + CONFIG.supportEmail + " · Sürüm " + CONFIG.version);
        }

        static void Init_35()
        {
            CLOUD.status = CLOUD_ON ? "bekliyor" : "kapalı";
            try { TOK = FromJson<CloudTok>(LsGet(TOK_KEY) ?? "null"); } catch (Exception) { TOK = null; }
            try { EVQ = FromJson<List<CloudEv>>(LsGet(EVQ_KEY) ?? "[]") ?? new List<CloudEv>(); } catch (Exception) { EVQ = new List<CloudEv>(); }
            try { PLAYER = DocIdOf(Account()).Substring(0, 16); } catch (Exception) { }
            // window 'error' / 'unhandledrejection' karşılığı: yakalanmamış istisnalar
            Application.logMessageReceived += (cond, stack, type) => { if (type == LogType.Exception) { try { ReportError(cond, stack); } catch (Exception) { } } };
            SetInterval(() => { FlushEvents(); if (CLOUD_ON && S != null && DateNow() - CLOUD.last > 60000) SyncCloud(false); }, 30000);
        }
    }
}
