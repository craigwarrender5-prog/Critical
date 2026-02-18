// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// BidirectionalGaugePOC.cs — Center-zero gauge for +/- values
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [UxmlElement]
    public partial class BidirectionalGaugePOC : VisualElement
    {
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_TRACK = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color COLOR_BORDER = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color COLOR_CENTER = new Color(0.4f, 0.4f, 0.5f, 1f);
        private static readonly Color COLOR_POSITIVE = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color COLOR_NEGATIVE = new Color(0.4f, 0.8f, 1f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private float _minValue = -100f;
        private float _maxValue = 100f;
        private float _value = 0f;
        
        [UxmlAttribute]
        public float minValue { get => _minValue; set { _minValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float maxValue { get => _maxValue; set { _maxValue = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public float value { get => _value; set { _value = Mathf.Clamp(value, _minValue, _maxValue); MarkDirtyRepaint(); } }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public BidirectionalGaugePOC()
        {
            style.minWidth = 100;
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
            
            if (width < 40f || height < 10f) return;
            
            var painter = mgc.painter2D;
            
            float barHeight = height - 8f;
            float barY = 4f;
            float barX = 4f;
            float barWidth = width - 8f;
            
            // Draw track background
            painter.fillColor = COLOR_TRACK;
            painter.BeginPath();
            DrawRoundedRect(painter, barX, barY, barWidth, barHeight, 3f);
            painter.Fill();
            
            // Calculate center and value position
            float range = _maxValue - _minValue;
            float centerX = barX + barWidth / 2f;
            
            // Calculate value position relative to center
            float zeroPoint = -_minValue / range;  // Where zero falls in 0-1 range
            float valueNorm = (_value - _minValue) / range;
            
            float zeroPixel = barX + barWidth * zeroPoint;
            float valuePixel = barX + barWidth * valueNorm;
            
            // Draw value bar
            if (Mathf.Abs(_value) > 0.01f)
            {
                Color fillColor = _value >= 0 ? COLOR_POSITIVE : COLOR_NEGATIVE;
                painter.fillColor = fillColor;
                painter.BeginPath();
                
                float fillX = Mathf.Min(zeroPixel, valuePixel);
                float fillWidth = Mathf.Abs(valuePixel - zeroPixel);
                
                DrawRoundedRect(painter, fillX, barY + 2f, fillWidth, barHeight - 4f, 2f);
                painter.Fill();
            }
            
            // Draw center line (zero point)
            painter.strokeColor = COLOR_CENTER;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(zeroPixel, barY - 2f));
            painter.LineTo(new Vector2(zeroPixel, barY + barHeight + 2f));
            painter.Stroke();
            
            // Draw border
            painter.strokeColor = COLOR_BORDER;
            painter.lineWidth = 1f;
            painter.BeginPath();
            DrawRoundedRect(painter, barX, barY, barWidth, barHeight, 3f);
            painter.Stroke();
            
            // Draw end markers
            painter.strokeColor = COLOR_BORDER;
            painter.lineWidth = 1f;
            
            // Left marker (min)
            painter.BeginPath();
            painter.MoveTo(new Vector2(barX + 2f, barY - 3f));
            painter.LineTo(new Vector2(barX + 2f, barY + barHeight + 3f));
            painter.Stroke();
            
            // Right marker (max)
            painter.BeginPath();
            painter.MoveTo(new Vector2(barX + barWidth - 2f, barY - 3f));
            painter.LineTo(new Vector2(barX + barWidth - 2f, barY + barHeight + 3f));
            painter.Stroke();
        }
        
        private void DrawRoundedRect(Painter2D painter, float x, float y, float w, float h, float r)
        {
            if (w < r * 2) r = w / 2f;
            if (h < r * 2) r = h / 2f;
            
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
