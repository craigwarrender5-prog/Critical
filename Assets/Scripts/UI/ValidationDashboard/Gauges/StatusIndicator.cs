// ============================================================================
// CRITICAL: Master the Atom - Status Indicator Component
// StatusIndicator.cs - Boolean On/Off Indicator with Pulse Animation
// ============================================================================
//
// PURPOSE:
//   Renders a pill-shaped or circular status indicator for boolean states:
//   - On/Off, Active/Inactive, Running/Stopped
//   - Configurable colors for each state
//   - Optional pulse/glow animation for alarm states
//   - Label text display
//
// VISUAL DESIGN:
//   Normal:  [████ RCP-1 ████]  (solid green)
//   Alarm:   [▓▓▓▓ RCP-1 ▓▓▓▓]  (pulsing red)
//   Off:     [░░░░ RCP-1 ░░░░]  (dim gray)
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Boolean status indicator with optional pulse animation.
    /// </summary>
    public class StatusIndicator : MonoBehaviour
    {
        // ====================================================================
        // ENUMS
        // ====================================================================

        public enum IndicatorShape
        {
            Pill,       // Rounded rectangle
            Circle,     // Circle
            Square      // Square with rounded corners
        }

        public enum IndicatorState
        {
            Off,        // Inactive/stopped
            Normal,     // Active/running normally
            Warning,    // Active with warning condition
            Alarm       // Active with alarm condition
        }

        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("State")]
        [SerializeField] private IndicatorState currentState = IndicatorState.Off;

        [Header("Shape")]
        [SerializeField] private IndicatorShape shape = IndicatorShape.Pill;

        [Header("Animation")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.5f;
        [SerializeField] private float pulseMaxAlpha = 1.0f;

        [Header("Colors")]
        [SerializeField] private Color offColor = new Color32(50, 55, 70, 255);
        [SerializeField] private Color normalColor = new Color32(46, 217, 64, 255);
        [SerializeField] private Color warningColor = new Color32(255, 199, 0, 255);
        [SerializeField] private Color alarmColor = new Color32(255, 46, 46, 255);

        [Header("Text Colors")]
        [SerializeField] private Color offTextColor = new Color32(100, 105, 120, 255);
        [SerializeField] private Color onTextColor = new Color32(20, 22, 28, 255);

        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image glowImage;

        // ====================================================================
        // PRIVATE STATE
        // ====================================================================

        private float _pulseTime;
        private Color _baseColor;
        private bool _isPulsing;

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        public IndicatorState State => currentState;
        public bool IsOn => currentState != IndicatorState.Off;
        public bool IsAlarming => currentState == IndicatorState.Alarm;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            UpdateVisuals();
        }

        void Update()
        {
            if (_isPulsing && enablePulse)
            {
                _pulseTime += Time.deltaTime * pulseSpeed;
                float pulse = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, 
                    (Mathf.Sin(_pulseTime * Mathf.PI * 2f) + 1f) * 0.5f);
                
                ApplyPulse(pulse);
            }
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set the indicator state.
        /// </summary>
        public void SetState(IndicatorState state)
        {
            currentState = state;
            UpdateVisuals();
        }

        /// <summary>
        /// Set indicator to simple on/off.
        /// </summary>
        public void SetOn(bool on)
        {
            currentState = on ? IndicatorState.Normal : IndicatorState.Off;
            UpdateVisuals();
        }

        /// <summary>
        /// Set indicator with boolean and alarm flag.
        /// </summary>
        public void SetState(bool on, bool alarm)
        {
            if (!on)
                currentState = IndicatorState.Off;
            else if (alarm)
                currentState = IndicatorState.Alarm;
            else
                currentState = IndicatorState.Normal;
            UpdateVisuals();
        }

        /// <summary>
        /// Set the label text.
        /// </summary>
        public void SetLabel(string label)
        {
            if (labelText != null)
                labelText.text = label;
        }

        /// <summary>
        /// Set custom colors.
        /// </summary>
        public void SetColors(Color off, Color normal, Color warning, Color alarm)
        {
            offColor = off;
            normalColor = normal;
            warningColor = warning;
            alarmColor = alarm;
            UpdateVisuals();
        }

        /// <summary>
        /// Backward-compatible color setter for the current state bucket.
        /// </summary>
        public void SetColor(Color color)
        {
            switch (currentState)
            {
                case IndicatorState.Off:
                    offColor = color;
                    break;
                case IndicatorState.Normal:
                    normalColor = color;
                    break;
                case IndicatorState.Warning:
                    warningColor = color;
                    break;
                case IndicatorState.Alarm:
                    alarmColor = color;
                    break;
            }

            UpdateVisuals();
        }

        // ====================================================================
        // VISUALS
        // ====================================================================

        private void UpdateVisuals()
        {
            // Determine base color and pulse state
            switch (currentState)
            {
                case IndicatorState.Off:
                    _baseColor = offColor;
                    _isPulsing = false;
                    break;
                case IndicatorState.Normal:
                    _baseColor = normalColor;
                    _isPulsing = false;
                    break;
                case IndicatorState.Warning:
                    _baseColor = warningColor;
                    _isPulsing = enablePulse;
                    break;
                case IndicatorState.Alarm:
                    _baseColor = alarmColor;
                    _isPulsing = enablePulse;
                    break;
            }

            // Apply colors
            if (backgroundImage != null)
            {
                backgroundImage.color = _baseColor;
            }

            if (labelText != null)
            {
                labelText.color = currentState == IndicatorState.Off ? offTextColor : onTextColor;
            }

            if (glowImage != null)
            {
                if (currentState == IndicatorState.Off)
                {
                    glowImage.enabled = false;
                }
                else
                {
                    glowImage.enabled = true;
                    Color glowColor = _baseColor;
                    glowColor.a = 0.3f;
                    glowImage.color = glowColor;
                }
            }

            // Reset pulse time when stopping
            if (!_isPulsing)
            {
                _pulseTime = 0f;
            }
        }

        private void ApplyPulse(float intensity)
        {
            if (backgroundImage != null)
            {
                Color c = _baseColor;
                c.a = intensity;
                backgroundImage.color = c;
            }

            if (glowImage != null && glowImage.enabled)
            {
                Color gc = _baseColor;
                gc.a = 0.3f * intensity;
                glowImage.color = gc;
            }
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a StatusIndicator programmatically.
        /// </summary>
        public static StatusIndicator Create(Transform parent, string label, 
            IndicatorShape indicatorShape = IndicatorShape.Pill, float width = 80f, float height = 24f)
        {
            GameObject container = new GameObject($"StatusIndicator_{label}");
            container.transform.SetParent(parent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(width, height);

            // Background image
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(container.transform, false);

            RectTransform bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = ValidationDashboardTheme.Neutral;

            // Apply shape
            // Note: For proper rounded corners, you'd use a custom shader or 9-slice sprite
            // For now, we use the default Image which is rectangular
            // A real implementation would use UIRounded or similar

            // Glow image (behind, slightly larger)
            GameObject glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(container.transform, false);
            glowGO.transform.SetAsFirstSibling();

            RectTransform glowRT = glowGO.AddComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = Vector2.one;
            glowRT.offsetMin = new Vector2(-4, -4);
            glowRT.offsetMax = new Vector2(4, 4);

            Image glowImg = glowGO.AddComponent<Image>();
            glowImg.color = new Color(0, 1, 0, 0.2f);

            // Label text
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(container.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 0);
            labelRT.offsetMax = new Vector2(-4, 0);

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = height * 0.45f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = ValidationDashboardTheme.TextDark;

            // Add StatusIndicator component
            StatusIndicator indicator = container.AddComponent<StatusIndicator>();
            indicator.shape = indicatorShape;
            indicator.backgroundImage = bgImg;
            indicator.labelText = labelTMP;
            indicator.glowImage = glowImg;
            indicator.offColor = ValidationDashboardTheme.AnnunciatorOff;
            indicator.normalColor = ValidationDashboardTheme.NormalGreen;
            indicator.warningColor = ValidationDashboardTheme.WarningAmber;
            indicator.alarmColor = ValidationDashboardTheme.AlarmRed;
            indicator.offTextColor = ValidationDashboardTheme.TextSecondary;
            indicator.onTextColor = ValidationDashboardTheme.TextDark;

            indicator.UpdateVisuals();

            return indicator;
        }

        /// <summary>
        /// Create a compact circular indicator.
        /// </summary>
        public static StatusIndicator CreateCompact(Transform parent, string label, float size = 20f)
        {
            return Create(parent, label, IndicatorShape.Circle, size, size);
        }
    }
}
