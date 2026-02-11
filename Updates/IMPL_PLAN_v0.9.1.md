# Implementation Plan v0.9.1 — Dashboard BRS Display Enhancement

**Date:** 2026-02-07  
**Type:** Patch (UI Enhancement)  
**Priority:** LOW  
**Scope:** Heatup Dashboard — Left Column Gauge Layout

---

## Problem Statement

Currently, the heatup validation dashboard displays the **VCT (Volume Control Tank)** and **BRS (Boron Recycle System)** as separate gauge groups, each with their own arc gauge:

- **VCT Group**: VCT Level arc gauge (centered) + 3 mini bars below
- **BRS Group**: BRS Holdup arc gauge (centered) + 3 mini bars below

This layout consumes vertical space unnecessarily and does not provide a unified view of the CVCS liquid waste processing chain. The operator cannot see VCT and BRS levels at a glance together.

**User Request:**  
Add a BRS arc gauge **next to** the VCT arc gauge (side-by-side), plus a **third bidirectional arc gauge** showing BRS flow direction and magnitude.

---

## Physics Clarification: BRS Flow Direction

After examining `HeatupSimEngine.CVCS.cs`, the BRS inflow and outflow are **mutually exclusive**:

| Condition | BRS Flow Direction |
|-----------|-------------------|
| VCT Level > 70% (Divert Setpoint) | **Inflow** to BRS holdup tanks via LCV-112A |
| VCT Level < 27% (Makeup Setpoint) AND BRS distillate available | **Outflow** from BRS distillate to VCT |
| VCT Level 27–70% | **No flow** — BRS idle or processing only |

Since flow is never bidirectional simultaneously, a **center-zero bidirectional arc gauge** is ideal — needle at 12 o'clock for zero, deflecting right for inflow, left for outflow.

---

## Expected Outcome

A consolidated **VCT & BRS — LIQUID INVENTORY** gauge group displaying:

1. **Three arc gauges in a row:**
   - **VCT Level** (left) — 0–100% with band coloring
   - **BRS Holdup** (center) — 0–100% with processing status coloring
   - **BRS Flow** (right) — Bidirectional gauge, center-zero, ±40 gpm range

2. **Mini bars below:**
   - VCT BORON (ppm)
   - MASS CONS (gal) — Conservation error tracking

3. **Removal of separate VCT and BRS groups**

---

## Visual Design: Bidirectional Arc Gauge

```
           BRS FLOW
        ← Out    In →
           
    -40  -20  0  +20  +40
      ╲   ╲  │  ╱   ╱
       ╲   ╲ │ ╱   ╱
        ╲   ╲│╱   ╱
         ────●────      ← needle at 0 (no flow)
              
    BLUE zone   ORANGE zone
    (outflow)   (inflow)
```

