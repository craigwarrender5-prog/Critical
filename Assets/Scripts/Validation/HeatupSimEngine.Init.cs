// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (Initialization Partial)
// HeatupSimEngine.Init.cs - Cold Start & Warm Start Initialization
// ============================================================================
//
// PURPOSE:
//   All initialization logic for the heatup simulation, including cold
//   shutdown (solid PZR) and warm start (bubble exists) entry conditions.
//   Extracted from RunSimulation() to achieve single responsibility (G1).
//
// PHYSICS:
//   Cold shutdown initialization per NRC ML11223A342 Section 19.2.2:
//     - Solid pressurizer at 350 psig (320-400 psig control band)
//     - PZR level 100% (water-solid, no steam space)
//     - Heaters energized at full power (STARTUP_FULL_POWER mode)
//     - CCP not started, aux spray not tested
//   Warm start initialization:
//     - Steam bubble already exists at specified PZR level
//     - CVCS PI controller initialized for two-phase operations
//
// SOURCES:
//   - NRC HRTD 19.2.1 — Solid plant pressure control band (320-400 psig)
//   - NRC HRTD 19.2.2 — Cold shutdown startup sequence
//   - NRC HRTD 19.0 — RHR letdown path at low temperature
//
// ARCHITECTURE:
//   Partial class of HeatupSimEngine. This file owns:
//     - InitializeColdShutdown() — Mode 5 cold start state setup
//     - InitializeWarmStart() — Hot start with existing bubble
//     - InitializeCommon() — Shared initialization for both paths
//   Called by RunSimulation() in the main HeatupSimEngine.cs.
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using Critical.Physics;

/// <summary>
/// Explicit CVCS lineup state used by cold shutdown initialization.
/// </summary>
public struct ColdShutdownCvcsLineupState
{
    public bool Hcv128Open;
    public bool Pcv131Auto;
    public bool LetdownIsolated;
    public int Orifice75OpenCount;
    public bool Orifice45Open;
    public float LetdownFlow_gpm;
    public float ChargingFlow_gpm;
}

/// <summary>
/// Formal cold-shutdown profile authority for startup initialization.
/// </summary>
public struct ColdShutdownProfile
{
    public float Pressure_psia;
    public float Temperature_F;
    public float PzrLiquidMass_lbm;
    public float PzrVaporMass_lbm;
    public HeaterMode HeaterMode;
    public bool StartupHoldEnabled;
    public float StartupHoldDuration_sec;
    public ColdShutdownCvcsLineupState CvcsLineup;

    /// <summary>
    /// Build the approved baseline profile for deterministic cold starts.
    /// </summary>
    public static ColdShutdownProfile CreateApprovedBaseline()
    {
        float pressure_psia = PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA;
        float temperature_F = 120f;
        float rho = WaterProperties.WaterDensity(temperature_F, pressure_psia);
        float pzrLiquidMass_lbm = PlantConstants.PZR_TOTAL_VOLUME * rho;

        return new ColdShutdownProfile
        {
            Pressure_psia = pressure_psia,
            Temperature_F = temperature_F,
            PzrLiquidMass_lbm = pzrLiquidMass_lbm,
            PzrVaporMass_lbm = 0f,
            // Default to automatic pressurization control after startup hold release.
            // Until heater UI controls are implemented, this prevents hold-release
            // from leaving heaters latched OFF.
            HeaterMode = HeaterMode.PRESSURIZE_AUTO,
            StartupHoldEnabled = true,
            StartupHoldDuration_sec = 15f,
            CvcsLineup = new ColdShutdownCvcsLineupState
            {
                Hcv128Open = true,
                Pcv131Auto = true,
                LetdownIsolated = false,
                Orifice75OpenCount = 1,
                Orifice45Open = false,
                LetdownFlow_gpm = PlantConstants.LETDOWN_NORMAL_GPM,
                ChargingFlow_gpm = PlantConstants.LETDOWN_NORMAL_GPM
            }
        };
    }
}

public partial class HeatupSimEngine
{
    // ========================================================================
    // INITIALIZATION ENTRY POINT — Called by RunSimulation()
    // ========================================================================

