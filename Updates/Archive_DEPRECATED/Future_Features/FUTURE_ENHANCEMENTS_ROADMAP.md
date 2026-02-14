# Future Enhancements Roadmap
## Critical: Master the Atom — NSSS Simulator

**Document Version:** 5.6
**Last Updated:** 2026-02-13
**Maintainer:** Project Development Team
**Aligned With:** Master Development Roadmap v3.0
**Current Simulator Version:** v5.4.1 (Complete)

---

## Overview

This document tracks all planned enhancements for the Critical: Master the Atom NSSS simulator. It is organized to align with the **Master Development Roadmap v3.0**, which defines strict execution phases with exit criteria that must be satisfied before later phases may begin.

Every work item is assigned to a **specific version release**. Versions v5.0.0 through v5.4.1 used three-level semantic versioning. **Starting after v5.4.1**, all new versions use four-level structured versioning (`Major.Minor.Patch.Revision`) per the Versioning Policy. See `VERSIONING_POLICY.md` for definitions and governance rules. Historical version numbers are not retroactively amended.

**Governing Rule:** No Phase 1+ work may begin until Phase 0 exit criteria are met.

### SG Validation Closure Update (2026-02-14)
- IP-0015 (`DP-0003` SG secondary physics stabilization) is formally closed with Stage E PASS evidence.
- Evidence:
  - `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164359.md`
  - `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_164456.md`
- Closure scope completed:
  - `CS-0014`, `CS-0015`, `CS-0016`, `CS-0018`, `CS-0047`, `CS-0048`

---

## 🚨 v5.4.0 — Primary Mass & Pressurizer Stabilization Release (PARTIALLY COMPLETE)

**Status:** PARTIALLY COMPLETE — v5.4.1 resolved audit/pressurization items; mass conservation residuals targeted for v5.4.2.0
**Phase:** Core Physics Stabilization
**Priority:** CRITICAL — Blocks further thermodynamic modules including SG boiling and UI stabilization
**Implementation Plan:** `Critical\Updates\IMPLEMENTATION_PLAN_v5.4.0.md`

### Problem Statement

Multiple interconnected physics issues have been identified that prevent reliable simulation across operating regimes:

1. **Bubble Formation Drain Semantics** — Current drain takes ~2 hours; steam boiloff treated as liquid shrink instead of volume expansion
2. **Inventory Discontinuity** — Mass conservation holds in solid regime but breaks after switch to two-phase; canonical ledger not enforced in solver
3. **RVLIS Level Inconsistency** — RCSWaterMass not updated during drain, causing RVLIS to read stale mass (~88%)
4. **PZR Level Spike on RCP Start** — Sharp upward transient observed, likely one-frame canonical overwrite or density ordering issue
5. **VCT Conservation Error Growth** — VCT verifier shows ~1,600–1,700 gal error at 15.75 hr; likely caused by RCS-side accounting drift propagating into VCT verification equation
6. **SG Boiling Does Not Pressurize Secondary** — During 100% boiling, SG pressure pinned near atmospheric (~3 psig) instead of rising; steam generation not building inventory in closed steam space

### Design Objectives

This release establishes architectural rules for mass handling:

- **Single canonical primary mass ledger** across ALL regimes
- **No solver allowed to redefine total mass via V×ρ**
- **Bubble formation driven primarily by steam volume growth**, not liquid shrink
- **RCSWaterMass derived from ledger remainder** during drain
- **Transient consistency during RCP spool-up**
- **VCT verification aligned with canonical RCS ledger** — no secondary sources of truth
- **SG secondary pressure solved from steam inventory** when isolated (closed-volume model)

### Why This Blocks Everything

Until mass conservation and pressurizer physics are semantically correct:
- SG boiling models cannot be validated (energy balance depends on mass balance)
- Dashboard displays will show incorrect values
- Two-phase CVCS operations will drift
- Mode 4 → Mode 3 transition physics cannot be trusted

### Staged Implementation

| Stage | Description |
|-------|-------------|
| 0 | Baseline logging & acceptance criteria |
| 1 | Bubble formation volume displacement correction |
| 2 | Drain-phase mass reconciliation + RVLIS fix |
| 3 | Canonical mass unification across regimes |
| 4 | RCP transient spike diagnosis + correction |
| 5 | VCT conservation error diagnosis + reconciliation |
| 6 | SG secondary pressure / steam inventory diagnosis |
| 7 | Validation suite & regression testing |

### Acceptance Criteria

- Drain duration within realistic band (not 2 hours for normal operation)
- Inventory conservation tolerance < 0.1% across all regimes
- No RVLIS drop unless true boundary mass loss occurs
- No PZR spike > 0.5% per timestep on RCP start
- Stable mass across regime transitions
- VCT conservation error < 10 gal steady-state over multi-hour runs
- SG pressure rises appropriately when isolated and boiling

---

## ⚠️ v5.3.0 IN PROGRESS — Primary Inventory Boundary Repair (VALIDATION FAILED)

**Status:** ⚠️ IN PROGRESS — VALIDATION FAILED (2026-02-12)
**Priority:** #2 — Superseded by v5.4.0 restructuring
**Implementation Plan:** `Critical\Updates\IMPLEMENTATION_PLAN_v5.3.0.md`

### What Was Implemented (Stages 0-6 Complete)

1. **Canonical Two-Phase Primary Mass Ledger** — Added `TotalPrimaryMass_lb` to `SystemState`, persists across all regimes
2. **CVCS Boundary Flows to Ledger** — Boundary flows update the canonical ledger (never overwritten by solver)
3. **CoupledThermo Conserves Provided Total** — Solver uses `TotalPrimaryMass_lb` as constraint; RCS mass computed as remainder
4. **Relief Mass Is Real Boundary Sink** — Relief valve flow subtracted from ledger with cumulative tracking
5. **Seal Flow Accounting Corrected** — 3 gpm/pump seal leakoff to VCT properly excluded from RCS boundary flow
6. **Comprehensive Diagnostics** — Mass ledger display fields, alarm thresholds, and compact logging added

### Why Not Complete?

Despite implementation of all planned stages, **validation testing has revealed failures:**

