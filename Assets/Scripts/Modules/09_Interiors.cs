// ================= İÇ MEKÂN (09-interiors.js) =================
// Kümes/ahır/ağıl/besi/hara/kulübe iç mekânları: ayrı kök (W_LAYER_INT katmanı), ayrı kamera (Icam) ve yörünge denetimi (Ictl).
// Ayrıca iris geçişi (fade), gövde sınıfları (applyUI / setModeUI) ve iç mekâna giriş/çıkış.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Ciftlik
{
    public class Int_Interior
    {
        public string key; public Transform scene; public List<Action<float, float>> fx = new List<Action<float, float>>();
        public Transform dyn; public List<Transform> nests = new List<Transform>(); public List<double[]> buckets = new List<double[]>();
        public Transform shelf; public string sig = ""; public Action refresh;
        public readonly HashSet<Action<float, float>> dynFx = new HashSet<Action<float, float>>();   // JS: fx.dyn=1
    }
    public class Int_Def { public Func<Unit, Material> floor, wall; public Action<Int_Interior, Unit> build, dyn; }

    public static partial class Game
    {
        public static string Mode = "desk", Focus = null;
        public static Int_Interior Interior = null;
        public static Camera Icam; public static OrbitControls Ictl;

        // ---- gövde sınıfları (document.body.classList / dataset.mode) ----
        public static readonly HashSet<string> Int_BodyCls = new HashSet<string>();
        public static string Int_BodyMode = "desk";
        public static bool Int_BackHidden = true; public static string Int_BackText = "← Masa";   // #backBtn
        public static event Action Int_OnBody;
        public static bool Int_BodyHas(string c) => Int_BodyCls.Contains(c);
        public static void Int_BodyToggle(string c, bool on) { bool ch = on ? Int_BodyCls.Add(c) : Int_BodyCls.Remove(c); if (ch) Int_OnBody?.Invoke(); }

        static void Init_09()
        {
            Icam = Eng_Camera("icam", 42, .1f, 200); var go = Icam.gameObject;
            Icam.clearFlags = CameraClearFlags.SolidColor; Icam.backgroundColor = Hex3(0x2b1d12); Icam.cullingMask = 1 << W_LAYER_INT; Icam.enabled = false;
            go.AddComponent<World_CamHook>();
            Ictl = new OrbitControls(Icam) { enabled = false, enablePan = false, enableDamping = true, minDistance = 7, maxDistance = 18, minPolarAngle = .75f, maxPolarAngle = 1.4f, minAzimuthAngle = -.75f, maxAzimuthAngle = .75f, rotateSpeed = .5f };
            Ictl.mouseLEFT = OrbitControls.Act.ROTATE; Ictl.mouseMIDDLE = OrbitControls.Act.DOLLY; Ictl.mouseRIGHT = OrbitControls.Act.ROTATE;
            Ictl.touchONE = OrbitControls.Act.ROTATE; Ictl.touchTWO = OrbitControls.Act.DOLLY_ROTATE;
            // Hangi kameranın çizdiğini 33-loop (Loop_Cameras) seçer.
        }

        static Light Int_Light(Transform par, LightType type, int color, double intensity, double range = 10)
        {
            var l = Eng_PointLight(par, color, (float)intensity, (float)range); l.type = type; l.cullingMask = 1 << W_LAYER_INT;
            return l;
        }
        public static Action<float, float> Room(Transform sc, Material floorMat, Material wallMat)
        {
            W_Shadow(P(sc, "box", floorMat, W_V(14, .3, 10), W_V(0, -.15, 0)), false, true);
            P(sc, "box", wallMat, W_V(14.6, 5.6, .3), W_V(0, 2.8, -5.15)); P(sc, "box", wallMat, W_V(.3, 5.6, 10.3), W_V(-7.15, 2.8, 0)); P(sc, "box", wallMat, W_V(.3, 5.6, 10.3), W_V(7.15, 2.8, 0));
            for (int x = -6; x <= 6; x += 3) P(sc, "box", 0x4f3119, W_V(.32, .36, 10.4), W_V(x, 5.5, 0)); P(sc, "box", 0x4f3119, W_V(14.6, .4, .4), W_V(0, 5.6, 4.95)); foreach (var x in new[] { -6.95, 6.95 }) P(sc, "box", 0x4f3119, W_V(.4, 5.6, .4), W_V(x, 2.8, 4.95));
            P(sc, "box", 0x4f3119, W_V(14.3, .3, .15), W_V(0, .15, -4.98));
            var g = G(sc, W_V(3.6, 3.3, -4.98)); P(g, "box", 0x4f3119, W_V(2.1, 1.7, .12)); P(g, "box", 0xbfe6ff, W_V(1.8, 1.4, .14), W_V(0, 0, .01), null, new MatOpts { emissive = 0xffe4a8, emissiveIntensity = .9f }); P(g, "box", 0x4f3119, W_V(.08, 1.4, .18)); P(g, "box", 0x4f3119, W_V(1.8, .08, .18));
            Eng_HemiLight(sc, 0xfff0d6, 0x5a4030, .7f);
            var d = Eng_Sun(sc, 0xffe2b0, 1.1f, 5, 12, 9, true, 2048); d.cullingMask = 1 << W_LAYER_INT;
            var sl = Int_Light(sc, LightType.Spot, 0xffe7b8, .9, 22); sl.spotAngle = .6f * 2 * Mathf.Rad2Deg;
            sl.transform.localPosition = V(3.6, 3.6, -4.5); sl.transform.localRotation = Quaternion.LookRotation(V(2, 0, 1) - V(3.6, 3.6, -4.5));
            var dust = new List<Transform>();
            var sh = Shader.Find("Sprites/Default"); Material dm = null;
            if (sh != null) { dm = new Material(sh); dm.color = new Color(1, 242 / 255f, 200 / 255f, .5f); }
            for (int k = 0; k < 26; k++)
            {
                var m = P(sc, "lsph", 0xfff2c8, W_V(.025, .025, .025), W_V(1 + Rnd() * 5, Rnd() * 4.5, -4 + Rnd() * 7));
                var r = m.GetComponent<Renderer>(); if (r != null && dm != null) r.sharedMaterial = dm; W_Shadow(m, false, false); dust.Add(m);
            }
            return (dt, t) => { for (int k = 0; k < dust.Count; k++) { var p = W_Pos3(dust[k]); p.y += Mathf.Sin(t * .5f + k) * .002f; p.x += Mathf.Cos(t * .3f + k) * .002f; W_SetPos3(dust[k], p); } };
        }
        public static Transform HayBale(Transform sc, double x, double z, double y = 0, double ry = 0)
        {
            var g = G(sc, W_V(x, y, z), ry); P(g, "box", StdTex(TEX.straw(W_V(1, 1))), W_V(1.2, .6, .7), W_V(0, .3, 0));
            foreach (var s in new[] { -.3, .3 }) P(g, "box", 0x8a5a2a, W_V(.04, .62, .72), W_V(s, .3, 0)); return g;
        }
        public static void StallDivider(Transform sc, double x, double z1, double z2) { P(sc, "box", 0x6e4a2a, W_V(.16, 1.4, z2 - z1), W_V(x, .7, (z1 + z2) / 2)); P(sc, "box", 0x5a3a20, W_V(.2, .12, z2 - z1 + .1), W_V(x, 1.42, (z1 + z2) / 2)); }
        public static Transform Clickable(Transform o, string act) { foreach (var t in o.GetComponentsInChildren<Transform>(true)) W_UD(t).act = act; return o; }

        public static readonly Dictionary<string, Int_Def> INT = new Dictionary<string, Int_Def>
        {
            ["kumes"] = new Int_Def
            {
                floor = t => StdTex(TEX.straw(W_V(4, 3))), wall = t => StdTex(TEX.planks("#b58a58", "#a87d4c", W_V(3, 1.5), true)),
                build = (ic, t) =>
                {
                    var sc = ic.scene; P(sc, "box", 0x7a5233, W_V(11, .18, 1.3), W_V(0, 1.5, -4.35));
                    for (int k = 0; k < 6; k++)
                    {
                        double x = -4.5 + k * 1.8; var nb = G(sc, W_V(x, 1.6, -4.35)); P(nb, "box", 0x9a6a3e, W_V(1.6, .08, 1.1), W_V(0, 0, 0));
                        foreach (var s in new[] { -1, 1 }) P(nb, "box", 0x9a6a3e, W_V(.08, .9, 1.1), W_V(s * .78, .45, 0)); P(nb, "box", 0x9a6a3e, W_V(1.6, .9, .08), W_V(0, .45, -.52)); P(nb, "box", 0x8a5a32, W_V(1.6, .25, .08), W_V(0, .12, .52)); P(nb, "ico1", 0xe0c070, W_V(.6, .15, .4), W_V(0, .12, 0)); ic.nests.Add(nb);
                    }
                    P(sc, "box", 0x8a5a32, W_V(11, .08, 1.3), W_V(0, 2.55, -4.35));
                    for (int k = 0; k < 4; k++) P(sc, "cyl", 0x8a6038, W_V(.07, 4, .07), W_V(-6.4, .9 + k * .7, -.8), W_V(Math.PI / 2, 0, 0)); foreach (var z in new[] { -2.8, 1.2 }) P(sc, "box", 0x6e4a2a, W_V(.12, 3.4, .12), W_V(-6.4, 1.7, z));
                    P(sc, "box", 0x8a5a32, W_V(3.2, .35, .7), W_V(0, .18, 1.6)); P(sc, "box", 0x7a4a28, W_V(3.3, .1, .8), W_V(0, .38, 1.6));
                    P(sc, "cyl", 0xc4492e, W_V(.5, .15, .5), W_V(4.6, .08, 1.8)); P(sc, "cyl", 0xbfe6ff, W_V(.3, .9, .3), W_V(4.6, .6, 1.8), null, new MatOpts { transparent = true, opacity = .6f }); P(sc, "cone", 0xc4492e, W_V(.32, .3, .32), W_V(4.6, 1.2, 1.8));
                    HayBale(sc, 5.4, -2.6); HayBale(sc, 5.4, -1.7); HayBale(sc, 5.4, -2.15, .6, .3);
                    int n = 3 + 2 * t.l; int[] cc = { 0xfaf6ee, 0xb8693a, 0xfaf6ee, 0x3a2a22, 0xd99a5a };
                    for (int k = 0; k < n; k++) { var c = Chicken(cc[k % 5]); W_Scale(c, 1.7); W_Add(sc, c); Clickable(c, "pet"); ic.fx.Add(WanderFx(c, 0, .2, 5, 2.6, .9)); }
                },
                dyn = (ic, t) =>
                {
                    var d = ic.dyn; if (t.job != null && Prog(t.job) < 1) P(d, "box", 0xe8c25a, W_V(3, .08, .5), W_V(0, .44, 1.6));
                    if (t.job != null && Prog(t.job) >= 1)
                    {
                        int n = Scaled(B["kumes"].recipes[0], t.l).@out["yumurta"];
                        for (int k = 0; k < n; k++)
                        {
                            var nb = ic.nests[k % ic.nests.Count]; var np = W_Pos3(nb);
                            var e = P(d, "sph", k % 2 == 0 ? 0xf6ead2 : 0xd9a066, W_V(.13, .17, .13), W_V(np.x - .3 + Math.Floor(k / 6.0) * .3, np.y + .32, np.z + .1)); Clickable(e, "collect");
                        }
                    }
                }
            },
            ["ahir"] = new Int_Def
            {
                floor = t => StdTex(TEX.planks("#8a6a48", "#7d5f40", W_V(4, 3))), wall = t => StdTex(TEX.planks("#a33a2a", "#962f22", W_V(3, 1.5), true)),
                build = (ic, t) =>
                {
                    var sc = ic.scene; int n = Math.Min(4, t.l + 1); double W = 2.8, x0 = -(n - 1) * W / 2;
                    for (int k = 0; k <= n; k++) StallDivider(sc, x0 - W / 2 + k * W, -4.8, -.4);
                    for (int k = 0; k < n; k++) { double x = x0 + k * W; var c = Cow(); W_Scale(c, 1.55); SetPos(c, x, 0, -2.6); W_Add(sc, c); Clickable(c, "pet"); ic.fx.Add(IdleFx(c, k * 1.3)); P(sc, "box", 0x7a4a28, W_V(1.6, .5, .6), W_V(x, .25, .3)); ic.buckets.Add(new[] { x + .9, .9 }); }
                    HayBale(sc, -5.6, 2.4); HayBale(sc, -5.6, 3.2); HayBale(sc, -5.6, 2.8, .6); P(sc, "ico1", 0xd9b45e, W_V(1, .25, .8), W_V(-4.4, .1, 3));
                    var fork = G(sc, W_V(6.4, 0, -1)); SetRot(fork, 0, 0, .2); P(fork, "cyl", 0x8a6038, W_V(.04, 2.4, .04), W_V(0, 1.2, 0)); foreach (var s in new[] { -.1, 0, .1 }) P(fork, "cyl", 0x8a8a8a, W_V(.015, .35, .015), W_V(s, 2.5, 0));
                    var lan = G(sc, W_V(0, 4.6, 0)); P(lan, "cyl", 0x333333, W_V(.02, .8, .02), W_V(0, .4, 0)); P(lan, "cyl", 0xffd27a, W_V(.18, .3, .18), W_V(0, 0, 0), null, new MatOpts { emissive = 0xffb648, emissiveIntensity = 1.2f }); Int_Light(lan, LightType.Point, 0xffc070, .7, 14);
                },
                dyn = (ic, t) =>
                {
                    var d = ic.dyn; bool run = t.job != null && Prog(t.job) < 1, ready = t.job != null && Prog(t.job) >= 1;
                    foreach (var xz in ic.buckets)
                    {
                        double x = xz[0], z = xz[1]; if (run) P(d, "ico1", 0xd9b45e, W_V(.6, .15, .25), W_V(x - .9, .55, z - .6)); var b = G(d, W_V(x, 0, z)); P(b, "cyl", 0xc7ccd2, W_V(.25, .45, .22), W_V(0, .23, 0));
                        if (ready) { P(b, "cyl", 0xfdfcf6, W_V(.22, .03, .22), W_V(0, .44, 0), null, new MatOpts { emissive = 0xffffff, emissiveIntensity = .25f }); Clickable(b, "collect"); }
                    }
                }
            },
            ["agil"] = new Int_Def
            {
                floor = t => StdTex(TEX.straw(W_V(4, 3))), wall = t => StdTex(TEX.planks("#9b7a52", "#8e6e48", W_V(3, 1.5), true)),
                build = (ic, t) =>
                {
                    var sc = ic.scene; var rack = G(sc, W_V(0, 0, -4.6)); P(rack, "box", 0x6e4a2a, W_V(6, .15, .6), W_V(0, 1.2, 0)); for (int k = 0; k < 12; k++) P(rack, "box", 0x6e4a2a, W_V(.06, 1.2, .06), W_V(-2.8 + k * .5, 1.8, .2), W_V(.3, 0, 0)); P(rack, "ico1", 0xd9b45e, W_V(2.8, .4, .4), W_V(0, 1.5, 0));
                    HayBale(sc, -5.5, -3); HayBale(sc, 5.5, -3); P(sc, "box", 0x8a5a32, W_V(2.2, .35, .6), W_V(3.5, .18, 2.8));
                    int n = 2 + 2 * t.l; for (int k = 0; k < n; k++) { var s = Sheep(); W_Scale(s, 1.5); W_Add(sc, s); Clickable(s, "pet"); ic.fx.Add(WanderFx(s, 0, -.3, 4.8, 2.4, .5, .35)); }
                },
                dyn = (ic, t) =>
                {
                    if (t.job != null && Prog(t.job) >= 1)
                    {
                        int n = Scaled(B["agil"].recipes[0], t.l).@out["yun"];
                        for (int k = 0; k < n; k++) { var w = G(ic.dyn, W_V(-4 + k * 1.1, 0, 3.3)); for (int m = 0; m < 4; m++) P(w, "ico1", 0xf6f2ea, W_V(.35, .25, .35), W_V((m % 2) * .3, .2 + Math.Floor(m / 2.0) * .18, 0)); Clickable(w, "collect"); }
                    }
                    if (t.job != null && Prog(t.job) < 1) P(ic.dyn, "box", 0xe8c25a, W_V(2, .08, .45), W_V(3.5, .38, 2.8));
                }
            },
            ["besi"] = new Int_Def
            {
                floor = t => StdTex(TEX.planks("#7d6a55", "#736150", W_V(4, 3))), wall = t => StdTex(TEX.stone(W_V(3, 1.5))),
                build = (ic, t) =>
                {
                    var sc = ic.scene; int n = Math.Min(4, t.l + 1); double W = 2.8, x0 = -(n - 1) * W / 2; for (int k = 0; k <= n; k++) StallDivider(sc, x0 - W / 2 + k * W, -4.8, -.4);
                    for (int k = 0; k < n; k++) { double x = x0 + k * W; var c = Cow(0x7a4a2a, 0x5a3420, true); W_Scale(c, 1.55); SetPos(c, x, 0, -2.6); W_Add(sc, c); Clickable(c, "pet"); ic.fx.Add(IdleFx(c, k)); P(sc, "box", 0x7a4a28, W_V(1.6, .5, .6), W_V(x, .25, .3)); }
                    HayBale(sc, 5.6, 2.5); HayBale(sc, 5.6, 3.3);
                },
                dyn = (ic, t) =>
                {
                    if (t.job != null && Prog(t.job) >= 1)
                    {
                        int n = Scaled(B["besi"].recipes[0], t.l).@out["et"];
                        for (int k = 0; k < n; k++) { var c = G(ic.dyn, W_V(-2 + k * .9, 0, 3)); Crate(c, 0, 0, .7, 0xa7743f); P(c, "box", 0xc4492e, W_V(.3, .18, .02), W_V(0, .45, .36)); Clickable(c, "collect"); }
                    }
                }
            },
            ["hara"] = new Int_Def
            {
                floor = t => StdTex(TEX.straw(W_V(4, 3))), wall = t => StdTex(TEX.planks("#8a5a34", "#7e5030", W_V(3, 1.5), true)),
                build = (ic, t) =>
                {
                    var sc = ic.scene; int n = Math.Min(3, t.l); double W = 3.6, x0 = -(n - 1) * W / 2; int[] hc = { 0x8a5530, 0x3a2a22, 0xd9c3a0 };
                    for (int k = 0; k <= n; k++) StallDivider(sc, x0 - W / 2 + k * W, -4.8, -.8);
                    for (int k = 0; k < n; k++)
                    {
                        double x = x0 + k * W; var h = Horse(hc[k]); W_Scale(h, 1.35); SetPos(h, x, 0, -2.6); W_Add(sc, h); Clickable(h, "pet"); ic.fx.Add(IdleFx(h, k));
                        P(sc, "box", 0x5e3d22, W_V(W - .3, .9, .1), W_V(x, .45, -.8)); P(sc, "box", 0xe0b84a, W_V(.5, .2, .04), W_V(x, .75, -.73));
                    }
                    for (int k = 0; k < 3; k++) P(sc, "tor", 0x8a8a8a, W_V(.25, .25, .25), W_V(-5.5 + k * .7, 3, -4.9)); P(sc, "box", 0x6e4a2a, W_V(1.2, .9, .6), W_V(5.4, .45, -3.8)); P(sc, "box", 0x5a3418, W_V(1, .6, .5), W_V(5.4, 1.1, -3.8));
                },
                dyn = (ic, t) =>
                {
                    if (t.job != null && Prog(t.job) >= 1)
                    {
                        var f = Horse(0xb07a4a, 0x5a3a22); W_Scale(f, .85); SetPos(f, 0, 0, 2.4); W_Add(ic.dyn, f); Clickable(f, "collect");
                        var fx = IdleFx(f, 2); ic.dynFx.Add(fx); ic.fx.Add(fx);
                    }
                }
            },
            ["kulube"] = new Int_Def
            {
                floor = t => StdTex(TEX.planks("#a8733f", "#9b6837", W_V(3, 3))),
                wall = t => t.l == 1 ? StdTex(TEX.planks("#8b5a32", "#7e5030", W_V(3, 1.5))) : ToonMat(new MatOpts { color = new[] { 0, 0, 0xf0e4c8, 0xf3ead6, 0xefe2c6 }[t.l] }),
                build = (ic, t) =>
                {
                    var sc = ic.scene; int l = t.l;
                    var rug = F(sc, StdTex(TEX.kilim(GetOr(KILIM, GetOr(S.decor.house, "kilim")))), W_V(4, .03, 6), W_V(0, .02, .6), W_V(0, Math.PI / 2, 0)); W_Shadow(rug, false, true);
                    var tb = G(sc, W_V(0, 0, .6)); P(tb, "box", 0x8a5a32, W_V(2.6, .12, 1.4), W_V(0, 1, 0)); foreach (var xz in new[] { new[] { 1.1, .55 }, new[] { -1.1, .55 }, new[] { 1.1, -.55 }, new[] { -1.1, -.55 } }) P(tb, "box", 0x6e4424, W_V(.12, 1, .12), W_V(xz[0], .5, xz[1]));
                    P(tb, "box", 0xf6efe0, W_V(2.7, .02, .6), W_V(0, 1.07, 0)); P(tb, "sph", 0xc98a3a, W_V(.35, .16, .2), W_V(-.5, 1.15, 0)); P(tb, "cyl", 0xc4492e, W_V(.16, .28, .16), W_V(.4, 1.22, 0)); P(tb, "cyl", 0xc4492e, W_V(.11, .18, .11), W_V(.4, 1.45, 0));
                    for (int k = 0; k < 3; k++) P(tb, "cyl", 0xb5541f, W_V(.05, .12, .05), W_V(.85, 1.13, -.3 + k * .3));
                    foreach (var xr in new[] { new[] { -1.7, Math.PI / 2 }, new[] { 1.7, -Math.PI / 2 } })
                    {
                        var c = G(sc, W_V(xr[0], 0, .6), xr[1]); P(c, "box", 0x7a4a28, W_V(.7, .08, .7), W_V(0, .6, 0));
                        foreach (var ab in new[] { new[] { .28, .28 }, new[] { -.28, .28 }, new[] { .28, -.28 }, new[] { -.28, -.28 } }) P(c, "box", 0x6e4424, W_V(.07, .6, .07), W_V(ab[0], .3, ab[1])); P(c, "box", 0x7a4a28, W_V(.7, .8, .08), W_V(0, 1, -.32));
                    }
                    var fp = G(sc, W_V(-4, 0, -4.6)); var st = StdTex(TEX.stone(W_V(1, 1))); P(fp, "box", st, W_V(2.6, 2.2, .8), W_V(0, 1.1, 0)); P(fp, "box", 0x1e1410, W_V(1.4, 1, .1), W_V(0, .6, .42)); P(fp, "box", st, W_V(1.8, .12, .6), W_V(0, .06, .65)); P(fp, "box", 0x6e4424, W_V(3, .18, 1), W_V(0, 2.3, .1)); P(fp, "box", st, W_V(1.2, 3, .6), W_V(0, 3.9, -.1));
                    for (int k = 0; k < 3; k++) P(fp, "cyl", 0x5a3a22, W_V(.08, .9, .08), W_V(0, .2, .6 + k * .05), W_V(0, k * .5, Math.PI / 2));
                    var flames = new List<Transform>(); int[] fc = { 0xff8a2a, 0xffc23a, 0xff5a1a, 0xffd86a };
                    for (int k = 0; k < 4; k++) flames.Add(P(fp, "cone", fc[k], W_V(.18 - k * .02, .5, .18 - k * .02), W_V(-.3 + k * .2, .45, .6), null, new MatOpts { emissive = 0xff6a1a, emissiveIntensity = 1.4f }));
                    var fl = Int_Light(fp, LightType.Point, 0xff8a3a, 1.4, 10); fl.transform.localPosition = V(0, 1, 1.4); P(fp, "cyl", 0x2a2a2a, W_V(.3, .25, .3), W_V(1, 2.55, .1));
                    ic.fx.Add((dt, tm) =>
                    {
                        for (int k = 0; k < flames.Count; k++) { var f = flames[k]; float sy = .4f + Mathf.Abs(Mathf.Sin(tm * 8 + k * 1.7f)) * .25f, sx = .14f + Mathf.Sin(tm * 6 + k) * .03f; f.localScale = new Vector3(sx, sy, sx); }
                        if (fl) fl.intensity = 1.2f + (float)Rnd() * .4f;
                    });
                    var sh = G(sc, W_V(6.5, 0, -1.5), -Math.PI / 2); for (int k = 0; k < 4; k++) P(sh, "box", 0x7a4a28, W_V(3.2, .1, .6), W_V(0, .6 + k * .9, 0)); foreach (var x in new[] { -1.6, 1.6 }) P(sh, "box", 0x6e4424, W_V(.1, 3.4, .6), W_V(x, 1.7, 0)); ic.shelf = sh;
                    if (l >= 2)
                    {
                        var bd = G(sc, W_V(4.3, 0, -3.6)); P(bd, "box", 0x6e4424, W_V(2, .5, 2.8), W_V(0, .3, 0)); P(bd, "box", 0xf6efe0, W_V(1.9, .25, 2.7), W_V(0, .65, 0));
                        var bl = F(bd, StdTex(TEX.kilim(null)), W_V(1.95, .06, 1.7), W_V(0, .8, .5)); W_Shadow(bl, true, true);
                        P(bd, "box", 0xffffff, W_V(1.4, .2, .5), W_V(0, .85, -1.05)); P(bd, "box", 0x6e4424, W_V(2, 1.4, .15), W_V(0, .7, -1.45));
                    }
                    if (l >= 3)
                    {
                        var sd = G(sc, W_V(-6.2, 0, 1.5)); P(sd, "box", 0x8a5a32, W_V(1.2, .5, 4.2), W_V(0, .25, 0)); P(sd, "box", 0x2f5a8a, W_V(1.1, .3, 4.1), W_V(0, .62, 0));
                        int[] pc = { 0xc4492e, 0xf2b330, 0x2f6b3a, 0xc4492e }; for (int k = 0; k < 4; k++) P(sd, "box", pc[k], W_V(.3, .7, .85), W_V(-.4, 1, -1.5 + k * 1), W_V(0, 0, -.2));
                        for (int k = 0; k < 2; k++) { var pic = G(sc, W_V(-6.95, 3, -1.5 + k * 3)); P(pic, "box", 0xc9a050, W_V(.08, 1.3, 1.8)); P(pic, "box", k == 0 ? 0x7aa6c8 : 0x9cbf6b, W_V(.1, 1.1, 1.6)); }
                    }
                    if (l >= 4)
                    {
                        var ch = G(sc, W_V(0, 4.6, .6)); P(ch, "cyl", 0xe0b84a, W_V(.03, .8, .03), W_V(0, .4, 0)); P(ch, "tor", 0xe0b84a, W_V(.7, .7, .7), W_V(0, 0, 0), W_V(Math.PI / 2, 0, 0));
                        for (int k = 0; k < 6; k++) { double a = k / 6.0 * 6.28; P(ch, "cone", 0xfff1c8, W_V(.06, .22, .06), W_V(Math.Cos(a) * .7, .12, Math.Sin(a) * .7), null, new MatOpts { emissive = 0xffd27a, emissiveIntensity = 1.5f }); }
                        Int_Light(ch, LightType.Point, 0xffe0a0, .9, 14); var gc = G(sc, W_V(-6.5, 0, -4.2)); P(gc, "box", 0x5a3418, W_V(.8, 3.2, .6), W_V(0, 1.6, 0)); P(gc, "cyl", 0xf6efe0, W_V(.28, .05, .28), W_V(0, 2.6, .31), W_V(Math.PI / 2, 0, 0));
                        P(sc, "cyl", 0xc4492e, W_V(.35, .6, .35), W_V(2.6, .3, 3.6)); P(sc, "ico1", 0x3f7f34, W_V(.6, .8, .6), W_V(2.6, 1.2, 3.6));
                    }
                    HouseDecor(sc); var lan = Int_Light(sc, LightType.Point, 0xffd090, .6, 16); lan.transform.localPosition = V(1, 4, 2);
                },
                dyn = (ic, t) =>
                {
                    var sh = ic.shelf; if (sh == null) return; var g = G(ic.dyn); g.localPosition = sh.localPosition; g.localRotation = sh.localRotation;
                    int n = RoundI(Math.Min(1, (double)Stored() / Cap()) * 24);
                    int[] cols = { 0xc4492e, 0xf2b330, 0x7aa64a, 0xe8d8b0, 0x8a5a32, 0xd9734a };
                    for (int k = 0; k < n; k++)
                    {
                        int r = k / 6, c = k % 6;
                        if (k % 3 == 0) P(g, "cyl", cols[k % 6], W_V(.18, .4, .18), W_V(-1.3 + c * .52, .85 + r * .9, 0));
                        else { P(g, "cyl", cols[(k + 2) % 6], W_V(.14, .3, .14), W_V(-1.3 + c * .52, .8 + r * .9, 0)); P(g, "cyl", 0xf6efe0, W_V(.15, .05, .15), W_V(-1.3 + c * .52, .97 + r * .9, 0)); }
                    }
                }
            },
        };
        public static Unit IntT(string key) => key == "kulube" ? new Unit { l = S.house } : GetOr(S.units, key);

        // İç mekân bırakılırken o sahneye özel GPU kaynakları (doku, malzeme, ağ) serbest bırakılır.
        // Paylaşılan (önbellekteki: GE, MC, OUT ve dünya sahnesinde kullanılan) kaynaklara dokunulmaz.
        public static void DisposeScene(Transform sc)
        {
            if (sc == null) return;
            var sharedM = new HashSet<Material>(MC.Values); if (OUT != null) sharedM.Add(OUT);
            var sharedG = new HashSet<Mesh>(GE.Values); foreach (var gm in Geo.AllCached) if (gm != null) sharedG.Add(gm);
            var sharedT = new HashSet<Texture>();
            // dünya, masa sahnesi, küçük resim sahnesi vb. — iç mekân dışındaki TÜM çizicilerin kaynakları korunur
            var all = Resources.FindObjectsOfTypeAll<Renderer>();
            if (all != null) foreach (var r in all)
                {
                    if (r == null || r.transform.IsChildOf(sc)) continue;
                    foreach (var m in r.sharedMaterials) if (m != null) { sharedM.Add(m); if (m.mainTexture != null) sharedT.Add(m.mainTexture); }
                    var mf = r.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != null) sharedG.Add(mf.sharedMesh);
                }
            foreach (var m in sharedM) if (m != null && m.mainTexture != null) sharedT.Add(m.mainTexture);
            var doneM = new HashSet<Material>(); var doneG = new HashSet<Mesh>();
            foreach (var r in sc.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue;   // yazı tipi malzemesi/atlası paylaşılır (Text3D)
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && !sharedG.Contains(mf.sharedMesh) && doneG.Add(mf.sharedMesh)) UnityEngine.Object.Destroy(mf.sharedMesh);
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || sharedM.Contains(m) || !doneM.Add(m)) continue;
                    var tx = m.mainTexture; if (tx != null && !sharedT.Contains(tx)) UnityEngine.Object.Destroy(tx);
                    UnityEngine.Object.Destroy(m);
                }
            }
            UnityEngine.Object.Destroy(sc.gameObject);
        }
        public static void DropInterior() { var o = Interior; Interior = null; if (o != null && o.scene != null) { o.scene.gameObject.SetActive(false); SetTimeout(() => DisposeScene(o.scene), 0); } }
        public static void BuildInterior(string key)
        {
            if (Interior != null && Interior.scene != null) DisposeScene(Interior.scene);
            var t = IntT(key); var def = INT[key];
            var root = new GameObject("interior_" + key); UnityEngine.Object.DontDestroyOnLoad(root); var sc = root.transform;
            // sc.background = 0x2b1d12 (Icam düz renk), sc.fog = Fog(0x2b1d12,24,42) → kökün ortamı (EngEnv; kök etkinken geçerli)
            Eng_SceneFog(sc, 0x2b1d12, 24, 42);
            var ic = new Int_Interior { key = key, scene = sc }; ic.dyn = G(sc);
            ic.fx.Add(Room(sc, def.floor(t), def.wall(t))); def.build(ic, t);
            foreach (var l in sc.GetComponentsInChildren<Light>(true)) l.cullingMask = 1 << W_LAYER_INT;
            ic.refresh = () =>
            {
                W_Remove(ic.dyn); ic.dyn = G(sc); ic.fx.RemoveAll(f => ic.dynFx.Contains(f)); ic.dynFx.Clear();
                def.dyn(ic, IntT(key)); W_SetLayer(sc, W_LAYER_INT); ic.sig = IntSig(key);
                foreach (var l in ic.dyn.GetComponentsInChildren<Light>(true)) l.cullingMask = 1 << W_LAYER_INT;
            };
            ic.refresh(); Interior = ic;
        }
        public static string IntSig(string key)
        {
            var t = IntT(key); if (t == null) return "";
            return (t.job != null ? (Prog(t.job) >= 1 ? "r" : "g") : "i") + t.l + (key == "kulube" ? RoundI((double)Stored() / Cap() * 24).ToString() : "");
        }

        // ---- iris geçişi (#iris) ----
        static RectTransform _irisRoot; static RectTransform _irisHole; static Image[] _irisSides;
        static float _irisK = 1, _irisFrom = 1, _irisTo = 1, _irisT = 1, _irisDur = .28f;
        static void Int_IrisBuild()
        {
            if (_irisRoot != null) return;
            _irisRoot = UI.New("iris", UI.Overlay); UI.Stretch(_irisRoot);
            var col = UI.Hex("#1d140b");
            const int N = 128; var px = new Color32[N * N];
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(N / 2f, N / 2f));
                    var c = col; c.a = Mathf.Clamp01(d - N / 2f + .5f); px[y * N + x] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            var hole = UI.Fill(_irisRoot, Color.white, 0, "hole"); hole.sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100); hole.raycastTarget = false;
            _irisHole = hole.rectTransform; _irisHole.anchorMin = _irisHole.anchorMax = _irisHole.pivot = new Vector2(.5f, .5f);
            _irisSides = new Image[4];
            for (int i = 0; i < 4; i++) { var s = UI.Fill(_irisRoot, col, 0, "side"); s.raycastTarget = false; _irisSides[i] = s; }
            OnFrame += dt =>
            {
                if (_irisT < 1) { _irisT = Mathf.Min(1, _irisT + Time.unscaledDeltaTime / _irisDur); _irisK = Mathf.Lerp(_irisFrom, _irisTo, W_Bezier(.65f, 0, .35f, 1, _irisT)); }
                Int_IrisLayout();
            };
        }
        static void Int_IrisLayout()
        {
            bool open = _irisK >= .999f && _irisTo >= 1; UI.Show(_irisRoot, !open); if (open) return;
            float W = UI.W, H = UI.H, D = 3 * Mathf.Max(W, H) * _irisK, r = D / 2;
            _irisHole.sizeDelta = new Vector2(D, D); _irisHole.anchoredPosition = Vector2.zero;
            // üst, alt, sol, sağ şeritler (deliğin dışını kapatır)
            float hw = W / 2, hh = H / 2;
            void side(Image im, float x0, float y0, float x1, float y1)
            {
                var rt = im.rectTransform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(Mathf.Max(0, x1 - x0), Mathf.Max(0, y1 - y0)); rt.anchoredPosition = new Vector2((x0 + x1) / 2, (y0 + y1) / 2);
            }
            side(_irisSides[0], -hw - 2, r, hw + 2, hh + 2); side(_irisSides[1], -hw - 2, -hh - 2, hw + 2, -r);
            side(_irisSides[2], -hw - 2, -r, -r, r); side(_irisSides[3], r, -r, hw + 2, r);
        }
        static void Int_Iris(bool close)
        {
            Int_IrisBuild(); _irisRoot.SetAsLastSibling();
            _irisDur = Int_BodyHas("fastnav") ? .15f : .28f; _irisFrom = _irisK; _irisTo = close ? 0 : 1; _irisT = 0; Int_IrisLayout();
        }
        static IEnumerator Int_Frames(int n, Action a) { for (int i = 0; i < n; i++) yield return null; a(); }
        public static void Fade(Action fn)
        {
            Int_BodyToggle("fastnav", S != null && S.fastNav); Int_Iris(true);
            SetTimeout(() => { fn(); Host.Run(Int_Frames(2, () => Int_Iris(false))); }, S != null && S.fastNav ? 160 : 290);
        }
        // Yazı boyutu ve sol el modu: gövdeye sınıf olarak uygulanır
        public static void ApplyUI()
        {
            var u = (S != null ? S.ui : null) ?? new UiPrefs();
            Int_BodyToggle("fs-b", u.fs == "b"); Int_BodyToggle("fs-xb", u.fs == "xb"); Int_BodyToggle("lefty", u.left);
            SetTimeout(() => { try { Resize(); } catch (Exception) { } }, 0);
        }
        public static void SetModeUI()
        {
            ApplyUI(); Int_BodyMode = Mode; if (S != null) WxUI(); Int_BodyToggle("hub", Mode == "hub"); Int_BodyToggle("lowgfx", GFX.low);
            if (Mode == "hub") HubLimits(); else if (Mode == "focus") FocusLimits(); RenderStrip(true);
            if (Mode != "hub" && Int_BodyHas("photo")) PhotoMode(false);
            Int_BodyToggle("focus", Mode == "focus"); Int_BodyToggle("inside", Mode == "interior");
            Int_BodyToggle("desk", Mode == "desk"); Int_BackHidden = Mode == "desk"; Int_BackText = Mode == "interior" ? "← Dışarı" : Mode == "focus" ? "← Harita" : "← Masa";
            Int_OnBody?.Invoke();
        }
        public static void EnterInterior(string key)
        {
            if (!INT.ContainsKey(key) || (key != "kulube" && GetOr(S.units, key) == null)) return;
            Fade(() =>
            {
                BuildInterior(key); Mode = "interior"; var lk = key == "kulube" ? "ev" : LocOf(key); if (Focus == null || !LOC.ContainsKey(Focus)) Focus = lk;
                if (View == null || View.k != "loc") View = new View { k = "loc", loc = lk };
                Ctl.enabled = false; Ictl.enabled = true;
                Ictl.position = new Vector3(0, 6.4f, 14.5f); Ictl.target = new Vector3(0, 1.4f, -.6f); Ictl.update(); if (View != null) View.mini = false;
                SetModeUI(); RenderSheet(); SyncMarks(); Stat("enter_" + key);
            });
        }
        public static void LeaveInterior()
        {
            if (Mode != "interior") return;
            Fade(() => { Mode = "focus"; DropInterior(); Ctl.enabled = true; Ictl.enabled = false; SetModeUI(); RenderSheet(); SyncMarks(); });
        }
    }
}
