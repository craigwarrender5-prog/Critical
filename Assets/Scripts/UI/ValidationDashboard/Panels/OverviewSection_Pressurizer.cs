// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Pressurizer
// OverviewSection_Pressurizer.cs - PZR Level and Status Summary
// ============================================================================
// VERSION: 1.0.1
// IP: IP-0031 Stage 3
// ============================================================================

using UnityEngine;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class OverviewSection_Pressurizer : OverviewSectionBase
    {
        private ParameterRow _levelRow;
        private ParameterRow _tempRow;
        private StatusRow _bubbleRow;
        private StatusRow _heaterRow;

        protected override void BuildContent()
        {
            _levelRow = CreateRow("LEVEL", "%", "F1");
            _tempRow = CreateRow("T-PZR", "°F", "F1");
            _bubbleRow = CreateStatusRow("BUBBLE");
            _heaterRow = CreateStatusRow("HEATERS");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // Level with normal band (17-80%) and alarm thresholds (12-92%)
            float level = engine.pzrLevel;
            _levelRow.SetValueWithThresholds(level, 17f, 80f, 12f, 92f);

            // PZR temperature
            _tempRow.SetValue(engine.T_pzr);

            // Bubble status
            string bubbleText = engine.solidPressurizer ? "SOLID" : 
                               engine.bubbleFormed ? "FORMED" : "FORMING";
            bool bubbleOK = engine.bubbleFormed;
            _bubbleRow.SetStatus(bubbleText, bubbleOK);

            // Heater status
            string heaterText = engine.pzrHeatersOn ? 
                $"ON ({engine.pzrHeaterPower * 1000f:F0} kW)" : "OFF";
            _heaterRow.SetStatus(heaterText, engine.pzrHeatersOn);
        }
    }
}
