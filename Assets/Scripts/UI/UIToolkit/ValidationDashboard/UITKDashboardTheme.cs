// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// UITKDashboardTheme.cs — Color Palette and Constants for UI Toolkit
// ============================================================================
//
// PURPOSE:
//   Centralized color and styling constants for the UI Toolkit Validation
//   Dashboard. Mirrors ValidationDashboardTheme.cs but optimized for
//   UI Toolkit patterns (StyleColor, USS variables).
//
// USAGE:
//   Colors can be used directly or converted to StyleColor:
//     element.style.color = UITKDashboardTheme.NormalGreen;
//     element.style.backgroundColor = new StyleColor(UITKDashboardTheme.BackgroundPanel);
//
// VERSION: 1.0.0
// DATE: 2026-02-18
// CS: CS-0127 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// Color palette and style constants for the UI Toolkit Validation Dashboard.
    /// </summary>
    public static class UITKDashboardTheme
    {
        // ====================================================================
        // BACKGROUND COLORS
        // ====================================================================
        
        /// <summary>Main dashboard background.</summary>
        public static readonly Color BackgroundDark = new Color32(15, 17, 24, 255);
        
        /// <summary>Panel container background.</summary>
        public static readonly Color BackgroundPanel = new Color32(23, 26, 36, 245);
        
        /// <summary>Header bar background.</summary>
        public static readonly Color BackgroundHeader = new Color32(12, 14, 20, 255);
        
        /// <summary>Section header background.</summary>
        public static readonly Color BackgroundSection = new Color32(30, 35, 50, 255);
        
        /// <summary>Gauge track background.</summary>
        public static readonly Color BackgroundGauge = new Color32(38, 41, 51, 255);
        
        /// <summary>Graph plot area background.</summary>
        public static readonly Color BackgroundGraph = new Color32(10, 12, 18, 255);
        
        // ====================================================================
        // FUNCTIONAL STATUS COLORS
        // ====================================================================
        
        /// <summary>Normal / safe state - bright green.</summary>
        public static readonly Color NormalGreen = new Color32(46, 217, 64, 255);
        
        /// <summary>Warning / approach to limit - bright amber.</summary>
        public static readonly Color WarningAmber = new Color32(255, 199, 0, 255);
        
        /// <summary>Alarm / limit violation - bright red.</summary>
        public static readonly Color AlarmRed = new Color32(255, 46, 46, 255);
        
        /// <summary>Critical / trip condition - magenta.</summary>
        public static readonly Color TripMagenta = new Color32(255, 0, 204, 255);
        
        /// <summary>Informational / in-progress - cyan.</summary>
        public static readonly Color InfoCyan = new Color32(0, 217, 242, 255);
        
        /// <summary>Active / selected accent - blue.</summary>
        public static readonly Color AccentBlue = new Color32(51, 128, 255, 255);
        
        // ====================================================================
        // TEXT COLORS
        // ====================================================================
        
        /// <summary>Primary text (white).</summary>
        public static readonly Color TextPrimary = new Color32(235, 237, 242, 255);
        
        /// <summary>Secondary / dim text.</summary>
        public static readonly Color TextSecondary = new Color32(140, 148, 166, 255);
        
        /// <summary>Disabled text.</summary>
        public static readonly Color TextDisabled = new Color32(80, 85, 100, 255);
        
        // ====================================================================
        // GAUGE COLORS
        // ====================================================================
        
        /// <summary>Gauge needle color.</summary>
        public static readonly Color GaugeNeedle = new Color32(255, 255, 255, 242);
        
        /// <summary>Gauge tick marks.</summary>
        public static readonly Color GaugeTick = new Color32(102, 107, 122, 255);
        
        /// <summary>Gauge arc track (unlit).</summary>
        public static readonly Color GaugeTrack = new Color32(38, 41, 51, 255);
        
        // ====================================================================
        // TRACE COLORS (for strip charts)
        // ====================================================================
        
        public static readonly Color Trace1 = new Color32(46, 217, 64, 255);    // Green
        public static readonly Color Trace2 = new Color32(255, 102, 51, 255);   // Orange
        public static readonly Color Trace3 = new Color32(77, 153, 255, 255);   // Blue
        public static readonly Color Trace4 = new Color32(255, 199, 0, 255);    // Amber
        public static readonly Color Trace5 = new Color32(204, 51, 230, 255);   // Purple
        public static readonly Color Trace6 = new Color32(0, 217, 242, 255);    // Cyan
        
        /// <summary>Grid lines color.</summary>
        public static readonly Color TraceGrid = new Color32(51, 56, 71, 255);
        
        // ====================================================================
        // ANNUNCIATOR COLORS
        // ====================================================================
        
        public static readonly Color AnnunciatorOff = new Color(0.12f, 0.13f, 0.16f, 1f);
        public static readonly Color AnnunciatorNormal = new Color(0.10f, 0.35f, 0.12f, 1f);
        public static readonly Color AnnunciatorWarning = new Color(0.45f, 0.35f, 0.00f, 1f);
        public static readonly Color AnnunciatorAlarm = new Color(0.50f, 0.08f, 0.08f, 1f);
        public static readonly Color AnnunciatorTextDim = new Color(0.55f, 0.58f, 0.65f, 1f);
        
        // ====================================================================
        // SIZING CONSTANTS
        // ====================================================================
        
        public const float GaugeDiameter = 120f;
        public const float GaugeMiniDiameter = 80f;
        public const float GaugeLargeDiameter = 160f;
        public const float GaugeArcWidth = 10f;
        public const float GaugeNeedleWidth = 2.5f;
        
        public const float PaddingSmall = 4f;
        public const float PaddingStandard = 8f;
        public const float PaddingLarge = 16f;
        
        public const float HeaderHeight = 48f;
        public const float TabBarHeight = 36f;
        public const float SectionHeaderHeight = 28f;
        
        // ====================================================================
        // ANIMATION CONSTANTS
        // ====================================================================
        
        public const float NeedleSmoothTime = 0.08f;
        public const float ColorTransitionDuration = 0.2f;
        public const float AlarmFlashRate = 3f;  // Hz
        
        // ====================================================================
        // HELPER METHODS
        // ====================================================================
        
        /// <summary>
        /// Get threshold color for a value.
        /// </summary>
        public static Color GetThresholdColor(float value, float warnLow, float warnHigh,
            float alarmLow, float alarmHigh)
        {
            if (value < alarmLow || value > alarmHigh) return AlarmRed;
            if (value < warnLow || value > warnHigh) return WarningAmber;
            return NormalGreen;
        }
        
        /// <summary>
        /// Get trace color by index.
        /// </summary>
        public static Color GetTraceColor(int index)
        {
            return (index % 6) switch
            {
                0 => Trace1,
                1 => Trace2,
                2 => Trace3,
                3 => Trace4,
                4 => Trace5,
                5 => Trace6,
                _ => Trace1
            };
        }
        
        /// <summary>
        /// Create a glow version of a color.
        /// </summary>
        public static Color GetGlowColor(Color baseColor, float alpha = 0.4f)
        {
            return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
        
        /// <summary>
        /// Convert to StyleColor for UI Toolkit.
        /// </summary>
        public static StyleColor ToStyleColor(this Color color)
        {
            return new StyleColor(color);
        }
    }
}
