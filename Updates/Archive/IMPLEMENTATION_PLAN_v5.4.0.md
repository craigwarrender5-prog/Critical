# Implementation Plan v5.4.0 — Primary Mass & Pressurizer Stabilization Release

**Version:** v5.4.0  
**Date:** 2026-02-12  
**Phase:** Core Physics Stabilization  
**Priority:** CRITICAL — Blocks further thermodynamic modules including SG boiling and UI stabilization  
**Predecessor:** v5.3.0 (validation failed), v5.3.1 (deprecated, folded into this release)  

---

## 0. Executive Summary

This release addresses six interconnected physics issues that prevent reliable simulation across operating regimes. Rather than patching individual symptoms, v5.4.0 establishes a unified architectural foundation for mass conservation that will support all future thermodynamic work.

**This plan is PREPARED but NOT APPROVED for execution. No code changes until explicitly authorized.**

---

## 1. Problem Statement

### Issue #1 — Bubble Formation Drain Semantics

**Observed Behavior:**
- Current drain operations take approximately **2 hours** to achieve expected level changes
- This is unrealistically slow for normal pressurizer operations

**Root Cause Analysis:**
- Steam boiloff during bubble formation is being treated as **liquid shrinkage** rather than **volume expansion**
- When the bubble forms, the physics model reduces liquid mass to "make room" for steam
- Correct semantics: steam volume **grows into** the available space; liquid is **displaced** into the RCS loops, not destroyed

**Physical Reality:**
- As pressure drops, saturation temperature drops
- Liquid at the prior saturation temperature is now superheated
- Flash evaporation occurs: liquid → steam
- Steam volume increases, displacing liquid downward
- Liquid flows out of pressurizer through surge line
- **Total mass in pressurizer decreases** because liquid is being displaced to the RCS, not because it's disappearing

**Current Code Behavior (Incorrect):**
```
Steam mass increases by Δm_steam
Liquid mass decreases by Δm_steam  ← WRONG: This treats it as phase change in place
PZR level drops because liquid "shrank"
```

**Correct Behavior:**
```
Steam volume increases by ΔV_steam
Liquid is displaced: Δm_displaced = ρ_liquid × ΔV_steam
Liquid flows to RCS through surge line
PZR water mass decreases by Δm_displaced
RCS water mass increases by Δm_displaced
Total primary mass unchanged (just redistributed)
```

---

### Issue #2 — Inventory Discontinuity Across Regimes

**Observed Behavior:**
- Mass conservation validation **PASSES** in solid regime (Regime 1)
- Mass conservation validation **FAILS** in two-phase regimes (Regime 2/3)
- At Sim Time 10.25 hr, interval log shows "Mass Conservation: FAIL"

**Root Cause Analysis:**
- The v5.0.2 fix established canonical mass tracking for solid operations
- The v5.3.0 implementation attempted to extend this to two-phase but the solver overwrites boundary-corrected values
- `CoupledThermo.SolveEquilibrium()` recalculates total mass from V×ρ instead of accepting the canonical ledger as constraint

**The Violation:**
```csharp
// WHAT HAPPENS (incorrect):
// 1. Boundary flows update TotalPrimaryMass_lb correctly
// 2. Solver is called with TotalPrimaryMass_lb parameter
// 3. Solver ignores parameter and computes M_total = V_RCS × ρ_RCS + V_PZR × ρ_PZR
// 4. Computed mass differs from ledger due to density/volume discretization
// 5. Conservation check fails because ledger ≠ component sum
```

**Architectural Requirement:**
- The canonical ledger `TotalPrimaryMass_lb` must be the **sole source of truth**
- Solvers must **accept** this value and compute component distribution to match
- RCS mass must be **derived** as: `M_RCS = M_total - M_PZR_water - M_PZR_steam`

---

### Issue #3 — RVLIS Level Inconsistency (~88% Drop)

**Observed Behavior:**
- During drain operations, RVLIS level indicator drops to ~88%
- This occurs even when no actual boundary mass loss has occurred
- The indication recovers later, suggesting it's a stale data issue

**Root Cause Analysis:**
- `RCSWaterMass` field is not being updated during drain phases
- RVLIS calculation reads this stale value
- The pressurizer is draining liquid to the RCS, but `RCSWaterMass` doesn't reflect the incoming mass

**Data Flow Problem:**
```
Pressurizer drains: PZRWaterMass decreases by 1000 lb
Liquid flows to RCS through surge line
RCSWaterMass should increase by 1000 lb  ← NOT HAPPENING
RVLIS reads RCSWaterMass → shows low level
```

**Fix Requirement:**
- When pressurizer drains, the displaced mass must be added to `RCSWaterMass`
- Or: `RCSWaterMass` must be computed as remainder: `M_total - M_PZR_water - M_PZR_steam`
- The remainder approach guarantees conservation by construction

---

### Issue #4 — PZR Level Spike on RCP Start

**Observed Behavior:**
- When RCPs start, pressurizer level shows a sharp upward spike
- The spike is transient (one or few frames) then settles
- Magnitude appears to be > 0.5% per timestep

**Root Cause Analysis (Hypotheses):**

**Hypothesis A — Canonical Overwrite:**
- RCP start triggers a regime change or state recalculation
- The recalculation overwrites `TotalPrimaryMass_lb` with a V×ρ computation
- The new value differs from the boundary-corrected ledger
- Level indication reflects the discontinuity

**Hypothesis B — Density Ordering Issue:**
- RCP start causes rapid temperature/pressure changes
- Density is updated before or after volume in the same timestep
- The mismatch creates a momentary mass imbalance
- Level is computed from the inconsistent state

**Hypothesis C — Thermal Expansion Double-Counting:**
- RCP heat addition causes thermal expansion
- Expansion is applied to both RCS and PZR independently
- The total expansion is double-counted
- Pressurizer level spikes because both systems "expanded into" it

