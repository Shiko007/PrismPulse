using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PrismPulse.Gameplay.Audio;

namespace PrismPulse.Gameplay.UI
{
    /// <summary>
    /// Overlay shown when a puzzle is solved.
    /// Shows stars, stats, and next/restart buttons.
    /// </summary>
    public class WinScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _starsText;
        private TextMeshProUGUI _statsText;
        private Button _nextButton;
        private Button _restartButton;

        private CanvasGroup _canvasGroup;

        public System.Action OnNextLevel;
        public System.Action OnRestart;
        public System.Action OnMainMenu;

        public void Initialize()
        {
            BuildUI();
            _canvas.gameObject.SetActive(false);
        }

        public void Show(int stars, int moves, float time, bool hasNextLevel)
        {
            _canvas.gameObject.SetActive(true);
            _panel.SetActive(true);

            _starsText.text = new string('*', stars) + new string('-', 3 - stars);
            _starsText.color = stars == 3
                ? new Color(1f, 0.85f, 0.2f)   // gold
                : new Color(0.8f, 0.8f, 0.8f);  // silver

            _titleText.text = stars == 3 ? "PERFECT!" : "SOLVED!";
            _statsText.text = $"{moves} moves  |  {FormatTime(time)}";

            _nextButton.gameObject.SetActive(hasNextLevel);

            // Animate in
            _canvasGroup.alpha = 0f;
            _panel.transform.localScale = Vector3.one * 0.8f;
            _panel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            // Fade in via coroutine since DOTween UI module may not be enabled
            StartCoroutine(FadeIn(0.3f));
        }

        public void Hide()
        {
            _canvas.gameObject.SetActive(false);
        }

        private string FormatTime(float seconds)
        {
            int mins = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            if (mins > 0) return $"{mins}:{secs:D2}";
            return $"{secs}s";
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private void BuildUI()
        {
            // Canvas (separate from HUD, higher sort order)
            var canvasGO = new GameObject("Win Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Darkened background (full screen, MUST be first child so buttons render on top)
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGO.transform, false);

            // Safe area container (after background so it renders on top)
            var safeAreaGO = new GameObject("SafeArea");
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            var safeRect = safeAreaGO.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            ApplySafeArea(safeRect);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.6f);

            // Panel (inside safe area)
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(safeAreaGO.transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.2f);
            panelRect.anchorMax = new Vector2(0.9f, 0.75f);
            panelRect.sizeDelta = Vector2.zero;
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.15f, 0.95f);

            _canvasGroup = _panel.AddComponent<CanvasGroup>();

            // Title
            _titleText = CreateText(_panel.transform, "Title", "SOLVED!",
                new Vector2(0.5f, 0.88f), 56, TextAlignmentOptions.Center);
            _titleText.color = new Color(0.5f, 1f, 0.7f);

            // Stars
            _starsText = CreateText(_panel.transform, "Stars", "***",
                new Vector2(0.5f, 0.72f), 72, TextAlignmentOptions.Center);

            // Stats
            _statsText = CreateText(_panel.transform, "Stats", "",
                new Vector2(0.5f, 0.57f), 28, TextAlignmentOptions.Center);
            _statsText.color = new Color(0.7f, 0.7f, 0.8f);

            // Next button
            _nextButton = CreateButton(_panel.transform, "Next Level",
                new Vector2(0.5f, 0.38f), new Color(0.2f, 0.7f, 0.4f));
            _nextButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnNextLevel?.Invoke();
            });

            // Restart button
            _restartButton = CreateButton(_panel.transform, "Restart",
                new Vector2(0.5f, 0.22f), new Color(0.3f, 0.3f, 0.4f));
            _restartButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnRestart?.Invoke();
            });

            // Menu button
            var menuButton = CreateButton(_panel.transform, "Menu",
                new Vector2(0.5f, 0.08f), new Color(0.25f, 0.25f, 0.35f));
            menuButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnMainMenu?.Invoke();
            });
        }

        private static void ApplySafeArea(RectTransform rect)
        {
            var safeArea = Screen.safeArea;
            rect.anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
            rect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            Vector2 anchorPos, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 80);
            rect.anchoredPosition = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            return tmp;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchorPos, Color bgColor)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            // Button label
            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
