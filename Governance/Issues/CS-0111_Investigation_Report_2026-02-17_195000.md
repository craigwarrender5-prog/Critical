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
3. **No T_pzr sparkline trend** — Cannot observe heating rate or trajectory toward saturation
4. **No T_pzr vs T_sat comparative gauge** — Operators cannot visually see approach to saturation

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
- Move T_pzr readout from RCS column to PZR column
- Keep existing PZR ΔT_SAT readout below it

### Option B: Comprehensive — Arc Gauge + Sparkline
- Option A changes
- Add T_pzr sparkline (replace one existing or add 9th sparkline)
- Shows temperature trajectory toward saturation

### Option C: Full Treatment — Dual-Scale Comparative Gauge
- T_pzr arc gauge with T_sat marker overlay
- Visual indication of approach to saturation on the gauge itself
- Sparkline trend

---

## 6. Recommended Fix

**Option A: Minimal — Add T_pzr Arc Gauge to PZR Column**

Rationale:
- Provides primary visualization that's currently missing
- Arc gauge is consistent with existing PRESSURE and LEVEL gauges
- Moving T_pzr readout to PZR column improves logical grouping
- Does not require sparkline system changes
- Can be extended to Option B/C in future if needed

Implementation scope:
1. Add T_pzr arc gauge to Pressurizer column (after LEVEL gauge, before volumes)
2. Remove T_pzr readout from RCS column
3. Ensure PZR ΔT_SAT readout remains in PZR column (already there from IP-0050)
4. Color-code gauge based on approach to saturation

---

## 7. Acceptance Criteria

1. T_pzr arc gauge visible in Pressurizer column
2. Gauge range appropriate for startup (50-600°F)
3. Color indication: normal (cyan) → approaching saturation (green) → at saturation (green)
4. T_pzr readout in PZR column (not RCS)
5. RCS column T_pzr readout removed (avoid duplication)

---

## 8. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| PZR column crowding | MEDIUM | May need to reduce other element sizes or reorganize |
| RCS column gap after T_pzr removal | LOW | Shift remaining readouts up |

**Affected Systems:**
- Tabs/OverviewTab.cs (DrawPressurizerColumn, DrawRCSColumn)

**No physics module changes required.**

---

## 9. Domain Assignment

**Assigned Domain:** Operator Interface & Scenarios (DP-0008)

Rationale: This is a dashboard display enhancement for operator visibility.

---

## 10. Conclusion

Investigation complete. Root cause confirmed as missing primary T_pzr visualization. Recommended fix is Option A (arc gauge + readout relocation). Ready for IP creation.

**Status Transition:** INVESTIGATING → READY