**Investigation Required:**
- Add diagnostic logging at RCP start
- Track `TotalPrimaryMass_lb`, component masses, and level calculation frame-by-frame
- Identify which frame shows the discontinuity and what changed

---

### Issue #5 — VCT Conservation Error Growth (~1,600–1,700 gal)

**Observed Behavior:**
- Dashboard shows VCT CONS ERR ~1.7k gal (failing) at 15.75 hr simulation time
- Interval log at 15.75 hr shows VCT "Conservation Err" = 1,667.05 gal
- This represents a **major failure mode** alongside primary mass conservation

**Root Cause Hypothesis:**
- VCT internal volume balance may be correct
- However, the VCT conservation verifier depends on an **RCS-side inventory change term** (`rcsInventoryChange_gal` or accumulated RCS delta)
- If the primary-side accounting is drifting or being overwritten (Issues #2, #3), the VCT verifier will fail **even if VCT flows are correct**
- This is a **cascading failure** — the VCT error is likely a symptom of primary mass drift, not an independent bug

**The VCT Verification Equation (Expected Form):**
```
conservationError = |vctChange + rcsChange - externalNet|

where:
  vctChange     = VCT_Volume_gal - VCT_InitialVolume_gal
  rcsChange     = accumulated RCS inventory change (gal)
  externalNet   = (ExternalIn_gal - ExternalOut_gal)  // makeup, bleed, etc.
```

**If `rcsChange` is computed from a drifting/overwritten mass source, the equation fails even with correct VCT accounting.**

**Diagnosis Required (Must Be Performed Before Any Fix):**

1. **Locate the exact VCT verification equation:**
   - Find `VCTPhysics.VerifyMassConservation(...)` or equivalent
   - Find the engine call site that supplies the RCS change term
   - Document the exact equation used

2. **Add instrumentation when `VCT_CONS_ERR > 100 gal`:**
   ```csharp
   // Log individual components of the verification equation:
   Debug.Log($"[VCT_DIAG] vctChange={vctChange_gal:F2} " +
             $"rcsChange={rcsChange_gal:F2} " +
             $"externalNet={externalNet_gal:F2} " +
             $"computedError={computedError:F2}");
   Debug.Log($"[VCT_DIAG] CumIn={cumIn_gal:F2} CumOut={cumOut_gal:F2} " +
             $"CumExtIn={cumExtIn_gal:F2} CumExtOut={cumExtOut_gal:F2}");
   ```

3. **Determine root cause category:**
   - **A) Incorrect RCS delta term** — Most likely, due to primary canonical drift (Issues #2/#3)
   - **B) Real VCT flow accounting bug** — Double-count or missed flow path
   - **C) Unit conversion / dt mismatch** — lb vs gal, per-second vs per-frame accumulation

4. **Propose minimal fix aligned with canonical ledger approach:**
   - The RCS change term must derive from the **canonical ledger** (`TotalPrimaryMass_lb`), not from V×ρ calculations
   - No new second sources of truth
   - VCT verifier should read: `rcsChange_gal = (InitialPrimaryMass_lb - TotalPrimaryMass_lb) / 8.34`

---

### Issue #6 — SG Boiling Does Not Pressurize Secondary (Pressure Pinned Near Atmospheric)

**Observed Behavior:**
- During "BOILING (100%)" state, SG secondary pressure remains ~3 psig (~17 psia)
- Corresponding T_sat is only ~220°F despite large SG heat transfer occurring
- Pressure does not rise even though steam is supposedly being generated

**Expected Behavior:**
- If the SG is isolated (closed steam space with no outlet path), boiling should:
  1. Generate steam mass from liquid evaporation
  2. Accumulate steam inventory in the fixed steam-space volume
  3. Drive pressure upward as steam density increases
- Pressure should rise until either:
  - A steam outlet opens (dump valves, relief, atmospheric vent)
  - Equilibrium is reached with heat removal

**Root Cause Hypotheses:**

**Hypothesis A — Pressure Clamped from T_sat (Not Inventory-Based):**
- Current logic may compute: `P_secondary = P_sat(T_hottest_node)`
- This couples pressure directly to temperature, ignoring steam mass accumulation
- Would explain why pressure tracks temperature but doesn't rise independently

**Hypothesis B — Always-Open Steam Sink:**
- An implicit steam removal path may be active even when "ISOLATED"
- Possible culprits:
  - Steam dump to condenser (should be closed)
  - Atmospheric dump valves (should be closed)
  - Safety/relief valves (should be closed below setpoint)
  - Implicit "condenser bypass" or atmospheric vent
  - Steam mass being discarded rather than accumulated

**Hypothesis C — Both (Clamped + Sink):**
- Pressure computed from T_sat AND steam simultaneously removed
- Would create double suppression of pressure rise

**Diagnosis Required (Must Be Performed Before Any Fix):**

1. **Locate SG secondary pressure computation during boiling:**
   - Find in `SGMultiNodePhysics.cs`, `SGThermalModel.cs`, or equivalent
   - Document the exact equation: Is it `P = P_sat(T)` or inventory-based?
   - Identify if steam mass is tracked at all

2. **Identify any implicit steam sink paths:**
   - Search for steam mass removal even when "ISOLATED"
   - Check steam dump valve state logic
   - Check for atmospheric vent or condenser bypass paths
   - Check if steam mass is simply not accumulated (created and discarded)

3. **Add per-step diagnostic logging:**
   ```csharp
   Debug.Log($"[SG_STEAM_DIAG] SimTime={simTime:F2}hr");
   Debug.Log($"[SG_STEAM_DIAG] SteamGenRate_lbps={steamGenRate:F4} SteamOutflow_lbps={steamOutflow:F4}");
   Debug.Log($"[SG_STEAM_DIAG] SteamSpaceVol_ft3={steamSpaceVol:F2} SteamMass_lb={steamMass:F2}");
   Debug.Log($"[SG_STEAM_DIAG] P_secondary_psia={P_sec:F2} T_sat_F={Tsat:F2}");
   Debug.Log($"[SG_STEAM_DIAG] IsolationState={isIsolated} DumpValvePos={dumpPos:F2}");
   ```

