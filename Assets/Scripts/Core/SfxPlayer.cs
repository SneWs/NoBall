using UnityEngine;

namespace NoBall
{
    public sealed class SfxPlayer : MonoBehaviour
    {
        AudioSource _oneShot;
        AudioSource _bounceSource;
        AudioSource _grow;
        AudioClip _bounce;
        AudioClip _growStart;
        AudioClip _growLoop;
        AudioClip _hit;
        AudioClip _complete;
        AudioClip _win;
        AudioClip _levelComplete;
        AudioClip _lose;
        AudioClip _click;
        float _lastBounceTime = -1f;

        public void Build()
        {
            _oneShot = CreateSource(128);
            _bounceSource = CreateSource(100);
            _grow = CreateSource(96);
            _grow.loop = true;
            GameSettings.SoundChanged += OnSoundChanged;
            _bounce = BounceClip();
            _growStart = GrowStartClip();
            _growLoop = GrowLoopClip();
            _hit = HitClip();
            _complete = CompleteClip();
            _win = WinClip();
            _levelComplete = Resources.Load<AudioClip>("Music/level_complete");
            if (_levelComplete != null)
                _levelComplete.LoadAudioData();
            _lose = LoseClip();
            _click = ClickClip();
            _grow.clip = _growLoop;
        }

        public void PlayBounce()
        {
            if (Time.unscaledTime - _lastBounceTime < 0.042f)
                return;
            _lastBounceTime = Time.unscaledTime;
            if (!GameSettings.SoundEnabled || _bounce == null || _bounceSource == null)
                return;
            _bounceSource.pitch = Random.Range(0.9f, 1.16f);
            _bounceSource.PlayOneShot(_bounce, 0.32f);
        }

        void OnDestroy()
        {
            GameSettings.SoundChanged -= OnSoundChanged;
        }

        void OnSoundChanged()
        {
            if (!GameSettings.SoundEnabled)
                StopGrow();
        }

        public void PlayGrowStart()
        {
            StopGrow();
            Play(_growStart, 0.42f);
            if (!GameSettings.SoundEnabled || _grow == null || _growLoop == null)
                return;
            _grow.volume = 0.22f;
            _grow.pitch = 1f;
            _grow.Play();
        }

        public void StopGrow()
        {
            if (_grow != null && _grow.isPlaying)
                _grow.Stop();
        }

        public void PlayComplete()
        {
            StopGrow();
            Play(_complete, 0.58f);
        }

        public void PlayHit()
        {
            StopGrow();
            Play(_hit, 0.72f);
        }

        public void PlayWin()
        {
            PlayLevelComplete();
        }

        public float PlayLevelComplete()
        {
            StopGrow();
            var clip = _levelComplete != null ? _levelComplete : _win;
            if (!GameSettings.SoundEnabled || clip == null || _oneShot == null)
                return 0f;
            _oneShot.pitch = 1f;
            _oneShot.PlayOneShot(clip, _levelComplete != null ? 0.9f : 0.7f);
            return Mathf.Max(0.05f, clip.length);
        }

        public void PlayLose()
        {
            StopGrow();
            Play(_lose, 0.78f);
        }

        public void PlayClick() => Play(_click, 0.38f);

        void Play(AudioClip clip, float volume)
        {
            if (!GameSettings.SoundEnabled || clip == null || _oneShot == null)
                return;
            _oneShot.PlayOneShot(clip, volume);
        }

        AudioSource CreateSource(int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = priority;
            return source;
        }

        static AudioClip BounceClip()
        {
            var data = AudioSynth.Buffer(0.07f);
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float env = AudioSynth.Exp(i, n);
                float body = AudioSynth.Sin(420f, t) * env;
                float click = (AudioSynth.Hash(i) - 0.5f) * Mathf.Exp(-t * 70f);
                data[i] = body * 0.75f + click * 0.18f;
            }

            AudioSynth.Normalize(data, 0.85f);
            return AudioSynth.Clip("bounce", data);
        }

