// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Reactor Core
// OverviewSection_ReactorCore.cs - Temperature and Mode Display
// ============================================================================
// VERSION: 1.0.1
// IP: IP-0031 Stage 3
// ============================================================================

using UnityEngine;
using Critical.Physics;

using Critical.Validation;
namespace Critical.UI.ValidationDashboard
{
    public class OverviewSection_ReactorCore : OverviewSectionBase
    {
        private ParameterRow _tAvgRow;
        private ParameterRow _tHotRow;
        private ParameterRow _tColdRow;
        private StatusRow _modeRow;

        protected override void BuildContent()
        {
            _tAvgRow = CreateRow("T-AVG", "Â°F", "F1");
            _tHotRow = CreateRow("T-HOT", "Â°F", "F1");
            _tColdRow = CreateRow("T-COLD", "Â°F", "F1");
            _modeRow = CreateStatusRow("MODE");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // T-AVG with threshold coloring based on mode temps
            float tAvg = engine.T_avg;
            float normalLow = PlantConstants.MODE_3_TEMP_MIN_F;
            float normalHigh = PlantConstants.T_AVG_NO_LOAD;
            float warnLow = normalLow - 50f;
            float warnHigh = normalHigh + 20f;
            _tAvgRow.SetValueWithThresholds(tAvg, normalLow, normalHigh, warnLow, warnHigh);

            _tHotRow.SetValue(engine.T_hot);
            _tColdRow.SetValue(engine.T_cold);

            // Plant mode display
            string modeText = engine.GetModeString();
            bool modeOK = engine.plantMode <= 3;
            _modeRow.SetStatus(modeText, modeOK);
        }
    }
}

