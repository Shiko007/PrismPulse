using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Drop this on an empty GameObject in the scene to auto-setup
    /// all required objects for testing. Creates camera, board, beams, and lighting.
    /// Delete this once proper scene setup is in place.
    /// </summary>
    public class BootstrapScene : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 120; // unlock from 30fps default on mobile
            SetupCamera();
            var tileMaterial = CreateTileMaterial();
            var beamMaterial = CreateBeamMaterial();
            var indicatorMaterial = CreateBeamMaterial(); // unlit, glows with bloom
            var tilePrefab = TilePrefabBuilder.CreateDefaultTilePrefab(tileMaterial, indicatorMaterial);

            // Board View
            var boardGO = new GameObject("BoardView");
            var boardView = boardGO.AddComponent<BoardView.BoardView>();
            SetSerializedField(boardView, "_tilePrefab",
                tilePrefab.GetComponent<BoardView.TileView>());
            SetSerializedField(boardView, "_tileMaterial", tileMaterial);

            // Beam Renderer
            var beamGO = new GameObject("BeamRenderer");
            var beamRenderer = beamGO.AddComponent<BeamRenderer.BeamRenderer>();
            SetSerializedField(beamRenderer, "_beamMaterial", beamMaterial);
            SetSerializedField(beamRenderer, "_boardView", boardView);

            // HUD
            var hudGO = new GameObject("GameHUD");
            var hud = hudGO.AddComponent<UI.GameHUD>();

            // Win Screen
            var winGO = new GameObject("WinScreen");
            var winScreen = winGO.AddComponent<UI.WinScreen>();

            // Main Menu
            var menuGO = new GameObject("MainMenu");
            var mainMenu = menuGO.AddComponent<UI.MainMenu>();

            // Level Select
            var levelSelectGO = new GameObject("LevelSelect");
            var levelSelect = levelSelectGO.AddComponent<UI.LevelSelectScreen>();

            // Tutorial
            var tutorialGO = new GameObject("TutorialManager");
            var tutorial = tutorialGO.AddComponent<UI.TutorialManager>();

            // Game Manager
            var managerGO = new GameObject("GameManager");
            var gameManager = managerGO.AddComponent<GameManager>();
            SetSerializedField(gameManager, "_boardView", boardView);
            SetSerializedField(gameManager, "_beamRenderer", beamRenderer);
            SetSerializedField(gameManager, "_hud", hud);
            SetSerializedField(gameManager, "_winScreen", winScreen);
            SetSerializedField(gameManager, "_mainMenu", mainMenu);
            SetSerializedField(gameManager, "_levelSelect", levelSelect);
            SetSerializedField(gameManager, "_tutorial", tutorial);

            // Sound Manager
            var soundGO = new GameObject("SoundManager");
            soundGO.AddComponent<Audio.SoundManager>();

            // EventSystem — required for UI button clicks
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<InputSystemUIInputModule>();
            }

            // Post-processing volume for bloom
            SetupBloom();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Ensure AudioListener for sound playback
            if (cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();

            // Ensure URP camera data
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
        }

        private void SetupBloom()
        {
            var volumeGO = new GameObject("PostProcess Volume");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(1.5f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(0.8f, 0.85f, 1f));

            volume.profile = profile;
        }

        private Material CreateTileMaterial()
        {
            // Use Unlit — same as beams. Simpler, no lighting dependency,
            // and SRP Batcher reliably picks up color changes on iOS/Metal.
            // HDR colors (>1) trigger bloom for glow effects.
            var shader = FindShader(
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default");

            var mat = new Material(shader);
            return mat;
        }

        private Material CreateBeamMaterial()
        {
            var shader = FindShader(
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default");

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", Color.white * 3f);
            return mat;
        }

        private static Shader FindShader(params string[] names)
        {
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader != null) return shader;
            }
            // Last resort — always exists
            Debug.LogWarning("[BootstrapScene] No preferred shader found, using built-in fallback.");
            return Shader.Find("Hidden/InternalErrorShader");
        }

        /// <summary>
        /// Set a private [SerializeField] at runtime via reflection.
        /// Only used for bootstrap — proper setup uses Inspector or DI.
        /// </summary>
        private static void SetSerializedField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"Field '{fieldName}' not found on {type.Name}");
        }
    }
}
