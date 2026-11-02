// ================= KOLEKSİYON (17-collection.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public class ColDef { public string id, n, e, src; public double p; public string bonus, hint; }

    public static partial class Game
    {
        static ColDef Col_C(string id, string n, string e, string src, double p, string bonus, string hint) => new ColDef { id = id, n = n, e = e, src = src, p = p, bonus = bonus, hint = hint };
        static List<ColDef> _col;
        public static List<ColDef> COL => _col ?? (_col = new List<ColDef>
        {
            Col_C("altin_yumurta", "Altın Yumurta", "🥇", "kumes", .03, "Kümes her toplamada +1 yumurta", "Kümeste"),
            Col_C("simental", "Simental İnek", "🐮", "ahir", .04, "Ahır her toplamada +1 süt", "Ahırda"),
            Col_C("ankara", "Ankara Keçisi", "🐐", "agil", .04, "Ağıl her toplamada +1 yün", "Ağılda"),
            Col_C("kralice_ari", "Kraliçe Arı", "👑", "kovan", .06, "Arılık her toplamada +1 bal", "Arılıkta"),
            Col_C("bakir_kazan", "Bakır Kazan", "🫕", "mandira", .05, "Mandıra %10 daha hızlı", "Mandırada"),
            Col_C("arap", "Arap Atı Soyağacı", "📜", "hara", .2, "Atların yarışta daha güçlü", "Harada"),
            Col_C("altin_bugday", "Altın Buğday Tohumu", "🌟", "field", .02, "Altın Buğday ekilebilir", "Tarlalarda"),
            Col_C("mor_havuc", "Mor Havuç Tohumu", "🥕", "field", .015, "Mor Havuç ekilebilir", "Tarlalarda"),
            Col_C("kangal", "Kangal Yavrusu", "🐕", "any", .02, "Sipariş ödülleri +%5", "Her yerde"),
            Col_C("eski_tapu", "Eski Tapu", "🗺️", "any", .015, "Arazi fiyatları -%10", "Her yerde"),
            Col_C("nazar", "Nazar Boncuğu", "🧿", "any", .02, "Mini oyun ödülleri artar", "Her yerde"),
            Col_C("kupa", "Panayır Kupası", "🏆", "fair", 0, "İtibar +5", "Panayırda birinci ol"),
        });
        public static readonly Dictionary<string, string> COL_BONUS = new Dictionary<string, string> { { "kumes", "altin_yumurta" }, { "ahir", "simental" }, { "agil", "ankara" }, { "kovan", "kralice_ari" } };
        public static bool Has1(string id) => S != null && S.album != null && Get(S.album, id) != 0;
        public static bool CropLocked(Recipe c) => c.lvl > S.lv || (c.col != null && !Has1(c.col)) || !CropSeasonOk(c);
        public static double RegCost(string r) => Round(REG[r].cost * (Has1("eski_tapu") ? .9 : 1));

        public static void ShowFind(ColDef c)
        {
            Sfx("win");
            Hlp_Modal(c.e, "Nadir buluntu!", "<b>" + c.n + "</b>\n" + c.bonus + "\n+1 🍀", "Koleksiyona ekle", false);   // .modal.login
        }
        public static ColDef TryDrop(string src)
        {
            foreach (var c in COL)
            {
                if (c.src != src && c.src != "any") continue;
                if (Rnd() < c.p * (c.src == "any" ? .3 : 1))
                {
                    bool first = Get(S.album, c.id) == 0; S.album[c.id] = Get(S.album, c.id) + 1;
                    if (first) { S.gems += 1; SetTimeout(() => ShowFind(c), 500); }
                    return c;
                }
            }
            return null;
        }

        // 40-fun bu sayfanın gövdesinin başına sekme ekler (JS'te sheetAlbum'u sarar): kanca
        public static Action<Transform> SheetAlbumBodyTop;

        public static void SheetAlbumCol(Transform parent)
        {
            int n = COL.Count(c => Get(S.album, c.id) != 0);
            Head(parent, "📖", "Koleksiyon", n + " / " + COL.Count);
            RectTransform body;
            var sc = UI.Scroll(parent, out body, 10, UI.Pad(12)); UI.Size(sc, -1, -1, 1, 1); sc.name = "sh-body";
            SheetAlbumBodyTop?.Invoke(body);
            // .places: 3 sütunlu ızgara
            var places = UI.VStack(body, 8, null, TextAnchor.UpperLeft, "places");
            RectTransform row = null;
            for (int i = 0; i < COL.Count; i++)
            {
                var c = COL[i]; bool got = Get(S.album, c.id) != 0;
                if (i % 3 == 0) { row = UI.HStack(places, 8, TextAnchor.UpperLeft, null, "row"); row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = true; }
                var pl = UI.Box(row, got ? UI.C.card : UI.Hex("#e6d9bb"), 14, 2, UI.C.ink, new Vector2(2, 3), got ? "place" : "place locked");
                UI.Size(pl, 0, -1, 1);
                var v = pl.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                v.spacing = 4; v.padding = UI.Pad(6, 4); v.childAlignment = TextAnchor.UpperCenter; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                var th = UI.HStack(pl, 0, TextAnchor.MiddleCenter, null, "thumbRow");
                var thumb = UI.Box(th, UI.Hex("#d1d29b"), 12, 2, UI.C.ink, null, "thumb sm"); UI.Size(thumb, 42, 42);
                var te = UI.Txt(thumb, got ? c.e : "❓", 20, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(te.rectTransform); UI.IgnoreLayout(te);
                UI.Txt(pl, got ? c.n : "???", 11.5f, got ? UI.C.text : UI.C.muted, true, TextAnchor.UpperCenter, true);
                UI.Txt(pl, got ? c.bonus : c.hint, 10.5f, UI.C.muted, true, TextAnchor.UpperCenter, true);   // title: got ? bonus : "Henüz bulunmadı · "+hint
            }
            if (n == COL.Count && Get(S.stats, "albumDone") == 0) UI.ActBtn(body, "Albüm tamam! +🍀50", "albumDone", "big gold");
            UI.Txt(body, "Nadir buluntular üretim ve hasat sırasında rastgele çıkar. Her yeni buluntu +1 🍀 verir.", 12.5f, UI.C.muted, true).lineSpacing = 1.45f;
        }
    }
}
