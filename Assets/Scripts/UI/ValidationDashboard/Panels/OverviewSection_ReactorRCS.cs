// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Reactor / RCS (Merged)
// OverviewSection_ReactorRCS.cs - Combined Reactor Core + RCS Primary
// ============================================================================
//
// PURPOSE:
//   Merged overview section displaying reactor core temperatures and
//   RCS primary parameters with arc gauge instruments and RCP status
//   indicators. Replaces text-only OverviewSection_ReactorCore and
//   OverviewSection_RCS.
//
// INSTRUMENTS:
//   Top: 2×2 grid of ArcGauges (RCS Pressure, Tavg, Subcooling, ΔT)
//   Bottom: 4× compact StatusIndicators (RCP-1 through RCP-4)
//
// VERSION: 2.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 3
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Combined Reactor/RCS overview section with arc gauges.
    /// </summary>
    public class OverviewSection_ReactorRCS : OverviewSectionBase
    {
        // Gauge references
        private ArcGauge _pressureGauge;
        private ArcGauge _tavgGauge;
        private ArcGauge _subcoolGauge;
        private ArcGauge _deltaTGauge;

        // RCP status indicators
        private StatusIndicator _rcp1;
        private StatusIndicator _rcp2;
        private StatusIndicator _rcp3;
        private StatusIndicator _rcp4;

        protected override void BuildContent()
        {
            // --- Gauge grid: 2×2 layout ---
            GameObject gaugeGrid = new GameObject("GaugeGrid");
            gaugeGrid.transform.SetParent(ContentRoot, false);

            RectTransform gridRT = gaugeGrid.AddComponent<RectTransform>();
            LayoutElement gridLE = gaugeGrid.AddComponent<LayoutElement>();
            gridLE.flexibleHeight = 1;
            gridLE.flexibleWidth = 1;

            GridLayoutGroup grid = gaugeGrid.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(130, 130);
            grid.spacing = new Vector2(4, 4);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(2, 2, 2, 2);

            // ArcGauges — heatup ranges, not full-power ranges
            _pressureGauge = ArcGauge.Create(gaugeGrid.transform,
                "RCS PRESS", 0f, 2500f, 350f, 2300f, 300f, 2385f, " psia");

            _tavgGauge = ArcGauge.Create(gaugeGrid.transform,
                "T-AVG", 50f, 650f, 100f, 550f, 70f, 600f, " °F");

            _subcoolGauge = ArcGauge.Create(gaugeGrid.transform,
                "SUBCOOL", 0f, 200f, 30f, 999f, 20f, 999f, " °F");

            _deltaTGauge = ArcGauge.Create(gaugeGrid.transform,
                "DELTA-T", 0f, 80f, 0f, 60f, 0f, 70f, " °F");

            // --- RCP status row ---
            GameObject rcpRow = new GameObject("RCPRow");
            rcpRow.transform.SetParent(ContentRoot, false);

            LayoutElement rcpLE = rcpRow.AddComponent<LayoutElement>();
            rcpLE.preferredHeight = 28;
            rcpLE.minHeight = 28;

            HorizontalLayoutGroup rcpLayout = rcpRow.AddComponent<HorizontalLayoutGroup>();
            rcpLayout.childAlignment = TextAnchor.MiddleCenter;
            rcpLayout.childControlWidth = false;
            rcpLayout.childControlHeight = false;
            rcpLayout.childForceExpandWidth = false;
            rcpLayout.childForceExpandHeight = false;
            rcpLayout.spacing = 6;

            _rcp1 = StatusIndicator.Create(rcpRow.transform, "RCP1",
                StatusIndicator.IndicatorShape.Pill, 56, 22);
            _rcp2 = StatusIndicator.Create(rcpRow.transform, "RCP2",
                StatusIndicator.IndicatorShape.Pill, 56, 22);
            _rcp3 = StatusIndicator.Create(rcpRow.transform, "RCP3",
                StatusIndicator.IndicatorShape.Pill, 56, 22);
            _rcp4 = StatusIndicator.Create(rcpRow.transform, "RCP4",
                StatusIndicator.IndicatorShape.Pill, 56, 22);
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            _pressureGauge?.SetValue(engine.pressure);
            _tavgGauge?.SetValue(engine.T_avg);
            _subcoolGauge?.SetValue(engine.subcooling);
            _deltaTGauge?.SetValue(engine.T_hot - engine.T_cold);

            if (engine.rcpRunning != null)
            {
                if (_rcp1 != null) _rcp1.SetOn(engine.rcpRunning.Length > 0 && engine.rcpRunning[0]);
                if (_rcp2 != null) _rcp2.SetOn(engine.rcpRunning.Length > 1 && engine.rcpRunning[1]);
                if (_rcp3 != null) _rcp3.SetOn(engine.rcpRunning.Length > 2 && engine.rcpRunning[2]);
                if (_rcp4 != null) _rcp4.SetOn(engine.rcpRunning.Length > 3 && engine.rcpRunning[3]);
            }
        }

        public override void UpdateVisuals()
        {
            // Gauges handle their own animation via SmoothDamp in Update()
            // StatusIndicators handle their own pulse animation
        }
    }
}
