using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PrismPulse.Gameplay.Audio;

namespace PrismPulse.Gameplay.UI
{
    /// <summary>
    /// Title screen shown on launch.
    /// Play button, settings with sound toggle, decorative animated beam line.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _root;
        private CanvasGroup _canvasGroup;
        private Image _beamLine;
        private TextMeshProUGUI _titleText;

        private GameObject _settingsPanel;
        private TextMeshProUGUI _soundToggleLabel;
        private bool _soundOn = true;

        public System.Action OnPlay;

        public void Initialize()
        {
            BuildUI();
        }

        public void Show()
        {
            _root.SetActive(true);
            _canvas.gameObject.SetActive(true);

            _canvasGroup.alpha = 0f;
            _root.transform.localScale = Vector3.one * 0.8f;
            _root.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            StartCoroutine(FadeIn(0.3f));
            StartCoroutine(AnimateBeamLine());
            AnimateTitle();
        }

        public void Hide()
        {
            _titleText.transform.DOKill();
            _canvas.gameObject.SetActive(false);
        }

        private void BuildUI()
        {
            // Canvas
            var canvasGO = new GameObject("Menu Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Full-screen background
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGO.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.02f, 0.02f, 0.06f, 1f);

            // Safe area
            var safeAreaGO = new GameObject("SafeArea");
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            var safeRect = safeAreaGO.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            ApplySafeArea(safeRect);

            // Root panel (inside safe area)
            _root = new GameObject("Root");
            _root.transform.SetParent(safeAreaGO.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            _canvasGroup = _root.AddComponent<CanvasGroup>();

            // Title
            _titleText = CreateText(_root.transform, "Title", "PRISM PULSE",
                new Vector2(0.5f, 0.75f), 72, TextAlignmentOptions.Center);
            _titleText.color = new Color(0.5f, 1f, 0.9f);
            _titleText.fontStyle = FontStyles.Bold;

            // Subtitle
            var subtitle = CreateText(_root.transform, "Subtitle", "A Light Puzzle",
                new Vector2(0.5f, 0.68f), 28, TextAlignmentOptions.Center);
            subtitle.color = new Color(0.5f, 0.5f, 0.6f);

            // Decorative beam line
            var lineGO = new GameObject("BeamLine");
            lineGO.transform.SetParent(_root.transform, false);
            var lineRect = lineGO.AddComponent<RectTransform>();
            lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 0.58f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(800, 4);
            lineRect.anchoredPosition = Vector2.zero;
            _beamLine = lineGO.AddComponent<Image>();
            _beamLine.color = new Color(0.3f, 0.8f, 1f);

            // Play button
            var playBtn = CreateButton(_root.transform, "PLAY",
                new Vector2(0.5f, 0.40f), new Color(0.2f, 0.7f, 0.4f),
                new Vector2(360, 80), 36);
            playBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnPlay?.Invoke();
            });

            // Settings button
            var settingsBtn = CreateButton(_root.transform, "Settings",
                new Vector2(0.5f, 0.28f), new Color(0.3f, 0.3f, 0.4f),
                new Vector2(300, 60), 28);
            settingsBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                ShowSettings(true);
            });

            // Settings panel (hidden by default)
            BuildSettingsPanel(safeAreaGO.transform);
        }

        private void BuildSettingsPanel(Transform parent)
        {
            _settingsPanel = new GameObject("SettingsPanel");
            _settingsPanel.transform.SetParent(parent, false);
            var panelRect = _settingsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.35f);
            panelRect.anchorMax = new Vector2(0.9f, 0.65f);
            panelRect.sizeDelta = Vector2.zero;
            var panelImage = _settingsPanel.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.06f, 0.12f, 0.95f);

            // Title
            var title = CreateText(_settingsPanel.transform, "SettingsTitle", "Settings",
                new Vector2(0.5f, 0.85f), 42, TextAlignmentOptions.Center);
            title.color = new Color(0.7f, 0.8f, 1f);

            // Sound label
            CreateText(_settingsPanel.transform, "SoundLabel", "Sound",
                new Vector2(0.3f, 0.55f), 32, TextAlignmentOptions.Center);

            // Sound toggle button
            _soundOn = SoundManager.Instance == null || !SoundManager.Instance.IsMuted;
            var toggleBtn = CreateButton(_settingsPanel.transform, _soundOn ? "ON" : "OFF",
                new Vector2(0.7f, 0.55f), _soundOn ? new Color(0.2f, 0.7f, 0.4f) : new Color(0.5f, 0.2f, 0.2f),
                new Vector2(140, 50), 28);

            var toggleImage = toggleBtn.GetComponent<Image>();
            toggleBtn.onClick.AddListener(() =>
            {
                _soundOn = !_soundOn;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.IsMuted = !_soundOn;

                // Update button visual
                _soundToggleLabel.text = _soundOn ? "ON" : "OFF";
                var c = _soundOn ? new Color(0.2f, 0.7f, 0.4f) : new Color(0.5f, 0.2f, 0.2f);
                toggleImage.color = c;
                var colors = toggleBtn.colors;
                colors.normalColor = c;
                colors.highlightedColor = c * 1.3f;
                colors.pressedColor = c * 0.8f;
                toggleBtn.colors = colors;

                if (SoundManager.Instance != null && _soundOn)
                    SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
            });
            _soundToggleLabel = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Close button
            var closeBtn = CreateButton(_settingsPanel.transform, "Close",
                new Vector2(0.5f, 0.15f), new Color(0.3f, 0.3f, 0.4f),
                new Vector2(200, 50), 28);
            closeBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                ShowSettings(false);
            });

            _settingsPanel.SetActive(false);
        }

        private void ShowSettings(bool show)
        {
            _settingsPanel.SetActive(show);
        }

        private void AnimateTitle()
        {
            _titleText.transform.DOScale(1.03f, 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private IEnumerator AnimateBeamLine()
        {
            float hue = 0f;
            while (_canvas != null && _canvas.gameObject.activeInHierarchy)
            {
                hue += Time.deltaTime * 0.15f;
                if (hue > 1f) hue -= 1f;
                _beamLine.color = Color.HSVToRGB(hue, 0.7f, 1f);
                yield return null;
            }
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

        private Button CreateButton(Transform parent, string label, Vector2 anchorPos,
            Color bgColor, Vector2 size, int fontSize)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
