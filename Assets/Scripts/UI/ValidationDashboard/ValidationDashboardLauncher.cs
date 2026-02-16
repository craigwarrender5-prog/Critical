// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Launcher
// ValidationDashboardLauncher.cs - Runtime Dashboard Instantiation
// ============================================================================
//
// PURPOSE:
//   Provides a simple way to launch the Validation Dashboard at runtime.
//   Attach this component to any GameObject (e.g., the SceneBridge or
//   HeatupSimEngine GameObject) to enable the dashboard.
//
// USAGE:
//   1. Attach to a DontDestroyOnLoad GameObject (recommended: SceneBridge)
//   2. The dashboard will be built on Start() if buildOnStart is true
//   3. Press F1 at runtime to toggle dashboard visibility
//   4. Use Ctrl+1-7 to switch tabs while dashboard is visible
//
// NOTE:
//   This launcher creates the dashboard programmatically without prefabs.
//   All UI is built via ValidationDashboardBuilder.Build().
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using UnityEngine;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Launcher component that creates the Validation Dashboard at runtime.
    /// </summary>
    public class ValidationDashboardLauncher : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Launch Settings")]
        [Tooltip("Build the dashboard automatically on Start()")]
        [SerializeField] private bool buildOnStart = true;

        [Tooltip("Reference to the HeatupSimEngine. Auto-finds if not assigned.")]
        [SerializeField] private HeatupSimEngine engine;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // ====================================================================
        // PRIVATE STATE
        // ====================================================================

        private ValidationDashboardController _controller;
        private bool _dashboardBuilt = false;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Start()
        {
            if (buildOnStart)
            {
                BuildDashboard();
            }
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Build the dashboard if not already built.
        /// </summary>
        [ContextMenu("Build Dashboard")]
        public void BuildDashboard()
        {
            if (_dashboardBuilt)
            {
                if (enableDebugLogging)
                    Debug.Log("[ValidationDashboardLauncher] Dashboard already built");
                return;
            }

            // Auto-find engine if not assigned
            if (engine == null)
            {
                engine = FindObjectOfType<HeatupSimEngine>();
            }

            if (engine == null)
            {
                Debug.LogError("[ValidationDashboardLauncher] Cannot build dashboard - no HeatupSimEngine found!");
                return;
            }

            if (enableDebugLogging)
                Debug.Log("[ValidationDashboardLauncher] Building dashboard...");

            _controller = ValidationDashboardBuilder.Build(engine);

            if (_controller != null)
            {
                _dashboardBuilt = true;
                
                // Make dashboard persist across scene loads
                DontDestroyOnLoad(_controller.gameObject);

                if (enableDebugLogging)
                    Debug.Log("[ValidationDashboardLauncher] Dashboard built successfully. Press F1 to show.");
            }
            else
            {
                Debug.LogError("[ValidationDashboardLauncher] Dashboard build failed!");
            }
        }

        /// <summary>
        /// Get the dashboard controller (null if not built).
        /// </summary>
        public ValidationDashboardController Controller => _controller;

        /// <summary>
        /// Is the dashboard built and ready?
        /// </summary>
        public bool IsDashboardReady => _dashboardBuilt && _controller != null;

        /// <summary>
        /// Toggle dashboard visibility.
        /// </summary>
        public void ToggleDashboard()
        {
            if (_controller != null)
            {
                _controller.ToggleVisibility();
            }
        }

        /// <summary>
        /// Show the dashboard.
        /// </summary>
        public void ShowDashboard()
        {
            if (_controller != null)
            {
                _controller.SetVisibility(true);
            }
        }

        /// <summary>
        /// Hide the dashboard.
        /// </summary>
        public void HideDashboard()
        {
            if (_controller != null)
            {
                _controller.SetVisibility(false);
            }
        }
    }
}
