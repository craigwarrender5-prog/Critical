// ============================================================================
// CRITICAL: Master the Atom - Digital Readout Component
// DigitalReadout.cs - Large Numeric Display with Glow Effect
// ============================================================================
//
// PURPOSE:
//   Renders a large digital numeric display similar to control room
//   digital indicators with:
//   - Large, bold numeric value
//   - Optional unit suffix
//   - Threshold-based color coding
//   - Optional glow/bloom effect
//   - Value change animation
//
// VISUAL DESIGN:
//   ┌─────────────────────────────────┐
//   │                                 │
//   │         2235.5                  │
//   │          psig                   │
//   │       RCS PRESSURE              │
//   │                                 │
//   └─────────────────────────────────┘
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
    /// Large digital readout display with threshold-based coloring.
    /// </summary>
    public class DigitalReadout : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Value Settings")]
        [SerializeField] private float currentValue = 0f;

        [Header("Thresholds")]
        [SerializeField] private bool useThresholds = true;
        [SerializeField] private float warningLow = float.MinValue;
        [SerializeField] private float warningHigh = float.MaxValue;
        [SerializeField] private float alarmLow = float.MinValue;
        [SerializeField] private float alarmHigh = float.MaxValue;

        [Header("Display")]
        [SerializeField] private string valueFormat = "F1";
        [SerializeField] private string unitSuffix = "";
        [SerializeField] private float valueFontSize = 32f;
        [SerializeField] private float unitFontSize = 12f;
        [SerializeField] private float labelFontSize = 10f;

        [Header("Animation")]
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private bool enableAnimation = true;
        [SerializeField] private bool enableColorTransition = true;
        [SerializeField] private float colorTransitionSpeed = 5f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color32(46, 217, 64, 255);
        [SerializeField] private Color warningColor = new Color32(255, 199, 0, 255);
        [SerializeField] private Color alarmColor = new Color32(255, 46, 46, 255);
        [SerializeField] private Color neutralColor = new Color32(235, 237, 242, 255);

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI unitText;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image glowImage;

        // ====================================================================
        // PRIVATE STATE
        // ====================================================================

        private float _displayValue;
        private float _velocity;
        private float _targetValue;
        private Color _targetColor;
        private Color _currentColor;
        private bool _hasTextOverride;
        private string _textOverride = string.Empty;

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        public float DisplayValue => _displayValue;
        public float TargetValue => _targetValue;
        public Color CurrentColor => _currentColor;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            _displayValue = currentValue;
            _targetValue = currentValue;
            _currentColor = GetTargetColor();
            _targetColor = _currentColor;
        }

        void Update()
        {
            bool needsUpdate = false;

            // Value animation
            if (enableAnimation && !Mathf.Approximately(_displayValue, _targetValue))
            {
                _displayValue = Mathf.SmoothDamp(_displayValue, _targetValue, ref _velocity, smoothTime);
                
                if (Mathf.Abs(_displayValue - _targetValue) < 0.001f)
                {
                    _displayValue = _targetValue;
                    _velocity = 0f;
                }
                needsUpdate = true;
            }

            // Color transition
            if (enableColorTransition && _currentColor != _targetColor)
            {
                _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * colorTransitionSpeed);
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                UpdateDisplay();
            }
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set the displayed value.
        /// </summary>
        public void SetValue(float value)
        {
            _hasTextOverride = false;
            _targetValue = value;
            currentValue = value;
            _targetColor = GetTargetColor();
            
            if (!enableAnimation)
            {
                _displayValue = value;
                _velocity = 0f;
            }
            if (!enableColorTransition)
            {
                _currentColor = _targetColor;
            }
            
            UpdateDisplay();
        }

        /// <summary>
        /// Backward-compatible text mode for non-numeric readouts.
        /// </summary>
        public void SetText(string text)
        {
            _hasTextOverride = true;
            _textOverride = text ?? string.Empty;
            UpdateDisplay();
        }

        /// <summary>
        /// Set threshold values.
        /// </summary>
        public void SetThresholds(float warnLow, float warnHigh, float almLow, float almHigh)
        {
            warningLow = warnLow;
            warningHigh = warnHigh;
            alarmLow = almLow;
            alarmHigh = almHigh;
            _targetColor = GetTargetColor();
            UpdateDisplay();
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
        /// Set the unit suffix.
        /// </summary>
        public void SetUnit(string unit)
        {
            unitSuffix = unit;
            if (unitText != null)
                unitText.text = unit;
        }

        /// <summary>
        /// Set the value format string.
        /// </summary>
        public void SetFormat(string format)
        {
            valueFormat = format;
            UpdateDisplay();
        }

        /// <summary>
        /// Enable or disable threshold-based coloring.
        /// </summary>
        public void SetUseThresholds(bool use)
        {
            useThresholds = use;
            _targetColor = GetTargetColor();
            UpdateDisplay();
        }

        /// <summary>
        /// Force a specific color (ignores thresholds).
        /// </summary>
        public void SetColor(Color color)
        {
            useThresholds = false;
            _targetColor = color;
            if (!enableColorTransition)
                _currentColor = color;
            UpdateDisplay();
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private Color GetTargetColor()
        {
            if (!useThresholds)
                return neutralColor;

            if (_displayValue <= alarmLow || _displayValue >= alarmHigh)
                return alarmColor;
            if (_displayValue <= warningLow || _displayValue >= warningHigh)
                return warningColor;
            return normalColor;
        }

        private void UpdateDisplay()
        {
            if (valueText != null)
            {
                valueText.text = _hasTextOverride ? _textOverride : _displayValue.ToString(valueFormat);
                valueText.color = _currentColor;
            }

            if (unitText != null)
            {
                unitText.color = _currentColor;
            }

            if (glowImage != null)
            {
                Color glowColor = _currentColor;
                glowColor.a = 0.3f;
                glowImage.color = glowColor;
            }
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a DigitalReadout programmatically.
        /// </summary>
        public static DigitalReadout Create(Transform parent, string label, string unit = "",
            string format = "F1", float fontSize = 24f)
        {
            GameObject container = new GameObject($"DigitalReadout_{label}");
            container.transform.SetParent(parent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(100, 60);

            // Vertical layout
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0;

            // Value text
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(container.transform, false);

            LayoutElement valueLE = valueGO.AddComponent<LayoutElement>();
            valueLE.preferredHeight = fontSize + 4;

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "---";
            valueTMP.fontSize = fontSize;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.alignment = TextAlignmentOptions.Center;
            valueTMP.color = ValidationDashboardTheme.TextPrimary;

            // Unit text
            TextMeshProUGUI unitTMP = null;
            if (!string.IsNullOrEmpty(unit))
            {
                GameObject unitGO = new GameObject("Unit");
                unitGO.transform.SetParent(container.transform, false);

                LayoutElement unitLE = unitGO.AddComponent<LayoutElement>();
                unitLE.preferredHeight = 14;

                unitTMP = unitGO.AddComponent<TextMeshProUGUI>();
                unitTMP.text = unit;
                unitTMP.fontSize = 11;
                unitTMP.fontStyle = FontStyles.Normal;
                unitTMP.alignment = TextAlignmentOptions.Center;
                unitTMP.color = ValidationDashboardTheme.TextSecondary;
            }

            // Label text
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(container.transform, false);

            LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 14;

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 10;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = ValidationDashboardTheme.TextSecondary;

            // Add DigitalReadout component
            DigitalReadout readout = container.AddComponent<DigitalReadout>();
            readout.valueText = valueTMP;
            readout.unitText = unitTMP;
            readout.labelText = labelTMP;
            readout.valueFormat = format;
            readout.unitSuffix = unit;
            readout.valueFontSize = fontSize;
            readout.normalColor = ValidationDashboardTheme.NormalGreen;
            readout.warningColor = ValidationDashboardTheme.WarningAmber;
            readout.alarmColor = ValidationDashboardTheme.AlarmRed;
            readout.neutralColor = ValidationDashboardTheme.TextPrimary;

            return readout;
        }
    }
}
