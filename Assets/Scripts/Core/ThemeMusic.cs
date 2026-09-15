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
            if (id == GameBackgroundId.Aurora)
                Aurora(data, duration);
            else
                Laboratory(data, duration);

            AudioSynth.Normalize(data, 0.22f);
            return AudioSynth.Clip("Theme_" + id, data);
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
