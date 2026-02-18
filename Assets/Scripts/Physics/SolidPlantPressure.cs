// CRITICAL: Master the Atom - Physics Module
// SolidPlantPressure.cs - Solid Plant Pressure-Volume-Temperature Coupling
//
// Implements: Solid pressurizer operations from cold shutdown through bubble formation
//
// PHYSICS:
//   During cold shutdown the pressurizer is water-solid (no steam bubble).
//   Pressure is controlled by the CVCS charging/letdown flow balance.
//   As PZR heaters warm the water, thermal expansion creates excess volume.
//   The CVCS must remove that excess volume to keep pressure in band.
//   If CVCS cannot keep up, the RHR relief valve opens at 450 psig.
//
//   The fundamental equation is:
//     dP/dt = (dV_thermal/dt - dV_cvcs/dt) / (V_total * kappa)
//   where:
//     dV_thermal/dt = V * beta * dT/dt   (thermal expansion rate)
//     dV_cvcs/dt    = (letdown - charging) / rho  (net volume removal by CVCS)
//     kappa         = isothermal compressibility of water
//
// Sources:
//   - NRC HRTD 19.2.1 - Solid Plant Operations
//   - NRC HRTD 19.2.2 - Bubble Formation
//   - NRC HRTD 4.1    - CVCS Operations
//
// Units: psia for pressure, Â°F for temperature, ftÂ³ for volume,
//        gpm for flow, lb for mass, seconds for time

using System;

namespace Critical.Physics
{
    /// <summary>
    /// State container for solid plant pressure control.
    /// Tracks all variables needed for the pressure-volume-temperature coupling
    /// during water-solid pressurizer operations.
    /// </summary>
    public struct SolidPlantState
    {
        // Primary state
        public float Pressure;              // psia
        public float T_pzr;                 // Pressurizer water temperature (Â°F)
        public float T_rcs;                 // RCS bulk temperature (Â°F)
        
        // Pressurizer thermal state
        public float HeaterEffectivePower;  // kW (after thermal lag)
        public float PzrWaterMass;          // lb (conserved â€” updated only by surge transfer)
        public float PzrWallTemp;           // Â°F
        
        // v5.0.2: Mass conservation diagnostics
        public float PzrDensity;            // lbm/ftÂ³ â€” current PZR water density
        public float PzrVolumeImplied;      // ftÂ³ â€” PzrWaterMass / PzrDensity (display only)
        public float PzrMassFlowRate;       // lbm/hr â€” net mass flow rate (surge, for logging)
        public float SurgeMassTransfer_lb;  // lbm â€” mass transferred PZRâ†’RCS this step (for engine)
        
        // CVCS controller state
        public float ControllerIntegral;    // Integral error accumulator (gpmÂ·sec)
        public float LetdownFlow;           // Current letdown flow (gpm)
        public float ChargingFlow;          // Current charging flow (gpm)

        // CVCS actuator dynamics state
        public float LetdownAdjustCmd;      // gpm â€” raw PI controller output (before lag/slew)
        public float LetdownAdjustEff;      // gpm â€” effective adjustment (after lag + slew)
        public bool SlewClampActive;        // True when slew-rate limiter is active this tick
        public float PressureFiltered;      // psia â€” low-pass filtered P for controller input
        
        // Relief valve state
        public float ReliefFlow;            // RHR relief valve flow (gpm), 0 if closed
        
        // Calculated rates (for diagnostics and display)
        public float PressureRate;          // psi/hr
        public float PzrHeatRate;           // Â°F/hr
        public float ThermalExpansionRate;  // ftÂ³/hr (volume rate from thermal expansion)
        public float CVCSRemovalRate;       // ftÂ³/hr (net volume removed by CVCS)
        public float ExcessVolumeRemoved;   // gallons cumulative (sent to VCT/BRS)
        public float SurgeFlow;             // gpm â€” PZR thermal expansion through surge line
        public float SurgeLineHeat_MW;      // MW â€” natural convection heat transfer through surge line
        
        // Bubble formation
        public bool BubbleFormed;           // True when T_pzr reaches T_sat
        public float BubbleFormationTemp;   // Â°F at which bubble formed
        public float T_sat;                 // Current saturation temperature at pressure
        
        // Display helpers
        public float PressureSetpoint;      // psia (CVCS target â€” final setpoint)
        public float PressureSetpointRamped;// psia (= PressureSetpoint; kept for backward compat)
        public float PressureError;         // psi (actual - ramped setpoint)
        public bool InControlBand;          // True if within 320-400 psig

        // v5.5.0: Three-phase solid pressurization control state
        public string ControlMode;          // PREHEATER_CVCS / HEATER_PRESSURIZE / HOLD_SOLID
        public float PreHeaterHandoffTimer_sec; // seconds P has remained above pre-heater handoff pressure
        public float HoldEntryTimer_sec;    // seconds P has been within hold-entry band (0 if outside)
        public float PressurizationElapsed_sec; // total seconds since init (diagnostic)
        public float PreHeaterTargetNetCharging_gpm;    // Diagnostic target command (documentation envelope)
        public float PreHeaterEffectiveNetCharging_gpm; // Effective net charging applied to model

