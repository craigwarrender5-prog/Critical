# CS-0121 Investigation Report (2026-02-18_131500)

**Title:** Dashboard visual issues: SOLID PZR indicator not lit, alarm symbol incorrectly transcoded  
**Severity:** LOW  
**Domain:** Operator Interface & Scenarios  
**Status:** READY  
**Created:** 2026-02-18T11:15:00Z  
**Updated:** 2026-02-18T13:15:00Z  
**Assigned DP:** DP-0008

---

## 1. Problem Summary

Two dashboard visual defects are confirmed:

1. **SOLID PZR indicator** is displayed as off in Overview while plant state is solid.
2. **Alarm symbol** in dashboard header is mojibake/transcoded text instead of intended warning glyph.

---

## 2. Root Cause Analysis

### Issue A: SOLID PZR indicator logic mismatch

Code path in Overview tab uses bubble-formed flag as LED ON condition:
- `Assets/Scripts/Validation/Tabs/OverviewTab.cs:321-322`

Current logic:
- Label shows `SOLID PZR` when `s.SolidPressurizer` is true.
- LED ON flag uses `s.BubbleFormed`.

Result: in solid state (`BubbleFormed=false`, `SolidPressurizer=true`), label is correct but LED remains unlit.

Cross-check: Pressurizer tab has a dedicated SOLID PZR LED correctly bound to `s.SolidPressurizer`:
- `Assets/Scripts/Validation/Tabs/PressurizerTab.cs:239-240`

### Issue B: Alarm symbol mojibake in header

Header alarm string contains corrupted symbol literal:
- `Assets/Scripts/Validation/ValidationDashboard.cs:354`

Current string literal contains `âš ` sequence rather than clean ASCII-safe marker or valid glyph for active font path, producing garbled display in runtime.

---

## 3. Evidence

1. Overview visual behavior aligns with the code mismatch at `OverviewTab.cs:321-322`.
2. Header alarm glyph corruption aligns with explicit mojibake literal at `ValidationDashboard.cs:354`.

---

## 4. Disposition

**Disposition: READY (full investigation complete).**

Both defects are localized rendering-layer issues with clear code-level fixes.

---

## 5. Proposed Resolution

1. Update Overview SOLID PZR indicator ON-state binding to match solid-state telemetry semantics.
2. Replace header alarm symbol literal with encoding-safe output (ASCII-safe token or validated glyph path).
3. Regression-check visual consistency across dashboard tabs.

---

## 6. Impact Assessment

- **User Impact:** LOW — UI correctness and operator trust issue.
- **Technical Debt:** LOW — localized display logic corrections.
- **Blocking:** NONE for physics behavior; should close within DP-0008 UI pass.

---

## 7. Acceptance Criteria

1. SOLID PZR indicator illuminates whenever solid-state telemetry indicates solid pressurizer.
2. Bubble-state and solid-state indicators are mutually consistent across Overview and Pressurizer tabs.
3. Header alarm marker renders without mojibake/garbled characters.

---

## 8. Tags

- `Dashboard-Indicator`
- `PZR-Solid-State`
- `Alarm-Symbol`
- `Character-Encoding`
- `Visual-Bug`
- `Low-Priority`
- `User-Request-2026-02-18`
