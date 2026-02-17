// CRITICAL: Master the Atom - Physics Module
// RCPSequencer.cs - Reactor Coolant Pump Startup Sequence
//
// Implements: Engine Architecture Audit Fix - Issue #6
//   Extracts RCP startup timing logic from engine to physics module
//
// PHYSICS:
//   Per NRC ML11223A342 Section 19.2.2:
//   "Prior to starting an RCP, a steam bubble must be established"
//   
//   RCP Startup Requirements:
//     1. Steam bubble must exist in pressurizer
//     2. Pressure must be at least 400 psig for adequate NPSH
//     3. Sequential start to limit electrical inrush
//   
//   Startup Sequence:
//     - First RCP: Started RCP1_START_DELAY (~10 min) after bubble formation
//     - Subsequent RCPs: Started at RCP_START_INTERVAL (30 min) spacing
//     - All 4 RCPs started within ~100 min of bubble formation
//
// Sources:
//   - NRC HRTD Section 19.2.2 - Plant Heatup Operations
//   - NRC HRTD Section 3.2 - RCP Operations
//   - Plant Operating Procedures - RCP Startup Precautions
//
// Units: hours for time

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Per-pump ramp-up state during staged RCP startup.
    /// Tracks individual pump progression through startup stages (0–4).
    /// v0.4.0 Issue #3: Replaces binary on/off RCP model with staged ramp-up.
    /// </summary>
    public struct RCPStartupState
    {
        /// <summary>Pump index (0–3)</summary>
        public int PumpIndex;
        /// <summary>Sim time when start command was issued (hours)</summary>
        public float StartTime_hr;
        /// <summary>Elapsed time since start command (hours)</summary>
        public float ElapsedSinceStart_hr;
        /// <summary>Current startup stage (0=pre-start, 1–4=ramping, 4=complete)</summary>
        public int CurrentStage;
        /// <summary>Effective flow fraction (0.0–1.0), interpolated within stage</summary>
        public float FlowFraction;
        /// <summary>Effective heat fraction (0.0–1.0), interpolated within stage</summary>
        public float HeatFraction;
        /// <summary>True when Stage 4 is complete and pump is at rated conditions</summary>
        public bool FullyRunning;
    }

    /// <summary>
    /// Aggregate RCP contribution across all pumps.
    /// v0.4.0 Issue #3: Provides blended flow/heat fractions for physics coupling.
    /// </summary>
    public struct RCPContribution
    {
        /// <summary>Aggregate flow fraction across all started pumps (0.0–N, where N = pumps started)</summary>
        public float TotalFlowFraction;
        /// <summary>Aggregate heat fraction across all started pumps (0.0–N)</summary>
        public float TotalHeatFraction;
        /// <summary>Effective total RCP heat input (MW) = TotalHeatFraction × RCP_HEAT_MW_EACH</summary>
        public float EffectiveHeat_MW;
        /// <summary>Effective total RCP flow (gpm) = TotalFlowFraction × RCP_FLOW_EACH</summary>
        public float EffectiveFlow_gpm;
        /// <summary>Number of pumps that have received a start command</summary>
        public int PumpsStarted;
        /// <summary>Number of pumps fully at rated conditions</summary>
        public int PumpsFullyRunning;
        /// <summary>True if all started pumps are fully ramped</summary>
        public bool AllFullyRunning;
    }

    /// <summary>
    /// RCP sequencer state container.
    /// </summary>
    public struct RCPSequencerState
    {
        public int TargetRCPCount;          // Target number of RCPs to be running
        public int CurrentRCPCount;         // Current number running
        public float TimeToNextStart;       // Hours until next RCP can start
        public bool[] RCPRunning;           // Status of each RCP (0-3)
        public bool CanStartRCP;            // True if conditions allow RCP start
        public bool BubbleRequired;         // True if waiting for bubble
        public bool PressureRequired;       // True if waiting for pressure
        public string StatusMessage;        // Human-readable status
    }
    
    /// <summary>
    /// RCP startup sequence controller.
    /// 
    /// This module manages the automatic sequencing of RCP starts during heatup.
    /// It enforces:
    ///   - Bubble must exist before first RCP start
    ///   - Minimum pressure for RCP NPSH
    ///   - Sequential start timing to limit electrical inrush
    /// 
    /// The engine should call GetTargetRCPCount() each timestep to determine
    /// how many RCPs should be running based on current conditions.
    /// </summary>
    public static class RCPSequencer
    {
        #region Constants
        
        /// <summary>
        /// Delay from bubble formation complete to first RCP start (hours).
        /// Per startup permissive policy: RCPs can start once bubble exists and P >= 400 psig.
        /// No mandated wait. 10-minute delay models operator verification activities
        /// (checking seal injection, aligning breakers, verifying prerequisites).
        /// v2.0.10: Changed from 1.0 hr (no procedural basis) to 10 min.
        /// </summary>
        public const float RCP1_START_DELAY = 10f / 60f;  // 10 minutes
        
        /// <summary>Interval between subsequent RCP starts (hours)</summary>
        public const float RCP_START_INTERVAL = 0.5f;
        
        /// <summary>Total number of RCPs in plant</summary>
        public const int TOTAL_RCP_COUNT = 4;
        
        #endregion
        
        #region Main Calculation
        
        /// <summary>
        /// Get target RCP count based on current conditions and timing.
        /// 
        /// This is the main entry point for the sequencer. It determines
        /// how many RCPs should be running based on:
        ///   - Whether a steam bubble exists
        ///   - System pressure (must be >= 400 psig)
        ///   - Time elapsed since bubble formation
        /// </summary>
        /// <param name="bubbleFormed">True if steam bubble exists in PZR</param>
        /// <param name="simTime">Current simulation time (hours)</param>
        /// <param name="bubbleFormationTime">Time when bubble formed (hours)</param>
        /// <param name="pressure_psia">Current RCS pressure (psia)</param>
        /// <returns>Target number of RCPs that should be running (0-4)</returns>
        public static int GetTargetRCPCount(
            bool bubbleFormed,
            float simTime,
            float bubbleFormationTime,
            float pressure_psia = PlantConstants.OPERATING_PRESSURE)
        {
            // Requirement 1: Bubble must exist
            if (!bubbleFormed)
                return 0;
            
            // Requirement 2: Pressure must be adequate for NPSH
            if (pressure_psia < PlantConstants.MIN_RCP_PRESSURE_PSIA)
                return 0;
            
            // Requirement 3: Wait for RCP1 start delay after bubble
            float timeSinceBubble = simTime - bubbleFormationTime;
            if (timeSinceBubble < RCP1_START_DELAY)
                return 0;
            
            // Sequential start timing
            float timeSinceFirstStart = timeSinceBubble - RCP1_START_DELAY;
            int targetCount = 1 + (int)Math.Floor(timeSinceFirstStart / RCP_START_INTERVAL);
            
            // Clamp to maximum
            return Math.Min(targetCount, TOTAL_RCP_COUNT);
        }
        
        /// <summary>
        /// Get detailed sequencer state for display and diagnostics.
        /// </summary>
        public static RCPSequencerState GetState(
            bool bubbleFormed,
            float simTime,
            float bubbleFormationTime,
            float pressure_psia,
            int currentRCPCount)
        {
            var state = new RCPSequencerState();
            state.CurrentRCPCount = currentRCPCount;
            state.RCPRunning = new bool[TOTAL_RCP_COUNT];
            for (int i = 0; i < TOTAL_RCP_COUNT; i++)
                state.RCPRunning[i] = (i < currentRCPCount);
            
            // Check requirements
            state.BubbleRequired = !bubbleFormed;
            state.PressureRequired = (pressure_psia < PlantConstants.MIN_RCP_PRESSURE_PSIA);
            state.CanStartRCP = bubbleFormed && !state.PressureRequired;
            
            if (!bubbleFormed)
            {
                state.TargetRCPCount = 0;
                state.TimeToNextStart = float.MaxValue;
                state.StatusMessage = "WAITING FOR BUBBLE";
                return state;
            }
            
            if (state.PressureRequired)
            {
                state.TargetRCPCount = 0;
                state.TimeToNextStart = float.MaxValue;
                state.StatusMessage = $"LOW PRESSURE ({pressure_psia - 14.7f:F0} psig < {PlantConstants.MIN_RCP_PRESSURE_PSIG:F0} psig)";
                return state;
            }
            
            // Calculate target and timing
            state.TargetRCPCount = GetTargetRCPCount(bubbleFormed, simTime, bubbleFormationTime, pressure_psia);
            
            float timeSinceBubble = simTime - bubbleFormationTime;
            
            if (timeSinceBubble < RCP1_START_DELAY)
            {
                state.TimeToNextStart = RCP1_START_DELAY - timeSinceBubble;
                state.StatusMessage = $"RCP START IN {state.TimeToNextStart * 60:F0} MIN";
            }
            else if (state.TargetRCPCount < TOTAL_RCP_COUNT)
            {
                float timeSinceFirstStart = timeSinceBubble - RCP1_START_DELAY;
                float timeInCurrentInterval = timeSinceFirstStart % RCP_START_INTERVAL;
                state.TimeToNextStart = RCP_START_INTERVAL - timeInCurrentInterval;
                state.StatusMessage = $"RCPs: {state.TargetRCPCount}/{TOTAL_RCP_COUNT} - NEXT IN {state.TimeToNextStart * 60:F0} MIN";
            }
            else
            {
                state.TimeToNextStart = 0f;
                state.StatusMessage = "ALL 4 RCPs RUNNING";
            }
            
            return state;
        }
        
        #endregion
        
        #region Event Detection
        
        /// <summary>
        /// Check if an RCP start event should occur this timestep.
        /// Returns the RCP number (1-4) that should start, or 0 if none.
        /// </summary>
        public static int CheckForStartEvent(
            bool bubbleFormed,
            float simTime,
            float bubbleFormationTime,
            float pressure_psia,
            int currentRCPCount)
        {
            int target = GetTargetRCPCount(bubbleFormed, simTime, bubbleFormationTime, pressure_psia);
            
            if (target > currentRCPCount)
            {
                return currentRCPCount + 1;  // Return the RCP number that should start
            }
            
            return 0;  // No start event
        }
        
        /// <summary>
        /// Get the time when a specific RCP should start.
        /// </summary>
        /// <param name="rcpNumber">RCP number (1-4)</param>
        /// <param name="bubbleFormationTime">Time when bubble formed</param>
        /// <returns>Scheduled start time in hours</returns>
        public static float GetScheduledStartTime(int rcpNumber, float bubbleFormationTime)
        {
            if (rcpNumber < 1 || rcpNumber > TOTAL_RCP_COUNT)
                return float.MaxValue;
            
            return bubbleFormationTime + RCP1_START_DELAY + (rcpNumber - 1) * RCP_START_INTERVAL;
        }
        
        #endregion
        
        #region Staged Ramp-Up — v0.4.0 Issue #3

        /// <summary>
        /// Update the ramp-up state for a single pump given elapsed time since its start.
        /// Linearly interpolates flow/heat fraction within each stage.
        /// 
        /// Stage progression:
        ///   Stage 1 (0 → 2 min):    flow 0→10%, heat 0→5%   (motor start)
        ///   Stage 2 (2 → 9.5 min):  flow 10→30%, heat 5→20% (low flow)
        ///   Stage 3 (9.5 → 22 min): flow 30→70%, heat 20→60% (moderate flow)
        ///   Stage 4 (22 → 39.5 min): flow 70→100%, heat 60→100% (full speed)
        /// 
        /// Source: NRC HRTD 3.2 / 19.2.2
        /// </summary>
        /// <param name="pumpIndex">Pump index (0–3)</param>
        /// <param name="startTime_hr">Sim time when start command was issued</param>
        /// <param name="currentTime_hr">Current sim time</param>
        /// <returns>Updated per-pump ramp state</returns>
        public static RCPStartupState UpdatePumpRampState(int pumpIndex, float startTime_hr, float currentTime_hr)
        {
            var state = new RCPStartupState();
            state.PumpIndex = pumpIndex;
            state.StartTime_hr = startTime_hr;
            state.ElapsedSinceStart_hr = currentTime_hr - startTime_hr;

            if (state.ElapsedSinceStart_hr < 0f)
            {
                // Not yet started
                state.CurrentStage = 0;
                state.FlowFraction = 0f;
                state.HeatFraction = 0f;
                state.FullyRunning = false;
                return state;
            }

            float elapsed = state.ElapsedSinceStart_hr;

            // Stage boundaries (cumulative)
            float t1End = PlantConstants.RCP_STAGE_1_DURATION_HR;
            float t2End = t1End + PlantConstants.RCP_STAGE_2_DURATION_HR;
            float t3End = t2End + PlantConstants.RCP_STAGE_3_DURATION_HR;
            float t4End = t3End + PlantConstants.RCP_STAGE_4_DURATION_HR;

            if (elapsed < t1End)
            {
                // Stage 1: 0 → Stage 1 fractions
                float progress = elapsed / PlantConstants.RCP_STAGE_1_DURATION_HR;
                state.CurrentStage = 1;
                state.FlowFraction = progress * PlantConstants.RCP_STAGE_1_FLOW_FRACTION;
                state.HeatFraction = progress * PlantConstants.RCP_STAGE_1_HEAT_FRACTION;
            }
            else if (elapsed < t2End)
            {
                // Stage 2: Stage 1 fractions → Stage 2 fractions
                float progress = (elapsed - t1End) / PlantConstants.RCP_STAGE_2_DURATION_HR;
                state.CurrentStage = 2;
                state.FlowFraction = PlantConstants.RCP_STAGE_1_FLOW_FRACTION +
                    progress * (PlantConstants.RCP_STAGE_2_FLOW_FRACTION - PlantConstants.RCP_STAGE_1_FLOW_FRACTION);
                state.HeatFraction = PlantConstants.RCP_STAGE_1_HEAT_FRACTION +
                    progress * (PlantConstants.RCP_STAGE_2_HEAT_FRACTION - PlantConstants.RCP_STAGE_1_HEAT_FRACTION);
            }
            else if (elapsed < t3End)
            {
                // Stage 3: Stage 2 fractions → Stage 3 fractions
                float progress = (elapsed - t2End) / PlantConstants.RCP_STAGE_3_DURATION_HR;
                state.CurrentStage = 3;
                state.FlowFraction = PlantConstants.RCP_STAGE_2_FLOW_FRACTION +
                    progress * (PlantConstants.RCP_STAGE_3_FLOW_FRACTION - PlantConstants.RCP_STAGE_2_FLOW_FRACTION);
                state.HeatFraction = PlantConstants.RCP_STAGE_2_HEAT_FRACTION +
                    progress * (PlantConstants.RCP_STAGE_3_HEAT_FRACTION - PlantConstants.RCP_STAGE_2_HEAT_FRACTION);
            }
            else if (elapsed < t4End)
            {
                // Stage 4: Stage 3 fractions → Stage 4 fractions (1.0)
                float progress = (elapsed - t3End) / PlantConstants.RCP_STAGE_4_DURATION_HR;
                state.CurrentStage = 4;
                state.FlowFraction = PlantConstants.RCP_STAGE_3_FLOW_FRACTION +
                    progress * (PlantConstants.RCP_STAGE_4_FLOW_FRACTION - PlantConstants.RCP_STAGE_3_FLOW_FRACTION);
                state.HeatFraction = PlantConstants.RCP_STAGE_3_HEAT_FRACTION +
                    progress * (PlantConstants.RCP_STAGE_4_HEAT_FRACTION - PlantConstants.RCP_STAGE_3_HEAT_FRACTION);
            }
            else
            {
                // Fully running
                state.CurrentStage = 4;
                state.FlowFraction = 1.0f;
                state.HeatFraction = 1.0f;
                state.FullyRunning = true;
            }

            return state;
        }

        /// <summary>
        /// Get aggregate effective RCP contribution across all pumps,
        /// accounting for each pump's individual ramp-up progress.
        /// 
        /// The engine tracks per-pump start times. This method calculates
        /// each pump's ramp state and aggregates the results.
        /// 
        /// Example: Pump 1 at Stage 3 (70% heat) + Pump 2 at Stage 1 (5% heat)
        ///   → EffectiveHeat = (0.70 + 0.05) × 5.25 MW = 3.94 MW
        /// 
        /// Source: NRC HRTD 3.2 / 19.2.2
        /// </summary>
        /// <param name="pumpStartTimes">Start time for each pump (hours). Use float.MaxValue for not-yet-started pumps.</param>
        /// <param name="currentTime_hr">Current sim time (hours)</param>
        /// <param name="pumpCount">Number of pumps that have received start commands</param>
        /// <returns>Aggregate RCP contribution with blended fractions</returns>
        public static RCPContribution GetEffectiveRCPContribution(
            float[] pumpStartTimes, float currentTime_hr, int pumpCount)
        {
            var result = new RCPContribution();
            result.PumpsStarted = pumpCount;

            for (int i = 0; i < pumpCount && i < TOTAL_RCP_COUNT; i++)
            {
                if (pumpStartTimes[i] > currentTime_hr)
                    continue;  // Not yet started

                var pumpState = UpdatePumpRampState(i, pumpStartTimes[i], currentTime_hr);
                result.TotalFlowFraction += pumpState.FlowFraction;
                result.TotalHeatFraction += pumpState.HeatFraction;

                if (pumpState.FullyRunning)
                    result.PumpsFullyRunning++;
            }

            result.EffectiveHeat_MW = result.TotalHeatFraction * PlantConstants.RCP_HEAT_MW_EACH;
            result.EffectiveFlow_gpm = result.TotalFlowFraction * PlantConstants.RCP_FLOW_EACH;
            result.AllFullyRunning = (result.PumpsFullyRunning == pumpCount) && (pumpCount > 0);

            return result;
        }

        #endregion

        #region Heat Input Calculation
        
        /// <summary>
        /// Calculate total RCP heat input for given number of running pumps.
        /// </summary>
        public static float GetRCPHeat_MW(int rcpCount)
        {
            return rcpCount * PlantConstants.RCP_HEAT_MW_EACH;
        }
        
        /// <summary>
        /// Get heater power setting based on RCP count.
        /// During heatup, heaters typically run at full power until RCPs provide sufficient heat.
        /// </summary>
        public static float GetHeaterPower_MW(int rcpCount, bool heatersEnabled = true)
        {
            if (!heatersEnabled)
                return 0f;
            
            // PZR heaters remain at 1.8 MW during heatup
            return PlantConstants.HEATER_POWER_TOTAL / 1000f;  // kW to MW
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate RCP sequencer logic.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // v2.0.10: Tests updated for RCP1_START_DELAY = 10/60 hr (~0.1667 hr)
            // Bubble formation at t=1.0 hr; first RCP start at t=1.0 + 0.1667 = 1.1667 hr
            
            // Test 1: No RCPs without bubble
            int count1 = GetTargetRCPCount(false, 10f, 0f);
            if (count1 != 0) valid = false;
            
            // Test 2: No RCPs before delay expires (5 min < 10 min delay)
            int count2 = GetTargetRCPCount(true, 1.083f, 1.0f);  // 5 min (0.083 hr) since bubble
            if (count2 != 0) valid = false;
            
            // Test 3: First RCP after 10-min delay
            int count3 = GetTargetRCPCount(true, 1.2f, 1.0f);  // 12 min since bubble (> 10 min)
            if (count3 != 1) valid = false;
            
            // Test 4: Second RCP after first interval (10 min + 30 min = 40 min)
            int count4 = GetTargetRCPCount(true, 1.7f, 1.0f);  // 42 min since bubble
            if (count4 != 2) valid = false;
            
            // Test 5: All 4 RCPs eventually
            int count5 = GetTargetRCPCount(true, 5.0f, 1.0f);  // 4 hr since bubble
            if (count5 != 4) valid = false;
            
            // Test 6: Low pressure blocks RCP start
            int count6 = GetTargetRCPCount(true, 5.0f, 1.0f, 300f);  // Low pressure
            if (count6 != 0) valid = false;
            
            // Test 7: Scheduled start times are correct with new delay
            // RCP1: bubbleTime + RCP1_START_DELAY = 1.0 + 10/60 = 1.1667
            // RCP2: bubbleTime + RCP1_START_DELAY + RCP_START_INTERVAL = 1.0 + 10/60 + 0.5 = 1.6667
            float t1 = GetScheduledStartTime(1, 1.0f);
            float t2 = GetScheduledStartTime(2, 1.0f);
            float expectedT1 = 1.0f + 10f / 60f;           // ~1.1667
            float expectedT2 = 1.0f + 10f / 60f + 0.5f;    // ~1.6667
            if (Math.Abs(t1 - expectedT1) > 0.01f) valid = false;
            if (Math.Abs(t2 - expectedT2) > 0.01f) valid = false;
            
            // Test 8: RCP heat calculation
            float heat4 = GetRCPHeat_MW(4);
            if (Math.Abs(heat4 - 21f) > 0.1f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
