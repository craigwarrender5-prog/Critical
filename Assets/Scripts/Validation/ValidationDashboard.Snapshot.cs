// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Snapshot.cs - Data Snapshot Class
// ============================================================================
//
// PURPOSE:
//   Atomic data snapshot captured at 10 Hz from HeatupSimEngine.
//   The dashboard reads ONLY from this snapshot during OnGUI, never
//   directly from the engine. This ensures:
//     - Thread safety (snapshot is immutable once captured)
//     - Consistent data within a frame (no partial updates)
//     - Clean separation between simulation and rendering
//
// ARCHITECTURE:
//   - CaptureFrom() copies all needed values from HeatupSimEngine
//   - All fields are public readonly after capture
//   - New snapshot created each capture cycle (no mutation)
//
// PERFORMANCE:
//   - Capture happens in Update() at refresh rate, not in OnGUI
//   - String formatting done separately in PreformatStrings()
//   - Zero allocations during OnGUI rendering
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;
using Critical.Physics;

using Critical.Validation;
namespace Critical.Validation
{
    /// <summary>
    /// Atomic snapshot of all dashboard-relevant engine state.
    /// Captured at 10 Hz, read during OnGUI rendering.
    /// </summary>
    public class DashboardSnapshot
    {
        // ====================================================================
        // CORE PARAMETERS
        // ====================================================================

        /// <summary>Simulation time in hours</summary>
        public float SimTime;

        /// <summary>Wall clock time in hours</summary>
        public float WallClockTime;

        /// <summary>Plant mode (5=Cold SD, 4=Hot SD, 3=Hot Standby)</summary>
        public int PlantMode;

        /// <summary>Current heatup phase description</summary>
        public string HeatupPhaseDesc;

        /// <summary>Time acceleration speed index (0-4)</summary>
        public int SpeedIndex;

        /// <summary>Is time accelerated beyond 1x?</summary>
        public bool IsAccelerated;

        // ====================================================================
        // RCS PRIMARY
        // ====================================================================

        /// <summary>Average RCS temperature (Â°F)</summary>
        public float T_avg;

        /// <summary>Hot leg temperature (Â°F)</summary>
        public float T_hot;

        /// <summary>Cold leg temperature (Â°F)</summary>
        public float T_cold;

        /// <summary>Core delta-T (Â°F)</summary>
        public float CoreDeltaT;

        /// <summary>RCS bulk temperature (Â°F)</summary>
        public float T_rcs;

        /// <summary>RCS pressure (psia)</summary>
        public float Pressure;

        /// <summary>Saturation temperature at current pressure (Â°F)</summary>
        public float T_sat;

        /// <summary>Subcooling margin (Â°F)</summary>
        public float Subcooling;

        /// <summary>Heatup rate (Â°F/hr)</summary>
        public float HeatupRate;

        /// <summary>Pressure rate of change (psi/hr)</summary>
        public float PressureRate;

        /// <summary>Number of RCPs running</summary>
        public int RcpCount;

        /// <summary>RCP heat input (MW)</summary>
        public float RcpHeat;

        /// <summary>Individual RCP status</summary>
        public bool[] RcpRunning = new bool[4];

        // ====================================================================
        // PRESSURIZER
        // ====================================================================

        /// <summary>Pressurizer temperature (Â°F)</summary>
        public float T_pzr;

        /// <summary>Pressurizer level (%)</summary>
        public float PzrLevel;

        /// <summary>Pressurizer water volume (ftÂ³)</summary>
        public float PzrWaterVolume;

        /// <summary>Pressurizer steam volume (ftÂ³)</summary>
        public float PzrSteamVolume;

        /// <summary>Pressurizer heater power (MW)</summary>
        public float PzrHeaterPower;

        /// <summary>Are PZR heaters on?</summary>
        public bool PzrHeatersOn;

        /// <summary>Heater mode string</summary>
        public string HeaterMode;

        /// <summary>Is spray active?</summary>
        public bool SprayActive;

