// 06-engine.js modelleri: yapı parçaları (çatı, pencere, kapı, çit), bitki örtüsü, hayvanlar ve canlandırmaları, ekinler, tarla,
// evler (4 seviye), atölyeler, MODELS (tüm binalar), insanlar, dekorasyon, panayır/pazar/han, kuyu, korkuluk, temel, yarış pisti.
// Tüm ölçü/renk/konumlar JS ile birebir (Three koordinatı; P/G/SetPos çevirir). Game'deki genel adlar dosya sonunda.
using System;
using System.Collections.Generic;
using UnityEngine;
using static Ciftlik.Game;

namespace Ciftlik
{
    static class EngM
    {
        const double PI = Math.PI;
        static double[] A(double x, double y, double z) => new[] { x, y, z };
        static double Sin(double a) => Math.Sin(a);
        static double Cos(double a) => Math.Cos(a);
        static double Hyp(double a, double b) => Math.Sqrt(a * a + b * b);
        static int[] C(params int[] c) => c;
        static bool Job(string unit) { Unit u; return S != null && S.units != null && S.units.TryGetValue(unit, out u) && u != null && u.job != null; }

        // --- yapı parçaları ---
        internal static Transform GableRoof(Transform par, double w, double d, double h, double y, int roofC, int wallC, string axis = "x", double ov = .28, double th = .12)
        {
            bool ax = axis == "x";
            double across = ax ? d : w, len = (ax ? w : d) + ov * 2, half = across / 2, k = h / half;
            foreach (var s in new[] { -1, 1 })
            {
                double a1 = s * (half + ov), y1 = y - ov * k, a2 = 0, y2 = y + h, L = Hyp(a2 - a1, y2 - y1), ma = (a1 + a2) / 2, my = (y1 + y2) / 2 + th / 2;
                if (ax) P(par, "box", roofC, A(len, th, L), A(0, my, ma), A(Math.Atan2(-(y2 - y1), (a2 - a1)), 0, 0));
                else P(par, "box", roofC, A(L, th, len), A(ma, my, 0), A(0, 0, Math.Atan2(y2 - y1, a2 - a1)));
            }
            if (ax) P(par, "box", roofC, A(len, .14, .16), A(0, y + h + .07, 0)); else P(par, "box", roofC, A(.16, .14, len), A(0, y + h + .07, 0));
            var gm = P(par, "tri", wallC, ax ? A(d, h, w) : A(w, h, d), A(0, y, 0));
            if (ax) SetRotY(gm, PI / 2);
            return gm;
        }
        internal static Transform WindowM(Transform par, double x, double y, double z, double w = .42, double h = .48, double ry = 0, double lit = .5)
        {
            var g = G(par, A(x, y, z), ry);
            P(g, "box", 0xf3ead8, A(w + .12, h + .12, .06), A(0, 0, 0));
            P(g, "box", 0xffd27a, A(w, h, .08), A(0, 0, .012), null, new MatOpts { emissive = 0xffa53a, emissiveIntensity = (float)lit });
            P(g, "box", 0xf3ead8, A(.04, h, .1), A(0, 0, .03)); P(g, "box", 0xf3ead8, A(w, .04, .1), A(0, 0, .03)); P(g, "box", 0xf3ead8, A(w + .2, .06, .14), A(0, -h / 2 - .06, .05));
            return g;
        }
        internal static Transform Door(Transform par, double x, double y, double z, double w = .5, double h = .85, int c = 0x6b3f20, double ry = 0)
        {
            var g = G(par, A(x, y, z), ry);
            P(g, "box", 0xf3ead8, A(w + .1, h + .06, .05), A(0, h / 2, 0)); P(g, "box", c, A(w, h, .08), A(0, h / 2, .01)); P(g, "sph", 0xe0b84a, A(.04, .04, .04), A(w * .32, h * .48, .07));
            return g;
        }
        internal static void FenceRun(Transform par, double x1, double z1, double x2, double z2, int c = 0x9a7247, double ht = .62)
        {
            double L = Hyp(x2 - x1, z2 - z1); int n = Math.Max(1, RoundI(L / .85)); double ang = Math.Atan2(z2 - z1, x2 - x1);
            for (int k = 0; k <= n; k++) { double t = (double)k / n; P(par, "box", c, A(.11, ht, .11), A(x1 + (x2 - x1) * t, ht / 2, z1 + (z2 - z1) * t)); }
            foreach (var hh in new[] { ht * .45, ht * .85 }) P(par, "box", c, A(L, .07, .05), A((x1 + x2) / 2, hh, (z1 + z2) / 2), A(0, -ang, 0));
        }
        internal static void Bush(Transform par, double x, double z, double s = 1, int c = 0x4f8f37)
        {
            P(par, "ico1", c, A(.45 * s, .38 * s, .45 * s), A(x, .3 * s, z)); P(par, "ico1", c, A(.3 * s, .28 * s, .3 * s), A(x + .28 * s, .22 * s, z + .1 * s));
        }
        internal static void Flowers(Transform par, double x, double z, double r, double seed)
        {
            var q = Rng(seed); var cs = C(0xf4d03f, 0xe8657a, 0xffffff, 0xb78be0);
            for (int k = 0; k < 7; k++)
            {
                double a = x + (q() - .5) * r, b = z + (q() - .5) * r;
                P(par, "cyl", 0x4f8f37, A(.015, .2, .015), A(a, .1, b)); P(par, "lsph", cs[k % 4], A(.07, .05, .07), A(a, .22, b));
            }
        }
        internal static void SwayFx(Transform par, Transform top, double x)
        {
            return; // akıcılık için kapalı: sallanan ağaçlar birleştirilemiyor, etkisi de çok küçüktü
            Action<float, float> fn = (dt, t) => Eng_SetRot3(top, Math.Cos(t * 1.1 + x * .21) * .03, 0, Math.Sin(t * 1.3 + x * .37) * .05);
            if (par == SCENE) SwayList.Add(fn); else AddFx(par, fn);
        }
        internal static void Pine(Transform par, double x, double z, double s = 1)
        {
            P(par, "cyl", 0x6b4423, A(.12 * s, .6 * s, .12 * s), A(x, .3 * s, z));
            var top = G(par, A(x, .5 * s, z)); var cs = C(0x2f6b3a, 0x3a7d42, 0x2a5f33);
            for (int k = 0; k < 3; k++) P(top, "cone6", cs[k], A((1 - k * .25) * .9 * s, 1.1 * s, (1 - k * .25) * .9 * s), A(0, (.4 + k * .6) * s, 0));
            SwayFx(par, top, x + z);
        }
        internal static void Oak(Transform par, double x, double z, double s = 1, bool apples = false)
        {
            P(par, "cyl", 0x6e4527, A(.16 * s, 1 * s, .16 * s), A(x, .5 * s, z));
            var top = G(par, A(x, .9 * s, z)); var c = C(0x5c9a3e, 0x4f8a36, 0x68a846);
            P(top, "ico1", c[0], A(1 * s, .85 * s, 1 * s), A(0, .55 * s, 0)); P(top, "ico1", c[1], A(.7 * s, .6 * s, .7 * s), A(.5 * s, .35 * s, .2 * s)); P(top, "ico1", c[2], A(.6 * s, .55 * s, .6 * s), A(-.4 * s, .85 * s, -.2 * s));
            if (apples)
            {
                var q = Rng(RoundI(x * 100 + z * 7 + 500));
                for (int k = 0; k < 9; k++) { double a = q() * 6.28, b = q() * 1.2; P(top, "lsph", 0xd43a2a, A(.1 * s, .1 * s, .1 * s), A(Cos(a) * .85 * s, .3 * s + b * .5 * s, Sin(a) * .85 * s)); }
            }
            SwayFx(par, top, x * 1.7 + z);
        }
        internal static void AddSmoke(Transform host, double[] ch, Func<bool> isOn, Transform fxHost = null)
        {
            var sm = new List<Transform>(); var mats = new List<Material>();
            for (int k = 0; k < 5; k++)
            {
                var mt = ToonMat(new MatOpts { color = 0xf2ede0, transparent = true, opacity = .7f });
                var m = Eng_Mesh(host, GE["ico1"], mt, false, false, "smoke"); Eng_Scale(m, .15);
                sm.Add(m); mats.Add(mt);
            }
            AddFx(fxHost ?? host, (dt, tm) =>
            {
                bool on = isOn();
                for (int k = 0; k < sm.Count; k++)
                {
                    var m = sm[k]; double ph = (tm * .35 + k / 5.0) % 1;
                    Eng_Visible(m, on);
                    SetPos(m, ch[0] + Sin(ph * 6 + k) * .15, ch[1] + ph * 2.2, ch[2] - ph * .4);
                    Eng_Scale(m, .14 + ph * .42); Eng_SetOpacity(mats[k], (float)(.65 * (1 - ph)));
                }
            });
        }

