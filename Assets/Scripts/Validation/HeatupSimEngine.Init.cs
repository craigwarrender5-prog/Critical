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
        // v0.1.0.0: Reset canonical ledger flags for new simulation run
        firstStepLedgerBaselined = false;
        CoupledThermo.ResetSessionFlags();

        // v0.1.0.0 Phase C: Reset diagnostic display to pre-run state
        primaryMassStatus = "NOT_CHECKED";
        primaryMassConservationOK = true;
        primaryMassAlarm = false;
        _previousMassAlarmState = false;
        _previousMassConservationOK = true;

        simTime = 0f;
        T_avg = startTemperature;
        T_cold = startTemperature - 2f;
        T_hot = startTemperature + 2f;
        pressure = startPressure;
        pzrLevel = startPZRLevel;
        rcpCount = 0;
        rcpHeat = 0f;
        pzrHeaterPower = PZR_HEATER_POWER_MW;
        gridEnergy = 0f;
        heatupRate = 0f;
        statusMessage = "PZR HEATERS ENERGIZED - ESTABLISHING CONDITIONS";

        T_rcs = startTemperature;
        T_avg = startTemperature;
        T_cold = startTemperature;
        T_hot = startTemperature;
        
        // v0.8.0: Initialize SG secondary temperature (thermal equilibrium at cold shutdown)
        T_sg_secondary = SGSecondaryThermal.InitializeSecondaryTemperature(startTemperature);
        sgHeatTransfer_MW = 0f;
        
        // v1.3.0: Initialize multi-node SG model
        sgMultiNodeState = SGMultiNodeThermal.Initialize(startTemperature);
        sgTopNodeTemp = startTemperature;
        sgBottomNodeTemp = startTemperature;
        sgStratificationDeltaT = 0f;
        sgCirculationFraction = 0f;
        sgCirculationActive = false;
        
        // v3.0.0: Initialize thermocline display state
        sgThermoclineHeight = PlantConstants.SG_TUBE_TOTAL_HEIGHT_FT;
        sgActiveAreaFraction = PlantConstants.SG_UBEND_AREA_FRACTION;
        sgBoilingActive = false;
        
        // v3.0.0: Initialize RHR system
        // Cold shutdown: RHR running in heatup mode (HX bypassed, pumps on)
        // Warm start: RHR in standby (RCPs already running)
        if (coldShutdownStart && startTemperature < 200f)
        {
            rhrState = RHRSystem.Initialize(startTemperature);
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
        if (coldShutdownStart && startTemperature < 200f)
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
        bubblePreDrainPhase = false;

        // v0.2.0: Initialize CCP, heater mode, and aux spray state
        ccpStarted = false;
        ccpStartTime = 999f;
        ccpStartLevel = 0f;
        currentHeaterMode = HeaterMode.STARTUP_FULL_POWER;
        auxSprayActive = false;
        auxSprayTestPassed = false;
        auxSprayPressureDrop = 0f;

        // v5.4.1 Audit Fix: Initialize at post-fill/vent pressure (100 psig).
        // CVCS PI controller will ramp pressure up to the 350 psig setpoint.
        // Previous code used SOLID_PLANT_INITIAL_PRESSURE_PSIA (365 psia = setpoint),
        // which skipped the pressurization ramp entirely.
        pressure = PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA;

        // Initialize solid plant physics module — owns all P-T-V coupling during solid ops
        solidPlantState = SolidPlantPressure.Initialize(
            pressure, startTemperature, startTemperature, 75f, 75f);

        // Display values from physics module
        solidPlantPressureSetpoint = solidPlantState.PressureSetpoint;
        solidPlantPressureLow = PlantConstants.SOLID_PLANT_P_LOW_PSIA;
        solidPlantPressureHigh = PlantConstants.SOLID_PLANT_P_HIGH_PSIA;

        float rhoWater = WaterProperties.WaterDensity(T_rcs, pressure);
        rcsWaterMass = PlantConstants.RCS_WATER_VOLUME * rhoWater;

        pzrLevel = 100f;
        pzrWaterVolume = PlantConstants.PZR_TOTAL_VOLUME;
        pzrSteamVolume = 0f;
        float pzrWaterMass = pzrWaterVolume * rhoWater;

        totalSystemMass = rcsWaterMass + pzrWaterMass;

        T_pzr = T_rcs;
        T_sat = WaterProperties.SaturationTemperature(pressure);

        physicsState = new SystemState();
        physicsState.Temperature = T_rcs;
        physicsState.Pressure = pressure;
        physicsState.RCSVolume = PlantConstants.RCS_WATER_VOLUME;
        physicsState.RCSWaterMass = rcsWaterMass;
        physicsState.PZRWaterVolume = pzrWaterVolume;
        physicsState.PZRSteamVolume = 0f;
        physicsState.PZRWaterMass = pzrWaterMass;
        physicsState.PZRSteamMass = 0f;

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

        // v0.6.0: Initialize BRS
        // v0.9.6 FIX: Pre-load BRS with distillate inventory from prior operating cycle.
        // Without this, VCT makeup cannot draw from BRS (holdup never reaches 5000 gal
        // evaporator threshold), forcing RMS primary water makeup which dilutes boron.
        // Real plants would have processed water available from previous operations.
        brsState = BRSPhysics.Initialize(PlantConstants.BORON_COLD_SHUTDOWN_BOL_PPM);
        brsState.DistillateAvailable_gal = PlantConstants.BRS_INITIAL_DISTILLATE_GAL;
        brsState.DistillateBoron_ppm = PlantConstants.BRS_DISTILLATE_BORON_PPM;  // 0 ppm (clean)

        statusMessage = "COLD SHUTDOWN - SOLID PZR - HEATERS ENERGIZING";
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
        bubblePreDrainPhase = false;
        pressure = startPressure;

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
        letdownFlow = 75f;
        chargingFlow = 75f;
        cvcsIntegralError = 0f;

        // Cold start always begins with RHR crossconnect letdown path
        // Per NRC HRTD 19.0: at low RCS pressure, letdown is via RHR-CVCS crossconnect (HCV-128)
        letdownViaRHR = (coldShutdownStart && startTemperature < PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F);
        letdownViaOrifice = !letdownViaRHR;
        surgeFlow = 0f;
        pressureRate = 0f;
        pzrHeatRate = 0f;
        rcsHeatRate = 0f;

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
        pzrHeatersOn = true;
        ccwRunning = true;
        sealInjectionOK = true;
        chargingActive = true;
        letdownActive = true;

        // Clear history buffers and event log
        ClearHistoryAndEvents();

        // Reset alarm edge detection state
        prev_rcpCount = -1;
        prev_plantMode = -1;

        // Log initial state
        if (solidPressurizer)
            LogEvent(EventSeverity.INFO, "INIT: Cold shutdown - solid pressurizer");
        else
            LogEvent(EventSeverity.INFO, "INIT: Warm start - steam bubble exists");
        LogEvent(EventSeverity.INFO, $"T_rcs={T_rcs:F0}F  P={pressure:F0}psia  PZR={pzrLevel:F0}%");
        LogEvent(EventSeverity.ACTION, "PZR heaters energized");

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
