// ============================================================================
// CRITICAL: Master the Atom - Physics Module
// BRSPhysics.cs - Boron Recycle System Physics
// ============================================================================
//
// PURPOSE:
//   Models the Boron Recycle System (BRS) as a simplified buffer tank and
//   batch evaporator system. Closes the primary coolant inventory loop by
//   providing a destination for VCT divert flow (LCV-112A) and a return
//   path for processed water (distillate â†’ VCT makeup, concentrate â†’ BAT).
//
// PHYSICS:
//   Holdup tank mass balance:
//     dV_holdup/dt = Q_divert_in - Q_evaporator_out
//   Boron mass balance (mixing equation):
//     C_holdup = (V_holdup Ã— C_holdup + Q_in Ã— dt Ã— C_in) / (V_holdup + Q_in Ã— dt)
//   Evaporator mass/boron balance:
//     F = D + C  (feed = distillate + concentrate, mass basis)
//     F Ã— Cf = D Ã— Cd + C Ã— Cc
//     where Cd â‰ˆ 0 ppm (demineralised condensate), Cc â‰ˆ 7000 ppm (4 wt%)
//     Distillate fraction: D/F = 1 - Cf / Cc
//     Concentrate fraction: C/F = Cf / Cc
//
// SOURCES:
//   - NRC HRTD 4.1 (ML11223A214) â€” Sections 4.1.2.6 (BRS description),
//     4.1.3.1 (LCV-112A), Fig 4.1-3 (BRS flow), Fig 4.1-4 (evaporator)
//   - Callaway FSAR Chapter 11 (ML21195A182) â€” Fig 11.1A-2 (BRS design
//     parameters: 56,000 gal holdup, 21,600 gpd evaporator, DFs)
//   - NRC HRTD 15.1 (ML11223A332) â€” Table 15.1-2 (evaporator capacities)
//
// UNITS:
//   Volume: gallons | Flow: gpm | Concentration: ppm | Mass: lb
//   Time: hours (dt parameter) | Boron mass: lb (ppm Ã— gal Ã— 8.34e-6)
//
// ARCHITECTURE:
//   - Called by: HeatupSimEngine.CVCS.cs (coordinates VCTâ†”BRS transfers)
//   - Delegates to: PlantConstants.BRS.cs (all design constants)
//   - State owned: BRSState struct (holdup volume, processing, products)
//   - No direct coupling to VCTPhysics â€” engine coordinates all transfers
//
// GOLD STANDARD: Yes
// ============================================================================

using System;
using UnityEngine;

namespace Critical.Physics
{
    // ========================================================================
    // STATE STRUCT (G4)
    // Owned by the engine, passed by ref to update methods.
    // ========================================================================

    /// <summary>
    /// Persistent state for the Boron Recycle System.
    /// Created by Initialize(), updated by ReceiveDivert/UpdateProcessing/
    /// WithdrawDistillate, read by engine for logging and display.
    /// </summary>
    public struct BRSState
    {
        // --- Holdup Tank State ---
        public float HoldupVolume_gal;          // gal â€” Current water volume in holdup tanks
        public float HoldupBoronConc_ppm;       // ppm â€” Boron concentration of holdup inventory
        public float HoldupBoronMass_lb;        // lb  â€” Boron mass in holdup tanks

        // --- Evaporator Processing State ---
        public bool ProcessingActive;           // Evaporator feed pump running
        public float EvaporatorFeedRate_gpm;    // gpm â€” Current feed rate to evaporator
        public float DistillateRate_gpm;        // gpm â€” Clean water output rate
        public float ConcentrateRate_gpm;       // gpm â€” Concentrated boric acid output rate

        // --- Processed Inventory (Available for Return) ---
        public float DistillateAvailable_gal;   // gal â€” Clean water in monitor tanks
        public float ConcentrateAvailable_gal;  // gal â€” Concentrated boric acid in BAT
        public float DistillateBoron_ppm;       // ppm â€” Should be â‰ˆ 0 (clean condensate)
        public float ConcentrateBoron_ppm;      // ppm â€” Should be â‰ˆ 7000 (4 wt% boric acid)