        // --- hayvanlar ---
        internal static Transform Chicken(int col = 0xfaf6ee)
        {
            var g = G(null);
            P(g, "sph", col, A(.22, .2, .28), A(0, .32, 0)); P(g, "box", col, A(.08, .18, .14), A(0, .42, -.24), A(-.5, 0, 0));
            var h = G(g, A(0, .5, .17));
            P(h, "sph", col, A(.12, .13, .12)); P(h, "box", 0xd8352a, A(.035, .09, .11), A(0, .13, 0)); P(h, "cone", 0xf2b134, A(.04, .11, .04), A(0, -.01, .13), A(PI / 2, 0, 0)); P(h, "lsph", 0xd8352a, A(.03, .05, .03), A(0, -.08, .08));
            P(h, "lsph", 0x1a1a1a, A(.018, .018, .018), A(.08, .03, .06)); P(h, "lsph", 0x1a1a1a, A(.018, .018, .018), A(-.08, .03, .06));
            foreach (var s in new[] { -1, 1 }) P(g, "cyl", 0xf2b134, A(.02, .16, .02), A(s * .07, .08, 0));
            UData.Of(g).head = h;
            return g;
        }
        internal static Transform Cow(int body = 0xf7f3ea, int spot = 0x2b2522, bool bull = false)
        {
            var g = G(null);
            P(g, "box", body, A(.62, .56, 1.15), A(0, .78, 0));
            P(g, "box", spot, A(.64, .3, .36), A(0, .86, .18)); P(g, "box", spot, A(.64, .24, .28), A(0, .72, -.32)); P(g, "box", spot, A(.3, .04, .3), A(.12, 1.07, -.05));
            var h = G(g, A(0, 1.02, .62));
            P(h, "box", body, A(.36, .36, .4), A(0, 0, .08)); P(h, "box", bull ? 0x5a3a2a : 0xe8a8a0, A(.38, .18, .16), A(0, -.1, .32)); P(h, "lsph", 0x111111, A(.04, .04, .04), A(.19, .07, .16)); P(h, "lsph", 0x111111, A(.04, .04, .04), A(-.19, .07, .16));
            foreach (var s in new[] { -1, 1 }) { P(h, "box", body, A(.18, .08, .1), A(s * .25, .1, 0)); P(h, "cone", 0xe9dcc0, bull ? A(.06, .26, .06) : A(.04, .14, .04), A(s * .16, .25, 0), A(0, 0, -s * (bull ? .9 : .3))); }
            foreach (var xz in new[] { A(.2, .42, 0), A(-.2, .42, 0), A(.2, -.42, 0), A(-.2, -.42, 0) }) { P(g, "box", body, A(.13, .52, .13), A(xz[0], .26, xz[1])); P(g, "box", 0x2a2420, A(.14, .08, .14), A(xz[0], .04, xz[1])); }
            if (!bull) P(g, "lsph", 0xf0b0a8, A(.15, .1, .15), A(0, .48, -.2));
            var tail = G(g, A(0, .95, -.58)); P(tail, "box", body, A(.05, .55, .05), A(0, -.27, 0)); P(tail, "box", spot, A(.09, .12, .09), A(0, -.55, 0));
            var u = UData.Of(g); u.head = h; u.tail = tail;
            return g;
        }
        internal static Transform Sheep()
        {
            var g = G(null); const int w = 0xf3efe6;
            P(g, "ico1", w, A(.42, .36, .55), A(0, .62, 0));
            foreach (var p in new[] { A(.2, .78, .2), A(-.2, .78, -.15), A(0, .85, -.3), A(.15, .6, -.4), A(-.2, .62, .3) }) P(g, "ico1", w, A(.22, .22, .22), p);
            var h = G(g, A(0, .82, .5));
            P(h, "box", 0x2b2522, A(.22, .26, .3), A(0, 0, .08)); P(h, "ico1", w, A(.16, .12, .14), A(0, .16, 0));
            foreach (var s in new[] { -1, 1 }) P(h, "box", 0x2b2522, A(.16, .06, .08), A(s * .16, .04, 0));
            foreach (var xz in new[] { A(.16, .3, 0), A(-.16, .3, 0), A(.16, -.3, 0), A(-.16, -.3, 0) }) P(g, "box", 0x2b2522, A(.09, .4, .09), A(xz[0], .2, xz[1]));
            UData.Of(g).head = h;
            return g;
        }
        internal static Transform Horse(int c = 0x8a5530, int m = 0x2e1d12, int? jockey = null)
        {
            var g = G(null);
            P(g, "box", c, A(.48, .52, 1.25), A(0, 1.12, 0));
            var n = G(g, A(0, 1.3, .52)); SetRot(n, -.55, 0, 0);
            P(n, "box", c, A(.26, .75, .32), A(0, .32, 0)); P(n, "box", m, A(.08, .75, .14), A(0, .34, -.16));
            var h = G(n, A(0, .72, .1)); SetRot(h, 1.15, 0, 0);
            P(h, "box", c, A(.24, .28, .58), A(0, 0, .15)); P(h, "box", 0x3a2a20, A(.22, .2, .12), A(0, -.03, .46));
            foreach (var k in new[] { -1, 1 }) { P(h, "box", c, A(.06, .16, .06), A(k * .08, .17, -.08)); P(h, "lsph", 0x111111, A(.035, .035, .035), A(k * .125, .06, .14)); }
            var legs = new List<Transform>();
            foreach (var xz in new[] { A(.17, .48, 0), A(-.17, .48, 0), A(.17, -.48, 0), A(-.17, -.48, 0) })
            {
                var l = G(g, A(xz[0], .86, xz[1])); P(l, "box", c, A(.13, .86, .13), A(0, -.43, 0)); P(l, "box", 0x2a2420, A(.14, .08, .14), A(0, -.82, 0)); legs.Add(l);
            }
            if (jockey != null) { P(g, "box", jockey.Value, A(.36, .5, .32), A(0, 1.62, .05)); P(g, "sph", 0xe8b48a, A(.14, .15, .14), A(0, 1.98, .1)); P(g, "hsph", jockey.Value, A(.16, .12, .16), A(0, 2.05, .1)); }
            var tail = G(g, A(0, 1.25, -.62)); SetRot(tail, .5, 0, 0); P(tail, "box", m, A(.1, .7, .12), A(0, -.35, 0));
            var u = UData.Of(g); u.head = n; u.tail = tail; u.headRest = -.55f; u.legs = legs;
            return g;
        }
        internal static Action<float, float> WanderFx(Transform a, double cx, double cz, double rx, double rz, double speed = .6, double peck = .6)
        {
            var u = UData.Of(a); u.tx = (float)cx; u.tz = (float)cz; u.wait = (float)(Rnd() * 2);
            SetPos(a, cx + (Rnd() - .5) * rx, 0, cz + (Rnd() - .5) * rz); Eng_RotY(a, Rnd() * 6.28);
            double rest = u.headRest;
            return (dt, t) =>
            {
                var p = Eng_Pos3(a); double dx = u.tx - p.x, dz = u.tz - p.z, d = Hyp(dx, dz);
                if (u.tail != null) Eng_RotZ(u.tail, Sin(t * 3 + cx) * .25);
                if (u.hop != 0) return;
                if (u.wait > 0)
                {
                    if (u.bs != null) Eng_Scale(a, u.bs.Value);
                    u.wait -= dt; if (u.head != null) Eng_RotX(u.head, rest + Math.Max(0, Sin(t * (peck > 0.5 ? 9 : 2) + cz)) * peck);
                    p.y = 0; Eng_SetPos3(a, p); return;
                }
                if (d < .05) { u.tx = (float)(cx + (Rnd() * 2 - 1) * rx); u.tz = (float)(cz + (Rnd() * 2 - 1) * rz); u.wait = (float)(.5 + Rnd() * 3); return; }
                double s = Math.Min(d, speed * dt); p.x += (float)(dx / d * s); p.z += (float)(dz / d * s);
                double ry = Eng_Rot3(a).y; double da = Math.Atan2(dx, dz) - ry; da = Math.Atan2(Sin(da), Cos(da)); Eng_RotY(a, ry + da * Math.Min(1, dt * 7));
                if (u.bs == null) u.bs = a.localScale.x;
                float bs = u.bs.Value; double hop = Math.Abs(Sin(t * 11 + cx)); p.y = (float)(hop * .06 * bs);
                a.localScale = new Vector3((float)(bs * (1 - hop * .05)), (float)(bs * (1 + hop * .08)), (float)(bs * (1 - hop * .05)));
                if (u.head != null) Eng_RotX(u.head, rest);
                Eng_SetPos3(a, p);
            };
        }
        internal static Action<float, float> IdleFx(Transform a, double ph = 0)
        {
            var u = UData.Of(a); double rest = u.headRest;
            return (dt, t) => { if (u.head != null) Eng_RotX(u.head, rest + Math.Max(0, Sin(t * .9 + ph)) * .45); if (u.tail != null) Eng_RotZ(u.tail, Sin(t * 2.6 + ph) * .3); };
        }

        // --- ekinler ---
        internal static void Plant(Transform par, string type, int stage, double x, double z, Func<double> q)
        {
            double j = (q() - .5) * .12;
            if (stage == 1) { P(par, "cone", 0x7cc04a, A(.05, .18, .05), A(x + j, .09, z)); P(par, "cone", 0x6ab03e, A(.04, .14, .04), A(x + j + .05, .07, z + .03), A(0, 0, .4)); return; }
            if (stage == 2) { double hh = type == "misir" || type == "aycicegi" ? .6 : .32; P(par, "cone", 0x5fa83a, A(.13, hh, .13), A(x + j, hh / 2, z)); return; }
            switch (type)
            {
                case "bugday": for (int k = 0; k < 3; k++) { double a = x + j + (k - 1) * .07, b = z + (q() - .5) * .08; P(par, "cyl", 0xd9b44a, A(.012, .5, .012), A(a, .25, b)); P(par, "cyl", 0xe8c25a, A(.035, .16, .035), A(a, .55, b)); } break;
                case "misir": P(par, "cyl", 0x4f9a35, A(.04, 1.05, .04), A(x + j, .52, z)); P(par, "box", 0x5fae3d, A(.5, .03, .08), A(x + j, .55, z), A(0, 0, .5)); P(par, "box", 0x5fae3d, A(.45, .03, .08), A(x + j, .75, z), A(0, 0, -.5)); P(par, "cyl", 0xf3c63c, A(.06, .24, .06), A(x + j + .08, .7, z), A(0, 0, .25)); P(par, "cone", 0xc9b24a, A(.05, .18, .05), A(x + j, 1.12, z)); break;
                case "domates": P(par, "ico1", 0x4f9636, A(.22, .24, .22), A(x + j, .24, z)); for (int k = 0; k < 3; k++) P(par, "lsph", 0xe03c2c, A(.07, .07, .07), A(x + j + Cos(k * 2.1) * .17, .2 + k * .06, z + Sin(k * 2.1) * .17)); break;
                case "aycicegi":
                    {
                        P(par, "cyl", 0x4f9a35, A(.03, 1, .03), A(x + j, .5, z)); P(par, "box", 0x5fae3d, A(.25, .02, .12), A(x + j, .5, z));
                        var h = G(par, A(x + j, 1.02, z + .03)); SetRot(h, -.35, 0, 0);
                        P(h, "cyl", 0xf5c518, A(.2, .03, .2), A(0, 0, 0), A(PI / 2, 0, 0)); P(h, "cyl", 0x5a3a1a, A(.1, .05, .1), A(0, 0, .02), A(PI / 2, 0, 0)); break;
                    }
                case "pamuk": P(par, "ico1", 0x5a8f3a, A(.18, .22, .18), A(x + j, .22, z)); for (int k = 0; k < 4; k++) P(par, "ico1", 0xffffff, A(.07, .07, .07), A(x + j + Cos(k * 1.6) * .14, .36 + ((k % 2) * .06), z + Sin(k * 1.6) * .14)); break;
                case "kabak": P(par, "ico1", 0x4f9636, A(.25, .12, .25), A(x + j, .1, z)); P(par, "sph", 0xf08a24, A(.2, .16, .2), A(x + j + .1, .16, z)); break;
                case "altinbugday": for (int k = 0; k < 3; k++) { double a = x + j + (k - 1) * .07; P(par, "cyl", 0xe8c25a, A(.012, .5, .012), A(a, .25, z)); P(par, "cyl", 0xffd23f, A(.04, .17, .04), A(a, .56, z), null, new MatOpts { emissive = 0xf2b330, emissiveIntensity = .5f }); } break;
                case "morhavuc": for (int k = 0; k < 3; k++) P(par, "cone", 0x5fae3d, A(.04, .25, .04), A(x + j + (k - 1) * .05, .18, z), A(0, 0, (k - 1) * .3)); P(par, "cone", 0x7a2f8a, A(.07, .14, .07), A(x + j, .05, z), A(PI, 0, 0)); break;
                case "lale": for (int k = 0; k < 2; k++) { double a = x + j + (k - .5) * .12; P(par, "cyl", 0x4f9a35, A(.015, .42, .015), A(a, .21, z)); P(par, "cone", 0x5fae3d, A(.05, .2, .05), A(a + .04, .1, z), A(0, 0, .4)); P(par, "cyl", C(0xe03c2c, 0xf2b330)[k], A(.07, .12, .07), A(a, .45, z)); } break;
                case "karpuz": P(par, "ico1", 0x4f9636, A(.22, .06, .22), A(x + j, .05, z)); P(par, "sph", 0x2f7a35, A(.2, .17, .24), A(x + j + .06, .15, z)); break;
                case "patates": P(par, "ico1", 0x5a8f3a, A(.2, .16, .2), A(x + j, .16, z)); for (int k = 0; k < 3; k++) P(par, "ico1", 0xf0f0e0, A(.03, .03, .03), A(x + j + Cos(k * 2.1) * .12, .3, z + Sin(k * 2.1) * .12)); break;
                case "lahana": P(par, "ico1", 0x7cb85a, A(.2, .14, .2), A(x + j, .12, z)); P(par, "ico1", 0xa8d98a, A(.13, .12, .13), A(x + j, .2, z)); break;
                case "cilek": P(par, "ico1", 0x4f9636, A(.18, .1, .18), A(x + j, .1, z)); for (int k = 0; k < 3; k++) P(par, "cone", 0xe0303c, A(.05, .09, .05), A(x + j + Cos(k * 2) * .14, .07, z + Sin(k * 2) * .14), A(PI, 0, 0)); break;
            }
        }
        internal static void FieldBed(Transform par, Job job, double w = 3.7, double d = 3.7, int rows = 5, int cols = 6)
        {
            P(par, "box", 0x6b4423, A(w, .18, d), A(0, .09, 0));
            string crop = job != null ? job.r : null; int stage = crop != null ? StageOf(job) : 0; var q = Rng(7);
            var pg = G(par, A(0, .28, 0)); var piv = new List<Transform>(); var pxz = new List<double[]>();
            for (int r = 0; r < rows; r++)
            {
                double z = -d / 2 + d / rows * (r + .5);
                P(par, "box", 0x7d5230, A(w - .2, .12, d / rows * .55), A(0, .22, z));
                if (crop != null) for (int c = 0; c < cols; c++) { double x = -w / 2 + w / cols * (c + .5); var pv = G(pg, A(x, 0, z)); Plant(pv, crop, stage, 0, 0, q); piv.Add(pv); pxz.Add(new[] { x, z }); }
            }
            if (stage >= 2) AddFx(par, (dt, tm) =>
            {
                for (int i = 0; i < piv.Count; i++) { double px = pxz[i][0], pz = pxz[i][1]; SetRot(piv[i], Cos(tm * 1.7 + pz) * .04, 0, Sin(tm * 2.1 + px * .9 + pz * .4) * .09); }
            });
        }

