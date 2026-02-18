// ============================================================================
// CRITICAL: Master the Atom - Scene Bridge
// SceneBridge.cs - Scene Management Controller for Operator/Validator Views
// ============================================================================
//
// PURPOSE:
//   Manages switching between the Operator Screens view (MainScene) and
//   the Heatup Validation Dashboard (Validator scene loaded additively).
//   Ensures the HeatupSimEngine runs continuously regardless of which
//   view is active.
//
// ARCHITECTURE:
//   - Lives on a DontDestroyOnLoad GameObject alongside HeatupSimEngine
//   - MainScene is always loaded (primary scene, build index 0)
//   - Validator.unity is loaded/unloaded additively on demand
//   - HeatupSimEngine persists via DontDestroyOnLoad (never destroyed)
//   - HeatupValidationVisual finds the persistent engine via FindObjectOfType
//
// KEYBOARD:
//   V key      â†’ Load Validator overlay, hide operator Canvas
//   1-8/Tab    â†’ Unload Validator, show operator Canvas (ScreenManager handles screen selection)
//   Esc        â†’ If Validator active: return to operator screens
//                 If operator screens active: no action (reserved for future)
//   F2         â†’ Toggle validator scenario selector overlay (routed via SceneBridge)
//   F1         â†’ Toggle Validator dashboard visibility (handled by HeatupValidationVisual)
//   F5-F9      â†’ Time acceleration (handled by HeatupValidationVisual)
//
// STATE MACHINE:
//   [OperatorScreens]  â”€â”€ V key â”€â”€â–º  [Validator]
//     (Canvas visible)                  (Canvas hidden, OnGUI overlay)
//     (Engine running)                  (Engine running)
//          â–²                                  â”‚
//          â””â”€â”€â”€â”€ 1-8 / Tab / Esc â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// VERSION: 2.0.11
// DATE: 2026-02-10
// CLASSIFICATION: Core â€” Scene Infrastructure
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using Critical.Validation;
namespace Critical.Core
{
    /// <summary>
    /// Manages scene transitions between Operator Screens and Validator Dashboard.
    /// Ensures HeatupSimEngine persistence across all scene operations.
    /// </summary>
    public class SceneBridge : MonoBehaviour
    {
        // ====================================================================
        // ENUMS
        // ====================================================================

        /// <summary>
        /// Which view is currently active.
        /// </summary>
        public enum ActiveView
        {
            OperatorScreens,
            Validator
        }

        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Scene Names")]
        [Tooltip("Name of the Validator scene (must be in Build Settings)")]
        [SerializeField] private string validatorSceneName = "Validator";

        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;

        // ====================================================================
        // PUBLIC STATE
        // ====================================================================

        /// <summary>Current active view.</summary>
        public ActiveView CurrentView { get; private set; } = ActiveView.OperatorScreens;

        /// <summary>Is the Validator scene currently loaded?</summary>
        public bool IsValidatorLoaded { get; private set; } = false;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        /// <summary>Singleton instance.</summary>
        private static SceneBridge _instance;

        /// <summary>Cached reference to the operator screens Canvas GameObject.</summary>
        private GameObject _operatorCanvas;

        /// <summary>True while an async scene operation is in progress.</summary>
        private bool _sceneTransitionInProgress = false;

        /// <summary>Reference to the persistent HeatupSimEngine.</summary>
        private HeatupSimEngine _engine;

        /// <summary>Cached validator dashboard component for routed input actions.</summary>
        private HeatupValidationVisual _validatorVisual;

        /// <summary>
        /// True when F2 was pressed before validator availability and the selector
        /// should be opened immediately after validator load completes.
        /// </summary>
        private bool _openScenarioSelectorOnValidatorReady;

        // ====================================================================
        // SINGLETON ACCESS
        // ====================================================================

        /// <summary>Global singleton instance.</summary>
        public static SceneBridge Instance => _instance;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                if (debugLogging)
                    Debug.Log("[SceneBridge] Duplicate found â€” destroying this instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure the HeatupSimEngine on this (or sibling) GameObject also persists
            EnsureEnginePersistence();

            if (debugLogging)
                Debug.Log("[SceneBridge] Initialized â€” DontDestroyOnLoad active");
        }