    /// <summary>
    /// Initialize the simulation based on inspector settings.
    /// Dispatches to cold shutdown or warm start path, then runs common init.
    /// </summary>
    void InitializeSimulation()
    {
        // ============================================================
        // v0.3.0.0 Phase A (CS-0032): Frame rate cap — 30 FPS target.
        // Prevents main thread from burning 100% CPU on render/physics,
        // ensures OS message pump and input events are serviced.
        // ============================================================
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;  // Disable VSync so targetFrameRate takes effect

        // v0.1.0.0: Reset canonical ledger flags for new simulation run
        firstStepLedgerBaselined = false;
        regime3CVCSPreApplied = false;
        deliveredRcpHeat_MW = 0f;
        noRcpTransportFactor = 1f;
        thermoStateWriter = "INIT";
        smoothedRegime2Alpha = 0f;
        previousPhysicsRegimeId = 0;
        previousPhysicsRegimeLabel = "UNSET";
        nextRegime2ConvergenceWarnTime_hr = 0f;
        nextRegime3ConvergenceWarnTime_hr = 0f;
        nextR1MassAuditWarnTime_hr = 0f;
        nextPbocPairingWarnTime_hr = 0f;
        hotPathWarningSuppressedCount = 0;
        pbocTickIndex = 0;
        pbocEventActiveThisTick = false;
        pbocCurrentEvent = default;
        pbocLastEvent = default;
        pbocLastEvent.RegimeLabel = "NONE";
        pbocLastEvent.PairingCheckPass = true;
        pbocEventCount = 0;
        pbocPairingAssertionFailures = 0;
        CoupledThermo.ResetSessionFlags();
        ResetHZPSystemsLifecycle();

        longHoldPressureAuditActive = false;
        longHoldPressureWriteCount = 0;
        longHoldPressureStateDerivedWriteCount = 0;
        longHoldPressureOverrideAttemptCount = 0;
        longHoldPressureBlockedOverrideCount = 0;
        longHoldPressureInvariantFailed = false;
        longHoldPressureInvariantReason = "";
        longHoldLastPressureBefore = 0f;
        longHoldLastPressureAfter = 0f;
        longHoldLastPressureSource = "NONE";
        longHoldLastPressureStack = "";
        longHoldPressureTickCount = 0;
        longHoldPressureRegime = "UNSET";
        longHoldPressureEquationBranch = "UNSET";
        longHoldPressureUsesSaturation = false;
        longHoldPressureSaturationPsia = 0f;
        longHoldPressureModelDensity = 0f;
        longHoldPressureModelCompressibility = 0f;
        longHoldPressureModelDeltaPsi = 0f;

        // v0.1.0.0 Phase C: Reset diagnostic display to pre-run state
        primaryMassStatus = "NOT_CHECKED";
        primaryMassConservationOK = true;
        primaryMassAlarm = false;
        _previousMassAlarmState = false;
        _previousMassConservationOK = true;

        // IP-0018 DP-0003 deterministic telemetry reset
        stageE_PrimaryHeatInput_MW = 0f;
        stageE_TotalPrimaryEnergy_MJ = 0f;
        stageE_TotalSGEnergyRemoved_MJ = 0f;
        stageE_PercentMismatch = 0f;
        stageE_EnergySampleCount = 0;

        pzrOrificeDiagTickCounter = 0;
        pzrOrificeDiagLast75Count = -1;
        pzrOrificeDiagLast45Open = false;

        // IP-0016 RTCC/session-scoped telemetry reset
        rtccTransitionCount = 0;
        rtccAssertionFailureCount = 0;
        rtccMaxRawDeltaAbs_lbm = 0f;
        rtccLastPreMass_lbm = 0f;
        rtccLastReconstructedMass_lbm = 0f;
        rtccLastRawDelta_lbm = 0f;
        rtccLastPostMass_lbm = 0f;
        rtccLastAssertDelta_lbm = 0f;
        rtccLastTransition = "NONE";
        rtccLastAuthorityFrom = "UNSET";
        rtccLastAuthorityTo = "UNSET";
        rtccLastAssertPass = true;
        rtccTelemetryPresent = false;

        // IP-0016 plant-wide external boundary accumulators
        plantExternalIn_gal = 0f;
        plantExternalOut_gal = 0f;
        plantExternalNet_gal = 0f;

        bool useColdProfile = coldShutdownStart && startTemperature < 200f;
        coldShutdownProfile = useColdProfile
            ? ColdShutdownProfile.CreateApprovedBaseline()
            : default;

        float initTemperature = useColdProfile ? coldShutdownProfile.Temperature_F : startTemperature;
        float initPressure = useColdProfile ? coldShutdownProfile.Pressure_psia : startPressure;
        float initPzrLevel = useColdProfile ? 100f : startPZRLevel;

        simTime = 0f;
        T_avg = initTemperature;
        T_cold = initTemperature - 2f;
        T_hot = initTemperature + 2f;
        pressure = initPressure;
        pzrLevel = initPzrLevel;
        rcpCount = 0;
        rcpHeat = 0f;
        pzrHeaterPower = 0f;
        gridEnergy = 0f;
        heatupRate = 0f;
        statusMessage = useColdProfile
            ? "COLD PROFILE LOADED - STARTUP HOLD ACTIVE"
            : "PZR HEATERS ENERGIZED - ESTABLISHING CONDITIONS";

        T_rcs = initTemperature;
        T_avg = initTemperature;
        T_cold = initTemperature;
        T_hot = initTemperature;
        
        // v0.8.0: Initialize SG secondary temperature (thermal equilibrium at cold shutdown)
        T_sg_secondary = SGSecondaryThermal.InitializeSecondaryTemperature(initTemperature);
        sgHeatTransfer_MW = 0f;
        
        // v1.3.0: Initialize multi-node SG model
        sgMultiNodeState = SGMultiNodeThermal.Initialize(initTemperature);
        sgTopNodeTemp = initTemperature;
        sgBottomNodeTemp = initTemperature;
        sgStratificationDeltaT = 0f;
        sgCirculationFraction = 0f;
        sgCirculationActive = false;
        
        // v3.0.0: Initialize thermocline display state
        sgThermoclineHeight = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT;
        sgActiveAreaFraction = PlantConstants.SG_UBEND_AREA_FRACTION;
        sgBoilingActive = false;
        sgBoundaryMode = "OPEN";
        sgPressureSourceBranch = "floor";
        sgSteamInventory_lb = 0f;
        ResetSGBoundaryStartupState();
        dp0003BaselineSignature =
            $"T0={initTemperature:F1}F;P0={initPressure:F1}psia;PZR0={initPzrLevel:F1}%;dt={DP0003_DETERMINISTIC_TIMESTEP_HR:F6}hr;log={DP0003_INTERVAL_LOG_HR:F2}hr";
        netPlantHeat_MW = 0f;
        
        // v3.0.0: Initialize RHR system
        // Cold shutdown: RHR running in heatup mode (HX bypassed, pumps on)
        // Warm start: RHR in standby (RCPs already running)
        if (useColdProfile)
        {
            rhrState = RHRSystem.Initialize(initTemperature);
        }
        else
        {
            rhrState = RHRSystem.InitializeStandby();
        }
        rhrNetHeat_MW = 0f;
        rhrHXRemoval_MW = 0f;
        rhrPumpHeat_MW = 0f;
        rhrActive = rhrState.Mode != RHRMode.Standby;
        rhrModeString = rhrState.Mode.ToString();

        // Initialize time acceleration module — start at 1x real-time
        TimeAcceleration.Initialize(0);
        wallClockTime = 0f;
        currentSpeedIndex = 0;
        isAccelerated = false;

        // Dispatch to appropriate initialization path
        if (useColdProfile)
        {
            InitializeColdShutdown();
        }
        else
        {
            InitializeWarmStart();
        }

        // Common initialization for both paths
        InitializeCommon();
    }

