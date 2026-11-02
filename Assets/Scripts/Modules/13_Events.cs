// ================= OLAYLAR (13-events.js) =================
// document 'click' yetkisi → her data-act durumu RegisterAct ile kaydedilir (önce sfx('click')).
// data-act dışındaki tıklamalar da eylem olarak kaydedilir: "data-desk"(desk), "data-quick"(quick), "data-quickall", "data-locstep"(locstep),
// "data-go"(go), "data-region"(region), "data-open"(open). Css.Act ile kurulan düğmeler tıklanan öğenin konumunu "_x","_y" (CSS px) olarak ekler.
// 'change' → Ev_Change(id, işaretli), 'keydown' → her kare Input denetimi, renderHUD/commit, kart içi parsel kaydırma ve başlık kaydırma.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciftlik
{
    public static partial class Game
    {
        // document.body.classList (09-interiors'taki Int_BodyCls üzerinde)
        public static bool BodyHas(string cls) => Int_BodyHas(cls);
        public static void BodyClass(string cls, bool on) => Int_BodyToggle(cls, on);

        // Tıklanan öğenin orta-üst noktası (rc.left+rc.width/2, rc.top); yoksa imleç konumu
        static float Ev_X(Dictionary<string, string> d) { double v; return Arg(d, "_x") != null && double.TryParse(Arg(d, "_x"), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? (float)v : UI.ScreenToCss(Input.mousePosition).x; }
        static float Ev_Y(Dictionary<string, string> d) { double v; return Arg(d, "_y") != null && double.TryParse(Arg(d, "_y"), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? (float)v : UI.ScreenToCss(Input.mousePosition).y; }
        static Vector2? Ev_Pos(Dictionary<string, string> d) => Arg(d, "_x") != null ? (Vector2?)new Vector2(Ev_X(d), Ev_Y(d)) : null;

        // data-act: sfx('click') + durum
        static void Ev(string act, Action<Dictionary<string, string>> h) { RegisterAct(act, d => { Sfx("click"); h(d); }); }

        static void Init_13()
        {
            // ---- data-act dışı tıklamalar (switch'ten önceki closest() denetimleri) ----
            RegisterAct("data-desk", d => { Sfx("click"); DeskAction(Arg(d, "desk")); });
            RegisterAct("data-quick", d => QuickFeedback(QuickLoc(Arg(d, "quick")), Ev_Pos(d)));
            RegisterAct("data-quickall", d => QuickAll(Ev_Pos(d)));
            RegisterAct("data-locstep", d => StepLoc(ArgI(d, "locstep")));
            RegisterAct("data-go", d => { PLATFORM.Hap(); GoLocation(Arg(d, "go")); });
            RegisterAct("data-region", d => OpenRegion(Arg(d, "region")));
            RegisterAct("data-open", d => OpenView(Arg(d, "open")));

            // ---- switch(d.act) ----
            Ev("close", d => CloseSheet());
            Ev("back", d => { if (Mode == "interior") LeaveInterior(); else if (Mode == "focus") ToHub(); else ToDesk(); });
            Ev("home", d => ToDesk());
            Ev("proj", d => DoProj(Arg(d, "id")));
            Ev("gen", d => AskGen());
            Ev("quest", d => DoQuest(Arg(d, "n")));
            Ev("ach", d => ClaimAch(Arg(d, "id")));
            Ev("fs", d => { S.ui = S.ui ?? new UiPrefs(); S.ui.fs = Arg(d, "k"); ApplyUI(); RenderSheet(); Save(); });
            Ev("qpclose", d => CloseQpop());
            Ev("qpplant", d => { PlantAll(); RefreshQpop(); });
            Ev("keepN", d => { S.keepN = ArgI(d, "n"); RenderSheet(); Save(); });
            Ev("sellEx", d => SellExcess(S.keepN));
            Ev("renew", d => RenewWorker(Arg(d, "k")));
            Ev("guide", d => OpenView("guide"));
            Ev("enterIn", d => EnterInterior(Arg(d, "k")));
            Ev("build", d => BuildUnit(Arg(d, "u")));
            Ev("upU", d => UpgradeUnit(Arg(d, "u")));
            Ev("upHouse", d => UpgradeHouse());
            Ev("upFields", d => UpgradeFields());
            Ev("start", d => StartUnit(Arg(d, "u"), Arg(d, "r")));
            Ev("queue", d => QueueAdd(Arg(d, "u"), Arg(d, "r")));
            Ev("qdel", d => QueueDel(Arg(d, "u"), ArgI(d, "i")));
            Ev("collect", d => CollectUnit(Arg(d, "u"), Ev_X(d), Ev_Y(d)));
            Ev("collectU", d => CollectUnit(Arg(d, "id"), Ev_X(d), Ev_Y(d) - 20));
            Ev("harvestP", d => { HarvestPlot(ArgI(d, "id"), Ev_X(d), Ev_Y(d) - 20); Commit(); });
            Ev("seed", d => { S.lastCrop = Arg(d, "r"); RenderSheet(); RefreshQpop(); Save(); });
            Ev("plantAll", d => PlantAll());
            Ev("harvestAll", d => HarvestAll());
            Ev("plot", d =>
            {
                if (PanelSwipeUsed) { PanelSwipeUsed = false; return; }
                int i = ArgI(d, "i"); var f = i >= 0 && i < S.fields.Count ? S.fields[i] : null;
                if (f != null && f.job == null) { if (PlantPlot(i, S.lastCrop)) Commit(); }
                else if (f != null && Now() >= f.job.e) { HarvestPlot(i, Ev_X(d), Ev_Y(d)); Commit(); }
            });
            Ev("buyPlot", d => BuyPlot());
            Ev("region", d => BuyRegion(Arg(d, "r")));
            Ev("sell", d => Sell(Arg(d, "k"), ArgI(d, "n")));
            Ev("feed", d => BuyFeed(ArgI(d, "n")));
            Ev("order", d => FillOrder(ArgI(d, "i")));
            Ev("reorder", d => { int i = ArgI(d, "i"); if (i >= 0 && i < S.orders.Count) S.orders[i] = GenOrder(); else if (i >= S.orders.Count) S.orders.Add(GenOrder()); Commit(); });
            Ev("goal", d => ClaimGoal());
            Ev("reset", d => { View.confirm = true; RenderSheet(); });
            Ev("reset0", d => { View.confirm = false; RenderSheet(); });
            Ev("reset2", d => { S = Fresh(); RefreshMarket(true); EnsureOrders(); Save(); RegSig = ""; View = null; ToHub(); SyncWorld(true); RenderHUD(); Toast("Yeni bir başlangıç"); });
            Ev("iap", d => PLATFORM.Buy(Arg(d, "id")));
            Ev("restore", d => { var rc = Dom.Q("#rcode"); var inp = rc != null ? rc.GetComponent<InputField>() : null; RestoreAccount(inp != null ? inp.text : null); });
            Ev("copyCode", d =>
            {
                var c = RecoveryCode();
                try { GUIUtility.systemCopyBuffer = c; Toast("Kod kopyalandı"); }
                catch (Exception) { Toast("Kodu seçip kopyala"); }
            });
            Ev("gfx", d => { if (Arg(d, "k") != GFX.mode) { Save(); SetGfx(Arg(d, "k")); } });
            Ev("photo", d => PhotoMode(true));
            Ev("photoOff", d => PhotoMode(false));
            Ev("shoot", d => Shoot());
            Ev("rotL", d => RotateCam(Math.PI / 4));
            Ev("rotR", d => RotateCam(-Math.PI / 4));
            Ev("zin", d => ZoomCam(.75));
            Ev("zout", d => ZoomCam(1.33));
            Ev("adU", d => SpeedUnit(Arg(d, "u"), "ad"));
            Ev("gemU", d => SpeedUnit(Arg(d, "u"), "gem"));
            Ev("adF", d => SpeedFields("ad"));
            Ev("gemF", d => SpeedFields("gem"));
            Ev("mg", d => { var k = Arg(d, "k"); if (k == "milk") MgMilk(Arg(d, "u")); else if (k == "eggs") MgEggs(); else MgHaggle(); });
            Ev("hire", d => Hire(Arg(d, "k")));
            Ev("vtab", d => { View.tab = Arg(d, "t"); RenderSheet(); });
            Ev("contract", d => DeliverContract(ArgI(d, "i")));
            Ev("contest", d => EnterContest(Arg(d, "id")));
            Ev("stable", d => StableHorse());
            Ev("htrain", d => TrainHorse(ArgI(d, "i")));
            Ev("lrace", d => LeagueRace(ArgI(d, "i")));
            Ev("rnew", d => { Race = null; RenderSheet(); });
            Ev("decor", d => BuyDecor(Arg(d, "id")));
            Ev("adGem", d => WatchAd(() => { S.gems++; Toast("+1 🍀"); Sfx("coin"); Commit(); }));
            Ev("dclaim", d =>
            {
                int i = ArgI(d, "i"); var t = S.daily != null && i >= 0 && i < S.daily.tasks.Count ? S.daily.tasks[i] : null;
                if (t != null && t.p >= t.n && !t.c) { t.c = true; Gain(t.r, "Ödül"); AddXP(15); Sfx("coin"); FlyCoins(4); Commit(); }
            });
            Ev("dbonus", d => { if (S.daily != null && S.daily.tasks.All(t => t.c) && !S.daily.bonus) { S.daily.bonus = true; S.gems += 2; Sfx("win"); Commit(); } });
            Ev("albumDone", d => { if (Get(S.stats, "albumDone") == 0) { S.stats["albumDone"] = 1; S.gems += 50; Sfx("win"); Commit(); } });

            // 'change' olayının eylem karşılığı (başka modüllerin kurduğu onay kutuları için): Act("change", D("id","optAn","on","1"))
            RegisterAct("change", d => Ev_Change(Arg(d, "id"), Arg(d, "on") == "1" || Arg(d, "on") == "true"));

            // 'keydown'
            OnFrame += Ev_Keys;
            // body sınıflarını Dom.Body'ye yansıt (Dom.Q("body.photo") gibi sorgular için)
            Int_OnBody += Ev_SyncBody;
            UI.OnResize += () => { if (View != null) RenderSheet(); };
        }

        static void Ev_SyncBody()
        {
            var b = Dom.Body; if (b == null) return;
            foreach (var c in b.cls.ToList()) if (!Int_BodyCls.Contains(c)) b.Toggle(c, false);
            foreach (var c in Int_BodyCls.ToList()) b.Toggle(c, true);   // OnClass işleyicisi kümeyi değiştirebilir
            b.data["mode"] = Int_BodyMode;
        }

        // ---- document 'change' ----
        public static void Ev_Change(string id, bool on)
        {
            if (id == "optMusic") { S.sound.music = on; AudioInit(); ApplySound(); Save(); }
            if (id == "optSfx") { S.sound.sfx = on; Save(); }
            if (id == "optFast") { S.fastNav = on; Save(); }
            if (id == "optLeft") { S.ui = S.ui ?? new UiPrefs(); S.ui.left = on; ApplyUI(); Save(); }
            if (id == "optAutoSell") { S.autoSell = on; Save(); if (S.autoSell) AutoSellTick(); }
            if (id == "optAn")
            {
                S.consent = new Consent { analytics = on, extra = new Dictionary<string, JToken> { { "at", JToken.FromObject(DateNow()) } } }; Save();
                Toast(on ? "Anonim veri paylaşımı açık" : "Anonim veri paylaşımı kapalı");
            }
            if (id == "optNotif" && NATIVE)
            {
                if (on) PLATFORM.AskNotif(g => { S.sound.notif = g == true; if (g != true) Toast("Bildirim izni verilmedi"); Save(); RenderSheet(); });
                else { S.sound.notif = false; Save(); }
                return;
            }
            if (id == "optNotif")
            {
                // Tarayıcı Notification API'si bu ortamda yok: web sürümündeki catch dalı (e.target.checked=false)
                if (on) { Toast("Bildirimler bu ortamda desteklenmiyor"); RenderSheet(); }
                else { S.sound.notif = false; Save(); }
            }
        }

        // ---- document 'keydown' ----
        static void Ev_Keys(float dt)
        {
            if (S == null) return;
            if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.Escape)) // telefonda geri tuşunu 34-platform işler
            {
                if (Mode == "interior") LeaveInterior(); else if (Mode == "focus") ToHub(); else if (View != null) { View = null; RenderSheet(); } else if (Mode == "hub") ToDesk();
            }
            if (Mode == "hub" && !Ev_InputFocused())
            {
                if (Input.GetKeyDown(KeyCode.Q)) RotateCam(Math.PI / 4);
                if (Input.GetKeyDown(KeyCode.E)) RotateCam(-Math.PI / 4);
            }
        }
        static bool Ev_InputFocused()
        {
            var es = EventSystem.current; var g = es != null ? es.currentSelectedGameObject : null;
            return g != null && g.GetComponentInParent<InputField>() != null;
        }

        // ---- renderHUD ----
        public static double? LastCoins = null;
        public static void RenderHUD()
        {
            HudView.Ensure(); if (S == null) return;
            HudView.Coins.text = Fmt(S.coins);
            if (LastCoins != null && S.coins > LastCoins) UI.Boing(HudView.PCoin);
            LastCoins = S.coins;
            HudView.Lv.text = S.lv.ToString();
            if (HudView.LvFill != null) HudView.LvFill.fillAmount = (float)(Math.Min(100, S.xp / XpNeed(S.lv) * 100) / 100);
            Dom.Of(HudView.PLv).title = "Seviye " + S.lv + " · " + Fmt(S.xp) + "/" + Fmt(XpNeed(S.lv)) + " xp";
            HudView.Rep.text = S.rep.ToString("0.##", CultureInfo.InvariantCulture);
            HudView.Store.text = Stored() + "/" + Cap();
            bool full = (double)Stored() / Cap() >= .9; Dom.Of(HudView.PStore).Toggle("full", full); Css.SetBg(HudView.PStore, full ? UI.Hex("#fbe0d6") : UI.C.paper);
            HudView.Gems.text = S.gems.ToString();
            var se = SEASONS[Season()]; var wx = WEATHER[S.weather.k];
            HudView.Era.text = (se.e + " " + se.n + " · " + wx.e + " " + ClockStr()).ToUpperInvariant();
            Dom.Of(HudView.GoalDot).Hidden = !GoalReady() && !(S.daily != null && S.daily.tasks.Any(t => t.p >= t.n && !t.c));
        }
        public static void Commit(bool skipSheet = false)
        {
            Save(); RenderHUD(); SyncWorld(); if (!skipSheet) RenderSheet(); RefreshQpop();
            if (Interior != null && IntSig(Interior.key) != Interior.sig) Interior.refresh();
        }

        // ---- Kart içindeki parsel ızgarasında da sürükleyerek ekme/toplama ----
        public static bool PanelSwipeUsed = false;
        static In_Swipe _evPanelSwipe;
        internal static void Ev_PlotDown(int i, RectTransform b)
        {
            var k = SwipeKind(i); if (k == null) return;
            Swipe = new In_Swipe { kind = k, done = new HashSet<int>(), n = 0 }; _evPanelSwipe = Swipe; PanelSwipeUsed = true;
            var r = Dom.Of(b).Rect(); SwipeApply(i, r.x + r.width / 2, r.y);
        }
        internal static void Ev_PlotEnter(int i, RectTransform b)
        {
            if (Swipe == null || !ReferenceEquals(Swipe, _evPanelSwipe)) return;
            if (!Swipe.done.Contains(i)) { var r = Dom.Of(b).Rect(); SwipeApply(i, r.x + r.width / 2, r.y); }
        }
        internal static void Ev_PlotUp() { if (Swipe != null && ReferenceEquals(Swipe, _evPanelSwipe)) { _evPanelSwipe = null; SwipeEnd(); } }
        // ---- Başlıkta sağa-sola kaydırınca bir sonraki / önceki bölüme geç ----
        internal static bool Ev_HeadSwipeOk() => View != null && View.k == "loc";
    }

    public class Ev_PlotSwipe : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        public int i;
        public void OnPointerDown(PointerEventData e) { try { Game.Ev_PlotDown(i, (RectTransform)transform); } catch (Exception x) { Debug.LogException(x); } }
        public void OnPointerEnter(PointerEventData e) { if (e.eligibleForClick || Input.GetMouseButton(0) || Input.touchCount > 0) { try { Game.Ev_PlotEnter(i, (RectTransform)transform); } catch (Exception x) { Debug.LogException(x); } } }
        public void OnPointerUp(PointerEventData e) { try { Game.Ev_PlotUp(); } catch (Exception x) { Debug.LogException(x); } }
    }
    public class Ev_HeadSwipe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Vector2? hs;
        public void OnPointerDown(PointerEventData e) { hs = Game.Ev_HeadSwipeOk() ? (Vector2?)(e.position / UI.Scale) : null; }
        public void OnPointerUp(PointerEventData e)
        {
            if (hs == null) return; var p = e.position / UI.Scale; float dx = p.x - hs.Value.x, dy = p.y - hs.Value.y; hs = null;
            if (Mathf.Abs(dx) > 50 && Mathf.Abs(dx) > Mathf.Abs(dy) * 1.5f) Game.StepLoc(dx < 0 ? 1 : -1);
        }
    }
}
