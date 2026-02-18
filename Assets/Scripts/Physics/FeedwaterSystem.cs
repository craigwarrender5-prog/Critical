// ============================================================================
// CRITICAL: Master the Atom — Feedwater System Physics Module
// FeedwaterSystem.cs — Hotwell, Condensate/AFW Pumps, CST, Return Flow
// ============================================================================
//
// PURPOSE:
//   Tracks secondary-side water inventory through the condenser/feedwater
//   return path during startup. Models the closed-loop mass accounting:
//   steam dumped to condenser condenses in hotwell, returns via condensate
//   or AFW pumps to SGs. Steam dumped to atmosphere represents permanent
//   CST inventory loss.
//
// MODELING LEVEL:
//   For startup simulation, the feedwater train is modeled at inventory
//   and flow availability level, not at full thermal-hydraulic detail.
//   The key question answered: "Is there feedwater return capacity available,
//   and what is the mass flow rate?"
//
//   The detailed LP/HP heater thermal chain is NOT modeled. Feedwater
//   temperature is parameterized based on startup phase:
//     - Pre-heater operation: T_fw ≈ T_hotwell (~120°F)
//     - With heaters (>20% power): T_fw per documented profile (120→440°F)
//
// PHYSICS:
//   Hotwell Mass Balance:
//     dM/dt = m_dot_condensed - m_dot_pumps_out + m_dot_cst_makeup - m_dot_cst_reject
//
//   Hotwell Level Control (documented setpoints):
//     Normal: 24 in. | Reject opens: 28 in. | Reject full: 40 in.
//     Makeup opens: 21 in. | Makeup full: 8 in.
//
//   CST Balance:
//     dV/dt = V_reject - V_makeup - V_afw_draw
//
//   Pump Models (simplified on/off with rated capacity):
//     Condensate: 2 × 11,000 gpm | MFP: 2 × 19,800 gpm
//     Motor AFW: 2 × 440 gpm | Turbine AFW: 1 × 880 gpm
//     Startup AFW: 1 × 1,020 gpm
//
// SOURCES:
//   - NRC HRTD Section 7.2 — Condensate and Feedwater System
//   - NRC HRTD Section 5.7 — Auxiliary Feedwater System
//   - NRC HRTD Condenser System Reference
//   - Technical_Documentation/Condenser_Feedwater_Architecture_Specification.md
//
// UNITS:
//   Mass: lb | Flow: lb/hr and gpm | Volume: gallons
//   Temperature: °F | Level: inches | Time: hours
//
// ARCHITECTURE:
//   - Called by: HeatupSimEngine.StepSimulation() (Stage I integration)
//   - Uses constants from: PlantConstants.Condenser, PlantConstants.Feedwater
//   - State owned: FeedwaterState struct (owned by engine, passed by ref)
//   - Pattern: Static module with Initialize() / Update()
//
// CS REFERENCE: CS-0115
// IP REFERENCE: IP-0046, Stage G
// VERSION: 1.0.0
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;

namespace Critical.Physics
{
    // ========================================================================
    // AFW PUMP TYPE ENUM
    // ========================================================================

    /// <summary>
    /// Auxiliary feedwater pump types.
    /// Two motor-driven (serve 2 SGs each) and one turbine-driven (all 4 SGs).
    /// </summary>
    public enum AFWPumpType
    {
        /// <summary>Motor-driven AFW pump 1 (serves SG-1 and SG-4)</summary>
        Motor1,

        /// <summary>Motor-driven AFW pump 2 (serves SG-2 and SG-3)</summary>
        Motor2,

        /// <summary>Turbine-driven AFW pump (serves all 4 SGs, steam from SG-2/SG-3)</summary>
        TurbineDriven
    }

    // ========================================================================
    // STATE STRUCT
    // ========================================================================

    /// <summary>
    /// Feedwater system state for persistence between timesteps.
    /// Owned by HeatupSimEngine, passed by ref to FeedwaterSystem.Update().
    /// </summary>
    public struct FeedwaterState
    {
        // --- Hotwell ---

        /// <summary>Current hotwell water inventory in lb</summary>
        public float HotwellMass_lb;

        /// <summary>Hotwell level indication in inches (0-40)</summary>
        public float HotwellLevel_in;

        /// <summary>Hotwell water temperature in °F (from condenser backpressure)</summary>
        public float HotwellTemp_F;

        // --- CST ---

        /// <summary>Current CST volume in gallons</summary>
        public float CST_Volume_gal;

