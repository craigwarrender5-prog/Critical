// ============================================================================
// CRITICAL: Master the Atom - Linear Gauge Component
// LinearGauge.cs - Horizontal or Vertical Bar Gauge
// ============================================================================
//
// PURPOSE:
//   Renders a linear bar gauge with:
//   - Horizontal or vertical orientation
//   - Gradient fill with threshold-based coloring
//   - Optional tick marks and scale labels
//   - Animated fill with smooth interpolation
//   - Digital value readout
//
// VISUAL DESIGN (Horizontal):
//   ┌─────────────────────────────────────────────────────────┐
//   │  VCT LEVEL                                              │
//   │  ┌─────────────────────────────────────────────────────┐│
//   │  │████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░││
//   │  └─────────────────────────────────────────────────────┘│
//   │  0%                          50%                   100% │
//   │                          42.5%                          │
//   └─────────────────────────────────────────────────────────┘
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
    /// Linear bar gauge with threshold-based coloring.
    /// </summary>
    public class LinearGauge : MaskableGraphic
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Orientation")]
        [SerializeField] private bool isVertical = false;

        [Header("Value Range")]
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private float currentValue = 50f;

        [Header("Thresholds")]
        [SerializeField] private float warningLow = 20f;
        [SerializeField] private float warningHigh = 80f;
        [SerializeField] private float alarmLow = 10f;
        [SerializeField] private float alarmHigh = 90f;

        [Header("Bar Settings")]
        [SerializeField] private float barThickness = 16f;
        [SerializeField] private float borderWidth = 1f;
        [SerializeField] private Color borderColor = new Color32(60, 65, 80, 255);

        [Header("Animation")]
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private bool enableAnimation = true;

        [Header("Display")]
        [SerializeField] private string valueFormat = "F1";
        [SerializeField] private string unitSuffix = "%";
        [SerializeField] private bool showTickMarks = true;
        [SerializeField] private int tickCount = 5;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI labelText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color32(46, 217, 64, 255);
        [SerializeField] private Color warningColor = new Color32(255, 199, 0, 255);
        [SerializeField] private Color alarmColor = new Color32(255, 46, 46, 255);
        [SerializeField] private Color backgroundColor = new Color32(38, 41, 51, 255);
        [SerializeField] private Color tickColor = new Color32(80, 85, 100, 255);

        // ====================================================================
        // PRIVATE STATE
        // ====================================================================

        private float _displayValue;
        private float _velocity;
        private float _targetValue;
        private Color _currentColor;

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        public float DisplayValue => _displayValue;
        public float TargetValue => _targetValue;
        public Color CurrentColor => _currentColor;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        protected override void Awake()
        {
            base.Awake();
            _displayValue = currentValue;
            _targetValue = currentValue;
            UpdateColor();
        }

        void Update()
        {
            if (enableAnimation && !Mathf.Approximately(_displayValue, _targetValue))
            {
                _displayValue = Mathf.SmoothDamp(_displayValue, _targetValue, ref _velocity, smoothTime);
                
                if (Mathf.Abs(_displayValue - _targetValue) < 0.001f)
                {
                    _displayValue = _targetValue;
                    _velocity = 0f;
                }
                
                UpdateColor();
                SetVerticesDirty();
                UpdateValueText();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _targetValue = currentValue;
            if (!enableAnimation)
                _displayValue = currentValue;
            UpdateColor();
            SetVerticesDirty();
        }
#endif

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set the gauge value.
        /// </summary>
        public void SetValue(float value)
        {
            _targetValue = Mathf.Clamp(value, minValue, maxValue);
            currentValue = _targetValue;
            
            if (!enableAnimation)
            {
                _displayValue = _targetValue;
                _velocity = 0f;
            }
            
            UpdateColor();
            SetVerticesDirty();
            UpdateValueText();
        }

        /// <summary>
        /// Set value range.
        /// </summary>
        public void SetRange(float min, float max)
        {
            minValue = min;
            maxValue = max;
            SetVerticesDirty();
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
            UpdateColor();
            SetVerticesDirty();
        }

        /// <summary>
        /// Set label text.
        /// </summary>
        public void SetLabel(string label)
        {
            if (labelText != null)
                labelText.text = label;
        }

        /// <summary>
        /// Set unit suffix.
        /// </summary>
        public void SetUnit(string unit)
        {
            unitSuffix = unit;
            UpdateValueText();
        }

        // ====================================================================
        // RENDERING
        // ====================================================================

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();

            // Draw background
            DrawRect(vh, rect, backgroundColor);

            // Draw border
            if (borderWidth > 0)
            {
                DrawBorder(vh, rect, borderWidth, borderColor);
            }

            // Calculate fill rect
            float range = maxValue - minValue;
            float fillPercent = range > 0 ? Mathf.Clamp01((_displayValue - minValue) / range) : 0;

            Rect fillRect;
            if (isVertical)
            {
                float fillHeight = (rect.height - borderWidth * 2) * fillPercent;
                fillRect = new Rect(
                    rect.x + borderWidth,
                    rect.y + borderWidth,
                    rect.width - borderWidth * 2,
                    fillHeight);
            }
            else
            {
                float fillWidth = (rect.width - borderWidth * 2) * fillPercent;
                fillRect = new Rect(
                    rect.x + borderWidth,
                    rect.y + borderWidth,
                    fillWidth,
                    rect.height - borderWidth * 2);
            }

            // Draw fill
            if (fillRect.width > 0 && fillRect.height > 0)
            {
                DrawRect(vh, fillRect, _currentColor);
            }

            // Draw tick marks
            if (showTickMarks && tickCount > 1)
            {
                DrawTickMarks(vh, rect);
            }
        }

        private void DrawRect(VertexHelper vh, Rect rect, Color rectColor)
        {
            int vertStart = vh.currentVertCount;

            UIVertex v0 = UIVertex.simpleVert;
            v0.position = new Vector2(rect.xMin, rect.yMin);
            v0.color = rectColor;

            UIVertex v1 = UIVertex.simpleVert;
            v1.position = new Vector2(rect.xMin, rect.yMax);
            v1.color = rectColor;

            UIVertex v2 = UIVertex.simpleVert;
            v2.position = new Vector2(rect.xMax, rect.yMax);
            v2.color = rectColor;

            UIVertex v3 = UIVertex.simpleVert;
            v3.position = new Vector2(rect.xMax, rect.yMin);
            v3.color = rectColor;

            vh.AddVert(v0);
            vh.AddVert(v1);
            vh.AddVert(v2);
            vh.AddVert(v3);

            vh.AddTriangle(vertStart, vertStart + 1, vertStart + 2);
            vh.AddTriangle(vertStart, vertStart + 2, vertStart + 3);
        }

        private void DrawBorder(VertexHelper vh, Rect rect, float width, Color bColor)
        {
            // Top border
            DrawRect(vh, new Rect(rect.x, rect.yMax - width, rect.width, width), bColor);
            // Bottom border
            DrawRect(vh, new Rect(rect.x, rect.y, rect.width, width), bColor);
            // Left border
            DrawRect(vh, new Rect(rect.x, rect.y, width, rect.height), bColor);
            // Right border
            DrawRect(vh, new Rect(rect.xMax - width, rect.y, width, rect.height), bColor);
        }

        private void DrawTickMarks(VertexHelper vh, Rect rect)
        {
            float tickLength = 4f;
            float tickWidth = 1f;

            for (int i = 0; i <= tickCount; i++)
            {
                float t = (float)i / tickCount;
                
                Rect tickRect;
                if (isVertical)
                {
                    float y = rect.y + rect.height * t;
                    tickRect = new Rect(rect.xMax - tickLength, y - tickWidth * 0.5f, tickLength, tickWidth);
                }
                else
                {
                    float x = rect.x + rect.width * t;
                    tickRect = new Rect(x - tickWidth * 0.5f, rect.y, tickWidth, tickLength);
                }
                
                DrawRect(vh, tickRect, tickColor);
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private void UpdateColor()
        {
            if (_displayValue <= alarmLow || _displayValue >= alarmHigh)
                _currentColor = alarmColor;
            else if (_displayValue <= warningLow || _displayValue >= warningHigh)
                _currentColor = warningColor;
            else
                _currentColor = normalColor;
        }

        private void UpdateValueText()
        {
            if (valueText != null)
            {
                valueText.text = _displayValue.ToString(valueFormat) + unitSuffix;
                valueText.color = _currentColor;
            }
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a LinearGauge programmatically.
        /// </summary>
        public static LinearGauge Create(Transform parent, string label, float min, float max,
            bool vertical = false, float warnLow = -1, float warnHigh = -1, float almLow = -1, float almHigh = -1)
        {
            // Auto-set thresholds if not specified
            if (warnLow < 0) warnLow = min + (max - min) * 0.2f;
            if (warnHigh < 0) warnHigh = min + (max - min) * 0.8f;
            if (almLow < 0) almLow = min + (max - min) * 0.1f;
            if (almHigh < 0) almHigh = min + (max - min) * 0.9f;

            GameObject container = new GameObject($"LinearGauge_{label}");
            container.transform.SetParent(parent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            if (vertical)
            {
                containerRT.sizeDelta = new Vector2(60, 120);
            }
            else
            {
                containerRT.sizeDelta = new Vector2(150, 50);
            }

            // Label at top
            GameObject labelGO = new GameObject("LabelText");
            labelGO.transform.SetParent(container.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 1);
            labelRT.anchorMax = new Vector2(1, 1);
            labelRT.pivot = new Vector2(0.5f, 1);
            labelRT.sizeDelta = new Vector2(0, 14);
            labelRT.anchoredPosition = Vector2.zero;

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 10;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = ValidationDashboardTheme.TextSecondary;

            // Gauge bar
            GameObject gaugeGO = new GameObject("Gauge");
            gaugeGO.transform.SetParent(container.transform, false);

            RectTransform gaugeRT = gaugeGO.AddComponent<RectTransform>();
            if (vertical)
            {
                gaugeRT.anchorMin = new Vector2(0.5f, 0);
                gaugeRT.anchorMax = new Vector2(0.5f, 1);
                gaugeRT.pivot = new Vector2(0.5f, 0.5f);
                gaugeRT.sizeDelta = new Vector2(ValidationDashboardTheme.LinearGaugeHeight, -30);
                gaugeRT.anchoredPosition = new Vector2(0, -8);
            }
            else
            {
                gaugeRT.anchorMin = new Vector2(0, 0.5f);
                gaugeRT.anchorMax = new Vector2(1, 0.5f);
                gaugeRT.pivot = new Vector2(0.5f, 0.5f);
                gaugeRT.sizeDelta = new Vector2(-10, ValidationDashboardTheme.LinearGaugeHeight);
                gaugeRT.anchoredPosition = new Vector2(0, 2);
            }

            LinearGauge gauge = gaugeGO.AddComponent<LinearGauge>();
            gauge.isVertical = vertical;
            gauge.minValue = min;
            gauge.maxValue = max;
            gauge.warningLow = warnLow;
            gauge.warningHigh = warnHigh;
            gauge.alarmLow = almLow;
            gauge.alarmHigh = almHigh;
            gauge.normalColor = ValidationDashboardTheme.NormalGreen;
            gauge.warningColor = ValidationDashboardTheme.WarningAmber;
            gauge.alarmColor = ValidationDashboardTheme.AlarmRed;
            gauge.backgroundColor = ValidationDashboardTheme.GaugeArcBackground;
            gauge.labelText = labelTMP;

            // Value text at bottom
            GameObject valueGO = new GameObject("ValueText");
            valueGO.transform.SetParent(container.transform, false);

            RectTransform valueRT = valueGO.AddComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0, 0);
            valueRT.anchorMax = new Vector2(1, 0);
            valueRT.pivot = new Vector2(0.5f, 0);
            valueRT.sizeDelta = new Vector2(0, 16);
            valueRT.anchoredPosition = Vector2.zero;

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.fontSize = 12;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.alignment = TextAlignmentOptions.Center;
            valueTMP.color = ValidationDashboardTheme.TextPrimary;
            gauge.valueText = valueTMP;

            return gauge;
        }
    }
}
