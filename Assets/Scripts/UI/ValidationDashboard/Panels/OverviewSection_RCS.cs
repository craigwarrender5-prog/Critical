// ============================================================================
// CRITICAL: Master the Atom - Overview Section: RCS Primary
// OverviewSection_RCS.cs - RCS Pressure and Flow Display
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - RCS Pressure (psia)
//   - Subcooling Margin (°F)
//   - RCP Count (running/total)
//   - RCS Flow Status
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
    /// RCS primary system section.
    /// </summary>
    public class OverviewSection_RCS : OverviewSectionBase
    {
        private ParameterRow _pressureRow;
        private ParameterRow _subcoolRow;
        private ParameterRow _rcpCountRow;
        private StatusRow _flowStatusRow;

        protected override void BuildContent()
        {
            _pressureRow = CreateRow("Pressure", " psia", "F0");
            _subcoolRow = CreateRow("Subcool", "°F", "F1");
            _rcpCountRow = CreateRow("RCPs", "", "F0");
            _flowStatusRow = CreateStatusRow("Flow");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // RCS Pressure
            float pressure = engine.pressure;
            _pressureRow.SetValueWithThresholds(pressure,
                400f, PlantConstants.PZR_OPERATING_PRESSURE_PSIA - 50f,  // warn
                350f, PlantConstants.PZR_OPERATING_PRESSURE_PSIA + 100f); // alarm
            
            // Color based on operating band
            if (pressure >= PlantConstants.PZR_OPERATING_PRESSURE_PSIA - 50f &&
                pressure <= PlantConstants.PZR_OPERATING_PRESSURE_PSIA + 50f)
            {
                _pressureRow.SetColor(ValidationDashboardTheme.NormalGreen);
            }
            else if (engine.pressureLow || engine.pressureHigh)
            {
                _pressureRow.SetColor(ValidationDashboardTheme.AlarmRed);
            }
            else
            {
                _pressureRow.SetColor(ValidationDashboardTheme.WarningAmber);
            }

            // Subcooling margin
            float subcool = engine.subcooling;
            _subcoolRow.SetValueWithThresholds(subcool,
                20f, 200f,   // warn low/high
                10f, 250f);  // alarm low/high
            
            if (engine.subcoolingLow)
                _subcoolRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else if (subcool < 30f)
                _subcoolRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _subcoolRow.SetColor(ValidationDashboardTheme.NormalGreen);

            // RCP count
            int rcpCount = engine.rcpCount;
            _rcpCountRow.SetText($"{rcpCount}/4");
            
            if (rcpCount == 4)
                _rcpCountRow.SetColor(ValidationDashboardTheme.NormalGreen);
            else if (rcpCount > 0)
                _rcpCountRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _rcpCountRow.SetColor(ValidationDashboardTheme.Neutral);

            // Flow status
            bool hasFlow = rcpCount > 0 || engine.rhrActive;
            bool flowLow = engine.rcsFlowLow;
            
            if (flowLow)
                _flowStatusRow.SetStatusText("LOW", ValidationDashboardTheme.AlarmRed);
            else if (hasFlow)
                _flowStatusRow.SetStatusText("OK", ValidationDashboardTheme.NormalGreen);
            else
                _flowStatusRow.SetStatusText("NONE", ValidationDashboardTheme.Neutral);
        }
    }
}
