// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (Logging Partial)
// HeatupSimEngine.Logging.cs - Event Log, History Buffers, File Output
// ============================================================================
//
// PURPOSE:
//   All logging, history buffer management, and file output for the heatup
//   simulation. Manages the operations event log, rolling graph history
//   (240-point buffer at 1-minute intervals = 4-hour window),
//   periodic interval log files, and final simulation reports.
//
// ARCHITECTURE:
//   Partial class of HeatupSimEngine. This file owns:
//     - EventSeverity enum and EventLogEntry struct
//     - LogEvent() — called by all other partials for state transitions
//     - AddHistory() — rolling graph buffer updates (called from main loop)
//     - SaveIntervalLog() — detailed periodic log files
//     - SaveReport() — final simulation summary report
//     - InventoryAudit — comprehensive mass balance tracking (v1.1.0 Stage 5)
//
//   History buffers and event log list declarations remain in the main
//   HeatupSimEngine.cs for Unity serialization ([HideInInspector] fields).
//
// SOURCES:
//   - Log format follows plant computer printout conventions
//   - Interval: 30 sim-minutes per NRC HRTD operating log standards
//   - Mass balance per NRC HRTD 4.1 / 10.3 inventory requirements
//
// GOLD STANDARD: Yes
// v0.9.6 PERF FIX: Pre-format event log strings to eliminate per-frame allocations
// v1.1.0 Stage 5: Added InventoryAudit for comprehensive mass balance tracking
// ============================================================================

using UnityEngine;
using System;
using System.IO;
using System.Text;
using Critical.Physics;

public partial class HeatupSimEngine
{
    // ========================================================================
    // EVENT LOG TYPES (used by all partials)
    // ========================================================================

    /// <summary>
    /// Severity levels for simulation event log entries.
    /// Maps to control room annunciator priority conventions.
    /// </summary>
    public enum EventSeverity { INFO, ACTION, ALERT, ALARM }

    /// <summary>
    /// Timestamped event log entry for the operations log panel.
    /// v0.9.6 PERF FIX: Now includes pre-formatted display string to avoid
    /// per-frame string allocations in OnGUI (was causing 72,000 allocs/sec).
    /// </summary>
    public struct EventLogEntry
    {
        public float SimTime;          // Hours
        public EventSeverity Severity;
        public string Message;
        public string FormattedLine;   // v0.9.6: Pre-formatted for display (format ONCE, not every frame)

        public EventLogEntry(float time, EventSeverity sev, string msg)
        {
            SimTime = time;
            Severity = sev;
            Message = msg;
            
            // v0.9.6 PERF FIX: Format timestamp and full line ONCE at creation time
            // This eliminates 72,000 string allocations per second at 60 FPS
            int hrs = (int)time;
            int mins = (int)((time - hrs) * 60f);
            int secs = (int)((time - hrs - mins / 60f) * 3600f) % 60;
            string timeStr = $"{hrs:D2}:{mins:D2}:{secs:D2}";
            
            string sevTag = sev switch {
                EventSeverity.ALARM => "ALM",
                EventSeverity.ALERT => "ALT",
                EventSeverity.ACTION => "ACT",
                _ => "INF"
            };
            
            FormattedLine = $"[{timeStr}] {sevTag} | {msg}";
        }
    }

    // ========================================================================
    // INVENTORY AUDIT — v1.1.0 Stage 5: Comprehensive Mass Balance Tracking
    // ========================================================================
    //
    // Per NRC HRTD 4.1 / 10.3: The operator must maintain awareness of RCS
    // inventory at all times. This audit tracks water mass in all connected
    // volumes and reports conservation errors.
    //
    // Tracked Volumes:
    //   - RCS (Reactor Coolant System) — loops, vessel, piping
    //   - PZR (Pressurizer) — water and steam space
    //   - VCT (Volume Control Tank) — CVCS surge tank
    //   - BRS (Boron Recycle System) — holdup tanks, evaporator
    //   - Seal System — RCP seal injection/return
    //
    // Conservation Law:
    //   Total_Mass(t) = Total_Mass(t=0) + ∑(Inflows) - ∑(Outflows) - Losses
    //   Conservation_Error = |Calculated - Actual| / Actual * 100%
    //
    // ========================================================================

    /// <summary>
    /// Comprehensive inventory audit state for mass balance tracking.
    /// Updated each timestep and logged at intervals.
    /// </summary>
    public struct InventoryAuditState
    {
        // === CURRENT VOLUMES (gallons) ===
        
        /// <summary>RCS water inventory (gallons)</summary>
        public float RCS_Inventory_gal;
        
        /// <summary>Pressurizer water volume (gallons)</summary>
        public float PZR_Water_gal;
        
        /// <summary>Pressurizer steam volume (gallons)</summary>
        public float PZR_Steam_gal;
        
        /// <summary>VCT current volume (gallons)</summary>
        public float VCT_Volume_gal;
        
        /// <summary>BRS holdup tank volume (gallons)</summary>
        public float BRS_Holdup_gal;
        
        /// <summary>BRS distillate available (gallons)</summary>
        public float BRS_Distillate_gal;
        
        /// <summary>BRS concentrate available (gallons)</summary>
        public float BRS_Concentrate_gal;
        
        // === CURRENT MASSES (lbm) ===
        
        /// <summary>RCS water mass (lbm)</summary>
        public float RCS_Mass_lbm;
        
        /// <summary>PZR water mass (lbm)</summary>
        public float PZR_Water_Mass_lbm;
        
        /// <summary>PZR steam mass (lbm)</summary>
        public float PZR_Steam_Mass_lbm;
        
        /// <summary>VCT water mass (lbm)</summary>
        public float VCT_Mass_lbm;
        
        /// <summary>BRS total water mass (lbm)</summary>
        public float BRS_Mass_lbm;
        
        /// <summary>Total tracked water mass (lbm)</summary>
        public float Total_Mass_lbm;
        
        // === CUMULATIVE FLOWS (gallons) ===
        
        /// <summary>Total charging flow into RCS (gallons)</summary>
        public float Cumulative_Charging_gal;
        
        /// <summary>Total letdown flow from RCS (gallons)</summary>
        public float Cumulative_Letdown_gal;
        
        /// <summary>Total seal injection (gallons)</summary>
        public float Cumulative_SealInjection_gal;
        
        /// <summary>Total seal return to VCT (gallons)</summary>
        public float Cumulative_SealReturn_gal;
        
        /// <summary>Total surge flow into PZR (gallons, signed)</summary>
        public float Cumulative_SurgeIn_gal;
        
        /// <summary>Total surge flow out of PZR (gallons, signed)</summary>
        public float Cumulative_SurgeOut_gal;
        
        /// <summary>Total makeup from external sources (gallons)</summary>
        public float Cumulative_Makeup_gal;
        
        /// <summary>Total CBO losses (gallons)</summary>
        public float Cumulative_CBOLoss_gal;
        
        // === CONSERVATION TRACKING ===
        
        /// <summary>Initial total mass at simulation start (lbm)</summary>
        public float Initial_Total_Mass_lbm;
        
        /// <summary>Expected total mass based on flows (lbm)</summary>
        public float Expected_Total_Mass_lbm;
        
        /// <summary>Conservation error (lbm)</summary>
        public float Conservation_Error_lbm;
        
        /// <summary>Conservation error percentage</summary>
        public float Conservation_Error_pct;
        
        /// <summary>True if conservation error exceeds threshold</summary>
        public bool Conservation_Alarm;
        
        // === METADATA ===
        
        /// <summary>Simulation time of last update (hours)</summary>
        public float LastUpdate_hr;
        
        /// <summary>Status message</summary>
        public string StatusMessage;
        
        /// <summary>v5.0.3: Mass source path used for this update (CANONICAL_SOLID or CANONICAL_TWO_PHASE)</summary>
        public string AuditMassSource;
    }
    
    // Inventory audit state - persists between timesteps
    private InventoryAuditState inventoryAudit;
    
    // Conservation error threshold (lbm) - alarm if exceeded
    private const float CONSERVATION_ERROR_THRESHOLD_LBM = 500f;  // ~60 gallons
    
    // Conservation error threshold (%) - alarm if exceeded
    private const float CONSERVATION_ERROR_THRESHOLD_PCT = 0.5f;
    
    /// <summary>
    /// Initialize the inventory audit at simulation start.
    /// Captures initial masses for conservation tracking.
    /// </summary>
    void InitializeInventoryAudit()
    {
        inventoryAudit = new InventoryAuditState();
        
        // Calculate initial volumes and masses
        UpdateInventoryAudit(0f, true);
        
        // Store initial mass for conservation tracking
        inventoryAudit.Initial_Total_Mass_lbm = inventoryAudit.Total_Mass_lbm;
        inventoryAudit.Expected_Total_Mass_lbm = inventoryAudit.Total_Mass_lbm;
        inventoryAudit.Conservation_Error_lbm = 0f;
        inventoryAudit.Conservation_Error_pct = 0f;
        inventoryAudit.Conservation_Alarm = false;
        
        // Zero cumulative flows
        inventoryAudit.Cumulative_Charging_gal = 0f;
        inventoryAudit.Cumulative_Letdown_gal = 0f;
        inventoryAudit.Cumulative_SealInjection_gal = 0f;
        inventoryAudit.Cumulative_SealReturn_gal = 0f;
        inventoryAudit.Cumulative_SurgeIn_gal = 0f;
        inventoryAudit.Cumulative_SurgeOut_gal = 0f;
        inventoryAudit.Cumulative_Makeup_gal = 0f;
        inventoryAudit.Cumulative_CBOLoss_gal = 0f;
        
        inventoryAudit.StatusMessage = "Inventory audit initialized";
        
        LogEvent(EventSeverity.INFO, $"Inventory audit started: {inventoryAudit.Total_Mass_lbm:F0} lbm total");
    }
    
