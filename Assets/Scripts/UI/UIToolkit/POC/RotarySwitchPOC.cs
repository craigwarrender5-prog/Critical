// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// RotarySwitchPOC.cs — 3-Position Industrial Rotary Pole Switch
// ============================================================================
//
// PURPOSE:
//   Implements a realistic 3-position industrial rotary selector switch
//   commonly found on nuclear plant control panels.
//
// INTERACTION:
//   - Left-click: Rotate clockwise to next position
//   - Right-click: Rotate counter-clockwise to previous position
//
// POSITIONS:
//   Position 0 (Left):   Typically OFF / MANUAL / A
//   Position 1 (Center): Typically AUTO / NEUTRAL
//   Position 2 (Right):  Typically ON / REMOTE / B
//
// VISUAL DESIGN:
//   Modeled after Allen-Bradley 800T series selector switches
//   - Heavy-duty zinc die-cast bezel with machined finish
//   - Black phenolic/Bakelite knob with knurled grip texture
//   - Engraved position indicator line
//   - Detent notches machined into faceplate
//   - Industrial nameplate below switch
//
// VERSION: 2.0 — Enhanced industrial realism
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    /// <summary>
    /// Represents the three discrete positions of the rotary switch.
    /// </summary>
    public enum RotarySwitchPosition
    {
        Left = 0,    // -45 degrees
        Center = 1,  // 0 degrees  
        Right = 2    // +45 degrees
    }
    
    /// <summary>
    /// 3-Position Industrial Rotary Pole Switch with click-to-rotate interaction.
    /// </summary>
    [UxmlElement]
    public partial class RotarySwitchPOC : VisualElement
    {
        // ====================================================================
        // VISUAL CONSTANTS — Industrial Switch Styling (Allen-Bradley 800T)
        // ====================================================================
        
        // Mounting plate (dark industrial gray panel)
        private static readonly Color PANEL_COLOR = new Color(0.18f, 0.19f, 0.20f, 1f);
        private static readonly Color PANEL_SHADOW = new Color(0.08f, 0.08f, 0.09f, 1f);
        
        // Bezel (heavy zinc die-cast, machined finish)
        private static readonly Color BEZEL_CHROME = new Color(0.72f, 0.74f, 0.76f, 1f);
        private static readonly Color BEZEL_HIGHLIGHT = new Color(0.88f, 0.90f, 0.92f, 1f);
        private static readonly Color BEZEL_SHADOW = new Color(0.35f, 0.36f, 0.38f, 1f);
        private static readonly Color BEZEL_DARK = new Color(0.25f, 0.26f, 0.28f, 1f);
        private static readonly Color BEZEL_EDGE = new Color(0.50f, 0.52f, 0.54f, 1f);
        
        // Faceplate (black phenolic insert with machined detents)
        private static readonly Color FACEPLATE = new Color(0.06f, 0.06f, 0.07f, 1f);
        private static readonly Color FACEPLATE_RING = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color DETENT_COLOR = new Color(0.03f, 0.03f, 0.04f, 1f);
        
        // Knob (black Bakelite with knurled texture)
        private static readonly Color KNOB_BASE = new Color(0.08f, 0.08f, 0.09f, 1f);
        private static readonly Color KNOB_SURFACE = new Color(0.14f, 0.14f, 0.15f, 1f);
        private static readonly Color KNOB_HIGHLIGHT = new Color(0.22f, 0.22f, 0.24f, 1f);
        private static readonly Color KNOB_RIDGE = new Color(0.18f, 0.18f, 0.20f, 1f);
        private static readonly Color KNOB_GROOVE = new Color(0.06f, 0.06f, 0.07f, 1f);
        
        // Pointer/indicator (engraved white line)
        private static readonly Color POINTER_COLOR = new Color(0.92f, 0.92f, 0.90f, 1f);
        private static readonly Color POINTER_SHADOW = new Color(0.04f, 0.04f, 0.05f, 1f);
        
        // Position markers and labels
        private static readonly Color MARKER_INACTIVE = new Color(0.45f, 0.46f, 0.48f, 1f);
        private static readonly Color MARKER_ACTIVE = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color LABEL_ENGRAVED = new Color(0.85f, 0.85f, 0.83f, 1f);
        
        // Nameplate (industrial legend plate)
        private static readonly Color NAMEPLATE_BG = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color NAMEPLATE_BORDER = new Color(0.30f, 0.30f, 0.32f, 1f);
        private static readonly Color NAMEPLATE_TEXT = new Color(0.90f, 0.90f, 0.88f, 1f);
        
        // Geometry ratios (relative to half-size)
        private const float BEZEL_OUTER_RATIO = 0.98f;
        private const float BEZEL_INNER_RATIO = 0.88f;
        private const float FACEPLATE_RATIO = 0.82f;
        private const float KNOB_OUTER_RATIO = 0.52f;
        private const float KNOB_INNER_RATIO = 0.42f;
        private const float POINTER_START_RATIO = 0.15f;
        private const float POINTER_END_RATIO = 0.48f;
        private const float DETENT_RADIUS_RATIO = 0.72f;
        private const float MARKER_RADIUS_RATIO = 0.92f;
        
        // Knurling parameters
        private const int KNURL_COUNT = 24;  // Number of ridges around knob
        
        // Animation
        private const float DETENT_ANGLE = 45f;
        private const float ANIMATION_DURATION = 0.12f;
        
        // ====================================================================
        // BACKING FIELDS
        // ====================================================================
        
        private RotarySwitchPosition _position = RotarySwitchPosition.Center;
        private string _labelLeft = "OFF";
        private string _labelCenter = "AUTO";
        private string _labelRight = "ON";
        private string _switchLabel = "";
        private bool _enabled = true;
        
        // Animation state
        private float _displayAngle = 0f;
        private float _targetAngle = 0f;
        private IVisualElementScheduledItem _animationSchedule;
        private float _animationStartAngle;
        private float _animationProgress;
        
        // ====================================================================
        // UXML ATTRIBUTES
        // ====================================================================
        
        [UxmlAttribute]
        public RotarySwitchPosition position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    AnimateToPosition(_position);
                    PositionChanged?.Invoke(_position);
                }
            }
        }
        
        [UxmlAttribute]
        public string labelLeft
        {
            get => _labelLeft;
            set { _labelLeft = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public string labelCenter
        {
            get => _labelCenter;
            set { _labelCenter = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public string labelRight
        {
            get => _labelRight;
            set { _labelRight = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public string switchLabel
        {
            get => _switchLabel;
            set { _switchLabel = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute("enabled")]
        public bool switchEnabled
        {
            get => _enabled;
            set { _enabled = value; MarkDirtyRepaint(); }
        }
        
        // ====================================================================
        // EVENTS
        // ====================================================================
        
        public event Action<RotarySwitchPosition> PositionChanged;
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public RotarySwitchPOC()
        {
            style.width = 120;
            style.height = 140;
            style.minWidth = 80;
            style.minHeight = 100;
            
            _displayAngle = 0f;
            _targetAngle = 0f;
            
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                _animationSchedule?.Pause();
                _animationSchedule = null;
            });
        }
        
        // ====================================================================
        // INPUT HANDLING
        // ====================================================================
        
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_enabled) return;
            
            if (evt.button == 0)
            {
                RotateClockwise();
                evt.StopPropagation();
            }
            else if (evt.button == 1)
            {
                RotateCounterClockwise();
                evt.StopPropagation();
            }
        }
        
        public void RotateClockwise()
        {
            if (_position < RotarySwitchPosition.Right)
                position = (RotarySwitchPosition)((int)_position + 1);
        }
        
        public void RotateCounterClockwise()
        {
            if (_position > RotarySwitchPosition.Left)
                position = (RotarySwitchPosition)((int)_position - 1);
        }
        
        // ====================================================================
        // ANIMATION
        // ====================================================================
        
        private void AnimateToPosition(RotarySwitchPosition targetPosition)
        {
            _targetAngle = ((int)targetPosition - 1) * DETENT_ANGLE;
            _animationStartAngle = _displayAngle;
            _animationProgress = 0f;
            
            _animationSchedule?.Pause();
            _animationSchedule = schedule.Execute(UpdateAnimation).Every(16);
        }
        
        private void UpdateAnimation()
        {
            _animationProgress += 16f / 1000f / ANIMATION_DURATION;
            
            if (_animationProgress >= 1f)
            {
                _displayAngle = _targetAngle;
                _animationSchedule?.Pause();
                _animationSchedule = null;
            }
            else
            {
                float t = 1f - Mathf.Pow(1f - _animationProgress, 3f);
                _displayAngle = Mathf.Lerp(_animationStartAngle, _targetAngle, t);
            }
            
            MarkDirtyRepaint();
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Rect rect = contentRect;
            if (rect.width < 10f || rect.height < 10f) return;
            
            var painter = mgc.painter2D;
            
            float switchDiameter = Mathf.Min(rect.width, rect.height - 30f);
            float halfSize = switchDiameter / 2f;
            Vector2 center = new Vector2(rect.width / 2f, halfSize + 5f);
            
            // Draw components back-to-front
            DrawMountingShadow(painter, center, halfSize);
            DrawBezel(painter, center, halfSize);
            DrawFaceplate(painter, center, halfSize);
            DrawDetentNotches(painter, center, halfSize);
            DrawKnobShadow(painter, center, halfSize, _displayAngle);
            DrawKnob(painter, center, halfSize, _displayAngle);
            DrawKnurling(painter, center, halfSize, _displayAngle);
            DrawPointer(painter, center, halfSize, _displayAngle);
            DrawPositionMarkers(painter, center, halfSize);
        }
        
        private void DrawMountingShadow(Painter2D painter, Vector2 center, float halfSize)
        {
            // Deep shadow beneath the entire assembly
            float radius = halfSize * BEZEL_OUTER_RATIO;
            
            painter.fillColor = new Color(0f, 0f, 0f, 0.5f);
            painter.BeginPath();
            painter.Arc(center + new Vector2(2f, 4f), radius + 3f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            painter.fillColor = new Color(0f, 0f, 0f, 0.3f);
            painter.BeginPath();
            painter.Arc(center + new Vector2(1f, 2f), radius + 1f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
        
        private void DrawBezel(Painter2D painter, Vector2 center, float halfSize)
        {
            float outerRadius = halfSize * BEZEL_OUTER_RATIO;
            float innerRadius = halfSize * BEZEL_INNER_RATIO;
            
            // Outer bezel ring - machined metal look with gradient simulation
            // Bottom dark edge (shadow)
            painter.fillColor = BEZEL_DARK;
            painter.BeginPath();
            painter.Arc(center, outerRadius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Main chrome surface
            painter.fillColor = BEZEL_CHROME;
            painter.BeginPath();
            painter.Arc(center - new Vector2(0f, 1f), outerRadius - 1f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Top highlight arc (machined reflection)
            painter.strokeColor = BEZEL_HIGHLIGHT;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(center, outerRadius - 2f, 200f, 280f);
            painter.Stroke();
            
            // Secondary highlight
            painter.strokeColor = new Color(1f, 1f, 1f, 0.4f);
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.Arc(center, outerRadius - 4f, 210f, 270f);
            painter.Stroke();
            
            // Inner bevel (chamfer into faceplate)
            painter.strokeColor = BEZEL_SHADOW;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.Arc(center, innerRadius + 2f, 0f, 360f);
            painter.Stroke();
            
            // Machined ring detail
            painter.strokeColor = BEZEL_EDGE;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.Arc(center, outerRadius - 1f, 0f, 360f);
            painter.Stroke();
        }
        
        private void DrawFaceplate(Painter2D painter, Vector2 center, float halfSize)
        {
            float radius = halfSize * FACEPLATE_RATIO;
            
            // Main black faceplate
            painter.fillColor = FACEPLATE;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Subtle inner ring (machined detail)
            painter.strokeColor = FACEPLATE_RING;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.Arc(center, radius - 3f, 0f, 360f);
            painter.Stroke();
            
            // Inner edge shadow
            painter.strokeColor = new Color(0f, 0f, 0f, 0.5f);
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.Arc(center, radius, 20f, 160f);
            painter.Stroke();
        }
        
        private void DrawDetentNotches(Painter2D painter, Vector2 center, float halfSize)
        {
            float detentRadius = halfSize * DETENT_RADIUS_RATIO;
            float notchLength = 8f;
            float notchWidth = 3f;
            
            for (int i = 0; i < 3; i++)
            {
                float angle = (i - 1) * DETENT_ANGLE - 90f;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector2 innerPos = center + new Vector2(
                    Mathf.Cos(rad) * (detentRadius - notchLength),
                    Mathf.Sin(rad) * (detentRadius - notchLength)
                );
                Vector2 outerPos = center + new Vector2(
                    Mathf.Cos(rad) * detentRadius,
                    Mathf.Sin(rad) * detentRadius
                );
                
                // Notch groove (dark inset)
                painter.strokeColor = DETENT_COLOR;
                painter.lineWidth = notchWidth;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(innerPos);
                painter.LineTo(outerPos);
                painter.Stroke();
                
                // Highlight edge on notch
                bool isActive = (int)_position == i;
                painter.strokeColor = isActive ? MARKER_ACTIVE : new Color(0.25f, 0.25f, 0.28f, 1f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(innerPos);
                painter.LineTo(outerPos);
                painter.Stroke();
            }
        }
        
        private void DrawKnobShadow(Painter2D painter, Vector2 center, float halfSize, float angle)
        {
            float knobRadius = halfSize * KNOB_OUTER_RATIO;
            
            // Cast shadow (offset based on implied light source)
            painter.fillColor = new Color(0f, 0f, 0f, 0.6f);
            painter.BeginPath();
            painter.Arc(center + new Vector2(2f, 4f), knobRadius + 2f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            painter.fillColor = new Color(0f, 0f, 0f, 0.3f);
            painter.BeginPath();
            painter.Arc(center + new Vector2(1f, 2f), knobRadius + 1f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
        
        private void DrawKnob(Painter2D painter, Vector2 center, float halfSize, float angle)
        {
            float outerRadius = halfSize * KNOB_OUTER_RATIO;
            float innerRadius = halfSize * KNOB_INNER_RATIO;
            
            // Knob base (darker edge)
            painter.fillColor = KNOB_BASE;
            painter.BeginPath();
            painter.Arc(center, outerRadius + 1f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Knob outer ring (grip area - where knurling goes)
            painter.fillColor = KNOB_SURFACE;
            painter.BeginPath();
            painter.Arc(center, outerRadius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Knob inner surface (slightly raised center cap)
            painter.fillColor = KNOB_HIGHLIGHT;
            painter.BeginPath();
            painter.Arc(center - new Vector2(0f, 1f), innerRadius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Center highlight reflection
            painter.fillColor = new Color(0.30f, 0.30f, 0.32f, 1f);
            painter.BeginPath();
            painter.Arc(center - new Vector2(2f, 3f), innerRadius * 0.4f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Edge definition ring
            painter.strokeColor = KNOB_BASE;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.Arc(center, outerRadius, 0f, 360f);
            painter.Stroke();
            
            // Inner ring edge
            painter.strokeColor = new Color(0.10f, 0.10f, 0.11f, 1f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.Arc(center, innerRadius, 0f, 360f);
            painter.Stroke();
        }
        
        private void DrawKnurling(Painter2D painter, Vector2 center, float halfSize, float angle)
        {
            float outerRadius = halfSize * KNOB_OUTER_RATIO;
            float innerRadius = halfSize * KNOB_INNER_RATIO;
            float knurlMidRadius = (outerRadius + innerRadius) / 2f;
            float knurlDepth = (outerRadius - innerRadius) * 0.8f;
            
            // Draw radial knurl lines (ridges)
            for (int i = 0; i < KNURL_COUNT; i++)
            {
                float knurlAngle = (i * 360f / KNURL_COUNT) + angle;
                float rad = knurlAngle * Mathf.Deg2Rad;
                
                Vector2 inner = center + new Vector2(
                    Mathf.Cos(rad) * (innerRadius + 2f),
                    Mathf.Sin(rad) * (innerRadius + 2f)
                );
                Vector2 outer = center + new Vector2(
                    Mathf.Cos(rad) * (outerRadius - 1f),
                    Mathf.Sin(rad) * (outerRadius - 1f)
                );
                
                // Ridge highlight (left side of ridge catches light)
                float highlightAngle = knurlAngle + 3f;
                float highlightRad = highlightAngle * Mathf.Deg2Rad;
                
                // Draw groove (shadow)
                painter.strokeColor = KNOB_GROOVE;
                painter.lineWidth = 1.5f;
                painter.lineCap = LineCap.Butt;
                painter.BeginPath();
                painter.MoveTo(inner);
                painter.LineTo(outer);
                painter.Stroke();
                
                // Draw ridge highlight
                Vector2 innerH = center + new Vector2(
                    Mathf.Cos(highlightRad) * (innerRadius + 2f),
                    Mathf.Sin(highlightRad) * (innerRadius + 2f)
                );
                Vector2 outerH = center + new Vector2(
                    Mathf.Cos(highlightRad) * (outerRadius - 1f),
                    Mathf.Sin(highlightRad) * (outerRadius - 1f)
                );
                
                painter.strokeColor = KNOB_RIDGE;
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(innerH);
                painter.LineTo(outerH);
                painter.Stroke();
            }
        }
        
        private void DrawPointer(Painter2D painter, Vector2 center, float halfSize, float angle)
        {
            float pointerAngle = (angle - 90f) * Mathf.Deg2Rad;
            float innerRadius = halfSize * POINTER_START_RATIO;
            float outerRadius = halfSize * POINTER_END_RATIO;
            
            Vector2 pointerStart = center + new Vector2(
                Mathf.Cos(pointerAngle) * innerRadius,
                Mathf.Sin(pointerAngle) * innerRadius
            );
            Vector2 pointerEnd = center + new Vector2(
                Mathf.Cos(pointerAngle) * outerRadius,
                Mathf.Sin(pointerAngle) * outerRadius
            );
            
            // Engraved shadow (inset effect)
            painter.strokeColor = POINTER_SHADOW;
            painter.lineWidth = 4f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(pointerStart + new Vector2(0.5f, 0.5f));
            painter.LineTo(pointerEnd + new Vector2(0.5f, 0.5f));
            painter.Stroke();
            
            // Main pointer line (white fill of engraving)
            painter.strokeColor = POINTER_COLOR;
            painter.lineWidth = 2.5f;
            painter.BeginPath();
            painter.MoveTo(pointerStart);
            painter.LineTo(pointerEnd);
            painter.Stroke();
            
            // Center highlight
            painter.strokeColor = new Color(1f, 1f, 1f, 0.8f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(pointerStart);
            painter.LineTo(pointerEnd);
            painter.Stroke();
            
            // Center dot
            painter.fillColor = POINTER_COLOR;
            painter.BeginPath();
            painter.Arc(center, 3f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
        }
        
        private void DrawPositionMarkers(Painter2D painter, Vector2 center, float halfSize)
        {
            float markerRadius = halfSize * MARKER_RADIUS_RATIO;
            
            for (int i = 0; i < 3; i++)
            {
                float angle = (i - 1) * DETENT_ANGLE - 90f;
                float rad = angle * Mathf.Deg2Rad;
                
                Vector2 markerPos = center + new Vector2(
                    Mathf.Cos(rad) * markerRadius,
                    Mathf.Sin(rad) * markerRadius
                );
                
                bool isActive = (int)_position == i;
                
                if (isActive)
                {
                    // Glow effect for active position
                    painter.fillColor = new Color(MARKER_ACTIVE.r, MARKER_ACTIVE.g, MARKER_ACTIVE.b, 0.2f);
                    painter.BeginPath();
                    painter.Arc(markerPos, 8f, 0f, 360f);
                    painter.ClosePath();
                    painter.Fill();
                    
                    painter.fillColor = new Color(MARKER_ACTIVE.r, MARKER_ACTIVE.g, MARKER_ACTIVE.b, 0.4f);
                    painter.BeginPath();
                    painter.Arc(markerPos, 5f, 0f, 360f);
                    painter.ClosePath();
                    painter.Fill();
                }
                
                // Marker dot
                painter.fillColor = isActive ? MARKER_ACTIVE : MARKER_INACTIVE;
                painter.BeginPath();
                painter.Arc(markerPos, isActive ? 4f : 3f, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
                
                // Highlight on marker
                if (isActive)
                {
                    painter.fillColor = new Color(1f, 1f, 1f, 0.5f);
                    painter.BeginPath();
                    painter.Arc(markerPos - new Vector2(1f, 1f), 1.5f, 0f, 360f);
                    painter.ClosePath();
                    painter.Fill();
                }
            }
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        public void SetPositionImmediate(RotarySwitchPosition newPosition)
        {
            _position = newPosition;
            _displayAngle = ((int)newPosition - 1) * DETENT_ANGLE;
            _targetAngle = _displayAngle;
            
            _animationSchedule?.Pause();
            _animationSchedule = null;
            
            MarkDirtyRepaint();
            PositionChanged?.Invoke(_position);
        }
        
        public float GetCurrentAngle()
        {
            return _displayAngle;
        }
    }
}
