---
Issue: CS-0043
Title: Pressurizer Pressure Boundary Failure During Bubble Formation
Severity: Critical
Status: SPEC/DRAFT — Investigation Complete, Awaiting Authorization
Detected In: v0.3.1.1 (Stage E Long-Run Validation)
Date: 2026-02-14
Validation Result: Stage E FAILED
---

# CS-0043 INVESTIGATION REPORT
## Pressurizer Pressure Boundary Failure — Runaway Depressurization During Bubble Formation

---

## 1. Executive Summary

Stage E long-run validation of the Bubble Formation state machine (v0.3.0.0 + v0.3.1.1) revealed a critical pressure boundary failure. Upon entering the DRAIN phase, pressurizer pressure collapses monotonically from ~368 psia to ~154 psia over approximately 2 hours despite 1.8 MW heaters at full power. The rate of depressurization accelerates from -78 psi/hr at DETECTION to -383 psi/hr during active DRAIN.

**Root cause: Dual energy application.** Heater power is consumed twice in the same timestep — once as sensible heat (temperature rise) in `IsolatedHeatingStep`, and again as latent heat (steam generation) in `UpdateDrainPhase`. A T_sat cap in `IsolatedHeatingStep` creates a feedback ratchet that guarantees monotonic pressure decline.

In a real PWR, pressurizer heaters maintain saturation conditions during bubble formation. Pressure should be stable or slowly rising as heater energy generates steam to establish the vapor space. The simulated behavior is physically impossible.

---

## 2. Root Cause Analysis

### 2.1 Failure Mechanism — Four-Step Feedback Loop

The runaway depressurization is caused by four interacting mechanisms in the Regime 1 physics pipeline:

**Step A — Subcooled Energy Model Applied to Two-Phase System**

`RCSHeatup.IsolatedHeatingStep()` (RCSHeatup.cs, lines 290-362) computes PZR temperature rise using a subcooled liquid model:

```
pzrNetHeat_BTU = pzrHeatInput_BTU - conductionHeat_BTU - pzrInsulLoss_BTU
pzrDeltaT = pzrNetHeat_BTU / pzrHeatCapacity
result.T_pzr = T_pzr + pzrDeltaT
```

This model assumes all heater energy goes to sensible heat (temperature rise of liquid water). This is correct when the PZR contains only subcooled liquid (solid water operations). It is incorrect when a steam bubble exists, because in two-phase conditions the heater energy goes primarily to latent heat (boiling liquid into steam at constant temperature).

**Step B — T_sat Cap Discards the Energy**

After computing the temperature rise, `IsolatedHeatingStep` applies a saturation cap:

```
T_sat = WaterProperties.SaturationTemperature(pressure)  // pressure = P_old
result.T_pzr = Math.Min(result.T_pzr, T_sat)
```

Once the PZR is at saturation (which it is during bubble formation), the computed dT is entirely discarded because T_pzr + dT > T_sat(P_old) and the cap clamps it back to T_sat(P_old). The heater energy is consumed (computed) but produces no effect on temperature.

**Step C — Psat Override Creates the Ratchet**

In the Regime 1 physics branch (HeatupSimEngine.cs, line 1143), a two-phase pressure override applies:

```
pressure = WaterProperties.SaturationPressure(state.T_pzr)
```

Since T_pzr ≤ T_sat(P_old), this yields P_new = Psat(T_pzr) ≤ P_old. If T_pzr was clamped to exactly T_sat(P_old), then P_new = P_old (neutral). But any numerical noise, conduction loss, or insulation loss that pulls T_pzr even slightly below T_sat(P_old) produces P_new < P_old.

On the next timestep, T_sat(P_new) < T_sat(P_old), so the cap ratchets lower. This creates a one-way pressure decline.

**Step D — Double Energy Application**

Simultaneously, `UpdateDrainPhase()` (HeatupSimEngine.BubbleFormation.cs, line 343) independently consumes the same heater power for steam generation:

