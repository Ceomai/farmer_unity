// ================= CSS DÜZEN KARŞILIKLARI (G6) =================
// FlowLayout  = display:flex; flex-wrap:wrap (.urow, .needs, .ins, .queue, .pills)
// ColsLayout  = display:grid; grid-template-columns:repeat(N,minmax(0,1fr)) (.tiles, .plots, .places, .inv, .cal)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class FlowLayout : LayoutGroup
    {
        public float gapX = 7, gapY = 7;
        public int justify;            // 0 başa, 1 ortaya, 2 sona yasla
        public bool stretchLast;       // son satır öğesi kalan genişliği alsın mı (kullanılmaz: CSS'te yok)

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float min = 0, pref = 0; int n = 0;
            foreach (var c in rectChildren)
            {
                min = Mathf.Max(min, LayoutUtility.GetMinSize(c, 0));
                pref += LayoutUtility.GetPreferredSize(c, 0); n++;
            }
            if (n > 1) pref += gapX * (n - 1);
            SetLayoutInputForAxis(min + padding.horizontal, pref + padding.horizontal, -1, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            float h = Run(false);
            SetLayoutInputForAxis(h, h, -1, 1);
        }
        public override void SetLayoutHorizontal() { Run(true, 0); }
        public override void SetLayoutVertical() { Run(true, 1); }

        float Run(bool apply, int axis = -1)
        {
            float W = rectTransform.rect.width - padding.horizontal;
            if (W <= 0) W = 100000;
            float y = padding.top, total = 0;
            var row = new List<RectTransform>(); var ws = new List<float>(); float rowW = 0;
            var all = rectChildren; int i = 0;
            while (i <= all.Count)
            {
                RectTransform c = i < all.Count ? all[i] : null;
                float w = 0;
                if (c != null) { w = Mathf.Min(LayoutUtility.GetPreferredSize(c, 0), W); w = Mathf.Max(w, Mathf.Min(LayoutUtility.GetMinSize(c, 0), W)); }
                bool flush = c == null || (row.Count > 0 && rowW + gapX + w > W + .5f);
                if (flush && row.Count > 0)
                {
                    float rh = 0; foreach (var r in row) rh = Mathf.Max(rh, LayoutUtility.GetPreferredSize(r, 1));
                    float x = padding.left + (justify == 1 ? (W - rowW) / 2 : justify == 2 ? W - rowW : 0);
                    for (int k = 0; k < row.Count; k++)
                    {
                        float ch = LayoutUtility.GetPreferredSize(row[k], 1);
                        if (apply && axis == 0) SetChildAlongAxis(row[k], 0, x, ws[k]);
                        if (apply && axis == 1) SetChildAlongAxis(row[k], 1, y + (rh - ch) / 2, ch);
                        x += ws[k] + gapX;
                    }
                    y += rh + gapY; total = y - gapY;
                    row.Clear(); ws.Clear(); rowW = 0;
                }
                if (c == null) break;
                row.Add(c); ws.Add(w); rowW += (row.Count > 1 ? gapX : 0) + w;
                i++;
            }
            return (all.Count > 0 ? total : padding.top) + padding.bottom;
        }
    }

    public class ColsLayout : LayoutGroup
    {
        public int cols = 3;
        public float gapX = 7, gapY = 7;
        public float aspect;     // >0 ise hücre yüksekliği = genişlik / aspect (aspect-ratio)
        public bool sameRowHeight = true;

        float CellW => Mathf.Max(0, (rectTransform.rect.width - padding.horizontal - gapX * (cols - 1)) / Mathf.Max(1, cols));

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float min = 0; foreach (var c in rectChildren) min = Mathf.Max(min, LayoutUtility.GetMinSize(c, 0));
            SetLayoutInputForAxis(padding.horizontal, padding.horizontal + min * cols, 1, 0);
        }
        public override void CalculateLayoutInputVertical() { float h = Run(false, -1); SetLayoutInputForAxis(h, h, -1, 1); }
        public override void SetLayoutHorizontal() { Run(true, 0); }
        public override void SetLayoutVertical() { Run(true, 1); }

        float Run(bool apply, int axis)
        {
            var all = rectChildren; float cw = CellW, y = padding.top;
            for (int r = 0; r * cols < all.Count; r++)
            {
                float rh = 0;
                for (int k = 0; k < cols && r * cols + k < all.Count; k++)
                    rh = Mathf.Max(rh, aspect > 0 ? cw / aspect : LayoutUtility.GetPreferredSize(all[r * cols + k], 1));
                for (int k = 0; k < cols && r * cols + k < all.Count; k++)
                {
                    var c = all[r * cols + k];
                    if (apply && axis == 0) SetChildAlongAxis(c, 0, padding.left + k * (cw + gapX), cw);
                    if (apply && axis == 1) SetChildAlongAxis(c, 1, y, sameRowHeight || aspect > 0 ? rh : LayoutUtility.GetPreferredSize(c, 1));
                }
                y += rh + gapY;
            }
            return (all.Count > 0 ? y - gapY : y) + padding.bottom;
        }
    }
}