- **Two-phase run still fails conservation validation** — At Sim Time 10.25 hr, interval log shows **"Mass Conservation: FAIL"** while operating in Regime 3 (RCPs ON / two-phase).
- **Acceptance tests AT-1 through AT-5 are NOT passing** — Re-opened as PENDING/FAIL.
- **Root cause under investigation** — The conservation-by-construction pattern in CoupledThermo may have an implementation gap, or logging may be incorrectly placed and causing diagnostic confusion.

### Status

**Superseded by v5.4.0** — The issues identified in v5.3.0 validation are symptomatic of deeper architectural problems addressed comprehensively in v5.4.0.

---

## ⏳ v5.3.1 DEPRECATED — Folded into v5.4.0

**Status:** DEPRECATED
**Disposition:** Work items folded into v5.4.0 Primary Mass & Pressurizer Stabilization Release

The logging refactor and two-phase ledger correctness fixes originally planned for v5.3.1 are now incorporated as stages within v5.4.0 for a unified approach to mass conservation.

---

## Version Map Summary

> **Note:** Versions v5.0.1 through v5.4.1 use historical 3-level numbering (not retroactively amended). All versions from v5.4.2.0 onwards use 4-level structured versioning per `VERSIONING_POLICY.md`.

### Completed Versions (Historical — 3-Level)

| Version | Phase | Scope | Status |
|---------|-------|-------|--------|
| **v5.0.1** | 0 | Heat-Up Stability Patch | **COMPLETE** |
| **v5.0.2** | 0 | Solid PZR Mass Conservation Fix | **COMPLETE** |
| **v5.0.3** | 0 | Inventory Audit Reconciliation Patch | **COMPLETE** |
| **v5.1.0** | 0 | SG Pressure–Saturation Coupling Correction | **COMPLETE** |
| **v5.2.0** | 0 | CRITICAL Tab (At-a-Glance Validation Overview) | **COMPLETE** |
| **v5.3.0** | 0 | Primary Inventory Boundary Repair (Two-Phase Mass Ledger) | ⚠️ VALIDATION FAILED |
| **v5.3.1** | — | ~~Validation Fix + Logging Refactor~~ | DEPRECATED (folded into v5.4.0) |
| **v5.4.0** | 0 | **Primary Mass & Pressurizer Stabilization Release** | **PARTIALLY COMPLETE** — residuals → v5.4.2.0 |
| **v5.4.1** | 0 | **Inventory Audit Fix + Startup Pressurization Stabilization** | **COMPLETE** ✅ (current baseline) |

### Planned Versions (4-Level — from v5.4.2.0 onwards)

| Version | Phase | Scope | FF Ref | Status |
|---------|-------|-------|--------|--------|
| **v5.4.2.0** | 0 | Mass Conservation Tightening (Phase 0 Residuals) | FF-05 (4 of 5) | **IMPLEMENTED — PENDING VALIDATION** |
| **v5.4.3.0** | 0 | RCP Startup Transient Fidelity | FF-07 | BLOCKED by v5.4.2.0 |
| **v5.4.4.0** | 0 | VCT Conservation & Flow Boundary Audit | FF-06 | BLOCKED by v5.4.2.0 |
| **v5.5.0.0** | 1 | Validation Dashboard Redesign | — | BLOCKED by Phase 0 exit |
| **v5.5.1.0** | 1 | Scenario Selector Framework | — | BLOCKED by Phase 0 exit |
| **v5.5.2.0** | 1 | 200°F Heatup Hold | — | BLOCKED by Phase 0 exit |
| **v5.5.3.0** | 1 | In-Game Help System (F1) | — | BLOCKED by Phase 0 exit |
| **v5.6.0.0** | 2 | SG Energy, Pressure & Secondary Boundary Corrections | FF-01/02/03/08 | BLOCKED by Phase 1 exit |
| **v5.6.1.0** | 2 | SG Constants Calibration & Sensitivity Audit | FF-04 | BLOCKED by v5.6.0.0 |
| **v5.6.2.0** | 2 | Placeholder Wiring + Core Data Validation | — | BLOCKED by Phase 1 exit |
| **v5.6.3.0** | 2 | PORV/SV Valve Models + CVCS Flow Models | — | BLOCKED by Phase 1 exit |
| **v5.6.4.0** | 2 | CCW System Model | — | BLOCKED by Phase 1 exit |
| **v5.6.5.0** | 2 | Heat Exchanger Models | — | BLOCKED by Phase 1 exit |
| **v5.6.6.0** | 2 | BRS Enhancements | — | BLOCKED by Phase 1 exit |
| **v5.6.7.0** | 2 | Excess Letdown Path Model | — | BLOCKED by Phase 1 exit |
| **v5.6.8.0** | 2 | VCT/CCP Thermal & Pump Models | — | BLOCKED by Phase 1 exit |
| **v5.6.9.0** | 2 | Active Operator SG Pressure Management | — | BLOCKED by v5.6.0.0 |
| **v5.7.0.0** | Arch | Thermal/Mass/Flow Architecture Hardening | FF-09/10 | BLOCKED by Phase 2 exit |
| **v5.7.1.0** | Arch | Multicore / Performance & Determinism | — | BLOCKED by v5.7.0.0 |
| **v5.8.0.0** | 3 | Cooldown Procedures & AFW | — | BLOCKED by Phase 2 exit |
| **v5.8.1.0** | 3 | Advanced Steam Dump Modes | — | BLOCKED by v5.8.0.0 |
| **v5.9.0.0** | 4 | Power Ascension & Turbine-Generator | — | BLOCKED by Phase 3 exit |
| **v6.0.0.0** | 5 | Full NSSS Thermal Realism | — | BLOCKED by Phase 4 exit |

---

## v5.4.1 — Inventory Audit Fix + Startup Pressurization Stabilization

**Status:** COMPLETE (2026-02-13)
**Phase:** Core Physics Stabilization
**Implementation Plan:** `Critical\Updates\IMPLEMENTATION_PLAN_v5.4.1_AUDIT_FIX.md`
**Changelog:** `Critical\Updates\Changelogs\CHANGELOG_v5.4.1_AUDIT_FIX.md`

