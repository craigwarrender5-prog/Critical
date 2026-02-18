// ============================================================================
// CRITICAL: Master the Atom - Scene Bridge
// SceneBridge.cs - Scene Management Controller for Operator/Validation Views
// ============================================================================
//
// PURPOSE:
//   Manages switching between the Operator Screens view (MainScene) and
//   the UI Toolkit Validation Dashboard (Validation scene loaded additively).
//   Ensures the HeatupSimEngine runs continuously regardless of which
//   view is active.
//
// ARCHITECTURE:
//   - Lives on a DontDestroyOnLoad GameObject alongside HeatupSimEngine
//   - MainScene is always loaded (primary scene, build index 0)
//   - Validation.unity is loaded/unloaded additively on demand
//   - HeatupSimEngine persists via DontDestroyOnLoad (never destroyed)
//   - UITKDashboardV2Controller finds the persistent engine via FindObjectOfType
//
// KEYBOARD:
//   V key      -> Load Validation overlay, hide operator Canvas
//   1-8/Tab    -> Unload Validation, show operator Canvas (ScreenManager handles screen selection)
//   Esc        -> If Validation active: return to operator screens
//                 If operator screens active: no action (reserved for future)
//   F2         -> Toggle scenario selector overlay (routed via SceneBridge)
//   F5-F9      -> Time acceleration (handled by UITKDashboardV2Controller)
//
// STATE MACHINE:
//   [OperatorScreens]  -- V key -->  [Validation]
//     (Canvas visible)                 (Canvas hidden, UITK dashboard)
//     (Engine running)                 (Engine running)
//          ^                                  |
//          <---- 1-8 / Tab / Esc -------------+
//
// VERSION: 3.0.0
// DATE: 2026-02-18
// CLASSIFICATION: Core - Scene Infrastructure
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using Critical.Validation;
using Critical.UI.UIToolkit.ValidationDashboard;

