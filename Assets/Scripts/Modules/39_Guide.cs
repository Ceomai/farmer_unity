// ================= REHBER (KILAVUZ) (39-guide.js) =================
// Masa ve harita ekranında soldaki "📖 Rehber" düğmesiyle her zaman açılır.
// Ürün / bina / bölge bilgileri doğrudan oyun verisinden (ITEMS, CROPS, B, LOC, REG...) üretilir; oyun değişince rehber de kendiliğinden güncel kalır.
// Girdi gövdeleri web sürümündeki HTML parçalarıyla birebir yazılır; Guide_Rich bunları Unity zengin metnine çevirir.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // <details class="gd" data-q="..."> karşılığı
    public class GuideEntry : MonoBehaviour
    {
        public string q, sum; public int idx; public GameObject body; public Text arrow; public bool open;
        public void SetOpen(bool on) { open = on; if (body) body.SetActive(on); if (arrow) arrow.text = on ? "▴" : "▾"; }
    }

    public static partial class Game
    {
        public static readonly List<string[]> GTABS = new List<string[]> {
            new[] { "start", "🧭 Başla" }, new[] { "res", "💰 Kaynak" }, new[] { "items", "🧺 Ürün" }, new[] { "places", "🏗️ Bina" }, new[] { "events", "🎪 Etkinlik" }, new[] { "tips", "💡 İpucu" } };
        public static string GEsc(string s) => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        public static string GIn(Recipe r) { var s = string.Join(" + ", (r.@in ?? new Dictionary<string, int>()).Select(e => $"{ITEMS[e.Key].e}{e.Value} {ITEMS[e.Key].n}")); return s != "" ? s : "girdi yok"; }
        public static string GOut(Recipe r) => string.Join(", ", r.@out.Select(e => $"{ITEMS[e.Key].e}{e.Value} {ITEMS[e.Key].n}"));
        public static string GLv(int l) => $"<span class=\"gtag{(S.lv >= l ? " ok" : "")}\">Sv {l}</span>";

        // toLocaleLowerCase('tr')
        static string Guide_Lower(string s) => (s ?? "").Replace('I', 'ı').Replace('İ', 'i').ToLowerInvariant();
        static string Guide_Strip(string s) => Regex.Replace(s ?? "", "<[^>]+>", " ");

        // HTML parçası → Unity zengin metni (<p>, <p class="note">, <ul>/<ol>/<li>, <br>, <b>, <em>, <small>, <span class="gtag">)
        static readonly Regex Guide_Tok = new Regex("<[^>]+>|[^<]+", RegexOptions.Compiled);
        internal static string Guide_Rich(string html)
        {
            var sb = new StringBuilder(); var lists = new Stack<int>(); var closers = new Stack<string>();
            Action nl = () => { if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append('\n'); };
            foreach (Match m in Guide_Tok.Matches(html ?? ""))
            {
                string t = m.Value;
                if (t[0] != '<') { sb.Append(Regex.Replace(t, "\\s+", " ").Replace("&amp;", "&").Replace("&lt;", "‹").Replace("&gt;", "›").Replace("&quot;", "\"")); continue; }
                string tl = t.ToLowerInvariant();
                if (tl.StartsWith("<p")) { nl(); if (tl.Contains("note")) { sb.Append("<size=12><color=#7d6c52>"); closers.Push("</color></size>"); } else closers.Push(""); }
                else if (tl == "</p>") { if (closers.Count > 0) sb.Append(closers.Pop()); nl(); }
                else if (tl == "<ul>") { nl(); lists.Push(-1); }
                else if (tl == "<ol>") { nl(); lists.Push(0); }
                else if (tl == "</ul>" || tl == "</ol>") { if (lists.Count > 0) lists.Pop(); nl(); }
                else if (tl == "<li>") { nl(); if (lists.Count > 0 && lists.Peek() >= 0) { int k = lists.Pop() + 1; lists.Push(k); sb.Append("\u0001" + k + ". "); } else sb.Append("\u0001• "); }
                else if (tl == "</li>") { }
                else if (tl.StartsWith("<br")) sb.Append('\n');
                else if (tl == "<small>") sb.Append(" <size=11><color=#7d6c52>");
                else if (tl == "</small>") sb.Append("</color></size>");
                else if (tl.StartsWith("<span") && tl.Contains("gtag")) { bool ok = tl.Contains(" ok"); sb.Append(ok ? " <size=10><b><color=#4b8f3a>" : " <size=10><b><color=#8f7d62>"); closers.Push("</color></b></size>"); }
                else if (tl == "</span>") { if (closers.Count > 0) sb.Append(closers.Pop()); }
                else if (tl == "<b>" || tl == "</b>" || tl == "<i>" || tl == "</i>" || tl.StartsWith("<size") || tl.StartsWith("</size") || tl.StartsWith("<color") || tl.StartsWith("</color")) sb.Append(t);
                // <em>, </em> ve diğerleri: yalnızca biçim, atla
            }
            var s = Regex.Replace(sb.ToString(), "[ ]*\n[ ]*", "\n").Replace("\u0001", "  ");
            return s.Trim('\n', ' ');
        }

        // Bir girdi kartı: başlık + içerik; arama bu metin üstünde yapılır
        static int Guide_Idx;
        public static void GEntry(Transform parent, string title, string body, bool open = false)
        {
            string q = Guide_Lower(Guide_Strip(title + " " + body));
            // .gd{border:2px ink;border-radius:13px;background:card;box-shadow:2px 2px 0 rgba(42,26,16,.85)}
            var card = UI.Box(parent, UI.C.card, 13, 2, UI.C.ink, new Vector2(2, 2), "gd");
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>(); v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var ge = card.gameObject.AddComponent<GuideEntry>(); ge.q = q; ge.idx = Guide_Idx++;
            // .gd>summary{padding:10px 34px 10px 12px;font-weight:900;font-size:14px;gap:8px} em{font-size:20px} small{margin-left:auto;font-size:11.5px;color:muted}
            string left = title, small = null; int si = title.IndexOf("<small>");
            if (si >= 0) { left = title.Substring(0, si); int se = title.IndexOf("</small>", si); small = title.Substring(si + 7, (se > si ? se : title.Length) - si - 7); }
            ge.sum = Guide_Lower(Guide_Strip(title));
            var sum = UI.HStack(card, 8, TextAnchor.MiddleLeft, UI.Pad(12, 12, 10, 10), "summary");
            var lt = UI.Txt(sum, Guide_Rich(left.Replace("<em>", "<size=20>").Replace("</em>", "</size>")), 14, UI.C.text, true); UI.Size(lt, -1, -1, 1);
            if (small != null) UI.Txt(sum, Guide_Rich(small), 11.5f, UI.C.muted, true, TextAnchor.MiddleRight, false);
            ge.arrow = UI.Txt(sum, "▾", 14, UI.C.wood, true, TextAnchor.MiddleCenter, false);
            UI.Clickable(sum, () => ge.SetOpen(!ge.open));
            // .gd-b{padding:0 12px 11px;font-size:13px;line-height:1.5;font-weight:700;color:#3a2414}
            var b = UI.VStack(card, 0, UI.Pad(12, 12, 0, 11), TextAnchor.UpperLeft, "gd-b");
            var bt = UI.Txt(b, Guide_Rich(body), 13, UI.Hex("#3a2414"), true); bt.lineSpacing = 1.25f;
            ge.body = b.gameObject; ge.SetOpen(open);
        }

        public static List<string> GSources(string k)
        {
            var @out = new List<string>();
            foreach (var c in CROPS)
                if (Get(c.@out, k) > 0) @out.Add($"🌾 <b>Tarlada ek</b>{(c.season != null ? " (yalnızca " + string.Join(", ", c.season.Select(i => SEASONS[i].n)) + "; serada her mevsim)" : "")}: tohum ₺{Fun_Num(c.cost)}, {Secs(c.t)} sürer, {Get(c.@out, k)} adet çıkar {GLv(c.lvl)}{(!string.IsNullOrEmpty(c.col) ? $" · koleksiyondan <b>{COL.Find(x => x.id == c.col).n}</b> bulunmalı" : "")}");
            foreach (var kv in B)
            {
                string u = kv.Key; var d = kv.Value; if (d.recipes == null) continue;
                foreach (var r in d.recipes)
                {
                    if (Get(r.@out, k) == 0) continue; if (u == "sera" && CROPS.Any(c => c.id == r.id)) continue;
                    @out.Add($"{d.e} <b>{d.n}</b> → {r.n}: {GIn(r)}{(r.cost != 0 ? $" + ₺{Fun_Num(r.cost)}" : "")}, {Secs(r.t)} {GLv(r.lvl)}");
                }
            }
            if (B.ContainsKey("sera") && CROPS.Any(c => Get(c.@out, k) > 0)) @out.Add($"🏕️ <b>Serada</b> iki kat hızlı yetişir {GLv(B["sera"].lvl)}");
            if (ITEMS[k].buy != 0) @out.Add($"🏪 <b>Pazardan satın al</b>: tanesi ₺{Fun_Num(ITEMS[k].buy)}");
            return @out;
        }
        public static List<string> GUses(string k)
        {
            var @out = new List<string>();
            foreach (var kv in B) foreach (var r in kv.Value.recipes ?? new List<Recipe>()) { if (r.@in != null && Get(r.@in, k) > 0) @out.Add($"{kv.Value.e} {kv.Value.n} → <b>{r.n}</b> ({Get(r.@in, k)} adet) → {GOut(r)}"); }
            foreach (var c in CONTESTS) if (Get(c.need, k) > 0) @out.Add($"🎪 Panayır: <b>{c.n}</b> yarışması ({Get(c.need, k)} adet)");
            if (k == "yem") @out.Add("🐎 At antrenmanı (4 yem)"); if (k == "at") @out.Add("🏇 Yarış alanında ahıra al, ligde yarıştır");
            return @out;
        }
        public static void GItems(Transform parent)
        {
            Func<string, int> first = k =>
            {
                int l = 99;
                foreach (var c in CROPS) if (Get(c.@out, k) > 0) l = Math.Min(l, c.lvl + (!string.IsNullOrEmpty(c.col) ? 50 : 0));
                foreach (var kv in B) foreach (var r in kv.Value.recipes ?? new List<Recipe>()) if (Get(r.@out, k) > 0) l = Math.Min(l, Math.Max(r.lvl, kv.Value.lvl));
                return l;
            };
            foreach (var k in ITEMS.Keys.OrderBy(x => first(x)).ToList())
            {
                var it = ITEMS[k]; var src = GSources(k); var use = GUses(k);
                GEntry(parent, $"<em>{it.e}</em> {it.n}<small>~₺{Fun_Num(it.p)}{(Has(k) > 0 ? $" · ambarda {Has(k)}" : "")}</small>",
                    $"<p><b>Nereden elde edilir?</b></p><ul>{(src.Count > 0 ? string.Join("", src.Select(s => $"<li>{s}</li>")) : "<li>Özel ödüllerle</li>")}</ul>" +
                    (use.Count > 0 ? $"<p><b>Nerede kullanılır?</b></p><ul>{string.Join("", use.Select(s => $"<li>{s}</li>"))}</ul>" : "") +
                    $"<p class=\"note\">Pazar fiyatı ~₺{Fun_Num(it.p)}; her 3 dakikada %80–125 arasında değişir (Borsa'dan takip et). Aynı üründen çok satarsan pazar doyar ve fiyat düşer (📉), 15 dakikada toparlanır. Siparişler ve sözleşmeler doygunluktan etkilenmez.</p>");
            }
        }
        public static void GPlaces(Transform parent)
        {
            GEntry(parent, "<em>🌾</em> Tarlalar", $@"<p>Ürün yetiştirdiğin parseller. Boş parsele dokun ya da parmağını parseller üstünde sürükle: ekilir. Olgunlaşınca aynı şekilde topla.</p>
    <ul><li>Başta 2 parsel var, en fazla 9 olur. Yeni parsel fiyatları: {string.Join(", ", PLOT_COST.Skip(2).Select((c, i) => $"₺{Fmt(c)} (Sv {PLOT_LV.ElementAt(i + 2)})"))}</li>
    <li>Tarla seviyesi (⬆): +%30 ürün ve %10 daha hızlı büyüme. Fiyatlar: {string.Join(", ", FIELD_UP.Skip(1).Select((c, i) => $"Sv {i + 2}: ₺{Fmt(c)}"))}</li>
    <li>Ekilebilir ürünler: {string.Join(" · ", CROPS.Select(c => $"{ITEMS[c.id].e} {c.n} ({Secs(c.t)}, Sv {c.lvl})"))}</li>
    <li>Yağmurda ekinler daha hızlı büyür.</li><li>🍂 <b>Mevsim ürünleri</b> (lale, karpuz, patates, lahana) yalnızca kendi mevsiminde ekilir ve daha değerlidir; serada her mevsim yetişir.</li></ul>");
            foreach (var kv in B)
            {
                string u = kv.Key; var d = kv.Value; string k = LocOf(u); var L = k != null ? GetOr(LOC, k) : null; var R0 = L != null ? GetOr(REG, L.reg) : null;
                var recs = (d.recipes ?? new List<Recipe>()).Where(r => !(u == "sera" && CROPS.Any(c => c.id == r.id)));
                GEntry(parent, $"<em>{d.e}</em> {d.n}<small>₺{Fmt(d.cost)} · Sv {d.lvl}</small>", $@"<p>{d.d}</p><ul>
      <li>Yeri: <b>{(L != null ? L.n : "-")}</b>{(R0 != null && R0.cost != 0 ? $" ({R0.n} arazisi: ₺{Fmt(R0.cost)}, Sv {R0.lvl})" : "")}</li>
      {(d.storage != 0 ? $"<li>Her seviyede ambara +{d.storage} yer.</li>" : "")}
      {string.Join("", recs.Select(r => $"<li><b>{r.n}</b>: {GIn(r)}{(r.cost != 0 ? $" + ₺{Fun_Num(r.cost)}" : "")} → {GOut(r)} · {Secs(r.t)} {GLv(r.lvl)}</li>"))}
      {(u == "sera" ? "<li>Tarladaki bütün ürünler burada yarı sürede yetişir.</li>" : "")}
      <li>Yükseltme (⬆): her seviye +%30 ürün ve %10 kısa süre; maliyeti her seviyede iki katına çıkar. En fazla Sv {MAXL}. Sv 4–5 için ev ve oyuncu seviyesi gerekir.</li>
      {(INSIDE_UNITS.ContainsKey(u) ? "<li>🚪 İçeri girip hayvanları sev: mutluluk 70 üstündeyse her toplamada +1 ürün.</li>" : "")}</ul>");
            }
            var amb = new[] { 80, 160, 260, 400 };
            GEntry(parent, "<em>🏡</em> Çiftlik Evi", $"<p>Evin büyüdükçe ambar büyür ve binaların üst seviyelerinin kilidi açılır.</p><ul>{string.Join("", HOUSE.Select((x, i) => $"<li>{x.e} <b>{x.n}</b>{(x.cost != 0 ? $" · ₺{Fmt(x.cost)} · Sv {x.lvl}" : " (başlangıç)")} · ambar {amb[i]}</li>"))}</ul><p>Kumbaradan aldığın süsleri evin içine ve bahçeye yerleştirebilirsin.</p>");
            GEntry(parent, "<em>🗺️</em> Araziler", $"<p>Haritadaki bulutlu alanlar satın alınabilir araziler. Alt şeritteki 🔒 kartlara ya da bulutlara dokun.</p><ul>{string.Join("", REG.Values.Where(r => r.rect != null && r.cost != 0).Select(r => $"<li><b>{r.n}</b>: ₺{Fmt(r.cost)} · Sv {r.lvl} · {string.Join(", ", LOC.Values.Where(L => GetOr(REG, L.reg) == r).Select(L => L.e + " " + L.n))}</li>"))}</ul>");
        }

        static readonly string[][] Guide_Desk = {
            new[]{"map","Çiftliğe (haritaya) çık"}, new[]{"window","Pencere de haritaya çıkarır"}, new[]{"ledger","Tüccar siparişleri: istenen ürünleri teslim et, para + itibar kazan"},
            new[]{"mail","Sözleşmeler: büyük toplu teslimatlar, yonca ödüllü"}, new[]{"calendar","Günlük ödül, günlük görevler ve ana hedefler"}, new[]{"borsa","Ürünlerin güncel fiyatları ve son değişimleri"},
            new[]{"butce","Bugünkü gelir ve giderlerin"}, new[]{"gazete","İstatistiklerin ve rekorların"}, new[]{"piggy","Kumbara: süs dükkânı, yonca, ödüllü reklam"},
            new[]{"trophy","Koleksiyon: nadir buluntular ve bonusları"}, new[]{"basket","Ambar: elindeki ürünler ve kapasite"}, new[]{"radio","Müziği aç / kapat"}, new[]{"clock","Ayarlar: ses, grafik, hesap ve yedek kodu"} };

        public static void GuideBody(Transform parent, string tab, bool inAll = false)
        {
            switch (tab)
            {
                case "start":
                    GEntry(parent, "🔁 Temel döngü", "<ol><li><b>Ek</b> → tarlaya tohum ek, hayvanları besle.</li><li><b>Bekle</b> → süre dolunca kartta yeşil ✓ ve <b>🧺 Topla</b> çıkar.</li><li><b>Topla</b> → ürünler ambara gelir, deneyim (XP) kazanırsın.</li><li><b>İşle ya da sat</b> → buğdayı una, unu ekmeğe çevir; ya da pazarda sat.</li><li><b>Büyü</b> → yeni bina, parsel, arazi al; seviye atla, yeni ürünler açılsın.</li></ol>", true);
                    GEntry(parent, "🪑 Masa (ana ekran)", $"<p>Masadaki her eşya bir menü açar:</p><ul>{string.Join("", Guide_Desk.Select(e => $"<li>{(DNAMES.ContainsKey(e[0]) ? DNAMES[e[0]][0] : "🪟")} <b>{(DNAMES.ContainsKey(e[0]) ? DNAMES[e[0]][1] : "Pencere")}</b>: {e[1]}</li>"))}</ul>");
                    GEntry(parent, "🗺️ Harita", "<ul><li><b>Alttaki kartlar</b>: her bölüm bir kart. Karta dokun, oraya git.</li><li>Karttaki <b>🧺 Topla</b> / <b>▶ Başlat</b> düğmeleri hiç gitmeden işi yapar.</li><li><b>Hepsini topla</b>: bütün çiftliği tek dokunuşla toplar ve yeniden başlatır.</li><li>Bölüm panelinde <b>‹ ›</b> okları ya da başlığı sağa-sola kaydırmak bir sonraki bölüme geçirir.</li><li>Haritayı parmakla döndür, iki parmakla yakınlaştır. Sağdaki düğmeler de aynı işi yapar; 📷 fotoğraf modudur.</li><li>Rozetler: ✓ hazır, 🔨 kurulabilir, 💤 boşta, ☕ işçi molada, 🔒 kilitli. Kartta ⏱ en yakın işin kalan süresi yazar.</li><li>Karta <b>basılı tut</b>: bölüme gitmeden tarif seç, ekim yap ya da sıraya ekle.</li></ul>");
                    GEntry(parent, "📋 Üretim sırası", "<p>Bir bina çalışırken başka bir tarife dokunursan iş <b>sıraya</b> eklenir (en fazla 3). Biri bitip toplanınca sıradaki kendiliğinden başlar. Malzeme o an alınır; eksikse iş sırada bekler. Sıradaki işe dokunarak (✕) çıkarabilirsin.</p>");
                    GEntry(parent, "🚪 İç mekân", "<p>Kümes, ahır, ağıl, besi ahırı ve harada panelden <b>🚪 İçeri</b> ile girebilirsin. Hayvanlara dokunup sev, ürünleri yerden topla. Mutlu hayvanlar daha çok ürün verir.</p>");
                    break;
                case "res":
                    GEntry(parent, "₺ Para", "<p><b>Kazan</b>: pazarda satış, siparişler, sözleşmeler, panayır ve at yarışları.<br><b>Harca</b>: tohum, bina, yükseltme, parsel, arazi, işçi, süs.</p>", true);
                    GEntry(parent, "🍀 Yonca", "<p>Özel para birimi.</p><ul><li><b>Kazan</b>: seviye atlayınca (+2), günlük giriş ödülü, sözleşmeler, dostluk seviyesi, panayır birinciliği, at ligi, koleksiyon buluntusu, mini oyunlar, ödüllü reklam.</li><li><b>Harca</b>: üretimi anında bitir (kalan her 10 dakika için 1 yonca), özel süsler.</li></ul>");
                    GEntry(parent, "⭐ Seviye ve XP", $"<p>Her toplama XP verir. Seviye atlayınca yeni ürünler, binalar ve araziler açılır. Sıradaki seviye için {Fun_Num(XpNeed(S.lv))} XP gerekiyor (şu an {Fun_Num(S.xp)}).</p>");
                    GEntry(parent, "★ İtibar", "<p>0–100 arası. Sipariş ve sözleşme ödemelerini etkiler (itibar 0'da %70, 100'de %130).<br><b>Artırmak için</b>: sipariş teslim et (+1), sözleşme tamamla (+2), panayırda derece yap (+3 / +5).</p>");
                    GEntry(parent, "💰 Fazlayı sat", "<p>Ambar panelinde her üründen kaç tane tutacağını seç (0/5/10/20) ve gerisini tek dokunuşla sat. <b>Ambar dolunca otomatik sat</b> açıksa, ambar %95 dolduğunda fazlası kendiliğinden satılır, üretim durmaz. Yem ve taylar satılmaz.</p>");
                    GEntry(parent, "📦 Ambar", $"<p>Elinde aynı anda tutabileceğin ürün sınırı (şu an {Fun_Num(Stored())} / {Fun_Num(Cap())}). Ev büyüdükçe artar (80 → 160 → 260 → 400), her silo seviyesi +150 ekler. Ambar dolunca toplama yapılamaz; pazarda sat.</p>");
                    GEntry(parent, "🌦️ Mevsim ve hava", "<p>Mevsimler değiştikçe hava değişir. 🌧️ Yağmur ve ⛈️ fırtınada ekinler ve sera daha hızlı büyür; ilkbahar ve sonbahar daha yağışlıdır, kışın kar yağar.</p>");
                    break;
                case "items":
                    // .gall>p.note:not(.gempty){display:none}
                    if (!inAll) Fun_Note(parent, "Bir ürüne dokun: nereden çıktığını ve nerede kullanıldığını gör. Yeşil \"Sv\" etiketi açık, gri olan henüz kilitli demek.");
                    GItems(parent);
                    break;
                case "places":
                    GPlaces(parent);
                    break;
                case "events":
                    GEntry(parent, "📜 Tüccar Hanı (siparişler)", "<p>Kasabalılar ürün ister. Hepsini teslim edince para, XP ve itibar kazanırsın. Teslim ettikçe o kişiyle dostluğun artar.</p>", true);
                    GEntry(parent, "✉️ Sözleşmeler", "<p>Postadan gelir. Tek bir üründen çok sayıda, birkaç saat içinde teslim edilir. Parça parça teslim edebilirsin. Ödülü yüksektir, ayrıca 🍀 ve itibar verir.</p>");
                    GEntry(parent, "🎪 Panayır", $"<p>Her gün 3 yarışma çıkar ({string.Join(", ", CONTESTS.Select(c => c.e + " " + c.n))}). İstenen ürünleri ver ve katıl. İlgili binan ya da tarlan ne kadar yüksek seviyedeyse şansın o kadar artar. Birinciye para, 🍀 ve itibar, şanslıysan 🏆 Panayır Kupası.</p>");
                    GEntry(parent, "🏇 At ligi", $"<p>Harada tay yetiştir, Yarış Alanı'nda ahıra al (en fazla 4 at). Antrenman (4 yem) hız ya da dayanıklılığı artırır. Ligler: {string.Join(", ", TIERS.Select(t => $"{t.n} (giriş ₺{Fmt(t.fee)}, ödül ₺{Fmt(t.prize)})"))}.</p>");
                    GEntry(parent, "🏆 Koleksiyon", $"<p>Toplama yaparken nadiren özel buluntular çıkar. Her biri kalıcı bir bonus verir:</p><ul>{string.Join("", COL.Select(c => $"<li>{c.e} <b>{c.n}</b> ({c.hint}): {c.bonus}</li>"))}</ul>");
                    GEntry(parent, "👷 İşçiler", $"<p>Seviye {WORKER_LV}'ten itibaren her bina ve tarla için işçi tutabilirsin. İşçi hazır ürünü toplar ve aynı üretimi yeniden başlatır; sen oyunda yokken de çalışır.</p><ul><li><b>Vardiya:</b> işçi 8 saat çalışır, sonra ☕ çay molası verir. Panelde ya da alttaki kartta <b>☕ Devam</b>'a dokun, 8 saat daha çalışsın.</li><li><b>Kaç işçi?</b> Evin büyüdükçe artar: Kulübe 2, Taş Ev 5, Çiftlik Evi 9, Konak sınırsız.</li></ul>");
                    GEntry(parent, "🤝 Dostluk", $"<p>Kasabalılara sipariş ve sözleşme teslim ettikçe dostluğun artar: {string.Join(" → ", FR_N)}. Her kademe ödemeleri %6 artırır ve 🍀 verir; dostların her gün küçük hediyeler bırakır.</p>");
                    GEntry(parent, "🎮 Mini oyunlar", "<ul><li>🥚 <b>Yumurta yakala</b>: kümes çalışırken panelden.</li><li>🥛 <b>Sağım ritmi</b>: ahır çalışırken panelden.</li><li>🤝 <b>Pazarlık</b>: pazarda tüccarla.</li></ul><p>Ödül olarak üretimi hızlandırır, ekstra ürün ya da 🍀 verir.</p>");
                    GEntry(parent, "🎲 Rastgele olaylar", "<p>Oyun sırasında 20–40 dakikada bir küçük bir olay çıkar; çiftçinin balonunda duyurulur ve 10 dakika bekler:</p><ul><li>🦊 <b>Tilki</b>: 10 saniyede 5 kez dokunup kovala, yumurta ve 🍀 kazan.</li><li>🐑 <b>Kayıp kuzu</b>: üç çalıdan birinde saklanıyor, iki hakkın var.</li><li>🧳 <b>Gezgin tüccar</b>: ürün takası, koleksiyon tohumu ya da toplu yem.</li><li>🐝 <b>Arı oğulu</b> ve 🌈 <b>gökkuşağı</b>: küçük hediyeler.</li></ul>");
                    GEntry(parent, "⭐ Haftanın yarışması", "<p>Her hafta panayır yarışmalarından biri öne çıkar; ödülü iki katıdır (₺1.200 + 🍀6). Panayır panelinde sarı ile işaretlidir.</p>");
                    GEntry(parent, "📚 Kasabalı hikâyeleri", "<p>Bir kasabalıyla <b>Tanıdık</b> olunca 3 adımlık küçük hikâyesi açılır (Sipariş Defteri › Kasabalılar). Her adım bir ürün ister; karşılığında para, 🍀 ve dostluk kazanırsın.</p>");
                    GEntry(parent, "🏅 Başarımlar", $"<p>{ACH.Count} başarım var (Koleksiyon › Başarımlar). Koşulu sağlayınca 🍀 ödülünü al; masadaki kupada 🏅 rozeti çıkar.</p>");
                    GEntry(parent, "🏛️ Kasaba projeleri", $"<p>Tüccar Hanı › Kasaba sekmesinde büyük bağışlarla kasabayı güzelleştir; her proje kalıcı bir avantaj verir:</p><ul>{string.Join("", PROJ.Select(p => $"<li>{p.e} <b>{p.n}</b> (Sv {p.lv}, ₺{Fmt(p.c)}): {p.bonus}</li>"))}</ul>");
                    GEntry(parent, "🌳 Yeni nesil", $"<p>Konak ve seviye {GEN_LV}'dan sonra Çiftlik Evi panelinden çiftliği yeni nesle devredebilirsin. Baştan başlarsın ama bütün üretim her nesilde kalıcı +%10 artar; yonca, koleksiyon, başarımlar, süsler, dostluklar ve kasaba projeleri kalır.</p>");
                    GEntry(parent, "📅 Günlük ödül ve görevler", "<p>Her gün giriş yap: 7 günlük seri ödülleri (para, yem, 🍀). Takvimde her gün 3 küçük görev çıkar; bitirince ödülünü al.</p>");
                    break;
                case "tips":
                    GEntry(parent, "💡 Hızlı ilerleme", "<ul><li>Uzun süreli ürünleri (kabak, kaşar, kestane balı, büyük hasat) oyundan çıkmadan önce başlat.</li><li>Ham ürün yerine işlenmiş ürün sat: un → ekmek, süt → peynir çok daha kârlı.</li><li>Borsa'da ▲ olan ürünleri sat, ▼ olanları bekle.</li><li>Yemi yem değirmeninde üret, pazardan almaktan ucuz.</li><li>Ambar dolmasın: siloyu yükselt, evi büyüt.</li><li>İşçiler zamanını en çok kazandıran yatırımdır.</li><li>Ayarlar → <b>Hızlı gezinme</b> açıkken kamera uçmadan anında geçer.</li></ul>", true);
                    GEntry(parent, "🔐 Hesap ve yedek", "<p>İlerlemen cihazda ve buluta kaydedilir. Ayarlar'daki <b>kurtarma kodunu</b> bir yere not et: yeni bir cihazda bu kodla çiftliğine geri dönebilirsin.</p>");
                    GEntry(parent, "🔠 Yazı boyutu ve sol el", "<p>Ayarlar'dan yazıları <b>Büyük</b> ya da <b>Çok büyük</b> yapabilirsin. <b>Sol el modu</b> kamera düğmelerini sola alır.</p>");
                    GEntry(parent, "🔋 Pil ve akıcılık", "<p>Ayarlar → Grafik: <b>Pil tasarrufu</b> gölgeleri kapatır ve kare hızını sınırlar. Eski telefonlarda daha akıcı oynar.</p>");
                    break;
            }
        }

        // Arama durumu (o an açık rehber paneli)
        static RectTransform Guide_BodyEl, Guide_AllEl, Guide_TabsEl; static Text Guide_Empty; static bool Guide_Built;
        public static void SheetGuide(Transform parent)
        {
            string tab = View != null && View.tab != null ? View.tab : "start";
            Head(parent, "📖", "Rehber", "Ne nedir, nereden çıkar, nasıl yapılır");
            var body = Fun_ShBody(parent, "guide");
            // .guide .gsearch{width:100%;border:2px ink;border-radius:999px;padding:9px 14px;font:800 14px;background:card}
            var inp = UI.Input(body, "", "Ara: un, kaşar, işçi, yonca...", null);
            var ib = (RectTransform)inp.transform; UI.SetBoxColor(ib, UI.C.card);
            var img = ib.GetComponent<Image>(); if (img != null) UI.SetRadius(img, 20);
            var inner = ib.Find("bg"); if (inner != null) { var ii = inner.GetComponent<Image>(); if (ii != null) UI.SetRadius(ii, 18); }
            inp.onValueChanged.AddListener(Guide_OnInput);
            // .guide .gtabs{flex-wrap:wrap;border-radius:16px} button{flex:1 1 30%;font-size:12px;padding:6px 4px}
            Guide_TabsEl = Fun_Tabs(body, GTABS, tab, 3);
            Guide_BodyEl = UI.VStack(body, 8, null, TextAnchor.UpperLeft, "gbody");
            GuideBody(Guide_BodyEl, tab);
            Guide_AllEl = UI.VStack(body, 8, null, TextAnchor.UpperLeft, "gall");
            Guide_AllEl.gameObject.SetActive(false); Guide_Built = false; Guide_Empty = null;
        }
        // Arama: bütün sekmelerde arar, eşleşen kartları açık gösterir
        static void Guide_OnInput(string val)
        {
            if (Guide_BodyEl == null || Guide_AllEl == null) return;
            string q = Guide_Lower((val ?? "").Trim());
            if (q == "") { Guide_AllEl.gameObject.SetActive(false); Guide_BodyEl.gameObject.SetActive(true); Guide_TabsEl.gameObject.SetActive(true); return; }
            if (!Guide_Built) { foreach (var t in GTABS) GuideBody(Guide_AllEl, t[0], true); Guide_Built = true; }
            Guide_BodyEl.gameObject.SetActive(false); Guide_TabsEl.gameObject.SetActive(false); Guide_AllEl.gameObject.SetActive(true);
            int n = 0;
            var ents = Guide_AllEl.GetComponentsInChildren<GuideEntry>(true).OrderBy(e => e.idx).ToList();
            var tops = new List<GuideEntry>(); var rest = new List<GuideEntry>();
            foreach (var d in ents)
            {
                bool ok = d.q.Contains(q), top = ok && d.sum.Contains(q);
                d.gameObject.SetActive(ok); d.SetOpen(top || (ok && q.Length > 2 && n < 3));
                (top ? tops : rest).Add(d);   // style.order = top ? -1 : 0
                if (ok) n++;
            }
            if (Guide_Empty == null) { Guide_Empty = Fun_Note(Guide_AllEl, ""); Guide_Empty.name = "gempty"; }
            int si = 0;
            foreach (var d in tops) d.transform.SetSiblingIndex(si++);
            Guide_Empty.transform.SetSiblingIndex(si++);
            foreach (var d in rest) d.transform.SetSiblingIndex(si++);
            Guide_Empty.text = n > 0 ? "" : "Sonuç yok. Başka bir kelime dene.";
            Guide_Empty.gameObject.SetActive(n == 0);
        }
    }
}
