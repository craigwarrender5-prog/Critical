// ============================================================================
// CRITICAL: Master the Atom - Arc Gauge Component
// ArcGauge.cs - 180° Sweep Gauge with Animated Needle and Glow
// ============================================================================
//
// PURPOSE:
//   Renders a semicircular arc gauge with:
//   - 180° sweep from left to right
//   - Animated needle with smooth interpolation
//   - Color-coded arc segments (green/amber/red zones)
//   - Digital value readout below gauge
//   - Optional glow effect on arc
//   - Configurable min/max range and thresholds
//
// VISUAL DESIGN:
//   ┌─────────────────────────────────┐
//   │         ╭───────────╮           │
//   │       ╱   ▲           ╲         │
//   │      ╱    │            ╲        │
//   │     ╱     │             ╲       │
//   │    ╱      │              ╲      │
//   │   ▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔      │
//   │         2235.0 psig             │
//   │         RCS PRESSURE            │
//   └─────────────────────────────────┘
//
// USAGE:
//   1. Add ArcGauge component to a GameObject with RectTransform
//   2. Configure min/max range, warning/alarm thresholds in Inspector
//   3. Call SetValue(float) to update the gauge
//   4. Needle animates smoothly to new position
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
    /// 180° arc gauge with animated needle and threshold-based coloring.
    /// </summary>
    public class ArcGauge : MaskableGraphic
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Value Range")]
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private float currentValue = 50f;

        [Header("Thresholds")]
        [SerializeField] private float warningLow = 20f;
        [SerializeField] private float warningHigh = 80f;
        [SerializeField] private float alarmLow = 10f;
        [SerializeField] private float alarmHigh = 90f;

        [Header("Arc Settings")]
        [SerializeField] private float arcWidth = 8f;
        [SerializeField] private float arcRadius = 50f;
        [SerializeField] private int arcSegments = 60;

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
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI labelText;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color32(46, 217, 64, 255);
        [SerializeField] private Color warningColor = new Color32(255, 199, 0, 255);
        [SerializeField] private Color alarmColor = new Color32(255, 46, 46, 255);
        [SerializeField] private Color arcBackgroundColor = new Color32(38, 41, 51, 255);

        // ====================================================================
        // PRIVATE STATE
        // ====================================================================

        private float _displayValue;
        private float _velocity;
        private float _targetValue;
        private Color _currentArcColor;
        private bool _needsRebuild = true;

        // Mesh data
        private UIVertex[] _arcVerts;
        private UIVertex[] _needleVerts;

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        /// <summary>Current displayed value (may lag behind target during animation).</summary>
        public float DisplayValue => _displayValue;

        /// <summary>Target value the gauge is animating toward.</summary>
        public float TargetValue => _targetValue;

        /// <summary>Current arc color based on value thresholds.</summary>
        public Color CurrentColor => _currentArcColor;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        protected override void Awake()
        {
            base.Awake();
            _displayValue = currentValue;
            _targetValue = currentValue;
            UpdateArcColor();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _needsRebuild = true;
        }

        void Update()
        {
            if (enableAnimation && !Mathf.Approximately(_displayValue, _targetValue))
            {
                _displayValue = Mathf.SmoothDamp(_displayValue, _targetValue, ref _velocity, smoothTime);
                
                // Snap when very close
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
            UpdateArcColor();
            _needsRebuild = true;
            SetVerticesDirty();
        }
#endif

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set the gauge value. If animation is enabled, needle smoothly transitions.
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
            
            UpdateArcColor();
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
            _needsRebuild = true;
            SetVerticesDirty();
        }

        /// <summary>
        /// Set threshold values for color zones.
        /// </summary>
        public void SetThresholds(float warnLow, float warnHigh, float almLow, float almHigh)
        {
            warningLow = warnLow;
            warningHigh = warnHigh;
            alarmLow = almLow;
            alarmHigh = almHigh;
            UpdateArcColor();
            _needsRebuild = true;
            SetVerticesDirty();
        }

        /// <summary>
        /// Set the label text displayed below the gauge.
        /// </summary>
        public void SetLabel(string label)
        {
            if (labelText != null)
                labelText.text = label;
        }

        /// <summary>
        /// Set the unit suffix for value display.
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
            center.y -= arcRadius * 0.2f; // Offset center down slightly

            // Draw arc background
            DrawArc(vh, center, arcRadius, arcWidth, 0f, 1f, arcBackgroundColor);

            // Draw colored arc segments based on thresholds
            DrawThresholdArc(vh, center);

            // Draw needle
            DrawNeedle(vh, center);
        }

        private void DrawArc(VertexHelper vh, Vector2 center, float radius, float width, 
            float startNorm, float endNorm, Color arcColor)
        {
            float innerRadius = radius - width * 0.5f;
            float outerRadius = radius + width * 0.5f;

            // 180° arc: left to right (π to 0 radians)
            float startAngle = Mathf.PI - (startNorm * Mathf.PI);
            float endAngle = Mathf.PI - (endNorm * Mathf.PI);

            int segments = Mathf.Max(1, Mathf.CeilToInt(arcSegments * Mathf.Abs(endNorm - startNorm)));
            float angleStep = (endAngle - startAngle) / segments;

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

        private void DrawThresholdArc(VertexHelper vh, Vector2 center)
        {
            float range = maxValue - minValue;
            if (range <= 0) return;

            // Normalize threshold positions
            float normAlarmLow = (alarmLow - minValue) / range;
            float normWarnLow = (warningLow - minValue) / range;
            float normWarnHigh = (warningHigh - minValue) / range;
            float normAlarmHigh = (alarmHigh - minValue) / range;
            float normValue = (_displayValue - minValue) / range;

            // Clamp all values
            normAlarmLow = Mathf.Clamp01(normAlarmLow);
            normWarnLow = Mathf.Clamp01(normWarnLow);
            normWarnHigh = Mathf.Clamp01(normWarnHigh);
            normAlarmHigh = Mathf.Clamp01(normAlarmHigh);
            normValue = Mathf.Clamp01(normValue);

            // Draw colored segments up to current value
            // Low alarm zone (red)
            if (normValue > 0 && normAlarmLow > 0)
            {
                float segEnd = Mathf.Min(normValue, normAlarmLow);
                DrawArc(vh, center, arcRadius, arcWidth * 0.9f, 0f, segEnd, alarmColor);
            }

            // Low warning zone (amber)
            if (normValue > normAlarmLow && normWarnLow > normAlarmLow)
            {
                float segStart = Mathf.Max(0, normAlarmLow);
                float segEnd = Mathf.Min(normValue, normWarnLow);
                if (segEnd > segStart)
                    DrawArc(vh, center, arcRadius, arcWidth * 0.9f, segStart, segEnd, warningColor);
            }

            // Normal zone (green)
            if (normValue > normWarnLow && normWarnHigh > normWarnLow)
            {
                float segStart = Mathf.Max(0, normWarnLow);
                float segEnd = Mathf.Min(normValue, normWarnHigh);
                if (segEnd > segStart)
                    DrawArc(vh, center, arcRadius, arcWidth * 0.9f, segStart, segEnd, normalColor);
            }

            // High warning zone (amber)
            if (normValue > normWarnHigh && normAlarmHigh > normWarnHigh)
            {
                float segStart = Mathf.Max(0, normWarnHigh);
                float segEnd = Mathf.Min(normValue, normAlarmHigh);
                if (segEnd > segStart)
                    DrawArc(vh, center, arcRadius, arcWidth * 0.9f, segStart, segEnd, warningColor);
            }

            // High alarm zone (red)
            if (normValue > normAlarmHigh)
            {
                float segStart = Mathf.Max(0, normAlarmHigh);
                float segEnd = normValue;
                if (segEnd > segStart)
                    DrawArc(vh, center, arcRadius, arcWidth * 0.9f, segStart, segEnd, alarmColor);
            }
        }

        private void DrawNeedle(VertexHelper vh, Vector2 center)
        {
            float range = maxValue - minValue;
            if (range <= 0) return;

            float normValue = Mathf.Clamp01((_displayValue - minValue) / range);
            
            // Needle angle: 180° at min (left), 0° at max (right)
            float angle = Mathf.PI - (normValue * Mathf.PI);

            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            // Needle tip
            Vector2 tip = center + direction * needleLength;
            
            // Needle base (slightly behind center for pivot look)
            Vector2 baseLeft = center + perpendicular * (needleWidth * 0.5f) - direction * (needleWidth * 0.3f);
            Vector2 baseRight = center - perpendicular * (needleWidth * 0.5f) - direction * (needleWidth * 0.3f);

            int vertStart = vh.currentVertCount;

            UIVertex v0 = UIVertex.simpleVert;
            v0.position = tip;
            v0.color = needleColor;

            UIVertex v1 = UIVertex.simpleVert;
            v1.position = baseLeft;
            v1.color = needleColor;

            UIVertex v2 = UIVertex.simpleVert;
            v2.position = baseRight;
            v2.color = needleColor;

            vh.AddVert(v0);
            vh.AddVert(v1);
            vh.AddVert(v2);
            vh.AddTriangle(vertStart, vertStart + 1, vertStart + 2);

            // Center cap circle
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

        private void UpdateArcColor()
        {
            if (_displayValue <= alarmLow || _displayValue >= alarmHigh)
                _currentArcColor = alarmColor;
            else if (_displayValue <= warningLow || _displayValue >= warningHigh)
                _currentArcColor = warningColor;
            else
                _currentArcColor = normalColor;
        }

        private void UpdateValueText()
        {
            if (valueText != null)
            {
                valueText.text = _displayValue.ToString(valueFormat) + unitSuffix;
                valueText.color = _currentArcColor;
            }
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create an ArcGauge programmatically.
        /// </summary>
        public static ArcGauge Create(Transform parent, string label, float min, float max,
            float warnLow, float warnHigh, float almLow, float almHigh, string unit = "")
        {
            // Container
            GameObject container = new GameObject($"ArcGauge_{label}");
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
                ValidationDashboardTheme.GaugeArcDiameter * 0.6f);
            gaugeRT.anchoredPosition = new Vector2(0, 10);

            ArcGauge gauge = gaugeGO.AddComponent<ArcGauge>();
            gauge.minValue = min;
            gauge.maxValue = max;
            gauge.warningLow = warnLow;
            gauge.warningHigh = warnHigh;
            gauge.alarmLow = almLow;
            gauge.alarmHigh = almHigh;
            gauge.unitSuffix = unit;
            gauge.arcRadius = ValidationDashboardTheme.GaugeArcDiameter * 0.4f;
            gauge.arcWidth = ValidationDashboardTheme.GaugeArcWidth;
            gauge.needleLength = ValidationDashboardTheme.GaugeArcDiameter * 0.35f;
            gauge.needleWidth = ValidationDashboardTheme.GaugeNeedleWidth;
            gauge.normalColor = ValidationDashboardTheme.NormalGreen;
            gauge.warningColor = ValidationDashboardTheme.WarningAmber;
            gauge.alarmColor = ValidationDashboardTheme.AlarmRed;
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
            valueRT.anchoredPosition = new Vector2(0, 30);

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.alignment = TextAlignmentOptions.Center;
            InstrumentFontHelper.ApplyInstrumentStyle(valueTMP, 18f);
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
            labelRT.anchoredPosition = new Vector2(0, 8);

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
