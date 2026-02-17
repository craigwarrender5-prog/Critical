// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// OverviewTab.cs - Primary Operations Surface
// ============================================================================
//
// PURPOSE:
//   The Overview tab is the primary operations surface, displaying ALL critical
//   parameters on a single 1920×1080 screen with NO scrolling. This provides
//   operators with at-a-glance plant status during heatup operations.
//
// LAYOUT (5-column + footer):
//   ┌────────┬────────┬────────┬────────┬────────────────┐
//   │  RCS   │  PZR   │  CVCS  │ SG/RHR │    TRENDS      │
//   │  18%   │  16%   │  16%   │  16%   │     24%        │
//   │        │        │        │        │                │
//   │ 18     │  16    │  14    │  12    │  8 sparklines  │
//   │ params │ params │ params │ params │                │
//   ├────────┴────────┴────────┴────────┴────────────────┤
//   │                    FOOTER (18%)                     │
//   │  27 annunciator tiles (60%) │ Event log (40%)       │
//   └─────────────────────────────┴───────────────────────┘
//
// REFERENCE:
//   Westinghouse 4-Loop PWR main control board layout
//   NRC HRTD Section 19 — Plant Operations monitoring requirements
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// Overview tab — primary operations surface for at-a-glance plant status.
    /// </summary>
    public class OverviewTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public OverviewTab(ValidationDashboard dashboard) 
            : base(dashboard, "OVERVIEW", 0)
        {
        }

        // ====================================================================
        // MAIN DRAW METHOD
        // ====================================================================

        public override void Draw(Rect area)
        {
            // Calculate layout (uses cached rects when screen size unchanged)
            CalculateLayout(area);

            // Draw 5 columns
            DrawRCSColumn();
            DrawPressurizerColumn();
            DrawCVCSColumn();
            DrawSGRHRColumn();
            DrawTrendsColumn();

            // Draw footer
            DrawFooter();
        }

        // ====================================================================
        // LAYOUT CALCULATIONS
        // ====================================================================

        // Cached rects
        private Rect _rcsCol;
        private Rect _pzrCol;
        private Rect _cvcsCol;
        private Rect _sgRhrCol;
        private Rect _trendsCol;
        private Rect _footer;
        private Rect _annunciatorArea;
        private Rect _eventLogArea;

        // Layout constants
        private const float COL_GAP = 4f;
        private const float SECTION_PAD = 6f;
        private const float FOOTER_FRAC = 0.18f;
        private const float FOOTER_MIN_H = 100f;

        // Column fractions
        private const float RCS_FRAC = 0.18f;
        private const float PZR_FRAC = 0.16f;
        private const float CVCS_FRAC = 0.16f;
        private const float SGRHR_FRAC = 0.16f;
        // TRENDS_FRAC = remainder (~0.24f)

        // Cached screen dimensions for change detection
        private float _cachedW;
        private float _cachedH;

        private void CalculateLayout(Rect area)
        {
            // Skip recalculation if screen unchanged
            if (Mathf.Approximately(_cachedW, ScreenWidth) &&
                Mathf.Approximately(_cachedH, ScreenHeight))
            {
                return;
            }
            _cachedW = ScreenWidth;
            _cachedH = ScreenHeight;

            // Calculate footer height
            float footerH = Mathf.Max(area.height * FOOTER_FRAC, FOOTER_MIN_H);
            float mainH = area.height - footerH - SECTION_PAD;

            // Calculate column widths
            float totalGaps = COL_GAP * 4; // 4 gaps between 5 columns
            float availW = area.width - totalGaps - SECTION_PAD * 2;

            float rcsW = availW * RCS_FRAC;
            float pzrW = availW * PZR_FRAC;
            float cvcsW = availW * CVCS_FRAC;
            float sgRhrW = availW * SGRHR_FRAC;
            float trendsW = availW - rcsW - pzrW - cvcsW - sgRhrW;

            // Build column rects
            float x = area.x + SECTION_PAD;
            float y = area.y + SECTION_PAD;

            _rcsCol = new Rect(x, y, rcsW, mainH);
            x += rcsW + COL_GAP;

            _pzrCol = new Rect(x, y, pzrW, mainH);
            x += pzrW + COL_GAP;

            _cvcsCol = new Rect(x, y, cvcsW, mainH);
            x += cvcsW + COL_GAP;

            _sgRhrCol = new Rect(x, y, sgRhrW, mainH);
            x += sgRhrW + COL_GAP;

            _trendsCol = new Rect(x, y, trendsW, mainH);

            // Footer
            float footerY = area.y + mainH + SECTION_PAD;
            _footer = new Rect(area.x + SECTION_PAD, footerY, 
                area.width - SECTION_PAD * 2, footerH);

            // Footer split: annunciators (60%) | event log (40%)
            float annW = _footer.width * 0.60f;
            _annunciatorArea = new Rect(_footer.x, _footer.y, annW, _footer.height);
            _eventLogArea = new Rect(_footer.x + annW + COL_GAP, _footer.y,
                _footer.width - annW - COL_GAP, _footer.height);
        }

        // ====================================================================
        // RCS COLUMN (18 parameters)
        // ====================================================================

        private void DrawRCSColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            // Column frame and header
            d.DrawColumnFrame(_rcsCol, "RCS PRIMARY");

            float y = _rcsCol.y + 22f; // After header
            float colW = _rcsCol.width;
            float centerX = _rcsCol.x + colW / 2f;

            // T_avg arc gauge (main gauge, larger)
            float gaugeR = 28f;
            Vector2 tavgCenter = new Vector2(centerX, y + 35f);
            Color tavgColor = ValidationDashboard.GetThresholdColor(s.T_avg, 100f, 500f, 50f, 557f);
            d.DrawGaugeArc(tavgCenter, gaugeR, s.T_avg, 50f, 600f, tavgColor, 
                "T_AVG", $"{s.T_avg:F1}", "°F");
            y += 80f;

            // Temperature readouts row
            float readoutW = colW - 8f;
            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f), 
                "T_HOT", s.T_hot, "°F", "F1", 
                ValidationDashboard.GetHighThresholdColor(s.T_hot, 580f, 600f));
            y += 18f;

            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "T_COLD", s.T_cold, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "CORE ΔT", s.CoreDeltaT, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "T_PZR", s.T_pzr, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "T_SAT", s.T_sat, "°F", "F1", ValidationDashboard._cTextSecondary);
            y += 24f;

            // Subcooling arc gauge
            Vector2 subcoolCenter = new Vector2(centerX, y + 30f);
            Color subcoolColor = ValidationDashboard.GetLowThresholdColor(s.Subcooling, 30f, 20f);
            d.DrawGaugeArc(subcoolCenter, 24f, s.Subcooling, 0f, 100f, subcoolColor,
                "SUBCOOL", $"{s.Subcooling:F1}", "°F");
            y += 70f;

            // Rates
            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "HEATUP", s.HeatupRate, "°F/hr", "F1",
                ValidationDashboard.GetThresholdColor(s.HeatupRate, 10f, 45f, 5f, 50f));
            y += 18f;

            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "PRESS RT", s.PressureRate, "psi/hr", "F1", ValidationDashboard._cTextPrimary);
            y += 24f;

            // RCP LEDs (2x2 grid)
            float ledW = (colW - 12f) / 2f;
            d.DrawLED(new Rect(_rcsCol.x + 4f, y, ledW, 18f), "RCP 1", s.RcpRunning[0], false);
            d.DrawLED(new Rect(_rcsCol.x + 4f + ledW + 4f, y, ledW, 18f), "RCP 2", s.RcpRunning[1], false);
            y += 20f;
            d.DrawLED(new Rect(_rcsCol.x + 4f, y, ledW, 18f), "RCP 3", s.RcpRunning[2], false);
            d.DrawLED(new Rect(_rcsCol.x + 4f + ledW + 4f, y, ledW, 18f), "RCP 4", s.RcpRunning[3], false);
            y += 24f;

            // RCP summary
            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "RCP CNT", s.RcpCount, "", "F0", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_rcsCol.x + 4f, y, readoutW, 16f),
                "RCP HEAT", s.RcpHeat, "MW", "F2", ValidationDashboard._cCyanInfo);
        }

        // ====================================================================
        // PRESSURIZER COLUMN (16 parameters)
        // ====================================================================

        private void DrawPressurizerColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_pzrCol, "PRESSURIZER");

            float y = _pzrCol.y + 22f;
            float colW = _pzrCol.width;
            float centerX = _pzrCol.x + colW / 2f;
            float readoutW = colW - 8f;

            // Pressure arc gauge
            Vector2 pressCenter = new Vector2(centerX, y + 35f);
            Color pressColor = ValidationDashboard.GetThresholdColor(s.Pressure, 400f, 2235f, 350f, 2400f);
            d.DrawGaugeArc(pressCenter, 28f, s.Pressure, 0f, 2500f, pressColor,
                "PRESSURE", $"{s.Pressure:F0}", "psia");
            y += 80f;

            // Level arc gauge
            Vector2 levelCenter = new Vector2(centerX, y + 30f);
            Color levelColor = ValidationDashboard.GetThresholdColor(s.PzrLevel, 20f, 80f, 17f, 92f);
            d.DrawGaugeArc(levelCenter, 24f, s.PzrLevel, 0f, 100f, levelColor,
                "LEVEL", $"{s.PzrLevel:F1}", "%");
            y += 70f;

            // Volumes
            d.DrawDigitalReadout(new Rect(_pzrCol.x + 4f, y, readoutW, 16f),
                "WATER", s.PzrWaterVolume, "ft³", "F1", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_pzrCol.x + 4f, y, readoutW, 16f),
                "STEAM", s.PzrSteamVolume, "ft³", "F1", ValidationDashboard._cTextSecondary);
            y += 24f;

            // Heater bar
            d.DrawBarGauge(new Rect(_pzrCol.x + 4f, y, readoutW, 18f),
                "HTR", s.PzrHeaterPower * 1000f, 0f, 1800f, 
                s.PzrHeatersOn ? ValidationDashboard._cWarningAmber : ValidationDashboard._cTextSecondary);
            y += 22f;

            // Heater mode
            d.DrawDigitalReadoutString(new Rect(_pzrCol.x + 4f, y, readoutW, 16f),
                "MODE", s.HeaterMode ?? "---", ValidationDashboard._cTextSecondary);
            y += 22f;

            // Spray bar
            d.DrawBarGauge(new Rect(_pzrCol.x + 4f, y, readoutW, 18f),
                "SPRAY", s.SprayValvePosition * 100f, 0f, 100f,
                s.SprayActive ? ValidationDashboard._cCyanInfo : ValidationDashboard._cTextSecondary);
            y += 22f;

            // Spray flow
            d.DrawDigitalReadout(new Rect(_pzrCol.x + 4f, y, readoutW, 16f),
                "FLOW", s.SprayFlow, "gpm", "F1", 
                s.SprayActive ? ValidationDashboard._cCyanInfo : ValidationDashboard._cTextSecondary);
            y += 22f;

            // Surge flow (bidirectional)
            d.DrawBidirectionalBar(new Rect(_pzrCol.x + 4f, y, readoutW, 18f),
                "SURGE", s.SurgeFlow, -50f, 50f);
            y += 26f;

            // Bubble state
            d.DrawLED(new Rect(_pzrCol.x + 4f, y, colW - 8f, 18f), 
                s.BubbleFormed ? "BUBBLE OK" : (s.SolidPressurizer ? "SOLID PZR" : s.BubblePhase),
                s.BubbleFormed, !s.BubbleFormed && !s.SolidPressurizer);
        }

        // ====================================================================
        // CVCS COLUMN (14 parameters)
        // ====================================================================

        private void DrawCVCSColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_cvcsCol, "CVCS / VCT");

            float y = _cvcsCol.y + 22f;
            float colW = _cvcsCol.width;
            float centerX = _cvcsCol.x + colW / 2f;
            float readoutW = colW - 8f;

            // VCT Level arc gauge
            Vector2 vctCenter = new Vector2(centerX, y + 35f);
            Color vctColor = ValidationDashboard.GetThresholdColor(s.VctLevel, 20f, 80f, 10f, 90f);
            d.DrawGaugeArc(vctCenter, 28f, s.VctLevel, 0f, 100f, vctColor,
                "VCT LEVEL", $"{s.VctLevel:F1}", "%");
            y += 80f;

            // Charging/Letdown bars
            d.DrawBarGauge(new Rect(_cvcsCol.x + 4f, y, readoutW, 18f),
                "CHARG", s.ChargingFlow, 0f, 150f, ValidationDashboard._cNormalGreen);
            y += 22f;

            d.DrawBarGauge(new Rect(_cvcsCol.x + 4f, y, readoutW, 18f),
                "LETDN", s.LetdownFlow, 0f, 150f, ValidationDashboard._cCyanInfo);
            y += 22f;

            // Net CVCS (bidirectional)
            float netCvcs = s.ChargingFlow - s.LetdownFlow;
            d.DrawBidirectionalBar(new Rect(_cvcsCol.x + 4f, y, readoutW, 18f),
                "NET", netCvcs, -50f, 50f);
            y += 26f;

            // Status LEDs
            float ledW = (colW - 12f) / 2f;
            d.DrawLED(new Rect(_cvcsCol.x + 4f, y, ledW, 18f), "CHG", s.ChargingActive, false);
            d.DrawLED(new Rect(_cvcsCol.x + 4f + ledW + 4f, y, ledW, 18f), "LTD", s.LetdownActive, false);
            y += 20f;

            d.DrawLED(new Rect(_cvcsCol.x + 4f, y, ledW, 18f), "MKUP", s.VctMakeupActive, true);
            d.DrawLED(new Rect(_cvcsCol.x + 4f + ledW + 4f, y, ledW, 18f), "DVRT", s.VctDivertActive, true);
            y += 24f;

            // Mass error (alarmed)
            Color massColor = Mathf.Abs(s.MassError) > 100f ? ValidationDashboard._cAlarmRed :
                             (Mathf.Abs(s.MassError) > 50f ? ValidationDashboard._cWarningAmber : 
                              ValidationDashboard._cNormalGreen);
            d.DrawDigitalReadout(new Rect(_cvcsCol.x + 4f, y, readoutW, 16f),
                "MASS ERR", s.MassError, "lbm", "F0", massColor);
            y += 22f;

            // BRS
            d.DrawDigitalReadout(new Rect(_cvcsCol.x + 4f, y, readoutW, 16f),
                "BRS HU", s.BrsHoldupLevel, "%", "F1", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_cvcsCol.x + 4f, y, readoutW, 16f),
                "BRS DIST", s.BrsDistillateLevel, "%", "F1", ValidationDashboard._cTextPrimary);
            y += 22f;

            // Seal injection
            d.DrawLED(new Rect(_cvcsCol.x + 4f, y, colW - 8f, 18f), 
                "SEAL INJ OK", s.SealInjectionOK, false);
        }

        // ====================================================================
        // SG/RHR COLUMN (12 parameters)
        // ====================================================================

        private void DrawSGRHRColumn()
        {
            var d = Dashboard;
            var s = Snapshot;

            d.DrawColumnFrame(_sgRhrCol, "SG / RHR");

            float y = _sgRhrCol.y + 22f;
            float colW = _sgRhrCol.width;
            float centerX = _sgRhrCol.x + colW / 2f;
            float readoutW = colW - 8f;

            // SG Pressure arc gauge
            Vector2 sgPressCenter = new Vector2(centerX, y + 35f);
            Color sgPressColor = ValidationDashboard.GetThresholdColor(s.SgSecondaryPressure, 50f, 1000f, 14.7f, 1100f);
            d.DrawGaugeArc(sgPressCenter, 28f, s.SgSecondaryPressure, 0f, 1200f, sgPressColor,
                "SG PRESS", $"{s.SgSecondaryPressure:F0}", "psia");
            y += 80f;

            // SG temperatures
            d.DrawDigitalReadout(new Rect(_sgRhrCol.x + 4f, y, readoutW, 16f),
                "SG T_SAT", s.SgSatTemp, "°F", "F1", ValidationDashboard._cTextSecondary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_sgRhrCol.x + 4f, y, readoutW, 16f),
                "SG BULK", s.SgBulkTemp, "°F", "F1", ValidationDashboard._cTextPrimary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_sgRhrCol.x + 4f, y, readoutW, 16f),
                "SG Q", s.SgHeatTransfer, "MW", "F2", ValidationDashboard._cCyanInfo);
            y += 24f;

            // Status LEDs
            float ledW = (colW - 12f) / 2f;
            d.DrawLED(new Rect(_sgRhrCol.x + 4f, y, ledW, 18f), "BOIL", s.SgBoilingActive, true);
            d.DrawLED(new Rect(_sgRhrCol.x + 4f + ledW + 4f, y, ledW, 18f), "DUMP", s.SteamDumpActive, false);
            y += 24f;

            // RHR section
            d.DrawSubsectionDivider(_sgRhrCol, y, "RHR");
            y += 16f;

            d.DrawLED(new Rect(_sgRhrCol.x + 4f, y, colW - 8f, 18f), 
                s.RhrActive ? "RHR RUNNING" : "RHR STANDBY", s.RhrActive, false);
            y += 20f;

            d.DrawDigitalReadoutString(new Rect(_sgRhrCol.x + 4f, y, readoutW, 16f),
                "MODE", s.RhrMode ?? "---", 
                s.RhrActive ? ValidationDashboard._cNormalGreen : ValidationDashboard._cTextSecondary);
            y += 18f;

            d.DrawDigitalReadout(new Rect(_sgRhrCol.x + 4f, y, readoutW, 16f),
                "NET Q", s.RhrNetHeat, "MW", "F2", 
                s.RhrNetHeat > 0 ? ValidationDashboard._cWarningAmber : ValidationDashboard._cCyanInfo);
            y += 26f;

            // HZP progress bar
            d.DrawBarGauge(new Rect(_sgRhrCol.x + 4f, y, readoutW, 18f),
                "HZP", s.HzpProgress, 0f, 100f, ValidationDashboard._cNormalGreen);
            y += 22f;

            d.DrawLED(new Rect(_sgRhrCol.x + 4f, y, colW - 8f, 18f),
                "HZP READY", s.HzpStable, false);
        }

        // ====================================================================
        // TRENDS COLUMN (8 sparklines)
        // ====================================================================

        // Sparkline initialization flag
        private bool _sparklinesInitialized = false;

        private void DrawTrendsColumn()
        {
            var d = Dashboard;
            var sm = d.SparklineManager;

            d.DrawColumnFrame(_trendsCol, "TRENDS");

            // Initialize sparklines on first draw (need column width)
            if (!_sparklinesInitialized && sm != null)
            {
                int sparkW = (int)(_trendsCol.width - 8f);
                int sparkH = (int)((_trendsCol.height - 22f - 8f) / 8f) - 2;
                sparkH = Mathf.Max(sparkH, 24); // Minimum height
                sm.Initialize(sparkW, sparkH);
                _sparklinesInitialized = true;
            }

            // Calculate sparkline layout
            float y = _trendsCol.y + 22f;
            float trendH = (_trendsCol.height - 22f - 8f) / 8f;
            float trendW = _trendsCol.width - 8f;

            // Draw all 8 sparklines
            if (sm != null && sm.IsInitialized)
            {
                for (int i = 0; i < SparklineManager.SPARKLINE_COUNT; i++)
                {
                    Rect sparkRect = new Rect(_trendsCol.x + 4f, y, trendW, trendH - 2f);
                    sm.Draw(i, sparkRect, d._gaugeLabelStyle, d._statusValueStyle);
                    y += trendH;
                }
            }
            else
            {
                // Fallback placeholder if manager not ready
                string[] labels = { "RCS PRESS", "PZR LEVEL", "T_AVG", "HEATUP", 
                                   "SUBCOOL", "NET CVCS", "SG PRESS", "NET HEAT" };

                for (int i = 0; i < 8; i++)
                {
                    Rect sparkRect = new Rect(_trendsCol.x + 4f, y, trendW, trendH - 2f);
                    GUI.Box(sparkRect, GUIContent.none, d._gaugeBgStyle);
                    GUI.Label(new Rect(sparkRect.x + 2f, sparkRect.y + 2f, 60f, 12f),
                        labels[i], d._gaugeLabelStyle);
                    y += trendH;
                }
            }
        }

        // ====================================================================
        // FOOTER (Annunciators + Event Log)
        // ====================================================================

        // Cached tile style to avoid allocation in OnGUI
        private GUIStyle _cachedTileStyle;

        private void DrawFooter()
        {
            var d = Dashboard;
            var am = d.AnnunciatorManager;

            // Draw annunciator panel
            DrawAnnunciatorPanel(d, am);

            // Draw event log panel
            DrawEventLogPanel(d, am);
        }

        private void DrawAnnunciatorPanel(ValidationDashboard d, AnnunciatorManager am)
        {
            // Header with alarm count
            int alertCount = am?.AlertingCount ?? 0;
            int alarmCount = am?.AlarmCount ?? 0;
            string headerText = alertCount > 0 || alarmCount > 0
                ? $"ANNUNCIATORS ({alertCount + alarmCount} ACTIVE)"
                : "ANNUNCIATORS";
            d.DrawColumnFrame(_annunciatorArea, headerText);

            // Calculate tile dimensions
            float availW = _annunciatorArea.width - 8f;
            float availH = _annunciatorArea.height - 22f - 30f; // Leave room for buttons
            float tileW = (availW - (AnnunciatorManager.TILES_PER_ROW - 1) * 3f) / AnnunciatorManager.TILES_PER_ROW;
            float tileH = (availH - (AnnunciatorManager.ROW_COUNT - 1) * 3f) / AnnunciatorManager.ROW_COUNT;
            tileW = Mathf.Min(tileW, 95f);
            tileH = Mathf.Min(tileH, 28f);
            float gap = 3f;

            float startY = _annunciatorArea.y + 22f;

            // Draw tiles
            if (am != null && am.IsInitialized)
            {
                for (int row = 0; row < AnnunciatorManager.ROW_COUNT; row++)
                {
                    float x = _annunciatorArea.x + 4f;
                    for (int col = 0; col < AnnunciatorManager.TILES_PER_ROW; col++)
                    {
                        int idx = row * AnnunciatorManager.TILES_PER_ROW + col;
                        if (idx >= AnnunciatorManager.TILE_COUNT) break;

                        var tile = am.GetTile(idx);
                        if (tile == null) continue;

                        Rect tileRect = new Rect(x, startY + row * (tileH + gap), tileW, tileH);

                        // Handle click-to-acknowledge
                        if (Event.current.type == EventType.MouseDown && 
                            tileRect.Contains(Event.current.mousePosition))
                        {
                            if (am.AcknowledgeTile(idx))
                            {
                                Event.current.Use();
                            }
                        }

                        // Draw tile with live state
                        DrawAnnunciatorTileWithState(d, tileRect, tile);

                        x += tileW + gap;
                    }
                }
            }

            // Draw ACK and RESET buttons
            float btnY = _annunciatorArea.y + _annunciatorArea.height - 26f;
            float btnW = 70f;
            float btnH = 22f;
            float btnGap = 8f;

            Rect ackRect = new Rect(_annunciatorArea.x + _annunciatorArea.width - btnW * 2 - btnGap - 8f, 
                btnY, btnW, btnH);
            Rect resetRect = new Rect(_annunciatorArea.x + _annunciatorArea.width - btnW - 4f, 
                btnY, btnW, btnH);

            // ACK button
            bool hasAlerting = am != null && am.AlertingCount > 0;
            GUI.enabled = hasAlerting;
            if (GUI.Button(ackRect, "ACK ALL", d._buttonStyle))
            {
                am?.AcknowledgeAll();
            }
            GUI.enabled = true;

            // RESET button
            bool hasAcked = am != null && am.AcknowledgedCount > 0;
            GUI.enabled = hasAcked;
            if (GUI.Button(resetRect, "RESET", d._buttonStyle))
            {
                am?.ResetAll();
            }
            GUI.enabled = true;
        }

        private void DrawAnnunciatorTileWithState(ValidationDashboard d, Rect tileRect, AnnunciatorTile tile)
        {
            // Get colors based on state
            Color bgColor;
            Color textColor;

            switch (tile.State)
            {
                case AnnunciatorState.Normal:
                    bgColor = ValidationDashboard._cAnnNormal;
                    textColor = ValidationDashboard._cTextPrimary;
                    break;
                case AnnunciatorState.Alerting:
                    bool flash = (Time.time % 0.5f) < 0.25f;
                    bgColor = flash ? ValidationDashboard._cAnnAlerting : ValidationDashboard._cAnnOff;
                    textColor = flash ? ValidationDashboard._cTextBright : ValidationDashboard._cTextSecondary;
                    break;
                case AnnunciatorState.Acknowledged:
                    bgColor = ValidationDashboard._cAnnAcked;
                    textColor = ValidationDashboard._cTextPrimary;
                    break;
                case AnnunciatorState.Alarm:
                    bool alarmFlash = (Time.time % 0.3f) < 0.15f;
                    bgColor = alarmFlash ? ValidationDashboard._cAnnAlarm : ValidationDashboard._cAnnOff;
                    textColor = ValidationDashboard._cTextBright;
                    break;
                default:
                    bgColor = tile.ConditionActive 
                        ? ValidationDashboard._cAnnNormal 
                        : ValidationDashboard._cAnnOff;
                    textColor = tile.ConditionActive 
                        ? ValidationDashboard._cTextPrimary 
                        : ValidationDashboard._cTextSecondary;
                    break;
            }

            // Draw background
            GUI.DrawTexture(tileRect, d.GetColorTex(bgColor));

            // Draw border
            ValidationDashboard.DrawLine(new Vector2(tileRect.x, tileRect.y),
                new Vector2(tileRect.x + tileRect.width, tileRect.y), 
                ValidationDashboard._cGaugeTick, 1f);
            ValidationDashboard.DrawLine(new Vector2(tileRect.x + tileRect.width, tileRect.y),
                new Vector2(tileRect.x + tileRect.width, tileRect.y + tileRect.height), 
                ValidationDashboard._cGaugeTick, 1f);
            ValidationDashboard.DrawLine(new Vector2(tileRect.x + tileRect.width, tileRect.y + tileRect.height),
                new Vector2(tileRect.x, tileRect.y + tileRect.height), 
                ValidationDashboard._cGaugeTick, 1f);
            ValidationDashboard.DrawLine(new Vector2(tileRect.x, tileRect.y + tileRect.height),
                new Vector2(tileRect.x, tileRect.y), 
                ValidationDashboard._cGaugeTick, 1f);

            // Cache tile style
            if (_cachedTileStyle == null)
            {
                _cachedTileStyle = new GUIStyle(d._gaugeLabelStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 8
                };
            }
            _cachedTileStyle.normal.textColor = textColor;

            // Draw label
            GUI.Label(new Rect(tileRect.x + 2f, tileRect.y + 2f,
                tileRect.width - 4f, tileRect.height - 4f), tile.Label, _cachedTileStyle);
        }

        private void DrawEventLogPanel(ValidationDashboard d, AnnunciatorManager am)
        {
            d.DrawColumnFrame(_eventLogArea, "EVENT LOG");

            float logY = _eventLogArea.y + 22f;
            float logH = 16f;
            float logW = _eventLogArea.width - 8f;

            if (am != null && am.EventCount > 0)
            {
                var events = am.GetEvents(AnnunciatorManager.VISIBLE_EVENTS);

                foreach (var evt in events)
                {
                    // Format time
                    int hours = (int)evt.SimTime;
                    int minutes = (int)((evt.SimTime - hours) * 60f);
                    int seconds = (int)((evt.SimTime * 60f - hours * 60f - minutes) * 60f);
                    string timeStr = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

                    // Get severity color
                    Color sevColor;
                    switch (evt.Severity)
                    {
                        case AlarmSeverity.Alarm:
                            sevColor = ValidationDashboard._cAlarmRed;
                            break;
                        case AlarmSeverity.Warning:
                            sevColor = ValidationDashboard._cWarningAmber;
                            break;
                        default:
                            sevColor = ValidationDashboard._cCyanInfo;
                            break;
                    }

                    // Draw time
                    GUI.contentColor = ValidationDashboard._cTextSecondary;
                    GUI.Label(new Rect(_eventLogArea.x + 4f, logY, 60f, logH),
                        timeStr, d._statusLabelStyle);

                    // Draw message
                    GUI.contentColor = sevColor;
                    GUI.Label(new Rect(_eventLogArea.x + 68f, logY, logW - 68f, logH),
                        evt.Message, d._statusLabelStyle);

                    logY += logH;
                    if (logY > _eventLogArea.y + _eventLogArea.height - 8f) break;
                }

                GUI.contentColor = Color.white;
            }
            else
            {
                // No events
                GUI.contentColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(_eventLogArea.x + 4f, logY, logW, logH),
                    "No events recorded", d._statusLabelStyle);
                GUI.contentColor = Color.white;
            }
        }

    }
}