        /// <summary>True when CST volume is below Tech Spec minimum (239,000 gal)</summary>
        public bool CST_BelowTechSpec;

        // --- Condensate Pumps ---

        /// <summary>Number of condensate pumps running (0-2)</summary>
        public int CondensatePumpsRunning;

        /// <summary>Total condensate flow out of hotwell in gpm</summary>
        public float CondensateFlow_gpm;

        /// <summary>Total condensate mass flow in lb/hr</summary>
        public float CondensateFlow_lbhr;

        // --- Main Feedwater Pumps ---

        /// <summary>Number of main feedwater pumps running (0-2)</summary>
        public int MFP_Running;

        /// <summary>Total feedwater flow to SGs in gpm</summary>
        public float FeedwaterFlow_gpm;

        /// <summary>Total feedwater mass flow to SGs in lb/hr</summary>
        public float FeedwaterFlow_lbhr;

        /// <summary>Feedwater temperature at SG inlet in °F</summary>
        public float FeedwaterTemp_F;

        // --- Auxiliary Feedwater ---

        /// <summary>Number of motor-driven AFW pumps running (0-2)</summary>
        public int AFW_MotorPumpsRunning;

        /// <summary>True if turbine-driven AFW pump is running</summary>
        public bool AFW_TurbinePumpRunning;

        /// <summary>Total AFW flow to SGs in gpm</summary>
        public float AFW_Flow_gpm;

        /// <summary>Total AFW mass flow to SGs in lb/hr</summary>
        public float AFW_Flow_lbhr;

        // --- Derived ---

        /// <summary>Total mass flow returning to SGs (FW + AFW) in lb/hr</summary>
        public float TotalReturnFlow_lbhr;

        /// <summary>Steam mass flow lost to atmosphere (not condensed) in lb/hr</summary>
        public float NetSteamLoss_lbhr;

        // --- Status ---

        /// <summary>True if at least one feedwater return path exists to SGs</summary>
        public bool FeedwaterAvailable;

        /// <summary>Status message for display and logging</summary>
        public string StatusMessage;
    }

    // ========================================================================
    // PHYSICS MODULE
    // ========================================================================

    /// <summary>
    /// Feedwater System Physics Module — hotwell inventory, pump models, CST tracking.
    ///
    /// Tracks the closed-loop secondary mass path:
    ///   SG steam → condenser → hotwell → condensate/FW pumps → SG
    ///   SG steam → atmosphere → LOST (CST depletion)
    ///
    /// During startup (Mode 5→3), AFW is the primary return path to SGs.
    /// Condensate pumps and MFPs start later during power ascension.
    /// </summary>
    public static class FeedwaterSystem
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        /// <summary>
        /// Conversion factor: gpm to lb/hr (at ~62 lb/ft³ water density).
        /// 1 gpm × 8.34 lb/gal × 60 min/hr = 500.4 lb/hr
        /// </summary>
        private const float GPM_TO_LBHR = 500.4f;

        /// <summary>
        /// Hotwell level control: makeup/reject valve gain in lb/hr per inch of error.
        /// Sized to maintain level within control band during steam dump transients.
        /// </summary>
        private const float HOTWELL_LEVEL_CONTROL_GAIN = 5000f;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        /// <summary>
        /// Initialize feedwater system for cold startup.
        /// CST at full capacity, no pumps running, hotwell at normal level.
        /// </summary>
        /// <param name="cstVolume_gal">Initial CST volume in gallons (default: full capacity)</param>
        /// <returns>Initialized feedwater state</returns>
        public static FeedwaterState Initialize(float cstVolume_gal = 0f)
        {
            if (cstVolume_gal <= 0f)
                cstVolume_gal = PlantConstants.Feedwater.CST_TOTAL_CAPACITY_GAL;

            float normalMass = PlantConstants.Condenser.HOTWELL_DESIGN_MASS_LB
                             * (PlantConstants.Condenser.HOTWELL_NORMAL_LEVEL_IN
                              / PlantConstants.Condenser.HOTWELL_LEVEL_SPAN_IN);

            return new FeedwaterState
            {
                HotwellMass_lb = normalMass,
                HotwellLevel_in = PlantConstants.Condenser.HOTWELL_NORMAL_LEVEL_IN,
                HotwellTemp_F = PlantConstants.Feedwater.FW_TEMP_CST_F,

                CST_Volume_gal = cstVolume_gal,
                CST_BelowTechSpec = false,

                CondensatePumpsRunning = 0,
                CondensateFlow_gpm = 0f,
                CondensateFlow_lbhr = 0f,

                MFP_Running = 0,
                FeedwaterFlow_gpm = 0f,
                FeedwaterFlow_lbhr = 0f,
                FeedwaterTemp_F = PlantConstants.Feedwater.FW_TEMP_CST_F,

                AFW_MotorPumpsRunning = 0,
                AFW_TurbinePumpRunning = false,
                AFW_Flow_gpm = 0f,
                AFW_Flow_lbhr = 0f,

                TotalReturnFlow_lbhr = 0f,
                NetSteamLoss_lbhr = 0f,

                FeedwaterAvailable = false,
                StatusMessage = "Feedwater idle — no pumps running"
            };
        }