        // --- binalar ---
        internal static Transform House(int l)
        {
            var g = G(null);
            if (l == 1)
            {
                P(g, "box", 0x8b5a32, A(2.2, 1.3, 1.8), A(0, .65, 0));
                for (int k = 0; k < 5; k++) P(g, "cyl", 0x74482a, A(.09, 2.24, .09), A(0, .13 + k * .26, .92), A(0, 0, PI / 2));
                for (int k = 0; k < 5; k++) P(g, "cyl", 0x74482a, A(.09, 1.84, .09), A(1.12, .13 + k * .26, 0), A(PI / 2, 0, 0));
                GableRoof(g, 2.2, 1.8, 1.05, 1.3, 0xc9a050, 0x8b5a32, "x", .32, .18); Door(g, .45, 0, .95); WindowM(g, -.5, .75, .96, .42, .42);
                P(g, "box", 0x8a8478, A(.32, 1.1, .32), A(-.7, 1.95, -.35)); P(g, "box", 0x6f6a60, A(.38, .1, .38), A(-.7, 2.5, -.35));
                for (int k = 0; k < 3; k++) for (int m = 0; m < 3 - k; m++) P(g, "cyl", 0xa6773f, A(.1, .6, .1), A(1.45 + m * .2 + k * .1, .1 + k * .17, .3), A(PI / 2, 0, 0));
                P(g, "cyl", 0x8a6038, A(.18, .4, .18), A(-1.4, .2, .9)); UData.Of(g).chimney = new[] { -.7f, 2.6f, -.35f }; return g;
            }
            if (l == 2)
            {
                P(g, "box", StdTex(TEX.stone(new double[] { 2, 1.2 })), A(2.7, 1.6, 2.1), A(0, .8, 0));
                GableRoof(g, 2.7, 2.1, 1.1, 1.6, 0xb4432c, 0xd8cdb8, "x", .3); Door(g, 0, 0, 1.06, .55, .95, 0x5a3418); WindowM(g, -.85, .95, 1.07); WindowM(g, .85, .95, 1.07);
                foreach (var x in new[] { -.85, .85 }) { P(g, "box", 0x6b4423, A(.55, .12, .16), A(x, .6, 1.14)); var fc = C(0xe8657a, 0xf4d03f, 0xffffff, 0xe8657a); for (int k = 0; k < 4; k++) P(g, "lsph", fc[k], A(.06, .06, .06), A(x - .2 + k * .13, .7, 1.15)); }
                P(g, "box", 0xa34a32, A(.34, 1.1, .34), A(.8, 2.3, -.4)); P(g, "box", 0x7a3524, A(.42, .1, .42), A(.8, 2.86, -.4)); Bush(g, -1.6, .6, .9); Bush(g, 1.6, .7, .8);
                P(g, "box", 0x9b9282, A(.9, .1, .5), A(0, .05, 1.32)); UData.Of(g).chimney = new[] { .8f, 2.95f, -.4f }; return g;
            }
            if (l == 3)
            {
                P(g, "box", 0xf2ead8, A(2.8, 2.2, 2.1), A(-.3, 1.1, -.2)); GableRoof(G(g, A(-.3, 0, -.2)), 2.8, 2.1, 1.2, 2.2, 0x9c3a2a, 0xf2ead8, "x", .3);
                for (int k = 0; k < 6; k++) P(g, "box", 0xe2d8c2, A(2.82, .03, 2.12), A(-.3, .2 + k * .36, -.2));
                P(g, "box", 0xe9dfc8, A(1.2, 1.5, 1.8), A(1.75, .75, -.35)); GableRoof(G(g, A(1.75, 0, -.35)), 1.2, 1.8, .8, 1.5, 0x9c3a2a, 0xe9dfc8, "z", .2);
                P(g, "box", 0x9a7247, A(3, .14, .9), A(-.3, .12, 1.3)); foreach (var x in new[] { -1.65, -.3, 1.05 }) P(g, "box", 0xf8f2e4, A(.12, 1.1, .12), A(x, .7, 1.68));
                { var pr = P(g, "box", 0x7a2f22, A(3.2, .1, 1.1), A(-.3, 1.3, 1.35)); SetRot(pr, .18, 0, 0); }
                Door(g, -.3, .18, .86, .6, 1, 0x2f5a3a); WindowM(g, -1.25, .9, .87); WindowM(g, .65, .9, .87); WindowM(g, -1.25, 1.75, .87, .4, .42); WindowM(g, .65, 1.75, .87, .4, .42); WindowM(g, 1.75, .85, .56, .38, .42);
                P(g, "box", 0x8b8478, A(.36, 1.2, .36), A(-1.3, 3.2, -.6)); UData.Of(g).chimney = new[] { -1.3f, 3.85f, -.6f };
                P(g, "cyl", 0xbb5d3a, A(.16, .2, .16), A(-1.75, .3, 1.55)); P(g, "ico1", 0x4f8f37, A(.22, .22, .22), A(-1.75, .52, 1.55)); Eng_Scale(g, .92); return g;
            }
            P(g, "box", 0xf3e6c8, A(2.4, 2.6, 2), A(0, 1.3, -.2)); GableRoof(G(g, A(0, 0, -.2)), 2.4, 2, 1, 2.6, 0x44536f, 0xf3e6c8, "z", .25);
            foreach (var s in new[] { -1, 1 }) { P(g, "box", 0xeadbbb, A(1.1, 1.9, 1.7), A(s * 1.75, .95, -.3)); GableRoof(G(g, A(s * 1.75, 0, -.3)), 1.1, 1.7, .7, 1.9, 0x44536f, 0xeadbbb, "x", .2); WindowM(g, s * 1.75, 1.05, .56, .42, .6); }
            foreach (var x in new[] { -.85, -.3, .3, .85 }) P(g, "cyl", 0xfffaf0, A(.1, 2.2, .1), A(x, 1.25, 1.05)); P(g, "box", 0xfffaf0, A(2.2, .2, .5), A(0, 2.4, 1));
            { var pd = G(g, A(0, 2.5, 1.02)); P(pd, "tri", 0xfffaf0, A(2.2, .55, .4), A(0, 0, 0)); }
            for (int k = 0; k < 3; k++) P(g, "box", 0xd8ccb2, A(1.8 - k * .2, .1, .35), A(0, .05 + k * .1, 1.45 - k * .15));
            Door(g, 0, .3, .81, .6, 1.1, 0x3a2f5a); WindowM(g, -.65, 1.9, .81, .36, .45); WindowM(g, .65, 1.9, .81, .36, .45); WindowM(g, -.65, .95, .81, .3, .5); WindowM(g, .65, .95, .81, .3, .5);
            P(g, "box", 0xf3e6c8, A(.7, .8, .7), A(0, 3.9, -.2)); P(g, "pyr", 0x44536f, A(.6, .8, .6), A(0, 4.7, -.2), A(0, PI / 4, 0)); P(g, "sph", 0xe0b84a, A(.07, .07, .07), A(0, 5.15, -.2));
            foreach (var x in new[] { -2.1, 2.1 }) P(g, "box", 0x3f7f34, A(.5, .45, 1.6), A(x, .22, 1.25)); P(g, "cyl", 0xd8ccb2, A(.35, .2, .35), A(0, .1, 1.85)); Eng_Scale(g, .82); return g;
        }
        internal static void Workshop(Transform g, int wall, int roof, double w = 2.5, double h = 1.5, double d = 1.9, double z = -.4)
        {
            P(g, "box", wall, A(w, h, d), A(0, h / 2, z)); GableRoof(G(g, A(0, 0, z)), w, d, .85, h, roof, wall, "x", .25);
            Door(g, 0, 0, z + d / 2 + .01, .6, .95, 0x5c3a1f); WindowM(g, -w / 2 + .45, h * .6, z + d / 2 + .02, .36, .4); WindowM(g, w / 2 - .45, h * .6, z + d / 2 + .02, .36, .4);
        }
        internal static void Crate(Transform g, double x, double z, double s = .38, int c = 0x9a6b3c) { P(g, "box", c, A(s, s, s), A(x, s / 2, z)); P(g, "box", 0x6e4a28, A(s + .02, .05, s + .02), A(x, s * .8, z)); }
        internal static void Sack(Transform g, double x, double z, int c = 0xe2d3ae) { P(g, "sph", c, A(.22, .26, .2), A(x, .24, z)); P(g, "cyl", 0xc9b48a, A(.07, .1, .07), A(x, .5, z)); }

