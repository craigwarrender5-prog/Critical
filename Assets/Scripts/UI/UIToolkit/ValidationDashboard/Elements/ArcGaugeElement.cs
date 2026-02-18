// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// ArcGaugeElement.cs — Professional Arc Gauge with Animated Needle
// ============================================================================
//
// PURPOSE:
//   A 270° arc gauge control for UI Toolkit with:
//   - Smooth needle animation with configurable damping
//   - Threshold-based color zones (normal/warning/alarm)
//   - Digital value readout with units
//   - Label display
//   - USS-styleable colors via custom properties
//
// DESIGN:
//   ┌─────────────────────────────────┐
//   │         ╭───────────╮           │
//   │       ╱   ▲           ╲         │
//   │      ╱    │            ╲        │
//   │     ╱     │             ╲       │
//   │    ╱      │              ╲      │
//   │            ●                    │
//   │         557.4°F                 │
//   │           T_AVG                 │
//   └─────────────────────────────────┘
//
// VERSION: 1.0.0
// DATE: 2026-02-18
// CS: CS-0127 Stage 1
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// Professional arc gauge element with animated needle and threshold coloring.
    /// Uses Painter2D for smooth vector rendering.
    /// </summary>
    [UxmlElement]
    public partial class ArcGaugeElement : VisualElement
    {
        // ====================================================================
        // USS CLASS NAMES
        // ====================================================================
        
        public new static readonly string ussClassName = "arc-gauge";
        public static readonly string ussLabelClassName = "arc-gauge__label";
        public static readonly string ussValueClassName = "arc-gauge__value";
        
        // ====================================================================
        // ARC GEOMETRY (DEGREES)
        // ====================================================================
        
        private const float ARC_START_ANGLE = 135f;   // 7:30 position
        private const float ARC_END_ANGLE = 405f;     // 4:30 position (45° + 360°)
        private const float ARC_SWEEP = 270f;         // Total arc sweep
        
        // ====================================================================
        // USS CUSTOM STYLE PROPERTIES
        // ====================================================================
        
        private static readonly CustomStyleProperty<Color> s_TrackColor = 
            new CustomStyleProperty<Color>("--track-color");
        private static readonly CustomStyleProperty<Color> s_NormalColor = 
            new CustomStyleProperty<Color>("--normal-color");
        private static readonly CustomStyleProperty<Color> s_WarningColor = 
            new CustomStyleProperty<Color>("--warning-color");
        private static readonly CustomStyleProperty<Color> s_AlarmColor = 
            new CustomStyleProperty<Color>("--alarm-color");
        private static readonly CustomStyleProperty<Color> s_NeedleColor = 
            new CustomStyleProperty<Color>("--needle-color");
        private static readonly CustomStyleProperty<float> s_ArcWidth = 
            new CustomStyleProperty<float>("--arc-width");
        
        // ====================================================================
        // COLORS (defaults, overrideable via USS)
        // ====================================================================
        
        private Color _trackColor = new Color(0.15f, 0.16f, 0.20f, 1f);
        private Color _normalColor = new Color(0.18f, 0.85f, 0.25f, 1f);
        private Color _warningColor = new Color(1f, 0.78f, 0f, 1f);
        private Color _alarmColor = new Color(1f, 0.18f, 0.18f, 1f);
        private Color _needleColor = Color.white;
        private Color _centerDotColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        private float _arcWidth = 10f;
        
        // ====================================================================
        // VALUE & THRESHOLDS
        // ====================================================================
        
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private float _value = 0f;
        private float _displayValue = 0f;  // Animated value
        
        private float _warningLow = float.MinValue;
        private float _warningHigh = float.MaxValue;
        private float _alarmLow = float.MinValue;
        private float _alarmHigh = float.MaxValue;
        
        // ====================================================================
        // ANIMATION
        // ====================================================================
        
        private float _velocity = 0f;
        private float _smoothTime = 0.08f;
        private bool _animationEnabled = true;
        private long _lastUpdateTicks;
        
        // ====================================================================
        // DISPLAY
        // ====================================================================
        
        private string _label = "GAUGE";
        private string _unit = "";
        private string _valueFormat = "F1";
        
        // Child elements for label and value
        private Label _labelElement;
        private Label _valueElement;
        
        // ====================================================================
        // UXML ATTRIBUTES
        // ====================================================================
        
        [UxmlAttribute]
        public float minValue
        {
            get => _minValue;
            set { _minValue = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public float maxValue
        {
            get => _maxValue;
            set { _maxValue = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public float value
        {
            get => _value;
            set => SetValue(value);
        }
        
        [UxmlAttribute]
        public string label
        {
            get => _label;
            set
            {
                _label = value;
                if (_labelElement != null)
                    _labelElement.text = value;
            }
        }
        
        [UxmlAttribute]
        public string unit
        {
            get => _unit;
            set
            {
                _unit = value;
                UpdateValueText();
            }
        }
        
        [UxmlAttribute]
        public string valueFormat
        {
            get => _valueFormat;
            set
            {
                _valueFormat = value;
                UpdateValueText();
            }
        }
        
        [UxmlAttribute]
        public float smoothTime
        {
            get => _smoothTime;
            set => _smoothTime = Mathf.Max(0.01f, value);
        }
        
        [UxmlAttribute]
        public bool animationEnabled
        {
            get => _animationEnabled;
            set => _animationEnabled = value;
        }
        
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================
        
        /// <summary>Current displayed value (may lag during animation).</summary>
        public float DisplayValue => _displayValue;
        
        /// <summary>Current arc color based on value thresholds.</summary>
        public Color CurrentColor => GetValueColor(_displayValue);
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public ArcGaugeElement()
        {
            AddToClassList(ussClassName);
            
            // Set default size
            style.minWidth = 100;
            style.minHeight = 120;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Center;
            
            // Create value label (positioned at bottom of gauge)
            _valueElement = new Label("0.0");
            _valueElement.AddToClassList(ussValueClassName);
            _valueElement.style.fontSize = 16;
            _valueElement.style.color = _normalColor;
            _valueElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            _valueElement.style.marginTop = 0;
            
            // Create label
            _labelElement = new Label(_label);
            _labelElement.AddToClassList(ussLabelClassName);
            _labelElement.style.fontSize = 10;
            _labelElement.style.color = new Color(0.55f, 0.58f, 0.65f, 1f);
            _labelElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            _labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Add children (they'll be positioned below the arc)
            Add(_valueElement);
            Add(_labelElement);
            
            // Register callbacks
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStylesResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            generateVisualContent += OnGenerateVisualContent;
            
            // Register for updates
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
            _lastUpdateTicks = DateTime.Now.Ticks;
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Set the gauge value with optional immediate update (no animation).
        /// </summary>
        public void SetValue(float newValue, bool immediate = false)
        {
            float clamped = Mathf.Clamp(newValue, _minValue, _maxValue);
            _value = clamped;
            
            if (immediate || !_animationEnabled)
            {
                _displayValue = clamped;
                _velocity = 0f;
            }
            
            UpdateValueText();
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Set all threshold values at once.
        /// </summary>
        public void SetThresholds(float warningLow, float warningHigh, float alarmLow, float alarmHigh)
        {
            _warningLow = warningLow;
            _warningHigh = warningHigh;
            _alarmLow = alarmLow;
            _alarmHigh = alarmHigh;
            UpdateValueText();
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Set only high thresholds (for parameters where high is bad).
        /// </summary>
        public void SetHighThresholds(float warning, float alarm)
        {
            _warningLow = float.MinValue;
            _alarmLow = float.MinValue;
            _warningHigh = warning;
            _alarmHigh = alarm;
            UpdateValueText();
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Set only low thresholds (for parameters where low is bad).
        /// </summary>
        public void SetLowThresholds(float warning, float alarm)
        {
            _warningLow = warning;
            _alarmLow = alarm;
            _warningHigh = float.MaxValue;
            _alarmHigh = float.MaxValue;
            UpdateValueText();
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Force an animation update tick. Call this from the controller's update loop.
        /// </summary>
        public void UpdateAnimation()
        {
            if (!_animationEnabled) return;
            
            if (Mathf.Abs(_displayValue - _value) > 0.001f)
            {
                long nowTicks = DateTime.Now.Ticks;
                float dt = (nowTicks - _lastUpdateTicks) / 10000000f; // Ticks to seconds
                _lastUpdateTicks = nowTicks;
                
                dt = Mathf.Clamp(dt, 0.001f, 0.1f); // Clamp to reasonable range
                
                _displayValue = Mathf.SmoothDamp(_displayValue, _value, ref _velocity, _smoothTime, 
                    float.MaxValue, dt);
                
                // Snap when very close
                if (Mathf.Abs(_displayValue - _value) < 0.01f)
                {
                    _displayValue = _value;
                    _velocity = 0f;
                }
                
                UpdateValueText();
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            // Reserve space for text elements at bottom
            float textHeight = 40f;
            float gaugeHeight = height - textHeight;
            
            if (width < 20f || gaugeHeight < 20f)
                return;
            
            var painter = mgc.painter2D;
            if (painter == null) return;
            
            // Calculate layout - center the arc in the gauge area
            Vector2 center = new Vector2(width * 0.5f, gaugeHeight * 0.55f);
            float radius = Mathf.Min(width, gaugeHeight) * 0.38f;
            
            // Calculate normalized and angle values
            float range = _maxValue - _minValue;
            float normalizedValue = (range > 0f) ? (_displayValue - _minValue) / range : 0f;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            float valueAngle = ARC_START_ANGLE + (ARC_SWEEP * normalizedValue);
            
            Color valueColor = GetValueColor(_displayValue);
            
            // 1. Draw background track arc
            painter.strokeColor = _trackColor;
            painter.lineWidth = _arcWidth;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.Arc(center, radius, ARC_START_ANGLE, ARC_END_ANGLE);
            painter.Stroke();
            
            // 2. Draw value arc
            if (normalizedValue > 0.001f)
            {
                painter.strokeColor = valueColor;
                painter.lineWidth = _arcWidth;
                painter.lineCap = LineCap.Round;
                
                painter.BeginPath();
                painter.Arc(center, radius, ARC_START_ANGLE, valueAngle);
                painter.Stroke();
            }
            
            // 3. Draw needle
            float needleAngleRad = valueAngle * Mathf.Deg2Rad;
            float needleLength = radius * 0.78f;
            
            Vector2 needleTip = new Vector2(
                center.x + Mathf.Cos(needleAngleRad) * needleLength,
                center.y + Mathf.Sin(needleAngleRad) * needleLength
            );
            
            painter.strokeColor = _needleColor;
            painter.lineWidth = 2.5f;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(needleTip);
            painter.Stroke();
            
            // 4. Draw center cap
            painter.fillColor = _needleColor;
            painter.BeginPath();
            painter.Arc(center, 5f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            painter.fillColor = _centerDotColor;
            painter.BeginPath();
            painter.Arc(center, 2.5f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
        
        // ====================================================================
        // HELPERS
        // ====================================================================
        
        private Color GetValueColor(float val)
        {
            if (val < _alarmLow || val > _alarmHigh)
                return _alarmColor;
            if (val < _warningLow || val > _warningHigh)
                return _warningColor;
            return _normalColor;
        }
        
        private void UpdateValueText()
        {
            if (_valueElement != null)
            {
                string text = _displayValue.ToString(_valueFormat);
                if (!string.IsNullOrEmpty(_unit))
                    text += _unit;
                _valueElement.text = text;
                _valueElement.style.color = GetValueColor(_displayValue);
            }
        }
        
        // ====================================================================
        // CALLBACKS
        // ====================================================================
        
        private void OnCustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            bool repaint = false;
            
            if (customStyle.TryGetValue(s_TrackColor, out var trackColor))
            {
                _trackColor = trackColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_NormalColor, out var normalColor))
            {
                _normalColor = normalColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_WarningColor, out var warningColor))
            {
                _warningColor = warningColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_AlarmColor, out var alarmColor))
            {
                _alarmColor = alarmColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_NeedleColor, out var needleColor))
            {
                _needleColor = needleColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_ArcWidth, out var arcWidth))
            {
                _arcWidth = arcWidth;
                repaint = true;
            }
            
            if (repaint)
                MarkDirtyRepaint();
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _lastUpdateTicks = DateTime.Now.Ticks;
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Cleanup if needed
        }
    }
}
