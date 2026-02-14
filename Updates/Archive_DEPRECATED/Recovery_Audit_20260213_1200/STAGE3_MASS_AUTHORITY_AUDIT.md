# STAGE 3A — CANONICAL MASS AUTHORITY AUDIT
## Recovery Audit 2026-02-13

---

## EXECUTIVE SUMMARY

**The canonical mass enforcement architecture (Rules R1, R3, R5) is correctly implemented inside `CoupledThermo.SolveEquilibrium()` but is NEVER ACTIVATED at runtime.** Both Regime 2 and Regime 3 call `BulkHeatupStep()` without passing the `totalPrimaryMass_lb` parameter. The parameter defaults to `0f`, causing the solver to take the LEGACY code path where all masses are derived from V×ρ. The "canonical ledger" field `TotalPrimaryMass_lb` exists and is written to, but the solver never reads it.

Additionally, three boundary-flow accumulators (`CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb`) are declared and read by diagnostics but are **never incremented** — they remain at `0f` for the entire simulation.

**Verdict: The simulator has TWO competing mass authorities — `TotalPrimaryMass_lb` (written by engine) and the V×ρ-derived masses (computed by solver). The solver wins because it's the last writer. The ledger is decorative.**

---

## 1. WRITE-PATH TABLE

### Field: `TotalPrimaryMass_lb`

| Regime | Writer | File:Lines | How | Authority Type |
|--------|--------|------------|-----|---------------|
| Init (cold) | Engine | Init.cs:191 | `rcsWaterMass + pzrWaterMass` (V×ρ at init) | V×ρ derived |
| Init (warm) | Engine | Init.cs:254-256 | `RCSWaterMass + PZRWaterMass + PZRSteamMass` (V×ρ at init) | V×ρ derived |
| Regime 1 (solid) | Engine | HeatupSimEngine.cs:1085 | `TotalPrimaryMassSolid` (from solid sync) | Copied from solid mirror |
| Regime 1 (bubble, no RCPs) | **Nobody** | — | Not written in isolated heating path | Stale from last write |
| Regime 2 (RCPs ramping) | **Nobody** | — | Not written anywhere in Regime 2 | Stale from last write |
| Regime 3 (full RCPs) | **Nobody** | — | Not written anywhere in Regime 3 | Stale from last write |
| First-step rebase | Engine | HeatupSimEngine.cs:1455 | `RCS + PZR_water + PZR_steam` (one-time V×ρ snapshot) | V×ρ snapshot |

**Observation:** After the first-step rebase (line 1455), `TotalPrimaryMass_lb` is never updated again in Regime 2/3. It freezes at the first-step value. CVCS boundary flows modify `RCSWaterMass` (lines 1182, 1344), but never update the ledger. The solver then overwrites all component masses from V×ρ, so the pre-applied CVCS mass is also destroyed.

---

### Field: `RCSWaterMass`

| Regime | Writer | File:Lines | How | Authority Type |
|--------|--------|------------|-----|---------------|
| Init (cold) | Engine | Init.cs:176 | V×ρ | V×ρ derived |
| Init (warm) | Engine | Init.cs:236 | V×ρ | V×ρ derived |
| Regime 1 (solid) | Engine | HeatupSimEngine.cs:1077 | `+= CVCS mass change` (boundary) | Boundary-conserved |
| Regime 1 (bubble, no RCPs) | RCSHeatup | (via IsolatedHeatingStep) | — | Module-owned |
| BubbleFormation DRAIN | Engine | BubbleFormation.cs:397 | `+= dm_cvcsActual` (mass from PZR) | Mass-conserving transfer |
| Regime 2 pre-solver | Engine | HeatupSimEngine.cs:1182 | `+= cvcsNetMass_lb_r2` (CVCS) | Boundary flow |
| Regime 2 post-solver | **CoupledThermo** | CoupledThermo.cs:278 | `V_RCS × rho_RCS` (LEGACY V×ρ) | **V×ρ OVERWRITE** |
| Regime 3 pre-solver | Engine | HeatupSimEngine.cs:1344 | `+= cvcsNetMass_lb` (CVCS) | Boundary flow |
| Regime 3 post-solver | **CoupledThermo** | CoupledThermo.cs:278 | `V_RCS × rho_RCS` (LEGACY V×ρ) | **V×ρ OVERWRITE** |
| CVCS partial (post-solver) | Engine | CVCS.cs:205 | `+= massChange_lb` | Boundary flow |
| First-step rebase | Engine | HeatupSimEngine.cs:1454 | Read (not overwritten) | — |