This patch resolved five interconnected issues in the solid-plant pressurizer regime:
1. **Orphaned canonical mass fields** -- 4 SystemState fields never assigned, INVENTORY AUDIT reported 0 lbm
2. **Conservation cliff** -- ~824k lbm error at solid-to-two-phase transition (reduced to ~20k pre-existing step)
3. **VALIDATION STATUS** -- Mass Conservation check corrected (massError_lbm, not VCT gallons)
4. **Cold-start pressurization** -- Restored to 114.7 psia, heater-driven (HEATER_PRESSURIZE/HOLD_SOLID)
5. **CVCS actuator dynamics** -- Pressure filter, lag, slew-rate limiter for smooth pressure traces

**Note:** The original v5.4.1 scope (PZR DRAIN Mass Spike + Inventory Audit Baseline Type Mismatch) has been deferred. Those issues remain in Technical Debt and are not blocked by this closure.

---

### Part A: PZR DRAIN Mass Spike Fix

#### Problem Statement (Stage 7 Validation Evidence)

During v5.4.0 Stage 7 validation testing (2026-02-12), the following mass conservation behavior was observed:

| Time | Phase | Total Mass | Error (lbm) | Error % | Status |
|------|-------|------------|-------------|---------|--------|
| 8.25 hr | SOLID | 924,212 | 56.3 | 0.006% | ✅ OK |
| 8.50 hr | VERIFICATION | 924,219 | 62.6 | 0.007% | ✅ OK |
| 8.75 hr | **STABILIZE/DRAIN** | 920,401 | **3,755** | **0.406%** | ⚠️ ALARM |
| 9.00 hr | COMPLETE | 925,196 | 1,040 | 0.113% | ⚠️ ALARM |
| 9.50 hr | COMPLETE + 1 RCP | 924,982 | 1,039 | 0.112% | ⚠️ ALARM |
| 9.75 hr | COMPLETE + 2 RCPs | 924,824 | 1,005 | 0.109% | ⚠️ ALARM |

**Key Observations:**
1. **Solid→Two-Phase transition is clean** — Error remains ~60 lbm (0.007%) through VERIFICATION phase
2. **DRAIN phase causes massive spike** — Error jumps to 3,755 lbm (0.406%) during STABILIZE
3. **Error partially recovers** — Drops to ~1,000 lbm (0.11%) by bubble COMPLETE
4. **Error then stabilizes** — Remains at ~1,000 lbm through RCP starts and regime changes
5. **Steady-state exceeds target** — 0.11% is above the 0.1% acceptance criterion

#### Root Cause Hypothesis

The error pattern suggests a **volume↔mass transfer semantics mismatch** and/or **canonical measurement source mismatch** during the DRAIN window:

1. **During DRAIN:** PZR level drops from 100% to 25% as liquid is displaced to RCS
2. **Possible issues:**
   - Steam volume growth may be using geometric displacement instead of mass-based transfer
   - dm_transfer calculation may not match the actual state variable updates
   - CANONICAL_TWO_PHASE may be reading stale or alternate buckets during DRAIN
   - Update ordering: drain may execute after canonical mass computation
3. **Recovery mechanism:** Once DRAIN completes, the steady-state mass accounting "catches up" but retains ~1,000 lbm offset

#### Staged Implementation Plan

##### Stage 0 — Instrumented Mass Ledger (Investigation, MANDATORY)

**Objective:** Capture detailed forensic data during PZR DRAIN to identify the exact source of mass drift.

**Implementation:**
1. During PZR DRAIN phase only (when `BubblePhase == DRAIN` or `STABILIZE`), log the following **per timestep**:
   - **Before/After masses:** PZR liquid mass, PZR vapor mass, RCS mass (all buckets used by CANONICAL_TWO_PHASE)
   - **Drain terms:** `dV_drain`, `rho_used`, `dm_transfer`, any bubble/steam displacement terms
   - **Clamp hits:** Any min/max clamp activations on mass or volume
   - **Step Δm_total:** Change in total canonical mass this timestep
   - **Forensic flag:** If `|Δm_total| > 10 lbm`, emit detailed forensic line with all terms

2. **Output format:** CSV to `Forensics/DRAIN_MassLedger_*.csv`
3. **Fields:** `SimTime_hr, Phase, PZR_Liquid_Before, PZR_Liquid_After, PZR_Vapor_Before, PZR_Vapor_After, RCS_Before, RCS_After, dV_drain, rho_drain, dm_transfer, dm_steam, Canonical_Before, Canonical_After, Delta_Total, Clamp_Flags, Forensic_Detail`

**Acceptance Criteria:**
- [ ] Forensic logging active during DRAIN/STABILIZE phases
- [ ] All mass terms captured with sufficient precision (0.1 lbm)
- [ ] Large Δm events (>10 lbm) flagged with detailed breakdown

##### Stage 1 — Fix Transfer Semantics

**Objective:** Enforce mass-based transfer semantics for all inter-component flows.

**Rules to Enforce:**
1. **All inter-component transfers are mass-based (dm)**
   - Volumes and levels are DERIVED outputs, never primary transfer quantities
   - `dm_out_of_PZR = dm_into_RCS` (exactly, in the same timestep)

2. **Steam mass changes only via explicit energy-based boiloff**
   - Steam mass increases due to boiling (energy → latent heat → phase change)
   - Steam mass does NOT increase due to "geometric displacement"
   - If liquid is displaced, steam VOLUME grows but steam MASS only changes via thermodynamics

3. **Conservation by construction:**
   ```
   dm_PZR_liquid + dm_PZR_steam + dm_RCS = 0  (for internal transfers)
   dm_total = dm_boundary_only (CVCS net, relief, etc.)
   ```

**Implementation:**
- Audit `BubbleFormation.cs` drain logic
- Audit `PressurizerPhysics.cs` displacement calculations
- Ensure no code path modifies canonical mass except boundary flows

**Acceptance Criteria:**
- [ ] All transfer code uses mass (lbm) as primary quantity
- [ ] No geometric volume used to compute mass changes
- [ ] dm_out = dm_in for all internal transfers

##### Stage 2 — Canonical Measurement Audit

**Objective:** Verify CANONICAL_TWO_PHASE reads the correct, current state variables.

**Audit Points:**
1. **State variable identity:** Confirm CANONICAL_TWO_PHASE reads the EXACT variables updated during DRAIN
   - No stale copies or alternate buckets
   - No "last frame" values used for "this frame" computation