```
steamGenRate_lb_sec = heaterPower_BTU_sec / h_fg
```

The heater energy is now applied twice in the same timestep:
1. As sensible heat dT in `IsolatedHeatingStep` (capped and discarded)
2. As latent heat steam generation in `UpdateDrainPhase` (applied to mass transfer)

This violates energy conservation (first law of thermodynamics). The steam generation drives level down (correct behavior for DRAIN), but the pressure collapse means the steam is generated at ever-decreasing saturation conditions — an unphysical runaway.

### 2.2 Contributing Factor — `TwoPhaseHeatingUpdate` Reinforces the Pattern

`PressurizerPhysics.TwoPhaseHeatingUpdate()` (PressurizerPhysics.cs, lines 217-270) uses the same thermal-expansion pressure model with a DAMPING_FACTOR of 0.5. This method is not the primary driver during Regime 1 (the Psat override at HeatupSimEngine.cs:1143 overwrites its output), but it reinforces the flawed assumption that two-phase PZR behavior can be modeled as subcooled liquid thermal expansion.

### 2.3 Correct Three-Region Model Exists But Is Not Used

`PressurizerPhysics.ThreeRegionUpdate()` (lines 362-439) contains a proper two-phase PZR model that accounts for flash evaporation, spray condensation, heater steam generation rate, wall condensation, and rainout. This method correctly models pressure through steam mass balance rather than thermal expansion. However, **this method is only called during Regime 2/3** (RCPs running, coupled flow). During Regime 1 (isolated PZR heating), the engine uses `IsolatedHeatingStep` + Psat override instead.

### 2.4 Root Cause Summary

| Component | Error |
|-----------|-------|
| `RCSHeatup.IsolatedHeatingStep` | Applies subcooled sensible-heat model (dT = Q/mCp) to a two-phase system where energy should go to latent heat |
| T_sat cap in `IsolatedHeatingStep` | Discards computed energy without redirecting it to steam generation; creates ratchet via Psat(capped T) |
| Two-phase Psat override (HeatupSimEngine.cs:1143) | Converts the capped T_pzr into a monotonically declining pressure |
| `UpdateDrainPhase` steam generation | Independently consumes heater energy via h_fg — double-counting with `IsolatedHeatingStep` |
| Regime 1 architecture | Does not invoke `ThreeRegionUpdate()` which has the correct two-phase pressure model |

---

## 3. Evidence — Interval Log Data

| Sim Time | Phase | P (psia) | T_pzr (F) | T_rcs (F) | dP/dt (psi/hr) | PZR Level (%) | Heater Mode |
|----------|-------|----------|-----------|-----------|----------------|---------------|-------------|
| 8.00 hr | Pre-bubble | 368.2 | 427.18 | 247.71 | +20.65 | 100.0 | STARTUP_FULL |
| 8.25 hr | DETECTION | 367.3 | 427.00 | 248.59 | -77.79 | ~100.0 | BUBBLE_AUTO |
| 8.50 hr | DRAIN | 338.0 | 414.51 | 249.42 | -382.61 | ~95.8 | BUBBLE_AUTO |
| 10.00 hr | DRAIN | 153.5 | 359.46 | 253.45 | -4.92 | ~74.3 | BUBBLE_AUTO |

Key observations:
- Pressure decline begins at DETECTION (8.25 hr) and accelerates during DRAIN
- T_pzr tracks T_sat(P) downward — confirming the ratchet mechanism
- dP/dt magnitude increases from -78 to -383 psi/hr as the loop accelerates
- By 10.00 hr, rate slows to -4.92 psi/hr because T_pzr is far below original operating temperature (flattening region of saturation curve at low pressures)
- Mass conservation error jumps to 17,447 lbm at 8.25 hr — indicating the bubble formation mass partition introduces accounting issues

---

## 4. Impact Analysis

### 4.1 Direct Impact

