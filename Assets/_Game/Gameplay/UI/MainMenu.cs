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
            panelRect.anchorMin = new Vector2(0.05f, 0.2f);
            panelRect.anchorMax = new Vector2(0.95f, 0.8f);
            panelRect.sizeDelta = Vector2.zero;
            var panelImage = _settingsPanel.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.06f, 0.12f, 0.95f);

            // Title
            var title = CreateText(_settingsPanel.transform, "SettingsTitle", "Settings",
                new Vector2(0.5f, 0.92f), 42, TextAlignmentOptions.Center);
            title.color = new Color(0.7f, 0.8f, 1f);

            // --- Volume ---
            CreateText(_settingsPanel.transform, "VolumeLabel", "Volume",
                new Vector2(0.25f, 0.78f), 30, TextAlignmentOptions.Center);

            float currentVolume = SoundManager.Instance != null ? SoundManager.Instance.Volume : 0.7f;
            var volumeSlider = CreateSlider(_settingsPanel.transform, "VolumeSlider",
                new Vector2(0.65f, 0.78f), new Vector2(320, 40), currentVolume);
            volumeSlider.onValueChanged.AddListener(v =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.Volume = v;
            });

            // --- Sound toggle ---
            CreateText(_settingsPanel.transform, "SoundLabel", "Sound",
                new Vector2(0.25f, 0.63f), 30, TextAlignmentOptions.Center);

            _soundOn = SoundManager.Instance == null || !SoundManager.Instance.IsMuted;
            var soundBtn = CreateToggle(_settingsPanel.transform, "SoundToggle",
                new Vector2(0.65f, 0.63f), _soundOn);
            var soundBtnImage = soundBtn.GetComponent<Image>();
            _soundToggleLabel = soundBtn.GetComponentInChildren<TextMeshProUGUI>();
            soundBtn.onClick.AddListener(() =>
            {
                _soundOn = !_soundOn;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.IsMuted = !_soundOn;
                UpdateToggleVisual(soundBtn, soundBtnImage, _soundToggleLabel, _soundOn);
                if (SoundManager.Instance != null && _soundOn)
                    SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
            });

            // --- Haptics toggle ---
            CreateText(_settingsPanel.transform, "HapticsLabel", "Haptics",
                new Vector2(0.25f, 0.48f), 30, TextAlignmentOptions.Center);

            bool hapticsOn = HapticFeedback.Enabled;
            var hapticsBtn = CreateToggle(_settingsPanel.transform, "HapticsToggle",
                new Vector2(0.65f, 0.48f), hapticsOn);
            var hapticsBtnImage = hapticsBtn.GetComponent<Image>();
            var hapticsLabel = hapticsBtn.GetComponentInChildren<TextMeshProUGUI>();
            hapticsBtn.onClick.AddListener(() =>
            {
                hapticsOn = !hapticsOn;
                HapticFeedback.Enabled = hapticsOn;
                UpdateToggleVisual(hapticsBtn, hapticsBtnImage, hapticsLabel, hapticsOn);
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                if (hapticsOn) HapticFeedback.LightTap();
            });

            // --- Color-blind mode toggle ---
            CreateText(_settingsPanel.transform, "CbLabel", "Color-Blind",
                new Vector2(0.25f, 0.33f), 30, TextAlignmentOptions.Center);

            bool cbOn = LightColorMap.ColorBlindMode;
            var cbBtn = CreateToggle(_settingsPanel.transform, "CbToggle",
                new Vector2(0.65f, 0.33f), cbOn);
            var cbBtnImage = cbBtn.GetComponent<Image>();
            var cbLabel = cbBtn.GetComponentInChildren<TextMeshProUGUI>();
            cbBtn.onClick.AddListener(() =>
            {
                cbOn = !cbOn;
                LightColorMap.ColorBlindMode = cbOn;
                UpdateToggleVisual(cbBtn, cbBtnImage, cbLabel, cbOn);
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
            });

            // Close button
            var closeBtn = CreateButton(_settingsPanel.transform, "Close",
                new Vector2(0.5f, 0.12f), new Color(0.3f, 0.3f, 0.4f),
                new Vector2(200, 50), 28);
            closeBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                ShowSettings(false);
            });

            _settingsPanel.SetActive(false);
        }

        private Button CreateToggle(Transform parent, string name, Vector2 anchorPos, bool isOn)
        {
            var color = isOn ? new Color(0.2f, 0.7f, 0.4f) : new Color(0.5f, 0.2f, 0.2f);
            var btn = CreateButton(parent, isOn ? "ON" : "OFF", anchorPos, color, new Vector2(140, 50), 28);
            return btn;
        }

        private void UpdateToggleVisual(Button btn, Image bg, TextMeshProUGUI label, bool isOn)
        {
            label.text = isOn ? "ON" : "OFF";
            var c = isOn ? new Color(0.2f, 0.7f, 0.4f) : new Color(0.5f, 0.2f, 0.2f);
            bg.color = c;
            var colors = btn.colors;
            colors.normalColor = c;
            colors.highlightedColor = c * 1.3f;
            colors.pressedColor = c * 0.8f;
            btn.colors = colors;
        }

        private Slider CreateSlider(Transform parent, string name, Vector2 anchorPos, Vector2 size, float value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.35f);
            bgRect.anchorMax = new Vector2(1f, 0.65f);
            bgRect.sizeDelta = Vector2.zero;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.6f, 0.9f);

            // Handle slide area
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24, 24);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;

            return slider;
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
