# RISK_REGISTER.md — Systemic Risk Register
## Recovery Audit 2026-02-13

---

## Purpose

This register catalogs **systemic risks** — patterns, architectural weaknesses, and latent hazards that can **re-break the simulator even after individual issues are fixed**. Each risk is derived from the Stage 3 findings and cross-referenced to the ISSUES_REGISTER and VALIDATION_MAP.

Unlike issues (specific bugs or gaps with concrete fixes), risks describe **classes of failure** that require ongoing vigilance, architectural guardrails, or process changes to mitigate.

---

## Summary

| Risk ID | Title | Severity | Status |
|---------|-------|----------|--------|
| RISK-001 | Default Parameter Gate Pattern | HIGH | ACTIVE |
| RISK-002 | Dead Diagnostics Create False Confidence | HIGH | ACTIVE |
| RISK-003 | Formula-Only Test Suite Masks Runtime Failures | HIGH | ACTIVE |
| RISK-004 | CVCS Pre-Apply + Solver Overwrite Order-of-Operations Fragility | MEDIUM | ACTIVE |
| RISK-005 | V*rho Last-Writer-Wins Authority Pattern | MEDIUM | ACTIVE |
| RISK-006 | Stale Ledger Cascading Through Dependent Diagnostics | MEDIUM | ACTIVE |
| RISK-007 | Regime Transition Mass Discontinuity | MEDIUM | ACTIVE |
| RISK-008 | Single-Step Rebase as Initialization Assumption | LOW | ACTIVE |
| RISK-009 | Boundary Accumulator Incompleteness | MEDIUM | ACTIVE |
| RISK-010 | SG Model Trust Without Cross-Check | MEDIUM | ACTIVE |

---

## RISK-001: Default Parameter Gate Pattern
### "A single default value silently disables an entire architecture"

- **Description:** The canonical mass enforcement architecture (Rules R1-R8, implemented in CoupledThermo v5.3.0-v5.4.0) is gated by a single parameter default: `totalPrimaryMass_lb = 0f`. Because the parameter has a safe-looking default, callers can omit it without compiler error. The omission silently forces LEGACY mode. This is a **class of bug**, not a one-off mistake — any future feature gated by a default parameter is vulnerable to the same pattern.
- **Conditions that trigger it:**
  - A method signature uses `float param = 0f` to gate a code path
  - The calling site passes fewer arguments than the method accepts
  - The default value is the "off" / "disabled" / "legacy" case
  - No compile-time or runtime warning alerts that the feature is disabled
- **Current instance:** `RCSHeatup.BulkHeatupStep()` parameter 10 (`totalPrimaryMass_lb = 0f`) → ISSUE-0001
- **Detection:**
  - VALIDATION_MAP: No runtime check for `useCanonicalMass` state. The solver doesn't log which mode it's running in.
  - Stage 3C: CoupledThermo solver has no mode-announcement logging.
  - The only way to detect this today is by reading the code — no telemetry, no log line, no UI indicator shows "LEGACY MODE ACTIVE".
- **Mitigation:**
  1. **Immediate:** Fix ISSUE-0001 (pass the 10th argument at both call sites)
  2. **Architectural:** Replace the default-parameter gate with an explicit enum: `SolverMode { CANONICAL, LEGACY }`. Require callers to choose. Remove the default.
  3. **Defensive:** Add a one-time startup log: `"CoupledThermo solver mode: {CANONICAL|LEGACY}"`. If LEGACY, log as WARNING.
  4. **Process:** For any future feature gated by a parameter default, require the call site to be updated in the same commit. Code review checklist item: "Are all parameters with defaults intentionally omitted?"
- **Residual risk after fix:** LOW — once the enum pattern is adopted and the call sites are updated, the compiler enforces the choice. But any NEW default-gated feature introduced later would recreate this risk.
- **Owner:** Architecture (v5.7.0.0 hardening scope, FF-09/FF-10)
- **Cross-references:** ISSUE-0001, STAGE3_MASS_AUTHORITY_AUDIT.md Section 2