**Critical Chain in Regime 3:**
1. Engine pre-applies CVCS mass: `RCSWaterMass += cvcsNetMass_lb` (line 1344)
2. Solver runs in LEGACY mode: `RCSWaterMass = V_RCS × rho_RCS` (CoupledThermo.cs:278)
3. **The CVCS pre-application at step 1 is DESTROYED by step 2.**
4. Then `UpdateRCSInventory()` is skipped (double-count guard flag is set at line 1351).
5. Net result: CVCS mass is applied to `RCSWaterMass` pre-solver, but the solver overwrites it.

**However:** The solver uses `M_total` from component sum (line 133), which includes the pre-applied CVCS mass in `RCSWaterMass`. The solver redistributes this total via V×ρ to all components. So the CVCS mass IS implicitly included in the solver's M_total — it just gets redistributed differently than the engine expected.

---

### Field: `PZRWaterMass`

| Regime | Writer | File:Lines | How | Authority Type |
|--------|--------|------------|-----|---------------|
| Init (cold) | Engine | Init.cs:179 | V×ρ | V×ρ derived |
| Init (warm) | Engine | Init.cs:246 | V×ρ | V×ρ derived |
| Regime 1 (solid) | SolidPlantPressure | (via engine sync) | State-machine managed | Module-owned |
| BubbleFormation | Engine | BubbleFormation.cs:122 | V×ρ (init) | V×ρ derived |
| BubbleFormation DRAIN | Engine | BubbleFormation.cs:391,396 | `-= dm_steam, -= dm_cvcs` | Mass-conserving |
| Regime 2 pre-solver | Engine | HeatupSimEngine.cs:1164 | `pzrWaterVolume × ρ_water(T_sat, P)` | **V×ρ OVERWRITE** |
| Regime 2 spray | Engine | HeatupSimEngine.cs:1200 | `+= SteamCondensed_lbm` | Mass-conserving transfer |
| Regime 2 post-solver | CoupledThermo | CoupledThermo.cs:286 | `PZRWaterVolume × ρ_water` (LEGACY) | **V×ρ OVERWRITE** |
| Regime 3 pre-solver | Engine | HeatupSimEngine.cs:1318 | `pzrWaterVolume × ρ_water(T_sat, P)` | **V×ρ OVERWRITE** |
| Regime 3 spray | Engine | HeatupSimEngine.cs:1364 | `+= SteamCondensed_lbm` | Mass-conserving transfer |
| Regime 3 post-solver | CoupledThermo | CoupledThermo.cs:286 | `PZRWaterVolume × ρ_water` (LEGACY) | **V×ρ OVERWRITE** |

**Multiple writers per timestep in Regime 3:**
1. Engine writes V×ρ from engine volumes (line 1318)
2. Spray modifies mass-conservingly (line 1364)
3. Then solver overwrites EVERYTHING from V×ρ at new T/P (CoupledThermo.cs:286)

---

### Field: `PZRSteamMass`

| Regime | Writer | File:Lines | How | Authority Type |
|--------|--------|------------|-----|---------------|
| Init (cold) | Engine | Init.cs:180 | `0f` (solid, no steam) | Literal |
| Init (warm) | Engine | Init.cs:247 | V×ρ | V×ρ derived |
| BubbleFormation DRAIN | Engine | BubbleFormation.cs:392 | `+= dm_steamActual` | Mass-conserving |
| Regime 2 pre-solver | Engine | HeatupSimEngine.cs:1165 | `pzrSteamVolume × ρ_steam(P)` | **V×ρ OVERWRITE** |
| Regime 2 spray | Engine | HeatupSimEngine.cs:1199 | `-= SteamCondensed_lbm` | Mass-conserving |
| Regime 2 post-solver | CoupledThermo | CoupledThermo.cs:287 | V×ρ (LEGACY) | **V×ρ OVERWRITE** |
| Regime 3 pre-solver | Engine | HeatupSimEngine.cs:1319 | `pzrSteamVolume × ρ_steam(P)` | **V×ρ OVERWRITE** |
| Regime 3 spray | Engine | HeatupSimEngine.cs:1363 | `-= SteamCondensed_lbm` | Mass-conserving |
| Regime 3 post-solver | CoupledThermo | CoupledThermo.cs:287 | V×ρ (LEGACY) | **V×ρ OVERWRITE** |

---

### Fields: `TotalPrimaryMassSolid`, `PZRWaterMassSolid`

