// ============================================================================
// CRITICAL: Master the Atom - Heatup Simulation Engine
// HeatupSimEngine.cs - Core State, Lifecycle, and Physics Dispatch
// ============================================================================
//
// PURPOSE:
//   Top-level simulation coordinator for PWR Cold Shutdown → HZP heatup.
//   All physics state, lifecycle management, and timestep dispatch.
//   No GUI code — the companion HeatupValidationVisual.cs handles all display.
//
// ARCHITECTURE:
//   This is a COORDINATOR, not a physics module.
//   All physics are delegated to modules in Critical.Physics.
//   This engine:
//     - Manages simulation lifecycle (start/stop/pause)
//     - Calls physics modules each timestep in correct order
//     - Reads results from modules into public state fields
//     - Exposes state for GUI/dashboard consumption
//     - Handles time acceleration and frame-rate decoupling
//
//   Partial class files (single responsibility per file):
//     - HeatupSimEngine.Init.cs            : Cold/warm start initialization
//     - HeatupSimEngine.BubbleFormation.cs  : 7-phase bubble formation state machine
//     - HeatupSimEngine.CVCS.cs             : CVCS flow control, RCS inventory, VCT
//     - HeatupSimEngine.Alarms.cs           : Annunciators, RVLIS, edge detection
//     - HeatupSimEngine.Logging.cs          : Event log, history buffers, file output
//     - HeatupSimEngine.HZP.cs              : v1.1.0 HZP stabilization and handoff
//
// PHYSICS MODULES USED (all from Critical.Physics namespace):
//   - WaterProperties      : Steam tables (density, Tsat, Psat, enthalpy)
//   - CoupledThermo        : P-T-V equilibrium solver
//   - ThermalExpansion      : Expansion coefficients, compressibility
//   - ThermalMass          : Heat capacity of RCS metal + fluid
//   - HeatTransfer         : Insulation losses, surge line natural convection
//   - SolidPlantPressure   : Solid pressurizer P-T-V coupling and CVCS control
//   - RCSHeatup            : Isolated and bulk heatup step calculations
//   - VCTPhysics           : Volume Control Tank inventory/boron tracking
//   - CVCSController       : PI level controller, heater control, seal flows
//   - LoopThermodynamics   : T_hot/T_cold calculation
//   - RCPSequencer         : RCP startup timing and requirements
//   - RVLISPhysics         : Reactor Vessel Level Indication System
//   - AlarmManager         : Centralized annunciator setpoint checking
//   - PlantConstants       : Westinghouse 4-Loop reference values
//   - TimeAcceleration     : Dual-clock time warp (wall vs sim time)
//   - SteamDumpController   : v1.1.0 Steam dump for HZP heat removal
//   - HZPStabilizationController : v1.1.0 HZP state machine
//   - SGSecondaryThermal    : v1.1.0 SG secondary steaming detection
//   - RHRSystem             : v3.0.0 RHR thermal model (pump heat, HX, isolation)
//
// GUI COMPANION: HeatupValidationVisual.cs (reads public state, renders dashboard)
//
// PERSISTENCE:
//   v2.0.11: This engine uses DontDestroyOnLoad to persist across scene
//   operations. SceneBridge.cs marks this GameObject persistent on startup.
//   The engine runs continuously regardless of which scene/view is active.
//   Singleton pattern prevents duplicates when scenes load additively.
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Critical.Physics;

/// <summary>
/// Simulation engine for PWR Cold Shutdown → HZP heatup.
/// Coordinates physics modules. Exposes state for dashboard.
/// No inline physics calculations (G3).
/// </summary>
public partial class HeatupSimEngine : MonoBehaviour
{
    enum SGStartupBoundaryStateMode
    {
        OpenPreheat,
        Pressurize,
        Hold,
        IsolatedHeatup
    }

    enum HeaterAuthorityState
    {
        HOLD_LOCKED,
        OFF,
        MANUAL_DISABLED,
        AUTO
    }

    struct DynamicIntervalSample
    {
        public float PrimaryHeatInput_MW;
        public float Pressure_psia;
        public float TopTemp_F;
        public float Tsat_F;
        public bool ActiveHeating;
        public bool IsHoldState;
        public bool IsIsolatedHeatupState;
    }

    // ========================================================================
    // INSPECTOR SETTINGS
    // ========================================================================

    [Header("Simulation Settings")]
    public bool runOnStart = true;

    [Header("Initial Conditions")]
    public float startTemperature = 100f;
    public float startPressure = PlantConstants.PRESSURIZE_INITIAL_PRESSURE_PSIA;
    public float startPZRLevel = 100f;

    [Header("Cold Shutdown Mode (NRC ML11223A342)")]
    [Tooltip("True cold shutdown: SOLID pressurizer (100% water, no steam bubble), low pressure")]
    public bool coldShutdownStart = true;

    [Header("Targets")]
    public float targetTemperature = 557f;
    public float targetPressure = PlantConstants.PZR_OPERATING_PRESSURE_PSIG;

    [Header("Runtime Performance Controls (IP-0023)")]
    [Tooltip("Enable async interval-log file writing while simulation is running.")]
    public bool enableAsyncLogWriter = true;

    [Tooltip("Enable high-frequency debug logs inside simulation substep paths.")]
    public bool enableHighFrequencyPerfLogs = false;

    [Tooltip("Enable worker-thread stepping mode for deterministic validation experiments.")]
    public bool enableWorkerThreadStepping = false;

    [Tooltip("IP-0025 Stage A: route per-step execution through PlantSimulationCoordinator + LegacySimulatorModule.")]
    public bool enableModularCoordinatorPath = false;

    [Tooltip("Enable temporary deep diagnostics for PZR bubble two-phase closure investigations.")]
    public bool enablePzrBubbleDiagnostics = false;

    [Tooltip("Run label attached to PZR bubble diagnostic log lines.")]
    public string pzrBubbleDiagnosticsLabel = "DEFAULT";

    [Tooltip("Diagnostic-only residual tolerance (ft^3) for shadow closure convergence traces.")]
    public float pzrBubbleDiagnosticsResidualTolerance_ft3 = 10f;

    [Tooltip("Diagnostic-only max iterations for shadow closure convergence traces.")]
    public int pzrBubbleDiagnosticsMaxIterations = 8;

    [Tooltip("Enable temporary diagnostics for PZR letdown orifice aggregation investigation.")]
    public bool enablePzrOrificeDiagnostics = false;

    [Tooltip("PZR orifice diagnostics sample stride in simulation ticks.")]
    public int pzrOrificeDiagnosticsSampleStrideTicks = 30;

    // ========================================================================
    // PUBLIC SIMULATION STATE — Read by the visual dashboard
    // All [HideInInspector] fields stay in this file for Unity serialization.
    // ========================================================================

    #region Core Parameters

    [HideInInspector] public float simTime;
    [HideInInspector] public float T_avg, pressure, pzrLevel;
    [HideInInspector] public float T_sat, subcooling;
    [HideInInspector] public float rcpHeat;
    [HideInInspector] public float pzrHeaterPower;
    [HideInInspector] public int rcpCount;
    [HideInInspector] public float gridEnergy;
    [HideInInspector] public float heatupRate;
    [HideInInspector] public int plantMode;
    [HideInInspector] public bool isRunning;
    [HideInInspector] public string statusMessage = "";

    #endregion

    #region Time Acceleration State

    [HideInInspector] public float wallClockTime;
    [HideInInspector] public int currentSpeedIndex;
    [HideInInspector] public bool isAccelerated;

    #endregion

    #region Detailed Instrumentation

    [HideInInspector] public float T_cold;
    [HideInInspector] public float T_hot;
    [HideInInspector] public float T_pzr;
    [HideInInspector] public float T_rcs;
    [HideInInspector] public float T_sg_secondary;  // v0.8.0: SG secondary side temperature (bulk avg)
    [HideInInspector] public float sgHeatTransfer_MW;  // v0.8.0: Heat transfer to SG secondary
    
    // v1.3.0: Multi-node SG model state
    [HideInInspector] public SGMultiNodeState sgMultiNodeState;
    [HideInInspector] public float sgTopNodeTemp;          // Top node temp (°F)
    [HideInInspector] public float sgBottomNodeTemp;       // Bottom node temp (°F)
    [HideInInspector] public float sgStratificationDeltaT;  // Top-Bottom ΔT (°F)
    [HideInInspector] public float sgCirculationFraction;   // [DEPRECATED v3.0.0] Always 0
    [HideInInspector] public bool  sgCirculationActive;     // [DEPRECATED v3.0.0] Always false
    
    // v3.0.0: Thermocline model display state
    [HideInInspector] public float sgThermoclineHeight;     // Thermocline position (ft from tubesheet)
    [HideInInspector] public float sgActiveAreaFraction;    // Fraction of tube area above thermocline
    [HideInInspector] public bool  sgBoilingActive;         // Boiling onset in top node
    
    // v4.3.0: SG secondary pressure model state
    [HideInInspector] public float sgSecondaryPressure_psia;  // SG secondary pressure (psia)
    [HideInInspector] public float sgSaturationTemp_F;        // T_sat at current SG secondary pressure (°F)
    [HideInInspector] public float sgMaxSuperheat_F;          // Max node superheat above T_sat (°F)
    [HideInInspector] public bool  sgNitrogenIsolated;        // N₂ blanket isolation status
    [HideInInspector] public float sgBoilingIntensity;        // Peak boiling intensity fraction (0-1)
    [HideInInspector] public string sgBoundaryMode = "OPEN";  // OPEN / ISOLATED
    [HideInInspector] public string sgPressureSourceBranch = "floor"; // floor / P_sat / inventory-derived
    [HideInInspector] public float sgSteamInventory_lb;       // SG steam inventory (lb)
    [HideInInspector] public string sgStartupBoundaryState = "OPEN_PREHEAT";
    [HideInInspector] public int sgStartupBoundaryStateTicks;
    [HideInInspector] public float sgStartupBoundaryStateTime_hr;
    [HideInInspector] public float sgHoldTargetPressure_psia;
    [HideInInspector] public float sgHoldPressureDeviation_pct;
    [HideInInspector] public float sgHoldNetLeakage_pct;
    [HideInInspector] public bool sgPressurizationWindowActive;
    [HideInInspector] public float sgPressurizationWindowStartTime_hr;
    [HideInInspector] public float sgPressurizationWindowNetPressureRise_psia;
    [HideInInspector] public float sgPressurizationWindowTsatDelta_F;
    [HideInInspector] public int sgPressurizationConsecutivePressureRiseIntervals;
    [HideInInspector] public int sgPressurizationConsecutiveTsatRiseIntervals;
    [HideInInspector] public float sgPreBoilTempApproachToTsat_F;

    // IP-0018 Stage E telemetry fields (DP-0003 deterministic profile)
    [HideInInspector] public string dp0003ValidationProfile = "IP-0018 deterministic startup profile";
    [HideInInspector] public string dp0003BaselineSignature = "";
    [HideInInspector] public float stageE_PrimaryHeatInput_MW;
    [HideInInspector] public float stageE_TotalPrimaryEnergy_MJ;
    [HideInInspector] public float stageE_TotalSGEnergyRemoved_MJ;
    [HideInInspector] public float stageE_PercentMismatch;
    [HideInInspector] public int stageE_EnergySampleCount;
    [HideInInspector] public int stageE_DynamicActiveHeatingIntervalCount;
    [HideInInspector] public int stageE_DynamicPrimaryRiseCheckCount;
    [HideInInspector] public int stageE_DynamicPrimaryRisePassCount;
    [HideInInspector] public int stageE_DynamicPrimaryRiseFailCount;
    [HideInInspector] public int stageE_DynamicTempDelta3WindowCount;
    [HideInInspector] public float stageE_DynamicTempDelta3Last_F;
    [HideInInspector] public float stageE_DynamicTempDelta3Min_F;
    [HideInInspector] public float stageE_DynamicTempDelta3Max_F;
    [HideInInspector] public int stageE_DynamicTempDelta3Above5Count;
    [HideInInspector] public int stageE_DynamicTempDelta3Below2Count;
    [HideInInspector] public int stageE_DynamicPressureFlatline3Count;
    [HideInInspector] public int stageE_DynamicHardClampViolationCount;
    [HideInInspector] public int stageE_DynamicHardClampStreak;
    [HideInInspector] public float stageE_DynamicLastPressureDelta_psia;
    [HideInInspector] public float stageE_DynamicLastTopToTsatDelta_F;
    [HideInInspector] public bool stageE_EnergyWindowActive;
    [HideInInspector] public float stageE_LastSGHeatRemoval_MW;
    [HideInInspector] public int stageE_EnergyNegativeViolationCount;
    [HideInInspector] public int stageE_EnergyOverPrimaryViolationCount;
    [HideInInspector] public float stageE_EnergyMaxOverPrimaryPct;

    // v5.0.0 Stage 4: SG draining & level state
    [HideInInspector] public bool  sgDrainingActive;            // True if SG is actively draining
    [HideInInspector] public bool  sgDrainingComplete;          // True if SG draining completed
    [HideInInspector] public float sgDrainingRate_gpm;          // Draining flow rate (gpm per SG)
    [HideInInspector] public float sgTotalMassDrained_lb;       // Cumulative mass drained (lb)
    [HideInInspector] public float sgSecondaryMass_lb;          // Current SG secondary water mass (lb)
    [HideInInspector] public float sgWideRangeLevel_pct;        // Wide-range level indication (%)
    [HideInInspector] public float sgNarrowRangeLevel_pct;      // Narrow-range level indication (%)

    // v3.0.0: RHR system state
    [HideInInspector] public RHRState rhrState;
    [HideInInspector] public float rhrNetHeat_MW;           // Net RHR thermal effect (+ = heating)
    [HideInInspector] public float rhrHXRemoval_MW;         // RHR HX heat removal
    [HideInInspector] public float rhrPumpHeat_MW;          // RHR pump heat input
    [HideInInspector] public bool  rhrActive;               // RHR connected to RCS
    [HideInInspector] public string rhrModeString = "";     // RHR mode display string
    [HideInInspector] public float pzrWaterVolume;
    [HideInInspector] public float pzrSteamVolume;
    [HideInInspector] public float pzrTotalEnthalpy_BTU;
    [HideInInspector] public float pzrSpecificEnthalpy_BTU_lb;
    [HideInInspector] public float pzrClosureVolumeResidual_ft3;
    [HideInInspector] public float pzrClosureEnergyResidual_BTU;
    [HideInInspector] public bool pzrClosureConverged;
    [HideInInspector] public int pzrClosureSolveAttempts;
    [HideInInspector] public int pzrClosureSolveConverged;
    [HideInInspector] public float pzrClosureConvergencePct;
    [HideInInspector] public int pzrClosureLastIterationCount;
    [HideInInspector] public string pzrClosureLastFailureReason = "NONE";
    [HideInInspector] public string pzrClosureLastConvergencePattern = "UNSET";
    [HideInInspector] public float pzrClosureLastPhaseFraction;
    [HideInInspector] public float pzrClosureBracketMassTarget_lbm;
    [HideInInspector] public float pzrClosureBracketEnthalpyTarget_BTU;
    [HideInInspector] public float pzrClosureBracketVolumeTarget_ft3;
    [HideInInspector] public float pzrClosureBracketPressureGuess_psia;
    [HideInInspector] public float pzrClosureBracketOperatingMin_psia;
    [HideInInspector] public float pzrClosureBracketOperatingMax_psia;
    [HideInInspector] public float pzrClosureBracketHardMin_psia;
    [HideInInspector] public float pzrClosureBracketHardMax_psia;
    [HideInInspector] public float pzrClosureBracketLastLow_psia;
    [HideInInspector] public float pzrClosureBracketLastHigh_psia;
    [HideInInspector] public float pzrClosureBracketResidualLow_ft3;
    [HideInInspector] public float pzrClosureBracketResidualHigh_ft3;
    [HideInInspector] public int pzrClosureBracketResidualSignLow;
    [HideInInspector] public int pzrClosureBracketResidualSignHigh;
    [HideInInspector] public string pzrClosureBracketRegimeLow = "UNSET";
    [HideInInspector] public string pzrClosureBracketRegimeHigh = "UNSET";
    [HideInInspector] public int pzrClosureBracketWindowsTried;
    [HideInInspector] public int pzrClosureBracketValidEvaluations;
    [HideInInspector] public int pzrClosureBracketInvalidEvaluations;
    [HideInInspector] public int pzrClosureBracketNanEvaluations;
    [HideInInspector] public int pzrClosureBracketOutOfRangeEvaluations;
    [HideInInspector] public bool pzrClosureBracketFound;
    [HideInInspector] public string pzrClosureBracketSearchTrace = "";
    [HideInInspector] public float rcsWaterMass;
    [HideInInspector] public float chargingFlow;
    [HideInInspector] public float letdownFlow;
    [HideInInspector] public float surgeFlow;
    [HideInInspector] public float pressureRate;
    [HideInInspector] public float pzrHeatRate;
    [HideInInspector] public float rcsHeatRate;

    // v0.3.2.0 CS-0043: PZR loss values from IsolatedHeatingStep for UpdateDrainPhase
    private float pzrConductionLoss_MW;
    private float pzrInsulationLoss_MW;

    #endregion

    #region RVLIS

    [HideInInspector] public float rvlisDynamic;
    [HideInInspector] public float rvlisFull;
    [HideInInspector] public float rvlisUpper;
    [HideInInspector] public bool rvlisDynamicValid;
    [HideInInspector] public bool rvlisFullValid;
    [HideInInspector] public bool rvlisUpperValid;

    #endregion

    #region VCT State

    [HideInInspector] public VCTPhysics.VCTState vctState;
    [HideInInspector] public float rcsBoronConcentration;
    [HideInInspector] public float massConservationError;

    #endregion

    #region BRS State (Boron Recycle System) — v0.6.0

    [HideInInspector] public BRSState brsState;

    // v5.4.1 Fix B: Canonical MASS-based inventory validation (replaces volume-based _gal fields)
    // Mass is the conserved quantity; volume varies with temperature/pressure.
    [HideInInspector] public float initialSystemMass_lbm;          // At T=0 for conservation baseline
    [HideInInspector] public float totalSystemMass_lbm;            // RCS+PZR+VCT+BRS current total (lbm)
    [HideInInspector] public float externalNetMass_lbm;            // Net external boundary crossings (lbm)
    [HideInInspector] public float massError_lbm;                  // |actual - expected| conservation error
    [HideInInspector] public float plantExternalIn_gal;            // Cumulative true external makeup to tracked plant
    [HideInInspector] public float plantExternalOut_gal;           // Cumulative true external losses from tracked plant
    [HideInInspector] public float plantExternalNet_gal;           // plantExternalIn_gal - plantExternalOut_gal

    // v5.3.0 Stage 6: Primary mass ledger diagnostics (display fields)
    [HideInInspector] public float primaryMassLedger_lb;           // Canonical total primary mass from ledger
    [HideInInspector] public float primaryMassComponents_lb;       // Sum of component masses (RCS+PZR water+steam)
    [HideInInspector] public float primaryMassDrift_lb;            // Ledger minus components
    [HideInInspector] public float primaryMassDrift_pct;           // Drift as percentage of total
    [HideInInspector] public float primaryMassExpected_lb;         // Expected mass from boundary flow integration
    [HideInInspector] public float primaryMassBoundaryError_lb;    // |Ledger - Expected|
    [HideInInspector] public bool  primaryMassConservationOK;      // True if drift within warning threshold
    [HideInInspector] public bool  primaryMassAlarm;               // True if drift exceeds alarm threshold
    [HideInInspector] public string primaryMassStatus = "NOT_CHECKED";  // v0.1.0.0 Phase C: Default until first diagnostic run

    // IP-0016: Primary Boundary Ownership Contract (PBOC) telemetry.
    public struct PrimaryBoundaryFlowEvent
    {
        public int TickIndex;
        public float SimTime_hr;
        public float Dt_hr;
        public int RegimeId;
        public string RegimeLabel;

        public float LetdownFlow_gpm;
        public float ChargingFlow_gpm;
        public float SealInjection_gpm;
        public float SealReturn_gpm;
        public float ChargingToPrimary_gpm;
        public float PrimaryOutflow_gpm;
        public float MakeupExternal_gpm;
        public float Divert_gpm;
        public float CboLoss_gpm;

