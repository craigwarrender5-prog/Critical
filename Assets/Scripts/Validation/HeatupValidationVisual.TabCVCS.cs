// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab CVCS Partial)
// HeatupValidationVisual.TabCVCS.cs - Tab 3: CVCS / Inventory
// ============================================================================
//
// PURPOSE:
//   Renders the CVCS and Inventory detail tab — comprehensive view of all
//   Chemical and Volume Control System flows, liquid inventory (VCT/BRS),
//   and system mass conservation tracking. Pairs CVCS and inventory gauges
//   with the most relevant trend graphs (CVCS flows + VCT/BRS levels).
//
//   Layout (2-column):
//     ┌───────────────────────┬──────────────────────────────┐
//     │ CVCS FLOW GAUGES      │  TREND GRAPHS                │
//     │  - Charging arc       │  (CVCS graph — top)           │
//     │  - Letdown arc        │  (VCT/BRS graph — bottom)     │
//     │  - Surge bar          │                              │
//     │  - Net CVCS bar       │                              │
//     │  - Seal Inj bar       │                              │
//     │  - LD Path indicator  │                              │
//     │─────────────────────  │                              │
//     │ VCT/BRS GAUGES        │                              │
//     │  - VCT Level arc      │                              │
//     │  - BRS Holdup arc     │                              │
//     │  - BRS Flow bidir     │                              │
//     │  - VCT Boron bar      │                              │
//     │─────────────────────  │                              │
//     │ INVENTORY PANEL       │                              │
//     └───────────────────────┴──────────────────────────────┘
//
// READS FROM:
//   Delegates all rendering to existing partial methods:
//     - Gauges partial: DrawCVCSFlowGauges(), DrawLiquidInventoryGauges()
//     - Graphs partial: DrawGraphContent() for CVCS (2) and VCT/BRS (3)
//     - Panels partial: DrawInventoryPanel()
//
// REFERENCE:
//   NRC HRTD 4.1 — Chemical and Volume Control System
//   NRC HRTD 4.1.2 — VCT level control and BRS operations
//   Westinghouse 4-Loop PWR FSAR Chapter 9.3 — CVCS
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawCVCSTab(Rect) — dispatched from Core tab switch
//   Contains layout orchestration only — no rendering logic.
//
// GOLD STANDARD: Yes
// v5.0.0: New file — CVCS/Inventory tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // CVCS TAB LAYOUT CONSTANTS
    // ========================================================================

    const float CVCS_LEFT_COL_FRAC = 0.35f;

    // Scroll state for CVCS left column
    private Vector2 _cvcsLeftScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawCVCSTab(Rect area)
    {
        if (engine == null) return;

        float leftW = Mathf.Max(area.width * CVCS_LEFT_COL_FRAC, MIN_GAUGE_WIDTH + 40f);
        float rightW = area.width - leftW;
        if (rightW < MIN_GRAPH_WIDTH)
        {
            rightW = MIN_GRAPH_WIDTH;
            leftW = area.width - rightW;
        }

        // LEFT COLUMN: CVCS Gauges + VCT/BRS Gauges + Inventory Panel
        Rect leftArea = new Rect(area.x, area.y, leftW, area.height);
        DrawCVCSLeftColumn(leftArea);

        // RIGHT COLUMN: Stacked Trend Graphs (CVCS + VCT/BRS)
        Rect rightArea = new Rect(area.x + leftW, area.y, rightW, area.height);
        DrawStackedGraphs(rightArea, 2, 3);  // 2=CVCS, 3=VCT/BRS
    }

    // ========================================================================
    // CVCS LEFT COLUMN
    // CVCS flow gauges, VCT/BRS liquid inventory gauges, system inventory panel.
    // ========================================================================

    private void DrawCVCSLeftColumn(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        GUI.Label(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH),
            "CVCS / INVENTORY DETAIL", _sectionHeaderStyle);

        float contentH = CVCS_GROUP_H + LIQUID_INV_GROUP_H + INVENTORY_PANEL_H
                         + STATUS_SECTION_GAP + 20f;

        float availH = area.height - labelH - 4f;
        Rect contentArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        if (contentH <= availH)
        {
            DrawCVCSLeftContent(contentArea.x, contentArea.y, contentArea.width);
        }
        else
        {
            _cvcsLeftScroll = GUI.BeginScrollView(contentArea, _cvcsLeftScroll,
                new Rect(0, 0, contentArea.width - 20f, contentH));
            DrawCVCSLeftContent(0f, 0f, contentArea.width - 20f);
            GUI.EndScrollView();
        }
    }

    private void DrawCVCSLeftContent(float x, float y, float w)
    {
        // CVCS Flow Gauges (Charging, Letdown, Surge, Net CVCS, Seal Inj, LD Path)
        DrawCVCSFlowGauges(x, ref y, w);

        // VCT/BRS Liquid Inventory Gauges (VCT Level, BRS Holdup, BRS Flow bidir)
        DrawLiquidInventoryGauges(x, ref y, w);

        // System Inventory Conservation Panel
        DrawInventoryPanel(x, ref y, w);
    }
}
