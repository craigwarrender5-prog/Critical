// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// TrendArrowPOC.cs — Rate-of-Change Trend Arrow Indicator
// ============================================================================
//
// PURPOSE:
//   Standalone directional arrow that indicates rate-of-change for any
//   plant parameter. Designed to sit adjacent to analog gauges in the
//   Core Flight Deck. Arrow direction indicates sign (up = rising,
//   down = falling), arrow size/length scales with magnitude.
//
// INTEGRATION:
//   In production, fed from ScreenDataBridge rate getters:
//     - GetPressureRate()  → psi/hr
//     - GetHeatupRate()    → °F/hr
//   Or any computed delta-over-time for other parameters.
//
// RENDERING:
//   Uses Painter2D to draw a filled triangular arrowhead with a stem.
//   Color transitions from green (normal) → amber (elevated) → red (alarm)
//   based on configurable magnitude thresholds.
//
// CONVENTIONS:
//   - [UxmlElement] attribute (Unity 6 pattern, not UxmlFactory)
//   - Painter2D with degrees for Arc() calls
//   - Matches Critical.UI.POC namespace and coding style
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    /// <summary>
    /// A rate-of-change trend arrow indicator for UI Toolkit.
    /// Points up when value > 0 (rising), down when value < 0 (falling).
    /// Arrow length scales with magnitude. Color indicates severity.
    /// </summary>
    [UxmlElement]
    public partial class TrendArrowPOC : VisualElement
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        /// <summary>
        /// Minimum absolute value below which no arrow is drawn (deadband).
        /// Prevents flickering on noise near zero.
        /// </summary>
        private const float DEFAULT_DEADBAND = 0.5f;

        /// <summary>Arrowhead width as fraction of element width.</summary>
        private const float ARROWHEAD_WIDTH_FRACTION = 0.6f;

        /// <summary>Arrowhead height as fraction of total arrow length.</summary>
        private const float ARROWHEAD_HEIGHT_FRACTION = 0.4f;

        /// <summary>Stem width as fraction of element width.</summary>
        private const float STEM_WIDTH_FRACTION = 0.15f;

        /// <summary>Minimum arrow length as fraction of element height (at deadband edge).</summary>
        private const float MIN_ARROW_FRACTION = 0.3f;

        /// <summary>Maximum arrow length as fraction of element height (at maxMagnitude).</summary>
        private const float MAX_ARROW_FRACTION = 0.85f;

        // ====================================================================
        // COLORS
        // ====================================================================

        private static readonly Color COLOR_NORMAL = new Color(0f, 1f, 0.533f, 1f);     // Green
        private static readonly Color COLOR_ELEVATED = new Color(1f, 0.667f, 0f, 1f);   // Amber
        private static readonly Color COLOR_ALARM = new Color(1f, 0.267f, 0.267f, 1f);  // Red
        private static readonly Color COLOR_NEUTRAL = new Color(0.4f, 0.4f, 0.45f, 1f); // Dim gray for deadband dash

        // ====================================================================
        // BACKING FIELDS
        // ====================================================================

        private float _value = 0f;
        private float _maxMagnitude = 100f;
        private float _deadband = DEFAULT_DEADBAND;
        private float _elevatedThreshold = 50f;
        private float _alarmThreshold = 80f;

        // ====================================================================
        // UXML PROPERTIES
        // ====================================================================

        /// <summary>
        /// Rate-of-change value. Positive = rising (arrow up), negative = falling (arrow down).
        /// Values within ±deadband show a neutral indicator.
        /// </summary>
        [UxmlAttribute]
        public float value
        {
            get => _value;
            set
            {
                if (Mathf.Abs(_value - value) > 0.001f)
                {
                    _value = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Maximum expected magnitude. Arrow reaches full length at this value.
        /// Values beyond this are clamped.
        /// </summary>
        [UxmlAttribute]
        public float maxMagnitude
        {
            get => _maxMagnitude;
            set { _maxMagnitude = Mathf.Max(value, 0.001f); MarkDirtyRepaint(); }
        }

        /// <summary>
        /// Deadband around zero. Absolute values below this show neutral (no arrow).
        /// </summary>
        [UxmlAttribute]
        public float deadband
        {
            get => _deadband;
            set { _deadband = Mathf.Max(value, 0f); MarkDirtyRepaint(); }
        }

        /// <summary>
        /// Magnitude threshold for amber (elevated) color.
        /// </summary>
        [UxmlAttribute]
        public float elevatedThreshold
        {
            get => _elevatedThreshold;
            set { _elevatedThreshold = value; MarkDirtyRepaint(); }
        }

        /// <summary>
        /// Magnitude threshold for red (alarm) color.
        /// </summary>
        [UxmlAttribute]
        public float alarmThreshold
        {
            get => _alarmThreshold;
            set { _alarmThreshold = value; MarkDirtyRepaint(); }
        }

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public TrendArrowPOC()
        {
            style.minWidth = 24;
            style.minHeight = 48;

            generateVisualContent += OnGenerateVisualContent;
        }

        // ====================================================================
        // RENDERING
        // ====================================================================

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;

            if (width < 8f || height < 16f)
                return;

            var painter = mgc.painter2D;
            if (painter == null)
                return;

            float absMag = Mathf.Abs(_value);

            // ---------------------------------------------------------
            // DEADBAND — draw a small neutral horizontal dash
            // ---------------------------------------------------------
            if (absMag <= _deadband)
            {
                DrawNeutralDash(painter, width, height);
                return;
            }

            // ---------------------------------------------------------
            // COMPUTE ARROW PARAMETERS
            // ---------------------------------------------------------

            // Normalize magnitude: 0 (at deadband) to 1 (at maxMagnitude)
            float normalizedMag = Mathf.Clamp01((absMag - _deadband) / (_maxMagnitude - _deadband));

            // Arrow length scales between min and max fractions of element height
            float arrowLength = height * Mathf.Lerp(MIN_ARROW_FRACTION, MAX_ARROW_FRACTION, normalizedMag);

            // Direction: 1 = up, -1 = down
            float direction = (_value > 0f) ? 1f : -1f;

            // Color based on magnitude
            Color arrowColor = GetMagnitudeColor(absMag);

            // ---------------------------------------------------------
            // DRAW ARROW
            // ---------------------------------------------------------

            float centerX = width * 0.5f;
            float centerY = height * 0.5f;

            // Arrow runs from base to tip, centered vertically
            float halfArrow = arrowLength * 0.5f;

            // Tip is at the top (if direction=up) or bottom (if direction=down)
            float tipY = centerY - (direction * halfArrow);
            float baseY = centerY + (direction * halfArrow);

            // Arrowhead dimensions
            float arrowheadHeight = arrowLength * ARROWHEAD_HEIGHT_FRACTION;
            float arrowheadHalfWidth = width * ARROWHEAD_WIDTH_FRACTION * 0.5f;

            // Where arrowhead meets stem
            float neckY = tipY + (direction * arrowheadHeight);

            // Stem dimensions
            float stemHalfWidth = width * STEM_WIDTH_FRACTION * 0.5f;

            // Draw filled arrow shape (arrowhead + stem as one path)
            painter.fillColor = arrowColor;
            painter.BeginPath();

            // Start at tip
            painter.MoveTo(new Vector2(centerX, tipY));

            // Right side of arrowhead
            painter.LineTo(new Vector2(centerX + arrowheadHalfWidth, neckY));

            // Step in to stem right edge
            painter.LineTo(new Vector2(centerX + stemHalfWidth, neckY));

            // Down stem right side to base
            painter.LineTo(new Vector2(centerX + stemHalfWidth, baseY));

            // Across base
            painter.LineTo(new Vector2(centerX - stemHalfWidth, baseY));

            // Up stem left side to neck
            painter.LineTo(new Vector2(centerX - stemHalfWidth, neckY));

            // Step out to left arrowhead
            painter.LineTo(new Vector2(centerX - arrowheadHalfWidth, neckY));

            // Close back to tip
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>
        /// Draws a small horizontal dash to indicate zero/deadband rate.
        /// </summary>
        private void DrawNeutralDash(Painter2D painter, float width, float height)
        {
            float centerY = height * 0.5f;
            float dashHalfWidth = width * 0.3f;
            float dashHalfHeight = 1.5f;

            painter.fillColor = COLOR_NEUTRAL;
            painter.BeginPath();
            painter.MoveTo(new Vector2(width * 0.5f - dashHalfWidth, centerY - dashHalfHeight));
            painter.LineTo(new Vector2(width * 0.5f + dashHalfWidth, centerY - dashHalfHeight));
            painter.LineTo(new Vector2(width * 0.5f + dashHalfWidth, centerY + dashHalfHeight));
            painter.LineTo(new Vector2(width * 0.5f - dashHalfWidth, centerY + dashHalfHeight));
            painter.ClosePath();
            painter.Fill();
        }

        /// <summary>
        /// Determine arrow color based on absolute magnitude and thresholds.
        /// </summary>
        private Color GetMagnitudeColor(float absMagnitude)
        {
            if (absMagnitude >= _alarmThreshold)
                return COLOR_ALARM;
            if (absMagnitude >= _elevatedThreshold)
                return COLOR_ELEVATED;
            return COLOR_NORMAL;
        }
    }
}
