// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// SGRHRTab.cs - Steam Generator and RHR System Detail Tab
// ============================================================================
//
// PURPOSE:
//   Expanded view of Steam Generator secondary side and Residual Heat Removal
//   system parameters. Includes SG thermal conditions, steam dump status,
//   RHR cooling capacity, and HZP (Hot Zero Power) progression tracking.
//
// LAYOUT:
//   ┌─────────────────┬─────────────────┬─────────────────────────────────┐
//   │ STEAM GENERATOR │      RHR        │          TRENDS                 │
//   │      (30%)      │     (30%)       │           (40%)                 │
//   │                 │                 │                                 │
//   │   [LARGE ARC]   │   RHR STATUS    │  ┌─────────────────────────┐   │
//   │   SG PRESSURE   │   ACTIVE ●      │  │     SG PRESSURE         │   │
//   │                 │   MODE ───      │  └─────────────────────────┘   │
//   │   T_SAT  ───    │                 │  ┌─────────────────────────┐   │
//   │   T_BULK ───    │   [ARC]         │  │     RCS PRESSURE        │   │
//   │                 │   RHR HEAT      │  └─────────────────────────┘   │
//   │   SG HEAT ───   │                 │  ┌─────────────────────────┐   │
//   │   BOILING ●     │   COOLING ───   │  │     T_AVG               │   │
//   │   DUMP ●        │   HEATING ───   │  └─────────────────────────┘   │
//   │                 │                 │  ┌─────────────────────────┐   │
//   │                 │   HZP PROGRESS  │  │     NET HEAT            │   │
//   │                 │   ═══════════   │  └─────────────────────────┘   │
//   │                 │   HZP READY ●   │                               │
//   └─────────────────┴─────────────────┴─────────────────────────────────┘
//
// REFERENCE:
//   NRC HRTD Section 5 — Steam Generators
//   NRC HRTD Section 10 — Residual Heat Removal System
//   Westinghouse HZP entry criteria
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// SG/RHR detail tab with expanded secondary system and heat removal monitoring.
    /// </summary>
    public class SGRHRTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public SGRHRTab(ValidationDashboard dashboard) 
            : base(dashboard, "SG/RHR", 4)
        {
        }

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private Rect _sgCol;
        private Rect _rhrCol;
        private Rect _trendsCol;

        private const float SG_FRAC = 0.30f;
        private const float RHR_FRAC = 0.30f;
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
            float sgW = availW * SG_FRAC;
            float rhrW = availW * RHR_FRAC;
            float trendsW = availW - sgW - rhrW;

            float x = area.x + PAD;
            float y = area.y + PAD;
            float h = area.height - PAD * 2;

            _sgCol = new Rect(x, y, sgW, h);
            x += sgW + COL_GAP;

            _rhrCol = new Rect(x, y, rhrW, h);
            x += rhrW + COL_GAP;

            _trendsCol = new Rect(x, y, trendsW, h);
        }

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            CalculateLayout(area);

            DrawSGColumn();
            DrawRHRColumn();
            DrawTrendsColumn();
        }

        // ====================================================================
        // STEAM GENERATOR COLUMN
        // ====================================================================

        private void DrawSGColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_sgCol, "STEAM GENERATOR");

            float y = _sgCol.y + 26f;
            float colW = _sgCol.width;
            float centerX = _sgCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // Large SG pressure arc gauge
            float gaugeR = 45f;
            Vector2 sgPressCenter = new Vector2(centerX, y + 55f);
            Color sgPressColor = ValidationDashboard.GetThresholdColor(s.SgSecondaryPressure, 50f, 1000f, 14.7f, 1100f);
            d.DrawGaugeArc(sgPressCenter, gaugeR, s.SgSecondaryPressure, 0f, 1200f, sgPressColor,
                "SG PRESSURE", $"{s.SgSecondaryPressure:F0}", "psia");
            y += 130f;

            // SG temperatures
            d.DrawDigitalReadout(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "T_SAT", s.SgSatTemp, "°F", "F1", ValidationDashboard._cTextSecondary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "T_BULK", s.SgBulkTemp, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 32f;

            // Heat transfer
            d.DrawSubsectionDivider(_sgCol, y, "HEAT");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "SG Q", s.SgHeatTransfer, "MW", "F2", ValidationDashboard._cCyanInfo);
            y += 24f;

            // SG status LEDs
            d.DrawLED(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "SG BOILING", s.SgBoilingActive, true);
            y += 24f;

            d.DrawLED(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "STEAM DUMP", s.SteamDumpActive, false);
            y += 32f;

            // Primary side reference
            d.DrawSubsectionDivider(_sgCol, y, "PRIMARY");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "T_HOT", s.T_hot, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "T_COLD", s.T_cold, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_sgCol.x + 8f, y, readoutW, 20f),
                "CORE ΔT", s.CoreDeltaT, "°F", "F1", ValidationDashboard._cTextSecondary);
        }

        // ====================================================================
        // RHR COLUMN
        // ====================================================================

        private void DrawRHRColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_rhrCol, "RHR SYSTEM");

            float y = _rhrCol.y + 26f;
            float colW = _rhrCol.width;
            float centerX = _rhrCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // RHR status
            d.DrawLED(new Rect(_rhrCol.x + 8f, y, readoutW, 22f),
                s.RhrActive ? "RHR RUNNING" : "RHR STANDBY",
                s.RhrActive, false);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_rhrCol.x + 8f, y, readoutW, 20f),
                "MODE", 0f, s.RhrMode ?? "---", "F0",
                s.RhrActive ? ValidationDashboard._cNormalGreen : ValidationDashboard._cTextSecondary);
            y += 32f;

            // RHR heat arc gauge
            Vector2 rhrCenter = new Vector2(centerX, y + 45f);
            Color rhrColor = s.RhrNetHeat > 0 
                ? ValidationDashboard._cWarningAmber 
                : ValidationDashboard._cCyanInfo;
            float rhrAbs = Mathf.Abs(s.RhrNetHeat);
            d.DrawGaugeArc(rhrCenter, 38f, rhrAbs, 0f, 20f, rhrColor,
                "RHR HEAT", $"{s.RhrNetHeat:F2}", "MW");
            y += 110f;

            // Heat breakdown
            d.DrawDigitalReadout(new Rect(_rhrCol.x + 8f, y, readoutW, 20f),
                "NET Q", s.RhrNetHeat, "MW", "F2",
                s.RhrNetHeat > 0 ? ValidationDashboard._cWarningAmber : ValidationDashboard._cCyanInfo);
            y += 32f;

            // HZP section
            d.DrawSubsectionDivider(_rhrCol, y, "HZP");
            y += 24f;

            // HZP progress bar
            d.DrawBarGauge(new Rect(_rhrCol.x + 8f, y, readoutW, 24f),
                "PROGRESS", s.HzpProgress, 0f, 100f, ValidationDashboard._cNormalGreen);
            y += 30f;

            d.DrawDigitalReadout(new Rect(_rhrCol.x + 8f, y, readoutW, 20f),
                "COMPLETE", s.HzpProgress, "%", "F1", ValidationDashboard._cTextPrimary);
            y += 28f;

            d.DrawLED(new Rect(_rhrCol.x + 8f, y, readoutW, 22f),
                "HZP READY", s.HzpStable, false);
            y += 32f;

            // Plant mode reference
            d.DrawSubsectionDivider(_rhrCol, y, "MODE");
            y += 24f;

            string modeStr = s.PlantMode switch
            {
                5 => "MODE 5 - COLD SD",
                4 => "MODE 4 - HOT SD",
                3 => "MODE 3 - HOT STBY",
                _ => $"MODE {s.PlantMode}"
            };

            d.DrawDigitalReadout(new Rect(_rhrCol.x + 8f, y, readoutW, 20f),
                "PLANT", 0f, modeStr, "F0", ValidationDashboard._cCyanInfo);
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

            // Draw 4 larger sparklines for SG/RHR-relevant parameters
            if (sm != null && sm.IsInitialized)
            {
                // SG PRESSURE (index 6)
                Rect spark0 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(6, spark0, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // RCS PRESSURE (index 0)
                Rect spark1 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(0, spark1, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // T_AVG (index 2)
                Rect spark2 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(2, spark2, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // NET HEAT (index 7)
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
