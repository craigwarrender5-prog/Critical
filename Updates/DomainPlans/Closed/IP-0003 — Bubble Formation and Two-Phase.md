> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

---
Identifier: IP-0003
Domain: Primary Side â€” Bubble Formation State Machine / Two-Phase Energy Balance
Status: Open (Phase E FAILED â€” CS-0043 pressure boundary failure during bubble formation)
Priority: CRITICAL
Tier: 1
Linked Issues: CS-0024, CS-0025, CS-0026, CS-0027, CS-0028, CS-0029, CS-0030, CS-0036, CS-0043
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# IP-0003 â€” Bubble Formation and Two-Phase
## Bubble Formation and Two-Phase State Machine Corrections

---

## Document Control

| Field | Value |
|-------|-------|
| Identifier | **IP-0003** |
| Plan Severity | **CRITICAL** (recalculated: CS-0043 Critical) |
| Architectural Domain | Primary Side â€” Bubble Formation State Machine / Two-Phase Energy Balance |
| System Area | HeatupSimEngine.BubbleFormation, PressurizerPhysics, RCSHeatup, CoupledThermo, HeatupSimEngine |
| Discipline | Primary Thermodynamics â€” Two-Phase Regime |
| Status | **PHASE E FAILED â€” Pressure boundary collapse during bubble formation (CS-0043)** |
| Constitution Reference | PROJECT_CONSTITUTION.md, Articles V, III, X |

---

## Purpose

Correct the bubble formation state machine and two-phase energy balance coupling.
The investigation has identified **three root-cause families** that produce all seven
assigned issues. This plan specifies exact file/method ownership, line-level root
causes, fix designs with state machine rules and continuity constraints, and
quantitative acceptance tests tied to runtime logs.

---

## Architectural Domain Definition

| Field | Value |
|-------|-------|
| System Area | Primary Side â€” Bubble Formation / Two-Phase PZR Dynamics |
| Discipline | Two-Phase Thermodynamics, State Machine Logic |
| Architectural Boundary | Bubble detection logic, DRAIN/STABILIZE/PRESSURIZE state machine, Regime 1 isolated heating pressure model, PZR mass ledger during pre-RCP two-phase operation |

This domain is distinct from solid-regime physics (v0.2.0.0, resolved) and SG secondary physics (CS-0014 through CS-0020, separate domain).

---

## Issues Assigned

| Issue ID | Title | Severity | Root-Cause Family |
|----------|-------|----------|-------------------|
| CS-0024 | PZR 100% level, zero steam model may be clamping dynamics | Medium | A: Solid-Regime Clamping |
| CS-0025 | Bubble detection threshold alignment (validation item) | Low | A: Solid-Regime Clamping |
| CS-0026 | Post-bubble pressure escalation magnitude questionable | Medium-High | B: Regime 1 Pressure Model |
| CS-0027 | Bubble phase labeling inconsistent with observed thermodynamics | Medium | C: State Machine Logic |
| CS-0028 | Bubble flag/state timing inconsistent with saturation and pressure rise | High | C: State Machine Logic |
| CS-0029 | Very high pressure ramp while RCS heat rate near zero | Medium-High | B: Regime 1 Pressure Model |
| CS-0030 | Nonlinear/inconsistent sensitivity of pressure to CVCS sign changes | Medium | B: Regime 1 Pressure Model |
| CS-0043 | Pressurizer pressure boundary failure during bubble formation | **Critical** | D: Two-Phase Energy Partition |

**Plan Severity:** CRITICAL (highest assigned issue = CS-0043 Critical)

---

## Scope

This plan SHALL:

- Fix mass ledger maintenance during Regime 1 (isolated heating + DRAIN phase)
- Replace the IsolatedHeatingStep pressure model with saturation-derived pressure
- Correct bubble phase state machine transition conditions
- Verify PZR level/steam clamping in solid regime is physically appropriate
- Ensure pressure-CVCS sensitivity scales correctly in two-phase regime
- Ensure `bubbleFormed` flag timing aligns with thermodynamic state

This plan shall NOT:

- Modify solid-regime SolidPlantPressure physics (v0.2.0.0, resolved)
- Modify the SG secondary model (separate domain CS-0014 through CS-0020)
- Modify canonical mass conservation architecture (v0.1.0.0, resolved)
- Change UI/validation infrastructure
- Modify primary mass ledger structure or authority rules

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| v0.1.0.0 (Primary Mass Conservation) | Prerequisite | Canonical mass enforcement must be active. **SATISFIED.** |
| v0.2.0.0 (Solid Regime Pressure) | Prerequisite | Solid-regime transport delay and PI controller must be stable. **SATISFIED.** |
| SG Secondary Physics (CS-0014â€“0020) | Not required | SG issues are independent; they affect late-heatup behavior (>16 hr) but not bubble formation or Regime 1 two-phase. |
| RCP Thermal Inertia (CS-0031) | Not required | RCP heat rate is downstream of bubble formation. |

---

# SECTION 1: FILE/METHOD OWNERSHIP

## 1.1 Files in Scope

| File | Ownership | Modifications Expected |
|------|-----------|----------------------|
| `HeatupSimEngine.BubbleFormation.cs` | **Primary** â€” state machine logic | Fix DRAINâ†’STABILIZEâ†’PRESSURIZE transitions; add continuity guards |
| `HeatupSimEngine.cs` (main) | **Primary** â€” Regime 1 branch (lines 1106-1137) | Mass ledger updates during Regime 1; pressure model replacement |
| `RCSHeatup.cs` | **Secondary** â€” IsolatedHeatingStep (lines 290-362) | Pressure model correction (saturation-derived) |
| `PressurizerPhysics.cs` | **Read-only investigation** | Validate two-phase P-V-T coupling used by CoupledThermo |
| `PlantConstants.Pressure.cs` | **Minor** â€” Constants for transition thresholds | May add/adjust bubble phase timing constants |
| `HeatupSimEngine.Logging.cs` | **Minor** â€” Forensic logging | Add regime-1 mass ledger audit logging |

## 1.2 Files NOT in Scope

| File | Reason |
|------|--------|
| `SolidPlantPressure.cs` | Resolved in v0.2.0.0 |
| `SGMultiNodeThermal.cs` | SG domain (CS-0014 through CS-0020) |
| `CoupledThermo.cs` | Conservation architecture resolved in v0.1.0.0; canonical mass enforcement is correct |
| `CVCSController.cs` | CVCS controller logic is correct; only its inputs change |
| `HeatupSimEngine.Init.cs` | No initialization changes expected |
| UI files | Out of scope |

---

# SECTION 2: ROOT-CAUSE EVIDENCE (LINE-LEVEL)

## Root-Cause Family A: Solid-Regime PZR Clamping (CS-0024, CS-0025)

### Evidence

**Log data (Build/HeatupLogs):** From 0.00 hr to 8.25 hr, PZR Level = 100.0%, PZR Water Vol = 1800.0 ftÂ³, PZR Steam Vol = 0.0 ftÂ³. During this entire period, heaters are running at 1.8 MW heating water toward Tsat.

**Root cause location:** `SolidPlantPressure.cs` and `HeatupSimEngine.cs:1085-1090`

During solid operations, the PZR is physically water-solid (correct per plant design â€” the solid plant regime IS water-full by definition). The PZR level clamping at 100% is **physically correct** for a solid, water-full pressurizer. Thermal expansion of the PZR water exits through the surge line (reflected in the rising Surge Flow values: 1.2â†’8.2 gpm across the solid regime).

**CS-0024 Assessment: The 100% level / zero steam is physically correct for solid plant operations.** The expansion is accommodated by surge flow into the RCS, not by creating steam volume. The solid-regime physics module correctly handles this via the PI-controlled CVCS balance (v0.2.0.0 transport delay fix).

**CS-0025 Assessment: Bubble detection triggers at T_pzr = 435.43Â°F vs T_sat = 435.83Â°F** (0.4Â°F subcooling). This is within the correct threshold per `SolidPlantPressure.cs` detection logic. The detection is functioning correctly.

### Conclusion

CS-0024 and CS-0025 are **non-issues** in the current codebase â€” the observed behavior is physically correct. They should be **reclassified as Validated-Correct** and closed.

---

## Root-Cause Family B: Regime 1 Pressure Model and Mass Ledger Gap (CS-0026, CS-0029, CS-0030)

### Evidence

**THE PRIMARY ROOT CAUSE OF THIS DOMAIN.**

**Log data:** At bubble detection (8.25 hr), mass conservation error jumps from 49 lbm to **17,447 lbm**. Error then grows **linearly** through the entire DRAIN phase: 13,933 â†’ 49,384 â†’ 50,158 lbm by 12.00 hr. This is a **~50,000 lbm mass error** â€” roughly 6% of total primary mass.