        // v5.4.2.0 Phase A: CVCS transport delay state
        public float[] TransportDelayBuffer;    // Ring buffer of past LetdownAdjustEff values
        public int DelayBufferHead;             // Read/write index (oldest slot â€” read-before-write)
        public int DelayBufferLength;           // Active slots = ceil(delay / dt)
        public float DelayedLetdownAdjust;      // The delayed adjustment applied this step (diagnostic)
        public bool TransportDelayActive;       // True once buffer is fully primed
        public bool AntiWindupActive;           // True when integral accumulation is inhibited

        // v5.4.2.0 Phase A: CS-0023 diagnostic
        public bool SurgePressureConsistent;    // True when surge and pressure trends are consistent

        // Pressure model diagnostics for engine long-hold tracing.
        public string PressureEquationBranch;    // Active equation branch identifier
        public bool IsolatedNoFlowHold;          // True when base charging/letdown are both zero
        public bool PressureModelUsesSaturation; // True if pressure was saturation-constrained
        public float PressureModelSaturationPsia;// Psat(T_pzr) diagnostic value
        public float PressureModelDensity;       // Density used in pressure model (lbm/ft^3)
        public float PressureModelCompressibility;// Compressibility used in pressure model (1/psi)
        public float NetCvcsFlow_gpm;            // Letdown - charging + relief (gpm)
        public float ThermalVolumeDelta_ft3;     // dV_thermal this step
        public float CvcsVolumeDelta_ft3;        // dV_cvcs this step
        public float NetVolumeDelta_ft3;         // dV_net this step
        public float PressureDelta_psi;          // dP this step from pressure equation
    }
    
    /// <summary>
    /// Solid plant pressure-volume-temperature coupling and CVCS pressure control.
    /// 
    /// This module owns all physics for the solid pressurizer regime:
    ///   - PZR water heating (heaters + surge line conduction - losses)
    ///   - Thermal expansion pressure response (dP/dt from dV_thermal - dV_cvcs)
    ///   - CVCS PI controller for pressure (adjusts letdown/charging balance)
    ///   - RHR relief valve (safety backup above 450 psig)
    ///   - Bubble formation detection (T_pzr reaches T_sat)
    ///   - RCS heating via surge line natural convection
    ///
    /// The engine should call Initialize() once, then Update() each timestep,
    /// and read results from the returned SolidPlantState.
    /// </summary>
    public static partial class SolidPlantPressure
    {
        /// <summary>
        /// Test-only override to bypass the atmospheric pressure floor clamp.
        /// Default false preserves physical floor behavior in production runs.
        /// </summary>
        public static bool DisableAmbientPressureFloorForDiagnostics { get; set; } = false;
        
        #region Initialization
        
