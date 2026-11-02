// ================= YARDIMCILAR (03-helpers.js) =================
// $ (querySelector) Unity'de yok; fmt / TIME_OFF / now çekirdekte (Game.cs: Fmt, TIME_OFF, Now).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class ScaledDef { public Dictionary<string, int> @out; public double t; }

    public static partial class Game
    {
        public static int Has(string k) => Get(S.items, k);
        public static int Stored() { int a = 0; foreach (var kv in S.items) if (kv.Key != "at") a += kv.Value; return a; } // taylar ambarda değil ahırda durur
        public static bool Owned(string r) => Get(S.regions, r) != 0;
        static readonly int[] Hlp_CAP = { 80, 160, 260, 400 };
        public static int Cap() => Hlp_CAP[Mathf.Clamp(S.house - 1, 0, 3)] + (S.units.ContainsKey("silo") && S.units["silo"] != null ? 150 * S.units["silo"].l : 0);
        public static double XpNeed(int l) => Round(60 * Math.Pow(l, 1.55));
        public static readonly HashSet<string> TRACKED = new HashSet<string> { "built", "orders", "contracts", "contest", "race", "mg", "photo", "sold", "harvest_field", "iap" };
        public static void Stat(string k, double n = 1)
        {
            S.stats[k] = Get(S.stats, k) + n;
            if (TRACKED.Contains(k)) Track(k, new Dictionary<string, object> { { "n", n } });
            if (S.daily != null && S.daily.tasks != null)
                foreach (var t in S.daily.tasks)
                    if (t.k == k || (t.k == "plant" && k.StartsWith("plant_") && CROPS.Any(c => "plant_" + c.id == k))) t.p += n;
        }

        // JS sayı → metin (tam sayılar ondalıksız)
        public static string Hlp_N(double v) => v == Math.Floor(v) && Math.Abs(v) < 1e15 ? ((long)v).ToString(CultureInfo.InvariantCulture) : v.ToString("R", CultureInfo.InvariantCulture);

        public static string Secs(double s)
        {
            if (s >= 3600) { double m = Math.Floor(s % 3600 / 60); return Hlp_N(Math.Floor(s / 3600)) + " sa" + (m != 0 ? " " + Hlp_N(m) + " dk" : ""); }
            if (s >= 60) return Hlp_N(Math.Floor(s / 60)) + " dk" + (s % 60 != 0 ? " " + Hlp_N(s % 60).PadLeft(2, '0') + " sn" : "");
            return Hlp_N(s) + " sn";
        }

        // Seviye başına +%30 ürün ve %10 kısa süre (Sv5: 2,2 kat ürün, %60 süre). Yeni nesil bonusu (S.gen) tüm üretime eklenir.
        public static ScaledDef Scaled(Recipe r, int l)
        {
            double g = 1 + 0.1 * (S != null ? S.gen : 0); var o = new Dictionary<string, int>();
            foreach (var kv in r.@out) o[kv.Key] = (int)Math.Max(1, Round(kv.Value * (1 + 0.3 * (l - 1)) * g));
            return new ScaledDef { @out = o, t = Round(r.t * Math.Max(.6, 1 - 0.1 * (l - 1)) * (ProjOn("saat") ? .95 : 1)) };
        }
        public static bool CanPay(Recipe r)
        {
            if (r.cost > S.coins) return false;
            if (r.@in != null) foreach (var kv in r.@in) if (Has(kv.Key) < kv.Value) return false;
            return true;
        }
        public static double Prog(Job j) => Math.Min(1, (Now() - j.s) / (j.e - j.s));
        public static int StageOf(Job j) { var p = Prog(j); return p < .34 ? 1 : p < .67 ? 2 : 3; }
        public static Recipe UnitRecipe(string u) => B[u].recipes.Find(x => x.id == S.units[u].job.r);
        public static string SecShort(double s)
        {
            if (s <= 0) return "hazır";
            if (s < 60) return Hlp_N(s) + "sn";
            if (s < 3600) return Hlp_N(Math.Ceiling(s / 60)) + "dk";
            if (s < 86400) return Hlp_N(Math.Floor(s / 3600)) + "sa" + (s % 3600 >= 60 ? " " + Hlp_N(Math.Floor(s % 3600 / 60)) + "dk" : "");
            return Hlp_N(Math.Floor(s / 86400)) + "g";
        }
        // Mevsim ürünleri yalnızca kendi mevsiminde ekilir
        public static bool CropSeasonOk(Recipe c) => c.season == null || c.season.Contains(Season());
        public static Recipe CropOf(string id) => CROPS.Find(c => c.id == id);

        // .chip listesi: emoji + adet (+ "/eldeki"); eksikse kırmızı
        public static void Chips(Transform parent, Dictionary<string, int> obj, bool check, string cls = "")
        {
            foreach (var kv in obj)
            {
                var ch = UI.Chip(parent, ITEMS[kv.Key].e, kv.Value.ToString(), check && Has(kv.Key) < kv.Value, check ? Has(kv.Key).ToString() : null);
                if (!string.IsNullOrEmpty(cls)) ch.name = "chip " + cls;
            }
        }
        public static void Toast(string msg) => UI.Toast(msg);
        public static void FloatXY(float x, float y, string txt) => UI.Float(x, y, txt);

        public static void FinRoll()
        {
            var t = TodayStr();
            if (S.fin == null) S.fin = new Fin { day = "", inc = new Dictionary<string, double>(), exp = new Dictionary<string, double>(), hist = new List<FinDay>() };
            if (S.fin.day != t)
            {
                if (!string.IsNullOrEmpty(S.fin.day))
                {
                    if (S.fin.hist == null) S.fin.hist = new List<FinDay>();
                    S.fin.hist.Add(new FinDay { d = S.fin.day, i = S.fin.inc.Values.Sum(), e = S.fin.exp.Values.Sum() });
                    if (S.fin.hist.Count > 7) S.fin.hist = S.fin.hist.Skip(S.fin.hist.Count - 7).ToList();
                }
                S.fin.day = t; S.fin.inc = new Dictionary<string, double>(); S.fin.exp = new Dictionary<string, double>();
            }
        }
        public static void Pay(double n, string c = "Diğer") { S.coins -= n; if (n > 0) { FinRoll(); S.fin.exp[c] = Get(S.fin.exp, c) + n; } }
        public static void Gain(double n, string c = "Diğer") { S.coins += n; if (n > 0) { FinRoll(); S.fin.inc[c] = Get(S.fin.inc, c) + n; } }

        public static void AddXP(double n) { S.xp += n; while (S.xp >= XpNeed(S.lv)) { S.xp -= XpNeed(S.lv); S.lv++; LevelUp(); } }

        public static void LevelUp()
        {
            Track("level_up", new Dictionary<string, object> { { "lv", S.lv } });
            var all = B.Values.Where(b => b.lvl == S.lv).Select(b => b.n)
                .Concat(CROPS.Where(c => c.lvl == S.lv).Select(c => c.n))
                .Concat(REG.Values.Where(z => z.lvl == S.lv && z.cost != 0).Select(z => z.n + " arazisi")).ToList();
            S.gems += 2; Sfx("level");
            Hlp_Modal(S.lv.ToString(), "Seviye atladın!",
                (all.Count > 0 ? "Yeni açılanlar: <b>" + string.Join(", ", all) + "</b>" : "Çiftliğin büyüyor, böyle devam.") + "\n+2 🍀", "Harika", true);
        }

        // .modal kartı: rozet + başlık + metin + tek düğme. burst: seviye atlama arka planı (kırmızı-turuncu-krem halkalar), değilse .modal.login (koyu perde)
        public static RectTransform Hlp_Modal(string badge, string title, string body, string btn, bool burst)
        {
            RectTransform root = null;
            root = UI.Modal(card =>
            {
                var v = card.GetComponent<VerticalLayoutGroup>(); if (v != null) v.padding = UI.Pad(22, 22, 24, 18);
                var row = UI.HStack(card, 0, TextAnchor.MiddleCenter);
                var b = UI.Box(row, C_Honey, 40, 3, UI.C.ink, null, "badge"); UI.Size(b, 80, 80);
                bool digits = badge.All(char.IsDigit);
                var bt = UI.Txt(b, badge, 36, UI.Hex("#6b4100"), false, TextAnchor.MiddleCenter, false, digits); UI.Stretch(bt.rectTransform); UI.IgnoreLayout(bt);
                UI.Boing(b);
                UI.Txt(card, title, 30, UI.C.forest, false, TextAnchor.MiddleCenter, true, true);
                UI.Txt(card, body, 14, UI.Hex("#5d503c"), true, TextAnchor.MiddleCenter, true).lineSpacing = 1.5f;
                UI.Btn(card, btn, () => UI.Kill(root), "big gold");
            }, !burst, false, 320);
            if (burst) Hlp_Burst(root);
            return root;
        }
        static Color C_Honey => UI.C.honey;

        // repeating-radial-gradient(rgba(196,73,46,.88) 0 34px, rgba(232,140,60,.88) 34 68px, rgba(243,230,196,.92) 68 102px)
        static Texture2D _burstTex;
        static float _burstSpan;
        static void Hlp_Burst(RectTransform root)
        {
            float span = Mathf.Sqrt(UI.W * UI.W + UI.H * UI.H);
            if (_burstTex == null || Mathf.Abs(span - _burstSpan) > 1)
            {
                _burstSpan = span; const int N = 512; float k = N / span;
                var cols = new Color32[] { new Color(196 / 255f, 73 / 255f, 46 / 255f, .88f), new Color(232 / 255f, 140 / 255f, 60 / 255f, .88f), new Color(243 / 255f, 230 / 255f, 196 / 255f, .92f) };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        float d = Mathf.Sqrt((x + .5f - N / 2f) * (x + .5f - N / 2f) + (y + .5f - N / 2f) * (y + .5f - N / 2f)) / k;
                        px[y * N + x] = cols[((int)(d / 34)) % 3];
                    }
                if (_burstTex == null) _burstTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                _burstTex.SetPixels32(px); _burstTex.Apply();
            }
            var bg = root.GetComponent<Image>(); if (bg != null) bg.color = UI.C.clear;
            var ri = UI.Picture(root, _burstTex, span, span); ri.raycastTarget = true;
            var rt = ri.rectTransform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(span, span);
            rt.SetAsFirstSibling();
        }
    }
}
