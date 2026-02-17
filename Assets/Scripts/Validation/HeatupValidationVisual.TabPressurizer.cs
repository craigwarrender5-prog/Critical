// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab Pressurizer Partial)
// HeatupValidationVisual.TabPressurizer.cs - Tab 2: Pressurizer
// ============================================================================
//
// PURPOSE:
//   Renders the Pressurizer system detail tab â€” comprehensive view of all
//   pressurizer-related parameters, heater control, bubble formation state,
//   and surge flow dynamics. Pairs PZR gauges and status panels with the
//   most relevant trend graphs (Pressure + CVCS).
//
//   Layout (2-column):
//     â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
//     â”‚ PZR GAUGES            â”‚  TREND GRAPHS                â”‚
//     â”‚  - RCS Pressure arc   â”‚  (PRESSURE graph â€” top)      â”‚
//     â”‚  - PZR Level arc      â”‚  (CVCS graph â€” bottom)       â”‚
//     â”‚  - Heater Power bar   â”‚                              â”‚
//     â”‚  - Press Rate bar     â”‚                              â”‚
//     â”‚  - Level Setpt bar    â”‚                              â”‚
//     â”‚  - Surge Flow bidir   â”‚                              â”‚
//     â”‚â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚                              â”‚
//     â”‚ HEATER CONTROL PANEL  â”‚                              â”‚
//     â”‚â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚                              â”‚
//     â”‚ BUBBLE STATE PANEL    â”‚                              â”‚
//     â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// READS FROM:
//   Delegates all rendering to existing partial methods:
//     - Gauges partial: DrawPressurizerGauges(), DrawGaugeArcBidirectional()
//     - Graphs partial: DrawGraphContent() for PRESSURE (1) and CVCS (2)
//     - Panels partial: DrawHeaterModePanel(), DrawBubbleStatePanel()
//
// REFERENCE:
//   NRC HRTD 6.1 â€” Pressurizer design and operation
//   NRC HRTD 10.2 â€” Pressurizer heater control
//   NRC HRTD 19.2.2 â€” Bubble formation procedure
//   Westinghouse 4-Loop PWR FSAR Chapter 5.4 â€” Pressurizer
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawPressurizerTab(Rect) â€” dispatched from Core tab switch
//   Contains layout orchestration only â€” no rendering logic.
//
// GOLD STANDARD: Yes
// v5.0.0: New file â€” Pressurizer tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;
using Critical.Physics;


namespace Critical.Validation
{

public partial class HeatupValidationVisual
{
    // ========================================================================
    // PRESSURIZER TAB LAYOUT CONSTANTS
    // ========================================================================

    // Left column width fraction
    const float PZR_LEFT_COL_FRAC = 0.35f;

    // Scroll state for PZR left column (separate from other tabs)
    private Vector2 _pzrLeftScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawPressurizerTab(Rect area)
    {
        if (engine == null) return;

        // Calculate column widths
        float leftW = Mathf.Max(area.width * PZR_LEFT_COL_FRAC, MIN_GAUGE_WIDTH + 40f);
        float rightW = area.width - leftW;
        if (rightW < MIN_GRAPH_WIDTH)
        {
            rightW = MIN_GRAPH_WIDTH;
            leftW = area.width - rightW;
        }

        // ============================================================
        // LEFT COLUMN: PZR Gauges + Surge Flow + Heater + Bubble State
        // ============================================================
        Rect leftArea = new Rect(area.x, area.y, leftW, area.height);
        DrawPZRLeftColumn(leftArea);

        // ============================================================
        // RIGHT COLUMN: Stacked Trend Graphs (Pressure + CVCS)
        // ============================================================
        Rect rightArea = new Rect(area.x + leftW, area.y, rightW, area.height);
        DrawStackedGraphs(rightArea, 1, 2);  // 1=PRESSURE, 2=CVCS
    }

    // ========================================================================
    // PZR LEFT COLUMN
    // PZR gauges, bidirectional surge flow, heater control, bubble formation.
    // Scrollable if content exceeds available height.
    // ========================================================================

    private void DrawPZRLeftColumn(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        GUI.Label(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH),
            "PRESSURIZER DETAIL", _sectionHeaderStyle);

        // Calculate total content height
        float surgeGaugeH = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + GAUGE_GROUP_GAP;
        float contentH = PZR_GROUP_H + surgeGaugeH
                         + HEATER_PANEL_H + STATUS_SECTION_GAP
                         + BUBBLE_PANEL_H + STATUS_SECTION_GAP + 20f;

        float availH = area.height - labelH - 4f;
        Rect contentArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        if (contentH <= availH)
        {
            // Fits without scroll
            DrawPZRLeftContent(contentArea.x, contentArea.y, contentArea.width);
        }
        else
        {
            // Scrollable
            _pzrLeftScroll = GUI.BeginScrollView(contentArea, _pzrLeftScroll,
                new Rect(0, 0, contentArea.width - 20f, contentH));
            DrawPZRLeftContent(0f, 0f, contentArea.width - 20f);
            GUI.EndScrollView();
        }
    }

    /// <summary>
    /// Render all PZR left column content at the given origin.
    /// Called either directly or inside a scroll view.
    /// </summary>
    private void DrawPZRLeftContent(float x, float y, float w)
    {
        // PZR Gauge Group (Pressure arc, Level arc, Heater/Rate/Setpt bars)
        DrawPressurizerGauges(x, ref y, w);

        // Surge Flow â€” Bidirectional arc gauge (from CVCS group, shown here
        // because surge flow is operationally a PZR parameter)
        DrawPZRSurgeGauge(x, ref y, w);

        // Heater Control Panel
        DrawHeaterModePanel(x, ref y, w);
        y += STATUS_SECTION_GAP;

        // Bubble Formation State Panel
        DrawBubbleStatePanel(x, ref y, w);
    }

    // ========================================================================
    // PZR SURGE FLOW GAUGE
    // Bidirectional arc gauge showing insurge (negative) / outsurge (positive).
    // Surge flow is the net flow into/out of the pressurizer via the surge line.
    // Per NRC HRTD 6.1: Outsurge occurs when RCS heats up (thermal expansion),
    // insurge occurs during cooldown or pressure control spray activation.
    // ========================================================================

    private void DrawPZRSurgeGauge(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "SURGE LINE FLOW");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;

        // Bidirectional surge flow gauge (centered)
        float rowY = y;
        DrawGaugeArcBidirectional(
            new Vector2(x + w * 0.5f, rowY + arcR + 14f), arcR,
            engine.surgeFlow, -50f, 50f,
            _cOrangeAccent,   // Positive (outsurge) â€” expansion, heatup
            _cBlueAccent,     // Negative (insurge) â€” contraction, spray
            "SURGE", engine.surgeFlow, "gpm");

        y += GAUGE_ROW_H;
        y += GAUGE_GROUP_GAP;
    }
}

}