**Root cause 1 â€” Mass ledger not maintained in Regime 1:**

`HeatupSimEngine.cs` lines 1106-1137 (Regime 1 isolated path): After `solidPressurizer = false`, the engine enters the `else` branch which calls `RCSHeatup.IsolatedHeatingStep()`. This method:

- Takes only thermal parameters (T_pzr, T_rcs, pressure, pzrHeaterPower, pzrWaterVolume, pzrHeatCap, rcsHeatCap, dt)
- Returns only thermal results (T_pzr, T_rcs, Pressure, SurgeFlow)
- **Does NOT receive or update `physicsState.TotalPrimaryMass_lb`**
- **Does NOT update `physicsState.RCSWaterMass`, `PZRWaterMass`, or `PZRSteamMass`**

Meanwhile, `UpdateBubbleFormation()` in the DRAIN phase (`BubbleFormation.cs:313-487`) directly mutates `physicsState.PZRWaterMass`, `PZRSteamMass`, and `RCSWaterMass` via:
- Phase change: waterâ†’steam (line 391-392)
- CVCS drain: PZR waterâ†’RCS water (line 396-397)

But `physicsState.TotalPrimaryMass_lb` is **never updated** during Regime 1. The solid-regime path updates it (line 1090), and the coupled-regime paths (R2/R3) update it via CVCS pre-apply. But the Regime 1 path has **no ledger mutation**.

**Root cause 2 â€” IsolatedHeatingStep pressure model uses thermal expansion, not saturation:**

`RCSHeatup.cs:346-351`: The isolated heating path computes pressure as:
```
dP = ThermalExpansion.PressureChangeFromTemp(pzrDeltaT, T_pzr, pressure)
result.Pressure = pressure + dP * 0.5f  (DAMPING_FACTOR)
```

This is a **subcooled-liquid thermal expansion** model being applied to a **two-phase saturated system** during DRAIN. Once a steam bubble exists, pressure should be governed by **saturation thermodynamics**: P = Psat(T_pzr). The thermal expansion dP formula is appropriate for solid/subcooled conditions but wrong for two-phase.

This explains CS-0026 (pressure escalation magnitude), CS-0029 (high pressure ramp while RCS rate ~zero â€” the PZR heats independently at ~44Â°F/hr and pressure tracks thermal expansion, not saturation), and CS-0030 (CVCS doesn't affect pressure through mass-volume coupling because the pressure model doesn't use mass at all).

**Quantitative evidence from logs:**

| Sim Time | Pressure (psia) | T_pzr (Â°F) | Psat(T_pzr) | Delta |
|----------|-----------------|------------|-------------|-------|
| 8.25 | 365.1 | 435.43 | ~365 | ~0 |
| 8.50 | 377.3 | 438.97 | ~371 | +6.3 |
| 9.00 | 443.9 | 455.15 | ~438 | +5.9 |
| 10.00 | 581.6 | 483.31 | ~558 | +23.6 |
| 11.00 | 730.3 | 508.24 | ~691 | +39.3 |
| 12.00 | 891.9 | 531.01 | ~837 | +54.9 |

Pressure systematically **exceeds** Psat(T_pzr) by a growing margin, confirming the thermal-expansion model overshoots the saturation curve. The 0.5Ã— damping partially masks this but doesn't correct the fundamental model error.

### Conclusion

The Regime 1 path needs: (1) mass ledger maintenance, (2) saturation-based pressure model.

---

## Root-Cause Family C: State Machine Logic (CS-0027, CS-0028)

### Evidence

**CS-0028 (flag timing):** The `bubbleFormed` flag stays `false` during the entire DRAIN phase (by design â€” see `BubbleFormation.cs:108`). It becomes `true` only when PRESSURIZE completes at P â‰¥ 320 psig (`BubbleFormation.cs:503`). This is a **deliberate design choice** that gates RCP startup: RCPs cannot start until pressure is sufficient for NPSH.

However, the Regime selection gate at `HeatupSimEngine.cs:1045` checks:
```
if ((solidPressurizer && !bubbleFormed) || bubblePreDrainPhase)
```

During DRAIN: `solidPressurizer=false`, `bubbleFormed=false`, `bubblePreDrainPhase=false`.
This means the engine falls through to the `else` branch (Regime 1 isolated) at line 1106.