---

## RISK-002: Dead Diagnostics Create False Confidence
### "The diagnostic exists, so surely it's running"

- **Description:** `UpdatePrimaryMassLedgerDiagnostics()` is a fully implemented, 114-line diagnostic method that checks ledger-vs-component drift. It is never called. Its presence in the codebase — complete with thresholds (0.1% WARNING, 1.0% ALARM), status fields, and a comment in Init.cs referencing it — creates the **impression** that the system is being monitored. This is worse than having no diagnostic at all, because it actively misleads anyone reading the code or documentation.
- **Conditions that trigger it:**
  - A diagnostic method is implemented but has zero call sites
  - Comments or documentation reference the method as if it runs
  - Display fields populated by the method remain at zero/default — but no one notices because the default looks like "no errors"
  - Code reviewers see the method and assume it's wired up
- **Current instances:**
  - `UpdatePrimaryMassLedgerDiagnostics()` → ISSUE-0006
  - `CumulativeCVCSIn_lb`, `CumulativeCVCSOut_lb`, `CumulativeReliefMass_lb` (never incremented) → ISSUE-0003
  - Init.cs:184 comment: "read by UpdatePrimaryMassLedgerDiagnostics()" → misleading
- **Detection:**
  - VALIDATION_MAP CHECK-002 (DEAD CODE flag): Identified during Stage 2 sweep.
  - Stage 3B: DIAGNOSTIC_CALLSITE_RECOMMENDATION.md confirms zero call sites.
  - **Currently undetectable at runtime.** No log line, no alarm, no UI indicator reveals the diagnostic is inactive. The `primaryMassStatus` field shows its default (empty string / "OK"), which looks like "no problems" rather than "not checked."
- **Mitigation:**
  1. **Immediate:** Resurrect the diagnostic per ISSUE-0006 (single call-site addition)
  2. **Architectural:** Adopt a "diagnostic registration" pattern — diagnostics register themselves at init and the engine verifies all registered diagnostics have been called at least once within the first N timesteps. Log WARNING if any registered diagnostic is never invoked.
  3. **Process:** When adding diagnostic methods, the commit must include both the method AND its call site. Code review checklist: "Is the new diagnostic wired up? Prove it with a log line."
  4. **Defensive:** Change default `primaryMassStatus` from empty/"OK" to "NOT_CHECKED". If the diagnostic never runs, the UI shows "NOT_CHECKED" instead of the misleading green state.
- **Residual risk after fix:** LOW for current diagnostics. MEDIUM ongoing — any future diagnostic added without a call site recreates this risk. The registration pattern would catch it.
- **Owner:** Engine maintenance (v5.4.2.0 scope, ISSUE-0006)
- **Cross-references:** ISSUE-0003, ISSUE-0006, ISSUE-0007, VALIDATION_MAP CHECK-002, DIAGNOSTIC_CALLSITE_RECOMMENDATION.md

---

## RISK-003: Formula-Only Test Suite Masks Runtime Failures
### "All 10 tests pass — on paper"

- **Description:** The acceptance test suite (AcceptanceTests_v5_4_0.cs, 10 tests) validates that conservation formulas are mathematically correct. Every test passes by checking calculations against expected results — but **no test runs the actual simulation**. Each test contains a `// REQUIRES SIMULATION` note acknowledging this gap. The quality gate ("All 10 tests must pass before changelog creation") is satisfied vacuously. This creates a **quality illusion**: the test suite's green status gives confidence that conservation is working, while in reality ISSUE-0001 means the canonical architecture isn't even active at runtime.
- **Conditions that trigger it:**
  - Tests validate logic/formulas rather than runtime behavior
  - The test pass criteria don't require a simulation run
  - Tests reference a specific architecture (Rules R1-R8) that isn't enforced at runtime
  - "REQUIRES SIMULATION" notes are treated as future work, not blockers
