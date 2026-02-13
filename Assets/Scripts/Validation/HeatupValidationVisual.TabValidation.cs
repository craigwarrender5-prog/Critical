// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab Validation Partial)
// HeatupValidationVisual.TabValidation.cs - Tab 7: Validation
// ============================================================================
//
// PURPOSE:
//   Renders the Validation/Debug tab — all PASS/FAIL checks, RVLIS panel,
//   inventory audit detail, conservation error tracking, and memory/performance
//   monitoring. This tab is primarily for developer validation and debugging,
//   not typical operator monitoring.
//
//   Layout (single scrollable column, full width):
//     ┌─────────────────────────────────────────────────┐
//     │ RVLIS PANEL                                     │
//     │ (Dynamic/Full/Upper ranges + validity)          │
//     ├─────────────────────────────────────────────────┤
//     │ INVENTORY PANEL                                 │
//     │ (RCS + PZR + VCT + BRS mass balance)            │
//     ├─────────────────────────────────────────────────┤
//     │ MEMORY / PERFORMANCE                            │
//     │ (reserved + graphics memory usage)              │
//     ├─────────────────────────────────────────────────┤
//     │ PASS/FAIL VALIDATION CHECKS (scrollable)        │
//     │ (all engine validation checks with status)      │
//     └─────────────────────────────────────────────────┘
//
// READS FROM:
//   Delegates rendering to existing partial methods:
//     - Panels partial: DrawRVLISPanel(), DrawInventoryPanel()
//   Plus inline memory and validation check rendering.
//
// REFERENCE:
//   NRC HRTD 4.1 — RVLIS instrumentation
//   NRC HRTD 19.0 — Plant operations validation criteria
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawValidationTab(Rect) — dispatched from Core tab switch
//
// GOLD STANDARD: Yes
// v5.0.0: New file — Validation tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // VALIDATION TAB STATE
    // ========================================================================

    private Vector2 _validationScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawValidationTab(Rect area)
    {
        if (engine == null) return;

        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float pad = 8f;
        float x = area.x + pad;
        float w = Mathf.Min(area.width - pad * 2f, 800f);  // Cap width for readability

        // Center the content if wider than 800px
        if (area.width > w + pad * 2f)
            x = area.x + (area.width - w) / 2f;

        float labelH = 22f;
        GUI.Label(new Rect(x, area.y + 2f, w, labelH),
            "VALIDATION & DIAGNOSTICS", _sectionHeaderStyle);

        // Estimate total content height
        float contentH = RVLIS_PANEL_H + STATUS_SECTION_GAP
                         + INVENTORY_PANEL_H + STATUS_SECTION_GAP
                         + 120f    // Memory section
                         + STATUS_SECTION_GAP
                         + 600f    // Validation checks (estimated)
                         + 40f;

        float availH = area.height - labelH - 4f;
        Rect scrollArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        _validationScroll = GUI.BeginScrollView(scrollArea, _validationScroll,
            new Rect(0, 0, area.width - 20f, contentH));

        float scrollX = (area.width - 20f > w + pad * 2f)
            ? (area.width - 20f - w) / 2f : pad;
        float y = 4f;

        // RVLIS Panel
        DrawRVLISPanel(scrollX, ref y, w);
        y += STATUS_SECTION_GAP;

        // Inventory Panel
        DrawInventoryPanel(scrollX, ref y, w);
        y += STATUS_SECTION_GAP;

        // Memory / Performance Section
        DrawValidationMemorySection(scrollX, ref y, w);
        y += STATUS_SECTION_GAP;

        // Validation Checks
        DrawValidationChecks(scrollX, ref y, w);

        GUI.EndScrollView();
    }

    // ========================================================================
    // MEMORY / PERFORMANCE SECTION
    // ========================================================================

    private void DrawValidationMemorySection(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "MEMORY / PERFORMANCE");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Reserved memory (managed heap + native)
        long reservedMem = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
        float reservedMB = reservedMem / (1024f * 1024f);
        DrawStatusRow(ref y, x, w, "RESERVED", $"{reservedMB:F0} MB",
            reservedMB < 200f ? _cNormalGreen : _cWarningAmber);

        // Graphics memory
        long graphicsMem = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
        float graphicsMB = graphicsMem / (1024f * 1024f);
        DrawStatusRow(ref y, x, w, "GRAPHICS", $"{graphicsMB:F0} MB", _cCyanInfo);

        // Total
        float totalMB = reservedMB + graphicsMB;
        Color totalC = totalMB < 100f ? _cNormalGreen :
                       totalMB < 300f ? _cWarningAmber : _cAlarmRed;
        DrawStatusRow(ref y, x, w, "TOTAL", $"{totalMB:F0} MB", totalC);

        // Sim performance
        if (engine.simTime > 0f)
        {
            float ratio = engine.wallClockTime > 0f
                ? engine.simTime / engine.wallClockTime : 0f;
            DrawStatusRow(ref y, x, w, "SIM RATIO", $"{ratio:F1}x realtime", _cCyanInfo);
        }

        // Event log size
        int logCount = engine.eventLog != null ? engine.eventLog.Count : 0;
        DrawStatusRow(ref y, x, w, "LOG ENTRIES", $"{logCount}", _cTextSecondary);
    }

    // ========================================================================
    // VALIDATION CHECKS — All PASS/FAIL criteria
    // Aggregates checks from v0.9.6, v1.1.0, v4.4.0 validation systems.
    // ========================================================================

    private void DrawValidationChecks(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "VALIDATION CHECKS");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Physics Conservation — canonical mass-based check is primary
        // v5.4.2: Primary mass conservation uses canonical lbm ledger (Section 3, Constitution)
        // Thresholds: PASS < 100 lbm, WARN 100–500 lbm, FAIL > 500 lbm
        DrawCheckRowThreeState(ref y, x, w, "Primary Mass Conservation",
            engine.massError_lbm, 100f, 500f,
            $"Error: {engine.massError_lbm:F1} lbm");

        // VCT cumulative flow imbalance — CVCS loop-level diagnostic, not primary conservation
        // This tracks VCT gallon-based flow accounting and may drift due to density approximations.
        // Thresholds: PASS < 10 gal, WARN 10–50 gal, FAIL > 50 gal
        DrawCheckRowThreeState(ref y, x, w, "VCT Flow Imbalance",
            engine.massConservationError, 10f, 50f,
            $"Imbalance: {engine.massConservationError:F1} gal");

        // v0.1.0.0 Phase C: Primary ledger drift — canonical ledger vs solver component sum (CS-0007)
        // Thresholds: PASS < 100 lb, WARN 100–1000 lb, FAIL > 1000 lb
        if (engine.primaryMassStatus == "NOT_CHECKED")
        {
            DrawCheckRow(ref y, x, w, "Primary Ledger Drift",
                true, "Not checked yet");
        }
        else
        {
            DrawCheckRowThreeState(ref y, x, w, "Primary Ledger Drift",
                engine.primaryMassDrift_lb, 100f, 1000f,
                $"Drift: {engine.primaryMassDrift_pct:F3}%");
        }

        // Temperature Limits
        DrawCheckRow(ref y, x, w, "Heatup Rate ≤ 50 °F/hr",
            Mathf.Abs(engine.heatupRate) <= 50f,
            $"Current: {engine.heatupRate:F1} °F/hr");

        DrawCheckRow(ref y, x, w, "Subcooling ≥ 15 °F",
            engine.subcooling >= 15f,
            $"Current: {engine.subcooling:F1} °F");

        // PZR Checks
        DrawCheckRow(ref y, x, w, "PZR Level In Band",
            Mathf.Abs(engine.pzrLevel - engine.pzrLevelSetpointDisplay) < 15f,
            $"Level: {engine.pzrLevel:F1}%, Setpt: {engine.pzrLevelSetpointDisplay:F1}%");

        DrawCheckRow(ref y, x, w, "Pressure Rate Acceptable",
            Mathf.Abs(engine.pressureRate) < 200f,
            $"Rate: {engine.pressureRate:F1} psi/hr");

        // CVCS Checks
        DrawCheckRow(ref y, x, w, "Seal Injection OK",
            engine.sealInjectionOK,
            engine.sealInjectionOK ? "All RCP seals supplied" : "SEAL INJECTION FAULT");

        DrawCheckRow(ref y, x, w, "Letdown Not Isolated",
            !engine.letdownIsolatedFlag || !engine.letdownActive,
            engine.letdownIsolatedFlag ? "LETDOWN ISOLATED" : "Normal");

        // VCT/BRS Checks
        DrawCheckRow(ref y, x, w, "VCT Level In Normal Band",
            engine.vctState.Level_percent >= PlantConstants.VCT_LEVEL_NORMAL_LOW
            && engine.vctState.Level_percent <= PlantConstants.VCT_LEVEL_NORMAL_HIGH,
            $"VCT: {engine.vctState.Level_percent:F1}% (normal: {PlantConstants.VCT_LEVEL_NORMAL_LOW}-{PlantConstants.VCT_LEVEL_NORMAL_HIGH}%)");

        // RVLIS
        DrawCheckRow(ref y, x, w, "RVLIS Level OK",
            !engine.rvlisLevelLow,
            engine.rvlisLevelLow ? "LOW LEVEL ALARM" : "Normal");

        // RCP Checks
        if (engine.rcpCount > 0)
        {
            DrawCheckRow(ref y, x, w, "RCPs Fully Ramped",
                engine.rcpContribution.AllFullyRunning,
                $"{engine.rcpContribution.PumpsFullyRunning}/{engine.rcpContribution.PumpsStarted} rated");
        }

        // HZP Checks (if active)
        if (engine.IsHZPActive())
        {
            var prereqs = engine.GetStartupReadiness();
            DrawCheckRow(ref y, x, w, "HZP T_avg Target",
                prereqs.TemperatureOK, prereqs.TemperatureStatus);
            DrawCheckRow(ref y, x, w, "HZP Pressure Target",
                prereqs.PressureOK, prereqs.PressureStatus);
            DrawCheckRow(ref y, x, w, "HZP PZR Level Target",
                prereqs.LevelOK, prereqs.LevelStatus);
            DrawCheckRow(ref y, x, w, "HZP All RCPs Running",
                prereqs.RCPsOK, prereqs.RCPsStatus);

            y += 4f;
            DrawCheckRow(ref y, x, w, "STARTUP READINESS",
                prereqs.AllMet,
                prereqs.AllMet ? "ALL PREREQUISITES MET" : "NOT READY");
        }
    }

    /// <summary>
    /// Draw a single PASS/FAIL check row with colored indicator.
    /// </summary>
    private void DrawCheckRow(ref float y, float x, float w, string checkName,
        bool passed, string detail)
    {
        float rowH = 18f;
        float indW = 50f;

        // PASS/FAIL indicator
        string indText = passed ? "PASS" : "FAIL";
        Color indC = passed ? _cNormalGreen : _cAlarmRed;

        var prev = GUI.contentColor;
        GUI.contentColor = indC;
        GUI.Label(new Rect(x, y, indW, rowH), indText, _statusValueStyle);
        GUI.contentColor = prev;

        // Check name
        GUI.Label(new Rect(x + indW + 4f, y, 200f, rowH), checkName, _statusLabelStyle);

        // Detail (right-aligned)
        prev = GUI.contentColor;
        GUI.contentColor = _cTextSecondary;
        GUI.Label(new Rect(x + indW + 210f, y, w - indW - 214f, rowH), detail, _statusLabelStyle);
        GUI.contentColor = prev;

        y += rowH;
    }

    /// <summary>
    /// Three-state validation row: PASS (green) / WARN (amber) / FAIL (red).
    /// PASS when absValue &lt; warnThreshold, WARN when between warn and fail,
    /// FAIL when absValue &gt;= failThreshold.
    /// </summary>
    private void DrawCheckRowThreeState(ref float y, float x, float w,
        string checkName, float value, float warnThreshold, float failThreshold,
        string detail)
    {
        float rowH = 18f;
        float indW = 50f;
        float absVal = Mathf.Abs(value);

        string indText;
        Color indC;
        if (absVal >= failThreshold)
        {
            indText = "FAIL";
            indC = _cAlarmRed;
        }
        else if (absVal >= warnThreshold)
        {
            indText = "WARN";
            indC = _cWarningAmber;
        }
        else
        {
            indText = "PASS";
            indC = _cNormalGreen;
        }

        var prev = GUI.contentColor;
        GUI.contentColor = indC;
        GUI.Label(new Rect(x, y, indW, rowH), indText, _statusValueStyle);
        GUI.contentColor = prev;

        GUI.Label(new Rect(x + indW + 4f, y, 200f, rowH), checkName, _statusLabelStyle);

        prev = GUI.contentColor;
        GUI.contentColor = _cTextSecondary;
        GUI.Label(new Rect(x + indW + 210f, y, w - indW - 214f, rowH), detail, _statusLabelStyle);
        GUI.contentColor = prev;

        y += rowH;
    }
}