**This is architecturally correct** â€” DRAIN is intentionally routed through Regime 1 because no RCPs are running. The issue is not the routing but the **Regime 1 physics model** being wrong for two-phase (Root-Cause Family B).

**CS-0027 (phase labeling):** The log shows `Bubble Phase: DRAIN` from 8.50 hr through 12.00 hr while pressure rises from 377 to 892 psia. The DRAIN label is correct â€” the PZR IS draining (level 96%â†’26%). The confusion arises because the pressure is rising simultaneously, which looks like PRESSURIZE behavior. But per NRC HRTD 19.2.2, pressure DOES rise during drain because heaters continue converting water to steam, and the steam compresses the shrinking water volume.

The **label is correct**; the **pressure magnitude is wrong** (Root-Cause Family B again). Once the pressure model is fixed, the labeling will appear consistent.

However, there IS a genuine logic gap: the DRAINâ†’STABILIZE transition is gated purely on level (`BubbleFormation.cs:463`):
```
if (pzrLevel <= PlantConstants.PZR_LEVEL_AFTER_BUBBLE + 0.5f)
```

It does NOT check that the system has reached thermodynamic equilibrium. A continuity guard should verify that pressure rate and level rate are within expected bounds before transitioning.

### Conclusion

CS-0028 is by-design (bubbleFormed gates RCP start, not physics routing). CS-0027 requires minor transition guards but the labels are fundamentally correct.

---

# SECTION 3: FIX DESIGN

## Fix 3.1: Regime 1 Mass Ledger Maintenance

**Location:** `HeatupSimEngine.cs` lines 1106-1137 (Regime 1 else-branch)

**Current behavior:** After `IsolatedHeatingStep()` returns, the engine updates T_pzr, T_rcs, pressure, surgeFlow, T_sat â€” but does NOT update mass fields. The canonical ledger `TotalPrimaryMass_lb` is never mutated during Regime 1.

**Constitutional principle (v0.1.0.0 Article III):** The ledger is the sole mass authority. It is mutated incrementally by boundary flows. The component sum is a diagnostic assertion, never the source of the ledger value. This distinction is load-bearing â€” inverting it (deriving ledger from components) would regress the v0.1.0.0 architecture.

**Fix design:**

### Step 1: Incremental ledger mutation by boundary flows (CVCS)

After the existing IsolatedHeatingStep call and thermal updates (after line 1126), add CVCS boundary flow ledger mutation â€” same pattern as R2/R3:

- Compute `netCVCS_gpm = chargingFlow - letdownFlow`
- Compute `cvcsNetMass_lb = netCVCS_gpm * dt_sec * GPM_TO_FT3_SEC * rhoWater`
- **Mutate `physicsState.TotalPrimaryMass_lb += cvcsNetMass_lb`** (INV-2: boundary flows enter through ledger)
- Increment boundary accumulators (`CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`)
- Feed to VCT tracking

This is the ONLY site where the ledger changes in Regime 1.

### Step 2: Component masses mutated by physics (NO ledger derivation)

The bubble formation state machine (UpdateDrainPhase) already mutates component masses correctly:
- Phase change: `PZRWaterMass -= dm`, `PZRSteamMass += dm` (internal, conserves PZR total)
- CVCS drain: `PZRWaterMass -= dm`, `RCSWaterMass += dm` (internal redistribution, conserves system total)

These are internal transfers â€” they redistribute mass among components but do NOT cross the primary system boundary. The ledger does not change from internal transfers. This is correct.

### Step 3: Post-step diagnostic assertion (NOT ledger derivation)

After all physics and state machine updates complete for this timestep:

```
float componentSum = physicsState.RCSWaterMass + physicsState.PZRWaterMass + physicsState.PZRSteamMass;
float ledgerDrift = componentSum - physicsState.TotalPrimaryMass_lb;
if (Mathf.Abs(ledgerDrift) > 1.0f)
    Debug.LogWarning($"[R1 MASS AUDIT] Ledger drift = {ledgerDrift:F1} lbm (component sum vs ledger)");
```

This assertion detects bugs. It does NOT correct them â€” the ledger remains authoritative.

### Step 4: Guard against CVCS double-counting

The DRAIN phase has its own CVCS flow logic (`BubbleFormation.cs:369-372`) which mutates component masses directly (PZR water â†’ RCS water). This is an internal redistribution, not a boundary flow.

