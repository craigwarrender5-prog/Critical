// CRITICAL: Master the Atom - Physics Module
// SolidPlantPressure.Diagnostics.cs - Relief, utility, and validation helpers
//
// File: Assets/Scripts/Physics/SolidPlantPressure.Diagnostics.cs
// Module: Critical.Physics.SolidPlantPressure
// Responsibility: Relief valve model and diagnostic helper/validation APIs.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
namespace Critical.Physics
{
    public static partial class SolidPlantPressure
    {
        #region Relief Valve
        
        /// <summary>
        /// Calculate RHR relief valve flow based on pressure.
        /// Proportional opening above setpoint with hysteresis on reseat.
        /// </summary>
        /// <param name="pressure_psig">Current pressure in psig</param>
        /// <param name="currentlyOpen">True if valve is currently flowing</param>
        /// <returns>Relief flow in gpm</returns>
        public static float CalculateReliefFlow(float pressure_psig, bool currentlyOpen)
        {
            // Valve opens above setpoint
            if (pressure_psig >= RELIEF_SETPOINT_PSIG)
            {
                float fraction = (pressure_psig - RELIEF_SETPOINT_PSIG) / RELIEF_ACCUMULATION_PSI;
                fraction = Math.Max(0f, Math.Min(fraction, 1f));
                return fraction * RELIEF_CAPACITY_GPM;
            }
            
            // Hysteresis: if already open, don't close until reseat pressure
            if (currentlyOpen && pressure_psig > RELIEF_RESEAT_PSIG)
            {
                // Reduced flow between reseat and setpoint
                float fraction = (pressure_psig - RELIEF_RESEAT_PSIG) / 
                                (RELIEF_SETPOINT_PSIG - RELIEF_RESEAT_PSIG);
                fraction = Math.Max(0f, Math.Min(fraction, 0.3f));
                return fraction * RELIEF_CAPACITY_GPM;
            }
            
            return 0f;
        }
        
        #endregion
        #region Utility
        
        /// <summary>
        /// Estimate time to bubble formation from current conditions.
        /// </summary>
        /// <param name="state">Current solid plant state</param>
        /// <returns>Estimated hours to bubble formation, or 999 if rate is too low</returns>
        public static float EstimateTimeToBubble(SolidPlantState state)
        {
            float tempMargin = state.T_sat - state.T_pzr;
            if (tempMargin <= 0f) return 0f;
            if (state.PzrHeatRate < 0.1f) return 999f;
            return tempMargin / state.PzrHeatRate;
        }
        
        /// <summary>
        /// Get a human-readable status string for the solid plant state.
        /// </summary>
        public static string GetStatusString(SolidPlantState state)
        {
            if (state.BubbleFormed)
                return "BUBBLE FORMED";
            
            float margin = state.T_sat - state.T_pzr;
            float P_psig = state.Pressure - PlantConstants.PSIG_TO_PSIA;
            
            if (state.ReliefFlow > 0.1f)
                return $"SOLID PZR {P_psig:F0}psig - RELIEF VALVE OPEN ({state.ReliefFlow:F0}gpm)";
            
            return $"SOLID PZR {P_psig:F0}psig - {margin:F0}Â°F to bubble";
        }
        
        #endregion
        #region Validation
        
        /// <summary>
        /// Validate solid plant pressure physics.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initialization should produce valid state
            var state = Initialize(365f, 100f, 100f);
            if (state.BubbleFormed) valid = false;
            if (state.Pressure != 365f) valid = false;
            if (state.ReliefFlow != 0f) valid = false;
            
            // Test 2: Heating should raise PZR temperature
            float initialT = state.T_pzr;
            Update(ref state, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (state.T_pzr <= initialT) valid = false;
            
            // Test 3: Thermal expansion should cause pressure change
            // (not zero â€” the old bug was zero pressure change)
            // Note: with CVCS active, pressure change will be small but nonzero
            var state2 = Initialize(365f, 100f, 100f);
            float P0 = state2.Pressure;
            // Several steps to accumulate measurable change
            for (int i = 0; i < 100; i++)
                Update(ref state2, 1800f, 75f, 75f, 1100000f, 1f/360f);
            // Pressure should have moved from initial (CVCS is responding but not perfect)
            // Accept any nonzero change as proof the physics is working
            float dP = Math.Abs(state2.Pressure - P0);
            if (dP < 0.01f) valid = false;
            
            // Test 4: Relief valve should open above 450 psig
            float reliefFlow = CalculateReliefFlow(460f, false);
            if (reliefFlow <= 0f) valid = false;
            
            // Test 5: Relief valve should be closed well below setpoint
            float noRelief = CalculateReliefFlow(400f, false);
            if (noRelief != 0f) valid = false;
            
            // Test 6: CVCS should increase letdown when pressure is high
            // v5.4.2.0: With transport delay, the PI output takes N steps to reach
            // LetdownFlow. Run enough steps for the delay to propagate (>6 at 10s/step).
            var stateHigh = Initialize(PlantConstants.SOLID_PLANT_P_HIGH_PSIA, 100f, 100f);
            for (int i = 0; i < 10; i++)
                Update(ref stateHigh, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (stateHigh.LetdownFlow <= 75f) valid = false; // Should be above base

            // Test 7: CVCS should decrease letdown when pressure is low
            // v5.4.2.0: Same transport delay consideration as Test 6.
            var stateLow = Initialize(PlantConstants.SOLID_PLANT_P_LOW_PSIA, 100f, 100f);
            for (int i = 0; i < 10; i++)
                Update(ref stateLow, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (stateLow.LetdownFlow >= 75f) valid = false; // Should be below base
            
            // Test 8: Bubble formation at T_sat
            var stateBubble = Initialize(365f, 100f, 430f);
            stateBubble.T_pzr = 436f; // Above T_sat at 365 psia (~435Â°F)
            Update(ref stateBubble, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (!stateBubble.BubbleFormed) valid = false;
            
            // Test 9: Surge flow should be non-zero when PZR is heating
            // PZR thermal expansion drives water through surge line
            var stateSurge = Initialize(365f, 100f, 100f);
            for (int i = 0; i < 10; i++)
                Update(ref stateSurge, 1800f, 75f, 75f, 1100000f, 1f/360f);
            if (stateSurge.SurgeFlow <= 0f) valid = false;
            
            // Test 10: Surge line heat should be non-zero when T_pzr > T_rcs
            // After a few steps, PZR will be warmer than RCS
            if (stateSurge.SurgeLineHeat_MW <= 0f) valid = false;
            
            // Test 11: v5.0.2 â€” Mass conservation during solid ops heating
            // After 100 heating steps with balanced CVCS, PZR mass should decrease
            // only by cumulative surge transfer (small, physically correct displacement),
            // NOT by thousands of lbm from VÃ—Ï density-driven overwrite.
            var stateMass = Initialize(365f, 100f, 100f);
            float initialPzrMass = stateMass.PzrWaterMass;
            float totalSurgeTransfer = 0f;
            for (int i = 0; i < 100; i++)
            {
                Update(ref stateMass, 1800f, 75f, 75f, 1100000f, 1f/360f);
                totalSurgeTransfer += stateMass.SurgeMassTransfer_lb;
            }
            // Mass change must match cumulative surge transfer within float tolerance
            float massDelta = initialPzrMass - stateMass.PzrWaterMass;
            float massError = Math.Abs(massDelta - totalSurgeTransfer);
            if (massError > 1f) valid = false;  // Within 1 lbm numerical tolerance
            // Mass change should be small (order 10s of lbm, not thousands)
            if (massDelta > 500f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
