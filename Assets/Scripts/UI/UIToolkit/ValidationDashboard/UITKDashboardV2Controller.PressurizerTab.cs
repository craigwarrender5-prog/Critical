// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.PressurizerTab.cs — PRESSURIZER Tab (Tab 2)
// ============================================================================
//
// PURPOSE:
//   Builds the PRESSURIZER Tab (Tab 2) — Pressurizer System Detail:
//     - Left: Dual ArcGauge cluster (PZR Pressure + PZR Level)
//     - Center: PZR state machine panel, heater/spray status, surge flow
//     - Right: TankLevel for PZR vessel, PZR thermal strip chart
//     - Bottom: Heater authority / limiter detail, closure solver status
//
// DATA BINDING:
//   RefreshPressurizerTab() at 5Hz when tab 2 is active.
//
// IP: IP-0060 Stage 5
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
        // PRESSURIZER TAB — REFERENCES
        // ====================================================================

        // Arc gauges
        private ArcGaugeElement _pzr_arcPressure;
        private ArcGaugeElement _pzr_arcLevel;

        // Metric labels
        private readonly Dictionary<string, Label> _pzr_metricValues =
            new Dictionary<string, Label>(32);

        // Tank graphic
        private TankLevelPOC _pzr_tankVessel;

        // Strip chart (PZR temp + T_sat + pressure)
        private StripChartPOC _pzr_chart;
        private int _pzr_tracePzrTemp = -1;
        private int _pzr_traceTsat = -1;
        private int _pzr_tracePressure = -1;

        // Heater bar
        private LinearGaugePOC _pzr_heaterBar;
        private Label _pzr_heaterValue;

        // Spray bar
        private LinearGaugePOC _pzr_sprayBar;
        private Label _pzr_sprayValue;

        // LEDs
        private LEDIndicatorElement _pzr_ledHeater;
        private LEDIndicatorElement _pzr_ledSpray;
        private LEDIndicatorElement _pzr_ledBubble;
        private LEDIndicatorElement _pzr_ledHold;

        // ====================================================================
        // BUILD
        // ====================================================================

        private VisualElement BuildPressurizerTab()
        {
            var root = MakeTabRoot("v2-tab-pzr");

            // ── Top row: Gauges + State + Vessel/Chart ──────────────────
            var topRow = MakeRow(2.2f);
            topRow.style.minHeight = 0f;

            topRow.Add(BuildPZRGaugeCluster());
            topRow.Add(BuildPZRStatePanel());
            topRow.Add(BuildPZRVesselAndChart());

            root.Add(topRow);

            // ── Bottom row: Heater authority + Closure solver ───────────
            var bottomRow = MakeRow(1f);
            bottomRow.style.minHeight = 0f;

            bottomRow.Add(BuildPZRHeaterAuthority());
            bottomRow.Add(BuildPZRClosureSolver());

            root.Add(bottomRow);

            return root;
        }

        // ====================================================================
        // LEFT — Dual ArcGauge Cluster
        // ====================================================================

        private VisualElement BuildPZRGaugeCluster()
        {
            var panel = MakePanel("PZR INSTRUMENTS");
            panel.style.flexGrow = 0.85f;
            panel.style.minWidth = 0f;
            panel.style.alignItems = Align.Center;

            // Pressure gauge
            _pzr_arcPressure = new ArcGaugeElement
            {
                minValue = 0f,
                maxValue = 2600f,
                label = "PRESSURE",
                unit = " psia",
                valueFormat = "F0"
            };
            _pzr_arcPressure.SetThresholds(350f, 2400f, 200f, 2500f);
            _pzr_arcPressure.style.width = 160f;
            _pzr_arcPressure.style.height = 170f;
            _allArcGauges.Add(_pzr_arcPressure);
            panel.Add(_pzr_arcPressure);

            // Level gauge
            _pzr_arcLevel = new ArcGaugeElement
            {
                minValue = 0f,
                maxValue = 100f,
                label = "PZR LEVEL",
                unit = " %",
                valueFormat = "F1"
            };
            _pzr_arcLevel.SetThresholds(17f, 92f, 10f, 97f);
            _pzr_arcLevel.style.width = 160f;
            _pzr_arcLevel.style.height = 170f;
            _pzr_arcLevel.style.marginTop = 8f;
            _allArcGauges.Add(_pzr_arcLevel);
            panel.Add(_pzr_arcLevel);

            return panel;
        }

        // ====================================================================
        // CENTER — PZR State Machine, Heater/Spray bars, Metrics
        // ====================================================================

        private VisualElement BuildPZRStatePanel()
        {
            var panel = MakePanel("PZR STATE & CONTROL");
            panel.style.flexGrow = 1.3f;
            panel.style.minWidth = 0f;

            // ── LED status row ──────────────────────────────────────────
            var ledRow = MakeRow();
            ledRow.style.justifyContent = Justify.SpaceAround;
            ledRow.style.marginBottom = 8f;

            _pzr_ledBubble = new LEDIndicatorElement();
            _pzr_ledBubble.Configure("BUBBLE", new Color(0.18f, 0.85f, 0.25f, 1f), false);
            _allLEDs.Add(_pzr_ledBubble);

            _pzr_ledHeater = new LEDIndicatorElement();
            _pzr_ledHeater.Configure("HEATERS", new Color(1f, 0.55f, 0f, 1f), false);
            _allLEDs.Add(_pzr_ledHeater);

            _pzr_ledSpray = new LEDIndicatorElement();
            _pzr_ledSpray.Configure("SPRAY", new Color(0.3f, 0.7f, 1f, 1f), false);
            _allLEDs.Add(_pzr_ledSpray);

            _pzr_ledHold = new LEDIndicatorElement();
            _pzr_ledHold.Configure("HOLD", new Color(1f, 0.2f, 0.2f, 1f), false);
            _pzr_ledHold.isFlashing = true;
            _allLEDs.Add(_pzr_ledHold);

            ledRow.Add(_pzr_ledBubble);
            ledRow.Add(_pzr_ledHeater);
            ledRow.Add(_pzr_ledSpray);
            ledRow.Add(_pzr_ledHold);
            panel.Add(ledRow);

            // ── Metric rows ─────────────────────────────────────────────
            panel.Add(MakePZRMetric("pzr_state", "PZR STATE", ""));
            panel.Add(MakePZRMetric("pzr_phase", "PHASE", ""));
            panel.Add(MakePZRMetric("pzr_temp", "PZR TEMP", "°F"));
            panel.Add(MakePZRMetric("pzr_tsat", "T SAT", "°F"));
            panel.Add(MakePZRMetric("pzr_subcool", "PZR SUBCOOL", "°F"));
            panel.Add(MakePZRMetric("pzr_surge", "SURGE FLOW", "gpm"));
            panel.Add(MakePZRMetric("pzr_heat_rate", "PZR dT", "°F/hr"));
            panel.Add(MakePZRMetric("pzr_press_rate", "dP/dt", "psi/hr"));

            // ── Heater bar ──────────────────────────────────────────────
            var heaterLabel = MakeLabel("HEATER OUTPUT", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            heaterLabel.style.marginTop = 8f;
            panel.Add(heaterLabel);

            var heaterRow = MakeRow();
            heaterRow.style.alignItems = Align.Center;

            _pzr_heaterBar = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = 0f,
                maxValue = 1.8f,
                warningThreshold = 1.4f,
                alarmThreshold = 1.7f
            };
            _pzr_heaterBar.style.height = 16f;
            _pzr_heaterBar.style.flexGrow = 1f;
            heaterRow.Add(_pzr_heaterBar);

            _pzr_heaterValue = MakeLabel("0.0 MW", 10f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            _pzr_heaterValue.style.marginLeft = 6f;
            _pzr_heaterValue.style.minWidth = 60f;
            heaterRow.Add(_pzr_heaterValue);

            panel.Add(heaterRow);

            // ── Spray bar ───────────────────────────────────────────────
            var sprayLabel = MakeLabel("SPRAY FLOW", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            sprayLabel.style.marginTop = 6f;
            panel.Add(sprayLabel);

            var sprayRow = MakeRow();
            sprayRow.style.alignItems = Align.Center;

            _pzr_sprayBar = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = 0f,
                maxValue = 500f,
                warningThreshold = 300f,
                alarmThreshold = 450f
            };
            _pzr_sprayBar.style.height = 16f;
            _pzr_sprayBar.style.flexGrow = 1f;
            sprayRow.Add(_pzr_sprayBar);

            _pzr_sprayValue = MakeLabel("0.0 gpm", 10f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            _pzr_sprayValue.style.marginLeft = 6f;
            _pzr_sprayValue.style.minWidth = 60f;
            sprayRow.Add(_pzr_sprayValue);

            panel.Add(sprayRow);

            return panel;
        }

        /// <summary>Compact metric tile for PZR tab.</summary>
        private VisualElement MakePZRMetric(string key, string title, string unit)
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
            _pzr_metricValues[key] = valueLbl;

            return row;
        }

        // ====================================================================
        // RIGHT — PZR Vessel Tank + Thermal Strip Chart
        // ====================================================================

        private VisualElement BuildPZRVesselAndChart()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1.05f;
            container.style.minWidth = 0f;
            container.style.flexDirection = FlexDirection.Column;

            // ── Vessel graphic ──────────────────────────────────────────
            var vesselPanel = MakePanel("PZR VESSEL");
            vesselPanel.style.flexGrow = 0f;
            vesselPanel.style.alignItems = Align.Center;

            _pzr_tankVessel = new TankLevelPOC
            {
                minValue = 0f,
                maxValue = 100f,
                lowAlarm = 17f,
                highAlarm = 92f
            };
            _pzr_tankVessel.style.width = 80f;
            _pzr_tankVessel.style.height = 120f;
            vesselPanel.Add(_pzr_tankVessel);

            // Volume readouts
            vesselPanel.Add(MakePZRMetric("pzr_water_vol", "WATER", "ft³"));
            vesselPanel.Add(MakePZRMetric("pzr_steam_vol", "STEAM", "ft³"));

            container.Add(vesselPanel);

            // ── Thermal strip chart ─────────────────────────────────────
            var chartPanel = MakePanel("PZR THERMAL");
            chartPanel.style.flexGrow = 1f;

            _pzr_chart = new StripChartPOC();
            _pzr_chart.style.flexGrow = 1f;
            _pzr_chart.style.minHeight = 140f;

            _pzr_tracePzrTemp = _pzr_chart.AddTrace("T PZR",
                new Color(1f, 0.55f, 0.3f, 1f), 50f, 700f);
            _pzr_traceTsat = _pzr_chart.AddTrace("T SAT",
                new Color(0.8f, 0.8f, 0.3f, 1f), 50f, 700f);
            _pzr_tracePressure = _pzr_chart.AddTrace("P (×0.25)",
                new Color(0.3f, 0.7f, 1f, 1f), 0f, 700f);

            chartPanel.Add(_pzr_chart);
            container.Add(chartPanel);

            return container;
        }

        // ====================================================================
        // BOTTOM LEFT — Heater Authority Detail
        // ====================================================================

        private VisualElement BuildPZRHeaterAuthority()
        {
            var panel = MakePanel("HEATER AUTHORITY");
            panel.style.flexGrow = 1.2f;
            panel.style.minWidth = 0f;

            panel.Add(MakePZRMetric("pzr_htr_mode", "HEATER MODE", ""));
            panel.Add(MakePZRMetric("pzr_htr_auth", "AUTHORITY", ""));
            panel.Add(MakePZRMetric("pzr_htr_limiter", "LIMITER", ""));
            panel.Add(MakePZRMetric("pzr_htr_detail", "DETAIL", ""));
            panel.Add(MakePZRMetric("pzr_pid_active", "PID ACTIVE", ""));
            panel.Add(MakePZRMetric("pzr_pid_output", "PID OUTPUT", ""));
            panel.Add(MakePZRMetric("pzr_hold_status", "STARTUP HOLD", ""));

            return panel;
        }

        // ====================================================================
        // BOTTOM RIGHT — Closure Solver Status
        // ====================================================================

        private VisualElement BuildPZRClosureSolver()
        {
            var panel = MakePanel("CLOSURE SOLVER");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            panel.Add(MakePZRMetric("pzr_closure_ok", "CONVERGED", ""));
            panel.Add(MakePZRMetric("pzr_closure_iters", "ITERATIONS", ""));
            panel.Add(MakePZRMetric("pzr_closure_vol_res", "VOL RESID", "ft³"));
            panel.Add(MakePZRMetric("pzr_closure_nrg_res", "NRG RESID", "BTU"));
            panel.Add(MakePZRMetric("pzr_closure_pct", "CONVERGENCE", "%"));
            panel.Add(MakePZRMetric("pzr_closure_pattern", "PATTERN", ""));

            return panel;
        }

        // ====================================================================
        // DATA REFRESH — PRESSURIZER TAB (5Hz)
        // ====================================================================

        private void RefreshPressurizerTab()
        {
            if (engine == null) return;

            // ── Arc gauges ──────────────────────────────────────────────
            if (_pzr_arcPressure != null) _pzr_arcPressure.value = engine.pressure;
            if (_pzr_arcLevel != null) _pzr_arcLevel.value = engine.pzrLevel;

            // ── LEDs ────────────────────────────────────────────────────
            if (_pzr_ledBubble != null)
                _pzr_ledBubble.isOn = engine.bubbleFormed && !engine.solidPressurizer;
            if (_pzr_ledHeater != null)
                _pzr_ledHeater.isOn = engine.pzrHeatersOn;
            if (_pzr_ledSpray != null)
                _pzr_ledSpray.isOn = engine.sprayActive;
            if (_pzr_ledHold != null)
            {
                _pzr_ledHold.isOn = engine.startupHoldActive;
                _pzr_ledHold.isFlashing = engine.startupHoldActive;
            }

            // ── State metrics ───────────────────────────────────────────
            string pzrState = engine.solidPressurizer ? "SOLID"
                : engine.bubbleFormed ? "TWO-PHASE" : "TRANSITION";
            Color pzrStateColor = engine.solidPressurizer
                ? UITKDashboardTheme.WarningAmber
                : engine.bubbleFormed
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed;
            SetPZRMetric("pzr_state", pzrState, pzrStateColor);

            SetPZRMetric("pzr_phase", engine.heatupPhaseDesc,
                UITKDashboardTheme.TextSecondary);

            float pzrSubcool = engine.T_sat - engine.T_pzr;
            SetPZRMetric("pzr_temp", $"{engine.T_pzr:F1}", TempColor(engine.T_pzr));
            SetPZRMetric("pzr_tsat", $"{engine.T_sat:F1}", UITKDashboardTheme.TextSecondary);
            SetPZRMetric("pzr_subcool", $"{pzrSubcool:F1}",
                pzrSubcool < 10f && !engine.solidPressurizer
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);

            SetPZRMetric("pzr_surge", $"{engine.surgeFlow:F1}",
                Mathf.Abs(engine.surgeFlow) > 50f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.InfoCyan);

            SetPZRMetric("pzr_heat_rate", $"{engine.pzrHeatRate:F1}",
                UITKDashboardTheme.InfoCyan);

            float absPR = Mathf.Abs(engine.pressureRate);
            SetPZRMetric("pzr_press_rate", $"{engine.pressureRate:F1}",
                absPR > 200f ? UITKDashboardTheme.AlarmRed
                : absPR > 100f ? UITKDashboardTheme.WarningAmber
                : UITKDashboardTheme.NormalGreen);

            // ── Heater bar ──────────────────────────────────────────────
            if (_pzr_heaterBar != null) _pzr_heaterBar.value = engine.pzrHeaterPower;
            if (_pzr_heaterValue != null)
            {
                _pzr_heaterValue.text = $"{engine.pzrHeaterPower:F3} MW";
                _pzr_heaterValue.style.color = engine.pzrHeatersOn
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary;
            }

            // ── Spray bar ───────────────────────────────────────────────
            if (_pzr_sprayBar != null) _pzr_sprayBar.value = engine.sprayFlow_GPM;
            if (_pzr_sprayValue != null)
            {
                _pzr_sprayValue.text = $"{engine.sprayFlow_GPM:F1} gpm";
                _pzr_sprayValue.style.color = engine.sprayActive
                    ? UITKDashboardTheme.AccentBlue
                    : UITKDashboardTheme.TextSecondary;
            }

            // ── Tank vessel ─────────────────────────────────────────────
            if (_pzr_tankVessel != null) _pzr_tankVessel.value = engine.pzrLevel;

            SetPZRMetric("pzr_water_vol", $"{engine.pzrWaterVolume:F1}",
                UITKDashboardTheme.AccentBlue);
            SetPZRMetric("pzr_steam_vol", $"{engine.pzrSteamVolume:F1}",
                engine.pzrSteamVolume > 0.1f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);

            // ── Strip chart ─────────────────────────────────────────────
            if (_pzr_chart != null)
            {
                _pzr_chart.AddValue(_pzr_tracePzrTemp, engine.T_pzr);
                _pzr_chart.AddValue(_pzr_traceTsat, engine.T_sat);
                // Scale pressure to fit temp range (0-2600 → 0-650)
                _pzr_chart.AddValue(_pzr_tracePressure, engine.pressure * 0.25f);
            }

            // ── Heater authority ─────────────────────────────────────────
            SetPZRMetric("pzr_htr_mode", engine.currentHeaterMode.ToString(),
                UITKDashboardTheme.InfoCyan);
            SetPZRMetric("pzr_htr_auth", engine.heaterAuthorityState,
                engine.heaterAuthorityState == "AUTO"
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);
            SetPZRMetric("pzr_htr_limiter", engine.heaterLimiterReason,
                engine.heaterLimiterReason == "NONE"
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);
            SetPZRMetric("pzr_htr_detail", TruncateString(engine.heaterLimiterDetail, 40),
                UITKDashboardTheme.TextSecondary);

            SetPZRMetric("pzr_pid_active", engine.heaterPIDActive ? "YES" : "NO",
                engine.heaterPIDActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetPZRMetric("pzr_pid_output", $"{engine.heaterPIDOutput * 100f:F1}%",
                UITKDashboardTheme.InfoCyan);

            SetPZRMetric("pzr_hold_status",
                engine.startupHoldActive
                    ? $"ACTIVE ({engine.startupHoldReleaseBlockReason})"
                    : "RELEASED",
                engine.startupHoldActive
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.NormalGreen);

            // ── Closure solver ───────────────────────────────────────────
            SetPZRMetric("pzr_closure_ok",
                engine.pzrClosureConverged ? "YES" : "NO",
                engine.pzrClosureConverged
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed);
            SetPZRMetric("pzr_closure_iters",
                $"{engine.pzrClosureLastIterationCount}",
                UITKDashboardTheme.InfoCyan);
            SetPZRMetric("pzr_closure_vol_res",
                $"{engine.pzrClosureVolumeResidual_ft3:F4}",
                Mathf.Abs(engine.pzrClosureVolumeResidual_ft3) > 1f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);
            SetPZRMetric("pzr_closure_nrg_res",
                $"{engine.pzrClosureEnergyResidual_BTU:F1}",
                Mathf.Abs(engine.pzrClosureEnergyResidual_BTU) > 100f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);
            SetPZRMetric("pzr_closure_pct",
                $"{engine.pzrClosureConvergencePct:F1}",
                engine.pzrClosureConvergencePct > 95f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);
            SetPZRMetric("pzr_closure_pattern",
                TruncateString(engine.pzrClosureLastConvergencePattern, 30),
                UITKDashboardTheme.TextSecondary);
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private void SetPZRMetric(string key, string text, Color color)
        {
            if (_pzr_metricValues.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        /// <summary>Truncate a string to max length with ellipsis.</summary>
        private static string TruncateString(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "--";
            return s.Length <= maxLen ? s : s.Substring(0, maxLen - 1) + "…";
        }
    }
}