However, `UpdateRCSInventory()` in the post-physics path must be prevented from re-applying CVCS:
- Set `regime3CVCSPreApplied = true` when Regime 1 CVCS ledger mutation runs
- This reuses the existing double-count guard from R2/R3

### Authority ownership summary

| Field | Authority in Regime 1 | Mutated by |
|-------|----------------------|------------|
| `TotalPrimaryMass_lb` | **LEDGER (engine)** | CVCS boundary flow only (Step 1) |
| `RCSWaterMass` | **Physics (BubbleFormation)** | CVCS internal redistribution, surge |
| `PZRWaterMass` | **Physics (BubbleFormation)** | Phase change, CVCS drain |
| `PZRSteamMass` | **Physics (BubbleFormation)** | Phase change |
| Component sum vs ledger | **Diagnostic assertion** | Never overwrites ledger |

**Continuity constraint:** `|TotalPrimaryMass_lb - (RCSWaterMass + PZRWaterMass + PZRSteamMass)| < 1.0 lbm` at every timestep â€” as an assertion, not a correction.

## Fix 3.2: Saturation-Based Pressure Model for Two-Phase Regime 1

**Location:** `HeatupSimEngine.cs` Regime 1 branch (lines 1106-1137)

**Current behavior:** Pressure = previous + ThermalExpansion.PressureChangeFromTemp Ã— 0.5 (subcooled-liquid model applied to two-phase system)

**Guard condition design:**

The override guard must use **state machine authority**, not derived volume fields. `pzrSteamVolume > 0` is fragile â€” it's a display-level derivative updated by the DRAIN phase, not the authoritative state indicator.

The bubble phase enum (`BubbleFormationPhase`) is the sole authority on whether two-phase conditions exist. The phases that reach the Regime 1 else-branch with real steam present are DRAIN, STABILIZE, and PRESSURIZE. (DETECTION and VERIFICATION route through the solid-plant branch via `bubblePreDrainPhase = true` at line 1045 and never reach Regime 1.)

**Fix design (engine-level override, state-machine-gated):**

After `pressure = isoResult.Pressure` (line 1117), add:

```
// Two-phase pressure override: P = Psat(T_pzr) when state machine
// confirms active two-phase operation. Guard uses bubble phase enum
// (state machine authority), not derived volume fields.
bool twoPhaseActive = bubblePhase == BubbleFormationPhase.DRAIN
                   || bubblePhase == BubbleFormationPhase.STABILIZE
                   || bubblePhase == BubbleFormationPhase.PRESSURIZE;
if (twoPhaseActive)
{
    pressure = WaterProperties.SaturationPressure(T_pzr);
}
```

**Rationale:** IsolatedHeatingStep correctly computes T_pzr (PZR temperature rise from heater power). The only error is computing dP from thermal expansion instead of from saturation. The fix is minimal: replace the pressure output with Psat(T_pzr), gated by the state machine that owns the two-phase transition.

**Why not `pzrSteamVolume > 0` or `pzrSteamMass > 0`:** These are downstream derivatives of the physics â€” they reflect the consequence of two-phase operation, not its authoritative declaration. Using them as guards creates a circular dependency (physics output gates physics model selection). The bubble phase enum is set by explicit state machine transitions and is the correct authority.

**Why not `!solidPressurizer && bubbleDrainActive`:** `bubbleDrainActive` is only true during DRAIN, missing STABILIZE and PRESSURIZE which also need saturation pressure.

**Continuity constraint:** At the DRAIN phase entry (first timestep after VERIFICATIONâ†’DRAIN transition), pressure must be continuous. This is naturally satisfied because T_pzr â‰ˆ T_sat at that moment (heaters brought T_pzr to saturation during solid ops), so Psat(T_pzr) â‰ˆ current pressure. No step discontinuity.

## Fix 3.3: State Machine Transition Guards

**Location:** `HeatupSimEngine.BubbleFormation.cs` UpdateDrainPhase (line 462-466)

**Current behavior:** DRAINâ†’STABILIZE transitions when `pzrLevel <= target + 0.5%`

**Fix design:** Add supplementary continuity checks:

```
bool levelReached = pzrLevel <= PlantConstants.PZR_LEVEL_AFTER_BUBBLE + 0.5f;
bool pressureStable = Mathf.Abs(pressureRate) < MAX_DRAIN_EXIT_PRESSURE_RATE;  // e.g., 50 psi/hr
bool drainComplete = levelReached && pressureStable;
```

