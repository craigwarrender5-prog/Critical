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

        // ============================================================
        // v5.4.1 Stage 0: Snapshot masses BEFORE any changes for forensic audit
        // ============================================================
        float pzrWaterMass_before = physicsState.PZRWaterMass;
        float pzrSteamMass_before = physicsState.PZRSteamMass;
        float pzrTotalMass_before = pzrWaterMass_before + pzrSteamMass_before;
        float rcsMass_before = physicsState.RCSWaterMass;
        float systemMass_before = rcsMass_before + pzrTotalMass_before;

        // Water properties at PZR saturation conditions
        float rhoWater = WaterProperties.WaterDensity(T_pzr, pressure);
        float rhoSteam = WaterProperties.SaturatedSteamDensity(pressure);
        float h_fg = WaterProperties.LatentHeat(pressure);  // BTU/lb

        // ============================================================
        // MECHANISM 1 (PRIMARY): Thermodynamic steam displacement
        // Heater power converts liquid water to steam at T_sat.
        // v5.4.1 Stage 1: Mass-based transfer — steam generated from
        // water is a mass-conserving phase change, NOT volume recalc.
        // dm_steam_gen = Q_heater / h_fg (lbm created from water)
        // dm_water_loss = dm_steam_gen (same mass, different phase)
        //
        // v0.3.2.0 CS-0043 FIX: Net heater power for steam generation.
        // Heater energy available for boiling = gross heater power minus
        // PZR conduction loss (surge line) and PZR insulation loss.
        // These losses are computed by IsolatedHeatingStep and stored
        // as engine fields. This is the SOLE consumer of heater energy
        // during two-phase — IsolatedHeatingStep bypasses PZR dT.
        // ============================================================
        float grossHeaterPower_MW = pzrHeaterPower;
        float netHeaterPower_MW = grossHeaterPower_MW - pzrConductionLoss_MW - pzrInsulationLoss_MW;
        if (netHeaterPower_MW < 0f) netHeaterPower_MW = 0f;
        float heaterPower_BTU_sec = netHeaterPower_MW * PlantConstants.MW_TO_BTU_SEC;

        float steamGenRate_lb_sec = 0f;
        if (h_fg > 0f)
            steamGenRate_lb_sec = heaterPower_BTU_sec / h_fg;

        float dm_steamGen_lbm = steamGenRate_lb_sec * dt_sec_drain;

        // ============================================================
        // MECHANISM 2 (SECONDARY): CVCS trim
        // v0.2.0: Charging starts at 0 gpm. CCP starts at level < 80%.
        // v5.4.1 Stage 1: CVCS drain is mass LEAVING the PZR into RCS.
        // dm_out_of_PZR == dm_into_RCS in the same timestep.
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

        // v5.4.1 Stage 1: Compute CVCS drain as MASS transfer, not volume
        float dm_cvcsDrain_lbm = netOutflow_gpm * PlantConstants.GPM_TO_FT3_SEC * dt_sec_drain * rhoWater;

        // ============================================================
        // v5.4.1 Stage 1: MASS-BASED TRANSFER SEMANTICS
        // Conservation law: total PZR mass changes only by CVCS net outflow.
        // Phase change (water→steam) is internal to PZR, conserves mass.
        //
        // Step 1: Phase change — transfer dm_steamGen from water to steam
        //         (PZR total mass unchanged)
        // Step 2: CVCS drain — remove dm_cvcsDrain from PZR water,
        //         add same mass to RCS (system total unchanged)
        // Step 3: Derive volumes from masses (not the other way around)
        // ============================================================

        // Clamp steam generation to available water mass
        float availableWaterForSteam = physicsState.PZRWaterMass - dm_cvcsDrain_lbm;
        float dm_steamActual = Mathf.Min(dm_steamGen_lbm, Mathf.Max(0f, availableWaterForSteam));

        // Step 1: Phase change (water → steam), mass-conserving within PZR
        physicsState.PZRWaterMass -= dm_steamActual;
        physicsState.PZRSteamMass += dm_steamActual;

        // Step 2: CVCS drain — water leaves PZR, enters RCS (mass-conserving system-wide)
        float dm_cvcsActual = Mathf.Min(dm_cvcsDrain_lbm, Mathf.Max(0f, physicsState.PZRWaterMass));
        physicsState.PZRWaterMass -= dm_cvcsActual;
        physicsState.RCSWaterMass += dm_cvcsActual;

        // v5.4.1 Stage 2: Feed CVCS drain to VCT mass conservation tracking.
        // UpdateRCSInventory() is now skipped during DRAIN (double-count guard),
        // so we must accumulate VCT tracking here to keep the audit correct.
        if (rhoWater > 0.1f)
        {
            float rcsChange_gal_drain = (dm_cvcsActual / rhoWater) * PlantConstants.FT3_TO_GAL;
            VCTPhysics.AccumulateRCSChange(ref vctState, rcsChange_gal_drain);
        }

        // Step 3: Derive volumes from masses (canonical direction: mass → volume)
        float targetWaterVol = PlantConstants.PZR_TOTAL_VOLUME * PlantConstants.PZR_LEVEL_AFTER_BUBBLE / 100f;

        if (rhoWater > 0.1f)
            physicsState.PZRWaterVolume = Mathf.Max(targetWaterVol, physicsState.PZRWaterMass / rhoWater);
        physicsState.PZRSteamVolume = PlantConstants.PZR_TOTAL_VOLUME - physicsState.PZRWaterVolume;

        // v5.4.2.0 FF-05 Fix #3: Do NOT overwrite steam mass from V×ρ.
        // Steam mass is set by the mass-conserving phase change at Step 1 (line 392).
        // The canonical direction is mass → volume, not volume → mass.
        // The previous V×ρ reconciliation here destroyed conservation and caused
        // the ~3,755 lbm DRAIN spike flagged by the forensic audit.

        // Update display variables
        pzrWaterVolume = physicsState.PZRWaterVolume;
        pzrSteamVolume = physicsState.PZRSteamVolume;
        pzrLevel = physicsState.PZRLevel;

        // Update CVCS display flows for this phase
        letdownFlow = PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM;
        chargingFlow = currentCharging_gpm;

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

        // Flag if mass created/destroyed beyond numerical tolerance (0.1 lbm)
        bool massViolation = Mathf.Abs(dm_system) > 0.1f;

        // Periodic forensic logging (every 5 sim-minutes during DRAIN)
        if (phaseElapsed < dt || (simTime % (5f / 60f) < dt))
        {
            Debug.Log($"[T+{simTime:F2}hr] DRAIN AUDIT: Level={pzrLevel:F1}%, " +
                $"dm_steam={dm_steamActual:F1}lbm, dm_cvcs={dm_cvcsActual:F1}lbm, " +
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
        if (levelReached)
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

            bubblePhase = BubbleFormationPhase.STABILIZE;
            bubblePhaseStartTime = simTime;

            // Initialize CVCS Controller for two-phase operations
            float initialSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
            cvcsControllerState = CVCSController.Initialize(
                pzrLevel, initialSetpoint,
                PlantConstants.LETDOWN_NORMAL_GPM,
                PlantConstants.LETDOWN_NORMAL_GPM,
                0f);  // No RCPs yet

            LogEvent(EventSeverity.ACTION, $"PZR drain complete at {pzrLevel:F1}% - stabilizing CVCS");
            LogEvent(EventSeverity.INFO, $"CCP running: {(ccpStarted ? "YES" : "NO")}, Aux spray: {(auxSprayTestPassed ? "PASSED" : "N/A")}");
            LogEvent(EventSeverity.INFO, $"DRAIN final mass audit: Δm_sys={dm_system:F1}lbm, PZR_total={pzrTotalMass_after:F0}lbm");
            LogEvent(EventSeverity.INFO, $"DRAIN exit pressure rate: {pressureRate:F1} psi/hr ({(pressureStable ? "STABLE" : "ADVISORY")})");
            Debug.Log($"[T+{simTime:F2}hr] DRAIN complete. Level={pzrLevel:F1}%, Steam={pzrSteamVolume:F0}ft³, dP/dt={pressureRate:F1} psi/hr");
        }
        else
        {
            float drainProgress = (bubbleDrainStartLevel - pzrLevel) / (bubbleDrainStartLevel - PlantConstants.PZR_LEVEL_AFTER_BUBBLE) * 100f;
            heatupPhaseDesc = $"BUBBLE FORMATION - DRAINING ({drainProgress:F0}%)";
            statusMessage = $"PZR DRAIN: {pzrLevel:F1}% → {PlantConstants.PZR_LEVEL_AFTER_BUBBLE:F0}% | LD={PlantConstants.BUBBLE_DRAIN_LETDOWN_GPM:F0} CHG={currentCharging_gpm:F0} gpm{(ccpStarted ? " (CCP)" : "")}";
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
}
