// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Annunciators.cs - ISA-18.1 Compliant Annunciator System
// ============================================================================
//
// PURPOSE:
//   Implements a 27-tile annunciator panel with ISA-18.1 compliant state
//   machine behavior. Provides visual alarming for plant parameter deviations
//   with click-to-acknowledge and batch acknowledge/reset functionality.
//
// ISA-18.1 STATE MACHINE:
//   - OFF: Condition normal, never alarmed
//   - NORMAL: Condition normal, was previously alarmed but acknowledged/reset
//   - ALERTING: Condition abnormal, not yet acknowledged (FLASHING)
//   - ACKNOWLEDGED: Condition abnormal, operator acknowledged (STEADY)
//   - ALARM: Critical condition (RED, may flash)
//
// BEHAVIOR:
//   - When condition goes abnormal → ALERTING (flashing)
//   - Click on ALERTING tile → ACKNOWLEDGED (steady amber)
//   - When condition returns to normal while ACKNOWLEDGED → ready for RESET
//   - RESET clears tiles that have returned to normal
//   - ACK acknowledges all currently ALERTING tiles
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Critical.Validation
{
    /// <summary>
    /// ISA-18.1 compliant annunciator state.
    /// </summary>
    public enum AnnunciatorState
    {
        Off,           // Condition normal, inactive
        Normal,        // Condition normal, previously alarmed
        Alerting,      // Abnormal, not acknowledged (FLASHING)
        Acknowledged,  // Abnormal, acknowledged (STEADY)
        Alarm          // Critical (RED)
    }

    /// <summary>
    /// Severity level for annunciators and events.
    /// </summary>
    public enum AlarmSeverity
    {
        Info,      // Informational (cyan)
        Warning,   // Warning (amber)
        Alarm      // Alarm (red)
    }

    /// <summary>
    /// Individual annunciator tile definition.
    /// </summary>
    public class AnnunciatorTile
    {
        public int Index;
        public string Label;
        public string Description;
        public AlarmSeverity Severity;
        public AnnunciatorState State;
        public bool ConditionActive;      // Is the alarm condition currently true?
        public bool WasAcknowledged;      // Was this acknowledged while active?
        public float ActivationTime;      // When did it become active?
        public Func<DashboardSnapshot, bool> Condition;  // Evaluation function

        public AnnunciatorTile(int index, string label, string desc, 
            AlarmSeverity severity, Func<DashboardSnapshot, bool> condition)
        {
            Index = index;
            Label = label;
            Description = desc;
            Severity = severity;
            Condition = condition;
            State = AnnunciatorState.Off;
            ConditionActive = false;
            WasAcknowledged = false;
        }

        /// <summary>
        /// Update the tile state based on current condition.
        /// </summary>
        public void Update(DashboardSnapshot snapshot)
        {
            bool wasActive = ConditionActive;
            ConditionActive = Condition != null && Condition(snapshot);

            // INFO severity tiles: Simple ON/OFF, no alarm state machine
            // These are status indicators (green when on, gray when off)
            if (Severity == AlarmSeverity.Info)
            {
                State = ConditionActive ? AnnunciatorState.Normal : AnnunciatorState.Off;
                return;
            }

            // WARNING/ALARM severity: Full ISA-18.1 state machine
            if (ConditionActive && !wasActive)
            {
                // Condition just became active
                if (State == AnnunciatorState.Off || State == AnnunciatorState.Normal)
                {
                    State = Severity == AlarmSeverity.Alarm 
                        ? AnnunciatorState.Alarm 
                        : AnnunciatorState.Alerting;
                    ActivationTime = Time.time;
                    WasAcknowledged = false;
                }
            }
            else if (!ConditionActive && wasActive)
            {
                // Condition just cleared
                if (State == AnnunciatorState.Acknowledged || WasAcknowledged)
                {
                    // Ready for reset - stays acknowledged but condition is clear
                    State = AnnunciatorState.Normal;
                }
                else if (State == AnnunciatorState.Alerting || State == AnnunciatorState.Alarm)
                {
                    // Cleared before acknowledgment
                    State = AnnunciatorState.Normal;
                }
            }
        }

        /// <summary>
        /// Acknowledge this tile if it's alerting.
        /// </summary>
        public bool Acknowledge()
        {
            if (State == AnnunciatorState.Alerting || State == AnnunciatorState.Alarm)
            {
                State = AnnunciatorState.Acknowledged;
                WasAcknowledged = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reset this tile if condition has cleared.
        /// </summary>
        public bool Reset()
        {
            if (!ConditionActive && (State == AnnunciatorState.Acknowledged || 
                State == AnnunciatorState.Normal))
            {
                State = AnnunciatorState.Off;
                WasAcknowledged = false;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Event log entry.
    /// </summary>
    public class EventLogEntry
    {
        public float SimTime;
        public string Message;
        public AlarmSeverity Severity;

        public EventLogEntry(float simTime, string message, AlarmSeverity severity)
        {
            SimTime = simTime;
            Message = message;
            Severity = severity;
        }
    }

    /// <summary>
    /// Manages the 27-tile annunciator panel and event log.
    /// </summary>
    public class AnnunciatorManager
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        public const int TILE_COUNT = 27;
        public const int TILES_PER_ROW = 9;
        public const int ROW_COUNT = 3;
        public const int EVENT_LOG_SIZE = 32;
        public const int VISIBLE_EVENTS = 8;

        // ====================================================================
        // STATE
        // ====================================================================

        private readonly AnnunciatorTile[] _tiles;
        private readonly List<EventLogEntry> _eventLog;
        private bool _initialized = false;

        // Counts
        private int _alertingCount;
        private int _acknowledgedCount;
        private int _alarmCount;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public AnnunciatorManager()
        {
            _tiles = new AnnunciatorTile[TILE_COUNT];
            _eventLog = new List<EventLogEntry>(EVENT_LOG_SIZE);
        }

        /// <summary>
        /// Initialize all annunciator tiles with their conditions.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            // Row 1: Pressurizer and RCS primary
            _tiles[0] = new AnnunciatorTile(0, "HTR ON", "PZR Heaters Energized",
                AlarmSeverity.Info, s => s.PzrHeatersOn);

            _tiles[1] = new AnnunciatorTile(1, "HEATUP", "Heatup In Progress",
                AlarmSeverity.Info, s => s.HeatupInProgress);

            _tiles[2] = new AnnunciatorTile(2, "BUBBLE", "PZR Bubble Formed",
                AlarmSeverity.Info, s => s.BubbleFormed);

            _tiles[3] = new AnnunciatorTile(3, "MODE 5", "Plant in Mode 5 (Cold SD)",
                AlarmSeverity.Info, s => s.PlantMode == 5);

            _tiles[4] = new AnnunciatorTile(4, "P LO", "RCS Pressure Low",
                AlarmSeverity.Warning, s => s.PressureLow);

            _tiles[5] = new AnnunciatorTile(5, "P HI", "RCS Pressure High",
                AlarmSeverity.Alarm, s => s.PressureHigh);

            _tiles[6] = new AnnunciatorTile(6, "SC LO", "Subcooling Low",
                AlarmSeverity.Warning, s => s.SubcoolingLow);

            _tiles[7] = new AnnunciatorTile(7, "FLOW", "RCS Flow Low",
                AlarmSeverity.Warning, s => s.RcsFlowLow);

            _tiles[8] = new AnnunciatorTile(8, "MODE 4", "Plant in Mode 4 (Hot SD)",
                AlarmSeverity.Info, s => s.PlantMode == 4);

            // Row 2: Pressurizer level and CVCS
            _tiles[9] = new AnnunciatorTile(9, "L LO", "PZR Level Low",
                AlarmSeverity.Warning, s => s.PzrLevelLow);

            _tiles[10] = new AnnunciatorTile(10, "L HI", "PZR Level High",
                AlarmSeverity.Warning, s => s.PzrLevelHigh);

            _tiles[11] = new AnnunciatorTile(11, "VCT LO", "VCT Level Low",
                AlarmSeverity.Warning, s => s.VctLevelLow);

            _tiles[12] = new AnnunciatorTile(12, "VCT HI", "VCT Level High",
                AlarmSeverity.Warning, s => s.VctLevelHigh);

            _tiles[13] = new AnnunciatorTile(13, "CHG", "Charging Active",
                AlarmSeverity.Info, s => s.ChargingActive);

            _tiles[14] = new AnnunciatorTile(14, "LTD", "Letdown Active",
                AlarmSeverity.Info, s => s.LetdownActive);

            _tiles[15] = new AnnunciatorTile(15, "SEAL", "Seal Injection OK",
                AlarmSeverity.Info, s => s.SealInjectionOK);

            _tiles[16] = new AnnunciatorTile(16, "DVRT", "VCT Divert Active",
                AlarmSeverity.Warning, s => s.VctDivertActive);

            _tiles[17] = new AnnunciatorTile(17, "MKUP", "VCT Makeup Active",
                AlarmSeverity.Warning, s => s.VctMakeupActive);

            // Row 3: Support systems and RCPs
            _tiles[18] = new AnnunciatorTile(18, "CCW", "CCW Running",
                AlarmSeverity.Info, s => s.CcwRunning);

            _tiles[19] = new AnnunciatorTile(19, "RCP 1", "RCP 1 Running",
                AlarmSeverity.Info, s => s.RcpRunning[0]);

            _tiles[20] = new AnnunciatorTile(20, "RCP 2", "RCP 2 Running",
                AlarmSeverity.Info, s => s.RcpRunning[1]);

            _tiles[21] = new AnnunciatorTile(21, "RCP 3", "RCP 3 Running",
                AlarmSeverity.Info, s => s.RcpRunning[2]);

            _tiles[22] = new AnnunciatorTile(22, "RCP 4", "RCP 4 Running",
                AlarmSeverity.Info, s => s.RcpRunning[3]);

            _tiles[23] = new AnnunciatorTile(23, "SG P", "SG Boiling Active",
                AlarmSeverity.Info, s => s.SgBoilingActive);

            _tiles[24] = new AnnunciatorTile(24, "RHR", "RHR System Active",
                AlarmSeverity.Info, s => s.RhrActive);

            _tiles[25] = new AnnunciatorTile(25, "HZP", "HZP Conditions Ready",
                AlarmSeverity.Info, s => s.HzpStable);

            _tiles[26] = new AnnunciatorTile(26, "PZR SAT", "PZR At Saturation Temperature",
                AlarmSeverity.Info, s => s.PzrAtSaturation);

            _initialized = true;
        }

        // ====================================================================
        // UPDATE
        // ====================================================================

        /// <summary>
        /// Update all annunciator states from snapshot.
        /// </summary>
        public void Update(DashboardSnapshot snapshot)
        {
            if (!_initialized || snapshot == null) return;

            _alertingCount = 0;
            _acknowledgedCount = 0;
            _alarmCount = 0;

            for (int i = 0; i < TILE_COUNT; i++)
            {
                var tile = _tiles[i];
                var prevState = tile.State;
                
                tile.Update(snapshot);

                // Track counts
                switch (tile.State)
                {
                    case AnnunciatorState.Alerting:
                        _alertingCount++;
                        break;
                    case AnnunciatorState.Acknowledged:
                        _acknowledgedCount++;
                        break;
                    case AnnunciatorState.Alarm:
                        _alarmCount++;
                        break;
                }

                // Log state transitions
                if (prevState != tile.State)
                {
                    LogStateChange(tile, prevState, snapshot.SimTime);
                }
            }
        }

        private void LogStateChange(AnnunciatorTile tile, AnnunciatorState prevState, float simTime)
        {
            string msg = null;

            if (prevState == AnnunciatorState.Off && 
                (tile.State == AnnunciatorState.Alerting || tile.State == AnnunciatorState.Alarm))
            {
                msg = $"{tile.Label}: {tile.Description}";
            }
            else if (tile.State == AnnunciatorState.Off && prevState != AnnunciatorState.Off)
            {
                msg = $"{tile.Label}: Cleared";
            }
            else if (tile.State == AnnunciatorState.Acknowledged)
            {
                msg = $"{tile.Label}: Acknowledged";
            }

            if (msg != null)
            {
                AddEvent(simTime, msg, tile.Severity);
            }
        }

        // ====================================================================
        // ACKNOWLEDGE / RESET
        // ====================================================================

        /// <summary>
        /// Acknowledge a specific tile by index.
        /// Returns true if tile was acknowledged.
        /// </summary>
        public bool AcknowledgeTile(int index)
        {
            if (index < 0 || index >= TILE_COUNT) return false;
            return _tiles[index].Acknowledge();
        }

        /// <summary>
        /// Acknowledge all currently alerting tiles.
        /// Returns count of tiles acknowledged.
        /// </summary>
        public int AcknowledgeAll()
        {
            int count = 0;
            for (int i = 0; i < TILE_COUNT; i++)
            {
                if (_tiles[i].Acknowledge()) count++;
            }
            return count;
        }

        /// <summary>
        /// Reset all tiles that have returned to normal.
        /// Returns count of tiles reset.
        /// </summary>
        public int ResetAll()
        {
            int count = 0;
            for (int i = 0; i < TILE_COUNT; i++)
            {
                if (_tiles[i].Reset()) count++;
            }
            return count;
        }

        // ====================================================================
        // EVENT LOG
        // ====================================================================

        /// <summary>
        /// Add an event to the log.
        /// </summary>
        public void AddEvent(float simTime, string message, AlarmSeverity severity)
        {
            _eventLog.Insert(0, new EventLogEntry(simTime, message, severity));
            
            // Trim to max size
            while (_eventLog.Count > EVENT_LOG_SIZE)
            {
                _eventLog.RemoveAt(_eventLog.Count - 1);
            }
        }

        /// <summary>
        /// Get recent events (most recent first).
        /// </summary>
        public IReadOnlyList<EventLogEntry> GetEvents(int count)
        {
            int n = Mathf.Min(count, _eventLog.Count);
            return _eventLog.GetRange(0, n);
        }

        /// <summary>
        /// Clear the event log.
        /// </summary>
        public void ClearEvents()
        {
            _eventLog.Clear();
        }

        // ====================================================================
        // ACCESSORS
        // ====================================================================

        public AnnunciatorTile GetTile(int index)
        {
            if (index < 0 || index >= TILE_COUNT) return null;
            return _tiles[index];
        }

        public int AlertingCount => _alertingCount;
        public int AcknowledgedCount => _acknowledgedCount;
        public int AlarmCount => _alarmCount;
        public int TotalActiveCount => _alertingCount + _acknowledgedCount + _alarmCount;
        public bool IsInitialized => _initialized;
        public int EventCount => _eventLog.Count;
    }

}
