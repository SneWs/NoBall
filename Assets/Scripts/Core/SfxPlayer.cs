using UnityEngine;

namespace NoBall
{
    public sealed class SfxPlayer : MonoBehaviour
    {
        AudioSource _source;
        AudioClip _complete;
        AudioClip _hit;
        AudioClip _win;
        AudioClip _lose;
        AudioClip _click;

        public void Build()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _complete = Tone(740f, 0.07f);
            _hit = Tone(160f, 0.14f, 0.55f);
            _win = Chord(new[] { 523f, 659f, 784f }, 0.28f);
            _lose = Tone(110f, 0.32f, 0.7f);
            _click = Tone(520f, 0.04f, 0.25f);
        }

        public void PlayComplete() => Play(_complete, 0.55f);
        public void PlayHit() => Play(_hit, 0.7f);
        public void PlayWin() => Play(_win, 0.65f);
        public void PlayLose() => Play(_lose, 0.75f);
        public void PlayClick() => Play(_click, 0.4f);

        void Play(AudioClip clip, float volume)
        {
            if (!GameSettings.SoundEnabled || clip == null || _source == null)
                return;
            _source.PlayOneShot(clip, volume);
        }

        static AudioClip Tone(float freq, float duration, float volume = 0.4f)
        {
            int rate = 22050;
            int samples = Mathf.CeilToInt(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float env = 1f - i / (float)samples;
                env *= env;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * volume;
            }

            var clip = AudioClip.Create("tone", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(float[] freqs, float duration)
        {
            int rate = 22050;
            int samples = Mathf.CeilToInt(rate * duration);
            var data = new float[samples];
            float inv = 1f / freqs.Length;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float env = 1f - i / (float)samples;
                float s = 0f;
                for (int f = 0; f < freqs.Length; f++)
                    s += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                data[i] = s * inv * env * 0.35f;
            }

            var clip = AudioClip.Create("chord", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
