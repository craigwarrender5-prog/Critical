// ============================================================================
// CRITICAL: Master the Atom — Condenser Physics Module
// CondenserPhysics.cs — Condenser Vacuum Dynamics, Backpressure, and C-9 Interlock
// ============================================================================
//
// PURPOSE:
//   Models the main condenser as a first-order dynamic system for startup and
//   HZP operations. The condenser is the primary heat sink for steam dump
//   operations; this module provides vacuum dynamics, backpressure response
//   to steam load, and the C-9 "Condenser Available" interlock output.
//
// PHYSICS:
//   The condenser operates as a three-shell multipressure surface condenser.
//   For the startup simulation scope, the three shells are lumped into a
//   single equivalent backpressure that responds to the balance between
//   steam condensing load (input) and CW heat rejection (output).
//
//   Vacuum Dynamics:
//     dP/dt = (P_equilibrium - P_current) / τ
//     τ = 30 seconds (first-order time constant)
//
//   Equilibrium Backpressure:
//     loadFraction = Q_steam / Q_cw_total
//     P_eq = P_design + (P_max - P_design) × loadFraction²
//
//   Vacuum Pulldown Sequence:
//     1. CW pumps start → tube-side flow established
//     2. Hogging ejectors → atmospheric to ~18 in. Hg (~8 min)
//     3. Transfer to main ejectors → 18 to design vacuum (~8 min)
//     4. C-9 satisfied when vacuum > 22 in. Hg with CW ≥ 1
//
//   C-9 Interlock:
//     C9 = (Vacuum ≥ 22 in. Hg) AND (CW_pumps ≥ 1)
//
// SOURCES:
//   - NRC HRTD Condenser System Reference (Technical_Documentation/)
//   - NRC HRTD Section 11.2 — Steam Dump Control System
//   - Technical_Documentation/Condenser_Feedwater_Architecture_Specification.md
//
// UNITS:
//   Pressure: psia (backpressure), in. Hg (vacuum) | Temperature: °F
//   Heat Rate: BTU/hr | Flow: gpm | Time: hours
//
// ARCHITECTURE:
//   - Called by: HeatupSimEngine.StepSimulation() (Stage I integration)
//   - Uses constants from: PlantConstants.Condenser
//   - State owned: CondenserState struct (owned by engine, passed by ref)
//   - Pattern: Static module with Initialize() / Update()
//     (matches RHRSystem.cs, SGMultiNodeThermal.cs pattern)
//
// CS REFERENCE: CS-0115
// IP REFERENCE: IP-0046, Stage F
// VERSION: 1.0.0
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;

namespace Critical.Physics
{
    // ========================================================================
    // VACUUM PULLDOWN PHASE ENUM
    // ========================================================================

    /// <summary>
    /// Condenser vacuum pulldown phases during startup.
    /// Tracks progress from atmospheric to design vacuum.
    /// </summary>
    public enum VacuumPulldownPhase
    {
        /// <summary>Condenser at atmospheric pressure, no vacuum established</summary>
        Atmospheric,

        /// <summary>CW pumps running, hogging ejectors pulling initial vacuum</summary>
        Hogging,

        /// <summary>Transferred to main ejectors, pulling to design vacuum</summary>
        MainEjectors,

        /// <summary>Design vacuum established and maintained</summary>
        Established
    }

    // ========================================================================
    // STATE STRUCT
    // ========================================================================

    /// <summary>
    /// Condenser state for persistence between timesteps.
    /// Owned by HeatupSimEngine, passed by ref to CondenserPhysics.Update().
    /// </summary>
    public struct CondenserState
    {
        // --- Vacuum State ---

        /// <summary>Current condenser vacuum in inches Hg (0 = atmospheric, 29.92 = perfect)</summary>
        public float Vacuum_inHg;

        /// <summary>Current condenser absolute backpressure in psia</summary>
        public float Backpressure_psia;

        /// <summary>Hotwell saturation temperature at current backpressure in °F</summary>
        public float HotwellTemp_F;