- **Current instance:** AcceptanceTests_v5_4_0.cs, all 10 tests → ISSUE-0011
- **Detection:**
  - VALIDATION_MAP Section 3: All 10 tests cataloged with "formula-only" note.
  - Stage 3C Section 11: Critical gap identified.
  - **Currently undetectable by the test framework.** The tests will continue to pass even if the simulator is completely broken, because they don't exercise the simulator.
- **Mitigation:**
  1. **Immediate:** Add "SIMULATION NOT VALIDATED" label to test output header so the result is honest about its scope
  2. **Short-term (v5.4.2.0):** Embed minimal runtime checkpoints:
     - AT-02 runtime: After 4-hr balanced CVCS, check `|mass_final - mass_initial| < 60 lb`
     - AT-03 runtime: Record mass at bubble formation, compare solid vs two-phase ledger
     - AT-08 runtime: Track max PZR level delta per timestep
  3. **Medium-term (v5.5.x):** Build end-to-end test harness that runs a full simulation and evaluates all 10 AT criteria against live data
  4. **Process:** Quality gate should require "X of 10 acceptance tests pass with SIMULATION data" (not formula-only). The count can grow as runtime tests are added.
- **Residual risk after fix:** LOW once 3+ runtime tests exist. The formula-only tests remain valuable as design validation — they just shouldn't be the ONLY quality gate.
- **Owner:** Test infrastructure (v5.5.0.0 Phase 1 scope)
- **Cross-references:** ISSUE-0011, VALIDATION_MAP Section 3, SUBSYSTEM_VALIDATION_GAPS.md Section 11

---

## RISK-004: CVCS Pre-Apply + Solver Overwrite Order-of-Operations Fragility
### "It works, but only because the pieces happen to execute in exactly the right order"

- **Description:** In Regime 2/3, the CVCS mass flow is applied in a specific sequence:
  1. Engine pre-applies CVCS to `RCSWaterMass` (line 1344)
  2. Solver reads `M_total = RCS + PZR_w + PZR_s` (line 133) — CVCS delta is in M_total
  3. Solver overwrites `RCSWaterMass = V*rho` (line 278) — redistributes M_total
  4. Double-count guard prevents CVCS from being applied again (line 1351 flag)

  This works because step 2 captures the CVCS delta implicitly. But the mechanism is **fragile**: any change to the execution order (e.g., moving CVCS after solver, or having the solver NOT read M_total from components) would silently break conservation. The double-count guard (step 4) masks the underlying fragility.
- **Conditions that trigger it:**
  - A physics quantity is modified by module A, then overwritten by module B
  - The overwrite preserves the modification only as a side effect (implicit capture via sum)
  - A guard prevents re-application, masking the fact that the first application was overwritten
  - The architecture assumes a specific call order that isn't enforced by code structure
- **Current instance:** CVCS pre-apply + CoupledThermo solver + double-count guard → ISSUE-0005
- **Detection:**
  - VALIDATION_MAP CHECK-008: Double-count guard flag existence noted.
  - Stage 3A Section 1 (RCSWaterMass write-path table): Documents the full 4-step chain.
  - **Partially detectable at runtime:** CHECK-001 (`massError_lbm`) would catch a gross conservation error if the order broke. But subtle drift (e.g., from floating-point ordering) would slip through the 100 lbm threshold.
- **Mitigation:**
  1. **With ISSUE-0001 fix:** Change CVCS pre-application target from `RCSWaterMass` to `TotalPrimaryMass_lb`. The solver then reads the ledger as M_total, and computes `RCS = ledger - PZR_w - PZR_s`. The CVCS effect is captured EXPLICITLY, not implicitly. The double-count guard remains as a safety net but the architecture no longer depends on call order.
  2. **Defensive:** Add an assertion that M_total_in (before solver) equals M_total_out (after solver) within 1 lb. This catches any solver-introduced mass error regardless of call order. See ISSUE-0008.
  3. **Architectural:** Document the call-order dependency in SYSTEM_MAP.md. Mark the pre-apply block with a WARNING comment: "Must execute BEFORE solver — solver reads M_total from component sum."
