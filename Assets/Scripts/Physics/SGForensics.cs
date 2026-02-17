// ============================================================================
// CRITICAL: Master the Atom â€” SG Forensics Black Box Logging
// SGForensics.cs â€” High-Resolution Ring Buffer & Triggered Dump
// ============================================================================
//
// PURPOSE:
//   Provides seconds-resolution forensic capture of SG thermal state to
//   diagnose MW-scale heat transfer spikes, regime transition discontinuities,
//   and drain-triggered thermal shifts that are invisible at the 15-minute
//   interval log resolution.
//
// ARCHITECTURE:
//   - ForensicsSnapshot: Flat struct of primitives (no arrays, no references).
//     Captures a complete picture of SG thermal state at one timestep.
//   - Ring buffer: Fixed-size circular buffer (~90 entries at 2-second sim
//     resolution = ~180 seconds of history). Configurable via BUFFER_SIZE.
//   - Trigger evaluation: Called after each SG update. Fires dump on:
//       1. SG regime change (Subcooled â†” Boiling â†” SteamDump)
//       2. |Î” sgHeatTransfer_MW| > 5 MW between consecutive timesteps
//       3. SG drain start/stop edge
//       4. Inventory conservation alarm edge (rising only)
//   - CSV dump: Writes entire ring buffer to Logs/Forensics/ with metadata
//     header. One file per trigger event.
//
//   This module is STATELESS except for the ring buffer and previous-frame
//   tracking fields. It does not modify any physics state.
//
// CALLED BY:
//   HeatupSimEngine.cs â€” after each SG model update in all three regimes.
//
// v5.0.1: Initial implementation
// ============================================================================

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Critical.Physics
{
    // ========================================================================
    // FORENSICS SNAPSHOT â€” Flat primitives only, no heap allocations
    // ========================================================================

    /// <summary>
    /// Complete SG thermal state at a single timestep.
    /// All fields are value types (float, int, bool, enum) to avoid
    /// GC pressure in the ring buffer. Captured every physics step.
    /// </summary>
    public struct ForensicsSnapshot
    {
        // --- Timing ---
        /// <summary>Simulation time in hours</summary>
        public float SimTime_hr;

        // --- RCS State ---
        /// <summary>RCS bulk temperature (Â°F)</summary>
        public float T_rcs_F;
        /// <summary>RCS pressure (psia)</summary>
        public float Pressure_psia;
        /// <summary>Number of RCPs running (0-4)</summary>
        public int RCPCount;
        /// <summary>Effective RCP heat input (MW)</summary>
        public float RCPHeat_MW;

        // --- SG Regime ---
        /// <summary>Current SG thermal regime</summary>
        public SGThermalRegime Regime;

        // --- SG Heat Transfer ---
        /// <summary>Total SG heat absorption across all 4 SGs (MW)</summary>
        public float SGHeatTransfer_MW;
        /// <summary>Total SG heat absorption (BTU/hr)</summary>
        public float SGHeatTransfer_BTUhr;

        // --- SG Temperatures ---
        /// <summary>SG secondary bulk average temperature (Â°F)</summary>
        public float SGBulkTemp_F;
        /// <summary>SG top node temperature (Â°F)</summary>
        public float SGTopNodeTemp_F;
        /// <summary>SG bottom node temperature (Â°F)</summary>
        public float SGBottomNodeTemp_F;
        /// <summary>Stratification Î”T between top and bottom nodes (Â°F)</summary>
        public float SGStratification_F;

        // --- SG Secondary Pressure ---
        /// <summary>SG secondary side pressure (psia)</summary>
        public float SGSecondaryPressure_psia;
        /// <summary>Saturation temperature at SG secondary pressure (Â°F)</summary>
        public float SGSaturationTemp_F;
        /// <summary>Max node superheat above T_sat (Â°F)</summary>
        public float SGMaxSuperheat_F;
        /// <summary>Boiling intensity fraction (0-1)</summary>
        public float SGBoilingIntensity;

        // --- SG Thermocline ---
        /// <summary>Thermocline height from tubesheet (ft)</summary>
        public float ThermoclineHeight_ft;
        /// <summary>Active area fraction (above thermocline)</summary>
        public float ActiveAreaFraction;

        // --- SG Mass / Level ---
        /// <summary>Total secondary water mass all 4 SGs (lb)</summary>
        public float SecondaryMass_lb;
        /// <summary>Wide Range level (%)</summary>
        public float WideRangeLevel_pct;
        /// <summary>Narrow Range level (%)</summary>
        public float NarrowRangeLevel_pct;

        // --- SG Draining ---
        /// <summary>True if SG blowdown draining is in progress</summary>
        public bool DrainingActive;
        /// <summary>True if draining has completed</summary>
        public bool DrainingComplete;
        /// <summary>Per-SG drain rate (gpm)</summary>
        public float DrainingRate_gpm;

        // --- Steam Production ---
        /// <summary>Instantaneous steam production rate (lb/hr)</summary>
        public float SteamRate_lbhr;
        /// <summary>Steam production as thermal power (MW)</summary>
        public float SteamRate_MW;
        /// <summary>Cumulative steam produced since boiling onset (lb)</summary>
        public float TotalSteamProduced_lb;

        // --- Per-Node HTC (5 nodes max) ---
        /// <summary>Node 0 (top) HTC BTU/(hrÂ·ftÂ²Â·Â°F)</summary>
        public float Node0_HTC;
        /// <summary>Node 1 HTC</summary>
        public float Node1_HTC;
        /// <summary>Node 2 HTC</summary>
        public float Node2_HTC;
        /// <summary>Node 3 HTC</summary>
        public float Node3_HTC;
        /// <summary>Node 4 (bottom) HTC</summary>
        public float Node4_HTC;

        // --- Per-Node Area Fractions ---
        /// <summary>Node 0 effective area fraction</summary>
        public float Node0_AreaFrac;
        /// <summary>Node 1 effective area fraction</summary>
        public float Node1_AreaFrac;
        /// <summary>Node 2 effective area fraction</summary>
        public float Node2_AreaFrac;
        /// <summary>Node 3 effective area fraction</summary>
        public float Node3_AreaFrac;
        /// <summary>Node 4 effective area fraction</summary>
        public float Node4_AreaFrac;

        // --- Per-Node Heat Rates (MW) ---
        /// <summary>Node 0 heat rate (MW)</summary>
        public float Node0_Q_MW;
        /// <summary>Node 1 heat rate (MW)</summary>
        public float Node1_Q_MW;
        /// <summary>Node 2 heat rate (MW)</summary>
        public float Node2_Q_MW;
        /// <summary>Node 3 heat rate (MW)</summary>
        public float Node3_Q_MW;
        /// <summary>Node 4 heat rate (MW)</summary>
        public float Node4_Q_MW;

        // --- Per-Node Regime Blend (v5.0.1) ---
        /// <summary>Node 0 regime blend factor (0=subcooled, 1=boiling)</summary>
        public float Node0_Blend;
        /// <summary>Node 1 regime blend</summary>
        public float Node1_Blend;
        /// <summary>Node 2 regime blend</summary>
        public float Node2_Blend;
        /// <summary>Node 3 regime blend</summary>
        public float Node3_Blend;
        /// <summary>Node 4 regime blend</summary>
        public float Node4_Blend;

        // --- Per-Node Boiling State ---
        /// <summary>Node 0 boiling</summary>
        public bool Node0_Boiling;
        /// <summary>Node 1 boiling</summary>
        public bool Node1_Boiling;
        /// <summary>Node 2 boiling</summary>
        public bool Node2_Boiling;
        /// <summary>Node 3 boiling</summary>
        public bool Node3_Boiling;
        /// <summary>Node 4 boiling</summary>
        public bool Node4_Boiling;

        // --- Per-Node Temperatures ---
        /// <summary>Node 0 temperature (Â°F)</summary>
        public float Node0_Temp_F;
        /// <summary>Node 1 temperature (Â°F)</summary>
        public float Node1_Temp_F;
        /// <summary>Node 2 temperature (Â°F)</summary>
        public float Node2_Temp_F;
        /// <summary>Node 3 temperature (Â°F)</summary>
        public float Node3_Temp_F;
        /// <summary>Node 4 temperature (Â°F)</summary>
        public float Node4_Temp_F;

        // --- Inventory Audit ---
        /// <summary>True if inventory conservation alarm is active</summary>
        public bool InventoryAlarm;

        // --- Engine Regime ---
        /// <summary>Engine physics regime (1=Isolated, 2=Blended, 3=Coupled)</summary>
        public int EngineRegime;
        /// <summary>Coupling factor alpha (0-1, Regime 2 blend factor)</summary>
        public float CouplingAlpha;

        // --- Heater State ---
        /// <summary>Pressurizer heater power (MW)</summary>
        public float PZRHeaterPower_MW;
    }

    // ========================================================================
    // TRIGGER REASON ENUM
    // ========================================================================

    /// <summary>
    /// Identifies why a forensic dump was triggered.
    /// Multiple triggers can fire on the same timestep; the first one
    /// detected takes priority for the filename.
    /// </summary>
    public enum ForensicsTrigger
    {
        /// <summary>SG thermal regime changed (Subcooled â†” Boiling â†” SteamDump)</summary>
        RegimeChange,

        /// <summary>|Î” sgHeatTransfer_MW| > 5 MW between consecutive timesteps</summary>
        HeatTransferSpike,

        /// <summary>SG drain started (DrainingActive: false â†’ true)</summary>
        DrainStart,

        /// <summary>SG drain stopped (DrainingActive: true â†’ false)</summary>
        DrainStop,

        /// <summary>Inventory conservation alarm activated</summary>
        InventoryAlarm
    }

    // ========================================================================
    // SG FORENSICS MODULE
    // ========================================================================

    /// <summary>
    /// Black box forensic logging for SG thermal diagnostics.
    /// Maintains a ring buffer of high-resolution snapshots and writes
    /// CSV dumps when trigger conditions are met.
    ///
    /// Thread safety: Not thread-safe. Called only from the main simulation
    /// loop on Unity's main thread.
    ///
    /// Memory: Ring buffer is allocated once at Initialize(). No per-frame
    /// heap allocations during normal operation. CSV StringBuilder is only
    /// allocated when a dump fires.
    ///
    /// v5.0.1: Initial implementation
    /// </summary>
    public static class SGForensics
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Ring buffer size. At 10-second sim timesteps (dt = 1/360 hr),
        /// 90 entries = 900 sim-seconds = 15 sim-minutes of history.
        /// Captures the full window around any transient event.
        /// </summary>
        public const int BUFFER_SIZE = 90;

        /// <summary>
        /// Heat transfer delta threshold in MW. If |Q_current - Q_previous|
        /// exceeds this value in a single timestep, a dump is triggered.
        /// 5 MW is well above any physically realistic single-step change
        /// during normal heatup (typical rate is ~0.1 MW/step).
        /// </summary>
        public const float HEAT_TRANSFER_SPIKE_THRESHOLD_MW = 5.0f;

        /// <summary>Conversion: MW to BTU/hr</summary>
        private const float MW_TO_BTU_HR = 3.412e6f;

        // ====================================================================
        // RING BUFFER STATE
        // ====================================================================

        private static ForensicsSnapshot[] _buffer;
        private static int _writeIndex;
        private static int _count;
        private static bool _initialized;

        // ====================================================================
        // PREVIOUS-FRAME STATE â€” For edge detection
        // ====================================================================

        private static SGThermalRegime _prevRegime;
        private static float _prevSGHeatTransfer_MW;
        private static bool _prevDrainingActive;
        private static bool _prevInventoryAlarm;

        // ====================================================================
        // DUMP TRACKING â€” Cooldown to prevent dump floods
        // ====================================================================

        /// <summary>
        /// Minimum sim-time between consecutive dumps (hours).
        /// 30 sim-seconds = 30/3600 â‰ˆ 0.00833 hr.
        /// Prevents a single oscillating condition from generating
        /// hundreds of files.
        /// </summary>
        private const float DUMP_COOLDOWN_HR = 30f / 3600f;

        private static float _lastDumpTime_hr;
        private static int _dumpCount;

        // ====================================================================
        // OUTPUT PATH
        // ====================================================================

        private static string _forensicsPath;

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Initialize the forensics system. Allocates ring buffer and sets
        /// initial edge detection state. Call once at simulation start.
        /// </summary>
        /// <param name="logBasePath">
        /// Base log directory (e.g., Application.dataPath/../HeatupLogs).
        /// Forensics subfolder will be created at logBasePath/Forensics/.
        /// </param>
        public static void Initialize(string logBasePath)
        {
            _buffer = new ForensicsSnapshot[BUFFER_SIZE];
            _writeIndex = 0;
            _count = 0;
            _initialized = true;

            _prevRegime = SGThermalRegime.Subcooled;
            _prevSGHeatTransfer_MW = 0f;
            _prevDrainingActive = false;
            _prevInventoryAlarm = false;

            _lastDumpTime_hr = -1f;
            _dumpCount = 0;

            // Create forensics output directory
            _forensicsPath = Path.Combine(logBasePath, "Forensics");
            if (!Directory.Exists(_forensicsPath))
            {
                Directory.CreateDirectory(_forensicsPath);
                Debug.Log($"[SGForensics] Created forensics directory: {_forensicsPath}");
            }

            Debug.Log($"[SGForensics] Initialized â€” buffer={BUFFER_SIZE} slots, " +
                      $"spike threshold={HEAT_TRANSFER_SPIKE_THRESHOLD_MW} MW, " +
                      $"cooldown={DUMP_COOLDOWN_HR * 3600f:F0}s");
        }

        /// <summary>
        /// Record a snapshot into the ring buffer. Call every physics timestep
        /// after the SG model update.
        /// </summary>
        /// <param name="snapshot">Pre-filled snapshot struct</param>
        public static void RecordSnapshot(ForensicsSnapshot snapshot)
        {
            if (!_initialized || _buffer == null) return;

            _buffer[_writeIndex] = snapshot;
            _writeIndex = (_writeIndex + 1) % BUFFER_SIZE;
            if (_count < BUFFER_SIZE) _count++;
        }

        /// <summary>
        /// Evaluate trigger conditions and write a dump if any fire.
        /// Call after RecordSnapshot() each timestep.
        /// </summary>
        /// <param name="currentRegime">Current SG thermal regime</param>
        /// <param name="currentSGHeat_MW">Current total SG heat transfer (MW)</param>
        /// <param name="drainingActive">Current SG draining state</param>
        /// <param name="inventoryAlarm">Current inventory conservation alarm state</param>
        /// <param name="simTime_hr">Current simulation time (hours)</param>
        /// <returns>True if a dump was written</returns>
        public static bool EvaluateTriggers(
            SGThermalRegime currentRegime,
            float currentSGHeat_MW,
            bool drainingActive,
            bool inventoryAlarm,
            float simTime_hr)
        {
            if (!_initialized) return false;

            // Check cooldown
            if (simTime_hr - _lastDumpTime_hr < DUMP_COOLDOWN_HR)
            {
                // Still update previous-frame state even during cooldown
                _prevRegime = currentRegime;
                _prevSGHeatTransfer_MW = currentSGHeat_MW;
                _prevDrainingActive = drainingActive;
                _prevInventoryAlarm = inventoryAlarm;
                return false;
            }

            ForensicsTrigger? trigger = null;
            string triggerDetail = null;

            // --- Trigger 1: Regime change ---
            if (currentRegime != _prevRegime)
            {
                trigger = ForensicsTrigger.RegimeChange;
                triggerDetail = $"{_prevRegime} -> {currentRegime}";
            }

            // --- Trigger 2: Heat transfer spike ---
            if (trigger == null)
            {
                float delta = Mathf.Abs(currentSGHeat_MW - _prevSGHeatTransfer_MW);
                if (delta > HEAT_TRANSFER_SPIKE_THRESHOLD_MW)
                {
                    trigger = ForensicsTrigger.HeatTransferSpike;
                    triggerDetail = $"delta={delta:F2} MW ({_prevSGHeatTransfer_MW:F2} -> {currentSGHeat_MW:F2})";
                }
            }

            // --- Trigger 3: Drain start edge ---
            if (trigger == null && drainingActive && !_prevDrainingActive)
            {
                trigger = ForensicsTrigger.DrainStart;
                triggerDetail = "DrainingActive: false -> true";
            }

            // --- Trigger 4: Drain stop edge ---
            if (trigger == null && !drainingActive && _prevDrainingActive)
            {
                trigger = ForensicsTrigger.DrainStop;
                triggerDetail = "DrainingActive: true -> false";
            }

            // --- Trigger 5: Inventory alarm edge (rising only) ---
            if (trigger == null && inventoryAlarm && !_prevInventoryAlarm)
            {
                trigger = ForensicsTrigger.InventoryAlarm;
                triggerDetail = "Conservation alarm activated";
            }

            // Update previous-frame state
            _prevRegime = currentRegime;
            _prevSGHeatTransfer_MW = currentSGHeat_MW;
            _prevDrainingActive = drainingActive;
            _prevInventoryAlarm = inventoryAlarm;

            // Fire dump if triggered
            if (trigger.HasValue)
            {
                WriteDump(trigger.Value, triggerDetail, simTime_hr);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Build a ForensicsSnapshot from current engine and SG state.
        /// This factory method centralizes snapshot construction to avoid
        /// duplication across the three engine regime branches.
        /// </summary>
        public static ForensicsSnapshot BuildSnapshot(
            float simTime_hr,
            float T_rcs, float pressure_psia, int rcpCount, float rcpHeat_MW,
            SGMultiNodeState sgState,
            bool inventoryAlarm,
            int engineRegime, float couplingAlpha,
            float pzrHeaterPower_MW)
        {
            var snap = new ForensicsSnapshot();

            snap.SimTime_hr = simTime_hr;
            snap.T_rcs_F = T_rcs;
            snap.Pressure_psia = pressure_psia;
            snap.RCPCount = rcpCount;
            snap.RCPHeat_MW = rcpHeat_MW;

            snap.Regime = sgState.CurrentRegime;
            snap.SGHeatTransfer_MW = sgState.TotalHeatAbsorption_MW;
            snap.SGHeatTransfer_BTUhr = sgState.TotalHeatAbsorption_BTUhr;

            snap.SGBulkTemp_F = sgState.BulkAverageTemp_F;
            snap.SGTopNodeTemp_F = sgState.TopNodeTemp_F;
            snap.SGBottomNodeTemp_F = sgState.BottomNodeTemp_F;
            snap.SGStratification_F = sgState.StratificationDeltaT_F;

            snap.SGSecondaryPressure_psia = sgState.SecondaryPressure_psia;
            snap.SGSaturationTemp_F = sgState.SaturationTemp_F;
            snap.SGMaxSuperheat_F = sgState.MaxSuperheat_F;
            // Boiling intensity not stored on state; pass 0 â€” engine can override
            snap.SGBoilingIntensity = 0f;

            snap.ThermoclineHeight_ft = sgState.ThermoclineHeight_ft;
            snap.ActiveAreaFraction = sgState.ActiveAreaFraction;

            snap.SecondaryMass_lb = sgState.SecondaryWaterMass_lb;
            snap.WideRangeLevel_pct = sgState.WideRangeLevel_pct;
            snap.NarrowRangeLevel_pct = sgState.NarrowRangeLevel_pct;

            snap.DrainingActive = sgState.DrainingActive;
            snap.DrainingComplete = sgState.DrainingComplete;
            snap.DrainingRate_gpm = sgState.DrainingRate_gpm;

            snap.SteamRate_lbhr = sgState.SteamProductionRate_lbhr;
            snap.SteamRate_MW = sgState.SteamProductionRate_MW;
            snap.TotalSteamProduced_lb = sgState.TotalSteamProduced_lb;

            // Per-node data â€” flat fields to avoid array allocation
            int N = sgState.NodeCount;
            // v5.0.1: Check if blend array is available
            bool hasBlend = sgState.NodeRegimeBlend != null && sgState.NodeRegimeBlend.Length >= N;

            if (N >= 1 && sgState.NodeHTCs != null)
            {
                snap.Node0_HTC = sgState.NodeHTCs[0];
                snap.Node0_AreaFrac = sgState.NodeEffectiveAreaFractions[0];
                snap.Node0_Q_MW = sgState.NodeHeatRates[0] / MW_TO_BTU_HR;
                snap.Node0_Boiling = sgState.NodeBoiling[0];
                snap.Node0_Temp_F = sgState.NodeTemperatures[0];
                snap.Node0_Blend = hasBlend ? sgState.NodeRegimeBlend[0] : 0f;
            }
            if (N >= 2)
            {
                snap.Node1_HTC = sgState.NodeHTCs[1];
                snap.Node1_AreaFrac = sgState.NodeEffectiveAreaFractions[1];
                snap.Node1_Q_MW = sgState.NodeHeatRates[1] / MW_TO_BTU_HR;
                snap.Node1_Boiling = sgState.NodeBoiling[1];
                snap.Node1_Temp_F = sgState.NodeTemperatures[1];
                snap.Node1_Blend = hasBlend ? sgState.NodeRegimeBlend[1] : 0f;
            }
            if (N >= 3)
            {
                snap.Node2_HTC = sgState.NodeHTCs[2];
                snap.Node2_AreaFrac = sgState.NodeEffectiveAreaFractions[2];
                snap.Node2_Q_MW = sgState.NodeHeatRates[2] / MW_TO_BTU_HR;
                snap.Node2_Boiling = sgState.NodeBoiling[2];
                snap.Node2_Temp_F = sgState.NodeTemperatures[2];
                snap.Node2_Blend = hasBlend ? sgState.NodeRegimeBlend[2] : 0f;
            }
            if (N >= 4)
            {
                snap.Node3_HTC = sgState.NodeHTCs[3];
                snap.Node3_AreaFrac = sgState.NodeEffectiveAreaFractions[3];
                snap.Node3_Q_MW = sgState.NodeHeatRates[3] / MW_TO_BTU_HR;
                snap.Node3_Boiling = sgState.NodeBoiling[3];
                snap.Node3_Temp_F = sgState.NodeTemperatures[3];
                snap.Node3_Blend = hasBlend ? sgState.NodeRegimeBlend[3] : 0f;
            }
            if (N >= 5)
            {
                snap.Node4_HTC = sgState.NodeHTCs[4];
                snap.Node4_AreaFrac = sgState.NodeEffectiveAreaFractions[4];
                snap.Node4_Q_MW = sgState.NodeHeatRates[4] / MW_TO_BTU_HR;
                snap.Node4_Boiling = sgState.NodeBoiling[4];
                snap.Node4_Temp_F = sgState.NodeTemperatures[4];
                snap.Node4_Blend = hasBlend ? sgState.NodeRegimeBlend[4] : 0f;
            }

            snap.InventoryAlarm = inventoryAlarm;
            snap.EngineRegime = engineRegime;
            snap.CouplingAlpha = couplingAlpha;
            snap.PZRHeaterPower_MW = pzrHeaterPower_MW;

            return snap;
        }

        /// <summary>
        /// Get the total number of forensic dumps written this session.
        /// </summary>
        public static int DumpCount => _dumpCount;

        /// <summary>
        /// Get the number of snapshots currently in the ring buffer.
        /// </summary>
        public static int BufferCount => _count;

        // ====================================================================
        // PRIVATE â€” CSV DUMP
        // ====================================================================

        /// <summary>
        /// Write the entire ring buffer to a CSV file with metadata header.
        /// </summary>
        private static void WriteDump(ForensicsTrigger trigger, string detail, float simTime_hr)
        {
            _dumpCount++;
            _lastDumpTime_hr = simTime_hr;

            // Format sim time for filename: HH-MM-SS
            int hrs = (int)simTime_hr;
            int mins = (int)((simTime_hr - hrs) * 60f);
            int secs = (int)((simTime_hr - hrs - mins / 60f) * 3600f) % 60;
            string timeTag = $"{hrs:D2}-{mins:D2}-{secs:D2}";

            string filename = $"SG_Forensics_{_dumpCount:D3}_{timeTag}_{trigger}.csv";
            string filepath = Path.Combine(_forensicsPath, filename);

            var sb = new StringBuilder(BUFFER_SIZE * 512);  // Pre-size estimate

            // ================================================================
            // METADATA HEADER (comment lines starting with #)
            // ================================================================
            sb.AppendLine($"# SG FORENSICS DUMP #{_dumpCount}");
            sb.AppendLine($"# Trigger: {trigger}");
            sb.AppendLine($"# Detail: {detail}");
            sb.AppendLine($"# SimTime: {simTime_hr:F4} hr ({hrs:D2}:{mins:D2}:{secs:D2})");
            sb.AppendLine($"# WallTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"# BufferEntries: {_count}");
            sb.AppendLine($"# BufferSize: {BUFFER_SIZE}");
            sb.AppendLine($"# SpikeThreshold: {HEAT_TRANSFER_SPIKE_THRESHOLD_MW} MW");
            sb.AppendLine($"# Version: v5.0.1");
            sb.AppendLine("#");

            // ================================================================
            // CSV HEADER
            // ================================================================
            sb.AppendLine(
                "SimTime_hr," +
                "T_rcs_F,Pressure_psia,RCPCount,RCPHeat_MW," +
                "Regime,SGHeatTransfer_MW,SGHeatTransfer_BTUhr," +
                "SGBulkTemp_F,SGTopNodeTemp_F,SGBottomNodeTemp_F,SGStratification_F," +
                "SGSecPressure_psia,SGTsat_F,SGMaxSuperheat_F,SGBoilingIntensity," +
                "ThermoclineHeight_ft,ActiveAreaFraction," +
                "SecondaryMass_lb,WR_Level_pct,NR_Level_pct," +
                "DrainingActive,DrainingComplete,DrainingRate_gpm," +
                "SteamRate_lbhr,SteamRate_MW,TotalSteamProduced_lb," +
                "N0_HTC,N1_HTC,N2_HTC,N3_HTC,N4_HTC," +
                "N0_AreaFrac,N1_AreaFrac,N2_AreaFrac,N3_AreaFrac,N4_AreaFrac," +
                "N0_Q_MW,N1_Q_MW,N2_Q_MW,N3_Q_MW,N4_Q_MW," +
                "N0_Blend,N1_Blend,N2_Blend,N3_Blend,N4_Blend," +
                "N0_Boiling,N1_Boiling,N2_Boiling,N3_Boiling,N4_Boiling," +
                "N0_Temp_F,N1_Temp_F,N2_Temp_F,N3_Temp_F,N4_Temp_F," +
                "InventoryAlarm,EngineRegime,CouplingAlpha,PZRHeaterPower_MW"
            );

            // ================================================================
            // CSV DATA â€” Walk ring buffer from oldest to newest
            // ================================================================
            int startIdx;
            if (_count < BUFFER_SIZE)
                startIdx = 0;  // Buffer not yet full â€” start at 0
            else
                startIdx = _writeIndex;  // Buffer full â€” oldest entry is at write index

            for (int i = 0; i < _count; i++)
            {
                int idx = (startIdx + i) % BUFFER_SIZE;
                ref ForensicsSnapshot s = ref _buffer[idx];

                sb.AppendLine(
                    $"{s.SimTime_hr:F5}," +
                    $"{s.T_rcs_F:F2},{s.Pressure_psia:F1},{s.RCPCount},{s.RCPHeat_MW:F3}," +
                    $"{s.Regime},{s.SGHeatTransfer_MW:F4},{s.SGHeatTransfer_BTUhr:F0}," +
                    $"{s.SGBulkTemp_F:F2},{s.SGTopNodeTemp_F:F2},{s.SGBottomNodeTemp_F:F2},{s.SGStratification_F:F2}," +
                    $"{s.SGSecondaryPressure_psia:F1},{s.SGSaturationTemp_F:F1},{s.SGMaxSuperheat_F:F2},{s.SGBoilingIntensity:F4}," +
                    $"{s.ThermoclineHeight_ft:F2},{s.ActiveAreaFraction:F4}," +
                    $"{s.SecondaryMass_lb:F0},{s.WideRangeLevel_pct:F2},{s.NarrowRangeLevel_pct:F2}," +
                    $"{(s.DrainingActive ? 1 : 0)},{(s.DrainingComplete ? 1 : 0)},{s.DrainingRate_gpm:F1}," +
                    $"{s.SteamRate_lbhr:F0},{s.SteamRate_MW:F3},{s.TotalSteamProduced_lb:F0}," +
                    $"{s.Node0_HTC:F1},{s.Node1_HTC:F1},{s.Node2_HTC:F1},{s.Node3_HTC:F1},{s.Node4_HTC:F1}," +
                    $"{s.Node0_AreaFrac:F4},{s.Node1_AreaFrac:F4},{s.Node2_AreaFrac:F4},{s.Node3_AreaFrac:F4},{s.Node4_AreaFrac:F4}," +
                    $"{s.Node0_Q_MW:F4},{s.Node1_Q_MW:F4},{s.Node2_Q_MW:F4},{s.Node3_Q_MW:F4},{s.Node4_Q_MW:F4}," +
                    $"{s.Node0_Blend:F4},{s.Node1_Blend:F4},{s.Node2_Blend:F4},{s.Node3_Blend:F4},{s.Node4_Blend:F4}," +
                    $"{(s.Node0_Boiling ? 1 : 0)},{(s.Node1_Boiling ? 1 : 0)},{(s.Node2_Boiling ? 1 : 0)},{(s.Node3_Boiling ? 1 : 0)},{(s.Node4_Boiling ? 1 : 0)}," +
                    $"{s.Node0_Temp_F:F2},{s.Node1_Temp_F:F2},{s.Node2_Temp_F:F2},{s.Node3_Temp_F:F2},{s.Node4_Temp_F:F2}," +
                    $"{(s.InventoryAlarm ? 1 : 0)},{s.EngineRegime},{s.CouplingAlpha:F3},{s.PZRHeaterPower_MW:F4}"
                );
            }

            // ================================================================
            // WRITE FILE
            // ================================================================
            try
            {
                File.WriteAllText(filepath, sb.ToString());
                Debug.Log($"[SGForensics] DUMP #{_dumpCount} WRITTEN: {filename} ({_count} entries, trigger={trigger})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SGForensics] Failed to write dump: {ex.Message}");
            }
        }
    }
}