        // --- Equipment State ---

        /// <summary>Number of CW pumps currently running (0-4)</summary>
        public int CW_PumpsRunning;

        /// <summary>True when air ejectors (hogging or main) are in service</summary>
        public bool AirEjectorsRunning;

        /// <summary>True when vacuum has been established above C-9 threshold</summary>
        public bool VacuumEstablished;

        /// <summary>Current vacuum pulldown phase</summary>
        public VacuumPulldownPhase PulldownPhase;

        // --- C-9 Interlock Output ---

        /// <summary>
        /// C-9 "Condenser Available" interlock status.
        /// True when vacuum ≥ 22 in. Hg AND CW pumps ≥ 1.
        /// Steam dump valves are blocked from opening when C-9 is false.
        /// </summary>
        public bool C9_CondenserAvailable;

        // --- Heat Balance ---

        /// <summary>Current steam condensing heat load on condenser in BTU/hr</summary>
        public float SteamLoad_BTUhr;

        /// <summary>CW heat rejection capacity based on running pumps in BTU/hr</summary>
        public float CW_HeatRejection_BTUhr;

        /// <summary>Steady-state equilibrium backpressure target in psia</summary>
        public float EquilibriumBackpressure_psia;

        // --- Startup Tracking ---

        /// <summary>Simulation time when vacuum pulldown was initiated in hours</summary>
        public float VacuumPulldownStartTime_hr;

        /// <summary>True when hogging ejectors have completed and transferred to main</summary>
        public bool HoggingComplete;

        // --- Telemetry ---

        /// <summary>Status message for display and logging</summary>
        public string StatusMessage;
    }

    // ========================================================================
    // PHYSICS MODULE
    // ========================================================================

    /// <summary>
    /// Condenser Physics Module — vacuum dynamics, backpressure, and C-9 interlock.
    ///
    /// Models the main condenser as a first-order dynamic system responding to
    /// steam heat load versus CW rejection capacity. Provides the C-9
    /// "Condenser Available" interlock consumed by StartupPermissives and
    /// SteamDumpController.
    ///
    /// Per NRC HRTD: C-9 requires condenser vacuum > 22 in. Hg AND at least
    /// 1 CW pump running. Without C-9, steam dump valves are blocked.
    /// </summary>
    public static class CondenserPhysics
    {
        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        /// <summary>
        /// Initialize condenser state to atmospheric (pre-startup) conditions.
        /// No vacuum, no CW pumps, C-9 false.
        /// Called at simulation startup from HeatupSimEngine.InitializeColdShutdown().
        /// </summary>
        /// <returns>Initialized condenser state at atmospheric conditions</returns>
        public static CondenserState Initialize()
        {
            return new CondenserState
            {
                Vacuum_inHg = 0f,
                Backpressure_psia = PlantConstants.P_ATM, // 14.7 psia (atmospheric)
                HotwellTemp_F = PlantConstants.AMBIENT_TEMP_F,

                CW_PumpsRunning = 0,
                AirEjectorsRunning = false,
                VacuumEstablished = false,
                PulldownPhase = VacuumPulldownPhase.Atmospheric,

                C9_CondenserAvailable = false,

                SteamLoad_BTUhr = 0f,
                CW_HeatRejection_BTUhr = 0f,
                EquilibriumBackpressure_psia = PlantConstants.P_ATM,

                VacuumPulldownStartTime_hr = 0f,
                HoggingComplete = false,

                StatusMessage = "Condenser at atmospheric — no vacuum"
            };
        }

        // ====================================================================
        // VACUUM PULLDOWN
        // ====================================================================