        void Start()
        {
            // Cache the operator Canvas reference
            FindOperatorCanvas();

            // Find engine reference
            if (_engine == null)
                _engine = FindObjectOfType<HeatupSimEngine>();

            if (_engine == null)
                Debug.LogWarning("[SceneBridge] No HeatupSimEngine found in scene! " +
                                 "Validator dashboard will have no data.");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Don't process input during scene transitions
            if (_sceneTransitionInProgress) return;

            switch (CurrentView)
            {
                case ActiveView.OperatorScreens:
                    // F2 from operator view should still open the scenario selector.
                    if (kb.f2Key.wasPressedThisFrame)
                    {
                        RequestScenarioSelectorFromAnyView();
                    }
                    // V key â†’ switch to Validator
                    else if (kb.vKey.wasPressedThisFrame)
                    {
                        SwitchToValidator();
                    }
                    break;

                case ActiveView.Validator:
                    // F2 â†’ toggle validator scenario selector overlay
                    if (kb.f2Key.wasPressedThisFrame)
                    {
                        ToggleValidatorScenarioSelector();
                    }
                    // Any screen key â†’ return to operator screens
                    else if (IsScreenKeyPressed(kb))
                    {
                        SwitchToOperatorScreens();
                    }
                    // Esc â†’ return to operator screens
                    else if (kb.escapeKey.wasPressedThisFrame)
                    {
                        SwitchToOperatorScreens();
                    }
                    break;
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ====================================================================
        // VIEW SWITCHING
        // ====================================================================

        /// <summary>
        /// Switch to Validator dashboard view.
        /// Loads Validator scene additively and hides operator Canvas.
        /// </summary>
        public void SwitchToValidator()
        {
            if (CurrentView == ActiveView.Validator || _sceneTransitionInProgress)
                return;

            if (debugLogging)
                Debug.Log("[SceneBridge] Switching to Validator view...");

            _sceneTransitionInProgress = true;

            // Hide operator Canvas
            SetOperatorCanvasVisible(false);

            // Load Validator additively
            if (!IsValidatorLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(
                    validatorSceneName, LoadSceneMode.Additive);

                if (loadOp == null)
                {
                    Debug.LogError($"[SceneBridge] Failed to load scene '{validatorSceneName}'! " +
                                   "Is it in Build Settings?");
                    SetOperatorCanvasVisible(true); // Restore Canvas
                    _sceneTransitionInProgress = false;
                    _openScenarioSelectorOnValidatorReady = false;
                    return;
                }

                loadOp.completed += (op) =>
                {
                    IsValidatorLoaded = true;
                    CurrentView = ActiveView.Validator;
                    _sceneTransitionInProgress = false;
                    _validatorVisual = FindObjectOfType<HeatupValidationVisual>();

                    // Re-resolve ScreenDataBridge sources now that both scenes are loaded
                    ResolveDataBridgeSources();
                    ProcessPendingScenarioSelectorOpen();

                    if (debugLogging)
                        Debug.Log("[SceneBridge] Validator loaded â€” view active");
                };
            }
            else
            {
                // Validator already loaded (shouldn't normally happen)
                CurrentView = ActiveView.Validator;
                _sceneTransitionInProgress = false;
                ProcessPendingScenarioSelectorOpen();

                if (debugLogging)
                    Debug.Log("[SceneBridge] Validator already loaded â€” view active");
            }
        }

        /// <summary>
        /// Switch to Operator Screens view.
        /// Unloads Validator scene and shows operator Canvas.
        /// </summary>
        public void SwitchToOperatorScreens()
        {
            if (CurrentView == ActiveView.OperatorScreens || _sceneTransitionInProgress)
                return;

            if (debugLogging)
                Debug.Log("[SceneBridge] Switching to Operator Screens view...");

            _sceneTransitionInProgress = true;

            // Show operator Canvas
            SetOperatorCanvasVisible(true);

            // Unload Validator
            if (IsValidatorLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(validatorSceneName);

                if (unloadOp == null)
                {
                    // Scene may not be loaded â€” just update state
                    IsValidatorLoaded = false;
                    CurrentView = ActiveView.OperatorScreens;
                    _sceneTransitionInProgress = false;

                    if (debugLogging)
                        Debug.Log("[SceneBridge] Validator was not loaded â€” Operator Screens active");
                    return;
                }

                unloadOp.completed += (op) =>
                {
                    IsValidatorLoaded = false;
                    CurrentView = ActiveView.OperatorScreens;
                    _sceneTransitionInProgress = false;
                    _validatorVisual = null;

                    if (debugLogging)
                        Debug.Log("[SceneBridge] Validator unloaded â€” Operator Screens active");
                };
            }
            else
            {
                CurrentView = ActiveView.OperatorScreens;
                _sceneTransitionInProgress = false;
                _validatorVisual = null;
            }
        }

        // ====================================================================
        // CANVAS MANAGEMENT
        // ====================================================================

        /// <summary>
        /// Find the OperatorScreensCanvas in the scene.
        /// Called once on Start and cached.
        /// </summary>
        private void FindOperatorCanvas()
        {
            if (_operatorCanvas != null) return;

            // Search by name (created by MultiScreenBuilder)
            GameObject found = GameObject.Find("OperatorScreensCanvas");
            if (found != null)
            {
                _operatorCanvas = found;
                if (debugLogging)
                    Debug.Log("[SceneBridge] Found OperatorScreensCanvas");
                return;
            }

            // Fallback: find any Canvas with ScreenManager as sibling
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    _operatorCanvas = canvas.gameObject;
                    if (debugLogging)
                        Debug.Log($"[SceneBridge] Using Canvas '{canvas.name}' as operator Canvas (fallback)");
                    return;
                }
            }

            Debug.LogWarning("[SceneBridge] No OperatorScreensCanvas found! " +
                             "View switching will not hide/show operator screens.");
        }