        // --- Flow Tracking ---
        public float InFlow_gpm;                // gpm â€” Current inflow from VCT divert
        public float ReturnFlow_gpm;            // gpm â€” Current return to VCT
        public float CumulativeIn_gal;          // gal â€” Total received from VCT divert
        public float CumulativeProcessed_gal;   // gal â€” Total processed through evaporator
        public float CumulativeDistillate_gal;  // gal â€” Total clean water produced
        public float CumulativeConcentrate_gal; // gal â€” Total boric acid concentrate produced
        public float CumulativeReturned_gal;    // gal â€” Total returned to plant systems

        // --- Alarms ---
        public bool HoldupHighLevel;            // Holdup tanks approaching capacity
        public bool HoldupLowLevel;             // Holdup tanks near empty
    }

    // ========================================================================
    // MODULE CLASS
    // ========================================================================

    /// <summary>
    /// Boron Recycle System physics: holdup tank buffering, batch evaporator
    /// processing, and processed water return paths.
    ///
    /// Called by HeatupSimEngine.CVCS.cs. State owned by engine.
    /// See file header for physics basis and NRC/FSAR sources.
    /// </summary>
    public static class BRSPhysics
    {
        // ====================================================================
        // CONSTANTS (G5, G6)
        // Module-specific constants. Shared constants from PlantConstants.BRS.
        // ====================================================================

        #region Constants

        /// <summary>
        /// Boron-to-mass conversion factor: ppm Ã— gallons Ã— factor = lb boron.
        /// 1 ppm = 1 mg/kg. Water density â‰ˆ 8.34 lb/gal.
        /// Mass_lb = ppm Ã— gal Ã— 8.34 / 1,000,000 = ppm Ã— gal Ã— 8.34e-6.
        /// Source: Standard chemistry unit conversion.
        /// </summary>
        private const float BORON_MASS_FACTOR = 8.34e-6f;

        #endregion

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        #region Public API

        /// <summary>
        /// Initialize BRS state for cold shutdown.
        /// Holdup tanks start empty (no prior processing). Boron concentration
        /// set to match RCS so residual piping fluid is at equilibrium.
        /// Called once at simulation start by HeatupSimEngine.Init.cs.
        /// </summary>
        /// <param name="initialBoronConc_ppm">RCS boron concentration at init (ppm)</param>
        /// <returns>Initialized BRS state struct</returns>
        public static BRSState Initialize(float initialBoronConc_ppm)
        {
            var state = new BRSState();

            // Holdup tanks empty at cold shutdown start
            state.HoldupVolume_gal = 0f;
            state.HoldupBoronConc_ppm = initialBoronConc_ppm;
            state.HoldupBoronMass_lb = 0f;

            // Evaporator idle
            state.ProcessingActive = false;
            state.EvaporatorFeedRate_gpm = 0f;
            state.DistillateRate_gpm = 0f;
            state.ConcentrateRate_gpm = 0f;

            // No processed water available
            state.DistillateAvailable_gal = 0f;
            state.ConcentrateAvailable_gal = 0f;
            state.DistillateBoron_ppm = PlantConstants.BRS_DISTILLATE_BORON_PPM;
            state.ConcentrateBoron_ppm = PlantConstants.BRS_CONCENTRATE_BORON_PPM;

            // Flow tracking zeroed
            state.InFlow_gpm = 0f;
            state.ReturnFlow_gpm = 0f;
            state.CumulativeIn_gal = 0f;
            state.CumulativeProcessed_gal = 0f;
            state.CumulativeDistillate_gal = 0f;
            state.CumulativeConcentrate_gal = 0f;
            state.CumulativeReturned_gal = 0f;

            // No alarms
            state.HoldupHighLevel = false;
            state.HoldupLowLevel = false;

            return state;
        }

