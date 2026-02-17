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
        [SerializeField] private bool useNewDashboard = true;

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
            // Re-show dashboard when scene becomes active
            if (_initialized && useNewDashboard && _newDashboard != null)
            {
                _newDashboard.SetVisibility(true);
            }
        }

        void OnDisable()
        {
            // Hide when scene is being unloaded
            if (_newDashboard != null)
            {
                _newDashboard.SetVisibility(false);
            }
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

            // Build new dashboard
            _newDashboard = ValidationDashboardBuilder.Build(_engine);

            if (_newDashboard != null)
            {
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