| Regime | Writer | File:Lines | How | Authority Type |
|--------|--------|------------|-----|---------------|
| Init (cold) | Engine | Init.cs:185-186 | From init masses | V×ρ derived |
| Regime 1 (solid) | Engine | HeatupSimEngine.cs:1083-1084 | Copied from PZRWaterMass, sum | Mirror of current state |
| Post-solid | **Nobody** | — | Never written again | Stale |

These fields are only relevant during solid operations and are correctly managed there.

---

## 2. THE MISSING LINK: `totalPrimaryMass_lb` PARAMETER

### Root Cause

**File:** `RCSHeatup.cs:114`
```csharp
float totalPrimaryMass_lb = 0f)  // v5.3.0: Canonical mass constraint for conservation
```

The parameter was added in v5.3.0 with a default value of `0f`. The CoupledThermo solver checks `if (totalPrimaryMass_lb > 0f)` to decide canonical vs legacy mode (CoupledThermo.cs:123).

**Neither call site passes this parameter:**

| Call Site | File:Lines | Arguments Passed | `totalPrimaryMass_lb` Value |
|-----------|------------|------------------|-----------------------------|
| Regime 2 | HeatupSimEngine.cs:1214-1217 | 9 of 10 args | **0f** (default → LEGACY MODE) |
| Regime 3 | HeatupSimEngine.cs:1378-1381 | 9 of 10 args | **0f** (default → LEGACY MODE) |

**Confidence:** High — verified by reading both call sites and the method signature. The 10th parameter is never supplied.

### Impact

The entire v5.4.0 canonical mass architecture (Rules R1-R8) is correctly coded inside the solver but **never exercised**:

| CoupledThermo Feature | Code Path | Status |
|----------------------|-----------|--------|
| `M_total = totalPrimaryMass_lb` | CoupledThermo.cs:127 | **DEAD** (legacy path taken instead) |
| `RCSWaterMass = M_total - PZR_water - PZR_steam` (R5) | CoupledThermo.cs:259 | **DEAD** |
| Conservation-by-construction identity | CoupledThermo.cs:264-272 | **DEAD** (DEBUG-only, and wrong path) |

Instead, the LEGACY path executes:
- `RCSWaterMass = V_RCS × rho_RCS` (CoupledThermo.cs:278) — V×ρ overwrite
- PZR masses from V×ρ (CoupledThermo.cs:286-287)

---

## 3. BOUNDARY-FLOW ACCUMULATORS: NEVER INCREMENTED

**File:** `CoupledThermo.cs:785-787` (declaration in SystemState struct)

| Field | Declared | Read By | Written By | Value |
|-------|----------|---------|-----------|-------|
| `CumulativeCVCSIn_lb` | CoupledThermo.cs:785 | Logging.cs:482,527,701 | **NOBODY** | Always 0f |
| `CumulativeCVCSOut_lb` | CoupledThermo.cs:786 | Logging.cs:483,528,702 | **NOBODY** | Always 0f |
| `CumulativeReliefMass_lb` | CoupledThermo.cs:787 | Logging.cs:484,529,703 | **NOBODY** | Always 0f |

**Impact:** The `primaryMassExpected_lb` calculation in `UpdatePrimaryMassLedgerDiagnostics()` (Logging.cs:526-529) always computes:
```
expected = InitialPrimaryMass_lb + 0 - 0 - 0 = InitialPrimaryMass_lb
```

Even if the diagnostic were called, its boundary error check would falsely alarm on any CVCS-driven mass change because it doesn't know about boundary flows. The diagnostic is doubly dead: uncalled AND broken.

---

## 4. VERDICT BY REGIME

### Regime 1 — Solid Operations
- **Authority:** `SolidPlantPressure` owns P-T-V. Engine manages mass via `TotalPrimaryMassSolid` / `PZRWaterMassSolid`.
- **Classification:** Single source of truth. CVCS boundary flows correctly applied. `TotalPrimaryMass_lb` is a mirror copy.
- **Status:** CORRECT

### Regime 1 — Bubble, No RCPs (Isolated)
- **Authority:** `RCSHeatup.IsolatedHeatingStep` owns T/P. BubbleFormation state machine owns mass during DRAIN.
- **Classification:** Single source of truth during DRAIN (mass-conserving transfers). Post-DRAIN, `TotalPrimaryMass_lb` is stale.
- **Status:** CORRECT during DRAIN; **STALE LEDGER** post-drain.

