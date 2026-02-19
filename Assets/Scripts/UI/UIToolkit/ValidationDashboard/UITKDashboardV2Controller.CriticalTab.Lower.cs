// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.CriticalTab.Lower.cs — CRITICAL Tab Lower Half
// ============================================================================
//
// PURPOSE:
//   Builds the lower half of the CRITICAL tab (Tab 0):
//     - Process Meter Deck:
//       · 3× EdgewiseMeter (Press Rate, Heatup Rate, Subcooling)
//       · 2× TankLevel (VCT, Hotwell)
//       · 1× BidirectionalGauge (Net CVCS flow)
//       · 5× LinearGauge (SG Heat, Charging, Letdown, HZP Progress, Mass Error)
//     - PZR Vessel Detail:
//       · Animated PressurizerVesselPOC with phase and flow visualization
//       · Compact critical PZR metrics (level, pressure, temp, heater/spray state)
//
// DATA BINDING:
//   RefreshCriticalTabLower() is called from RefreshActiveTabData()
//   at 5Hz when tab 0 is active.
//
// IP: IP-0060 Stage 3
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
        // CRITICAL TAB — LOWER HALF REFERENCES
        // ====================================================================

        // Edgewise meters
        private EdgewiseMeterElement _crit_edgePressRate;
        private EdgewiseMeterElement _crit_edgeHeatup;
        private EdgewiseMeterElement _crit_edgeSubcool;

        // Tanks
        private TankLevelPOC _crit_tankVct;
        private TankLevelPOC _crit_tankHotwell;

        // Bidirectional gauge
        private BidirectionalGaugePOC _crit_cvcsNet;

        // Linear gauges
        private LinearGaugePOC _crit_linSgHeat;
        private LinearGaugePOC _crit_linCharging;
        private LinearGaugePOC _crit_linLetdown;
        private LinearGaugePOC _crit_linHzp;
        private LinearGaugePOC _crit_linMass;

        // Pressurizer vessel panel
        private PressurizerVesselPOC _crit_pzrVessel;
        private readonly Dictionary<string, Label> _crit_pzrMetricValues =
            new Dictionary<string, Label>(12);

        // Process value labels (readouts next to linear gauges, tanks, edge meters)
        private readonly Dictionary<string, Label> _crit_processLabels =
            new Dictionary<string, Label>(16);

        // Ops log (hosted under annunciator wall in upper section; refreshed here)
        private ScrollView _crit_logScroll;
        private int _crit_lastLogCount = -1;

        // ====================================================================
        // BUILD — Appended to _crit_tabRoot by BuildCriticalTab()
        // ====================================================================

        /// <summary>
        /// Build the lower-half row of the CRITICAL tab. Called from
        /// BuildCriticalTab() after the upper half is constructed.
        /// </summary>
        private VisualElement BuildCriticalTabLower()
        {
            // Lower split: process deck on the left, detailed PZR vessel on the right.
            var bottomRow = MakeRow(1.45f);
            bottomRow.style.minHeight = 0f;
            bottomRow.style.flexShrink = 0f;

            var processDeck = BuildProcessMeterDeck();
            processDeck.style.flexGrow = 1.6f;
            processDeck.style.flexBasis = 0f;
            processDeck.style.flexShrink = 1f;
            processDeck.style.minWidth = 0f;
            bottomRow.Add(processDeck);

            var pzrPanel = BuildCriticalPzrVesselPanel();
            pzrPanel.style.flexGrow = 0.85f;
            pzrPanel.style.flexBasis = 0f;
            pzrPanel.style.flexShrink = 1f;
            pzrPanel.style.minWidth = 320f;
            pzrPanel.style.maxWidth = new Length(46f, LengthUnit.Percent);
            pzrPanel.style.marginLeft = 10f;
            bottomRow.Add(pzrPanel);

            return bottomRow;
        }

        // ====================================================================
        // PROCESS METER DECK (center)
        // ====================================================================

        private VisualElement BuildProcessMeterDeck()
        {
            var panel = MakePanel("PROCESS METER DECK");
            panel.style.flexGrow = 1f;
            panel.style.minWidth = 0f;

            // ── Edgewise meters row ─────────────────────────────────────
            var edgeRow = MakeRow();
            edgeRow.style.justifyContent = Justify.SpaceBetween;
            edgeRow.style.marginBottom = 12f;

            edgeRow.Add(MakeEdgeMeterCard("edge_press_rate", "PRESS RATE", "psi/hr",
                -300f, 300f, out _crit_edgePressRate));
            edgeRow.Add(MakeEdgeMeterCard("edge_heatup", "HEATUP", "F/hr",
                -120f, 120f, out _crit_edgeHeatup));
            edgeRow.Add(MakeEdgeMeterCard("edge_subcool", "SUBCOOL", "F",
                0f, 400f, out _crit_edgeSubcool));

            panel.Add(edgeRow);

            // ── Tank level row ──────────────────────────────────────────
            var tankRow = MakeRow();
            tankRow.style.justifyContent = Justify.SpaceBetween;
            tankRow.style.marginBottom = 8f;

            tankRow.Add(MakeTankCard("tank_vct", "VCT",
                0f, 100f, 20f, 80f, out _crit_tankVct));
            tankRow.Add(MakeTankCard("tank_hotwell", "HOTWELL",
                0f, 100f, 20f, 80f, out _crit_tankHotwell));

            panel.Add(tankRow);

            // ── CVCS net flow bidirectional gauge ────────────────────────
            var netLabel = MakeLabel("NET CVCS FLOW (+CHG / -LTD)", 11f,
                FontStyle.Bold, new Color(0.80f, 0.90f, 0.98f, 1f));
            netLabel.style.marginTop = 2f;
            netLabel.style.marginBottom = 6f;
            panel.Add(netLabel);

            _crit_cvcsNet = new BidirectionalGaugePOC
            {
                minValue = -140f,
                maxValue = 140f
            };
            _crit_cvcsNet.style.height = 18f;
            _crit_cvcsNet.style.marginBottom = 8f;
            panel.Add(_crit_cvcsNet);

            // ── Linear gauge metrics ────────────────────────────────────
            panel.Add(MakeLinearGaugeRow("lin_sg_heat", "SG HEAT",
                0f, 120f, 80f, 100f, out _crit_linSgHeat));
            panel.Add(MakeLinearGaugeRow("lin_charging", "CHARGING",
                0f, 220f, 150f, 190f, out _crit_linCharging));
            panel.Add(MakeLinearGaugeRow("lin_letdown", "LETDOWN",
                0f, 220f, 150f, 190f, out _crit_linLetdown));
            panel.Add(MakeLinearGaugeRow("lin_hzp", "HZP PROGRESS",
                0f, 100f, 80f, 95f, out _crit_linHzp));
            panel.Add(MakeLinearGaugeRow("lin_mass", "MASS ERROR",
                0f, 1000f, 300f, 600f, out _crit_linMass));

            return panel;
        }

        private VisualElement BuildCriticalPzrVesselPanel()
        {
            var panel = MakePanel("PZR VESSEL");
            panel.style.minWidth = 0f;

            _crit_pzrVessel = new PressurizerVesselPOC
            {
                level = 100f,
                levelSetpoint = 100f,
                liquidTemperature = 120f,
                pressure = 115f,
                pressureTarget = 2249.7f,
                heaterPower = 0f,
                sprayActive = false,
                showBubbleZone = false,
                surgeFlow = 0f
            };
            _crit_pzrVessel.style.flexGrow = 1f;
            _crit_pzrVessel.style.minHeight = 320f;
            _crit_pzrVessel.style.marginBottom = 8f;
            panel.Add(_crit_pzrVessel);

            var metricsRow = MakeRow();
            metricsRow.style.justifyContent = Justify.SpaceBetween;
            metricsRow.style.alignItems = Align.FlexStart;
            metricsRow.style.marginTop = 2f;

            var leftCol = new VisualElement();
            leftCol.style.width = new Length(49.2f, LengthUnit.Percent);
            leftCol.style.flexDirection = FlexDirection.Column;
            leftCol.Add(MakeCriticalPzrMetric("pzr_level", "LEVEL", "%"));
            leftCol.Add(MakeCriticalPzrMetric("pzr_setpoint", "SETPOINT", "%"));
            leftCol.Add(MakeCriticalPzrMetric("pzr_temp", "TEMP", "F"));
            leftCol.Add(MakeCriticalPzrMetric("pzr_pressure", "PRESS", "psia"));

            var rightCol = new VisualElement();
            rightCol.style.width = new Length(49.2f, LengthUnit.Percent);
            rightCol.style.flexDirection = FlexDirection.Column;
            rightCol.Add(MakeCriticalPzrMetric("pzr_phase", "PHASE", ""));
            rightCol.Add(MakeCriticalPzrMetric("pzr_heater", "HEATER", "%"));
            rightCol.Add(MakeCriticalPzrMetric("pzr_spray", "SPRAY", ""));
            rightCol.Add(MakeCriticalPzrMetric("pzr_flows", "CVCS", "gpm"));

            metricsRow.Add(leftCol);
            metricsRow.Add(rightCol);
            panel.Add(metricsRow);

            return panel;
        }

        // ====================================================================
        // ELEMENT FACTORIES — Edgewise Cards, Tank Cards, Linear Gauge Rows
        // ====================================================================

        /// <summary>Edgewise meter in a dark card (~32% width).</summary>
        private VisualElement MakeEdgeMeterCard(
            string key, string title, string unit,
            float min, float max,
            out EdgewiseMeterElement meter)
        {
            var card = new VisualElement();
            card.style.width = new Length(32.2f, LengthUnit.Percent);
            card.style.minHeight = 96f;
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(card, 4f);
            SetBorder(card, 1f, new Color(0.173f, 0.247f, 0.373f, 1f));
            card.style.paddingTop = 4f;
            card.style.paddingBottom = 4f;
            card.style.paddingLeft = 4f;
            card.style.paddingRight = 4f;

            var titleLbl = MakeLabel(title, 10f, FontStyle.Bold,
                new Color(0.73f, 0.8f, 0.9f, 1f));
            titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(titleLbl);

            meter = new EdgewiseMeterElement
            {
                Orientation = MeterOrientation.Horizontal,
                MinValue = min,
                MaxValue = max,
                CenterZero = min < 0f && max > 0f,
                Unit = unit,
                Title = string.Empty,
                MajorTickInterval = Mathf.Max(5f, (max - min) / 4f),
                MinorTicksPerMajor = 1
            };
            meter.style.width = new Length(100f, LengthUnit.Percent);
            meter.style.height = 72f;
            card.Add(meter);

            var valueLbl = MakeLabel("--", 12f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            valueLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLbl.style.marginTop = 2f;
            card.Add(valueLbl);
            _crit_processLabels[key] = valueLbl;

            return card;
        }

        /// <summary>Tank level in a dark card (~49% width).</summary>
        private VisualElement MakeTankCard(
            string key, string title,
            float min, float max, float lowAlarm, float highAlarm,
            out TankLevelPOC tank)
        {
            var card = new VisualElement();
            card.style.width = new Length(49f, LengthUnit.Percent);
            card.style.minHeight = 124f;
            card.style.alignItems = Align.Center;
            card.style.paddingTop = 4f;
            card.style.paddingBottom = 4f;
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(card, 4f);

            var titleLbl = MakeLabel(title, 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            card.Add(titleLbl);

            tank = new TankLevelPOC
            {
                minValue = min,
                maxValue = max,
                lowAlarm = lowAlarm,
                highAlarm = highAlarm
            };
            tank.style.width = 62f;
            tank.style.height = 86f;
            tank.style.marginTop = 4f;
            card.Add(tank);

            var valueLbl = MakeLabel("--", 10f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            valueLbl.style.marginTop = 3f;
            card.Add(valueLbl);
            _crit_processLabels[key] = valueLbl;

            return card;
        }

        /// <summary>Linear gauge row with left label, center bar, and right value.</summary>
        private VisualElement MakeLinearGaugeRow(
            string key, string title,
            float min, float max, float warning, float alarm,
            out LinearGaugePOC gauge)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 8f;
            row.style.minHeight = 24f;
            row.style.paddingLeft = 2f;
            row.style.paddingRight = 2f;

            var titleLbl = MakeLabel(title, 11f, FontStyle.Bold,
                new Color(0.80f, 0.90f, 0.98f, 1f));
            titleLbl.style.width = 116f;
            titleLbl.style.marginRight = 8f;
            titleLbl.style.whiteSpace = WhiteSpace.NoWrap;
            titleLbl.style.overflow = Overflow.Hidden;
            row.Add(titleLbl);

            var barHost = new VisualElement();
            barHost.style.flexGrow = 1f;
            barHost.style.minWidth = 140f;
            barHost.style.marginRight = 10f;
            barHost.style.marginTop = 1f;
            row.Add(barHost);

            var valueLbl = MakeLabel("--", 12f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            valueLbl.style.width = 108f;
            valueLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(valueLbl);
            _crit_processLabels[key] = valueLbl;

            // Gauge bar
            gauge = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = min,
                maxValue = max,
                warningThreshold = warning,
                alarmThreshold = alarm
            };
            gauge.style.height = 15f;
            barHost.Add(gauge);

            return row;
        }

        private VisualElement MakeCriticalPzrMetric(string key, string title, string unit)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 19f;
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

            var valueLbl = MakeLabel("--", 10f, FontStyle.Bold, UITKDashboardTheme.InfoCyan);
            row.Add(valueLbl);
            _crit_pzrMetricValues[key] = valueLbl;

            return row;
        }

        // ====================================================================
        // DATA REFRESH — CRITICAL TAB LOWER HALF (5Hz)
        // ====================================================================

        /// <summary>Refresh all lower-half CRITICAL tab elements.</summary>
        private void RefreshCriticalTabLower()
        {
            if (engine == null) return;

            float netCvcs = engine.chargingFlow - engine.letdownFlow;
            float hzpPct = Mathf.Clamp(engine.hzpProgress, 0f, 100f);
            float massErr = engine.massError_lbm;
            float absPressRate = Mathf.Abs(engine.pressureRate);
            float absMassErr = Mathf.Abs(massErr);

            // ── Color decisions ─────────────────────────────────────────
            Color pressRateColor = absPressRate < 100f
                ? UITKDashboardTheme.NormalGreen
                : absPressRate < 200f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.AlarmRed;

            Color vctColor = (engine.vctState.Level < 20f || engine.vctState.Level > 80f)
                ? UITKDashboardTheme.WarningAmber
                : UITKDashboardTheme.NormalGreen;

            Color massColor = absMassErr < 100f
                ? UITKDashboardTheme.NormalGreen
                : absMassErr < 500f
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.AlarmRed;

            // ── Edgewise meters ─────────────────────────────────────────
            if (_crit_edgePressRate != null) _crit_edgePressRate.Value = engine.pressureRate;
            if (_crit_edgeHeatup != null) _crit_edgeHeatup.Value = engine.heatupRate;
            if (_crit_edgeSubcool != null) _crit_edgeSubcool.Value = engine.subcooling;

            SetProcessLabel("edge_press_rate", $"{engine.pressureRate:F1}", pressRateColor);
            SetProcessLabel("edge_heatup", $"{engine.heatupRate:F1}",
                UITKDashboardTheme.InfoCyan);
            SetProcessLabel("edge_subcool", $"{engine.subcooling:F1}",
                engine.subcoolingLow
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen);

            // ── Tanks ───────────────────────────────────────────────────
            if (_crit_tankVct != null) _crit_tankVct.value = engine.vctState.Level;
            if (_crit_tankHotwell != null) _crit_tankHotwell.value = engine.hotwellLevel_pct;

            SetProcessLabel("tank_vct", $"{engine.vctState.Level:F1}%", vctColor);
            SetProcessLabel("tank_hotwell", $"{engine.hotwellLevel_pct:F1}%",
                UITKDashboardTheme.InfoCyan);

            // ── CVCS net ────────────────────────────────────────────────
            if (_crit_cvcsNet != null) _crit_cvcsNet.value = netCvcs;

            // ── Linear gauges ───────────────────────────────────────────
            if (_crit_linSgHeat != null) _crit_linSgHeat.value = engine.sgHeatTransfer_MW;
            if (_crit_linCharging != null) _crit_linCharging.value = engine.chargingFlow;
            if (_crit_linLetdown != null) _crit_linLetdown.value = engine.letdownFlow;
            if (_crit_linHzp != null) _crit_linHzp.value = hzpPct;
            if (_crit_linMass != null) _crit_linMass.value = absMassErr;

            SetProcessLabel("lin_sg_heat", $"{engine.sgHeatTransfer_MW:F2} MW",
                engine.sgHeatTransfer_MW > 0.1f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.TextSecondary);
            SetProcessLabel("lin_charging", $"{engine.chargingFlow:F1} gpm",
                UITKDashboardTheme.NormalGreen);
            SetProcessLabel("lin_letdown", $"{engine.letdownFlow:F1} gpm",
                UITKDashboardTheme.WarningAmber);
            SetProcessLabel("lin_hzp", $"{hzpPct:F1}%",
                UITKDashboardTheme.InfoCyan);
            SetProcessLabel("lin_mass", $"{massErr:F1} lbm", massColor);

            // Use the active setpoint from the simulator so arrow motion tracks
            // real control state throughout startup transitions.
            float pzrSetpointDisplay = engine.pzrLevelSetpointDisplay;
            float heaterPct = Mathf.Clamp01(engine.pzrHeaterPower / 1.8f) * 100f;
            if (_crit_pzrVessel != null)
            {
                _crit_pzrVessel.level = engine.pzrLevel;
                _crit_pzrVessel.levelSetpoint = pzrSetpointDisplay;
                _crit_pzrVessel.pressure = engine.pressure;
                _crit_pzrVessel.pressureTarget = engine.targetPressure + PlantConstants.PSIG_TO_PSIA;
                _crit_pzrVessel.liquidTemperature = engine.T_pzr;
                _crit_pzrVessel.heaterPower = heaterPct;
                _crit_pzrVessel.sprayActive = engine.sprayActive;
                _crit_pzrVessel.showBubbleZone = !engine.solidPressurizer;
                _crit_pzrVessel.chargingFlow = engine.chargingFlow;
                _crit_pzrVessel.letdownFlow = engine.letdownFlow;
                _crit_pzrVessel.surgeFlow = engine.surgeFlow;
            }

            string pzrPhase = engine.solidPressurizer
                ? "SOLID"
                : engine.bubbleFormed ? "TWO-PHASE" : "TRANSITION";
            Color pzrPhaseColor = engine.solidPressurizer
                ? UITKDashboardTheme.WarningAmber
                : engine.bubbleFormed
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed;

            SetCriticalPzrMetric("pzr_level", $"{engine.pzrLevel:F1}%",
                engine.pzrLevelLow || engine.pzrLevelHigh
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.InfoCyan);
            SetCriticalPzrMetric("pzr_setpoint", $"{pzrSetpointDisplay:F1}%",
                UITKDashboardTheme.TextSecondary);
            SetCriticalPzrMetric("pzr_temp", $"{engine.T_pzr:F1}", TempColor(engine.T_pzr));
            SetCriticalPzrMetric("pzr_pressure", $"{engine.pressure:F0}",
                engine.pressureLow || engine.pressureHigh
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.InfoCyan);
            SetCriticalPzrMetric("pzr_phase", pzrPhase, pzrPhaseColor);
            SetCriticalPzrMetric("pzr_heater", $"{heaterPct:F0}%",
                engine.pzrHeatersOn
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.TextSecondary);
            SetCriticalPzrMetric("pzr_spray", engine.sprayActive ? "ON" : "OFF",
                engine.sprayActive
                    ? UITKDashboardTheme.AccentBlue
                    : UITKDashboardTheme.TextSecondary);
            SetCriticalPzrMetric("pzr_flows",
                $"C {engine.chargingFlow:F1} / L {engine.letdownFlow:F1}",
                netCvcs >= 0f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.WarningAmber);


            // ── Event log ──────────────────────────────────────
            RefreshCriticalEventLog();
        }

        // ====================================================================
        // EVENT LOG
        // ====================================================================

        private void RefreshCriticalEventLog()
        {
            if (_crit_logScroll == null || engine == null) return;
            if (_crit_lastLogCount == engine.eventLog.Count) return;

            _crit_lastLogCount = engine.eventLog.Count;
            _crit_logScroll.Clear();

            const int MAX_ENTRIES = 28;
            int start = Mathf.Max(0, engine.eventLog.Count - MAX_ENTRIES);
            for (int i = start; i < engine.eventLog.Count; i++)
            {
                var line = MakeLabel(engine.eventLog[i].FormattedLine, 10f,
                    FontStyle.Normal, UITKDashboardTheme.TextSecondary);
                line.style.whiteSpace = WhiteSpace.NoWrap;
                line.style.overflow = Overflow.Hidden;
                _crit_logScroll.Add(line);
            }

            // Auto-scroll to bottom
            _crit_logScroll.schedule.Execute(() =>
            {
                if (_crit_logScroll.verticalScroller != null)
                    _crit_logScroll.verticalScroller.value =
                        _crit_logScroll.verticalScroller.highValue;
            }).StartingIn(5);
        }

        // ====================================================================
        // HELPERS — Process label setter
        // ====================================================================

        private void SetProcessLabel(string key, string text, Color color)
        {
            if (_crit_processLabels.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        private void SetCriticalPzrMetric(string key, string text, Color color)
        {
            if (_crit_pzrMetricValues.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        private void TickCriticalLowerAnimations(float dt)
        {
            _crit_pzrVessel?.UpdateFlowAnimation(dt);
        }
    }
}

