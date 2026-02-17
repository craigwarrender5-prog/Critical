// ============================================================================
// CRITICAL: Master the Atom - Overview Section: System Health
// OverviewSection_SystemHealth.cs - Mass/Energy Conservation Status
// ============================================================================
//
// PURPOSE:
//   Displays system conservation metrics and overall health indicators
//   with digital readouts and status indicators. Replaces text-only
//   OverviewSection_GlobalHealth.
//
// INSTRUMENTS:
//   Top: 2Ã— DigitalReadouts (Mass Error, Energy Balance)
//   Bottom: 3Ã— StatusIndicators (Mass Cons OK, SG Boiling, RHR Active)
//
// VERSION: 2.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 5
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

using Critical.Validation;
namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// System health overview section with digital readouts and status indicators.
    /// </summary>
    public class OverviewSection_SystemHealth : OverviewSectionBase
    {
        private DigitalReadout _massErrorReadout;
        private DigitalReadout _energyReadout;
        private StatusIndicator _massConsIndicator;
        private StatusIndicator _sgBoilingIndicator;
        private StatusIndicator _rhrIndicator;

        protected override void BuildContent()
        {
            // --- Digital readouts (vertical stack) ---
            _massErrorReadout = DigitalReadout.Create(ContentRoot, "MASS ERR", "lbm", "F1", 18f);
            _energyReadout = DigitalReadout.Create(ContentRoot, "ENERGY", "MW", "F3", 16f);

            // --- Status indicator row ---
            GameObject statusRow = new GameObject("StatusRow");
            statusRow.transform.SetParent(ContentRoot, false);

            LayoutElement statusLE = statusRow.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 28;
            statusLE.minHeight = 28;

            HorizontalLayoutGroup statusLayout = statusRow.AddComponent<HorizontalLayoutGroup>();
            statusLayout.childAlignment = TextAnchor.MiddleCenter;
            statusLayout.childControlWidth = false;
            statusLayout.childControlHeight = false;
            statusLayout.spacing = 4;

            _massConsIndicator = StatusIndicator.Create(statusRow.transform, "MASS",
                StatusIndicator.IndicatorShape.Square, 48, 22);
            _sgBoilingIndicator = StatusIndicator.Create(statusRow.transform, "SG BLR",
                StatusIndicator.IndicatorShape.Square, 48, 22);
            _rhrIndicator = StatusIndicator.Create(statusRow.transform, "RHR",
                StatusIndicator.IndicatorShape.Square, 48, 22);
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            float massErr = engine.massError_lbm;
            _massErrorReadout?.SetValue(massErr);
            _energyReadout?.SetValue(engine.netPlantHeat_MW);

            // Mass conservation: green if <500 lbm, warning <1000, alarm otherwise
            if (_massConsIndicator != null)
            {
                float absMass = Mathf.Abs(massErr);
                if (absMass < 500f)
                    _massConsIndicator.SetState(StatusIndicator.IndicatorState.Normal);
                else if (absMass < 1000f)
                    _massConsIndicator.SetState(StatusIndicator.IndicatorState.Warning);
                else
                    _massConsIndicator.SetState(StatusIndicator.IndicatorState.Alarm);
            }

            // SG boiling status
            if (_sgBoilingIndicator != null)
                _sgBoilingIndicator.SetOn(engine.sgBoilingActive);

            // RHR status
            if (_rhrIndicator != null)
                _rhrIndicator.SetOn(engine.rhrActive);
        }

        public override void UpdateVisuals()
        {
            // Gauges and indicators handle their own animation
        }
    }
}