        internal static Dictionary<string, Action<Transform, Unit>> BuildModels()
        {
            var MD = new Dictionary<string, Action<Transform, Unit>>();
            MD["kumes"] = (g, t) =>
            {
                var c = G(g, A(-.6, 0, -.9));
                foreach (var xz in new[] { A(-.7, -.5, 0), A(.7, -.5, 0), A(-.7, .5, 0), A(.7, .5, 0) }) P(c, "box", 0x7a5233, A(.1, .45, .1), A(xz[0], .22, xz[1]));
                P(c, "box", 0xe7c48c, A(1.7, 1, 1.3), A(0, .95, 0)); GableRoof(c, 1.7, 1.3, .7, 1.45, 0xb5402c, 0xe7c48c, "x", .2); P(c, "box", 0x3a2a1a, A(.36, .4, .05), A(.3, .75, .66));
                { var ramp = P(c, "box", 0x9a7247, A(.3, .05, 1), A(.3, .25, 1.05)); SetRot(ramp, .5, 0, 0); }
                WindowM(c, -.4, 1.05, .67, .3, .3, 0, .2);
                FenceRun(g, -1.8, .2, 1.8, .2, 0xd9c8a5, .45); FenceRun(g, -1.8, 1.85, 1.8, 1.85, 0xd9c8a5, .45); FenceRun(g, -1.8, .2, -1.8, 1.85, 0xd9c8a5, .45); FenceRun(g, 1.8, .2, 1.8, 1.85, 0xd9c8a5, .45);
                P(g, "box", 0xc9a05a, A(3.4, .03, 1.5), A(0, .02, 1.02));
                int n = 2 + (t != null ? t.l : 1); var cc = C(0xfaf6ee, 0xb8693a, 0xfaf6ee, 0x3a2a22);
                for (int k = 0; k < n; k++)
                {
                    var ch = Chicken(cc[k % 4]); Eng_Scale(ch, .85); ch.SetParent(g, false);
                    AddFx(g, WanderFx(ch, 0, 1.02, 1.4, .55, .5));
                    AddFx(g, (dt, tm) => Eng_Visible(ch, S.weather.k != "storm"));
                }
                P(g, "box", 0x9a7247, A(.6, .15, .25), A(1.1, .08, -.2)); P(g, "box", 0xe8c25a, A(.5, .04, .18), A(1.1, .16, -.2));
            };
            MD["silo"] = (g, t) =>
            {
                int l = t != null ? t.l : 1;
                P(g, "cyl", 0xc7ccd2, A(.9, 3.2, .9), A(-.4, 1.6, -.3)); for (int k = 0; k < 6; k++) P(g, "cyl", 0xa9afb8, A(.92, .05, .92), A(-.4, .3 + k * .5, -.3));
                P(g, "hsph", 0xb5402c, A(.92, .6, .92), A(-.4, 3.2, -.3));
                if (l >= 2) { P(g, "cyl", 0xc7ccd2, A(.6, 2.4, .6), A(1.1, 1.2, .4)); P(g, "hsph", 0xb5402c, A(.62, .4, .62), A(1.1, 2.4, .4)); }
                if (l >= 3) { P(g, "cyl", 0xc7ccd2, A(.55, 2, .55), A(1.2, 1, -1.1)); P(g, "hsph", 0xb5402c, A(.57, .35, .57), A(1.2, 2, -1.1)); }
                P(g, "box", 0x6e6e6e, A(.08, 3, .08), A(-1.32, 1.5, .2)); for (int k = 0; k < 8; k++) P(g, "box", 0x6e6e6e, A(.3, .04, .04), A(-1.32, .3 + k * .36, .2));
                Sack(g, .4, 1.2); Sack(g, .75, 1.3, 0xd8c79e);
            };
            MD["yemhane"] = (g, t) =>
            {
                Workshop(g, 0xa47148, 0x5d6b4a, 2.2, 1.4, 1.6, -.6); P(g, "cone", 0xa9afb8, A(.55, .8, .55), A(1.3, 1.8, .5), A(PI, 0, 0)); P(g, "cyl", 0xa9afb8, A(.55, .5, .55), A(1.3, 2.45, .5));
                foreach (var xz in new[] { A(1, .25, 0), A(1.6, .25, 0), A(1, .75, 0), A(1.6, .75, 0) }) P(g, "box", 0x6e6e6e, A(.06, 1.4, .06), A(xz[0], .7, xz[1]));
                for (int k = 0; k < 4; k++) Sack(g, -1.3 + k * .32, 1.1 + (k % 2) * .15, 0xe2d3ae);
                var gear = P(g, "cyl", 0x707680, A(.35, .08, .35), A(-.9, 1.15, .24), A(PI / 2, 0, 0));
                AddFx(g, (dt, tm) => Eng_RotY(gear, Eng_Rot3(gear).y + dt * (Job("yemhane") ? 3 : .3)));
            };
            MD["ahir"] = (g, t) =>
            {
                var b = G(g, A(0, 0, -.5)); P(b, "box", 0xb23b2b, A(2.8, 1.7, 2.3), A(0, .85, 0)); P(b, "gam", 0xb23b2b, A(2.8, 1.35, 2.3), A(0, 1.7, 0));
                const int rc = 0x5e2a22;
                foreach (var s in new[] { -1, 1 })
                {
                    var pts = new[] { new[] { s * 1.5, 1.62 }, new[] { s * 1.16, 2.48 }, new[] { 0, 3.1 } };
                    for (int k = 0; k < 2; k++)
                    {
                        double a1 = pts[k][0], y1 = pts[k][1], a2 = pts[k + 1][0], y2 = pts[k + 1][1];
                        P(b, "box", rc, A(Hyp(a2 - a1, y2 - y1) + .04, .1, 2.55), A((a1 + a2) / 2, (y1 + y2) / 2 + .05, 0), A(0, 0, Math.Atan2(y2 - y1, a2 - a1)));
                    }
                }
                foreach (var x in new[] { -1.42, 1.42 }) P(b, "box", 0xf6f0e0, A(.1, 1.7, .1), A(x, .85, 1.16)); P(b, "box", 0xf6f0e0, A(2.9, .1, .1), A(0, 1.7, 1.16));
                P(b, "box", 0x8f2a1e, A(1.1, 1.15, .06), A(0, .58, 1.16)); P(b, "box", 0xf6f0e0, A(1.2, .08, .08), A(0, 1.17, 1.19));
                foreach (var s in new[] { -1, 1 }) { P(b, "box", 0xf6f0e0, A(.08, 1.2, .08), A(s * .56, .6, 1.19)); P(b, "box", 0xf6f0e0, A(1.5, .07, .07), A(0, .58, 1.2), A(0, 0, s * .83)); }
                P(b, "box", 0x8f2a1e, A(.6, .55, .06), A(0, 2.2, 1.17)); P(b, "box", 0xf6f0e0, A(.68, .62, .04), A(0, 2.2, 1.155));
                for (int k = 0; k < 2; k++) P(g, "cyl", 0xd9b45e, A(.35, .6, .35), A(1.6, .35, .8 + k * .7), A(PI / 2, 0, 0));
                int n = 1 + (t != null ? t.l : 1);
                for (int k = 0; k < Math.Min(n, 3); k++) { var c = Cow(); Eng_Scale(c, .62); c.SetParent(g, false); AddFx(g, WanderFx(c, -.6 + k * .4, 1.35, .9, .35, .25, .5)); }
            };
            MD["degirmen"] = (g, t) =>
            {
                var tw = Eng_Mesh(g, Geo.Cylinder(.62f, 1.05f, 3, 8), M(0xeee4cc), true, true, "tower"); SetPos(tw, 0, 1.5, 0); Outline(tw);
                P(g, "cone", 0x7a3a28, A(.85, 1, .85), A(0, 3.45, 0)); Door(g, 0, 0, 1.0, .45, .8); WindowM(g, 0, 1.9, .82, .28, .32);
                var hub = G(g, A(0, 3.05, .86)); P(hub, "cyl", 0x5a3a22, A(.15, .25, .15), A(0, 0, 0), A(PI / 2, 0, 0));
                for (int k = 0; k < 4; k++) { var bl = G(hub); SetRot(bl, 0, 0, k * PI / 2); P(bl, "box", 0x6e4a2a, A(.08, 1.9, .05), A(0, 1, 0)); P(bl, "box", 0xf8f1de, A(.42, 1.4, .03), A(.25, 1.15, .02)); }
                AddFx(g, (dt, tm) => Eng_RotZ(hub, Eng_Rot3(hub).z + dt * (Job("degirmen") ? 2.4 : .35)));
                Sack(g, 1.2, .9); Sack(g, 1.45, 1.15, 0xd8c79e);
            };
            MD["firin"] = (g, t) =>
            {
                Workshop(g, 0xc9774a, 0x6b3a28, 2.4, 1.4, 1.7, -.6); P(g, "hsph", 0xb25a36, A(.85, .9, .85), A(1.05, 0, .75)); P(g, "arch", 0x2a1a12, A(.5, .5, .2), A(1.05, 0, 1.55));
                P(g, "box", 0x9a4a30, A(.35, 1.2, .35), A(-.8, 2.2, -.9));
                for (int k = 0; k < 3; k++) P(g, "cyl", 0xa6773f, A(.08, .6, .08), A(-1.1 + k * .18, .08, .95), A(PI / 2, 0, 0));
                AddSmoke(g, A(-.8, 2.85, -.9), () => Job("firin"));
            };
            MD["agil"] = (g, t) =>
            {
                var sh = G(g, A(0, 0, -1)); P(sh, "box", 0xa77b4f, A(3.2, 1.1, 1.3), A(0, .55, 0)); { var r = P(sh, "box", 0x5e4b38, A(3.5, .12, 1.8), A(0, 1.25, .15)); SetRot(r, -.18, 0, 0); }
                FenceRun(g, -1.8, -.3, -1.8, 1.85); FenceRun(g, 1.8, -.3, 1.8, 1.85); FenceRun(g, -1.8, 1.85, 1.8, 1.85); P(g, "box", 0xa58a4a, A(3.3, .03, 2), A(0, .02, .8));
                int n = 2 + (t != null ? t.l : 1);
                for (int k = 0; k < n; k++) { var s = Sheep(); Eng_Scale(s, .62); s.SetParent(g, false); AddFx(g, WanderFx(s, 0, .85, 1.3, .7, .3, .4)); }
                P(g, "box", 0x9a7247, A(.8, .4, .35), A(-1.2, .2, -.2)); P(g, "box", 0xd9b45e, A(.7, .12, .3), A(-1.2, .42, -.2));
            };
            MD["mandira"] = (g, t) =>
            {
                Workshop(g, 0xf4f1ea, 0x3f6fa0, 2.6, 1.6, 1.9, -.5); P(g, "box", 0x3f6fa0, A(2.62, .25, 1.92), A(0, .12, -.5));
                for (int k = 0; k < 3; k++) { double x = -1.4 + k * .35; P(g, "cyl", 0xc7ccd2, A(.13, .38, .13), A(x, .19, .85)); P(g, "cyl", 0xc7ccd2, A(.08, .08, .08), A(x, .42, .85)); }
                P(g, "cyl", 0xf2c94c, A(.32, .18, .32), A(1.3, .09, .9)); P(g, "cyl", 0xf2c94c, A(.28, .16, .28), A(1.3, .26, .9));
            };
            MD["kovan"] = (g, t) =>
            {
                for (int k = 0; k < 4; k++)
                {
                    double x = -1.2 + (k % 2) * 1.9, z = -.9 + Math.Floor(k / 2.0) * 1.5; P(g, "box", 0x8a6a44, A(.7, .15, .6), A(x, .08, z));
                    var hc = C(0xf4e3b0, 0xf0c75e, 0xfff3d0);
                    for (int m = 0; m < 3; m++) P(g, "box", hc[m], A(.6, .3, .5), A(x, .3 + m * .31, z));
                    P(g, "box", 0xb5402c, A(.72, .08, .62), A(x, 1.2, z)); P(g, "box", 0x2a1a12, A(.2, .05, .02), A(x, .22, z + .26));
                }
                Flowers(g, .3, .2, 3.2, 4); Flowers(g, 0, 1.3, 3, 8);
                var bees = new List<Transform>();
                for (int k = 0; k < 6; k++) bees.Add(P(g, "lsph", 0x2a1a12, A(.05, .04, .06), A(0, 1, 0), null, new MatOpts { emissive = 0xf2b330, emissiveIntensity = .6f }));
                AddFx(g, (dt, tm) => { for (int k = 0; k < bees.Count; k++) SetPos(bees[k], Sin(tm * (1 + k * .2) + k) * 1.4, 1 + Sin(tm * 3 + k) * .3, Cos(tm * (.8 + k * .15) + k * 2) * 1.2); });
            };
            MD["bahce"] = (g, t) =>
            {
                bool ready = t != null && t.job != null && Prog(t.job) > .6;
                P(g, "box", 0x6b8f3a, A(3.8, .02, 3.8), A(0, .01, 0));
                foreach (var xz in new[] { A(-1, -1, 0), A(1, -1, 0), A(-1, 1, 0), A(1, 1, 0) }) Oak(g, xz[0], xz[1], .75, ready);
                Crate(g, 0, .2, .35);
            };
            MD["konserve"] = (g, t) =>
            {
                Workshop(g, 0xd56b3f, 0x5e3a2a, 2.4, 1.5, 1.7, -.6);
                for (int k = 0; k < 3; k++) Crate(g, -1.2 + k * .42, 1.05, .36, 0xa7743f);
                for (int k = 0; k < 5; k++) P(g, "cyl", 0xb5262a, A(.07, .14, .07), A(.6 + k * .17, .07, 1.1));
                P(g, "cyl", 0xc7ccd2, A(.4, .9, .4), A(1.2, .45, .6));
            };
            MD["besi"] = (g, t) =>
            {
                var sh = G(g, A(0, 0, -.7));
                foreach (var x in new[] { -1.6, 0, 1.6 }) foreach (var z in new[] { -.8, .8 }) P(sh, "box", 0x6e4a2a, A(.14, 1.5, .14), A(x, .75, z));
                P(sh, "box", 0x6e4a2a, A(3.4, 1.5, .1), A(0, .75, -.85));
                { var r = P(sh, "box", 0x4d5a6b, A(3.7, .1, 2.1), A(0, 1.62, 0)); SetRot(r, .12, 0, 0); }
                P(sh, "box", 0x9a7247, A(3.2, .35, .3), A(0, .18, .75)); P(sh, "box", 0xd9b45e, A(3, .1, .22), A(0, .37, .75));
                FenceRun(g, -1.8, .3, -1.8, 1.85); FenceRun(g, 1.8, .3, 1.8, 1.85); FenceRun(g, -1.8, 1.85, 1.8, 1.85);
                int n = 1 + (t != null ? t.l : 1);
                for (int k = 0; k < n; k++) { var c = Cow(0x7a4a2a, 0x5a3420, true); Eng_Scale(c, .6); c.SetParent(g, false); AddFx(g, WanderFx(c, -.8 + k * .6, 1.1, .6, .4, .2, .5)); }
            };
            MD["bag"] = (g, t) =>
            {
                bool ready = t != null && t.job != null && Prog(t.job) > .6;
                for (int r = 0; r < 3; r++)
                {
                    double z = -1.2 + r * 1.2;
                    foreach (var x in new[] { -1.6, 1.6 }) P(g, "cyl", 0x6e4a2a, A(.05, 1, .05), A(x, .5, z));
                    P(g, "box", 0x5a4a3a, A(3.2, .02, .02), A(0, .85, z));
                    for (int k = 0; k < 5; k++) { double x = -1.3 + k * .65; P(g, "ico1", C(0x5c9a3e, 0x4f8a36)[k % 2], A(.32, .3, .22), A(x, .72, z)); if (ready) P(g, "ico1", 0x6b3a8a, A(.1, .15, .1), A(x + .08, .55, z + .12)); }
                }
                Crate(g, 1.4, 1.4, .32, 0x7a4a8a);
            };
            MD["pekmezhane"] = (g, t) =>
            {
                Workshop(g, 0xe2d0a8, 0x7a3b2a, 2.4, 1.5, 1.7, -.6); P(g, "cyl", 0x3a2a20, A(.55, .45, .55), A(.95, .23, .9));
                P(g, "cyl", 0x5a1a2a, A(.46, .05, .46), A(.95, .47, .9), null, new MatOpts { emissive = 0x3a0a12, emissiveIntensity = .3f });
                foreach (var x in new[] { -1.1, -.6 }) P(g, "cyl", 0x8a5a32, A(.24, .42, .24), A(x, .21, 1));
            };
            MD["yaghane"] = (g, t) =>
            {
                Workshop(g, 0xe9c45a, 0x5d4b3a, 2.4, 1.5, 1.7, -.6); foreach (var x in new[] { .9, 1.5 }) P(g, "cyl", 0xd7a82b, A(.32, 1.3, .32), A(x, .65, .85));
                P(g, "box", 0x6e6e6e, A(.9, .06, .06), A(1.2, 1.25, .85)); for (int k = 0; k < 4; k++) P(g, "cyl", 0xf2c94c, A(.06, .2, .06), A(-1.2 + k * .16, .1, 1.1));
            };
            MD["dokuma"] = (g, t) =>
            {
                Workshop(g, 0x8e6aa8, 0x4a3a5a, 2.5, 1.5, 1.8, -.6); var yc = C(0xd8435a, 0x3f6fa0, 0xf2c94c);
                for (int k = 0; k < 3; k++) { P(g, "cyl", yc[k], A(.17, .4, .17), A(-1.2 + k * .42, .2, 1.05)); P(g, "cyl", 0x8a6038, A(.06, .48, .06), A(-1.2 + k * .42, .24, 1.05)); }
                P(g, "box", 0x9a7247, A(.9, .7, .3), A(1.2, .35, 1)); P(g, "box", 0xd8435a, A(.8, .5, .04), A(1.2, .45, 1.16));
            };
            MD["sera"] = (g, t) =>
            {
                var glass = ToonMat(new MatOpts { color = 0xcfeefa, transparent = true, opacity = .28f, depthWrite = false });
                FieldBed(G(g), t != null ? t.job : null, 3.1, 3.1, 4, 5);
                const int frame = 0xf4f1ea;
                foreach (var x in new[] { -1.65, -.55, .55, 1.65 }) foreach (var z in new[] { -1.65, 1.65 }) P(g, "box", frame, A(.07, 1.6, .07), A(x, .8, z));
                foreach (var z in new[] { -1.65, 1.65 }) P(g, "box", frame, A(3.3, .07, .07), A(0, 1.6, z));
                foreach (var x in new[] { -1.65, 1.65 }) P(g, "box", frame, A(.07, .07, 3.3), A(x, 1.6, 0));
                var box = Eng_Mesh(g, GE["box"], glass, false, false, "glass"); box.localScale = new Vector3(3.3f, 1.6f, 3.3f); SetPos(box, 0, .8, 0);
                var roof = Eng_Mesh(g, GE["tri"], glass, false, false, "glassRoof"); roof.localScale = new Vector3(3.3f, .9f, 3.3f); SetPos(roof, 0, 1.6, 0);
                P(g, "box", frame, A(.06, .06, 3.3), A(0, 2.5, 0));
                foreach (var z in new[] { -1.65, 1.65 }) { P(g, "box", frame, A(1.75, .06, .06), A(-.82, 2.05, z), A(0, 0, .5)); P(g, "box", frame, A(1.75, .06, .06), A(.82, 2.05, z), A(0, 0, -.5)); }
            };
            MD["hara"] = (g, t) =>
            {
                var st = G(g, A(0, 0, -.4)); P(st, "box", 0x9a6a3e, A(3.3, 1.3, 1.2), A(0, .65, 0)); GableRoof(st, 3.3, 1.2, .6, 1.3, 0x4f3b2a, 0x9a6a3e, "x", .2);
                for (int k = 0; k < 3; k++) { P(st, "box", 0x5e3d22, A(.75, .6, .05), A(-1.05 + k * 1.05, .35, .61)); P(st, "box", 0x2a1a12, A(.75, .5, .04), A(-1.05 + k * 1.05, .95, .6)); }
                int n = Math.Min(2, t != null ? t.l : 1); var hc = C(0x8a5530, 0x3a2a22);
                for (int k = 0; k < n; k++) { var h = Horse(hc[k]); Eng_Scale(h, .5); h.SetParent(g, false); AddFx(g, WanderFx(h, 0, 1.1, 1.3, .3, .5, .4)); }
            };
            return MD;
        }

