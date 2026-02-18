// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.RCSTab.cs — RCS Tab Builder (Tab 1)
// ============================================================================
//
// PURPOSE:
//   Builds the RCS Tab (Tab 1) — Primary Loop Detail:
//     - Center: RCSLoopDiagramPOC (animated 4-loop schematic)
//     - Left sidebar: Temperature / Pressure metric cards
//     - Right sidebar: RCP status panel + Physics regime display
//     - Bottom: Heat balance bar and core ΔT indicator
//
// DATA BINDING:
//   RefreshRCSTab() is called from RefreshActiveTabData() at 5Hz
//   when tab 1 is active. RCSLoopDiagramPOC animation runs every frame.
//
// IP: IP-0060 Stage 4
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
        // RCS TAB — REFERENCES
        // ====================================================================

        // Loop diagram
        private RCSLoopDiagramPOC _rcs_loopDiagram;

        // Metric labels (left sidebar)
        private readonly Dictionary<string, Label> _rcs_metricValues =
            new Dictionary<string, Label>(24);

        // RCP cards (right sidebar)
        private readonly LEDIndicatorElement[] _rcs_rcpLeds = new LEDIndicatorElement[4];
        private readonly Label[] _rcs_rcpLabels = new Label[4];

        // Regime display
        private Label _rcs_regimeLabel;
        private Label _rcs_regimeDetail;
        private Label _rcs_couplingLabel;

        // Heat balance bar (bottom)
        private LinearGaugePOC _rcs_heatBar;
        private Label _rcs_heatValue;

        // Core ΔT edgewise meter
        private EdgewiseMeterElement _rcs_deltaTMeter;
        private Label _rcs_deltaTValue;

        // ====================================================================
        // BUILD — Called from BuildTabContents()
        // ====================================================================

        private VisualElement BuildRCSTab()
        {
            var root = MakeTabRoot("v2-tab-rcs");

            // ── Main row: Left sidebar + Center diagram + Right sidebar ──
            var mainRow = MakeRow(2.5f);
            mainRow.style.minHeight = 0f;

            mainRow.Add(BuildRCSLeftSidebar());
            mainRow.Add(BuildRCSCenter());
            mainRow.Add(BuildRCSRightSidebar());

            root.Add(mainRow);

            // ── Bottom row: Heat balance + Core ΔT ──
            var bottomRow = MakeRow(0.6f);
            bottomRow.style.minHeight = 0f;
            bottomRow.Add(BuildRCSHeatBalance());
            bottomRow.Add(BuildRCSCoreDeltaT());
            root.Add(bottomRow);

            return root;
        }

        // ====================================================================
        // LEFT SIDEBAR — Temperature / Pressure metric cards
        // ====================================================================

        private VisualElement BuildRCSLeftSidebar()
        {
            var panel = MakePanel("RCS PARAMETERS");
            panel.style.flexGrow = 0.75f;
            panel.style.minWidth = 0f;

            // Each card is a compact metric tile
            panel.Add(MakeRCSMetric("rcs_thot", "T HOT", "°F"));
            panel.Add(MakeRCSMetric("rcs_tcold", "T COLD", "°F"));
            panel.Add(MakeRCSMetric("rcs_tavg", "T AVG", "°F"));
            panel.Add(MakeRCSMetric("rcs_tsat", "T SAT", "°F"));
            panel.Add(MakeRCSMetric("rcs_subcool", "SUBCOOLING", "°F"));
            panel.Add(MakeRCSMetric("rcs_pressure", "PRESSURE", "PSIA"));
            panel.Add(MakeRCSMetric("rcs_press_rate", "PRESS RATE", "PSI/HR"));
            panel.Add(MakeRCSMetric("rcs_heatup_rate", "HEATUP RATE", "°F/HR"));
            panel.Add(MakeRCSMetric("rcs_rhr_mode", "RHR MODE", ""));
            panel.Add(MakeRCSMetric("rcs_rhr_heat", "RHR NET", "MW"));

            return panel;
        }

        /// <summary>Create a compact metric tile (title + unit on left, value on right).</summary>
        private VisualElement MakeRCSMetric(string key, string title, string unit)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 22f;
            row.style.marginBottom = 3f;
            row.style.paddingLeft = 4f;
            row.style.paddingRight = 4f;
            row.style.paddingTop = 2f;
            row.style.paddingBottom = 2f;
            row.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            SetCornerRadius(row, 3f);

            string titleText = string.IsNullOrEmpty(unit) ? title : $"{title} ({unit})";
            var titleLbl = MakeLabel(titleText, 9f, FontStyle.Normal,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            row.Add(titleLbl);

            var valueLbl = MakeLabel("--", 11f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            row.Add(valueLbl);
            _rcs_metricValues[key] = valueLbl;

            return row;
        }

        // ====================================================================
        // CENTER — RCS Loop Diagram
        // ====================================================================

        private VisualElement BuildRCSCenter()
        {
            var panel = MakePanel("PRIMARY LOOP SCHEMATIC");
            panel.style.flexGrow = 1.7f;
            panel.style.minWidth = 0f;
            panel.style.alignItems = Align.Center;
            panel.style.justifyContent = Justify.Center;

            _rcs_loopDiagram = new RCSLoopDiagramPOC();
            _rcs_loopDiagram.style.width = new Length(100f, LengthUnit.Percent);
            _rcs_loopDiagram.style.height = new Length(100f, LengthUnit.Percent);
            _rcs_loopDiagram.style.minHeight = 300f;
            _rcs_loopDiagram.style.flexGrow = 1f;
            panel.Add(_rcs_loopDiagram);

            return panel;
        }

        // ====================================================================
        // RIGHT SIDEBAR — RCP status + Physics regime
        // ====================================================================

        private VisualElement BuildRCSRightSidebar()
        {
            var container = new VisualElement();
            container.style.flexGrow = 0.8f;
            container.style.minWidth = 0f;
            container.style.flexDirection = FlexDirection.Column;

            // ── RCP Status panel ────────────────────────────────────────
            var rcpPanel = MakePanel("RCP STATUS");
            rcpPanel.style.flexGrow = 0f;

            for (int i = 0; i < 4; i++)
            {
                var rcpRow = MakeRow();
                rcpRow.style.alignItems = Align.Center;
                rcpRow.style.marginBottom = 4f;

                _rcs_rcpLeds[i] = new LEDIndicatorElement();
                _rcs_rcpLeds[i].Configure($"RCP #{i + 1}", new Color(0.18f, 0.85f, 0.25f, 1f), false);
                _rcs_rcpLeds[i].style.marginRight = 8f;
                _allLEDs.Add(_rcs_rcpLeds[i]);

                _rcs_rcpLabels[i] = MakeLabel("OFF", 10f, FontStyle.Bold,
                    UITKDashboardTheme.TextSecondary);
                rcpRow.Add(_rcs_rcpLeds[i]);
                rcpRow.Add(_rcs_rcpLabels[i]);
                rcpPanel.Add(rcpRow);
            }

            // Effective heat summary
            var heatSummary = MakeRCSMetric("rcs_rcp_heat", "EFF. RCP HEAT", "MW");
            rcpPanel.Add(heatSummary);

            // RCP count
            var countSummary = MakeRCSMetric("rcs_rcp_count", "RUNNING", "");
            rcpPanel.Add(countSummary);

            container.Add(rcpPanel);

            // ── Physics Regime panel ────────────────────────────────────
            var regimePanel = MakePanel("PHYSICS REGIME");
            regimePanel.style.flexGrow = 1f;

            _rcs_regimeLabel = MakeLabel("REGIME 1", 14f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            regimePanel.Add(_rcs_regimeLabel);

            _rcs_regimeDetail = MakeLabel("Isolated — No RCPs", 10f, FontStyle.Normal,
                UITKDashboardTheme.TextSecondary);
            _rcs_regimeDetail.style.marginTop = 4f;
            _rcs_regimeDetail.style.whiteSpace = WhiteSpace.Normal;
            regimePanel.Add(_rcs_regimeDetail);

            _rcs_couplingLabel = MakeLabel("α = 0.00", 11f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            _rcs_couplingLabel.style.marginTop = 6f;
            regimePanel.Add(_rcs_couplingLabel);

            // Plant mode + mass
            regimePanel.Add(MakeRCSMetric("rcs_plant_mode", "PLANT MODE", ""));
            regimePanel.Add(MakeRCSMetric("rcs_rcs_mass", "RCS MASS", "lbm"));
            regimePanel.Add(MakeRCSMetric("rcs_transport", "TRANSPORT", ""));

            container.Add(regimePanel);

            return container;
        }

        // ====================================================================
        // BOTTOM — Heat Balance Bar + Core ΔT
        // ====================================================================

        private VisualElement BuildRCSHeatBalance()
        {
            var panel = MakePanel("NET PLANT HEAT BALANCE");
            panel.style.flexGrow = 1.5f;
            panel.style.minWidth = 0f;

            _rcs_heatBar = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = -10f,
                maxValue = 30f,
                warningThreshold = 20f,
                alarmThreshold = 25f
            };
            _rcs_heatBar.style.height = 20f;
            panel.Add(_rcs_heatBar);

            var heatRow = MakeRow();
            heatRow.style.justifyContent = Justify.SpaceBetween;
            heatRow.style.marginTop = 4f;

            var sourcesLbl = MakeLabel("Sources: RCP + HTR + RHR", 9f, FontStyle.Normal,
                new Color(0.63f, 0.72f, 0.84f, 1f));
            heatRow.Add(sourcesLbl);

            _rcs_heatValue = MakeLabel("0.0 MW", 12f, FontStyle.Bold,
                UITKDashboardTheme.NormalGreen);
            heatRow.Add(_rcs_heatValue);

            panel.Add(heatRow);

            var sinksLbl = MakeLabel("Sinks: Insulation + SG + Steam Dump", 9f,
                FontStyle.Normal, new Color(0.49f, 0.57f, 0.71f, 1f));
            sinksLbl.style.marginTop = 2f;
            panel.Add(sinksLbl);

            return panel;
        }

        private VisualElement BuildRCSCoreDeltaT()
        {
            var panel = MakePanel("CORE ΔT");
            panel.style.flexGrow = 0.7f;
            panel.style.minWidth = 0f;
            panel.style.alignItems = Align.Center;

            _rcs_deltaTMeter = new EdgewiseMeterElement
            {
                Orientation = MeterOrientation.Vertical,
                MinValue = 0f,
                MaxValue = 80f,
                CenterZero = false,
                Unit = "°F",
                Title = "ΔT",
                MajorTickInterval = 20f,
                MinorTicksPerMajor = 4
            };
            _rcs_deltaTMeter.style.width = 48f;
            _rcs_deltaTMeter.style.height = new Length(100f, LengthUnit.Percent);
            _rcs_deltaTMeter.style.minHeight = 120f;
            panel.Add(_rcs_deltaTMeter);

            _rcs_deltaTValue = MakeLabel("--", 12f, FontStyle.Bold,
                UITKDashboardTheme.InfoCyan);
            _rcs_deltaTValue.style.marginTop = 4f;
            panel.Add(_rcs_deltaTValue);

            return panel;
        }

        // ====================================================================
        // DATA REFRESH — RCS TAB (5Hz)
        // ====================================================================

        private void RefreshRCSTab()
        {
            if (engine == null) return;

            // ── Left sidebar metrics ────────────────────────────────────
            SetRCSMetric("rcs_thot", $"{engine.T_hot:F1}", TempColor(engine.T_hot));
            SetRCSMetric("rcs_tcold", $"{engine.T_cold:F1}", TempColor(engine.T_cold));
            SetRCSMetric("rcs_tavg", $"{engine.T_avg:F1}", TempColor(engine.T_avg));
            SetRCSMetric("rcs_tsat", $"{engine.T_sat:F1}", UITKDashboardTheme.TextSecondary);
            SetRCSMetric("rcs_subcool", $"{engine.subcooling:F1}",
                engine.subcoolingLow
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.NormalGreen);
            SetRCSMetric("rcs_pressure", $"{engine.pressure:F0}",
                engine.pressureLow || engine.pressureHigh
                    ? UITKDashboardTheme.AlarmRed
                    : UITKDashboardTheme.InfoCyan);

            float absPressRate = Mathf.Abs(engine.pressureRate);
            SetRCSMetric("rcs_press_rate", $"{engine.pressureRate:F1}",
                absPressRate > 200f ? UITKDashboardTheme.AlarmRed
                : absPressRate > 100f ? UITKDashboardTheme.WarningAmber
                : UITKDashboardTheme.NormalGreen);

            SetRCSMetric("rcs_heatup_rate", $"{engine.heatupRate:F1}",
                UITKDashboardTheme.InfoCyan);
            SetRCSMetric("rcs_rhr_mode", engine.rhrModeString,
                engine.rhrActive ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.TextSecondary);
            SetRCSMetric("rcs_rhr_heat", $"{engine.rhrNetHeat_MW:F3}",
                engine.rhrActive ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.TextSecondary);

            // ── Loop diagram ────────────────────────────────────────────
            if (_rcs_loopDiagram != null)
            {
                _rcs_loopDiagram.T_hot = engine.T_hot;
                _rcs_loopDiagram.T_cold = engine.T_cold;
                _rcs_loopDiagram.T_avg = engine.T_avg;
                _rcs_loopDiagram.pressure = engine.pressure;
                _rcs_loopDiagram.rcpCount = engine.rcpCount;
                _rcs_loopDiagram.rhrActive = engine.rhrActive;

                for (int i = 0; i < 4; i++)
                    _rcs_loopDiagram.SetRCPRunning(i, RcpRunning(engine, i));
            }

            // ── RCP status ──────────────────────────────────────────────
            for (int i = 0; i < 4; i++)
            {
                bool running = RcpRunning(engine, i);
                if (_rcs_rcpLeds[i] != null)
                    _rcs_rcpLeds[i].isOn = running;
                if (_rcs_rcpLabels[i] != null)
                {
                    _rcs_rcpLabels[i].text = running ? "RUNNING" : "OFF";
                    _rcs_rcpLabels[i].style.color = running
                        ? UITKDashboardTheme.NormalGreen
                        : UITKDashboardTheme.TextSecondary;
                }
            }

            SetRCSMetric("rcs_rcp_heat", $"{engine.effectiveRCPHeat:F2}",
                engine.rcpCount > 0 ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.TextSecondary);
            SetRCSMetric("rcs_rcp_count", $"{engine.rcpCount}/4",
                engine.rcpCount >= 4
                    ? UITKDashboardTheme.NormalGreen
                    : engine.rcpCount > 0
                        ? UITKDashboardTheme.WarningAmber
                        : UITKDashboardTheme.TextSecondary);

            // ── Physics regime ──────────────────────────────────────────
            float alpha = engine.rcpContribution.TotalFlowFraction;
            int regimeId = GetPhysicsRegimeId(engine);
            UpdateRegimeDisplay(regimeId, alpha);

            SetRCSMetric("rcs_plant_mode", engine.GetModeString().Replace("\n", " "),
                engine.GetModeColor());
            SetRCSMetric("rcs_rcs_mass", $"{engine.rcsWaterMass:F0}",
                UITKDashboardTheme.InfoCyan);
            SetRCSMetric("rcs_transport",
                engine.rcpCount == 0
                    ? $"{engine.noRcpTransportFactor:F3}"
                    : "FORCED FLOW",
                UITKDashboardTheme.TextSecondary);

            // ── Heat balance ────────────────────────────────────────────
            float netHeat = engine.netPlantHeat_MW;
            if (_rcs_heatBar != null)
                _rcs_heatBar.value = Mathf.Clamp(netHeat, -10f, 30f);
            if (_rcs_heatValue != null)
            {
                _rcs_heatValue.text = $"{netHeat:F2} MW";
                _rcs_heatValue.style.color = netHeat > 0f
                    ? UITKDashboardTheme.NormalGreen
                    : UITKDashboardTheme.AccentBlue;
            }

            // ── Core ΔT ─────────────────────────────────────────────────
            float coredt = engine.T_hot - engine.T_cold;
            if (_rcs_deltaTMeter != null)
                _rcs_deltaTMeter.Value = Mathf.Clamp(coredt, 0f, 80f);
            if (_rcs_deltaTValue != null)
            {
                _rcs_deltaTValue.text = $"{coredt:F1} °F";
                _rcs_deltaTValue.style.color = coredt > 65f
                    ? UITKDashboardTheme.AlarmRed
                    : coredt > 50f
                        ? UITKDashboardTheme.WarningAmber
                        : UITKDashboardTheme.InfoCyan;
            }
        }

        // ====================================================================
        // RCS TAB ANIMATION (called every frame from TickAnimations)
        // ====================================================================

        /// <summary>Update loop diagram flow animation.</summary>
        private void TickRCSLoopAnimation(float dt)
        {
            _rcs_loopDiagram?.UpdateAnimation(dt);
        }

        // ====================================================================
        // HELPERS — RCS Tab
        // ====================================================================

        private void SetRCSMetric(string key, string text, Color color)
        {
            if (_rcs_metricValues.TryGetValue(key, out var lbl))
            {
                lbl.text = text;
                lbl.style.color = color;
            }
        }

        /// <summary>Temperature-based color (cold→warm→hot).</summary>
        private static Color TempColor(float tempF)
        {
            if (tempF < 200f) return UITKDashboardTheme.InfoCyan;
            if (tempF < 350f) return UITKDashboardTheme.WarningAmber;
            return new Color(1f, 0.55f, 0.3f, 1f); // Orange-red for hot
        }

        /// <summary>Determine physics regime from engine state.</summary>
        private static int GetPhysicsRegimeId(HeatupSimEngine e)
        {
            if (e.rcpCount == 0) return 1;
            if (!e.rcpContribution.AllFullyRunning) return 2;
            return 3;
        }

        /// <summary>Update the regime display labels.</summary>
        private void UpdateRegimeDisplay(int regimeId, float alpha)
        {
            string regimeName;
            string regimeDesc;
            Color regimeColor;

            switch (regimeId)
            {
                case 1:
                    regimeName = "REGIME 1";
                    regimeDesc = engine.solidPressurizer
                        ? "Solid pressurizer — CVCS pressure control"
                        : "Isolated — PZR/RCS thermally decoupled";
                    regimeColor = UITKDashboardTheme.InfoCyan;
                    break;
                case 2:
                    regimeName = "REGIME 2";
                    regimeDesc = $"Blended — {engine.rcpCount} RCP(s) ramping, " +
                                 $"α={alpha:F3}";
                    regimeColor = UITKDashboardTheme.WarningAmber;
                    break;
                default:
                    regimeName = "REGIME 3";
                    regimeDesc = "Fully coupled — CoupledThermo solver active";
                    regimeColor = UITKDashboardTheme.NormalGreen;
                    break;
            }

            if (_rcs_regimeLabel != null)
            {
                _rcs_regimeLabel.text = regimeName;
                _rcs_regimeLabel.style.color = regimeColor;
            }
            if (_rcs_regimeDetail != null)
                _rcs_regimeDetail.text = regimeDesc;
            if (_rcs_couplingLabel != null)
            {
                _rcs_couplingLabel.text = $"α = {alpha:F3}";
                _rcs_couplingLabel.style.color = regimeColor;
            }
        }
    }
}