        /// <summary>
        /// Show or hide the operator screens Canvas.
        /// </summary>
        private void SetOperatorCanvasVisible(bool visible)
        {
            if (_operatorCanvas == null)
                FindOperatorCanvas();

            if (_operatorCanvas != null)
            {
                _operatorCanvas.SetActive(visible);

                if (debugLogging)
                    Debug.Log($"[SceneBridge] Operator Canvas {(visible ? "SHOWN" : "HIDDEN")}");
            }
        }

        // ====================================================================
        // ENGINE PERSISTENCE
        // ====================================================================

        /// <summary>
        /// Ensure the HeatupSimEngine in this scene gets DontDestroyOnLoad.
        /// If the engine is on a different GameObject, parent it or mark it.
        /// </summary>
        private void EnsureEnginePersistence()
        {
            // Check if engine is on this same GameObject
            _engine = GetComponent<HeatupSimEngine>();

            if (_engine != null)
            {
                // Engine is on our GameObject â€” already covered by our DontDestroyOnLoad
                if (debugLogging)
                    Debug.Log("[SceneBridge] HeatupSimEngine on same GameObject â€” persistence inherited");
                return;
            }

            // Search scene for engine
            _engine = FindObjectOfType<HeatupSimEngine>();

            if (_engine != null)
            {
                // Mark the engine's GameObject as persistent
                DontDestroyOnLoad(_engine.gameObject);

                if (debugLogging)
                    Debug.Log($"[SceneBridge] HeatupSimEngine on '{_engine.gameObject.name}' â€” marked DontDestroyOnLoad");
            }
        }

        // ====================================================================
        // DATA BRIDGE RE-RESOLUTION
        // ====================================================================

        /// <summary>
        /// Trigger ScreenDataBridge to re-discover data sources.
        /// Called after scene loads/unloads to ensure bridge finds the
        /// persistent HeatupSimEngine.
        /// </summary>
        private void ResolveDataBridgeSources()
        {
            var bridge = UI.ScreenDataBridge.Instance;
            if (bridge != null)
            {
                bridge.ResolveSources();

                if (debugLogging)
                    Debug.Log("[SceneBridge] ScreenDataBridge sources re-resolved");
            }
        }

        // ====================================================================
        // INPUT HELPERS
        // ====================================================================

        /// <summary>
        /// Check if any screen switching key was pressed this frame.
        /// Keys 1-8 and Tab (matching ScreenManager's OperatorScreens action map).
        /// </summary>
        private bool IsScreenKeyPressed(Keyboard kb)
        {
            return kb.digit1Key.wasPressedThisFrame ||
                   kb.digit2Key.wasPressedThisFrame ||
                   kb.digit3Key.wasPressedThisFrame ||
                   kb.digit4Key.wasPressedThisFrame ||
                   kb.digit5Key.wasPressedThisFrame ||
                   kb.digit6Key.wasPressedThisFrame ||
                   kb.digit7Key.wasPressedThisFrame ||
                   kb.digit8Key.wasPressedThisFrame ||
                   kb.tabKey.wasPressedThisFrame;
        }

        /// <summary>
        /// Request scenario selector from either view. If validator is not active yet,
        /// queue the selector open and switch views first.
        /// </summary>
        private void RequestScenarioSelectorFromAnyView()
        {
            if (CurrentView == ActiveView.Validator)
            {
                ToggleValidatorScenarioSelector();
                return;
            }

            _openScenarioSelectorOnValidatorReady = true;
            SwitchToValidator();
        }

        /// <summary>
        /// Execute queued scenario-selector open after validator becomes available.
        /// </summary>
        private void ProcessPendingScenarioSelectorOpen()
        {
            if (!_openScenarioSelectorOnValidatorReady)
            {
                return;
            }

            _openScenarioSelectorOnValidatorReady = false;
            ToggleValidatorScenarioSelector();
        }

        /// <summary>
        /// Route F2 selector toggle to the validator dashboard component.
        /// </summary>
        private void ToggleValidatorScenarioSelector()
        {
            if (_validatorVisual == null)
            {
                _validatorVisual = FindObjectOfType<HeatupValidationVisual>();
            }

            if (_validatorVisual == null)
            {
                if (debugLogging)
                    Debug.LogWarning("[SceneBridge] F2 selector toggle ignored: HeatupValidationVisual not found.");
                return;
            }

            _validatorVisual.ToggleScenarioSelector();
        }
    }
}