    // ========================================================================
    // COLD SHUTDOWN INITIALIZATION
    // Per NRC ML11223A342 Section 19.2.2: Solid pressurizer, low pressure
    // ========================================================================

    void InitializeColdShutdown()
    {
        solidPressurizer = true;
        bubbleFormed = false;
        bubbleFormationTime = 999f;  // Not yet formed
        bubblePhase = BubbleFormationPhase.NONE;
        bubblePhaseStartTime = 0f;
        bubbleDrainStartLevel = 0f;
        bubblePreDrainPhase = false;

        // v0.2.0: Initialize CCP, heater mode, and aux spray state
        ccpStarted = false;
        ccpStartTime = 999f;
        ccpStartLevel = 0f;
        currentHeaterMode = coldShutdownProfile.HeaterMode;
        auxSprayActive = false;
        auxSprayTestPassed = false;
        auxSprayPressureDrop = 0f;
        startupHoldActive = coldShutdownProfile.StartupHoldEnabled;
        startupHoldReleaseTime_hr = startupHoldActive
            ? coldShutdownProfile.StartupHoldDuration_sec / 3600f
            : 0f;
        startupHoldReleaseLogged = false;
        startupHoldActivationLogged = false;
        startupHoldStartTime_hr = 0f;
        startupHoldElapsedTime_hr = 0f;
        startupHoldPressureRateAbs_psi_hr = 0f;
        startupHoldTimeGatePassed = false;
        startupHoldPressureRateGatePassed = false;
        startupHoldStateQualityGatePassed = false;
        startupHoldReleaseBlockReason = startupHoldActive ? "MIN_TIME_NOT_REACHED" : "NONE";
        startupHoldPressureRateStableAccum_sec = 0f;
        startupHoldLastBlockedLogTime_hr = -1f;

        // v5.4.1 Audit Fix: Initialize at post-fill/vent pressure (100 psig).
        // CVCS PI controller will ramp pressure up to the 350 psig setpoint.
        // Previous code used SOLID_PLANT_INITIAL_PRESSURE_PSIA (365 psia = setpoint),
        // which skipped the pressurization ramp entirely.
        pressure = coldShutdownProfile.Pressure_psia;

        // Initialize solid plant physics module — owns all P-T-V coupling during solid ops
        solidPlantState = SolidPlantPressure.Initialize(
            pressure,
            coldShutdownProfile.Temperature_F,
            coldShutdownProfile.Temperature_F,
            coldShutdownProfile.CvcsLineup.LetdownFlow_gpm,
            coldShutdownProfile.CvcsLineup.ChargingFlow_gpm);

        // Display values from physics module
        solidPlantPressureSetpoint = solidPlantState.PressureSetpoint;
        solidPlantPressureLow = PlantConstants.SOLID_PLANT_P_LOW_PSIA;
        solidPlantPressureHigh = PlantConstants.SOLID_PLANT_P_HIGH_PSIA;

        float rhoWater = WaterProperties.WaterDensity(coldShutdownProfile.Temperature_F, pressure);
        rcsWaterMass = PlantConstants.RCS_WATER_VOLUME * rhoWater;

        pzrLevel = 100f;
        pzrWaterVolume = PlantConstants.PZR_TOTAL_VOLUME;
        pzrSteamVolume = 0f;
        float pzrWaterMass = coldShutdownProfile.PzrLiquidMass_lbm;

        totalSystemMass = rcsWaterMass + pzrWaterMass;

        T_rcs = coldShutdownProfile.Temperature_F;
        T_avg = coldShutdownProfile.Temperature_F;
        T_hot = coldShutdownProfile.Temperature_F;
        T_cold = coldShutdownProfile.Temperature_F;
        T_pzr = coldShutdownProfile.Temperature_F;
        T_sat = WaterProperties.SaturationTemperature(pressure);

        physicsState = new SystemState();
        physicsState.Temperature = T_rcs;
        physicsState.Pressure = pressure;
        physicsState.RCSVolume = PlantConstants.RCS_WATER_VOLUME;
        physicsState.RCSWaterMass = rcsWaterMass;
        physicsState.PZRWaterVolume = pzrWaterVolume;
        physicsState.PZRSteamVolume = 0f;
        physicsState.PZRWaterMass = pzrWaterMass;
        physicsState.PZRSteamMass = coldShutdownProfile.PzrVaporMass_lbm;
        physicsState.PZRTotalEnthalpy_BTU = pzrWaterMass * WaterProperties.WaterEnthalpy(T_pzr, pressure);
        physicsState.PZRClosureConverged = true;
        physicsState.PZRClosureVolumeResidual_ft3 = 0f;
        physicsState.PZRClosureEnergyResidual_BTU = 0f;

        // v5.4.1 Audit Fix Stage 0: Populate SOLID canonical fields at init.
        // These are read by UpdateInventoryAudit() solid branch (Logging.cs:303-310)
        // and by UpdatePrimaryMassLedgerDiagnostics() (Logging.cs:470-489).
        physicsState.PZRWaterMassSolid = pzrWaterMass;
        physicsState.TotalPrimaryMassSolid = rcsWaterMass + pzrWaterMass;

        // v5.4.1 Audit Fix Stage 0: Populate v5.3.0 canonical ledger fields.
        // TotalPrimaryMass_lb is the authoritative ledger for all regimes (CoupledThermo.cs:768).
        // InitialPrimaryMass_lb is the baseline for conservation diagnostics (CoupledThermo.cs:778).
        physicsState.TotalPrimaryMass_lb = rcsWaterMass + pzrWaterMass;
        physicsState.InitialPrimaryMass_lb = rcsWaterMass + pzrWaterMass;
        // v5.4.2.0 FF-05 Fix #4: Ledger will be re-baselined after first physics step
        // to eliminate init-to-first-step density mismatch. See HeatupSimEngine.cs.

        vctState = VCTPhysics.InitializeColdShutdown(PlantConstants.BORON_COLD_SHUTDOWN_BOL_PPM);
        rcsBoronConcentration = PlantConstants.BORON_COLD_SHUTDOWN_BOL_PPM;

        pzrTotalEnthalpy_BTU = physicsState.PZRTotalEnthalpy_BTU;
        pzrSpecificEnthalpy_BTU_lb = physicsState.PZRWaterMass > 0f ? physicsState.PZRTotalEnthalpy_BTU / physicsState.PZRWaterMass : 0f;
        pzrClosureVolumeResidual_ft3 = 0f;
        pzrClosureEnergyResidual_BTU = 0f;
        pzrClosureConverged = true;
        pzrClosureBracketMassTarget_lbm = 0f;
        pzrClosureBracketEnthalpyTarget_BTU = 0f;
        pzrClosureBracketVolumeTarget_ft3 = PlantConstants.PZR_TOTAL_VOLUME;
        pzrClosureBracketPressureGuess_psia = pressure;
        pzrClosureBracketOperatingMin_psia = 0f;
        pzrClosureBracketOperatingMax_psia = 0f;
        pzrClosureBracketHardMin_psia = 0f;
        pzrClosureBracketHardMax_psia = 0f;
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
        pzrClosureBracketSearchTrace = "";

        // v0.6.0: Initialize BRS
        // v0.9.6 FIX: Pre-load BRS with distillate inventory from prior operating cycle.
        // Without this, VCT makeup cannot draw from BRS (holdup never reaches 5000 gal
        // evaporator threshold), forcing RMS primary water makeup which dilutes boron.
        // Real plants would have processed water available from previous operations.
        brsState = BRSPhysics.Initialize(PlantConstants.BORON_COLD_SHUTDOWN_BOL_PPM);
        brsState.DistillateAvailable_gal = PlantConstants.BRS_INITIAL_DISTILLATE_GAL;
        brsState.DistillateBoron_ppm = PlantConstants.BRS_DISTILLATE_BORON_PPM;  // 0 ppm (clean)

        pzrHeaterPower = 0f;
        pzrHeatersOn = false;
        orifice75Count = coldShutdownProfile.CvcsLineup.Orifice75OpenCount;
        orifice45Open = coldShutdownProfile.CvcsLineup.Orifice45Open;
        orificeLineupDesc = coldShutdownProfile.CvcsLineup.Orifice45Open
            ? "1x75 + 1x45 gpm (PROFILE)"
            : "1x75 gpm (PROFILE)";
        statusMessage = startupHoldActive
            ? "COLD SHUTDOWN - PROFILE LOADED - STARTUP HOLD ACTIVE"
            : "COLD SHUTDOWN - PROFILE LOADED";
        Debug.Log($"[HeatupEngine] COLD SHUTDOWN: Solid pressurizer");
        Debug.Log($"  T_rcs = {T_rcs:F1}°F, P = {pressure - 14.7f:F0} psig ({pressure:F1} psia)");
        Debug.Log($"  Total mass = {totalSystemMass:F0} lb (conserved)");
        Debug.Log($"  RCS mass = {rcsWaterMass:F0} lb, PZR mass = {pzrWaterMass:F0} lb");
        Debug.Log($"  VCT Level = {vctState.Level_percent:F0}%, Boron = {vctState.BoronConcentration_ppm:F0} ppm");
        Debug.Log($"  BRS Distillate Available = {brsState.DistillateAvailable_gal:F0} gal (v0.9.6 pre-loaded)");
    }