        /// <summary>
        /// Initiate condenser vacuum pulldown sequence.
        /// Starts CW pumps and hogging air ejectors to begin establishing
        /// condenser vacuum. Called by engine when T_avg approaches Mode 3
        /// (typically ~325°F).
        ///
        /// Sequence:
        ///   1. Start 2 CW pumps (adequate for startup)
        ///   2. Start hogging ejectors (3 units, pull to ~18 in. Hg in ~8 min)
        ///   3. Transfer to main ejectors (2 units, pull to design vacuum)
        /// </summary>
        /// <param name="state">Condenser state to modify</param>
        /// <param name="simTime_hr">Current simulation time in hours</param>
        public static void StartVacuumPulldown(ref CondenserState state, float simTime_hr)
        {
            if (state.PulldownPhase != VacuumPulldownPhase.Atmospheric)
                return; // Already initiated

            state.CW_PumpsRunning = 2; // Start 2 of 4 CW pumps for startup
            state.AirEjectorsRunning = true;
            state.PulldownPhase = VacuumPulldownPhase.Hogging;
            state.VacuumPulldownStartTime_hr = simTime_hr;
            state.HoggingComplete = false;

            // CW provides heat rejection capacity immediately
            state.CW_HeatRejection_BTUhr = state.CW_PumpsRunning
                * PlantConstants.Condenser.CW_PUMP_MAX_REJECTION_BTUHR;

            state.StatusMessage = "Vacuum pulldown — hogging ejectors";

            Debug.Log($"[Condenser] Vacuum pulldown initiated at T+{simTime_hr:F2}hr. " +
                      $"CW pumps: {state.CW_PumpsRunning}, hogging ejectors started.");
        }

        // ====================================================================
        // MAIN UPDATE
        // ====================================================================

        /// <summary>
        /// Update condenser state for one timestep.
        ///
        /// 1. Advances vacuum pulldown phase based on elapsed time
        /// 2. Calculates equilibrium backpressure from steam load vs. CW capacity
        /// 3. Applies first-order lag vacuum dynamics
        /// 4. Evaluates C-9 interlock
        /// 5. Updates hotwell temperature from backpressure
        /// </summary>
        /// <param name="state">Condenser state (modified in place)</param>
        /// <param name="steamLoad_BTUhr">Current steam heat load on condenser in BTU/hr</param>
        /// <param name="simTime_hr">Current simulation time in hours</param>
        /// <param name="dt_hr">Timestep in hours</param>
        public static void Update(
            ref CondenserState state,
            float steamLoad_BTUhr,
            float simTime_hr,
            float dt_hr)
        {
            state.SteamLoad_BTUhr = steamLoad_BTUhr;

            // ================================================================
            // 1. ADVANCE VACUUM PULLDOWN PHASE
            // ================================================================
            AdvancePulldownPhase(ref state, simTime_hr);

            // ================================================================
            // 2. CW HEAT REJECTION CAPACITY
            // ================================================================
            state.CW_HeatRejection_BTUhr = state.CW_PumpsRunning
                * PlantConstants.Condenser.CW_PUMP_MAX_REJECTION_BTUHR;

            // ================================================================
            // 3. EQUILIBRIUM BACKPRESSURE
            // ================================================================
            state.EquilibriumBackpressure_psia = CalculateEquilibriumBackpressure(
                steamLoad_BTUhr, state.CW_HeatRejection_BTUhr, state.PulldownPhase);

            // ================================================================
            // 4. VACUUM DYNAMICS (first-order lag)
            // ================================================================
            ApplyVacuumDynamics(ref state, dt_hr);

            // ================================================================
            // 5. C-9 INTERLOCK EVALUATION
            // ================================================================
            EvaluateC9(ref state);

            // ================================================================
            // 6. HOTWELL TEMPERATURE
            // ================================================================
            state.HotwellTemp_F = GetHotwellSatTemp(state.Backpressure_psia);

            // ================================================================
            // 7. STATUS MESSAGE
            // ================================================================
            UpdateStatusMessage(ref state);
        }

        // ====================================================================
        // VACUUM PULLDOWN PHASE MANAGEMENT
        // ====================================================================

