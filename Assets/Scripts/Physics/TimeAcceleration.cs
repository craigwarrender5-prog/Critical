// ============================================================================
// CRITICAL: Master the Atom - Time Acceleration Module
// ============================================================================
// 
// FILE: TimeAcceleration.cs
// PUT IN: Assets/Scripts/
//
// PURPOSE:
//   Manages dual time tracking (wall-clock vs simulation time) and provides
//   MSFS-style time acceleration with discrete speed steps.
//   
//   Like MSFS when you're over the ocean, there are periods in a PWR heatup
//   (especially the solid-pressurizer phase, waiting for bubble formation)
//   where the operator may want to accelerate time to reach the interesting
//   parts faster.
//
// DESIGN:
//   - Wall-Clock Time: Actual elapsed real-world time since simulation start.
//     Always advances at 1:1 with the system clock. This tells you "how long
//     have I been sitting here watching this."
//
//   - Simulation Time: How far the plant has progressed through its heatup.
//     At 1x, sim time = wall time. At 4x, every real second advances the
//     plant state by 4 simulated seconds.
//
//   - Speed Steps: Discrete multipliers (1x, 2x, 4x, 8x, 10x) selectable
//     via the dashboard dropdown. Matches the MSFS convention of discrete
//     warp steps rather than a continuous slider.
//
// PHYSICS SAFETY:
//   The physics timestep (dt) is NOT changed by acceleration. Instead,
//   more physics steps are executed per real frame. This preserves numerical
//   stability at all warp speeds. The engine's existing simTimeBudget
//   approach already supports this — we simply feed it more sim-seconds
//   per real frame.
//
//   At 10x on a 60fps display, we execute ~10 physics steps per frame
//   (at dt = 10s sim-time each = 1/360 hr). This is well within the
//   existing maxStepsPerFrame = 50 budget.
//
// INTEGRATION:
//   HeatupSimEngine reads TimeAcceleration.SimDeltaTime each frame
//   instead of computing its own. The Visual dashboard reads the public
//   state for display and provides the dropdown UI for speed selection.
//
// ============================================================================

using UnityEngine;
using System;

namespace Critical.Physics
{
    /// <summary>
    /// Time acceleration controller for the PWR heatup simulation.
    /// Tracks wall-clock time independently from simulation time and provides
    /// discrete speed multipliers for time warp.
    /// </summary>
    public static class TimeAcceleration
    {
        // ====================================================================
        // SPEED STEPS — Discrete multipliers, MSFS-style
        // ====================================================================
        
        /// <summary>Available time warp multipliers. Index 0 = real-time.</summary>
        public static readonly float[] SpeedSteps = { 1f, 2f, 4f, 8f, 10f };
        
        /// <summary>Display labels for the dropdown menu.</summary>
        public static readonly string[] SpeedLabels = { "1x (Real-Time)", "2x", "4x", "8x", "10x" };
        
        /// <summary>Short labels for the header bar display.</summary>
        public static readonly string[] SpeedLabelsShort = { "1x RT", "2x", "4x", "8x", "10x" };
        
        // ====================================================================
        // STATE
        // ====================================================================
        
        /// <summary>Currently selected speed step index (0 = real-time).</summary>
        public static int CurrentSpeedIndex { get; private set; } = 0;
        
        /// <summary>Current speed multiplier (1.0 at real-time).</summary>
        public static float CurrentMultiplier => SpeedSteps[CurrentSpeedIndex];
        
        /// <summary>True when running at 1x (real-time). False when accelerated.</summary>
        public static bool IsRealTime => CurrentSpeedIndex == 0;
        
        /// <summary>
        /// Wall-clock time elapsed since simulation start, in hours.
        /// Always advances at 1:1 with the system clock regardless of warp speed.
        /// This answers: "How long have I been sitting here?"
        /// </summary>
        public static float WallClockTime_Hours { get; private set; } = 0f;
        
        /// <summary>
        /// Simulation time elapsed, in hours.
        /// At 1x this matches wall time. At 4x, advances 4x faster.
        /// This answers: "How far has the plant progressed?"
        /// Read by HeatupSimEngine as the master sim time source.
        /// </summary>
        public static float SimulationTime_Hours { get; private set; } = 0f;
        
        /// <summary>
        /// The simulation time delta to apply this frame, in hours.
        /// = Time.deltaTime * CurrentMultiplier / 3600
        /// The engine reads this instead of computing its own.
        /// </summary>
        public static float SimDeltaTime_Hours { get; private set; } = 0f;
        
        /// <summary>
        /// Wall-clock time delta this frame, in hours.
        /// = Time.deltaTime / 3600
        /// </summary>
        public static float WallDeltaTime_Hours { get; private set; } = 0f;
        
        /// <summary>
        /// The DateTime when the simulation was started/reset.
        /// Used for absolute wall-clock timestamps in logs.
        /// </summary>
        public static DateTime StartRealTime { get; private set; }
        
        /// <summary>
        /// Cumulative ratio: total sim time / total wall time.
        /// Shows overall effective acceleration including periods at different speeds.
        /// Returns 1.0 if wall time is zero.
        /// </summary>
        public static float EffectiveMultiplier =>
            WallClockTime_Hours > 0.0001f ? SimulationTime_Hours / WallClockTime_Hours : 1f;
        
        // ====================================================================
        // INITIALIZATION
        // ====================================================================
        
