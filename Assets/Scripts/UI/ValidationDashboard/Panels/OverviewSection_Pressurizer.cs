// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Pressurizer
// OverviewSection_Pressurizer.cs - PZR Level Gauge and Status Summary
// ============================================================================
//
// PURPOSE:
//   Pressurizer overview section with arc gauge for level, status
//   indicators for bubble/heater state, and digital readouts for
//   temperature and surge flow.
//
// INSTRUMENTS:
//   Top: ArcGauge (PZR Level 0-100%)
//   Middle: 2× StatusIndicators (BUBBLE, HEATERS)
//   Bottom: 2× DigitalReadouts (PZR Temp, Surge Flow)
//
// VERSION: 2.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class OverviewSection_Pressurizer : OverviewSectionBase
    {
        // Gauge references
        private ArcGauge _levelGauge;
        private StatusIndicator _bubbleIndicator;
        private StatusIndicator _heaterIndicator;
        private DigitalReadout _tempReadout;
        private DigitalReadout _surgeReadout;

        protected override void BuildContent()
        {
            // --- Hero gauge: PZR Level ---
            GameObject gaugeHolder = new GameObject("LevelGaugeHolder");
            gaugeHolder.transform.SetParent(ContentRoot, false);

            LayoutElement gaugeLE = gaugeHolder.AddComponent<LayoutElement>();
            gaugeLE.preferredHeight = 140;
            gaugeLE.flexibleWidth = 1;

            HorizontalLayoutGroup gaugeLayout = gaugeHolder.AddComponent<HorizontalLayoutGroup>();
            gaugeLayout.childAlignment = TextAnchor.MiddleCenter;
            gaugeLayout.childControlWidth = false;
            gaugeLayout.childControlHeight = false;

            _levelGauge = ArcGauge.Create(gaugeHolder.transform,
                "PZR LEVEL", 0f, 100f, 17f, 80f, 12f, 92f, " %");

            // --- Status indicators row ---
            GameObject statusRow = new GameObject("StatusRow");
            statusRow.transform.SetParent(ContentRoot, false);

            LayoutElement statusLE = statusRow.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 28;
            statusLE.minHeight = 28;

            HorizontalLayoutGroup statusLayout = statusRow.AddComponent<HorizontalLayoutGroup>();
            statusLayout.childAlignment = TextAnchor.MiddleCenter;
            statusLayout.childControlWidth = false;
            statusLayout.childControlHeight = false;
            statusLayout.spacing = 8;

            _bubbleIndicator = StatusIndicator.Create(statusRow.transform, "BUBBLE",
                StatusIndicator.IndicatorShape.Pill, 70, 22);
            _heaterIndicator = StatusIndicator.Create(statusRow.transform, "HEATERS",
                StatusIndicator.IndicatorShape.Pill, 70, 22);

            // --- Digital readouts row ---
            GameObject readoutRow = new GameObject("ReadoutRow");
            readoutRow.transform.SetParent(ContentRoot, false);

            LayoutElement readoutLE = readoutRow.AddComponent<LayoutElement>();
            readoutLE.preferredHeight = 50;
            readoutLE.flexibleWidth = 1;

            HorizontalLayoutGroup readoutLayout = readoutRow.AddComponent<HorizontalLayoutGroup>();
            readoutLayout.childAlignment = TextAnchor.MiddleCenter;
            readoutLayout.childControlWidth = true;
            readoutLayout.childControlHeight = true;
            readoutLayout.childForceExpandWidth = true;
            readoutLayout.spacing = 4;

            _tempReadout = DigitalReadout.Create(readoutRow.transform, "PZR TEMP", "°F", "F1", 16f);
            _surgeReadout = DigitalReadout.Create(readoutRow.transform, "SURGE", "gpm", "F1", 16f);
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            _levelGauge?.SetValue(engine.pzrLevel);
            _tempReadout?.SetValue(engine.T_pzr);
            _surgeReadout?.SetValue(engine.surgeFlow);

            // Bubble status
            if (_bubbleIndicator != null)
            {
                if (engine.bubbleFormed)
                    _bubbleIndicator.SetState(StatusIndicator.IndicatorState.Normal);
                else if (engine.heatupInProgress)
                    _bubbleIndicator.SetState(StatusIndicator.IndicatorState.Warning);
                else
                    _bubbleIndicator.SetState(StatusIndicator.IndicatorState.Off);
            }

            // Heater status
            if (_heaterIndicator != null)
            {
                _heaterIndicator.SetOn(engine.pzrHeatersOn);
            }
        }

        public override void UpdateVisuals()
        {
            // Gauges and indicators handle their own animation
        }
    }
}