        /// <summary>
        /// Advance vacuum pulldown phase based on elapsed time since initiation.
        /// Hogging ejectors run for ~8 minutes to reach ~18 in. Hg, then transfer
        /// to main ejectors which bring vacuum to design in another ~8 minutes.
        /// </summary>
        private static void AdvancePulldownPhase(ref CondenserState state, float simTime_hr)
        {
            if (state.PulldownPhase == VacuumPulldownPhase.Atmospheric ||
                state.PulldownPhase == VacuumPulldownPhase.Established)
                return;

            float elapsed_min = (simTime_hr - state.VacuumPulldownStartTime_hr) * 60f;

            if (state.PulldownPhase == VacuumPulldownPhase.Hogging)
            {
                if (elapsed_min >= PlantConstants.Condenser.HOGGING_DURATION_MIN)
                {
                    state.PulldownPhase = VacuumPulldownPhase.MainEjectors;
                    state.HoggingComplete = true;
                    Debug.Log($"[Condenser] Hogging complete at T+{simTime_hr:F2}hr. " +
                              $"Transferring to main ejectors. Vacuum: {state.Vacuum_inHg:F1} in. Hg");
                }
            }

            if (state.PulldownPhase == VacuumPulldownPhase.MainEjectors)
            {
                float totalPulldownTime = PlantConstants.Condenser.HOGGING_DURATION_MIN
                                        + PlantConstants.Condenser.MAIN_EJECTOR_RAMPUP_MIN;

                if (elapsed_min >= totalPulldownTime &&
                    state.Vacuum_inHg >= PlantConstants.Condenser.C9_VACUUM_THRESHOLD_INHG)
                {
                    state.PulldownPhase = VacuumPulldownPhase.Established;
                    state.VacuumEstablished = true;
                    Debug.Log($"[Condenser] Design vacuum established at T+{simTime_hr:F2}hr. " +
                              $"Vacuum: {state.Vacuum_inHg:F1} in. Hg");
                }
            }
        }

        // ====================================================================
        // EQUILIBRIUM BACKPRESSURE
        // ====================================================================

        /// <summary>
        /// Calculate the steady-state equilibrium backpressure from the heat
        /// balance between steam condensing load and CW rejection capacity.
        ///
        /// At zero steam load with CW running, backpressure approaches design
        /// (~1.5 psia). As steam load increases toward full dump capacity,
        /// backpressure rises toward max operating (~3.3 psia).
        ///
        /// Quadratic model: higher loads cause disproportionate backpressure rise
        /// because the CW delta-T increases, reducing the driving force.
        /// </summary>
        /// <param name="steamLoad_BTUhr">Steam heat load in BTU/hr</param>
        /// <param name="cwCapacity_BTUhr">CW heat rejection capacity in BTU/hr</param>
        /// <param name="phase">Current pulldown phase</param>
        /// <returns>Equilibrium backpressure in psia</returns>
        private static float CalculateEquilibriumBackpressure(
            float steamLoad_BTUhr,
            float cwCapacity_BTUhr,
            VacuumPulldownPhase phase)
        {
            // Before vacuum establishment, target is phase-dependent
            if (phase == VacuumPulldownPhase.Atmospheric)
                return PlantConstants.P_ATM; // 14.7 psia

            if (phase == VacuumPulldownPhase.Hogging)
            {
                // Hogging ejectors target ~18 in. Hg
                // = 29.92 - 18.0 = 11.92 in. Hg absolute
                // = 11.92 / 2.036 ≈ 5.85 psia
                return AbsoluteFromVacuum(PlantConstants.Condenser.HOGGING_TARGET_INHG);
            }

            // Main ejectors or established: equilibrium from heat balance
            if (cwCapacity_BTUhr <= 0f)
                return PlantConstants.Condenser.MAX_OPERATING_BACKPRESSURE_PSIA;

            float loadFraction = Mathf.Clamp01(steamLoad_BTUhr / cwCapacity_BTUhr);

            // Quadratic interpolation between design and max backpressure
            float pDesign = PlantConstants.Condenser.DESIGN_BACKPRESSURE_PSIA;
            float pMax = PlantConstants.Condenser.MAX_OPERATING_BACKPRESSURE_PSIA;

            return pDesign + (pMax - pDesign) * loadFraction * loadFraction;
        }

