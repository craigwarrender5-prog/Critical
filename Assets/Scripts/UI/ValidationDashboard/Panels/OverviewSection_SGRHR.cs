// ============================================================================
// CRITICAL: Master the Atom - Overview Section: SG/RHR
// OverviewSection_SGRHR.cs - Steam Generator and RHR System Display
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - SG Secondary Pressure (psia)
//   - SG Heat Transfer (MW)
//   - RHR Mode/Status
//   - Boiling Status
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
    /// Steam generator and RHR system section.
    /// </summary>
    public class OverviewSection_SGRHR : OverviewSectionBase
    {
        private ParameterRow _sgPressureRow;
        private ParameterRow _sgHeatRow;
        private StatusRow _rhrRow;
        private StatusRow _boilingRow;

        protected override void BuildContent()
        {
            _sgPressureRow = CreateRow("SG Press", " psia", "F0");
            _sgHeatRow = CreateRow("SG Heat", " MW", "F2");
            _rhrRow = CreateStatusRow("RHR");
            _boilingRow = CreateStatusRow("Boiling");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // SG Secondary Pressure
            float sgPress = engine.sgSecondaryPressure_psia;
            _sgPressureRow.SetValue(sgPress);
            
            // Normal SG pressure during heatup rises with temperature
            if (sgPress > 1100f) // Approaching relief valve
                _sgPressureRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else if (sgPress > 1000f)
                _sgPressureRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _sgPressureRow.SetColor(ValidationDashboardTheme.NormalGreen);

            // SG Heat Transfer
            float sgHeat = engine.sgHeatTransfer_MW;
            _sgHeatRow.SetValue(sgHeat);
            
            // Heat removal should be positive during heatup (cooling RCS)
            if (sgHeat > 0.5f)
                _sgHeatRow.SetColor(ValidationDashboardTheme.InfoCyan);
            else if (sgHeat < -0.5f)
                _sgHeatRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _sgHeatRow.SetColor(ValidationDashboardTheme.Neutral);

            // RHR Status
            bool rhrActive = engine.rhrActive;
            string rhrMode = engine.rhrModeString;
            
            if (rhrActive)
            {
                _rhrRow.SetStatusText(rhrMode, ValidationDashboardTheme.NormalGreen);
            }
            else
            {
                _rhrRow.SetStatusText("ISOL", ValidationDashboardTheme.Neutral);
            }

            // Boiling Status
            bool boiling = engine.sgBoilingActive;
            
            if (boiling)
            {
                _boilingRow.SetStatusText("YES", ValidationDashboardTheme.WarningAmber);
            }
            else
            {
                _boilingRow.SetStatusText("NO", ValidationDashboardTheme.NormalGreen);
            }
        }
    }
}
