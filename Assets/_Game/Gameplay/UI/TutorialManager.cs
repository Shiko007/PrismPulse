using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismPulse.Core.Board;
using PrismPulse.Gameplay.Audio;

namespace PrismPulse.Gameplay.UI
{
    /// <summary>
    /// Shows contextual tutorial overlays on first play of early levels.
    /// Tap anywhere to advance through steps, then dismisses.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _messageText;
        private TextMeshProUGUI _tapHint;
        private Button _overlayButton;

        private TutorialStep[] _currentSteps;
        private int _currentStepIndex;
        private bool _active;

        public System.Action OnTutorialComplete;

        private struct TutorialStep
        {
            public string Message;
            public GridPosition? HighlightTile;

            public TutorialStep(string message, GridPosition? highlight = null)
            {
                Message = message;
                HighlightTile = highlight;
            }
        }

        private static readonly Dictionary<int, TutorialStep[]> TutorialData = new Dictionary<int, TutorialStep[]>
        {
            {
                0, new[]
                {
                    new TutorialStep("Tap a tile to rotate it", new GridPosition(1, 0)),
                    new TutorialStep("Guide the beam from the\nsource to the target")
                }
            },
            {
                1, new[]
                {
                    new TutorialStep("Different colored beams\ncan cross without mixing")
                }
            },
            {
                2, new[]
                {
                    new TutorialStep("Beams combine colors!\nRed + Blue = Purple")
                }
            },
            {
                3, new[]
                {
                    new TutorialStep("Splitter tiles divide\na beam into two paths"),
                    new TutorialStep("Locked tiles cannot\nbe rotated")
                }
            },
            {
                4, new[]
                {
                    new TutorialStep("Dark tiles block all light\nexcept their activation color")
                }
            },
        };

        public void Initialize()
        {
            BuildUI();
            _canvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Show tutorial for this level if it exists and hasn't been seen.
        /// Returns true if a tutorial was shown.
        /// </summary>
        public bool TryShowTutorial(int levelIndex)
        {
            if (!TutorialData.ContainsKey(levelIndex)) return false;
            if (HasSeenTutorial(levelIndex)) return false;

            _currentSteps = TutorialData[levelIndex];
            _currentStepIndex = 0;
            _active = true;

            _canvas.gameObject.SetActive(true);
            ShowStep(_currentSteps[0]);
            StartCoroutine(FadeIn(0.25f));

            MarkTutorialSeen(levelIndex);
            return true;
        }

        public bool IsActive => _active;

        private void ShowStep(TutorialStep step)
        {
            _messageText.text = step.Message;
        }

        private void Advance()
        {
            _currentStepIndex++;
            if (_currentStepIndex < _currentSteps.Length)
            {
                ShowStep(_currentSteps[_currentStepIndex]);
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            }
            else
            {
                Hide();
            }
        }

        private void Hide()
        {
            _active = false;
            _canvas.gameObject.SetActive(false);
            OnTutorialComplete?.Invoke();
        }

        private static bool HasSeenTutorial(int levelIndex)
        {
            return PlayerPrefs.GetInt($"tutorial_seen_{levelIndex}", 0) == 1;
        }

        private static void MarkTutorialSeen(int levelIndex)
        {
            PlayerPrefs.SetInt($"tutorial_seen_{levelIndex}", 1);
            PlayerPrefs.Save();
        }

        private void BuildUI()
        {
            // Canvas
            var canvasGO = new GameObject("Tutorial Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 15;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Dark overlay (full screen, blocks input)
            var overlayGO = new GameObject("Overlay");
            overlayGO.transform.SetParent(canvasGO.transform, false);
            var overlayRect = overlayGO.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.6f);

            // Tap-anywhere button (covers entire screen)
            _overlayButton = overlayGO.AddComponent<Button>();
            var btnColors = _overlayButton.colors;
            btnColors.highlightedColor = new Color(0f, 0f, 0f, 0.6f);
            btnColors.pressedColor = new Color(0f, 0f, 0f, 0.5f);
            _overlayButton.colors = btnColors;
            _overlayButton.onClick.AddListener(() =>
            {
                HapticFeedback.LightTap();
                Advance();
            });

            _canvasGroup = overlayGO.AddComponent<CanvasGroup>();

            // Safe area
            var safeAreaGO = new GameObject("SafeArea");
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            var safeRect = safeAreaGO.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            ApplySafeArea(safeRect);

            // Message text — center of screen
            var msgGO = new GameObject("Message");
            msgGO.transform.SetParent(safeAreaGO.transform, false);
            var msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.4f);
            msgRect.anchorMax = new Vector2(0.9f, 0.6f);
            msgRect.sizeDelta = Vector2.zero;

            _messageText = msgGO.AddComponent<TextMeshProUGUI>();
            _messageText.text = "";
            _messageText.fontSize = 42;
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.color = Color.white;
            _messageText.enableWordWrapping = true;
            _messageText.raycastTarget = false;

            // "Tap to continue" hint — bottom
            var hintGO = new GameObject("TapHint");
            hintGO.transform.SetParent(safeAreaGO.transform, false);
            var hintRect = hintGO.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.2f, 0.08f);
            hintRect.anchorMax = new Vector2(0.8f, 0.15f);
            hintRect.sizeDelta = Vector2.zero;

            _tapHint = hintGO.AddComponent<TextMeshProUGUI>();
            _tapHint.text = "Tap to continue";
            _tapHint.fontSize = 26;
            _tapHint.alignment = TextAlignmentOptions.Center;
            _tapHint.color = new Color(0.5f, 0.5f, 0.6f);
            _tapHint.fontStyle = FontStyles.Italic;
            _tapHint.raycastTarget = false;
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 0f;
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
    }
}