        // ====================================================================
        // VACUUM DYNAMICS
        // ====================================================================

        /// <summary>
        /// Apply first-order lag dynamics to condenser backpressure.
        /// Backpressure tracks toward equilibrium with time constant τ=30s.
        ///
        /// dP/dt = (P_eq - P_current) / τ
        ///
        /// Vacuum is derived from backpressure:
        /// Vacuum_inHg = 29.92 - (Backpressure_psia × 2.036)
        /// </summary>
        private static void ApplyVacuumDynamics(ref CondenserState state, float dt_hr)
        {
            if (state.PulldownPhase == VacuumPulldownPhase.Atmospheric)
            {
                // No dynamics at atmospheric — just hold at ambient
                state.Backpressure_psia = PlantConstants.P_ATM;
                state.Vacuum_inHg = 0f;
                return;
            }

            float tau_hr = PlantConstants.Condenser.CONDENSER_TAU_HR;

            // Prevent division by zero and ensure numerical stability
            if (tau_hr <= 0f || dt_hr <= 0f)
                return;

            // First-order lag: dP/dt = (P_eq - P_current) / τ
            float dP = (state.EquilibriumBackpressure_psia - state.Backpressure_psia)
                      * dt_hr / tau_hr;

            state.Backpressure_psia += dP;

            // Clamp to physical limits: cannot go below perfect vacuum or above atmospheric
            state.Backpressure_psia = Mathf.Clamp(
                state.Backpressure_psia,
                0.1f, // Near-perfect vacuum lower bound (practical limit)
                PlantConstants.P_ATM); // Cannot exceed atmospheric

            // Convert to vacuum reading
            state.Vacuum_inHg = VacuumFromAbsolute(state.Backpressure_psia);

            // Clamp vacuum to 0-29.92 range
            state.Vacuum_inHg = Mathf.Clamp(state.Vacuum_inHg, 0f,
                PlantConstants.Condenser.ATM_PRESSURE_INHG);
        }

        // ====================================================================
        // C-9 INTERLOCK
        // ====================================================================

        /// <summary>
        /// Evaluate C-9 "Condenser Available" interlock.
        ///
        /// Per NRC HRTD: C-9 requires both conditions simultaneously:
        ///   1. Condenser vacuum ≥ 22 in. Hg (2/2 pressure switches)
        ///   2. At least 1 CW pump running (1/2 breaker status)
        ///
        /// C-9 is evaluated every timestep. Loss of vacuum or CW pumps
        /// immediately removes C-9, forcing steam dumps closed.
        /// </summary>
        private static void EvaluateC9(ref CondenserState state)
        {
            bool vacuumOK = state.Vacuum_inHg >= PlantConstants.Condenser.C9_VACUUM_THRESHOLD_INHG;
            bool cwOK = state.CW_PumpsRunning >= PlantConstants.Condenser.C9_MIN_CW_PUMPS;

            state.C9_CondenserAvailable = vacuumOK && cwOK;
        }

        // ====================================================================
        // UTILITY METHODS
        // ====================================================================

        /// <summary>
        /// Convert vacuum reading (in. Hg) to absolute pressure (psia).
        /// P_abs = (ATM_INHG - Vacuum_inHg) / INHG_PER_PSIA
        /// </summary>
        /// <param name="vacuum_inHg">Vacuum reading in inches Hg</param>
        /// <returns>Absolute pressure in psia</returns>
        public static float AbsoluteFromVacuum(float vacuum_inHg)
        {
            float abs_inHg = PlantConstants.Condenser.ATM_PRESSURE_INHG - vacuum_inHg;
            return abs_inHg / PlantConstants.Condenser.INHG_PER_PSIA;
        }