        // --- insanlar ---
        internal static Transform Person(PersonOpt o = null)
        {
            o = o ?? new PersonOpt();
            var g = G(null); int skin = o.skin ?? 0xe8b48a, shirt = o.shirt ?? 0xe9e2cf, pants = o.pants ?? 0x3f5f8a; var legs = new List<Transform>(); var arms = new List<Transform>();
            foreach (var s in new[] { -1, 1 }) { var l = G(g, A(s * .11, .52, 0)); P(l, "box", pants, A(.15, .5, .17), A(0, -.25, 0)); P(l, "box", 0x3a2416, A(.17, .09, .24), A(0, -.5, .03)); legs.Add(l); }
            bool dr = o.dress != null;
            P(g, "box", o.dress ?? shirt, A(dr ? .5 : .4, dr ? .72 : .46, .28), A(0, dr ? .66 : .76, 0)); if (o.apron != null) P(g, "box", o.apron.Value, A(.3, .42, .03), A(0, .7, .15));
            foreach (var s in new[] { -1, 1 }) { var a = G(g, A(s * .27, 1.06, 0)); P(a, "box", shirt, A(.13, .44, .14), A(0, -.21, 0)); P(a, "sph", skin, A(.075, .075, .075), A(0, -.46, 0)); arms.Add(a); }
            var hd = G(g, A(0, 1.33, 0));
            P(hd, "sph", skin, A(.21, .22, .21)); P(hd, "sph", 0xd99a6a, A(.055, .055, .055), A(0, -.03, .21)); P(hd, "lsph", 0x1a1a1a, A(.03, .035, .03), A(.08, .05, .18)); P(hd, "lsph", 0x1a1a1a, A(.03, .035, .03), A(-.08, .05, .18));
            if (o.mus) P(hd, "box", 0x5a3a22, A(.22, .06, .05), A(0, -.1, .19));
            if (o.hat == "fez") { P(hd, "cyl", 0xb5262a, A(.15, .22, .13), A(0, .25, 0)); }
            else if (o.hat == "cap") { P(hd, "hsph", o.hatc ?? 0x3f6fa0, A(.22, .14, .22), A(0, .08, 0)); P(hd, "box", o.hatc ?? 0x3f6fa0, A(.2, .03, .16), A(0, .09, .18)); }
            else if (o.hat == "scarf") { P(hd, "hsph", o.hatc ?? 0xc4492e, A(.24, .2, .24), A(0, 0, -.02)); }
            else if (o.hat == "straw") { P(hd, "cyl", 0xe2c46a, A(.38, .03, .38), A(0, .15, 0)); P(hd, "cyl", 0xe2c46a, A(.18, .2, .18), A(0, .26, 0)); P(hd, "cyl", 0xb5402c, A(.185, .045, .185), A(0, .19, 0)); }
            var u = UData.Of(g); u.legs = legs; u.arms = arms; u.head = hd;
            return g;
        }
        internal static Action<float, float> IdlePerson(Transform p, double ph)
        {
            var u = UData.Of(p);
            return (dt, t) =>
            {
                Eng_RotY(u.head, Sin(t * .7 + ph) * .5);
                Eng_RotX(u.arms[1], Sin(t * .9 + ph) > .75 ? -1.3 + Sin(t * 9) * .3 : Sin(t * 1.3 + ph) * .1);
                Eng_RotX(u.arms[0], Sin(t * 1.1 + ph) * .08);
                var pp = Eng_Pos3(p); pp.y = (float)(Math.Abs(Sin(t * 1.6 + ph)) * .02); Eng_SetPos3(p, pp);
            };
        }
        internal static Action<float, float> WalkPerson(Transform p, double[][] pts, double speed = 1.2, double pause = 1.5)
        {
            int i = 0; double w = Rnd() * 2; SetPos(p, pts[0][0], 0, pts[0][1]);
            var u = UData.Of(p);
            return (dt, t) =>
            {
                double tx = pts[i][0], tz = pts[i][1]; var pos = Eng_Pos3(p); double dx = tx - pos.x, dz = tz - pos.z, d = Hyp(dx, dz); bool walking = false;
                if (w > 0) w -= dt;
                else if (d < .06) { i = (i + 1) % pts.Length; w = pause * (.5 + Rnd()); }
                else
                {
                    walking = true; double s = Math.Min(d, speed * dt); pos.x += (float)(dx / d * s); pos.z += (float)(dz / d * s);
                    Eng_RotY(p, LerpA(Eng_Rot3(p).y, Math.Atan2(dx, dz), Math.Min(1, dt * 8)));
                }
                double sw = walking ? Sin(t * 9) : 0;
                Eng_RotX(u.legs[0], sw * .6); Eng_RotX(u.legs[1], -sw * .6); Eng_RotX(u.arms[0], -sw * .5); Eng_RotX(u.arms[1], sw * .5);
                pos.y = (float)(walking ? Math.Abs(Sin(t * 9)) * .06 : 0); Eng_SetPos3(p, pos);
            };
        }

