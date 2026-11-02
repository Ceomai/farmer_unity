// ================= DOM KARŞILIĞI (G6) =================
// Web sürümünde modüller arayüz öğelerine document.querySelector ile ulaşır (#backBtn, .scard[data-go="tarla"], [data-left]...).
// uGUI'de aynı işi bu küçük etiket bileşeni görür: arayüzü kuran kod öğeye id / sınıf / data-* bilgisi takar (Dom.Tag),
// diğer modüller Dom.Q("seçici") / Dom.QAll("seçici") ile bulur. Desteklenen seçici: etiket adı (yok sayılır), #id, .sınıf, [data-x], [data-x="y"],
// boşlukla ayrılmış soy (descendant) zinciri. body sınıfları (hub, focus, inside, desk, photo, sheet-open, wx-rain...) Dom.Body üzerindedir.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class Dom : MonoBehaviour
    {
        public string id;
        public readonly HashSet<string> cls = new HashSet<string>();
        public readonly Dictionary<string, string> data = new Dictionary<string, string>();
        public string title;
        public event Action<string, bool> OnClass;   // (sınıf, açık mı)

        static Dom _body;
        // <body>: arayüz kökü. Sınıfları (hub/focus/inside/desk/photo/sheet-open/fs-b/lefty/wx-rain...) arayüz düzenini belirler.
        public static Dom Body
        {
            get
            {
                if (_body == null && UI.Root != null) { _body = UI.Root.gameObject.GetOrAdd<Dom>(); _body.id = "body"; }
                return _body;
            }
        }
        // body.dataset.mode
        public static string BodyMode { get { var b = Body; string v; return b != null && b.data.TryGetValue("mode", out v) ? v : null; } }

        public bool Has(string c) => cls.Contains(c);
        public Dom Toggle(string c, bool on)
        {
            bool had = cls.Contains(c);
            if (on) cls.Add(c); else cls.Remove(c);
            if (had != on && OnClass != null) { try { OnClass(c, on); } catch (Exception e) { Debug.LogException(e); } }
            return this;
        }
        public Dom Toggle(string c) => Toggle(c, !cls.Contains(c));
        public Dom Add(string c) => Toggle(c, true);
        public Dom Remove(string c) => Toggle(c, false);

        public bool Hidden
        {
            get => !gameObject.activeSelf;
            set { if (gameObject.activeSelf == value) gameObject.SetActive(!value); }
        }
        // textContent
        public string Text
        {
            get { var t = GetComponentInChildren<Text>(true); return t != null ? t.text : ""; }
            set { var t = GetComponentInChildren<Text>(true); if (t != null) t.text = value ?? ""; }
        }
        public string Data(string k) { string v; return data.TryGetValue(k, out v) ? v : null; }
        public void Click() { var b = GetComponent<Button>(); if (b != null && b.interactable) b.onClick.Invoke(); }
        public void RemoveEl() { Destroy(gameObject); }

        // getBoundingClientRect(): CSS px, sol üstten (x,y,genişlik,yükseklik)
        public Rect Rect()
        {
            var rt = transform as RectTransform; if (rt == null) return new Rect(0, 0, 0, 0);
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            float s = UI.Scale <= 0 ? 1 : UI.Scale;
            float x0 = c[0].x / s, x1 = c[2].x / s, yTop = (Screen.height - c[1].y) / s, yBot = (Screen.height - c[0].y) / s;
            return new Rect(x0, yTop, x1 - x0, yBot - yTop);
        }
        // offsetParent!==null karşılığı: görünür mü
        public bool Visible => gameObject.activeInHierarchy;

        // Saydamlıkla kaybolup silinme (bt.style.opacity='0' + remove)
        public void FadeOut(float sec, bool remove = true)
        {
            var cg = gameObject.GetOrAdd<CanvasGroup>();
            var go = gameObject;
            UI.Tween(sec, k => { if (cg) cg.alpha = 1 - k; }, () => { if (remove && go) Destroy(go); });
        }

        // ---------- Etiketleme ----------
        // Dom.Tag(öğe, ".scard.has-qa#id[data-go=tarla]") — seçici biçiminde id/sınıf/data yazılır.
        public static Dom Tag(Component c, string sel)
        {
            if (c == null) return null;
            var d = c.gameObject.GetOrAdd<Dom>();
            var cp = Compound.Parse(sel ?? "");
            if (cp.id != null) d.id = cp.id;
            foreach (var k in cp.cls) d.cls.Add(k);
            foreach (var kv in cp.attrs) d.data[kv.Key] = kv.Value ?? "";
            return d;
        }
        public static Dom Of(Component c) => c == null ? null : (c.gameObject.GetOrAdd<Dom>());

        // ---------- Sorgu ----------
        public static Dom Q(string sel)
        {
            var all = AllDom(); if (all == null) return null;
            var chain = ParseChain(sel);
            foreach (var d in all) if (d != null && Matches(d, chain)) return d;
            return null;
        }
        public static List<Dom> QAll(string sel)
        {
            var res = new List<Dom>(); var all = AllDom(); if (all == null) return res;
            var chain = ParseChain(sel);
            foreach (var d in all) if (d != null && Matches(d, chain)) res.Add(d);
            return res;
        }
        // el.matches(sel)
        public bool Is(string sel) => Matches(this, ParseChain(sel));
        // Bir öğenin içinde ara (el.querySelector)
        public Dom QIn(string sel)
        {
            var chain = ParseChain(sel);
            foreach (var d in GetComponentsInChildren<Dom>(true)) if (d != this && Matches(d, chain)) return d;
            return null;
        }
        public List<Dom> QAllIn(string sel)
        {
            var res = new List<Dom>(); var chain = ParseChain(sel);
            foreach (var d in GetComponentsInChildren<Dom>(true)) if (d != this && Matches(d, chain)) res.Add(d);
            return res;
        }
        // el.closest(sel)
        public Dom Closest(string sel)
        {
            var cp = ParseChain(sel);
            for (var t = transform; t != null; t = t.parent) { var d = t.GetComponent<Dom>(); if (d != null && Matches(d, cp)) return d; }
            return null;
        }

        static Dom[] AllDom()
        {
            if (UI.Root == null) return null;
            return UI.Root.GetComponentsInChildren<Dom>(true);   // belge sırası
        }

        class Compound
        {
            public string id; public readonly List<string> cls = new List<string>(); public readonly Dictionary<string, string> attrs = new Dictionary<string, string>();
            public bool body;
            public static Compound Parse(string s)
            {
                var c = new Compound(); int i = 0; s = s.Trim();
                // etiket adı
                int st = i; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '-' || s[i] == '_')) i++;
                if (i > st && s.Substring(st, i - st) == "body") c.body = true;
                while (i < s.Length)
                {
                    char ch = s[i];
                    if (ch == '#' || ch == '.')
                    {
                        i++; int a = i; while (i < s.Length && s[i] != '.' && s[i] != '#' && s[i] != '[') i++;
                        var name = s.Substring(a, i - a);
                        if (ch == '#') c.id = name; else c.cls.Add(name);
                    }
                    else if (ch == '[')
                    {
                        int end = s.IndexOf(']', i); if (end < 0) end = s.Length;
                        var inner = s.Substring(i + 1, end - i - 1); i = end + 1;
                        int eq = inner.IndexOf('=');
                        string k = eq < 0 ? inner.Trim() : inner.Substring(0, eq).Trim(), v = eq < 0 ? null : inner.Substring(eq + 1).Trim().Trim('"', '\'');
                        if (k.StartsWith("data-")) k = k.Substring(5);
                        if (k == "id") c.id = v; else c.attrs[k] = v;
                    }
                    else i++;
                }
                return c;
            }
            public bool Match(Dom d)
            {
                if (body && d != _body) return false;
                if (id != null && d.id != id) return false;
                foreach (var k in cls) if (!d.cls.Contains(k)) return false;
                foreach (var kv in attrs) { string v; if (!d.data.TryGetValue(kv.Key, out v)) return false; if (kv.Value != null && v != kv.Value) return false; }
                return true;
            }
        }
        static List<Compound> ParseChain(string sel)
        {
            var parts = new List<Compound>();
            if (string.IsNullOrEmpty(sel)) return parts;
            // tırnak içindeki boşlukları koru
            var sb = new StringBuilder(); bool inq = false, inb = false;
            foreach (var ch in sel)
            {
                if (ch == '"') inq = !inq; if (ch == '[') inb = true; if (ch == ']') inb = false;
                if (ch == ' ' && !inq && !inb) { if (sb.Length > 0) { parts.Add(Compound.Parse(sb.ToString())); sb.Length = 0; } continue; }
                if (ch == '>' && !inq && !inb) continue;
                sb.Append(ch);
            }
            if (sb.Length > 0) parts.Add(Compound.Parse(sb.ToString()));
            return parts;
        }
        static bool Matches(Dom d, List<Compound> chain)
        {
            if (chain.Count == 0) return false;
            if (!chain[chain.Count - 1].Match(d)) return false;
            int ci = chain.Count - 2;
            for (var t = d.transform.parent; t != null && ci >= 0; t = t.parent)
            {
                var a = t.GetComponent<Dom>(); if (a != null && chain[ci].Match(a)) ci--;
            }
            return ci < 0;
        }
    }
}
