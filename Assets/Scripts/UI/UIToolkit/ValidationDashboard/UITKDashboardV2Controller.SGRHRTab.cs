// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.SGRHRTab.cs — SG / RHR Tab Builder (Tab 4)
// ============================================================================
//
// PURPOSE:
//   Builds the SG / RHR Tab (Tab 4) — Steam Generator & Residual Heat Removal:
//     - Left: SG thermal state (multi-node temps, thermocline, boiling)
//     - Center: SG secondary pressure model, boundary/startup state
//     - Right: RHR system status, heat balance, draining status
//     - Bottom: SG thermal strip chart (top/bottom node, T_sat, pressure)
//
// DATA BINDING:
//   RefreshSGRHRTab() at 5Hz when tab 4 is active.
//
// IP: IP-0060 Stage 7
// VERSION: 6.0.0
// DATE: 2026-02-18
// ============================================================================

using System.Collections.Generic;
using Critical.Physics;
using Critical.UI.Elements;
using Critical.UI.POC;
using Critical.Validation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    public partial class UITKDashboardV2Controller
    {
        // ====================================================================
        // SG/RHR TAB — REFERENCES
        // ====================================================================

        private readonly Dictionary<string, Label> _sg_metricValues =
            new Dictionary<string, Label>(48);

        // LEDs
        private LEDIndicatorElement _sg_ledBoiling;
        private LEDIndicatorElement _sg_ledDraining;
        private LEDIndicatorElement _sg_ledN2Isolated;
        private LEDIndicatorElement _sg_ledRHRActive;
        private LEDIndicatorElement _sg_ledSteaming;

        // Strip chart
        private StripChartPOC _sg_chart;
        private int _sg_traceTopNode = -1;
        private int _sg_traceBottomNode = -1;
        private int _sg_traceTsat = -1;
        private int _sg_traceSecPressure = -1;

        // Thermocline bar
        private LinearGaugePOC _sg_thermoclineBar;

        // RHR heat bar
        private LinearGaugePOC _sg_rhrHeatBar;
        private Label _sg_rhrHeatLabel;

        // SG draining bar
        private LinearGaugePOC _sg_drainingBar;

        // ====================================================================
        // BUILD
        // ====================================================================

        private VisualElement BuildSGRHRTab()
        {
            var root = MakeTabRoot("v2-tab-sgrhr");

            // ── Top row: SG Thermal + SG Pressure/Boundary + RHR ────────
            var topRow = MakeRow(2.2f);
            topRow.style.minHeight = 0f;

            topRow.Add(BuildSGThermalPanel());
            topRow.Add(BuildSGPressurePanel());
            topRow.Add(BuildRHRPanel());

            root.Add(topRow);

            // ── Bottom: Strip chart ─────────────────────────────────────
            var bottomRow = MakeRow(1.1f);
            bottomRow.style.minHeight = 0f;
            bottomRow.Add(BuildSGStripChart());
            root.Add(bottomRow);

            return root;
        }

        // ====================================================================
        // LEFT — SG Thermal State (Multi-Node Model)
        // ====================================================================

        private VisualElement BuildSGThermalPanel()
        {
            var panel = MakePanel("SG THERMAL STATE");
            panel.style.flexGrow = 1.1f;
            panel.style.minWidth = 0f;

            // ── LED row ─────────────────────────────────────────────────
            var ledRow = MakeRow();
            ledRow.style.justifyContent = Justify.SpaceAround;
            ledRow.style.marginBottom = 6f;

            _sg_ledBoiling = new LEDIndicatorElement();
            _sg_ledBoiling.Configure("BOIL", new Color(1f, 0.35f, 0.15f, 1f), false);
            _allLEDs.Add(_sg_ledBoiling);

            _sg_ledDraining = new LEDIndicatorElement();
            _sg_ledDraining.Configure("DRAIN", new Color(1f, 0.78f, 0f, 1f), false);
            _allLEDs.Add(_sg_ledDraining);

            _sg_ledN2Isolated = new LEDIndicatorElement();
            _sg_ledN2Isolated.Configure("N₂ ISO", new Color(0.3f, 0.7f, 1f, 1f), false);
            _allLEDs.Add(_sg_ledN2Isolated);

            _sg_ledSteaming = new LEDIndicatorElement();
            _sg_ledSteaming.Configure("STEAM", new Color(0.8f, 0.8f, 0.3f, 1f), false);
            _allLEDs.Add(_sg_ledSteaming);

            ledRow.Add(_sg_ledBoiling);
            ledRow.Add(_sg_ledDraining);
            ledRow.Add(_sg_ledN2Isolated);
            ledRow.Add(_sg_ledSteaming);
            panel.Add(ledRow);

            // ── Temperature metrics ─────────────────────────────────────
            panel.Add(MakeSGMetric("sg_t_secondary", "T SECONDARY", "°F"));
            panel.Add(MakeSGMetric("sg_t_top", "TOP NODE", "°F"));
            panel.Add(MakeSGMetric("sg_t_bottom", "BOTTOM NODE", "°F"));
            panel.Add(MakeSGMetric("sg_strat_dt", "STRATIFY ΔT", "°F"));
            panel.Add(MakeSGMetric("sg_t_sat", "SG T SAT", "°F"));
            panel.Add(MakeSGMetric("sg_superheat", "MAX SUPERHEAT", "°F"));
            panel.Add(MakeSGMetric("sg_heat_xfer", "SG HEAT XFER", "MW"));
            panel.Add(MakeSGMetric("sg_boil_intens", "BOILING", ""));

            // ── Thermocline bar ─────────────────────────────────────────
            var tcLabel = MakeLabel("THERMOCLINE HEIGHT", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            tcLabel.style.marginTop = 6f;
            panel.Add(tcLabel);

            _sg_thermoclineBar = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = 0f,
                maxValue = 70f,     // SG tube height ~68 ft
                warningThreshold = 50f,
                alarmThreshold = 65f
            };
            _sg_thermoclineBar.style.height = 16f;
            panel.Add(_sg_thermoclineBar);

            panel.Add(MakeSGMetric("sg_tc_height", "HEIGHT", "ft"));
            panel.Add(MakeSGMetric("sg_active_area", "ACTIVE AREA", "%"));

            return panel;
        }

        // ====================================================================
        // CENTER — SG Secondary Pressure & Boundary State
        // ====================================================================

        private VisualElement BuildSGPressurePanel()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1f;
            container.style.minWidth = 0f;
            container.style.flexDirection = FlexDirection.Column;

            // ── SG Pressure Model ───────────────────────────────────────
            var pressPanel = MakePanel("SG SECONDARY PRESSURE");
            pressPanel.style.flexGrow = 1f;

            pressPanel.Add(MakeSGMetric("sg_press_psia", "PRESSURE", "psia"));
            pressPanel.Add(MakeSGMetric("sg_press_psig", "PRESSURE", "psig"));
            pressPanel.Add(MakeSGMetric("sg_bound_mode", "BOUNDARY", ""));
            pressPanel.Add(MakeSGMetric("sg_press_src", "P SOURCE", ""));
            pressPanel.Add(MakeSGMetric("sg_steam_inv", "STEAM INV", "lb"));

            container.Add(pressPanel);

            // ── Startup Boundary State ──────────────────────────────────
            var startPanel = MakePanel("SG STARTUP STATE");
            startPanel.style.flexGrow = 1f;

            startPanel.Add(MakeSGMetric("sg_startup_state", "STATE", ""));
            startPanel.Add(MakeSGMetric("sg_startup_ticks", "TICKS", ""));
            startPanel.Add(MakeSGMetric("sg_startup_time", "TIME", "hr"));
            startPanel.Add(MakeSGMetric("sg_hold_target", "HOLD TARGET", "psia"));
            startPanel.Add(MakeSGMetric("sg_hold_dev", "HOLD DEV", "%"));
            startPanel.Add(MakeSGMetric("sg_hold_leak", "HOLD LEAK", "%"));

            container.Add(startPanel);

            // ── Draining State ──────────────────────────────────────────
            var drainPanel = MakePanel("SG DRAINING");
            drainPanel.style.flexGrow = 0.7f;

            drainPanel.Add(MakeSGMetric("sg_drain_active", "ACTIVE", ""));
            drainPanel.Add(MakeSGMetric("sg_drain_rate", "RATE", "gpm"));
            drainPanel.Add(MakeSGMetric("sg_drain_total", "DRAINED", "lb"));
            drainPanel.Add(MakeSGMetric("sg_sec_mass", "SEC MASS", "lb"));

            _sg_drainingBar = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = 0f,
                maxValue = 100f,
                warningThreshold = 30f,
                alarmThreshold = 10f
            };
            _sg_drainingBar.style.height = 14f;
            _sg_drainingBar.style.marginTop = 4f;
            drainPanel.Add(_sg_drainingBar);

            drainPanel.Add(MakeSGMetric("sg_wr_level", "WR LEVEL", "%"));
            drainPanel.Add(MakeSGMetric("sg_nr_level", "NR LEVEL", "%"));

            container.Add(drainPanel);

            return container;
        }

        // ====================================================================
        // RIGHT — RHR System Status
        // ====================================================================

        private VisualElement BuildRHRPanel()
        {
            var panel = MakePanel("RESIDUAL HEAT REMOVAL");
            panel.style.flexGrow = 0.85f;
            panel.style.minWidth = 0f;

            // ── RHR LED ─────────────────────────────────────────────────
            _sg_ledRHRActive = new LEDIndicatorElement();
            _sg_ledRHRActive.Configure("RHR", new Color(0.18f, 0.85f, 0.25f, 1f), false);
            _sg_ledRHRActive.style.alignSelf = Align.Center;
            _sg_ledRHRActive.style.marginBottom = 6f;
            _allLEDs.Add(_sg_ledRHRActive);
            panel.Add(_sg_ledRHRActive);

            // ── RHR metrics ─────────────────────────────────────────────
            panel.Add(MakeSGMetric("rhr_mode", "MODE", ""));
            panel.Add(MakeSGMetric("rhr_net_heat", "NET HEAT", "MW"));
            panel.Add(MakeSGMetric("rhr_hx_removal", "HX REMOVAL", "MW"));
            panel.Add(MakeSGMetric("rhr_pump_heat", "PUMP HEAT", "MW"));
            panel.Add(MakeSGMetric("rhr_flow", "FLOW", "gpm"));
            panel.Add(MakeSGMetric("rhr_suction", "SUCTION", ""));
            panel.Add(MakeSGMetric("rhr_disc_temp", "DISC TEMP", "°F"));

            // ── RHR heat bar ────────────────────────────────────────────
            var heatLabel = MakeLabel("RHR NET THERMAL", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            heatLabel.style.marginTop = 8f;
            panel.Add(heatLabel);

            var heatRow = MakeRow();
            heatRow.style.alignItems = Align.Center;

            _sg_rhrHeatBar = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = -2f,
                maxValue = 2f,
                warningThreshold = 1.5f,
                alarmThreshold = 1.8f
            };
            _sg_rhrHeatBar.style.height = 16f;
            _sg_rhrHeatBar.style.flexGrow = 1f;
            heatRow.Add(_sg_rhrHeatBar);

            _sg_rhrHeatLabel = MakeLabel("0.0 MW", 10f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            _sg_rhrHeatLabel.style.marginLeft = 6f;
            _sg_rhrHeatLabel.style.minWidth = 55f;
            heatRow.Add(_sg_rhrHeatLabel);

            panel.Add(heatRow);

            // ── Letdown path info ───────────────────────────────────────
            panel.Add(MakeSGMetric("rhr_ltd_path", "LTD PATH", ""));
            panel.Add(MakeSGMetric("rhr_ltd_flow", "LTD FLOW", "gpm"));

            return panel;
        }

        // ====================================================================
        // BOTTOM — SG Thermal Strip Chart
        // ====================================================================

        private VisualElement BuildSGStripChart()
        {
            var panel = MakePanel("SG THERMAL TRENDS");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            _sg_chart = new StripChartPOC();
            _sg_chart.style.flexGrow = 1f;
            _sg_chart.style.minHeight = 160f;

            _sg_traceTopNode = _sg_chart.AddTrace("TOP",
                new Color(1f, 0.55f, 0.3f, 1f), 50f, 700f);
            _sg_traceBottomNode = _sg_chart.AddTrace("BOTTOM",
                new Color(0.3f, 0.7f, 1f, 1f), 50f, 700f);
            _sg_traceTsat = _sg_chart.AddTrace("T SAT",
                new Color(0.8f, 0.8f, 0.3f, 1f), 50f, 700f);
            _sg_traceSecPressure = _sg_chart.AddTrace("P (×0.5)",
                new Color(0.6f, 0.3f, 1f, 1f), 0f, 700f);

            panel.Add(_sg_chart);

            return panel;
        }

        // ====================================================================
        // METRIC FACTORY
        // ====================================================================

        private VisualElement MakeSGMetric(string key, string title, string unit)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 20f;
            row.style.marginBottom = 2f;
            row.style.paddingLeft = 4f;
            row.style.paddingRight = 4f;
            row.style.paddingTop = 1f;
            row.style.paddingBottom = 1f;
            row.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(row, 3f);

            string titleText = string.IsNullOrEmpty(unit) ? title : $"{title} ({unit})";
            row.Add(MakeLabel(titleText, 9f, FontStyle.Normal,
                new Color(0.63f, 0.72f, 0.84f, 1f)));

            var valueLbl = MakeLabel("--", 11f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            row.Add(valueLbl);
            _sg_metricValues[key] = valueLbl;

            return row;
        }

        // ====================================================================
        // DATA REFRESH — SG/RHR TAB (5Hz)
        // ====================================================================

        private void RefreshSGRHRTab()
        {
            if (engine == null) return;

            // ── LEDs ────────────────────────────────────────────────────
            if (_sg_ledBoiling != null) _sg_ledBoiling.isOn = engine.sgBoilingActive;
            if (_sg_ledDraining != null) _sg_ledDraining.isOn = engine.sgDrainingActive;
            if (_sg_ledN2Isolated != null) _sg_ledN2Isolated.isOn = engine.sgNitrogenIsolated;
            if (_sg_ledSteaming != null) _sg_ledSteaming.isOn = engine.sgSteaming;
            if (_sg_ledRHRActive != null) _sg_ledRHRActive.isOn = engine.rhrActive;

            // ── SG Thermal ──────────────────────────────────────────────
            SetSGMetric("sg_t_secondary", $"{engine.T_sg_secondary:F1}",
                TempColor(engine.T_sg_secondary));
            SetSGMetric("sg_t_top", $"{engine.sgTopNodeTemp:F1}",
                TempColor(engine.sgTopNodeTemp));
            SetSGMetric("sg_t_bottom", $"{engine.sgBottomNodeTemp:F1}",
                TempColor(engine.sgBottomNodeTemp));
            SetSGMetric("sg_strat_dt", $"{engine.sgStratificationDeltaT:F1}",
                engine.sgStratificationDeltaT > 50f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.InfoCyan);
            SetSGMetric("sg_t_sat", $"{engine.sgSaturationTemp_F:F1}",
                UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_superheat", $"{engine.sgMaxSuperheat_F:F1}",
                engine.sgMaxSuperheat_F > 5f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_heat_xfer", $"{engine.sgHeatTransfer_MW:F3}",
                engine.sgHeatTransfer_MW > 0.1f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);

            string boilText = engine.sgBoilingActive
                ? $"ACTIVE ({engine.sgBoilingIntensity * 100f:F0}%)"
                : "NONE";
            SetSGMetric("sg_boil_intens", boilText,
                engine.sgBoilingActive
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.TextSecondary);

            // ── Thermocline ─────────────────────────────────────────────
            if (_sg_thermoclineBar != null)
                _sg_thermoclineBar.value = engine.sgThermoclineHeight;
            SetSGMetric("sg_tc_height", $"{engine.sgThermoclineHeight:F1}",
                UITKDashboardTheme.InfoCyan);
            SetSGMetric("sg_active_area", $"{engine.sgActiveAreaFraction * 100f:F1}",
                engine.sgActiveAreaFraction > 0.5f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);

            // ── SG Secondary Pressure ───────────────────────────────────
            SetSGMetric("sg_press_psia", $"{engine.sgSecondaryPressure_psia:F1}",
                UITKDashboardTheme.InfoCyan);
            float sgPsig = engine.sgSecondaryPressure_psia - 14.696f;
            SetSGMetric("sg_press_psig", $"{sgPsig:F1}",
                engine.sgSecondaryPressureHigh
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.InfoCyan);
            SetSGMetric("sg_bound_mode", engine.sgBoundaryMode,
                engine.sgBoundaryMode == "ISOLATED"
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);
            SetSGMetric("sg_press_src", engine.sgPressureSourceBranch,
                UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_steam_inv", $"{engine.sgSteamInventory_lb:F0}",
                engine.sgSteamInventory_lb > 100f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);

            // ── Startup Boundary State ──────────────────────────────────
            SetSGMetric("sg_startup_state", engine.sgStartupBoundaryState,
                GetSGStartupColor(engine.sgStartupBoundaryState));
            SetSGMetric("sg_startup_ticks", $"{engine.sgStartupBoundaryStateTicks}",
                UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_startup_time", $"{engine.sgStartupBoundaryStateTime_hr:F3}",
                UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_hold_target", $"{engine.sgHoldTargetPressure_psia:F1}",
                UITKDashboardTheme.InfoCyan);
            SetSGMetric("sg_hold_dev", $"{engine.sgHoldPressureDeviation_pct:F2}",
                Mathf.Abs(engine.sgHoldPressureDeviation_pct) > 3f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);
            SetSGMetric("sg_hold_leak", $"{engine.sgHoldNetLeakage_pct:F3}",
                engine.sgHoldNetLeakage_pct > 0.2f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);

            // ── Draining ────────────────────────────────────────────────
            SetSGMetric("sg_drain_active",
                engine.sgDrainingActive ? "YES" :
                engine.sgDrainingComplete ? "COMPLETE" : "NO",
                engine.sgDrainingActive
                    ? UITKDashboardTheme.WarningAmber
                    : engine.sgDrainingComplete
                        ? UITKDashboardTheme.NormalGreen
                        : UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_drain_rate", $"{engine.sgDrainingRate_gpm:F1}",
                engine.sgDrainingActive
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("sg_drain_total", $"{engine.sgTotalMassDrained_lb:F0}",
                UITKDashboardTheme.InfoCyan);
            SetSGMetric("sg_sec_mass", $"{engine.sgSecondaryMass_lb:F0}",
                UITKDashboardTheme.AccentBlue);

            if (_sg_drainingBar != null)
                _sg_drainingBar.value = engine.sgWideRangeLevel_pct;
            SetSGMetric("sg_wr_level", $"{engine.sgWideRangeLevel_pct:F1}",
                UITKDashboardTheme.InfoCyan);
            SetSGMetric("sg_nr_level", $"{engine.sgNarrowRangeLevel_pct:F1}",
                UITKDashboardTheme.InfoCyan);

            // ── RHR ─────────────────────────────────────────────────────
            SetSGMetric("rhr_mode", engine.rhrModeString,
                engine.rhrActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("rhr_net_heat", $"{engine.rhrNetHeat_MW:F3}",
                engine.rhrNetHeat_MW > 0f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.AccentBlue);
            SetSGMetric("rhr_hx_removal", $"{engine.rhrHXRemoval_MW:F3}",
                engine.rhrHXRemoval_MW > 0.01f
                    ? UITKDashboardTheme.AccentBlue
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("rhr_pump_heat", $"{engine.rhrPumpHeat_MW:F3}",
                engine.rhrPumpHeat_MW > 0.01f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("rhr_flow", $"{engine.rhrState.FlowRate_gpm:F0}",
                engine.rhrActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("rhr_suction",
                engine.rhrState.SuctionValvesOpen ? "OPEN" : "CLOSED",
                engine.rhrState.SuctionValvesOpen
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetSGMetric("rhr_disc_temp", $"{engine.rhrState.MixedReturnTemp_F:F1}",
                TempColor(engine.rhrState.MixedReturnTemp_F));

            // RHR heat bar
            if (_sg_rhrHeatBar != null) _sg_rhrHeatBar.value = engine.rhrNetHeat_MW;
            if (_sg_rhrHeatLabel != null)
            {
                _sg_rhrHeatLabel.text = $"{engine.rhrNetHeat_MW:F3} MW";
                _sg_rhrHeatLabel.style.color = engine.rhrActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary;
            }

            // RHR letdown path
            SetSGMetric("rhr_ltd_path",
                engine.letdownViaRHR ? "RHR XCONN" : "ORIFICE",
                engine.letdownViaRHR
                    ? UITKDashboardTheme.InfoCyan
                    : UITKDashboardTheme.NormalGreen);
            SetSGMetric("rhr_ltd_flow", $"{engine.rhrLetdownFlow:F1}",
                engine.letdownViaRHR
                    ? UITKDashboardTheme.InfoCyan
                    : UITKDashboardTheme.TextSecondary);

            // ── Strip chart ─────────────────────────────────────────────
            if (_sg_chart != null)
            {
                _sg_chart.AddValue(_sg_traceTopNode, engine.sgTopNodeTemp);
                _sg_chart.AddValue(_sg_traceBottomNode, engine.sgBottomNodeTemp);
                _sg_chart.AddValue(_sg_traceTsat, engine.sgSaturationTemp_F);
                // Scale pressure to fit temp range
                _sg_chart.AddValue(_sg_traceSecPressure,
                    engine.sgSecondaryPressure_psia * 0.5f);
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private void SetSGMetric(string key, string text, Color color)
        {
            if (_sg_metricValues.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        private static Color GetSGStartupColor(string state)
        {
            if (string.IsNullOrEmpty(state)) return UITKDashboardTheme.TextSecondary;
            if (state.Contains("ISOLATED")) return UITKDashboardTheme.NormalGreen;
            if (state.Contains("HOLD")) return UITKDashboardTheme.WarningAmber;
            if (state.Contains("PRESSURIZE")) return UITKDashboardTheme.InfoCyan;
            return UITKDashboardTheme.TextSecondary;
        }
    }
}
