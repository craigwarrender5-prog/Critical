// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.TrendsTab.cs — Full-Screen Trends Tab (Tab 6)
// ============================================================================
//
// PURPOSE:
//   Builds the TRENDS Tab (Tab 6) — Maximum data visualization with 4 large
//   strip charts stacked vertically, each showing related parameter groups:
//     - Chart 1: TEMPERATURES (T_avg, T_hot, T_cold, T_pzr, T_sat, T_sg)
//     - Chart 2: PRESSURES (RCS Pressure, SG Pressure, Pressure Rate)
//     - Chart 3: LEVELS & FLOWS (PZR Level, VCT Level, Charging, Letdown, Surge)
//     - Chart 4: THERMAL BALANCE (RCP Heat, SG Heat, RHR Net, Steam Dump, Net Heat)
//
//   Each chart uses full width with ~200px height and synced time axis.
//   Color-coded trace labels are shown inline via panel titles.
//
// DATA BINDING:
//   RefreshTrendsTab() at 5Hz when tab 6 is active.
//   All 4 charts receive new data points at each refresh cycle.
//
// IP: IP-0060 Stage 8B
// VERSION: 6.0.0
// DATE: 2026-02-18
// ============================================================================

using Critical.Physics;
using Critical.UI.POC;
using Critical.Validation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    public partial class UITKDashboardV2Controller
    {
        // ====================================================================
        // TRENDS TAB — REFERENCES
        // ====================================================================

        // Chart 1: Temperatures
        private StripChartPOC _trend_chartTemp;
        private int _trend_traceTAvg = -1;
        private int _trend_traceTHot = -1;
        private int _trend_traceTCold = -1;
        private int _trend_traceTPzr = -1;
        private int _trend_traceTSat = -1;
        private int _trend_traceTSg = -1;

        // Chart 2: Pressures
        private StripChartPOC _trend_chartPress;
        private int _trend_traceRCSPress = -1;
        private int _trend_traceSGPress = -1;
        private int _trend_tracePressRate = -1;

        // Chart 3: Levels & Flows
        private StripChartPOC _trend_chartLevels;
        private int _trend_tracePzrLevel = -1;
        private int _trend_traceVCTLevel = -1;
        private int _trend_traceCharging = -1;
        private int _trend_traceLetdown = -1;
        private int _trend_traceSurge = -1;

        // Chart 4: Thermal Balance
        private StripChartPOC _trend_chartThermal;
        private int _trend_traceRCPHeat = -1;
        private int _trend_traceSGHeat = -1;
        private int _trend_traceRHRNet = -1;
        private int _trend_traceSteamDump = -1;
        private int _trend_traceNetHeat = -1;

        // ====================================================================
        // BUILD
        // ====================================================================

        private VisualElement BuildTrendsTab()
        {
            var root = MakeTabRoot("v2-tab-trends");

            // All 4 charts stacked vertically, each taking equal space
            root.Add(BuildTrendChartTemperatures());
            root.Add(BuildTrendChartPressures());
            root.Add(BuildTrendChartLevelsFlows());
            root.Add(BuildTrendChartThermalBalance());

            return root;
        }

        // ====================================================================
        // CHART 1 — TEMPERATURES
        // ====================================================================

        private VisualElement BuildTrendChartTemperatures()
        {
            var panel = MakePanel("TEMPERATURES — T_avg (green) | T_hot (red) | T_cold (cyan) | T_pzr (amber) | T_sat (magenta) | T_sg (yellow)");
            panel.style.flexGrow = 1f;
            panel.style.minHeight = 0f;

            _trend_chartTemp = new StripChartPOC();
            _trend_chartTemp.style.flexGrow = 1f;
            _trend_chartTemp.style.minHeight = 120f;

            _trend_traceTAvg  = _trend_chartTemp.AddTrace("T_avg",
                new Color(0.18f, 0.85f, 0.25f, 1f), 50f, 650f);
            _trend_traceTHot  = _trend_chartTemp.AddTrace("T_hot",
                new Color(1f, 0.4f, 0.4f, 1f), 50f, 650f);
            _trend_traceTCold = _trend_chartTemp.AddTrace("T_cold",
                new Color(0.4f, 0.8f, 1f, 1f), 50f, 650f);
            _trend_traceTPzr  = _trend_chartTemp.AddTrace("T_pzr",
                new Color(1f, 0.667f, 0f, 1f), 50f, 700f);
            _trend_traceTSat  = _trend_chartTemp.AddTrace("T_sat",
                new Color(0.85f, 0.35f, 0.85f, 1f), 50f, 700f);
            _trend_traceTSg   = _trend_chartTemp.AddTrace("T_sg",
                new Color(1f, 1f, 0.4f, 1f), 50f, 650f);

            panel.Add(_trend_chartTemp);
            return panel;
        }

        // ====================================================================
        // CHART 2 — PRESSURES
        // ====================================================================

        private VisualElement BuildTrendChartPressures()
        {
            var panel = MakePanel("PRESSURES — RCS (green) | SG (cyan) | dP/dt scaled ×10 (amber)");
            panel.style.flexGrow = 1f;
            panel.style.minHeight = 0f;

            _trend_chartPress = new StripChartPOC();
            _trend_chartPress.style.flexGrow = 1f;
            _trend_chartPress.style.minHeight = 120f;

            _trend_traceRCSPress  = _trend_chartPress.AddTrace("RCS P",
                new Color(0.18f, 0.85f, 0.25f, 1f), 0f, 2600f);
            _trend_traceSGPress   = _trend_chartPress.AddTrace("SG P",
                new Color(0.4f, 0.8f, 1f, 1f), 0f, 1400f);
            // Pressure rate scaled to fit: value × 10 mapped to 0–2600 range
            _trend_tracePressRate = _trend_chartPress.AddTrace("dP/dt×10",
                new Color(1f, 0.667f, 0f, 1f), 0f, 2600f);

            panel.Add(_trend_chartPress);
            return panel;
        }

        // ====================================================================
        // CHART 3 — LEVELS & FLOWS
        // ====================================================================

        private VisualElement BuildTrendChartLevelsFlows()
        {
            var panel = MakePanel("LEVELS & FLOWS — PZR % (green) | VCT % (cyan) | Charging (amber) | Letdown (red) | Surge (magenta)");
            panel.style.flexGrow = 1f;
            panel.style.minHeight = 0f;

            _trend_chartLevels = new StripChartPOC();
            _trend_chartLevels.style.flexGrow = 1f;
            _trend_chartLevels.style.minHeight = 120f;

            _trend_tracePzrLevel = _trend_chartLevels.AddTrace("PZR %",
                new Color(0.18f, 0.85f, 0.25f, 1f), 0f, 100f);
            _trend_traceVCTLevel = _trend_chartLevels.AddTrace("VCT %",
                new Color(0.4f, 0.8f, 1f, 1f), 0f, 100f);
            // Flows scaled: divide by 2 to share 0–100 range (max ~200 gpm → 100)
            _trend_traceCharging = _trend_chartLevels.AddTrace("CHG/2",
                new Color(1f, 0.667f, 0f, 1f), 0f, 100f);
            _trend_traceLetdown  = _trend_chartLevels.AddTrace("LTD/2",
                new Color(1f, 0.4f, 0.4f, 1f), 0f, 100f);
            _trend_traceSurge    = _trend_chartLevels.AddTrace("SURGE/2",
                new Color(0.85f, 0.35f, 0.85f, 1f), 0f, 100f);

            panel.Add(_trend_chartLevels);
            return panel;
        }

        // ====================================================================
        // CHART 4 — THERMAL BALANCE
        // ====================================================================

        private VisualElement BuildTrendChartThermalBalance()
        {
            var panel = MakePanel("THERMAL BALANCE (MW) — RCP (green) | SG (cyan) | RHR (amber) | Steam Dump (red) | Net (white)");
            panel.style.flexGrow = 1f;
            panel.style.minHeight = 0f;

            _trend_chartThermal = new StripChartPOC();
            _trend_chartThermal.style.flexGrow = 1f;
            _trend_chartThermal.style.minHeight = 120f;

            _trend_traceRCPHeat   = _trend_chartThermal.AddTrace("RCP",
                new Color(0.18f, 0.85f, 0.25f, 1f), 0f, 35f);
            _trend_traceSGHeat    = _trend_chartThermal.AddTrace("SG",
                new Color(0.4f, 0.8f, 1f, 1f), 0f, 35f);
            _trend_traceRHRNet    = _trend_chartThermal.AddTrace("RHR",
                new Color(1f, 0.667f, 0f, 1f), 0f, 35f);
            _trend_traceSteamDump = _trend_chartThermal.AddTrace("DUMP",
                new Color(1f, 0.4f, 0.4f, 1f), 0f, 35f);
            _trend_traceNetHeat   = _trend_chartThermal.AddTrace("NET",
                new Color(0.9f, 0.9f, 0.9f, 1f), -10f, 35f);

            panel.Add(_trend_chartThermal);
            return panel;
        }

        // ====================================================================
        // DATA REFRESH — TRENDS TAB (5Hz)
        // ====================================================================

        private void RefreshTrendsTab()
        {
            if (engine == null) return;

            // ── Chart 1: Temperatures ───────────────────────────────────
            if (_trend_chartTemp != null)
            {
                _trend_chartTemp.AddValue(_trend_traceTAvg, engine.T_avg);
                _trend_chartTemp.AddValue(_trend_traceTHot, engine.T_hot);
                _trend_chartTemp.AddValue(_trend_traceTCold, engine.T_cold);
                _trend_chartTemp.AddValue(_trend_traceTPzr, engine.T_pzr);
                _trend_chartTemp.AddValue(_trend_traceTSat, engine.T_sat);
                _trend_chartTemp.AddValue(_trend_traceTSg, engine.T_sg_secondary);
            }

            // ── Chart 2: Pressures ──────────────────────────────────────
            if (_trend_chartPress != null)
            {
                _trend_chartPress.AddValue(_trend_traceRCSPress, engine.pressure);
                _trend_chartPress.AddValue(_trend_traceSGPress,
                    engine.sgSecondaryPressure_psia);
                // Scale pressure rate: offset to positive range, ×10 for visibility
                float scaledRate = Mathf.Clamp(
                    1300f + engine.pressureRate * 10f, 0f, 2600f);
                _trend_chartPress.AddValue(_trend_tracePressRate, scaledRate);
            }

            // ── Chart 3: Levels & Flows ─────────────────────────────────
            if (_trend_chartLevels != null)
            {
                _trend_chartLevels.AddValue(_trend_tracePzrLevel, engine.pzrLevel);
                _trend_chartLevels.AddValue(_trend_traceVCTLevel,
                    engine.vctState.Level_percent);
                _trend_chartLevels.AddValue(_trend_traceCharging,
                    engine.chargingFlow * 0.5f);
                _trend_chartLevels.AddValue(_trend_traceLetdown,
                    engine.letdownFlow * 0.5f);
                _trend_chartLevels.AddValue(_trend_traceSurge,
                    Mathf.Abs(engine.surgeFlow) * 0.5f);
            }

            // ── Chart 4: Thermal Balance ────────────────────────────────
            if (_trend_chartThermal != null)
            {
                _trend_chartThermal.AddValue(_trend_traceRCPHeat, engine.rcpHeat);
                _trend_chartThermal.AddValue(_trend_traceSGHeat,
                    engine.sgHeatTransfer_MW);
                _trend_chartThermal.AddValue(_trend_traceRHRNet,
                    Mathf.Max(0f, engine.rhrNetHeat_MW));
                _trend_chartThermal.AddValue(_trend_traceSteamDump,
                    engine.steamDumpHeat_MW);
                // Net heat can be negative; offset to keep in range
                _trend_chartThermal.AddValue(_trend_traceNetHeat,
                    engine.netPlantHeat_MW);
            }
        }
    }
}
