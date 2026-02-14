// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab Critical Partial)
// HeatupValidationVisual.TabCritical.cs - Tab 8: Critical Variables Overview
// ============================================================================
//
// PURPOSE:
//   Renders a single-screen, no-scroll "at a glance" validation tab showing
//   the five most important subsystem summaries simultaneously:
//     1. RCS Primary  — Temps, pressure, pressure rate, heat input
//     2. Pressurizer  — Pressure, temp, level, heaters, spray, bubble
//     3. Steam Generator — Pressure, T_sat, T_bulk, ΔT, boiling, steam dump
//     4. CVCS          — Charging, letdown, net flow, inventory, conservation
//     5. VCT           — Level, makeup/divert/RWST flags
//
//   Layout (2-row, 3+2 grid, no scrolling at 1920×1080+):
//     ┌─────────────────┬──────────────────┬──────────────────┐
//     │  RCS PRIMARY     │  PRESSURIZER     │  STEAM GENERATOR │
//     ├─────────────────┴─────────┬────────┴──────────────────┤
//     │  CVCS (Flows & Inventory)  │  VCT (Level & Status)     │
//     └────────────────────────────┴───────────────────────────┘
//
// READS FROM:
//   HeatupSimEngine public fields only — no private access, no new data.
//   All fields verified to exist in HeatupSimEngine.cs as of v5.1.0.
//
// RENDERING:
//   Uses only existing cached styles and colors from Styles partial.
//   No new Color objects, no new Texture2D allocations, no new GUIStyles.
//   Large readouts use _gaugeValueStyle (24pt). Secondary rows use
//   _statusLabelStyle / _statusValueStyle (14pt).
//
// REFERENCE:
//   Westinghouse 4-Loop PWR control room — operator "board walk" overview
//   NRC HRTD 19.0 — Plant Operations monitoring requirements
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawCriticalTab(Rect) — dispatched from Core tab switch (case 7)
//     - DrawCriticalBlock_RCS()
//     - DrawCriticalBlock_PZR()
//     - DrawCriticalBlock_SG()
//     - DrawCriticalBlock_CVCS()
//     - DrawCriticalBlock_VCT()
//     - DrawCriticalBigRow() — large numeric readout row
//     - DrawCriticalSmallRow() — normal-sized status row
//     - DrawCriticalIndicator() — ON/OFF or YES/NO boolean indicator
//
// GOLD STANDARD: No (new additive UI — no physics)
// v5.2.0: New file — Critical tab for at-a-glance plant validation
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // CRITICAL TAB — LAYOUT CONSTANTS
    // ========================================================================

    // Top row: 3 equal blocks. Bottom row: 2 equal blocks.
    const float CRIT_TOP_ROW_FRAC = 0.55f;    // Top row gets 55% of height
    const float CRIT_BOTTOM_ROW_FRAC = 0.45f;  // Bottom row gets 45%
    const float CRIT_BLOCK_PAD = 4f;            // Padding between blocks

    // Row heights inside blocks
    const float CRIT_BIG_ROW_H = 26f;          // Large numeric readout
    const float CRIT_SMALL_ROW_H = 18f;        // Normal status row (matches STATUS_ROW_H)

    // ========================================================================
    // WARNING SUPPRESSION — One-shot missing field warnings
    // ========================================================================

    private static bool _critMissingFieldWarned = false;

    // ========================================================================
    // THRESHOLD CONSTANTS — Placeholder values for color coding
    // TODO: Move to PlantConstants or configuration asset in future version
    // ========================================================================

    // RCS pressure rate thresholds (psi/hr)
    const float CRIT_PRESS_RATE_WARN = 100f;
    const float CRIT_PRESS_RATE_ALARM = 200f;

    // PZR level deviation from setpoint (%)
    const float CRIT_PZR_LEVEL_DEV_WARN = 10f;
    const float CRIT_PZR_LEVEL_DEV_ALARM = 15f;

    // CVCS net flow magnitude (gpm)
    const float CRIT_NET_FLOW_WARN = 10f;
    const float CRIT_NET_FLOW_ALARM = 20f;

    // SG early boiling temperature threshold (°F)
    // Boiling below this T_rcs is unexpected during heatup
    const float CRIT_SG_EARLY_BOIL_TEMP = 350f;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawCriticalTab(Rect area)
    {
        if (engine == null) return;

        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float pad = CRIT_BLOCK_PAD;
        float topH = (area.height - pad) * CRIT_TOP_ROW_FRAC;
        float botH = (area.height - pad) * CRIT_BOTTOM_ROW_FRAC;

        // === TOP ROW: 3 blocks (RCS, PZR, SG) ===
        float topBlockW = (area.width - pad * 4f) / 3f;
        float topY = area.y + pad;

        Rect rcsRect = new Rect(area.x + pad, topY, topBlockW, topH - pad);
        Rect pzrRect = new Rect(area.x + pad * 2f + topBlockW, topY, topBlockW, topH - pad);
        Rect sgRect  = new Rect(area.x + pad * 3f + topBlockW * 2f, topY, topBlockW, topH - pad);

        DrawCriticalBlock_RCS(rcsRect);
        DrawCriticalBlock_PZR(pzrRect);
        DrawCriticalBlock_SG(sgRect);

        // === BOTTOM ROW: 2 blocks (CVCS, VCT) ===
        float botBlockW = (area.width - pad * 3f) / 2f;
        float botY = area.y + topH + pad;

        Rect cvcsRect = new Rect(area.x + pad, botY, botBlockW, botH - pad);
        Rect vctRect  = new Rect(area.x + pad * 2f + botBlockW, botY, botBlockW, botH - pad);

        DrawCriticalBlock_CVCS(cvcsRect);
        DrawCriticalBlock_VCT(vctRect);
    }

    // ========================================================================
    // BLOCK: RCS PRIMARY
    // ========================================================================

    private void DrawCriticalBlock_RCS(Rect area)
    {
        GUI.Box(area, GUIContent.none, _gaugeBgStyle);

        float x = area.x + 6f;
        float w = area.width - 12f;
        float y = area.y;

        DrawSectionHeader(new Rect(area.x, y, area.width, GAUGE_GROUP_HEADER_H),
            "RCS PRIMARY");
        y += GAUGE_GROUP_HEADER_H + 4f;

        // Big readouts: T_avg and Pressure
        Color tavgC = GetHighThresholdColor(engine.T_avg, 545f, 570f,
            _cNormalGreen, _cWarningAmber, _cAlarmRed);
        DrawCriticalBigRow(ref y, x, w, "T_AVG", $"{engine.T_avg:F1} °F", tavgC);

        float psia = engine.pressure;
        float psig = psia - 14.696f;
        DrawCriticalBigRow(ref y, x, w, "PRESSURE",
            $"{psia:F0} psia  ({psig:F0} psig)", _cTextPrimary);

        y += 4f;

        // Secondary rows
        DrawCriticalSmallRow(ref y, x, w, "T_HOT", $"{engine.T_hot:F1} °F", _cTrace2);
        DrawCriticalSmallRow(ref y, x, w, "T_COLD", $"{engine.T_cold:F1} °F", _cTrace3);

        // Pressure rate with color coding
        float absRate = Mathf.Abs(engine.pressureRate);
        Color rateC = absRate < CRIT_PRESS_RATE_WARN ? _cNormalGreen :
                      absRate < CRIT_PRESS_RATE_ALARM ? _cWarningAmber : _cAlarmRed;
        DrawCriticalSmallRow(ref y, x, w, "PRESS RATE",
            $"{engine.pressureRate:F1} psi/hr", rateC);

        // Heat input
        float totalHeatIn = engine.effectiveRCPHeat + engine.pzrHeaterPower / 1000f;
        DrawCriticalSmallRow(ref y, x, w, "HEAT IN",
            $"{totalHeatIn:F2} MW", _cCyanInfo);

        DrawCriticalSmallRow(ref y, x, w, "HEAT TO SG",
            $"{engine.sgHeatTransfer_MW:F2} MW",
            engine.sgHeatTransfer_MW > 0.1f ? _cOrangeAccent : _cTextSecondary);
    }

    // ========================================================================
    // BLOCK: PRESSURIZER
    // ========================================================================

    private void DrawCriticalBlock_PZR(Rect area)
    {
        GUI.Box(area, GUIContent.none, _gaugeBgStyle);

        float x = area.x + 6f;
        float w = area.width - 12f;
        float y = area.y;

        DrawSectionHeader(new Rect(area.x, y, area.width, GAUGE_GROUP_HEADER_H),
            "PRESSURIZER");
        y += GAUGE_GROUP_HEADER_H + 4f;

        // Big readouts: Pressure and Level
        DrawCriticalBigRow(ref y, x, w, "PZR PRESS",
            $"{engine.pressure:F0} psia", _cTextPrimary);

        float levelDev = Mathf.Abs(engine.pzrLevel - engine.pzrLevelSetpointDisplay);
        Color levelC = levelDev < CRIT_PZR_LEVEL_DEV_WARN ? _cNormalGreen :
                       levelDev < CRIT_PZR_LEVEL_DEV_ALARM ? _cWarningAmber : _cAlarmRed;
        DrawCriticalBigRow(ref y, x, w, "PZR LEVEL",
            $"{engine.pzrLevel:F1} %", levelC);

        y += 4f;

        // Secondary rows
        DrawCriticalSmallRow(ref y, x, w, "PZR TEMP",
            $"{engine.T_pzr:F1} °F", _cTrace4);

        DrawCriticalSmallRow(ref y, x, w, "HEATER",
            $"{engine.pzrHeaterPower:F0} kW",
            engine.pzrHeatersOn ? _cNormalGreen : _cTextSecondary);

        // Spray indicator
        DrawCriticalIndicator(ref y, x, w, "SPRAY",
            engine.sprayActive, "ON", "OFF",
            engine.sprayActive ? _cCyanInfo : _cTextSecondary);

        // Bubble state
        string bubbleStr;
        Color bubbleC;
        if (engine.solidPressurizer)
        {
            bubbleStr = "SOLID";
            bubbleC = _cWarningAmber;
        }
        else if (engine.bubbleFormed)
        {
            bubbleStr = "NORMAL (BUBBLE)";
            bubbleC = _cNormalGreen;
        }
        else
        {
            bubbleStr = "FORMING";
            bubbleC = _cCyanInfo;
        }
        DrawCriticalSmallRow(ref y, x, w, "BUBBLE", bubbleStr, bubbleC);
    }

    // ========================================================================
    // BLOCK: STEAM GENERATOR
    // ========================================================================

    private void DrawCriticalBlock_SG(Rect area)
    {
        GUI.Box(area, GUIContent.none, _gaugeBgStyle);

        float x = area.x + 6f;
        float w = area.width - 12f;
        float y = area.y;

        DrawSectionHeader(new Rect(area.x, y, area.width, GAUGE_GROUP_HEADER_H),
            "STEAM GENERATOR (×4)");
        y += GAUGE_GROUP_HEADER_H + 4f;

        // Big readouts: SG Pressure and ΔT
        DrawCriticalBigRow(ref y, x, w, "SG PRESS",
            $"{engine.sgSecondaryPressure_psia:F0} psia", _cTextPrimary);

        float deltaT = engine.T_rcs - engine.T_sg_secondary;
        Color deltaTColor = deltaT > 0f ? _cNormalGreen : _cWarningAmber;
        DrawCriticalBigRow(ref y, x, w, "PRI–SEC ΔT",
            $"{deltaT:F1} °F", deltaTColor);

        y += 4f;

        // Secondary rows
        DrawCriticalSmallRow(ref y, x, w, "T_SAT (SG)",
            $"{engine.sgSaturationTemp_F:F1} °F", _cTrace5);

        DrawCriticalSmallRow(ref y, x, w, "SG BULK TEMP",
            $"{engine.T_sg_secondary:F1} °F", _cTrace6);

        // Steam dump indicator
        DrawCriticalIndicator(ref y, x, w, "STEAM DUMP",
            engine.steamDumpActive, "ACTIVE", "OFF",
            engine.steamDumpActive ? _cOrangeAccent : _cTextSecondary);

        // Boiling indicator with early-boiling warning
        bool boilingAlarm = engine.sgBoilingActive && engine.T_rcs < CRIT_SG_EARLY_BOIL_TEMP;
        Color boilC = boilingAlarm ? _cAlarmRed :
                      engine.sgBoilingActive ? _cWarningAmber : _cNormalGreen;
        DrawCriticalIndicator(ref y, x, w, "BOILING?",
            engine.sgBoilingActive, "YES", "NO", boilC);
    }

    // ========================================================================
    // BLOCK: CVCS (Flows & Inventory)
    // ========================================================================

    private void DrawCriticalBlock_CVCS(Rect area)
    {
        GUI.Box(area, GUIContent.none, _gaugeBgStyle);

        float x = area.x + 6f;
        float w = area.width - 12f;
        float y = area.y;

        DrawSectionHeader(new Rect(area.x, y, area.width, GAUGE_GROUP_HEADER_H),
            "CVCS — FLOWS & INVENTORY");
        y += GAUGE_GROUP_HEADER_H + 4f;

        // Big readouts: Charging, Letdown, Net
        DrawCriticalBigRow(ref y, x, w, "CHARGING",
            $"{engine.chargingFlow:F1} gpm",
            engine.chargingActive ? _cNormalGreen : _cTextSecondary);

        DrawCriticalBigRow(ref y, x, w, "LETDOWN",
            $"{engine.letdownFlow:F1} gpm",
            engine.letdownActive ? _cNormalGreen : _cTextSecondary);

        float netFlow = engine.chargingFlow - engine.letdownFlow;
        float absNet = Mathf.Abs(netFlow);
        Color netC = absNet < CRIT_NET_FLOW_WARN ? _cNormalGreen :
                     absNet < CRIT_NET_FLOW_ALARM ? _cWarningAmber : _cAlarmRed;
        string netSign = netFlow >= 0f ? "+" : "";
        DrawCriticalBigRow(ref y, x, w, "NET FLOW",
            $"{netSign}{netFlow:F1} gpm", netC);

        y += 4f;

        // Inventory tracking (v5.4.1: mass-based)
        DrawCriticalSmallRow(ref y, x, w, "SYSTEM MASS",
            $"{engine.totalSystemMass_lbm:F0} lbm", _cTextPrimary);

        // v5.4.2: Primary mass conservation — canonical lbm ledger
        // Thresholds: green < 100, amber 100–500, red > 500 lbm
        float massErr = engine.massError_lbm;
        Color massC = Mathf.Abs(massErr) < 100f ? _cNormalGreen :
                      Mathf.Abs(massErr) < 500f ? _cWarningAmber : _cAlarmRed;
        DrawCriticalSmallRow(ref y, x, w, "MASS CONS ERR",
            $"{massErr:F1} lbm", massC);

        // VCT flow imbalance — CVCS loop diagnostic (gallons, not primary conservation)
        // Thresholds: green < 10, amber 10–50, red > 50 gal
        float vctErr = engine.massConservationError;
        Color vctC = Mathf.Abs(vctErr) < 10f ? _cNormalGreen :
                     Mathf.Abs(vctErr) < 50f ? _cWarningAmber : _cAlarmRed;
        DrawCriticalSmallRow(ref y, x, w, "VCT FLOW IMB",
            $"{vctErr:F1} gal", vctC);
    }

    // ========================================================================
    // BLOCK: VCT (Level & Status)
    // ========================================================================

    private void DrawCriticalBlock_VCT(Rect area)
    {
        GUI.Box(area, GUIContent.none, _gaugeBgStyle);

        float x = area.x + 6f;
        float w = area.width - 12f;
        float y = area.y;

        DrawSectionHeader(new Rect(area.x, y, area.width, GAUGE_GROUP_HEADER_H),
            "VCT — VOLUME CONTROL TANK");
        y += GAUGE_GROUP_HEADER_H + 4f;

        // Big readout: VCT Level
        float vctLvl = engine.vctState.Level_percent;
        Color vctLvlC;
        if (vctLvl < PlantConstants.VCT_LEVEL_NORMAL_LOW)
            vctLvlC = vctLvl < PlantConstants.VCT_LEVEL_NORMAL_LOW - 5f ? _cAlarmRed : _cWarningAmber;
        else if (vctLvl > PlantConstants.VCT_LEVEL_NORMAL_HIGH)
            vctLvlC = vctLvl > PlantConstants.VCT_LEVEL_NORMAL_HIGH + 5f ? _cAlarmRed : _cWarningAmber;
        else
            vctLvlC = _cNormalGreen;

        DrawCriticalBigRow(ref y, x, w, "VCT LEVEL",
            $"{vctLvl:F1} %", vctLvlC);

        // Normal band reference
        DrawCriticalSmallRow(ref y, x, w, "NORMAL BAND",
            $"{PlantConstants.VCT_LEVEL_NORMAL_LOW:F0}–{PlantConstants.VCT_LEVEL_NORMAL_HIGH:F0} %",
            _cTextSecondary);

        y += 4f;

        // Status flags
        DrawCriticalIndicator(ref y, x, w, "MAKEUP",
            engine.vctMakeupActive, "ACTIVE", "OFF",
            engine.vctMakeupActive ? _cCyanInfo : _cTextSecondary);

        DrawCriticalIndicator(ref y, x, w, "DIVERT",
            engine.vctDivertActive, "ACTIVE", "OFF",
            engine.vctDivertActive ? _cOrangeAccent : _cTextSecondary);

        DrawCriticalIndicator(ref y, x, w, "RWST SUCTION",
            engine.vctRWSTSuction, "YES", "NO",
            engine.vctRWSTSuction ? _cAlarmRed : _cNormalGreen);

        // Annunciator flags
        if (engine.vctLevelLow)
            DrawCriticalSmallRow(ref y, x, w, "⚠ VCT LO", "ALARM", _cAlarmRed);
        if (engine.vctLevelHigh)
            DrawCriticalSmallRow(ref y, x, w, "⚠ VCT HI", "ALARM", _cAlarmRed);

        // If no alarms, show all-clear
        if (!engine.vctLevelLow && !engine.vctLevelHigh)
            DrawCriticalSmallRow(ref y, x, w, "VCT ALARMS", "NONE", _cNormalGreen);
    }

    // ========================================================================
    // RENDERING HELPERS — Big value row, small row, indicator
    // ========================================================================

    /// <summary>
    /// Draw a large numeric readout row (label left, big value right).
    /// Uses _gaugeValueStyle for the value to make key numbers prominent.
    /// </summary>
    private void DrawCriticalBigRow(ref float y, float x, float w,
        string label, string value, Color valueColor)
    {
        float labelW = w * 0.35f;
        float valueW = w * 0.65f;

        // Label in secondary style (smaller, dimmer)
        GUI.Label(new Rect(x, y + 3f, labelW, CRIT_BIG_ROW_H),
            label, _statusLabelStyle);

        // Value in gauge style (larger font)
        var prev = GUI.contentColor;
        GUI.contentColor = valueColor;
        GUI.Label(new Rect(x + labelW, y, valueW, CRIT_BIG_ROW_H),
            value, _gaugeValueStyle);
        GUI.contentColor = prev;

        y += CRIT_BIG_ROW_H;
    }

    /// <summary>
    /// Draw a normal-sized status row (wraps existing DrawStatusRow pattern).
    /// </summary>
    private void DrawCriticalSmallRow(ref float y, float x, float w,
        string label, string value, Color valueColor)
    {
        DrawStatusRow(ref y, x, w, label, value, valueColor);
    }

    /// <summary>
    /// Draw a boolean ON/OFF or YES/NO indicator row with color.
    /// </summary>
    private void DrawCriticalIndicator(ref float y, float x, float w,
        string label, bool state, string trueStr, string falseStr,
        Color stateColor)
    {
        string val = state ? trueStr : falseStr;
        DrawStatusRow(ref y, x, w, label, val, stateColor);
    }
}
