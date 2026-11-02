// Unity tarafındaki tek kalıcı nesne: sahne ne olursa olsun oyunu koddan kurar,
// zamanlayıcıları (setTimeout/setInterval) ve kare döngüsünü (requestAnimationFrame) sağlar.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ciftlik
{
    public class GameHost : MonoBehaviour
    {
        class Timer { public int id; public Action a; public double at, every; public bool repeat, dead; }
        readonly List<Timer> _timers = new List<Timer>();
        int _nextId = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Game.Host != null) return;
            var go = new GameObject("GameHost");
            DontDestroyOnLoad(go);
            Game.Host = go.AddComponent<GameHost>();
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Input.multiTouchEnabled = true;
            UI.Init();
            Game.BootModules();
        }

        public int AddTimer(Action a, double ms, bool repeat)
        {
            var t = new Timer { id = _nextId++, a = a, every = Math.Max(0, ms), repeat = repeat, at = Game.PerfNow() + Math.Max(0, ms) };
            _timers.Add(t);
            return t.id;
        }
        public void RemoveTimer(int id) { foreach (var t in _timers) if (t.id == id) t.dead = true; }

        void Update()
        {
            var n = Game.PerfNow();
            for (int i = 0; i < _timers.Count; i++)
            {
                var t = _timers[i];
                if (t.dead || n < t.at) continue;
                if (t.repeat) t.at = n + Math.Max(1, t.every); else t.dead = true;
                try { t.a(); } catch (Exception e) { Debug.LogException(e); Game.ReportError(e); }
            }
            _timers.RemoveAll(t => t.dead);
            try { Game.RaiseFrame(Time.deltaTime); } catch (Exception e) { Debug.LogException(e); Game.ReportError(e); }
        }

        void OnApplicationPause(bool paused) { Game.SetHidden(paused); }
        void OnApplicationFocus(bool focus) { if (Application.isMobilePlatform) Game.SetHidden(!focus); }
        void OnApplicationQuit() { Game.SetHidden(true); }

        public Coroutine Run(IEnumerator co) => StartCoroutine(co);
    }
}