        // ====================================================================
        // MAIN UPDATE
        // ====================================================================

        /// <summary>
        /// Update feedwater system for one timestep.
        ///
        /// 1. Calculates pump flow rates based on running equipment
        /// 2. Updates hotwell mass balance (condensed steam in, pump flow out)
        /// 3. Applies hotwell level control (CST makeup/reject)
        /// 4. Updates CST inventory
        /// 5. Determines feedwater availability and temperature
        /// </summary>
        /// <param name="state">Feedwater state (modified in place)</param>
        /// <param name="steamToCondenser_lbhr">Steam mass flow condensing in hotwell in lb/hr</param>
        /// <param name="steamToAtmosphere_lbhr">Steam mass flow vented to atmosphere in lb/hr (lost)</param>
        /// <param name="hotwellTemp_F">Hotwell water temperature from condenser backpressure in °F</param>
        /// <param name="dt_hr">Timestep in hours</param>
        public static void Update(
            ref FeedwaterState state,
            float steamToCondenser_lbhr,
            float steamToAtmosphere_lbhr,
            float hotwellTemp_F,
            float dt_hr)
        {
            state.HotwellTemp_F = hotwellTemp_F;
            state.NetSteamLoss_lbhr = steamToAtmosphere_lbhr;

            // ================================================================
            // 1. PUMP FLOW RATES
            // ================================================================
            CalculatePumpFlows(ref state);

            // ================================================================
            // 2. HOTWELL MASS BALANCE
            // ================================================================
            // dM/dt = steam_condensed - condensate_pumps_out
            // (Makeup/reject handled separately via level control)
            float hotwellNetFlow_lbhr = steamToCondenser_lbhr - state.CondensateFlow_lbhr;

            // AFW draws from CST, not from hotwell, so it does not appear here
            state.HotwellMass_lb += hotwellNetFlow_lbhr * dt_hr;

            // Prevent negative mass
            state.HotwellMass_lb = Mathf.Max(state.HotwellMass_lb, 0f);

            // ================================================================
            // 3. HOTWELL LEVEL
            // ================================================================
            UpdateHotwellLevel(ref state);

            // ================================================================
            // 4. HOTWELL LEVEL CONTROL (CST makeup/reject)
            // ================================================================
            float makeupReject_lbhr = CalculateHotwellLevelControl(state.HotwellLevel_in);
            state.HotwellMass_lb += makeupReject_lbhr * dt_hr;
            state.HotwellMass_lb = Mathf.Max(state.HotwellMass_lb, 0f);

            // Update CST for makeup/reject and AFW draw
            float cstDelta_gal = 0f;

            // Makeup flow: CST → hotwell (positive makeupReject means adding to hotwell)
            if (makeupReject_lbhr > 0f)
            {
                cstDelta_gal -= (makeupReject_lbhr / PlantConstants.Feedwater.CST_WATER_DENSITY_LB_GAL)
                              * dt_hr / 60f; // Convert lb/hr to gal over timestep
            }
            // Reject flow: hotwell → CST (negative makeupReject means removing from hotwell)
            else if (makeupReject_lbhr < 0f)
            {
                cstDelta_gal += (-makeupReject_lbhr / PlantConstants.Feedwater.CST_WATER_DENSITY_LB_GAL)
                              * dt_hr / 60f;
            }

            // AFW draws from CST directly
            float afwDraw_gal = state.AFW_Flow_gpm * dt_hr * 60f; // gpm × hr × 60 min/hr
            cstDelta_gal -= afwDraw_gal;

            // Atmospheric steam loss → CST doesn't recover this mass
            // (It was secondary inventory that left the system)

            state.CST_Volume_gal += cstDelta_gal;
            state.CST_Volume_gal = Mathf.Clamp(state.CST_Volume_gal, 0f,
                PlantConstants.Feedwater.CST_TOTAL_CAPACITY_GAL);

            // ================================================================
            // 5. CST TECH SPEC CHECK
            // ================================================================
            state.CST_BelowTechSpec = state.CST_Volume_gal < PlantConstants.Feedwater.CST_TECH_SPEC_MIN_GAL;

            // ================================================================
            // 6. TOTAL RETURN FLOW AND AVAILABILITY
            // ================================================================
            // During startup: AFW is the primary return path
            // Condensate/MFP contribute when running (post-power ascension)
            state.TotalReturnFlow_lbhr = state.FeedwaterFlow_lbhr + state.AFW_Flow_lbhr;

            state.FeedwaterAvailable = state.TotalReturnFlow_lbhr > 0f
                                    || state.CondensatePumpsRunning > 0
                                    || state.AFW_MotorPumpsRunning > 0
                                    || state.AFW_TurbinePumpRunning;

            // ================================================================
            // 7. FEEDWATER TEMPERATURE
            // ================================================================
            // During startup: AFW delivers CST water (~100°F)
            // Condensate from hotwell: ~120°F at design vacuum
            // Full heater train: 440°F (not modeled until power ascension)
            if (state.AFW_Flow_lbhr > 0f && state.FeedwaterFlow_lbhr <= 0f)
            {
                // AFW only — CST temperature
                state.FeedwaterTemp_F = PlantConstants.Feedwater.FW_TEMP_CST_F;
            }
            else if (state.CondensatePumpsRunning > 0)
            {
                // Condensate from hotwell
                state.FeedwaterTemp_F = state.HotwellTemp_F;
            }
            else
            {
                state.FeedwaterTemp_F = PlantConstants.Feedwater.FW_TEMP_CST_F;
            }

            // ================================================================
            // 8. RECALCULATE LEVEL AFTER CONTROLS
            // ================================================================
            UpdateHotwellLevel(ref state);

            // ================================================================
            // 9. STATUS MESSAGE
            // ================================================================
            UpdateStatusMessage(ref state);
        }