If `levelReached` but NOT `pressureStable`, log a warning but still transition (avoid infinite DRAIN). The pressure stability check is advisory, not blocking.

**Location:** `HeatupSimEngine.BubbleFormation.cs` UpdatePressurizePhase (line 499)

**Current behavior:** PRESSURIZEâ†’COMPLETE when `pressure_psig >= MIN_RCP_PRESSURE_PSIG`

**Fix design:** Add PZR level stability guard:

```
bool pressureSufficient = pressure_psig >= minRcpP_psig;
bool levelStable = pzrLevel >= PlantConstants.PZR_LEVEL_AFTER_BUBBLE - 2f;  // within 2% of target
bool formationComplete = pressureSufficient && levelStable;
```

This prevents completing bubble formation if level has drifted far from target.

## Fix 3.4: Close CS-0024 and CS-0025 as Validated-Correct

**No code changes required.** Update Issue Registry entries with validation evidence from this investigation:

- CS-0024: PZR 100%/zero-steam is physically correct for solid plant operations. Expansion exits via surge flow.
- CS-0025: Detection at T_pzr = 435.4Â°F vs T_sat = 435.8Â°F is correct (0.4Â°F subcooling margin).

---

# SECTION 4: QUANTITATIVE ACCEPTANCE TESTS

All tests are evaluated against runtime interval logs (Build/HeatupLogs/).

## Test AT-BF-001: Mass Conservation Through Bubble Detection Transition

**Metric:** `|TotalPrimaryMass_lb error|` at the interval immediately after bubble detection
**Acceptance:** `< 100 lbm` (current: 17,447 lbm â€” **FAIL**)
**Log field:** MASS CONSERVATION section â†’ Error (lbm)
**Test interval:** First interval where `Bubble Phase â‰  NONE`

## Test AT-BF-002: Mass Conservation During DRAIN Phase

**Metric:** `|TotalPrimaryMass_lb error|` maximum during entire DRAIN phase
**Acceptance:** `< 500 lbm` (current: 50,158 lbm â€” **FAIL**)
**Log field:** MASS CONSERVATION section â†’ Error (lbm), checked across all intervals with `Bubble Phase = DRAIN`
**Note:** The 500 lbm threshold accommodates normal floating-point accumulation across ~4 hr of DRAIN.

## Test AT-BF-003: Pressure Tracks Saturation During Two-Phase Operation

**Metric:** `|Pressure - Psat(T_pzr)|` during all intervals where `Bubble Formed = NO` and `Solid Pressurizer = NO`
**Acceptance:** `< 5.0 psi` (current: up to 54.9 psi â€” **FAIL**)
**Log field:** Computed from logged Pressure and T_pzr via offline Psat lookup
**Note:** Small deviations acceptable due to dynamic thermal lag and aux spray effects.

## Test AT-BF-004: Pressure Tracks Saturation in Coupled Regime

**Metric:** `|Pressure - Psat(T_pzr)|` during all intervals where `Bubble Formed = YES` and `Physics Regime = REGIME 2 or 3`
**Acceptance:** `< 2.0 psi` (current: ~0 psi â€” already passes in R2/R3 since T_pzr = T_sat by construction)
**Log field:** Computed from logged Pressure and T_pzr
**Note:** This test guards against regression.

## Test AT-BF-005: No Pressure Discontinuity at Bubble Detection

**Metric:** `|Pressure[detection interval] - Pressure[previous interval]|`
**Acceptance:** `< 15.0 psi` (current: ~0.0 psi â€” **PASS**, guard against regression)
**Log field:** Pressure psia at consecutive intervals around detection

## Test AT-BF-006: Mass Conservation Through RCP Start (Regime 1â†’2 Transition)

**Metric:** `|TotalPrimaryMass_lb error|` change across the Regime 1â†’2 boundary
**Acceptance:** Error change `< 1000 lbm` in single interval (current: error drops ~36,000 lbm â€” indicates the R2 rebase compensates for R1 drift â€” **FAIL** on root cause even if R2 recovers)
**Log field:** MASS CONSERVATION section â†’ Error at last R1 interval vs first R2 interval
**Note:** After fix, error should be small in R1 so R2 rebase produces minimal change.

## Test AT-BF-007: DRAIN Phase Level Decreases Monotonically

