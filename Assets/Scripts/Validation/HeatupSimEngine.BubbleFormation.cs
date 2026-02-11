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

            // PZR is still 100% water — only a thin steam film at heater surfaces
            float rhoWater = WaterProperties.WaterDensity(T_pzr, pressure);
            physicsState.PZRWaterMass = physicsState.PZRWaterVolume * rhoWater;
            physicsState.PZRSteamVolume = 0f;
            physicsState.PZRSteamMass = 0f;

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
                UpdatePressurizePhase();
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
                pressure -= sprayEffect_psi_sec * (dt * 3600f);
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

            // v0.2.0: Transition heater mode to auto pressure-rate feedback
            currentHeaterMode = HeaterMode.BUBBLE_FORMATION_AUTO;

            LogEvent(EventSeverity.ACTION, $"Bubble verified - beginning thermodynamic drain from {pzrLevel:F0}% to {PlantConstants.PZR_LEVEL_AFTER_BUBBLE:F0}%");
            LogEvent(EventSeverity.INFO, $"Letdown={PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM:F0} gpm, Charging=0 gpm (CCP not started), Heaters=AUTO");
            Debug.Log($"[T+{simTime:F2}hr] Bubble VERIFIED, entering DRAIN phase (thermodynamic)");
            Debug.Log($"  Steam displacement primary, CVCS trim secondary");
            Debug.Log($"  Letdown={PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM:F0} gpm, Charging=0 (CCP at <{PlantConstants.CCP_START_LEVEL:F0}%)");
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

        // Water properties at PZR saturation conditions
        float rhoWater = WaterProperties.WaterDensity(T_pzr, pressure);
        float rhoSteam = WaterProperties.SaturatedSteamDensity(pressure);
        float h_fg = WaterProperties.LatentHeat(pressure);  // BTU/lb

        // ============================================================
        // MECHANISM 1 (PRIMARY): Thermodynamic steam displacement
        // Heater power converts liquid water to steam at T_sat.
        // ============================================================
        float heaterPower_BTU_sec = pzrHeaterPower * PlantConstants.MW_TO_BTU_SEC;

        float steamGenRate_lb_sec = 0f;
        if (h_fg > 0f)
            steamGenRate_lb_sec = heaterPower_BTU_sec / h_fg;

        // Volume displacement: steam occupies more volume than the water it came from
        float volumeDisplacement_ft3_sec = 0f;
        if (rhoSteam > 0.01f && rhoWater > 0.1f)
            volumeDisplacement_ft3_sec = steamGenRate_lb_sec * (1f / rhoSteam - 1f / rhoWater);

        float steamDisplacement_ft3 = volumeDisplacement_ft3_sec * dt_sec_drain;

        // ============================================================
        // MECHANISM 2 (SECONDARY): CVCS trim
        // v0.2.0: Charging starts at 0 gpm. CCP starts at level < 80%.
        // ============================================================
        float currentCharging_gpm = PlantConstants.BUBBLE_DRAIN_CHARGING_INITIAL_GPM;

        // CCP start check: level drops below threshold
        if (!ccpStarted && pzrLevel < PlantConstants.CCP_START_LEVEL)
        {
            ccpStarted = true;
            ccpStartTime = simTime;
            ccpStartLevel = pzrLevel;
            LogEvent(EventSeverity.ACTION, $"CCP STARTED at PZR level {pzrLevel:F1}% (< {PlantConstants.CCP_START_LEVEL:F0}%)");
            LogEvent(EventSeverity.INFO, $"Charging flow: 0 -> {PlantConstants.BUBBLE_DRAIN_CHARGING_CCP_GPM:F0} gpm, Net outflow: {PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM:F0} -> {PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM - PlantConstants.BUBBLE_DRAIN_CHARGING_CCP_GPM:F0} gpm");
            Debug.Log($"[T+{simTime:F2}hr] === CCP STARTED === Level={pzrLevel:F1}%, Charging=0->{PlantConstants.BUBBLE_DRAIN_CHARGING_CCP_GPM:F0} gpm");
        }

        if (ccpStarted)
            currentCharging_gpm = PlantConstants.BUBBLE_DRAIN_CHARGING_CCP_GPM;

        float netOutflow_gpm = PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM - currentCharging_gpm;
        float cvcsDrain_ft3 = netOutflow_gpm * PlantConstants.GPM_TO_FT3_SEC * dt_sec_drain;

        // ============================================================
        // COMBINED: Total water volume reduction
        // ============================================================
        float totalDrain_ft3 = steamDisplacement_ft3 + cvcsDrain_ft3;
        float targetWaterVol = PlantConstants.PZR_TOTAL_VOLUME * PlantConstants.PZR_LEVEL_AFTER_BUBBLE / 100f;

        physicsState.PZRWaterVolume = Mathf.Max(
            targetWaterVol,
            physicsState.PZRWaterVolume - totalDrain_ft3);

        // Steam fills the void created by drainage
        physicsState.PZRSteamVolume = PlantConstants.PZR_TOTAL_VOLUME - physicsState.PZRWaterVolume;

        // Update masses
        physicsState.PZRWaterMass = physicsState.PZRWaterVolume * rhoWater;
        physicsState.PZRSteamMass = physicsState.PZRSteamVolume * rhoSteam;

        // Update display variables
        pzrWaterVolume = physicsState.PZRWaterVolume;
        pzrSteamVolume = physicsState.PZRSteamVolume;
        pzrLevel = physicsState.PZRLevel;

        // Update CVCS display flows for this phase
        letdownFlow = PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM;
        chargingFlow = currentCharging_gpm;

        // Recalculate total system mass
        totalSystemMass = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;

        // Calculate latent heat demand for logging
        float latentDemand_kW = 0f;
        if (PlantConstants.KW_TO_BTU_SEC > 0f)
            latentDemand_kW = steamGenRate_lb_sec * h_fg / PlantConstants.KW_TO_BTU_SEC;

        // Check if drain is complete (level reached target)
        if (pzrLevel <= PlantConstants.PZR_LEVEL_AFTER_BUBBLE + 0.5f)
        {
            bubblePhase = BubbleFormationPhase.STABILIZE;
            bubblePhaseStartTime = simTime;

            // Initialize CVCS Controller for two-phase operations
            // v0.4.0 Issue #2 fix: Was GetPZRLevelProgram (at-power only, clamps to 25% below 557°F).
            // Unified function uses heatup program below 557°F, at-power program above.
            float initialSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
            cvcsControllerState = CVCSController.Initialize(
                pzrLevel, initialSetpoint,
                PlantConstants.LETDOWN_NORMAL_GPM,
                PlantConstants.LETDOWN_NORMAL_GPM,
                0f);  // No RCPs yet

            LogEvent(EventSeverity.ACTION, $"PZR drain complete at {pzrLevel:F1}% - stabilizing CVCS");
            LogEvent(EventSeverity.INFO, $"CCP running: {(ccpStarted ? "YES" : "NO")}, Aux spray: {(auxSprayTestPassed ? "PASSED" : "N/A")}");
            Debug.Log($"[T+{simTime:F2}hr] DRAIN complete. Level={pzrLevel:F1}%, Steam={pzrSteamVolume:F0}ft³");
        }
        else
        {
            float drainProgress = (bubbleDrainStartLevel - pzrLevel) / (bubbleDrainStartLevel - PlantConstants.PZR_LEVEL_AFTER_BUBBLE) * 100f;
            heatupPhaseDesc = $"BUBBLE FORMATION - DRAINING ({drainProgress:F0}%)";
            statusMessage = $"PZR DRAIN: {pzrLevel:F1}% → {PlantConstants.PZR_LEVEL_AFTER_BUBBLE:F0}% | LD={PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM:F0} CHG={currentCharging_gpm:F0} gpm{(ccpStarted ? " (CCP)" : "")}";
        }

        // Periodic logging
        if (phaseElapsed < dt || (simTime % (5f / 60f) < dt))
        {
            Debug.Log($"[T+{simTime:F2}hr] DRAIN: Level={pzrLevel:F1}%, SteamVol={pzrSteamVolume:F0}ft³, SteamDisp={steamDisplacement_ft3 * PlantConstants.FT3_TO_GAL:F1}gal, CVCS={cvcsDrain_ft3 * PlantConstants.FT3_TO_GAL:F1}gal, CCP={(ccpStarted ? "ON" : "OFF")}");
        }
    }

    // ========================================================================
    // PRESSURIZE PHASE — Heaters raise pressure for RCP NPSH
    // Per NRC HRTD 19.2.2
    // ========================================================================

    void UpdatePressurizePhase()
    {
        float pressure_psig = pressure - 14.7f;
        float minRcpP_psig = PlantConstants.MIN_RCP_PRESSURE_PSIG;

        if (pressure_psig >= minRcpP_psig)
        {
            // Bubble formation complete!
            bubblePhase = BubbleFormationPhase.COMPLETE;
            bubbleFormed = true;
            bubbleFormationTime = simTime;

            // Pre-seed PI integral for stable RCP start transition (Bug #4 Fix)
            CVCSController.PreSeedForRCPStart(ref cvcsControllerState, pzrLevel, T_avg, 0);

            LogEvent(EventSeverity.ALERT, $"BUBBLE FORMATION COMPLETE at P={pressure_psig:F0}psig - READY FOR RCPs");
            LogEvent(EventSeverity.INFO, $"Total bubble formation time: {(simTime - bubbleFormationTemp):F1} min");
            Debug.Log($"[T+{simTime:F2}hr] === BUBBLE FORMATION COMPLETE ===");
            Debug.Log($"  P={pressure:F0}psia ({pressure_psig:F0}psig), Level={pzrLevel:F1}%");
            Debug.Log($"  Steam space: {pzrSteamVolume:F0}ft³, RCP start permitted");
        }
        else
        {
            float pProgress = pressure_psig / minRcpP_psig * 100f;
            heatupPhaseDesc = "BUBBLE FORMATION - PRESSURIZING";
            statusMessage = $"PRESSURIZING: {pressure_psig:F0}/{minRcpP_psig:F0} psig ({pProgress:F0}%) | Heaters ON";
        }
    }
}