- **Center (12 o'clock)**: 0 gpm — no flow
- **Right deflection**: Positive flow (inflow to BRS from VCT divert) — Orange
- **Left deflection**: Negative flow (outflow from BRS to VCT/plant) — Blue
- **At zero**: Gray/neutral color

---

## Proposed Layout

```
┌─────────────────────────────────────────────────┐
│ VCT & BRS — LIQUID INVENTORY                    │
├─────────────────────────────────────────────────┤
│   ┌────────┐    ┌────────┐    ┌────────┐        │
│   │VCT LVL │    │BRS HU  │    │BRS FLOW│        │  ← Three arcs
│   │ 62.6%  │    │ 15.2%  │    │ +5.0   │        │     (third is bidirectional)
│   └────────┘    └────────┘    └────────┘        │
│ VCT BORON     ████████████████████    2000 ppm  │
│ MASS CONS     ██                       2.1 gal  │
└─────────────────────────────────────────────────┘
```

---

## Technical Approach

### Stage 1: Add Bidirectional Arc Gauge Drawing Function

**File:** `HeatupValidationVisual.Gauges.cs`

Create a new `DrawGaugeArcBidirectional()` method that:
- Takes a center-zero range (e.g., -40 to +40)
- Draws needle at 12 o'clock for zero value
- Deflects right for positive values, left for negative
- Colors the arc/needle based on direction (orange for positive, blue for negative, gray for zero)

### Stage 2: Create Combined Liquid Inventory Group

**File:** `HeatupValidationVisual.Gauges.cs`

- Create new `DrawLiquidInventoryGauges()` method
- Three arc gauges in a row (w/3 cell width each)
- Two mini bars below
- Remove old `DrawVCTGauges()` and `DrawBRSGauges()` methods

### Stage 3: Update Layout Constants and Call Sequence

**File:** `HeatupValidationVisual.Gauges.cs`

- Update height constants
- Update `DrawGaugeColumnContent()` to call new method
- Update `TOTAL_GAUGE_H`

---

## Detailed Code Changes

### Stage 1: New Bidirectional Arc Gauge Function

```csharp
/// <summary>
/// Draw a bidirectional arc gauge with center-zero.
/// Needle at 12 o'clock = zero, right = positive, left = negative.
/// </summary>
/// <param name="center">Center position of the gauge</param>
/// <param name="radius">Radius of the arc</param>
/// <param name="value">Current value (can be negative)</param>
/// <param name="minValue">Minimum value (negative, e.g., -40)</param>
/// <param name="maxValue">Maximum value (positive, e.g., +40)</param>
/// <param name="positiveColor">Color for positive values</param>
/// <param name="negativeColor">Color for negative values</param>
/// <param name="label">Gauge label text</param>
/// <param name="valueText">Formatted value text</param>
/// <param name="units">Units text</param>
void DrawGaugeArcBidirectional(Vector2 center, float radius, float value,
    float minValue, float maxValue, Color positiveColor, Color negativeColor,
    string label, string valueText, string units)
{
    // Arc sweep: 135° (bottom-left) to 45° (bottom-right) = 270° total sweep
    // Center (zero) is at 90° (12 o'clock / straight up)
    
    float arcStartAngle = 225f;  // Bottom-left (for min negative value)
    float arcEndAngle = -45f;    // Bottom-right (for max positive value)
    float arcSweep = 270f;
    
    // Background arc (dark)
    DrawArcBackground(center, radius, arcStartAngle, arcSweep);
    
    // Determine needle angle
    // Zero maps to 90° (straight up)
    // minValue maps to 225° (left side)
    // maxValue maps to -45° (right side)
    float normalizedValue = Mathf.InverseLerp(minValue, maxValue, value);
    float needleAngle = Mathf.Lerp(arcStartAngle, arcEndAngle, normalizedValue);
    
    // Determine color based on value sign
    Color needleColor;
    if (value > 0.1f)
        needleColor = positiveColor;
    else if (value < -0.1f)
        needleColor = negativeColor;
    else
        needleColor = _cTextSecondary;  // Gray for zero/near-zero
    
    // Draw needle
    DrawNeedle(center, radius * 0.85f, needleAngle, needleColor);
    
    // Draw center pivot
    DrawPivot(center, 4f, needleColor);
    
    // Draw tick marks at key positions
    DrawBidirectionalTicks(center, radius, minValue, maxValue);
    
    // Draw labels
    DrawGaugeLabel(center, radius, label, valueText, units, needleColor);
}
```

### Stage 2: New Combined Liquid Inventory Group

```csharp
// ========================================================================
// GROUP: VCT & BRS — LIQUID INVENTORY (Combined)
//   Arc gauges: VCT Level, BRS Holdup, BRS Flow (bidirectional)
//   Mini bars: VCT Boron, Mass Conservation Error
// ========================================================================

void DrawLiquidInventoryGauges(float x, ref float y, float w)
{
    DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H),
        "VCT & BRS — LIQUID INVENTORY");
    y += GAUGE_GROUP_HEADER_H;

    float arcR = GAUGE_ARC_SIZE / 2f;
    float cell3W = w / 3f;

    // ROW: VCT Level (left), BRS Holdup (center), BRS Flow (right)
    {
        float rowY = y;

        // VCT Level — band-based coloring per NRC HRTD 4.1 setpoints
        float vl = engine.vctState.Level_percent;
        Color vlC;
        if (vl < PlantConstants.VCT_LEVEL_LOW_LOW || vl > PlantConstants.VCT_LEVEL_HIGH_HIGH)
            vlC = _cAlarmRed;
        else if (vl < PlantConstants.VCT_LEVEL_LOW || vl > PlantConstants.VCT_LEVEL_HIGH)
            vlC = _cWarningAmber;
        else if (vl >= PlantConstants.VCT_LEVEL_NORMAL_LOW && vl <= PlantConstants.VCT_LEVEL_NORMAL_HIGH)
            vlC = _cNormalGreen;
        else
            vlC = _cCyanInfo;

        DrawGaugeArc(
            new Vector2(x + cell3W * 0.5f, rowY + arcR + 14f), arcR,
            vl, 0f, 100f, vlC,
            "VCT LEVEL", $"{vl:F1}", "%");

        // BRS Holdup Level
        float holdupPct = BRSPhysics.GetHoldupLevelPercent(engine.brsState);
        Color huC;
        if (holdupPct > PlantConstants.BRS_HOLDUP_HIGH_LEVEL_PCT)
            huC = _cWarningAmber;
        else if (holdupPct < PlantConstants.BRS_HOLDUP_LOW_LEVEL_PCT && engine.brsState.ProcessingActive)
            huC = _cWarningAmber;
        else if (engine.brsState.ProcessingActive)
            huC = _cOrangeAccent;
        else
            huC = _cCyanInfo;

        DrawGaugeArc(
            new Vector2(x + cell3W * 1.5f, rowY + arcR + 14f), arcR,
            holdupPct, 0f, 100f, huC,
            "BRS HOLDUP", $"{holdupPct:F1}", "%");

        // BRS Flow — Bidirectional (center-zero)
        // Positive = inflow to BRS (divert), Negative = outflow from BRS (return)
        float brsNetFlow = engine.brsState.InFlow_gpm - engine.brsState.ReturnFlow_gpm;
        
        DrawGaugeArcBidirectional(
            new Vector2(x + cell3W * 2.5f, rowY + arcR + 14f), arcR,
            brsNetFlow, -40f, 40f,
            _cOrangeAccent,  // Positive (inflow) color
            _cBlueAccent,    // Negative (outflow) color
            "BRS FLOW", $"{brsNetFlow:+0.0;-0.0;0.0}", "gpm");

        y += GAUGE_ROW_H;
    }

    // Mini bars
    {
        float barH = 18f;

        // VCT Boron concentration
        DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
            "VCT BORON", engine.vctState.BoronConcentration_ppm, 0f, 3000f,
            _cCyanInfo, "F0", "ppm");
        y += barH + 2f;

        // Mass Conservation Error
        Color consC = engine.massConservationError < 10f ? _cNormalGreen :
                      engine.massConservationError < 50f ? _cWarningAmber : _cAlarmRed;
        DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
            "MASS CONS", engine.massConservationError, 0f, 100f, consC, "F1", "gal");
        y += barH + 2f;
    }

    y += GAUGE_GROUP_GAP;
}
```

### Stage 3: Update Constants and Call Sequence

```csharp
// Height constants — REMOVE old VCT and BRS, ADD combined
// const float VCT_GROUP_H = ...  // DELETE
// const float BRS_GROUP_H = ...  // DELETE
const float LIQUID_INV_GROUP_H = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 2 + GAUGE_GROUP_GAP;

// Update total
const float TOTAL_GAUGE_H = TEMP_GROUP_H + PZR_GROUP_H + CVCS_GROUP_H
                            + LIQUID_INV_GROUP_H + RCP_GROUP_H + 20f;

// Update DrawGaugeColumnContent()
partial void DrawGaugeColumnContent(Rect area)
{
    if (engine == null) return;

    float y = area.y;
    float w = area.width;

    DrawTemperatureGauges(area.x, ref y, w);
    DrawPressurizerGauges(area.x, ref y, w);
    DrawCVCSFlowGauges(area.x, ref y, w);
    DrawLiquidInventoryGauges(area.x, ref y, w);  // NEW combined group
    // REMOVED: DrawVCTGauges(area.x, ref y, w);
    // REMOVED: DrawBRSGauges(area.x, ref y, w);
    DrawRCPHeatGauges(area.x, ref y, w);
}
```

---

## Height Calculation

**Current layout:**
- VCT_GROUP_H = 24 + 85 + 60 + 8 = **177px**
- BRS_GROUP_H = 24 + 85 + 60 + 8 = **177px**
- **Total for these 2 groups: 354px**

**Proposed layout:**
- LIQUID_INV_GROUP_H = 24 + 85 + 40 + 8 = **157px** (header + arc row + 2 bars + gap)
- **Total: 157px**

**Space saved: 197px**

---

## Files to Modify

| File | Changes |
|------|---------|
| `HeatupValidationVisual.Gauges.cs` | Add `DrawGaugeArcBidirectional()`, add `DrawLiquidInventoryGauges()`, remove old VCT/BRS methods, update constants |

---

## Validation Criteria

| Test | Expected Result |
|------|-----------------|
| VCT arc gauge displays correctly | Shows level %, proper color coding for bands |
| BRS Holdup arc gauge displays correctly | Shows holdup %, orange when processing |
| BRS Flow gauge at zero | Needle points straight up (12 o'clock), gray color |
| BRS Flow gauge during divert | Needle deflects RIGHT, orange color, positive value shown |
| BRS Flow gauge during return | Needle deflects LEFT, blue color, negative value shown |
| Value text shows sign | "+5.0 gpm" for inflow, "-3.2 gpm" for outflow, "0.0 gpm" for idle |
| Vertical layout reduced | ~197px saved vs two separate groups |

---

## Westinghouse PWR Technical Validation

**BRS Flow Rates (per NRC HRTD 4.1, Callaway FSAR Ch.11):**
- **Inflow (Divert)**: 0–77 gpm proportional to VCT level above 70%
- **Outflow (Return)**: 0–35 gpm (AUTO_MAKEUP_FLOW_GPM when BRS-sourced)
- **Gauge Range**: ±40 gpm provides comfortable margin for typical operations

---

## Implementation Sequence

1. **Stage 1**: Add `DrawGaugeArcBidirectional()` function
2. **Stage 2**: Add `DrawLiquidInventoryGauges()` with three arc gauges
3. **Stage 3**: Update constants, remove old methods, update call sequence
4. **Test**: Verify all three gauges display correctly in various VCT states

---

## Approval

**Status:** APPROVED BY USER

Ready to proceed with implementation.