        public float RhoRcs_lbm_ft3;
        public float RhoAux_lbm_ft3;
        public float MassIn_lbm;
        public float MassOut_lbm;

        public float dm_RCS_lbm;
        public float dm_PZRw_lbm;
        public float dm_PZRs_lbm;
        public float dm_VCT_lbm;
        public float dm_BRS_lbm;
        public float dm_external_lbm;

        public float ExternalIn_gal;
        public float ExternalOut_gal;
        public bool AppliedToComponents;
        public bool AppliedToLedger;
        public bool PairingCheckPass;
    }

    [HideInInspector] public PrimaryBoundaryFlowEvent pbocLastEvent;
    [HideInInspector] public int pbocEventCount;
    [HideInInspector] public int pbocPairingAssertionFailures;

    #endregion

    #region v1.1.0 HZP Systems — Steam Dump, HZP Stabilization, Heater PID

    // Steam Dump Controller State
    [HideInInspector] public SteamDumpState steamDumpState;
    [HideInInspector] public float steamDumpHeat_MW;              // Current steam dump heat removal (MW)
    [HideInInspector] public float steamPressure_psig;            // Steam header pressure (psig)
    [HideInInspector] public bool steamDumpActive;                // True if steam dump removing heat
    
    // HZP Stabilization Controller State
    [HideInInspector] public HZPStabilizationState hzpState;
    [HideInInspector] public bool hzpStable;                      // True if HZP conditions stable
    [HideInInspector] public bool hzpReadyForStartup;             // True if ready for reactor startup
    [HideInInspector] public float hzpProgress;                   // Stabilization progress (0-100%)
    
    // Heater PID Controller State (replaces bang-bang at HZP)
    [HideInInspector] public HeaterPIDState heaterPIDState;
    [HideInInspector] public bool heaterPIDActive;                // True if PID controller is active
    [HideInInspector] public float heaterPIDOutput;               // PID controller output (0-1)
    
    // v4.4.0: Pressurizer Spray System State
    [HideInInspector] public SprayControlState sprayState;
    [HideInInspector] public float sprayFlow_GPM;                 // Current spray flow (gpm)
    [HideInInspector] public float sprayValvePosition;            // Spray valve position (0-1)
    [HideInInspector] public bool sprayActive;                    // True if spray valve open beyond bypass
    [HideInInspector] public float spraySteamCondensed_lbm;       // Steam condensed this step (lbm)
    [HideInInspector] public float netPlantHeat_MW;               // Net plant heat = sources - sinks (MW)
    
    // SG Secondary Steaming State
    [HideInInspector] public bool sgSteaming;                     // True if SG secondary at saturation
    [HideInInspector] public float sgSecondaryPressure_psig;      // SG secondary pressure (psig)
    
    // Handoff State
    [HideInInspector] public bool handoffInitiated;               // True if operator requested handoff
    [HideInInspector] public StartupPrerequisites startupPrereqs; // Current startup prerequisites status

    #endregion

    #region VCT Annunciators

    [HideInInspector] public bool vctLevelLow;
    [HideInInspector] public bool vctLevelHigh;
    [HideInInspector] public bool vctDivertActive;
    [HideInInspector] public bool vctMakeupActive;
    [HideInInspector] public bool vctRWSTSuction;

    #endregion

    #region CVCS Controller State

    [HideInInspector] public CVCSControllerState cvcsControllerState;
    [HideInInspector] public float cvcsIntegralError;
    [HideInInspector] public bool letdownViaRHR = true;
    [HideInInspector] public bool letdownViaOrifice;
    [HideInInspector] public bool letdownIsolatedFlag;
    [HideInInspector] public float orificeLetdownFlow;
    [HideInInspector] public float rhrLetdownFlow;
    [HideInInspector] public float pzrLevelSetpointDisplay = 25f;
    [HideInInspector] public float chargingToRCS;
    [HideInInspector] public float totalCCPOutput;
    [HideInInspector] public float divertFraction;
    [HideInInspector] public float cvcsThermalMixing_MW;
    [HideInInspector] public float cvcsThermalMixingDeltaF;

    #endregion

    #region Annunciator States

    [HideInInspector] public bool pzrHeatersOn;
    [HideInInspector] public bool pzrLevelLow;
    [HideInInspector] public bool pzrLevelHigh;
    [HideInInspector] public bool rcsFlowLow;
    [HideInInspector] public bool chargingActive;
    [HideInInspector] public bool letdownActive;
    [HideInInspector] public bool steamBubbleOK;
    [HideInInspector] public bool sealInjectionOK;
    [HideInInspector] public bool ccwRunning;
    [HideInInspector] public bool[] rcpRunning = new bool[4];
    [HideInInspector] public bool heatupInProgress;
    [HideInInspector] public bool pressureLow;
    [HideInInspector] public bool pressureHigh;
    [HideInInspector] public bool sgSecondaryPressureHigh;
    [HideInInspector] public bool subcoolingLow;
    [HideInInspector] public bool modePermissive;
    [HideInInspector] public bool smmLowMargin;
    [HideInInspector] public bool smmNoMargin;
    [HideInInspector] public bool rvlisLevelLow;

    #endregion

    #region Solid Pressurizer / Bubble Formation State

    [HideInInspector] public bool solidPressurizer = true;
    [HideInInspector] public bool bubbleFormed;
    [HideInInspector] public float bubbleFormationTemp;
    [HideInInspector] public float bubbleFormationTime;

    [HideInInspector] public ColdShutdownProfile coldShutdownProfile;
    [HideInInspector] public bool startupHoldActive;
    [HideInInspector] public float startupHoldReleaseTime_hr;
    [HideInInspector] public bool startupHoldReleaseLogged;
    [HideInInspector] public bool startupHoldActivationLogged;
    [HideInInspector] public float startupHoldStartTime_hr;
    [HideInInspector] public float startupHoldElapsedTime_hr;
    [HideInInspector] public float startupHoldPressureRateAbs_psi_hr;
    [HideInInspector] public bool startupHoldTimeGatePassed;
    [HideInInspector] public bool startupHoldPressureRateGatePassed;
    [HideInInspector] public bool startupHoldStateQualityGatePassed;
    [HideInInspector] public string startupHoldReleaseBlockReason = "NONE";
    [HideInInspector] public string heaterAuthorityState = "AUTO";
    [HideInInspector] public string heaterAuthorityReason = "INIT";
    [HideInInspector] public string heaterLimiterReason = "NONE";
    [HideInInspector] public string heaterLimiterDetail = "NONE";
    [HideInInspector] public bool heaterPressureRateClampActive;
    [HideInInspector] public bool heaterRampRateClampActive;
    [HideInInspector] public bool heaterManualDisabled;
    [HideInInspector] public bool heaterAutoDemandComputeSuppressed;

    // Bubble formation state machine (see HeatupSimEngine.BubbleFormation.cs)
    [HideInInspector] public BubbleFormationPhase bubblePhase = BubbleFormationPhase.NONE;
    [HideInInspector] public float bubblePhaseStartTime;
    [HideInInspector] public float bubbleDrainStartLevel;

    // v0.2.0: CCP tracking during bubble formation drain
    [HideInInspector] public bool ccpStarted;
    [HideInInspector] public float ccpStartTime;
    [HideInInspector] public float ccpStartLevel;

    // v0.2.0: Heater mode tracking
    [HideInInspector] public HeaterMode currentHeaterMode = HeaterMode.STARTUP_FULL_POWER;

    // v2.0.10: Smoothed heater output for bubble formation / pressurize modes
    // Persists between timesteps for rate-limited heater control.
    // Prevents bang-bang oscillation by limiting output change rate.
    [HideInInspector] public float bubbleHeaterSmoothedOutput = 1.0f;

    // v0.2.0: Aux spray test tracking
    [HideInInspector] public bool auxSprayActive;
    [HideInInspector] public float auxSprayStartTime;
    [HideInInspector] public float auxSprayPressureBefore;
    [HideInInspector] public float auxSprayPressureDrop;
    [HideInInspector] public bool auxSprayTestPassed;

    // DP-0002 Group 3: explicit DRAIN gate and reconciliation telemetry.
    [HideInInspector] public float drainSteamDisplacement_lbm;
    [HideInInspector] public float drainCvcsTransfer_lbm;
    [HideInInspector] public float drainDuration_hr;
    [HideInInspector] public float drainExitPressure_psia;
    [HideInInspector] public float drainExitLevel_pct;
    [HideInInspector] public bool drainHardGateTriggered;
    [HideInInspector] public bool drainPressureBandMaintained;
    [HideInInspector] public string drainTransitionReason = "NONE";
    [HideInInspector] public string drainCvcsPolicyMode = "LEGACY_FIXED";
    [HideInInspector] public float drainLetdownFlow_gpm;
    [HideInInspector] public float drainLetdownDemand_gpm;
    [HideInInspector] public float drainChargingFlow_gpm;
    [HideInInspector] public float drainNetOutflowFlow_gpm;
    [HideInInspector] public int drainLineupDemandIndex = 1;
    [HideInInspector] public float drainHydraulicCapacity_gpm;
    [HideInInspector] public float drainHydraulicDeltaP_psi;
    [HideInInspector] public float drainHydraulicDensity_lbm_ft3;
    [HideInInspector] public float drainHydraulicQuality;
    [HideInInspector] public bool drainLetdownSaturated;
    [HideInInspector] public bool drainLineupEventThisStep;
    [HideInInspector] public int drainLineupEventCount;
    [HideInInspector] public float drainLastLineupEventTime_hr = -1f;
    [HideInInspector] public int drainLastLineupPrevIndex = 1;
    [HideInInspector] public int drainLastLineupNewIndex = 1;
    [HideInInspector] public string drainLastLineupTrigger = "NONE";
    [HideInInspector] public string drainLastLineupReason = "NONE";

    private bool drainLineupChangePending;
    private int drainLineupRequestedIndex = 1;
    private string drainLineupRequestedTrigger = "NONE";
    private string drainLineupRequestedReason = "NONE";
    private const float STARTUP_HOLD_PRESSURE_RATE_GATE_PSI_HR = 200f;
    private const float STARTUP_HOLD_STABILITY_WINDOW_SEC = 10f;
    private const float STARTUP_HOLD_BLOCKED_LOG_INTERVAL_SEC = 30f;
    private float startupHoldPressureRateStableAccum_sec;
    private float startupHoldLastStepDt_hr = 1f / 360f;
    private float startupHoldLastBlockedLogTime_hr = -1f;
    private string lastLoggedHeaterAuthorityState = "UNSET";
    private string lastLoggedHeaterLimiterReason = "UNSET";

    // Pre-drain phase flag (DETECTION/VERIFICATION still use solid-plant CVCS)
    private bool bubblePreDrainPhase = false;
    internal bool BubblePreDrainPhaseActive => bubblePreDrainPhase;

    // v5.4.2.0 FF-05 Fix #4: One-time ledger re-baseline after first physics step
    private bool firstStepLedgerBaselined = false;

    // Solid plant state — owned by SolidPlantPressure physics module
    [HideInInspector] public SolidPlantState solidPlantState;

    // Solid plant display values
    [HideInInspector] public float solidPlantPressureSetpoint;
    [HideInInspector] public float solidPlantPressureLow;
    [HideInInspector] public float solidPlantPressureHigh;
    [HideInInspector] public float solidPlantPressureError;
    [HideInInspector] public bool solidPlantPressureInBand;
    [HideInInspector] public float bubbleTargetTemp;
    [HideInInspector] public float timeToBubble;
    [HideInInspector] public string heatupPhaseDesc = "";
    [HideInInspector] public float pzrHeatRateDisplay;
    [HideInInspector] public float estimatedTotalHeatupTime;

    // Long-hold pressure audit state (IP-0019 Stage E Extended harness).
    [HideInInspector] public bool longHoldPressureAuditActive;
    [HideInInspector] public int longHoldPressureWriteCount;
    [HideInInspector] public int longHoldPressureStateDerivedWriteCount;
    [HideInInspector] public int longHoldPressureOverrideAttemptCount;
    [HideInInspector] public int longHoldPressureBlockedOverrideCount;
    [HideInInspector] public bool longHoldPressureInvariantFailed;
    [HideInInspector] public string longHoldPressureInvariantReason = "";
    [HideInInspector] public float longHoldLastPressureBefore;
    [HideInInspector] public float longHoldLastPressureAfter;
    [HideInInspector] public string longHoldLastPressureSource = "NONE";
    [HideInInspector] public string longHoldLastPressureStack = "";
    [HideInInspector] public int longHoldPressureTickCount;
    [HideInInspector] public string longHoldPressureRegime = "UNSET";
    [HideInInspector] public string longHoldPressureEquationBranch = "UNSET";
    [HideInInspector] public bool longHoldPressureUsesSaturation;
    [HideInInspector] public float longHoldPressureSaturationPsia;
    [HideInInspector] public float longHoldPressureModelDensity;
    [HideInInspector] public float longHoldPressureModelCompressibility;
    [HideInInspector] public float longHoldPressureModelDeltaPsi;

    #region RTCC State (IP-0016)

    [HideInInspector] public int rtccTransitionCount;
    [HideInInspector] public int rtccAssertionFailureCount;
    [HideInInspector] public float rtccMaxRawDeltaAbs_lbm;
    [HideInInspector] public float rtccLastPreMass_lbm;
    [HideInInspector] public float rtccLastReconstructedMass_lbm;
    [HideInInspector] public float rtccLastRawDelta_lbm;
    [HideInInspector] public float rtccLastPostMass_lbm;
    [HideInInspector] public float rtccLastAssertDelta_lbm;
    [HideInInspector] public string rtccLastTransition = "NONE";
    [HideInInspector] public string rtccLastAuthorityFrom = "UNSET";
    [HideInInspector] public string rtccLastAuthorityTo = "UNSET";
    [HideInInspector] public bool rtccLastAssertPass = true;
    [HideInInspector] public bool rtccTelemetryPresent;

    #endregion

    #endregion

    #region History Buffers — Read by visual for graphing

    [HideInInspector] public List<float> tempHistory = new List<float>();
    [HideInInspector] public List<float> pressHistory = new List<float>();
    [HideInInspector] public List<float> timeHistory = new List<float>();
    [HideInInspector] public List<float> pzrLevelHistory = new List<float>();
    [HideInInspector] public List<float> subcoolHistory = new List<float>();
    [HideInInspector] public List<float> heatRateHistory = new List<float>();
    [HideInInspector] public List<float> chargingHistory = new List<float>();
    [HideInInspector] public List<float> letdownHistory = new List<float>();
    [HideInInspector] public List<float> vctLevelHistory = new List<float>();
    [HideInInspector] public List<float> surgeFlowHistory = new List<float>();
    [HideInInspector] public List<float> tHotHistory = new List<float>();
    [HideInInspector] public List<float> tColdHistory = new List<float>();
    [HideInInspector] public List<float> tSgSecondaryHistory = new List<float>();  // v0.8.0: SG secondary temperature history
    [HideInInspector] public List<float> brsHoldupHistory = new List<float>();
    [HideInInspector] public List<float> brsDistillateHistory = new List<float>();
    
    // v0.9.0: Critical missing history buffers for proper graph display
    [HideInInspector] public List<float> tPzrHistory = new List<float>();         // Pressurizer temperature
    [HideInInspector] public List<float> tSatHistory = new List<float>();         // Saturation temperature
    [HideInInspector] public List<float> pressureRateHistory = new List<float>(); // Pressure rate of change
    
    // v1.1.0: HZP stabilization history buffers
    [HideInInspector] public List<float> steamDumpHeatHistory = new List<float>();     // Steam dump heat removal
    [HideInInspector] public List<float> steamPressureHistory = new List<float>();     // Steam header pressure
    [HideInInspector] public List<float> heaterPIDOutputHistory = new List<float>();   // Heater PID output
    [HideInInspector] public List<float> hzpProgressHistory = new List<float>();       // HZP progress
    
    // v4.4.0: Spray system history
    [HideInInspector] public List<float> sprayFlowHistory = new List<float>();          // Spray flow (gpm)

    #endregion

    #region Event Log — Read by visual dashboard

    [HideInInspector] public List<EventLogEntry> eventLog = new List<EventLogEntry>();
    const int MAX_EVENT_LOG = 200;

    #endregion

    // ========================================================================
    // PRIVATE STATE
    // ========================================================================

    private SystemState physicsState;
    private float totalSystemMass;
    private string logPath;

    // v0.4.0 Issue #3: Per-pump ramp-up tracking
    private float[] rcpStartTimes = new float[4] { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };
    [HideInInspector] public RCPContribution rcpContribution;  // Aggregate ramp state for display
    [HideInInspector] public float effectiveRCPHeat;            // Ramped RCP heat (MW) fed to physics

    // Constants — shared across partials
    const float MAX_RATE = 50f;  // Tech Spec limit °F/hr
    const float PZR_HEATER_POWER_MW = PlantConstants.HEATER_POWER_TOTAL / 1000f;  // 1800 kW = 1.8 MW
    const float DP0003_DETERMINISTIC_TIMESTEP_HR = 1f / 360f;  // 10-second deterministic physics step
    const float DP0003_INTERVAL_LOG_HR = 0.25f;                // 15-minute deterministic log period
    const int SG_STARTUP_MIN_PRESSURIZE_INTERVALS = 2;
    const int SG_STARTUP_MIN_HOLD_INTERVALS = 2;
    const float SG_HOLD_PRESSURE_BAND_PCT = 3f;
    const float SG_PRESSURIZE_MIN_GAIN_PCT = 2f;
    const float SG_HOLD_MAX_CUM_LEAKAGE_PCT = 0.2f;

    SGStartupBoundaryStateMode sgStartupStateMode = SGStartupBoundaryStateMode.OpenPreheat;
    int sgStartupStateIntervalCount = 0;
    float sgStartupIntervalAccumulator_hr = 0f;
    float sgHoldCumulativeLeakage_pct = 0f;
    float sgHoldBaselineSecondaryMass_lb = 0f;
    bool sgPressurizationWindowStarted = false;
    bool sgBoilingTransitionObserved = false;
    float sgPressurizationWindowStartPressure_psia = 0f;
    float sgPressurizationWindowStartTsat_F = 0f;
    float sgPressurizationLastPressure_psia = 0f;
    float sgPressurizationLastTsat_F = 0f;
    readonly DynamicIntervalSample[] stageEDynamicWindow = new DynamicIntervalSample[3];
    int stageEDynamicWindowCount = 0;
    int stageEDynamicWindowHead = 0;
    bool stageEDynamicPrevIntervalValid = false;
    float stageEDynamicPrevPrimaryHeatInput_MW = 0f;
    float stageEDynamicPrevPressure_psia = 0f;

    // v0.9.5: Shutdown flag for immediate termination without blocking
    private volatile bool _shutdownRequested = false;

    // IP-0016 PBOC: tick-scoped boundary-flow event state.
    private int pbocTickIndex = 0;
    private PrimaryBoundaryFlowEvent pbocCurrentEvent;
    private bool pbocEventActiveThisTick = false;

    // Flag retained as UpdateRCSInventory guard so no secondary boundary
    // mutation path can apply outside PBOC.
    private bool regime3CVCSPreApplied = false;

    // v5.3.0 Stage 6: Mass alarm edge detection state
    private bool _previousMassAlarmState = false;
    private bool _previousMassConservationOK = true;

    // v5.4.0 Stage 0: Mass diagnostics control
    [HideInInspector] public bool enableMassDiagnostics = false;
    [HideInInspector] public int diagSampleIntervalFrames = 10;
    private int _diagFrameCounter = 0;
    private float deliveredRcpHeat_MW = 0f;
    [HideInInspector] public float noRcpTransportFactor = 1f;
    [HideInInspector] public string thermoStateWriter = "UNSET";
    private float smoothedRegime2Alpha = 0f;
    private int previousPhysicsRegimeId = 0;
    private string previousPhysicsRegimeLabel = "UNSET";
    private float nextRegime2ConvergenceWarnTime_hr = 0f;
    private float nextRegime3ConvergenceWarnTime_hr = 0f;
    private float nextR1MassAuditWarnTime_hr = 0f;
    private float nextPbocPairingWarnTime_hr = 0f;
    [HideInInspector] public int hotPathWarningSuppressedCount = 0;

