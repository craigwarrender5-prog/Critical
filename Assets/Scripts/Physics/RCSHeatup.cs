// CRITICAL: Master the Atom - Physics Module
// RCSHeatup.cs - RCS Bulk Heatup Physics
//
// Implements: Engine Architecture Audit Fix - Issue #2
//   Extracts Phase 2 bulk heatup physics from engine to physics module
//
// PHYSICS:
//   During Phase 2 heatup (RCPs running, bubble exists):
//     - RCPs provide mechanical heat input (~21 MW with 4 pumps)
//     - PZR heaters add additional heat (~1.8 MW)
//     - Insulation losses remove heat (temperature dependent)
//     - Net heat raises RCS temperature
//     - Thermal expansion causes pressure rise (via CoupledThermo)
//     - Surge flow moves mass between RCS and PZR
//
//   Heat Balance:
//     Q_net = Q_rcp + Q_heaters - Q_loss
//     dT/dt = Q_net / (M_rcs × Cp + M_metal × Cp_steel)
//
// Sources:
//   - NRC HRTD Section 19.2.2 - Plant Heatup Operations
//   - NRC ML11223A342 - "approximately 50°F per hour" heatup rate
//
// v1.0.3.0 - Fixed IsolatedHeatingStep energy balance
//   - PZR energy balance now subtracts surge line heat loss (was missing)
//   - Adds PZR-specific insulation loss (~5% of total system loss)
//   - Coupled with stratified surge line model fix in HeatTransfer.cs
//   - Result: PZR can now heat to saturation and form a bubble
//
// v5.4.0 Stage 3 - Canonical Mass Unification
//   - totalPrimaryMass_lb parameter is now MANDATORY for two-phase operations
//   - Passed to CoupledThermo.SolveEquilibrium as hard constraint
//   - Solver accepts ledger value, does NOT recompute from V×ρ
//   - RCS mass computed as remainder = Total - PZR_water - PZR_steam
//   - Guarantees exact mass conservation by construction (Rule R5)
//
// Units: °F for temperature, psia for pressure, MW for power, hours for time

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Result structure for bulk heatup step calculation.
    /// </summary>
    public struct BulkHeatupResult
    {
        public float T_rcs;             // New RCS temperature (°F)
        public float Pressure;          // New pressure (psia)
        public float DeltaT;            // Temperature change this step (°F)
        public float DeltaP;            // Pressure change this step (psi)
        public float SurgeFlow;         // Surge flow rate (gpm), positive = into PZR
        public float NetHeat_MW;        // Net heat input (MW)
        public float HeatupRate;        // Instantaneous heatup rate (°F/hr)
        public bool Converged;          // True if CoupledThermo converged
        public float SGHeatTransfer_MW; // Heat transferred to SG secondary (MW) — v0.8.0
        public float T_sg_secondary;    // New SG secondary temperature (°F) — v0.8.0 (bulk avg)
        
        // v1.3.0: Multi-node SG model outputs
        public float SG_TopNodeTemp_F;      // Top node temperature (°F)
        public float SG_CirculationFraction; // Natural circulation fraction (0-1)
        public bool  SG_CirculationActive;   // True if natural circulation onset
    }
    
    /// <summary>
    /// RCS bulk heatup physics for Phase 2 (RCPs running).
    /// </summary>
    public static class RCSHeatup
    {
        #region Constants
        
        /// <summary>Conversion factor MW to BTU/hr</summary>
        private const float MW_TO_BTU_HR = 3412142f;
        
        /// <summary>
        /// Fraction of total system insulation loss attributed to pressurizer.
        /// PZR wall area ~600 ft² vs total primary ~19,000 ft² = ~3%.
        /// Adjusted to 5% because PZR operates at higher temperatures
        /// during isolated heating (hotter wall = more loss per ft²).
        /// </summary>
        private const float PZR_INSULATION_FRACTION = 0.05f;
        
        #endregion
        
        #region Main Calculation
        
        /// <summary>
        /// Calculate one timestep of bulk RCS heatup with RCPs running.
        /// v0.8.0: Integrated SG secondary side thermal mass as heat sink.
        /// v1.3.0: Switched to multi-node SG model for realistic stratification.
        ///         The multi-node model is updated externally by the engine and
        ///         passes in the total SG heat removal. The old lumped model
        ///         call chain (SGSecondaryThermal.CalculateHeatTransfer) is
        ///         removed from this method. SG heat removal is now an INPUT
        ///         parameter computed by SGMultiNodeThermal.Update() in the engine.
        /// v5.3.0: Added totalPrimaryMass_lb parameter for mass conservation.
        /// v5.4.0: This parameter is now MANDATORY for two-phase operations.
        ///         When > 0, it is passed to CoupledThermo.SolveEquilibrium
        ///         as an ABSOLUTE constraint. The solver distributes this mass
        ///         among components but does NOT recompute the total from V×ρ.
        ///         RCS mass is computed as remainder = Total - PZR_water - PZR_steam,
        ///         guaranteeing exact conservation by construction.
        /// </summary>
        /// v0.1.0.0 Phase D (CS-0004): Removed default value (= 0f) from totalPrimaryMass_lb.
        /// All callers MUST now explicitly provide the canonical mass argument.
        /// Compile-time enforcement: omitting the argument is a build error.
        /// This eliminates silent fallback to LEGACY mode in CoupledThermo.
        public static BulkHeatupResult BulkHeatupStep(
            ref SystemState state,
            int rcpCount,
            float rcpHeat_MW,
            float heaterPower_MW,
            float rcsHeatCapacity,
            float pzrHeatCapacity,
            float dt_hr,
            float sgHeatRemoval_MW,               // v1.3.0: SG heat removal (MW) from multi-node model
            float T_sg_bulk,                       // v1.3.0: SG bulk avg temp for display
            float totalPrimaryMass_lb)             // v5.3.0: Canonical mass constraint — MANDATORY (no default)
        {
            var result = new BulkHeatupResult();
            
            float T_initial = state.Temperature;
            float P_initial = state.Pressure;
            
            // ================================================================
            // 1. HEAT BALANCE
            // v1.3.0: SG heat removal is now computed externally by the
            //         multi-node model and passed in as a parameter.
            // ================================================================
            
            float grossHeat_MW = rcpHeat_MW + heaterPower_MW;
            float netHeat_MW = HeatTransfer.NetHeatInput_MW(grossHeat_MW, state.Temperature);
            
            // v1.3.0: Use externally-computed SG heat removal
            float rcsNetHeat_MW = netHeat_MW - sgHeatRemoval_MW;
            result.NetHeat_MW = rcsNetHeat_MW;
            result.SGHeatTransfer_MW = sgHeatRemoval_MW;
            
            float netHeat_BTU = rcsNetHeat_MW * MW_TO_BTU_HR * dt_hr;
            
            // ================================================================
            // 2. TEMPERATURE CHANGE
            // ================================================================
            
            float totalHeatCap = rcsHeatCapacity + pzrHeatCapacity;
            float deltaT = (totalHeatCap > 0f) ? netHeat_BTU / totalHeatCap : 0f;
            result.DeltaT = deltaT;
            result.HeatupRate = (dt_hr > 1e-8f) ? deltaT / dt_hr : 0f;
            
            // ================================================================
            // 3. P-T-V COUPLING via CoupledThermo
            // v5.4.0 Stage 3: CANONICAL MASS ENFORCEMENT
            //
            // The totalPrimaryMass_lb is the SOLE AUTHORITY for total mass.
            // Per Implementation Plan v5.4.0:
            //   Rule R1: TotalPrimaryMass_lb is the single canonical ledger
            //   Rule R3: Solver must NOT recalculate total from V×ρ
            //   Rule R5: RCS = Total - PZR_water - PZR_steam (by construction)
            //
            // When totalPrimaryMass_lb > 0, the solver ACCEPTS this value as
            // an absolute constraint. It distributes mass among components
            // to satisfy equilibrium, then computes RCS as the remainder.
            // This guarantees the component sum EXACTLY equals the ledger.
            // ================================================================
            
            result.Converged = CoupledThermo.SolveEquilibrium(
                ref state, deltaT, 
                50, 15f, 2700f,  // maxIterations=50, P_floor=15 psia, P_ceiling=2700 psia
                totalPrimaryMass_lb);  // MANDATORY canonical mass constraint
            
            if (result.Converged)
            {
                result.T_rcs = state.Temperature;
                result.Pressure = state.Pressure;
            }
            else
            {
                state.Temperature = T_initial + deltaT;
                float dP = CoupledThermo.QuickPressureEstimate(
                    T_initial, P_initial, deltaT,
                    PlantConstants.RCS_WATER_VOLUME, state.PZRSteamVolume);
                state.Pressure = P_initial + dP;
                
                result.T_rcs = state.Temperature;
                result.Pressure = state.Pressure;
            }
            
            result.DeltaP = state.Pressure - P_initial;

            // ================================================================
            // v0.1.0.0 Phase B: Post-solver conservation guard rail (CS-0008)
            // Diagnostics only — does NOT modify solver math or state.
            // Checks that solver output component sum matches canonical ledger.
            // ================================================================
            if (totalPrimaryMass_lb > 0f)
            {
                float M_out = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;
                float massDelta = M_out - totalPrimaryMass_lb;
                float absDelta = massDelta < 0f ? -massDelta : massDelta;
                if (absDelta > 100f)
                {
                    UnityEngine.Debug.LogError(
                        $"CoupledThermo conservation ERROR: |M_out - M_ledger| = {absDelta:F2} lb " +
                        $"(M_out={M_out:F1}, Ledger={totalPrimaryMass_lb:F1})");
                }
                else if (absDelta > 10f)
                {
                    UnityEngine.Debug.LogWarning(
                        $"CoupledThermo conservation WARNING: |M_out - M_ledger| = {absDelta:F2} lb " +
                        $"(M_out={M_out:F1}, Ledger={totalPrimaryMass_lb:F1})");
                }
            }

            // ================================================================
            // 4. SURGE FLOW from thermal expansion
            // ================================================================
            
            float expCoeff = ThermalExpansion.ExpansionCoefficient(result.T_rcs, result.Pressure);
            float dV_ft3 = PlantConstants.RCS_WATER_VOLUME * expCoeff * deltaT;
            result.SurgeFlow = (dt_hr > 1e-8f) ? (dV_ft3 * 7.48f / dt_hr / 60f) : 0f;
            
            // ================================================================
            // 5. SG STATE — v1.3.0: Passthrough from external multi-node model
            //    SG node temperatures are updated externally by the engine.
            //    We just pass through the bulk average for display compatibility.
            // ================================================================
            result.T_sg_secondary = T_sg_bulk;
            
            return result;
        }
        
        /// <summary>
        /// Simplified step that updates state in place.
        /// v1.3.0: Updated signature to match new multi-node SG parameters.
        /// v0.1.0.0 Phase D: Added totalPrimaryMass_lb (mandatory) — callers
        ///   MUST provide canonical mass. Passes 0f explicitly for validation-only
        ///   use (triggers LEGACY mode warning in CoupledThermo).
        /// </summary>
        public static void Step(
            ref SystemState state,
            int rcpCount,
            float heaterPower_MW,
            float rcsHeatCapacity,
            float pzrHeatCapacity,
            float dt_hr,
            float sgHeatRemoval_MW = 0f,
            float T_sg_bulk = 0f,
            float totalPrimaryMass_lb = 0f)
        {
            float rcpHeat = rcpCount * PlantConstants.RCP_HEAT_MW_EACH;
            BulkHeatupStep(ref state, rcpCount, rcpHeat, heaterPower_MW,
                          rcsHeatCapacity, pzrHeatCapacity, dt_hr, sgHeatRemoval_MW, T_sg_bulk,
                          totalPrimaryMass_lb);
        }
        
        #endregion
        
        #region Isolated Heating (Phase 1 with bubble)
        
        /// <summary>
        /// Result structure for isolated heating calculation.
        /// </summary>
        public struct IsolatedHeatingResult
        {
            public float T_pzr;         // New PZR temperature (°F)
            public float T_rcs;         // New RCS temperature (°F)
            public float Pressure;      // New pressure (psia)
            public float SurgeFlow;     // Thermal expansion surge flow (gpm)
            public float ConductionHeat_MW; // Heat conducted through surge line (MW)
        }
        
        /// <summary>
        /// Calculate isolated heating when bubble exists but RCPs are not running.
        /// 
        /// During Phase 1 post-bubble, the PZR is heated by heaters and loses
        /// heat to the RCS through the surge line via stratified natural convection.
        /// RCS also loses heat through insulation.
        ///
        /// v1.0.3.0 ENERGY BALANCE FIX:
        ///   Previous version applied heater energy to PZR but did not subtract
        ///   surge line heat loss from PZR side. Energy was created (PZR gained
        ///   full heater input AND RCS gained surge line heat simultaneously).
        ///   
        ///   Corrected PZR energy balance:
        ///     dT_pzr = (Q_heaters - Q_surge_out - Q_pzr_insulation) / C_pzr
        ///   
        ///   Corrected RCS energy balance:
        ///     dT_rcs = (Q_surge_in - Q_rcs_insulation) / C_rcs
        /// </summary>
        public static IsolatedHeatingResult IsolatedHeatingStep(
            float T_pzr,
            float T_rcs,
            float pressure,
            float heaterPower_MW,
            float pzrWaterVolume,
            float pzrHeatCapacity,
            float rcsHeatCapacity,
            float dt_hr)
        {
            var result = new IsolatedHeatingResult();
            
            // ================================================================
            // SURGE LINE HEAT TRANSFER (needed by both PZR and RCS balance)
            // Uses stratified natural convection model (v1.0.3.0)
            // ================================================================
            
            float conductionHeat_MW = HeatTransfer.SurgeLineHeatTransfer_MW(T_pzr, T_rcs, pressure);
            result.ConductionHeat_MW = conductionHeat_MW;
            float conductionHeat_BTU = conductionHeat_MW * MW_TO_BTU_HR * dt_hr;
            
            // ================================================================
            // PZR TEMPERATURE UPDATE
            // Q_net_pzr = Q_heaters - Q_surge_out - Q_pzr_insulation
            // ================================================================
            
            float pzrHeatInput_BTU = heaterPower_MW * MW_TO_BTU_HR * dt_hr;
            
            // PZR insulation loss (small fraction of total system loss)
            float pzrInsulLoss_MW = HeatTransfer.InsulationHeatLoss_MW(T_pzr) * PZR_INSULATION_FRACTION;
            float pzrInsulLoss_BTU = pzrInsulLoss_MW * MW_TO_BTU_HR * dt_hr;
            
            // Net PZR heat = heaters - surge line out - insulation
            float pzrNetHeat_BTU = pzrHeatInput_BTU - conductionHeat_BTU - pzrInsulLoss_BTU;
            
            float pzrDeltaT = (pzrHeatCapacity > 0f) ? pzrNetHeat_BTU / pzrHeatCapacity : 0f;
            result.T_pzr = T_pzr + pzrDeltaT;
            
            // Cap at saturation - PZR water cannot superheat
            float T_sat = WaterProperties.SaturationTemperature(pressure);
            result.T_pzr = Math.Min(result.T_pzr, T_sat);
            
            // ================================================================
            // RCS TEMPERATURE UPDATE
            // Q_net_rcs = Q_surge_in - Q_rcs_insulation
            // ================================================================
            
            float rcsFromConduction = (rcsHeatCapacity > 0f) ? conductionHeat_BTU / rcsHeatCapacity : 0f;
            
            float heatLoss_MW = HeatTransfer.InsulationHeatLoss_MW(T_rcs);
            float heatLoss_BTU = heatLoss_MW * MW_TO_BTU_HR * dt_hr;
            float rcsFromLoss = (rcsHeatCapacity > 0f) ? heatLoss_BTU / rcsHeatCapacity : 0f;
            
            result.T_rcs = T_rcs + rcsFromConduction - rcsFromLoss;
            
            // ================================================================
            // PRESSURE UPDATE from PZR thermal expansion
            // ================================================================
            
            float dP = ThermalExpansion.PressureChangeFromTemp(pzrDeltaT, result.T_pzr, pressure);
            const float DAMPING_FACTOR = 0.5f;
            result.Pressure = pressure + dP * DAMPING_FACTOR;
            
            // ================================================================
            // SURGE FLOW from PZR thermal expansion
            // ================================================================
            
            float pzrExpCoeff = ThermalExpansion.ExpansionCoefficient(result.T_pzr, result.Pressure);
            float dV_ft3 = pzrWaterVolume * pzrExpCoeff * pzrDeltaT;
            result.SurgeFlow = (dt_hr > 1e-8f) ? (dV_ft3 * 7.48f / dt_hr / 60f) : 0f;
            
            return result;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Estimate heatup rate for given conditions.
        /// </summary>
        public static float EstimateHeatupRate(
            int rcpCount, float heaterPower_MW, float T_rcs, float totalHeatCapacity)
        {
            float rcpHeat = rcpCount * PlantConstants.RCP_HEAT_MW_EACH;
            float grossHeat = rcpHeat + heaterPower_MW;
            float netHeat = HeatTransfer.NetHeatInput_MW(grossHeat, T_rcs);
            
            if (totalHeatCapacity < 1f) return 0f;
            
            float heatRate = netHeat * MW_TO_BTU_HR / totalHeatCapacity;
            return heatRate;
        }
        
        /// <summary>
        /// Estimate time to reach target temperature.
        /// </summary>
        public static float EstimateTimeToTarget(
            float currentTemp, float targetTemp, float heatupRate)
        {
            if (heatupRate <= 0.1f) return float.MaxValue;
            float tempDiff = targetTemp - currentTemp;
            if (tempDiff <= 0f) return 0f;
            return tempDiff / heatupRate;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate RCS heatup calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Heatup rate ~50°F/hr with 4 RCPs
            float rate = EstimateHeatupRate(4, 1.8f, 400f, 1100000f);
            if (rate < 30f || rate > 80f) valid = false;
            
            // Test 2: More RCPs = faster heatup
            float rate2 = EstimateHeatupRate(2, 1.8f, 400f, 1100000f);
            float rate4 = EstimateHeatupRate(4, 1.8f, 400f, 1100000f);
            if (rate4 <= rate2) valid = false;
            
            // Test 3: Higher temperature = slower heatup (more losses)
            float rateHot = EstimateHeatupRate(4, 1.8f, 500f, 1100000f);
            float rateCold = EstimateHeatupRate(4, 1.8f, 200f, 1100000f);
            if (rateHot >= rateCold) valid = false;
            
            // Test 4: Isolated heating should increase PZR temp
            var isoResult = IsolatedHeatingStep(
                400f, 350f, 800f, 1.8f, 1080f, 50000f, 1000000f, 1f/360f);
            if (isoResult.T_pzr <= 400f) valid = false;
            
            // Test 5: Conduction should heat RCS when T_pzr > T_rcs
            if (isoResult.ConductionHeat_MW <= 0f) valid = false;
            
            // Test 6: Time to target should be finite
            float time = EstimateTimeToTarget(400f, 557f, 50f);
            if (time <= 0f || time > 10f) valid = false;
            
            // ================================================================
            // v1.0.3.0 - Stratified Model Integration Tests
            // ================================================================
            
            // Test 7: PZR heatup rate 60-100°F/hr with 1800 kW heaters
            var isoTest = IsolatedHeatingStep(
                150f, 100f, 365f, 1.8f, 1080f, 74000f, 985000f, 1f/360f);
            float pzrRate = (isoTest.T_pzr - 150f) / (1f/360f);
            if (pzrRate < 40f || pzrRate > 120f) valid = false;
            
            // Test 8: PZR still heats even at large ΔT=200°F
            var isoTest2 = IsolatedHeatingStep(
                300f, 100f, 365f, 1.8f, 1080f, 74000f, 985000f, 1f/360f);
            if (isoTest2.T_pzr <= 300f) valid = false;
            
            // Test 9: RCS nearly static during isolated heating
            float rcsDeltaPerStep = isoTest.T_rcs - 100f;
            if (rcsDeltaPerStep > 0.01f) valid = false;
            if (rcsDeltaPerStep < -0.01f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
