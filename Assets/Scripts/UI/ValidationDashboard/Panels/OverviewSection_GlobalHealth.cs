// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Global Health
// OverviewSection_GlobalHealth.cs - Mass/Energy Conservation Display
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - Mass Conservation Error (lbm)
//   - Energy Conservation Status
//   - Net Plant Heat (MW)
//   - Timestep Stability
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 3
// ============================================================================

using UnityEngine;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Global simulation health section showing conservation metrics.
    /// </summary>
    public class OverviewSection_GlobalHealth : OverviewSectionBase
    {
        private ParameterRow _massErrorRow;
        private ParameterRow _netHeatRow;
        private StatusRow _massStatusRow;
        private StatusRow _energyStatusRow;

        protected override void BuildContent()
        {
            _massErrorRow = CreateRow("Mass Error", " lbm", "F2");
            _netHeatRow = CreateRow("Net Heat", " MW", "F2");
            _massStatusRow = CreateStatusRow("Mass Cons.");
            _energyStatusRow = CreateStatusRow("Energy");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // Mass conservation error
            float massErr = engine.massError_lbm;
            _massErrorRow.SetValueWithThresholds(massErr, -5f, 5f, -50f, 50f);

            // Net plant heat
            float netHeat = engine.netPlantHeat_MW;
            _netHeatRow.SetValue(netHeat);
            
            // Color based on sign (positive = heating, negative = cooling)
            if (netHeat > 0.5f)
                _netHeatRow.SetColor(ValidationDashboardTheme.WarningAmber);
            else if (netHeat < -0.5f)
                _netHeatRow.SetColor(ValidationDashboardTheme.InfoCyan);
            else
                _netHeatRow.SetColor(ValidationDashboardTheme.NormalGreen);

            // Mass conservation status
            bool massOK = engine.primaryMassConservationOK;
            bool massAlarm = engine.primaryMassAlarm;
            _massStatusRow.SetStatus(massOK && !massAlarm, massAlarm);

            // Energy status (derived from net heat balance)
            bool energyOK = Mathf.Abs(netHeat) < 5f; // Within 5 MW is OK
            _energyStatusRow.SetStatus(energyOK, false);
        }
    }
}
