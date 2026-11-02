// ================= YAPILANDIRMA (000-config.js) =================
// Yayından önce doldurulacak değerler. Boş bırakılan özellikler sessizce kapalı kalır.
using System.Collections.Generic;

namespace Ciftlik
{
    public class IapProductDef { public string id; public int gems; }

    public class ConfigDef
    {
        // Firebase (bulut kaydı, sunucu saati, analitik, hata raporu). Kurallar: firebase/firestore.rules
        // Firebase konsolu → Proje ayarları → Genel → "Web API Key" ve "Project ID". Bu değerler herkese açıktır, güvenliği kurallar sağlar.
        public string firebaseApiKey;
        public string firebaseProjectId;
        // AdMob ödüllü reklam birimleri. Şu anki değerler Google'ın TEST kimlikleri.
        public string admobRewardedAndroid;
        public string admobRewardedIos;
        public bool admobTesting;
        // Uygulama içi satın alma ürünleri (Play Console / App Store Connect'te aynı kimliklerle oluşturulmalı)
        // Satın alma doğrulama servisi (firebase/functions). Boşsa doğrulama telefonda yapılır: YAYINDA BOŞ BIRAKMA.
        public string iapValidatorUrl;     // örn. https://europe-west3-ciftlik-empire.cloudfunctions.net/validatePurchase
        public List<IapProductDef> iapProducts;
        // Yasal metinler
        public string privacyUrl;
        public string termsUrl;
        public string supportEmail;
        public string version;
    }

    public static partial class Game
    {
        static ConfigDef _config;
        public static ConfigDef CONFIG => _config ?? (_config = new ConfigDef
        {
            firebaseApiKey = "AIzaSyBOb93Y51J5R_8MMQH-lblgghYtMkQCouI",
            firebaseProjectId = "ciftlik-empire",
            admobRewardedAndroid = "ca-app-pub-3940256099942544/5224354917",
            admobRewardedIos = "ca-app-pub-3940256099942544/1712485313",
            admobTesting = true,
            iapValidatorUrl = "",
            iapProducts = new List<IapProductDef> {
                new IapProductDef { id = "yonca_30", gems = 30 },
                new IapProductDef { id = "yonca_90", gems = 90 },
                new IapProductDef { id = "yonca_250", gems = 250 } },
            privacyUrl = "https://github.com/Ceomai/farmer/blob/main/docs/gizlilik-politikasi.md",
            termsUrl = "https://github.com/Ceomai/farmer/blob/main/docs/kullanim-kosullari.md",
            supportEmail = "destek@ornek.com",
            version = "1.0.0",
        });
    }
}
