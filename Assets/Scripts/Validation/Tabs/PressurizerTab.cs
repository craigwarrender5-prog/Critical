// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// PressurizerTab.cs - Pressurizer System Detail Tab
// ============================================================================
//
// PURPOSE:
//   Expanded view of Pressurizer parameters with larger gauges, complete
//   parameter list including heater banks, spray system, surge flow, and
//   bubble formation details.
//
// LAYOUT:
//   ┌─────────────────┬─────────────────┬─────────────────────────────────┐
//   │  PRESSURE/LEVEL │  HEATER/SPRAY   │          TRENDS                 │
//   │      (30%)      │     (30%)       │           (40%)                 │
//   │                 │                 │                                 │
//   │   [LARGE ARC]   │  HEATER POWER   │  ┌─────────────────────────┐   │
//   │    PRESSURE     │  ═══════════    │  │     PZR PRESSURE        │   │
//   │                 │  MODE: xxxxxx   │  └─────────────────────────┘   │
//   │   [LARGE ARC]   │                 │  ┌─────────────────────────┐   │
//   │     LEVEL       │  SPRAY VALVE    │  │     PZR LEVEL           │   │
//   │                 │  ═══════════    │  └─────────────────────────┘   │
//   │   WATER ───     │  FLOW  ───      │  ┌─────────────────────────┐   │
//   │   STEAM ───     │  ACTIVE ●       │  │     SURGE FLOW          │   │
//   │                 │                 │  └─────────────────────────┘   │
//   │   SURGE ══════  │  BUBBLE ●       │  ┌─────────────────────────┐   │
//   │                 │  PHASE: xxxxx   │  │     HEATER POWER        │   │
//   │                 │                 │  └─────────────────────────┘   │
//   └─────────────────┴─────────────────┴─────────────────────────────────┘
//
// REFERENCE:
//   NRC HRTD Section 4 — Pressurizer System
//   Westinghouse Electric Heater Control Logic
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// Pressurizer detail tab with expanded pressure, level, and control monitoring.
    /// </summary>
    public class PressurizerTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public PressurizerTab(ValidationDashboard dashboard) 
            : base(dashboard, "PZR", 2)
        {
        }

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private Rect _pressLevelCol;
        private Rect _controlCol;
        private Rect _trendsCol;

        private const float PRESS_FRAC = 0.30f;
        private const float CONTROL_FRAC = 0.30f;
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
            float pressW = availW * PRESS_FRAC;
            float controlW = availW * CONTROL_FRAC;
            float trendsW = availW - pressW - controlW;

            float x = area.x + PAD;
            float y = area.y + PAD;
            float h = area.height - PAD * 2;

            _pressLevelCol = new Rect(x, y, pressW, h);
            x += pressW + COL_GAP;

            _controlCol = new Rect(x, y, controlW, h);
            x += controlW + COL_GAP;

            _trendsCol = new Rect(x, y, trendsW, h);
        }

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            CalculateLayout(area);

            DrawPressureLevelColumn();
            DrawControlColumn();
            DrawTrendsColumn();
        }

        // ====================================================================
        // PRESSURE/LEVEL COLUMN
        // ====================================================================

        private void DrawPressureLevelColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_pressLevelCol, "PRESSURE / LEVEL");

            float y = _pressLevelCol.y + 26f;
            float colW = _pressLevelCol.width;
            float centerX = _pressLevelCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // Large pressure arc gauge
            float gaugeR = 45f;
            Vector2 pressCenter = new Vector2(centerX, y + 55f);
            Color pressColor = ValidationDashboard.GetThresholdColor(s.Pressure, 400f, 2235f, 350f, 2400f);
            d.DrawGaugeArc(pressCenter, gaugeR, s.Pressure, 0f, 2500f, pressColor,
                "PRESSURE", $"{s.Pressure:F0}", "psia");
            y += 130f;

            // Large level arc gauge
            Vector2 levelCenter = new Vector2(centerX, y + 45f);
            Color levelColor = ValidationDashboard.GetThresholdColor(s.PzrLevel, 20f, 80f, 17f, 92f);
            d.DrawGaugeArc(levelCenter, 38f, s.PzrLevel, 0f, 100f, levelColor,
                "LEVEL", $"{s.PzrLevel:F1}", "%");
            y += 110f;

            // Volume readouts
            d.DrawDigitalReadout(new Rect(_pressLevelCol.x + 8f, y, readoutW, 20f),
                "WATER VOL", s.PzrWaterVolume, "ft³", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_pressLevelCol.x + 8f, y, readoutW, 20f),
                "STEAM VOL", s.PzrSteamVolume, "ft³", "F1", ValidationDashboard._cTextSecondary);
            y += 32f;

            // Surge flow (bidirectional bar)
            d.DrawBidirectionalBar(new Rect(_pressLevelCol.x + 8f, y, readoutW, 22f),
                "SURGE FLOW", s.SurgeFlow, -100f, 100f);
            y += 30f;

            // Temperature
            d.DrawDigitalReadout(new Rect(_pressLevelCol.x + 8f, y, readoutW, 20f),
                "T_PZR", s.T_pzr, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_pressLevelCol.x + 8f, y, readoutW, 20f),
                "T_SAT", s.T_sat, "°F", "F1", ValidationDashboard._cTextSecondary);
        }

        // ====================================================================
        // CONTROL COLUMN (Heaters/Spray)
        // ====================================================================

        private void DrawControlColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_controlCol, "HEATER / SPRAY");

            float y = _controlCol.y + 26f;
            float colW = _controlCol.width;
            float readoutW = colW - 16f;

            // Heater section
            d.DrawSubsectionDivider(_controlCol, y, "HEATERS");
            y += 24f;

            // Heater power bar
            float heaterKW = s.PzrHeaterPower * 1000f;
            d.DrawBarGauge(new Rect(_controlCol.x + 8f, y, readoutW, 22f),
                "POWER", heaterKW, 0f, 1800f,
                s.PzrHeatersOn ? ValidationDashboard._cWarningAmber : ValidationDashboard._cTextSecondary);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "kW", heaterKW, "", "F0",
                s.PzrHeatersOn ? ValidationDashboard._cWarningAmber : ValidationDashboard._cTextSecondary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "MODE", 0f, s.HeaterMode ?? "---", "F0", ValidationDashboard._cTextSecondary);
            y += 24f;

            d.DrawLED(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "HEATERS ON", s.PzrHeatersOn, false);
            y += 32f;

            // Spray section
            d.DrawSubsectionDivider(_controlCol, y, "SPRAY");
            y += 24f;

            // Spray valve bar
            d.DrawBarGauge(new Rect(_controlCol.x + 8f, y, readoutW, 22f),
                "VALVE", s.SprayValvePosition * 100f, 0f, 100f,
                s.SprayActive ? ValidationDashboard._cCyanInfo : ValidationDashboard._cTextSecondary);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "FLOW", s.SprayFlow, "gpm", "F1",
                s.SprayActive ? ValidationDashboard._cCyanInfo : ValidationDashboard._cTextSecondary);
            y += 24f;

            d.DrawLED(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "SPRAY ACTIVE", s.SprayActive, false);
            y += 32f;

            // Bubble section
            d.DrawSubsectionDivider(_controlCol, y, "BUBBLE");
            y += 24f;

            d.DrawLED(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                s.BubbleFormed ? "BUBBLE FORMED" : "NO BUBBLE",
                s.BubbleFormed, !s.BubbleFormed && !s.SolidPressurizer);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "PHASE", 0f, s.BubblePhase ?? "---", "F0", ValidationDashboard._cTextSecondary);
            y += 24f;

            d.DrawLED(new Rect(_controlCol.x + 8f, y, readoutW, 20f),
                "SOLID PZR", s.SolidPressurizer, true);
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

            // Draw 4 larger sparklines for PZR-relevant parameters
            if (sm != null && sm.IsInitialized)
            {
                // RCS PRESSURE (index 0)
                Rect spark0 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(0, spark0, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // PZR LEVEL (index 1)
                Rect spark1 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(1, spark1, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // NET CVCS (index 5) - shows surge trend
                Rect spark2 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(5, spark2, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // NET HEAT (index 7) - shows heater contribution
                Rect spark3 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(7, spark3, d._gaugeLabelStyle, d._statusValueStyle);
            }
            else
            {
                GUI.contentColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(_trendsCol.x + 8f, y, sparkW, 20f),
                    "Sparklines initializing...", d._statusLabelStyle);
                GUI.contentColor = Color.white;
            }
        }
    }
}
