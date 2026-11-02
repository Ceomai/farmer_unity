// ================= İLK OYUN REHBERİ (37-tutorial.js) =================
// Yeni oyuncuyu el işaretiyle ilk döngüden geçirir: haritaya çık → tarla → ek → topla → pazar → sat.
// Web sürümü hedefleri document.querySelector ile bulur. Unity'de arayüz öğeleri seçici adıyla etiketlenir:
//   Game.Tut_Tag(öğe, ".scard[data-go=\"tarla\"]")  (ya da GameObject adı seçicinin kendisi olur).
// Tut_Query(seçici) etkin (görünür) ilk eşleşmeyi döndürür (querySelector + offsetParent!==null).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // Kayıttaki S.tut / S.tut2 ek alanları
    public partial class Tut
    {
        public int? skipped;
        public int? tap;
        public TutBase b;
    }
    public partial class TutBase { public double plant, harv, sold; }

    // querySelector karşılığı: öğeye CSS seçicisi etiketi
    public class TutSel : MonoBehaviour { public List<string> sels = new List<string>(); }

    public class TutStepDef
    {
        public string say;
        public Func<RectTransform> el;
        public Func<Tut, bool> done;
        public bool tap;
    }

    public static partial class Game
    {
        // ---------- Seçici yardımcıları ----------
        public static void Tut_Tag(Component c, params string[] sels)
        {
            if (c == null) return;
            var t = c.gameObject.GetOrAdd<TutSel>();
            foreach (var s in sels) if (!t.sels.Contains(s)) t.sels.Add(s);
        }
        public static RectTransform Tut_Query(string sel)
        {
            if (UI.Root == null || sel == null) return null;
            // önbellek: aynı öğe hâlâ etkin ve eşleşiyorsa tüm ağacı taramadan döndür
            RectTransform c;
            if (Tut_Cache.TryGetValue(sel, out c) && c != null && c.gameObject.activeInHierarchy && Tut_Matches(c, sel)) return c;
            c = Tut_Scan(sel); Tut_Cache[sel] = c; return c;
        }
        static readonly Dictionary<string, RectTransform> Tut_Cache = new Dictionary<string, RectTransform>();
        static bool Tut_Matches(RectTransform rt, string sel)
        {
            var t = rt.GetComponent<TutSel>(); if ((t != null && t.sels.Contains(sel)) || rt.name == sel) return true;
            var d = rt.GetComponent<Dom>(); return d != null && d.Is(sel);   // Dom.Tag ile bulunan öğe de önbellekten dönebilsin
        }
        static RectTransform Tut_Scan(string sel)
        {
            var tags = UI.Root.GetComponentsInChildren<TutSel>(false);
            if (tags != null) foreach (var t in tags) if (t != null && t.sels.Contains(sel)) return t.transform as RectTransform;
            var all = UI.Root.GetComponentsInChildren<RectTransform>(false);
            if (all != null) foreach (var rt in all) if (rt != null && rt.name == sel) return rt;
            // Arayüz grubu öğeleri Dom.Tag ile etiketliyor: querySelector karşılığı
            // querySelector + offsetParent!==null: görünür ilk eşleşme (gizli bir eşleşme görünür olanı örtmesin)
            foreach (var d in Dom.QAll(sel)) if (d != null && d.Visible) return d.transform as RectTransform;
            return null;
        }
        static readonly Vector3[] Tut_Corners = new Vector3[4];
        // getBoundingClientRect(): CSS px, sol üstten (x=left, y=top)
        public static Rect Tut_CssRect(RectTransform rt)
        {
            rt.GetWorldCorners(Tut_Corners);
            var bl = UI.ScreenToCss(Tut_Corners[0]); var tr = UI.ScreenToCss(Tut_Corners[2]);
            return Rect.MinMaxRect(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y), Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
        }

        // ---------- Adımlar ----------
        public static readonly List<TutStepDef> TUT = new List<TutStepDef> {
            new TutStepDef{ say="Hoş geldin! Masadaki <b>haritaya</b> dokun, çiftliğine çıkalım.", el=()=>Tut_Query("[data-desk=\"map\"]"), done=t=>Mode=="hub" },
            new TutStepDef{ say="Bu senin çiftliğin. Alttaki <b>Tarlalar</b> kartına dokun.", el=()=>Tut_Query(".scard[data-go=\"tarla\"]"), done=t=>Mode=="focus"&&Focus=="tarla" },
            new TutStepDef{ say="Boş bir parsele dokun ya da parmağını parsellerin üzerinde gezdirerek <b>buğday ek</b>.", el=()=>Tut_Query(".plot.empty"), done=t=>Get(S.stats,"plant_bugday")>t.b.plant },
            new TutStepDef{ say="Buğdaylar olgunlaşıyor… Hazır olunca parsele dokunup <b>topla</b>.", el=()=>Tut_Query(".plot.ready"), done=t=>Get(S.stats,"harvest_field")>t.b.harv },
            new TutStepDef{ say="Harika hasat! Şimdi <b>← Harita</b> düğmesiyle geri dön.", el=()=>Tut_Query("#backBtn"), done=t=>Mode=="hub" },
            new TutStepDef{ say="Ürünlerini satmak için <b>Köy Pazarı</b> kartına dokun.", el=()=>Tut_Query(".scard[data-go=\"pazar\"]"), done=t=>Mode=="focus"&&Focus=="pazar" },
            new TutStepDef{ say="Buğdayın yanındaki <b>Tümü</b> düğmesiyle sat.", el=()=>Tut_Query("[data-act=\"sell\"][data-k=\"bugday\"][data-n=\"999999\"]"), done=t=>Get(S.stats,"sold")>t.b.sold },
        };
        // İkinci tur (seviye 2 ve kümes varken): üretim sırası, sipariş ve yükseltme
        public static readonly List<TutStepDef> TUT2 = new List<TutStepDef> {
            new TutStepDef{ say="Tavuklar acıktı! Alttaki <b>Kümes</b> kartına dokun.", el=()=>Tut_Query(".scard[data-go=\"kumes\"]"), done=t=>Mode!="desk"&&Focus=="kumes" },
            new TutStepDef{ say="Tarife dokunup <b>yemle</b>. Çalışırken başka tariflere dokunursan <b>sıraya</b> eklenir; biri bitince sıradaki kendiliğinden başlar.", el=()=>Tut_Query(".tile[data-u=\"kumes\"]"), done=t=>Fun_HasUnit("kumes")&&S.units["kumes"].job!=null },
            new TutStepDef{ say="Beklerken kasabaya bakalım. <b>← Harita</b> ile geri dön.", el=()=>Tut_Query("#backBtn"), done=t=>Mode=="hub" },
            new TutStepDef{ say="<b>Tüccar Hanı</b> kartına dokun: kasabalılar burada ürün ister.", el=()=>Tut_Query(".scard[data-go=\"han\"]"), done=t=>Focus=="han"&&Mode=="focus" },
            new TutStepDef{ say="İstenen ürünler tamam olunca <b>Teslim et</b>. Siparişler pazardan daha çok öder, itibarını ve dostluğunu artırır.", el=()=>Tut_Query(".order"), tap=true },
            new TutStepDef{ say="İpucu: alttaki karta <b>basılı tut</b>, bölüme gitmeden tarif seç. Binaları <b>⬆</b> ile yükseltince daha çok ürün verirler.", el=()=>null, tap=true },
        };

        // ---------- Görünüm (.tut) ----------
        static RectTransform tutEl, tutHand, tutSay; static Text tutTxt; static Button tutNext;
        static bool tutTop, tutHandUp; static Vector2 tutHandTarget, tutHandPos; static bool tutHandPlaced;
        static void TutHide() { if (tutEl != null) tutEl.gameObject.SetActive(false); }
        static void Tut_SetTop(bool on) { tutTop = on; Tut_Layout(); }

        static void Init_37()
        {
            // .tut{position:fixed;inset:0;z-index:35;pointer-events:none}
            tutEl = UI.New("tut", UI.ToastLayer); UI.Stretch(tutEl);
            // .tut-hand{width:44px;height:50px;font-size:44px;filter:drop-shadow(2px 3px 0 rgba(42,26,16,.85));animation:tap 1s infinite}
            var hand = UI.Txt(tutEl, "👆", 44, null, false, TextAnchor.MiddleCenter, false);
            var sh = hand.gameObject.AddComponent<Shadow>(); sh.effectColor = UI.A(UI.C.ink, .85f); sh.effectDistance = new Vector2(2, -3);
            tutHand = hand.rectTransform; UI.PlaceTL(tutHand, 0, 0, 44, 50); tutHand.pivot = new Vector2(.5f, .5f);
            // .tut-say{width:min(380px,100%-28px);gap:9px;padding:9px 10px 9px 8px;border:2.5px;border-radius:18px;background:#fffaf0;box-shadow:3px 4px 0 ink;font-weight:800;font-size:13.5px}
            tutSay = UI.Box(tutEl, UI.Hex("#fffaf0"), 18, 2.5f, UI.C.ink, new Vector2(3, 4), "tut-say");
            var h = tutSay.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 9; h.padding = UI.Pad(8, 10, 9, 9); h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            var fit = tutSay.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Talk_Face(tutSay);
            tutTxt = UI.Txt(tutSay, "", 13.5f, UI.C.ink, true, TextAnchor.MiddleLeft, true); UI.Size(tutTxt, -1, -1, 1);
            // .tut-next{border:2px ink;border-radius:999px;background:honey;font-weight:900;font-size:12px;padding:4px 10px;box-shadow:1px 2px 0 ink}
            var nb = UI.Box(tutSay, UI.C.honey, 12, 2, UI.C.ink, new Vector2(1, 2), "tut-next");
            var nh = nb.gameObject.AddComponent<HorizontalLayoutGroup>(); nh.padding = UI.Pad(10, 10, 4, 4); nh.childControlWidth = nh.childControlHeight = true; nh.childForceExpandWidth = nh.childForceExpandHeight = false;
            UI.Txt(nb, "Tamam ›", 12, UI.C.ink, true, TextAnchor.MiddleCenter, false);
            tutNext = UI.Clickable(nb, () => { if (Tut2Step() >= 0) { S.tut2.tap = 1; Tut2Tick(); } });
            nb.gameObject.SetActive(false);
            // .tut-skip{border:0;background:none;color:muted;font-weight:900;font-size:11px;text-decoration:underline;padding:4px}
            var sk = UI.New("tut-skip", tutSay);
            var skh = sk.gameObject.AddComponent<HorizontalLayoutGroup>(); skh.padding = UI.Pad(4); skh.childControlWidth = skh.childControlHeight = true; skh.childForceExpandWidth = skh.childForceExpandHeight = false;
            UI.Txt(sk, "<i>Geç</i>", 11, UI.C.muted, true, TextAnchor.MiddleCenter, false);
            var ul = UI.Fill(sk, UI.C.muted, 0, "u"); ul.raycastTarget = false; UI.IgnoreLayout(ul);
            ul.rectTransform.anchorMin = new Vector2(0, 0); ul.rectTransform.anchorMax = new Vector2(1, 0); ul.rectTransform.offsetMin = new Vector2(4, 4); ul.rectTransform.offsetMax = new Vector2(-4, 5);
            UI.Clickable(sk, () =>
            {
                if (Tut2Step() >= 0) { S.tut2 = new Tut { done = 1, skipped = 1 }; TutHide(); Save(); Track("tutorial2_skip"); return; }
                S.tut = new Tut { done = 1, skipped = 1 }; TutHide(); Save(); Track("tutorial_skip", new Dictionary<string, object> { { "step", TutStep() } });
            });
            tutEl.gameObject.SetActive(false);
            UI.OnResize += Tut_Layout;
            // tap / tapd animasyonu ve "transition:left .25s,top .25s"
            OnFrame += dt =>
            {
                if (tutEl == null || !tutEl.gameObject.activeSelf || !tutHand.gameObject.activeSelf) return;
                tutHandPos = tutHandPlaced ? Vector2.Lerp(tutHandPos, tutHandTarget, 1 - Mathf.Exp(-dt / .08f)) : tutHandTarget; tutHandPlaced = true;
                float ph = Time.unscaledTime % 1f, k = .5f - .5f * Mathf.Cos(ph * Mathf.PI * 2);
                float off = tutHandUp ? 10 * k : -10 * k;
                tutHand.anchoredPosition = new Vector2(tutHandPos.x + 22, -(tutHandPos.y + 25 + off));
                tutHand.localRotation = Quaternion.Euler(0, 0, tutHandUp ? 180 : 0);
            };
        }
        // .tut-say: bottom:170px ortada; .tut.top → top:110px (700px altı 150px)
        static void Tut_Layout()
        {
            if (tutSay == null) return;
            float w = Mathf.Min(380, UI.W - 28);
            tutSay.sizeDelta = new Vector2(w, tutSay.sizeDelta.y);
            if (tutTop) { tutSay.anchorMin = tutSay.anchorMax = tutSay.pivot = new Vector2(.5f, 1); tutSay.anchoredPosition = new Vector2(0, -(UI.W <= 700 ? 150 : 110)); }
            else { tutSay.anchorMin = tutSay.anchorMax = tutSay.pivot = new Vector2(.5f, 0); tutSay.anchoredPosition = new Vector2(0, 170); }
        }
        static void Tut_Show(string say)
        {
            tutEl.gameObject.SetActive(true); tutEl.SetAsLastSibling();
            tutTxt.text = say; Tut_Layout();
        }
        // El işaretini öğenin altına (ya da ekranın altındaysa üstüne) koy
        static void Tut_Point(RectTransform el)
        {
            if (el != null && el.gameObject.activeInHierarchy)
            {
                var r = Tut_CssRect(el); bool wasHidden = !tutHand.gameObject.activeSelf;
                tutHand.gameObject.SetActive(true); bool up = r.yMin > UI.H * .55f; tutHandUp = up;
                tutHandTarget = new Vector2(RoundI(r.xMin + r.width / 2 - 22), RoundI(up ? r.yMin - 56 : r.yMax + 4));
                if (wasHidden) tutHandPlaced = false;
                Tut_SetTop(r.yMin > UI.H * .5f);
            }
            else { tutHand.gameObject.SetActive(false); Tut_SetTop(true); }
        }

        static bool Tut_IsDone(Tut t) => t != null && t.done.HasValue && t.done.Value != 0;
        public static int Tut2Step() => S != null && S.tut2 != null && !Tut_IsDone(S.tut2) ? (S.tut2.i ?? -1) : -1;
        public static void Tut2Start()
        {
            if (S.tut2 != null || S.tut == null || !Tut_IsDone(S.tut) || S.lv < 2 || !Fun_HasUnit("kumes") || Mode == "desk") return;
            if (Get(S.stats, "orders") > 0) { S.tut2 = new Tut { done = 1 }; return; }
            S.tut2 = new Tut { i = 0 }; Track("tutorial2_start");
        }
        public static bool Tut2Tick()
        {
            int i = Tut2Step(); if (i < 0) return false;
            if (UI.AnyModal) { TutHide(); return true; }
            var st = TUT2[i];
            if (st.tap ? (S.tut2.tap ?? 0) != 0 : st.done(S.tut2))
            {
                S.tut2.i = S.tut2.i + 1; S.tut2.tap = 0;
                if (S.tut2.i >= TUT2.Count) { S.tut2 = new Tut { done = 1 }; TutHide(); Gain(100, "Ödül"); S.gems += 2; Sfx("win"); Toast("Çiftlik işleri tamam: +₺100 +🍀2"); Track("tutorial2_done"); Commit(); return true; }
                Save(); return Tut2Tick();
            }
            Tut_Show(st.say); tutNext.gameObject.SetActive(st.tap);
            Tut_Point(st.el());
            return true;
        }
        public static int TutStep() => S != null && S.tut != null && !Tut_IsDone(S.tut) ? (S.tut.i ?? -1) : -1;
        public static void TutStart()
        {
            if (S.tut != null) return;
            if (S.goal > 0 || Get(S.stats, "plant_bugday") > 0) { S.tut = new Tut { done = 1 }; return; }
            S.tut = new Tut { i = 0, b = new TutBase { plant = 0, harv = 0, sold = 0 } }; Track("tutorial_start");
        }
        public static void TutTick()
        {
            int i = TutStep();
            if (i < 0) { Tut2Start(); if (!Tut2Tick()) TutHide(); return; }
            tutNext.gameObject.SetActive(false);
            if (UI.AnyModal) { TutHide(); return; }
            var st = TUT[i];
            if (S.tut.b == null) S.tut.b = new TutBase();
            if (st.done(S.tut))
            {
                S.tut.i = S.tut.i + 1; Track("tutorial_step", new Dictionary<string, object> { { "i", S.tut.i } });
                S.tut.b = new TutBase { plant = Get(S.stats, "plant_bugday"), harv = Get(S.stats, "harvest_field"), sold = Get(S.stats, "sold") };
                // rehberdeki ilk ekin hızlı büyüsün
                if (S.tut.i == 3) foreach (var f in S.fields) { if (f.job != null && f.job.r == "bugday") f.job.e = Math.Min(f.job.e, Now() + 5000); }
                if (S.tut.i >= TUT.Count)
                {
                    S.tut = new Tut { done = 1 }; TutHide(); Gain(150, "Ödül"); S.gems += 3; Sfx("win"); Celebrate("Aferin!");
                    SetTimeout(() => Toast("İlk adımlar tamam: +₺150 +🍀3 · Çiftçinin baloncukları ve 📖 Rehber seni yönlendirmeye devam edecek"), 900); Track("tutorial_done"); Commit(); return;
                }
                Save(); return;
            }
            Tut_Show(st.say);
            Tut_Point(st.el());
        }
    }
}
