// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// TankLevelPOC.cs — Vertical tank/vessel level indicator
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [UxmlElement]
    public partial class TankLevelPOC : VisualElement
    {
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_TANK_BG = new Color(0.08f, 0.08f, 0.12f, 1f);
        private static readonly Color COLOR_TANK_BORDER = new Color(0.3f, 0.3f, 0.4f, 1f);
        private static readonly Color COLOR_WATER_TOP = new Color(0.2f, 0.6f, 1f, 0.9f);
        private static readonly Color COLOR_WATER_BOTTOM = new Color(0.1f, 0.3f, 0.6f, 0.9f);
        private static readonly Color COLOR_SCALE_LINE = new Color(0.4f, 0.4f, 0.5f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private float _value = 50f;
        private float _lowAlarm = 20f;
        private float _highAlarm = 80f;
        private bool _showScale = true;
        
        [UxmlAttribute]
        public float minValue { get => _minValue; set { _minValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float maxValue { get => _maxValue; set { _maxValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float value { get => _value; set { _value = Mathf.Clamp(value, _minValue, _maxValue); MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float lowAlarm { get => _lowAlarm; set { _lowAlarm = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float highAlarm { get => _highAlarm; set { _highAlarm = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public bool showScale { get => _showScale; set { _showScale = value; MarkDirtyRepaint(); } }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public TankLevelPOC()
        {
            style.minWidth = 40;
            style.minHeight = 80;
            
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (width < 20f || height < 40f) return;
            
            var painter = mgc.painter2D;
            
            float tankMargin = 4f;
            float tankX = tankMargin;
            float tankY = tankMargin;
            float tankWidth = width - tankMargin * 2f;
            float tankHeight = height - tankMargin * 2f;
            
            // Calculate level
            float range = _maxValue - _minValue;
            float normalizedValue = range > 0 ? (_value - _minValue) / range : 0f;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Draw tank background
            painter.fillColor = COLOR_TANK_BG;
            painter.BeginPath();
            DrawTankShape(painter, tankX, tankY, tankWidth, tankHeight);
            painter.Fill();
            
            // Draw water level
            float waterHeight = tankHeight * normalizedValue;
            if (waterHeight > 2f)
            {
                // Determine color based on alarm thresholds
                Color waterColor = COLOR_WATER_TOP;
                float normalizedLow = (_lowAlarm - _minValue) / range;
                float normalizedHigh = (_highAlarm - _minValue) / range;
                
                if (normalizedValue < normalizedLow || normalizedValue > normalizedHigh)
                {
                    waterColor = new Color(1f, 0.3f, 0.3f, 0.9f);  // Alarm color
                }
                
                painter.fillColor = waterColor;
                painter.BeginPath();
                
                // Water fills from bottom
                float waterTop = tankY + tankHeight - waterHeight;
                painter.MoveTo(new Vector2(tankX + 4f, tankY + tankHeight - 4f));
                painter.LineTo(new Vector2(tankX + 4f, waterTop));
                painter.LineTo(new Vector2(tankX + tankWidth - 4f, waterTop));
                painter.LineTo(new Vector2(tankX + tankWidth - 4f, tankY + tankHeight - 4f));
                painter.ClosePath();
                painter.Fill();
                
                // Water surface line
                painter.strokeColor = new Color(1f, 1f, 1f, 0.5f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(tankX + 6f, waterTop + 2f));
                painter.LineTo(new Vector2(tankX + tankWidth - 6f, waterTop + 2f));
                painter.Stroke();
            }
            
            // Draw scale lines
            if (_showScale)
            {
                painter.strokeColor = COLOR_SCALE_LINE;
                painter.lineWidth = 1f;
                
                for (int i = 1; i < 10; i++)
                {
                    float y = tankY + tankHeight - (tankHeight * i / 10f);
                    float lineLength = (i == 5) ? 8f : 4f;
                    
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(tankX + tankWidth - lineLength - 2f, y));
                    painter.LineTo(new Vector2(tankX + tankWidth - 2f, y));
                    painter.Stroke();
                }
            }
            
            // Draw alarm threshold markers
            painter.lineWidth = 2f;
            
            // Low alarm
            float lowY = tankY + tankHeight - (tankHeight * ((_lowAlarm - _minValue) / range));
            painter.strokeColor = new Color(1f, 0.667f, 0f, 1f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(tankX, lowY));
            painter.LineTo(new Vector2(tankX + 8f, lowY));
            painter.Stroke();
            
            // High alarm
            float highY = tankY + tankHeight - (tankHeight * ((_highAlarm - _minValue) / range));
            painter.strokeColor = new Color(1f, 0.667f, 0f, 1f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(tankX, highY));
            painter.LineTo(new Vector2(tankX + 8f, highY));
            painter.Stroke();
            
            // Draw tank border
            painter.strokeColor = COLOR_TANK_BORDER;
            painter.lineWidth = 2f;
            painter.BeginPath();
            DrawTankShape(painter, tankX, tankY, tankWidth, tankHeight);
            painter.Stroke();
        }
        
        private void DrawTankShape(Painter2D painter, float x, float y, float w, float h)
        {
            float r = 6f;  // Corner radius
            
            // Top-left corner
            painter.MoveTo(new Vector2(x + r, y));
            // Top edge
            painter.LineTo(new Vector2(x + w - r, y));
            // Top-right corner
            painter.Arc(new Vector2(x + w - r, y + r), r, 270f, 360f);
            // Right edge
            painter.LineTo(new Vector2(x + w, y + h - r));
            // Bottom-right corner
            painter.Arc(new Vector2(x + w - r, y + h - r), r, 0f, 90f);
            // Bottom edge
            painter.LineTo(new Vector2(x + r, y + h));
            // Bottom-left corner
            painter.Arc(new Vector2(x + r, y + h - r), r, 90f, 180f);
            // Left edge
            painter.LineTo(new Vector2(x, y + r));
            // Top-left corner
            painter.Arc(new Vector2(x + r, y + r), r, 180f, 270f);
            painter.ClosePath();
        }
    }
}
