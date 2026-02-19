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
        
        // Water color transitions with temperature (cool -> hot)
        private static readonly Color COLOR_WATER_COOL_TOP = new Color(0.2f, 0.5f, 0.9f, 0.95f);
        private static readonly Color COLOR_WATER_COOL_BOTTOM = new Color(0.1f, 0.25f, 0.5f, 0.95f);
        private static readonly Color COLOR_WATER_HOT_TOP = new Color(0.95f, 0.46f, 0.16f, 0.95f);
        private static readonly Color COLOR_WATER_HOT_BOTTOM = new Color(0.58f, 0.2f, 0.09f, 0.95f);
        
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
        private float _levelSetpoint = 60f;   // Target level (requested)
        private float _levelSetpointVisual = 60f; // Rendered level setpoint (smoothed)
        private float _pressure = 2235f;      // psia
        private float _heaterPower = 0f;      // 0-100%
        private bool _sprayActive = false;
        private bool _showBubbleZone = false; // During bubble formation
        private float _bubbleZoneHeight = 5f; // % of vessel height
        private float _liquidTemperature = 120f; // degF
        private float _chargingFlow = 0f;     // gpm (0 = off)
        private float _letdownFlow = 0f;      // gpm (0 = off)
        private float _surgeFlow = 0f;        // gpm (+ into PZR, - out of PZR)
        private float _pressureTarget = 2249.7f; // psia (display context)
        private float _flowAnimPhase = 0f;    // Animation phase for flow dots
        private const float SETPOINT_SLEW_RATE_PCT_PER_SEC = 42f;
        private bool _setpointInitialized;

        // Overlay labels to clarify side indicators and flow paths.
        private readonly Label _leftSetpointLabel;
        private readonly Label _leftPressureLabel;
        private readonly Label _rightChargingLabel;
        private readonly Label _rightLetdownLabel;
        private readonly Label _surgeLabel;
        
        [UxmlAttribute]
        public float level 
        { 
            get => _level; 
            set { _level = Mathf.Clamp(value, 0f, 100f); MarkDirtyRepaint(); UpdateOverlayLabels(); } 
        }
        
        [UxmlAttribute]
        public float levelSetpoint 
        { 
            get => _levelSetpoint; 
            set
            {
                _levelSetpoint = Mathf.Clamp(value, 0f, 100f);
                if (!_setpointInitialized)
                {
                    _levelSetpointVisual = _levelSetpoint;
                    _setpointInitialized = true;
                }
                // Do not snap the marker; animation tick slews _levelSetpointVisual.
                UpdateOverlayLabels();
            }
        }
        
        [UxmlAttribute]
        public float pressure 
        { 
            get => _pressure; 
            set { _pressure = value; MarkDirtyRepaint(); UpdateOverlayLabels(); } 
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
            set { _sprayActive = value; MarkDirtyRepaint(); UpdateOverlayLabels(); } 
        }
        
        [UxmlAttribute]
        public bool showBubbleZone 
        { 
            get => _showBubbleZone; 
            set { _showBubbleZone = value; MarkDirtyRepaint(); } 
        }

        [UxmlAttribute]
        public float liquidTemperature
        {
            get => _liquidTemperature;
            set { _liquidTemperature = value; MarkDirtyRepaint(); UpdateOverlayLabels(); }
        }
        
        [UxmlAttribute]
        public float chargingFlow 
        { 
            get => _chargingFlow; 
            set { _chargingFlow = Mathf.Max(0f, value); MarkDirtyRepaint(); UpdateOverlayLabels(); } 
        }
        
        [UxmlAttribute]
        public float letdownFlow 
        { 
            get => _letdownFlow; 
            set { _letdownFlow = Mathf.Max(0f, value); MarkDirtyRepaint(); UpdateOverlayLabels(); } 
        }

        [UxmlAttribute]
        public float surgeFlow
        {
            get => _surgeFlow;
            set { _surgeFlow = value; MarkDirtyRepaint(); UpdateOverlayLabels(); }
        }

        [UxmlAttribute]
        public float pressureTarget
        {
            get => _pressureTarget;
            set { _pressureTarget = value; MarkDirtyRepaint(); UpdateOverlayLabels(); }
        }
        
        public void UpdateFlowAnimation(float deltaTime)
        {
            _levelSetpointVisual = Mathf.MoveTowards(
                _levelSetpointVisual,
                _levelSetpoint,
                SETPOINT_SLEW_RATE_PCT_PER_SEC * Mathf.Max(0f, deltaTime));

            _flowAnimPhase += deltaTime * 2f;  // Speed of flow animation
            if (_flowAnimPhase > 1f) _flowAnimPhase -= 1f;
            MarkDirtyRepaint();
            UpdateOverlayLabels();
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public PressurizerVesselPOC()
        {
            style.minWidth = 80;
            style.minHeight = 150;
            style.position = Position.Relative;

            _leftSetpointLabel = CreateOverlayLabel(9, TextAnchor.MiddleLeft, COLOR_LEVEL_SETPOINT);
            _leftPressureLabel = CreateOverlayLabel(9, TextAnchor.MiddleLeft, new Color(0.72f, 0.84f, 0.95f, 1f));
            _rightChargingLabel = CreateOverlayLabel(9, TextAnchor.MiddleLeft, COLOR_CHARGING);
            _rightLetdownLabel = CreateOverlayLabel(9, TextAnchor.MiddleLeft, COLOR_LETDOWN);
            _surgeLabel = CreateOverlayLabel(9, TextAnchor.MiddleCenter, new Color(0.72f, 0.84f, 0.95f, 1f));

            // Fixed pressure context tag in the upper-left corner of the panel.
            _leftPressureLabel.style.backgroundColor = new Color(0.03f, 0.08f, 0.15f, 0.72f);
            _leftPressureLabel.style.paddingLeft = 4f;
            _leftPressureLabel.style.paddingRight = 4f;
            _leftPressureLabel.style.paddingTop = 1f;
            _leftPressureLabel.style.paddingBottom = 1f;
            _leftPressureLabel.style.borderTopLeftRadius = 3f;
            _leftPressureLabel.style.borderTopRightRadius = 3f;
            _leftPressureLabel.style.borderBottomLeftRadius = 3f;
            _leftPressureLabel.style.borderBottomRightRadius = 3f;

            Add(_leftSetpointLabel);
            Add(_leftPressureLabel);
            Add(_rightChargingLabel);
            Add(_rightLetdownLabel);
            Add(_surgeLabel);

            RegisterCallback<GeometryChangedEvent>(_ => UpdateOverlayLabels());
            generateVisualContent += OnGenerateVisualContent;
        }

        private static Label CreateOverlayLabel(float size, TextAnchor align, Color color)
        {
            var lbl = new Label("--");
            lbl.pickingMode = PickingMode.Ignore;
            lbl.style.position = Position.Absolute;
            lbl.style.unityTextAlign = align;
            lbl.style.fontSize = size;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color = color;
            lbl.style.whiteSpace = WhiteSpace.NoWrap;
            return lbl;
        }

        private void UpdateOverlayLabels()
        {
            if (!TryComputeVesselLayout(
                contentRect.width, contentRect.height,
                out float vesselX, out float vesselY, out float vesselWidth, out float vesselHeight,
                out _, out _))
                return;

            const float markerInsetPx = 8f;
            float setpointYRaw = vesselY + vesselHeight - (vesselHeight * _levelSetpointVisual / 100f);
            float setpointY = Mathf.Clamp(setpointYRaw, vesselY + markerInsetPx, vesselY + vesselHeight - markerInsetPx);
            float chargingY = vesselY + vesselHeight * 0.65f;
            float letdownY = vesselY + vesselHeight * 0.80f;
            float surgeY = vesselY + vesselHeight + 14f;
            float labelBottomMax = contentRect.height - 16f;
            float setpointLabelX = Mathf.Clamp(vesselX - 58f, 14f, contentRect.width - 122f);

            _leftSetpointLabel.text = $"LVL SP {_levelSetpoint:F1}%";
            _leftSetpointLabel.style.left = setpointLabelX;
            _leftSetpointLabel.style.top = Mathf.Clamp(setpointY + 1f, 12f, labelBottomMax);
            _leftSetpointLabel.style.width = 118f;

            _leftPressureLabel.text = $"P/TGT {_pressure:F0}/{_pressureTarget:F0} psia";
            _leftPressureLabel.style.left = 8f;
            _leftPressureLabel.style.top = 8f;
            _leftPressureLabel.style.width = 140f;

            _rightChargingLabel.text = $"CHG {_chargingFlow:F1} -> IN";
            _rightChargingLabel.style.left = vesselX + vesselWidth + 24f;
            _rightChargingLabel.style.top = Mathf.Clamp(chargingY - 8f, 2f, contentRect.height - 16f);
            _rightChargingLabel.style.width = 136f;

            _rightLetdownLabel.text = $"LTD {_letdownFlow:F1} <- OUT";
            _rightLetdownLabel.style.left = vesselX + vesselWidth + 24f;
            _rightLetdownLabel.style.top = Mathf.Clamp(letdownY - 8f, 2f, contentRect.height - 16f);
            _rightLetdownLabel.style.width = 136f;

            string surgeDir = _surgeFlow >= 0f ? "-> PZR" : "<- RCS";
            _surgeLabel.text = $"SURGE {_surgeFlow:+0.0;-0.0;0.0} gpm {surgeDir}";
            _surgeLabel.style.left = Mathf.Clamp(vesselX - 8f, 6f, contentRect.width - 170f);
            _surgeLabel.style.top = Mathf.Clamp(surgeY - 12f, 2f, contentRect.height - 16f);
            _surgeLabel.style.width = 160f;

            // Keep the actual level marker visible above overlays.
            _leftSetpointLabel.BringToFront();
            _leftPressureLabel.BringToFront();
            _rightChargingLabel.BringToFront();
            _rightLetdownLabel.BringToFront();
            _surgeLabel.BringToFront();
        }

        private static bool TryComputeVesselLayout(
            float width, float height,
            out float vesselX, out float vesselY, out float vesselWidth, out float vesselHeight,
            out float domeHeight, out float bodyHeight)
        {
            vesselX = vesselY = vesselWidth = vesselHeight = domeHeight = bodyHeight = 0f;
            if (width < 40f || height < 80f) return false;

            float vesselMargin = 10f;
            vesselX = vesselMargin + 20f;
            vesselY = vesselMargin + 15f;
            float availableWidth = width - vesselMargin * 2f - 40f;
            float availableHeight = height - vesselMargin * 2f - 30f;
            if (availableWidth < 20f || availableHeight < 20f) return false;

            const float bodyToWidth = 0.8f;
            float widthByHeight = availableHeight / (1f + bodyToWidth);
            vesselWidth = Mathf.Clamp(Mathf.Min(availableWidth, widthByHeight), 70f, availableWidth);
            domeHeight = vesselWidth * 0.5f;
            bodyHeight = vesselWidth * bodyToWidth;
            vesselHeight = domeHeight * 2f + bodyHeight;

            float extraX = availableWidth - vesselWidth;
            float extraY = availableHeight - vesselHeight;
            vesselX += Mathf.Max(0f, extraX * 0.35f);
            vesselY += Mathf.Max(0f, extraY * 0.5f);
            return true;
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

            if (!TryComputeVesselLayout(width, height,
                out float vesselX, out float vesselY, out float vesselWidth, out float vesselHeight,
                out float domeHeight, out _))
                return;
            
            // Calculate level position
            float levelY = vesselY + vesselHeight - (vesselHeight * _level / 100f);
            const float markerInsetPx = 8f;
            float setpointYRaw = vesselY + vesselHeight - (vesselHeight * _levelSetpointVisual / 100f);
            float setpointY = Mathf.Clamp(setpointYRaw, vesselY + markerInsetPx, vesselY + vesselHeight - markerInsetPx);
            
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
                float waterTempNorm = Mathf.InverseLerp(70f, 650f, _liquidTemperature);
                Color waterTopColor = Color.Lerp(COLOR_WATER_COOL_TOP, COLOR_WATER_HOT_TOP, waterTempNorm);
                Color waterBottomColor = Color.Lerp(COLOR_WATER_COOL_BOTTOM, COLOR_WATER_HOT_BOTTOM, waterTempNorm);
                painter.fillColor = Color.Lerp(waterBottomColor, waterTopColor, 0.55f);
                if (_level >= 99.9f)
                {
                    painter.BeginPath();
                    // Fully solid pressurizer: fill the entire vessel geometry
                    // to avoid top-cap artifacts from the partial-fill path.
                    DrawVesselShape(painter, vesselX, vesselY, vesselWidth, vesselHeight, domeHeight);
                    painter.Fill();
                }
                else
                {
                    painter.BeginPath();

                    // Start at water surface
                    float waterTop = levelY;
                    float domeBottomY = vesselY + domeHeight;
                    float bodyBottomY = vesselY + vesselHeight - domeHeight;
                    float radius = vesselWidth * 0.5f;
                    float centerX = vesselX + radius;
                    float topDomeCenterY = domeBottomY;
                    bool waterInTopDome = waterTop < domeBottomY;

                    if (waterInTopDome)
                    {
                        // Waterline intersects the top dome; follow curved shell so
                        // the fill never creates diagonal wedges.
                        float dy = topDomeCenterY - waterTop;
                        float dx2 = radius * radius - dy * dy;
                        float dx = Mathf.Sqrt(Mathf.Max(0f, dx2));
                        float leftWaterX = centerX - dx;
                        float rightWaterX = centerX + dx;

                        float leftAngle = Mathf.Atan2(waterTop - topDomeCenterY, leftWaterX - centerX) * Mathf.Rad2Deg;
                        float rightAngle = Mathf.Atan2(waterTop - topDomeCenterY, rightWaterX - centerX) * Mathf.Rad2Deg;
                        if (leftAngle < 0f) leftAngle += 360f;
                        if (rightAngle < 0f) rightAngle += 360f;

                        painter.MoveTo(new Vector2(leftWaterX, waterTop));
                        painter.LineTo(new Vector2(rightWaterX, waterTop));

                        // Right top dome shell down to body tangent.
                        painter.Arc(new Vector2(centerX, topDomeCenterY), radius, rightAngle, 360f);
                        painter.LineTo(new Vector2(vesselX + vesselWidth, bodyBottomY));

                        // Bottom dome shell.
                        painter.Arc(new Vector2(centerX, bodyBottomY), radius, 0f, 180f);
                        painter.LineTo(new Vector2(vesselX, domeBottomY));

                        // Left top dome shell back up to waterline.
                        painter.Arc(new Vector2(centerX, topDomeCenterY), radius, 180f, leftAngle);
                    }
                    else
                    {
                        painter.MoveTo(new Vector2(vesselX, waterTop));
                        painter.LineTo(new Vector2(vesselX + vesselWidth, waterTop));
                        painter.LineTo(new Vector2(vesselX + vesselWidth, bodyBottomY));
                        painter.Arc(new Vector2(centerX, bodyBottomY), radius, 0f, 180f);
                        painter.LineTo(new Vector2(vesselX, waterTop));
                    }

                    painter.ClosePath();
                    painter.Fill();
                    
                    // Water surface ripple
                    if (_level < 98f)
                    {
                        painter.strokeColor = Color.Lerp(new Color(0.4f, 0.7f, 1f, 0.6f), new Color(1f, 0.63f, 0.25f, 0.65f), waterTempNorm);
                        painter.lineWidth = 1.5f;
                        painter.BeginPath();
                        
                        float rippleY = waterTop + 2f;
                        float rippleLeft = vesselX + 5f;
                        float rippleRight = vesselX + vesselWidth - 5f;
                        
                        // If in dome, adjust ripple width
                        if (waterInTopDome)
                        {
                            float domeCenter = vesselY + domeHeight;
                            float dy = domeCenter - waterTop;
                            float dx2 = (vesselWidth / 2f) * (vesselWidth / 2f) - dy * dy;
                            float dx = Mathf.Sqrt(Mathf.Max(0f, dx2));
                            rippleLeft = vesselX + vesselWidth / 2f - dx + 3f;
                            rippleRight = vesselX + vesselWidth / 2f + dx - 3f;
                        }
                        
                        painter.MoveTo(new Vector2(rippleLeft, rippleY));
                        painter.LineTo(new Vector2(rippleRight, rippleY));
                        painter.Stroke();
                    }
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

            if (Mathf.Abs(_surgeFlow) > 0.05f)
            {
                bool intoVessel = _surgeFlow >= 0f;
                Color surgeColor = intoVessel ? COLOR_CHARGING : COLOR_LETDOWN;
                Vector2 surgeStart = intoVessel
                    ? new Vector2(surgeX - 15f, surgeY + 10f)
                    : new Vector2(surgeX, surgeY + 10f);
                Vector2 surgeEnd = intoVessel
                    ? new Vector2(surgeX, surgeY + 10f)
                    : new Vector2(surgeX - 15f, surgeY + 10f);

                DrawFlowArrows(painter, surgeStart, surgeEnd, surgeColor, _flowAnimPhase, intoVessel);
            }
            
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
