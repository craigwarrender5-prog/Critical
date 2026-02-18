// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// PressurizerVesselPOC.cs — Two-zone vessel with water/steam visualization
// ============================================================================
//
// PURPOSE:
//   Visualize pressurizer with:
//   - Water region (bottom) with level indication
//   - Steam region (top) 
//   - Heater elements (visual indication)
//   - Spray indication (top)
//   - Surge line connection (bottom)
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [UxmlElement]
    public partial class PressurizerVesselPOC : VisualElement
    {
        // ====================================================================
        // COLORS
        // ====================================================================
        
        // Vessel
        private static readonly Color COLOR_VESSEL_BG = new Color(0.06f, 0.06f, 0.1f, 1f);
        private static readonly Color COLOR_VESSEL_BORDER = new Color(0.4f, 0.4f, 0.5f, 1f);
        
        // Water - blue gradient
        private static readonly Color COLOR_WATER_TOP = new Color(0.2f, 0.5f, 0.9f, 0.95f);
        private static readonly Color COLOR_WATER_BOTTOM = new Color(0.1f, 0.25f, 0.5f, 0.95f);
        
        // Steam - gray/white gradient
        private static readonly Color COLOR_STEAM_TOP = new Color(0.6f, 0.6f, 0.65f, 0.7f);
        private static readonly Color COLOR_STEAM_BOTTOM = new Color(0.4f, 0.4f, 0.45f, 0.5f);
        
        // Bubble region (transition zone)
        private static readonly Color COLOR_BUBBLE_ZONE = new Color(0.3f, 0.45f, 0.7f, 0.8f);
        
        // Heaters
        private static readonly Color COLOR_HEATER_OFF = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color COLOR_HEATER_ON = new Color(1f, 0.4f, 0.1f, 1f);
        private static readonly Color COLOR_HEATER_GLOW = new Color(1f, 0.6f, 0.2f, 0.5f);
        
        // Spray
        private static readonly Color COLOR_SPRAY = new Color(0.4f, 0.8f, 1f, 0.9f);
        
        // Flow lines
        private static readonly Color COLOR_CHARGING = new Color(0.3f, 0.8f, 1f, 1f);  // Cyan - cold water in
        private static readonly Color COLOR_LETDOWN = new Color(1f, 0.6f, 0.2f, 1f);   // Orange - hot water out
        
        // Level indicator
        private static readonly Color COLOR_LEVEL_LINE = new Color(1f, 1f, 1f, 0.8f);
        private static readonly Color COLOR_LEVEL_SETPOINT = new Color(0f, 1f, 0.533f, 0.8f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private float _level = 50f;           // 0-100%
        private float _levelSetpoint = 60f;   // Target level
        private float _pressure = 2235f;      // psia
        private float _heaterPower = 0f;      // 0-100%
        private bool _sprayActive = false;
        private bool _showBubbleZone = false; // During bubble formation
        private float _bubbleZoneHeight = 5f; // % of vessel height
        private float _chargingFlow = 0f;     // gpm (0 = off)
        private float _letdownFlow = 0f;      // gpm (0 = off)
        private float _flowAnimPhase = 0f;    // Animation phase for flow dots
        
        [UxmlAttribute]
        public float level 
        { 
            get => _level; 
            set { _level = Mathf.Clamp(value, 0f, 100f); MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public float levelSetpoint 
        { 
            get => _levelSetpoint; 
            set { _levelSetpoint = Mathf.Clamp(value, 0f, 100f); MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public float pressure 
        { 
            get => _pressure; 
            set { _pressure = value; MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public float heaterPower 
        { 
            get => _heaterPower; 
            set { _heaterPower = Mathf.Clamp(value, 0f, 100f); MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public bool sprayActive 
        { 
            get => _sprayActive; 
            set { _sprayActive = value; MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public bool showBubbleZone 
        { 
            get => _showBubbleZone; 
            set { _showBubbleZone = value; MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public float chargingFlow 
        { 
            get => _chargingFlow; 
            set { _chargingFlow = Mathf.Max(0f, value); MarkDirtyRepaint(); } 
        }
        
        [UxmlAttribute]
        public float letdownFlow 
        { 
            get => _letdownFlow; 
            set { _letdownFlow = Mathf.Max(0f, value); MarkDirtyRepaint(); } 
        }
        
        public void UpdateFlowAnimation(float deltaTime)
        {
            _flowAnimPhase += deltaTime * 2f;  // Speed of flow animation
            if (_flowAnimPhase > 1f) _flowAnimPhase -= 1f;
            MarkDirtyRepaint();
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public PressurizerVesselPOC()
        {
            style.minWidth = 80;
            style.minHeight = 150;
            
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (width < 40f || height < 80f) return;
            
            var painter = mgc.painter2D;
            
            // Vessel dimensions
            float vesselMargin = 10f;
            float vesselX = vesselMargin + 20f;  // Extra left margin for scale
            float vesselY = vesselMargin + 15f;  // Extra top margin for spray nozzle
            float vesselWidth = width - vesselMargin * 2f - 40f;  // Extra right margin for labels
            float vesselHeight = height - vesselMargin * 2f - 30f;
            
            float domeHeight = vesselWidth * 0.4f;  // Hemispherical dome
            float bodyHeight = vesselHeight - domeHeight * 2f;
            
            // Calculate level position
            float levelY = vesselY + vesselHeight - (vesselHeight * _level / 100f);
            float setpointY = vesselY + vesselHeight - (vesselHeight * _levelSetpoint / 100f);
            
            // ================================================================
            // Draw vessel background
            // ================================================================
            painter.fillColor = COLOR_VESSEL_BG;
            painter.BeginPath();
            DrawVesselShape(painter, vesselX, vesselY, vesselWidth, vesselHeight, domeHeight);
            painter.Fill();
            
            // ================================================================
            // Draw steam region (top)
            // ================================================================
            if (_level < 100f)
            {
                // Clip to vessel shape using steam color
                painter.fillColor = COLOR_STEAM_BOTTOM;
                painter.BeginPath();
                
                // Top dome
                painter.Arc(new Vector2(vesselX + vesselWidth / 2f, vesselY + domeHeight), 
                           vesselWidth / 2f, 180f, 360f);
                
                // Body down to water level
                float steamBottom = Mathf.Max(levelY, vesselY + domeHeight);
                painter.LineTo(new Vector2(vesselX + vesselWidth, steamBottom));
                painter.LineTo(new Vector2(vesselX, steamBottom));
                painter.ClosePath();
                painter.Fill();
                
                // Steam wisps (decorative lines)
                if (_level < 95f)
                {
                    painter.strokeColor = new Color(0.7f, 0.7f, 0.75f, 0.3f);
                    painter.lineWidth = 1f;
                    
                    float steamTop = vesselY + domeHeight * 0.5f;
                    float steamRegionHeight = steamBottom - steamTop;
                    
                    for (int i = 0; i < 3; i++)
                    {
                        float wispY = steamTop + steamRegionHeight * (0.2f + i * 0.3f);
                        float wispX = vesselX + vesselWidth * (0.3f + i * 0.2f);
                        
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(wispX, wispY));
                        painter.BezierCurveTo(
                            new Vector2(wispX + 10f, wispY - 5f),
                            new Vector2(wispX + 15f, wispY + 5f),
                            new Vector2(wispX + 20f, wispY)
                        );
                        painter.Stroke();
                    }
                }
            }
            
            // ================================================================
            // Draw bubble zone (during bubble formation)
            // ================================================================
            if (_showBubbleZone && _level > 0f && _level < 100f)
            {
                float bubbleHeight = vesselHeight * _bubbleZoneHeight / 100f;
                float bubbleTop = levelY - bubbleHeight / 2f;
                float bubbleBottom = levelY + bubbleHeight / 2f;
                
                painter.fillColor = COLOR_BUBBLE_ZONE;
                painter.BeginPath();
                painter.MoveTo(new Vector2(vesselX + 2f, bubbleTop));
                painter.LineTo(new Vector2(vesselX + vesselWidth - 2f, bubbleTop));
                painter.LineTo(new Vector2(vesselX + vesselWidth - 2f, bubbleBottom));
                painter.LineTo(new Vector2(vesselX + 2f, bubbleBottom));
                painter.ClosePath();
                painter.Fill();
                
                // Bubble circles
                painter.fillColor = new Color(0.5f, 0.6f, 0.8f, 0.6f);
                float[] bubbleXOffsets = { 0.25f, 0.5f, 0.75f, 0.4f, 0.6f };
                float[] bubbleYOffsets = { 0.3f, 0.5f, 0.7f, 0.2f, 0.8f };
                float[] bubbleSizes = { 3f, 4f, 3f, 2f, 2.5f };
                
                for (int i = 0; i < bubbleXOffsets.Length; i++)
                {
                    float bx = vesselX + vesselWidth * bubbleXOffsets[i];
                    float by = bubbleTop + bubbleHeight * bubbleYOffsets[i];
                    painter.BeginPath();
                    painter.Arc(new Vector2(bx, by), bubbleSizes[i], 0f, 360f);
                    painter.Fill();
                }
            }
            
            // ================================================================
            // Draw water region (bottom)
            // ================================================================
            if (_level > 0f)
            {
                painter.fillColor = COLOR_WATER_TOP;
                painter.BeginPath();
                
                // Start at water surface
                float waterTop = levelY;
                float clampedWaterTop = Mathf.Max(waterTop, vesselY + domeHeight);
                
                // If water is in the dome region
                if (waterTop < vesselY + domeHeight)
                {
                    // Calculate arc intersection
                    float domeCenter = vesselY + domeHeight;
                    float dy = domeCenter - waterTop;
                    float dx = Mathf.Sqrt((vesselWidth / 2f) * (vesselWidth / 2f) - dy * dy);
                    
                    painter.MoveTo(new Vector2(vesselX + vesselWidth / 2f - dx, waterTop));
                    painter.LineTo(new Vector2(vesselX + vesselWidth / 2f + dx, waterTop));
                }
                else
                {
                    painter.MoveTo(new Vector2(vesselX, clampedWaterTop));
                    painter.LineTo(new Vector2(vesselX + vesselWidth, clampedWaterTop));
                }
                
                // Right side down
                painter.LineTo(new Vector2(vesselX + vesselWidth, vesselY + vesselHeight - domeHeight));
                
                // Bottom dome
                painter.Arc(new Vector2(vesselX + vesselWidth / 2f, vesselY + vesselHeight - domeHeight),
                           vesselWidth / 2f, 0f, 180f);
                
                // Left side up
                painter.LineTo(new Vector2(vesselX, clampedWaterTop));
                painter.ClosePath();
                painter.Fill();
                
                // Water surface ripple
                if (_level < 98f)
                {
                    painter.strokeColor = new Color(0.4f, 0.7f, 1f, 0.6f);
                    painter.lineWidth = 1.5f;
                    painter.BeginPath();
                    
                    float rippleY = waterTop + 2f;
                    float rippleLeft = vesselX + 5f;
                    float rippleRight = vesselX + vesselWidth - 5f;
                    
                    // If in dome, adjust ripple width
                    if (waterTop < vesselY + domeHeight)
                    {
                        float domeCenter = vesselY + domeHeight;
                        float dy = domeCenter - waterTop;
                        float dx = Mathf.Sqrt((vesselWidth / 2f) * (vesselWidth / 2f) - dy * dy);
                        rippleLeft = vesselX + vesselWidth / 2f - dx + 3f;
                        rippleRight = vesselX + vesselWidth / 2f + dx - 3f;
                    }
                    
                    painter.MoveTo(new Vector2(rippleLeft, rippleY));
                    painter.LineTo(new Vector2(rippleRight, rippleY));
                    painter.Stroke();
                }
            }
            
            // ================================================================
            // Draw heaters (bottom of vessel)
            // ================================================================
            float heaterY = vesselY + vesselHeight - domeHeight * 0.7f;
            float heaterSpacing = vesselWidth / 4f;
            
            for (int i = 0; i < 3; i++)
            {
                float hx = vesselX + heaterSpacing * (i + 0.75f);
                
                // Heater glow when on
                if (_heaterPower > 10f)
                {
                    float glowIntensity = _heaterPower / 100f;
                    painter.fillColor = new Color(1f, 0.5f, 0.2f, 0.3f * glowIntensity);
                    painter.BeginPath();
                    painter.Arc(new Vector2(hx, heaterY), 8f + glowIntensity * 4f, 0f, 360f);
                    painter.Fill();
                }
                
                // Heater element
                Color heaterColor = Color.Lerp(COLOR_HEATER_OFF, COLOR_HEATER_ON, _heaterPower / 100f);
                painter.fillColor = heaterColor;
                painter.BeginPath();
                painter.Arc(new Vector2(hx, heaterY), 4f, 0f, 360f);
                painter.Fill();
                
                // Heater stem
                painter.strokeColor = heaterColor;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(hx, heaterY));
                painter.LineTo(new Vector2(hx, heaterY + 15f));
                painter.Stroke();
            }
            
            // ================================================================
            // Draw spray (top of vessel)
            // ================================================================
            float sprayX = vesselX + vesselWidth / 2f;
            float sprayY = vesselY + domeHeight * 0.3f;
            
            // Spray nozzle
            painter.strokeColor = COLOR_VESSEL_BORDER;
            painter.lineWidth = 3f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(sprayX, vesselY - 10f));
            painter.LineTo(new Vector2(sprayX, sprayY));
            painter.Stroke();
            
            // Spray droplets when active
            if (_sprayActive)
            {
                painter.fillColor = COLOR_SPRAY;
                
                float[] dropX = { -8f, 0f, 8f, -12f, 12f, -5f, 5f };
                float[] dropY = { 10f, 15f, 10f, 20f, 20f, 25f, 25f };
                float[] dropSize = { 2f, 2.5f, 2f, 1.5f, 1.5f, 1.5f, 1.5f };
                
                for (int i = 0; i < dropX.Length; i++)
                {
                    painter.BeginPath();
                    painter.Arc(new Vector2(sprayX + dropX[i], sprayY + dropY[i]), dropSize[i], 0f, 360f);
                    painter.Fill();
                }
            }
            
            // ================================================================
            // Draw vessel outline
            // ================================================================
            painter.strokeColor = COLOR_VESSEL_BORDER;
            painter.lineWidth = 2f;
            painter.BeginPath();
            DrawVesselShape(painter, vesselX, vesselY, vesselWidth, vesselHeight, domeHeight);
            painter.Stroke();
            
            // ================================================================
            // Draw level indicator line
            // ================================================================
            painter.strokeColor = COLOR_LEVEL_LINE;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(vesselX + vesselWidth + 5f, levelY));
            painter.LineTo(new Vector2(vesselX + vesselWidth + 15f, levelY));
            painter.Stroke();
            
            // Level setpoint marker
            painter.strokeColor = COLOR_LEVEL_SETPOINT;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(vesselX - 5f, setpointY));
            painter.LineTo(new Vector2(vesselX - 15f, setpointY));
            painter.Stroke();
            
            // Setpoint triangle
            painter.fillColor = COLOR_LEVEL_SETPOINT;
            painter.BeginPath();
            painter.MoveTo(new Vector2(vesselX - 5f, setpointY));
            painter.LineTo(new Vector2(vesselX - 10f, setpointY - 4f));
            painter.LineTo(new Vector2(vesselX - 10f, setpointY + 4f));
            painter.ClosePath();
            painter.Fill();
            
            // ================================================================
            // Draw scale markings
            // ================================================================
            painter.strokeColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
            painter.lineWidth = 1f;
            
            for (int pct = 0; pct <= 100; pct += 25)
            {
                float markY = vesselY + vesselHeight - (vesselHeight * pct / 100f);
                float markLength = (pct % 50 == 0) ? 8f : 4f;
                
                painter.BeginPath();
                painter.MoveTo(new Vector2(vesselX - 3f, markY));
                painter.LineTo(new Vector2(vesselX - 3f - markLength, markY));
                painter.Stroke();
            }
            
            // ================================================================
            // Draw surge line (bottom connection)
            // ================================================================
            float surgeX = vesselX + vesselWidth * 0.3f;
            float surgeY = vesselY + vesselHeight;
            
            painter.strokeColor = COLOR_VESSEL_BORDER;
            painter.lineWidth = 4f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(surgeX, surgeY - 5f));
            painter.LineTo(new Vector2(surgeX, surgeY + 10f));
            painter.LineTo(new Vector2(surgeX - 15f, surgeY + 10f));
            painter.Stroke();
            
            // ================================================================
            // Draw charging line (right side - water entering)
            // ================================================================
            float chargingX = vesselX + vesselWidth + 5f;
            float chargingY = vesselY + vesselHeight * 0.65f;  // Below water surface typically
            
            // Pipe
            painter.strokeColor = COLOR_VESSEL_BORDER;
            painter.lineWidth = 4f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(chargingX + 25f, chargingY));
            painter.LineTo(new Vector2(chargingX, chargingY));
            painter.Stroke();
            
            // Connection to vessel
            painter.lineWidth = 3f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(vesselX + vesselWidth - 1f, chargingY));
            painter.LineTo(new Vector2(chargingX, chargingY));
            painter.Stroke();
            
            // Charging flow animation
            if (_chargingFlow > 0f)
            {
                DrawFlowArrows(painter, 
                    new Vector2(chargingX + 25f, chargingY), 
                    new Vector2(vesselX + vesselWidth, chargingY),
                    COLOR_CHARGING, _flowAnimPhase, true);
            }
            
            // ================================================================
            // Draw letdown line (right side below charging - water leaving)
            // ================================================================
            float letdownX = vesselX + vesselWidth + 5f;
            float letdownY = vesselY + vesselHeight * 0.80f;  // Lower on vessel
            
            // Pipe
            painter.strokeColor = COLOR_VESSEL_BORDER;
            painter.lineWidth = 4f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(vesselX + vesselWidth - 1f, letdownY));
            painter.LineTo(new Vector2(letdownX + 25f, letdownY));
            painter.Stroke();
            
            // Letdown flow animation
            if (_letdownFlow > 0f)
            {
                DrawFlowArrows(painter,
                    new Vector2(vesselX + vesselWidth, letdownY),
                    new Vector2(letdownX + 25f, letdownY),
                    COLOR_LETDOWN, _flowAnimPhase, false);
            }
        }
        
        private void DrawFlowArrows(Painter2D painter, Vector2 start, Vector2 end, Color color, float phase, bool inward)
        {
            float length = Vector2.Distance(start, end);
            Vector2 dir = (end - start).normalized;
            
            // Draw animated chevrons/arrows along the pipe
            int numArrows = 3;
            float spacing = length / (numArrows + 1);
            float arrowSize = 4f;
            
            painter.strokeColor = color;
            painter.lineWidth = 2f;
            
            for (int i = 0; i < numArrows; i++)
            {
                // Animate position along pipe
                float t = (i + 1f) / (numArrows + 1f) + phase * (1f / (numArrows + 1f));
                if (t > 1f) t -= 1f;
                
                Vector2 pos = start + dir * (t * length);
                
                // Draw chevron pointing in flow direction
                Vector2 perp = new Vector2(-dir.y, dir.x);
                Vector2 tip = pos + dir * arrowSize * (inward ? 1f : 1f);
                Vector2 back1 = pos - dir * arrowSize * 0.5f + perp * arrowSize * 0.5f;
                Vector2 back2 = pos - dir * arrowSize * 0.5f - perp * arrowSize * 0.5f;
                
                painter.BeginPath();
                painter.MoveTo(back1);
                painter.LineTo(tip);
                painter.LineTo(back2);
                painter.Stroke();
            }
            
            // Draw flow rate indicator dot at entry/exit
            painter.fillColor = color;
            painter.BeginPath();
            Vector2 dotPos = inward ? start : end;
            painter.Arc(dotPos, 3f, 0f, 360f);
            painter.Fill();
        }
        
        private void DrawVesselShape(Painter2D painter, float x, float y, float w, float h, float domeH)
        {
            float centerX = x + w / 2f;
            float radius = w / 2f;
            
            // Top dome (semicircle)
            painter.Arc(new Vector2(centerX, y + domeH), radius, 180f, 360f);
            
            // Right side
            painter.LineTo(new Vector2(x + w, y + h - domeH));
            
            // Bottom dome (semicircle)
            painter.Arc(new Vector2(centerX, y + h - domeH), radius, 0f, 180f);
            
            // Left side
            painter.LineTo(new Vector2(x, y + domeH));
            
            painter.ClosePath();
        }
    }
}
