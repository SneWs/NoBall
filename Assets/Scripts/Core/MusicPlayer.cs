using UnityEngine;

namespace NoBall
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        AudioSource _source;
        GameBackgroundId _theme;
        float _targetVolume = 0.34f;
        bool _built;

        public void Build()
        {
            if (_built)
                return;
            _built = true;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.priority = 64;
            GameSettings.SoundChanged += Refresh;
            GameSettings.BackgroundChanged += OnBackgroundChanged;
            Apply(GameSettings.Background, true);
        }

        void OnDestroy()
        {
            GameSettings.SoundChanged -= Refresh;
            GameSettings.BackgroundChanged -= OnBackgroundChanged;
        }

        void Update()
        {
            if (_source == null)
                return;
            float want = GameSettings.SoundEnabled ? _targetVolume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, want, Time.unscaledDeltaTime * 1.2f);
            if (!GameSettings.SoundEnabled && _source.volume <= 0.001f && _source.isPlaying)
                _source.Pause();
        }

        void OnBackgroundChanged()
        {
            Apply(GameSettings.Background, false);
        }

        void Apply(GameBackgroundId id, bool immediate)
        {
            if (!immediate && _theme == id && _source.clip != null)
            {
                Refresh();
                return;
            }

            _theme = id;
            var clip = ThemeMusic.Get(id);
            if (_source.clip != clip)
            {
                bool wasPlaying = _source.isPlaying || immediate;
                _source.clip = clip;
                if (!immediate)
                    _source.volume = 0f;
                if (wasPlaying && GameSettings.SoundEnabled)
                    _source.Play();
            }

            Refresh();
        }

        void Refresh()
        {
            if (_source == null || _source.clip == null)
                return;
            if (!GameSettings.SoundEnabled)
                return;
            if (_source.isPlaying)
                return;
            _source.UnPause();
            if (!_source.isPlaying)
                _source.Play();
        }
    }
}
