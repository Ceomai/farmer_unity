# Çiftlik İmparatorluğu → Unity taşıma kuralları

Kaynak: `C:\Users\PC\Projects\farmer` (web sürümü, DEĞİŞTİRME). Hedef: `C:\Users\PC\Projects\farmer_unity` (Unity 6 LTS, Built-in render pipeline, C# 9).
Amaç: oyunun tüm mekaniklerini, sayılarını, metinlerini (Türkçe, birebir), ekranlarını ve davranışlarını AYNEN taşımak. Özetleme yok, "TODO" bırakma yok; her fonksiyon gerçekten çalışır biçimde yazılır.

## Mimari (çekirdek hazır: `Assets/Scripts/Core/`, OKU ama genel imzalarını değiştirme)
- Web sürümünde tüm modüller tek küresel kapsamı paylaşır. Unity'de bu kapsam `namespace Ciftlik { public static partial class Game { … } }`.
- Her JS modülü `src/js/NN-ad.js` → `Assets/Scripts/Modules/NN_Ad.cs` (ör. `06-engine.js` → `06_Engine.cs`; dosya adı rakamla başlayabilir). İçinde `public static partial class Game`.
  Yardımcı MonoBehaviour/sınıflar aynı dosyada ya da `Assets/Scripts/<Grup>/` altında olabilir.
- **Adlandırma:** JS küresel adı → PascalCase (`addXP`→`AddXP`, `sheetBarn`→`SheetBarn`, `fresh`→`Fresh`, `S` aynı). BÜYÜK HARFLİ sabitler aynen (`ITEMS`, `CROPS`, `B`, `LOC`, `MAXL`…). Kendi uydurduğun yardımcıların adı modül ön ekiyle başlasın (`Eng_Hash`, `Desk_DrawMap`…) ki partial sınıfta çakışmasın. JS'te zaten küresel olan bir adı yalnızca SAHİBİ modül tanımlar.
- **Üst düzey kod:** JS dosyasının yüklenirken çalışan kodu `static void Init_NN()` içine (ör. `Init_06`, `Init_000`). `Game.BootModules` bunları `src/js/ORDER` sırasıyla çağırır (33-loop en son).
- **Durum:** `Game.S` (`State.cs`). Alan adları JS kayıt anahtarlarıyla aynı (JSON uyumu). Eksik alan gerekiyorsa ilgili sınıfa (hepsi `partial`) KENDİ dosyanda `public partial class State { public X yeniAlan; }` diye ekle; State.cs'yi düzenleme.
- **Zaman:** `Now()` (ms, double, `TIME_OFF` dahil), `DateNow()` (Date.now), `PerfNow()`. Süreler ms olarak double.
- **Sayılar:** JS `Math.round` → `Round()/RoundI()` (C# Math.Round KULLANMA), `Math.random` → `Rnd()`. Para/xp/itibar `double`; seviye, adet, yonca (gems) `int`. `Fmt(n)` = toLocaleString('tr-TR').
- **Sözlükler:** `Get(dict,k)` (yoksa 0), `Inc(dict,k,n)`. JS nesne-sözlüğü → `Dictionary<string,T>` (ekleme sırası korunur), JS dizisi → `List<T>` (her zaman List, `.Count`).
- **Veri tanımları:** nesneler için sınıf, alan adları JS anahtarlarıyla aynı (küçük harf). C# anahtar sözcükleri `@in`, `@out` gibi yazılır. 01-data şu sınıfları tanımlar (diğer modüller bunlara güvenebilir):
  - `ItemDef { string n, e; double p; double buy; }` → `Dictionary<string,ItemDef> ITEMS`
  - `Recipe { string id, n; double cost; Dictionary<string,int> @in; double t; Dictionary<string,int> @out; int lvl; string col; List<int> season; }` → `List<Recipe> CROPS`
  - `BuildingDef { string n, e, d; double cost; int lvl; List<Recipe> recipes; int storage; bool field; }` → `Dictionary<string,BuildingDef> B`
  - `HouseDef { string n, e; double cost; int lvl; }` → `List<HouseDef> HOUSE`
  - `RegionDef { string n; double cost; int lvl; float[] rect; }` → `Dictionary<string,RegionDef> REG`
  - `LocDef { string n, reg, kind, e; float[] p; float r, h; List<string> units; List<float[]> slots; float[] s; }` → `Dictionary<string,LocDef> LOC`
  - `Job { string r; double s, e; }` (State.cs'de)
  Başka modüllerin sabitleri için de aynı kural: `XXX` nesne-sözlüğü ise `Dictionary<string, XxxDef>`, dizi ise `List<XxxDef>`; alanlar JS adlarıyla.
- **HTML → uGUI:** JS'te HTML dizgisi döndüren her fonksiyon C#'ta **ilk parametresi `Transform parent` olan `void`** yöntemdir ve içeriği oraya kurar (`chips(obj,check)` → `Chips(Transform parent, Dictionary<string,int> obj, bool check, string cls="")`, `sheetBarn()` → `SheetBarn(Transform parent)`).
  Arayüz kiti: `UI.cs` (`UI.VStack/HStack/Grid/Card/Box/Txt/Title/Btn/ActBtn/Chip/Bar/Check/Range/Input/Scroll/Modal/Toast/Float/Tween/Boing/Clickable/OnLongPress`, renkler `UI.C.*` = game.css değişkenleri). Birimler CSS pikseli; game.css'teki ölçü ve renkleri kullan. Zengin metin (`<b>`, `<color=#..>`) açık. Kullanıcı metnini `UI.Esc()` ile bas.
- **data-act olayları:** `data-act="x" data-k="y"` → `UI.ActBtn(p,"etiket","x","", "k","y")` ya da `Game.Act("x", D("k","y"))`. Her modül KENDİ eylemlerini `Init_NN` içinde `RegisterAct("x", d => …)` ile kaydeder (`Arg(d,"k")`, `ArgI`, `ArgD`). 13-events'teki büyük switch'in her case'i orada kaydedilir.
- **Zamanlayıcı/döngü:** `SetTimeout(a,ms)`, `SetInterval(a,ms)`, `ClearTimer(id)`, kare olayı `Game.OnFrame += dt => …`, görünürlük `Game.Hidden` / `Game.OnVisibility`. Coroutine: `Game.Host.Run(...)`.
- **localStorage:** `LsGet/LsSet/LsRemove`. JSON: `ToJson/FromJson<T>` (Newtonsoft, `JsonSettings`).
- **3D:** Built-in RP. Three.js koordinatı → `V(x,y,z)` (z ters), dönüş `ThreeRot(x,y,z)` (radyan, XYZ), `SetPos/SetRot/SetRotY`, renk `Hex3(0xRRGGBB)`. Gölgelendiriciler `Assets/Resources/Shaders/*.shader` (`Shader.Find("Ciftlik/…")`). Canvas dokusu → `Paint` (şekil), 3D yazı → `Text3D(...)` (TextMesh, `Ciftlik/Text3D`). Fizik yok; seçim için `Collider` + `Physics.Raycast` ya da kendi ışın testi.
- **Ses:** WebAudio sentezi → `AudioClip.Create` ile PCM üretimi (14-audio).
- **Platform eklentileri (Capacitor):** AdMob, satın alma, bildirim, titreşim, paylaşım → C# arayüzü + Unity karşılığı (`Handheld.Vibrate`, `Application.OpenURL`…); gerçek AdMob/IAP SDK'sı projede yok: arayüz arkasında, editörde/SDK yokken benzetimle çalışan uygulama yaz ve SDK'nın nereye bağlanacağını yorumla belirt.

## Derleme denetimi (Unity kurulu değil)
`bash Tools/check.sh Assets/Scripts/Modules/06_Engine.cs …` → Çekirdek + verdiğin dosyalar Unity taslaklarına (Tools/stubs) karşı derlenir.
Başka gruplara ait henüz yazılmamış adlardan gelen `CS0103/CS0117/CS1061` hatalarını yok say; kendi kodundaki diğer TÜM hataları düzelt.
Taslakta olmayan GERÇEK bir Unity API'si kullandıysan `Tools/stubs/extra_<grup>.cs` dosyasına partial olarak ekle (uydurma API ekleme).
C# 9 ile sınırlı kal (file-scoped namespace, record, init, `new()` hedef türlü ifade YOK).

## Başka gruplardan beklediklerin
Başka bir modülün fonksiyonunu çağırdıysan, `Tools/expect/<grup>.txt` dosyasına bir satır yaz: `AdımYöntem(param türleri) -> dönüş   // hangi JS fonksiyonu`. Entegrasyonda imzalar bununla eşleştirilecek.

## 3D tür eşlemesi
THREE.Group/Object3D/Mesh → `Transform` (dönüş türü olarak; GameObject'e `.gameObject` ile erişilir). THREE.Material → `Material`. THREE.Texture/CanvasTexture → `Texture2D`. THREE.Color/hex sayı → `Color` (`Hex3`). THREE.Vector3 → `Vector3` (Three koordinatında tutuluyorsa adında belirt). Sahne kökleri: `Game.SCENE` (dünya), iç mekân sahneleri ayrı kök Transform'lar.
userData → küçük bir `MonoBehaviour` (ör. `class UData : MonoBehaviour { public string kind, loc, unit; public int plot; … }`) — sahibi 06-engine; ihtiyaç duyulan alanı kendi dosyanda partial ekle (`public partial class UData`).

## Gruplar (paralel çalışır; yalnızca kendi dosyalarını yaz)
- G1 çekirdek: 000-config, 00-boot, 01-data, 02-state, 03-helpers, 04-economy, 05-farm-actions, 16-time-weather, 17-collection
- G2 sistemler: 18-workers, 19-contracts, 20-fair, 21-horse-league, 22-minigames, 23-shop, 24-daily (Migrate dahil), 26-animal-love, 27-friends
- G3 motor: 06-engine (+ gölgelendiriciler, mesh üreticileri)
- G4 dünya: 07-world, 08-markers, 09-interiors, 10-camera, 11-input, 15-fly-items, 29-celebrate, 30-window-react, 31-photo, 32-thumbnails
- G5 masa/platform: 25-desk, 33-loop, 34-platform, 35-cloud, 14-audio
- G6 arayüz: index.template.html (HUD, dok, kamera düğmeleri, geri/rehber düğmesi, şerit kapları, iris), game.css teması, 12-panels, 13-events, 36-strip, 38-batch
- G7 içerik: 28-farmer-talk, 37-tutorial, 39-guide, 40-fun, 41-content
