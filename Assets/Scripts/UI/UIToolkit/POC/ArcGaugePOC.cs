// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// ArcGaugePOC.cs — Arc Gauge with CORRECTED Painter2D Arc() usage
// ============================================================================
//
// PURPOSE:
//   Prove that Painter2D.Arc() works correctly in Unity 6.3 when given
//   angles in DEGREES (not radians).
//
// KEY FIX FROM IP-0042 FAILURE:
//   The previous attempt multiplied angles by Mathf.Deg2Rad before passing
//   to Arc(). This caused Arc() to interpret values like 3.14 as 3.14 DEGREES
//   instead of 180 degrees, resulting in a near-zero sweep (line/dot).
//
// CORRECT USAGE:
//   painter.Arc(center, radius, startAngleDEGREES, endAngleDEGREES);
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    /// <summary>
    /// A simple 270° arc gauge for UI Toolkit proof of concept.
    /// Uses [UxmlElement] attribute (Unity 6 pattern, not UxmlFactory).
    /// </summary>
    [UxmlElement]
    public partial class ArcGaugePOC : VisualElement
    {
        // ====================================================================
        // CONSTANTS — Arc geometry in DEGREES (critical!)
        // ====================================================================
        
        // Unity coordinate system: 0° = right (3 o'clock), angles increase clockwise
        // Gauge arc: 135° (7:30) to 405° (4:30) = 270° sweep with gap at bottom
        private const float ARC_START_ANGLE = 135f;   // degrees - start at 7:30 position
        private const float ARC_END_ANGLE = 405f;     // degrees - end at 4:30 position (45° + 360°)
        private const float ARC_SWEEP = 270f;         // total arc sweep in degrees
        private const float ARC_THICKNESS = 10f;      // stroke width in pixels
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_TRACK = new Color(0.25f, 0.25f, 0.3f, 1f);
        private static readonly Color COLOR_NORMAL = new Color(0f, 1f, 0.533f, 1f);     // Green
        private static readonly Color COLOR_WARNING = new Color(1f, 0.667f, 0f, 1f);    // Amber
        private static readonly Color COLOR_ALARM = new Color(1f, 0.267f, 0.267f, 1f);  // Red
        private static readonly Color COLOR_NEEDLE = Color.white;
        private static readonly Color COLOR_CENTER = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private float _value = 50f;
        private string _label = "GAUGE";
        
        // Thresholds for color changes
        private float _warningLow = float.MinValue;
        private float _warningHigh = float.MaxValue;
        private float _alarmLow = float.MinValue;
        private float _alarmHigh = float.MaxValue;
        
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
            set 
            { 
                float clamped = Mathf.Clamp(value, _minValue, _maxValue);
                if (Mathf.Abs(_value - clamped) > 0.001f)
                {
                    _value = clamped; 
                    MarkDirtyRepaint(); 
                }
            }
        }
        
        [UxmlAttribute]
        public string label
        {
            get => _label;
            set { _label = value; MarkDirtyRepaint(); }
        }
        
        /// <summary>
        /// Set threshold values for color transitions.
        /// </summary>
        public void SetThresholds(float warningLow = float.MinValue, float warningHigh = float.MaxValue,
                                  float alarmLow = float.MinValue, float alarmHigh = float.MaxValue)
        {
            _warningLow = warningLow;
            _warningHigh = warningHigh;
            _alarmLow = alarmLow;
            _alarmHigh = alarmHigh;
            MarkDirtyRepaint();
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public ArcGaugePOC()
        {
            // Set minimum size so the gauge has space to render
            style.minWidth = 100;
            style.minHeight = 100;
            
            // Register the visual content generator callback
            generateVisualContent += OnGenerateVisualContent;
            
            // Debug: Log construction
            Debug.Log("[ArcGaugePOC] Element constructed");
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            // Safety check - need minimum size to render
            if (width < 20f || height < 20f)
            {
                Debug.LogWarning($"[ArcGaugePOC] contentRect too small: {width}x{height}");
                return;
            }
            
            var painter = mgc.painter2D;
            if (painter == null)
            {
                Debug.LogError("[ArcGaugePOC] painter2D is null!");
                return;
            }
            
            // Calculate layout
            Vector2 center = new Vector2(width * 0.5f, height * 0.55f);
            float radius = Mathf.Min(width, height) * 0.38f;
            
            // Calculate normalized value (0 to 1)
            float range = _maxValue - _minValue;
            float normalizedValue = (range > 0f) ? (_value - _minValue) / range : 0f;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Calculate angle for current value
            // KEY: Keep everything in DEGREES - Arc() expects degrees!
            float valueAngle = ARC_START_ANGLE + (ARC_SWEEP * normalizedValue);
            
            // Get color based on current value
            Color valueColor = GetValueColor(_value);
            
            // ================================================================
            // 1. Draw background track arc
            // ================================================================
            painter.strokeColor = COLOR_TRACK;
            painter.lineWidth = ARC_THICKNESS;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.Arc(center, radius, ARC_START_ANGLE, ARC_END_ANGLE);
            painter.Stroke();
            
            // ================================================================
            // 2. Draw value arc (colored portion based on value)
            // ================================================================
            if (normalizedValue > 0.001f)
            {
                painter.strokeColor = valueColor;
                painter.lineWidth = ARC_THICKNESS;
                painter.lineCap = LineCap.Round;
                
                painter.BeginPath();
                painter.Arc(center, radius, ARC_START_ANGLE, valueAngle);
                painter.Stroke();
            }
            
            // ================================================================
            // 3. Draw needle
            // ================================================================
            // For the needle position calculation, we need radians for Cos/Sin
            // But we keep the Arc() calls in degrees
            float needleAngleRad = valueAngle * Mathf.Deg2Rad;
            float needleLength = radius * 0.75f;
            
            Vector2 needleTip = new Vector2(
                center.x + Mathf.Cos(needleAngleRad) * needleLength,
                center.y + Mathf.Sin(needleAngleRad) * needleLength
            );
            
            painter.strokeColor = COLOR_NEEDLE;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(needleTip);
            painter.Stroke();
            
            // ================================================================
            // 4. Draw center cap
            // ================================================================
            // Outer white circle
            painter.fillColor = COLOR_NEEDLE;
            painter.BeginPath();
            painter.Arc(center, 6f, 0f, 360f);  // 0° to 360° = full circle
            painter.ClosePath();
            painter.Fill();
            
            // Inner dark circle
            painter.fillColor = COLOR_CENTER;
            painter.BeginPath();
            painter.Arc(center, 3f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
        
        /// <summary>
        /// Determine arc color based on value and thresholds.
        /// </summary>
        private Color GetValueColor(float val)
        {
            if (val < _alarmLow || val > _alarmHigh)
                return COLOR_ALARM;
            if (val < _warningLow || val > _warningHigh)
                return COLOR_WARNING;
            return COLOR_NORMAL;
        }
    }
}