        // --- dekorasyon ---
        internal static void HouseDecor(Transform sc)
        {
            var D = S.decor.house; string duvar = GetOr(D, "duvar"), kose = GetOr(D, "kose"), masa = GetOr(D, "masa"), tavan = GetOr(D, "tavan");
            if (!string.IsNullOrEmpty(duvar))
            {
                var g = G(sc, A(-.8, 3.1, -4.92)); P(g, "box", 0x8a5a32, A(1.7, 1.25, .08));
                if (duvar == "nal") { P(g, "box", 0xf3e6c4, A(1.5, 1.05, .1)); P(g, "tor", 0x9aa0a8, A(.32, .36, .32), A(0, 0, .08)); }
                else
                {
                    P(g, "box", duvar == "tablo_at" ? 0xc9a06a : 0x9fc7de, A(1.5, 1.05, .1));
                    if (duvar == "tablo_koy") { P(g, "box", 0x7fae47, A(1.5, .45, .11), A(0, -.3, 0)); P(g, "cyl", 0xf2c94c, A(.13, .02, .13), A(.45, .25, .06), A(PI / 2, 0, 0)); P(g, "box", 0xc4492e, A(.25, .2, .12), A(-.3, -.05, 0)); }
                    else { var h = Horse(0x6a4a30, 0x2e1d12); Eng_Scale(h, .3); SetRotY(h, PI / 2); SetPos(h, -.05, -.45, .08); h.SetParent(g, false); }
                }
            }
            if (!string.IsNullOrEmpty(kose))
            {
                var g = G(sc, A(5.6, 0, 3));
                if (kose == "saksi") { P(g, "cyl", 0xbb5d3a, A(.35, .6, .35), A(0, .3, 0)); P(g, "cyl", 0x6e4527, A(.06, .6, .06), A(0, .8, 0)); P(g, "ico1", 0x3f7f34, A(.7, .9, .7), A(0, 1.5, 0)); }
                else if (kose == "sandik") { P(g, "box", 0x9a3a22, A(1.2, .7, .7), A(0, .35, 0)); P(g, "box", 0xe0b84a, A(1.22, .08, .72), A(0, .55, 0)); P(g, "box", 0xe0b84a, A(.08, .72, .72), A(-.35, .35, 0)); P(g, "box", 0xe0b84a, A(.08, .72, .72), A(.35, .35, 0)); }
                else { P(g, "box", 0x6e4424, A(.7, .6, .6), A(0, .3, 0)); P(g, "cyl", 0x3a2a20, A(.25, .04, .25), A(0, .62, 0)); P(g, "cone", 0xe0b84a, A(.35, .7, .35), A(.1, 1, 0), A(0, 0, -.9)); }
            }
            if (!string.IsNullOrEmpty(masa))
            {
                var g = G(sc, A(-1.05, 1.08, .6));
                if (masa == "semaver") { P(g, "cyl", 0xc98a3a, A(.18, .5, .18), A(0, .25, 0)); P(g, "cone", 0xc98a3a, A(.12, .15, .12), A(0, .56, 0)); P(g, "cyl", 0xc4492e, A(.09, .12, .09), A(0, .68, 0)); }
                else if (masa == "vazo") { P(g, "cyl", 0x3f6fa0, A(.12, .3, .12), A(0, .15, 0)); var fc = C(0xe8657a, 0xf4d03f, 0xffffff); for (int k = 0; k < 5; k++) P(g, "lsph", fc[k % 3], A(.08, .08, .08), A(Cos(k * 1.3) * .1, .38 + (k % 2) * .06, Sin(k * 1.3) * .1)); }
                else { P(g, "cyl", 0xa6773f, A(.25, .14, .25), A(0, .07, 0)); var fc = C(0xd43a2a, 0xf3c63c, 0x7cc04a); for (int k = 0; k < 6; k++) P(g, "lsph", fc[k % 3], A(.08, .08, .08), A(Cos(k) * .12, .17, Sin(k) * .12)); }
            }
            if (!string.IsNullOrEmpty(tavan))
            {
                var g = G(sc, A(-2.6, 4.3, 2.2)); P(g, "cyl", 0x333333, A(.02, 1, .02), A(0, .5, 0));
                if (tavan == "fener") { P(g, "cyl", 0xffd27a, A(.16, .3, .16), A(0, -.1, 0), null, new MatOpts { emissive = 0xffb648, emissiveIntensity = 1.2f }); Eng_PointLight(g, 0xffc070, .6f, 12); }
                else
                {
                    P(g, "tor", 0xc98a3a, A(.6, .6, .6), A(0, -.1, 0), A(PI / 2, 0, 0));
                    for (int k = 0; k < 6; k++) { double a = k / 6.0 * 6.28; P(g, "cone", 0xfff1c8, A(.05, .2, .05), A(Cos(a) * .6, .02, Sin(a) * .6), null, new MatOpts { emissive = 0xffd27a, emissiveIntensity = 1.5f }); }
                    Eng_PointLight(g, 0xffe0a0, .7f, 12);
                }
            }
        }
        internal static void FarmDecor(Transform g)
        {
            var D = S.decor.farm; string b1 = GetOr(D, "bahce1"), b2 = GetOr(D, "bahce2"), gi = GetOr(D, "giris");
            if (!string.IsNullOrEmpty(b1))
            {
                var s = G(g, A(-3.6, 0, 4.4));
                if (b1 == "heykel") { P(s, "cyl", StoneMat(), A(.4, .6, .4), A(0, .3, 0)); P(s, "sph", 0xf5d24a, A(.3, .25, .38), A(0, .8, 0)); P(s, "sph", 0xf5d24a, A(.17, .17, .17), A(0, 1.1, .2)); P(s, "box", 0xf08a24, A(.1, .05, .18), A(0, 1.08, .38)); }
                else { F(s, 0x7a4a28, A(1.6, .12, 1), A(0, .06, 0)); Flowers(s, 0, 0, 1.3, 77); }
            }
            if (!string.IsNullOrEmpty(b2))
            {
                var s = G(g, A(3.4, 0, 4.2));
                if (b2 == "kusevi") { P(s, "box", 0x6e4a2a, A(.08, 1.6, .08), A(0, .8, 0)); P(s, "box", 0xc9774a, A(.45, .4, .4), A(0, 1.75, 0)); GableRoof(G(s), .45, .4, .25, 1.95, 0x2f6b3a, 0xc9774a, "x", .06); P(s, "cyl", 0x2a1a10, A(.07, .02, .07), A(0, 1.75, .21), A(PI / 2, 0, 0)); }
                else { P(s, "cyl", StoneMat(), A(.8, .45, .8), A(0, .22, 0)); P(s, "cyl", 0x5aa0c8, A(.68, .05, .68), A(0, .44, 0)); P(s, "cyl", 0xb4ab9b, A(.12, .8, .12), A(0, .7, 0)); P(s, "sph", 0xb4ab9b, A(.25, .12, .25), A(0, 1.1, 0)); }
            }
            if (!string.IsNullOrEmpty(gi))
            {
                var s = G(g, A(0, 0, 5.4));
                if (gi == "bayrak")
                {
                    foreach (var x in new[] { -1.6, 1.6 }) P(s, "cyl", 0x6e4a2a, A(.05, 2.2, .05), A(x, 1.1, 0));
                    var bc = C(0xc4492e, 0xf2b330, 0x3f6fa0, 0x2f6b3a);
                    for (int k = 0; k < 9; k++) { double x = -1.4 + k * .35, y = 2 - Sin(k / 8.0 * PI) * .35; P(s, "cone", bc[k % 4], A(.13, .3, .03), A(x, y - .15, 0), A(PI, 0, 0)); }
                }
                else
                {
                    var ar = Eng_Mesh(s, Geo.Torus(1.4f, .12f, 6, 18, (float)PI), M(0x7a4e2d), true, false, "arch"); SetPos(ar, 0, .1, 0); Outline(ar);
                    var fc = C(0xf3b6c8, 0xffffff, 0xe8657a);
                    for (int k = 0; k < 12; k++) { double a = k / 11.0 * PI; P(s, "lsph", fc[k % 3], A(.13, .13, .13), A(Cos(a) * 1.4, .1 + Sin(a) * 1.4, .1)); }
                }
            }
        }

        // --- panayır ---
        internal static void FairModel(Transform g)
        {
            var stripes = ToonMat(new MatOpts { map = CanvasTex(128, 32, (x, w, h) => { for (int i = 0; i < 8; i++) x.FillRect(i * 16, 0, 16, h, i % 2 == 1 ? "#f6efe0" : "#c4492e"); }, new double[] { 2, 1 }) });
            F(g, 0xd8c8a2, A(11, .04, 7.5), A(0, .02, 1));
            P(g, "cyl", stripes, A(2.4, 1.8, 2.4), A(0, .9, -1)); P(g, "cone", stripes, A(2.8, 1.8, 2.8), A(0, 2.7, -1)); P(g, "cyl", 0x6e4a2a, A(.05, .8, .05), A(0, 3.9, -1));
            var fl = P(g, "box", 0xf2b330, A(.6, .35, .03), A(.3, 4.1, -1)); AddFx(g, (dt, t) => SetRotY(fl, Sin(t * 3) * .4));
            P(g, "box", 0x2a1a10, A(1, 1.3, .05), A(0, .65, 1.4));
            var pd = G(g, A(3.4, 0, 1.2));
            foreach (var e in new[] { A(0, .6, 0xf2b330), A(-.8, .4, 0xc9ccd2), A(.8, .25, 0xc98a3a) }) P(pd, "box", (int)e[2], A(.75, e[1], .75), A(e[0], e[1] / 2, 0));
            var jt = G(g, A(-3.2, 0, 1.4)); P(jt, "box", 0xf6efe0, A(2, .8, .7), A(0, .4, 0)); P(jt, "box", 0x3f6fa0, A(2.02, .2, .72), A(0, .7, 0));
            var judge = Person(new PersonOpt { shirt = 0x2a2a2a, pants = 0x2a2a2a, hat = "cap", hatc = 0x2a2a2a, mus = true }); SetPos(judge, -3.2, 0, .7); judge.SetParent(g, false); AddFx(g, IdlePerson(judge, 1));
            P(g, "sph", 0xf08a24, A(.75, .55, .75), A(1.8, .55, 2.6)); P(g, "cyl", 0x5f8a3a, A(.06, .25, .06), A(1.8, 1.15, 2.6));
            foreach (var x in new[] { -5, 5 }) P(g, "cyl", 0x6e4a2a, A(.06, 3.3, .06), A(x, 1.65, 3));
            var bc = C(0xc4492e, 0xf2b330, 0x3f6fa0, 0x2f6b3a);
            for (int k = 0; k < 15; k++) { double t = k / 14.0, x = -5 + 10 * t, y = 3.2 - Sin(t * PI) * .6; P(g, "cone", bc[k % 4], A(.14, .32, .03), A(x, y - .15, 3), A(PI, 0, 0)); }
            var v1 = Person(new PersonOpt { dress = 0xc4492e, shirt = 0xc4492e, hat = "scarf", hatc = 0xf2b330 }); v1.SetParent(g, false); AddFx(g, WalkPerson(v1, new[] { new[] { -2, 3.8 }, new[] { 2.5, 3.8 }, new[] { 2.5, 2.2 }, new[] { -2, 2.6 } }, 1, 2));
            var v2 = Person(new PersonOpt { shirt = 0x2f6b3a, hat = "cap", hatc = 0x7a4e2d }); v2.SetParent(g, false); AddFx(g, WalkPerson(v2, new[] { new[] { 3.5, 4.0 }, new[] { -3.5, 3.4 } }, .9, 3));
        }