4. **Classify root cause and propose fix:**

   **If Cause A (P clamped from T_sat):**
   ```csharp
   // Current (incorrect):
   // P_secondary = SteamTables.Psat(T_hottest);
   
   // Fix: Inventory-based pressure when isolated
   if (isIsolated && steamMass > 0)
   {
       float steamDensity = steamMass / steamSpaceVolume_ft3;
       P_secondary = SteamTables.PressureFromDensity(steamDensity, quality=1.0);
   }
   ```
   
   **If Cause B (Always-open sink):**
   - Locate the sink path and add proper isolation logic
   - Ensure steam mass accumulates when all outlets are closed
   ```csharp
   if (!isIsolated)
   {
       steamOutflow = ComputeSteamOutflow(...);
   }
   else
   {
       steamOutflow = 0f;  // Truly isolated
   }
   steamMass += (steamGenRate - steamOutflow) * dt;
   ```
   
   **If Cause C (Both):**
   - Fix both: implement inventory-based pressure AND close the sink

**Physical Model Requirements (Closed-Volume Steam Space):**

When SG is isolated:
```
Steam mass balance:  dm_steam/dt = m_dot_evap - m_dot_outflow
When isolated:       m_dot_outflow = 0
Steam accumulates:   m_steam(t) = m_steam(0) + ∫ m_dot_evap dt
Pressure from state: P = f(ρ_steam, T) where ρ_steam = m_steam / V_steam_space
```

This is the same thermodynamic model used for the pressurizer steam space.

---

## 2. Design Objectives

### 2.1 Architectural Rules (Non-Negotiable)

| Rule | Description |
|------|-------------|
| **R1: Single Canonical Ledger** | `TotalPrimaryMass_lb` is the ONLY source of truth for total RCS+PZR mass across ALL regimes |
| **R2: Boundary-Only Modification** | Only CVCS net flow and relief valve discharge may modify the canonical ledger |
| **R3: No V×ρ Overwrite** | Solvers must NOT recalculate total mass from volume × density; they must accept the ledger value as constraint |
| **R4: Bubble Formation = Volume Growth** | Steam bubble formation increases steam volume; liquid is displaced, not destroyed |
| **R5: Derived RCS Mass** | `RCSWaterMass = TotalPrimaryMass_lb - PZRWaterMass - PZRSteamMass` (guarantees conservation by construction) |
| **R6: Transient Stability** | No single-timestep jumps > 0.5% in any mass or level indication during normal operations |
| **R7: VCT Verification Alignment** | VCT conservation verifier must use canonical RCS ledger for inventory change term; no secondary mass sources |
| **R8: SG Closed-Volume Steam Model** | When SG is isolated, pressure must be solved from steam inventory in fixed steam-space volume; no implicit sinks |

### 2.2 Success Metrics

| Metric | Target |
|--------|--------|
| Drain duration | Realistic band (minutes, not hours) for normal operations |
| Inventory conservation | < 0.1% drift over 8-hour simulation |
| RVLIS accuracy | No drops unless true boundary mass loss |
| PZR level stability | No spikes > 0.5% per timestep on RCP start |
| Regime transition continuity | Mass identical ± 1 lb across solid→two-phase handoff |
| VCT conservation error | < 10 gal steady-state over multi-hour runs |
| SG pressure response | Pressure rises when isolated and boiling; tracks steam inventory |

---

## 3. Staged Implementation Plan

### Stage 0 — Baseline Logging & Acceptance Criteria

**Objective:** Establish comprehensive diagnostics to measure current behavior and validate fixes.

**Files Affected:**
- `HeatupSimEngine.cs` — Add diagnostic hooks
- `HeatupSimEngine.Logging.cs` — Add mass continuity logging methods
- `PressurizerPhysics.cs` — Add displacement flow logging
- `CoupledThermo.cs` — Add solver entry/exit logging

**Implementation:**

1. **Add frame-by-frame mass logging at key points:**
   ```csharp
   // Log at: bubble formation, regime transitions, RCP start, every N seconds
   LogMassState("CHECKPOINT", new {
       TotalPrimaryMass_lb,
       PZRWaterMass,
       PZRSteamMass,
       RCSWaterMass,
       ComputedSum = PZRWaterMass + PZRSteamMass + RCSWaterMass,
       Delta = TotalPrimaryMass_lb - ComputedSum,
       Regime,
       SimTime
   });
   ```

2. **Add surge line flow tracking:**
   ```csharp
   // Track mass flowing between PZR and RCS
   float surgeFlowRate_lbps;  // Positive = into PZR, Negative = out of PZR
   float cumulativeSurgeFlow_lb;
   ```

3. **Add RCP start event detection:**
   ```csharp
   // Detect RCP state changes and log surrounding frames
   if (rcpCountChanged)
   {
       LogMassState("RCP_TRANSITION_START", ...);
       _rcpTransitionLoggingFrames = 10;  // Log next 10 frames
   }
   ```

4. **Define acceptance test parameters:**
   - AT-1: CVCS step test — net -15 gpm for 10 min → expect -1,250 lb ± 4%
   - AT-2: No-flow drift — 4 hours with balanced CVCS → drift < 0.01%
   - AT-3: Transition continuity — solid→two-phase → mass identical ± 1 lb
   - AT-4: Relief test — open relief → mass decreases by ∫ṁ_relief dt ± 1%
   - AT-5: VCT cross-check — full heatup → conservation error bounded

**Expected Behavior:**
- Diagnostic logs capture all mass state changes
- Baseline measurements established for comparison after fixes

**Validation Criteria:**
- [ ] Logging compiles and emits data at specified checkpoints
- [ ] RCP transition detection triggers correctly
- [ ] Baseline acceptance test results documented (all expected to FAIL initially)

**Risks:**
- Excessive logging may impact performance → mitigate with sampling rate control
- Log file size may grow large → mitigate with rotation/truncation

