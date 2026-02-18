// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.CondenserTab.cs — Condenser/FW/Permissives Tab (Tab 5)
// ============================================================================
//
// PURPOSE:
//   Builds the CONDENSER Tab (Tab 5) — Secondary Systems Overview:
//     - Left: Condenser vacuum dynamics (vacuum gauge, backpressure, C-9)
//     - Center: Steam Dump section (mode, heat removal, steam pressure)
//     - Right: HZP Stabilization (progress, stable flag, heater PID, net heat)
//     - Bottom-Left: Permissive status panel (bridge FSM, individual checks)
//     - Bottom-Right: Condenser/Steam Dump strip chart
//
// DATA BINDING:
//   RefreshCondenserTab() at 5Hz when tab 5 is active.
//
// REFERENCES:
//   - IP-0046 CS-0115: Condenser/feedwater/permissives integration
//   - NRC HRTD 11.2: Steam Dump System
//   - NRC HRTD 19.0: Plant Operations at HZP
//
// IP: IP-0060 Stage 8A
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
        // CONDENSER TAB — REFERENCES
        // ====================================================================

        private readonly Dictionary<string, Label> _cond_metricValues =
            new Dictionary<string, Label>(48);

        // ArcGauges
        private ArcGaugeElement _cond_gaugeVacuum;
        private ArcGaugeElement _cond_gaugeHotwellLevel;
        private ArcGaugeElement _cond_gaugeSteamDumpHeat;
        private ArcGaugeElement _cond_gaugeSteamPressure;
        private ArcGaugeElement _cond_gaugeHzpProgress;

        // Tanks
        private TankLevelPOC _cond_cstTank;

        // LEDs
        private LEDIndicatorElement _cond_ledC9;
        private LEDIndicatorElement _cond_ledP12Bypass;
        private LEDIndicatorElement _cond_ledSteamDumpPermit;
        private LEDIndicatorElement _cond_ledSteamDumpActive;
        private LEDIndicatorElement _cond_ledHzpStable;
        private LEDIndicatorElement _cond_ledHzpReady;
        private LEDIndicatorElement _cond_ledHeaterPID;

        // Strip chart
        private StripChartPOC _cond_chart;
        private int _cond_traceVacuum = -1;
        private int _cond_traceSteamDumpHeat = -1;
        private int _cond_traceHotwellLevel = -1;

        // ====================================================================
        // BUILD
        // ====================================================================

        private VisualElement BuildCondenserTab()
        {
            var root = MakeTabRoot("v2-tab-condenser");

            // ── Top row: Condenser + Steam Dump + HZP ───────────────────
            var topRow = MakeRow(2f);
            topRow.style.minHeight = 0f;

            topRow.Add(BuildCondenserPanel());
            topRow.Add(BuildSteamDumpPanel());
            topRow.Add(BuildHZPPanel());

            root.Add(topRow);

            // ── Bottom row: Permissives + Strip chart ───────────────────
            var bottomRow = MakeRow(1.2f);
            bottomRow.style.minHeight = 0f;

            bottomRow.Add(BuildPermissivesPanel());
            bottomRow.Add(BuildCondenserStripChart());

            root.Add(bottomRow);

            return root;
        }

        // ====================================================================
        // LEFT — Condenser Vacuum Dynamics
        // ====================================================================

        private VisualElement BuildCondenserPanel()
        {
            var panel = MakePanel("CONDENSER VACUUM DYNAMICS");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            // ── Vacuum ArcGauge ─────────────────────────────────────────
            _cond_gaugeVacuum = new ArcGaugeElement
            {
                label = "VACUUM",
                unit = "inHg",
                minValue = 0f,
                maxValue = 30f,
                value = 0f
            };
            _cond_gaugeVacuum.style.width = 130f;
            _cond_gaugeVacuum.style.height = 80f;
            _cond_gaugeVacuum.style.alignSelf = Align.Center;
            _cond_gaugeVacuum.style.marginBottom = 4f;
            RegisterArcGauge("cond_vacuum", _cond_gaugeVacuum);
            panel.Add(_cond_gaugeVacuum);

            // ── LED row: C-9 + P-12 Bypass ──────────────────────────────
            var ledRow = MakeRow();
            ledRow.style.justifyContent = Justify.SpaceAround;
            ledRow.style.marginBottom = 6f;

            _cond_ledC9 = new LEDIndicatorElement();
            _cond_ledC9.Configure("C-9", new Color(0.18f, 0.85f, 0.25f, 1f), false);
            _allLEDs.Add(_cond_ledC9);

            _cond_ledP12Bypass = new LEDIndicatorElement();
            _cond_ledP12Bypass.Configure("P-12 BYP", new Color(1f, 0.78f, 0f, 1f), false);
            _allLEDs.Add(_cond_ledP12Bypass);

            _cond_ledSteamDumpPermit = new LEDIndicatorElement();
            _cond_ledSteamDumpPermit.Configure("DUMP OK", new Color(0.3f, 0.7f, 1f, 1f), false);
            _allLEDs.Add(_cond_ledSteamDumpPermit);

            ledRow.Add(_cond_ledC9);
            ledRow.Add(_cond_ledP12Bypass);
            ledRow.Add(_cond_ledSteamDumpPermit);
            panel.Add(ledRow);

            // ── Condenser metrics ───────────────────────────────────────
            panel.Add(MakeCondMetric("cond_vacuum_val", "VACUUM", "inHg"));
            panel.Add(MakeCondMetric("cond_backpress", "BACKPRESSURE", "psia"));
            panel.Add(MakeCondMetric("cond_pulldown", "PULLDOWN PHASE", ""));
            panel.Add(MakeCondMetric("cond_c9_status", "C-9 INTERLOCK", ""));

            // ── Hotwell level ArcGauge ──────────────────────────────────
            _cond_gaugeHotwellLevel = new ArcGaugeElement
            {
                label = "HOTWELL",
                unit = "%",
                minValue = 0f,
                maxValue = 100f,
                value = 0f
            };
            _cond_gaugeHotwellLevel.style.width = 110f;
            _cond_gaugeHotwellLevel.style.height = 70f;
            _cond_gaugeHotwellLevel.style.alignSelf = Align.Center;
            _cond_gaugeHotwellLevel.style.marginTop = 4f;
            RegisterArcGauge("cond_hotwell", _cond_gaugeHotwellLevel);
            panel.Add(_cond_gaugeHotwellLevel);

            panel.Add(MakeCondMetric("cond_hotwell_lvl", "HOTWELL LEVEL", "%"));
            panel.Add(MakeCondMetric("cond_fw_return", "FW RETURN", "lb/hr"));

            // ── CST Tank ────────────────────────────────────────────────
            var cstLabel = MakeLabel("CST", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            cstLabel.style.marginTop = 4f;
            panel.Add(cstLabel);

            _cond_cstTank = new TankLevelPOC
            {
                minValue = 0f, maxValue = 100f,
                lowAlarm = 15f, highAlarm = 95f
            };
            _cond_cstTank.style.width = 60f;
            _cond_cstTank.style.height = 60f;
            _cond_cstTank.style.alignSelf = Align.Center;
            panel.Add(_cond_cstTank);

            panel.Add(MakeCondMetric("cond_cst_level", "CST LEVEL", "%"));

            return panel;
        }

        // ====================================================================
        // CENTER — Steam Dump Controller
        // ====================================================================

        private VisualElement BuildSteamDumpPanel()
        {
            var panel = MakePanel("STEAM DUMP CONTROLLER");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            // ── Steam Dump Heat ArcGauge ────────────────────────────────
            _cond_gaugeSteamDumpHeat = new ArcGaugeElement
            {
                label = "DUMP HEAT",
                unit = "MW",
                minValue = 0f,
                maxValue = 30f,
                value = 0f
            };
            _cond_gaugeSteamDumpHeat.style.width = 130f;
            _cond_gaugeSteamDumpHeat.style.height = 80f;
            _cond_gaugeSteamDumpHeat.style.alignSelf = Align.Center;
            _cond_gaugeSteamDumpHeat.style.marginBottom = 4f;
            RegisterArcGauge("cond_dump_heat", _cond_gaugeSteamDumpHeat);
            panel.Add(_cond_gaugeSteamDumpHeat);

            // ── Steam Pressure ArcGauge ─────────────────────────────────
            _cond_gaugeSteamPressure = new ArcGaugeElement
            {
                label = "STEAM P",
                unit = "psig",
                minValue = 0f,
                maxValue = 1200f,
                value = 0f
            };
            _cond_gaugeSteamPressure.style.width = 110f;
            _cond_gaugeSteamPressure.style.height = 70f;
            _cond_gaugeSteamPressure.style.alignSelf = Align.Center;
            _cond_gaugeSteamPressure.style.marginBottom = 4f;
            RegisterArcGauge("cond_steam_press", _cond_gaugeSteamPressure);
            panel.Add(_cond_gaugeSteamPressure);

            // ── Steam Dump LED ──────────────────────────────────────────
            var sdLedRow = MakeRow();
            sdLedRow.style.justifyContent = Justify.Center;
            sdLedRow.style.marginBottom = 6f;

            _cond_ledSteamDumpActive = new LEDIndicatorElement();
            _cond_ledSteamDumpActive.Configure("DUMP ACTIVE",
                new Color(1f, 0.55f, 0f, 1f), false);
            _allLEDs.Add(_cond_ledSteamDumpActive);
            sdLedRow.Add(_cond_ledSteamDumpActive);
            panel.Add(sdLedRow);

            // ── Steam Dump metrics ──────────────────────────────────────
            panel.Add(MakeCondMetric("cond_sd_mode", "DUMP MODE", ""));
            panel.Add(MakeCondMetric("cond_sd_heat", "HEAT REMOVAL", "MW"));
            panel.Add(MakeCondMetric("cond_sd_steam_p", "STEAM PRESSURE", "psig"));
            panel.Add(MakeCondMetric("cond_sd_status", "STATUS", ""));
            panel.Add(MakeCondMetric("cond_net_plant", "NET PLANT HEAT", "MW"));

            return panel;
        }

        // ====================================================================
        // RIGHT — HZP Stabilization
        // ====================================================================

        private VisualElement BuildHZPPanel()
        {
            var panel = MakePanel("HZP STABILIZATION");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            // ── HZP Progress ArcGauge ───────────────────────────────────
            _cond_gaugeHzpProgress = new ArcGaugeElement
            {
                label = "HZP %",
                unit = "%",
                minValue = 0f,
                maxValue = 100f,
                value = 0f
            };
            _cond_gaugeHzpProgress.style.width = 130f;
            _cond_gaugeHzpProgress.style.height = 80f;
            _cond_gaugeHzpProgress.style.alignSelf = Align.Center;
            _cond_gaugeHzpProgress.style.marginBottom = 4f;
            RegisterArcGauge("cond_hzp_prog", _cond_gaugeHzpProgress);
            panel.Add(_cond_gaugeHzpProgress);

            // ── HZP LEDs ────────────────────────────────────────────────
            var hzpLedRow = MakeRow();
            hzpLedRow.style.justifyContent = Justify.SpaceAround;
            hzpLedRow.style.marginBottom = 6f;

            _cond_ledHzpStable = new LEDIndicatorElement();
            _cond_ledHzpStable.Configure("STABLE",
                new Color(0.18f, 0.85f, 0.25f, 1f), false);
            _allLEDs.Add(_cond_ledHzpStable);

            _cond_ledHzpReady = new LEDIndicatorElement();
            _cond_ledHzpReady.Configure("READY",
                new Color(0.18f, 0.85f, 0.25f, 1f), false);
            _allLEDs.Add(_cond_ledHzpReady);

            _cond_ledHeaterPID = new LEDIndicatorElement();
            _cond_ledHeaterPID.Configure("HTR PID",
                new Color(0.3f, 0.7f, 1f, 1f), false);
            _allLEDs.Add(_cond_ledHeaterPID);

            hzpLedRow.Add(_cond_ledHzpStable);
            hzpLedRow.Add(_cond_ledHzpReady);
            hzpLedRow.Add(_cond_ledHeaterPID);
            panel.Add(hzpLedRow);

            // ── HZP metrics ─────────────────────────────────────────────
            panel.Add(MakeCondMetric("cond_hzp_state", "HZP STATE", ""));
            panel.Add(MakeCondMetric("cond_hzp_progress", "PROGRESS", "%"));
            panel.Add(MakeCondMetric("cond_hzp_detail", "DETAIL", ""));
            panel.Add(MakeCondMetric("cond_htr_pid_out", "HTR PID OUTPUT", ""));
            panel.Add(MakeCondMetric("cond_htr_pid_status", "HTR PID STATUS", ""));
            panel.Add(MakeCondMetric("cond_net_heat_mw", "NET HEAT", "MW"));

            // ── Startup prerequisites ───────────────────────────────────
            var prereqLabel = MakeLabel("STARTUP PREREQUISITES", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            prereqLabel.style.marginTop = 6f;
            panel.Add(prereqLabel);

            panel.Add(MakeCondMetric("cond_prereq_temp", "T_AVG OK", ""));
            panel.Add(MakeCondMetric("cond_prereq_press", "PRESSURE OK", ""));
            panel.Add(MakeCondMetric("cond_prereq_level", "PZR LEVEL OK", ""));
            panel.Add(MakeCondMetric("cond_prereq_rcps", "ALL RCPs OK", ""));
            panel.Add(MakeCondMetric("cond_prereq_boron", "BORON OK", ""));
            panel.Add(MakeCondMetric("cond_prereq_all", "ALL MET", ""));

            return panel;
        }

        // ====================================================================
        // BOTTOM-LEFT — Permissive Status Panel
        // ====================================================================

        private VisualElement BuildPermissivesPanel()
        {
            var panel = MakePanel("STARTUP PERMISSIVES");
            panel.style.flexGrow = 0.85f;
            panel.style.minWidth = 0f;

            panel.Add(MakeCondMetric("cond_bridge_state", "BRIDGE FSM", ""));
            panel.Add(MakeCondMetric("cond_perm_c9", "C-9 CONDENSER", ""));
            panel.Add(MakeCondMetric("cond_perm_p12", "P-12 LO PRESS", ""));
            panel.Add(MakeCondMetric("cond_perm_p12_byp", "P-12 BYPASS", ""));
            panel.Add(MakeCondMetric("cond_perm_dump_sel", "DUMP MODE SEL", ""));
            panel.Add(MakeCondMetric("cond_perm_steam_p", "STEAM P CHECK", ""));
            panel.Add(MakeCondMetric("cond_perm_final", "DUMP PERMITTED", ""));
            panel.Add(MakeCondMetric("cond_perm_status", "STATUS MSG", ""));

            return panel;
        }

        // ====================================================================
        // BOTTOM-RIGHT — Strip Chart
        // ====================================================================

        private VisualElement BuildCondenserStripChart()
        {
            var panel = MakePanel("CONDENSER / STEAM DUMP TRENDS");
            panel.style.flexGrow = 1.15f;
            panel.style.minWidth = 0f;

            _cond_chart = new StripChartPOC();
            _cond_chart.style.flexGrow = 1f;
            _cond_chart.style.minHeight = 160f;

            _cond_traceVacuum = _cond_chart.AddTrace("VACUUM",
                new Color(0.3f, 0.7f, 1f, 1f), 0f, 30f);
            _cond_traceSteamDumpHeat = _cond_chart.AddTrace("DUMP HEAT",
                new Color(1f, 0.55f, 0f, 1f), 0f, 30f);
            _cond_traceHotwellLevel = _cond_chart.AddTrace("HOTWELL %",
                new Color(0.18f, 0.85f, 0.25f, 1f), 0f, 100f);

            panel.Add(_cond_chart);

            return panel;
        }

        // ====================================================================
        // METRIC FACTORY
        // ====================================================================

        private VisualElement MakeCondMetric(string key, string title, string unit)
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
            _cond_metricValues[key] = valueLbl;

            return row;
        }

        // ====================================================================
        // DATA REFRESH — CONDENSER TAB (5Hz)
        // ====================================================================

        private void RefreshCondenserTab()
        {
            if (engine == null) return;

            // ── ArcGauges ───────────────────────────────────────────────
            if (_cond_gaugeVacuum != null)
                _cond_gaugeVacuum.value = engine.condenserVacuum_inHg;
            if (_cond_gaugeHotwellLevel != null)
                _cond_gaugeHotwellLevel.value = engine.hotwellLevel_pct;
            if (_cond_gaugeSteamDumpHeat != null)
                _cond_gaugeSteamDumpHeat.value = engine.steamDumpHeat_MW;
            if (_cond_gaugeSteamPressure != null)
                _cond_gaugeSteamPressure.value = Mathf.Max(0f, engine.steamPressure_psig);
            if (_cond_gaugeHzpProgress != null)
                _cond_gaugeHzpProgress.value = engine.hzpProgress;

            // ── Tank ────────────────────────────────────────────────────
            if (_cond_cstTank != null)
                _cond_cstTank.value = engine.cstLevel_pct;

            // ── LEDs ────────────────────────────────────────────────────
            if (_cond_ledC9 != null)
                _cond_ledC9.isOn = engine.condenserC9Available;
            if (_cond_ledP12Bypass != null)
                _cond_ledP12Bypass.isOn = engine.p12BypassCommanded;
            if (_cond_ledSteamDumpPermit != null)
                _cond_ledSteamDumpPermit.isOn = engine.steamDumpPermitted;
            if (_cond_ledSteamDumpActive != null)
                _cond_ledSteamDumpActive.isOn = engine.steamDumpActive;
            if (_cond_ledHzpStable != null)
                _cond_ledHzpStable.isOn = engine.hzpStable;
            if (_cond_ledHzpReady != null)
                _cond_ledHzpReady.isOn = engine.hzpReadyForStartup;
            if (_cond_ledHeaterPID != null)
                _cond_ledHeaterPID.isOn = engine.heaterPIDActive;

            // ── Condenser metrics ───────────────────────────────────────
            SetCondMetric("cond_vacuum_val",
                $"{engine.condenserVacuum_inHg:F1}",
                engine.condenserVacuum_inHg >= 25f
                    ? UITKDashboardTheme.NormalGreen
                    : engine.condenserVacuum_inHg >= 15f
                        ? UITKDashboardTheme.WarningAmber
                        : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_backpress",
                $"{engine.condenserBackpressure_psia:F2}",
                UITKDashboardTheme.InfoCyan);
            SetCondMetric("cond_pulldown",
                engine.condenserPulldownPhase ?? "OFF",
                UITKDashboardTheme.InfoCyan);
            SetCondMetric("cond_c9_status",
                engine.condenserC9Available ? "SATISFIED" : "NOT AVAILABLE",
                engine.condenserC9Available
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);

            SetCondMetric("cond_hotwell_lvl",
                $"{engine.hotwellLevel_pct:F1}",
                UITKDashboardTheme.InfoCyan);
            SetCondMetric("cond_fw_return",
                $"{engine.feedwaterReturnFlow_lbhr:F0}",
                engine.feedwaterReturnFlow_lbhr > 100f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_cst_level",
                $"{engine.cstLevel_pct:F1}",
                UITKDashboardTheme.InfoCyan);

            // ── Steam Dump metrics ──────────────────────────────────────
            string sdMode = engine.steamDumpState.Mode.ToString();
            SetCondMetric("cond_sd_mode", sdMode,
                sdMode == "OFF"
                    ? UITKDashboardTheme.TextSecondary
                    : UITKDashboardTheme.NormalGreen);
            SetCondMetric("cond_sd_heat",
                $"{engine.steamDumpHeat_MW:F2}",
                engine.steamDumpActive
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_sd_steam_p",
                $"{engine.steamPressure_psig:F0}",
                engine.steamPressure_psig > 100f
                    ? UITKDashboardTheme.InfoCyan
                    : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_sd_status",
                engine.GetSteamDumpStatus(),
                engine.steamDumpActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_net_plant",
                $"{engine.netPlantHeat_MW:F2}",
                engine.netPlantHeat_MW > 1f
                    ? UITKDashboardTheme.WarningAmber
                    : engine.netPlantHeat_MW < -1f
                        ? UITKDashboardTheme.AccentBlue
                        : UITKDashboardTheme.NormalGreen);

            // ── HZP metrics ─────────────────────────────────────────────
            SetCondMetric("cond_hzp_state",
                engine.GetHZPStatusString(),
                engine.hzpStable
                    ? UITKDashboardTheme.NormalGreen
                    : engine.IsHZPActive()
                        ? UITKDashboardTheme.WarningAmber
                        : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_hzp_progress",
                $"{engine.hzpProgress:F0}",
                engine.hzpProgress >= 100f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.InfoCyan);
            SetCondMetric("cond_hzp_detail",
                engine.GetHZPDetailedStatus(),
                UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_htr_pid_out",
                $"{engine.heaterPIDOutput * 100f:F1}%",
                engine.heaterPIDActive
                    ? UITKDashboardTheme.InfoCyan
                    : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_htr_pid_status",
                engine.GetHeaterPIDStatus(),
                engine.heaterPIDActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetCondMetric("cond_net_heat_mw",
                $"{engine.netPlantHeat_MW:F2}",
                UITKDashboardTheme.InfoCyan);

            // ── Startup prerequisites ───────────────────────────────────
            var prereqs = engine.GetStartupReadiness();
            SetPrereqMetric("cond_prereq_temp", prereqs.TemperatureOK);
            SetPrereqMetric("cond_prereq_press", prereqs.PressureOK);
            SetPrereqMetric("cond_prereq_level", prereqs.LevelOK);
            SetPrereqMetric("cond_prereq_rcps", prereqs.RCPsOK);
            SetPrereqMetric("cond_prereq_boron", prereqs.BoronOK);
            SetCondMetric("cond_prereq_all",
                prereqs.AllMet ? "ALL MET" : "NOT MET",
                prereqs.AllMet
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed);

            // ── Permissives ─────────────────────────────────────────────
            SetCondMetric("cond_bridge_state",
                engine.steamDumpBridgeState ?? "--",
                UITKDashboardTheme.InfoCyan);
            SetCondMetric("cond_perm_c9",
                engine.condenserC9Available ? "YES" : "NO",
                engine.condenserC9Available
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed);

            bool p12Blocking = engine.permissiveState.P12_Blocking;
            SetCondMetric("cond_perm_p12",
                p12Blocking ? "BLOCKING" : "CLEAR",
                p12Blocking
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.NormalGreen);
            SetCondMetric("cond_perm_p12_byp",
                engine.p12BypassCommanded ? "ENGAGED" : "NORMAL",
                engine.p12BypassCommanded
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);

            bool dumpModeSelected = engine.steamDumpState.Mode != SteamDumpMode.OFF;
            SetCondMetric("cond_perm_dump_sel",
                dumpModeSelected ? "YES" : "NO",
                dumpModeSelected
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);

            bool steamPressOK = engine.steamPressure_psig > 50f;
            SetCondMetric("cond_perm_steam_p",
                steamPressOK ? "OK" : "LOW",
                steamPressOK
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);

            SetCondMetric("cond_perm_final",
                engine.steamDumpPermitted ? "PERMITTED" : "BLOCKED",
                engine.steamDumpPermitted
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed);
            SetCondMetric("cond_perm_status",
                engine.permissiveStatusMessage ?? "--",
                UITKDashboardTheme.TextSecondary);

            // ── Strip chart ─────────────────────────────────────────────
            if (_cond_chart != null)
            {
                _cond_chart.AddValue(_cond_traceVacuum,
                    engine.condenserVacuum_inHg);
                _cond_chart.AddValue(_cond_traceSteamDumpHeat,
                    engine.steamDumpHeat_MW);
                _cond_chart.AddValue(_cond_traceHotwellLevel,
                    engine.hotwellLevel_pct);
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private void SetCondMetric(string key, string text, Color color)
        {
            if (_cond_metricValues.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        private void SetPrereqMetric(string key, bool met)
        {
            SetCondMetric(key,
                met ? "YES" : "NO",
                met ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.AlarmRed);
        }
    }
}
