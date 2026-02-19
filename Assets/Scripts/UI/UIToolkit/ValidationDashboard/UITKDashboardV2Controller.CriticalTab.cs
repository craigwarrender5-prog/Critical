// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.CriticalTab.cs — CRITICAL Tab Builder (Upper)
// ============================================================================
//
// PURPOSE:
//   Builds the upper half of the CRITICAL tab (Tab 0) — the "Core Flight
//   Deck" for instant situational awareness. Contains:
//     - 6-card primary instrument deck:
//       T_avg, PZR/RCS Pressure, PZR Level (tank), PZR Temp, Subcooling, SG Pressure
//     - 3x10 Annunciator Wall with INFO / WARNING / ALARM tones
//     - 4× RCP LED status row
//     - Plant status banner (Mode, Phase, PZR State, Steam Dump)
//
//   The lower half (Process Meters) is built
//   in UITKDashboardV2Controller.CriticalTab.Lower.cs (Stage 3).
//
// DATA BINDING:
//   RefreshCriticalTabUpper() is called from RefreshActiveTabData()
//   at 5Hz when tab 0 is active. Annunciator flash runs every frame.
//
// IP: IP-0060 Stage 2
// VERSION: 6.0.0
// DATE: 2026-02-18
// ============================================================================

using System;
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
        // CRITICAL TAB — UPPER HALF REFERENCES
        // ====================================================================

        // Analog gauge cards
        private AnalogGaugeElement _crit_gaugeTAvg;
        private AnalogGaugeElement _crit_gaugePressure;
        private AnalogGaugeElement _crit_gaugePressRate;
        private AnalogGaugeElement _crit_gaugePzrTemp;
        private AnalogGaugeElement _crit_gaugeSubcool;
        private AnalogGaugeElement _crit_gaugeSgPressure;
        private const float PRESS_RATE_ABS_MAX = 300f;
        private const float PRESS_RATE_ABS_WARN = 100f;
        private const float PRESS_RATE_ABS_ALARM = 200f;
        private const float HEAT_RATE_ABS_MAX = 120f;
        private const float HEAT_RATE_ABS_WARN = 50f;
        private const float HEAT_RATE_ABS_ALARM = 80f;

        private sealed class GaugeTrendBinding
        {
            public TrendArrowPOC Arrow;
            public Label Readout;
            public float WarnAbs;
            public float AlarmAbs;
        }

        private readonly Dictionary<string, GaugeTrendBinding> _crit_gaugeTrendBindings =
            new Dictionary<string, GaugeTrendBinding>(6);

        // Digital readouts beneath each gauge card
        private readonly Dictionary<string, Label> _crit_gaugeReadouts =
            new Dictionary<string, Label>(12);

        // ── Annunciator system ──────────────────────────────────────────
        private enum AnnunciatorTone { Info, Warning, Alarm }

        private sealed class AnnunciatorBinding
        {
            public VisualElement Root;
            public Label Text;
            public AnnunciatorTone Tone;
            public Func<HeatupSimEngine, bool> Condition;
            public bool IsActive;
            public bool IsAcknowledged;
        }

        private readonly List<AnnunciatorBinding> _crit_annBindings =
            new List<AnnunciatorBinding>(32);
        private float _crit_annFlashTimer;
        private bool _crit_annFlashOn = true;

        // ── Upper half root (so lower-half builder can append below it) ──
        private VisualElement _crit_tabRoot;
        private VisualElement _crit_upperRow;

        // ====================================================================
        // BUILD — Called from BuildTabContents() (replaces placeholder)
        // ====================================================================

        /// <summary>
        /// Build the complete CRITICAL tab content. Stage 2 builds the upper
        /// half; Stage 3 will append the lower half to _crit_tabRoot.
        /// </summary>
        private VisualElement BuildCriticalTab()
        {
            _crit_tabRoot = MakeTabRoot("v2-tab-critical");

            // ── Upper row: Gauge Cluster (left) + Annunciator Wall (right) ──
            // Give the upper "Core Flight Deck" more height so the lower deck
            // does not get occluded by the LED/status strips on typical 16:9 layouts.
            _crit_upperRow = MakeRow(2.05f);
            _crit_upperRow.style.minHeight = 0f;

            _crit_upperRow.Add(BuildCoreFlightDeck());
            _crit_upperRow.Add(BuildAnnunciatorWall());

            _crit_tabRoot.Add(_crit_upperRow);

            // Lower half — Process Meter Deck
            _crit_tabRoot.Add(BuildCriticalTabLower());

            return _crit_tabRoot;
        }

        // ====================================================================
        // CORE FLIGHT DECK — Left panel with primary instruments + LEDs + status
        // ====================================================================

        private VisualElement BuildCoreFlightDeck()
        {
            var panel = MakePanel("CORE FLIGHT DECK");
            panel.style.flexGrow = 1.55f;
            panel.style.minWidth = 0f;
            _crit_gaugeTrendBindings.Clear();

            // ── Gauge grid (3 columns × 3 rows) ────────────────────────
            var gaugeGrid = new VisualElement();
            gaugeGrid.style.flexDirection = FlexDirection.Row;
            gaugeGrid.style.flexWrap = Wrap.Wrap;
            gaugeGrid.style.alignContent = Align.FlexStart;
            gaugeGrid.style.flexShrink = 0f;
            gaugeGrid.style.marginBottom = 4f;

            gaugeGrid.Add(MakeGaugeCard("cg_tavg", "RCS AVG TEMP", "F",
                70f, 600f, 100f, 4, out _crit_gaugeTAvg,
                trendBindingKey: "heatup_rate",
                trendMaxMagnitude: HEAT_RATE_ABS_MAX,
                trendWarnAbs: HEAT_RATE_ABS_WARN,
                trendAlarmAbs: HEAT_RATE_ABS_ALARM));
            gaugeGrid.Add(MakeGaugeCard("cg_pressure", "PZR / RCS PRESSURE", "PSIA",
                0f, 2600f, 500f, 4, out _crit_gaugePressure,
                trendBindingKey: "pressure_rate",
                trendMaxMagnitude: PRESS_RATE_ABS_MAX,
                trendWarnAbs: PRESS_RATE_ABS_WARN,
                trendAlarmAbs: PRESS_RATE_ABS_ALARM));
            gaugeGrid.Add(MakeGaugeCard("cg_press_rate", "PZR PRESS RATE", "PSI/HR",
                -PRESS_RATE_ABS_MAX, PRESS_RATE_ABS_MAX, 100f, 2, out _crit_gaugePressRate));
            gaugeGrid.Add(MakeGaugeCard("cg_pzr_temp", "PZR TEMP", "F",
                50f, 700f, 100f, 4, out _crit_gaugePzrTemp));
            gaugeGrid.Add(MakeGaugeCard("cg_subcool", "SUBCOOLING", "F",
                0f, 400f, 50f, 4, out _crit_gaugeSubcool));
            gaugeGrid.Add(MakeGaugeCard("cg_sg_pressure", "SG PRESSURE", "PSIA",
                0f, 1400f, 200f, 4, out _crit_gaugeSgPressure));

            panel.Add(gaugeGrid);

            return panel;
        }

        // ====================================================================
        // ANNUNCIATOR WALL — Right panel with 3×10 tile matrix
        // ====================================================================

        private VisualElement BuildAnnunciatorWall()
        {
            var panel = MakePanel("ANNUNCIATOR WALL");
            panel.style.flexGrow = 1.15f;
            panel.style.minWidth = 0f;

            _crit_annBindings.Clear();
            _crit_annFlashTimer = 0f;
            _crit_annFlashOn = true;

            // ── Legend row ───────────────────────────────────────────────
            var legend = MakeRow();
            legend.style.justifyContent = Justify.FlexEnd;
            legend.style.marginBottom = 6f;
            legend.Add(MakeLegendPill("INFO", AnnunciatorTone.Info));
            legend.Add(MakeLegendPill("WARNING", AnnunciatorTone.Warning));
            legend.Add(MakeLegendPill("ALARM", AnnunciatorTone.Alarm));
            panel.Add(legend);

            // ── Tile matrix container ───────────────────────────────────
            var matrix = new VisualElement();
            matrix.style.flexDirection = FlexDirection.Column;
            matrix.style.flexGrow = 1f;
            matrix.style.backgroundColor = new Color(0.035f, 0.051f, 0.086f, 1f);
            matrix.style.borderTopWidth = 1f;
            matrix.style.borderBottomWidth = 1f;
            matrix.style.borderLeftWidth = 1f;
            matrix.style.borderRightWidth = 1f;
            var borderColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            matrix.style.borderTopColor = borderColor;
            matrix.style.borderBottomColor = borderColor;
            matrix.style.borderLeftColor = borderColor;
            matrix.style.borderRightColor = borderColor;
            matrix.style.paddingTop = 6f;
            matrix.style.paddingBottom = 6f;
            matrix.style.paddingLeft = 6f;
            matrix.style.paddingRight = 6f;
            matrix.style.minHeight = 0f;
            matrix.style.flexGrow = 1f;

            // 3 rows × 10 columns
            var rows = new VisualElement[3];
            for (int r = 0; r < 3; r++)
            {
                rows[r] = new VisualElement();
                rows[r].style.flexDirection = FlexDirection.Row;
                rows[r].style.justifyContent = Justify.SpaceBetween;
                rows[r].style.marginBottom = r < 2 ? 2f : 0f;
                matrix.Add(rows[r]);
            }

            // Tile index tracker — tiles fill left-to-right, row by row
            int tileIdx = 0;
            void Tile(string label, AnnunciatorTone tone, Func<HeatupSimEngine, bool> cond)
            {
                int row = Mathf.Clamp(tileIdx / 10, 0, 2);
                AddAnnunciatorTile(rows[row], label, tone, cond);
                tileIdx++;
            }

            // ── Row 0 (tiles 0-9) ──────────────────────────────────────
            Tile("PZR HTRS\nON",       AnnunciatorTone.Info,    e => e.pzrHeatersOn);
            Tile("HEATUP\nIN PROG",    AnnunciatorTone.Info,    e => e.heatupInProgress);
            Tile("STEAM\nBUBBLE OK",   AnnunciatorTone.Info,    e => e.steamBubbleOK);
            Tile("MODE\nPERMISSIVE",   AnnunciatorTone.Info,    e => e.modePermissive);
            Tile("PRESS\nLOW",         AnnunciatorTone.Alarm,   e => e.pressureLow);
            Tile("PRESS\nHIGH",        AnnunciatorTone.Alarm,   e => e.pressureHigh);
            Tile("SUBCOOL\nLOW",       AnnunciatorTone.Alarm,   e => e.subcoolingLow);
            Tile("RCS FLOW\nLOW",      AnnunciatorTone.Alarm,   e => e.rcsFlowLow);
            Tile("PZR LVL\nLOW",       AnnunciatorTone.Alarm,   e => e.pzrLevelLow);
            Tile("PZR LVL\nHIGH",      AnnunciatorTone.Warning, e => e.pzrLevelHigh);

            // ── Row 1 (tiles 10-19) ─────────────────────────────────────
            Tile("RVLIS\nLOW",         AnnunciatorTone.Alarm,   e => e.rvlisLevelLow);
            Tile("CCW\nRUNNING",       AnnunciatorTone.Info,    e => e.ccwRunning);
            Tile("CHARGING\nACTIVE",   AnnunciatorTone.Info,    e => e.chargingActive);
            Tile("LETDOWN\nACTIVE",    AnnunciatorTone.Info,    e => e.letdownActive);
            Tile("SEAL INJ\nOK",       AnnunciatorTone.Info,    e => e.sealInjectionOK);
            Tile("VCT LVL\nLOW",       AnnunciatorTone.Alarm,   e => e.vctLevelLow);
            Tile("VCT LVL\nHIGH",      AnnunciatorTone.Warning, e => e.vctLevelHigh);
            Tile("VCT\nDIVERT",        AnnunciatorTone.Info,    e => e.vctDivertActive);
            Tile("VCT\nMAKEUP",        AnnunciatorTone.Info,    e => e.vctMakeupActive);
            Tile("RWST\nSUCTION",      AnnunciatorTone.Alarm,   e => e.vctRWSTSuction);

            // ── Row 2 (tiles 20-29) ─────────────────────────────────────
            Tile("RCP #1\nRUN",        AnnunciatorTone.Info,    e => RcpRunning(e, 0));
            Tile("RCP #2\nRUN",        AnnunciatorTone.Info,    e => RcpRunning(e, 1));
            Tile("RCP #3\nRUN",        AnnunciatorTone.Info,    e => RcpRunning(e, 2));
            Tile("RCP #4\nRUN",        AnnunciatorTone.Info,    e => RcpRunning(e, 3));
            Tile("SMM LOW\nMARGIN",    AnnunciatorTone.Warning, e => e.smmLowMargin);
            Tile("SMM NO\nMARGIN",     AnnunciatorTone.Alarm,   e => e.smmNoMargin);
            Tile("SG PRESS\nHIGH",     AnnunciatorTone.Alarm,   e => e.sgSecondaryPressureHigh);

            // Fill remaining tiles as spares
            while (tileIdx < 30)
                Tile("SPARE", AnnunciatorTone.Info, _ => false);

            // Use lower unused wall space for a compact, scrollable ops log.
            var logHost = new VisualElement();
            logHost.style.flexDirection = FlexDirection.Column;
            logHost.style.flexGrow = 1f;
            logHost.style.minHeight = 0f;
            logHost.style.marginTop = 8f;

            var logTitle = MakeLabel("OPS LOG", 9f, FontStyle.Bold,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            logTitle.style.marginBottom = 4f;
            logHost.Add(logTitle);

            _crit_logScroll = new ScrollView(ScrollViewMode.Vertical);
            _crit_logScroll.style.flexGrow = 1f;
            _crit_logScroll.style.minHeight = 0f;
            _crit_logScroll.style.backgroundColor = new Color(0.032f, 0.043f, 0.07f, 1f);
            SetCornerRadius(_crit_logScroll, 4f);
            _crit_logScroll.style.paddingLeft = 4f;
            _crit_logScroll.style.paddingRight = 4f;
            _crit_logScroll.style.paddingTop = 2f;
            _crit_logScroll.style.paddingBottom = 2f;
            logHost.Add(_crit_logScroll);
            _crit_lastLogCount = -1;

            matrix.Add(logHost);
            panel.Add(matrix);

            return panel;
        }

        // ====================================================================
        // ELEMENT FACTORIES — Gauge Cards, LEDs, Status Metrics, Tiles
        // ====================================================================

        /// <summary>
        /// Creates a gauge card: title label + AnalogGaugeElement + digital readout.
        /// Width is ~32% so 3 cards fill a row.
        /// </summary>
        private VisualElement MakeGaugeCard(
            string key, string title, string unit,
            float min, float max, float majorTick, int minorTicks,
            out AnalogGaugeElement gauge,
            string trendBindingKey = null,
            float trendMaxMagnitude = 0f,
            float trendWarnAbs = 0f,
            float trendAlarmAbs = 0f)
        {
            var card = new VisualElement();
            card.style.width = new Length(32.2f, LengthUnit.Percent);
            card.style.marginRight = 4f;
            card.style.marginBottom = 4f;
            card.style.paddingTop = 3f;
            card.style.paddingBottom = 3f;
            card.style.paddingLeft = 3f;
            card.style.paddingRight = 3f;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(card, 4f);
            SetBorder(card, 1f, new Color(0.118f, 0.173f, 0.271f, 1f));

            // Title
            var titleLbl = MakeLabel(title, 9f, FontStyle.Bold,
                new Color(0.73f, 0.8f, 0.9f, 1f));
            titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(titleLbl);

            // Gauge
            gauge = new AnalogGaugeElement
            {
                Title = string.Empty,
                Unit = unit,
                MinValue = min,
                MaxValue = max,
                MajorTickInterval = majorTick,
                MinorTicksPerMajor = minorTicks,
                Value = min
            };
            gauge.style.width = 96f;
            gauge.style.height = 96f;
            gauge.style.minWidth = 96f;
            gauge.style.minHeight = 96f;
            gauge.style.maxWidth = 96f;
            gauge.style.maxHeight = 96f;
            gauge.style.flexGrow = 0f;
            gauge.style.flexShrink = 0f;

            if (!string.IsNullOrEmpty(trendBindingKey))
            {
                var gaugeRow = new VisualElement();
                gaugeRow.style.flexDirection = FlexDirection.Row;
                gaugeRow.style.alignItems = Align.Center;
                gaugeRow.style.justifyContent = Justify.Center;
                gaugeRow.style.marginTop = 1f;
                gaugeRow.Add(gauge);

                var rateCol = new VisualElement();
                rateCol.style.flexDirection = FlexDirection.Column;
                rateCol.style.alignItems = Align.Center;
                rateCol.style.justifyContent = Justify.Center;
                rateCol.style.marginLeft = 6f;
                rateCol.style.width = 42f;

                var arrowHost = new VisualElement();
                arrowHost.style.width = 18f;
                arrowHost.style.height = 62f;
                arrowHost.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
                SetCornerRadius(arrowHost, 2f);

                var trendArrow = new TrendArrowPOC
                {
                    maxMagnitude = Mathf.Max(0.001f, trendMaxMagnitude),
                    deadband = 0.5f,
                    elevatedThreshold = trendWarnAbs,
                    alarmThreshold = trendAlarmAbs,
                    value = 0f
                };
                trendArrow.style.width = new Length(100f, LengthUnit.Percent);
                trendArrow.style.height = new Length(100f, LengthUnit.Percent);
                arrowHost.Add(trendArrow);
                rateCol.Add(arrowHost);

                var trendReadout = MakeLabel("0.0", 9f, FontStyle.Bold, UITKDashboardTheme.NormalGreen);
                trendReadout.style.unityTextAlign = TextAnchor.MiddleCenter;
                trendReadout.style.marginTop = 2f;
                trendReadout.style.whiteSpace = WhiteSpace.NoWrap;
                rateCol.Add(trendReadout);

                gaugeRow.Add(rateCol);
                card.Add(gaugeRow);

                RegisterGaugeTrendBinding(
                    trendBindingKey, trendArrow, trendReadout, trendWarnAbs, trendAlarmAbs);
            }
            else
            {
                card.Add(gauge);
            }

            // Digital readout
            var readout = MakeLabel("--", 12f, FontStyle.Bold, UITKDashboardTheme.InfoCyan);
            readout.style.marginTop = 2f;
            readout.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(readout);

            _crit_gaugeReadouts[key] = readout;

            return card;
        }

        private void RegisterGaugeTrendBinding(
            string bindingKey,
            TrendArrowPOC arrow,
            Label readout,
            float warnAbs,
            float alarmAbs)
        {
            _crit_gaugeTrendBindings[bindingKey] = new GaugeTrendBinding
            {
                Arrow = arrow,
                Readout = readout,
                WarnAbs = warnAbs,
                AlarmAbs = alarmAbs
            };
        }

        /// <summary>
        /// Creates a tank-style card for inventory-like parameters (for example PZR level).
        /// Width is ~32% so 3 cards fill a row.
        /// </summary>
        private VisualElement MakeTankGaugeCard(
            string key, string title,
            float min, float max, float lowAlarm, float highAlarm,
            out TankLevelPOC tank)
        {
            var card = new VisualElement();
            card.style.width = new Length(32.2f, LengthUnit.Percent);
            card.style.marginRight = 4f;
            card.style.marginBottom = 4f;
            card.style.paddingTop = 3f;
            card.style.paddingBottom = 3f;
            card.style.paddingLeft = 3f;
            card.style.paddingRight = 3f;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(card, 4f);
            SetBorder(card, 1f, new Color(0.118f, 0.173f, 0.271f, 1f));

            var titleLbl = MakeLabel(title, 9f, FontStyle.Bold,
                new Color(0.73f, 0.8f, 0.9f, 1f));
            titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(titleLbl);

            tank = new TankLevelPOC
            {
                minValue = min,
                maxValue = max,
                lowAlarm = lowAlarm,
                highAlarm = highAlarm
            };
            tank.style.width = 58f;
            tank.style.height = 82f;
            tank.style.marginTop = 4f;
            card.Add(tank);

            var readout = MakeLabel("--", 12f, FontStyle.Bold, UITKDashboardTheme.InfoCyan);
            readout.style.unityTextAlign = TextAnchor.MiddleCenter;
            readout.style.marginTop = 3f;
            card.Add(readout);
            _crit_gaugeReadouts[key] = readout;

            return card;
        }

        /// <summary>Create a single annunciator tile and register its binding.</summary>
        private void AddAnnunciatorTile(
            VisualElement parent, string label,
            AnnunciatorTone tone, Func<HeatupSimEngine, bool> condition)
        {
            var tile = new VisualElement();
            tile.style.width = new Length(9.6f, LengthUnit.Percent);
            tile.style.height = 38f;
            tile.style.minHeight = 38f;
            tile.style.alignItems = Align.Center;
            tile.style.justifyContent = Justify.Center;
            tile.style.backgroundColor = ANN_BG_OFF;
            SetBorder(tile, 1f, ANN_BORDER_OFF);

            var text = MakeLabel(label, 9f, FontStyle.Bold, ANN_TEXT_OFF);
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            text.style.whiteSpace = WhiteSpace.Normal;
            tile.Add(text);
            parent.Add(tile);

            var binding = new AnnunciatorBinding
            {
                Root = tile,
                Text = text,
                Tone = tone,
                Condition = condition,
                IsActive = false,
                IsAcknowledged = false
            };

            // Alarm-tone annunciators can be acknowledged by clicking the tile.
            tile.RegisterCallback<ClickEvent>(_ =>
            {
                if (!binding.IsActive || binding.Tone != AnnunciatorTone.Alarm)
                    return;

                binding.IsAcknowledged = true;
                ApplyTileVisual(binding, false);
            });

            _crit_annBindings.Add(binding);
        }

        /// <summary>Create a legend pill for the annunciator wall.</summary>
        private VisualElement MakeLegendPill(string text, AnnunciatorTone tone)
        {
            Color fg, bg, border;
            switch (tone)
            {
                case AnnunciatorTone.Warning:
                    fg = new Color(1f, 0.835f, 0.439f, 1f);
                    bg = new Color(0.22f, 0.133f, 0.016f, 1f);
                    border = new Color(0.769f, 0.529f, 0.075f, 1f);
                    break;
                case AnnunciatorTone.Alarm:
                    fg = new Color(1f, 0.604f, 0.604f, 1f);
                    bg = new Color(0.231f, 0.047f, 0.047f, 1f);
                    border = new Color(0.745f, 0.212f, 0.212f, 1f);
                    break;
                default: // Info
                    fg = new Color(0.569f, 1f, 0.682f, 1f);
                    bg = new Color(0.051f, 0.18f, 0.09f, 1f);
                    border = new Color(0.259f, 0.604f, 0.345f, 1f);
                    break;
            }

            var pill = MakeLabel(text, 9f, FontStyle.Bold, fg);
            pill.style.unityTextAlign = TextAnchor.MiddleCenter;
            pill.style.marginLeft = 4f;
            pill.style.paddingLeft = 6f;
            pill.style.paddingRight = 6f;
            pill.style.paddingTop = 2f;
            pill.style.paddingBottom = 2f;
            pill.style.backgroundColor = bg;
            SetBorder(pill, 1f, border);
            return pill;
        }

        // ====================================================================
        // ANNUNCIATOR COLORS
        // ====================================================================

        private static readonly Color ANN_BG_OFF = new Color(0.055f, 0.063f, 0.086f, 1f);
        private static readonly Color ANN_BORDER_OFF = new Color(0.2f, 0.224f, 0.263f, 1f);
        private static readonly Color ANN_TEXT_OFF = new Color(0.58f, 0.62f, 0.69f, 1f);

        private static readonly Color ANN_BG_INFO = new Color(0.039f, 0.149f, 0.086f, 1f);
        private static readonly Color ANN_BORDER_INFO = new Color(0.169f, 0.663f, 0.349f, 1f);
        private static readonly Color ANN_TEXT_INFO = new Color(0.376f, 0.922f, 0.533f, 1f);

        private static readonly Color ANN_BG_WARN = new Color(0.22f, 0.145f, 0.016f, 1f);
        private static readonly Color ANN_BORDER_WARN = new Color(0.769f, 0.529f, 0.075f, 1f);
        private static readonly Color ANN_TEXT_WARN = new Color(1f, 0.765f, 0.329f, 1f);

        private static readonly Color ANN_BG_ALARM = new Color(0.251f, 0.055f, 0.063f, 1f);
        private static readonly Color ANN_BORDER_ALARM = new Color(0.82f, 0.29f, 0.29f, 1f);
        private static readonly Color ANN_TEXT_ALARM = new Color(1f, 0.49f, 0.49f, 1f);

        // ====================================================================
        // DATA REFRESH — CRITICAL TAB UPPER HALF (5Hz)
        // ====================================================================

        /// <summary>Refresh all upper-half CRITICAL tab elements.</summary>
        private void RefreshCriticalTabUpper()
        {
            if (engine == null) return;


            // ── Gauge cards ─────────────────────────────────────────────
            SetGaugeCard(_crit_gaugeTAvg, "cg_tavg",
                engine.T_avg, $"{engine.T_avg:F1} F", UITKDashboardTheme.InfoCyan);
            SetGaugeTrendBindingValue("heatup_rate", engine.heatupRate);

            SetGaugeCard(_crit_gaugePressure, "cg_pressure",
                engine.pressure, $"{engine.pressure:F0} psia",
                engine.pressureHigh || engine.pressureLow
                    ? UITKDashboardTheme.AlarmRed : UITKDashboardTheme.InfoCyan);

            float absPressRate = Mathf.Abs(engine.pressureRate);
            Color pressRateGaugeColor = absPressRate > PRESS_RATE_ABS_ALARM
                ? UITKDashboardTheme.AlarmRed
                : absPressRate > PRESS_RATE_ABS_WARN
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen;
            SetGaugeCard(_crit_gaugePressRate, "cg_press_rate",
                engine.pressureRate, $"{engine.pressureRate:F1} psi/hr",
                pressRateGaugeColor);
            SetGaugeTrendBindingValue("pressure_rate", engine.pressureRate);

            SetGaugeCard(_crit_gaugePzrTemp, "cg_pzr_temp",
                engine.T_pzr, $"{engine.T_pzr:F1} F", UITKDashboardTheme.InfoCyan);

            SetGaugeCard(_crit_gaugeSubcool, "cg_subcool",
                engine.subcooling, $"{engine.subcooling:F1} F",
                engine.subcoolingLow
                    ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.NormalGreen);

            SetGaugeCard(_crit_gaugeSgPressure, "cg_sg_pressure",
                engine.sgSecondaryPressure_psia, $"{engine.sgSecondaryPressure_psia:F0} psia",
                engine.sgSecondaryPressureHigh
                    ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.InfoCyan);


            // ── Annunciator wall ────────────────────────────────────────
            RefreshAnnunciators();
        }

        // ====================================================================
        // ANNUNCIATOR REFRESH & FLASH
        // ====================================================================

        private void RefreshAnnunciators()
        {
            if (engine == null || _crit_annBindings.Count == 0) return;

            foreach (var tile in _crit_annBindings)
            {
                bool active = tile.Condition != null && tile.Condition(engine);
                if (!active)
                    tile.IsAcknowledged = false;
                else if (!tile.IsActive)
                    tile.IsAcknowledged = false;

                tile.IsActive = active;

                bool flashOff = active &&
                                tile.Tone == AnnunciatorTone.Alarm &&
                                !tile.IsAcknowledged &&
                                !_crit_annFlashOn;
                ApplyTileVisual(tile, flashOff);
            }
        }

        /// <summary>Runs every frame to flash alarm-tone annunciators.</summary>
        private void TickAnnunciatorFlash(float dt)
        {
            if (_crit_annBindings.Count == 0) return;

            _crit_annFlashTimer += dt;
            if (_crit_annFlashTimer < 0.18f) return;
            _crit_annFlashTimer = 0f;
            _crit_annFlashOn = !_crit_annFlashOn;

            foreach (var tile in _crit_annBindings)
            {
                if (tile.Tone == AnnunciatorTone.Alarm &&
                    tile.IsActive &&
                    !tile.IsAcknowledged)
                {
                    ApplyTileVisual(tile, !_crit_annFlashOn);
                }
            }
        }

        private static void ApplyTileVisual(AnnunciatorBinding tile, bool forceOff)
        {
            if (!tile.IsActive || forceOff)
            {
                tile.Root.style.backgroundColor = ANN_BG_OFF;
                SetBorder(tile.Root, 1f, ANN_BORDER_OFF);
                tile.Text.style.color = ANN_TEXT_OFF;
                return;
            }

            switch (tile.Tone)
            {
                case AnnunciatorTone.Info:
                    tile.Root.style.backgroundColor = ANN_BG_INFO;
                    SetBorder(tile.Root, 1f, ANN_BORDER_INFO);
                    tile.Text.style.color = ANN_TEXT_INFO;
                    break;
                case AnnunciatorTone.Warning:
                    tile.Root.style.backgroundColor = ANN_BG_WARN;
                    SetBorder(tile.Root, 1f, ANN_BORDER_WARN);
                    tile.Text.style.color = ANN_TEXT_WARN;
                    break;
                case AnnunciatorTone.Alarm:
                    tile.Root.style.backgroundColor = ANN_BG_ALARM;
                    SetBorder(tile.Root, 1f, ANN_BORDER_ALARM);
                    tile.Text.style.color = ANN_TEXT_ALARM;
                    break;
            }
        }

        // ====================================================================
        // HELPERS — Shared by CRITICAL tab partial classes
        // ====================================================================

        /// <summary>Set a gauge card's value and readout text/color.</summary>
        private void SetGaugeCard(AnalogGaugeElement gauge, string readoutKey,
            float value, string text, Color color)
        {
            if (gauge != null) gauge.Value = value;
            if (_crit_gaugeReadouts.TryGetValue(readoutKey, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        /// <summary>Set a tank gauge card's value and readout text/color.</summary>
        private void SetTankGaugeCard(TankLevelPOC tank, string readoutKey,
            float value, string text, Color color)
        {
            if (tank != null) tank.value = value;
            if (_crit_gaugeReadouts.TryGetValue(readoutKey, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        /// <summary>Set a gauge-side trend arrow value and matching readout color.</summary>
        private void SetGaugeTrendBindingValue(string bindingKey, float rateValue)
        {
            if (string.IsNullOrEmpty(bindingKey) ||
                !_crit_gaugeTrendBindings.TryGetValue(bindingKey, out var binding))
                return;

            if (binding.Arrow != null)
                binding.Arrow.value = rateValue;

            if (binding.Readout == null)
                return;

            float absRate = Mathf.Abs(rateValue);
            Color trendColor = absRate > binding.AlarmAbs
                ? UITKDashboardTheme.AlarmRed
                : absRate > binding.WarnAbs
                    ? UITKDashboardTheme.WarningAmber
                    : UITKDashboardTheme.NormalGreen;

            binding.Readout.text = $"{rateValue:+0.0;-0.0;0.0}";
            binding.Readout.style.color = trendColor;
        }

        /// <summary>Safe RCP running check.</summary>
        private static bool RcpRunning(HeatupSimEngine e, int idx)
        {
            return e != null && e.rcpRunning != null &&
                   e.rcpRunning.Length > idx && e.rcpRunning[idx];
        }

        // ── Style helpers (static, used by all partial classes) ─────────

        private static void SetCornerRadius(VisualElement el, float r)
        {
            el.style.borderTopLeftRadius = r;
            el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r;
            el.style.borderBottomRightRadius = r;
        }

        private static void SetBorder(VisualElement el, float width, Color color)
        {
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            el.style.borderTopColor = color;
            el.style.borderBottomColor = color;
            el.style.borderLeftColor = color;
            el.style.borderRightColor = color;
        }
    }
}