        // ====================================================================
        // PUMP FLOW CALCULATIONS
        // ====================================================================

        /// <summary>
        /// Calculate flow rates for all running pumps.
        /// Pumps are modeled as on/off with rated capacity (simplified for startup).
        /// </summary>
        private static void CalculatePumpFlows(ref FeedwaterState state)
        {
            // Condensate pumps
            state.CondensateFlow_gpm = state.CondensatePumpsRunning
                * PlantConstants.Feedwater.CONDENSATE_PUMP_FLOW_GPM;
            state.CondensateFlow_lbhr = state.CondensateFlow_gpm * GPM_TO_LBHR;

            // MFPs (downstream of condensate pumps in feedwater train)
            // MFP flow is limited by condensate pump input when both running
            float mfpCapacity_gpm = state.MFP_Running * PlantConstants.Feedwater.MFP_FLOW_GPM;
            state.FeedwaterFlow_gpm = Mathf.Min(mfpCapacity_gpm, state.CondensateFlow_gpm);
            state.FeedwaterFlow_lbhr = state.FeedwaterFlow_gpm * GPM_TO_LBHR;

            // AFW pumps (suction from CST, discharge to SGs)
            float afwMotor_gpm = state.AFW_MotorPumpsRunning
                * PlantConstants.Feedwater.AFW_MOTOR_FLOW_GPM;
            float afwTurbine_gpm = state.AFW_TurbinePumpRunning
                ? PlantConstants.Feedwater.AFW_TURBINE_FLOW_GPM : 0f;

            state.AFW_Flow_gpm = afwMotor_gpm + afwTurbine_gpm;
            state.AFW_Flow_lbhr = state.AFW_Flow_gpm * GPM_TO_LBHR;
        }

        // ====================================================================
        // HOTWELL LEVEL
        // ====================================================================

        /// <summary>
        /// Update hotwell level from current mass.
        /// Level is linearly proportional to mass within the measurement span.
        /// </summary>
        private static void UpdateHotwellLevel(ref FeedwaterState state)
        {
            float designMass = PlantConstants.Condenser.HOTWELL_DESIGN_MASS_LB;
            float span = PlantConstants.Condenser.HOTWELL_LEVEL_SPAN_IN;

            if (designMass > 0f)
            {
                // Mass at full span (40 in.) = HOTWELL_DESIGN_MASS_LB
                // Level = (mass / designMass) × span
                state.HotwellLevel_in = (state.HotwellMass_lb / designMass) * span;
            }
            else
            {
                state.HotwellLevel_in = 0f;
            }

            state.HotwellLevel_in = Mathf.Clamp(state.HotwellLevel_in, 0f, span);
        }