- **Residual risk after fix:** LOW if CVCS targets the ledger (explicit capture). MEDIUM if CVCS still targets RCSWaterMass (implicit capture remains fragile).
- **Owner:** Engine physics (v5.4.2.0 scope, tied to ISSUE-0001/0005)
- **Cross-references:** ISSUE-0005, ISSUE-0008, STAGE3_MASS_AUTHORITY_AUDIT.md Section 1 (RCSWaterMass table), SYSTEM_MAP.md

---

## RISK-005: V*rho Last-Writer-Wins Authority Pattern
### "Multiple modules write the same field — the last one wins"

- **Description:** In Regime 2/3, `PZRWaterMass` and `PZRSteamMass` are written by up to THREE different sources in a single timestep:
  1. Engine pre-solver: `PZRWaterMass = volume * density(T_sat, P)` (HeatupSimEngine.cs:1164/1318)
  2. Engine spray: `PZRWaterMass += SteamCondensed_lbm` (lines 1200/1364)
  3. Solver post-solve: `PZRWaterMass = PZRWaterVolume * rho_water` (CoupledThermo.cs:286)

  Writer #3 (solver) always wins because it executes last. Writers #1 and #2 are wasted computation in LEGACY mode. This "last-writer-wins" pattern means the spray condensation mass adjustment (#2) has NO EFFECT on PZR mass — the solver recomputes it independently. Nobody detects this because the PZR level/mass looks reasonable from the solver's V*rho calculation.
- **Conditions that trigger it:**
  - Multiple modules write the same state field within one timestep
  - No convention or locking mechanism establishes field ownership
  - The "correct" final value happens to come from the last writer (the solver), masking the fact that intermediate writes are discarded
- **Current instance:** PZR mass fields in Regime 2/3 → ISSUE-0004
- **Detection:**
  - Stage 3A Write-Path Table (PZRWaterMass, PZRSteamMass): Documents triple-write pattern.
  - **Not detectable at runtime.** No check compares pre-solver vs post-solver PZR masses. The spray condensation effect is silently discarded.
- **Mitigation:**
  1. **With ISSUE-0001 fix:** In canonical mode, the solver reads PZR masses as INPUT (part of M_total) rather than recomputing from scratch. This makes the pre-solver writes meaningful — spray condensation would affect M_total and thus the solver's distribution.
  2. **Architectural:** Establish clear field ownership per regime. Document in SYSTEM_MAP.md: "In Regime 2/3, CoupledThermo owns PZR mass fields post-solve. Pre-solver writes to PZR mass are inputs to M_total only."
  3. **Defensive:** If spray condensation should affect physics, apply it to the LEDGER (not to PZR mass directly) before the solver runs. The solver then distributes the updated total.
- **Residual risk after fix:** LOW in canonical mode (solver reads, not overwrites). But any new module that writes to solver-owned fields would recreate this risk.
- **Owner:** Architecture (document in v5.4.2.0, enforce in v5.7.0.0)
- **Cross-references:** ISSUE-0004, STAGE3_MASS_AUTHORITY_AUDIT.md Sections 1 (PZRWaterMass, PZRSteamMass tables)

---

## RISK-006: Stale Ledger Cascading Through Dependent Diagnostics
### "If the ledger is wrong, everything that reads it is wrong too"

- **Description:** `TotalPrimaryMass_lb` freezes after the first-step rebase in Regime 2/3 (ISSUE-0002). Any diagnostic, display, or calculation that reads this field gets a stale value. Currently, the stale ledger doesn't cause visible errors because:
  - `UpdatePrimaryMassLedgerDiagnostics()` is dead code (ISSUE-0006) — it would alarm on the staleness
  - `UpdateInventoryAudit()` reads component masses directly, not the ledger
  - The UI shows component-derived values, not ledger values

  But if ISSUE-0006 is fixed (diagnostic resurrected) WITHOUT fixing ISSUE-0001/0002 (ledger staleness), the diagnostic will fire false alarms. And if any future feature reads `TotalPrimaryMass_lb` assuming it's current, it will get a value frozen at the first timestep.
