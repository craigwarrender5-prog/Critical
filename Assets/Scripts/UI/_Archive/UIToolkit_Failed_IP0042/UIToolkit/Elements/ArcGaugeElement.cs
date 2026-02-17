// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Element
// ArcGaugeElement.cs — Custom 180° Arc Gauge with Animated Needle
// ============================================================================
//
// PURPOSE:
//   A custom UI Toolkit VisualElement that renders a 180° arc gauge with:
//   - Configurable min/max value range
//   - Threshold-based color bands (green/amber/red)
//   - Smooth animated needle with SmoothDamp behavior
//
// IP-0042 Stage 0: Proof of Concept
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Critical.UI.UIToolkit.Elements
{
    /// <summary>
    /// A 180° arc gauge with animated needle and threshold coloring.
    /// </summary>
    public class ArcGaugeElement : VisualElement
    {
        // ====================================================================
        // UXML FACTORY
        // ====================================================================
        
        public new class UxmlFactory : UxmlFactory<ArcGaugeElement, UxmlTraits> { }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "GAUGE" };
            UxmlFloatAttributeDescription m_MinValue = new() { name = "min-value", defaultValue = 0f };
            UxmlFloatAttributeDescription m_MaxValue = new() { name = "max-value", defaultValue = 100f };
            UxmlFloatAttributeDescription m_Value = new() { name = "value", defaultValue = 0f };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var gauge = (ArcGaugeElement)ve;
                gauge.Label = m_Label.GetValueFromBag(bag, cc);
                gauge.MinValue = m_MinValue.GetValueFromBag(bag, cc);
                gauge.MaxValue = m_MaxValue.GetValueFromBag(bag, cc);
                gauge.Value = m_Value.GetValueFromBag(bag, cc);
            }
        }
        
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const float ARC_START_ANGLE = 180f;
        private const float ARC_SWEEP = 180f;
        private const float NEEDLE_SMOOTH_TIME = 0.15f;
        private const float UPDATE_INTERVAL_MS = 16f;
        private const float ARC_THICKNESS = 10f;
        private const int ARC_SEGMENTS = 32;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_NORMAL = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.667f, 0f, 1f);
        private static readonly Color COLOR_ALARM = new Color(1f, 0.267f, 0.267f, 1f);
        private static readonly Color COLOR_ARC_BG = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color COLOR_NEEDLE = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color COLOR_CENTER = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private string _label = "GAUGE";
        public string Label
        {
            get => _label;
            set { _label = value; MarkDirtyRepaint(); }
        }
        
        private string _unit = "";
        public string Unit
        {
            get => _unit;
            set { _unit = value; MarkDirtyRepaint(); }
        }
        
        private float _minValue = 0f;
        public float MinValue
        {
            get => _minValue;
            set { _minValue = value; MarkDirtyRepaint(); }
        }
        
        private float _maxValue = 100f;
        public float MaxValue
        {
            get => _maxValue;
            set { _maxValue = value; MarkDirtyRepaint(); }
        }
        
        private float _value = 0f;
        public float Value
        {
            get => _value;
            set 
            { 
                _targetValue = Mathf.Clamp(value, _minValue, _maxValue);
            }
        }
        
        public float DisplayedValue => _displayedValue;
        
        // Thresholds
        private float _warnLow = float.MinValue;
        private float _warnHigh = float.MaxValue;
        private float _alarmLow = float.MinValue;
        private float _alarmHigh = float.MaxValue;
        
        public void SetThresholds(float warnLow = float.MinValue, float warnHigh = float.MaxValue,
                                   float alarmLow = float.MinValue, float alarmHigh = float.MaxValue)
        {
            _warnLow = warnLow;
            _warnHigh = warnHigh;
            _alarmLow = alarmLow;
            _alarmHigh = alarmHigh;
            MarkDirtyRepaint();
        }
        
        // Animation state
        private float _targetValue = 0f;
        private float _displayedValue = 0f;
        private float _needleVelocity = 0f;
        private IVisualElementScheduledItem _animationSchedule;
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public ArcGaugeElement()
        {
            AddToClassList("arc-gauge");
            
            style.minWidth = 120;
            style.minHeight = 100;
            style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            
            generateVisualContent += OnGenerateVisualContent;
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _animationSchedule = schedule.Execute(UpdateAnimation).Every((long)UPDATE_INTERVAL_MS);
            Debug.Log($"[ArcGauge] Attached to panel: {_label}");
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _animationSchedule?.Pause();
            _animationSchedule = null;
        }
        
        private void UpdateAnimation()
        {
            float previousValue = _displayedValue;
            _displayedValue = Mathf.SmoothDamp(_displayedValue, _targetValue, ref _needleVelocity, NEEDLE_SMOOTH_TIME);
            
            if (Mathf.Abs(_displayedValue - previousValue) > 0.001f)
            {
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Rect rect = contentRect;
            
            Debug.Log($"[ArcGauge] OnGenerateVisualContent: {_label}, rect={rect}, value={_displayedValue}");
            
            if (rect.width < 10 || rect.height < 10)
            {
                Debug.LogWarning($"[ArcGauge] Rect too small: {rect}");
                return;
            }
            
            var painter = mgc.painter2D;
            if (painter == null)
            {
                Debug.LogError("[ArcGauge] Painter2D is null!");
                return;
            }
            
            float centerX = rect.width / 2f;
            float centerY = rect.height * 0.65f;
            float radius = Mathf.Min(rect.width / 2f - 15f, rect.height * 0.55f);
            
            if (radius < 15f)
            {
                Debug.LogWarning($"[ArcGauge] Radius too small: {radius}");
                return;
            }
            
            // Draw background arc
            DrawArc(painter, centerX, centerY, radius, 0f, 1f, COLOR_ARC_BG);
            
            // Draw value arc
            float normalizedValue = (_maxValue > _minValue) 
                ? Mathf.Clamp01((_displayedValue - _minValue) / (_maxValue - _minValue)) 
                : 0f;
            
            if (normalizedValue > 0.001f)
            {
                Color valueColor = GetValueColor(_displayedValue);
                DrawArc(painter, centerX, centerY, radius, 0f, normalizedValue, valueColor);
            }
            
            // Draw needle
            DrawNeedle(painter, centerX, centerY, radius * 0.8f, normalizedValue);
            
            // Draw center cap
            DrawCircle(painter, centerX, centerY, 8f, COLOR_NEEDLE);
            DrawCircle(painter, centerX, centerY, 4f, COLOR_CENTER);
        }
        
        private void DrawArc(Painter2D painter, float cx, float cy, float radius, 
                            float startNorm, float endNorm, Color color)
        {
            float startAngle = (ARC_START_ANGLE - startNorm * ARC_SWEEP) * Mathf.Deg2Rad;
            float endAngle = (ARC_START_ANGLE - endNorm * ARC_SWEEP) * Mathf.Deg2Rad;
            
            painter.strokeColor = color;
            painter.lineWidth = ARC_THICKNESS;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), radius, startAngle, endAngle, ArcDirection.CounterClockwise);
            painter.Stroke();
        }
        
        private void DrawNeedle(Painter2D painter, float cx, float cy, float length, float normalizedValue)
        {
            float angleRad = (ARC_START_ANGLE - normalizedValue * ARC_SWEEP) * Mathf.Deg2Rad;
            
            float tipX = cx + Mathf.Cos(angleRad) * length;
            float tipY = cy - Mathf.Sin(angleRad) * length;
            
            // Draw needle as a thick line
            painter.strokeColor = COLOR_NEEDLE;
            painter.lineWidth = 4f;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.MoveTo(new Vector2(cx, cy));
            painter.LineTo(new Vector2(tipX, tipY));
            painter.Stroke();
        }
        
        private void DrawCircle(Painter2D painter, float cx, float cy, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), radius, 0f, Mathf.PI * 2f, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill();
        }
        
        private Color GetValueColor(float value)
        {
            if (value < _alarmLow || value > _alarmHigh)
                return COLOR_ALARM;
            if (value < _warnLow || value > _warnHigh)
                return COLOR_WARNING;
            return COLOR_NORMAL;
        }
    }
}