        /// <summary>
        /// Initialize solid plant state for cold shutdown conditions.
        /// </summary>
        /// <param name="pressure_psia">Initial RCS pressure in psia</param>
        /// <param name="T_rcs_F">Initial RCS temperature in Â°F</param>
        /// <param name="T_pzr_F">Initial PZR temperature in Â°F (typically = T_rcs at cold shutdown)</param>
        /// <param name="baseLetdown_gpm">Base letdown flow in gpm</param>
        /// <param name="baseCharging_gpm">Base charging flow in gpm</param>
        /// <returns>Initialized solid plant state</returns>
        public static SolidPlantState Initialize(
            float pressure_psia,
            float T_rcs_F,
            float T_pzr_F,
            float baseLetdown_gpm = 75f,
            float baseCharging_gpm = 75f)
        {
            var state = new SolidPlantState();
            
            state.Pressure = pressure_psia;
            state.T_pzr = T_pzr_F;
            state.T_rcs = T_rcs_F;
            
            // PZR water mass at initial conditions
            float rho = WaterProperties.WaterDensity(T_pzr_F, pressure_psia);
            state.PzrWaterMass = PlantConstants.PZR_TOTAL_VOLUME * rho;
            state.PzrWallTemp = T_pzr_F;
            state.HeaterEffectivePower = 0f;
            
            // CVCS starts balanced
            state.LetdownFlow = baseLetdown_gpm;
            state.ChargingFlow = baseCharging_gpm;
            state.ControllerIntegral = 0f;

            // Actuator dynamics: start with zero adjustment, filter seeded to actual P
            state.LetdownAdjustCmd = 0f;
            state.LetdownAdjustEff = 0f;
            state.SlewClampActive = false;
            state.PressureFiltered = pressure_psia;
            
            // Relief valve closed
            state.ReliefFlow = 0f;
            
            // Rates start at zero
            state.PressureRate = 0f;
            state.PzrHeatRate = 0f;
            state.ThermalExpansionRate = 0f;
            state.CVCSRemovalRate = 0f;
            state.ExcessVolumeRemoved = 0f;
            state.SurgeFlow = 0f;
            state.SurgeLineHeat_MW = 0f;
            state.SurgeMassTransfer_lb = 0f;
            
            // v5.0.2: Initialize mass diagnostics
            state.PzrDensity = rho;
            state.PzrVolumeImplied = PlantConstants.PZR_TOTAL_VOLUME;
            state.PzrMassFlowRate = 0f;
            
            // Bubble not yet formed
            state.BubbleFormed = false;
            state.BubbleFormationTemp = 0f;
            state.T_sat = WaterProperties.SaturationTemperature(pressure_psia);
            
            // Control band
            state.PressureSetpoint = PlantConstants.SOLID_PLANT_P_SETPOINT_PSIA;
            state.InControlBand = (pressure_psia >= PlantConstants.SOLID_PLANT_P_LOW_PSIA &&
                                   pressure_psia <= PlantConstants.SOLID_PLANT_P_HIGH_PSIA);

            // v5.5.0: Three-phase solid pressurization control.
            // If below handoff pressure, start in PREHEATER_CVCS.
            // If above handoff pressure but below setpoint band, start in HEATER_PRESSURIZE.
            // If already near setpoint, start in HOLD_SOLID.
            state.PreHeaterHandoffTimer_sec = 0f;
            state.HoldEntryTimer_sec = 0f;
            state.PressurizationElapsed_sec = 0f;
            state.PreHeaterTargetNetCharging_gpm = PREHEATER_DOC_MIN_NET_CHARGING_GPM;
            state.PreHeaterEffectiveNetCharging_gpm = PREHEATER_EFFECTIVE_MIN_NET_CHARGING_GPM;
            state.PressureSetpointRamped = state.PressureSetpoint;  // Always target final SP

            if (pressure_psia < PREHEATER_HANDOFF_PRESSURE_PSIA)
            {
                state.ControlMode = "PREHEATER_CVCS";
            }
            else if (Math.Abs(pressure_psia - state.PressureSetpoint) > HOLD_ENTRY_BAND_PSI)
            {
                state.ControlMode = "HEATER_PRESSURIZE";
            }
            else
            {
                // Already near setpoint â€” go straight to hold
                state.ControlMode = "HOLD_SOLID";
            }

            state.PressureError = pressure_psia - state.PressureSetpoint;

            // v5.4.2.0 Phase A: CVCS transport delay buffer â€” initialized to zero (balanced CVCS).
            // During priming (first N steps), buffer outputs zeros â†’ LetdownFlow = baseLetdown + 0.
            // This is physically correct: no PI adjustment has completed transit yet.
            state.TransportDelayBuffer = new float[DELAY_BUFFER_MAX_SLOTS];
            state.DelayBufferHead = 0;
            state.DelayBufferLength = 0;  // Computed on first Update() from dt
            state.DelayedLetdownAdjust = 0f;
            state.TransportDelayActive = false;
            state.AntiWindupActive = false;
            state.SurgePressureConsistent = true;
            state.PressureEquationBranch = "INIT";
            state.IsolatedNoFlowHold = false;
            state.PressureModelUsesSaturation = false;
            state.PressureModelSaturationPsia = WaterProperties.SaturationPressure(T_pzr_F);
            state.PressureModelDensity = rho;
            state.PressureModelCompressibility = ThermalExpansion.Compressibility(T_pzr_F, pressure_psia);
            state.NetCvcsFlow_gpm = 0f;
            state.ThermalVolumeDelta_ft3 = 0f;
            state.CvcsVolumeDelta_ft3 = 0f;
            state.NetVolumeDelta_ft3 = 0f;
            state.PressureDelta_psi = 0f;

            return state;
        }
        
        #endregion
        
        #region Main Update
        
