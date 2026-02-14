# Implementation Plan v5.3.1 — Primary Inventory Boundary Repair (Validation Fix + Logging Refactor)

**Version:** v5.3.1  
**Date:** 2026-02-12  
**Phase:** 0 — Thermal & Conservation Stability (CRITICAL FOUNDATION)  
**Priority:** 1 of 20 — ABSOLUTE TOP PRIORITY (continuation of v5.3.0)  
**Blocks:** v5.4.0 and all Phase 1+ work until acceptance tests pass  
**Predecessor:** v5.3.0 (implementation complete, validation failed)

---

## 1. Problem Summary

### 1.1 Background: v5.3.0 Status

The v5.3.0 implementation completed all six planned stages:

1. ✅ Stage 0 — Preflight audit
2. ✅ Stage 1 — Canonical ledger field (`TotalPrimaryMass_lb`) added with transition handoff
3. ✅ Stage 2 — CVCS flows applied to ledger before solver
4. ✅ Stage 3 — CoupledThermo modified to conserve provided total (RCS as remainder)
5. ✅ Stage 4 — Relief mass applied to ledger
6. ✅ Stage 5 — Seal flow audit complete
7. ✅ Stage 6 — Instrumentation and logging added

**However, validation testing revealed failures.** The changelog was created prematurely before acceptance tests were executed.

### 1.2 Evidence of Failure

From interval log at Sim Time 10.25 hr (two-phase, RCPs ON, Regime 3):
```
Mass Conservation: FAIL
```

This indicates mass drift is occurring in the later regime despite the conservation-by-construction architecture.

### 1.3 Issues Identified

#### Issue 1: Logging Code in Wrong Partial Class

During v5.3.0 Stage 6, logging methods, formatters, and log-scheduling logic were placed directly in `HeatupSimEngine.cs` instead of the dedicated logging partial `HeatupSimEngine.Logging.cs`. This violates the partial class architecture:

```
HeatupSimEngine.cs           — Core state, lifecycle, physics dispatch
HeatupSimEngine.Logging.cs   — Event log, history buffers, file output  ← Logging belongs HERE
```

While this doesn't directly cause mass drift, it:
- Makes the codebase harder to maintain
- Could introduce confusion during debugging
- Should be corrected for code hygiene

#### Issue 2: Two-Phase Mass Ledger Still Showing Conservation Errors

Despite Stage 3's conservation-by-construction pattern, the mass conservation check still fails in two-phase. Possible causes:

1. **Handoff timing issue:** `TotalPrimaryMass_lb` may not be correctly set at bubble formation moment
2. **CVCS boundary flow not reaching solver:** The ledger may be updated correctly, but the wrong value passed to `BulkHeatupStep`
3. **Cumulative tracker mismatch:** `CumulativeCVCSIn_lb` / `CumulativeCVCSOut_lb` may be tracking differently than ledger updates
4. **Diagnostic calculation error:** The validation check itself may be computing incorrectly

---

## 2. Expected Behavior (Target)

After this patch:

| Parameter | Expected Behavior |
|-----------|-------------------|
| Mass drift (two-phase, steady CVCS) | < 0.01% per hour |
| Component sum vs ledger | Exactly equal (by construction) |
| AT-1 through AT-5 | All PASS |
| Logging code | All in `HeatupSimEngine.Logging.cs` |

---

## 3. Proposed Fix — Staged Implementation

### Stage 0 — Refactor: Move Logging to Correct Partial

**Problem:** Logging code incorrectly placed inside `HeatupSimEngine.cs` during v5.3.0 Stage 6.

**Goal:** No behavior change. Only code organization.

**Scope:**
- Move all logging methods/formatters and log-scheduling logic out of `HeatupSimEngine.cs` into `HeatupSimEngine.Logging.cs`
- Leave only minimal call sites in `HeatupSimEngine.cs` (e.g., inline `Debug.Log` statements are acceptable; dedicated methods should be in the logging partial)

**Work Items:**

1. **Identify logging code in HeatupSimEngine.cs that should move:**
   - `UpdatePrimaryMassLedgerDiagnostics()` is already defined in Logging.cs — verify no duplicate
   - Any mass ledger log formatting or scheduling logic
   - Edge detection variables for alarm states (`_previousMassConservationOK`, `_previousMassAlarmState`) should stay in main file for Unity serialization proximity, but be documented

2. **Verify existing logging partial structure:**
   - `HeatupSimEngine.Logging.cs` already contains:
     - `EventSeverity` enum and `EventLogEntry` struct
     - `LogEvent()` method
     - `AddHistory()` method
     - `SaveIntervalLog()` method
     - `SaveReport()` method
     - `InventoryAuditState` struct and related methods
     - `UpdatePrimaryMassLedgerDiagnostics()` method (v5.3.0 Stage 6)

