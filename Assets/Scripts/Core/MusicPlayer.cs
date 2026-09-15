using UnityEngine;

namespace NoBall
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        AudioSource _source;
        float _targetVolume = 0.42f;
        bool _built;
        bool _held;

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
            _source.clip = Resources.Load<AudioClip>("Music/lab_background_music");
            if (_source.clip != null)
                _source.clip.LoadAudioData();
            GameSettings.SoundChanged += Refresh;
            Refresh();
        }

        public void Hold()
        {
            _held = true;
            if (_source != null && _source.isPlaying)
                _source.Pause();
        }

        public void Release()
        {
            _held = false;
            Refresh();
        }

        void OnDestroy()
        {
            GameSettings.SoundChanged -= Refresh;
        }

        void Update()
        {
            if (_source == null)
                return;
            float want = !_held && GameSettings.SoundEnabled ? _targetVolume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, want, Time.unscaledDeltaTime * 1.2f);
            if ((_held || !GameSettings.SoundEnabled) && _source.volume <= 0.001f && _source.isPlaying)
                _source.Pause();
        }

        void Refresh()
        {
            if (_held || _source == null || _source.clip == null)
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
