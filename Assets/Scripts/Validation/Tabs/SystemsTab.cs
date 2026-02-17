// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// SystemsTab.cs - Auxiliary Systems Detail Tab
// ============================================================================
//
// PURPOSE:
//   Displays auxiliary plant systems including Boron Recycle System (BRS),
//   letdown orifice status, mass conservation audit, and system diagnostics.
//   Provides detailed breakdown of water inventory and flow paths.
//
// LAYOUT:
//   ┌─────────────────┬─────────────────┬─────────────────────────────────┐
//   │      BRS        │  MASS BALANCE   │      ORIFICES / DIAG            │
//   │     (30%)       │     (35%)       │           (35%)                 │
//   │                 │                 │                                 │
//   │   HOLDUP TANK   │   RCS MASS      │   LETDOWN ORIFICES              │
//   │   [ARC] LEVEL   │   INITIAL ───   │   ORIFICE 1 ●  45 gpm          │
//   │                 │   CURRENT ───   │   ORIFICE 2 ●  60 gpm          │
//   │   DISTILLATE    │   DELTA   ───   │   ORIFICE 3 ●  75 gpm          │
//   │   [ARC] LEVEL   │                 │                                 │
//   │                 │   VCT MASS      │   DIAGNOSTICS                   │
//   │   EVAPORATOR    │   LEVEL   ───   │   SIM TIME  ───                │
//   │   MODE  ───     │   VOLUME  ───   │   WALL TIME ───                │
//   │   ACTIVE ●      │                 │   PLANT MODE ───               │
//   │                 │   PZR MASS      │   PHASE     ───                │
//   │   GAS STRIPPER  │   LEVEL   ───   │   SPEED     ───                │
//   │   MODE  ───     │   VOLUME  ───   │                                 │
//   │   ACTIVE ●      │                 │   PERFORMANCE                   │
//   │                 │   TOTAL ERROR   │   ONGUI MS  ───                │
//   │                 │   ═══════════   │   MAX MS    ───                │
//   └─────────────────┴─────────────────┴─────────────────────────────────┘
//
// REFERENCE:
//   NRC HRTD Section 7 — Boron Recycle System
//   Westinghouse CVCS letdown orifice specifications
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// Systems tab with BRS, mass balance, and diagnostic information.
    /// </summary>
    public class SystemsTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public SystemsTab(ValidationDashboard dashboard) 
            : base(dashboard, "SYSTEMS", 5)
        {
        }

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private Rect _brsCol;
        private Rect _massCol;
        private Rect _diagCol;

        private const float BRS_FRAC = 0.30f;
        private const float MASS_FRAC = 0.35f;
        // DIAG_FRAC = remainder (0.35f)

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
            float brsW = availW * BRS_FRAC;
            float massW = availW * MASS_FRAC;
            float diagW = availW - brsW - massW;

            float x = area.x + PAD;
            float y = area.y + PAD;
            float h = area.height - PAD * 2;

            _brsCol = new Rect(x, y, brsW, h);
            x += brsW + COL_GAP;

            _massCol = new Rect(x, y, massW, h);
            x += massW + COL_GAP;

            _diagCol = new Rect(x, y, diagW, h);
        }

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            CalculateLayout(area);

            DrawBRSColumn();
            DrawMassBalanceColumn();
            DrawDiagnosticsColumn();
        }

        // ====================================================================
        // BRS COLUMN
        // ====================================================================

        private void DrawBRSColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_brsCol, "BORON RECYCLE SYSTEM");

            float y = _brsCol.y + 26f;
            float colW = _brsCol.width;
            float centerX = _brsCol.x + colW / 2f;
            float readoutW = colW - 16f;

            // Holdup Tank
            d.DrawSubsectionDivider(_brsCol, y, "HOLDUP");
            y += 24f;

            Vector2 holdupCenter = new Vector2(centerX, y + 45f);
            Color holdupColor = ValidationDashboard.GetThresholdColor(s.BrsHoldupLevel, 10f, 90f, 5f, 95f);
            d.DrawGaugeArc(holdupCenter, 38f, s.BrsHoldupLevel, 0f, 100f, holdupColor,
                "HOLDUP TANK", $"{s.BrsHoldupLevel:F1}", "%");
            y += 110f;

            // Distillate Tank
            d.DrawSubsectionDivider(_brsCol, y, "DISTILLATE");
            y += 24f;

            Vector2 distCenter = new Vector2(centerX, y + 45f);
            Color distColor = ValidationDashboard.GetThresholdColor(s.BrsDistillateLevel, 10f, 90f, 5f, 95f);
            d.DrawGaugeArc(distCenter, 38f, s.BrsDistillateLevel, 0f, 100f, distColor,
                "DISTILLATE TANK", $"{s.BrsDistillateLevel:F1}", "%");
            y += 110f;

            // BRS Status (placeholder - expand when BRS model is enhanced)
            d.DrawSubsectionDivider(_brsCol, y, "STATUS");
            y += 24f;

            d.DrawLED(new Rect(_brsCol.x + 8f, y, readoutW, 20f),
                "BRS AVAILABLE", true, false);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_brsCol.x + 8f, y, readoutW, 20f),
                "BORON PPM", 0f, "---", "F0", ValidationDashboard._cTextSecondary);
        }

        // ====================================================================
        // MASS BALANCE COLUMN
        // ====================================================================

        private void DrawMassBalanceColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_massCol, "MASS CONSERVATION");

            float y = _massCol.y + 26f;
            float colW = _massCol.width;
            float readoutW = colW - 16f;

            // RCS Mass section
            d.DrawSubsectionDivider(_massCol, y, "RCS");
            y += 24f;

            // Note: These are placeholders - actual mass tracking would need engine support
            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "RCS PRESSURE", s.Pressure, "psia", "F0", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "T_AVG", s.T_avg, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 32f;

            // VCT section
            d.DrawSubsectionDivider(_massCol, y, "VCT");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "LEVEL", s.VctLevel, "%", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            // Estimate VCT volume (using typical VCT size ~400 ft³)
            float vctVolume = s.VctLevel * 4f; // Rough estimate
            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "VOLUME", vctVolume, "ft³", "F0", ValidationDashboard._cTextSecondary);
            y += 32f;

            // PZR section
            d.DrawSubsectionDivider(_massCol, y, "PZR");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "LEVEL", s.PzrLevel, "%", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "WATER", s.PzrWaterVolume, "ft³", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "STEAM", s.PzrSteamVolume, "ft³", "F1", ValidationDashboard._cTextSecondary);
            y += 32f;

            // Mass Error section
            d.DrawSubsectionDivider(_massCol, y, "ERROR");
            y += 24f;

            Color massColor = Mathf.Abs(s.MassError) > 100f ? ValidationDashboard._cAlarmRed :
                             (Mathf.Abs(s.MassError) > 50f ? ValidationDashboard._cWarningAmber : 
                              ValidationDashboard._cNormalGreen);

            d.DrawBarGauge(new Rect(_massCol.x + 8f, y, readoutW, 22f),
                "ERROR", Mathf.Abs(s.MassError), 0f, 200f, massColor);
            y += 28f;

            d.DrawDigitalReadout(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                "MASS ERR", s.MassError, "lbm", "F0", massColor);
            y += 24f;

            bool massOk = Mathf.Abs(s.MassError) < 50f;
            d.DrawLED(new Rect(_massCol.x + 8f, y, readoutW, 20f),
                massOk ? "MASS BALANCE OK" : "MASS IMBALANCE",
                massOk, !massOk);
        }

        // ====================================================================
        // DIAGNOSTICS COLUMN
        // ====================================================================

        private void DrawDiagnosticsColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_diagCol, "DIAGNOSTICS");

            float y = _diagCol.y + 26f;
            float colW = _diagCol.width;
            float readoutW = colW - 16f;

            // CVCS Flow section
            d.DrawSubsectionDivider(_diagCol, y, "CVCS FLOWS");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "CHARGING", s.ChargingFlow, "gpm", "F1", ValidationDashboard._cNormalGreen);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "LETDOWN", s.LetdownFlow, "gpm", "F1", ValidationDashboard._cCyanInfo);
            y += 24f;

            float netFlow = s.ChargingFlow - s.LetdownFlow;
            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "NET FLOW", netFlow, "gpm", "F1",
                netFlow > 0 ? ValidationDashboard._cNormalGreen : ValidationDashboard._cCyanInfo);
            y += 32f;

            // Simulation Info section
            d.DrawSubsectionDivider(_diagCol, y, "SIMULATION");
            y += 24f;

            // Format sim time as HH:MM:SS
            int simHours = (int)s.SimTime;
            int simMins = (int)((s.SimTime - simHours) * 60f);
            int simSecs = (int)(((s.SimTime - simHours) * 60f - simMins) * 60f);
            string simTimeStr = $"{simHours:D2}:{simMins:D2}:{simSecs:D2}";

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "SIM TIME", 0f, simTimeStr, "F0", ValidationDashboard._cTextPrimary);
            y += 24f;

            // Format wall time
            int wallHours = (int)s.WallClockTime;
            int wallMins = (int)((s.WallClockTime - wallHours) * 60f);
            int wallSecs = (int)(((s.WallClockTime - wallHours) * 60f - wallMins) * 60f);
            string wallTimeStr = $"{wallHours:D2}:{wallMins:D2}:{wallSecs:D2}";

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "WALL TIME", 0f, wallTimeStr, "F0", ValidationDashboard._cTextSecondary);
            y += 24f;

            string modeStr = s.PlantMode switch
            {
                5 => "MODE 5",
                4 => "MODE 4",
                3 => "MODE 3",
                _ => $"MODE {s.PlantMode}"
            };
            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "PLANT", 0f, modeStr, "F0", ValidationDashboard._cCyanInfo);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "PHASE", 0f, s.HeatupPhaseDesc ?? "---", "F0", ValidationDashboard._cTextSecondary);
            y += 32f;

            // Performance section
            d.DrawSubsectionDivider(_diagCol, y, "PERFORMANCE");
            y += 24f;

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "ONGUI", d.LastOnGuiTime, "ms", "F2", 
                d.LastOnGuiTime > 2f ? ValidationDashboard._cWarningAmber : ValidationDashboard._cNormalGreen);
            y += 24f;

            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "MAX", d.MaxOnGuiTime, "ms", "F2",
                d.MaxOnGuiTime > 2f ? ValidationDashboard._cWarningAmber : ValidationDashboard._cNormalGreen);
            y += 24f;

            // Speed indicator
            string[] speedLabels = { "1×", "10×", "60×", "300×", "1800×" };
            string speedStr = s.SpeedIndex >= 0 && s.SpeedIndex < speedLabels.Length 
                ? speedLabels[s.SpeedIndex] 
                : "---";
            d.DrawDigitalReadout(new Rect(_diagCol.x + 8f, y, readoutW, 20f),
                "SPEED", 0f, speedStr, "F0", 
                s.IsAccelerated ? ValidationDashboard._cWarningAmber : ValidationDashboard._cTextPrimary);
        }
    }
}
