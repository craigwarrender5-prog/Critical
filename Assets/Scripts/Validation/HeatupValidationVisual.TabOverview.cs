// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab Overview Partial)
// HeatupValidationVisual.TabOverview.cs - Tab 1: Overview
// ============================================================================
//
// PURPOSE:
//   Renders the "operator's eye" overview tab â€” the primary monitoring view
//   for at-a-glance plant status during heatup operations. Displays the most
//   critical parameters without requiring scrolling or tab switching.
//
//   Layout (3-column):
//     â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
//     â”‚ KEY      â”‚  TREND GRAPHS        â”‚  PLANT STATUS      â”‚
//     â”‚ GAUGES   â”‚  (Temps + Pressure,  â”‚  (Overview panel)  â”‚
//     â”‚          â”‚   stacked vertically) â”‚                    â”‚
//     â”‚ T_avg    â”‚                      â”‚  ALARM SUMMARY     â”‚
//     â”‚ T_hot    â”‚                      â”‚  (Annunciator      â”‚
//     â”‚ T_cold   â”‚                      â”‚   tile grid)       â”‚
//     â”‚ Subcool  â”‚                      â”‚                    â”‚
//     â”‚ Pressure â”‚                      â”‚                    â”‚
//     â”‚ PZR Lvl  â”‚                      â”‚                    â”‚
//     â”‚ Heater   â”‚                      â”‚                    â”‚
//     â”‚ Press Rt â”‚                      â”‚                    â”‚
//     â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// READS FROM:
//   Delegates all rendering to existing partial methods:
//     - Gauges partial: DrawTemperatureGauges(), DrawPressurizerGauges()
//     - Graphs partial: DrawGraphContent() for TEMPS (0) and PRESSURE (1)
//     - Panels partial: DrawPlantOverview()
//     - Annunciators partial: DrawAnnunciatorContent()
//
// REFERENCE:
//   Westinghouse 4-Loop PWR main control board â€” operator monitoring position
//   NRC HRTD Section 19 â€” Plant Operations monitoring requirements
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawOverviewTab(Rect) â€” dispatched from Core tab switch
//   Contains layout orchestration only â€” no rendering logic.
//
// GOLD STANDARD: Yes
// v5.0.0: New file â€” Overview tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;


namespace Critical.Validation
{

public partial class HeatupValidationVisual
{
    // ========================================================================
    // OVERVIEW TAB LAYOUT CONSTANTS
    // ========================================================================

    // Column width fractions for Overview tab
    const float OV_LEFT_COL_FRAC  = 0.22f;   // Key gauges
    const float OV_RIGHT_COL_FRAC = 0.30f;   // Plant status + alarms

    // Right column split: Plant Overview (top) vs Alarm Summary (bottom)
    const float OV_STATUS_FRAC = 0.55f;       // Plant overview gets top 55%
    const float OV_ALARMS_FRAC = 0.45f;       // Alarm tiles get bottom 45%

    // Scroll state for the overview status panel (separate from global _statusScroll)
    private Vector2 _overviewStatusScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawOverviewTab(Rect area)
    {
        if (engine == null) return;

        // Calculate column widths
        float leftW  = Mathf.Max(area.width * OV_LEFT_COL_FRAC, MIN_GAUGE_WIDTH);
        float rightW = area.width * OV_RIGHT_COL_FRAC;
        float centerW = area.width - leftW - rightW;
        if (centerW < MIN_GRAPH_WIDTH)
        {
            centerW = MIN_GRAPH_WIDTH;
            rightW = area.width - leftW - centerW;
        }

        // ============================================================
        // LEFT COLUMN: Key Gauges (Temperature + Pressurizer)
        // ============================================================
        Rect gaugeArea = new Rect(area.x, area.y, leftW, area.height);
        DrawOverviewGauges(gaugeArea);

        // ============================================================
        // CENTER COLUMN: Stacked Trend Graphs (Temps + Pressure)
        // ============================================================
        float centerX = area.x + leftW;
        Rect graphArea = new Rect(centerX, area.y, centerW, area.height);
        DrawStackedGraphs(graphArea, 0, 1);  // 0=TEMPS, 1=PRESSURE

        // ============================================================
        // RIGHT COLUMN: Plant Status (top) + Alarm Summary (bottom)
        // ============================================================
        float rightX = centerX + centerW;
        float statusH = area.height * OV_STATUS_FRAC;
        float alarmsH = area.height * OV_ALARMS_FRAC;

        // Plant Overview panel (scrollable if needed)
        Rect statusArea = new Rect(rightX, area.y, rightW, statusH);
        DrawOverviewStatusPanel(statusArea);

        // Alarm summary (annunciator tiles)
        Rect alarmArea = new Rect(rightX, area.y + statusH, rightW, alarmsH);
        DrawAnnunciatorContent(alarmArea);
    }

    // ========================================================================
    // OVERVIEW GAUGE COLUMN
    // Renders Temperature + Pressurizer gauge groups only (key parameters).
    // Other gauge groups (CVCS, VCT/BRS, RCP, HZP) are on their own tabs.
    // ========================================================================

    private void DrawOverviewGauges(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        GUI.Label(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH),
            "KEY INSTRUMENTS", _sectionHeaderStyle);

        // Calculate height needed for Temperature + Pressurizer groups
        float contentH = TEMP_GROUP_H + PZR_GROUP_H + 10f;
        float availH = area.height - labelH - 4f;

        Rect contentArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        if (contentH <= availH)
        {
            // Fits without scroll
            float y = contentArea.y;
            DrawTemperatureGauges(contentArea.x, ref y, contentArea.width);
            DrawPressurizerGauges(contentArea.x, ref y, contentArea.width);
        }
        else
        {
            // Scroll fallback for small screens
            _gaugeScroll = GUI.BeginScrollView(contentArea, _gaugeScroll,
                new Rect(0, 0, contentArea.width - 20f, contentH));
            float y = 0f;
            DrawTemperatureGauges(0f, ref y, contentArea.width - 20f);
            DrawPressurizerGauges(0f, ref y, contentArea.width - 20f);
            GUI.EndScrollView();
        }
    }

    // ========================================================================
    // OVERVIEW STATUS PANEL
    // Renders the Plant Overview panel + a compact bubble state summary.
    // Full system status is available on system-specific tabs.
    // ========================================================================

    private void DrawOverviewStatusPanel(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        GUI.Label(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH),
            "PLANT STATUS", _sectionHeaderStyle);

        // Content height: Plant Overview + Heater Mode + gap
        float contentH = OVERVIEW_PANEL_H + HEATER_PANEL_H + STATUS_SECTION_GAP * 2 + 20f;
        float availH = area.height - labelH - 4f;

        Rect scrollArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        if (contentH <= availH)
        {
            // Fits without scroll â€” draw directly
            float x = area.x + 4f;
            float y = area.y + labelH + 4f;
            float w = area.width - 8f;

            DrawPlantOverview(x, ref y, w);
            y += STATUS_SECTION_GAP;
            DrawHeaterModePanel(x, ref y, w);
        }
        else
        {
            // Scrollable
            _overviewStatusScroll = GUI.BeginScrollView(scrollArea, _overviewStatusScroll,
                new Rect(0, 0, area.width - 20f, contentH));

            float x = 4f;
            float y = 0f;
            float w = scrollArea.width - 28f;

            DrawPlantOverview(x, ref y, w);
            y += STATUS_SECTION_GAP;
            DrawHeaterModePanel(x, ref y, w);

            GUI.EndScrollView();
        }
    }
}

}