namespace Critical.Core
{
    /// <summary>
    /// Manages scene transitions between Operator Screens and Validation Dashboard.
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
            Validation
        }

        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Scene Names")]
        [Tooltip("Name of the Validation scene (must be in Build Settings)")]
        [SerializeField] private string validationSceneName = "Validation";

        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;

        [Header("Audio")]
        [Tooltip("If enabled, SceneBridge enforces exactly one enabled AudioListener at runtime.")]
        [SerializeField] private bool enforceSingleAudioListener = true;

        [Tooltip("How often (seconds) to re-audit AudioListeners while running.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float audioListenerAuditInterval = 0.5f;

        // ====================================================================
        // PUBLIC STATE
        // ====================================================================

        /// <summary>Current active view.</summary>
        public ActiveView CurrentView { get; private set; } = ActiveView.OperatorScreens;

        /// <summary>Is the Validation scene currently loaded?</summary>
        public bool IsValidationLoaded { get; private set; } = false;

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

        /// <summary>Cached V2 dashboard controller for routed input actions.</summary>
        private UITKDashboardV2Controller _dashboardController;

        /// <summary>Timer for periodic AudioListener audits.</summary>
        private float _audioListenerAuditTimer;

        /// <summary>Fallback listener created only if no listener exists in loaded scenes.</summary>
        private AudioListener _fallbackAudioListener;

        /// <summary>
        /// True when F2 was pressed before dashboard availability and the selector
        /// should be opened immediately after scene load completes.
        /// </summary>
        private bool _openScenarioSelectorOnDashboardReady;

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
                    Debug.Log("[SceneBridge] Duplicate found - destroying this instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure the HeatupSimEngine on this (or sibling) GameObject also persists
            EnsureEnginePersistence();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            if (enforceSingleAudioListener)
            {
                _audioListenerAuditTimer = 0f;
                EnforceSingleAudioListener(true);
            }

            if (debugLogging)
                Debug.Log("[SceneBridge] Initialized - DontDestroyOnLoad active");
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
                                 "Validation dashboard will have no data.");
        }

        void Update()
        {
            // Keep listener state sane even while scene transitions are in flight.
            if (enforceSingleAudioListener)
            {
                _audioListenerAuditTimer -= Time.unscaledDeltaTime;
                if (_audioListenerAuditTimer <= 0f)
                {
                    _audioListenerAuditTimer = Mathf.Max(0.1f, audioListenerAuditInterval);
                    EnforceSingleAudioListener(true);
                }
            }

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
                    // V key -> switch to Validation
                    else if (kb.vKey.wasPressedThisFrame)
                    {
                        SwitchToValidation();
                    }
                    break;

                case ActiveView.Validation:
                    // F2 -> toggle scenario selector overlay
                    if (kb.f2Key.wasPressedThisFrame)
                    {
                        ToggleScenarioSelector();
                    }
                    // Any screen key -> return to operator screens
                    else if (IsScreenKeyPressed(kb))
                    {
                        SwitchToOperatorScreens();
                    }
                    // Esc -> return to operator screens
                    else if (kb.escapeKey.wasPressedThisFrame)
                    {
                        SwitchToOperatorScreens();
                    }
                    break;
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            if (_instance == this)
                _instance = null;
        }

        // ====================================================================
        // VIEW SWITCHING
        // ====================================================================

        /// <summary>
        /// Switch to Validation dashboard view.
        /// Loads Validation scene additively and hides operator Canvas.
        /// </summary>
        public void SwitchToValidation()
        {
            if (CurrentView == ActiveView.Validation || _sceneTransitionInProgress)
                return;

            if (debugLogging)
                Debug.Log("[SceneBridge] Switching to Validation view...");

            _sceneTransitionInProgress = true;

            // Hide operator Canvas
            SetOperatorCanvasVisible(false);

            // Load Validation additively
            if (!IsValidationLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(
                    validationSceneName, LoadSceneMode.Additive);

                if (loadOp == null)
                {
                    Debug.LogError($"[SceneBridge] Failed to load scene '{validationSceneName}'! " +
                                   "Is it in Build Settings?");
                    SetOperatorCanvasVisible(true); // Restore Canvas
                    _sceneTransitionInProgress = false;
                    _openScenarioSelectorOnDashboardReady = false;
                    return;
                }

                loadOp.completed += (op) =>
                {
                    IsValidationLoaded = true;
                    CurrentView = ActiveView.Validation;
                    _sceneTransitionInProgress = false;

                    // Find the V2 dashboard controller in the loaded scene
                    _dashboardController = FindObjectOfType<UITKDashboardV2Controller>();

                    if (_dashboardController == null)
                        Debug.LogWarning("[SceneBridge] UITKDashboardV2Controller not found in Validation scene!");

                    // Re-resolve ScreenDataBridge sources now that both scenes are loaded
                    ResolveDataBridgeSources();
                    EnforceSingleAudioListener(true);
                    ProcessPendingScenarioSelectorOpen();

                    if (debugLogging)
                        Debug.Log("[SceneBridge] Validation loaded - view active");
                };
            }
            else
            {
                // Validation already loaded (shouldn't normally happen)
                CurrentView = ActiveView.Validation;
                _sceneTransitionInProgress = false;
                EnforceSingleAudioListener(true);
                ProcessPendingScenarioSelectorOpen();

                if (debugLogging)
                    Debug.Log("[SceneBridge] Validation already loaded - view active");
            }
        }

        /// <summary>
        /// Switch to Operator Screens view.
        /// Unloads Validation scene and shows operator Canvas.
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

            // Unload Validation
            if (IsValidationLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(validationSceneName);

                if (unloadOp == null)
                {
                    // Scene may not be loaded - just update state
                    IsValidationLoaded = false;
                    CurrentView = ActiveView.OperatorScreens;
                    _sceneTransitionInProgress = false;

                    if (debugLogging)
                        Debug.Log("[SceneBridge] Validation was not loaded - Operator Screens active");
                    return;
                }

                unloadOp.completed += (op) =>
                {
                    IsValidationLoaded = false;
                    CurrentView = ActiveView.OperatorScreens;
                    _sceneTransitionInProgress = false;
                    _dashboardController = null;
                    EnforceSingleAudioListener(true);

                    if (debugLogging)
                        Debug.Log("[SceneBridge] Validation unloaded - Operator Screens active");
                };
            }
            else
            {
                CurrentView = ActiveView.OperatorScreens;
                _sceneTransitionInProgress = false;
                _dashboardController = null;
                EnforceSingleAudioListener(true);
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
                // Engine is on our GameObject - already covered by our DontDestroyOnLoad
                if (debugLogging)
                    Debug.Log("[SceneBridge] HeatupSimEngine on same GameObject - persistence inherited");
                return;
            }

            // Search scene for engine
            _engine = FindObjectOfType<HeatupSimEngine>();

            if (_engine != null)
            {
                // Mark the engine's GameObject as persistent
                DontDestroyOnLoad(_engine.gameObject);

                if (debugLogging)
                    Debug.Log($"[SceneBridge] HeatupSimEngine on '{_engine.gameObject.name}' - marked DontDestroyOnLoad");
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
        /// Request scenario selector from either view. If dashboard is not active yet,
        /// queue the selector open and switch views first.
        /// </summary>
        private void RequestScenarioSelectorFromAnyView()
        {
            if (CurrentView == ActiveView.Validation)
            {
                ToggleScenarioSelector();
                return;
            }

            _openScenarioSelectorOnDashboardReady = true;
            SwitchToValidation();
        }

        /// <summary>
        /// Execute queued scenario-selector open after dashboard becomes available.
        /// </summary>
        private void ProcessPendingScenarioSelectorOpen()
        {
            if (!_openScenarioSelectorOnDashboardReady)
            {
                return;
            }

            _openScenarioSelectorOnDashboardReady = false;
            ToggleScenarioSelector();
        }

        /// <summary>
        /// Route F2 selector toggle to the V2 dashboard controller.
        /// </summary>
        private void ToggleScenarioSelector()
        {
            if (_dashboardController == null)
            {
                _dashboardController = FindObjectOfType<UITKDashboardV2Controller>();
            }

            if (_dashboardController == null)
            {
                if (debugLogging)
                    Debug.LogWarning("[SceneBridge] F2 selector toggle ignored: UITKDashboardV2Controller not found.");
                return;
            }

            _dashboardController.ToggleScenarioSelector();
        }

        // ====================================================================
        // AUDIO LISTENER ARBITRATION
        // ====================================================================

        /// <summary>
        /// Scene callbacks used to immediately reconcile listener state after loads/unloads.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnforceSingleAudioListener(true);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            EnforceSingleAudioListener(true);
        }

        /// <summary>
        /// Guarantees exactly one enabled AudioListener across all loaded scenes.
        /// If none exist, creates a fallback listener under SceneBridge.
        /// </summary>
        private void EnforceSingleAudioListener(bool createFallbackIfMissing)
        {
            if (!enforceSingleAudioListener)
                return;

            AudioListener selected = null;
            AudioListener[] listeners = Resources.FindObjectsOfTypeAll<AudioListener>();

            // 1) Prefer MainCamera's listener if active in hierarchy.
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                AudioListener mainListener = mainCamera.GetComponent<AudioListener>();
                if (IsSceneListener(mainListener) && mainListener.gameObject.activeInHierarchy)
                    selected = mainListener;
            }

            // 2) Prefer active listener in the currently relevant scene.
            if (selected == null)
            {
                string preferredScene = (CurrentView == ActiveView.Validation && IsValidationLoaded)
                    ? validationSceneName
                    : SceneManager.GetActiveScene().name;

                for (int i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    if (!IsSceneListener(listener)) continue;
                    if (!listener.gameObject.activeInHierarchy) continue;
                    if (listener.gameObject.scene.name != preferredScene) continue;

                    selected = listener;
                    break;
                }
            }

            // 3) Fallback to any active scene listener.
            if (selected == null)
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    if (!IsSceneListener(listener)) continue;
                    if (!listener.gameObject.activeInHierarchy) continue;

                    selected = listener;
                    break;
                }
            }

            // 4) Last resort: create a dedicated fallback listener.
            if (selected == null && createFallbackIfMissing)
            {
                selected = GetOrCreateFallbackAudioListener();
            }

            if (selected == null)
                return;

            if (!selected.enabled)
                selected.enabled = true;

            // Disable all other enabled listeners so Unity never has >1 active.
            for (int i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (!IsSceneListener(listener)) continue;
                if (listener == selected) continue;
                if (!listener.enabled) continue;

                listener.enabled = false;
            }
        }

        private AudioListener GetOrCreateFallbackAudioListener()
        {
            if (_fallbackAudioListener != null)
                return _fallbackAudioListener;

            var fallbackGo = new GameObject("SceneBridge_AudioListener_Fallback");
            fallbackGo.transform.SetParent(transform, false);
            _fallbackAudioListener = fallbackGo.AddComponent<AudioListener>();

            if (debugLogging)
                Debug.Log("[SceneBridge] Created fallback AudioListener.");

            return _fallbackAudioListener;
        }

        private static bool IsSceneListener(AudioListener listener)
        {
            if (listener == null)
                return false;

            Scene scene = listener.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