- **Conditions that trigger it:**
  - A canonical "source of truth" field is written once and never updated
  - Multiple consumers read the field assuming it's current
  - Some consumers happen to use alternative sources (component sum), masking the staleness
  - New code that reads the field will silently get stale data
- **Current instance:** `TotalPrimaryMass_lb` frozen after first-step rebase → ISSUE-0002
- **Detection:**
  - `UpdatePrimaryMassLedgerDiagnostics()` (if resurrected) would detect it immediately: `drift = |ledger - componentSum|` would grow with every CVCS cycle.
  - Stage 3A Section 1 (TotalPrimaryMass_lb write-path table): Shows "Nobody" as writer in Regime 2/3.
  - **Currently undetectable at runtime.** No consumer of the ledger field reports its age or deviation from component sum.
- **Mitigation:**
  1. **Fix dependency chain in order:** ISSUE-0001 first (activates canonical mode, solver maintains ledger), then ISSUE-0003 (accumulators), then ISSUE-0006 (diagnostic). Do NOT resurrect the diagnostic before the ledger is live — it will false-alarm.
  2. **If diagnostic must be resurrected before ISSUE-0001:** Add a mode check: if `useCanonicalMass == false`, skip the ledger drift check and log "LEDGER NOT ACTIVE — drift check skipped."
  3. **Defensive:** Add a `TotalPrimaryMass_lb_lastWriteSimTime` field. Any reader can check if the ledger is stale by comparing `simTime - lastWriteSimTime > dt`.
- **Residual risk after fix:** LOW once the full dependency chain (ISSUE-0001 → 0003 → 0006) is resolved. The ledger becomes live and the diagnostic validates it.
- **Owner:** Engine physics (v5.4.2.0 dependency chain)
- **Cross-references:** ISSUE-0001, ISSUE-0002, ISSUE-0003, ISSUE-0006, DIAGNOSTIC_CALLSITE_RECOMMENDATION.md

---

## RISK-007: Regime Transition Mass Discontinuity
### "Mass might jump when switching between physics models"

- **Description:** The simulator uses three regimes (Isolated/Blended/Coupled) with different physics models. At regime boundaries:
  - Regime 1 → Regime 2: Physics shifts from isolated heating to coupled P-T-V solver (blended by alpha)
  - Regime 2 → Regime 3: Blending factor alpha reaches 1.0, fully coupled mode

  Each regime may compute mass differently:
  - Regime 1: `TotalPrimaryMassSolid` (mirror copy, boundary-conserved)
  - Regime 2/3: `M_total = RCS + PZR_w + PZR_s` (V*rho derived in LEGACY mode)

  The first-step rebase (HeatupSimEngine.cs:1452-1458) captures the initial R2/R3 mass from component V*rho — this may differ from the Regime 1 mass if the V*rho calculation at the transition temperature produces a different total than the solid-regime mass tracking.
- **Conditions that trigger it:**
  - First RCP start (transitions from Regime 1 to Regime 2)
  - Physics model change (isolated → coupled) uses different mass derivation method
  - No cross-check validates that pre-transition mass equals post-transition mass
- **Current instance:** No ISSUE filed (no evidence of significant discontinuity found in audit), but no validation check exists to detect it.
- **Detection:**
  - VALIDATION_MAP: No check specifically monitors mass across regime transitions.
  - Stage 3C Section 10: "No regime transition logging" identified as gap.
  - `UpdateInventoryAudit()` runs every timestep — it would catch a gross jump if it exceeded 500 lbm. But subtle discontinuities (10-100 lbm) would pass the threshold.
  - ISSUE-0012 (regime transition logging) would help diagnose but not prevent.
