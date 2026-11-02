// ================= game.css TEMASI → uGUI (G6) =================
// game.css'teki sınıfların (.btn, .pill-btn, .up, .tile, .job, .pbar, .stars, .thumb, .tabs, .switch, .seed, .plot, .place, .slot, .order, .need,
// .note, .uhead, .urow, .queue, .qchip, .build, .scard ...) koddan kurulan karşılıkları. Ölçüler CSS pikseli, renkler :root değişkenleri.
// Diğer modüller de kullanabilir: ör. Css.PillBtn(p,"✓ Teslim et","contract",ok,"i","0").
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciftlik
{
    public static class Css
    {
        // ---------- Renkler ----------
        public static readonly Color Ink = UI.C.ink, Paper = UI.C.paper, Paper2 = UI.C.paper2, Card = UI.C.card, Line = UI.C.line, Muted = UI.C.muted;
        public static readonly Color Forest = UI.C.forest, Leaf = UI.C.leaf, Honey = UI.C.honey, HoneyDk = UI.C.honeyDk, Brick = UI.C.brick, Wood = UI.C.wood;
        public static readonly Color GoldText = UI.Hex("#4a2f00"), TitleCol = UI.Hex("#3a2414"), Ink90 = UI.A(UI.C.ink, .9f);
        public static readonly Color DisBg = UI.Hex("#ddd1b6"), DisFg = UI.Hex("#8b7d62");
        public static readonly Color MissBg = UI.Hex("#fbe0d6"), OkBg = UI.Hex("#e3f1d6"), Locked = UI.Hex("#e6d9bb"), White = Color.white;
        public static readonly Color Green = UI.Hex("#3c8a3c");   // var(--green) tanımsız → tarayıcı currentColor yerine yeşil gösterir

        // Panel gövdesinin o anki iç genişliği (tohum şeridi gibi yüzdeli ölçüler için)
        public static float BodyW = 311;

        // ---------- Yazı ----------
        public static Text T(Transform p, string s, float size = 14, Color? col = null, bool bold = true, TextAnchor a = TextAnchor.MiddleLeft, bool wrap = true, bool display = false)
        {
            var t = UI.Txt(p, s, size, col ?? UI.C.text, bold && !display, a, wrap, display);
            return t;
        }
        public static Text Flex(Text t) { UI.Size(t, -1, -1, 1); return t; }

        // ---------- Doku üreticileri ----------
        static readonly Dictionary<string, Sprite> _spr = new Dictionary<string, Sprite>();
        static Sprite MakeSprite(string key, int n, Func<float, float, Color> px)
        {
            Sprite s; if (_spr.TryGetValue(key, out s) && s != null) return s;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var arr = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) arr[y * n + x] = px((x + .5f) / n, 1 - (y + .5f) / n);  // (u,v) v aşağı doğru (CSS gibi)
            tex.SetPixels32(arr); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100);
            _spr[key] = s; return s;
        }
        static float Aa(float d, float n) => Mathf.Clamp01(d * n + .5f);   // kenar yumuşatma
        public static Sprite CircleSprite => MakeSprite("circle", 96, (u, v) => { float d = .5f - Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)); return new Color(1, 1, 1, Aa(d, 96)); });
        // radial-gradient(circle at cx cy, c0, c1 p1, c2) dairesi
        public static Sprite RadialCircle(string key, Color c0, Color c1, float p1, Color c2, float cx = .35f, float cy = .3f)
            => MakeSprite("rc:" + key, 96, (u, v) =>
            {
                float d = .5f - Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f));
                float r = Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy)) / .75f;
                Color c = r < p1 ? Color.Lerp(c0, c1, r / p1) : Color.Lerp(c1, c2, Mathf.Clamp01((r - p1) / (1 - p1)));
                c.a *= Aa(d, 96); return c;
            });
        // Yuvarlatılmış kare + radyal degrade (thumb arka planı): radial-gradient(circle at 50% 35%,#e9e3b8,#b9c27e)
        public static Sprite ThumbBg(Color a, Color b, string key = "thumb")
            => MakeSprite("tb:" + key, 112, (u, v) =>
            {
                const float R = .286f; float cx = Mathf.Clamp(u, R, 1 - R), cy = Mathf.Clamp(v, R, 1 - R);
                float d = R - Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy));
                float r = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .35f)) / .78f);
                var c = Color.Lerp(a, b, r); c.a = Aa(d, 112); return c;
            });
        // CSS clip-path yıldızı
        public static Sprite StarSprite => MakeSprite("star", 64, (u, v) =>
        {
            float[] P = { .5f, 0, .61f, .35f, .98f, .35f, .68f, .57f, .79f, .91f, .5f, .70f, .21f, .91f, .32f, .57f, .02f, .35f, .39f, .35f };
            bool inside = false; int n = P.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = P[i * 2], yi = P[i * 2 + 1], xj = P[j * 2], yj = P[j * 2 + 1];
                if (((yi > v) != (yj > v)) && (u < (xj - xi) * (v - yi) / (yj - yi) + xi)) inside = !inside;
            }
            return new Color(1, 1, 1, inside ? 1 : 0);
        });
        // .pbar şeritleri: repeating-linear-gradient(-45deg, rgba(255,255,255,.28) 0 6px, transparent 6px 12px), 17px döşeme
        static Texture2D _stripes;
        public static Texture2D StripeTex
        {
            get
            {
                if (_stripes != null) return _stripes;
                const int N = 34; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
                var a = new Color32[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float d = ((x + y) % (N / 2)) / (float)(N / 2) * 17f; a[y * N + x] = new Color32(255, 255, 255, (byte)(d < 8.5f ? 71 : 0)); }
                t.SetPixels32(a); t.Apply(); _stripes = t; return t;
            }
        }

        // ---------- Kutu ----------
        // background + border + radius + box-shadow (ayrı gölge rengiyle)
        public static RectTransform Box(Transform p, Color bg, float radius = 14, float bw = 2, Color? bc = null, Vector2? shadow = null, Color? shadowCol = null, string name = "box")
        {
            Image outer;
            if (bw > 0)
            {
                outer = UI.Fill(p, bc ?? Ink, radius, name);
                // Yarı saydam zeminde kenarlık rengi ortadan görünmesin: kenarlık yalnızca halka olarak çizilir
                if (bg.a < .999f) { outer.sprite = RingSprite(bw, Mathf.Max(1, radius)); outer.type = Image.Type.Sliced; outer.pixelsPerUnitMultiplier = 32f / Mathf.Max(1, radius); outer.fillCenter = false; }
                var inner = UI.Fill(outer.transform, bg, Mathf.Max(0, radius - bw), "bg");
                UI.Stretch(inner.rectTransform, bw, bw, bw, bw); UI.IgnoreLayout(inner); inner.raycastTarget = false;
            }
            else outer = UI.Fill(p, bg, radius, name);
            if (shadow.HasValue)
            {
                var sh = outer.gameObject.AddComponent<Shadow>();
                sh.effectColor = shadowCol ?? Ink; sh.effectDistance = new Vector2(shadow.Value.x, -shadow.Value.y); sh.useGraphicAlpha = false;
            }
            return outer.rectTransform;
        }
        // Yuvarlatılmış dikdörtgen halka (9 dilimli; köşe yarıçapı 32 doku pikseli = "radius" CSS px)
        public static Sprite RingSprite(float bw, float radius)
        {
            string key = "ring:" + bw.ToString("0.##") + ":" + radius.ToString("0.##");
            Sprite s; if (_spr.TryGetValue(key, out s) && s != null) return s;
            const int N = 64; const float R = 32; float T = bw * R / radius;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, R, N - R), cy = Mathf.Clamp(y + .5f, R, N - R);
                    float d = R - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy));   // kenardan içe uzaklık
                    float a = Mathf.Clamp01(d + .5f) * Mathf.Clamp01(T - d + .5f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            _spr[key] = s; return s;
        }
        public static void SetBg(RectTransform box, Color bg) => UI.SetBoxColor(box, bg);
        public static void SetBorder(RectTransform box, Color c) { var i = box.GetComponent<Image>(); if (i != null) i.color = c; }
        public static void SetShadow(RectTransform box, Color c) { var s = box.GetComponent<Shadow>(); if (s != null) s.effectColor = c; }
        public static RectTransform Row(RectTransform box, float gap, RectOffset pad, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = gap; h.padding = pad; h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false; return box;
        }
        public static RectTransform Col(RectTransform box, float gap, RectOffset pad, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = gap; v.padding = pad; v.childAlignment = align;
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = false; v.childForceExpandHeight = false; return box;
        }
        public static RectTransform Flow(Transform p, float gapX = 7, float gapY = 7, int justify = 0, RectOffset pad = null, string name = "flow")
        {
            var rt = UI.New(name, p); var f = rt.gameObject.AddComponent<FlowLayout>(); f.gapX = gapX; f.gapY = gapY; f.justify = justify; f.padding = pad ?? new RectOffset(); f.childAlignment = TextAnchor.UpperLeft;
            return rt;
        }
        public static RectTransform Cols(Transform p, int cols, float gap = 7, float aspect = 0, string name = "grid")
        {
            var rt = UI.New(name, p); var g = rt.gameObject.AddComponent<ColsLayout>(); g.cols = cols; g.gapX = g.gapY = gap; g.aspect = aspect; g.padding = new RectOffset();
            return rt;
        }

        // ---------- Tıklama ----------
        // data-act → Game.Act; tıklanan öğenin orta-üst noktası (CSS px) "_x","_y" olarak eklenir (rc.left+rc.width/2, rc.top).
        public static Button Click(RectTransform box, Action<Vector2> onClick, bool enabled = true, bool boing = true)
        {
            var g = box.GetComponent<Graphic>(); if (g == null) { var i = box.gameObject.AddComponent<Image>(); i.color = UI.C.clear; g = i; }
            g.raycastTarget = true;
            var b = box.gameObject.GetOrAdd<Button>();
            b.transition = Selectable.Transition.None; b.targetGraphic = g; b.interactable = enabled;
            b.onClick.AddListener(() =>
            {
                try
                {
                    var d = Dom.Of(box); var r = d.Rect();
                    if (boing) UI.Boing(box);
                    onClick?.Invoke(new Vector2(r.x + r.width / 2, r.y));
                }
                catch (Exception e) { Debug.LogException(e); Game.ReportError(e); }
            });
            if (enabled && box.GetComponent<Press>() == null) box.gameObject.AddComponent<Press>();
            return b;
        }
        public static Button Act(RectTransform box, string act, bool enabled, params string[] kv)
        {
            var dom = Dom.Of(box); dom.data["act"] = act;
            for (int i = 0; i + 1 < kv.Length; i += 2) dom.data[kv[i]] = kv[i + 1];
            if (!enabled) dom.data["disabled"] = "";
            return Click(box, pos =>
            {
                var d = Game.D(kv); d["_x"] = pos.x.ToString(System.Globalization.CultureInfo.InvariantCulture); d["_y"] = pos.y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Game.Act(act, d);
            }, enabled);
        }
        // Devre dışı görünüm: saydamlık (tile/seed/plot/place)
        public static void Dim(Component c, float a) { var cg = c.gameObject.GetOrAdd<CanvasGroup>(); cg.alpha = a; }
        static void RecolorTexts(Transform t, Color c) { foreach (var x in t.GetComponentsInChildren<Text>(true)) x.color = c; }

        // ---------- Düğmeler ----------
        // .btn (cls: gold, soft, red, big, ghost)
        public static RectTransform Btn(Transform p, string label, string act, string cls, bool enabled, params string[] kv)
        {
            bool has(string c) => (" " + cls + " ").Contains(" " + c + " ");
            bool ghost = has("ghost"), big = has("big");
            Color bg = has("gold") ? Honey : has("soft") ? Paper2 : has("red") ? Brick : Forest;
            Color fg = has("gold") ? GoldText : has("soft") ? Wood : White;
            RectTransform box;
            if (ghost) { var gr = UI.HStack(p, 0, TextAnchor.MiddleLeft, null, "ghost-row"); box = UI.New("btn", gr);   /* align-self:flex-start */ var gi = box.gameObject.AddComponent<Image>(); gi.color = UI.C.clear; fg = Brick; }
            else box = Box(p, enabled ? bg : DisBg, big ? 14 : 12, 2, enabled ? Ink : DisFg, new Vector2(2, 3), enabled ? Ink : DisFg, "btn");
            Row(box, 6, ghost ? UI.Pad(0, 0, 4, 4) : big ? UI.Pad(11) : UI.Pad(13, 13, 7, 7), ghost ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            var t = T(box, label, ghost ? 12.5f : big ? 15 : 14, enabled ? fg : DisFg, true, TextAnchor.MiddleCenter, false);
            if (big) UI.Size(box, -1, -1, 1, -1, -1, 44);
            Dom.Tag(box, ".btn" + (cls != "" ? "." + cls.Trim().Replace(" ", ".") : ""));
            if (act != null) Act(box, act, enabled, kv);
            return box;
        }
        // .pill-btn (cls: "", gold, go)
        public static RectTransform PillBtn(Transform p, string label, string act, string cls, bool enabled, params string[] kv)
        {
            Color bg = cls == "gold" ? Honey : cls == "go" ? Forest : Paper2, fg = cls == "gold" ? GoldText : cls == "go" ? White : UI.C.text;
            var box = Box(p, enabled ? bg : DisBg, 16, 2, enabled ? Ink : DisFg, new Vector2(2, 2), enabled ? Ink : DisFg, "pill-btn");
            Row(box, 6, UI.Pad(12, 12, 6, 6), TextAnchor.MiddleCenter);
            T(box, label, 12.5f, enabled ? fg : DisFg, true, TextAnchor.MiddleCenter, false);
            Dom.Tag(box, ".pill-btn" + (cls != "" ? "." + cls : ""));
            if (act != null) Act(box, act, enabled, kv);
            return box;
        }
        // .up: "⬆ Sv 2 <span>₺1.000</span>"
        public static RectTransform Up(Transform p, string label, string sub, string act, bool enabled, params string[] kv)
        {
            var box = Box(p, enabled ? Honey : DisBg, 16, 2, enabled ? Ink : DisFg, new Vector2(2, 2), enabled ? Ink : DisFg, "up");
            Row(box, 6, UI.Pad(12, 12, 6, 6), TextAnchor.MiddleCenter);
            var fg = enabled ? GoldText : DisFg;
            T(box, label, 12.5f, fg, true, TextAnchor.MiddleCenter, false);
            var s = T(box, sub, 12.5f, UI.A(fg, .85f), true, TextAnchor.MiddleCenter, false);
            Dom.Tag(box, ".up");
            if (act != null) Act(box, act, enabled, kv);
            return box;
        }

        // ---------- Rozet / yıldız / küçük parçalar ----------
        public static RectTransform Stars(Transform p, int n, int max)
        {
            var row = UI.HStack(p, 2, TextAnchor.MiddleLeft, null, "stars");
            for (int k = 0; k < max; k++) { var i = UI.New("i", row).gameObject.AddComponent<Image>(); i.sprite = StarSprite; i.color = k < n ? Honey : UI.Hex("#d8c9a6"); i.raycastTarget = false; UI.Size(i, 13, 13); }
            Dom.Tag(row, ".stars");
            return row;
        }
        // .big: emoji 26px, sağ altta ×n rozeti
        public static RectTransform Big(Transform p, string emoji, string badge = null, float size = 26)
        {
            var box = UI.New("big", p);
            var t = UI.Txt(box, emoji, size, null, false, TextAnchor.MiddleCenter, false); UI.Stretch(t.rectTransform);
            var le = UI.Size(box, size * 1.15f, size * 1.15f);
            le.minWidth = size * 1.15f;
            if (!string.IsNullOrEmpty(badge))
            {
                var b = Box(box, Ink, 7, 0, null, null, null, "i"); UI.IgnoreLayout(b);
                b.anchorMin = b.anchorMax = new Vector2(1, 0); b.pivot = new Vector2(1, 0);
                var bt = UI.Txt(b, badge, 10.5f, White, true, TextAnchor.MiddleCenter, false);
                UI.Stretch(bt.rectTransform, 4, 0, 4, 0);
                float w = Mathf.Max(16, badge.Length * 6.4f + 8);
                b.sizeDelta = new Vector2(w, 15); b.anchoredPosition = new Vector2(w - 13 - 2, -1);
                b.GetComponent<Image>().raycastTarget = false;
            }
            Dom.Tag(box, ".big");
            return box;
        }
        // .s rozeti (ok/new/idle/warn) sağ üst köşede
        public static RectTransform Badge(Transform p, string c, string txt, float size = 20, float off = -8, bool left = false)
        {
            Color bg = c == "ok" ? Leaf : c == "new" ? Honey : c == "warn" ? Brick : Card;
            var b = Box(p, bg, size / 2, 2, Ink, null, null, "s"); UI.IgnoreLayout(b);
            b.anchorMin = b.anchorMax = new Vector2(left ? 0 : 1, 1); b.pivot = new Vector2(left ? 0 : 1, 1);
            var t = UI.Txt(b, txt, size * .5f, c == "ok" || c == "warn" ? White : UI.C.text, true, TextAnchor.MiddleCenter, false);
            UI.Stretch(t.rectTransform, 2, 0, 2, 0);
            float w = Mathf.Max(size, txt.Length * size * .32f + 8);
            b.sizeDelta = new Vector2(w, size); b.anchoredPosition = new Vector2(left ? off : -off, -off);
            b.GetComponent<Image>().raycastTarget = false;
            if (c == "ok") b.gameObject.AddComponent<Bob>();
            Dom.Tag(b, ".s." + c);
            return b;
        }
        public static Text Note(Transform p, string s)
        {
            var t = T(p, s, 12.5f, Muted, true); t.lineSpacing = 1.2f; Dom.Tag(t, ".note"); return t;
        }
        public static Text Tm(Transform p, string s, Color? c = null) => T(p, s, 10.5f, c ?? Muted, true, TextAnchor.MiddleCenter, false);
        // .uhead: başlık solda, sağda ek
        public static RectTransform UHead(Transform p, string left, string right = null, Action<Transform> rightBuild = null)
        {
            var row = UI.HStack(p, 6, TextAnchor.MiddleLeft, null, "uhead");
            var l = T(row, left, 14, TitleCol, false, TextAnchor.MiddleLeft, true, true); Flex(l);
            if (right != null) Tm(row, right);
            rightBuild?.Invoke(row);
            Dom.Tag(row, ".uhead");
            return row;
        }
        public static RectTransform Urow(Transform p) { var r = Flow(p, 7, 7, 0, null, "urow"); Dom.Tag(r, ".urow"); return r; }

        // ---------- .job / .pw / .pbar ----------
        public static RectTransform Job(Transform p, string name = "job")
        {
            var box = Box(p, Card, 14, 2, Ink, new Vector2(2, 3), Ink90, name);
            Row(box, 12, UI.Pad(12, 12, 8, 8), TextAnchor.MiddleLeft);
            Dom.Tag(box, ".job");
            return box;
        }
        public static RectTransform Pw(Transform p)
        {
            var v = UI.VStack(p, 3, null, TextAnchor.MiddleLeft, "pw"); UI.Size(v, -1, -1, 1); Dom.Tag(v, ".pw"); return v;
        }
        public static Text PwB(Transform p, string s, float size = 13.5f) => T(p, s, size, UI.C.text, true);
        public static Text PwSmall(Transform p, string s) => T(p, s, 11.5f, Muted, true);
        // İlerleme çubuğu; s/e verilirse kendi kendine güncellenir (data-s / data-e)
        public static Image PBar(Transform p, float frac, double s = 0, double e = 0, float h = 10, bool anim = true)
        {
            var track = Box(p, UI.Hex("#e8dcbf"), h * .9f, 1.5f, UI.A(Ink, .45f), null, null, "pbar");
            UI.Size(track, -1, h, 1);
            var clip = UI.New("clip", track); UI.Stretch(clip, 1.5f, 1.5f, 1.5f, 1.5f); clip.gameObject.AddComponent<RectMask2D>(); UI.IgnoreLayout(clip);
            var f = UI.Fill(clip, Honey, 0, "i");
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1); f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
            var hi = UI.Fill(f.rectTransform, UI.Hex("#f6cf6b"), 0, "grad"); hi.rectTransform.anchorMin = new Vector2(.5f, 0); hi.rectTransform.anchorMax = Vector2.one; hi.rectTransform.offsetMin = hi.rectTransform.offsetMax = Vector2.zero; hi.raycastTarget = false;
            if (anim)
            {
                var st = UI.New("stripes", f.rectTransform); UI.Stretch(st); var ri = st.gameObject.AddComponent<RawImage>(); ri.texture = StripeTex; ri.raycastTarget = false;
                st.gameObject.AddComponent<Stripes>();
            }
            var d = Dom.Tag(f, "");
            if (e > s)
            {
                d.data["s"] = s.ToString("R", System.Globalization.CultureInfo.InvariantCulture); d.data["e"] = e.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                var lv = f.gameObject.AddComponent<Live>(); lv.s = s; lv.e = e; lv.bar = f;
            }
            return f;
        }
        // data-left geri sayım yazısı ("⏱ 1 dk 05 sn")
        public static Text Countdown(Transform p, double left, string pre, bool shortFmt = false, float size = 11.5f, Color? col = null)
        {
            var t = T(p, "", size, col ?? Muted, true);
            var d = Dom.Tag(t, ""); d.data["left"] = left.ToString("R", System.Globalization.CultureInfo.InvariantCulture); d.data["pre"] = pre; if (shortFmt) d.data["short"] = "1";
            var lv = t.gameObject.AddComponent<Live>(); lv.left = left; lv.pre = pre; lv.shortFmt = shortFmt; lv.txt = t; lv.Tick();
            return t;
        }

        // ---------- .tile / .tiles / .ins ----------
        public static RectTransform Tiles(Transform p, bool queue = false, int cols = 3)
        {
            var g = Cols(p, cols, 7, 0, "tiles"); Dom.Tag(g, ".tiles" + (queue ? ".qtiles" : "")); return g;
        }
        public static RectTransform Tile(Transform p, string act, bool enabled, bool dashed, params string[] kv)
        {
            var box = Box(p, Card, 14, 2, dashed ? UI.A(Ink, .75f) : Ink, new Vector2(2, 3), Ink90, "tile");
            Col(box, 2, UI.Pad(3, 3, 6, 5), TextAnchor.UpperCenter);
            Dom.Tag(box, ".tile");
            if (act != null) Act(box, act, enabled, kv);
            if (!enabled) Dim(box, .55f); else if (dashed) Dim(box, .92f);
            return box;
        }
        public static RectTransform Ins(Transform p) { var f = Flow(p, 5, 1, 1, null, "ins"); Dom.Tag(f, ".ins"); return f; }
        public static Text InsB(Transform p, string s, bool miss) => T(p, s, 11.5f, miss ? Brick : UI.C.text, true, TextAnchor.MiddleCenter, false);

        // ---------- .tabs ----------
        public struct Tab { public string label, act; public bool on; public string[] kv; public Tab(string l, bool on, string act, params string[] kv) { label = l; this.on = on; this.act = act; this.kv = kv; } }
        public static RectTransform Tabs(Transform p, params Tab[] tabs)
        {
            var box = Box(p, Paper2, 18, 2, Ink, null, null, "tabs");
            // padding:3px + 2px kenarlık; düğmeler eşit genişlikte (flex:1)
            Row(box, 0, UI.Pad(5), TextAnchor.MiddleCenter);
            var g = Cols(box, Mathf.Max(1, tabs.Length), 5, 0, "row"); UI.Size(g, -1, -1, 1);
            foreach (var tb in tabs)
            {
                var b = tb.on ? Box(g, Card, 14, 1.5f, Ink, null, null, "tab") : Box(g, UI.C.clear, 14, 0, null, null, null, "tab");
                Row(b, 0, UI.Pad(5), TextAnchor.MiddleCenter);
                T(b, tb.label, 12.5f, UI.C.text, true, TextAnchor.MiddleCenter, true).alignment = TextAnchor.MiddleCenter;
                var dom = Dom.Tag(b, ""); dom.data["selected"] = tb.on ? "true" : "false";
                if (tb.act != null) Act(b, tb.act, true, tb.kv ?? new string[0]);
            }
            Dom.Tag(box, ".tabs");
            return box;
        }

        // ---------- .switch (label + checkbox) ----------
        public static Toggle Switch(Transform p, string id, bool on, string title, string note, Action<string, bool> onChange)
        {
            var box = Box(p, Card, 14, 2, Ink, new Vector2(2, 3), Ink90, "switch");
            Row(box, 12, UI.Pad(12, 12, 10, 10), TextAnchor.UpperLeft);
            var cb = Box(box, Color.white, 5, 2, Forest, null, null, "input"); UI.Size(cb, 20, 20); UI.Size(cb).minWidth = 20;
            var mark = UI.Txt(cb, "✓", 15, Forest, true, TextAnchor.MiddleCenter, false); UI.Stretch(mark.rectTransform); UI.IgnoreLayout(mark);
            var txt = UI.VStack(box, 2, null, TextAnchor.UpperLeft, "span"); UI.Size(txt, -1, -1, 1);
            T(txt, title, 14, UI.C.text, true);
            if (!string.IsNullOrEmpty(note)) Note(txt, note);
            var tg = box.gameObject.AddComponent<Toggle>();
            tg.transition = Selectable.Transition.None;
            tg.targetGraphic = box.GetComponent<Image>(); tg.graphic = mark; tg.isOn = on;
            var inner = cb.Find("bg"); if (inner != null) inner.GetComponent<Image>().color = on ? Forest : Color.white;
            mark.color = Color.white;
            tg.onValueChanged.AddListener(v =>
            {
                if (inner != null) inner.GetComponent<Image>().color = v ? Forest : Color.white;
                try { onChange?.Invoke(id, v); } catch (Exception e) { Debug.LogException(e); Game.ReportError(e); }
            });
            var d = Dom.Tag(box, ".switch"); var di = Dom.Tag(cb, "#" + id); di.data["checked"] = on ? "1" : "0";
            return tg;
        }

        // ---------- Küçük resim (.thumb) ----------
        public static RectTransform Thumb(Transform p, Action<Transform> content, bool sm = false)
        {
            float s = sm ? 42 : 56, r = sm ? 12 : 16;
            var box = Box(p, UI.C.clear, r, 2, Ink, sm ? (Vector2?)null : new Vector2(2, 3), Ink, "thumb");
            var bg = box.Find("bg").GetComponent<Image>(); bg.sprite = ThumbBg(UI.Hex("#e9e3b8"), UI.Hex("#b9c27e")); bg.type = Image.Type.Simple; bg.color = Color.white;
            UI.Size(box, s, s).minWidth = s;
            var clip = UI.New("clip", box); UI.Stretch(clip, 2, 2, 2, 2); clip.gameObject.AddComponent<RectMask2D>(); UI.IgnoreLayout(clip);
            content?.Invoke(clip);
            // içerik yazıysa ortala ve boyutlandır
            foreach (var t in clip.GetComponentsInChildren<Text>(true)) { t.alignment = TextAnchor.MiddleCenter; t.fontSize = (int)(sm ? 20 : 26); UI.Stretch(t.rectTransform); }
            Dom.Tag(box, ".thumb" + (sm ? ".sm" : ""));
            return box;
        }
        // <img src> ya da emoji: resim varsa object-fit:cover %115
        public static void ImgOrEmoji(Transform p, Texture tex, string emoji)
        {
            if (tex != null)
            {
                var rt = UI.New("img", p); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, .5f);
                rt.offsetMin = rt.offsetMax = Vector2.zero; rt.localScale = Vector3.one * 1.15f;
                var ri = rt.gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false;
                float a = tex.height > 0 ? tex.width / (float)tex.height : 1;
                if (a > 1) ri.uvRect = new Rect((1 - 1 / a) / 2, 0, 1 / a, 1); else if (a < 1) ri.uvRect = new Rect(0, (1 - a) / 2, 1, a);
            }
            else UI.Txt(p, emoji, 26, null, false, TextAnchor.MiddleCenter, false);
        }
    }

    // ---------- Canlandırma bileşenleri ----------
    // @keyframes bob: 0 → -4px → 0 (1.1s)
    public class Bob : MonoBehaviour
    {
        public float amp = 4, period = 1.1f;
        RectTransform rt; Vector2 basePos; float lastOff; bool init;
        void LateUpdate()
        {
            if (rt == null) rt = transform as RectTransform; if (rt == null) return;
            var p = rt.anchoredPosition;
            if (!init || (p - (basePos + new Vector2(0, lastOff))).sqrMagnitude > .0001f) { basePos = p; init = true; }
            float k = (Mathf.Sin(Time.unscaledTime / period * Mathf.PI * 2 - Mathf.PI / 2) + 1) / 2;   // ease-in-out
            lastOff = amp * k;                                                                          // CSS -4px (yukarı) = Unity +y
            rt.anchoredPosition = basePos + new Vector2(0, lastOff);
        }
    }
    // @keyframes qaPulse: ölçek 1 → 1.05 → 1 (1.6s)
    public class Pulse : MonoBehaviour
    {
        public float amp = .05f, period = 1.6f;
        void Update() { float k = (Mathf.Sin(Time.unscaledTime / period * Mathf.PI * 2 - Mathf.PI / 2) + 1) / 2; transform.localScale = Vector3.one * (1 + amp * k); }
    }
    // .pbar i şeritlerinin kayması (stripes 1s linear infinite, 17px)
    public class Stripes : MonoBehaviour
    {
        RawImage ri; RectTransform rt;
        void Update()
        {
            if (ri == null) { ri = GetComponent<RawImage>(); rt = transform as RectTransform; }
            if (ri == null || rt == null) return;
            float w = Mathf.Max(1, rt.rect.width), h = Mathf.Max(1, rt.rect.height);
            ri.uvRect = new Rect(-(Time.unscaledTime % 1f), 0, w / 17f, h / 17f);
        }
    }
    // data-s/data-e çubuğu ve data-left geri sayımı (33-loop'taki tick ile aynı hesap, kare kare)
    public class Live : MonoBehaviour
    {
        public double s, e, left; public string pre; public bool shortFmt; public Image bar; public Text txt;
        float acc;
        public void Tick()
        {
            if (bar != null && e > s) bar.rectTransform.anchorMax = new Vector2((float)Math.Min(1, Math.Max(0, (Game.Now() - s) / (e - s))), 1);
            if (txt != null && pre != null)
            {
                int sec = Math.Max(0, (int)Math.Ceiling((left - Game.Now()) / 1000));
                txt.text = pre + " " + (shortFmt ? G6Fmt.SecShort(sec) : G6Fmt.Secs(sec));
            }
        }
        void Update() { acc += Time.unscaledDeltaTime; if (bar != null || acc >= .25f) { acc = 0; try { Tick(); } catch { } } }
    }
    // Üzerine gelince yukarı kalkma (.dock button:hover translateY(-3px))
    public class HoverLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float lift = 3;
        RectTransform rt; Vector2 basePos; float cur, target; bool init;
        public void OnPointerEnter(PointerEventData e) { target = lift; }
        public void OnPointerExit(PointerEventData e) { target = 0; }
        void LateUpdate()
        {
            if (rt == null) rt = transform as RectTransform; if (rt == null) return;
            var p = rt.anchoredPosition;
            if (!init || (p - (basePos + new Vector2(0, cur))).sqrMagnitude > .0001f) { basePos = p; init = true; }
            cur = Mathf.MoveTowards(cur, target, Time.unscaledDeltaTime * lift / .15f);   // transition .15s
            rt.anchoredPosition = basePos + new Vector2(0, cur);
        }
    }
    // secs / secShort (03-helpers) — Live bileşeni G1'e bağımlı olmadan çalışsın diye aynı biçimlendirme
    public static class G6Fmt
    {
        public static string Secs(int s) => s >= 3600 ? s / 3600 + " sa" + ((s % 3600 / 60) != 0 ? " " + (s % 3600 / 60) + " dk" : "") : s >= 60 ? s / 60 + " dk" + (s % 60 != 0 ? " " + (s % 60).ToString("00") + " sn" : "") : s + " sn";
        public static string SecShort(int s) => s <= 0 ? "hazır" : s < 60 ? s + "sn" : s < 3600 ? (int)Math.Ceiling(s / 60.0) + "dk" : s < 86400 ? s / 3600 + "sa" + (s % 3600 >= 60 ? " " + (s % 3600 / 60) + "dk" : "") : s / 86400 + "g";
    }
}