    // v4.4.0: Letdown orifice lineup state.
    // Per NRC HRTD 4.1: Three parallel orifices (2×75 + 1×45 gpm).
    // Operator adjusts lineup based on PZR level trend during heatup.
    // Normal lineup: one 75-gpm orifice. During thermal expansion,
    // operator opens additional orifices to control PZR level.
    [HideInInspector] public int orifice75Count = 1;      // 75-gpm orifices open (1-2)
    [HideInInspector] public bool orifice45Open = false;   // 45-gpm orifice open
    [HideInInspector] public string orificeLineupDesc = "1×75 gpm";  // Display string
    private int pzrOrificeDiagTickCounter = 0;
    private int pzrOrificeDiagLast75Count = -1;
    private bool pzrOrificeDiagLast45Open = false;

    // RCP startup sequence timing (hours)
    const float RCP1_START_TIME = 1.0f;
    const float RCP_START_INTERVAL = 0.5f;
    const float RCP_HEAT_DELIVERY_TAU_HR = 0.05f;   // 3 min thermal uptake smoothing
    const float NO_RCP_TRANSPORT_GAIN = 18f;        // Flow turnover to bulk-transport coupling
    const float NO_RCP_NATURAL_FLOOR = 0.08f;       // Natural convection floor when no forced circulation
    const float RHR_ISOLATION_NEAR_TEMP_BAND_F = 5f;
    const float REGIME2_MAX_PZR_LEVEL_STEP_PCT = 0.5f;
    const float REGIME2_STARTUP_LEVEL_STEP_MIN_PCT = 0.02f;
    const float REGIME2_ALPHA_SMOOTH_TAU_HR = 0.03f;
    const float RCP_TRANSIENT_TRACE_WINDOW_HR = 0.35f;
    const float STRESS_FORCE_PRESSURE_MAX_STEP_PSI = 10f;
    const float HOT_PATH_WARNING_MIN_INTERVAL_SEC = 60f;

    // v0.8.2: History cap: 1-minute samples, 240-minute (4-hour) rolling window
    const int MAX_HISTORY = 240;

    // Log file interval tracking
    [HideInInspector] public int logCount;
    [HideInInspector] public float lastLogSimTime;

    // ========================================================================
    // PUBLIC ACCESSORS
    // ========================================================================

    public float MaxRate => MAX_RATE;
    public string LogPath => logPath;

    // ========================================================================
    // TIME ACCELERATION CONTROL — Called by the visual dashboard
    // ========================================================================

    /// <summary>
    /// Change time warp speed. Called by dashboard dropdown or keyboard shortcut.
    /// Speed index maps to TimeAcceleration.SpeedSteps: 0=1x, 1=2x, 2=4x, 3=8x, 4=10x
    /// </summary>
    public void SetTimeAcceleration(int speedIndex)
    {
        TimeAcceleration.SetSpeed(speedIndex);
        currentSpeedIndex = TimeAcceleration.CurrentSpeedIndex;
        isAccelerated = !TimeAcceleration.IsRealTime;

        string label = TimeAcceleration.SpeedLabelsShort[currentSpeedIndex];
        LogEvent(EventSeverity.INFO, $"TIME WARP: {label}");
        Debug.Log($"[TimeAccel] Speed set to {label} ({TimeAcceleration.CurrentMultiplier}x)");
    }

    // ========================================================================
    // LIFECYCLE
    // ========================================================================

