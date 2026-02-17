// ============================================================================
// CRITICAL: Master the Atom - Overview Section: CVCS / SG (Merged)
// OverviewSection_CVCSG.cs - Combined CVCS + Steam Generator
// ============================================================================
//
// PURPOSE:
//   Merged overview section displaying CVCS flow parameters and steam
//   generator data with digital readouts and a bidirectional gauge for
//   net CVCS flow. Replaces text-only OverviewSection_CVCS and
//   OverviewSection_SGRHR.
//
// INSTRUMENTS:
//   Top: 2× DigitalReadouts (Charging Flow, Letdown Flow)
//   Middle: BidirectionalGauge (Net CVCS Flow)
//   Bottom: 2× DigitalReadouts (SG Secondary Pressure, Net Plant Heat)
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
    /// <summary>
    /// Combined CVCS/SG overview section with gauges.
    /// </summary>
    public class OverviewSection_CVCSG : OverviewSectionBase
    {
        // Gauge references
        private DigitalReadout _chargingReadout;
        private DigitalReadout _letdownReadout;
        private BidirectionalGauge _netCvcsGauge;
        private DigitalReadout _sgPressureReadout;
        private DigitalReadout _netHeatReadout;

        protected override void BuildContent()
        {
            // --- Top: Charging / Letdown readout row ---
            GameObject flowRow = new GameObject("FlowRow");
            flowRow.transform.SetParent(ContentRoot, false);

            LayoutElement flowLE = flowRow.AddComponent<LayoutElement>();
            flowLE.preferredHeight = 55;
            flowLE.flexibleWidth = 1;

            HorizontalLayoutGroup flowLayout = flowRow.AddComponent<HorizontalLayoutGroup>();
            flowLayout.childAlignment = TextAnchor.MiddleCenter;
            flowLayout.childControlWidth = true;
            flowLayout.childControlHeight = true;
            flowLayout.childForceExpandWidth = true;
            flowLayout.spacing = 4;

            _chargingReadout = DigitalReadout.Create(flowRow.transform, "CHARGING", "gpm", "F1", 16f);
            _letdownReadout = DigitalReadout.Create(flowRow.transform, "LETDOWN", "gpm", "F1", 16f);

            // --- Middle: Net CVCS bidirectional gauge ---
            GameObject biGaugeHolder = new GameObject("NetCVCSHolder");
            biGaugeHolder.transform.SetParent(ContentRoot, false);

            LayoutElement biLE = biGaugeHolder.AddComponent<LayoutElement>();
            biLE.preferredHeight = 130;
            biLE.flexibleWidth = 1;

            HorizontalLayoutGroup biLayout = biGaugeHolder.AddComponent<HorizontalLayoutGroup>();
            biLayout.childAlignment = TextAnchor.MiddleCenter;
            biLayout.childControlWidth = false;
            biLayout.childControlHeight = false;

            _netCvcsGauge = BidirectionalGauge.Create(biGaugeHolder.transform,
                "NET CVCS", 75f, " gpm");

            // --- Bottom: SG Pressure / Net Heat readout row ---
            GameObject sgRow = new GameObject("SGRow");
            sgRow.transform.SetParent(ContentRoot, false);

            LayoutElement sgLE = sgRow.AddComponent<LayoutElement>();
            sgLE.preferredHeight = 55;
            sgLE.flexibleWidth = 1;

            HorizontalLayoutGroup sgLayout = sgRow.AddComponent<HorizontalLayoutGroup>();
            sgLayout.childAlignment = TextAnchor.MiddleCenter;
            sgLayout.childControlWidth = true;
            sgLayout.childControlHeight = true;
            sgLayout.childForceExpandWidth = true;
            sgLayout.spacing = 4;

            _sgPressureReadout = DigitalReadout.Create(sgRow.transform, "SG PRESS", "psia", "F0", 16f);
            _netHeatReadout = DigitalReadout.Create(sgRow.transform, "NET HEAT", "MW", "F2", 16f);
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            _chargingReadout?.SetValue(engine.chargingFlow);
            _letdownReadout?.SetValue(engine.letdownFlow);

            float netCvcs = engine.chargingFlow - engine.letdownFlow;
            _netCvcsGauge?.SetValue(netCvcs);

            _sgPressureReadout?.SetValue(engine.sgSecondaryPressure_psia);
            _netHeatReadout?.SetValue(engine.netPlantHeat_MW);
        }

        public override void UpdateVisuals()
        {
            // Gauges handle their own animation
        }
    }
}
