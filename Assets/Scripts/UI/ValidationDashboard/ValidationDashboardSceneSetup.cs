// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Scene Integration
// ValidationDashboardSceneSetup.cs - Setup for Validator Scene
// ============================================================================
//
// PURPOSE:
//   Integrates the new uGUI Validation Dashboard into the existing Validator
//   scene infrastructure. This component should be placed on a GameObject in
//   the Validator.unity scene (alongside or replacing HeatupValidationVisual).
//
// USAGE:
//   1. Open Validator.unity scene
//   2. Add empty GameObject named "ValidationDashboardRoot"
//   3. Attach this component
//   4. Set useNewDashboard = true to use uGUI, false for legacy OnGUI
//   5. The dashboard will build when the scene loads
//
// INTEGRATION:
//   - Works with SceneBridge's additive scene loading (V key)
//   - Finds HeatupSimEngine via DontDestroyOnLoad persistence
//   - Coexists with HeatupValidationVisual (only one should be active)
//   - F1 toggles visibility (consistent with legacy behavior)
//
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0031 Stage 6
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Critical.UI.ValidationDashboard;

namespace Critical.Validation
{
    /// <summary>
    /// Scene setup component for the Validation Dashboard.
    /// Place in Validator.unity scene to enable the new uGUI dashboard.
    /// </summary>
    public class ValidationDashboardSceneSetup : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Dashboard Selection")]
        [Tooltip("Use new uGUI dashboard (true) or legacy OnGUI (false)")]
        [SerializeField] private bool useNewDashboard = false;  // IP-0041: Reverted to legacy until new dashboard font/material issues resolved

        [Header("Legacy Reference")]
        [Tooltip("Reference to legacy HeatupValidationVisual (auto-found if null)")]
        [SerializeField] private HeatupValidationVisual legacyDashboard;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private ValidationDashboardController _newDashboard;
        private HeatupSimEngine _engine;
        private bool _initialized = false;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            if (debugLogging)
                Debug.Log("[ValidationDashboardSceneSetup] Awake - initializing...");
        }

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            // If re-enabled after a previous disable (scene reload cycle),
            // re-initialize since the dashboard was destroyed with the scene.
            if (_initialized && useNewDashboard && _newDashboard == null)
            {
                _initialized = false;
                Initialize();
            }
            else if (_initialized && useNewDashboard && _newDashboard != null)
            {
                _newDashboard.SetVisibility(true);
            }
        }

        void OnDisable()
        {
            // When the Validator scene is unloaded, the dashboard canvas is
            // destroyed with it (we moved it into this scene in SetupNewDashboard).
            // Clear our reference so we rebuild fresh on next scene load.
            _newDashboard = null;
        }

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        private void Initialize()
        {
            // Find the persistent HeatupSimEngine
            _engine = FindObjectOfType<HeatupSimEngine>();
            if (_engine == null)
            {
                Debug.LogError("[ValidationDashboardSceneSetup] No HeatupSimEngine found! " +
                               "Dashboard will have no data.");
                return;
            }

            // Find legacy dashboard if not assigned
            if (legacyDashboard == null)
            {
                legacyDashboard = FindObjectOfType<HeatupValidationVisual>();
            }

            if (useNewDashboard)
            {
                SetupNewDashboard();
            }
            else
            {
                SetupLegacyDashboard();
            }

            _initialized = true;

            if (debugLogging)
                Debug.Log($"[ValidationDashboardSceneSetup] Initialized - " +
                          $"Using {(useNewDashboard ? "NEW uGUI" : "LEGACY OnGUI")} dashboard");
        }

        private void SetupNewDashboard()
        {
            // Disable legacy dashboard if present
            if (legacyDashboard != null)
            {
                legacyDashboard.dashboardVisible = false;
                legacyDashboard.enabled = false;

                if (debugLogging)
                    Debug.Log("[ValidationDashboardSceneSetup] Legacy dashboard disabled");
            }

            // Ensure an EventSystem exists (required for uGUI pointer events).
            // The MainScene EventSystem may be on the hidden operator canvas.
            EnsureEventSystem();

            // Build new dashboard
            _newDashboard = ValidationDashboardBuilder.Build(_engine);

            if (_newDashboard != null)
            {
                // Move the runtime-created canvas into the Validator scene so it
                // is destroyed automatically when SceneBridge unloads the scene.
                // Without this, the canvas becomes an orphan root object that
                // persists in DontDestroyOnLoad territory.
                Scene validatorScene = gameObject.scene;
                if (validatorScene.IsValid() && validatorScene.isLoaded)
                {
                    SceneManager.MoveGameObjectToScene(
                        _newDashboard.gameObject, validatorScene);

                    if (debugLogging)
                        Debug.Log($"[ValidationDashboardSceneSetup] Dashboard canvas moved to scene '{validatorScene.name}'");
                }

                // Show immediately (we're in the Validator scene)
                _newDashboard.SetVisibility(true);

                if (debugLogging)
                    Debug.Log("[ValidationDashboardSceneSetup] New uGUI dashboard built and visible");
            }
            else
            {
                Debug.LogError("[ValidationDashboardSceneSetup] Failed to build new dashboard!");

                // Fallback to legacy
                if (legacyDashboard != null)
                {
                    legacyDashboard.enabled = true;
                    legacyDashboard.dashboardVisible = true;
                    Debug.Log("[ValidationDashboardSceneSetup] Falling back to legacy dashboard");
                }
            }
        }

        /// <summary>
        /// Ensure an EventSystem exists so uGUI pointer events (tab clicks,
        /// tooltips, etc.) function correctly. The MainScene's EventSystem
        /// may be on the operator canvas which is hidden when Validator loads.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            // Check if one exists but is inactive
            EventSystem existing = FindObjectOfType<EventSystem>(true);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                if (debugLogging)
                    Debug.Log("[ValidationDashboardSceneSetup] Reactivated existing EventSystem");
                return;
            }

            // Create a new one in our scene
            GameObject esGO = new GameObject("EventSystem_Validator");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            if (debugLogging)
                Debug.Log("[ValidationDashboardSceneSetup] Created EventSystem for Validator scene");
        }

        private void SetupLegacyDashboard()
        {
            // Ensure legacy dashboard is active
            if (legacyDashboard != null)
            {
                legacyDashboard.enabled = true;
                legacyDashboard.dashboardVisible = true;

                if (debugLogging)
                    Debug.Log("[ValidationDashboardSceneSetup] Legacy dashboard enabled");
            }
            else
            {
                Debug.LogWarning("[ValidationDashboardSceneSetup] No legacy dashboard found!");
            }
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Switch between new and legacy dashboards at runtime.
        /// </summary>
        public void SetUseNewDashboard(bool useNew)
        {
            if (useNew == useNewDashboard) return;

            useNewDashboard = useNew;

            if (useNew)
            {
                SetupNewDashboard();
            }
            else
            {
                // Destroy new dashboard
                if (_newDashboard != null)
                {
                    Destroy(_newDashboard.gameObject);
                    _newDashboard = null;
                }

                SetupLegacyDashboard();
            }
        }

        /// <summary>
        /// Toggle dashboard visibility.
        /// </summary>
        public void ToggleVisibility()
        {
            if (useNewDashboard && _newDashboard != null)
            {
                _newDashboard.ToggleVisibility();
            }
            else if (legacyDashboard != null)
            {
                legacyDashboard.dashboardVisible = !legacyDashboard.dashboardVisible;
            }
        }
    }
}
