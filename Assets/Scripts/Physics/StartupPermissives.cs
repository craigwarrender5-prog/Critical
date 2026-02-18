// ============================================================================
// CRITICAL: Master the Atom — Startup Permissives Module
// StartupPermissives.cs — C-9/P-12 Interlock Evaluation and Steam Dump Bridge FSM
// ============================================================================
//
// PURPOSE:
//   Evaluates plant startup interlock permissives (C-9 Condenser Available,
//   P-12 Low-Low Tavg) and manages the steam dump bridge state machine.
//   Provides a single authoritative gating contract consumed by
//   SteamDumpController.
//
// INTERLOCKS:
//   C-9 (Condenser Available):
//     - Pass-through from CondenserPhysics.C9_CondenserAvailable
//     - Requires vacuum ≥ 22 in. Hg AND CW pump ≥ 1
//     - When false: steam dumps blocked from opening
//
//   P-12 (Low-Low Tavg):
//     - Active when T_avg < 553°F
//     - Blocks steam dumps unless deliberately bypassed per operator action
//     - During startup, bypassed when T_avg > ~350°F and condenser established
//
// STEAM DUMP BRIDGE FSM:
//   DumpsUnavailable → DumpsArmed → DumpsModulating
//
//   DumpsUnavailable: !C9 OR (P12 active AND not bypassed)
//   DumpsArmed:       C9 AND !P12_blocking AND mode selected; valves closed
//   DumpsModulating:  Armed + pressure > (setpoint + deadband)
//
// SOURCES:
//   - NRC HRTD Section 11.2 — Steam Dump Control System
//   - Technical_Documentation/Startup_Boundary_and_SteamDump_Authoritative_Spec.md
//   - Technical_Documentation/PWR_Startup_State_Sequence.md
//   - Technical_Documentation/Condenser_Feedwater_Architecture_Specification.md
//
// ARCHITECTURE:
//   - Called by: HeatupSimEngine.StepSimulation() (Stage I integration)
//   - Consumes: CondenserState (from CondenserPhysics)
//   - Consumed by: SteamDumpController (gating authority)
//   - State owned: PermissiveState struct (owned by engine, passed by ref)
//   - Pattern: Static module with Initialize() / Evaluate()
//
// CS REFERENCE: CS-0115
// IP REFERENCE: IP-0046, Stage H
// VERSION: 1.0.0
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;

namespace Critical.Physics
{
    // ========================================================================
    // STEAM DUMP BRIDGE STATE ENUM
    // ========================================================================

    /// <summary>
    /// Steam dump bridge states per the Startup Boundary Authoritative Spec.
    /// Governs whether steam dump valves can open.
    /// </summary>
    public enum SteamDumpBridgeState
    {
        /// <summary>
        /// Dumps unavailable: !C9 OR (P12 active AND no bypass).
        /// Dump valves forced closed. SG pressure follows thermodynamics
        /// and boundary-state mass balance only.
        /// </summary>
        DumpsUnavailable,

        /// <summary>
        /// Dumps armed but closed: C9 AND !P12_blocking AND mode selected.
        /// Controller armed; valves remain closed within pressure deadband.
        /// </summary>
        DumpsArmed,

        /// <summary>
        /// Dumps modulating: Armed + SG pressure above (setpoint + deadband).
        /// Valves modulate to hold steam pressure near setpoint.
        /// </summary>
        DumpsModulating
    }

    // ========================================================================
    // STATE STRUCT
    // ========================================================================

    /// <summary>
    /// Startup permissive state for persistence between timesteps.
    /// Owned by HeatupSimEngine, passed by ref to StartupPermissives.Evaluate().
    /// </summary>
    public struct PermissiveState
    {
        // --- C-9: Condenser Available ---

        /// <summary>C-9 permissive satisfied (from CondenserPhysics)</summary>
        public bool C9_Satisfied;

        // --- P-12: Low-Low T_avg ---

        /// <summary>P-12 is active (T_avg below P-12 threshold)</summary>
        public bool P12_Active;

        /// <summary>P-12 has been deliberately bypassed by operator action</summary>
        public bool P12_Bypassed;

        /// <summary>P-12 is blocking steam dump operation (active AND not bypassed)</summary>
        public bool P12_Blocking;

        // --- Steam Dump Bridge ---

        /// <summary>Current steam dump bridge state</summary>
        public SteamDumpBridgeState BridgeState;

        // --- Combined Authority ---

        /// <summary>
        /// Final steam dump authority: C9 satisfied AND P12 not blocking.
        /// When false, SteamDumpController must force valves closed.
        /// </summary>
        public bool SteamDumpPermitted;

        // --- Telemetry ---

        /// <summary>Current P-12 threshold value in °F (for display)</summary>
        public float P12_Threshold_F;

        /// <summary>Status message for display and logging</summary>
        public string StatusMessage;
    }

    // ========================================================================
    // PHYSICS MODULE
    // ========================================================================

    /// <summary>
    /// Startup Permissives Module — C-9/P-12 evaluation and steam dump bridge FSM.
    ///
    /// Per the Startup Boundary Authoritative Spec:
    ///   - C-9 (Condenser Available) must be satisfied for steam dumps to open
    ///   - P-12 (Low-Low Tavg) blocks steam dumps when active unless bypassed
    ///   - Steam pressure mode selection does not override C-9 or P-12
    ///
    /// This module provides the single authoritative gating signal consumed by
    /// SteamDumpController to determine whether dump valves may open.
    /// </summary>
    public static class StartupPermissives
    {
        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        /// <summary>
        /// Initialize permissive state to all-blocked (cold startup).
        /// C-9 false (no condenser vacuum), P-12 active (T_avg << 553°F).
        /// </summary>
        /// <returns>Initialized permissive state with all interlocks blocking</returns>
        public static PermissiveState Initialize()
        {
            return new PermissiveState
            {
                C9_Satisfied = false,
                P12_Active = true,
                P12_Bypassed = false,
                P12_Blocking = true,
                BridgeState = SteamDumpBridgeState.DumpsUnavailable,
                SteamDumpPermitted = false,
                P12_Threshold_F = PlantConstants.Condenser.P12_LOW_LOW_TAVG_F,
                StatusMessage = "Dumps unavailable — C-9 false, P-12 active"
            };
        }

