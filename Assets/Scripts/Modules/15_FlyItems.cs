// ================= UÇAN ÜRÜNLER (15-fly-items.js) =================
// Toplanan ürün/para emojisi ekrandaki noktadan HUD hapına (#pStore / #pCoin) uçar, hap "boing" yapar.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public static partial class Game
    {
        // x,y: CSS px (sol üst); sel: HUD öğesi seçicisi ("#pStore", "#pCoin")
        public static void FlyOne(float x, float y, string emo, string sel, double delay = 0)
        {
            SetTimeout(() =>
            {
                var dom = Dom.Q(sel); if (dom == null) return; var tg = (RectTransform)dom.transform; var r = dom.Rect();
                var d = UI.Txt(UI.ToastLayer, emo, 26, null, false, TextAnchor.MiddleCenter, false); var rt = d.rectTransform;
                var sh = d.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, .3f); sh.effectDistance = new Vector2(0, -2);
                UI.PlaceTL(rt, x, y, 40, 40); rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = new Vector2(x, -y);
                var start = rt.anchoredPosition; var end = new Vector2(r.x + 16, -(r.y + r.height / 2));
                Host.Run(Fly_Frames(2, () => UI.Tween(.65f, k =>
                {
                    if (!rt) return; float e = W_Bezier(.55f, -.45f, .7f, 1, k);
                    rt.anchoredPosition = Vector2.LerpUnclamped(start, end, e); rt.localScale = Vector3.one * Mathf.LerpUnclamped(1, .55f, e);
                    d.color = UI.A(d.color, Mathf.LerpUnclamped(1, .5f, e));
                })));
                SetTimeout(() => { UI.Kill(rt); if (tg) UI.Boing(tg); }, 700);
            }, delay);
        }
        static System.Collections.IEnumerator Fly_Frames(int n, Action a) { for (int i = 0; i < n; i++) yield return null; a(); }
        public static void FlyItems(float x, float y, Dictionary<string, int> @out)
        {
            int i = 0;
            foreach (var kv in @out) for (int j = 0; j < Math.Min(kv.Value, 4); j++) FlyOne(x + (float)(Rnd() - .5) * 30, y, ITEMS[kv.Key].e, "#pStore", (i++) * 70);
        }
        public static void FlyCoins(int n = 5) { for (int i = 0; i < n; i++) FlyOne((float)(LastP.x + (Rnd() - .5) * 40), (float)LastP.y, "🪙", "#pCoin", i * 60); }
    }
}