        /// <summary>
        /// Update solid plant state for one timestep.
        /// 
        /// This is the main entry point called by the engine each physics step.
        /// It calculates:
        ///   1. PZR water temperature change (heaters, surge line, losses)
        ///   2. Thermal expansion volume rate
        ///   3. CVCS pressure controller response (PI on letdown/charging)
        ///   4. RHR relief valve (if pressure exceeds setpoint)
        ///   5. Net pressure change from volume imbalance
        ///   6. Bubble formation check
        ///
        /// Also updates RCS temperature from surge line heat transfer.
        /// </summary>
        /// <param name="state">Current state (modified in place)</param>
        /// <param name="heaterPower_kW">PZR heater demand in kW</param>
        /// <param name="baseLetdown_gpm">Base letdown flow before controller adjustment (gpm)</param>
        /// <param name="baseCharging_gpm">Base charging flow (gpm)</param>
        /// <param name="rcsHeatCapacity_BTU_F">Total RCS heat capacity in BTU/Â°F (metal + water)</param>
        /// <param name="dt_hr">Timestep in hours</param>
        /// <param name="bulkTransportFactor">0-1 transport coupling factor for no-RCP bulk-state application</param>
        public static void Update(
            ref SolidPlantState state,
            float heaterPower_kW,
            float baseLetdown_gpm,
            float baseCharging_gpm,
            float rcsHeatCapacity_BTU_F,
            float dt_hr,
            float bulkTransportFactor = 1f)
        {
            if (state.BubbleFormed) return;
            
            bulkTransportFactor = Math.Max(0f, Math.Min(1f, bulkTransportFactor));
            float dt_sec = dt_hr * 3600f;
            float prevT_pzr = state.T_pzr;
            float prevT_rcs = state.T_rcs;
            float prevPressure = state.Pressure;
            state.IsolatedNoFlowHold = baseLetdown_gpm <= 1e-4f && baseCharging_gpm <= 1e-4f;

            // v5.4.2.0 FF-05 Fix #1: Capture PZR density BEFORE temperature update.
            // Surge mass transfer uses the density at which water actually leaves
            // the PZR, not the post-heating density. During rapid pressurization
            // (dP/dt > 100 psi/hr), post-step density error can exceed 10 lbm/step.
            float rho_pzr_preStep = WaterProperties.WaterDensity(state.T_pzr, state.Pressure);

            // ================================================================
            // 1. PZR WATER TEMPERATURE UPDATE
            //    Heaters warm PZR water. Heat conducts to RCS via surge line.
            //    Losses through PZR insulation to containment.
            // ================================================================
            
            // Heater thermal lag (20s time constant)
            state.HeaterEffectivePower = PressurizerPhysics.HeaterLagResponse(
                state.HeaterEffectivePower, heaterPower_kW, PlantConstants.HEATER_TAU, dt_sec);
            
            // Heat input from heaters (BTU/sec)
            float heaterHeat_BTU_sec = state.HeaterEffectivePower * PlantConstants.KW_TO_BTU_SEC;
            
            // Heat conducted from PZR to RCS via surge line (BTU/sec)
            float surgeLineHeat_MW = HeatTransfer.SurgeLineHeatTransfer_MW(
                state.T_pzr, state.T_rcs, state.Pressure) * bulkTransportFactor;
            float surgeLineHeat_BTU_sec = surgeLineHeat_MW * PlantConstants.MW_TO_BTU_SEC;
            state.SurgeLineHeat_MW = surgeLineHeat_MW;
            
            // PZR insulation loss to containment (BTU/sec)
            float pzrAmbientLoss_BTU_sec = 0f;
            if (state.T_pzr > PlantConstants.AMBIENT_TEMP_F)
            {
                float deltaT_ambient = state.T_pzr - PlantConstants.AMBIENT_TEMP_F;
                float deltaT_ref = 650f - PlantConstants.AMBIENT_TEMP_F;
                if (deltaT_ref > 0f)
                    pzrAmbientLoss_BTU_sec = 50f * PlantConstants.KW_TO_BTU_SEC * (deltaT_ambient / deltaT_ref);
            }
            
            // Net heat to PZR water
            float netPzrHeat_BTU_sec = heaterHeat_BTU_sec - surgeLineHeat_BTU_sec - pzrAmbientLoss_BTU_sec;
            
            // PZR temperature change
            float rho_pzr = WaterProperties.WaterDensity(state.T_pzr, state.Pressure);
            float Cp_pzr = WaterProperties.WaterSpecificHeat(state.T_pzr, state.Pressure);
            // v5.0.2: Use conserved mass for thermal capacity (not V Ã— Ï recalculation)
            float pzrWaterMassForCp = state.PzrWaterMass;
            float pzrWallCapacity = ThermalMass.PressurizerWallHeatCapacity();
            float pzrEffectiveCapacity = pzrWaterMassForCp * Cp_pzr + pzrWallCapacity;
            
            if (pzrEffectiveCapacity > 0f)
                state.T_pzr += netPzrHeat_BTU_sec * dt_sec / pzrEffectiveCapacity;
            
            state.PzrWallTemp = state.T_pzr;
            // v5.0.2: PZR mass is conserved â€” NOT recalculated from V Ã— Ï(T,P).
            // Mass update deferred to Section 7 after dV_pzr_ft3 is computed.
            
            // ================================================================
            // 2. RCS TEMPERATURE UPDATE
            //    Heat enters RCS via surge line conduction.
            //    Heat leaves RCS via insulation losses.
            // ================================================================
            
            float rcsInsulationLoss_MW = HeatTransfer.InsulationHeatLoss_MW(state.T_rcs);
            float rcsInsulationLoss_BTU_sec = rcsInsulationLoss_MW * PlantConstants.MW_TO_BTU_SEC;
            
            float netRcsHeat_BTU_sec = surgeLineHeat_BTU_sec - rcsInsulationLoss_BTU_sec;
            
            if (rcsHeatCapacity_BTU_F > 0f)
                state.T_rcs += netRcsHeat_BTU_sec * dt_sec / rcsHeatCapacity_BTU_F;
            
            // ================================================================
            // 3. THERMAL EXPANSION VOLUME RATE
            //    Both PZR and RCS water expand as they heat up.
            //    In a closed water-solid system, this expansion must be
            //    accommodated by the CVCS or pressure will rise.
            // ================================================================
            
            float pzrDeltaT = state.T_pzr - prevT_pzr;
            float rcsDeltaT = state.T_rcs - prevT_rcs;
            
            // PZR expansion
            float beta_pzr = ThermalExpansion.ExpansionCoefficient(state.T_pzr, state.Pressure);
            float dV_pzr_ft3 = PlantConstants.PZR_TOTAL_VOLUME * beta_pzr * pzrDeltaT;
            
            // RCS expansion
            float beta_rcs = ThermalExpansion.ExpansionCoefficient(state.T_rcs, state.Pressure);
            float dV_rcs_ft3 = PlantConstants.RCS_WATER_VOLUME * beta_rcs * rcsDeltaT;
            
            // Total thermal expansion this timestep (ftÂ³)
            float dV_thermal_ft3 = dV_pzr_ft3 + dV_rcs_ft3;
            
            // ================================================================
            // 4. SOLID PRESSURIZATION CONTROL POLICY
            //    Phase A: PREHEATER_CVCS (CVCS-dominant, heaters locked out upstream)
            //    Phase B: HEATER_PRESSURIZE (heater-led with bounded CVCS trim)
            //    Phase C: HOLD_SOLID (PI fine hold near setpoint)
            // ================================================================

            state.PressurizationElapsed_sec += dt_sec;

            if (state.IsolatedNoFlowHold)
            {
                // Explicit no-flow hold: prevent PI/transport-delay logic from
                // synthesizing CVCS-driven pressure changes when base flows are zero.
                state.ControlMode = "ISOLATED_NO_FLOW";
                state.PreHeaterHandoffTimer_sec = 0f;
                state.HoldEntryTimer_sec = 0f;
                state.ControllerIntegral = 0f;
                state.LetdownAdjustCmd = 0f;
                state.LetdownAdjustEff = 0f;
                state.DelayedLetdownAdjust = 0f;
                state.SlewClampActive = false;
                state.AntiWindupActive = false;
                state.PressureFiltered = state.Pressure;
                state.TransportDelayActive = false;
                state.LetdownFlow = 0f;
                state.ChargingFlow = 0f;
            }
            else
            {
                // Mode transition logic
                float distToSetpoint = state.Pressure - state.PressureSetpoint;
                if (state.ControlMode == "PREHEATER_CVCS")
                {
                    if (state.Pressure >= PREHEATER_HANDOFF_PRESSURE_PSIA)
                    {
                        state.PreHeaterHandoffTimer_sec += dt_sec;
                        if (state.PreHeaterHandoffTimer_sec >= PREHEATER_HANDOFF_DWELL_SEC)
                        {
                            state.ControlMode = "HEATER_PRESSURIZE";
                            state.PreHeaterHandoffTimer_sec = 0f;
                            state.ControllerIntegral = 0f;
                            state.HoldEntryTimer_sec = 0f;
                        }
                    }
                    else
                    {
                        state.PreHeaterHandoffTimer_sec = 0f;
                    }
                }
                else if (state.ControlMode == "HEATER_PRESSURIZE")
                {
                    if (Math.Abs(distToSetpoint) <= HOLD_ENTRY_BAND_PSI)
                    {
                        state.HoldEntryTimer_sec += dt_sec;
                        if (state.HoldEntryTimer_sec >= HOLD_ENTRY_DWELL_SEC)
                        {
                            state.ControlMode = "HOLD_SOLID";
                            state.ControllerIntegral = 0f;
                        }
                    }
                    else
                    {
                        state.HoldEntryTimer_sec = 0f;
                    }
                }
                else if (state.ControlMode == "HOLD_SOLID")
                {
                    if (distToSetpoint < -HOLD_EXIT_DROP_PSI)
                    {
                        state.ControlMode = "HEATER_PRESSURIZE";
                        state.HoldEntryTimer_sec = 0f;
                        state.ControllerIntegral = 0f;
                    }
                }

                // Pressure filter for controller input
                if (CVCS_PRESSURE_FILTER_TAU_SEC > 0f && dt_sec > 0f)
                {
                    float alpha = Math.Min(dt_sec / CVCS_PRESSURE_FILTER_TAU_SEC, 1f);
                    state.PressureFiltered += (state.Pressure - state.PressureFiltered) * alpha;
                }
                else
                {
                    state.PressureFiltered = state.Pressure;
                }

                if (state.ControlMode == "PREHEATER_CVCS")
                {
                    float pressureRate = state.PressureRate;
                    float rateNorm = 0.5f;
                    float rateSpan = PREHEATER_TARGET_RATE_MAX_PSI_HR - PREHEATER_TARGET_RATE_MIN_PSI_HR;
                    if (rateSpan > 1e-6f)
                    {
                        rateNorm = (PREHEATER_TARGET_RATE_MAX_PSI_HR - pressureRate) / rateSpan;
                    }
                    rateNorm = Math.Max(0f, Math.Min(rateNorm, 1f));

                    float effectiveNetCharging = PREHEATER_EFFECTIVE_MIN_NET_CHARGING_GPM
                        + rateNorm * (PREHEATER_EFFECTIVE_MAX_NET_CHARGING_GPM - PREHEATER_EFFECTIVE_MIN_NET_CHARGING_GPM);
                    float docTargetNetCharging = PREHEATER_DOC_MIN_NET_CHARGING_GPM
                        + rateNorm * (PREHEATER_DOC_MAX_NET_CHARGING_GPM - PREHEATER_DOC_MIN_NET_CHARGING_GPM);

                    state.PreHeaterEffectiveNetCharging_gpm = effectiveNetCharging;
                    state.PreHeaterTargetNetCharging_gpm = docTargetNetCharging;
                    state.ControllerIntegral = 0f;
                    state.AntiWindupActive = false;
                    state.SlewClampActive = false;
                    state.TransportDelayActive = false;
                    state.LetdownAdjustCmd = -effectiveNetCharging;
                    state.LetdownAdjustEff = -effectiveNetCharging;
                    state.DelayedLetdownAdjust = -effectiveNetCharging;

                    state.ChargingFlow = baseCharging_gpm;
                    state.LetdownFlow = baseLetdown_gpm - effectiveNetCharging;
                    state.LetdownFlow = Math.Max(MIN_LETDOWN_GPM, Math.Min(state.LetdownFlow, MAX_LETDOWN_GPM));
                }
                else
                {
                    state.PreHeaterTargetNetCharging_gpm = 0f;
                    state.PreHeaterEffectiveNetCharging_gpm = 0f;

                    // PI controller
                    float pressureError_psi = state.PressureFiltered - state.PressureSetpoint;
                    float pTerm = KP_PRESSURE * pressureError_psi;
                    float provisionalCmd = pTerm + KI_PRESSURE * state.ControllerIntegral;
                    float deadTimeGap = Math.Abs(state.LetdownAdjustEff - state.DelayedLetdownAdjust);
                    bool actuatorClamped = state.SlewClampActive ||
                        (state.ControlMode == "HEATER_PRESSURIZE" &&
                         Math.Abs(provisionalCmd) > HEATER_PRESS_MAX_NET_TRIM_GPM);
                    bool deadTimeInhibit = deadTimeGap > ANTIWINDUP_DEADTIME_THRESHOLD_GPM;

                    state.AntiWindupActive = actuatorClamped || deadTimeInhibit;
                    if (!state.AntiWindupActive)
                    {
                        state.ControllerIntegral += pressureError_psi * dt_sec;
                    }

                    float integralLimit = INTEGRAL_LIMIT_GPM / KI_PRESSURE;
                    state.ControllerIntegral = Math.Max(-integralLimit,
                        Math.Min(state.ControllerIntegral, integralLimit));
                    float iTerm = KI_PRESSURE * state.ControllerIntegral;

                    float letdownAdjustCmd = pTerm + iTerm;
                    letdownAdjustCmd = Math.Max(-MAX_LETDOWN_ADJUSTMENT_GPM,
                                        Math.Min(letdownAdjustCmd, MAX_LETDOWN_ADJUSTMENT_GPM));
                    state.LetdownAdjustCmd = letdownAdjustCmd;

                    if (state.ControlMode == "HEATER_PRESSURIZE")
                    {
                        letdownAdjustCmd = Math.Max(-HEATER_PRESS_MAX_NET_TRIM_GPM,
                            Math.Min(letdownAdjustCmd, HEATER_PRESS_MAX_NET_TRIM_GPM));
                    }

                    // CVCS actuator dynamics
                    float effAdj = state.LetdownAdjustEff;
                    if (CVCS_ACTUATOR_TAU_SEC > 0f && dt_sec > 0f)
                    {
                        float alphaA = Math.Min(dt_sec / CVCS_ACTUATOR_TAU_SEC, 1f);
                        effAdj += (letdownAdjustCmd - effAdj) * alphaA;
                    }
                    else
                    {
                        effAdj = letdownAdjustCmd;
                    }

                    float maxDelta = CVCS_MAX_SLEW_GPM_PER_SEC * dt_sec;
                    float delta = effAdj - state.LetdownAdjustEff;
                    if (Math.Abs(delta) > maxDelta)
                    {
                        effAdj = state.LetdownAdjustEff + Math.Sign(delta) * maxDelta;
                        state.SlewClampActive = true;
                    }
                    else
                    {
                        state.SlewClampActive = false;
                    }
                    state.LetdownAdjustEff = effAdj;

                    // CVCS transport delay
                    if (state.DelayBufferLength == 0 && dt_sec > 0f)
                    {
                        state.DelayBufferLength = Math.Max(1,
                            (int)Math.Ceiling(CVCS_TRANSPORT_DELAY_SEC / dt_sec));
                        state.DelayBufferLength = Math.Min(state.DelayBufferLength,
                            DELAY_BUFFER_MAX_SLOTS);
                    }

                    if (state.TransportDelayBuffer != null && state.DelayBufferLength > 0)
                    {
                        state.DelayedLetdownAdjust = state.TransportDelayBuffer[state.DelayBufferHead];
                        state.TransportDelayBuffer[state.DelayBufferHead] = state.LetdownAdjustEff;
                        state.DelayBufferHead = (state.DelayBufferHead + 1) % state.DelayBufferLength;
                        if (!state.TransportDelayActive && state.PressurizationElapsed_sec >= CVCS_TRANSPORT_DELAY_SEC)
                        {
                            state.TransportDelayActive = true;
                        }
                    }
                    else
                    {
                        state.DelayedLetdownAdjust = state.LetdownAdjustEff;
                    }

                    state.LetdownFlow = baseLetdown_gpm + state.DelayedLetdownAdjust;
                    state.LetdownFlow = Math.Max(MIN_LETDOWN_GPM, Math.Min(state.LetdownFlow, MAX_LETDOWN_GPM));
                    state.ChargingFlow = baseCharging_gpm;
                }
            }
            
            // ================================================================
            // 5. RHR RELIEF VALVE
            //    Safety backup - opens if CVCS cannot maintain pressure below 450 psig.
            //    Proportional opening above setpoint.
            // ================================================================
            
            float pressure_psig = state.Pressure - PlantConstants.PSIG_TO_PSIA;
            state.ReliefFlow = CalculateReliefFlow(pressure_psig, state.ReliefFlow > 0f);
            
            // ================================================================
            // 6. NET VOLUME BALANCE AND PRESSURE CHANGE
            //    dP = (dV_thermal - dV_removed) / (V_total * kappa)
            //    where dV_removed includes CVCS net flow and relief valve flow
            // ================================================================
            
            // CVCS net volume removal rate (ftÂ³/sec)
            // Positive = net volume leaving RCS (letdown > charging = pressure decreases)
            float netCVCS_gpm = state.LetdownFlow - state.ChargingFlow + state.ReliefFlow;
            float rho_avg = WaterProperties.WaterDensity(
                (state.T_pzr + state.T_rcs) / 2f, state.Pressure);
            float netCVCS_ft3_sec = netCVCS_gpm * PlantConstants.GPM_TO_FT3_SEC;
            
            // Volume removed by CVCS this timestep
            float dV_cvcs_ft3 = netCVCS_ft3_sec * dt_sec;
            
            // Net volume imbalance (positive = pressure rises)
            float dV_net_ft3 = dV_thermal_ft3 - dV_cvcs_ft3;
            
            // Pressure change from volume imbalance in a closed, water-solid system
            // dP = dV_net / (V_total * kappa)
            // where kappa is isothermal compressibility
            float T_avg = (state.T_pzr * PlantConstants.PZR_TOTAL_VOLUME + 
                          state.T_rcs * PlantConstants.RCS_WATER_VOLUME) /
                         (PlantConstants.PZR_TOTAL_VOLUME + PlantConstants.RCS_WATER_VOLUME);
            float kappa = ThermalExpansion.Compressibility(T_avg, state.Pressure);
            float V_total = PlantConstants.RCS_WATER_VOLUME + PlantConstants.PZR_TOTAL_VOLUME;
            
            float dP_psi = 0f;
            if (kappa > 1e-12f && V_total > 0f)
                dP_psi = dV_net_ft3 / (V_total * kappa);
            state.PressureDelta_psi = dP_psi;
            
            state.Pressure += dP_psi;
            
            // Hard physical floor: pressure cannot go below atmospheric.
            // Investigation-only override allows clamp bypass for bracket testing.
            if (!DisableAmbientPressureFloorForDiagnostics)
                state.Pressure = Math.Max(state.Pressure, PlantConstants.P_ATM);
            
            // ================================================================
            // 7. UPDATE DIAGNOSTIC RATES
            // ================================================================
            
            if (dt_hr > 1e-8f)
            {
                state.PressureRate = (state.Pressure - prevPressure) / dt_hr;
                state.PzrHeatRate = (state.T_pzr - prevT_pzr) / dt_hr;
            }
            
            state.ThermalExpansionRate = (dt_hr > 1e-8f) ? dV_thermal_ft3 / dt_hr : 0f;
            state.CVCSRemovalRate = (dt_hr > 1e-8f) ? dV_cvcs_ft3 / dt_hr : 0f;
            
            // Surge flow: PZR thermal expansion drives water through the surge line
            // into the hot leg. Same formula as RCSHeatup.IsolatedHeatingStep:
            //   flow_gpm = dV_pzr (ftÂ³) Ã— FT3_TO_GAL / dt_hr / 60
            // Positive = PZR expanding, water flowing out to RCS
            state.SurgeFlow = (dt_hr > 1e-8f) ? (dV_pzr_ft3 * PlantConstants.FT3_TO_GAL / dt_hr / 60f) : 0f;

            // v5.4.2.0 Phase A: Surge-pressure consistency diagnostic (CS-0023)
            // During HEATER_PRESSURIZE, surge flow and pressure rate should have
            // consistent signs (both positive = expanding + pressurizing).
            // During HOLD_SOLID, CVCS opposition makes brief inconsistency normal.
            bool surgePositive = state.SurgeFlow > 0.01f;
            bool pressureRising = state.PressureRate > 0.1f;
            bool bothZero = Math.Abs(state.SurgeFlow) < 0.01f && Math.Abs(state.PressureRate) < 0.1f;
            state.SurgePressureConsistent = bothZero || (surgePositive == pressureRising)
                || state.ControlMode == "HOLD_SOLID"
                || state.ControlMode == "PREHEATER_CVCS";  // CVCS-dominant policy decouples surge/pressure trends

            // Pressure model diagnostics for engine-level long-hold tracing.
            state.PressureEquationBranch = state.IsolatedNoFlowHold
                ? "SOLID_NO_FLOW_dP=dV_thermal/(V*kappa)"
                : "SOLID_CVCS_dP=(dV_thermal-dV_cvcs)/(V*kappa)";
            state.PressureModelUsesSaturation = false;
            state.PressureModelSaturationPsia = WaterProperties.SaturationPressure(state.T_pzr);
            state.PressureModelDensity = rho_avg;
            state.PressureModelCompressibility = kappa;
            state.NetCvcsFlow_gpm = netCVCS_gpm;
            state.ThermalVolumeDelta_ft3 = dV_thermal_ft3;
            state.CvcsVolumeDelta_ft3 = dV_cvcs_ft3;
            state.NetVolumeDelta_ft3 = dV_net_ft3;

            // â”€â”€ v5.0.2: PZR MASS CONSERVATION â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // Mass changes only via surge transfer (internal PZRâ†’RCS).
            // Thermal expansion displaces water OUT of PZR through surge line.
            // surgeMass derived from same dV_pzr_ft3 used for SurgeFlow (single source).
            // Positive dV_pzr = expansion = mass LEAVING PZR.
            // v5.4.2.0 FF-05 Fix #1: Use pre-step density for surge mass transfer.
            // Water leaves PZR at the density it had BEFORE this step's heating.
            float surgeMass_lb = dV_pzr_ft3 * rho_pzr_preStep;
            state.PzrWaterMass -= surgeMass_lb;
            state.SurgeMassTransfer_lb = surgeMass_lb;

            // Diagnostics â€” post-step density for display only
            float rho_pzr_post = WaterProperties.WaterDensity(state.T_pzr, state.Pressure);
            state.PzrDensity = rho_pzr_post;
            state.PzrVolumeImplied = (rho_pzr_post > 0.1f)
                ? state.PzrWaterMass / rho_pzr_post
                : PlantConstants.PZR_TOTAL_VOLUME;
            state.PzrMassFlowRate = (dt_hr > 1e-8f) ? (-surgeMass_lb / dt_hr) : 0f;
            
            // Track cumulative excess volume removed (in gallons for operator display)
            if (dV_cvcs_ft3 > 0f)
                state.ExcessVolumeRemoved += dV_cvcs_ft3 * PlantConstants.FT3_TO_GAL;
            
            // ================================================================
            // 8. UPDATE SATURATION AND CONTROL BAND STATUS
            // ================================================================
            
            state.T_sat = WaterProperties.SaturationTemperature(state.Pressure);
            // v5.4.1 Audit Fix: Error is vs ramped setpoint (what the controller actually uses)
            state.PressureError = state.Pressure - state.PressureSetpointRamped;
            state.InControlBand = (state.Pressure >= PlantConstants.SOLID_PLANT_P_LOW_PSIA &&
                                   state.Pressure <= PlantConstants.SOLID_PLANT_P_HIGH_PSIA);
            
            // ================================================================
            // 9. BUBBLE FORMATION CHECK
            //    Bubble forms when PZR water reaches saturation temperature
            //    at current system pressure.
            // ================================================================
            
            if (state.T_pzr >= state.T_sat - 2f && 
                state.T_pzr >= PlantConstants.BUBBLE_FORMATION_TEMP_F)
            {
                state.BubbleFormed = true;
                state.BubbleFormationTemp = state.T_pzr;
            }
        }
        
        #endregion
    }
}

