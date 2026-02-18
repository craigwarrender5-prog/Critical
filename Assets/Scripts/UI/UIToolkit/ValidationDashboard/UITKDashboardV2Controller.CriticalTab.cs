// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.CriticalTab.cs — CRITICAL Tab Builder (Upper)
// ============================================================================
//
// PURPOSE:
//   Builds the upper half of the CRITICAL tab (Tab 0) — the "Core Flight
//   Deck" for instant situational awareness. Contains:
//     - 9× AnalogGauge cards (T_avg, Pressure, PZR Level, PZR Temp,
//       Subcooling, SG Pressure, Heatup Rate, Press Rate, Cond Vacuum)
//     - 3×10 Annunciator Wall with INFO / WARNING / ALARM tones
//     - 4× RCP LED status row
//     - Plant status banner (Mode, Phase, PZR State, Steam Dump)
//
//   The lower half (Strip Trends, Process Meters, Ops Log) is built
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
        private AnalogGaugeElement _crit_gaugePzrLevel;
        private AnalogGaugeElement _crit_gaugePzrTemp;
        private AnalogGaugeElement _crit_gaugeSubcool;
        private AnalogGaugeElement _crit_gaugeSgPressure;
        private AnalogGaugeElement _crit_gaugeHeatupRate;
        private AnalogGaugeElement _crit_gaugePressRate;
        private AnalogGaugeElement _crit_gaugeCondVac;

        // Digital readouts beneath each gauge card
        private readonly Dictionary<string, Label> _crit_gaugeReadouts =
            new Dictionary<string, Label>(12);

        // RCP LEDs
        private LEDIndicatorElement _crit_ledRcpA;
        private LEDIndicatorElement _crit_ledRcpB;
        private LEDIndicatorElement _crit_ledRcpC;
        private LEDIndicatorElement _crit_ledRcpD;

        // Status banner metric labels (Plant Mode, Heatup Phase, etc.)
        private readonly Dictionary<string, Label> _crit_statusLabels =
            new Dictionary<string, Label>(8);

        // ── Annunciator system ──────────────────────────────────────────
        private enum AnnunciatorTone { Info, Warning, Alarm }

        private sealed class AnnunciatorBinding
        {
            public VisualElement Root;
            public Label Text;
            public AnnunciatorTone Tone;
            public Func<HeatupSimEngine, bool> Condition;
            public bool IsActive;
        }

        private readonly List<AnnunciatorBinding> _crit_annBindings =
            new List<AnnunciatorBinding>(32);
        private Label _crit_annSummary;
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
            // Give the upper "Core Flight Deck" more height so the 3rd gauge row
            // does not get occluded by the LED/status strips on typical 16:9 layouts.
            _crit_upperRow = MakeRow(2.45f);
            _crit_upperRow.style.minHeight = 0f;

            _crit_upperRow.Add(BuildCoreFlightDeck());
            _crit_upperRow.Add(BuildAnnunciatorWall());

            _crit_tabRoot.Add(_crit_upperRow);

            // Lower half — Strip Trends, Process Meter Deck, Ops Log
            _crit_tabRoot.Add(BuildCriticalTabLower());

            return _crit_tabRoot;
        }

        // ====================================================================
        // CORE FLIGHT DECK — Left panel with 9 gauge cards + LEDs + status
        // ====================================================================

        private VisualElement BuildCoreFlightDeck()
        {
            var panel = MakePanel("CORE FLIGHT DECK");
            panel.style.flexGrow = 1.55f;
            panel.style.minWidth = 0f;

            // ── Gauge grid (3 columns × 3 rows) ────────────────────────
            var gaugeGrid = new VisualElement();
            gaugeGrid.style.flexDirection = FlexDirection.Row;
            gaugeGrid.style.flexWrap = Wrap.Wrap;
            gaugeGrid.style.alignContent = Align.FlexStart;
            gaugeGrid.style.flexShrink = 0f;
            gaugeGrid.style.marginBottom = 4f;

            gaugeGrid.Add(MakeGaugeCard("cg_tavg", "RCS AVG TEMP", "F",
                70f, 600f, 100f, 4, out _crit_gaugeTAvg));
            gaugeGrid.Add(MakeGaugeCard("cg_pressure", "RCS PRESSURE", "PSIA",
                0f, 2600f, 500f, 4, out _crit_gaugePressure));
            gaugeGrid.Add(MakeGaugeCard("cg_pzr_level", "PZR LEVEL", "%",
                0f, 100f, 20f, 4, out _crit_gaugePzrLevel));
            gaugeGrid.Add(MakeGaugeCard("cg_pzr_temp", "PZR TEMP", "F",
                50f, 700f, 100f, 4, out _crit_gaugePzrTemp));
            gaugeGrid.Add(MakeGaugeCard("cg_subcool", "SUBCOOLING", "F",
                0f, 120f, 20f, 4, out _crit_gaugeSubcool));
            gaugeGrid.Add(MakeGaugeCard("cg_sg_pressure", "SG PRESSURE", "PSIA",
                0f, 1400f, 200f, 4, out _crit_gaugeSgPressure));
            gaugeGrid.Add(MakeGaugeCard("cg_heatup", "HEATUP RATE", "F/HR",
                0f, 120f, 20f, 4, out _crit_gaugeHeatupRate));
            gaugeGrid.Add(MakeGaugeCard("cg_press_rate", "PRESS RATE", "PSI/HR",
                0f, 300f, 50f, 5, out _crit_gaugePressRate));
            gaugeGrid.Add(MakeGaugeCard("cg_vac", "COND VACUUM", "inHg",
                0f, 30f, 5f, 5, out _crit_gaugeCondVac));

            panel.Add(gaugeGrid);

            // ── RCP LED row ─────────────────────────────────────────────
            var ledRow = MakeRow();
            ledRow.style.marginTop = 2f;
            ledRow.style.marginBottom = 2f;

            _crit_ledRcpA = MakeLED("RCP-A");
            _crit_ledRcpB = MakeLED("RCP-B");
            _crit_ledRcpC = MakeLED("RCP-C");
            _crit_ledRcpD = MakeLED("RCP-D");

            ledRow.Add(_crit_ledRcpA);
            ledRow.Add(_crit_ledRcpB);
            ledRow.Add(_crit_ledRcpC);
            ledRow.Add(_crit_ledRcpD);
            panel.Add(ledRow);

            // ── Status banner (2×2 metric grid) ─────────────────────────
            var statusGrid = new VisualElement();
            statusGrid.style.flexDirection = FlexDirection.Row;
            statusGrid.style.flexWrap = Wrap.Wrap;
            statusGrid.style.justifyContent = Justify.SpaceBetween;
            statusGrid.style.marginTop = 2f;

            statusGrid.Add(MakeStatusMetric("crit_mode", "Plant Mode"));
            statusGrid.Add(MakeStatusMetric("crit_phase", "Heatup Phase"));
            statusGrid.Add(MakeStatusMetric("crit_pzr_state", "PZR State"));
            statusGrid.Add(MakeStatusMetric("crit_steam_dump", "Steam Dump"));

            panel.Add(statusGrid);

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

            panel.Add(matrix);

            // ── Summary line ────────────────────────────────────────────
            var summaryRow = MakeRow();
            summaryRow.style.justifyContent = Justify.SpaceBetween;
            summaryRow.style.alignItems = Align.Center;
            summaryRow.style.marginTop = 4f;

            var matrixLabel = MakeLabel("3×10 MATRIX", 10f, FontStyle.Bold,
                new Color(0.49f, 0.57f, 0.71f, 1f));
            summaryRow.Add(matrixLabel);

            _crit_annSummary = MakeLabel("0 active / 0 alarm", 11f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            summaryRow.Add(_crit_annSummary);

            panel.Add(summaryRow);

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
            out AnalogGaugeElement gauge)
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
            card.Add(gauge);

            // Digital readout
            var readout = MakeLabel("--", 12f, FontStyle.Bold, UITKDashboardTheme.InfoCyan);
            readout.style.unityTextAlign = TextAnchor.MiddleCenter;
            readout.style.marginTop = 2f;
            card.Add(readout);
            _crit_gaugeReadouts[key] = readout;

            return card;
        }

        /// <summary>Create an LED indicator for the RCP status row.</summary>
        private LEDIndicatorElement MakeLED(string label)
        {
            var led = new LEDIndicatorElement();
            led.Configure(label, new Color(0.18f, 0.85f, 0.25f, 1f), false);
            led.style.marginRight = 12f;
            _allLEDs.Add(led);
            return led;
        }

        /// <summary>Create a compact status metric tile (title + value).</summary>
        private VisualElement MakeStatusMetric(string key, string title)
        {
            var tile = new VisualElement();
            tile.style.width = new Length(48f, LengthUnit.Percent);
            tile.style.minHeight = 32f;
            tile.style.backgroundColor = new Color(0.055f, 0.071f, 0.106f, 1f);
            SetCornerRadius(tile, 4f);
            tile.style.marginBottom = 3f;
            tile.style.paddingTop = 3f;
            tile.style.paddingBottom = 3f;
            tile.style.paddingLeft = 5f;
            tile.style.paddingRight = 5f;

            var titleLbl = MakeLabel(title, 9f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            tile.Add(titleLbl);

            var valueLbl = MakeLabel("--", 12f, FontStyle.Bold,
                UITKDashboardTheme.TextPrimary);
            tile.Add(valueLbl);
            _crit_statusLabels[key] = valueLbl;

            return tile;
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

            _crit_annBindings.Add(new AnnunciatorBinding
            {
                Root = tile,
                Text = text,
                Tone = tone,
                Condition = condition,
                IsActive = false
            });
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

            float absPressRate = Mathf.Abs(engine.pressureRate);
            Color pressRateColor = absPressRate < 100f ? UITKDashboardTheme.NormalGreen :
                absPressRate < 200f ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.AlarmRed;

            // ── Gauge cards ─────────────────────────────────────────────
            SetGaugeCard(_crit_gaugeTAvg, "cg_tavg",
                engine.T_avg, $"{engine.T_avg:F1} F", UITKDashboardTheme.InfoCyan);

            SetGaugeCard(_crit_gaugePressure, "cg_pressure",
                engine.pressure, $"{engine.pressure:F0} psia",
                engine.pressureHigh || engine.pressureLow
                    ? UITKDashboardTheme.AlarmRed : UITKDashboardTheme.InfoCyan);

            SetGaugeCard(_crit_gaugePzrLevel, "cg_pzr_level",
                engine.pzrLevel, $"{engine.pzrLevel:F1}%",
                engine.pzrLevelLow || engine.pzrLevelHigh
                    ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.InfoCyan);

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

            SetGaugeCard(_crit_gaugeHeatupRate, "cg_heatup",
                Mathf.Abs(engine.heatupRate), $"{engine.heatupRate:F1} F/hr",
                UITKDashboardTheme.InfoCyan);

            SetGaugeCard(_crit_gaugePressRate, "cg_press_rate",
                absPressRate, $"{engine.pressureRate:F1} psi/hr", pressRateColor);

            SetGaugeCard(_crit_gaugeCondVac, "cg_vac",
                engine.condenserVacuum_inHg, $"{engine.condenserVacuum_inHg:F1} inHg",
                UITKDashboardTheme.InfoCyan);

            // ── RCP LEDs ────────────────────────────────────────────────
            if (_crit_ledRcpA != null) _crit_ledRcpA.isOn = RcpRunning(engine, 0);
            if (_crit_ledRcpB != null) _crit_ledRcpB.isOn = RcpRunning(engine, 1);
            if (_crit_ledRcpC != null) _crit_ledRcpC.isOn = RcpRunning(engine, 2);
            if (_crit_ledRcpD != null) _crit_ledRcpD.isOn = RcpRunning(engine, 3);

            // ── Status banner ───────────────────────────────────────────
            SetStatusLabel("crit_mode", GetPlantModeString(engine.plantMode),
                GetPlantModeColor(engine.plantMode));
            SetStatusLabel("crit_phase", engine.heatupPhaseDesc,
                UITKDashboardTheme.InfoCyan);
            SetStatusLabel("crit_pzr_state",
                engine.solidPressurizer ? "SOLID" : "BUBBLE",
                engine.solidPressurizer
                    ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.NormalGreen);
            SetStatusLabel("crit_steam_dump",
                engine.steamDumpPermitted ? "PERMITTED" : "BLOCKED",
                engine.steamDumpPermitted
                    ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.AlarmRed);

            // ── Annunciator wall ────────────────────────────────────────
            RefreshAnnunciators();
        }

        // ====================================================================
        // ANNUNCIATOR REFRESH & FLASH
        // ====================================================================

        private void RefreshAnnunciators()
        {
            if (engine == null || _crit_annBindings.Count == 0) return;

            int activeCount = 0;
            int alarmCount = 0;

            foreach (var tile in _crit_annBindings)
            {
                bool active = tile.Condition != null && tile.Condition(engine);
                tile.IsActive = active;

                bool flashOff = active &&
                                tile.Tone == AnnunciatorTone.Alarm &&
                                !_crit_annFlashOn;
                ApplyTileVisual(tile, flashOff);

                if (!active) continue;
                activeCount++;
                if (tile.Tone == AnnunciatorTone.Alarm) alarmCount++;
            }

            if (_crit_annSummary != null)
            {
                _crit_annSummary.text = $"{activeCount} active / {alarmCount} alarm";
                _crit_annSummary.style.color = alarmCount > 0
                    ? UITKDashboardTheme.AlarmRed
                    : activeCount > 0
                        ? UITKDashboardTheme.NormalGreen
                        : UITKDashboardTheme.TextSecondary;
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
                if (tile.Tone == AnnunciatorTone.Alarm && tile.IsActive)
                    ApplyTileVisual(tile, !_crit_annFlashOn);
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

        /// <summary>Set a status label's text and color.</summary>
        private void SetStatusLabel(string key, string text, Color color)
        {
            if (_crit_statusLabels.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
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
