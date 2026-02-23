using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrismPulse.Gameplay.Audio;

namespace PrismPulse.Gameplay.UI
{
    /// <summary>
    /// In-game HUD showing level name, move counter, timer, and par info.
    /// Built programmatically for bootstrap — convert to prefab later.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        private TextMeshProUGUI _levelNameText;
        private TextMeshProUGUI _movesText;
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _parText;
        private Canvas _canvas;

        private float _elapsedTime;
        private int _moveCount;
        private bool _running;
        private int _parMoves;
        private float _parTime;
        private Button _undoButton;
        private Button _hintButton;

        public System.Action OnUndo;
        public System.Action OnHint;

        public void Initialize()
        {
            BuildUI();
        }

        public void SetLevelInfo(string levelName, int parMoves, float parTime)
        {
            _parMoves = parMoves;
            _parTime = parTime;
            _levelNameText.text = levelName;
            _parText.text = $"Par: {parMoves} moves / {parTime:0}s";
            _moveCount = 0;
            _elapsedTime = 0f;
            _running = true;
            UpdateDisplay();
        }

        public void OnMove(int totalMoves)
        {
            _moveCount = totalMoves;
            UpdateDisplay();
        }

        public void Stop()
        {
            _running = false;
        }

        public void Show() { if (_canvas != null) _canvas.gameObject.SetActive(true); }
        public void Hide() { if (_canvas != null) _canvas.gameObject.SetActive(false); }

        public float ElapsedTime => _elapsedTime;
        public int MoveCount => _moveCount;

        public void SetUndoInteractable(bool interactable)
        {
            if (_undoButton != null) _undoButton.interactable = interactable;
        }

        public void SetHintInteractable(bool interactable)
        {
            if (_hintButton != null) _hintButton.interactable = interactable;
        }

        /// <summary>
        /// Returns 1-3 stars based on performance vs par.
        /// </summary>
        public int GetStarRating()
        {
            if (_moveCount <= _parMoves && _elapsedTime <= _parTime) return 3;
            if (_moveCount <= _parMoves * 1.5f) return 2;
            return 1;
        }

        private void Update()
        {
            if (!_running) return;
            _elapsedTime += Time.deltaTime;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _movesText.text = $"Moves: {_moveCount}";
            _timerText.text = FormatTime(_elapsedTime);

            // Color moves text based on par
            if (_parMoves > 0)
            {
                if (_moveCount <= _parMoves)
                    _movesText.color = new Color(0.3f, 1f, 0.5f); // green — on par
                else
                    _movesText.color = new Color(1f, 0.5f, 0.3f); // orange — over par
            }
        }

        private string FormatTime(float seconds)
        {
            int mins = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            if (mins > 0)
                return $"{mins}:{secs:D2}";
            return $"{secs}s";
        }

        private void BuildUI()
        {
            // Canvas
            var canvasGO = new GameObject("HUD Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Safe area container — respects notch/Dynamic Island/rounded corners
            var safeAreaGO = new GameObject("SafeArea");
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            var safeRect = safeAreaGO.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            ApplySafeArea(safeRect, canvasGO.GetComponent<RectTransform>());

            // Level name — top center
            _levelNameText = CreateText(safeRect, "LevelName", "",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20f),
                fontSize: 42, alignment: TextAlignmentOptions.Center);
            _levelNameText.color = new Color(0.7f, 0.8f, 1f);

            // Par info — below level name
            _parText = CreateText(safeRect, "Par", "",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -65f),
                fontSize: 24, alignment: TextAlignmentOptions.Center);
            _parText.color = new Color(0.5f, 0.5f, 0.6f);

            // Moves counter — top left
            _movesText = CreateText(safeRect, "Moves", "Moves: 0",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f),
                fontSize: 32, alignment: TextAlignmentOptions.Left);
            _movesText.color = Color.white;

            // Timer — top right
            _timerText = CreateText(safeRect, "Timer", "0s",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f),
                fontSize: 32, alignment: TextAlignmentOptions.Right);
            _timerText.color = Color.white;

            // Undo button — bottom left
            _undoButton = CreateButton(safeRect, "Undo",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f),
                new Vector2(160, 60), new Color(0.25f, 0.25f, 0.35f), 26);
            _undoButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnUndo?.Invoke();
            });
            _undoButton.interactable = false;

            // Hint button — bottom right
            _hintButton = CreateButton(safeRect, "Hint",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f),
                new Vector2(160, 60), new Color(0.2f, 0.3f, 0.45f), 26);
            _hintButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticFeedback.LightTap();
                OnHint?.Invoke();
            });
        }

        private static void ApplySafeArea(RectTransform rect, RectTransform canvasRect)
        {
            var safeArea = Screen.safeArea;
            var canvasSize = canvasRect.sizeDelta;

            // If canvas size is zero (ScreenSpaceOverlay), use screen dimensions
            if (canvasSize.x <= 0) canvasSize.x = Screen.width;
            if (canvasSize.y <= 0) canvasSize.y = Screen.height;

            var anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
            var anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
            int fontSize = 32, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
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
            tmp.enableAutoSizing = false;

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
            colors.disabledColor = bgColor * 0.4f;
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
