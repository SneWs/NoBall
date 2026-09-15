using UnityEngine;

namespace NoBall
{
    public sealed class GameAudio : MonoBehaviour
    {
        static GameAudio _instance;

        public SfxPlayer Sfx { get; private set; }
        public MusicPlayer Music { get; private set; }

        public static GameAudio Ensure()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameAudio>();
            _instance.Build();
            return _instance;
        }

        void Build()
        {
            Sfx = gameObject.AddComponent<SfxPlayer>();
            Sfx.Build();
            Music = gameObject.AddComponent<MusicPlayer>();
            Music.Build();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
