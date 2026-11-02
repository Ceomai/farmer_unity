// ================= ARAYÜZ KİTİ =================
// Web sürümündeki HTML/CSS arayüzün uGUI karşılığı. Her şey koddan kurulur (prefab yok).
// Birim: CSS pikseli. Tuval ölçeği ekran yoğunluğuna göre ayarlanır (1 birim ≈ 1 CSS px ≈ 1 dp),
// böylece game.css'teki ölçüler (font-size:14px, padding:7px 13px...) doğrudan kullanılabilir.
// Kural: JS'te HTML dizgisi döndüren fonksiyon, C#'ta ilk parametresi "Transform parent" olan void yöntemdir ve içeriği oraya kurar.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciftlik
{
    public static class UI
    {
        // ---------- Renkler (game.css :root) ----------
        public static class C
        {
            public static readonly Color skyTop = Hex("#a3c4c0"), skyLow = Hex("#efe0b6");
            public static readonly Color paper = Hex("#f3e6c4"), paper2 = Hex("#ead9b2"), card = Hex("#fbf3df"), line = Hex("#d9c497");
            public static readonly Color ink = Hex("#2a1a10"), text = Hex("#2c2418"), muted = Hex("#7d6c52");
            public static readonly Color forest = Hex("#2f6b3a"), forestDk = Hex("#1f4d28"), leaf = Hex("#5aa047");
            public static readonly Color honey = Hex("#f2b330"), honeyDk = Hex("#c98a12"), brick = Hex("#c4492e"), brickDk = Hex("#8f2f1c"), sky = Hex("#3f6fa0");
            public static readonly Color wood = Hex("#7a4e2d"), woodDk = Hex("#4f3119");
            public static readonly Color goldText = Hex("#4a2f00"), white = Color.white, clear = new Color(0, 0, 0, 0);
            public static readonly Color dim = new Color(30 / 255f, 20 / 255f, 10 / 255f, .55f);
        }
        public static Color Hex(string h)
        {
            Color c; if (ColorUtility.TryParseHtmlString(h.StartsWith("#") ? h : "#" + h, out c)) return c; return Color.magenta;
        }
        public static Color A(Color c, float a) { c.a = a; return c; }

        // ---------- Kurulum ----------
        public static Canvas Canvas;
        public static RectTransform Root;
        // Katmanlar (alttan üste): Marks (haritadaki etiketler), Hud, Sheet, Modal, Toast, Overlay (geçiş perdesi)
        public static RectTransform Marks, Hud, SheetLayer, ModalLayer, ToastLayer, Overlay;
        public static Font Font, DisplayFont;
        public static float Scale = 1;
        // görünür genişlik/yükseklik (CSS px). ConstantPixelSize tuvalinde kök boyu = ekran / ölçek; Root.rect ilk tuval
        // güncellemesine kadar varsayılan (100x100) kalabildiği için doğrudan ekrandan hesaplanır.
        public static float W => Screen.width / Mathf.Max(.01f, Scale);
        public static float H => Screen.height / Mathf.Max(.01f, Scale);
        public static bool Phone => W < 760;                               // game.css telefon kırılımı
        public static event Action OnResize;
        static Vector2 _lastSize;

        public static void Init()
        {
            Font = Resources.Load<Font>("Fonts/Nunito");
            if (Font == null)
            {
                try { Font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "Noto Sans", "Segoe UI Emoji", "Segoe UI Symbol", "Noto Color Emoji", "Apple Color Emoji" }, 32); } catch { }
            }
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            DisplayFont = Resources.Load<Font>("Fonts/AlfaSlabOne");
            if (DisplayFont == null)
            {
                try { DisplayFont = Font.CreateDynamicFontFromOSFont(new[] { "Rockwell Extra Bold", "Rockwell", "Georgia", "Roboto Slab", "Noto Serif", "Segoe UI Emoji", "Noto Color Emoji" }, 32); } catch { }
            }
            if (DisplayFont == null) DisplayFont = Font;

            var go = new GameObject("UI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Scale = Screen.dpi > 0 ? Mathf.Max(1f, Screen.dpi / 160f) : 1f;
            if (!Application.isMobilePlatform) Scale = Mathf.Max(1f, Screen.height / 900f);
            scaler.scaleFactor = Scale;
            go.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)go.transform;

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                UnityEngine.Object.DontDestroyOnLoad(es);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            Marks = Layer("Marks"); Hud = Layer("Hud"); SheetLayer = Layer("Sheet"); ModalLayer = Layer("Modal"); ToastLayer = Layer("Toast"); Overlay = Layer("Overlay");
            Game.OnFrame += _ =>
            {
                var s = new Vector2(Screen.width, Screen.height);
                if (s != _lastSize) { _lastSize = s; if (OnResize != null) OnResize(); }
            };
        }
        static RectTransform Layer(string n)
        {
            var rt = New(n, Root); Stretch(rt);
            return rt;
        }

        // Dokunma bir arayüz öğesinin üzerinde mi? (3D sahneye tıklamayı engellemek için)
        public static bool PointerOverUI(int pointerId = -1)
        {
            var es = EventSystem.current; if (es == null) return false;
            return pointerId >= 0 ? es.IsPointerOverGameObject(pointerId) : es.IsPointerOverGameObject();
        }
        // Ekran pikseli → CSS px (sol üstten)
        public static Vector2 ScreenToCss(Vector2 screen) => new Vector2(screen.x / Scale, (Screen.height - screen.y) / Scale);

        // ---------- Temel yapı ----------
        public static RectTransform New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }
        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }
        // Sol üstten konum (CSS left/top)
        public static RectTransform PlaceTL(RectTransform rt, float x, float y, float w = -1, float h = -1)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            if (w >= 0 || h >= 0) rt.sizeDelta = new Vector2(w >= 0 ? w : rt.sizeDelta.x, h >= 0 ? h : rt.sizeDelta.y);
            return rt;
        }
        // Çapa + pivot ile konum: ax/ay 0..1 (0,0 = sol alt)
        public static RectTransform Anchor(RectTransform rt, float ax, float ay, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay); rt.pivot = new Vector2(ax, ay);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }
        public static void Clear(Transform t)
        {
            if (t == null) return;
            // Destroy kare sonunda çalışır: önce kapat ve ağaçtan ayır ki aynı karede childCount/Dom.Q/GetComponentsInChildren eski öğeleri görmesin
            for (int i = t.childCount - 1; i >= 0; i--) { var c = t.GetChild(i); c.gameObject.SetActive(false); c.SetParent(null, false); UnityEngine.Object.Destroy(c.gameObject); }
        }
        public static void Show(Component c, bool on) { if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on); }
        public static void Kill(Component c)
        {
            if (c == null) return;
            var go = c.gameObject; go.SetActive(false);              // AnyModal / Dom.Q aynı karede silinmiş öğeyi görmesin
            if (go.transform.parent != null) go.transform.SetParent(null, false);
            UnityEngine.Object.Destroy(go);
        }

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1, float minW = -1, float minH = -1)
        {
            var le = c.gameObject.GetOrAdd<LayoutElement>();
            if (w >= 0) le.preferredWidth = w; if (h >= 0) le.preferredHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW; if (flexH >= 0) le.flexibleHeight = flexH;
            if (minW >= 0) le.minWidth = minW; if (minH >= 0) le.minHeight = minH;
            return le;
        }
        public static void IgnoreLayout(Component c) { Size(c).ignoreLayout = true; }

        // ---------- Yuvarlatılmış kutu ----------
        static Sprite _round;
        public static Sprite RoundSprite
        {
            get
            {
                if (_round != null) return _round;
                const int N = 64; const float R = 32;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    {
                        float cx = Mathf.Clamp(x + .5f, R, N - R), cy = Mathf.Clamp(y + .5f, R, N - R);
                        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy));
                        byte a = (byte)(Mathf.Clamp01(R - d + .5f) * 255);
                        px[y * N + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px); tex.Apply();
                _round = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
                return _round;
            }
        }
        public static Image Fill(Transform parent, Color c, float radius = 0, string name = "box")
        {
            var rt = New(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            SetRadius(img, radius);
            return img;
        }
        public static void SetRadius(Image img, float radius)
        {
            if (radius > 0) { img.sprite = RoundSprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 32f / radius; }
            else { img.sprite = null; img.type = Image.Type.Simple; }
        }
        // CSS kutusu: arka plan + kenarlık + gölge (box-shadow:Xpx Ypx 0 ink). İçerik doğrudan döndürülen RectTransform'a eklenir.
        public static RectTransform Box(Transform parent, Color bg, float radius = 12, float border = 0, Color? borderColor = null, Vector2? shadow = null, string name = "box")
        {
            Image outer;
            if (border > 0)
            {
                outer = Fill(parent, borderColor ?? C.ink, radius, name);
                var inner = Fill(outer.transform, bg, Mathf.Max(0, radius - border), "bg");
                Stretch(inner.rectTransform, border, border, border, border);
                IgnoreLayout(inner); inner.raycastTarget = false;
            }
            else outer = Fill(parent, bg, radius, name);
            if (shadow.HasValue)
            {
                var sh = outer.gameObject.AddComponent<Shadow>();
                sh.effectColor = borderColor ?? C.ink; sh.effectDistance = new Vector2(shadow.Value.x, -shadow.Value.y); sh.useGraphicAlpha = false;
            }
            return outer.rectTransform;
        }
        public static void SetBoxColor(RectTransform box, Color bg)
        {
            var inner = box.Find("bg"); var img = inner != null ? inner.GetComponent<Image>() : box.GetComponent<Image>();
            if (img != null) img.color = bg;
        }

        // ---------- Yazı ----------
        // Zengin metin açık: <b>, <i>, <color=#hex>, <size=n> kullanılabilir.
        public static Text Txt(Transform parent, string s, float size = 14, Color? color = null, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft, bool wrap = true, bool display = false)
        {
            var rt = New("txt", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = display ? DisplayFont : Font;
            t.fontSize = Mathf.Max(1, Mathf.RoundToInt(size));
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color ?? C.text;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.05f;
            t.raycastTarget = false;
            t.text = s ?? "";
            return t;
        }
        public static Text Title(Transform parent, string s, float size = 19, Color? color = null) => Txt(parent, s, size, color ?? C.ink, false, TextAnchor.MiddleLeft, true, true);
        public static string Esc(string s) => s == null ? "" : s.Replace("<", "‹").Replace(">", "›");   // kullanıcı metni zengin metin olarak yorumlanmasın
        public static string B(string s) => "<b>" + s + "</b>";
        public static string Col(string s, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGBA(c) + ">" + s + "</color>";

        // ---------- Yerleşim ----------
        public static RectTransform HStack(Transform parent, float gap = 8, TextAnchor align = TextAnchor.MiddleLeft, RectOffset pad = null, string name = "row")
        {
            var rt = New(name, parent);
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = gap; g.childAlignment = align; g.padding = pad ?? new RectOffset();
            g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = false; g.childForceExpandHeight = false;
            return rt;
        }
        public static RectTransform VStack(Transform parent, float gap = 8, RectOffset pad = null, TextAnchor align = TextAnchor.UpperLeft, string name = "col")
        {
            var rt = New(name, parent);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = gap; g.childAlignment = align; g.padding = pad ?? new RectOffset();
            g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            return rt;
        }
        public static RectTransform Grid(Transform parent, float cellW, float cellH, float gap = 8, int cols = 0, string name = "grid")
        {
            var rt = New(name, parent);
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(cellW, cellH); g.spacing = new Vector2(gap, gap);
            if (cols > 0) { g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = cols; }
            return rt;
        }
        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
        public static RectOffset Pad(int v, int h) => new RectOffset(h, h, v, v);
        public static RectOffset Pad(int l, int r, int t, int b) => new RectOffset(l, r, t, b);
        public static ContentSizeFitter FitV(Component c)
        {
            var f = c.gameObject.GetOrAdd<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize; return f;
        }
        public static ContentSizeFitter FitBoth(Component c)
        {
            var f = FitV(c); f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; return f;
        }
        public static RectTransform Spacer(Transform parent, float h = 8, float w = 0)
        {
            var rt = New("sp", parent); Size(rt, w, h, w > 0 ? 0 : -1); return rt;
        }
        public static RectTransform Flex(Transform parent) { var rt = New("flex", parent); Size(rt, 0, 0, 1); return rt; }
        public static Image Sep(Transform parent) { var i = Fill(parent, C.line, 0, "sep"); Size(i, -1, 2); return i; }

        // Kaydırılabilir alan; içerik dikey yığındır
        public static RectTransform Scroll(Transform parent, out RectTransform content, float gap = 10, RectOffset pad = null)
        {
            var rt = New("scroll", parent);
            var sr = rt.gameObject.AddComponent<ScrollRect>();
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = C.clear;
            var vp = New("viewport", rt); Stretch(vp); vp.gameObject.AddComponent<RectMask2D>();
            var vimg = vp.gameObject.AddComponent<Image>(); vimg.color = C.clear;
            content = VStack(vp, gap, pad ?? Pad(12), TextAnchor.UpperLeft, "content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            FitV(content);
            sr.viewport = vp; sr.content = content; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30; sr.inertia = true;
            return rt;
        }

        // ---------- Düğme ----------
        // cls: "" (yeşil), gold, soft, red, big, ghost, go, sm, dis (kapalı) — birlikte boşlukla yazılabilir ("big gold").
        public static Button Btn(Transform parent, string label, Action onClick, string cls = "")
        {
            bool has(string c) => (" " + cls + " ").Contains(" " + c + " ");
            Color bg = C.forest, fg = Color.white;
            if (has("gold")) { bg = C.honey; fg = C.goldText; }
            else if (has("soft")) { bg = C.paper2; fg = C.wood; }
            else if (has("red")) { bg = C.brick; fg = Color.white; }
            else if (has("sky")) { bg = C.sky; fg = Color.white; }
            bool ghost = has("ghost"), big = has("big"), sm = has("sm");
            RectTransform box = ghost ? New("btn", parent) : Box(parent, bg, big ? 14 : 12, 2, C.ink, new Vector2(2, 3), "btn");
            if (ghost) { var gi = box.gameObject.AddComponent<Image>(); gi.color = C.clear; }
            var b = box.gameObject.AddComponent<Button>();
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            h.padding = ghost ? Pad(4, 0) : big ? Pad(11, 11) : sm ? Pad(4, 8) : Pad(7, 13);
            var t = Txt(box, label, big ? 15 : sm ? 12 : ghost ? 12.5f : 14, ghost ? C.brick : fg, true, TextAnchor.MiddleCenter, false);
            Size(box, -1, -1, big ? 1 : -1, -1, -1, big ? 44 : sm ? 28 : 36);
            var cb = b.colors; cb.pressedColor = new Color(.85f, .85f, .85f); cb.disabledColor = new Color(1, 1, 1, .5f); b.colors = cb;
            b.targetGraphic = box.GetComponent<Image>();
            if (onClick != null) b.onClick.AddListener(() => { try { onClick(); } catch (Exception e) { Debug.LogException(e); Game.ReportError(e); } });
            box.gameObject.AddComponent<Press>();
            if (has("dis")) SetEnabled(b, false);
            return b;
        }
        public static Button ActBtn(Transform parent, string label, string act, string cls = "", params string[] kv)
        {
            return Btn(parent, label, () => Game.Act(act, Game.D(kv)), cls);   // her tıklamada taze sözlük (işleyici değiştirebilir)
        }
        public static void SetEnabled(Button b, bool on)
        {
            if (b == null) return; b.interactable = on;
            var cg = b.gameObject.GetOrAdd<CanvasGroup>(); cg.alpha = on ? 1 : .5f;
        }
        public static Text BtnLabel(Button b) => b.GetComponentInChildren<Text>();

        // Herhangi bir öğeyi tıklanabilir yap (kart, satır...)
        public static Button Clickable(Component c, Action onClick)
        {
            var g = c.GetComponent<Graphic>(); if (g == null) { var i = c.gameObject.AddComponent<Image>(); i.color = C.clear; g = i; }
            g.raycastTarget = true;
            var b = c.gameObject.GetOrAdd<Button>();
            b.transition = Selectable.Transition.None; b.targetGraphic = g;
            b.onClick.AddListener(() => { try { onClick(); } catch (Exception e) { Debug.LogException(e); Game.ReportError(e); } });
            if (c.GetComponent<Press>() == null) c.gameObject.AddComponent<Press>();
            return b;
        }

        // Uzun basma (ör. kartta tarif menüsü)
        public static void OnLongPress(Component c, Action a, float sec = .5f) { var lp = c.gameObject.GetOrAdd<LongPress>(); lp.sec = sec; lp.action = a; }

        // ---------- Küçük bileşenler ----------
        // .chip: emoji + sayı (+ "/eldeki"); miss → kırmızı
        public static RectTransform Chip(Transform parent, string emoji, string txt, bool miss = false, string sub = null)
        {
            var box = Box(parent, miss ? Hex("#fbe0d6") : C.paper2, 9, 0, null, null, "chip");
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 3; h.padding = Pad(2, 7); h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            Txt(box, emoji, 13, null, false, TextAnchor.MiddleCenter, false);
            Txt(box, txt + (sub != null ? "<color=#2c24188c>/" + sub + "</color>" : ""), 12.5f, miss ? C.brickDk : C.text, true, TextAnchor.MiddleCenter, false);
            return box;
        }
        // İlerleme çubuğu; dolguyu döndürür (SetBar ile güncellenir)
        public static Image Bar(Transform parent, float frac, Color? fill = null, float h = 8, Color? bg = null)
        {
            var track = Box(parent, bg ?? A(C.ink, .12f), h / 2, 0, null, null, "bar");
            Size(track, -1, h, 1);
            var f = Fill(track, fill ?? C.leaf, h / 2, "fill");
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1);
            f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
            return f;
        }
        public static void SetBar(Image fill, float frac) { if (fill != null) fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1); }

        public static Toggle Check(Transform parent, string label, bool on, Action<bool> onChange)
        {
            var row = HStack(parent, 8, TextAnchor.MiddleLeft, null, "check");
            var box = Box(row, Color.white, 6, 2, C.ink, null, "box"); Size(box, 22, 22);
            var mark = Txt(box, "✓", 16, C.forest, true, TextAnchor.MiddleCenter, false); Stretch(mark.rectTransform); IgnoreLayout(mark);
            var lt = Txt(row, label, 14, C.text, true); Size(lt, -1, -1, 1);
            var tg = row.gameObject.AddComponent<Toggle>();
            tg.targetGraphic = box.GetComponent<Image>(); tg.graphic = mark; tg.isOn = on;
            var bgimg = row.gameObject.AddComponent<Image>(); bgimg.color = C.clear;
            if (onChange != null) tg.onValueChanged.AddListener(v => { try { onChange(v); } catch (Exception e) { Debug.LogException(e); } });
            return tg;
        }
        public static Slider Range(Transform parent, float min, float max, float val, Action<float> onChange, bool whole = true)
        {
            var rt = New("range", parent); Size(rt, -1, 28, 1);
            var bg = Fill(rt, A(C.ink, .15f), 4, "bg"); bg.rectTransform.anchorMin = new Vector2(0, .5f); bg.rectTransform.anchorMax = new Vector2(1, .5f); bg.rectTransform.sizeDelta = new Vector2(0, 8);
            var fa = New("fillArea", rt); fa.anchorMin = new Vector2(0, .5f); fa.anchorMax = new Vector2(1, .5f); fa.sizeDelta = new Vector2(-20, 8);
            var fill = Fill(fa, C.honey, 4, "fill"); fill.rectTransform.sizeDelta = Vector2.zero;
            var ha = New("handleArea", rt); Stretch(ha, 10, 0, 10, 0);
            var handle = Fill(ha, Color.white, 11, "handle"); handle.rectTransform.sizeDelta = new Vector2(22, 0);
            handle.gameObject.AddComponent<Outline>().effectColor = C.ink;
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = whole; s.value = val;
            if (onChange != null) s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }
        public static InputField Input(Transform parent, string value, string placeholder, Action<string> onEnd, int maxLen = 0)
        {
            var box = Box(parent, Color.white, 10, 2, C.ink, null, "input"); Size(box, -1, 40, 1);
            var t = Txt(box, "", 15, C.text, true, TextAnchor.MiddleLeft, false); Stretch(t.rectTransform, 10, 4, 10, 4); t.supportRichText = false;
            var ph = Txt(box, placeholder ?? "", 15, A(C.muted, .8f), false, TextAnchor.MiddleLeft, false); Stretch(ph.rectTransform, 10, 4, 10, 4);
            var f = box.gameObject.AddComponent<InputField>();
            f.textComponent = t; f.placeholder = ph; f.characterLimit = maxLen; f.text = value ?? "";
            if (onEnd != null) f.onEndEdit.AddListener(v => onEnd(v));
            return f;
        }
        public static RawImage Picture(Transform parent, Texture tex, float w, float h)
        {
            var rt = New("img", parent); var ri = rt.gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false;
            Size(rt, w, h); rt.sizeDelta = new Vector2(w, h); return ri;
        }
        // Kart (kâğıt rengi, ince kenarlık) — içinde dikey yığın
        public static RectTransform Card(Transform parent, float gap = 6, RectOffset pad = null, Color? bg = null)
        {
            var box = Box(parent, bg ?? C.card, 14, 2, C.line, null, "card");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = gap; v.padding = pad ?? Pad(10, 12); v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return box;
        }

        // ---------- Modal / toast / uçan yazı ----------
        public static RectTransform Modal(Action<RectTransform> build, bool dim = true, bool closeOnBg = false, float width = 360)
        {
            var root = New("modal", ModalLayer); Stretch(root);
            var bg = root.gameObject.AddComponent<Image>(); bg.color = dim ? C.dim : C.clear; bg.raycastTarget = true;
            if (closeOnBg) Clickable(bg, () => Kill(root));
            // .modal>div: border 2.5px ink, radius 26px, padding 24px 22px 18px, box-shadow 6px 7px 0 ink
            var card = Box(root, C.paper, 26, 2.5f, C.ink, new Vector2(6, 7), "card");
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.padding = Pad(22, 22, 24, 18); v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var fit = card.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.sizeDelta = new Vector2(Mathf.Min(width, W - 32), 0);
            card.gameObject.AddComponent<Pop>();
            // kartın boş yerine tıklama arka plana (kapatma düğmesine) kabarmasın
            if (closeOnBg) card.gameObject.AddComponent<UiSwallow>();
            build?.Invoke(card);
            return root;
        }
        public static bool AnyModal => ModalLayer != null && ModalLayer.childCount > 0;

        // .toast: üstte ortada (top:62px), mürekkep zemin, kâğıt renkli 2.5px kenarlık, hap biçimi; @keyframes toast .35s
        static GameObject _toast;
        public static void Toast(string msg)
        {
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            var box = Box(ToastLayer, C.ink, 19, 2.5f, C.paper, null, "toast");
            SetBoxColor(box, C.ink);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(.5f, 1);
            float safeT = (Screen.height - Screen.safeArea.yMax) / Mathf.Max(.01f, Scale);
            box.anchoredPosition = new Vector2(0, -(62 + safeT));
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = Pad(8 + 2, 16 + 2);
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false;
            FitBoth(box);
            var t = Txt(box, msg, 14, Hex("#fff6e0"), true, TextAnchor.MiddleCenter, true);
            var le = Size(t); le.preferredWidth = Mathf.Min(W - 32 - 36, Mathf.Max(20, t.preferredWidth));
            box.GetComponent<Image>().raycastTarget = false;
            // yazı boyutu ayarı (body.fs-b / fs-xb .toast{zoom})
            var ui = Game.S != null ? Game.S.ui : null; float z = ui == null ? 1 : ui.fs == "b" ? 1.12f : ui.fs == "xb" ? 1.25f : 1;
            var cg = box.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
            var p0 = box.anchoredPosition;
            Tween(.35f, k =>
            {
                if (!box) return;
                float e = 1 - Mathf.Pow(1 - k, 3) + Mathf.Sin(k * Mathf.PI) * .12f;   // cubic-bezier(.2,1.5,.4,1) taşması
                cg.alpha = Mathf.Clamp01(k * 2.5f);
                box.anchoredPosition = p0 + new Vector2(0, 10 * (1 - e));
                box.localScale = Vector3.one * z * Mathf.LerpUnclamped(.9f, 1, e);
            });
            _toast = box.gameObject;
            var g = _toast;
            Game.SetTimeout(() => { if (g != null) UnityEngine.Object.Destroy(g); }, 2400);
        }
        // .float: ekranda yukarı süzülen yazı (CSS px, sol üstten; yatayda ortalı) — Alfa Slab 19px beyaz, mürekkep dış çizgi; @keyframes up 1.3s
        public static void Float(float x, float y, string txt, Color? color = null)
        {
            var t = Txt(ToastLayer, txt, 19, color ?? Color.white, false, TextAnchor.MiddleCenter, false, true);
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = C.ink; o.effectDistance = new Vector2(2, -2);
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = C.ink; sh.effectDistance = new Vector2(0, -2);
            t.raycastTarget = false;
            var rt = t.rectTransform; PlaceTL(rt, x, y, 400, 30); rt.pivot = new Vector2(.5f, .5f);
            var start = rt.anchoredPosition;
            Tween(1.3f, k =>
            {
                if (!rt) return;
                float op = k < .15f ? k / .15f : 1 - (k - .15f) / .85f;
                float sc = k < .15f ? Mathf.Lerp(.7f, 1.08f, k / .15f) : k < .3f ? Mathf.Lerp(1.08f, 1, (k - .15f) / .15f) : 1;
                float ty = k < .15f ? Mathf.Lerp(-8, 0, k / .15f) : Mathf.Lerp(0, 60, (k - .15f) / .85f);
                rt.anchoredPosition = start + new Vector2(0, ty); rt.localScale = Vector3.one * sc; t.color = A(t.color, Mathf.Clamp01(op));
            }, () => Kill(rt));
        }

        // ---------- Basit ara geçiş (tween) ----------
        public static void Tween(float sec, Action<float> step, Action done = null, bool unscaled = true)
        {
            if (Game.Host == null) { step(1); done?.Invoke(); return; }
            Game.Host.Run(TweenCo(sec, step, done, unscaled));
        }
        static IEnumerator TweenCo(float sec, Action<float> step, Action done, bool unscaled)
        {
            float t = 0;
            while (t < sec)
            {
                try { step(Mathf.Clamp01(t / sec)); } catch (Exception e) { Debug.LogException(e); yield break; }
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            try { step(1); done?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }
        // CSS "boing" sekmesi
        public static void Boing(Transform t)
        {
            if (t == null) return;
            Tween(.45f, k =>
            {
                if (!t) return;
                Vector3 s = k < .3f ? Vector3.Lerp(Vector3.one, new Vector3(1.12f, .9f, 1), k / .3f)
                          : k < .6f ? Vector3.Lerp(new Vector3(1.12f, .9f, 1), new Vector3(.95f, 1.06f, 1), (k - .3f) / .3f)
                          : Vector3.Lerp(new Vector3(.95f, 1.06f, 1), Vector3.one, (k - .6f) / .4f);
                t.localScale = s;
            });
        }
    }

    // Basınca hafif küçülme
    public class Press : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData e) { transform.localScale = new Vector3(.95f, .95f, 1); }
        public void OnPointerUp(PointerEventData e) { transform.localScale = Vector3.one; }
        public void OnPointerExit(PointerEventData e) { transform.localScale = Vector3.one; }
    }
    // İşaretçi olaylarını yutar (üst öğedeki Button/Press'e kabarmaz)
    public class UiSwallow : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public void OnPointerDown(PointerEventData e) { }
        public void OnPointerClick(PointerEventData e) { }
    }
    // Açılırken büyüyerek gelme
    public class Pop : MonoBehaviour
    {
        float t;
        void OnEnable() { t = 0; transform.localScale = Vector3.one * .85f; }
        void Update() { if (t >= 1) return; t = Mathf.Min(1, t + Time.unscaledDeltaTime / .18f); float k = 1 - Mathf.Pow(1 - t, 3); transform.localScale = Vector3.one * Mathf.Lerp(.85f, 1, k) * (1 + .04f * Mathf.Sin(k * Mathf.PI)); }
    }
    public class LongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float sec = .5f; public Action action; float down = -1; bool fired;
        public void OnPointerDown(PointerEventData e) { down = Time.unscaledTime; fired = false; }
        public void OnPointerUp(PointerEventData e) { down = -1; }
        public void OnPointerExit(PointerEventData e) { down = -1; }
        void Update()
        {
            if (down < 0 || fired) return;
            if (Time.unscaledTime - down >= sec)
            {
                fired = true; down = -1;
                // Uzun basma sonrası normal tıklama tetiklenmesin
                var b = GetComponent<Button>(); if (b != null) { b.enabled = false; Game.SetTimeout(() => { if (b) b.enabled = true; }, 50); }
                action?.Invoke();
            }
        }
    }
}
