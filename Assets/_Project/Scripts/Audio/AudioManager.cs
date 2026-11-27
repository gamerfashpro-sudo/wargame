using System.Collections.Generic;
using UnityEngine;
using WG.Core;

namespace WG.Audio
{
    public enum AudioChannel { Music, Sfx, UI, Ambient }
    public enum UISound { Hover, Click, Confirm, Back, Error, Toggle }

    /// <summary>Громкость по каналам + синтезированные звуки интерфейса (без сторонних файлов).</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        readonly Dictionary<UISound, AudioClip> uiClips = new();
        readonly List<AudioSource> pool = new();
        const int PoolSize = 8;
        int next;
        float lastHover;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < PoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                pool.Add(src);
            }
            uiClips[UISound.Hover] = Synth("ui_hover", 0.035f, 1800f, 2400f, 0.25f, noise: 0.15f);
            uiClips[UISound.Click] = Synth("ui_click", 0.06f, 900f, 500f, 0.6f, noise: 0.35f);
            uiClips[UISound.Confirm] = Chord("ui_confirm", 0.22f, 660f, 990f);
            uiClips[UISound.Back] = Synth("ui_back", 0.09f, 700f, 380f, 0.45f, noise: 0.2f);
            uiClips[UISound.Error] = Synth("ui_error", 0.18f, 220f, 180f, 0.5f, noise: 0.05f, square: true);
            uiClips[UISound.Toggle] = Synth("ui_toggle", 0.05f, 1300f, 1000f, 0.45f, noise: 0.25f);
        }

        public static float ChannelVolume(AudioChannel ch)
        {
            var s = SettingsManager.Current;
            return ch switch
            {
                AudioChannel.Music => s.musicVolume,
                AudioChannel.Sfx => s.sfxVolume,
                AudioChannel.UI => s.uiVolume,
                _ => s.ambientVolume
            };
        }

        public static void PlayUI(UISound sound)
        {
            var am = Instance;
            if (am == null) return;
            if (sound == UISound.Hover)
            {
                if (Time.unscaledTime - am.lastHover < 0.05f) return;
                am.lastHover = Time.unscaledTime;
            }
            am.PlayClip(am.uiClips[sound], ChannelVolume(AudioChannel.UI));
        }

        public void PlayClip(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null) return;
            var src = pool[next];
            next = (next + 1) % pool.Count;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        // ---- синтез ----
        static AudioClip Synth(string name, float dur, float f0, float f1, float amp, float noise = 0f, bool square = false)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            var data = new float[n];
            float phase = 0f;
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * f / rate;
                float s = square ? Mathf.Sign(Mathf.Sin(phase)) * 0.5f : Mathf.Sin(phase);
                s = s * (1f - noise) + ((float)rng.NextDouble() * 2f - 1f) * noise;
                float env = Mathf.Min(1f, i / (rate * 0.002f)) * Mathf.Pow(1f - t, 3f);
                data[i] = s * env * amp;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(string name, float dur, float fa, float fb)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float k = i / (float)n;
                float second = k > 0.25f ? Mathf.Sin(2f * Mathf.PI * fb * t) : 0f;
                float env = Mathf.Min(1f, i / (rate * 0.003f)) * Mathf.Pow(1f - k, 2f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * fa * t) * 0.5f + second * 0.5f) * env * 0.4f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
