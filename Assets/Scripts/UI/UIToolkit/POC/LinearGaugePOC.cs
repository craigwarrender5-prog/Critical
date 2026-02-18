// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// LinearGaugePOC.cs — Horizontal/Vertical bar gauge
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    public enum LinearGaugeOrientation
    {
        Horizontal,
        Vertical
    }
    
    [UxmlElement]
    public partial class LinearGaugePOC : VisualElement
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const float BORDER_WIDTH = 1f;
        private const float CORNER_RADIUS = 3f;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_TRACK = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color COLOR_BORDER = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color COLOR_NORMAL = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.667f, 0f, 1f);
        private static readonly Color COLOR_ALARM = new Color(1f, 0.267f, 0.267f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private float _value = 0f;
        private LinearGaugeOrientation _orientation = LinearGaugeOrientation.Horizontal;
        private float _warningThreshold = 70f;
        private float _alarmThreshold = 90f;
        private bool _showThresholdMarkers = true;
        
        [UxmlAttribute]
        public float minValue { get => _minValue; set { _minValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float maxValue { get => _maxValue; set { _maxValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float value { get => _value; set { _value = Mathf.Clamp(value, _minValue, _maxValue); MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public LinearGaugeOrientation orientation { get => _orientation; set { _orientation = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float warningThreshold { get => _warningThreshold; set { _warningThreshold = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float alarmThreshold { get => _alarmThreshold; set { _alarmThreshold = value; MarkDirtyRepaint(); } }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public LinearGaugePOC()
        {
            style.minWidth = 20;
            style.minHeight = 20;
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (width < 10f || height < 10f) return;
            
            var painter = mgc.painter2D;
            
            float range = _maxValue - _minValue;
            float normalizedValue = range > 0 ? (_value - _minValue) / range : 0f;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Determine fill color based on thresholds
            Color fillColor = COLOR_NORMAL;
            if (_value >= _alarmThreshold) fillColor = COLOR_ALARM;
            else if (_value >= _warningThreshold) fillColor = COLOR_WARNING;
            
            if (_orientation == LinearGaugeOrientation.Horizontal)
            {
                DrawHorizontalGauge(painter, width, height, normalizedValue, fillColor);
            }
            else
            {
                DrawVerticalGauge(painter, width, height, normalizedValue, fillColor);
            }
        }
        
        private void DrawHorizontalGauge(Painter2D painter, float width, float height, float normalizedValue, Color fillColor)
        {
            float barHeight = height - 4f;
            float barY = 2f;
            
            // Track background
            painter.fillColor = COLOR_TRACK;
            painter.BeginPath();
            DrawRoundedRect(painter, 2f, barY, width - 4f, barHeight, CORNER_RADIUS);
            painter.Fill();
            
            // Value fill
            float fillWidth = (width - 4f) * normalizedValue;
            if (fillWidth > 2f)
            {
                painter.fillColor = fillColor;
                painter.BeginPath();
                DrawRoundedRect(painter, 2f, barY, fillWidth, barHeight, CORNER_RADIUS);
                painter.Fill();
            }
            
            // Border
            painter.strokeColor = COLOR_BORDER;
            painter.lineWidth = BORDER_WIDTH;
            painter.BeginPath();
            DrawRoundedRect(painter, 2f, barY, width - 4f, barHeight, CORNER_RADIUS);
            painter.Stroke();
            
            // Threshold markers
            if (_showThresholdMarkers)
            {
                DrawThresholdMarker(painter, width, height, _warningThreshold, COLOR_WARNING, true);
                DrawThresholdMarker(painter, width, height, _alarmThreshold, COLOR_ALARM, true);
            }
        }
        
        private void DrawVerticalGauge(Painter2D painter, float width, float height, float normalizedValue, Color fillColor)
        {
            float barWidth = width - 4f;
            float barX = 2f;
            
            // Track background
            painter.fillColor = COLOR_TRACK;
            painter.BeginPath();
            DrawRoundedRect(painter, barX, 2f, barWidth, height - 4f, CORNER_RADIUS);
            painter.Fill();
            
            // Value fill (from bottom)
            float fillHeight = (height - 4f) * normalizedValue;
            if (fillHeight > 2f)
            {
                painter.fillColor = fillColor;
                painter.BeginPath();
                DrawRoundedRect(painter, barX, height - 2f - fillHeight, barWidth, fillHeight, CORNER_RADIUS);
                painter.Fill();
            }
            
            // Border
            painter.strokeColor = COLOR_BORDER;
            painter.lineWidth = BORDER_WIDTH;
            painter.BeginPath();
            DrawRoundedRect(painter, barX, 2f, barWidth, height - 4f, CORNER_RADIUS);
            painter.Stroke();
            
            // Threshold markers
            if (_showThresholdMarkers)
            {
                DrawThresholdMarker(painter, width, height, _warningThreshold, COLOR_WARNING, false);
                DrawThresholdMarker(painter, width, height, _alarmThreshold, COLOR_ALARM, false);
            }
        }
        
        private void DrawThresholdMarker(Painter2D painter, float width, float height, float threshold, Color color, bool horizontal)
        {
            float range = _maxValue - _minValue;
            if (range <= 0) return;
            
            float normalizedThreshold = (threshold - _minValue) / range;
            if (normalizedThreshold < 0 || normalizedThreshold > 1) return;
            
            painter.strokeColor = color;
            painter.lineWidth = 2f;
            painter.BeginPath();
            
            if (horizontal)
            {
                float x = 2f + (width - 4f) * normalizedThreshold;
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
            }
            else
            {
                float y = height - 2f - (height - 4f) * normalizedThreshold;
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
            }
            painter.Stroke();
        }
        
        private void DrawRoundedRect(Painter2D painter, float x, float y, float w, float h, float r)
        {
            painter.MoveTo(new Vector2(x + r, y));
            painter.LineTo(new Vector2(x + w - r, y));
            painter.Arc(new Vector2(x + w - r, y + r), r, 270f, 360f);
            painter.LineTo(new Vector2(x + w, y + h - r));
            painter.Arc(new Vector2(x + w - r, y + h - r), r, 0f, 90f);
            painter.LineTo(new Vector2(x + r, y + h));
            painter.Arc(new Vector2(x + r, y + h - r), r, 90f, 180f);
            painter.LineTo(new Vector2(x, y + r));
            painter.Arc(new Vector2(x + r, y + r), r, 180f, 270f);
            painter.ClosePath();
        }
    }
}