        /// <summary>
        /// Calculate hotwell level control action (makeup or reject flow).
        /// Implements documented level control setpoints:
        ///   - Level > 28 in.: reject valve opens (returns to CST)
        ///   - Level < 21 in.: makeup valve opens (draws from CST)
        ///   - Between 21-28 in.: no action (deadband)
        ///
        /// Returns positive value for makeup (CST → hotwell),
        /// negative for reject (hotwell → CST).
        /// </summary>
        /// <param name="level_in">Current hotwell level in inches</param>
        /// <returns>Net flow into hotwell in lb/hr (positive = makeup, negative = reject)</returns>
        private static float CalculateHotwellLevelControl(float level_in)
        {
            float rejectOpen = PlantConstants.Condenser.HOTWELL_REJECT_OPEN_IN;
            float rejectFull = PlantConstants.Condenser.HOTWELL_REJECT_FULL_IN;
            float makeupOpen = PlantConstants.Condenser.HOTWELL_MAKEUP_OPEN_IN;
            float makeupFull = PlantConstants.Condenser.HOTWELL_MAKEUP_FULL_IN;

            if (level_in > rejectOpen)
            {
                // Reject: remove water from hotwell back to CST
                float fraction = Mathf.Clamp01((level_in - rejectOpen) / (rejectFull - rejectOpen));
                return -fraction * HOTWELL_LEVEL_CONTROL_GAIN;
            }

            if (level_in < makeupOpen)
            {
                // Makeup: add water from CST to hotwell
                float fraction = Mathf.Clamp01((makeupOpen - level_in) / (makeupOpen - makeupFull));
                return fraction * HOTWELL_LEVEL_CONTROL_GAIN;
            }

            // Deadband: no control action
            return 0f;
        }

        // ====================================================================
        // PUMP START/STOP
        // ====================================================================

        /// <summary>Start a condensate pump (0→1 or 1→2).</summary>
        public static void StartCondensatePump(ref FeedwaterState state)
        {
            if (state.CondensatePumpsRunning < PlantConstants.Feedwater.CONDENSATE_PUMP_COUNT)
            {
                state.CondensatePumpsRunning++;
                Debug.Log($"[Feedwater] Condensate pump started. Running: {state.CondensatePumpsRunning}/2");
            }
        }

        /// <summary>Stop a condensate pump.</summary>
        public static void StopCondensatePump(ref FeedwaterState state)
        {
            if (state.CondensatePumpsRunning > 0)
            {
                state.CondensatePumpsRunning--;
                Debug.Log($"[Feedwater] Condensate pump stopped. Running: {state.CondensatePumpsRunning}/2");
            }
        }

        /// <summary>Start a main feedwater pump.</summary>
        public static void StartMFP(ref FeedwaterState state)
        {
            if (state.MFP_Running < PlantConstants.Feedwater.MFP_COUNT)
            {
                state.MFP_Running++;
                Debug.Log($"[Feedwater] MFP started. Running: {state.MFP_Running}/2");
            }
        }

        /// <summary>Stop a main feedwater pump.</summary>
        public static void StopMFP(ref FeedwaterState state)
        {
            if (state.MFP_Running > 0)
            {
                state.MFP_Running--;
                Debug.Log($"[Feedwater] MFP stopped. Running: {state.MFP_Running}/2");
            }
        }

        /// <summary>
        /// Start an AFW pump by type.
        /// Motor-driven pumps serve 2 SGs each; turbine-driven serves all 4.
        /// </summary>
        /// <param name="state">Feedwater state to modify</param>
        /// <param name="pumpType">Type of AFW pump to start</param>
        public static void StartAFWPump(ref FeedwaterState state, AFWPumpType pumpType)
        {
            switch (pumpType)
            {
                case AFWPumpType.Motor1:
                case AFWPumpType.Motor2:
                    if (state.AFW_MotorPumpsRunning < PlantConstants.Feedwater.AFW_MOTOR_PUMP_COUNT)
                    {
                        state.AFW_MotorPumpsRunning++;
                        Debug.Log($"[Feedwater] AFW motor pump started ({pumpType}). " +
                                  $"Motor pumps running: {state.AFW_MotorPumpsRunning}/2");
                    }
                    break;

                case AFWPumpType.TurbineDriven:
                    if (!state.AFW_TurbinePumpRunning)
                    {
                        state.AFW_TurbinePumpRunning = true;
                        Debug.Log("[Feedwater] AFW turbine-driven pump started.");
                    }
                    break;
            }
        }

