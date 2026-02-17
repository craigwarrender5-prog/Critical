// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Panels.cs - Panel and Section Rendering Helpers
// ============================================================================
//
// PURPOSE:
//   Reusable panel and section rendering helpers for the Validation Dashboard.
//   Provides column headers, section dividers, status rows, and panel frames.
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
        // SECTION RENDERING
        // ====================================================================

        /// <summary>
        /// Draw a section header bar at the top of a column.
        /// </summary>
        private void DrawSectionHeader(Rect column, string title)
        {
            Rect headerRect = new Rect(column.x, column.y, column.width, SECTION_HEADER_HEIGHT);
            GUI.DrawTexture(headerRect, _headerTex);
            GUI.Label(headerRect, title, _sectionHeaderStyle);
        }

        /// <summary>
        /// Draw a subsection divider within a column.
        /// </summary>
        internal void DrawSubsectionDivider(Rect column, float y, string label)
        {
            float lineY = y + 6f;
            float labelW = 40f;
            float centerX = column.x + column.width / 2f;

            // Left line
            DrawLine(new Vector2(column.x + 4f, lineY),
                     new Vector2(centerX - labelW / 2f - 4f, lineY),
                     _cTextSecondary, 1f);

            // Label
            Rect labelRect = new Rect(centerX - labelW / 2f, y, labelW, 12f);
            GUI.contentColor = _cTextSecondary;
            GUI.Label(labelRect, label, _gaugeLabelStyle);
            GUI.contentColor = Color.white;

            // Right line
            DrawLine(new Vector2(centerX + labelW / 2f + 4f, lineY),
                     new Vector2(column.x + column.width - 4f, lineY),
                     _cTextSecondary, 1f);
        }

        // ====================================================================
        // STATUS ROW RENDERING
        // ====================================================================

        /// <summary>
        /// Draw a status row with label and value.
        /// </summary>
        private void DrawStatusRow(Rect column, ref float y, string label, string value, Color valueColor)
        {
            float labelW = column.width * 0.55f;
            float valueW = column.width - labelW - 8f;

            Rect labelRect = new Rect(column.x + 4f, y, labelW, READOUT_HEIGHT);
            GUI.Label(labelRect, label, _statusLabelStyle);

            Rect valueRect = new Rect(column.x + 4f + labelW, y, valueW, READOUT_HEIGHT);
            GUI.contentColor = valueColor;
            GUI.Label(valueRect, value, _statusValueStyle);
            GUI.contentColor = Color.white;

            y += READOUT_SPACING;
        }

        /// <summary>
        /// Draw a status row with default (primary) value color.
        /// </summary>
        private void DrawStatusRow(Rect column, ref float y, string label, string value)
        {
            DrawStatusRow(column, ref y, label, value, _cTextPrimary);
        }

        /// <summary>
        /// Draw a status row with dynamic threshold coloring.
        /// </summary>
        private void DrawStatusRowThreshold(Rect column, ref float y, string label, 
            float value, string format, float warnLow, float warnHigh, float alarmLow, float alarmHigh)
        {
            Color color = GetThresholdColor(value, warnLow, warnHigh, alarmLow, alarmHigh);
            DrawStatusRow(column, ref y, label, value.ToString(format), color);
        }

        // ====================================================================
        // PANEL FRAMES
        // ====================================================================

        /// <summary>
        /// Draw a panel background with rounded corners (approximation).
        /// </summary>
        private void DrawPanelFrame(Rect area)
        {
            GUI.Box(area, GUIContent.none, _panelBgStyle);
        }

        /// <summary>
        /// Draw a column with header and background.
        /// </summary>
        internal void DrawColumnFrame(Rect column, string title)
        {
            DrawPanelFrame(column);
            DrawSectionHeader(column, title);
        }

        // ====================================================================
        // COMPACT GAUGE GROUPS
        // ====================================================================

        /// <summary>
        /// Draw a compact 2-gauge row (two arc gauges side by side).
        /// </summary>
        private void DrawDualArcGaugeRow(Rect column, float y,
            float value1, float min1, float max1, Color color1, string label1, string unit1,
            float value2, float min2, float max2, Color color2, string label2, string unit2)
        {
            float halfW = column.width / 2f;
            float gaugeR = ARC_GAUGE_SIZE * 0.35f;

            // Left gauge
            Vector2 center1 = new Vector2(column.x + halfW / 2f, y + ARC_GAUGE_SIZE / 2f);
            DrawGaugeArc(center1, gaugeR, value1, min1, max1, color1, label1, 
                value1.ToString("F1"), unit1);

            // Right gauge
            Vector2 center2 = new Vector2(column.x + halfW + halfW / 2f, y + ARC_GAUGE_SIZE / 2f);
            DrawGaugeArc(center2, gaugeR, value2, min2, max2, color2, label2,
                value2.ToString("F1"), unit2);
        }

        /// <summary>
        /// Draw a compact 4-LED row.
        /// </summary>
        private void DrawQuadLedRow(Rect column, float y,
            string label1, bool on1, bool warn1,
            string label2, bool on2, bool warn2,
            string label3, bool on3, bool warn3,
            string label4, bool on4, bool warn4)
        {
            float quarterW = (column.width - 12f) / 4f;
            float x = column.x + 4f;

            DrawLED(new Rect(x, y, quarterW, LED_HEIGHT), label1, on1, warn1);
            x += quarterW;
            DrawLED(new Rect(x, y, quarterW, LED_HEIGHT), label2, on2, warn2);
            x += quarterW;
            DrawLED(new Rect(x, y, quarterW, LED_HEIGHT), label3, on3, warn3);
            x += quarterW;
            DrawLED(new Rect(x, y, quarterW, LED_HEIGHT), label4, on4, warn4);
        }

    }
}