        /// <summary>
        /// Receive diverted letdown flow from VCT (via LCV-112A).
        /// Updates holdup tank volume and boron concentration using mixing equation.
        /// Called by the engine each timestep when VCT divert is active.
        /// </summary>
        /// <param name="state">BRS state (modified in place)</param>
        /// <param name="divertVolume_gal">Volume diverted this timestep (gal)</param>
        /// <param name="divertBoronConc_ppm">Boron concentration of diverted water (ppm)</param>
        public static void ReceiveDivert(ref BRSState state, float divertVolume_gal, float divertBoronConc_ppm)
        {
            if (divertVolume_gal <= 0f)
            {
                state.InFlow_gpm = 0f;
                return;
            }

            // Capacity check: clamp to usable capacity
            float availableCapacity = PlantConstants.BRS_HOLDUP_USABLE_CAPACITY_GAL - state.HoldupVolume_gal;
            float actualReceived = Mathf.Min(divertVolume_gal, availableCapacity);

            if (actualReceived <= 0f)
            {
                // Holdup tanks full â€” cannot accept divert flow
                state.InFlow_gpm = 0f;
                return;
            }

            // Mixing equation for boron concentration
            // C_new = (V_old Ã— C_old + V_add Ã— C_add) / (V_old + V_add)
            float oldBoronMass = state.HoldupVolume_gal * state.HoldupBoronConc_ppm;
            float addBoronMass = actualReceived * divertBoronConc_ppm;
            float newVolume = state.HoldupVolume_gal + actualReceived;

            state.HoldupVolume_gal = newVolume;
            state.HoldupBoronConc_ppm = newVolume > 0f
                ? (oldBoronMass + addBoronMass) / newVolume
                : divertBoronConc_ppm;
            state.HoldupBoronMass_lb = state.HoldupBoronConc_ppm * state.HoldupVolume_gal * BORON_MASS_FACTOR;

            // Tracking
            state.CumulativeIn_gal += actualReceived;
        }

        /// <summary>
        /// Advance evaporator batch processing by one timestep.
        /// When holdup volume exceeds minimum batch threshold, evaporator runs
        /// at rated capacity, splitting feed into distillate (â‰ˆ 0 ppm) and
        /// concentrate (â‰ˆ 7000 ppm). Products accumulate in monitor tanks / BAT.
        /// Called every timestep by the engine.
        /// </summary>
        /// <param name="state">BRS state (modified in place)</param>
        /// <param name="dt">Timestep in hours</param>
        public static void UpdateProcessing(ref BRSState state, float dt)
        {
            // Determine if evaporator should run
            // Start: holdup above minimum batch volume
            // Stop: holdup below low-level setpoint (prevent cavitation)
            float holdupPercent = (state.HoldupVolume_gal / PlantConstants.BRS_HOLDUP_USABLE_CAPACITY_GAL) * 100f;

            if (!state.ProcessingActive &&
                state.HoldupVolume_gal >= PlantConstants.BRS_EVAPORATOR_MIN_BATCH_GAL)
            {
                state.ProcessingActive = true;
            }
            else if (state.ProcessingActive &&
                     holdupPercent <= PlantConstants.BRS_HOLDUP_LOW_LEVEL_PCT)
            {
                state.ProcessingActive = false;
            }

            if (state.ProcessingActive && state.HoldupVolume_gal > 0f)
            {
                // Evaporator feed rate: rated capacity or limited by available volume
                float maxProcessVolume = state.HoldupVolume_gal;
                float ratedVolume = PlantConstants.BRS_EVAPORATOR_RATE_GPM * dt * 60f; // gpm Ã— hr Ã— 60 min/hr = gal
                float processVolume = Mathf.Min(ratedVolume, maxProcessVolume);
                float effectiveRate = processVolume / (dt * 60f); // back-calculate effective gpm

                state.EvaporatorFeedRate_gpm = effectiveRate;

                // Evaporator mass/boron balance:
                // Feed at C_holdup â†’ Distillate at â‰ˆ 0 ppm + Concentrate at 7000 ppm
                // Distillate fraction = 1 - C_holdup / C_concentrate
                // Concentrate fraction = C_holdup / C_concentrate
                float concBoron = PlantConstants.BRS_CONCENTRATE_BORON_PPM;
                float feedBoron = state.HoldupBoronConc_ppm;

                // Guard against edge case where holdup boron exceeds concentrate target
                float concentrateFraction = Mathf.Clamp01(feedBoron / concBoron);
                float distillateFraction = 1f - concentrateFraction;

                float distillateVolume = processVolume * distillateFraction;
                float concentrateVolume = processVolume * concentrateFraction;

                state.DistillateRate_gpm = effectiveRate * distillateFraction;
                state.ConcentrateRate_gpm = effectiveRate * concentrateFraction;

                // Remove processed volume from holdup
                state.HoldupVolume_gal -= processVolume;
                state.HoldupVolume_gal = Mathf.Max(0f, state.HoldupVolume_gal);

                // Holdup boron concentration unchanged by removal (well-mixed assumption
                // per NRC HRTD 4.1 â€” recirculation pump maintains homogeneity)
                state.HoldupBoronMass_lb = state.HoldupBoronConc_ppm * state.HoldupVolume_gal * BORON_MASS_FACTOR;

                // Accumulate products
                state.DistillateAvailable_gal += distillateVolume;
                state.ConcentrateAvailable_gal += concentrateVolume;

                // Tracking
                state.CumulativeProcessed_gal += processVolume;
                state.CumulativeDistillate_gal += distillateVolume;
                state.CumulativeConcentrate_gal += concentrateVolume;
            }
            else
            {
                state.EvaporatorFeedRate_gpm = 0f;
                state.DistillateRate_gpm = 0f;
                state.ConcentrateRate_gpm = 0f;
            }

            // Update alarms
            UpdateAlarms(ref state);
        }

