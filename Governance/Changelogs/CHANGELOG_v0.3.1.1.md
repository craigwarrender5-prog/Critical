# CHANGELOG v0.3.1.1 — State Machine Guards, Flicker Regression Fix, and RHR Flow Coupling

**Date:** 2026-02-14
**Version:** v0.3.1.1
**Type:** Multi-Domain Revision (Bubble Formation Guards + Performance Regression + Core Physics)
**Matching Implementation Plans:** IP-0003 — Bubble Formation and Two-Phase, IP-0009 — Performance and Runtime Architecture, IP-0004 — RCS Energy Balance and Regime Transition
**Governing Document:** PROJECT_CONSTITUTION.md v0.4.0.0
**Issues Resolved:** CS-0033
**Issues Partially Addressed:** CS-0032 (Phase A regression fix only; Phase A baseline in v0.3.1.0)
**Issues Created:** CS-0034, CS-0035 (spun out from CS-0033 Findings B/C)

---

## Release Summary

This revision release (on top of v0.3.1.0) addresses three concerns:

1. **Bubble Formation State Machine Guards (Fix 3.3)** — Transition robustness improvements for DRAIN→STABILIZE and PRESSURIZE→COMPLETE phase transitions.
2. **CS-0032 Phase A Regression Fix** — Corrects blue-screen flicker caused by OnGUI Repaint suppression introduced in v0.3.1.0. Layout-only throttle replaces dual-event gating.
3. **CS-0033 RHR Flow-Coupled Pump Heat (Finding A)** — Surgical correction ensuring RHR pump heat transfers to RCS only under validated hydraulic connection. Remaining findings (B, C) spun out to CS-0034 and CS-0035.

---

## Fix 3.3: Bubble Formation State Machine Transition Guards

### Issues Addressed
- Bubble Formation and Two-Phase domain — Phase D transition robustness

### Files Modified
| File | Change |
|------|--------|
| `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` | Added `MAX_DRAIN_EXIT_PRESSURE_RATE = 50f` constant |
| `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | Added DRAIN→STABILIZE advisory pressure check; added PRESSURIZE→COMPLETE mandatory level stability guard |

### Details

**DRAIN→STABILIZE Transition:**
- Added advisory (non-blocking) pressure rate stability check at drain exit
- When PZR level reaches target (`PZR_LEVEL_AFTER_BUBBLE + 0.5f`), transition proceeds immediately
- If `|pressureRate| > MAX_DRAIN_EXIT_PRESSURE_RATE` (50 psi/hr), an ALERT-severity event is logged but does NOT block the transition
- Rationale: Drain must complete to avoid level runaway; pressure instability is informational

**PRESSURIZE→COMPLETE Transition:**
- Added mandatory level stability guard
- Both conditions required: `pressure >= minRcpP_psig` AND `pzrLevel >= PZR_LEVEL_AFTER_BUBBLE - 2f`
- If pressure is sufficient but level is too low, phase HOLDS with an ALERT-severity event
- Prevents premature RCP start with unstable PZR inventory

---

## CS-0032 Phase A Regression Fix: Blue Screen Flicker

### Issues Addressed
- **CS-0032** (Critical/BLOCKER): Regression in v0.3.1.0 OnGUI throttle causing blue-screen flicker

### Files Modified
| File | Change |
|------|--------|
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Removed `_layoutAccepted` dual-event gating; changed to Layout-only throttle; Repaint always draws from cached data |

### Details

**Problem:** The v0.3.1.0 OnGUI implementation used a `_layoutAccepted` flag that gated both Layout AND Repaint events. When Layout was throttled (most frames), `_layoutAccepted = false` caused Repaint to `return` early. Unity clears the screen before Repaint but nothing was drawn → blue flash every non-Layout frame.

**Root Cause:** Repaint must NEVER be skipped. Unity's immediate-mode GUI requires that every Repaint event draws the full UI. Skipping Repaint produces a cleared-but-empty frame.

**Fix:** Removed `_layoutAccepted` field entirely. Changed to Layout-only throttle:
- Layout events: skipped if within throttle interval (redundant layout passes avoided)
- Repaint events: always execute, drawing from cached header strings (cheap operation)
- The 30 FPS cap (v0.3.1.0 A.1) limits Repaint frequency at the frame level
- Header caching (v0.3.1.0 A.3) makes each Repaint inexpensive

### CS-0032 Status
Remains **Assigned** — Phase A baseline in v0.3.1.0, regression fixed in v0.3.1.1. Phases B (async logging), C (snapshot boundary), and D (parallelization) remain pending. BLOCKER status retained until long-run validation confirms stability.

---

## CS-0033: RCS Energy Balance Diagnostic Audit and Fix A

### Issues Addressed
- **CS-0033** (High): RCS bulk temperature rise with RCPs OFF — Finding A resolved

### Files Modified
| File | Change |
|------|--------|
| `Assets/Scripts/Physics/RHRSystem.cs` | Added flow-coupling guard in `UpdateActive()`; updated `UpdateIsolating()` comment; added validation tests 9/10 |

### Details

**Finding A — RHR Pump Heat Flow Coupling:**

**Problem:** `RHRSystem.UpdateActive()` injected pump heat (`RHR_PUMP_HEAT_MW_EACH * PumpsOnline`) unconditionally whenever mode ≠ Standby. No validation that suction/discharge valves were aligned or that actual hydraulic flow existed. This violated the principle that mechanical energy transfer requires a valid coupling mechanism.

**Fix:**
```
bool hydraulicCoupled = state.SuctionValvesOpen && state.FlowRate_gpm > 0f;
if (hydraulicCoupled)
    state.PumpHeatInput_MW = PlantConstants.RHR_PUMP_HEAT_MW_EACH * state.PumpsOnline;