### Regime 2 — RCPs Ramping (Blended)
- **Authority:** **AUTHORITY CONFLICT**
  - Engine writes PZR masses from V×ρ (lines 1164-1165)
  - Engine pre-applies CVCS to RCSWaterMass (line 1182)
  - Solver overwrites ALL masses from V×ρ in LEGACY mode (CoupledThermo.cs:278,286-287)
  - Engine blends isolated + coupled results by α (lines 1219-1246)
  - `TotalPrimaryMass_lb` is NOT updated
- **Classification:** **Multiple competing authorities. Solver is de facto authority via V×ρ. CVCS pre-application is preserved only indirectly via M_total component sum.**
- **Drift vector:** Order-of-operations (pre/post solver placement) + Authority conflict (V×ρ overwrite)

### Regime 3 — Full RCPs
- **Authority:** **AUTHORITY CONFLICT** (same pattern as Regime 2)
  - Engine writes PZR masses from V×ρ (lines 1318-1319)
  - Engine pre-applies CVCS to RCSWaterMass (line 1344)
  - Solver overwrites ALL masses from V×ρ in LEGACY mode
  - `TotalPrimaryMass_lb` is NOT updated
  - CVCS partial skipped due to double-count guard (but the solver already overwrote the pre-applied mass)
- **Classification:** **Multiple competing authorities. The canonical ledger `TotalPrimaryMass_lb` is decorative — it freezes after the first-step rebase and diverges from actual state.**
- **Drift vectors:**
  1. **Authority conflict:** Solver (V×ρ) vs engine (boundary flows) both write RCSWaterMass
  2. **Boundary accounting gap:** `TotalPrimaryMass_lb` never updated by CVCS flows in Regime 2/3
  3. **Order-of-operations:** CVCS pre-applied, then solver overwrites, then CVCS guard prevents re-application
  4. **State ownership confusion:** Engine pre-computes PZR V×ρ, solver re-computes PZR V×ρ — redundant with different T/P inputs

---

## 5. WHY IT MIGHT "WORK ANYWAY"

Despite the architectural violations, the simulator may appear to function because:

1. **The solver's LEGACY mode still does valid physics.** V×ρ at consistent T/P produces physically reasonable mass distributions. The total mass is derived from components, and CVCS changes to RCSWaterMass are captured in the component sum that the solver uses as M_total.

2. **The CVCS pre-application IS implicitly included.** When the engine writes `RCSWaterMass += cvcsNetMass` (line 1344) and the solver then reads `M_total = RCS + PZR_w + PZR_s` (line 133), the CVCS delta IS in M_total. The solver redistributes mass to satisfy equilibrium, so the CVCS effect manifests as a change in PZR level. This is physically reasonable — it's just not the "canonical ledger" architecture the comments describe.

3. **The actual conservation check (`massError_lbm`) in CVCS.cs:300 works.** It tracks `totalSystemMass_lbm` (which includes VCT + BRS), compared against `initialSystemMass_lbm + externalNet`. Since this is computed from live state (not the ledger), it catches real drift.

4. **The decorator `TotalPrimaryMass_lb` is read by the inventory audit (Logging.cs:497) and used for display, but the audit has its own regime-aware mass source selection** (Logging.cs:303-320) that reads component masses directly. The frozen ledger would cause the dead diagnostics to alarm, but since they're dead, nobody notices.

---

## 6. ISSUE CARDS (for ISSUES_REGISTER.md)

### ISSUE-0001: CoupledThermo Canonical Mass Mode Never Activated
- **Subsystem:** CoupledThermo / Engine
- **Evidence:** RCSHeatup.cs:114 (default param = 0f), HeatupSimEngine.cs:1214-1217 and 1378-1381 (10th arg not passed), CoupledThermo.cs:123 (gate check)
- **Trigger:** Every timestep in Regime 2 and 3
- **Impact:** CRITICAL — Rules R1, R3, R5 are unenforced. All masses derived from V×ρ.
- **Classification:** Authority conflict
- **Hypothesis:** Parameter was added in v5.3.0 but the call sites were never updated to pass `physicsState.TotalPrimaryMass_lb`.
- **Suggested fix:** Pass `physicsState.TotalPrimaryMass_lb` as the 10th argument at lines 1217 and 1381. Also ensure the ledger is updated by CVCS flows each timestep.
- **Tests:** After fix, `primaryMassDrift_lb` (ledger - components) should be ≤ 0.01 lb.
- **Confidence:** High