        /// <summary>
        /// Convert absolute pressure (psia) to vacuum reading (in. Hg).
        /// Vacuum_inHg = ATM_INHG - (P_abs × INHG_PER_PSIA)
        /// </summary>
        /// <param name="pressure_psia">Absolute pressure in psia</param>
        /// <returns>Vacuum reading in inches Hg</returns>
        public static float VacuumFromAbsolute(float pressure_psia)
        {
            return PlantConstants.Condenser.ATM_PRESSURE_INHG
                 - (pressure_psia * PlantConstants.Condenser.INHG_PER_PSIA);
        }

        /// <summary>
        /// Get hotwell saturation temperature at the current condenser backpressure.
        /// Uses WaterProperties.SaturationTemperature() for the low-pressure range.
        /// </summary>
        /// <param name="backpressure_psia">Condenser backpressure in psia</param>
        /// <returns>Hotwell water saturation temperature in °F</returns>
        public static float GetHotwellSatTemp(float backpressure_psia)
        {
            // At very low backpressure (< 1 psia), clamp to prevent out-of-range
            float clampedP = Mathf.Max(backpressure_psia, 0.5f);
            return WaterProperties.SaturationTemperature(clampedP);
        }

        // ====================================================================
        // CW PUMP CONTROL
        // ====================================================================

        /// <summary>
        /// Start an additional CW pump.
        /// </summary>
        /// <param name="state">Condenser state to modify</param>
        public static void StartCWPump(ref CondenserState state)
        {
            if (state.CW_PumpsRunning < PlantConstants.Condenser.CW_PUMP_COUNT)
            {
                state.CW_PumpsRunning++;
                Debug.Log($"[Condenser] CW pump started. Running: {state.CW_PumpsRunning}/{PlantConstants.Condenser.CW_PUMP_COUNT}");
            }
        }

        /// <summary>
        /// Stop a CW pump. C-9 will be re-evaluated on next Update().
        /// </summary>
        /// <param name="state">Condenser state to modify</param>
        public static void StopCWPump(ref CondenserState state)
        {
            if (state.CW_PumpsRunning > 0)
            {
                state.CW_PumpsRunning--;
                Debug.Log($"[Condenser] CW pump stopped. Running: {state.CW_PumpsRunning}/{PlantConstants.Condenser.CW_PUMP_COUNT}");
            }
        }

        // ====================================================================
        // STATUS AND DIAGNOSTICS
        // ====================================================================

        /// <summary>
        /// Update the status message based on current condenser state.
        /// </summary>
        private static void UpdateStatusMessage(ref CondenserState state)
        {
            switch (state.PulldownPhase)
            {
                case VacuumPulldownPhase.Atmospheric:
                    state.StatusMessage = "Condenser at atmospheric — no vacuum";
                    break;

                case VacuumPulldownPhase.Hogging:
                    state.StatusMessage = $"Hogging ejectors — {state.Vacuum_inHg:F1} in. Hg";
                    break;

                case VacuumPulldownPhase.MainEjectors:
                    state.StatusMessage = $"Main ejectors — {state.Vacuum_inHg:F1} in. Hg";
                    break;

                case VacuumPulldownPhase.Established:
                    string c9Status = state.C9_CondenserAvailable ? "C-9 TRUE" : "C-9 FALSE";
                    state.StatusMessage = $"Vacuum {state.Vacuum_inHg:F1} in. Hg — {c9Status}";
                    break;
            }
        }

        /// <summary>
        /// Get a diagnostic string for logging and telemetry.
        /// </summary>
        /// <param name="state">Condenser state to format</param>
        /// <returns>Multi-line diagnostic string</returns>
        public static string GetDiagnosticString(in CondenserState state)
        {
            return $"--- CONDENSER ---\n" +
                   $"  Phase:           {state.PulldownPhase}\n" +
                   $"  Vacuum:          {state.Vacuum_inHg:F1} in. Hg\n" +
                   $"  Backpressure:    {state.Backpressure_psia:F2} psia\n" +
                   $"  Hotwell Temp:    {state.HotwellTemp_F:F1} °F\n" +
                   $"  CW Pumps:        {state.CW_PumpsRunning}/{PlantConstants.Condenser.CW_PUMP_COUNT}\n" +
                   $"  Air Ejectors:    {(state.AirEjectorsRunning ? "Running" : "Off")}\n" +
                   $"  C-9 Available:   {state.C9_CondenserAvailable}\n" +
                   $"  Steam Load:      {state.SteamLoad_BTUhr:E2} BTU/hr\n" +
                   $"  Eq. Backpress:   {state.EquilibriumBackpressure_psia:F2} psia\n" +
                   $"  Status:          {state.StatusMessage}";
        }