    // ========================================================================
    // WARM START INITIALIZATION
    // Steam bubble already exists at specified PZR level.
    // ========================================================================

    void InitializeWarmStart()
    {
        solidPressurizer = false;
        bubbleFormed = true;
        bubbleFormationTime = 0f;  // Already formed at start
        bubblePhase = BubbleFormationPhase.NONE;
        bubblePhaseStartTime = 0f;
        bubbleDrainStartLevel = 0f;
        bubblePreDrainPhase = false;
        pressure = startPressure;
        startupHoldActive = false;
        startupHoldReleaseTime_hr = 0f;
        startupHoldReleaseLogged = true;
        startupHoldActivationLogged = true;
        startupHoldStartTime_hr = 0f;
        startupHoldElapsedTime_hr = 0f;
        startupHoldPressureRateAbs_psi_hr = 0f;
        startupHoldTimeGatePassed = true;
        startupHoldPressureRateGatePassed = true;
        startupHoldStateQualityGatePassed = true;
        startupHoldReleaseBlockReason = "NONE";
        startupHoldPressureRateStableAccum_sec = 0f;
        startupHoldLastBlockedLogTime_hr = -1f;
        coldShutdownProfile = default;

        physicsState = new SystemState();
        physicsState.Temperature = T_rcs;
        physicsState.Pressure = pressure;
        physicsState.RCSVolume = PlantConstants.RCS_WATER_VOLUME;

        float rcsRho = WaterProperties.WaterDensity(T_rcs, pressure);
        physicsState.RCSWaterMass = physicsState.RCSVolume * rcsRho;

        T_sat = WaterProperties.SaturationTemperature(pressure);
        T_pzr = T_sat;

        float pzrRhoWater = WaterProperties.WaterDensity(T_sat, pressure);
        float pzrRhoSteam = WaterProperties.SaturatedSteamDensity(pressure);

        physicsState.PZRWaterVolume = PlantConstants.PZR_TOTAL_VOLUME * startPZRLevel / 100f;
        physicsState.PZRSteamVolume = PlantConstants.PZR_TOTAL_VOLUME - physicsState.PZRWaterVolume;
        physicsState.PZRWaterMass = physicsState.PZRWaterVolume * pzrRhoWater;
        physicsState.PZRSteamMass = physicsState.PZRSteamVolume * pzrRhoSteam;
        physicsState.PZRTotalEnthalpy_BTU =
            physicsState.PZRWaterMass * WaterProperties.SaturatedLiquidEnthalpy(pressure) +
            physicsState.PZRSteamMass * WaterProperties.SaturatedSteamEnthalpy(pressure);
        physicsState.PZRClosureConverged = true;
        physicsState.PZRClosureVolumeResidual_ft3 = 0f;
        physicsState.PZRClosureEnergyResidual_BTU = 0f;

        totalSystemMass = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;

        // v5.4.1 Audit Fix Stage 0: Populate v5.3.0 canonical ledger fields for warm start.
        // Warm start begins in two-phase, so SOLID fields are not needed,
        // but the ledger and initial fields must be set for diagnostics.
        physicsState.TotalPrimaryMass_lb = physicsState.RCSWaterMass
                                          + physicsState.PZRWaterMass
                                          + physicsState.PZRSteamMass;
        physicsState.InitialPrimaryMass_lb = physicsState.TotalPrimaryMass_lb;
        // v5.4.2.0 FF-05 Fix #4: Ledger will be re-baselined after first physics step
        // to eliminate init-to-first-step density mismatch. See HeatupSimEngine.cs.

        rcsWaterMass = physicsState.RCSWaterMass;
        pzrWaterVolume = physicsState.PZRWaterVolume;
        pzrSteamVolume = physicsState.PZRSteamVolume;
        pzrLevel = physicsState.PZRLevel;
        pzrTotalEnthalpy_BTU = physicsState.PZRTotalEnthalpy_BTU;
        pzrSpecificEnthalpy_BTU_lb =
            (physicsState.PZRWaterMass + physicsState.PZRSteamMass) > 0f
                ? physicsState.PZRTotalEnthalpy_BTU / (physicsState.PZRWaterMass + physicsState.PZRSteamMass)
                : 0f;
        pzrClosureVolumeResidual_ft3 = 0f;
        pzrClosureEnergyResidual_BTU = 0f;
        pzrClosureConverged = true;
        pzrClosureBracketMassTarget_lbm = 0f;
        pzrClosureBracketEnthalpyTarget_BTU = 0f;
        pzrClosureBracketVolumeTarget_ft3 = PlantConstants.PZR_TOTAL_VOLUME;
        pzrClosureBracketPressureGuess_psia = pressure;
        pzrClosureBracketOperatingMin_psia = 0f;
        pzrClosureBracketOperatingMax_psia = 0f;
        pzrClosureBracketHardMin_psia = 0f;
        pzrClosureBracketHardMax_psia = 0f;
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
        pzrClosureBracketSearchTrace = "";

        vctState = VCTPhysics.InitializeNormal(55f, 1000f);
        rcsBoronConcentration = 1000f;

        // v0.6.0: Initialize BRS
        // v0.9.6: Pre-load with distillate for consistency with cold shutdown
        brsState = BRSPhysics.Initialize(1000f);
        brsState.DistillateAvailable_gal = PlantConstants.BRS_INITIAL_DISTILLATE_GAL;
        brsState.DistillateBoron_ppm = PlantConstants.BRS_DISTILLATE_BORON_PPM;

        // Initialize CVCS Controller for two-phase operations (warm start)
        // v0.4.0 Issue #2: Use unified function for full operating range
        float warmStartSetpoint = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
        cvcsControllerState = CVCSController.Initialize(
            pzrLevel, warmStartSetpoint, 75f, 75f, 0f);

        statusMessage = "BUBBLE EXISTS - PZR HEATERS ENERGIZED";
        Debug.Log($"[HeatupEngine] WARM START: Bubble exists at {pzrLevel:F0}% level");
        Debug.Log($"  VCT Level = {vctState.Level_percent:F0}%, Boron = {vctState.BoronConcentration_ppm:F0} ppm");
    }