---

### Stage 1 — Bubble Formation Volume Displacement Correction

**Objective:** Fix the semantic error where steam formation destroys liquid instead of displacing it.

**Files Affected:**
- `PressurizerPhysics.cs` — Core bubble formation logic
- `HeatupSimEngine.BubbleFormation.cs` — Bubble detection and handoff
- `SystemState.cs` — Add surge flow tracking fields (if not present)

**Implementation:**

1. **Identify current bubble formation code:**
   - Locate where `PZRSteamMass` increases during bubble growth
   - Locate where `PZRWaterMass` decreases correspondingly
   - Document current mass balance equation

2. **Implement volume displacement semantics:**
   ```csharp
   // BEFORE (incorrect):
   // deltaLiquidMass = -evaporationRate * dt;
   // deltaSteamMass = +evaporationRate * dt;
   // PZRWaterMass += deltaLiquidMass;
   // PZRSteamMass += deltaSteamMass;
   
   // AFTER (correct):
   // Step 1: Compute steam volume growth
   float newSteamVolume = ComputeSteamVolume(P_pzr, T_pzr, steamMass + deltaSteamMass);
   float volumeGrowth = newSteamVolume - currentSteamVolume;
   
   // Step 2: Compute liquid displacement (not destruction)
   float displacedLiquidMass = volumeGrowth * liquidDensity;
   
   // Step 3: Update pressurizer masses
   PZRSteamMass += deltaSteamMass;  // Steam from evaporation
   PZRWaterMass -= displacedLiquidMass;  // Liquid displaced to RCS
   
   // Step 4: Track surge flow for RCS update
   surgeFlowToRCS_lb += displacedLiquidMass;
   ```

3. **Ensure mass conservation during evaporation:**
   ```csharp
   // The evaporated mass comes FROM the liquid
   // So total PZR mass changes only by displacement, not by evaporation
   // Evaporation: liquid → steam (phase change, no mass change)
   // Displacement: liquid leaves PZR → goes to RCS (mass transfer)
   
   float evaporatedMass = evaporationRate * dt;
   PZRWaterMass -= evaporatedMass;   // Lost to evaporation
   PZRSteamMass += evaporatedMass;   // Gained from evaporation
   // Net PZR mass change from evaporation = 0
   
   // Then separately handle displacement due to volume growth
   float displacementMass = volumeGrowth * liquidDensity;
   PZRWaterMass -= displacementMass;  // Leaves PZR
   surgeFlowToRCS_lb += displacementMass;  // Goes to RCS
   ```

4. **Add diagnostic verification:**
   ```csharp
   float pzrMassBefore = PZRWaterMass + PZRSteamMass;
   // ... apply evaporation only ...
   float pzrMassAfter = PZRWaterMass + PZRSteamMass;
   Debug.Assert(Math.Abs(pzrMassAfter - pzrMassBefore) < 0.01f, 
       "Evaporation changed PZR mass!");
   ```

**Expected Behavior:**
- Bubble formation proceeds at realistic rate
- PZR level drops because liquid is flowing to RCS, not disappearing
- Total primary mass remains constant during bubble formation (no boundary flows)

**Validation Criteria:**
- [ ] Evaporation alone does not change total PZR mass (phase change only)
- [ ] Displacement correctly transfers mass from PZR to RCS
- [ ] Drain time reduced from ~2 hours to realistic duration
- [ ] `TotalPrimaryMass_lb` unchanged during bubble formation (no boundary flows)

**Risks:**
- May interact with existing surge line flow calculations
- Density values at saturation may need careful handling
- Level calculation may need corresponding update

---

### Stage 2 — Drain-Phase Mass Reconciliation + RVLIS Fix

**Objective:** Ensure `RCSWaterMass` is correctly updated during drain operations and RVLIS reads accurate values.

**Files Affected:**
- `CoupledThermo.cs` — RCS mass derivation
- `HeatupSimEngine.cs` — Mass reconciliation after solver
- `SystemState.cs` — Ensure `RCSWaterMass` field exists and is updated
- RVLIS calculation code (location TBD)

**Implementation:**

1. **Implement derived RCS mass (conservation by construction):**
   ```csharp
   // In CoupledThermo or post-solver reconciliation:
   // RCS mass is NOT computed from V×ρ
   // RCS mass IS computed as the remainder after PZR accounting
   
   state.RCSWaterMass = state.TotalPrimaryMass_lb 
                      - state.PZRWaterMass 
                      - state.PZRSteamMass;
   ```

2. **Remove any V×ρ calculation of RCS mass:**
   ```csharp
   // Find and remove/disable:
   // state.RCSWaterMass = rcsVolume * rcsDensity;  // REMOVE THIS
   ```

3. **Add verification logging:**
   ```csharp
   float computedTotal = state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass;
   float ledgerTotal = state.TotalPrimaryMass_lb;
   if (Math.Abs(computedTotal - ledgerTotal) > 0.01f)
   {
       Debug.LogError($"Mass reconciliation failed: computed={computedTotal}, ledger={ledgerTotal}");
   }
   ```

4. **Verify RVLIS reads from correct source:**
   - Locate RVLIS level calculation
   - Confirm it reads `state.RCSWaterMass` (which is now correctly derived)
   - If RVLIS has its own mass calculation, replace with state reference

**Expected Behavior:**
- `RCSWaterMass` always equals `TotalPrimaryMass_lb - PZRWaterMass - PZRSteamMass`
- RVLIS shows correct level during drain operations
- No ~88% drop unless actual boundary mass loss occurs

**Validation Criteria:**
- [ ] `RCSWaterMass` derived from remainder (no V×ρ calculation)
- [ ] RVLIS reads `state.RCSWaterMass` directly
- [ ] No spurious RVLIS drops during drain operations
- [ ] Conservation check passes: `RCS + PZR_water + PZR_steam == Total`