        /// <summary>Spray flow (gpm)</summary>
        public float SprayFlow;

        /// <summary>Spray valve position (0-1)</summary>
        public float SprayValvePosition;

        /// <summary>Surge flow (gpm, + = insurge)</summary>
        public float SurgeFlow;

        /// <summary>Has bubble formed?</summary>
        public bool BubbleFormed;

        /// <summary>Is pressurizer solid (no bubble)?</summary>
        public bool SolidPressurizer;

        /// <summary>Bubble formation phase</summary>
        public string BubblePhase;

        /// <summary>PZR subcooling to saturation (T_sat - T_pzr) in °F. Positive = subcooled, zero/negative = at/above saturation.</summary>
        public float PzrSubcooling;

        /// <summary>True when PZR is at or near saturation temperature (subcooling ≤ 5°F). Indicates bubble formation readiness.</summary>
        public bool PzrAtSaturation;

        // ====================================================================
        // CVCS
        // ====================================================================

        /// <summary>Charging flow (gpm)</summary>
        public float ChargingFlow;

        /// <summary>Letdown flow (gpm)</summary>
        public float LetdownFlow;

        /// <summary>Is charging active?</summary>
        public bool ChargingActive;

        /// <summary>Is letdown active?</summary>
        public bool LetdownActive;

        /// <summary>VCT level (%)</summary>
        public float VctLevel;

        /// <summary>VCT makeup active?</summary>
        public bool VctMakeupActive;

        /// <summary>VCT divert active?</summary>
        public bool VctDivertActive;

        /// <summary>Mass conservation error (lbm)</summary>
        public float MassError;

        /// <summary>Seal injection OK?</summary>
        public bool SealInjectionOK;

        // ====================================================================
        // STEAM GENERATORS / RHR
        // ====================================================================

        /// <summary>SG secondary pressure (psia)</summary>
        public float SgSecondaryPressure;

        /// <summary>SG saturation temperature (Â°F)</summary>
        public float SgSatTemp;

        /// <summary>SG bulk temperature (Â°F)</summary>
        public float SgBulkTemp;

        /// <summary>SG heat transfer (MW)</summary>
        public float SgHeatTransfer;

        /// <summary>Is SG boiling?</summary>
        public bool SgBoilingActive;

        /// <summary>Is steam dump active?</summary>
        public bool SteamDumpActive;

        /// <summary>Is RHR active?</summary>
        public bool RhrActive;

        /// <summary>RHR mode string</summary>
        public string RhrMode;

        /// <summary>RHR net heat effect (MW)</summary>
        public float RhrNetHeat;

        /// <summary>HZP progress (0-100%)</summary>
        public float HzpProgress;

        /// <summary>Is HZP stable?</summary>
        public bool HzpStable;

        // ====================================================================
        // BRS (Boron Recycle System)
        // ====================================================================

        /// <summary>BRS holdup tank level (%)</summary>
        public float BrsHoldupLevel;

        /// <summary>BRS distillate tank level (%)</summary>
        public float BrsDistillateLevel;

        // ====================================================================
        // ANNUNCIATOR STATES
        // ====================================================================

        public bool PzrLevelLow;
        public bool PzrLevelHigh;
        public bool PressureLow;
        public bool PressureHigh;
        public bool SubcoolingLow;
        public bool HeatupInProgress;
        public bool VctLevelLow;
        public bool VctLevelHigh;
        public bool RcsFlowLow;
        public bool CcwRunning;
        public bool ModePermissive;

        // ====================================================================
        // CAPTURE METHOD
        // ====================================================================