        /// <summary>Stop an AFW pump by type.</summary>
        public static void StopAFWPump(ref FeedwaterState state, AFWPumpType pumpType)
        {
            switch (pumpType)
            {
                case AFWPumpType.Motor1:
                case AFWPumpType.Motor2:
                    if (state.AFW_MotorPumpsRunning > 0)
                    {
                        state.AFW_MotorPumpsRunning--;
                        Debug.Log($"[Feedwater] AFW motor pump stopped ({pumpType}). " +
                                  $"Motor pumps running: {state.AFW_MotorPumpsRunning}/2");
                    }
                    break;

                case AFWPumpType.TurbineDriven:
                    if (state.AFW_TurbinePumpRunning)
                    {
                        state.AFW_TurbinePumpRunning = false;
                        Debug.Log("[Feedwater] AFW turbine-driven pump stopped.");
                    }
                    break;
            }
        }

        // ====================================================================
        // QUERY METHODS
        // ====================================================================

        /// <summary>
        /// Get total available return flow capacity to SGs in lb/hr.
        /// Includes both FW train and AFW system.
        /// </summary>
        public static float GetAvailableReturnFlow_lbhr(in FeedwaterState state)
        {
            return state.TotalReturnFlow_lbhr;
        }

        /// <summary>
        /// Get CST remaining volume as a percentage of total capacity.
        /// </summary>
        public static float GetCSTLevelPercent(in FeedwaterState state)
        {
            return (state.CST_Volume_gal / PlantConstants.Feedwater.CST_TOTAL_CAPACITY_GAL) * 100f;
        }

        // ====================================================================
        // STATUS AND DIAGNOSTICS
        // ====================================================================

        /// <summary>Update status message based on current state.</summary>
        private static void UpdateStatusMessage(ref FeedwaterState state)
        {
            if (state.AFW_Flow_gpm > 0f && state.FeedwaterFlow_gpm <= 0f)
            {
                state.StatusMessage = $"AFW active — {state.AFW_Flow_gpm:F0} gpm to SGs";
            }
            else if (state.FeedwaterFlow_gpm > 0f)
            {
                state.StatusMessage = $"MFW active — {state.FeedwaterFlow_gpm:F0} gpm to SGs";
            }
            else
            {
                state.StatusMessage = "Feedwater idle — no pumps running";
            }

            if (state.CST_BelowTechSpec)
            {
                state.StatusMessage += " | CST BELOW TECH SPEC";
            }
        }

        /// <summary>
        /// Get a diagnostic string for logging and telemetry.
        /// </summary>
        public static string GetDiagnosticString(in FeedwaterState state)
        {
            return $"--- FEEDWATER ---\n" +
                   $"  Hotwell Level:    {state.HotwellLevel_in:F1} in. ({state.HotwellMass_lb:F0} lb)\n" +
                   $"  Hotwell Temp:     {state.HotwellTemp_F:F1} °F\n" +
                   $"  CST Volume:       {state.CST_Volume_gal:F0} gal ({GetCSTLevelPercent(state):F1}%)\n" +
                   $"  CST Below Spec:   {state.CST_BelowTechSpec}\n" +
                   $"  Condensate Pumps: {state.CondensatePumpsRunning}/2 ({state.CondensateFlow_gpm:F0} gpm)\n" +
                   $"  MFP:              {state.MFP_Running}/2 ({state.FeedwaterFlow_gpm:F0} gpm)\n" +
                   $"  AFW Motor:        {state.AFW_MotorPumpsRunning}/2\n" +
                   $"  AFW Turbine:      {(state.AFW_TurbinePumpRunning ? "Running" : "Off")}\n" +
                   $"  AFW Total:        {state.AFW_Flow_gpm:F0} gpm ({state.AFW_Flow_lbhr:F0} lb/hr)\n" +
                   $"  Total Return:     {state.TotalReturnFlow_lbhr:F0} lb/hr\n" +
                   $"  Atm Loss:         {state.NetSteamLoss_lbhr:F0} lb/hr\n" +
                   $"  FW Temp:          {state.FeedwaterTemp_F:F1} °F\n" +
                   $"  FW Available:     {state.FeedwaterAvailable}\n" +
                   $"  Status:           {state.StatusMessage}";
        }
    }
}