**Risks:**
- RVLIS may have independent calculation that needs removal
- Existing code may depend on V×ρ-computed RCS mass
- May surface other inconsistencies in density/volume assumptions

---

### Stage 3 — Canonical Mass Unification Across Regimes

**Objective:** Ensure the canonical ledger `TotalPrimaryMass_lb` is the sole authority across ALL regimes.

**Files Affected:**
- `CoupledThermo.cs` — Enforce ledger constraint in solver
- `HeatupSimEngine.cs` — Regime transition handling
- `HeatupSimEngine.BubbleFormation.cs` — Solid→two-phase handoff
- `RCSHeatup.cs` — Verify parameter passing

**Implementation:**

1. **Verify solid→two-phase handoff:**
   ```csharp
   // At bubble formation (bubbleFormed becomes true):
   // The two-phase ledger must inherit from solid ledger exactly
   
   if (newlyFormedBubble)
   {
       state.TotalPrimaryMass_lb = state.TotalPrimaryMassSolid;
       state.InitialPrimaryMass_lb = state.TotalPrimaryMass_lb;
       Debug.Log($"[HANDOFF] Solid→Two-phase: TotalPrimaryMass_lb={state.TotalPrimaryMass_lb:F1}");
   }
   ```

2. **Enforce ledger constraint in CoupledThermo:**
   ```csharp
   public static void SolveEquilibrium(ref SystemState state, float dt, 
       int maxIterations, float minP, float maxP, float totalPrimaryMass_lb)
   {
       // CRITICAL: totalPrimaryMass_lb is NOT a suggestion, it's a CONSTRAINT
       // The solver must distribute this mass among components
       // The solver must NOT compute a different total
       
       // ... solve for PZR state (pressure, temperature, steam/water split) ...
       
       // FINAL STEP: Derive RCS mass as remainder (MANDATORY)
       state.RCSWaterMass = totalPrimaryMass_lb - state.PZRWaterMass - state.PZRSteamMass;
       
       // VERIFICATION
       Debug.Assert(Math.Abs(state.RCSWaterMass + state.PZRWaterMass + state.PZRSteamMass 
           - totalPrimaryMass_lb) < 0.01f, "Solver violated mass constraint!");
   }
   ```

3. **Remove all V×ρ total mass calculations:**
   - Search for patterns like: `totalMass = volume * density`
   - Remove or guard with `// DISABLED: V×ρ calculation replaced by canonical ledger`

4. **Add regime-transition continuity check:**
   ```csharp
   // At every regime transition, verify mass continuity
   float massBefore = state.TotalPrimaryMass_lb;
   // ... regime transition logic ...
   float massAfter = state.TotalPrimaryMass_lb;
   if (Math.Abs(massAfter - massBefore) > 1.0f)
   {
       Debug.LogError($"Regime transition violated mass continuity: " +
           $"before={massBefore:F1}, after={massAfter:F1}, delta={massAfter-massBefore:F1}");
   }
   ```

**Expected Behavior:**
- `TotalPrimaryMass_lb` is identical before and after regime transitions (within 1 lb)
- No solver overwrites the canonical value
- All component masses sum to canonical total exactly

**Validation Criteria:**
- [ ] Solid→two-phase handoff preserves mass exactly (AT-3)
- [ ] Two-phase→solid transition (if applicable) preserves mass
- [ ] No V×ρ calculations of total mass remain active
- [ ] CoupledThermo always uses provided constraint
- [ ] Conservation check passes in all regimes

**Risks:**
- May require significant refactoring of CoupledThermo
- Could surface inconsistencies in density calculations
- Regime-specific logic may have hidden V×ρ assumptions

---

### Stage 4 — RCP Transient Spike Diagnosis + Correction

**Objective:** Identify and fix the cause of PZR level spikes when RCPs start.

**Files Affected:**
- `HeatupSimEngine.cs` — RCP state change handling
- `PressurizerPhysics.cs` — Level calculation
- `ThermalExpansion.cs` (or equivalent) — Expansion accounting
- (Additional files TBD based on diagnosis)

**Implementation:**

1. **Add intensive logging around RCP start:**
   ```csharp
   // Before RCP count changes
   LogMassState("RCP_PRE_CHANGE", ...);
   LogPZRState("RCP_PRE_CHANGE", level, waterMass, steamMass, pressure, temp);
   
   // Apply RCP change
   rcpCount = newCount;
   
   // After each physics step for next 10 frames
   for (int i = 0; i < 10; i++)
   {
       // ... physics step ...
       LogMassState($"RCP_POST_FRAME_{i}", ...);
       LogPZRState($"RCP_POST_FRAME_{i}", ...);
   }
   ```

2. **Run diagnostic test:**
   - Start simulation in stable two-phase state
   - Start RCPs
   - Capture frame-by-frame log
   - Identify which frame shows the spike
   - Identify which variable changed unexpectedly

3. **Based on diagnosis, apply appropriate fix:**

   **If Hypothesis A (Canonical Overwrite):**
   ```csharp
   // Find where ledger is being overwritten and remove/guard it
   // state.TotalPrimaryMass_lb = computedFromVρ;  // REMOVE
   ```

   **If Hypothesis B (Density Ordering):**
   ```csharp
   // Ensure density and volume are updated atomically
   // Or ensure level calculation uses consistent state
   float snapshotDensity = currentDensity;
   float snapshotVolume = currentVolume;
   float level = ComputeLevel(snapshotDensity, snapshotVolume);
   ```

   **If Hypothesis C (Thermal Expansion Double-Counting):**
   ```csharp
   // Ensure expansion is applied once, with proper attribution
   // RCS expansion increases surge flow to PZR
   // PZR level rises from incoming mass
   // Do NOT also expand PZR liquid independently
   ```

4. **Verify fix:**
   - Repeat RCP start test
   - Confirm no spike > 0.5% per timestep

**Expected Behavior:**
- RCP start causes gradual thermal expansion effects
- PZR level changes smoothly, proportional to heat addition
- No single-frame spikes

