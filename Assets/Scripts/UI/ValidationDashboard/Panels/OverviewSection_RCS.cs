// ============================================================================
// CRITICAL: Master the Atom - Overview Section: RCS
// OverviewSection_RCS.cs - RCS Pressure and Flow Summary
// ============================================================================
// VERSION: 1.0.1
// IP: IP-0031 Stage 3
// ============================================================================

using UnityEngine;
using Critical.Physics;

using Critical.Validation;
namespace Critical.UI.ValidationDashboard
{
    public class OverviewSection_RCS : OverviewSectionBase
    {
        private ParameterRow _pressureRow;
        private ParameterRow _subcoolRow;
        private StatusRow _rcpRow;

        protected override void BuildContent()
        {
            _pressureRow = CreateRow("PRESSURE", " psia", "F0");
            _subcoolRow = CreateRow("SUBCOOL", "Â°F", "F1");
            _rcpRow = CreateStatusRow("RCPs");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // Pressure with operating band thresholds
            float p = engine.pressure;
            float targetP = PlantConstants.PZR_OPERATING_PRESSURE_PSIG + 14.7f;
            float normalLow = 400f;
            float normalHigh = targetP + 50f;
            float warnLow = 350f;
            float warnHigh = targetP + 100f;
            _pressureRow.SetValueWithThresholds(p, normalLow, normalHigh, warnLow, warnHigh);

            // Subcooling
            float sc = engine.subcooling;
            _subcoolRow.SetValueWithThresholds(sc, 30f, 100f, 20f, 150f);

            // RCP status
            string rcpText = $"{engine.rcpCount}/4 RUNNING";
            bool allRunning = engine.rcpCount >= 4;
            _rcpRow.SetStatus(rcpText, allRunning);
        }
    }
}

