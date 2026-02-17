// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// CVCSTab.cs - Chemical and Volume Control System Detail Tab
// ============================================================================
//
// PURPOSE:
//   Expanded view of CVCS parameters including charging, letdown, VCT level,
//   mass conservation, seal injection, and BRS (Boron Recycle System) status.
//
// LAYOUT:
//   ┌─────────────────┬─────────────────┬─────────────────────────────────┐
//   │   FLOW CONTROL  │   VCT / MASS    │          TRENDS                 │
//   │      (30%)      │     (30%)       │           (40%)                 │
//   │                 │                 │                                 │
//   │  CHARGING       │   [LARGE ARC]   │  ┌─────────────────────────┐   │
//   │  ═══════════    │    VCT LEVEL    │  │     NET CVCS FLOW       │   │
//   │  FLOW  ───      │                 │  └─────────────────────────┘   │
//   │  ACTIVE ●       │   MAKEUP ●      │  ┌─────────────────────────┐   │
//   │                 │   DIVERT ●      │  │     VCT LEVEL           │   │
//   │  LETDOWN        │                 │  └─────────────────────────┘   │
//   │  ═══════════    │   MASS CONSRV   │  ┌─────────────────────────┐   │
//   │  FLOW  ───      │   ERROR ───     │  │     PZR LEVEL           │   │
//   │  ACTIVE ●       │   STATUS ●      │  └─────────────────────────┘   │
//   │                 │                 │  ┌─────────────────────────┐   │
//   │  NET ══════     │   BRS HOLDUP    │  │     RCS PRESSURE        │   │
//   │                 │   BRS DISTILL   │  └─────────────────────────┘   │
//   │  SEAL INJ ●     │                 │                               │
//   └─────────────────┴─────────────────┴─────────────────────────────────┘
//
// REFERENCE:
//   NRC HRTD Section 6 — Chemical and Volume Control System
//   Westinghouse CVCS flow balance requirements
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// CVCS detail tab with expanded flow control and inventory monitoring.
    /// </summary>
    public class CVCSTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public CVCSTab(ValidationDashboard dashboard) 
            : base(dashboard, "CVCS", 3)
        {
        }

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private Rect _flowCol;
        private Rect _vctMassCol;
        private Rect _trendsCol;

        private const float FLOW_FRAC = 0.30f;
        private const float VCT_FRAC = 0.30f;
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
            float flowW = availW * FLOW_FRAC;
            float vctW = availW * VCT_FRAC;
            float trendsW = availW - flowW - vctW;

            float x = area.x + PAD;
            float y = area.y + PAD;
            float h = area.height - PAD * 2;

            _flowCol = new Rect(x, y, flowW, h);
            x += flowW + COL_GAP;

            _vctMassCol = new Rect(x, y, vctW, h);
            x += vctW + COL_GAP;

            _trendsCol = new Rect(x, y, trendsW, h);
        }

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            CalculateLayout(area);

            DrawFlowColumn();
            DrawVctMassColumn();
            DrawTrendsColumn();
        }

        // ====================================================================
        // FLOW CONTROL COLUMN
        // ====================================================================

        private void DrawFlowColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_flowCol, "FLOW CONTROL");

            float y = _flowCol.y + 26f;
            float colW = _flowCol.width;
            float readoutW = colW - 16f;

            // Charging section
            d.DrawSubsectionDivider(_flowCol, y, "CHARGING");
            y += 24f;

            d.DrawBarGauge(new Rect(_flowCol.x + 8f, y, readoutW, 22f),
                "FLOW", s.ChargingFlow, 0f, 150f, ValidationDashboard._cNormalGreen);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_flowCol.x + 8f, y, readoutW, 20f),
                "GPM", s.ChargingFlow, "", "F1", ValidationDashboard._cNormalGreen);
            y += 24f;

            d.DrawLED(new Rect(_flowCol.x + 8f, y, readoutW, 20f),
                "CHARGING ACTIVE", s.ChargingActive, false);
            y += 36f;

            // Letdown section
            d.DrawSubsectionDivider(_flowCol, y, "LETDOWN");
            y += 24f;

            d.DrawBarGauge(new Rect(_flowCol.x + 8f, y, readoutW, 22f),
                "FLOW", s.LetdownFlow, 0f, 150f, ValidationDashboard._cCyanInfo);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_flowCol.x + 8f, y, readoutW, 20f),
                "GPM", s.LetdownFlow, "", "F1", ValidationDashboard._cCyanInfo);
            y += 24f;

            d.DrawLED(new Rect(_flowCol.x + 8f, y, readoutW, 20f),
                "LETDOWN ACTIVE", s.LetdownActive, false);
            y += 36f;

            // Net flow section
            d.DrawSubsectionDivider(_flowCol, y, "NET FLOW");
            y += 24f;

            float netCvcs = s.ChargingFlow - s.LetdownFlow;
            d.DrawBidirectionalBar(new Rect(_flowCol.x + 8f, y, readoutW, 24f),
                "NET", netCvcs, -75f, 75f);
            y += 32f;

            d.DrawDigitalReadout(new Rect(_flowCol.x + 8f, y, readoutW, 20f),
                "NET GPM", netCvcs, "", "F1",
                netCvcs > 0 ? ValidationDashboard._cNormalGreen : 
                (netCvcs < 0 ? ValidationDashboard._cCyanInfo : ValidationDashboard._cTextPrimary));
            y += 32f;

            // Seal injection
            d.DrawLED(new Rect(_flowCol.x + 8f, y, readoutW, 20f),
                "SEAL INJECTION OK", s.SealInjectionOK, false);
        }

        // ====================================================================
        // VCT / MASS COLUMN
        // ====================================================================

        private void DrawVctMassColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_vctMassCol, "VCT / MASS");

            float y = _vctMassCol.y + 26f;
            float colW = _vctMassCol.width;
            float centerX = _vctMassCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // Large VCT level arc gauge
            float gaugeR = 45f;
            Vector2 vctCenter = new Vector2(centerX, y + 55f);
            Color vctColor = ValidationDashboard.GetThresholdColor(s.VctLevel, 20f, 80f, 10f, 90f);
            d.DrawGaugeArc(vctCenter, gaugeR, s.VctLevel, 0f, 100f, vctColor,
                "VCT LEVEL", $"{s.VctLevel:F1}", "%");
            y += 130f;

            // VCT control LEDs
            float ledW = (colW - 24f) / 2f;
            d.DrawLED(new Rect(_vctMassCol.x + 8f, y, ledW, 20f), "MAKEUP", s.VctMakeupActive, true);
            d.DrawLED(new Rect(_vctMassCol.x + 8f + ledW + 8f, y, ledW, 20f), "DIVERT", s.VctDivertActive, true);
            y += 32f;

            // Mass conservation section
            d.DrawSubsectionDivider(_vctMassCol, y, "MASS");
            y += 24f;

            Color massColor = Mathf.Abs(s.MassError) > 100f ? ValidationDashboard._cAlarmRed :
                             (Mathf.Abs(s.MassError) > 50f ? ValidationDashboard._cWarningAmber : 
                              ValidationDashboard._cNormalGreen);

            d.DrawDigitalReadout(new Rect(_vctMassCol.x + 8f, y, readoutW, 20f),
                "MASS ERROR", s.MassError, "lbm", "F0", massColor);
            y += 24f;

            bool massOk = Mathf.Abs(s.MassError) < 50f;
            d.DrawLED(new Rect(_vctMassCol.x + 8f, y, readoutW, 20f),
                massOk ? "MASS BALANCE OK" : "MASS IMBALANCE",
                massOk, !massOk);
            y += 36f;

            // BRS section
            d.DrawSubsectionDivider(_vctMassCol, y, "BRS");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_vctMassCol.x + 8f, y, readoutW, 20f),
                "HOLDUP", s.BrsHoldupLevel, "%", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_vctMassCol.x + 8f, y, readoutW, 20f),
                "DISTILLATE", s.BrsDistillateLevel, "%", "F1", ValidationDashboard._cTextPrimary);
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

            // Draw 4 larger sparklines for CVCS-relevant parameters
            if (sm != null && sm.IsInitialized)
            {
                // NET CVCS (index 5)
                Rect spark0 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(5, spark0, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // PZR LEVEL (index 1) - shows inventory effect
                Rect spark1 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(1, spark1, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // RCS PRESSURE (index 0)
                Rect spark2 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(0, spark2, d._gaugeLabelStyle, d._statusValueStyle);
                y += sparkH;

                // T_AVG (index 2)
                Rect spark3 = new Rect(_trendsCol.x + 8f, y, sparkW, sparkH - 4f);
                sm.Draw(2, spark3, d._gaugeLabelStyle, d._statusValueStyle);
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