**Validation Criteria:**
- [ ] Diagnostic logging captures pre/post RCP state
- [ ] Root cause identified and documented
- [ ] Fix applied and spike eliminated
- [ ] No level change > 0.5% per timestep during RCP start

**Risks:**
- Root cause may be complex interaction requiring significant debugging
- Fix may have side effects on other transients
- May need to refactor thermal expansion accounting

---

### Stage 5 — VCT Conservation Error Diagnosis + Reconciliation

**Objective:** Diagnose the VCT conservation error and align verification with the canonical RCS ledger.

**Files Affected:**
- `VCTPhysics.cs` — Conservation verification logic
- `HeatupSimEngine.cs` — Call site supplying RCS change term
- `HeatupSimEngine.Logging.cs` — VCT diagnostic logging
- `SystemState.cs` — Verify field availability for canonical delta

**Implementation:**

1. **Locate and document VCT verification equation:**
   ```csharp
   // Find in VCTPhysics.cs or equivalent:
   // Document the exact conservation check equation
   // Identify where rcsInventoryChange_gal comes from
   ```

2. **Add conditional diagnostic logging:**
   ```csharp
   // In VCT verification method:
   float conservationError = Math.Abs(vctChange + rcsChange - externalNet);
   
   if (conservationError > 100f)  // > 100 gal threshold
   {
       Debug.Log($"[VCT_DIAG] SimTime={simTime:F2}hr ERROR={conservationError:F2}gal");
       Debug.Log($"[VCT_DIAG] vctChange={vctChange:F2} rcsChange={rcsChange:F2} extNet={externalNet:F2}");
       Debug.Log($"[VCT_DIAG] CumIn={cumIn:F2} CumOut={cumOut:F2} CumExtIn={cumExtIn:F2} CumExtOut={cumExtOut:F2}");
       Debug.Log($"[VCT_DIAG] TotalPrimaryMass_lb={state.TotalPrimaryMass_lb:F1} Initial={state.InitialPrimaryMass_lb:F1}");
   }
   ```

3. **Run diagnostic test:**
   - Start fresh simulation
   - Run through bubble formation and into two-phase
   - Monitor VCT error growth
   - Capture diagnostic output when error exceeds threshold
   - Analyze which term is drifting

4. **Classify root cause:**
   
   **If Cause A (RCS delta from drifting source):**
   ```csharp
   // Current (likely incorrect):
   // rcsChange_gal computed from V×ρ or non-canonical source
   
   // Fix: Derive from canonical ledger
   float rcsChange_lb = state.InitialPrimaryMass_lb - state.TotalPrimaryMass_lb;
   float rcsChange_gal = rcsChange_lb / 8.34f;  // lb to gal conversion
   ```
   
   **If Cause B (VCT flow accounting bug):**
   - Audit all VCT flow paths
   - Check for double-counting (charging counted twice?)
   - Check for missed paths (seal return not counted?)
   - Fix the specific flow accounting error
   
   **If Cause C (Unit/dt mismatch):**
   - Verify lb vs gal conversions consistent
   - Verify dt accumulation matches integration method
   - Fix the conversion or accumulation logic

5. **Apply fix and verify:**
   - Implement minimal fix based on diagnosis
   - Re-run multi-hour simulation
   - Confirm VCT error remains < 10 gal steady-state

**Expected Behavior:**
- VCT conservation error near-zero when no external makeup/sinks active
- Error bounded and explainable when external flows occur
- No cascading failure from primary mass drift

**Validation Criteria:**
- [ ] VCT verification equation documented
- [ ] Diagnostic logging captures error components
- [ ] Root cause classified (A, B, or C)
- [ ] Fix implemented and aligned with canonical ledger (Rule R7)
- [ ] VCT error < 10 gal over 4+ hour simulation
- [ ] No new second sources of truth introduced

**Risks:**
- VCT verifier may have multiple RCS input paths requiring careful audit
- Fix may require coordination with Stages 2/3 (RCS mass derivation)
- Unit conversion errors may be subtle

---

### Stage 6 — SG Secondary Pressure / Steam Inventory Diagnosis

**Objective:** Diagnose why SG pressure does not rise during boiling and implement closed-volume steam model if needed.

**Files Affected:**
- `SGMultiNodePhysics.cs` or `SGThermalModel.cs` — Pressure computation logic
- `SGMultiNodeState.cs` — State fields for steam mass tracking
- `HeatupSimEngine.cs` — SG isolation state handling
- `SteamDumpController.cs` or equivalent — Valve state logic
- `HeatupSimEngine.Logging.cs` — SG steam diagnostic logging

**Implementation:**

1. **Locate and document SG secondary pressure computation:**
   ```csharp
   // Find the line(s) that set P_secondary or SecondaryPressure_psia
   // Document: Is it P_sat(T), inventory-based, or something else?
   // Example patterns to search for:
   //   SecondaryPressure = SteamTables.Psat(temperature);
   //   SecondaryPressure = ComputePressureFromSteamMass(...);
   ```

2. **Search for implicit steam sinks:**
   ```csharp
   // Search for any steam mass removal or outflow:
   //   steamMass -= ...
   //   steamOutflow = ...
   //   RemoveSteam(...)
   // Check if these are gated by isolation state
   ```

3. **Add diagnostic logging:**
   ```csharp
   // Add to SG physics update method:
   if (boilingFraction > 0.5f)  // Only log during significant boiling
   {
       Debug.Log($"[SG_STEAM] t={simTime:F2}hr P={SecondaryPressure_psia:F1}psia Tsat={Tsat:F1}F");
       Debug.Log($"[SG_STEAM] SteamGen={steamGenRate_lbps:F4}lb/s Outflow={steamOutflow_lbps:F4}lb/s");
       Debug.Log($"[SG_STEAM] SteamMass={steamMass_lb:F1}lb SteamVol={steamSpaceVol_ft3:F1}ft3");
       Debug.Log($"[SG_STEAM] Isolated={isIsolated} DumpOpen={dumpValveOpen} ReliefOpen={reliefOpen}");
   }
   ```

