// ============================================================================
// CRITICAL: Master the Atom - Overview Section: CVCS
// OverviewSection_CVCS.cs - Charging, Letdown, VCT Display
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - Charging Flow (gpm)
//   - Letdown Flow (gpm)
//   - Net CVCS Flow (gpm)
//   - VCT Level (%)
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
    /// CVCS section showing charging, letdown, and VCT status.
    /// </summary>
    public class OverviewSection_CVCS : OverviewSectionBase
    {
        private ParameterRow _chargingRow;
        private ParameterRow _letdownRow;
        private ParameterRow _netFlowRow;
        private ParameterRow _vctLevelRow;

        protected override void BuildContent()
        {
            _chargingRow = CreateRow("Charging", " gpm", "F1");
            _letdownRow = CreateRow("Letdown", " gpm", "F1");
            _netFlowRow = CreateRow("Net CVCS", " gpm", "F1");
            _vctLevelRow = CreateRow("VCT Level", "%", "F1");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // Charging flow
            float charging = engine.chargingFlow;
            _chargingRow.SetValue(charging);
            
            if (engine.chargingActive)
                _chargingRow.SetColor(ValidationDashboardTheme.NormalGreen);
            else
                _chargingRow.SetColor(ValidationDashboardTheme.Neutral);

            // Letdown flow
            float letdown = engine.letdownFlow;
            _letdownRow.SetValue(letdown);
            
            if (engine.letdownActive)
                _letdownRow.SetColor(ValidationDashboardTheme.NormalGreen);
            else if (engine.letdownIsolatedFlag)
                _letdownRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _letdownRow.SetColor(ValidationDashboardTheme.Neutral);

            // Net CVCS flow (charging - letdown)
            float netFlow = charging - letdown;
            _netFlowRow.SetValue(netFlow);
            
            // Color based on direction
            if (netFlow > 5f)
                _netFlowRow.SetColor(ValidationDashboardTheme.AccentBlue); // Adding to RCS
            else if (netFlow < -5f)
                _netFlowRow.SetColor(ValidationDashboardTheme.AccentOrange); // Removing from RCS
            else
                _netFlowRow.SetColor(ValidationDashboardTheme.NormalGreen); // Balanced

            // VCT Level
            float vctLevel = engine.vctState.Level_percent;
            _vctLevelRow.SetValueWithThresholds(vctLevel,
                20f, 80f,   // warn low/high
                11f, 95f);  // alarm (auto-makeup at 11%, divert at 95%)
            
            if (engine.vctLevelLow)
                _vctLevelRow.SetColor(ValidationDashboardTheme.AlarmRed);
            else if (engine.vctLevelHigh)
                _vctLevelRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else if (vctLevel < 25f || vctLevel > 75f)
                _vctLevelRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else
                _vctLevelRow.SetColor(ValidationDashboardTheme.NormalGreen);
        }
    }
}
