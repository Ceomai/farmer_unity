// ================= AÇILIŞ / GRAFİK KALİTESİ (00-boot.js) =================
// JS'teki "THREE yüklenemedi" denetimi Unity'de gereksiz (motor her zaman var).
// 'auto' : cihaz gücüne göre başlar, kare hızına göre çözünürlüğü kendisi ayarlar
// 'high' : en yüksek kalite ; 'low' : pil tasarrufu (gölge yok, 30 FPS, düşük çözünürlük)
using System;
using UnityEngine;

namespace Ciftlik
{
    public class GfxDef
    {
        public string mode;
        public bool low, shadows;
        public double maxPR, pr;
        public int fpsCap, shadowSize, winEvery;
        public double rtScale;
    }

    public static partial class Game
    {
        static GfxDef _gfx;
        public static GfxDef GFX => _gfx ?? (_gfx = Boot_MakeGfx());

        static GfxDef Boot_MakeGfx()
        {
            string m = "auto";
            try { m = LsGet("ci-gfx") ?? "auto"; if (m == "") m = "auto"; } catch (Exception) { }
            // navigator.deviceMemory (GB) ≤ 3 ya da hardwareConcurrency ≤ 4 → zayıf cihaz
            int mem = SystemInfo.systemMemorySize, cpu = SystemInfo.processorCount;
            bool weak = (mem > 0 && mem <= 3 * 1024) || (cpu > 0 && cpu <= 4);
            // devicePixelRatio karşılığı: mobilde dpi/160, masaüstünde 1
            double dpr = Application.isMobilePlatform && Screen.dpi > 0 ? Screen.dpi / 160.0 : 1;
            if (dpr <= 0) dpr = 1;
            bool low = m == "low" || (m == "auto" && weak);
            var g = new GfxDef
            {
                mode = m, low = low, shadows = !low,
                maxPR = m == "high" ? Math.Min(dpr, 3) : low ? Math.Min(dpr, 1.25) : Math.Min(dpr, 2),
                pr = 0, fpsCap = low ? 30 : 0, shadowSize = m == "high" ? 4096 : low ? 1024 : 2048, winEvery = low ? 6 : 3,
                rtScale = low ? .45 : m == "high" ? .75 : .6
            };
            g.pr = g.maxPR;
            return g;
        }

        static void Init_00() { var g = GFX; }

        // JS: ayarı kaydedip sayfayı yeniden yükler. Unity: kaydı yaz, ayarı yeniden oku ve motora yeniden uygula.
        public static void SetGfx(string m)
        {
            try { LsSet("ci-gfx", m); } catch (Exception) { }
            if (S != null) Save();
            _gfx = Boot_MakeGfx();
            _fpsN = 0; _fpsT = 0; _lowN = 0; _hiN = 0; _lastDrop = 0;
            ApplyGfx();                 // 06-engine: gölge, gölge haritası boyutu, çözünürlük ölçeği, kare sınırı yeniden uygulanır
            SetPixelRatio(GFX.pr);
            Resize();
        }

        static int _fpsN, _lowN, _hiN;
        static double _fpsT, _lastDrop;
        public static double LastFrame = 0;   // JS _lastFrame (33-loop kullanır)

        public static void GfxAdapt(double ms)
        {
            if (GFX.mode != "auto") return; _fpsN++; _fpsT += ms; if (_fpsT < 3000) return; double fps = _fpsN * 1000 / _fpsT; _fpsN = 0; _fpsT = 0;
            // Çözünürlük değişimi kısa bir takılmaya yol açar: art arda iki ölçüm gerekir, düşürdükten sonra 30 sn yükseltmez (gidip gelme olmasın)
            double pr = GFX.pr; _lowN = fps < 44 ? _lowN + 1 : 0; _hiN = fps > 58 ? _hiN + 1 : 0; double tn = PerfNow();
            if (_lowN >= 2 && pr > 1) { pr = Math.Max(1, pr - .25); _lowN = 0; _lastDrop = tn; }
            else if (_hiN >= 3 && pr < GFX.maxPR && tn - _lastDrop > 30000) { pr = Math.Min(GFX.maxPR, pr + .25); _hiN = 0; }
            if (pr != GFX.pr) { GFX.pr = pr; SetPixelRatio(pr); Resize(); }
        }
    }
}