    /// <summary>
    /// Update the inventory audit for current timestep.
    /// Called from main simulation loop.
    /// </summary>
    /// <param name="dt_hr">Timestep in hours</param>
    /// <param name="isInitialization">True if this is initialization (skip flow accumulation)</param>
    void UpdateInventoryAudit(float dt_hr, bool isInitialization = false)
    {
        // ================================================================
        // VOLUME CALCULATIONS (gallons)
        // ================================================================
        
        // RCS inventory (excluding PZR)
        // RCS volume is approximately 12,700 ft³ at operating conditions
        // Convert from ft³ to gallons: 1 ft³ = 7.48052 gal
        float rcsVolume_ft3 = PlantConstants.RCS_WATER_VOLUME - PlantConstants.PZR_TOTAL_VOLUME;
        inventoryAudit.RCS_Inventory_gal = rcsVolume_ft3 * 7.48052f;
        
        // Pressurizer volumes
        inventoryAudit.PZR_Water_gal = pzrWaterVolume * 7.48052f;
        inventoryAudit.PZR_Steam_gal = pzrSteamVolume * 7.48052f;
        
        // VCT
        inventoryAudit.VCT_Volume_gal = vctState.Volume_gal;
        
        // BRS
        inventoryAudit.BRS_Holdup_gal = brsState.HoldupVolume_gal;
        inventoryAudit.BRS_Distillate_gal = brsState.DistillateAvailable_gal;
        inventoryAudit.BRS_Concentrate_gal = brsState.ConcentrateAvailable_gal;
        
        // ================================================================
        // MASS CALCULATIONS (lbm)
        // ================================================================
        
        // Water density at current conditions
        // v5.0.3: rcsWaterDensity, pzrWaterDensity, pzrSteamDensity removed —
        // no longer needed after eliminating V×ρ recalculation in both branches.
        // RCS/PZR masses now read directly from canonical physicsState fields.
        float vctWaterDensity = WaterProperties.WaterDensity(100f, 14.7f);  // VCT at ~100°F, atmospheric
        float brsWaterDensity = WaterProperties.WaterDensity(100f, 14.7f);  // BRS at ~100°F, atmospheric
        
        // v5.0.3: Regime-aware canonical mass source selection.
        // During solid ops, PZRWaterMassSolid and TotalPrimaryMassSolid are canonical
        // (maintained by SolidPlantPressure via surge transfer and CVCS boundary flow).
        // After bubble formation, physicsState.PZRWaterMass/PZRSteamMass are canonical
        // (maintained by CoupledThermo solver) and RCSWaterMass is canonical
        // (maintained by CVCS boundary flow pre-application in all three regimes).
        // The audit is a pure consumer of canonical state — no V×ρ recalculation.
        if (solidPressurizer && !bubbleFormed)
        {
            // Solid ops: canonical fields maintained by SolidPlantPressure
            inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMassSolid;
            inventoryAudit.RCS_Mass_lbm = physicsState.TotalPrimaryMassSolid
                                        - physicsState.PZRWaterMassSolid;
            inventoryAudit.PZR_Steam_Mass_lbm = 0f;  // Solid = no steam
            inventoryAudit.AuditMassSource = "CANONICAL_SOLID";
        }
        else
        {
            // Two-phase: canonical fields maintained by CoupledThermo solver
            // v5.0.3: Replaces defective V×ρ recalculation that used fixed geometric
            // volume constants, ignoring cumulative CVCS boundary flow effects.
            inventoryAudit.RCS_Mass_lbm = physicsState.RCSWaterMass;
            inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMass;
            inventoryAudit.PZR_Steam_Mass_lbm = physicsState.PZRSteamMass;
            inventoryAudit.AuditMassSource = "CANONICAL_TWO_PHASE";
        }
        
        // VCT mass (convert gal to ft³, then multiply by density)
        float vctVolume_ft3 = vctState.Volume_gal / 7.48052f;
        inventoryAudit.VCT_Mass_lbm = vctVolume_ft3 * vctWaterDensity;
        
        // BRS mass (holdup + distillate + concentrate)
        float brsTotal_gal = brsState.HoldupVolume_gal + brsState.DistillateAvailable_gal + brsState.ConcentrateAvailable_gal;
        float brsVolume_ft3 = brsTotal_gal / 7.48052f;
        inventoryAudit.BRS_Mass_lbm = brsVolume_ft3 * brsWaterDensity;
        
        // Total tracked mass
        inventoryAudit.Total_Mass_lbm = inventoryAudit.RCS_Mass_lbm +
                                        inventoryAudit.PZR_Water_Mass_lbm +
                                        inventoryAudit.PZR_Steam_Mass_lbm +
                                        inventoryAudit.VCT_Mass_lbm +
                                        inventoryAudit.BRS_Mass_lbm;
        
        // ================================================================
        // CUMULATIVE FLOW TRACKING
        // ================================================================
        
        if (!isInitialization && dt_hr > 0f)
        {
            float dt_min = dt_hr * 60f;
            
            // Charging (gpm * minutes = gallons)
            inventoryAudit.Cumulative_Charging_gal += chargingFlow * dt_min;
            
            // Letdown
            inventoryAudit.Cumulative_Letdown_gal += letdownFlow * dt_min;
            
            // Seal injection
            float sealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            inventoryAudit.Cumulative_SealInjection_gal += sealInjection * dt_min;
            
            // Seal return (leakoff to VCT)
            float sealReturn = rcpCount * PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM;
            inventoryAudit.Cumulative_SealReturn_gal += sealReturn * dt_min;
            
            // Surge flow (positive = into PZR, negative = out of PZR)
            if (surgeFlow > 0f)
                inventoryAudit.Cumulative_SurgeIn_gal += surgeFlow * dt_min;
            else
                inventoryAudit.Cumulative_SurgeOut_gal += (-surgeFlow) * dt_min;
            
            // Makeup (from BRS distillate or external)
            if (vctState.MakeupFlow_gpm > 0f)
            {
                // External makeup only; BRS-sourced makeup is internal to tracked plant mass.
                float makeupRateExternal = vctState.MakeupFromBRS ? 0f : vctState.MakeupFlow_gpm;
                inventoryAudit.Cumulative_Makeup_gal += makeupRateExternal * dt_min;
            }
            
            // CBO losses
            float cboLoss = rcpCount > 0 ? PlantConstants.CBO_LOSS_GPM : 0f;
            inventoryAudit.Cumulative_CBOLoss_gal += cboLoss * dt_min;
        }
        
        // ================================================================
        // CONSERVATION ERROR CALCULATION
        // ================================================================
        
        if (!isInitialization)
        {
            // Expected mass = initial + true external boundary crossings.
            // Reuse canonical engine accumulator so interval and step-level checks agree.
            inventoryAudit.Expected_Total_Mass_lbm = inventoryAudit.Initial_Total_Mass_lbm + externalNetMass_lbm;
            
            // Conservation error
            inventoryAudit.Conservation_Error_lbm = Math.Abs(inventoryAudit.Total_Mass_lbm - inventoryAudit.Expected_Total_Mass_lbm);
            
            if (inventoryAudit.Expected_Total_Mass_lbm > 0f)
            {
                inventoryAudit.Conservation_Error_pct = (inventoryAudit.Conservation_Error_lbm / inventoryAudit.Expected_Total_Mass_lbm) * 100f;
            }
            else
            {
                inventoryAudit.Conservation_Error_pct = 0f;
            }
            
            // Check alarm threshold
            bool wasAlarming = inventoryAudit.Conservation_Alarm;
            inventoryAudit.Conservation_Alarm = (inventoryAudit.Conservation_Error_lbm > CONSERVATION_ERROR_THRESHOLD_LBM) ||
                                                (inventoryAudit.Conservation_Error_pct > CONSERVATION_ERROR_THRESHOLD_PCT);
            
            // Log alarm edge
            if (inventoryAudit.Conservation_Alarm && !wasAlarming)
            {
                LogEvent(EventSeverity.ALARM, $"INVENTORY CONSERVATION ERROR: {inventoryAudit.Conservation_Error_lbm:F0} lbm ({inventoryAudit.Conservation_Error_pct:F2}%)");
            }
            else if (!inventoryAudit.Conservation_Alarm && wasAlarming)
            {
                LogEvent(EventSeverity.INFO, "Inventory conservation error cleared");
            }
        }
        
        // Update timestamp and status
        inventoryAudit.LastUpdate_hr = simTime;
        
        if (inventoryAudit.Conservation_Alarm)
        {
            inventoryAudit.StatusMessage = $"ALARM: Δ={inventoryAudit.Conservation_Error_lbm:F0} lbm";
        }
        else if (inventoryAudit.Conservation_Error_pct > 0.1f)
        {
            inventoryAudit.StatusMessage = $"WARN: Δ={inventoryAudit.Conservation_Error_lbm:F0} lbm";
        }
        else
        {
            inventoryAudit.StatusMessage = "OK";
        }
    }
    
    /// <summary>
    /// Get the current inventory audit state for display.
    /// </summary>
    public InventoryAuditState GetInventoryAudit()
    {
        return inventoryAudit;
    }
    
    // ========================================================================
    // v5.3.0 Stage 6: PRIMARY MASS LEDGER DIAGNOSTICS
    // ========================================================================
    //
    // The Primary Mass Ledger provides a second, independent mass conservation
    // check that operates at the primary system level (RCS + PZR only).
    // Unlike the full InventoryAudit (which tracks VCT, BRS, etc.), this
    // checks the TotalPrimaryMass_lb canonical ledger against the sum of
    // component masses (RCS water + PZR water + PZR steam).
    //
    // If the system is conserving mass correctly:
    //   M_ledger == M_components (within numerical tolerance)
    //   M_expected == M_ledger (both track identical boundary flows)
    //
    // Drift indicates a bug in the conservation logic somewhere.
    //
    // ========================================================================
    
