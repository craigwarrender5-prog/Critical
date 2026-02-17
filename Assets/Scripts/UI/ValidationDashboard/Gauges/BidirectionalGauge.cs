// ============================================================================
// CRITICAL: Master the Atom - Bidirectional Gauge Component
// BidirectionalGauge.cs - 270° Sweep Center-Zero Gauge
// ============================================================================
//
// PURPOSE:
//   Renders a gauge that can show positive and negative values with:
//   - 270° total sweep with zero at 12 o'clock (top center)
//   - Positive values sweep clockwise (right side - blue)
//   - Negative values sweep counter-clockwise (left side - orange)
//   - Animated needle with smooth interpolation
//   - Ideal for surge flow, net CVCS flow, pressure rate, etc.
//
// VISUAL DESIGN:
//   ┌─────────────────────────────────┐
//   │            ▲ (0)                │
//   │         ╭──┼──╮                 │
//   │       ╱   │    ╲                │
//   │      ╱    │     ╲               │
//   │ (-) ╱     │      ╲ (+)          │
//   │    │      │       │             │
//   │    │      ●       │             │
//   │     ╲           ╱               │
//   │       ╲       ╱                 │
//   │         ╰───╯                   │
//   │         +125.3 GPM              │
//   │         SURGE FLOW              │
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
    /// 270° bidirectional gauge with center-zero and dual-color arcs.
    /// </summary>
    public class BidirectionalGauge : MaskableGraphic
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Value Range")]
        [Tooltip("Maximum absolute value (gauge shows -maxValue to +maxValue)")]
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private float currentValue = 0f;

        [Header("Arc Settings")]
        [SerializeField] private float arcWidth = 8f;
        [SerializeField] private float arcRadius = 50f;
        [SerializeField] private int arcSegments = 80;
        [SerializeField] private float gapDegrees = 90f; // Gap at bottom (total 360 - 270 = 90)

        [Header("Needle Settings")]
        [SerializeField] private float needleLength = 45f;
        [SerializeField] private float needleWidth = 3f;
        [SerializeField] private Color needleColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private bool enableAnimation = true;

        [Header("Display")]
        [SerializeField] private string valueFormat = "F1";
        [SerializeField] private string unitSuffix = "";
        [SerializeField] private bool showSign = true;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI labelText;

        [Header("Colors")]
        [SerializeField] private Color positiveColor = new Color32(51, 128, 255, 255);  // Blue
        [SerializeField] private Color negativeColor = new Color32(255, 140, 26, 255);  // Orange
        [SerializeField] private Color arcBackgroundColor = new Color32(38, 41, 51, 255);
        [SerializeField] private Color zeroTickColor = new Color32(100, 105, 120, 255);

        // ====================================================================
        // PRIVATE STATE
        // ====================================================================

        private float _displayValue;
        private float _velocity;
        private float _targetValue;

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        public float DisplayValue => _displayValue;
        public float TargetValue => _targetValue;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        protected override void Awake()
        {
            base.Awake();
            _displayValue = currentValue;
            _targetValue = currentValue;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
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
            SetVerticesDirty();
        }