        /// <summary>
        /// Reset all time tracking. Call at simulation start.
        /// Optionally set an initial speed index (default 0 = real-time).
        /// </summary>
        public static void Initialize(int startSpeedIndex = 0)
        {
            CurrentSpeedIndex = Mathf.Clamp(startSpeedIndex, 0, SpeedSteps.Length - 1);
            WallClockTime_Hours = 0f;
            SimulationTime_Hours = 0f;
            SimDeltaTime_Hours = 0f;
            WallDeltaTime_Hours = 0f;
            StartRealTime = DateTime.Now;
        }
        
        // ====================================================================
        // PER-FRAME UPDATE — Called once per frame by the engine
        // ====================================================================
        
        /// <summary>
        /// Advance both time tracks by one frame. Call once per frame from
        /// the simulation engine's update loop (before physics stepping).
        /// 
        /// Uses Time.deltaTime for the real-time base, then applies the
        /// current multiplier for simulation time.
        /// </summary>
        public static void Tick()
        {
            float realDt = Time.deltaTime;
            
            // Wall clock always advances at 1:1
            WallDeltaTime_Hours = realDt / 3600f;
            WallClockTime_Hours += WallDeltaTime_Hours;
            
            // Simulation time advances at the selected multiplier
            SimDeltaTime_Hours = realDt * CurrentMultiplier / 3600f;
            SimulationTime_Hours += SimDeltaTime_Hours;
        }
        
        /// <summary>
        /// Overload for manual dt injection (e.g., fixed timestep testing).
        /// realDeltaSeconds is the actual elapsed real time this tick.
        /// </summary>
        public static void Tick(float realDeltaSeconds)
        {
            WallDeltaTime_Hours = realDeltaSeconds / 3600f;
            WallClockTime_Hours += WallDeltaTime_Hours;
            
            SimDeltaTime_Hours = realDeltaSeconds * CurrentMultiplier / 3600f;
            SimulationTime_Hours += SimDeltaTime_Hours;
        }
        
        // ====================================================================
        // SPEED CONTROL
        // ====================================================================
        
        /// <summary>
        /// Set the speed by index into SpeedSteps array.
        /// Clamped to valid range. Index 0 = real-time.
        /// </summary>
        public static void SetSpeed(int index)
        {
            CurrentSpeedIndex = Mathf.Clamp(index, 0, SpeedSteps.Length - 1);
        }
        
        /// <summary>
        /// Set speed to the nearest available multiplier.
        /// E.g., SetSpeedByMultiplier(4f) selects the 4x step.
        /// </summary>
        public static void SetSpeedByMultiplier(float multiplier)
        {
            int bestIndex = 0;
            float bestDelta = float.MaxValue;
            for (int i = 0; i < SpeedSteps.Length; i++)
            {
                float delta = Mathf.Abs(SpeedSteps[i] - multiplier);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestIndex = i;
                }
            }
            CurrentSpeedIndex = bestIndex;
        }
        
        /// <summary>Increase speed one step (if not already at max).</summary>
        public static void SpeedUp()
        {
            SetSpeed(CurrentSpeedIndex + 1);
        }
        
        /// <summary>Decrease speed one step (if not already at 1x).</summary>
        public static void SlowDown()
        {
            SetSpeed(CurrentSpeedIndex - 1);
        }
        
        /// <summary>Return to real-time (1x).</summary>
        public static void ResetToRealTime()
        {
            CurrentSpeedIndex = 0;
        }
        
        // ====================================================================
        // FORMATTING HELPERS — For the dashboard display
        // ====================================================================
        
        /// <summary>
        /// Format hours as HH:MM:SS string. Used for both time displays.
        /// </summary>
        public static string FormatTime(float hours)
        {
            if (hours < 0f) hours = 0f;
            int totalSeconds = Mathf.FloorToInt(hours * 3600f);
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return $"{h:D2}:{m:D2}:{s:D2}";
        }
        
        /// <summary>
        /// Format hours as compact H:MM string (for graph axes, etc).
        /// </summary>
        public static string FormatTimeCompact(float hours)
        {
            if (hours < 0f) hours = 0f;
            int totalMinutes = Mathf.FloorToInt(hours * 60f);
            int h = totalMinutes / 60;
            int m = totalMinutes % 60;
            return $"{h}:{m:D2}";
        }
        
        /// <summary>
        /// Returns a status string for the header bar.
        /// E.g., "4x WARP" or "REAL-TIME"
        /// </summary>
        public static string GetStatusString()
        {
            if (IsRealTime)
                return "REAL-TIME";
            else
                return $"{CurrentMultiplier:F0}x WARP";
        }
        
        /// <summary>
        /// Returns a detailed status for the time panel.
        /// E.g., "Wall: 00:12:34  |  Sim: 00:50:16  |  4x WARP"
        /// </summary>
        public static string GetDetailedStatus()
        {
            return $"Wall: {FormatTime(WallClockTime_Hours)}  |  " +
                   $"Sim: {FormatTime(SimulationTime_Hours)}  |  " +
                   GetStatusString();
        }
        
        // ====================================================================
        // SYNC HELPER — For engine integration
        // ====================================================================
        
        /// <summary>
        /// Sync the simulation time from the engine's simTime value.
        /// Called once at initialization if the engine has a non-zero start time.
        /// After init, the engine should use SimulationTime_Hours as its
        /// authoritative sim time source.
        /// </summary>
        public static void SyncSimTime(float engineSimTime_Hours)
        {
            SimulationTime_Hours = engineSimTime_Hours;
        }
    }
}
