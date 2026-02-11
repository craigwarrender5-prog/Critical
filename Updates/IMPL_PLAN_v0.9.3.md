# Implementation Plan v0.9.3 — Dashboard Layout Reorganization

**Date:** 2026-02-07  
**Type:** Minor (UI Enhancement)  
**Priority:** MEDIUM  
**Scope:** Dashboard Layout — Full Height Columns

---

## Problem Statement

The current dashboard layout has:
- Left gauge column constrained to 62% height, requiring scrolling
- Status panels on right already scrolling
- Annunciators and Event Log taking full width at bottom unnecessarily

**User Request:**  
- Gauges on left (full height, no scroll)
- Trend Graphs top center
- Annunciators bottom center
- Status Panels most of right side
- Event Log smaller on bottom right

---

## Proposed Layout

```
┌──────────────────────────────────────────────────────────┐
│  HEADER BAR (6%)                                         │
├──────────┬────────────────────────┬──────────────────────┤
│          │                        │                      │
│ GAUGES   │    TREND GRAPHS        │   STATUS PANELS      │
│ (22%)    │    (48%)               │   (30%)              │
│          │    (~65% of height)    │   (~75% of height)   │
│ FULL     │                        │   (scrollable)       │
│ HEIGHT   ├────────────────────────┤                      │
│          │                        ├──────────────────────┤
│ NO       │    ANNUNCIATORS        │   EVENT LOG          │
│ SCROLL   │    (~35% of height)    │   (~25% of height)   │
│          │                        │   (scrollable)       │
└──────────┴────────────────────────┴──────────────────────┘
```

---

## Technical Approach

### File: `HeatupValidationVisual.cs`

**Changes:**

1. Remove `MAIN_FRAC` and `FOOTER_FRAC` constants
2. Update OnGUI to use three full-height columns
3. Center column: split between Graphs (top) and Annunciators (bottom)
4. Right column: split between Status (top) and Event Log (bottom)
5. Left column: remove scroll view, draw gauges directly

---

## Detailed Code Changes

### Constants Update

```csharp
// REMOVE:
// const float MAIN_FRAC = 0.62f;
// const float FOOTER_FRAC = 0.32f;

// ADD: Split ratios for center and right columns
const float CENTER_GRAPH_FRAC = 0.65f;    // Graphs get 65% of center column
const float CENTER_ANN_FRAC = 0.35f;      // Annunciators get 35% of center column
const float RIGHT_STATUS_FRAC = 0.75f;    // Status panels get 75% of right column
const float RIGHT_LOG_FRAC = 0.25f;       // Event log gets 25% of right column
```

### Updated OnGUI Layout

```csharp
void OnGUI()
{
    if (!dashboardVisible || engine == null) return;

    _sw = Screen.width;
    _sh = Screen.height;

    if (!_stylesInitialized)
        InitializeStyles();

    // Full-screen background
    GUI.DrawTexture(new Rect(0, 0, _sw, _sh), _bgTex, ScaleMode.StretchToFill);

    // Header
    float headerH = Mathf.Max(_sh * HEADER_FRAC, 40f);
    Rect headerRect = new Rect(0, 0, _sw, headerH);
    DrawHeaderBar(headerRect);

    // ================================================================
    // MAIN AREA — 3 columns, FULL HEIGHT
    // v0.9.3: No footer, columns extend to bottom of screen
    // ================================================================
    float mainY = headerH;
    float mainH = _sh - headerH;

    float leftW  = Mathf.Max(_sw * LEFT_COL_FRAC, MIN_GAUGE_WIDTH);
    float rightW = _sw * RIGHT_COL_FRAC;
    float centerW = _sw - leftW - rightW;
    if (centerW < MIN_GRAPH_WIDTH)
    {
        centerW = MIN_GRAPH_WIDTH;
        rightW = _sw - leftW - centerW;
    }

    // LEFT: Gauge panels (full height, no scroll)
    Rect gaugeRect = new Rect(0, mainY, leftW, mainH);
    DrawGaugeColumn(gaugeRect);

    // CENTER: Trend graphs (top) + Annunciators (bottom)
    Rect centerRect = new Rect(leftW, mainY, centerW, mainH);
    DrawCenterColumn(centerRect);

    // RIGHT: Status panels (top) + Event log (bottom)
    Rect rightRect = new Rect(leftW + centerW, mainY, rightW, mainH);
    DrawRightColumn(rightRect);
}
```

### New DrawCenterColumn Method

```csharp
/// <summary>
/// v0.9.3: Draw the center column with two stacked sections:
///   - Trend Graphs (top ~65%)
///   - Annunciator Tiles (bottom ~35%)
/// </summary>
void DrawCenterColumn(Rect area)
{
    float graphH = area.height * CENTER_GRAPH_FRAC;
    float annH = area.height - graphH;

    // Top: Trend Graphs
    Rect graphRect = new Rect(area.x, area.y, area.width, graphH);
    DrawGraphArea(graphRect);

    // Bottom: Annunciators
    Rect annRect = new Rect(area.x, area.y + graphH, area.width, annH);
    GUI.Box(annRect, GUIContent.none, _panelBgStyle);
    DrawAnnunciatorContent(annRect);
}
```

