// ================= MİNİ OYUNLAR (22-minigames.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    // mgOpen'ın döndürdüğü nesne: {d (modal), body (.mgb), onClose}
    public class MgBox
    {
        public RectTransform d, card, body;
        public Button x;     // .mgx (Vazgeç)
        public Action onClose;
    }

    public static partial class Game
    {
        public static MgBox MgOpen(string title, string desc)
        {
            RectTransform card;
            var d = G2_Modal("mg", out card);
            G2_H2(card, title, 23);
            G2_Note(card, desc, TextAnchor.UpperCenter);
            var body = UI.VStack(card, 6, UI.Pad(0, 0, 6, 6), TextAnchor.UpperCenter, "mgb");
            var o = new MgBox { d = d, card = card, body = body };
            var xw = UI.HStack(card, 0, TextAnchor.MiddleCenter, null, "mgxwrap");
            o.x = UI.Btn(xw, "Vazgeç", () => { o.onClose?.Invoke(); UI.Kill(d); }, "ghost");
            return o;
        }

        public static void MgEnd(MgBox o, string msg)
        {
            o.onClose?.Invoke(); o.onClose = null;
            UI.Clear(o.body);
            G2_MgFace(o.body, "🎉");
            UI.Txt(o.body, msg, 14, UI.C.text, true, TextAnchor.MiddleCenter, true);
            G2_BigBtn(o.body, "Harika", () => UI.Kill(o.d));
            if (o.x != null) UI.Kill(o.x.transform.parent);
            o.x = null;
        }

        // Kare döngüsü aboneliği (requestAnimationFrame karşılığı); dönen eylem aboneliği iptal eder
        static Action G2_Raf(Action<float> loop)
        {
            Action<float> h = null; bool dead = false;
            h = dt => { if (dead) return; loop(dt); };
            OnFrame += h;
            return () => { dead = true; OnFrame -= h; };
        }

        public static void MgMilk(string u)
        {
            Unit s; if (u == null || !S.units.TryGetValue(u, out s) || s == null || s.job == null || Now() >= s.job.e || s.job.mg != 0) return;
            var o = MgOpen("🥛 Sağım ritmi", "İbre yeşil bölgedeyken SAĞ! düğmesine bas. 6 deneme.");
            // .mgbar
            var bar = UI.Box(o.body, G2_PbarBg, 15, 2, UI.C.ink, null, "mgbar");
            UI.Size(bar, -1, 30, 1);
            var area = UI.New("area", bar); UI.Stretch(area, 2, 2, 2, 2); area.gameObject.AddComponent<RectMask2D>(); UI.IgnoreLayout(area);
            var z = UI.Fill(area, UI.C.leaf, 0, "zone").rectTransform;
            var nd = UI.Fill(area, UI.C.ink, 0, "needle").rectTransform;
            nd.pivot = new Vector2(.5f, .5f); nd.sizeDelta = new Vector2(6, 0);   // width:6px; margin-left:-3px
            var sc = G2_MgScore(o.body, "0 / 6");
            double pos = 0, dir = 1, sp = 70, zone = 40; int hits = 0, tries = 0; bool over = false;
            Action setZone = () =>
            {
                zone = 10 + Rnd() * 68;
                z.anchorMin = new Vector2((float)(zone / 100), 0); z.anchorMax = new Vector2((float)((zone + 18) / 100), 1); z.offsetMin = z.offsetMax = Vector2.zero;
            };
            setZone();
            Action<double> setNeedle = p => { nd.anchorMin = new Vector2((float)(p / 100), 0); nd.anchorMax = new Vector2((float)(p / 100), 1); nd.anchoredPosition = Vector2.zero; nd.sizeDelta = new Vector2(6, 0); };
            setNeedle(0);
            var cancel = G2_Raf(rdt =>
            {
                if (!nd) return;
                double dt = Math.Min(.05, rdt);
                pos += dir * sp * dt;
                if (pos > 100) { pos = 100; dir = -1; }
                if (pos < 0) { pos = 0; dir = 1; }
                setNeedle(pos);
            });
            o.onClose = cancel;
            G2_BigBtn(o.body, "SAĞ!", () =>
            {
                if (over) return;
                tries++;
                bool ok = pos >= zone && pos <= zone + 18;
                if (ok) { hits++; Sfx("moo"); } else Sfx("fail");
                sc.text = hits + " / 6"; sp += 14; setZone();
                if (tries >= 6)
                {
                    over = true;
                    if (s.job != null)
                    {
                        double cut = Math.Min(.5, hits * .08), left = s.job.e - Now();
                        s.job.e -= Math.Max(0, left * cut); s.job.mg = 1;
                        int bonus = (hits >= 5 ? 1 : 0) + (Has1("nazar") && hits >= 3 ? 1 : 0);
                        if (bonus != 0) S.items["sut"] = Has("sut") + bonus;
                        Stat("mg");
                        MgEnd(o, $"{hits} isabet! Süre %{Round(cut * 100)} kısaldı{(bonus != 0 ? " · +" + bonus + " 🥛" : "")}");
                        Commit();
                    }
                    else MgEnd(o, $"{hits} isabet!");
                }
            });
        }

        class G2Egg { public RectTransform el; public double x, y, v; public bool g, done; }

        public static void MgEggs()
        {
            Unit s; if (!S.units.TryGetValue("kumes", out s) || s == null || s.job == null || Now() >= s.job.e || s.job.mg != 0) return;
            var o = MgOpen("🥚 Yumurta yakala", "Sepeti sürükle, düşen yumurtaları yakala! 15 saniye.");
            // .mgfield
            var f = UI.Box(o.body, UI.Hex("#cfe6f0"), 14, 2, UI.C.ink, null, "mgfield");
            UI.Size(f, -1, 250, 1);
            var fin = f.Find("bg"); fin.gameObject.AddComponent<RectMask2D>();
            G2_Gradient(fin, UI.Hex("#cfe6f0"), UI.Hex("#efe0b6"), false);
            var playRt = (RectTransform)fin;
            var bk = UI.Txt(fin, "🧺", 42, null, false, TextAnchor.LowerCenter, false).rectTransform;
            bk.anchorMin = bk.anchorMax = new Vector2(.5f, 0); bk.pivot = new Vector2(.5f, 0); bk.sizeDelta = new Vector2(60, 50); bk.anchoredPosition = new Vector2(0, 2);
            var sc = G2_MgScore(o.body, "0");
            double bx = 50; int got = 0, gold = 0; var eggs = new List<G2Egg>();
            double tEnd = PerfNow() + 15000, spawn = 0; bool finished = false;
            Vector3 lastMouse = Input.mousePosition;
            Action<Vector2> mv = screen =>
            {
                Vector2 lp;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(playRt, screen, null, out lp)) return;
                var r = playRt.rect;
                if (lp.x < r.xMin || lp.x > r.xMax || lp.y < r.yMin || lp.y > r.yMax) return;   // yalnızca alanın üstündeyken
                bx = Math.Max(6, Math.Min(94, (lp.x - r.xMin) / r.width * 100));
            };
            Action cancel = null;
            cancel = G2_Raf(rdt =>
            {
                if (!fin || finished) return;
                double dt = Math.Min(.05, rdt);
                // pointermove / pointerdown
                if (Input.touchCount > 0) mv(Input.GetTouch(0).position);
                else if (Input.mousePosition != lastMouse || Input.GetMouseButtonDown(0)) { lastMouse = Input.mousePosition; mv(lastMouse); }
                spawn -= dt;
                if (spawn <= 0)
                {
                    spawn = .4 + Rnd() * .4;
                    bool g = Rnd() < .06;
                    var el = UI.Txt(fin, g ? "🌟" : "🥚", 24, null, false, TextAnchor.UpperCenter, false).rectTransform;
                    el.pivot = new Vector2(.5f, 1); el.sizeDelta = new Vector2(34, 32);   // transform:translateX(-50%)
                    double x = 6 + Rnd() * 88;
                    eggs.Add(new G2Egg { el = el, x = x, y = -10, v = 30 + Rnd() * 28, g = g });
                }
                bk.anchorMin = bk.anchorMax = new Vector2((float)(bx / 100), 0);
                float H = playRt.rect.height;
                foreach (var e in eggs)
                {
                    e.y += e.v * dt;
                    if (e.el) { e.el.anchorMin = e.el.anchorMax = new Vector2((float)(e.x / 100), 1); e.el.anchoredPosition = new Vector2(0, -(float)(e.y / 100 * H)); }
                    if (!e.done && e.y > 80 && e.y < 96 && Math.Abs(e.x - bx) < 10)
                    {
                        e.done = true; UI.Kill(e.el); if (e.g) gold++; else got++; Sfx("pop");
                        sc.text = got + (gold != 0 ? " · 🌟" + gold : "");
                    }
                    else if (!e.done && e.y > 102) { e.done = true; UI.Kill(e.el); }
                }
                eggs.RemoveAll(e => e.done);
                if (PerfNow() < tEnd) return;
                finished = true; cancel();
                int bonus = Math.Min(6, FloorI(got / 4.0) + (Has1("nazar") ? 1 : 0));
                S.items["yumurta"] = Has("yumurta") + bonus;
                if (s.job != null) s.job.mg = 1;
                if (gold != 0 && Get(S.album, "altin_yumurta") == 0 && Rnd() < .5) { S.album["altin_yumurta"] = 1; S.gems += 1; SetTimeout(() => ShowFind(COL[0]), 400); }
                Stat("mg");
                MgEnd(o, $"{got} yumurta yakaladın! +{bonus} 🥚");
                Commit();
            });
            o.onClose = cancel;
        }

        public static void MgHaggle()
        {
            if (Now() < S.haggleAt) { UI.Toast("Tüccar biraz dinleniyor, birazdan gel"); return; }
            var inv = ITEMS.Keys.Where(k => Has(k) > 0 && k != "at" && k != "yem").ToList();
            if (inv.Count == 0) { UI.Toast("Satacak ürünün yok"); return; }
            var k0 = inv.OrderByDescending(a => ITEMS[a].p * Has(a)).First();
            int qty = Math.Min(Has(k0), 5);
            double bse = SellPrice(k0);
            var o = MgOpen("🤝 Pazarlık", $"Tüccar {qty} {ITEMS[k0].e} {ITEMS[k0].n} için tanesine ₺{G2_Num(bse)} veriyor.");
            int pat = 3;
            Action<string> draw = null;
            draw = msg =>
            {
                UI.Clear(o.body);
                G2_MgFace(o.body, "🧔");
                G2_Note(o.body, msg, TextAnchor.UpperCenter);
                var r1 = G2_Urow(o.body, TextAnchor.MiddleCenter);
                var offers = new[] { new KeyValuePair<double, string>(1.15, "+%15"), new KeyValuePair<double, string>(1.3, "+%30"), new KeyValuePair<double, string>(1.6, "+%60") };
                Action<double> pick = null;
                foreach (var of in offers) { var m = of.Key; G2_Pill(r1, $"{of.Value} · ₺{G2_Num(Round(bse * m))}", () => pick(m), "gold"); }
                var r2 = G2_Urow(o.body, TextAnchor.MiddleCenter);
                r2.GetComponent<HorizontalLayoutGroup>().padding = UI.Pad(0, 0, 8, 0);
                G2_Pill(r2, $"Kabul et · ₺{G2_Num(bse)}", () => pick(1));
                G2_MgScore(o.body, "Sabır " + string.Concat(Enumerable.Repeat("😐", pat)));
                pick = m =>
                {
                    double acc = m == 1 ? 1 : Math.Max(.1, 1 - (m - 1) * 1.5 - (3 - pat) * .1 + (Has1("nazar") ? .1 : 0));
                    if (Rnd() < acc)
                    {
                        int n = Math.Min(qty, Has(k0));
                        S.items[k0] -= n; AddSat(k0, n);
                        double earn = Round(bse * m) * n;
                        Gain(earn, "Pazar"); Stat("sold", n); Stat("mg"); S.haggleAt = Now() + 180000;
                        Sfx("coin"); FlyCoins(6);
                        MgEnd(o, $"Anlaştınız! {n} {ITEMS[k0].n} → +₺{Fmt(earn)}");
                        Commit();
                    }
                    else
                    {
                        pat--; Sfx("fail");
                        if (pat <= 0) { S.haggleAt = Now() + 180000; Stat("mg"); MgEnd(o, "Tüccar sinirlendi ve gitti 😤"); Save(); }
                        else draw(new[] { "", "\"Son teklifim bu!\"", "\"Biraz in bakalım…\"" }[pat]);
                    }
                };
            };
            draw("Teklifini seç:");
        }

        static void Init_22()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("mg", d =>
            // {
                // var k = Arg(d, "k");
                // if (k == "milk") MgMilk(Arg(d, "u")); else if (k == "eggs") MgEggs(); else MgHaggle();
            // });
        }
    }

    // İşin mini oyun işareti (job.mg)
    public partial class Job { [Newtonsoft.Json.JsonProperty(DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)] public int mg; }
}