| Impact Area | Assessment |
|-------------|------------|
| Stage E validation | **FAILED** — Cannot validate bubble formation with collapsing pressure |
| DRAIN phase fidelity | **Broken** — Drain occurs under collapsing pressure instead of stable saturation conditions |
| CS-0036 (DRAIN duration) | **Likely same root cause** — Excessive drain duration may be caused by the same energy double-counting distorting steam generation rates and level response |
| Phase 0 exit | **BLOCKED** — CS-0043 is a Phase 0 critical-path item; Phase E cannot exit until resolved |
| Downstream plans | All Phase 1 items remain blocked (CS-0042 dashboard, operator framework) |

### 4.2 Scope of Required Changes

The fix is localized to the Regime 1 physics pipeline. The following files require modification:

| File | Change Required |
|------|----------------|
| `RCSHeatup.cs` | `IsolatedHeatingStep()` must detect two-phase condition (steam volume > 0) and route heater energy to latent heat path instead of subcooled dT |
| `HeatupSimEngine.cs` | Regime 1 branch (lines 1106-1195) must coordinate energy application — single path, not dual |
| `HeatupSimEngine.BubbleFormation.cs` | `UpdateDrainPhase()` steam generation must use the energy NOT consumed by `IsolatedHeatingStep`, or `IsolatedHeatingStep` must not consume it at all during two-phase |
| `PressurizerPhysics.cs` | `TwoPhaseHeatingUpdate()` may need revision or replacement with `ThreeRegionUpdate()` call during Regime 1 |

### 4.3 What Is NOT Affected

- Regime 0 (solid water, no bubble): Subcooled model is correct pre-bubble
- Regime 2/3 (RCPs running): Uses `BulkHeatupStep` + `ThreeRegionUpdate` — different code path
- CVCS mass accounting: Separate subsystem, not involved in this failure
- SG physics: Not active during Regime 1
- Canonical mass ledger architecture: Ledger itself is correct; the issue is energy routing, not mass tracking

---

## 5. Proposed Corrective Solution

### 5.1 Design Principle

**Single energy application per timestep.** When the PZR is in two-phase conditions (steam volume > 0), heater energy must be routed entirely to the latent heat path (steam generation at constant T_sat). The subcooled sensible-heat model (dT = Q/mCp) must be bypassed. Pressure must be derived from steam mass balance, not from Psat(capped temperature).

### 5.2 Proposed Architecture

```
IF (steamVolume > 0 AND bubblePhase IN {DRAIN, STABILIZE, PRESSURIZE}):
    // Two-phase energy path
    steamGenRate = heaterPower / h_fg(P)
    T_pzr = T_sat(P)                     // locked to saturation
    P = f(steam_mass, steam_volume, T)    // from steam mass balance or ThreeRegionUpdate
    // IsolatedHeatingStep skips PZR dT computation
    // UpdateDrainPhase uses steamGenRate for level change
ELSE:
    // Subcooled energy path (existing IsolatedHeatingStep logic)
    dT = Q_net / (m * Cp)
    T_pzr = T_pzr + dT
    P = thermal expansion model
```

### 5.3 Implementation Options

**Option A — Minimal: Bypass IsolatedHeatingStep PZR Branch During Two-Phase**

Add a guard in `IsolatedHeatingStep` that skips PZR dT computation when steam volume > 0. Let `UpdateDrainPhase` be the sole consumer of heater energy. Remove the Psat override in HeatupSimEngine.cs:1143 and let pressure be set by the bubble formation state machine.

- Pros: Smallest code change, eliminates double-counting directly
- Cons: Does not invoke the proper `ThreeRegionUpdate` model; pressure model in DRAIN remains simplified

**Option B — Moderate: Route Regime 1 Two-Phase Through ThreeRegionUpdate**

When bubble exists in Regime 1, invoke `PressurizerPhysics.ThreeRegionUpdate()` instead of the thermal expansion model. This gives proper flash evaporation, condensation, and steam mass balance. `UpdateDrainPhase` drain logic works with the proper pressure.