    /// <summary>
    /// v2.0.11: Singleton enforcement. Prevents duplicate engines when
    /// Validator scene loads additively while MainScene's engine persists.
    /// DontDestroyOnLoad is handled by SceneBridge for the shared GameObject,
    /// but if the engine is on its own GameObject, it marks itself persistent.
    /// </summary>
    void Awake()
    {
        // Check for existing instance
        HeatupSimEngine[] instances = FindObjectsOfType<HeatupSimEngine>();
        if (instances.Length > 1)
        {
            // Another engine already exists (the persistent one) — destroy this duplicate
            Debug.Log($"[HeatupSimEngine] Duplicate detected — destroying this instance on '{gameObject.name}'");
            Destroy(gameObject);
            return;
        }

        // If not already marked DontDestroyOnLoad (e.g. not on SceneBridge's object),
        // mark ourselves persistent so the engine survives scene operations
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[HeatupSimEngine] Marked DontDestroyOnLoad on '{gameObject.name}'");
        }
    }

    void Start()
    {
        Application.runInBackground = true;

        // Log to project root (not AppData) for external tool access
        logPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "HeatupLogs");
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);
        else
            ClearLogDirectory();  // v0.8.2: Clean up old logs on startup

        if (runOnStart)
            StartSimulation();
    }

    /// <summary>
    /// v0.8.2: Clear all existing log files from the HeatupLogs directory on startup.
    /// Since these are validation logs only, starting fresh each session is appropriate.
    /// </summary>
    void ClearLogDirectory()
    {
        try
        {
            string[] files = Directory.GetFiles(logPath, "*.txt");
            int count = 0;
            foreach (string file in files)
            {
                File.Delete(file);
                count++;
            }
            if (count > 0)
                Debug.Log($"[HeatupSimEngine] Cleared {count} old log files from {logPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HeatupSimEngine] Failed to clear log directory: {e.Message}");
        }
    }

    /// <summary>
    /// v0.9.5: Called when the application is quitting. Requests immediate shutdown
    /// using the new shutdown flag to prevent freeze on exit.
    /// </summary>
    void OnApplicationQuit()
    {
        Debug.Log("[HeatupSimEngine] OnApplicationQuit - requesting immediate shutdown");
        RequestImmediateShutdown();
    }

    /// <summary>
    /// v0.9.5: Called when this MonoBehaviour is destroyed. Ensures cleanup even
    /// if destroyed before OnApplicationQuit (e.g., scene unload).
    /// </summary>
    void OnDestroy()
    {
        Debug.Log("[HeatupSimEngine] OnDestroy - requesting immediate shutdown");
        RequestImmediateShutdown();
    }

    /// <summary>
    /// v0.9.5: Called when the MonoBehaviour is disabled.
    /// </summary>
    void OnDisable()
    {
        Debug.Log("[HeatupSimEngine] OnDisable - requesting immediate shutdown");
        RequestImmediateShutdown();
    }

    /// <summary>
    /// v0.9.2: Force immediate simulation stop. Stops all coroutines first,
    /// then sets the flag. This prevents any chance of the coroutine continuing
    /// to run after we've signaled stop.
    /// </summary>
    void ForceStop()
    {
        // Stop coroutines FIRST to prevent any further execution
        StopAllCoroutines();
        // Then set the flag
        isRunning = false;
        _shutdownRequested = true;
    }

    /// <summary>
    /// v0.9.5: Request immediate shutdown. Sets the shutdown flag BEFORE stopping
    /// coroutines to ensure the coroutine sees the flag on its next check.
    /// This prevents the coroutine from continuing during the yield gap.
    /// </summary>
    public void RequestImmediateShutdown()
    {
        Debug.Log("[HeatupSimEngine] IMMEDIATE SHUTDOWN REQUESTED");
        // Set flag FIRST so coroutine sees it immediately
        _shutdownRequested = true;
        isRunning = false;
        _plantSimulationCoordinator?.Shutdown();
        // IP-0023: bounded best-effort flush avoids exit-time blocking spikes.
        FlushAsyncLogWriter(500);
        // Then stop coroutines
        StopAllCoroutines();
    }

    [ContextMenu("Start Simulation")]
    public void StartSimulation()
    {
        if (isRunning) return;
        StartCoroutine(RunSimulation());
    }

    [ContextMenu("Stop Simulation")]
    public void StopSimulation()
    {
        Debug.Log("[HeatupSimEngine] StopSimulation called");
        RequestImmediateShutdown();
    }

    // ========================================================================
    // MAIN SIMULATION COROUTINE — Frame-rate decoupled
    // Initialization delegated to HeatupSimEngine.Init.cs
    // ========================================================================

    IEnumerator RunSimulation()
    {
        isRunning = true;
        _shutdownRequested = false;  // v0.9.5: Reset shutdown flag at start

        // Initialize — delegates to Init partial
        InitializeSimulation();
        PublishTelemetrySnapshot();

        // ================================================================
        // FRAME-RATE DECOUPLED SIMULATION LOOP
        // ================================================================
        float dt = DP0003_DETERMINISTIC_TIMESTEP_HR;
        float intervalLogHr = DP0003_INTERVAL_LOG_HR;
        float prevTemp = T_rcs;
        float rateTimer = 0f;
        float historyTimer = 0f;
        float logTimer = 0f;
        logCount = 0;
        lastLogSimTime = 0f;
        float simTimeBudget = 0f;

        // v5.4.1 Audit Fix: Emit Interval_000 at t=0.00 for cold-start observability.
        // Captures initial pressure, CVCS state, and audit baseline before any physics steps.
        SaveIntervalLog();  // logCount 0→1, creates Interval_001_0.00hr.txt
        // Note: Interval numbering starts at 001 (SaveIntervalLog increments internally).
        // Subsequent interval logs continue from current logCount.

        // v5.4.1 Audit Fix: Startup burst timer for pressure ramp observability.
        // Emits concise pressure/CVCS state every 60 sim-seconds for the first 15 minutes.
        float startupBurstTimer = 0f;

        // v0.9.5: Check shutdown flag in addition to isRunning for immediate exit
        while (isRunning && !_shutdownRequested && T_rcs < targetTemperature - 2f)
        {
            // Tick the time acceleration module
            TimeAcceleration.Tick();

            // Feed sim time budget from the time acceleration module
            simTimeBudget += TimeAcceleration.SimDeltaTime_Hours;

            // Cap budget to prevent runaway after alt-tab or lag spike
            simTimeBudget = Mathf.Min(simTimeBudget, 5f / 60f);

            // Sync public time state for the visual dashboard
            wallClockTime = TimeAcceleration.WallClockTime_Hours;
            currentSpeedIndex = TimeAcceleration.CurrentSpeedIndex;
            isAccelerated = !TimeAcceleration.IsRealTime;

            int stepsThisFrame = 0;
            int maxStepsPerFrame = 50;

            while (simTimeBudget >= dt && stepsThisFrame < maxStepsPerFrame)
            {
                // v0.9.5: Check for shutdown at each physics step
                if (_shutdownRequested) break;

                RunPhysicsStep(dt);
                simTimeBudget -= dt;
                stepsThisFrame++;

                // v5.4.1: Authoritative startup burst log.
                // Every 60 sim-seconds for the first 30 minutes (0.5 hr).
                // Shows all values the controller actually uses + heater correlation.
                if (simTime < 0.5f)
                {
                    startupBurstTimer += dt;
                    float burstInterval = 1f / 60f;  // 1 sim-minute
                    if (enableHighFrequencyPerfLogs && startupBurstTimer >= burstInterval)
                    {
                        float simSec = simTime * 3600f;
                        float pSP = solidPlantState.PressureSetpoint;
                        float dpSP = pressure - pSP;
                        string mode = solidPlantState.ControlMode ?? "N/A";
                        float netCVCS = chargingFlow - letdownFlow;
                        float netCmd = solidPlantState.LetdownAdjustCmd;
                        float netEff = solidPlantState.LetdownAdjustEff;
                        bool slewClamp = solidPlantState.SlewClampActive;
                        float pFilt = solidPlantState.PressureFiltered;
                        float pzrT = solidPlantState.T_pzr;
                        float pzrDThr = solidPlantState.PzrHeatRate;
                        Debug.Log($"[STARTUP T+{simSec:F0}s] P={pressure:F1}psia  P_filt={pFilt:F1}  " +
                                  $"SP={pSP:F0}  ΔP={dpSP:+F1;-F1}  " +
                                  $"Mode={mode}  " +
                                  $"PZR_T={pzrT:F1}°F  dT={pzrDThr:F1}°F/hr  " +
                                  $"Htr={pzrHeaterPower:F3}MW  " +
                                  $"Chg={chargingFlow:F1}  Ltd={letdownFlow:F1}  Net={netCVCS:+F1;-F1}gpm  " +
                                  $"NetCmd={netCmd:+F2;-F2}  NetEff={netEff:+F2;-F2}  Slew={slewClamp}");
                        startupBurstTimer = 0f;
                    }
                }

                rateTimer += dt;
                if (rateTimer >= 0.25f)
                {
                    prevTemp = T_rcs;
                    rateTimer = 0f;
                }

                // v0.9.5: Faster history collection at start for responsive graphs
                // First 5 sim-minutes: sample every 10 sim-seconds (1/360 hr = 10 sec)
                // After: sample every 1 sim-minute (existing behavior)
                float historyInterval = simTime < (5f / 60f) ? dt : (1f / 60f);
                historyTimer += dt;
                if (historyTimer >= historyInterval)
                {
                    AddHistory();
                    historyTimer = 0f;
                }

                // v0.7.1: Save detailed log file every 15 sim-minutes (was 30 min)
                logTimer += dt;
                if (logTimer >= intervalLogHr)
                {
                    SaveIntervalLog();
                    logCount++;
                    lastLogSimTime = simTime;
                    logTimer = 0f;
                    if (enableHighFrequencyPerfLogs)
                    {
                        Debug.Log($"[T+{simTime:F2}hr] T_avg={T_avg:F1}°F, P={pressure:F0}psia, Subcool={subcooling:F1}°F, PZR={pzrLevel:F1}%, RCPs={rcpCount}, Rate={heatupRate:F1}°F/hr");
                    }
                }

                if (!isRunning || _shutdownRequested || T_rcs >= targetTemperature - 2f)
                    break;
            }

            // v0.9.5: Check for shutdown before yielding
            if (_shutdownRequested) break;

            yield return null;
        }

        // v0.9.5: Only save report if not shutting down (prevents I/O blocking on exit)
        if (!_shutdownRequested)
        {
            SaveReport();
        }
        else
        {
            Debug.Log("[HeatupSimEngine] Skipping SaveReport due to shutdown request");
        }

        // v0.9.5: Exit immediately if shutdown requested
        while (isRunning && !_shutdownRequested)
        {
            yield return null;
        }
        
        Debug.Log("[HeatupSimEngine] Coroutine exiting cleanly");
    }

    // ========================================================================
    // PHYSICS STEP — Dispatches to physics modules and partial class methods
    // ========================================================================

    void StepSimulation(float dt)
    {
        pbocTickIndex++;
        pbocEventActiveThisTick = false;
        regime3CVCSPreApplied = false;
        startupHoldLastStepDt_hr = dt;
        int prevRcpCountStep = rcpCount;
        float prevRcpHeatStep = rcpHeat;
        float prevPzrLevelStep = pzrLevel;
        float prevNoRcpTransportStep = noRcpTransportFactor;
        float prevCouplingAlphaStep = smoothedRegime2Alpha;

        // ================================================================
        // 1. RCP STARTUP SEQUENCE — Delegated to RCPSequencer module
        //    v0.4.0 Issue #3: Tracks per-pump start times for staged ramp-up.
        //    Pre-seeds PI controller at each new pump start.
        // ================================================================
        {
            int targetRcpCount = RCPSequencer.GetTargetRCPCount(
                bubbleFormed && !solidPressurizer, simTime, bubbleFormationTime, pressure);

            if (targetRcpCount > rcpCount)
            {
                // Record start time for each new pump and pre-seed PI controller
                for (int p = rcpCount; p < targetRcpCount; p++)
                {
                    rcpStartTimes[p] = simTime;
                    LogEvent(EventSeverity.ACTION, $"RCP #{p + 1} START COMMAND at T_pzr={T_pzr:F1}°F, T_rcs={T_rcs:F1}°F (ramping ~{PlantConstants.RCP_TOTAL_RAMP_DURATION_HR * 60:F0} min)");
                    Debug.Log($"[T+{simTime:F2}hr] === RCP #{p + 1} START COMMAND === T_pzr={T_pzr:F1}°F, T_rcs={T_rcs:F1}°F, ΔT={T_pzr - T_rcs:F1}°F");
                }

                rcpCount = targetRcpCount;

                // v0.4.0 Issue #3 Part C: Pre-seed PI controller at each RCP start
                // Scales with temperature differential for proportional recovery bias
                CVCSController.PreSeedForRCPStart(ref cvcsControllerState, pzrLevel, T_avg, rcpCount);
            }
            else if (targetRcpCount < rcpCount)
            {
                rcpCount = targetRcpCount;
            }

            if (rcpCount == 0 && bubbleFormed && !solidPressurizer)
            {
                var seqState = RCPSequencer.GetState(true, simTime, bubbleFormationTime, pressure, rcpCount);
                statusMessage = seqState.StatusMessage;
            }

            // v0.4.0 Issue #3: Calculate effective ramped RCP contribution
            rcpContribution = RCPSequencer.GetEffectiveRCPContribution(rcpStartTimes, simTime, rcpCount);
            effectiveRCPHeat = rcpContribution.EffectiveHeat_MW;
        }

        // ================================================================
        // 1B. HEATER CONTROL — Must run BEFORE physics so throttled
        //     power feeds into IsolatedHeatingStep / BulkHeatupStep.
        //     Uses pressureRate from PREVIOUS timestep (one-step lag is
        //     physically reasonable — real controllers have 1–3 s sensor
        //     and signal processing delay).
        //     Fix for Issue #1: Heater throttle output was previously
        //     calculated AFTER physics, so physics always consumed the
        //     full 1.8 MW regardless of controller output.
        //
        //     v4.4.0: Added PRESSURIZE_AUTO → AUTOMATIC_PID transition.
        //     When pressure reaches ~2200 psia, the 20% minimum power
        //     floor is no longer appropriate. PID control takes over
        //     with proportional + backup heater staging at 2235 psig.
        // ================================================================
        if (!IsLegacyPressurizerControlBypassedForCoordinatorStep())
        {
            UpdateStartupHoldState();
            heaterPressureRateClampActive = false;
            heaterRampRateClampActive = false;
            heaterAutoDemandComputeSuppressed = false;
            heaterLimiterReason = "NONE";
            heaterLimiterDetail = "NONE";

            float pzrLevelSetpointForHeater;
            if (solidPressurizer || bubblePreDrainPhase)
                pzrLevelSetpointForHeater = 100f;
            else
                pzrLevelSetpointForHeater = PlantConstants.GetPZRLevelSetpointUnified(T_avg);

            bool letdownIsolatedForHeater = (pzrLevel < PlantConstants.PZR_LOW_LEVEL_ISOLATION);
            HeaterAuthorityState effectiveAuthority = ResolveHeaterAuthorityState();
            string requestedMode = currentHeaterMode.ToString();

            // ==============================================================
            // v4.4.0: MODE TRANSITION — PRESSURIZE_AUTO → AUTOMATIC_PID
            // Per NRC HRTD 10.2: Normal pressure control uses PID with
            // proportional + backup heater groups at 2235 psig setpoint.
            // Transition occurs when pressure reaches operating band.
            // ==============================================================
            if (effectiveAuthority == HeaterAuthorityState.AUTO &&
                currentHeaterMode == HeaterMode.PRESSURIZE_AUTO &&
                pressure >= PlantConstants.HEATER_MODE_TRANSITION_PRESSURE_PSIA)
            {
                currentHeaterMode = HeaterMode.AUTOMATIC_PID;

                // Initialize PID controller at current pressure
                float currentPressure_psig = pressure - PlantConstants.PSIG_TO_PSIA;
                heaterPIDState = CVCSController.InitializeHeaterPID(currentPressure_psig);
                heaterPIDActive = true;

                LogEvent(EventSeverity.ACTION,
                    $"HEATER MODE: PRESSURIZE_AUTO → AUTOMATIC_PID at {pressure:F0} psia ({currentPressure_psig:F0} psig)");
                LogEvent(EventSeverity.INFO,
                    $"PID heater control active — setpoint {PlantConstants.PZR_OPERATING_PRESSURE_PSIG:F0} psig");
            }

            // ==============================================================
            // HEATER POWER CALCULATION
            // Mode determines which controller runs:
            //   AUTOMATIC_PID: PID controller with prop/backup staging
            //   All others: CalculateHeaterState (rate-modulated or fixed)
            // ==============================================================
            if (effectiveAuthority == HeaterAuthorityState.HOLD_LOCKED)
            {
                heaterAutoDemandComputeSuppressed = true;
                pzrHeaterPower = 0f;
                pzrHeatersOn = false;
                heaterPIDOutput = 0f;
                heaterPIDActive = false;
                heaterLimiterReason = "HOLD_LOCKED";
                heaterLimiterDetail = "Startup hold authority active";
            }
            else if (effectiveAuthority == HeaterAuthorityState.OFF)
            {
                heaterAutoDemandComputeSuppressed = true;
                pzrHeaterPower = 0f;
                pzrHeatersOn = false;
                heaterPIDOutput = 0f;
                heaterPIDActive = false;
                heaterLimiterReason = "MODE_OFF";
                heaterLimiterDetail = "Heater mode OFF";
            }
            else if (effectiveAuthority == HeaterAuthorityState.MANUAL_DISABLED)
            {
                heaterAutoDemandComputeSuppressed = true;
                pzrHeaterPower = 0f;
                pzrHeatersOn = false;
                heaterPIDOutput = 0f;
                heaterPIDActive = false;
                heaterLimiterReason = "MANUAL_DISABLED";
                heaterLimiterDetail = "Manual disable lockout";
            }
            else if (currentHeaterMode == HeaterMode.AUTOMATIC_PID)
            {
                // v4.4.0: PID heater control for normal operations
                float currentP_psig = pressure - PlantConstants.PSIG_TO_PSIA;
                pzrHeaterPower = CVCSController.UpdateHeaterPID(
                    ref heaterPIDState, currentP_psig, pzrLevel, dt);
                pzrHeatersOn = (pzrHeaterPower > 0.001f);
                heaterPIDOutput = heaterPIDState.SmoothedOutput;
                heaterPIDActive = true;
                heaterLimiterReason = "PID_CONTROL";
                heaterLimiterDetail = "AUTOMATIC_PID active";
            }
            else
            {
                var heaterState = CVCSController.CalculateHeaterState(
                    pzrLevel, pzrLevelSetpointForHeater, letdownIsolatedForHeater,
                    solidPressurizer || bubblePreDrainPhase, PZR_HEATER_POWER_MW,
                    currentHeaterMode, pressureRate,
                    dt, bubbleHeaterSmoothedOutput);  // v2.0.10: pass dt and smoothed state
                pzrHeaterPower = heaterState.HeaterPower_MW;
                pzrHeatersOn = heaterState.HeatersEnabled;
                heaterPressureRateClampActive = heaterState.PressureRateLimited;
                heaterRampRateClampActive = heaterState.RampRateLimited;

                if (heaterState.TrippedByInterlock)
                {
                    heaterLimiterReason = "LOW_LEVEL_INTERLOCK";
                    heaterLimiterDetail = heaterState.StatusReason;
                }
                else if (heaterState.PressureRateLimited)
                {
                    heaterLimiterReason = "PRESSURE_RATE_CLAMP";
                    heaterLimiterDetail =
                        $"abs_dPdt={heaterState.PressureRateAbsPsiHr:F1} psi/hr " +
                        $"target={heaterState.TargetFraction * 100f:F1}% output={heaterState.HeaterFraction * 100f:F1}%";
                }
                else if (heaterState.RampRateLimited)
                {
                    heaterLimiterReason = "HEATER_RAMP_LIMIT";
                    heaterLimiterDetail =
                        $"target={heaterState.TargetFraction * 100f:F1}% output={heaterState.HeaterFraction * 100f:F1}%";
                }
                else if (solidPressurizer &&
                    string.Equals(solidPlantState.ControlMode, "HOLD_SOLID", StringComparison.Ordinal) &&
                    solidPlantPressureInBand)
                {
                    heaterLimiterReason = "SOLID_HOLD_BAND";
                    heaterLimiterDetail = "Solid pressure control in HOLD_SOLID with in-band pressure";
                }
                else
                {
                    heaterLimiterReason = "NONE";
                    heaterLimiterDetail = heaterState.StatusReason ?? "No active limiter";
                }

                // v2.0.10: Persist smoothed output for next timestep
                if (currentHeaterMode == HeaterMode.BUBBLE_FORMATION_AUTO ||
                    currentHeaterMode == HeaterMode.PRESSURIZE_AUTO)
                {
                    bubbleHeaterSmoothedOutput = heaterState.SmoothedOutput;
                }
            }

            UpdateHeaterAuthorityTelemetry(requestedMode, effectiveAuthority);
        }

        // ================================================================
        // 1B-SPRAY: PRESSURIZER SPRAY SYSTEM (v4.4.0)
        //     Spray modulates above 2260 psig to condense PZR steam.
        //     Uses T_cold from PREVIOUS timestep (1-step lag, same
        //     approach as pressureRate — physically reasonable).
        //     Spray system enabled when heater PID is active (AUTOMATIC_PID).
        //     Per NRC HRTD 10.2: spray and heaters are coordinated by
        //     the same master pressure controller.
        // ================================================================
        {
            float currentP_psig_spray = pressure - PlantConstants.PSIG_TO_PSIA;
            CVCSController.UpdateSpray(
                ref sprayState,
                currentP_psig_spray,
                T_pzr,
                T_cold,  // Previous timestep T_cold (1-step lag)
                pzrSteamVolume,
                pressure,
                rcpCount,
                dt);
            
            // Log spray activation event (one-time, check BEFORE syncing)
            if (sprayState.IsActive && !sprayActive)
            {
                LogEvent(EventSeverity.ACTION,
                    $"SPRAY ACTIVATED at {pressure:F0} psia — valve {sprayState.ValvePosition * 100:F0}%");
            }
            
            // Sync display fields
            sprayFlow_GPM = sprayState.SprayFlow_GPM;
            sprayValvePosition = sprayState.ValvePosition;
            sprayActive = sprayState.IsActive;
            spraySteamCondensed_lbm = sprayState.SteamCondensed_lbm;
        }

        // v0.4.0 Issue #3: Use effective ramped heat, not binary full heat.
        // IP-0019 (CS-0031): smooth delivered heat to avoid one-step thermal shocks
        // at startup-stage transitions while preserving staged sequencer intent.
        float heatAlpha = Mathf.Clamp01(dt / RCP_HEAT_DELIVERY_TAU_HR);
        deliveredRcpHeat_MW += (effectiveRCPHeat - deliveredRcpHeat_MW) * heatAlpha;
        rcpHeat = deliveredRcpHeat_MW;
        gridEnergy += (rcpCount * 6f + pzrHeaterPower + 25f) * dt;

        float prevT_pzr = T_pzr;
        float prevT_rcs = T_rcs;
        float prevPressure = pressure;

        // ================================================================
        // 1C. RHR SYSTEM UPDATE (v3.0.0)
        //     RHR runs during cold shutdown before RCPs take over.
        //     Provides pump heat (~1 MW) and minimal HX cooling during heatup.
        //     Auto-isolates when RCS pressure exceeds 585 psig.
        //     After isolation, net thermal effect is zero.
        // ================================================================
        {
            float P_rcs_psig = pressure - PlantConstants.PSIG_TO_PSIA;
            var rhrResult = RHRSystem.Update(ref rhrState, T_rcs, P_rcs_psig, rcpCount, dt);
            
            rhrNetHeat_MW = rhrResult.NetHeatEffect_MW;
            rhrHXRemoval_MW = rhrResult.HeatRemoval_MW;
            rhrPumpHeat_MW = rhrResult.PumpHeatInput_MW;
            rhrActive = rhrResult.IsActive;
            rhrModeString = rhrState.Mode.ToString();
            
            // IP-0019 (CS-0056): begin RHR isolation only after all 4 RCPs are
            // running and the startup temperature is near the 350F transition.
            if (ShouldBeginRhrIsolation(T_rcs))
            {
                RHRSystem.BeginIsolation(ref rhrState);
                LogEvent(EventSeverity.ACTION,
                    $"RHR ISOLATION INITIATED - 4 RCPs established near {PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F:F0}F window; P={P_rcs_psig:F0} psig");
            }
        }

        // ================================================================
        // 2. PHYSICS CALCULATIONS — Using actual modules
        // ================================================================
        float pzrWaterMass_lb = pzrWaterVolume * WaterProperties.WaterDensity(T_pzr, pressure);
        float pzrHeatCap = ThermalMass.FluidHeatCapacity(pzrWaterMass_lb, T_pzr, pressure);

        float rcsWaterMass_lb = physicsState.RCSWaterMass;
        float rcsHeatCap = ThermalMass.RCSHeatCapacity(
            PlantConstants.RCS_METAL_MASS,
            rcsWaterMass_lb,
            T_rcs,
            pressure);

        float heatLoss_MW = HeatTransfer.InsulationHeatLoss_MW(T_rcs);

        // ================================================================
        // v0.4.0 Issue #3: Three-regime physics model
        //   Regime 1: No RCPs (flow fraction = 0) — PZR/RCS thermally isolated
        //   Regime 2: RCPs ramping (0 < α < 1) — blended isolated/coupled
        //   Regime 3: All started pumps fully running (α = 1) — fully coupled
        //
        // Coupling factor α = min(1.0, totalFlowFraction) represents the
        // degree of thermal coupling between PZR and RCS via surge line.
        // This smoothly transitions the physics from isolated to coupled
        // over ~40 min per pump, preventing convergence discontinuities.
        // ================================================================

        // Coupling factor: 0 = fully isolated, 1 = fully coupled.
        // IP-0019 transient stabilization: smooth raw RCP flow fraction to avoid
        // startup coupling discontinuities that can amplify pressure/level transients.
        float alphaRaw = Mathf.Clamp01(rcpContribution.TotalFlowFraction);
        float alpha = ComputeRegime2CouplingAlpha(alphaRaw, dt);
        noRcpTransportFactor = 1f;
        LogPhysicsRegimeTransitionIfNeeded(alpha);

        if (rcpCount == 0 || alpha < 0.001f)
        {
            // ============================================================
            // REGIME 1: No RCPs — PZR and RCS thermally isolated
            // ============================================================

            // v3.0.0: Update SG model even with no RCPs (for display state)
            // Heat transfer is negligible (HTC_NO_RCPS ≈ 8), but this keeps
            // thermocline and boiling state current for the dashboard.
            ApplySGBoundaryAuthority();
            var sgResult_r1 = SGMultiNodeThermal.Update(ref sgMultiNodeState, T_rcs, 0, pressure, dt);
            T_sg_secondary = sgMultiNodeState.BulkAverageTemp_F;
            sgHeatTransfer_MW = sgMultiNodeState.TotalHeatAbsorption_MW;
            sgTopNodeTemp = sgMultiNodeState.TopNodeTemp_F;
            sgBottomNodeTemp = sgMultiNodeState.BottomNodeTemp_F;
            sgStratificationDeltaT = sgMultiNodeState.StratificationDeltaT_F;
            sgThermoclineHeight = sgMultiNodeState.ThermoclineHeight_ft;
            sgActiveAreaFraction = sgMultiNodeState.ActiveAreaFraction;
            sgBoilingActive = sgMultiNodeState.BoilingActive;
            // v4.3.0: SG secondary pressure model
            sgSecondaryPressure_psia = sgResult_r1.SecondaryPressure_psia;
            sgSaturationTemp_F = sgResult_r1.SaturationTemp_F;
            sgMaxSuperheat_F = sgMultiNodeState.MaxSuperheat_F;
            sgNitrogenIsolated = sgResult_r1.NitrogenIsolated;
            sgBoilingIntensity = sgResult_r1.BoilingIntensity;
            UpdateSGBoundaryDiagnostics(sgResult_r1, dt);
            // v5.4.2: SG draining/level display sync (forensic fix — 7 fields)
            sgDrainingActive       = sgMultiNodeState.DrainingActive;
            sgDrainingComplete     = sgMultiNodeState.DrainingComplete;
            sgDrainingRate_gpm     = sgMultiNodeState.DrainingRate_gpm;
            sgTotalMassDrained_lb  = sgMultiNodeState.TotalMassDrained_lb;
            sgSecondaryMass_lb     = sgMultiNodeState.SecondaryWaterMass_lb;
            sgWideRangeLevel_pct   = sgMultiNodeState.WideRangeLevel_pct;
            sgNarrowRangeLevel_pct = sgMultiNodeState.NarrowRangeLevel_pct;

            if ((solidPressurizer && !bubbleFormed) || bubblePreDrainPhase)
            {
                // SOLID PRESSURIZER (or pre-drain bubble phases)
                // Delegated to SolidPlantPressure module
                float regime1TransportFactor = ComputeNoRcpBulkTransportFactor(dt);
                noRcpTransportFactor = regime1TransportFactor;
                bool explicitNoFlowHold =
                    rcpCount == 0 &&
                    Mathf.Abs(chargingFlow) <= 1e-4f &&
                    Mathf.Abs(letdownFlow) <= 1e-4f &&
                    Mathf.Abs(chargingToRCS) <= 1e-4f &&
                    !pzrHeatersOn &&
                    Mathf.Abs(pzrHeaterPower) <= 1e-6f &&
                    rhrState.Mode == RHRMode.Standby;
                float solidBaseLetdown = explicitNoFlowHold ? 0f : 75f;
                float solidBaseCharging = explicitNoFlowHold ? 0f : 75f;
                SolidPlantPressure.Update(
                    ref solidPlantState,
                    pzrHeaterPower * 1000f,   // MW -> kW
                    solidBaseLetdown,
                    solidBaseCharging,
                    rcsHeatCap, dt,
                    regime1TransportFactor);

                // Read module outputs into local candidates, then commit once.
                float nextT_pzr = solidPlantState.T_pzr;
                float nextT_rcs = solidPlantState.T_rcs;
                float nextPressure = solidPlantState.Pressure;

                // IP-0019 (CS-0055): no-RCP bulk transport gating for RHR thermal path.
                if (rhrActive && rcsHeatCap > 1f)
                {
                    nextT_rcs += ComputeNoRcpHeatDeltaF(rhrNetHeat_MW, rcsHeatCap, dt);
                    solidPlantState.T_rcs = nextT_rcs;
                }

                T_pzr = nextT_pzr;
                T_rcs = nextT_rcs;
                SetLongHoldPressureModelTrace(
                    "REGIME1_SOLID",
                    solidPlantState.PressureEquationBranch,
                    solidPlantState.PressureModelUsesSaturation,
                    solidPlantState.PressureModelSaturationPsia,
                    solidPlantState.PressureModelDensity,
                    solidPlantState.PressureModelCompressibility,
                    solidPlantState.PressureDelta_psi);
                ApplyPressureWrite(nextPressure, "REGIME1_SOLID_SolidPlantPressure", stateDerived: true);
                letdownFlow = solidPlantState.LetdownFlow;
                chargingFlow = solidPlantState.ChargingFlow;
                surgeFlow = solidPlantState.SurgeFlow;

                // RCS INVENTORY UPDATE — CVCS net flow during solid ops
                float rho_rcs = WaterProperties.WaterDensity(T_rcs, pressure);
                ApplyPrimaryBoundaryFlowPBOC(dt, rho_rcs, 0, "SOLID");
                rcsWaterMass = physicsState.RCSWaterMass;

                // v5.4.1 Audit Fix Stage 1: Update SOLID canonical fields every tick.
                // Single source of truth: PZRWaterMass is already maintained by Init and
                // is invariant during solid ops (no surge transfer changes it here).
                physicsState.PZRWaterMassSolid = physicsState.PZRWaterMass;
                physicsState.TotalPrimaryMassSolid = physicsState.RCSWaterMass + physicsState.PZRWaterMassSolid;
                physicsState.TotalPrimaryMass_lb = physicsState.TotalPrimaryMassSolid;

                // Update display values from physics module
                T_sat = solidPlantState.T_sat;
                bubbleTargetTemp = solidPlantState.T_sat;
                solidPlantPressureError = solidPlantState.PressureError;
                solidPlantPressureInBand = solidPlantState.InControlBand;
                timeToBubble = SolidPlantPressure.EstimateTimeToBubble(solidPlantState);

                // Check for bubble formation (delegated to BubbleFormation partial)
                ProcessBubbleDetection();
                AssignThermoWriterByInvariants();
            }
            else
            {
                // ============================================================
                // REGIME 1: Bubble exists, no RCPs — Isolated PZR heating
                // Delegated to RCSHeatup.IsolatedHeatingStep()
                //
                // ---- v0.3.0.0 Phase B: AUTHORITY OWNERSHIP (CS-0026/0029/0030) ----
                // Field Authority:
                //   TotalPrimaryMass_lb  → ENGINE (mutated only by CVCS boundary flows)
                //   RCSWaterMass         → PHYSICS (BubbleFormation state machine)
                //   PZRWaterMass/Steam   → PHYSICS (BubbleFormation state machine)
                // Order of Operations:
                //   1. IsolatedHeatingStep → thermal updates (T_pzr, T_rcs, surgeFlow)
                //      v0.3.2.0: When two-phase active, PZR dT bypassed (CS-0043 fix)
                //   2. CVCS boundary flow → mutate TotalPrimaryMass_lb
                //   3. BubbleFormation state machine → mutate component masses (step 3 below)
                //   4. Post-step assertion → component sum vs ledger
                // ============================================================

                // v0.3.2.0 CS-0043: Determine two-phase state BEFORE calling IsolatedHeatingStep.
                // When two-phase is active, IsolatedHeatingStep bypasses PZR sensible heat
                // (heater energy goes to latent heat in UpdateDrainPhase instead).
                bool twoPhaseActive = bubblePhase == BubbleFormationPhase.DRAIN
                                   || bubblePhase == BubbleFormationPhase.STABILIZE
                                   || bubblePhase == BubbleFormationPhase.PRESSURIZE;
                float regime1TransportFactor = ComputeNoRcpBulkTransportFactor(dt);
                noRcpTransportFactor = regime1TransportFactor;

                var isoResult = RCSHeatup.IsolatedHeatingStep(
                    T_pzr, T_rcs, pressure,
                    pzrHeaterPower, pzrWaterVolume,
                    pzrHeatCap, rcsHeatCap, dt,
                    twoPhaseActive,
                    regime1TransportFactor,
                    pzrSteamVolume);

                float nextT_pzr = isoResult.T_pzr;
                float nextT_rcs = isoResult.T_rcs;
                float nextPressure = isoResult.Pressure;
                float nextSurgeFlow = isoResult.SurgeFlow;

                // v0.3.2.0 CS-0043: Store PZR loss values for UpdateDrainPhase energy accounting
                pzrConductionLoss_MW = isoResult.PZRConductionLoss_MW;
                pzrInsulationLoss_MW = isoResult.PZRInsulationLoss_MW;

                // v0.3.2.0 CS-0043: Psat override REMOVED.
                // Previously (v0.3.0.0 Fix 3.2), two-phase pressure was set to Psat(T_pzr).
                // This created a ratchet: conduction/insulation losses pulled T_pzr below
                // T_sat(P_old), so Psat(T_pzr) < P_old — monotonic decline.
                // With the two-phase bypass, IsolatedHeatingStep returns T_pzr = T_sat(P)
                // and P unchanged. Psat(T_sat(P)) = P (identity, no ratchet).
                // Pressure during two-phase is now stable by construction.

                T_sat = WaterProperties.SaturationTemperature(nextPressure);

                // IP-0019 (CS-0055): no-RCP bulk transport gating for RHR thermal path.
                if (rhrActive && rcsHeatCap > 1f)
                {
                    nextT_rcs += ComputeNoRcpHeatDeltaF(rhrNetHeat_MW, rcsHeatCap, dt);
                }

                T_pzr = nextT_pzr;
                T_rcs = nextT_rcs;
                SetLongHoldPressureModelTrace(
                    "REGIME1_ISOLATED",
                    string.IsNullOrWhiteSpace(isoResult.PressureEquationBranch)
                        ? (twoPhaseActive
                            ? "R1_ISOLATED_TWO_PHASE_LOCKED_PSAT(T)"
                            : "R1_ISOLATED_SUBCOOLED_dP=PressureCoeff(T,P)*dT*0.5")
                        : isoResult.PressureEquationBranch,
                    isoResult.PressureModelUsesSaturation,
                    isoResult.PressureModelSaturationPsia,
                    isoResult.PressureModelDensity,
                    isoResult.PressureModelCompressibility,
                    isoResult.PressureDeltaPsi);
                ApplyPressureWrite(nextPressure, "REGIME1_ISOLATED_RCSHeatup", stateDerived: true);
                surgeFlow = nextSurgeFlow;
                AssignThermoWriterByInvariants();

                // ============================================================
                // IP-0016 PBOC: Primary boundary flow single-owner application.
                // Compute once and apply to both component authority (RCS) and
                // ledger/accumulators using the same signed event values.
                // ============================================================
                {
                    float rho_rcs_r1 = WaterProperties.WaterDensity(T_rcs, pressure);
                    ApplyPrimaryBoundaryFlowPBOC(dt, rho_rcs_r1, 1, "R1_TWO_PHASE_ISOLATED");
                }

                // Debug heat balance periodically
                if (enableHighFrequencyPerfLogs && (simTime < 0.02f || (simTime % 0.25f < dt)))
                {
                    float netHeat_MW = isoResult.ConductionHeat_MW - heatLoss_MW;
                    Debug.Log($"[Phase1 Heat Balance] T_pzr={T_pzr:F1}°F, T_rcs={T_rcs:F1}°F, DeltaT={T_pzr - T_rcs:F2}°F");
                    Debug.Log($"  Surge line natural convection: {isoResult.ConductionHeat_MW * 1000:F3} kW");
                    Debug.Log($"  RCS insulation heat loss: {heatLoss_MW * 1000:F3} kW");
                    Debug.Log($"  Net heat to RCS: {netHeat_MW * 1000:F3} kW ({(netHeat_MW >= 0 ? "warming" : "cooling")})");
                }
            }
        }
        else if (!rcpContribution.AllFullyRunning)
        if (!IsLegacyPressurizerControlBypassedForCoordinatorStep())
        {
            // ============================================================
            // REGIME 2: RCPs Ramping — Blended isolated/coupled physics
            // v0.4.0 Issue #3: Runs BOTH physics paths and blends results
            // by coupling factor α, smoothly transitioning from isolated
            // to coupled over ~40 min as pumps ramp up.
            //
            // v0.9.6 FIX: Must sync physicsState PZR volumes from engine state
            // BEFORE calling BulkHeatupStep. Without this, CoupledThermo uses
            // stale/uninitialized PZR state and computes wildly incorrect volumes.
            // This caused PZR level to crash from 25% to 5% on RCP start.
            //
            // ---- v0.1.0.0 Phase D: AUTHORITY OWNERSHIP (CS-0004) ----
            // Field Authority:
            //   TotalPrimaryMass_lb  → ENGINE (mutated only by CVCS boundary flows)
            //   RCSWaterMass         → SOLVER (derived as remainder = Total - PZR)
            //   PZRWaterMass/Steam   → SOLVER (distributed by CoupledThermo)
            // Order of Operations:
            //   1. Rebase ledger (first step only)
            //   2. CVCS boundary flow → mutate TotalPrimaryMass_lb
            //   3. Spray condensation (internal PZR transfer)
            //   4. BulkHeatupStep → CoupledThermo distributes canonical mass
            //   5. Blend with isolated result by α
            // ============================================================

            float prevPzrLevel_pct = pzrLevel;

            // --- Run isolated path (weighted by 1-α) ---
            var isoResult = RCSHeatup.IsolatedHeatingStep(
                T_pzr, T_rcs, pressure,
                pzrHeaterPower, pzrWaterVolume,
                pzrHeatCap, rcsHeatCap, dt,
                pzrSteamVolume: pzrSteamVolume);

            // --- Run coupled path (weighted by α) ---
            // Use effective ramped heat, not full binary heat
            physicsState.Temperature = T_rcs;
            physicsState.Pressure = pressure;
            
            // IP-0016 PBOC/CS-0050: Sync PZR volumes for solver continuity
            // without re-deriving canonical masses from V×ρ here.
            physicsState.PZRWaterVolume = pzrWaterVolume;
            physicsState.PZRSteamVolume = pzrSteamVolume;

            // v1.3.0: Update multi-node SG model BEFORE RCS physics step
            ApplySGBoundaryAuthority();
            var sgResult_r2 = SGMultiNodeThermal.Update(
                ref sgMultiNodeState, T_rcs, rcpCount, pressure, dt);

            // ============================================================
            // v0.1.0.0 Phase A: Rebase canonical ledger BEFORE any CVCS mutation (initial authority seed)
            // On the first coupled step, seed TotalPrimaryMass_lb from component sum.
            // This MUST occur before CVCS ledger mutation so the ledger starts from
            // a clean component-sum baseline.
            // ============================================================
            if (!firstStepLedgerBaselined)
            {
                float actualTotal_r2 = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
                physicsState.TotalPrimaryMass_lb = actualTotal_r2;
                physicsState.InitialPrimaryMass_lb = actualTotal_r2;
                firstStepLedgerBaselined = true;
            }

            // ============================================================
            // IP-0016 PBOC: Apply boundary event before coupled solve.
            // Same event values drive component and ledger updates.
            // ============================================================
            {
                float rho_rcs_r2 = WaterProperties.WaterDensity(T_rcs, pressure);
                ApplyPrimaryBoundaryFlowPBOC(dt, rho_rcs_r2, 2, "R2_BLEND");
            }

            // ============================================================
            // v4.4.0: SPRAY CONDENSATION — Apply BEFORE CoupledThermo solver
            // Spray condenses PZR steam, transferring mass from steam to water.
            // This directly reduces pressure by shrinking the steam bubble.
            // Must be applied before solver so it computes equilibrium with
            // the reduced steam mass.
            // ============================================================
            if (sprayState.SteamCondensed_lbm > 0f)
            {
                physicsState.PZRSteamMass -= sprayState.SteamCondensed_lbm;
                physicsState.PZRWaterMass += sprayState.SteamCondensed_lbm;
                if (physicsState.PZRSteamMass < 0f) physicsState.PZRSteamMass = 0f;
                
                // Update volumes from masses
                float rhoSteam_r2 = WaterProperties.SaturatedSteamDensity(pressure);
                float rhoWater_r2 = WaterProperties.WaterDensity(
                    WaterProperties.SaturationTemperature(pressure), pressure);
                if (rhoSteam_r2 > 0.01f)
                    physicsState.PZRSteamVolume = physicsState.PZRSteamMass / rhoSteam_r2;
                if (rhoWater_r2 > 0.1f)
                    physicsState.PZRWaterVolume = physicsState.PZRWaterMass / rhoWater_r2;
            }

            // INV-1: Ledger is sole mass authority in coupled regimes
            var coupledResult = RCSHeatup.BulkHeatupStep(
                ref physicsState, rcpCount, effectiveRCPHeat,
                pzrHeaterPower, rcsHeatCap, pzrHeatCap, dt,
                sgResult_r2.TotalHeatRemoval_MW, sgMultiNodeState.BulkAverageTemp_F,
                physicsState.TotalPrimaryMass_lb);

            // --- Blend results using coupling factor α ---
            float oneMinusAlpha = 1.0f - alpha;
            // IP-0019 transient stabilization: early startup coupling is nonlinear.
            // Low RCP flow fractions should not immediately force full PZR/RCS mass exchange.
            float alphaMass = alpha * alpha;
            float oneMinusAlphaMass = 1.0f - alphaMass;

            T_rcs = isoResult.T_rcs * oneMinusAlpha + coupledResult.T_rcs * alpha;
            float blendedPressure = isoResult.Pressure * oneMinusAlphaMass + coupledResult.Pressure * alphaMass;
            SetLongHoldPressureModelTrace(
                "REGIME2_BLEND",
                "R2_BLEND_P=iso*(1-alpha^2)+coupled*alpha^2",
                false,
                WaterProperties.SaturationPressure(T_pzr),
                WaterProperties.WaterDensity(T_rcs, pressure),
                ThermalExpansion.Compressibility(T_rcs, pressure),
                blendedPressure - pressure);
            ApplyPressureWrite(
                blendedPressure,
                "REGIME2_BLEND_WeightedPressure",
                stateDerived: true);
            surgeFlow = isoResult.SurgeFlow * oneMinusAlpha + coupledResult.SurgeFlow * alpha;
            AssignThermoWriterByInvariants();

            // PZR temperature: blend between isolated T_pzr and T_sat (coupled)
            T_sat = WaterProperties.SaturationTemperature(pressure);
            T_pzr = isoResult.T_pzr * oneMinusAlpha + T_sat * alpha;

            // v0.9.6 FIX: Use incremental change blending for PZR volume instead of
            // absolute blending. This prevents discontinuities at regime transitions.
            // 
            // Old (problematic): pzrWaterVolume = pzrWaterVolume * (1-α) + physicsState.PZRWaterVolume * α
            // This blended between current volume and a "target" that could be very different.
            //
            // New: Blend the DELTA (change) from each physics path, then apply to current volume.
            // Isolated path produces minimal PZR volume change (thermal expansion only).
            // Coupled path produces PZR volume change from CoupledThermo solver.
            float pzrVolumeBefore = pzrWaterVolume;  // Volume at start of this timestep
            float deltaPZR_isolated = 0f;  // Isolated regime: PZR volume essentially unchanged
            float deltaPZR_coupled = physicsState.PZRWaterVolume - pzrVolumeBefore;  // Change from coupled solver
            float deltaPZR_blended = deltaPZR_isolated * oneMinusAlphaMass + deltaPZR_coupled * alphaMass;
            
            pzrWaterVolume = pzrVolumeBefore + deltaPZR_blended;
            pzrSteamVolume = PlantConstants.PZR_TOTAL_VOLUME - pzrWaterVolume;
            pzrLevel = pzrWaterVolume / PlantConstants.PZR_TOTAL_VOLUME * 100f;
            pzrLevel = ClampRegime2PzrLevelStep(prevPzrLevel_pct, pzrLevel, alpha);
            pzrWaterVolume = pzrLevel * 0.01f * PlantConstants.PZR_TOTAL_VOLUME;
            pzrSteamVolume = PlantConstants.PZR_TOTAL_VOLUME - pzrWaterVolume;

            // v1.3.0/v3.0.0: Update SG display state from multi-node model
            T_sg_secondary = sgMultiNodeState.BulkAverageTemp_F;
            sgHeatTransfer_MW = sgResult_r2.TotalHeatRemoval_MW;
            sgTopNodeTemp = sgMultiNodeState.TopNodeTemp_F;
            sgBottomNodeTemp = sgMultiNodeState.BottomNodeTemp_F;
            sgStratificationDeltaT = sgMultiNodeState.StratificationDeltaT_F;
            sgCirculationFraction = sgMultiNodeState.CirculationFraction;   // deprecated, always 0
            sgCirculationActive = sgMultiNodeState.CirculationActive;       // deprecated, always false
            sgThermoclineHeight = sgMultiNodeState.ThermoclineHeight_ft;
            sgActiveAreaFraction = sgMultiNodeState.ActiveAreaFraction;
            sgBoilingActive = sgMultiNodeState.BoilingActive;
            // v4.3.0: SG secondary pressure model
            sgSecondaryPressure_psia = sgResult_r2.SecondaryPressure_psia;
            sgSaturationTemp_F = sgResult_r2.SaturationTemp_F;
            sgMaxSuperheat_F = sgMultiNodeState.MaxSuperheat_F;
            sgNitrogenIsolated = sgResult_r2.NitrogenIsolated;
            sgBoilingIntensity = sgResult_r2.BoilingIntensity;
            UpdateSGBoundaryDiagnostics(sgResult_r2, dt);
            // v5.4.2: SG draining/level display sync (forensic fix — 7 fields)
            sgDrainingActive       = sgMultiNodeState.DrainingActive;
            sgDrainingComplete     = sgMultiNodeState.DrainingComplete;
            sgDrainingRate_gpm     = sgMultiNodeState.DrainingRate_gpm;
            sgTotalMassDrained_lb  = sgMultiNodeState.TotalMassDrained_lb;
            sgSecondaryMass_lb     = sgMultiNodeState.SecondaryWaterMass_lb;
            sgWideRangeLevel_pct   = sgMultiNodeState.WideRangeLevel_pct;
            sgNarrowRangeLevel_pct = sgMultiNodeState.NarrowRangeLevel_pct;

            // Update physicsState to reflect blended result for downstream consumers
            physicsState.Temperature = T_rcs;
            physicsState.Pressure = pressure;
            physicsState.PZRWaterVolume = pzrWaterVolume;
            physicsState.PZRSteamVolume = pzrSteamVolume;
            // PZRLevel is a computed property (PZRWaterVolume / PZR_TOTAL_VOLUME * 100)
            // — automatically correct once PZRWaterVolume is set above
            rcsWaterMass = physicsState.RCSWaterMass;

            if (!coupledResult.Converged &&
                ShouldEmitHotPathWarning(ref nextRegime2ConvergenceWarnTime_hr))
            {
                Debug.LogWarning($"[T+{simTime:F2}hr] CoupledThermo did not converge during blended regime");
            }

            // Status display for ramping regime
            if (rcpCount < 4)
            {
                float rcpBaseTime = bubbleFormationTime + RCP1_START_TIME;
                float timeToNext = RCP_START_INTERVAL - ((simTime - rcpBaseTime) % RCP_START_INTERVAL);
                statusMessage = $"RCPs: {rcpCount}/4 RAMPING (α={alpha:F2}) - NEXT IN {timeToNext * 60:F0} MIN";
                heatupPhaseDesc = $"RCS HEATUP - {rcpCount} RCPs RAMPING ({effectiveRCPHeat:F1}/{rcpCount * PlantConstants.RCP_HEAT_MW_EACH:F1} MW)";
            }
            else
            {
                statusMessage = $"ALL 4 RCPs RAMPING (α={alpha:F2}, {rcpContribution.PumpsFullyRunning}/4 at rated)";
                heatupPhaseDesc = $"HEATUP - 4 RCPs RAMPING ({effectiveRCPHeat:F1}/{PlantConstants.RCP_HEAT_MW:F0} MW)";
            }

            if (heatupRate > 0.5f)
                estimatedTotalHeatupTime = simTime + (targetTemperature - T_rcs) / heatupRate;
            else
                estimatedTotalHeatupTime = simTime;
        }
        else
        {
            // ============================================================
            // REGIME 3: All Started Pumps Fully Running — Full CoupledThermo
            // v0.9.6: Added PZR state sync for consistency with REGIME 2 fix
            //
            // ---- v0.1.0.0 Phase D: AUTHORITY OWNERSHIP (CS-0004) ----
            // Field Authority:
            //   TotalPrimaryMass_lb  → ENGINE (mutated only by CVCS boundary flows)
            //   RCSWaterMass         → SOLVER (derived as remainder = Total - PZR)
            //   PZRWaterMass/Steam   → SOLVER (distributed by CoupledThermo)
            // Order of Operations:
            //   1. Rebase ledger (first step only)
            //   2. CVCS boundary flow → mutate TotalPrimaryMass_lb
            //   3. Spray condensation (internal PZR transfer)
            //   4. BulkHeatupStep → CoupledThermo distributes canonical mass
            // ============================================================
            physicsState.Temperature = T_rcs;
            physicsState.Pressure = pressure;
            
            // IP-0016 PBOC/CS-0050: Sync PZR volumes for solver continuity
            // without re-deriving canonical masses from V×ρ here.
            physicsState.PZRWaterVolume = pzrWaterVolume;
            physicsState.PZRSteamVolume = pzrSteamVolume;

            // v1.3.0: Update multi-node SG model BEFORE RCS physics step
            ApplySGBoundaryAuthority();
            var sgResult_r3 = SGMultiNodeThermal.Update(
                ref sgMultiNodeState, T_rcs, rcpCount, pressure, dt);

            // ============================================================
            // v0.1.0.0 Phase A: Rebase canonical ledger BEFORE any CVCS mutation (initial authority seed)
            // On the first coupled step, seed TotalPrimaryMass_lb from component sum.
            // This MUST occur before CVCS ledger mutation so the ledger starts from
            // a clean component-sum baseline.
            // ============================================================
            if (!firstStepLedgerBaselined)
            {
                float actualTotal_r3 = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
                physicsState.TotalPrimaryMass_lb = actualTotal_r3;
                physicsState.InitialPrimaryMass_lb = actualTotal_r3;
                firstStepLedgerBaselined = true;
            }

            // ============================================================
            // IP-0016 PBOC: Apply boundary event before coupled solve.
            // Same event values drive component and ledger updates.
            // ============================================================
            {
                float rho_rcs_r3 = WaterProperties.WaterDensity(T_rcs, pressure);
                ApplyPrimaryBoundaryFlowPBOC(dt, rho_rcs_r3, 3, "R3_COUPLED");
            }

            // ============================================================
            // v4.4.0: SPRAY CONDENSATION — Apply BEFORE CoupledThermo solver
            // Spray condenses PZR steam, transferring mass from steam to water.
            // This directly reduces pressure by shrinking the steam bubble.
            // The CoupledThermo solver then computes new equilibrium P/T/V
            // with the reduced steam mass, naturally lowering pressure.
            // ============================================================
            if (sprayState.SteamCondensed_lbm > 0f)
            {
                physicsState.PZRSteamMass -= sprayState.SteamCondensed_lbm;
                physicsState.PZRWaterMass += sprayState.SteamCondensed_lbm;
                if (physicsState.PZRSteamMass < 0f) physicsState.PZRSteamMass = 0f;
                
                // Update volumes from masses
                float rhoSteam_r3 = WaterProperties.SaturatedSteamDensity(pressure);
                float rhoWater_r3 = WaterProperties.WaterDensity(
                    WaterProperties.SaturationTemperature(pressure), pressure);
                if (rhoSteam_r3 > 0.01f)
                    physicsState.PZRSteamVolume = physicsState.PZRSteamMass / rhoSteam_r3;
                if (rhoWater_r3 > 0.1f)
                    physicsState.PZRWaterVolume = physicsState.PZRWaterMass / rhoWater_r3;
            }

            // INV-1: Ledger is sole mass authority in coupled regimes
            var heatupResult = RCSHeatup.BulkHeatupStep(
                ref physicsState, rcpCount, rcpHeat,
                pzrHeaterPower, rcsHeatCap, pzrHeatCap, dt,
                sgResult_r3.TotalHeatRemoval_MW, sgMultiNodeState.BulkAverageTemp_F,
                physicsState.TotalPrimaryMass_lb);

            T_rcs = heatupResult.T_rcs;
            SetLongHoldPressureModelTrace(
                "REGIME3_COUPLED",
                "R3_COUPLED_CoupledThermo",
                true,
                WaterProperties.SaturationPressure(T_pzr),
                WaterProperties.WaterDensity(T_rcs, pressure),
                ThermalExpansion.Compressibility(T_rcs, pressure),
                heatupResult.Pressure - pressure);
            ApplyPressureWrite(heatupResult.Pressure, "REGIME3_COUPLED_BulkHeatup", stateDerived: true);
            surgeFlow = heatupResult.SurgeFlow;
            AssignThermoWriterByInvariants();

            // v1.3.0/v3.0.0: Update SG display state from multi-node model
            T_sg_secondary = sgMultiNodeState.BulkAverageTemp_F;
            sgHeatTransfer_MW = sgResult_r3.TotalHeatRemoval_MW;
            sgTopNodeTemp = sgMultiNodeState.TopNodeTemp_F;
            sgBottomNodeTemp = sgMultiNodeState.BottomNodeTemp_F;
            sgStratificationDeltaT = sgMultiNodeState.StratificationDeltaT_F;
            sgCirculationFraction = sgMultiNodeState.CirculationFraction;   // deprecated, always 0
            sgCirculationActive = sgMultiNodeState.CirculationActive;       // deprecated, always false
            sgThermoclineHeight = sgMultiNodeState.ThermoclineHeight_ft;
            sgActiveAreaFraction = sgMultiNodeState.ActiveAreaFraction;
            sgBoilingActive = sgMultiNodeState.BoilingActive;
            // v4.3.0: SG secondary pressure model
            sgSecondaryPressure_psia = sgResult_r3.SecondaryPressure_psia;
            sgSaturationTemp_F = sgResult_r3.SaturationTemp_F;
            sgMaxSuperheat_F = sgMultiNodeState.MaxSuperheat_F;
            sgNitrogenIsolated = sgResult_r3.NitrogenIsolated;
            sgBoilingIntensity = sgResult_r3.BoilingIntensity;
            UpdateSGBoundaryDiagnostics(sgResult_r3, dt);
            // v5.4.2: SG draining/level display sync (forensic fix — 7 fields)
            sgDrainingActive       = sgMultiNodeState.DrainingActive;
            sgDrainingComplete     = sgMultiNodeState.DrainingComplete;
            sgDrainingRate_gpm     = sgMultiNodeState.DrainingRate_gpm;
            sgTotalMassDrained_lb  = sgMultiNodeState.TotalMassDrained_lb;
            sgSecondaryMass_lb     = sgMultiNodeState.SecondaryWaterMass_lb;
            sgWideRangeLevel_pct   = sgMultiNodeState.WideRangeLevel_pct;
            sgNarrowRangeLevel_pct = sgMultiNodeState.NarrowRangeLevel_pct;

            rcsWaterMass = physicsState.RCSWaterMass;
            pzrWaterVolume = physicsState.PZRWaterVolume;
            pzrSteamVolume = physicsState.PZRSteamVolume;
            pzrLevel = physicsState.PZRLevel;

            T_sat = WaterProperties.SaturationTemperature(pressure);
            T_pzr = T_sat;

            if (!heatupResult.Converged &&
                ShouldEmitHotPathWarning(ref nextRegime3ConvergenceWarnTime_hr))
            {
                Debug.LogWarning($"[T+{simTime:F2}hr] CoupledThermo did not converge, using estimate");
            }

            if (rcpCount < 4)
            {
                float rcpBaseTime = bubbleFormationTime + RCP1_START_TIME;
                float timeToNext = RCP_START_INTERVAL - ((simTime - rcpBaseTime) % RCP_START_INTERVAL);
                statusMessage = $"RCPs: {rcpCount}/4 - NEXT IN {timeToNext * 60:F0} MIN";
                heatupPhaseDesc = $"RCS BULK HEATUP - {rcpCount} RCPs ({rcpCount * PlantConstants.RCP_HEAT_MW_EACH:F1} MW)";
            }
            else
            {
                statusMessage = "ALL 4 RCPs RUNNING - FULL HEATUP";
                heatupPhaseDesc = $"FULL HEATUP - 4 RCPs ({PlantConstants.RCP_HEAT_MW:F0} MW)";
            }

            if (heatupRate > 0.5f)
                estimatedTotalHeatupTime = simTime + (targetTemperature - T_rcs) / heatupRate;
            else
                estimatedTotalHeatupTime = simTime;
        }

        // ================================================================
        // 3. FINAL UPDATES — Temperature averaging, rates, mode
        // v0.7.1: Corrected T_avg calculation to Westinghouse definition
        // ================================================================

        // v0.1.0.0: Ledger rebase moved INSIDE Regime 2/3 blocks (before CVCS mutation).
        // The rebase now occurs before any boundary flow modifies the ledger,
        // ensuring the canonical ledger starts from a clean component-sum baseline.
        // See: Regime 2 (~line 1171) and Regime 3 (~line 1325) rebase blocks.

        // T_HOT / T_COLD — Delegated to LoopThermodynamics module (calculate FIRST)
        {
            var loopTemps = LoopThermodynamics.CalculateLoopTemperatures(
                T_rcs, pressure, rcpCount, rcpHeat, T_pzr);
            T_hot = loopTemps.T_hot;
            T_cold = loopTemps.T_cold;
        }

        // T_avg per Westinghouse definition: simple average of loop temperatures
        // Per PlantConstants.T_AVG = 588.5°F = (T_HOT + T_COLD) / 2 = (619 + 558) / 2
        // The pressurizer is NOT included in T_avg (it's tracked separately)
        // This ensures T_COLD ≤ T_AVG ≤ T_HOT in all operating regimes
        T_avg = (T_hot + T_cold) / 2.0f;

        pzrHeatRate = (T_pzr - prevT_pzr) / dt;
        rcsHeatRate = (T_rcs - prevT_rcs) / dt;
        heatupRate = rcsHeatRate;
        pressureRate = (pressure - prevPressure) / dt;

        T_sat = WaterProperties.SaturationTemperature(pressure);
        subcooling = T_sat - T_rcs;
        plantMode = GetMode(T_rcs);
        rcsWaterMass = physicsState.RCSWaterMass;

        // ================================================================
        // 4. BUBBLE FORMATION STATE MACHINE — Delegated to BubbleFormation partial
        // ================================================================
        bool bubbleDrainActive = UpdateBubbleFormation(dt);

        // ================================================================
        // 5. CVCS, RCS INVENTORY, VCT — Delegated to CVCS partial
        // ================================================================
        UpdateCVCSFlows(dt, bubbleDrainActive);
        AssignThermoWriterByInvariants();
        TraceRcpTransientStep(
            prevRcpCountStep,
            prevRcpHeatStep,
            prevPzrLevelStep,
            prevPressure,
            prevNoRcpTransportStep,
            prevCouplingAlphaStep,
            alphaRaw,
            alpha);

        // ================================================================
        // 5a. v0.3.0.0 Phase B (Fix 3.1): REGIME 1 MASS LEDGER ASSERTION
        // Diagnostic check: component sum must match ledger.
        // This assertion detects bugs — it does NOT correct them.
        // The ledger remains authoritative (v0.1.0.0 Article III).
        // Only active during Regime 1 two-phase (DRAIN/STABILIZE/PRESSURIZE).
        // ================================================================
        {
            bool regime1TwoPhase = !solidPressurizer && !bubbleFormed && rcpCount == 0
                                && bubblePhase != BubbleFormationPhase.NONE;
            if (regime1TwoPhase)
            {
                float componentSum_r1 = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
                float ledgerDrift_r1 = componentSum_r1 - physicsState.TotalPrimaryMass_lb;
                if (Mathf.Abs(ledgerDrift_r1) > 1.0f)
                {
                    if (ShouldEmitHotPathWarning(ref nextR1MassAuditWarnTime_hr))
                    {
                        Debug.LogWarning($"[T+{simTime:F2}hr] [R1 MASS AUDIT] Ledger drift = {ledgerDrift_r1:F1} lbm " +
                            $"(components={componentSum_r1:F0}, ledger={physicsState.TotalPrimaryMass_lb:F0}, phase={bubblePhase})");
                    }
                }
            }
        }

        simTime += dt;

        // ================================================================
        // 6. RVLIS & ANNUNCIATORS — Delegated to Alarms partial
        // ================================================================
        UpdateRVLIS();
        UpdateAnnunciators();
        
        // ================================================================
        // 7. v1.1.0: HZP STABILIZATION SYSTEMS
        // Steam dump, heater PID, and HZP state machine
        // Only active when approaching HZP conditions
        // ================================================================
        UpdateHZPLifecycle();
        UpdateHZPSystems(dt);

        // Diagnostic: signed startup net heat (no clipping).
        float currentHeatLoss = HeatTransfer.InsulationHeatLoss_MW(T_rcs);
        netPlantHeat_MW = (rcpHeat + pzrHeaterPower + rhrNetHeat_MW)
                        - (currentHeatLoss + sgHeatTransfer_MW + steamDumpHeat_MW);
        UpdateIP0018EnergyTelemetry(dt);
        
        // ================================================================
        // 8. v1.1.0 Stage 5: INVENTORY AUDIT
        // Comprehensive mass balance tracking
        // ================================================================
        UpdateInventoryAudit(dt);

        // ================================================================
        // 9. v0.1.0.0 Phase C: PRIMARY MASS LEDGER DIAGNOSTICS (CS-0006)
        // Computes ledger vs component drift, expected-mass identity,
        // and sets status/alarm fields for UI display.
        // Must run AFTER solver + boundary accounting + inventory audit
        // so all mass values are final for this timestep.
        // ================================================================
        UpdatePrimaryMassLedgerDiagnostics();

        // IP-0016 runtime assertion hardening: fail fast on non-finite mass state.
        if (float.IsNaN(physicsState.TotalPrimaryMass_lb) || float.IsInfinity(physicsState.TotalPrimaryMass_lb) ||
            float.IsNaN(physicsState.RCSWaterMass) || float.IsInfinity(physicsState.RCSWaterMass) ||
            float.IsNaN(physicsState.PZRWaterMass) || float.IsInfinity(physicsState.PZRWaterMass) ||
            float.IsNaN(physicsState.PZRSteamMass) || float.IsInfinity(physicsState.PZRSteamMass))
        {
            throw new InvalidOperationException(
                $"Non-finite mass state detected at T+{simTime:F4}hr " +
                $"(ledger={physicsState.TotalPrimaryMass_lb}, rcs={physicsState.RCSWaterMass}, " +
                $"pzrW={physicsState.PZRWaterMass}, pzrS={physicsState.PZRSteamMass})");
        }
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    HeaterAuthorityState ResolveHeaterAuthorityState()
    {
        if (startupHoldActive)
            return HeaterAuthorityState.HOLD_LOCKED;
        if (currentHeaterMode == HeaterMode.OFF)
            return HeaterAuthorityState.OFF;
        if (heaterManualDisabled)
            return HeaterAuthorityState.MANUAL_DISABLED;
        return HeaterAuthorityState.AUTO;
    }

    void UpdateHeaterAuthorityTelemetry(string requestedMode, HeaterAuthorityState effectiveAuthority)
    {
        heaterAuthorityState = effectiveAuthority.ToString();
        switch (effectiveAuthority)
        {
            case HeaterAuthorityState.HOLD_LOCKED:
                heaterAuthorityReason = "startup_hold_active";
                break;
            case HeaterAuthorityState.OFF:
                heaterAuthorityReason = "mode_off";
                break;
            case HeaterAuthorityState.MANUAL_DISABLED:
                heaterAuthorityReason = "manual_disable_lockout";
                break;
            default:
                heaterAuthorityReason = "auto_authority";
                break;
        }

        bool authorityChanged = !string.Equals(lastLoggedHeaterAuthorityState, heaterAuthorityState, StringComparison.Ordinal);
        bool limiterChanged = !string.Equals(lastLoggedHeaterLimiterReason, heaterLimiterReason, StringComparison.Ordinal);
        if (!authorityChanged && !limiterChanged)
            return;

        string overrideReason = heaterAuthorityState == "AUTO"
            ? "none"
            : $"resolved_{heaterAuthorityState.ToLowerInvariant()}";
        LogEvent(
            EventSeverity.INFO,
            $"HEATER AUTHORITY RESOLVED requested={requestedMode} effective={heaterAuthorityState} " +
            $"override={overrideReason} limiter={heaterLimiterReason} detail={heaterLimiterDetail}");
        lastLoggedHeaterAuthorityState = heaterAuthorityState;
        lastLoggedHeaterLimiterReason = heaterLimiterReason;
    }

    void UpdateStartupHoldState()
    {
        startupHoldElapsedTime_hr = Mathf.Max(0f, simTime - startupHoldStartTime_hr);
        startupHoldPressureRateAbs_psi_hr = Mathf.Abs(pressureRate);

        if (!startupHoldActive)
        {
            startupHoldTimeGatePassed = true;
            startupHoldPressureRateGatePassed = true;
            startupHoldStateQualityGatePassed = true;
            startupHoldReleaseBlockReason = "NONE";
            return;
        }

        if (!startupHoldActivationLogged)
        {
            float holdSeconds = Mathf.Max(0f, startupHoldReleaseTime_hr * 3600f);
            LogEvent(
                EventSeverity.INFO,
                $"HEATER STARTUP HOLD ACTIVE for {holdSeconds:F0}s " +
                $"(dP/dt gate <= {STARTUP_HOLD_PRESSURE_RATE_GATE_PSI_HR:F1} psi/hr for " +
                $"{STARTUP_HOLD_STABILITY_WINDOW_SEC:F0}s, mode={currentHeaterMode})");
            startupHoldActivationLogged = true;
        }

        float dtSec = Mathf.Max(0f, startupHoldLastStepDt_hr * 3600f);
        bool pressureRateWithinGate = startupHoldPressureRateAbs_psi_hr <= STARTUP_HOLD_PRESSURE_RATE_GATE_PSI_HR;
        startupHoldPressureRateStableAccum_sec = pressureRateWithinGate
            ? startupHoldPressureRateStableAccum_sec + dtSec
            : 0f;

        startupHoldTimeGatePassed = simTime >= startupHoldReleaseTime_hr;
        startupHoldPressureRateGatePassed = startupHoldPressureRateStableAccum_sec >= STARTUP_HOLD_STABILITY_WINDOW_SEC;
        startupHoldStateQualityGatePassed =
            IsFinite(pressure) &&
            IsFinite(T_rcs) &&
            IsFinite(T_pzr) &&
            !float.IsNaN(simTime) &&
            !float.IsInfinity(simTime);

        if (!startupHoldTimeGatePassed)
        {
            startupHoldReleaseBlockReason = "MIN_TIME_NOT_REACHED";
            return;
        }

        if (startupHoldTimeGatePassed &&
            startupHoldPressureRateGatePassed &&
            startupHoldStateQualityGatePassed)
        {
            startupHoldActive = false;
            startupHoldReleaseLogged = true;
            startupHoldReleaseBlockReason = "NONE";
            LogEvent(
                EventSeverity.ACTION,
                $"HEATER STARTUP HOLD RELEASED at T+{simTime:F4} hr " +
                $"(dP/dt={startupHoldPressureRateAbs_psi_hr:F2} psi/hr, " +
                $"stable_window={startupHoldPressureRateStableAccum_sec:F1}s, mode={currentHeaterMode})");
            return;
        }

        startupHoldReleaseBlockReason = !startupHoldPressureRateGatePassed
            ? "PRESSURE_RATE_UNSTABLE"
            : "STATE_QUALITY_FAIL";

        float holdElapsedSec = startupHoldElapsedTime_hr * 3600f;
        bool shouldLogBlocked =
            startupHoldLastBlockedLogTime_hr < 0f ||
            (simTime - startupHoldLastBlockedLogTime_hr) * 3600f >= STARTUP_HOLD_BLOCKED_LOG_INTERVAL_SEC;
        if (shouldLogBlocked)
        {
            startupHoldLastBlockedLogTime_hr = simTime;
            LogEvent(
                EventSeverity.ALERT,
                $"HEATER STARTUP HOLD BLOCKED release reason={startupHoldReleaseBlockReason} " +
                $"elapsed={holdElapsedSec:F1}s dP/dt={startupHoldPressureRateAbs_psi_hr:F2} psi/hr " +
                $"stable_window={startupHoldPressureRateStableAccum_sec:F1}s");
        }
    }

    public void BeginLongHoldPressureAudit()
    {
        longHoldPressureAuditActive = true;
        longHoldPressureTickCount = 0;
        longHoldPressureWriteCount = 0;
        longHoldPressureStateDerivedWriteCount = 0;
        longHoldPressureOverrideAttemptCount = 0;
        longHoldPressureBlockedOverrideCount = 0;
        longHoldPressureInvariantFailed = false;
        longHoldPressureInvariantReason = "";
        longHoldLastPressureBefore = pressure;
        longHoldLastPressureAfter = pressure;
        longHoldLastPressureSource = "BEGIN_LONG_HOLD_AUDIT";
        longHoldLastPressureStack = "";
        longHoldPressureRegime = "UNSET";
        longHoldPressureEquationBranch = "UNSET";
        longHoldPressureUsesSaturation = false;
        longHoldPressureSaturationPsia = 0f;
        longHoldPressureModelDensity = 0f;
        longHoldPressureModelCompressibility = 0f;
        longHoldPressureModelDeltaPsi = 0f;
    }

    public void EndLongHoldPressureAudit()
    {
        longHoldPressureAuditActive = false;
    }

    void SetLongHoldPressureModelTrace(
        string regimeLabel,
        string equationBranch,
        bool usesSaturation,
        float saturationPsia,
        float modelDensity,
        float modelCompressibility,
        float modelDeltaPsi)
    {
        longHoldPressureRegime = string.IsNullOrWhiteSpace(regimeLabel) ? "UNSET" : regimeLabel.Trim();
        longHoldPressureEquationBranch = string.IsNullOrWhiteSpace(equationBranch) ? "UNSET" : equationBranch.Trim();
        longHoldPressureUsesSaturation = usesSaturation;
        longHoldPressureSaturationPsia = saturationPsia;
        longHoldPressureModelDensity = modelDensity;
        longHoldPressureModelCompressibility = modelCompressibility;
        longHoldPressureModelDeltaPsi = modelDeltaPsi;
    }

    string GetLongHoldPressureTrace()
    {
        return
            $"tick={longHoldPressureTickCount} regime={longHoldPressureRegime} writer={thermoStateWriter} " +
            $"solid={solidPressurizer} bubble={bubbleFormed} rcp={rcpCount} rhr={rhrState.Mode} " +
            $"eqBranch={longHoldPressureEquationBranch} satUsed={longHoldPressureUsesSaturation} " +
            $"satPsia={longHoldPressureSaturationPsia:F3} rho={longHoldPressureModelDensity:F4} " +
            $"kappa={longHoldPressureModelCompressibility:E3} dP_model={longHoldPressureModelDeltaPsi:F6}";
    }

    public bool ApplyPressureWrite(float nextPressure, string source, bool stateDerived)
    {
        float prevPressure = pressure;
        if (!stateDerived && !longHoldPressureAuditActive && IsStressPressureForceSource(source))
        {
            // IP-0019 transient stabilization: runner-forced stress pressure writes are
            // applied with a physical slew limit to avoid single-tick discontinuities.
            nextPressure = Mathf.MoveTowards(prevPressure, nextPressure, STRESS_FORCE_PRESSURE_MAX_STEP_PSI);
        }
        float deltaPressure = nextPressure - prevPressure;
        bool noChange = Mathf.Abs(deltaPressure) <= 1e-6f;
        string stack = longHoldPressureAuditActive ? Environment.StackTrace : "";

        if (longHoldPressureAuditActive)
        {
            longHoldPressureTickCount++;
            longHoldPressureWriteCount++;

            if (stateDerived)
            {
                longHoldPressureStateDerivedWriteCount++;
            }
            else
            {
                longHoldPressureOverrideAttemptCount++;
                longHoldPressureBlockedOverrideCount++;
                longHoldPressureInvariantFailed = true;
                longHoldPressureInvariantReason =
                    $"Non-state pressure write blocked from {source} at T+{simTime:F4}hr ({prevPressure:F3}->{nextPressure:F3} psia). {GetLongHoldPressureTrace()}";
                longHoldLastPressureBefore = prevPressure;
                longHoldLastPressureAfter = nextPressure;
                longHoldLastPressureSource = source;
                longHoldLastPressureStack = stack;

                Debug.LogError(
                    $"[LONG_HOLD][PRESSURE_BLOCKED] source={source} derived={stateDerived} " +
                    $"prev={prevPressure:F3} next={nextPressure:F3} t={simTime:F4}hr {GetLongHoldPressureTrace()}\n{stack}");
                return false;
            }

            longHoldLastPressureBefore = prevPressure;
            longHoldLastPressureAfter = nextPressure;
            longHoldLastPressureSource = source;
            longHoldLastPressureStack = stack;
            if (noChange)
            {
                Debug.Log(
                    $"[LONG_HOLD][PRESSURE_TICK] source={source} action=NO_CHANGE derived={stateDerived} " +
                    $"p={prevPressure:F3} t={simTime:F4}hr {GetLongHoldPressureTrace()}");
            }
            else
            {
                Debug.Log(
                    $"[LONG_HOLD][PRESSURE_WRITE] source={source} derived={stateDerived} " +
                    $"prev={prevPressure:F3} next={nextPressure:F3} dP={deltaPressure:F6} t={simTime:F4}hr {GetLongHoldPressureTrace()}\n{stack}");
            }
        }

        if (noChange)
            return true;

        pressure = nextPressure;
        return true;
    }

    void AssignThermoWriterByInvariants()
    {
        if (rcpCount <= 0)
        {
            thermoStateWriter = solidPressurizer ? "REGIME1_SOLID" : "REGIME1_ISOLATED";
            return;
        }

        thermoStateWriter = rcpContribution.AllFullyRunning
            ? "REGIME3_COUPLED"
            : "REGIME2_BLEND";
    }

    float GetSealInjectionFlowGpm()
    {
        return rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
    }

    float GetSealLeakoffFlowGpm()
    {
        return rcpCount * PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM;
    }

    void UpdateIP0018EnergyTelemetry(float dt_hr)
    {
        const float overPrimaryThresholdPct = 5f;
        stageE_LastSGHeatRemoval_MW = sgHeatTransfer_MW;
        stageE_PrimaryHeatInput_MW = ComputeStageEPrimaryHeatInput_MW();
        stageE_EnergyWindowActive = sgStartupStateMode != SGStartupBoundaryStateMode.OpenPreheat;

        if (stageE_EnergyWindowActive)
        {
            if (stageE_LastSGHeatRemoval_MW < 0f)
                stageE_EnergyNegativeViolationCount++;

            float overPrimaryPct = 0f;
            if (stageE_PrimaryHeatInput_MW > 0.001f)
            {
                overPrimaryPct = 100f * (stageE_LastSGHeatRemoval_MW - stageE_PrimaryHeatInput_MW)
                    / stageE_PrimaryHeatInput_MW;
            }
            else if (stageE_LastSGHeatRemoval_MW > 0.001f)
            {
                overPrimaryPct = float.PositiveInfinity;
            }

            if (!float.IsInfinity(overPrimaryPct) && !float.IsNaN(overPrimaryPct))
                stageE_EnergyMaxOverPrimaryPct = Mathf.Max(stageE_EnergyMaxOverPrimaryPct, overPrimaryPct);
            else
                stageE_EnergyMaxOverPrimaryPct = float.PositiveInfinity;

            if (overPrimaryPct > overPrimaryThresholdPct)
                stageE_EnergyOverPrimaryViolationCount++;

            float dt_s = dt_hr * 3600f;
            stageE_TotalPrimaryEnergy_MJ += stageE_PrimaryHeatInput_MW * dt_s;
            stageE_TotalSGEnergyRemoved_MJ += Mathf.Max(0f, stageE_LastSGHeatRemoval_MW) * dt_s;
            stageE_EnergySampleCount++;
        }

        if (stageE_TotalPrimaryEnergy_MJ > 0.001f)
        {
            stageE_PercentMismatch = 100f * (stageE_TotalSGEnergyRemoved_MJ - stageE_TotalPrimaryEnergy_MJ)
                                   / stageE_TotalPrimaryEnergy_MJ;
        }
        else
        {
            stageE_PercentMismatch = 0f;
        }
    }

    float ComputeStageEPrimaryHeatInput_MW()
    {
        // CS-0062: preserve primary heat-input semantics.
        // This value must represent primary-side heat additions, never SG removal.
        float rcpHeatInput_MW = Mathf.Max(0f, rcpHeat);
        float heaterInput_MW = Mathf.Max(0f, pzrHeaterPower);
        float rhrHeating_MW = Mathf.Max(0f, rhrNetHeat_MW);
        return rcpHeatInput_MW + heaterInput_MW + rhrHeating_MW;
    }

    int GetCurrentPhysicsRegimeId(float couplingAlpha)
    {
        if (rcpCount == 0 || couplingAlpha < 0.001f)
            return 1;
        if (!rcpContribution.AllFullyRunning)
            return 2;
        return 3;
    }

    string GetPhysicsRegimeLabel(int regimeId, float couplingAlpha)
    {
        switch (regimeId)
        {
            case 1:
                return "REGIME 1 (Isolated)";
            case 2:
                return $"REGIME 2 (Blended alpha={couplingAlpha:F2})";
            case 3:
                return "REGIME 3 (Coupled)";
            default:
                return "REGIME UNKNOWN";
        }
    }

    void LogPhysicsRegimeTransitionIfNeeded(float couplingAlpha)
    {
        int currentRegimeId = GetCurrentPhysicsRegimeId(couplingAlpha);
        string currentLabel = GetPhysicsRegimeLabel(currentRegimeId, couplingAlpha);

        if (previousPhysicsRegimeId == 0)
        {
            previousPhysicsRegimeId = currentRegimeId;
            previousPhysicsRegimeLabel = currentLabel;
            return;
        }

        if (currentRegimeId != previousPhysicsRegimeId)
        {
            string reason = currentRegimeId == 1
                ? "no_rcp_or_zero_coupling"
                : (currentRegimeId == 2 ? "partial_rcp_coupling" : "all_rcps_fully_running");
            LogEvent(
                EventSeverity.INFO,
                $"PHYSICS REGIME TRANSITION: {previousPhysicsRegimeLabel} -> {currentLabel} " +
                $"(alpha={couplingAlpha:F3}, rcp={rcpCount}, reason={reason})");
            Debug.Log(
                $"[REGIME_TRANSITION] T+{simTime:F4}hr {previousPhysicsRegimeLabel} -> {currentLabel} " +
                $"(alpha={couplingAlpha:F3}, rcp={rcpCount}, reason={reason})");
        }

        previousPhysicsRegimeId = currentRegimeId;
        previousPhysicsRegimeLabel = currentLabel;
    }

    bool ShouldEmitHotPathWarning(ref float nextAllowedTime_hr)
    {
        float minInterval_hr = HOT_PATH_WARNING_MIN_INTERVAL_SEC / 3600f;
        if (simTime + 1e-9f < nextAllowedTime_hr)
        {
            hotPathWarningSuppressedCount++;
            return false;
        }

        nextAllowedTime_hr = simTime + minInterval_hr;
        return true;
    }

    void ResetSGBoundaryStartupState()
    {
        sgStartupStateMode = SGStartupBoundaryStateMode.OpenPreheat;
        sgStartupStateIntervalCount = 0;
        sgStartupBoundaryStateTicks = 0;
        sgStartupBoundaryStateTime_hr = 0f;
        sgStartupIntervalAccumulator_hr = 0f;
        sgStartupBoundaryState = GetSGBoundaryStateLabel(sgStartupStateMode);
        sgHoldTargetPressure_psia = 0f;
        sgHoldPressureDeviation_pct = 0f;
        sgHoldNetLeakage_pct = 0f;
        sgHoldBaselineSecondaryMass_lb = 0f;
        sgHoldCumulativeLeakage_pct = 0f;
        sgPressurizationWindowActive = false;
        sgPressurizationWindowStartTime_hr = 0f;
        sgPressurizationWindowNetPressureRise_psia = 0f;
        sgPressurizationWindowTsatDelta_F = 0f;
        sgPressurizationConsecutivePressureRiseIntervals = 0;
        sgPressurizationConsecutiveTsatRiseIntervals = 0;
        sgPreBoilTempApproachToTsat_F = 0f;
        sgPressurizationWindowStarted = false;
        sgBoilingTransitionObserved = false;
        sgPressurizationWindowStartPressure_psia = 0f;
        sgPressurizationWindowStartTsat_F = 0f;
        sgPressurizationLastPressure_psia = 0f;
        sgPressurizationLastTsat_F = 0f;
        stageE_DynamicActiveHeatingIntervalCount = 0;
        stageE_DynamicPrimaryRiseCheckCount = 0;
        stageE_DynamicPrimaryRisePassCount = 0;
        stageE_DynamicPrimaryRiseFailCount = 0;
        stageE_DynamicTempDelta3WindowCount = 0;
        stageE_DynamicTempDelta3Last_F = 0f;
        stageE_DynamicTempDelta3Min_F = 0f;
        stageE_DynamicTempDelta3Max_F = 0f;
        stageE_DynamicTempDelta3Above5Count = 0;
        stageE_DynamicTempDelta3Below2Count = 0;
        stageE_DynamicPressureFlatline3Count = 0;
        stageE_DynamicHardClampViolationCount = 0;
        stageE_DynamicHardClampStreak = 0;
        stageE_DynamicLastPressureDelta_psia = 0f;
        stageE_DynamicLastTopToTsatDelta_F = 0f;
        stageE_EnergyWindowActive = false;
        stageE_LastSGHeatRemoval_MW = 0f;
        stageE_EnergyNegativeViolationCount = 0;
        stageE_EnergyOverPrimaryViolationCount = 0;
        stageE_EnergyMaxOverPrimaryPct = 0f;
        stageEDynamicPrevIntervalValid = false;
        stageEDynamicPrevPrimaryHeatInput_MW = 0f;
        stageEDynamicPrevPressure_psia = 0f;
        stageEDynamicWindowCount = 0;
        stageEDynamicWindowHead = 0;
    }

    bool ShouldBeginRhrIsolation(float currentRcsTempF)
    {
        if (rhrState.Mode != RHRMode.Heatup)
            return false;

        bool allRcpsRunning = rcpCount >= 4;
        float nearIsolationTempF = PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F - RHR_ISOLATION_NEAR_TEMP_BAND_F;
        bool nearMode3Temperature = currentRcsTempF >= nearIsolationTempF;
        return allRcpsRunning && nearMode3Temperature;
    }

    float ComputeRegime2CouplingAlpha(float alphaRaw, float dt_hr)
    {
        if (rcpCount <= 0)
        {
            smoothedRegime2Alpha = 0f;
            return 0f;
        }

        float alphaTarget = Mathf.Clamp01(alphaRaw);
        float blend = Mathf.Clamp01(dt_hr / REGIME2_ALPHA_SMOOTH_TAU_HR);
        smoothedRegime2Alpha += (alphaTarget - smoothedRegime2Alpha) * blend;
        return Mathf.Clamp01(smoothedRegime2Alpha);
    }

    float ComputeNoRcpBulkTransportFactor(float dt_hr)
    {
        if (rcpCount > 0)
            return 1f;

        float systemVolume_gal =
            (PlantConstants.RCS_WATER_VOLUME + PlantConstants.PZR_TOTAL_VOLUME) * PlantConstants.FT3_TO_GAL;
        if (systemVolume_gal <= 1f)
            return NO_RCP_NATURAL_FLOOR;

        float forcedFlow_gpm = (rhrState.SuctionValvesOpen && rhrState.FlowRate_gpm > 0f)
            ? rhrState.FlowRate_gpm
            : 0f;
        float turnoverFraction = forcedFlow_gpm * dt_hr * 60f / systemVolume_gal;
        float forcedFactor = Mathf.Clamp01(turnoverFraction * NO_RCP_TRANSPORT_GAIN);
        return Mathf.Clamp01(Mathf.Max(NO_RCP_NATURAL_FLOOR, forcedFactor));
    }

    float ComputeNoRcpHeatDeltaF(float heatMw, float rcsHeatCapBtuF, float dt_hr)
    {
        if (rcsHeatCapBtuF <= 1f)
            return 0f;

        float transportedHeatMw = heatMw * noRcpTransportFactor;
        float transportedHeatBtu = transportedHeatMw * PlantConstants.MW_TO_BTU_HR * dt_hr;
        return transportedHeatBtu / rcsHeatCapBtuF;
    }

    float ClampRegime2PzrLevelStep(float previousLevelPct, float candidateLevelPct, float couplingAlpha)
    {
        float maxStepPct = Mathf.Lerp(
            REGIME2_STARTUP_LEVEL_STEP_MIN_PCT,
            REGIME2_MAX_PZR_LEVEL_STEP_PCT,
            Mathf.Clamp01(couplingAlpha * couplingAlpha * couplingAlpha));
        float delta = candidateLevelPct - previousLevelPct;
        if (Mathf.Abs(delta) <= maxStepPct)
            return candidateLevelPct;

        return previousLevelPct + Mathf.Sign(delta) * maxStepPct;
    }

    bool IsStressPressureForceSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        return source.StartsWith("RUNNER_STRESS_FORCE_", StringComparison.OrdinalIgnoreCase)
            || source.StartsWith("RUNNER_FORCE_RCP_STATE", StringComparison.OrdinalIgnoreCase);
    }

    void TraceRcpTransientStep(
        int prevRcpCount,
        float prevRcpHeatMw,
        float prevPzrLevelPct,
        float prevPressurePsia,
        float prevNoRcpTransport,
        float prevCouplingAlpha,
        float alphaRaw,
        float alphaSmoothed)
    {
        bool rcpCountChanged = prevRcpCount != rcpCount;
        bool inRcpRamp = rcpCount > 0 && (alphaSmoothed < 0.999f || alphaRaw < 0.999f);
        if (!rcpCountChanged && !inRcpRamp)
            return;

        float latestStart = float.MinValue;
        for (int i = 0; i < rcpCount && i < rcpStartTimes.Length; i++)
            latestStart = Mathf.Max(latestStart, rcpStartTimes[i]);
        bool withinTraceWindow = latestStart > float.MinValue
            && (simTime - latestStart) <= RCP_TRANSIENT_TRACE_WINDOW_HR;
        if (!withinTraceWindow && !rcpCountChanged)
            return;

        float dHeat = rcpHeat - prevRcpHeatMw;
        float dPressure = pressure - prevPressurePsia;
        float dLevel = pzrLevel - prevPzrLevelPct;
        float dTransport = noRcpTransportFactor - prevNoRcpTransport;
        float dAlpha = alphaSmoothed - prevCouplingAlpha;

        if (enableHighFrequencyPerfLogs)
        {
            Debug.Log(
                $"[RCP_TRANSIENT] t={simTime:F4}hr rcp={prevRcpCount}->{rcpCount} " +
                $"heatTarget={effectiveRCPHeat:F3}MW heatApplied={rcpHeat:F3}MW dHeat={dHeat:F4}MW " +
                $"dP={dPressure:F4}psi dPdt={pressureRate:F3}psi/hr " +
                $"pzr={pzrLevel:F4}% dLevel={dLevel:F4}% " +
                $"alphaRaw={alphaRaw:F5} alpha={alphaSmoothed:F5} dAlpha={dAlpha:F5} " +
                $"transport={noRcpTransportFactor:F5} dTransport={dTransport:F5}");
        }
    }

    void ComputePrimaryBoundaryMassFlows(
        float dt_hr,
        float rho_rcs,
        out float sealInjection_gpm,
        out float sealReturn_gpm,
        out float chargingToPrimary_gpm,
        out float primaryOutflow_gpm,
        out float rhoTransfer_lbm_ft3,
        out float massIn_lb,
        out float massOut_lb,
        out float netMass_lb)
    {
        float dt_sec = dt_hr * 3600f;
        sealInjection_gpm = GetSealInjectionFlowGpm();
        sealReturn_gpm = GetSealLeakoffFlowGpm();
        // IP-0016 PBOC: Charging flow is treated as primary inflow authority.
        // Seal return is handled as a primary outflow counterpart term.
        chargingToPrimary_gpm = Mathf.Max(0f, chargingFlow);
        primaryOutflow_gpm = Mathf.Max(0f, letdownFlow) + sealReturn_gpm;
        // IP-0019 (CS-0061): use runtime state density for boundary transfer mass
        // conversion; fixed 100F atmospheric density is no longer permitted.
        if (rho_rcs > 1f && !float.IsNaN(rho_rcs) && !float.IsInfinity(rho_rcs))
        {
            rhoTransfer_lbm_ft3 = rho_rcs;
        }
        else
        {
            rhoTransfer_lbm_ft3 = WaterProperties.WaterDensity(T_rcs, pressure);
        }

        float massPerGpmStep = dt_sec * PlantConstants.GPM_TO_FT3_SEC * rhoTransfer_lbm_ft3;

        massIn_lb = chargingToPrimary_gpm * massPerGpmStep;
        massOut_lb = primaryOutflow_gpm * massPerGpmStep;
        netMass_lb = massIn_lb - massOut_lb;
    }

    PrimaryBoundaryFlowEvent ComputePrimaryBoundaryFlowEvent(
        float dt_hr,
        float rho_rcs,
        int regimeId,
        string regimeLabel)
    {
        ComputePrimaryBoundaryMassFlows(
            dt_hr,
            rho_rcs,
            out float sealInjection_gpm,
            out float sealReturn_gpm,
            out float chargingToPrimary_gpm,
            out float primaryOutflow_gpm,
            out float rhoTransfer_lbm_ft3,
            out float massIn_lb,
            out float massOut_lb,
            out float netMass_lb);

        PrimaryBoundaryFlowEvent evt = new PrimaryBoundaryFlowEvent
        {
            TickIndex = pbocTickIndex,
            SimTime_hr = simTime,
            Dt_hr = dt_hr,
            RegimeId = regimeId,
            RegimeLabel = regimeLabel,
            LetdownFlow_gpm = Mathf.Max(0f, letdownFlow),
            ChargingFlow_gpm = Mathf.Max(0f, chargingFlow),
            SealInjection_gpm = sealInjection_gpm,
            SealReturn_gpm = sealReturn_gpm,
            ChargingToPrimary_gpm = chargingToPrimary_gpm,
            PrimaryOutflow_gpm = primaryOutflow_gpm,
            RhoRcs_lbm_ft3 = rho_rcs,
            RhoAux_lbm_ft3 = rhoTransfer_lbm_ft3,
            MassIn_lbm = massIn_lb,
            MassOut_lbm = massOut_lb,
            dm_RCS_lbm = netMass_lb,
            dm_PZRw_lbm = 0f,
            dm_PZRs_lbm = 0f,
            dm_VCT_lbm = 0f,
            dm_BRS_lbm = 0f,
            dm_external_lbm = 0f,
            ExternalIn_gal = 0f,
            ExternalOut_gal = 0f,
            AppliedToComponents = false,
            AppliedToLedger = false,
            PairingCheckPass = true
        };

        return evt;
    }

    void ApplyPrimaryBoundaryFlowPBOC(float dt_hr, float rho_rcs, int regimeId, string regimeLabel)
    {
        PrimaryBoundaryFlowEvent evt = ComputePrimaryBoundaryFlowEvent(dt_hr, rho_rcs, regimeId, regimeLabel);

        float componentMassBefore = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float pzrMassBefore = physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float pzrLevelBefore = physicsState.PZRLevel;
        float ledgerBefore = physicsState.TotalPrimaryMass_lb;

        // PBOC single-apply to primary component masses.
        physicsState.RCSWaterMass += evt.dm_RCS_lbm;
        physicsState.PZRWaterMass += evt.dm_PZRw_lbm;
        physicsState.PZRSteamMass += evt.dm_PZRs_lbm;
        evt.AppliedToComponents = true;

        // PBOC single-apply to canonical ledger and flow accumulators.
        float ledgerDelta = evt.dm_RCS_lbm + evt.dm_PZRw_lbm + evt.dm_PZRs_lbm;
        physicsState.TotalPrimaryMass_lb += ledgerDelta;
        physicsState.CumulativeCVCSIn_lb += evt.MassIn_lbm;
        physicsState.CumulativeCVCSOut_lb += evt.MassOut_lbm;
        evt.AppliedToLedger = true;

        if (evt.RhoAux_lbm_ft3 > 0.1f)
        {
            float rcsChange_gal = (evt.dm_RCS_lbm / evt.RhoAux_lbm_ft3) * PlantConstants.FT3_TO_GAL;
            VCTPhysics.AccumulateRCSChange(ref vctState, rcsChange_gal);
        }

        float componentMassAfter = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float pzrMassAfter = physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float pzrLevelAfter = physicsState.PZRLevel;
        float ledgerAfter = physicsState.TotalPrimaryMass_lb;
        float componentDelta = componentMassAfter - componentMassBefore;
        float ledgerDeltaApplied = ledgerAfter - ledgerBefore;
        if (Mathf.Abs(componentDelta - ledgerDeltaApplied) > 0.5f)
        {
            throw new InvalidOperationException(
                $"PBOC primary apply mismatch at tick {evt.TickIndex}: " +
                $"componentDelta={componentDelta:F4} lbm, ledgerDelta={ledgerDeltaApplied:F4} lbm");
        }

        pbocCurrentEvent = evt;
        pbocEventActiveThisTick = true;
        regime3CVCSPreApplied = true;

        if (bubblePhase != BubbleFormationPhase.DRAIN)
        {
            TryLogPzrOrificeDiagnostics(
                "PBOC_BOUNDARY",
                evt.LetdownFlow_gpm,
                evt.ChargingFlow_gpm,
                pzrLevelBefore,
                pzrLevelAfter,
                pzrMassBefore,
                pzrMassAfter);
        }
    }

    void FinalizePrimaryBoundaryFlowEventAux(
        float dmVCT_lbm,
        float dmBRS_lbm,
        float dmExternal_lbm,
        float externalIn_gal,
        float externalOut_gal,
        float makeupExternal_gpm,
        float divert_gpm,
        float cboLoss_gpm)
    {
        if (!pbocEventActiveThisTick)
            return;

        PrimaryBoundaryFlowEvent evt = pbocCurrentEvent;
        evt.dm_VCT_lbm = dmVCT_lbm;
        evt.dm_BRS_lbm = dmBRS_lbm;
        evt.dm_external_lbm = dmExternal_lbm;
        evt.ExternalIn_gal = externalIn_gal;
        evt.ExternalOut_gal = externalOut_gal;
        evt.MakeupExternal_gpm = makeupExternal_gpm;
        evt.Divert_gpm = divert_gpm;
        evt.CboLoss_gpm = cboLoss_gpm;

        float pairedBucketMagnitude = Mathf.Abs(evt.dm_VCT_lbm) + Mathf.Abs(evt.dm_BRS_lbm) + Mathf.Abs(evt.dm_external_lbm);
        bool flowReconfiguredThisTick =
            Mathf.Abs(evt.LetdownFlow_gpm - Mathf.Max(0f, letdownFlow)) > 1f ||
            Mathf.Abs(evt.ChargingFlow_gpm - Mathf.Max(0f, chargingFlow)) > 1f;
        if (!flowReconfiguredThisTick &&
            Mathf.Abs(evt.dm_RCS_lbm) > 0.5f &&
            pairedBucketMagnitude < 0.1f)
        {
            pbocPairingAssertionFailures++;
            evt.PairingCheckPass = false;
            LogEvent(
                EventSeverity.ALARM,
                $"PBOC pairing check fail at tick {evt.TickIndex}: " +
                $"dm_RCS={evt.dm_RCS_lbm:F3}lbm, paired={pairedBucketMagnitude:F3}lbm");
            if (ShouldEmitHotPathWarning(ref nextPbocPairingWarnTime_hr))
            {
                Debug.LogWarning(
                    $"[PBOC] Pairing check fail tick={evt.TickIndex} dm_RCS={evt.dm_RCS_lbm:F3}lbm " +
                    $"paired={pairedBucketMagnitude:F3}lbm");
            }
        }
        else
        {
            evt.PairingCheckPass = true;
        }
        pbocCurrentEvent = evt;
        pbocLastEvent = evt;
        pbocEventCount++;

        if (enableHighFrequencyPerfLogs &&
            (simTime < 0.02f || (simTime % 0.25f < evt.Dt_hr) || Mathf.Abs(evt.dm_RCS_lbm) > 25f))
        {
            Debug.Log($"[PBOC] tick={evt.TickIndex} t={evt.SimTime_hr:F4}hr regime={evt.RegimeLabel} " +
                      $"dm_RCS={evt.dm_RCS_lbm:+0.00;-0.00;0.00}lbm dm_VCT={evt.dm_VCT_lbm:+0.00;-0.00;0.00}lbm " +
                      $"dm_BRS={evt.dm_BRS_lbm:+0.00;-0.00;0.00}lbm dm_ext={evt.dm_external_lbm:+0.00;-0.00;0.00}lbm " +
                      $"flows[chg2RCS={evt.ChargingToPrimary_gpm:F2}, letdown={evt.LetdownFlow_gpm:F2}, " +
                      $"sealRet={evt.SealReturn_gpm:F2}, mkExt={evt.MakeupExternal_gpm:F2}, div={evt.Divert_gpm:F2}, cbo={evt.CboLoss_gpm:F2}]");
        }
    }

    void RecordRtccTransition(
        string transitionName,
        string fromAuthority,
        string toAuthority,
        float preMass_lbm,
        float reconstructedMass_lbm,
        float rawDelta_lbm,
        float postMass_lbm,
        float assertDelta_lbm,
        bool assertPass)
    {
        rtccTransitionCount++;
        rtccTelemetryPresent = true;
        rtccLastTransition = transitionName;
        rtccLastAuthorityFrom = fromAuthority;
        rtccLastAuthorityTo = toAuthority;
        rtccLastPreMass_lbm = preMass_lbm;
        rtccLastReconstructedMass_lbm = reconstructedMass_lbm;
        rtccLastRawDelta_lbm = rawDelta_lbm;
        rtccLastPostMass_lbm = postMass_lbm;
        rtccLastAssertDelta_lbm = assertDelta_lbm;
        rtccLastAssertPass = assertPass;
        rtccMaxRawDeltaAbs_lbm = Mathf.Max(rtccMaxRawDeltaAbs_lbm, Mathf.Abs(rawDelta_lbm));
        if (!assertPass)
            rtccAssertionFailureCount++;

        EventSeverity severity = assertPass ? EventSeverity.INFO : EventSeverity.ALARM;
        LogEvent(
            severity,
            $"RTCC {transitionName} [{fromAuthority}->{toAuthority}] pre={preMass_lbm:F1}lbm " +
            $"reconstructed={reconstructedMass_lbm:F1}lbm rawDelta={rawDelta_lbm:+0.0;-0.0;0.0}lbm " +
            $"post={postMass_lbm:F1}lbm assertDelta={assertDelta_lbm:+0.000;-0.000;0.000}lbm " +
            $"tol={PlantConstants.RTCC_EPSILON_MASS_LBM:F1}lbm result={(assertPass ? "PASS" : "FAIL")}");
    }

    void AssertRtccPassOrThrow(string transitionName, float assertDelta_lbm)
    {
        float absDelta = Mathf.Abs(assertDelta_lbm);
        if (absDelta <= PlantConstants.RTCC_EPSILON_MASS_LBM)
            return;

        throw new InvalidOperationException(
            $"RTCC assertion failed for {transitionName}: |delta|={absDelta:F3} lbm " +
            $"exceeds epsilon {PlantConstants.RTCC_EPSILON_MASS_LBM:F1} lbm");
    }

    void ApplySGBoundaryAuthority()
    {
        bool shouldIsolate = ShouldIsolateSGBoundary();
        SGMultiNodeThermal.SetSteamIsolation(ref sgMultiNodeState, shouldIsolate);
    }

    bool ShouldIsolateSGBoundary()
    {
        if (sgStartupStateMode == SGStartupBoundaryStateMode.OpenPreheat)
            return false;

        // Re-open secondary boundary after SG reaches steam-dump control.
        return sgMultiNodeState.CurrentRegime != SGThermalRegime.SteamDump;
    }

    void UpdateSGBoundaryDiagnostics(SGMultiNodeResult sgResult, float dt_hr)
    {
        sgBoundaryMode = sgResult.SteamIsolated ? "ISOLATED" : "OPEN";
        sgPressureSourceBranch = GetPressureSourceBranchLabel(sgResult.PressureSourceMode);
        sgSteamInventory_lb = sgResult.SteamInventory_lb;

        AdvanceSGBoundaryStartupState(sgResult, dt_hr);
    }

    void AdvanceSGBoundaryStartupState(SGMultiNodeResult sgResult, float dt_hr)
    {
        sgStartupBoundaryStateTicks++;
        sgStartupBoundaryStateTime_hr += dt_hr;
        sgStartupIntervalAccumulator_hr += dt_hr;

        bool intervalBoundary = false;
        if (sgStartupIntervalAccumulator_hr >= DP0003_INTERVAL_LOG_HR)
        {
            sgStartupIntervalAccumulator_hr -= DP0003_INTERVAL_LOG_HR;
            sgStartupStateIntervalCount++;
            intervalBoundary = true;
            UpdatePressurizationProgressTelemetry();
            UpdateDynamicSecondaryResponseTelemetry();
        }

        if (sgStartupStateMode == SGStartupBoundaryStateMode.Hold)
        {
            float target = Mathf.Max(1f, sgHoldTargetPressure_psia);
            sgHoldPressureDeviation_pct = 100f * (sgSecondaryPressure_psia - target) / target;

            float baselineMass = Mathf.Max(1f, sgHoldBaselineSecondaryMass_lb);
            float leakPctStep = Mathf.Abs(sgMultiNodeState.SteamOutflow_lbhr) * dt_hr * 100f / baselineMass;
            sgHoldCumulativeLeakage_pct += leakPctStep;
            sgHoldNetLeakage_pct = sgHoldCumulativeLeakage_pct;
        }

        switch (sgStartupStateMode)
        {
            case SGStartupBoundaryStateMode.OpenPreheat:
                if (T_rcs >= PlantConstants.SG_NITROGEN_ISOLATION_TEMP_F)
                {
                    TransitionSGBoundaryState(
                        SGStartupBoundaryStateMode.Pressurize,
                        "SG startup boundary transition: OPEN_PREHEAT -> PRESSURIZE");
                }
                break;

            case SGStartupBoundaryStateMode.Pressurize:
                if (intervalBoundary)
                {
                    float pressureGainPct = 100f * (sgSecondaryPressure_psia - PlantConstants.SG_INITIAL_PRESSURE_PSIA)
                                           / PlantConstants.SG_INITIAL_PRESSURE_PSIA;

                    if (sgStartupStateIntervalCount >= SG_STARTUP_MIN_PRESSURIZE_INTERVALS &&
                        pressureGainPct >= SG_PRESSURIZE_MIN_GAIN_PCT)
                    {
                        TransitionSGBoundaryState(
                            SGStartupBoundaryStateMode.Hold,
                            "SG startup boundary transition: PRESSURIZE -> HOLD");
                    }
                }
                break;

            case SGStartupBoundaryStateMode.Hold:
                if (intervalBoundary &&
                    sgStartupStateIntervalCount >= SG_STARTUP_MIN_HOLD_INTERVALS &&
                    Mathf.Abs(sgHoldPressureDeviation_pct) <= SG_HOLD_PRESSURE_BAND_PCT &&
                    sgHoldCumulativeLeakage_pct <= SG_HOLD_MAX_CUM_LEAKAGE_PCT)
                {
                    TransitionSGBoundaryState(
                        SGStartupBoundaryStateMode.IsolatedHeatup,
                        "SG startup boundary transition: HOLD -> ISOLATED_HEATUP");
                }
                break;
        }
    }

    void TransitionSGBoundaryState(SGStartupBoundaryStateMode nextState, string logMessage)
    {
        if (sgStartupStateMode == nextState)
            return;

        sgStartupStateMode = nextState;
        sgStartupStateIntervalCount = 0;
        sgStartupBoundaryStateTicks = 0;
        sgStartupBoundaryStateTime_hr = 0f;
        sgStartupIntervalAccumulator_hr = 0f;
        sgStartupBoundaryState = GetSGBoundaryStateLabel(nextState);

        if (nextState == SGStartupBoundaryStateMode.Hold)
        {
            sgHoldTargetPressure_psia = sgSecondaryPressure_psia;
            sgHoldPressureDeviation_pct = 0f;
            sgHoldCumulativeLeakage_pct = 0f;
            sgHoldNetLeakage_pct = 0f;
            sgHoldBaselineSecondaryMass_lb = Mathf.Max(1f, sgSecondaryMass_lb);
        }
        else if (nextState != SGStartupBoundaryStateMode.IsolatedHeatup)
        {
            sgHoldTargetPressure_psia = 0f;
            sgHoldPressureDeviation_pct = 0f;
            sgHoldNetLeakage_pct = 0f;
            sgHoldBaselineSecondaryMass_lb = 0f;
            sgHoldCumulativeLeakage_pct = 0f;
        }

        LogEvent(EventSeverity.INFO, logMessage);
    }

    void UpdatePressurizationProgressTelemetry()
    {
        const float pressureStartOffset_psia = 1.0f;
        float currentPressure = sgSecondaryPressure_psia;
        float currentTsat = sgSaturationTemp_F;

        if (!sgPressurizationWindowStarted &&
            currentPressure >= PlantConstants.SG_INITIAL_PRESSURE_PSIA + pressureStartOffset_psia)
        {
            sgPressurizationWindowStarted = true;
            sgPressurizationWindowActive = true;
            sgPressurizationWindowStartTime_hr = simTime;
            sgPressurizationWindowStartPressure_psia = currentPressure;
            sgPressurizationWindowStartTsat_F = currentTsat;
            sgPressurizationLastPressure_psia = currentPressure;
            sgPressurizationLastTsat_F = currentTsat;
            sgPressurizationConsecutivePressureRiseIntervals = 1;
            sgPressurizationConsecutiveTsatRiseIntervals = 1;
            sgPressurizationWindowNetPressureRise_psia = 0f;
            sgPressurizationWindowTsatDelta_F = 0f;
            sgPreBoilTempApproachToTsat_F = Mathf.Abs(sgTopNodeTemp - sgSaturationTemp_F);
        }

        if (!sgPressurizationWindowStarted || sgBoilingTransitionObserved)
            return;

        if (sgBoilingActive)
        {
            sgBoilingTransitionObserved = true;
            sgPressurizationWindowActive = false;
            sgPreBoilTempApproachToTsat_F = Mathf.Abs(sgTopNodeTemp - sgSaturationTemp_F);
            return;
        }

        if (currentPressure > sgPressurizationLastPressure_psia)
            sgPressurizationConsecutivePressureRiseIntervals++;
        else
            sgPressurizationConsecutivePressureRiseIntervals = 0;

        if (currentTsat > sgPressurizationLastTsat_F)
            sgPressurizationConsecutiveTsatRiseIntervals++;
        else
            sgPressurizationConsecutiveTsatRiseIntervals = 0;

        sgPressurizationWindowNetPressureRise_psia = currentPressure - sgPressurizationWindowStartPressure_psia;
        sgPressurizationWindowTsatDelta_F = currentTsat - sgPressurizationWindowStartTsat_F;
        sgPreBoilTempApproachToTsat_F = Mathf.Abs(sgTopNodeTemp - sgSaturationTemp_F);
        sgPressurizationLastPressure_psia = currentPressure;
        sgPressurizationLastTsat_F = currentTsat;
    }

    void UpdateDynamicSecondaryResponseTelemetry()
    {
        const float activeHeatThreshold_MW = 1.0f;
        const float pressureFlatlineBand_psia = 0.1f;
        const float primaryIncreaseThresholdFraction = 0.02f;
        const float hardClampTargetDelta_F = 50f;
        const float hardClampTolerance_F = 0.5f;

        DynamicIntervalSample sample = new DynamicIntervalSample
        {
            PrimaryHeatInput_MW = stageE_PrimaryHeatInput_MW,
            Pressure_psia = sgSecondaryPressure_psia,
            TopTemp_F = sgTopNodeTemp,
            Tsat_F = sgSaturationTemp_F,
            ActiveHeating = stageE_PrimaryHeatInput_MW > activeHeatThreshold_MW,
            IsHoldState = sgStartupStateMode == SGStartupBoundaryStateMode.Hold,
            IsIsolatedHeatupState = sgStartupStateMode == SGStartupBoundaryStateMode.IsolatedHeatup
        };

        stageE_DynamicLastTopToTsatDelta_F = sample.Tsat_F - sample.TopTemp_F;

        if (sample.ActiveHeating)
            stageE_DynamicActiveHeatingIntervalCount++;

        if (stageEDynamicPrevIntervalValid &&
            sample.ActiveHeating &&
            stageEDynamicPrevPrimaryHeatInput_MW > activeHeatThreshold_MW)
        {
            float primaryRiseFraction =
                (sample.PrimaryHeatInput_MW - stageEDynamicPrevPrimaryHeatInput_MW)
                / Mathf.Max(0.001f, stageEDynamicPrevPrimaryHeatInput_MW);

            if (primaryRiseFraction >= primaryIncreaseThresholdFraction)
            {
                stageE_DynamicPrimaryRiseCheckCount++;
                stageE_DynamicLastPressureDelta_psia = sample.Pressure_psia - stageEDynamicPrevPressure_psia;
                if (stageE_DynamicLastPressureDelta_psia > 0f)
                    stageE_DynamicPrimaryRisePassCount++;
                else
                    stageE_DynamicPrimaryRiseFailCount++;
            }
        }

        stageEDynamicPrevIntervalValid = true;
        stageEDynamicPrevPrimaryHeatInput_MW = sample.PrimaryHeatInput_MW;
        stageEDynamicPrevPressure_psia = sample.Pressure_psia;

        stageEDynamicWindow[stageEDynamicWindowHead] = sample;
        stageEDynamicWindowHead = (stageEDynamicWindowHead + 1) % 3;
        if (stageEDynamicWindowCount < 3)
            stageEDynamicWindowCount++;

        if (stageEDynamicWindowCount == 3)
        {
            DynamicIntervalSample a = stageEDynamicWindow[stageEDynamicWindowHead];
            DynamicIntervalSample b = stageEDynamicWindow[(stageEDynamicWindowHead + 1) % 3];
            DynamicIntervalSample c = stageEDynamicWindow[(stageEDynamicWindowHead + 2) % 3];
            bool allActiveHeating = a.ActiveHeating && b.ActiveHeating && c.ActiveHeating;

            if (allActiveHeating)
            {
                float tempDelta3_F = c.TopTemp_F - a.TopTemp_F;
                stageE_DynamicTempDelta3Last_F = tempDelta3_F;
                if (stageE_DynamicTempDelta3WindowCount == 0)
                {
                    stageE_DynamicTempDelta3Min_F = tempDelta3_F;
                    stageE_DynamicTempDelta3Max_F = tempDelta3_F;
                }
                else
                {
                    stageE_DynamicTempDelta3Min_F = Mathf.Min(stageE_DynamicTempDelta3Min_F, tempDelta3_F);
                    stageE_DynamicTempDelta3Max_F = Mathf.Max(stageE_DynamicTempDelta3Max_F, tempDelta3_F);
                }

                stageE_DynamicTempDelta3WindowCount++;
                if (tempDelta3_F > 5f)
                    stageE_DynamicTempDelta3Above5Count++;
                if (tempDelta3_F < 2f)
                    stageE_DynamicTempDelta3Below2Count++;

                float pressureDelta3_psia = c.Pressure_psia - a.Pressure_psia;
                bool pressureResponseWindow = a.IsIsolatedHeatupState
                    && b.IsIsolatedHeatupState
                    && c.IsIsolatedHeatupState;
                if (pressureResponseWindow && Mathf.Abs(pressureDelta3_psia) <= pressureFlatlineBand_psia)
                    stageE_DynamicPressureFlatline3Count++;
            }
        }

        if (sample.ActiveHeating && !sample.IsHoldState)
        {
            bool nearHardClamp =
                Mathf.Abs(stageE_DynamicLastTopToTsatDelta_F - hardClampTargetDelta_F) <= hardClampTolerance_F;
            stageE_DynamicHardClampStreak = nearHardClamp
                ? stageE_DynamicHardClampStreak + 1
                : 0;
            if (stageE_DynamicHardClampStreak >= 3)
                stageE_DynamicHardClampViolationCount++;
        }
        else
        {
            stageE_DynamicHardClampStreak = 0;
        }
    }

    static string GetSGBoundaryStateLabel(SGStartupBoundaryStateMode state)
    {
        switch (state)
        {
            case SGStartupBoundaryStateMode.Pressurize:
                return "PRESSURIZE";
            case SGStartupBoundaryStateMode.Hold:
                return "HOLD";
            case SGStartupBoundaryStateMode.IsolatedHeatup:
                return "ISOLATED_HEATUP";
            default:
                return "OPEN_PREHEAT";
        }
    }

    static string GetPressureSourceBranchLabel(SGPressureSourceMode mode)
    {
        switch (mode)
        {
            case SGPressureSourceMode.Floor:
                return "floor";
            case SGPressureSourceMode.Saturation:
                return "P_sat";
            default:
                return "inventory-derived";
        }
    }

    float GetTsat(float P)
    {
        return WaterProperties.SaturationTemperature(Mathf.Clamp(P, 14.7f, 3200f));
    }

    float GetTargetPressure()
    {
        float Tsat_target = T_avg + 50f;
        float P = WaterProperties.SaturationPressure(Tsat_target);
        return Mathf.Clamp(P, 350f, targetPressure);
    }

    int GetMode(float T)
    {
        if (T < 200f) return 5;
        if (T < 350f) return 4;
        return 3;
    }

    public string GetModeString()
    {
        switch (plantMode)
        {
            case 5: return "MODE 5\nCold Shutdown";
            case 4: return "MODE 4\nHot Shutdown";
            case 3: return "MODE 3\nHot Standby";
            default: return "UNKNOWN";
        }
    }

    public Color GetModeColor()
    {
        switch (plantMode)
        {
            case 5: return Color.cyan;
            case 4: return Color.yellow;
            case 3: return Color.green;
            default: return Color.white;
        }
    }

    // v0.7.1: Helper method for logging physics regime
    /// <summary>
    /// Get a human-readable description of the current physics regime.
    /// Used for logging and display.
    /// </summary>
    string GetPhysicsRegimeString()
    {
        int regimeId = GetCurrentPhysicsRegimeId(smoothedRegime2Alpha);
        return GetPhysicsRegimeLabel(regimeId, smoothedRegime2Alpha);
    }
}