        static AudioClip GrowStartClip()
        {
            var data = AudioSynth.Buffer(0.12f);
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float env = AudioSynth.AttackDecay(i, n, 0.08f);
                float freq = Mathf.Lerp(320f, 920f, t / 0.12f);
                data[i] = AudioSynth.SoftSaw(freq, t) * env * 0.55f
                          + AudioSynth.Sin(freq * 2.02f, t) * env * 0.2f;
            }

            AudioSynth.Normalize(data, 0.8f);
            return AudioSynth.Clip("growStart", data);
        }

        static AudioClip GrowLoopClip()
        {
            const float duration = 0.25f;
            var data = AudioSynth.Buffer(duration);
            int n = data.Length;
            float hum = AudioSynth.LoopFreq(196f, duration);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float p = i / (float)n;
                float pulse = 0.55f + 0.45f * AudioSynth.Sin(16f, t);
                float noise = AudioSynth.LoopNoise(p, 0.9f);
                data[i] = noise * 0.28f * pulse + AudioSynth.Sin(hum, t) * 0.16f * pulse;
            }

            AudioSynth.Normalize(data, 0.55f);
            return AudioSynth.Clip("growLoop", data);
        }

        static AudioClip HitClip()
        {
            var data = AudioSynth.Buffer(0.24f);
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float env = AudioSynth.Exp(i, n);
                float body = AudioSynth.Sin(78f, t) * env + AudioSynth.Sin(132f, t) * env * 0.55f;
                float noise = (AudioSynth.Hash(i) - 0.5f) * Mathf.Exp(-t * 18f);
                data[i] = body * 0.7f + noise * 0.55f;
            }

            AudioSynth.Normalize(data, 0.9f);
            return AudioSynth.Clip("hit", data);
        }

        static AudioClip CompleteClip()
        {
            var data = AudioSynth.Buffer(0.22f);
            int n = data.Length;
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float s = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * 0.04f;
                    if (t < start)
                        continue;
                    float lt = t - start;
                    float env = Mathf.Exp(-lt * 9f);
                    s += AudioSynth.Sin(notes[k], lt) * env;
                }

                data[i] = s * 0.45f;
            }

            AudioSynth.Normalize(data, 0.8f);
            return AudioSynth.Clip("complete", data);
        }

        static AudioClip WinClip()
        {
            var data = AudioSynth.Buffer(0.55f);
            int n = data.Length;
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float s = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = k * 0.08f;
                    if (t < start)
                        continue;
                    float lt = t - start;
                    float env = Mathf.Exp(-lt * 5f);
                    s += AudioSynth.Sin(notes[k], lt) * env;
                    s += AudioSynth.Sin(notes[k] * 2.01f, lt) * env * 0.12f;
                }

                data[i] = s * 0.4f;
            }

            AudioSynth.Normalize(data, 0.85f);
            return AudioSynth.Clip("win", data);
        }

        static AudioClip LoseClip()
        {
            var data = AudioSynth.Buffer(0.5f);
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float env = AudioSynth.Exp(i, n);
                float slide = Mathf.Lerp(180f, 70f, t / 0.5f);
                float rumble = (AudioSynth.Hash(i / 8) - 0.5f) * env * 0.2f;
                data[i] = AudioSynth.SoftSaw(slide, t) * env * 0.55f
                          + AudioSynth.Sin(slide * 0.5f, t) * env * 0.3f
                          + rumble;
            }

            AudioSynth.Normalize(data, 0.88f);
            return AudioSynth.Clip("lose", data);
        }

        static AudioClip ClickClip()
        {
            var data = AudioSynth.Buffer(0.045f);
            int n = data.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float env = AudioSynth.Exp(i, n);
                data[i] = AudioSynth.Sin(880f, t) * env * 0.55f
                          + (AudioSynth.Hash(i) - 0.5f) * env * 0.12f;
            }

            AudioSynth.Normalize(data, 0.7f);
            return AudioSynth.Clip("click", data);
        }
    }
}