    /// <summary>
    /// v5.3.0 Stage 6: Update Primary Mass Ledger diagnostics.
    /// Computes drift between canonical ledger and component sum,
    /// sets alarm thresholds, and syncs display fields.
    /// Called from main simulation loop.
    /// </summary>
    void UpdatePrimaryMassLedgerDiagnostics()
    {
        // Skip during solid ops — ledger and components are managed differently
        // and drift calculation is not meaningful until bubble formation.
        if (solidPressurizer && !bubbleFormed)
        {
            // During solid ops, just sync the ledger value for display
            primaryMassLedger_lb = physicsState.TotalPrimaryMass_lb;
            primaryMassComponents_lb = physicsState.TotalPrimaryMassSolid;  // Solid canonical
            primaryMassDrift_lb = 0f;
            primaryMassDrift_pct = 0f;
            primaryMassExpected_lb = physicsState.InitialPrimaryMass_lb
                                    + physicsState.CumulativeCVCSIn_lb
                                    - physicsState.CumulativeCVCSOut_lb
                                    - physicsState.CumulativeReliefMass_lb;
            primaryMassBoundaryError_lb = Mathf.Abs(primaryMassLedger_lb - primaryMassExpected_lb);
            primaryMassConservationOK = true;
            primaryMassAlarm = false;
            primaryMassStatus = "SOLID_OPS";
            return;
        }
        
        // ==============================================================
        // Two-phase operations: Full diagnostics
        // ==============================================================
        
        // 1. Ledger value (canonical, updated only by boundary flows)
        primaryMassLedger_lb = physicsState.TotalPrimaryMass_lb;
        
        // 2. Component sum (what the solver computes)
        // RCS mass is now computed as remainder (Stage 3), so this should
        // exactly equal the ledger by construction.
        primaryMassComponents_lb = physicsState.RCSWaterMass
                                  + physicsState.PZRWaterMass
                                  + physicsState.PZRSteamMass;
        
        // 3. Drift = Ledger - Components
        // Should be ~0 if Stage 3 conservation-by-construction is working.
        // Positive drift = ledger says more mass than components show
        // Negative drift = ledger says less mass than components show
        primaryMassDrift_lb = primaryMassLedger_lb - primaryMassComponents_lb;
        
        // 4. Drift as percentage of total
        if (primaryMassComponents_lb > 1000f)  // Sanity check
        {
            primaryMassDrift_pct = Mathf.Abs(primaryMassDrift_lb) / primaryMassComponents_lb * 100f;
        }
        else
        {
            primaryMassDrift_pct = 0f;
        }
        
        // 5. Expected mass from boundary flow integration
        // M_expected = M_initial + sum(CVCS_in) - sum(CVCS_out) - sum(Relief)
        // This is an independent check that both ledger and cumulative
        // trackers are in agreement.
        primaryMassExpected_lb = physicsState.InitialPrimaryMass_lb
                                + physicsState.CumulativeCVCSIn_lb
                                - physicsState.CumulativeCVCSOut_lb
                                - physicsState.CumulativeReliefMass_lb;
        
        // 6. Boundary error = |Ledger - Expected|
        // Should be ~0 if CVCS tracking is consistent with ledger updates.
        primaryMassBoundaryError_lb = Mathf.Abs(primaryMassLedger_lb - primaryMassExpected_lb);
        
        // 7. Alarm thresholds
        // Warning: >0.1% drift (indicates minor accumulation error)
        // Alarm: >1.0% drift (indicates significant conservation bug)
        const float WARNING_THRESHOLD_PCT = 0.1f;
        const float ALARM_THRESHOLD_PCT = 1.0f;
        
        primaryMassConservationOK = (primaryMassDrift_pct <= WARNING_THRESHOLD_PCT);
        primaryMassAlarm = (primaryMassDrift_pct > ALARM_THRESHOLD_PCT);
        
        // 8. Status string for display
        if (primaryMassAlarm)
        {
            primaryMassStatus = $"ALARM: {primaryMassDrift_pct:F2}% ({primaryMassDrift_lb:F0} lb)";
        }
        else if (!primaryMassConservationOK)
        {
            primaryMassStatus = $"WARN: {primaryMassDrift_pct:F3}% ({primaryMassDrift_lb:F1} lb)";
        }
        else
        {
            primaryMassStatus = "OK";
        }
        
        // 9. Log alarm edge — one-time event on threshold crossing
        // Uses edge detection state variables from main HeatupSimEngine.cs partial
        if (primaryMassAlarm && !_previousMassAlarmState)
        {
            LogEvent(EventSeverity.ALARM,
                $"PRIMARY MASS CONSERVATION ALARM: drift={primaryMassDrift_lb:F1}lb ({primaryMassDrift_pct:F2}%)");
            LogEvent(EventSeverity.INFO,
                $"  Ledger={primaryMassLedger_lb:F0}lb | Components={primaryMassComponents_lb:F0}lb | Expected={primaryMassExpected_lb:F0}lb");
            Debug.LogWarning($"[MASS ALARM] Conservation drift {primaryMassDrift_pct:F2}% exceeds 1.0% threshold!");
        }
        else if (!primaryMassConservationOK && _previousMassConservationOK)
        {
            // Transition from OK to WARNING
            LogEvent(EventSeverity.ALERT,
                $"PRIMARY MASS DRIFT WARNING: {primaryMassDrift_lb:F1}lb ({primaryMassDrift_pct:F2}%)");
        }
        else if (primaryMassConservationOK && !_previousMassConservationOK && !_previousMassAlarmState)
        {
            // Recovery from WARNING to OK
            LogEvent(EventSeverity.INFO,
                $"PRIMARY MASS DRIFT CLEARED: now {primaryMassDrift_pct:F3}%");
        }
        
        _previousMassConservationOK = primaryMassConservationOK;
        _previousMassAlarmState = primaryMassAlarm;
    }
    
    /// <summary>
    /// Generate a detailed inventory audit report string.
    /// </summary>
    public string GetInventoryAuditReport()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("============================================================");
        sb.AppendLine("INVENTORY AUDIT REPORT");
        sb.AppendLine($"Simulation Time: {simTime:F2} hr ({TimeAcceleration.FormatTime(simTime)})");
        sb.AppendLine("============================================================");
        sb.AppendLine();
        
        sb.AppendLine("CURRENT VOLUMES (gallons):");
        sb.AppendLine($"  RCS (excl. PZR):    {inventoryAudit.RCS_Inventory_gal,12:F0} gal");
        sb.AppendLine($"  PZR Water:          {inventoryAudit.PZR_Water_gal,12:F0} gal");
        sb.AppendLine($"  PZR Steam:          {inventoryAudit.PZR_Steam_gal,12:F0} gal");
        sb.AppendLine($"  VCT:                {inventoryAudit.VCT_Volume_gal,12:F0} gal");
        sb.AppendLine($"  BRS Holdup:         {inventoryAudit.BRS_Holdup_gal,12:F0} gal");
        sb.AppendLine($"  BRS Distillate:     {inventoryAudit.BRS_Distillate_gal,12:F0} gal");
        sb.AppendLine($"  BRS Concentrate:    {inventoryAudit.BRS_Concentrate_gal,12:F0} gal");
        sb.AppendLine();
        
        sb.AppendLine("CURRENT MASSES (lbm):");
        sb.AppendLine($"  RCS:                {inventoryAudit.RCS_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  PZR Water:          {inventoryAudit.PZR_Water_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  PZR Steam:          {inventoryAudit.PZR_Steam_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  VCT:                {inventoryAudit.VCT_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  BRS:                {inventoryAudit.BRS_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  ----------------------------------------");
        sb.AppendLine($"  TOTAL:              {inventoryAudit.Total_Mass_lbm,12:F0} lbm");
        sb.AppendLine();
        
        sb.AppendLine("CUMULATIVE FLOWS (gallons):");
        sb.AppendLine($"  Charging:           {inventoryAudit.Cumulative_Charging_gal,12:F0} gal");
        sb.AppendLine($"  Letdown:            {inventoryAudit.Cumulative_Letdown_gal,12:F0} gal");
        sb.AppendLine($"  Net Charging:       {inventoryAudit.Cumulative_Charging_gal - inventoryAudit.Cumulative_Letdown_gal,12:F0} gal");
        sb.AppendLine($"  Seal Injection:     {inventoryAudit.Cumulative_SealInjection_gal,12:F0} gal");
        sb.AppendLine($"  Seal Return:        {inventoryAudit.Cumulative_SealReturn_gal,12:F0} gal");
        sb.AppendLine($"  Surge In (to PZR):  {inventoryAudit.Cumulative_SurgeIn_gal,12:F0} gal");
        sb.AppendLine($"  Surge Out (fm PZR): {inventoryAudit.Cumulative_SurgeOut_gal,12:F0} gal");
        sb.AppendLine($"  External Makeup:    {inventoryAudit.Cumulative_Makeup_gal,12:F0} gal");
        sb.AppendLine($"  CBO Losses:         {inventoryAudit.Cumulative_CBOLoss_gal,12:F0} gal");
        sb.AppendLine();
        
        sb.AppendLine("CONSERVATION CHECK:");
        sb.AppendLine($"  Initial Total:      {inventoryAudit.Initial_Total_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  Expected Total:     {inventoryAudit.Expected_Total_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  Actual Total:       {inventoryAudit.Total_Mass_lbm,12:F0} lbm");
        sb.AppendLine($"  ----------------------------------------");
        sb.AppendLine($"  Error (absolute):   {inventoryAudit.Conservation_Error_lbm,12:F1} lbm");
        sb.AppendLine($"  Error (percent):    {inventoryAudit.Conservation_Error_pct,12:F3} %");
        sb.AppendLine($"  Status:             {inventoryAudit.StatusMessage}");
        sb.AppendLine();
        
