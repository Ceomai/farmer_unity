// ================= HUD / SAYFA İSKELETİ (index.template.html + game.css düzeni) — G6 =================
// #boot, .rainfx, #grain, #vignette, header.hud (.plate + .pills), #backBtn, #guideBtn, nav.strip, nav.dock, .cam, .shutter, .photo-x, aside.sheet, #iris
// Her öğe Dom etiketi taşır (Dom.Q("#coins") gibi bulunabilir). Görünürlük/konum kuralları game.css'teki body sınıflarına göre
// her karede HudView.Layout ile uygulanır (telefon ≤700px, masaüstü; hub/focus/inside/desk/photo/sheet-open/lefty/fs-b/fs-xb).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public static class HudView
    {
        public static RectTransform Fx, Header, Plate, Pills, PCoin, PGem, PLv, PRep, PStore, Back, Guide, Dock, Cam, CamPh, Shutter, PhotoX;
        public static RectTransform StripRoot, StripContent, SheetRoot, Boot, RainFx, Grain, Vignette, GoalDot;
        public static ScrollRect StripScroll;
        public static Text Title, Era, Coins, Gems, Lv, Rep, Store, BackTxt;
        public static Image LvFill;
        public static readonly Dictionary<string, RectTransform> DockBtns = new Dictionary<string, RectTransform>();
        static bool _built, _phoneBuilt;
        static string _sig;

        public static bool Phone => UI.W <= 700;                 // @media (max-width:700px)
        public static bool ShortLandscape => UI.H <= 500 && UI.W > UI.H;
        public static float Zoom { get { var u = Game.S != null ? Game.S.ui : null; return u == null ? 1 : u.fs == "b" ? 1.12f : u.fs == "xb" ? 1.25f : 1; } }
        // env(safe-area-inset-*) (CSS px)
        public static float SafeL => Screen.safeArea.x / UI.Scale;
        public static float SafeR => (Screen.width - Screen.safeArea.xMax) / UI.Scale;
        public static float SafeT => (Screen.height - Screen.safeArea.yMax) / UI.Scale;
        public static float SafeB => Screen.safeArea.y / UI.Scale;

        public static void Ensure()
        {
            if (_built || UI.Root == null) return;
            _built = true;
            Dom.Body.id = "body";
            // Arka katman: yağmur, gren, kenar karartması (z-index:1, haritadaki etiketlerin altında)
            Fx = UI.New("Fx", UI.Root); UI.Stretch(Fx); Fx.SetAsFirstSibling();
            BuildRain(); BuildGrain(); BuildVignette();
            BuildHeader();
            BuildBack(); BuildGuide();
            BuildStrip(); BuildDock(); BuildCam(); BuildPhoto();
            BuildSheet(); BuildBoot();
            UI.Root.gameObject.AddComponent<HudDriver>();
        }

        // ---------- Yardımcılar ----------
        static Text Label(Transform p, string s, float size, Color c, bool display = false) => Css.T(p, s, size, c, true, TextAnchor.MiddleCenter, false, display);
        static RectTransform RoundBtn(Transform p, Color bg, float radius, float bw, Vector2? shadow, string name)
        {
            var b = Css.Box(p, bg, radius, bw, Css.Ink, shadow, Css.Ink, name);
            return b;
        }
        static Image Circle(Transform p, float size, Color c, string name = "circle")
        {
            var i = UI.New(name, p).gameObject.AddComponent<Image>(); i.sprite = Css.CircleSprite; i.color = c; i.raycastTarget = false;
            UI.Size(i, size, size).minWidth = size; i.rectTransform.sizeDelta = new Vector2(size, size); return i;
        }
        static Texture2D Tex(int w, int h, Func<int, int, Color32> f, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear };
            var a = new Color32[w * h]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) a[y * w + x] = f(x, y);
            t.SetPixels32(a); t.Apply(); return t;
        }

        // ---------- .rainfx ----------
        static RawImage _rainA, _rainB;
        static void BuildRain()
        {
            RainFx = UI.New("rainfx", Fx); UI.Stretch(RainFx); RainFx.gameObject.SetActive(false);
            var rnd = new System.Random(7);
            var streaks = new List<int[]>(); for (int i = 0; i < 26; i++) streaks.Add(new[] { rnd.Next(180), rnd.Next(180), 10 + rnd.Next(14) });
            var tex = Tex(180, 180, (x, y) =>
            {
                foreach (var s in streaks) { int dx = x - s[0]; int dy = (y - s[1] + 180) % 180; if (Math.Abs(dx) <= 0 && dy < s[2]) return new Color32(220, 232, 245, (byte)(150 * (1 - dy / (float)s[2]) + 60)); }
                return new Color32(0, 0, 0, 0);
            }, TextureWrapMode.Repeat);
            _rainA = MakeTiled(RainFx, tex, "i"); _rainB = MakeTiled(RainFx, tex, "i"); _rainB.color = new Color(1, 1, 1, .55f);
            var b = _rainB.rectTransform; b.offsetMin = new Vector2(37, 0);
            Dom.Tag(RainFx, ".rainfx"); Dom.Tag(_rainA, ""); Dom.Tag(_rainB, "");
        }
        static RawImage MakeTiled(Transform p, Texture t, string n)
        {
            var rt = UI.New(n, p); UI.Stretch(rt); var ri = rt.gameObject.AddComponent<RawImage>(); ri.texture = t; ri.raycastTarget = false; return ri;
        }
        // ---------- #grain / #vignette ----------
        static RawImage _grain;
        static void BuildGrain()
        {
            var rnd = new System.Random(3);
            var tex = Tex(220, 220, (x, y) => new Color32((byte)(.35f * 255), (byte)(.25f * 255), (byte)(.15f * 255), (byte)(rnd.NextDouble() * .9 * 255)), TextureWrapMode.Repeat);
            _grain = MakeTiled(Fx, tex, "grain"); _grain.color = new Color(1, 1, 1, .15f); Grain = _grain.rectTransform;
            Dom.Tag(Grain, "#grain");
        }
        static void BuildVignette()
        {
            var tex = Tex(128, 128, (x, y) =>
            {
                float u = (x + .5f) / 128f, v = 1 - (y + .5f) / 128f;
                float d = Mathf.Sqrt(Mathf.Pow((u - .5f) / (.5f * 1.4142f), 2) + Mathf.Pow((v - .45f) / (.55f * 1.4142f), 2));
                float k = Mathf.Clamp01((d - .58f) / .42f);
                return new Color32(60, 35, 10, (byte)(k * .3f * 255));
            });
            var v2 = MakeTiled(Fx, tex, "vignette"); Vignette = v2.rectTransform; Dom.Tag(Vignette, "#vignette");
        }

        // ---------- header.hud ----------
        static void BuildHeader()
        {
            if (Header != null) UI.Kill(Header);
            bool ph = Phone; _phoneBuilt = ph;
            Header = UI.New("hud", UI.Hud); Header.anchorMin = new Vector2(0, 1); Header.anchorMax = new Vector2(1, 1); Header.pivot = new Vector2(.5f, 1);
            Dom.Tag(Header, ".hud");
            // .plate
            var plate = Css.Box(Header, UI.Hex("#8a4c2a"), 14, 2.5f, Css.Ink, new Vector2(3, 4), Css.Ink, "plate");
            var shine = UI.Fill(plate.Find("bg"), UI.Hex("#9a5a32"), 11.5f, "shine"); shine.raycastTarget = false;
            shine.rectTransform.anchorMin = new Vector2(0, .5f); shine.rectTransform.anchorMax = Vector2.one; shine.rectTransform.offsetMin = shine.rectTransform.offsetMax = Vector2.zero;
            var low = UI.Fill(plate.Find("bg"), UI.Hex("#7a3f22"), 11.5f, "low"); low.raycastTarget = false;
            low.rectTransform.anchorMin = Vector2.zero; low.rectTransform.anchorMax = new Vector2(1, .35f); low.rectTransform.offsetMin = low.rectTransform.offsetMax = Vector2.zero;
            Css.Col(plate, 0, ph ? UI.Pad(10, 10, 4, 5) : UI.Pad(14, 14, 5, 7), TextAnchor.UpperLeft);
            plate.anchorMin = plate.anchorMax = new Vector2(0, 1); plate.pivot = new Vector2(0, 1); plate.anchoredPosition = Vector2.zero;
            UI.FitBoth(plate);
            plate.localRotation = Quaternion.Euler(0, 0, 1.2f);   // rotate(-1.2deg)
            Title = Css.T(plate, "Çiftlik İmparatorluğu", ph ? 14 : 19, UI.C.paper, false, TextAnchor.MiddleLeft, false, true);
            var ts = Title.gameObject.AddComponent<Shadow>(); ts.effectColor = new Color(0, 0, 0, .25f); ts.effectDistance = new Vector2(0, -2);
            Era = Css.T(plate, "KULÜBE DÖNEMI", 10, UI.Hex("#f3d28a"), true, TextAnchor.MiddleLeft, false);
            Era.gameObject.SetActive(!ph);
            Dom.Tag(Era, "#era"); Plate = plate; Dom.Tag(plate, ".plate");
            // .pills
            Pills = Css.Flow(Header, 7, 7, 2, null, "pills"); Dom.Tag(Pills, ".pills"); UI.FitV(Pills);
            Pills.anchorMin = Pills.anchorMax = new Vector2(1, 1); Pills.pivot = new Vector2(1, 1); Pills.anchoredPosition = Vector2.zero;
            float isz = ph ? 21 : 26, ifs = ph ? 11 : 13;
            PCoin = Pill("pCoin", "Para", out Coins, p =>
            {
                var c = Circle(p, isz, Color.white, "coin"); c.sprite = Css.RadialCircle("coin", UI.Hex("#ffe9a3"), Css.Honey, .55f, Css.HoneyDk);
                Label(c.transform, "₺", ph ? 11 : 12, UI.Hex("#7a4a00")).rectTransform.sizeDelta = new Vector2(isz, isz);
            }, "coins");
            PGem = Pill("pGem", "Yonca: hızlandırma ve özel süsler", out Gems, p => { var c = Circle(p, isz, UI.Hex("#e3f1d6"), "gem"); Label(c.transform, "🍀", ph ? 11 : 14, UI.C.text).rectTransform.sizeDelta = new Vector2(isz, isz); }, "gems");
            PLv = Pill("pLv", "Seviye", out Lv, p =>
            {
                float r = ph ? 24 : 28, rb = ph ? 18 : 21;
                var ring = Circle(p, r, UI.Hex("#e4dccb"), "ring"); Dom.Tag(ring, "#lvring");
                var f = UI.New("p", ring.transform).gameObject.AddComponent<Image>(); f.sprite = Css.CircleSprite; f.color = Css.Leaf; f.raycastTarget = false;
                f.type = Image.Type.Filled; f.fillMethod = Image.FillMethod.Radial360; f.fillOrigin = (int)Image.Origin360.Top; f.fillClockwise = true; f.fillAmount = 0;
                UI.Stretch(f.rectTransform); LvFill = f;
                var b = UI.New("b", ring.transform).gameObject.AddComponent<Image>(); b.sprite = Css.CircleSprite; b.color = Css.Forest; b.raycastTarget = false;
                b.rectTransform.sizeDelta = new Vector2(rb, rb);
                var t = Label(b.transform, "1", ph ? 10 : 11, Color.white); t.rectTransform.sizeDelta = new Vector2(rb, rb);
                return t;
            }, "lv", true);
            PRep = Pill("pRep", "İtibar", out Rep, p => { var c = Circle(p, isz, UI.Hex("#fde7d9"), "star"); Label(c.transform, "★", ph ? 11 : 13, Css.Brick).rectTransform.sizeDelta = new Vector2(isz, isz); }, "rep");
            PStore = Pill("pStore", "Ambar", out Store, p => { var c = Circle(p, isz, UI.Hex("#e6f0dd"), "barnic"); Label(c.transform, "📦", ph ? 11 : 13, UI.C.text).rectTransform.sizeDelta = new Vector2(isz, isz); }, "store");
            Coins.text = "0"; Gems.text = "0"; Rep.text = "50"; Store.text = "0/0";
        }
        static RectTransform Pill(string id, string title, out Text txt, Action<Transform> icon, string txtId) { Text t = null; var r = Pill(id, title, out t, p => { icon(p); return null; }, txtId, false); txt = t; return r; }
        static RectTransform Pill(string id, string title, out Text txt, Func<Transform, Text> icon, string txtId, bool lvl)
        {
            bool ph = Phone; float h = ph ? 31 : 36;
            var box = Css.Box(Pills, UI.C.paper, h / 2, 2.5f, Css.Ink, new Vector2(2, 3), Css.Ink, "pill");
            Css.Row(box, 7, UI.Pad(4, lvl ? 4 : (ph ? 9 : 13), 0, 0), TextAnchor.MiddleLeft);
            UI.Size(box, -1, h, -1, -1, -1, h);
            var inner = icon(box);
            if (inner != null) { txt = inner; Dom.Tag(inner, "#" + txtId); }
            else { txt = Css.T(box, "0", ph ? 12.5f : 14.5f, UI.C.text, true, TextAnchor.MiddleLeft, false); Dom.Tag(txt, "#" + txtId); }
            var d = Dom.Tag(box, "#" + id + ".pill" + (lvl ? ".lvl" : "")); d.title = title;
            return box;
        }

        // ---------- #backBtn / #guideBtn ----------
        static void BuildBack()
        {
            Back = RoundBtn(UI.Hud, Css.Forest, 19, 2.5f, new Vector2(2, 3), "back");
            Css.Row(Back, 0, UI.Pad(12, 16, 7, 7), TextAnchor.MiddleCenter); UI.FitBoth(Back);
            BackTxt = Label(Back, "← Harita", 14, Color.white);
            Back.anchorMin = Back.anchorMax = Back.pivot = new Vector2(0, 1);
            Css.Act(Back, "back", true);
            var d = Dom.Tag(Back, "#backBtn.back"); d.Hidden = true;
        }
        static void BuildGuide()
        {
            Guide = RoundBtn(UI.Hud, Css.Honey, 19, 2.5f, new Vector2(2, 3), "guidebtn");
            Css.Row(Guide, 0, UI.Pad(11, 14, 7, 7), TextAnchor.MiddleCenter); UI.FitBoth(Guide);
            Label(Guide, "📖 Rehber", 14, Css.Ink);
            Guide.anchorMin = Guide.anchorMax = Guide.pivot = new Vector2(0, 1);
            Css.Act(Guide, "guide", true);
            Dom.Tag(Guide, "#guideBtn.guidebtn");
        }

        // ---------- nav.strip ----------
        static void BuildStrip()
        {
            StripRoot = UI.New("strip", UI.Hud);
            StripRoot.anchorMin = new Vector2(0, 0); StripRoot.anchorMax = new Vector2(1, 0); StripRoot.pivot = new Vector2(.5f, 0);
            // background:linear-gradient(180deg,rgba(243,230,196,0) 0,rgba(243,230,196,.92) 22px)
            var grad = Tex(1, 32, (x, y) => { float fromTop = 31 - y; return new Color32(243, 230, 196, (byte)(Mathf.Clamp01(fromTop / 22f) * .92f * 255)); });
            var bg = StripRoot.gameObject.AddComponent<RawImage>(); bg.texture = grad; bg.raycastTarget = true;
            var vp = UI.New("viewport", StripRoot); UI.Stretch(vp); vp.gameObject.AddComponent<RectMask2D>();
            var vimg = vp.gameObject.AddComponent<Image>(); vimg.color = UI.C.clear;
            StripContent = UI.HStack(vp, 10, TextAnchor.LowerLeft, UI.Pad(14, 14, 14, 12), "content");
            StripContent.anchorMin = new Vector2(0, 0); StripContent.anchorMax = new Vector2(0, 1); StripContent.pivot = new Vector2(0, .5f);
            StripContent.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;   // align-items:stretch (kartlar eşit boyda)
            StripContent.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            var fit = StripContent.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            StripScroll = StripRoot.gameObject.AddComponent<ScrollRect>();
            StripScroll.viewport = vp; StripScroll.content = StripContent; StripScroll.horizontal = true; StripScroll.vertical = false;
            StripScroll.movementType = ScrollRect.MovementType.Clamped; StripScroll.inertia = true; StripScroll.scrollSensitivity = 30;
            var d = Dom.Tag(StripRoot, "#strip.strip"); d.Hidden = true;
        }

        // ---------- nav.dock ----------
        static void BuildDock()
        {
            if (Dock != null) UI.Kill(Dock);
            DockBtns.Clear();
            bool ph = Phone; float bs = ph ? 40 : 44;
            Dock = Css.Box(UI.Hud, UI.C.paper, 22, 2.5f, Css.Ink, new Vector2(3, 4), Css.Ink, "dock");
            Css.Row(Dock, 5, UI.Pad(8, 8, 8, 8), TextAnchor.MiddleCenter); UI.FitBoth(Dock);
            Dock.anchorMin = Dock.anchorMax = Dock.pivot = new Vector2(.5f, 0);
            Dom.Tag(Dock, ".dock");
            var items = new[] { new[] { "act", "home", "🪑", "Masaya dön" }, new[] { "open", "places", "📍", "Konumlar" }, new[] { "open", "goals", "🎯", "Hedefler" }, new[] { "open", "shop", "🛍️", "Dükkân" },
                new[] { "open", "album", "📖", "Koleksiyon" }, new[] { "open", "barn", "📦", "Ambar" }, new[] { "open", "settings", "⚙️", "Ayarlar" } };
            foreach (var it in items)
            {
                var b = Css.Box(Dock, UI.C.card, 14, 2, Css.Ink, null, null, "button");
                UI.Size(b, bs, bs).minWidth = bs;
                var t = Label(b, it[2], ph ? 19 : 21, UI.C.text); UI.Stretch(t.rectTransform); UI.IgnoreLayout(t);
                var dom = Dom.Tag(b, ""); dom.title = it[3];
                if (it[0] == "act") Css.Act(b, it[1], true);
                else { dom.data["open"] = it[1]; Css.Act(b, "data-open", true, "open", it[1]); DockBtns[it[1]] = b; }
                if (!ph) b.gameObject.AddComponent<HoverLift>();
                if (it[1] == "goals")
                {
                    var dot = Css.Box(b, Css.Brick, 9, 2, Css.Ink, null, null, "dot"); UI.IgnoreLayout(dot);
                    dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(1, 1); dot.sizeDelta = new Vector2(18, 18); dot.anchoredPosition = new Vector2(7, 7);
                    var dt = Label(dot, "!", 10, Color.white); UI.Stretch(dt.rectTransform);
                    GoalDot = dot; var dd = Dom.Tag(dot, "#goalDot.dot"); dd.Hidden = true;
                }
            }
        }

        // ---------- .cam ----------
        static void BuildCam()
        {
            Cam = UI.New("cam", UI.Hud); Cam.anchorMin = Cam.anchorMax = Cam.pivot = new Vector2(1, 0);
            Cam.sizeDelta = new Vector2(74, 34 * 3 + 12);
            Dom.Tag(Cam, "#camCtl.cam");
            void B(string act, string lbl, float x, float y, float w, string label)
            {
                var b = Css.Box(Cam, UI.C.paper, w > 34 ? 17 : 11, 2.5f, Css.Ink, new Vector2(2, 3), Css.Ink, "button");
                b.anchorMin = b.anchorMax = b.pivot = new Vector2(0, 1); b.sizeDelta = new Vector2(w, 34); b.anchoredPosition = new Vector2(x, -y);
                var t = Label(b, lbl, 15, Css.Wood); UI.Stretch(t.rectTransform);
                Css.Act(b, act, true); Dom.Of(b).title = label;
                if (act == "photo") { CamPh = b; Dom.Tag(b, ".ph"); }
            }
            B("rotL", "⟲", 0, 0, 34, "Sola döndür"); B("rotR", "⟳", 40, 0, 34, "Sağa döndür");
            B("zin", "+", 0, 40, 34, "Yakınlaş"); B("zout", "−", 40, 40, 34, "Uzaklaş");
            B("photo", "📷", 0, 80, 74, "Fotoğraf modu");
        }
        // ---------- .shutter / .photo-x ----------
        static void BuildPhoto()
        {
            Shutter = UI.New("shutter", UI.Hud); Shutter.anchorMin = Shutter.anchorMax = Shutter.pivot = new Vector2(.5f, 0); Shutter.sizeDelta = new Vector2(80, 80);
            var ink = Circle(Shutter, 80, Css.Ink, "ring"); UI.Stretch(ink.rectTransform);
            var sh = ink.gameObject.AddComponent<Shadow>(); sh.effectColor = Css.Ink; sh.effectDistance = new Vector2(4, -5);
            var w = Circle(ink.transform, 74, UI.Hex("#fffaf0"), "edge"); w.rectTransform.sizeDelta = new Vector2(74, 74);
            var r = Circle(w.transform, 64, Css.Brick, "face"); r.rectTransform.sizeDelta = new Vector2(64, 64);
            Label(r.transform, "📸", 26, Color.white).rectTransform.sizeDelta = new Vector2(64, 64);
            ink.raycastTarget = true; Css.Act(Shutter, "shoot", true); Dom.Of(Shutter).title = "Fotoğraf çek";
            Dom.Tag(Shutter, ".shutter"); Shutter.gameObject.SetActive(false);

            PhotoX = RoundBtn(UI.Hud, UI.Hex("#fffaf0"), 18, 2.5f, new Vector2(2, 3), "photo-x");
            Css.Row(PhotoX, 0, UI.Pad(14, 14, 7, 7), TextAnchor.MiddleCenter); UI.FitBoth(PhotoX);
            Label(PhotoX, "✕ Fotoğraf modundan çık", 14, UI.C.text);
            PhotoX.anchorMin = PhotoX.anchorMax = PhotoX.pivot = new Vector2(0, 1);
            Css.Act(PhotoX, "photoOff", true); Dom.Tag(PhotoX, ".photo-x"); PhotoX.gameObject.SetActive(false);
        }

        // ---------- aside.sheet ----------
        static void BuildSheet()
        {
            SheetRoot = Css.Box(UI.SheetLayer, UI.C.paper, 22, 2.5f, Css.Ink, new Vector2(5, 6), Css.Ink, "sheet");
            var v = SheetRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = UI.Pad(3); v.spacing = 0; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            SheetRoot.gameObject.AddComponent<CanvasGroup>();
            var d = Dom.Tag(SheetRoot, "#sheet.sheet"); d.Hidden = true;
        }
        static float _sheetAnim = 1; static bool _sheetWasShown;
        public static void SheetShown() { if (!_sheetWasShown) _sheetAnim = 0; }

        // ---------- #boot ----------
        static void BuildBoot()
        {
            Boot = UI.New("boot", UI.Overlay); UI.Stretch(Boot);
            var bg = Boot.gameObject.AddComponent<RawImage>();
            bg.texture = Tex(1, 64, (x, y) => (Color32)Color.Lerp(UI.C.skyLow, UI.C.skyTop, y / 63f)); bg.raycastTarget = true;
            var box = Css.Box(Boot, UI.C.paper, 22, 3, Css.Ink, new Vector2(6, 7), Css.Ink, "div");
            var shine = UI.Fill(box.Find("bg"), UI.Hex("#9a5a32"), 19, "shine"); shine.rectTransform.anchorMin = new Vector2(0, .5f); shine.rectTransform.anchorMax = Vector2.one; shine.rectTransform.offsetMin = shine.rectTransform.offsetMax = Vector2.zero;
            UI.SetBoxColor(box, UI.Hex("#8a4c2a"));
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(.5f, .5f);
            Css.Col(box, 0, UI.Pad(24, 24, 16, 16), TextAnchor.MiddleCenter); UI.FitBoth(box);
            Css.T(box, "Çiftlik\nİmparatorluğu", 30, UI.C.paper, false, TextAnchor.MiddleCenter, false, true);
            box.localRotation = Quaternion.Euler(0, 0, 2);
            Dom.Tag(Boot, "#boot");
        }

        static readonly Vector2[] _grainOff = { Vector2.zero, new Vector2(-.03f, .02f), new Vector2(.02f, -.03f), new Vector2(-.01f, .03f) };
        // ---------- Her kare: game.css kuralları ----------
        public static void Layout(float dt)
        {
            if (!_built) return;
            var body = Dom.Body;
            // body sınıfları 09-interiors'ta (Int_BodyCls) tutulur; Game.BodyHas onları okur
            string mode = Game.Mode ?? "desk";
            bool ph = Phone, photo = Game.BodyHas("photo"), sheetOpen = Game.BodyHas("sheet-open");
            bool hub = mode == "hub", focus = mode == "focus", inside = mode == "interior", desk = mode == "desk";
            bool lefty = Game.S != null && Game.S.ui != null && Game.S.ui.left;
            if (ph != _phoneBuilt) { BuildHeader(); BuildDock(); _sig = null; }
            float z = Zoom, W = UI.W, H = UI.H;
            // Animasyonlar (her kare)
            if (_grain != null && _grain.gameObject.activeSelf) { int st = (int)(Time.unscaledTime / .125f) % 4; var o = _grainOff; _grain.uvRect = new Rect(o[st].x * 2, o[st].y * 2, W * 2 / 220f, H * 2 / 220f); }
            bool rain = Game.BodyHas("wx-rain") || Game.BodyHas("wx-storm"), storm = Game.BodyHas("wx-storm");
            if (RainFx.gameObject.activeSelf != rain) RainFx.gameObject.SetActive(rain);
            if (rain)
            {
                float da = storm ? .36f : .5f, db = storm ? .55f : .8f;
                _rainA.uvRect = new Rect(0, (Time.unscaledTime / da) % 1, (W * 1.3f) / 180f, (H + 220) / 180f);
                _rainB.uvRect = new Rect(0, (Time.unscaledTime / db) % 1, (W * 1.3f) / 180f, (H + 220) / 180f);
                var cg = RainFx.gameObject.GetOrAdd<CanvasGroup>(); cg.alpha = storm ? .65f : .5f; cg.blocksRaycasts = false;
                RainFx.localRotation = Quaternion.Euler(0, 0, storm ? -12 : 0);
                RainFx.offsetMin = new Vector2(-.15f * W, -20); RainFx.offsetMax = new Vector2(.15f * W, 200);
            }
            var sheetDom = Dom.Of(SheetRoot);
            SheetSize(ph, focus || inside, photo, z);
            StripFit();

            string sig = string.Join("|", W, H, mode, photo, sheetOpen, hub, focus, inside, desk, lefty, z, sheetDom.Hidden, Dom.Of(StripRoot).Hidden, Game.Int_BackHidden, Game.Int_BackText, SafeT, SafeB, SafeL, SafeR);
            if (sig == _sig) return; _sig = sig;

            float st0 = SafeT, sb = SafeB, sl = SafeL, sr = SafeR;
            float gl = Mathf.Max(12, sl), gr = Mathf.Max(12, sr);
            // header.hud: üstte 10px, solda/sağda 12px; yazı boyutu ayarı (zoom) ölçekle
            Vis(Header, !photo);
            Header.anchorMin = Header.anchorMax = Header.pivot = new Vector2(0, 1);
            Header.sizeDelta = new Vector2((W - gl - gr) / z, 40); Header.anchoredPosition = new Vector2(gl, -(10 + st0)); Header.localScale = Vector3.one * z;
            // .pills: plakanın sağında kalan alana sağa yaslı sarılır
            float plateW = LayoutUtility.GetPreferredWidth(Plate);
            Pills.sizeDelta = new Vector2(Mathf.Max(60, (W - gl - gr) / z - plateW - 8), Pills.sizeDelta.y);
            // body.photo: #marks, #vignette, #grain gizli
            if (UI.Marks != null) Vis(UI.Marks, !photo);
            Vis(Vignette, !photo); Vis(Grain, !photo);
            // .back (hidden özniteliği setModeUI'de; fotoğrafta gizli)
            Dom.Of(Back).Hidden = Game.Int_BackHidden; BackTxt.text = Game.Int_BackText;
            Vis(Back, !photo); Back.localScale = Vector3.one * z;
            Back.anchoredPosition = new Vector2(gl, -((ph ? 96 : 62) + st0));
            // .guidebtn: yalnızca masa/harita/odakta; harita/odakta 112px (telefonda 146px)
            bool gShow = (mode == "desk" || mode == "hub" || mode == "focus") && !photo && !(ph && sheetOpen);
            if (Guide.gameObject.activeSelf != gShow) Guide.gameObject.SetActive(gShow);
            Guide.localScale = Vector3.one * z;
            Guide.anchoredPosition = new Vector2(gl, -((mode == "hub" || mode == "focus" ? (ph ? 146 : 112) : 62) + st0));
            // .dock: harita, odak, iç mekân, masa ve fotoğrafta gizli; telefonda kart açıkken gizli
            bool dockShow = !(hub || focus || inside || desk || photo || (ph && sheetOpen));
            if (Dock.gameObject.activeSelf != dockShow) Dock.gameObject.SetActive(dockShow);
            Dock.anchoredPosition = new Vector2(0, 12 + sb);
            // .cam: odak/iç/masa'da gizli; harita 150px, telefon 72px, fotoğraf 24px; sol el modunda solda
            bool camShow = !(focus || inside || desk || (ph && sheetOpen));
            if (Cam.gameObject.activeSelf != camShow) Cam.gameObject.SetActive(camShow);
            float camB = photo ? 24 : hub ? 150 : ph ? 72 : 16;
            if (lefty) { Cam.anchorMin = Cam.anchorMax = Cam.pivot = new Vector2(0, 0); Cam.anchoredPosition = new Vector2(Mathf.Max(14, sl), camB + sb); }
            else { Cam.anchorMin = Cam.anchorMax = Cam.pivot = new Vector2(1, 0); Cam.anchoredPosition = new Vector2(-Mathf.Max(14, sr), camB + sb); }
            if (CamPh.gameObject.activeSelf == photo) CamPh.gameObject.SetActive(!photo);
            // .shutter / .photo-x yalnızca fotoğraf modunda
            if (Shutter.gameObject.activeSelf != photo) Shutter.gameObject.SetActive(photo);
            Shutter.anchoredPosition = new Vector2(0, 22 + sb - 3);
            if (PhotoX.gameObject.activeSelf != photo) PhotoX.gameObject.SetActive(photo);
            PhotoX.anchoredPosition = new Vector2(Mathf.Max(14, sl), -(14 + st0));
            // .strip: telefonda harita + kart açıkken gizli
            Vis(StripRoot, !(ph && hub && sheetOpen));
            StripRoot.localScale = Vector3.one * z;
            StripRoot.anchorMin = StripRoot.anchorMax = StripRoot.pivot = new Vector2(0, 0);
            StripRoot.anchoredPosition = Vector2.zero;
            var hl = StripContent.GetComponent<HorizontalLayoutGroup>(); hl.padding = new RectOffset((int)Mathf.Max(14, sl), (int)Mathf.Max(14, sr), 14, (int)(12 + sb));
        }
        // CSS display:none (Dom.Hidden'a dokunmadan): görünmez + tıklanmaz
        public static void Vis(RectTransform rt, bool on)
        {
            if (rt == null) return;
            var cg = rt.gameObject.GetOrAdd<CanvasGroup>();
            cg.alpha = on ? 1 : 0; cg.blocksRaycasts = on; cg.interactable = on;
        }
        // Şerit genişliği/ortalama: masaüstünde sığarsa ortada (justify-content:safe center)
        public static void StripFit()
        {
            if (StripRoot == null) return;
            float z = Zoom, W = UI.W / z;
            float ch = LayoutUtility.GetPreferredHeight(StripContent);
            StripRoot.sizeDelta = new Vector2(W, Mathf.Max(0, ch));
            float cw = LayoutUtility.GetPreferredWidth(StripContent);
            bool center = !Phone && cw < W;
            StripContent.anchorMin = new Vector2(center ? .5f : 0, 0); StripContent.anchorMax = new Vector2(center ? .5f : 0, 1); StripContent.pivot = new Vector2(center ? .5f : 0, .5f);
            if (center) StripContent.anchoredPosition = new Vector2(0, 0);
        }
        // Sheet konumu/boyutu (her kare; içerik yüksekliğine göre)
        static void SheetSize(bool ph, bool focusOrInside, bool photo, float z)
        {
            // [hidden] → SetActive(false) (Dom.Hidden); body.photo → görünmez
            if (!SheetRoot.gameObject.activeSelf) { _sheetWasShown = false; return; }
            var cgp = SheetRoot.GetComponent<CanvasGroup>(); cgp.blocksRaycasts = !photo; cgp.interactable = !photo;
            if (!_sheetWasShown) { _sheetWasShown = true; _sheetAnim = 0; }
            float W = UI.W / z, H = UI.H / z;
            float maxH = ShortLandscape ? H - 70 : ph ? H * (focusOrInside ? .48f : .52f) : H - (focusOrInside ? 80 : 140);
            // içerik yüksekliği: başlık + gövde (gövde kaydırılır)
            float want = 6;
            for (int i = 0; i < SheetRoot.childCount; i++)
            {
                var c = SheetRoot.GetChild(i) as RectTransform; if (c == null || !c.gameObject.activeSelf || c.name == "bg") continue;
                var le = c.GetComponent<LayoutElement>(); if (le != null && le.ignoreLayout) continue;
                var sr = c.GetComponent<ScrollRect>();
                if (sr != null && sr.content != null) { float ch = sr.content.rect.height; if (le == null) le = UI.Size(c); le.preferredHeight = ch; le.minHeight = 0; le.flexibleHeight = 1; want += ch; }
                else { float ph0 = LayoutUtility.GetPreferredHeight(c); if (le == null) le = UI.Size(c); le.minHeight = ph0; want += ph0; }
            }
            float bottomPad = ph ? 22 + SafeB : 0;
            float h = Mathf.Min(maxH, want) + bottomPad;
            var v = SheetRoot.GetComponent<VerticalLayoutGroup>(); var pb = (int)(3 + bottomPad); if (v.padding.bottom != pb) v.padding = new RectOffset(3, 3, 3, pb);
            SheetRoot.localScale = Vector3.one * z;
            float slide = 0, alpha = 1;
            if (_sheetAnim < 1) { _sheetAnim = Mathf.Min(1, _sheetAnim + Time.unscaledDeltaTime / .25f); float k = 1 - Mathf.Pow(1 - _sheetAnim, 2); slide = 16 * (1 - k); alpha = k; }
            SheetRoot.GetComponent<CanvasGroup>().alpha = photo ? 0 : alpha;
            var shd = SheetRoot.GetComponent<Shadow>();
            if (ph)
            {
                SheetRoot.anchorMin = new Vector2(0, 0); SheetRoot.anchorMax = new Vector2(z == 1 ? 1 : 0, 0); SheetRoot.pivot = new Vector2(0, 0);
                SheetRoot.sizeDelta = new Vector2(z == 1 ? 0 : W, h); SheetRoot.anchoredPosition = new Vector2(slide, -22);
                if (shd != null) shd.effectDistance = new Vector2(0, 3);
            }
            else
            {
                SheetRoot.anchorMin = SheetRoot.anchorMax = SheetRoot.pivot = new Vector2(1, 1);
                SheetRoot.sizeDelta = new Vector2(340, h);
                SheetRoot.anchoredPosition = new Vector2(-Mathf.Max(12, SafeR) + slide, -(60 + SafeT));
                if (shd != null) shd.effectDistance = new Vector2(5, -6);
            }
            Css.BodyW = (ph ? UI.W / z : 340) - 5 - 6 - 24;
        }
    }

    public class HudDriver : MonoBehaviour
    {
        void LateUpdate() { try { HudView.Layout(Time.unscaledDeltaTime); } catch (Exception e) { Debug.LogException(e); } }
    }
}
