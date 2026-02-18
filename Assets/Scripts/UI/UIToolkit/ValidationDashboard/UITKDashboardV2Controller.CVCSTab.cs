// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.CVCSTab.cs — CVCS Tab Builder (Tab 3)
// ============================================================================
//
// PURPOSE:
//   Builds the CVCS Tab (Tab 3) — Chemical & Volume Control System:
//     - Left: Flow balance panel (charging, letdown, seal, surge, orifice lineup)
//     - Center: VCT + BRS inventory panels with TankLevel graphics
//     - Right: PI controller status, mass conservation, boron tracking
//     - Bottom: CVCS strip chart (charging, letdown, VCT level)
//
// DATA BINDING:
//   RefreshCVCSTab() at 5Hz when tab 3 is active.
//
// IP: IP-0060 Stage 6
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
        // CVCS TAB — REFERENCES
        // ====================================================================

        private readonly Dictionary<string, Label> _cvcs_metricValues =
            new Dictionary<string, Label>(48);

        // Tank graphics
        private TankLevelPOC _cvcs_vctTank;
        private TankLevelPOC _cvcs_brsTank;

        // LEDs
        private LEDIndicatorElement _cvcs_ledCharging;
        private LEDIndicatorElement _cvcs_ledLetdown;
        private LEDIndicatorElement _cvcs_ledDivert;
        private LEDIndicatorElement _cvcs_ledMakeup;
        private LEDIndicatorElement _cvcs_ledSealOK;

        // Charging/Letdown balance bar (bidirectional)
        private BidirectionalGaugePOC _cvcs_netFlowGauge;
        private Label _cvcs_netFlowLabel;

        // Strip chart
        private StripChartPOC _cvcs_chart;
        private int _cvcs_traceCharging = -1;
        private int _cvcs_traceLetdown = -1;
        private int _cvcs_traceVCT = -1;

        // ====================================================================
        // BUILD
        // ====================================================================

        private VisualElement BuildCVCSTab()
        {
            var root = MakeTabRoot("v2-tab-cvcs");

            // ── Top row: Flows + Inventory + Controller ─────────────────
            var topRow = MakeRow(2f);
            topRow.style.minHeight = 0f;

            topRow.Add(BuildCVCSFlowPanel());
            topRow.Add(BuildCVCSInventoryPanel());
            topRow.Add(BuildCVCSControllerPanel());

            root.Add(topRow);

            // ── Bottom: Strip chart ─────────────────────────────────────
            var bottomRow = MakeRow(1.1f);
            bottomRow.style.minHeight = 0f;
            bottomRow.Add(BuildCVCSStripChart());
            root.Add(bottomRow);

            return root;
        }

        // ====================================================================
        // LEFT — Flow Balance Panel
        // ====================================================================

        private VisualElement BuildCVCSFlowPanel()
        {
            var panel = MakePanel("CVCS FLOW BALANCE");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            // ── LED status row ──────────────────────────────────────────
            var ledRow = MakeRow();
            ledRow.style.justifyContent = Justify.SpaceAround;
            ledRow.style.marginBottom = 6f;

            _cvcs_ledCharging = new LEDIndicatorElement();
            _cvcs_ledCharging.Configure("CHG", new Color(0.18f, 0.85f, 0.25f, 1f), false);
            _allLEDs.Add(_cvcs_ledCharging);

            _cvcs_ledLetdown = new LEDIndicatorElement();
            _cvcs_ledLetdown.Configure("LTD", new Color(1f, 0.78f, 0f, 1f), false);
            _allLEDs.Add(_cvcs_ledLetdown);

            _cvcs_ledSealOK = new LEDIndicatorElement();
            _cvcs_ledSealOK.Configure("SEAL", new Color(0.3f, 0.7f, 1f, 1f), false);
            _allLEDs.Add(_cvcs_ledSealOK);

            ledRow.Add(_cvcs_ledCharging);
            ledRow.Add(_cvcs_ledLetdown);
            ledRow.Add(_cvcs_ledSealOK);
            panel.Add(ledRow);

            // ── Flow metrics ────────────────────────────────────────────
            panel.Add(MakeCVCSMetric("cvcs_charging", "CHARGING", "gpm"));
            panel.Add(MakeCVCSMetric("cvcs_letdown", "LETDOWN", "gpm"));
            panel.Add(MakeCVCSMetric("cvcs_chg_to_rcs", "CHG→RCS", "gpm"));
            panel.Add(MakeCVCSMetric("cvcs_ccp_total", "CCP OUTPUT", "gpm"));
            panel.Add(MakeCVCSMetric("cvcs_surge", "SURGE FLOW", "gpm"));
            panel.Add(MakeCVCSMetric("cvcs_seal_inj", "SEAL INJ", "gpm"));

            // ── Net flow balance (bidirectional) ────────────────────────
            var netLabel = MakeLabel("NET CVCS FLOW", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            netLabel.style.marginTop = 6f;
            panel.Add(netLabel);

            _cvcs_netFlowGauge = new BidirectionalGaugePOC
            {
                minValue = -140f,
                maxValue = 140f
            };
            _cvcs_netFlowGauge.style.height = 20f;
            panel.Add(_cvcs_netFlowGauge);

            _cvcs_netFlowLabel = MakeLabel("0.0 gpm", 10f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            _cvcs_netFlowLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _cvcs_netFlowLabel.style.marginTop = 2f;
            panel.Add(_cvcs_netFlowLabel);

            // ── Letdown path / orifice lineup ───────────────────────────
            panel.Add(MakeCVCSMetric("cvcs_ltd_path", "LTD PATH", ""));
            panel.Add(MakeCVCSMetric("cvcs_orifice", "ORIFICE", ""));
            panel.Add(MakeCVCSMetric("cvcs_orifice_flow", "ORIFICE FLOW", "gpm"));
            panel.Add(MakeCVCSMetric("cvcs_rhr_ltd_flow", "RHR LTD", "gpm"));

            // ── Thermal mixing ──────────────────────────────────────────
            panel.Add(MakeCVCSMetric("cvcs_therm_mix", "THERMAL MIX", "MW"));
            panel.Add(MakeCVCSMetric("cvcs_therm_dt", "MIXING ΔT", "°F"));

            return panel;
        }

        // ====================================================================
        // CENTER — VCT + BRS Inventory
        // ====================================================================

        private VisualElement BuildCVCSInventoryPanel()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1.1f;
            container.style.minWidth = 0f;
            container.style.flexDirection = FlexDirection.Column;

            // ── VCT Panel ───────────────────────────────────────────────
            var vctPanel = MakePanel("VOLUME CONTROL TANK");
            vctPanel.style.flexGrow = 1f;

            // VCT LEDs
            var vctLedRow = MakeRow();
            vctLedRow.style.justifyContent = Justify.SpaceAround;
            vctLedRow.style.marginBottom = 4f;

            _cvcs_ledDivert = new LEDIndicatorElement();
            _cvcs_ledDivert.Configure("DIVERT", new Color(1f, 0.55f, 0f, 1f), false);
            _allLEDs.Add(_cvcs_ledDivert);

            _cvcs_ledMakeup = new LEDIndicatorElement();
            _cvcs_ledMakeup.Configure("MAKEUP", new Color(0.3f, 0.7f, 1f, 1f), false);
            _allLEDs.Add(_cvcs_ledMakeup);

            vctLedRow.Add(_cvcs_ledDivert);
            vctLedRow.Add(_cvcs_ledMakeup);
            vctPanel.Add(vctLedRow);

            // VCT tank graphic + metrics
            var vctRow = MakeRow();
            vctRow.style.flexGrow = 1f;

            _cvcs_vctTank = new TankLevelPOC
            {
                minValue = 0f, maxValue = 100f,
                lowAlarm = 20f, highAlarm = 80f
            };
            _cvcs_vctTank.style.width = 70f;
            _cvcs_vctTank.style.flexGrow = 0f;
            _cvcs_vctTank.style.minHeight = 100f;
            vctRow.Add(_cvcs_vctTank);

            var vctMetrics = new VisualElement();
            vctMetrics.style.flexGrow = 1f;
            vctMetrics.style.marginLeft = 6f;
            vctMetrics.Add(MakeCVCSMetric("cvcs_vct_level", "LEVEL", "%"));
            vctMetrics.Add(MakeCVCSMetric("cvcs_vct_volume", "VOLUME", "gal"));
            vctMetrics.Add(MakeCVCSMetric("cvcs_vct_boron", "BORON", "ppm"));
            vctMetrics.Add(MakeCVCSMetric("cvcs_vct_status", "STATUS", ""));
            vctRow.Add(vctMetrics);

            vctPanel.Add(vctRow);
            container.Add(vctPanel);

            // ── BRS Panel ───────────────────────────────────────────────
            var brsPanel = MakePanel("BORON RECYCLE SYSTEM");
            brsPanel.style.flexGrow = 0.8f;

            var brsRow = MakeRow();
            brsRow.style.flexGrow = 1f;

            _cvcs_brsTank = new TankLevelPOC
            {
                minValue = 0f, maxValue = 100f,
                lowAlarm = 10f, highAlarm = 90f
            };
            _cvcs_brsTank.style.width = 70f;
            _cvcs_brsTank.style.flexGrow = 0f;
            _cvcs_brsTank.style.minHeight = 80f;
            brsRow.Add(_cvcs_brsTank);

            var brsMetrics = new VisualElement();
            brsMetrics.style.flexGrow = 1f;
            brsMetrics.style.marginLeft = 6f;
            brsMetrics.Add(MakeCVCSMetric("cvcs_brs_holdup", "HOLDUP", "gal"));
            brsMetrics.Add(MakeCVCSMetric("cvcs_brs_level", "LEVEL", "%"));
            brsMetrics.Add(MakeCVCSMetric("cvcs_brs_status", "STATUS", ""));
            brsMetrics.Add(MakeCVCSMetric("cvcs_brs_distil", "DISTILLATE", "gal"));
            brsRow.Add(brsMetrics);

            brsPanel.Add(brsRow);
            container.Add(brsPanel);

            return container;
        }

        // ====================================================================
        // RIGHT — PI Controller Status + Mass Conservation
        // ====================================================================

        private VisualElement BuildCVCSControllerPanel()
        {
            var container = new VisualElement();
            container.style.flexGrow = 0.95f;
            container.style.minWidth = 0f;
            container.style.flexDirection = FlexDirection.Column;

            // ── PI Controller ───────────────────────────────────────────
            var piPanel = MakePanel("PI LEVEL CONTROLLER");
            piPanel.style.flexGrow = 1f;

            piPanel.Add(MakeCVCSMetric("cvcs_pi_setpoint", "SETPOINT", "%"));
            piPanel.Add(MakeCVCSMetric("cvcs_pi_error", "INTEGRAL ERR", ""));
            piPanel.Add(MakeCVCSMetric("cvcs_pi_divert", "DIVERT FRAC", ""));
            piPanel.Add(MakeCVCSMetric("cvcs_pi_mode", "CTRL MODE", ""));
            piPanel.Add(MakeCVCSMetric("cvcs_ltd_isolated", "LTD ISOLATED", ""));

            container.Add(piPanel);

            // ── Mass Conservation ────────────────────────────────────────
            var massPanel = MakePanel("MASS CONSERVATION");
            massPanel.style.flexGrow = 1f;

            massPanel.Add(MakeCVCSMetric("cvcs_mass_total", "TOTAL MASS", "lbm"));
            massPanel.Add(MakeCVCSMetric("cvcs_mass_error", "MASS ERROR", "lbm"));
            massPanel.Add(MakeCVCSMetric("cvcs_mass_drift", "DRIFT", "%"));
            massPanel.Add(MakeCVCSMetric("cvcs_mass_status", "STATUS", ""));
            massPanel.Add(MakeCVCSMetric("cvcs_ext_net", "EXT NET", "gal"));

            container.Add(massPanel);

            // ── Boron Tracking ──────────────────────────────────────────
            var boronPanel = MakePanel("BORON");
            boronPanel.style.flexGrow = 0f;

            boronPanel.Add(MakeCVCSMetric("cvcs_rcs_boron", "RCS BORON", "ppm"));

            container.Add(boronPanel);

            return container;
        }

        // ====================================================================
        // BOTTOM — Strip Chart
        // ====================================================================

        private VisualElement BuildCVCSStripChart()
        {
            var panel = MakePanel("CVCS FLOW TRENDS");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            _cvcs_chart = new StripChartPOC();
            _cvcs_chart.style.flexGrow = 1f;
            _cvcs_chart.style.minHeight = 160f;

            _cvcs_traceCharging = _cvcs_chart.AddTrace("CHARGING",
                new Color(0.18f, 0.85f, 0.25f, 1f), 0f, 220f);
            _cvcs_traceLetdown = _cvcs_chart.AddTrace("LETDOWN",
                new Color(1f, 0.78f, 0f, 1f), 0f, 220f);
            _cvcs_traceVCT = _cvcs_chart.AddTrace("VCT %",
                new Color(0.3f, 0.7f, 1f, 1f), 0f, 100f);

            panel.Add(_cvcs_chart);

            return panel;
        }

        // ====================================================================
        // METRIC FACTORY
        // ====================================================================

        private VisualElement MakeCVCSMetric(string key, string title, string unit)
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
            _cvcs_metricValues[key] = valueLbl;

            return row;
        }

        // ====================================================================
        // DATA REFRESH — CVCS TAB (5Hz)
        // ====================================================================

        private void RefreshCVCSTab()
        {
            if (engine == null) return;

            // ── LEDs ────────────────────────────────────────────────────
            if (_cvcs_ledCharging != null) _cvcs_ledCharging.isOn = engine.chargingActive;
            if (_cvcs_ledLetdown != null) _cvcs_ledLetdown.isOn = engine.letdownActive;
            if (_cvcs_ledSealOK != null) _cvcs_ledSealOK.isOn = engine.sealInjectionOK;
            if (_cvcs_ledDivert != null) _cvcs_ledDivert.isOn = engine.vctDivertActive;
            if (_cvcs_ledMakeup != null) _cvcs_ledMakeup.isOn = engine.vctMakeupActive;

            // ── Flow metrics ────────────────────────────────────────────
            SetCVCSMetric("cvcs_charging", $"{engine.chargingFlow:F1}",
                engine.chargingActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetCVCSMetric("cvcs_letdown", $"{engine.letdownFlow:F1}",
                engine.letdownActive
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetCVCSMetric("cvcs_chg_to_rcs", $"{engine.chargingToRCS:F1}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_ccp_total", $"{engine.totalCCPOutput:F1}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_surge", $"{engine.surgeFlow:F1}",
                Mathf.Abs(engine.surgeFlow) > 50f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.InfoCyan);

            float sealInj = engine.rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            SetCVCSMetric("cvcs_seal_inj", $"{sealInj:F1}",
                engine.sealInjectionOK
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed);

            // ── Net flow balance ────────────────────────────────────────
            float netCVCS = engine.chargingFlow - engine.letdownFlow;
            if (_cvcs_netFlowGauge != null) _cvcs_netFlowGauge.value = netCVCS;
            if (_cvcs_netFlowLabel != null)
            {
                string sign = netCVCS >= 0f ? "+" : "";
                _cvcs_netFlowLabel.text = $"{sign}{netCVCS:F1} gpm";
                _cvcs_netFlowLabel.style.color = netCVCS > 5f
                    ? UITKDashboardTheme.NormalGreen
                    : netCVCS < -5f
                        ? UITKDashboardTheme.AccentBlue
                        : UITKDashboardTheme.TextSecondary;
            }

            // ── Letdown path / orifice ──────────────────────────────────
            string ltdPath = engine.letdownViaRHR ? "RHR" :
                engine.letdownViaOrifice ? "ORIFICE" : "ISOLATED";
            SetCVCSMetric("cvcs_ltd_path", ltdPath,
                engine.letdownIsolatedFlag
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.NormalGreen);
            SetCVCSMetric("cvcs_orifice", engine.orificeLineupDesc,
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_orifice_flow", $"{engine.orificeLetdownFlow:F1}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_rhr_ltd_flow", $"{engine.rhrLetdownFlow:F1}",
                engine.letdownViaRHR
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);

            // ── Thermal mixing ──────────────────────────────────────────
            SetCVCSMetric("cvcs_therm_mix", $"{engine.cvcsThermalMixing_MW:F4}",
                Mathf.Abs(engine.cvcsThermalMixing_MW) > 0.01f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetCVCSMetric("cvcs_therm_dt", $"{engine.cvcsThermalMixingDeltaF:F1}",
                UITKDashboardTheme.InfoCyan);

            // ── VCT ─────────────────────────────────────────────────────
            float vctLevel = engine.vctState.Level_percent;
            if (_cvcs_vctTank != null) _cvcs_vctTank.value = vctLevel;

            SetCVCSMetric("cvcs_vct_level", $"{vctLevel:F1}",
                engine.vctLevelLow ? UITKDashboardTheme.AlarmRed
                : engine.vctLevelHigh ? UITKDashboardTheme.AlarmRed
                : UITKDashboardTheme.NormalGreen);
            SetCVCSMetric("cvcs_vct_volume", $"{engine.vctState.Volume_gal:F0}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_vct_boron",
                $"{engine.vctState.BoronConcentration_ppm:F0}",
                UITKDashboardTheme.InfoCyan);

            string vctStatus = engine.vctDivertActive ? "DIVERTING"
                : engine.vctMakeupActive ? "MAKEUP"
                : engine.vctRWSTSuction ? "RWST SUCTION"
                : "NORMAL";
            Color vctStatusColor = engine.vctDivertActive
                ? UITKDashboardTheme.WarningAmber
                : engine.vctMakeupActive
                    ? UITKDashboardTheme.AccentBlue
                    : UITKDashboardTheme.NormalGreen;
            SetCVCSMetric("cvcs_vct_status", vctStatus, vctStatusColor);

            // ── BRS ─────────────────────────────────────────────────────
            float brsLevel = BRSPhysics.GetHoldupLevelPercent(engine.brsState);
            if (_cvcs_brsTank != null) _cvcs_brsTank.value = brsLevel;

            SetCVCSMetric("cvcs_brs_holdup",
                $"{engine.brsState.HoldupVolume_gal:F0}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_brs_level", $"{brsLevel:F1}",
                engine.brsState.HoldupHighLevel
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.NormalGreen);
            SetCVCSMetric("cvcs_brs_status",
                BRSPhysics.GetStatusString(engine.brsState),
                engine.brsState.ProcessingActive
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetCVCSMetric("cvcs_brs_distil",
                $"{engine.brsState.DistillateAvailable_gal:F0}",
                UITKDashboardTheme.AccentBlue);

            // ── PI controller ───────────────────────────────────────────
            SetCVCSMetric("cvcs_pi_setpoint",
                $"{engine.pzrLevelSetpointDisplay:F1}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_pi_error",
                $"{engine.cvcsIntegralError:F3}",
                Mathf.Abs(engine.cvcsIntegralError) > 10f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);
            SetCVCSMetric("cvcs_pi_divert",
                $"{engine.divertFraction * 100f:F1}%",
                engine.divertFraction > 0.01f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);

            string ctrlMode = engine.solidPressurizer ? "SOLID" :
                engine.letdownIsolatedFlag ? "ISOLATED" : "PI AUTO";
            SetCVCSMetric("cvcs_pi_mode", ctrlMode,
                ctrlMode == "PI AUTO"
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);
            SetCVCSMetric("cvcs_ltd_isolated",
                engine.letdownIsolatedFlag ? "YES" : "NO",
                engine.letdownIsolatedFlag
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.NormalGreen);

            // ── Mass conservation ────────────────────────────────────────
            SetCVCSMetric("cvcs_mass_total",
                $"{engine.totalSystemMass_lbm:F0}",
                UITKDashboardTheme.InfoCyan);
            SetCVCSMetric("cvcs_mass_error",
                $"{engine.massError_lbm:F1}",
                engine.primaryMassAlarm
                    ? UITKDashboardTheme.AlarmRed
                    : engine.massError_lbm > 300f
                        ? UITKDashboardTheme.WarningAmber
                        : UITKDashboardTheme.NormalGreen);
            SetCVCSMetric("cvcs_mass_drift",
                $"{engine.primaryMassDrift_pct:F3}",
                Mathf.Abs(engine.primaryMassDrift_pct) > 0.1f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);
            SetCVCSMetric("cvcs_mass_status", engine.primaryMassStatus,
                engine.primaryMassConservationOK
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed);
            SetCVCSMetric("cvcs_ext_net",
                $"{engine.plantExternalNet_gal:F1}",
                UITKDashboardTheme.TextSecondary);

            // ── Boron ───────────────────────────────────────────────────
            SetCVCSMetric("cvcs_rcs_boron",
                $"{engine.rcsBoronConcentration:F0}",
                UITKDashboardTheme.InfoCyan);

            // ── Strip chart ─────────────────────────────────────────────
            if (_cvcs_chart != null)
            {
                _cvcs_chart.AddValue(_cvcs_traceCharging, engine.chargingFlow);
                _cvcs_chart.AddValue(_cvcs_traceLetdown, engine.letdownFlow);
                _cvcs_chart.AddValue(_cvcs_traceVCT,
                    engine.vctState.Level_percent);
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private void SetCVCSMetric(string key, string text, Color color)
        {
            if (_cvcs_metricValues.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }
    }
}
