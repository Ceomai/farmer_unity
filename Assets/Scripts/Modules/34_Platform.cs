// ================= PLATFORM (Capacitor yerel köprüleri → Unity) (34-platform.js) =================
// Uygulama içinde çalışırken telefonun reklam, satın alma, bildirim, titreşim ve depolama özelliklerini kullanır.
// Web sürümündeki Capacitor eklentilerinin her biri burada bir C# arayüzüdür:
//   Preferences  → PrefsBackup (Application.persistentDataPath altında dosya; SDK gerekmez, gerçek uygulama)
//   Haptics      → Handheld.Vibrate (Unity'nin yerleşik titreşimi; şiddet ayarı yok)
//   App          → Game.OnVisibility (OnApplicationPause) + Android geri tuşu (Unity'de KeyCode.Escape)
//   AdMob        → IAdsProvider   (gerçek SDK: Google Mobile Ads Unity eklentisi, aşağıdaki AdsProvider ataması)
//   CdvPurchase  → IIapProvider   (gerçek SDK: Unity IAP / Google Play Billing, aşağıdaki IapProvider ataması)
//   LocalNotifications → INotifProvider (gerçek SDK: com.unity.mobile.notifications, aşağıdaki NotifProvider ataması)
// Projede reklam/satın alma/bildirim SDK'sı yoktur: editörde ve geliştirme derlemelerinde benzetim sağlayıcıları çalışır,
// yayın derlemesinde (SDK bağlanmadıysa) web sürümündeki gibi bu özellikler sessizce kapalı kalır.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ciftlik
{
    // ---------- Reklam (AdMob ödüllü video) ----------
    public interface IAdsProvider
    {
        // AdMob.initialize({initializeForTesting})
        void Initialize(bool testing, Action ok, Action<string> fail);
        // AdMob.requestConsentInfo() → (isConsentFormAvailable, status)
        void RequestConsentInfo(Action<bool, string> done, Action<string> fail);
        // AdMob.showConsentForm()
        void ShowConsentForm(Action done);
        // AdMob.prepareRewardVideoAd({adId,isTesting})
        void PrepareRewarded(string adId, bool testing, Action ok, Action<string> fail);
        // AdMob.showRewardVideoAd(): onReward = 'onRewardedVideoAdReward' olayı, closed = gösterim bitti (promise çözüldü)
        void ShowRewarded(Action onReward, Action closed, Action<string> fail);
    }

    // ---------- Uygulama içi satın alma (cordova-plugin-purchase) ----------
    public interface IIapProvider
    {
        // store.validator / store.applicationUsername
        void SetValidator(string url, Func<string> applicationUsername);
        // store.register([{id,type:CONSUMABLE,platform}])
        void Register(List<string> ids, bool apple);
        // store.when().productUpdated(p) → (id, yerelleştirilmiş fiyat)
        Action<string, string> ProductUpdated { get; set; }
        // approved → verify → verified → finish → finished(tx): sağlayıcı zinciri kendisi yürütür, bitince ürün kimlikleri bildirilir
        Action<List<string>> Finished { get; set; }
        // store.initialize([platform])
        void Initialize(Action ok, Action<string> fail);
        // store.get(id).getOffer() var mı
        bool HasOffer(string id);
        // offer.order() → (isError)
        void Order(string id, Action<bool> done);
    }

    // ---------- Yerel bildirimler ----------
    public class LocalNotif { public int id; public string title, body; public DateTime at; }
    public interface INotifProvider
    {
        void Schedule(List<LocalNotif> list, Action ok, Action<string> fail);
        void Cancel(int[] ids);
        void RequestPermissions(Action<bool> granted, Action<string> fail);
    }

    // ---------- Benzetim sağlayıcıları (editör / geliştirme derlemesi; SDK yokken test için) ----------
    public class SimulatedAds : IAdsProvider
    {
        bool _ready;
        public void Initialize(bool testing, Action ok, Action<string> fail) { ok(); }
        public void RequestConsentInfo(Action<bool, string> done, Action<string> fail) { done(false, "NOT_REQUIRED"); }
        public void ShowConsentForm(Action done) { done(); }
        public void PrepareRewarded(string adId, bool testing, Action ok, Action<string> fail) { _ready = true; Game.SetTimeout(ok, 200); }
        public void ShowRewarded(Action onReward, Action closed, Action<string> fail)
        {
            if (!_ready) { fail("not ready"); return; }
            _ready = false;
            // Ekranı kaplayan sahte reklam: 2 sn sonra ödül verilir ve kapanır
            RectTransform m = null;
            m = UI.Modal(card =>
            {
                UI.Txt(card, "📺", 40, null, false, TextAnchor.MiddleCenter);
                UI.Txt(card, "Test reklamı (benzetim)", 16, UI.C.ink, true, TextAnchor.MiddleCenter);
            }, true, false, 300);
            Game.SetTimeout(() => { UI.Kill(m); onReward(); closed(); }, 2000);
        }
    }

    public class SimulatedIap : IIapProvider
    {
        readonly List<string> _ids = new List<string>();
        public Action<string, string> ProductUpdated { get; set; }
        public Action<List<string>> Finished { get; set; }
        public void SetValidator(string url, Func<string> u) { }
        public void Register(List<string> ids, bool apple) { _ids.Clear(); _ids.AddRange(ids); }
        public void Initialize(Action ok, Action<string> fail)
        {
            ok();
            foreach (var id in _ids)
            {
                var c = Game.CONFIG.iapProducts.Find(x => x.id == id);
                ProductUpdated?.Invoke(id, c != null ? "₺" + (c.gems * 2) + ",99 (test)" : "test");
            }
        }
        public bool HasOffer(string id) => _ids.Contains(id);
        public void Order(string id, Action<bool> done)
        {
            // Gerçek ödeme yok: onaylı ve doğrulanmış sayılır
            Game.SetTimeout(() => { done(false); Finished?.Invoke(new List<string> { id }); }, 600);
        }
    }

    public class SimulatedNotif : INotifProvider
    {
        public void Schedule(List<LocalNotif> list, Action ok, Action<string> fail) { foreach (var n in list) Debug.Log("[bildirim " + n.id + "] " + n.at.ToString("u") + " · " + n.title + ": " + n.body); ok(); }
        public void Cancel(int[] ids) { Debug.Log("[bildirim iptal] " + string.Join(",", ids)); }
        public void RequestPermissions(Action<bool> granted, Action<string> fail) { granted(true); }
    }

    public class PlatformBridge
    {
        public bool ads = false, iap = false, adReady = false;
        public Dictionary<string, string> prices = new Dictionary<string, string>();

        // Sağlayıcılar. Gerçek SDK eklendiğinde buraya o SDK'yı saran sınıf atanır:
        //   AdsProvider   = new AdMobProvider();      // GoogleMobileAds.Api.RewardedAd.Load/Show + UMP ConsentInformation
        //   IapProvider   = new UnityIapProvider();   // UnityEngine.Purchasing: IStoreListener.ProcessPurchase → doğrulama → ConfirmPendingPurchase
        //   NotifProvider = new AndroidNotifProvider(); // Unity.Notifications.Android.AndroidNotificationCenter.SendNotificationWithExplicitID
        public IAdsProvider AdsProvider;
        public IIapProvider IapProvider;
        public INotifProvider NotifProvider;

        static bool Simulate => Application.isEditor || Debug.isDebugBuild;

        public void Init(Action done)
        {
            if (Simulate)
            {
                if (AdsProvider == null) AdsProvider = new SimulatedAds();
                if (IapProvider == null) IapProvider = new SimulatedIap();
                if (NotifProvider == null) NotifProvider = new SimulatedNotif();
            }
            if (!Game.NATIVE && !Simulate) { done?.Invoke(); return; }
            // StatusBar.setOverlaysWebView({overlay:true}) ve SplashScreen.hide(): Unity'de durum çubuğu ve açılış ekranı
            // Player Settings ile yönetilir (Android: "Render outside safe area", Splash Screen); çalışma anında yapılacak iş yok.
            // Kalıcı yedek: localStorage (PlayerPrefs) boşsa yedek dosyasından geri yükle
            try
            {
                if (Game.LsGet(Game.SAVE) == null)
                {
                    var v = PrefsGet(Game.SAVE);
                    if (!string.IsNullOrEmpty(v)) Game.LsSet(Game.SAVE, v);
                }
            }
            catch (Exception) { }
            InitAds(); InitIap(); InitApp();
            done?.Invoke();
        }

        // ---- Preferences (kalıcı yedek) ----
        static string PrefsPath(string key) => Path.Combine(Application.persistentDataPath, "pref_" + key + ".json");
        static string PrefsGet(string key) { try { var p = PrefsPath(key); return File.Exists(p) ? File.ReadAllText(p) : null; } catch (Exception) { return null; } }
        public void Backup(string json)
        {
            if (!Game.NATIVE) return;
            try { var p = PrefsPath(Game.SAVE); File.WriteAllText(p + ".tmp", json); if (File.Exists(p)) File.Delete(p); File.Move(p + ".tmp", p); } catch (Exception) { }
        }

        // ---- Titreşim ----
        public void Hap(string kind = "light")
        {
            if (Game.S == null || !Game.S.sound.sfx) return;
            // Capacitor Haptics.impact({style}) / navigator.vibrate(heavy?[30,40,30]:12) → Unity yalnızca tek tip titreşim sunar
            if (Application.isMobilePlatform) { try { Handheld.Vibrate(); } catch (Exception) { } }
        }

        // ---- AdMob ödüllü reklam ----
        public void InitAds()
        {
            var A = AdsProvider; if (A == null) return;
            A.Initialize(Game.CONFIG.admobTesting, () =>
            {
                A.RequestConsentInfo((available, status) =>
                {
                    if (available && status == "REQUIRED") A.ShowConsentForm(() => { ads = true; PrepAd(null); });
                    else { ads = true; PrepAd(null); }
                }, err => { ads = true; PrepAd(null); });
            }, e => Game.Track("ads_init_fail", new Dictionary<string, object> { { "m", e } }));
        }
        public void PrepAd(Action then)
        {
            var A = AdsProvider; if (A == null) { then?.Invoke(); return; }
            A.PrepareRewarded(Game.PLAT == "ios" ? Game.CONFIG.admobRewardedIos : Game.CONFIG.admobRewardedAndroid, Game.CONFIG.admobTesting,
                () => { adReady = true; then?.Invoke(); }, e => { adReady = false; then?.Invoke(); });
        }
        public bool ShowAd(Action cb)
        {
            var A = AdsProvider; if (A == null || !ads) return false;
            bool got = false, finished = false;
            Action done = () =>
            {
                if (finished) return; finished = true;
                adReady = false; PrepAd(null);
                if (got) { Game.S.ads.n++; cb(); Game.Save(); Game.Track("ad_reward"); } else UI.Toast("Reklam tamamlanmadı");
            };
            Action show = () => A.ShowRewarded(() => { got = true; }, done, e => { UI.Toast("Şu an reklam yok, birazdan tekrar dene"); done(); });
            if (adReady) show(); else PrepAd(show);
            return true;
        }

        // ---- Uygulama içi satın alma ----
        public void InitIap()
        {
            var st = IapProvider; if (st == null) return;
            bool apple = Game.PLAT == "ios";
            if (!string.IsNullOrEmpty(Game.CONFIG.iapValidatorUrl)) st.SetValidator(Game.CONFIG.iapValidatorUrl, () => !string.IsNullOrEmpty(Game.PLAYER) ? Game.PLAYER : Game.Account().id.Substring(0, Math.Min(16, Game.Account().id.Length)));
            st.Register(Game.CONFIG.iapProducts.ConvertAll(p => p.id), apple);
            st.ProductUpdated = (id, price) => { if (price != null) prices[id] = price; if (Game.View != null && Game.View.k == "shop") Game.RenderSheet(); };
            st.Finished = ids =>
            {
                foreach (var id in ids)
                {
                    var c = Game.CONFIG.iapProducts.Find(x => x.id == id);
                    if (c != null) { Game.S.gems += c.gems; Game.Stat("iap"); Game.Track("iap", new Dictionary<string, object> { { "id", c.id } }); UI.Toast("+" + c.gems + " 🍀 Teşekkürler!"); Game.Sfx("win"); Game.Commit(); }
                }
            };
            st.Initialize(() => { iap = true; }, e => Game.Track("iap_init_fail", new Dictionary<string, object> { { "m", e } }));
        }
        public void Buy(string id)
        {
            var st = IapProvider;
            if (st == null || !iap) { UI.Toast("Satın alma şu an kullanılamıyor"); return; }
            if (!st.HasOffer(id)) { UI.Toast("Ürün bulunamadı"); return; }
            st.Order(id, isError => { if (isError) UI.Toast("Satın alma tamamlanmadı"); });
        }

        // ---- Uygulama yaşam döngüsü, geri tuşu, bildirimler ----
        bool _appInit;
        public void InitApp()
        {
            if (_appInit || !Game.NATIVE) return; _appInit = true;
            Game.OnVisibility += hidden =>
            {
                if (hidden) { Game.Save(); Game.FlushEvents(); ScheduleReady(); }
                else { CancelReady(); Game.SyncCloud(false); }
            };
            // Android geri tuşu Unity'de KeyCode.Escape olarak gelir
            Game.OnFrame += dt => { if (Input.GetKeyDown(KeyCode.Escape)) BackButton(); };
        }
        void BackButton()
        {
            if (UI.AnyModal) { UnityEngine.Object.Destroy(UI.ModalLayer.GetChild(0).gameObject); return; }
            if (Game.Int_BodyHas("photo")) { Game.PhotoMode(false); return; }
            if (Game.Mode == "interior") { Game.LeaveInterior(); return; }
            if (Game.Mode == "focus") { Game.ToHub(); return; }
            if (Game.View != null) { Game.View = null; Game.RenderSheet(); return; }
            if (Game.Mode == "hub") { Game.ToDesk(); return; }
            MinimizeApp();
        }
        static void MinimizeApp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var act = up.GetStatic<AndroidJavaObject>("currentActivity")) act.Call<bool>("moveTaskToBack", true);
            }
            catch (Exception) { }
