// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (Bubble Formation Partial)
// HeatupSimEngine.BubbleFormation.cs - Multi-Phase Bubble Formation Procedure
// ============================================================================
//
// PURPOSE:
//   Complete pressurizer bubble formation state machine, from initial steam
//   detection through thermodynamic drain to pressurization for RCP NPSH.
//   Implements the 7-phase procedure per NRC HRTD 19.2.2.
//
// PHYSICS:
//   Bubble formation is a thermodynamic process, NOT mechanical drain:
//     PRIMARY: Steam generation from heater power at T_sat
//       dV_steam = (Q_heaters / h_fg) × (1/ρ_steam - 1/ρ_water) × dt
//     SECONDARY: CVCS trim via letdown/charging imbalance
//       Net outflow = letdown - charging (75 gpm - 0/44 gpm)
//   
//   Phase sequence: NONE → DETECTION → VERIFICATION → DRAIN → STABILIZE
//                   → PRESSURIZE → COMPLETE
//
// SOURCES:
//   - NRC HRTD 2.1 — Steam displacement mechanism
//   - NRC HRTD 4.1 — CCP capacity (44 gpm), CVCS flow balance
//   - NRC HRTD 19.2.2 — Bubble formation procedure, CCP start, aux spray test
//   - NRC ML11223A342 — Heatup limits, RCP pressure requirements
//
// ARCHITECTURE:
//   Partial class of HeatupSimEngine. This file owns:
//     - BubbleFormationPhase enum (7 phases)
//     - ProcessBubbleDetection() — detects first steam in solid PZR branch
//     - UpdateBubbleFormation() — full state machine (switch on bubblePhase)
//     - Thermodynamic drain calculation (steam displacement + CVCS trim)
//     - CCP start logic and aux spray test model
//
//   State fields (BubbleFormationPhase, CCP tracking, aux spray tracking)
//   are declared in the main HeatupSimEngine.cs for Unity serialization.
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupSimEngine
{
    // ========================================================================
    // BUBBLE FORMATION PHASE ENUM
    // Per NRC ML11223A342 Section 19.2.2
    // ========================================================================

    /// <summary>
    /// Sub-phases of the pressurizer bubble formation procedure.
    /// Per NRC HRTD 19.2.2: Bubble formation is a multi-phase process.
    /// </summary>
    public enum BubbleFormationPhase
    {
        /// <summary>No bubble formation in progress (solid plant or bubble complete)</summary>
        NONE,
        /// <summary>First steam detected at heater surfaces, instruments confirming</summary>
        DETECTION,
        /// <summary>Aux spray test confirms compressible gas (not solid water)</summary>
        VERIFICATION,
        /// <summary>Controlled drain: thermodynamic steam displacement + CVCS trim, PZR ~100% to 25%</summary>
        DRAIN,
        /// <summary>CVCS rebalanced, level control to auto, PI initialized</summary>
        STABILIZE,
        /// <summary>Heaters raising pressure to >=320 psig for RCP NPSH</summary>
        PRESSURIZE,
        /// <summary>Bubble formation complete, ready for RCP startup</summary>
        COMPLETE
    }

    // ========================================================================
    // BUBBLE DETECTION — Called from solid pressurizer physics branch
    // Detects when SolidPlantPressure module signals bubble formation.
    // ========================================================================

    /// <summary>
    /// Check for bubble formation detection and initiate the multi-phase
    /// bubble formation procedure if steam is first detected.
    /// Called each timestep during solid pressurizer operations.
    /// </summary>
    /// <returns>True if bubble was just detected this timestep</returns>
    bool ProcessBubbleDetection()
    {
        // v1.3.1.0: During pre-drain phases, suppress re-detection by
        // resetting the physics module's flag each step. The engine's
        // bubblePhase state machine owns the transition now.
        if (bubblePreDrainPhase && solidPlantState.BubbleFormed)
        {
            solidPlantState.BubbleFormed = false;
        }

        // Check for bubble formation (detected by physics module)
        if (solidPlantState.BubbleFormed && bubblePhase == BubbleFormationPhase.NONE)
        {
            // ================================================================
            // BUBBLE DETECTED — Begin multi-phase formation procedure
            // Per NRC HRTD 19.2.2: This is NOT instant. The bubble is first
            // detected, then verified, then the PZR is drained over ~40 min.
            //
            // BUG #1 FIX: Previously this block instantly set PZR from
            // 100% water to 25% in a single timestep, creating a mass
            // discontinuity that cascaded into Bugs #2, #3, #4.
            // ================================================================

            // RTCC pre-handoff snapshot: solid authority is canonical at detection boundary.
            float preHandoffMass_lbm = physicsState.TotalPrimaryMassSolid;
            if (preHandoffMass_lbm <= 0f)
                preHandoffMass_lbm = physicsState.RCSWaterMass + physicsState.PZRWaterMass;

            solidPressurizer = false;
            // bubbleFormed stays FALSE until drain completes — gates RCP starts
            bubbleFormationTemp = solidPlantState.BubbleFormationTemp;

            // ================================================================
            // PRESSURE TRANSITION (v1.3.1.0 fix)
            // Do NOT jump to Psat(T_pzr) — the PZR is still functionally
            // water-solid with only a thin steam film at heater surfaces.
            // Pressure remains at current solid-plant-controlled value.
            // ================================================================
            T_sat = WaterProperties.SaturationTemperature(pressure);
            physicsState.Pressure = pressure;

            // PZR is still 100% water — only a thin steam film at heater surfaces.
            // Apply RTCC reconciliation so authority transfer preserves total tracked mass.
            float rhoWater = WaterProperties.WaterDensity(T_pzr, pressure);
            float reconstructedPzrWater_lbm = physicsState.PZRWaterVolume * rhoWater;
            float reconstructedPzrSteam_lbm = 0f;
            float reconstructedRcs_lbm = physicsState.RCSWaterMass;
            float reconstructedTotal_lbm = reconstructedRcs_lbm + reconstructedPzrWater_lbm + reconstructedPzrSteam_lbm;
            float rawDelta_lbm = reconstructedTotal_lbm - preHandoffMass_lbm;
            float reconciledRcs_lbm = reconstructedRcs_lbm - rawDelta_lbm;
            if (reconciledRcs_lbm < 0f)
                throw new System.InvalidOperationException($"RTCC reconciliation produced negative RCS mass: {reconciledRcs_lbm:F3} lbm");

            physicsState.PZRWaterMass = reconstructedPzrWater_lbm;
            physicsState.PZRSteamVolume = 0f;
            physicsState.PZRSteamMass = reconstructedPzrSteam_lbm;
            physicsState.RCSWaterMass = reconciledRcs_lbm;
            physicsState.TotalPrimaryMass_lb = preHandoffMass_lbm;

            float postHandoffMass_lbm = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
            float assertDelta_lbm = postHandoffMass_lbm - preHandoffMass_lbm;
            bool assertPass = Mathf.Abs(assertDelta_lbm) <= PlantConstants.RTCC_EPSILON_MASS_LBM;

            RecordRtccTransition(
                "SOLID_TO_TWO_PHASE",
                "CANONICAL_SOLID",
                "CANONICAL_TWO_PHASE",
                preHandoffMass_lbm,
                reconstructedTotal_lbm,
                rawDelta_lbm,
                postHandoffMass_lbm,
                assertDelta_lbm,
                assertPass);
            AssertRtccPassOrThrow("SOLID_TO_TWO_PHASE", assertDelta_lbm);
            SeedPzrEnergyStateFromCurrentMasses("SOLID_TO_TWO_PHASE_HANDOFF");

            // v1.3.1.0: Reset solid plant state's BubbleFormed flag so the
            // SolidPlantPressure module continues updating during DETECTION
            // and VERIFICATION. The engine's state machine owns the transition.
            solidPlantState.BubbleFormed = false;

            // Enter DETECTION phase
            bubblePhase = BubbleFormationPhase.DETECTION;
            bubblePhaseStartTime = simTime;
            bubblePreDrainPhase = true;  // v1.3.1.0: CVCS continues solid-plant-style

            heatupPhaseDesc = "BUBBLE DETECTED - CONFIRMING";
            statusMessage = "*** STEAM BUBBLE DETECTED ***";
            LogEvent(EventSeverity.ALERT, $"STEAM BUBBLE DETECTED at T_pzr={T_pzr:F0}F  P={pressure:F0}psia");
            LogEvent(EventSeverity.INFO, "Beginning bubble formation procedure (NRC HRTD 19.2.2)");
            Debug.Log($"[T+{simTime:F2}hr] === STEAM BUBBLE DETECTED ===");
            Debug.Log($"  T_pzr={T_pzr:F1}F, T_sat={T_sat:F1}F, P={pressure:F0}psia ({pressure - 14.7f:F0}psig)");
            Debug.Log($"  Beginning multi-phase bubble formation (~60 min total)");

            return true;
        }

        // No bubble — update status display
        if (bubblePhase == BubbleFormationPhase.NONE)
        {
            heatupPhaseDesc = $"SOLID PZR - HEATING TO TSAT ({T_sat:F0}F)";
            statusMessage = SolidPlantPressure.GetStatusString(solidPlantState);
        }

        return false;
    }

    // ========================================================================
    // BUBBLE FORMATION STATE MACHINE — Runs each timestep during active phases
    // Returns true if DRAIN phase is active (signals CVCS override)
    // ========================================================================

    /// <summary>
    /// Advance the bubble formation state machine by one timestep.
    /// Handles phase transitions: DETECTION → VERIFICATION → DRAIN →
    /// STABILIZE → PRESSURIZE → COMPLETE.
    /// </summary>
    /// <returns>True if DRAIN phase is active (CVCS flows overridden)</returns>
    bool UpdateBubbleFormation(float dt)
    {
        if (bubblePhase == BubbleFormationPhase.NONE || bubblePhase == BubbleFormationPhase.COMPLETE)
            return false;

        float phaseElapsed = simTime - bubblePhaseStartTime;
        bool drainActive = false;

        switch (bubblePhase)
        {
            // ============================================================
            // DETECTION (~5 min): First steam at heater surfaces
            // PZR level drops slightly without pressure drop (diagnostic)
            // Per NRC HRTD 2.1: "Level decreases without pressure decrease"
            // ============================================================
            case BubbleFormationPhase.DETECTION:
                if (phaseElapsed >= PlantConstants.BUBBLE_PHASE_DETECTION_HR)
                {
                    bubblePhase = BubbleFormationPhase.VERIFICATION;
                    bubblePhaseStartTime = simTime;
                    LogEvent(EventSeverity.ACTION, "Bubble confirmed by instruments - beginning verification");
                    Debug.Log($"[T+{simTime:F2}hr] Bubble DETECTION complete, entering VERIFICATION");
                }
                heatupPhaseDesc = "BUBBLE FORMATION - DETECTION";
                statusMessage = $"BUBBLE DETECTED - CONFIRMING ({phaseElapsed * 60:F0}/{PlantConstants.BUBBLE_PHASE_DETECTION_HR * 60:F0} min)";
                break;

            // ============================================================
            // VERIFICATION (~5 min): Aux spray test confirms compressible gas
            // v0.2.0: Models actual aux spray test per NRC HRTD 19.2.2.
            // ============================================================
            case BubbleFormationPhase.VERIFICATION:
                UpdateVerificationPhase(dt, phaseElapsed);
                break;

            // ============================================================
            // DRAIN (~40 min): Thermodynamic drain from ~100% to 25%
            // v0.2.0: Steam displacement is primary mechanism.
            // ============================================================
            case BubbleFormationPhase.DRAIN:
                UpdateDrainPhase(dt, phaseElapsed);
                drainActive = true;
                break;

            // ============================================================
            // STABILIZE (~10 min): CVCS rebalanced, auto level control
            // ============================================================
            case BubbleFormationPhase.STABILIZE:
                ApplyTwoPhasePressurizerHeating(
                    dt,
                    "BUBBLE_STABILIZE_MASS_CLOSURE");
                if (phaseElapsed >= PlantConstants.BUBBLE_PHASE_STABILIZE_HR)
                {
                    bubblePhase = BubbleFormationPhase.PRESSURIZE;
                    bubblePhaseStartTime = simTime;

                    // v0.2.0: Transition heater mode to pressurize auto
                    currentHeaterMode = HeaterMode.PRESSURIZE_AUTO;

                    LogEvent(EventSeverity.ACTION, "CVCS stabilized - pressurizing for RCP start");
                    LogEvent(EventSeverity.INFO, $"Heater mode: PRESSURIZE_AUTO (target >= {PlantConstants.MIN_RCP_PRESSURE_PSIG:F0} psig)");
                    Debug.Log($"[T+{simTime:F2}hr] STABILIZE complete, entering PRESSURIZE");
                }
                heatupPhaseDesc = "BUBBLE FORMATION - STABILIZING CVCS";
                statusMessage = $"CVCS STABILIZING ({phaseElapsed * 60:F0}/{PlantConstants.BUBBLE_PHASE_STABILIZE_HR * 60:F0} min) | Level={pzrLevel:F1}%";
                break;

            // ============================================================
            // PRESSURIZE: Heaters raise pressure to >=320 psig for RCP NPSH
            // ============================================================
            case BubbleFormationPhase.PRESSURIZE:
                UpdatePressurizePhase(dt);
                break;
        }

        return drainActive;
    }

    // ========================================================================
    // VERIFICATION PHASE — Aux spray test per NRC HRTD 19.2.2
    // ========================================================================

    void UpdateVerificationPhase(float dt, float phaseElapsed)
    {
        // Start aux spray test at beginning of verification phase
        if (!auxSprayActive && phaseElapsed < 0.001f)
        {
            auxSprayActive = true;
            auxSprayStartTime = simTime;
            auxSprayPressureBefore = pressure;
            auxSprayPressureDrop = 0f;
            LogEvent(EventSeverity.ACTION, $"AUX SPRAY TEST initiated at P={pressure:F0} psia");
        }

        // Aux spray effect: condensing spray reduces steam pressure
        if (auxSprayActive)
        {
            float sprayElapsed_sec = (simTime - auxSprayStartTime) * 3600f;
            if (sprayElapsed_sec < PlantConstants.AUX_SPRAY_TEST_DURATION_SEC)
            {
                // Spray condensation reduces pressure ~0.2-0.3 psi/sec
                float sprayEffect_psi_sec = 0.25f;
                float nextPressure = pressure - sprayEffect_psi_sec * (dt * 3600f);
                ApplyPressureWrite(nextPressure, "BUBBLE_VERIFICATION_AUX_SPRAY", stateDerived: true);
                auxSprayPressureDrop = auxSprayPressureBefore - pressure;
            }
            else if (sprayElapsed_sec >= PlantConstants.AUX_SPRAY_TEST_DURATION_SEC && auxSprayActive)
            {
                // Spray secured — log result
                auxSprayActive = false;
                auxSprayPressureDrop = auxSprayPressureBefore - pressure;
                auxSprayTestPassed = (auxSprayPressureDrop >= PlantConstants.AUX_SPRAY_MIN_PRESSURE_DROP &&
                                      auxSprayPressureDrop <= PlantConstants.AUX_SPRAY_MAX_PRESSURE_DROP);
                LogEvent(EventSeverity.INFO, $"AUX SPRAY SECURED: dP={auxSprayPressureDrop:F1} psi ({(auxSprayTestPassed ? "PASS" : "MARGINAL")})");
                Debug.Log($"[T+{simTime:F2}hr] Aux spray result: dP={auxSprayPressureDrop:F1} psi (expected 5-15)");
            }
        }

        // Phase transition to DRAIN when verification time elapsed
        if (phaseElapsed >= PlantConstants.BUBBLE_PHASE_VERIFY_HR)
        {
            bubblePhase = BubbleFormationPhase.DRAIN;
            bubblePhaseStartTime = simTime;
            bubbleDrainStartLevel = pzrLevel;
            bubblePreDrainPhase = false;  // v1.3.1.0: Solid-plant CVCS control ends
            ccpStarted = false;  // v0.2.0: CCP not yet started
            drainSteamDisplacement_lbm = 0f;
            drainCvcsTransfer_lbm = 0f;
            drainDuration_hr = 0f;
            drainExitPressure_psia = 0f;
            drainExitLevel_pct = 0f;
            drainHardGateTriggered = false;
            drainPressureBandMaintained = true;
            drainTransitionReason = "IN_PROGRESS";
            drainCvcsPolicyMode = "LINEUP_HYDRAULIC_CAUSAL";
            drainLetdownFlow_gpm = 0f;
            drainLetdownDemand_gpm = 0f;
            drainChargingFlow_gpm = 0f;
            drainNetOutflowFlow_gpm = 0f;
            drainLineupDemandIndex = GetDrainLineupIndex(orifice75Count, orifice45Open);
            drainHydraulicCapacity_gpm = 0f;
            drainHydraulicDeltaP_psi = 0f;
            drainHydraulicDensity_lbm_ft3 = 0f;
            drainHydraulicQuality = 0f;
            drainLetdownSaturated = false;
            drainLineupEventThisStep = false;
            drainLineupEventCount = 0;
            drainLastLineupEventTime_hr = -1f;
            drainLastLineupPrevIndex = drainLineupDemandIndex;
            drainLastLineupNewIndex = drainLineupDemandIndex;
            drainLastLineupTrigger = "NONE";
            drainLastLineupReason = "NONE";
            drainLineupChangePending = false;
            drainLineupRequestedIndex = drainLineupDemandIndex;
            drainLineupRequestedTrigger = "NONE";
            drainLineupRequestedReason = "NONE";
            SeedPzrEnergyStateFromCurrentMasses("BUBBLE_DRAIN_ENTRY");

            // v0.2.0: Transition heater mode to auto pressure-rate feedback
            currentHeaterMode = HeaterMode.BUBBLE_FORMATION_AUTO;

            LogEvent(EventSeverity.ACTION, $"Bubble verified - beginning thermodynamic drain from {pzrLevel:F0}% to {PlantConstants.PZR_LEVEL_AFTER_BUBBLE:F0}%");
            LogEvent(EventSeverity.INFO,
                $"DRAIN policy={drainCvcsPolicyMode}, letdown envelope " +
                $"{PlantConstants.BUBBLE_DRAIN_PROCEDURE_MIN_LETDOWN_GPM:F0}-" +
                $"{PlantConstants.BUBBLE_DRAIN_PROCEDURE_MAX_LETDOWN_GPM:F0} gpm, CCP charging ramp enabled");
            Debug.Log($"[T+{simTime:F2}hr] Bubble VERIFIED, entering DRAIN phase (thermodynamic)");
            Debug.Log($"  Steam displacement primary, CVCS trim secondary");
            Debug.Log($"  CVCS policy={drainCvcsPolicyMode}, CCP starts below {PlantConstants.CCP_START_LEVEL:F0}%");
        }

        heatupPhaseDesc = "BUBBLE FORMATION - VERIFICATION";
        float sprayElapsedDisp = auxSprayActive ? (simTime - auxSprayStartTime) * 3600f : PlantConstants.AUX_SPRAY_TEST_DURATION_SEC;
        statusMessage = auxSprayActive
            ? $"AUX SPRAY TEST IN PROGRESS ({sprayElapsedDisp:F0}s / {PlantConstants.AUX_SPRAY_TEST_DURATION_SEC:F0}s) dP={auxSprayPressureDrop:F1}psi"
            : $"AUX SPRAY COMPLETE dP={auxSprayPressureDrop:F1}psi - RECOVERING ({phaseElapsed * 60:F0}/{PlantConstants.BUBBLE_PHASE_VERIFY_HR * 60:F0} min)";
    }

    // ========================================================================
    // DRAIN PHASE — Thermodynamic steam displacement + CVCS trim
    // Per NRC HRTD 19.2.2 / 2.1
    // ========================================================================

    void UpdateDrainPhase(float dt, float phaseElapsed)
    {
        float dt_sec_drain = dt * 3600f;

        // ============================================================
        // v5.4.1 Stage 0: Snapshot masses BEFORE any changes for forensic audit
        // ============================================================
        float pzrWaterMass_before = physicsState.PZRWaterMass;
        float pzrSteamMass_before = physicsState.PZRSteamMass;
        float pzrTotalMass_before = pzrWaterMass_before + pzrSteamMass_before;
        float pzrLevel_before = pzrLevel;
        float rcsMass_before = physicsState.RCSWaterMass;
        float systemMass_before = rcsMass_before + pzrTotalMass_before;

        // Water properties at current PZR conditions
        float rhoWater = WaterProperties.WaterDensity(T_pzr, pressure);
        float hLiquidOut_BTU_lb = WaterProperties.SaturatedLiquidEnthalpy(Mathf.Max(20f, pressure));

        // ============================================================
        // MECHANISM 1 (PRIMARY): PZR energy update.
        // Two-phase closure now solves pressure and phase split from:
        //   - total PZR mass after net flows
        //   - persistent total PZR enthalpy after this timestep's energy terms
        // ============================================================
        float grossHeaterPower_MW = pzrHeaterPower;
        float grossHeaterEnergy_BTU = Mathf.Max(0f, grossHeaterPower_MW) * PlantConstants.MW_TO_BTU_SEC * dt_sec_drain;
        float conductionLoss_BTU = Mathf.Max(0f, pzrConductionLoss_MW) * PlantConstants.MW_TO_BTU_SEC * dt_sec_drain;
        float insulationLoss_BTU = Mathf.Max(0f, pzrInsulationLoss_MW) * PlantConstants.MW_TO_BTU_SEC * dt_sec_drain;

        // ============================================================
        // MECHANISM 2 (SECONDARY): CVCS trim
        // v0.2.0: Charging starts at 0 gpm. CCP starts at level < 80%.
        // v5.4.1 Stage 1: CVCS drain is mass LEAVING the PZR into RCS.
        // dm_out_of_PZR == dm_into_RCS in the same timestep.
        // ============================================================
        ResolveDrainCvcsPolicy(
            out float currentLetdown_gpm,
            out float currentCharging_gpm,
            out float netOutflow_gpm);

        // v5.4.1 Stage 1: Compute CVCS drain as MASS transfer, not volume
        float dm_cvcsDrain_lbm = netOutflow_gpm * PlantConstants.GPM_TO_FT3_SEC * dt_sec_drain * rhoWater;

        // ============================================================
        // MASS + ENERGY LEDGER TERMS THIS STEP
        //   Mass: PZR total mass changes only by net CVCS outflow.
        //   Energy: +heater input - thermal losses - enthalpy carried by outflow.
        // ============================================================
        float dm_cvcsActual = Mathf.Min(dm_cvcsDrain_lbm, Mathf.Max(0f, pzrWaterMass_before));
        float targetPzrMass_lbm = Mathf.Max(1f, pzrTotalMass_before - dm_cvcsActual);
        float netPzrEnergyDelta_BTU = grossHeaterEnergy_BTU - conductionLoss_BTU - insulationLoss_BTU
                                      - dm_cvcsActual * hLiquidOut_BTU_lb;

        bool closureConverged = UpdateTwoPhaseStateFromMassClosure(
            "BUBBLE_DRAIN_MASS_CLOSURE",
            dt,
            targetPzrMass_lbm,
            netPzrEnergyDelta_BTU);

        if (closureConverged)
        {
            // Commit externalized transfer only after closure converges.
            physicsState.RCSWaterMass += dm_cvcsActual;
            drainCvcsTransfer_lbm += dm_cvcsActual;
            float dm_steamActual = Mathf.Max(0f, physicsState.PZRSteamMass - pzrSteamMass_before);
            drainSteamDisplacement_lbm += dm_steamActual;
        }
        else
        {
            dm_cvcsActual = 0f;
        }

        drainDuration_hr = phaseElapsed;
        if (pressure < PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA)
            drainPressureBandMaintained = false;

        // IP-0022 CS-0039: DRAIN CVCS transfer is an internal PZR↔RCS movement.
        // Do not mutate VCT RCS-change accumulator here; PBOC owns boundary tracking.

        float dm_steamActual_step = physicsState.PZRSteamMass - pzrSteamMass_before;

        // Update display variables
        pzrWaterVolume = physicsState.PZRWaterVolume;
        pzrSteamVolume = physicsState.PZRSteamVolume;
        pzrLevel = physicsState.PZRLevel;

        // Update CVCS display flows for this phase
        letdownFlow = currentLetdown_gpm;
        chargingFlow = currentCharging_gpm;
        drainLetdownFlow_gpm = currentLetdown_gpm;
        drainChargingFlow_gpm = currentCharging_gpm;
        drainNetOutflowFlow_gpm = netOutflow_gpm;

        // Recalculate total system mass
        totalSystemMass = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;

        // ============================================================
        // v5.4.1 Stage 0: Forensic drain mass audit (0.1 lbm precision)
        // Logs Δm for each mass transfer to detect conservation violations.
        // ============================================================
        float pzrTotalMass_after = physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float systemMass_after = physicsState.RCSWaterMass + pzrTotalMass_after;
        float dm_pzr = pzrTotalMass_after - pzrTotalMass_before;
        float dm_system = systemMass_after - systemMass_before;

        TryLogPzrOrificeDiagnostics(
            "DRAIN_INTERNAL_TRANSFER",
            currentLetdown_gpm,
            currentCharging_gpm,
            pzrLevel_before,
            pzrLevel,
            pzrTotalMass_before,
            pzrTotalMass_after);

        // Flag if mass created/destroyed beyond numerical tolerance (0.1 lbm)
        bool massViolation = Mathf.Abs(dm_system) > 0.1f;

        // Periodic forensic logging (every 5 sim-minutes during DRAIN)
        if (phaseElapsed < dt || (simTime % (5f / 60f) < dt))
        {
            Debug.Log($"[T+{simTime:F2}hr] DRAIN AUDIT: Level={pzrLevel:F1}%, " +
                $"dm_steam={dm_steamActual_step:F1}lbm, dm_cvcs={dm_cvcsActual:F1}lbm, " +
                $"Δm_PZR={dm_pzr:F1}lbm, Δm_sys={dm_system:F1}lbm{(massViolation ? " *** VIOLATION ***" : "")}");
            Debug.Log($"  PZR: water={physicsState.PZRWaterMass:F1}lbm, steam={physicsState.PZRSteamMass:F1}lbm, " +
                $"total={pzrTotalMass_after:F1}lbm | RCS={physicsState.RCSWaterMass:F0}lbm | " +
                $"SysTotal={systemMass_after:F0}lbm");
        }

        if (massViolation)
        {
            LogEvent(EventSeverity.ALERT,
                $"DRAIN Δm_sys={dm_system:F1}lbm (steam reconciliation at V={pzrSteamVolume:F1}ft³)");
        }

        // ============================================================
        // v0.3.0.0 Phase D (Fix 3.3): DRAIN→STABILIZE continuity guard
        // Level gate is primary (mandatory). Pressure rate is advisory:
        // if pressure is changing rapidly at drain exit, log a warning
        // but still transition (prevents infinite DRAIN).
        // ============================================================
        bool levelReached = pzrLevel <= PlantConstants.PZR_LEVEL_AFTER_BUBBLE + 0.5f;
        bool pressureInBand = pressure >= PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA;
        bool hardDurationExceeded = phaseElapsed >= 60f / 60f;
        bool readyByLevelAndPressure = levelReached && pressureInBand;
        if (readyByLevelAndPressure || hardDurationExceeded)
        {
            // Advisory pressure stability check (not blocking)
            bool pressureStable = Mathf.Abs(pressureRate) < PlantConstants.MAX_DRAIN_EXIT_PRESSURE_RATE;
            if (!pressureStable)
            {
                LogEvent(EventSeverity.ALERT,
                    $"DRAIN exit advisory: pressure rate {pressureRate:F1} psi/hr exceeds " +
                    $"{PlantConstants.MAX_DRAIN_EXIT_PRESSURE_RATE:F0} psi/hr threshold — proceeding to STABILIZE");
                Debug.LogWarning($"[T+{simTime:F2}hr] DRAIN→STABILIZE: pressureRate={pressureRate:F1} psi/hr " +
                    $"(advisory limit {PlantConstants.MAX_DRAIN_EXIT_PRESSURE_RATE:F0} psi/hr)");
            }

            if (hardDurationExceeded && !readyByLevelAndPressure)
            {
                drainHardGateTriggered = true;
                LogEvent(EventSeverity.ALERT,
                    $"DRAIN hard duration gate reached at {phaseElapsed * 60f:F1} min; forcing transition.");
            }

            drainDuration_hr = phaseElapsed;
            drainExitPressure_psia = pressure;
            drainExitLevel_pct = pzrLevel;
            drainTransitionReason = hardDurationExceeded && !readyByLevelAndPressure
                ? "HARD_TIMEOUT"
                : "LEVEL_PRESSURE_READY";

            float displacementTotal_lbm = drainSteamDisplacement_lbm + drainCvcsTransfer_lbm;
            float steamFrac = displacementTotal_lbm > 1e-6f
                ? drainSteamDisplacement_lbm / displacementTotal_lbm
                : 0f;

            bubblePhase = BubbleFormationPhase.STABILIZE;
            bubblePhaseStartTime = simTime;

            // DP-0002 Group 4 (CS-0059): do not invoke CVCSController.Initialize
            // from UPDATE transition flow. Re-seed the pre-initialized state.
            float initialSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
            ReSeedCvcsControllerForTwoPhase(initialSetpoint);

            LogEvent(EventSeverity.ACTION, $"PZR drain complete at {pzrLevel:F1}% - stabilizing CVCS");
            LogEvent(EventSeverity.INFO, $"CCP running: {(ccpStarted ? "YES" : "NO")}, Aux spray: {(auxSprayTestPassed ? "PASSED" : "N/A")}");
            LogEvent(EventSeverity.INFO, $"DRAIN final mass audit: Δm_sys={dm_system:F1}lbm, PZR_total={pzrTotalMass_after:F0}lbm");
            LogEvent(EventSeverity.INFO, $"DRAIN exit pressure rate: {pressureRate:F1} psi/hr ({(pressureStable ? "STABLE" : "ADVISORY")})");
            LogEvent(EventSeverity.INFO,
                $"DRAIN reconciliation: steam={drainSteamDisplacement_lbm:F1} lbm, cvcs={drainCvcsTransfer_lbm:F1} lbm, steam_frac={steamFrac:F2}");
            LogEvent(EventSeverity.INFO,
                $"DRAIN gates: duration={phaseElapsed * 60f:F1} min, pressureInBand={pressureInBand}, levelReached={levelReached}, transition={drainTransitionReason}");
            LogEvent(EventSeverity.INFO,
                $"DRAIN CVCS policy: mode={drainCvcsPolicyMode}, letdown={drainLetdownFlow_gpm:F1} gpm, charging={drainChargingFlow_gpm:F1} gpm, netOut={drainNetOutflowFlow_gpm:F1} gpm");
            Debug.Log($"[T+{simTime:F2}hr] DRAIN complete. Level={pzrLevel:F1}%, Steam={pzrSteamVolume:F0}ft³, dP/dt={pressureRate:F1} psi/hr");
        }
        else
        {
            float drainProgress = (bubbleDrainStartLevel - pzrLevel) / (bubbleDrainStartLevel - PlantConstants.PZR_LEVEL_AFTER_BUBBLE) * 100f;
            heatupPhaseDesc = $"BUBBLE FORMATION - DRAINING ({drainProgress:F0}%)";
            statusMessage =
                $"PZR DRAIN: {pzrLevel:F1}% → {PlantConstants.PZR_LEVEL_AFTER_BUBBLE:F0}% | " +
                $"LD={currentLetdown_gpm:F0} CHG={currentCharging_gpm:F0} NET={netOutflow_gpm:F0} gpm ({drainCvcsPolicyMode})";
        }
    }

    void ResolveDrainCvcsPolicy(
        out float letdown_gpm,
        out float charging_gpm,
        out float netOutflow_gpm)
    {
        // Stage H remediation: lineup transitions are explicit events only.
        // Demand can exceed current lineup capacity, but lineup does not auto-open.
        drainLineupEventThisStep = false;

        float levelSpan = Mathf.Max(1f, bubbleDrainStartLevel - PlantConstants.PZR_LEVEL_AFTER_BUBBLE);
        float levelFraction = Mathf.Clamp01(
            (pzrLevel - PlantConstants.PZR_LEVEL_AFTER_BUBBLE) / levelSpan);

        float letdownMin = PlantConstants.BUBBLE_DRAIN_PROCEDURE_MIN_LETDOWN_GPM;
        float letdownMax = PlantConstants.BUBBLE_DRAIN_PROCEDURE_MAX_LETDOWN_GPM;
        int targetLineupIndex = levelFraction >= 0.67f ? 3 : (levelFraction >= 0.34f ? 2 : 1);
        drainLineupDemandIndex = targetLineupIndex;

        ApplyPendingDrainLineupChange();
        int currentLineupIndex = GetDrainLineupIndex(orifice75Count, orifice45Open);

        float pressure_psig = pressure - PlantConstants.PSIG_TO_PSIA;
        float hydraulicLetdown_gpm = PlantConstants.CalculateOrificeLineupFlow(
            pressure_psig,
            orifice75Count,
            orifice45Open);
        drainHydraulicDeltaP_psi = Mathf.Max(0f, pressure_psig - PlantConstants.LETDOWN_BACKPRESSURE_PSIG);
        drainHydraulicDensity_lbm_ft3 = Mathf.Max(
            0f,
            WaterProperties.WaterDensity(T_pzr, Mathf.Max(TWO_PHASE_CLOSURE_HARD_MIN_PRESSURE_PSIA, pressure)));
        drainHydraulicQuality = Mathf.Clamp01(pzrClosureLastPhaseFraction);
        drainHydraulicCapacity_gpm = Mathf.Clamp(hydraulicLetdown_gpm, letdownMin, letdownMax);

        float demandLetdown_gpm = Mathf.Lerp(letdownMin, letdownMax, levelFraction);
        drainLetdownDemand_gpm = demandLetdown_gpm;
        letdown_gpm = Mathf.Min(demandLetdown_gpm, drainHydraulicCapacity_gpm);
        drainLetdownSaturated = demandLetdown_gpm > drainHydraulicCapacity_gpm + 1e-4f;
        if (drainLetdownSaturated && !drainLineupEventThisStep && targetLineupIndex > currentLineupIndex)
        {
            LogEvent(
                EventSeverity.INFO,
                $"DRAIN LETDOWN SATURATED: demand={demandLetdown_gpm:F1} gpm, achieved={letdown_gpm:F1} gpm, " +
                $"lineup={GetDrainLineupDescription(currentLineupIndex)}, targetLineup={GetDrainLineupDescription(targetLineupIndex)}");
        }

        charging_gpm = PlantConstants.BUBBLE_DRAIN_CHARGING_INITIAL_GPM;

        if (!ccpStarted && pzrLevel < PlantConstants.CCP_START_LEVEL)
        {
            ccpStarted = true;
            ccpStartTime = simTime;
            ccpStartLevel = pzrLevel;
            LogEvent(
                EventSeverity.ACTION,
                $"CCP STARTED at PZR level {pzrLevel:F1}% (< {PlantConstants.CCP_START_LEVEL:F0}%)");
            Debug.Log(
                $"[T+{simTime:F2}hr] === CCP STARTED === Level={pzrLevel:F1}%, policy={drainCvcsPolicyMode}");
        }

        if (ccpStarted)
        {
            // CS-0073: dynamic post-CCP charging (not fixed 44 gpm).
            float toTargetFraction = Mathf.Clamp01(1f - levelFraction);
            float chargingRamp = toTargetFraction;
            charging_gpm = PlantConstants.BUBBLE_DRAIN_CHARGING_CCP_GPM * chargingRamp;
        }

        netOutflow_gpm = Mathf.Max(0f, letdown_gpm - charging_gpm);
    }

    bool RequestDrainLineupChange(
        int newLineupIndex,
        string triggerCondition,
        string reason)
    {
        drainLineupChangePending = true;
        drainLineupRequestedIndex = Mathf.Clamp(newLineupIndex, 1, 3);
        drainLineupRequestedTrigger = string.IsNullOrWhiteSpace(triggerCondition)
            ? "UNSPECIFIED_TRIGGER"
            : triggerCondition.Trim();
        drainLineupRequestedReason = string.IsNullOrWhiteSpace(reason)
            ? "UNSPECIFIED_REASON"
            : reason.Trim();
        return true;
    }

    void ApplyPendingDrainLineupChange()
    {
        if (!drainLineupChangePending)
            return;

        drainLineupChangePending = false;
        int previousIndex = GetDrainLineupIndex(orifice75Count, orifice45Open);
        int requestedIndex = Mathf.Clamp(drainLineupRequestedIndex, 1, 3);
        if (requestedIndex == previousIndex)
            return;

        SetDrainLineupByIndexInternal(requestedIndex);
        drainLineupEventThisStep = true;
        drainLineupEventCount++;
        drainLastLineupEventTime_hr = simTime;
        drainLastLineupPrevIndex = previousIndex;
        drainLastLineupNewIndex = requestedIndex;
        drainLastLineupTrigger = drainLineupRequestedTrigger;
        drainLastLineupReason = drainLineupRequestedReason;

        LogEvent(
            EventSeverity.INFO,
            $"DRAIN LINEUP EVENT: {GetDrainLineupDescription(previousIndex)} -> {GetDrainLineupDescription(requestedIndex)} " +
            $"trigger={drainLastLineupTrigger}, reason={drainLastLineupReason}, sim_hr={simTime:F4}");
    }

    int GetDrainLineupIndex(int num75Open, bool open45)
    {
        if (num75Open >= 2 && open45)
            return 3;
        if (num75Open >= 2 || open45)
            return 2;
        return 1;
    }

    string GetDrainLineupDescription(int lineupIndex)
    {
        switch (Mathf.Clamp(lineupIndex, 1, 3))
        {
            case 3:
                return "2x75 + 1x45 gpm";
            case 2:
                return "1x75 + 1x45 gpm";
            default:
                return "1x75 gpm";
        }
    }

    void SetDrainLineupByIndexInternal(int lineupIndex)
    {
        switch (Mathf.Clamp(lineupIndex, 1, 3))
        {
            case 3:
                orifice75Count = 2;
                orifice45Open = true;
                orificeLineupDesc = "2x75 + 1x45 gpm (DRAIN_EVENT)";
                break;

            case 2:
                orifice75Count = 1;
                orifice45Open = true;
                orificeLineupDesc = "1x75 + 1x45 gpm (DRAIN_EVENT)";
                break;

            default:
                orifice75Count = 1;
                orifice45Open = false;
                orificeLineupDesc = "1x75 gpm (DRAIN_EVENT)";
                break;
        }
    }

    void ReSeedCvcsControllerForTwoPhase(float levelSetpoint)
    {
        cvcsControllerState.IsActive = true;
        cvcsControllerState.LevelSetpoint = levelSetpoint;
        cvcsControllerState.LevelError = pzrLevel - levelSetpoint;
        cvcsControllerState.LastLevelError = cvcsControllerState.LevelError;
        cvcsControllerState.IntegralError = 0f;
        cvcsControllerState.ChargingFlow = PlantConstants.LETDOWN_NORMAL_GPM;
        cvcsControllerState.LetdownFlow = PlantConstants.LETDOWN_NORMAL_GPM;
        cvcsControllerState.SealInjection = 0f;
        cvcsControllerState.LetdownIsolated = false;
    }

    // ========================================================================
    // PRESSURIZE PHASE — Heaters raise pressure for RCP NPSH
    // Per NRC HRTD 19.2.2
    // ========================================================================

    void UpdatePressurizePhase(float dt)
    {
        ApplyTwoPhasePressurizerHeating(
            dt,
            "BUBBLE_PRESSURIZE_MASS_CLOSURE");

        float pressure_psig = pressure - 14.7f;
        float minRcpP_psig = PlantConstants.MIN_RCP_PRESSURE_PSIG;

        // ============================================================
        // v0.3.0.0 Phase D (Fix 3.3): PRESSURIZE→COMPLETE continuity guard
        // Pressure gate is primary (mandatory, unchanged).
        // Level stability gate is mandatory: prevents completing bubble
        // formation if PZR level has drifted far from the DRAIN target.
        // This ensures RCP NPSH conditions are met at both pressure AND level.
        // ============================================================
        bool pressureSufficient = pressure_psig >= minRcpP_psig;
        bool levelStable = pzrLevel >= PlantConstants.PZR_LEVEL_AFTER_BUBBLE - 2f;

        if (pressureSufficient && levelStable)
        {
            // Bubble formation complete!
            bubblePhase = BubbleFormationPhase.COMPLETE;
            bubbleFormed = true;
            bubbleFormationTime = simTime;

            // Pre-seed PI integral for stable RCP start transition (Bug #4 Fix)
            CVCSController.PreSeedForRCPStart(ref cvcsControllerState, pzrLevel, T_avg, 0);

            LogEvent(EventSeverity.ALERT, $"BUBBLE FORMATION COMPLETE at P={pressure_psig:F0}psig, Level={pzrLevel:F1}% - READY FOR RCPs");
            LogEvent(EventSeverity.INFO, $"Total bubble formation time: {(simTime - bubbleFormationTemp):F1} min");
            Debug.Log($"[T+{simTime:F2}hr] === BUBBLE FORMATION COMPLETE ===");
            Debug.Log($"  P={pressure:F0}psia ({pressure_psig:F0}psig), Level={pzrLevel:F1}%");
            Debug.Log($"  Steam space: {pzrSteamVolume:F0}ft³, RCP start permitted");
        }
        else if (pressureSufficient && !levelStable)
        {
            // Pressure met but level has drifted below acceptable range — hold in PRESSURIZE
            float pProgress = pressure_psig / minRcpP_psig * 100f;
            heatupPhaseDesc = "BUBBLE FORMATION - PRESSURIZING (LEVEL LOW)";
            statusMessage = $"PRESSURIZE HOLD: P={pressure_psig:F0}psig OK, Level={pzrLevel:F1}% < {PlantConstants.PZR_LEVEL_AFTER_BUBBLE - 2f:F0}% min";
            Debug.LogWarning($"[T+{simTime:F2}hr] PRESSURIZE→COMPLETE blocked: pressure OK ({pressure_psig:F0} psig) " +
                $"but level too low ({pzrLevel:F1}% < {PlantConstants.PZR_LEVEL_AFTER_BUBBLE - 2f:F0}%)");
        }
        else
        {
            float pProgress = pressure_psig / minRcpP_psig * 100f;
            heatupPhaseDesc = "BUBBLE FORMATION - PRESSURIZING";
            statusMessage = $"PRESSURIZING: {pressure_psig:F0}/{minRcpP_psig:F0} psig ({pProgress:F0}%) | Level={pzrLevel:F1}% | Heaters ON";
        }
    }
    void ApplyTwoPhasePressurizerHeating(
        float dt,
        string stateSource)
    {
        float dtSec = dt * 3600f;
        float grossHeaterEnergy_BTU = Mathf.Max(0f, pzrHeaterPower) * PlantConstants.MW_TO_BTU_SEC * dtSec;
        float conductionLoss_BTU = Mathf.Max(0f, pzrConductionLoss_MW) * PlantConstants.MW_TO_BTU_SEC * dtSec;
        float insulationLoss_BTU = Mathf.Max(0f, pzrInsulationLoss_MW) * PlantConstants.MW_TO_BTU_SEC * dtSec;
        float netPzrEnergyDelta_BTU = grossHeaterEnergy_BTU - conductionLoss_BTU - insulationLoss_BTU;
        float targetPzrMass_lbm = Mathf.Max(1f, physicsState.PZRWaterMass + physicsState.PZRSteamMass);

        bool converged = UpdateTwoPhaseStateFromMassClosure(
            stateSource,
            dt,
            targetPzrMass_lbm,
            netPzrEnergyDelta_BTU);

        if (!converged)
        {
            LogEvent(
                EventSeverity.ALERT,
                $"Two-phase closure failed in {stateSource}: state hold (Vres={pzrClosureVolumeResidual_ft3:F2}ft^3, Eres={pzrClosureEnergyResidual_BTU:F1}BTU)");
        }
    }

    const float TWO_PHASE_CLOSURE_VOLUME_TOLERANCE_FT3 = 1f;
    const float TWO_PHASE_CLOSURE_SPECIFIC_ENTHALPY_TOL_BTU_LB = 0.5f;
    const float TWO_PHASE_CLOSURE_MASS_CONTRACT_TOL_LBM = 0.1f;
    const int TWO_PHASE_CLOSURE_SCAN_POINTS = 48;
    const int TWO_PHASE_CLOSURE_MAX_ITERATIONS = 48;
    const float TWO_PHASE_BRACKET_INITIAL_HALFSPAN_PSIA = 15f;
    const float TWO_PHASE_BRACKET_GROWTH = 1.8f;
    const int TWO_PHASE_BRACKET_MAX_WINDOWS_PER_BAND = 8;
    const float TWO_PHASE_CLOSURE_HARD_MIN_PRESSURE_PSIA = 20f;
    const float TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA = 2800f;

    enum TwoPhaseEvalStatus
    {
        OK,
        V_EVAL_NAN,
        EOS_OUT_OF_RANGE,
        INFEASIBLE_ENERGY_FOR_MASS_VOLUME
    }

    enum TwoPhaseRegime
    {
        SubcooledLiquid,
        SaturatedMixture,
        SuperheatedSteam
    }

    struct TwoPhaseClosurePoint
    {
        public float Pressure_psia;
        public float QualityRaw;
        public float QualityClamped;
        public float WaterMass_lbm;
        public float SteamMass_lbm;
        public float WaterVolume_ft3;
        public float SteamVolume_ft3;
        public float VolumeResidual_ft3;
        public float EnergyResidual_BTU;
        public float EnthalpyTotal_BTU;
        public string Regime;
    }

    struct TwoPhaseBracketSearchResult
    {
        public bool HasBracket;
        public TwoPhaseClosurePoint BracketLow;
        public TwoPhaseClosurePoint BracketHigh;
        public bool HasBestPoint;
        public TwoPhaseClosurePoint BestPoint;
        public int ValidEvaluations;
        public int InvalidEvaluations;
        public int NanEvaluations;
        public int OutOfRangeEvaluations;
        public int WindowsTried;
        public bool PositiveResidualSeen;
        public bool NegativeResidualSeen;
        public float MinEvaluatedVolume_ft3;
        public float MaxEvaluatedVolume_ft3;
        public string FailReason;
        public string Trace;
    }

    bool UpdateTwoPhaseStateFromMassClosure(
        string stateSource,
        float dt_hr,
        float targetTotalMass_lbm,
        float deltaEnthalpy_BTU)
    {
        if (!EnsurePzrEnergyStateInitialized(stateSource))
            return false;

        float pressureFloor_psia = 20f;
        if (bubblePhase == BubbleFormationPhase.DRAIN ||
            bubblePhase == BubbleFormationPhase.STABILIZE ||
            bubblePhase == BubbleFormationPhase.PRESSURIZE)
        {
            pressureFloor_psia = Mathf.Max(pressureFloor_psia, PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA);
        }

        targetTotalMass_lbm = Mathf.Max(1f, targetTotalMass_lbm);
        float targetEnthalpy_BTU = Mathf.Max(0f, physicsState.PZRTotalEnthalpy_BTU + deltaEnthalpy_BTU);
        float operatingCeiling_psia = PlantConstants.PZR_BASELINE_PORV_OPEN_PSIG + PlantConstants.PSIG_TO_PSIA;
        operatingCeiling_psia = Mathf.Clamp(
            operatingCeiling_psia,
            pressureFloor_psia + 1f,
            TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA);
        float pressureGuess_psia = Mathf.Clamp(Mathf.Max(20f, pressure), pressureFloor_psia, operatingCeiling_psia);
        float energyTolerance_BTU = Mathf.Max(50f, targetTotalMass_lbm * TWO_PHASE_CLOSURE_SPECIFIC_ENTHALPY_TOL_BTU_LB);

        pzrClosureSolveAttempts++;

        bool solved = TrySolveTwoPhaseMassEnergyClosure(
            stateSource,
            targetTotalMass_lbm,
            targetEnthalpy_BTU,
            pressureGuess_psia,
            pressureFloor_psia,
            operatingCeiling_psia,
            TWO_PHASE_CLOSURE_VOLUME_TOLERANCE_FT3,
            energyTolerance_BTU,
            out TwoPhaseClosurePoint solvedPoint,
            out int iterationsUsed,
            out string failReason,
            out string convergencePattern);

        if (!solved)
        {
            physicsState.PZRClosureConverged = false;
            physicsState.PZRClosureVolumeResidual_ft3 = solvedPoint.VolumeResidual_ft3;
            physicsState.PZRClosureEnergyResidual_BTU = solvedPoint.EnergyResidual_BTU;
            pzrClosureLastIterationCount = iterationsUsed;
            pzrClosureLastFailureReason = failReason;
            pzrClosureLastConvergencePattern = convergencePattern;
            pzrClosureLastPhaseFraction = solvedPoint.QualityClamped;
            SyncPzrEnergyDiagnosticsFromPhysicsState();

            LogEvent(
                EventSeverity.ALERT,
                $"Two-phase closure failed in {stateSource}: reason={failReason}, pattern={convergencePattern}, Vres={solvedPoint.VolumeResidual_ft3:F2} ft^3, Eres={solvedPoint.EnergyResidual_BTU:F1} BTU");

            if (enablePzrBubbleDiagnostics)
            {
                Debug.Log(
                    $"[PZR_BUBBLE_DIAG][{pzrBubbleDiagnosticsLabel}] CLOSURE_FAIL source={stateSource} sim_hr={simTime:F4} " +
                    $"phase={bubblePhase} reason={failReason} pattern={convergencePattern} iterations={iterationsUsed} " +
                    $"pressure_psia={solvedPoint.Pressure_psia:F3} quality_raw={solvedPoint.QualityRaw:F5} quality={solvedPoint.QualityClamped:F5} " +
                    $"mass_total_lbm={targetTotalMass_lbm:F3} h_total_target_BTU={targetEnthalpy_BTU:F3} " +
                    $"V_residual_ft3={solvedPoint.VolumeResidual_ft3:F3} E_residual_BTU={solvedPoint.EnergyResidual_BTU:F3} " +
                    $"bracket_found={pzrClosureBracketFound} bracket_low_psia={pzrClosureBracketLastLow_psia:F3} " +
                    $"bracket_high_psia={pzrClosureBracketLastHigh_psia:F3} sign_low={pzrClosureBracketResidualSignLow} " +
                    $"sign_high={pzrClosureBracketResidualSignHigh} regime_low={pzrClosureBracketRegimeLow} " +
                    $"regime_high={pzrClosureBracketRegimeHigh} trace={pzrClosureBracketSearchTrace}");
            }

            return false;
        }

        // Stage D state contract: closure-committed mass/volume/energy must match target state.
        float closureMassResidual_lbm =
            (solvedPoint.WaterMass_lbm + solvedPoint.SteamMass_lbm) - targetTotalMass_lbm;
        if (Mathf.Abs(closureMassResidual_lbm) > TWO_PHASE_CLOSURE_MASS_CONTRACT_TOL_LBM)
        {
            physicsState.PZRClosureConverged = false;
            physicsState.PZRClosureVolumeResidual_ft3 = solvedPoint.VolumeResidual_ft3;
            physicsState.PZRClosureEnergyResidual_BTU = solvedPoint.EnergyResidual_BTU;
            pzrClosureLastIterationCount = iterationsUsed;
            pzrClosureLastFailureReason = "MASS_CONTRACT_RESIDUAL";
            pzrClosureLastConvergencePattern = convergencePattern;
            pzrClosureLastPhaseFraction = solvedPoint.QualityClamped;
            SyncPzrEnergyDiagnosticsFromPhysicsState();
            return false;
        }

        ApplyPressureWrite(solvedPoint.Pressure_psia, stateSource, stateDerived: true);
        physicsState.Pressure = pressure;
        physicsState.PZRWaterMass = solvedPoint.WaterMass_lbm;
        physicsState.PZRSteamMass = solvedPoint.SteamMass_lbm;
        physicsState.PZRWaterVolume = solvedPoint.WaterVolume_ft3;
        physicsState.PZRSteamVolume = solvedPoint.SteamVolume_ft3;
        physicsState.PZRTotalEnthalpy_BTU = solvedPoint.EnthalpyTotal_BTU;
        physicsState.PZRClosureConverged = true;
        physicsState.PZRClosureVolumeResidual_ft3 = solvedPoint.VolumeResidual_ft3;
        physicsState.PZRClosureEnergyResidual_BTU = solvedPoint.EnergyResidual_BTU;
        pzrClosureSolveConverged++;
        pzrClosureLastIterationCount = iterationsUsed;
        pzrClosureLastFailureReason = "NONE";
        pzrClosureLastConvergencePattern = convergencePattern;
        pzrClosureLastPhaseFraction = solvedPoint.QualityClamped;

        T_sat = WaterProperties.SaturationTemperature(pressure);
        T_pzr = T_sat;
        pzrWaterVolume = physicsState.PZRWaterVolume;
        pzrSteamVolume = physicsState.PZRSteamVolume;
        pzrLevel = physicsState.PZRLevel;
        SyncPzrEnergyDiagnosticsFromPhysicsState();

        if (enablePzrBubbleDiagnostics)
        {
            Debug.Log(
                $"[PZR_BUBBLE_DIAG][{pzrBubbleDiagnosticsLabel}] CLOSURE_OK source={stateSource} sim_hr={simTime:F4} " +
                $"phase={bubblePhase} dt_hr={dt_hr:F6} pattern={convergencePattern} iterations={iterationsUsed} pressure_psia={solvedPoint.Pressure_psia:F3} " +
                $"quality_raw={solvedPoint.QualityRaw:F5} quality={solvedPoint.QualityClamped:F5} " +
                $"m_water_lbm={solvedPoint.WaterMass_lbm:F3} m_steam_lbm={solvedPoint.SteamMass_lbm:F3} " +
                $"V_water_ft3={solvedPoint.WaterVolume_ft3:F3} V_steam_ft3={solvedPoint.SteamVolume_ft3:F3} " +
                $"V_residual_ft3={solvedPoint.VolumeResidual_ft3:F4} E_residual_BTU={solvedPoint.EnergyResidual_BTU:F4}");
        }

        return true;
    }

    bool EnsurePzrEnergyStateInitialized(string source)
    {
        bool invalid = float.IsNaN(physicsState.PZRTotalEnthalpy_BTU) ||
                       float.IsInfinity(physicsState.PZRTotalEnthalpy_BTU) ||
                       physicsState.PZRTotalEnthalpy_BTU <= 0f;
        if (!invalid)
            return true;

        SeedPzrEnergyStateFromCurrentMasses($"{source}_AUTO_SEED");
        return !(float.IsNaN(physicsState.PZRTotalEnthalpy_BTU) ||
                 float.IsInfinity(physicsState.PZRTotalEnthalpy_BTU) ||
                 physicsState.PZRTotalEnthalpy_BTU <= 0f);
    }

    void SeedPzrEnergyStateFromCurrentMasses(string source)
    {
        float safePressure_psia = Mathf.Max(20f, pressure);
        float waterMass_lbm = Mathf.Max(0f, physicsState.PZRWaterMass);
        float steamMass_lbm = Mathf.Max(0f, physicsState.PZRSteamMass);

        float hWater_BTU_lb = (steamMass_lbm > 0.01f || !solidPressurizer)
            ? WaterProperties.SaturatedLiquidEnthalpy(safePressure_psia)
            : WaterProperties.WaterEnthalpy(T_pzr, safePressure_psia);
        float hSteam_BTU_lb = WaterProperties.SaturatedSteamEnthalpy(safePressure_psia);

        physicsState.PZRTotalEnthalpy_BTU = waterMass_lbm * hWater_BTU_lb + steamMass_lbm * hSteam_BTU_lb;
        physicsState.PZRClosureConverged = true;
        physicsState.PZRClosureVolumeResidual_ft3 = 0f;
        physicsState.PZRClosureEnergyResidual_BTU = 0f;
        SyncPzrEnergyDiagnosticsFromPhysicsState();

        if (enablePzrBubbleDiagnostics)
        {
            Debug.Log(
                $"[PZR_BUBBLE_DIAG][{pzrBubbleDiagnosticsLabel}] ENERGY_SEED source={source} sim_hr={simTime:F4} " +
                $"pressure_psia={safePressure_psia:F3} m_water_lbm={waterMass_lbm:F3} m_steam_lbm={steamMass_lbm:F3} " +
                $"H_total_BTU={physicsState.PZRTotalEnthalpy_BTU:F3}");
        }
    }

    void SyncPzrEnergyDiagnosticsFromPhysicsState()
    {
        pzrTotalEnthalpy_BTU = physicsState.PZRTotalEnthalpy_BTU;
        float totalPzrMass_lbm = Mathf.Max(1e-3f, physicsState.PZRWaterMass + physicsState.PZRSteamMass);
        pzrSpecificEnthalpy_BTU_lb = pzrTotalEnthalpy_BTU / totalPzrMass_lbm;
        pzrClosureVolumeResidual_ft3 = physicsState.PZRClosureVolumeResidual_ft3;
        pzrClosureEnergyResidual_BTU = physicsState.PZRClosureEnergyResidual_BTU;
        pzrClosureConverged = physicsState.PZRClosureConverged;
        pzrClosureConvergencePct = pzrClosureSolveAttempts > 0
            ? 100f * pzrClosureSolveConverged / pzrClosureSolveAttempts
            : 0f;
    }

    bool RunTwoPhaseBracketOnlyProbe(
        string probeSource,
        out bool bracketFound,
        out float bracketLow_psia,
        out float bracketHigh_psia,
        out string outcomeReason)
    {
        bracketFound = false;
        bracketLow_psia = 0f;
        bracketHigh_psia = 0f;
        outcomeReason = "UNSET";

        if (!EnsurePzrEnergyStateInitialized(probeSource))
        {
            outcomeReason = "INFEASIBLE_ENERGY_FOR_MASS_VOLUME";
            return false;
        }

        float pressureFloor_psia = 20f;
        if (bubblePhase == BubbleFormationPhase.DRAIN ||
            bubblePhase == BubbleFormationPhase.STABILIZE ||
            bubblePhase == BubbleFormationPhase.PRESSURIZE)
        {
            pressureFloor_psia = Mathf.Max(pressureFloor_psia, PlantConstants.BUBBLE_DRAIN_MIN_PRESSURE_PSIA);
        }

        float operatingCeiling_psia = PlantConstants.PZR_BASELINE_PORV_OPEN_PSIG + PlantConstants.PSIG_TO_PSIA;
        operatingCeiling_psia = Mathf.Clamp(
            operatingCeiling_psia,
            pressureFloor_psia + 1f,
            TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA);

        float targetMass_lbm = Mathf.Max(1f, physicsState.PZRWaterMass + physicsState.PZRSteamMass);
        float targetEnthalpy_BTU = Mathf.Max(0f, physicsState.PZRTotalEnthalpy_BTU);
        float pressureGuess_psia = Mathf.Clamp(Mathf.Max(20f, pressure), pressureFloor_psia, operatingCeiling_psia);

        TrySearchTwoPhaseVolumeBracket(
            probeSource,
            targetMass_lbm,
            targetEnthalpy_BTU,
            pressureGuess_psia,
            pressureFloor_psia,
            operatingCeiling_psia,
            TWO_PHASE_CLOSURE_VOLUME_TOLERANCE_FT3,
            out TwoPhaseBracketSearchResult search);

        bracketFound = search.HasBracket;
        bracketLow_psia = search.HasBracket ? search.BracketLow.Pressure_psia : pzrClosureBracketLastLow_psia;
        bracketHigh_psia = search.HasBracket ? search.BracketHigh.Pressure_psia : pzrClosureBracketLastHigh_psia;
        outcomeReason = search.HasBracket ? "BRACKET_FOUND" : search.FailReason;
        return search.HasBracket;
    }

    bool TrySolveTwoPhaseMassEnergyClosure(
        string stateSource,
        float totalMass_lbm,
        float targetEnthalpy_BTU,
        float pressureGuess_psia,
        float pressureFloor_psia,
        float pressureCeiling_psia,
        float volumeTolerance_ft3,
        float energyTolerance_BTU,
        out TwoPhaseClosurePoint solvedPoint,
        out int iterationsUsed,
        out string failReason,
        out string convergencePattern)
    {
        solvedPoint = default;
        iterationsUsed = 0;
        failReason = "UNSET";
        convergencePattern = "UNSET";

        pressureFloor_psia = Mathf.Clamp(
            pressureFloor_psia,
            TWO_PHASE_CLOSURE_HARD_MIN_PRESSURE_PSIA,
            TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA - 1f);
        pressureCeiling_psia = Mathf.Clamp(
            pressureCeiling_psia,
            pressureFloor_psia + 1f,
            TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA);
        pressureGuess_psia = Mathf.Clamp(pressureGuess_psia, pressureFloor_psia, pressureCeiling_psia);

        TrySearchTwoPhaseVolumeBracket(
            stateSource,
            totalMass_lbm,
            targetEnthalpy_BTU,
            pressureGuess_psia,
            pressureFloor_psia,
            pressureCeiling_psia,
            volumeTolerance_ft3,
            out TwoPhaseBracketSearchResult search);

        if (!search.HasBestPoint)
        {
            failReason = string.IsNullOrWhiteSpace(search.FailReason) ? "EVALUATION_FAILED" : search.FailReason;
            convergencePattern = "NO_VALID_POINTS";
            return false;
        }

        float bestAbsVolumeResidual = Mathf.Abs(search.BestPoint.VolumeResidual_ft3);
        if (bestAbsVolumeResidual <= volumeTolerance_ft3 &&
            Mathf.Abs(search.BestPoint.EnergyResidual_BTU) <= energyTolerance_BTU)
        {
            solvedPoint = search.BestPoint;
            failReason = "SCAN_POINT_TOLERANCE";
            convergencePattern = "MONOTONIC_DESCENT";
            return true;
        }

        if (!search.HasBracket)
        {
            solvedPoint = search.BestPoint;
            failReason = string.IsNullOrWhiteSpace(search.FailReason) ? "NO_VOLUME_BRACKET" : search.FailReason;
            convergencePattern = "NO_BRACKET";
            return false;
        }

        TwoPhaseClosurePoint bracketLow = search.BracketLow;
        TwoPhaseClosurePoint bracketHigh = search.BracketHigh;
        TwoPhaseClosurePoint bestPoint = search.BestPoint;

        bool monotonicResidualDescent = true;
        int residualSignFlipCount = 0;
        bool hasResidualHistory = false;
        float prevAbsResidual = 0f;
        float prevResidual = 0f;
        float oscillationBoundResidual = Mathf.Max(
            Mathf.Abs(bracketLow.VolumeResidual_ft3),
            Mathf.Abs(bracketHigh.VolumeResidual_ft3));

        for (int iter = 0; iter < TWO_PHASE_CLOSURE_MAX_ITERATIONS; iter++)
        {
            iterationsUsed = iter + 1;
            float midPressure_psia = 0.5f * (bracketLow.Pressure_psia + bracketHigh.Pressure_psia);
            TwoPhaseEvalStatus midStatus = EvaluateTwoPhaseMassEnergyAtPressure(
                midPressure_psia,
                totalMass_lbm,
                targetEnthalpy_BTU,
                out TwoPhaseClosurePoint midPoint);
            if (midStatus != TwoPhaseEvalStatus.OK)
            {
                failReason = StatusToFailureReason(midStatus);
                solvedPoint = bestPoint;
                convergencePattern = monotonicResidualDescent ? "MONOTONIC_DESCENT" : "NON_COMPLIANT";
                return false;
            }

            float absMidResidual = Mathf.Abs(midPoint.VolumeResidual_ft3);
            if (hasResidualHistory)
            {
                if (absMidResidual > prevAbsResidual + 1e-6f)
                    monotonicResidualDescent = false;

                if (Mathf.Sign(midPoint.VolumeResidual_ft3) != Mathf.Sign(prevResidual))
                    residualSignFlipCount++;
            }
            else
            {
                hasResidualHistory = true;
            }

            prevAbsResidual = absMidResidual;
            prevResidual = midPoint.VolumeResidual_ft3;

            if (absMidResidual < bestAbsVolumeResidual)
            {
                bestAbsVolumeResidual = absMidResidual;
                bestPoint = midPoint;
            }

            if (enablePzrBubbleDiagnostics)
            {
                Debug.Log(
                    $"[PZR_BUBBLE_DIAG][{pzrBubbleDiagnosticsLabel}] ITER source={stateSource} sim_hr={simTime:F4} iter={iter} " +
                    $"P_psia={midPoint.Pressure_psia:F4} q_raw={midPoint.QualityRaw:F6} q={midPoint.QualityClamped:F6} " +
                    $"V_residual_ft3={midPoint.VolumeResidual_ft3:F6} E_residual_BTU={midPoint.EnergyResidual_BTU:F6} " +
                    $"regime={midPoint.Regime} sign_flips={residualSignFlipCount}");
            }

            if (Mathf.Abs(midPoint.VolumeResidual_ft3) <= volumeTolerance_ft3 &&
                Mathf.Abs(midPoint.EnergyResidual_BTU) <= energyTolerance_BTU)
            {
                bool boundedOscillatory =
                    !monotonicResidualDescent &&
                    residualSignFlipCount > 0 &&
                    bestAbsVolumeResidual <= oscillationBoundResidual + 1e-6f;

                if (monotonicResidualDescent)
                    convergencePattern = "MONOTONIC_DESCENT";
                else if (boundedOscillatory)
                    convergencePattern = "BOUNDED_OSCILLATORY";
                else
                {
                    solvedPoint = bestPoint;
                    failReason = "CONVERGENCE_PATTERN_INVALID";
                    convergencePattern = "NON_COMPLIANT";
                    return false;
                }

                solvedPoint = midPoint;
                failReason = "RESIDUAL_WITHIN_TOLERANCE";
                return true;
            }

            if (Mathf.Sign(midPoint.VolumeResidual_ft3) == Mathf.Sign(bracketLow.VolumeResidual_ft3))
                bracketLow = midPoint;
            else
                bracketHigh = midPoint;
        }

        solvedPoint = bestPoint;
        failReason = "MAX_ITERATIONS";
        bool residualWithinTolerance =
            Mathf.Abs(bestPoint.VolumeResidual_ft3) <= volumeTolerance_ft3 &&
            Mathf.Abs(bestPoint.EnergyResidual_BTU) <= energyTolerance_BTU;
        bool boundedOscillatoryAtExit =
            !monotonicResidualDescent &&
            residualSignFlipCount > 0 &&
            bestAbsVolumeResidual <= oscillationBoundResidual + 1e-6f;
        convergencePattern = monotonicResidualDescent
            ? "MONOTONIC_DESCENT"
            : (boundedOscillatoryAtExit ? "BOUNDED_OSCILLATORY" : "NON_COMPLIANT");
        if (residualWithinTolerance && convergencePattern == "NON_COMPLIANT")
        {
            failReason = "CONVERGENCE_PATTERN_INVALID";
            return false;
        }

        return residualWithinTolerance;
    }

    TwoPhaseEvalStatus EvaluateTwoPhaseMassEnergyAtPressure(
        float pressure_psia,
        float totalMass_lbm,
        float targetEnthalpy_BTU,
        out TwoPhaseClosurePoint point)
    {
        point = default;
        if (totalMass_lbm <= 0f ||
            float.IsNaN(totalMass_lbm) ||
            float.IsInfinity(totalMass_lbm) ||
            float.IsNaN(targetEnthalpy_BTU) ||
            float.IsInfinity(targetEnthalpy_BTU))
        {
            return TwoPhaseEvalStatus.INFEASIBLE_ENERGY_FOR_MASS_VOLUME;
        }

        if (pressure_psia < TWO_PHASE_CLOSURE_HARD_MIN_PRESSURE_PSIA ||
            pressure_psia > TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA)
        {
            return TwoPhaseEvalStatus.EOS_OUT_OF_RANGE;
        }

        float hSpecificTarget_BTU_lb = targetEnthalpy_BTU / totalMass_lbm;
        if (!IsFinite(hSpecificTarget_BTU_lb))
            return TwoPhaseEvalStatus.V_EVAL_NAN;

        float hf_BTU_lb = WaterProperties.SaturatedLiquidEnthalpy(pressure_psia);
        float hg_BTU_lb = WaterProperties.SaturatedSteamEnthalpy(pressure_psia);
        float hfg_BTU_lb = hg_BTU_lb - hf_BTU_lb;
        if (!IsFinite(hfg_BTU_lb) || hfg_BTU_lb <= 1e-4f)
            return TwoPhaseEvalStatus.EOS_OUT_OF_RANGE;

        float tSat_F = WaterProperties.SaturationTemperature(pressure_psia);
        if (!IsFinite(tSat_F))
            return TwoPhaseEvalStatus.V_EVAL_NAN;

        float qualityRaw = (hSpecificTarget_BTU_lb - hf_BTU_lb) / hfg_BTU_lb;
        float quality = Mathf.Clamp01(qualityRaw);

        TwoPhaseClosurePoint candidateMix = default;
        candidateMix.Pressure_psia = pressure_psia;
        candidateMix.QualityRaw = qualityRaw;
        candidateMix.QualityClamped = quality;
        candidateMix.SteamMass_lbm = totalMass_lbm * quality;
        candidateMix.WaterMass_lbm = Mathf.Max(0f, totalMass_lbm - candidateMix.SteamMass_lbm);
        candidateMix.Regime = (quality > 0f && quality < 1f) ? "SATURATED_MIX" :
            (quality <= 0f ? "SATURATED_LIQUID_LIMIT" : "SATURATED_VAPOR_LIMIT");

        float rhoSatWater = WaterProperties.WaterDensity(tSat_F, pressure_psia);
        float rhoSatSteam = WaterProperties.SaturatedSteamDensity(pressure_psia);
        if (!IsFinite(rhoSatWater) || !IsFinite(rhoSatSteam) || rhoSatWater <= 0f || rhoSatSteam <= 0f)
            return TwoPhaseEvalStatus.V_EVAL_NAN;

        candidateMix.WaterVolume_ft3 = candidateMix.WaterMass_lbm / rhoSatWater;
        candidateMix.SteamVolume_ft3 = candidateMix.SteamMass_lbm / rhoSatSteam;
        candidateMix.EnthalpyTotal_BTU = candidateMix.WaterMass_lbm * hf_BTU_lb + candidateMix.SteamMass_lbm * hg_BTU_lb;
        candidateMix.EnergyResidual_BTU = candidateMix.EnthalpyTotal_BTU - targetEnthalpy_BTU;
        candidateMix.VolumeResidual_ft3 =
            (candidateMix.WaterVolume_ft3 + candidateMix.SteamVolume_ft3) - PlantConstants.PZR_TOTAL_VOLUME;

        TwoPhaseRegime phaseGuess = DeterminePhaseFromEnthalpy(hSpecificTarget_BTU_lb, pressure_psia);
        bool liquidValid = TryEvaluateSinglePhaseLiquid(
            pressure_psia,
            totalMass_lbm,
            hSpecificTarget_BTU_lb,
            out TwoPhaseClosurePoint liquidCandidate);
        bool steamValid = TryEvaluateSinglePhaseSteam(
            pressure_psia,
            totalMass_lbm,
            hSpecificTarget_BTU_lb,
            out TwoPhaseClosurePoint steamCandidate);

        bool mixValid = IsFinitePoint(candidateMix);
        liquidValid = liquidValid && IsFinitePoint(liquidCandidate);
        steamValid = steamValid && IsFinitePoint(steamCandidate);
        if (!mixValid && !liquidValid && !steamValid)
            return TwoPhaseEvalStatus.INFEASIBLE_ENERGY_FOR_MASS_VOLUME;

        point = SelectEvaluationCandidate(
            phaseGuess,
            mixValid,
            candidateMix,
            liquidValid,
            liquidCandidate,
            steamValid,
            steamCandidate);

        if (!IsFinitePoint(point))
            return TwoPhaseEvalStatus.V_EVAL_NAN;

        return TwoPhaseEvalStatus.OK;
    }

    bool TrySearchTwoPhaseVolumeBracket(
        string stateSource,
        float totalMass_lbm,
        float targetEnthalpy_BTU,
        float pressureGuess_psia,
        float operatingMin_psia,
        float operatingMax_psia,
        float volumeTolerance_ft3,
        out TwoPhaseBracketSearchResult result)
    {
        TwoPhaseBracketSearchResult search = new TwoPhaseBracketSearchResult
        {
            HasBracket = false,
            HasBestPoint = false,
            ValidEvaluations = 0,
            InvalidEvaluations = 0,
            NanEvaluations = 0,
            OutOfRangeEvaluations = 0,
            WindowsTried = 0,
            PositiveResidualSeen = false,
            NegativeResidualSeen = false,
            MinEvaluatedVolume_ft3 = float.MaxValue,
            MaxEvaluatedVolume_ft3 = -float.MaxValue,
            FailReason = "UNSET",
            Trace = string.Empty
        };

        float hardMin_psia = TWO_PHASE_CLOSURE_HARD_MIN_PRESSURE_PSIA;
        float hardMax_psia = TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA;
        operatingMin_psia = Mathf.Clamp(operatingMin_psia, hardMin_psia, hardMax_psia - 1f);
        operatingMax_psia = Mathf.Clamp(operatingMax_psia, operatingMin_psia + 1f, hardMax_psia);
        pressureGuess_psia = Mathf.Clamp(pressureGuess_psia, operatingMin_psia, operatingMax_psia);

        pzrClosureBracketMassTarget_lbm = totalMass_lbm;
        pzrClosureBracketEnthalpyTarget_BTU = targetEnthalpy_BTU;
        pzrClosureBracketVolumeTarget_ft3 = PlantConstants.PZR_TOTAL_VOLUME;
        pzrClosureBracketPressureGuess_psia = pressureGuess_psia;
        pzrClosureBracketOperatingMin_psia = operatingMin_psia;
        pzrClosureBracketOperatingMax_psia = operatingMax_psia;
        pzrClosureBracketHardMin_psia = hardMin_psia;
        pzrClosureBracketHardMax_psia = hardMax_psia;
        pzrClosureBracketLastLow_psia = 0f;
        pzrClosureBracketLastHigh_psia = 0f;
        pzrClosureBracketResidualLow_ft3 = 0f;
        pzrClosureBracketResidualHigh_ft3 = 0f;
        pzrClosureBracketResidualSignLow = 0;
        pzrClosureBracketResidualSignHigh = 0;
        pzrClosureBracketRegimeLow = "UNSET";
        pzrClosureBracketRegimeHigh = "UNSET";
        pzrClosureBracketWindowsTried = 0;
        pzrClosureBracketValidEvaluations = 0;
        pzrClosureBracketInvalidEvaluations = 0;
        pzrClosureBracketNanEvaluations = 0;
        pzrClosureBracketOutOfRangeEvaluations = 0;
        pzrClosureBracketFound = false;
        pzrClosureBracketSearchTrace = string.Empty;

        float hSpecificTarget_BTU_lb = targetEnthalpy_BTU / Mathf.Max(1e-6f, totalMass_lbm);
        if (!IsSpecificEnthalpyInModelRange(hSpecificTarget_BTU_lb))
        {
            search.FailReason = "INFEASIBLE_ENERGY_FOR_MASS_VOLUME";
            pzrClosureBracketSearchTrace = "ENERGY_OUT_OF_MODEL_RANGE";
            result = search;
            return false;
        }

        float bestAbsResidual = float.MaxValue;
        float bestBracketScore = float.MaxValue;
        bool sawNan = false;
        bool sawOutOfRange = false;

        void AccumulateEvaluation(TwoPhaseClosurePoint pt)
        {
            search.ValidEvaluations++;
            float absResidual = Mathf.Abs(pt.VolumeResidual_ft3);
            if (!search.HasBestPoint || absResidual < bestAbsResidual)
            {
                search.HasBestPoint = true;
                bestAbsResidual = absResidual;
                search.BestPoint = pt;
            }

            int sign = ResidualSign(pt.VolumeResidual_ft3);
            if (sign > 0)
                search.PositiveResidualSeen = true;
            else if (sign < 0)
                search.NegativeResidualSeen = true;

            float totalVolume = pt.WaterVolume_ft3 + pt.SteamVolume_ft3;
            search.MinEvaluatedVolume_ft3 = Mathf.Min(search.MinEvaluatedVolume_ft3, totalVolume);
            search.MaxEvaluatedVolume_ft3 = Mathf.Max(search.MaxEvaluatedVolume_ft3, totalVolume);
        }

        bool TryEvaluateProbe(float probePressure_psia, out TwoPhaseClosurePoint point, out TwoPhaseEvalStatus status)
        {
            status = EvaluateTwoPhaseMassEnergyAtPressure(
                probePressure_psia,
                totalMass_lbm,
                targetEnthalpy_BTU,
                out point);
            if (status == TwoPhaseEvalStatus.OK)
            {
                AccumulateEvaluation(point);
                return true;
            }

            search.InvalidEvaluations++;
            if (status == TwoPhaseEvalStatus.V_EVAL_NAN)
            {
                search.NanEvaluations++;
                sawNan = true;
            }
            else if (status == TwoPhaseEvalStatus.EOS_OUT_OF_RANGE)
            {
                search.OutOfRangeEvaluations++;
                sawOutOfRange = true;
            }

            return false;
        }

        string BuildStatusToken(bool ok, TwoPhaseEvalStatus status)
        {
            return ok ? "OK" : StatusToFailureReason(status);
        }

        bool ScanWindow(float low_psia, float high_psia, string bandLabel, int windowOrdinal)
        {
            search.WindowsTried++;
            bool hasPrev = false;
            TwoPhaseClosurePoint prevPoint = default;
            bool windowHasBracket = false;
            TwoPhaseClosurePoint windowBracketLow = default;
            TwoPhaseClosurePoint windowBracketHigh = default;

            bool lowOk = TryEvaluateProbe(low_psia, out TwoPhaseClosurePoint lowPoint, out TwoPhaseEvalStatus lowStatus);
            bool highOk = TryEvaluateProbe(high_psia, out TwoPhaseClosurePoint highPoint, out TwoPhaseEvalStatus highStatus);

            if (lowOk && highOk)
            {
                int lowSign = ResidualSign(lowPoint.VolumeResidual_ft3);
                int highSign = ResidualSign(highPoint.VolumeResidual_ft3);
                if (lowSign != 0 && highSign != 0 && lowSign != highSign)
                {
                    windowHasBracket = true;
                    windowBracketLow = lowPoint;
                    windowBracketHigh = highPoint;
                }
            }

            for (int i = 0; i <= TWO_PHASE_CLOSURE_SCAN_POINTS; i++)
            {
                float alpha = (float)i / TWO_PHASE_CLOSURE_SCAN_POINTS;
                float probePressure_psia = Mathf.Lerp(low_psia, high_psia, alpha);
                if (!TryEvaluateProbe(probePressure_psia, out TwoPhaseClosurePoint point, out _))
                    continue;

                if (hasPrev)
                {
                    int prevSign = ResidualSign(prevPoint.VolumeResidual_ft3);
                    int currSign = ResidualSign(point.VolumeResidual_ft3);
                    if (prevSign != 0 && currSign != 0 && prevSign != currSign)
                    {
                        float score = Mathf.Abs(0.5f * (prevPoint.Pressure_psia + point.Pressure_psia) - pressureGuess_psia);
                        if (!windowHasBracket || score < bestBracketScore)
                        {
                            windowHasBracket = true;
                            windowBracketLow = prevPoint;
                            windowBracketHigh = point;
                            bestBracketScore = score;
                        }
                    }
                }

                prevPoint = point;
                hasPrev = true;
            }

            pzrClosureBracketLastLow_psia = low_psia;
            pzrClosureBracketLastHigh_psia = high_psia;
            pzrClosureBracketResidualLow_ft3 = lowOk ? lowPoint.VolumeResidual_ft3 : float.NaN;
            pzrClosureBracketResidualHigh_ft3 = highOk ? highPoint.VolumeResidual_ft3 : float.NaN;
            pzrClosureBracketResidualSignLow = lowOk ? ResidualSign(lowPoint.VolumeResidual_ft3) : 0;
            pzrClosureBracketResidualSignHigh = highOk ? ResidualSign(highPoint.VolumeResidual_ft3) : 0;
            pzrClosureBracketRegimeLow = lowOk ? SafeString(lowPoint.Regime) : "UNSET";
            pzrClosureBracketRegimeHigh = highOk ? SafeString(highPoint.Regime) : "UNSET";

            string traceLine =
                $"[{bandLabel}:{windowOrdinal}] Pmin={low_psia:F3} Pmax={high_psia:F3} " +
                $"status_low={BuildStatusToken(lowOk, lowStatus)} status_high={BuildStatusToken(highOk, highStatus)} " +
                $"Vres_low={(lowOk ? lowPoint.VolumeResidual_ft3.ToString("F3") : "NaN")} " +
                $"Vres_high={(highOk ? highPoint.VolumeResidual_ft3.ToString("F3") : "NaN")} " +
                $"sign_low={pzrClosureBracketResidualSignLow} sign_high={pzrClosureBracketResidualSignHigh} " +
                $"regime_low={pzrClosureBracketRegimeLow} regime_high={pzrClosureBracketRegimeHigh} source={SafeString(stateSource)}";
            search.Trace = string.IsNullOrEmpty(search.Trace)
                ? traceLine
                : $"{search.Trace} || {traceLine}";

            if (windowHasBracket)
            {
                search.HasBracket = true;
                search.BracketLow = windowBracketLow;
                search.BracketHigh = windowBracketHigh;
                return true;
            }

            return false;
        }

        bool SearchBand(string bandLabel, float bandMin_psia, float bandMax_psia)
        {
            float halfSpan_psia = Mathf.Max(
                TWO_PHASE_BRACKET_INITIAL_HALFSPAN_PSIA,
                0.05f * Mathf.Max(1f, pressureGuess_psia));
            bool fullCoverageAttempted = false;
            for (int window = 1; window <= TWO_PHASE_BRACKET_MAX_WINDOWS_PER_BAND; window++)
            {
                float low_psia = Mathf.Clamp(pressureGuess_psia - halfSpan_psia, bandMin_psia, bandMax_psia);
                float high_psia = Mathf.Clamp(pressureGuess_psia + halfSpan_psia, bandMin_psia, bandMax_psia);
                if (high_psia - low_psia < 1e-4f)
                    break;

                if (ScanWindow(low_psia, high_psia, bandLabel, window))
                    return true;

                if (low_psia <= bandMin_psia + 1e-4f && high_psia >= bandMax_psia - 1e-4f)
                {
                    fullCoverageAttempted = true;
                    break;
                }

                halfSpan_psia *= TWO_PHASE_BRACKET_GROWTH;
            }

            if (!fullCoverageAttempted)
                return ScanWindow(bandMin_psia, bandMax_psia, bandLabel, TWO_PHASE_BRACKET_MAX_WINDOWS_PER_BAND + 1);

            return false;
        }

        bool found = SearchBand("OPERATING", operatingMin_psia, operatingMax_psia);
        if (!found)
            found = SearchBand("HARD", hardMin_psia, hardMax_psia);

        if (!search.HasBestPoint)
        {
            if (sawNan)
                search.FailReason = "V_EVAL_NAN";
            else if (sawOutOfRange)
                search.FailReason = "EOS_OUT_OF_RANGE";
            else
                search.FailReason = "EVALUATION_FAILED";
        }
        else if (!search.HasBracket)
        {
            bool sameSignAcrossRange = !(search.PositiveResidualSeen && search.NegativeResidualSeen);
            bool infeasibleVolumeEnvelope =
                search.MinEvaluatedVolume_ft3 > PlantConstants.PZR_TOTAL_VOLUME + volumeTolerance_ft3 ||
                search.MaxEvaluatedVolume_ft3 < PlantConstants.PZR_TOTAL_VOLUME - volumeTolerance_ft3;

            if (sawNan)
                search.FailReason = "V_EVAL_NAN";
            else if (sawOutOfRange && search.ValidEvaluations == 0)
                search.FailReason = "EOS_OUT_OF_RANGE";
            else if (infeasibleVolumeEnvelope)
                search.FailReason = "INFEASIBLE_ENERGY_FOR_MASS_VOLUME";
            else if (sameSignAcrossRange)
                search.FailReason = "RESIDUAL_SAME_SIGN_FULL_RANGE";
            else
                search.FailReason = "NO_VOLUME_BRACKET";
        }
        else
        {
            search.FailReason = "NONE";
        }

        pzrClosureBracketValidEvaluations = search.ValidEvaluations;
        pzrClosureBracketInvalidEvaluations = search.InvalidEvaluations;
        pzrClosureBracketNanEvaluations = search.NanEvaluations;
        pzrClosureBracketOutOfRangeEvaluations = search.OutOfRangeEvaluations;
        pzrClosureBracketWindowsTried = search.WindowsTried;
        pzrClosureBracketFound = search.HasBracket;
        pzrClosureBracketSearchTrace = search.Trace;

        if (search.HasBracket)
        {
            pzrClosureBracketLastLow_psia = search.BracketLow.Pressure_psia;
            pzrClosureBracketLastHigh_psia = search.BracketHigh.Pressure_psia;
            pzrClosureBracketResidualLow_ft3 = search.BracketLow.VolumeResidual_ft3;
            pzrClosureBracketResidualHigh_ft3 = search.BracketHigh.VolumeResidual_ft3;
            pzrClosureBracketResidualSignLow = ResidualSign(search.BracketLow.VolumeResidual_ft3);
            pzrClosureBracketResidualSignHigh = ResidualSign(search.BracketHigh.VolumeResidual_ft3);
            pzrClosureBracketRegimeLow = SafeString(search.BracketLow.Regime);
            pzrClosureBracketRegimeHigh = SafeString(search.BracketHigh.Regime);
        }

        result = search;
        return search.HasBracket;
    }

    static int ResidualSign(float residual)
    {
        if (float.IsNaN(residual) || float.IsInfinity(residual))
            return 0;
        if (Mathf.Abs(residual) <= 1e-6f)
            return 0;
        return residual > 0f ? 1 : -1;
    }

    static bool IsFinite(float value)
    {
        return !(float.IsNaN(value) || float.IsInfinity(value));
    }

    static bool IsFinitePoint(TwoPhaseClosurePoint point)
    {
        return IsFinite(point.Pressure_psia) &&
               IsFinite(point.WaterMass_lbm) &&
               IsFinite(point.SteamMass_lbm) &&
               IsFinite(point.WaterVolume_ft3) &&
               IsFinite(point.SteamVolume_ft3) &&
               IsFinite(point.VolumeResidual_ft3) &&
               IsFinite(point.EnergyResidual_BTU) &&
               IsFinite(point.EnthalpyTotal_BTU);
    }

    static string SafeString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "UNSET" : value;
    }

    bool IsSpecificEnthalpyInModelRange(float hSpecific_BTU_lb)
    {
        if (!IsFinite(hSpecific_BTU_lb))
            return false;

        float hMin = WaterProperties.WaterEnthalpy(32f, TWO_PHASE_CLOSURE_HARD_MAX_PRESSURE_PSIA);
        float hMax = WaterProperties.SteamEnthalpy(700f, TWO_PHASE_CLOSURE_HARD_MIN_PRESSURE_PSIA);
        return hSpecific_BTU_lb >= hMin - 5f && hSpecific_BTU_lb <= hMax + 5f;
    }

    TwoPhaseRegime DeterminePhaseFromEnthalpy(float hSpecific_BTU_lb, float pressure_psia)
    {
        PhaseState phase = SteamThermodynamics.DeterminePhaseFromEnthalpy(
            hSpecific_BTU_lb,
            pressure_psia);
        switch (phase)
        {
            case PhaseState.SubcooledLiquid:
                return TwoPhaseRegime.SubcooledLiquid;
            case PhaseState.SuperheatedSteam:
                return TwoPhaseRegime.SuperheatedSteam;
            default:
                return TwoPhaseRegime.SaturatedMixture;
        }
    }

    bool TryEvaluateSinglePhaseLiquid(
        float pressure_psia,
        float totalMass_lbm,
        float targetSpecificEnthalpy_BTU_lb,
        out TwoPhaseClosurePoint point)
    {
        point = default;
        float tSat_F = WaterProperties.SaturationTemperature(pressure_psia);
        float tMin_F = 32f;
        float tMax_F = Mathf.Max(tMin_F + 1e-3f, tSat_F - 1e-3f);
        float hMin = WaterProperties.WaterEnthalpy(tMin_F, pressure_psia);
        float hMax = WaterProperties.WaterEnthalpy(tMax_F, pressure_psia);
        if (targetSpecificEnthalpy_BTU_lb < hMin || targetSpecificEnthalpy_BTU_lb > hMax)
            return false;

        float low = tMin_F;
        float high = tMax_F;
        for (int i = 0; i < 32; i++)
        {
            float mid = 0.5f * (low + high);
            float hMid = WaterProperties.WaterEnthalpy(mid, pressure_psia);
            if (hMid < targetSpecificEnthalpy_BTU_lb)
                low = mid;
            else
                high = mid;
        }

        float temp_F = 0.5f * (low + high);
        float hSolved = WaterProperties.WaterEnthalpy(temp_F, pressure_psia);
        float rhoWater = WaterProperties.WaterDensity(temp_F, pressure_psia);
        if (!IsFinite(rhoWater) || rhoWater <= 0f)
            return false;

        point.Pressure_psia = pressure_psia;
        point.QualityRaw = (targetSpecificEnthalpy_BTU_lb - WaterProperties.SaturatedLiquidEnthalpy(pressure_psia)) /
                           Mathf.Max(1e-4f, WaterProperties.LatentHeat(pressure_psia));
        point.QualityClamped = 0f;
        point.WaterMass_lbm = totalMass_lbm;
        point.SteamMass_lbm = 0f;
        point.WaterVolume_ft3 = totalMass_lbm / rhoWater;
        point.SteamVolume_ft3 = 0f;
        point.EnthalpyTotal_BTU = totalMass_lbm * hSolved;
        point.EnergyResidual_BTU = point.EnthalpyTotal_BTU - totalMass_lbm * targetSpecificEnthalpy_BTU_lb;
        point.VolumeResidual_ft3 = point.WaterVolume_ft3 - PlantConstants.PZR_TOTAL_VOLUME;
        point.Regime = "SUBCOOLED_LIQUID";
        return true;
    }

    bool TryEvaluateSinglePhaseSteam(
        float pressure_psia,
        float totalMass_lbm,
        float targetSpecificEnthalpy_BTU_lb,
        out TwoPhaseClosurePoint point)
    {
        point = default;
        float tSat_F = WaterProperties.SaturationTemperature(pressure_psia);
        float tMin_F = tSat_F;
        float tMax_F = 700f;
        float hMin = WaterProperties.SteamEnthalpy(tMin_F, pressure_psia);
        float hMax = WaterProperties.SteamEnthalpy(tMax_F, pressure_psia);
        if (targetSpecificEnthalpy_BTU_lb < hMin || targetSpecificEnthalpy_BTU_lb > hMax)
            return false;

        float low = tMin_F;
        float high = tMax_F;
        for (int i = 0; i < 32; i++)
        {
            float mid = 0.5f * (low + high);
            float hMid = WaterProperties.SteamEnthalpy(mid, pressure_psia);
            if (hMid < targetSpecificEnthalpy_BTU_lb)
                low = mid;
            else
                high = mid;
        }

        float temp_F = 0.5f * (low + high);
        float hSolved = WaterProperties.SteamEnthalpy(temp_F, pressure_psia);
        float rhoSteam = WaterProperties.SteamDensity(temp_F, pressure_psia);
        if (!IsFinite(rhoSteam) || rhoSteam <= 0f)
            return false;

        point.Pressure_psia = pressure_psia;
        point.QualityRaw = (targetSpecificEnthalpy_BTU_lb - WaterProperties.SaturatedLiquidEnthalpy(pressure_psia)) /
                           Mathf.Max(1e-4f, WaterProperties.LatentHeat(pressure_psia));
        point.QualityClamped = 1f;
        point.WaterMass_lbm = 0f;
        point.SteamMass_lbm = totalMass_lbm;
        point.WaterVolume_ft3 = 0f;
        point.SteamVolume_ft3 = totalMass_lbm / rhoSteam;
        point.EnthalpyTotal_BTU = totalMass_lbm * hSolved;
        point.EnergyResidual_BTU = point.EnthalpyTotal_BTU - totalMass_lbm * targetSpecificEnthalpy_BTU_lb;
        point.VolumeResidual_ft3 = point.SteamVolume_ft3 - PlantConstants.PZR_TOTAL_VOLUME;
        point.Regime = "SUPERHEATED_STEAM";
        return true;
    }

    TwoPhaseClosurePoint SelectEvaluationCandidate(
        TwoPhaseRegime phaseGuess,
        bool mixValid,
        TwoPhaseClosurePoint mixCandidate,
        bool liquidValid,
        TwoPhaseClosurePoint liquidCandidate,
        bool steamValid,
        TwoPhaseClosurePoint steamCandidate)
    {
        if (phaseGuess == TwoPhaseRegime.SaturatedMixture && mixValid)
            return mixCandidate;
        if (phaseGuess == TwoPhaseRegime.SubcooledLiquid && liquidValid)
            return liquidCandidate;
        if (phaseGuess == TwoPhaseRegime.SuperheatedSteam && steamValid)
            return steamCandidate;

        TwoPhaseClosurePoint best = default;
        float bestScore = float.MaxValue;

        void Consider(bool valid, TwoPhaseClosurePoint candidate)
        {
            if (!valid)
                return;
            float score = Mathf.Abs(candidate.EnergyResidual_BTU);
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        Consider(mixValid, mixCandidate);
        Consider(liquidValid, liquidCandidate);
        Consider(steamValid, steamCandidate);
        return best;
    }

    static string StatusToFailureReason(TwoPhaseEvalStatus status)
    {
        switch (status)
        {
            case TwoPhaseEvalStatus.V_EVAL_NAN:
                return "V_EVAL_NAN";
            case TwoPhaseEvalStatus.EOS_OUT_OF_RANGE:
                return "EOS_OUT_OF_RANGE";
            case TwoPhaseEvalStatus.INFEASIBLE_ENERGY_FOR_MASS_VOLUME:
                return "INFEASIBLE_ENERGY_FOR_MASS_VOLUME";
            default:
                return "EVALUATION_FAILED";
        }
    }
}
