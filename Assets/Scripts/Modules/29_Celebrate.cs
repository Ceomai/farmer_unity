// ================= HASAT KUTLAMASI (29-celebrate.js) =================
// Büyük yazı + 46 konfeti + sahne sarsıntısı (#stage.shake → kamera izdüşümü kaydırma, World_CamHook).
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public static partial class Game
    {
        static double celebT = 0;
        public static Vector2 Cel_ShakeOff = Vector2.zero; public static float Cel_ShakeRot = 0;
        // @keyframes shake (.5s): yüzde, x, y, açı
        static readonly float[][] Cel_SHAKE = { new[] { 0f, 0, 0, 0 }, new[] { .2f, -6, 3, -.4f }, new[] { .4f, 6, -3, .4f }, new[] { .6f, -4, 2, 0 }, new[] { .8f, 3, -1, 0 }, new[] { 1f, 0, 0, 0 } };
        static float Cel_Ease(float t) => W_Bezier(.25f, .1f, .25f, 1, t);   // CSS "ease"
        static void Cel_Shake()
        {
            foreach (var c in new[] { Cam, Icam, Dcam }) if (c != null && c.GetComponent<World_CamHook>() == null) c.gameObject.AddComponent<World_CamHook>();
            UI.Tween(.5f, k =>
            {
                int i = 0; while (i < Cel_SHAKE.Length - 2 && k > Cel_SHAKE[i + 1][0]) i++;
                var a = Cel_SHAKE[i]; var b = Cel_SHAKE[i + 1]; float u = Cel_Ease(Mathf.Clamp01((k - a[0]) / (b[0] - a[0])));
                Cel_ShakeOff = new Vector2(Mathf.Lerp(a[1], b[1], u), Mathf.Lerp(a[2], b[2], u)); Cel_ShakeRot = Mathf.Lerp(a[3], b[3], u);
            }, () => { Cel_ShakeOff = Vector2.zero; Cel_ShakeRot = 0; });
        }
        public static void Celebrate(string txt = null)
        {
            if (Now() - celebT < 4000) return; celebT = Now(); Sfx("win"); Cel_Shake();
            float W = UI.W, H = UI.H;
            var w = UI.Txt(UI.ToastLayer, "<b>" + (string.IsNullOrEmpty(txt) ? "Bereketli!" : txt) + "</b>", Mathf.Clamp(W * .09f, 38, 72), UI.C.honey, false, TextAnchor.MiddleCenter, false, true);
            w.raycastTarget = false; var ol = w.gameObject.AddComponent<Outline>(); ol.effectColor = UI.C.ink; ol.effectDistance = new Vector2(2, -2);
            var sh = w.gameObject.AddComponent<Shadow>(); sh.effectColor = UI.C.ink; sh.effectDistance = new Vector2(4, -5);
            var wr = w.rectTransform; wr.anchorMin = wr.anchorMax = wr.pivot = new Vector2(.5f, .5f); wr.sizeDelta = new Vector2(W, 120); wr.anchoredPosition = Vector2.zero; wr.localRotation = Quaternion.Euler(0, 0, 6);
            // @keyframes celeb: 0 (.2,0) 20% (1.15,1) 35% (1) 80% (op 1) 100% (op 0, -30px); cubic-bezier(.2,1.6,.4,1) her aralıkta
            UI.Tween(1.6f, k =>
            {
                if (!wr) return; float s, op, dy = 0;
                if (k < .2f) { float u = W_Bezier(.2f, 1.6f, .4f, 1, k / .2f); s = Mathf.LerpUnclamped(.2f, 1.15f, u); op = Mathf.LerpUnclamped(0, 1, u); }
                else if (k < .35f) { float u = W_Bezier(.2f, 1.6f, .4f, 1, (k - .2f) / .15f); s = Mathf.LerpUnclamped(1.15f, 1, u); op = 1; }
                else if (k < .8f) { s = 1; op = 1; }
                else { float u = W_Bezier(.2f, 1.6f, .4f, 1, (k - .8f) / .2f); s = 1; op = Mathf.Clamp01(1 - u); dy = 30 * u; }
                wr.localScale = Vector3.one * s; wr.anchoredPosition = new Vector2(0, dy); w.color = UI.A(UI.C.honey, op);
                ol.effectColor = UI.A(UI.C.ink, op); sh.effectColor = UI.A(UI.C.ink, op);
            });
            string[] cols = { "#c4492e", "#f2b330", "#5aa047", "#3f6fa0", "#e8657a", "#fff6e0" };
            for (int i = 0; i < 46; i++)
            {
                var c = UI.Box(UI.ToastLayer, UI.Hex(cols[i % cols.Length]), 2, 1.5f, UI.C.ink, null, "conf");
                foreach (var g in c.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
                float left = (50 + (float)(Rnd() - .5) * 20) / 100 * W, top = .55f * H;
                UI.PlaceTL(c, left, top, 10, 14); c.pivot = new Vector2(.5f, .5f); c.anchoredPosition = new Vector2(left + 5, -(top + 7));
                var cg = c.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
                float dx = (float)(Rnd() - .5) * 110 / 100 * W, dyv = (-30 - (float)Rnd() * 45) / 100 * H, rr = (float)(Rnd() * 900 - 450), delay = (float)(Rnd() * .12);
                var start = c.anchoredPosition; var cc = c;
                SetTimeout(() => UI.Tween(1.8f, k =>
                {
                    if (!cc) return; float e = W_Bezier(.15f, .7f, .4f, 1, k);
                    cc.anchoredPosition = start + new Vector2(dx * e, -dyv * e); cc.localRotation = Quaternion.Euler(0, 0, -rr * e); cg.alpha = 1 - e;
                }), delay * 1000);
                SetTimeout(() => UI.Kill(cc), 1900);
            }
            SetTimeout(() => UI.Kill(wr), 1700); PLATFORM.hap("heavy");
        }
    }
}
