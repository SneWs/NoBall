using UnityEngine;

namespace NoBall
{
    public static class AudioSynth
    {
        public const int Rate = 22050;

        public static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Buffer(float seconds)
        {
            return new float[Mathf.Max(2, Mathf.RoundToInt(Rate * seconds))];
        }

        public static float LoopFreq(float freq, float duration)
        {
            return Mathf.Max(1f, Mathf.Round(freq * duration)) / duration;
        }

        public static float Midi(float note)
        {
            return 440f * Mathf.Pow(2f, (note - 69f) / 12f);
        }

        public static float Sin(float freq, float t)
        {
            return Mathf.Sin(2f * Mathf.PI * freq * t);
        }

        public static float SoftSquare(float freq, float t)
        {
            return Sin(freq, t) * 0.7f + Sin(freq * 3f, t) * 0.22f + Sin(freq * 5f, t) * 0.08f;
        }

        public static float SoftSaw(float freq, float t)
        {
            float s = 0f;
            s += Sin(freq, t);
            s += Sin(freq * 2f, t) * 0.5f;
            s += Sin(freq * 3f, t) * 0.32f;
            s += Sin(freq * 4f, t) * 0.18f;
            return s * 0.45f;
        }

        public static float Exp(int i, int n)
        {
            float e = 1f - i / (float)n;
            return e * e;
        }

        public static float AttackDecay(int i, int n, float attack)
        {
            float t = i / (float)n;
            if (t < attack)
                return t / Mathf.Max(attack, 0.0001f);
            float d = 1f - (t - attack) / Mathf.Max(1f - attack, 0.0001f);
            return d * d;
        }

        public static float Hash(int i)
        {
            uint x = (uint)(i * 747796405 + 2891336453);
            x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return (x & 0xFFFFFF) / 16777215f;
        }

        public static float LoopNoise(float phase01, float seed)
        {
            float a = phase01 * 2f * Mathf.PI;
            float n = Mathf.PerlinNoise(Mathf.Cos(a) * 3.2f + seed, Mathf.Sin(a) * 3.2f + seed * 1.7f);
            float n2 = Mathf.PerlinNoise(Mathf.Cos(a) * 8.1f + seed * 2.1f, Mathf.Sin(a) * 8.1f + seed);
            return (n - 0.5f) * 0.7f + (n2 - 0.5f) * 0.3f;
        }

        public static void Add(float[] data, int i, float sample)
        {
            data[i] = Mathf.Clamp(data[i] + sample, -1f, 1f);
        }

        public static void Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            float g = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= g;
        }

        public static void Pluck(float[] data, int start, int length, float freq, float amp)
        {
            int n = Mathf.Min(length, data.Length - start);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Exp(i, n);
                Add(data, start + i, Sin(freq, t) * env * amp + Sin(freq * 2.01f, t) * env * amp * 0.18f);
            }
        }
    }
}
