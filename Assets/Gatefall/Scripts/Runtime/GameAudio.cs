using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed class GameAudio : MonoBehaviour
    {
        private AudioSource source;
        private readonly Dictionary<GameEventKind, AudioClip> clips = new Dictionary<GameEventKind, AudioClip>();
        private float lastShot, lastHit;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.spatialBlend = 0; source.volume = .3f;
            clips[GameEventKind.Shot] = Tone("Pulse", 380, 140, .07f, .2f);
            clips[GameEventKind.Eliminated] = Tone("Pop", 180, 65, .09f, .3f);
            clips[GameEventKind.BaseHit] = Tone("Defense hit", 120, 45, .25f, .5f);
            clips[GameEventKind.GateCrossed] = Tone("Multiply", 700, 1200, .08f, .12f);
            clips[GameEventKind.WaveCleared] = Tone("Wave clear", 440, 880, .4f, .3f);
            clips[GameEventKind.BossSpawned] = Tone("Titan", 70, 110, .65f, .4f);
        }

        public void Play(GameEvent message)
        {
            if (!clips.TryGetValue(message.Kind, out AudioClip clip)) return;
            if (message.Kind == GameEventKind.Shot)
            { if (Time.unscaledTime - lastShot < .085f) return; lastShot = Time.unscaledTime; }
            if (message.Kind == GameEventKind.Eliminated || message.Kind == GameEventKind.GateCrossed)
            { if (Time.unscaledTime - lastHit < .08f) return; lastHit = Time.unscaledTime; }
            source.PlayOneShot(clip);
        }

        private static AudioClip Tone(string name, float from, float to, float length, float gain)
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * length)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += Mathf.Lerp(from, to, t) * 2 * Mathf.PI / rate;
                samples[i] = Mathf.Sin(phase) * Mathf.Pow(1 - t, 2) * Mathf.Min(1, t * 30) * gain;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }

        private void OnDestroy()
        {
            foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
        }
    }
}