        // ====================================================================
        // VALIDATION
        // ====================================================================

        /// <summary>
        /// Validate condenser physics calculations.
        /// Exercises initialization, vacuum pulldown, dynamics, and C-9 logic.
        /// </summary>
        /// <returns>True if all validations pass</returns>
        public static bool ValidateCalculations()
        {
            bool valid = true;

            // Test 1: Initialization should produce atmospheric state
            var state = Initialize();
            if (state.Vacuum_inHg != 0f) valid = false;
            if (state.C9_CondenserAvailable) valid = false;
            if (state.PulldownPhase != VacuumPulldownPhase.Atmospheric) valid = false;
            if (state.Backpressure_psia != PlantConstants.P_ATM) valid = false;

            // Test 2: Vacuum pulldown should start hogging phase
            StartVacuumPulldown(ref state, 5.0f);
            if (state.PulldownPhase != VacuumPulldownPhase.Hogging) valid = false;
            if (state.CW_PumpsRunning != 2) valid = false;
            if (!state.AirEjectorsRunning) valid = false;

            // Test 3: Vacuum/pressure conversion round-trip
            float testVacuum = 26.0f;
            float testAbs = AbsoluteFromVacuum(testVacuum);
            float roundTrip = VacuumFromAbsolute(testAbs);
            if (Mathf.Abs(roundTrip - testVacuum) > 0.01f) valid = false;

            // Test 4: C-9 threshold — vacuum of 22 in. Hg with CW should be true
            state.Vacuum_inHg = 22.0f;
            state.CW_PumpsRunning = 1;
            EvaluateC9(ref state);
            if (!state.C9_CondenserAvailable) valid = false;

            // Test 5: C-9 threshold — vacuum of 21.9 in. Hg should be false
            state.Vacuum_inHg = 21.9f;
            EvaluateC9(ref state);
            if (state.C9_CondenserAvailable) valid = false;

            // Test 6: C-9 — adequate vacuum but no CW should be false
            state.Vacuum_inHg = 26.0f;
            state.CW_PumpsRunning = 0;
            EvaluateC9(ref state);
            if (state.C9_CondenserAvailable) valid = false;

            // Test 7: Equilibrium backpressure at zero load should be near design
            float eqP = CalculateEquilibriumBackpressure(0f, 1e9f, VacuumPulldownPhase.Established);
            if (Mathf.Abs(eqP - PlantConstants.Condenser.DESIGN_BACKPRESSURE_PSIA) > 0.01f) valid = false;

            // Test 8: Equilibrium backpressure at full load should be near max
            float fullLoad = PlantConstants.Condenser.CW_PUMP_MAX_REJECTION_BTUHR * 2f; // 2 pumps
            float eqPFull = CalculateEquilibriumBackpressure(fullLoad, fullLoad, VacuumPulldownPhase.Established);
            if (Mathf.Abs(eqPFull - PlantConstants.Condenser.MAX_OPERATING_BACKPRESSURE_PSIA) > 0.01f) valid = false;

            // Test 9: Hotwell temp at design vacuum should be near 120°F
            float hotwellT = GetHotwellSatTemp(PlantConstants.Condenser.DESIGN_BACKPRESSURE_PSIA);
            // At ~1.5 psia, T_sat should be ~115°F from steam tables (WaterProperties range)
            if (hotwellT < 100f || hotwellT > 140f) valid = false;

            return valid;
        }
    }
}
