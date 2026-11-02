// ================= KAMERA VE GEÇİŞLER (10-camera.js) =================
// Tüm vektörler THREE koordinatındadır (Ctl.target / Ctl.position).
using System;
using UnityEngine;

namespace Ciftlik
{
    public class Cam_Fly { public double t0; public Vector3 ft, fp, tt, tp, off; public double arc; }
    public class Cam_Saved { public Vector3 t, p; }

    public static partial class Game
    {
        public static Cam_Fly Fly = null;
        public static Cam_Saved HubSaved = null;
        public static string HoverLoc = null;
        public static readonly Vector3 FOCUS_DIR = new Vector3(.16f, .8f, .58f).normalized;

        public static void FlyCam(Vector3 tt, Vector3 tp) { Fly = new Cam_Fly { t0 = Now(), ft = Ctl.target, fp = Ctl.position, tt = tt, tp = tp }; }
        public static void RotateCam(double a) { var off = Ctl.position - Ctl.target; Fly = new Cam_Fly { t0 = Now(), ft = Ctl.target, fp = Ctl.position, tt = Ctl.target, arc = a, off = off }; }
        public static void ZoomCam(double f)
        {
            var off = Ctl.position - Ctl.target; double d = Math.Max(Ctl.minDistance, Math.Min(Ctl.maxDistance, off.magnitude * f));
            FlyCam(Ctl.target, Ctl.target + off.normalized * (float)d);
        }
        // Y ekseni çevresinde (sağ el, THREE) döndürme
        static Vector3 Cam_RotY(Vector3 v, double a) { float c = (float)Math.Cos(a), s = (float)Math.Sin(a); return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c); }
        public static void StepFly()
        {
            if (Fly == null) return;
            double k = S != null && S.fastNav ? 1 : Math.Min(1, (Now() - Fly.t0) / 420), e = k < .5 ? 4 * k * k * k : 1 - Math.Pow(-2 * k + 2, 3) / 2;
            Ctl.target = Vector3.LerpUnclamped(Fly.ft, Fly.tt, (float)e);
            if (Fly.arc != 0) Ctl.position = Ctl.target + Cam_RotY(Fly.off, Fly.arc * e);
            else Ctl.position = Vector3.LerpUnclamped(Fly.fp, Fly.tp, (float)e);
            if (k >= 1) Fly = null;
        }
        public static void GoLocation(string k)
        {
            if (Mode == "desk") { EnterWorld(() => GoLocation(k)); return; }
            var L = LOC[k]; if (!Owned(L.reg)) { OpenRegion(L.reg); return; }
            if (Mode == "interior") { Fade(() => { Mode = "hub"; DropInterior(); Ctl.enabled = true; Ictl.enabled = false; GoLocation(k); }); return; }
            if (Mode == "hub") HubSaved = new Cam_Saved { t = Ctl.target, p = Ctl.position };
            if (LocGroups.TryGetValue(k, out var lg) && lg != null && Mode == "hub") W_UD(lg).pop = .55;
            Mode = "focus"; Focus = k; View = new View { k = "loc", loc = k }; SetModeUI(); RenderSheet();
            double d = L.r * 2.3 + 9; Ctl.enablePan = false; Ctl.minDistance = (float)(d * .55); Ctl.maxDistance = (float)(d * 1.5);
            var tt = new Vector3(L.p[0], 0, L.p[2]); FlyCam(tt, tt + FOCUS_DIR * (float)d); SyncMarks();
        }
        public static void ToHub()
        {
            Action go = () =>
            {
                Mode = "hub"; Focus = null; View = null; DropInterior(); Ctl.enabled = true; Ictl.enabled = false; HubLimits(); SetModeUI(); RenderSheet(); SyncMarks();
                var h = HubSaved ?? new Cam_Saved { t = HUB_T, p = HUB_T + HUB_DIR * HUB_D }; FlyCam(h.t, h.p);
            };
            if (Mode == "interior") Fade(go); else go();
        }
        public static void OpenRegion(string r)
        {
            if (r == null || !REG.ContainsKey(r) || REG[r].rect == null) return;
            if (Mode == "desk") { EnterWorld(() => OpenRegion(r)); return; }
            if (Mode != "hub") ToHub();
            View = new View { k = "region", r = r }; RenderSheet();
            var d = REG[r]; float x1 = d.rect[0], z1 = d.rect[1], x2 = d.rect[2], z2 = d.rect[3]; var tt = new Vector3((x1 + x2) / 2, 0, (z1 + z2) / 2);
            FlyCam(tt, tt + HUB_DIR * Mathf.Max(50, Vector3.Distance(Ctl.position, Ctl.target) * .8f));
        }
    }
}
