using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NoBall
{
    public sealed class GameHud : MonoBehaviour
    {
        Text _claimed;
        Text _lives;
        Text _score;
        Text _hint;
        GameObject _endPanel;
        Text _endTitle;
        Text _endBody;
        RectTransform _safe;
        float _hintTimer;

        public void Build(UnityAction onMenu, UnityAction onRetry)
        {
            var canvas = UiFactory.CreateCanvas("GameHUD", 10);
            canvas.transform.SetParent(transform, false);
            _safe = UiFactory.CreateSafeArea(canvas.transform);

            var top = UiFactory.CreateImage("TopBar", _safe, GameColors.Hud);
            var topRt = top.rectTransform;
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(0f, 96f);
            topRt.anchoredPosition = Vector2.zero;
            top.raycastTarget = false;

            var menu = UiFactory.CreateButton("Menu", topRt, "Menu", GameColors.ButtonSecondary, onMenu, new Vector2(160, 64));
            UiFactory.SetAnchored(menu.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160, 64), new Vector2(24f, 0f));

            _lives = UiFactory.CreateLabel("Lives", topRt, "Lives  2", 32, GameColors.Title, TextAnchor.MiddleLeft);
            UiFactory.SetAnchored(_lives.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220, 64), new Vector2(204f, 0f));

            _claimed = UiFactory.CreateLabel("Claimed", topRt, "0%  /  75%", 32, GameColors.Muted, TextAnchor.MiddleCenter);
            UiFactory.SetAnchored(_claimed.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 64), Vector2.zero);

            _score = UiFactory.CreateLabel("Score", topRt, "0", 36, GameColors.Building, TextAnchor.MiddleRight);
            UiFactory.SetAnchored(_score.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(280, 64), new Vector2(-28f, 0f));
            _score.fontStyle = FontStyle.Bold;

            _hint = UiFactory.CreateLabel("Hint", _safe, "Swipe horizontally or vertically to build a wall", 28, GameColors.Muted);
            UiFactory.SetAnchored(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(900, 64), new Vector2(0f, 36f));

            BuildEndPanel(onMenu, onRetry);
            HideEnd();
        }

        public void RefreshSafeArea()
        {
            if (_safe != null)
                UiFactory.ApplySafeArea(_safe);
        }

        public void SetStats(int lives, float claimed, float target, int score)
        {
            _lives.text = "Lives  " + lives;
            _claimed.text = Mathf.FloorToInt(claimed * 100f) + "%  /  " + Mathf.FloorToInt(target * 100f) + "%";
            _score.text = score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public void ShowHint(string text, float seconds)
        {
            _hint.text = text;
            _hint.enabled = true;
            _hintTimer = seconds;
        }

        public void ShowEnd(string title, string body)
        {
            _endTitle.text = title;
            _endBody.text = body;
            _endPanel.SetActive(true);
        }

        public void HideEnd()
        {
            if (_endPanel != null)
                _endPanel.SetActive(false);
        }

        void Update()
        {
            if (_hintTimer > 0f)
            {
                _hintTimer -= Time.deltaTime;
                if (_hintTimer <= 0f)
                    _hint.enabled = false;
            }
        }

        void BuildEndPanel(UnityAction onMenu, UnityAction onRetry)
        {
            var dim = UiFactory.CreateImage("EndPanel", _safe, new Color(0f, 0f, 0f, 0.62f));
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            _endPanel = dim.gameObject;

            var card = UiFactory.CreateImage("Card", dim.rectTransform, GameColors.Panel, SpriteFactory.Rounded);
            UiFactory.SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640, 420), Vector2.zero);
            card.raycastTarget = true;

            _endTitle = UiFactory.CreateLabel("Title", card.rectTransform, "Title", 48, GameColors.Title);
            UiFactory.SetAnchored(_endTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560, 80), new Vector2(0f, -36f));
            _endTitle.fontStyle = FontStyle.Bold;

            _endBody = UiFactory.CreateLabel("Body", card.rectTransform, "Body", 28, GameColors.Muted);
            UiFactory.SetAnchored(_endBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560, 80), new Vector2(0f, -120f));

            var retry = UiFactory.CreateButton("Retry", card.rectTransform, "Play again", GameColors.ButtonPrimary, onRetry, new Vector2(420, 84));
            UiFactory.SetAnchored(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420, 84), new Vector2(0f, 128f));

            var menu = UiFactory.CreateButton("Menu", card.rectTransform, "Main menu", GameColors.ButtonSecondary, onMenu, new Vector2(420, 84));
            UiFactory.SetAnchored(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420, 84), new Vector2(0f, 28f));
        }
    }
}