3. **Confirm no duplication:**
   - The v5.3.0 implementation may have added mass ledger diagnostics in BOTH files
   - Remove any duplicates, keeping the authoritative version in `HeatupSimEngine.Logging.cs`

4. **Clean up inline log statements:**
   - Inline `Debug.Log` calls for mass ledger in `HeatupSimEngine.cs` main loop are acceptable (they're diagnostic)
   - The `[MASS]` compact log line formatting should be in a helper method in the logging partial if it isn't already

**Validation:**
- Build compiles without errors
- Logs still emit at same cadence as before
- No behavior change (same simulation results)

**Deliverables:**
- List of code moved
- Confirmation that logging partial contains all logging methods
- Build verification

---

### Stage 1 — Re-open Two-Phase Mass Ledger Correctness

**Goal:** Verify and fix the conservation-by-construction chain so acceptance tests pass.

**Non-negotiable architectural requirements (from v5.3.0):**

1. A single canonical ledger `TotalPrimaryMass_lb` persists across all regimes
2. CVCS boundary flows update the ledger (not component masses)
3. CoupledThermo must not overwrite boundary-applied mass changes
4. RCS mass is computed as remainder: `M_RCS = M_total - M_PZR_water - M_PZR_steam`

**Implementation Checklist:**

1. **Verify `SystemState` contains required fields:**
   ```csharp
   public float TotalPrimaryMass_lb;      // Canonical ledger
   public float InitialPrimaryMass_lb;    // For conservation checks
   public float CumulativeCVCSIn_lb;      // Boundary tracking
   public float CumulativeCVCSOut_lb;     // Boundary tracking
   public float CumulativeReliefMass_lb;  // Boundary tracking
   ```

2. **Verify solid→two-phase handoff:**
   - At bubble formation moment (when `bubbleFormed` becomes true):
     ```csharp
     physicsState.TotalPrimaryMass_lb = physicsState.TotalPrimaryMassSolid;
     physicsState.InitialPrimaryMass_lb = physicsState.TotalPrimaryMass_lb;
     ```
   - Location: `ProcessBubbleDetection()` in `HeatupSimEngine.BubbleFormation.cs`

3. **Verify CVCS integration in Regime 2/3 updates the ledger:**
   - Before solver call in both Regime 2 and Regime 3:
     ```csharp
     // Update canonical ledger FIRST (never overwritten)
     physicsState.TotalPrimaryMass_lb += cvcsNetMass_lb;
     ```
   - Location: `HeatupSimEngine.cs` in Regime 2 and Regime 3 blocks

4. **Verify CoupledThermo uses provided total as constraint:**
   - `BulkHeatupStep()` passes `physicsState.TotalPrimaryMass_lb` to solver:
     ```csharp
     var result = RCSHeatup.BulkHeatupStep(
         ref physicsState, rcpCount, rcpHeat,
         pzrHeaterPower, rcsHeatCap, pzrHeatCap, dt,
         sgHeatRemoval_MW, T_sg_bulk,
         physicsState.TotalPrimaryMass_lb);  // <-- Must pass this
     ```
   - `RCSHeatup.BulkHeatupStep()` forwards to `CoupledThermo.SolveEquilibrium()`:
     ```csharp
     CoupledThermo.SolveEquilibrium(ref state, deltaT, 50, 15f, 2700f, totalPrimaryMass_lb);
     ```
   - `CoupledThermo.SolveEquilibrium()` uses canonical mass when provided:
     ```csharp
     bool useCanonicalMass = (totalPrimaryMass_lb > 0f);
     if (useCanonicalMass)
     {
         M_total = totalPrimaryMass_lb;
         // ... later ...
         state.RCSWaterMass = M_total - state.PZRWaterMass - state.PZRSteamMass;
     }
     ```

5. **Debug: Add explicit verification logging at key points:**
   - At bubble formation handoff
   - Before and after each Regime 2/3 solver call
   - Compare ledger vs component sum after solver returns

**Specific Investigation Points:**

- **CHECK A:** Is `totalPrimaryMass_lb` parameter actually reaching `CoupledThermo.SolveEquilibrium()`?
  - Add `Debug.Log($"[CoupledThermo] totalPrimaryMass_lb={totalPrimaryMass_lb:F0}");` at solver entry

- **CHECK B:** Is `useCanonicalMass` branch executing?
  - Add `Debug.Log($"[CoupledThermo] useCanonicalMass={useCanonicalMass}");`

- **CHECK C:** After solver, does `M_RCS + M_PZR_w + M_PZR_s == TotalPrimaryMass_lb`?
  - Add verification immediately after solver returns

- **CHECK D:** Is the handoff at bubble formation setting both fields?
  - Search for where `bubbleFormed = true` is set and verify handoff code is present

**Validation:**
- Debug logs show canonical mass path executing
- Component sum equals ledger after every solver call (within 0.01 lb)
- Mass drift reduced to < 0.01% per hour

---

### Stage 2 — Acceptance Tests (Must Be Executed and Recorded)

**Do not mark complete until ALL tests pass.**

Each test must produce either:
- An interval log excerpt showing the result, OR
- A dedicated "TEST RESULT" log line

#### AT-1: Two-Phase CVCS Step Test

**Procedure:**
1. Run simulation to bubble formation (or start at two-phase state)
2. Set charging = 60 gpm, letdown = 75 gpm (net -15 gpm)
3. Run for 10 minutes sim time
4. Expected mass loss: 15 gpm × 10 min × 8.34 lb/gal ≈ 1,250 lb

**Pass Criterion:** `TotalPrimaryMass_lb` decreased by 1,250 ± 50 lb (within 4%)

**Result:** _____________ (PASS/FAIL)

---

#### AT-2: No-Flow Drift Test

**Procedure:**
1. Start at two-phase steady state
2. Set charging = letdown = 60 gpm (net 0), relief closed
3. Run for 4+ hours sim time
4. Monitor `TotalPrimaryMass_lb`

**Pass Criterion:** Ledger drift < 0.01% over 4 hours (< 60 lb for ~600,000 lb system)

**Result:** _____________ (PASS/FAIL)

---

#### AT-3: Solid→Two-Phase Transition Continuity

**Procedure:**
1. Start at solid ops, run to bubble formation
2. Record `TotalPrimaryMassSolid` just before transition
3. Record `TotalPrimaryMass_lb` just after transition

**Pass Criterion:** `TotalPrimaryMass_lb` equals pre-transition `TotalPrimaryMassSolid` within ± 1 lbm

**Result:** _____________ (PASS/FAIL)

---

#### AT-4: Relief Open Test

**Procedure:**
1. Force relief valve open (overpressure scenario)
2. Measure relief flow and duration
3. Compute expected mass loss: ∫ṁ_relief dt

**Pass Criterion:** `TotalPrimaryMass_lb` decreased by expected amount within ± 1%

**Result:** _____________ (PASS/FAIL)

---

#### AT-5: VCT Conservation Cross-Check

**Procedure:**
1. Run full heatup simulation (solid → two-phase → HZP approach)
2. Monitor `InventoryAudit` conservation error throughout
3. Compare to pre-v5.3.0 baseline

**Pass Criterion:** Conservation error bounded and improved (no masking by phantom mass)

**Result:** _____________ (PASS/FAIL)

---

### Stage 3 — Changelog (ONLY After Validation Passes)

**Do NOT create changelog until Stage 2 shows ALL FIVE tests passing.**

Create: `Critical\Updates\Changelogs\CHANGELOG_v5.3.1.md`

Include:
- Files changed
- What moved (logging refactor)
- What fixed (two-phase ledger / CoupledThermo constraint / CVCS application)
- Which AT tests were executed and PASSED (with evidence)

---

## 4. Output Requirements for Each Stage

After completing each stage, output:

1. **Files modified** (paths)
2. **Key code snippets** (only the edited sections)
3. **How to run/verify** that stage
4. **Explicit statement:** "Stage N complete. Awaiting approval to proceed."

---

## 5. Coding Rules

1. **Implement ONE STAGE per reply.**
2. After completing each stage: show changed files, key code snippets, and how to run validation.
3. Do NOT proceed to next stage until current stage is validated.
4. Do NOT create a changelog until Stage 2 validates with ALL FIVE acceptance tests passing.
5. Keep behavior changes strictly contained to the stage objectives.

---

## 6. Files Expected to Be Modified

| File | Stage | Nature of Change |
|------|-------|-----------------|
| `HeatupSimEngine.cs` | 0, 1 | Move logging code out; verify/fix ledger passing to solver |
| `HeatupSimEngine.Logging.cs` | 0, 1 | Receive logging code; verify diagnostics method |
| `HeatupSimEngine.BubbleFormation.cs` | 1 | Verify handoff at bubble formation |
| `RCSHeatup.cs` | 1 | Verify `totalPrimaryMass_lb` parameter passing |
| `CoupledThermo.cs` | 1 | Verify canonical mass branch execution |

---

## 7. Unaddressed Issues

| Issue | Disposition | Target |
|-------|------------|--------|
| Per-loop mass tracking | Out of scope | v6.0.0 |
| Secondary mass conservation | Requires feedwater | v5.6.4 |
| PRT modeling | Not needed | Future |

---

## 8. Success Criteria

This patch is successful when:

1. ✅ Logging code is in correct partial class
2. ✅ AT-1 passes (CVCS step test)
3. ✅ AT-2 passes (no-flow drift test)
4. ✅ AT-3 passes (transition continuity)
5. ✅ AT-4 passes (relief open test)
6. ✅ AT-5 passes (VCT cross-check)
7. ✅ Changelog created with evidence of all tests passing

---

*Prepared: 2026-02-12*  
*Status: READY FOR IMPLEMENTATION*