### ISSUE-0002: TotalPrimaryMass_lb Freezes After First Step in Regime 2/3
- **Subsystem:** Engine (mass ledger)
- **Evidence:** HeatupSimEngine.cs:1455 (one-time write), no subsequent writes in Regime 2/3 code paths
- **Trigger:** Always, after first physics step
- **Impact:** HIGH — Ledger diverges from actual state as CVCS changes accumulate
- **Classification:** Boundary accounting gap
- **Hypothesis:** The ledger was meant to be updated by the solver (in canonical mode) or by explicit boundary flow tracking. Since canonical mode is never active, no one updates it.
- **Suggested fix:** Either (a) activate canonical mode (ISSUE-0001) which inherently preserves the ledger, or (b) update `TotalPrimaryMass_lb` explicitly from component sum after solver each timestep.
- **Tests:** After CVCS operations, `TotalPrimaryMass_lb` should equal `RCS + PZR_water + PZR_steam` within 0.01 lb.
- **Confidence:** High

### ISSUE-0003: Boundary Flow Accumulators Never Incremented
- **Subsystem:** SystemState / Engine
- **Evidence:** CoupledThermo.cs:785-787 (declarations), full codebase grep shows zero assignment sites
- **Trigger:** Always — values permanently 0f
- **Impact:** MEDIUM — `UpdatePrimaryMassLedgerDiagnostics()` (dead code) would compute wrong expected mass. No runtime impact currently since the diagnostic is uncalled.
- **Classification:** Boundary accounting gap
- **Hypothesis:** Fields were declared as part of v5.3.0 canonical architecture but the increment logic was never added to the CVCS flow paths.
- **Suggested fix:** In Regime 2/3 CVCS pre-application blocks (lines 1177-1188, 1338-1352), add: `physicsState.CumulativeCVCSIn_lb += max(0, cvcsNetMass)` and `CumulativeCVCSOut_lb += max(0, -cvcsNetMass)`.
- **Tests:** After balanced CVCS, `CumulativeCVCSIn_lb` and `CumulativeCVCSOut_lb` should both be > 0 and roughly equal.
- **Confidence:** High

### ISSUE-0004: V×ρ PZR Mass Pre-Computation Redundant with Solver
- **Subsystem:** Engine (Regime 2/3 pre-solver)
- **Evidence:** HeatupSimEngine.cs:1164-1165 (Regime 2), 1318-1319 (Regime 3), then CoupledThermo.cs:286-287 (solver output)
- **Trigger:** Every timestep in Regime 2/3
- **Impact:** LOW — Functionally harmless since solver immediately overwrites, but creates confusion about who owns PZR mass state. The spray condensation (lines 1197-1211, 1361-1375) modifies these pre-computed values, which are then OVERWRITTEN by the solver output.
- **Classification:** State ownership confusion + Order-of-operations
- **Hypothesis:** The pre-computation was added in v0.9.6 to "sync" engine state to physicsState before the solver. This made sense when the solver might have needed correct PZR masses as starting point. But since the solver independently recomputes everything from V×ρ in LEGACY mode, the pre-computation (and the spray adjustment to it) is overwritten.
- **Suggested fix:** If canonical mode is activated (ISSUE-0001), the pre-computation becomes the solver's INPUT (not overwritten), so it would become relevant again. Otherwise, remove the spray adjustment of PZR masses pre-solver (since the solver ignores them) and instead apply spray post-solver.
- **Tests:** Verify spray condensation effect on pressure is unchanged after refactor.
- **Confidence:** High (code verified), impact assessment Medium

### ISSUE-0005: Regime 2/3 CVCS Double-Count Guard May Be Correct by Accident
- **Subsystem:** Engine (CVCS + solver interaction)
- **Evidence:** HeatupSimEngine.cs:1351 (sets flag), CVCS.cs:190-194 (checks flag), CoupledThermo.cs:133 (reads M_total from components)
- **Trigger:** Every timestep in Regime 2/3
- **Impact:** MEDIUM — The CVCS mass is pre-applied to `RCSWaterMass`, which feeds into the solver's `M_total`. The solver then redistributes mass via V×ρ. The double-count guard prevents the CVCS partial from applying the same flow again. This works because the CVCS delta enters M_total implicitly. But if canonical mode were activated, the CVCS delta would need to be applied to the ledger instead, and the guard logic would need revision.
- **Classification:** Order-of-operations
- **Suggested fix:** When activating canonical mode, the CVCS pre-application should modify `TotalPrimaryMass_lb` (the ledger) rather than `RCSWaterMass`. The solver would then distribute the updated ledger.
- **Tests:** Conservation error should be < 1 lb after 4 hours of balanced CVCS.
- **Confidence:** Medium (the "correct by accident" assessment is based on tracing M_total through the solver)