4. **Run diagnostic test:**
   - Start simulation with SG isolated (all steam outlets closed)
   - Run until boiling reaches 100%
   - Monitor diagnostic output
   - Observe: Does steam mass accumulate? Does pressure rise? Is there unexplained outflow?

5. **Classify root cause:**
   
   **If Cause A (P clamped from T_sat):**
   - Pressure is computed directly from saturation temperature
   - Steam mass is either not tracked or not used for pressure
   - **Evidence:** `SecondaryPressure = Psat(T_hottest)` found in code
   
   **If Cause B (Always-open sink):**
   - Steam mass is tracked but immediately removed
   - Isolation state not properly checked before steam removal
   - **Evidence:** `steamOutflow > 0` even when `isIsolated == true`
   
   **If Cause C (Both):**
   - Pressure clamped AND steam removed
   - **Evidence:** Both patterns found

6. **Propose minimal fix based on diagnosis:**
   
   **For Cause A — Implement inventory-based pressure:**
   ```csharp
   // Add steam mass tracking if not present
   public float SteamMass_lb;
   public float SteamSpaceVolume_ft3;  // Fixed volume above water level
   
   // In physics update:
   float steamGenerated_lb = boilingRate_lbps * dt;
   float steamRemoved_lb = isIsolated ? 0f : steamOutflowRate_lbps * dt;
   SteamMass_lb += steamGenerated_lb - steamRemoved_lb;
   
   // Compute pressure from steam state
   if (SteamMass_lb > 0 && SteamSpaceVolume_ft3 > 0)
   {
       float steamDensity_lbft3 = SteamMass_lb / SteamSpaceVolume_ft3;
       SecondaryPressure_psia = SteamTables.PressureFromSteamDensity(steamDensity_lbft3);
   }
   ```
   
   **For Cause B — Fix isolation logic:**
   ```csharp
   // Ensure steam is not removed when isolated
   bool steamPathOpen = dumpValveOpen || reliefValveOpen || atmosphericVentOpen;
   
   if (steamPathOpen)
   {
       steamOutflow_lbps = ComputeSteamOutflow(...);
   }
   else
   {
       steamOutflow_lbps = 0f;  // Truly isolated - no steam exits
   }
   ```

7. **Verify fix:**
   - Re-run diagnostic test with SG isolated
   - Confirm steam mass accumulates
   - Confirm pressure rises with steam inventory
   - Confirm pressure stabilizes when steam outlet opens

**Expected Behavior After Fix:**
- When SG is isolated and boiling, pressure rises as steam accumulates
- Pressure tracks steam inventory, not just temperature
- When steam outlet opens, pressure controlled by outlet capacity
- Model matches pressurizer steam-space behavior

**Validation Criteria:**
- [ ] SG pressure computation method documented
- [ ] Steam sink paths identified and documented
- [ ] Diagnostic logging captures steam generation/outflow/mass/pressure
- [ ] Root cause classified (A, B, or C)
- [ ] If Cause A: Steam mass tracking added, pressure computed from inventory
- [ ] If Cause B: Isolation logic fixed, steam accumulates when isolated
- [ ] Pressure rises during isolated boiling (qualitative validation)
- [ ] Rule R8 (Closed-Volume Steam Model) enforced

**Risks:**
- SG model may need significant restructuring to add steam mass tracking
- Steam-space volume calculation may be complex (depends on water level)
- May need steam tables lookup for density→pressure (ensure consistency with PZR model)
- Fix may affect SG behavior in other operating modes

---

### Stage 7 — Validation Suite & Regression Testing

**Objective:** Execute all acceptance tests and confirm no regressions.

**Files Affected:**
- Test scripts/procedures (documentation)
- Potentially `HeatupSimEngine.cs` for test mode hooks

**Implementation:**

1. **Execute AT-1: Two-Phase CVCS Step Test**
   - Procedure: Bubble formed, set charging=60gpm, letdown=75gpm, run 10 min
   - Expected: `TotalPrimaryMass_lb` decreases by 1,250 ± 50 lb
   - Record: Initial mass, final mass, delta, pass/fail

2. **Execute AT-2: No-Flow Drift Test**
   - Procedure: Two-phase steady, charging=letdown=60gpm, relief closed, run 4 hr
   - Expected: Ledger drift < 0.01% (< 60 lb for ~600,000 lb system)
   - Record: Initial mass, final mass, drift %, pass/fail

3. **Execute AT-3: Solid→Two-Phase Transition Continuity**
   - Procedure: Start solid, run to bubble formation
   - Expected: `TotalPrimaryMass_lb` equals `TotalPrimaryMassSolid` within ± 1 lb
   - Record: Pre-transition mass, post-transition mass, delta, pass/fail

4. **Execute AT-4: Relief Open Test**
   - Procedure: Force relief open, measure flow and duration
   - Expected: Mass decreases by ∫ṁ_relief dt within ± 1%
   - Record: Relief flow integral, mass decrease, error %, pass/fail

5. **Execute AT-5: VCT Conservation Cross-Check**
   - Procedure: Full heatup (solid → two-phase → HZP approach)
   - Expected: VCT conservation error < 10 gal steady-state
   - Record: Max conservation error, timestamps, pass/fail

6. **Execute NEW tests:**

   **AT-6: Drain Duration Test**
   - Procedure: From full PZR, initiate drain to 50% level
   - Expected: Duration in realistic range (not 2 hours)
   - Record: Drain time, pass/fail

   **AT-7: RVLIS Stability Test**
   - Procedure: Normal operations, monitor RVLIS through drain/fill cycles
   - Expected: No spurious drops > 1%
   - Record: Min RVLIS during test, any anomalies, pass/fail

   **AT-8: RCP Start Stability Test**
   - Procedure: From stable two-phase, start RCPs
   - Expected: No level spike > 0.5% per timestep
   - Record: Max frame-to-frame level change, pass/fail

   **AT-9: VCT Conservation Steady-State Test**
   - Procedure: Run 4+ hours with balanced CVCS (charging = letdown)
   - Expected: VCT conservation error < 10 gal throughout
   - Record: Max error, error trend, pass/fail

   **AT-10: SG Isolated Boiling Pressure Rise Test**
   - Procedure: Isolate SG (close all steam outlets), run until 100% boiling
   - Expected: SG secondary pressure rises above atmospheric as steam accumulates
   - Record: Initial pressure, final pressure, steam mass accumulated, pass/fail