    // ========================================================================
    // COMMON INITIALIZATION — Shared by both cold shutdown and warm start
    // ========================================================================

    void InitializeCommon()
    {
        if (solidPressurizer)
        {
            letdownFlow = coldShutdownProfile.CvcsLineup.LetdownFlow_gpm;
            chargingFlow = coldShutdownProfile.CvcsLineup.ChargingFlow_gpm;
            orifice75Count = coldShutdownProfile.CvcsLineup.Orifice75OpenCount;
            orifice45Open = coldShutdownProfile.CvcsLineup.Orifice45Open;
            orificeLineupDesc = coldShutdownProfile.CvcsLineup.Orifice45Open
                ? "1x75 + 1x45 gpm (PROFILE)"
                : "1x75 gpm (PROFILE)";
        }
        else
        {
            letdownFlow = 75f;
            chargingFlow = 75f;
        }
        cvcsIntegralError = 0f;

        // Cold start always begins with RHR crossconnect letdown path
        // Per NRC HRTD 19.0: at low RCS pressure, letdown is via RHR-CVCS crossconnect (HCV-128)
        letdownViaRHR = (coldShutdownStart && startTemperature < PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F);
        letdownViaOrifice = !letdownViaRHR;
        surgeFlow = 0f;
        pressureRate = 0f;
        pzrHeatRate = 0f;
        rcsHeatRate = 0f;
        pzrClosureSolveAttempts = 0;
        pzrClosureSolveConverged = 0;
        pzrClosureConvergencePct = 0f;
        pzrClosureLastIterationCount = 0;
        pzrClosureLastFailureReason = "NONE";
        pzrClosureLastConvergencePattern = "UNSET";
        pzrClosureLastPhaseFraction = 0f;
        heaterManualDisabled = false;
        heaterAuthorityState = startupHoldActive ? "HOLD_LOCKED" : "AUTO";
        heaterAuthorityReason = startupHoldActive ? "startup_hold_active" : "auto_authority";
        heaterLimiterReason = startupHoldActive ? "HOLD_LOCKED" : "NONE";
        heaterLimiterDetail = startupHoldActive ? "Startup hold authority active" : "No active limiter";
        heaterPressureRateClampActive = false;
        heaterRampRateClampActive = false;
        heaterAutoDemandComputeSuppressed = startupHoldActive;
        lastLoggedHeaterAuthorityState = "UNSET";
        lastLoggedHeaterLimiterReason = "UNSET";
        drainSteamDisplacement_lbm = 0f;
        drainCvcsTransfer_lbm = 0f;
        drainDuration_hr = 0f;
        drainExitPressure_psia = 0f;
        drainExitLevel_pct = 0f;
        drainHardGateTriggered = false;
        drainPressureBandMaintained = true;
        drainTransitionReason = "NONE";
        drainCvcsPolicyMode = "LEGACY_FIXED";
        drainLetdownFlow_gpm = 0f;
        drainLetdownDemand_gpm = 0f;
        drainChargingFlow_gpm = 0f;
        drainNetOutflowFlow_gpm = 0f;
        drainLineupDemandIndex = 1;
        drainHydraulicCapacity_gpm = 0f;
        drainHydraulicDeltaP_psi = 0f;
        drainHydraulicDensity_lbm_ft3 = 0f;
        drainHydraulicQuality = 0f;
        drainLetdownSaturated = false;
        drainLineupEventThisStep = false;
        drainLineupEventCount = 0;
        drainLastLineupEventTime_hr = -1f;
        drainLastLineupPrevIndex = 1;
        drainLastLineupNewIndex = 1;
        drainLastLineupTrigger = "NONE";
        drainLastLineupReason = "NONE";
        drainLineupChangePending = false;
        drainLineupRequestedIndex = 1;
        drainLineupRequestedTrigger = "NONE";
        drainLineupRequestedReason = "NONE";
        cvcsThermalMixing_MW = 0f;
        cvcsThermalMixingDeltaF = 0f;

        rvlisDynamic = 40f;
        rvlisFull = 100f;
        rvlisUpper = 100f;
        rvlisDynamicValid = false;
        rvlisFullValid = true;
        rvlisUpperValid = true;

        for (int i = 0; i < 4; i++) rcpRunning[i] = false;

        // v0.4.0 Issue #3: Reset per-pump ramp-up tracking
        for (int i = 0; i < 4; i++) rcpStartTimes[i] = float.MaxValue;
        rcpContribution = new RCPContribution();
        effectiveRCPHeat = 0f;
        pzrHeatersOn = !startupHoldActive && currentHeaterMode != HeaterMode.OFF;
        ccwRunning = true;
        sealInjectionOK = true;
        chargingActive = true;
        letdownActive = true;
        sgSecondaryPressureHigh = false;

        // Clear history buffers and event log
        ClearHistoryAndEvents();

        // Reset alarm edge detection state
        prev_rcpCount = -1;
        prev_plantMode = -1;
        prev_sgSecondaryPressureHigh = false;

        // Log initial state
        if (solidPressurizer)
            LogEvent(EventSeverity.INFO, "INIT: Cold shutdown - solid pressurizer");
        else
            LogEvent(EventSeverity.INFO, "INIT: Warm start - steam bubble exists");
        LogEvent(EventSeverity.INFO, $"T_rcs={T_rcs:F0}F  P={pressure:F0}psia  PZR={pzrLevel:F0}%");
        LogEvent(
            EventSeverity.ACTION,
            pzrHeatersOn ? "PZR heaters energized" : "PZR heaters inhibited (cold profile startup state)");

        T_sat = GetTsat(pressure);
        subcooling = T_sat - T_rcs;
        plantMode = GetMode(T_rcs);

        // v0.9.5: Add initial data point immediately so graphs have something to display
        AddHistory();

        // v5.4.1 Fix B: Compute initial total system MASS for conservation check.
        // Mass is the conserved quantity — volume varies with T/P.
        // Total = RCS(lbm) + PZR_water(lbm) + PZR_steam(lbm) + VCT(lbm) + BRS(lbm)
        {
            float rhoVCT = WaterProperties.WaterDensity(100f, 14.7f);  // VCT at ~100°F, atmospheric

            // Use tracked masses (already computed in Init paths above)
            float rcsMass = physicsState.RCSWaterMass;
            float pzrMass = physicsState.PZRWaterMass + physicsState.PZRSteamMass;
            float vctMass = (vctState.Volume_gal / PlantConstants.FT3_TO_GAL) * rhoVCT;
            float brsTotalGal = brsState.HoldupVolume_gal + brsState.DistillateAvailable_gal
                + brsState.ConcentrateAvailable_gal;
            float brsMass = (brsTotalGal / PlantConstants.FT3_TO_GAL) * rhoVCT;

            initialSystemMass_lbm = rcsMass + pzrMass + vctMass + brsMass;
            totalSystemMass_lbm = initialSystemMass_lbm;
            externalNetMass_lbm = 0f;
            massError_lbm = 0f;
        }

        // v4.4.0: Initialize pressurizer spray system
        sprayState = CVCSController.InitializeSpray();
        sprayFlow_GPM = PlantConstants.SPRAY_BYPASS_FLOW_GPM;
        sprayValvePosition = 0f;
        sprayActive = false;
        spraySteamCondensed_lbm = 0f;

        // v1.1.1 FIX: Initialize inventory audit for comprehensive mass balance tracking
        // Without this call, Initial_Total_Mass_lbm remains 0, causing immediate conservation alarms
        InitializeInventoryAudit();
    }
}
