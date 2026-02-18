// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.CriticalTab.Lower.cs — CRITICAL Tab Lower Half
// ============================================================================
//
// PURPOSE:
//   Builds the lower half of the CRITICAL tab (Tab 0):
//     - Strip Chart trends (T_avg, Pressure, PZR Level — 3 traces)
//     - Process Meter Deck:
//       · 3× EdgewiseMeter (Press Rate, Heatup Rate, Subcooling)
//       · 2× TankLevel (VCT, Hotwell)
//       · 1× BidirectionalGauge (Net CVCS flow)
//       · 5× LinearGauge (SG Heat, Charging, Letdown, HZP Progress, Mass Error)
//     - Compact Ops Log (alarm summary + last 28 event entries)
//
// DATA BINDING:
//   RefreshCriticalTabLower() is called from RefreshActiveTabData()
//   at 5Hz when tab 0 is active. Strip chart traces are fed new values
//   each refresh cycle.
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

        // Strip chart
        private StripChartPOC _crit_chart;
        private int _crit_traceTemp = -1;
        private int _crit_tracePressure = -1;
        private int _crit_traceLevel = -1;

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

        // Process value labels (readouts next to linear gauges, tanks, edge meters)
        private readonly Dictionary<string, Label> _crit_processLabels =
            new Dictionary<string, Label>(16);

        // Ops log
        private Label _crit_alarmSummary;
        private readonly List<Label> _crit_alarmRows = new List<Label>(4);
        private readonly List<string> _crit_alarmMessages = new List<string>(16);
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
            // Slightly reduce lower-row share so upper gauge deck has room for all 3 rows.
            var bottomRow = MakeRow(1.25f);
            bottomRow.style.minHeight = 0f;

            bottomRow.Add(BuildStripTrendsPanel());
            bottomRow.Add(BuildProcessMeterDeck());
            bottomRow.Add(BuildOpsLogPanel());

            return bottomRow;
        }

        // ====================================================================
        // STRIP TRENDS PANEL (left)
        // ====================================================================

        private VisualElement BuildStripTrendsPanel()
        {
            var panel = MakePanel("STRIP TRENDS");
            panel.style.flexGrow = 1.2f;
            panel.style.minWidth = 0f;

            _crit_chart = new StripChartPOC();
            _crit_chart.style.flexGrow = 1f;
            _crit_chart.style.minHeight = 260f;

            _crit_traceTemp = _crit_chart.AddTrace("T AVG",
                new Color(0f, 0.9f, 0.45f, 1f), 70f, 600f);
            _crit_tracePressure = _crit_chart.AddTrace("PRESS",
                new Color(1f, 0.75f, 0f, 1f), 0f, 2600f);
            _crit_traceLevel = _crit_chart.AddTrace("PZR %",
                new Color(0.3f, 0.8f, 1f, 1f), 0f, 100f);

            panel.Add(_crit_chart);
            return panel;
        }

        // ====================================================================
        // PROCESS METER DECK (center)
        // ====================================================================

        private VisualElement BuildProcessMeterDeck()
        {
            var panel = MakePanel("PROCESS METER DECK");
            panel.style.flexGrow = 1.05f;
            panel.style.minWidth = 0f;

            // ── Edgewise meters row ─────────────────────────────────────
            var edgeRow = MakeRow();
            edgeRow.style.justifyContent = Justify.SpaceBetween;
            edgeRow.style.marginBottom = 8f;

            edgeRow.Add(MakeEdgeMeterCard("edge_press_rate", "PRESS RATE", "psi/hr",
                -300f, 300f, out _crit_edgePressRate));
            edgeRow.Add(MakeEdgeMeterCard("edge_heatup", "HEATUP", "F/hr",
                -120f, 120f, out _crit_edgeHeatup));
            edgeRow.Add(MakeEdgeMeterCard("edge_subcool", "SUBCOOL", "F",
                0f, 120f, out _crit_edgeSubcool));

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
            var netLabel = MakeLabel("NET CVCS (+ charging / - letdown)", 9f,
                FontStyle.Normal, new Color(0.63f, 0.72f, 0.84f, 1f));
            netLabel.style.marginBottom = 2f;
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

        // ====================================================================
        // OPS LOG PANEL (right)
        // ====================================================================

        private VisualElement BuildOpsLogPanel()
        {
            var panel = MakePanel("OPS LOG");
            panel.style.flexGrow = 0.95f;
            panel.style.minWidth = 0f;

            // Alarm summary line
            _crit_alarmSummary = MakeLabel("No active alarms", 11f,
                FontStyle.Bold, UITKDashboardTheme.NormalGreen);
            _crit_alarmSummary.style.marginBottom = 4f;
            panel.Add(_crit_alarmSummary);

            // 4 dedicated alarm slot rows
            _crit_alarmRows.Clear();
            for (int i = 0; i < 4; i++)
            {
                var row = MakeLabel("--", 10f, FontStyle.Normal,
                    UITKDashboardTheme.TextSecondary);
                row.style.marginBottom = 1f;
                _crit_alarmRows.Add(row);
                panel.Add(row);
            }

            // Scrollable event log
            _crit_logScroll = new ScrollView(ScrollViewMode.Vertical);
            _crit_logScroll.style.flexGrow = 1f;
            _crit_logScroll.style.marginTop = 6f;
            _crit_logScroll.style.backgroundColor = new Color(0.032f, 0.043f, 0.07f, 1f);
            SetCornerRadius(_crit_logScroll, 4f);
            _crit_logScroll.style.paddingLeft = 4f;
            _crit_logScroll.style.paddingRight = 4f;
            _crit_logScroll.style.paddingTop = 2f;
            _crit_logScroll.style.paddingBottom = 2f;
            panel.Add(_crit_logScroll);

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
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(card, 4f);
            card.style.paddingTop = 4f;
            card.style.paddingBottom = 4f;
            card.style.paddingLeft = 4f;
            card.style.paddingRight = 4f;

            meter = new EdgewiseMeterElement
            {
                Orientation = MeterOrientation.Horizontal,
                MinValue = min,
                MaxValue = max,
                CenterZero = min < 0f && max > 0f,
                Unit = unit,
                Title = title,
                MajorTickInterval = Mathf.Max(5f, (max - min) / 4f),
                MinorTicksPerMajor = 1
            };
            meter.style.width = new Length(100f, LengthUnit.Percent);
            meter.style.height = 56f;
            card.Add(meter);

            var valueLbl = MakeLabel("--", 10f, FontStyle.Bold,
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

        /// <summary>Linear gauge with title/value header and horizontal bar.</summary>
        private VisualElement MakeLinearGaugeRow(
            string key, string title,
            float min, float max, float warning, float alarm,
            out LinearGaugePOC gauge)
        {
            var row = new VisualElement();
            row.style.marginBottom = 7f;

            // Header: title (left) + value (right)
            var header = MakeRow();
            header.style.justifyContent = Justify.SpaceBetween;

            var titleLbl = MakeLabel(title, 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            header.Add(titleLbl);

            var valueLbl = MakeLabel("--", 10f, FontStyle.Bold,
                UITKDashboardTheme.TextPrimary);
            header.Add(valueLbl);
            _crit_processLabels[key] = valueLbl;

            row.Add(header);

            // Gauge bar
            gauge = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = min,
                maxValue = max,
                warningThreshold = warning,
                alarmThreshold = alarm
            };
            gauge.style.height = 14f;
            gauge.style.marginTop = 2f;
            row.Add(gauge);

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

            // ── Strip chart ─────────────────────────────────────────────
            if (_crit_chart != null)
            {
                _crit_chart.AddValue(_crit_traceTemp, engine.T_avg);
                _crit_chart.AddValue(_crit_tracePressure, engine.pressure);
                _crit_chart.AddValue(_crit_traceLevel, engine.pzrLevel);
            }

            // ── Alarms + Event log ──────────────────────────────────────
            RefreshCriticalAlarms();
            RefreshCriticalEventLog();
        }

        // ====================================================================
        // ALARM SUMMARY
        // ====================================================================

        private void RefreshCriticalAlarms()
        {
            _crit_alarmMessages.Clear();

            if (engine.pressureLow) _crit_alarmMessages.Add("RCS PRESSURE LOW");
            if (engine.pressureHigh) _crit_alarmMessages.Add("RCS PRESSURE HIGH");
            if (engine.pzrLevelLow) _crit_alarmMessages.Add("PZR LEVEL LOW");
            if (engine.pzrLevelHigh) _crit_alarmMessages.Add("PZR LEVEL HIGH");
            if (engine.subcoolingLow) _crit_alarmMessages.Add("SUBCOOLING LOW");
            if (engine.vctLevelLow) _crit_alarmMessages.Add("VCT LEVEL LOW");
            if (engine.vctLevelHigh) _crit_alarmMessages.Add("VCT LEVEL HIGH");
            if (engine.rcsFlowLow) _crit_alarmMessages.Add("RCS FLOW LOW");
            if (Mathf.Abs(engine.massConservationError) > 500f)
                _crit_alarmMessages.Add("MASS CONSERVATION ALERT");
            if (!engine.modePermissive)
                _crit_alarmMessages.Add("MODE PERMISSIVE BLOCKED");

            if (_crit_alarmSummary != null)
            {
                int count = _crit_alarmMessages.Count;
                _crit_alarmSummary.text = count == 0
                    ? "No active alarms"
                    : $"{count} active alarm(s)";
                _crit_alarmSummary.style.color = count == 0
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AlarmRed;
            }

            for (int i = 0; i < _crit_alarmRows.Count; i++)
            {
                if (i < _crit_alarmMessages.Count)
                {
                    _crit_alarmRows[i].text = _crit_alarmMessages[i];
                    _crit_alarmRows[i].style.color = UITKDashboardTheme.AlarmRed;
                }
                else
                {
                    _crit_alarmRows[i].text = "--";
                    _crit_alarmRows[i].style.color = UITKDashboardTheme.TextSecondary;
                }
            }
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
    }
}