- **Mitigation:**
  1. **Add transition mass assertion:** At the first timestep after regime change, compare `M_total_new_regime` vs `M_total_old_regime`. Log WARNING if delta > 10 lbm.
  2. **Smooth the transition:** If canonical mode is activated (ISSUE-0001), the ledger carries across regimes — the solver in Regime 2/3 uses the same `TotalPrimaryMass_lb` that Regime 1 maintained. This eliminates the discontinuity by construction.
  3. **ISSUE-0012:** Add regime transition logging to make any discontinuity visible in the event log.
- **Residual risk after fix:** LOW if canonical mode is active (ledger carries across). MEDIUM if LEGACY mode is retained (V*rho at transition T/P may differ from solid-regime tracking).
- **Owner:** Engine physics (v5.4.3.0 scope, related to FF-07 RCP transients)
- **Cross-references:** ISSUE-0012, SUBSYSTEM_VALIDATION_GAPS.md Section 10, STAGE3_MASS_AUTHORITY_AUDIT.md Section 4 (Regime 1 verdict)

---

## RISK-008: Single-Step Rebase as Initialization Assumption
### "The first step seeds everything — if it's wrong, all subsequent steps are wrong"

- **Description:** The first-step rebase (HeatupSimEngine.cs:1452-1458) sets both `TotalPrimaryMass_lb` and `InitialPrimaryMass_lb` from the V*rho component sum. These values become the reference for ALL subsequent conservation checks:
  - `massError_lbm = |total - initial - externalNet|` (CVCS.cs:300)
  - `primaryMassExpected_lb = Initial + In - Out - Relief` (Logging.cs:526-529)

  If the first-step V*rho computation is inaccurate (e.g., due to transient T/P from the regime transition, or from startup artifact), the entire conservation baseline is skewed. The conservation checks would show "0 error" even if the actual mass is wrong, because both actual and expected are derived from the same potentially-wrong first-step snapshot.
- **Conditions that trigger it:**
  - First physics step occurs during a transient (regime transition, RCP startup)
  - V*rho at the transient T/P differs from the "correct" steady-state mass
  - The snapshot becomes the conservation baseline for the entire simulation
- **Current instance:** HeatupSimEngine.cs:1452-1458 (one-time rebase, only executes on `firstCoupledStep`)
- **Detection:**
  - No check validates the rebase value against an independent source (e.g., the Init values, or the Regime 1 mass at the moment of transition).
  - The conservation checks will show low error because they're self-referential (actual vs initial, both from same source).
- **Mitigation:**
  1. **Cross-check at rebase:** When setting `InitialPrimaryMass_lb`, compare against `TotalPrimaryMassSolid` (Regime 1 value). Log WARNING if delta > 50 lbm.
  2. **With canonical mode (ISSUE-0001):** The ledger is maintained continuously from Init through all regimes. The "rebase" becomes unnecessary — the canonical ledger IS the reference, updated by boundary flows. The first-step snapshot is just a verification, not an initialization.
  3. **Defensive:** Record both the rebase value AND the pre-transition value. If they differ by > 0.1%, log it.
- **Residual risk after fix:** LOW with canonical mode. In canonical mode, the rebase is a verification step, not a one-time seed.
- **Owner:** Engine physics (addressed implicitly by ISSUE-0001)
- **Cross-references:** ISSUE-0001, ISSUE-0002, STAGE3_MASS_AUTHORITY_AUDIT.md Section 1 (TotalPrimaryMass_lb table)

---

## RISK-009: Boundary Accumulator Incompleteness
### "Expected mass = initial + in - out - relief... but 'in', 'out', and 'relief' are all zero"

