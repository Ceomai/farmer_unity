// ================= ÇİFTÇİ BALONCUĞU (28-farmer-talk.js) =================
// Çiftçinin sözü haritanın ortasında değil: üst çubuğun hemen altında, sağa yaslı küçük bir bildirim.
// Masaüstünde panel açıksa panelin soluna kayar. Konum 4 kez/sn hesaplanır (her karede düzen ölçmemek için).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // farmerHints() öğesi: [anahtar, metin, eylem]
    public class FarmerHint
    {
        public string key, txt; public Action act;
        public FarmerHint(string key, string txt, Action act) { this.key = key; this.txt = txt; this.act = act; }
    }

    public static partial class Game
    {
        // const sayB=document.createElement('button');sayB.className='farmer-say';sayB.hidden=true
        public static RectTransform SayB;
        static Text Talk_Text; static RectTransform Talk_Go; static LayoutElement Talk_TextLE;
        static double sayT = 0; static Action sayAct = null; static string sayKey = "";
        static double bubT = 0;
        public static bool SayHidden => SayB == null || !SayB.gameObject.activeSelf;   // sayB.hidden
        public static void SayHide() { if (SayB != null) SayB.gameObject.SetActive(false); }  // sayB.hidden=true

        static void Init_28()
        {
            // .farmer-say{right:12px;top:110px;max-width:min(330px,calc(100vw - 150px));padding:6px 10px 6px 5px;font-size:12px;border-radius:14px;border:2.5px;background:#fffaf0;box-shadow:2px 3px 0 ink}
            SayB = UI.Box(UI.Hud, UI.Hex("#fffaf0"), 14, 2.5f, UI.C.ink, new Vector2(2, 3), "farmer-say");
            SayB.anchorMin = SayB.anchorMax = SayB.pivot = new Vector2(1, 1);
            SayB.anchoredPosition = new Vector2(-12, -110);
            var h = SayB.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8; h.padding = UI.Pad(5, 10, 6, 6); h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            UI.FitBoth(SayB);
            Talk_Face(SayB);
            Talk_Text = UI.Txt(SayB, "", 12, UI.Hex("#2a1a10"), true, TextAnchor.MiddleLeft, true);
            Talk_Text.lineSpacing = 1.3f / 1.2f;
            Talk_TextLE = UI.Size(Talk_Text);
            // .fs-go{width:22px;height:22px;border-radius:50%;background:forest;color:#fff;font-size:12px}
            var go = UI.Box(SayB, UI.C.forest, 11, 0, null, null, "fs-go"); UI.Size(go, 22, 22, 0, 0, 22, 22);
            var gt = UI.Txt(go, "▸", 12, Color.white, true, TextAnchor.MiddleCenter, false); UI.Stretch(gt.rectTransform); UI.IgnoreLayout(gt);
            Talk_Go = go;
            UI.Clickable(SayB, () => { Sfx("click"); var a = sayAct; SayHide(); sayT = Now(); if (a != null) a(); });
            SayB.gameObject.SetActive(false);
        }
        // <span class="fs-face">👨‍🌾</span>: 32px yuvarlak, #f2d7a8, 2px mürekkep kenarlık, 18px yazı (37-tutorial de kullanır)
        internal static RectTransform Talk_Face(Transform parent)
        {
            var f = UI.Box(parent, UI.Hex("#f2d7a8"), 16, 2, UI.C.ink, null, "fs-face"); UI.Size(f, 32, 32, 0, 0, 32, 32);
            var t = UI.Txt(f, "👨‍🌾", 18, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform); UI.IgnoreLayout(t);
            return f;
        }
        // max-width:min(330px,calc(100vw - 150px)); 701px ve üstü: 340px
        static void Talk_FitWidth()
        {
            if (Talk_Text == null) return;
            float maxW = UI.W >= 701 ? 340 : Mathf.Min(330, UI.W - 150);
            float inner = maxW - 5 - 10 - 5 - 32 - 8 - (Talk_Go.gameObject.activeSelf ? 8 + 22 : 0);
            Talk_TextLE.preferredWidth = Mathf.Max(40, Mathf.Min(inner, Talk_Text.preferredWidth));
        }

        public static List<FarmerHint> FarmerHints()
        {
            var H = new List<FarmerHint>();
            int idle = S.fields.Count(f => f.job == null), rf = S.fields.Count(f => f.job != null && Now() >= f.job.e), ro = ReadyOrders();
            string wx = S.weather.k;
            { var eh = EvtHint(); if (eh != null) H.Add(eh); }
            if (rf > 0) H.Add(new FarmerHint("tarla", $"Tarlada {rf} parsel hasada hazır, haydi toplayalım!", () => GoLocation("tarla")));
            foreach (var kv in S.units)
            {
                string u = kv.Key; var s = kv.Value;
                if (s != null && s.job != null && Now() >= s.job.e && B.ContainsKey(u) && B[u].recipes != null)
                {
                    string snd = GetOr(UNIT_SFX, u);
                    H.Add(new FarmerHint(u, $"{B[u].n} hazır! {(snd == "cluck" ? "Tavuklar gıdaklıyor." : snd == "moo" ? "İnekler sesleniyor." : "")}", () => GoLocation(LocOf(u))));
                    break;
                }
            }
            if (ro > 0) H.Add(new FarmerHint("han", $"{ro} sipariş teslime hazır. Kasabalılar bekliyor!", () => { if (Mode == "desk") DeskAction("ledger"); else GoLocation("han"); }));
            if ((double)Stored() / Cap() >= .9) H.Add(new FarmerHint("ambar", "Ambar dolmak üzere, pazara gidip biraz satalım.", () => GoLocation("pazar")));
            if (idle > 0 && wx == "rain") H.Add(new FarmerHint("yag", "Yağmur yağıyor, ekim için tam zamanı!", () => GoLocation("tarla")));
            else if (idle > 0) H.Add(new FarmerHint("bos", $"{idle} parsel boş duruyor. Bir şeyler ekelim mi?", () => GoLocation("tarla")));
            if (Has("yem") < 3 && Fun_HasUnit("kumes")) H.Add(new FarmerHint("yem", "Yem azaldı. Mısırdan öğütelim ya da pazardan alalım.", () => GoLocation("pazar")));
            foreach (var u in LOVE_U)
                if (Fun_HasUnit(u) && LoveOf(u).v < 40) { var uu = u; H.Add(new FarmerHint("sev" + u, $"{B[u].n} hayvanları biraz sevgi bekliyor.", () => { GoLocation(LocOf(uu)); })); break; }
            if (S.daily != null && !S.daily.login) H.Add(new FarmerHint("gun", "Günlük ödülün takvimde seni bekliyor!", () => ShowLogin()));
            var rc = S.recent;
            if (rc != null && Now() - rc.at < 120000 && Mode == "desk") H.Insert(0, new FarmerHint("yeni", $"Yeni {rc.n} pencereden ne güzel görünüyor!", null));
            var amb = new List<FarmerHint> {
                new FarmerHint("hava", wx == "storm" ? "Fırtına var, hayvanları içeride tutalım." : wx == "snow" ? "Kar yağıyor, sobayı yakmanın tam zamanı." : DayFactor() < .3 ? "Gece oldu, çiftlik uyuyor… ama işler beklemez!" : "Güzel bir gün, çiftlik seni bekliyor!", null),
                new FarmerHint("mev", $"{SEASONS[Season()].n} geldi, {new[] { "çiçekler açtı!", "güneş yakıyor!", "yapraklar sararıyor.", "her yer bembeyaz." }[Season()]}", null)
            };
            return H.Count > 0 ? H : new List<FarmerHint> { amb[(int)(Math.Floor(Now() / 20000) % 2)] };
        }

        public static void FarmerSay(bool force = false)
        {
            if (TutStep() >= 0 || Tut2Step() >= 0) { SayHide(); return; }
            if (UI.AnyModal || Mode == "interior" || (View != null && UI.W < 700)) { SayHide(); return; }
            if (!force && Now() - sayT < 9000 && !SayHidden) return;
            var H = FarmerHints(); var h = H[(int)(Math.Floor(Now() / 9000) % H.Count)]; sayT = Now(); sayAct = h.act;
            if (h.key != sayKey || SayHidden)
            {
                sayKey = h.key;
                Talk_Text.text = h.txt;
                Talk_Go.gameObject.SetActive(h.act != null);
                Talk_FitWidth();
                SayB.gameObject.SetActive(true);
                bubT = 0; PlaceBubble();
                // .farmer-say.in{animation:sayDrop .35s cubic-bezier(.2,1.4,.4,1)} from{opacity:0;translate:0 -10px}
                var cg = SayB.gameObject.GetOrAdd<CanvasGroup>();
                var bub = SayB;
                UI.Tween(.35f, k =>
                {
                    if (!bub) return;
                    float e = Talk_Ease(k);
                    cg.alpha = Mathf.Clamp01(e);
                    var p = bub.anchoredPosition; bub.anchoredPosition = new Vector2(p.x, -Talk_Top + (1 - e) * 10);
                });
            }
        }
        // cubic-bezier(.2,1.4,.4,1) yaklaşık: hafif taşan yumuşak çıkış
        static float Talk_Ease(float k) { float t = 1 - k; return 1 - t * t * t + 0.25f * Mathf.Sin(k * Mathf.PI) * t; }

        static float Talk_Top = 110;
        public static void PlaceBubble()
        {
            if (SayHidden) return;
            double t = PerfNow(); if (t - bubT < 250) return; bubT = t;
            var hudEl = Tut_Query(".hud");
            float hb = hudEl != null ? Tut_CssRect(hudEl).yMax : 102;
            float right = 12;
            var sh = Tut_Query("#sheet");
            if (sh != null && UI.W >= 700) { var r = Tut_CssRect(sh); if (r.yMin < hb + 90) right = Mathf.Max(12, UI.W - r.xMin + 12); }
            Talk_Top = RoundI(hb + 8);
            SayB.anchoredPosition = new Vector2(-RoundI(right), -Talk_Top);
            Talk_FitWidth();
        }
    }
}
