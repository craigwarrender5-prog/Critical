// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Controller
// ValidationDashboardController.cs - Main Controller for uGUI Dashboard
// ============================================================================
//
// PURPOSE:
//   Top-level controller for the new uGUI-based Validation Dashboard.
//   Manages canvas lifecycle, engine binding, data refresh timing, and
//   coordinates all child panels and components.
//
// ARCHITECTURE:
//   - Replaces HeatupValidationVisual.cs OnGUI-based rendering
//   - Uses Unity uGUI Canvas system for modern visual effects
//   - Additive overlay - not default startup, toggled with F1
//   - Separates data refresh rate (10 Hz) from visual refresh rate (60 Hz)
//   - All gauge animations interpolate between data updates
//
// KEYBOARD:
//   Ctrl+1-7   → Switch tabs (when visible)
//   F5-F9      → Time acceleration
//   +/-        → Increment/decrement time acceleration
//   NOTE: F1 is reserved for future help feature — NOT used here.
//   Dashboard visibility is managed by SceneBridge (V key).
//
// RELATIONSHIP TO EXISTING SYSTEM:
//   - HeatupValidationVisual.cs remains for backward compatibility
//   - This new system can coexist (only one should be active)
//   - Reads from same HeatupSimEngine public state fields
//   - Does NOT modify any engine state (read-only consumer)
//
// VERSION: 1.0.1
// DATE: 2026-02-17
// IP: IP-0031 Stage 1, IP-0031-A (blue screen fix)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Main controller for the uGUI Validation Dashboard.
    /// Manages lifecycle, engine binding, and refresh timing.
    /// </summary>
    public class ValidationDashboardController : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Engine Reference")]
        [Tooltip("Reference to the HeatupSimEngine. Auto-finds if not assigned.")]
        [SerializeField] private HeatupSimEngine engine;

        [Header("Refresh Rates")]
        [Tooltip("Data update rate in Hz (values read from engine)")]
        [Range(5f, 30f)]
        [SerializeField] private float dataRefreshRate = 10f;

        [Tooltip("Visual update rate in Hz (animations, interpolation)")]
        [Range(30f, 60f)]
        [SerializeField] private float visualRefreshRate = 60f;

        [Header("Canvas Reference")]
        [Tooltip("The main dashboard Canvas. Auto-finds if not assigned.")]
        [SerializeField] private Canvas dashboardCanvas;

        [Header("Tab System")]
        [Tooltip("Container for tab buttons")]
        [SerializeField] private Transform tabButtonContainer;

        [Tooltip("Container for tab content panels")]
        [SerializeField] private Transform tabContentContainer;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;

        // ====================================================================
        // PUBLIC STATE
        // ====================================================================

        /// <summary>Is the dashboard currently visible?</summary>
        public bool IsVisible => dashboardCanvas != null && dashboardCanvas.enabled;

        /// <summary>Currently selected tab index (0-based).</summary>
        public int CurrentTabIndex { get; private set; } = 0;

        /// <summary>Direct reference to engine for data access.</summary>
        /// <remarks>Components read public fields directly from engine.</remarks>
        public HeatupSimEngine EngineData => engine;

        /// <summary>Singleton instance for global access.</summary>
        public static ValidationDashboardController Instance { get; private set; }

        // ====================================================================
        // EVENTS
        // ====================================================================

        /// <summary>Fired when data is refreshed from engine (at dataRefreshRate).</summary>
        public event Action OnDataRefresh;

        /// <summary>Fired when visuals should update (at visualRefreshRate).</summary>
        public event Action OnVisualRefresh;

        /// <summary>Fired when tab changes.</summary>
        public event Action<int> OnTabChanged;

        /// <summary>Fired when visibility changes.</summary>
        public event Action<bool> OnVisibilityChanged;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private float _dataRefreshTimer;
        private float _visualRefreshTimer;
        private float _dataRefreshInterval;
        private float _visualRefreshInterval;

        private List<IValidationPanel> _registeredPanels = new List<IValidationPanel>();
        private List<TabDefinition> _tabs = new List<TabDefinition>();

        private bool _initialized = false;

        // Tab definitions
        private static readonly string[] TAB_NAMES = new string[]
        {
            "OVERVIEW",
            "PRIMARY",
            "PRESSURIZER",
            "CVCS",
            "SG / RHR",
            "ALARMS",
            "VALIDATION"
        };

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ValidationDashboard] Duplicate controller found - destroying");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Calculate refresh intervals
            _dataRefreshInterval = 1f / dataRefreshRate;
            _visualRefreshInterval = 1f / visualRefreshRate;
        }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (!_initialized || engine == null) return;

            // Handle keyboard input
            HandleInput();

            // Data refresh timer
            _dataRefreshTimer += Time.unscaledDeltaTime;
            if (_dataRefreshTimer >= _dataRefreshInterval)
            {
                RefreshData();
                _dataRefreshTimer = 0f;
            }

            // Visual refresh timer (only when visible)
            if (IsVisible)
            {
                _visualRefreshTimer += Time.unscaledDeltaTime;
                if (_visualRefreshTimer >= _visualRefreshInterval)
                {
                    RefreshVisuals();
                    _visualRefreshTimer = 0f;
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            // Clear event subscribers to prevent dangling references
            OnDataRefresh = null;
            OnVisualRefresh = null;
            OnTabChanged = null;
            OnVisibilityChanged = null;
        }

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        private void Initialize()
        {
            // Auto-find engine if not assigned
            if (engine == null)
            {
                engine = FindObjectOfType<HeatupSimEngine>();
                if (engine == null)
                {
                    Debug.LogError("[ValidationDashboard] No HeatupSimEngine found!");
                    return;
                }
            }

            // Auto-find canvas if not assigned
            if (dashboardCanvas == null)
            {
                dashboardCanvas = GetComponentInChildren<Canvas>(true);
                if (dashboardCanvas == null)
                {
                    Debug.LogError("[ValidationDashboard] No Canvas found!");
                    return;
                }
            }

            // Initialize tabs
            InitializeTabs();

            // Find and register all panels
            RegisterPanels();

            // Visibility is controlled by the launch path:
            //   - ValidationDashboardSceneSetup calls SetVisibility(true) after build
            //   - ValidationDashboardLauncher manages visibility externally
            // The controller does NOT set its own visibility during initialization.

            _initialized = true;

            if (enableDebugLogging)
                Debug.Log($"[ValidationDashboard] Initialized - {_registeredPanels.Count} panels registered");
        }

        private void InitializeTabs()
        {
            _tabs.Clear();
            
            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                _tabs.Add(new TabDefinition
                {
                    Index = i,
                    Name = TAB_NAMES[i],
                    Panel = null,  // Will be linked during panel registration
                    Button = null  // Will be created by UI builder or assigned
                });
            }

            if (enableDebugLogging)
                Debug.Log($"[ValidationDashboard] {_tabs.Count} tabs defined");
        }

        private void RegisterPanels()
        {
            _registeredPanels.Clear();

            // Find all panels that implement IValidationPanel
            var panels = GetComponentsInChildren<IValidationPanel>(true);
            foreach (var panel in panels)
            {
                _registeredPanels.Add(panel);
                panel.Initialize(this);

                if (enableDebugLogging)
                    Debug.Log($"[ValidationDashboard] Registered panel: {panel.PanelName}");
            }
        }

        // ====================================================================
        // REFRESH METHODS
        // ====================================================================

        private void RefreshData()
        {
            if (engine == null) return;

            // Notify all subscribers
            OnDataRefresh?.Invoke();

            // Update all registered panels
            foreach (var panel in _registeredPanels)
            {
                panel.UpdateData(engine);
            }
        }

        private void RefreshVisuals()
        {
            // Notify all subscribers
            OnVisualRefresh?.Invoke();

            // Update all registered panels
            foreach (var panel in _registeredPanels)
            {
                panel.UpdateVisuals();
            }
        }

        // ====================================================================
        // VISIBILITY CONTROL
        // ====================================================================

        /// <summary>
        /// Toggle dashboard visibility.
        /// </summary>
        public void ToggleVisibility()
        {
            SetVisibility(!IsVisible);
        }

        /// <summary>
        /// Set dashboard visibility explicitly.
        /// </summary>
        public void SetVisibility(bool visible)
        {
            if (dashboardCanvas == null) return;

            dashboardCanvas.enabled = visible;
            OnVisibilityChanged?.Invoke(visible);

            if (enableDebugLogging)
                Debug.Log($"[ValidationDashboard] Visibility set to {visible}");
        }

        // ====================================================================
        // TAB NAVIGATION
        // ====================================================================

        /// <summary>
        /// Switch to a specific tab by index with optional fade transition.
        /// </summary>
        public void SwitchToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count)
            {
                Debug.LogWarning($"[ValidationDashboard] Invalid tab index: {tabIndex}");
                return;
            }

            if (tabIndex == CurrentTabIndex) return;

            int previousTab = CurrentTabIndex;
            CurrentTabIndex = tabIndex;

            // Update tab button states
            UpdateTabButtonStates();

            // Fade transition if content container has CanvasGroup
            if (tabContentContainer != null)
            {
                CanvasGroup cg = tabContentContainer.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = tabContentContainer.gameObject.AddComponent<CanvasGroup>();

                StopAllCoroutines();
                StartCoroutine(FadeTabTransition(cg, previousTab, tabIndex));
            }
            else
            {
                UpdatePanelVisibility();
            }

            // Fire event
            OnTabChanged?.Invoke(tabIndex);

            if (enableDebugLogging)
                Debug.Log($"[ValidationDashboard] Tab switched: {TAB_NAMES[previousTab]} -> {TAB_NAMES[tabIndex]}");
        }

        private System.Collections.IEnumerator FadeTabTransition(CanvasGroup cg, int fromTab, int toTab)
        {
            float fadeDuration = ValidationDashboardTheme.TabTransitionDuration * 0.5f;

            // Fade out
            yield return TabNavigationController.FadeCanvasGroup(cg, 0f, fadeDuration);

            // Swap panels
            UpdatePanelVisibility();

            // Fade in
            yield return TabNavigationController.FadeCanvasGroup(cg, 1f, fadeDuration);
        }

        private void UpdateTabButtonStates()
        {
            // Update visual state of tab buttons
            // Implementation depends on how buttons are created
        }

        private void UpdatePanelVisibility()
        {
            // Show only the current tab's panel
            foreach (var panel in _registeredPanels)
            {
                bool shouldShow = (panel.TabIndex == CurrentTabIndex) || panel.AlwaysVisible;
                panel.SetVisible(shouldShow);
            }
        }

        // ====================================================================
        // INPUT HANDLING
        // ====================================================================

        private void HandleInput()
        {
            // NOTE: F1 is reserved for future help feature — NOT used here.
            // Dashboard visibility is managed by SceneBridge (V key loads/unloads
            // the Validator scene). This controller only handles tab switching
            // and time acceleration while the dashboard is visible.

            var kb = Keyboard.current;
            if (kb == null) return;

            // Only handle keys if visible
            if (!IsVisible) return;

            // Ctrl+1-7 - Tab switching (Ctrl modifier prevents conflict with
            // SceneBridge digit-key detection which returns to operator screens)
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl)
            {
                if (kb.digit1Key.wasPressedThisFrame) SwitchToTab(0);
                if (kb.digit2Key.wasPressedThisFrame) SwitchToTab(1);
                if (kb.digit3Key.wasPressedThisFrame) SwitchToTab(2);
                if (kb.digit4Key.wasPressedThisFrame) SwitchToTab(3);
                if (kb.digit5Key.wasPressedThisFrame) SwitchToTab(4);
                if (kb.digit6Key.wasPressedThisFrame) SwitchToTab(5);
                if (kb.digit7Key.wasPressedThisFrame) SwitchToTab(6);
            }

            // Time acceleration (F5-F9)
            if (kb.f5Key.wasPressedThisFrame) engine?.SetTimeAcceleration(0);
            if (kb.f6Key.wasPressedThisFrame) engine?.SetTimeAcceleration(1);
            if (kb.f7Key.wasPressedThisFrame) engine?.SetTimeAcceleration(2);
            if (kb.f8Key.wasPressedThisFrame) engine?.SetTimeAcceleration(3);
            if (kb.f9Key.wasPressedThisFrame) engine?.SetTimeAcceleration(4);

            // +/- for time acceleration increment/decrement
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
            {
                int newIndex = (engine.currentSpeedIndex + 1) % 5;
                engine?.SetTimeAcceleration(newIndex);
            }
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                int newIndex = (engine.currentSpeedIndex - 1 + 5) % 5;
                engine?.SetTimeAcceleration(newIndex);
            }
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Get direct access to the engine for complex queries.
        /// </summary>
        public HeatupSimEngine Engine => engine;

        /// <summary>
        /// Register a panel dynamically (for runtime-created panels).
        /// </summary>
        public void RegisterPanel(IValidationPanel panel)
        {
            if (!_registeredPanels.Contains(panel))
            {
                _registeredPanels.Add(panel);
                panel.Initialize(this);
            }
        }

        /// <summary>
        /// Unregister a panel.
        /// </summary>
        public void UnregisterPanel(IValidationPanel panel)
        {
            _registeredPanels.Remove(panel);
        }

        /// <summary>
        /// Get tab name by index.
        /// </summary>
        public string GetTabName(int index)
        {
            if (index >= 0 && index < TAB_NAMES.Length)
                return TAB_NAMES[index];
            return "UNKNOWN";
        }

        /// <summary>
        /// Get total tab count.
        /// </summary>
        public int TabCount => TAB_NAMES.Length;

        // ====================================================================
        // NESTED TYPES
        // ====================================================================

        /// <summary>
        /// Definition for a dashboard tab.
        /// </summary>
        [Serializable]
        public class TabDefinition
        {
            public int Index;
            public string Name;
            public IValidationPanel Panel;
            public Button Button;
        }
    }

    // ========================================================================
    // PANEL INTERFACE
    // ========================================================================

    /// <summary>
    /// Interface that all validation dashboard panels must implement.
    /// </summary>
    public interface IValidationPanel
    {
        /// <summary>Name of this panel for logging/debugging.</summary>
        string PanelName { get; }

        /// <summary>Tab index this panel belongs to (-1 for floating/always-visible).</summary>
        int TabIndex { get; }

        /// <summary>If true, panel is visible regardless of current tab.</summary>
        bool AlwaysVisible { get; }

        /// <summary>Called once during dashboard initialization.</summary>
        void Initialize(ValidationDashboardController controller);

        /// <summary>Called at data refresh rate with fresh engine data.</summary>
        void UpdateData(HeatupSimEngine engine);

        /// <summary>Called at visual refresh rate for animations/interpolation.</summary>
        void UpdateVisuals();

        /// <summary>Show or hide this panel.</summary>
        void SetVisible(bool visible);
    }
}
