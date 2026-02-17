// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab SG/RHR Partial)
// HeatupValidationVisual.TabSGRHR.cs - Tab 4: Steam Generators / RHR
// ============================================================================
//
// PURPOSE:
//   Renders the Steam Generator and Residual Heat Removal detail tab.
//   Combines the SG/RHR thermal balance status panel with dedicated SG
//   arc gauges (secondary pressure, heat transfer) and the TEMPS history
//   graph which includes T_SG_SEC and T_SAT traces.
//
//   Layout (2-column):
//     â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
//     â”‚ SG ARC GAUGES         â”‚  TREND GRAPHS                â”‚
//     â”‚  - SG Pressure arc    â”‚  (TEMPS graph â€” top)          â”‚
//     â”‚  - SG Heat Xfer arc   â”‚  (RATES graph â€” bottom)       â”‚
//     â”‚â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚                              â”‚
//     â”‚ SG/RHR STATUS PANEL   â”‚                              â”‚
//     â”‚  (full DrawSGRHRPanel)â”‚                              â”‚
//     â”‚  - SG secondary state â”‚                              â”‚
//     â”‚  - Boiling/N2/superheatâ”‚                             â”‚
//     â”‚  - RHR mode/HX/pump   â”‚                              â”‚
//     â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// READS FROM:
//   Delegates rendering to existing partial methods:
//     - Panels partial: DrawSGRHRPanel()
//     - Graphs partial: DrawGraphContent() for TEMPS (0) and RATES (4)
//   Plus new inline arc gauges using DrawGaugeArc() from Gauges partial.
//
// REFERENCE:
//   NRC HRTD 5.1 â€” Steam Generator design and operation
//   NRC HRTD 19.2.2 â€” SG secondary side during heatup
//   NRC HRTD 11.2 â€” Residual Heat Removal System
//   Westinghouse 4-Loop PWR FSAR Chapter 5.4 â€” SGs, Chapter 5.5 â€” RHR
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawSGRHRTab(Rect) â€” dispatched from Core tab switch
//   Contains layout orchestration + 2 new arc gauges (SG-specific).
//
// GOLD STANDARD: Yes
// v5.0.0: New file â€” SG/RHR tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;
using Critical.Physics;


namespace Critical.Validation
{

public partial class HeatupValidationVisual
{
    // ========================================================================
    // SG/RHR TAB LAYOUT CONSTANTS
    // ========================================================================

    const float SGRHR_LEFT_COL_FRAC = 0.35f;

    // Height for the SG gauges section (header + 1 arc row + 2 mini bars + gap)
    const float SG_GAUGES_H = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 2 + GAUGE_GROUP_GAP;

    // Scroll state for SG/RHR left column
    private Vector2 _sgRhrLeftScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawSGRHRTab(Rect area)
    {
        if (engine == null) return;

        float leftW = Mathf.Max(area.width * SGRHR_LEFT_COL_FRAC, MIN_GAUGE_WIDTH + 40f);
        float rightW = area.width - leftW;
        if (rightW < MIN_GRAPH_WIDTH)
        {
            rightW = MIN_GRAPH_WIDTH;
            leftW = area.width - rightW;
        }

        // LEFT COLUMN: SG Gauges + SG/RHR Status Panel
        Rect leftArea = new Rect(area.x, area.y, leftW, area.height);
        DrawSGRHRLeftColumn(leftArea);

        // RIGHT COLUMN: Stacked Trend Graphs (TEMPS + RATES)
        // TEMPS has T_RCS, T_SG_SEC, T_SAT traces; RATES has heatup/pressure rates
        Rect rightArea = new Rect(area.x + leftW, area.y, rightW, area.height);
        DrawStackedGraphs(rightArea, 0, 4);  // 0=TEMPS, 4=RATES
    }

    // ========================================================================
    // SG/RHR LEFT COLUMN
    // ========================================================================

    private void DrawSGRHRLeftColumn(Rect area)
    {
        GUI.Box(area, GUIContent.none, _panelBgStyle);

        float labelH = 22f;
        GUI.Label(new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH),
            "SG / RHR DETAIL", _sectionHeaderStyle);