        // --- kasaba ve özel konum modelleri ---
        internal static Transform Stall(Transform par, double x, double z, double ry, int ac)
        {
            var g = G(par, A(x, 0, z), ry);
            P(g, "box", 0x9a6a3e, A(2.6, .9, 1), A(0, .45, 0)); P(g, "box", 0x7a4e2d, A(2.8, .1, 1.2), A(0, .93, 0));
            foreach (var sx in new[] { -1.25, 1.25 }) foreach (var sz in new[] { -.45, .45 }) P(g, "box", 0x6e4a2a, A(.1, 2.1, .1), A(sx, 1.05, sz));
            for (int k = 0; k < 6; k++) { var a = P(g, "box", k % 2 == 1 ? 0xf6efe0 : ac, A(.46, .05, 1.5), A(-1.15 + k * .46, 2.15, .15)); SetRot(a, .3, 0, 0); }
            var prod = C(0xe03c2c, 0xf3c63c, 0x7cc04a, 0xf5a623);
            for (int k = 0; k < 3; k++) { Crate(g, -.8 + k * .8, .7, .36); for (int m = 0; m < 3; m++) P(g, "lsph", prod[(k + m) % 4], A(.1, .1, .1), A(-.85 + k * .8 + (m % 2) * .15, 1.02, .62 + (m > 1 ? .12 : 0))); }
            return g;
        }
        internal static void MarketModel(Transform g)
        {
            Stall(g, -3.3, -.2, .3, 0xc4492e); Stall(g, 0, -1.3, 0, 0x3f6fa0); Stall(g, 3.3, -.2, -.3, 0x2f6b3a);
            var sellers = new[]
            {
                new { x = -3.5, z = -1.25, ry = .3, o = new PersonOpt { shirt = 0xf6efe0, apron = 0xc4492e, hat = "scarf", hatc = 0xc4492e, dress = 0x8a5a32 } },
                new { x = 0.0, z = -2.35, ry = 0.0, o = new PersonOpt { shirt = 0x3f6fa0, hat = "cap", hatc = 0x3f6fa0, mus = true } },
                new { x = 3.5, z = -1.25, ry = -.3, o = new PersonOpt { shirt = 0xe9e2cf, apron = 0x2f6b3a, hat = "straw" } },
            };
            for (int i = 0; i < sellers.Length; i++) { var sl = sellers[i]; var p = Person(sl.o); SetPos(p, sl.x, 0, sl.z); SetRotY(p, sl.ry); p.SetParent(g, false); AddFx(g, IdlePerson(p, i * 2)); }
            var cu = Person(new PersonOpt { shirt = 0xb5402c, hat = "cap", hatc = 0x2a2a2a }); cu.SetParent(g, false);
            AddFx(g, WalkPerson(cu, new[] { new[] { -3.3, .9 }, new[] { 0, .1 }, new[] { 3.3, .9 }, new[] { 0, 3.0 } }, .9, 2.5));
            var fn = G(g, A(0, 0, 2.2));
            P(fn, "cyl", StoneMat(), A(1, .5, 1), A(0, .25, 0)); P(fn, "cyl", 0x5aa0c8, A(.85, .05, .85), A(0, .49, 0)); P(fn, "cyl", 0xb4ab9b, A(.15, 1, .15), A(0, .8, 0)); P(fn, "sph", 0xb4ab9b, A(.3, .15, .3), A(0, 1.3, 0));
            var drops = new List<Transform>();
            for (int k = 0; k < 6; k++) { var d = Eng_Mesh(fn, GE["lsph"], ToonMat(new MatOpts { color = 0xbfe6ff }), false, false, "drop"); Eng_Scale(d, .06); drops.Add(d); }
            AddFx(g, (dt, t) => { for (int k = 0; k < drops.Count; k++) { double ph = (t * 1.2 + k / 6.0) % 1, a = k / 6.0 * 6.28; SetPos(drops[k], Cos(a) * ph * .7, 1.35 + ph * .4 - ph * ph * 1.3, Sin(a) * ph * .7); } });
            Sack(g, -4.8, 1.5); Sack(g, -4.5, 1.9, 0xd8c79e);
            var cart = G(g, A(4.9, 0, 1.8), .4); P(cart, "box", 0x9a6a3e, A(1.4, .4, .9), A(0, .75, 0));
            foreach (var s in new[] { -1, 1 }) P(cart, "tor", 0x5a3a22, A(.38, .38, .38), A(0, .42, s * .5));
            P(cart, "box", 0x6e4a2a, A(1.2, .06, .06), A(1.2, .6, 0)); for (int k = 0; k < 4; k++) P(cart, "lsph", 0xf3c63c, A(.18, .18, .18), A(-.3 + k * .2, 1.05, 0));
            F(g, 0xcfb27e, A(11, .04, 6), A(0, .02, .6));
        }
        internal static void HanModel(Transform g)
        {
            { var m = Person(new PersonOpt { shirt = 0x3f6fa0, pants = 0x2a2a2a, hat = "fez", mus = true }); SetPos(m, 1.7, 0, 2.3); SetRotY(m, .3); m.SetParent(g, false); AddFx(g, IdlePerson(m, 3)); }
            P(g, "box", 0xe9d6aa, A(4.6, 2.4, 3), A(0, 1.2, -.6)); P(g, "box", 0x8a5a32, A(4.7, .15, 3.1), A(0, 1.25, -.6)); GableRoof(G(g, A(0, 0, -.6)), 4.6, 3, 1.4, 2.4, 0x7a3a28, 0xe9d6aa, "x", .3); Door(g, 0, 0, .92, .8, 1.05, 0x5a3418);
            foreach (var x in new[] { -1.6, 1.6 }) { WindowM(g, x, .75, .92, .5, .5); WindowM(g, x, 1.8, .92, .45, .45); }
            WindowM(g, 0, 1.85, .92, .45, .4); P(g, "box", 0x8b8478, A(.4, 1.2, .4), A(1.6, 3.5, -1.2)); AddSmoke(g, A(1.6, 4.2, -1.2), () => true);
            var sg = G(g, A(1.0, 1.5, 1.25)); P(sg, "box", 0x4f3119, A(.06, .06, .7), A(0, .3, -.35)); var pl = P(sg, "box", 0xc9a050, A(.75, .45, .06), A(0, 0, 0));
            AddFx(g, (dt, t) => SetRot(pl, Sin(t * 1.4) * .12, 0, 0));
            for (int k = 0; k < 3; k++) P(g, "cyl", 0x8a5a32, A(.32, .7, .32), A(-2 + k * .7, .35, 1.5));
            var bd = G(g, A(2.9, 0, 1.9), -.3);
            foreach (var x in new[] { -.8, .8 }) P(bd, "box", 0x6e4a2a, A(.13, 2, .13), A(x, 1, 0));
            P(bd, "box", 0x9a6a3e, A(1.9, 1.1, .1), A(0, 1.4, 0)); P(bd, "box", 0x5e3d22, A(2.1, .12, .35), A(0, 2.05, 0));
            var nc = C(0xfff8e6, 0xf6e7b8, 0xe8f0ff, 0xfff8e6);
            for (int k = 0; k < 4; k++) { var n = P(bd, "box", nc[k], A(.36, .42, .02), A(-.6 + k * .4, 1.4 + (k % 2) * .1, .07)); SetRot(n, 0, 0, (k - 1.5) * .06); }
            F(g, 0xcfb27e, A(8, .04, 5), A(0, .02, .8));
        }
        internal static Transform Well(Transform par, double x, double z)
        {
            var g = G(par, A(x, 0, z));
            P(g, "cyl", StoneMat(), A(.6, .6, .6), A(0, .3, 0)); P(g, "cyl", 0x2a3a4a, A(.5, .05, .5), A(0, .58, 0));
            foreach (var s in new[] { -1, 1 }) P(g, "box", 0x6e4a2a, A(.08, 1.2, .08), A(s * .5, 1.1, 0));
            P(g, "cyl", 0x6e4a2a, A(.05, 1.1, .05), A(0, 1.45, 0), A(0, 0, PI / 2));
            GableRoof(G(g), 1.3, 1, .45, 1.65, 0xb5402c, 0x8a5a32, "x", .1); P(g, "cyl", 0x8a6038, A(.12, .18, .12), A(.2, .9, 0));
            return g;
        }
        internal static Transform Scarecrow(Transform par, double x, double z)
        {
            var g = G(par, A(x, 0, z)); P(g, "cyl", 0x6e4a2a, A(.06, 1.8, .06), A(0, .9, 0));
            var top = G(g, A(0, 1.3, 0)); P(top, "box", 0x6e4a2a, A(1.4, .07, .07)); P(top, "box", 0x5a7ab0, A(.5, .6, .25), A(0, -.1, 0));
            foreach (var s in new[] { -1, 1 }) P(top, "box", 0x5a7ab0, A(.45, .16, .18), A(s * .45, 0, 0));
            P(top, "sph", 0xe2c890, A(.22, .22, .22), A(0, .42, 0)); P(top, "cyl", 0x8a6a3a, A(.38, .03, .38), A(0, .6, 0)); P(top, "cyl", 0x8a6a3a, A(.18, .2, .18), A(0, .7, 0));
            P(top, "lsph", 0x2a1a10, A(.035, .035, .035), A(.08, .45, .2)); P(top, "lsph", 0x2a1a10, A(.035, .035, .035), A(-.08, .45, .2));
            AddFx(par, (dt, t) => SetRot(top, 0, Sin(t * .7) * .15, Sin(t * 1.6) * .08));
            return g;
        }
        internal static Transform Foundation(Transform par, bool locked)
        {
            var g = G(par); P(g, "box", 0x9a8f7a, A(3.4, .16, 3.4), A(0, .08, 0)); P(g, "box", 0xc2b494, A(3, .18, 3), A(0, .1, 0));
            foreach (var ab in new[] { A(-1.5, -1.5, 0), A(1.5, -1.5, 0), A(-1.5, 1.5, 0), A(1.5, 1.5, 0) }) P(g, "box", 0xc9a05a, A(.09, .7, .09), A(ab[0], .45, ab[1]));
            foreach (var e in new[] { new[] { -1.5, -1.5, 1.5, -1.5 }, new[] { 1.5, -1.5, 1.5, 1.5 }, new[] { 1.5, 1.5, -1.5, 1.5 }, new[] { -1.5, 1.5, -1.5, -1.5 } })
                P(g, "box", 0xe8d9b0, A(Hyp(e[2] - e[0], e[3] - e[1]), .03, .03), A((e[0] + e[2]) / 2, .68, (e[1] + e[3]) / 2), A(0, -Math.Atan2(e[3] - e[1], e[2] - e[0]), 0));
            var s = G(g, A(0, 0, 1.1)); P(s, "box", 0x6e4a2a, A(.09, 1.1, .09), A(0, .55, 0)); var b = P(s, "box", locked ? 0x8a8478 : 0xf2b330, A(.8, .5, .06), A(0, 1.05, 0));
            if (!locked)
            {
                var sc = C(0xb4ab9b, 0xc9a05a, 0xb4ab9b);
                for (int k = 0; k < 3; k++) P(g, "box", sc[k], A(.5, .18, .3), A(-.6 + k * .25, .28 + k * .18, -.6));
                AddFx(par, (dt, t) => SetRot(b, 0, 0, Sin(t * 2.4) * .08));
            }
            return g;
        }
        internal static void RaceTrack(Transform g, Unit hara)
        {
            const double rx = 7.5, rz = 5;
            var ring = Eng_Mesh(g, Geo.Ring(.7f, 1, 72), ToonMat(new MatOpts { color = 0xc9a06a }), false, true, "ring"); SetRot(ring, -PI / 2, 0, 0); ring.localScale = new Vector3((float)rx, (float)rz, 1); SetPos(ring, 0, .05, 0);
            var inner = Eng_Mesh(g, Geo.Circle(.7f, 64), ToonMat(new MatOpts { color = 0x8cb655 }), false, true, "inner"); SetRot(inner, -PI / 2, 0, 0); inner.localScale = new Vector3((float)rx, (float)rz, 1); SetPos(inner, 0, .045, 0);
            const int N = 56;
            foreach (var rr in new[] { 1.02, .68 })
            {
                double[] prev = null;
                for (int k = 0; k <= N; k++)
                {
                    double a = (double)k / N * PI * 2, x = Cos(a) * rx * rr, z = Sin(a) * rz * rr;
                    if (k < N && k % 2 == 0) P(g, "box", 0xfaf6ee, A(.09, .6, .09), A(x, .3, z));
                    if (prev != null) P(g, "box", 0xfaf6ee, A(Hyp(x - prev[0], z - prev[1]) + .02, .07, .07), A((x + prev[0]) / 2, .58, (z + prev[1]) / 2), A(0, -Math.Atan2(z - prev[1], x - prev[0]), 0));
                    prev = new[] { x, z };
                }
            }
            Eng_CastShadow(P(g, "box", 0xfaf6ee, A(.25, .06, rz * .32), A(rx * .85, .07, 0)), false);
            var gs = G(g, A(0, 0, rz + 2.3)); var sc = C(0x9a6a3e, 0x8a5a32, 0x7a4e2d);
            for (int k = 0; k < 3; k++) P(gs, "box", sc[k], A(7, .5, 1), A(0, .25 + k * .5, -k * .0 + k * .9 - 0.9), A(0, 0, 0));
            foreach (var x in new[] { -3.4, 3.4 }) P(gs, "box", 0x6e4a2a, A(.15, 2.8, .15), A(x, 1.4, 1.4));
            { var rf = P(gs, "box", 0xc4492e, A(7.6, .12, 2.8), A(0, 2.8, .5)); SetRot(rf, -.12, 0, 0); }
            var crowd = new List<Transform>(); var cy0 = new List<double>(); var q = Rng(77); var cc = C(0xc4492e, 0x3f6fa0, 0xf2b330, 0x2f6b3a, 0xf6efe0);
            for (int k = 0; k < 18; k++)
            {
                int row = k % 3; double cx = -3 + Math.Floor(k / 3.0) * 1.2 + q() * .3, cyy = .75 + row * .5, czz = -.9 + row * .9;
                crowd.Add(P(gs, "sph", cc[k % 5], A(.17, .2, .17), A(cx, cyy, czz))); cy0.Add(cyy);
            }
            AddFx(g, (dt, t) => { for (int k = 0; k < crowd.Count; k++) { var p = Eng_Pos3(crowd[k]); p.y = (float)(cy0[k] + Math.Max(0, Sin(t * 6 + k)) * .12); Eng_SetPos3(crowd[k], p); } });
            foreach (var x in new[] { -rx - 1, rx + 1 })
            {
                P(g, "cyl", 0xfaf6ee, A(.05, 3, .05), A(x, 1.5, rz + .5)); var fl = P(g, "box", 0xc4492e, A(.6, .35, .03), A(x + .32, 2.8, rz + .5));
                double fx = x; AddFx(g, (dt, t) => SetRotY(fl, Sin(t * 3 + fx) * .3));
            }
            if (hara != null)
            {
                int n = Math.Min(5, 2 + hara.l);
                var cols = new[] { C(0x8a5530, 0xc4492e), C(0x3a2a22, 0x3f6fa0), C(0xd9c3a0, 0xf2b330), C(0x6a4a30, 0x2f6b3a), C(0xb07a4a, 0xf6efe0) };
                for (int k = 0; k < n; k++)
                {
                    int kk = k;
                    var h = Horse(cols[k][0], 0x2e1d12, cols[k][1]); Eng_Scale(h, .55); h.SetParent(g, false);
                    double a = k * .5; double sp = .42 + k * .03 + Rnd() * .05, rr = .85 - k * .035;
                    var legs = UData.Of(h).legs;
                    AddFx(g, (dt, t) =>
                    {
                        a += dt * sp * (1 + Sin(t * .7 + kk) * .15);
                        double x = Cos(a) * rx * rr, z = Sin(a) * rz * rr;
                        SetPos(h, x, Math.Abs(Sin(t * 10 + kk)) * .15, z); SetRotY(h, Math.Atan2(-Sin(a) * rx, Cos(a) * rz));
                        for (int i = 0; i < legs.Count; i++) SetRot(legs[i], Sin(t * 10 + kk + (i > 1 ? PI : 0) + (i % 2) * .6) * .7, 0, 0);
                    });
                }
            }
        }
    }