        // ====================================================================
        // MAIN EVALUATION
        // ====================================================================

        /// <summary>
        /// Evaluate startup permissives for current plant conditions.
        ///
        /// 1. C-9 pass-through from condenser state
        /// 2. P-12 evaluation against T_avg
        /// 3. Steam dump bridge FSM transition
        /// 4. Final authority determination
        /// </summary>
        /// <param name="state">Permissive state (modified in place)</param>
        /// <param name="condenserState">Current condenser state (read-only)</param>
        /// <param name="T_avg_F">Current RCS average temperature in °F</param>
        /// <param name="steamDumpModeSelected">True if steam dump mode is selected (not OFF)</param>
        /// <param name="steamPressure_psig">Current steam header pressure in psig</param>
        /// <param name="dumpSetpoint_psig">Steam dump pressure setpoint in psig</param>
        /// <param name="deadband_psi">Pressure deadband for dump modulation in psi</param>
        public static void Evaluate(
            ref PermissiveState state,
            in CondenserState condenserState,
            float T_avg_F,
            bool steamDumpModeSelected,
            float steamPressure_psig,
            float dumpSetpoint_psig,
            float deadband_psi)
        {
            // ================================================================
            // 1. C-9 PASS-THROUGH
            // ================================================================
            state.C9_Satisfied = condenserState.C9_CondenserAvailable;

            // ================================================================
            // 2. P-12 EVALUATION
            // ================================================================
            state.P12_Active = T_avg_F < PlantConstants.Condenser.P12_LOW_LOW_TAVG_F;
            state.P12_Blocking = state.P12_Active && !state.P12_Bypassed;

            // ================================================================
            // 3. COMBINED AUTHORITY
            // ================================================================
            state.SteamDumpPermitted = state.C9_Satisfied && !state.P12_Blocking;

            // ================================================================
            // 4. BRIDGE FSM
            // ================================================================
            if (!state.SteamDumpPermitted)
            {
                state.BridgeState = SteamDumpBridgeState.DumpsUnavailable;
            }
            else if (steamDumpModeSelected)
            {
                // Permitted and mode selected — check pressure for modulation
                float pressureError = steamPressure_psig - dumpSetpoint_psig;

                if (pressureError > deadband_psi)
                {
                    state.BridgeState = SteamDumpBridgeState.DumpsModulating;
                }
                else
                {
                    state.BridgeState = SteamDumpBridgeState.DumpsArmed;
                }
            }
            else
            {
                // Permitted but no mode selected — armed state not possible
                state.BridgeState = SteamDumpBridgeState.DumpsUnavailable;
            }

            // ================================================================
            // 5. STATUS MESSAGE
            // ================================================================
            UpdateStatusMessage(ref state);
        }

        // ====================================================================
        // P-12 BYPASS CONTROL
        // ====================================================================

        /// <summary>
        /// Set or clear the P-12 bypass.
        ///
        /// During startup, P-12 is bypassed per procedure when T_avg > ~350°F
        /// and condenser vacuum is established, to allow steam dump arming
        /// before T_avg reaches the 553°F P-12 auto-clear threshold.
        /// </summary>
        /// <param name="state">Permissive state to modify</param>
        /// <param name="bypassed">True to bypass P-12, false to restore blocking</param>
        public static void SetP12Bypass(ref PermissiveState state, bool bypassed)
        {
            if (state.P12_Bypassed != bypassed)
            {
                state.P12_Bypassed = bypassed;
                Debug.Log($"[Permissives] P-12 bypass {(bypassed ? "ENGAGED" : "REMOVED")}");
            }
        }

        // ====================================================================
        // STATUS AND DIAGNOSTICS
        // ====================================================================

        /// <summary>Update status message based on current permissive state.</summary>
        private static void UpdateStatusMessage(ref PermissiveState state)
        {
            string c9 = state.C9_Satisfied ? "C-9 TRUE" : "C-9 FALSE";
            string p12;

            if (!state.P12_Active)
                p12 = "P-12 CLEAR";
            else if (state.P12_Bypassed)
                p12 = "P-12 BYPASSED";
            else
                p12 = "P-12 BLOCKING";

            state.StatusMessage = $"{state.BridgeState} — {c9}, {p12}";
        }

        /// <summary>
        /// Get a diagnostic string for logging and telemetry.
        /// </summary>
        public static string GetDiagnosticString(in PermissiveState state)
        {
            return $"--- STARTUP PERMISSIVES ---\n" +
                   $"  C-9 Satisfied:     {state.C9_Satisfied}\n" +
                   $"  P-12 Active:       {state.P12_Active}\n" +
                   $"  P-12 Bypassed:     {state.P12_Bypassed}\n" +
                   $"  P-12 Blocking:     {state.P12_Blocking}\n" +
                   $"  Dump Permitted:    {state.SteamDumpPermitted}\n" +
                   $"  Bridge State:      {state.BridgeState}\n" +
                   $"  P-12 Threshold:    {state.P12_Threshold_F:F1} °F\n" +
                   $"  Status:            {state.StatusMessage}";
        }
    }
}
