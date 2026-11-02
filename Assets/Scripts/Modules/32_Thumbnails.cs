// ================= KÜÇÜK RESİMLER (32-thumbnails.js) =================
// Her bina/ev/konum modeli çimen diski üzerinde 220×220 saydam RenderTexture'a çizilir → THUMB[anahtar] (Texture2D).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciftlik
{
    public static partial class Game
    {
        public static void MakeThumbs()
        {
            RenderTexture rt;
            try { rt = new RenderTexture(220, 220, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4; rt.Create(); } catch (Exception) { return; }
            int L = W_LAYER_THUMB; var basePos = new Vector3(0, -3000, 0);
            var scRoot = new GameObject("thumbs").transform; scRoot.position = basePos;
            // HemisphereLight(0xf3ead2,0x7d6a3a,.9) + DirectionalLight(0xffe6c0,1.3) (-4,8,6)
            Eng_HemiLight(scRoot, 0xf3ead2, 0x7d6a3a, .9f);
            var dgo = new GameObject("thumbLight"); dgo.transform.SetParent(scRoot, false); var dl = dgo.AddComponent<Light>(); dl.type = LightType.Directional; dl.color = Hex3(0xffe6c0); dl.intensity = 1.3f; dl.cullingMask = 1 << L; dl.shadows = LightShadows.None;
            dgo.transform.rotation = Quaternion.LookRotation(-V(-4, 8, 6));
            var cgo = new GameObject("thumbCam"); var c = cgo.AddComponent<Camera>(); c.enabled = false; c.fieldOfView = 30; c.nearClipPlane = .1f; c.farClipPlane = 200;
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0, 0, 0, 0); c.cullingMask = 1 << L; c.targetTexture = rt;
            // global ortam/sis geçici olarak küçük resim ışığına ayarlanır
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            void doOne(string key, Action<Transform> fn, double @base = 2.6)
            {
                var g = G(scRoot); P(g, "cyl", 0x86bc4f, W_V(@base, .3, @base), W_V(0, -.15, 0)); var inner = G(g); fn(inner);
                W_SetLayer(g, L);
                var rs = inner.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) { W_Remove(g); return; }
                var box = rs[0].bounds; foreach (var r in rs) box.Encapsulate(r.bounds);
                var sz = box.size; var ct = ToThree(box.center - basePos);
                double d = Math.Max(sz.x, Math.Max(sz.y * 1.2, sz.z)) * 2.55 + 1.2;
                c.transform.position = basePos + V(ct.x + d * .62, ct.y + d * .5, ct.z + d * .75);
                c.transform.LookAt(basePos + V(ct.x, ct.y * .85, ct.z), Vector3.up);
                c.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(220, 220, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 220, 220), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                if (THUMB.TryGetValue(key, out var old) && old != null) UnityEngine.Object.Destroy(old);
                THUMB[key] = tex; W_Remove(g);
            }
            try
            {
                foreach (var k in MODELS.Keys.ToList())
                    doOne(k, g => MODELS[k](g, new Unit { l = 1, job = k == "sera" ? new Job { r = "domates", s = 0, e = 1 } : k == "bahce" ? new Job { r = "elma", s = 0, e = 1 } : null }));
                for (int l = 1; l <= 4; l++) { int ll = l; doOne("kulube" + l, g => W_Add(g, House(ll))); }
                doOne("tarla", g => FieldBed(g, new Field { job = new Job { r = "bugday", s = 0, e = 1 } })); doOne("pazar", g => MarketModel(g), 5); doOne("han", g => HanModel(g), 4);
                doOne("panayir", g => FairModel(g), 5);
                doOne("hipo", g => { var h = Horse(0x8a5530, 0x2e1d12, 0xc4492e); W_Add(g, h); }, 1.6);
            }
            finally
            {
                scRoot.gameObject.SetActive(false);   // EngEnv yığından çıkar → önceki ortam geri gelir
                RenderSettings.fog = fog;
                c.targetTexture = null; UnityEngine.Object.Destroy(cgo); UnityEngine.Object.Destroy(scRoot.gameObject); rt.Release(); UnityEngine.Object.Destroy(rt);
            }
        }
    }
}
