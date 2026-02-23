using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PrismPulse.Core.Puzzle;
using PrismPulse.Gameplay.Audio;
using PrismPulse.Gameplay.Progress;

namespace PrismPulse.Gameplay.UI
{
    /// <summary>
    /// Scrollable grid of level buttons with star ratings and lock states.
    /// </summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _root;
        private CanvasGroup _canvasGroup;

        private LevelDefinition[] _levels;
        private readonly List<LevelCell> _cells = new List<LevelCell>();

        public System.Action<int> OnLevelSelected;
        public System.Action OnBack;

        public void Initialize(LevelDefinition[] levels)
        {
            _levels = levels;
            BuildUI();
            _canvas.gameObject.SetActive(false);
        }

        public void Show()
        {
            RefreshProgress();
            _canvas.gameObject.SetActive(true);
            _root.SetActive(true);

            _canvasGroup.alpha = 0f;
            _root.transform.localScale = Vector3.one * 0.8f;
            _root.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            StartCoroutine(FadeIn(0.3f));
        }

        public void Hide()
        {
            _canvas.gameObject.SetActive(false);
        }

        public void RefreshProgress()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                int stars = ProgressManager.GetStars(_levels[i].Id);
                bool unlocked = ProgressManager.IsUnlocked(i, i > 0 ? _levels[i - 1].Id : "");

                cell.SetState(unlocked, stars);
            }
        }

        private void BuildUI()
        {
            // Canvas
            var canvasGO = new GameObject("LevelSelect Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 8;

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

            // Root
            _root = new GameObject("Root");
            _root.transform.SetParent(safeAreaGO.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            _canvasGroup = _root.AddComponent<CanvasGroup>();

            // Header area
            BuildHeader(_root.transform);

            // Scrollable grid
            BuildGrid(_root.transform);
        }

        private void BuildHeader(Transform parent)
        {
            // Title
            var title = CreateText(parent, "Title", "SELECT LEVEL",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -45f),
                38, TextAlignmentOptions.Center);
            title.color = new Color(0.7f, 0.8f, 1f);

            // Back button
            var backBtn = CreateButton(parent, "<  Back",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -45f),
                new Vector2(160, 50), new Color(0.3f, 0.3f, 0.4f), 24);
            backBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnBack?.Invoke();
            });
        }

        private void BuildGrid(Transform parent)
        {
            // Scroll area (below header)
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(parent, false);
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(20f, 20f);   // bottom + left padding
            scrollRect.offsetMax = new Vector2(-20f, -140f);  // top padding for header

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; // stop at edges, no bounce
            scroll.scrollSensitivity = 40f;

            // RectMask2D clips children to scroll area (no Image required)
            scrollGO.AddComponent<RectMask2D>();

            // Content container with GridLayout
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
            contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);

            var grid = contentGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220, 220);
            grid.spacing = new Vector2(40, 40);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(20, 20, 10, 10);

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            // Create level cells
            for (int i = 0; i < _levels.Length; i++)
            {
                var cell = CreateLevelCell(contentGO.transform, i, _levels[i]);
                _cells.Add(cell);
            }
        }

        private LevelCell CreateLevelCell(Transform parent, int index, LevelDefinition level)
        {
            var go = new GameObject($"Level_{level.Id}");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            var btn = go.AddComponent<Button>();

            // Number text (large)
            var numberGO = new GameObject("Number");
            numberGO.transform.SetParent(go.transform, false);
            var numRect = numberGO.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0.35f);
            numRect.anchorMax = new Vector2(1f, 0.9f);
            numRect.sizeDelta = Vector2.zero;
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;
            var numText = numberGO.AddComponent<TextMeshProUGUI>();
            numText.text = level.Id;
            numText.fontSize = 48;
            numText.alignment = TextAlignmentOptions.Center;
            numText.color = Color.white;

            // Name text (small)
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(go.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.15f);
            nameRect.anchorMax = new Vector2(1f, 0.4f);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = level.Name;
            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = new Color(0.6f, 0.6f, 0.7f);

            // Stars text
            var starsGO = new GameObject("Stars");
            starsGO.transform.SetParent(go.transform, false);
            var starsRect = starsGO.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0f, 0f);
            starsRect.anchorMax = new Vector2(1f, 0.2f);
            starsRect.sizeDelta = Vector2.zero;
            starsRect.offsetMin = Vector2.zero;
            starsRect.offsetMax = Vector2.zero;
            var starsText = starsGO.AddComponent<TextMeshProUGUI>();
            starsText.text = "---";
            starsText.fontSize = 26;
            starsText.alignment = TextAlignmentOptions.Center;
            starsText.color = new Color(0.4f, 0.4f, 0.4f);

            int levelIndex = index; // capture for closure
            btn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnLevelSelected?.Invoke(levelIndex);
            });

            return new LevelCell(image, btn, numText, nameText, starsText);
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
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 60);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            return tmp;
        }

        private Button CreateButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            Vector2 size, Color bgColor, int fontSize)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

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

        /// <summary>
        /// Tracks references for a single level cell to update state.
        /// </summary>
        private class LevelCell
        {
            private readonly Image _bg;
            private readonly Button _button;
            private readonly TextMeshProUGUI _number;
            private readonly TextMeshProUGUI _name;
            private readonly TextMeshProUGUI _stars;

            private static readonly Color UnlockedBg = new Color(0.12f, 0.14f, 0.22f, 1f);
            private static readonly Color LockedBg = new Color(0.06f, 0.06f, 0.08f, 1f);

            public LevelCell(Image bg, Button button,
                TextMeshProUGUI number, TextMeshProUGUI name, TextMeshProUGUI stars)
            {
                _bg = bg;
                _button = button;
                _number = number;
                _name = name;
                _stars = stars;
            }

            public void SetState(bool unlocked, int stars)
            {
                _button.interactable = unlocked;
                _bg.color = unlocked ? UnlockedBg : LockedBg;
                _number.color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f);
                _name.color = unlocked ? new Color(0.6f, 0.6f, 0.7f) : new Color(0.25f, 0.25f, 0.3f);

                if (!unlocked)
                {
                    _stars.text = "";
                    return;
                }

                if (stars <= 0)
                {
                    _stars.text = "---";
                    _stars.color = new Color(0.4f, 0.4f, 0.4f);
                }
                else
                {
                    _stars.text = new string('*', stars) + new string('-', 3 - stars);
                    _stars.color = stars == 3
                        ? new Color(1f, 0.85f, 0.2f)    // gold
                        : new Color(0.8f, 0.8f, 0.8f);  // silver
                }
            }
        }
    }
}
