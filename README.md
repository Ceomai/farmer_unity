# Çiftlik İmparatorluğu — Unity sürümü

[Ceomai/farmer](https://github.com/Ceomai/farmer) web oyununun (JS + Three.js) Unity 6 LTS ile baştan yazılmış hâli. Oyun kuralları, sayılar, Türkçe metinler, ekranlar, 3D modeller ve kayıt biçimi web sürümüyle aynıdır; web kaydı ya da bulut kaydı bu sürümde açılabilir.

## Hemen oyna

| 🌐 Web'de oyna | 📱 Android APK (test sürümü) |
|:---:|:---:|
| <img src="docs/qr/web-qr.png" width="200" alt="Web QR"> | <img src="docs/qr/apk-qr.png" width="200" alt="APK QR"> |
| **[ceomai.github.io/farmer_unity](https://ceomai.github.io/farmer_unity/)** | **[ciftlik-imparatorlugu.apk](https://github.com/Ceomai/farmer_unity/releases/download/test-surum/ciftlik-imparatorlugu.apk)** |

Bağlantılar sabittir: `main` dalına her gönderimde GitHub Actions (`.github/workflows/build.yml`, GameCI) projeyi Unity ile derler, web sürümünü GitHub Pages'e ve APK'yı `test-surum` sürümüne yükler.

Derlemenin çalışması için repo ayarlarında (Settings → Secrets and variables → Actions) üç gizli değer gerekir:

| Ad | Değer |
|---|---|
| `UNITY_EMAIL` | Unity hesabının e-postası |
| `UNITY_PASSWORD` | Unity hesabının şifresi |
| `UNITY_LICENSE` | Ücretsiz Personal lisans dosyasının içeriği: bir bilgisayarda Unity Hub'a giriş yapıp lisansı etkinleştirdikten sonra `C:\ProgramData\Unity\Unity_lic.ulf` dosyasındaki metnin tamamı |

## Açma

1. Unity Hub → **Add project from disk** → bu klasör.
2. Unity **6000.0 LTS** (ProjectVersion.txt'teki sürüm ya da daha yeni bir 6000.x) ile aç.
3. `Assets/Scenes/Main.unity` sahnesini aç ve **Play**. Sahne boştur: oyun `GameHost.Bootstrap` ile tamamen koddan kurulur (kamera, ışıklar, arayüz, dünya).

Android: File → Build Profiles → Android → Switch Platform → Build. Paket adı `com.ceomai.ciftlik`, dikey ekran.

## Yapı

```
Assets/
  Scenes/Main.unity          Boş başlangıç sahnesi
  Resources/Shaders/         Toon, Outline, Unlit, Sky, Text3D (Built-in RP)
  Scripts/
    Core/                    Ortak çekirdek: Game (küresel kapsam), State (kayıt biçimi), UI kiti, Paint, Three.js dönüşümleri, GameHost
    Modules/NN_Ad.cs         Web sürümündeki src/js/NN-ad.js modülünün birebir karşılığı (aynı sıra: Game.ORDER)
    Engine/                  Mesh üreticileri (Three.js geometrileri), modeller, ortam
    UI/                      HUD, game.css teması, düzen bileşenleri, Dom (querySelector karşılığı)
    G2/                      Sistem panellerinin ortak parçaları
Tools/
  PORTING.md                 Taşıma kuralları ve adlandırma
  check.sh, stubs/           Unity kurulu olmayan makinede C# tür denetimi (Unity projesinin parçası değil)
  expect/                    Modüller arası bağımlılık listeleri
```

Mimari notu: Web sürümündeki tüm modüller tek küresel kapsamı paylaşır. Unity sürümünde bu kapsam `Ciftlik.Game` adlı statik partial sınıftır; JS fonksiyon adları PascalCase'e çevrilmiştir (`addXP` → `AddXP`). Her modülün yüklenirken çalışan kodu `Init_NN` yöntemindedir.

## Yayından önce

- `Modules/000_Config.cs`: Firebase anahtarları, AdMob kimlikleri, satın alma ürünleri, yasal bağlantılar (web sürümüyle aynı alanlar).
- Reklam, uygulama içi satın alma ve yerel bildirimler `34_Platform.cs` içindeki arayüzlerin arkasındadır (`IAdsProvider`, `IIapProvider`, `INotifProvider`). Editörde ve geliştirme derlemesinde benzetimle çalışırlar; yayın için Google Mobile Ads, Unity IAP ve Mobile Notifications paketleri eklenip yorumlarda belirtilen yerlere bağlanmalıdır.
- Yazı tipleri: `Assets/Resources/Fonts/Nunito.ttf` ve `AlfaSlabOne.ttf` eklenirse kullanılır (Google Fonts, OFL); yoksa sistem yazı tipleri kullanılır.

## Doğrulama durumu

Bu proje Unity Editor kurulu olmayan bir makinede yazıldı. C# kodu `bash Tools/check.sh` ile UnityEngine taslaklarına karşı hatasız derlenir; ancak gölgelendiriciler, sahne, oyun içi görünüm ve çalışma zamanı davranışı henüz Unity'de çalıştırılarak denenmedi.