### New DrawRightColumn Method

```csharp
/// <summary>
/// v0.9.3: Draw the right column with two stacked sections:
///   - Status Panels (top ~75%)
///   - Event Log (bottom ~25%)
/// </summary>
void DrawRightColumn(Rect area)
{
    float statusH = area.height * RIGHT_STATUS_FRAC;
    float logH = area.height - statusH;

    // Top: Status Panels (scrollable)
    Rect statusRect = new Rect(area.x, area.y, area.width, statusH);
    DrawStatusColumn(statusRect);

    // Bottom: Event Log (scrollable)
    Rect logRect = new Rect(area.x, area.y + statusH, area.width, logH);
    GUI.Box(logRect, GUIContent.none, _panelBgStyle);
    DrawEventLogContent(logRect);
}
```

### Updated DrawGaugeColumn (No Scroll)

```csharp
void DrawGaugeColumn(Rect area)
{
    GUI.Box(area, GUIContent.none, _panelBgStyle);

    // Column header
    float labelH = 22f;
    Rect labelRect = new Rect(area.x + 4f, area.y + 2f, area.width - 8f, labelH);
    GUI.Label(labelRect, "INSTRUMENTATION", _sectionHeaderStyle);

    // v0.9.3: Direct content draw (no scroll with full height)
    // Fallback to scroll if content is taller than available (small screens)
    float contentH = GetGaugeContentHeight();
    float availableH = area.height - labelH - 4f;

    if (contentH <= availableH)
    {
        // Content fits - draw directly
        Rect contentRect = new Rect(area.x, area.y + labelH + 2f, 
            area.width, availableH);
        DrawGaugeColumnContent(contentRect);
    }
    else
    {
        // Content too tall - use scroll (fallback)
        Rect scrollOuter = new Rect(area.x, area.y + labelH + 2f,
            area.width, availableH);

        _gaugeScroll = GUI.BeginScrollView(scrollOuter, _gaugeScroll,
            new Rect(0, 0, area.width - 20f, contentH));

        Rect contentRect = new Rect(0, 0, scrollOuter.width - 20f, contentH);
        DrawGaugeColumnContent(contentRect);

        GUI.EndScrollView();
    }
}
```

### Remove DrawFooter

Delete the `DrawFooter()` method entirely - it's no longer used.

---

## Height Calculations

### At 1080p (typical)

```
Screen height:       1080px
Header (6%):          65px
Available:          1015px

LEFT (Gauges):      1015px available, ~888px content → fits!
CENTER Graph:        660px (65% of 1015)
CENTER Ann:          355px (35% of 1015)
RIGHT Status:        761px (75% of 1015)
RIGHT Log:           254px (25% of 1015)
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `HeatupValidationVisual.cs` | Layout restructure, add DrawCenterColumn, DrawRightColumn, update DrawGaugeColumn, remove DrawFooter |

---

## Visual Mockup

```
┌────────────────────────────────────────────────────────────────┐
│  MODE 5  │  SOLID PZR  │  SIM: 00:01:00  │  WARP: [1×]        │
├──────────┬────────────────────────────────┬────────────────────┤
│ TEMP     │                                │ PLANT OVERVIEW     │
│ T-AVG    │  [TEMPS] [PRES] [CVCS] [VCT]   │ MODE 5, PHASE...   │
│ T-HOT    │                                │                    │
│ T-COLD   │                                │ RCP STARTUP STATUS │
├──────────┤      TREND GRAPH AREA          │ RCP #1-4: OFF      │
│ PRESS    │                                │                    │
│ RCS PRES │                                │ BUBBLE FORMATION   │
│ PZR LVL  │                                │ 0. NONE ←          │
├──────────┤                                │                    │
│ CVCS     │                                │ PZR HEATER CONTROL │
│ CHARGE   ├────────────────────────────────┤                    │
│ LETDOWN  │                                │ CVCS CONTROLLER    │
├──────────┤     ANNUNCIATOR PANEL          │                    │
│ VCT&BRS  │                                │ INVENTORY TRACKING │
│ VCT LVL  │  [PZR HTRS] [HEATUP] [BUBBLE]  ├────────────────────┤
│ BRS HU   │  [RCS FLOW] [CCW   ] [CHARGE]  │ OPERATIONS LOG     │
│ BRS FLOW │  [SEAL INJ] [VCT LO] [LETDN ]  │ [00:00] INF Init   │
├──────────┤  [RCP #1  ] [RCP #2] [RCP #3]  │ [00:01] ACT Heat   │
│ RCP/HEAT │                                │ [00:02] INF Warp   │
│ RCP HEAT │                                │                    │
└──────────┴────────────────────────────────┴────────────────────┘
```

---

## Validation Criteria

| Test | Expected Result |
|------|-----------------|
| All gauge groups visible | No scrolling required on left column |
| Trend graphs visible | Top of center column with tabs |
| Annunciators visible | Bottom of center column |
| Status panels visible | Top of right column (scrollable) |
| Event log visible | Bottom of right column (scrollable) |
| Small screen fallback | Scroll appears on gauge column if needed |

---

## Approval

**Status:** AWAITING USER APPROVAL

Confirm to proceed with implementation.
