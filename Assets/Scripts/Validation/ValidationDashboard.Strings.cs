// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Strings.cs - Preformatted String Caching
// ============================================================================
//
// PURPOSE:
//   Preformats all display strings in Update() to avoid string allocations
//   in OnGUI. The dashboard only updates strings when values actually change,
//   providing zero-allocation rendering.
//
// PERFORMANCE CRITICAL:
//   - All string formatting happens in Update() at 10 Hz, not in OnGUI
//   - Change detection prevents unnecessary reformatting
//   - Cached strings used directly in OnGUI Label calls
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;
using Critical.Physics;

namespace Critical.Validation
{
    public partial class ValidationDashboard
    {
        // ====================================================================
        // CACHED HEADER STRINGS
        // ====================================================================

        private string _cachedModeStr = "---";
        private string _cachedPhaseStr = "INITIALIZING";
        private string _cachedSimTimeStr = "SIM: 0:00:00";
        private string _cachedWallTimeStr = "WALL: 0:00:00";
        private string _cachedSpeedStr = "1x";

        private Color _cachedModeColor = _cTextPrimary;
        private Color _cachedSpeedColor = _cTextPrimary;

        // Change detection values
        private int _prevPlantMode = -1;
        private string _prevPhaseDesc = "";
        private float _prevSimTime = -1f;
        private float _prevWallTime = -1f;
        private int _prevSpeedIndex = -1;

        // ====================================================================
        // PREFORMAT METHOD
        // ====================================================================

        /// <summary>
        /// Preformat all display strings from snapshot.
        /// Called in Update() at refresh rate, not in OnGUI.
        /// Only reformats when values change.
        /// </summary>
        private void PreformatStrings()
        {
            if (_snapshot == null) return;

            // Mode string (changes rarely)
            if (_snapshot.PlantMode != _prevPlantMode)
            {
                _prevPlantMode = _snapshot.PlantMode;
                _cachedModeStr = GetModeString(_snapshot.PlantMode);
                _cachedModeColor = GetModeColor(_snapshot.PlantMode);
            }

            // Phase description (changes at phase transitions)
            if (_snapshot.HeatupPhaseDesc != _prevPhaseDesc)
            {
                _prevPhaseDesc = _snapshot.HeatupPhaseDesc;
                _cachedPhaseStr = string.IsNullOrEmpty(_snapshot.HeatupPhaseDesc) 
                    ? "INITIALIZING" 
                    : _snapshot.HeatupPhaseDesc;
            }

            // Sim time (changes every second of sim time)
            float simTimeTrunc = Mathf.Floor(_snapshot.SimTime * 3600f);
            if (simTimeTrunc != _prevSimTime)
            {
                _prevSimTime = simTimeTrunc;
                _cachedSimTimeStr = $"SIM: {FormatTime(_snapshot.SimTime)}";
            }

            // Wall time (changes every second of real time)
            float wallTimeTrunc = Mathf.Floor(_snapshot.WallClockTime * 3600f);
            if (wallTimeTrunc != _prevWallTime)
            {
                _prevWallTime = wallTimeTrunc;
                _cachedWallTimeStr = $"WALL: {FormatTime(_snapshot.WallClockTime)}";
            }

            // Speed (changes on hotkey)
            if (_snapshot.SpeedIndex != _prevSpeedIndex)
            {
                _prevSpeedIndex = _snapshot.SpeedIndex;
                string[] speedLabels = { "1x", "2x", "4x", "8x", "10x" };
                int idx = Mathf.Clamp(_snapshot.SpeedIndex, 0, speedLabels.Length - 1);
                _cachedSpeedStr = speedLabels[idx];
                _cachedSpeedColor = _snapshot.IsAccelerated ? _cWarningAmber : _cTextPrimary;
            }
        }

        // ====================================================================
        // FORMATTING HELPERS
        // ====================================================================

        /// <summary>
        /// Format hours to HH:MM:SS string.
        /// </summary>
        private static string FormatTime(float hours)
        {
            if (float.IsNaN(hours) || float.IsInfinity(hours))
                return "0:00:00";

            float totalSeconds = hours * 3600f;
            int h = (int)(totalSeconds / 3600f);
            int m = (int)((totalSeconds % 3600f) / 60f);
            int s = (int)(totalSeconds % 60f);
            return $"{h}:{m:D2}:{s:D2}";
        }

        /// <summary>
        /// Get mode display string.
        /// </summary>
        private static string GetModeString(int mode)
        {
            switch (mode)
            {
                case 5: return "MODE 5 Cold SD";
                case 4: return "MODE 4 Hot SD";
                case 3: return "MODE 3 Hot Standby";
                default: return "UNKNOWN";
            }
        }

        /// <summary>
        /// Get mode display color.
        /// </summary>
        private static Color GetModeColor(int mode)
        {
            switch (mode)
            {
                case 5: return _cCyanInfo;
                case 4: return _cWarningAmber;
                case 3: return _cNormalGreen;
                default: return _cTextSecondary;
            }
        }

        // ====================================================================
        // VALUE FORMATTING (used by gauge/panel rendering)
        // ====================================================================

        /// <summary>
        /// Format temperature value.
        /// </summary>
        private static string FormatTemp(float value)
        {
            return $"{value:F1}";
        }

        /// <summary>
        /// Format pressure value.
        /// </summary>
        private static string FormatPressure(float value)
        {
            return $"{value:F0}";
        }

        /// <summary>
        /// Format level percentage.
        /// </summary>
        private static string FormatLevel(float value)
        {
            return $"{value:F1}";
        }

        /// <summary>
        /// Format flow rate.
        /// </summary>
        private static string FormatFlow(float value)
        {
            return $"{value:F1}";
        }

        /// <summary>
        /// Format rate of change.
        /// </summary>
        private static string FormatRate(float value)
        {
            return $"{value:F1}";
        }

        /// <summary>
        /// Format power in MW.
        /// </summary>
        private static string FormatPower(float value)
        {
            return $"{value:F3}";
        }

        /// <summary>
        /// Format signed value with +/-.
        /// </summary>
        private static string FormatSigned(float value)
        {
            return $"{value:+0.0;-0.0;0.0}";
        }
    }
}