        /// <summary>
        /// Capture all values from engine into this snapshot.
        /// Called at refresh rate (10 Hz) from Update().
        /// </summary>
        public void CaptureFrom(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // Core parameters
            SimTime = engine.simTime;
            WallClockTime = engine.wallClockTime;
            PlantMode = engine.plantMode;
            HeatupPhaseDesc = engine.heatupPhaseDesc ?? "";
            SpeedIndex = engine.currentSpeedIndex;
            IsAccelerated = engine.isAccelerated;

            // RCS Primary
            T_avg = engine.T_avg;
            T_hot = engine.T_hot;
            T_cold = engine.T_cold;
            CoreDeltaT = T_hot - T_cold;
            T_rcs = engine.T_rcs;
            Pressure = engine.pressure;
            T_sat = engine.T_sat;
            Subcooling = engine.subcooling;
            HeatupRate = engine.heatupRate;
            PressureRate = engine.pressureRate;
            RcpCount = engine.rcpCount;
            RcpHeat = engine.rcpHeat;
            for (int i = 0; i < 4; i++)
            {
                RcpRunning[i] = engine.rcpRunning != null && i < engine.rcpRunning.Length && engine.rcpRunning[i];
            }

            // Pressurizer
            T_pzr = engine.T_pzr;
            PzrLevel = engine.pzrLevel;
            PzrWaterVolume = engine.pzrWaterVolume;
            PzrSteamVolume = engine.pzrSteamVolume;
            PzrHeaterPower = engine.pzrHeaterPower;
            PzrHeatersOn = engine.pzrHeatersOn;
            HeaterMode = engine.currentHeaterMode.ToString();
            SprayActive = engine.sprayActive;
            SprayFlow = engine.sprayFlow_GPM;
            SprayValvePosition = engine.sprayValvePosition;
            SurgeFlow = engine.surgeFlow;
            BubbleFormed = engine.bubbleFormed;
            SolidPressurizer = engine.solidPressurizer;
            BubblePhase = engine.bubblePhase.ToString();

            // PZR subcooling for bubble formation readiness (T_sat - T_pzr)
            // Positive = subcooled, zero/negative = at or above saturation
            PzrSubcooling = T_sat - T_pzr;
            PzrAtSaturation = PzrSubcooling <= 5f;

            // CVCS
            ChargingFlow = engine.chargingFlow;
            LetdownFlow = engine.letdownFlow;
            ChargingActive = engine.chargingActive;
            LetdownActive = engine.letdownActive;
            VctLevel = engine.vctState.Level;
            VctMakeupActive = engine.vctMakeupActive;
            VctDivertActive = engine.vctDivertActive;
            MassError = engine.massConservationError;
            SealInjectionOK = engine.sealInjectionOK;

            // Steam Generators / RHR
            SgSecondaryPressure = engine.sgSecondaryPressure_psia;
            SgSatTemp = engine.sgSaturationTemp_F;
            SgBulkTemp = engine.T_sg_secondary;
            SgHeatTransfer = engine.sgHeatTransfer_MW;
            SgBoilingActive = engine.sgBoilingActive;
            SteamDumpActive = engine.steamDumpActive;
            RhrActive = engine.rhrActive;
            RhrMode = engine.rhrModeString ?? "";
            RhrNetHeat = engine.rhrNetHeat_MW;
            HzpProgress = engine.hzpProgress;
            HzpStable = engine.hzpStable;

            // BRS
            BrsHoldupLevel = BRSPhysics.GetHoldupLevelPercent(engine.brsState);
            BrsDistillateLevel = engine.brsState.DistillateAvailable_gal > 0f 
                ? (engine.brsState.DistillateAvailable_gal / 5000f) * 100f  // Approximate distillate tank capacity
                : 0f;

            // Annunciators
            PzrLevelLow = engine.pzrLevelLow;
            PzrLevelHigh = engine.pzrLevelHigh;
            PressureLow = engine.pressureLow;
            PressureHigh = engine.pressureHigh;
            SubcoolingLow = engine.subcoolingLow;
            HeatupInProgress = engine.heatupInProgress;
            VctLevelLow = engine.vctLevelLow;
            VctLevelHigh = engine.vctLevelHigh;
            RcsFlowLow = engine.rcsFlowLow;
            CcwRunning = engine.ccwRunning;
            ModePermissive = engine.modePermissive;
        }
    }
}

