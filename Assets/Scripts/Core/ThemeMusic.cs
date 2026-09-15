using System.Collections.Generic;
using UnityEngine;

namespace NoBall
{
    public static class ThemeMusic
    {
        static readonly Dictionary<GameBackgroundId, AudioClip> Cache = new Dictionary<GameBackgroundId, AudioClip>();

        public static AudioClip Get(GameBackgroundId id)
        {
            if (Cache.TryGetValue(id, out var clip) && clip != null)
                return clip;
            clip = Build(id);
            Cache[id] = clip;
            return clip;
        }

        static AudioClip Build(GameBackgroundId id)
        {
            const float duration = 16f;
            var data = AudioSynth.Buffer(duration);
            switch (id)
            {
                case GameBackgroundId.Waterfall:
                    Waterfall(data, duration);
                    break;
                case GameBackgroundId.Laboratory:
                    Laboratory(data, duration);
                    break;
                case GameBackgroundId.Aquarium:
                    Aquarium(data, duration);
                    break;
                case GameBackgroundId.Aurora:
                    Aurora(data, duration);
                    break;
                default:
                    Classic(data, duration);
                    break;
            }

            AudioSynth.Normalize(data, 0.22f);
            return AudioSynth.Clip("Theme_" + id, data);
        }

        static void Classic(float[] data, float duration)
        {
            int n = data.Length;
            float bass = AudioSynth.LoopFreq(AudioSynth.Midi(36f), duration);
            float fifth = AudioSynth.LoopFreq(AudioSynth.Midi(43f), duration);
            float[] arp = {
                AudioSynth.LoopFreq(AudioSynth.Midi(60f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(63f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(67f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(70f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(72f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(70f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(67f), duration),
                AudioSynth.LoopFreq(AudioSynth.Midi(63f), duration)
            };

            float step = 0.125f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float s = 0f;
                s += AudioSynth.SoftSquare(bass, t) * 0.22f * (0.7f + 0.3f * AudioSynth.Sin(2f, t));
                s += AudioSynth.Sin(fifth, t) * 0.08f;
                int idx = Mathf.FloorToInt(t / step) % arp.Length;
                float local = t - Mathf.Floor(t / step) * step;
                float env = Mathf.Exp(-local * 14f);
                s += AudioSynth.Sin(arp[idx], t) * env * 0.16f;
                float beat = t % 0.5f;
                if (beat < 0.03f)
                {
                    float e = 1f - beat / 0.03f;
                    s += AudioSynth.Sin(70f, beat) * e * e * 0.18f;
                    s += (AudioSynth.Hash(i) - 0.5f) * e * 0.08f;
                }

                data[i] = s;
            }
        }

        static void Waterfall(float[] data, float duration)
        {
            int n = data.Length;
            float c = AudioSynth.LoopFreq(AudioSynth.Midi(48f), duration);
            float e = AudioSynth.LoopFreq(AudioSynth.Midi(52f), duration);
            float g = AudioSynth.LoopFreq(AudioSynth.Midi(55f), duration);
            float a = AudioSynth.LoopFreq(AudioSynth.Midi(57f), duration);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float p = i / (float)n;
                float water = AudioSynth.LoopNoise(p, 0.4f) * 0.22f
                              + AudioSynth.LoopNoise(p, 2.2f) * 0.1f;
                float pad = AudioSynth.Sin(c, t) * 0.14f
                            + AudioSynth.Sin(e, t) * 0.1f
                            + AudioSynth.Sin(g, t) * 0.08f
                            + AudioSynth.Sin(a, t) * 0.05f;
                pad *= 0.75f + 0.25f * AudioSynth.Sin(0.125f, t);
                data[i] = water + pad;
            }
        }

        static void Laboratory(float[] data, float duration)
        {
            int n = data.Length;
            float drone = AudioSynth.LoopFreq(AudioSynth.Midi(38f), duration);
            float fifth = AudioSynth.LoopFreq(AudioSynth.Midi(45f), duration);
            float hum = AudioSynth.LoopFreq(60f, duration);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float p = i / (float)n;
                float s = AudioSynth.SoftSaw(drone, t) * 0.16f;
                s += AudioSynth.Sin(fifth, t) * 0.07f;
                s += AudioSynth.Sin(hum, t) * 0.03f;
                s += AudioSynth.LoopNoise(p, 1.1f) * 0.04f * (0.5f + 0.5f * AudioSynth.Sin(0.25f, t));
                float pulseT = t % 2f;
                if (pulseT < 0.12f)
                {
                    float env = 1f - pulseT / 0.12f;
                    s += AudioSynth.Sin(48f, pulseT) * env * env * 0.2f;
                }

                data[i] = s;
            }
        }

        static void Aquarium(float[] data, float duration)
        {
            int n = data.Length;
            float f = AudioSynth.LoopFreq(AudioSynth.Midi(53f), duration);
            float a = AudioSynth.LoopFreq(AudioSynth.Midi(57f), duration);
            float c = AudioSynth.LoopFreq(AudioSynth.Midi(60f), duration);
            float e = AudioSynth.LoopFreq(AudioSynth.Midi(64f), duration);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float p = i / (float)n;
                float water = AudioSynth.LoopNoise(p, 3.4f) * 0.12f;
                float pad = AudioSynth.Sin(f, t) * 0.13f
                            + AudioSynth.Sin(a, t) * 0.1f
                            + AudioSynth.Sin(c, t) * 0.08f
                            + AudioSynth.Sin(e, t) * 0.06f;
                pad *= 0.8f + 0.2f * AudioSynth.Sin(0.1875f, t);
                data[i] = water + pad;
            }
        }

        static void Aurora(float[] data, float duration)
        {
            int n = data.Length;
            float d = AudioSynth.LoopFreq(AudioSynth.Midi(50f), duration);
            float a = AudioSynth.LoopFreq(AudioSynth.Midi(57f), duration);
            float d4 = AudioSynth.LoopFreq(AudioSynth.Midi(62f), duration);
            float a4 = AudioSynth.LoopFreq(AudioSynth.Midi(69f), duration);
            float detune = AudioSynth.LoopFreq(AudioSynth.Midi(50.08f), duration);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float swell = 0.55f + 0.45f * AudioSynth.Sin(0.125f, t);
                float pad = AudioSynth.Sin(d, t) * 0.12f
                            + AudioSynth.Sin(detune, t) * 0.1f
                            + AudioSynth.Sin(a, t) * 0.1f
                            + AudioSynth.Sin(d4, t) * 0.07f
                            + AudioSynth.Sin(a4, t) * 0.05f;
                data[i] = pad * swell;
            }
        }
    }
}
