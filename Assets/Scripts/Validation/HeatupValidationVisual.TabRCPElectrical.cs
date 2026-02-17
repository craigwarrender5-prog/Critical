// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab RCP/Electrical Partial)
// HeatupValidationVisual.TabRCPElectrical.cs - Tab 5: RCP / Electrical
// ============================================================================
//
// PURPOSE:
//   Renders the RCP and Electrical/HZP detail tab â€” per-pump startup status,
//   RCP heat contribution gauges, HZP stabilization state (steam dump, PID,
//   startup readiness), and HZP-specific arc gauges. Pairs with RCP Heat
//   and HZP history graphs.
//
//   Layout (2-column):
//     â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
//     â”‚ RCP STARTUP GRID      â”‚  TREND GRAPHS                â”‚
//     â”‚  (4-pump status)      â”‚  (RCP HEAT graph â€” top)       â”‚
//     â”‚â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚  (HZP graph â€” bottom)         â”‚
//     â”‚ RCP HEAT GAUGES       â”‚                              â”‚
//     â”‚  - Eff RCP Heat arc   â”‚                              â”‚
//     â”‚  - Coupling Î± bar     â”‚                              â”‚
//     â”‚  - Flow Frac bar      â”‚                              â”‚
//     â”‚  - Grid Energy bar    â”‚                              â”‚
//     â”‚â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚                              â”‚
//     â”‚ HZP GAUGES            â”‚                              â”‚
//     â”‚  - Steam Dump arc     â”‚                              â”‚
//     â”‚  - HZP Progress arc   â”‚                              â”‚
//     â”‚â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚                              â”‚
//     â”‚ HZP STABILIZATION     â”‚                              â”‚
//     â”‚  (steam dump, PID,    â”‚                              â”‚
//     â”‚   startup readiness)  â”‚                              â”‚
//     â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// READS FROM:
//   Delegates all rendering to existing partial methods:
//     - Panels partial: DrawRCPGrid(), DrawHZPPanel()
//     - Gauges partial: DrawRCPHeatGauges(), DrawHZPGauges()
//     - Graphs partial: DrawGraphContent() for RCP HEAT (5) and HZP (6)
//
// REFERENCE:
//   NRC HRTD 3.2 â€” Reactor Coolant Pump operations
//   NRC HRTD 10.2 â€” Heater control during HZP
//   NRC HRTD 11.2 â€” Steam dump system
//   NRC HRTD 19.0 â€” Plant startup operations
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawRCPElectricalTab(Rect) â€” dispatched from Core tab switch
//   Contains layout orchestration only â€” no rendering logic.
//
// GOLD STANDARD: Yes
// v5.0.0: New file â€” RCP/Electrical tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;


namespace Critical.Validation
{

public partial class HeatupValidationVisual
{
    // ========================================================================
    // RCP TAB LAYOUT CONSTANTS
    // ========================================================================

    const float RCP_LEFT_COL_FRAC = 0.35f;

    // Scroll state for RCP left column
    private Vector2 _rcpLeftScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawRCPElectricalTab(Rect area)
    {
        if (engine == null) return;

        float leftW = Mathf.Max(area.width * RCP_LEFT_COL_FRAC, MIN_GAUGE_WIDTH + 40f);
        float rightW = area.width - leftW;
        if (rightW < MIN_GRAPH_WIDTH)
        {
            rightW = MIN_GRAPH_WIDTH;
            leftW = area.width - rightW;
        }

        // LEFT COLUMN: RCP Grid + RCP Gauges + HZP Gauges + HZP Panel
        Rect leftArea = new Rect(area.x, area.y, leftW, area.height);
        DrawRCPLeftColumn(leftArea);

        // RIGHT COLUMN: Stacked Trend Graphs (RCP HEAT + HZP)
        Rect rightArea = new Rect(area.x + leftW, area.y, rightW, area.height);
        DrawStackedGraphs(rightArea, 5, 6);  // 5=RCP HEAT, 6=HZP
    }

    // ========================================================================
    // RCP LEFT COLUMN
    // ========================================================================

    private void DrawRCPLeftColumn(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        GUI.Label(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH),
            "RCP / ELECTRICAL / HZP", _sectionHeaderStyle);

        float contentH = RCP_PANEL_H + STATUS_SECTION_GAP
                         + RCP_GROUP_H + STATUS_SECTION_GAP
                         + HZP_GROUP_H + STATUS_SECTION_GAP
                         + HZP_PANEL_H + 20f;

        float availH = area.height - labelH - 4f;
        Rect contentArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        if (contentH <= availH)
        {
            DrawRCPLeftContent(contentArea.x, contentArea.y, contentArea.width);
        }
        else
        {
            _rcpLeftScroll = GUI.BeginScrollView(contentArea, _rcpLeftScroll,
                new Rect(0, 0, contentArea.width - 20f, contentH));
            DrawRCPLeftContent(0f, 0f, contentArea.width - 20f);
            GUI.EndScrollView();
        }
    }

    private void DrawRCPLeftContent(float x, float y, float w)
    {
        // RCP Startup Grid (4-pump status + aggregate)
        DrawRCPGrid(x, ref y, w);
        y += STATUS_SECTION_GAP;

        // RCP Heat Gauges (Eff RCP Heat arc, Coupling Î±, Flow Frac, Grid Energy)
        DrawRCPHeatGauges(x, ref y, w);
        y += STATUS_SECTION_GAP;

        // HZP Gauges (Steam Dump Heat arc, HZP Progress arc, bars)
        DrawHZPGauges(x, ref y, w);
        y += STATUS_SECTION_GAP;

        // HZP Stabilization Panel (steam dump, PID, startup readiness)
        DrawHZPPanel(x, ref y, w);
    }
}

}

