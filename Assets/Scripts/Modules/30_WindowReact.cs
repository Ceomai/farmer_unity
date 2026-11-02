// ================= PENCERE TEPKİSİ (30-window-react.js) =================
// Masadaki pencere kamerası (WinCam, 25-desk) son yapılan işe döner; yeni binalarda iskele görünür.
using System;
using UnityEngine;

namespace Ciftlik
{
    public partial class Recent { public bool puffed; }

    public static partial class Game
    {
        public static void MarkRecent(string locKey, string n) { S.recent = new Recent { loc = locKey, n = n, at = Now() }; }
        public static readonly Cam_Saved WIN_HOME = new Cam_Saved { p = new Vector3(2, 24, 40), t = new Vector3(2, 0, -4) };   // THREE koordinatı
        public static string WinFocus = null;
        public static Vector3 WinLook = new Vector3(2, 0, -4);
        public static void StepWinCam(double dt)
        {
            var rc = S.recent; Vector3 tp = WIN_HOME.p, tt = WIN_HOME.t;
            if (rc != null && Now() - rc.at < 60000 && rc.loc != null && LOC.ContainsKey(rc.loc))
            {
                var L = LOC[rc.loc]; tt = new Vector3(L.p[0], 0, L.p[2]); tp = tt + new Vector3(0, 16, 22);
                if (!rc.puffed && Mode == "desk") { rc.puffed = true; Puff(new Vector3(L.p[0], 1, L.p[2]), 16, 1.6); }
            }
            float k = (float)Math.Min(1, dt * 1.2);
            if (WinCam == null) return;
            var wp = ToThree(WinCam.transform.position); wp = Vector3.Lerp(wp, tp, k); WinCam.transform.position = V(wp.x, wp.y, wp.z);
            WinLook = Vector3.Lerp(WinLook, tt, k); WinCam.transform.LookAt(V(WinLook.x, WinLook.y, WinLook.z), Vector3.up);
        }
        public static void Scaffold(Transform ug, Unit st)
        {
            if (st.built == null || st.built.Value == 0 || Now() - st.built.Value > 9000) return; var g = G(ug); int c = 0xc9a05a;
            foreach (var xz in new[] { new[] { -1.7, -1.7 }, new[] { 1.7, -1.7 }, new[] { -1.7, 1.7 }, new[] { 1.7, 1.7 } }) P(g, "box", c, W_V(.1, 3.2, .1), W_V(xz[0], 1.6, xz[1]));
            foreach (var y in new[] { 1.1, 2.2 }) foreach (var a in new[] { new[] { 0, -1.7, 3.5, .1 }, new[] { 0, 1.7, 3.5, .1 }, new[] { -1.7, 0, .1, 3.5 }, new[] { 1.7, 0, .1, 3.5 } }) P(g, "box", 0x9a7247, W_V(a[2], .08, a[3]), W_V(a[0], y, a[1]));
            var hm = G(g, W_V(1.7, 2.4, 1.7)); P(hm, "box", 0x6e4a2a, W_V(.05, .4, .05), W_V(0, .2, 0)); P(hm, "box", 0x707680, W_V(.2, .1, .1), W_V(0, .42, 0));
            AddFx(ug, (dt, t) =>
            {
                if (!g) return; double a = Now() - st.built.GetValueOrDefault(); bool vis = a < 9000; if (g.gameObject.activeSelf != vis) g.gameObject.SetActive(vis);
                if (a > 7000) W_Scale(g, Math.Max(.01, 1 - (a - 7000) / 2000)); SetRot(hm, 0, 0, Math.Sin(t * 14) * .7);
            });
        }
    }
}
