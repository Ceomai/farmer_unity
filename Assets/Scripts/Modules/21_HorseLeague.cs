// ================= AT LİGİ (21-horse-league.js) =================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class TierDef { public string n; public double fee, prize; public int g; public double opp; }

    // Süren/biten lig yarışı (JS: race={horses:[{n,own,sp,pos,place}],running,done,msg}). Küresel "race" değişkeni 12-panels'te (Race) tutulur.
    public class RaceHorse { public string n; public int own; public double sp, pos; public int place; }
    public class RaceState { public List<RaceHorse> horses = new List<RaceHorse>(); public bool running, done; public string msg; }

    public static partial class Game
    {
        public static readonly List<TierDef> TIERS = new List<TierDef> {
            new TierDef { n = "Köy Ligi", fee = 100, prize = 400, g = 1, opp = 10 },
            new TierDef { n = "İl Ligi", fee = 300, prize = 1200, g = 2, opp = 14 },
            new TierDef { n = "Bölge Ligi", fee = 800, prize = 3000, g = 4, opp = 18 },
            new TierDef { n = "Ulusal Lig", fee = 2000, prize = 8000, g = 8, opp = 23 },
        };

        // Şeritteki at simgeleri (JS: #lane{j} .h) — RaceBody her çizimde yeniler
        static readonly List<RectTransform> _g2LaneH = new List<RectTransform>();
        static readonly List<Text> _g2LaneNm = new List<Text>();

        public static void StableHorse()
        {
            if (Has("at") == 0) return;
            if (S.horses.Count >= 4) { UI.Toast("Ahırda en fazla 4 at olabilir"); return; }
            S.items["at"]--;
            int hl = LvOf("hara"); if (hl == 0) hl = 1;
            S.horses.Add(new Horse { n = HNAMES[FloorI(Rnd() * HNAMES.Count)], spd = 4 + hl + FloorI(Rnd() * 4), sta = 4 + hl + FloorI(Rnd() * 4), w = 0, rest = 0, train = 0 });
            Sfx("neigh"); UI.Toast("🐴 Yeni at ahırda!"); Commit();
        }

        public static void TrainHorse(int i)
        {
            if (i < 0 || i >= S.horses.Count) return;
            var h = S.horses[i]; if (h == null || Now() < h.train) return;
            if (Has("yem") < 4) { UI.Toast("Antrenman için 4 yem gerekli"); return; }
            S.items["yem"] -= 4;
            bool spd = Rnd() < .5;
            if (spd) h.spd = Math.Min(20, h.spd + 1); else h.sta = Math.Min(20, h.sta + 1);
            h.train = Now() + 300000;
            Sfx("neigh"); UI.Toast($"{h.n}: {(spd ? "Hız" : "Dayanıklılık")} +1"); Commit();
        }

        public static void LeagueRace(int i)
        {
            var h = i >= 0 && i < S.horses.Count ? S.horses[i] : null;
            var T = TIERS[S.league.tier];
            if (h == null || Now() < h.rest || (Race != null && Race.running)) return;
            if (S.coins < T.fee) { UI.Toast("Giriş ücreti için paran yok"); return; }
            Pay(T.fee, "Yarış");
            double power = h.spd + h.sta * .6 + (Has1("arap") ? 2 : 0);
            var field = new List<KeyValuePair<RaceHorse, double>> { new KeyValuePair<RaceHorse, double>(new RaceHorse { n = h.n, own = 1 }, power) };
            for (int k = 0; k < 4; k++)
                field.Add(new KeyValuePair<RaceHorse, double>(new RaceHorse { n = HNAMES[(k * 3 + S.league.pts + 7 + i) % HNAMES.Count] }, T.opp * (.85 + Rnd() * .35)));
            var rc = new RaceState { running = true };
            foreach (var x in G2_Shuffle(field)) { x.Key.sp = .9 + x.Value / 30; x.Key.pos = 0; rc.horses.Add(x.Key); }
            Race = rc; h.rest = Now() + 180000; RenderSheet(); Sfx("neigh");
            Action tk = null;
            tk = () =>
            {
                var race = rc;
                int fin = race.horses.Count(x => x.place != 0);
                for (int j = 0; j < race.horses.Count; j++)
                {
                    var x = race.horses[j];
                    if (x.place == 0) { x.pos = Math.Min(90, x.pos + x.sp * (.7 + Rnd() * .9)); if (x.pos >= 90) x.place = ++fin; }
                    if (Race == rc && j < _g2LaneH.Count && _g2LaneH[j] != null) G2_SetLanePos(_g2LaneH[j], x.pos);
                }
                if (race.horses.Any(x => x.place == 0)) { SetTimeout(tk, 90); return; }
                race.running = false; race.done = true;
                var me = race.horses.Find(x => x.own == 1); int pl = me.place;
                int pts = new[] { 0, 3, 2, 1, 0, 0 }[pl];
                S.league.pts += pts;
                string msg = $"{me.n} {pl}. oldu · +{pts} puan";
                double tri = ProjOn("tribun") ? 1.5 : 1;
                if (pl == 1) { Gain(Round(T.prize * tri), "Yarış"); h.w++; msg += $" · +₺{Fmt(Round(T.prize * tri))}"; Sfx("win"); FlyCoins(8); }
                else if (pl <= 3) { var p2 = Round(T.prize * (pl == 2 ? .4 : .15) * tri); Gain(p2, "Yarış"); msg += $" · +₺{Fmt(p2)}"; Sfx("coin"); }
                else Sfx("fail");
                if (S.league.pts >= 9 && S.league.tier < TIERS.Count - 1) { S.league.tier++; S.league.pts = 0; S.gems += T.g; msg += $" · 🎉 {TIERS[S.league.tier].n}'ne yükseldin! +🍀{T.g}"; }
                Stat("race"); AddXP(pl == 1 ? 50 : 15); race.msg = msg; Commit();
            };
            SetTimeout(tk, 300);
        }

        static void G2_SetLanePos(RectTransform hrt, double pos)
        {
            if (hrt == null) return;
            hrt.anchorMin = hrt.anchorMax = new Vector2((float)(pos / 100), 1);
        }

        static Texture2D _g2LaneTex, _g2FinishTex;
        static Texture2D G2_Stripes(Color a, Color b, int half, bool vertical)
        {
            var tex = new Texture2D(vertical ? 1 : half * 2, vertical ? half * 2 : 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var px = new Color[half * 2];
            for (int i = 0; i < half * 2; i++) px[i] = i < half ? a : b;
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        public static void RaceBody(Transform parent)
        {
            var T = TIERS[S.league.tier];
            var unit = G2_Unit(parent);
            G2_Uhead(unit, "🏆 " + T.n, $"Puan {S.league.pts}/9");
            G2_Pbar(unit, S.league.pts / 9f);
            _g2LaneH.Clear(); _g2LaneNm.Clear();
            if (Race != null && (Race.running || Race.done))
            {
                // .track
                var track = UI.Box(unit, UI.Hex("#4f7a35"), 14, 2, UI.C.ink, null, "track");
                G2_Gradient(track.Find("bg"), UI.Hex("#4f7a35"), UI.Hex("#3f6a2a"), false);
                var tv = track.gameObject.AddComponent<VerticalLayoutGroup>();
                tv.padding = UI.Pad(7); tv.spacing = 4; tv.childControlWidth = tv.childControlHeight = true; tv.childForceExpandWidth = true; tv.childForceExpandHeight = false;
                if (_g2LaneTex == null) _g2LaneTex = G2_Stripes(UI.Hex("#b88a55"), UI.Hex("#ad7f4a"), 22, false);
                if (_g2FinishTex == null) _g2FinishTex = G2_Stripes(Color.white, UI.Hex("#222222"), 4, true);
                for (int i = 0; i < Race.horses.Count; i++)
                {
                    var x = Race.horses[i];
                    var lane = UI.Box(track, UI.Hex("#b88a55"), 7, 0, null, null, "lane" + i);
                    UI.Size(lane, -1, 21);
                    if (x.own == 1) { var ol = lane.gameObject.AddComponent<Outline>(); ol.effectColor = UI.C.honey; ol.effectDistance = new Vector2(2, -2); }
                    var strip = UI.New("stripes", lane); UI.Stretch(strip, 2, 0, 2, 0);
                    var sri = strip.gameObject.AddComponent<RawImage>(); sri.texture = _g2LaneTex; sri.raycastTarget = false; strip.gameObject.AddComponent<G2Tiled>().period = 44;
                    var fin = UI.New("finish", lane); fin.anchorMin = new Vector2(1, 0); fin.anchorMax = new Vector2(1, 1); fin.pivot = new Vector2(1, .5f); fin.sizeDelta = new Vector2(4, 0); fin.anchoredPosition = Vector2.zero;
                    var fri = fin.gameObject.AddComponent<RawImage>(); fri.texture = _g2FinishTex; fri.raycastTarget = false; var ft = fin.gameObject.AddComponent<G2Tiled>(); ft.period = 8; ft.vertical = true;
                    var nm = UI.Txt(lane, UI.Esc(x.n) + (x.place != 0 ? " · " + x.place + "." : ""), 10.5f, UI.Hex("#3a2410"), true, TextAnchor.MiddleRight, false);
                    var nrt = nm.rectTransform; nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(1, 1); nrt.offsetMin = new Vector2(0, 0); nrt.offsetMax = new Vector2(-8, -1);
                    var ht = UI.Txt(lane, "🐎", 16, null, false, TextAnchor.UpperLeft, false);
                    var hrt = ht.rectTransform; hrt.pivot = new Vector2(1, 1); hrt.sizeDelta = new Vector2(22, 21);
                    hrt.localScale = new Vector3(-1, 1, 1);   // transform:scaleX(-1)
                    hrt.anchoredPosition = Vector2.zero;
                    G2_SetLanePos(hrt, x.pos);
                    _g2LaneH.Add(hrt); _g2LaneNm.Add(nm);
                }
                if (Race.done)
                {
                    var log = UI.Box(unit, UI.Hex("#1a1924"), 12, 2, UI.C.ink, null, "log");
                    var lv = log.gameObject.AddComponent<VerticalLayoutGroup>(); lv.padding = UI.Pad(9, 11); lv.childControlWidth = lv.childControlHeight = true; lv.childForceExpandWidth = true;
                    var lt = UI.Txt(log, UI.Esc(Race.msg), 12.5f, UI.Hex("#ece6d6"), false); lt.lineSpacing = 1.3f;
                    UI.ActBtn(unit, "Tamam", "rnew", "big");
                }
                return;
            }
            if (Has("at") > 0) G2_PillAct(unit, $"🐴 Tayı ahıra al ({Has("at")})", "stable", "go", true);
            if (S.horses.Count > 0)
            {
                for (int i = 0; i < S.horses.Count; i++)
                {
                    var x = S.horses[i];
                    bool rest = Now() < x.rest, tr = Now() < x.train;
                    var job = G2_Job(unit);
                    G2_Big(job, "🐎");
                    var pw = G2_Pw(job);
                    G2_PwB(pw, $"{UI.Esc(x.n)} · {x.w} galibiyet");
                    G2_PwSmall(pw, $"⚡ Hız {x.spd} · 🫀 Dayanıklılık {x.sta}");
                    if (rest) { var lt = G2_Left(pw, x.rest, "💤", 11.5f); }
                    var colm = UI.VStack(job, 5, null, TextAnchor.MiddleCenter, "btns");
                    colm.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
                    G2_PillAct(colm, "🏁 ₺" + G2_Num(T.fee), "lrace", "gold", !(rest || S.coins < T.fee), "i", i.ToString());
                    // title="4 yem"
                    G2_PillAct(colm, "🏋️ Antrenman", "htrain", "", !(tr || Has("yem") < 4), "i", i.ToString());
                }
            }
            else G2_Note(unit, "Harada tay yetiştir, ahıra al ve lige kaydet. İlk üçe girmek puan kazandırır, 9 puan bir üst lig demek.");
        }

        static void Init_21()
        {
            // Bu eylemler 13_Events.cs'te de (JS'teki gibi önce sfx('click') çalarak) kaydedilir; burada yeniden kaydetmek
            // sonra çalışan bu Init ile tıklama sesini düşürüyordu. Tek kaynak 13_Events.

            // RegisterAct("stable", d => StableHorse());
            // RegisterAct("htrain", d => TrainHorse(ArgI(d, "i", -1)));
            // RegisterAct("lrace", d => LeagueRace(ArgI(d, "i", -1)));
            // RegisterAct("rnew", d => { Race = null; RenderSheet(); });
        }
    }
}
