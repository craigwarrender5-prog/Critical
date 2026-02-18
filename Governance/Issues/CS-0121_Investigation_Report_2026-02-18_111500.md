# CS-0121 Investigation Report

**Title:** SOLID PZR indicator not illuminated despite pressurizer being in solid condition  
**Severity:** LOW  
**Domain:** Operator Interface & Scenarios  
**Status:** READY  
**Created:** 2026-02-18T11:15:00Z  
**Assigned DP:** DP-0008  

---

## 1. Problem Summary

The "SOLID PZR" indicator in the Pressurizer panel on the Validation Dashboard Overview tab is not illuminated even when the pressurizer is clearly in a solid (water-solid) condition.

---

## 2. Evidence from Screenshot

| Parameter | Value | Interpretation |
|-----------|-------|----------------|
| Header | "SOLID PZR - HEATING TO TSAT (358f)" | System recognizes solid state |
| Plant Mode | MODE 5 Cold SD | Cold shutdown, solid plant expected |
| WATER | 1800.0 | Full water inventory |
| STEAM | 0.0 | No steam space |
| PZR Level Gauge | 100% (visual) | Fully water-solid |
| SOLID PZR Indicator | NOT LIT | **Incorrect** |

The phase description in the header explicitly states "SOLID PZR" yet the indicator checkbox is not illuminated.

---

## 3. Likely Root Cause

The indicator is likely bound to the wrong state variable or has inverted logic. Possibilities include:

1. **Wrong variable binding**: Indicator may be checking `hasBubble` instead of `!hasBubble` or similar
2. **Threshold mismatch**: Indicator may use a different level threshold than the phase state machine
3. **State not exposed**: The solid/bubble state may not be properly exposed to the dashboard telemetry snapshot
4. **Drawing logic error**: The indicator drawing code may have incorrect conditional logic

---

## 4. Files to Investigate

| File | Likely Location | Purpose |
|------|-----------------|---------|
| `HeatupValidationVisual.TabOverview.cs` | PZR panel drawing | Where indicator is rendered |
| `HeatupValidationVisual.Panels.cs` | Panel helpers | May contain indicator logic |
| `HeatupSimEngine.cs` | `hasBubble` field | State variable |
| `RuntimeTelemetrySnapshot` | Telemetry struct | May need to expose solid state |

---

## 5. Proposed Resolution

1. Locate the SOLID PZR indicator drawing code in the Overview tab
2. Verify the conditional logic for illumination
3. Ensure it checks for `!hasBubble` or equivalent solid-state condition
4. Validate against the telemetry snapshot or engine state

---

## 6. Impact Assessment

- **User Impact:** LOW — Cosmetic/informational only, header shows correct state
- **Technical Debt:** LOW — Localized fix in dashboard rendering
- **Blocking:** NONE — No simulation or physics impact

---

## 7. Acceptance Criteria

1. SOLID PZR indicator illuminates when `hasBubble == false` and PZR level is at/near 100%
2. SOLID PZR indicator is not illuminated when bubble has formed
3. Indicator state matches the phase description in the header bar

---

## 8. Tags

- `Dashboard-Indicator`
- `PZR-Solid-State`
- `Visual-Bug`
- `Low-Priority`
- `User-Request-2026-02-18`