- Pros: Uses the correct physics model that already exists; proper pressure from steam balance
- Cons: `ThreeRegionUpdate` may need adaptation for Regime 1 conditions (no forced flow, different heat transfer coefficients)

**Option C — Full: Unified Energy Router**

Create an explicit energy router that determines the PZR energy path based on phase state (subcooled vs two-phase) before any physics method runs. All downstream consumers (`IsolatedHeatingStep`, `UpdateDrainPhase`, `TwoPhaseHeatingUpdate`) receive their energy allocation from the router rather than independently computing from raw heater power.

- Pros: Architectural clarity, eliminates all double-counting by construction, extensible
- Cons: Largest change scope, higher risk of regression, may be over-engineering for current needs

### 5.4 Recommended Approach

**Option B (Moderate)** is recommended. `ThreeRegionUpdate` already contains the correct two-phase physics. Routing Regime 1 two-phase conditions through it aligns with the principle of using validated physics models rather than ad-hoc overrides. The Psat(T_pzr) override at HeatupSimEngine.cs:1143 would be replaced by the pressure output from `ThreeRegionUpdate`.

Option A is acceptable as a minimum viable fix if Option B proves too complex for the current architecture.

Option C should be deferred to a future architecture release (aligns with v5.7.0.0 solver partition redesign in the roadmap).

---

## 6. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Fix introduces regression in Regime 0 (pre-bubble) | Low | High | Guard two-phase path on steamVolume > 0; Regime 0 path unchanged |
| Fix introduces regression in Regime 2/3 | Very Low | High | Regime 2/3 uses separate code path (BulkHeatupStep); not modified |
| `ThreeRegionUpdate` not validated for Regime 1 conditions | Medium | Medium | Run targeted unit scenarios: heater-only at 300-400 psia, no forced flow, verify stable pressure |
| Mass conservation impacted by energy path change | Low | Critical | Verify canonical ledger before/after fix; mass assertion at every timestep |
| CS-0036 (DRAIN duration) not fully resolved by this fix | Medium | Medium | DRAIN duration depends on drain rate model, not just energy model; may need separate tuning pass |
| Conduction/insulation loss model needs recalibration | Low | Low | Losses are small (~2% of heater power); recalibration can follow in a revision |

---

## 7. Relationship to Other Issues

| Issue | Relationship |
|-------|-------------|
| **CS-0036** (DRAIN duration excessive) | **Likely same root cause.** The 4-hour DRAIN duration is consistent with pressure collapse distorting the steam generation rate. Fixing CS-0043 may resolve or substantially improve CS-0036. |
| **CS-0026** (post-bubble pressure escalation) | Related — pressure behavior after bubble formation depends on the energy model being correct during bubble formation. |
| **CS-0029** (high pressure ramp, zero heat rate) | Related — may be the inverse symptom (pressure rises without heat) if energy routing is asymmetric between phases. |
| **CS-0034** (no equilibrium ceiling Regime 0/1) | Related energy model — the subcooled dT model in `IsolatedHeatingStep` is the same code path. The T_sat cap was intended as a ceiling but became a ratchet. |
| **CS-0040** (RVLIS stale during drain) | Independent — RVLIS display issue, not affected by pressure model fix. |

---

## 8. Conclusion

CS-0043 is a confirmed Critical defect with a clear root cause and a tractable fix path. The fundamental error is applying a subcooled liquid energy model to a two-phase system, then double-counting the heater energy through an independent steam generation calculation. The correct physics model (`ThreeRegionUpdate`) already exists in the codebase but is not invoked during Regime 1.

**Awaiting explicit authorization to implement corrective changes.**

---

## Document Control

| Field | Value |
|-------|-------|
| Report Author | Claude (AI Assistant) |
| Constitution Authority | Article III (Issue Registry), Article V (Investigation) |
| Authorization Required | `AUTHORIZED TO IMPLEMENT: CS-0043 corrective fix` |
| Mode | SPEC/DRAFT |
