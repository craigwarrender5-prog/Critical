// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Gauges.cs - Arc Gauge, Bar Gauge, LED Rendering
// ============================================================================
//
// PURPOSE:
//   Reusable gauge rendering primitives for the new Validation Dashboard.
//   Provides arc gauges, bar gauges, bidirectional bars, LEDs, and
//   digital readouts. All rendering uses cached textures and GL primitives.
//
// PERFORMANCE CRITICAL:
//   - All GL material cached (GetGLMaterial)
//   - No allocations during rendering
//   - Arc segments use fixed vertex count (24 segments)
//
// REFERENCE:
//   Westinghouse 4-Loop PWR instrumentation conventions:
//     - Arc gauges with colored bands (green/amber/red)
//     - Digital readouts below gauges
//     - Status LEDs with on/off/warning states
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    public partial class ValidationDashboard
    {
        // ====================================================================
        // GL MATERIAL (cached for all GL drawing)
        // ====================================================================

        private static Material _glMat;

        /// <summary>
        /// Get or create the GL drawing material.
        /// </summary>
        private static Material GetGLMaterial()
        {
            if (_glMat == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _glMat = new Material(shader);
                _glMat.hideFlags = HideFlags.DontSave;
                _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _glMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _glMat.SetInt("_ZWrite", 0);
            }
            return _glMat;
        }

        // ====================================================================
        // ARC GAUGE
        // ====================================================================

        /// <summary>
        /// Draw an arc gauge with colored fill and needle.
        /// Arc spans 180° from left (min) to right (max), bottom-centered.
        /// </summary>
        /// <param name="center">Center point (bottom of semicircle)</param>
        /// <param name="radius">Arc radius in pixels</param>
        /// <param name="value">Current value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="valueColor">Color for fill and value text</param>
        /// <param name="label">Label text above gauge</param>
        /// <param name="valueText">Formatted value text</param>
        /// <param name="unitText">Unit text</param>
        internal void DrawGaugeArc(Vector2 center, float radius, float value,
            float min, float max, Color valueColor, string label, string valueText, string unitText)
        {
            if (Event.current.type != EventType.Repaint) return;

            float normalized = Mathf.Clamp01((value - min) / (max - min));

            // Draw arc background (full sweep)
            DrawArcSegment(center, radius, 0f, 1f, _cGaugeArcBg, 3f);

            // Draw filled arc up to value
            if (normalized > 0.001f)
            {
                DrawArcSegment(center, radius, 0f, normalized, valueColor, 3f);
            }

            // Draw needle
            float needleAngle = Mathf.Lerp(Mathf.PI, 0f, normalized);
            Vector2 needleTip = center + new Vector2(
                Mathf.Cos(needleAngle) * radius,
                -Mathf.Sin(needleAngle) * radius);
            DrawLine(center, needleTip, _cGaugeNeedle, 2f);

            // Center dot
            DrawFilledRect(new Rect(center.x - 2f, center.y - 2f, 4f, 4f), _cGaugeNeedle);

            // Label above arc
            Rect labelRect = new Rect(center.x - radius, center.y - radius - 12f, radius * 2f, 12f);
            GUI.Label(labelRect, label, _gaugeLabelStyle);

            // Value readout below arc
            Rect valRect = new Rect(center.x - radius, center.y + 2f, radius * 2f, 14f);
            GUI.contentColor = valueColor;
            GUI.Label(valRect, valueText, _gaugeValueStyle);

            // Units
            Rect unitRect = new Rect(center.x - radius, center.y + 14f, radius * 2f, 10f);
            GUI.contentColor = _cTextSecondary;
            GUI.Label(unitRect, unitText, _gaugeUnitStyle);
            GUI.contentColor = Color.white;
        }

        /// <summary>
        /// Draw an arc segment using GL lines.
        /// </summary>
        private static void DrawArcSegment(Vector2 center, float radius,
            float startFrac, float endFrac, Color color, float thickness)
        {
            GetGLMaterial().SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();

            const int segments = 24;
            float startAngle = Mathf.Lerp(Mathf.PI, 0f, startFrac);
            float endAngle = Mathf.Lerp(Mathf.PI, 0f, endFrac);

            // Draw thick arc as multiple offset lines
            for (float offset = -thickness / 2f; offset <= thickness / 2f; offset += 1f)
            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float angle = Mathf.Lerp(startAngle, endAngle, t);
                    float r = radius + offset;
                    float px = center.x + Mathf.Cos(angle) * r;
                    float py = center.y - Mathf.Sin(angle) * r;
                    GL.Vertex3(px, py, 0);
                }
                GL.End();
            }

            GL.PopMatrix();
        }

        // ====================================================================
        // BAR GAUGE
        // ====================================================================

        /// <summary>
        /// Draw a horizontal bar gauge with label and value.
        /// </summary>
        internal void DrawBarGauge(Rect area, string label, float value, float min, float max, Color barColor)
        {
            float labelW = 70f;
            float valueW = 50f;
            float barW = area.width - labelW - valueW - 8f;
            float barH = area.height - 4f;

            // Label
            Rect labelRect = new Rect(area.x, area.y, labelW, area.height);
            GUI.Label(labelRect, label, _statusLabelStyle);

            // Bar background
            Rect barBgRect = new Rect(area.x + labelW + 2f, area.y + 2f, barW, barH);
            GUI.DrawTexture(barBgRect, _gaugeArcBgTex);

            // Bar fill
            float frac = Mathf.Clamp01((value - min) / (max - min));
            Rect fillRect = new Rect(barBgRect.x, barBgRect.y, barBgRect.width * frac, barBgRect.height);
            GUI.DrawTexture(fillRect, GetColorTex(barColor));

            // Value text
            Rect valueRect = new Rect(area.x + labelW + barW + 6f, area.y, valueW, area.height);
            GUI.contentColor = barColor;
            GUI.Label(valueRect, $"{value:F1}", _statusValueStyle);
            GUI.contentColor = Color.white;
        }

        /// <summary>
        /// Draw a vertical bar gauge (for column layouts).
        /// </summary>
        internal void DrawVerticalBar(Rect area, string label, float value, float min, float max, Color barColor)
        {
            float labelH = 14f;
            float valueH = 14f;
            float barH = area.height - labelH - valueH - 4f;

            // Label at top
            Rect labelRect = new Rect(area.x, area.y, area.width, labelH);
            GUI.Label(labelRect, label, _gaugeLabelStyle);

            // Bar background
            Rect barBgRect = new Rect(area.x + 2f, area.y + labelH + 2f, area.width - 4f, barH);
            GUI.DrawTexture(barBgRect, _gaugeArcBgTex);

            // Bar fill (from bottom)
            float frac = Mathf.Clamp01((value - min) / (max - min));
            float fillH = barBgRect.height * frac;
            Rect fillRect = new Rect(barBgRect.x, barBgRect.y + barBgRect.height - fillH, 
                barBgRect.width, fillH);
            GUI.DrawTexture(fillRect, GetColorTex(barColor));

            // Value at bottom
            Rect valueRect = new Rect(area.x, area.y + area.height - valueH, area.width, valueH);
            GUI.contentColor = barColor;
            GUI.Label(valueRect, $"{value:F1}", _gaugeValueStyle);
            GUI.contentColor = Color.white;
        }

        /// <summary>
        /// Draw a bidirectional bar (centered zero, +/- fill).
        /// </summary>
        internal void DrawBidirectionalBar(Rect area, string label, float value, float min, float max)
        {
            float labelW = 70f;
            float valueW = 50f;
            float barW = area.width - labelW - valueW - 8f;
            float barH = area.height - 4f;

            // Label
            Rect labelRect = new Rect(area.x, area.y, labelW, area.height);
            GUI.Label(labelRect, label, _statusLabelStyle);

            // Bar background
            Rect barBgRect = new Rect(area.x + labelW + 2f, area.y + 2f, barW, barH);
            GUI.DrawTexture(barBgRect, _gaugeArcBgTex);

            // Center line
            float centerX = barBgRect.x + barBgRect.width / 2f;
            DrawLine(new Vector2(centerX, barBgRect.y), 
                     new Vector2(centerX, barBgRect.y + barBgRect.height), 
                     _cGaugeTick, 1f);

            // Fill bar
            float range = max - min;
            float normalized = (value - min) / range; // 0 to 1
            float halfW = barBgRect.width / 2f;

            Color barColor = value >= 0 ? _cNormalGreen : _cWarningAmber;
            
            if (value >= 0)
            {
                // Positive: fill right of center
                float fillW = (value / max) * halfW;
                fillW = Mathf.Min(fillW, halfW);
                Rect fillRect = new Rect(centerX, barBgRect.y, fillW, barBgRect.height);
                GUI.DrawTexture(fillRect, GetColorTex(barColor));
            }
            else
            {
                // Negative: fill left of center
                float fillW = (-value / -min) * halfW;
                fillW = Mathf.Min(fillW, halfW);
                Rect fillRect = new Rect(centerX - fillW, barBgRect.y, fillW, barBgRect.height);
                GUI.DrawTexture(fillRect, GetColorTex(barColor));
            }

            // Value text
            Rect valueRect = new Rect(area.x + labelW + barW + 6f, area.y, valueW, area.height);
            GUI.contentColor = barColor;
            GUI.Label(valueRect, $"{value:+0.0;-0.0;0.0}", _statusValueStyle);
            GUI.contentColor = Color.white;
        }

        // ====================================================================
        // LED INDICATOR
        // ====================================================================

        /// <summary>
        /// Draw an LED indicator with label.
        /// </summary>
        /// <param name="area">Area for LED and label</param>
        /// <param name="label">Label text</param>
        /// <param name="isOn">Is LED on?</param>
        /// <param name="isWarning">Use warning color instead of normal green?</param>
        internal void DrawLED(Rect area, string label, bool isOn, bool isWarning)
        {
            float ledSize = 12f;
            float ledX = area.x;
            float ledY = area.y + (area.height - ledSize) / 2f;

            // LED circle (using rect approximation)
            Rect ledRect = new Rect(ledX, ledY, ledSize, ledSize);
            Texture2D ledTex;
            if (!isOn)
                ledTex = _ledOffTex;
            else if (isWarning)
                ledTex = _ledWarningTex;
            else
                ledTex = _ledOnTex;
            GUI.DrawTexture(ledRect, ledTex);

            // Label
            Rect labelRect = new Rect(area.x + ledSize + 4f, area.y, 
                area.width - ledSize - 4f, area.height);
            GUI.contentColor = isOn ? _cTextPrimary : _cTextSecondary;
            GUI.Label(labelRect, label, _ledLabelStyle);
            GUI.contentColor = Color.white;
        }

        /// <summary>
        /// Draw an alarm LED (red when active).
        /// </summary>
        internal void DrawAlarmLED(Rect area, string label, bool isAlarming)
        {
            float ledSize = 12f;
            float ledX = area.x;
            float ledY = area.y + (area.height - ledSize) / 2f;

            Rect ledRect = new Rect(ledX, ledY, ledSize, ledSize);
            Texture2D ledTex = isAlarming ? _ledAlarmTex : _ledOffTex;
            GUI.DrawTexture(ledRect, ledTex);

            Rect labelRect = new Rect(area.x + ledSize + 4f, area.y,
                area.width - ledSize - 4f, area.height);
            GUI.contentColor = isAlarming ? _cAlarmRed : _cTextSecondary;
            GUI.Label(labelRect, label, _ledLabelStyle);
            GUI.contentColor = Color.white;
        }

        // ====================================================================
        // DIGITAL READOUT
        // ====================================================================

        /// <summary>
        /// Draw a digital readout with label, value, and unit.
        /// </summary>
        internal void DrawDigitalReadout(Rect area, string label, float value, string unit, 
            string format, Color valueColor)
        {
            float labelW = area.width * 0.4f;
            float valueW = area.width * 0.4f;
            float unitW = area.width * 0.2f;

            // Label
            Rect labelRect = new Rect(area.x, area.y, labelW, area.height);
            GUI.Label(labelRect, label, _statusLabelStyle);

            // Value
            Rect valueRect = new Rect(area.x + labelW, area.y, valueW, area.height);
            GUI.contentColor = valueColor;
            GUI.Label(valueRect, value.ToString(format), _digitalReadoutStyle);

            // Unit
            Rect unitRect = new Rect(area.x + labelW + valueW, area.y, unitW, area.height);
            GUI.contentColor = _cTextSecondary;
            GUI.Label(unitRect, unit, _statusLabelStyle);
            GUI.contentColor = Color.white;
        }

        /// <summary>
        /// Draw a digital readout with a string value (for modes/states).
        /// </summary>
        internal void DrawDigitalReadoutString(Rect area, string label, string stringValue, Color valueColor)
        {
            float labelW = area.width * 0.4f;
            float valueW = area.width * 0.6f;

            Rect labelRect = new Rect(area.x, area.y, labelW, area.height);
            GUI.Label(labelRect, label, _statusLabelStyle);

            Rect valueRect = new Rect(area.x + labelW, area.y, valueW, area.height);
            GUI.contentColor = valueColor;
            GUI.Label(valueRect, stringValue, _digitalReadoutStyle);
            GUI.contentColor = Color.white;
        }

        // ====================================================================
        // GL PRIMITIVES
        // ====================================================================

        /// <summary>
        /// Draw a line between two points.
        /// </summary>
        internal static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            GetGLMaterial().SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();

            Vector2 dir = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            for (float off = -width / 2f; off <= width / 2f; off += 1f)
            {
                Vector2 offset = perp * off;
                GL.Begin(GL.LINES);
                GL.Color(color);
                GL.Vertex3(a.x + offset.x, a.y + offset.y, 0);
                GL.Vertex3(b.x + offset.x, b.y + offset.y, 0);
                GL.End();
            }

            GL.PopMatrix();
        }

        /// <summary>
        /// Draw a filled rectangle.
        /// </summary>
        private void DrawFilledRect(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, GetColorTex(color));
        }
    }
}
