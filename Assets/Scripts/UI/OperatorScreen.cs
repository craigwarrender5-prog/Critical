// ============================================================================
// CRITICAL: Master the Atom - Operator Screen Base Class
// OperatorScreen.cs - Abstract Base Class for All Operator Screens
// ============================================================================
//
// PURPOSE:
//   Provides common functionality for all operator screens in the simulator.
//   Each screen (Reactor Core, RCS Loop, Pressurizer, etc.) inherits from this
//   class to ensure consistent behavior, input handling, and integration
//   with the ScreenManager.
//
// FEATURES:
//   - New Input System toggle functionality (fallback when no ScreenManager)
//   - Visibility state management with events
//   - Panel layout zone helpers
//   - Automatic registration with ScreenManager
//   - Common color theme properties
//
// USAGE:
//   1. Create a new class inheriting from OperatorScreen
//   2. Override ToggleKey, ScreenName, and ScreenIndex properties
//   3. Implement screen-specific initialization and updates
//   4. Attach to a Canvas GameObject
//
// ARCHITECTURE:
//   OperatorScreen (abstract)
//       ├── ReactorScreenAdapter (Key 1 — bridges GOLD STANDARD Screen 1)
//       ├── RCSPrimaryLoopScreen (Key 2)
//       ├── PressurizerScreen (Key 3)
//       ├── CVCSScreen (Key 4)
//       ├── SteamGeneratorScreen (Key 5)
//       ├── TurbineGeneratorScreen (Key 6)
//       ├── SecondarySystemsScreen (Key 7)
//       ├── AuxiliarySystemsScreen (Key 8)
//       └── PlantOverviewScreen (Tab)
//
// INPUT SYSTEM NOTES:
//   - The project uses Unity New Input System ONLY (activeInputHandler: 1)
//   - Legacy UnityEngine.Input API is disabled project-wide
//   - When ScreenManager is present, IT handles all keyboard input
//   - The ToggleKey property (KeyCode) is retained for backward compatibility
//     and for ScreenManager registration, but is NOT used for polling
//   - The fallback self-toggle (when no ScreenManager) uses New Input System
//     via InputAction created programmatically from the ToggleKey
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md
//
// VERSION: 2.0.3
// DATE: 2026-02-10
// CLASSIFICATION: UI - Base Infrastructure
// CHANGE: v2.0.0 — Migrated from legacy Input.GetKeyDown() to New Input System
// CHANGE: v2.0.3 — Start() no longer overrides visibility when ScreenManager
//         has already shown the screen (fixes inactive screen activation race)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Critical.UI
{
    /// <summary>
    /// Abstract base class for all operator screens.
    /// Provides common toggle, visibility, and layout functionality.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class OperatorScreen : MonoBehaviour
    {
        // ====================================================================
        // ABSTRACT PROPERTIES - Must be implemented by subclasses
        // ====================================================================

        #region Abstract Properties

        /// <summary>
        /// Keyboard key associated with this screen.
        /// Used by ScreenManager for registration and display.
        /// Note: Legacy Input.GetKeyDown() is NOT used (New Input System only).
        /// ScreenManager handles input via its own Input Action callbacks.
        /// </summary>
        public abstract KeyCode ToggleKey { get; }

        /// <summary>
        /// Human-readable name for this screen.
        /// Example: "RCS Primary Loop"
        /// </summary>
        public abstract string ScreenName { get; }

        /// <summary>
        /// Unique index for this screen (1-8, 100 for overview).
        /// Used by ScreenManager for registration and lookup.
        /// </summary>
        public abstract int ScreenIndex { get; }

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS - Inspector Configuration
        // ====================================================================

        #region Inspector Fields

        [Header("Screen Settings")]
        [Tooltip("Start with this screen visible when the game begins")]
        [SerializeField] protected bool startVisible = false;

        [Tooltip("Allow keyboard toggle (can be disabled for modal screens)")]
        [SerializeField] protected bool allowKeyboardToggle = true;

        [Header("Color Theme")]
        [SerializeField] protected Color backgroundColor = new Color(0.10f, 0.10f, 0.12f);   // #1A1A1F
        [SerializeField] protected Color panelColor = new Color(0.12f, 0.12f, 0.16f);        // #1E1E28
        [SerializeField] protected Color borderColor = new Color(0.16f, 0.16f, 0.21f);       // #2A2A35
        [SerializeField] protected Color textColor = new Color(0.9f, 0.9f, 0.9f);            // #E6E6E6
        [SerializeField] protected Color accentColor = new Color(0f, 0.6f, 1f);              // #0099FF

        [Header("Panel References (Optional)")]
        [Tooltip("Left panel for gauges - typically 0-15% width")]
        [SerializeField] protected RectTransform leftPanel;

        [Tooltip("Center panel for main visualization - typically 15-65% width")]
        [SerializeField] protected RectTransform centerPanel;

        [Tooltip("Right panel for gauges/detail - typically 65-100% width")]
        [SerializeField] protected RectTransform rightPanel;

        [Tooltip("Bottom panel for controls - typically 0-26% height")]
        [SerializeField] protected RectTransform bottomPanel;

        [Header("Status Bar References (Optional)")]
        [SerializeField] protected Text screenTitleText;
        [SerializeField] protected Text simTimeText;
        [SerializeField] protected Text modeText;
        [SerializeField] protected Text timeCompressionText;

        #endregion

        // ====================================================================
        // EVENTS
        // ====================================================================

        #region Events

        /// <summary>
        /// Fired when the screen becomes visible.
        /// </summary>
        public event Action OnScreenShown;

        /// <summary>
        /// Fired when the screen becomes hidden.
        /// </summary>
        public event Action OnScreenHidden;

        /// <summary>
        /// Fired when visibility changes. Parameter is new visibility state.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;

        #endregion

        // ====================================================================
        // PROTECTED FIELDS
        // ====================================================================

        #region Protected Fields

        protected bool isVisible;
        protected RectTransform rectTransform;
        protected CanvasGroup canvasGroup;
        protected Image backgroundImage;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS — New Input System Fallback
        // ====================================================================

        #region Private Fields

        /// <summary>
        /// Fallback InputAction for self-toggle when no ScreenManager is present.
        /// Created programmatically from ToggleKey.
        /// </summary>
        private InputAction _fallbackToggleAction;

        /// <summary>
        /// Whether the fallback input is active (only when no ScreenManager).
        /// </summary>
        private bool _fallbackInputActive = false;

        /// <summary>
        /// Maps KeyCode values to New Input System binding paths.
        /// </summary>
        private static readonly Dictionary<KeyCode, string> _keyCodeToBindingPath = new Dictionary<KeyCode, string>()
        {
            { KeyCode.Alpha1, "<Keyboard>/1" },
            { KeyCode.Alpha2, "<Keyboard>/2" },
            { KeyCode.Alpha3, "<Keyboard>/3" },
            { KeyCode.Alpha4, "<Keyboard>/4" },
            { KeyCode.Alpha5, "<Keyboard>/5" },
            { KeyCode.Alpha6, "<Keyboard>/6" },
            { KeyCode.Alpha7, "<Keyboard>/7" },
            { KeyCode.Alpha8, "<Keyboard>/8" },
            { KeyCode.Alpha9, "<Keyboard>/9" },
            { KeyCode.Alpha0, "<Keyboard>/0" },
            { KeyCode.Tab,    "<Keyboard>/tab" },
            { KeyCode.F1,     "<Keyboard>/f1" },
            { KeyCode.F2,     "<Keyboard>/f2" },
            { KeyCode.F3,     "<Keyboard>/f3" },
            { KeyCode.F4,     "<Keyboard>/f4" },
        };

        #endregion

        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        #region Public Properties

        /// <summary>
        /// Is this screen currently visible?
        /// </summary>
        public bool IsVisible => isVisible;

        /// <summary>
        /// Background color for the screen.
        /// </summary>
        public Color BackgroundColor => backgroundColor;

        /// <summary>
        /// Panel background color.
        /// </summary>
        public Color PanelColor => panelColor;

        /// <summary>
        /// Border/divider color.
        /// </summary>
        public Color BorderColor => borderColor;

        /// <summary>
        /// Primary text color.
        /// </summary>
        public Color TextColor => textColor;

        /// <summary>
        /// Accent color for highlights.
        /// </summary>
        public Color AccentColor => accentColor;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Override in subclass but call base.Awake() first.
        /// </summary>
        protected virtual void Awake()
        {
            // Cache component references
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            backgroundImage = GetComponent<Image>();

            // Ensure CanvasGroup exists
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Apply background color if Image exists
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
        }

        /// <summary>
        /// Called before the first frame update.
        /// Override in subclass but call base.Start() first.
        /// </summary>
        protected virtual void Start()
        {
            // Register with ScreenManager
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.RegisterScreen(this);
            }
            else
            {
                Debug.LogWarning($"[{ScreenName}] ScreenManager not found. Setting up fallback input.");
                SetupFallbackInput();
            }

            // Set initial title
            if (screenTitleText != null)
            {
                screenTitleText.text = ScreenName.ToUpper();
            }

            // Set initial visibility — but ONLY if ScreenManager has not already
            // set us visible. When a screen starts inactive (SetActive=false from
            // the builder), ScreenManager.ShowScreen() activates it, which triggers
            // this Start(). We must NOT override the visibility that ScreenManager
            // just set, or the screen will immediately hide itself.
            bool alreadyManagedVisible = (ScreenManager.Instance != null &&
                                          ScreenManager.Instance.IsScreenRegistered(ScreenIndex) &&
                                          gameObject.activeSelf);
            if (!alreadyManagedVisible)
            {
                SetVisible(startVisible, silent: true);
            }
            else
            {
                // Sync our internal state with the actual GameObject state
                isVisible = true;
            }

            Debug.Log($"[OperatorScreen] {ScreenName} initialized. Key: {ToggleKey}, Index: {ScreenIndex}");
        }

        /// <summary>
        /// Called every frame.
        /// Override in subclass but call base.Update() first.
        /// </summary>
        protected virtual void Update()
        {
            // Note: Keyboard input is handled by ScreenManager via New Input System
            // callbacks (not by polling in Update). The fallback input action also
            // uses callbacks, so no polling needed here.

            // Update status bar if visible
            if (isVisible)
            {
                UpdateStatusBar();
            }
        }

        /// <summary>
        /// Called when the MonoBehaviour will be destroyed.
        /// Override in subclass but call base.OnDestroy().
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Cleanup fallback input
            CleanupFallbackInput();

            // Unregister from ScreenManager
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.UnregisterScreen(this);
            }
        }

        #endregion

        // ====================================================================
        // FALLBACK INPUT — New Input System (when no ScreenManager)
        // ====================================================================

        #region Fallback Input

        /// <summary>
        /// Sets up a programmatic InputAction for self-toggle.
        /// Only used when ScreenManager is not present in the scene.
        /// </summary>
        private void SetupFallbackInput()
        {
            if (!allowKeyboardToggle) return;

            if (_keyCodeToBindingPath.TryGetValue(ToggleKey, out string bindingPath))
            {
                _fallbackToggleAction = new InputAction(
                    name: $"{ScreenName}_Toggle",
                    type: InputActionType.Button,
                    binding: bindingPath
                );

                _fallbackToggleAction.performed += _ => ToggleVisibility();
                _fallbackToggleAction.Enable();
                _fallbackInputActive = true;

                Debug.Log($"[OperatorScreen] Fallback input set up for {ScreenName}: {bindingPath}");
            }
            else
            {
                Debug.LogWarning($"[OperatorScreen] No binding path found for KeyCode {ToggleKey}. " +
                               $"Fallback input not available for {ScreenName}.");
            }
        }

        /// <summary>
        /// Cleanup the fallback InputAction.
        /// </summary>
        private void CleanupFallbackInput()
        {
            if (_fallbackToggleAction != null)
            {
                _fallbackToggleAction.Disable();
                _fallbackToggleAction.Dispose();
                _fallbackToggleAction = null;
                _fallbackInputActive = false;
            }
        }

        #endregion

        // ====================================================================
        // VISIBILITY CONTROL
        // ====================================================================

        #region Visibility Control

        /// <summary>
        /// Toggle screen visibility.
        /// </summary>
        public void ToggleVisibility()
        {
            SetVisible(!isVisible);
        }

        /// <summary>
        /// Show the screen.
        /// </summary>
        public void Show()
        {
            SetVisible(true);
        }

        /// <summary>
        /// Hide the screen.
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// Set screen visibility with optional silent mode (no events).
        /// </summary>
        /// <param name="visible">New visibility state</param>
        /// <param name="silent">If true, don't fire events</param>
        public virtual void SetVisible(bool visible, bool silent = false)
        {
            if (isVisible == visible) return;

            isVisible = visible;
            gameObject.SetActive(visible);

            // Update CanvasGroup for potential fade effects
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (!silent)
            {
                // Fire events
                OnVisibilityChanged?.Invoke(visible);

                if (visible)
                {
                    OnScreenShownInternal();
                    OnScreenShown?.Invoke();
                }
                else
                {
                    OnScreenHiddenInternal();
                    OnScreenHidden?.Invoke();
                }

                Debug.Log($"[OperatorScreen] {ScreenName} visibility: {visible}");
            }
        }

        /// <summary>
        /// Called internally when screen is shown. Override for custom behavior.
        /// </summary>
        protected virtual void OnScreenShownInternal()
        {
            // Subclasses can override to refresh data, etc.
        }

        /// <summary>
        /// Called internally when screen is hidden. Override for custom behavior.
        /// </summary>
        protected virtual void OnScreenHiddenInternal()
        {
            // Subclasses can override to cleanup, etc.
        }

        /// <summary>
        /// Fire the OnVisibilityChanged event. Accessible to subclasses.
        /// C# events can only be invoked from the declaring class.
        /// </summary>
        protected void FireVisibilityChanged(bool visible)
        {
            OnVisibilityChanged?.Invoke(visible);
        }

        /// <summary>
        /// Fire the OnScreenShown event. Accessible to subclasses.
        /// </summary>
        protected void FireScreenShown()
        {
            OnScreenShown?.Invoke();
        }

        /// <summary>
        /// Fire the OnScreenHidden event. Accessible to subclasses.
        /// </summary>
        protected void FireScreenHidden()
        {
            OnScreenHidden?.Invoke();
        }

        #endregion

        // ====================================================================
        // STATUS BAR
        // ====================================================================

        #region Status Bar

        /// <summary>
        /// Update the status bar displays. Override to customize.
        /// </summary>
        protected virtual void UpdateStatusBar()
        {
            // Update simulation time
            if (simTimeText != null)
            {
                float simTime = Time.time; // Override in subclass with actual sim time
                int hours = Mathf.FloorToInt(simTime / 3600f);
                int minutes = Mathf.FloorToInt((simTime % 3600f) / 60f);
                int seconds = Mathf.FloorToInt(simTime % 60f);
                simTimeText.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }

            // Update time compression
            if (timeCompressionText != null)
            {
                float timeScale = Time.timeScale;
                if (timeScale <= 0f)
                {
                    timeCompressionText.text = "PAUSED";
                }
                else if (timeScale >= 1000f)
                {
                    timeCompressionText.text = $"{timeScale / 1000f:F1}kx";
                }
                else
                {
                    timeCompressionText.text = $"{timeScale:F0}x";
                }
            }
        }

        #endregion

        // ====================================================================
        // LAYOUT HELPERS
        // ====================================================================

        #region Layout Helpers

        /// <summary>
        /// Standard layout zone definitions for 1920x1080 reference resolution.
        /// </summary>
        public static class LayoutZones
        {
            // Horizontal zones (X anchors)
            public const float LEFT_MIN = 0f;
            public const float LEFT_MAX = 0.15f;
            public const float CENTER_MIN = 0.15f;
            public const float CENTER_MAX = 0.65f;
            public const float RIGHT_MIN = 0.65f;
            public const float RIGHT_MAX = 1f;

            // Vertical zones (Y anchors)
            public const float BOTTOM_MIN = 0f;
            public const float BOTTOM_MAX = 0.26f;
            public const float MAIN_MIN = 0.26f;
            public const float MAIN_MAX = 1f;
            public const float STATUS_MIN = 0.97f;
            public const float STATUS_MAX = 1f;
        }

        /// <summary>
        /// Get anchor bounds for a standard panel zone.
        /// </summary>
        public static (Vector2 anchorMin, Vector2 anchorMax) GetZoneAnchors(ScreenZone zone)
        {
            return zone switch
            {
                ScreenZone.LeftGauges => (
                    new Vector2(LayoutZones.LEFT_MIN, LayoutZones.MAIN_MIN),
                    new Vector2(LayoutZones.LEFT_MAX, LayoutZones.MAIN_MAX)),
                
                ScreenZone.CenterVisualization => (
                    new Vector2(LayoutZones.CENTER_MIN, LayoutZones.MAIN_MIN),
                    new Vector2(LayoutZones.CENTER_MAX, LayoutZones.MAIN_MAX)),
                
                ScreenZone.RightGauges => (
                    new Vector2(LayoutZones.RIGHT_MIN, LayoutZones.MAIN_MIN),
                    new Vector2(LayoutZones.RIGHT_MAX, LayoutZones.MAIN_MAX)),
                
                ScreenZone.BottomControls => (
                    new Vector2(0f, LayoutZones.BOTTOM_MIN),
                    new Vector2(1f, LayoutZones.BOTTOM_MAX)),
                
                ScreenZone.StatusBar => (
                    new Vector2(0f, LayoutZones.STATUS_MIN),
                    new Vector2(1f, LayoutZones.STATUS_MAX)),
                
                _ => (Vector2.zero, Vector2.one)
            };
        }

        /// <summary>
        /// Apply zone anchors to a RectTransform.
        /// </summary>
        protected void ApplyZoneAnchors(RectTransform target, ScreenZone zone)
        {
            if (target == null) return;

            var (anchorMin, anchorMax) = GetZoneAnchors(zone);
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        #endregion

        // ====================================================================
        // UTILITY METHODS
        // ====================================================================

        #region Utility Methods

        /// <summary>
        /// Format a temperature value for display.
        /// </summary>
        protected string FormatTemperature(float tempF, int decimals = 1)
        {
            return $"{tempF.ToString($"F{decimals}")}°F";
        }

        /// <summary>
        /// Format a pressure value for display.
        /// </summary>
        protected string FormatPressure(float psia, int decimals = 0)
        {
            return $"{psia.ToString($"F{decimals}")} psia";
        }

        /// <summary>
        /// Format a flow rate for display.
        /// </summary>
        protected string FormatFlow(float gpm, int decimals = 0)
        {
            if (gpm >= 1000f)
            {
                return $"{(gpm / 1000f).ToString($"F{decimals}")}K gpm";
            }
            return $"{gpm.ToString($"F{decimals}")} gpm";
        }

        /// <summary>
        /// Format a power value for display.
        /// </summary>
        protected string FormatPower(float mw, int decimals = 1)
        {
            return $"{mw.ToString($"F{decimals}")} MW";
        }

        #endregion
    }

    // ========================================================================
    // SUPPORTING ENUMS
    // ========================================================================

    /// <summary>
    /// Standard screen layout zones.
    /// </summary>
    public enum ScreenZone
    {
        /// <summary>Left panel for gauges (0-15% width)</summary>
        LeftGauges,

        /// <summary>Center panel for main visualization (15-65% width)</summary>
        CenterVisualization,

        /// <summary>Right panel for gauges or detail (65-100% width)</summary>
        RightGauges,

        /// <summary>Bottom panel for controls (0-26% height)</summary>
        BottomControls,

        /// <summary>Top status bar (97-100% height)</summary>
        StatusBar
    }
}
