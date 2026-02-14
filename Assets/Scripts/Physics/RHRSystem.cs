// ============================================================================
// CRITICAL: Master the Atom — Residual Heat Removal System Physics Module
// RHRSystem.cs — RHR Thermal Model for Heatup/Cooldown Operations
// ============================================================================
//
// PURPOSE:
//   Models the RHR system thermal behavior during cold shutdown heatup.
//   Provides forced circulation through the core, heat removal via RHR
//   heat exchangers (throttled during heatup), and pump heat input.
//   Handles the operational transition from RHR-dominated cooling to
//   RCP-driven heatup with SG heat sink.
//
// PHYSICS:
//   The RHR system during heatup operates as follows:
//
//   1. RHR pumps take suction from Loop 4 hot leg (T_hot)
//   2. Flow splits: fraction through HX (cooled), remainder bypasses HX
//   3. Cooled and bypass flows recombine → return to all 4 cold legs
//   4. Heat exchanger: Q_hx = UA_eff × LMTD (counter-flow, CCW on shell side)
//   5. Pump heat: ~0.5 MW per pump adds energy to RCS coolant
//   6. Net effect = pump heat - HX removal (positive = heating RCS)
//
//   During heatup, HX is mostly bypassed (85% bypass), so:
//     - HX removal is minimal (~0.1-0.5 MW)
//     - Pump heat dominates (~1.0 MW)
//     - Net effect is slow heating (~5-15°F/hr)
//
//   RHR Operating Modes:
//     STANDBY    — Aligned to ECCS, not connected to RCS
//     COOLING    — Normal cooldown (HX fully engaged, 0% bypass)
//     HEATUP     — HX mostly bypassed (85%), allowing temp rise
//     ISOLATING  — Transitioning from RCS to standby (during RCP start)
//
// TRANSITION SEQUENCE (per NRC HRTD 19.0):
//   1. Start: RHR in HEATUP mode, pumps running, HX bypassed
//   2. RCS heats slowly from pump heat + decay heat
//   3. Hold at ~160°F (cold water addition accident limit)
//   4. Bubble forms in pressurizer at ~230°F
//   5. First RCP started (requires P ≥ 320 psig, bubble exists)
//   6. After all RCPs running → begin RHR isolation
//   7. Close suction valves → RHR in STANDBY (ECCS alignment)
//   8. Letdown transitions from HCV-128 to normal CVCS orifices
//
// SOURCES:
//   - NRC HRTD 5.1 — Residual Heat Removal System (ML11223A219)
//   - NRC HRTD 19.0 — Plant Operations (ML11223A342)
//   - Byron NRC Exam 2019 (ML20054A571) — RHR HX bypass operation
//   - Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md
//
// UNITS:
//   Temperature: °F | Flow: gpm | Pressure: psig
//   Power: MW | Heat Rate: BTU/hr | Time: hours
//
// ARCHITECTURE:
//   - Called by: HeatupSimEngine.StepSimulation()
//   - Uses constants from: PlantConstants.RHR, PlantConstants.Pressure
//   - State owned: RHRState struct (owned by engine, passed by ref)
//   - Returns: RHRResult struct
//   - Pattern: Static module with Initialize() / Update() / GetDiagnosticString()
//     (matches SGMultiNodeThermal.cs pattern)
//
// GOLD STANDARD: Yes (v3.0.0)
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
    // ========================================================================
    // OPERATING MODE ENUM
    // ========================================================================

    /// <summary>
    /// RHR system operating modes.
    /// Determines HX bypass fraction and system alignment.
    /// </summary>
    public enum RHRMode
    {
        /// <summary>RHR aligned to ECCS standby, not connected to RCS</summary>
        Standby,

        /// <summary>Normal cooldown — HX fully engaged (0% bypass)</summary>
        Cooling,

        /// <summary>Heatup — HX mostly bypassed (85%), allowing RCS temp rise</summary>
        Heatup,

        /// <summary>Transitioning from RCS to standby during RCP start sequence</summary>
        Isolating
    }

    // ========================================================================
    // STATE STRUCT
    // Persistent state for RHR system. Owned by engine, passed by ref.
    // ========================================================================

    /// <summary>
    /// Persistent state for the RHR system thermal model.
    /// Created by Initialize(), updated by Update(), read by engine.
    /// </summary>
    public struct RHRState
    {
        /// <summary>Current operating mode</summary>
        public RHRMode Mode;

        /// <summary>Are RHR pumps operating</summary>
        public bool PumpsRunning;

        /// <summary>Number of pumps running (0, 1, or 2)</summary>
        public int PumpsOnline;

        /// <summary>Current total RHR flow in gpm</summary>
        public float FlowRate_gpm;

        /// <summary>
        /// HX bypass fraction (0.0 = full HX cooling, 1.0 = full bypass).
        /// During heatup: ~0.85. During cooldown: 0.0.
        /// Operator-adjustable to control heatup/cooldown rate.
        /// </summary>
        public float HXBypassFraction;

        /// <summary>RCS hot leg temperature entering RHR suction (°F)</summary>
        public float HXInletTemp_F;

        /// <summary>Temperature leaving RHR HX tube side (°F) — cooled stream</summary>
        public float HXOutletTemp_F;

        /// <summary>Mixed temperature after HX and bypass streams recombine (°F)</summary>
        public float MixedReturnTemp_F;

        /// <summary>CCW outlet temperature from RHR HX shell side (°F)</summary>
        public float CCWOutletTemp_F;

        /// <summary>Current heat removed by RHR HX (MW) — positive = cooling</summary>
        public float HeatRemoval_MW;

        /// <summary>Current heat removed by RHR HX (BTU/hr)</summary>
        public float HeatRemoval_BTUhr;

        /// <summary>Heat added by RHR pumps (MW) — always positive when running</summary>
        public float PumpHeatInput_MW;

        /// <summary>
        /// Net thermal effect on RCS (MW).
        /// Positive = net heating (pump heat > HX removal).
        /// Negative = net cooling (HX removal > pump heat).
        /// </summary>
        public float NetHeatEffect_MW;

        /// <summary>Net thermal effect on RCS (BTU/hr)</summary>
        public float NetHeatEffect_BTUhr;

        /// <summary>True when RHR suction valves (8701/8702) are open to RCS</summary>
        public bool SuctionValvesOpen;

        /// <summary>Letdown flow via HCV-128 cross-connect to CVCS (gpm)</summary>
        public float LetdownFlow_gpm;

        /// <summary>LMTD of RHR HX at current conditions (°F)</summary>
        public float LMTD_F;

        /// <summary>Effective UA being used (after fouling and bypass) in BTU/(hr·°F)</summary>
        public float EffectiveUA;
    }

    // ========================================================================
    // RESULT STRUCT
    // Returned by Update() for engine consumption.
    // ========================================================================

    /// <summary>
    /// Result of a single timestep update for the RHR system model.
    /// Returned by Update(). Engine reads results; module never mutates
    /// engine state directly (except through state ref).
    /// </summary>
    public struct RHRResult
    {
        /// <summary>Net thermal effect on RCS (MW). Positive = heating.</summary>
        public float NetHeatEffect_MW;

        /// <summary>Net thermal effect on RCS (BTU/hr). Positive = heating.</summary>
        public float NetHeatEffect_BTUhr;

        /// <summary>Heat removed by HX (MW)</summary>
        public float HeatRemoval_MW;

        /// <summary>Heat added by pumps (MW)</summary>
        public float PumpHeatInput_MW;

        /// <summary>Mixed return temperature to RCS cold legs (°F)</summary>
        public float MixedReturnTemp_F;

        /// <summary>Current operating mode</summary>
        public RHRMode Mode;

        /// <summary>True if RHR is connected to and affecting RCS</summary>
        public bool IsActive;
    }

    // ========================================================================
    // MODULE CLASS
    // ========================================================================

    /// <summary>
    /// RHR system thermal model for heatup and cooldown operations.
    /// Models both RHR trains as a single lumped system (both trains
    /// operating identically with combined parameters).
    ///
    /// Called by HeatupSimEngine.StepSimulation().
    /// Returns RHRResult.
    /// See file header for physics basis and NRC sources.
    /// </summary>
    public static class RHRSystem
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        #region Constants

        /// <summary>Conversion: MW to BTU/hr</summary>
        private const float MW_TO_BTU_HR = 3.412e6f;

        /// <summary>
        /// Water specific heat approximation for RHR LMTD calculation (BTU/(lb·°F)).
        /// At 100-350°F range, cp ≈ 1.0 BTU/(lb·°F) for subcooled water.
        /// Slight variation (~0.998-1.012) is negligible for RHR HX calculation.
        /// </summary>
        private const float CP_WATER_APPROX = 1.0f;

        /// <summary>
        /// Water density approximation for flow-to-mass conversion (lb/gal).
        /// At 100-200°F: ~8.2 lb/gal. At 300°F: ~7.6 lb/gal.
        /// Using ~8.0 as reasonable average for cold shutdown conditions.
        /// </summary>
        private const float WATER_DENSITY_LB_PER_GAL = 8.0f;

        /// <summary>
        /// Minimum LMTD for HX calculation (°F).
        /// Below this, HX heat transfer is negligible. Prevents ln(0) issues.
        /// </summary>
        private const float MIN_LMTD = 0.1f;

        /// <summary>
        /// Minimum temperature difference for meaningful HX calculation (°F).
        /// If RCS temp is within this range of CCW temp, HX does nothing useful.
        /// </summary>
        private const float MIN_DELTA_T_HX = 1.0f;

        /// <summary>
        /// RHR isolation ramp time in hours.
        /// When isolating, flow ramps down over this duration to avoid
        /// thermal transients in the RCS.
        /// Source: Operating practice — gradual valve closure over ~5 minutes
        /// </summary>
        private const float ISOLATION_RAMP_HR = 5f / 60f;

        #endregion

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        #region Public API

        /// <summary>
        /// Initialize RHR state for cold shutdown heatup conditions.
        /// Both trains running, HX bypassed for heatup, suction valves open.
        /// Called once at simulation start.
        /// </summary>
        /// <param name="initialRcsTemp_F">Starting RCS temperature (°F)</param>
        /// <returns>Initialized RHRState for heatup mode</returns>
        public static RHRState Initialize(float initialRcsTemp_F)
        {
            var state = new RHRState();

            state.Mode = RHRMode.Heatup;
            state.PumpsRunning = true;
            state.PumpsOnline = PlantConstants.RHR_PUMP_COUNT;  // Both trains
            state.FlowRate_gpm = PlantConstants.RHR_PUMP_FLOW_GPM_TOTAL;
            state.HXBypassFraction = PlantConstants.RHR_HX_BYPASS_FRACTION_HEATUP;

            state.HXInletTemp_F = initialRcsTemp_F;
            state.HXOutletTemp_F = initialRcsTemp_F;
            state.MixedReturnTemp_F = initialRcsTemp_F;
            state.CCWOutletTemp_F = PlantConstants.RHR_CCW_INLET_TEMP_F;

            state.HeatRemoval_MW = 0f;
            state.HeatRemoval_BTUhr = 0f;
            state.PumpHeatInput_MW = PlantConstants.RHR_PUMP_HEAT_MW_TOTAL;
            state.NetHeatEffect_MW = PlantConstants.RHR_PUMP_HEAT_MW_TOTAL;
            state.NetHeatEffect_BTUhr = PlantConstants.RHR_PUMP_HEAT_MW_TOTAL * MW_TO_BTU_HR;

            state.SuctionValvesOpen = true;
            state.LetdownFlow_gpm = PlantConstants.RHR_CVCS_LETDOWN_FLOW_GPM;

            state.LMTD_F = 0f;
            state.EffectiveUA = 0f;

            return state;
        }

        /// <summary>
        /// Initialize RHR state for standby (RHR not in service).
        /// Used when simulation starts with RCPs already running.
        /// </summary>
        /// <returns>Initialized RHRState in standby mode</returns>
        public static RHRState InitializeStandby()
        {
            var state = new RHRState();

            state.Mode = RHRMode.Standby;
            state.PumpsRunning = false;
            state.PumpsOnline = 0;
            state.FlowRate_gpm = 0f;
            state.HXBypassFraction = 0f;

            state.HXInletTemp_F = 0f;
            state.HXOutletTemp_F = 0f;
            state.MixedReturnTemp_F = 0f;
            state.CCWOutletTemp_F = PlantConstants.RHR_CCW_INLET_TEMP_F;

            state.HeatRemoval_MW = 0f;
            state.HeatRemoval_BTUhr = 0f;
            state.PumpHeatInput_MW = 0f;
            state.NetHeatEffect_MW = 0f;
            state.NetHeatEffect_BTUhr = 0f;

            state.SuctionValvesOpen = false;
            state.LetdownFlow_gpm = 0f;

            state.LMTD_F = 0f;
            state.EffectiveUA = 0f;

            return state;
        }

        /// <summary>
        /// Advance the RHR system model by one timestep.
        /// Called every physics step by HeatupSimEngine.
        ///
        /// Physics sequence:
        /// 1. Check interlocks (auto-isolation on high pressure)
        /// 2. Handle mode transitions (isolation ramp-down)
        /// 3. Calculate HX heat removal (UA × LMTD method)
        /// 4. Calculate pump heat input
        /// 5. Compute net thermal effect on RCS
        /// 6. Calculate mixed return temperature
        /// </summary>
        /// <param name="state">RHR state (modified in place)</param>
        /// <param name="T_rcs_F">Current RCS average temperature (°F)</param>
        /// <param name="P_rcs_psig">Current RCS pressure (psig)</param>
        /// <param name="rcpsRunning">Number of RCPs currently running (0-4)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <returns>Result struct with net thermal effect and diagnostics</returns>
        public static RHRResult Update(
            ref RHRState state,
            float T_rcs_F,
            float P_rcs_psig,
            int rcpsRunning,
            float dt_hr)
        {
            var result = new RHRResult();

            // ================================================================
            // 1. INTERLOCK CHECK — Auto-isolation on high pressure
            // ================================================================
            if (state.SuctionValvesOpen && P_rcs_psig >= PlantConstants.RHR_SUCTION_VALVE_AUTO_CLOSE_PSIG)
            {
                // Forced isolation — suction valves auto-close at 585 psig
                state.Mode = RHRMode.Standby;
                state.SuctionValvesOpen = false;
                state.PumpsRunning = false;
                state.PumpsOnline = 0;
                state.FlowRate_gpm = 0f;
                state.LetdownFlow_gpm = 0f;
                Debug.Log($"[RHR] AUTO-ISOLATION: P_rcs = {P_rcs_psig:F0} psig >= {PlantConstants.RHR_SUCTION_VALVE_AUTO_CLOSE_PSIG:F0} psig limit");
            }

            // ================================================================
            // 2. MODE-SPECIFIC BEHAVIOR
            // ================================================================
            switch (state.Mode)
            {
                case RHRMode.Standby:
                    UpdateStandby(ref state);
                    break;

                case RHRMode.Heatup:
                case RHRMode.Cooling:
                    UpdateActive(ref state, T_rcs_F, dt_hr);
                    break;

                case RHRMode.Isolating:
                    UpdateIsolating(ref state, T_rcs_F, dt_hr);
                    break;
            }

            // ================================================================
            // 3. POPULATE RESULT
            // ================================================================
            result.NetHeatEffect_MW = state.NetHeatEffect_MW;
            result.NetHeatEffect_BTUhr = state.NetHeatEffect_BTUhr;
            result.HeatRemoval_MW = state.HeatRemoval_MW;
            result.PumpHeatInput_MW = state.PumpHeatInput_MW;
            result.MixedReturnTemp_F = state.MixedReturnTemp_F;
            result.Mode = state.Mode;
            result.IsActive = state.Mode == RHRMode.Heatup ||
                              state.Mode == RHRMode.Cooling ||
                              state.Mode == RHRMode.Isolating;

            return result;
        }

        /// <summary>
        /// Command RHR to begin isolation sequence.
        /// Called by engine when RCPs are started and RHR is no longer needed.
        /// Flow ramps down gradually to avoid thermal transients.
        /// </summary>
        /// <param name="state">RHR state (modified in place)</param>
        public static void BeginIsolation(ref RHRState state)
        {
            if (state.Mode == RHRMode.Standby) return;  // Already isolated

            state.Mode = RHRMode.Isolating;
            Debug.Log("[RHR] Isolation sequence initiated — ramping down flow");
        }

        /// <summary>
        /// Set HX bypass fraction (operator control).
        /// Adjusts the fraction of RHR flow that bypasses the heat exchanger.
        /// Higher bypass = less cooling = faster heatup.
        /// </summary>
        /// <param name="state">RHR state (modified in place)</param>
        /// <param name="bypassFraction">Bypass fraction (0.0 to 1.0)</param>
        public static void SetHXBypass(ref RHRState state, float bypassFraction)
        {
            state.HXBypassFraction = Mathf.Clamp01(bypassFraction);
        }

        /// <summary>
        /// Get a summary string for logging/diagnostics.
        /// </summary>
        public static string GetDiagnosticString(RHRState state)
        {
            string modeStr = state.Mode.ToString().ToUpper();
            if (!state.PumpsRunning)
            {
                return $"RHR [{modeStr}] Pumps OFF | Suction valves {(state.SuctionValvesOpen ? "OPEN" : "CLOSED")}";
            }

            return $"RHR [{modeStr}] Pumps={state.PumpsOnline} | " +
                   $"Flow={state.FlowRate_gpm:F0} gpm | " +
                   $"Bypass={state.HXBypassFraction:P0} | " +
                   $"T_in={state.HXInletTemp_F:F1}°F → T_out={state.HXOutletTemp_F:F1}°F → T_mix={state.MixedReturnTemp_F:F1}°F | " +
                   $"Q_hx={state.HeatRemoval_MW:F3} MW | Q_pump={state.PumpHeatInput_MW:F3} MW | " +
                   $"Q_net={state.NetHeatEffect_MW:+0.000;-0.000} MW | " +
                   $"LMTD={state.LMTD_F:F1}°F | UA_eff={state.EffectiveUA:F0}";
        }

        #endregion

        // ====================================================================
        // PRIVATE METHODS
        // ====================================================================

        #region Private Methods

        /// <summary>
        /// Update for STANDBY mode — all outputs zeroed, no thermal effect.
        /// </summary>
        private static void UpdateStandby(ref RHRState state)
        {
            state.PumpsRunning = false;
            state.PumpsOnline = 0;
            state.FlowRate_gpm = 0f;
            state.SuctionValvesOpen = false;
            state.LetdownFlow_gpm = 0f;

            state.HXInletTemp_F = 0f;
            state.HXOutletTemp_F = 0f;
            state.MixedReturnTemp_F = 0f;
            state.CCWOutletTemp_F = PlantConstants.RHR_CCW_INLET_TEMP_F;

            state.HeatRemoval_MW = 0f;
            state.HeatRemoval_BTUhr = 0f;
            state.PumpHeatInput_MW = 0f;
            state.NetHeatEffect_MW = 0f;
            state.NetHeatEffect_BTUhr = 0f;
            state.LMTD_F = 0f;
            state.EffectiveUA = 0f;
        }

        /// <summary>
        /// Update for active modes (HEATUP or COOLING).
        /// Calculates HX heat removal and pump heat input.
        /// </summary>
        private static void UpdateActive(ref RHRState state, float T_rcs_F, float dt_hr)
        {
            // ----- Flow -----
            state.FlowRate_gpm = PlantConstants.RHR_PUMP_FLOW_GPM_TOTAL *
                                 ((float)state.PumpsOnline / PlantConstants.RHR_PUMP_COUNT);

            // ----- Inlet temperature (from RCS hot leg) -----
            state.HXInletTemp_F = T_rcs_F;

            // ----- HX Heat Removal Calculation -----
            CalculateHXHeatRemoval(ref state, T_rcs_F);

            // ============================================================
            // v0.3.0.0 (CS-0033 Fix A): FLOW-COUPLED PUMP HEAT
            // Pump mechanical energy transfers to RCS coolant ONLY when a
            // valid hydraulic coupling exists: suction valves open AND
            // actual flow > 0 gpm. When uncoupled (no flow or valves
            // closed), pump energy is dissipated as bearing/casing heat
            // to containment ambient — not modeled as RCS heat input.
            //
            // This corrects the unconditional injection of 1 MW pump heat
            // that was previously applied whenever Mode != Standby
            // regardless of hydraulic state.
            // ============================================================
            bool hydraulicCoupled = state.SuctionValvesOpen && state.FlowRate_gpm > 0f;
            if (hydraulicCoupled)
            {
                state.PumpHeatInput_MW = PlantConstants.RHR_PUMP_HEAT_MW_EACH * state.PumpsOnline;
            }
            else
            {
                state.PumpHeatInput_MW = 0f;
            }

            // ----- Net Effect -----
            // Positive = net heating (pump adds more than HX removes)
            // Negative = net cooling (HX removes more than pump adds)
            state.NetHeatEffect_MW = state.PumpHeatInput_MW - state.HeatRemoval_MW;
            state.NetHeatEffect_BTUhr = state.NetHeatEffect_MW * MW_TO_BTU_HR;

            // ----- Letdown flow (active when suction valves open) -----
            state.LetdownFlow_gpm = state.SuctionValvesOpen
                ? PlantConstants.RHR_CVCS_LETDOWN_FLOW_GPM
                : 0f;
        }

        /// <summary>
        /// Update for ISOLATING mode.
        /// Ramps flow down to zero over ISOLATION_RAMP_HR, then transitions
        /// to STANDBY. Thermal effects decrease proportionally with flow.
        /// </summary>
        private static void UpdateIsolating(ref RHRState state, float T_rcs_F, float dt_hr)
        {
            // Ramp flow down
            float rampRate = PlantConstants.RHR_PUMP_FLOW_GPM_TOTAL / ISOLATION_RAMP_HR;
            state.FlowRate_gpm -= rampRate * dt_hr;

            if (state.FlowRate_gpm <= 0f)
            {
                // Isolation complete
                state.FlowRate_gpm = 0f;
                state.Mode = RHRMode.Standby;
                state.SuctionValvesOpen = false;
                state.PumpsRunning = false;
                state.PumpsOnline = 0;
                state.LetdownFlow_gpm = 0f;
                UpdateStandby(ref state);
                Debug.Log("[RHR] Isolation COMPLETE — aligned to ECCS standby");
                return;
            }

            // During ramp-down, scale all thermal effects by flow fraction
            float flowFraction = state.FlowRate_gpm / PlantConstants.RHR_PUMP_FLOW_GPM_TOTAL;

            state.HXInletTemp_F = T_rcs_F;
            CalculateHXHeatRemoval(ref state, T_rcs_F);

            // Scale HX removal by flow fraction (less flow = less heat transfer)
            state.HeatRemoval_MW *= flowFraction;
            state.HeatRemoval_BTUhr *= flowFraction;

            // v0.3.0.0 (CS-0033): Pump heat scales with flow fraction — flow-coupled.
            // As suction valves close, flow decreases and pump energy transfer
            // to coolant decreases proportionally. At zero flow, pump heat = 0.
            state.PumpHeatInput_MW = PlantConstants.RHR_PUMP_HEAT_MW_TOTAL * flowFraction;

            // Net effect
            state.NetHeatEffect_MW = state.PumpHeatInput_MW - state.HeatRemoval_MW;
            state.NetHeatEffect_BTUhr = state.NetHeatEffect_MW * MW_TO_BTU_HR;

            // Letdown ramps down with flow
            state.LetdownFlow_gpm = PlantConstants.RHR_CVCS_LETDOWN_FLOW_GPM * flowFraction;
        }

        /// <summary>
        /// Calculate RHR heat exchanger heat removal using UA-LMTD method.
        ///
        /// The RHR HX is a counter-flow shell-and-tube exchanger:
        ///   Tube side: RCS coolant (hot fluid)
        ///   Shell side: CCW (cold fluid, assumed constant inlet temperature)
        ///
        /// Q_hx = UA_effective × LMTD
        ///
        /// Where:
        ///   UA_effective = UA_total × fouling_factor × (1 - bypass_fraction)
        ///   
        ///   LMTD = (ΔT1 - ΔT2) / ln(ΔT1 / ΔT2)
        ///   ΔT1 = T_rcs_in - T_ccw_out    (hot end)
        ///   ΔT2 = T_rcs_out - T_ccw_in    (cold end)
        ///
        /// The CCW outlet temperature is estimated from energy balance:
        ///   T_ccw_out = T_ccw_in + Q / (m_ccw × cp)
        ///
        /// For the initial estimate, we use a simplified approach where
        /// CCW outlet is iteratively estimated. Since CCW flow is high
        /// relative to heat load during heatup, T_ccw_out ≈ T_ccw_in + small δ.
        ///
        /// Source: NRC HRTD 5.1, standard HX design methodology
        /// </summary>
        private static void CalculateHXHeatRemoval(ref RHRState state, float T_rcs_F)
        {
            float T_ccw_in = PlantConstants.RHR_CCW_INLET_TEMP_F;

            // If RCS is close to or below CCW temperature, no meaningful HX cooling
            if (T_rcs_F - T_ccw_in < MIN_DELTA_T_HX)
            {
                state.HXOutletTemp_F = T_rcs_F;
                state.MixedReturnTemp_F = T_rcs_F;
                state.CCWOutletTemp_F = T_ccw_in;
                state.HeatRemoval_MW = 0f;
                state.HeatRemoval_BTUhr = 0f;
                state.LMTD_F = 0f;
                state.EffectiveUA = 0f;
                return;
            }

            // Effective UA: accounts for fouling and bypass fraction
            // Only the non-bypassed fraction of flow passes through HX
            float flowThroughHX = 1f - state.HXBypassFraction;
            float UA_eff = PlantConstants.RHR_HX_UA_TOTAL
                         * PlantConstants.RHR_HX_FOULING_FACTOR
                         * flowThroughHX;
            state.EffectiveUA = UA_eff;

            if (UA_eff < 1f)
            {
                // Essentially full bypass — no HX cooling
                state.HXOutletTemp_F = T_rcs_F;
                state.MixedReturnTemp_F = T_rcs_F;
                state.CCWOutletTemp_F = T_ccw_in;
                state.HeatRemoval_MW = 0f;
                state.HeatRemoval_BTUhr = 0f;
                state.LMTD_F = 0f;
                return;
            }

            // ----- Iterative LMTD solution -----
            // First estimate: assume T_ccw_out ≈ T_ccw_in (high CCW flow approximation)
            // Then refine with one iteration using energy balance.

            // Mass flow rates for energy balance
            float m_rcs_hx = state.FlowRate_gpm * flowThroughHX
                           * WATER_DENSITY_LB_PER_GAL * 60f;  // lb/hr
            float m_ccw = PlantConstants.RHR_CCW_FLOW_GPM_EACH
                        * PlantConstants.RHR_HX_COUNT
                        * WATER_DENSITY_LB_PER_GAL * 60f;  // lb/hr (both HXs)

            // Iteration 1: estimate Q with T_ccw_out ≈ T_ccw_in
            float T_ccw_out_est = T_ccw_in;
            float lmtd = CalculateLMTD(T_rcs_F, T_ccw_in, T_rcs_F, T_ccw_out_est);
            float Q_est = UA_eff * lmtd;  // BTU/hr

            // Calculate tube-side outlet temp from energy balance
            float T_rcs_out;
            if (m_rcs_hx > 1f)
            {
                T_rcs_out = T_rcs_F - Q_est / (m_rcs_hx * CP_WATER_APPROX);
                T_rcs_out = Mathf.Max(T_rcs_out, T_ccw_in);  // Cannot cool below CCW
            }
            else
            {
                T_rcs_out = T_rcs_F;
            }

            // Estimate CCW outlet from energy balance
            if (m_ccw > 1f)
            {
                T_ccw_out_est = T_ccw_in + Q_est / (m_ccw * CP_WATER_APPROX);
                T_ccw_out_est = Mathf.Min(T_ccw_out_est, T_rcs_F);  // Cannot exceed RCS
            }

            // Iteration 2: refine with updated temperatures
            lmtd = CalculateLMTD(T_rcs_F, T_ccw_out_est, T_rcs_out, T_ccw_in);
            float Q_hx = UA_eff * lmtd;  // BTU/hr

            // Final tube-side outlet temperature
            if (m_rcs_hx > 1f)
            {
                T_rcs_out = T_rcs_F - Q_hx / (m_rcs_hx * CP_WATER_APPROX);
                T_rcs_out = Mathf.Max(T_rcs_out, T_ccw_in);
            }
            else
            {
                T_rcs_out = T_rcs_F;
            }

            // Final CCW outlet
            if (m_ccw > 1f)
            {
                T_ccw_out_est = T_ccw_in + Q_hx / (m_ccw * CP_WATER_APPROX);
            }

            // ----- Mixed return temperature -----
            // Bypass stream at T_rcs, HX stream at T_rcs_out, weighted by flow fraction
            float T_mixed = T_rcs_F * state.HXBypassFraction
                          + T_rcs_out * flowThroughHX;

            // ----- Store results -----
            state.HXOutletTemp_F = T_rcs_out;
            state.MixedReturnTemp_F = T_mixed;
            state.CCWOutletTemp_F = T_ccw_out_est;
            state.LMTD_F = lmtd;
            state.HeatRemoval_BTUhr = Q_hx;
            state.HeatRemoval_MW = Q_hx / MW_TO_BTU_HR;
        }

        /// <summary>
        /// Calculate Log Mean Temperature Difference for counter-flow HX.
        ///
        /// LMTD = (ΔT1 - ΔT2) / ln(ΔT1 / ΔT2)
        ///
        /// Where for counter-flow:
        ///   ΔT1 = T_hot_in - T_cold_out   (hot end)
        ///   ΔT2 = T_hot_out - T_cold_in   (cold end)
        ///
        /// Special case: if ΔT1 ≈ ΔT2, LMTD = ΔT1 (L'Hôpital's rule)
        /// </summary>
        private static float CalculateLMTD(
            float T_hot_in, float T_cold_out,
            float T_hot_out, float T_cold_in)
        {
            float dT1 = T_hot_in - T_cold_out;   // Hot end
            float dT2 = T_hot_out - T_cold_in;    // Cold end

            // Ensure both positive (otherwise HX direction is wrong)
            dT1 = Mathf.Max(dT1, MIN_LMTD);
            dT2 = Mathf.Max(dT2, MIN_LMTD);

            // Special case: equal ΔTs (avoid ln(1) = 0 → division by zero)
            float ratio = dT1 / dT2;
            if (Mathf.Abs(ratio - 1f) < 0.001f)
            {
                return (dT1 + dT2) * 0.5f;
            }

            // Standard LMTD
            return (dT1 - dT2) / Mathf.Log(ratio);
        }

        #endregion

        // ====================================================================
        // VALIDATION
        // ====================================================================

        #region Validation

        /// <summary>
        /// Validate the RHR system model produces realistic results.
        /// </summary>
        public static bool ValidateModel()
        {
            bool valid = true;

            // Test 1: Initialize at 100°F in heatup mode
            var state = Initialize(100f);
            if (state.Mode != RHRMode.Heatup)
            {
                Debug.LogWarning("[RHR Validation] Test 1 FAIL: Mode not HEATUP after init");
                valid = false;
            }
            if (!state.PumpsRunning || state.PumpsOnline != 2)
            {
                Debug.LogWarning("[RHR Validation] Test 1 FAIL: Pumps not running (both trains)");
                valid = false;
            }
            if (Math.Abs(state.HXBypassFraction - PlantConstants.RHR_HX_BYPASS_FRACTION_HEATUP) > 0.01f)
            {
                Debug.LogWarning("[RHR Validation] Test 1 FAIL: HX bypass not at heatup setting");
                valid = false;
            }

            // Test 2: One step at 150°F, heatup mode — net should be positive (heating)
            state = Initialize(150f);
            var result = Update(ref state, 150f, 350f, 0, 1f / 360f);
            if (result.NetHeatEffect_MW <= 0f)
            {
                Debug.LogWarning($"[RHR Validation] Test 2 FAIL: Net={result.NetHeatEffect_MW:F3} MW (expected positive in heatup)");
                valid = false;
            }
            // Net should be ~0.5-1.5 MW (pump heat minus minimal HX removal)
            if (result.NetHeatEffect_MW < 0.3f || result.NetHeatEffect_MW > 2.0f)
            {
                Debug.LogWarning($"[RHR Validation] Test 2 FAIL: Net={result.NetHeatEffect_MW:F3} MW (expected 0.3-2.0 MW)");
                valid = false;
            }

            // Test 3: Cooling mode (0% bypass) should remove heat (net negative at 300°F)
            state = Initialize(300f);
            state.Mode = RHRMode.Cooling;
            state.HXBypassFraction = PlantConstants.RHR_HX_BYPASS_FRACTION_COOLDOWN;
            result = Update(ref state, 300f, 350f, 0, 1f / 360f);
            if (result.NetHeatEffect_MW >= 0f)
            {
                Debug.LogWarning($"[RHR Validation] Test 3 FAIL: Net={result.NetHeatEffect_MW:F3} MW (expected negative in cooling)");
                valid = false;
            }

            // Test 4: Standby mode — no thermal effect
            state = InitializeStandby();
            result = Update(ref state, 200f, 350f, 0, 1f / 360f);
            if (Math.Abs(result.NetHeatEffect_MW) > 0.001f)
            {
                Debug.LogWarning($"[RHR Validation] Test 4 FAIL: Net={result.NetHeatEffect_MW:F3} MW (expected 0 in standby)");
                valid = false;
            }

            // Test 5: Auto-isolation at high pressure
            state = Initialize(200f);
            result = Update(ref state, 200f, 600f, 0, 1f / 360f);  // 600 psig > 585 limit
            if (state.Mode != RHRMode.Standby)
            {
                Debug.LogWarning("[RHR Validation] Test 5 FAIL: Did not auto-isolate at 600 psig");
                valid = false;
            }

            // Test 6: HX heat removal sanity at 300°F with full HX engagement
            // Expected: UA_eff = 72000 × 0.85 × 1.0 = 61,200 BTU/(hr·°F)
            // LMTD ≈ ~170°F (300 - 95 = 205 hot end, ~150 cold end)
            // Q ≈ 61,200 × ~170 ≈ ~10 × 10⁶ BTU/hr ≈ ~3 MW
            state = Initialize(300f);
            state.Mode = RHRMode.Cooling;
            state.HXBypassFraction = 0f;
            Update(ref state, 300f, 350f, 0, 1f / 360f);
            if (state.HeatRemoval_MW < 1.0f || state.HeatRemoval_MW > 8.0f)
            {
                Debug.LogWarning($"[RHR Validation] Test 6 FAIL: Q_hx={state.HeatRemoval_MW:F2} MW at 300°F cooling (expected 1-8 MW)");
                valid = false;
            }

            // Test 7: Heatup mode HX removal should be very small (mostly bypassed)
            state = Initialize(150f);
            Update(ref state, 150f, 350f, 0, 1f / 360f);
            if (state.HeatRemoval_MW > 0.5f)
            {
                Debug.LogWarning($"[RHR Validation] Test 7 FAIL: Q_hx={state.HeatRemoval_MW:F3} MW in heatup (expected < 0.5 MW)");
                valid = false;
            }

            // Test 8: Pump heat should match constants when hydraulically coupled
            state = Initialize(100f);
            Update(ref state, 100f, 350f, 0, 1f / 360f);
            if (Math.Abs(state.PumpHeatInput_MW - PlantConstants.RHR_PUMP_HEAT_MW_TOTAL) > 0.01f)
            {
                Debug.LogWarning($"[RHR Validation] Test 8 FAIL: Pump heat={state.PumpHeatInput_MW:F3} MW (expected {PlantConstants.RHR_PUMP_HEAT_MW_TOTAL:F1})");
                valid = false;
            }

            // Test 9: v0.3.0.0 (CS-0033) — Pump heat = 0 when hydraulically uncoupled
            // Pumps online but suction valves closed → no RCS heat transfer
            state = Initialize(100f);
            state.SuctionValvesOpen = false;
            result = Update(ref state, 100f, 200f, 0, 1f / 360f);
            if (state.PumpHeatInput_MW > 0.001f)
            {
                Debug.LogWarning($"[RHR Validation] Test 9 FAIL: Pump heat={state.PumpHeatInput_MW:F3} MW with valves CLOSED (expected 0)");
                valid = false;
            }

            // Test 10: v0.3.0.0 (CS-0033) — Pump heat = 0 when flow = 0
            // Suction valves open but flow somehow zero → no heat transfer
            state = Initialize(100f);
            state.FlowRate_gpm = 0f;
            Update(ref state, 100f, 200f, 0, 1f / 360f);
            // Note: UpdateActive recalculates FlowRate_gpm from PumpsOnline, so
            // flow will be restored. This test verifies that if PumpsOnline=0
            // (which would give FlowRate=0), pump heat is zero.
            state = Initialize(100f);
            state.PumpsOnline = 0;
            state.PumpsRunning = false;
            Update(ref state, 100f, 200f, 0, 1f / 360f);
            if (state.PumpHeatInput_MW > 0.001f)
            {
                Debug.LogWarning($"[RHR Validation] Test 10 FAIL: Pump heat={state.PumpHeatInput_MW:F3} MW with 0 pumps online (expected 0)");
                valid = false;
            }

            if (valid)
                Debug.Log("[RHR] All validation tests PASSED");
            else
                Debug.LogError("[RHR] Validation FAILED — check warnings above");

            return valid;
        }

        #endregion
    }
}