- **Description:** The conservation diagnostic (`UpdatePrimaryMassLedgerDiagnostics`) computes expected mass as:
  ```
  expected = InitialPrimaryMass_lb + CumulativeCVCSIn_lb - CumulativeCVCSOut_lb - CumulativeReliefMass_lb
  ```
  All three accumulators are declared in SystemState but never incremented (ISSUE-0003). This means even after the diagnostic is resurrected (ISSUE-0006), it will compute `expected = initial` regardless of CVCS operations. Any net CVCS flow will cause a false alarm ("mass drift detected!") when the real issue is missing accounting.

  More broadly, this represents a **boundary completeness** risk: every boundary flow path (charging, letdown, seal injection, CBO, relief) must be tracked in the accumulators. If a new boundary path is added without updating the accumulators, the expected-mass check will drift.
- **Conditions that trigger it:**
  - Accumulators exist but are never incremented (current state)
  - A new boundary flow path is added without incrementing the relevant accumulator
  - The expected-mass formula assumes all flows are tracked — any omission causes false alarms or masked real errors
- **Current instance:** Three accumulators at zero → ISSUE-0003
- **Detection:**
  - If diagnostic is resurrected: false alarm immediately (expected != actual after any CVCS flow)
  - Stage 3A Section 3: Full table of declarations vs. read sites vs. write sites.
  - **Currently undetectable at runtime** (diagnostic is dead, accumulators are invisible).
- **Mitigation:**
  1. **Fix ISSUE-0003:** Add increment logic at every CVCS flow application point (Regime 1 solid, Regime 2/3 pre-apply)
  2. **Completeness check:** Enumerate all boundary flow paths: charging, letdown, seal injection, CBO, relief valve, makeup, BRS return. For each, verify an accumulator is incremented. The current set (`In`, `Out`, `Relief`) may need expansion.
  3. **Cross-check:** After ISSUE-0003 fix, verify `In - Out - Relief` approximately equals `TotalPrimaryMass_lb - InitialPrimaryMass_lb` (within CVCS tolerance).
  4. **Process:** When adding a new boundary flow path, require the accumulator update in the same commit.
- **Residual risk after fix:** LOW for current flows. MEDIUM ongoing — any new boundary path without accumulator update will break the expected-mass check.
- **Owner:** Engine physics (v5.4.2.0 scope, ISSUE-0003)
- **Cross-references:** ISSUE-0003, ISSUE-0006, STAGE3_MASS_AUTHORITY_AUDIT.md Section 3

---

## RISK-010: SG Model Trust Without Cross-Check
### "The steam generator reports heat removal — nobody verifies it"

- **Description:** `SGMultiNodeThermal.Update()` returns `TotalHeatRemoval_MW`, which is consumed directly by `BulkHeatupStep()` as an input to the P-T-V solver. The SG model is trusted as a physics module with no output validation:
  - No bounds check on heat removal (could go negative or exceed input power)
  - No energy balance cross-check (SG heat removal vs primary temperature change)
  - No SG secondary mass conservation check (draining + boiloff vs initial inventory)
  - No SG secondary pressure alarm (safety valve setpoint)

  In the current heatup-only scope, the SG model is relatively simple and unlikely to produce wildly wrong results. But as the simulator expands to cooldown (v5.8.x) and power ascension (v5.9.x), the SG model becomes critical — any unvalidated error in SG heat removal directly affects primary temperature, pressure, and PZR level.
- **Conditions that trigger it:**
  - SG model produces unrealistic heat removal (negative, or exceeding gross heat input)
  - SG secondary side has a mass or energy accounting error
  - No cross-check catches the error before it propagates to the primary side
- **Current instances:** ISSUE-0009 (no energy balance), ISSUE-0010 (no pressure alarm)
- **Detection:**
  - VALIDATION_MAP: No SG-specific runtime checks cataloged (only the formula-only AT-10).
  - Stage 3C Section 6: Three gaps identified (energy balance, mass conservation, pressure alarm).
  - **Currently undetectable at runtime** until primary-side alarms fire (subcooling, pressure — significant delay).