2. **Update ordering:** Verify execution sequence:
   ```
   1. DRAIN updates execute (modify PZR liquid, RCS mass)
   2. Canonical mass computation runs (reads updated values)
   3. Conservation check runs (compares ledger to component sum)
   ```
   - If canonical computation runs BEFORE drain updates, it will read stale values

3. **Variable aliasing:** Check for any cases where:
   - A display variable is used instead of the physics variable
   - A smoothed/filtered value is used instead of the raw state

**Implementation:**
- Add temporary debug assertions during DRAIN to verify read/write ordering
- Trace data flow from drain logic through canonical computation

**Acceptance Criteria:**
- [ ] CANONICAL_TWO_PHASE reads post-drain state variables
- [ ] Update ordering verified: drain → canonical → conservation check
- [ ] No stale/aliased variables in canonical computation

---

### Part B: Inventory Audit Baseline Type Mismatch Fix

#### Problem Statement

The dashboard/validation compares **INITIAL system inventory** in geometric gallons (ft³→gal from `PlantConstants` at t=0) against **SYSTEM TOTAL** computed as mass-derived gallons (`RCSWaterMass / ρ(T,P)` → ft³ → gal). This produces phantom "inventory errors" as density changes with temperature, even when mass is conserved.

The "SYSTEM TOTAL" and "INV ERROR" displays in the dashboard compare **thermodynamically incompatible quantities**:

| Metric | Calculation | Type |
|--------|-------------|------|
| `initialSystemInventory_gal` (at t=0) | `RCS_WATER_VOLUME × FT3_TO_GAL` | **Geometric volume** |
| `totalSystemInventory_gal` (at t>0) | `RCSWaterMass / ρ(T,P) × FT3_TO_GAL` | **Mass-derived volume** |

As water heats from 100°F to 550°F:
- **Mass is conserved** (constant within CVCS boundary flows)
- **Volume increases ~25%** due to thermal expansion (ρ decreases from ~62 to ~53 lb/ft³)

The current `systemInventoryError_gal` calculation:
```csharp
systemInventoryError_gal = |totalSystemInventory_gal - initialSystemInventory_gal - externalNet|
```

This compares **mass-derived gallons** against a **geometric baseline**, creating a phantom "error" of **~13,000+ gallons** that is purely thermal expansion — not an actual conservation failure.

### Root Cause Analysis

**Data Flow Chain:**
```
PlantConstants.RCS_WATER_VOLUME = 11,500 ft³ (geometric)
    ↓
Initialization (t=0): rcsVol_gal = 11,500 × 7.48 = 86,026 gal (geometric) ✅
    ↓
Runtime (t>0): rcsVol_gal = RCSWaterMass / ρ(T,P) × 7.48 (mass-derived) ⚠️
    ↓
Comparison: mass-derived vs geometric = TYPE C/D VIOLATION ⛔
```

**At 100°F:** ρ ≈ 62.0 lb/ft³ → 713,000 lb in 11,500 ft³
**At 400°F:** ρ ≈ 53.6 lb/ft³ → same 713,000 lb occupies 13,302 ft³
**Phantom "error":** |13,302 - 11,500| × 7.48 ≈ **13,474 gallons**

This is **not a bug in mass conservation** — it is a **bug in the display metric**.

### Design Objectives

1. **Deprecate or replace `systemInventoryError_gal`** — This metric is fundamentally flawed
2. **Use canonical mass ledger for conservation display** — `primaryMassDrift_lb` is the correct metric
3. **Update dashboard panels** to show mass-based conservation status
4. **Remove misleading "INV ERROR" from Validation tab** or relabel with correct semantics

### Proposed Fix

Make inventory validation **canonical in MASS (lbm)**:

1. **Store `initialSystemMass_lbm` at init** — Total primary mass at t=0
2. **Compute `totalSystemMass_lbm` at runtime** — Sum of all compartment masses
3. **Compute `externalNetMass_lbm`** — True boundary flows only (RWST makeup, relief losses, etc.)
4. **Define mass error:** `massError_lbm = |totalMass - initialMass - externalNetMass|`
5. **Optional:** Display "EqVolError" as gallons @ reference density for operator familiarity
6. **Keep geometric gallons only for capacity/level display** — Never use for conservation checks

### Alternative Options (Not Recommended)

**Option B: Reference Density Normalization**
- Normalize all volumes to a reference density (e.g., 62.0 lb/ft³ at 100°F)
- `rcsVol_gal_normalized = (RCSWaterMass / 62.0) × FT3_TO_GAL`
- Less intuitive but maintains gallon units

**Option C: Deprecate System Inventory Display**
- Remove the "SYSTEM TOTAL" / "INITIAL" / "INV ERROR" section entirely
- Rely solely on the canonical mass ledger diagnostics

### Files Affected

| File | Change |
|------|--------|
| `HeatupSimEngine.cs` | Remove or repurpose `totalSystemInventory_gal`, `initialSystemInventory_gal`, `systemInventoryError_gal` |
| `HeatupSimEngine.Init.cs` | Update initialization to use mass-based metrics |
| `HeatupSimEngine.CVCS.cs` | Remove flawed `systemInventoryError_gal` calculation in `UpdateVCT()` |
| `HeatupValidationVisual.Panels.cs` | Update `DrawInventoryPanel()` to display mass-based conservation |
| `HeatupValidationVisual.TabValidation.cs` | Update validation checks to use correct metric |

### Acceptance Criteria

- **During heatup with density changes, dashboard "inventory error" remains bounded (~≤1 lbm)**
- Inventory error matches **Inventory Audit status OK** (no discrepancy between display and internal validation)
- **No large gallon swings from thermal expansion appear as errors**
- Conservation metric uses canonical mass ledger (`TotalPrimaryMass_lb`)
- Dashboard correctly reports < 0.01% drift during balanced CVCS operations
- No false FAIL indications in validation tab due to thermal expansion

---

### Combined Validation Criteria (v5.4.1)

Re-run v5.4.0 Stage 7 validation and confirm:

| Criterion | Target | Current (v5.4.0) |
|-----------|--------|------------------|
| DRAIN spike eliminated | No spike >100 lbm | ⛔ 3,755 lbm spike |
| Steady-state error | ≤ 0.1% | ⚠️ 0.11% |
| RVLIS stability | No spurious drops >1% | ✅ Pass |
| RCP start behavior | No level spike >0.5%/step | ✅ Pass |
| Dashboard inventory error | Bounded (~≤1 lbm) | ⛔ ~13,000 gal phantom error |
| Inventory Audit status match | Display matches internal OK | ⛔ Mismatch |

**Exit Criteria:**
- [ ] DRAIN phase error remains < 100 lbm (no spike)
- [ ] Steady-state error ≤ 0.1% (meets AC-2)
- [ ] Dashboard "inventory error" bounded, no thermal expansion artifacts
- [ ] Inventory Audit status matches dashboard display
- [ ] All AT-1 through AT-10 pass
- [ ] No regressions in RVLIS, RCP start, or other validated behaviors

### Files Affected (Combined)

| File | Part | Changes |
|------|------|--------|
| `HeatupSimEngine.BubbleFormation.cs` | A | Forensic logging, drain semantics audit |
| `PressurizerPhysics.cs` | A | Displacement transfer semantics |
| `CoupledThermo.cs` | A | Canonical measurement verification |
| `SystemState.cs` | A | State variable audit |
| `HeatupSimEngine.cs` | A, B | Forensic output triggers; remove/repurpose inventory gal fields |
| `HeatupSimEngine.Init.cs` | B | Update initialization to use mass-based metrics |
| `HeatupSimEngine.CVCS.cs` | B | Remove flawed `systemInventoryError_gal` calculation |
| `HeatupValidationVisual.Panels.cs` | B | Update `DrawInventoryPanel()` for mass-based conservation |
| `HeatupValidationVisual.TabValidation.cs` | B | Update validation checks to use correct metric |

### Implementation Notes

- The canonical mass ledger infrastructure already exists from v5.0.2 through v5.4.0
- `primaryMassDrift_lb` and `primaryMassDrift_pct` are already computed in `UpdatePrimaryMassLedgerDiagnostics()`
- Part A (DRAIN spike) is a **physics fix**
- Part B (inventory display) is primarily a **display refactor**
- Both issues share root cause: inconsistent use of mass vs volume metrics

---

# Phase 0 — Thermal & Conservation Stability (CRITICAL FOUNDATION)

**Status:** IN PROGRESS (v5.0.1–v5.4.1 complete, residuals remain — v5.4.2.0 through v5.4.4.0)
**Purpose:** Resolve all numerical and thermodynamic discontinuities. This is the **simulation-critical foundation** — no UI, scenario, or turbine work may proceed until this phase exits successfully.

**Phase 0 Planned Releases:**
- v5.4.2.0 — Mass Conservation Tightening (FF-05)
- v5.4.3.0 — RCP Startup Transient Fidelity (FF-07)
- v5.4.4.0 — VCT Conservation & Flow Boundary Audit (FF-06)

---

## v5.4.0 — Primary Mass & Pressurizer Stabilization Release

**Status:** PARTIALLY COMPLETE — v5.4.1 resolved audit/pressurization items; mass conservation residuals targeted for v5.4.2.0. See `TRIAGE_v5.4.1_PostStabilization.md` FF-05 for detailed code-level issue list.
**Implementation Plan:** `Critical\Updates\IMPLEMENTATION_PLAN_v5.4.0.md`

### Problem Statement

The current physics implementation has fundamental issues in how mass is tracked and how pressurizer state transitions are handled:

#### Issue #1 — Bubble Formation Drain Semantics
- Current drain behavior takes ~2 hours (unrealistic)
- Steam boiloff is treated as liquid shrink instead of volume expansion
- Volume displacement semantics are inverted

#### Issue #2 — Inventory Discontinuity Across Regimes
- Mass conservation holds in solid regime (v5.0.2 fix)
- Conservation breaks after switch to two-phase
- Canonical ledger not enforced in solver — V×ρ calculations override boundary-corrected mass

#### Issue #3 — RVLIS Level Inconsistency (~88% drop)
- `RCSWaterMass` not updated during drain operations
- RVLIS indicator reads stale mass value
- Creates false indication of inventory loss

#### Issue #4 — PZR Level Spike on RCP Start
- Sharp upward transient in pressurizer level when RCPs start
- Likely causes: one-frame canonical overwrite, density ordering issue, or thermal expansion double-counting
- Creates unrealistic plant behavior during startup

#### Issue #5 — VCT Conservation Error Growth (~1,600–1,700 gal)
- Dashboard shows VCT CONS ERR ~1.7k gal (failing) at 15.75 hr
- VCT internal flows may be correct, but verifier depends on RCS-side `rcsInventoryChange_gal` term
- If primary-side accounting drifts or is overwritten, VCT verification fails even with correct VCT flows
- Creates cascading validation failures masking root cause

#### Issue #6 — SG Boiling Does Not Pressurize Secondary
- During "BOILING (100%)" state, SG pressure remains ~3 psig (~17 psia) with T_sat ~220°F
- Large SG heat transfer is occurring, but pressure does not rise
- If SG is isolated (closed steam space), boiling should generate steam inventory and drive pressure upward
- Suggests pressure is either clamped from T_sat or held down by implicit steam sink

### Design Objectives

Architectural rules that MUST be enforced:

1. **Single canonical primary mass ledger** — `TotalPrimaryMass_lb` is the ONLY source of truth for total RCS mass across ALL regimes
2. **Boundary-only mass modification** — Only CVCS net flow and relief valve discharge may modify the canonical ledger
3. **No V×ρ overwrite** — Solvers may NOT recalculate total mass from volume × density; they must accept the ledger value as constraint
4. **Bubble formation = volume growth** — Steam bubble formation increases steam volume; liquid does not "shrink" — it is displaced
5. **Derived RCS mass** — `RCSWaterMass = TotalPrimaryMass_lb - PZRWaterMass - PZRSteamMass` (by construction, guarantees conservation)
6. **Transient stability** — No single-timestep jumps > 0.5% in any mass or level indication during normal operations

### Exit Criteria

