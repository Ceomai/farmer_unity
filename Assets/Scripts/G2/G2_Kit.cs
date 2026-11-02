// G2 (sistemler) ortak arayüz parçaları: game.css'teki .pill-btn, .job, .pw, .pbar, .order, .who, .need, .tiles/.tile,
// .uhead, .note, .tm, .tabs, .modal (login/mg) sınıflarının uGUI karşılıkları. Yalnızca 18–27 modüllerinin panelleri kullanır.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public static partial class Game
    {
        // ---------- game.css renkleri (UI.C dışında kalanlar) ----------
        internal static readonly Color G2_DisBg = UI.Hex("#ddd1b6"), G2_DisFg = UI.Hex("#8b7d62");
        internal static readonly Color G2_PbarBg = UI.Hex("#e8dcbf"), G2_PbarBorder = new Color(42 / 255f, 26 / 255f, 16 / 255f, .45f);
        internal static readonly Color G2_Shadow = new Color(42 / 255f, 26 / 255f, 16 / 255f, .9f);
        internal static readonly Color G2_NeedBg = UI.Hex("#e3f1d6"), G2_MissBg = UI.Hex("#fbe0d6"), G2_NeedBorder = new Color(42 / 255f, 26 / 255f, 16 / 255f, .4f);
        internal static readonly Color G2_ModalP = UI.Hex("#5d503c"), G2_UheadCol = UI.Hex("#3a2414");

        // JS'te `${sayı}` yazımı: tam sayı ise ondalıksız, değilse nokta ile
        internal static string G2_Num(double v)
        {
            if (!IsFinite(v)) return double.IsNaN(v) ? "NaN" : v > 0 ? "Infinity" : "-Infinity";
            if (Math.Floor(v) == v && Math.Abs(v) < 1e15) return ((long)v).ToString(CultureInfo.InvariantCulture);
            return v.ToString("R", CultureInfo.InvariantCulture);
        }

        // ---------- .pill-btn ----------
        // cls: "", "gold", "go". span=true → tıklanamayan bilgi hapı (cursor:default)
        internal static RectTransform G2_PillBox(Transform p, string label, string cls, bool enabled, bool span = false)
        {
            bool has(string c) => (" " + cls + " ").Contains(" " + c + " ");
            Color bg = UI.C.paper2, fg = UI.C.text, br = UI.C.ink;
            if (has("gold")) { bg = UI.C.honey; fg = UI.C.goldText; }
            else if (has("go")) { bg = UI.C.forest; fg = Color.white; }
            if (!enabled && !span) { bg = G2_DisBg; fg = G2_DisFg; br = G2_DisFg; }
            var box = UI.Box(p, bg, 15, 2, br, new Vector2(2, 2), "pill");
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = UI.Pad(6, 12); h.spacing = 6; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            UI.Txt(box, label, 12.5f, fg, true, TextAnchor.MiddleCenter, false);
            UI.Size(box, -1, -1, -1, -1, -1, 30);
            return box;
        }
        internal static Button G2_Pill(Transform p, string label, Action onClick, string cls = "", bool enabled = true)
        {
            var box = G2_PillBox(p, label, cls, enabled);
            var b = UI.Clickable(box, () => { if (enabled) { UI.Boing(box); onClick?.Invoke(); } });
            b.interactable = enabled;
            return b;
        }
        internal static Button G2_PillAct(Transform p, string label, string act, string cls, bool enabled, params string[] kv)
        {
            var d = D(kv);
            return G2_Pill(p, label, () => Act(act, d), cls, enabled);
        }
        internal static RectTransform G2_PillSpan(Transform p, string label) => G2_PillBox(p, label, "", true, true);

        // ---------- .btn.big ----------
        internal static Button G2_BigBtn(Transform p, string label, Action onClick, string cls = "gold", bool enabled = true)
        {
            var b = UI.Btn(p, label, onClick, "big " + cls);
            if (!enabled) UI.SetEnabled(b, false);
            return b;
        }

        // ---------- .job / .pw / .big ----------
        internal static RectTransform G2_Job(Transform p, int padV = 8, int padH = 12)
        {
            var box = UI.Box(p, UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "job");
            box.GetComponent<Shadow>().effectColor = G2_Shadow;
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = UI.Pad(padV, padH); h.spacing = 12; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            return box;
        }
        internal static Text G2_Big(Transform p, string emoji, float size = 26, string badge = null)
        {
            var t = UI.Txt(p, emoji, size, null, false, TextAnchor.MiddleCenter, false);
            if (badge != null)
            {
                // .big i: sağ altta koyu rozet
                var b = UI.Box(t.transform, UI.C.ink, 7, 0, null, null, "badge");
                b.anchorMin = b.anchorMax = new Vector2(1, 0); b.pivot = new Vector2(0, 0);
                var bt = UI.Txt(b, badge, 10.5f, Color.white, true, TextAnchor.MiddleCenter, false);
                UI.Stretch(bt.rectTransform, 4, 0, 4, 0);
                float bw = bt.preferredWidth + 8;
                b.sizeDelta = new Vector2(bw, 15);
                b.anchoredPosition = new Vector2(13 - bw, -1);   // right:-13px; bottom:-1px
                UI.IgnoreLayout(b);
            }
            return t;
        }
        internal static RectTransform G2_Pw(Transform row)
        {
            var pw = UI.VStack(row, 3, null, TextAnchor.MiddleLeft, "pw");
            UI.Size(pw, -1, -1, 1, -1, 0);
            return pw;
        }
        internal static Text G2_PwB(Transform pw, string s, float size = 13.5f) => UI.Txt(pw, s, size, UI.C.text, true);
        internal static Text G2_PwSmall(Transform pw, string s) => UI.Txt(pw, s, 11.5f, UI.C.muted, true);

        // ---------- .pbar ----------
        // fill: düz renk ya da (fill2 verilirse) yatay gradyan
        internal static RectTransform G2_Pbar(Transform p, float frac, Color? fill = null, Color? fill2 = null, float h = 10)
        {
            var track = UI.Box(p, G2_PbarBg, 9, 1.5f, G2_PbarBorder, null, "pbar");
            UI.Size(track, -1, h, 1);
            var inner = track.Find("bg");
            var area = UI.New("fillArea", track); UI.Stretch(area, 1.5f, 1.5f, 1.5f, 1.5f); area.gameObject.AddComponent<RectMask2D>();
            UI.IgnoreLayout(area);
            Graphic g;
            Color a = fill ?? UI.C.honey, b = fill2 ?? (fill.HasValue ? fill.Value : UI.Hex("#f6cf6b"));
            if (a != b) { var ri = G2_Gradient(area, a, b, true); g = ri; }
            else { var img = UI.Fill(area, a, 0, "fill"); g = img; }
            var rt = g.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(Mathf.Clamp01(frac), 1); rt.offsetMin = rt.offsetMax = Vector2.zero;
            return track;
        }

        // Gradyan doku (CSS linear-gradient karşılığı)
        static readonly Dictionary<string, Texture2D> _g2Grad = new Dictionary<string, Texture2D>();
        internal static RawImage G2_Gradient(Transform p, Color a, Color b, bool horizontal)
        {
            var key = ColorUtility.ToHtmlStringRGBA(a) + ColorUtility.ToHtmlStringRGBA(b) + (horizontal ? "h" : "v");
            Texture2D tex;
            if (!_g2Grad.TryGetValue(key, out tex) || tex == null)
            {
                const int N = 32;
                tex = new Texture2D(horizontal ? N : 1, horizontal ? 1 : N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color[N];
                // dikeyde doku y=0 alttadır: CSS'teki ilk renk üstte olmalı
                for (int i = 0; i < N; i++) { float k = i / (float)(N - 1); px[i] = horizontal ? Color.Lerp(a, b, k) : Color.Lerp(b, a, k); }
                tex.SetPixels(px); tex.Apply();
                _g2Grad[key] = tex;
            }
            var rt = UI.New("grad", p);
            var ri = rt.gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false;
            UI.Stretch(rt);
            return ri;
        }

        // ---------- .unit / .uhead / .note / .tm / .urow ----------
        internal static RectTransform G2_Unit(Transform p) => UI.VStack(p, 8, null, TextAnchor.UpperLeft, "unit");
        internal static RectTransform G2_Uhead(Transform p, string left, string rightTm = null)
        {
            var r = UI.HStack(p, 8, TextAnchor.MiddleLeft, null, "uhead");
            var l = UI.Txt(r, left, 14, G2_UheadCol, false, TextAnchor.MiddleLeft, true, true); UI.Size(l, -1, -1, 1);
            if (rightTm != null) G2_Tm(r, rightTm);
            return r;
        }
        internal static Text G2_Note(Transform p, string s, TextAnchor align = TextAnchor.UpperLeft)
        {
            var t = UI.Txt(p, s, 12.5f, UI.C.muted, true, align, true); t.lineSpacing = 1.2f; return t;
        }
        internal static Text G2_Tm(Transform p, string s) => UI.Txt(p, s, 10.5f, UI.C.muted, true, TextAnchor.MiddleLeft, false);
        internal static RectTransform G2_Urow(Transform p, TextAnchor align = TextAnchor.MiddleLeft) => UI.HStack(p, 7, align, null, "urow");

        // data-left geri sayımı (33-loop'taki [data-left] güncellemesinin karşılığı)
        internal static Text G2_Left(Transform p, double until, string pre, float size = 10.5f, bool shortFmt = false)
        {
            var t = UI.Txt(p, "", size, UI.C.muted, true, TextAnchor.MiddleLeft, false);
            var c = t.gameObject.AddComponent<G2Left>(); c.until = until; c.pre = pre; c.shortFmt = shortFmt; c.Tick();
            return t;
        }

        // ---------- .order / .who / .needs / .need ----------
        internal static RectTransform G2_Order(Transform p, Color? bg = null)
        {
            var box = UI.Box(p, bg ?? UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "order");
            box.GetComponent<Shadow>().effectColor = G2_Shadow;
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = UI.Pad(8, 10); v.spacing = 6; v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return box;
        }
        internal static RectTransform G2_Who(Transform order, string left, string right)
        {
            var r = UI.HStack(order, 8, TextAnchor.MiddleLeft, null, "who");
            var l = UI.Txt(r, left, 13.5f, UI.C.text, true); UI.Size(l, -1, -1, 1);
            if (right != null) UI.Txt(r, right, 12, UI.C.forest, true, TextAnchor.MiddleRight, false);
            return r;
        }
        internal static RectTransform G2_Needs(Transform p) => UI.HStack(p, 5, TextAnchor.MiddleLeft, null, "needs");
        internal static RectTransform G2_Need(Transform p, string emoji, string txt, bool miss, Color? bg = null, Color? fg = null)
        {
            var box = UI.Box(p, bg ?? (miss ? G2_MissBg : G2_NeedBg), 11, 1.5f, G2_NeedBorder, null, "need");
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = UI.Pad(4, 8, 1, 1); h.spacing = 2; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            if (emoji != null) UI.Txt(box, emoji, 15, null, false, TextAnchor.MiddleCenter, false);
            UI.Txt(box, txt, 12, fg ?? (miss ? UI.C.brick : UI.C.forest), true, TextAnchor.MiddleCenter, false);
            UI.Size(box, -1, -1, -1, -1, -1, 22);
            return box;
        }

        // ---------- .tiles / .tile ----------
        // 3 sütunlu ızgara (grid-template-columns:repeat(3,1fr); gap:7px)
        internal static RectTransform G2_Tiles(Transform p, int n, Action<Transform, int> build)
        {
            var col = UI.VStack(p, 7, null, TextAnchor.UpperLeft, "tiles");
            RectTransform row = null;
            for (int i = 0; i < n; i++)
            {
                if (i % 3 == 0)
                {
                    row = UI.HStack(col, 7, TextAnchor.UpperLeft, null, "trow");
                    row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                }
                var cell = UI.New("cell", row); UI.Size(cell, 0, -1, 1);
                var cv = cell.gameObject.AddComponent<VerticalLayoutGroup>(); cv.childControlWidth = cv.childControlHeight = true; cv.childForceExpandWidth = cv.childForceExpandHeight = true;
                build(cell, i);
            }
            if (row != null) for (int k = n % 3; k > 0 && k < 3; k++) { var e = UI.New("cell", row); UI.Size(e, 0, -1, 1); }
            return col;
        }
        internal static RectTransform G2_Tile(Transform p, bool on, bool enabled, Action onClick, float alpha = 1)
        {
            var box = UI.Box(p, on ? UI.C.honey : UI.C.card, 14, 2, UI.C.ink, new Vector2(2, 3), "tile");
            box.GetComponent<Shadow>().effectColor = G2_Shadow;
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = UI.Pad(3, 3, 6, 5); v.spacing = 2; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            if (onClick != null)
            {
                var b = UI.Clickable(box, () => { if (enabled) { UI.Boing(box); onClick(); } });
                b.interactable = enabled;
            }
            float a = enabled ? alpha : .55f * alpha;
            if (a < 1) (box.gameObject.GetOrAdd<CanvasGroup>()).alpha = a;
            return box;
        }
        internal static Text G2_Ins(Transform tile, string s) => UI.Txt(tile, "<b>" + s + "</b>", 11.5f, UI.C.text, true, TextAnchor.MiddleCenter, false);
        internal static Text G2_TileTm(Transform tile, string s, bool wrap = false) => UI.Txt(tile, s, 10.5f, UI.C.muted, true, TextAnchor.MiddleCenter, wrap);

        // ---------- .tabs ----------
        internal static RectTransform G2_Tabs(Transform p, string cur, string[] ids, string[] labels)
        {
            var box = UI.Box(p, UI.C.paper2, 17, 2, UI.C.ink, null, "tabs");
            var h = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = UI.Pad(3); h.spacing = 5; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = false;
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i]; bool sel = cur == id;
                var tb = sel ? UI.Box(box, UI.C.card, 14, 1.5f, UI.C.ink, null, "tab") : UI.Box(box, UI.C.clear, 14, 0, null, null, "tab");
                var tv = tb.gameObject.AddComponent<HorizontalLayoutGroup>(); tv.padding = UI.Pad(5); tv.childAlignment = TextAnchor.MiddleCenter;
                tv.childControlWidth = tv.childControlHeight = true; tv.childForceExpandWidth = true;
                UI.Txt(tb, labels[i], 12.5f, UI.C.text, true, TextAnchor.MiddleCenter, false);
                UI.Size(tb, 0, -1, 1);
                UI.Clickable(tb, () => Act("vtab", D("t", id)));
            }
            return box;
        }

        // ---------- .sh-body ----------
        internal static RectTransform G2_ShBody(Transform parent)
        {
            RectTransform content;
            var sc = UI.Scroll(parent, out content, 10, UI.Pad(12));
            UI.Size(sc, -1, -1, 1, 1);
            return content;
        }

        // ---------- .modal (.login / .mg) ----------
        // Döndürür: modal kökü (kapatmak için UI.Kill). card: içerik kartı. cls "login" ya da "mg" adla işaretlenir (.modal.login araması için)
        internal static RectTransform G2_Modal(string cls, out RectTransform card)
        {
            RectTransform c = null;
            var root = UI.Modal(k => { c = k; }, true, false, 320);
            root.name = "modal " + cls;
            var v = c.GetComponent<VerticalLayoutGroup>(); if (v != null) { v.padding = UI.Pad(22, 22, 24, 18); v.childAlignment = TextAnchor.UpperCenter; }
            card = c;
            return root;
        }
        internal static bool G2_ModalOpen(string cls)
        {
            if (UI.ModalLayer == null) return false;
            for (int i = 0; i < UI.ModalLayer.childCount; i++)
            {
                var ch = UI.ModalLayer.GetChild(i);
                if (ch.gameObject.activeSelf && (" " + ch.name + " ").Contains(" " + cls + " ")) return true;
            }
            return false;
        }
        internal static Text G2_H2(Transform card, string s, float size = 30) => UI.Txt(card, s, size, UI.C.forest, false, TextAnchor.MiddleCenter, true, true);
        internal static Text G2_P(Transform card, string s) { var t = UI.Txt(card, s, 14, G2_ModalP, true, TextAnchor.MiddleCenter, true); t.lineSpacing = 1.25f; return t; }
        internal static RectTransform G2_Badge(Transform card, string s)
        {
            var wrap = UI.HStack(card, 0, TextAnchor.MiddleCenter, UI.Pad(0, 0, 0, 10), "badgeWrap");
            var b = UI.Box(wrap, UI.C.honey, 40, 3, UI.C.ink, null, "badge");
            UI.Size(b, 80, 80);
            // radial-gradient(#ffe9a3 → honey → honey-dk): yuvarlak iç zemin maske olur, gradyan onun içinde
            var inner = b.Find("bg");
            var gm = inner.gameObject.AddComponent<Mask>(); gm.showMaskGraphic = true;
            G2_Gradient(inner, UI.Hex("#ffe9a3"), UI.C.honeyDk, false);
            var t = UI.Txt(b, s, 36, UI.Hex("#6b4100"), false, TextAnchor.MiddleCenter, false, true); UI.Stretch(t.rectTransform);
            UI.IgnoreLayout(t);
            Game.SetTimeout(() => { if (b) UI.Boing(b); }, 200);
            return b;
        }
        internal static Text G2_MgFace(Transform p, string s) => UI.Txt(p, s, 52, null, false, TextAnchor.MiddleCenter, false);
        internal static Text G2_MgScore(Transform p, string s) => UI.Txt(p, s, 20, UI.C.ink, false, TextAnchor.MiddleCenter, false, true);
    }

    // Geri sayım yazısı: pre + ' ' + secs(kalan)
    public class G2Left : MonoBehaviour
    {
        public double until; public string pre = ""; public bool shortFmt;
        float acc;
        Text t;
        public void Tick()
        {
            if (t == null) t = GetComponent<Text>();
            if (t == null) return;
            var sec = Math.Max(0, Math.Ceiling((until - Game.Now()) / 1000));
            t.text = pre + " " + (shortFmt ? Game.SecShort(sec) : Game.Secs(sec));
        }
        void Update() { acc += Time.unscaledDeltaTime; if (acc >= 1) { acc = 0; Tick(); } }
    }

    // Yinelenen çizgili doku (yarış şeridi): genişliğe göre uvRect ayarlanır
    public class G2Tiled : MonoBehaviour
    {
        public float period = 44; public bool vertical;
        RawImage ri; RectTransform rt;
        void Update()
        {
            if (ri == null) { ri = GetComponent<RawImage>(); rt = (RectTransform)transform; }
            if (ri == null) return;
            var r = rt.rect; var u = ri.uvRect;
            if (vertical) u.height = Mathf.Max(.01f, r.height / period); else u.width = Mathf.Max(.01f, r.width / period);
            ri.uvRect = u;
        }
    }
}