#endif

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set the gauge value. Positive = clockwise, Negative = counter-clockwise.
        /// </summary>
        public void SetValue(float value)
        {
            _targetValue = Mathf.Clamp(value, -maxValue, maxValue);
            currentValue = _targetValue;
            
            if (!enableAnimation)
            {
                _displayValue = _targetValue;
                _velocity = 0f;
            }
            
            SetVerticesDirty();
            UpdateValueText();
        }

        /// <summary>
        /// Set the maximum absolute value.
        /// </summary>
        public void SetMaxValue(float max)
        {
            maxValue = Mathf.Max(0.001f, max);
            SetVerticesDirty();
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
            Vector2 center = rect.center;

            // Calculate arc angles
            // 270° sweep: from -135° to +135° (0° = top/12 o'clock)
            // Gap is at bottom (180° = 6 o'clock)
            float halfSweep = (360f - gapDegrees) * 0.5f * Mathf.Deg2Rad;
            float topAngle = Mathf.PI * 0.5f; // 90° = 12 o'clock in Unity coords

            // Draw arc background
            DrawArc(vh, center, arcRadius, arcWidth, -halfSweep + topAngle, halfSweep + topAngle, arcBackgroundColor);

            // Draw colored arc based on value
            if (maxValue > 0)
            {
                float normValue = _displayValue / maxValue; // -1 to +1
                float valueAngle = normValue * halfSweep;

                if (_displayValue > 0)
                {
                    // Positive: from zero (top) clockwise
                    DrawArc(vh, center, arcRadius, arcWidth * 0.85f, 
                        topAngle, topAngle - valueAngle, positiveColor);
                }
                else if (_displayValue < 0)
                {
                    // Negative: from zero (top) counter-clockwise
                    DrawArc(vh, center, arcRadius, arcWidth * 0.85f,
                        topAngle, topAngle - valueAngle, negativeColor);
                }
            }

            // Draw zero tick mark
            DrawZeroTick(vh, center, topAngle);

            // Draw needle
            DrawNeedle(vh, center, topAngle, halfSweep);
        }

        private void DrawArc(VertexHelper vh, Vector2 center, float radius, float width,
            float startAngle, float endAngle, Color arcColor)
        {
            float innerRadius = radius - width * 0.5f;
            float outerRadius = radius + width * 0.5f;

            // Ensure we draw in correct direction
            if (startAngle > endAngle)
            {
                float temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }

            float angleRange = endAngle - startAngle;
            int segments = Mathf.Max(1, Mathf.CeilToInt(arcSegments * Mathf.Abs(angleRange) / (Mathf.PI * 2)));
            float angleStep = angleRange / segments;

            int vertStart = vh.currentVertCount;

            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + angleStep * i;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector2 innerPos = center + new Vector2(cos * innerRadius, sin * innerRadius);
                Vector2 outerPos = center + new Vector2(cos * outerRadius, sin * outerRadius);

                UIVertex innerVert = UIVertex.simpleVert;
                innerVert.position = innerPos;
                innerVert.color = arcColor;

                UIVertex outerVert = UIVertex.simpleVert;
                outerVert.position = outerPos;
                outerVert.color = arcColor;

                vh.AddVert(innerVert);
                vh.AddVert(outerVert);

                if (i > 0)
                {
                    int idx = vertStart + (i - 1) * 2;
                    vh.AddTriangle(idx, idx + 1, idx + 3);
                    vh.AddTriangle(idx, idx + 3, idx + 2);
                }
            }
        }

        private void DrawZeroTick(VertexHelper vh, Vector2 center, float topAngle)
        {
            float tickLength = 8f;
            float tickWidth = 2f;
            float innerR = arcRadius - arcWidth * 0.5f - 2f;
            float outerR = innerR + tickLength;

            Vector2 dir = new Vector2(Mathf.Cos(topAngle), Mathf.Sin(topAngle));
            Vector2 perp = new Vector2(-dir.y, dir.x);

            Vector2 p0 = center + dir * innerR - perp * tickWidth * 0.5f;
            Vector2 p1 = center + dir * innerR + perp * tickWidth * 0.5f;
            Vector2 p2 = center + dir * outerR + perp * tickWidth * 0.5f;
            Vector2 p3 = center + dir * outerR - perp * tickWidth * 0.5f;

            int vertStart = vh.currentVertCount;

            UIVertex v0 = UIVertex.simpleVert; v0.position = p0; v0.color = zeroTickColor;
            UIVertex v1 = UIVertex.simpleVert; v1.position = p1; v1.color = zeroTickColor;
            UIVertex v2 = UIVertex.simpleVert; v2.position = p2; v2.color = zeroTickColor;
            UIVertex v3 = UIVertex.simpleVert; v3.position = p3; v3.color = zeroTickColor;

            vh.AddVert(v0);
            vh.AddVert(v1);
            vh.AddVert(v2);
            vh.AddVert(v3);

            vh.AddTriangle(vertStart, vertStart + 1, vertStart + 2);
            vh.AddTriangle(vertStart, vertStart + 2, vertStart + 3);
        }

        private void DrawNeedle(VertexHelper vh, Vector2 center, float topAngle, float halfSweep)
        {
            if (maxValue <= 0) return;

            float normValue = Mathf.Clamp(_displayValue / maxValue, -1f, 1f);
            
            // Needle angle: 0 at top, positive = clockwise, negative = counter-clockwise
            float needleAngle = topAngle - (normValue * halfSweep);

            Vector2 direction = new Vector2(Mathf.Cos(needleAngle), Mathf.Sin(needleAngle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            Vector2 tip = center + direction * needleLength;
            Vector2 baseLeft = center + perpendicular * (needleWidth * 0.5f) - direction * (needleWidth * 0.3f);
            Vector2 baseRight = center - perpendicular * (needleWidth * 0.5f) - direction * (needleWidth * 0.3f);

            int vertStart = vh.currentVertCount;

            // Needle color based on value sign
            Color nColor = _displayValue >= 0 ? 
                Color.Lerp(needleColor, positiveColor, 0.3f) : 
                Color.Lerp(needleColor, negativeColor, 0.3f);

            UIVertex v0 = UIVertex.simpleVert; v0.position = tip; v0.color = nColor;
            UIVertex v1 = UIVertex.simpleVert; v1.position = baseLeft; v1.color = nColor;
            UIVertex v2 = UIVertex.simpleVert; v2.position = baseRight; v2.color = nColor;

            vh.AddVert(v0);
            vh.AddVert(v1);
            vh.AddVert(v2);
            vh.AddTriangle(vertStart, vertStart + 1, vertStart + 2);

            // Center cap
            DrawCircle(vh, center, needleWidth * 0.8f, needleColor, 12);
        }

        private void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color circleColor, int segments)
        {
            int centerIdx = vh.currentVertCount;

            UIVertex centerVert = UIVertex.simpleVert;
            centerVert.position = center;
            centerVert.color = circleColor;
            vh.AddVert(centerVert);

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                UIVertex vert = UIVertex.simpleVert;
                vert.position = pos;
                vert.color = circleColor;
                vh.AddVert(vert);

                if (i > 0)
                {
                    vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
                }
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private void UpdateValueText()
        {
            if (valueText != null)
            {
                string sign = showSign && _displayValue > 0 ? "+" : "";
                valueText.text = sign + _displayValue.ToString(valueFormat) + unitSuffix;
                valueText.color = _displayValue >= 0 ? positiveColor : negativeColor;
            }
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a BidirectionalGauge programmatically.
        /// </summary>
        public static BidirectionalGauge Create(Transform parent, string label, float maxAbsValue, string unit = "")
        {
            GameObject container = new GameObject($"BiGauge_{label}");
            container.transform.SetParent(parent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(
                ValidationDashboardTheme.GaugeArcDiameter + 20,
                ValidationDashboardTheme.GaugeArcDiameter + 40);

            // Gauge graphic
            GameObject gaugeGO = new GameObject("Gauge");
            gaugeGO.transform.SetParent(container.transform, false);

            RectTransform gaugeRT = gaugeGO.AddComponent<RectTransform>();
            gaugeRT.anchorMin = new Vector2(0.5f, 0.5f);
            gaugeRT.anchorMax = new Vector2(0.5f, 0.5f);
            gaugeRT.sizeDelta = new Vector2(
                ValidationDashboardTheme.GaugeArcDiameter,
                ValidationDashboardTheme.GaugeArcDiameter);
            gaugeRT.anchoredPosition = new Vector2(0, 5);

            BidirectionalGauge gauge = gaugeGO.AddComponent<BidirectionalGauge>();
            gauge.maxValue = maxAbsValue;
            gauge.unitSuffix = unit;
            gauge.arcRadius = ValidationDashboardTheme.GaugeArcDiameter * 0.4f;
            gauge.arcWidth = ValidationDashboardTheme.GaugeArcWidth;
            gauge.needleLength = ValidationDashboardTheme.GaugeArcDiameter * 0.32f;
            gauge.needleWidth = ValidationDashboardTheme.GaugeNeedleWidth;
            gauge.positiveColor = ValidationDashboardTheme.AccentBlue;
            gauge.negativeColor = ValidationDashboardTheme.AccentOrange;
            gauge.arcBackgroundColor = ValidationDashboardTheme.GaugeArcBackground;
            gauge.needleColor = ValidationDashboardTheme.GaugeNeedle;

            // Value text
            GameObject valueGO = new GameObject("ValueText");
            valueGO.transform.SetParent(container.transform, false);

            RectTransform valueRT = valueGO.AddComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0.5f, 0);
            valueRT.anchorMax = new Vector2(0.5f, 0);
            valueRT.pivot = new Vector2(0.5f, 1);
            valueRT.sizeDelta = new Vector2(100, 24);
            valueRT.anchoredPosition = new Vector2(0, 25);

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.alignment = TextAlignmentOptions.Center;
            InstrumentFontHelper.ApplyInstrumentStyle(valueTMP, 16f);
            gauge.valueText = valueTMP;

            // Recessed backing behind digital readout
            InstrumentFontHelper.CreateRecessedBacking(valueGO.transform, 96, 22);

            // Label text
            GameObject labelGO = new GameObject("LabelText");
            labelGO.transform.SetParent(container.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 0);
            labelRT.anchorMax = new Vector2(0.5f, 0);
            labelRT.pivot = new Vector2(0.5f, 1);
            labelRT.sizeDelta = new Vector2(120, 16);
            labelRT.anchoredPosition = new Vector2(0, 5);

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 10;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = ValidationDashboardTheme.TextSecondary;
            gauge.labelText = labelTMP;

            return gauge;
        }
    }
}
