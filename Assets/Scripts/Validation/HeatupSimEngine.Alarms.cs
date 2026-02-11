// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (Alarms Partial)
// HeatupSimEngine.Alarms.cs - Annunciator Update & RVLIS
// ============================================================================
//
// PURPOSE:
//   Annunciator state update, alarm edge detection with event logging,
//   and RVLIS (Reactor Vessel Level Indication System) update.
//   Centralizes all alarm processing for the heatup simulation.
//
// ARCHITECTURE:
//   Partial class of HeatupSimEngine. This file owns:
//     - UpdateAnnunciators() — alarm state update via AlarmManager module
//     - Alarm edge detection with table-driven pattern (G10 deduplication)
//     - UpdateRVLIS() — delegates to RVLISPhysics module
//     - Previous-frame alarm state tracking fields
//
// SOURCES:
//   - NRC HRTD 10.2 — Pressure control setpoints
//   - NRC HRTD 19.2.1 — Solid plant alarm suppression
//   - NUREG-0737 Supp. 1 — RVLIS requirements
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupSimEngine
{
    // ========================================================================
    // PREVIOUS-FRAME ALARM STATES — For edge detection
    // ========================================================================

    #region Alarm Edge Detection State

    private bool prev_pzrLevelLow, prev_pzrLevelHigh;
    private bool prev_pressureLow, prev_pressureHigh;
    private bool prev_subcoolingLow, prev_smmLowMargin, prev_smmNoMargin;
    private bool prev_vctLevelLow, prev_vctLevelHigh;
    private bool prev_vctMakeupActive, prev_vctRWSTSuction;
    private bool prev_letdownIsolated;
    private bool prev_rvlisLevelLow;
    private int prev_plantMode = -1;
    private int prev_rcpCount = -1;
    
    // v0.9.6: BRS makeup tracking for validation
    private bool prev_brsMakeupActive;

    #endregion

    // ========================================================================
    // TABLE-DRIVEN ALARM EDGE DETECTION (G10 — eliminates 15 duplicate pairs)
    //
    // Each alarm is defined as a descriptor with:
    //   - A getter for the current alarm state
    //   - A ref to the previous-frame state for edge detection
    //   - Rising/falling edge log messages and severity
    //
    // A single loop handles all edge detection, replacing ~90 lines of
    // identical if (new && !prev) / if (!new && prev) pairs.
    // ========================================================================

    #region Table-Driven Alarm Infrastructure

    /// <summary>
    /// Descriptor for a single alarm edge-detection entry.
    /// </summary>
    private struct AlarmEdgeDescriptor
    {
        public bool CurrentValue;
        public bool PreviousValue;
        public EventSeverity RisingSeverity;
        public string RisingMessage;
        public string FallingMessage;      // null = no falling edge log
    }

    /// <summary>
    /// Build the alarm edge table for the current timestep, process all edges,
    /// and update previous-frame states. Called at the end of UpdateAnnunciators().
    /// </summary>
    private void ProcessAlarmEdges()
    {
        // Plant mode transitions (special — not boolean alarm)
        if (plantMode != prev_plantMode && prev_plantMode >= 0)
        {
            LogEvent(EventSeverity.INFO, $"MODE CHANGE: Mode {prev_plantMode} -> Mode {plantMode}");
        }
        prev_plantMode = plantMode;

        // Build alarm descriptor table — one entry per alarm
        var alarms = new AlarmEdgeDescriptor[]
        {
            new AlarmEdgeDescriptor {
                CurrentValue = pzrLevelLow, PreviousValue = prev_pzrLevelLow,
                RisingSeverity = EventSeverity.ALERT,
                RisingMessage = $"PZR LEVEL LOW  ({pzrLevel:F1}%)",
                FallingMessage = "PZR level low CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = pzrLevelHigh, PreviousValue = prev_pzrLevelHigh,
                RisingSeverity = EventSeverity.ALERT,
                RisingMessage = $"PZR LEVEL HIGH  ({pzrLevel:F1}%)",
                FallingMessage = "PZR level high CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = pressureLow, PreviousValue = prev_pressureLow,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = $"PRESSURE LOW  ({pressure:F0} psia)",
                FallingMessage = "Pressure low CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = pressureHigh, PreviousValue = prev_pressureHigh,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = $"PRESSURE HIGH  ({pressure:F0} psia)",
                FallingMessage = "Pressure high CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = subcoolingLow, PreviousValue = prev_subcoolingLow,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = $"SUBCOOLING LOW  ({subcooling:F1}F < 30F)",
                FallingMessage = "Subcooling margin RESTORED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = smmLowMargin, PreviousValue = prev_smmLowMargin,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = $"SMM LOW MARGIN  ({subcooling:F1}F < 15F)",
                FallingMessage = null  // No clearing message for SMM
            },
            new AlarmEdgeDescriptor {
                CurrentValue = smmNoMargin, PreviousValue = prev_smmNoMargin,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = $"SMM SATURATION  (subcooling <= 0F)",
                FallingMessage = null
            },
            new AlarmEdgeDescriptor {
                CurrentValue = vctLevelLow, PreviousValue = prev_vctLevelLow,
                RisingSeverity = EventSeverity.ALERT,
                RisingMessage = $"VCT LEVEL LOW  ({vctState.Level_percent:F0}%)",
                FallingMessage = "VCT level low CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = vctLevelHigh, PreviousValue = prev_vctLevelHigh,
                RisingSeverity = EventSeverity.ALERT,
                RisingMessage = $"VCT LEVEL HIGH  ({vctState.Level_percent:F0}%)",
                FallingMessage = "VCT level high CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = vctMakeupActive, PreviousValue = prev_vctMakeupActive,
                RisingSeverity = EventSeverity.ACTION,
                RisingMessage = "VCT MAKEUP initiated",
                FallingMessage = null
            },
            new AlarmEdgeDescriptor {
                CurrentValue = vctRWSTSuction, PreviousValue = prev_vctRWSTSuction,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = "RWST SUCTION ACTIVATED",
                FallingMessage = null
            },
            new AlarmEdgeDescriptor {
                CurrentValue = letdownIsolatedFlag, PreviousValue = prev_letdownIsolated,
                RisingSeverity = EventSeverity.ALERT,
                RisingMessage = $"LETDOWN ISOLATED (PZR lvl {pzrLevel:F1}%)",
                FallingMessage = "Letdown isolation CLEARED"
            },
            new AlarmEdgeDescriptor {
                CurrentValue = rvlisLevelLow, PreviousValue = prev_rvlisLevelLow,
                RisingSeverity = EventSeverity.ALARM,
                RisingMessage = $"RVLIS LEVEL LOW  ({rvlisFull:F0}%)",
                FallingMessage = null
            },
            // v0.9.6: BRS distillate makeup tracking
            new AlarmEdgeDescriptor {
                CurrentValue = vctState.MakeupFromBRS && vctState.AutoMakeupActive,
                PreviousValue = prev_brsMakeupActive,
                RisingSeverity = EventSeverity.INFO,
                RisingMessage = $"BRS DISTILLATE MAKEUP initiated (VCT={vctState.Level_percent:F0}%, BRS avail={brsState.DistillateAvailable_gal:F0} gal)",
                FallingMessage = null
            },
        };

        // Process all alarm edges in a single loop
        for (int i = 0; i < alarms.Length; i++)
        {
            // Rising edge — alarm activating
            if (alarms[i].CurrentValue && !alarms[i].PreviousValue)
                LogEvent(alarms[i].RisingSeverity, alarms[i].RisingMessage);

            // Falling edge — alarm clearing (only if message defined)
            if (!alarms[i].CurrentValue && alarms[i].PreviousValue && alarms[i].FallingMessage != null)
                LogEvent(EventSeverity.INFO, alarms[i].FallingMessage);
        }

        // Store previous states for next cycle
        prev_pzrLevelLow = pzrLevelLow;
        prev_pzrLevelHigh = pzrLevelHigh;
        prev_pressureLow = pressureLow;
        prev_pressureHigh = pressureHigh;
        prev_subcoolingLow = subcoolingLow;
        prev_smmLowMargin = smmLowMargin;
        prev_smmNoMargin = smmNoMargin;
        prev_vctLevelLow = vctLevelLow;
        prev_vctLevelHigh = vctLevelHigh;
        prev_vctMakeupActive = vctMakeupActive;
        prev_vctRWSTSuction = vctRWSTSuction;
        prev_letdownIsolated = letdownIsolatedFlag;
        prev_rvlisLevelLow = rvlisLevelLow;
        prev_brsMakeupActive = vctState.MakeupFromBRS && vctState.AutoMakeupActive;  // v0.9.6
    }

    #endregion

    // ========================================================================
    // RVLIS — Reactor Vessel Level Indication System
    // Delegated to RVLISPhysics module per NUREG-0737 Supp. 1
    // ========================================================================

    void UpdateRVLIS()
    {
        var rvlisState = RVLISPhysics.Calculate(physicsState.RCSWaterMass, T_rcs, pressure, rcpCount);

        rvlisDynamic = rvlisState.DynamicRange;
        rvlisFull = rvlisState.FullRange;
        rvlisUpper = rvlisState.UpperRange;
        rvlisDynamicValid = rvlisState.DynamicValid;
        rvlisFullValid = rvlisState.FullRangeValid;
        rvlisUpperValid = rvlisState.UpperRangeValid;
        rvlisLevelLow = rvlisState.LevelLowAlarm;
    }

    // ========================================================================
    // ANNUNCIATOR UPDATE — Delegates alarm checking to AlarmManager module,
    // then processes edge detection for event logging.
    // ========================================================================

    void UpdateAnnunciators()
    {
        for (int i = 0; i < 4; i++)
            rcpRunning[i] = (i < rcpCount);

        // ALARM CHECKING — Owned by AlarmManager module
        // Per NRC HRTD: Centralized setpoint checking for all annunciators
        var alarmInputs = new AlarmInputs
        {
            PZRLevel = pzrLevel,
            Pressure = pressure,
            Subcooling = subcooling,
            HeatupRate = heatupRate,
            RVLISFull = rvlisFull,
            RVLISFullValid = rvlisFullValid,
            BubbleFormed = bubbleFormed,
            SolidPressurizer = solidPressurizer,
            RCPCount = rcpCount,
            ChargingFlow = chargingFlow,
            LetdownFlow = letdownFlow,
            SealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM
        };
        var alarms = AlarmManager.CheckAlarms(alarmInputs);

        // Map alarm state to engine display variables
        pzrHeatersOn = (pzrHeaterPower > 0.1f);
        pzrLevelLow = alarms.PZRLevelLow;
        pzrLevelHigh = alarms.PZRLevelHigh;
        steamBubbleOK = alarms.SteamBubbleOK;
        rcsFlowLow = alarms.RCSFlowLow;
        pressureLow = alarms.PressureLow;
        pressureHigh = alarms.PressureHigh;
        subcoolingLow = alarms.SubcoolingLow;
        smmLowMargin = alarms.SMMLowMargin;
        smmNoMargin = alarms.SMMNoMargin;
        rvlisLevelLow = alarms.RVLISLevelLow;
        ccwRunning = alarms.CCWRunning;
        sealInjectionOK = alarms.SealInjectionOK;
        chargingActive = alarms.ChargingActive;
        letdownActive = alarms.LetdownActive;
        heatupInProgress = alarms.HeatupInProgress;
        modePermissive = alarms.ModePermissive;

        // Table-driven edge detection and event logging (G10 refactor)
        ProcessAlarmEdges();
    }
}