- **Mitigation:**
  1. **Immediate (v5.4.2.0):** Add SG heat removal sanity check: `assert sgHeatTransfer_MW >= 0 && sgHeatTransfer_MW <= grossHeat * 2.0` (ISSUE-0009)
  2. **Short-term:** Add SG secondary pressure alarm at 1085 psia (ISSUE-0010)
  3. **Medium-term (v5.6.0.0):** Full SG energy/mass/pressure validation per FF-01/02/03 scope
  4. **Defensive:** Add a cumulative energy balance check: `sum(sgHeatRemoval * dt) vs primary energy change` over the simulation. Log WARNING if cumulative mismatch > 5%.
- **Residual risk after fix:** LOW for heatup scope. MEDIUM for cooldown/power ascension (SG model complexity increases).
- **Owner:** SG physics (v5.6.0.0 Phase 2 scope, FF-01/02/03/08)
- **Cross-references:** ISSUE-0009, ISSUE-0010, SUBSYSTEM_VALIDATION_GAPS.md Section 6

---

## RISK INTERACTION MATRIX

The following matrix shows how risks compound when multiple are active simultaneously:

```
                R001  R002  R003  R004  R005  R006  R007  R008  R009  R010
RISK-001  (---)  AMPF  AMPF  DEPS  DEPS  DEPS  RELS  DEPS   --    --
RISK-002  AMPF  (---)  AMPF   --    --   AMPF   --    --   AMPF   --
RISK-003  AMPF  AMPF  (---)   --    --    --    --    --    --    --
RISK-004  DEPS   --    --   (---)  RELS   --    --    --    --    --
RISK-005  DEPS   --    --   RELS  (---)   --    --    --    --    --
RISK-006  DEPS  AMPF   --    --    --   (---)   --   RELS  DEPS   --
RISK-007  RELS   --    --    --    --    --   (---)  RELS   --    --
RISK-008  DEPS   --    --    --    --   RELS  RELS  (---)   --    --
RISK-009  --   AMPF   --    --    --   DEPS   --    --   (---)   --
RISK-010  --    --    --    --    --    --    --    --    --   (---)

DEPS = dependent (fixing one helps the other)
AMPF = amplifies (both active makes each worse)
RELS = related (share root cause or mitigation)
--   = independent
```

**Critical amplification loop:** RISK-001 + RISK-002 + RISK-003 form a reinforcing triangle:
- Canonical mode is off (R001) → diagnostics that would detect it are dead (R002) → tests that would catch runtime failures are formula-only (R003) → no signal reaches the developer that canonical mode is off.

This is the **keystone risk cluster**. Fixing ISSUE-0001 (R001) breaks the loop and reduces R002/R003 from "actively misleading" to "incomplete coverage."

---

## RESOLUTION PRIORITY

Based on the interaction matrix and the dependency chain from ISSUES_REGISTER.md:

| Priority | Risk | Resolved By | Version Target |
|----------|------|-------------|----------------|
| 1 | RISK-001 | ISSUE-0001 (pass 10th arg) + enum refactor | v5.4.2.0 |
| 2 | RISK-006 | ISSUE-0001 (live ledger) + ISSUE-0003 (accumulators) | v5.4.2.0 |
| 3 | RISK-002 | ISSUE-0006 (resurrect diagnostic) + registration pattern | v5.4.2.0 |
| 4 | RISK-009 | ISSUE-0003 (increment accumulators) + completeness audit | v5.4.2.0 |
| 5 | RISK-004 | ISSUE-0005 (CVCS → ledger) + ISSUE-0008 (solver check) | v5.4.2.0 |
| 6 | RISK-005 | ISSUE-0004 (cleanup) + field ownership docs | v5.4.2.0 / v5.7.0.0 |
| 7 | RISK-003 | ISSUE-0011 (runtime test points) | v5.5.0.0 |
| 8 | RISK-007 | ISSUE-0012 (transition logging) + transition assertion | v5.4.3.0 |
| 9 | RISK-008 | Canonical mode (ISSUE-0001 auto-resolves) | v5.4.2.0 |
| 10 | RISK-010 | ISSUE-0009 + ISSUE-0010 + FF-01/02/03 | v5.6.0.0 |