#endif
        }
        public void ScheduleReady()
        {
            var N = NotifProvider; if (N == null || !Game.S.sound.notif) return;
            var ends = new List<KeyValuePair<double, string>>();
            foreach (var f in Game.S.fields) if (f.job != null && f.job.e > Game.Now()) ends.Add(new KeyValuePair<double, string>(f.job.e, "Ekinlerin hasada hazır 🌾"));
            foreach (var kv in Game.S.units) { var s = kv.Value; if (s.job != null && s.job.e > Game.Now()) ends.Add(new KeyValuePair<double, string>(s.job.e, Game.B[kv.Key].n + " hazır! " + Game.B[kv.Key].e)); }
            if (ends.Count == 0) return;
            ends.Sort((a, b) => a.Key.CompareTo(b.Key));
            var list = new List<KeyValuePair<double, string>> { ends[0] }; var last = ends[ends.Count - 1];
            if (last.Key - ends[0].Key > 600000) list.Add(new KeyValuePair<double, string>(last.Key, "Bütün üretimler tamamlandı, çiftlik seni bekliyor!"));
            var notifs = new List<LocalNotif>();
            for (int i = 0; i < list.Count; i++) notifs.Add(new LocalNotif { id = 100 + i, title = "Çiftlik İmparatorluğu", body = list[i].Value, at = Game.LocalDate(list[i].Key - Game.TIME_OFF) });
            try { N.Schedule(notifs, () => { }, e => { }); } catch (Exception) { }
        }
        // Anında yerel bildirim (web: new Notification(title,{body}))
        public void LocalNotify(string title, string body)
        {
            var N = NotifProvider; if (N == null) return;
            try { N.Schedule(new List<LocalNotif> { new LocalNotif { id = 200, title = title, body = body, at = DateTime.Now } }, () => { }, e => { }); } catch (Exception) { }
        }
        public void CancelReady() { var N = NotifProvider; if (N != null) try { N.Cancel(new[] { 100, 101 }); } catch (Exception) { } }
        // Sonuç: null (bildirim yok), true/false (izin)
        public void AskNotif(Action<bool?> cb)
        {
            var N = NotifProvider; if (N == null) { cb(null); return; }
            try { N.RequestPermissions(g => cb(g), e => cb(false)); } catch (Exception) { cb(false); }
        }

        // JS adlarıyla da çağrılabilsin (PLATFORM.hap() vb.)
        public void init(Action done) => Init(done);
        public void backup(string json) => Backup(json);
        public void hap(string kind = "light") => Hap(kind);
        public bool showAd(Action cb) => ShowAd(cb);
        public void buy(string id) => Buy(id);
        public void askNotif(Action<bool?> cb) => AskNotif(cb);
        public void scheduleReady() => ScheduleReady();
        public void cancelReady() => CancelReady();
    }

    public static partial class Game
    {
        // Capacitor.isNativePlatform() → telefonda çalışan derleme
        public static bool NATIVE => Application.isMobilePlatform && !Application.isEditor;
        public static string PLAT => !NATIVE ? "web" : Application.platform == RuntimePlatform.IPhonePlayer ? "ios" : "android";
        public static PlatformBridge PLATFORM = new PlatformBridge();
    }
}