    // ---------- Game üzerindeki genel adlar (JS küresel adları) ----------
    public static partial class Game
    {
        public static Transform GableRoof(Transform par, double w, double d, double h, double y, int roofC, int wallC, string axis = "x", double ov = .28, double th = .12) => EngM.GableRoof(par, w, d, h, y, roofC, wallC, axis, ov, th);
        public static Transform WindowM(Transform par, double x, double y, double z, double w = .42, double h = .48, double ry = 0, double lit = .5) => EngM.WindowM(par, x, y, z, w, h, ry, lit);
        public static Transform Door(Transform par, double x, double y, double z, double w = .5, double h = .85, int c = 0x6b3f20, double ry = 0) => EngM.Door(par, x, y, z, w, h, c, ry);
        public static void FenceRun(Transform par, double x1, double z1, double x2, double z2, int c = 0x9a7247, double ht = .62) => EngM.FenceRun(par, x1, z1, x2, z2, c, ht);
        public static void Bush(Transform par, double x, double z, double s = 1, int c = 0x4f8f37) => EngM.Bush(par, x, z, s, c);
        public static void Flowers(Transform par, double x, double z, double r, double seed) => EngM.Flowers(par, x, z, r, seed);
        public static void SwayFx(Transform par, Transform top, double x) => EngM.SwayFx(par, top, x);
        public static void Pine(Transform par, double x, double z, double s = 1) => EngM.Pine(par, x, z, s);
        public static void Oak(Transform par, double x, double z, double s = 1, bool apples = false) => EngM.Oak(par, x, z, s, apples);
        public static void AddSmoke(Transform host, double[] ch, Func<bool> isOn, Transform fxHost = null) => EngM.AddSmoke(host, ch, isOn, fxHost);
        public static void AddSmoke(Transform host, float[] ch, Func<bool> isOn, Transform fxHost = null) => EngM.AddSmoke(host, new double[] { ch[0], ch[1], ch[2] }, isOn, fxHost);
        public static Transform Chicken(int col = 0xfaf6ee) => EngM.Chicken(col);
        public static Transform Cow(int body = 0xf7f3ea, int spot = 0x2b2522, bool bull = false) => EngM.Cow(body, spot, bull);
        public static Transform Sheep() => EngM.Sheep();
        public static Transform Horse(int c = 0x8a5530, int m = 0x2e1d12, int? jockey = null) => EngM.Horse(c, m, jockey);
        public static Action<float, float> WanderFx(Transform a, double cx, double cz, double rx, double rz, double speed = .6, double peck = .6) => EngM.WanderFx(a, cx, cz, rx, rz, speed, peck);
        public static Action<float, float> IdleFx(Transform a, double ph = 0) => EngM.IdleFx(a, ph);
        public static void Plant(Transform par, string type, int stage, double x, double z, Func<double> q) => EngM.Plant(par, type, stage, x, z, q);
        public static void FieldBed(Transform par, Job job, double w = 3.7, double d = 3.7, int rows = 5, int cols = 6) => EngM.FieldBed(par, job, w, d, rows, cols);
        public static void FieldBed(Transform par, Field t, double w = 3.7, double d = 3.7, int rows = 5, int cols = 6) => EngM.FieldBed(par, t != null ? t.job : null, w, d, rows, cols);
        public static void FieldBed(Transform par, Unit t, double w = 3.7, double d = 3.7, int rows = 5, int cols = 6) => EngM.FieldBed(par, t != null ? t.job : null, w, d, rows, cols);
        public static Transform House(int l) => EngM.House(l);
        public static void Workshop(Transform g, int wall, int roof, double w = 2.5, double h = 1.5, double d = 1.9, double z = -.4) => EngM.Workshop(g, wall, roof, w, h, d, z);
        public static void Crate(Transform g, double x, double z, double s = .38, int c = 0x9a6b3c) => EngM.Crate(g, x, z, s, c);
        public static void Sack(Transform g, double x, double z, int c = 0xe2d3ae) => EngM.Sack(g, x, z, c);
        // MODELS[k](g, S.units[k]) — t null olabilir (kurulmamış/önizleme)
        static Dictionary<string, Action<Transform, Unit>> _models;
        public static Dictionary<string, Action<Transform, Unit>> MODELS => _models ?? (_models = EngM.BuildModels());
        public static Transform Person(PersonOpt o) => EngM.Person(o);
        // person({shirt:…, hat:…}) adlandırılmış bağımsız değişkenlerle
        public static Transform Person(int? skin = null, int? shirt = null, int? pants = null, int? dress = null, int? apron = null, string hat = null, int? hatc = null, bool mus = false) =>
            EngM.Person(new PersonOpt { skin = skin, shirt = shirt, pants = pants, dress = dress, apron = apron, hat = hat, hatc = hatc, mus = mus });
        public static Action<float, float> IdlePerson(Transform p, double ph) => EngM.IdlePerson(p, ph);
        public static Action<float, float> WalkPerson(Transform p, double[][] pts, double speed = 1.2, double pause = 1.5) => EngM.WalkPerson(p, pts, speed, pause);
        public static readonly Dictionary<string, string[]> KILIM = new Dictionary<string, string[]>
        {
            { "kilim_mavi", new[] { "#2c4f8a", "#c4492e", "#f0c24b", "#f6ead0", "#a3271f" } },
            { "kilim_yesil", new[] { "#3f7a4a", "#f0c24b", "#a3271f", "#f6ead0", "#2c3e6b" } },
            { "kilim_altin", new[] { "#c98a12", "#7a2f22", "#f6ead0", "#2c3e6b", "#3f7a4a" } },
        };
        public static void HouseDecor(Transform sc) => EngM.HouseDecor(sc);
        public static void FarmDecor(Transform g) => EngM.FarmDecor(g);
        public static void FairModel(Transform g) => EngM.FairModel(g);
        public static Transform Stall(Transform par, double x, double z, double ry, int ac) => EngM.Stall(par, x, z, ry, ac);
        public static void MarketModel(Transform g) => EngM.MarketModel(g);
        public static void HanModel(Transform g) => EngM.HanModel(g);
        public static Transform Well(Transform par, double x, double z) => EngM.Well(par, x, z);
        public static Transform Scarecrow(Transform par, double x, double z) => EngM.Scarecrow(par, x, z);
        public static Transform Foundation(Transform par, bool locked) => EngM.Foundation(par, locked);
        public static void RaceTrack(Transform g, Unit hara) => EngM.RaceTrack(g, hara);
    }
}
