// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Reactor Core
// OverviewSection_ReactorCore.cs - Core Temperature Display
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - T_avg (Average RCS Temperature)
//   - T_hot (Hot Leg Temperature)
//   - T_cold (Cold Leg Temperature)
//   - Core ΔT (T_hot - T_cold)
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 3
// ============================================================================

using UnityEngine;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Reactor core temperature section.
    /// </summary>
    public class OverviewSection_ReactorCore : OverviewSectionBase
    {
        private ParameterRow _tAvgRow;
        private ParameterRow _tHotRow;
        private ParameterRow _tColdRow;
        private ParameterRow _deltaTRow;

        protected override void BuildContent()
        {
            _tAvgRow = CreateRow("T-avg", "°F", "F1");
            _tHotRow = CreateRow("T-hot", "°F", "F1");
            _tColdRow = CreateRow("T-cold", "°F", "F1");
            _deltaTRow = CreateRow("Core ΔT", "°F", "F1");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // T_avg - target is ~557°F for HZP
            float tAvg = engine.T_avg;
            _tAvgRow.SetValueWithThresholds(tAvg, 
                100f, PlantConstants.MODE_3_TEMP_F,  // warn low/high
                80f, PlantConstants.MODE_3_TEMP_F + 20f);  // alarm low/high
            
            // Color T_avg based on approach to target
            if (tAvg >= PlantConstants.MODE_3_TEMP_F - 10f)
                _tAvgRow.SetColor(ValidationDashboardTheme.NormalGreen);
            else if (tAvg >= PlantConstants.MODE_4_TEMP_F)
                _tAvgRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _tAvgRow.SetColor(ValidationDashboardTheme.InfoCyan);

            // T_hot
            float tHot = engine.T_hot;
            _tHotRow.SetValue(tHot);
            _tHotRow.SetColor(ValidationDashboardTheme.Trace2); // Orange-red for hot

            // T_cold
            float tCold = engine.T_cold;
            _tColdRow.SetValue(tCold);
            _tColdRow.SetColor(ValidationDashboardTheme.Trace3); // Blue for cold

            // Delta T
            float deltaT = tHot - tCold;
            _deltaTRow.SetValue(deltaT);
            
            // Normal ΔT is ~60°F at full power, less during heatup
            if (deltaT > 65f)
                _deltaTRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else if (deltaT > 80f)
                _deltaTRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else
                _deltaTRow.SetColor(ValidationDashboardTheme.TextPrimary);
        }
    }
}
