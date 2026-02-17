// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Theme
// ValidationDashboardTheme.cs - Color Palette and Style Constants
// ============================================================================
//
// PURPOSE:
//   Centralized definition of all colors, fonts, sizing, and styling
//   constants for the Validation Dashboard. All visual components should
//   reference this class rather than defining their own colors.
//
// REFERENCE:
//   Westinghouse 4-Loop PWR control room color conventions:
//     - Dark background (near-black with slight blue tint)
//     - Green for normal/safe parameters
//     - Amber/yellow for warnings and approach to limit
//     - Red for alarms and limit violations
//     - Cyan for informational / in-progress states
//     - White for text labels and digital readouts
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using UnityEngine;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Centralized theme and style constants for the Validation Dashboard.
    /// </summary>
    public static class ValidationDashboardTheme
    {
        // ====================================================================
        // BACKGROUND COLORS
        // ====================================================================

        /// <summary>Main dashboard background (near-black with blue tint).</summary>
        public static readonly Color BackgroundDark = new Color32(15, 17, 24, 255);      // #0F1118

        /// <summary>Panel container background.</summary>
        public static readonly Color BackgroundPanel = new Color32(23, 26, 36, 245);     // #171A24 (96% alpha)

        /// <summary>Header bar background.</summary>
        public static readonly Color BackgroundHeader = new Color32(12, 14, 20, 255);    // #0C0E14

        /// <summary>Gauge background (unlit arc).</summary>
        public static readonly Color BackgroundGauge = new Color32(38, 41, 51, 255);     // #262933

        /// <summary>Graph plot area background.</summary>
        public static readonly Color BackgroundGraph = new Color32(10, 12, 18, 255);     // #0A0C12

        /// <summary>Section header background.</summary>
        public static readonly Color BackgroundSection = new Color32(30, 35, 50, 255);   // #1E2332

        // ====================================================================
        // FUNCTIONAL STATUS COLORS
        // ====================================================================

        /// <summary>Normal / safe state - bright green.</summary>
        public static readonly Color NormalGreen = new Color32(46, 217, 64, 255);        // #2ED940

        /// <summary>Warning / approach to limit - bright amber.</summary>
        public static readonly Color WarningAmber = new Color32(255, 199, 0, 255);       // #FFC700

        /// <summary>Alarm / limit violation - bright red.</summary>
        public static readonly Color AlarmRed = new Color32(255, 46, 46, 255);           // #FF2E2E

        /// <summary>Critical / trip condition - magenta.</summary>
        public static readonly Color TripMagenta = new Color32(255, 0, 204, 255);        // #FF00CC

        // ====================================================================
        // INFORMATIONAL COLORS
        // ====================================================================

        /// <summary>Informational / in-progress - cyan.</summary>
        public static readonly Color InfoCyan = new Color32(0, 217, 242, 255);           // #00D9F2

        /// <summary>Active / selected accent - blue.</summary>
        public static readonly Color AccentBlue = new Color32(51, 128, 255, 255);        // #3380FF

        /// <summary>Special / BRS accent - orange.</summary>
        public static readonly Color AccentOrange = new Color32(255, 140, 26, 255);      // #FF8C1A

        /// <summary>Neutral / inactive - gray.</summary>
        public static readonly Color Neutral = new Color32(100, 105, 120, 255);          // #646978

        // ====================================================================
        // TEXT COLORS
        // ====================================================================

        /// <summary>Primary text (white).</summary>
        public static readonly Color TextPrimary = new Color32(235, 237, 242, 255);      // #EBEDF2

        /// <summary>Secondary / dim text.</summary>
        public static readonly Color TextSecondary = new Color32(140, 148, 166, 255);    // #8C94A6

        /// <summary>Dark text (for light backgrounds).</summary>
        public static readonly Color TextDark = new Color32(38, 41, 51, 255);            // #262933

        /// <summary>Disabled text.</summary>
        public static readonly Color TextDisabled = new Color32(80, 85, 100, 255);       // #505564

        // ====================================================================
        // GAUGE COLORS
        // ====================================================================

        /// <summary>Gauge needle / pointer.</summary>
        public static readonly Color GaugeNeedle = new Color32(255, 255, 255, 242);      // White, 95% alpha

        /// <summary>Gauge tick marks.</summary>
        public static readonly Color GaugeTick = new Color32(102, 107, 122, 255);        // #666B7A

        /// <summary>Gauge arc unlit background.</summary>
        public static readonly Color GaugeArcBackground = new Color32(38, 41, 51, 255);  // #262933

        // ====================================================================
        // ANNUNCIATOR TILE COLORS
        // Matches MosaicAlarmPanel.cs proven palette (NRC HRTD Section 4)
        // ====================================================================

        // --- Tile background states ---

        /// <summary>Annunciator off / inactive background.</summary>
        public static readonly Color AnnunciatorOff = new Color(0.12f, 0.13f, 0.16f, 1f);

        /// <summary>Annunciator normal / status background (green).</summary>
        public static readonly Color AnnunciatorNormal = new Color(0.10f, 0.35f, 0.12f, 1f);

        /// <summary>Annunciator warning background (amber).</summary>
        public static readonly Color AnnunciatorWarning = new Color(0.45f, 0.35f, 0.00f, 1f);

        /// <summary>Annunciator alarm background (red).</summary>
        public static readonly Color AnnunciatorAlarm = new Color(0.50f, 0.08f, 0.08f, 1f);

        // --- Tile text/border active colors ---

        /// <summary>Annunciator text/border green (status active).</summary>
        public static readonly Color AnnunciatorTextGreen = new Color(0.18f, 0.82f, 0.25f, 1f);

        /// <summary>Annunciator text/border amber (warning active).</summary>
        public static readonly Color AnnunciatorTextAmber = new Color(1.00f, 0.78f, 0.00f, 1f);

        /// <summary>Annunciator text/border red (alarm active).</summary>
        public static readonly Color AnnunciatorTextRed = new Color(1.00f, 0.18f, 0.18f, 1f);

        // --- Tile inactive colors ---

        /// <summary>Annunciator dim text (inactive tile label).</summary>
        public static readonly Color AnnunciatorTextDim = new Color(0.55f, 0.58f, 0.65f, 1f);

        /// <summary>Annunciator dim border (inactive tile edge).</summary>
        public static readonly Color AnnunciatorBorderDim = new Color(0.20f, 0.22f, 0.26f, 1f);

        // --- Tile acknowledged colors ---

        /// <summary>Annunciator acknowledged background (muted).</summary>
        public static readonly Color AnnunciatorAckBg = new Color(0.18f, 0.18f, 0.20f, 1f);

        /// <summary>Annunciator acknowledged text (muted).</summary>
        public static readonly Color AnnunciatorAckText = new Color(0.50f, 0.50f, 0.50f, 1f);

        // ====================================================================
        // GRAPH TRACE COLORS (distinct for multi-trace plots)
        // ====================================================================

        /// <summary>Trace 1 - Green (primary, T_rcs).</summary>
        public static readonly Color Trace1 = new Color32(46, 217, 64, 255);             // Same as NormalGreen

        /// <summary>Trace 2 - Orange-red (T_hot).</summary>
        public static readonly Color Trace2 = new Color32(255, 102, 51, 255);            // #FF6633

        /// <summary>Trace 3 - Blue (T_cold).</summary>
        public static readonly Color Trace3 = new Color32(77, 153, 255, 255);            // #4D99FF

        /// <summary>Trace 4 - Amber (T_pzr, level).</summary>
        public static readonly Color Trace4 = new Color32(255, 199, 0, 255);             // Same as WarningAmber

        /// <summary>Trace 5 - Purple (T_sat).</summary>
        public static readonly Color Trace5 = new Color32(204, 51, 230, 255);            // #CC33E6

        /// <summary>Trace 6 - Cyan (secondary).</summary>
        public static readonly Color Trace6 = new Color32(0, 217, 242, 255);             // Same as InfoCyan

        /// <summary>Grid lines (subtle).</summary>
        public static readonly Color TraceGrid = new Color32(51, 56, 71, 255);           // #333847

        // ====================================================================
        // GLOW EFFECT COLORS (with transparency)
        // ====================================================================

        /// <summary>Glow base for normal state.</summary>
        public static readonly Color GlowNormal = new Color(0.18f, 0.85f, 0.25f, 0.4f);

        /// <summary>Glow base for warning state.</summary>
        public static readonly Color GlowWarning = new Color(1.0f, 0.78f, 0.0f, 0.5f);

        /// <summary>Glow base for alarm state.</summary>
        public static readonly Color GlowAlarm = new Color(1.0f, 0.18f, 0.18f, 0.6f);

        /// <summary>Glow base for cyan/info state.</summary>
        public static readonly Color GlowCyan = new Color(0.0f, 0.85f, 0.95f, 0.4f);

        // ====================================================================
        // SIZING CONSTANTS
        // ====================================================================

        /// <summary>Standard gauge arc diameter (pixels).</summary>
        public const float GaugeArcDiameter = 120f;

        /// <summary>Mini gauge arc diameter (pixels).</summary>
        public const float GaugeMiniDiameter = 80f;

        /// <summary>Large gauge arc diameter (pixels).</summary>
        public const float GaugeLargeDiameter = 160f;

        /// <summary>Gauge arc stroke width (pixels).</summary>
        public const float GaugeArcWidth = 8f;

        /// <summary>Gauge needle width (pixels).</summary>
        public const float GaugeNeedleWidth = 3f;

        /// <summary>Linear gauge height (pixels).</summary>
        public const float LinearGaugeHeight = 16f;

        /// <summary>Mini trend strip height (pixels).</summary>
        public const float MiniTrendHeight = 40f;

        /// <summary>Standard row height for status panels (pixels).</summary>
        public const float StatusRowHeight = 22f;

        /// <summary>Section header height (pixels).</summary>
        public const float SectionHeaderHeight = 28f;

        /// <summary>Tab bar height (pixels).</summary>
        public const float TabBarHeight = 36f;

        /// <summary>Header bar height (pixels).</summary>
        public const float HeaderBarHeight = 48f;

        /// <summary>Standard padding (pixels).</summary>
        public const float PaddingStandard = 8f;

        /// <summary>Small padding (pixels).</summary>
        public const float PaddingSmall = 4f;

        /// <summary>Large padding (pixels).</summary>
        public const float PaddingLarge = 16f;

        // ====================================================================
        // ANIMATION CONSTANTS
        // ====================================================================

        /// <summary>Gauge needle animation smoothing time (seconds).</summary>
        public const float NeedleSmoothTime = 0.1f;

        /// <summary>Color transition duration (seconds).</summary>
        public const float ColorTransitionDuration = 0.2f;

        /// <summary>Tab transition duration (seconds).</summary>
        public const float TabTransitionDuration = 0.25f;

        /// <summary>Alarm pulse cycle duration (seconds).</summary>
        public const float AlarmPulseDuration = 0.5f;

        /// <summary>ISA-18.1 alerting flash rate (Hz) — fast flash for new alarms.</summary>
        public const float AnnunciatorAlertFlashHz = 3.0f;

        /// <summary>ISA-18.1 clearing flash rate (Hz) — slow flash for cleared conditions.</summary>
        public const float AnnunciatorClearFlashHz = 0.7f;

        /// <summary>ISA-18.1 auto-reset delay (seconds) after clearing flash.</summary>
        public const float AnnunciatorAutoResetDelay = 5.0f;

        /// <summary>Glow pulse minimum alpha.</summary>
        public const float GlowPulseMin = 0.3f;

        /// <summary>Glow pulse maximum alpha.</summary>
        public const float GlowPulseMax = 0.8f;

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        /// <summary>
        /// Get appropriate color for a value relative to thresholds.
        /// </summary>
        public static Color GetThresholdColor(float value, float warnLow, float warnHigh,
            float alarmLow, float alarmHigh)
        {
            if (value < alarmLow || value > alarmHigh) return AlarmRed;
            if (value < warnLow || value > warnHigh) return WarningAmber;
            return NormalGreen;
        }

        /// <summary>
        /// Get color for a "low is bad" parameter.
        /// </summary>
        public static Color GetLowThresholdColor(float value, float warn, float alarm)
        {
            if (value < alarm) return AlarmRed;
            if (value < warn) return WarningAmber;
            return NormalGreen;
        }

        /// <summary>
        /// Get color for a "high is bad" parameter.
        /// </summary>
        public static Color GetHighThresholdColor(float value, float warn, float alarm)
        {
            if (value > alarm) return AlarmRed;
            if (value > warn) return WarningAmber;
            return NormalGreen;
        }

        /// <summary>
        /// Lerp between two colors for smooth transitions.
        /// </summary>
        public static Color LerpColor(Color from, Color to, float t)
        {
            return Color.Lerp(from, to, t);
        }

        /// <summary>
        /// Get glow color for a given state color.
        /// </summary>
        public static Color GetGlowColor(Color stateColor)
        {
            // Create glow version with appropriate alpha
            float alpha = 0.4f;
            if (stateColor == AlarmRed) alpha = 0.6f;
            else if (stateColor == WarningAmber) alpha = 0.5f;
            
            return new Color(stateColor.r, stateColor.g, stateColor.b, alpha);
        }

        /// <summary>
        /// Get trace color by index (0-5).
        /// </summary>
        public static Color GetTraceColor(int index)
        {
            switch (index)
            {
                case 0: return Trace1;
                case 1: return Trace2;
                case 2: return Trace3;
                case 3: return Trace4;
                case 4: return Trace5;
                case 5: return Trace6;
                default: return Trace1;
            }
        }
    }
}
