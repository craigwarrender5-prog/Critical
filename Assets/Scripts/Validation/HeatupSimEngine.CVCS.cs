// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (CVCS Partial)
// HeatupSimEngine.CVCS.cs - CVCS Flow Control, RCS Inventory, VCT Update
// ============================================================================
//
// PURPOSE:
//   All CVCS (Chemical & Volume Control System) flow management, heater
//   control dispatch, RCS inventory mass tracking, and VCT physics update.
//   Consolidates the post-physics-step flow control logic.
//
// PHYSICS:
//   CVCS mass balance: dm_rcs/dt = (charging - letdown) × ρ × conversion
//   PI level controller via CVCSController module
//   VCT mass conservation tracking via VCTPhysics module
//   Heater control via CVCSController multi-mode controller
//
// SOURCES:
//   - NRC HRTD 4.1 — CVCS flow balance, purification
//   - NRC HRTD 6.1 — Pressurizer heater control
//   - NRC HRTD 10.2 — Pressure control setpoints
//   - NRC HRTD 10.3 — Letdown isolation interlock
//   - NRC HRTD 19.0 — RHR letdown path, letdown path selection
//   - NRC IN 93-84 — RCP seal injection requirements
//
// ARCHITECTURE:
//   Partial class of HeatupSimEngine. This file owns:
//     - UpdateCVCSFlows() — CVCS flow control, PI controller, heater dispatch
//     - UpdateRCSInventory() — Mass conservation for two-phase operations
//     - UpdateVCT() — VCT physics update and mass conservation check
//     - UpdateLetdownPath() — Letdown path state for display
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupSimEngine
{
    // ========================================================================
    // CVCS FLOW CONTROL — Per NRC HRTD 4.1, 10.3, 19.0
    // Called each timestep after physics and bubble formation updates.
    // ========================================================================

    /// <summary>
    /// Update all CVCS flows, heater control, and seal flows.
    /// Dispatches to appropriate controller based on plant state.
    /// </summary>
    /// <param name="dt">Timestep in hours</param>
    /// <param name="bubbleDrainActive">True if DRAIN phase is active</param>
    void UpdateCVCSFlows(float dt, bool bubbleDrainActive)
    {
        // SEAL FLOWS — Owned by CVCSController module
        // Per NRC IN 93-84: Each RCP requires seal injection from charging
        var sealFlows = CVCSController.CalculateSealFlows(rcpCount);
        float sealInjection = sealFlows.SealInjection;
        float sealReturnToVCT = sealFlows.SealReturnToVCT;

        // LOW-LEVEL INTERLOCK — Owned by CVCSController (with hysteresis)
        // Per NRC HRTD 10.3: PZR level < 17% isolates letdown and trips heaters
        bool letdownIsolated = false;

        // ================================================================
        // v4.4.0: ORIFICE LINEUP MANAGEMENT
        // Update orifice lineup based on PZR level error before flow calc.
        // Per NRC HRTD 4.1: Operator adjusts orifices during heatup.
        // ================================================================
        UpdateOrificeLineup();

        // ================================================================
        // LETDOWN FLOW (Gap C3 + C4)
        // During solid ops, SolidPlantPressure module controls letdown/charging
        // ================================================================
        if (solidPressurizer || bubblePreDrainPhase)
        {
            // Flows already set by SolidPlantPressure.Update() — do not override
            // v1.3.1.0: Also applies during DETECTION/VERIFICATION
        }
        else if (bubbleDrainActive)
        {
            // During DRAIN phase: flows set by bubble formation procedure
            // v0.2.0: Flows set in UpdateDrainPhase() — do not override
        }
        else
        {
            // v4.4.0: Use orifice lineup model for letdown flow calculation
            letdownFlow = PlantConstants.CalculateTotalLetdownFlow(
                T_rcs, pressure, numOrificesOpen: 1,
                num75Open: orifice75Count, open45: orifice45Open);
        }

        // ================================================================
        // PZR LEVEL PROGRAM (Gap C2)
        // ================================================================
        float pzrLevelSetpoint;
        if (solidPressurizer || bubblePreDrainPhase)
        {
            pzrLevelSetpoint = 100f;  // v1.3.1.0: pre-drain phases are still at 100%
        }
        else
        {
            // v0.4.0 Issue #2 fix: Was GetPZRLevelProgram (at-power only, clamps to 25% below 557°F).
            // Unified function uses heatup program below 557°F, at-power program above.
            pzrLevelSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
        }

        // ================================================================
        // PI CHARGING FLOW CONTROLLER (Gap C1)
        // Delegated to CVCSController physics module.
        // ================================================================
        if (solidPressurizer || bubblePreDrainPhase)
        {
            // Flows already set by SolidPlantPressure.Update() — do not override
        }
        else if (bubbleDrainActive)
        {
            // During DRAIN phase: flows are fixed, PI controller not active
        }
        else
        {
            // v4.4.0: Update CVCS controller with orifice lineup — calculates
            // charging flow via PI control and letdown based on lineup.
            CVCSController.Update(
                ref cvcsControllerState,
                pzrLevel,
                T_avg,
                pressure,
                rcpCount,
                dt,
                num75Open: orifice75Count,
                open45: orifice45Open);

            // Read results from physics module
            chargingFlow = cvcsControllerState.ChargingFlow;
            letdownFlow = cvcsControllerState.LetdownFlow;
            letdownIsolated = cvcsControllerState.LetdownIsolated;

            // Sync legacy display variable from controller state
            cvcsIntegralError = cvcsControllerState.IntegralError;
        }

        // HEATER CONTROL: Now runs in Section 1B of StepSimulation()
        // (before physics) so throttled power reaches physics calculations.
        // See Issue #1 fix — v0.4.0.

        chargingActive = chargingFlow > 0.1f;
        letdownActive = letdownFlow > 0.1f;
        sealInjectionOK = (rcpCount == 0) || (sealInjection >= rcpCount * 7f);

        // ================================================================
        // RCS INVENTORY UPDATE — Two-Phase Operations (Bug #2 Fix)
        // ================================================================
        UpdateRCSInventory(dt, bubbleDrainActive);

        // ================================================================
        // VCT PHYSICS UPDATE — Per NRC HRTD Section 4.1
        // ================================================================
        UpdateVCT(dt, bubbleDrainActive, sealReturnToVCT);

        // ================================================================
        // LETDOWN PATH STATE — For display
        // ================================================================
        UpdateLetdownPath(pzrLevelSetpoint, letdownIsolated, sealInjection);
    }

    // ========================================================================
    // RCS INVENTORY UPDATE — Mass conservation tracking
    // Per NRC HRTD 4.1: CVCS net flow changes RCS water mass.
    // ========================================================================

    void UpdateRCSInventory(float dt, bool bubbleDrainActive)
    {
        // IP-0016 PBOC: This method must never apply boundary mass directly.
        // Boundary event ownership is centralized in StepSimulation() via
        // ApplyPrimaryBoundaryFlowPBOC(), then this method performs only
        // sync/guard behavior.
        if (!regime3CVCSPreApplied)
        {
            throw new System.InvalidOperationException(
                $"PBOC contract violated: UpdateRCSInventory reached without pre-applied " +
                $"boundary event at T+{simTime:F4}hr (solid={solidPressurizer}, preDrain={bubblePreDrainPhase}, drain={bubbleDrainActive})");
        }

        regime3CVCSPreApplied = false;  // reset for next tick
        rcsWaterMass = physicsState.RCSWaterMass;
    }

    // ========================================================================
    // VCT PHYSICS UPDATE — Per NRC HRTD Section 4.1
    // v0.6.0: Now coordinates BRS divert inflow, processing, and return.
    // ========================================================================

    void UpdateVCT(float dt, bool bubbleDrainActive, float sealReturnToVCT)
    {
        float dt_sec = dt * 3600f;
        float dt_min = dt_sec / 60f;
        float rhoAux = WaterProperties.WaterDensity(100f, 14.7f);
        float vctMassBefore_lbm = (vctState.Volume_gal / PlantConstants.FT3_TO_GAL) * rhoAux;
        float brsTotalGalBefore = brsState.HoldupVolume_gal + brsState.DistillateAvailable_gal
            + brsState.ConcentrateAvailable_gal;
        float brsMassBefore_lbm = (brsTotalGalBefore / PlantConstants.FT3_TO_GAL) * rhoAux;

        // v0.2.0: During DRAIN, VCT sees actual flows (letdown returns to VCT)
        float vctLetdownFlow = letdownFlow;

        // v0.6.0: Pass BRS distillate availability so VCT knows if BRS is a
        // viable makeup source (closed-loop priority over RMS/RWST)
        VCTPhysics.Update(ref vctState, dt_sec, vctLetdownFlow, chargingFlow,
            sealReturnToVCT, rcpCount, brsState.DistillateAvailable_gal);

        // ==============================================================
        // v0.6.0: BRS COORDINATION — Divert → BRS → Processing → Return
        // Per NRC HRTD 4.1 Section 4.1.2.6: LCV-112A diverts excess
        // letdown to BRS recycle holdup tanks. Evaporator processes
        // holdup into distillate (≈ 0 ppm) and concentrate (≈ 7000 ppm).
        // Distillate is first-priority VCT makeup source.
        // ==============================================================

        // Step 1: Transfer VCT divert flow to BRS holdup tanks
        if (vctState.DivertActive && vctState.DivertFlow_gpm > 0f)
        {
            float divertVolume_gal = vctState.DivertFlow_gpm * dt_min;
            BRSPhysics.ReceiveDivert(ref brsState, divertVolume_gal,
                vctState.BoronConcentration_ppm);
            brsState.InFlow_gpm = vctState.DivertFlow_gpm;
        }
        else
        {
            brsState.InFlow_gpm = 0f;
        }

        // Step 2: Advance evaporator batch processing
        BRSPhysics.UpdateProcessing(ref brsState, dt);

        // Step 3: If VCT auto-makeup is active and sourced from BRS,
        // withdraw distillate from BRS monitor tanks
        if (vctState.AutoMakeupActive && vctState.MakeupFromBRS)
        {
            float makeupVolume_gal = vctState.MakeupFlow_gpm * dt_min;
            float actualWithdrawn = BRSPhysics.WithdrawDistillate(
                ref brsState, makeupVolume_gal);
            brsState.ReturnFlow_gpm = actualWithdrawn / Mathf.Max(dt_min, 0.001f);
        }
        else
        {
            brsState.ReturnFlow_gpm = 0f;
        }

        // True plant boundary crossings for system-wide conservation:
        //  - External IN  : makeup not sourced from BRS (RMS/RWST)
        //  - External OUT : CBO bleedoff when RCPs are running
        float externalInStep_gal = vctState.MakeupFromBRS ? 0f : (vctState.MakeupFlow_gpm * dt_min);
        float externalOutStep_gal = (rcpCount > 0 ? PlantConstants.CBO_LOSS_GPM : 0f) * dt_min;
        float externalNetStep_gal = externalInStep_gal - externalOutStep_gal;
        plantExternalIn_gal += externalInStep_gal;
        plantExternalOut_gal += externalOutStep_gal;
        plantExternalNet_gal = plantExternalIn_gal - plantExternalOut_gal;

        float vctMassAfter_lbm = (vctState.Volume_gal / PlantConstants.FT3_TO_GAL) * rhoAux;
        float brsTotalGalAfter = brsState.HoldupVolume_gal + brsState.DistillateAvailable_gal
            + brsState.ConcentrateAvailable_gal;
        float brsMassAfter_lbm = (brsTotalGalAfter / PlantConstants.FT3_TO_GAL) * rhoAux;
        float externalNetStep_lbm = (externalNetStep_gal / PlantConstants.FT3_TO_GAL) * rhoAux;

        FinalizePrimaryBoundaryFlowEventAux(
            dmVCT_lbm: vctMassAfter_lbm - vctMassBefore_lbm,
            dmBRS_lbm: brsMassAfter_lbm - brsMassBefore_lbm,
            dmExternal_lbm: externalNetStep_lbm,
            externalIn_gal: externalInStep_gal,
            externalOut_gal: externalOutStep_gal,
            makeupExternal_gpm: vctState.MakeupFromBRS ? 0f : vctState.MakeupFlow_gpm,
            divert_gpm: vctState.DivertFlow_gpm,
            cboLoss_gpm: (rcpCount > 0 ? PlantConstants.CBO_LOSS_GPM : 0f));

        // ==============================================================
        // v5.4.1 Fix B: Canonical MASS-based inventory conservation check.
        // Mass is the conserved quantity; volume varies with T/P.
        // Tracks RCS + PZR(water+steam) + VCT + BRS (all compartments).
        // Only true external boundary crossings (RWST additions, CBO
        // losses) change the total. BRS closes the divert/return loop.
        // ==============================================================
        {
            // v5.4.1 Fix B: Use tracked mass values (not recomputed from V×ρ)
            // for RCS and PZR. Recomputing from volume would mask CVCS transfers.
            float rhoVCT = rhoAux;

            float rcsMass = physicsState.RCSWaterMass;  // Tracked, includes CVCS changes
            float pzrWaterMassNow = physicsState.PZRWaterMass;
            float pzrSteamMassNow = physicsState.PZRSteamMass;
            float vctMass = (vctState.Volume_gal / PlantConstants.FT3_TO_GAL) * rhoVCT;
            float brsTotalGal = brsState.HoldupVolume_gal + brsState.DistillateAvailable_gal
                + brsState.ConcentrateAvailable_gal;
            float brsMass = (brsTotalGal / PlantConstants.FT3_TO_GAL) * rhoVCT;

            totalSystemMass_lbm = rcsMass + pzrWaterMassNow + pzrSteamMassNow + vctMass + brsMass;

            // External boundary crossings (converted to mass) — plant-wide,
            // excludes internal transfers such as VCT↔BRS and seal leakoff.
            float externalNetGal = plantExternalNet_gal;
            externalNetMass_lbm = (externalNetGal / PlantConstants.FT3_TO_GAL) * rhoVCT;

            massError_lbm = Mathf.Abs(
                totalSystemMass_lbm - initialSystemMass_lbm - externalNetMass_lbm);

            if (float.IsNaN(massError_lbm) || float.IsInfinity(massError_lbm))
            {
                throw new System.InvalidOperationException(
                    $"Mass conservation produced non-finite value: {massError_lbm}");
            }
        }

        rcsBoronConcentration = vctState.BoronConcentration_ppm;

        // Original CVCS-loop mass conservation cross-check (retained)
        massConservationError = VCTPhysics.VerifyMassConservation(
            vctState,
            vctState.CumulativeRCSChange_gal,
            plantExternalIn_gal,
            plantExternalOut_gal);

        // VCT annunciators
        vctLevelLow = vctState.LowLevelAlarm;
        vctLevelHigh = vctState.HighLevelAlarm;
        vctDivertActive = vctState.DivertActive;
        vctMakeupActive = vctState.AutoMakeupActive;
        vctRWSTSuction = vctState.RWSTSuctionActive;
    }

    // ========================================================================
    // LETDOWN ORIFICE LINEUP MANAGEMENT — v4.4.0
    // Per NRC HRTD 4.1: Operator adjusts orifice lineup during heatup
    // to manage RCS thermal expansion volume removal.
    //
    // Logic: Simulates operator actions based on PZR level error:
    //   1. Normal: 1×75 gpm orifice (default)
    //   2. Level > setpoint + 5%: Open 45-gpm orifice
    //   3. Level > setpoint + 10%: Open second 75-gpm orifice
    //   4. Close additional orifices when level controlled (with hysteresis)
    //
    // Orifice changes are stepped (operator action), not continuous.
    // Hysteresis prevents rapid cycling (valve hunting).
    // ========================================================================

    /// <summary>
    /// v4.4.0: Manage letdown orifice lineup based on PZR level error.
    /// Simulates operator opening/closing additional orifices during heatup.
    /// Only active during two-phase operations (not solid PZR or drain).
    /// </summary>
    void UpdateOrificeLineup()
    {
        // Only manage orifices during normal two-phase operations
        // (not during solid plant, pre-drain, or active drain phases)
        if (solidPressurizer || bubblePreDrainPhase)
            return;

        // Calculate level error (positive = level above setpoint)
        float levelError = pzrLevel - pzrLevelSetpointDisplay;

        // ================================================================
        // OPEN additional orifices when level is rising above setpoint
        // Operator response: open smaller orifice first, then second large
        // ================================================================

        // Step 1: Open 45-gpm orifice at +5% level error
        if (!orifice45Open && levelError > PlantConstants.ORIFICE_OPEN_45_LEVEL_ERROR)
        {
            orifice45Open = true;
            LogEvent(EventSeverity.INFO,
                $"ORIFICE: Opened 45-gpm orifice (PZR level {pzrLevel:F1}%, error +{levelError:F1}%)");
        }

        // Step 2: Open second 75-gpm orifice at +10% level error
        if (orifice75Count < 2 && levelError > PlantConstants.ORIFICE_OPEN_2ND75_LEVEL_ERROR)
        {
            orifice75Count = 2;
            LogEvent(EventSeverity.INFO,
                $"ORIFICE: Opened 2nd 75-gpm orifice (PZR level {pzrLevel:F1}%, error +{levelError:F1}%)");
        }

        // ================================================================
        // CLOSE additional orifices when level is controlled
        // Hysteresis: close threshold = open threshold - hysteresis band
        // Close in reverse order: large orifice first, then small
        // ================================================================

        float hysteresis = PlantConstants.ORIFICE_CLOSE_HYSTERESIS;

        // Step 1: Close second 75-gpm orifice when level error drops
        if (orifice75Count > 1 &&
            levelError < PlantConstants.ORIFICE_OPEN_2ND75_LEVEL_ERROR - hysteresis)
        {
            orifice75Count = 1;
            LogEvent(EventSeverity.INFO,
                $"ORIFICE: Closed 2nd 75-gpm orifice (PZR level {pzrLevel:F1}%, error +{levelError:F1}%)");
        }

        // Step 2: Close 45-gpm orifice when level error drops further
        if (orifice45Open &&
            levelError < PlantConstants.ORIFICE_OPEN_45_LEVEL_ERROR - hysteresis)
        {
            orifice45Open = false;
            LogEvent(EventSeverity.INFO,
                $"ORIFICE: Closed 45-gpm orifice (PZR level {pzrLevel:F1}%, error +{levelError:F1}%)");
        }

        // Update display string
        if (orifice75Count == 2 && orifice45Open)
            orificeLineupDesc = "2×75 + 1×45 gpm";
        else if (orifice75Count == 2)
            orificeLineupDesc = "2×75 gpm";
        else if (orifice45Open)
            orificeLineupDesc = "1×75 + 1×45 gpm";
        else
            orificeLineupDesc = "1×75 gpm";
    }

    // ========================================================================
    // LETDOWN PATH STATE — For display variables
    // ========================================================================

    void UpdateLetdownPath(float pzrLevelSetpoint, bool letdownIsolated, float sealInjection)
    {
        // LETDOWN PATH SELECTION — Owned by CVCSController module
        // Per NRC HRTD 19.0: Path depends on RCS temperature and interlock status
        var letdownPathState = CVCSController.GetLetdownPath(
            T_rcs, pressure, solidPressurizer || bubblePreDrainPhase, letdownIsolated);
        letdownViaRHR = letdownPathState.ViaRHR;
        letdownViaOrifice = letdownPathState.ViaOrifice;
        letdownIsolatedFlag = letdownPathState.IsIsolated;
        pzrLevelSetpointDisplay = pzrLevelSetpoint;
        chargingToRCS = Mathf.Max(0f, chargingFlow - sealInjection);
        totalCCPOutput = chargingFlow;
        orificeLetdownFlow = letdownViaOrifice ? letdownFlow : 0f;
        rhrLetdownFlow = letdownViaRHR ? letdownFlow : 0f;
        divertFraction = vctState.DivertActive ? vctState.DivertFlow_gpm / Mathf.Max(1f, letdownFlow) : 0f;
    }
}