        float contentH = SG_GAUGES_H + SG_RHR_PANEL_H + STATUS_SECTION_GAP + 20f;

        float availH = area.height - labelH - 4f;
        Rect contentArea = new Rect(area.x, area.y + labelH + 2f, area.width, availH);

        if (contentH <= availH)
        {
            DrawSGRHRLeftContent(contentArea.x, contentArea.y, contentArea.width);
        }
        else
        {
            _sgRhrLeftScroll = GUI.BeginScrollView(contentArea, _sgRhrLeftScroll,
                new Rect(0, 0, contentArea.width - 20f, contentH));
            DrawSGRHRLeftContent(0f, 0f, contentArea.width - 20f);
            GUI.EndScrollView();
        }
    }

    private void DrawSGRHRLeftContent(float x, float y, float w)
    {
        // SG-specific arc gauges (new for v5.0.0)
        DrawSGTabGauges(x, ref y, w);

        // Full SG/RHR thermal balance panel (existing from Panels partial)
        DrawSGRHRPanel(x, ref y, w);
    }

    // ========================================================================
    // SG TAB GAUGES â€” SG Secondary Pressure + SG Heat Transfer
    //
    // These arc gauges provide at-a-glance SG status that complements
    // the detailed text in DrawSGRHRPanel().
    //
    // SG Secondary Pressure: 0â€“1100 psig range
    //   Per NRC HRTD 5.1: SG secondary pressure rises from atmospheric
    //   during heatup as primary heats the secondary side through the
    //   U-tubes. At HZP, SG pressure reaches ~1000-1050 psig.
    //
    // SG Heat Transfer: 0â€“25 MW range
    //   Total heat transfer from primary to SG secondaries via natural
    //   circulation and forced flow (with RCPs). During heatup the SGs
    //   absorb thermal energy as a heat sink.
    // ========================================================================

    private void DrawSGTabGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "SG INSTRUMENTS");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;
        float cell2W = w / 2f;

        // ROW: SG Pressure (left), SG Heat Transfer (right)
        {
            float rowY = y;

            // SG Secondary Pressure (psig)
            float sgP_psig = engine.sgSecondaryPressure_psia - 14.7f;
            Color sgPC = sgP_psig > 1050f ? _cNormalGreen :
                         sgP_psig > 100f ? _cCyanInfo : _cTextSecondary;
            DrawGaugeArc(
                new Vector2(x + cell2W * 0.5f, rowY + arcR + 14f), arcR,
                sgP_psig, 0f, 1100f, sgPC,
                "SG PRESS", $"{sgP_psig:F0}", "psig");

            // SG Heat Transfer (MW)
            float sgQ = engine.sgHeatTransfer_MW;
            Color sgQC = sgQ > 0.1f ? _cOrangeAccent : _cTextSecondary;
            DrawGaugeArc(
                new Vector2(x + cell2W * 1.5f, rowY + arcR + 14f), arcR,
                sgQ, 0f, 25f, sgQC,
                "SG Q", $"{sgQ:F2}", "MW");

            y += GAUGE_ROW_H;
        }

        // Mini bars: RCS-SG Î”T and Superheat
        {
            float barH = 18f;

            // RCS-SG Delta T (thermal coupling indicator)
            float deltaTsg = engine.T_rcs - engine.T_sg_secondary;
            Color sgDtC;
            if (engine.rcpCount == 0)
                sgDtC = _cTextSecondary;
            else if (deltaTsg > 30f || deltaTsg < 5f)
                sgDtC = _cWarningAmber;
            else
                sgDtC = _cCyanInfo;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "RCS-SG Î”T", deltaTsg, 0f, 50f, sgDtC, "F1", "Â°F");
            y += barH + 2f;

            // SG Superheat (Â°F above T_sat)
            Color shC = engine.sgMaxSuperheat_F > 10f ? _cOrangeAccent :
                        engine.sgMaxSuperheat_F > 0f ? _cWarningAmber : _cTextSecondary;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "SUPERHEAT", engine.sgMaxSuperheat_F, 0f, 50f, shC, "F1", "Â°F");
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }
}

}