        /// <summary>
        /// Withdraw processed distillate for use as VCT makeup water.
        /// Returns the actual volume withdrawn (may be less than requested
        /// if distillate inventory is insufficient).
        /// Called by the engine when VCT auto-makeup triggers and BRS
        /// distillate is available as the preferred makeup source.
        /// </summary>
        /// <param name="state">BRS state (modified in place)</param>
        /// <param name="requestedVolume_gal">Volume requested by VCT makeup (gal)</param>
        /// <returns>Actual volume withdrawn (gal)</returns>
        public static float WithdrawDistillate(ref BRSState state, float requestedVolume_gal)
        {
            if (requestedVolume_gal <= 0f || state.DistillateAvailable_gal <= 0f)
            {
                state.ReturnFlow_gpm = 0f;
                return 0f;
            }

            float actualWithdraw = Mathf.Min(requestedVolume_gal, state.DistillateAvailable_gal);
            state.DistillateAvailable_gal -= actualWithdraw;
            state.CumulativeReturned_gal += actualWithdraw;

            return actualWithdraw;
        }

        /// <summary>
        /// Get the holdup tank level as a percentage of usable capacity.
        /// </summary>
        public static float GetHoldupLevelPercent(BRSState state)
        {
            if (PlantConstants.BRS_HOLDUP_USABLE_CAPACITY_GAL <= 0f) return 0f;
            return (state.HoldupVolume_gal / PlantConstants.BRS_HOLDUP_USABLE_CAPACITY_GAL) * 100f;
        }

        /// <summary>
        /// Get a human-readable status string for the BRS.
        /// </summary>
        public static string GetStatusString(BRSState state)
        {
            if (state.HoldupHighLevel) return "HIGH LEVEL";
            if (state.ProcessingActive) return "PROCESSING";
            if (state.HoldupVolume_gal > 0f) return "HOLDING";
            return "IDLE";
        }

        #endregion

        // ====================================================================
        // PRIVATE METHODS
        // ====================================================================

        #region Private Methods

        /// <summary>
        /// Update holdup tank level alarms.
        /// </summary>
        private static void UpdateAlarms(ref BRSState state)
        {
            float holdupPercent = GetHoldupLevelPercent(state);
            state.HoldupHighLevel = holdupPercent >= PlantConstants.BRS_HOLDUP_HIGH_LEVEL_PCT;
            state.HoldupLowLevel = state.ProcessingActive && holdupPercent <= PlantConstants.BRS_HOLDUP_LOW_LEVEL_PCT;
        }

        #endregion
    }
}


