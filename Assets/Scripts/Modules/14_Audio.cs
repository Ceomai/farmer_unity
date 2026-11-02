// ================= SES VE MÜZİK (14-audio.js) =================
// WebAudio sentezinin karşılığı: osilatör + üstel zarf (gain) + üstel frekans kayması aynı formüllerle,
// ses iş parçacığında (OnAudioFilterRead) örnek örnek üretilir. "currentTime" = üretilen örnek sayısı / örnekleme hızı,
// böylece müzik zamanlayıcısı (90 ms'de bir ileriye not planlar) web sürümüyle aynı biçimde çalışır.
// Zincir: ses → sfx (1) ya da mus (.13 / 0) → master (.55) → çıkış.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ciftlik
{
    // Bir kazanç düğümü (GainNode) karşılığı
    public class SndBus { public volatile float gain = 1; }

    public class SndState
    {
        public SndSynth ctx;            // AudioContext karşılığı (null → henüz başlatılmadı)
        public SndBus master, sfx, mus;
    }

    public class MusState { public double next; public int step; public int timer; public List<int> mel = new List<int>(); }

    // Ses iş parçacığında çalan basit sentezleyici (AudioContext + OscillatorNode + GainNode)
    public class SndSynth : MonoBehaviour
    {
        class Voice { public double t0, f0, f1, d, v; public int type; public SndBus bus; public double phase; }
        readonly List<Voice> _voices = new List<Voice>();
        readonly List<Voice> _pending = new List<Voice>();
        readonly object _lock = new object();
        int _sr = 44100;
        long _samples;                      // üretilen örnek sayısı (ses saati)
        public SndBus master;
        public string state = "running";    // AudioContext.state

        public int sampleRate => _sr;
        public double currentTime { get { lock (_lock) return (double)_samples / _sr; } }

        public void Begin()
        {
            _sr = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            var src = gameObject.AddComponent<AudioSource>();
            // OnAudioFilterRead'in çağrılması için kaynak çalıyor olmalı: sessiz, döngülü bir klip çalınır, çıkışın tamamı sentezden gelir.
            src.clip = AudioClip.Create("synth", _sr, 1, _sr, false);
            src.loop = true; src.playOnAwake = false; src.spatialBlend = 0; src.volume = 1; src.priority = 0;
            src.Play();
        }

        // o.type: sine 0, triangle 1, square 2, sawtooth 3
        public void Add(double t0, double f, double d, int type, double v, double slide, SndBus bus)
        {
            var vo = new Voice { t0 = t0, f0 = f, f1 = slide != 0 ? Math.Max(30, f * slide) : 0, d = d, v = v, type = type, bus = bus };
            lock (_lock) _pending.Add(vo);
        }

        static double Wave(int type, double p)
        {
            switch (type)
            {
                case 2: return p < .5 ? 1 : -1;
                case 3: return 2 * (p - Math.Floor(p + .5));
                case 1: { double q = p - .25; q -= Math.Floor(q); return 4 * Math.Abs(q - .5) - 1; }
                default: return Math.Sin(p * Math.PI * 2);
            }
        }

        // WebAudio: gain .0001 → v (üstel, .012 sn) → .0001 (üstel, t+d'ye kadar); durdurma t+d+.05
        static double Env(Voice vo, double tau)
        {
            if (tau < .012) return .0001 * Math.Pow(vo.v / .0001, tau / .012);
            if (tau < vo.d) return vo.v * Math.Pow(.0001 / vo.v, (tau - .012) / Math.Max(1e-6, vo.d - .012));
            return .0001;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            long s0; List<Voice> act;
            lock (_lock) { if (_pending.Count > 0) { _voices.AddRange(_pending); _pending.Clear(); } s0 = _samples; _samples += data.Length / Math.Max(1, channels); act = _voices; }
            int n = data.Length / Math.Max(1, channels);
            float mg = master != null ? master.gain : 1;
            for (int i = 0; i < n; i++)
            {
                double now = (double)(s0 + i) / _sr, acc = 0;
                for (int k = 0; k < act.Count; k++)
                {
                    var vo = act[k];
                    double tau = now - vo.t0;
                    if (tau < 0 || tau >= vo.d + .05) continue;
                    double f = vo.f1 > 0 ? vo.f0 * Math.Pow(vo.f1 / vo.f0, Math.Min(1, tau / vo.d)) : vo.f0;
                    acc += Wave(vo.type, vo.phase) * Env(vo, tau) * vo.bus.gain;
                    vo.phase += f / _sr; if (vo.phase >= 1) vo.phase -= Math.Floor(vo.phase);
                }
                float smp = (float)Math.Max(-1, Math.Min(1, acc * mg));
                for (int c = 0; c < channels; c++) data[i * channels + c] = smp;
            }
            double end = (double)(s0 + n) / _sr;
            lock (_lock) _voices.RemoveAll(vo => end - vo.t0 >= vo.d + .05);
        }
    }

    public static partial class Game
    {
        public static SndState SND = new SndState();
        public static bool Booted = false;

        public static void AudioInit()
        {
            if (SND.ctx != null) return;
            try
            {
                var go = new GameObject("AudioContext");
                UnityEngine.Object.DontDestroyOnLoad(go);
                if (UnityEngine.Object.FindObjectOfType<AudioListener>() == null) go.AddComponent<AudioListener>();
                var c = go.AddComponent<SndSynth>();
                SND.ctx = c;
                SND.master = new SndBus { gain = .55f }; c.master = SND.master;
                SND.sfx = new SndBus();
                SND.mus = new SndBus { gain = S.sound.music ? .13f : 0 };
                c.Begin();
                StartMusic();
            }
            catch (Exception e) { Debug.LogWarning(e); }
        }

        static int Snd_Type(string type) => type == "triangle" ? 1 : type == "square" ? 2 : type == "sawtooth" ? 3 : 0;

        public static void Tone(double f, double d, string type = null, double v = 0, double when = 0, double slide = 0, SndBus dest = null)
        {
            var c = SND.ctx; if (c == null) return;
            double t = c.currentTime + when;
            c.Add(t, f, d, Snd_Type(type ?? "sine"), v != 0 ? v : .2, slide, dest ?? SND.sfx);
        }

        // Web sürümünde bu işlev ilk satırında "return" ile kapatılmıştır (gürültü sesi kullanılmıyor); aynen korunur.
        static void Noise(double d, double v = 0, double when = 0, double freq = 0, double q = 0) { return; }

        public static readonly Dictionary<string, Action> SFX = new Dictionary<string, Action>
        {
            { "pop", () => Tone(480, .14, "triangle", .25, 0, 2.2) },
            { "coin", () => { Tone(1046, .09, "square", .08); Tone(1568, .22, "square", .08, .08); } },
            { "click", () => Tone(620, .05, "triangle", .1) },
            { "plant", () => Tone(420, .12, "triangle", .1, 0, 1.5) },
            { "cluck", () => { Tone(760, .07, "square", .05, 0, .55); Tone(900, .09, "square", .05, .1, .5); Tone(700, .07, "square", .04, .22, .6); } },
            { "moo", () => { Tone(220, .7, "triangle", .07, 0, .75); Tone(165, .7, "sine", .06, 0, .8); } },
            { "baa", () => { for (int i = 0; i < 5; i++) Tone(380 + (i % 2) * 30, .09, "sawtooth", .05, i * .08, .9); } },
            { "neigh", () => { for (int i = 0; i < 6; i++) Tone(900 - i * 80, .08, "sawtooth", .04, i * .06, .8); } },
            { "buzz", () => Tone(220, .5, "sawtooth", .03, 0, 1.05) },
            { "build", () => { var fs = new double[] { 392, 523, 659 }; for (int i = 0; i < fs.Length; i++) Tone(fs[i], .25, "triangle", .08, i * .09); } },
            { "level", () => { var fs = new double[] { 523, 659, 784, 1047, 784, 1047 }; for (int i = 0; i < fs.Length; i++) Tone(fs[i], .22, "square", .08, i * .1); } },
            { "whoosh", () => { Tone(660, .5, "triangle", .05, .05); Tone(990, .6, "triangle", .04, .18); } },
            { "thunder", () => { } },
            { "chime", () => { var fs = new double[] { 523, 659, 784, 1047 }; for (int i = 0; i < fs.Length; i++) { Tone(fs[i], .9, "triangle", .06, i * .16); Tone(fs[i] * 2, .6, "sine", .02, i * .16 + .02); } } },
            { "fail", () => Tone(330, .25, "triangle", .06, 0, .7) },
            { "win", () => { var fs = new double[] { 784, 988, 1175, 1568 }; for (int i = 0; i < fs.Length; i++) Tone(fs[i], .18, "triangle", .18, i * .09); } },
        };

        public static readonly Dictionary<string, string> UNIT_SFX = new Dictionary<string, string> { { "kumes", "cluck" }, { "ahir", "moo" }, { "besi", "moo" }, { "agil", "baa" }, { "hara", "neigh" }, { "kovan", "buzz" } };

        public static void Sfx(string k)
        {
            if (SND.ctx == null || S == null || !S.sound.sfx || k == null || !SFX.ContainsKey(k)) return;
            try { SFX[k](); } catch (Exception) { }
        }

        public static MusState MUS = new MusState();

        public static void MakeMelody()
        {
            var sc = new int[] { 0, 2, 4, 7, 9, 12, 14, 16 };
            MUS.mel = new List<int>();
            for (int i = 0; i < 32; i++) { int st = i % 8; MUS.mel.Add(Rnd() < (st % 2 != 0 ? .35 : .75) ? sc[(int)Math.Floor(Rnd() * sc.Length)] : -1); }
        }

        public static void StartMusic()
        {
            if (MUS.timer != 0 || SND.ctx == null) return;
            MakeMelody(); MUS.next = SND.ctx.currentTime + .2;
            MUS.timer = SetInterval(() =>
            {
                var c = SND.ctx; if (c == null || c.state != "running") return;
                double beat = 60.0 / (DayFactor() < .3 ? 88 : 104);
                double cur = c.currentTime;
                while (MUS.next < cur + .35)
                {
                    int s = MUS.step, bar = (int)Math.Floor(s / 8.0) % 4, st = s % 8, root = new int[] { 48, 53, 55, 48 }[bar];
                    double w = Math.Max(0, MUS.next - cur);
                    Func<double, double> mf = n => 440 * Math.Pow(2, (n - 69) / 12.0);
                    if (S.sound.music)
                    {
                        if (st == 0) Tone(mf(root - 12), .32, "triangle", .3, w, 0, SND.mus);
                        if (st == 4) Tone(mf(root - 5), .32, "triangle", .26, w, 0, SND.mus);
                        if (st == 2 || st == 6) foreach (var iv in new int[] { 0, 4, 7 }) Tone(mf(root + 12 + iv), .12, "square", .03, w, 0, SND.mus);
                        int m = MUS.mel[s % 32]; if (m >= 0) Tone(mf(60 + m + (bar == 2 ? 2 : 0)), .2, "triangle", .11, w, 0, SND.mus);
                    }
                    MUS.next += (st % 2 == 0 ? .6 : .4) * beat; MUS.step++; if (MUS.step % 128 == 0) MakeMelody();
                }
            }, 90);
        }

        public static void ApplySound() { if (SND.mus != null) SND.mus.gain = S.sound.music ? .13f : 0; }

        // Son dokunma noktası (CSS px, sol üstten) — uçan simgeler buradan çıkar
        public static Vector2 LastP;

        static void Init_14()
        {
            LastP = new Vector2(UI.W / 2, UI.H / 2);
            // addEventListener('pointerdown', …, {capture:true}) karşılığı: her karede yeni dokunma/tıklama aranır
            OnFrame += dt =>
            {
                bool down = false; Vector2 pos = default(Vector2);
                if (Input.touchCount > 0) { for (int i = 0; i < Input.touchCount; i++) { var tc = Input.GetTouch(i); if (tc.phase == TouchPhase.Began) { down = true; pos = tc.position; } } }
                else if (Input.GetMouseButtonDown(0)) { down = true; pos = Input.mousePosition; }
                if (!down) return;
                LastP = UI.ScreenToCss(pos);
                if (S != null) AudioInit();
                if (SND.ctx != null && SND.ctx.state == "suspended") SND.ctx.state = "running";
            };
            // Arka plana atılınca ses bağlamı askıya alınır (tarayıcı davranışı), dönünce sürer
            OnVisibility += h => { if (SND.ctx != null) SND.ctx.state = h ? "suspended" : "running"; };
        }
    }
}
