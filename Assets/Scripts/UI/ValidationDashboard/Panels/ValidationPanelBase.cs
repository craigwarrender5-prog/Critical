// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Base Panel
// ValidationPanelBase.cs - Abstract Base Class for All Dashboard Panels
// ============================================================================
//
// PURPOSE:
//   Provides common functionality for all dashboard panels including:
//   - IValidationPanel interface implementation
//   - CanvasGroup-based visibility control with fade animation
//   - Common references (controller, RectTransform)
//   - Template methods for derived classes
//
// USAGE:
//   1. Create a new panel script inheriting from ValidationPanelBase
//   2. Override abstract properties (PanelName, TabIndex)
//   3. Override OnUpdateData() to read engine values
//   4. Override OnUpdateVisuals() for animation/interpolation
//   5. Optionally override OnInitialize() for setup
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Abstract base class for all validation dashboard panels.
    /// Provides common functionality and enforces interface compliance.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class ValidationPanelBase : MonoBehaviour, IValidationPanel
    {
        // ====================================================================
        // ABSTRACT PROPERTIES (must be implemented by derived classes)
        // ====================================================================

        /// <summary>Unique name for this panel (used in logging).</summary>
        public abstract string PanelName { get; }

        /// <summary>Tab index this panel belongs to. -1 for floating panels.</summary>
        public abstract int TabIndex { get; }

        // ====================================================================
        // VIRTUAL PROPERTIES (can be overridden)
        // ====================================================================

        /// <summary>If true, panel is visible regardless of current tab.</summary>
        public virtual bool AlwaysVisible => false;

        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Panel Settings")]
        [Tooltip("Enable fade animation when showing/hiding")]
        [SerializeField] protected bool enableFadeAnimation = true;

        [Tooltip("Fade animation duration in seconds")]
        [SerializeField] protected float fadeDuration = 0.15f;

        // ====================================================================
        // PROTECTED FIELDS
        // ====================================================================

        /// <summary>Reference to the main dashboard controller.</summary>
        protected ValidationDashboardController Controller { get; private set; }

        /// <summary>This panel's RectTransform.</summary>
        protected RectTransform RectTransform { get; private set; }

        /// <summary>CanvasGroup for visibility/fade control.</summary>
        protected CanvasGroup CanvasGroup { get; private set; }

        /// <summary>Is panel currently visible?</summary>
        protected bool IsVisible { get; private set; }

        /// <summary>Most recent engine reference.</summary>
        protected HeatupSimEngine Engine { get; private set; }

        /// <summary>Has Initialize() been called?</summary>
        protected bool IsInitialized { get; private set; }

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private float _targetAlpha;
        private float _currentAlpha;
        private bool _isFading;

        // ====================================================================
        // INTERFACE IMPLEMENTATION
        // ====================================================================

        /// <summary>
        /// Called once during dashboard initialization.
        /// </summary>
        public void Initialize(ValidationDashboardController controller)
        {
            Controller = controller;

            // Cache components
            RectTransform = GetComponent<RectTransform>();
            
            // Ensure CanvasGroup exists
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Call derived class initialization
            OnInitialize();

            IsInitialized = true;
        }

        /// <summary>
        /// Called at data refresh rate with fresh engine data.
        /// </summary>
        public void UpdateData(HeatupSimEngine engine)
        {
            if (!IsInitialized) return;

            Engine = engine;

            // Only update if visible (or always visible)
            if (IsVisible || AlwaysVisible)
            {
                OnUpdateData();
            }
        }

        /// <summary>
        /// Called at visual refresh rate for animations/interpolation.
        /// </summary>
        public void UpdateVisuals()
        {
            if (!IsInitialized) return;

            // Handle fade animation
            if (_isFading)
            {
                UpdateFadeAnimation();
            }

            // Only update visuals if visible
            if (IsVisible || AlwaysVisible)
            {
                OnUpdateVisuals();
            }
        }

        /// <summary>
        /// Show or hide this panel.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (IsVisible == visible && !_isFading) return;

            IsVisible = visible;
            _targetAlpha = visible ? 1f : 0f;

            if (enableFadeAnimation && fadeDuration > 0f)
            {
                // Start fade animation
                _isFading = true;
            }
            else
            {
                // Instant visibility change
                ApplyVisibility(_targetAlpha);
            }

            // When becoming visible, force an immediate data update so the
            // panel doesn't show stale/empty values for one refresh cycle.
            if (visible && IsInitialized && Engine != null)
            {
                OnUpdateData();
            }

            // Call derived class handler
            OnVisibilityChanged(visible);
        }

        // ====================================================================
        // VIRTUAL METHODS (optional overrides)
        // ====================================================================

        /// <summary>
        /// Called during initialization. Override to set up panel-specific components.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Called when new data is available. Override to read engine values.
        /// </summary>
        protected virtual void OnUpdateData() { }

        /// <summary>
        /// Called at visual refresh rate. Override for animations.
        /// </summary>
        protected virtual void OnUpdateVisuals() { }

        /// <summary>
        /// Called when visibility changes. Override for special handling.
        /// </summary>
        protected virtual void OnVisibilityChanged(bool visible) { }

        // ====================================================================
        // FADE ANIMATION
        // ====================================================================

        private void UpdateFadeAnimation()
        {
            if (!_isFading) return;

            // Lerp toward target alpha
            float step = Time.unscaledDeltaTime / fadeDuration;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, step);

            ApplyVisibility(_currentAlpha);

            // Check if fade complete
            if (Mathf.Approximately(_currentAlpha, _targetAlpha))
            {
                _isFading = false;
            }
        }

        private void ApplyVisibility(float alpha)
        {
            _currentAlpha = alpha;

            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = alpha;
                CanvasGroup.interactable = alpha > 0.5f;
                CanvasGroup.blocksRaycasts = alpha > 0.5f;
            }

            // Disable GameObject when fully hidden for performance
            if (alpha < 0.01f && !AlwaysVisible)
            {
                gameObject.SetActive(false);
            }
            else if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        // ====================================================================
        // UTILITY METHODS
        // ====================================================================

        /// <summary>
        /// Format a time value in hours to HH:MM:SS string.
        /// </summary>
        protected string FormatTime(float hours)
        {
            int totalSeconds = Mathf.FloorToInt(hours * 3600f);
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return $"{h}:{m:D2}:{s:D2}";
        }

        /// <summary>
        /// Get status color for a boolean state.
        /// </summary>
        protected Color GetStatusColor(bool isActive, bool isAlarm = false)
        {
            if (isAlarm)
                return isActive ? ValidationDashboardTheme.AlarmRed : ValidationDashboardTheme.NormalGreen;
            else
                return isActive ? ValidationDashboardTheme.NormalGreen : ValidationDashboardTheme.Neutral;
        }

        /// <summary>
        /// Get color based on threshold comparison.
        /// </summary>
        protected Color GetThresholdColor(float value, float warnLow, float warnHigh,
            float alarmLow, float alarmHigh)
        {
            return ValidationDashboardTheme.GetThresholdColor(value, warnLow, warnHigh, alarmLow, alarmHigh);
        }

        /// <summary>
        /// Safely get text component and set value.
        /// </summary>
        protected void SetText(TMPro.TextMeshProUGUI textComponent, string value)
        {
            if (textComponent != null)
                textComponent.text = value;
        }

        /// <summary>
        /// Safely set text color.
        /// </summary>
        protected void SetTextColor(TMPro.TextMeshProUGUI textComponent, Color color)
        {
            if (textComponent != null)
                textComponent.color = color;
        }

        /// <summary>
        /// Safely set image color.
        /// </summary>
        protected void SetImageColor(Image image, Color color)
        {
            if (image != null)
                image.color = color;
        }
    }
}
