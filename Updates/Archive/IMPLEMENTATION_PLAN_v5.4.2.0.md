# Implementation Plan — v5.4.2.0

## Primary Mass Conservation Tightening (FF-05)

**Version:** 5.4.2.0
**Date:** 2026-02-13
**Baseline:** v5.4.1
**Triage Source:** FF-05 in `TRIAGE_v5.4.1_PostStabilization.md`
**Classification:** Critical Physics Blocker — Phase 0 residual
**Governing Document:** `PROJECT_CONSTITUTION.md`

---

## Scope

Four mass conservation gaps identified through static code review of the v5.4.1 baseline. Each gap violates the conservation-by-construction principle established in v5.3.0–v5.4.1: the canonical mass ledger (`TotalPrimaryMass_lb`) is authoritative, and no code path may overwrite mass from V×ρ after the ledger is established.

| Stage | Issue | File(s) to Modify |
|-------|-------|--------------------|
| 1 | Surge mass transfer uses post-step density | `Assets/Scripts/Physics/SolidPlantPressure.cs` |
| 2 | FormBubble recalculates mass from V×ρ | `Assets/Scripts/Physics/PressurizerPhysics.cs` |
| 3 | DRAIN steam reconciliation overwrites canonical mass | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` |
| 4 | Canonical mass ledger initialization fragility | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`, `Assets/Scripts/Validation/HeatupSimEngine.cs` |

---

## Non-Goals

- No changes to CoupledThermo partitioning logic (FF-05 Issue #5 deferred)
- No changes to the ledger architecture (it is correct in design)
- No changes to boundary flow accounting (CVCS net, relief)
- No changes to logging or display
- No regime dispatch refactors
- No UI/dashboard edits
- No opportunistic cleanup

---

## Stage 1 — Fix Surge Mass Pre-Step Density

**File:** `SolidPlantPressure.cs`

**Problem:** `SolidPlantPressure.Update()` computes `surgeMass_lb = dV_pzr_ft3 * rho_pzr_post` at line 632-633, where `rho_pzr_post` is evaluated AFTER T_pzr has already been updated by heater input (line 391). The surge mass transfer should use the density at which water actually left the PZR (pre-heating conditions). During rapid pressurization (dP/dt > 100 psi/hr), the error can exceed 10 lbm per step.

**Change:**
1. Capture `rho_pzr_preStep = WaterDensity(T_pzr, P)` before the temperature update
2. Use `rho_pzr_preStep` for surge mass calculation
3. Retain `rho_pzr_post` for diagnostics display only

**Verification:** No change to diagnostic outputs. Surge mass calculation uses physically correct density.

---

## Stage 2 — Fix FormBubble Mass Preservation

**File:** `PressurizerPhysics.cs`

**Problem:** `FormBubble()` at lines 149-153 computes `WaterMass = WaterVolume * rhoWater` and `SteamMass = SteamVolume * rhoSteam`, overwriting the conserved solid water mass. If the V×ρ sum differs from the pre-bubble mass, ~100 lbm can appear or vanish at the transition.

**Change:**
1. Snapshot `totalPzrMass = state.WaterMass` before any overwrite
2. Compute `SteamMass = SteamVolume * rhoSteam` (geometry-constrained by target level)
3. Derive `WaterMass = totalPzrMass - SteamMass` (remainder guarantees exact conservation)

**Verification:** `WaterMass + SteamMass == totalPzrMass` by construction. Existing self-test (Test 13) must still pass.

---

## Stage 3 — Remove DRAIN Steam V×ρ Reconciliation

**File:** `HeatupSimEngine.BubbleFormation.cs`

**Problem:** Line 417 overwrites `PZRSteamMass = PZRSteamVolume * rhoSteam` after the mass-conserving phase change (lines 391-392) and CVCS drain (lines 395-397). This V×ρ reconciliation destroys the mass established by energy-based phase change, causing the ~3,755 lbm DRAIN spike flagged by the v5.4.1 forensic audit.

**Change:**
1. Delete the `PZRSteamMass = PZRSteamVolume * rhoSteam` overwrite
2. Steam mass is set exclusively by the mass-conserving phase change at Step 1
3. Canonical direction enforced: mass → volume, never volume → mass

**Verification:** Forensic audit (line 448) should report `Δm_sys ≈ 0` instead of VIOLATION.

---

## Stage 4 — Init-to-First-Step Ledger Re-Baseline

**Files:** `HeatupSimEngine.Init.cs` (comments), `HeatupSimEngine.cs` (field + logic)

**Problem:** `TotalPrimaryMass_lb` is initialized at simulation start using V×ρ. If the first physics timestep produces slightly different component masses (due to density lookup path differences between Init and first Step), the ledger baseline is already wrong. No warm-up tolerance exists.

**Change:**
1. Add `private bool firstStepLedgerBaselined = false` field
2. After all regime branches converge in the main update loop (section "3. FINAL UPDATES"), insert one-time re-baseline: `actualTotal = RCS + PZR_water + PZR_steam`, update both `TotalPrimaryMass_lb` and `InitialPrimaryMass_lb`
3. Runs exactly once, then flag prevents re-execution
4. Add forward-reference comments at both cold-shutdown and warm-start init paths

**Verification:** First-timestep conservation check passes. Ledger matches actual solver state.

---

## Deferred Items

| Item | Reason | Target |
|------|--------|--------|
| FF-05 Issue #5: CoupledThermo V×ρ intermediate estimate (`CoupledThermo.cs:237-259`) | Requires solver redesign; out of scope per user directive | TBD |

---

## Acceptance Criteria

| # | Criterion | Target |
|---|-----------|--------|
| AC-1 | No code path computes mass from V×ρ after ledger initialization | Zero violations |
| AC-2 | FormBubble preserves total PZR mass | Within 0.01 lbm |
| AC-3 | Surge transfer uses density at transfer conditions (pre-step) | Implemented |
| AC-4 | Max transient mass error during DRAIN | < 50 lbm (was ~3,755 lbm) |
| AC-5 | Steady-state mass error over 8-hour simulation | < 0.05% |
| AC-6 | First-timestep conservation check passes | Zero drift |

---

## Validation Requirements

Per Constitution Section 6, all physics changes must report:
- Max transient mass error (lbm + %)
- Steady-state mass error
- Timestamp of peak deviation

Per Constitution Section 7.1, version closure requires:
- Full baseline scenario run
- Conservation metrics within tolerance
- No regression of previously passing checks

**Changelog will be created only after all acceptance criteria are verified and validation completes (per Constitution Section 2.2).**

---

## Stage-Gate Protocol

Per Constitution Section 1.2: Physics changes require **MANDATORY** stage-gating. Execute one stage at a time, stop, print artifacts, wait for explicit approval.