- AT-1 through AT-5 acceptance tests from v5.3.0 all PASS
- Drain duration matches realistic plant behavior
- No spurious RVLIS drops
- No PZR level spikes on RCP start
- Mass drift < 0.1% over 8-hour simulation

**Phase 0 Result:** NSSS primary mass inventory is plant-credible.

---

## v5.5.0.0 — Validation Dashboard Redesign

**Status:** BLOCKED by Phase 0 exit (v5.4.4.0)
**Priority:** First item in Phase 1 (after Phase 0 exits)
**Versioning Note:** Minor release (new subsystem — telemetry bus and dashboard architecture)

### Scope

Convert the CRITICAL tab into a real-time **Thermal Diagnostic Dashboard** with:
- Arc gauges for RCS pressure and T_avg
- Strip charts with rolling 60-second history
- SG thermal stack visualization with T_sat overlay
- Heat balance integrity display
- Telemetry bus decoupling UI from physics

### UI/UX Design Principles (Hard Requirements)

- **Animated state transitions** — Gauges, indicators, and fill levels must use smooth interpolated motion (needle sweep, fill animation, state fades). No jump-cuts unless an alarm trip demands an immediate visual snap.
- **Clear visual hierarchy and space usage** — Remove "text wall" panels; prioritize at-a-glance visuals (gauges, bars, sparklines) over dense numeric tables. Minimize wasted whitespace; every pixel earns its place.
- **No primary reliance on raw text blocks** — Critical parameters are displayed via graphical elements first. Raw text is permitted only as secondary detail (tooltips, expandable detail views, log panes).
- **Standardized alarm/state color semantics** — All indicators follow a consistent four-tier escalation: Normal → Warn → Alarm → Trip, each with defined color, border, and animation behavior. No ad-hoc color choices.
- **UI fully decoupled from physics** — No UI component may read simulation state objects directly. All data flows through a telemetry/observer layer that publishes snapshots.
- **Telemetry must support buffered history** — The telemetry layer provides per-channel ring buffers with configurable sampling rate and retention window, sufficient for strip charts and trend displays.
- **Performance: 60 FPS at 10× sim speed** — No per-frame heap allocations in the render path. Chart data points must use object pooling. UI update cost must stay under 4 ms/frame budget.

### Telemetry & Data Model Notes

- **Data schema:** Each monitored parameter is exposed as a `TelemetryChannel` containing the current value plus a fixed-length history ring buffer. Channels are grouped by subsystem (PZR, RCS, SG, CVCS).
- **Update cadence:** Telemetry snapshots are published once per physics tick (not per frame). UI interpolates between snapshots for smooth rendering.
- **Buffering approach:** Ring buffer per channel; default retention = 120 seconds of history at the current sampling rate. Buffer size is pre-allocated at init; no runtime resizing.
- **Hard rule:** UI reads ONLY the published `TelemetrySnapshot`. No UI code may reference `SystemState`, solver internals, or physics module fields directly.

### Exit Criteria
- All validation criteria V1–V10 pass
- Performance targets met (60 FPS at 10× speed)
- Telemetry decoupling verified
- UX principles above implemented and verified
- Telemetry decoupling verified — no direct physics reads from any UI component
- Performance verified with profiling evidence (frame-time captures at 10× speed)

---

# Phase 1 — Operator & Scenario Framework

**Status:** BLOCKED — Awaiting Phase 0 exit (v5.4.4.0)

## v5.5.0.0 — Validation Dashboard Redesign
## v5.5.1.0 — Scenario Selector Framework
## v5.5.2.0 — Heatup Temperature Hold at ~200°F
## v5.5.3.0 — In-Game Help System (F1)

---

# Phase 2 — Missing NSSS System Physics

**Status:** BLOCKED — Awaiting Phase 1 exit

## v5.6.0.0 — SG Energy, Pressure & Secondary Boundary Corrections (FF-01/02/03/08)
## v5.6.1.0 — SG Constants Calibration & Sensitivity Audit (FF-04)
## v5.6.2.0 — Placeholder Wiring + Core Data Validation
## v5.6.3.0 — PORV/SV Valve Models + CVCS Flow Models
## v5.6.4.0 — CCW System Model
## v5.6.5.0 — Heat Exchanger Models
## v5.6.6.0 — BRS Enhancements
## v5.6.7.0 — Excess Letdown Path Model
## v5.6.8.0 — VCT/CCP Thermal & Pump Models
## v5.6.9.0 — Active Operator SG Pressure Management

---

# Architecture Hardening

**Status:** BLOCKED — Awaiting Phase 2 exit

## v5.7.0.0 — Thermal/Mass/Flow Architecture Hardening (FF-09/10 absorbed)
## v5.7.1.0 — Multicore / Performance & Determinism

---

# Phase 3 — Cooldown & Secondary Systems

**Status:** BLOCKED — Awaiting Phase 2 exit

## v5.8.0.0 — Cooldown Procedures & AFW
## v5.8.1.0 — Advanced Steam Dump Modes

---

# Phase 4 — Power Ascension & Turbine Systems

**Status:** BLOCKED — Awaiting Phase 3 exit

## v5.9.0.0 — Power Ascension & Turbine-Generator

---

# Phase 5 — Full NSSS Thermal Realism

**Status:** BLOCKED — Awaiting Phase 4 exit

## v6.0.0.0 — Full NSSS Thermal Realism

---

# Technical Debt