7. **Regression checks:**
   - Verify existing functionality still works
   - SG heat transfer
   - CVCS automatic operation
   - Heater/spray control
   - Mode transitions

**Expected Behavior:**
- All acceptance tests pass
- No regressions in existing functionality

**Validation Criteria:**
- [ ] AT-1 PASS
- [ ] AT-2 PASS
- [ ] AT-3 PASS
- [ ] AT-4 PASS
- [ ] AT-5 PASS
- [ ] AT-6 PASS (drain duration)
- [ ] AT-7 PASS (RVLIS stability)
- [ ] AT-8 PASS (RCP start stability)
- [ ] AT-9 PASS (VCT conservation steady-state)
- [ ] AT-10 PASS (SG isolated boiling pressure rise)
- [ ] No regressions identified

**Risks:**
- Some tests may reveal additional issues requiring iteration
- Test environment setup may require specific initial conditions

---

## 4. Acceptance Criteria Summary

| ID | Criterion | Pass Definition |
|----|-----------|-----------------|
| **AC-1** | Drain duration realistic | Normal drain completes in minutes, not hours |
| **AC-2** | Inventory conservation | < 0.1% drift over 8-hour simulation in all regimes |
| **AC-3** | No spurious RVLIS drop | RVLIS stable unless true boundary mass loss |
| **AC-4** | No PZR spike on RCP start | Max level change < 0.5% per timestep |
| **AC-5** | Regime transition continuity | Mass identical ± 1 lb across transitions |
| **AC-6** | VCT conservation | < 10 gal steady-state error over multi-hour runs |
| **AC-7** | SG pressure response | Pressure rises when isolated and boiling |
| **AC-8** | AT-1 through AT-10 | All acceptance tests PASS |
| **AC-9** | No regressions | Existing functionality preserved |

---

## 5. Files Summary

| File | Stages | Changes |
|------|--------|---------|
| `SystemState.cs` | 0, 1, 2 | Add surge flow tracking, verify mass fields |
| `HeatupSimEngine.cs` | 0, 1, 3, 4 | Diagnostic logging, regime handling, RCP detection |
| `HeatupSimEngine.Logging.cs` | 0 | Mass continuity logging methods |
| `HeatupSimEngine.BubbleFormation.cs` | 1, 3 | Displacement semantics, handoff verification |
| `PressurizerPhysics.cs` | 0, 1, 4 | Displacement flow, level calculation |
| `CoupledThermo.cs` | 0, 2, 3 | Enforce ledger constraint, remove V×ρ |
| `RCSHeatup.cs` | 3 | Verify parameter passing |
| ThermalExpansion code (TBD) | 4 | Fix double-counting if applicable |
| `VCTPhysics.cs` | 5 | Conservation verification alignment with canonical ledger |
| `SGMultiNodePhysics.cs` | 6 | Pressure computation, steam mass tracking |
| `SGMultiNodeState.cs` | 6 | Add steam mass/volume fields if needed |

---

## 6. Not Addressed / Future Work

| Item | Disposition | Target Version |
|------|-------------|----------------|
| Per-loop mass tracking | Out of scope — single RCS mass is sufficient for current model | v6.0.0 |
| Secondary (SG) mass conservation | Requires feedwater modeling | v5.6.4+ |
| PRT (Pressurizer Relief Tank) modeling | Not needed for current scenarios | Future |
| Detailed surge line hydraulics | Simplified flow model sufficient | v6.0.0 |
| Sub-cooled vs. saturated surge flow | Out of scope | v6.0.0 |

---

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Stage 1 interacts with surge line code | Medium | High | Careful integration, extensive testing |
| CoupledThermo refactor is complex | Medium | Medium | Incremental changes, verification at each step |
| RCP spike root cause is elusive | Low | Medium | Intensive logging, systematic hypothesis testing |
| Fixes cause regressions | Medium | High | Full regression test suite in Stage 6 |
| Performance impact from logging | Low | Low | Logging rate controls, disable in release |
| VCT error from multiple sources | Medium | Medium | Systematic diagnosis in Stage 5; audit all flow paths |
| SG model restructuring for steam mass | Medium | High | Incremental changes; ensure consistency with PZR model |

---

## 8. Implementation Rules

1. **Implement ONE STAGE per reply** when authorized to proceed
2. After completing each stage: show files modified, key code changes, validation results
3. **Stop and await approval** before proceeding to next stage
4. **Do NOT create changelog** until Stage 5 validates with ALL tests passing
5. Keep changes strictly contained to stage objectives
6. Follow existing code conventions
7. Add comments explaining architectural decisions (especially Rule R1-R6 enforcement)

---

## 9. Output Requirements per Stage

After completing each stage, provide:

1. **Files modified** — Full paths
2. **Key code changes** — Relevant snippets showing the fix
3. **Validation performed** — What was tested and results
4. **Explicit statement:** "Stage N complete. Awaiting approval to proceed to Stage N+1."

---

## 10. Changelog (Created After Stage 7)

Changelog will be created in `Critical\Updates\Changelogs\CHANGELOG_v5.4.0.md` only after:
- All 8 stages (0-7) complete
- All 10 acceptance tests pass
- No regressions identified

---

*Prepared: 2026-02-12*  
*Status: PLAN COMPLETE — NOT APPROVED FOR IMPLEMENTATION*  
*Next Action: Craig reviews and authorizes Stage 0 execution*
