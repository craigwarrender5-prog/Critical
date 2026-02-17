# CS-0111 Investigation Report

**Issue ID:** CS-0111
**Title:** PZR temperature (T_pzr) lacks primary visualization — no arc gauge or trend display
**Date:** 2026-02-17T19:45:00Z
**Investigator:** Codex
**Status:** INVESTIGATING → READY

---

## 1. Observed Symptoms

Despite T_pzr being a critical parameter during cold startup (pre-bubble PZR heating), the ValidationDashboard provides inadequate visualization:

1. **No T_pzr arc gauge** — The Pressurizer column has arc gauges for PRESSURE and LEVEL, but not temperature
2. **T_pzr readout is misplaced** — Currently displayed in RCS column, not Pressurizer column
3. **No T_pzr sparkline trend** — Cannot observe heating rate or trajectory toward saturation over time
4. **No T_pzr vs T_sat comparative gauge** — Operators cannot visually see approach to saturation

Per NRC HRTD 17.0: "When pressurizer temperature reaches saturation temperature for the pressure being maintained (450°F for 400 psig), a pressurizer bubble is established." This makes T_pzr trajectory monitoring essential during cold startup.

The recently added "PZR ΔT_SAT" readout (IP-0050) shows subcooling, but operators need to see the *actual temperature* and its progression.

---

## 2. Impact

During cold startup:
- PZR heaters are energized to heat pressurizer water from ~120°F to ~450°F (saturation at 400 psig)
- This is a multi-hour process requiring continuous monitoring
- Operators cannot visualize:
  - Current T_pzr position relative to target
  - Heating rate / trajectory
  - Whether heaters are effective

Per NRC HRTD 17.0, reaching T_sat is a critical milestone for bubble formation. Without proper T_pzr visualization, operators are "flying blind" during this phase.

---

## 3. Root Cause Analysis

**Confirmed Root Cause:** The dashboard was designed with post-bubble operations as primary focus. The Pressurizer column emphasizes:
- Pressure (arc gauge) — critical post-bubble
- Level (arc gauge) — critical post-bubble
- Heater power (bar gauge)
- Spray (bar gauge)

Temperature was treated as secondary, relegated to a small readout in the RCS column alongside T_avg, T_hot, T_cold.

---

## 4. Technical Evidence

| Reference | Notes |
|-----------|-------|
| `Assets/Scripts/Validation/Tabs/OverviewTab.cs:260-275` | Pressurizer column has PRESSURE and LEVEL arc gauges, no T_pzr gauge |
| `Assets/Scripts/Validation/Tabs/OverviewTab.cs:205` | T_pzr displayed in RCS column, not PZR column |
| `Assets/Scripts/Validation/ValidationDashboard.Snapshot.cs:108` | T_pzr captured but underutilized |
| `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:38` | "Heat PZR to T_sat (450°F at 400 psig)" — heating phase is procedurally critical |

---

## 5. Proposed Fix

### Option A: Minimal — Add T_pzr Arc Gauge to PZR Column
- Add T_pzr arc gauge (range 50-600°F) as primary temperature visualization
- Remove T_pzr readout from RCS column (eliminate duplication)
- Keep existing PZR ΔT_SAT readout

### Option B: Comprehensive — Arc Gauge + Sparkline (SELECTED)
- Option A changes
- Replace NET HEAT sparkline (slot 7) with T_PZR sparkline
- Shows temperature trajectory toward saturation over ~1 hour

### Option C: Full Treatment — Dual-Scale Comparative Gauge
- T_pzr arc gauge with T_sat marker overlay
- Visual indication of approach to saturation on the gauge itself
- Sparkline trend

---

## 6. Recommended Fix

**Option B: Comprehensive — Arc Gauge + Sparkline**

Rationale:
- Provides primary visualization that's currently missing (arc gauge)
- Arc gauge is consistent with existing PRESSURE and LEVEL gauges
- Sparkline provides trajectory history essential for monitoring bubble formation approach
- NET HEAT is a calculated diagnostic value; T_PZR is a primary operational parameter
- Color-coded gauge: green at saturation, cyan when subcooled

Implementation scope:
1. Add T_pzr arc gauge to Pressurizer column (after LEVEL gauge, before volumes)
2. Remove T_pzr readout from RCS column
3. Replace NET HEAT sparkline (IDX_NET_HEAT = 7) with T_PZR sparkline
4. Ensure PZR ΔT_SAT readout remains in PZR column (already there from IP-0050)

---

## 7. Acceptance Criteria

1. T_pzr arc gauge visible in Pressurizer column
2. Gauge range appropriate for startup (50-600°F)
3. Color indication: cyan (subcooled) → green (at saturation)
4. T_pzr readout removed from RCS column (no duplication)
5. T_pzr sparkline visible in Trends column (slot 7, replaces NET HEAT)
6. Sparkline shows ~1 hour of history
7. Existing PZR ΔT_SAT readout still visible in PZR column

---

## 8. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| PZR column crowding | MEDIUM | Gauge radius matches existing; layout tested |
| RCS column gap after T_pzr removal | LOW | Shift remaining readouts up |
| Loss of NET HEAT diagnostic | LOW | Calculated value can be restored if needed |

**Affected Systems:**
- Tabs/OverviewTab.cs (DrawPressurizerColumn, DrawRCSColumn)
- ValidationDashboard.Sparklines.cs (SparklineManager)

**No physics module changes required.**

---

## 9. Domain Assignment

**Assigned Domain:** Operator Interface & Scenarios (DP-0008)

Rationale: This is a dashboard display enhancement for operator visibility.

---

## 10. Conclusion

Investigation complete. Root cause confirmed as missing primary T_pzr visualization. Recommended fix is Option A (arc gauge + readout relocation). Ready for IP creation.

**Status Transition:** INVESTIGATING → READY