**Metric:** PZR Level at consecutive DRAIN intervals should be non-increasing (within 0.5% tolerance for PI oscillation)
**Acceptance:** `Level[n+1] <= Level[n] + 0.5%` for all consecutive DRAIN intervals (current: **PASS** â€” level decreases 96.4% â†’ 25.5%)
**Log field:** PZR Level (%)
**Note:** Regression guard.

## Test AT-BF-008: Bubble Formation Completes Within Expected Time

**Metric:** Total time from DETECTION to COMPLETE
**Acceptance:** 2.5 to 5.0 hours (current: ~4.0 hr, 8.25 â†’ 12.25 â€” **PASS**)
**Log field:** Sim Time at first `Bubble Phase = DETECTION` and first `Bubble Phase = COMPLETE`
**Note:** NRC HRTD 19.2.2 reference: ~60 min for drain alone; total procedure 2-4 hours. Current timing is reasonable.

---

# IMPLEMENTATION STAGES

## Phase A: Investigation and Design (THIS DOCUMENT)
- Status: **COMPLETE**
- Deliverables: File/method ownership, root-cause evidence, fix design, acceptance tests
- **Awaiting authorization to proceed to Phase B**

## Phase B: Mass Ledger Fix (Regime 1)
- Fix 3.1: Add mass ledger maintenance to Regime 1 path
- Fix 3.4: Close CS-0024, CS-0025 in Issue Registry
- Acceptance: AT-BF-001, AT-BF-002, AT-BF-006
- Files modified: `HeatupSimEngine.cs` (Regime 1 branch), `HeatupSimEngine.BubbleFormation.cs` (CVCS guard)

## Phase C: Saturation Pressure Model
- Fix 3.2: Replace thermal-expansion pressure with Psat(T_pzr) in two-phase Regime 1
- Acceptance: AT-BF-003, AT-BF-004, AT-BF-005
- Files modified: `HeatupSimEngine.cs` (Regime 1 branch, post-IsolatedHeatingStep)

## Phase D: State Machine Transition Guards
- Fix 3.3: Add DRAINâ†’STABILIZE and PRESSURIZEâ†’COMPLETE continuity guards
- Acceptance: AT-BF-007, AT-BF-008
- Files modified: `HeatupSimEngine.BubbleFormation.cs` (UpdateDrainPhase, UpdatePressurizePhase)

## Phase E: Validation and Documentation â€” **FAILED**
- Full heatup run (0 â†’ 13.25 hr) â€” **FAILED at bubble formation**
- **Failure:** Pressurizer does not pressurize during DRAIN/STABILIZE/PRESSURIZE phases. Pressure collapses from 368 psia to 154 psia despite heaters at 1.8 MW. Steam volume present (805 ftÂ³ at 10.00 hr), yet system pressure drops monotonically.
- **Root Cause:** CS-0043 â€” IsolatedHeatingStep thermal model double-counts heater energy: applies Q/mCp temperature rise to PZR water that is already at T_sat, while BubbleFormation.cs independently generates steam from the same heater power via h_fg. The T_sat cap in IsolatedHeatingStep ratchets T_pzr downward each timestep, and the Psat(T_pzr) override tracks the falling T_pzr, creating a runaway depressurization spiral.
- **New Issue Filed:** CS-0043 (Critical)
- **Acceptance Test Results:** AT-BF-003 FAIL (pressure does not track saturation â€” it falls below Psat), AT-BF-001/002 FAIL (mass conservation errors persist ~17,000 lbm)
- Phase E will be re-run after CS-0043 is resolved

---

## Known Limitations

| Limitation | Impact |
|-----------|--------|
| SG secondary issues (CS-0014 through CS-0020) remain unresolved | Late-heatup behavior (>16 hr) will still show SG-related anomalies. These are outside this domain. |
| RCP thermal inertia (CS-0031) unresolved | Heatup rate after RCP start may be numerically aggressive. Outside this domain. |
| No automated test harness (CS-0011 deferred) | Acceptance tests require manual log inspection. |
| IsolatedHeatingStep thermal model remains subcooled | The T_pzr heating calculation uses subcooled specific heat. In two-phase, heater energy should go to latent heat (steam generation), which is handled by UpdateDrainPhase. The engine-level Psat override (Fix 3.2) corrects pressure without modifying the thermal model. |

---

## Out-of-Domain Findings

During investigation, no new out-of-domain issues were discovered beyond those already registered in the Issue Registry.

