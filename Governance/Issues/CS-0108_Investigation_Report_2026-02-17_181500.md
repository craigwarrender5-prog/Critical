# CS-0108 Investigation Report

**Issue ID:** CS-0108  
**Title:** PZR temperature monitoring missing for bubble formation readiness during cold startup  
**Date:** 2026-02-17T18:15:00Z  
**Investigator:** Codex  
**Status:** INVESTIGATING → READY

---

## 1. Observed Symptoms

The ValidationDashboard displays `T_pzr` (pressurizer temperature) as a numeric readout in the RCS column, but:

1. **No comparison to T_sat is shown** — Operators cannot see how close T_pzr is to saturation temperature
2. **No PZR subcooling indicator** — The difference (T_sat - T_pzr) is not displayed
3. **No annunciator for PZR approaching saturation** — Critical bubble formation milestone is not alarmed
4. **No sparkline trend for T_pzr** — Temperature rise toward saturation is not trended

This gap means operators have no visual indication of when the pressurizer is ready for bubble formation during cold startup.

---

## 2. Reproduction Steps

1. Start simulation in Mode 5 Cold Shutdown (T_avg = 120°F, solid pressurizer)
2. Observe ValidationDashboard Overview tab
3. Note that T_pzr is displayed but T_sat comparison is absent
4. Note that annunciator panel has no "PZR SAT" or "BUBBLE READY" tile
5. Observe that bubble formation timing must be inferred from other parameters

---

## 3. Root Cause Analysis

**Confirmed Root Cause:** The ValidationDashboard was designed with post-bubble operations as the primary focus. The pre-bubble cold startup phase requires monitoring the *approach* to saturation conditions, which was not implemented.

Per NRC HRTD 17.0:
> "When pressurizer temperature reaches saturation temperature for the pressure being maintained (450°F for 400 psig), a pressurizer bubble is established."

The T_pzr → T_sat approach is the critical precondition for bubble formation. Without monitoring this approach:
- Operators cannot anticipate when bubble draw can begin
- The timing milestone "PZR at saturation" is invisible
- Heatup rate optimization is impaired

---

## 4. Technical Evidence

| Reference | Notes |
|-----------|-------|
| `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:38` | "Heat PZR to T_sat (450°F at 400 psig)" as key sequencing step |
| `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:53` | "When pressurizer temperature reaches saturation temperature... a pressurizer bubble is established" |
| `Assets/Scripts/Validation/ValidationDashboard.Snapshot.cs:108` | T_pzr captured but no T_pzr subcooling calculated |
| `Assets/Scripts/Validation/ValidationDashboard.Annunciators.cs` | No annunciator tile for PZR saturation approach |
| `Assets/Scripts/Validation/Tabs/OverviewTab.cs:219` | T_pzr displayed as readout only, no comparison gauge |

---

## 5. Proposed Fix Options

### Option A: Minimal — Add PZR Subcooling Readout
- Add `PzrSubcooling = T_sat - T_pzr` to snapshot
- Display as digital readout in PZR column
- **Pro:** Minimal change, quick implementation
- **Con:** No visual alarm, no trend

### Option B: Moderate — Add Annunciator + Readout
- Add PZR subcooling readout (Option A)
- Add "PZR SAT" annunciator tile (INFO severity, triggers when T_pzr ≥ T_sat - 5°F)
- **Pro:** Provides clear visual milestone indicator
- **Con:** No trend history

### Option C: Comprehensive — Annunciator + Readout + Trend
- Add PZR subcooling readout
- Add "PZR SAT" annunciator tile
- Replace one sparkline (candidate: NET HEAT or consider 9th sparkline) with T_PZR trend
- **Pro:** Full visibility of approach to saturation
- **Con:** Requires sparkline system modification or replacement decision

---

## 6. Recommended Fix

**Option B: Moderate — Add Annunciator + Readout**

Rationale:
- Provides actionable visual indication via annunciator
- PZR subcooling readout gives numeric precision
- Does not require sparkline system changes
- Aligns with ISA-18.1 annunciator philosophy (INFO tile for milestone reached)

Implementation scope:
1. Add `PzrSubcooling` field to `DashboardSnapshot` (T_sat - T_pzr)
2. Add digital readout "PZR ΔT_SAT" in Pressurizer column showing subcooling to saturation
3. Add annunciator tile "PZR SAT" (INFO severity) that activates when `PzrSubcooling ≤ 5°F`
4. Consider color-coding the T_pzr readout based on approach to saturation

---

## 7. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Display clutter in PZR column | LOW | Replace existing readout or use compact format |
| Annunciator tile count (27 limit) | LOW | One tile slot available or repurpose existing |
| Snapshot field addition | MINIMAL | Simple calculated field, no physics impact |

**Affected Systems:**
- ValidationDashboard.Snapshot.cs
- ValidationDashboard.Annunciators.cs  
- Tabs/OverviewTab.cs (PZR column)

**No physics module changes required.**

---

## 8. Validation Method

1. Start simulation in Mode 5 Cold Shutdown
2. Verify PZR subcooling readout shows (T_sat - T_pzr) accurately
3. Observe annunciator tile "PZR SAT" is OFF (gray) when T_pzr << T_sat
4. Run heatup until T_pzr approaches T_sat
5. Verify annunciator tile "PZR SAT" activates (green) when subcooling ≤ 5°F
6. Verify event log records "PZR SAT: Pressurizer At Saturation" milestone

---

## 9. Domain Assignment Justification

**Assigned Domain:** Operator Interface & Scenarios (DP-0008)

Rationale: This is a dashboard display/monitoring enhancement for operator visibility during cold startup. The ValidationDashboard operator interface is the affected system, which falls under the Operator Interface & Scenarios domain (DP-0008), not Validation & Diagnostics. DP-0008 covers operator interface design, dashboard/instrumentation displays, and scenario handling.

---

## 10. Conclusion

Investigation complete. Root cause confirmed as missing pre-bubble monitoring instrumentation. Recommended fix is Option B (annunciator + readout). Ready for implementation planning.

**Status Transition:** INVESTIGATING → READY