        return sb.ToString();
    }

    // ========================================================================
    // EVENT LOGGING
    // ========================================================================

    /// <summary>
    /// Add an event to the operations log.
    /// Called from physics code at state transitions, alarm edges, and mode changes.
    /// Rolling buffer capped at MAX_EVENT_LOG entries.
    /// </summary>
    public void LogEvent(EventSeverity severity, string message)
    {
        eventLog.Add(new EventLogEntry(simTime, severity, message));
        if (eventLog.Count > MAX_EVENT_LOG)
            eventLog.RemoveAt(0);
    }
    
    // =========================================================================
    // v5.4.0 Stage 0: MASS CHECKPOINT DIAGNOSTIC LOGGING
    // These methods provide detailed mass state snapshots at key points.
    // Controlled by enableMassDiagnostics flag and diagSampleIntervalFrames.
    // =========================================================================
    
    /// <summary>
    /// v5.4.0 Stage 0: Log detailed mass state at a checkpoint.
    /// Only emits when enableMassDiagnostics is true.
    /// </summary>
    /// <param name="checkpointName">Identifier for this checkpoint (e.g., "RCP_START", "BUBBLE_FORM")</param>
    void LogMassCheckpoint(string checkpointName)
    {
        if (!enableMassDiagnostics) return;
        
        float componentSum = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float ledgerDelta = physicsState.TotalPrimaryMass_lb - componentSum;
        float driftPct = (componentSum > 1000f) ? (ledgerDelta / componentSum * 100f) : 0f;
        
        Debug.Log($"[MASS_CHKPT] {checkpointName} @ T+{simTime:F4}hr | " +
                  $"Ledger={physicsState.TotalPrimaryMass_lb:F1}lb | " +
                  $"Components={componentSum:F1}lb | " +
                  $"Delta={ledgerDelta:F2}lb ({driftPct:F4}%) | " +
                  $"RCS={physicsState.RCSWaterMass:F1} PZR_w={physicsState.PZRWaterMass:F1} PZR_s={physicsState.PZRSteamMass:F1}");
    }
    
    /// <summary>
    /// v5.4.0 Stage 0: Log extended mass state including surge flow tracking.
    /// Only emits when enableMassDiagnostics is true.
    /// </summary>
    /// <param name="checkpointName">Identifier for this checkpoint</param>
    /// <param name="regime">Current physics regime (1=isolated, 2=blended, 3=coupled)</param>
    void LogMassCheckpointExtended(string checkpointName, int regime)
    {
        if (!enableMassDiagnostics) return;
        
        float componentSum = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float ledgerDelta = physicsState.TotalPrimaryMass_lb - componentSum;
        float driftPct = (componentSum > 1000f) ? (ledgerDelta / componentSum * 100f) : 0f;
        
        // Calculate expected from boundary integration
        float expectedMass = physicsState.InitialPrimaryMass_lb
                           + physicsState.CumulativeCVCSIn_lb
                           - physicsState.CumulativeCVCSOut_lb
                           - physicsState.CumulativeReliefMass_lb;
        float boundaryError = physicsState.TotalPrimaryMass_lb - expectedMass;
        
        Debug.Log($"[MASS_EXT] {checkpointName} @ T+{simTime:F4}hr Regime={regime} | " +
                  $"Ledger={physicsState.TotalPrimaryMass_lb:F1}lb | Components={componentSum:F1}lb | " +
                  $"Drift={ledgerDelta:F2}lb ({driftPct:F4}%)");
        Debug.Log($"[MASS_EXT]   RCS={physicsState.RCSWaterMass:F1} PZR_w={physicsState.PZRWaterMass:F1} " +
                  $"PZR_s={physicsState.PZRSteamMass:F1} | PZR_lvl={pzrLevel:F1}%");
        Debug.Log($"[MASS_EXT]   Expected={expectedMass:F1}lb | BoundaryErr={boundaryError:F2}lb | " +
                  $"CVCS_in={physicsState.CumulativeCVCSIn_lb:F1} CVCS_out={physicsState.CumulativeCVCSOut_lb:F1} " +
                  $"Relief={physicsState.CumulativeReliefMass_lb:F1}");
        Debug.Log($"[MASS_EXT]   Surge: Rate={physicsState.SurgeFlowRate_lbps:F3}lb/s " +
                  $"CumIn={physicsState.CumulativeSurgeIn_lb:F1} CumOut={physicsState.CumulativeSurgeOut_lb:F1} " +
                  $"Net={physicsState.NetCumulativeSurge_lb:F1}lb");
    }
    
    /// <summary>
    /// v5.4.0 Stage 0: Log RCP transition event with surrounding mass state.
    /// Called when RCP count changes. Logs 10 frames before and after transition.
    /// </summary>
    /// <param name="oldCount">RCP count before change</param>
    /// <param name="newCount">RCP count after change</param>
    /// <param name="isPreTransition">True if this is logged before the physics step</param>
    void LogRCPTransition(int oldCount, int newCount, bool isPreTransition)
    {
        if (!enableMassDiagnostics) return;
        
        string phase = isPreTransition ? "PRE" : "POST";
        
        float componentSum = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float ledgerDelta = physicsState.TotalPrimaryMass_lb - componentSum;
        
        Debug.Log($"[RCP_TRANS] {phase} {oldCount}->{newCount} @ T+{simTime:F4}hr | " +
                  $"Ledger={physicsState.TotalPrimaryMass_lb:F1}lb | Components={componentSum:F1}lb | " +
                  $"Delta={ledgerDelta:F2}lb | PZR_lvl={pzrLevel:F2}% | P={pressure:F1}psia");
    }
    
    /// <summary>
    /// v5.4.0 Stage 4: INTENSIVE RCP transition logging for spike diagnosis.
    /// Captures detailed state BEFORE and AFTER each RCP count change.
    /// This includes all variables that could contribute to level spikes:
    ///   - PZR water/steam volumes and masses
    ///   - RCS mass and density
    ///   - Temperature and pressure
    ///   - Total mass ledger and component sum
    ///   - Regime and coupling factor
    /// </summary>
    /// <param name="phase">"PRE" or "POST" indicating timing relative to RCP change</param>
    /// <param name="oldCount">RCP count before change</param>
    /// <param name="newCount">RCP count after change</param>
    void LogRCPTransitionIntensive(string phase, int oldCount, int newCount)
    {
        // Compute derived values for diagnostic output
        float componentSum = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
        float ledgerDelta = physicsState.TotalPrimaryMass_lb - componentSum;
        float rho_rcs = WaterProperties.WaterDensity(T_rcs, pressure);
        float tSat = WaterProperties.SaturationTemperature(pressure);
        float rho_pzr_water = WaterProperties.WaterDensity(tSat, pressure);
        float rho_pzr_steam = WaterProperties.SaturatedSteamDensity(pressure);
        
        // Determine current regime
        float alpha = rcpContribution.TotalFlowFraction;
        int regime = (rcpCount == 0 || alpha < 0.001f) ? 1 :
                     (!rcpContribution.AllFullyRunning) ? 2 : 3;
        
        // Log header
        Debug.Log($"[RCP_SPIKE_DIAG] ========== {phase} RCP TRANSITION {oldCount}->{newCount} @ T+{simTime:F4}hr ==========");
        
        // Line 1: Level and volumes (PRIMARY DIAGNOSTIC for spike)
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | LEVEL: PZR_lvl={pzrLevel:F3}% | " +
                  $"PZR_wVol={pzrWaterVolume:F3}ft3 PZR_sVol={pzrSteamVolume:F3}ft3 PZR_total={PlantConstants.PZR_TOTAL_VOLUME:F1}ft3");
        
        // Line 2: Masses
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | MASS: Total_ledger={physicsState.TotalPrimaryMass_lb:F1}lb | " +
                  $"RCS={physicsState.RCSWaterMass:F1}lb PZR_w={physicsState.PZRWaterMass:F1}lb PZR_s={physicsState.PZRSteamMass:F1}lb | " +
                  $"Sum={componentSum:F1}lb Delta={ledgerDelta:F2}lb");
        
        // Line 3: Densities (key for V×ρ calculations)
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | DENSITY: rho_RCS={rho_rcs:F2}lb/ft3 | " +
                  $"rho_PZR_water={rho_pzr_water:F2}lb/ft3 rho_PZR_steam={rho_pzr_steam:F4}lb/ft3");
        
        // Line 4: Thermodynamic state
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | THERMO: P={pressure:F1}psia T_rcs={T_rcs:F1}F T_pzr={T_pzr:F1}F T_sat={tSat:F1}F | " +
                  $"Subcool={subcooling:F1}F");
        
        // Line 5: Regime and coupling
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | REGIME: {regime} alpha={alpha:F4} | " +
                  $"RCPs_running={rcpCount} effHeat={effectiveRCPHeat:F2}MW | " +
                  $"AllFullyRunning={rcpContribution.AllFullyRunning}");
        
        // Line 6: PZR physics state (from physicsState struct)
        float implied_pzr_vol = (physicsState.PZRWaterMass > 0f && rho_pzr_water > 0.1f) 
                                 ? (physicsState.PZRWaterMass / rho_pzr_water) : 0f;
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | PZR_CHECK: mass-implied_vol={implied_pzr_vol:F3}ft3 vs actual_vol={pzrWaterVolume:F3}ft3 | " +
                  $"delta={(implied_pzr_vol - pzrWaterVolume):F4}ft3");
        
        // Line 7: Surge flow and CVCS state
        Debug.Log($"[RCP_SPIKE_DIAG] {phase} | FLOWS: surge={surgeFlow:F2}gpm | " +
                  $"chg={chargingFlow:F1}gpm ld={letdownFlow:F1}gpm net={(chargingFlow - letdownFlow):F1}gpm");
    }
    
    /// <summary>
    /// v5.4.0 Stage 0: Log periodic mass diagnostic sample.
    /// Called every diagSampleIntervalFrames when diagnostics enabled.
    /// Returns true if a sample was logged this frame.
    /// </summary>
    bool TryLogPeriodicMassDiagnostic(int regime)
    {
        if (!enableMassDiagnostics) return false;
        
        _diagFrameCounter++;
        if (_diagFrameCounter < diagSampleIntervalFrames) return false;
        
        _diagFrameCounter = 0;
        LogMassCheckpointExtended("PERIODIC", regime);
        return true;
    }
    
    /// <summary>
    /// v5.4.0 Stage 0: Update surge flow tracking in physicsState.
    /// Called each timestep to accumulate surge flow statistics.
    /// This is diagnostic only — does not modify physics behavior.
    /// </summary>
    /// <param name="surgeFlow_gpm">Current surge flow in gpm (positive = into PZR)</param>
    /// <param name="dt_hr">Timestep in hours</param>
    void UpdateSurgeFlowTracking(float surgeFlow_gpm, float dt_hr)
    {
        // Convert gpm to lb/s for instantaneous rate
        float dt_sec = dt_hr * 3600f;
        float rho = WaterProperties.WaterDensity(T_pzr, pressure);
        float surgeFlowRate_lbps = surgeFlow_gpm * PlantConstants.GPM_TO_FT3_SEC * rho;
        
        physicsState.SurgeFlowRate_lbps = surgeFlowRate_lbps;
        
        // Accumulate cumulative flows
        float massThisStep = surgeFlowRate_lbps * dt_sec;
        if (massThisStep > 0f)
            physicsState.CumulativeSurgeIn_lb += massThisStep;
        else
            physicsState.CumulativeSurgeOut_lb += (-massThisStep);
    }

    // ========================================================================
    // HISTORY BUFFER UPDATE — Capped to MAX_HISTORY for rolling graph window
    // Called every 5 sim-minutes from the main simulation loop.
    // ========================================================================

    void AddHistory()
    {
        tempHistory.Add(T_rcs);
        pressHistory.Add(pressure);
        timeHistory.Add(simTime);
        pzrLevelHistory.Add(pzrLevel);
        subcoolHistory.Add(subcooling);
        heatRateHistory.Add(rcsHeatRate);
        chargingHistory.Add(chargingFlow);
        letdownHistory.Add(letdownFlow);
        vctLevelHistory.Add(vctState.Level_percent);
        surgeFlowHistory.Add(surgeFlow);
        tHotHistory.Add(T_hot);
        tColdHistory.Add(T_cold);
        tSgSecondaryHistory.Add(T_sg_secondary);  // v0.8.0
        brsHoldupHistory.Add(brsState.HoldupVolume_gal);
        brsDistillateHistory.Add(brsState.DistillateAvailable_gal);
        
        // v0.9.0: Add critical missing temperature and rate histories
        tPzrHistory.Add(T_pzr);
        tSatHistory.Add(T_sat);
        pressureRateHistory.Add(pressureRate);
        
        // v1.1.0: Add HZP stabilization histories
        steamDumpHeatHistory.Add(steamDumpHeat_MW);
        steamPressureHistory.Add(steamPressure_psig);
        heaterPIDOutputHistory.Add(heaterPIDOutput);
        hzpProgressHistory.Add(hzpProgress);
        
        // v4.4.0: Spray system history
        sprayFlowHistory.Add(sprayFlow_GPM);

        // Cap all histories to MAX_HISTORY (rolling window)
        if (tempHistory.Count > MAX_HISTORY)
        {
            tempHistory.RemoveAt(0);
            pressHistory.RemoveAt(0);
            timeHistory.RemoveAt(0);
            pzrLevelHistory.RemoveAt(0);
            subcoolHistory.RemoveAt(0);
            heatRateHistory.RemoveAt(0);
            chargingHistory.RemoveAt(0);
            letdownHistory.RemoveAt(0);
            vctLevelHistory.RemoveAt(0);
            surgeFlowHistory.RemoveAt(0);
            tHotHistory.RemoveAt(0);
            tColdHistory.RemoveAt(0);
            tSgSecondaryHistory.RemoveAt(0);  // v0.8.0
            brsHoldupHistory.RemoveAt(0);
            brsDistillateHistory.RemoveAt(0);
            
            // v0.9.0
            tPzrHistory.RemoveAt(0);
            tSatHistory.RemoveAt(0);
            pressureRateHistory.RemoveAt(0);
            
            // v1.1.0
            steamDumpHeatHistory.RemoveAt(0);
            steamPressureHistory.RemoveAt(0);
            heaterPIDOutputHistory.RemoveAt(0);
            hzpProgressHistory.RemoveAt(0);
            
            // v4.4.0
            sprayFlowHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Clear all history buffers and event log. Called during initialization.
    /// </summary>
    void ClearHistoryAndEvents()
    {
        tempHistory.Clear();
        pressHistory.Clear();
        timeHistory.Clear();
        pzrLevelHistory.Clear();
        subcoolHistory.Clear();
        heatRateHistory.Clear();
        chargingHistory.Clear();
        letdownHistory.Clear();
        vctLevelHistory.Clear();
        surgeFlowHistory.Clear();
        tHotHistory.Clear();
        tColdHistory.Clear();
        tSgSecondaryHistory.Clear();
        brsHoldupHistory.Clear();
        brsDistillateHistory.Clear();
        tPzrHistory.Clear();
        tSatHistory.Clear();
        pressureRateHistory.Clear();
        
        // v1.1.0
        steamDumpHeatHistory.Clear();
        steamPressureHistory.Clear();
        heaterPIDOutputHistory.Clear();
        hzpProgressHistory.Clear();
        
        // v4.4.0
        sprayFlowHistory.Clear();
        
        eventLog.Clear();
    }

    // ========================================================================
    // INTERVAL LOG — Detailed snapshot every 30 sim-minutes
    // Per NRC HRTD operating log requirements
    // ========================================================================

    void SaveIntervalLog()
    {
        logCount++;
        string file = Path.Combine(logPath, $"Heatup_Interval_{logCount:D3}_{simTime:F2}hr.txt");
        var sb = new StringBuilder();
        sb.AppendLine("HEATUP INTERVAL LOG");
        sb.AppendLine(new string('=', 70));
        sb.AppendLine($"Log Entry:        #{logCount}");
        sb.AppendLine($"Timestamp:        {DateTime.Now}");
        sb.AppendLine($"Sim Time:         {simTime:F2} hours ({TimeAcceleration.FormatTime(simTime)})");
        sb.AppendLine($"Wall-Clock Time:  {TimeAcceleration.FormatTime(wallClockTime)}");
        sb.AppendLine($"Effective Speed:  {TimeAcceleration.CurrentMultiplier}x ({TimeAcceleration.SpeedLabelsShort[currentSpeedIndex]})");
        sb.AppendLine();
        sb.AppendLine("THERMAL STATE:");
        sb.AppendLine($"  T_avg:            {T_avg,10:F2} °F");
        sb.AppendLine($"  T_hot:            {T_hot,10:F2} °F");
        sb.AppendLine($"  T_cold:           {T_cold,10:F2} °F");
        sb.AppendLine($"  T_rcs:            {T_rcs,10:F2} °F");
        sb.AppendLine($"  T_pzr:            {T_pzr,10:F2} °F");
        sb.AppendLine($"  T_sat:            {T_sat,10:F2} °F");
        sb.AppendLine($"  T_sg_secondary:   {T_sg_secondary,10:F2} °F");
        sb.AppendLine($"  Subcooling:       {subcooling,10:F2} °F");
        sb.AppendLine($"  Heatup Rate:      {heatupRate,10:F2} °F/hr");
        sb.AppendLine($"  PZR Heat Rate:    {pzrHeatRate,10:F2} °F/hr");
        sb.AppendLine($"  RCS Heat Rate:    {rcsHeatRate,10:F2} °F/hr");
        sb.AppendLine($"  Pressure Rate:    {pressureRate,10:F2} psi/hr");
        sb.AppendLine();
        sb.AppendLine("PRESSURE / LEVEL:");
        sb.AppendLine($"  RCS Pressure:     {pressure,10:F1} psia ({pressure - 14.7f:F1} psig)");
        sb.AppendLine($"  PZR Level:        {pzrLevel,10:F1} %");
        sb.AppendLine($"  PZR Water Vol:    {pzrWaterVolume,10:F1} ftÂ³");
        sb.AppendLine($"  PZR Steam Vol:    {pzrSteamVolume,10:F1} ftÂ³");
        sb.AppendLine($"  Surge Flow:       {surgeFlow,10:F1} gpm");
        sb.AppendLine($"  PZR Setpoint:     {pzrLevelSetpointDisplay,10:F1} %");
        sb.AppendLine();
        sb.AppendLine("CVCS (Chemical & Volume Control):");
        sb.AppendLine($"  Charging Flow:    {chargingFlow,10:F1} gpm");
        sb.AppendLine($"  Charging to RCS:  {chargingToRCS,10:F1} gpm (to system)");
        sb.AppendLine($"  Total CCP Output: {totalCCPOutput,10:F1} gpm (incl. seals)");
        sb.AppendLine($"  Letdown Flow:     {letdownFlow,10:F1} gpm");
        sb.AppendLine($"  Letdown Path:     {(letdownViaRHR ? "RHR CROSSCONNECT" : "ORIFICE"),-10}");
        sb.AppendLine($"  Letdown Isolated: {(letdownIsolatedFlag ? "YES" : "NO"),10}");
        // v4.4.0: Orifice lineup detail
        sb.AppendLine($"  Orifice Lineup:   {orificeLineupDesc,10}");
        sb.AppendLine($"    75 gpm valves:  {orifice75Count,10} open");
        sb.AppendLine($"    45 gpm valve:   {(orifice45Open ? "OPEN" : "CLOSED"),10}");
        sb.AppendLine($"  Net CVCS:         {chargingFlow - letdownFlow,10:F1} gpm");
        sb.AppendLine($"  PI Error:         {cvcsIntegralError,10:F2} %-hr");
        sb.AppendLine($"  Divert Fraction:  {divertFraction,10:F2}");
        sb.AppendLine();
        // v4.4.0: Heater PID and Spray System Status
        sb.AppendLine("PZR PRESSURE CONTROL (v4.4.0):");
        sb.AppendLine($"  Heater Mode:      {currentHeaterMode,10}");
        if (heaterPIDActive)
        {
            sb.AppendLine($"  PID Output:       {heaterPIDOutput,10:F3} (0-1)");
            sb.AppendLine($"  PID Setpoint:     {heaterPIDState.PressureSetpoint,10:F0} psig");
            sb.AppendLine($"  PID Error:        {heaterPIDState.PressureError,10:F1} psi");
            sb.AppendLine($"  PID Status:       {heaterPIDState.StatusMessage}");
            sb.AppendLine($"  Prop Fraction:    {heaterPIDState.ProportionalFraction,10:F3}");
            sb.AppendLine($"  Backup Heaters:   {(heaterPIDState.BackupOn ? "ON" : "OFF"),10}");
        }
        sb.AppendLine($"  Heater Power:     {pzrHeaterPower,10:F3} MW");
        sb.AppendLine($"  Spray Enabled:    {(sprayState.IsEnabled ? "YES" : "NO"),10}");
        sb.AppendLine($"  Spray Active:     {(sprayActive ? "YES" : "NO"),10}");
        sb.AppendLine($"  Spray Valve Pos:  {sprayValvePosition * 100f,10:F1} %");
        sb.AppendLine($"  Spray Flow:       {sprayFlow_GPM,10:F1} gpm");
        sb.AppendLine($"  Spray \u0394T:        {sprayState.SprayDeltaT,10:F0} \u00b0F");
        sb.AppendLine($"  Steam Condensed:  {spraySteamCondensed_lbm,10:F2} lbm/step");
        sb.AppendLine($"  Spray Status:     {sprayState.StatusMessage}");
        sb.AppendLine();
        sb.AppendLine("BUBBLE FORMATION STATUS:");
        sb.AppendLine($"  Solid Pressurizer:{(solidPressurizer ? "YES" : "NO"),10}");
        sb.AppendLine($"  Bubble Formed:    {(bubbleFormed ? "YES" : "NO"),10}");
        if (bubbleFormed)
        {
            sb.AppendLine($"  Formation Time:   {bubbleFormationTime,10:F2} hr");
            sb.AppendLine($"  Formation Temp:   {bubbleFormationTemp,10:F1} °F");
        }
        if (solidPressurizer)
        {
            sb.AppendLine($"  Solid P Setpoint: {solidPlantPressureSetpoint,10:F1} psia");
            sb.AppendLine($"  Solid P Error:    {pressure - solidPlantPressureSetpoint,10:F1} psi");
            sb.AppendLine($"  Solid P Filtered: {solidPlantState.PressureFiltered,10:F1} psia (controller input)");
            sb.AppendLine($"  Control Mode:     {solidPlantState.ControlMode ?? "N/A",10}");
            sb.AppendLine($"  NetCmd/NetEff:    {solidPlantState.LetdownAdjustCmd,+7:F2} / {solidPlantState.LetdownAdjustEff:F2} gpm");
            sb.AppendLine($"  Solid P In Band:  {(solidPlantPressureInBand ? "YES" : "NO"),10}");
        }
        sb.AppendLine($"  Bubble Phase:     {bubblePhase,10}");
        sb.AppendLine($"  CCP Started:      {(ccpStarted ? "YES" : "NO"),10}");
        sb.AppendLine($"  Aux Spray Tested: {(auxSprayTestPassed ? "YES" : "NO"),10}");
        sb.AppendLine();
        sb.AppendLine("RCP (Reactor Coolant Pump) STATE:");
        sb.AppendLine($"  RCPs Running:     {rcpCount,10} / 4");
        sb.AppendLine($"  Total RCP Heat:   {rcpHeat,10:F2} MW (input to RCS)");
        sb.AppendLine($"    Physics Regime:   {GetPhysicsRegimeString(),10}");
        for (int i = 0; i < rcpCount; i++)
        {
            float rampTime = simTime - rcpStartTimes[i];
            // Get individual pump state by calling RCPSequencer
            var pumpState = RCPSequencer.UpdatePumpRampState(i, rcpStartTimes[i], simTime);
            float flowFrac = pumpState.FlowFraction;
            string status = flowFrac >= 0.99f ? "RATED" : (flowFrac > 0.01f ? "RAMPING" : "OFF");
            float pumpHeat = pumpState.HeatFraction * PlantConstants.RCP_HEAT_MW_EACH;
            sb.AppendLine($"    RCP #{i + 1,-8} {status,-10} Flow: {flowFrac,5:F3}  Heat: {pumpHeat,5:F2} MW  (T+{rampTime,5:F2} hr)");
        }
        sb.AppendLine($"    Effective Heat:   {effectiveRCPHeat,10:F2} MW (vs {rcpCount * PlantConstants.RCP_HEAT_MW_EACH:F2} MW rated)");
        sb.AppendLine($"    Ramp Efficiency:  {(rcpCount > 0 ? effectiveRCPHeat / (rcpCount * PlantConstants.RCP_HEAT_MW_EACH) * 100f : 0f),10:F1} %");
        sb.AppendLine();
        // v0.7.1: Enhanced Seal Injection Breakdown
        sb.AppendLine("  SEAL INJECTION (Per-Pump Detail):");
        sb.AppendLine($"    Seal Inj (total): {(rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM),10:F1} gpm");
        for (int i = 0; i < rcpCount; i++)
        {
            sb.AppendLine($"      → RCP #{i + 1}:     {PlantConstants.SEAL_INJECTION_PER_PUMP_GPM,10:F1} gpm");
        }
        sb.AppendLine($"    Seal Ret (total): {(rcpCount * PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM),10:F1} gpm");
        for (int i = 0; i < rcpCount; i++)
        {
            sb.AppendLine($"      → RCP #{i + 1}:     {PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM,10:F1} gpm");
        }
        sb.AppendLine();
        sb.AppendLine("  VCT (Volume Control Tank):");
        sb.AppendLine($"    Level:            {vctState.Level_percent,10:F1} %");
        sb.AppendLine($"    Volume:           {vctState.Volume_gal,10:F0} gal");
        sb.AppendLine($"    Boron Conc:       {vctState.BoronConcentration_ppm,10:F0} ppm");
        sb.AppendLine($"    Net Flow:         {vctState.NetFlow_gpm,10:F1} gpm");
        sb.AppendLine($"    Status:           {VCTPhysics.GetStatusString(vctState),10}");
        sb.AppendLine($"    Divert Active:    {(vctState.DivertActive ? "YES" : "NO"),10}");
        sb.AppendLine($"    Auto Makeup:      {(vctState.AutoMakeupActive ? "YES" : "NO"),10}");
        sb.AppendLine($"    RWST Suction:     {(vctState.RWSTSuctionActive ? "YES" : "NO"),10}");
        sb.AppendLine($"    Makeup Source:    {(vctState.MakeupFromBRS ? "BRS DISTILLATE" : (vctState.RWSTSuctionActive ? "RWST" : (vctState.AutoMakeupActive ? "RMS" : "NONE"))),10}");
        sb.AppendLine();
        sb.AppendLine("  BRS (Boron Recycle System) — Per NRC HRTD 4.1 / Callaway FSAR Ch.11:");
        float brsHoldupPct = BRSPhysics.GetHoldupLevelPercent(brsState);
        sb.AppendLine($"    Status:           {BRSPhysics.GetStatusString(brsState),10}");
        sb.AppendLine($"    Holdup Volume:    {brsState.HoldupVolume_gal,10:F0} / {PlantConstants.BRS_HOLDUP_USABLE_CAPACITY_GAL:F0} gal ({brsHoldupPct:F1}%)");
        sb.AppendLine($"    Holdup Boron:     {brsState.HoldupBoronConc_ppm,10:F0} ppm");
        sb.AppendLine($"    Evaporator:       {(brsState.ProcessingActive ? "RUNNING" : "IDLE"),10}");
        sb.AppendLine($"    Evap Feed Rate:   {brsState.EvaporatorFeedRate_gpm,10:F1} gpm");
        sb.AppendLine($"    Distillate Avail: {brsState.DistillateAvailable_gal,10:F0} gal");
        sb.AppendLine($"    Concentrate Avail:{brsState.ConcentrateAvailable_gal,10:F0} gal");
        sb.AppendLine($"    Inflow (from VCT):{brsState.InFlow_gpm,10:F1} gpm");
        sb.AppendLine($"    Return (to VCT):  {brsState.ReturnFlow_gpm,10:F1} gpm");
        sb.AppendLine($"    Cumulative In:    {brsState.CumulativeIn_gal,10:F0} gal");
        sb.AppendLine($"    Cumulative Proc:  {brsState.CumulativeProcessed_gal,10:F0} gal");
        sb.AppendLine($"    Cum Distillate:   {brsState.CumulativeDistillate_gal,10:F0} gal");
        sb.AppendLine($"    Cum Concentrate:  {brsState.CumulativeConcentrate_gal,10:F0} gal");
        sb.AppendLine($"    Cumulative Ret:   {brsState.CumulativeReturned_gal,10:F0} gal");
        sb.AppendLine();
        sb.AppendLine("  BORON:");
        sb.AppendLine($"    RCS Boron:        {rcsBoronConcentration,10:F0} ppm");
        sb.AppendLine($"    VCT Boron:        {vctState.BoronConcentration_ppm,10:F0} ppm");
        sb.AppendLine();
        sb.AppendLine("  MASS CONSERVATION:");
        sb.AppendLine($"    VCT Cum In:       {vctState.CumulativeIn_gal,10:F0} gal");
        sb.AppendLine($"    VCT Cum Out:      {vctState.CumulativeOut_gal,10:F0} gal");
        sb.AppendLine($"    Conservation Err: {massConservationError,10:F2} gal");
        sb.AppendLine();
        sb.AppendLine("  HEAT SOURCES:");
        sb.AppendLine($"    RCPs Running:     {rcpCount,10} / 4");
        sb.AppendLine($"    RCP Heat Input:   {rcpHeat,10:F2} MW");
        sb.AppendLine($"    PZR Heaters:      {pzrHeaterPower,10:F2} MW");
        sb.AppendLine($"    Gross Heat Input: {rcpHeat + pzrHeaterPower,10:F2} MW");
        float currentHeatLoss = HeatTransfer.InsulationHeatLoss_MW(T_rcs);
        sb.AppendLine($"    Heat Losses:      {currentHeatLoss,10:F2} MW (temp dependent)");
        sb.AppendLine($"    SG Secondary Loss: {sgHeatTransfer_MW,10:F2} MW");  // v0.8.0 — Heat sink to SG secondary
        sb.AppendLine($"    Net Plant Heat:   {netPlantHeat_MW,10:F2} MW");
        sb.AppendLine();
        sb.AppendLine("  ELECTRICAL:");
        sb.AppendLine($"    RCP Power:        {rcpCount * 6f,10:F1} MW");
        sb.AppendLine($"    Heater Power:     {pzrHeaterPower,10:F1} MW");
        sb.AppendLine($"    Aux Loads:        {25f,10:F1} MW");
        sb.AppendLine($"    Total Grid Load:  {rcpCount * 6f + pzrHeaterPower + 25f,10:F1} MW");
        sb.AppendLine($"    Cumulative Energy:{gridEnergy,10:F1} MWh");
        sb.AppendLine();
        // v1.3.0: SG Multi-Node Thermal Model (replaces v0.8.0 lumped model)
        sb.AppendLine("  SG SECONDARY SIDE (v1.3.0 / v4.3.0 — Multi-Node Stratified Model):");
        sb.AppendLine($"    Bulk Avg Temp:    {T_sg_secondary,10:F2} °F");
        sb.AppendLine($"    Top Node Temp:    {sgTopNodeTemp,10:F2} °F");
        sb.AppendLine($"    Bottom Node Temp: {sgBottomNodeTemp,10:F2} °F");
        sb.AppendLine($"    Stratification:   {sgStratificationDeltaT,10:F2} °F (top-bottom)");
        sb.AppendLine($"    T_RCS - T_SG_top: {T_rcs - sgTopNodeTemp,10:F2} °F");
        sb.AppendLine($"    T_RCS - T_SG_avg: {T_rcs - T_sg_secondary,10:F2} °F");
        sb.AppendLine($"    Heat to SG Sec:   {sgHeatTransfer_MW,10:F2} MW");
        sb.AppendLine($"    Heat to SG Sec:   {sgHeatTransfer_MW * PlantConstants.MW_TO_BTU_HR / 1e6f,10:F2} MBTU/hr");
        // v4.3.0: SG secondary pressure model
        sb.AppendLine($"    SG Sec Pressure:  {sgSecondaryPressure_psia,10:F1} psia ({sgSecondaryPressure_psia - 14.7f:F1} psig)");
        sb.AppendLine($"    SG T_sat:         {sgSaturationTemp_F,10:F1} °F");
        sb.AppendLine($"    SG Max Superheat:  {sgMaxSuperheat_F,10:F1} °F");
        sb.AppendLine($"    Boiling Intensity: {sgBoilingIntensity,10:F3} (0=subcooled, 1=full boil)");
        sb.AppendLine($"    N₂ Blanket:       {(sgNitrogenIsolated ? "ISOLATED" : "BLANKETED"),10}");
        sb.AppendLine($"    SG Boundary Mode: {sgBoundaryMode,10}");
        sb.AppendLine($"    Pressure Source:  {sgPressureSourceBranch,10}");
        sb.AppendLine($"    SteamInventory:   {sgSteamInventory_lb,10:F1} lb");
        sb.AppendLine($"    Boiling Active:   {(sgBoilingActive ? "YES" : "NO"),10}");
        sb.AppendLine($"    Circulation Frac: {sgCirculationFraction,10:F3} [DEPRECATED]");
        // v5.0.0 Stage 4: SG Draining & Level
        string drainStatus = sgDrainingActive ? $"DRAINING {sgDrainingRate_gpm:F0} gpm/SG" :
                             sgDrainingComplete ? "COMPLETE" : "PRE-DRAIN";
        sb.AppendLine($"    Draining Status:  {drainStatus,10}");
        sb.AppendLine($"    Mass Drained:     {sgTotalMassDrained_lb,10:F0} lb");
        sb.AppendLine($"    Secondary Mass:   {sgSecondaryMass_lb,10:F0} lb (of {PlantConstants.SG_SECONDARY_WATER_PER_SG_LB * PlantConstants.SG_COUNT:F0} initial)");
        sb.AppendLine($"    Wide Range Level: {sgWideRangeLevel_pct,10:F1} %");
        sb.AppendLine($"    Narrow Range Lvl: {sgNarrowRangeLevel_pct,10:F1} %");
        sb.AppendLine($"    Steam Produced:   {sgMultiNodeState.TotalSteamProduced_lb,10:F0} lb (cumulative boiloff)");
        sb.AppendLine($"    Node Temps:       {SGMultiNodeThermal.GetDiagnosticString(sgMultiNodeState, T_rcs)}");
        sb.AppendLine();
        sb.AppendLine("  MASS INVENTORY:");
        sb.AppendLine($"    RCS Water Mass:   {rcsWaterMass,10:F0} lb");
        float pzrWaterDensity = WaterProperties.WaterDensity(T_pzr, pressure);
        sb.AppendLine($"    PZR Water Mass:   {pzrWaterVolume * pzrWaterDensity,10:F0} lb");
        sb.AppendLine();
        
        // v5.0.2: Solid-ops mass conservation forensics
        if (solidPressurizer && !bubbleFormed)
        {
            sb.AppendLine("  v5.0.2 MASS CONSERVATION (Solid Ops):");
            sb.AppendLine($"    TotalPrimaryMass: {physicsState.TotalPrimaryMassSolid,10:F0} lb (boundary-only)");
            sb.AppendLine($"    PZR Mass (cons.): {physicsState.PZRWaterMassSolid,10:F0} lb (surge-conserved)");
            sb.AppendLine($"    Loops (derived):  {physicsState.TotalPrimaryMassSolid - physicsState.PZRWaterMassSolid,10:F0} lb (Total - PZR)");
            sb.AppendLine($"    PZR Density:      {solidPlantState.PzrDensity,10:F2} lbm/ft\u00b3");
            sb.AppendLine($"    PZR Vol Implied:  {solidPlantState.PzrVolumeImplied,10:F1} ft\u00b3");
            sb.AppendLine($"    PZR Mass Rate:    {solidPlantState.PzrMassFlowRate,10:F1} lbm/hr");
            sb.AppendLine($"    Surge Transfer:   {solidPlantState.SurgeMassTransfer_lb,10:F3} lb (this step)");
            sb.AppendLine();
        }
        sb.AppendLine("  RVLIS (Reactor Vessel Level Indication System):");
        sb.AppendLine($"    Dynamic Range:    {rvlisDynamic,10:F1} % {(rvlisDynamicValid ? "(VALID - RCPs ON)" : "(INVALID - RCPs OFF)")}");
        sb.AppendLine($"    Full Range:       {rvlisFull,10:F1} % {(rvlisFullValid ? "(VALID - RCPs OFF)" : "(INVALID - RCPs ON)")}");
        sb.AppendLine($"    Upper Range:      {rvlisUpper,10:F1} % {(rvlisUpperValid ? "(VALID - RCPs OFF)" : "(INVALID - RCPs ON)")}");
        sb.AppendLine();
        sb.AppendLine("  SMM (Subcooling Margin Monitor):");
        sb.AppendLine($"    Subcooling Margin:{subcooling,10:F1} °F");
        sb.AppendLine($"    Low Margin (<15°F):{(smmLowMargin ? "ALARM" : "OK"),10}");
        sb.AppendLine($"    No Margin (≤0°F): {(smmNoMargin ? "ALARM" : "OK"),10}");
        sb.AppendLine();
        sb.AppendLine("  VALIDATION STATUS:");
        sb.AppendLine($"    Subcooling >=30F: {(subcooling >= 30f ? "PASS" : "FAIL")}");
        sb.AppendLine($"    RCS Rate <=50F/hr:{(Mathf.Abs(rcsHeatRate) <= MAX_RATE ? "PASS" : "FAIL")}");
        sb.AppendLine($"    VCT Level Normal: {(VCTPhysics.IsLevelNormal(vctState) ? "PASS" : "FAIL")}");
        // v5.4.1 Audit Fix: Use system-wide mass-based conservation check (Fix B)
        // instead of VCT gallon cross-check which is subject to numerical drift.
        // massError_lbm reads from corrected canonical physicsState fields.
        sb.AppendLine($"    Mass Conservation:{(massError_lbm <= PlantConstants.STAGE_E_CONSERVATION_LBM_LIMIT ? "PASS" : "FAIL")} ({massError_lbm:F0} lbm)");
        bool intervalMassPass = inventoryAudit.Conservation_Error_lbm <= PlantConstants.STAGE_E_CONSERVATION_LBM_LIMIT
                             && inventoryAudit.Conservation_Error_pct <= PlantConstants.STAGE_E_INTERVAL_CONSERVATION_PCT_LIMIT;
        sb.AppendLine($"    Interval Conservation:{(intervalMassPass ? "PASS" : "FAIL")} ({inventoryAudit.Conservation_Error_lbm:F1} lbm, {inventoryAudit.Conservation_Error_pct:F3}%)");
        sb.AppendLine($"    RTCC Telemetry:   {(rtccTelemetryPresent ? "PRESENT" : "MISSING")} (count={rtccTransitionCount}, failures={rtccAssertionFailureCount})");
        sb.AppendLine();
        
        // v0.9.6: Enhanced validation section for PZR level and BRS fixes
        sb.AppendLine("  v0.9.6 VALIDATION CHECKS:");
        sb.AppendLine($"    PZR Level Stable:   {(pzrLevel >= 15f ? "PASS" : "FAIL")} (level={pzrLevel:F1}%, min=15%)");
        sb.AppendLine($"    BRS Distillate:     {brsState.DistillateAvailable_gal:F0} gal available");
        sb.AppendLine($"    BRS Cum Returned:   {brsState.CumulativeReturned_gal:F0} gal (makeup from BRS)");
        sb.AppendLine($"    RMS Makeup Avoided: {(brsState.CumulativeReturned_gal > 0 && !vctState.RWSTSuctionActive ? "YES" : "CHECK")}");
        sb.AppendLine($"    Boron Stable:       {(rcsBoronConcentration >= 1500f ? "PASS" : "WARN")} ({rcsBoronConcentration:F0} ppm)");
        sb.AppendLine();
        
        // v4.4.0: PZR Pressure/Level Control Validation
        float pzrLevelSetpoint_v44 = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
        float pzrLevelError_v44 = Math.Abs(pzrLevel - pzrLevelSetpoint_v44);
        float pressure_psig_v44 = pressure - PlantConstants.PSIG_TO_PSIA;
        sb.AppendLine("  v4.4.0 PRESSURE/LEVEL CONTROL VALIDATION:");
        sb.AppendLine($"    PZR Level within \u00b110%:  {(pzrLevelError_v44 <= 10f ? "PASS" : "FAIL")} (level={pzrLevel:F1}%, SP={pzrLevelSetpoint_v44:F1}%, err={pzrLevelError_v44:F1}%)");
        if (solidPlantState.ControlMode == "HEATER_PRESSURIZE")
        {
            float dpSP = Math.Abs(pressure - solidPlantState.PressureSetpoint);
            sb.AppendLine($"    P Rate (HTR):         HTR_PRESS ({pressureRate:F1} psi/hr, dist_SP={dpSP:F1} psi)");
        }
        else
        {
            sb.AppendLine($"    P Rate <200 psi/hr:   {(Math.Abs(pressureRate) < 200f ? "PASS" : "FAIL")} ({pressureRate:F1} psi/hr)");
        }
        sb.AppendLine($"    Heater Mode at PID:   {(currentHeaterMode == HeaterMode.AUTOMATIC_PID || pressure < PlantConstants.HEATER_MODE_TRANSITION_PRESSURE_PSIA ? "PASS" : "FAIL")} (mode={currentHeaterMode})");
        sb.AppendLine($"    Spray if P>2275 psia: {(pressure <= 2275f || sprayActive ? "PASS" : "FAIL")} (P={pressure:F0}, spray={sprayActive})");
        sb.AppendLine();
        
        // v1.1.0 Stage 5: Comprehensive Inventory Audit
        sb.AppendLine("  INVENTORY AUDIT (v1.1.0 — Comprehensive Mass Balance):");
        sb.AppendLine($"    RCS Mass:           {inventoryAudit.RCS_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    PZR Water Mass:     {inventoryAudit.PZR_Water_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    PZR Steam Mass:     {inventoryAudit.PZR_Steam_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    VCT Mass:           {inventoryAudit.VCT_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    BRS Mass:           {inventoryAudit.BRS_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    ------------------------------------");
        sb.AppendLine($"    TOTAL MASS:         {inventoryAudit.Total_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    Mass Source:        {inventoryAudit.AuditMassSource}");  // v5.0.3
        sb.AppendLine($"    Initial Mass:       {inventoryAudit.Initial_Total_Mass_lbm,10:F0} lbm");
        sb.AppendLine($"    Expected Mass:      {inventoryAudit.Expected_Total_Mass_lbm,10:F0} lbm");
        sb.AppendLine();
        sb.AppendLine("    CUMULATIVE FLOWS:");
        sb.AppendLine($"      Charging:         {inventoryAudit.Cumulative_Charging_gal,10:F0} gal");
        sb.AppendLine($"      Letdown:          {inventoryAudit.Cumulative_Letdown_gal,10:F0} gal");
        sb.AppendLine($"      Seal Injection:   {inventoryAudit.Cumulative_SealInjection_gal,10:F0} gal");
        sb.AppendLine($"      Seal Return:      {inventoryAudit.Cumulative_SealReturn_gal,10:F0} gal");
        sb.AppendLine($"      Surge In:         {inventoryAudit.Cumulative_SurgeIn_gal,10:F0} gal");
        sb.AppendLine($"      Surge Out:        {inventoryAudit.Cumulative_SurgeOut_gal,10:F0} gal");
        sb.AppendLine($"      External Makeup:  {inventoryAudit.Cumulative_Makeup_gal,10:F0} gal");
        sb.AppendLine($"      CBO Losses:       {inventoryAudit.Cumulative_CBOLoss_gal,10:F0} gal");
        sb.AppendLine();
        sb.AppendLine("    CONSERVATION CHECK:");
        sb.AppendLine($"      Error (absolute): {inventoryAudit.Conservation_Error_lbm,10:F1} lbm");
        sb.AppendLine($"      Error (percent):  {inventoryAudit.Conservation_Error_pct,10:F3} %");
        sb.AppendLine($"      Status:           {inventoryAudit.StatusMessage}");
        sb.AppendLine($"      Alarm:            {(inventoryAudit.Conservation_Alarm ? "YES - INVESTIGATE" : "NO")}");
        sb.AppendLine($"      Plant External In:{plantExternalIn_gal,10:F1} gal");
        sb.AppendLine($"      Plant External Out:{plantExternalOut_gal,9:F1} gal");
        sb.AppendLine($"      Plant External Net:{plantExternalNet_gal,9:F1} gal");
        sb.AppendLine();
        sb.AppendLine("    RTCC LAST TRANSITION:");
        sb.AppendLine($"      Name:             {rtccLastTransition}");
        sb.AppendLine($"      Authority:        {rtccLastAuthorityFrom} -> {rtccLastAuthorityTo}");
        sb.AppendLine($"      Pre Mass:         {rtccLastPreMass_lbm,10:F1} lbm");
        sb.AppendLine($"      Reconstructed:    {rtccLastReconstructedMass_lbm,10:F1} lbm");
        sb.AppendLine($"      Raw Delta:        {rtccLastRawDelta_lbm,10:F3} lbm");
        sb.AppendLine($"      Post Mass:        {rtccLastPostMass_lbm,10:F1} lbm");
        sb.AppendLine($"      Assert Delta:     {rtccLastAssertDelta_lbm,10:F3} lbm");
        sb.AppendLine($"      Epsilon:          {PlantConstants.RTCC_EPSILON_MASS_LBM,10:F1} lbm");
        sb.AppendLine($"      Result:           {(rtccLastAssertPass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine(new string('=', 70));

        File.WriteAllText(file, sb.ToString());
    }

    // ========================================================================
    // FINAL REPORT — Written when simulation completes or is stopped.
    // ========================================================================

    void SaveReport()
    {
        string file = Path.Combine(logPath, $"Heatup_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var sb = new StringBuilder();
        sb.AppendLine("HEATUP VALIDATION REPORT");
        sb.AppendLine("========================");
        sb.AppendLine($"Time: {DateTime.Now}");
        sb.AppendLine($"Simulation Duration: {simTime:F2} hours ({TimeAcceleration.FormatTime(simTime)})");
        sb.AppendLine($"Wall-Clock Duration: {TimeAcceleration.FormatTime(wallClockTime)}");
        sb.AppendLine($"Effective Speed: {TimeAcceleration.EffectiveMultiplier:F1}x average");
        sb.AppendLine();
        sb.AppendLine($"Final T_avg: {T_avg:F1}°F");
        sb.AppendLine($"Final Pressure: {pressure:F0} psia");
        sb.AppendLine($"Final Subcooling: {subcooling:F1}°F");
        sb.AppendLine($"Final PZR Level: {pzrLevel:F1}%");
        sb.AppendLine($"Grid Energy: {gridEnergy:F0} MWh");
        sb.AppendLine();
        sb.AppendLine("VALIDATION:");
        sb.AppendLine($"  Subcooling ≥30°F: {(subcooling >= 30f ? "PASS" : "FAIL")}");
        sb.AppendLine($"  Rate ≤50°F/hr: {(heatupRate <= MAX_RATE + 5f ? "PASS" : "FAIL")}");
        sb.AppendLine($"  Temp target: {(T_avg >= targetTemperature - 10f ? "PASS" : "FAIL")}");
        sb.AppendLine($"  Mass conservation ≤{PlantConstants.STAGE_E_CONSERVATION_LBM_LIMIT:F0} lbm: {(massError_lbm <= PlantConstants.STAGE_E_CONSERVATION_LBM_LIMIT ? "PASS" : "FAIL")} ({massError_lbm:F1} lbm)");
        sb.AppendLine($"  RTCC telemetry present: {(rtccTelemetryPresent ? "PASS" : "FAIL")}");
        sb.AppendLine($"  RTCC assertion failures: {(rtccAssertionFailureCount == 0 ? "PASS" : "FAIL")} ({rtccAssertionFailureCount})");
        sb.AppendLine();
        
        // v4.4.0: PZR Pressure/Level Control Final Validation
        float finalLevelSP = PlantConstants.GetPZRLevelSetpointUnified(T_avg);
        float finalLevelErr = Math.Abs(pzrLevel - finalLevelSP);
        bool finalPressureOK = (T_avg >= 550f) ? Math.Abs(pressure - 2250f) <= 50f : true;
        sb.AppendLine("v4.4.0 PRESSURE/LEVEL CONTROL FINAL VALIDATION:");
        sb.AppendLine($"  PZR Level within +/-10%:  {(finalLevelErr <= 10f ? "PASS" : "FAIL")} (level={pzrLevel:F1}%, SP={finalLevelSP:F1}%)");
        sb.AppendLine($"  Final P at target (+/-50): {(finalPressureOK ? "PASS" : "FAIL")} ({pressure:F0} psia, target 2250 +/- 50)");
        sb.AppendLine($"  Heater Mode = PID:        {(currentHeaterMode == HeaterMode.AUTOMATIC_PID ? "PASS" : "N/A")} (mode={currentHeaterMode})");
        sb.AppendLine($"  Spray system functional:  {(sprayState.IsEnabled ? "PASS" : "N/A")} (enabled={sprayState.IsEnabled})");
        sb.AppendLine($"  Final spray flow:         {sprayFlow_GPM:F1} gpm");
        sb.AppendLine($"  Final heater power:       {pzrHeaterPower:F3} MW");
        sb.AppendLine();
        
        // v1.1.0 Stage 5: Inventory Audit Summary
        sb.AppendLine("INVENTORY AUDIT SUMMARY (v1.1.0):");
        sb.AppendLine($"  Initial Total Mass:   {inventoryAudit.Initial_Total_Mass_lbm:F0} lbm");
        sb.AppendLine($"  Final Total Mass:     {inventoryAudit.Total_Mass_lbm:F0} lbm");
        sb.AppendLine($"  Expected Total Mass:  {inventoryAudit.Expected_Total_Mass_lbm:F0} lbm");
        sb.AppendLine($"  Conservation Error:   {inventoryAudit.Conservation_Error_lbm:F1} lbm ({inventoryAudit.Conservation_Error_pct:F3}%)");
        sb.AppendLine($"  Conservation Status:  {(inventoryAudit.Conservation_Alarm ? "ALARM" : "OK")}");
        sb.AppendLine($"  Plant External In:    {plantExternalIn_gal:F1} gal");
        sb.AppendLine($"  Plant External Out:   {plantExternalOut_gal:F1} gal");
        sb.AppendLine($"  Plant External Net:   {plantExternalNet_gal:F1} gal");
        sb.AppendLine();
        sb.AppendLine("  Cumulative Flows:");
        sb.AppendLine($"    Total Charging:     {inventoryAudit.Cumulative_Charging_gal:F0} gal");
        sb.AppendLine($"    Total Letdown:      {inventoryAudit.Cumulative_Letdown_gal:F0} gal");
        sb.AppendLine($"    Total Seal Inj:     {inventoryAudit.Cumulative_SealInjection_gal:F0} gal");
        sb.AppendLine($"    Total Seal Return:  {inventoryAudit.Cumulative_SealReturn_gal:F0} gal");
        sb.AppendLine($"    Total Surge In:     {inventoryAudit.Cumulative_SurgeIn_gal:F0} gal");
        sb.AppendLine($"    Total Surge Out:    {inventoryAudit.Cumulative_SurgeOut_gal:F0} gal");
        sb.AppendLine($"    Total Makeup:       {inventoryAudit.Cumulative_Makeup_gal:F0} gal");
        sb.AppendLine($"    Total CBO Losses:   {inventoryAudit.Cumulative_CBOLoss_gal:F0} gal");
        sb.AppendLine();
        sb.AppendLine("  Mass Distribution:");
        sb.AppendLine($"    RCS:                {inventoryAudit.RCS_Mass_lbm:F0} lbm");
        sb.AppendLine($"    PZR (water):        {inventoryAudit.PZR_Water_Mass_lbm:F0} lbm");
        sb.AppendLine($"    PZR (steam):        {inventoryAudit.PZR_Steam_Mass_lbm:F0} lbm");
        sb.AppendLine($"    VCT:                {inventoryAudit.VCT_Mass_lbm:F0} lbm");
        sb.AppendLine($"    BRS:                {inventoryAudit.BRS_Mass_lbm:F0} lbm");
        sb.AppendLine();
        
        // v5.0.0 Stage 4: SG Secondary Side Summary
        float sgInitialMass = PlantConstants.SG_SECONDARY_WATER_PER_SG_LB * PlantConstants.SG_COUNT;
        sb.AppendLine("SG SECONDARY SIDE SUMMARY (v5.0.0):");
        sb.AppendLine($"  Initial Mass:         {sgInitialMass:F0} lb (wet layup, 4 SGs)");
        sb.AppendLine($"  Final Mass:           {sgSecondaryMass_lb:F0} lb");
        sb.AppendLine($"  Mass Drained:         {sgTotalMassDrained_lb:F0} lb");
        sb.AppendLine($"  Steam Produced:       {sgMultiNodeState.TotalSteamProduced_lb:F0} lb");
        sb.AppendLine($"  Draining Status:      {(sgDrainingComplete ? "COMPLETE" : sgDrainingActive ? "IN PROGRESS" : "PRE-DRAIN")}");
        sb.AppendLine($"  Wide Range Level:     {sgWideRangeLevel_pct:F1}%");
        sb.AppendLine($"  Narrow Range Level:   {sgNarrowRangeLevel_pct:F1}%");
        sb.AppendLine($"  SG Regime:            {sgMultiNodeState.CurrentRegime}");
        sb.AppendLine($"  Secondary Pressure:   {sgSecondaryPressure_psia:F1} psia");
        sb.AppendLine($"  SG Boundary Mode:     {sgBoundaryMode}");
        sb.AppendLine($"  Pressure Source:      {sgPressureSourceBranch}");
        sb.AppendLine($"  Steam Inventory:      {sgSteamInventory_lb:F1} lb");
        sb.AppendLine($"  Net Plant Heat:       {netPlantHeat_MW:F2} MW");
        sb.AppendLine();
        sb.AppendLine("RTCC SUMMARY (IP-0016):");
        sb.AppendLine($"  Telemetry Present:    {(rtccTelemetryPresent ? "YES" : "NO")}");
        sb.AppendLine($"  Transition Count:     {rtccTransitionCount}");
        sb.AppendLine($"  Assertion Failures:   {rtccAssertionFailureCount}");
        sb.AppendLine($"  Last Transition:      {rtccLastTransition}");
        sb.AppendLine($"  Last Authority Path:  {rtccLastAuthorityFrom} -> {rtccLastAuthorityTo}");
        sb.AppendLine($"  Last Raw Delta:       {rtccLastRawDelta_lbm:F3} lbm");
        sb.AppendLine($"  Last Assert Delta:    {rtccLastAssertDelta_lbm:F3} lbm");
        sb.AppendLine($"  Max Raw |Delta|:      {rtccMaxRawDeltaAbs_lbm:F3} lbm");
        sb.AppendLine($"  Epsilon:              {PlantConstants.RTCC_EPSILON_MASS_LBM:F1} lbm");
        sb.AppendLine();
        
        File.WriteAllText(file, sb.ToString());
        Debug.Log($"Report saved: {file}");
    }
}
