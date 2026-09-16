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
        Text _backgroundCaption;
        Image[] _backgroundBorders;
        SfxPlayer _sfx;
        int _lastWidth;
        int _lastHeight;

        void Awake()
        {
            var camera = UiFactory.EnsureCamera(GameColors.CameraBg);
            UiFactory.EnsureEventSystem();
            _sfx = GameAudio.Ensure().Sfx;
            var backgroundGo = new GameObject("Background");
            backgroundGo.AddComponent<GameBackgroundView>().BuildFullScreen(camera);
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
                GameConfig.HowToPlay,
                24,
                GameColors.Muted);
            UiFactory.SetAnchored(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900, 80), new Vector2(0f, -240f));

            BuildSettingsPanel();
        }

        void BuildSettingsPanel()
        {
            var dim = UiFactory.CreateImage("SettingsDim", _safe, new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            _settingsPanel = dim.gameObject;

            var card = UiFactory.CreateImage("Card", dim.rectTransform, GameColors.Panel, SpriteFactory.Rounded);
            UiFactory.SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720, 620), Vector2.zero);
            card.raycastTarget = true;

            var title = UiFactory.CreateLabel("Title", card.rectTransform, "Settings", 48, GameColors.Title);
            UiFactory.SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640, 64), new Vector2(0f, -28f));
            title.fontStyle = FontStyle.Bold;

            var sound = UiFactory.CreateButton("Sound", card.rectTransform, SoundCaption(), GameColors.ButtonSecondary, OnToggleSound, new Vector2(420, 76));
            UiFactory.SetAnchored(sound.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 76), new Vector2(0f, -108f));
            _soundLabel = sound.GetComponentInChildren<Text>();

            var backgroundLabel = UiFactory.CreateLabel("BackgroundLabel", card.rectTransform, "Background", 26, GameColors.Muted);
            UiFactory.SetAnchored(backgroundLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640, 36), new Vector2(0f, -196f));

            BuildBackgroundRow(card.rectTransform);

            _backgroundCaption = UiFactory.CreateLabel("BackgroundCaption", card.rectTransform, "", 22, GameColors.Title);
            UiFactory.SetAnchored(_backgroundCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640, 32), new Vector2(0f, -328f));

            var help = UiFactory.CreateLabel(
                "Help",
                card.rectTransform,
                GameConfig.ControlsHelp,
                22,
                GameColors.Muted);
            UiFactory.SetAnchored(help.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640, 100), new Vector2(0f, -370f));

            var close = UiFactory.CreateButton("Close", card.rectTransform, "Close", GameColors.ButtonPrimary, OnCloseSettings, new Vector2(420, 80));
            UiFactory.SetAnchored(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420, 80), new Vector2(0f, 24f));

            RefreshBackgroundUi();
            _settingsPanel.SetActive(false);
        }

        void BuildBackgroundRow(RectTransform parent)
        {
            const float total = 640f;
            const float gap = 10f;
            float width = (total - gap * (GameBackgrounds.Count - 1)) / GameBackgrounds.Count;
            float height = 80f;
            float origin = -total * 0.5f + width * 0.5f;
            _backgroundBorders = new Image[GameBackgrounds.Count];

            for (int i = 0; i < GameBackgrounds.Count; i++)
            {
                var id = (GameBackgroundId)i;
                var border = UiFactory.CreateImage("Bg" + id, parent, GameColors.PanelBorder, SpriteFactory.Rounded);
                border.raycastTarget = true;
                UiFactory.SetAnchored(
                    border.rectTransform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(width, height),
                    new Vector2(origin + i * (width + gap), -240f));

                var button = border.gameObject.AddComponent<Button>();
                button.targetGraphic = border;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                int captured = i;
                button.onClick.AddListener(() => OnSelectBackground((GameBackgroundId)captured));

                var mask = border.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;

                var inner = UiFactory.CreateRect("Art", border.rectTransform);
                UiFactory.Stretch(inner, 5f);
                var raw = inner.gameObject.AddComponent<RawImage>();
                var tex = GameBackgroundView.TextureFor(id);
                raw.texture = tex != null ? tex : SpriteFactory.White.texture;
                raw.color = tex != null ? Color.white : GameColors.Playfield;
                raw.raycastTarget = false;

                var capBar = UiFactory.CreateImage("CapBar", border.rectTransform, new Color(0f, 0f, 0f, 0.55f));
                UiFactory.SetAnchored(capBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(width - 10f, 22f), new Vector2(0f, 5f));

                var caption = UiFactory.CreateLabel("Name", border.rectTransform, ThumbName(id), 16, GameColors.Title);
                UiFactory.SetAnchored(caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(width, 22), new Vector2(0f, 5f));

                _backgroundBorders[i] = border;
            }
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

        void OnSelectBackground(GameBackgroundId id)
        {
            GameSettings.Background = id;
            RefreshBackgroundUi();
            _sfx.PlayClick();
        }

        void RefreshBackgroundUi()
        {
            var current = GameSettings.Background;
            if (_backgroundCaption != null)
                _backgroundCaption.text = GameBackgrounds.DisplayName(current);
            if (_backgroundBorders == null)
                return;
            for (int i = 0; i < _backgroundBorders.Length; i++)
            {
                if (_backgroundBorders[i] == null)
                    continue;
                _backgroundBorders[i].color = (GameBackgroundId)i == current
                    ? GameColors.Building
                    : GameColors.PanelBorder;
            }
        }

        static string SoundCaption() => GameSettings.SoundEnabled ? "Sound: On" : "Sound: Off";

        static string ThumbName(GameBackgroundId id) => id switch
        {
            GameBackgroundId.Aurora => "Aurora",
            _ => "Lab"
        };
    }
}
