// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Pressurizer
// OverviewSection_Pressurizer.cs - PZR Level, Pressure, Heater/Spray Status
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - PZR Level (%)
//   - PZR Pressure (psia)
//   - Heater Status
//   - Spray Status
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
    /// Pressurizer section showing level, pressure, and control systems.
    /// </summary>
    public class OverviewSection_Pressurizer : OverviewSectionBase
    {
        private ParameterRow _levelRow;
        private ParameterRow _pressureRow;
        private StatusRow _heaterRow;
        private StatusRow _sprayRow;

        protected override void BuildContent()
        {
            _levelRow = CreateRow("Level", "%", "F1");
            _pressureRow = CreateRow("Pressure", " psia", "F0");
            _heaterRow = CreateStatusRow("Heaters");
            _sprayRow = CreateStatusRow("Spray");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // PZR Level
            float level = engine.pzrLevel;
            _levelRow.SetValueWithThresholds(level,
                17f, 80f,   // warn low/high (17% low, 80% high per HRTD)
                12f, 92f);  // alarm (12% lo-lo, 92% hi-hi)
            
            if (engine.pzrLevelLow)
                _levelRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else if (engine.pzrLevelHigh)
                _levelRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else if (level < 20f || level > 75f)
                _levelRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _levelRow.SetColor(ValidationDashboardTheme.NormalGreen);

            // PZR Pressure (same as RCS)
            float pressure = engine.pressure;
            _pressureRow.SetValue(pressure);
            
            if (engine.pressureLow || engine.pressureHigh)
                _pressureRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else if (pressure >= PlantConstants.PZR_OPERATING_PRESSURE_PSIA - 50f &&
                     pressure <= PlantConstants.PZR_OPERATING_PRESSURE_PSIA + 50f)
                _pressureRow.SetColor(ValidationDashboardTheme.NormalGreen);
            else
                _pressureRow.SetColor(ValidationDashboardTheme.WarningAmber);

            // Heater status
            bool heatersOn = engine.pzrHeatersOn;
            float heaterPower = engine.pzrHeaterPower;
            
            if (heatersOn && heaterPower > 0.01f)
            {
                // Show power percentage
                float pctPower = (heaterPower / PlantConstants.HEATER_POWER_TOTAL) * 100f * 1000f; // MW to kW
                string heaterText = $"{pctPower:F0}%";
                _heaterRow.SetStatusText(heaterText, ValidationDashboardTheme.WarningAmber);
            }
            else
            {
                _heaterRow.SetStatusText("OFF", ValidationDashboardTheme.Neutral);
            }

            // Spray status
            bool sprayActive = engine.sprayActive;
            float sprayFlow = engine.sprayFlow_GPM;
            
            if (sprayActive && sprayFlow > 1f)
            {
                _sprayRow.SetStatusText($"{sprayFlow:F0}gpm", ValidationDashboardTheme.InfoCyan);
            }
            else
            {
                _sprayRow.SetStatusText("OFF", ValidationDashboardTheme.Neutral);
            }
        }
    }
}
