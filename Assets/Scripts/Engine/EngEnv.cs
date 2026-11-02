// Sahne ortamı: THREE.Scene.fog / scene.background / HemisphereLight karşılığı.
// Unity'de tek bir küresel ortam (RenderSettings + gölgelendirici globalleri) vardır; bu bileşen bir sahne kökünde (Game.SCENE, iç mekân
// kökü, masa sahnesi…) durur ve kök etkinleştiğinde kendi ayarlarını uygular. En son etkinleşen kök geçerlidir; devre dışı kalınca
// bir öncekine döner. (Gizli/etkin olmayan sahnelerin ışığı/sisi böylece karışmaz.)
using System.Collections.Generic;
using UnityEngine;

namespace Ciftlik
{
    public class EngEnv : MonoBehaviour
    {
        public bool hasHemi; public Color hemiSky, hemiGround; public float hemiI;
        public bool hasFog; public Color fogColor; public float fogNear, fogFar;
        public bool hasBg; public Color bgTop, bgLow;

        static readonly List<EngEnv> _stack = new List<EngEnv>();

        void OnEnable() { _stack.Remove(this); _stack.Add(this); ApplyAll(); }
        void OnDisable() { _stack.Remove(this); ApplyAll(); }
        void OnDestroy() { _stack.Remove(this); }

        public bool IsTop(System.Func<EngEnv, bool> has)
        {
            for (int i = _stack.Count - 1; i >= 0; i--) if (_stack[i] != null && has(_stack[i])) return _stack[i] == this;
            return false;
        }

        public static void ApplyAll()
        {
            EngEnv h = null, f = null, b = null;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var e = _stack[i]; if (e == null) continue;
                if (h == null && e.hasHemi) h = e;
                if (f == null && e.hasFog) f = e;
                if (b == null && e.hasBg) b = e;
            }
            if (h != null) Game.Eng_ApplyHemi(h.hemiSky, h.hemiGround, h.hemiI); else Game.Eng_ApplyHemi(Color.black, Color.black, 0);
            if (f != null) Game.Eng_ApplyFog(true, f.fogColor, f.fogNear, f.fogFar); else Game.Eng_ApplyFog(false, Color.black, 0, 0);
            if (b != null) Game.Eng_ApplySky(b.bgTop, b.bgLow);
        }
    }
}
