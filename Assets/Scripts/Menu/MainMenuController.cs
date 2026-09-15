using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoBall
{
    public sealed class MainMenuController : MonoBehaviour
    {
        RectTransform _safe;
        GameObject _settingsPanel;
        Text _soundLabel;
        SfxPlayer _sfx;
        int _lastWidth;
        int _lastHeight;

        void Awake()
        {
            UiFactory.EnsureCamera(GameColors.CameraBg);
            UiFactory.EnsureEventSystem();
            _sfx = gameObject.AddComponent<SfxPlayer>();
            _sfx.Build();
            BuildUi();
        }

        void Update()
        {
            if (_lastWidth == Screen.width && _lastHeight == Screen.height)
                return;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            UiFactory.ApplySafeArea(_safe);
        }

        void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas("MainMenuCanvas");
            canvas.transform.SetParent(transform, false);
            _safe = UiFactory.CreateSafeArea(canvas.transform);

            var title = UiFactory.CreateLabel("Title", _safe, "NOBALL", 96, GameColors.Title);
            UiFactory.SetAnchored(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 120), new Vector2(0f, 220f));
            title.fontStyle = FontStyle.Bold;

            var subtitle = UiFactory.CreateLabel("Subtitle", _safe, "Box the bouncing atoms in", 32, GameColors.Muted);
            UiFactory.SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 48), new Vector2(0f, 130f));

            var newGame = UiFactory.CreateButton("NewGame", _safe, "New game", GameColors.ButtonPrimary, OnNewGame, new Vector2(480, 96));
            UiFactory.SetAnchored(newGame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480, 96), new Vector2(0f, 20f));

            var settings = UiFactory.CreateButton("Settings", _safe, "Settings", GameColors.ButtonSecondary, OnOpenSettings, new Vector2(480, 96));
            UiFactory.SetAnchored(settings.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480, 96), new Vector2(0f, -110f));

            var how = UiFactory.CreateLabel(
                "HowTo",
                _safe,
                "Swipe across the room to grow a wall.\nClose off empty space. Claim 75%.",
                24,
                GameColors.Muted);
            UiFactory.SetAnchored(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 80), new Vector2(0f, -240f));

            BuildSettingsPanel();
        }

        void BuildSettingsPanel()
        {
            var dim = UiFactory.CreateImage("SettingsDim", _safe, new Color(0f, 0f, 0f, 0.62f));
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            _settingsPanel = dim.gameObject;

            var card = UiFactory.CreateImage("Card", dim.rectTransform, GameColors.Panel, SpriteFactory.Rounded);
            UiFactory.SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640, 460), Vector2.zero);
            card.raycastTarget = true;

            var title = UiFactory.CreateLabel("Title", card.rectTransform, "Settings", 48, GameColors.Title);
            UiFactory.SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560, 72), new Vector2(0f, -28f));
            title.fontStyle = FontStyle.Bold;

            var sound = UiFactory.CreateButton("Sound", card.rectTransform, SoundCaption(), GameColors.ButtonSecondary, OnToggleSound, new Vector2(420, 84));
            UiFactory.SetAnchored(sound.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420, 84), new Vector2(0f, 36f));
            _soundLabel = sound.GetComponentInChildren<Text>();

            var help = UiFactory.CreateLabel(
                "Help",
                card.rectTransform,
                "Horizontal swipe: left/right wall\nVertical swipe: up/down wall\nA ball hitting a growing wall costs a life.",
                22,
                GameColors.Muted);
            UiFactory.SetAnchored(help.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 110), new Vector2(0f, -90f));

            var close = UiFactory.CreateButton("Close", card.rectTransform, "Close", GameColors.ButtonPrimary, OnCloseSettings, new Vector2(420, 84));
            UiFactory.SetAnchored(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420, 84), new Vector2(0f, 28f));

            _settingsPanel.SetActive(false);
        }

        void OnNewGame()
        {
            _sfx.PlayClick();
            SceneManager.LoadScene(GameConfig.GameScene);
        }

        void OnOpenSettings()
        {
            _sfx.PlayClick();
            _settingsPanel.SetActive(true);
        }

        void OnCloseSettings()
        {
            _sfx.PlayClick();
            _settingsPanel.SetActive(false);
        }

        void OnToggleSound()
        {
            GameSettings.SoundEnabled = !GameSettings.SoundEnabled;
            if (_soundLabel != null)
                _soundLabel.text = SoundCaption();
            _sfx.PlayClick();
        }

        static string SoundCaption() => GameSettings.SoundEnabled ? "Sound: On" : "Sound: Off";
    }
}
