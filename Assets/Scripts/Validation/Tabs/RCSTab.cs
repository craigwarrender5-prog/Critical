// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// RCSTab.cs - RCS Primary System Detail Tab
// ============================================================================
//
// PURPOSE:
//   Expanded view of Reactor Coolant System parameters with larger gauges,
//   complete parameter list, and dedicated trend graphs for temperatures
//   and pressure.
//
// LAYOUT:
//   ┌─────────────────┬─────────────────┬─────────────────────────────────┐
//   │   TEMPERATURES  │    PRESSURE     │          TRENDS                 │
//   │      (35%)      │     (25%)       │           (40%)                 │
//   │                 │                 │                                 │
//   │   [LARGE ARC]   │  [LARGE ARC]    │  ┌─────────────────────────┐   │
//   │     T_AVG       │   PRESSURE      │  │ T_AVG / T_HOT / T_COLD  │   │
//   │                 │                 │  └─────────────────────────┘   │
//   │   T_HOT  ───    │   T_SAT  ───    │  ┌─────────────────────────┐   │
//   │   T_COLD ───    │   SUBCOOL ───   │  │     RCS PRESSURE        │   │
//   │   CORE ΔT ───   │   RATE  ───     │  └─────────────────────────┘   │
//   │   T_PZR  ───    │                 │  ┌─────────────────────────┐   │
//   │   T_RCS  ───    │   [ARC]         │  │     HEATUP RATE         │   │
//   │                 │   SUBCOOL       │  └─────────────────────────┘   │
//   │   [ARC]         │                 │  ┌─────────────────────────┐   │
//   │   HEATUP RATE   │   RCPs ●●●●     │  │     SUBCOOLING          │   │
//   │                 │   HEAT MW ───   │  └─────────────────────────┘   │
//   └─────────────────┴─────────────────┴─────────────────────────────────┘
//
// REFERENCE:
//   NRC HRTD Section 3 — Reactor Coolant System
//   Westinghouse 4-Loop PWR RCS monitoring requirements
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// RCS detail tab with expanded temperature and pressure monitoring.
    /// </summary>
    public class RCSTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public RCSTab(ValidationDashboard dashboard) 
            : base(dashboard, "RCS", 1)
        {
        }

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private Rect _tempCol;
        private Rect _pressCol;
        private Rect _trendsCol;

        private const float TEMP_FRAC = 0.35f;
        private const float PRESS_FRAC = 0.25f;
        // TRENDS_FRAC = remainder (0.40f)

        private const float COL_GAP = 6f;
        private const float PAD = 8f;

        private float _cachedW;
        private float _cachedH;

        private void CalculateLayout(Rect area)
        {
            if (Mathf.Approximately(_cachedW, area.width) &&
                Mathf.Approximately(_cachedH, area.height))
                return;

            _cachedW = area.width;
            _cachedH = area.height;

            float availW = area.width - PAD * 2 - COL_GAP * 2;
            float tempW = availW * TEMP_FRAC;
            float pressW = availW * PRESS_FRAC;
            float trendsW = availW - tempW - pressW;

            float x = area.x + PAD;
            float y = area.y + PAD;
            float h = area.height - PAD * 2;

            _tempCol = new Rect(x, y, tempW, h);
            x += tempW + COL_GAP;

            _pressCol = new Rect(x, y, pressW, h);
            x += pressW + COL_GAP;

            _trendsCol = new Rect(x, y, trendsW, h);
        }

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            CalculateLayout(area);

            DrawTemperatureColumn();
            DrawPressureColumn();
            DrawTrendsColumn();
        }

        // ====================================================================
        // TEMPERATURE COLUMN
        // ====================================================================

        private void DrawTemperatureColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_tempCol, "TEMPERATURES");

            float y = _tempCol.y + 26f;
            float colW = _tempCol.width;
            float centerX = _tempCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // Large T_avg arc gauge
            float gaugeR = 45f;
            Vector2 tavgCenter = new Vector2(centerX, y + 55f);
            Color tavgColor = ValidationDashboard.GetThresholdColor(s.T_avg, 100f, 500f, 50f, 557f);
            d.DrawGaugeArc(tavgCenter, gaugeR, s.T_avg, 50f, 600f, tavgColor,
                "T_AVG", $"{s.T_avg:F1}", "°F");
            y += 130f;

            // Temperature readouts
            d.DrawDigitalReadout(new Rect(_tempCol.x + 8f, y, readoutW, 20f),
                "T_HOT", s.T_hot, "°F", "F1",
                ValidationDashboard.GetHighThresholdColor(s.T_hot, 580f, 600f));
            y += 24f;

            d.DrawDigitalReadout(new Rect(_tempCol.x + 8f, y, readoutW, 20f),
                "T_COLD", s.T_cold, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_tempCol.x + 8f, y, readoutW, 20f),
                "CORE ΔT", s.CoreDeltaT, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_tempCol.x + 8f, y, readoutW, 20f),
                "T_PZR", s.T_pzr, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_tempCol.x + 8f, y, readoutW, 20f),
                "T_RCS BULK", s.T_rcs, "°F", "F1", ValidationDashboard._cTextSecondary);
            y += 32f;

            // Heatup rate arc gauge
            Vector2 heatupCenter = new Vector2(centerX, y + 40f);
            Color heatupColor = ValidationDashboard.GetThresholdColor(s.HeatupRate, 10f, 45f, 5f, 50f);
            d.DrawGaugeArc(heatupCenter, 32f, s.HeatupRate, 0f, 60f, heatupColor,
                "HEATUP RATE", $"{s.HeatupRate:F1}", "°F/hr");
            y += 95f;

            // Additional readouts
            d.DrawDigitalReadout(new Rect(_tempCol.x + 8f, y, readoutW, 20f),
                "PRESS RATE", s.PressureRate, "psi/hr", "F1", ValidationDashboard._cTextPrimary);
        }

        // ====================================================================
        // PRESSURE COLUMN
        // ====================================================================

        private void DrawPressureColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_pressCol, "PRESSURE");

            float y = _pressCol.y + 26f;
            float colW = _pressCol.width;
            float centerX = _pressCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // Large pressure arc gauge
            float gaugeR = 45f;
            Vector2 pressCenter = new Vector2(centerX, y + 55f);
            Color pressColor = ValidationDashboard.GetThresholdColor(s.Pressure, 400f, 2235f, 350f, 2400f);
            d.DrawGaugeArc(pressCenter, gaugeR, s.Pressure, 0f, 2500f, pressColor,
                "RCS PRESSURE", $"{s.Pressure:F0}", "psia");
            y += 130f;

            // Pressure-related readouts
            d.DrawDigitalReadout(new Rect(_pressCol.x + 8f, y, readoutW, 20f),
                "T_SAT", s.T_sat, "°F", "F1", ValidationDashboard._cTextSecondary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_pressCol.x + 8f, y, readoutW, 20f),
                "SUBCOOL", s.Subcooling, "°F", "F1",
                ValidationDashboard.GetLowThresholdColor(s.Subcooling, 30f, 20f));
            y += 32f;

            // Subcooling arc gauge
            Vector2 subcoolCenter = new Vector2(centerX, y + 40f);
            Color subcoolColor = ValidationDashboard.GetLowThresholdColor(s.Subcooling, 30f, 20f);
            d.DrawGaugeArc(subcoolCenter, 32f, s.Subcooling, 0f, 100f, subcoolColor,
                "SUBCOOLING", $"{s.Subcooling:F1}", "°F");
            y += 100f;

            // RCP section
            d.DrawSubsectionDivider(_pressCol, y, "RCPs");
            y += 20f;

            // RCP LEDs (2x2 grid)
            float ledW = (colW - 24f) / 2f;
            d.DrawLED(new Rect(_pressCol.x + 8f, y, ledW, 20f), "RCP 1", s.RcpRunning[0], false);
            d.DrawLED(new Rect(_pressCol.x + 8f + ledW + 8f, y, ledW, 20f), "RCP 2", s.RcpRunning[1], false);
            y += 24f;
            d.DrawLED(new Rect(_pressCol.x + 8f, y, ledW, 20f), "RCP 3", s.RcpRunning[2], false);
            d.DrawLED(new Rect(_pressCol.x + 8f + ledW + 8f, y, ledW, 20f), "RCP 4", s.RcpRunning[3], false);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_pressCol.x + 8f, y, readoutW, 20f),
                "RCP COUNT", s.RcpCount, "", "F0", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_pressCol.x + 8f, y, readoutW, 20f),
                "RCP HEAT", s.RcpHeat, "MW", "F2", ValidationDashboard._cCyanInfo);
        }

        // ====================================================================
        // TRENDS COLUMN
        // ====================================================================

        private void DrawTrendsColumn()
        {
            var d = Dashboard;
            var sm = d.SparklineManager;

            d.DrawColumnFrame(_trendsCol, "TRENDS");

            float y = _trendsCol.y + 26f;
            float sparkW = _trendsCol.width - 16f;
            float sparkH = (_trendsCol.height - 26f - 32f) / 4f;

            // Draw 4 larger sparklines for RCS-relevant parameters
            if (sm != null && sm.IsInitialized)
            {
                // T_AVG (index 2)
                Rect spark0 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(2, spark0, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // RCS PRESSURE (index 0)
                Rect spark1 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(0, spark1, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // HEATUP RATE (index 3)
                Rect spark2 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(3, spark2, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // SUBCOOLING (index 4)
                Rect spark3 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(4, spark3, d._gaugeLabelStyle, d._statusValueStyle);
            }
            else
            {
                // Placeholder
                GUI.contentColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(_trendsCol.x + 8f, y, sparkW, 20f),
                    "Sparklines initializing...", d._statusLabelStyle);
                GUI.contentColor = Color.white;
            }
        }
    }
}