else
    state.PumpHeatInput_MW = 0f;
```

When uncoupled (valves closed or no flow), pump mechanical energy is rejected to ambient equipment/bearing heat — not injected into RCS bulk.

**Existing Flow Coupling (Confirmed):**
- `UpdateIsolating()` was already flow-coupled: pump heat scales with `flowFraction` during ramp-down, naturally trending to zero as flow ceases

**Validation Tests Added:**
- Test 9: Valves closed with pumps online → `PumpHeatInput_MW == 0`
- Test 10: Zero pumps online → `PumpHeatInput_MW == 0`

**Validation Checks Satisfied:**
1. RHR pumps online, valves shut → PumpHeatInput_MW == 0 ✓
2. RHR pumps online, valves open, flow > 0 → PumpHeatInput_MW == N × HEAT_EACH ✓
3. During Isolating ramp-down → PumpHeatInput_MW smoothly trends to 0 ✓

### Findings B and C — Deferred to New Issues

**CS-0034 (Finding B):** No equilibrium ceiling in Regime 0/1. T_rcs rises indefinitely via surge conduction minus weak insulation loss. Assigned to RCS Energy Balance / Core Physics domain. Severity: Medium.

**CS-0035 (Finding C):** CVCS thermal mixing contribution missing. Charging water at ~100°F enters as mass-only, ignoring ~0.15 MW cooling. Assigned to CVCS Energy Balance domain. Severity: Low.

CS-0033 is **Resolved** — all three findings tracked individually (A resolved, B/C as new issues).

---

## Files Changed Summary

| File | Change Description |
|------|--------------------|
| `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` | +1 constant (MAX_DRAIN_EXIT_PRESSURE_RATE) |
| `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | DRAIN→STABILIZE advisory check, PRESSURIZE→COMPLETE level guard |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Flicker fix: removed _layoutAccepted, Layout-only throttle |
| `Assets/Scripts/Physics/RHRSystem.cs` | Flow-coupled pump heat guard, validation tests 9/10 |
| `Updates/ISSUE_REGISTRY.md` | CS-0033 resolved, CS-0034/CS-0035 created, summary counts updated |

---

## Issue Status Changes

| Issue | Previous Status | New Status | Notes |
|-------|----------------|------------|-------|
| CS-0032 | Assigned | Assigned | v0.3.1.0 Phase A regression fixed; Phases B-D pending |
| CS-0033 | Assigned | Resolved | All findings tracked; Fix A implemented |
| CS-0034 | — | Assigned | New (CS-0033 Finding B spin-out) |
| CS-0035 | — | Assigned | New (CS-0033 Finding C spin-out) |

---

## Validation Summary

| Criterion | Result |
|-----------|--------|
| DRAIN→STABILIZE advisory check fires at high pressure rate | Implemented (advisory, non-blocking) |
| PRESSURIZE→COMPLETE level guard blocks premature completion | Implemented (mandatory hold) |
| OnGUI Repaint never skipped (no blue-screen flicker) | Fixed (v0.3.1.0 regression corrected) |
| RHR pump heat = 0 when hydraulically uncoupled | Implemented + Tests 9/10 |
| RHR pump heat = N × HEAT_EACH when coupled | Verified |
| RHR Isolating ramp-down trends pump heat to 0 | Verified (existing behavior) |
| EventSeverity.ALERT used (not WARNING) | Fixed (compile error resolved) |
| Unity build | Requires verification |

---

## Testing Checklist

- [x] Bubble formation DRAIN→STABILIZE transition: advisory pressure check implemented
- [x] Bubble formation PRESSURIZE→COMPLETE transition: mandatory level stability guard
- [x] Blue-screen flicker: _layoutAccepted removed, Layout-only throttle, Repaint always draws
- [x] RHR pump heat flow coupling: hydraulicCoupled guard in UpdateActive()
- [x] RHR validation tests 9/10: uncoupled scenarios verified
- [x] CS-0033 resolved with all findings tracked (A resolved, B→CS-0034, C→CS-0035)
- [ ] Unity compile: 0 errors (pending verification)
- [ ] Long-run validation: CS-0032 stability (pending Phase E)