| Item | Priority | Description | Target |
|------|----------|-------------|--------|
| **PZR DRAIN mass spike** | **CRITICAL** | 3,755 lbm spike (0.406%) during DRAIN/STABILIZE phase; recovers to ~1,000 lbm (0.11%) steady-state; volume↔mass semantics mismatch. See FF-05. | **v5.4.2.0** |
| **Bubble formation displacement semantics** | **CRITICAL** | Steam boiloff treated as liquid shrink. See FF-05. | **v5.4.2.0** |
| **Canonical mass continuity** | **CRITICAL** | V×ρ overwrites boundary-corrected mass. See FF-05. | **v5.4.2.0** |
| **RVLIS stale during drain** | **CRITICAL** | RCSWaterMass not updated. See FF-05. | **v5.4.2.0** |
| **PZR level spike on RCP start** | **CRITICAL** | Single-frame transient. See FF-07. | **v5.4.3.0 / FF-07** |
| **VCT conservation error growth** | **CRITICAL** | ~1,700 gal error due to RCS accounting drift. See FF-06. | **v5.4.4.0 / FF-06** |
| **SG pressure pinned during boiling** | **CRITICAL** | Pressure not rising despite steam generation. See FF-03. | **v5.6.0.0 / FF-03** |
| **Two-phase steady-state error exceeds target** | **HIGH** | 0.11% error at steady-state exceeds 0.1% target. See FF-05. | **v5.4.2.0** |
| **CoupledThermo V×ρ intermediate estimate (FF-05 #5)** | **HIGH** | In canonical mode, `M_RCS_est = V_RCS × ρ_RCS` intermediate estimate feeds PZR volume partitioning. If estimate is slightly off, PZR volumes are perturbed, causing spurious mass redistribution at regime transitions. **Deferred from v5.4.2.0:** requires solver partition redesign beyond conservation-tightening scope; no safety or conservation impact in current heatup-only regime. | **v5.7.0.0** (Architecture Hardening) |
| **Inventory Audit Baseline Type Mismatch** | **HIGH** | Dashboard compares INITIAL (geometric gal) vs SYSTEM TOTAL (mass-derived gal); thermal expansion causes phantom errors. Deferred from v5.4.1 closure. | **v5.4.2.0** |
| **Simulation Timestep & Throughput Architecture** | **MEDIUM** | Sim ratio cap (6.5x vs 10x), fixed timestep decoupling from frame rate, possible substepping for PVT coupling. At 1/360 hr (~10s) timestep, actuator lag filters are transparent (alpha=1.0). Finer timesteps would enable smoother physics traces and better control dynamics. See `Future_Features/FUTURE_ARCHITECTURE_ITEMS.md` Item 2. | **v5.7.1.0** |
| **Thermal/Mass/Flow Architecture Hardening** | **MEDIUM** | Eliminate duplicate state, define explicit module boundaries, centralize unit handling, regime state machine, improve testability. Prerequisite for multicore work. See `Future_Features/FUTURE_ARCHITECTURE_ITEMS.md` Item 1. | **v5.7.0.0** |
| Two-phase mass conservation | HIGH | v5.3.0 validation failed. See FF-05. | v5.4.2.0 |
| Logging placement | MEDIUM | Logging code in wrong partial class | v5.4.2.0 |
| HeaterMode cleanup | LOW | Consolidate heater mode enums | v5.5.0.0 |
| Physics module tests | MEDIUM | Add unit tests for all physics modules | Ongoing |
| PlantConstants validation | LOW | Add ValidateConstants() coverage | Ongoing |
| Memory optimization | LOW | Profile and optimize per-frame allocations | v5.5.0.0 |
| Heatup level program discontinuity | LOW | 60% at 557°F vs 25% at-power | v5.5.0.0 |
| v5.0.2 forensics block not logged post-bubble | LOW | Cosmetic | v5.4.2.0 |
| RCS heatup rate exceeds 50°F/hr post-RCP start | MEDIUM | Tech Spec limit check | v5.4.3.0 |
| ~~rcsWaterMass stale during solid ops~~ | — | **RESOLVED v5.0.2** | — |
| ~~SG heat sink causes RCS cooldown~~ | — | **RESOLVED v5.1.0** | — |

---

# Completed Enhancements

| Version | Description | Date |
|---------|-------------|------|
| v0.7.0 | Heatup Simulation Core | 2026-02 |
| v0.8.0 | SG Secondary Thermal Mass | 2026-02 |
| v1.0.0 | Reactor Operator GUI | 2026-02-07 |
| v1.1.0 | HZP Stabilization & Reactor Operations Handoff | 2026-02 |
| v1.3.0 | RHR Heat Exchanger Model | 2026-02-10 |
| v2.0.10 | Inventory audit state-based mass fix | 2026-02 |
| v3.0.0 | SG Thermal Model Physics Overhaul + RHR | 2026-02-10 |
| v4.0.0 | Reactor Operator Screen Visual Overhaul | 2026-02-11 |
| v4.1.0 | Mosaic Board Visual Upgrade | 2026-02-11 |
| v4.2.2 | Bottom Panel Layout Fix & Annunciator Alarm Panel | 2026-02-11 |
| v4.3.0 | SG Secondary Pressure Model, Dynamic Boiling | 2026-02-11 |
| v4.4.0 | PZR Level/Pressure Control Fix | 2026-02-11 |
| v5.0.0 | Startup Sequence Realism Overhaul | 2026-02-11 |
| v5.0.2 | Solid PZR Mass Conservation Fix | 2026-02-11 |
| v5.0.3 | Inventory Audit Reconciliation Patch | 2026-02-12 |
| v5.1.0 | SG Pressure–Saturation Coupling Fix | 2026-02-12 |
| v5.2.0 | CRITICAL Tab (At-a-Glance Validation Overview) | 2026-02-12 |
| v5.4.1 | Inventory Audit Fix + Startup Pressurization Stabilization | 2026-02-13 |

---

# Document Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-13 | **5.6** | **v5.4.2.0 status updated to IMPLEMENTED — PENDING VALIDATION (4 of 5 FF-05 items). FF-05 Issue #5 (CoupledThermo V×ρ intermediate estimate) deferred to v5.7.0.0 (Architecture Hardening) — requires solver partition redesign beyond conservation-tightening scope. Added to Technical Debt with deferral reason. Triage FF-05 updated with deferral notice and implementation plan reference.** |
| 2026-02-13 | **5.5** | **Complete re-versioning of all outstanding features under 4-level structured versioning (VERSIONING_POLICY.md). Baseline: v5.4.1. All planned versions now use 4-level format from v5.4.2.0 onwards. Physics corrections (FF-05/07/06) retained under Phase 0 as v5.4.2.0–v5.4.4.0. Phase 1 operator tooling: v5.5.0.0–v5.5.3.0. FF-01/02/03/08 (SG corrections) consolidated into v5.6.0.0; FF-04 (SG calibration) assigned v5.6.1.0. Phase 2 NSSS physics: v5.6.2.0–v5.6.9.0. FF-09/10 absorbed into v5.7.0.0 (Architecture Hardening). Multicore v5.7.1.0 with single-thread canonical / parity-lock acceptance criteria. Phase 3: v5.8.x.0, Phase 4: v5.9.0.0, Phase 5: v6.0.0.0. VERSIONING_POLICY.md updated: Patch scope clarified for small feature additions within established domains. No unexplained version jumps. Version map split into Completed (historical 3-level) and Planned (4-level) tables.** |
| 2026-02-13 | **5.4** | **Adopted four-level versioning policy (Major.Minor.Patch.Revision) per VERSIONING_POLICY.md. Applies to all versions after v5.4.1. Historical versions not retroactively amended. Updated all forward-looking v5.4.2 references to v5.4.2.0 (first version under new policy). Added versioning policy notice to Overview section.** |
| 2026-02-13 | **5.3** | **Version baseline correction: re-baselined all forward-looking references to v5.4.1. v5.4.0 status changed from STAGE 7 VALIDATION IN PROGRESS to PARTIALLY COMPLETE. Added v5.4.2 (Mass Conservation Tightening) to version map as PLANNED — NEXT. Retargeted all Technical Debt items previously assigned to v5.4.0/v5.4.1 residuals → v5.4.2. Updated Phase 0/1 blocked-by references from v5.4.0 → v5.4.2. Cross-referenced Technical Debt items to TRIAGE_v5.4.1_PostStabilization.md feature entries (FF-03/05/06/07). No historical changelogs modified.** |
| 2026-02-13 | **5.2** | **v5.4.1 marked COMPLETE. Updated title to "Inventory Audit Fix + Startup Pressurization Stabilization". Added to Completed Enhancements. Added "Simulation Timestep & Throughput Architecture" to Technical Debt (MEDIUM priority). Updated document header to v5.4.1. Note: Original v5.4.1 scope items (PZR DRAIN Mass Spike, Inventory Audit Baseline Type Mismatch) deferred to Technical Debt, not blocked by this closure.** |
| 2026-02-13 | **5.1** | **v5.5.0: Added "UI/UX Design Principles (Hard Requirements)" subsection (animated transitions, visual hierarchy, no text-wall reliance, alarm color semantics, telemetry decoupling, buffered history, 60 FPS perf). Added "Telemetry & Data Model Notes" subsection (TelemetryChannel schema, update cadence, ring buffer approach, hard read-only rule). Updated Exit Criteria with UX verification, telemetry decoupling evidence, and profiling requirement.** |
| 2026-02-12 | **5.0** | **Consolidated v5.4.1 to address BOTH issues from Stage 7 validation: (Part A) PZR DRAIN Mass Spike — 3,755 lbm spike (0.406%) during DRAIN/STABILIZE, recovers to ~1,000 lbm (0.11%) at steady-state; added 3-stage investigation plan (instrumented mass ledger, transfer semantics fix, canonical measurement audit). (Part B) Inventory Audit Baseline Type Mismatch — display compares geometric vs mass-derived gallons. Combined validation criteria and files affected. Removed v5.4.2 (merged into v5.4.1). Updated v5.4.0 status to STAGE 7 VALIDATION IN PROGRESS.** |
| 2026-02-12 | **4.1** | **Renamed v5.4.1 to "Inventory Audit Baseline Type Mismatch Fix". Clarified problem: INITIAL uses geometric gallons, SYSTEM TOTAL uses mass-derived gallons; thermal expansion causes phantom errors even when mass conserved. Updated fix to make validation canonical in MASS (lbm). Updated acceptance criteria: bounded error (~≤1 lbm), matches Inventory Audit OK status, no gal swings from thermal expansion.** |
| 2026-02-12 | 4.0 | Added v5.4.1 Inventory Display Conservation Fix to roadmap. Documented Type C/D comparison bug (mass-derived gallons vs geometric baseline) causing phantom ~13,000 gal "error" from thermal expansion. Added to Technical Debt as HIGH priority. Root cause: `systemInventoryError_gal` compares incompatible quantities. |
| 2026-02-12 | 3.9 | Added Issue #6 (SG Boiling Does Not Pressurize Secondary) to v5.4.0 scope. Added Stage 6 for SG pressure/steam inventory diagnosis. Added SG pressure acceptance criterion. Added SG pressure pinned to technical debt as CRITICAL. Renumbered validation stage to 7.** |
| 2026-02-12 | 3.8 | Added Issue #5 (VCT Conservation Error Growth ~1,600–1,700 gal) to v5.4.0 scope. Added Stage 5 for VCT diagnosis. Added VCT conservation acceptance criterion (<10 gal steady-state). Added VCT to technical debt as CRITICAL. |
| 2026-02-12 | 3.7 | Added v5.4.0 Primary Mass & Pressurizer Stabilization Release as HIGH-PRIORITY item. Moved above Dashboard Implementation (v5.5.0). Marked as Phase: Core Physics Stabilization, Priority: CRITICAL. Deprecated v5.3.1 (folded into v5.4.0). Updated version map. Added four critical technical debt items (bubble formation semantics, canonical mass continuity, RVLIS stale, PZR spike). Updated Phase 0 description. |
| 2026-02-12 | 3.6 | v5.3.0 status changed from COMPLETE to IN PROGRESS — VALIDATION FAILED. Evidence: Mass Conservation FAIL at Sim Time 10.25 hr. AT-1 through AT-5 re-opened as FAIL/PENDING. Added v5.3.1 as immediate follow-on patch (PLANNED/NEXT). v5.4.0 now BLOCKED by v5.3.1. Two-phase mass conservation returned to Technical Debt as CRITICAL. |
| 2026-02-12 | 3.5 | v5.3.0 marked COMPLETE (premature — validation not performed) |
| 2026-02-12 | 3.4 | Added v5.3.0 Primary Inventory Boundary Repair as #1 priority. Added critical priority override section. Added two-phase mass conservation to Technical Debt as CRITICAL. Renumbered subsequent versions. Updated version map. |
| 2026-02-12 | 3.3 | v5.2.0 COMPLETE: Added CRITICAL tab |
| 2026-02-12 | 3.2 | v5.1.0 completed |
| 2026-02-11 | 3.1 | Full version assignment |
| 2026-02-11 | 3.0 | Major restructure: Aligned to Master Roadmap v3.0 |

---

*End of Roadmap*
